using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Models;
using OSGeo.OGR;
using OSGeo.OSR;
using OgrLayer = OSGeo.OGR.Layer;
using OgrFeature = OSGeo.OGR.Feature;
using OgrGeometry = OSGeo.OGR.Geometry;

namespace GdalToolsLib.Workflows;

public sealed class MasterLayerSeparator
{
    private static readonly string[] WildlifeOnly = ["Kanton", "Schutzstatus", "Grundlage", "Beschlussjahr", "Bestimmungen", "Bestimmungen_Kt", "Zusatzinformation", "RefKanton", "Schutzzeit"];
    private static readonly string[] WildlifeFields = ["ObjNummer", "Name", "RefObjBlatt", "DesignatType", "IUCNCategory", "Inkraftsetzungsdatum", "Mutationsdatum", "Mutationsgrund", "TeilObjNummer", .. WildlifeOnly];
    private readonly OgctDataSourceAccessor _accessor = new();

    public string[] GetLayerNames(string path)
    {
        using var db = _accessor.OpenOrCreateDatasource(path);
        var names = new List<string>();
        foreach (var layer in db.GetLayers())
        {
            using (layer) names.Add(layer.Name);
        }
        return names.ToArray();
    }

    public void Run(string sourcePath, string sourceLayer, string targetPath, string jbg, string wildlife,
        IReadOnlyCollection<string> approvedReplacements, IProgress<string> progress)
    {
        var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourcePath));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetPath));
        if (!Directory.Exists(source) || !source.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase) ||
            !target.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(source, target, StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith(target + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Bitte separate Quell- und Ziel-FGDB wählen.");
        if (string.Equals(jbg, wildlife, StringComparison.OrdinalIgnoreCase) ||
            new[] { jbg, wildlife }.Any(n => !Regex.IsMatch(n, @"\A[\p{L}][\p{L}\d_]{0,159}\z")))
            throw new ArgumentException("Zwei unterschiedliche Layernamen angeben (max. 160 Zeichen, mit Buchstaben beginnen, danach Buchstaben, Ziffern oder Unterstriche).");

        using var masterDb = _accessor.OpenOrCreateDatasource(source);
        using var master = masterDb.OgrDataSource.GetLayerByName(sourceLayer) ?? throw new InvalidDataException("MASTER-Layer fehlt.");
        using var schema = master.GetLayerDefn();
        using var srs = master.GetSpatialRef();
        using var lv95 = new SpatialReference("");
        Check(lv95.ImportFromEPSG(2056), "EPSG:2056 laden");
        if (srs == null || srs.IsSame(lv95, null) != 1 || Ogr.GT_Flatten(master.GetGeomType()) is not (wkbGeometryType.wkbMultiPolygon or wkbGeometryType.wkbPolygon))
            throw new InvalidDataException("Eine Polygon-Featureclass in CH1903+ / LV95 (EPSG:2056) wählen.");
        foreach (var name in WildlifeFields.Append("JB_KAT"))
            if (schema.GetFieldIndex(name) < 0) throw new InvalidDataException($"MASTER-Feld fehlt: {name}");
        using var partNumber = schema.GetFieldDefn(schema.GetFieldIndex("TeilObjNummer"));
        if (partNumber.GetFieldType() != FieldType.OFTString || partNumber.GetWidth() == 1)
            throw new InvalidDataException("TeilObjNummer muss ein Textfeld für mindestens zwei Zeichen sein.");

        progress.Report("Prüfe Attribute je ObjNummer …");
        var groups = ReadGroups(master);
        using var destination = _accessor.OpenOrCreateDatasource(target, EAccessLevel.Full, true);
        var db = destination.OgrDataSource;
        using var exports = new SchemaExports(Path.GetDirectoryName(source)!);
        Check(db.StartTransaction(1), "FGDB-Transaktion öffnen");
        try
        {
            foreach (var name in new[] { jbg, wildlife })
            {
                using (var existing = db.GetLayerByName(name))
                    if (existing == null) continue;
                if (!approvedReplacements.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new IOException($"Ziel-Layer existiert bereits: {name}");
                Check(db.DeleteLayer(destination.GetLayerIndexByName(name)), $"Layer {name} ersetzen");
            }

            progress.Report($"Kopiere {master.GetFeatureCount(1)} JBG-Features …");
            master.ResetReading();
            using (var jb = db.CopyLayer(master, jbg, []) ?? throw new IOException($"Kopieren von {jbg} fehlgeschlagen."))
            {
                CheckName(jb, jbg);
                using var jbSchema = jb.GetLayerDefn();
                foreach (var name in WildlifeOnly) Check(jb.DeleteField(jbSchema.GetFieldIndex(name)), $"Feld {name} entfernen");
                if (jb.GetFeatureCount(1) != master.GetFeatureCount(1)) throw new IOException("JBG-Kopie unvollständig.");
                exports.Stage("JBG", jb);
            }
            using (var wt = db.CreateLayer(wildlife, srs, wkbGeometryType.wkbMultiPolygon, []) ?? throw new IOException($"Anlegen von {wildlife} fehlgeschlagen."))
            {
                CheckName(wt, wildlife);
                foreach (var name in WildlifeFields)
                {
                    using var field = schema.GetFieldDefn(schema.GetFieldIndex(name));
                    Check(wt.CreateField(field, 0), $"Feld {name} anlegen");
                }
                var count = 0;
                foreach (var (number, group) in groups)
                {
                    WriteGroup(master, wt, number, group.Ids);
                    if (++count % 10 == 0 || count == groups.Count) progress.Report($"Dissolve: {count} von {groups.Count} Objekten");
                }
                exports.Stage("WTSCHG", wt);
            }
            exports.Stage("MASTER", master);
            exports.Publish();
            Check(db.CommitTransaction(), "Ziel-FGDB speichern");
        }
        catch
        {
            try { Check(db.RollbackTransaction(), "Ziel-FGDB zurücksetzen"); }
            finally { exports.Restore(); }
            throw;
        }
        progress.Report($"Gespeichert: {jbg} und {wildlife} in {target}. JSON-Dateien in {Path.GetDirectoryName(source)}.");
    }

    private static Dictionary<string, (List<long> Ids, string?[] Values)> ReadGroups(OgrLayer master)
    {
        var groups = new Dictionary<string, (List<long> Ids, string?[] Values)>(StringComparer.Ordinal);
        var compared = WildlifeFields.Except(["TeilObjNummer", "ObjNummer"]).ToArray();
        OgrFeature? feature;
        while ((feature = master.GetNextFeature()) != null)
        {
            using (feature)
            {
                if (feature.GetFieldAsString("JB_KAT") == "III") continue;
                var number = feature.GetFieldAsString("ObjNummer");
                if (string.IsNullOrWhiteSpace(number)) throw new InvalidDataException($"ObjNummer fehlt bei FID {feature.GetFID()}.");
                var values = compared.Select(name => feature.IsFieldSetAndNotNull(feature.GetFieldIndex(name)) ? feature.GetFieldAsString(name) : null).ToArray();
                if (!groups.TryGetValue(number, out var group))
                    groups.Add(number, group = (new List<long>(), values));
                for (var i = 0; i < compared.Length; i++)
                    if (group.Values[i] != values[i])
                        throw new InvalidDataException($"ObjNummer {number}, Feld {compared[i]}: «{group.Values[i] ?? "<NULL>"}» und «{values[i] ?? "<NULL>"}».");
                group.Ids.Add(feature.GetFID());
            }
        }
        return groups;
    }

    private static void WriteGroup(OgrLayer master, OgrLayer target, string number, List<long> ids)
    {
        using var polygons = new OgrGeometry(wkbGeometryType.wkbMultiPolygon);
        using var definition = target.GetLayerDefn();
        using var result = new OgrFeature(definition);
        foreach (var id in ids)
        {
            using var feature = master.GetFeature(id) ?? throw new InvalidDataException($"MASTER-FID {id} fehlt.");
            if (id == ids[0]) Check(result.SetFrom(feature, 1), $"Attribute von {number} übernehmen");
            using var geom = feature.GetGeometryRef();
            if (geom == null || geom.IsEmpty()) throw new InvalidDataException($"Leere Geometrie bei ObjNummer {number}, FID {id}.");
            if (Ogr.GT_Flatten(geom.GetGeometryType()) == wkbGeometryType.wkbPolygon)
                Check(polygons.AddGeometry(geom), $"Polygon {id} übernehmen");
            else
                for (var i = 0; i < geom.GetGeometryCount(); i++)
                {
                    using var part = geom.GetGeometryRef(i);
                    Check(polygons.AddGeometry(part), $"Teilfläche {id} übernehmen");
                }
        }
        using var union = polygons.UnionCascaded();
        if (union == null || union.IsEmpty()) throw new InvalidDataException($"Dissolve für ObjNummer {number} fehlgeschlagen.");
        result.SetField("TeilObjNummer", "00");
        result.SetFID(-1);
        Check(result.SetGeometry(union), $"Geometrie {number} setzen");
        Check(target.CreateFeature(result), $"ObjNummer {number} schreiben");
    }

    private static void CheckName(OgrLayer layer, string name)
    {
        if (layer.GetName() != name) throw new InvalidDataException($"Die FGDB akzeptiert den Layernamen {name} nicht unverändert.");
    }

    private static void Check(int error, string operation)
    {
        if (error != 0) throw new IOException($"{operation}: {OSGeo.GDAL.Gdal.GetLastErrorMsg()} ({error})");
    }
}


using System;
using System.Collections.Generic;
using GdalToolsLib.Layer;
using OSGeo.OGR;

namespace GdalToolsLib.Models;

/// <summary>Checked operations for comparing and updating polygon features.</summary>
public static class PolygonComparisonAccess
{
    public static string[] GetLayerNamesForComparison(this OgctDataSource source)
    {
        var names = new List<string>();
        for (int i = 0; i < source.OgrDataSource.GetLayerCount(); i++)
        {
            using var layer = source.OgrDataSource.GetLayerByIndex(i);
            names.Add(layer.GetName());
        }
        return names.ToArray();
    }

    public static List<FieldDefnInfo> GetComparisonFields(this IOgctLayer layer)
    {
        using var definition = ((OgctLayer)layer).OgrLayer.GetLayerDefn();
        var fields = new List<FieldDefnInfo>();
        for (int i = 0; i < definition.GetFieldCount(); i++)
        {
            using var field = definition.GetFieldDefn(i);
            fields.Add(new FieldDefnInfo(i, field));
        }
        return fields;
    }

    public static double ValidatePolygonPair(this IOgctLayer master, IOgctLayer slave)
    {
        var left = ((OgctLayer)master).OgrLayer;
        var right = ((OgctLayer)slave).OgrLayer;
        if (!IsPolygon(left.GetGeomType()) || !IsPolygon(right.GetGeomType()))
            throw new InvalidOperationException("Beide Featureclasses müssen Polygon- oder Multi-Polygon-Layer sein.");
        using var leftSrs = left.GetSpatialRef();
        using var rightSrs = right.GetSpatialRef();
        if (leftSrs == null || rightSrs == null)
            throw new InvalidOperationException("Mindestens einem Layer fehlt das Koordinatensystem.");
        if (leftSrs.IsSame(rightSrs, null) != 1)
            throw new InvalidOperationException("Die Layer haben unterschiedliche Koordinatensysteme. Bitte einen anderen Layer auswählen.");
        if (leftSrs.IsProjected() != 1)
            throw new InvalidOperationException("Für die Fläche in qm ist ein projiziertes Koordinatensystem erforderlich.");
        double units = leftSrs.GetLinearUnits();
        if (!double.IsFinite(units) || units <= 0)
            throw new InvalidOperationException("Die Längeneinheit des Koordinatensystems ist ungültig.");
        return units * units;
    }

    public static object? ReadComparisonKey(this IOgctFeature feature, FieldDefnInfo field)
    {
        var raw = ((OgctFeature)feature).OgrFeature;
        if (!raw.IsFieldSetAndNotNull(field.OgrIndex)) return null;
        return field.Type switch
        {
            FieldType.OFTInteger => raw.GetFieldAsInteger(field.OgrIndex),
            FieldType.OFTInteger64 => raw.GetFieldAsInteger64(field.OgrIndex),
            FieldType.OFTReal => raw.GetFieldAsDouble(field.OgrIndex),
            _ => raw.GetFieldAsString(field.OgrIndex)
        };
    }

    public static void ValidatePolygon(this IOgctGeometry geometry)
    {
        var raw = ((OgctGeometry)geometry).OgrGeometry;
        if (raw == null || raw.IsEmpty()) throw new InvalidOperationException("Geometrie fehlt oder ist leer.");
        if (!IsPolygon(raw.GetGeometryType())) throw new InvalidOperationException("Geometrie ist keine Fläche.");
        if (!raw.IsValid()) throw new InvalidOperationException("Geometrie ist ungültig.");
    }

    public static double SymmetricDifferenceArea(this IOgctGeometry master, IOgctGeometry slave)
    {
        master.ValidatePolygon();
        slave.ValidatePolygon();
        using var difference = ((OgctGeometry)master).OgrGeometry.SymDifference(((OgctGeometry)slave).OgrGeometry);
        if (difference == null) throw new InvalidOperationException("Symmetrische Differenz konnte nicht berechnet werden.");
        double area = difference.IsEmpty() ? 0 : difference.GetArea();
        if (!double.IsFinite(area) || area < 0) throw new InvalidOperationException("Ungültiges Ergebnis der Flächenberechnung.");
        return area;
    }

    public static void UpdatePolygonChecked(this IOgctLayer layer, IOgctFeature feature, IOgctGeometry geometry)
    {
        var rawLayer = ((OgctLayer)layer).OgrLayer;
        if (!rawLayer.TestCapability("RandomWrite")) throw new InvalidOperationException("Der Slave-Layer unterstützt keine Aktualisierung von Features.");
        geometry.ValidatePolygon();
        var rawFeature = ((OgctFeature)feature).OgrFeature;
        int setResult = rawFeature.SetGeometry(((OgctGeometry)geometry).OgrGeometry);
        if (setResult != 0) throw new InvalidOperationException($"Geometrie konnte nicht zugewiesen werden (OGR {setResult}).");
        int updateResult = rawLayer.SetFeature(rawFeature);
        if (updateResult != 0) throw new InvalidOperationException($"Feature konnte nicht gespeichert werden (OGR {updateResult}).");
        int syncResult = rawLayer.SyncToDisk();
        if (syncResult != 0) throw new InvalidOperationException($"Speichern auf Datenträger fehlgeschlagen (OGR {syncResult}).");
    }

    private static bool IsPolygon(wkbGeometryType type)
    {
        string name = type.ToString();
        return name.StartsWith("wkbPolygon", StringComparison.Ordinal) || name.StartsWith("wkbMultiPolygon", StringComparison.Ordinal);
    }
}


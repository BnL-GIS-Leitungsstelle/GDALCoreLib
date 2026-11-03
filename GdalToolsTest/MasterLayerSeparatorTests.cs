using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using GdalToolsLib.Models;
using GdalToolsLib.Workflows;
using OSGeo.OGR;
using OSGeo.OSR;
using Xunit;

namespace GdalToolsTest;

public sealed class MasterLayerSeparatorTests
{
    public static bool HasAcceptanceSample => File.Exists(Environment.GetEnvironmentVariable("JBG_MASTER_SAMPLE_ZIP"));

    [Fact(SkipUnless = nameof(HasAcceptanceSample), Skip = "JBG_MASTER_SAMPLE_ZIP muss auf das Referenzarchiv zeigen.")]
    public void SuppliedMasterProduces114And43Features()
    {
        var directory = Directory.CreateTempSubdirectory("JbgSample-");
        try
        {
            ZipFile.ExtractToDirectory(Environment.GetEnvironmentVariable("JBG_MASTER_SAMPLE_ZIP")!, directory.FullName);
            var source = Path.Combine(directory.FullName, "jbg_wtschg_master.gdb");
            var target = Path.Combine(directory.FullName, "result.gdb");
            const string jbName = "N2023_Revision_jagdbanngebiete_230510_250501";
            const string wtName = "N2023_Stand_wildtierschutzgebiete";
            var separator = new MasterLayerSeparator();
            separator.GetLayerNames(source).Should().Contain("jbg_wtschg_master");
            separator.Run(source, "jbg_wtschg_master", target, jbName, wtName, [], new Progress<string>());
            using var db = Ogr.Open(target, 0);
            using var jb = db.GetLayerByName(jbName);
            using var wt = db.GetLayerByName(wtName);
            jb.GetFeatureCount(1).Should().Be(114);
            wt.GetFeatureCount(1).Should().Be(43);
            using var srs = wt.GetSpatialRef();
            srs.GetAuthorityCode(null).Should().Be("2056");
            using var schema = wt.GetLayerDefn();
            schema.GetFieldCount().Should().Be(18);
            Feature row;
            while ((row = wt.GetNextFeature()) != null)
            {
                using (row)
                {
                    row.GetFieldAsString("TeilObjNummer").Should().Be("00");
                    using var geometry = row.GetGeometryRef();
                    geometry.GetArea().Should().BePositive();
                }
            }
        }
        finally { directory.Delete(true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparatesPolygonsOrReportsConflictingAttributes(bool conflict)
    {
        var directory = Directory.CreateTempSubdirectory("JbgTest-");
        try
        {
            var source = Path.Combine(directory.FullName, "master.gdb");
            var target = Path.Combine(directory.FullName, "ziel.gdb");
            var separator = new MasterLayerSeparator();
            CreateMaster(source, conflict);
            void Run(string[] replacements) => separator.Run(source, "master", target, "JBG", "WTSCHG", replacements, new Progress<string>());
            if (conflict)
            {
                ((Action)(() => Run([]))).Should().Throw<InvalidDataException>().WithMessage("*ObjNummer 1*Name*Alpha*Beta*");
                Directory.Exists(target).Should().BeFalse();
                Directory.GetFiles(directory.FullName, "*.json").Should().BeEmpty();
                return;
            }

            Run([]);
            ((Action)(() => Run([]))).Should().Throw<IOException>().WithMessage("*existiert bereits*");
            Run(["JBG", "WTSCHG"]);
            using var db = Ogr.Open(target, 0);
            using var jb = db.GetLayerByName("JBG");
            jb.GetFeatureCount(1).Should().Be(4);
            using var jbSchema = jb.GetLayerDefn();
            jbSchema.GetFieldIndex("Kanton").Should().Be(-1);
            jbSchema.GetFieldIndex("JB_KAT").Should().BeGreaterThanOrEqualTo(0);
            using var wt = db.GetLayerByName("WTSCHG");
            wt.GetFeatureCount(1).Should().Be(1);
            using var feature = wt.GetNextFeature();
            feature.GetFieldAsString("TeilObjNummer").Should().Be("00");
            using var geom = feature.GetGeometryRef();
            geom.GetGeometryCount().Should().Be(2);
            geom.GetArea().Should().BeApproximately(10, 0.00001);
            foreach (var role in new[] { "MASTER", "JBG", "WTSCHG" })
            {
                using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, role + ".json")));
                json.RootElement.GetProperty("Fields").EnumerateArray().Select(f => f.GetProperty("Name").GetString()).Should().Contain("ObjNummer");
            }
        }
        finally { directory.Delete(true); }
    }

    private static void CreateMaster(string path, bool conflict)
    {
        _ = new OgctDataSourceAccessor();
        using var driver = Ogr.GetDriverByName("OpenFileGDB");
        using var db = driver.CreateDataSource(path, []);
        using var srs = new SpatialReference("");
        srs.ImportFromEPSG(2056);
        using var layer = db.CreateLayer("master", srs, wkbGeometryType.wkbMultiPolygon, []);
        foreach (var field in "ObjNummer Name RefObjBlatt DesignatType IUCNCategory Inkraftsetzungsdatum Mutationsdatum Mutationsgrund TeilObjNummer JB_KAT Kanton Schutzstatus Grundlage Beschlussjahr Bestimmungen Bestimmungen_Kt Zusatzinformation RefKanton Schutzzeit".Split(' '))
        {
            using var def = new FieldDefn(field, FieldType.OFTString);
            layer.CreateField(def, 0);
        }
        using var schema = layer.GetLayerDefn();
        foreach (var offset in new[] { 0, 1, 10, 20 })
        {
            var x = 2600000 + offset;
            using var geom = Geometry.CreateFromWkt($"POLYGON (({x} 1200000,{x + 2} 1200000,{x + 2} 1200002,{x} 1200002,{x} 1200000))");
            using var feature = new Feature(schema);
            feature.SetField("ObjNummer", "1");
            feature.SetField("Name", (conflict && offset == 1) || offset == 20 ? "Beta" : "Alpha");
            feature.SetField("JB_KAT", offset == 20 ? "III" : "II");
            feature.SetGeometry(geom);
            layer.CreateFeature(feature).Should().Be(0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BnL.ExcelXYPointDataToEnvelopeLayers.Models;
using GdalToolsLib.Common;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using OSGeo.OGR;

namespace BnL.ExcelXYPointDataToEnvelopeLayers;

internal class Program
{
    private static void Main(string[] args)
    {
        var excelPath = Path.Combine(AppContext.BaseDirectory, "Excel", "Liste_regulierte_Woelfe_Koordinaten.xlsx");

        if (!File.Exists(excelPath))
        {
            Console.WriteLine($"Excel file not found at {excelPath}");
            return;
        }

        var wolves = ExcelWolfReader.Read(excelPath);
        Console.WriteLine($"Loaded {wolves.Count} wolf records.");


        var envelopes1000m = CreateEnvelopes(wolves, 1000);
        var envelopes2000m = CreateEnvelopes(wolves, 2000);
        var envelopes5000m = CreateEnvelopes(wolves, 5000);

        Console.WriteLine($"Created {envelopes1000m.Count} envelopes (1000 m), {envelopes2000m.Count} envelopes (2000 m), {envelopes5000m.Count} envelopes (5000 m).");

        var fgdbPath = Path.Combine(AppContext.BaseDirectory, "WolfRegulierung.gdb");
        CreateFgdbWithEnvelopes(fgdbPath, ("envelopes1000m", envelopes1000m), ("envelopes2000m", envelopes2000m), ("envelopes5000m", envelopes5000m));
        Console.WriteLine($"FileGDB written to {fgdbPath}");
    }

    private static List<WolfEnvelopeModel> CreateEnvelopes(List<WolfModel> wolves, int envelopeLengthInMeter)
    {
        var envelopes = new List<WolfEnvelopeModel>();

        if (envelopeLengthInMeter <= 0 || wolves.Count == 0)
        {
            return envelopes;
        }

        foreach (var wolf in wolves)
        {
            if (wolf.X is null || wolf.Y is null)
            {
                continue;
            }

            var newModel = new WolfEnvelopeModel
            {
                ObservationDate = wolf.ObservationDate?.ToString("yyyy-MM-dd"),
                IndividualId = wolf.IndividualId,
                CompartmentMain = wolf.CompartmentMain,
                Canton = wolf.Canton,
                X = wolf.X,
                Y = wolf.Y,
                EnvelopeLowerLeftX = wolf.X - (wolf.X % envelopeLengthInMeter),
                EnvelopeLowerLeftY = wolf.Y - (wolf.Y % envelopeLengthInMeter),
                EnvelopeUpperRightX = wolf.X - (wolf.X % envelopeLengthInMeter) + envelopeLengthInMeter,
                EnvelopeUpperRightY = wolf.Y - (wolf.Y % envelopeLengthInMeter) + envelopeLengthInMeter
            };

            if (EnvelopeExists(envelopes, newModel, out var existingModel))
            {
                existingModel!.IndividualId = AppendValue(existingModel.IndividualId, newModel.IndividualId);
                existingModel.Canton = AppendValue(existingModel.Canton, newModel.Canton);
                existingModel.CompartmentMain = AppendValue(existingModel.CompartmentMain, newModel.CompartmentMain);
                existingModel.ObservationDate = AppendValue(existingModel.ObservationDate, newModel.ObservationDate);
                existingModel.IndividuumCount += 1;
            }
            else
            {
                envelopes.Add(newModel);
            }


        }

        return envelopes;
    }

    private static string? AppendValue(string? existing, string? addition)
    {
        if (string.IsNullOrWhiteSpace(existing)) return addition;
        if (string.IsNullOrWhiteSpace(addition)) return existing;
        return $"{existing}, {addition}";
    }

    private static bool EnvelopeExists(List<WolfEnvelopeModel> envelopes, WolfEnvelopeModel newModel, out WolfEnvelopeModel? existingModel)
    {
        const double toleranceMeters = 1.0;

        foreach (var envelope in envelopes)
        {
            if (IsWithinTolerance(envelope.EnvelopeLowerLeftX, newModel.EnvelopeLowerLeftX, toleranceMeters) &&
                IsWithinTolerance(envelope.EnvelopeLowerLeftY, newModel.EnvelopeLowerLeftY, toleranceMeters) &&
                IsWithinTolerance(envelope.EnvelopeUpperRightX, newModel.EnvelopeUpperRightX, toleranceMeters) &&
                IsWithinTolerance(envelope.EnvelopeUpperRightY, newModel.EnvelopeUpperRightY, toleranceMeters))
            {
                existingModel = envelope;
                return true;
            }
        }

        existingModel = null;
        return false;
    }

    private static bool IsWithinTolerance(double? left, double? right, double toleranceMeters)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return Math.Abs(left.Value - right.Value) <= toleranceMeters;
    }

    private static void CreateFgdbWithEnvelopes(string fgdbPath, params (string LayerName, List<WolfEnvelopeModel> Envelopes)[] envelopeLayers)
    {
        var dataSourceAccessor = new OgctDataSourceAccessor();

        using var dataSource = dataSourceAccessor.CreateAndOpenDatasource(fgdbPath, ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon);

        foreach (var (layerName, envelopes) in envelopeLayers)
        {
            WriteEnvelopeLayer(dataSource, layerName, envelopes);
        }
    }

    private static void WriteEnvelopeLayer(OgctDataSource dataSource, string layerName, List<WolfEnvelopeModel> envelopes)
    {
        using var layer = dataSource.CreateAndOpenLayer(layerName, ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon, CreateEnvelopeFields(), overwriteExisting: true);

        var schema = layer.LayerDetails.Schema;
        if (schema is null)
        {
            throw new InvalidOperationException($"Schema could not be created for layer {layerName}.");
        }

        var observationDateField = schema?.GetField("ObservationDate");
        var individualIdField = schema?.GetField("IndividualId");
        var individuumCountField = schema?.GetField("IndividuumCount");
        var compartmentField = schema?.GetField("CompartmentMain");
        var cantonField = schema?.GetField("Canton");
        var xField = schema?.GetField("X");
        var yField = schema?.GetField("Y");
        var lowerLeftXField = schema?.GetField("EnvelopeLowerLeftX");
        var lowerLeftYField = schema?.GetField("EnvelopeLowerLeftY");
        var upperRightXField = schema?.GetField("EnvelopeUpperRightX");
        var upperRightYField = schema?.GetField("EnvelopeUpperRightY");

        if (observationDateField is null || individualIdField is null || individuumCountField is null || compartmentField is null ||
            cantonField is null || xField is null || yField is null || lowerLeftXField is null ||
            lowerLeftYField is null || upperRightXField is null || upperRightYField is null)
        {
            throw new InvalidOperationException($"One or more required fields are missing on layer {layerName}.");
        }

        long fid = 0;

        foreach (var envelope in envelopes)
        {
            if (envelope.EnvelopeLowerLeftX is null || envelope.EnvelopeLowerLeftY is null ||
                envelope.EnvelopeUpperRightX is null || envelope.EnvelopeUpperRightY is null)
            {
                continue;
            }

            using var feature = layer.CreateAndOpenFeature(++fid);
            using var geometry = CreateEnvelopeGeometry(envelope);

            feature.SetGeometry(geometry);

#pragma warning disable CS8604 // WriteValue accepts nulls for nullable fields
            feature.WriteValue(observationDateField!, envelope.ObservationDate);
            feature.WriteValue(individualIdField!, envelope.IndividualId);
            feature.WriteValue(individuumCountField!, envelope.IndividuumCount);
            feature.WriteValue(compartmentField!, (object?)envelope.CompartmentMain);
            feature.WriteValue(cantonField!, (object?)envelope.Canton);
            feature.WriteValue(xField!, envelope.X!.Value);
            feature.WriteValue(yField!, envelope.Y!.Value);
            feature.WriteValue(lowerLeftXField!, envelope.EnvelopeLowerLeftX!.Value);
            feature.WriteValue(lowerLeftYField!, envelope.EnvelopeLowerLeftY!.Value);
            feature.WriteValue(upperRightXField!, envelope.EnvelopeUpperRightX!.Value);
            feature.WriteValue(upperRightYField!, envelope.EnvelopeUpperRightY!.Value);
#pragma warning restore CS8604

            layer.AddFeature(feature);
        }
    }

    private static List<FieldDefnInfo> CreateEnvelopeFields()
    {
        return new List<FieldDefnInfo>
        {
            new FieldDefnInfo("ObservationDate", FieldType.OFTString, 20, isNullable: true, isUnique: false),
            new FieldDefnInfo("IndividualId", FieldType.OFTString, 50, isNullable: true, isUnique: false),
            new FieldDefnInfo("IndividuumCount", FieldType.OFTInteger, 50, isNullable: false, isUnique: false),
            new FieldDefnInfo("CompartmentMain", FieldType.OFTString, 50, isNullable: true, isUnique: false),
            new FieldDefnInfo("Canton", FieldType.OFTString, 10, isNullable: true, isUnique: false),
            new FieldDefnInfo("X", FieldType.OFTReal, 24, isNullable: true, isUnique: false),
            new FieldDefnInfo("Y", FieldType.OFTReal, 24, isNullable: true, isUnique: false),
            new FieldDefnInfo("EnvelopeLowerLeftX", FieldType.OFTReal, 24, isNullable: true, isUnique: false),
            new FieldDefnInfo("EnvelopeLowerLeftY", FieldType.OFTReal, 24, isNullable: true, isUnique: false),
            new FieldDefnInfo("EnvelopeUpperRightX", FieldType.OFTReal, 24, isNullable: true, isUnique: false),
            new FieldDefnInfo("EnvelopeUpperRightY", FieldType.OFTReal, 24, isNullable: true, isUnique: false)
        };
    }

    private static OgctGeometry CreateEnvelopeGeometry(WolfEnvelopeModel envelope)
    {
        var ring = new Geometry(wkbGeometryType.wkbLinearRing);

        ring.AddPoint_2D(envelope.EnvelopeLowerLeftX!.Value, envelope.EnvelopeLowerLeftY!.Value);
        ring.AddPoint_2D(envelope.EnvelopeUpperRightX!.Value, envelope.EnvelopeLowerLeftY!.Value);
        ring.AddPoint_2D(envelope.EnvelopeUpperRightX!.Value, envelope.EnvelopeUpperRightY!.Value);
        ring.AddPoint_2D(envelope.EnvelopeLowerLeftX!.Value, envelope.EnvelopeUpperRightY!.Value);
        ring.AddPoint_2D(envelope.EnvelopeLowerLeftX!.Value, envelope.EnvelopeLowerLeftY!.Value);

        var polygon = new Geometry(wkbGeometryType.wkbPolygon);
        polygon.AddGeometry(ring);
        ring.Dispose();

        return new OgctGeometry(polygon);
    }
}

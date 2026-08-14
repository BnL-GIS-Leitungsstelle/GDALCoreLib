using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BnL.FGDBAddToMasterGUI.Models;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using OSGeo.OGR;

namespace BnL.FGDBAddToMasterGUI.Services;

public sealed class GeodatabaseTransferService : IGeodatabaseTransferService
{
    private static readonly HashSet<FieldType> SupportedFieldTypes =
    [
        FieldType.OFTInteger,
        FieldType.OFTInteger64,
        FieldType.OFTReal,
        FieldType.OFTString,
        FieldType.OFTDate,
        FieldType.OFTDateTime
    ];

    private readonly IOgctSourceAccessor _sourceAccessor = new OgctDataSourceAccessor();

    public IReadOnlyList<string> GetLayerNames(string databasePath)
    {
        ValidateGeodatabasePath(databasePath);
        using var dataSource = _sourceAccessor.OpenOrCreateDatasource(databasePath, EAccessLevel.ReadOnly);
        return dataSource.GetLayerNames().Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name!).OrderBy(name => name).ToList();
    }

    public IReadOnlyList<FieldDescriptor> GetFields(string databasePath, string layerName)
    {
        ValidateGeodatabasePath(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        using var dataSource = _sourceAccessor.OpenOrCreateDatasource(databasePath, EAccessLevel.ReadOnly);
        using var layer = dataSource.OpenLayer(layerName);
        var fields = layer.LayerDetails.Schema?.FieldList
            ?? throw new InvalidOperationException($"Das Schema des Layers „{layerName}“ konnte nicht gelesen werden.");
        return fields.Select(FieldDescriptor.From).OrderBy(field => field.Name).ToList();
    }

    public string CreateJoinCondition(IEnumerable<string> fieldNames)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        return string.Join(
            " AND ",
            fieldNames.Select(fieldName => $"MASTER.[{EscapeIdentifier(fieldName)}] = ADD.[{EscapeIdentifier(fieldName)}]"));
    }

    public TransferResult Transfer(TransferRequest request)
    {
        ValidateRequest(request);

        using var addDataSource = _sourceAccessor.OpenOrCreateDatasource(request.AddDatabasePath, EAccessLevel.ReadOnly);
        using var addLayer = addDataSource.OpenLayer(request.AddLayerName);
        using var masterDataSource = _sourceAccessor.OpenOrCreateDatasource(request.MasterDatabasePath, EAccessLevel.Full);
        using var masterLayer = masterDataSource.OpenLayer(request.MasterLayerName);

        var addFields = GetFieldsByName(addLayer.LayerDetails.Schema?.FieldList
            ?? throw new InvalidOperationException("Das Schema des AddToMASTER-Layers konnte nicht gelesen werden."));
        var masterFields = GetFieldsByName(masterLayer.LayerDetails.Schema?.FieldList
            ?? throw new InvalidOperationException("Das Schema des MASTER-Layers konnte nicht gelesen werden."));
        var fieldsToAdd = request.FieldNamesToAdd.Select(fieldName => RequireField(addFields, fieldName, "AddToMASTER")).ToList();
        var addJoinFields = request.JoinFieldNames.Select(fieldName => RequireField(addFields, fieldName, "AddToMASTER")).ToList();
        var masterJoinFields = request.JoinFieldNames.Select(fieldName => RequireField(masterFields, fieldName, "MASTER")).ToList();

        ValidateFieldTypes(fieldsToAdd, "Felder zum Hinzufügen");
        ValidateFieldTypes(addJoinFields, "Join-Felder");
        ValidateJoinTypes(addJoinFields, masterJoinFields);
        ValidateFieldCollisions(fieldsToAdd, masterFields);

        var addValueReadResult = ReadAddValues(addLayer, addJoinFields, fieldsToAdd);
        if (!addValueReadResult.IsSuccess)
        {
            return TransferResult.Failed(addValueReadResult.ErrorMessage!);
        }

        var addValuesByKey = addValueReadResult.ValuesByKey;

        var destinationFields = CreateDestinationFields(masterLayer, fieldsToAdd);
        var updatedCount = 0;
        var unmatchedMasterCount = 0;
        var masterNullKeyCount = 0;
        var matchedAddKeys = new HashSet<string>(StringComparer.Ordinal);

        OgctFeature? masterFeature;
        while ((masterFeature = masterLayer.OpenNextFeature()) is not null)
        {
            using (masterFeature)
            {
                var key = CreateCompositeKey(masterFeature, masterJoinFields);
                if (key is null)
                {
                    masterNullKeyCount++;
                    continue;
                }

                if (!addValuesByKey.TryGetValue(key, out var values))
                {
                    unmatchedMasterCount++;
                    continue;
                }

                for (var index = 0; index < destinationFields.Count - 1; index++)
                {
                    masterFeature.SetValue(destinationFields[index], values[index]!);
                }

                var writeResult = masterFeature.WriteValue(destinationFields[^1], values[^1]!);
                if (!writeResult.Valid)
                {
                    throw new InvalidOperationException($"Wert für MASTER-Feature FID {masterFeature.FID} konnte nicht geschrieben werden: {writeResult.ResultValidationType}.");
                }

                matchedAddKeys.Add(key);
                updatedCount++;
            }
        }

        masterDataSource.FlushCache();

        return new TransferResult(
            destinationFields.Count,
            updatedCount,
            unmatchedMasterCount,
            addValuesByKey.Count - matchedAddKeys.Count,
            addValueReadResult.NullKeyCount + masterNullKeyCount);
    }

    private static Dictionary<string, FieldDefnInfo> GetFieldsByName(IEnumerable<FieldDefnInfo> fields)
    {
        return fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static FieldDefnInfo RequireField(IReadOnlyDictionary<string, FieldDefnInfo> fields, string fieldName, string layerRole)
    {
        return fields.TryGetValue(fieldName, out var field)
            ? field
            : throw new InvalidOperationException($"Das Feld „{fieldName}“ ist nicht im {layerRole}-Layer vorhanden.");
    }

    private static void ValidateRequest(TransferRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateGeodatabasePath(request.MasterDatabasePath);
        ValidateGeodatabasePath(request.AddDatabasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MasterLayerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AddLayerName);

        if (request.FieldNamesToAdd.Count == 0)
        {
            throw new InvalidOperationException("Mindestens ein Feld zum Hinzufügen auswählen.");
        }

        if (request.JoinFieldNames.Count == 0)
        {
            throw new InvalidOperationException("Mindestens ein Join-Feld auswählen.");
        }
    }

    private static void ValidateGeodatabasePath(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath) || !Directory.Exists(databasePath) || !databasePath.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bitte einen vorhandenen Ordner mit der Endung .gdb auswählen.");
        }
    }

    private static void ValidateFieldTypes(IEnumerable<FieldDefnInfo> fields, string fieldGroup)
    {
        var unsupportedNames = fields.Where(field => !SupportedFieldTypes.Contains(field.Type)).Select(field => field.Name).ToList();
        if (unsupportedNames.Count > 0)
        {
            throw new InvalidOperationException($"{fieldGroup} enthalten nicht unterstützte Feldtypen: {string.Join(", ", unsupportedNames)}.");
        }
    }

    private static void ValidateJoinTypes(IReadOnlyList<FieldDefnInfo> addJoinFields, IReadOnlyList<FieldDefnInfo> masterJoinFields)
    {
        for (var index = 0; index < addJoinFields.Count; index++)
        {
            if (addJoinFields[index].Type != masterJoinFields[index].Type)
            {
                throw new InvalidOperationException($"Das Join-Feld „{addJoinFields[index].Name}“ hat unterschiedliche Datentypen im MASTER- und AddToMASTER-Layer.");
            }
        }
    }

    private static void ValidateFieldCollisions(IEnumerable<FieldDefnInfo> fieldsToAdd, IReadOnlyDictionary<string, FieldDefnInfo> masterFields)
    {
        var collisions = fieldsToAdd.Where(field => masterFields.ContainsKey(field.Name)).Select(field => field.Name).ToList();
        if (collisions.Count > 0)
        {
            throw new InvalidOperationException($"Diese Felder existieren bereits im MASTER-Layer: {string.Join(", ", collisions)}.");
        }
    }

    private static AddValueReadResult ReadAddValues(
        IOgctLayer addLayer,
        IReadOnlyList<FieldDefnInfo> joinFields,
        IReadOnlyList<FieldDefnInfo> valueFields)
    {
        var valuesByKey = new Dictionary<string, object?[]>(StringComparer.Ordinal);
        var nullKeyCount = 0;

        OgctFeature? addFeature;
        while ((addFeature = addLayer.OpenNextFeature()) is not null)
        {
            using (addFeature)
            {
                var key = CreateCompositeKey(addFeature, joinFields);
                if (key is null)
                {
                    nullKeyCount++;
                    continue;
                }

                if (!valuesByKey.TryAdd(key, valueFields.Select(addFeature.ReadValue).Cast<object?>().ToArray()))
                {
                    return AddValueReadResult.Failed(
                        $"Der AddToMASTER-Layer enthält mehrere Objekte mit demselben Join-Schlüssel ({key}).");
                }
            }
        }

        return AddValueReadResult.Success(valuesByKey, nullKeyCount);
    }

    private sealed record AddValueReadResult(
        Dictionary<string, object?[]> ValuesByKey,
        int NullKeyCount,
        string? ErrorMessage = null)
    {
        public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);

        public static AddValueReadResult Success(Dictionary<string, object?[]> valuesByKey, int nullKeyCount) =>
            new(valuesByKey, nullKeyCount);

        public static AddValueReadResult Failed(string errorMessage) =>
            new([], 0, errorMessage);
    }

    private static List<FieldDefnInfo> CreateDestinationFields(IOgctLayer masterLayer, IReadOnlyList<FieldDefnInfo> sourceFields)
    {
        var firstNewOgrIndex = masterLayer.LayerDetails.Schema?.FieldList.Count
            ?? throw new InvalidOperationException("Das Schema des MASTER-Layers konnte nicht gelesen werden.");
        var destinationFields = new List<FieldDefnInfo>(sourceFields.Count);

        for (var index = 0; index < sourceFields.Count; index++)
        {
            var destinationField = FieldDescriptor.From(sourceFields[index]).ToFieldDefinition(firstNewOgrIndex + index);
            if (masterLayer.CreateField(destinationField) != 0)
            {
                throw new InvalidOperationException($"Das Feld „{destinationField.Name}“ konnte im MASTER-Layer nicht angelegt werden.");
            }

            destinationFields.Add(destinationField);
        }

        return destinationFields;
    }

    private static string? CreateCompositeKey(IOgctFeature feature, IReadOnlyList<FieldDefnInfo> fields)
    {
        var values = new List<string>(fields.Count);
        foreach (var field in fields)
        {
            var value = feature.ReadValue(field);
            if (value is null)
            {
                return null;
            }

            values.Add(NormalizeKeyValue(value).Replace("\u001f", "\u001f\u001f", StringComparison.Ordinal));
        }

        return string.Join("\u001f", values);
    }

    private static string NormalizeKeyValue(object value)
    {
        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string EscapeIdentifier(string identifier) => identifier.Replace("]", "]]", StringComparison.Ordinal);
}

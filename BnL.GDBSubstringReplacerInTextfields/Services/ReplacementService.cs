using System;
using System.Collections.Generic;
using System.Linq;
using BnL.GDBSubstringReplacerInTextfields.Models;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;

namespace BnL.GDBSubstringReplacerInTextfields.Services
{
    internal sealed class ReplacementService
    {
        private readonly OgctDataSourceAccessor _accessor;
        private readonly string _oldSubstring;
        private readonly string _newSubstring;

        public ReplacementService(OgctDataSourceAccessor accessor, string oldSubstring, string newSubstring)
        {
            _accessor = accessor;
            _oldSubstring = oldSubstring;
            _newSubstring = newSubstring;
        }

        public void Execute(List<LayerCandidate> candidates)
        {
            var grouped = candidates.GroupBy(c => c.GeodatabasePath, StringComparer.OrdinalIgnoreCase);

            foreach (var group in grouped)
            {
                Console.WriteLine($"\nUpdating {group.Key}...");
                using var dataSource = _accessor.OpenOrCreateDatasource(group.Key, EAccessLevel.Full);

                foreach (var candidate in group)
                {
                    using var layer = dataSource.OpenLayer(candidate.LayerName);
                    var schema = layer.LayerDetails.Schema;

                    if (schema == null)
                    {
                        Console.Error.WriteLine($"  ! Schema missing for layer {candidate.LayerName}; skipping.");
                        continue;
                    }

                    var field = schema.FieldList.FirstOrDefault(f => string.Equals(f.Name, candidate.FieldName, StringComparison.OrdinalIgnoreCase));
                    if (field == null)
                    {
                        Console.Error.WriteLine($"  ! Field \"{candidate.FieldName}\" is not present in layer {candidate.LayerName}; skipping.");
                        continue;
                    }

                    layer.ResetReading();
                    var replacements = 0;
                    OgctFeature feature;

                    while ((feature = layer.OpenNextFeature()) != null)
                    {
                        using var currentFeature = feature;
                        var fieldValue = currentFeature.ReadValue(field);

                        if (fieldValue is not string text || string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        if (!ContainsTargetPatterns(text))
                        {
                            continue;
                        }

                        var updatedValue = ReplaceUrl(text);
                        if (string.Equals(updatedValue, text, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var writeResult = currentFeature.WriteValue(field, updatedValue);
                        if (!writeResult.Valid)
                        {
                            Console.Error.WriteLine($"  ! Validation warning for feature {writeResult.FeatureFid} in {candidate.LayerName}.{candidate.FieldName}: {writeResult.ResultValidationType}");
                        }
                        replacements++;
                    }

                    Console.WriteLine($"  -> {candidate.LayerName}.{candidate.FieldName}: replaced {replacements} occurrence(s).");
                }

                dataSource.FlushCache();
            }
        }

        private bool ContainsTargetPatterns(string value)
        {
            return value.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0
                && value.IndexOf(_oldSubstring, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string ReplaceUrl(string value)
        {
            return value.Replace(_oldSubstring, _newSubstring, StringComparison.OrdinalIgnoreCase);
        }
    }
}


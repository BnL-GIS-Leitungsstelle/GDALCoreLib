using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BnL.GDBSubstringReplacerInTextfields.Models;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using OSGeo.OGR;

namespace BnL.GDBSubstringReplacerInTextfields.Services
{
    internal sealed class GeodatabaseScanner
    {
        private readonly OgctDataSourceAccessor _accessor;
        private readonly string _oldSubstring;

        public GeodatabaseScanner(OgctDataSourceAccessor accessor, string oldSubstring)
        {
            _accessor = accessor;
            _oldSubstring = oldSubstring;
        }

        public List<LayerCandidate> Scan(string startDirectory)
        {
            var results = new List<LayerCandidate>();

            foreach (var fgdbPath in EnumerateGeodatabases(startDirectory))
            {
                Console.WriteLine($"Scanning {Path.GetFileName(fgdbPath)}...");
                try
                {
                    using var dataSource = _accessor.OpenOrCreateDatasource(fgdbPath, EAccessLevel.ReadOnly);

                    foreach (var layerInstance in dataSource.GetLayers())
                    {
                        using var layer = layerInstance;
                        var schema = layer.LayerDetails.Schema;
                        if (schema == null)
                        {
                            continue;
                        }

                        var textFields = schema.FieldList.Where(IsTextField).ToList();
                        if (textFields.Count == 0)
                        {
                            continue;
                        }

                        layer.ResetReading();
                        using var firstFeature = layer.OpenNextFeature();
                        if (firstFeature == null)
                        {
                            continue;
                        }

                        foreach (var textField in textFields)
                        {
                            var fieldValue = firstFeature.ReadValue(textField);
                            if (fieldValue is not string textValue || string.IsNullOrWhiteSpace(textValue))
                            {
                                continue;
                            }

                            if (!ContainsTargetPatterns(textValue))
                            {
                                continue;
                            }

                            Console.WriteLine($"  -- Add Layer {layer.Name.PadRight(60)} ({textField.Name})");
                            results.Add(new LayerCandidate(fgdbPath, layer.Name, textField.Name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ! Skipped {fgdbPath}: {ex.Message}");
                }
            }

            return results;
        }

        private static IEnumerable<string> EnumerateGeodatabases(string startDirectory)
        {
            return Directory.EnumerateDirectories(startDirectory, "*.gdb", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsTextField(FieldDefnInfo field)
        {
            return field.Type == FieldType.OFTString
                || field.Type == FieldType.OFTWideString
                || field.Type == FieldType.OFTStringList
                || field.Type == FieldType.OFTWideStringList;
        }

        private bool ContainsTargetPatterns(string value)
        {
            return value.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0
                && value.IndexOf(_oldSubstring, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}


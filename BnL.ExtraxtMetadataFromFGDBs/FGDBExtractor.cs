using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using GdalToolsLib.DataAccess;
using Newtonsoft.Json;
using GdalConfiguration = GdalToolsLib.GdalConfiguration;

namespace BnL.ExtractMetadataFromFGDBs;

public class FgdbExtractor
{
    public string StartPath { get; }


    /// <summary>
    /// Information about the tool
    /// </summary>
    public IEnumerable<string> About
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            var lines = new List<string>
            {
                $"{fvi.ProductName} by {fvi.CompanyName}",
                $"Version: {fvi.FileVersion} ({fvi.LegalCopyright})",
                "",
                $"TopLevelPath = {StartPath}",
                "",
                "About:",
                "=================================================================================================",
                "OGR/GDAL-based tool to extract metadata from all layers in all FGDBs found,",
                " and write this metadata into separate xml and json-files (same filename as the layers).",
                "=================================================================================================",
            ""
            };
            return lines;
        }
    }


    public FgdbExtractor(string topLevelPath)
    {
        StartPath = topLevelPath;
    }

    public void Run()
    {
        List<string?> fgdbPathes = Directory.GetDirectories(StartPath, "*.gdb", SearchOption.AllDirectories).ToList();

        Console.WriteLine($"Found {fgdbPathes.Count} Filegeodatabases.");



        foreach (var fgdbPath in fgdbPathes)
        {
            Console.WriteLine($"Extract FDGB = {fgdbPath}.");

            using var ds = new GeoDataSourceAccessor().OpenDatasource(fgdbPath);
            if (ds == null)
            {
                continue;
            }
            var layerNameList = ds.GetLayerNames();

            foreach (var layerName in layerNameList)
            {
                Console.WriteLine($"  -- Write XML and JSON for layer {layerName}.");
                // 1. open the layer
                string xmlMetadata = ds.ExecuteSqlFgdbGetLayerMetadata(layerName);
                string xmlLayerDefinition = ds.ExecuteSqlFgdbGetLayerDefinition(layerName);

                // To convert an XML node contained in string xml into a JSON string

                //WriteXmlFile(xmlMetadata, path, layerName, "_metadata");
                //WriteXmlFile(xmlLayerDefinition, path, layerName, "_layerDefinition");

                //WriteJsonFile(xmlMetadata, path, layerName, "_metadata");
                //WriteJsonFile(xmlLayerDefinition, path, layerName, "_layerDefinition");
            }
        }
    }


    private void WriteXmlFile(string xmlContent, string path, string layerName, string postfix = "")
    {
        if (String.IsNullOrWhiteSpace(xmlContent))
        {
            return;
        }
        File.WriteAllText($"{path}\\{layerName}{postfix}.xml", xmlContent);
    }

    private void WriteJsonFile(string xmlContent, string path, string layerName, string postfix = "")
    {
        if (String.IsNullOrWhiteSpace(xmlContent))
        {
            return;
        }
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlContent);
        string jsonContent = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);

        File.WriteAllText($"{path}\\{layerName}{postfix}.json", jsonContent);
    }

    public void ShowAbout()
    {
        foreach (var line in About) Console.WriteLine(line);
    }
}

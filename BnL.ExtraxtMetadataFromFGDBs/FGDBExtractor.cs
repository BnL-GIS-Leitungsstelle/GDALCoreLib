using Newtonsoft.Json;
using OGCToolsNetCoreLib.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace BnL.ExtraxtMetadataFromFGDBs;

public class FGDBExtractor
{
    public string StartPath { get; private set; }


    /// <summary>
    /// Information about the tool
    /// </summary>
    public IEnumerable<string> About
    {
        get
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var companyName = fvi.CompanyName;
            var productName = fvi.ProductName;
            var productVersion = fvi.ProductVersion;

            var lines = new List<string>
            {
                $"{productName} Version: {productVersion} ",
                $"Author: {companyName}",
                "",
                "=================================================================================================",
                "OGR/GDAL-based tool to extract metadata from FGDB-geodata layer as xml/json file.",
                "",
                "Writes metadata of all layers in a Geodatabase into separate xml/json-files (named as the layer).",
                "=================================================================================================",
            ""
            };
            return lines;
        }
    }


    /// <summary>
    /// Information on usage
    /// </summary>
    public IEnumerable<string> Usage
    {
        get
        {
            var lines = new List<string>
            {
                "",
                "---------------- USAGE ---------------------------------------------------------",
                "use ExtractMetadata From FGDBs with ONE required parameters: ",
                string.Format(" /topLevelPath= path to first FGDB"),

                string.Format(@" e.g. /topLevelPath=C:\Data"),
                "",
                "--------------------------------------------------------------------------------",
                ""
            };
            return lines;
        }
    }

    public FGDBExtractor(string[] args)
    {
        ShowAbout();

        ReadArgs(args);
    }

    public void Run()
    {
        List<string> fgdbPathes = Directory.GetDirectories(StartPath, "*.gdb", SearchOption.AllDirectories).ToList();

        Console.WriteLine($"Found {fgdbPathes.Count} Filegeodatabases.");

        foreach (var fgdbPath in fgdbPathes)
        {
            string path = Path.GetDirectoryName(fgdbPath);

            Console.WriteLine($"Extract FDGB = {fgdbPath}.");

            using var ds = new GeoDataSourceAccessor().OpenDatasource(fgdbPath);
            var layerNameList = ds.GetLayerNames();

            foreach (var layerName in layerNameList)
            {
                Console.WriteLine($"  -- Write XML and JSON for layer {layerName}.");
                // 1. open the layer
                string xmlMetadata = ds.ExecuteSqlFgdbGetLayerMetadata(layerName);
                string xmlLayerDefinition = ds.ExecuteSqlFgdbGetLayerDefinition(layerName);

                // To convert an XML node contained in string xml into a JSON string

                WriteXmlFile(xmlMetadata, path, layerName, "_metadata");
                //WriteXmlFile(xmlLayerDefinition, path, layerName, "_layerDefinition");

                WriteJsonFile(xmlMetadata, path, layerName, "_metadata");
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

    private void ShowAbout()
    {
        foreach (var line in About) Console.WriteLine(line);
    }

    private void ShowUsage()
    {
        foreach (var line in Usage) Console.WriteLine(line);
    }



    private void ReadArgs(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }

        foreach (string argument in args)
        {
            if (argument.StartsWith("/topLevelPath="))
            {
                StartPath = argument.Remove(0, 14).Replace(@"\\", @"\");
            }
        }
    }



}

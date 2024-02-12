using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GdalToolsLib.DataAccess;
using Microsoft.Extensions.Configuration;

namespace ReadLayerDataIntoExcel;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("launchSettings.json", optional: true, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        var FullPathFOfFgdb = configuration["profiles:ConsoleApp:commandLineArgs"];
        Console.WriteLine($"Environment: {FullPathFOfFgdb}");


        var excelContents = new List<ExcelContent>();

        Console.WriteLine("Read data of all layers in a Geodatabase into a collection of data-rows.");
        Console.WriteLine("and write the content into excel-files with the layers name.");


        using var ds = new GeoDataSourceAccessor().OpenDatasource(FullPathFOfFgdb);
        var layerNameList = ds.GetLayerNames();

        foreach (var layerName in layerNameList)
        {
            // 1. open the layer
            using var layer = ds.OpenLayer(layerName);
            var layerInfo = layer.LayerDetails;

            Console.WriteLine($"Read Layer {layerName}");

            Console.WriteLine($"Read Layer {layerInfo.Schema.Json}");

            // 2. read all data into collection of ecxel contents
            var rows = layer.ReadRows(layerInfo.Schema.FieldList);
            excelContents.Add(new ExcelContent(layerInfo, rows));

        }

        // 3. write content-collection into excel-file
        foreach (var content in excelContents)
        {
            await WriteToExcel(content);
        }

        Console.WriteLine();
        Console.WriteLine("Press ENTER to end..");
        Console.ReadLine();
    }


    public async static Task WriteToExcel(ExcelContent content)
    {
        await ExcelWriter.WriteToFileAsync(content);
    }
}
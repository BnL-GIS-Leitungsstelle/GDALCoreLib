using System;
using System.IO;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Models;

namespace CreateOptimizedGeopackage;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Create optimized Geopackage in c:\\temp\\");

        // get a subfolder with all definitions to the GPKG
        var gpkgDef = GeopackageDefinition.ReadFromDefinitionFile("GeopackageDefinition.txt");
        gpkgDef.ShowDefinition();
        gpkgDef.Path = @"C:\temp";

        using var ds =
            new OgctDataSourceAccessor().CreateAndOpenDatasource(Path.Combine(gpkgDef.Path, gpkgDef.Name + ".gpkg"), null);

        foreach (var layerDef in gpkgDef.LayersDetailsList)
        {
            using var layer = ds.CreateAndOpenLayer( layerDef.Name, layerDef.Projection.SpRef, layerDef.GeomType,layerDef.Schema.FieldList);
        }

        Console.WriteLine();
        Console.WriteLine($"====> {Path.Combine(gpkgDef.Path, gpkgDef.Name + ".gpkg")} Created");
        Console.WriteLine();
        Console.WriteLine("Press ENTER to end..");
        Console.ReadLine();
    }
}
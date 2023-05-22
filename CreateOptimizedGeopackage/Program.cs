using System;
using System.IO;
using OGCToolsNetCoreLib;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.DataAccess;
using OSGeo.OGR;
using OSGeo.OSR;

namespace CreateOptimizedGeopackage.Utils
{
    // See https://aka.ms/new-console-template for more information

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
                new GeoDataSourceAccessor().CreateDatasource(Path.Combine(gpkgDef.Path, gpkgDef.Name + ".gpkg"), null);

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
}



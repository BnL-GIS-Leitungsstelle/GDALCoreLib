using GdalToolsLib.Geometry;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using NetTopologySuite.Index.Strtree;
using OSGeo.OGR;
using PerfTest;

// using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(@"D:\Daten\MMO\Bedeutung_Parzellen_Amphibien.gdb");
// using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(@"D:\Daten\scratch\HabitatMap_v1_1_20241025.gdb");
// using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(@"C:\Users\Z70AMMO\Downloads\wrz.gdb");

// using var layer = ds.OpenLayer("Bedeutung_Parzellen_Amphibien_flaeche_20250429");
// await Helper.Test(ds, "HabitatMap_v1_1_20241025");
// await Helper.Test(ds, "Bedeutung_Parzellen_Amphibien_flaeche_20250429");
// await Helper.Test(@"C:\Users\Z70AMMO\Downloads\wrz.gdb", "n2021_stand_wildruhezone_20211128");

// var dsName = @"D:\Daten\MMO\Bedeutung_Parzellen_Amphibien.gdb";
// var layerName = "Bedeutung_Parzellen_Amphibien_flaeche_20250429";
var dsName = @"D:\Daten\scratch\HabitatMap_v1_1_20241025.gdb";
var layerName = "HabitatMap_v1_1_20241025";
// var dsName = @"D:\Daten\MMO\temp\planerischer_gewaesserschutz_v1_2_2056.gpkg";
// var layerName = "grundwasserschutzzonen";
// var dsName = @"C:\Users\Z70AMMO\Downloads\wrz.gdb";
// var layerName = "n2021_stand_wildruhezone_20211128";

Console.WriteLine("Starting...");
var watch = System.Diagnostics.Stopwatch.StartNew();
var res = await Helper.GetSelfOverlaps(dsName, layerName, true);
watch.Stop();
Console.WriteLine($"Concurrent: {watch.Elapsed.TotalMinutes}");
foreach (var keyValuePair in res)
{
    Console.WriteLine(keyValuePair);
}
watch = System.Diagnostics.Stopwatch.StartNew();
await Helper.GetSelfOverlaps(dsName, layerName, false);
watch.Stop();
Console.WriteLine($"Simple: {watch.Elapsed.TotalMinutes}");

Console.ReadKey();
//
// var ctsSource = new CancellationTokenSource();
// Console.CancelKeyPress += (_, args) =>
// {
//     args.Cancel = true;
//     ctsSource.Cancel();
// };
//
// await Task.Run(async () =>
// {
//     var res = await layer.ValidateSelfOverlapAsync((progress) => Console.WriteLine(progress), ctsSource.Token);
//
//     Console.WriteLine(res.Count);
// });
//

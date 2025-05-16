using PerfTest;
// var dsName = @"D:\Daten\MMO\Bedeutung_Parzellen_Amphibien.gdb";
// var layerName = "Bedeutung_Parzellen_Amphibien_flaeche_20250429";
// var dsName = @"D:\Daten\scratch\HabitatMap_v1_1_20241025.gdb";
// var layerName = "HabitatMap_v1_1_20241025";
// var dsName = @"D:\Daten\MMO\temp\planerischer_gewaesserschutz_v1_2_2056.gpkg";
// var layerName = "grundwasserschutzzonen";
var dsName = @"G:\BnL\Daten\Ablage\DNL\Schutzgebiete\Wildruhezonen\2024_Nov\20241119_wildruhezonen_delivery\data\lv95\gdb\Wildruhezonen.gdb";
var layerName = "wildruhezone";

Console.WriteLine("Starting...");
var watch = System.Diagnostics.Stopwatch.StartNew();
var res = await SelfOverlapChecker.GetSelfOverlaps(dsName, layerName, true);
watch.Stop();
Console.WriteLine($"Concurrent: {watch.Elapsed.TotalMinutes}");
foreach (var keyValuePair in res.OrderBy(x => x.Value))
{
    Console.WriteLine(keyValuePair);
}
watch = System.Diagnostics.Stopwatch.StartNew();
var resSimple = await SelfOverlapChecker.GetSelfOverlaps(dsName, layerName, false);
watch.Stop();
Console.WriteLine($"Simple: {watch.Elapsed.TotalMinutes}");
foreach (var keyValuePair in resSimple.OrderBy(x => x.Value))
{
    Console.WriteLine(keyValuePair);
}
Console.ReadKey();

// var ctsSource = new CancellationTokenSource();
// Console.CancelKeyPress += (_, args) =>
// {
//     args.Cancel = true;
//     ctsSource.Cancel();
// };
//
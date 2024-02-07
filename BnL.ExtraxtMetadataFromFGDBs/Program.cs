using System;
using BnL.ExtractMetadataFromFGDBs;
using Cocona;

// https://gdal.org/drivers/vector/openfilegdb.html

//  use ExtractMetadata From FGDBs with ONE required parameters: 
//  topLevelPath is the path to top most dir with FGDBs
//  e.g. C:\Data


CoconaApp.Run(([Argument(Description = "top level path to first FGDB, e.g. ")]string topLevelPath) =>
{
    var extractor = new FgdbExtractor(topLevelPath);
    Console.Write($"Running ");
    extractor.ShowAbout();

    extractor.Run();

    Console.WriteLine();
    Console.WriteLine("Press ENTER to end..");
    Console.ReadLine();

});



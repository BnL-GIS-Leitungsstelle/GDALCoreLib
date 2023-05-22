using System;

namespace BnL.ExtraxtMetadataFromFGDBs;

// https://gdal.org/drivers/vector/openfilegdb.html

public class Program
{
    public static void Main(string[] args)
    {
        var extractor = new FGDBExtractor(args);

        extractor.Run();
           
        Console.WriteLine();
        Console.WriteLine("Press ENTER to end..");
        Console.ReadLine();
    }
}
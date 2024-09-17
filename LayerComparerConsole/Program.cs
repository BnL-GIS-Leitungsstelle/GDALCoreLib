using System;
using System.IO;
using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace LayerComparerConsole;

/// <summary>
/// template using DI, Serilog, Settings 
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        // config logger first
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();


        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .CreateLogger();

       // Log.Logger.Information("Application starting");


        var builder = CoconaApp.CreateBuilder();

        builder.Host.UseSerilog();

        builder.Services.AddTransient<ILayerCompareService, LayerCompareService>();

        var app = builder.Build();

        // Compare a single master layer with a candidate layer
        app.AddCommand("single-layer-compare", (
            [Argument(Description = @"path to master GDB, e.g. G:\BnL\Daten\Ablage\DNL\Bundesinventare\Auengebiete\Auengebiete.gdb")]
            string masterGdbPath,
            [Argument(Description = "name of master layer to compare, e.g. N2017_Revision_Auengebiete_Anhang2_20171101")]
            string masterLayer,
            [Argument(Description = @"path to candidate GDB, e.g. G:\BnL\Daten\Ablage\DNL\Bundesinventare\Auengebiete\vonBIOPzurValidierung20210219\AU.gdb")]
            string candidateGdbPath,
            [Argument(Description = "name of candidate layer to compare, e.g. au_Anhang2_20171101")]
            string candidateLayer,
            ILayerCompareService layerCompareService) => // DI für ILayerCompareService
        {
            layerCompareService.ShowAbout();

            layerCompareService.Compare( masterGdbPath, masterLayer, candidateGdbPath, candidateLayer );

            Console.WriteLine("Vergleich durchgeführt.");

            Console.WriteLine();
            Console.WriteLine("Press ENTER to end..");
            Console.ReadLine();
        })
        .WithDescription("Compare a single master layer with a candidate layer.");

        // Compare a list of master layers with candidate layers
        app.AddCommand("multi-layer-compare", (
            [Argument(Description = @"path to csv containing list with path to master GDB, name of master layer, path to candidate GDB and name of candidate layer (Format: MasterGdb;MasterLayer;CandidateGdb;CandidateLayer).")]
            string masterCandidateCsv,
            ILayerCompareService layerCompareService) => // DI für ILayerCompareService
        {
            layerCompareService.ShowAbout();

            var records = CsvParser.ParseRecords<MultiLayerInputEntry>(masterCandidateCsv);
            foreach (var record in records) 
            {
                layerCompareService.Compare(record.MasterGdb, record.MasterLayer, record.CandidateGdb, record.CandidateLayer);   
            }

            Console.WriteLine("Vergleich durchgeführt.");

            Console.WriteLine();
            Console.WriteLine("Press ENTER to end..");
            Console.ReadLine();
        })
        .WithDescription("Compare a list of master layers with candidate layers");

        app.Run();
    }
}



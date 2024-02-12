using System;
using System.IO;
using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole;
using Serilog.Enrichers;
using Serilog.Sinks.File;
using Serilog.Settings.Configuration;

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

        Log.Logger.Information("Application starting");


        var builder = CoconaApp.CreateBuilder();

        builder.Host.UseSerilog();

        builder.Services.AddTransient<ILayerCompareService, LayerCompareService>();

        var app = builder.Build();

        // Definieren Sie einen Command, der ILayerCompareService verwendet
        app.AddCommand("compare", (
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

            Console.WriteLine($"Running.. ");
            // Verwenden Sie die Argumente und den Service, um die Logik auszuführen
            //layerCompareService.Compare(args);
            layerCompareService.Compare(new string[] { masterGdbPath, masterLayer, candidateGdbPath, candidateLayer });

            Console.WriteLine("Vergleich durchgeführt.");

            Console.WriteLine();
            Console.WriteLine("Press ENTER to end..");
            Console.ReadLine();


        });

        app.Run();
    }
}



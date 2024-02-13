using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OGCGeometryValidatorCore;

/// <summary>
/// template using DI, Serilog, Settings 
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder();
        BuildConfig(builder);

        // config logger first
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Build())
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        Log.Logger.Information("Application starting");

        // set up DI
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<IParallelLayerGeometryValidator, ParallelLayerGeometryValidator>();
                services.AddTransient<IGeometryValidatorService, GeometryValidatorService>();
            })
            .UseSerilog()
            .Build();

        // start the service in the host
        var svc = ActivatorUtilities.CreateInstance<GeometryValidatorService>(host.Services);
        await svc.Run(args);
    }

    /// <summary>
    /// Set up logging manually.
    /// CreateTest connection to appsettings manually with logging configuration
    /// </summary>
    /// <param name="builder"></param>
    static void BuildConfig(IConfigurationBuilder builder)
    {
        // get overwriteable settings, first appsetting, then - if available - .Development. or .Production.json,
        // that will overwrite settings from appsettings (like CSS)
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables(); // eg. Default-connection string to sql express in the local env variables
    }

}
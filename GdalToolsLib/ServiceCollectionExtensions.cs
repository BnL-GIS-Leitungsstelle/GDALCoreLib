using GdalToolsLib.Models;
using GdalToolsLib.Raster;
using Microsoft.Extensions.DependencyInjection;

namespace GdalToolsLib;

public static class ServiceCollectionExtensions
{
    public static void AddOgcTools(this IServiceCollection services)
    {
        services.AddSingleton<IOgctSourceAccessor, OgctDataSourceAccessor>();
        services.AddScoped<IRasterTools, RasterTools>();
    }
}
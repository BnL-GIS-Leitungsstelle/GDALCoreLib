using GdalToolsLib.DataAccess;
using GdalToolsLib.Raster;
using Microsoft.Extensions.DependencyInjection;

namespace GdalToolsLib;

public static class ServiceCollectionExtensions
{
    public static void AddOgcTools(this IServiceCollection services)
    {
        services.AddSingleton<IGeoDataSourceAccessor, GeoDataSourceAccessor>();
        services.AddScoped<IRasterTools, RasterTools>();
    }
}
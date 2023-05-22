using Microsoft.Extensions.DependencyInjection;
using OGCToolsNetCoreLib.DataAccess;
using OGCToolsNetCoreLib.Raster;

namespace OGCToolsNetCoreLib
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOgcTools(this IServiceCollection services)
        {
            services.AddSingleton<IGeoDataSourceAccessor, GeoDataSourceAccessor>();
            services.AddScoped<IRasterTools, RasterTools>();
        }
    }
}

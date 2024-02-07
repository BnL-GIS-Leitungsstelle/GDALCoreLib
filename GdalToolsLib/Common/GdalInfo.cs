
using OSGeo.GDAL;
using System.IO;
using System.Reflection;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Common
{
    public class GdalInfo
    {
        public string PackageVersion { get; } = Assembly.GetAssembly(typeof(Ogr))
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

        public string WorkingDirectory { get; } = Directory.GetCurrentDirectory();
        public string Version { get; } = Gdal.VersionInfo("RELEASE_NAME");
        public string Info { get; } = Gdal.VersionInfo("");


        public override string ToString()
        {
            return $"GDAL-Configuration: WorkingDir={WorkingDirectory}, Package={PackageVersion}, GDAL-Version/Info={Version}/{Info} ";
        }
    }
}

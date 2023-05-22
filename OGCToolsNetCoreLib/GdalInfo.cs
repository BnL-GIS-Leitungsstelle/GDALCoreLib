
using OSGeo.GDAL;
using System.IO;
using System.Reflection;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib
{
    public class GdalInfo
    {
        public string PackageVersion { get; }
        public string WorkingDirectory { get; }
        public string Version { get; }
        public string Info { get; }


        public GdalInfo()
        {
            WorkingDirectory = Directory.GetCurrentDirectory();

            PackageVersion = Assembly.GetAssembly(typeof(Ogr))
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            Version = Gdal.VersionInfo("RELEASE_NAME");
            Info = Gdal.VersionInfo("");
        }

        public override string ToString()
        {
            return $"GDAL-Configuration: WorkingDir={WorkingDirectory}, Package={PackageVersion}, GDAL-Version/Info={Version}/{Info} ";
        }
    }
}

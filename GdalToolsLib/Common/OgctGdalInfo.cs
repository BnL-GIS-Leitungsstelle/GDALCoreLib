using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GdalToolsLib.DataAccess;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace GdalToolsLib.Common;

public class OgctGdalInfo : IGdalInfo
{
    public string PackageVersion { get; } = Assembly.GetAssembly(typeof(Ogr))!
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion;

    public string WorkingDirectory { get; } = Directory.GetCurrentDirectory();
    public string Version { get; } = Gdal.VersionInfo("RELEASE_NAME");
    public string Info { get; } = Gdal.VersionInfo("");


    public List<string> ShowSupportedDatasources()
    {
        var info = new List<string>
        {
            " --- GDAL Tools Lib ---",
            $"Currently supported drivers:"
        };

        foreach (var source in SupportedDatasource.Datasources)
        {
            info.Add($"{source.OgrDriverName,15} Access: {source.Access} Type: {source.Type}");
        }

        return info;
    }


    #region drivers installed

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<string> GetAvailableDriverNames()
    {
        var driversImplemented = new List<string>();

        for (int i = 0; i < Gdal.GetDriverCount(); i++)
        {
            var driver = Gdal.GetDriver(i);
            driversImplemented.Add(driver.ShortName);
        }

        return driversImplemented;
    }
    
    #endregion


    public override string ToString()
    {
        return
            $"GDAL-Configuration: WorkingDir={WorkingDirectory}, Package={PackageVersion}, GDAL-Version/Info={Version}/{Info} ";
    }
}
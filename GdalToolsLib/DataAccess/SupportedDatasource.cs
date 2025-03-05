using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GdalToolsLib.DataAccess;

public class SupportedDatasource
{
    private static List<SupportedDatasource> _supportedDatasources;
    public static List<SupportedDatasource> Datasources
    {
        get
        {
            if (_supportedDatasources != null) return _supportedDatasources;

            _supportedDatasources =
            [
                new SupportedDatasource(EDataSourceType.GPKG, EAccessLevel.Full, ".gpkg", "gpkg", EFileType.File),
                new SupportedDatasource(EDataSourceType.SHP, EAccessLevel.Full, ".shp", "ESRI Shapefile", EFileType.MultiFile),
                new SupportedDatasource(EDataSourceType.SHP_FOLDER, EAccessLevel.Full, null, "ESRI Shapefile", EFileType.Folder),
                new SupportedDatasource(EDataSourceType.OpenFGDB, EAccessLevel.Full, ".gdb", "openfilegdb", EFileType.Folder),
                new SupportedDatasource(EDataSourceType.InMemory, EAccessLevel.Full, ".inMemory", "inmemory", EFileType.File),
            ];


            return _supportedDatasources;
        }
    }

    public EDataSourceType Type { get; }
    public EAccessLevel Access { get; }
    public string Extension { get; }
    public string OgrDriverName { get; }
    public EFileType FileType { get; }

    private SupportedDatasource(EDataSourceType type, EAccessLevel access, string extension, string driverName, EFileType filetype)
    {
        Type = type;
        Access = access;
        Extension = extension;
        OgrDriverName = driverName;
        FileType = filetype;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="datasourcePath"></param>
    /// <returns></returns>
    public static SupportedDatasource GetSupportedDatasource(string? datasourcePath)
    {
        string extension = Path.GetExtension(datasourcePath);
        foreach (var supportedDatasource in Datasources.Where(ds => ds.Extension == extension))
        {
            return supportedDatasource;
        }

        if (!File.Exists(datasourcePath)) //no File, let's assume a shape folder
        {
            return Datasources.FirstOrDefault(ds => ds.Type == EDataSourceType.SHP_FOLDER);
        }
        throw new Exception($"Unsupported datasource: not found by extension  {extension}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="datasourceType"></param>
    /// <returns></returns>
    public static SupportedDatasource GetSupportedDatasource(EDataSourceType datasourceType)
    {
        var ds = Datasources.FirstOrDefault(ds => ds.Type == datasourceType);

        return ds ?? throw new Exception($"Unsupported datasource: not found by type  {datasourceType}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="datasourcePath"></param>
    /// <returns></returns>
    public static bool IsSupportedDatasource(string datasourcePath)
    {
        string extension = Path.GetExtension(datasourcePath);
        bool result = Datasources.Any(ds => ds.Extension == extension);
        return result;

    }

    public static bool Exists(SupportedDatasource suppDs, string? path)
    {
        return suppDs.FileType switch
        {
            EFileType.File => File.Exists(path),
            EFileType.MultiFile => File.Exists(path),
            EFileType.Folder => Directory.Exists(path),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override string ToString()
    {
        return $"{Type} {OgrDriverName}";
    }
}
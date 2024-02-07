using System.Collections.Generic;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.Models;
using OSGeo.OGR;
using OSGeo.OSR;

namespace OGCToolsNetCoreLib.DataAccess;

public interface IGeoDataSourceAccessor
{
    List<string> GetGdalVersionInfo();

    /// <summary>
    /// Version 3.3.3 of MaxRef.Gdal.Core provides more drivers ans retired some old
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    List<string> GetAvailableDrivers();

    /// <summary>
    /// valid dataformats are Geopackage, Filegeodatabase (ReadOnly) and Shape TODO: explain folder vs file.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="writePermissions"></param>
    /// <param name="createIfNotExist"></param>
    /// <param name="spRef">only on creation of SHP-file</param>
    /// <param name="geometryType">only on creation of SHP-file</param>

    /// <returns></returns>
    OgctDataSource OpenDatasource(string path, bool writePermissions = false, bool createIfNotExist = false, ESpatialRefWKT spRef = ESpatialRefWKT.None, wkbGeometryType geometryType = wkbGeometryType.wkbNone);


    public OgctDataSource CreateAndOpenInMemoryDatasource();

    OgctDataSource CreateAndOpenDatasource(string path, ESpatialRefWKT spatialRef,
        wkbGeometryType geometryType = wkbGeometryType.wkbNone);

    /// <summary>
    /// valid dataformats are Geopackage, Filegeodatabase (ReadOnly) and Shape.
    /// </summary>
    /// <param name="pathAndFilename"></param>
    /// <param name="spatialRef">must be defined, when creating a shapefile</param>
    /// <param name="geometryType">must be defined, when creating a shapefile</param>
    /// <returns></returns>
    OgctDataSource CreateDatasource(string pathAndFilename, SpatialReference spatialRef, wkbGeometryType geometryType = wkbGeometryType.wkbNone);

    /// <summary>
    /// the methods uses standard .net methods to copy the datasource. No gdal-functionality is used.
    /// depending on the filetype of the datasource, the copy-process is adopted to the special needs,
    /// eg. shp consists of many files, that needs all to be copied.
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="outDir"></param>
    /// <param name="outputFilename"></param>
    /// <returns>targetFullpath</returns>
    string CopyDatasource(string inputFile, string outDir, string outputFilename);

    /// <summary>
    /// the methods uses standard .net methods to delete files and folders of a datasource format. No gdal-functionality is used.
    /// depending on the filetype of the datasource, the delete-process is adopted to the special needs,
    /// eg. shp consists of many files, that needs all to be deleted.
    /// </summary>
    /// <param name="path"></param>
    void DeleteDatasource(string path);

    int GetVectorLayerCount(string file);
    IEnumerable<object[]> GetSupportedVectorData(string directory, bool ignoreShpFolder);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    string GetProjString(string file);

    string GetGdalInfoRaster(string file);
}
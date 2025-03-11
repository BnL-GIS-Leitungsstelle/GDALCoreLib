using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Extensions;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GdalToolsLib.Models;

/// <summary>
/// Access to different file-formats of geodata:
/// vector-formats: FGDB and GPKG mainly. sometimes shp..
/// raster-formats: tif
/// 
/// for new GDAL-Version >3.7.x with FGDb-write-access
/// </summary>
public class OgctDataSourceAccessor : IOgctSourceAccessor
{
    /// <summary>
    /// This needs to be a static constructor, because then it will only get called once across the runtime of the whole program.
    /// This prevents problems with multithreading and avoids unnecessary re-execution
    /// </summary>
    static OgctDataSourceAccessor()
    {
        GdalBase.ConfigureAll();

        if (Ogr.GetDriverCount() == 0)
        {
            Ogr.RegisterAll();
        }
    }


    #region Open

    /// <summary>
    /// valid dataformats are Geopackage, Filegeodatabase and Shape
    /// </summary>
    /// <param name="path"></param>
    /// <param name="accessLevel"></param>
    /// <param name="createIfNotExist"></param>
    /// <param name="spRef">only on creation of SHP-file</param>
    /// <param name="geometryType">only on creation of SHP-file</param>

    /// <returns></returns>
    public OgctDataSource OpenOrCreateDatasource(string? path, EAccessLevel accessLevel = EAccessLevel.ReadOnly, bool createIfNotExist = false, ESpatialRefWkt spRef = ESpatialRefWkt.None, wkbGeometryType geometryType = wkbGeometryType.wkbNone)
    {
        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(path);

        OgctDataSource dataSource = null;

        bool dataSourceExists = SupportedDatasource.Exists(supportedDatasource, path);

        if (dataSourceExists == false)
        {
            if (!createIfNotExist) throw new Exception($"datasource does not exist, {path}");

            // create, if not exists
            if (supportedDatasource.Type == EDataSourceType.SHP)
            {
                if (geometryType != wkbGeometryType.wkbNone)
                {
                    return CreateAndOpenDatasource(path, new SpatialReference(spRef.GetEnumDescription(typeof(ESpatialRefWkt))),
                  geometryType);
                }

                throw new NotSupportedException("Cannot create shapefile from table without geometry");
            }

            return CreateAndOpenDatasource(path, null);
        }


        var ds = Ogr.Open(path, accessLevel == EAccessLevel.Full ? 1 : 0);

        if (ds == null)  // fgdb is empty or contains only raster-files
        {
            throw new NotSupportedException("Cannot open empty FGDB: its broken or contains only raster-featureclasses");
        }

        return new OgctDataSource(ds);
    }


    public OgctDataSource CreateAndOpenInMemoryDatasource()
    {
        using OSGeo.OGR.Driver inMemoryDriver = Ogr.GetDriverByName("Memory");

        return new OgctDataSource(inMemoryDriver.CreateDataSource(Guid.NewGuid() + ".inMemory", []));

    }


    /// <summary>
    /// valid dataformats are Geopackage, Filegeodatabase and Shape.
    /// Deletes an existing datasource!
    /// </summary>
    /// <param name="pathAndFilename"></param>
    /// <param name="spatialRefEnum">must be defined, when creating a shapefile</param>
    /// <param name="geometryType">must be defined, when creating a shapefile</param>
    /// <returns></returns>
    public OgctDataSource CreateAndOpenDatasource(string? pathAndFilename, ESpatialRefWkt spatialRefEnum,
        wkbGeometryType geometryType = wkbGeometryType.wkbNone)
    {
        return CreateAndOpenDatasource(pathAndFilename, new SpatialReference(spatialRefEnum.GetEnumDescription(typeof(ESpatialRefWkt))), geometryType);
    }

    /// <summary>
    /// valid dataformats are Geopackage, Filegeodatabase and Shape.
    /// Deletes an existing datasource!
    /// </summary>
    /// <param name="pathAndFilename"></param>
    /// <param name="spatialRef">must be defined, when creating a shapefile</param>
    /// <param name="geometryType">must be defined, when creating a shapefile</param>
    /// <returns></returns>
    public OgctDataSource CreateAndOpenDatasource(string? pathAndFilename, SpatialReference spatialRef,
        wkbGeometryType geometryType = wkbGeometryType.wkbNone)
    {
        var supportedDs = SupportedDatasource.GetSupportedDatasource(pathAndFilename);

        if (supportedDs.Access == EAccessLevel.ReadOnly)
        {
            throw new DataSourceReadOnlyException($"no create method implemented for this format: {supportedDs.Type}");
        }

        switch (supportedDs.FileType)
        {
            case EFileType.Folder when supportedDs.Type is EDataSourceType.SHP_FOLDER:

                var shpDriver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                var shpLayerName = Path.GetFileNameWithoutExtension(pathAndFilename);

                if (Directory.Exists(pathAndFilename)) Directory.Delete(pathAndFilename);  // delete, if exists

                var dsDataSource = new OgctDataSource(shpDriver.CreateDataSource(pathAndFilename, []));

                dsDataSource.OgrDataSource.CreateLayer(shpLayerName, spatialRef, geometryType, []).Dispose();

                return dsDataSource;


            case EFileType.Folder when supportedDs.Type is EDataSourceType.OpenFGDB:

                if (Directory.Exists(pathAndFilename)) Directory.Delete(pathAndFilename);  // delete, if exists

                var gdbDriver = Ogr.GetDriverByName(supportedDs.OgrDriverName);

                return new OgctDataSource(gdbDriver.CreateDataSource(pathAndFilename, new string[] { }));


            case EFileType.File:
                if (supportedDs.Type != EDataSourceType.GPKG)
                {
                    throw new DataSourceMethodNotImplementedException($"no create method implemented for this format: {supportedDs.Type}");
                }

                if (Directory.Exists(Path.GetDirectoryName(pathAndFilename)) == false)
                    Directory.CreateDirectory(Path.GetDirectoryName(pathAndFilename));

                if (File.Exists(pathAndFilename)) File.Delete(pathAndFilename); // deletes the gpkg, if exists

                var gpkgDriver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                return new OgctDataSource(gpkgDriver.CreateDataSource(pathAndFilename, []));


            case EFileType.MultiFile:
                if (supportedDs.Type != EDataSourceType.SHP)
                {
                    throw new DataSourceMethodNotImplementedException($"no create method implemented for this format: {supportedDs.Type}");
                }

                if (Directory.Exists(Path.GetDirectoryName(pathAndFilename)) == false)
                    Directory.CreateDirectory(Path.GetDirectoryName(pathAndFilename));

                DeleteDatasource(pathAndFilename); // delete all known shape-file sub-formats, if shp already exists

                var driver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                var dataSource = new OgctDataSource(driver.CreateDataSource(pathAndFilename, new string[] { }));
                var layerName = Path.GetFileNameWithoutExtension(pathAndFilename);

                using (var layer = dataSource.OgrDataSource.CreateLayer(layerName, spatialRef, geometryType, []))
                {

                }

                return dataSource;

            default:
                throw new DataSourceMethodNotImplementedException($"unhandled EFiletype ({supportedDs.FileType}) is used");
        }
    }


    /// <summary>
    /// the methods uses standard .net methods to copy the datasource. N= gdal-functionality is used.
    /// depending on the filetype of the datasource, the copy-process is adopted to the special needs,
    /// eg. shp consists of many files, that needs all to be copied.
    /// </summary>
    /// <param name="inputFile"></param>
    /// <param name="outDir"></param>
    /// <param name="outputFilename"></param>
    /// <returns>targetFullpath</returns>
    public string CopyDatasource(string? inputFile, string outDir, string outputFilename)
    {
        var dataSourceType = SupportedDatasource.GetSupportedDatasource(inputFile);

        string dir = Path.GetDirectoryName(inputFile);

        if (Directory.Exists(outDir) == false) // create outputDir, if not exists
            Directory.CreateDirectory(outDir);


        switch (dataSourceType.FileType)
        {
            case EFileType.Folder:
                var sourceDir = new DirectoryInfo(inputFile);
                var sourceName = Path.GetFileName(inputFile);
                string targetPath = Path.Combine(outDir, sourceName);

                if (Directory.Exists(targetPath) == false)
                {
                    Directory.CreateDirectory(targetPath);
                }

                // Get the files in the fgdb directory and copy them to the new location.
                FileInfo[] files = sourceDir.GetFiles();
                foreach (FileInfo file in files)
                {
                    file.CopyTo(Path.Combine(targetPath, file.Name), true);
                }

                return targetPath;

            case EFileType.File:

                var copiedFile = Path.Combine(outDir, Path.GetFileName(outputFilename));

                File.Copy(inputFile, copiedFile, true);

                return copiedFile;

            case EFileType.MultiFile:

                if (dataSourceType.Type != EDataSourceType.SHP)
                {
                    throw new DataSourceMethodNotImplementedException($"No copy method defined for type {dataSourceType.Type}");
                }

                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);
                string outputFilenameWithoutExtension = Path.GetFileNameWithoutExtension(outputFilename);

                foreach (var wellKnownShapeExtension in GlobalGisConstants.WellKnownShapeExtensions)
                {
                    var sourceFile = Path.Combine(dir, filenameWithoutExtension + wellKnownShapeExtension);
                    var targetFile = Path.Combine(outDir, outputFilenameWithoutExtension + wellKnownShapeExtension);

                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, targetFile, true);
                    }
                }

                return Path.Combine(outDir, filenameWithoutExtension + dataSourceType.Extension); ;

            default:
                throw new NotImplementedException("unknown EFiletype is used");
        }
    }

    /// <summary>
    /// the methods uses standard .net methods to delete files and folders of a datasource format. No gdal-functionality is used.
    /// depending on the filetype of the datasource, the delete-process is adopted to the special needs,
    /// eg. shp consists of many files, that needs all to be deleted.
    /// </summary>
    /// <param name="path"></param>
    public void DeleteDatasource(string? path)
    {
        var dataSourceType = SupportedDatasource.GetSupportedDatasource(path);

        switch (dataSourceType.FileType)
        {
            case EFileType.Folder:
                Directory.Delete(path, true);
                break;

            case EFileType.File:

                File.Delete(path);
                break;

            case EFileType.MultiFile:

                if (dataSourceType.Type != EDataSourceType.SHP)
                {
                    throw new DataSourceMethodNotImplementedException($"No delete method defined for type {dataSourceType.Type}");
                }

                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(path);

                foreach (var wellKnownShapeExtension in GlobalGisConstants.WellKnownShapeExtensions)
                {
                    var sourceFile = Path.Combine(Path.GetDirectoryName(path), filenameWithoutExtension + wellKnownShapeExtension);

                    if (File.Exists(sourceFile))
                    {
                        File.Delete(sourceFile);
                    }
                }
                break;

            default:
                throw new DataSourceMethodNotImplementedException("unknown EFiletype is used");
        }
    }



    public int GetLayerCount(string? file)
    {
        using var ds = OpenOrCreateDatasource(file);
        return ds.GetLayerCount();
    }

    #endregion

    /// <summary>
    /// sed to scan whole directories to find datasources that can be processed
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="ignoreShpFolder"></param>
    /// <returns></returns>
    public IEnumerable<object[]> GetPathNamesOfSupportedVectordataFormats(string directory, bool ignoreShpFolder = false)
    {
        var names = new List<object>();

        var supportedDatasources = SupportedDatasource.Datasources;

        if (ignoreShpFolder)
        {
            supportedDatasources = SupportedDatasource.Datasources.Where(x => x.Type != EDataSourceType.SHP_FOLDER).ToList();
        }

        foreach (var item in supportedDatasources)
        {
            names.AddRange(Directory.EnumerateFiles(directory, $"*{item.Extension}", SearchOption.AllDirectories).ToList());

            names.AddRange(Directory.EnumerateDirectories(directory, $"*{item.Extension}", SearchOption.AllDirectories).ToList());
        }
        return names.Select(x => new[] { x });
    }


    /// <summary>
    /// gets the projection of a datasource as string in a specific format used in Gdal
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public string GetProjString(string file)
    {
        using var dataset = Gdal.Open(file, Access.GA_ReadOnly);

        string wkt = dataset.GetProjection();

        using var spatialReference = new SpatialReference(wkt);

        spatialReference.ExportToProj4(out string projString);

        return projString;
    }

    /// <summary>
    /// return information on raster data of a datasource 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public string GetGdalInfoRaster(string file)
    {
        using var inputDataset = Gdal.Open(file, Access.GA_ReadOnly);

        return Gdal.GDALInfo(inputDataset, new GDALInfoOptions(null));
    }


}
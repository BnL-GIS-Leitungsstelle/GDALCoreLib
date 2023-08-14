using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTopologySuite.Utilities;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.Exceptions;
using OGCToolsNetCoreLib.Extensions;
using OGCToolsNetCoreLib.Helpers;
using OGCToolsNetCoreLib.Models;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace OGCToolsNetCoreLib.DataAccess
{
    /// <summary>
    /// Access to different file-formats of geodata:
    /// vector-formats: FGDB and GPKG mainly. sometimes shp..
    /// raster-formats: tif
    /// 
    /// for new GDAL-Version 3.6.4 with FGDb-write-access
    /// </summary>
    public class GeoDataSourceAccessor : IGeoDataSourceAccessor
    {
        public GeoDataSourceAccessor()
        {
            if (Ogr.GetDriverCount() == 0)
            {
                Ogr.RegisterAll();
            }
        }

        public List<string> GetGdalVersionInfo()
        {
            var gdalInfo = new GdalInfo();
            var info = new List<string>
            {
                "GDAL configured:",
                $"WorkDir= {gdalInfo.WorkingDirectory}",
                $"Package-Version= {gdalInfo.PackageVersion}",
                $"Gdal   -Version= {gdalInfo.Version}",
                $"Gdal   -Info= {gdalInfo.Version}",
                $"Currently supported drivers"
            };

            foreach (var source in SupportedDatasource.Datasources)
            {
                info.Add($"{source.OgrDriverName,15} Access: {source.Access} Type: {source.Type}");
            }

            return info;
        }


        #region drivers installed

        /// <summary>
        /// Version 3.3.3 of MaxRef.Gdal.Core provides more drivers ans retired some old
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<string> GetAvailableDrivers()
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


        #region Open

        /// <summary>
        /// valid dataformats are Geopackage, Filegeodatabase (ReadOnly) and Shape TODO: explain folder vs file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="writePermissions"></param>
        /// <param name="createIfNotExist"></param>
        /// <param name="spRef">only on creation of SHP-file</param>
        /// <param name="geometryType">only on creation of SHP-file</param>

        /// <returns></returns>
        public OgctDataSource OpenDatasource(string path, bool writePermissions = false, bool createIfNotExist = false, ESpatialRefWKT spRef = ESpatialRefWKT.None, wkbGeometryType geometryType = wkbGeometryType.wkbNone)
        {
            var supportedDatasource = SupportedDatasource.GetSupportedDatasource(path);
            OgctDataSource dataSource = null;
            if (SupportedDatasource.Exists(supportedDatasource, path) == false)
            {
                if (createIfNotExist == false) // open, but is missing
                {
                    throw new Exception($"datasource does not exist, {path}");
                }

                if (supportedDatasource.Type == EDataSourceType.SHP)
                {
                    dataSource = CreateDatasource(path, new SpatialReference(spRef.GetEnumDescription(typeof(ESpatialRefWKT))),
                        geometryType);
                }
                else
                {
                    dataSource = CreateDatasource(path, null);
                }
            }

            if (dataSource != null)
            {
                return dataSource;
            }
            var ds = Ogr.Open(path, writePermissions ? 1 : 0);
            if (ds == null) throw new Exception("Could not open Datasource: " + path);

            return new OgctDataSource(ds);
        }


        public OgctDataSource CreateAndOpenInMemoryDatasource()
        {
            using OSGeo.OGR.Driver inMemoryDriver = Ogr.GetDriverByName("Memory");

            return new OgctDataSource(inMemoryDriver.CreateDataSource(Guid.NewGuid() + ".inMemory", new string[0]));

        }


        public OgctDataSource CreateAndOpenDatasource(string path, ESpatialRefWKT spatialRef,
            wkbGeometryType geometryType = wkbGeometryType.wkbNone)
        {
            return CreateDatasource(path, new SpatialReference(spatialRef.GetEnumDescription(typeof(ESpatialRefWKT))), geometryType);
        }

        /// <summary>
        /// valid dataformats are Geopackage, Filegeodatabase and Shape.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="spatialRef">must be defined, when creating a shapefile</param>
        /// <param name="geometryType">must be defined, when creating a shapefile</param>
        /// <returns></returns>
        public OgctDataSource CreateDatasource(string path, SpatialReference spatialRef, wkbGeometryType geometryType = wkbGeometryType.wkbNone)
        {
            var supportedDs = SupportedDatasource.GetSupportedDatasource(path);

            if (supportedDs.Access == EAccessLevel.ReadOnly)
            {
                throw new DataSourceReadOnlyException($"no create method implemented for this format: {supportedDs.Type}");
            }

            // create new dir for files, i case they don't exist
            string outputPath = Path.GetDirectoryName(path);

            switch (supportedDs.FileType)
            {
                case EFileType.Folder when supportedDs.Type is EDataSourceType.SHP_FOLDER:
                    var shpDriver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                    var dsDataSource = new OgctDataSource(shpDriver.CreateDataSource(path, new string[] { }));
                    var shpLayerName = Path.GetFileNameWithoutExtension(path);
                    dsDataSource.OgrDataSource.CreateLayer(shpLayerName, spatialRef, geometryType, new string[] { }).Dispose();

                    return dsDataSource;

                case EFileType.Folder when supportedDs.Type is EDataSourceType.OpenFGDB:

                    if (Directory.Exists(path) == true)
                    {
                        Directory.Delete(path);
                    }

                    var gdbDriver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                    return new OgctDataSource(gdbDriver.CreateDataSource(path, new string[] { }));


                case EFileType.File:
                    if (supportedDs.Type != EDataSourceType.GPKG)
                    {
                        throw new DataSourceMethodNotImplementedException($"no create method implemented for this format: {supportedDs.Type}");
                    }

                    if (Directory.Exists(outputPath) == false)
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    if (File.Exists(path)) File.Delete(path);

                    var gpkgDriver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                    return new OgctDataSource(gpkgDriver.CreateDataSource(path, new string[] { }));


                case EFileType.MultiFile:
                    if (supportedDs.Type != EDataSourceType.SHP)
                    {
                        throw new DataSourceMethodNotImplementedException($"no create method implemented for this format: {supportedDs.Type}");
                    }

                    if (Directory.Exists(outputPath) == false)
                    {
                        Directory.CreateDirectory(outputPath);
                    }

                    DeleteDatasource(path); // delete, if it already exists


                    var driver = Ogr.GetDriverByName(supportedDs.OgrDriverName);
                    var dataSource = new OgctDataSource(driver.CreateDataSource(path, new string[] { }));
                    if (supportedDs.Type == EDataSourceType.SHP)
                    {
                        var layerName = Path.GetFileNameWithoutExtension(path);
                        dataSource.OgrDataSource.CreateLayer(layerName, spatialRef, geometryType, new string[] { }).Dispose();
                    }

                    //OSGeo.OGR.LayerName shapeFileLayer = dataSource.CreateLayer(path, spatialRef, geometryType, new string[] { });
                    //if (shapeFileLayer == null) throw new Exception("Could not create shapefile " + fileName + " in " + directory);

                    return dataSource;

                default:
                    throw new DataSourceMethodNotImplementedException("unknown EFiletype is used");
            }
        }


        /// <summary>
        /// the methods uses standard .net methods to copy the datasource. No gdal-functionality is used.
        /// depending on the filetype of the datasource, the copy-process is adopted to the special needs,
        /// eg. shp consists of many files, that needs all to be copied.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outDir"></param>
        /// <param name="outputFilename"></param>
        /// <returns>targetFullpath</returns>
        public string CopyDatasource(string inputFile, string outDir, string outputFilename)
        {
            var dataSourceType = SupportedDatasource.GetSupportedDatasource(inputFile);

            string dir = Path.GetDirectoryName(inputFile);

            // create new dir for outputDir, i case it does not exist
            if (Directory.Exists(outDir) == false)
            {
                Directory.CreateDirectory(outDir);
            }

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
        public void DeleteDatasource(string path)
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



        public int GetVectorLayerCount(string file)
        {
            using (var ds = OpenDatasource(file))
            {
                return ds.GetLayerCount();
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="ignoreShpFolder"></param>
        /// <returns></returns>
        public IEnumerable<object[]> GetSupportedVectorData(string directory, bool ignoreShpFolder = false)
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
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string GetProjString(string file)
        {
            using var dataset = Gdal.Open(file, Access.GA_ReadOnly);

            string wkt = dataset.GetProjection();

            using var spatialReference = new OSGeo.OSR.SpatialReference(wkt);

            spatialReference.ExportToProj4(out string projString);

            return projString;
        }

       
        public string GetGdalInfoRaster(string file)
        {
            using var inputDataset = Gdal.Open(file, Access.GA_ReadOnly);

            return Gdal.GDALInfo(inputDataset, new GDALInfoOptions(null));
        }


    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.DataAccess;
using OGCToolsNetCoreLib.Models;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Raster
{
    public class RasterTools : IRasterTools
    {
        private readonly IGeoDataSourceAccessor _geoDataSourceAccessor;

        public RasterTools(IGeoDataSourceAccessor geoDataSourceAccessor)
        {
            _geoDataSourceAccessor = geoDataSourceAccessor;
            if (Ogr.GetDriverCount() == 0)
            {
                Ogr.RegisterAll();
            }
        }

        public void ClipTiff(string inputTifPath, string outputTifPath, Rectangle boundingBox)
        {
            using var ds = Gdal.OpenShared(inputTifPath, Access.GA_ReadOnly);
            var options = new string[] { "-of", "GTiff", "-co", "compress=lzw", "-co", "predictor=2", "-co", "bigtiff=yes",
                "-projwin", $"{boundingBox.Left}", $"{boundingBox.Bottom}", $"{boundingBox.Right}", $"{boundingBox.Top}" };
            Gdal.wrapper_GDALTranslate(outputTifPath, ds, new GDALTranslateOptions(options), null, null);
        }

        /// <summary>
        /// NoDataValue is used if pixel should be empty/transparent.
        /// </summary>
        public void SetNoDataValueForAllBands(string tifPath, double noDataValue)
        {
            using var ds = Gdal.Open(tifPath, Access.GA_Update);

            for (int i = 1; i <= ds.RasterCount; i++)
            {
                using var band = ds.GetRasterBand(i);
                band.SetNoDataValue(noDataValue);
                band.FlushCache();
            }
        }

        public void FilterTiff(string outputPath, double minValue, double maxValue,
            Action<int> reportProgressPercentage = null, double filterTrueValue = 1, double filterFalseValue = 0,
            int bandNumber = 1)
        {
            using var ds = Gdal.Open(outputPath, Access.GA_Update);
            using var band = ds.GetRasterBand(bandNumber);

            int xRasterSize = ds.RasterXSize;
            int yRasterSize = ds.RasterYSize;
            band.SetNoDataValue(0);

            // check how many raster rows fit in an array
            int numberOfYToLoad = 1;
            if (xRasterSize > 0)
            {
                numberOfYToLoad = ArrayUtils.GetMaxArrayLengthForType(typeof(double)) / xRasterSize;

                if (numberOfYToLoad > yRasterSize)
                {
                    numberOfYToLoad = yRasterSize;
                }
            }


            // Go through Raster row by row
            double[] rasterData = new double[xRasterSize * numberOfYToLoad];
            for (int i = 0; i < yRasterSize; i += numberOfYToLoad)
            {
                if (i + numberOfYToLoad > yRasterSize)
                {
                    numberOfYToLoad = yRasterSize - i;
                }

                band.ReadRaster(0, i, xRasterSize, numberOfYToLoad, rasterData, xRasterSize, numberOfYToLoad, 0, 0);

                for (int j = 0; j < rasterData.Length; j++)
                {
                    if (rasterData[j] < minValue || rasterData[j] > maxValue)
                    {
                        rasterData[j] = filterFalseValue;
                    }
                    else
                    {
                        rasterData[j] = filterTrueValue;
                    }
                }

                band.WriteRaster(0, i, xRasterSize, numberOfYToLoad, rasterData, xRasterSize, numberOfYToLoad, 0, 0);
                Array.Clear(rasterData, 0, rasterData.Length);

                reportProgressPercentage?.Invoke((int)(100 * (double)i / yRasterSize));

            }
            reportProgressPercentage?.Invoke(100);

            band.FlushCache();
        }

        /// <summary>
        /// Sets the same value to all the pixels in the raster that are set (!= noDataValue).
        /// </summary>
        public void SetSingleValueForRaster(string tifPath, double value, int bandNumber = 1)
        {
            using var ds = Gdal.Open(tifPath, Access.GA_Update);
            using var band = ds.GetRasterBand(bandNumber);

            int xRasterSize = ds.RasterXSize;
            int yRasterSize = ds.RasterYSize;
            band.GetNoDataValue(out double noDataValue, out int hasValue);

            // Go through Raster row by row
            for (int i = 0; i < yRasterSize; i++)
            {
                double[] rowData = new double[xRasterSize];
                band.ReadRaster(0, i, xRasterSize, 1, rowData, xRasterSize, 1, 0, 0);

                for (int j = 0; j < rowData.Length; j++)
                {
                    if (rowData[j] != noDataValue)
                    {
                        rowData[j] = value;
                    }
                }

                band.WriteRaster(0, i, xRasterSize, 1, rowData, xRasterSize, 1, 0, 0);
            }

            band.FlushCache();
        }

        /// <summary>
        /// Creates Polygons from Raster. For pixels that should not be polygonized use 0!
        /// </summary>
        public void PolygonizeToLayer(string tifPath, IOgctDataSource targetDataSource, string newLayerName,
            int bandNumber = 1, ESpatialRefWKT spatialRef = ESpatialRefWKT.CH1903plus_LV95)
        {
            using var rasterDs = Gdal.OpenShared(tifPath, Access.GA_ReadOnly);
            using var band = rasterDs.GetRasterBand(bandNumber);

            string tempLayerName = "temp";
            using var inMemoryDs = _geoDataSourceAccessor.CreateAndOpenInMemoryDatasource();
            using var inMemoryLayer =
                inMemoryDs.CreateAndOpenLayer(tempLayerName, spatialRef, wkbGeometryType.wkbMultiPolygon);

            var ogrLayer = ((OgctLayer)inMemoryLayer).OgrLayer;

            Gdal.Polygonize(band, band, ogrLayer, -1, new string[0], null, null);

            inMemoryLayer.CopyToLayer(targetDataSource, newLayerName, true);
        }

        public void PolygonizeAndDissolveToLayer(string tifPath, IOgctDataSource targetDataSource, string newLayerName,
            int bandNumber = 1, ESpatialRefWKT spatialRef = ESpatialRefWKT.CH1903plus_LV95)
        {
            using var rasterDs = Gdal.OpenShared(tifPath, Access.GA_ReadOnly);
            using var band = rasterDs.GetRasterBand(bandNumber);

            string tempLayerName = "temp";
            using var inMemoryDs = _geoDataSourceAccessor.CreateAndOpenInMemoryDatasource();
            using var inMemoryLayer =
                inMemoryDs.CreateAndOpenLayer(tempLayerName, spatialRef, wkbGeometryType.wkbMultiPolygon);

            var ogrLayer = ((OgctLayer)inMemoryLayer).OgrLayer;

            Gdal.Polygonize(band, band, ogrLayer, -1, new string[0], null, null);

            using var inMemoryUnifiedLayer = inMemoryDs.ExecuteSQL($"SELECT ST_Union(geometry) AS geometry FROM {tempLayerName}");
            inMemoryUnifiedLayer.CopyToLayer(targetDataSource, newLayerName, true);

        }

        /// <summary>
        /// Splits Raster to multiple smaller files.
        /// Tiled rasters are written to the outputDirectory as tifs (Filename-Format: tile_X-<x-offset>_Y-<y-offset>).
        /// </summary>
        /// <returns>A list with paths to the created tiled rasters.</returns>
        public IEnumerable<string> Tile(string tifPath, string outputDirectory, int tileSize,
            Action<int> reportProgressPercentage = null)
        {
            using var rasterDs = Gdal.OpenShared(tifPath, Access.GA_ReadOnly);
            List<(int, int)> xyCoordinates = new List<(int, int)>();

            for (int i = 0; i < rasterDs.RasterXSize; i += tileSize)
            {
                for (int j = 0; j < rasterDs.RasterYSize; j += tileSize)
                {
                    xyCoordinates.Add((i, j));
                }
            }

            ConcurrentBag<string> paths = new ConcurrentBag<string>();

            var processedTiles = 0;
            var tileCount = xyCoordinates.Count;

            Parallel.ForEach(xyCoordinates, new ParallelOptions { MaxDegreeOfParallelism = 200 }, (xy) =>
            {
                using var ds = Gdal.OpenShared(tifPath, Access.GA_ReadOnly); // create new file handle for each thread

                var tiledTifPath = Path.Combine(outputDirectory, $"tile_X{xy.Item1}_Y{xy.Item2}.tif");
                paths.Add(tiledTifPath);

                var options = new string[] { "-of", "GTiff", "-co", "compress=lzw", "-co", "predictor=2", "-co", "TILED=YES",
                    "-srcwin", $"{xy.Item1}", $"{xy.Item2}", $"{tileSize}", $"{tileSize}" };
                _ = Gdal.wrapper_GDALTranslate(tiledTifPath, ds, new GDALTranslateOptions(options), null, null);
                Interlocked.Increment(ref processedTiles);
                reportProgressPercentage?.Invoke(100*processedTiles / tileCount);
            });

            return paths.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.Models;

namespace OGCToolsNetCoreLib.Raster;

public interface IRasterTools
{
    void ClipTiff(string inputTifPath, string outputTifPath, Rectangle boundingBox);

    /// <summary>
    /// NoDataValue is used if pixel should be empty/transparent.
    /// </summary>
    void SetNoDataValueForAllBands(string tifPath, double noDataValue);

    void FilterTiff(string outputPath, double minValue, double maxValue, Action<int> reportProgressPercentage,
        double filterTrueValue = 1, double filterFalseValue = 0, int bandNumber = 1);

    /// <summary>
    /// Sets the same value to all the pixels in the raster that are set (!= noDataValue).
    /// </summary>
    void SetSingleValueForRaster(string tifPath, double value, int bandNumber = 1);

    /// <summary>
    /// Creates Polygons from Raster. For pixels that should not be polygonized use 0!
    /// </summary>
    void PolygonizeToLayer(string tifPath, IOgctDataSource targetDataSource, string newLayerName,
        int bandNumber = 1, ESpatialRefWKT spatialRef = ESpatialRefWKT.CH1903plus_LV95);

    void PolygonizeAndDissolveToLayer(string tifPath, IOgctDataSource targetDataSource, string newLayerName,
        int bandNumber = 1, ESpatialRefWKT spatialRef = ESpatialRefWKT.CH1903plus_LV95);

    /// <summary>
    /// Splits Raster to multiple smaller files.
    /// Tiled rasters are written to the outputDirectory as tifs (Filename-Format: tile_X-<x-offset>_Y-<y-offset>).
    /// </summary>
    /// <returns>A list with paths to the created tiled rasters.</returns>
    IEnumerable<string> Tile(string tifPath, string outputDirectory, int tileSize,
        Action<int> reportProgressPercentage = null);
}
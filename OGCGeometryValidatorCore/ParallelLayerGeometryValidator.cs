using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Geometry;
using Microsoft.Extensions.Logging;

namespace OGCGeometryValidatorCore;

internal class ParallelLayerGeometryValidator : IParallelLayerGeometryValidator
{
    private readonly ILogger<ParallelLayerGeometryValidator> _log;

    private int _limit;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="log"></param>
    public ParallelLayerGeometryValidator(ILogger<ParallelLayerGeometryValidator> log)
    {
        _log = log;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="limit">minimal record count to switch to parallel processing.
    /// will be used as number of records per chunk file</param>
    /// <exception cref="NotImplementedException"></exception>
    public void GetLimit(int limit)
    {
        _limit = limit;
    }

    /// <summary>
    /// Assumption: It is more efficient, to switch to parallel processing, if the layer has above [limit] records.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="layerName"></param>
    /// <param name="limit">minimal record count to switch to parallel processing</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool SwitchToParallelProcessing(string file, string layerName)
    {
        _log.LogInformation(" --  start examination to switch to parallel processing for layer {layer}", layerName);
        using var ds = new GeoDataSourceAccessor().OpenDatasource(file);
        using var layer = ds.OpenLayer(layerName);
        return layer.LayerDetails.FeatureCount >= _limit;
    }


    /// <summary>
    /// Parallel process of layer validation will separate the large layer into separate chunk files (created in a temporary subdirectory).
    /// The list of chunk files will be processed in parallel.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="layerName"></param>
    /// <returns></returns>
    public async Task<ConcurrentBag<LayerValidationResult>> ValidateGeometry(string fileName, string layerName)
    {
        _log.LogWarning(" --  create temporary chunk files for layer {layer}", layerName);
        var timeToChunkLayer = Stopwatch.StartNew();

        var layerToProcess = ChunkLargeLayerIntoFilesByRecords(fileName, layerName);

        timeToChunkLayer.Stop();
        _log.LogWarning(" --  {number} chunk files for layer {layer} created in {elapsedTime}", layerToProcess.Count, layerName,
            Utils.ToNicelyTimeFormatString(timeToChunkLayer));

        var bag = new ConcurrentBag<LayerValidationResult>();
        Parallel.ForEach(layerToProcess,
            async item =>
            {
                using var ds = new GeoDataSourceAccessor().OpenDatasource(item.fileName);
                using var layer = ds.OpenLayer(item.layerName);
                bag.Add(await layer.ValidateGeometryAsync());
            });

        return bag;
    }




    /// <summary>
    /// divide file and write separate part-files in temp-folder to be processed asyncronously 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="layerName"></param>
    /// <param name="limit">number of records per chunk-file</param>
    /// <returns>the list of separate part-files</returns>
    /// <exception cref="NotImplementedException"></exception>
    private List<(string fileName, string layerName)> ChunkLargeLayerIntoFilesByRecords(string fileName, string layerName)
    {
        var chunkLayers = new List<(string fileName, string layerName)>();

        using var ds = new GeoDataSourceAccessor().OpenDatasource(fileName);
        using var layer = ds.OpenLayer(layerName);

        var layerInfo = layer.LayerDetails;

        long numberOfParts = layerInfo.FeatureCount / _limit;

        for (int i = 1; i <= numberOfParts; i++)
        {
            var outputFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{_limit}_rec_{i}.gpkg";

            var tempDirectory = Path.Combine(Path.GetDirectoryName(fileName), "tempChunkFileWorkDir");
            if (Directory.Exists(tempDirectory) == false) Directory.CreateDirectory(tempDirectory);


            string outputFile = Path.Combine(tempDirectory, outputFileName);


            int offset = _limit * (i - 1);

            if (File.Exists(outputFile) == false) // don't overwrite existing files
            {
                using var outputDatasource = new GeoDataSourceAccessor().OpenDatasource(outputFile, true, true);
                _log.LogInformation(" --  write chunk file {file} ({limit} records, starting at {offset}) in {folder}", outputFileName, _limit, offset, tempDirectory);
                layer.CopyToLayer(outputDatasource, layerInfo.Name,
                    _limit, offset);
            }

            chunkLayers.Add((outputFile, layerName));
        }

        return chunkLayers;
    }

}
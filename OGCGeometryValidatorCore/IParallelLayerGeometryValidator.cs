using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GdalToolsLib.Geometry;

namespace OGCGeometryValidatorCore;

public interface IParallelLayerGeometryValidator
{
    /// <summary>
    /// Assumption: It is more efficient, to switch to parallel processing, if the layer has above [limit] records.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="layerName"></param>
    /// <param name="limit">minimal record count to switch to parallel processing</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    bool SwitchToParallelProcessing(string file, string layerName);

    /// <summary>
    /// Parallel process of layer validation will separate the large layer into separate chunk files (created in a temporary subdirectory).
    /// The list of chunk files will be processed in parallel.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="layerName"></param>
    /// <returns></returns>
    Task<ConcurrentBag<LayerValidationResult>> ValidateGeometry(string fileName, string layerName);

    void GetLimit(int getValue);
}
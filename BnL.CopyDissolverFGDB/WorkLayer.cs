using GdalToolsLib.Layer;
using GdalToolsLib.Models;

namespace BnL.CopyDissolverFGDB;

/// <summary>
/// Represents a layer (name, status), that is managed by the Worklist
/// </summary>
public class WorkLayer
{
    public string DataSourcePath { get; init; }

    public string OriginalLayerName { get; init; }

    public LayerNameBafuContent LayerContentInfo { get; set; }

    public string CurrentLayerName { get; set; }

    public WorkLayer(LayerDetails layerDetails)
    {
        DataSourcePath = layerDetails.DataSourceFileName;
        OriginalLayerName = layerDetails.Name;
        CurrentLayerName = OriginalLayerName;
        LayerContentInfo = new LayerNameBafuContent(OriginalLayerName);
    }
}
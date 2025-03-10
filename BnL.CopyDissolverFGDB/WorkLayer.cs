using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using OSGeo.OGR;

namespace BnL.CopyDissolverFGDB;

/// <summary>
/// Represents a layer (name, status), that is managed by the Worklist
/// </summary>
public class WorkLayer
{
    public string DataSourcePath { get; set; }

    public string OriginalLayerName { get; init; }

    public string CurrentLayerName { get; set; }

    public wkbGeometryType GeometryType { get; set; }

    public LayerNameBafuContent LayerContentInfo { get; set; }


    public WorkLayer(LayerDetails layerDetails)
    {
        DataSourcePath = layerDetails.DataSourceFileName;
        OriginalLayerName = layerDetails.Name;
        CurrentLayerName = OriginalLayerName;
        GeometryType = layerDetails.GeomType;
        LayerContentInfo = new LayerNameBafuContent(OriginalLayerName);
    }
}
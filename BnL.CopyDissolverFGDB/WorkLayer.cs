using GdalToolsLib.Layer;
using OSGeo.OGR;

namespace BnL.CopyDissolverFGDB;

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
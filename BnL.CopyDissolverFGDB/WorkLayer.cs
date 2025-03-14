using GdalToolsLib.Layer;
using OSGeo.OGR;

namespace BnL.CopyDissolverFGDB;

public class WorkLayer
{
    public string DataSourcePath { get; set; }

    public string CurrentLayerName { get; set; }

    public string OutputLayerName { get; set; }

    public wkbGeometryType GeometryType { get; set; }

    public LayerNameBafuContent LayerContentInfo { get; set; }


    public WorkLayer(LayerDetails layerDetails)
    {
        DataSourcePath = layerDetails.DataSourceFileName;
        CurrentLayerName = layerDetails.Name;
        OutputLayerName = CurrentLayerName;
        GeometryType = layerDetails.GeomType;
        LayerContentInfo = new LayerNameBafuContent(CurrentLayerName);
    }
}
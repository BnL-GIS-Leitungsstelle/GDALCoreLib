using BnL.CopyDissolverFGDB.Parameters;
using OSGeo.OGR;

namespace BnL.CopyDissolverFGDB;

public class WorkLayer
{
    public string CurrentLayerName { get; set; }
    public string OutputLayerName { get; }
    public wkbGeometryType GeometryType { get; set; }
    public FilterParameter? Filter { get; }
    public BufferParameter? Buffer { get; }

    public WorkLayer(string currentLayerName,
                     string outputLayerName,
                     wkbGeometryType geometryType,
                     FilterParameter? filterParameter = null,
                     BufferParameter? bufferParameter = null)
    {
        CurrentLayerName = currentLayerName;
        OutputLayerName = outputLayerName;
        GeometryType = geometryType;
        Filter = filterParameter;
        Buffer = bufferParameter;
    }
}
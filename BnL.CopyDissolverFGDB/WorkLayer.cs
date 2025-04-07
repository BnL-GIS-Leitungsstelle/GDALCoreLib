using BnL.CopyDissolverFGDB.Parameters;
using GdalToolsLib.Layer;
using OSGeo.OGR;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BnL.CopyDissolverFGDB;

public class WorkLayer
{
    public string CurrentLayerName { get; set; }
    public string OutputLayerName { get; }
    public wkbGeometryType GeometryType { get; set; }
    public FilterParameter? Filter { get; }
    public BufferParameter? Buffer { get; }
    public bool Dissolve { get; init; }

    public WorkLayer(string currentLayerName,
                     string outputLayerName,
                     wkbGeometryType geometryType,
                     FilterParameter? filterParameter = null,
                     BufferParameter? bufferParameter = null,
                     bool dissolve = false)
    {
        CurrentLayerName = currentLayerName;
        OutputLayerName = outputLayerName;
        GeometryType = geometryType;
        Filter = filterParameter;
        Buffer = bufferParameter;
        Dissolve = dissolve;
    }
}
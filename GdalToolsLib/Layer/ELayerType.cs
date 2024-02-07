using System.ComponentModel;

namespace GdalToolsLib.Layer;

public enum ELayerType
{
    [Description("Polygon")]
    Polygon = 0,
    [Description("Polyline")]
    Polyline = 1,
    [Description("Point")]
    Point = 2,
    [Description("Geometry")]
    AllGeometry = 3,
    [Description("Table")]
    Table = 4,
    [Description("All")]
    All = 5
}
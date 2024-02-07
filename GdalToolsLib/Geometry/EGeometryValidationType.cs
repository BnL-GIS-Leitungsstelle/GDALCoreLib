using System.ComponentModel;

namespace GdalToolsLib.Geometry;

public enum EGeometryValidationType
{
    [Description("Null Geometry")]
    NullGeometry,

    [Description("Empty Geometry")]
    EmptyGeometry,

    [Description("Geometry-Counter = Zero")]
    GeometryCounterZero,

    [Description("Ring Self-Intersects")]
    RingSelfIntersects,

    [Description("Is Simple has invalid arguments")]
    InvalidIsSimple,

    [Description("Geometrytype-Mismatch according to Layer")]
    GeometrytypeMismatchAccordingToLayer,

    [Description("Feature to Layer MultiSurface-Type mismatch")]
    FeatureToLayerMultiSurfaceTypeMismatch,

    [Description("Non-simple Geometry")]
    NonSimpleGeometry,

    [Description("Repeated Points")]
    RepeatedPoints,

    [Description("Invalid Geometry by unspecified Reason")]
    InvalidGeometryUnspecifiedReason,

    [Description("Not Named")]
    NotNamed,

    [Description("Very Small Geometry")]
    VerySmall,

    [Description("Is Valid")]
    IsValid
}
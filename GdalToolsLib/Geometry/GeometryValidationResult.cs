using GdalToolsLib.Extensions;
using GdalToolsLib.Feature;
using GdalToolsLib.Models;

namespace GdalToolsLib.Geometry;

public class GeometryValidationResult
{
    public bool Valid { get; set; }

    public EGeometryValidationType ValidationResultType { get; set; }

    public EFeatureErrorLevel ErrorLevel { get; set; }


    public long FeatureFid { get; set; }


    public string? ObjNummer { get; set; }
    public string? Name { get; set; }

    public string? Remarks { get; set; }

    private GeometryValidationResult()
    { }


    public GeometryValidationResult(OgctFeature feature, OgctLayer layer, EGeometryValidationType validationType, string? remarks = default)
    {
        Valid = validationType == EGeometryValidationType.IsValid;
        FeatureFid = feature.FID;

        ObjNummer = feature.ObjNumber;
        Name = feature.ObjName;
        ValidationResultType = validationType;

        switch (ValidationResultType)
        {
            case EGeometryValidationType.IsValid:
                ErrorLevel = EFeatureErrorLevel.None;
                break;

            case EGeometryValidationType.FeatureToLayerMultiSurfaceTypeMismatch:
            case EGeometryValidationType.VerySmall:
            case EGeometryValidationType.GeometrytypeMismatchAccordingToLayer:
            case EGeometryValidationType.NonSimpleGeometry:
                ErrorLevel = EFeatureErrorLevel.Warning;
                break;

            default:
                ErrorLevel = EFeatureErrorLevel.Error;
                break;
        }


        Remarks = remarks;

        // log..

    }

    public override string ToString()
    {
        return Valid ? $"Geometry in FID = ({FeatureFid}) is VALID" : $" Invalid geometry in FID = ({FeatureFid}, ObjNummer = {ObjNummer}, Name = {Name}), Message: {ValidationResultType.GetEnumDescription(typeof(EGeometryValidationType))} ({Remarks})";
    }
}
using System.Collections.Generic;
using GdalToolsLib.Layer;

namespace GdalToolsLib.Geometry;

/// <summary>
/// A Validation Result contains all information regarding the geometry validity of one layerName.
/// </summary>
public class LayerValidationResult
{
    /// <summary>
    /// Returns the name of the file that was validated.
    /// </summary>
    public string? FileName { get; private set; }
    /// <summary>
    /// Returns the name of the layerName that was validated.
    /// </summary>
    public string LayerName { get; private set; }

    /// <summary>
    /// Returns all invalid features as a dictionary, where the key is the FID of the invalid feature and the value is the error.
    /// </summary>
    public List<GeometryValidationResult> InvalidFeatures { get; private set; }

    /// <summary>
    /// Returns true if all features in the layerName are valid.
    /// </summary>
    public bool IsValid => InvalidFeatures.Count == 0;

    public ELayerValidationType ValidationResultType { get; set; }


    public LayerValidationResult(string? fileName,string layerName)
    {
        FileName = fileName;
        LayerName = layerName;
        InvalidFeatures = new List<GeometryValidationResult>();
        ValidationResultType = ELayerValidationType.NotNamed;
    }

    public void SetLayerHasNoGeometry()
    {
        ValidationResultType = ELayerValidationType.LayerIsNoneGeometryType;
    }

    public void AddInvalidFeature(GeometryValidationResult geomValidationResult)
    {
        InvalidFeatures.Add(geomValidationResult);
        ValidationResultType = ELayerValidationType.LayerHasInvalidGeometries;
    }

    public List<GeometryValidationResult> GetErrorsByType(EGeometryValidationType geometryValidationType)
    {
        var invalidFeaturesByType = new List<GeometryValidationResult>();

        foreach (var geomValidationResult in InvalidFeatures)
        {
            if (geomValidationResult.ValidationResultType == geometryValidationType)
            {
                invalidFeaturesByType.Add(geomValidationResult);
            }
        }

        return invalidFeaturesByType;
    }

    public override string ToString()
    {
        return $"LayerName {LayerName}: ValidationResultType= {ValidationResultType} Errors ={InvalidFeatures.Count}";
    }
}
using System.Collections.Generic;

namespace OGCToolsNetCoreLib.Feature;


/// <summary>
/// A Validation Result contains all information regarding the geometry validity of one layerName.
/// </summary>
public class LayerAttributeWriteResult
{
    /// <summary>
    /// Returns the name of the file that was validated.
    /// </summary>
    public string FileName { get; private set; }

    /// <summary>
    /// Returns the name of the layer that was validated.
    /// </summary>
    public string LayerName { get; private set; }

    /// <summary>
    /// Returns the name of the attribute that was validated.
    /// </summary>
    public string AttributeName { get; private set; }

    /// <summary>
    /// Returns the value that was given to update the attribute.
    /// </summary>
    public object UpdateValue { get; private set; }


    /// <summary>
    /// Returns all invalid features as a dictionary, where the key is the FID of the invalid feature and the value is the error.
    /// </summary>
    public List<FeatureFieldWriteResult> InvalidFeatures { get; private set; }

    /// <summary>
    /// Returns true if all features in the layerName are valid.
    /// </summary>
    public bool IsValid => InvalidFeatures.Count == 0 && FeatureWriteErrorResultType == EFeatureWriteErrorResult.IsValid;

    public EFeatureWriteErrorResult FeatureWriteErrorResultType { get; set; }

    
    public LayerAttributeWriteResult(string fileName, string layerName, string attributeName, object updateValue)
    {
        FileName = fileName;
        LayerName = layerName;
        AttributeName = attributeName;
        UpdateValue = updateValue;

        InvalidFeatures = new List<FeatureFieldWriteResult>();
        FeatureWriteErrorResultType = EFeatureWriteErrorResult.IsValid;
    }


    public void SetFieldNotFound()
    {
        FeatureWriteErrorResultType = EFeatureWriteErrorResult.FieldNotFound;
    }


    public void SetFieldNotEditable()
    {
        FeatureWriteErrorResultType = EFeatureWriteErrorResult.FieldNotEditable;
    }

    public void SetValueCastFailed()
    {
        FeatureWriteErrorResultType = EFeatureWriteErrorResult.ValueCastToDataTypeFailed;
    }

    public void AddInvalidFeature(FeatureFieldWriteResult result)
    {
        InvalidFeatures.Add(result);
    }

    public List<FeatureFieldWriteResult> GetErrorsByType(EFieldWriteErrorType writeErrorType)
    {
        var invalidFeaturesByType = new List<FeatureFieldWriteResult>();

        foreach (var result in InvalidFeatures)
        {
            if (result.ResultValidationType == writeErrorType)
            {
                invalidFeaturesByType.Add(result);
            }
        }

        return invalidFeaturesByType;
    }

    public override string ToString()
    {
        return $"LayerName {LayerName}: Attribute= {AttributeName} Valid= {IsValid}, Errors in Layer ={InvalidFeatures.Count}";
    }
}
using System.Collections.Generic;
using GdalToolsLib.Feature;

namespace GdalToolsLib.Layer;

/// <summary>
/// A comparison result contains all information regarding the differences between two layers.
/// </summary>
public class LayerComparisonResult
{
    /// <summary>
    /// expected value
    /// </summary>
    public string MasterValue { get; private set; }

    /// <summary>
    /// current value
    /// </summary>
    public string CandidateValue { get; private set; }

    /// <summary>
    /// current value
    /// </summary>
    public string ComparisonSubject { get; private set; }
    /// <summary>
    /// Returns all different layer properties in a list.
    /// </summary>
    public List<FeatureComparisonResult> DifferentFeatureList { get; private set; }

    public ELayerComparisonDifference ComparisonDifference { get; private set; }

    /// <summary>
    /// Returns true if layer details have no differences.
    /// </summary>
    public bool IsLayerDetailEqual => DifferentFeatureList.Count == 0;


    public LayerComparisonResult(string masterValue, string candidateValue, string subject, ELayerComparisonDifference comparisonDifference)
    {
        MasterValue = masterValue;
        CandidateValue = candidateValue;
        ComparisonSubject = subject;
        ComparisonDifference = comparisonDifference;

        DifferentFeatureList = new List<FeatureComparisonResult>();
    }

        
    public void AddDifferentFeature(FeatureComparisonResult result, ELayerComparisonDifference differenceType)
    {
        DifferentFeatureList.Add(result);
        ComparisonDifference = differenceType;
    }

    //public List<GeometryValidationResult> GetErrorsByType(EGeometryValidationType geometryValidationType)
    //{
    //    var invalidFeaturesByType = new List<GeometryValidationResult>();

    //    foreach (var geomValidationResult in InvalidFeatures)
    //    {
    //        if (geomValidationResult.ValidationResultType == geometryValidationType)
    //        {
    //            invalidFeaturesByType.Add(geomValidationResult);
    //        }
    //    }

    //    return invalidFeaturesByType;
    //}

    public override string ToString()
    {
        return $"MasterInfo has {MasterValue} {ComparisonSubject}, candidate has {CandidateValue} {ComparisonSubject}. DifferenceType= {ComparisonDifference}, FeatureDetails ={DifferentFeatureList.Count}";
    }

}
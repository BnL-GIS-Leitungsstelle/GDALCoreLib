using System.Collections.Generic;

namespace GdalToolsLib.Feature;

/// <summary>
///  A comparison result contains all information regarding the differences between two features.
/// </summary>
public class FeatureComparisonResult
{
    public string IdentifierFieldName { get; set; }

    public string IdentifierValue { get; set; }

    public List<FieldComparisonResult> DifferentFieldValueList { get; set; }

    public bool IsValid => DifferentFieldValueList.Count == 0;

    public FeatureComparisonResult(string identifierFieldName, string identifierValue)
    {
        DifferentFieldValueList = new List<FieldComparisonResult>();

        IdentifierFieldName = identifierFieldName;

        IdentifierValue = identifierValue;
    }

    public void AddFieldDifference(string actualValue, string expectedValue, string fieldName, string optAreaDifference = "", string optPercentageDifference = "")
    {
        DifferentFieldValueList.Add(new FieldComparisonResult(actualValue, expectedValue,fieldName, optAreaDifference, optPercentageDifference));
    }

    public override string ToString()
    {
        return $"Feature ({IdentifierFieldName}= {IdentifierValue})";
    }
}
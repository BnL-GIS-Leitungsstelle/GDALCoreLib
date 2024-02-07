namespace GdalToolsLib.Feature;

/// <summary>
///  A comparison result contains all information regarding the differences between two fields.
/// </summary>
public class FieldComparisonResult
{
    public string ActualValue { get; }

    public string ExpectedValue { get; }

    public string FieldName { get; }

    public string AreaDifference { get; }

    public string PercentageDifference { get; }

    public FieldComparisonResult(string actualValue, string expectedValue, string fieldName, string optAreaDifference = "", string optPercentageDifference = "")
    {
        ActualValue = actualValue;
        ExpectedValue = expectedValue;
        FieldName = fieldName;

        AreaDifference = optAreaDifference;
        PercentageDifference = optPercentageDifference;
    }

    public override string ToString()
    {
        return $" Field {FieldName}: actual={ActualValue} vs. expected={ExpectedValue}";
    }

    public string ShowGeometricDifference()
    {
        return $" Geometry difference in {FieldName}: {ActualValue} qm vs. {ExpectedValue} qm. {AreaDifference} qm difference ({PercentageDifference} %)";
    }
}
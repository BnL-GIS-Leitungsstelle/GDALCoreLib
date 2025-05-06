using System.Collections.Generic;
using System.Linq;

namespace GdalToolsLib.Layer;

public class LayerDetailComparer
{
    /// <summary>
    /// Returns details of the master layer that was compared.
    /// </summary>
    public LayerDetails MasterInfo { get; private set; }

    /// <summary>
    /// Returns details of the candidate layer that was compared.
    /// </summary>
    public LayerDetails CandidateInfo { get; private set; }


    /// <summary>
    /// Returns all differences of layer properties in a list.
    /// </summary>
    public List<LayerComparisonResult> DetailDifferences { get; private set; }


    public LayerDetailComparer(LayerDetails masterInfo, LayerDetails candidateInfo)
    {
        MasterInfo = masterInfo;
        CandidateInfo = candidateInfo;
        DetailDifferences = new List<LayerComparisonResult>();
    }

    /// <summary>
    /// compares the properties of two LayerDetails-objects
    /// </summary>
    public void Run()
    {
        if (MasterInfo.FeatureCount != CandidateInfo.FeatureCount)
        {
            DetailDifferences.Add(new LayerComparisonResult(MasterInfo.FeatureCount.ToString(), CandidateInfo.FeatureCount.ToString(), "features", ELayerComparisonDifference.LayerDetail));
        }

        if (MasterInfo.GeomType != CandidateInfo.GeomType)
        {
            DetailDifferences.Add(new LayerComparisonResult(MasterInfo.GeomType.ToString(), CandidateInfo.GeomType.ToString(), "geometry", ELayerComparisonDifference.LayerDetail));
        }

        if (MasterInfo.LayerType != CandidateInfo.LayerType)
        {
            DetailDifferences.Add(new LayerComparisonResult(MasterInfo.LayerType.ToString(), CandidateInfo.LayerType.ToString(), "layer type", ELayerComparisonDifference.LayerDetail));
        }

        if (MasterInfo.Projection.SpRef != CandidateInfo.Projection.SpRef)
        {
            DetailDifferences.Add(new LayerComparisonResult(MasterInfo.Projection.SpRef.ToString(), CandidateInfo.Projection.SpRef.ToString(), "projection", ELayerComparisonDifference.LayerDetail));
        }

        if (MasterInfo.Extent.IsEqual(CandidateInfo.Extent) == false)
        {
            DetailDifferences.Add(new LayerComparisonResult(MasterInfo.Extent.Json, CandidateInfo.Extent.Json, "extent", ELayerComparisonDifference.LayerDetail));
        }

        if (MasterInfo.Schema.FieldList.Count == CandidateInfo.Schema.FieldList.Count)
        {
            CompareSchema();
        }
        else
        {
            DetailDifferences.Add(new LayerComparisonResult(MasterInfo.Schema.FieldList.Count.ToString(),
                CandidateInfo.Schema.FieldList.Count.ToString(), "field count", ELayerComparisonDifference.LayerDetail));
            var masterFieldNames = MasterInfo.Schema.FieldList.Select(f => f.Name).ToList();
            var candidateFieldNames = CandidateInfo.Schema.FieldList.Select(f => f.Name).ToList();

            var commonFields = masterFieldNames.Intersect(candidateFieldNames).ToList();

            DetailDifferences.Add(
                new LayerComparisonResult($"[{string.Join(", ", masterFieldNames.Except(commonFields))}]",
                    $"[{string.Join(", ", candidateFieldNames.Except(commonFields))}]",
                    "fields not contained in other layer",
                    ELayerComparisonDifference.LayerDetail
                )
            );
        }
    }

    /// <summary>
    /// compares two Objects of LayerSchema
    /// </summary>
    private void CompareSchema()
    {
        var comparison = MasterInfo.Schema.Compare(CandidateInfo.Schema);
        if (comparison.Count == 0) return;
        DetailDifferences.AddRange(comparison);
        DetailDifferences.Add(new LayerComparisonResult(MasterInfo.Schema.Json, CandidateInfo.Schema.Json, "schema", ELayerComparisonDifference.Schema));
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;

namespace GdalToolsLib.Feature;

public class LayerFeaturesComparer
{
    /// <summary>
    /// Returns details of the master layer that was compared.
    /// </summary>
    private LayerDetails MasterInfo { get; }

    /// <summary>
    /// Returns details of the candidate layer that was compared.
    /// </summary>
    private LayerDetails CandidateInfo { get; }

    /// <summary>
    /// Returns all differences of layer properties in a list.
    /// </summary>
    public List<FeatureComparisonResult> DifferenceFeatureList { get; }

    private IEnumerable<string>? OrderByFields { get; }

    public LayerFeaturesComparer(LayerDetails master, LayerDetails candidate, IEnumerable<string>? orderByFields)
    {
        MasterInfo = master;
        CandidateInfo = candidate;
        DifferenceFeatureList = [];
        OrderByFields = orderByFields;
    }

    /// <summary>
    /// compares the values of all attributes (fields) of the layer
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void RunCompareAttributeValues()
    {
        using var masterDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(MasterInfo.DataSourceFileName);
        using var masterLayer = masterDataSource.OpenLayer(MasterInfo.Name, OrderByFields);

        using var candidateDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(CandidateInfo.DataSourceFileName);
        using var candidateLayer = candidateDataSource.OpenLayer(CandidateInfo.Name, OrderByFields);

        // Order fields by name in an attempt to not care about the field order of the two layers (Obviously won't work, if the number of fields are different)
        var orderedFieldListMaster = MasterInfo.Schema.FieldList.OrderBy(f => f.Name).ToList();
        var orderedFieldListCandidate = CandidateInfo.Schema.FieldList.OrderBy(f => f.Name).ToList();

        for (int i = 0; i < MasterInfo.FeatureCount; i++)
        {
            using var masterFeature = masterLayer.OpenNextFeature();
            using var candidateFeature = candidateLayer.OpenNextFeature();

            if (masterFeature != null && candidateFeature != null)
            {
                var masterRow = masterFeature.ReadRow(orderedFieldListMaster);
                var candidateRow = candidateFeature.ReadRow(orderedFieldListCandidate);
                var compareResult = masterRow.Compare(candidateRow, orderedFieldListMaster, OrderByFields.ElementAtOrDefault(0));

                if (compareResult.IsValid) continue;

                ////_log.Warn($" Invalid geometry - {validationResult}");

                DifferenceFeatureList.Add(compareResult);
            }
            else
            {
                throw new ArgumentOutOfRangeException("one or both features are null");
            }
        }
    }
    /// <summary>
    /// compares geometries of the both layer
    /// </summary>
    public void RunCompareGeometries()
    {
        using var masterDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(MasterInfo.DataSourceFileName);
        using var masterLayer = masterDataSource.OpenLayer(MasterInfo.Name, OrderByFields);

        using var candidateDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(CandidateInfo.DataSourceFileName);
        using var candidateLayer = candidateDataSource.OpenLayer(CandidateInfo.Name, OrderByFields);

        for (int i = 0; i < MasterInfo.FeatureCount; i++)
        {
            using var masterFeature = masterLayer.OpenNextFeature();

            using var candidateFeature = candidateLayer.OpenNextFeature();

            if (masterFeature != null && candidateFeature != null)
            {
                using var masterGeom = masterFeature.OpenGeometry();

                using var candidateGeom = candidateFeature.OpenGeometry();

                var intersectionGeom = masterGeom.GetAndOpenIntersection(candidateGeom);

                var masterArea = masterGeom.Area;
                var intersectionArea = intersectionGeom.Area;

                var differenceArea = Math.Abs(masterArea - intersectionArea);
                var differencePercentage = differenceArea * 100 / masterArea;


                double diffTolerancePercentage = 0.05;
                double diffToleranceSquaremeter = 100;

                if (differenceArea > diffToleranceSquaremeter || differencePercentage > diffTolerancePercentage)
                {
                    var compareResult = new FeatureComparisonResult(OrderByFields.ElementAtOrDefault(0), masterFeature.GetFieldAsString(OrderByFields.ElementAtOrDefault(0)));
                    compareResult.AddFieldDifference($"{intersectionArea:F1}", $"{masterArea:F1}", "area", $"{differenceArea:F0}", $"{differencePercentage:F2}");

                    ////_log.Warn($" Invalid geometry - {validationResult}");

                    DifferenceFeatureList.Add(compareResult);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("one or both features are null");
            }

        }
    }
}
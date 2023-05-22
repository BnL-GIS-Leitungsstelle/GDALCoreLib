using System;
using System.Collections.Generic;
using System.Linq;
using OGCToolsNetCoreLib.DataAccess;
using OGCToolsNetCoreLib.Layer;

namespace OGCToolsNetCoreLib.Feature
{
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

        private FieldDefnInfo OrderByField { get; }

        public LayerFeaturesComparer(LayerDetails master, LayerDetails candidate, string orderByFieldName)
        {
            MasterInfo = master;
            CandidateInfo = candidate;
            DifferenceFeatureList = new List<FeatureComparisonResult>();

            OrderByField = MasterInfo.Schema.FieldList.First(_ => _.Name == orderByFieldName);
        }

        /// <summary>
        /// compares the values of all attributes (fields) of the layer
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void RunCompareAttributeValues()
        {
            using var masterDataSource = new GeoDataSourceAccessor().OpenDatasource(MasterInfo.DataSourceFileName);
            using var masterLayer = masterDataSource.OpenLayer(MasterInfo.Name, OrderByField.Name);

            using var candidateDataSource = new GeoDataSourceAccessor().OpenDatasource(CandidateInfo.DataSourceFileName);
            using var candidateLayer = candidateDataSource.OpenLayer(CandidateInfo.Name, OrderByField.Name);

            for (int i = 0; i < MasterInfo.FeatureCount; i++)
            {
                using var masterFeature = masterLayer.OpenNextFeature();

                using var candidateFeature = candidateLayer.OpenNextFeature();

                if (masterFeature != null && candidateFeature != null)
                {
                    var masterRow = masterFeature.ReadRow(MasterInfo.Schema.FieldList);
                    var candidateRow = candidateFeature.ReadRow(CandidateInfo.Schema.FieldList);

                    var compareResult = masterRow.Compare(candidateRow, MasterInfo.Schema.FieldList, OrderByField.Name);

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
            using var masterDataSource = new GeoDataSourceAccessor().OpenDatasource(MasterInfo.DataSourceFileName);
            using var masterLayer = masterDataSource.OpenLayer(MasterInfo.Name, OrderByField.Name);

            using var candidateDataSource = new GeoDataSourceAccessor().OpenDatasource(CandidateInfo.DataSourceFileName);
            using var candidateLayer = candidateDataSource.OpenLayer(CandidateInfo.Name, OrderByField.Name);

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
                                var compareResult = new FeatureComparisonResult(OrderByField.Name, masterFeature.GetFieldAsString(OrderByField.Name));
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
}

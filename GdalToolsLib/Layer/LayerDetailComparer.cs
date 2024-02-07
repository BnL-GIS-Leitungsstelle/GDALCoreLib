using System.Collections.Generic;

namespace OGCToolsNetCoreLib.Layer
{
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

        private void AddDifferentLayerDetail(LayerComparisonResult result)
        {
            DetailDifferences.Add(result);
        }

        /// <summary>
        /// compares the properties of two LayerDetails-objects
        /// </summary>
        public void Run()
        {
            if (MasterInfo.FeatureCount != CandidateInfo.FeatureCount)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.FeatureCount.ToString(), CandidateInfo.FeatureCount.ToString(), "features", ELayerComparisonDifference.LayerDetail));
            }

            if (MasterInfo.FieldCount != CandidateInfo.FieldCount)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.FieldCount.ToString(), CandidateInfo.FieldCount.ToString(), "fields", ELayerComparisonDifference.LayerDetail));
            }

            if (MasterInfo.GeomType != CandidateInfo.GeomType)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.GeomType.ToString(), CandidateInfo.GeomType.ToString(), "geometry", ELayerComparisonDifference.LayerDetail));
            }

            if (MasterInfo.LayerType != CandidateInfo.LayerType)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.LayerType.ToString(), CandidateInfo.LayerType.ToString(), "layer type", ELayerComparisonDifference.LayerDetail));
            }
  
            if (MasterInfo.Projection.SpRef != CandidateInfo.Projection.SpRef)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.Projection.SpRef.ToString(), CandidateInfo.Projection.SpRef.ToString(), "projection", ELayerComparisonDifference.LayerDetail));
            }

            if (MasterInfo.Extent.IsEqual(CandidateInfo.Extent) == false)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.Extent.Json, CandidateInfo.Extent.Json, "extent", ELayerComparisonDifference.LayerDetail));
            }

            CompareSchema();
        }

        /// <summary>
        /// compares two Objects of LayerSchema
        /// </summary>
        private void CompareSchema()
        {
            if (MasterInfo.Schema.IsEqual(CandidateInfo.Schema) == false)
            {
                AddDifferentLayerDetail(new LayerComparisonResult(MasterInfo.Schema.Json, CandidateInfo.Schema.Json, "schema", ELayerComparisonDifference.Schema));
            }
        }
    }
}

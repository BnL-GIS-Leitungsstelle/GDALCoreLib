using GdalCoreTest.Helper;
using OGCToolsNetCoreLib;
using OGCToolsNetCoreLib.DataAccess;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsLayerDetailsTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsLayerDetailsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
        }

        // 1. GetDetails
        // 2. ...


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void GetDetailsOfLayer_WithValidFiles_IsWorking(string file)
        {
            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var layerDetails = layer.LayerDetails;

                Assert.NotNull(layerDetails);

                if (layerName.Contains("SpRefNone") || layerDetails.GeomType == wkbGeometryType.wkbNone)
                {
                    Assert.True(layerDetails.Projection.Name == "Undefined geographic SRS", $"No projection found in {layerName} of {file}"); 
                }
                else
                {
                    Assert.True(layerDetails.Projection.Name != "Undefined geographic SRS", $"No projection found in {layerName} of {file}"); 
                }

                if (layerDetails.GeomType == wkbGeometryType.wkbNone)  // tables
                {
                    Assert.False(layerDetails.Extent.HasExtent, $"Extent missing in {layerName} of {file}"); 
                }
                else
                {
                    if (layerDetails.FeatureCount > 0)
                    {
                        Assert.True(layerDetails.Extent.HasExtent, $"Extent missing in {layerName} of {file}");
                    }
                }
                Assert.True(layerDetails.FieldCount == layerDetails.Schema.FieldList.Count);
            }
          
        }


    }

}

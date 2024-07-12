using GdalCoreTest.Helper;
using GdalToolsLib;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using OSGeo.GDAL;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{

    [Collection("Sequential")]
    public class CommonDataInfoTests
    {

        private readonly ITestOutputHelper _outputHelper;

        public CommonDataInfoTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
        }




        [Theory]
       // [MemberData(nameof(TestDataPathProvider.ValidShapeVectorData), MemberType = typeof(TestDataPathProvider))]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]

        public void GetGdalInfoVector(string file)
        {
            var dataSource = Ogr.Open(file, 0);

            Assert.NotNull(dataSource);

            for (var i = 0; i < dataSource.GetLayerCount(); i++)
            {
                Assert.NotNull(dataSource.GetLayerByIndex(i));
            }
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.ValidTifRasterData), MemberType = typeof(TestDataPathProvider))]
        public void GetGdalInfoRaster(string file)
        {
            using var inputDataset = Gdal.Open(file, Access.GA_ReadOnly);

            var info = Gdal.GDALInfo(inputDataset, new GDALInfoOptions(null));

            Assert.NotNull(info);
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.ValidTifRasterData), MemberType = typeof(TestDataPathProvider))]
        public void GetProjString(string file)
        {
            using var dataset = Gdal.Open(file, Access.GA_ReadOnly);

            string wkt = dataset.GetProjection();

            using var spatialReference = new OSGeo.OSR.SpatialReference(wkt);

            spatialReference.ExportToProj4(out string projString);

            Assert.False(string.IsNullOrWhiteSpace(projString));
        }

    }

}

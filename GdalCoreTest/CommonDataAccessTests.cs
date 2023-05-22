using System.IO;
using GdalCoreTest.Helper;
using OGCToolsNetCoreLib.DataAccess;
using OSGeo.GDAL;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{

    [Collection("Sequential")]
    public class CommonDataAccessTests
    {

        private readonly ITestOutputHelper _outputHelper;

        public CommonDataAccessTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
            //Ogr.RegisterAll();
            //Gdal.AllRegister();
        }

        [Fact]
        public void CheckAllDriversAvailable()
        {
            var driversExpected = 190;
            var accessor = new GeoDataSourceAccessor();

            var list= accessor.GetAvailableDrivers();

            Assert.True(list.Count >= driversExpected, $"It looks like this version of Gdal.Core has less than {driversExpected} drivers"); 
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void OpenDataSourceReadOnly_WithValidVectorData_IsWorking(string file)
        {
            _outputHelper.WriteLine($"Datasource of file: {Path.GetFileName(file)}");
            var dataSource = Ogr.Open(file, 0);

            Assert.NotNull(dataSource);
        }
    }
}

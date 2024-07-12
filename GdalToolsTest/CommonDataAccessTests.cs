using System.IO;
using GdalCoreTest.Helper;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;
using GdalConfiguration = GdalToolsLib.GdalConfiguration;

namespace GdalCoreTest
{
    /// <summary>
    /// for new GDAL-Version 3.6.4 with FGDb-write-access
    /// </summary>
    [Collection("Sequential")]
    public class CommonDataAccessTests
    {

        private readonly ITestOutputHelper _outputHelper;

        public CommonDataAccessTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
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

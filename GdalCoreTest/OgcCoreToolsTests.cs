using OGCToolsNetCoreLib.DataAccess;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
        }


        [Fact]
        public void GdalInfoTest()
        {
            var dataAccessor = new GeoDataSourceAccessor();
            
            foreach (var info in dataAccessor.GetInfo())
            {
                _outputHelper.WriteLine(info);
            }
        }
    }
}

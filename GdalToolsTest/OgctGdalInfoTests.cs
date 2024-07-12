using GdalToolsLib.Common;
using Xunit;
using Xunit.Abstractions;

namespace GdalToolsTest;


[Collection("Sequential")]
public class OgctGdalInfoTests 
{

    private readonly ITestOutputHelper _output;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="output"></param>
    public OgctGdalInfoTests(ITestOutputHelper output)
    {
        _output = output;
        GdalConfiguration.ConfigureGdal();
    }


    [Fact]
    public void GdalInfoTest()
    {
        var gdalInfo = new OgctGdalInfo();

        foreach (var info in gdalInfo.ShowSupportedDatasources())
        {
            _output.WriteLine(info);
        }
    }

    [Fact]
    public void CheckAllDriversAvailable()
    {
        var driversExpected = 190;
        var gdalInfo = new OgctGdalInfo();

        var list= gdalInfo.GetAvailableDriverNames();

        Assert.True(list.Count >= driversExpected, $"It looks like this version of Gdal.Core has less than {driversExpected} drivers"); 
    }


    [Fact]
    public void CheckAllDriversAvailable_MoreThan100Drivers()
    {
        var cntDriver = new OgctGdalInfo().GetAvailableDriverNames().Count;
        Assert.True(cntDriver > 100);
    }

}





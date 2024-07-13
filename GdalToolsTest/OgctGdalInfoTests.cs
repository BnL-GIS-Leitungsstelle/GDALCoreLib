using GdalToolsLib.Common;
using GdalToolsTest.Helper;
using OSGeo.GDAL;
using OSGeo.OGR;
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





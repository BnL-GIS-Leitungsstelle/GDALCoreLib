using System.IO;
using GdalCoreTest.Helper;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Extensions;
using GdalToolsTest.Helper;
using OSGeo.OGR;
using OSGeo.OSR;
using Xunit;
using Xunit.Abstractions;

namespace GdalToolsTest;

[Collection("Sequential")]
public class OgctDataAccessTests : IClassFixture<DataAccessSourceFixture>
{
    private readonly ITestOutputHelper _output;

    private DataAccessSourceFixture _fixture;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="output"></param>
    /// <param name="fixture">will cleanup all created files within the output folder at the end of all tests</param>
    public OgctDataAccessTests(ITestOutputHelper output, DataAccessSourceFixture fixture)
    {
        _output = output;
        GdalConfiguration.ConfigureGdal();
        _fixture = fixture;
    }


    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void OpenDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        _output.WriteLine($"Datasource of file: {Path.GetFileName(file)}");

        var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);
        Assert.NotNull(dataSource);
    }


    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CreateDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "created");
        if (Directory.Exists(outputdirectory) == false)
        {
            Directory.CreateDirectory(outputdirectory);
        }

        file = Path.Combine(outputdirectory, Path.GetFileName(file));

        _output.WriteLine($"CreateTest datasource of file: {Path.GetFileName(file)}");

        bool isExpectedOnReadOnly = SupportedDatasource.GetSupportedDatasource(file).Access == EAccessLevel.ReadOnly;

        if (isExpectedOnReadOnly)
        {
            Assert.Throws<DataSourceReadOnlyException>(() => new GeoDataSourceAccessor().CreateAndOpenDatasource(file, null));
        }
        else
        {
            var spRef = ESpatialRefWkt.CH1903plus_LV95;

            using (var dataSource = new GeoDataSourceAccessor().CreateAndOpenDatasource(file, new SpatialReference(spRef.GetEnumDescription(typeof(ESpatialRefWkt))), wkbGeometryType.wkbPolygon))
            {
                Assert.NotNull(dataSource);
            }
        }
    }


    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copied");

        _output.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

        var resultFile = new GeoDataSourceAccessor().CopyDatasource(file, outputdirectory, Path.GetFileName(file));

        Assert.True(File.Exists(resultFile) || Directory.Exists(resultFile));

        var dataSource = new GeoDataSourceAccessor().OpenDatasource(resultFile);

        Assert.NotNull(dataSource);
    }

    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void DeleteDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copy_and_delete");

        _output.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

        var resultFile = new GeoDataSourceAccessor().CopyDatasource(file, outputdirectory, Path.GetFileName(file));

        new GeoDataSourceAccessor().DeleteDatasource(resultFile);

        bool isExpectedAsFile = SupportedDatasource.GetSupportedDatasource(resultFile).FileType == EFileType.File;

        Assert.False(isExpectedAsFile ? File.Exists(resultFile) : Directory.Exists(resultFile));
    }


}
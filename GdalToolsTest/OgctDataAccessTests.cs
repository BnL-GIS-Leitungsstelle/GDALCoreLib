using System.Collections.Generic;
using System.IO;
using System.Linq;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Extensions;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using GdalToolsTest.Helper;
using OSGeo.OGR;
using OSGeo.OSR;
using Xunit;
using Xunit.Abstractions;

namespace GdalToolsTest;

[Collection("Sequential")]
public class OgctDataAccessTests : IClassFixture<DataAccessTestFixture>
{
    private readonly ITestOutputHelper _output;

    private DataAccessTestFixture _fixture;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="output"></param>
    /// <param name="fixture">will cleanup all created files within the output folder at the end of all tests</param>
    public OgctDataAccessTests(ITestOutputHelper output, DataAccessTestFixture fixture)
    {
        _output = output;
        GdalConfiguration.ConfigureGdal();
        _fixture = fixture;
    }


    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void GetLayerNames_WithValidFiles_IsWorking(string file)
    {
        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);

        var layerNames = dataSource.GetLayerNames();

        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(file);

        if (supportedDatasource.Type == EDataSourceType.SHP)
        {
            Assert.True(layerNames.Count == 1);
        }
        else
        {
            Assert.True(layerNames.Count > 0 && layerNames.Count < 17);
        }
    }



    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void OpenDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        _output.WriteLine($"Datasource of file: {Path.GetFileName(file)}");

        var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
        Assert.NotNull(dataSource);
    }

    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void OpenLayer_WithValidFiles_IsWorking(string file)
    {
        _output.WriteLine($"Get first LayerName of Datasource: {Path.GetFileName(file)}");

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);

        var layerNames = dataSource.GetLayerNames();

        using (var source = new OgctDataSourceAccessor().OpenOrCreateDatasource(file))
        {
            using (var layer = source.OpenLayer(layerNames.First()))
            {
                Assert.NotNull(layer);
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void HasLayer_WithValidFiles_IsWorking(string file)
    {
        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);

        var layerNames = dataSource.GetLayerNames();

        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(file);

        if (supportedDatasource.Type == EDataSourceType.SHP)
        {
            var layerExists = dataSource.HasLayer(layerNames[0]);
            Assert.True(layerExists);
        }
        else
        {
            // Number 2 is random
            var layerExists = dataSource.HasLayer(layerNames[0]);
            Assert.True(layerExists);
        }
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
            Assert.Throws<DataSourceReadOnlyException>(() => new OgctDataSourceAccessor().CreateAndOpenDatasource(file, null));
        }
        else
        {
            var spRef = ESpatialRefWkt.CH1903plus_LV95;

            using (var dataSource = new OgctDataSourceAccessor().CreateAndOpenDatasource(file, new SpatialReference(spRef.GetEnumDescription(typeof(ESpatialRefWkt))), wkbGeometryType.wkbPolygon))
            {
                Assert.NotNull(dataSource);
            }
        }
    }


    /// <summary>
    /// creates new layers with spRef = LV95 and of polygon-Featuretype
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CreateLayer_WithValidFiles_IsWorking(string file)
    {
        // prepare test-infrastructure
        var outputDir = Path.Combine(Path.GetDirectoryName(file), "createLayerFolder");
        if (Path.Exists(outputDir) == false)
        {
            Directory.CreateDirectory(outputDir);
        }
        var outputFile = Path.Combine(outputDir, Path.GetFileName(file));

        string createdLayerName = "createLayer";
        
        // two fields to add in the created layer
        var fieldList = new List<FieldDefnInfo>
        {
            new("ID_CH", FieldType.OFTString, 15, false, true),
            new("Canton", FieldType.OFTString, 2, false, false)
        };

        // check, if datasource supports creation (else ignore)
        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(outputFile);
        if (supportedDatasource.Access != EAccessLevel.Full) return;

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(outputFile, EAccessLevel.Full, true,
            ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon);

        // test the creation of a layer
        if (supportedDatasource.Type == EDataSourceType.SHP)
        {
            using var layer = dataSource.OpenLayer(dataSource.GetLayerNames().First());
        }
        else
        {
            using var layer = dataSource.CreateAndOpenLayer(createdLayerName, ESpatialRefWkt.CH1903plus_LV95,
                wkbGeometryType.wkbPolygon, fieldList);
        }

        // verify the creation result
        var layerNames = dataSource.GetLayerNames();

        var layerInfo = dataSource.GetLayerInfo(layerNames.First());
        Assert.True(layerInfo.GeomType == wkbGeometryType.wkbPolygon ||
                    layerInfo.GeomType == wkbGeometryType.wkbMultiPolygon);
    }


    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copied");

        _output.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

        var resultFile = new OgctDataSourceAccessor().CopyDatasource(file, outputdirectory, Path.GetFileName(file));

        Assert.True(File.Exists(resultFile) || Directory.Exists(resultFile));

        using (var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(resultFile))
        {
            Assert.NotNull(dataSource);
        }
    }

    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void DeleteDataSourceVector_WithValidFiles_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copy_and_delete");

        _output.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

        var resultFile = new OgctDataSourceAccessor().CopyDatasource(file, outputdirectory, Path.GetFileName(file));

        new OgctDataSourceAccessor().DeleteDatasource(resultFile);

        bool isExpectedAsFile = SupportedDatasource.GetSupportedDatasource(resultFile).FileType == EFileType.File;

        Assert.False(isExpectedAsFile ? File.Exists(resultFile) : Directory.Exists(resultFile));
    }

    /// <summary>
    /// creates new layers with spRef = LV95 and of polygon-Featuretype
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void DeleteLayer_WithValidFiles_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "createdLayer");

        file = Path.Combine(outputdirectory, Path.GetFileName(file));

        _output.WriteLine($"CreateTest datasource of file: {Path.GetFileName(file)}");

        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(file);

        if (supportedDatasource.Access == EAccessLevel.ReadOnly)
        {
            Assert.Throws<DataSourceReadOnlyException>(() =>
            {
                using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95,
                    wkbGeometryType.wkbPolygon);
            });
            return;
        }

        using var source = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95,
            wkbGeometryType.wkbPolygon);

        if (supportedDatasource.Type == EDataSourceType.SHP)
        {
            using var layer = source.OpenLayer(source.GetLayerNames().First());
            Assert.Throws<DataSourceMethodNotImplementedException>(() => source.DeleteLayer("createdLayer"));
        }
        else
        {
            using var layer = source.CreateAndOpenLayer("createdLayer", ESpatialRefWkt.CH1903plus_LV95,
                wkbGeometryType.wkbPolygon);
            source.DeleteLayer("createdLayer");
        }
    }

}
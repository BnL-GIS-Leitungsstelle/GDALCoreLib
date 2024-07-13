using System;
using System.IO;
using System.Linq;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Models;
using GdalToolsTest.Helper;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GdalToolsTest;

[Collection("Sequential")]
public class OgctLayerTests : IClassFixture<LayerTestFixture>
{
    private readonly ITestOutputHelper _output;

    private LayerTestFixture _fixture;

    public OgctLayerTests(ITestOutputHelper outputHelper, LayerTestFixture fixture)
    {
        _output = outputHelper;
        GdalToolsLib.GdalConfiguration.ConfigureGdal();
        _fixture = fixture;
    }


    /// <summary>
    /// creates new layers with spRef = LV95 and of polygon-Featuretype
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyLayer2Shp_WithValidFiles_IsWorking(string file)
    {
        using var memberDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
        // use 1. LayerName as source layer to copy
        string memberLayerName = memberDataSource.GetLayerNames().First();

        string outputdir = Path.Combine(Path.GetDirectoryName(file), "copiedLayerToShp");
        var outputFullFilename = Path.Combine(outputdir, $"{memberLayerName}.shp");

        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(outputFullFilename);
        

        _output.WriteLine($"Copy layer {memberLayerName} in {Path.GetFileName(file)} to shp-file {Path.GetFileName(outputFullFilename)}.");

        switch (supportedDatasource.Access)
        {
            case EAccessLevel.Full:
            {
                using var memberLayer = memberDataSource.OpenLayer(memberLayerName);

                // check for data structures, that are not supported by shapefile-output
                var hasBinaryField =
                    memberLayer.LayerDetails.Schema.FieldList.Any(field => field.Type == FieldType.OFTBinary);

                //var listShortenedFieldnames = memberLayer.LayerDetails.Schema.FieldList
                //    .Select(field => field.Name.Substring(0, Math.Min(10, field.Name.Length))).Distinct();

                //var hasTooLongFieldNamesThatAreIndistinguishableWhenShortened =
                //    listShortenedFieldnames.Count() != memberLayer.LayerDetails.Schema.FieldList.Count;

                if(memberLayer.IsGeometryType() == false)
                {
                    Assert.Throws<NotSupportedException>(() =>
                    {
                        using var targetDataSource = new OgctDataSourceAccessor().
                            OpenOrCreateDatasource(outputFullFilename, EAccessLevel.Full, true, 
                                memberLayer.GetSpatialRef(), memberLayer.LayerDetails.GeomType);
                    });

                    break;
                }
                    

                using var targetDataSource = new OgctDataSourceAccessor().
                    OpenOrCreateDatasource(outputFullFilename, EAccessLevel.Full, true, 
                        memberLayer.GetSpatialRef(), memberLayer.LayerDetails.GeomType);

                using var targetLayer = targetDataSource.OpenLayer(targetDataSource.GetLayerNames().First());
                if (hasBinaryField) // || hasTooLongFieldNamesThatAreIndistinguishableWhenShortened)
                {
                    Assert.Throws<ApplicationException>(() => memberLayer.CopyToLayer(targetLayer));
                }
                else if(targetLayer.DataSource.SupportInfo.Type == EDataSourceType.SHP && targetLayer.IsGeometryType() == false)
                {
                    Assert.Throws<NotSupportedException>(() => memberLayer.CopyToLayer(targetLayer));
                }
                else
                {
                    memberLayer.CopyToLayer(targetLayer);  //oft leer in arcgis: "could not find spatial index at -1"
                }
                Assert.True(true); // dummy to ensure, no exception is thrown


                break;
            }

            case EAccessLevel.ReadOnly:
                Assert.Throws<DataSourceReadOnlyException>(() =>
                {
                    using var layer = memberDataSource.CreateAndOpenLayer("createdLayer", ESpatialRefWkt.CH1903plus_LV95,
                        wkbGeometryType.wkbPolygon, null);
                });

                break;
            default:
                throw new NotImplementedException($"unsupported access level = {supportedDatasource.Access} ");
        }
    }


    /// <summary>
    /// creates new layers with spRef = LV95 and of polygon-Featuretype
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyLayerByRecordFilter_WithValidFiles_toGpkg_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copiedPartedLayer");

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);

        var layerNames = dataSource.GetLayerNames();

        foreach (var layerName in layerNames)
        {
            using var layer = dataSource.OpenLayer(layerName);
            var layerInfo = layer.LayerDetails;

            if (layerInfo.FeatureCount < 100) continue;


            // copy parts of in-layer into different out-layer
            int recordsLimitToCopy = 25;

            long numberOfParts = layerInfo.FeatureCount / recordsLimitToCopy;

            for (int i = 1; i <= numberOfParts; i++)
            {
                var outputFileName = $"{Path.GetFileNameWithoutExtension(file)}_{i}.gpkg";

                string outputFile = Path.Combine(outputdirectory, outputFileName);

                using var outputDatasource = new OgctDataSourceAccessor().OpenOrCreateDatasource(outputFile, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95);

                layer.CopyToLayer(outputDatasource, layerInfo.Name,
                    recordsLimitToCopy * i, recordsLimitToCopy * (i - 1));

            }
        }
    }

    /// <summary>
    /// copy layers within fgdb
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyLayers_WithinFgdb_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copiedLayers");

        // only fgdbs are allowed in test
        if (file.EndsWith(".gdb") == false)
            return;

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);

        var layerNames = dataSource.GetLayerNames();

        foreach (var layerName in layerNames)
        {
            using var sourceLayer = dataSource.OpenLayer(layerName);
            var layerInfo = sourceLayer.LayerDetails;

            sourceLayer.CopyToLayer(dataSource, layerInfo.Name + "_copy");

            using var outputDatasource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full, false, ESpatialRefWkt.CH1903plus_LV95);
            sourceLayer.CopyToLayer(outputDatasource, layerInfo.Name + "_copy_Output");
        }
    }

    /// <summary>
    /// creates new layers with spRef = LV95 and of polygon-Featuretype
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyLayerByRecordFilter_WithValidFiles_toFgdb_IsWorking(string file)
    {
        string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copiedPartedLayer");

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);

        var layerNames = dataSource.GetLayerNames();

        foreach (var layerName in layerNames)
        {
            using var layer = dataSource.OpenLayer(layerName);
            var layerInfo = layer.LayerDetails;

            if (layerInfo.FeatureCount < 100) continue;


            // copy parts of in-layer into different out-layer
            int recordsLimitToCopy = 25;

            long numberOfParts = layerInfo.FeatureCount / recordsLimitToCopy;

            for (int i = 1; i <= numberOfParts; i++)
            {
                var outputFileName = $"{Path.GetFileNameWithoutExtension(file)}_{i}.gdb";

                string outputFile = Path.Combine(outputdirectory, outputFileName);

                using var outputDatasource = new OgctDataSourceAccessor().OpenOrCreateDatasource(outputFile, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95);

                layer.CopyToLayer(outputDatasource, layerInfo.Name,
                    recordsLimitToCopy * i, recordsLimitToCopy * (i - 1));

            }
        }
    }
}
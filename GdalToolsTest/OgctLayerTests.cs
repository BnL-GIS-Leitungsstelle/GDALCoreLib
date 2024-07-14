using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Models;
using GdalToolsTest.Helper;
using OSGeo.OGR;
using OSGeo.OSR;
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

                    if (memberLayer.IsGeometryType() == false)
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
                    else if (targetLayer.DataSource.SupportInfo.Type == EDataSourceType.SHP && targetLayer.IsGeometryType() == false)
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
    public void CopyLayer2Gpkg_WithValidFiles_IsWorking(string file)
    {
        var memberDatasource = SupportedDatasource.GetSupportedDatasource(file);

        string outputdir = Path.Combine(Path.GetDirectoryName(file), "copiedLayerToGpkg");

        string outputFullFilename = String.Empty;

        switch (memberDatasource.Type)
        {
            case EDataSourceType.SHP:
                var gdbName = Path.GetFileNameWithoutExtension(file);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gpkg");
                break;

            case EDataSourceType.GPKG:
                gdbName = Path.GetFileNameWithoutExtension(file);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gpkg");
                break;

            case EDataSourceType.SHP_FOLDER:
                // get name of last subdirectory
                DirectoryInfo directoryInfo = new DirectoryInfo(file);

                gdbName = directoryInfo.Name.Substring(0, directoryInfo.Name.Length - 6);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gpkg");
                break;

            case EDataSourceType.OpenFGDB:
                // get name of last subdirectory
                directoryInfo = new DirectoryInfo(file);

                gdbName = directoryInfo.Name.Substring(0, directoryInfo.Name.Length - 4);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gpkg");
                break;

            default:

                throw new NotImplementedException();
        }


        using (var memberDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file))
        {
            Assert.NotNull(memberDataSource);

            var memberLayerNames = memberDataSource.GetLayerNames();

            foreach (var layerName in memberLayerNames)
            {

                _output.WriteLine($"Copy layer {layerName} in {Path.GetFileName(file)} to GPKG {Path.GetFileName(outputFullFilename)}.");

                using var layer = memberDataSource.OpenLayer(layerName);
                var layerInfo = layer.LayerDetails;

                using var outputDatasource = new OgctDataSourceAccessor().OpenOrCreateDatasource(outputFullFilename, EAccessLevel.Full, true, layerInfo.Projection.SpRef);

                layer.CopyToLayer(outputDatasource, layerInfo.Name);
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
        // only fgdbs are used in test
        if (file.EndsWith(".gdb") == false)
            return;

        _output.WriteLine($"Copy layer within {Path.GetFileName(file)}.");

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);

        var copiedLayerNames = new List<string>();

        var layerNames = dataSource.GetLayerNames();

        foreach (var layerName in layerNames)
        {
            using var sourceLayer = dataSource.OpenLayer(layerName);
            var layerInfo = sourceLayer.LayerDetails;

            var copiedLayerName = layerInfo.Name + "_copy";

            _output.WriteLine($"Copy layer {layerName} to {copiedLayerName} within {Path.GetFileName(file)}.");

            sourceLayer.CopyToLayer(dataSource, copiedLayerName);

            copiedLayerNames.Add(copiedLayerName);
        }

        // cleanup
        foreach (string copiedLayerName in copiedLayerNames)
        {
            if (dataSource.HasLayer(copiedLayerName))
            {
               dataSource.DeleteLayer(copiedLayerName);
            }
        }
    }


    /// <summary>
    /// copy layers within GPKG
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyLayers_WithinGpkg_IsWorking(string file)
    {
        // only fgdbs are used in test
        if (file.EndsWith(".gpkg") == false)
            return;

        _output.WriteLine($"Copy layer within {Path.GetFileName(file)}.");

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);

        var copiedLayerNames = new List<string>();

        var layerNames = dataSource.GetLayerNames();

        foreach (var layerName in layerNames)
        {
            using var sourceLayer = dataSource.OpenLayer(layerName);
            var layerInfo = sourceLayer.LayerDetails;

            var copiedLayerName = layerInfo.Name + "_copy";

            _output.WriteLine($"Copy layer {layerName} to {copiedLayerName} within {Path.GetFileName(file)}.");

            sourceLayer.CopyToLayer(dataSource, copiedLayerName);

            copiedLayerNames.Add(copiedLayerName);
        }

        // cleanup
        foreach (string copiedLayerName in copiedLayerNames)
        {
            if (dataSource.HasLayer(copiedLayerName))
            {
                dataSource.DeleteLayer(copiedLayerName);
            }
        }
    }

    /// <summary>
    /// creates new layers with spRef = LV95 and of polygon-Featuretype
    /// </summary>
    /// <param name="file"></param>
    [Theory]
    [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
    public void CopyLayer2Fgdb_WithValidFiles_IsWorking(string file)
    {
        var memberDatasource = SupportedDatasource.GetSupportedDatasource(file);

        string outputdir = Path.Combine(Path.GetDirectoryName(file), "copiedLayerToFgdb");

        if (Directory.Exists(outputdir) == false)
        {
            Directory.CreateDirectory(outputdir);
        }

        string outputFullFilename = String.Empty;

        switch (memberDatasource.Type)
        {
            case EDataSourceType.SHP:
                var gdbName = Path.GetFileNameWithoutExtension(file);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gdb");
                break;

            case EDataSourceType.GPKG:
                gdbName = Path.GetFileNameWithoutExtension(file);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gdb");
                break;

            case EDataSourceType.SHP_FOLDER:
                // get name of last subdirectory
                DirectoryInfo directoryInfo = new DirectoryInfo(file);

                gdbName = directoryInfo.Name.Substring(0, directoryInfo.Name.Length - 6);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gdb");
                break;

            case EDataSourceType.OpenFGDB:
                // get name of last subdirectory
                directoryInfo = new DirectoryInfo(file);

                gdbName = directoryInfo.Name.Substring(0, directoryInfo.Name.Length - 4);
                outputFullFilename = Path.Combine(outputdir, $"{gdbName}_created.gdb");
                break;

            default:

                throw new NotImplementedException();
        }


        using (var memberDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file))
        {
            Assert.NotNull(memberDataSource);

            var memberLayerNames = memberDataSource.GetLayerNames();

            foreach (var layerName in memberLayerNames)
            {

                _output.WriteLine($"Copy layer {layerName} in {Path.GetFileName(file)} to FGDB {Path.GetFileName(outputFullFilename)}.");

                using var layer = memberDataSource.OpenLayer(layerName);
                var layerInfo = layer.LayerDetails;

                using var outputDatasource = new OgctDataSourceAccessor().OpenOrCreateDatasource(outputFullFilename, EAccessLevel.Full, true, layerInfo.Projection.SpRef);

                layer.CopyToLayer(outputDatasource, layerInfo.Name);
            }
        }



    }
}
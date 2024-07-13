using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using GdalToolsLib;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Geometry;
using GdalToolsLib.Models;
using GdalToolsTest.Helper;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{

    [Collection("Sequential")]
    public class OgcCoreToolsCheckGeometryTests
    {

        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsCheckGeometryTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
            //Ogr.RegisterAll();
            //Gdal.AllRegister();
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public async Task CheckGeom_DataShouldContain_RingSelfintersection(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();


            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var validationResult = await layer.ValidateGeometryAsync();

                if (validationResult.IsValid == false)
                {
                    var ringErrors = validationResult.GetErrorsByType(EGeometryValidationType.RingSelfIntersects).Count;
                    if (ringErrors > 0)
                    {
                        var interpreter = new LayernameParser(layerName,EGeometryValidationType.RingSelfIntersects);
                        if (interpreter.HasErrorsOfType)
                        {
                            Assert.True(interpreter.ErrorCount == ringErrors);
                        }
                    }
                }
            }
            Assert.True(true);
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public async Task CheckGeom_DataShouldContain_GeometrytypeMismatchAccordingToLayer(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var validationResult = await layer.ValidateGeometryAsync();

                if (validationResult.IsValid == false)
                {
                    var errorCnt = validationResult.GetErrorsByType(EGeometryValidationType.GeometrytypeMismatchAccordingToLayer).Count;
                    if (errorCnt > 0)
                    {
                        var layernameParser = new LayernameParser(layerName, EGeometryValidationType.GeometrytypeMismatchAccordingToLayer);
                        if (layernameParser.HasErrorsOfType)
                        {
                            Assert.True(layernameParser.ErrorCount == errorCnt,$"interpreter.ErrorCount {layernameParser.ErrorCount} == errorCnt {errorCnt}");
                        }
                    }
                }
            }
            Assert.True(true);
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public async Task CheckGeom_DataShouldContain_FeatureToLayerMultiSurfaceTypeMismatch(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var validationResult = await layer.ValidateGeometryAsync();

                if (validationResult.IsValid == false)
                {
                    var errorCnt = validationResult.GetErrorsByType(EGeometryValidationType.FeatureToLayerMultiSurfaceTypeMismatch).Count;
                    if (errorCnt > 0)
                    {
                        var interpreter = new LayernameParser(layerName, EGeometryValidationType.FeatureToLayerMultiSurfaceTypeMismatch);
                        if (interpreter.HasErrorsOfType)
                        {
                            Assert.True(interpreter.ErrorCount == errorCnt);
                        }
                    }
                }
            }
            Assert.True(true);
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public async Task CheckGeom_DataShouldContain_NoGeometry_MissingTestData(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var validationResult = await layer.ValidateGeometryAsync();

                if (validationResult.IsValid == false)
                {
                    var errorCnt = validationResult.GetErrorsByType(EGeometryValidationType.EmptyGeometry).Count;
                    if (errorCnt > 0)
                    {
                        var interpreter = new LayernameParser(layerName, EGeometryValidationType.EmptyGeometry);
                        if (interpreter.HasErrorsOfType)
                        {
                            Assert.True(interpreter.ErrorCount == errorCnt);
                        }
                    }
                }
            }
            Assert.True(true);
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public async Task CheckGeom_DataShouldContain_GeometryCounterZero_MissingTestData(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var validationResult = await layer.ValidateGeometryAsync();

                if (validationResult.IsValid == false)
                {
                    var errorCnt = validationResult.GetErrorsByType(EGeometryValidationType.GeometryCounterZero).Count;
                    if (errorCnt > 0)
                    {
                        var interpreter = new LayernameParser(layerName, EGeometryValidationType.GeometryCounterZero);
                        if (interpreter.HasErrorsOfType)
                        {
                            Assert.True(interpreter.ErrorCount == errorCnt);
                        }
                    }
                }
            }
            Assert.True(true);
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public async Task CheckGeom_DataShouldContain_InvalidGeometryUnspecifiedReason(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var validationResult = await layer.ValidateGeometryAsync();

                if (validationResult.IsValid == false)
                {
                    var errorCnt = validationResult.GetErrorsByType(EGeometryValidationType.InvalidGeometryUnspecifiedReason).Count;
                    if (errorCnt > 0)
                    {
                        var interpreter = new LayernameParser(layerName, EGeometryValidationType.InvalidGeometryUnspecifiedReason);
                        if (interpreter.HasErrorsOfType)
                        {
                            Assert.True(interpreter.ErrorCount == errorCnt);
                        }
                    }
                }
            }
            Assert.True(true);
        }

        [Fact]
        public async Task CheckSelfOverlap_PreparedGeodata_ShouldHaveOneSelfoverlap()
        {
            var basePath = TestDataPathProvider.GetTestDataFolder(TestDataPathProvider.TestDataFolderSpecific);
            var wildruhezonenPath = Path.Combine(basePath, "ValidateGeometry.gdb");
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(wildruhezonenPath);
            using var layer = dataSource.OpenLayer("LayerSelfOverlapError1_polygon_SpRefLV95_epsg2056_4rec");
            var result = await layer.ValidateSelfOverlapAsync();
            result.Count.Should().Be(1);
        }

        [Fact]
        public async Task MakeValid_PreparedGeodata_ShouldSolveIssues()
        {
            var basePath = TestDataPathProvider.GetTestDataFolder(TestDataPathProvider.TestDataFolderSpecific);
            var masterPath = Path.Combine(basePath, "ValidateGeometry.gpkg");

            var workSourceName = $"{Guid.NewGuid()}.gpkg";
            var workPath =
                new OgctDataSourceAccessor().CopyDatasource(masterPath, basePath, workSourceName);
            var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(workPath, EAccessLevel.Full);
            var layer = dataSource.OpenLayer("GeometryErrorRingSelfIntersects45_SpRefLV03_epsg_rec");
            var result = await layer.ValidateGeometryAsync();
            result.IsValid.Should().BeFalse();
            var objIDList = result.InvalidFeatures.OrderBy(x => x.FeatureFid).Select(x => x.FeatureFid).ToList();
            result = await layer.ValidateGeometryAsync(null, null, true);
            dataSource.FlushCache();
            result.IsValid.Should().BeFalse();
            result = await layer.ValidateGeometryAsync();
            result.IsValid.Should().BeTrue();
            layer.Dispose();
            dataSource.Dispose();
            new OgctDataSourceAccessor().DeleteDatasource(workPath);
        }

        [Fact]
        public void GdalDoesNotFailOnMakeValid()
        {
            var wkt = _staticWkt;
            var geom = Geometry.CreateFromWkt(wkt);
            Assert.False(geom.IsValid());
            var valid = geom.MakeValid(null);
            Assert.True(valid.IsValid());
        }

        private static string _staticWkt =
                "POLYGON((8.39475541866082 18.208975124406155,24.390849168660818 39.41962323304138,43.19944291866082 27.430752179449893,3.9123335436608198 22.736137385695233,8.39475541866082 18.208975124406155))";

    }
}

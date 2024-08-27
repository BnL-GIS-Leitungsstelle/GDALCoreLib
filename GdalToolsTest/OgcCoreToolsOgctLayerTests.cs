using System.Collections.Generic;
using System.Linq;
using GdalToolsLib;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Models;
using GdalToolsTest.Helper;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsOgctLayerTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsOgctLayerTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
        }


        #region buffer

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void Buffer_AllRecords_PointLayers_IsWorking(string file)
        {
            var sourceType = SupportedDatasource.GetSupportedDatasource(file);

            _outputHelper.WriteLine(file);

            double bufferDistance = 52.26;

            var layersTobuffer = new List<string>()
                    { "simple_polygon_SpRefLV95_epsg2056_11rec",      // polygon
                        "borders_polyline_SpRefNone_epsgNone_185rec", // polyline
                        "amphib_point_SpRefLV95_epsg2056_94rec" };    // point

            if (sourceType.Type == EDataSourceType.OpenFGDB || sourceType.Type == EDataSourceType.SHP_FOLDER)
            {
                return;
            }

            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                _outputHelper.WriteLine(layerName);

                if (layersTobuffer.Contains(layerName) == false)
                {
                    continue;
                }

                using var layer = dataSource.OpenLayer(layerName);

                string outputLayerName = layer.BufferToLayer(dataSource, bufferDistance);

                OgctDataSource outputDataSource = null;
                IOgctLayer outputlayer = null;

                if (dataSource.SupportInfo.Type == EDataSourceType.GPKG)
                {
                    outputlayer = dataSource.OpenLayer(outputLayerName);
                }
                else   // SHP
                {
                    outputDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file.Replace(layerName, outputLayerName));
                    outputlayer = outputDataSource.OpenLayer(outputLayerName);
                }

                var outputArea = outputlayer.CalculateArea(true);

                outputlayer.Dispose();

                if (outputDataSource != null)
                {
                    outputDataSource.Dispose();
                }

                if (layerName == "amphib_point_SpRefLV95_epsg2056_94rec")
                {
                    Assert.InRange(outputArea, 806154.1, 806154.2);
                }

                if (layerName == "simple_polygon_SpRefLV95_epsg2056_11rec")
                {
                    Assert.InRange(outputArea, 162113, 162113.3);
                }

                if (layerName == "borders_polyline_SpRefNone_epsgNone_185rec")
                {
                    Assert.InRange(outputArea, 64088.31, 64088.32);
                }

                // cleanup generated files / layers

                if (sourceType.Type == EDataSourceType.GPKG)
                {
                    dataSource.DeleteLayer(outputLayerName);
                    return;
                }

                if (sourceType.Type == EDataSourceType.SHP)
                {
                    var outputFilename = file.Replace(layerName, outputLayerName);
                    new OgctDataSourceAccessor().DeleteDatasource(outputFilename);
                    return;
                }
            }

        }

        #endregion


        #region dissolve

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void Dissolve_AllRecords_SelectedRecords_IsWorking(string file)
        {
            var sourceType = SupportedDatasource.GetSupportedDatasource(file);

            _outputHelper.WriteLine(file);
            
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                _outputHelper.WriteLine(layerName);

                if (layerName.StartsWith("N2016_GeoIV_Park"))
                {
                    using var layer = dataSource.OpenLayer(layerName);
                    var inputLayerInfo = layer.LayerDetails;
                    var fieldsToDissolve = inputLayerInfo.Schema.FieldList.Where(f => f.Name =="ObjNummer" || f.Name.Contains("Name")).ToList();
                   
                    // check, if datasource supports creation (else ignore)
                    if (sourceType.Access != EAccessLevel.Full) return;

                    using var dataSourcetest = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full, true,
                        ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon);

                    // test the creation of a layer
                    if (sourceType.Type == EDataSourceType.SHP)
                    {
                        using var testLayer = dataSource.OpenLayer(dataSource.GetLayerNames().First());
                    }
                    else
                    {
                        //using var testLayertest = dataSourcetest.CreateAndOpenLayer("N2016_GeoIV_ParkLayerTest", ESpatialRefWkt.CH1903plus_LV95,
                        //    wkbGeometryType.wkbPolygon, fieldsToDissolve);
                        //using var testLayer = dataSource.CreateAndOpenLayer("N2016_GeoIV_ParkTest", ESpatialRefWkt.CH1903plus_LV95,
                        //    wkbGeometryType.wkbPolygon, fieldsToDissolve);
                    }
                    
                    
                    string outputLayerName = layer.DissolveToLayer(null, fieldsToDissolve);

                    OgctDataSource outputDataSource = null;
                    IOgctLayer outputlayer = null;

                    if (dataSource.SupportInfo.Type == EDataSourceType.GPKG || dataSource.SupportInfo.Type == EDataSourceType.OpenFGDB)
                    {
                        outputlayer = dataSource.OpenLayer(outputLayerName);
                    }
                    else   // SHP
                    {
                        outputDataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file.Replace(layerName, outputLayerName));
                        outputlayer = outputDataSource.OpenLayer(outputLayerName);
                    }

                    using var feature = outputlayer.OpenNextFeature();

                    using var featureGeom = feature.OpenGeometry();

                    var area = featureGeom.Area;

                    var outputLayerInfo = outputlayer.LayerDetails;

                    outputlayer.Dispose();

                    if (outputDataSource != null)
                    {
                        outputDataSource.Dispose();
                    }

                    Assert.True(outputLayerInfo.FeatureCount == 20, "mismatch in number of records");
                   

                    // cleanup generated files / layers

                    if (sourceType.Type == EDataSourceType.GPKG)
                    {
                        dataSource.DeleteLayer(outputLayerName);
                        return;
                    }

                    if (sourceType.Type == EDataSourceType.SHP)
                    {
                        var outputFilename = file.Replace(layerName, outputLayerName);
                        new OgctDataSourceAccessor().DeleteDatasource(outputFilename);
                        return;
                    }

                }
            }
        }

        #endregion


    }
}

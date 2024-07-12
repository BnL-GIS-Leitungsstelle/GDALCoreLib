using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GdalCoreTest.Helper;
using GdalToolsLib;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Layer;
using OSGeo.OGR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsLayerAccessTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsLayerAccessTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
        }




        // 1. GetLayerNames
        // 2. Exists
        // 3. Open
        // 4. CreateTest
        // 5. Delete
        // 6. Copy


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void GetLayerNames_WithValidFiles_IsWorking(string file)
        {
            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

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
        public void LayerExists_WithValidFiles_IsWorking(string file)
        {
            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

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
        public void OpenLayer_WithValidFiles_IsWorking(string file)
        {
            _outputHelper.WriteLine($"Get first LayerName of Datasource: {Path.GetFileName(file)}");

            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

            var layerNames = dataSource.GetLayerNames();

            using (var source = new GeoDataSourceAccessor().OpenDatasource(file))
            {
                using (var layer = source.OpenLayer(layerNames.First()))
                {
                    Assert.NotNull(layer);
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
            string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "createdLayer");

            file = Path.Combine(outputdirectory, Path.GetFileName(file));

            _outputHelper.WriteLine($"CreateTest datasource of file: {Path.GetFileName(file)}");

            var supportedDatasource = SupportedDatasource.GetSupportedDatasource(file);

            // fields to add
            var fieldList = new List<FieldDefnInfo>
            {
                new("ID_CH", FieldType.OFTString, 15, false, true),
                new("Canton", FieldType.OFTString, 2, false, false)
            };

            if (supportedDatasource.Access == EAccessLevel.Full)
            {
                using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon);

                if (supportedDatasource.Type == EDataSourceType.SHP)
                {
                    using var layer = dataSource.OpenLayer(dataSource.GetLayerNames().First());
                }
                else
                {
                    using var layer = dataSource.CreateAndOpenLayer("createdLayer", ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon, fieldList);
                }

                var layerNames = dataSource.GetLayerNames();
                
                var layerInfo = dataSource.GetLayerInfo(layerNames.First());
                Assert.True(layerInfo.GeomType == wkbGeometryType.wkbPolygon || layerInfo.GeomType == wkbGeometryType.wkbMultiPolygon);

            }
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

            _outputHelper.WriteLine($"CreateTest datasource of file: {Path.GetFileName(file)}");

            var supportedDatasource = SupportedDatasource.GetSupportedDatasource(file);

            if (supportedDatasource.Access == EAccessLevel.ReadOnly)
            {
                Assert.Throws<DataSourceReadOnlyException>(() =>
                {
                    using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95,
                            wkbGeometryType.wkbPolygon);
                });
                return;
            }

            using var source = new GeoDataSourceAccessor().OpenDatasource(file, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95,
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



        /// <summary>
        /// creates new layers with spRef = LV95 and of polygon-Featuretype
        /// </summary>
        /// <param name="file"></param>
        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void CopyLayer_WithValidFiles_toShape_IsWorking(string file)
        {
            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

            string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copiedLayer");

            var fileOut = Path.Combine(outputdirectory, "copiedLayer.shp");
            // use 1. LayerName as source layer to copy
            string layerNameIn = dataSource.GetLayerNames().First();

            _outputHelper.WriteLine($"Copy layer {layerNameIn} in {Path.GetFileName(file)} to shp-file {Path.GetFileName(fileOut)}.");

            var supportedDatasource = SupportedDatasource.GetSupportedDatasource(fileOut);

            if (supportedDatasource.Access == EAccessLevel.Full)
            {
                using var inputLayer = dataSource.OpenLayer(layerNameIn);

                var hasBinaryField =
                    inputLayer.LayerDetails.Schema.FieldList.Any(field => field.Type == FieldType.OFTBinary);

                var hasTooLongFieldNamesThatAreIndistinguishableWhenShortened =
                    inputLayer.LayerDetails.Schema.FieldList.Select(field => field.Name.Substring(0, Math.Min(10, field.Name.Length))).Distinct()
                        .Count() != inputLayer.LayerDetails.Schema.FieldList.Count;



                using var targetDataSource = new GeoDataSourceAccessor().OpenDatasource(fileOut, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95);

                using var targetLayer = targetDataSource.OpenLayer(targetDataSource.GetLayerNames().First());
                if (hasBinaryField || hasTooLongFieldNamesThatAreIndistinguishableWhenShortened)
                {
                    Assert.Throws<ApplicationException>(() => inputLayer.CopyToLayer(targetLayer));
                }
                else
                {
                    inputLayer.CopyToLayer(targetLayer);
                }
                Assert.True(true); // dummy to ensure, no exception is thrown

            }
            if (supportedDatasource.Access == EAccessLevel.ReadOnly)
            {
                Assert.Throws<DataSourceReadOnlyException>(() =>
                {
                    using var layer = dataSource.CreateAndOpenLayer("createdLayer", ESpatialRefWkt.CH1903plus_LV95,
                            wkbGeometryType.wkbPolygon, null);
                });
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

            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

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

                    using var outputDatasource = new GeoDataSourceAccessor().OpenDatasource(outputFile, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95);

                    layer.CopyToLayer(outputDatasource, layerInfo.Name,
                        recordsLimitToCopy * i, recordsLimitToCopy * (i - 1));

                }

                continue;
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

            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var sourceLayer = dataSource.OpenLayer(layerName);
                var layerInfo = sourceLayer.LayerDetails;

                sourceLayer.CopyToLayer(dataSource, layerInfo.Name + "_copy");

                using var outputDatasource = new GeoDataSourceAccessor().OpenDatasource(file, EAccessLevel.Full, false, ESpatialRefWkt.CH1903plus_LV95);
                sourceLayer.CopyToLayer(outputDatasource, layerInfo.Name + "_copy_Output");



                continue;
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

            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(file);

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

                    using var outputDatasource = new GeoDataSourceAccessor().OpenDatasource(outputFile, EAccessLevel.Full, true, ESpatialRefWkt.CH1903plus_LV95);

                    layer.CopyToLayer(outputDatasource, layerInfo.Name,
                        recordsLimitToCopy * i, recordsLimitToCopy * (i - 1));

                }

                continue;
            }
        }
    }
}

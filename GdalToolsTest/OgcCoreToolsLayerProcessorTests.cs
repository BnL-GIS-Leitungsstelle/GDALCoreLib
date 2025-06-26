using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GdalToolsLib;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Feature;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Helpers;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using GdalToolsTest.Helper;
using Xunit;
using Xunit.Abstractions;
using MaxRev.Gdal.Core;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsLayerProcessorTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsLayerProcessorTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalBase.ConfigureAll();
        }

        // 1. Update attribute and ValueCast
        // 2. ...


        #region Write Attribute

        public static IEnumerable<object[]> ValueData =>
              new List<object[]>
              {
                new object[] { "Int", 1},
                new object[] { "Int64", 3147483647}, // greater than int 32
                new object[] { "Real", 1.5000},
                new object[] { "RealBig", -13800000000.5},
                new object[] { "DateTime", DateTime.Now },
                new object[] { "DateTimeMin", DateTime.MinValue },
                new object[] { "String5", "123"},
                new object[] { "String", "abcdefghij"},
                new object[] { "StringDateTime", "31.12.2021"},
                new object[] { "nullValue", null }
              };


        [Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoInt32_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("Int", FieldType.OFTInteger, 10, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "Int":
                    case "String5":
                    case "nullValue":
                        {
                            Assert.True(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                    case "Int64":
                    case "String":
                    case "DateTime":
                    case "Real":
                    case "RealBig":
                    case "DateTimeMin":
                    case "StringDateTime":
                        {
                            Assert.False(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                }
            }
        }


        [Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoInt64_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("Int64", FieldType.OFTInteger64, 10, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "Int":
                    case "Int64":
                    case "String5":
                    case "nullValue":
                        {
                            Assert.True(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                    case "String":
                    case "Real":
                    case "RealBig":
                    case "DateTime":
                    case "DateTimeMin":
                    case "StringDateTime":
                        {
                            Assert.False(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                }
            }
        }



        [Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoDateTime_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("DateTime", FieldType.OFTDateTime, 10, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "DateTime":
                    case "DateTimeMin":
                    case "StringDateTime":
                    case "nullValue":
                        {
                            Assert.True(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                    case "Int":
                    case "Int64":
                    case "Real":
                    case "RealBig":
                    case "String5":
                    case "String":
                        {
                            Assert.False(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                }
            }
        }


        [Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoDouble_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("Real", FieldType.OFTReal, 10, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "Int":
                    case "Int64":
                    case "Real":
                    case "RealBig":
                    case "String5":
                    case "nullValue":
                        {
                            Assert.True(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                    case "String":
                    case "DateTime":
                    case "DateTimeMin":
                    case "StringDateTime":
                        {
                            Assert.False(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoString5_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("String5", FieldType.OFTString, 5, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "Int":
                    case "Real":
                    case "String5":
                    case "nullValue":
                        {
                            Assert.True(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                    case "Int64":
                    case "RealBig":
                    case "DateTime":
                    case "DateTimeMin":
                    case "StringDateTime":
                    case "String":

                        {
                            Assert.False(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoString_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("String", FieldType.OFTString, 50, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "Int":
                    case "String5":
                    case "String":
                    case "Int64":
                    case "Real":
                    case "RealBig":
                    case "DateTime":
                    case "DateTimeMin":
                    case "StringDateTime":
                    case "nullValue":
                        {
                            Assert.True(canCast == EFieldWriteErrorType.IsValid);
                            break;
                        }

                }
            }
        }


        //[Theory]
        [MemberData(nameof(ValueData))]
        public void CastDifferentValueTypesIntoBACKUP_WithInlineData(string valueName, object value)
        {
            var schema = new LayerSchema();
            schema.FieldList.Add(new FieldDefnInfo("Int", FieldType.OFTInteger, 10, true, false));
            schema.FieldList.Add(new FieldDefnInfo("Int64", FieldType.OFTInteger64, 10, true, false));
            schema.FieldList.Add(new FieldDefnInfo("Real", FieldType.OFTReal, 10, true, false));
            schema.FieldList.Add(new FieldDefnInfo("DateTime", FieldType.OFTDateTime, 10, true, false));
            schema.FieldList.Add(new FieldDefnInfo("String5", FieldType.OFTString, 5, true, false));
            schema.FieldList.Add(new FieldDefnInfo("String", FieldType.OFTString, 50, true, false));

            foreach (FieldDefnInfo defnInfo in schema.FieldList)
            {
                var canCast = schema.CanValueBeCastToFieldDataType(defnInfo, value);

                switch (valueName)
                {
                    case "Int":
                        {
                            if (defnInfo.Name == "Int") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "Int64":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "DateTime":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "DateTimeMin":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "Real":
                        {
                            if (defnInfo.Name == "Int") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }  // WARNING needed: truncation
                            if (defnInfo.Name == "Int64") { Assert.True(canCast == EFieldWriteErrorType.IsValid); } // WARNING needed: truncation
                            if (defnInfo.Name == "Real") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "RealBig":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); } // WARNING needed: truncation
                            if (defnInfo.Name == "Int64") { Assert.True(canCast == EFieldWriteErrorType.IsValid); } // WARNING needed: truncation
                            if (defnInfo.Name == "Real") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "String5":
                        {
                            if (defnInfo.Name == "Int") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "String":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "nullValue":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                    case "StringDateTime":
                        {
                            if (defnInfo.Name == "Int") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Int64") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "Real") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "DateTime") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String5") { Assert.False(canCast == EFieldWriteErrorType.IsValid); }
                            if (defnInfo.Name == "String") { Assert.True(canCast == EFieldWriteErrorType.IsValid); }

                            break;
                        }
                }
            }
        }


        [Theory]
        [MemberData(nameof(ValueData))]
        public void WriteValueToLayer_WithValidFiles(string valueName, object value)
        {

            var assemblyLocation = Path.GetDirectoryName(Assembly.GetAssembly(typeof(OgcCoreToolsLayerProcessorTests))
                .Location);
            var projectLocation = Directory.GetParent(assemblyLocation).Parent.Parent;
            var fileList = new List<string>
            {
                @$"{projectLocation}\samples-vector\UpdateAttributes_polygon_SpRefWgs84_epsg4326_1recs.shp",
                @$"{projectLocation}\samples-vector\samples_vector.gpkg",
                //@"E:\Projects\NetCore\GIS\GDALCoreTestApp\GdalCoreTest\samples-vector\Attributes.gdb"
            };

            var expectedLayerName = "UpdateAttributes_polygon_SpRefWgs84_epsg4326_1recs";

            foreach (var file in fileList)
            {
                string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "updateAttribute");
                var supportedDs = SupportedDatasource.GetSupportedDatasource(file);

                if (supportedDs.Access == EAccessLevel.Full)
                {
                    _outputHelper.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

                    var resultFile = new OgctDataSourceAccessor().CopyDatasource(file, outputdirectory, Path.GetFileName(file));

                    var dataSourceType = SupportedDatasource.GetSupportedDatasource(resultFile);

                    using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);

                    var layerNames = dataSource.GetLayerNames();

                    foreach (var layerName in layerNames)
                    {
                        if (layerName != expectedLayerName) continue;

                        using var layer = dataSource.OpenLayer(layerName);

                        var layerDetails = layer.LayerDetails;

                        var writeResults = new List<LayerAttributeWriteResult>();

                        foreach (var fieldDef in layerDetails.Schema.FieldList)
                        {
                            writeResults.Add(layer.WriteValue(fieldDef, value));
                        }

                        int errorCount = writeResults.Count(_ => _.IsValid == false);
                        int validCount = layerDetails.Schema.FieldList.Count - errorCount;

                        // shp
                        if (dataSourceType.Type == EDataSourceType.SHP)
                        {
                            switch (valueName)
                            {
                                case "Int":
                                    Assert.True(validCount == 9, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "String5":
                                    Assert.True(validCount == 9, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "String":
                                    Assert.True(validCount == 2, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "nullValue":
                                    Assert.True(validCount == 12, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "Int64":
                                    Assert.True(validCount == 7, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "Real":
                                    Assert.True(validCount == 6, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "RealBig":
                                    Assert.True(validCount == 5, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "DateTime":
                                    Assert.True(validCount == 5, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "DateTimeMin":
                                    Assert.True(validCount == 5, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "StringDateTime":
                                    Assert.True(validCount == 5, $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                            }
                        }

                        if (dataSourceType.Type == EDataSourceType.GPKG)
                        {

                            switch (valueName)
                            {
                                case "Int":
                                    Assert.True(validCount == 10,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "String5":
                                    Assert.True(validCount == 10,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "String":
                                    Assert.True(validCount == 2,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "nullValue":
                                    Assert.True(validCount == 14,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "Int64":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "Real":
                                    Assert.True(validCount == 6,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "RealBig":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "DateTime":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "DateTimeMin":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "StringDateTime":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                            }
                        }

                        if (dataSourceType.Type == EDataSourceType.OpenFGDB)
                        {

                            switch (valueName)
                            {
                                case "Int":
                                    Assert.True(validCount == 10,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "String5":
                                    Assert.True(validCount == 10,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "String":
                                    Assert.True(validCount == 2,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "nullValue":
                                    Assert.True(validCount == 3,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "Int64":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "Real":
                                    Assert.True(validCount == 6,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "RealBig":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "DateTime":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "DateTimeMin":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                                case "StringDateTime":
                                    Assert.True(validCount == 5,
                                        $" Valid count mismatch while updating value {valueName} in layer {layerName}");
                                    break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region remove field

        /// <summary>
        /// deletes fields in subdir "createdLayer"
        /// </summary>
        /// <param name="file"></param>
        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void RemoveFields_SelectedGPKG_IsWorking(string file)
        {

            string fieldNameToDelete = "ID_CH";

            string path = Path.Combine(Path.GetDirectoryName(file), "createdLayer");

            string createdFile = (Path.Combine(path, Path.GetFileName(file)));

            var supportedDatasource = SupportedDatasource.GetSupportedDatasource(createdFile);

            if (supportedDatasource.Access == EAccessLevel.ReadOnly)
                return;

            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);

            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);

                var layerDetails = layer.LayerDetails;

                Assert.NotNull(layerDetails);

                if (layerDetails.Schema.HasField(fieldNameToDelete) == false) return;

                var fieldToDelete = layerDetails.Schema.FieldList.First(_ => _.Name == fieldNameToDelete);



                layer.DeleteField(fieldToDelete);

                var newLayerDetails = layer.LayerDetails;

                Assert.True(layerDetails.Schema.FieldList.Count - 1 == newLayerDetails.Schema.FieldList.Count);
                Assert.True(newLayerDetails.Schema.HasField(fieldNameToDelete) == false);
            }
        }

        #endregion

        #region set attribute filter

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void SetAttributeFilter_AllRecords_SelectedRecords_IsWorking(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                if (layerName == "borders_polyline_SpRefNone_epsgNone_185rec")
                {

                    using var layer = dataSource.OpenLayer(layerName);
                    long allRecordsCount = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop(null);

                    Assert.True(allRecordsCount == 185, "whereClause null does not work");

                    long recordsSelected = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop("name = 'test'");

                    Assert.True(recordsSelected == 3, "whereClause 'name = 'test' does not select 3 records");

                    allRecordsCount = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop(String.Empty);

                    Assert.True(allRecordsCount == 185, "whereClause String.Empty does not work");

                    long zerorecordsSelected = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop("name = 'notfound'");

                    Assert.True(zerorecordsSelected == 0, "whereClause with no matches has more than 0 records");

                    allRecordsCount = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop("");

                    Assert.True(allRecordsCount == 185, "whereClause \"\" does not work");

                    Action action = () => layer.FilterByAttributeOnlyRespectedInNextFeatureLoop("noname = 'test'");

                    //ApplicationException exception = Assert.Throws<ApplicationException>(action);

                    //The thrown exception can be used for even more detailed assertions.
                    //Assert.Equal("\"noname\" not recognised as an available field.", exception.Message);

                }
            }
        }


        [Fact]
        public void BuildWhereClause_IsWorking()
        {
            var clauseCondition = new WhereClauseCondition();
            clauseCondition.AddField("ObjNummer", FieldType.OFTString, 40);
            clauseCondition.AddCompareSign(ECompareSign.IsEqual);
            clauseCondition.AddContent("BE102");


            var sqlResult = QueryHelpers.BuildWhereClause(new List<WhereClauseCondition> { clauseCondition });

            Assert.True("ObjNummer = 'BE102'".Equals(sqlResult), $"Build where clause fails on {sqlResult}");

            var condition2 = new WhereClauseCondition();
            condition2.AddField("Type", FieldType.OFTInteger, 40);
            condition2.AddCompareSign(ECompareSign.IsEqualOrLessThan);
            condition2.AddContent(4);

            sqlResult = QueryHelpers.BuildWhereClause(new List<WhereClauseCondition> { clauseCondition, condition2 });

            Assert.True("ObjNummer = 'BE102' AND Type <= 4".Equals(sqlResult), $"Build where clause fails on {sqlResult}");

        }


        #endregion

        #region read rows

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void ReadRows_WithValidFiles_IsWorking(string file)
        {
            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file);
            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var layerDetails = layer.LayerDetails;

                var rows = layer.ReadRows();

                Assert.NotNull(rows);

                Assert.True(rows.Count == layerDetails.FeatureCount, "mismatch in record counts");
                if (rows.Count > 0)
                {
                    Assert.True(rows[0].Items.Count == layerDetails.FieldCount, "mismatch in number of fields");
                }

            }
        }

        #endregion

        #region geoprocessing

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void Union_TwoValidFiles_IsWorking(string file)
        {
            if (file.EndsWith("GeoprocessingTestData.gpkg") == false) return;


            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);
            var layerNames = dataSource.GetLayerNames();


            // verify total area
            var layerAreas = new Dictionary<string, double>();
            layerAreas.Add("N1998_BLN", 780708.73355330108);   // ok
            layerAreas.Add("N2017_BLN", 783043.59907112434);   // 783751.1
            layerAreas.Add("N2014_JB", 148878.32640205475);    // 150888.9
            layerAreas.Add("N2015_ML", 87425.106131092);       // ok
            layerAreas.Add("N2017_ML", 86360.805180382013);    // 87499.17
            layerAreas.Add("N2015_WZVV", 22451.133143917814);  // 22769.99
            layerAreas.Add("N2017_AM", 21671.804790296854);    // 21682.3
            layerAreas.Add("N2017_AU", 27798.433292692578);    // 27847.7
            layerAreas.Add("N2017_FM", 21406.903809125408);    // 21419.7
            layerAreas.Add("N2017_HM", 1541.956301074041);     // 1567.6
            layerAreas.Add("N2021_FM", 22488.727398013274);    // 22501.5
            layerAreas.Add("N2021_TWW", 28260.765802130973);   // 28281.2
            layerAreas.Add("N2021_Park", 443.99573427868785);  // 


            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var areaHa = layer.CalculateArea() / 10000;

                if (layerName == "N1998_BLN")
                {
                    Assert.InRange(areaHa, 780708.71, 780708.74);
                }

                if (layerName == "N2015_ML")
                {
                    Assert.InRange(areaHa, 87425.1, 87425.2);
                }
                if (layerName == "N2017_BLN")
                {
                  //  Assert.InRange(areaHa, 783751.08, 783751.11);
                }
                if (layerName == "N2017_Revision_moorlandschaft_20171101Dissolve")
                {
                    Assert.InRange(areaHa, 87499.16, 87499.18);
                }
            }

            // Unify two
            using var layerOne = dataSource.OpenLayer("N1998_Serie4_landschaftnaturdenkmal_19980401Dissolve");
            using var layerSecond = dataSource.OpenLayer("N2015_Revision_moorlandschaft_20150301Dissolve");
            var outputLayerName = layerOne.GeoProcessWithLayer(EGeoProcess.Union, layerSecond, "ResultN1998BLN_N2015MLToBeDeleted");

            using var layerResult = dataSource.OpenLayer(outputLayerName);
            var areaHaResult = layerResult.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {outputLayerName} has {areaHaResult} ha area.");
            // 868133 ha
            // Assert.InRange(areaHaResult, 826647.5, 826647.6);

            // Unify other two
            using var layerThree = dataSource.OpenLayer("N2017_Revision_landschaftnaturdenkmal_20170727_20190111Dissolve");
            using var layerFour = dataSource.OpenLayer("N2017_Revision_moorlandschaft_20171101Dissolve");
            var outputSecondLayerName = layerThree.GeoProcessWithLayer(EGeoProcess.Union, layerFour, "ResultN2017BLN_N2017MLToBeDeleted");

            using var layerResultTwo = dataSource.OpenLayer(outputSecondLayerName);
            var areaHaResultTwo = layerResultTwo.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {outputSecondLayerName} has {areaHaResultTwo} ha area.");

            Assert.InRange(areaHaResultTwo, 829565.6, 829565.7);
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void Union_SevenValidLayers_IsWorking(string file)
        {
            if (file.EndsWith("Union7Layers.gdb") == false) return;


            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);
            var layerNames = dataSource.GetLayerNames();


            // verify total area
            var layerAreas = new Dictionary<string, double>();
            layerAreas.Add("N1991_Serie1_hochmoor_19910201Dissolve", 1469.765);  
            layerAreas.Add("N2000_Erweiterung_nationalpark_20000619Dissolve", 17032.958);   
            layerAreas.Add("N2001_Ergaenzung1_Auengebiete_20010801_20020403Dissolve", 20083.029);  
            layerAreas.Add("N2001_Revision_flachmoor_20010801Dissolve", 19184.560);      
            layerAreas.Add("N2001_Revision_jagdbann_20010101Dissolve", 149529.145);    
            layerAreas.Add("N2001_Revision_wasserzugvogel_20010801Dissolve", 18920.235);  
            layerAreas.Add("N2001_Serie1_amphibLaichgebietUndWanderobjekteUnion", 10911.347);


            // test for expected area of each layer
            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                var areaHa = layer.CalculateArea() / 10000;

                switch (layerName)
                {
                    case "N1991_Serie1_hochmoor_19910201Dissolve":
                        Assert.InRange(areaHa, 1469.76, 1469.77);
                        break;
                    case "N2000_Erweiterung_nationalpark_20000619Dissolve":
                        Assert.InRange(areaHa, 17032.95, 17032.96);
                        break;
                    case "N2001_Ergaenzung1_Auengebiete_20010801_20020403Dissolve":
                        Assert.InRange(areaHa, 20083.02, 20083.03);
                        break;
                    case "N2001_Revision_flachmoor_20010801Dissolve":
                        Assert.InRange(areaHa, 19184.555, 19184.570);
                        break;
                    case "N2001_Revision_jagdbann_20010101Dissolve":
                        Assert.InRange(areaHa, 149529.14, 149529.15);
                        break;
                    case "N2001_Revision_wasserzugvogel_20010801Dissolve":
                        Assert.InRange(areaHa, 18920.23, 18920.24);
                        break;
                    case "N2001_Serie1_amphibLaichgebietUndWanderobjekteUnion":
                        Assert.InRange(areaHa, 10911.34, 10911.35);
                        break;
                }
            }

            // Unify all layers: FM+AM+HM+AU+JB+WZ+NP

            using var layerOne = dataSource.OpenLayer("N2001_Revision_flachmoor_20010801Dissolve");
            using var layerSecond = dataSource.OpenLayer("N2001_Serie1_amphibLaichgebietUndWanderobjekteUnion");
            var tempLayerName1 = layerOne.GeoProcessWithLayer(EGeoProcess.Union, layerSecond, "N2001_FM_AM_UnionTemp");

            using var tempLayer1 = dataSource.OpenLayer(tempLayerName1);
            var areaHaResult1 = tempLayer1.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {tempLayerName1} has {areaHaResult1} ha area.");
            Assert.InRange(areaHaResult1, 28404.9, 28405);

            using var layerThree = dataSource.OpenLayer("N1991_Serie1_hochmoor_19910201Dissolve");
            var tempLayerName2 = tempLayer1.GeoProcessWithLayer(EGeoProcess.Union, layerThree, "N2001_FM_AM_HM_UnionTemp");
            
            using var tempLayer2 = dataSource.OpenLayer(tempLayerName2);
            var areaHaResult2 = tempLayer2.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {tempLayerName2} has {areaHaResult2} ha area.");
            Assert.InRange(areaHaResult2, 29723.450, 29723.6);

            using var layerFour = dataSource.OpenLayer("N2001_Ergaenzung1_Auengebiete_20010801_20020403Dissolve");
            var tempLayerName3 = tempLayer2.GeoProcessWithLayer(EGeoProcess.Union, layerFour, "N2001_FM_AM_HM_AU_UnionTemp");
            
            using var tempLayer3 = dataSource.OpenLayer(tempLayerName3);
            var areaHaResult3 = tempLayer3.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {tempLayerName3} has {areaHaResult3} ha area.");
            Assert.InRange(areaHaResult3, 46992.75, 46992.90);
        
            
            using var layerFive = dataSource.OpenLayer("N2001_Revision_jagdbann_20010101Dissolve");
            var tempLayerName4 = tempLayer3.GeoProcessWithLayer(EGeoProcess.Union, layerFive, "N2001_FM_AM_HM_AU_JB_UnionTemp");
            
            using var tempLayer4 = dataSource.OpenLayer(tempLayerName4);
            var areaHaResult4 = tempLayer4.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {tempLayerName4} has {areaHaResult4} ha area.");
            Assert.InRange(areaHaResult4, 194692.1, 194692.2);

            using var layerSix = dataSource.OpenLayer("N2001_Revision_wasserzugvogel_20010801Dissolve");
            var tempLayerName5 = tempLayer4.GeoProcessWithLayer(EGeoProcess.Union, layerSix, "N2001_FM_AM_HM_AU_JB_WZ_UnionTemp");
            
            using var tempLayer5 = dataSource.OpenLayer(tempLayerName5);
            var areaHaResult5 = tempLayer5.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {tempLayerName5} has {areaHaResult5} ha area.");
            Assert.InRange(areaHaResult5, 211043.82, 211043.85);

            using var layerSeven = dataSource.OpenLayer("N2000_Erweiterung_nationalpark_20000619Dissolve");
            var finalUnionLayerName = tempLayer5.GeoProcessWithLayer(EGeoProcess.Union, layerSeven, "N2001_FM_AM_HM_AU_JB_WZ_NP_UnionFinal");
            
            using var finalUnionLayer = dataSource.OpenLayer(finalUnionLayerName);
            var areaHaResult6 = finalUnionLayer.CalculateArea() / 10000;
            _outputHelper.WriteLine($"Layer= {finalUnionLayerName} has {areaHaResult6} ha area.");
            Assert.InRange(areaHaResult6, 228076.77, 228076.8);
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void UnionInGroup_SevenValidLayers_IsWorking(string file)
        {
            if (file.EndsWith("Union7Layers.gdb") == false) return;


            using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(file, EAccessLevel.Full);
            var layerNames = dataSource.GetLayerNames();



        }

        #endregion



    }
}

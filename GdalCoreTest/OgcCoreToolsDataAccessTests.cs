using System;
using System.Collections.Generic;
using System.IO;
using GdalCoreTest.Helper;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.DataAccess;
using OGCToolsNetCoreLib.Exceptions;
using OGCToolsNetCoreLib.Extensions;
using OSGeo.OGR;
using OSGeo.OSR;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsDataAccessTests : IClassFixture<CreateDataSourceFixture>
    {
        private readonly ITestOutputHelper _outputHelper;

        private string _gdbFolderToCleanupAfterTest;

        private CreateDataSourceFixture _fixture;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputHelper"></param>
        /// <param name="fixture">will cleanup all created files within the output folder at the end of all tests</param>
        public OgcCoreToolsDataAccessTests(ITestOutputHelper outputHelper, CreateDataSourceFixture fixture)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
            _fixture = fixture;
        }


        [Fact]
        public void CheckAllDriversAvailable_MoreThan100Drivers()
        {
            var cntDriver = new GeoDataSourceAccessor().GetAvailableDrivers().Count;
            Assert.True(cntDriver > 100);
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void OpenDataSourceVector_WithValidFiles_IsWorking(string file)
        {
            _outputHelper.WriteLine($"Datasource of file: {Path.GetFileName(file)}");

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

            _outputHelper.WriteLine($"CreateTest datasource of file: {Path.GetFileName(file)}");

            bool isExpectedOnReadOnly = SupportedDatasource.GetSupportedDatasource(file).Access == EAccessLevel.ReadOnly;

            if (isExpectedOnReadOnly)
            {
                Assert.Throws<DataSourceReadOnlyException>(() => new GeoDataSourceAccessor().CreateDatasource(file, null));
            }
            else
            {
                var spRef = ESpatialRefWKT.CH1903plus_LV95;

                var dataSource = new GeoDataSourceAccessor().CreateDatasource(file, new SpatialReference(spRef.GetEnumDescription(typeof(ESpatialRefWKT))), wkbGeometryType.wkbPolygon);
                Assert.NotNull(dataSource);
            }
        }


        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void CopyDataSourceVector_WithValidFiles_IsWorking(string file)
        {
            string outputdirectory = Path.Combine(Path.GetDirectoryName(file), "copied");

            _outputHelper.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

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

            _outputHelper.WriteLine($"Copy datasource of file: {Path.GetFileName(file)}");

            var resultFile = new GeoDataSourceAccessor().CopyDatasource(file, outputdirectory, Path.GetFileName(file));

            new GeoDataSourceAccessor().DeleteDatasource(resultFile);

            bool isExpectedAsFile = SupportedDatasource.GetSupportedDatasource(resultFile).FileType == EFileType.File;

            Assert.False(isExpectedAsFile ? File.Exists(resultFile) : Directory.Exists(resultFile));
        }


    }
}

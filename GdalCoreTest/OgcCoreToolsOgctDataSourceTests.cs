using System;
using System.IO;
using GdalCoreTest.Helper;
using OGCToolsNetCoreLib.DataAccess;
using Xunit;
using Xunit.Abstractions;

namespace GdalCoreTest
{
    [Collection("Sequential")]
    public class OgcCoreToolsOgctDataSourceTests
    {

        private readonly ITestOutputHelper _outputHelper;

        public OgcCoreToolsOgctDataSourceTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GdalConfiguration.ConfigureGdal();
        }

        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void RenameLayerGpkg_WithValidFiles_IsWorking(string file)
        {
            if (SupportedDatasource.GetSupportedDatasource(file).Type != EDataSourceType.GPKG)
            {
                return;
            }

            _outputHelper.WriteLine($"Rename layer in datasource (file): {Path.GetFileName(file)}");

            string layerName = String.Empty;

            using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(file, true))
            {

                var layernames = dataSource.GetLayerNames();

                // 1. copy 1. layer with to new layer with appendix  "copy"
                // check if exists
                // 2. rename 1. layer with with appendix  "ToBeDeleted"
                // check if exists
                // 3. rename copied layer back to the name of the 1. layer
                // check if exists
                layerName = layernames[0];

                using var layer = dataSource.OpenLayer(layerName);
                {
                    layer.CopyToLayer(dataSource, $"{layerName}Copy");
                }

                Assert.True(dataSource.HasLayer($"{layerName}Copy"));
                Assert.True(dataSource.HasLayer(layerName));

                dataSource.RenameLayerGpkg(layerName, $"{layerName}ToBeDeleted");

            }

            using (var dataSourceReopened = new GeoDataSourceAccessor().OpenDatasource(file, true))
            {
                Assert.True(dataSourceReopened.HasLayer($"{layerName}ToBeDeleted"));
                Assert.False(dataSourceReopened.HasLayer(layerName));

                // asserts will be succeful, althpugh they don't reflect the changes in den database
                // This because the datasource dosn't recongnize changes in layer names without being re-opend
                dataSourceReopened.RenameLayerGpkg($"{layerName}Copy", layerName);
                Assert.False(dataSourceReopened.HasLayer(layerName));
                Assert.True(dataSourceReopened.HasLayer($"{layerName}Copy"));
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

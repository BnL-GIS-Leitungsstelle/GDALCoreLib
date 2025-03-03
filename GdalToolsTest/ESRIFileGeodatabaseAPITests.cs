using ESRIFileGeodatabaseAPI;
using GdalToolsLib.Common;
using GdalToolsLib.Models;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GdalToolsTest
{
    public class ESRIFileGeodatabaseAPITests : IDisposable
    {
        DirectoryInfo tempDir;
        string testDbPath;

        public ESRIFileGeodatabaseAPITests()
        {
            tempDir = Directory.CreateTempSubdirectory();
            testDbPath = Path.Join(tempDir.FullName, "test.gdb");
        }

        [Theory]
        [InlineData("Wasserstraßenabschnitte")]
        public void WritingWithESRIDriverShouldWork(string testMetadata)
        {
            var layerName = "test";

            using (var testDs = new OgctDataSourceAccessor().OpenOrCreateDatasource(testDbPath, GdalToolsLib.DataAccess.EAccessLevel.Full, true))
            {
                testDs.CreateAndOpenLayer(layerName, ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon).Dispose();
            }

            FGDBMetadataWriter.WriteLayerMetadata(testDbPath, layerName, testMetadata);

            using (var testDs = new OgctDataSourceAccessor().OpenOrCreateDatasource(testDbPath))
            {
                var readMetadata = testDs.ExecuteSqlFgdbGetLayerMetadata(layerName);

                Assert.Equal(testMetadata, readMetadata);
            }
        }

        [Theory]
        [InlineData("Wasserstraßenabschnitte")]
        public void WritingWithGDALShouldNotWork(string testMetadata)
        {
            var layerName = "test";

            using var testDs = new OgctDataSourceAccessor().OpenOrCreateDatasource(testDbPath, GdalToolsLib.DataAccess.EAccessLevel.Full, true);
            testDs.CreateAndOpenLayer(layerName, ESpatialRefWkt.CH1903plus_LV95, wkbGeometryType.wkbPolygon, documentation: testMetadata).Dispose();

            var readMetadata = testDs.ExecuteSqlFgdbGetLayerMetadata(layerName);
            Assert.NotEqual(testMetadata, readMetadata);
        }

        public void Dispose()
        {
            tempDir.Delete(true);
        }
    }
}

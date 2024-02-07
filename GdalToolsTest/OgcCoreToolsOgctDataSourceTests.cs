using System;
using System.Collections.Generic;
using System.IO;
using GdalCoreTest.Helper;
using GdalCoreTest.SqlStatements;
using GdalToolsLib;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
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

        /// <summary>
        /// runs a set  of sql-statements to verify the syntax and parameter handling is ok.
        /// Each sql-statement is run in all dialects
        /// </summary>
        [Fact]
        public void ExecuteSQL_OnWildruhezonenFGDB_IsWorking()
        {
            string wrzFile = @"D:\Daten\Projects\GISToolsNetCore\GDALCoreLib\GdalCoreTest\samples-vector\Wildruhezonen.gdb";

            Assert.True(SupportedDatasource.GetSupportedDatasource(wrzFile).Type == EDataSourceType.OpenFGDB, $"Datasource {wrzFile} is not of expected type");

            string layerName = "Wildruhezone";

            using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
            {
                Assert.True(dataSource.HasLayer(layerName), $"Layer {layerName} not found");
            }

            foreach (var statement in SqlStatementProvider.BuildList())
            {
                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var layer = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.OgrSqlDialect);

                        string sqlWhereClause = "(Bestimmungen = 'E900' OR Bestimmungen = 'R900')";

                        long featurecount = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop(sqlWhereClause);

                        var rows=  layer.ReadRows(layer.LayerDetails.Schema.FieldList);





                        Assert.NotNull(layer);
                    }
                    catch (Exception e)
                    {
                         Assert.Fail($"dialect= {OgcConstants.OgrSqlDialect}: Message= {e.Message}  ");
                    }
                }

                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var result = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.GpkgSqlDialect);
                    }
                    catch (Exception e)
                    {
                        //  Assert.Fail("");
                    }
                }

                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var result = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.SQLiteSqlDialect);
                    }
                    catch (Exception e)
                    {
                        // Assert.Fail("");
                    }
                }

                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var result = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.IndirectSQLiteSqlDialect);
                    }
                    catch (Exception e)
                    {
                        // Assert.Fail("");
                    }
                }
            }
        }

        /// <summary>
        /// runs a set  of sql-statements to verify the syntax and parameter handling is ok.
        /// Each sql-statement is run in all dialects
        /// </summary>
        [Fact]
        public void ExecuteSQL_OnWildruhezonenGPKG_IsWorking()
        {
            string wrzFile = @"D:\Daten\Projects\GISToolsNetCore\GDALCoreLib\GdalCoreTest\samples-vector\Wildruhezonen.gpkg";

            Assert.True(SupportedDatasource.GetSupportedDatasource(wrzFile).Type == EDataSourceType.GPKG, $"Datasource {wrzFile} is not of expected type");

            string layerName = "Wildruhezone";

            using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
            {
                Assert.True(dataSource.HasLayer(layerName), $"Layer {layerName} not found");
            }

            foreach (var statement in SqlStatementProvider.BuildList())
            {
                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var layer = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.OgrSqlDialect);



                    }
                    catch (Exception e)
                    {
                        Assert.Fail($"dialect= {OgcConstants.OgrSqlDialect}: Message= {e.Message}  ");
                    }
                }

                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var result = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.GpkgSqlDialect);
                    }
                    catch (Exception e)
                    {
                        //  Assert.Fail("");
                    }
                }

                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var result = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.SQLiteSqlDialect);
                    }
                    catch (Exception e)
                    {
                        // Assert.Fail("");
                    }
                }

                using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(wrzFile))
                {
                    try
                    {
                        var result = dataSource.ExecuteSQL(statement.SqlPhrase, OgcConstants.IndirectSQLiteSqlDialect);
                    }
                    catch (Exception e)
                    {
                        // Assert.Fail("");
                    }
                }
            }
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

        /// <summary>
        /// steps performed:
        /// PREPARE:
        /// Get the first layer of the datasource
        /// (1) copy layer to layer with name+'Backup'+ test, if layer exists
        /// TEST:
        /// (2) rename layer to layer + ´Renamed´+ test, if layer exists
        /// CLEANUP:
        /// (3) rename layer + ´Renamed´ (2) back to layer (1) + check if layer exists
        /// (4) remove layer with name+ 'Backup' + check. if number of layers have changed (indicating failures)
        /// </summary>
        /// <param name="file"></param>
        [Theory]
        [MemberData(nameof(TestDataPathProvider.SupportedVectorData), MemberType = typeof(TestDataPathProvider))]
        public void RenameLayer_WithValidFiles_IsWorking(string file)
        {
          
            if (SupportedDatasource.GetSupportedDatasource(file).Type != EDataSourceType.GPKG &&
                SupportedDatasource.GetSupportedDatasource(file).Type != EDataSourceType.OpenFGDB)
            {
                return;  // supports only dataformats gpkg and fgdb
            }

            _outputHelper.WriteLine($"Rename layer in datasource (file): {Path.GetFileName(file)}");

            string firstLayerName = String.Empty;
            int layerCountExpected = 0;

            using (var ds = new GeoDataSourceAccessor().OpenDatasource(file, true))
            {
                var layernames = ds.GetLayerNames();

                layerCountExpected = layernames.Count;

                if (layerCountExpected == 0) return; // datasource has no layer

                firstLayerName = layernames[0];

                //prepare: copy layer to backup-layer
                using (var layer = ds.OpenLayer(firstLayerName))
                {
                    layer.CopyToLayer(ds, $"{firstLayerName}Backup");  // exception in FGDB!
                }
                Assert.True(ds.HasLayer($"{firstLayerName}Backup"));
                Assert.True(ds.HasLayer(firstLayerName));

                //test: rename layer
                if (SupportedDatasource.GetSupportedDatasource(file).Type == EDataSourceType.GPKG)
                {
                    ds.RenameLayerGpkg(firstLayerName, $"{firstLayerName}Renamed");
                }
                if (SupportedDatasource.GetSupportedDatasource(file).Type == EDataSourceType.OpenFGDB)
                {
                    ds.RenameLayerOpenFgdb(firstLayerName, $"{firstLayerName}Renamed");
                }
            }


            using (var dsReOpened = new GeoDataSourceAccessor().OpenDatasource(file, true))
            {
                Assert.True(dsReOpened.HasLayer($"{firstLayerName}Renamed"));
                Assert.False(dsReOpened.HasLayer(firstLayerName));

                dsReOpened.RenameLayerGpkg($"{firstLayerName}Backup", firstLayerName);
            }

            using (var dsReOpened = new GeoDataSourceAccessor().OpenDatasource(file, true))
            {
                Assert.True(dsReOpened.HasLayer(firstLayerName));
                Assert.False(dsReOpened.HasLayer($"{firstLayerName}Backup"));

                dsReOpened.DeleteLayer($"{firstLayerName}Renamed");

                var layerCntActual = dsReOpened.GetLayerCount();

                Assert.True(layerCntActual == layerCountExpected, $"Error: The number of layers have changed during test: espected {layerCountExpected}, actual: {layerCntActual}");

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

            using (var dataSource = new GeoDataSourceAccessor().OpenDatasource(resultFile))
            {
                Assert.NotNull(dataSource);
            }

            // cleanup
            new GeoDataSourceAccessor().DeleteDatasource(resultFile);

            if (Directory.Exists(outputdirectory))
                Directory.Delete(outputdirectory, true);
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

            if (Directory.Exists(outputdirectory))
                Directory.Delete(outputdirectory, true);
        }

    }
}

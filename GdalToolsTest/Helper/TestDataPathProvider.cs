using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OGCToolsNetCoreLib.DataAccess;

namespace GdalCoreTest.Helper
{

    /// <summary>
    /// provides the path to the testdata
    /// </summary>
    public static class TestDataPathProvider
    {

        public const string TestDataFolderRaster = "samples-raster";

        public const string TestDataFolderVector = "samples-vector";
        public const string TestDataFolderSpecific = "testdata";


        public static IEnumerable<object[]> SupportedVectorData()
        {
            var names = new List<object>();

            foreach (var item in SupportedDatasource.Datasources)
            {
                if (item.Type == EDataSourceType.SHP_FOLDER)
                {
                    names.AddRange(Directory.EnumerateDirectories(GetTestDataFolder(TestDataFolderVector), $"*.shape").ToList());
                }
                else
                {
                    names.AddRange(Directory
                        .EnumerateFiles(GetTestDataFolder(TestDataFolderVector), $"*{item.Extension}").ToList());
                    names.AddRange(Directory
                        .EnumerateDirectories(GetTestDataFolder(TestDataFolderVector), $"*{item.Extension}").ToList());
                }
            }
            return names.Select(x => new[] { x });
        }




        public static IEnumerable<object[]> ValidTifRasterData()
        {
            var names = Directory.EnumerateFiles(GetTestDataFolder(TestDataFolderRaster), "*_valid.tif");
            return names.Select(x => new[] { x });
        }


        public static IEnumerable<object[]> ValidShapeVectorData()
        {
            var names = Directory.EnumerateDirectories(GetTestDataFolder(TestDataFolderVector), "*.shape");
            return names.Select(x => new[] { x });
        }


        public static IEnumerable<object[]> FileNameInterpreter()
        {
            var files = Directory.EnumerateFiles(GetTestDataFolder(TestDataFolderVector), "*.*", SearchOption.AllDirectories)
                     .Where(s => s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)). ToList();

            return files.Select(x => new[] { x });
        }




        public static string GetTestDataFolder(string testDataFolder)
        {
            var startupPath = AppContext.BaseDirectory;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            var projectPath = string.Join(Path.DirectorySeparatorChar.ToString(),
                pathItems.Take(pathItems.Length - pos - 1));
            return Path.Combine(projectPath, testDataFolder);
        }

    }




}

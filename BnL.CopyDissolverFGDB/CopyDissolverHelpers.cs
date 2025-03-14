using GdalToolsLib.DataAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BnL.CopyDissolverFGDB
{
    public static class CopyDissolverHelpers
    {
        public static IEnumerable<string[]> GetLinesWithoutComments(string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception($"File '{filePath}' not found");

            using var fileStream = File.OpenRead(filePath);
            using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128);

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine()!;
                if (!line.StartsWith("//"))
                {
                    yield return line.Split(';');
                }
            }
        }

        /// <summary>
        /// Collects fgdb-files from a starting directory
        /// </summary>
        /// <returns>list of gdbs</returns>
        public static List<string> CollectGeodataFiles(string path, int depth = 2)
        {
            int startLevel = path.Split('\\').Length;
            int targetLevel = startLevel + depth;

            var supportedDataSource = SupportedDatasource.GetSupportedDatasource(EDataSourceType.OpenFGDB);

            return Directory.GetDirectories(path, "*" + supportedDataSource.Extension, SearchOption.AllDirectories)
                .Where(folder => folder.Split('\\').Length <= targetLevel).ToList();

        }
    }
}

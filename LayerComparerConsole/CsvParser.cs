using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LayerComparerConsole
{
    public static class CsvParser
    {
        private static CsvConfiguration _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true
        };

        public static List<T> ParseRecords<T>(string csvPath)
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, _csvConfig);

            return csv
                .GetRecords<T>()
                .ToList();
        }
    }
}

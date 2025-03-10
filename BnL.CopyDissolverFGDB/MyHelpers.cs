using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnL.CopyDissolverFGDB
{
    public static class MyHelpers
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
    }
}

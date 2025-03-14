using BnL.CopyDissolverFGDB.Parameters;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Models;
using GdalToolsLib.VectorTranslate;
using OSGeo.GDAL;
using OSGeo.OGR;
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


        public static void ProcessFGDB(string sourceGdbPath, string workDir, string[] dissolveFieldNames, IEnumerable<FilterParameter> filterParameters, IEnumerable<BufferParameter> bufferParameters, IEnumerable<UnionParameter> unionParameters)
        {
            using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(sourceGdbPath, EAccessLevel.ReadOnly);

            var layers = ds.LayerIterator()
                            .Where(l => l
                                .LayerDetails
                                .Schema!
                                .FieldList
                                .Count(f => dissolveFieldNames.Contains(f.Name)) == dissolveFieldNames.Length
                            )
                            .Select(l => new WorkLayer(l.LayerDetails))
                            .ToList();

            var outGdb = Path.Join(workDir, Path.GetFileName(sourceGdbPath));
            var workGdb = "/vsimem/" + outGdb;

            foreach (var layer in layers)
            {
                // filter layers according to csv
                var filter = filterParameters.SingleOrDefault(p => layer.LayerContentInfo.Year == int.Parse(p.Year)
                                                          && layer.LayerContentInfo.Category.Equals(p.Theme, StringComparison.InvariantCultureIgnoreCase));

                VectorTranslate.Run(sourceGdbPath, workGdb, new VectorTranslateOptions
                {
                    Overwrite = true,
                    Update = true,
                    Where = filter?.WhereClause,
                    SourceLayerName = layer.CurrentLayerName,
                    // removes other dimensions than XY
                    OtherOptions = ["-dim", "XY"]
                });
                layer.DataSourcePath = workGdb;

                // removes the M and Z from the geometry type
                layer.GeometryType = Ogr.GT_Flatten(layer.GeometryType);

                var buffer = bufferParameters.SingleOrDefault(b => layer.LayerContentInfo.LegalState.Contains(b.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
                                                       layer.LayerContentInfo.Category.Contains(b.Theme, StringComparison.CurrentCultureIgnoreCase));

                var dissolveFieldsString = string.Join(", ", dissolveFieldNames);

                if (buffer != null)
                {
                    var sqlStatement = $"SELECT {dissolveFieldsString}, ST_Buffer(SHAPE, {buffer.BufferDistanceMeter}) as SHAPE FROM '{layer.CurrentLayerName}'";

                    VectorTranslate.Run(workGdb, workGdb, new VectorTranslateOptions
                    {
                        Overwrite = true,
                        Update = true,
                        SourceLayerName = layer.CurrentLayerName,
                        NewLayerName = layer.CurrentLayerName += "_buf",
                        Sql = sqlStatement,
                        NewGeometryType = layer.GeometryType = wkbGeometryType.wkbPolygon,
                        OtherOptions = ["-dialect", "SQLITE"]
                    });
                }

                var dissolveSql = $"""
        SELECT {dissolveFieldsString}, ST_Multi(ST_Union(SHAPE)) as SHAPE
        FROM '{layer.CurrentLayerName}'
        GROUP BY {dissolveFieldsString}
        """;
                VectorTranslate.Run(workGdb, workGdb, new VectorTranslateOptions
                {
                    SourceLayerName = layer.CurrentLayerName,
                    NewLayerName = layer.CurrentLayerName += "_dis",
                    Overwrite = true,
                    Update = true,
                    Sql = dissolveSql,
                    NewGeometryType = layer.GeometryType.ToMulti(),
                    OtherOptions = ["-dialect", "SQLITE"]
                });
            }

            foreach (var unionGroup in unionParameters.GroupBy(up => up.ResultLayerName))
            {
                var combinedName = unionGroup.Key;
                var layerToUnion = unionGroup.Select(ul => layers.SingleOrDefault(l =>
                            l.LayerContentInfo.Year == Convert.ToInt32(ul.Year) &&
                            l.LayerContentInfo.LegalState.Contains(ul.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
                            l.LayerContentInfo.SubCategory.StartsWith("Anhang") == false &&
                            l.LayerContentInfo.Category.Contains(ul.Theme, StringComparison.CurrentCultureIgnoreCase)))
                    .Where(x => x != null);
                if (!layerToUnion.Any()) continue;

                var workLayer1 = layerToUnion.ElementAt(0);
                var workLayer2 = layerToUnion.ElementAt(1);

                // new OgctDataSourceAccessor().CreateAndOpenDatasource() does not work wiha /vsimem/ path...
                using var workDs = new OgctDataSource(Ogr.Open(workGdb, GdalConst.GA_Update));

                using var layer = workDs.OpenLayer(workLayer1.CurrentLayerName);
                using var otherLayer = workDs.OpenLayer(workLayer2.CurrentLayerName);

                using var unionResultLayer = workDs.CreateAndOpenLayer(combinedName, layer.GetSpatialRef(), layer.LayerDetails.GeomType, overwriteExisting: false);

                //Console.Write($" -- > Unify areas from {layer.Name} and {otherLayer.Name} ");

                layer.GeoProcessWithLayer(EGeoProcess.Union, otherLayer, unionResultLayer);

                workLayer1.OutputLayerName = workLayer2.OutputLayerName = combinedName;

                //Console.WriteLine($" into {outputLayerName}.");
            }

            var copiedLayers = new List<string>();
            foreach (var layer in layers)
            {
                if (copiedLayers.Contains(layer.OutputLayerName)) continue;
                VectorTranslate.Run(workGdb, outGdb, new VectorTranslateOptions { SourceLayerName = layer.CurrentLayerName, NewLayerName = layer.OutputLayerName, Overwrite = true, Update = true });
                copiedLayers.Add(layer.OutputLayerName);
            }
        }
    }
}

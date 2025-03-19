using BnL.CopyDissolverFGDB.Parameters;
using ESRIFileGeodatabaseAPI;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using GdalToolsLib.VectorTranslate;
using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BnL.CopyDissolverFGDB
{
    class FGDBProcessor
    {
        public readonly string sourceGdbPath;
        private readonly string dissolveFieldsString;

        public List<WorkLayer> workLayers = [];
        private string workDb;

        public List<string> layersWithoutDissolveFields = [];
        public List<string> nonPointBufferLayers = [];
        public List<string> zMGeometryLayers = [];
        public bool HasWarnings => layersWithoutDissolveFields.Count > 0 || nonPointBufferLayers.Count > 0;

        public FGDBProcessor(
            string sourceGdbPath,
            string[] dissolveFieldNames,
            IEnumerable<FilterParameter> filterParameters,
            IEnumerable<BufferParameter> bufferParameters,
            IEnumerable<UnionParameter> unionParameters,
            (string, string)[] renamePatterns)
        {
            this.sourceGdbPath = sourceGdbPath;
            this.dissolveFieldsString = string.Join(", ", dissolveFieldNames);

            using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(sourceGdbPath, EAccessLevel.ReadOnly);

            foreach (var l in ds.GetLayers())
            {
                var hasDissolveFields = l
                                .LayerDetails
                                .Schema!
                                .FieldList
                                .Count(f => dissolveFieldNames.Contains(f.Name)) == dissolveFieldNames.Length;

                if (!hasDissolveFields)
                {
                    layersWithoutDissolveFields.Add(l.Name);
                    workLayers.Add(new WorkLayer(l.Name, l.Name, wkbGeometryType.wkbUnknown));
                    continue;
                }
                if ((Ogr.GT_HasZ(l.LayerDetails.GeomType) + Ogr.GT_HasM(l.LayerDetails.GeomType)) != 0)
                {
                    zMGeometryLayers.Add(l.Name);
                }

                var layerNameMetadata = new LayerNameBafuContent(l.Name);

                var filter = filterParameters.SingleOrDefault(p => layerNameMetadata.Year == p.Year
                                                          && layerNameMetadata.Category.Equals(p.Theme, StringComparison.InvariantCultureIgnoreCase));

                var buffer = bufferParameters.SingleOrDefault(b => layerNameMetadata.LegalState.Contains(b.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
                                                     layerNameMetadata.Category.Contains(b.Theme, StringComparison.CurrentCultureIgnoreCase));

                if (buffer != null && l.LayerDetails.GeomType is not wkbGeometryType.wkbPoint or wkbGeometryType.wkbMultiPoint)
                {
                    nonPointBufferLayers.Add(l.Name);
                }

                var union = unionParameters.SingleOrDefault(ul =>
                            layerNameMetadata.Year == ul.Year &&
                            layerNameMetadata.LegalState.Contains(ul.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
                            layerNameMetadata.SubCategory.StartsWith("Anhang") == false &&
                            layerNameMetadata.Category.Contains(ul.Theme, StringComparison.CurrentCultureIgnoreCase));

                var outputName = union != null ? union.ResultLayerName : l.Name;

                foreach (var (oldStr, newStr) in renamePatterns)
                {
                    outputName = outputName.Replace(oldStr, newStr);
                }
                workLayers.Add(new WorkLayer(l.Name, outputName, l.LayerDetails.GeomType, filter, buffer, true));
            }
        }

        public void Run(string destination)
        {
            workDb = "/vsimem/" + destination;

            foreach (var layer in workLayers)
            {
                // copy features from source into work Db, directly applying filter if present
                VectorTranslate.Run(sourceGdbPath, workDb, new VectorTranslateOptions
                {
                    Overwrite = true,
                    Update = true,
                    Where = layer.Filter?.WhereClause,
                    SourceLayerName = layer.CurrentLayerName,
                    MakeValid = true,
                    // removes dimensions other than XY
                    OtherOptions = ["-dim", "XY"]
                });
                // removes the M and Z from the geometry type in the worklayer
                layer.GeometryType = Ogr.GT_Flatten(layer.GeometryType);

                if (layer.Buffer != null) BufferLayer(layer);

                if (layer.Dissolve) DissolveLayer(layer);
            }

            // process unions in separate loop, since all layers have to be dissolved first
            foreach (var group in workLayers.GroupBy(l => l.OutputLayerName))
            {
                var layer = group.ElementAt(0);

                if (group.Count() > 2)
                {
                    throw new Exception("Trying to union more than two layers, which is not supported");
                }
                else if (group.Count() == 2)
                {
                    var combinedName = group.Key;

                    var otherLayer = group.ElementAt(1);

                    UnionLayers(layer, otherLayer, combinedName);
                }

                // Copy finished layer to output datasource
                VectorTranslate.Run(workDb, destination, new VectorTranslateOptions
                {
                    SourceLayerName = layer.CurrentLayerName,
                    NewLayerName = layer.OutputLayerName,
                    Overwrite = true,
                    Update = true
                });
            }

            FGDBMetadataWriter.CopyMetadataForAllLayers(sourceGdbPath, destination);
        }

        private void UnionLayers(WorkLayer workLayer1, WorkLayer workLayer2, string combinedName)
        {
            // new OgctDataSourceAccessor().CreateAndOpenDatasource() does not work with a /vsimem/ path...
            using var workDs = new OgctDataSource(Ogr.Open(workDb, GdalConst.GA_Update));

            using var layer = workDs.OpenLayer(workLayer1.CurrentLayerName);
            using var otherLayer = workDs.OpenLayer(workLayer2.CurrentLayerName);

            //Console.Write($" -- > Unify areas from {layer.Name} and {otherLayer.Name} ");
            var outName = layer.GeoProcessWithLayer(EGeoProcess.Union, otherLayer, combinedName)!;
            workLayer1.CurrentLayerName = workLayer2.CurrentLayerName = outName;
        }

        private void DissolveLayer(WorkLayer layer)
        {
            var dissolveSql = $"""
                    SELECT {dissolveFieldsString}, ST_Multi(ST_Union(SHAPE)) as SHAPE
                    FROM '{layer.CurrentLayerName}'
                    GROUP BY {dissolveFieldsString}
                """;

            VectorTranslate.Run(workDb, workDb, new VectorTranslateOptions
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

        private void BufferLayer(WorkLayer layer)
        {
            var sqlStatement = $"SELECT {dissolveFieldsString}, ST_Buffer(SHAPE, {layer.Buffer!.BufferDistanceMeter}) as SHAPE FROM '{layer.CurrentLayerName}'";

            VectorTranslate.Run(workDb, workDb, new VectorTranslateOptions
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

    }
}

using BnL.CopyDissolverFGDB;
using BnL.CopyDissolverFGDB.Parameters;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Models;
using GdalToolsLib.VectorTranslate;
using NetTopologySuite.Index.HPRtree;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

Console.WriteLine("Bonjour...");

var workDir = "D:\\Daten\\MMO\\temp\\CopyDissolverTest";
var gdbPath = @"G:\BnL\Daten\Ablage\DNL\Bundesinventare\Jagdbanngebiete\Jagdbanngebiete.gdb";
//var gdbPath = @"G:\BnL\Daten\Ablage\DNL\Bundesinventare\AmphibienIANB\IANB.gdb";
string[] dissolveFieldNames = ["ObjNummer", "Name"];

var filterParameters = MyHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\filters.txt")
                                .Select(line => new FilterParameter(line));
var bufferParameters = MyHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\buffers.txt")
                                .Select(line => new BufferParameter(line));

var unionParameters = MyHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\unions.txt")
                                .Select(line => new UnionParameterLayer(line));

using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(gdbPath, GdalToolsLib.DataAccess.EAccessLevel.ReadOnly);

var layers = ds.LayerIterator()
                .Where(l => l
                    .LayerDetails
                    .Schema!
                    .FieldList
                    .Count(f => dissolveFieldNames.Contains(f.Name)) == dissolveFieldNames.Length
                )
                .Select(l => new WorkLayer(l.LayerDetails))
                .ToList();

var layersToDissolve = new List<WorkLayer>();

foreach (var layer in layers)
{
    // filter layers according to csv
    var filter = filterParameters.SingleOrDefault(p => layer.LayerContentInfo.Year == int.Parse(p.Year)
                                              && layer.LayerContentInfo.Category.Equals(p.Theme, StringComparison.InvariantCultureIgnoreCase));

    var destination = Path.Join(workDir, Path.GetFileName(gdbPath));
    VectorTranslate.Run(gdbPath, destination, new VectorTranslateOptions { Overwrite = true, Where = filter?.WhereClause, SourceLayerName = layer.CurrentLayerName, Update = true });


    var buffer = bufferParameters.SingleOrDefault(b => layer.LayerContentInfo.LegalState.Contains(b.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
                                           layer.LayerContentInfo.Category.Contains(b.Theme, StringComparison.CurrentCultureIgnoreCase));

    var dissolveFieldsString = string.Join(", ", dissolveFieldNames);

    if (buffer != null)
    {
        var sqlStatement = $"SELECT {dissolveFieldsString}, ST_Buffer(SHAPE, {buffer.BufferDistanceMeter}) as SHAPE FROM '{layer.CurrentLayerName}'";


        VectorTranslate.Run(destination, destination, new VectorTranslateOptions
        {
            Overwrite = true,
            Update = true,
            SourceLayerName = layer.CurrentLayerName,
            NewLayerName = layer.CurrentLayerName += "_buf",
            Sql = sqlStatement,
            OtherOptions = ["-dialect", "SQLITE", "-nlt", "POLYGON"]
        });
    }

    var dissolveSql = $"""
        SELECT {dissolveFieldsString}, ST_Multi(ST_Union(SHAPE)) as SHAPE
        FROM '{layer.CurrentLayerName}'
        GROUP BY {dissolveFieldsString}
        """;
    VectorTranslate.Run(destination, destination, new VectorTranslateOptions
    {
        SourceLayerName = layer.CurrentLayerName,
        NewLayerName = layer.CurrentLayerName += "_dis",
        Overwrite = true,
        Update = true,
        Sql = dissolveSql,
        OtherOptions = ["-dialect", "SQLITE", "-nlt", "PROMOTE_TO_MULTI"]
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

    var layer1 = layerToUnion.ElementAt(0);
    var layer2 = layerToUnion.ElementAt(1);

    using var ds1 = new OgctDataSourceAccessor().OpenOrCreateDatasource(layer1.DataSourcePath);
    using var ds2 = new OgctDataSourceAccessor().OpenOrCreateDatasource(layer2.DataSourcePath);
    using var layer = ds1.OpenLayer(layer1.CurrentLayerName);  // open layer

    using var otherLayer = ds2.OpenLayer(layer2.CurrentLayerName);

    Console.Write($" -- > Unify areas from {layer.Name} and {otherLayer.Name} ");

    var outputLayerName = layer.GeoProcessWithLayer(EGeoProcess.Union, otherLayer, combinedName);

    Console.WriteLine($" into {outputLayerName}.");
}


Console.WriteLine("Finished...");
Console.ReadKey();

using BnL.CopyDissolverFGDB;
using BnL.CopyDissolverFGDB.Parameters;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using GdalToolsLib.VectorTranslate;
using OSGeo.GDAL;
using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

Console.WriteLine("Bonjour");

var workDir = "D:\\Daten\\MMO\\temp\\CopyDissolverTest";
//var gdbPath = @"G:\BnL\Daten\Ablage\DNL\Bundesinventare\Jagdbanngebiete\Jagdbanngebiete.gdb";
var gdbPath = @"G:\BnL\Daten\Ablage\DNL\Bundesinventare\AmphibienIANB\IANB.gdb";
string[] dissolveFieldNames = ["ObjNummer", "Name"];

var filterParameters = MyHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\filters.txt")
                                .Select(line => new FilterParameter(line));
var bufferParameters = MyHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\buffers.txt")
                                .Select(line => new BufferParameter(line));

using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(gdbPath, GdalToolsLib.DataAccess.EAccessLevel.ReadOnly);

var layers = ds.LayerIterator().Select(l => new WorkLayer(l)).ToList();

// get the layers that don't have the dissolve fields
var layersToIgnore = layers.Where(l => l
    .OgcLayer
    .LayerDetails
    .Schema!
    .FieldList
    .Count(f => dissolveFieldNames.Contains(f.Name)) != dissolveFieldNames.Length
);

var layersToDissolve = new List<WorkLayer>();
foreach (var layer in layers)
{
    // skip layers that can be ignored
    if (layersToIgnore.Contains(layer))
    {
        continue;
    }


    // filter layers according to csv
    var filter = filterParameters.SingleOrDefault(p => layer.LayerContentInfo.Year == int.Parse(p.Year)
                                              && layer.LayerContentInfo.Category.Equals(p.Theme, StringComparison.InvariantCultureIgnoreCase));

    var destination = Path.Join(workDir, Path.GetFileName(layer.OgcLayer.DataSource.Name));
    VectorTranslate.Run(gdbPath, destination, new VectorTranslateOptions { Overwrite = true, Where = filter?.WhereClause, SourceLayerName = layer.OgcLayer.Name });


    var buffer = bufferParameters.SingleOrDefault(b => layer.LayerContentInfo.LegalState.Contains(b.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
                                           layer.LayerContentInfo.Category.Contains(b.Theme, StringComparison.CurrentCultureIgnoreCase));
    if (buffer != null)
    {
        //using var lyr = Ogr.Open(destination, GdalConst.GA_ReadOnly);
       var sqlStatement = $"""
            SELECT *, ST_Buffer(SHAPE, {buffer.BufferDistanceMeter}) as SHAPE from '{layer.LayerName}'
            """;
        VectorTranslate.Run(destination, Path.Join(Path.GetDirectoryName(destination), "test.gdb"), new VectorTranslateOptions
        {
            Overwrite = true,
            SourceLayerName = layer.LayerName,
            NewLayerName = layer.LayerName + "_buf",
            Sql = sqlStatement,
            OtherOptions = ["-dialect", "SQLITE", "-nlt", "POLYGON"]
        });
        //lyr.ExecuteSQL(sqlStatement, null, null);
    }
}


Console.ReadKey();

//foreach (var layer in layers)
//{
//    var filters = filterParameters.Where(p => layer.LayerContentInfo.Year == p.Year
//                                              && layer.LayerContentInfo.Category.Equals(p.Theme, StringComparison.InvariantCultureIgnoreCase));
//    if (!filters.Any()) continue;

//    var newL = layer.OgcLayer.CopyToLayer(workDataSource, layer.OgcLayer.Name);
//}
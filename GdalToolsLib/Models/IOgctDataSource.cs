using System;
using System.Collections.Generic;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Layer;
using OSGeo.OGR;

namespace GdalToolsLib.Models;

public interface IOgctDataSource : IDisposable
{
    SupportedDatasource SupportInfo { get; }
    string? Name { get; }
    IOgctLayer CreateAndOpenLayer(
        string? layerName,
        ESpatialRefWkt eSpatialRef,
        wkbGeometryType geometryType,
        List<FieldDefnInfo> fieldDefnInfos = null,
        bool overwriteExisting = true,
        bool createAreaAndLengthFields = false,
        string? documentation = null);
    int GetLayerCount();
    
    //bool LayerExists(string layerName);
    bool HasLayer(string? layerName);

    IOgctLayer ExecuteSQL(string command);
    IOgctLayer ExecuteSQL(string command, string dialect);
    
    void FlushCache();
    List<string?> GetLayerNames(ELayerType layerType = ELayerType.All);
    IOgctLayer GetLayerByName(string? layerName);
    int GetLayerIndexByName(string? layerName);
    LayerDetails GetLayerInfo(string? layerName);
    bool DeleteLayer(string? layerName);
    IOgctLayer OpenLayer(string? layerName);
    IOgctLayer OpenLayer(string? layerName, string orderByFieldname);


    void RenameLayerGpkg(string? layerName, string? newLayerName);
    string ExecuteSqlFgdbGetLayerDefinition(string? layerName);
    string ExecuteSqlFgdbGetLayerMetadata(string? layerName);
    void CopyAllLayersToOtherDataSource(IOgctDataSource outputDataSource);
    IOgctLayer UnionManyLayers(List<string?> inputLayerNames, string? outputLayerName, string[] unionProcessOptions);
}
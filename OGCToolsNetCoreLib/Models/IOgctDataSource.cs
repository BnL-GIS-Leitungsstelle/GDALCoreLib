using System;
using System.Collections.Generic;
using OGCToolsNetCoreLib.Common;
using OGCToolsNetCoreLib.DataAccess;
using OGCToolsNetCoreLib.Layer;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Models;

public interface IOgctDataSource : IDisposable
{
    SupportedDatasource SupportInfo { get; }
    string Name { get; }
    IOgctLayer CreateAndOpenLayer(string layerName, ESpatialRefWKT eSpatialRef, wkbGeometryType geometryType, List<FieldDefnInfo> fieldDefnInfos = null, bool overwriteExisting = true);
    int GetLayerCount();
    bool LayerExists(string layerName);
    IOgctLayer ExecuteSQL(string command);
    IOgctLayer ExecuteSQL(string command, string dialect);
    void FlushCache();
    List<string> GetLayerNames(ELayerType layerType = ELayerType.All);
    IOgctLayer GetLayerByName(string layerName);
    int GetLayerIndexByName(string layerName);
    LayerDetails GetLayerInfo(string layerName);
    bool DeleteLayer(string layerName);
    IOgctLayer OpenLayer(string layerName);
    IOgctLayer OpenLayer(string layerName, string orderByFieldname);
    bool HasLayer(string layerName);

    void RenameLayerGpkg(string layerName, string newLayerName);
    string ExecuteSqlFgdbGetLayerDefinition(string layerName);
    string ExecuteSqlFgdbGetLayerMetadata(string layerName);
    void CopyAllLayersTo(IOgctDataSource outputDataSource);
    IOgctLayer UnionManyLayers(List<string> inputLayerNames, string outputLayerName, string[] unionProcessOptions);
}
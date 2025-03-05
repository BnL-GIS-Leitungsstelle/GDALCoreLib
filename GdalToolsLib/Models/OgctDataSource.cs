using System;
using System.Collections.Generic;
using System.IO;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Extensions;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Layer;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GdalToolsLib.Models;

public class OgctDataSource : IOgctDataSource
{
    private readonly DataSource _dataSource;
    public SupportedDatasource SupportInfo { get; set; }

    public string? Name => _dataSource.name;

    internal DataSource OgrDataSource => _dataSource;

    public OgctDataSource(DataSource dataSource)
    {
        _dataSource = dataSource;
        SupportInfo = SupportedDatasource.GetSupportedDatasource(Name);
    }

    private bool CanWrite => SupportInfo.Access == EAccessLevel.Full;

    private Exception CannotWriteException => new DataSourceMethodNotImplementedException($"Write operations are unsupported in this type of datasource");

    public IOgctLayer CreateAndOpenLayer(
        string? layerName, 
        ESpatialRefWkt eSpatialRef, 
        wkbGeometryType geometryType,
        List<FieldDefnInfo> fieldDefnInfos = null,
        bool overwriteExisting = true, 
        bool createAreaAndLengthFields = false,
        string? documentation = null)
    {
        var supportedDatasource = SupportedDatasource.GetSupportedDatasource(Name);

        switch (supportedDatasource.Type)
        {
            case EDataSourceType.SHP:
                // SHP File only contains one Layer
                return new OgctLayer(_dataSource.GetLayerByIndex(0), this);
            case EDataSourceType.SHP_FOLDER or EDataSourceType.GPKG or EDataSourceType.InMemory or EDataSourceType.OpenFGDB:

                SpatialReference spRef;
                spRef = eSpatialRef == ESpatialRefWkt.None ? null : new SpatialReference(eSpatialRef.GetEnumDescription(typeof(ESpatialRefWkt)));

                if (HasLayer(layerName) && overwriteExisting)
                {
                    DeleteLayer(layerName);
                }


                var layerOptions = new List<string>() 
                {
                    overwriteExisting ? OgcConstants.OptionOverwriteYes : OgcConstants.OptionOverwriteNo,
                    createAreaAndLengthFields ? OgcConstants.OptionCreateShapeAreaAndLengthFieldsYes : OgcConstants.OptionCreateShapeAreaAndLengthFieldsNo
                };
                if (!string.IsNullOrEmpty(documentation))
                    layerOptions.Add($"{OgcConstants.OptionDocumentationPrefix}{documentation}");

                var layer = _dataSource.CreateLayer(layerName, spRef, geometryType, layerOptions.ToArray());


                if (layer == null) throw new Exception("Could not create new LayerName " + layerName + " in " + _dataSource.name);

                if (fieldDefnInfos != null)
                {
                    foreach (var field in fieldDefnInfos)
                    {
                        layer.CreateField(field.GetFieldDefn(), 1);
                    }
                }

                return new OgctLayer(layer, this);
            default:
                throw new DataSourceMethodNotImplementedException("unknown datasource type");
        }

    }

    public int GetLayerCount()
    {
        return _dataSource.GetLayerCount();
    }





    /// <summary>
    ///
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    //[Obsolete("LayerExists is deprecated, please use HasLayer(layername) instead.")]
    //public bool LayerExists(string layerName)
    //{
    //    OSGeo.OGR.Layer result = _dataSource.GetLayerByName(layerName);
    //    return result != null;
    //}


    public bool HasLayer(string? layerName)
    {
        OSGeo.OGR.Layer result = _dataSource.GetLayerByName(layerName);
        return result != null;
    }

    /// <summary>
    /// takes sql-dialect into account
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public IOgctLayer ExecuteSQL(string command)
    {
        var result = _dataSource.ExecuteSQL(command, null, SupportInfo.Type == EDataSourceType.GPKG ? OgcConstants.GpkgSqlDialect : OgcConstants.OgrSqlDialect);

        return new OgctLayer(result, this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="dialect">can be chosen </param>
    /// <returns></returns>
    public IOgctLayer ExecuteSQL(string command, string dialect)
    {
        var result = _dataSource.ExecuteSQL(command, null, dialect);

        return new OgctLayer(result, this);
    }


    /// <summary>
    /// only for FGDB's
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    public string ExecuteSqlFgdbGetLayerDefinition(string? layerName)
    {
        if (SupportInfo.Type != EDataSourceType.OpenFGDB)
        {
            throw new DataSourceMethodNotImplementedException("Cannot only read layer definition of layers in an FGDB");
        }

        var tmpLayer = _dataSource.ExecuteSQL($"GetLayerDefinition {layerName}", null, OgcConstants.GpkgSqlDialect);
        var tmpOgctLayer = new OgctLayer(tmpLayer, this);

        var rows = tmpOgctLayer.ReadRows();

        tmpOgctLayer.Dispose();

        string xmlResult = rows[0].Items[0].ValueToString();

        return xmlResult;
    }


    // GetLayerMetadata a_layer_name
    /// <summary>
    /// Only for FGDB's
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    public string ExecuteSqlFgdbGetLayerMetadata(string? layerName)
    {
        if (SupportInfo.Type != EDataSourceType.OpenFGDB)
        {
            throw new DataSourceMethodNotImplementedException("Can only read metadata of layers in an FGDB");
        }

        var tmpLayer = _dataSource.ExecuteSQL($"GetLayerMetadata {layerName}", null, OgcConstants.GpkgSqlDialect);
        var tmpOgctLayer = new OgctLayer(tmpLayer, this);

        var rows = tmpOgctLayer.ReadRows();

        tmpOgctLayer.Dispose();

        string xmlResult = rows[0].Items[0].ValueToString();

        return xmlResult;
    }

    public void FlushCache()
    {
        _dataSource.FlushCache();
    }

    public List<string?> GetLayerNames(ELayerType layerType = ELayerType.All)
    {
        var layerNames = new List<string?>();

        for (int i = 0; i < GetLayerCount(); i++)
        {
            OSGeo.OGR.Layer layer = _dataSource.GetLayerByIndex(i); // test
            var layerName = _dataSource.GetLayerByIndex(i).GetName();

            var layerInfo = GetLayerInfo(layerName);

            switch (layerType)
            {
                case ELayerType.All:
                    layerNames.Add(layerName);
                    break;
                case ELayerType.Table:
                    if (layerInfo.LayerType == ELayerType.Table)
                    {
                        layerNames.Add(layerName);
                    }
                    break;

                case ELayerType.AllGeometry:
                    if (layerInfo.LayerType != ELayerType.Table)
                    {
                        layerNames.Add(layerName);
                    }
                    break;
                case ELayerType.Point:
                    if (layerInfo.LayerType == ELayerType.Point)
                    {
                        layerNames.Add(layerName);
                    }
                    break;
                case ELayerType.Polyline:
                    if (layerInfo.LayerType == ELayerType.Polyline)
                    {
                        layerNames.Add(layerName);
                    }
                    break;
                case ELayerType.Polygon:
                    if (layerInfo.LayerType == ELayerType.Polygon)
                    {
                        layerNames.Add(layerName);
                    }
                    break;

                default:
                    throw new NotImplementedException("unhandled layerType");
            }
        }

        return layerNames;
    }

    public IOgctLayer GetLayerByName(string? layerName)
    {
        return new OgctLayer(_dataSource.GetLayerByName(layerName), this);
    }

    public int GetLayerIndexByName(string? layerName)
    {
        for (int i = 0; i < _dataSource.GetLayerCount(); i++)
        {
            using (OSGeo.OGR.Layer layer = _dataSource.GetLayerByIndex(i))
            {
                if (layer.GetName().ToLower() == layerName!.ToLower()) return i;
            }
        }

        throw new DirectoryNotFoundException($"LayerName not found= {layerName} in {_dataSource.name} ");
    }

    public LayerDetails GetLayerInfo(string? layerName)
    {
        return new LayerDetails(this, layerName);
    }

    public bool DeleteLayer(string? layerName)
    {
        if (SupportInfo.Type == EDataSourceType.SHP)// if layer in shape then delete shp-file
        {
            throw new DataSourceMethodNotImplementedException("layer in shapefiles can be deleted by deleting the shapefile");
        }
        return _dataSource.DeleteLayer(GetLayerIndexByName(layerName)) == 0;
    }


    public IOgctLayer OpenLayer(string? layerName)
    {
        if (!HasLayer(layerName))
        {
            throw new Exception("Could not open LayerName (not found): " + layerName + " in " + Name);
        }
        var layer = GetLayerByName(layerName);
        return layer;
    }

    public IOgctLayer OpenLayer(string? layerName, string orderByFieldname)
    {
        if (!HasLayer(layerName))
        {
            throw new Exception("Could not open LayerName (not found): " + layerName + " in " + Name);
        }

        var sql = $"SELECT * FROM {layerName} ORDER BY {orderByFieldname}";

        var layer = ExecuteSQL(sql);
        return layer;
    }

    public void CopyAllLayersToOtherDataSource(IOgctDataSource outputDataSource)
    {
        foreach (var layerName in GetLayerNames())
        {
            using var sourceLayer = OpenLayer(layerName);
            sourceLayer.CopyToLayer(outputDataSource, layerName);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputLayerNames"></param>
    /// <param name="outputLayerName"></param>
    /// <param name="unionProcessOptions"></param>
    /// <returns></returns>
    public IOgctLayer UnionManyLayers(List<string?> inputLayerNames, string? outputLayerName,
        string[] unionProcessOptions)
    {
        var unionGroup = new UnionProcessLayerGroup(inputLayerNames, outputLayerName);

        foreach (var item in unionGroup.Items)
        {
            using (var inputLayer = OpenLayer(item.InputLayerName))
            {
                using (var otherLayer = OpenLayer(item.MethodLayerName))
                {
                    inputLayer.UnifyInSingleGpkg(otherLayer, item.ResultLayerName, unionProcessOptions);
                }
            }
        }


        var outputLayer = OpenLayer(outputLayerName);

        var AreaHa = outputLayer.CalculateArea() / 10000;


        return outputLayer;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="newLayerName"></param>
    public void RenameLayerGpkg(string? layerName, string? newLayerName)
    {
        if (SupportInfo.Type != EDataSourceType.GPKG || !HasLayer(layerName) || HasLayer(newLayerName))
            return;
        _dataSource.StartTransaction(1);

        var sqlDdl = $"ALTER TABLE {layerName} RENAME TO {newLayerName};";
        _ = ExecuteSQL(sqlDdl);

        _dataSource.SyncToDisk();
        _dataSource.CommitTransaction();
    }


    /// <summary>
    /// NOT WORKING
    /// </summary>
    /// <param name="file"></param>
    /// <param name="layerName"></param>
    /// <param name="newLayerName"></param>
    public void RenameLayerOpenFgdb(string? layerName, string? newLayerName)
    {
        if (SupportInfo.Type != EDataSourceType.OpenFGDB || !HasLayer(layerName) || HasLayer(newLayerName))
            return;
        _dataSource.StartTransaction(1);

        var sqlDdl = $"ALTER TABLE {layerName} RENAME TO {newLayerName}";
        _ = ExecuteSQL(sqlDdl);

        _dataSource.SyncToDisk();
        _dataSource.CommitTransaction();
    }


    /// <summary>
    /// only for FGDB and GPKG
    /// </summary>
    /// <returns></returns>
    public void ExecuteSqlCompress()
    {
        switch (SupportInfo.Type)
        {
            case EDataSourceType.GPKG:
                _dataSource.ExecuteSQL($"VACUUM", null, OgcConstants.GpkgSqlDialect);
                break;

            case EDataSourceType.OpenFGDB:
                _dataSource.ExecuteSQL($"REPACK", null, OgcConstants.OgrSqlDialect);
                break;

            default:
                throw new DataSourceMethodNotImplementedException("Unknown Datasource for SQL - compress");
        }
    }


    public void Dispose()
    {
        FlushCache();
        _dataSource?.Dispose();
    }

    public IEnumerable<IOgctLayer> LayerIterator()
    {
        var layerCount = OgrDataSource.GetLayerCount();
        for (int i = 0; i < layerCount; i++)
        {
            yield return new OgctLayer(_dataSource.GetLayerByIndex(i), this);
        }
    }

    ~OgctDataSource()
    {
        _dataSource?.Dispose();
    }
}
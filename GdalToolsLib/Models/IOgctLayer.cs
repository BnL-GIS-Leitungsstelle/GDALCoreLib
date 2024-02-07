using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GdalToolsLib.Common;
using GdalToolsLib.Feature;
using GdalToolsLib.Geometry;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Layer;

namespace GdalToolsLib.Models;

public interface IOgctLayer : IDisposable
{
    string Name { get; }
    LayerDetails LayerDetails { get; }
    IOgctDataSource DataSource { get; }
    ESpatialRefWkt GetSpatialRef();
    long CopyFeatures(IOgctLayer targetLayer, bool generateNewFids = false, Action<int> reportProgressPercentage = null);
    bool IsGeometryType();
    long CopyToLayer(IOgctLayer targetLayer);
    long CopyToLayer(IOgctDataSource targetDataSource, string newLayerName, bool overwriteExisting = true);

    /// <summary>
    /// Copies a named layer in the same or into other datasource (same: possible for gpkg).
    /// Overwrites an already existing datasource or layer as well as an existing shapefile.
    /// </summary>
    /// <remarks>
    /// Remark: FGDB is not implemented as target datasource due to missing write capabilities of the ogc-driver used.
    /// <list type="table">
    ///     <listheader>
    ///         <term>in / out</term>
    ///         <description>datasource types</description> 
    ///     </listheader>
    ///     <item>
    ///         <term>in</term>
    ///         <description> shp, gpkg or fgdb </description> 
    ///     </item>
    ///     <item>
    ///         <term>out</term>
    ///         <description> shp or gpkg </description> 
    ///     </item> 
    /// </list>
    /// <param name="sqlRecordFilter">filters a record range like: SELECT * FROM layername LIMIT recLimit OFFSET recOffset </param>
    /// </remarks>
    void CopyToLayer(IOgctDataSource targetDataSource, string layerNameOut, int recLimit, int recOffset);

    string BufferToLayer(IOgctDataSource dataSource, double bufferDistance, string outputLayerNameAppendix = "Buffer", bool overwriteExisting = true);

    string GeoProcessWithLayer(EGeoProcess geoProcess, IOgctLayer otherLayer, string outputLayerName = null);

    void UnifyInSingleGpkg(IOgctLayer otherLayer, string resultLayerName, string[] unionProcessOptions);

    IOgctFeature CreateAndOpenFeature(long fid);
    int CreateField(FieldDefnInfo newFieldInfo);
    int DeleteField(FieldDefnInfo field);

    /// <summary>
    /// calculate area directly by geometry, no field like "Shape_Area" needed
    /// </summary>
    /// <param name="useUnion"></param>
    /// <returns></returns>
    double CalculateArea(bool useUnion = false);

    /// <summary>
    /// write a given value into all records of the named featureclass and field
    /// </summary>
    /// <param name="fieldDefn"></param>
    /// <param name="value"></param>
    /// <returns>the result shows a list of invalidFeatures</returns>
    LayerAttributeWriteResult WriteValue(FieldDefnInfo fieldDefn, object value);

    List<FeatureRow> ReadRows(List<FieldDefnInfo> fieldList = null);

    /// <summary>
    /// Filters the layer by the where-condition. The filter is only respected in combination with a loop: layer.GetNextFeature().
    /// Error codes by https://gdal.org/doxygen/ogr__core_8h.html
    /// </summary>
    /// <param name="sqlWhereClause">e.g. string = 'SUB_REGION = 'Pacific''
    /// or String.Empty | null | "" to clear the query</param>
    /// <returns>the number of records selected by where clause</returns>
    long FilterByAttributeOnlyRespectedInNextFeatureLoop(string sqlWhereClause);

    OgctFeature OpenNextFeature();
    void AddFeature(IOgctFeature feature);
    Task<LayerValidationResult> ValidateGeometryAsync(Action<double> reportProgress = null, CancellationToken? cancellationToken = null, bool attemptRepair = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="wkt"></param>
    void SetSpatialFilterNotImplemented(string wkt);
    
    void CopySchema(IOgctDataSource targetDataSource, string newLayerName, bool overwriteExisting = true);

    /// <summary>
    /// Dissolves a named layer in the same (same: possible for gpkg) or into another datasource .
    /// Overwrites an already existing datasource or layer as well as an existing shapefile.
    /// </summary>
    /// <remarks>
    /// Remark: FGDB is not implemented as target datasource due to missing write capabilities of the ogc-driver used.
    /// <list type="table">
    ///     <listheader>
    ///         <term>in / out</term>
    ///         <description>datasource types</description> 
    ///     </listheader>
    ///     <item>
    ///         <term>in</term>
    ///         <description> shp or gpkg</description> 
    ///     </item>
    ///     <item>
    ///         <term>out</term>
    ///         <description> shp or gpkg </description> 
    ///     </item> 
    /// </list>
    /// <param name="fieldsUsedForDissolve">Dissolves the layer by the given list of fields like:
    ///        SELECT [fieldsUsedForDissolve] FROM [layername] GROUPBY [fieldsUsedForDissolve] </param>
    ///  <param name="overwriteExisting">overwites output-layer, if exists </param>      
    /// </remarks>
    /// <returns>name of the output layer</returns>
    string DissolveToLayer(IOgctDataSource dataSource, List<FieldDefnInfo> fieldsUsedForDissolve, string outputLayerNameAppendix = "Dissolve", bool overwriteExisting = true);
    Task<IList<SelfOverlapErrorResult>> ValidateSelfOverlapAsync(Action<double> reportSelfOverlapValidationProgress = null,
        CancellationToken? cancellationToken = null);
    IOgctFeature OpenFeatureByFid(long featureId);
    void UpdateFeature(IOgctFeature feature);
    void ResetReading();
    double GetLinearUnits();
}
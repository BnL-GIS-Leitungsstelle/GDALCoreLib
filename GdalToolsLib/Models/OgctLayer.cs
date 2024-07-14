using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GdalToolsLib.Common;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Extensions;
using GdalToolsLib.Feature;
using GdalToolsLib.Geometry;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Helpers;
using GdalToolsLib.Layer;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GdalToolsLib.Models;

public partial class OgctLayer : IOgctLayer
{
    private readonly OSGeo.OGR.Layer _layer;
    private readonly OgctDataSource _dataSource;
    private volatile uint _currentGeometryFeatureValidationIndex = 0;
    private volatile bool _isValidatingFeatures = false;

    private readonly string[] DefaultProcessOptions = { "SKIP_FAILURES=YES" };

    /// <summary>
    /// SKIP_FAILURES=YES/NO. Set it to YES to go on, even when a feature could not be inserted.
    ///    PROMOTE_TO_MULTI=YES/NO. Set it to YES to convert Polygons into MultiPolygons, or LineStrings to MultiLineStrings.
    ///    INPUT_PREFIX=string. Set a prefix for the field names that will be created from the fields of the input layer.
    ///    METHOD_PREFIX=string. Set a prefix for the field names that will be created from the fields of the method layer.
    ///    USE_PREPARED_GEOMETRIES=YES/NO. Set to NO to not use prepared geometries to pretest intersection of features of method layer with features of this layer.
    ///    KEEP_LOWER_DIMENSION_GEOMETRIES=YES/NO.Set to NO to skip result features with lower dimension geometry that would otherwise be added to the result layer.The default is to add but only if the result layer has an unknown geometry type. 
    /// https://www.gdal.org/1.11/ogr/classOGRLayer.html#aeb8ab475561f2aca2c0e605cfb810b22
    /// </summary>
    private readonly string[] UnionProcessOptions = { "SKIP_FAILURES=YES", "PROMOTE_TO_MULTI=YES", "INPUT_PREFIX=inLayer", "USE_PREPARED_GEOMETRIES=YES", "KEEP_LOWER_DIMENSION_GEOMETRIES=NO" };




    public OgctLayer(OSGeo.OGR.Layer layer, IOgctDataSource dataSource)
    {
        _layer = layer;
        _dataSource = (OgctDataSource)dataSource;
    }

    internal OSGeo.OGR.Layer OgrLayer => _layer;

    public IOgctDataSource DataSource => _dataSource;
    public string Name => _layer.GetName();

    public LayerDetails LayerDetails => new(this);

    public ESpatialRefWkt GetSpatialRef()
    {
        return new LayerSpatialRef(_layer).SpRef;
    }

    public double GetLinearUnits()
    {
        return _layer.GetSpatialRef().GetLinearUnits();
    }


    /// <summary>
    /// TODO: Fields with a reserved name cannot be used as user defined fields
    /// </summary>
    /// <param name="targetLayer"></param>
    /// <param name="generateNewFids"></param>
    /// <param name="reportProgressPercentage"></param>
    /// <returns></returns>
    public long CopyFeatures(IOgctLayer targetLayer, bool generateNewFids = false, Action<int> reportProgressPercentage = null)
    {
        var ogctTargetLayer = (OgctLayer)targetLayer;
        FeatureDefn fields = ogctTargetLayer._layer.GetLayerDefn();
        OSGeo.OGR.Feature sourceFeature;
        FeatureDefn sourceFields = _layer.GetLayerDefn();

        var layerFeatureCount = LayerDetails.FeatureCount;
        var copiedFeatures = 0L;

        var newFeatureIndex = targetLayer.LayerDetails.FeatureCount;

        ogctTargetLayer.OgrLayer.StartTransaction();

        while ((sourceFeature = _layer.GetNextFeature()) != null)
        {
            // Console.WriteLine($" - Copy Feature {sourceFeature.GetFID()}");
            using OSGeo.OGR.Feature newFeature = new OSGeo.OGR.Feature(fields);

            if (ogctTargetLayer.LayerDetails.LayerType == ELayerType.Table)
            {
                newFeature.CreateTableRecordFromOther(fields, sourceFields, sourceFeature);
            }
            else
            {
                newFeature.CreateFromOther(fields, sourceFields, sourceFeature, ogctTargetLayer.DataSource.SupportInfo.Type);
            }
            

            if (generateNewFids || targetLayer.DataSource.SupportInfo.Type == EDataSourceType.OpenFGDB)
            {
                newFeature.SetFID(++newFeatureIndex);
            }
            sourceFeature.Dispose();
            ogctTargetLayer._layer.CreateFeature(newFeature);
            if (copiedFeatures++ % 100 == 0)
            {
                reportProgressPercentage?.Invoke((int)(100 * copiedFeatures / layerFeatureCount));
            }
        }

        ogctTargetLayer.OgrLayer.CommitTransaction();

        return ogctTargetLayer._layer.GetFeatureCount(1);
    }

    public bool IsGeometryType()
    {
        return _layer.IsGeometryType();
    }

    //TODO: consider whether we should use the built-in layer copy for some scenarios
    //TODO:
    ////Duplicate an existing layer. 
    //This method creates a new layer, duplicate the field definitions of the source layer and then duplicate each features of the source layer.The papszOptions argument can be used to control driver specific creation options. These options are normally documented in the format specific documentation. 
    // The source layer may come from another datasource.

    // papszOptions a StringList of name = value options.Options are driver specific. 
    // SKIP_FAILURES=YES/NO. Set it to YES to go on, even when a feature could not be inserted. 
    // PROMOTE_TO_MULTI = YES / NO.Set it to YES to convert Polygons into MultiPolygons, or LineStrings to MultiLineStrings.

    // string[] defaultCopyOptions = { OgcConstants.OptionOverwriteYes, "PROMOTE_TO_MULTI=YES", "SKIP_FAILURES=YES" };
    // https://gdal.org/doxygen/classGDALDataset.html#afcaabd6468b256fda99bd50db34ceff1
    //    using (Layer targetLayer = ds.CopyLayer(sourceLayer, layerName, defaultCopyOptions))
    /*public OgctLayer CopyToLayer(string newLayerName, bool overwriteExisting = true)
    {
        var layer = _dataSource.CopyLayer(_layer, newLayerName,
            new string[] { overwriteExisting ? OgcConstants.OptionOverwriteYes : OgcConstants.OptionOverwriteNo });
        return new OgctLayer(layer, _dataSource);
    }*/

    public long CopyToLayer(IOgctLayer targetLayer)
    {
        var ogctLayer = (OgctLayer)targetLayer;
        CloneFieldSchema(ogctLayer);
        return CopyFeatures(targetLayer);
    }

    public long CopyToLayer(IOgctDataSource targetDataSource, string? newLayerName, bool overwriteExisting = true)
    {
        using var targetLayer = targetDataSource.CreateAndOpenLayer(newLayerName, GetSpatialRef(), _layer.GetGeomType(), null, overwriteExisting);
        CloneFieldSchema(targetLayer);
        return CopyFeatures(targetLayer);
    }

    public void CopySchema(IOgctDataSource targetDataSource, string? newLayerName, bool overwriteExisting = true)
    {
        using var targetLayer = targetDataSource.CreateAndOpenLayer(newLayerName, GetSpatialRef(), _layer.GetGeomType(), null, overwriteExisting);
        CloneFieldSchema(targetLayer);
    }

    public void CopySchema(IOgctLayer targetLayer)
    {
        var ogctLayer = (OgctLayer)targetLayer;
        CloneFieldSchema(ogctLayer);
    }

    /// <summary>
    /// Copies a named layer in the same or into other datasource (same: possible for gpkg, fgdb).
    /// Overwrites an already existing datasource or layer as well as an existing shapefile.
    /// </summary>
    /// <remarks>
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
    ///         <description> shp or gpkg or fgdb </description> 
    ///     </item> 
    /// </list>
    /// <param name="sqlRecordFilter">filters a record range like: SELECT * FROM layername LIMIT recLimit OFFSET recOffset </param>
    /// </remarks>
    public void CopyToLayer(IOgctDataSource targetDataSource, string? layerNameOut, int recLimit, int recOffset)
    {
        var sourceTypeIn = SupportedDatasource.GetSupportedDatasource(_dataSource.Name);
        var sourceTypeOut = SupportedDatasource.GetSupportedDatasource(targetDataSource.Name);


        var sqlRecordFilterQuery = $"SELECT * FROM {LayerDetails.Name} LIMIT {recLimit} OFFSET {recOffset}";


        switch (sourceTypeOut.Type)
        {
            case EDataSourceType.SHP:

                switch (sourceTypeIn.Type)
                {
                    case EDataSourceType.SHP:// SHP to SHP: copy datasource
                        throw new DataSourceMethodNotImplementedException("Cannot add layers to shape files");
                    case EDataSourceType.SHP_FOLDER:
                    case EDataSourceType.OpenFGDB:
                    case EDataSourceType.GPKG:
                        using (var result = _dataSource.ExecuteSQL(sqlRecordFilterQuery))
                        {
                            result.CopyToLayer(targetDataSource, layerNameOut);
                        }

                        break;

                    default:
                        throw new DataSourceMethodNotImplementedException("case not implemented");
                }

                break;

            case EDataSourceType.SHP_FOLDER:
            case EDataSourceType.GPKG:   // GPKG to GPKG
                CopyToLayer(targetDataSource, layerNameOut);

                break;

            case EDataSourceType.OpenFGDB:
                using (var result = _dataSource.ExecuteSQL(sqlRecordFilterQuery))
                {
                    result.CopyToLayer(targetDataSource, layerNameOut);
                }

                break;

            //throw new DataSourceReadOnlyException("FGDB is read-only");

            default:
                throw new DataSourceMethodNotImplementedException("data source unknown");
        }
    }


    /// <summary>
    /// Dissolves a named layer in the same (same: possible for gpkg) or into another datasource .
    /// Overwrites an already existing datasource or layer as well as an existing shapefile.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    ///     <listheader>
    ///         <term>in / out</term>
    ///         <description>datasource types</description> 
    ///     </listheader>
    ///     <item>
    ///         <term>in</term>
    ///         <description> shp or gpkg or fgdb</description> 
    ///     </item>
    ///     <item>
    ///         <term>out</term>
    ///         <description> shp or gpkg or fgdb</description> 
    ///     </item> 
    /// </list>
    /// <param name="fieldsUsedForDissolve">Dissolves the layer by the given list of fields like:
    ///        SELECT [fieldsUsedForDissolve] FROM [layername] GROUPBY [fieldsUsedForDissolve].
    /// if empty, all fields of the layer will be used for dissolved</param>
    ///  <param name="overwriteExisting">overwrites output-layer, if exists </param>      
    /// </remarks>
    /// <returns>name of the output layer</returns>
    public string? DissolveToLayer(IOgctDataSource dataSource, List<FieldDefnInfo> fieldsUsedForDissolve, string outputLayerNameAppendix = "Dissolve", bool overwriteExisting = true)
    {
        string? outputLayerName = String.IsNullOrWhiteSpace(outputLayerNameAppendix) ? LayerDetails.Name + "Dissolve" : LayerDetails.Name + outputLayerNameAppendix.Trim();

        if (fieldsUsedForDissolve.Count == 0)  // dissolve on all fields
        {
            fieldsUsedForDissolve = LayerDetails.Schema.FieldList;
        }
        var dissolveCondition = GetDissolveCondition(fieldsUsedForDissolve);


        // create outputLayer
        IOgctLayer outputLayer = null!;
        OgctDataSource outputDataSource = null!;

        // dissolved layers contain usually multiparts
        var multiGeomType = ForceMultiPartGeomType(LayerDetails.GeomType);

        switch (dataSource.SupportInfo.Type)
        {
            case EDataSourceType.SHP:
                var outputDsAccessor = new OgctDataSourceAccessor();

                string? outputDsPath = dataSource.Name.Replace(LayerDetails.Name, outputLayerName);

                outputDataSource = outputDsAccessor.CreateAndOpenDatasource(outputDsPath, GetSpatialRef(), multiGeomType);
                outputLayer = outputDataSource.OpenLayer(outputLayerName);
                break;

            case EDataSourceType.SHP_FOLDER:
            case EDataSourceType.GPKG:   // GPKG to GPKG

                outputLayer = _dataSource.CreateAndOpenLayer(outputLayerName, GetSpatialRef(), multiGeomType);
                break;

            case EDataSourceType.OpenFGDB:
                throw new DataSourceReadOnlyException("FGDB is read-only");
            default:
                throw new DataSourceMethodNotImplementedException("data source unknown");
        }

        CloneFieldSchema(outputLayer, fieldsUsedForDissolve);

        long outputFeatureFid = 0;

        foreach (var conditionGroup in dissolveCondition.DissolveGroups)
        {
            // Console.WriteLine($" --- filter group = {conditionGroup.ToSql()} ");

            FilterByAttributeOnlyRespectedInNextFeatureLoop(conditionGroup.ToSql());

            var outputFeature = outputLayer.CreateAndOpenFeature(outputFeatureFid++);

            var dissolvedGeom = GetDissolvedGeometryFromFilteredLayerAndOpen(outputLayer.LayerDetails.GeomType);

            if (dissolvedGeom.Type.Contains(multiGeomType.ToString(), StringComparison.InvariantCultureIgnoreCase) == false)
            {
                Console.WriteLine($" -- Dissolve - Geometrytype: {dissolvedGeom.Type} in layer {outputLayerName}: {multiGeomType}");
            }


            // fill geometry and attribute values
            outputFeature.SetGeometry(dissolvedGeom.CloneAndOpen());

            dissolvedGeom.Dispose();

            foreach (var field in outputLayer.LayerDetails.Schema.FieldList)
            {

                var content = conditionGroup.FieldConditions.Find(x => x.Field.Name == field.Name).Content;

                outputFeature.SetValue(field, content);
            }

            outputFeature.ValidateSchemaConstraints();

            outputLayer.AddFeature(outputFeature);

            outputFeature.Dispose();
        }

        outputLayer.Dispose();

        if (outputDataSource != null)
        {
            outputDataSource.Dispose();
        }

        return outputLayerName;
    }

    /// <summary>
    /// if a geomType is "single, like Point or Polygon, it will be converted to Multipoint / MultiPolygon
    /// </summary>
    /// <param name="geomType"></param>
    /// <returns></returns>
    private wkbGeometryType ForceMultiPartGeomType(wkbGeometryType geomType)
    {
        switch (geomType)
        {
            case wkbGeometryType.wkbPolygon:
                return wkbGeometryType.wkbMultiPolygon;

            case wkbGeometryType.wkbLineString:
                return wkbGeometryType.wkbMultiLineString;

            case wkbGeometryType.wkbPoint:
                return wkbGeometryType.wkbMultiPoint;

            default:
                return geomType;
        }


    }

    /// <summary>
    ///
    /// Output-Layer is always a polygon 
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="bufferDistance"></param>
    /// <param name="outputLayerNameAppendix"></param>
    /// <param name="overwriteExisting"></param>
    /// <returns></returns>
    /// <exception cref="DataSourceReadOnlyException"></exception>
    /// <exception cref="DataSourceMethodNotImplementedException"></exception>
    public string? BufferToLayer(IOgctDataSource dataSource, double bufferDistance,
        string outputLayerNameAppendix = "Buffer", bool overwriteExisting = true)
    {
        string? outputLayerName = String.IsNullOrWhiteSpace(outputLayerNameAppendix)
            ? LayerDetails.Name + "Buffer"
            : LayerDetails.Name + outputLayerNameAppendix.Trim();

        // create outputLayer
        IOgctLayer outputLayer = null;
        IOgctDataSource outputDataSource = null;

        switch (dataSource.SupportInfo.Type)
        {
            case EDataSourceType.SHP:
                var outputDsAccessor = new OgctDataSourceAccessor();

                string? outputDsPath = dataSource.Name.Replace(LayerDetails.Name, outputLayerName);

                outputDataSource = outputDsAccessor.CreateAndOpenDatasource(outputDsPath, GetSpatialRef(),
                    wkbGeometryType.wkbPolygon);
                outputLayer = outputDataSource.OpenLayer(outputLayerName);
                break;

            case EDataSourceType.SHP_FOLDER:
            case EDataSourceType.GPKG: // GPKG to GPKG
                outputLayer =
                    _dataSource.CreateAndOpenLayer(outputLayerName, GetSpatialRef(), wkbGeometryType.wkbPolygon);
                break;

            case EDataSourceType.OpenFGDB:
                throw new DataSourceReadOnlyException("FGDB is read-only");
            default:
                throw new DataSourceMethodNotImplementedException("data source unknown");
        }
        CopySchema(outputLayer);

        var feature = OpenNextFeature();

        while (feature != null)
        {
            using var geom = feature.OpenGeometry();

            using var bufferedGeometry = geom.GetAndOpenBuffer(bufferDistance);

            using var outputFeature = feature.CloneAndOpen();

            outputFeature.SetGeometry(bufferedGeometry);
            outputLayer.AddFeature(outputFeature);

            feature.Dispose();
            feature = OpenNextFeature();
        }

        outputLayer.Dispose();

        if (outputDataSource != null)
        {
            outputDataSource.Dispose();
        }

        return outputLayerName;
    }


    /// <summary>
    /// Geoprocessing in the same GPKG
    /// </summary>
    /// <param name="geoProcess"></param>
    /// <param name="otherLayer"></param>
    /// <param name="outputLayerName"></param>
    /// <returns></returns>
    public string? GeoProcessWithLayer(EGeoProcess geoProcess, IOgctLayer otherLayer, string? outputLayerName = null)
    {
        outputLayerName = outputLayerName == null ? $"{this.Name}{geoProcess}" : $"{outputLayerName}{geoProcess}";

        GeoprocessingInSingleGpkg(geoProcess, otherLayer, outputLayerName);

        return outputLayerName;
    }

    private IOgctFeature IterateAndOpenUnifiedFeature()
    {
        var feature = OpenNextFeature();

        IOgctGeometry dissolvedGeometry = null;
        IOgctFeature outputFeature = null;

        // to be cloned from inputFeature

        while (feature != null)
        {
            using var geom = feature.OpenGeometry();
            if (dissolvedGeometry == null)  // init dissolve-geometry
            {
                dissolvedGeometry = geom.CloneAndOpen();
            }
            else
            {
                try
                {
                    var geomUnion = dissolvedGeometry.GetAndOpenUnion(geom);
                    dissolvedGeometry.Dispose();
                    dissolvedGeometry = geomUnion;
                }
                catch (Exception e)
                {
                    // _log.Error($"Union of geometry-part of feature {feature.FID} failed: {e}");
                    throw;
                }
            }

            if (outputFeature == null)
            {
                outputFeature = feature.CloneAndOpen();
                var newFeature = new OSGeo.OGR.Feature(_layer.GetLayerDefn());

            }

            feature.Dispose();
            feature = OpenNextFeature();
        }

        if (outputFeature != null)  // update geom of output-feature
        {
            IOgctGeometry multiGeometry = null;


            if (dissolvedGeometry.IsAMultiGeometryType() == false)
            {
                multiGeometry = dissolvedGeometry.CreateMultipartGeometryAndOpen(outputFeature.GetGeomType());
                dissolvedGeometry.Dispose();
            }


            if (multiGeometry != null)
            {
                outputFeature.SetGeometry(multiGeometry);
                multiGeometry.Dispose();
            }
            else
            {
                outputFeature.SetGeometry(dissolvedGeometry);
                dissolvedGeometry.Dispose();
            }
        }
        return outputFeature;
    }

    private IOgctGeometry GetDissolvedGeometryFromFilteredLayerAndOpen(wkbGeometryType outputGeomType)
    {
        var feature = OpenNextFeature();

        IOgctGeometry dissolvedGeometry = null;
        //   IOgctFeature outputFeature = null;
        // to be cloned from inputFeature

        while (feature != null)
        {
            using var geom = feature.OpenGeometry();
            if (dissolvedGeometry == null)  // init dissolve-geometry
            {
                dissolvedGeometry = geom.CloneAndOpen();
            }
            else
            {
                try
                {
                    var geomUnion = dissolvedGeometry.GetAndOpenUnion(geom);
                    dissolvedGeometry.Dispose();
                    dissolvedGeometry = geomUnion;
                }
                catch (Exception e)
                {
                    // _log.Error($"Union of geometry-part of feature {feature.FID} failed: {e}");
                    throw;
                }
            }
            feature.Dispose();
            feature = OpenNextFeature();
        }

        IOgctGeometry multipartGeometry = null;

        if (dissolvedGeometry.IsAMultiGeometryType() == false)
        {
            multipartGeometry = dissolvedGeometry.CreateMultipartGeometryAndOpen(outputGeomType);
            dissolvedGeometry.Dispose();
            return multipartGeometry;
        }

        return dissolvedGeometry;
    }


    private List<string> GetWhereClauses(List<FieldDefnInfo> fieldsToDissolve)
    {
        var sqlWhereClauses = new List<string>();

        var dissolveCondition = new DissolveCondition();

        string listOfDissolveFieldNames = string.Join(",", fieldsToDissolve.Select(_ => _.Name));

        var sqlGroupDissolveFieldsFilterQuery = $"SELECT {listOfDissolveFieldNames} FROM {LayerDetails.Name} GROUP BY {listOfDissolveFieldNames}";

        using (var groupedInputLayer = _dataSource.ExecuteSQL(sqlGroupDissolveFieldsFilterQuery))
        {
            var groupValueRowsToDissolve = groupedInputLayer.ReadRows(); // collect distinct values to use in later dissolve

            foreach (var row in groupValueRowsToDissolve)
            {
                var group = new ConditionGroup();

                for (int i = 0; i < fieldsToDissolve.Count; i++)
                {
                    group.AddFieldCondition(fieldsToDissolve[i], ECompareSign.IsEqual, row.Items[i].Value);
                }
                dissolveCondition.AddDissolveGroup(group);

                // create list of sqlWhereClauses
                var sqlResult = QueryHelpers.BuildWhereClause(group.FieldConditions);

                sqlWhereClauses.Add(sqlResult);
            }
        }
        _layer.ResetReading();

        return sqlWhereClauses;
    }

    private DissolveCondition GetDissolveCondition(List<FieldDefnInfo> fieldsToDissolve)
    {
        var dissolveCondition = new DissolveCondition();

        string listOfDissolveFieldNames = string.Join(",", fieldsToDissolve.Select(_ => _.Name));

        var sqlGroupDissolveFieldsFilterQuery = $"SELECT {listOfDissolveFieldNames} FROM {LayerDetails.Name} GROUP BY {listOfDissolveFieldNames}";


        using (var groupedInputLayer = _dataSource.ExecuteSQL(sqlGroupDissolveFieldsFilterQuery))
        {
            var groupValueRowsToDissolve = groupedInputLayer.ReadRows(); // collect distinct values to use in later dissolve

            foreach (var row in groupValueRowsToDissolve)
            {
                var group = new ConditionGroup();

                for (int i = 0; i < fieldsToDissolve.Count; i++)
                {
                    group.AddFieldCondition(fieldsToDissolve[i], ECompareSign.IsEqual, row.Items[i].Value);
                }
                dissolveCondition.AddDissolveGroup(group);
            }
        }
        _layer.ResetReading();

        return dissolveCondition;
    }

    /// <summary>
    /// Clones the complete fieldlist of the layer into the targetLayer
    /// </summary>
    /// <param name="targetLayer"></param>
    private void CloneFieldSchema(IOgctLayer targetLayer)
    {
        var targetOgctLayer = (OgctLayer)targetLayer;
        for (int j = 0; j < _layer.GetLayerDefn().GetFieldCount(); j++)  // create fields in outputLayer
        {
            FieldDefn field = _layer.GetLayerDefn().GetFieldDefn(j);
            var name = field.GetName();

            if (name.ToUpper() == targetOgctLayer.LayerDetails.FidColumnName.ToUpper())
            {
                continue; // ignore FID-field (reserved name), its automatically generated by the database
            }
            targetOgctLayer._layer.CreateField(field, 1);

        }
    }

    /// <summary>
    /// Clones the given fieldlist of the layer into the targetLayer
    /// </summary>
    /// <param name="targetLayer"></param>
    /// <param name="fieldList">list of selected fields, to be cloned into target layer</param>
    private void CloneFieldSchema(IOgctLayer targetLayer, List<FieldDefnInfo> fieldList)
    {
        var targetOgctLayer = (OgctLayer)targetLayer;
        for (int j = 0; j < _layer.GetLayerDefn().GetFieldCount(); j++)  // create fields in outputLayer
        {
            FieldDefn field = _layer.GetLayerDefn().GetFieldDefn(j);
            var name = field.GetName();

            //TODO:  name == "Shape_Length" || name == "Shape_Area"

            if (fieldList.Any(f => f.Name.ToLower() == name.ToLower()))
            {
                targetOgctLayer._layer.CreateField(field, 1);
            }
        }
    }

    public int CreateField(FieldDefnInfo newFieldInfo)
    {
        return _layer.CreateField(newFieldInfo.GetFieldDefn(), 1);
    }

    public int DeleteField(FieldDefnInfo field)
    {
        return _layer.DeleteField(field.OgrIndex);
    }

    /// <summary>
    /// calculate area directly by geometry, no field like "Shape_Area" needed
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="useUnion"></param>
    /// <returns></returns>
    public double CalculateArea(bool useUnion = false)
    {
        if (_layer.GetGeomType() == wkbGeometryType.wkbNone)
        {
            return 0;
        }
        if (useUnion)
        {
            OSGeo.OGR.Geometry unionGeometry = null;

            OSGeo.OGR.Feature feature;
            while ((feature = _layer.GetNextFeature()) != null)
            {
                if (unionGeometry == null)
                {
                    unionGeometry = feature.GetGeometryRef();
                }
                unionGeometry = unionGeometry.Union(feature.GetGeometryRef());
            }
            return unionGeometry.GetArea();
        }
        else
        {
            var sum = 0d;
            OSGeo.OGR.Feature feature;
            while ((feature = _layer.GetNextFeature()) != null)
            {
                sum += feature.GetGeometryRef().Area();
            }
            return sum;
        }
    }

    /// <summary>
    /// write a given value into all records of the named featureclass and field
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="layerName"></param>
    /// <param name="fieldName"></param>
    /// <param name="value"></param>
    /// <returns>the result shows a list of invalidFeatures</returns>
    public LayerAttributeWriteResult WriteValue(FieldDefnInfo fieldDefn, object value)
    {
        var result = new LayerAttributeWriteResult(_dataSource.Name, Name, fieldDefn.Name, value);

        var layerInfo = LayerDetails;

        if (layerInfo.Schema.HasField(fieldDefn) == false)
        {
            // _log.Warn($"No geometry supported in layer '{layerName}', layer isn't of geometry-type)");
            result.SetFieldNotFound();
            return result;
        }
        // is geom-field or id-field
        if (layerInfo.Schema.IsEditable(fieldDefn) == false)
        {
            result.SetFieldNotEditable();
            return result;
        }

        // can value be cast into the fields datatype ?

        if (layerInfo.Schema.CanValueBeCastToFieldDataType(fieldDefn, value) != EFieldWriteErrorType.IsValid)
        {
            result.SetValueCastFailed();
        }
        else
        {
            OgctFeature feature;
            while ((feature = OpenNextFeature()) != null)
            {
                var validationResult = feature.WriteValue(fieldDefn, value);
                if (validationResult.Valid) continue;

                //_log.Warn($" Invalid geometry - {validationResult}");
                result.AddInvalidFeature(validationResult);
                feature.Dispose();
            }
        }

        _dataSource.FlushCache();

        return result;

    }


    public List<FeatureRow> ReadRows(List<FieldDefnInfo> fieldList = null)
    {
        if (fieldList == null)
        {
            fieldList = LayerDetails.Schema.FieldList;
        }
        var rows = new List<FeatureRow>();

        OgctFeature feature;
        while ((feature = OpenNextFeature()) != null)
        {
            rows.Add(feature.ReadRow(fieldList));
            feature.Dispose();
        }

        return rows;
    }

    /// <summary>
    /// Filters the layer by the where-condition. The filter is only respected in combination with a loop: layer.GetNextFeature().
    /// Error codes by https://gdal.org/doxygen/ogr__core_8h.html
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="sqlWhereClause">e.g. string = 'SUB_REGION = 'Pacific''
    /// or String.Empty | null | "" to clear the query</param>
    /// <returns>the number of records selected by where clause</returns>
    public long FilterByAttributeOnlyRespectedInNextFeatureLoop(string sqlWhereClause)
    {
        // clears the query
        if (String.IsNullOrWhiteSpace(sqlWhereClause))
        {
            _layer.SetAttributeFilter(sqlWhereClause);
            return _layer.GetFeatureCount(1);
        }
        // Test: null, empty, check Number of records before and after and reset filter
        // Test: 0 records  / query contains errors
        // var recordCount = layer.GetFeatureCount(1);

        try
        {
            int returnValue = _layer.SetAttributeFilter(sqlWhereClause);
            if (returnValue != 0)  // 0 = ogr.OGRERR_NONE
            {
                EOGRERR enumValue = (EOGRERR)returnValue;
                throw new NotSupportedException($"FilterByAttribute fails on WhereClause {sqlWhereClause} with return value {enumValue}!");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);  // TODO: enable logging in library
            throw;
        }

        long recordCount = 0;
        OgctFeature feature;
        while ((feature = OpenNextFeature()) != null)
        {
            recordCount++;
            feature.Dispose();
        }

        _layer.ResetReading();

        return recordCount;
    }


    public OgctFeature OpenNextFeature()
    {
        var nextFeature = _layer.GetNextFeature();
        return (nextFeature != null ? new OgctFeature(nextFeature, this) : null)!;
    }

    public IOgctFeature OpenFeatureByFid(long featureId)
    {
        var feature = _layer.GetFeature(featureId);
        return (feature != null ? new OgctFeature(feature, this) : null)!;

    }

    public void AddFeature(IOgctFeature feature)
    {
        var ogctFeature = (OgctFeature)feature;
        _layer.CreateFeature(ogctFeature.OgrFeature);
    }

    //public void CreateAndOpenFeature()
    //{
    //    var feature = new OSGeo.OGR.Feature(_layer.GetLayerDefn());

    //    _layer.CreateFeature(feature);
    //}

    /// <summary>
    /// Creates an empty feature without content: no geometry, no attribute values
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="fid">required and unique, must be equal or greater than 0</param>
    /// <returns></returns>
    public IOgctFeature CreateAndOpenFeature(long fid)
    {
        var feature = new OSGeo.OGR.Feature(OgrLayer.GetLayerDefn());
        feature.SetFID(fid);

        return new OgctFeature(feature, this);
    }

    public void UpdateFeature(IOgctFeature feature)
    {
        var ogctFeature = (OgctFeature)feature;
        _layer.SetFeature(ogctFeature.OgrFeature);
    }

    public void ResetReading()
    {
        _layer.ResetReading();
    }

    public async Task<LayerValidationResult> ValidateGeometryAsync(Action<double> reportProgress = null, CancellationToken? cancellationToken = null, bool attemptRepair = false)
    {
        var result = new LayerValidationResult(_dataSource.Name, Name);

        var layerInfo = LayerDetails;

        if (!IsGeometryType())
        {
            // _log.Warn($"No geometry supported in layer '{layerName}', layer isn't of geometry-type)");
            result.SetLayerHasNoGeometry();
            return result;
        }

        _layer.ResetReading();

        _isValidatingFeatures = true;
        OgctFeature feature;
        var tasks = new ConcurrentBag<Task<GeometryValidationResult>>();
        var results = new ConcurrentBag<GeometryValidationResult>();
        var processingTask = Task.Run(() => ProcessGeometryResultsAsync(tasks, results, cancellationToken));
        while ((feature = OpenNextFeature()) != null)
        {
            if (cancellationToken?.IsCancellationRequested ?? false)
            {
                feature.Dispose();
                break;
            }
            var featureToAnalyze = feature;
            while (tasks.Count > ThreadPool.ThreadCount * 6)
            {
                Thread.Yield();
            }
            tasks.Add(Task.Run(() => ValidateFeatureGeometry(featureToAnalyze, layerInfo, reportProgress)));
        }
        _isValidatingFeatures = false;

        await processingTask;

        result.InvalidFeatures.AddRange(results.ToList());

        if (attemptRepair)
        {
            if (DataSource.SupportInfo.Access == EAccessLevel.Full)
            {
                foreach (var validationIssue in result.InvalidFeatures.Where(validationIssue => validationIssue.ErrorLevel == EFeatureErrorLevel.Error))
                {
                    var featureToFix = OpenFeatureByFid(validationIssue.FeatureFid);
                    if (featureToFix == null)  // in case the feature could not be retrieved, skip loop
                    {
                        continue;
                    }

                    using var geometryToFix = featureToFix.OpenGeometry();
                    var fixedGeometry = geometryToFix.OpenRepaired();

                    featureToFix.SetGeometry(fixedGeometry);
                    UpdateFeature(featureToFix);
                    featureToFix.Dispose();
                    fixedGeometry.Dispose();
                }
            }
            else
            {
                throw new DataSourceMethodNotImplementedException("No write access on datasource");
            }
        }

        return (cancellationToken?.IsCancellationRequested ?? false ? null : result)!;
    }




    private async Task ProcessGeometryResultsAsync(ConcurrentBag<Task<GeometryValidationResult>> tasks,
        ConcurrentBag<GeometryValidationResult> results, CancellationToken? cancellationToken = null)
    {
        while (!(cancellationToken?.IsCancellationRequested ?? false))
        {
            while (tasks.TryTake(out var taskToProcess))
            {
                //Console.WriteLine($"{tasks.Count} tasks in the pipeline");
                var result = await taskToProcess;
                if (!result.Valid)
                {
                    results.Add(result);
                }
            }

            if (tasks.IsEmpty && !_isValidatingFeatures)
            {
                break;
            }
        }
    }

    private GeometryValidationResult ValidateFeatureGeometry(IOgctFeature feature, LayerDetails layerInfo, Action<double> reportProgress = null, CancellationToken? cancellationToken = null)
    {
        var validationResult = feature.ValidateGeometry();
        feature.Dispose();
        if (cancellationToken?.IsCancellationRequested ?? false) return validationResult;
        Interlocked.Increment(ref _currentGeometryFeatureValidationIndex);
        if (_currentGeometryFeatureValidationIndex % 5 == 0)
        {
            reportProgress?.Invoke((double)_currentGeometryFeatureValidationIndex / layerInfo.FeatureCount);
        }

        return validationResult;
    }

    private void GeoprocessingInSingleGpkg(EGeoProcess geoOperation, IOgctLayer otherLayer, string? resultLayerName)
    {
        using var tempInMemoryDataset = new OgctDataSourceAccessor().CreateAndOpenInMemoryDatasource();

        using var tempInMemoryLayer = tempInMemoryDataset.CreateAndOpenLayer(resultLayerName, GetSpatialRef(), LayerDetails.GeomType,
                                                            LayerDetails.Schema.FieldList, false);

        if (tempInMemoryLayer == null)
        {
            throw new ApplicationException($"Createlayer on {resultLayerName} failed");
        }

        switch (geoOperation)
        {
            case EGeoProcess.Intersection:
                _layer.Intersection(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    DefaultProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "Intersection");
                break;
            case EGeoProcess.Union:
                //https://gdal.org/api/ogrlayer_cpp.html?highlight=union#_CPPv4N8OGRLayer5UnionEP8OGRLayerP8OGRLayerPPc16GDALProgressFuncPv
                _layer.Union(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    UnionProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "Union");
                break;
            case EGeoProcess.SymmetricalDifference:
                _layer.SymDifference(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    DefaultProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "SymDifference");
                break;
            case EGeoProcess.Identity:
                _layer.Identity(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    DefaultProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "Identity");
                break;
            case EGeoProcess.Update:
                _layer.Update(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    DefaultProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "Update");
                break;
            case EGeoProcess.Clip:
                _layer.Clip(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    DefaultProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "Clip");
                break;
            case EGeoProcess.Erase:
                _layer.Erase(((OgctLayer)otherLayer)._layer, ((OgctLayer)tempInMemoryLayer)._layer,
                    DefaultProcessOptions, new Ogr.GDALProgressFuncDelegate(ProgressFunc), "Erase");
                break;
        }

        long cnt = tempInMemoryLayer.CopyToLayer(DataSource, resultLayerName, true);
        Console.WriteLine($"  -- result: copied {cnt} features into {resultLayerName} in {DataSource.Name}");

    }


    /// <summary>
    /// 
    /// https://gdal.org/api/ogrlayer_cpp.html?highlight=union#_CPPv4N8OGRLayer5UnionEP8OGRLayerP8OGRLayerPPc16GDALProgressFuncPv
    /// </summary>
    /// <param name="layers"></param>
    /// <param name="resultLayerName"></param>
    /// <param name="unionProcessOptions"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="ApplicationException"></exception>
    public void UnifyInSingleGpkg(IOgctLayer otherLayer, string? resultLayerName, string[] unionProcessOptions)
    {
        using (var resultLayer = DataSource.CreateAndOpenLayer(resultLayerName, GetSpatialRef(),
                   LayerDetails.GeomType, LayerDetails.Schema.FieldList, false))
        {
            _layer.Union(((OgctLayer)otherLayer)._layer, ((OgctLayer)resultLayer)._layer,
                unionProcessOptions, ProgressFunc, "Union");

        }
    }

    private static int ProgressFunc(double Complete, IntPtr Message, IntPtr Data)
    {
        return 1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="wkt"></param>
    public void SetSpatialFilterNotImplemented(string wkt)
    {
        wkt =
            "POLYGON ((-103.81402655265633 50.253951270672125,-102.94583419409656 51.535568561879401,-100.34125711841725 51.328856095555651,-100.34125711841725 51.328856095555651,-93.437060743203844 50.460663736995883,-93.767800689321859 46.450441890315041,-94.635993047881612 41.613370178339181,-100.75468205106476 41.365315218750681,-106.12920617548238 42.564247523428456,-105.96383620242338 47.277291755610058,-103.81402655265633 50.253951270672125))";
        _layer.SetSpatialFilter(Ogr.CreateGeometryFromWkt(ref wkt, new SpatialReference(wkt)));
    }

    public void Dispose()
    {
        _layer?.Dispose();
    }

    ~OgctLayer()
    {
        _layer?.Dispose();
    }
}

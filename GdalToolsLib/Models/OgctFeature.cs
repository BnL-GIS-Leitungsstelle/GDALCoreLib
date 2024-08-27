using System;
using System.Collections.Generic;
using GdalToolsLib.Exceptions;
using GdalToolsLib.Extensions;
using GdalToolsLib.Feature;
using GdalToolsLib.Geometry;
using GdalToolsLib.Layer;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Valid;
using OSGeo.OGR;

namespace GdalToolsLib.Models;

public class OgctFeature : IOgctFeature
{
    private readonly OSGeo.OGR.Feature _feature;
    private readonly OgctLayer _layer;

    public OgctFeature(OSGeo.OGR.Feature feature, IOgctLayer layer)
    {
        _feature = feature;
        _layer = (OgctLayer)layer;
    }

    internal OSGeo.OGR.Feature OgrFeature => _feature;

    public long FID => _feature.GetFID();
    public string? ObjNumber => _feature.ObjNumber(_layer.OgrLayer);
    public string? ObjName => _feature.ObjName(_layer.OgrLayer);

    /// <summary>
    /// 
    /// </summary>
    /// <returns>the object or null, if the feature has a null geometry</returns>
    public IOgctGeometry OpenGeometry()
    {
        if (_feature.GetGeometryRef() == null)
        {
            throw new GeometryCreationException("Referenced OGR Geometry is null.");
        }

        return new OgctGeometry(_feature.GetGeometryRef());
    }


    public void SetGeometry(IOgctGeometry other)
    {
        _feature.SetGeometry(((OgctGeometry)other).OgrGeometry);
    }

    public IOgctFeature CloneAndOpen()
    {
        return new OgctFeature(_feature.Clone(), _layer);
    }


    public wkbGeometryType GetGeomType()
    {
        using var geomRef = OgrFeature.GetGeometryRef();

        return geomRef.GetGeometryType();
    }

    /// <summary>
    ///  Validate that a feature meets constraints of its schema.
    /// </summary>
    /// <returns></returns>
    public bool ValidateSchemaConstraints()
    {
        //  @param nValidateFlags OGR_F_VAL_ALL or combination of OGR_F_VAL_NULL,
        //*OGR_F_VAL_GEOM_TYPE, OGR_F_VAL_WIDTH and OGR_F_VAL_ALLOW_NULL_WHEN_DEFAULT
        //    * with '|' operator

        bool passed = false;

        try
        {
            passed = OgrFeature.Validate((int) EOGR_F_VAL.OGR_F_VAL_ALL, 1) == 1;

            if (passed == false)
            {
                throw new NotImplementedException($"Validate failed in feature FID={FID}.");
            }
        }
        catch (Exception e)
        {
            throw new NotImplementedException($"Validate failed ({e.Message}) in feature FID={FID}.");
        }
        return true;
    }


public GeometryValidationResult ValidateGeometry()
{
    // Console.WriteLine($"Feature- fid= {_feature.GetFID()}");

    long fid = FID;

    IOgctGeometry geometry = null;

    try
    {
        geometry = OpenGeometry();
    }
    catch (GeometryCreationException ex)
    {
        if (ex.Message == "Referenced OGR Geometry is null.")
        {
            return new GeometryValidationResult(this, _layer, EGeometryValidationType.NullGeometry);
        }
    }

    string featureGeomType = geometry.Type;
    string layerGeometryType = _layer.LayerDetails.GeomType.ToString();

    // 1. check for empty geometries

    if (geometry.IsEmpty)
    {
        geometry.Dispose();
        return new GeometryValidationResult(this, _layer, EGeometryValidationType.EmptyGeometry);
    }

    if (geometry.GeometryCount == 0)
    {
        // if point or polyline
        geometry.Dispose();
        if (featureGeomType.StartsWith(wkbGeometryType.wkbPoint.ToString()) || featureGeomType.StartsWith(wkbGeometryType.wkbLineString.ToString()))
        {
            return new GeometryValidationResult(this, _layer, EGeometryValidationType.IsValid);
        }

        return new GeometryValidationResult(this, _layer, EGeometryValidationType.GeometryCounterZero);
    }

    bool isSimple;

    try
    {
        isSimple = geometry.IsSimple;
    }
    catch (Exception e)
    {
        if (e.Message.StartsWith("IllegalArgumentException"))
        {
            return new GeometryValidationResult(this, _layer, EGeometryValidationType.InvalidIsSimple, " Invalid number of points in LinearRing found 3 - must be 0 or >= 4.");

        }

        Console.WriteLine(e);
        throw;
    }


    if (isSimple == false)
    {
        // Linear geometries are simple if they do not self-intersect at points other than boundary points.
        if (featureGeomType.StartsWith(wkbGeometryType.wkbLineString.ToString()) ||
            featureGeomType.StartsWith(wkbGeometryType.wkbMultiLineString.ToString()))
        {
            geometry.Dispose();
            return new GeometryValidationResult(this, _layer, EGeometryValidationType.RingSelfIntersects, "Polyline has self-intersect points other than boundary points.");
        }

        // Zero - dimensional geometries(points) are simple if they have no repeated points.
        if (featureGeomType.StartsWith(wkbGeometryType.wkbPoint.ToString()) ||
            featureGeomType.StartsWith(wkbGeometryType.wkbMultiPoint.ToString()))
        {
            geometry.Dispose();
            return new GeometryValidationResult(this, _layer, EGeometryValidationType.RepeatedPoints, "Point has repeated points.");
        }
        // skip, if polygon
    }

    // 2. check for geometries of invalid type according to layer-type.
    // This is possible in some datasources, but has to be fixed before copying the
    // layer to another datasource-format, e.g FGDB to GPKG

    if (featureGeomType.StartsWith(wkbGeometryType.wkbMultiSurface.ToString()) &&
        !layerGeometryType.StartsWith(wkbGeometryType.wkbMultiSurface.ToString()))
    {
        geometry.Dispose();
        string? remarks = $"feature= {featureGeomType} in layer = {layerGeometryType}";
        return new GeometryValidationResult(this, _layer, EGeometryValidationType.FeatureToLayerMultiSurfaceTypeMismatch, remarks);
    }


    // 3. check for geometries have an invalid type according to the layer.
    if (featureGeomType.StartsWith(layerGeometryType) == false)
    {
        geometry.Dispose();
        string? remarks = $"layer = {layerGeometryType} - geom = {featureGeomType}";
        return new GeometryValidationResult(this, _layer, EGeometryValidationType.GeometrytypeMismatchAccordingToLayer, remarks);
    }

    // geometry seems valid
    var isGeometryValid = geometry.IsValid;

    // check, if NTS is vaild too
    var wktExport = geometry.GetWkt();
    var ntsValidationError = IsGeometryNTSValidationError(wktExport);

    if (isGeometryValid && ntsValidationError == null)
    {
        if ((featureGeomType.Contains("Point") || featureGeomType.Contains("Line")) == false)
        {
            double area = geometry.Area;
            geometry.Dispose();

            if (area < 1)
            {
                return new GeometryValidationResult(this, _layer, EGeometryValidationType.VerySmall, $"Geometry has small Area: {area} m²");
            }
        }

        return new GeometryValidationResult(this, _layer, EGeometryValidationType.IsValid);
    }

    if (ntsValidationError == null)
    {
        ntsValidationError = new TopologyValidationError(TopologyValidationErrors.HoleOutsideShell);
        // _log.Error($"Inconsistent Validation: NTS and GDAL disagree about validity of feature {result}");
    }

    geometry.Dispose();

    if (ntsValidationError.Message == "Ring Self-intersection")
    {
        return new GeometryValidationResult(this, _layer, EGeometryValidationType.RingSelfIntersects, $"at or near point {ntsValidationError.Coordinate}");
    }

    if (ntsValidationError.Message == "Nested shells")
    {
        return new GeometryValidationResult(this, _layer, EGeometryValidationType.NonSimpleGeometry, $"NTS has detected a non-simple feature");
    }


    // geometry is invalid, but reason is unspecified

    return new GeometryValidationResult(this, _layer, EGeometryValidationType.InvalidGeometryUnspecifiedReason, ntsValidationError.ToString());
}

private TopologyValidationError IsGeometryNTSValidationError(string wkt)
{
    var wktReader = new WKTReader();
    NetTopologySuite.Geometries.Geometry nktGeometry;
    try
    {
        nktGeometry = wktReader.Read(wkt);
    }
    catch (Exception)
    {
        return null!;
    }
    var isValidOp = new IsValidOp(nktGeometry);

    return isValidOp.ValidationError;
}

/// <summary>
/// 
/// </summary>
/// <param name="feature"></param>
/// <param name="fieldDef"></param>
/// <returns></returns>
/// <exception cref="NotImplementedException"></exception>
public dynamic ReadValue(FieldDefnInfo fieldDef)
{
    if (_feature.IsFieldNull(fieldDef.Name))
    {
        return null!;
    }

    switch (fieldDef.Type)
    {
        // is not implemented so far..
        // case FieldType.OFTBinary: return feature.GetFieldAsString(fieldDef.Name);
        // case FieldType.OFTWideString: return feature.GetFieldAsString(fieldDef.Name);
        // https://csharp.hotexamples.com/de/examples/-/OSGeo/GetFieldAsDateTime/php-osgeo-getfieldasdatetime-method-examples.html

        case FieldType.OFTTime:
            OgrFeature.GetFieldAsDateTime(fieldDef.OgrIndex, out int yearT, out int monthT, out int dayT,
                out int hourT, out int minuteT, out float secondT, out int flagT);

            return new TimeOnly(hourT, minuteT, Convert.ToInt32(secondT));

        case FieldType.OFTDateTime:

            OgrFeature.GetFieldAsDateTime(fieldDef.OgrIndex, out int year, out int month, out int day,
                out int hour, out int minute, out float second, out int flag);

            if (year == 0 && month == 0 && day == 0)
            {
                return DateTime.MinValue.AddMinutes(hour * 60 + minute);
            }

            return new DateTime(year, month, day, hour, minute, Convert.ToInt32(second));

        case FieldType.OFTDate:

            OgrFeature.GetFieldAsDateTime(fieldDef.OgrIndex, out int yearD, out int monthD, out int dayD,
                out int hourD, out int minuteD, out float secondD, out int flagD);

            return new DateOnly(yearD, monthD, dayD);

        case FieldType.OFTInteger: return OgrFeature.GetFieldAsInteger(fieldDef.Name);
        case FieldType.OFTIntegerList:
            int countInt;
            return OgrFeature.GetFieldAsIntegerList(fieldDef.OgrIndex, out countInt);
        case FieldType.OFTInteger64: return OgrFeature.GetFieldAsInteger64(fieldDef.Name);
        case FieldType.OFTReal: return OgrFeature.GetFieldAsDouble(fieldDef.Name);
        case FieldType.OFTRealList:
            int countReal;
            return OgrFeature.GetFieldAsDoubleList(fieldDef.OgrIndex, out countReal);
        case FieldType.OFTString:
        case FieldType.OFTStringList:
        case FieldType.OFTWideString:
        case FieldType.OFTWideStringList: return OgrFeature.GetFieldAsString(fieldDef.Name);

        default:
            throw new NotImplementedException($"Cannot handle Ogr Datatype {fieldDef.Type}");
    }
}

public FeatureRow ReadRow(List<FieldDefnInfo> fieldList)
{
    var row = new FeatureRow();

    foreach (var field in fieldList)
    {
        FeatureRowItem item = ReadValue(field);
        row.Items.Add(item);
    }

    return row;
}

/// <summary>
/// write value to field and feature to layer
/// </summary>
/// <param name="field"></param>
/// <param name="value"></param>
/// <returns></returns>
public FeatureFieldWriteResult WriteValue(FieldDefnInfo field, dynamic value)
{
    var result = SetValue(field, value);

    _layer.OgrLayer.SetFeature(OgrFeature);

    return result;
}

/// <summary>
/// write value to field
/// </summary>
/// <param name="field"></param>
/// <param name="value"></param>
/// <returns></returns>
public FeatureFieldWriteResult SetValue(FieldDefnInfo field, dynamic value)
{
    var result = new FeatureFieldWriteResult(FID);

    const int flag = 0; // unknown timezone is default to cast DateTime into DateTime-attribute

    if (value == null)
    {
        if (field.IsNullable)
        {
            OgrFeature.SetFieldNull(field.Name);
            result.ResultValidationType = EFieldWriteErrorType.IsValid;
        }
        else
        {
            result.ResultValidationType = EFieldWriteErrorType.InvalidNullCast;
        }
    }
    else
    {
        switch (field.Type)
        {
            case FieldType.OFTInteger:
            case FieldType.OFTInteger64:
            case FieldType.OFTReal:
            case FieldType.OFTString:
                OgrFeature.SetField(field.Name, ConvertValueType(value, field, result));
                break;

            case FieldType.OFTDate:
            case FieldType.OFTDateTime:
                var dtValue = ConvertValueType(value, field, result);

                OgrFeature.SetField(field.Name, dtValue.Year, dtValue.Month, dtValue.Day,
                    dtValue.Hour, dtValue.Minute, dtValue.Second, flag);
                break;

            default:
                throw new NotImplementedException($"Cast to field type {field.Type} not implemented");
        }
    }
    // feature.FillUnsetWithDefault();
    // Validate that a feature meets constraints of its schema.

    //  @param nValidateFlags OGR_F_VAL_ALL or combination of OGR_F_VAL_NULL,
    //*OGR_F_VAL_GEOM_TYPE, OGR_F_VAL_WIDTH and OGR_F_VAL_ALLOW_NULL_WHEN_DEFAULT
    //    * with '|' operator

    //bool passed = OgrFeature.Validate((int)EOGR_F_VAL.OGR_F_VAL_ALL, 1) == 1;

    //if (passed == false)
    //{
    //    throw new NotImplementedException($"Validate failed in field {field.Name} with value= {value}.");
    //}

    return result;
}

/// <summary>
/// converts the value type into the type of the field, in which it is stored
/// </summary>
/// <param name="value"></param>
/// <param name="field"></param>
/// <param name="writeResult"></param>
/// <returns></returns>
private dynamic ConvertValueType(dynamic value, FieldDefnInfo field, FeatureFieldWriteResult writeResult)
{
    if (value == null) return null!;

    switch (field.Type)
    {
        case FieldType.OFTInteger:

            try
            {
                int intValue = Convert.ToInt32(value);

                double dblValue = Convert.ToDouble(value);

                if (dblValue - intValue > 0.001)
                {
                    writeResult.ResultValidationType = EFieldWriteErrorType.LossOfFractionInInteger;
                    return intValue;
                }

                writeResult.ResultValidationType = EFieldWriteErrorType.IsValid;
                return intValue;
            }
            catch (InvalidCastException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.InvalidCast;
            }
            catch (OverflowException)  // too big values for int
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.OverflowInCast;
            }
            catch (FormatException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.CastFailureDueToFalseFormat;
            }
            break;

        case FieldType.OFTInteger64:
            try
            {
                long longValue = Convert.ToInt64(value);

                double dblValue = Convert.ToDouble(value);

                if (dblValue - longValue > 0.001)
                {
                    writeResult.ResultValidationType = EFieldWriteErrorType.LossOfFractionInInteger;
                    return longValue;
                }

                writeResult.ResultValidationType = EFieldWriteErrorType.IsValid;
                return longValue;
            }
            catch (InvalidCastException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.InvalidCast;
            }
            catch (FormatException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.CastFailureDueToFalseFormat;
            }
            break;

        case FieldType.OFTReal:
            try
            {
                double dblValue = Convert.ToDouble(value);
                writeResult.ResultValidationType = EFieldWriteErrorType.IsValid;
                return dblValue;
            }
            catch (InvalidCastException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.InvalidCast;
            }
            catch (FormatException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.CastFailureDueToFalseFormat;
            }
            break;

        case FieldType.OFTDate:
        case FieldType.OFTDateTime:
            try
            {
                var dtValue = Convert.ToDateTime(value);
                writeResult.ResultValidationType = EFieldWriteErrorType.IsValid;
                return dtValue;
            }
            catch (InvalidCastException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.InvalidCast;
            }
            catch (FormatException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.CastFailureDueToFalseFormat;
            }
            break;

        case FieldType.OFTString:
        case FieldType.OFTWideString:
        case FieldType.OFTStringList:
        case FieldType.OFTWideStringList:
            try
            {
                var strValue = Convert.ToString(value);
                writeResult.ResultValidationType = EFieldWriteErrorType.IsValid;

                if (strValue.Length > field.Width)
                {
                    writeResult.ResultValidationType = EFieldWriteErrorType.StringLengthExceedsTargetTypeLength;
                    return null!;
                }
                return strValue;
            }
            catch (InvalidCastException)
            {
                writeResult.ResultValidationType = EFieldWriteErrorType.InvalidCast;
            }
            break;
        default:
            throw new InvalidCastException($"Cast missing for field {field.Name}, type {field.Type}");

    }
    return null!;
}

public string GetFieldAsString(string fieldName)
{
    return _feature.GetFieldAsString(fieldName);
}

public void Dispose()
{
    _feature?.Dispose();
}

~OgctFeature()
{
    _feature?.Dispose();
}
}
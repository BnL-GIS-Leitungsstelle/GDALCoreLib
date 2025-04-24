using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GdalToolsLib.Feature;
using OSGeo.OGR;

namespace GdalToolsLib.Layer;

public class LayerSchema
{
    /// <summary>
    /// Name of the key-field in the DB
    /// </summary>
    public string? FidColumnName { get; private set; }

    /// <summary>
    /// Name of the geometry-field in the DB
    /// </summary>
    public string? GeometryColumnName { get; private set; }

    /// <summary>
    /// List of all fields, with attributes
    /// </summary>
    public List<FieldDefnInfo> FieldList { get; }

    /// <summary>
    /// representation as JSON
    /// </summary>
    public string Json { get; }

    public LayerSchema(OSGeo.OGR.Layer layer)
    {
        FieldList = new List<FieldDefnInfo>();

        GetSchema(layer);

        Json = JsonSerializer.Serialize(FieldList, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// for testing purposes
    /// </summary>
    public LayerSchema()
    {
        FieldList = new List<FieldDefnInfo>();
    }

    public bool IsEqual(LayerSchema? otherSchema)
    {
        if (FieldList.Count != otherSchema.FieldList.Count) return false;
        return Compare(otherSchema) == String.Empty;
    }

    public string Compare(LayerSchema? otherSchema)
    {
        string result = String.Empty;

        for (int i = 0; i < FieldList.Count; i++)
        {
            if (FieldList[i].Name != otherSchema.FieldList[i].Name)
            {
                result += $"Field name difference in {i+1}. position: {FieldList[i].Name} and {otherSchema.FieldList[i].Name}";
            }

            if (FieldList[i].Type != otherSchema.FieldList[i].Type)
            {
                result += $"Field type difference in {i + 1}. position: {FieldList[i].TypeName} and {otherSchema.FieldList[i].TypeName}";
            }
        }
        return result;
    }

    public FieldDefnInfo InsertField(string name, string type, string width, string isNullable, string isUnique)
    {
        FieldType fieldType = (FieldType)Enum.Parse(typeof(FieldType), "OFT"+ type);
      

        FieldDefnInfo fieldDef = new FieldDefnInfo(name, fieldType, Convert.ToInt32(width), Convert.ToBoolean(isNullable), Convert.ToBoolean(isUnique));

        FieldList.Add(fieldDef);

        return fieldDef;
    }


    public void RemoveStandardShape_Area_and_Shape_Length_FieldsIfPresent()
    {
        string shapeAreaName = "Shape_Area";
        string shapeLengthName = "Shape_Length";

        if (HasField(shapeAreaName))
        {
            FieldList.Remove(FieldList.First(_ => _.Name.ToUpper() == shapeAreaName.ToUpper()));
        }

        if (HasField(shapeLengthName))
        {
            FieldList.Remove(FieldList.First(_ => _.Name.ToUpper() == shapeLengthName.ToUpper()));
        }

    }

    public bool RemoveField(string name)
    {
        if (HasField(name))
        {
            FieldList.Remove(FieldList.First(_ => _.Name == name));
            return true;
        }
        else
        {
            return false;
        }
    }

    public void GetSchema(OSGeo.OGR.Layer layer)
    {
        FidColumnName = layer.GetFIDColumn();
        GeometryColumnName = layer.GetGeometryColumn();

        for (int j = 0; j < layer.GetLayerDefn().GetFieldCount(); j++)
        {
            using (var field = layer.GetLayerDefn().GetFieldDefn(j))
            {
                FieldList.Add(new FieldDefnInfo(j, field));
            }
        }
    }

    public bool HasField(string fieldName)
    {
        return FieldList.Any(_ => _.Name.ToUpper() == fieldName.ToUpper());
    }

    public bool HasField(FieldDefnInfo field)
    {
        return HasField(field.Name);
    }

    public FieldDefnInfo GetField(string fieldName)
    {
        return FieldList.Find(_ => _.Name == fieldName);
    }


    /// <summary>
    /// field is neither geometry- nor key-field
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public bool IsEditable(string fieldName)
    {
        return FidColumnName != fieldName && GeometryColumnName != fieldName;
    }

    public bool IsEditable(FieldDefnInfo field)
    {
        return IsEditable(field.Name);
    }

    /// <summary>
    /// checks, if value can be cast into target data type
    /// </summary>
    /// <param name="targetField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public EFieldWriteErrorType CanValueBeCastToFieldDataType(FieldDefnInfo targetField, object value)
    {
        if (value == null)
        {
            return EFieldWriteErrorType.IsValid;
        }
        switch (targetField.Type)
        {
            case FieldType.OFTInteger:
                // possible enhancement: https://stackoverflow.com/questions/5788883/how-can-i-convert-a-datetime-to-an-int

                try
                {
                    int intValue = Convert.ToInt32(value);

                    double dblValue = Convert.ToDouble(value);

                    if (Math.Abs(dblValue - intValue) > 0.001)
                    {
                        return EFieldWriteErrorType.LossOfFractionInInteger;
                    }

                    return EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }
                catch (OverflowException)  // too big values for int
                {
                    return EFieldWriteErrorType.OverflowInCast;
                }
                catch (FormatException)
                {
                    return EFieldWriteErrorType.CastFailureDueToFalseFormat;
                }

            case FieldType.OFTInteger64:
                // possible enhancement: https://stackoverflow.com/questions/5788883/how-can-i-convert-a-datetime-to-an-int

                try
                {
                    long lngValue = Convert.ToInt64(value);

                    double dblValue = Convert.ToDouble(value);

                    if (Math.Abs(dblValue - lngValue) > 0.001)
                    {
                        return EFieldWriteErrorType.LossOfFractionInInteger;
                    }

                    return EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }
                catch (FormatException)
                {
                    return EFieldWriteErrorType.CastFailureDueToFalseFormat;
                }

            case FieldType.OFTReal:

                try
                {
                    double dblValue = Convert.ToDouble(value);
                    return EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }
                catch (FormatException)
                {
                    return EFieldWriteErrorType.CastFailureDueToFalseFormat;
                }

            case FieldType.OFTDateTime:

                try
                {
                    var dtValue = Convert.ToDateTime(value);
                    return EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }
                catch (FormatException)
                {
                    return EFieldWriteErrorType.CastFailureDueToFalseFormat;
                }

            case FieldType.OFTDate:

                try
                {
                    var dtValue = Convert.ToDateTime(value);
                    return EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }
                catch (FormatException)
                {
                    return EFieldWriteErrorType.CastFailureDueToFalseFormat;
                }

            case FieldType.OFTString:

                try
                {
                    var strValue = Convert.ToString(value);

                    return strValue.Length > targetField.Width ?
                        EFieldWriteErrorType.StringLengthExceedsTargetTypeLength : EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }

            case FieldType.OFTBinary:

                try
                {
                    var byteValue = new BitArray(bytes: new byte[] { (byte)value });

                    return EFieldWriteErrorType.IsValid;
                }
                catch (InvalidCastException)
                {
                    return EFieldWriteErrorType.InvalidCast;
                }

            default:
                throw new NotSupportedException("Cast is not supported");
        }
    }
}
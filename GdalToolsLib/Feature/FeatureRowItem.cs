using System;
using OSGeo.OGR;

namespace GdalToolsLib.Feature;

public class FeatureRowItem
{
    internal int intValue;
    internal double dblValue;
    internal string stringValue;
    internal DateTime dateTimeValue;
    internal DateOnly dateValue;
    internal TimeOnly timeValue;


    public FieldType fieldType;

    public object Value
    {
        get
        {
            switch (fieldType)
            {
                case FieldType.OFTInteger:
                    return intValue;
                case FieldType.OFTReal:
                    return dblValue;

                case FieldType.OFTString:
                    return stringValue;
                case FieldType.OFTDateTime:
                    return dateTimeValue;
                case FieldType.OFTDate:
                    return dateValue;
                case FieldType.OFTTime:
                    return timeValue;
                //case FieldType.OFTBinary:
                //    return charValue;
                default:
                    return null;
            }
        }
    }

    // Implicit construction
    public static implicit operator FeatureRowItem(int i)
    {
        return new FeatureRowItem { intValue = i, fieldType = FieldType.OFTInteger };
    }
    public static implicit operator FeatureRowItem(string s)
    {
        return new FeatureRowItem { stringValue = s, fieldType = FieldType.OFTString };
    }
    public static implicit operator FeatureRowItem(DateTime dt)
    {
        return new FeatureRowItem { dateTimeValue = dt, fieldType = FieldType.OFTDateTime };
    }
    public static implicit operator FeatureRowItem(DateOnly dto)
    {
        return new FeatureRowItem { dateValue = dto, fieldType = FieldType.OFTDate };
    }
    public static implicit operator FeatureRowItem(TimeOnly tmo)
    {
        return new FeatureRowItem { timeValue = tmo, fieldType = FieldType.OFTTime };
    }
    public static implicit operator FeatureRowItem(double dbl)
    {
        return new FeatureRowItem { dblValue = dbl, fieldType = FieldType.OFTReal };
    }

    // Implicit value reference
    public static implicit operator int(FeatureRowItem item)
    {
        if (item.fieldType != FieldType.OFTInteger) // Optionally, you could validate the usage
        {
            throw new InvalidCastException("Trying to use a " + item.fieldType + " as an int");
        }
        return item.intValue;
    }
    public static implicit operator string(FeatureRowItem item)
    {
        return item.stringValue;
    }
    public static implicit operator DateTime(FeatureRowItem item)
    {
        return item.dateTimeValue;
    }
    public static implicit operator double(FeatureRowItem item)
    {
        return item.dblValue;
    }

    public string ValueToString()
    {
        string strValue = String.Empty;

        if (Value != null)
        {
            if (fieldType == FieldType.OFTDateTime)
            {
                strValue = ((DateTime)Value).ToString("dd.MM.yyyy");
            }
            else
            {
                strValue = Value.ToString();
            }
        }

        return strValue;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
using System;
using System.Collections.Generic;
using GdalToolsLib.Layer;

namespace GdalToolsLib.Feature;

public class FeatureRow
{
    public List<FeatureRowItem> Items { get; set; }

    public FeatureRow()
    {
        Items = new List<FeatureRowItem>();
    }

    public FeatureComparisonResult Compare(FeatureRow otherRow, List<FieldDefnInfo> fieldList, string identifierFieldName)
    {
        string identifierValue = String.Empty;

        for (int i = 0; i < fieldList.Count; i++)
        {
            if (fieldList[i].Name == identifierFieldName)
            {
                identifierValue = Items[i].Value.ToString();
            }
        }
        var result = new FeatureComparisonResult(identifierFieldName, identifierValue);


        for (int i = 0; i < Items.Count; i++)
        {
            string self = String.Empty;
            string other = String.Empty;

            if (Items[i] != null) self = Items[i].ValueToString();
            if (otherRow.Items[i] != null) other = otherRow.Items[i].ValueToString();

            if (self != other)
            {
                result.AddFieldDifference(self, other, fieldList[i].Name);
            }
        }

        return result;
    }

}
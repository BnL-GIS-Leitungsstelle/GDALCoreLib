using System;
using System.Collections.Generic;

using OGCToolsNetCoreLib.Extensions;
using OGCToolsNetCoreLib.Layer;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Helpers
{
    public static class QueryHelpers
    {
        /// <summary>
        /// constructs a where clause fron the given condition(s)
        /// </summary>
        /// <param name="conditions"></param>
        /// <returns></returns>
        public static string BuildWhereClause(List<WhereClauseCondition> conditions)
        {
            // For instance: "population > 1000000 and population < 5000000"
            string whereClause = String.Empty;

            foreach (var item in conditions)
            {
                if (String.IsNullOrWhiteSpace(item.Content)) continue;  // skip empty content 

                if (whereClause.Length > 0) whereClause += " AND ";

                // DesignatTy = 'NDA' AND Inkraftset = 01.10.1994 00:00:00
                string maskedContent;

                switch (item.Field.Type)
                {
                    case FieldType.OFTString:
                        maskedContent = $"'{item.Content.Replace("'", "''")}'";  // mask colons in text
                        break;
                    case FieldType.OFTDateTime:
                        // To use in Geopackage 
                        var dtToSqliteText = Convert.ToDateTime(item.Content).ToString("yyyy-MM-ddTHH:mm:ss");
                        //var dtToSqliteText = Convert.ToDateTime(item.Content).ToString("yyyy-MM-dd");
                        maskedContent = $"'{dtToSqliteText}'";
                        break;
                    case FieldType.OFTDate:
                        var dateOnlyText = Convert.ToDateTime(item.Content).ToString("yyyy-MM-dd");
                        maskedContent = $"'{dateOnlyText}'";
                        break;
                    case FieldType.OFTTime:
                        var timeOnlyText = Convert.ToDateTime(item.Content).ToString("HH:mm:ss");
                        maskedContent = $"'{timeOnlyText}'";
                        break;
                    default:
                        maskedContent = item.Content;
                        break;
                }

                whereClause += $"{item.Field.Name} {item.CompareSign.GetEnumDescription(typeof(ECompareSign))} {maskedContent}";
            }

            return whereClause;
        }
    }
}

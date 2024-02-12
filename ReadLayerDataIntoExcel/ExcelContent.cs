using System.Collections.Generic;
using System.IO;
using GdalToolsLib.Feature;
using GdalToolsLib.Layer;

namespace ReadLayerDataIntoExcel;

public class ExcelContent
{
    public List<FeatureRow> FeatureRows { get; private set; }

    public string Filename { get; private set; }

    public List<FieldDefnInfo> FieldList { get; private set; }

    public ExcelContent(LayerDetails layerInfo, List<FeatureRow> rows)
    {
            FeatureRows = rows;

            Filename = Path.Combine(Directory.GetParent(layerInfo.DataSourceFileName).FullName, $"{layerInfo.Name}.xlsx" );

            FieldList = layerInfo.Schema.FieldList;
        }


}
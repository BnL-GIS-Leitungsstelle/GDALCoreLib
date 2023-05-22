using System.Collections.Generic;
using System.IO;
using OGCToolsNetCoreLib.Feature;
using OGCToolsNetCoreLib.Layer;

namespace ReadLayerData
{
    public class ExcelContent
    {
        private LayerDetails layerInfo;

        public List<FeatureRow> FeatureRows { get; private set; }

        public string Filename { get; private set; }

        public List<FieldDefnInfo> FieldList { get; private set; }

        public ExcelContent(LayerDetails layerInfo, List<FeatureRow> rows)
        {
            this.layerInfo = layerInfo;
            FeatureRows = rows;

            Filename = Path.Combine(Directory.GetParent(layerInfo.DataSourceFileName).FullName, $"{layerInfo.Name}.xlsx" );

            FieldList = this.layerInfo.Schema.FieldList;
        }


    }
}

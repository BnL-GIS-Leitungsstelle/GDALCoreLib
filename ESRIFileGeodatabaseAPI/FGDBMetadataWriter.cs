using Esri.FileGDB;

namespace ESRIFileGeodatabaseAPI
{
    public class FGDBMetadataWriter
    {
        public static void WriteLayerMetadata(string fgdbPath, string layerName, string metadata)
        {
            using var db = Geodatabase.Open(fgdbPath);
            using var table = db.OpenTable(layerName);
            table.Documentation = metadata;
            table.Close();
            db.Close();
        }

        /// <summary>
        /// This method copies the metadata from the source data to all layers in the destination data. 
        /// It works for layers that have the same name in both files.
        /// </summary>
        public static void CopyMetadataForAllLayers(string sourceFgdbPath, string destinationFgdbpath)
        {
            using var source = Geodatabase.Open(sourceFgdbPath);

            // create a dict with the layername as key and the metadata as the value
            var layernameDocumentationMap = source.GetChildDatasets("\\", "").ToDictionary(name => name.Trim('\\'), name =>
            {
                using var table = source.OpenTable(name);
                return table.Documentation;
            });

            WriteMetadata(destinationFgdbpath, layernameDocumentationMap);
        }

        public static void WriteMetadata(string fgdbPath, IDictionary<string, string> layerMetadataMappings)
        {
            using var gdb = Geodatabase.Open(fgdbPath);

            foreach (var layerName in gdb.GetChildDatasets("\\", ""))
            {
                var escapedName = layerName.Trim('\\');
                if (layerMetadataMappings.TryGetValue(escapedName, out string? value))
                {
                    using var table = gdb.OpenTable(escapedName);
                    table.Documentation = value;
                }
            }
        }
    }
}

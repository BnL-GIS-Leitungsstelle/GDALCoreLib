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
        /// <param name="sourceFgdbPath"></param>
        /// <param name="destinationFgdbpath"></param>
        public static void CopyMetadataForAllLayers(string sourceFgdbPath, string destinationFgdbpath)
        {
            using var source = Geodatabase.Open(sourceFgdbPath);
            using var destination = Geodatabase.Open(destinationFgdbpath);

            // create a dict with the layername as key and the documentation as the value
            var layernameDocumentationMap = source.GetChildDatasets("\\", "").ToDictionary(name => name, name =>
            {
                using var table = source.OpenTable(name);
                return table.Documentation;
            });
            
            foreach (var layerName in destination.GetChildDatasets("\\", ""))
            {
                if (layernameDocumentationMap.TryGetValue(layerName, out string? value))
                {
                    using var table = destination.OpenTable(layerName);
                    table.Documentation = value;
                }
            }
        }
    }
}

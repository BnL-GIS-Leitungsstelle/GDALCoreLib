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
    }
}

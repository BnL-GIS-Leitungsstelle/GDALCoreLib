namespace GdalToolsLib.GeoProcessor;

public class UnionProcessItem
{
    public string InputLayerName { get; set; }

    public string MethodLayerName { get; set; }

    public string ResultLayerName { get; set; }

    public bool IsTemporary { get; set; }

    public UnionProcessItem(string inputLayerName, string methodLayerName, string resultLayerName, bool isTemporary)
    {
        InputLayerName = inputLayerName;

        MethodLayerName = methodLayerName;

        IsTemporary = isTemporary;

        ResultLayerName = IsTemporary ? $"{resultLayerName}TempUnion" : resultLayerName;
    }
}

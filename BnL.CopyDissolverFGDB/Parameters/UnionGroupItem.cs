namespace BnL.CopyDissolverFGDB.Parameters;

public class UnionGroupItem
{
    public string FileName { get; set; }
    public string LayerName { get; set; }
    public string LayerNameOther { get; set; }
    public string ResultLayerName { get; set; }

    public UnionGroupItem(string fileName, string layerName, string layerNameOther, string resultLayerName)
    {
        FileName= fileName;
        LayerName= layerName;
        LayerNameOther= layerNameOther;
        ResultLayerName= resultLayerName;
    }
}



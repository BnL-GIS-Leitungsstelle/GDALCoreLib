namespace GdalToolsLib.Layer;

public class LayerNameKnownContent
{
    public ELegalState LegalState { get; set; }

    /// <summary>
    /// the sub category, like Anhang
    /// </summary>
    public ESubCategory SubCategory { get; set; }

    /// <summary>
    /// the main category
    /// </summary>
    public ECategory Category { get; set; }
}
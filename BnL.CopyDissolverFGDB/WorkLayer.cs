using GdalToolsLib.Layer;

namespace BnL.CopyDissolverFGDB;

/// <summary>
/// Represents a layer (name, status), that is managed by the Worklist
/// </summary>
public class WorkLayer
{
    public string FileName { get; }

    public string LayerName { get; }

    public LayerNameBafuContent LayerContentInfo { get; set; }

    public EWorkState WorkState { get; set; }

    public WorkLayer(LayerDetails layerDetails, EWorkState workState)
    {
        FileName = layerDetails.DataSourceFileName;
        LayerName = layerDetails.Name;
        WorkState = workState;

        LayerContentInfo = new LayerNameBafuContent(LayerName);
    }


    public override string ToString()
    {
        return $"{WorkState}: {LayerName} {FileName}; {LayerContentInfo.Year} {LayerContentInfo.LegalState} {LayerContentInfo.Category} {LayerContentInfo.FormatErrorInfo}";
    }
}
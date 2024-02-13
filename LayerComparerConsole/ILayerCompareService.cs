namespace LayerComparerConsole;

public interface ILayerCompareService
{
    void ShowAbout();
    void Compare(string file1, string layer1, string file2, string layer2);

}
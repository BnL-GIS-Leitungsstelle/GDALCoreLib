using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using OSGeo.OGR;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace LayerComparer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public ObservableCollection<LayerInfo> LayersLeft { get; } = [];
    public ObservableCollection<LayerInfo> LayersRight { get; } = [];

    public MainWindow()
    {
        //DataContext = this;
        var testPath = "D:\\Daten\\MMO\\temp\\CopyDissolverTest\\Stand_20250320\\Auengebiete.gdb";
        var testPath2 = "G:\\BnL\\Daten\\Ablage\\DNL\\Bundesinventare\\Auengebiete\\Auengebiete.gdb";
        //var testPath2 = "D:\\Daten\\MMO\\temp\\CopyDissolverTest\\Old\\Auengebiete.gdb";

        using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(testPath);
        using var ds2 = new OgctDataSourceAccessor().OpenOrCreateDatasource(testPath2);


        LayersLeft = [.. ds.GetLayers().Select(GetLayerInfo).OrderBy(l => l.Name)];
        LayersRight = [.. ds2.GetLayers().Select(GetLayerInfo).OrderBy(l => l.Name)];

        InitializeComponent();

    }

    public static LayerInfo GetLayerInfo(IOgctLayer layer)
    {
        return new LayerInfo { Name = layer.Name, GeometryType = layer.LayerDetails.GeomType, FeatureCount = layer.LayerDetails.FeatureCount, TotalArea = 0, Fields = layer.LayerDetails.Schema!.FieldList };
    }

    private void Compare_Click(object sender, RoutedEventArgs e)
    {
        var res = dgLayersLeft.dataGrid.SelectedItems.Cast<LayerInfo>();
        var res1 = dgLayersRight.dataGrid.SelectedItems.Cast<LayerInfo>();
        var dialogResult = new OrderByFieldsDialog(res, res1).ShowDialog();

        //lbCompare.ItemsSource = intersection;
        //MessageBox.Show(string.Join(',', intersection));
    }
}

public class LayerInfo
{
    public required string Name { get; set; }
    public required wkbGeometryType GeometryType { get; set; }
    public required long FeatureCount { get; set; }
    public required int TotalArea { get; set; }
    public required List<FieldDefnInfo> Fields { get; set; }
}
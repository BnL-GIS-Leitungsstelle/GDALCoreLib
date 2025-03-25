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

    public ObservableCollection<string[]> OrderByFields { get; private set; } = [];

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

        LayersLeft.CollectionChanged += Layers_CollectionChanged;
        LayersRight.CollectionChanged += Layers_CollectionChanged;
        InitializeComponent();

    }

    private void Layers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        int len = Math.Min(LayersLeft.Count, LayersRight.Count);
        OrderByFields.Clear();
        for (int i = 0; i < len; i++)
        {
            var intersection = LayersRight[i].Fields.Select(l => l.Name).Intersect(LayersLeft[i].Fields.Select(l => l.Name));
            OrderByFields.Add(intersection.ToArray());
        }
    }

    public static LayerInfo GetLayerInfo(IOgctLayer layer)
    {
        return new LayerInfo { Name = layer.Name, GeometryType = layer.LayerDetails.GeomType, FeatureCount = layer.LayerDetails.FeatureCount, TotalArea = 0, Fields = layer.LayerDetails.Schema!.FieldList };
    }

    private void Compare_Click(object sender, RoutedEventArgs e)
    {
        var intersection = LayersRight.Select(l => l.Name).Intersect(LayersLeft.Select(l => l.Name));
        //lbCompare.ItemsSource = intersection;
        //MessageBox.Show(string.Join(',', intersection));
    }
}

public class LayerInfo
{
    public bool Expanded { get; set; }
    public required string Name { get; set; }
    public required wkbGeometryType GeometryType { get; set; }
    public required long FeatureCount { get; set; }
    public required int TotalArea { get; set; }
    public required List<FieldDefnInfo> Fields { get; set; }
}
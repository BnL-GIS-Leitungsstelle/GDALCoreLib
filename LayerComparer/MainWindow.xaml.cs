using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using LayerComparerConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OSGeo.OGR;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace LayerComparer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string datasourceOne;
    private string datasourceTwo;

    public ObservableCollection<LayerInfo> LayersLeft { get; } = [];
    public ObservableCollection<LayerInfo> LayersRight { get; } = [];

    public MainWindow()
    {
        datasourceOne = "D:\\Daten\\MMO\\temp\\CopyDissolverTest\\Stand_20250320\\Auengebiete.gdb";
        datasourceTwo = "G:\\BnL\\Daten\\Ablage\\DNL\\Bundesinventare\\Auengebiete\\Auengebiete.gdb";

        using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(datasourceOne);
        using var ds2 = new OgctDataSourceAccessor().OpenOrCreateDatasource(datasourceTwo);


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
        var layersLeft = dgLayersLeft.dataGrid.SelectedItems.Cast<LayerInfo>();
        var layersRight = dgLayersRight.dataGrid.SelectedItems.Cast<LayerInfo>();
        var dialog = new OrderByFieldsDialog(layersLeft, layersRight);

        if (dialog.ShowDialog() == true)
        {
            OutputTab.IsEnabled = true;
            TabControl.SelectedIndex = 1;

            var hostBuilder = Host.CreateDefaultBuilder();

            hostBuilder.ConfigureServices(s => s.AddTransient<ILayerCompareService, LayerCompareService>());

            hostBuilder.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration)
                    .WriteTo.Sink(new WpfTextBoxSink(OutputBox)));

            var host = hostBuilder.Build();

            var layerComparer = host.Services.GetService<ILayerCompareService>()!;

            foreach (var pair in dialog.LayerComparisonPairs)
            {
                var orderByFields = pair.SharedFields.Where(f => f.Selected).Select(f => f.Name);
                layerComparer.Compare(datasourceOne, pair.LayerOne, datasourceTwo, pair.LayerTwo, orderByFields);
            }
        }
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
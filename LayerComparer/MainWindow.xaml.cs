using CommunityToolkit.Mvvm.ComponentModel;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using LayerComparerConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OSGeo.OGR;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LayerComparer;

[INotifyPropertyChanged]
public partial class MainWindow : Window
{
    [ObservableProperty]
    public ObservableCollection<LayerInfo> layersLeft = [];

    [ObservableProperty]
    public ObservableCollection<LayerInfo> layersRight = [];

    [ObservableProperty]
    private string datasourceOne = "";

    [ObservableProperty]
    private string datasourceTwo = "";

    public MainWindow()
    {
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
                layerComparer.Compare(DatasourceOne, pair.LayerOne, DatasourceTwo, pair.LayerTwo, orderByFields);
            }
        }
    }

    private void LoadSources_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(DatasourceOne);
            using var ds2 = new OgctDataSourceAccessor().OpenOrCreateDatasource(DatasourceTwo);

            LayersLeft = [.. ds.GetLayers().OrderBy(l => l.Name).Select(GetLayerInfo)];
            LayersRight = [.. ds2.GetLayers().OrderBy(l => l.Name).Select(GetLayerInfo)];
        }
        catch
        {
            MessageBox.Show("Failed to open datasource.");
            return;
        }

    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private void TextBoxLeft_Drop(object sender, DragEventArgs e)
    {
        var data = (string[])e.Data.GetData(DataFormats.FileDrop);
        DatasourceOne = data[0];
    }

    private void TextBoxRight_Drop(object sender, DragEventArgs e)
    {
        var data = (string[])e.Data.GetData(DataFormats.FileDrop);
        DatasourceTwo = data[0];
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
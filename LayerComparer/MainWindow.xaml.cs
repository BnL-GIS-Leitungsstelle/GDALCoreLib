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

namespace LayerComparer;

[INotifyPropertyChanged]
public partial class MainWindow : Window
{
    [ObservableProperty] private ObservableCollection<LayerInfo> layersLeft = [];

    [ObservableProperty] private ObservableCollection<LayerInfo> layersRight = [];

    [ObservableProperty] private string datasourceOne = "";

    [ObservableProperty] private string datasourceTwo = "";

    public MainWindow()
    {
        InitializeComponent();
    }

    private static LayerInfo GetLayerInfo(IOgctLayer layer)
    {
        return new LayerInfo
        {
            Name = layer.Name, GeometryType = layer.LayerDetails.GeomType,
            FeatureCount = layer.LayerDetails.FeatureCount, TotalArea = 0, Fields = layer.LayerDetails.Schema!.FieldList
        };
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
        }
    }

    private async void Compare_Click(object sender, RoutedEventArgs e)
    {
        var layersLeft = DgLayersLeft.dataGrid.SelectedItems.Cast<LayerInfo>();
        var layersRight = DgLayersRight.dataGrid.SelectedItems.Cast<LayerInfo>();

        if (!layersLeft.Any() || !layersRight.Any())
        {
            MessageBox.Show("You must select at least one layer on both sides");
            return;
        }

        var dialog = new OrderByFieldsDialog(layersLeft, layersRight);

        if (dialog.ShowDialog() != true) return;

        OutputTab.IsEnabled = true;
        TabControl.SelectedIndex = 1;

        var hostBuilder = Host.CreateDefaultBuilder();

        hostBuilder.ConfigureServices(s => s.AddTransient<ILayerCompareService, LayerCompareService>());

        hostBuilder.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration)
            .WriteTo.Sink(new WpfTextBoxSink(OutputBox, OutputScrollViewer)));

        var host = hostBuilder.Build();

        var layerComparer = host.Services.GetService<ILayerCompareService>()!;

        // run as async task, so the UI can update in the meantime
        await Task.Run(() =>
        {
            foreach (var pair in dialog.DataGrid.Items.Cast<OrderByFieldsDialog.ComparisonPair>())
            {
                var orderByFields = pair.SharedFields.Where(f => f.Selected).Select(f => f.Name);
                layerComparer.Compare(DatasourceOne, pair.LayerOne, DatasourceTwo, pair.LayerTwo, orderByFields);
            }
        });
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
    public required string Name { get; init; }
    public required wkbGeometryType GeometryType { get; init; }
    public required long FeatureCount { get; init; }
    public required int TotalArea { get; init; }
    public required List<FieldDefnInfo> Fields { get; init; }
}
using System.Windows;
using GdalToolsLib.Workflows;

namespace BnL.JB_WTSCHG_MASTER_SeparateLayerBuilder.Gui;

public partial class MainWindow : Window
{
    private void LogChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ((System.Windows.Controls.TextBox)sender).ScrollToEnd();

    public MainWindow()
    {
        InitializeComponent();
        var model = new MainViewModel(new MasterLayerSeparator(), new SeparationDialogs());
        DataContext = model;
        Closing += (_, e) => e.Cancel = model.Busy;
    }
}


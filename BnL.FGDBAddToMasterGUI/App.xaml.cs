using System;
using System.Windows;
using BnL.FGDBAddToMasterGUI.Services;
using BnL.FGDBAddToMasterGUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BnL.FGDBAddToMasterGUI;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFolderPicker, WpfFolderPicker>();
        services.AddSingleton<IGeodatabaseTransferService, GeodatabaseTransferService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}

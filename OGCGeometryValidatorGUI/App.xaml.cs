using OGCGeometryValidatorGUI.Models;
using System.Windows;

namespace OGCGeometryValidatorGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var model = new OGCGeometryValidatorModel();
            var presenter = new ShellViewModel(model);
            var shell = new Shell { DataContext = presenter };

            //LoggingUtils.Init();

            shell.Show();
        }
    }
}

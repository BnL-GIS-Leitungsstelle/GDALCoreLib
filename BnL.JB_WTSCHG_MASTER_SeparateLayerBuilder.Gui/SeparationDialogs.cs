using System.Windows;
using Microsoft.Win32;

namespace BnL.JB_WTSCHG_MASTER_SeparateLayerBuilder.Gui;

internal sealed class SeparationDialogs
{
    public string? SelectDatabase(string title)
    {
        var dialog = new OpenFolderDialog { Title = title };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? NewDatabase()
    {
        var dialog = new SaveFileDialog { Title = "Neue Ziel-FGDB benennen", Filter = "File-Geodatabase (*.gdb)|*.gdb", DefaultExt = ".gdb", FileName = "Separation.gdb" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool ConfirmReplacement(string path, string names) =>
        MessageBox.Show($"In {path} werden folgende Layer ersetzt:\n\n{names}\n\nFortfahren?", "Vorhandene Layer ersetzen",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
}

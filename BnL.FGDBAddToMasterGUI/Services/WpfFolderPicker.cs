namespace BnL.FGDBAddToMasterGUI.Services;

public sealed class WpfFolderPicker : IFolderPicker
{
    public string? PickFolder(string title)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = title,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}

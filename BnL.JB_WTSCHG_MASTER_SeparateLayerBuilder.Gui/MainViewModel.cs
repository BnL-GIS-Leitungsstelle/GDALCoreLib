using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdalToolsLib.Workflows;

namespace BnL.JB_WTSCHG_MASTER_SeparateLayerBuilder.Gui;

internal partial class MainViewModel : ObservableObject
{
    private readonly MasterLayerSeparator _separator;
    private readonly SeparationDialogs _dialogs;
    private readonly string _settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BnL.JB_WTSCHG_MASTER_SeparateLayerBuilder.Gui", "Namen.json");
    private sealed record Names(string Jbg, string Wildlife);

    [ObservableProperty] private string _source = "";
    [ObservableProperty] private string _target = "";
    [ObservableProperty] private string[] _layers = [];
    [ObservableProperty] private string? _selectedLayer;
    [ObservableProperty] private string _jbg = "N2023_Revision_jagdbanngebiete_230510_250501";
    [ObservableProperty] private string _wildlife = "N2023_Stand_wildtierschutzgebiete";
    [ObservableProperty] private string _log = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Available))] private bool _busy;
    public bool Available => !Busy;

    public MainViewModel(MasterLayerSeparator separator, SeparationDialogs dialogs)
    {
        _separator = separator;
        _dialogs = dialogs;
        try
        {
            if (File.Exists(_settings) && JsonSerializer.Deserialize<Names>(File.ReadAllText(_settings)) is { } names)
                (Jbg, Wildlife) = (names.Jbg, names.Wildlife);
        }
        catch (Exception ex) { Append($"Gespeicherte Namen konnten nicht geladen werden: {ex.Message}"); }
    }

    [RelayCommand]
    private void ChooseSource()
    {
        var path = _dialogs.SelectDatabase("MASTER-File-Geodatabase (.gdb) wählen");
        if (path == null) return;
        Source = path;
        Layers = [];
        SelectedLayer = null;
        try
        {
            if (!path.EndsWith(".gdb", StringComparison.OrdinalIgnoreCase)) throw new IOException("Bitte einen .gdb-Ordner wählen.");
            Layers = _separator.GetLayerNames(path);
            SelectedLayer = Layers.FirstOrDefault(n => n.Equals("jbg_wtschg_master", StringComparison.OrdinalIgnoreCase)) ?? Layers.FirstOrDefault();
        }
        catch (Exception ex) { Append($"MASTER konnte nicht geöffnet werden: {ex.Message}"); }
    }

    [RelayCommand]
    private void ChooseTarget() => Target = _dialogs.SelectDatabase("Bestehende Ziel-FGDB wählen") ?? Target;

    [RelayCommand]
    private void NewTarget() => Target = _dialogs.NewDatabase() ?? Target;

    [RelayCommand]
    private void SaveNames()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Jbg) || string.IsNullOrWhiteSpace(Wildlife)) throw new ArgumentException("Bitte beide Layernamen eingeben.");
            Directory.CreateDirectory(Path.GetDirectoryName(_settings)!);
            File.WriteAllText(_settings, JsonSerializer.Serialize(new Names(Jbg, Wildlife)));
            Append("Layernamen für künftige Sitzungen gespeichert.");
        }
        catch (Exception ex) { Append($"Namen konnten nicht gespeichert werden: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task SeparateAsync()
    {
        if (SelectedLayer is not { } layer || string.IsNullOrWhiteSpace(Target))
        {
            Append("Bitte MASTER-Featureclass und Ziel-FGDB wählen.");
            return;
        }
        Busy = true;
        try
        {
            var existing = Directory.Exists(Target) ? _separator.GetLayerNames(Target) : [];
            var collisions = new[] { Jbg, Wildlife }.Intersect(existing, StringComparer.OrdinalIgnoreCase).ToArray();
            if (collisions.Length > 0 && !_dialogs.ConfirmReplacement(Target, string.Join("\n", collisions)))
            {
                Append("Separation abgebrochen.");
                return;
            }
            var progress = new Progress<string>(Append);
            await Task.Run(() => _separator.Run(Source, layer, Target, Jbg, Wildlife, collisions, progress));
        }
        catch (Exception ex) { Append($"Fehler: {ex.Message}"); }
        finally { Busy = false; }
    }

    private void Append(string message) => Log += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
}

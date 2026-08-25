using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BnL.FGDBAddToMasterGUI.Models;
using BnL.FGDBAddToMasterGUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BnL.FGDBAddToMasterGUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFolderPicker _folderPicker;
    private readonly IGeodatabaseTransferService _transferService;

    public MainViewModel(IFolderPicker folderPicker, IGeodatabaseTransferService transferService)
    {
        _folderPicker = folderPicker;
        _transferService = transferService;
        AddFields.CollectionChanged += FieldsCollectionChanged;
        JoinFields.CollectionChanged += FieldsCollectionChanged;
    }

    public ObservableCollection<string> MasterLayerNames { get; } = [];
    public ObservableCollection<string> AddLayerNames { get; } = [];
    public ObservableCollection<SelectableFieldViewModel> AddFields { get; } = [];
    public ObservableCollection<JoinFieldViewModel> JoinFields { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMasterDatabase))]
    private string? _masterDatabasePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAddDatabase))]
    private string? _addDatabasePath;

    [ObservableProperty]
    private string? _selectedMasterLayer;

    [ObservableProperty]
    private string? _selectedAddLayer;

    [ObservableProperty]
    private string _joinCondition = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Bitte MASTER- und AddToMASTER-FGDB auswählen.";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isTransferCompleted;

    public bool HasMasterDatabase => !string.IsNullOrWhiteSpace(MasterDatabasePath);
    public bool HasAddDatabase => !string.IsNullOrWhiteSpace(AddDatabasePath);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    [RelayCommand]
    private async Task BrowseMasterDatabaseAsync()
    {
        var path = _folderPicker.PickFolder("MASTER-File-Geodatabase auswählen");
        if (path is null)
        {
            return;
        }

        MasterDatabasePath = path;
        SelectedMasterLayer = null;
        MasterLayerNames.Clear();
        await LoadLayerNamesAsync(path, MasterLayerNames, "MASTER");
    }

    [RelayCommand]
    private async Task BrowseAddDatabaseAsync()
    {
        var path = _folderPicker.PickFolder("AddToMASTER-File-Geodatabase auswählen");
        if (path is null)
        {
            return;
        }

        AddDatabasePath = path;
        SelectedAddLayer = null;
        AddLayerNames.Clear();
        await LoadLayerNamesAsync(path, AddLayerNames, "AddToMASTER");
    }

    [RelayCommand(CanExecute = nameof(CanTransfer))]
    private async Task TransferAsync()
    {
        ClearError();
        IsBusy = true;
        TransferCommand.NotifyCanExecuteChanged();

        try
        {
            var request = new TransferRequest(
                MasterDatabasePath!,
                SelectedMasterLayer!,
                AddDatabasePath!,
                SelectedAddLayer!,
                AddFields.Where(field => field.IsSelected).Select(field => field.Name).ToList(),
                JoinFields.Where(field => field.IsSelected).Select(field => field.Name).ToList());

            var result = await Task.Run(() => _transferService.Transfer(request));
            if (!result.IsSuccess)
            {
                ErrorText = result.ErrorMessage!;
                StatusText = "Die Übertragung wurde nicht ausgeführt.";
                return;
            }

            StatusText = $"Fertig: {result.CreatedFieldCount} Feld(er) angelegt, {result.UpdatedMasterFeatureCount} MASTER-Objekt(e) aktualisiert, {result.UnmatchedMasterFeatureCount} ohne Treffer, {result.UnmatchedAddFeatureCount} AddToMASTER-Objekt(e) ohne Treffer, {result.NullJoinKeyCount} Objekt(e) mit Null-Schlüssel übersprungen.";
            IsTransferCompleted = true;
        }
        catch (Exception exception)
        {
            ErrorText = exception.Message;
            StatusText = "Die Übertragung wurde nicht abgeschlossen.";
        }
        finally
        {
            IsBusy = false;
            TransferCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnSelectedMasterLayerChanged(string? value)
    {
        IsTransferCompleted = false;
        _ = RefreshFieldListsAsync();
    }

    partial void OnSelectedAddLayerChanged(string? value)
    {
        _ = RefreshFieldListsAsync();
    }

    partial void OnIsBusyChanged(bool value)
    {
        BrowseMasterDatabaseCommand.NotifyCanExecuteChanged();
        BrowseAddDatabaseCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsTransferCompletedChanged(bool value)
    {
        TransferCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private static void Close()
    {
        Application.Current.Shutdown();
    }

    private async Task LoadLayerNamesAsync(string databasePath, ObservableCollection<string> target, string role)
    {
        ClearError();
        IsBusy = true;
        try
        {
            var layerNames = await Task.Run(() => _transferService.GetLayerNames(databasePath));
            foreach (var layerName in layerNames)
            {
                target.Add(layerName);
            }

            StatusText = layerNames.Count == 0
                ? $"In der {role}-FGDB wurden keine Layer gefunden."
                : $"{layerNames.Count} Layer in der {role}-FGDB gefunden.";
        }
        catch (Exception exception)
        {
            ErrorText = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshFieldListsAsync()
    {
        AddFields.Clear();
        JoinFields.Clear();
        JoinCondition = string.Empty;
        TransferCommand.NotifyCanExecuteChanged();

        if (string.IsNullOrWhiteSpace(MasterDatabasePath) || string.IsNullOrWhiteSpace(SelectedMasterLayer) ||
            string.IsNullOrWhiteSpace(AddDatabasePath) || string.IsNullOrWhiteSpace(SelectedAddLayer))
        {
            return;
        }

        ClearError();
        IsBusy = true;
        try
        {
            var masterFieldsTask = Task.Run(() => _transferService.GetFields(MasterDatabasePath, SelectedMasterLayer));
            var addFieldsTask = Task.Run(() => _transferService.GetFields(AddDatabasePath, SelectedAddLayer));
            await Task.WhenAll(masterFieldsTask, addFieldsTask);

            var masterFields = await masterFieldsTask;
            var addFields = await addFieldsTask;
            var masterFieldsByName = masterFields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var addField in addFields)
            {
                AddFields.Add(new SelectableFieldViewModel(addField));
            }

            foreach (var addField in addFields.Where(field => masterFieldsByName.ContainsKey(field.Name)))
            {
                JoinFields.Add(new JoinFieldViewModel(masterFieldsByName[addField.Name], addField));
            }

            StatusText = $"{AddFields.Count} AddToMASTER-Felder und {JoinFields.Count} gemeinsame Join-Felder geladen.";
            var incompatibleJoinFields = JoinFields.Where(field => !field.IsCompatible).Select(field => field.Name).ToList();
            if (incompatibleJoinFields.Count > 0)
            {
                ErrorText = $"Diese gemeinsamen Felder haben unterschiedliche Datentypen und sind nicht als Join-Feld verfügbar: {string.Join(", ", incompatibleJoinFields)}.";
            }
        }
        catch (Exception exception)
        {
            ErrorText = exception.Message;
        }
        finally
        {
            IsBusy = false;
            TransferCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanTransfer()
    {
        return !IsBusy &&
               !IsTransferCompleted &&
               !string.IsNullOrWhiteSpace(MasterDatabasePath) &&
               !string.IsNullOrWhiteSpace(SelectedMasterLayer) &&
               !string.IsNullOrWhiteSpace(AddDatabasePath) &&
               !string.IsNullOrWhiteSpace(SelectedAddLayer) &&
               AddFields.Any(field => field.IsSelected) &&
               JoinFields.Any(field => field.IsSelected);
    }

    private void FieldsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var field in e.NewItems.OfType<INotifyPropertyChanged>())
            {
                field.PropertyChanged += FieldPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var field in e.OldItems.OfType<INotifyPropertyChanged>())
            {
                field.PropertyChanged -= FieldPropertyChanged;
            }
        }
    }

    private void FieldPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(JoinFieldViewModel.IsSelected))
        {
            return;
        }

        JoinCondition = _transferService.CreateJoinCondition(JoinFields.Where(field => field.IsSelected).Select(field => field.Name));
        TransferCommand.NotifyCanExecuteChanged();
    }

    private void ClearError()
    {
        ErrorText = string.Empty;
    }
}

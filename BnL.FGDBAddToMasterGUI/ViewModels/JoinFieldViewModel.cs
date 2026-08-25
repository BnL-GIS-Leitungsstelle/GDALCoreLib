using BnL.FGDBAddToMasterGUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BnL.FGDBAddToMasterGUI.ViewModels;

public sealed partial class JoinFieldViewModel : ObservableObject
{
    public JoinFieldViewModel(FieldDescriptor masterField, FieldDescriptor addField)
    {
        Name = masterField.Name;
        MasterTypeDescription = FormatType(masterField);
        AddTypeDescription = FormatType(addField);
        IsCompatible = masterField.Type == addField.Type;
        ValidationMessage = IsCompatible
            ? string.Empty
            : $"Nicht auswählbar: MASTER ist {MasterTypeDescription}, AddToMASTER ist {AddTypeDescription}.";
    }

    public string Name { get; }
    public string MasterTypeDescription { get; }
    public string AddTypeDescription { get; }
    public bool IsCompatible { get; }
    public bool HasValidationError => !IsCompatible;
    public string ValidationMessage { get; }

    [ObservableProperty]
    private bool _isSelected;

    private static string FormatType(FieldDescriptor field)
    {
        return field.Width is > 0 ? $"{field.TypeName} ({field.Width})" : field.TypeName;
    }
}

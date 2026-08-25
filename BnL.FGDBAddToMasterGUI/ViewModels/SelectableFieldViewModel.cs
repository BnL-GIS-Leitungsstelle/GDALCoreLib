using CommunityToolkit.Mvvm.ComponentModel;
using BnL.FGDBAddToMasterGUI.Models;

namespace BnL.FGDBAddToMasterGUI.ViewModels;

public sealed partial class SelectableFieldViewModel : ObservableObject
{
    public SelectableFieldViewModel(FieldDescriptor field)
    {
        Field = field;
    }

    public FieldDescriptor Field { get; }
    public string Name => Field.Name;
    public string TypeDescription => Field.Width is > 0 ? $"{Field.TypeName} ({Field.Width})" : Field.TypeName;

    [ObservableProperty]
    private bool _isSelected;
}

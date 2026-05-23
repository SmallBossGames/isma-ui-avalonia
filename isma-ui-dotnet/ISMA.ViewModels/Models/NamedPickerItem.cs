using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.Models;

public partial class NamedPickerItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private bool _isSelected;
}

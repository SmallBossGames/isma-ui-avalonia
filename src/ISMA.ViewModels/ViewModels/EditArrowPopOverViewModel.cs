using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.ViewModels.ViewModels;

public partial class EditArrowPopOverViewModel : ObservableObject
{
    [ObservableProperty]
    private string _alias = "";

    [ObservableProperty]
    private string _predicate = "";
}

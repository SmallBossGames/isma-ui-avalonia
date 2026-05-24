using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public partial class ResultSavingViewModel : ObservableObject
{
    [ObservableProperty]
    private SaveTarget _savingTarget = SaveTarget.Memory;
}

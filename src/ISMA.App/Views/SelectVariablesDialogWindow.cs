using Avalonia.Controls;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class SelectVariablesDialogWindow : Window
{
    private readonly SelectVariablesDialogViewModel _vm;

    public SelectVariablesDialogWindow(SelectVariablesDialogViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Title = "Select Variables for Chart";
        Width = 400;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;
    }
}

using Avalonia.Controls;
using Avalonia.Threading;
using ISMA.App.ViewModels;

namespace ISMA.App.Views;

public partial class SelectVariablesDialogWindow : Window
{
    private readonly SelectVariablesDialogViewModel? _dialogVm;

    public SelectVariablesDialogWindow()
    {
        InitializeComponent();
    }

    public SelectVariablesDialogWindow(SelectVariablesDialogViewModel vm)
    {
        _dialogVm = vm;
        DataContext = vm;
        InitializeComponent();

        // Observe OkPressed/ClosePressed to close the dialog with result
        _dialogVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectVariablesDialogViewModel.OkPressed) && _dialogVm.OkPressed)
            {
                Dispatcher.UIThread.Post(() => Close(true));
            }
            else if (e.PropertyName == nameof(SelectVariablesDialogViewModel.ClosePressed) && _dialogVm.ClosePressed)
            {
                Dispatcher.UIThread.Post(() => Close(false));
            }
        };
    }
}

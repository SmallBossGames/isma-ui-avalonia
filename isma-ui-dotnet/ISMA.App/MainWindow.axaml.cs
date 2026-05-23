using Avalonia.Controls;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}

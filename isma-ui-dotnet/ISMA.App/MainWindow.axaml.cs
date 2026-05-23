using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

        // Key bindings are handled in the MenuBar and ToolBar views
        // via their own InputBindings
    }
}

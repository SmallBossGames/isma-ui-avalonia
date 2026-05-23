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

        InputBindings.Add(new KeyBinding { Command = viewModel.NewTextCommand, Gesture = "Ctrl+N" });
        InputBindings.Add(new KeyBinding { Command = viewModel.NewBlueprintCommand, Gesture = "Ctrl+B" });
        InputBindings.Add(new KeyBinding { Command = viewModel.OpenCommand, Gesture = "Ctrl+O" });
        InputBindings.Add(new KeyBinding { Command = viewModel.SaveCommand, Gesture = "Ctrl+S" });
        InputBindings.Add(new KeyBinding { Command = viewModel.CloseCommand, Gesture = "Ctrl+W" });
        InputBindings.Add(new KeyBinding { Command = viewModel.CutCommand, Gesture = "Ctrl+X" });
        InputBindings.Add(new KeyBinding { Command = viewModel.CopyCommand, Gesture = "Ctrl+C" });
        InputBindings.Add(new KeyBinding { Command = viewModel.PasteCommand, Gesture = "Ctrl+V" });
        InputBindings.Add(new KeyBinding { Command = viewModel.VerifyCommand, Gesture = "F4" });
        InputBindings.Add(new KeyBinding { Command = viewModel.RunCommand, Gesture = "F5" });
    }
}

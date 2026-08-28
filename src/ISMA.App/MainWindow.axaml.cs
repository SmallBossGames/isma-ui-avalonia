using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App;

public partial class MainWindow : Window
{
    private readonly IPreferencesProvider? _preferencesProvider;
    private readonly bool _shouldRestoreGeometry;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.ExitRequested += OnExitRequested;
    }

    public MainWindow(MainWindowViewModel viewModel, IPreferencesProvider preferencesProvider) : this()
    {
        DataContext = viewModel;
        _preferencesProvider = preferencesProvider;
        _shouldRestoreGeometry = true;
        viewModel.ExitRequested += OnExitRequested;
    }

    private void OnExitRequested()
    {
        Close();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_shouldRestoreGeometry && _preferencesProvider != null)
        {
            var preferences = _preferencesProvider.Load();
            var wp = preferences.WindowPreferences;

            if (wp.Width > 0 && wp.Height > 0)
            {
                Width = wp.Width;
                Height = wp.Height;
            }

            if (wp.X >= 0 && wp.Y >= 0)
            {
                Position = new PixelPoint((int)wp.X, (int)wp.Y);
            }

            if (wp.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_preferencesProvider != null)
        {
            var wp = new WindowPreferences
            {
                X = Position.X,
                Y = Position.Y,
                Width = Bounds.Width,
                Height = Bounds.Height,
                IsMaximized = WindowState == WindowState.Maximized
            };
            _preferencesProvider.CommitWindow(wp);
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ProjectService.CaptureOpenFiles();
        }

        base.OnClosing(e);
    }
}

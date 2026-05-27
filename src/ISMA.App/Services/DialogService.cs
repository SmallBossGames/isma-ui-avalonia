using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ISMA.App.Services;

/// <summary>
/// Service for showing error, success, and confirmation dialogs.
/// Registered in DI and injected into ViewModels that need user feedback.
/// </summary>
public interface IDialogService
{
    Task ShowErrorAsync(string message, string title = "Error");
    Task ShowSuccessAsync(string message, string title = "Success");
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");
}

/// <summary>
/// Simple dialog window for showing messages.
/// </summary>
public class SimpleDialogWindow : Window
{
    public bool DialogResultValue { get; private set; }

    public SimpleDialogWindow(string title, string message, bool showOkCancel = false)
    {
        Title = title;
        Width = 350;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        ShowInTaskbar = false;

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 12
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8
        };

        if (showOkCancel)
        {
            var okButton = new Button
            {
                Content = "OK",
                Width = 70,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            okButton.Click += (s, e) => { DialogResultValue = true; Close(); };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 70,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            cancelButton.Click += (s, e) => { DialogResultValue = false; Close(); };
            buttonPanel.Children.Add(cancelButton);
        }
        else
        {
            var okButton = new Button
            {
                Content = "OK",
                Width = 70,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };
            okButton.Click += (s, e) => { DialogResultValue = true; Close(); };
            buttonPanel.Children.Add(okButton);
        }

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(buttonPanel);
        Content = stackPanel;
    }
}

/// <summary>
/// Dialog service implementation using simple Window-based dialogs.
/// </summary>
public class DialogService : IDialogService
{
    private readonly Func<Window?> _getMainWindow;

    public DialogService(Func<Window?> getMainWindow)
    {
        _getMainWindow = getMainWindow;
    }

    public Task ShowErrorAsync(string message, string title = "Error")
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = _getMainWindow();
            var dialog = new SimpleDialogWindow(title, message);
            await dialog.ShowDialog(owner);
        });
    }

    public Task ShowSuccessAsync(string message, string title = "Success")
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = _getMainWindow();
            var dialog = new SimpleDialogWindow(title, message);
            await dialog.ShowDialog(owner);
        });
    }

    public Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = _getMainWindow();
            var dialog = new SimpleDialogWindow(title, message, showOkCancel: true);
            await dialog.ShowDialog(owner);
            return dialog.DialogResultValue;
        });
    }
}

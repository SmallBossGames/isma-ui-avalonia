using Avalonia.Controls;
using AvaloniaEdit;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace ISMA.App.Services;

public class EditorPlatformService
{
    private AvaloniaEdit.TextEditor? _focusedEditor;

    public Action? CutRequested { get; set; }
    public Action? CopyRequested { get; set; }
    public Action? PasteRequested { get; set; }

    public void SetFocusedEditor(AvaloniaEdit.TextEditor? editor)
    {
        _focusedEditor = editor;
    }

    public void HandleCut()
    {
        if (_focusedEditor is not null)
        {
            AvaloniaEdit.ApplicationCommands.Cut.Execute(null, _focusedEditor.TextArea);
            CutRequested?.Invoke();
        }
    }

    public void HandleCopy()
    {
        if (_focusedEditor is not null)
        {
            AvaloniaEdit.ApplicationCommands.Copy.Execute(null, _focusedEditor.TextArea);
            CopyRequested?.Invoke();
        }
    }

    public void HandlePaste()
    {
        if (_focusedEditor is not null)
        {
            AvaloniaEdit.ApplicationCommands.Paste.Execute(null, _focusedEditor.TextArea);
            PasteRequested?.Invoke();
        }
    }

    public void HandleSelectAll(AvaloniaEdit.TextEditor editor)
    {
        if (editor is not null)
        {
            editor.SelectAll();
        }
    }

    public void AttachToWindow(Window window)
    {
        window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) == 0) return;

        var focused = e.Source as AvaloniaEdit.TextEditor;
        if (focused is null) return;

        _focusedEditor = focused;

        if (e.Key == Key.X)
        {
            HandleCut();
            e.Handled = true;
        }
        else if (e.Key == Key.C)
        {
            HandleCopy();
            e.Handled = true;
        }
        else if (e.Key == Key.V)
        {
            HandlePaste();
            e.Handled = true;
        }
    }
}

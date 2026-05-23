using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace ISMA.App.Services;

public class EditorPlatformService
{
    private TextBox? _focusedTextBox;

    public Action<string>? CutRequested { get; set; }
    public Action<string>? CopyRequested { get; set; }
    public Action<string>? PasteRequested { get; set; }

    public void SetFocusedEditor(TextBox? textBox)
    {
        _focusedTextBox = textBox;
    }

    public void HandleCut()
    {
        if (_focusedTextBox is not null)
        {
            var start = _focusedTextBox.CaretIndex;
            var end = _focusedTextBox.CaretIndex;
            if (_focusedTextBox.SelectionStart != _focusedTextBox.SelectionEnd)
            {
                start = Math.Min(_focusedTextBox.SelectionStart, _focusedTextBox.SelectionEnd);
                end = Math.Max(_focusedTextBox.SelectionStart, _focusedTextBox.SelectionEnd);
                var selected = _focusedTextBox.Text.Substring(start, end - start);
                _focusedTextBox.Text = _focusedTextBox.Text.Remove(start, end - start);
                _focusedTextBox.CaretIndex = start;
                CutRequested?.Invoke(_focusedTextBox.Text);
            }
            else
            {
                CutRequested?.Invoke(_focusedTextBox.Text);
            }
        }
    }

    public void HandleCopy()
    {
        if (_focusedTextBox is not null)
        {
            if (_focusedTextBox.SelectionStart != _focusedTextBox.SelectionEnd)
            {
                var start = Math.Min(_focusedTextBox.SelectionStart, _focusedTextBox.SelectionEnd);
                var end = Math.Max(_focusedTextBox.SelectionStart, _focusedTextBox.SelectionEnd);
                var selected = _focusedTextBox.Text.Substring(start, end - start);
                CopyRequested?.Invoke(selected);
            }
            else
            {
                CopyRequested?.Invoke(_focusedTextBox.Text);
            }
        }
    }

    public void HandlePaste()
    {
        if (_focusedTextBox is not null)
        {
            var start = _focusedTextBox.CaretIndex;
            var selectedLen = Math.Abs(_focusedTextBox.SelectionEnd - _focusedTextBox.SelectionStart);
            var pasted = ClipboardContentProvider.GetText(_focusedTextBox);
            if (pasted is not null)
            {
                _focusedTextBox.Text = _focusedTextBox.Text.Remove(start, selectedLen).Insert(start, pasted);
                _focusedTextBox.CaretIndex = start + pasted.Length;
                PasteRequested?.Invoke(_focusedTextBox.Text);
            }
        }
    }

    public void AttachToWindow(Window window)
    {
        window.AttachedToVisualTree += (s, e) =>
        {
            window.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.X && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCut();
            e.Handled = true;
        }
        else if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandleCopy();
            e.Handled = true;
        }
        else if (e.Key == Key.V && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            HandlePaste();
            e.Handled = true;
        }
    }
}

internal sealed class RelayCommand(Action execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged;
}

internal static class ClipboardContentProvider
{
    public static string? GetText(Control control)
    {
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel?.Clipboard is null) return null;
        return ClipboardExtensions.TryGetTextAsync(topLevel.Clipboard!).Result;
    }
}

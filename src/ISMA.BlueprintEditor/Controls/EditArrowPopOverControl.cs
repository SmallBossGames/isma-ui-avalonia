using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// A popover control for editing transition alias and predicate.
/// Used by the blueprint editor to edit arrow properties.
/// </summary>
public class EditArrowPopOverControl : UserControl
{
    private EditArrowPopOverViewModel? _viewModel;

    public static readonly StyledProperty<EditArrowPopOverViewModel?> PopOverViewModelProperty =
        AvaloniaProperty.Register<EditArrowPopOverControl, EditArrowPopOverViewModel?>(nameof(PopOverViewModel));

    public EditArrowPopOverViewModel? PopOverViewModel
    {
        get => GetValue(PopOverViewModelProperty);
        set
        {
            SetValue(PopOverViewModelProperty, value);
            _viewModel = value;
            UpdateBindings();
        }
    }

    /// <summary>
    /// Raised when the popover should be dismissed.
    /// </summary>
    public event Action? DismissRequested;

    /// <summary>
    /// Raised when the alias changes.
    /// </summary>
    public event Action? AliasChanged;

    /// <summary>
    /// Raised when the predicate changes.
    /// </summary>
    public event Action? PredicateChanged;

    private TextBox? _aliasTextBox;
    private TextBox? _predicateTextBox;
    private StackPanel? _contentPanel;

    public EditArrowPopOverControl()
    {
        MinWidth = 300;
        CornerRadius = new CornerRadius(5);
        Padding = new Thickness(10);
        Background = Brushes.White;
        BorderBrush = new SolidColorBrush(Color.FromRgb(211, 211, 211));
        BorderThickness = new Thickness(1);

        Content = CreateContent();
    }

    private Panel CreateContent()
    {
        var stackPanel = new StackPanel { Spacing = 8 };
        _contentPanel = stackPanel;

        // Title
        var title = new TextBlock
        {
            Text = "Edit Transition",
            FontWeight = FontWeight.Bold,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 4)
        };
        stackPanel.Children.Add(title);

        // Alias
        var aliasStack = new StackPanel { Spacing = 4 };
        var aliasLabel = new TextBlock
        {
            Text = "Alias (optional):",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
        };
        _aliasTextBox = new TextBox
        {
            FontSize = 11,
            MinHeight = 28,
            PlaceholderText = "Enter alias..."
        };
        _aliasTextBox.TextChanged += OnAliasTextChanged;
        aliasStack.Children.Add(aliasLabel);
        aliasStack.Children.Add(_aliasTextBox);
        stackPanel.Children.Add(aliasStack);

        // Predicate
        var predicateStack = new StackPanel { Spacing = 4 };
        var predicateLabel = new TextBlock
        {
            Text = "Predicate:",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51))
        };
        _predicateTextBox = new TextBox
        {
            FontSize = 11,
            MinHeight = 60,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            PlaceholderText = "Enter predicate condition..."
        };
        _predicateTextBox.TextChanged += OnPredicateTextChanged;
        predicateStack.Children.Add(predicateLabel);
        predicateStack.Children.Add(_predicateTextBox);
        stackPanel.Children.Add(predicateStack);

        // Buttons
        var buttonStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var okButton = new Button
        {
            Content = "OK",
            Padding = new Thickness(16, 4)
        };
        okButton.Click += OnOkClick;

        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 4)
        };
        cancelButton.Click += OnCancelClick;

        buttonStack.Children.Add(okButton);
        buttonStack.Children.Add(cancelButton);
        stackPanel.Children.Add(buttonStack);

        return stackPanel;
    }

    private void UpdateBindings()
    {
        if (_viewModel == null) return;

        if (_aliasTextBox != null)
        {
            _aliasTextBox.Text = _viewModel.Alias;
        }

        if (_predicateTextBox != null)
        {
            _predicateTextBox.Text = _viewModel.Predicate;
        }
    }

    private void OnAliasTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Alias = _aliasTextBox?.Text ?? "";
        }
        AliasChanged?.Invoke();
    }

    private void OnPredicateTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Predicate = _predicateTextBox?.Text ?? "";
        }
        PredicateChanged?.Invoke();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Alias = _aliasTextBox?.Text ?? "";
            _viewModel.Predicate = _predicateTextBox?.Text ?? "";
        }
        DismissRequested?.Invoke();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        DismissRequested?.Invoke();
    }
}

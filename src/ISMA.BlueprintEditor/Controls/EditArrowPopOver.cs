using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ISMA.BlueprintEditor.Constants;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

public class EditArrowPopOver : Panel
{
    private TextBox? _aliasTextBox;
    private TextBox? _predicateTextBox;
    private readonly TransactionViewModel _viewModel;
    private readonly Control _parent;

    public event EventHandler? Closed;

    public EditArrowPopOver(TransactionViewModel viewModel, Control parent, Point position)
    {
        _viewModel = viewModel;
        _parent = parent;

        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        MinWidth = BlueprintEditorConstants.PopoverMinWidth;

        Canvas.SetLeft(this, position.X - MinWidth / 2);
        Canvas.SetTop(this, position.Y - 50);

        var border = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(BlueprintEditorConstants.PopoverCornerRadius),
            Padding = new Thickness(BlueprintEditorConstants.PopoverPadding),
            Child = CreateContent()
        };

        Children.Add(border);

        AddHandler(PointerPressedEvent, PopOverPointerPressedHandler, handledEventsToo: true);
    }

    private Control CreateContent()
    {
        var stackPanel = new StackPanel
        {
            Spacing = 8
        };

        var aliasLabel = new TextBlock
        {
            Text = "Alias (optional)",
            FontSize = 12,
            FontWeight = FontWeight.Bold
        };

        _aliasTextBox = new TextBox
        {
            PlaceholderText = "Alias"
        };

        var predicateLabel = new TextBlock
        {
            Text = "Predicate",
            FontSize = 12,
            FontWeight = FontWeight.Bold
        };

        _predicateTextBox = new TextBox
        {
            PlaceholderText = "e.g., x > 0"
        };

        stackPanel.Children.Add(aliasLabel);
        stackPanel.Children.Add(_aliasTextBox);
        stackPanel.Children.Add(predicateLabel);
        stackPanel.Children.Add(_predicateTextBox);

        _aliasTextBox.Text = _viewModel.Alias;
        _predicateTextBox.Text = _viewModel.Predicate;

        _aliasTextBox!.TextChanged += (_, _) => _viewModel.Alias = _aliasTextBox.Text ?? "";
        _predicateTextBox!.TextChanged += (_, _) => _viewModel.Predicate = _predicateTextBox.Text ?? "";

        _aliasTextBox.LostFocus += (_, _) => Close();
        _predicateTextBox.LostFocus += (_, _) => Close();

        return stackPanel;
    }

    private void PopOverPointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    public void Close()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        RemoveHandler(PointerPressedEvent, PopOverPointerPressedHandler);
    }
}

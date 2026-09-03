using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using ISMA.BlueprintEditor.Controls;
using ISMA.BlueprintEditor.Models;
using ISMA.BlueprintEditor.Services;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Views;

/// <summary>
/// The blueprint editor: a tabbed control with a diagram tab (canvas + toolbar)
/// and dynamically opened state/loop body editor tabs. Composite control —
/// this code-behind is intentional control logic (tab management, event
/// routing, popover placement), mirroring the original Kotlin
/// <c>IsmaBlueprintEditor</c> + <c>CanvasView</c> wiring.
/// </summary>
public partial class IsmaBlueprintEditor : UserControl
{
    private readonly Dictionary<StateViewModel, OpenEditorTab> _openStateTabs = new();
    private readonly Dictionary<LoopTransactionViewModel, OpenEditorTab> _openLoopTabs = new();
    private EditArrowPopOver? _popover;

    /// <summary>An open state/loop body editor tab.</summary>
    /// <param name="Tab">The tab item.</param>
    /// <param name="Editor">The text editor instance.</param>
    /// <param name="TitleBlock">The tab title element (updated on rename).</param>
    /// <param name="Close">Unbinds and disposes the editor.</param>
    private sealed record OpenEditorTab(TabItem Tab, ITextEditor Editor, TextBlock TitleBlock, Action Close);

    /// <summary>Creates the editor.</summary>
    /// <param name="editorFactory">Factory for the state/loop body text editors.</param>
    public IsmaBlueprintEditor(ITextEditorFactory editorFactory)
    {
        InitializeComponent();

        EditorFactory = editorFactory;
        ViewModel = new IsmaBlueprintViewModel();
        DataContext = ViewModel;

        DiagramCanvas.AddHandler(StateBox.SingleClickEvent, OnStateSingleClick);
        DiagramCanvas.AddHandler(StateBox.DoubleClickEvent, OnStateDoubleClick);
        DiagramCanvas.AddHandler(StateBox.NameCommittedEvent, OnStateNameCommitted);
        DiagramCanvas.AddHandler(TransactionArrow.ArrowHeadClickedEvent, OnArrowHeadClicked);
        DiagramCanvas.AddHandler(TransactionArrow.BodyClickedEvent, OnArrowBodyClicked);
        DiagramCanvas.AddHandler(LoopTransactionArrow.BodyClickedEvent, OnLoopBodyClicked);
        DiagramCanvas.AddHandler(LoopTransactionArrow.ArrowHeadDoubleClickedEvent, OnLoopArrowHeadDoubleClicked);

        Tabs.SelectionChanged += (_, _) =>
        {
            Toolbar.IsVisible = Tabs.SelectedItem == DiagramTab;
        };

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>The editor view model.</summary>
    public IsmaBlueprintViewModel ViewModel { get; }

    /// <summary>The factory of the state/loop body text editors.</summary>
    public ITextEditorFactory EditorFactory { get; }

    /// <summary>Gets the current diagram as a blueprint model.</summary>
    public BlueprintModel GetBlueprintModel() => ViewModel.ToBlueprintModel();

    /// <summary>Replaces the diagram with the given blueprint model.</summary>
    /// <param name="model">The model to load.</param>
    public void SetBlueprintModel(BlueprintModel model) => ViewModel.FromBlueprintModel(model);

    /// <summary>Closes all open state/loop editor tabs and disposes their editors.</summary>
    public void DisposeEditor()
    {
        ClosePopover();

        foreach (var entry in _openStateTabs.Values.ToList())
        {
            RemoveTab(entry);
        }

        foreach (var entry in _openLoopTabs.Values.ToList())
        {
            RemoveTab(entry);
        }

        _openStateTabs.Clear();
        _openLoopTabs.Clear();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IsmaBlueprintViewModel.Evt))
        {
            return;
        }

        switch (ViewModel.Evt)
        {
            case BlueprintEvent.OpenStateEditor openState:
                OpenStateEditorTab(openState.State);
                break;
            case BlueprintEvent.OpenLoopEditor openLoop:
                OpenLoopEditorTab(openLoop.Loop, openLoop.State);
                break;
        }
    }

    private void OnStateSingleClick(object? sender, StateBoxRoutedEventArgs e)
    {
        ViewModel.HandleStateClick(e.State);
    }

    private void OnStateDoubleClick(object? sender, StateBoxRoutedEventArgs e)
    {
        ViewModel.HandleStateDoubleClick(e.State);
    }

    private void OnStateNameCommitted(object? sender, NameCommittedRoutedEventArgs e)
    {
        ViewModel.CommitNameEdit(e.State, e.NewName);
    }

    private void OnArrowHeadClicked(object? sender, TransactionArrowRoutedEventArgs e)
    {
        if (ViewModel.HandleArrowheadClick(e.Transaction))
        {
            ShowPopover(e.Transaction, e.Point);
        }
    }

    private void OnArrowBodyClicked(object? sender, TransactionArrowRoutedEventArgs e)
    {
        ViewModel.HandleArrowBodyClick(e.Transaction);
    }

    private void OnLoopBodyClicked(object? sender, LoopArrowRoutedEventArgs e)
    {
        ViewModel.HandleLoopArrowBodyClick(e.Loop);
    }

    private void OnLoopArrowHeadDoubleClicked(object? sender, LoopArrowRoutedEventArgs e)
    {
        if (ViewModel.CanvasViewModel.StateByName(e.Loop.StateName) is { } state)
        {
            ViewModel.HandleLoopArrowheadDoubleClick(e.Loop, state);
        }
    }

    private void OpenStateEditorTab(StateViewModel state)
    {
        if (_openStateTabs.TryGetValue(state, out var existing))
        {
            Tabs.SelectedItem = existing.Tab;
            return;
        }

        var editor = EditorFactory.CreateEditor();
        bool updating = false;

        TextBlock? titleRef = null;

        EventHandler editorTextChanged = (_, _) =>
        {
            if (updating)
            {
                return;
            }

            updating = true;
            state.Text = editor.Text;
            updating = false;
        };

        void statePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(StateViewModel.Text) when !updating:
                    updating = true;
                    editor.Text = state.Text;
                    updating = false;
                    break;
                case nameof(StateViewModel.Name):
                    if (titleRef is { } title)
                    {
                        title.Text = state.Name;
                    }

                    break;
            }
        }

        var entry = new OpenEditorTab(
            CreateClosableTab(state.Name, editor.Node, out var titleBlock, out var closeButton),
            editor,
            titleBlock,
            () =>
            {
                editor.TextChanged -= editorTextChanged;
                state.PropertyChanged -= statePropertyChanged;
                editor.Dispose();
            });

        titleRef = titleBlock;

        editor.TextChanged += editorTextChanged;
        state.PropertyChanged += statePropertyChanged;
        editor.Text = state.Text;

        closeButton.Click += (_, _) =>
        {
            _openStateTabs.Remove(state);
            RemoveTab(entry);
        };

        _openStateTabs[state] = entry;
        Tabs.Items.Add(entry.Tab);
        Tabs.SelectedItem = entry.Tab;
    }

    private void OpenLoopEditorTab(LoopTransactionViewModel loop, StateViewModel state)
    {
        if (_openLoopTabs.TryGetValue(loop, out var existing))
        {
            Tabs.SelectedItem = existing.Tab;
            return;
        }

        var editor = EditorFactory.CreateEditor();
        bool updating = false;

        EventHandler editorTextChanged = (_, _) =>
        {
            if (updating)
            {
                return;
            }

            updating = true;
            loop.Text = editor.Text;
            updating = false;
        };

        TextBlock? titleRef = null;

        void loopPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoopTransactionViewModel.Text) && !updating)
            {
                updating = true;
                editor.Text = loop.Text;
                updating = false;
            }
        }

        void statePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StateViewModel.Name) && titleRef is { } title)
            {
                title.Text = state.Name + " (loop)";
            }
        }

        var entry = new OpenEditorTab(
            CreateClosableTab(state.Name + " (loop)", editor.Node, out var titleBlock, out var closeButton),
            editor,
            titleBlock,
            () =>
            {
                editor.TextChanged -= editorTextChanged;
                loop.PropertyChanged -= loopPropertyChanged;
                state.PropertyChanged -= statePropertyChanged;
                editor.Dispose();
            });

        titleRef = titleBlock;

        editor.TextChanged += editorTextChanged;
        loop.PropertyChanged += loopPropertyChanged;
        state.PropertyChanged += statePropertyChanged;
        editor.Text = loop.Text;

        closeButton.Click += (_, _) =>
        {
            _openLoopTabs.Remove(loop);
            RemoveTab(entry);
        };

        _openLoopTabs[loop] = entry;
        Tabs.Items.Add(entry.Tab);
        Tabs.SelectedItem = entry.Tab;
    }

    private TabItem CreateClosableTab(string title, Control content, out TextBlock titleBlock, out Button closeButton)
    {
        titleBlock = new TextBlock
        {
            Text = title,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        closeButton = new Button
        {
            Content = "×",
            Width = 18,
            Height = 18,
            Padding = new Thickness(0),
            Margin = new Thickness(6, 0, 0, 0),
        };

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Children = { titleBlock, closeButton },
        };

        return new TabItem
        {
            Header = header,
            Content = content,
        };
    }

    private void RemoveTab(OpenEditorTab entry)
    {
        entry.Close();
        Tabs.Items.Remove(entry.Tab);
    }

    private void ShowPopover(TransactionViewModel transaction, Point point)
    {
        ClosePopover();

        var popover = new EditArrowPopOver(transaction);
        _popover = popover;

        popover.SizeChanged += (_, _) =>
        {
            Canvas.SetLeft(popover, point.X - popover.DesiredSize.Width / 2);
            Canvas.SetTop(popover, point.Y - 2);
        };

        popover.PointerExited += (_, _) => ClosePopover();
        popover.ZIndex = 3;
        DiagramCanvas.DiagramCanvas.Children.Add(popover);
    }

    private void ClosePopover()
    {
        if (_popover is { } popover)
        {
            DiagramCanvas.DiagramCanvas.Children.Remove(popover);
            _popover = null;
        }
    }
}

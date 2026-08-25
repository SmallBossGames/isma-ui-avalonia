using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.ViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.App.Views;

public partial class BlueprintEditorView : UserControl
{
    private EditArrowPopOverView? _popOverView;
    private BlueprintEditorViewModel? _vm;
    private ITextEditorFactory? _textEditorFactory;
    private readonly Dictionary<BlueprintStateViewModel, EditorTab> _stateEditorTabs = new();
    private readonly Dictionary<BlueprintLoopTransactionViewModel, EditorTab> _loopEditorTabs = new();

    private sealed class EditorTab
    {
        public required TabItem Tab { get; init; }
        public required object Editor { get; init; }
        public required TextBlock Title { get; init; }
    }

    public BlueprintEditorView()
    {
        InitializeComponent();
        _textEditorFactory = AppServiceLocator.Services?.GetService<ITextEditorFactory>();
        _popOverView = new EditArrowPopOverView();
        _popOverView.DismissRequested += OnPopOverDismissRequested;
        _popOverView.AliasChanged += OnPopOverAliasChanged;
        _popOverView.PredicateChanged += OnPopOverPredicateChanged;
        _popOverView.PointerExited += OnPopOverPointerLeave;
        EditArrowPopup.Child = _popOverView;
        UpdateToolbarVisibility();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is BlueprintEditorViewModel oldVm)
        {
            Unsubscribe(oldVm);
        }

        if (DataContext is BlueprintEditorViewModel newVm)
        {
            Subscribe(newVm);
        }
    }

    private void Subscribe(BlueprintEditorViewModel vm)
    {
        _vm = vm;
        vm.EditArrowRequested += OnEditArrowRequested;
        vm.StateTextEditorRequested += OnStateTextEditorRequested;
        vm.LoopTextEditorRequested += OnLoopTextEditorRequested;
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void Unsubscribe(BlueprintEditorViewModel vm)
    {
        vm.EditArrowRequested -= OnEditArrowRequested;
        vm.StateTextEditorRequested -= OnStateTextEditorRequested;
        vm.LoopTextEditorRequested -= OnLoopTextEditorRequested;
        vm.PropertyChanged -= OnVmPropertyChanged;
        CloseAllEditorTabs();
        _vm = null;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlueprintEditorViewModel.PopOverViewModel) && _popOverView != null)
        {
            _popOverView.DataContext = _vm?.PopOverViewModel;
        }
    }

    private void OnEditorTabsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateToolbarVisibility();
    }

    private void UpdateToolbarVisibility()
    {
        if (ToolbarBorder != null)
        {
            ToolbarBorder.IsVisible = EditorTabs.SelectedItem == DiagramTab;
        }
    }

    private void OnEditArrowRequested(BlueprintTransitionViewModel tx, double x, double y)
    {
        _vm?.OpenPopOver(tx, x, y);
        Dispatcher.UIThread.InvokeAsync(() => PositionPopOver(x, y), DispatcherPriority.Normal);
    }

    private void PositionPopOver(double x, double y)
    {
        if (_popOverView == null) return;

        var savedDataContext = _popOverView.DataContext;

        var scrollViewer = this.FindControl<ScrollViewer>("CanvasScrollViewer");
        var offset = scrollViewer?.Offset ?? default;

        var popOverWidth = _popOverView.DesiredSize.Width;
        double left = x - popOverWidth / 2 - offset.X;
        Canvas.SetLeft(_popOverView, left);
        Canvas.SetTop(_popOverView, y - offset.Y);

        if (popOverWidth == 0)
        {
            EventHandler<SizeChangedEventArgs>? handler = null;
            handler = (s, ev) =>
            {
                if (_popOverView != null)
                {
                    _popOverView.SizeChanged -= handler;
                    var newWidth = _popOverView.DesiredSize.Width;
                    if (newWidth > 0)
                    {
                        Canvas.SetLeft(_popOverView, x - newWidth / 2 - offset.X);
                    }
                }
            };
            _popOverView.SizeChanged += handler;
        }

        if (EditArrowPopup.Child is not Avalonia.Controls.Canvas)
        {
            var canvas = new Canvas();
            canvas.Children.Add(_popOverView);
            EditArrowPopup.Child = canvas;
        }

        _popOverView.DataContext = savedDataContext;
    }

    private void OnPopOverDismissRequested() => _vm?.ClosePopOver();
    private void OnPopOverAliasChanged() => _vm?.OnPopOverAliasChanged();
    private void OnPopOverPredicateChanged() => _vm?.OnPopOverPredicateChanged();
    private void OnPopOverPointerLeave(object? sender, PointerEventArgs e) => _vm?.ClosePopOver();

    private void OnStateBoxStateClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStatePressed(stateVm);
    }

    private void OnStateBoxStateDoubleClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        _vm?.OnStateDoubleClicked(stateVm);
    }

    private void OnStateBoxNameCommitted(object? sender, Controls.StateNameCommittedEventArgs e)
    {
        if (sender is not Controls.StateBox stateBox || stateBox.DataContext is not BlueprintStateViewModel stateVm) return;
        var result = _vm?.OnStateNameCommitted(stateVm, e.NewName) ?? false;
        e.Handled = !result;
    }

    private void OnArrowHeadClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.ArrowLine arrow) return;
        var tx = GetTransaction(arrow);
        if (tx != null) _vm?.OnArrowHeadClicked(tx, e.ClickPosition.X, e.ClickPosition.Y);
    }

    private void OnArrowBodyClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.ArrowLine arrow) return;
        var tx = GetTransaction(arrow);
        if (tx != null) _vm?.OnArrowBodyClicked(tx);
    }

    private void OnLoopArrowDoubleClick(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null)
        {
            _vm?.OpenLoopTextEditor(loop);
        }
    }

    private void OnLoopBodyClicked(object? sender, Controls.ArrowHitTestEventArgs e)
    {
        if (sender is not Controls.LoopArrow arrow) return;
        var loop = GetLoop(arrow);
        if (loop != null) _vm?.OnLoopBodyClicked(loop);
    }

    private BlueprintTransitionViewModel? GetTransaction(Controls.ArrowLine arrow)
    {
        if (arrow.Id == null) return null;
        return _vm?.Transitions.FirstOrDefault(t => t.Id == arrow.Id);
    }

    private BlueprintLoopTransactionViewModel? GetLoop(Controls.LoopArrow arrow)
    {
        if (arrow.StateId == Guid.Empty) return null;
        return _vm?.LoopTransactions.FirstOrDefault(l => l.StateId == arrow.StateId);
    }

    private void OnStateTextEditorRequested(BlueprintStateViewModel state)
    {
        if (_textEditorFactory == null) return;

        if (_stateEditorTabs.TryGetValue(state, out var existing))
        {
            EditorTabs.SelectedItem = existing.Tab;
            return;
        }

        var editor = _textEditorFactory.CreateTextEditor(state.Text, newText => state.Text = newText);
        var title = new TextBlock
        {
            Text = state.Name,
            VerticalAlignment = VerticalAlignment.Center
        };
        var closeButton = new Button
        {
            Content = "×",
            Width = 16,
            Height = 16,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        header.Children.Add(title);
        header.Children.Add(closeButton);

        var tab = new TabItem { Header = header, Tag = state, Content = editor };
        void CloseTab(object? s, RoutedEventArgs e) => CloseStateEditorTab(state);
        closeButton.Click += CloseTab;

        void OnStateChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BlueprintStateViewModel.Name))
            {
                title.Text = state.Name;
            }
        }
        state.PropertyChanged += OnStateChanged;

        _stateEditorTabs[state] = new EditorTab { Tab = tab, Editor = editor, Title = title };
        EditorTabs.Items.Add(tab);
        EditorTabs.SelectedItem = tab;
    }

    private void CloseStateEditorTab(BlueprintStateViewModel state)
    {
        if (!_stateEditorTabs.Remove(state, out var entry)) return;

        EditorTabs.Items.Remove(entry.Tab);
        _textEditorFactory?.DisposeInstance(entry.Editor);
        if (EditorTabs.SelectedItem is TabItem selected)
        {
            EditorTabs.SelectedItem = selected;
        }
    }

    private void OnLoopTextEditorRequested(BlueprintLoopTransactionViewModel loop)
    {
        if (_textEditorFactory == null) return;

        if (_loopEditorTabs.TryGetValue(loop, out var existing))
        {
            EditorTabs.SelectedItem = existing.Tab;
            return;
        }

        var state = loop.GetState(_vm?.States ?? []) ?? _vm?.MainState ?? _vm?.InitState;
        var editor = _textEditorFactory.CreateTextEditor(loop.Text, newText => loop.Text = newText);
        var title = new TextBlock
        {
            Text = $"{state?.Name} (loop)",
            VerticalAlignment = VerticalAlignment.Center
        };
        var closeButton = new Button
        {
            Content = "×",
            Width = 16,
            Height = 16,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        header.Children.Add(title);
        header.Children.Add(closeButton);

        var tab = new TabItem { Header = header, Tag = loop, Content = editor };
        void CloseTab(object? s, RoutedEventArgs e) => CloseLoopEditorTab(loop);
        closeButton.Click += CloseTab;

        if (state != null)
        {
            void OnStateChanged(object? s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(BlueprintStateViewModel.Name))
                {
                    title.Text = $"{state.Name} (loop)";
                }
            }
            state.PropertyChanged += OnStateChanged;
        }

        _loopEditorTabs[loop] = new EditorTab { Tab = tab, Editor = editor, Title = title };
        EditorTabs.Items.Add(tab);
        EditorTabs.SelectedItem = tab;
    }

    private void CloseLoopEditorTab(BlueprintLoopTransactionViewModel loop)
    {
        if (!_loopEditorTabs.Remove(loop, out var entry)) return;

        EditorTabs.Items.Remove(entry.Tab);
        _textEditorFactory?.DisposeInstance(entry.Editor);
        if (EditorTabs.SelectedItem is TabItem selected)
        {
            EditorTabs.SelectedItem = selected;
        }
    }

    private void CloseAllEditorTabs()
    {
        foreach (var state in _stateEditorTabs.Keys.ToList())
        {
            CloseStateEditorTab(state);
        }
        foreach (var loop in _loopEditorTabs.Keys.ToList())
        {
            CloseLoopEditorTab(loop);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ISMA.App.Controls;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class BlueprintEditorView : UserControl
{
    private BlueprintCanvasPanel? _canvas;
    private Avalonia.Controls.Primitives.Popup? _editArrowPopup;

    public BlueprintEditorView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _canvas = this.FindControl<BlueprintCanvasPanel>("Canvas");
        _editArrowPopup = this.FindControl<Avalonia.Controls.Primitives.Popup>("EditArrowPopup");

        if (_canvas != null)
        {
            _canvas.StateSelected += OnStateSelected;
            _canvas.StateDoubleClicked += OnStateDoubleClicked;
            _canvas.ArrowClicked += OnArrowClicked;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_canvas != null)
        {
            _canvas.StateSelected -= OnStateSelected;
            _canvas.StateDoubleClicked -= OnStateDoubleClicked;
            _canvas.ArrowClicked -= OnArrowClicked;
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is BlueprintEditorViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlueprintEditorViewModel.CurrentMode))
        {
            UpdateDragPreviewVisibility();
        }
    }

    private void OnStateSelected(BlueprintStateViewModel? state)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm == null)
            return;

        if (vm.CurrentMode == BlueprintEditorMode.AddTransition)
        {
            if (state != null && vm.SelectedState != null && state != vm.SelectedState)
            {
                vm.SelectedTransaction = new BlueprintTransactionViewModel
                {
                    StartState = vm.SelectedState,
                    EndState = state
                };
                vm.AddTransitionCommand.Execute(null);
            }
        }
    }

    private void OnStateDoubleClicked(BlueprintStateViewModel? state)
    {
        if (state == null)
            return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm == null)
            return;

        state.IsEditable = true;
        vm.SetBlueprintModel(vm.GetBlueprintModel());
    }

    private void OnArrowClicked(BlueprintTransactionViewModel? arrow)
    {
        if (arrow == null || _editArrowPopup == null)
            return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm == null)
            return;

        vm.SelectedTransaction = arrow;

        var popOverVm = new EditArrowPopOverViewModel
        {
            Alias = arrow.Alias,
            Predicate = arrow.Predicate
        };

        var popupView = new EditArrowPopOverView { DataContext = popOverVm };

        _editArrowPopup.Child = popupView;
        _editArrowPopup.IsOpen = true;
    }

    private void UpdateDragPreviewVisibility()
    {
        if (_canvas == null)
            return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm == null)
            return;

        if (vm.CurrentMode == BlueprintEditorMode.AddTransition)
        {
            _canvas.InvalidateVisual();
        }
    }
}

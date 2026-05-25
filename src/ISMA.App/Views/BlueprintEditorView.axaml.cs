using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ISMA.App.Controls;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class BlueprintEditorView : UserControl
{
    private EditArrowPopOverView? _popOverView;
    private EditArrowPopOverViewModel? _popOverViewModel;
    private BlueprintTransactionViewModel? _editingTransaction;
    private const double ArrowOffset = 10.0;
    private const double ArrowheadSize = 14.0;
    private const double LoopRadius = 40.0;
    private const double StateWidth = 120;
    private const double StateHeight = 60;

    public BlueprintEditorView()
    {
        InitializeComponent();
        InitializePopOver();
    }

    private void InitializePopOver()
    {
        _popOverView = new EditArrowPopOverView();
        _popOverViewModel = new EditArrowPopOverViewModel();
        _popOverView.DataContext = _popOverViewModel;

        _popOverView.AliasChanged += OnPopOverAliasChanged;
        _popOverView.PredicateChanged += OnPopOverPredicateChanged;
        _popOverView.DismissRequested += OnPopOverDismissRequested;

        EditArrowPopup.Child = _popOverView;
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
    }

    private void OnStatePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not BlueprintStateViewModel stateVm)
            return;

        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        if (e.ClickCount > 1)
        {
            stateVm.IsEditable = true;
            return;
        }

        if (vm.CurrentMode == BlueprintEditorMode.AddTransition)
        {
            if (vm.SelectedState != null)
            {
                if (stateVm != vm.SelectedState)
                {
                    vm.SelectedState = stateVm;
                    vm.AddTransitionCommand.Execute(null);
                }
                else
                {
                    CreateLoopFromSelected(stateVm);
                }
            }
            else
            {
                vm.SelectedState = stateVm;
            }
        }
        else if (vm.CurrentMode == BlueprintEditorMode.RemoveState)
        {
            vm.SelectedState = stateVm;
            vm.RemoveStateCommand.Execute(null);
        }
    }

    private void OnArrowClicked(object? sender, BlueprintTransactionViewModel tx)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        if (vm.CurrentMode == BlueprintEditorMode.RemoveTransition)
        {
            vm.SelectedTransaction = tx;
            vm.RemoveTransitionCommand.Execute(null);
        }
    }

    private void OnArrowHeadClicked(object? sender, BlueprintTransactionViewModel tx)
    {
        _editingTransaction = tx;
        _popOverViewModel!.Alias = tx.Alias;
        _popOverViewModel.Predicate = tx.Predicate;
        EditArrowPopup.IsOpen = true;
    }

    private void OnLoopClicked(object? sender, BlueprintLoopTransactionViewModel loop)
    {
    }

    private void OnLoopArrowHeadClicked(object? sender, BlueprintLoopTransactionViewModel loop)
    {
    }

    private void OnPopOverAliasChanged()
    {
        if (_editingTransaction != null && _popOverViewModel != null)
        {
            _editingTransaction.Alias = _popOverViewModel.Alias ?? "";
        }
    }

    private void OnPopOverPredicateChanged()
    {
        if (_editingTransaction != null && _popOverViewModel != null)
        {
            _editingTransaction.Predicate = _popOverViewModel.Predicate ?? "";
        }
    }

    private void OnPopOverDismissRequested()
    {
        EditArrowPopup.IsOpen = false;
        _editingTransaction = null;
    }

    private void CreateLoopFromSelected(BlueprintStateViewModel state)
    {
        var vm = DataContext as BlueprintEditorViewModel;
        if (vm is null) return;

        foreach (var loop in vm.LoopTransactions)
        {
            if (loop.State == state) return;
        }

        var newLoop = new Domain.Models.BlueprintLoopTransactionModel
        {
            StateName = state.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };

        vm.AddLoop(newLoop);
        vm.ResetEditorModeCommand.Execute(null);
    }
}

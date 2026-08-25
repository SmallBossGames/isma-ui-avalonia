using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Views;

public partial class EditArrowPopOverView : UserControl
{
    private EditArrowPopOverViewModel? _viewModel;

    public event Action? AliasChanged;
    public event Action? PredicateChanged;
    public event Action? DismissRequested;

    public string? Alias
    {
        get => _viewModel?.Alias;
        set
        {
            if (_viewModel != null)
            {
                _viewModel.Alias = value ?? "";
            }
        }
    }

    public string? Predicate
    {
        get => _viewModel?.Predicate;
        set
        {
            if (_viewModel != null)
            {
                _viewModel.Predicate = value ?? "";
            }
        }
    }

    public EditArrowPopOverView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is EditArrowPopOverViewModel vm)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = vm;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditArrowPopOverViewModel.Alias))
        {
            AliasChanged?.Invoke();
        }
        else if (e.PropertyName == nameof(EditArrowPopOverViewModel.Predicate))
        {
            PredicateChanged?.Invoke();
        }
    }

}

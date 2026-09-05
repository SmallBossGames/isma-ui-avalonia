using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.App.Models;

namespace ISMA.App.ViewModels;

public partial class SelectVariablesDialogViewModel : ObservableObject
{
    private ObservableCollection<string> _allColumns = new();
    private ObservableCollection<NamedPickerItem> _yAxisItems = new();

    [ObservableProperty]
    private string _selectedXAxis = "TIME";

    public ObservableCollection<string> AllColumns
    {
        get => _allColumns;
        set => SetProperty(ref _allColumns, value);
    }

    public ObservableCollection<NamedPickerItem> YAxisItems
    {
        get => _yAxisItems;
        set => SetProperty(ref _yAxisItems, value);
    }


    [ObservableProperty]
    private bool _okPressed;

    [ObservableProperty]
    private bool _closePressed;

    public SelectVariablesDialogViewModel()
    {
        AllColumns.Add("TIME");
        SelectedXAxis = "TIME";
    }

    public void InitializeColumns(IEnumerable<string> columns)
    {
        AllColumns.Clear();
        foreach (var col in columns)
        {
            AllColumns.Add(col);
        }

        YAxisItems.Clear();
        foreach (var col in columns)
        {
            YAxisItems.Add(new NamedPickerItem { Name = col, Value = col });
        }
    }

    public IReadOnlyList<string> GetSelectedYAxes()
    {
        return YAxisItems
            .Where(item => item.IsSelected)
            .Select(item => item.Value)
            .ToList();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in YAxisItems)
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void UnselectAll()
    {
        foreach (var item in YAxisItems)
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        OkPressed = true;
    }

    [RelayCommand]
    private void Close()
    {
        ClosePressed = true;
    }
}

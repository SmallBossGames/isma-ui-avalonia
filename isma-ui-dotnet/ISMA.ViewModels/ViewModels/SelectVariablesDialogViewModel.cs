using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.ViewModels.Models;

namespace ISMA.ViewModels.ViewModels;

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
    private ObservableCollection<string> _selectedYAxes = new();

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

    [RelayCommand]
    private void SelectAll()
    {
        SelectedYAxes.Clear();
        foreach (var item in YAxisItems)
        {
            if (!SelectedYAxes.Contains(item.Value))
                SelectedYAxes.Add(item.Value);
        }
    }

    [RelayCommand]
    private void UnselectAll()
    {
        SelectedYAxes.Clear();
    }

    [RelayCommand]
    private void Ok()
    {
    }

    [RelayCommand]
    private void Close()
    {
    }
}

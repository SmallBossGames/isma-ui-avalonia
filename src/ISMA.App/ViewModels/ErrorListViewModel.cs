using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;
using ISMA.App.Services;

namespace ISMA.App.ViewModels;

public partial class ErrorListViewModel : ObservableObject, IModelErrorService
{
    private ObservableCollection<ErrorInfo> _errors = new();

    public ObservableCollection<ErrorInfo> Errors
    {
        get => _errors;
        set => SetProperty(ref _errors, value);
    }

    public int ErrorCount => Errors.Count;

    public void PutErrorList(IEnumerable<ErrorInfo> errorInfos)
    {
        Errors.Clear();
        foreach (var error in errorInfos)
        {
            Errors.Add(error);
        }
    }

    public void ClearErrors()
    {
        Errors.Clear();
    }

    public IReadOnlyList<ErrorInfo> GetErrors() => Errors;
}

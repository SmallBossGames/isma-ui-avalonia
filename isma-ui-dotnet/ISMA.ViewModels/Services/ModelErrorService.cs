using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.Services;

public sealed class ModelErrorService : IModelErrorService
{
    private readonly List<ErrorInfo> _errors = new();

    public void PutErrorList(IEnumerable<ErrorInfo> errors)
    {
        lock (_errors)
        {
            _errors.Clear();
            foreach (var error in errors)
            {
                _errors.Add(error);
            }
        }
    }

    public void ClearErrors()
    {
        lock (_errors)
        {
            _errors.Clear();
        }
    }

    public IReadOnlyList<ErrorInfo> GetErrors()
    {
        lock (_errors)
        {
            return _errors.AsReadOnly();
        }
    }
}

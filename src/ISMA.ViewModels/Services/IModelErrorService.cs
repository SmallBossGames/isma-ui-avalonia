using ISMA.Domain.Models;

namespace ISMA.ViewModels.Services;

public interface IModelErrorService
{
    void PutErrorList(IEnumerable<ErrorInfo> errors);
    void ClearErrors();
    IReadOnlyList<ErrorInfo> GetErrors();
}

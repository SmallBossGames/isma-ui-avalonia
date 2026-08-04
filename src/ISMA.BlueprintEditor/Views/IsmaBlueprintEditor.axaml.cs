using Avalonia.Controls;
using Avalonia.Layout;
using ISMA.BlueprintEditor.Models;
using ISMA.BlueprintEditor.Services;
using ISMA.BlueprintEditor.ViewModels;
using Panel = Avalonia.Controls.Panel;

namespace ISMA.BlueprintEditor.Views;

public partial class IsmaBlueprintEditor : Panel
{
    private readonly IsmaBlueprintViewModel _viewModel;
    private readonly CanvasView _canvasView;

    public IsmaBlueprintEditor()
    {
        _viewModel = new IsmaBlueprintViewModel();
        _canvasView = new CanvasView { CanvasViewModel = _viewModel.CanvasViewModel };
        Initialize();
    }

    public IsmaBlueprintEditor(ITextEditorFactory? textEditorFactory)
    {
        _viewModel = new IsmaBlueprintViewModel(textEditorFactory);
        _canvasView = new CanvasView { CanvasViewModel = _viewModel.CanvasViewModel };
        Initialize();
    }

    private void Initialize()
    {
        Children.Add(_canvasView);
    }

    public IsmaBlueprintViewModel ViewModel => _viewModel;

    public BlueprintModel GetBlueprintModel()
    {
        return _viewModel.ToBlueprintModel();
    }

    public void SetBlueprintModel(BlueprintModel model)
    {
        _viewModel.FromBlueprintModel(model);
    }
}

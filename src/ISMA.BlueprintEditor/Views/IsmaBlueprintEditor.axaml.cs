using Avalonia.Controls;
using ISMA.BlueprintEditor.Models;
using ISMA.BlueprintEditor.Services;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Views;

public partial class IsmaBlueprintEditor : Controls.Panel
{
    private readonly IsmaBlueprintViewModel _viewModel;

    public IsmaBlueprintEditor()
    {
        InitializeComponent();
        _viewModel = new IsmaBlueprintViewModel();
        DataContext = _viewModel;
    }

    public IsmaBlueprintEditor(ITextEditorFactory? textEditorFactory)
    {
        InitializeComponent();
        _viewModel = new IsmaBlueprintViewModel(textEditorFactory);
        DataContext = _viewModel;
    }

    private void OnTabsSelectionChanged(object? sender, Controls.SelectionChangedEventArgs e)
    {
        var diagramTabSelected = ReferenceEquals(DiagramTab.Content, Tabs.SelectedItem);
        _viewModel.DiagramTabSelected = diagramTabSelected;
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

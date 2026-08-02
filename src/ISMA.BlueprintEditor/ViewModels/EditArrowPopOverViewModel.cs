using CommunityToolkit.Mvvm.ComponentModel;

namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>
/// Simple data holder for the arrow editing popover.
/// </summary>
public partial class EditArrowPopOverViewModel : ObservableObject
{
    private string _alias = "";
    private string _predicate = "";

    /// <summary>
    /// Gets or sets the alias.
    /// </summary>
    public string Alias
    {
        get => _alias;
        set => SetProperty(ref _alias, value);
    }

    /// <summary>
    /// Gets or sets the predicate.
    /// </summary>
    public string Predicate
    {
        get => _predicate;
        set => SetProperty(ref _predicate, value);
    }
}

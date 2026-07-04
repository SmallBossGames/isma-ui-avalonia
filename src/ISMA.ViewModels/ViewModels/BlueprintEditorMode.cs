namespace ISMA.ViewModels.ViewModels;

public abstract class EditorMode
{
    public sealed class Default : EditorMode { }

    public sealed class AddTransition : EditorMode
    {
        public AddTransition(IList<BlueprintStateViewModel> selectedStates)
        {
            SelectedStates = selectedStates;
        }

        public IList<BlueprintStateViewModel> SelectedStates { get; }
    }

    public sealed class RemoveState : EditorMode { }

    public sealed class RemoveTransition : EditorMode { }
}

global using global::Xunit;
using FluentAssertions;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

/// <summary>
/// Ported from the original ISMA Kotlin/JavaFX <c>EditorModeTest</c>,
/// adapted to the C# mode type hierarchy.
/// </summary>
public class EditorModeTests
{
    [Fact]
    public void IdleModeIsCorrectlyIdentified()
    {
        EditorMode mode = EditorMode.Idle;

        (mode is EditorMode.RemoveState).Should().BeFalse();
        (mode is EditorMode.AddTransition).Should().BeFalse();
        (mode is EditorMode.RemoveTransition).Should().BeFalse();
    }

    [Fact]
    public void RemoveStateModeIsCorrectlyIdentified()
    {
        EditorMode mode = new EditorMode.RemoveState();

        (mode is EditorMode.RemoveState).Should().BeTrue();
        (mode is EditorMode.AddTransition).Should().BeFalse();
    }

    [Fact]
    public void AddTransitionModeCarriesSelectedStates()
    {
        EditorMode mode = new EditorMode.AddTransition();

        (mode as EditorMode.AddTransition)!.SelectedStates.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTransitionModeIsCorrectlyIdentified()
    {
        EditorMode mode = new EditorMode.RemoveTransition();

        (mode is EditorMode.RemoveTransition).Should().BeTrue();
    }

    [Fact]
    public void ModesAreMutuallyExclusiveByType()
    {
        EditorMode[] modes =
        {
            EditorMode.Idle,
            new EditorMode.AddTransition(),
            new EditorMode.RemoveState(),
            new EditorMode.RemoveTransition(),
        };

        modes.Distinct().Count().Should().Be(4);
    }

    [Fact]
    public void AddTransitionModeCanAddAndCheckStates()
    {
        var state1 = new StateViewModel("State 1", "", 0, 0, 110, 65, StateKind.User, _ => true);
        var state2 = new StateViewModel("State 2", "", 0, 0, 110, 65, StateKind.User, _ => true);
        var mode = new EditorMode.AddTransition();
        mode.SelectedStates.Add(state1);
        mode.SelectedStates.Add(state2);

        mode.SelectedStates.Should().Contain(state1);
        mode.SelectedStates.Should().Contain(state2);
        mode.SelectedStates.Count.Should().Be(2);
    }
}

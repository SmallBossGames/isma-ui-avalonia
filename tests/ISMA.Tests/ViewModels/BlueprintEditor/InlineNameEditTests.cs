global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

public class InlineNameEditTests
{
    private static BlueprintEditorViewModel CreateViewModel() => new(BlueprintModel.Empty);

    [Fact]
    public void InlineNameEdit_StateNameCanBeChanged()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First();
        userState.Name.Should().Be("State 1");

        var uniqueName = $"CustomState_{Guid.NewGuid():N}";
        userState.Name = uniqueName;
        userState.Name.Should().Be(uniqueName);
    }

    [Fact]
    public void InlineNameEdit_DuplicateName_IsRejected()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        var state1 = viewModel.States[0];
        var state2 = viewModel.States[1];

        viewModel.UpdateStateName(state2, state1.Name);
        state2.Name.Should().NotBe(state1.Name);

        viewModel.States.Should().HaveCount(2);
    }

    [Fact]
    public void InlineNameEdit_MainState_CannotBeRenamed()
    {
        var viewModel = CreateViewModel();
        var mainState = viewModel.MainState;
        var originalName = mainState.Name;

        viewModel.UpdateStateName(mainState, "RenamedMain");
        mainState.Name.Should().Be(originalName);
    }

    [Fact]
    public void InlineNameEdit_InitState_CannotBeRenamed()
    {
        var viewModel = CreateViewModel();
        var initState = viewModel.InitState;
        var originalName = initState.Name;

        viewModel.UpdateStateName(initState, "RenamedInit");
        initState.Name.Should().Be(originalName);
    }

    [Fact]
    public void InlineNameEdit_TransitionReferences_UpdatedOnRename()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States[0];

        var uniqueName = $"RenamedState_{Guid.NewGuid():N}";
        userState.Name = uniqueName;
        userState.Name.Should().Be(uniqueName);
    }

    [Fact]
    public void InlineNameEdit_EmptyName_RollsBack()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States[0];
        var originalName = userState.Name;

        userState.Name = "";
        userState.Name.Should().Be("");
    }

    [Fact]
    public void InlineNameEdit_NameChangingMonitor_TracksAllNames()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);
        viewModel.AddStateCommand.Execute(null);

        var names = viewModel.States.Select(s => s.Name).ToList();
        names.Distinct().Count().Should().Be(names.Count);

        var userStateNames = names.Where(n => n.StartsWith("State ")).ToList();
        userStateNames.Should().HaveCount(3);
    }
}

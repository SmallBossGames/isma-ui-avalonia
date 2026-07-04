global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.ViewModels.BlueprintEditor;

public class StateTextSyncTests
{
    private static BlueprintEditorViewModel CreateViewModel() => new(BlueprintModel.Empty);

    [Fact]
    public void OpenStateTextEditor_RaisesEvent()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First(s => !s.IsMain && !s.IsInit);

        bool eventFired = false;
        BlueprintStateViewModel? eventState = null;
        viewModel.StateTextEditorRequested += state =>
        {
            eventFired = true;
            eventState = state;
        };

        viewModel.OpenStateTextEditor(userState);

        eventFired.Should().BeTrue();
        eventState.Should().Be(userState);
        eventState!.Name.Should().Be(userState.Name);
    }

    [Fact]
    public void OpenLoopTextEditor_RaisesEvent()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First(s => !s.IsMain && !s.IsInit);

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateName = userState.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);
        viewModel.LoopTransactions.Should().HaveCount(1);

        bool eventFired = false;
        BlueprintLoopTransactionViewModel? eventLoop = null;
        viewModel.LoopTextEditorRequested += loop =>
        {
            eventFired = true;
            eventLoop = loop;
        };

        viewModel.OpenLoopTextEditor(viewModel.LoopTransactions[0]);

        eventFired.Should().BeTrue();
        eventLoop.Should().Be(viewModel.LoopTransactions[0]);
    }

    [Fact]
    public void UpdateStateText_UpdatesViewModel()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First(s => !s.IsMain && !s.IsInit);

        userState.Text.Should().BeNullOrEmpty();

        viewModel.UpdateStateText(userState, "This is a test state description");

        userState.Text.Should().Be("This is a test state description");
    }

    [Fact]
    public void UpdateLoopText_UpdatesViewModel()
    {
        var viewModel = CreateViewModel();
        viewModel.Mode = new EditorMode.Default();
        viewModel.AddStateCommand.Execute(null);
        var userState = viewModel.States.First(s => !s.IsMain && !s.IsInit);

        var loopModel = new BlueprintLoopTransactionModel
        {
            StateName = userState.Name,
            Predicate = "1 > 0",
            Alias = "",
            Text = ""
        };
        viewModel.AddLoop(loopModel);
        viewModel.LoopTransactions.Should().HaveCount(1);
        var loopVm = viewModel.LoopTransactions[0];

        loopVm.Text.Should().BeNullOrEmpty();

        viewModel.UpdateLoopText(loopVm, "Loop condition: x > 0");

        loopVm.Text.Should().Be("Loop condition: x > 0");
    }
}

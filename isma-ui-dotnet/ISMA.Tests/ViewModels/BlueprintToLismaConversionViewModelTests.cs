global using global::Xunit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class BlueprintToLismaConversionViewModelTests
{
    private static Mock<ITextEditorFactory> CreateEditorFactoryMock() => new();

    private static BlueprintProjectViewModel CreateProject(BlueprintModel? model = null, string? filePath = null)
    {
        var mockFileService = new Mock<IProjectFileService>();
        var mockEditorFactory = CreateEditorFactoryMock();
        var blueprintModel = model ?? BlueprintModel.Empty;

        return new BlueprintProjectViewModel(
            mockFileService.Object,
            mockEditorFactory.Object,
            blueprintModel,
            filePath);
    }

    [Fact]
    public void ViewModel_UsesConverterCorrectly_GeneratesLismaText()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "state0", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var project = CreateProject(model);

        var result = project.ConvertToLisma();

        result.Should().NotBeNull();
        result.FullText.Should().Contain("state main {");
        result.FullText.Should().Contain("state init {");
        result.FullText.Should().Contain("state state0 {");
        result.Regions.Should().HaveCount(3);
    }

    [Fact]
    public void ViewModel_ConvertsEmptyBlueprint_GeneratesMainAndInit()
    {
        var project = CreateProject();

        var result = project.ConvertToLisma();

        result.FullText.Should().Contain("state main {");
        result.FullText.Should().Contain("state init {");
        result.Regions.Should().HaveCount(2);
    }

    [Fact]
    public void ViewModel_MultipleStatesAndTransitions_ConvertsCorrectly()
    {
        var model = BlueprintModel.Empty;
        var state0 = new BlueprintStateModel(100, 200, "state0", "");
        var state1 = new BlueprintStateModel(150, 250, "state1", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(state0).Add(state1),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var tx = new BlueprintTransactionModel
        {
            StartStateName = "state0",
            EndStateName = "state1",
            Predicate = "condition"
        };
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States,
            Transactions = model.Transactions.Add(tx),
            LoopTransactions = model.LoopTransactions
        };

        var editorVm = new BlueprintEditorViewModel(model);
        var project = CreateProject(model);
        project.SetEditorViewModel(editorVm);

        var result = project.ConvertToLisma();

        result.FullText.Should().Contain("state main {");
        result.FullText.Should().Contain("state init {");
        result.Regions.Should().HaveCount(5);
    }

    [Fact]
    public void ViewModel_SetModelAndConvert_ReturnsCorrectOutput()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "customState", "custom code");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var project = CreateProject(model);

        var result = project.ConvertToLisma();

        result.FullText.Should().Contain("state customState {");
        result.FullText.Should().Contain("custom code");
        result.Regions.Should().Contain(r => r.Name == "customState");
    }

    [Fact]
    public void ViewModel_GetModel_ReturnsCurrentModel()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "testState", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var editorVm = new BlueprintEditorViewModel(model);

        var currentModel = editorVm.GetBlueprintModel();

        currentModel.States.Should().HaveCount(1);
        currentModel.Main.Name.Should().Be("main");
    }

    [Fact]
    public void ViewModel_ConvertsAfterEditorReset_PreservesData()
    {
        var model = BlueprintModel.Empty;
        var userState = new BlueprintStateModel(100, 200, "state0", "");
        model = new BlueprintModel
        {
            Main = model.Main,
            Init = model.Init,
            States = model.States.Add(userState),
            Transactions = model.Transactions,
            LoopTransactions = model.LoopTransactions
        };

        var editorVm = new BlueprintEditorViewModel(model);
        var project = CreateProject(model);
        project.SetEditorViewModel(editorVm);

        editorVm.ResetEditorModeCommand.Execute(null);

        var result = project.ConvertToLisma();

        result.FullText.Should().Contain("state state0 {");
        result.Regions.Should().HaveCount(3);
    }
}

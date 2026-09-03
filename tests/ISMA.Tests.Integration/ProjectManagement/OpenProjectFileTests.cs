using Avalonia;
using System.Text.Json;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.App.ViewModels;

namespace ISMA.Tests.Integration.ProjectManagement;

/// <summary>
/// UC-03: Open and edit existing projects.
/// The native file picker cannot be driven headless, so these tests open files
/// through the project service and assert on the resulting UI (tabs, editors).
/// </summary>
public class OpenProjectFileTests
{
    private const string OriginalIscm2 = """
        {"main":{"canvasPositionX":10.0,"canvasPositionY":10.0,"name":"Main","text":"v' = -g;\ny' = v;"},
        "init":{"canvasPositionX":10.0,"canvasPositionY":100.0,"name":"init","text":""},
        "states":[
          {"canvasPositionX":534.8,"canvasPositionY":4.8,"name":"Up","text":"set v = -v;"},
          {"canvasPositionX":535.6,"canvasPositionY":300.8,"name":"Down","text":""}
        ],
        "transactions":[
          {"startStateName":"init","endStateName":"Up","predicate":"y < 0"},
          {"startStateName":"init","endStateName":"Down","predicate":"v < 0"},
          {"startStateName":"Down","endStateName":"Up","predicate":"y < 0"},
          {"startStateName":"Up","endStateName":"Down","predicate":"v < 0"}
        ],
        "loopTransactions":[]}
        """;

    private readonly TestApp _app = (TestApp)Application.Current!;

    [AvaloniaFact]
    public async Task Open_OriginalIscm2File_CreatesBlueprintTabWithAllStates()
    {
        var dir = Directory.CreateTempSubdirectory("isma-open-test");
        try
        {
            var path = Path.Combine(dir.FullName, "Bouncing Ball.iscm2");
            File.WriteAllText(path, OriginalIscm2);

            var projectService = _app.GetRequiredService<ProjectService>();
            var project = await projectService.OpenAsync(path);
            _app.ViewModel.SyncProjects();

            project.Should().BeOfType<BlueprintProjectViewModel>();
            _app.Window.GetProjectCount().Should().Be(1);
            _app.Window.GetActiveProject()!.Name.Should().Be("Bouncing Ball.iscm2");

            var model = ((BlueprintProjectViewModel)project!).GetBlueprintModel();
            model.Main.Name.Should().Be("Main");
            model.Init.Name.Should().Be("init");
            model.States.Should().HaveCount(2);
            model.States.Select(s => s.Name).Should().BeEquivalentTo("Up", "Down");
            model.Transactions.Should().HaveCount(4);

        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public async Task Open_Im2File_CreatesTextProjectWithFileContent()
    {
        var dir = Directory.CreateTempSubdirectory("isma-open-test");
        try
        {
            var path = Path.Combine(dir.FullName, "model.im2");
            const string lisma = "v' = -g;\ny' = v;\n\ny(t0) = 10;";
            File.WriteAllText(path, lisma);

            var projectService = _app.GetRequiredService<ProjectService>();
            var project = await projectService.OpenAsync(path);
            _app.ViewModel.SyncProjects();

            project.Should().BeOfType<LismaProjectViewModel>();
            _app.Window.GetProjectCount().Should().Be(1);
            _app.Window.GetActiveProject()!.Name.Should().Be("model.im2");
            ((LismaProjectViewModel)project!).FullText.Should().Be(lisma);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public async Task Open_LegacyImFile_ReportsErrorAndCreatesNoProject()
    {
        var dir = Directory.CreateTempSubdirectory("isma-open-test");
        try
        {
            var path = Path.Combine(dir.FullName, "legacy.im");
            File.WriteAllText(path, "legacy content");

            var projectService = _app.GetRequiredService<ProjectService>();
            var project = await projectService.OpenAsync(path);
            _app.ViewModel.SyncProjects();

            project.Should().BeNull();
            _app.Window.GetProjectCount().Should().Be(0);
            _app.Window.GetErrorCount().Should().Be(1);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public async Task Save_BlueprintProject_WritesOriginalIscm2Schema()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprint = (BlueprintProjectViewModel)_app.Window.GetActiveProject()!;

        var dir = Directory.CreateTempSubdirectory("isma-save-test");
        try
        {
            var path = Path.Combine(dir.FullName, "saved.iscm2");
            blueprint.FilePath = path;
            (await blueprint.SaveAsync()).Should().BeTrue();

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.EnumerateObject().Select(p => p.Name)
                .Should().BeEquivalentTo("main", "init", "states", "transactions", "loopTransactions");
            root.GetProperty("main").GetProperty("name").GetString().Should().Be("Main");
            root.GetProperty("init").GetProperty("name").GetString().Should().Be("init");
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public async Task SaveAndReload_RoundTripsBlueprintThroughIscm2File()
    {
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        var blueprint = (BlueprintProjectViewModel)_app.Window.GetActiveProject()!;

        var dir = Directory.CreateTempSubdirectory("isma-roundtrip-test");
        try
        {
            var path = Path.Combine(dir.FullName, "roundtrip.iscm2");
            blueprint.FilePath = path;
            (await blueprint.SaveAsync()).Should().BeTrue();

            var projectService = _app.GetRequiredService<ProjectService>();
            var reloaded = await projectService.OpenAsync(path);
            _app.ViewModel.SyncProjects();

            reloaded.Should().BeOfType<BlueprintProjectViewModel>();
            var model = ((BlueprintProjectViewModel)reloaded!).GetBlueprintModel();
            model.Main.Name.Should().Be("Main");
            model.Init.Name.Should().Be("init");
            model.States.Should().BeEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }
}

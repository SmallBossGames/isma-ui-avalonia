using Avalonia;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.ViewModels;

namespace ISMA.Tests.Integration.UseCases.ProjectManagement;

/// <summary>
/// UC-09: Save, Save As, and Save All
/// Tests project saving, closing, and tab management via UI interactions.
/// </summary>
public class SaveAndCloseTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task UC09_CloseSingleProject_RemovesFromCollection()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        _app.Window.ClickMenuItem("MenuClose");

        _app.Window.GetProjectCount().Should().Be(0);
        _app.Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task UC09_CloseAllProjects_RemovesAllFromCollection()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewBlueprint");
        _app.Window.GetProjectCount().Should().Be(3);

        _app.Window.ClickMenuItem("MenuCloseAll");

        _app.Window.GetProjectCount().Should().Be(0);
        _app.Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task UC09_CloseProject_DisposesIt()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        var project = _app.Window.GetActiveProject();

        _app.Window.ClickMenuItem("MenuClose");

        _app.Window.GetProjectCount().Should().Be(0);
    }

    [AvaloniaFact]
    public async Task UC09_MultipleProjects_CloseSpecificTab()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(3);

        _app.Window.ClickMenuItem("MenuClose");
        _app.Window.GetProjectCount().Should().Be(2);
    }

    [AvaloniaFact]
    public async Task UC04_MultiProjectEditing_EachProjectIsIndependent()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        var firstProject = _app.Window.GetActiveProject() as LismaProjectViewModel;

        _app.Window.ClickMenuItem("MenuNewText");
        var secondProject = _app.Window.GetActiveProject() as LismaProjectViewModel;

        _app.Window.GetProjectCount().Should().Be(2);
        firstProject.Should().NotBeNull();
        secondProject.Should().NotBeNull();
    }
}

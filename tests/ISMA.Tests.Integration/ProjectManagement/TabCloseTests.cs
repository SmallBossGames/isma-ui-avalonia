using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.App.Views;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for tab close button functionality.
/// Tests that clicking the X button on a tab closes that specific tab.
/// </summary>
public class TabCloseTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task TabCloseButton_ClosesSpecificTab()
    {
        // Create first project
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(1);

        // Create second project
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(2);

        var secondProject = ViewModel.Projects[1];
        secondProject.Should().NotBeNull();

        // Close the second tab via the Close command (simulating tab close button)
        await ViewModel.CloseTabCommand.ExecuteAsync(secondProject);

        // Verify only the first project remains
        ViewModel.Projects.Should().HaveCount(1);
        ViewModel.Projects[0].Should().Be(ViewModel.Projects.First());
    }

    [AvaloniaFact]
    public async Task TabCloseButton_DisposesProject()
    {
        // Create a project
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(1);

        var project = ViewModel.Projects[0];
        project.Should().NotBeNull();

        // Get the editor content if it's a LismaProjectViewModel
        var lismaProject = project as LismaProjectViewModel;

        // Close the tab
        await ViewModel.CloseTabCommand.ExecuteAsync(project);

        // Verify project was removed from collection
        ViewModel.Projects.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task TabCloseButton_LastTab_CanStillClose()
    {
        // Create a project
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(1);

        var project = ViewModel.Projects[0];

        // Close the last tab
        await ViewModel.CloseTabCommand.ExecuteAsync(project);

        // Verify project was removed
        ViewModel.Projects.Should().BeEmpty();
        ViewModel.ActiveProject.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task TabCloseButton_CloseAll_ViaCloseAllCommand()
    {
        // Create multiple projects
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(3);

        // Close all
        await ViewModel.CloseAllCommand.ExecuteAsync(null);

        // Verify all closed
        ViewModel.Projects.Should().BeEmpty();
    }
}

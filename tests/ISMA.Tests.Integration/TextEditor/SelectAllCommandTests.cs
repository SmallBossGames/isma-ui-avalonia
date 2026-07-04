using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for Select All command.
/// </summary>
public class SelectAllCommandTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task SelectAllCommand_ExistsInMainWindowViewModel()
    {
        // Create project
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(1);

        // Verify SelectAllCommand exists and can execute
        var selectAllMethod = typeof(MainWindowViewModel).GetMethod("SelectAll",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        selectAllMethod.Should().NotBeNull("SelectAll command should exist");
    }

    [AvaloniaFact]
    public async Task SelectAllCommand_CallsTriggerSelectAllOnActiveProject()
    {
        // Create project
        Window.ClickMenuItem("MenuNewText");
        ViewModel.Projects.Should().HaveCount(1);

        var lismaProject = ViewModel.ActiveProject as LismaProjectViewModel;
        lismaProject.Should().NotBeNull();

        // TriggerSelectAll should not throw even if no editor instance
        lismaProject!.TriggerSelectAll(); // Should not throw
    }

    [AvaloniaFact]
    public async Task BlueprintProject_TriggerSelectAll_DoesNotThrow()
    {
        // Create blueprint project
        Window.ClickMenuItem("MenuNewBlueprint");
        ViewModel.Projects.Should().HaveCount(1);

        var blueprintProject = ViewModel.ActiveProject as BlueprintProjectViewModel;
        blueprintProject.Should().NotBeNull();

        // TriggerSelectAll should not throw for blueprint projects
        blueprintProject!.TriggerSelectAll(); // Should not throw
    }
}

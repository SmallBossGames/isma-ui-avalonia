using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// UI-based tests for closing project tabs via the tab close button (X).
/// All interactions go through the actual UI control tree, not ViewModels.
/// </summary>
public class TabCloseTests : IntegrationTestBase
{
    [AvaloniaFact]
    public async Task TabCloseButton_ClosesSpecificTab()
    {
        // Create 2 projects via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(2);

        // Close the first tab (index 0) via UI
        Window.ClickTabCloseButton(0);

        // Verify only the second project remains
        Window.GetProjectCount().Should().Be(1);
        Window.GetActiveProject().Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TabCloseButton_DisposesProject()
    {
        // Create 1 project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        // Close it via UI
        Window.ClickTabCloseButton(0);

        // Verify project count is 0
        Window.GetProjectCount().Should().Be(0);
    }

    [AvaloniaFact]
    public async Task TabCloseButton_LastTab_CanStillClose()
    {
        // Create 1 project via UI
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(1);

        // Close the last remaining tab — should not crash
        Window.ClickTabCloseButton(0);

        Window.GetProjectCount().Should().Be(0);
        Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task TabCloseButton_CloseAll_ViaCloseAllCommand()
    {
        // Create 3 projects via UI
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.ClickMenuItem("MenuNewText");
        Window.GetProjectCount().Should().Be(3);

        // Close all via UI menu
        Window.ClickMenuItem("MenuCloseAll");

        Window.GetProjectCount().Should().Be(0);
        Window.GetActiveProject().Should().BeNull();
    }
}

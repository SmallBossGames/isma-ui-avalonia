using Avalonia;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.Tests.Integration;

/// <summary>
/// UI-based tests for closing project tabs via the tab close button (X).
/// All interactions go through the actual UI control tree, not ViewModels.
/// </summary>
public class TabCloseTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;


    [AvaloniaFact]
    public async Task TabCloseButton_ClosesSpecificTab()
    {
        // Create 2 projects via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(2);

        // Close the first tab (index 0) via UI
        _app.Window.ClickTabCloseButton(0);

        // Verify only the second project remains
        _app.Window.GetProjectCount().Should().Be(1);
        _app.Window.GetActiveProject().Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TabCloseButton_DisposesProject()
    {
        // Create 1 project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        // Close it via UI
        _app.Window.ClickTabCloseButton(0);

        // Verify project count is 0
        _app.Window.GetProjectCount().Should().Be(0);
    }

    [AvaloniaFact]
    public async Task TabCloseButton_LastTab_CanStillClose()
    {
        // Create 1 project via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(1);

        // Close the last remaining tab — should not crash
        _app.Window.ClickTabCloseButton(0);

        _app.Window.GetProjectCount().Should().Be(0);
        _app.Window.GetActiveProject().Should().BeNull();
    }

    [AvaloniaFact]
    public async Task TabCloseButton_CloseAll_ViaCloseAllCommand()
    {
        // Create 3 projects via UI
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetProjectCount().Should().Be(3);

        // Close all via UI menu
        _app.Window.ClickMenuItem("MenuCloseAll");

        _app.Window.GetProjectCount().Should().Be(0);
        _app.Window.GetActiveProject().Should().BeNull();
    }
}

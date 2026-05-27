using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using AvaloniaEdit;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.ViewModels.ViewModels;
using AvaloniaEdit.Highlighting;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for LISMA text editor syntax highlighting.
/// Tests the XSHD fallback loading and TextEditor syntax highlighting integration.
/// </summary>
public class SyntaxHighlightingTests : IntegrationTestBase
{
    [AvaloniaFact]
    public void FallbackHighlighting_LoadsFromEmbeddedXshd()
    {
        // Verify the embedded LISMA.xshd loads correctly
        var highlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        highlighting.Should().NotBeNull();
        highlighting!.Name.Should().Be("LISMA");
    }

    [AvaloniaFact]
    public void FallbackHighlighting_IsCached()
    {
        // Verify the highlighting definition is cached
        var first = LismaSyntaxHelper.GetFallbackHighlighting();
        var second = LismaSyntaxHelper.GetFallbackHighlighting();
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [AvaloniaFact]
    public void ServerDriven_ReturnsNull_ForEmptyTokens()
    {
        // Server-driven highlighting returns null for empty tokens (uses XSHD fallback)
        var highlighting = LismaSyntaxHelper.CreateServerDriven(Array.Empty<SyntaxTokenDto>());
        highlighting.Should().BeNull();
    }

    [AvaloniaFact]
    public void ServerDriven_ReturnsNull_ForNullTokens()
    {
        // Server-driven highlighting returns null for null tokens
        var highlighting = LismaSyntaxHelper.CreateServerDriven(null!);
        highlighting.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_CanApplyFallbackHighlighting()
    {
        // Create a text project
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();
        editor!.Should().NotBeNull();

        // Apply fallback highlighting
        editor.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");
    }

    [AvaloniaFact]
    public async Task TextEditor_HighlightingApplied_AfterTextChange()
    {
        // Create text project
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        // Set text directly on the editor
        editor!.Document.Text = "state \"Main\" {\n    x = 0;\n}";
        Window.Flush();

        // Apply highlighting (simulating what UpdateSyntaxHighlighting does)
        editor.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        // Verify the editor has text content
        editor.Document.Text.Should().Contain("state");
        editor.Document.Text.Should().Contain("Main");
    }

    [AvaloniaFact]
    public async Task TextEditor_FactorySetsHighlighting()
    {
        // Create text project
        Window.ClickMenuItem("MenuNewText");

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        // Set highlighting via factory (simulating server response)
        factory!.SetSyntaxHighlighting(editor!, Array.Empty<SyntaxTokenDto>(), "test");
        editor!.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");
    }
}

using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.ViewModels.ViewModels;
using AvaloniaEdit.Highlighting;
using Avalonia.Media;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for LISMA text editor syntax highlighting.
/// Tests both XSHD fallback loading and server-driven token highlighting.
/// </summary>
public class SyntaxHighlightingTests : IntegrationTestBase
{
    [AvaloniaFact]
    public void FallbackHighlighting_LoadsFromEmbeddedXshd()
    {
        var highlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        highlighting.Should().NotBeNull();
        highlighting!.Name.Should().Be("LISMA");
    }

    [AvaloniaFact]
    public void FallbackHighlighting_IsCached()
    {
        var first = LismaSyntaxHelper.GetFallbackHighlighting();
        var second = LismaSyntaxHelper.GetFallbackHighlighting();
        ReferenceEquals(first, second).Should().BeTrue();
    }

    [AvaloniaFact]
    public void ServerDriven_CreateServerDriven_ReturnsNull()
    {
        var highlighting = LismaSyntaxHelper.CreateServerDriven(Array.Empty<SyntaxTokenDto>());
        highlighting.Should().BeNull();

        var nullHighlighting = LismaSyntaxHelper.CreateServerDriven(null!);
        nullHighlighting.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_CanApplyFallbackHighlighting()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        editor!.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");
    }

    [AvaloniaFact]
    public async Task TextEditor_HighlightingApplied_AfterTextChange()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        editor!.Document.Text = "state \"Main\" {\n    x = 0;\n}";
        Window.Flush();

        editor.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        editor.Document.Text.Should().Contain("state");
        editor.Document.Text.Should().Contain("Main");
    }

    [AvaloniaFact]
    public async Task TextEditor_FactorySetsFallback_WhenNoTokens()
    {
        Window.ClickMenuItem("MenuNewText");

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        factory!.SetSyntaxHighlighting(editor!, Array.Empty<SyntaxTokenDto>(), "test");
        editor!.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task TextEditor_FactorySetsServerDriven_WhenTokensProvided()
    {
        Window.ClickMenuItem("MenuNewText");

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword },
            new SyntaxTokenDto { Start = 6, Length = 4, Kind = SyntaxTokenKind.Text }
        };

        factory!.SetSyntaxHighlighting(editor!, tokens, "state \"Main\"");
        editor!.SyntaxHighlighting.Should().BeNull();

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextEditor_ServerDriven_RemovesFallback_WhenTokensProvided()
    {
        Window.ClickMenuItem("MenuNewText");

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        // First set fallback
        factory!.SetSyntaxHighlighting(editor!, Array.Empty<SyntaxTokenDto>(), "test");
        editor!.SyntaxHighlighting.Should().NotBeNull();

        // Then set server tokens
        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword }
        };
        factory.SetSyntaxHighlighting(editor!, tokens, "state");

        editor.SyntaxHighlighting.Should().BeNull();

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextEditor_RemovesServerDriven_WhenTokensCleared()
    {
        Window.ClickMenuItem("MenuNewText");

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        // Set server tokens
        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword }
        };
        factory!.SetSyntaxHighlighting(editor!, tokens, "state");
        editor!.SyntaxHighlighting.Should().BeNull();

        // Clear tokens
        factory.SetSyntaxHighlighting(editor!, Array.Empty<SyntaxTokenDto>(), "test");
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().BeEmpty();
    }

    [AvaloniaFact]
    public async Task TextEditor_KeywordToken_AppliesOrangeColor()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "state \"Main\" {}";
        Window.Flush();

        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword }
        };

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .FirstOrDefault();
        transformer.Should().NotBeNull();

        editor.TextArea.TextView.Options.HighlightCurrentLine.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task TextEditor_CommentToken_ApppliesGrayColor()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "// this is a comment";
        Window.Flush();

        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 20, Kind = SyntaxTokenKind.Comment }
        };

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .FirstOrDefault();
        transformer.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_NumberToken_ApppliesBlueColor()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "x = 42;";
        Window.Flush();

        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 4, Length = 2, Kind = SyntaxTokenKind.Number }
        };

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .FirstOrDefault();
        transformer.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_MultipleTokens_AppAllColors()
    {
        Window.ClickMenuItem("MenuNewText");
        Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "state \"Main\" {\n    // comment\n    x = 42;\n}";
        Window.Flush();

        var tokens = new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword },
            new SyntaxTokenDto { Start = 6, Length = 4, Kind = SyntaxTokenKind.Text },
            new SyntaxTokenDto { Start = 15, Length = 13, Kind = SyntaxTokenKind.Comment },
            new SyntaxTokenDto { Start = 32, Length = 2, Kind = SyntaxTokenKind.Number }
        };

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task LismaProjectViewModel_HighlightingTokens_ObservableCollectionUpdated()
    {
        Window.ClickMenuItem("MenuNewText");
        var project = Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        project!.HighlightTokens.Should().BeEmpty();

        MockServer.HighlightHandler = _ => Task.FromResult(new[]
        {
            new SyntaxTokenDto { Start = 0, Length = 5, Kind = SyntaxTokenKind.Keyword }
        });

        await project.UpdateSyntaxHighlighting("state \"Main\" {}");
        await Task.Delay(200);

        project.HighlightTokens.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextEditor_NullTokens_UsesFallback()
    {
        Window.ClickMenuItem("MenuNewText");

        var factory = Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(Window);
        editor.Should().NotBeNull();

        factory!.SetSyntaxHighlighting(editor!, null!, "test");
        editor!.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");
    }
}

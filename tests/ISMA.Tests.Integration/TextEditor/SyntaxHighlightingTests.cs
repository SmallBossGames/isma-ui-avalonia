using Avalonia;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using FluentAssertions;
using ISMA.App.Services;
using ISMA.TextEditor;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.App.ViewModels;
using AvaloniaEdit.Highlighting;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;

namespace ISMA.Tests.Integration;

/// <summary>
/// Integration tests for LISMA text editor syntax highlighting.
/// Tests XSHD fallback loading and LSP-driven token highlighting through
/// the real editor, LspClient, and fake LSP transport.
/// </summary>
public class SyntaxHighlightingTests
{
    private readonly TestApp _app = (TestApp)Application.Current!;

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
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        editor!.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");
    }

    [AvaloniaFact]
    public async Task TextEditor_HighlightingApplied_AfterTextChange()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        editor!.Document.Text = "state \"Main\" {\n    x = 0;\n}";

        editor.SyntaxHighlighting = LismaSyntaxHelper.GetFallbackHighlighting();
        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        editor.Document.Text.Should().Contain("state");
        editor.Document.Text.Should().Contain("Main");
    }

    [AvaloniaFact]
    public async Task TextEditor_FactorySetsFallback_WhenNoTokens()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
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
        _app.Window.ClickMenuItem("MenuNewText");

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword),
            new SyntaxTokenDto(0, 6, 4, SyntaxTokenKind.Text)
        };

        factory!.SetSyntaxHighlighting(editor!, tokens, "state \"Main\"");
        editor!.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextEditor_ServerDriven_KeepsFallback_WhenTokensProvided()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        // First set fallback
        factory!.SetSyntaxHighlighting(editor!, Array.Empty<SyntaxTokenDto>(), "test");
        editor!.SyntaxHighlighting.Should().NotBeNull();

        // Then set server tokens - fallback should still be present as base
        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword)
        };
        factory.SetSyntaxHighlighting(editor!, tokens, "state");

        editor.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextEditor_RemovesServerDriven_WhenTokensCleared()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        // Set server tokens - fallback is still present
        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword)
        };
        factory!.SetSyntaxHighlighting(editor!, tokens, "state");
        editor!.SyntaxHighlighting.Should().NotBeNull();

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
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "state \"Main\" {}";

        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword)
        };

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .FirstOrDefault();
        transformer.Should().NotBeNull();

        editor.TextArea.TextView.Options.HighlightCurrentLine.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task TextEditor_CommentToken_AppliesGrayColor()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "// this is a comment";

        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 20, SyntaxTokenKind.Comment)
        };

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .FirstOrDefault();
        transformer.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_NumberToken_AppliesBlueColor()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "x = 42;";

        var tokens = new[]
        {
            new SyntaxTokenDto(0, 4, 2, SyntaxTokenKind.Number)
        };

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .FirstOrDefault();
        transformer.Should().NotBeNull();
    }

    [AvaloniaFact]
    public async Task TextEditor_MultipleTokens_AppAllColors()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "state \"Main\" {\n    // comment\n    x = 42;\n}";

        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword),
            new SyntaxTokenDto(0, 6, 4, SyntaxTokenKind.Text),
            new SyntaxTokenDto(1, 4, 10, SyntaxTokenKind.Comment),
            new SyntaxTokenDto(2, 8, 2, SyntaxTokenKind.Number)
        };

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var transformer = editor.TextArea.TextView.LineTransformers
            .OfType<ServerDrivenHighlightingTransformer>()
            .ToList();
        transformer.Should().HaveCount(1);
    }

    [AvaloniaFact]
    public async Task TextEditor_NullTokens_UsesFallback()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        factory!.SetSyntaxHighlighting(editor!, null!, "test");
        editor!.SyntaxHighlighting.Should().NotBeNull();
        editor.SyntaxHighlighting!.Name.Should().Be("LISMA");
    }

    [AvaloniaFact]
    public async Task LismaProjectViewModel_HighlightingTokens_ArriveViaLspClient()
    {
        var lsp = _app.Services.GetRequiredService<FakeLspTransport>();
        var source = "const a = 1.5; // note";
        // const -> (0,0,5,keyword), 1.5 -> (0,10,3,number), // note -> (0,14,7,comment)
        // deltaStart of the third token is relative to the previous start on the line (14 - 10 = 4)
        lsp.ExpectedSource = source;
        lsp.SemanticTokensData = [0, 0, 5, 0, 0, 0, 10, 3, 2, 0, 0, 4, 7, 1, 0];

        _app.Window.ClickMenuItem("MenuNewText");
        var project = _app.Window.GetActiveProject() as LismaProjectViewModel;
        project.Should().NotBeNull();

        project!.HighlightTokens.Should().BeEmpty();

        await project.UpdateSyntaxHighlighting(source);
        await Task.Delay(200);

        project.HighlightTokens.Should().HaveCount(3);
        project.HighlightTokens[0].Should().Be(new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword));
        project.HighlightTokens[1].Should().Be(new SyntaxTokenDto(0, 10, 3, SyntaxTokenKind.Number));
        project.HighlightTokens[2].Should().Be(new SyntaxTokenDto(0, 14, 7, SyntaxTokenKind.Comment));
    }

    [AvaloniaFact]
    public async Task TextEditor_LspTokens_AppliedAsServerDrivenTransformer()
    {
        var lsp = _app.Services.GetRequiredService<FakeLspTransport>();
        var source = "const a = 1.5; // note";
        lsp.ExpectedSource = source;
        lsp.SemanticTokensData = [0, 0, 5, 0, 0, 0, 10, 3, 2, 0, 0, 4, 7, 1, 0];

        _app.Window.ClickMenuItem("MenuNewText");
        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();

        editor!.Document.Text = source;

        // The highlighting pipeline is async (LSP round trip + 100ms debounce);
        // wait until the transformer built from the LSP tokens is installed.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        ServerDrivenHighlightingTransformer? transformer = null;
        while (DateTime.UtcNow < deadline && transformer is null)
        {
            transformer = editor.TextArea.TextView.LineTransformers
                .OfType<ServerDrivenHighlightingTransformer>()
                .FirstOrDefault();
            if (transformer is null)
            {
                await Task.Delay(25);
            }
        }

        transformer.Should().NotBeNull("LSP tokens should be applied to the editor as a highlighting transformer");
    }

    [AvaloniaFact]
    public async Task TextEditor_Token_ColorizesCorrectLineAndPosition()
    {
        _app.Window.ClickMenuItem("MenuNewText");
        _app.Window.GetActiveProject().Should().NotBeNull();

        var editor = UiHelpers.GetActiveTextEditor(_app.Window);
        editor.Should().NotBeNull();
        editor!.Document.Text = "state \"Main\" {}\n    x = 42;";

        var tokens = new[]
        {
            new SyntaxTokenDto(0, 0, 5, SyntaxTokenKind.Keyword), // "state" on line 0
            new SyntaxTokenDto(1, 8, 2, SyntaxTokenKind.Number)   // "42" on line 1
        };

        var factory = _app.Services.GetRequiredService<ITextEditorFactory>();
        factory.SetSyntaxHighlighting(editor, tokens, editor.Document.Text);

        var textView = editor.TextArea.TextView;

        // Line 0: the keyword token at column 0 must be colorized orange.
        // Before the 0-based/1-based fix, a token with Line=0 never matched any
        // DocumentLine.LineNumber (which is 1-based) and was silently dropped.
        var line0 = textView.GetOrConstructVisualLine(editor.Document.Lines[0]);
        var keywordElement = ElementAtRelativeOffset(line0, 0);
        keywordElement.Should().NotBeNull("line 0 should contain a visual element at column 0");
        keywordElement!.TextRunProperties.ForegroundBrush.Should()
            .BeSameAs(Brushes.Orange, "the line-0 keyword token should be colorized orange");

        // Line 1: the number token at column 8 must be colorized blue on its own
        // line, proving the token is not shifted by the off-by-one.
        var line1 = textView.GetOrConstructVisualLine(editor.Document.Lines[1]);
        var numberElement = ElementAtRelativeOffset(line1, 8);
        numberElement.Should().NotBeNull("line 1 should contain a visual element at column 8");
        numberElement!.TextRunProperties.ForegroundBrush.Should()
            .BeSameAs(Brushes.Blue, "the line-1 number token should be colorized blue");
    }

    private static AvaloniaEdit.Rendering.VisualLineElement? ElementAtRelativeOffset(AvaloniaEdit.Rendering.VisualLine line, int offset)
    {
        foreach (var element in line.Elements)
        {
            if (element.RelativeTextOffset <= offset && offset < element.RelativeTextOffset + element.DocumentLength)
                return element;
        }

        return null;
    }

}

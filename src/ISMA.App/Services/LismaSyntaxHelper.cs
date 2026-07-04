using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using ISMA.Domain.Dtos;
using System;
using System.IO;
using System.Xml;

namespace ISMA.App.Services;

/// <summary>
/// Helper for loading and managing LISMA syntax highlighting definitions.
/// Uses the embedded LISMA.xshd file for client-side syntax highlighting.
/// </summary>
public static class LismaSyntaxHelper
{
    private static IHighlightingDefinition? _fallbackHighlighting;
    private static readonly object _lock = new();

    /// <summary>
    /// Creates a server-driven highlighting definition from syntax tokens.
    /// Returns null if no tokens are provided.
    /// </summary>
    public static IHighlightingDefinition? CreateServerDriven(SyntaxTokenDto[] tokens)
    {
        if (tokens == null || tokens.Length == 0)
            return null;

        // Server-driven highlighting deferred: AvaloniaEdit 12.0 API differences
        // prevent direct token-to-highlighting conversion.
        // The XSHD fallback provides solid client-side highlighting.
        return null;
    }

    /// <summary>
    /// Loads the embedded LISMA.xshd as a fallback highlighting definition.
    /// Cached after first load for performance.
    /// </summary>
    public static IHighlightingDefinition? GetFallbackHighlighting()
    {
        if (_fallbackHighlighting != null)
            return _fallbackHighlighting;

        lock (_lock)
        {
            if (_fallbackHighlighting != null)
                return _fallbackHighlighting;

            var assembly = typeof(LismaSyntaxHelper).Assembly;
            using var stream = assembly.GetManifestResourceStream("ISMA.App.Assets.LISMA.xshd");

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                using var xmlReader = XmlReader.Create(reader);
                _fallbackHighlighting = HighlightingLoader.Load(xmlReader, null);
                HighlightingManager.Instance.RegisterHighlighting("LISMA", new[] { ".iscm2", ".scisma", ".im" }, _fallbackHighlighting);
            }
            else
            {
                _fallbackHighlighting = null;
            }

            return _fallbackHighlighting;
        }
    }
}

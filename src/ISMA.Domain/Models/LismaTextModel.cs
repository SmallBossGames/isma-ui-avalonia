using System.Collections.Immutable;

namespace ISMA.Domain.Models;

public sealed class LismaTextModel
{
    public string FullText { get; set; }
    public ImmutableArray<CodeRegion> Regions { get; set; }

    public static readonly CodeRegion DefaultFragment = new("Main", 0, 0);

    public LismaTextModel(string fullText, IEnumerable<CodeRegion> regions)
    {
        FullText = fullText;
        Regions = regions.ToImmutableArray();
    }

    /// <summary>
    /// Resolves the fragment (state) name for a 1-based line number, ported from
    /// the original Kotlin <c>LismaTextModel.fragmentNameByLine</c>.
    /// </summary>
    public string FragmentNameByLine(int line) =>
        Regions.FirstOrDefault(r => line >= r.StartLine && line <= r.EndLine)?.Name
        ?? DefaultFragment.Name;
}

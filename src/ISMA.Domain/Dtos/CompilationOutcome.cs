using System.Collections.Immutable;

namespace ISMA.Domain.Dtos;

/// <summary>
/// Outcome of compiling LISMA source: the compiled model bytes on success,
/// the diagnostics on failure.
/// </summary>
public sealed record CompilationOutcome(bool Succeeded, byte[] CompiledModel, ImmutableArray<CompilationError> Errors)
{
    public static CompilationOutcome Success(byte[] compiledModel) =>
        new(true, compiledModel, ImmutableArray<CompilationError>.Empty);

    public static CompilationOutcome Failed(ImmutableArray<CompilationError> errors) =>
        new(false, Array.Empty<byte>(), errors);
}

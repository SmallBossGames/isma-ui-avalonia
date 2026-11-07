using System.Collections.Immutable;
using ISMA.Domain.Dtos;

namespace ISMA.Domain.Contracts;

/// <summary>
/// Port for a tool that compiles and validates LISMA source.
/// Implementations live in the infrastructure layer (the standalone compiler CLI).
/// </summary>
public interface ILismaCompiler
{
    /// <summary>
    /// Compiles LISMA source. A successful outcome carries the compiled model bytes;
    /// a failed outcome carries the diagnostics.
    /// </summary>
    Task<CompilationOutcome> CompileAsync(string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates LISMA source without producing a compiled model.
    /// Returns the diagnostics (empty when the source is valid).
    /// </summary>
    Task<ImmutableArray<CompilationError>> ValidateAsync(string source, CancellationToken cancellationToken = default);
}

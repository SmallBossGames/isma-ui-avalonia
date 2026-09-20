using System.Collections.Immutable;

namespace ISMA.Domain.Dtos;

public enum SyntaxTokenKind { Unspecified, Keyword, Comment, Number, Text }

public sealed class CompilationError
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = "";
}

public sealed class CompileResult
{
    public string ModelId { get; set; } = "";
    public ImmutableArray<CompilationError> Errors { get; set; } = [];
    public ImmutableArray<string> Warnings { get; set; } = [];
}

public sealed class ValidationResult
{
    public ImmutableArray<CompilationError> Errors { get; set; } = [];
    public ImmutableArray<string> Warnings { get; set; } = [];
}

/// <summary>
/// A semantic syntax token with line/column offsets (LSP model).
/// </summary>
public sealed record SyntaxTokenDto(int Line, int StartChar, int Length, SyntaxTokenKind Kind);

public sealed class CachedSimulationResult
{
    public string File { get; set; } = "";
    public ImmutableArray<string> ColumnNames { get; set; } = ImmutableArray<string>.Empty;
}

public sealed class RunSimulationParams
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double InitialStep { get; set; }
    public string MethodName { get; set; } = "";
    public double Accuracy { get; set; }
    public bool IsAccuracyInUse { get; set; }
    public bool IsStabilityControlInUse { get; set; }
    public string CompiledModelId { get; set; } = "";
    public bool IsEventDetectionInUse { get; set; }
    public double EventDetectionGamma { get; set; }
    public double EventDetectionLowBorder { get; set; }
}

public sealed class SocketPaths
{
    public string Grpc { get; set; } = "";
    public string Http { get; set; } = "";

    public SocketPaths(string grpc, string http)
    {
        Grpc = grpc;
        Http = http;
    }
}

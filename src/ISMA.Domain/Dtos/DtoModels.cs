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

public sealed class SyntaxTokenDto
{
    public int Start { get; set; }
    public int Length { get; set; }
    public SyntaxTokenKind Kind { get; set; }
}

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

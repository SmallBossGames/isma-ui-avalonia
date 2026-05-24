using System.Collections.Immutable;
using Grpc.Net.Client;
using Isma.Contracts.Simulation;
using ISMA.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace ISMA.Infrastructure.Server;

public sealed class GrpcLismaCompilerClient : IDisposable
{
    private readonly LismaCompilerService.LismaCompilerServiceClient _client;
    private readonly ILogger? _logger;
    private readonly IDisposable? _channelDispose;

    public GrpcLismaCompilerClient(GrpcChannel channel, ILogger? logger = null)
    {
        _client = new LismaCompilerService.LismaCompilerServiceClient(channel);
        _logger = logger;
        _channelDispose = channel;
    }

    public GrpcLismaCompilerClient(IUnixSocketHandler socketHandler, string grpcAddress, ILogger? logger = null)
    {
        _client = new LismaCompilerService.LismaCompilerServiceClient(socketHandler.CreateGrpcChannel(grpcAddress, logger));
        _logger = logger;
    }

    public async Task<CompileResult> CompileModelAsync(string source, CancellationToken ct = default)
    {
        var request = new CompileRequest { LismaSourceCode = source };
        _logger?.LogDebug("Compiling model with {Length} chars", source.Length);

        var response = await _client.CompileAsync(request, cancellationToken: ct).ConfigureAwait(false);

        var errors = ImmutableArray.CreateBuilder<ISMA.Domain.Dtos.CompilationError>();
        foreach (var e in response.Errors)
        {
            errors.Add(new ISMA.Domain.Dtos.CompilationError { Row = e.Row, Column = e.Column, Message = e.Message });
        }

        var warnings = ImmutableArray.CreateBuilder<string>();
        foreach (var w in response.Warnings)
        {
            warnings.Add(w);
        }

        return new CompileResult
        {
            ModelId = response.CompiledModelId,
            Errors = errors.ToImmutable(),
            Warnings = warnings.ToImmutable(),
        };
    }

    public CompileResult CompileModel(string source, CancellationToken ct = default)
    {
        return CompileModelAsync(source, ct).GetAwaiter().GetResult();
    }

    public async Task<ValidationResult> ValidateModelAsync(string source, CancellationToken ct = default)
    {
        var request = new ValidateRequest { LismaSourceCode = source };

        var response = await _client.ValidateAsync(request, cancellationToken: ct).ConfigureAwait(false);

        var errors = ImmutableArray.CreateBuilder<ISMA.Domain.Dtos.CompilationError>();
        foreach (var e in response.Errors)
        {
            errors.Add(new ISMA.Domain.Dtos.CompilationError { Row = e.Row, Column = e.Column, Message = e.Message });
        }

        var warnings = ImmutableArray.CreateBuilder<string>();
        foreach (var w in response.Warnings)
        {
            warnings.Add(w);
        }

        return new ValidationResult
        {
            Errors = errors.ToImmutable(),
            Warnings = warnings.ToImmutable(),
        };
    }

    public ValidationResult ValidateModel(string source, CancellationToken ct = default)
    {
        return ValidateModelAsync(source, ct).GetAwaiter().GetResult();
    }

    public async Task<SyntaxTokenDto[]> HighlightSourceAsync(string source, CancellationToken ct = default)
    {
        var request = new HighlightRequest { SourceCode = source };

        var response = await _client.HighlightAsync(request, cancellationToken: ct).ConfigureAwait(false);

        return response.Tokens.Select(t => new SyntaxTokenDto
        {
            Start = t.Start,
            Length = t.Length,
            Kind = MapTokenKind(t.Kind),
        }).ToArray();
    }

    public SyntaxTokenDto[] HighlightSource(string source, CancellationToken ct = default)
    {
        return HighlightSourceAsync(source, ct).GetAwaiter().GetResult();
    }

    public async Task DeleteCompiledModelAsync(string modelId, CancellationToken ct = default)
    {
        var request = new DeleteCompiledModelRequest { CompiledModelId = modelId };
        var response = await _client.DeleteAsync(request, cancellationToken: ct).ConfigureAwait(false);

        if (!response.Success)
        {
            throw new InvalidOperationException($"Failed to delete compiled model: {modelId}");
        }
    }

    public void DeleteCompiledModel(string modelId, CancellationToken ct = default)
    {
        DeleteCompiledModelAsync(modelId, ct).GetAwaiter().GetResult();
    }

    private static SyntaxTokenKind MapTokenKind(TokenKind kind)
    {
        return kind switch
        {
            TokenKind.Unspecified => SyntaxTokenKind.Unspecified,
            TokenKind.Keyword => SyntaxTokenKind.Keyword,
            TokenKind.Comment => SyntaxTokenKind.Comment,
            TokenKind.Number => SyntaxTokenKind.Number,
            TokenKind.Text => SyntaxTokenKind.Text,
            _ => SyntaxTokenKind.Unspecified,
        };
    }

    public void Dispose()
    {
        _channelDispose?.Dispose();
    }
}

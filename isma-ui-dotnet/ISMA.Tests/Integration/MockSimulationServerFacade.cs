using System;
using System.Collections.Immutable;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration;

public class MockSimulationServerFacade : ISimulationServerFacade
{
    public bool CompileCalled { get; private set; }
    public bool ValidateCalled { get; private set; }
    public bool HighlightCalled { get; private set; }
    public bool RunCalled { get; private set; }
    public bool CancelCalled { get; private set; }
    public bool ShutdownCalled { get; private set; }
    public string? LastCompileSource { get; private set; }
    public string? LastValidateSource { get; private set; }
    public string? LastHighlightSource { get; private set; }
    public RunSimulationParams? LastRunParams { get; private set; }

    public Func<string, Task<CompileResult>> CompileHandler { get; set; } = _ => Task.FromResult(new CompileResult { Errors = ImmutableArray<CompilationError>.Empty });
    public Func<string, Task<ValidationResult>> ValidateHandler { get; set; } = _ => Task.FromResult(new ValidationResult { Errors = ImmutableArray<CompilationError>.Empty });
    public Func<string, Task<SyntaxTokenDto[]>> HighlightHandler { get; set; } = _ => Task.FromResult(Array.Empty<SyntaxTokenDto>());
    public Func<RunSimulationParams, Task<long>> RunHandler { get; set; } = _ => Task.FromResult(1L);
    public Func<long, IAsyncEnumerable<SimulationProgress>> MonitorHandler { get; set; } = _ => AsyncEnumerable.Empty<SimulationProgress>();
    public Func<long, Task<CachedSimulationResult>> DownloadHandler { get; set; } = _ => Task.FromResult(new CachedSimulationResult { File = "/tmp/test.bin" });
    public Func<long, Task> CancelHandler { get; set; } = _ => Task.CompletedTask;
    public Func<Task<string[]>> GetMethodsHandler { get; set; } = () => Task.FromResult(Array.Empty<string>());

    public async Task<CompileResult> CompileModel(string source)
    {
        CompileCalled = true;
        LastCompileSource = source;
        return await CompileHandler(source);
    }

    public async Task<ValidationResult> ValidateModel(string source)
    {
        ValidateCalled = true;
        LastValidateSource = source;
        return await ValidateHandler(source);
    }

    public async Task<SyntaxTokenDto[]> HighlightSource(string source)
    {
        HighlightCalled = true;
        LastHighlightSource = source;
        return await HighlightHandler(source);
    }

    public async Task<long> RunSimulation(RunSimulationParams @params)
    {
        RunCalled = true;
        LastRunParams = @params;
        return await RunHandler(@params);
    }

    public IAsyncEnumerable<SimulationProgress> MonitorSimulation(long id)
    {
        return MonitorHandler(id);
    }

    public async Task<CachedSimulationResult> DownloadResult(long id)
    {
        return await DownloadHandler(id);
    }

    public async Task CancelSimulation(long id)
    {
        CancelCalled = true;
        await CancelHandler(id);
    }

    public async Task<string[]> GetSimulationMethods()
    {
        return await GetMethodsHandler();
    }

    public async Task Shutdown()
    {
        ShutdownCalled = true;
    }
}

public static class AsyncEnumerable
{
    public static IAsyncEnumerable<T> Empty<T>()
    {
        return new EmptyAsyncEnumerable<T>();
    }

    private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
        {
            return new EmptyAsyncEnumerator<T>();
        }
    }

    private sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        public T Current => default!;
        public ValueTask<bool> MoveNextAsync() => new(false);
        public ValueTask DisposeAsync() => default;
    }
}

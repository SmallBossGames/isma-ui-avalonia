using System.Collections;
using System.Collections.Immutable;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.Domain.Contracts;

public interface ISimulationServerFacade
{
    Task<CompileResult> CompileModel(string source);
    Task<ValidationResult> ValidateModel(string source);
    Task<SyntaxTokenDto[]> HighlightSource(string source);
    Task<long> RunSimulation(RunSimulationParams @params);
    IAsyncEnumerable<SimulationProgress> MonitorSimulation(long id);
    Task<CachedSimulationResult> DownloadResult(long id);
    Task CancelSimulation(long id);
    Task<string[]> GetSimulationMethods();
    Task Shutdown();
}

public interface IEquationIndexProvider
{
    int GetDifferentialEquationCount();
    int GetAlgebraicEquationCount();
    string GetDifferentialEquationCode(int index);
    string GetAlgebraicEquationCode(int index);
}

public interface ISimulationResultReader
{
    IEnumerable<SimulationPoint> Results { get; }
    static abstract SimulationMetadata ReadMetadata(string filePath);
}

public interface ISyntaxHighlighter
{
    Task<SyntaxTokenDto[]> Highlight(string source);
}

public interface ITextEditorFactory
{
    object CreateTextEditor(string text, Action<string>? onTextChanged);
    void DisposeInstance(object editor);
}

public interface ISimulationResultService
{
    IEnumerable<CompletedSimulation> TrackingTasksResults { get; }
    void CommitResult(CompletedSimulation simulation);
    void RemoveResult(CompletedSimulation simulation);
    Task ShowChart(CompletedSimulation simulation);
    Task ExportToFile(CompletedSimulation simulation, string filePath);
}

public interface IProjectFileService
{
    Task<IList<string>> Open(object? ownerWindow);
    Task<IList<ProjectType>> Open(IList<string> paths);
    Task<bool> Save(object project);
    Task<bool> SaveAs(object project);
    Task<bool> SaveAll(IList<object> projects);
}

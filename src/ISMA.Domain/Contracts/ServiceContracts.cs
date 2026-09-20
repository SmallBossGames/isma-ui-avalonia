using System.Collections;
using System.Collections.Immutable;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.Domain.Contracts;

public interface ISimulationServerFacade
{
    Task<CompileResult> CompileModel(string source);
    Task<ValidationResult> ValidateModel(string source);
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
    /// <summary>
    /// Requests semantic tokens for the full document. The document is
    /// identified by <paramref name="documentId"/>; the first call opens it
    /// on the provider, subsequent calls update it.
    /// </summary>
    Task<SyntaxTokenDto[]> Highlight(string documentId, string source);

    /// <summary>Releases provider state for the document.</summary>
    Task CloseDocument(string documentId);
}

public interface ITextEditorFactory
{
    object CreateTextEditor(string text, Action<string>? onTextChanged, string? highlightingDefinitionName = null);
    void SetSyntaxHighlighting(object editor, SyntaxTokenDto[] tokens, string source);
    void AddSearchPanel(object editor);
    void AddLineNumberMargin(object editor);
    void DisposeInstance(object editor);
}

public interface ISimulationResultService
{
    IEnumerable<CompletedSimulation> TrackingTasksResults { get; }
    void CommitResult(CompletedSimulation simulation);
    void RemoveResult(CompletedSimulation simulation);
    Task ShowChart(CompletedSimulation simulation);
    Task ExportToFile(CompletedSimulation simulation, string filePath);
    Task ShowExportDialog(CompletedSimulation simulation);
}

public interface IProjectFileService
{
    Task<IList<string>> Open(object? ownerWindow);
    Task<bool> Save(object project);
    Task<bool> SaveAs(object project);
    Task<bool> SaveAll(IList<object> projects);
}

public interface IPreferencesProvider
{
    Domain.Models.Preferences Load();
    void Save(Domain.Models.Preferences preferences);
    void CommitWindow(Domain.Models.WindowPreferences windowPreferences);
    void CommitFiles(Domain.Models.DefaultFilesPreferences defaultFilesPreferences);
}

global using global::Xunit;
using System.Collections.Immutable;
using FluentAssertions;
using ISMA.Domain.Contracts;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;
using ISMA.ViewModels.ViewModels;
using Moq;

namespace ISMA.Tests.ViewModels;

public class SimulationServiceViewModelTests
{
    private static LismaProjectViewModel CreateProject(string source, string name)
    {
        var facade = new Mock<ISimulationServerFacade>();
        var editorFactory = new Mock<ITextEditorFactory>();
        var fileService = new Mock<IProjectFileService>();
        var syntax = new Mock<ISyntaxHighlighter>();
        syntax.Setup(m => m.Highlight(It.IsAny<string>())).ReturnsAsync(Array.Empty<SyntaxTokenDto>());
        var project = new LismaProjectViewModel(
            facade.Object,
            editorFactory.Object,
            fileService.Object,
            syntax.Object,
            new LismaTextModel(source, Array.Empty<CodeRegion>()),
            null);
        project.Name = name;
        return project;
    }

    [Fact]
    public async Task CompileModel_ReturnsErrors_SetsCompilationFailedStatus()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        var compileErrors = ImmutableArray.Create(
            new CompilationError { Row = 1, Column = 5, Message = "Syntax error" });
        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { Errors = compileErrors });

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        var project = CreateProject("some source", "TestProject");

        await viewModel.SimulateAsync(project);

        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusText.Should().Be("Compilation failed");
        mockErrorService.Verify(e => e.PutErrorList(It.IsAny<IEnumerable<ErrorInfo>>()), Times.Once);
    }

    [Fact]
    public async Task CompileSuccess_CreatesCompletedResult()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { ModelId = "model123" });

        mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
            .ReturnsAsync(42L);

        mockFacade.Setup(f => f.MonitorSimulation(42L))
            .Returns(ToAsyncEnumerable(new List<SimulationProgress>
            {
                new() { StartTime = 0, EndTime = 10, CurrentTime = 10 }
            }));

        mockFacade.Setup(f => f.DownloadResult(42L))
            .ReturnsAsync(new CachedSimulationResult
            {
                File = "/tmp/result.bin",
                ColumnNames = ImmutableArray.Create("time", "y")
            });

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        var project = CreateProject("source", "TestProject");

        await viewModel.SimulateAsync(project);

        viewModel.IsRunning.Should().BeFalse();
        viewModel.StatusText.Should().Be("Simulation complete");
        mockResultService.Verify(r => r.CommitResult(It.IsAny<CompletedSimulation>()), Times.Once);
    }

    [Fact]
    public async Task Simulation_RunsProgressUpdates()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { ModelId = "model123" });

        mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
            .ReturnsAsync(1L);

        mockFacade.Setup(f => f.MonitorSimulation(1L))
            .Returns(ToAsyncEnumerable(new List<SimulationProgress>
            {
                new() { StartTime = 0, EndTime = 10, CurrentTime = 2 },
                new() { StartTime = 0, EndTime = 10, CurrentTime = 5 },
                new() { StartTime = 0, EndTime = 10, CurrentTime = 10 }
            }));

        mockFacade.Setup(f => f.DownloadResult(1L))
            .ReturnsAsync(new CachedSimulationResult { File = "/tmp/result.bin" });

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        var project = CreateProject("source", "TestProject");

        await viewModel.SimulateAsync(project);

        viewModel.TrackingTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task Simulation_Cancelled_StopsSimulation()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { ModelId = "model123" });

        mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
            .ReturnsAsync(1L);

        mockFacade.Setup(f => f.MonitorSimulation(1L))
            .Returns(ToAsyncEnumerable(new List<SimulationProgress>
            {
                new() { StartTime = 0, EndTime = 10, CurrentTime = 2 }
            }));

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        var inProgress = new InProgressSimulationViewModel { Id = 1, CanAbort = true };
        viewModel.TrackingTasks.Add(inProgress);

        await viewModel.StopSimulationAsync(inProgress);

        viewModel.TrackingTasks.Should().BeEmpty();
        viewModel.StatusText.Should().Be("Simulation stopped");
        mockFacade.Verify(f => f.CancelSimulation(1), Times.Once);
    }

    [Fact]
    public async Task StopSimulation_NullSimulation_DoesNotThrow()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        await viewModel.StopSimulationAsync(null!);

        viewModel.StatusText.Should().Be("Ready");
    }

    [Fact]
    public void ClearTrackingTasks_RemovesAllTasks()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        viewModel.TrackingTasks.Add(new InProgressSimulationViewModel { Id = 1 });
        viewModel.TrackingTasks.Add(new InProgressSimulationViewModel { Id = 2 });
        viewModel.TrackingTasks.Should().HaveCount(2);

        viewModel.ClearTrackingTasks();

        viewModel.TrackingTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task MonitorError_TaskMovesToFailedList()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();

        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { ModelId = "model123" });
        mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
            .ReturnsAsync(7L);
        mockFacade.Setup(f => f.MonitorSimulation(7L))
            .Returns(ThrowingAsyncEnumerable<SimulationProgress>("boom"));

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService,
            tasksPopOver);

        var project = CreateProject("source", "TestProject");

        await viewModel.SimulateAsync(project);

        viewModel.TrackingTasks.Should().BeEmpty();
        tasksPopOver.InProgress.Should().BeEmpty();
        tasksPopOver.Failed.Should().HaveCount(1);
        tasksPopOver.Failed[0].ErrorText.Should().Be("Monitor error: boom");
        tasksPopOver.Failed[0].TaskId.Should().Be(1);
    }

    [Fact]
    public async Task DownloadError_TaskMovesToFailedList()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();

        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { ModelId = "model123" });
        mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
            .ReturnsAsync(8L);
        mockFacade.Setup(f => f.MonitorSimulation(8L))
            .Returns(ToAsyncEnumerable(new List<SimulationProgress>
            {
                new() { StartTime = 0, EndTime = 10, CurrentTime = 10 }
            }));
        mockFacade.Setup(f => f.DownloadResult(8L))
            .ThrowsAsync(new Exception("no result"));

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService,
            tasksPopOver);

        await viewModel.SimulateAsync(CreateProject("source", "TestProject"));

        tasksPopOver.Failed.Should().HaveCount(1);
        tasksPopOver.Failed[0].ErrorText.Should().Be("Download error: no result");
    }

    [Fact]
    public async Task Progress_IsClampedToZeroOne()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();
        var tasksPopOver = new TasksPopOverViewModel();

        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { ModelId = "model123" });
        mockFacade.Setup(f => f.RunSimulation(It.IsAny<RunSimulationParams>()))
            .ReturnsAsync(9L);
        mockFacade.Setup(f => f.MonitorSimulation(9L))
            .Returns(ToAsyncEnumerable(new List<SimulationProgress>
            {
                new() { StartTime = 0, EndTime = 10, CurrentTime = -5 },
                new() { StartTime = 0, EndTime = 10, CurrentTime = 25 }
            }));
        mockFacade.Setup(f => f.DownloadResult(9L))
            .ReturnsAsync(new CachedSimulationResult { File = "/tmp/result.bin" });

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService,
            tasksPopOver);

        await viewModel.SimulateAsync(CreateProject("source", "TestProject"));

        tasksPopOver.Completed.Should().HaveCount(1);
        var completed = tasksPopOver.Completed[0];
        completed.TaskId.Should().Be(1);
        completed.Parameters.Should().NotBeNull();
    }

    [Fact]
    public async Task CompileErrors_MapFragmentNameByLine()
    {
        var mockFacade = new Mock<ISimulationServerFacade>();
        var mockErrorService = new Mock<IModelErrorService>();
        var mockResultService = new Mock<ISimulationResultService>();
        var paramsService = new SimulationParametersViewModel();

        var compileErrors = ImmutableArray.Create(
            new CompilationError { Row = 3, Column = 1, Message = "err in A" },
            new CompilationError { Row = 1, Column = 1, Message = "err in main" });
        mockFacade.Setup(f => f.CompileModel(It.IsAny<string>()))
            .ReturnsAsync(new CompileResult { Errors = compileErrors });

        IEnumerable<ErrorInfo>? capturedErrors = null;
        mockErrorService.Setup(e => e.PutErrorList(It.IsAny<IEnumerable<ErrorInfo>>()))
            .Callback<IEnumerable<ErrorInfo>>(list => capturedErrors = list);

        var viewModel = new SimulationServiceViewModel(
            mockFacade.Object,
            mockErrorService.Object,
            mockResultService.Object,
            paramsService);

        var lismaModel = new LismaTextModel(
            "main\nstate A (1 > 0) {\na body\n} from Main;\n",
            new[] { new CodeRegion("A", 2, 4) });
        var project = new LismaProjectViewModel(
            mockFacade.Object,
            new Mock<ITextEditorFactory>().Object,
            new Mock<IProjectFileService>().Object,
            new Mock<ISyntaxHighlighter>().Object,
            lismaModel,
            null,
            mockErrorService.Object);

        await viewModel.SimulateAsync(project);

        mockErrorService.Verify(
            e => e.PutErrorList(It.IsAny<IEnumerable<ErrorInfo>>()),
            Times.Once);
        capturedErrors.Should().NotBeNull();
        var errors = capturedErrors!.ToList();
        errors[0].FragmentName.Should().Be("A");
        errors[1].FragmentName.Should().Be("Main");
    }

    private static IAsyncEnumerable<T> ThrowingAsyncEnumerable<T>(string message) =>
        new ThrowingAsyncEnumerableImpl<T>(message);

    private sealed class ThrowingAsyncEnumerableImpl<T> : IAsyncEnumerable<T>
    {
        private readonly string _message;
        public ThrowingAsyncEnumerableImpl(string message) => _message = message;
        public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken _ = default) => new ThrowingEnumeratorImpl<T>(_message);
    }

    private sealed class ThrowingEnumeratorImpl<T> : IAsyncEnumerator<T>
    {
        private readonly string _message;
        public ThrowingEnumeratorImpl(string message) => _message = message;
        public T Current => default!;
        public ValueTask<bool> MoveNextAsync() => throw new Exception(_message);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        return new AsyncEnumerableImpl<T>(() => items.GetEnumerator());
    }

    private sealed class AsyncEnumerableImpl<T> : IAsyncEnumerable<T>
    {
        private readonly Func<IEnumerator<T>> _getEnumerator;
        public AsyncEnumerableImpl(Func<IEnumerator<T>> getEnumerator) => _getEnumerator = getEnumerator;
        public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken _ = default) => new AsyncEnumeratorImpl<T>(_getEnumerator());
    }

    private sealed class AsyncEnumeratorImpl<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public AsyncEnumeratorImpl(IEnumerator<T> inner) => _inner = inner;
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    }
}

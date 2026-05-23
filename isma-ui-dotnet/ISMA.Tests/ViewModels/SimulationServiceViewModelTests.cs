using System.Collections.Immutable;
using Xunit;
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
        var project = new LismaProjectViewModel(
            facade.Object,
            editorFactory.Object,
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
        var paramsService = new SimulationParametersService();

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
        var paramsService = new SimulationParametersService();

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
        var paramsService = new SimulationParametersService();

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
        var paramsService = new SimulationParametersService();

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
        var paramsService = new SimulationParametersService();

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
        var paramsService = new SimulationParametersService();

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

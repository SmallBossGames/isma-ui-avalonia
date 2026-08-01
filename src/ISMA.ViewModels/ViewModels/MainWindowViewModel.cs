using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ProjectService _projectService;

    public ProjectService ProjectService => _projectService;

    private readonly SimulationServiceViewModel _simulationService;
    private readonly ErrorListViewModel _errorList;
    private readonly SimulationParametersViewModel _simulationParameters;
    private readonly TasksPopOverViewModel _tasksPopOver;
    private readonly ISimulationParametersStoreService _parametersStore;
    private readonly IModelErrorService _modelErrorService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly Action<SimulationParameters>? _loadSettingsCallback;

    private ObservableCollection<IProjectViewModel> _projects = new();
    private IProjectViewModel? _activeProject;
    private bool _showSettings;

    public ObservableCollection<IProjectViewModel> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    public IProjectViewModel? ActiveProject
    {
        get => _activeProject;
        set => SetProperty(ref _activeProject, value);
    }

    public bool ShowSettings
    {
        get => _showSettings;
        set => SetProperty(ref _showSettings, value);
    }

    public SimulationServiceViewModel SimulationService => _simulationService;
    public ErrorListViewModel ErrorList => _errorList;
    public SimulationParametersViewModel SimulationParameters => _simulationParameters;
    public TasksPopOverViewModel TasksPopOver => _tasksPopOver;

    public MainWindowViewModel(
        ProjectService projectService,
        SimulationServiceViewModel simulationService,
        ErrorListViewModel errorList,
        SimulationParametersViewModel simulationParameters,
        TasksPopOverViewModel tasksPopOver,
        ISimulationParametersStoreService parametersStore,
        IModelErrorService modelErrorService,
        ISyntaxHighlighter syntaxHighlighter,
        Action<SimulationParameters>? loadSettingsCallback = null)
    {
        _projectService = projectService;
        _simulationService = simulationService;
        _errorList = errorList;
        _simulationParameters = simulationParameters;
        _tasksPopOver = tasksPopOver;
        _parametersStore = parametersStore;
        _modelErrorService = modelErrorService;
        _syntaxHighlighter = syntaxHighlighter;
        _loadSettingsCallback = loadSettingsCallback;

        LoadProjects();
        RestoreLastOpenedFiles();
    }

    private readonly List<(LismaProjectViewModel project, BlueprintStateViewModel state)> _stateTextEditorTabs = new();
    private readonly List<(LismaProjectViewModel project, BlueprintLoopTransactionViewModel loop)> _loopTextEditorTabs = new();

    public void OpenStateTextEditorTab(BlueprintStateViewModel state, string title)
    {
        var project = new LismaProjectViewModel(
            _simulationService.SimulationServerFacade,
            _projectService.TextEditorFactory,
            _projectService.ProjectFileService,
            _syntaxHighlighter,
            _modelErrorService);

        project.Name = title;
        project.FullText = state.Text;
        var editorVm = FindEditorViewModelForState(state);
        project.OnBeforeDispose = () =>
        {
            state.Text = project.FullText;
            editorVm?.UpdateStateText(state, project.FullText);
            _stateTextEditorTabs.RemoveAll(t => t.project == project);
        };

        _stateTextEditorTabs.Add((project, state));
        _projectService.AddProject(project);
        _projectService.SetActiveProject(project);
        SyncProjects();
    }

    private BlueprintEditorViewModel? FindEditorViewModelForState(BlueprintStateViewModel state)
    {
        foreach (var project in Projects)
        {
            if (project is BlueprintProjectViewModel bpProject)
            {
                var editorVm = bpProject.EditorContent as BlueprintEditorViewModel;
                if (editorVm?.States.Contains(state) == true)
                {
                    return editorVm;
                }
            }
        }
        return null;
    }

    public void OpenLoopTextEditorTab(BlueprintLoopTransactionViewModel loop, string title)
    {
        var project = new LismaProjectViewModel(
            _simulationService.SimulationServerFacade,
            _projectService.TextEditorFactory,
            _projectService.ProjectFileService,
            _syntaxHighlighter,
            _modelErrorService);

        project.Name = title;
        project.FullText = loop.Text;
        var capturedLoop = loop;
        project.OnBeforeDispose = () =>
        {
            capturedLoop.Text = project.FullText;
            _loopTextEditorTabs.RemoveAll(t => t.project == project);
        };

        _loopTextEditorTabs.Add((project, loop));
        _projectService.AddProject(project);
        ActiveProject = project;
        SyncProjects();
    }

    public void SyncProjects()
    {
        Projects.Clear();
        foreach (var project in _projectService.Projects)
        {
            Projects.Add(project);
        }
        ActiveProject = _projectService.ActiveProject;
    }

    private void LoadProjects()
    {
        SyncProjects();
    }

    private async void RestoreLastOpenedFiles()
    {
        var lastFiles = _projectService.GetLastOpenedFiles();
        foreach (var filePath in lastFiles)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    await _projectService.OpenAsync(filePath);
                }
            }
            catch
            {
                // Skip files that can't be opened
            }
        }

        LoadProjects();

        if (Projects.Count > 0 && ActiveProject == null)
        {
            ActiveProject = Projects[0];
        }
    }

    [RelayCommand]
    private async Task NewText()
    {
        var project = await _projectService.CreateNewAsync();
        if (project != null)
        {
            LoadProjects();
        }
    }

    [RelayCommand]
    private async Task NewBlueprint()
    {
        var project = await _projectService.CreateNewBlueprintAsync();
        if (project != null)
        {
            LoadProjects();
        }
    }

    [RelayCommand]
    private async Task Open()
    {
        var project = await _projectService.OpenAsync();
        if (project != null)
        {
            LoadProjects();
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        await _projectService.SaveAsync();
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        if (ActiveProject != null)
        {
            await _projectService.SaveAsAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAll()
    {
        await _projectService.SaveAllAsync();
    }

    [RelayCommand]
    private async Task Close()
    {
        await _projectService.CloseAsync();
        LoadProjects();
    }

    [RelayCommand]
    private async Task CloseTab(IProjectViewModel tab)
    {
        if (tab == null) return;

        await _projectService.CloseAsync(tab);
        LoadProjects();
    }

    [RelayCommand]
    private async Task CloseAll()
    {
        await _projectService.CloseAllAsync();
        LoadProjects();
    }

    [RelayCommand]
    private void Exit()
    {
        _projectService.CloseAllAsync().Wait();
    }

    [RelayCommand]
    private void Cut()
    {
        ActiveProject?.TriggerCut();
    }

    [RelayCommand]
    private void Copy()
    {
        ActiveProject?.TriggerCopy();
    }

    [RelayCommand]
    private void Paste()
    {
        ActiveProject?.TriggerPaste();
    }

    [RelayCommand]
    private void SelectAll()
    {
        ActiveProject?.TriggerSelectAll();
    }

    [RelayCommand]
    private async Task Verify()
    {
        if (ActiveProject is LismaProjectViewModel lismaProject)
        {
            await lismaProject.ValidateAsync();
        }
        else if (ActiveProject is BlueprintProjectViewModel blueprintProject)
        {
            var lismaText = blueprintProject.ConvertToLisma();
            var tempProject = new LismaProjectViewModel(
                _simulationService.SimulationServerFacade,
                _projectService.TextEditorFactory,
                _projectService.ProjectFileService,
                _syntaxHighlighter,
                _modelErrorService);
            tempProject.SetContent(lismaText.FullText);
            await tempProject.ValidateAsync();
        }
    }

    [RelayCommand]
    private async Task Run()
    {
        if (ActiveProject is LismaProjectViewModel lismaProject)
        {
            await _simulationService.SimulateAsync(lismaProject);
        }
        else if (ActiveProject is BlueprintProjectViewModel blueprintProject)
        {
            var lismaText = blueprintProject.ConvertToLisma();
            var tempProject = new LismaProjectViewModel(
                _simulationService.SimulationServerFacade,
                _projectService.TextEditorFactory,
                _projectService.ProjectFileService,
                _syntaxHighlighter,
                _modelErrorService);
            tempProject.SetContent(lismaText.FullText);
            await _simulationService.SimulateAsync(tempProject);
        }
    }

    [RelayCommand]
    private async Task StoreSettings()
    {
        await _parametersStore.StoreAsync();
    }

    [RelayCommand]
    private async Task LoadSettings()
    {
        if (_loadSettingsCallback != null)
        {
            await _parametersStore.LoadAsync();
        }
    }
}

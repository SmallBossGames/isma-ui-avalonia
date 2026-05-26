using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Models;
using ISMA.ViewModels.Services;

namespace ISMA.ViewModels.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ProjectService _projectService;
    private readonly SimulationServiceViewModel _simulationService;
    private readonly ErrorListViewModel _errorList;
    private readonly SimulationParametersViewModel _simulationParameters;
    private readonly TasksPopOverViewModel _tasksPopOver;
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
        Action<SimulationParameters>? loadSettingsCallback = null)
    {
        _projectService = projectService;
        _simulationService = simulationService;
        _errorList = errorList;
        _simulationParameters = simulationParameters;
        _tasksPopOver = tasksPopOver;
        _loadSettingsCallback = loadSettingsCallback;

        LoadProjects();
        RestoreLastOpenedFiles();
    }

    private void LoadProjects()
    {
        Projects.Clear();
        foreach (var project in _projectService.Projects)
        {
            Projects.Add(project);
        }
        ActiveProject = _projectService.ActiveProject;
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
                    var project = await _projectService.OpenAsync(filePath);
                    if (project != null)
                    {
                        Projects.Add(project);
                    }
                }
            }
            catch
            {
                // Skip files that can't be opened
            }
        }

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
            await _projectService.SaveAsync();
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
    private async Task Verify()
    {
        if (ActiveProject is LismaProjectViewModel lismaProject)
        {
            await lismaProject.ValidateAsync();
        }
    }

    [RelayCommand]
    private async Task Run()
    {
        if (ActiveProject is LismaProjectViewModel lismaProject)
        {
            await _simulationService.SimulateAsync(lismaProject);
        }
    }

    [RelayCommand]
    private void StoreSettings()
    {
        _simulationParameters.Snapshot();
    }

    [RelayCommand]
    private void LoadSettings()
    {
        if (_loadSettingsCallback != null)
        {
            _simulationParameters.Snapshot();
        }
    }
}

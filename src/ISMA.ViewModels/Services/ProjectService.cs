using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Services;

public sealed class ProjectService
{
    private readonly IProjectFileService _projectFileService;
    private readonly ISimulationServerFacade _serverFacade;
    private readonly ITextEditorFactory _editorFactory;
    private readonly SimulationParametersService _parametersService;
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly IModelErrorService? _errorService;
    private readonly IPreferencesProvider? _preferencesProvider;

    private readonly List<IProjectViewModel> _projects = new();
    private IProjectViewModel? _activeProject;

    public IReadOnlyList<IProjectViewModel> Projects => _projects.AsReadOnly();

    public IProjectViewModel? ActiveProject
    {
        get => _activeProject;
        private set
        {
            if (ReferenceEquals(_activeProject, value))
                return;
            _activeProject = value;
        }
    }

    public ProjectService(
        IProjectFileService projectFileService,
        ISimulationServerFacade serverFacade,
        ITextEditorFactory editorFactory,
        SimulationParametersService parametersService,
        ISyntaxHighlighter syntaxHighlighter,
        IModelErrorService? errorService = null,
        IPreferencesProvider? preferencesProvider = null)
    {
        _projectFileService = projectFileService;
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _parametersService = parametersService;
        _syntaxHighlighter = syntaxHighlighter;
        _errorService = errorService;
        _preferencesProvider = preferencesProvider;
    }

    public async Task<IProjectViewModel?> CreateNewAsync()
    {
        var project = new LismaProjectViewModel(_serverFacade, _editorFactory, _projectFileService, _syntaxHighlighter, _errorService);
        _projects.Add(project);
        ActiveProject = project;
        return project;
    }

    public async Task<IProjectViewModel?> CreateNewBlueprintAsync()
    {
        var project = new BlueprintProjectViewModel(_projectFileService, _editorFactory);
        var editorVm = new BlueprintEditorViewModel();
        project.SetEditorViewModel(editorVm);
        _projects.Add(project);
        ActiveProject = project;
        return project;
    }

    public async Task<IProjectViewModel?> OpenAsync()
    {
        var paths = await _projectFileService.Open((object?)null);
        if (paths == null || paths.Count == 0)
            return null;

        var types = await _projectFileService.Open((IList<string>)paths);
        var filePath = paths[0];
        IProjectViewModel project = CreateProject(filePath, types[0]);

        _projects.Add(project);
        ActiveProject = project;
        TrackOpenedFile(filePath);
        return project;
    }

    public async Task<IProjectViewModel?> OpenAsync(string filePath)
    {
        var project = CreateProject(filePath, ProjectType.LismaText);
        _projects.Add(project);
        ActiveProject = project;
        TrackOpenedFile(filePath);
        return project;
    }

    public IProjectViewModel CreateNewTextProject(string name)
    {
        var project = new LismaProjectViewModel(_serverFacade, _editorFactory, _projectFileService, _syntaxHighlighter, _errorService);
        project.Name = name;
        _projects.Add(project);
        return project;
    }

    public async Task<bool> SaveAsync()
    {
        if (ActiveProject == null)
            return false;

        return ActiveProject switch
        {
            LismaProjectViewModel lisma => await lisma.SaveAsync(),
            BlueprintProjectViewModel blueprint => await blueprint.SaveAsync(),
            _ => false
        };
    }

    public async Task<bool> SaveAsAsync()
    {
        if (ActiveProject == null)
            return false;

        return ActiveProject switch
        {
            LismaProjectViewModel lisma => await lisma.SaveAsAsync(),
            BlueprintProjectViewModel blueprint => await blueprint.SaveAsAsync(),
            _ => false
        };
    }

    public async Task<bool> SaveAllAsync()
    {
        var results = new List<bool>();
        foreach (var project in _projects)
        {
            var result = project switch
            {
                LismaProjectViewModel lisma => await lisma.SaveAsync(),
                BlueprintProjectViewModel blueprint => await blueprint.SaveAsync(),
                _ => false
            };
            results.Add(result);
        }
        return results.All(r => r);
    }

    public async Task<bool> CloseAsync()
    {
        if (ActiveProject == null)
            return false;

        var project = ActiveProject;
        _projects.Remove(project);
        ActiveProject = _projects.Count > 0 ? _projects[_projects.Count - 1] : null;
        project.Dispose();
        return true;
    }

    public async Task<bool> CloseAsync(IProjectViewModel project)
    {
        if (project == null)
            return false;

        _projects.Remove(project);
        if (ActiveProject == project)
        {
            ActiveProject = _projects.Count > 0 ? _projects[_projects.Count - 1] : null;
        }
        project.Dispose();
        return true;
    }

    public async Task CloseAllAsync()
    {
        foreach (var project in _projects)
        {
            project.Dispose();
        }
        _projects.Clear();
        ActiveProject = null;
    }

    private LismaProjectViewModel CreateLismaProject(string path)
    {
        var project = new LismaProjectViewModel(
            _serverFacade,
            _editorFactory,
            _projectFileService,
            _syntaxHighlighter,
            new ISMA.Domain.Models.LismaTextModel("", Array.Empty<ISMA.Domain.Models.CodeRegion>()),
            path,
            _errorService);
        return project;
    }

    private BlueprintProjectViewModel CreateBlueprintProject(string path)
    {
        var project = new BlueprintProjectViewModel(_projectFileService, _editorFactory);
        project.LoadFromFile(path);
        var editorVm = new BlueprintEditorViewModel();
        project.SetEditorViewModel(editorVm);
        return project;
    }

    private IProjectViewModel CreateProject(string path, ProjectType type)
    {
        return type switch
        {
            ProjectType.Blueprint => CreateBlueprintProject(path),
            ProjectType.Legacy or _ => CreateLismaProject(path)
        };
    }

    private void SetProperty(ref IProjectViewModel? field, IProjectViewModel? value)
    {
        if (field == value)
            return;

        field = value;
    }

    private void TrackOpenedFile(string filePath)
    {
        if (_preferencesProvider == null) return;

        var preferences = _preferencesProvider.Load();
        var existing = preferences.DefaultFilesPreferences.LastOpenedProjectPath
            .Where(p => p != filePath)
            .Take(4)
            .Prepend(filePath)
            .ToArray();

        _preferencesProvider.CommitFiles(new DefaultFilesPreferences { LastOpenedProjectPath = existing });
    }

    public IReadOnlyList<string> GetLastOpenedFiles()
    {
        if (_preferencesProvider == null) return Array.Empty<string>();

        var preferences = _preferencesProvider.Load();
        return preferences.DefaultFilesPreferences.LastOpenedProjectPath.ToList();
    }
}

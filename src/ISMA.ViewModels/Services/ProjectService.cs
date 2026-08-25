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
    private readonly ISyntaxHighlighter _syntaxHighlighter;
    private readonly IModelErrorService? _errorService;
    private readonly IPreferencesProvider? _preferencesProvider;

    public IProjectFileService ProjectFileService => _projectFileService;
    public ITextEditorFactory TextEditorFactory => _editorFactory;

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

    public void AddProject(IProjectViewModel project)
    {
        _projects.Add(project);
    }

    public void SetActiveProject(IProjectViewModel project)
    {
        ActiveProject = project;
    }

    public ProjectService(
        IProjectFileService projectFileService,
        ISimulationServerFacade serverFacade,
        ITextEditorFactory editorFactory,
        ISyntaxHighlighter syntaxHighlighter,
        IModelErrorService? errorService = null,
        IPreferencesProvider? preferencesProvider = null)
    {
        _projectFileService = projectFileService;
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
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

        return await OpenAsync(paths[0]);
    }

    public async Task<IProjectViewModel?> OpenAsync(string filePath)
    {
        var type = ProjectTypeFromPath(filePath);
        if (type == ProjectType.Legacy)
        {
            _errorService?.PutErrorList(new[]
            {
                new ErrorInfo
                {
                    Row = 0,
                    Position = 0,
                    FragmentName = "Open",
                    Message = "Legacy .im files are no longer supported. Please convert to .isma format first."
                }
            });
            return null;
        }

        IProjectViewModel project = type == ProjectType.Blueprint
            ? CreateBlueprintProject(filePath)
            : CreateLismaProject(filePath);
        _projects.Add(project);
        ActiveProject = project;
        return project;
    }

    private static ProjectType ProjectTypeFromPath(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".im2" => ProjectType.LismaText,
            ".iscm2" => ProjectType.Blueprint,
            ".im" => ProjectType.Legacy,
            _ => ProjectType.LismaText
        };
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
        foreach (var project in _projects)
        {
            if (string.IsNullOrEmpty(project.FilePath))
            {
                // Unsaved projects get a Save As dialog; cancelling just skips that project.
                switch (project)
                {
                    case LismaProjectViewModel lisma:
                        await lisma.SaveAsAsync();
                        break;
                    case BlueprintProjectViewModel blueprint:
                        await blueprint.SaveAsAsync();
                        break;
                }
            }
            else
            {
                switch (project)
                {
                    case LismaProjectViewModel lisma:
                        await lisma.SaveAsync();
                        break;
                    case BlueprintProjectViewModel blueprint:
                        await blueprint.SaveAsync();
                        break;
                }
            }
        }
        return true;
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
            _errorService);
        project.LoadFromFile(path);
        return project;
    }

    private BlueprintProjectViewModel CreateBlueprintProject(string path)
    {
        var project = new BlueprintProjectViewModel(_projectFileService, _editorFactory);
        project.LoadFromFile(path);
        return project;
    }

    private bool _sessionCaptured;

    /// <summary>
    /// Persists the file paths of all currently open projects (the session),
    /// matching the original app's exit-time capture. Called once per app run.
    /// </summary>
    public void CaptureOpenFiles()
    {
        if (_sessionCaptured || _preferencesProvider == null) return;
        _sessionCaptured = true;

        var paths = _projects
            .Where(p => !string.IsNullOrEmpty(p.FilePath))
            .Select(p => p.FilePath!)
            .ToArray();

        _preferencesProvider.CommitFiles(new DefaultFilesPreferences { LastOpenedProjectPath = paths });
    }

    public IReadOnlyList<string> GetLastOpenedFiles()
    {
        if (_preferencesProvider == null) return Array.Empty<string>();

        var preferences = _preferencesProvider.Load();
        return preferences.DefaultFilesPreferences.LastOpenedProjectPath.ToList();
    }
}

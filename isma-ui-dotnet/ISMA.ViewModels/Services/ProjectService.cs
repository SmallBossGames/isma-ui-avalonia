using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        SimulationParametersService parametersService)
    {
        _projectFileService = projectFileService;
        _serverFacade = serverFacade;
        _editorFactory = editorFactory;
        _parametersService = parametersService;
    }

    public async Task<IProjectViewModel?> CreateNewAsync()
    {
        var project = new LismaProjectViewModel(_serverFacade, _editorFactory);
        _projects.Add(project);
        ActiveProject = project;
        return project;
    }

    public async Task<IProjectViewModel?> CreateNewBlueprintAsync()
    {
        var project = new BlueprintProjectViewModel();
        var editorVm = new BlueprintEditorViewModel();
        project.SetEditorViewModel(editorVm);
        _projects.Add(project);
        ActiveProject = project;
        return project;
    }

   public async Task<IProjectViewModel?> OpenAsync()
    {
        var paths = await _projectFileService.Open(null);
        if (paths == null || paths.Count == 0)
            return null;

        var opened = await _projectFileService.Open((IList<string>)paths);
        var filePath = opened.FirstOrDefault().ToString() ?? "";
        IProjectViewModel project = CreateLismaProject(filePath);

        _projects.Add(project);
        ActiveProject = project;
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
        var project = new LismaProjectViewModel(_serverFacade, _editorFactory,
            new ISMA.Domain.Models.LismaTextModel("", Array.Empty<ISMA.Domain.Models.CodeRegion>()), path);
        return project;
    }

    private BlueprintProjectViewModel CreateBlueprintProject(string path)
    {
        var project = new BlueprintProjectViewModel();
        project.LoadFromFile(path);
        var editorVm = new BlueprintEditorViewModel();
        project.SetEditorViewModel(editorVm);
        return project;
    }

    private void SetProperty(ref IProjectViewModel? field, IProjectViewModel? value)
    {
        if (field == value)
            return;

        field = value;
    }
}

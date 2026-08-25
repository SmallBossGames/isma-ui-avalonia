using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ISMA.Domain.Contracts;
using ISMA.Domain.Conversion;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;

namespace ISMA.App.Services;

public class ProjectFileService : IProjectFileService
{
    private readonly Window? _owner;
    private readonly WindowProvider? _windowProvider;

    public ProjectFileService(Window? owner = null, WindowProvider? windowProvider = null)
    {
        _owner = owner;
        _windowProvider = windowProvider;
    }

    private Window? Owner => _owner ?? _windowProvider?.Current;

    public async Task<IList<string>> Open(object? ownerWindow)
    {
        var control = ownerWindow as Avalonia.Visual ?? Owner as Avalonia.Visual;
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is null) return Array.Empty<string>();

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Project File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("All ISMA project files") { Patterns = new[] { "*.im2", "*.iscm2" } },
            }
        });

        return files.Select(f => f.Path.LocalPath).ToList();
    }

    public async Task<bool> Save(object project)
    {
        if (project is not IProjectViewModel pv) return false;
        if (string.IsNullOrEmpty(pv.FilePath)) return await SaveAs(project);

        try
        {
            if (project is LismaProjectViewModel lisma)
            {
                await File.WriteAllTextAsync(pv.FilePath, lisma.FullText);
            }
            else if (project is BlueprintProjectViewModel blueprint)
            {
                var json = BlueprintFileSerializer.ToJson(blueprint.GetBlueprintModel());
                await File.WriteAllTextAsync(pv.FilePath, json);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAs(object project)
    {
        if (project is not IProjectViewModel pv) return false;
        var topLevel = TopLevel.GetTopLevel(Owner);
        if (topLevel is null) return false;

        var isBlueprint = project is BlueprintProjectViewModel;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project File",
            SuggestedFileName = pv.Name,
            SuggestedFileType = isBlueprint
                ? new FilePickerFileType("ISMA State Chart Project file") { Patterns = new[] { "*.iscm2" } }
                : new FilePickerFileType("ISMA Next Project file") { Patterns = new[] { "*.im2" } },
            FileTypeChoices = isBlueprint
                ? new[] { new FilePickerFileType("ISMA State Chart Project file") { Patterns = new[] { "*.iscm2" } } }
                : new[]
                {
                    new FilePickerFileType("ISMA Next Project file") { Patterns = new[] { "*.im2" } },
                    new FilePickerFileType("ISMA Project file") { Patterns = new[] { "*.im" } },
                },
            DefaultExtension = isBlueprint ? ".iscm2" : ".im2",
            ShowOverwritePrompt = true
        });

        if (file is null) return false;

        pv.FilePath = file.Path.LocalPath;
        UpdateNameAfterSave(project, Path.GetFileName(file.Path.LocalPath));
        return await Save(project);
    }

    public async Task<bool> SaveAll(IList<object> projects)
    {
        foreach (var project in projects)
        {
            if (!await Save(project)) return false;
        }
        return true;
    }

    private static void UpdateNameAfterSave(object project, string fileName)
    {
        switch (project)
        {
            case LismaProjectViewModel lisma:
                lisma.Name = fileName;
                break;
            case BlueprintProjectViewModel blueprint:
                blueprint.Name = fileName;
                break;
        }
    }
}

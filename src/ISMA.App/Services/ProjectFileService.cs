using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;
using ISMA.ViewModels.ViewModels;
using System.Text.Json;

namespace ISMA.App.Services;

public class ProjectFileService : IProjectFileService
{
    private readonly Window? _owner;

    public ProjectFileService(Window? owner = null)
    {
        _owner = owner;
    }

    public async Task<IList<string>> Open(object? ownerWindow)
    {
        var control = ownerWindow as Avalonia.Visual ?? _owner as Avalonia.Visual;
        var topLevel = TopLevel.GetTopLevel(control);
        if (topLevel is null) return Array.Empty<string>();

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open ISMA Project",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("All ISMA Files") { Patterns = new[] { "*.iscm2", "*.scisma", "*.im" } },
                new FilePickerFileType("LISMA Text") { Patterns = new[] { "*.iscm2" } },
                new FilePickerFileType("State Chart") { Patterns = new[] { "*.scisma" } },
                new FilePickerFileType("Legacy") { Patterns = new[] { "*.im" } },
            }
        });

        return files.Select(f => f.Path.LocalPath).ToList();
    }

    public async Task<IList<ProjectType>> Open(IList<string> paths)
    {
        var result = new List<ProjectType>();
        foreach (var path in paths)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            result.Add(ext switch
            {
                ".iscm2" => ProjectType.LismaText,
                ".scisma" => ProjectType.Blueprint,
                ".im" => ProjectType.Legacy,
                _ => ProjectType.LismaText
            });
        }
        return result;
    }

    public async Task<bool> Save(object project)
    {
        if (project is not IProjectViewModel pv) return false;
        if (string.IsNullOrEmpty(pv.FilePath)) return await SaveAs(project);

        try
        {
            if (project is LismaProjectViewModel lisma)
            {
                await File.WriteAllTextAsync(pv.FilePath, lisma.EditorContent as string ?? "");
            }
            else if (project is BlueprintProjectViewModel blueprint)
            {
                var model = blueprint.GetBlueprintModel();
                var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true });
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
        var topLevel = TopLevel.GetTopLevel(_owner);
        if (topLevel is null) return false;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Project",
            SuggestedFileName = pv.Name,
            DefaultExtension = ".iscm2",
            ShowOverwritePrompt = true
        });

        if (file is null) return false;

        pv.FilePath = file.Path.LocalPath;
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
}

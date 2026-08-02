using System;
using System.Collections.Generic;
using System.Linq;
using ISMA.BlueprintEditor.Utilities;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Services;

/// <summary>
/// In-memory clipboard for copying/pasting states in the blueprint editor.
/// Copy stores state list. Paste creates new states with new Guids, offset positions, and "(copy)" suffix.
/// </summary>
public class BlueprintClipboardService
{
    private readonly List<BlueprintStateViewModel> _copiedStates = new();

    /// <summary>
    /// Copies the specified states to the clipboard.
    /// </summary>
    /// <param name="states">The states to copy.</param>
    public void CopyStates(IEnumerable<BlueprintStateViewModel> states)
    {
        _copiedStates.Clear();
        _copiedStates.AddRange(states);
    }

    /// <summary>
    /// Pastes the copied states with new Guids, offset positions, and "(copy)" suffix.
    /// </summary>
    /// <param name="editor">The editor view model to paste into.</param>
    /// <param name="offsetX">X offset for pasted states.</param>
    /// <param name="offsetY">Y offset for pasted states.</param>
    /// <returns>The newly created pasted states.</returns>
    public IEnumerable<BlueprintStateViewModel> PasteStates(BlueprintEditorViewModel editor, double offsetX, double offsetY)
    {
        if (_copiedStates.Count == 0)
            yield break;

        var nameMonitor = new NameChangingMonitor();
        foreach (var name in editor.States.Select(s => s.Name).Concat(
                 new[] { editor.MainState?.Name ?? "", editor.InitState?.Name ?? "" }))
        {
            if (!string.IsNullOrEmpty(name))
                nameMonitor.TryRegister(name);
        }

        foreach (var copiedState in _copiedStates)
        {
            var newState = new BlueprintStateViewModel
            {
                Id = Guid.NewGuid(),
                CanvasPositionX = copiedState.CanvasPositionX + offsetX,
                CanvasPositionY = copiedState.CanvasPositionY + offsetY,
                Name = copiedState.Name + " (copy)",
                Text = copiedState.Text,
                IsEditable = true,
                IsMain = false,
                IsInit = false,
                FillColor = copiedState.FillColor,
                StateHeight = copiedState.StateHeight
            };

            // Ensure unique name
            if (!nameMonitor.TryRegister(newState.Name))
            {
                newState.Name = nameMonitor.CreateNextDefaultName("New state");
            }

            editor.States.Add(newState);
            yield return newState;
        }
    }
}

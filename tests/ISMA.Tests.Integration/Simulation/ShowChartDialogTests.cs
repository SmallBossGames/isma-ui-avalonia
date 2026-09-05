global using global::Xunit;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using ISMA.Domain.Dtos;
using ISMA.Domain.Models;

namespace ISMA.Tests.Integration.Simulation;

/// <summary>
/// Drives the real "Show" flow from the tasks list: the select-variables dialog
/// opens, a variable is checked via the UI, OK is clicked, and the real
/// GrinProcessLauncher starts a fake Grin script that records its arguments.
/// Also asserts the dialog layout: action buttons render below the list.
/// </summary>
public class ShowChartDialogTests : IDisposable
{
    private readonly TestApp _app = (TestApp)Application.Current!;
    private readonly List<string> _tempFiles = new();

    [AvaloniaFact]
    public async Task Dialog_Layout_ActionButtonsRenderBelowVariablesList()
    {
        var viewModel = new SelectVariablesDialogViewModel();
        viewModel.InitializeColumns(Enumerable.Range(0, 40).Select(i => $"DE_{i}-col{i}").Prepend("TIME"));

        var window = new SelectVariablesDialogWindow(viewModel);
        window.Show();

        try
        {
            var listBox = UiHelpers.FindDescendants<ListBox>(window).Single();
            await WaitForLayoutAsync(listBox);

            // The list is height-constrained (scrolls) and every action button
            // renders below it, not over it.
            var listBottom = listBox.TranslatePoint(new Point(0, listBox.Bounds.Height), window)!.Value.Y;
            foreach (var buttonName in new[] { "Select All", "Unselect All", "OK", "Cancel" })
            {
                var button = FindButton(window, buttonName);
                var top = button.TranslatePoint(default, window)!.Value.Y;
                top.Should().BeGreaterThanOrEqualTo(listBottom - 1, $"{buttonName} must be below the list");
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Show_Ok_LaunchesGrinWithSelectedColumns()
    {
        _app.Window.ClickMenuItem("MenuNewText");

        var resultFile = CreateResultFile();
        var argsFile = Path.Combine(Path.GetTempPath(), $"isma-test-grin-{Guid.NewGuid():N}.args");
        var script = CreateFakeGrinScript(argsFile);
        AppContext.SetData("isma.grin.script", script);

        _app.MockServer.CompileHandler = _ => Task.FromResult(new CompileResult { ModelId = "test-model" });
        _app.MockServer.RunHandler = _ => Task.FromResult(1L);
        _app.MockServer.MonitorHandler = _ => AsyncEnumerable.Empty<SimulationProgress>();
        _app.MockServer.DownloadHandler = _ => Task.FromResult(new CachedSimulationResult
        {
            File = resultFile,
            ColumnNames = ImmutableArray.Create("TIME", "DE_0-x", "DE_1-y", "f0")
        });

        _app.Window.ClickMenuItem("MenuRun");
        _app.ViewModel.TasksPopOver.Completed.Should().HaveCount(1);

        // The tasks popover flyout cannot be rendered headless, so the Show
        // command of the realized task item is invoked directly.
        _app.GetRequiredService<WindowProvider>().Current = _app.Window;
        var completedVm = _app.ViewModel.TasksPopOver.Completed.First();
        completedVm.ShowCommand.Execute(null);

        var dialog = await WaitForDialogAsync();

        // Layout: the list is height-constrained and all action buttons
        // render below it (not over it).
        var listBox = UiHelpers.FindDescendants<ListBox>(dialog).Single();
        await WaitForLayoutAsync(listBox);

        var listBottom = listBox.TranslatePoint(new Point(0, listBox.Bounds.Height), dialog)!.Value.Y;
        foreach (var buttonName in new[] { "Select All", "Unselect All", "OK", "Cancel" })
        {
            var button = FindButton(dialog, buttonName);
            var top = button.TranslatePoint(default, dialog)!.Value.Y;
            top.Should().BeGreaterThanOrEqualTo(listBottom - 1, $"{buttonName} must be below the list");
        }

        // Select DE_0-x through the UI (pointer click on the checkbox).
        var checkBox = FindCheckBox(dialog, "DE_0-x");
        ClickCenter(dialog, checkBox);
        checkBox.IsChecked.Should().BeTrue();

        // Click OK through the UI.
        ClickCenter(dialog, FindButton(dialog, "OK"));

        // The launcher starts the fake Grin script, which records its arguments.
        await WaitForFileAsync(argsFile);
        var args = File.ReadAllLines(argsFile);
        args.Should().ContainInOrder("--result-file", resultFile, "--x-axis", "TIME", "--charts", "DE_0-x");
        args.Should().NotContain("DE_1-y");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<SelectVariablesDialogWindow> WaitForDialogAsync()
    {
        for (var i = 0; i < 100; i++)
        {
            var dialog = _app.DialogTracker.LastDialog;
            if (dialog is not null)
            {
                return dialog;
            }

            await Task.Delay(20);
        }

        throw new InvalidOperationException("select variables dialog did not open");
    }

    private static async Task WaitForLayoutAsync(ListBox listBox)
    {
        for (var i = 0; i < 100; i++)
        {
            if (listBox.Bounds.Height > 0)
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new InvalidOperationException("list box was not laid out");
    }

    private static Button FindButton(Control root, string content)
    {
        var button = UiHelpers.FindDescendants<Button>(root)
            .FirstOrDefault(b => b.Content as string == content);
        button.Should().NotBeNull($"button '{content}'");
        return button!;
    }

    private static CheckBox FindCheckBox(Control root, string content)
    {
        var checkBox = UiHelpers.FindDescendants<CheckBox>(root)
            .FirstOrDefault(c => c.Content as string == content);
        checkBox.Should().NotBeNull($"checkbox '{content}'");
        return checkBox!;
    }

    private static void ClickCenter(Window window, Control control)
    {
        var point = control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
    }

    private static async Task WaitForFileAsync(string path)
    {
        for (var i = 0; i < 250; i++)
        {
            if (File.Exists(path))
            {
                return;
            }

            await Task.Delay(20);
        }

        throw new InvalidOperationException($"Grin arguments file was not written: {path}");
    }

    /// <summary>
    /// Writes a result file in the real ISMA binary exchange format
    /// (big-endian: short column count, per-column short length + UTF-8 name,
    /// then columnCount big-endian doubles per row until EOF).
    /// </summary>
    private string CreateResultFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"isma-test-{Guid.NewGuid():N}.bin");
        using var fs = File.Create(path);

        var columns = new[] { "TIME", "DE_0-x", "DE_1-y", "f0" };
        WriteUInt16BE(fs, (ushort)columns.Length);
        foreach (var name in columns)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(name);
            WriteUInt16BE(fs, (ushort)bytes.Length);
            fs.Write(bytes);
        }

        WriteRow(fs, 0.0, 1.0, 2.0, -1.0);
        WriteRow(fs, 0.1, 0.9, 1.8, -0.9);

        _tempFiles.Add(path);
        return path;
    }

    private static void WriteUInt16BE(Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    private static void WriteRow(Stream stream, params double[] values)
    {
        foreach (var value in values)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            stream.Write(bytes);
        }
    }

    /// <summary>
    /// Creates an executable fake Grin script that records its arguments to
    /// <paramref name="argsFile"/>.
    /// </summary>
    private string CreateFakeGrinScript(string argsFile)
    {
        var path = Path.Combine(Path.GetTempPath(), $"isma-test-grin-{Guid.NewGuid():N}.sh");
        File.WriteAllText(path, $"#!/bin/sh\nprintf '%s\\n' \"$@\" > \"{argsFile}\"\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}

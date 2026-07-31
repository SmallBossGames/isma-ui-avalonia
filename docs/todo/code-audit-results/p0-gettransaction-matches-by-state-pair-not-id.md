# GetTransaction matches by state pair, not by transition ID

**Status:** FIXED (verified 2026-08-01)

**Location:** `src/ISMA.App/Views/BlueprintEditorView.axaml.cs:235-244`

**Category:** correctness

## Original Issue

The `GetTransaction` method resolved a `BlueprintTransitionViewModel` by matching `StartStateId` and `EndStateId` only. If two transitions existed between the same pair of states, `FirstOrDefault` returned whichever was added first.

## Fix Applied

The `ArrowLine` control now carries a `Guid? Id` property (ArrowLine.cs:54-64), and the `GetTransaction` method prioritizes ID-based matching:

```csharp
private BlueprintTransitionViewModel? GetTransaction(Controls.ArrowLine arrow)
{
    if (arrow.Id != null)
    {
        return _vm?.Transitions.FirstOrDefault(t => t.Id == arrow.Id);
    }

    if (arrow.StartStateId == Guid.Empty || arrow.EndStateId == Guid.Empty) return null;
    return _vm?.Transitions.FirstOrDefault(t => t.StartStateId == arrow.StartStateId && t.EndStateId == arrow.EndStateId);
}
```

The XAML binds the transition `Id` to the `ArrowLine.Id` property (BlueprintEditorView.axaml:72):
```xml
<controls:ArrowLine Id="{Binding Id}" .../>
```

The fallback to state-pair matching remains for backward compatibility.

## Verification

- Build: ✅ `dotnet build isma-ui-dotnet.slnx` — 0 errors, 0 warnings
- Tests: ✅ `dotnet test` — 266 unit + 180 integration = 446 passed, 0 failed

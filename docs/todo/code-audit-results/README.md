# Blueprint Editor Code Audit Results

**Audit date:** 2026-08-01
**Total findings:** 21

## Most Critical Issues

1. **[LoopArrow never rendered](looparrow-positionresolver-not-bound.md)** — `PositionResolver` is not bound in XAML, so loop arrows are never drawn on the canvas.
2. **[PopOver changes not propagated to loops](popover-changes-not-propagated-to-loops.md)** — Editing alias/predicate in the PopOver discards changes when editing a loop transaction.
3. **[RemoveState redo is a no-op](removestate-undo-redo-redo-is-noop.md)** — The redo action for state removal is empty, breaking undo/redo symmetry.
4. **[RemoveLoop has no undo/redo](removeloop-no-undo-redo.md)** — Removing a loop transaction cannot be undone, inconsistent with state/transition removal.

## Correctness (7)

- [LoopArrow never rendered — PositionResolver not bound in XAML](looparrow-positionresolver-not-bound.md)
- [PopOver alias/predicate changes not propagated to loop transactions](popover-changes-not-propagated-to-loops.md)
- [PasteStates allows duplicate state names after TryRegister failure](pastestates-duplicate-names-allowed.md)
- [RemoveLoop does not use undo/redo service](removeloop-no-undo-redo.md)
- [GetTransaction matches by state pair, not by transition ID](gettransaction-matches-by-state-pair-not-id.md)
- [RemoveState undo action is a no-op — redo does nothing](removestate-undo-redo-redo-is-noop.md)
- [NameChangingMonitor uses case-sensitive HashSet — "Main" and "main" treated as distinct](namesettingmonitor-hashset-case-sensitivity.md)

## Architecture (2)

- [BlueprintEditorView code-behind tightly coupled to MainWindowViewModel](codebehind-tight-coupling-to-mainwindow.md)
- [BlueprintTransitionViewModel/_states field set externally creates hidden coupling](blueprinttransitionviewmodel-states-field-external-dependency.md)

## Quality (9)

- [Empty stub methods in BlueprintProjectViewModel](empty-stub-commands-in-blueprintproject.md)
- [State center position calculation duplicated across multiple files](state-position-calculation-duplicated.md)
- [Magic string "1 > 0" used as default loop predicate](magic-string-default-loop-predicate.md)
- [ArrowLine MeasureOverride returns fixed 200x200 even when not rendered](arrowline-measures-fixed-200x200-when-not-rendered.md)
- [SelectedStateCount property has confusing conditional logic](selectedstatecount-logic-confusing.md)
- [ArrowLine hit test uses magic number threshold for arrowhead detection](arrowline-hit-test-threshold-magic-number.md)
- [EditorMode type changes do not trigger property change notifications](blueprinteditormode-not-observable.md)
- [Clipboard paste performs shallow copy — FillColor reference shared with original](clipboard-shallow-copy-shared-fillcolor.md)
- [Dead OnLoaded handler registered but does nothing](dead-onloaded-handler.md)

## Performance (3)

- [LoadFromFile performs synchronous file I/O on the UI thread](synchronous-io-in-loadfromfile.md)
- [SaveAsync performs synchronous JSON serialization on the UI thread](synchronous-json-serialization-in-save.md)
- [DispatcherTimer event handler not unsubscribed causes memory leak](dispatcher-timer-memory-leak.md)

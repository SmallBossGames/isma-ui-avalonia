---
name: code-audit
description: >
  Perform strict, systematic code audits on any requested component, module,
  or directory. Find bugs, architectural flaws, anti-patterns, and deviations
  from project conventions. Report each finding as a separate file under
  docs/todo/. Do not propose fixes — only describe problems.
license: MIT
metadata:
  audience: developers
  workflow: code-review
---

## What I do

- Audit a requested scope (file, directory, module, or feature)
- Identify bugs, logic errors, and missing error handling
- Flag architectural anti-patterns, tight coupling, and poor separation of concerns
- Detect deviations from project conventions and coding rules
- Review UI / view-model boundaries for MVVM compliance
- Check for performance issues, memory leaks, and resource leaks
- Report each finding as a separate file in `docs/todo/code-audit-results/`

## When to use me

Use this skill when someone asks to:
- "Audit this component / module / feature"
- "Review this code for problems"
- "Find all issues in X"
- "Why doesn't this work?" (investigative audit)

## How I work

### 1. Understand the scope

- Confirm which files, classes, or features to audit
- Read the project's AGENTS.md or equivalent coding rules
- If the project has feature documentation, read it to understand intended behavior (but do not treat it as the architectural reference)

### 2. Build and test (catch compilation + runtime issues first)

- Run `dotnet build isma-ui-dotnet.slnx` — report all build errors/warnings as findings
- Run `dotnet test` — report failing tests as findings
- If the build fails, report the compiler errors as the first priority

### 3. Search for concrete bug patterns (active hunting, not passive reading)

Run these targeted searches across the requested scope. Each match is a potential finding:

**Async / concurrency bugs**
- `async` methods without `await` (fire-and-forget, swallowed exceptions)
- `Task` return types not awaited by callers (caller assumes sync completion)
- `List<T>` fields used without `lock` or `ConcurrentBag`/`ConcurrentQueue` (race conditions)
- `Task.Run` on UI thread context (deadlock risk)
- `ConfigureAwait(false)` missing in library code (ViewModel/Service layer)

**Null reference risks**
- Non-nullable fields assigned from `sp.GetService<T>()` without null check
- `?.` null-conditional operators used to suppress errors instead of handling them
- `async Task<T>` methods that can return `null` without documentation
- `IReadOnlyList<T>` backed by `ToList()` on null collections

**DI / dependency injection issues**
- Duplicate service registrations (same interface/type registered twice)
- `sp.GetService<T>()` inside factory lambdas (circular dependency, wrong lifetime)
- Concrete types registered instead of interfaces
- Services used by ViewModels but not registered in `ServiceCollectionExtensions.cs`
- Lifetime mismatches (singleton resolving transient/scoped dependency)

**Memory / resource leaks**
- `Dispose()` called without null check on objects that may not implement `IDisposable`
- Event subscriptions without corresponding unsubscriptions
- `StreamWriter`/`FileStream` / `using` missing around I/O
- `ObservableCollection` subscriptions not tracked

**API design bugs**
- `async Task<bool>` vs `async Task` — inconsistent error reporting (exceptions vs return values)
- Methods that silently return `false` or `null` without any way to distinguish "not attempted" from "failed"
- Overloads that behave differently (e.g. `CloseAsync()` with no args vs `CloseAsync(project)`)

**C# code rule violations**
- Properties with `set` that should be `init` (per AGENTS.md)
- Top-level classes not in separate files (per AGENTS.md)
- Missing XML documentation on public interfaces (per AGENTS.md)
- Records used where classes should be, or vice versa
- Reflection usage (per AGENTS.md: "avoid using reflection")

**Avalonia / MVVM issues**
- `x:DataType` missing on views (project uses compiled bindings globally)
- Code-behind with logic (per AGENTS.md: "no XAML code-behind logic")
- Binding paths that don't match any property on the ViewModel type
- `Command` bindings to methods that don't match `[RelayCommand]` patterns
- `ObservableProperty` fields with incompatible types (e.g. non-observable collections)

### 4. Trace critical execution paths

For each critical operation in the scope, follow the full call chain:

**Critical operations to trace:**
- Project open → save → close lifecycle
- Simulation start/stop/monitor
- Error handling flow (how errors propagate from gRPC → ViewModel → UI)
- Settings/preferences save/load

**For each path, check:**
- Missing null checks at each boundary
- Unhandled exceptions (no try/catch at async method entry points)
- Error propagation — how does the caller know the operation failed?
- State consistency — is the object in a valid state if an exception occurs mid-operation?

### 5. Audit DI registrations

Read `src/ISMA.App/ServiceCollectionExtensions.cs` and check:

- Duplicate registrations (same service type registered more than once)
- Lifetime mismatches (singleton resolving transient/scoped dependency)
- Factory lambdas using `sp.GetService()` — check for circular dependencies
- Services resolved by concrete type instead of interface
- ViewModels registered but their dependencies are not
- Order-dependent registrations (services registered after the types that depend on them)

### 6. Verify XAML bindings

For each `.axaml` file in scope:

- Check that every binding path exists as a public property on the `x:DataType` type
- Verify `x:DataType` is set on all views (compiled bindings are enabled globally)
- Check for bindings to private fields or non-observable properties
- Check for `Binding` without `x:DataType` (will fail at runtime with compiled bindings)

### 7. Report findings

- Create one file per finding in `docs/todo/code-audit-results/`
- File naming: `<short-descriptive-name>.md`
- Each file must contain:
  1. **Title** — one-line summary
  2. **Location** — file path and line numbers
  3. **Category** — correctness / architecture / quality / performance / security
  4. **Description** — what the problem is and why it matters
  5. **Evidence** — relevant code snippet with line references
  6. **Impact** — what could go wrong if not fixed

### 8. Do NOT

- Propose fixes or solutions (only describe problems)
- Audit code outside the requested scope
- Use feature docs as the architectural reference — they describe intent, not the current implementation
- Add subjective opinions — be specific and evidence-based

## Output format example

```markdown
# Missing null check on injected service

**Location:** `src/ISMA.ViewModels/Services/ProjectService.cs:45`
**Category:** correctness

The `_projectRepository` field is used without null checking after injection.
If DI fails to resolve this dependency, a `NullReferenceException` will be thrown at runtime.

```csharp
// Line 45
var project = _projectRepository.GetById(id);
```

**Impact:** Application crash when the service is not properly registered in DI container.
```

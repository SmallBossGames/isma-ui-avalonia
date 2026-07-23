---
name: code-audit
description: >
  Perform strict, systematic code audits on any requested component, module,
  or directory. Find bugs, architectural flaws, anti-patterns, and deviations
  from project conventions. Report each finding in a separate file under
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
- Report each finding as a separate file in `docs/todo/`

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

### 2. Explore deeply

- Start from the entry point and trace the full execution path
- Use LSP tools first (`goToDefinition`, `hover`, `findReferences`), fall back to grep/glob/read only
- Map the dependency graph and data flow
- For UI code: verify MVVM separation (no logic in code-behind, proper bindings, correct data context)
- For ViewModels: verify CommunityToolkit.Mvvm patterns, proper DI usage, correct async/await handling

### 3. Audit checklist

Run through these categories systematically:

**Correctness**
- Null reference risks, missing null checks
- Incorrect async/await usage (async without await, missing ConfigureAwait, deadlocks)
- Race conditions, missing synchronization
- Broken bindings, wrong binding paths, missing x:DataType
- Event handler leaks (subscribed but never unsubscribed)

**Architecture**
- Violation of dependency direction (e.g., Domain project referencing Presentation)
- Tight coupling, god classes, too-many-responsibilities
- Missing abstraction layers or unnecessary indirection
- MVVM violations (logic in code-behind, View referencing other Views directly)
- Circular dependencies

**Code Quality**
- Violation of project coding rules (from AGENTS.md)
- Duplicated code, copy-paste patterns
- Magic strings, magic numbers, hardcoded paths
- Inconsistent naming, unclear variable names
- Unused imports, unused fields, dead code

**Performance**
- Synchronous I/O on UI thread
- Unnecessary allocations, string concatenation in loops
- Missing caching where appropriate
- Event handler registration in loops or frequently-called methods

**Security**
- Secrets or keys hardcoded or logged
- SQL injection, command injection risks
- Unsafe deserialization
- Missing input validation

### 4. Report findings

- Create one file per finding in `docs/todo/code-audit-results/`
- File naming: `<short-descriptive-name>.md`
- Each file must contain:
  1. **Title** — one-line summary
  2. **Location** — file path and line numbers
  3. **Category** — correctness / architecture / quality / performance / security
  4. **Description** — what the problem is and why it matters
  5. **Evidence** — relevant code snippet with line references
  6. **Impact** — what could go wrong if not fixed

### 5. Do NOT

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

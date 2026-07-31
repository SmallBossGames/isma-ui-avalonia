# NameChangingMonitor uses case-sensitive HashSet — "Main" and "main" treated as distinct

**Location:** `src/ISMA.ViewModels/Services/NameChangingMonitor.cs:8`

**Category:** correctness

**Status:** RESOLVED (commit `d220394`)

The `_existedNames` field was a `HashSet<string>` without an explicit `StringComparer`:

```csharp
// NameChangingMonitor.cs:8 (BEFORE)
private readonly HashSet<string> _existedNames = new();
```

On Linux, `HashSet<string>` uses the default `StringComparer` which is case-sensitive. This meant `"Main"` and `"main"` were treated as different names. A user could register both "Main" and "main" as state names, and both would pass `TryRegister`.

The `TryRegister` method at line 12 checks `_existedNames.Contains(name)` which was case-sensitive on Linux.

**Impact:** On Linux systems, state names that differ only in casing could coexist in the same blueprint, potentially causing confusion in the UI and issues in downstream consumers. The behavior was platform-dependent — the same blueprint behaved differently on Windows (case-insensitive `StringComparer` by default) versus Linux.

## Resolution

Fixed by passing `StringComparer.OrdinalIgnoreCase` to the `HashSet<string>` constructor:

```csharp
// NameChangingMonitor.cs:8 (AFTER)
private readonly HashSet<string> _existedNames = new(StringComparer.OrdinalIgnoreCase);
```

This ensures consistent case-insensitive behavior across all platforms.

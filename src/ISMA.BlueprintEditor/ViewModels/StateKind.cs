namespace ISMA.BlueprintEditor.ViewModels;

/// <summary>Kind of a state; determines color and editability protections.</summary>
public enum StateKind
{
    /// <summary>The built-in Main state (protected from rename/removal).</summary>
    Main,
    /// <summary>The built-in init state (protected from rename/removal).</summary>
    Init,
    /// <summary>A user-defined state.</summary>
    User
}

using ISMA.ViewModels.ViewModels;

namespace ISMA.ViewModels.Services;

/// <summary>
/// Result of validating a blueprint editor.
/// </summary>
public class BlueprintValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates an invalid result with the given errors.
    /// </summary>
    public static BlueprintValidationResult Invalid(IEnumerable<string> errors)
    {
        return new BlueprintValidationResult { IsValid = false, Errors = errors.ToList().AsReadOnly() };
    }

    /// <summary>
    /// Creates a valid result.
    /// </summary>
    public static BlueprintValidationResult Valid()
    {
        return new BlueprintValidationResult { IsValid = true };
    }
}

/// <summary>
/// Service for validating blueprint editor consistency.
/// </summary>
public interface IBlueprintValidationService
{
    /// <summary>
    /// Validates the given blueprint editor.
    /// </summary>
    /// <param name="editor">The blueprint editor to validate.</param>
    /// <returns>The validation result.</returns>
    BlueprintValidationResult Validate(BlueprintEditorViewModel editor);
}

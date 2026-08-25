using System.Text.Json;
using System.Text.Json.Serialization;

namespace ISMA.Domain.Models;

/// <summary>
/// File schema for simulation parameters, matching the original Kotlin
/// <c>SimulationParametersModel</c> kotlinx.serialization output so files are
/// interoperable between the original app and this one.
/// </summary>
public sealed class SimulationParametersFileModel
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new KotlinDoubleJsonConverter(), new SaveTargetJsonConverter() }
    };

    [JsonPropertyName("cauchyInitials")]
    public CauchyInitials CauchyInitials { get; set; } = new();

    [JsonPropertyName("eventDetectionParameters")]
    public EventDetectionParameters EventDetection { get; set; } = new();

    [JsonPropertyName("integrationMethodParameters")]
    public IntegrationMethodParameters IntegrationMethod { get; set; } = new();

    [JsonPropertyName("resultSavingParameters")]
    public ResultSavingParameters ResultSaving { get; set; } = new();

    public static SimulationParametersFileModel From(SimulationParameters parameters) => new()
    {
        CauchyInitials = parameters.CauchyInitials,
        EventDetection = parameters.EventDetection,
        IntegrationMethod = parameters.IntegrationMethod,
        ResultSaving = parameters.ResultSaving
    };

    public SimulationParameters ToModel() => new()
    {
        CauchyInitials = CauchyInitials,
        EventDetection = EventDetection,
        IntegrationMethod = IntegrationMethod,
        ResultSaving = ResultSaving
    };
}

/// <summary>
/// Serializes doubles like Kotlin's <c>Double.toString</c>: shortest round-trip
/// representation with at least one decimal digit (0.0, 10.0, 0.1).
/// </summary>
public sealed class KotlinDoubleJsonConverter : JsonConverter<double>
{
    /// <summary>
    /// Formats a double like Kotlin's <c>Double.toString</c>: shortest round-trip
    /// representation with at least one decimal digit.
    /// </summary>
    public static string Format(double value)
    {
        var text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (!text.Contains('.') && !text.Contains('E') && !text.Contains('e'))
        {
            text += ".0";
        }
        return text;
    }

    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDouble();

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) =>
        writer.WriteRawValue(Format(value));
}

/// <summary>
/// Serializes <see cref="SaveTarget"/> using the original enum names ("MEMORY", "FILE").
/// </summary>
public sealed class SaveTargetJsonConverter : JsonConverter<SaveTarget>
{
    public override SaveTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "MEMORY" => SaveTarget.Memory,
            "FILE" => SaveTarget.File,
            _ => SaveTarget.Memory
        };
    }

    public override void Write(Utf8JsonWriter writer, SaveTarget value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == SaveTarget.File ? "FILE" : "MEMORY");
    }
}

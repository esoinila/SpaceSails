using System.Text.Json;
using SpaceSails.Contracts;

namespace SpaceSails.Core;

/// <summary>Loads scenario JSON files (scenarios/*.json).</summary>
public static class ScenarioLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ScenarioDefinition Parse(string json) =>
        JsonSerializer.Deserialize<ScenarioDefinition>(json, Options)
        ?? throw new InvalidOperationException("Scenario JSON deserialized to null.");

    public static ScenarioDefinition LoadFile(string path) => Parse(File.ReadAllText(path));
}

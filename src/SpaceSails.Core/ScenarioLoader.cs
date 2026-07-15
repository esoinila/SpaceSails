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

    public static ScenarioDefinition Parse(string json)
    {
        ScenarioDefinition scenario = JsonSerializer.Deserialize<ScenarioDefinition>(json, Options)
            ?? throw new InvalidOperationException("Scenario JSON deserialized to null.");
        Validate(scenario);
        return scenario;
    }

    // Rails carry only bound conics: eccentricity must be a finite value in [0, 1). A negative or
    // parabolic/hyperbolic (>= 1) eccentricity has no closed-orbit rail and would make the Kepler
    // solve diverge, so it is a hard load error rather than a silent clamp.
    private static void Validate(ScenarioDefinition scenario)
    {
        foreach (BodyDefinition body in scenario.Bodies)
        {
            if (!double.IsFinite(body.Eccentricity) || body.Eccentricity < 0.0 || body.Eccentricity >= 1.0)
            {
                throw new InvalidOperationException(
                    $"Body '{body.Id}' has eccentricity {body.Eccentricity}; rails require 0 <= e < 1.");
            }
        }
    }

    public static ScenarioDefinition LoadFile(string path) => Parse(File.ReadAllText(path));
}

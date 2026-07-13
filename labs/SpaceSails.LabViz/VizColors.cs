namespace SpaceSails.LabViz;

/// <summary>
/// The lab-viz palette, mirroring the live game's <c>Map.razor</c> constant region (~line 1716) so a
/// pop-up reads as a SpaceSails instrument rather than a generic plot. Kept in one place so every
/// lab wiring draws its flown itinerary and sweeps in the same hues.
/// </summary>
public static class VizColors
{
    /// <summary>The flown trajectory / itinerary. Mirrors the game's <c>TrajectoryColor = RgbaColor(255,165,0)</c>.</summary>
    public const string Trajectory = "#ffa500";

    /// <summary>Cool blue-gray for the aim-offset sweep family — distinct from the orange itinerary.</summary>
    public const string Sweep = "#7fa8c9";

    /// <summary>Body/marker label ink. Mirrors the game's <c>LabelColor = RgbaColor(224,228,236)</c>.</summary>
    public const string Label = "#e0e4ec";

    /// <summary>The ghost ship dot. Mirrors the game's <c>ShipColor = RgbaColor(255,210,80)</c>.</summary>
    public const string Ship = "#ffd250";
}

namespace SpaceSails.Core;

// #1332 A · EVERY HUB IS A LOBBY — one row per haven that grew a lower level in this slice, spread into
// DevStarts.All straight after Selene Gate's own service-level row (#1253), so the seven floors stand
// together in the list. A method, not a field, so no static initializer depends on which file is compiled
// first (#1163); a partial of its own because DevStarts.cs is held under the size gate.
public static partial class DevStarts
{
    /// <summary>#1332 A · The six lower levels that are not Selene Gate's, one button each, in the catalogue's
    /// own haven order. Each is the same cheat Selene Gate's row uses — <c>&amp;havenfloor=-1</c> rides the first
    /// car down — pointed at a different berth.</summary>
    private static IReadOnlyList<Entry> BelowTheLobbies() =>
    [
        Below("🛗🔑", "The Rusty Roadstead — LONG-STAY", "the-space-bar", HavenLevels.SpaceBarPlate),
        Below("🛗🌋", "Cinder Roost — BERTH HOTEL", "cinder-roost", HavenLevels.CinderRoostPlate),
        Below("🛗🪐", "Ringside Exchange — MEMBERS' ROOMS", "ringside-exchange", HavenLevels.RingsidePlate),
        Below("🛗🌀", "The Tilt — ROOMS", "the-tilt", HavenLevels.TiltPlate),
        Below("🛗🔴", "The Red Eye — CREW QUARTERS", "red-eye", HavenLevels.RedEyePlate),
        Below("🛗❄", "The Deep — COLD ROOMS", "the-deep", HavenLevels.DeepPlate),
    ];

    /// <summary>One lower-level row: the same blurb at every haven but for the plate the tester should read,
    /// because the floor IS the same floor at every haven but for the plate.</summary>
    private static Entry Below(string icon, string label, string berth, string plate) =>
        new(icon, label,
            "Ashore and then one floor DOWN, standing where the first cage's doors open. The floor's one label "
            + $"reads {plate}; a corridor round the ring, five CABIN doors that do not open for you (press one: "
            + "it is time, not a key), and three cars back up, each landing at its own edge of the hall. The "
            + "first ride down at each haven says one line, once, when the slot is free (#1332).",
            $"/map?dock={berth}&ashore=1&havenfloor=-1");
}

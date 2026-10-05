namespace SpaceSails.Core;

// #653 slice 1 · THE DEAD STATION — one row spread into DevStarts.All beside the charter hull (#1074 beat 5). A method,
// not a field, so no static initializer depends on which file is compiled first (#1163); a partial of its own because
// DevStarts.cs is held under the size gate.
public static partial class DevStarts
{
    /// <summary>#653 · The dead station, boarded at her crew lock. Two or three of her four tubes are severed (seeded),
    /// so the boat is how you get to the arms the tubes will not let you walk to.</summary>
    private static IReadOnlyList<Entry> TheDeadStation() =>
    [
        new("🛰", "The dead station — her severed tubes and the boat between them",
            "Landed at the crew lock of a dead station: a hub drum and four arms, each plated at its door. Read a severed "
            + "tube with [E] at its end (the line is the tube's own). Walk to the boat's dock fixtures: a destination "
            + "console prices the flight, and a face with no serviceable lock must be CUT first — with a hull cutter in "
            + "the satchel (🔥 from the bar), spent a cut at a time. A flight spends clock and nothing else: the tank, "
            + "the nerve and the magazine are exactly where they were. Nothing aboard says why she is dead (#653).",
            "/map?station=1&land=1"),
    ];
}

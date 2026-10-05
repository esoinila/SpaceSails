namespace SpaceSails.Core;

// #1074 beat 5 · THE RETURNING SHUTTLE — one row spread into DevStarts.All beside the office (#1332 C). A method, not a
// field, so no static initializer depends on which file is compiled first (#1163); a partial of its own because
// DevStarts.cs is held under the size gate.
public static partial class DevStarts
{
    /// <summary>#1074 beat 5 · The preserved site with the charter hull already parked and her shuttle already
    /// back. The captain lands at the tube; the hull stands beside the fence.</summary>
    private static IReadOnlyList<Entry> TheReturningShuttle() =>
    [
        new("🛸🪢", "The preserved site — the charter hull, her shuttle back",
            "Landed on the preserved rock: the fence and the notice round the shed as in ?preserved=1, and beside "
            + "it a parked hull plated " + ReturningShuttle.BoardTag + " with a gap in her long wall. Walk in: three "
            + "fixtures, each told once ([E] the airlock, the gun rack, the log desk — the desk hands over a page and "
            + "files one 📍 line). Press 5 for the traffic board: the hull is on it. Within a world window the wire "
            + "prints its one line, once. Nothing anywhere says whether anybody is alive (#1074).",
            "/map?shuttle=1&land=1"),
    ];
}

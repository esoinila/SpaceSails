namespace SpaceSails.Core;

// #1332 C · THE PRESERVATION OFFICE — two rows spread into DevStarts.All straight after the lower levels (#1332 A),
// so the office stands beside the floor it is on. A method, not a field, so no static initializer depends on which
// file is compiled first (#1163); a partial of its own because DevStarts.cs is held under the size gate.
public static partial class DevStarts
{
    /// <summary>#1332 C · The office's door two ways: shut, with the captain under its plate, and on the clerk's
    /// watch, with the captain two doors down the corridor. <c>&amp;office=</c> forces the door and nothing else.</summary>
    private static IReadOnlyList<Entry> ThePreservationOffice() =>
    [
        new("🏛🚪", "Ringside Exchange — the Preservation office, shut",
            "On MEMBERS' ROOMS, Ringside Exchange's hotel level, standing at the middle door of the cabin row: its "
            + $"plate reads {PreservationOffice.DoorPlate}. [E] says the office's line on every press, and the first "
            + "time files one line in the book under the Authority and Ringside Exchange (#1332).",
            "/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut"),
        new("🏛📋", "Ringside Exchange — the Preservation office, the clerk's watch",
            "The same corridor two doors down, on the clerk's watch: the office's door stands ajar, and a few "
            + "seconds in a walker plated Clerk steps out, walks to his car and rides up. Inside, a desk with one "
            + "sheet on it — [E] takes it into the sleeve; the first step in says one line, once per run (#1332).",
            "/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=open"),
    ];
}

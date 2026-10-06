namespace SpaceSails.Core;

// #1332 C/D/E · THE SIDE OFFICES — six rows spread into DevStarts.All straight after the lower levels (#1332 A), so each
// office stands beside the floor it is on: the Preservation office at Ringside Exchange (C), the forwarding desk at
// Cinder Roost (D), the adjuster's room at The Deep (E), each shut and ajar. A method, not a field, so no static
// initializer depends on which file is compiled first (#1163); a partial of its own because DevStarts.cs is held
// under the size gate.
public static partial class DevStarts
{
    /// <summary>#1332 C/D/E · Each office's door two ways: shut, with the captain under its plate, and ajar, forced
    /// whatever the watch. <c>&amp;office=</c> forces the door and nothing else.</summary>
    private static IReadOnlyList<Entry> TheSideOffices() =>
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
        new("🧊🚪", "Cinder Roost — the forwarding desk, shut",
            "On BERTH HOTEL · RESIDENTS ONLY, Cinder Roost's hotel level, standing at the second door of the cabin "
            + $"row: its plate reads {ForwardingDesk.DoorPlate}. [E] says the desk's line on every press, and the "
            + "first time files one line in the book under Plant and Cinder Roost (#1332 D).",
            "/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=shut"),
        new("🧊📋", "Cinder Roost — the forwarding desk, ajar",
            "The same door on the one watch in five it stands ajar: nobody inside, a desk with one consignment note "
            + "on it — [E] takes it into the sleeve and files it under Plant and Cinder Roost; the first step in says "
            + "one line, once per run (#1332 D).",
            "/map?dock=cinder-roost&ashore=1&havenfloor=-1&office=open"),
        new("📑🚪", "The Deep — the adjuster's room, shut",
            "On COLD ROOMS · BOOK AT THE DESK, The Deep's hotel level, standing at the fourth door of the cabin row: "
            + $"its plate reads {AdjustersRoom.DoorPlate}. [E] says the room's line on every press, and the first "
            + "time files one line in the book under Nebula Mutual and The Deep (#1332 E).",
            "/map?dock=the-deep&ashore=1&havenfloor=-1&office=shut"),
        new("📑📋", "The Deep — the adjuster's room, ajar",
            "The same door on the one watch in four it stands ajar: nobody inside, a desk with one blank claim form "
            + "on it — [E] takes it into the sleeve (a form, not evidence: the book files nothing); the first step in "
            + "says one line, once per run (#1332 E).",
            "/map?dock=the-deep&ashore=1&havenfloor=-1&office=open"),
        new("🧾✍", "The Deep — the claim, one loss from booked (#1151)",
            "The Deep's hotel level with the adjuster's door ajar on her watch, a completed sail-mend already in the "
            + "book (📋, the days it took) and the blank claim form in the sleeve. Walk back to the ship, sit at her "
            + "own desk, press ✍ to copy the loss onto the form, then come back and [E] the console on the desk (#1151).",
            "/map?claim=1"),
        new("🧾🪑", "The Deep — the interview, one press from seated (#1151)",
            "Everything the claim start stages, and the desk's and the console's work already done: the loss copied "
            + "onto the form, the claim booked (📍), the adjuster's door ajar on her watch. Walk to the desk and "
            + "[E] the console — the card seats you across from her: three questions, a paper for each, then the "
            + "outcome (#1151 slice 2).",
            "/map?claim=2"),
        new("🧾🧠", "The Deep - the interview, settling ADJUSTED (#1151 slice 3)",
            "Everything the interview start stages, with the booking's moment fixed so the real interview lands ADJUSTED whichever way the third question goes. Answer the three questions and the outcome is told - and then, once per run, the Flashback plate (A PAGE YOU DON'T REMEMBER WRITING), a dab on the nerve, and exactly one record: no signing sheet held, so the book's 📍 entry (#1151 slice 3).",
            "/map?claim=3"),
        new("🧾🖋", "The Deep - the interview, ADJUSTED, the signing sheet held (#1151 slice 3)",
            "The same start with #973's signing sheet already in the black book: the unease then grows ONE margin line onto that sheet, once ever, and files no book entry (#1151 slice 3).",
            "/map?claim=3&signing=1"),
    ];
}

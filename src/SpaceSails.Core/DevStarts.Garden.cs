namespace SpaceSails.Core;

// #1332 B · THE GARDEN BEHIND GLASS — rows spread into DevStarts.All straight after the lower levels (#1332 A),
// so the two halves of #1332 stand together in the list. A method, not a field, so no static initializer
// depends on which file is compiled first (#1163); a partial of its own because DevStarts.cs is held under the
// size gate.
public static partial class DevStarts
{
    /// <summary>#1332 B · Two gardens, one button each: the owner's own bar (The Rusty Roadstead) and the
    /// station with the walk (Selene Gate), whose garden stands north of the tube. Every haven's garden is the
    /// same room; the <c>&amp;garden=1</c> key stands the captain at THE door of whichever berth it is handed.</summary>
    private static IReadOnlyList<Entry> TheGardens() =>
    [
        Garden("🌿🍺", "The Rusty Roadstead — the garden behind glass", "the-space-bar"),
        Garden("🌿🌑", "Selene Gate — the garden behind glass", "selene-gate"),
    ];

    /// <summary>One garden row: the same blurb at every haven, because it is the same room at every haven.</summary>
    private static Entry Garden(string icon, string label, string berth) =>
        new(icon, label,
            "Ashore and walked across the concourse to the garden's door, one pace out on the hall side. Walk in "
            + $"(west-north-west): the plate {HavenGarden.Plate}, four raised beds plated in order from the door "
            + $"({string.Join(", ", HavenGarden.BedPlates)}), and a bench by the far glass — [E] sits you on "
            + "the end you walked up to, with the park bench's two moves (SIT A WHILE / Stand up). The first "
            + "step in says one line, once per station, when the slot is free. The bar's board now carries "
            + "the garden's menu line under the special (#1332).",
            $"/map?dock={berth}&ashore=1&garden=1");
}

namespace SpaceSails.Core;

/// <summary>
/// M28 · The ship's parrot 🦜 (carried PR-16). The alarm system with personality:
/// deterministic squawks triggered by game state — no randomness, the same event always
/// squawks from the same small rotation (indexed by a caller-supplied counter), so client
/// and any future server agree on every word. LLM-backed stage 2 stays future work.
/// </summary>
public static class Parrot
{
    public enum Squawk
    {
        Impact,
        DrunkDriver,
        Arcing,
        HunterNear,
        PreyInGlass,
        OffTheBooks,
        FiringSolution,
        PyramidSighted,
        RunningDark,
        FalseColors,
    }

    /// <summary>One squawk at a time, and then the bird sulks this long (real seconds).</summary>
    public const double CooldownSeconds = 20;

    /// <summary>How long a squawk stays on the bubble (real seconds).</summary>
    public const double BubbleSeconds = 6;

    private static readonly string[][] Lines =
    [
        // Impact — {0} is the body name
        ["WE'RE GOING TO CRASH — {0}!", "{0}! {0} DEAD AHEAD! SQUAWK!", "BRACE! {0} ON THE BOW!"],
        // DrunkDriver
        ["Drunk driver! Drunk driver!", "SQUAWK — who gave the helm the bottle!", "Steady as she… hic… goes!"],
        // Arcing
        ["She's glowing! SHE'S GLOWING!", "Sparks in the rigging! Vent! VENT!", "Too bright! Everyone can see us!"],
        // HunterNear
        ["Hunter on the wind!", "Wolf in the wake! SQUAWK!", "They're coming for the coin, captain!"],
        // PreyInGlass
        ["Prey in the glass!", "She's fat and slow, captain!", "Boarding weather! SQUAWK!"],
        // OffTheBooks
        ["Off the books, off the books!", "Secret sails in the sweep!", "She's not on any timetable, captain!"],
        // FiringSolution
        ["FIRING SOLUTION, CAPTAIN!", "GUNS SAY YES! SQUAWK!", "The numbers have her, captain!"],
        // PyramidSighted
        ["Old stone in the sky…", "SQUAWK… the bird does not like the triangle.", "It was here before the worms, captain."],
        // RunningDark
        ["Dark ship, quiet bird…", "Lights out, captain! SQUAWK… quietly.", "Nobody saw nothing!"],
        // FalseColors
        ["False colors on the beacon!", "The ghost flies the honest course, captain!", "Lie big, fly straight! SQUAWK!"],
    ];

    /// <summary>The squawk text for an event: a fixed rotation over that event's small line
    /// table, deterministic in the counter. <paramref name="subject"/> fills the {0} slot
    /// where a line wants a name.</summary>
    public static string Line(Squawk kind, int counter, string? subject = null)
    {
        string[] table = Lines[(int)kind];
        string line = table[((counter % table.Length) + table.Length) % table.Length];
        return subject is null ? line : string.Format(line, subject);
    }
}

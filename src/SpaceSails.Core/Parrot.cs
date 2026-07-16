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
        HunterBacksOff,
        SpaceBarBreak,
        FuelLow,
        OrbitDecay,
        ContractPaid,
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
        // HunterBacksOff — a warned or holed wolf turns tail; the bird gloats, with a wooden-leg jab
        ["SQUAWK! The wolf's got cold feet — and one of 'em's solid oak!", "He's peggin' it, captain! Hop along, timber-toes!", "One leg wood, the other WOULD if it could — he's RUNNING! SQUAWK!"],
        // SpaceBarBreak — puns for lying low at The Space Bar (sometimes you just need a break)
        ["Sometimes you just need a Space Bar break! SQUAWK!", "Pull up a stool, captain — no wolves drink here!", "Tab's open, sails are down — hit the Space Bar! SQUAWK!"],
        // FuelLow — the #166 fuel alarm gets a voice
        ["Tank's runnin' dry, captain! SQUAWK!", "Coastin' on fumes! Find a pump!", "Empty barrels rattle loudest — fuel low! SQUAWK!"],
        // OrbitDecay — the #180/#183 degradation warning, now the third founding alert
        ["She's slippin' the orbit! SQUAWK!", "The tide's got her, captain — re-park or run!", "Round and DOWN we go! Fix the orbit! SQUAWK!"],
        // ContractPaid (#185) — the bird SINGS when a job pays out. A celebration, not an alarm.
        ["🎵 A job well flown and the purse is FULL — SQUAWK, we're PAID, captain!", "🎵 Yo-ho, the hold's empty and the coin's aboard! Sing it! SQUAWK!", "🎵 Contract's DONE and the drinks are FREE — SQUAWK SQUAWK hooray!"],
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

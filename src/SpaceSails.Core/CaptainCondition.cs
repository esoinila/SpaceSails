namespace SpaceSails.Core;

/// <summary>
/// #453 · THE CAPTAIN CAN TAKE FIVE. Owner, live 2026-07-27: <i>"player health could be like 5 reever hits
/// but the reever sphere must touch the player sphere when a hit is received. Player should have some melee
/// blocking ability. Dice throw. We should narrate what happens to the player. Maybe a splash of blood when
/// reever hit goes through players attempt to block it. :-D"</i> — and <i>"Some kind of hit condition marker
/// below the nerves bar."</i>
///
/// <para>Being reached used to be a single instant with no roll in it: a touch cost nerve, and you only died
/// if the gauge was already empty. The scariest thing in the game resolved without the dice engine the whole
/// design is built on. Now a blow is an EXCHANGE — it comes in, you get something in the way of it, and the
/// die says whether that was enough.</para>
///
/// <para>This matters more since #453 took the tide's leash off and #456 gave them ears: the deep is open
/// and it can find you, so the price of being caught has to be graded rather than binary. Five blows is
/// enough room to fight to the door and lose skin doing it.</para>
///
/// <para>Pure and seeded, like every consequence in this codebase — the roll is a <see cref="DiceRule"/> d20
/// with a NAMED modifier stack, so the tray (#305) can show the player exactly why they were opened up.</para>
/// </summary>
public static class CaptainCondition
{
    /// <summary>How many landed blows a captain has in them. FLAGGED for tuning.</summary>
    public const int MaxHits = 5;

    /// <summary>A blow lands only when the two BODIES actually touch — the owner's rule: "the reever sphere
    /// must touch the player sphere". Both are <c>DeckPlan.AvatarRadius</c>, so contact is their sum.
    ///
    /// <para>#469: this is now literally <see cref="ReeverChase.CatchRadius"/> rather than a second, parallel
    /// number. There is ONE contact law on the regolith — bodies touching — and everything that keys off
    /// being reached reads the same value.</para></summary>
    public const double TouchDistance = ReeverChase.CatchRadius;

    /// <summary>Seconds an Old One takes to wind up again after a swing. Without this, a hunter held at
    /// arm's length by the pack shove (#441) would land a blow every frame.</summary>
    public const double SwingCooldownSeconds = 1.3;

    /// <summary>What the die said about one swing.</summary>
    public enum Exchange
    {
        /// <summary>Turned aside — the arm, the shovel, the shoulder. No blood, but it cost you.</summary>
        Blocked,

        /// <summary>It got past. One of the five is gone, and there is blood on the regolith.</summary>
        Hit,
    }

    /// <summary>The roll a captain makes to get something in the way. A d20 against
    /// <see cref="BlockTarget"/>, with the stack the UI can read aloud:
    /// <list type="bullet">
    /// <item>NERVE — steady hands block; a captain whose nerves are gone does not. Worth up to ±3.</item>
    /// <item>HANDS FULL — a chest in your arms is a chest not defending you.</item>
    /// <item>SWARMED — every Old One on you past the first is another thing to watch.</item>
    /// </list>
    /// Pure: same seed and same situation, same face, forever.</summary>
    public static DiceRoll BlockRoll(ulong seed, double nerve, bool carryingChest, int touchingCount)
    {
        var mods = new System.Collections.Generic.List<DiceModifier>();

        // Nerve maps to ±3 across the gauge: full is +3, empty is −3, mid is 0.
        int steady = (int)System.Math.Round(((nerve / NerveModel.Max) - 0.5) * 6.0);
        if (steady != 0)
        {
            mods.Add(new DiceModifier(steady > 0 ? "steady hands" : "nerves gone", steady));
        }
        if (carryingChest)
        {
            mods.Add(new DiceModifier("hands full — the chest", -3));
        }
        int swarm = System.Math.Max(0, touchingCount - 1);
        if (swarm > 0)
        {
            mods.Add(new DiceModifier($"swarmed ×{touchingCount}", -2 * swarm));
        }
        return DiceRule.Roll(seed, DiceRule.D20, mods);
    }

    /// <summary>Beat this and you turned it aside. Sits mid-die so a steady, unencumbered captain blocks
    /// most blows and a panicking one hugging a chest blocks almost none. FLAGGED for tuning.</summary>
    public const int BlockTarget = 11;

    /// <summary>Resolve one swing from its roll.</summary>
    public static Exchange Resolve(in DiceRoll roll) =>
        roll.Total >= BlockTarget ? Exchange.Blocked : Exchange.Hit;

    /// <summary>How the condition reads on the marker under the nerve bar. Five states, glanceable, so the
    /// decision to run is made on evidence.</summary>
    public static string Readout(int hitsTaken)
    {
        int left = System.Math.Clamp(MaxHits - hitsTaken, 0, MaxHits);
        return left switch
        {
            MaxHits => "unmarked",
            4 => "bruised",
            3 => "bleeding",
            2 => "badly cut",
            1 => "one more will do it",
            _ => "down",
        };
    }

    /// <summary>True once the captain has taken all five — the run ends here.</summary>
    public static bool IsDown(int hitsTaken) => hitsTaken >= MaxHits;

    /// <summary>What the captain got in the way, in the house voice. Seeded per swing so a long fight does
    /// not repeat itself, and so a block still reads as costly — you are being driven back.</summary>
    public static string BlockLine(ulong seed) => Pick(seed, "block", BlockLines);

    /// <summary>What it cost when the block failed. Blood, and what the blow actually did.</summary>
    public static string HitLine(ulong seed) => Pick(seed, "hit", HitLines);

    private static readonly string[] BlockLines =
    [
        "You get the shovel up in time — the blow rings off the steel and jars you back a step.",
        "You turn the arm aside with your own, and the thing shrieks close enough to fog your visor.",
        "A shoulder into it, and the weight goes past you into the regolith.",
        "You catch the swing on the pack straps. Something tears — webbing, not you.",
        "You duck, and the claw takes the air where your head was. Grit rains off your helmet.",
    ];

    private static readonly string[] HitLines =
    [
        "It gets inside your guard and opens your arm. Blood beads and freezes on the way down.",
        "The blow lands across your ribs — the suit holds, you do not. Something grinds when you breathe.",
        "Claws rake your thigh. The warm feeling is the wrong kind of warm.",
        "It catches your shoulder and turns you around. The horizon swings; the blood does not stop.",
        "You are too slow. The hand closes on you, and part of you stays with it.",
    ];

    private static string Pick(ulong seed, string tag, string[] pool) =>
        pool[(int)(DiceRule.Seed(seed, tag) % (ulong)pool.Length)];
}

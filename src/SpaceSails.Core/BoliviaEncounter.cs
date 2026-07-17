namespace SpaceSails.Core;

/// <summary>
/// PR-BUSTED · The Bolivia (owner ruling §5.3, 2026-07-17): resisting a heat-3 catch is the full last
/// stand — "one last stand with all guns blazing, Bolivia style." A short dice-scripted encounter
/// (initiative, a few choice points, mostly auto-played) that ends one of two ways:
/// <list type="bullet">
/// <item><b>FLEE</b> — you fight clear; the collector is left tied up at their own ship (that hunter
/// leaves the board) and you escape carrying heat 2.</item>
/// <item><b>THE FREEZE-FRAME</b> — a generated still in the spirit of Butch Cassidy and the Sundance
/// Kid's last charge: two silhouetted rogues bursting into blinding muzzle-flash light, sepia freeze,
/// game-over music under one massive volley — <em>an homage, not the film</em>. Death is not deletion:
/// brain-backup resurrection (the Iain M. Banks / Culture nod — forgiving, never weak) follows in the
/// client. See <see cref="BustedRule.Resurrect"/>.</list>
///
/// <para>Pure and deterministic: rolls on the shared <see cref="DiceRule"/>, seeded from the catch.
/// Runs on the tiny encounter-script shape (<see cref="EncounterBeat"/>). The choices trade small
/// modifiers — aggression for exposure — so the player steers the odds without deep-coding a
/// shooter. Net margin decides: hold the line (net &gt;= 0) and you break clear; go under and it's the
/// freeze-frame.</para>
/// </summary>
public static class BoliviaEncounter
{
    public enum Ending
    {
        /// <summary>You fought clear — the collector is tied up at their own ship; escape at heat 2.</summary>
        Flee,

        /// <summary>The sepia freeze-frame and the volley — then brain-backup resurrection.</summary>
        FreezeFrame,
    }

    /// <summary>The heat you escape with on a FLEE — one notch below the max that spawned the Bolivia
    /// (owner: "you escape carrying heat 2").</summary>
    public const int FleeHeat = 2;

    /// <summary>Net margin at or above this holds the line (FLEE); below it is the freeze-frame. Zero
    /// — a dead-even last stand still breaks clear (the house edge is already in each opposed roll's
    /// tie-to-defender).</summary>
    public const int FleeThreshold = 0;

    /// <summary>The three beats of the last stand — narration and the choices on offer. Data only;
    /// <see cref="Resolve"/> rolls them. Each choice's modifier rides that beat's opposed roll.</summary>
    public static IReadOnlyList<EncounterBeat> Script { get; } =
    [
        new EncounterBeat("breach", "The airlock buckles inward — boarders in the smoke.",
        [
            new EncounterChoice("charge", "Charge the breach", "all guns — big swing, wide open",
                new DiceModifier("charging the breach", +3)),
            new EncounterChoice("brace", "Brace the corridor", "steady, even odds",
                new DiceModifier("braced", +1)),
            new EncounterChoice("boat", "Break for the boat", "give ground, save your skin",
                new DiceModifier("running for the boat", -1)),
        ]),
        new EncounterBeat("crossfire", "Muzzle-flash fills the deck — nowhere clean to stand.",
        [
            new EncounterChoice("return", "Return fire", "trade shots in the open",
                new DiceModifier("trading fire", +2)),
            new EncounterChoice("smoke", "Smoke and move", "cover the ground you cross",
                new DiceModifier("smoke and move", 0)),
            new EncounterChoice("dig", "Dig in", "hold, but pinned",
                new DiceModifier("pinned down", -2)),
        ]),
        new EncounterBeat("run", "One clear run to their ship — and the door left open.",
        [
            new EncounterChoice("sprint", "Sprint the gap", "everything on the dash",
                new DiceModifier("all-out sprint", +2)),
            new EncounterChoice("cover", "Covering fire, then go", "safer, slower",
                new DiceModifier("covering fire", +1)),
        ]),
    ];

    /// <summary>One resolved beat: which beat, the choice taken, and the opposed roll it produced.</summary>
    public readonly record struct BeatOutcome(int BeatIndex, string ChoiceId, OpposedRoll Roll);

    /// <summary>A fully resolved Bolivia: the initiative roll, each beat's outcome, the net margin,
    /// and the ending. The client plays the beats and reveals the dice; tests read the ending.</summary>
    public readonly record struct Resolution(
        OpposedRoll Initiative, IReadOnlyList<BeatOutcome> Beats, int NetMargin, Ending Ending);

    /// <summary>Roll the opposed initiative that opens the stand — the player's standing dice helpers
    /// against the collector's heat-stiffened grip. Its margin seeds the net tally.</summary>
    public static OpposedRoll RollInitiative(ulong seed, int heat, IReadOnlyList<DiceModifier>? standingModifiers = null)
    {
        var collector = new List<DiceModifier> { new("collector's boarders (heat)", heat) };
        return DiceRule.Opposed(DiceRule.Seed(seed, "bolivia-init"), standingModifiers, collector);
    }

    /// <summary>Roll a single beat: the chosen option's modifier plus any standing helpers against the
    /// collector, salted by beat index so beats never correlate.</summary>
    public static OpposedRoll RollBeat(
        ulong seed, int beatIndex, EncounterChoice choice, int heat, IReadOnlyList<DiceModifier>? standingModifiers = null)
    {
        var playerMods = new List<DiceModifier> { choice.Modifier };
        if (standingModifiers is not null)
        {
            playerMods.AddRange(standingModifiers);
        }

        var collector = new List<DiceModifier> { new("boarders (heat)", heat) };
        return DiceRule.Opposed(DiceRule.Seed(seed, $"bolivia-beat-{beatIndex}"), playerMods, collector);
    }

    /// <summary>The ending for a net margin — the one decision rule, shared by the stepped client play
    /// and the whole-script <see cref="Resolve"/>.</summary>
    public static Ending Decide(int netMargin) => netMargin >= FleeThreshold ? Ending.Flee : Ending.FreezeFrame;

    /// <summary>
    /// Resolve the whole encounter for a chosen sequence of options (one choice id per beat, in order;
    /// a missing or unknown id auto-plays that beat's first option — "mostly auto-played"). The net
    /// margin sums the initiative and every beat's opposed margin; <see cref="Decide"/> reads the
    /// ending. Deterministic in <paramref name="seed"/>, so a given catch and a given plan always end
    /// the same way — and tests can name a seed for each ending.
    /// </summary>
    public static Resolution Resolve(
        ulong seed, int heat, IReadOnlyList<string>? choiceIds = null, IReadOnlyList<DiceModifier>? standingModifiers = null)
    {
        OpposedRoll initiative = RollInitiative(seed, heat, standingModifiers);
        int net = initiative.Margin;

        var outcomes = new List<BeatOutcome>(Script.Count);
        for (int i = 0; i < Script.Count; i++)
        {
            EncounterBeat beat = Script[i];
            EncounterChoice choice = ResolveChoice(beat, choiceIds, i);
            OpposedRoll roll = RollBeat(seed, i, choice, heat, standingModifiers);
            net += roll.Margin;
            outcomes.Add(new BeatOutcome(i, choice.Id, roll));
        }

        return new Resolution(initiative, outcomes, net, Decide(net));
    }

    private static EncounterChoice ResolveChoice(EncounterBeat beat, IReadOnlyList<string>? choiceIds, int index)
    {
        if (choiceIds is not null && index < choiceIds.Count)
        {
            foreach (EncounterChoice c in beat.Choices)
            {
                if (c.Id == choiceIds[index])
                {
                    return c;
                }
            }
        }

        return beat.Choices[0]; // auto-play the first (steadiest) option
    }
}

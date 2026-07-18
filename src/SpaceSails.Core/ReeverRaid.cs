namespace SpaceSails.Core;

/// <summary>
/// PR-295 · The 2D6 Reevers (owner, live playtest 2026-07-18): "I think it would be exciting to see
/// Reevers pop up and run back to the shuttle after burying the loot. I think Reevers generally do not
/// care for the loot at all. So they are like free watch dogs to make long term search really
/// dangerous."
///
/// <para>The watchdogs roll on the ONE shared engine (<see cref="DiceRule"/>, owner ruling §5.0 "the
/// dice are the engine") — this does not fork a new random source. It is the tabletop <b>2D6</b> the
/// owner pictured: two six-faced dice off the shared rule, salted apart, summed to a 2..12 total with
/// a NAMED modifier stack so the surface pop-up can SHOW the math. Fully deterministic in the caller's
/// seed (body + burial instant + the stash's standing watchdog level), so bury and every later
/// retrieval replay the same faces in a test — determinism is law in Core.</para>
///
/// <para><b>The watchdog economy.</b> The count that shows is the stash's danger: a burial LEAVES a
/// watchdog level equal to the pack that turned out (0..<see cref="MaxReevers"/>), stored on the
/// cache. Every later search of that ground — our own dig, or a future rival's — rolls THIS SAME 2D6
/// with the standing level added as a modifier, so a Reever-haunted moon grows more dangerous to
/// revisit, not less. That is the whole fiction: the best vault in the system with the most dangerous
/// key. The Reevers never touch the loot (there is deliberately no coin/cargo output on this rule);
/// they only decide how many hostiles the digger must sprint past.</para>
/// </summary>
public static class ReeverRaid
{
    /// <summary>The watchdogs' die: a d6, rolled twice — the tabletop "2D6" the owner named.</summary>
    public const int Faces = 6;

    /// <summary>The most Reevers a single roll ever rouses — a sprint, not a massacre.</summary>
    public const int MaxReevers = 3;

    /// <summary>Roll the 2D6 for a dig on haunted ground. <paramref name="seed"/> folds the place and
    /// the moment (the caller passes <see cref="DiceRule.Seed(string, long[])"/> over body + burial
    /// instant); <paramref name="watchdogLevel"/> is the stash's standing Reever presence, added to the
    /// roll as a named modifier so a haunted cache ropes in the pack more readily on every return. Extra
    /// <paramref name="modifiers"/> (a noisy night, a quiet approach) stack on top and stay visible.</summary>
    public static ReeverRoll Roll(ulong seed, int watchdogLevel = 0, IReadOnlyList<DiceModifier>? modifiers = null)
    {
        var stack = new List<DiceModifier>();
        if (watchdogLevel > 0)
        {
            stack.Add(new DiceModifier("haunted ground (watchdogs)", watchdogLevel));
        }
        if (modifiers is not null)
        {
            stack.AddRange(modifiers);
        }

        // Two d6 off the ONE shared rule, salted apart so the pair never correlate on a shared seed.
        int a = DiceRule.Roll(DiceRule.Seed(seed, "reever-die-a"), Faces).Face;
        int b = DiceRule.Roll(DiceRule.Seed(seed, "reever-die-b"), Faces).Face;
        return new ReeverRoll(a, b, stack, seed);
    }

    /// <summary>How many Reevers a settled 2D6 total rouses. Higher rolls wake more of the pack; a low
    /// roll is a quiet dig. The band the "watchdog economy" numbers live in: 7–8 → 1, 9–10 → 2,
    /// 11+ → the full pack, 6 or under → none.</summary>
    public static int ReeversFor(int total) => total switch
    {
        <= 6 => 0,
        <= 8 => 1,
        <= 10 => 2,
        _ => MaxReevers,
    };
}

/// <summary>
/// A settled 2D6 Reevers roll: the two natural d6 faces, the named modifier stack, and the seed it
/// came from (so a result re-verifies). Pure data — the sprint, the spawn and the reveal live
/// client-side. Mirrors the <see cref="DiceRoll"/> shape so the surface pop-up shows the same "dice +
/// math" grammar the BUSTED pop-ups do.
/// </summary>
public readonly record struct ReeverRoll(int Face1, int Face2, IReadOnlyList<DiceModifier> Modifiers, ulong Seed)
{
    /// <summary>The natural pips: the two d6 faces summed, 2..12 before modifiers.</summary>
    public int Pips => Face1 + Face2;

    /// <summary>The sum of every modifier's value (can be negative).</summary>
    public int ModifierTotal
    {
        get
        {
            int sum = 0;
            foreach (DiceModifier m in Modifiers)
            {
                sum += m.Value;
            }

            return sum;
        }
    }

    /// <summary>The pips plus every modifier — the number that decides the pack.</summary>
    public int Total => Pips + ModifierTotal;

    /// <summary>How many Reevers this roll rouses onto the surface (0..<see cref="ReeverRaid.MaxReevers"/>).</summary>
    public int Reevers => ReeverRaid.ReeversFor(Total);

    /// <summary>True when the dig woke at least one watchdog — the beat becomes a sprint.</summary>
    public bool Roused => Reevers > 0;

    /// <summary>The math, spelled out for the on-screen reveal:
    /// <c>2d6: 4 + 5 = 9  +1 (haunted ground)  = 10 → 2 Reevers</c>.</summary>
    public string Describe()
    {
        var parts = new System.Text.StringBuilder();
        parts.Append("2d6: ").Append(Face1).Append(" + ").Append(Face2).Append(" = ").Append(Pips);
        foreach (DiceModifier m in Modifiers)
        {
            parts.Append("  ").Append(m.Value >= 0 ? '+' : '−').Append(System.Math.Abs(m.Value))
                .Append(" (").Append(m.Label).Append(')');
        }

        parts.Append("  = ").Append(Total).Append(" → ").Append(Reevers)
            .Append(Reevers == 1 ? " Reever" : " Reevers");
        return parts.ToString();
    }
}

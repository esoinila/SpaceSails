namespace SpaceSails.Core;

/// <summary>
/// PR-BUSTED · The dice are the engine (owner ruling §5.0, 2026-07-17): "TTRPG mechanics — roll for
/// initiative, on-screen dice, mostly auto-played encounters with a few choices — are the DELIBERATE
/// cheap way to make playable the things we don't want to deep-code. One Core dice/modifier rule,
/// every consequence system rolls on it."
///
/// <para>This is that one rule. It is pure and fully deterministic: every roll is seeded from sim
/// state the caller passes in (never <see cref="System.Random"/> or the clock — determinism is law
/// in Core), so client and any future server agree on every face. Modifiers are a NAMED list
/// (<see cref="DiceModifier"/>) so the UI can SHOW the math — the whole point of the homage is that
/// the player watches the numbers add up. Sibling lanes (the favor bank's distress cut, future
/// consequence systems) roll on this same rule; it deliberately knows nothing about heat, coin, or
/// collectors.</para>
/// </summary>
public static class DiceRule
{
    /// <summary>The house die: a d20, the tabletop standard the homage nods to.</summary>
    public const int D20 = 20;

    /// <summary>Roll one die of <paramref name="sides"/> faces with a named modifier stack. The face
    /// is a uniform integer in [1, sides]; <see cref="DiceRoll.Total"/> adds the modifiers on top. The
    /// modifier LIST is preserved verbatim so the UI can list each line of the sum.</summary>
    public static DiceRoll Roll(ulong seed, int sides = D20, IReadOnlyList<DiceModifier>? modifiers = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 1);
        int face = new DeterministicRandom(seed).NextInt(1, sides + 1);
        return new DiceRoll($"d{sides}", face, modifiers ?? [], seed);
    }

    /// <summary>An opposed check: challenger vs. defender, each their own seeded d-<paramref name="sides"/>
    /// roll with their own modifier stack. Salted apart so the two dice never correlate on a shared seed.
    /// A tie goes to the DEFENDER (the house edge) — see <see cref="OpposedRoll.ChallengerWins"/>.</summary>
    public static OpposedRoll Opposed(
        ulong seed,
        IReadOnlyList<DiceModifier>? challengerModifiers = null,
        IReadOnlyList<DiceModifier>? defenderModifiers = null,
        int sides = D20)
    {
        DiceRoll challenger = Roll(Mix(seed, 0x1111_2222_3333_4444UL), sides, challengerModifiers);
        DiceRoll defender = Roll(Mix(seed, 0x5555_6666_7777_8888UL), sides, defenderModifiers);
        return new OpposedRoll(challenger, defender);
    }

    /// <summary>Roll a VALUE in [<paramref name="minInclusive"/>, <paramref name="maxInclusive"/>] (a
    /// bribe demand, a confiscation minimum, a fencing cut) and add the named modifiers. Returned as a
    /// <see cref="DiceRoll"/> — its natural face is the rolled amount and its <see cref="DiceRoll.Total"/>
    /// the amount after modifiers — so the on-screen math reads the same as any other roll. The total is
    /// floored at zero (a modifier stack never pays the player).</summary>
    public static DiceRoll RollAmount(
        ulong seed, int minInclusive, int maxInclusive, IReadOnlyList<DiceModifier>? modifiers = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minInclusive, maxInclusive);
        int amount = new DeterministicRandom(seed).NextInt(minInclusive, maxInclusive + 1);
        return new DiceRoll($"{minInclusive}–{maxInclusive}", amount, modifiers ?? [], seed);
    }

    /// <summary>Fold a caller's sim-state number and a purpose tag into a stable seed. Two different
    /// tags off the same state give independent streams — so the same catch can roll a bribe demand
    /// and a resist check that never move together. FNV-1a over the tag, mixed with the state.</summary>
    public static ulong Seed(ulong state, string tag)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (char c in tag)
        {
            hash ^= c;
            hash *= prime;
        }

        return Mix(state, hash);
    }

    /// <summary>Fold several sim-state numbers into one seed (e.g. a hunter-sequence id and a sim
    /// time), so a roll is reproducible from the exact moment it happened.</summary>
    public static ulong Seed(string tag, params long[] state)
    {
        ulong seed = Seed(0UL, tag);
        foreach (long s in state)
        {
            seed = Mix(seed, unchecked((ulong)s));
        }

        return seed;
    }

    // SplitMix64 finalizer over (a XOR b): a cheap, well-diffused two-input mix. Keeps every derived
    // seed as platform-stable as DeterministicRandom itself.
    private static ulong Mix(ulong a, ulong b)
    {
        ulong z = a ^ b;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}

/// <summary>One named line in a roll's modifier stack — the label the UI shows and the signed value
/// it adds. A "Boarding-nets jammer" grants <c>new DiceModifier("Boarding-nets jammer", +2)</c>; a
/// wound might be <c>new DiceModifier("Rattled", -1)</c>. Never OP (owner: "dice modifiers, never
/// OP") — the small-integer shape is the guardrail.</summary>
public readonly record struct DiceModifier(string Label, int Value);

/// <summary>
/// A settled roll: the natural die face, the named modifier stack, the total, and the seed it came
/// from (so a result can be re-verified). Pure data — the animation and the reveal live client-side.
/// </summary>
public readonly record struct DiceRoll(
    string DieLabel, int Face, IReadOnlyList<DiceModifier> Modifiers, ulong Seed)
{
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

    /// <summary>The face plus every modifier — the number that actually decides things.</summary>
    public int Total => Face + ModifierTotal;

    /// <summary>The math, spelled out for the on-screen reveal:
    /// <c>d20: 14  +2 (Boarding-nets jammer)  −1 (Rattled)  = 15</c>.</summary>
    public string Describe()
    {
        var parts = new System.Text.StringBuilder();
        parts.Append(DieLabel).Append(": ").Append(Face);
        foreach (DiceModifier m in Modifiers)
        {
            parts.Append("  ").Append(m.Value >= 0 ? '+' : '−').Append(System.Math.Abs(m.Value))
                .Append(" (").Append(m.Label).Append(')');
        }

        parts.Append("  = ").Append(Total);
        return parts.ToString();
    }
}

/// <summary>The result of an opposed check. A tie is a defender win (the house holds on a draw), so
/// <see cref="ChallengerWins"/> demands a strict edge.</summary>
public readonly record struct OpposedRoll(DiceRoll Challenger, DiceRoll Defender)
{
    /// <summary>Challenger's total over defender's — negative when the defender held.</summary>
    public int Margin => Challenger.Total - Defender.Total;

    /// <summary>The challenger beat the defender outright (ties go to the house).</summary>
    public bool ChallengerWins => Margin > 0;
}

/// <summary>One choice a scripted encounter offers at a beat: a stable id, the button label, a short
/// hint at what it trades, and the named modifier taking it grants to that beat's roll. The tiny
/// 'encounter script' shape (owner §5.0: "initiative → a few choice points → outcome") — reused by
/// the Bolivia last stand and any future dice-scripted set piece.</summary>
public readonly record struct EncounterChoice(string Id, string Label, string Hint, DiceModifier Modifier);

/// <summary>One beat of a scripted encounter: narration and the choices on offer. Data only — the
/// resolver rolls the dice, the client plays the beats.</summary>
public readonly record struct EncounterBeat(string Id, string Narration, IReadOnlyList<EncounterChoice> Choices);

namespace SpaceSails.Core;

/// <summary>
/// THE DICE TRAY's payload — the one record every dice-scripted system raises when it wants the cast
/// dice shown (#305, owner's homage: "it might be fun to show the player the dies that were cast in
/// some situations, as a homage to TTRPGs").
///
/// <para>It is deliberately system-agnostic: an <see cref="Source"/> tag (which table cast the dice),
/// the <see cref="DieLabel"/> and the <see cref="Faces"/> to show crude, the named
/// <see cref="Modifiers"/> so the tray can spell the math, the <see cref="Total"/> that decided things,
/// and the two lines of house-voice narration (<see cref="Headline"/> + <see cref="Detail"/>). The
/// aerobrake episodes raise it now; the 2D6 Reevers, BUSTED and the coming drinks-as-trust lane adopt
/// the SAME record and the SAME tray — a dice system emits a <see cref="DiceEvent"/> and the client's
/// single dice-tray component renders it, welded to nothing.</para>
///
/// <para>Pure data, built off a seeded <see cref="DicePool"/> (determinism is law in Core) — the
/// animation and the dismiss live client-side.</para>
/// </summary>
/// <param name="Source">Which table cast the dice — a short all-caps tag ("AEROBRAKE", "BUSTED").</param>
/// <param name="DieLabel">The die notation shown on the tray ("2D6", "d20").</param>
/// <param name="Faces">Every cast face, in roll order — the crude pips the tray paints.</param>
/// <param name="Modifiers">The named modifier stack, so the tray can list each line of the sum.</param>
/// <param name="Total">The face-sum plus modifiers — the number that decided the outcome.</param>
/// <param name="Headline">The episode line in the house voice (the top line of the tray).</param>
/// <param name="Detail">The consequence / fine print (the second line).</param>
public readonly record struct DiceEvent(
    string Source,
    string DieLabel,
    IReadOnlyList<int> Faces,
    IReadOnlyList<DiceModifier> Modifiers,
    int Total,
    string Headline,
    string Detail)
{
    /// <summary>Raise a tray event off a settled <see cref="DicePool"/> — the common path a dice system
    /// takes: roll a pool, pick its narration, hand the pool and the two lines here.</summary>
    public static DiceEvent FromPool(string source, DicePool pool, string headline, string detail) =>
        new(source, pool.DieLabel, pool.Faces, pool.Modifiers, pool.Total, headline, detail);

    /// <summary>The full spelled-out math for the reveal — <c>2D6: 4+5=9  −2 (load) = 7</c>.</summary>
    public string DescribeMath()
    {
        var parts = new System.Text.StringBuilder();
        parts.Append(DieLabel).Append(": ").Append(string.Join('+', Faces));
        int faceTotal = 0;
        foreach (int f in Faces)
        {
            faceTotal += f;
        }

        parts.Append('=').Append(faceTotal);
        foreach (DiceModifier m in Modifiers)
        {
            parts.Append("  ").Append(m.Value >= 0 ? '+' : '−').Append(System.Math.Abs(m.Value))
                .Append(" (").Append(m.Label).Append(')');
        }

        if (Modifiers.Count > 0)
        {
            parts.Append("  = ").Append(Total);
        }

        return parts.ToString();
    }
}

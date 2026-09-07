using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// THE REVEAL ROLL — house law: the die is shown. Which way it broke, what it paid, what it cost in
/// nerve, and how many woke up; plus #528's two plates, the sealed door with no marking on it at all and
/// the moment they stand off their benches.
///
/// <para>The family's shared pure builders sit at the foot of this file — <c>AddBox</c>, <c>Frac</c> and
/// <c>Lerp</c>, off the one dice engine. They are here because the base file put them here, after the
/// plates, and moving them would re-order the class and cost the cut its concatenate-and-diff purity
/// proof. Nothing about them is a reveal; the docblock says so rather than the filename lying.</para>
///
/// <para>Split out of <c>SecretLab.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class SecretLab
{
    // ── The reveal roll (house law: the die is shown). ──

    /// <summary>Which way the reveal broke.</summary>
    public enum RevealOutcome
    {
        /// <summary>You keep your head and strip the lab for the good stuff — a heroic pay.</summary>
        SalvageTech,

        /// <summary>It salvages YOU — the dormant thing stirs, a bigger nerve hit and a limited pack rouses.</summary>
        ItSalvagesYou,
    }

    /// <summary>A settled reveal: the raw D20 face (shown, house law), which way it broke, the salvage pay
    /// (0 unless <see cref="RevealOutcome.SalvageTech"/>), the nerve hit dealt, and the limited pack size the
    /// bad branch rouses (0 on the good branch).</summary>
    public readonly record struct RevealRoll(int Face, RevealOutcome Outcome, int PayCredits, double NerveHit, int PackSize);

    /// <summary>Roll the reveal for reading the core log (owner: "salvage the tech for pay, or it salvages
    /// you"). A single D20 (≥ <see cref="SalvageMinRoll"/> salvages), so the die reads cleanly on-screen.
    /// Fully deterministic in <paramref name="seed"/> — the client seeds it off the body + sim time.</summary>
    public static RevealRoll RollReveal(ulong seed)
    {
        int d20 = DiceRule.Roll(seed, 20).Face; // 1..20
        if (d20 >= SalvageMinRoll)
        {
            int pay = DiceRule.RollAmount(DiceRule.Seed(seed, "salvage-pay"), SalvagePayMin, SalvagePayMax).Face;
            return new RevealRoll(d20, RevealOutcome.SalvageTech, pay, RevealShock, 0);
        }
        int pack = DiceRule.RollAmount(DiceRule.Seed(seed, "wake-pack"), WakePackMin, WakePackMax).Face;
        return new RevealRoll(d20, RevealOutcome.ItSalvagesYou, 0, RevealShock + CostBranchExtraShock, pack);
    }

    // ── The two plates (#528's card, this lane's turnings) ────────────────────────────────────────────

    /// <summary>
    /// A SEALED DOOR WHERE NO DOOR HAS ANY RIGHT TO BE — the moment the ground stops being ground.
    ///
    /// <para>It is the only find in the beach-comber lane that is not a thing you pick up, and it was a
    /// pulse line. What the captain has actually done here is discover that somebody, generations ago, went
    /// to the trouble of putting a blast door under a moon and then covering it over; the decision that
    /// follows ("force it, or walk away and pretend you never found it") is one of the sharpest in the
    /// game, and it was being offered over a sentence that fades in a second and a half.</para>
    ///
    /// <para>The plate shows the door and NOTHING about what is behind it. There is no marking on it,
    /// which is the point: an unmarked door is what you get when the marking would have been the crime.
    /// </para>
    /// </summary>
    public static readonly RevealPlate DoorPlate = new(
        "A SEALED DOOR, BURIED FLUSH WITH THE REGOLITH",
        "art/lab-door-regolith.jpg",
        "Machined steel under a hand's depth of undisturbed dust, a ring of locking dogs the size of your "
        + "forearm, and not one mark, plate or stencil anywhere on it. It was not lost out here. It was put "
        + "here, and then it was covered over.");

    /// <summary>
    /// THEY ARE STANDING OFF THEIR BENCHES — the Hive's loudest moment, and it had no frame at all.
    ///
    /// <para>Raised only on <see cref="RevealOutcome.ItSalvagesYou"/>: the other branch already ends in a
    /// selfie against this room, and a card on both would be a card on a card. Because it fires strictly
    /// after the D20 has resolved and been shown, it can never be a tell — the captain already knows which
    /// way it went before the picture arrives.</para>
    ///
    /// <para>The caption obeys the ground's standing law (<c>TheHiveTests.NothingDownHereEXPLAINSAnything</c>):
    /// what you find is benches, restraints and a count. It never says what they are, and it never will.
    /// </para>
    /// </summary>
    public static readonly RevealPlate TheyStandPlate = new(
        "THEY ARE STANDING OFF THEIR BENCHES",
        "art/lab-they-stand.jpg",
        "Two rows of low steel benches with restraint cradles bolted to them, most still occupied and still "
        + "frosted over. The cradles nearest the door are open, their straps hanging, and the things that "
        + "were lying in them are on the floor with their backs to you. Nobody down here ever wrote down "
        + "what these were for.");

    // ── Builders + seeded sampling (pure, off the shared dice engine). ──

    private static void AddBox(List<SurfaceLayout.Wall> walls, double x1, double y1, double x2, double y2, bool hull)
    {
        double lox = System.Math.Min(x1, x2), hix = System.Math.Max(x1, x2);
        double loy = System.Math.Min(y1, y2), hiy = System.Math.Max(y1, y2);
        walls.Add(new(lox, loy, hix, loy, hull));
        walls.Add(new(lox, hiy, hix, hiy, hull));
        walls.Add(new(lox, loy, lox, hiy, hull));
        walls.Add(new(hix, loy, hix, hiy, hull));
    }

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"secretlab:{bodyId}:{tag}"), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}

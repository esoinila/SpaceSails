using System;
using System.IO;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #696 · THE DARKROOM. Owner, mid-run: <i>"How is our detective notebook / picture taking progressing for
/// our ability to process the files etc so we don't need carry them. That is something one would do without
/// using tanked air. It is good game mechanic... we take time to process the loot."</i>
///
/// <para>Two halves, and the second one is the whole ruling:</para>
/// <list type="number">
/// <item><b>The clock is ONE number.</b> The hold, the bar that draws it, the prompt that warns you and the
/// hint on the control all read <see cref="Processing.SecondsPerDocument"/>. A second copy is the shape this
/// repo has paid for five times.</item>
/// <item><b>The air prices the WHERE, and nothing computes it.</b> The hold passes sim time;
/// <see cref="SuitAir"/> prices sim time on whatever ground the captain chose. <i>Read it here or haul it to
/// the shelter</i> falls out of two systems that have never heard of each other, and the last three tests
/// here are about keeping them deaf.</item>
/// </list>
///
/// <para><b>Honesty about red.</b> <see cref="Processing"/> is a new class, so most of what is below pins
/// behaviour that did not exist yesterday and is not red-provable against the shipped build. What WAS watched
/// go red is named on each test that has a red to report — every one of them against the naive shape
/// transcribed into <c>Processing.cs</c> and then reverted. The client half of this lane
/// (<c>ProcessingTheLootTakesTimeTests</c>) is the part that goes red against the real previous build, and it
/// does.</para>
/// </summary>
public sealed class WeTakeTimeToProcessTheLootTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string CoreSource(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Core", file));

    /// <summary>Everything but the prose. A law about what a file may MENTION has to be a law about its
    /// code, or the comment explaining why the two systems never meet would itself break the rule.</summary>
    private static string CodeOnly(string source)
    {
        var kept = new System.Text.StringBuilder();
        foreach (string line in source.Split('\n'))
        {
            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            kept.Append(slashes >= 0 ? line[..slashes] : line).Append('\n');
        }
        return kept.ToString();
    }

    // ── THE CLOCK ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheClockIsOneNumberAndItSitsInTheOwnersBand()
    {
        // "order of ~15–30 s per document; tune by feel". A constant outside that band is not a tuning, it is
        // a different ruling — a five-second hold is a pause nobody plans around and a minute is a captain
        // who stops reading documents at all, which is the mechanic deleting the loop it was built for.
        Assert.InRange(Processing.SecondsPerDocument, 15.0, 30.0);

        // And it has to be a real slice of the tank or it prices nothing: a sixtieth of a full tank per sheet
        // means a sleeve of six worked through on open regolith is a tenth of your air. Bounded on both
        // sides, because a hold that could empty a tank on its own would be a trap rather than a decision.
        double share = Processing.SecondsPerDocument / SuitAir.TankSeconds;
        Assert.InRange(share, 1.0 / 200.0, 1.0 / 30.0);
    }

    [Fact]
    public void AHoldOfNoLengthIsFINISHEDRatherThanDividedBy()
    {
        // The ?process=0 cheat's world. Watched go RED twice against the obvious transcriptions: `Done` written
        // with `>` never finishes a zero-length hold (the story test hangs forever on a bar that will not
        // fill), and `Fraction` written as a bare elapsed/seconds answers NaN, which the renderer clamps to
        // nothing and draws as an empty bar that never moves. Both are silent.
        Assert.True(Processing.Done(0.0, 0.0));
        Assert.Equal(1.0, Processing.Fraction(0.0, 0.0));
        Assert.Equal(0.0, Processing.SecondsLeft(0.0, 0.0));
    }

    [Fact]
    public void TheBarFillingAndTheEffectFiringAgreeOnWhatFINISHEDMeans()
    {
        double s = Processing.SecondsPerDocument;

        Assert.False(Processing.Done(s - 0.001, s));
        Assert.True(Processing.Done(s, s));
        Assert.True(Processing.Done(s + 5, s));

        // The bar never overruns and never runs backwards — a captain watching a full bar that has not fired
        // is watching a bug, and the two answers come from one clock so they cannot disagree.
        Assert.Equal(1.0, Processing.Fraction(s, s));
        Assert.Equal(1.0, Processing.Fraction(s * 3, s));
        Assert.Equal(0.0, Processing.Fraction(-4, s));
        Assert.Equal(0.5, Processing.Fraction(s / 2, s), 9);

        Assert.Equal(0.0, Processing.SecondsLeft(s + 9, s));
        Assert.Equal(s, Processing.SecondsLeft(0, s));
    }

    // ── STANDING STILL ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StandingStillToleratesANudgeAndNeverAStep()
    {
        double t = Processing.StandingToleranceDu;

        Assert.False(Processing.Wandered(100, 100, 100, 100));
        Assert.False(Processing.Wandered(100, 100, 100 + t, 100));           // exactly the edge is still here
        Assert.True(Processing.Wandered(100, 100, 100 + t + 0.001, 100));    // and a hair past it is not

        // The circle, not the square. A tolerance tested only on the axes is the classic version of this
        // bug: drift the tolerance along BOTH axes and a box-shaped check says you never moved, while the
        // captain has walked 1.4 tolerances diagonally away from the paper on the floor. Watched go RED
        // against a per-axis transcription (|dx| <= t && |dy| <= t).
        Assert.True(Processing.Wandered(0, 0, t, t));

        // And half a second of an ordinary walk is out of the question — the surface walk is 9 du/s, so any
        // deliberate movement at all ends the hold. That is the mechanic, not an accident of tuning.
        Assert.True(Processing.Wandered(0, 0, 4.5, 0));
        Assert.True(t < 9.0 * 0.5);
    }

    // ── WHAT THE CAPTAIN IS TOLD ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheStartLineNamesThePriceBEFOREThePress()
    {
        // The decision the whole mechanic exists for is "here, or somewhere with air in it", and it cannot be
        // taken by somebody who learns the cost after committing to it.
        foreach (Processing.Work work in Enum.GetValues<Processing.Work>())
        {
            string said = Processing.StartLine(work, "📋 a pay sheet", Processing.SecondsPerDocument);
            Assert.Contains("📋 a pay sheet", said, StringComparison.Ordinal);
            Assert.Contains($"{Processing.SecondsPerDocument:F0} seconds", said, StringComparison.Ordinal);

            // The tracker keeps running through the hold and the line says to watch it. That sentence IS the
            // teeth — there is no new enemy in this feature, only a reason to be standing still.
            Assert.Contains("fan", said, StringComparison.OrdinalIgnoreCase);

            // With the clock off there is no clock to name. A cheat that made the game promise "0 seconds of
            // standing still" would read as a broken build to whoever inherits the URL.
            Assert.DoesNotContain("0 seconds", Processing.StartLine(work, "x", 0), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EveryInterruptionPromisesTheWorldIsExactlyAsItWasFound()
    {
        // Nothing filed, nothing consumed, the paper still in the pocket — and SAID, because twenty seconds
        // that evaporate in silence read as a lost press rather than as a decision the world took from you.
        foreach (Processing.Work work in Enum.GetValues<Processing.Work>())
        {
            foreach (Processing.Interruption why in Enum.GetValues<Processing.Interruption>())
            {
                string said = Processing.AbandonedLine(work, "📋 a shipping manifest", why);
                Assert.Contains("📋 a shipping manifest", said, StringComparison.Ordinal);

                // It still has the paper…
                Assert.Contains("sleeve", said, StringComparison.OrdinalIgnoreCase);
                // …and it is honest that it got nothing for the time.
                Assert.Contains("nothing", said, StringComparison.OrdinalIgnoreCase);
            }
        }

        // The four are four SENTENCES and not one sentence with a switch on the end: what took the paper out
        // of your hands is the only part of an abandoned hold worth remembering.
        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (Processing.Interruption why in Enum.GetValues<Processing.Interruption>())
        {
            Assert.True(seen.Add(Processing.AbandonedLine(Processing.Work.File, "x", why)),
                $"two interruptions answer with the same line — {why} says nothing the others do not (#696).");
        }
    }

    [Fact]
    public void TheHintAndTheHoldReadTheSameNumber()
    {
        // The hint on the leave control is the ONE place a number can drift away from the clock, because it
        // is the only one a razor could plausibly hard-code. It is composed here, from the value it is
        // handed, so a satchel that promised twenty seconds of a fifteen-second hold is not expressible.
        string hint = Processing.LeaveHint(Processing.SecondsPerDocument);
        Assert.Contains($"{Processing.SecondsPerDocument:F0} s", hint, StringComparison.Ordinal);

        // Clock off, no promise about a clock.
        Assert.DoesNotContain("s standing still", Processing.LeaveHint(0), StringComparison.Ordinal);

        // And the hold line counts DOWN, so the captain can decide whether they have time for the rest of it.
        string mid = Processing.HoldLine(Processing.Work.File, "📋 a pay sheet", 7);
        Assert.Contains("7 s left", mid, StringComparison.Ordinal);
    }

    // ── AND THE PART THAT IS NOT COMPUTED ANYWHERE ─────────────────────────────────────────────────────

    [Fact]
    public void ProcessingWhereTheAirIsCostsONLYTime()
    {
        // Owner: "That is something one would do without using tanked air." Not a rule anybody wrote — a
        // consequence. Every pressurised place the game already knows about answers Room or Ship, the drain
        // is gated on Drawing, and so a hold of any length spends nothing.
        double rate = SuitAir.Breathing.Rate(SuitAir.Breathing.Still, 100, 0, CaptainCondition.MaxHits);
        double tank = 900.0;

        (string what, SuitAir.Supply supply)[] free =
        [
            ("B1 — a floor of the Hive that still holds pressure (#585)",
                SuitAir.SourceOf("miranda", -1, insideShelter: false, aboard: false)),
            ("a pressure refuge cut into a dead floor (#608)",
                SuitAir.SourceOf("miranda", -2, insideShelter: false, aboard: false, inRefuge: true)),
            ("an emergency shelter on the surface (#573)",
                SuitAir.SourceOf("miranda", 0, insideShelter: true, aboard: false)),
            ("aboard her, or in her tube (#562)",
                SuitAir.SourceOf("miranda", 0, insideShelter: false, aboard: true)),
        ];

        foreach ((string what, SuitAir.Supply supply) in free)
        {
            Assert.False(SuitAir.Drawing(supply),
                $"the tank is running in {what} — the darkroom would be priced there, and the whole 'read it " +
                "here or haul it to safety' decision collapses (#696).");

            // The drain is what the sim skips when Drawing is false. Stated as arithmetic so this test says
            // what the hold COSTS and not merely which enum came back.
            double after = SuitAir.Drawing(supply)
                ? SuitAir.Drain(tank, Processing.SecondsPerDocument * rate)
                : tank;
            Assert.Equal(tank, after);
        }
    }

    [Fact]
    public void ProcessingOnOpenRegolithCostsEXACTLYTheHold()
    {
        // The other side of the same non-rule: out here the tank is running, so the paperwork is on the bill.
        // Standing still is the cheapest thing a captain can do (Breathing.Still, 0.8), which is why the hold
        // is a bite and not a catastrophe: twenty seconds of held position is sixteen seconds of tank.
        double rate = SuitAir.Breathing.Rate(SuitAir.Breathing.Still, 100, 0, CaptainCondition.MaxHits);
        SuitAir.Supply supply = SuitAir.SourceOf("miranda", 0, insideShelter: false, aboard: false);
        Assert.True(SuitAir.Drawing(supply));

        double tank = 900.0;
        double after = SuitAir.Drain(tank, Processing.SecondsPerDocument * rate);
        Assert.Equal(tank - (Processing.SecondsPerDocument * rate), after, 9);
        Assert.Equal(16.0, Processing.SecondsPerDocument * rate, 9);

        // A frightened, hurt captain pays more for the same twenty seconds, because the rate is the captain's
        // and the hold only supplies the seconds. Nothing in Processing knows this is happening.
        double panicking = SuitAir.Breathing.Rate(SuitAir.Breathing.Still, 5, CaptainCondition.MaxHits - 1,
            CaptainCondition.MaxHits);
        Assert.True(panicking > rate);
    }

    [Fact]
    public void TheSUITAndTheDARKROOMHaveNeverHeardOfEachOther()
    {
        // THE LAW THIS LANE IS BUILT ON, in the only form that can be enforced: neither file may name the
        // other. The instant Processing learns to charge a tank there are two answers to "what does standing
        // still cost", and the second one will be the one that is wrong on the floor where the first was
        // right. And the instant SuitAir learns about a hold, the walk-back arithmetic — the one number a
        // captain plans their life around — starts having opinions about their paperwork habits.
        //
        // Watched go RED both ways: a `SuitAir.Drain` inside the hold, and a `Processing.SecondsPerDocument`
        // term in NeededToGetHome.
        string darkroom = CodeOnly(CoreSource("Processing.cs"));
        foreach (string forbidden in new[] { "SuitAir", "AirSeconds", "TankSeconds", "Drain(", "Tanks" })
        {
            Assert.False(darkroom.Contains(forbidden, StringComparison.Ordinal),
                $"Processing.cs mentions `{forbidden}` in code — the hold has learned to price itself, and " +
                "the air sim is no longer the one place that knows what a second costs (#696).");
        }

        string suit = CodeOnly(CoreSource("SuitAir.cs"));
        Assert.False(suit.Contains("Processing", StringComparison.Ordinal),
            "SuitAir.cs mentions Processing — the point-of-no-return arithmetic now knows about the " +
            "captain's paperwork, which is exactly the coupling #696 was ruled to avoid.");
    }
}

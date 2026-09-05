using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #167 · A BURN MUST BE FELT — and the flame goes the OTHER WAY.
///
/// <para>Owner (2026-07-16): <i>"Now we should have some burn-happening sound and visual effect also."</i>
/// The issue's own three bullets are the three laws below: the picture is scaled by the pulses spent, it
/// leaves the stern opposite the push, and it lives on the WALL clock so that a burn which passes in one
/// frame at 10,000× warp still reads.</para>
///
/// <para>These guards bite on <see cref="BurnPlume"/> — the pure geometry the renderer walks — for the
/// reason <c>ThePlumeLeavesHerMastTests</c> gives about its own type: a picture built out of client
/// literals is this repo's first named bug class, and a picture that disagrees with the sim is its third.
/// The WIRING (that every burn kind actually reaches the pen, and makes a noise once) is Client.Tests'
/// <c>EveryBurnIsFeltTests</c>; nothing here asserts that any code calls this.</para>
/// </summary>
public sealed class TheExhaustLeavesHerSternTests
{
    // ── LAW ONE · NOTHING BURNED IS NOTHING DRAWN ─────────────────────────────────────────────────────

    /// <summary>
    /// A ship that has not burned draws NOTHING — not a dim flame, nothing. Three ways to have not burned:
    /// no pulses, a burn still in the future, and a burn whose window has closed (including the
    /// <see cref="double.NegativeInfinity"/> a ship that has never fired starts from).
    ///
    /// <para>ANTI-VACUOUS: the same row asserts that a real burn DOES draw, so a <c>Shape</c> that returned
    /// <see cref="BurnPlume.None"/> for everything could not pass this.</para>
    ///
    /// <para>RED PROOF (and it takes two edits, because the type guards this twice — deliberately): cut
    /// <c>Shape</c>'s opening test down to <c>IsNaN || &lt; 0</c> AND floor the size at
    /// <c>Math.Max(0.15, Brightness(pulses))</c> — the "make sure a burn is always visible" mistake. The
    /// row then fails with a flame on a ship that fired nothing and a plume a day after the burn.</para>
    /// </summary>
    [Fact]
    public void AShipThatHasNotBurnedDrawsNothing()
    {
        foreach (double age in new[] { 0.0, 1.0, 250.0, BurnPlume.FlashMs - 1 })
        {
            Assert.Equal(BurnPlume.None, BurnPlume.Shape(0, age));
            Assert.Equal(BurnPlume.None, BurnPlume.Shape(-3, age));
        }

        // Not yet, and no longer.
        Assert.Equal(BurnPlume.None, BurnPlume.Shape(12, -1.0));
        Assert.Equal(BurnPlume.None, BurnPlume.Shape(12, BurnPlume.FlashMs));
        Assert.Equal(BurnPlume.None, BurnPlume.Shape(12, 86_400_000.0));

        // A ship that has never fired: the age a caller computes from a NegativeInfinity stamp.
        Assert.Equal(BurnPlume.None, BurnPlume.Shape(12, double.PositiveInfinity));
        Assert.Equal(BurnPlume.None, BurnPlume.Shape(12, double.NaN));

        // …and the world can tell pass from fail: one real pulse, this instant, draws.
        Assert.True(BurnPlume.Shape(1, 0.0).Draws, "a one-pulse trim must still read as a burn");
        Assert.True(BurnPlume.Shape(40, BurnPlume.FlashMs - 1).Draws,
                    "the last millisecond of the window is still inside it");
    }

    // ── LAW TWO · SCALED BY THE PULSES SPENT ──────────────────────────────────────────────────────────

    /// <summary>
    /// THE ISSUE'S FIRST BULLET: <i>"a brief thruster plume/flash on the ship marker at the burn instant …
    /// scaled by pulses spent."</i> Bigger burn, bigger flame — intensity, reach and feather count all
    /// monotone in pulses, at a fixed age, and strictly bigger between a trim and an insertion.
    ///
    /// <para>ANTI-VACUOUS: monotone alone is satisfied by a constant, so the row also demands that one
    /// pulse and forty are actually DIFFERENT sizes, in every one of the three.</para>
    ///
    /// <para>RED PROOF: make <c>Brightness</c> return a constant 1.0 — "every burn is a full flame" — and
    /// the row fails on the strict half, a one-pulse trim and a forty-pulse insertion at the same
    /// intensity.</para>
    /// </summary>
    [Fact]
    public void ABiggerBurnIsABiggerFlame()
    {
        const double age = 0.0;

        BurnPlume.Plume previous = BurnPlume.Shape(1, age);
        for (int pulses = 2; pulses <= BurnPlume.FullPulses * 2; pulses++)
        {
            BurnPlume.Plume now = BurnPlume.Shape(pulses, age);
            Assert.True(now.Intensity >= previous.Intensity,
                $"{pulses} pulses burn dimmer ({now.Intensity:F4}) than {pulses - 1} ({previous.Intensity:F4})");
            Assert.True(now.ReachPx >= previous.ReachPx,
                $"{pulses} pulses reach less far ({now.ReachPx:F3}) than {pulses - 1} ({previous.ReachPx:F3})");
            Assert.True(now.Feathers >= previous.Feathers,
                $"{pulses} pulses draw fewer feathers ({now.Feathers}) than {pulses - 1} ({previous.Feathers})");
            previous = now;
        }

        BurnPlume.Plume trim = BurnPlume.Shape(1, age);
        BurnPlume.Plume insertion = BurnPlume.Shape(BurnPlume.FullPulses, age);
        Assert.True(insertion.Intensity > trim.Intensity, "a full insertion must not look like a one-pulse trim");
        Assert.True(insertion.ReachPx > trim.ReachPx, "…nor reach the same distance");
        Assert.True(insertion.Feathers > trim.Feathers, "…nor draw the same number of feathers");

        // The ear is scaled by the SAME number as the eye — one burn, one size, told twice.
        Assert.True(BurnPlume.CueScale(BurnPlume.FullPulses) > BurnPlume.CueScale(1));
        Assert.Equal(BurnPlume.Brightness(7), BurnPlume.CueScale(7));
        Assert.Equal(0.0, BurnPlume.CueScale(0));
    }

    // ── LAW THREE · THE FLAME LEAVES THE STERN, OPPOSITE THE PUSH ─────────────────────────────────────

    /// <summary>
    /// THE PHYSICS. A drive that pushes the ship one way throws its mass the other, so every feather leaves
    /// a nozzle <see cref="BurnPlume.NozzlePx"/> off the marker on the side AWAY from the burn, and every
    /// point of every feather lies aft of the marker along that axis.
    ///
    /// <para>Checked all the way round the compass and at both a trim and an insertion, because a sign
    /// error in one quadrant is exactly the mistake this exists to catch. "Aft" is stated as a projection
    /// onto the exhaust axis being positive — which is only guaranteed because
    /// <see cref="BurnPlume.SpreadRad"/> + <see cref="BurnPlume.KinkRad"/> stays under a right angle, so
    /// the row also states that.</para>
    ///
    /// <para>RED PROOF: drop the <c>+ Math.PI</c> from <c>ExhaustAngle</c> and every feather fails, naming
    /// a projection of −7 px onto an axis it should be +7 px along: the flame is drawn on the bow.</para>
    /// </summary>
    [Fact]
    public void EveryFeatherLeavesTheNozzleAndTravelsAft()
    {
        Assert.True(BurnPlume.SpreadRad + BurnPlume.KinkRad < Math.PI / 2,
                    "the fan can reach past the beam — 'aft' would stop being true of every feather");

        foreach (int pulses in new[] { 1, 12, BurnPlume.FullPulses })
        {
            for (int deg = 0; deg < 360; deg += 15)
            {
                double thrust = deg * Math.PI / 180.0;
                BurnPlume.Plume plume = BurnPlume.Shape(pulses, 0.0);
                double[] feathers = Feathers(plume, thrust, phase: 3.4);

                Assert.Equal(plume.Feathers * BurnPlume.FloatsPerFeather, feathers.Length);

                // THE AXIS IS THE BENCH'S OWN, and that is the whole point of the row. Reading it back out
                // of BurnPlume.ExhaustAngle would hand this guard a world that cannot tell pass from fail:
                // strip the turn through π and the flame would swing to the bow WITH the yardstick, and
                // every assertion below would still be true of it. So "aft" is stated here, once, as the
                // opposite of where the burn pushes — and ExhaustAngle is then measured AGAINST it.
                double ax = -Math.Cos(thrust);
                double ay = -Math.Sin(thrust);
                Assert.Equal(1.0,
                    (Math.Cos(BurnPlume.ExhaustAngle(thrust)) * ax) + (Math.Sin(BurnPlume.ExhaustAngle(thrust)) * ay),
                    9);

                (double nozzleX, double nozzleY) = BurnPlume.Nozzle(thrust);

                Assert.Equal(BurnPlume.NozzlePx, Math.Sqrt((nozzleX * nozzleX) + (nozzleY * nozzleY)), 9);
                Assert.Equal(BurnPlume.NozzlePx, (nozzleX * ax) + (nozzleY * ay), 9);

                for (int i = 0; i < feathers.Length; i += BurnPlume.FloatsPerFeather)
                {
                    // Every feather starts at the nozzle — one drive, one flame, not a spray.
                    Assert.Equal(nozzleX, feathers[i], 9);
                    Assert.Equal(nozzleY, feathers[i + 1], 9);

                    for (int p = 0; p < BurnPlume.FloatsPerFeather; p += 2)
                    {
                        double along = (feathers[i + p] * ax) + (feathers[i + p + 1] * ay);
                        Assert.True(along >= BurnPlume.NozzlePx - 1e-9,
                            $"a burn at {deg}° put a flame point {along:F3} px along its own exhaust axis, "
                            + $"and the nozzle is at {BurnPlume.NozzlePx}. It is drawing on the bow.");
                    }
                }
            }
        }
    }

    // ── LAW FOUR · THE WALL CLOCK, AND ONLY THE WALL CLOCK ────────────────────────────────────────────

    /// <summary>
    /// THE ISSUE'S THIRD BULLET: <i>"At high warp a burn instant can pass in one frame — the effect should
    /// be wall-clock-timed (~1 s) … so it reads at any warp."</i> The window is about a second of REAL
    /// time, the flame fades monotonically across it, and it is gone by the end of it.
    ///
    /// <para>#1086's two-clock rule is the reason there is nothing else to check: a burn is entirely a
    /// flash, so no term in this type reads sim time at all. The Client guard is where "and at 10,000×
    /// warp it still draws" is proved against a real map.</para>
    ///
    /// <para>RED PROOF: take the decay out — <c>double intensity = size;</c> — and the row fails at the
    /// first step, 0 ms to 25 ms, on a flame that never dims.</para>
    /// </summary>
    [Fact]
    public void TheFlameFadesOutAcrossAboutASecondOfRealTime()
    {
        Assert.InRange(BurnPlume.FlashMs, 700.0, 1500.0);   // "~1 s", stated where a retune would meet it

        BurnPlume.Plume previous = BurnPlume.Shape(20, 0.0);
        Assert.True(previous.Draws, "the burn instant itself must draw");

        for (double age = 25.0; age < BurnPlume.FlashMs; age += 25.0)
        {
            BurnPlume.Plume now = BurnPlume.Shape(20, age);
            Assert.True(now.Intensity < previous.Intensity,
                $"the flame did not fade between {age - 25} ms and {age} ms ({now.Intensity:F5})");
            previous = now;
        }

        Assert.False(BurnPlume.Shape(20, BurnPlume.FlashMs).Draws,
                     "the window closed and the flame is still on the glass");
        Assert.False(BurnPlume.Shape(BurnPlume.FullPulses * 10, BurnPlume.FlashMs).Draws,
                     "…and the biggest burn there is does not outlast its own window either");
    }

    // ── LAW FIVE · THE SAME INSTANT DRAWS THE SAME FLAME ──────────────────────────────────────────────

    /// <summary>
    /// Deterministic, and not merely deterministic-in-this-process. The same arguments give the same floats
    /// every time; a different flutter step gives different ones; and the mix is arithmetic this file owns
    /// rather than <c>System.HashCode</c>'s per-process seed, so the picture is reproducible across runs —
    /// which is what lets a frame carrying a burn be pinned in a fingerprint ledger at all.
    ///
    /// <para>ANTI-VACUOUS: a <c>Feathers</c> that wrote zeros would be perfectly stable, so the row also
    /// demands that the flutter MOVES the flame from one step to the next.</para>
    ///
    /// <para>RED PROOF: make <c>Flutter</c> ignore its step (<c>(ulong)0 * …</c>) and the row fails — the
    /// flame is frozen for the whole second. Seeding the mix with <c>HashCode.Combine(step, i)</c> instead
    /// fails the pinned numbers below on (nearly) every run, which is the cross-process half.</para>
    /// </summary>
    [Fact]
    public void TheSameInstantDrawsTheSameFlame()
    {
        BurnPlume.Plume plume = BurnPlume.Shape(9, 120.0);

        double[] once = Feathers(plume, 0.7, 1.33);
        double[] again = Feathers(plume, 0.7, 1.33);
        Assert.Equal(once, again);

        // …and holding still WITHIN a step: a burn snaps and flutters, it does not sweep.
        Assert.Equal(once, Feathers(plume, 0.7, 1.95));

        double[] nextStep = Feathers(plume, 0.7, 2.05);
        Assert.NotEqual(once, nextStep);
        Assert.True(FarthestApart(once, nextStep) > 0.1,
                    "the flutter moved the flame by less than a tenth of a pixel — it is not fluttering");

        // THE CROSS-PROCESS CLAIM, pinned. These are this file's own arithmetic, so they are the same
        // numbers tomorrow; a System.HashCode seed would make them the same numbers only until the next run.
        double[] pinned = Feathers(BurnPlume.Shape(16, 0.0), 0.0, 0.0);
        Assert.Equal(
            "-7.000,0.000,-13.535,0.156,-20.044,0.750|"
            + "-7.000,0.000,-15.059,-3.283,-22.134,-8.350|"
            + "-7.000,0.000,-12.781,2.925,-18.491,5.984|"
            + "-7.000,0.000,-13.821,-3.173,-20.528,-6.579|"
            + "-7.000,0.000,-15.157,2.134,-22.839,5.609",
            string.Join('|', Enumerable.Range(0, pinned.Length / BurnPlume.FloatsPerFeather)
                .Select(f => string.Join(',', pinned
                    .Skip(f * BurnPlume.FloatsPerFeather)
                    .Take(BurnPlume.FloatsPerFeather)
                    .Select(v => v.ToString("F3", System.Globalization.CultureInfo.InvariantCulture))))));
    }

    // ── LAW SIX · THE BEAT ALONG THE PATH AHEAD ───────────────────────────────────────────────────────

    /// <summary>
    /// THE ISSUE, again: <i>"the plotted path segment ahead re-tints for a beat."</i> A segment, not the
    /// whole ribbon — and never more vertices than the ribbon has, which is the buffer overrun a fraction
    /// of a length invites.
    ///
    /// <para>RED PROOF: return <c>vertexCount</c> from <c>BeatVertices</c> — re-tint the whole ribbon —
    /// and the row fails naming 40 vertices of 40, "that is not a segment".</para>
    /// </summary>
    [Fact]
    public void TheBeatCoversTheStretchAheadAndNoMore()
    {
        Assert.InRange(BurnPlume.BeatPathFraction, 0.05, 0.5);

        Assert.Equal(0, BurnPlume.BeatVertices(0));
        Assert.Equal(0, BurnPlume.BeatVertices(1));

        foreach (int count in new[] { 2, 3, 9, 40, 160, 4000 })
        {
            int beat = BurnPlume.BeatVertices(count);
            Assert.InRange(beat, 2, count);
            Assert.True(beat < count || count <= 9,
                        $"a ribbon of {count} vertices re-tinted {beat} of them — that is not a segment");
        }

        // The wash fades with the flame, on one timetable rather than two.
        Assert.True(BurnPlume.BeatAlpha(BurnPlume.Shape(20, 0.0))
                    > BurnPlume.BeatAlpha(BurnPlume.Shape(20, BurnPlume.FlashMs - 1)));
        Assert.Equal(0, BurnPlume.BeatAlpha(BurnPlume.None));
    }

    // ── LAW SEVEN · A BUFFER TOO SMALL IS AN ERROR, NOT HALF A FLAME ──────────────────────────────────

    /// <summary>The pen's contract: <see cref="BurnPlume.MaxFeathers"/> is genuinely the most any plume
    /// asks for, and a caller who sizes its span smaller is told so rather than drawing half a flame
    /// somebody would have to explain later.
    ///
    /// <para>RED PROOF: delete the <c>into.Length &lt; need</c> throw from <c>Feathers</c> and the row fails
    /// with an <c>IndexOutOfRangeException</c> where it asked for an <c>ArgumentException</c> — half a
    /// flame, written past the end of a caller's span.</para></summary>
    [Fact]
    public void TheBiggestPlumeFitsExactlyInTheAdvertisedBuffer()
    {
        for (int pulses = 1; pulses <= BurnPlume.FullPulses * 4; pulses++)
        {
            Assert.InRange(BurnPlume.Shape(pulses, 0.0).Feathers, BurnPlume.MinFeathers, BurnPlume.MaxFeathers);
        }

        BurnPlume.Plume biggest = BurnPlume.Shape(BurnPlume.FullPulses, 0.0);
        Assert.Equal(BurnPlume.MaxFeathers, biggest.Feathers);
        Assert.Throws<ArgumentException>(() =>
        {
            double[] tooSmall = new double[(BurnPlume.MaxFeathers * BurnPlume.FloatsPerFeather) - 1];
            BurnPlume.Feathers(biggest, 0.0, 0.0, tooSmall);
        });

        // Nothing drawing writes nothing — the pen's early-out, stated.
        double[] room = new double[BurnPlume.MaxFeathers * BurnPlume.FloatsPerFeather];
        Assert.Equal(0, BurnPlume.Feathers(BurnPlume.None, 0.0, 0.0, room));
    }

    // ── plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private static double[] Feathers(in BurnPlume.Plume plume, double thrustAngle, double phase)
    {
        double[] into = new double[BurnPlume.MaxFeathers * BurnPlume.FloatsPerFeather];
        int n = BurnPlume.Feathers(plume, thrustAngle, phase, into);
        return into[..n];
    }

    /// <summary>The largest distance any one written coordinate moved between two draws.</summary>
    private static double FarthestApart(double[] a, double[] b)
    {
        Assert.Equal(a.Length, b.Length);
        double worst = 0;
        for (int i = 0; i < a.Length; i++)
        {
            worst = Math.Max(worst, Math.Abs(a[i] - b[i]));
        }
        return worst;
    }
}

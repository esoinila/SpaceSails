using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #528 §7 · THE DISCHARGE IS A PLUME OFF HER MAST, NEVER A BALL ROUND HER HULL.
///
/// <para>Owner, on the charge board (#523): <i>"the charge is being equalized could have plasma ball like
/// beautifull effect if physics supports it."</i> It supports something better, and the issue said so: field
/// strength is potential over radius of curvature, so a discharge leaves the SHARPEST THING SHE HAS. A halo
/// about the hull is the one shape it can never be.</para>
///
/// <para>These guards bite on <see cref="DischargePlume"/> — the pure geometry the renderer walks — because a
/// picture built out of client literals is this repo's first named bug class, and a picture that disagrees with
/// the sim is its third. Every law below is asked of the same function the pen calls.</para>
/// </summary>
public sealed class ThePlumeLeavesHerMastTests
{
    // ── LAW ONE · NOTHING TO SHOW IS NOTHING DRAWN ────────────────────────────────────────────────────

    /// <summary>
    /// A hull that has dumped nothing and is not arcing draws NOTHING — not a dim plume, nothing. Charge is
    /// not discharge: she can be a lantern on the board and have nothing leaving her, and a glow drawn for a
    /// merely-charged hull would make the map say a thing is happening that is not.
    ///
    /// <para>ANTI-VACUOUS on both sides: the same sweep asserts the two states that DO draw, so a
    /// <c>Shape</c> that returned <see cref="DischargePlume.None"/> for everything could not pass this.</para>
    ///
    /// <para>RED PROOF: give the quiet bands the arcing simmer (<c>double simmer = ArcingSimmer;</c>
    /// unconditionally) and the quiet half fails on QUIET, RISING and GLOWING at every age.</para>
    /// </summary>
    [Fact]
    public void AHullThatDumpedNothingAndIsNotArcingDrawsNothing()
    {
        HullCharge.Band[] quiet =
            [HullCharge.Band.Quiet, HullCharge.Band.Rising, HullCharge.Band.Glowing];

        foreach (HullCharge.Band band in quiet)
        {
            foreach (double age in new[] { 0.0, 1.0, 300.0, DischargePlume.FlashMs, 5_000.0 })
            {
                DischargePlume.Plume plume = DischargePlume.Shape(0.0, band, age);
                Assert.False(plume.Draws,
                    $"{band} with nothing dumped {age} ms ago still draws — intensity {plume.Intensity:F4}, " +
                    $"{plume.Filaments} filaments. A charged hull is not a discharging hull.");
                Assert.Equal(DischargePlume.None, plume);
            }
        }

        // …and the two states that DO draw, so the row above is telling one from the other.
        Assert.True(DischargePlume.Shape(0.0, HullCharge.Band.Arcing, 99_999.0).Draws,
            "an ARCING hull draws no crawl at all — the band would then be unreadable from the map, which is " +
            "the whole point of the crawl.");
        Assert.True(DischargePlume.Shape(0.8, HullCharge.Band.Quiet, 0.0).Draws,
            "a fresh dump off a quiet hull draws nothing — this guard cannot tell a plume from no plume.");
    }

    /// <summary>A dump of NOTHING is still nothing, however fresh: halving a cold hull sheds nothing and there
    /// is nothing to light. (The vent halves her charge, so this is the state a captain reaches by dumping
    /// twice in a row.)</summary>
    [Fact]
    public void DumpingAColdHullLightsNothing()
    {
        Assert.False(DischargePlume.Shape(0.0, HullCharge.Band.Quiet, 0.0).Draws);
        Assert.Equal(0.0, DischargePlume.DumpBrightness(0.0));
    }

    // ── LAW TWO · BRIGHTNESS IS HOW MUCH SHE LET GO OF, ON THE SENSORS' OWN SCALE ─────────────────────

    /// <summary>
    /// The flash gets brighter the more charge actually left her, and it is MONOTONE — never a wobble, never a
    /// plateau across the range.
    ///
    /// <para>ANTI-VACUOUS: the sweep's ends are asserted to be far apart before the monotonicity is checked, so
    /// a constant intensity (which is trivially non-decreasing) fails here rather than passing.</para>
    ///
    /// <para>RED PROOF: make the flash <c>1.0 - sinceDumpMs / FlashMs</c> with no brightness term — which is
    /// what the code did before this lane — and the spread assertion fails at once: every dump, from a hair to
    /// a full hull, lights the same picture.</para>
    /// </summary>
    [Fact]
    public void TheFlashIsBrighterTheMoreSheLetGoOf()
    {
        double[] dumped = [.. Enumerable.Range(0, 51).Select(i => i / 50.0)];
        double[] intensity = [.. dumped.Select(d => DischargePlume.Shape(d, HullCharge.Band.Quiet, 0.0).Intensity)];

        Assert.True(intensity[^1] - intensity[0] > 0.5,
            $"the whole range of dumps spans {intensity[0]:F4}…{intensity[^1]:F4} of brightness — that is flat " +
            "enough that this row could not tell a big dump from a small one.");

        for (int i = 1; i < intensity.Length; i++)
        {
            Assert.True(intensity[i] > intensity[i - 1],
                $"dumping {dumped[i]:F2} lights {intensity[i]:F4} and dumping {dumped[i - 1]:F2} lights " +
                $"{intensity[i - 1]:F4} — brightness is not monotone in the charge that actually left her.");
        }

        // …and the reach follows it, because a bigger dump throws its filaments farther.
        Assert.True(DischargePlume.Shape(1.0, HullCharge.Band.Quiet, 0.0).ReachPx
                    > DischargePlume.Shape(0.1, HullCharge.Band.Quiet, 0.0).ReachPx);
    }

    /// <summary>
    /// …and it is the SENSORS' scale, not a second one. <see cref="DischargePlume.DumpBrightness"/> is
    /// <see cref="HullCharge.SeenFartherFactor"/>'s own excess, normalised — the same arithmetic the charge
    /// board prints and the sensors run. Retune <c>ChargeGlowFactor</c> and the picture follows.
    ///
    /// <para>This is the law the brief named: <i>reuse it, do not invent a second scale.</i> A private constant
    /// here would drift away from the board in the first tuning pass and nobody would ever see it happen.</para>
    ///
    /// <para>RED PROOF: replace the body with any hand-rolled curve (<c>Math.Sqrt(dumped)</c>, say) and every
    /// row of the sweep below fails, naming the two numbers.</para>
    /// </summary>
    [Fact]
    public void BrightnessIsTheSensorsOwnNumberAndNotASecondOpinion()
    {
        double most = HullCharge.SeenFartherFactor(1.0) - 1.0;
        Assert.True(most > 0,
            "a cold hull and a full one are heard at the same range in this build — then nothing about a dump " +
            "can be scaled off the sensors and this whole guard is asserting nothing.");

        foreach (double dumped in new[] { 0.0, 0.05, 0.18, 0.5, 0.9, 1.0 })
        {
            Assert.Equal((HullCharge.SeenFartherFactor(dumped) - 1.0) / most,
                         DischargePlume.DumpBrightness(dumped), 12);
        }

        Assert.Equal(0.0, DischargePlume.DumpBrightness(0.0), 12);
        Assert.Equal(1.0, DischargePlume.DumpBrightness(1.0), 12);

        // Out of range in either direction is clamped rather than extrapolated — a plume brighter than a full
        // hull, or a negative one, is a picture of nothing that ever happens.
        Assert.Equal(0.0, DischargePlume.DumpBrightness(-3.0), 12);
        Assert.Equal(1.0, DischargePlume.DumpBrightness(7.0), 12);
    }

    // ── LAW THREE · THE FLASH DECAYS TO NOTHING INSIDE ITS OWN WINDOW ─────────────────────────────────

    /// <summary>
    /// A dump is an EVENT: it fades, strictly, and is gone by <see cref="DischargePlume.FlashMs"/>. The honest
    /// discharge is 2.2 ms (Lab 43 §C); 600 ms is the smallest stylisation a player can see happen, and a plume
    /// that outlived it would be a lie about a static shock.
    ///
    /// <para>RED PROOF: drop the <c>sinceDumpMs &lt; FlashMs</c> bound (or make the decay term
    /// <c>1.0 - sinceDumpMs / (FlashMs * 10)</c>) and the after-the-window half fails on a hull still lit a
    /// second, a minute and an hour later.</para>
    /// </summary>
    [Fact]
    public void TheFlashDecaysToNothingWithinItsWindow()
    {
        double last = double.MaxValue;
        for (double age = 0; age < DischargePlume.FlashMs; age += DischargePlume.FlashMs / 40.0)
        {
            double now = DischargePlume.Shape(1.0, HullCharge.Band.Quiet, age).Intensity;
            Assert.True(now < last, $"the flash is {now:F4} at {age:F0} ms and was {last:F4} before it — it is " +
                                    "not fading.");
            Assert.True(now > 0, $"the flash reached zero at {age:F0} ms, inside its own {DischargePlume.FlashMs} " +
                                 "ms window.");
            last = now;
        }

        foreach (double age in new[] { DischargePlume.FlashMs, DischargePlume.FlashMs + 1, 60_000.0, 3.6e6 })
        {
            Assert.False(DischargePlume.Shape(1.0, HullCharge.Band.Quiet, age).Draws,
                $"a full dump is still on screen {age:F0} ms later.");
        }

        // A clock that ran backwards (a re-boot, a restored vault) is not a dump. Nothing is drawn for it.
        Assert.False(DischargePlume.Shape(1.0, HullCharge.Band.Quiet, -5.0).Draws);
        Assert.False(DischargePlume.Shape(1.0, HullCharge.Band.Quiet, double.NegativeInfinity).Draws);
        Assert.False(DischargePlume.Shape(1.0, HullCharge.Band.Quiet, double.NaN).Draws);
    }

    /// <summary>…and a dump off an ARCING hull falls back to the crawl she is still in, not to nothing. The two
    /// states are both true and the louder is drawn; a dump that switched the crawl off would report a band she
    /// never left.</summary>
    [Fact]
    public void AfterTheFlashAnArcingHullIsStillCrawling()
    {
        DischargePlume.Plume after = DischargePlume.Shape(1.0, HullCharge.Band.Arcing, DischargePlume.FlashMs + 1);
        Assert.True(after.Draws);
        Assert.False(after.Flashing);
        Assert.Equal(DischargePlume.ArcingSimmer, after.Intensity, 12);
        Assert.Equal(DischargePlume.ArcingFilaments, after.Filaments);

        // And at the snap the dump is the louder of the two, so it is what the eye gets.
        DischargePlume.Plume snap = DischargePlume.Shape(1.0, HullCharge.Band.Arcing, 0.0);
        Assert.True(snap.Flashing);
        Assert.True(snap.Intensity > after.Intensity);
        Assert.Equal(DischargePlume.FlashFilaments, snap.Filaments);
    }

    // ── LAW FOUR · EVERY FILAMENT LEAVES THE MASTHEAD ─────────────────────────────────────────────────

    /// <summary>
    /// THE PLUME LAW, and the reason this type exists. Every filament starts at the MASTHEAD — the sharpest
    /// extremity she has — and never at the hull centroid. A discharge centred on the hull is the shape Lab 43
    /// ruled out, and it is also the shape that is easiest to write by accident.
    ///
    /// <para>Two assertions, and the second is the one a ball would survive without: (1) each filament's first
    /// point IS <see cref="DischargePlume.Masthead"/>, which sits <see cref="DischargePlume.MastPx"/> away from
    /// the origin; and (2) the filaments' mean tip is well off the origin, so a fan raking OUT from the mast
    /// passes and a symmetric spray around the ship fails. A halo drawn from the masthead outward in every
    /// direction has a mean tip at the mast, not at the hull — hence the distance is measured from the SHIP.</para>
    ///
    /// <para>RED PROOF, two ways: start the filaments at <c>(0,0)</c> (the hull centroid) and (1) fails at
    /// every mast angle; spread them over a full <c>Math.Tau</c> instead of the whip's fan and (2) collapses
    /// toward the mast with the ball's own signature.</para>
    /// </summary>
    [Fact]
    public void EveryFilamentLeavesTheMastheadAndNoneTheHullCentroid()
    {
        Span<double> into = stackalloc double[DischargePlume.MaxFilaments * DischargePlume.FloatsPerFilament];

        foreach (DischargePlume.Plume plume in BothStates())
        {
            for (double mastAngle = -Math.PI; mastAngle < Math.PI; mastAngle += Math.PI / 8)
            {
                (double mx, double my) = DischargePlume.Masthead(mastAngle);
                Assert.Equal(DischargePlume.MastPx, Math.Sqrt((mx * mx) + (my * my)), 9);

                for (double phase = 0; phase < 6; phase += 0.37)
                {
                    int n = DischargePlume.Filaments(plume, mastAngle, phase, into);
                    Assert.Equal(plume.Filaments * DischargePlume.FloatsPerFilament, n);

                    double sumX = 0, sumY = 0;
                    for (int i = 0; i < n; i += DischargePlume.FloatsPerFilament)
                    {
                        Assert.Equal(mx, into[i], 9);
                        Assert.Equal(my, into[i + 1], 9);
                        Assert.True(Math.Sqrt((into[i] * into[i]) + (into[i + 1] * into[i + 1])) > 1e-6,
                            "a filament starts at the hull centroid — that is the ball the physics ruled out.");
                        sumX += into[i + 4];
                        sumY += into[i + 5];
                    }

                    double meanTip = Math.Sqrt(((sumX / plume.Filaments) * (sumX / plume.Filaments))
                                               + ((sumY / plume.Filaments) * (sumY / plume.Filaments)));
                    Assert.True(meanTip > DischargePlume.MastPx * 0.6,
                        $"the filaments' mean tip is {meanTip:F3} px from the ship against a mast at " +
                        $"{DischargePlume.MastPx} px — they are spraying evenly about her rather than raking " +
                        "out from the whip. That is a plasma ball.");
                }
            }
        }
    }

    /// <summary>The masthead swings with the whip's screen angle rather than sitting at some fixed corner of
    /// the glyph: a quarter turn of the mast moves it a quarter turn round the ship. (A mast pinned to a
    /// constant would draw a plume that ignores her heading.)</summary>
    [Fact]
    public void TheMastheadFollowsTheWhipRound()
    {
        AssertMasthead(0, DischargePlume.MastPx, 0.0);
        AssertMasthead(Math.PI / 2, 0.0, DischargePlume.MastPx);
        AssertMasthead(Math.PI, -DischargePlume.MastPx, 0.0);
        AssertMasthead(-Math.PI / 2, 0.0, -DischargePlume.MastPx);
    }

    private static void AssertMasthead(double angle, double x, double y)
    {
        (double gotX, double gotY) = DischargePlume.Masthead(angle);
        Assert.Equal(x, gotX, 9);
        Assert.Equal(y, gotY, 9);
    }

    // ── LAW FIVE · THE CRAWL IS DETERMINISTIC, AND IT STILL CRAWLS ────────────────────────────────────

    /// <summary>
    /// The arcing crawl is a pure function of the phase — the same sim time draws the same filaments, always.
    /// That is what makes a PAUSED map stable (the same frame drawn twice is the same frame) and what keeps the
    /// fingerprint ledgers still. There is no <c>Random</c> anywhere in this picture.
    ///
    /// <para>ANTI-VACUOUS, and this is the half that matters: a constant geometry is trivially deterministic,
    /// so the row also asserts the crawl MOVES between sim times. Both directions, or the guard is a rubber
    /// stamp for a plume that never animates.</para>
    ///
    /// <para>RED PROOF: seed the hash from <c>DateTime.UtcNow</c> or a <c>Random</c> and the repeatability half
    /// fails on the first pair; drop the phase from the hash entirely and the movement half fails.</para>
    /// </summary>
    [Fact]
    public void TheCrawlIsDeterministicForAGivenSimTimeAndStillMoves()
    {
        DischargePlume.Plume crawl = DischargePlume.Shape(0.0, HullCharge.Band.Arcing, 99_999.0);
        Assert.False(crawl.Flashing);

        const double simTime = 1234.5;
        double[] once = Bolts(crawl, 0.7, DischargePlume.CrawlPhase(simTime));
        double[] again = Bolts(crawl, 0.7, DischargePlume.CrawlPhase(simTime));
        Assert.Equal(once, again);

        // …a third time, after other work has run through the same function on other arguments — a cached or
        // stateful flicker would come apart here rather than on the pair above.
        _ = Bolts(crawl, 2.1, DischargePlume.CrawlPhase(simTime + 61));
        Assert.Equal(once, Bolts(crawl, 0.7, DischargePlume.CrawlPhase(simTime)));

        // And it MOVES: a quarter of a step apart is a visible swing, a whole step is a new set of bolts.
        double[] aBitLater = Bolts(crawl, 0.7, DischargePlume.CrawlPhase(simTime + (DischargePlume.CrawlStepSeconds / 4)));
        Assert.NotEqual(once, aBitLater);
        double[] aStepLater = Bolts(crawl, 0.7, DischargePlume.CrawlPhase(simTime + DischargePlume.CrawlStepSeconds));
        Assert.NotEqual(once, aStepLater);

        // …and within a step it CRAWLS rather than redraws: the fan swings about the masthead, so no point
        // travels as far as a filament is long. (Reseeding the hash every frame instead of every step moves
        // them by whole filament lengths and fails this.)
        double travelled = FarthestApart(once, aBitLater);
        Assert.True(travelled > 0, "a quarter of a step moved nothing at all — the crawl does not crawl.");
        Assert.True(travelled < crawl.ReachPx,
            $"a quarter of a step threw the filaments {travelled:F3} px about, against a reach of " +
            $"{crawl.ReachPx:F3} px — that is a strobe, not a crawl.");
    }

    /// <summary>A dump SNAPS: within one flash step the bolts are held still, because a 2.2 ms event that
    /// swung would be slow-motion lightning. The step boundary is where it changes.</summary>
    [Fact]
    public void ADumpSnapsRatherThanSwinging()
    {
        DischargePlume.Plume flash = DischargePlume.Shape(1.0, HullCharge.Band.Quiet, 0.0);
        Assert.True(flash.Flashing);

        double[] atStart = Bolts(flash, 0.4, DischargePlume.FlashPhase(0));
        double[] midStep = Bolts(flash, 0.4, DischargePlume.FlashPhase(DischargePlume.FlashStepMs * 0.9));
        Assert.Equal(atStart, midStep);

        double[] nextStep = Bolts(flash, 0.4, DischargePlume.FlashPhase(DischargePlume.FlashStepMs * 1.1));
        Assert.NotEqual(atStart, nextStep);
    }

    // ── LAW SIX · IT STAYS INSIDE ITS BUDGET ──────────────────────────────────────────────────────────

    /// <summary>
    /// A few dozen line segments, drawn every frame, allocating nothing. The budget is asserted rather than
    /// hoped for: no state draws more than <see cref="DischargePlume.MaxFilaments"/> filaments, the buffer the
    /// renderer stack-allocates is big enough for every one of them, and a buffer that is NOT big enough
    /// throws rather than drawing half a bolt somebody would have to explain.
    /// </summary>
    [Fact]
    public void ThePlumeStaysInsideTheBufferTheRendererGivesIt()
    {
        foreach (double dumped in new[] { 0.0, 0.3, 1.0 })
        {
            foreach (HullCharge.Band band in Enum.GetValues<HullCharge.Band>())
            {
                foreach (double age in new[] { 0.0, 100.0, 601.0 })
                {
                    DischargePlume.Plume plume = DischargePlume.Shape(dumped, band, age);
                    Assert.InRange(plume.Filaments, 0, DischargePlume.MaxFilaments);
                    Assert.InRange(plume.Intensity, 0.0, 1.0);
                    Assert.True(plume.ReachPx <= DischargePlume.MastPx * 2,
                        $"the plume reaches {plume.ReachPx:F2} px off a {DischargePlume.MastPx} px mast — it is " +
                        "growing into the HUD's room.");
                }
            }
        }

        DischargePlume.Plume biggest = DischargePlume.Shape(1.0, HullCharge.Band.Arcing, 0.0);
        Assert.Equal(DischargePlume.MaxFilaments, biggest.Filaments);
        Assert.Throws<ArgumentException>(() =>
        {
            double[] tooSmall = new double[(DischargePlume.MaxFilaments * DischargePlume.FloatsPerFilament) - 1];
            DischargePlume.Filaments(biggest, 0.0, 0.0, tooSmall);
        });
    }

    // ── plumbing ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The two states that draw: a full dump at its snap, and a hull merely arcing.</summary>
    private static IEnumerable<DischargePlume.Plume> BothStates()
    {
        yield return DischargePlume.Shape(1.0, HullCharge.Band.Quiet, 0.0);
        yield return DischargePlume.Shape(0.0, HullCharge.Band.Arcing, 99_999.0);
    }

    private static double[] Bolts(in DischargePlume.Plume plume, double mastAngle, double phase)
    {
        double[] into = new double[DischargePlume.MaxFilaments * DischargePlume.FloatsPerFilament];
        int n = DischargePlume.Filaments(plume, mastAngle, phase, into);
        return into[..n];
    }

    /// <summary>The largest distance any one written coordinate moved between two draws — how far the crawl
    /// travelled.</summary>
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

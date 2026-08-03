using SpaceSails.Contracts;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #411 · THE ROUTE — the guards for the cycler window and the run it carries.
///
/// <para>Three things had to be true before the arc's oldest promise could be kept, and each is a class of
/// bug this repo has already paid for:</para>
///
/// <list type="number">
/// <item><b>The sentence matches the sim</b> (bug class 3). The window quotes a wait. If the number in the
/// sentence and the number the gate reads are computed separately, they will drift — that is exactly how
/// this same arc's ledger came to count down to the wrong threshold for weeks. Everything here is asked of
/// the same functions the gate asks.</item>
/// <item><b>The world handed to the guard can tell pass from fail</b> (bug class 5). The window's rarity is
/// checked BOTH ways over a real span; the arrival placement is checked against the shipped scenario's own
/// numbers rather than against a typed-in radius.</item>
/// <item><b>It states nothing.</b> The route is the loudest part of the arc and the closest to the truth,
/// so its prose is held to the same forbidden vocabulary as the front door.</item>
/// </list>
///
/// <para><b>Proven RED</b> — see the PR body for which constant was moved for which test.</para>
/// </summary>
public class TheCyclerWindowTests
{
    private static ScenarioDefinition Sol =>
        ScenarioLoader.LoadFile(Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json"));

    private const double Day = 86_400.0;

    // ── 1 · The timetable is a timetable ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Two-sided, over a real span: across ten periods of sim time sampled every hour, the window is open
    /// for a share of the time that matches its own declared duty cycle, and it is neither always open nor
    /// never open.
    ///
    /// <para>A one-sided check ("it opens sometimes") passes on a window that is ALWAYS open, which is the
    /// bug that makes the whole "hold the berth and wait" instruction a lie.</para>
    /// </summary>
    [Fact]
    public void TheWindowIsOpenExactlyAsOftenAsItSaysItIs()
    {
        int open = 0, total = 0;
        for (double t = 0; t < CyclerWindow.PeriodSeconds * 10; t += 3600.0)
        {
            total++;
            if (CyclerWindow.IsOpen(t))
            {
                open++;
            }
        }

        double duty = (double)open / total;
        double declared = CyclerWindow.OpenSeconds / CyclerWindow.PeriodSeconds;

        Assert.True(open > 0, "the window never opened in ten periods — that is not a window");
        Assert.True(open < total, "the window was open at every sample — then nothing is ever waited for");
        Assert.InRange(duty, declared * 0.9, declared * 1.1);
    }

    /// <summary>The quoted wait IS the wait. Whatever the gate says the window will open in, it is open
    /// then — the one guard that makes the button's own label trustworthy.</summary>
    [Fact]
    public void TheQuotedWaitIsTheActualWait()
    {
        for (double t = 0; t < CyclerWindow.PeriodSeconds * 3; t += 7_777.0)
        {
            double until = CyclerWindow.SecondsUntilOpen(t);

            if (CyclerWindow.IsOpen(t))
            {
                Assert.Equal(0.0, until);
                continue;
            }

            Assert.True(until > 0, $"t={t}: shut, but quoted a zero wait");
            Assert.True(CyclerWindow.IsOpen(t + until),
                $"t={t}: the gate quoted {until / Day:F2} d and was still shut when it came");

            // …and not a moment sooner. Quoting a wait that is longer than the real one is the same lie
            // the other way round, and is what the ledger's old pool-sized countdown did.
            Assert.False(CyclerWindow.IsOpen(t + (until * 0.999)),
                $"t={t}: it was already open BEFORE the quoted wait elapsed");
        }
    }

    /// <summary>An open window shuts, and it shuts when it said it would.</summary>
    [Fact]
    public void AnOpenWindowShutsOnSchedule()
    {
        double opening = CyclerWindow.NextOpening(0);
        Assert.True(CyclerWindow.IsOpen(opening));

        double left = CyclerWindow.SecondsUntilShut(opening);
        Assert.Equal(CyclerWindow.OpenSeconds, left, 3);
        Assert.True(CyclerWindow.IsOpen(opening + (left * 0.999)));
        Assert.False(CyclerWindow.IsOpen(opening + left + 1.0));
    }

    /// <summary>The wait is worth waiting: the arc must not come round so often that "hold the berth" is
    /// a formality, nor so rarely that a captain who has earned the code is told to come back next year.
    /// Measured against the constant, both ways.</summary>
    [Fact]
    public void TheWaitIsRareButNotPunitive()
    {
        Assert.InRange(CyclerWindow.PeriodSeconds / Day, 14.0, 90.0);
        Assert.InRange(CyclerWindow.OpenSeconds / Day, 1.0, 5.0);
        Assert.True(CyclerWindow.OpenSeconds < CyclerWindow.PeriodSeconds / 4.0,
            "a window that stands open a quarter of the time is a door, not a window");
    }

    /// <summary>The crossing costs real sim time — it is a free-return arc, not a burn. A ride that took
    /// no time would make the whole "the window is not open yet" fiction pointless.</summary>
    [Fact]
    public void TheCrossingCostsWeeks()
    {
        Assert.True(CyclerWindow.CrossingSeconds >= 14.0 * Day,
            "a ride you can do over lunch is not a slow arc that comes round rarely");
    }

    // ── 2 · The gate ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NotOnTheBoardIsTheFirstThingInTheWay()
    {
        double open = CyclerWindow.NextOpening(0);
        Assert.Equal(CyclerWindow.Blocker.NotOnTheBoard, CyclerWindow.Evaluate(false, false, open));
        Assert.Equal(CyclerWindow.Blocker.NotOnTheBoard, CyclerWindow.Evaluate(false, true, open));
    }

    [Fact]
    public void OnTheBoardWithNothingFiledIsRefusedEvenInAnOpenWindow()
    {
        double open = CyclerWindow.NextOpening(0);
        Assert.True(CyclerWindow.IsOpen(open));
        Assert.Equal(CyclerWindow.Blocker.NoRunFiled, CyclerWindow.Evaluate(true, false, open));
    }

    [Fact]
    public void FiledAndOpenIsTheOnlyWayThrough()
    {
        double open = CyclerWindow.NextOpening(0);
        double shut = open + CyclerWindow.OpenSeconds + Day;

        Assert.Equal(CyclerWindow.Blocker.None, CyclerWindow.Evaluate(true, true, open));
        Assert.Equal(CyclerWindow.Blocker.WindowShut, CyclerWindow.Evaluate(true, true, shut));
    }

    /// <summary>Every refusal has words, and the one non-refusal has none. A control that is
    /// visible-but-disabled with no reason is what #212 was filed about.</summary>
    [Fact]
    public void EveryBlockerSaysWhy()
    {
        double shut = CyclerWindow.NextOpening(0) + CyclerWindow.OpenSeconds + Day;
        foreach (CyclerWindow.Blocker b in Enum.GetValues<CyclerWindow.Blocker>())
        {
            string text = CyclerWindow.RefusalText(b, shut);
            if (b == CyclerWindow.Blocker.None)
            {
                Assert.Equal("", text);
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(text), $"{b} refuses in silence");
            Assert.DoesNotContain("TODO", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("not implemented", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pending", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── 3 · The arrival is somewhere a ship can actually be ──────────────────────────────────────────────

    /// <summary>
    /// The ride's arrival offset, measured against the SHIPPED scenario rather than a typed-in number —
    /// the fifth bug class's own rule ("a test is only as honest as the world you hand it").
    ///
    /// <para>Two ends: inside one shuttle hop, or the captain arrives at a moon they cannot reach and the
    /// whole route delivers them to a view; and well outside the moon's own radius, or the arrival is
    /// underground. The shuttle board's own filter refuses anything at or inside the body radius.</para>
    /// </summary>
    [Fact]
    public void TheRideLetsHerGoWhereTheShuttleCanFinishTheJob()
    {
        BodyDefinition ice = Sol.Bodies.Single(b => b.Id == KaamosLore.IceMoonBodyId);

        Assert.True(CyclerWindow.ArrivalOffsetMeters < ShuttleRange.RangeMeters,
            $"the ride lets her go {CyclerWindow.ArrivalOffsetMeters:E2} m out, past the shuttle's " +
            $"{ShuttleRange.RangeMeters:E2} m reach — the last mile would be impossible");
        Assert.True(CyclerWindow.ArrivalOffsetMeters > ice.BodyRadiusM * 4.0,
            $"the ride lets her go {CyclerWindow.ArrivalOffsetMeters:E2} m out, which is not clear of a " +
            $"{ice.BodyRadiusM:E2} m moon");
    }

    /// <summary>And the moon the whole arc points at is a moon, is charted, and is a haven — so the
    /// ordinary cargo-run completion path (park in orbit there and it is done) can settle the supply run
    /// with no second code path.</summary>
    [Fact]
    public void TheIceMoonIsAnOrdinaryCharedHavenMoonSoTheRunSettlesItself()
    {
        BodyDefinition ice = Sol.Bodies.Single(b => b.Id == KaamosLore.IceMoonBodyId);

        Assert.Equal("moon", ice.Kind);
        Assert.True(ice.Haven, "the run completes by parking in its orbit; that path only runs at a haven");
        Assert.False(ice.Hidden, "the arc is about permission, never about finding the moon");
        Assert.True(ShuttleExcursion.IsLandableSurface(BodyKind.Moon));
    }

    // ── 4 · It states nothing ────────────────────────────────────────────────────────────────────────────

    /// <summary>The route's prose is the closest this arc gets to the thing under the ice, and it is the
    /// loudest. Same forbidden vocabulary as the front door: the bible's §2 words never appear.</summary>
    [Fact]
    public void TheRouteNeverStatesTheTruth()
    {
        string[] routeProse =
        [
            CyclerWindow.Departing,
            CyclerWindow.Arrived,
            CyclerWindow.BerthListedHeadline,
            CyclerWindow.MenuAction,
            KaamosLore.SupplyRunTitle,
            KaamosLore.SupplyRunAccepted,
            KaamosLore.SupplyRunBlurb(12_345),
            CyclerWindow.RefusalText(CyclerWindow.Blocker.NotOnTheBoard, 0),
            CyclerWindow.RefusalText(CyclerWindow.Blocker.NoRunFiled, 0),
            CyclerWindow.RefusalText(CyclerWindow.Blocker.WindowShut, 0),
        ];

        string[] forbidden =
        [
            "Vantar", "lucid", "awake", "one voice", "continuous", "mind", "alive", "waiting for you",
        ];

        foreach (string line in routeProse)
        {
            foreach (string word in forbidden)
            {
                Assert.False(line.Contains(word, StringComparison.OrdinalIgnoreCase),
                    $"the route said \"{word}\" out loud: {line}");
            }
        }
    }

    /// <summary>The run's manifest quotes the cold pod's own slug, because it IS that consignment — the one
    /// that was packed and then HELD. It is the only joining of the pieces the route text does, and it does
    /// it by repeating a line the player has already read rather than by explaining. If the pod's prose is
    /// ever reworded, this is what notices the echo has gone.</summary>
    [Fact]
    public void TheRunCarriesTheSameManifestTheHeldPodCarried()
    {
        string pod = KaamosLore.ById("cold-pod")!.Lore;
        foreach (string slug in new[] { "CONSUMABLES", "WINTERING CREW", "DEST. KAAMOS", "CYCLER WINDOW" })
        {
            Assert.Contains(slug, pod, StringComparison.Ordinal);
            Assert.Contains(slug, KaamosLore.SupplyRunAccepted, StringComparison.Ordinal);
        }
    }

    /// <summary>The wire's line is a clerk's housekeeping note and names nobody. An arc landing on the news
    /// as an ANNOUNCEMENT is the wrong shape (`SECURITY ALERTED` as a banner); a dormant listing quietly
    /// reconciled is the right one.</summary>
    [Fact]
    public void TheWireFilesItAndDoesNotAnnounceIt()
    {
        string head = CyclerWindow.BerthListedHeadline;
        Assert.DoesNotContain("KAAMOS", head, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("you", head, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("!", head, StringComparison.Ordinal);

        // …and it goes out on the wire as itself, not wrapped in a template that would editorialise it.
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks, 0, head);
        Assert.Equal(head, NewsWire.Headline(evt));
    }
}

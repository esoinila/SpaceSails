using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1074 beat 5 · THE RETURNING SHUTTLE — every band of the beat pinned: the canon, the eligibility
/// predicate arm by arm, the next-arrival firing, once-per-run, the first-three-sim-days gate, the phases, the
/// told-once fixtures, the hull's geometry, and the preserved site left byte-identical.
///
/// <para>The key guards were watched go RED against a revert of the behaviour they name; the table is in the PR
/// (#587's lesson: a guard nobody has seen fail is a guess).</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheReturningShuttleTests
{
    private const string Site = "rock-a";
    private const double Day = NewsWire.SecondsPerDay;

    private static string? ParentOf(string id) => id == Site ? "berth-1" : null;

    private static bool InCare(string id) => id == Site;

    private static FieldNote Note(string text) => new(text, 0, "somewhere", "📋", "");

    private static IReadOnlyList<FieldNote> Clipped(string text) => [Note("unrelated"), Note(text)];

    private static string? Fires(
        ReturningShuttle.Row? row = null, string[]? seen = null, IReadOnlyList<FieldNote>? book = null,
        double simTime = 10 * Day, string? arrival = Site, Func<string, bool>? inCare = null) =>
        ReturningShuttle.Fires(row, seen ?? [Site], book ?? Clipped(MoneyTrail.PourLineItem),
            simTime, arrival, ParentOf, inCare ?? InCare);

    // ══ THE CANON ══════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE LINES ARE FABLE'S, VERBATIM. Typed out here again on purpose: a constant compared with itself
    /// proves nothing.
    /// </summary>
    [Fact]
    public void TheCanonLinesAreHeldVerbatim()
    {
        Assert.Equal("CHARTER SURVEY", ReturningShuttle.BoardTag);
        Assert.Equal(
            "A charter survey's shuttle came back to its ship on its own autopilot. The ship has not moved since. "
            + "The Authority has posted a structural notice at the site.", ReturningShuttle.WireLine);
        Assert.Equal("The mooring line is cut clean. Not snapped: cut, by somebody with time.",
            ReturningShuttle.AirlockLine);
        Assert.Equal("The boarding gun is racked, charged, unfired. Nobody lost a fight.", ReturningShuttle.RackLine);
        Assert.Equal("survey-log-last", ReturningShuttle.LogPaperId);
        Assert.Equal("A survey log, last page", ReturningShuttle.LogTitle);
        Assert.Equal(
            "Day four. The seal below the listed bottom opened to the key we were not given. "
            + "Descending with the full party at 06:10. Will report at the—", ReturningShuttle.LogDocument);
        Assert.Equal(
            "Six went down. The shuttle came up. Nothing on this hull says which of those is the strange part.",
            ReturningShuttle.FieldBookLine);
        Assert.Equal("📍", ReturningShuttle.FieldBookGlyph);
    }

    /// <summary>
    /// THE REPO'S ORDINARY PAPER SEAMS READ THE PAGE AS THE CANON TITLED IT — away from its room, in the
    /// satchel and on the card.
    /// </summary>
    [Fact]
    public void TheLogIsAnAuthoredPaperTheBookReadsByItsOwnTitle()
    {
        Assert.True(FieldClue.IsAuthored(ReturningShuttle.LogPaperId));
        Assert.Equal(ReturningShuttle.LogTitle, FieldClue.Title(ReturningShuttle.LogPaperId));
        Assert.Equal(ReturningShuttle.LogDocument, FieldClue.Document(ReturningShuttle.LogPaperId));
        CarriedObject.Reveal read = CarriedObject.PaperReveal(ReturningShuttle.LogPaperId);
        Assert.Equal(ReturningShuttle.LogTitle, read.Label);
        Assert.Equal(ReturningShuttle.LogDocument, read.Story);
    }

    /// <summary>
    /// NO FOURTH STRING, AND NO LINE THAT CLOSES THE QUESTION. Every public string the type publishes is in
    /// <see cref="ReturningShuttle.AllProse"/> (bar the two machine words), and none of them says the reserved
    /// word, the canon list, or anything about anybody being alive, dead, found or missing.
    ///
    /// </summary>
    [Fact]
    public void NoLineClosesTheQuestionAndThereIsNoFourthString()
    {
        var published = new List<string>();
        foreach (FieldInfo f in typeof(ReturningShuttle).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (f.FieldType == typeof(string) && f.GetValue(null) is string v
                && f.Name is not (nameof(ReturningShuttle.LogPaperId) or nameof(ReturningShuttle.FieldBookGlyph)))
            {
                published.Add(v);
            }
        }
        Assert.Equal(
            published.OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            ReturningShuttle.AllProse().OrderBy(s => s, StringComparer.Ordinal).ToArray());

        string[] forbidden =
        [
            "monolith", "old one", "reever", "restore", "backup", "revive", "resurrect", "clone", "kaamos",
            "minister", "ancient", "alien", "experiment", "specimen",
            // the question is never closed
            "alive", "dead", "died", "corpse", "missing", "survivor", "found",
        ];
        foreach (string line in ReturningShuttle.AllProse())
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }

            // whole words, because "somebody" is the canon's own word and is not a body
            Assert.DoesNotMatch(@"(body|bodies)", line.ToLowerInvariant());
        }
    }

    // ══ THE ELIGIBILITY PREDICATE, ARM BY ARM ══════════════════════════════════════════════════════════

    /// <summary>
    /// ARM 1 · NO PRESERVED SITE SEEN → NO BEAT, whatever else he has done.
    /// </summary>
    [Fact]
    public void WithoutAPreservedSiteSeenThereIsNoBeat()
    {
        Assert.Null(Fires(seen: []));
        Assert.Null(ReturningShuttle.Fires(null, null, Clipped(MoneyTrail.PourLineItem), 10 * Day, Site, ParentOf, InCare));
    }

    /// <summary>
    /// ARM 2 · A SITE SEEN BUT NOTHING OF BEAT 3 OR 4 TOUCHED → NO BEAT. The book holds plenty — just none of
    /// the five lines that count.
    /// </summary>
    [Fact]
    public void ASiteSeenWithoutTheTrailTouchedIsNoBeat()
    {
        Assert.Null(Fires(book: []));
        Assert.Null(Fires(book: [Note("Perimeter rail"), Note("a mug, somewhere"), Note("Transferred.")]));
    }

    /// <summary>
    /// ARM 3 · BOTH → ELIGIBLE, by each of the five ways to have touched the trail: a clipped rail, rota or
    /// pour (beat 3) or either colleague's answer (beat 4). The population is asserted to be five.
    ///
    /// </summary>
    [Fact]
    public void ASiteSeenAndTheTrailTouchedIsEligibleByEachOfTheFiveWays()
    {
        string[] ways =
        [
            MoneyTrail.RailLineItem, MoneyTrail.RotaLineItem, MoneyTrail.PourLineItem,
            CareerCost.ColleagueLine, CareerCost.MugLine,
        ];
        Assert.Equal(5, ways.Distinct().Count());
        foreach (string way in ways)
        {
            Assert.Equal(Site, Fires(book: Clipped(way)));
        }

        // …and the paper's own field-book body counts as the same clip.
        Assert.Equal(Site, Fires(book: Clipped(PaperHeads.PourDocument)));
    }

    /// <summary>
    /// A SITE THE OFFICE HAS NOT TAKEN INTO CARE IS NOT A SEEN SITE. The beat is staged on a PRESERVED ground.
    ///
    /// </summary>
    [Fact]
    public void OnlyAGroundStillInCareCounts()
    {
        Assert.Null(Fires(inCare: _ => false));
    }

    // ══ NEXT ARRIVAL, ONCE, AND NEVER IN THE FIRST THREE SIM-DAYS ═════════════════════════════════════

    /// <summary>
    /// IT FIRES ON HIS NEXT ARRIVAL IN THAT SITE'S SYSTEM — the site itself or the berth it orbits — and on no
    /// other arrival.
    /// </summary>
    [Fact]
    public void ItFiresOnTheNextArrivalInTheSitesSystemAndNoOther()
    {
        Assert.Equal(Site, Fires(arrival: Site));
        Assert.Equal(Site, Fires(arrival: "berth-1"));
        Assert.Null(Fires(arrival: "luna"));
        Assert.Null(Fires(arrival: null));
        Assert.Null(Fires(arrival: ""));
    }

    /// <summary>
    /// NEVER IN THE FIRST THREE SIM-DAYS (owner 2026-10-04). Both edges: one second short is no; exactly three
    /// days is yes.
    /// </summary>
    [Fact]
    public void NeverInTheFirstThreeSimDays()
    {
        Assert.Equal(3.0, ReturningShuttle.FirstSimDays);
        Assert.Null(Fires(simTime: 0));
        Assert.Null(Fires(simTime: 2.5 * Day));
        Assert.Null(Fires(simTime: (3 * Day) - 1));
        Assert.Equal(Site, Fires(simTime: 3 * Day));
        Assert.Equal(Site, Fires(simTime: 400 * Day));
    }

    /// <summary>
    /// ONCE PER RUN: a row that exists — at any phase, on any ground — means it never fires again.
    ///
    /// </summary>
    [Fact]
    public void OncePerRun()
    {
        foreach (bool wired in new[] { false, true })
        {
            Assert.Null(Fires(row: new ReturningShuttle.Row(Site, 5, wired)));
            Assert.Null(Fires(row: new ReturningShuttle.Row("another-rock", 5, wired)));
        }
    }

    // ══ THE PHASES ═════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// LANDED, THEN — ONE WORLD WINDOW LATER — RETURNED. And the wire owes its line exactly once, only after
    /// the return.
    /// </summary>
    [Fact]
    public void LandedThenReturnedOneWindowLaterAndTheWireIsOwedOnce()
    {
        Assert.Equal(1, ReturningShuttle.WindowsToReturn);
        var row = new ReturningShuttle.Row(Site, 100);
        Assert.Equal(ReturningShuttle.Phase.None, ReturningShuttle.PhaseOf(null, 100));
        Assert.Equal(ReturningShuttle.Phase.Landed, ReturningShuttle.PhaseOf(row, 100));
        Assert.Equal(ReturningShuttle.Phase.Returned, ReturningShuttle.PhaseOf(row, 101));
        Assert.Equal(ReturningShuttle.Phase.Returned, ReturningShuttle.PhaseOf(row, 5000));

        Assert.False(ReturningShuttle.WireIsOwed(null, 500));
        Assert.False(ReturningShuttle.WireIsOwed(row, 100));
        Assert.True(ReturningShuttle.WireIsOwed(row, 101));
        Assert.False(ReturningShuttle.WireIsOwed(row with { Wired = true }, 101));
    }

    // ══ THE FIXTURES ABOARD, TOLD ONCE ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// NOTHING IS ABOARD UNTIL THE SHUTTLE IS BACK; THEN THREE FIXTURES; AND THE LOG DESK GOES WHEN ITS PAGE
    /// HAS BEEN TAKEN (the 📍 line filed). Judged AFTER the whole sequence, not one pulse of it.
    ///
    /// </summary>
    [Fact]
    public void ThreeFixturesOnceTheShuttleIsBackAndTheLogDeskGoesWithItsPage()
    {
        var row = new ReturningShuttle.Row(Site, 100);
        Assert.Empty(ReturningShuttle.Fixtures(null, 200, []));
        Assert.Empty(ReturningShuttle.Fixtures(row, 100, []));
        Assert.Equal(
            [ReturningShuttle.AirlockPlate, ReturningShuttle.RackPlate, ReturningShuttle.LogPlate],
            ReturningShuttle.Fixtures(row, 101, []));

        IReadOnlyList<FieldNote> book = [Note(ReturningShuttle.FieldBookLine)];
        Assert.Equal(
            [ReturningShuttle.AirlockPlate, ReturningShuttle.RackPlate],
            ReturningShuttle.Fixtures(row, 101, book));
    }

    /// <summary>
    /// THE AIRLOCK AND THE RACK EACH TELL ONCE — the first press the line, every press after it nothing — and
    /// the log desk tells no line at all. A sequence, judged at its end.
    /// </summary>
    [Fact]
    public void TheAirlockAndTheRackTellOnce()
    {
        var book = new List<FieldNote>();
        Assert.Equal(ReturningShuttle.AirlockLine, ReturningShuttle.TellsOnPress(ReturningShuttle.AirlockPlate, book));
        Assert.Equal(ReturningShuttle.RackLine, ReturningShuttle.TellsOnPress(ReturningShuttle.RackPlate, book));
        Assert.Null(ReturningShuttle.TellsOnPress(ReturningShuttle.LogPlate, book));

        book.Add(Note(ReturningShuttle.AirlockLine));
        Assert.Null(ReturningShuttle.TellsOnPress(ReturningShuttle.AirlockPlate, book));
        Assert.Equal(ReturningShuttle.RackLine, ReturningShuttle.TellsOnPress(ReturningShuttle.RackPlate, book));

        book.Add(Note(ReturningShuttle.RackLine));
        Assert.Null(ReturningShuttle.TellsOnPress(ReturningShuttle.AirlockPlate, book));
        Assert.Null(ReturningShuttle.TellsOnPress(ReturningShuttle.RackPlate, book));
        Assert.Null(ReturningShuttle.TellsOnPress("some other plate", book));
    }

    // ══ THE HULL ON THE BOARD ══════════════════════════════════════════════════════════════════════════

    private sealed class OneRock : ICelestialEphemeris
    {
        public IReadOnlyList<CelestialBody> Bodies { get; } =
        [
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody(Site, "Rock A", "sun", 0, 2e4, 1.5e11, 3.15e7, 0),
        ];

        public Vector2d Position(string bodyId, double simTime) =>
            bodyId == "sun" ? default : new Vector2d(1.5e11, simTime);
    }

    /// <summary>
    /// ON THE BOARD: the board's tag BEFORE an ordinary hauler callsign, a parked fixture on the ground's own
    /// orbit with no plan and no cargo, deterministic.
    /// </summary>
    [Fact]
    public void ThePlateOnTheBoardIsTheTagThenAnOrdinaryCallsign()
    {
        NpcShip hull = ReturningShuttle.Parked(new OneRock(), Site);
        Assert.StartsWith(ReturningShuttle.BoardTag + " ", hull.Callsign, StringComparison.Ordinal);
        string name = hull.Callsign[(ReturningShuttle.BoardTag.Length + 1)..];
        Assert.Contains(name, TrafficSchedule.Callsigns);
        Assert.Equal(Site, hull.DepotBodyId);
        Assert.Equal(Site, hull.DestinationId);
        Assert.Empty(hull.Plan.Nodes);
        Assert.Equal(0, hull.CargoUnits);
        Assert.Equal(ReturningShuttle.ShipIdFor(Site), hull.Id);

        NpcShip again = ReturningShuttle.Parked(new OneRock(), Site);
        Assert.Equal(hull.Callsign, again.Callsign);
        Assert.Equal(hull.DepotPhase, again.DepotPhase);
    }

    // ══ THE HULL ON THE GROUND ═════════════════════════════════════════════════════════════════════════

    private static readonly SurfaceLayout.Field Field = SurfaceLayout.DefaultField;

    /// <summary>
    /// SHE FITS — inside the field, off the way home, closed but for ONE gap in the long wall, with the three
    /// stations inside her and clear of the walls; and refuses (null) when the ground is blocked everywhere.
    ///
    /// </summary>
    [Fact]
    public void TheHullFitsHasOneGapAndHoldsTheThreeStationsInside()
    {
        double fx = Field.AnchorX, fy = Field.AnchorY;   // deep in the field: room all round
        ReturningShuttle.Hull? parked = ReturningShuttle.Park(fx, fy, 8, Field, (_, _, _, _) => false);
        Assert.NotNull(parked);
        ReturningShuttle.Hull h = parked.Value;

        Assert.Equal(5, h.Walls.Count);   // four sides, one of them in two halves round the gap
        double x0 = h.CentreX - (ReturningShuttle.HullLength / 2), x1 = h.CentreX + (ReturningShuttle.HullLength / 2);
        double y0 = h.CentreY - (ReturningShuttle.HullBreadth / 2), y1 = h.CentreY + (ReturningShuttle.HullBreadth / 2);
        foreach ((double X, double Y) s in new[] { h.Airlock, h.Rack, h.Log })
        {
            Assert.InRange(s.X, x0 + 0.5, x1 - 0.5);
            Assert.InRange(s.Y, y0 + 0.5, y1 - 0.5);
            foreach (SurfaceLayout.Wall w in h.Walls)
            {
                Assert.False(
                    ReturningShuttle.SegmentHitsRect(w.X1, w.Y1, w.X2, w.Y2, s.X - 0.7, s.Y - 0.7, s.X + 0.7, s.Y + 0.7),
                    $"a station at ({s.X:F1}, {s.Y:F1}) is touching the hull's own wall");
            }
        }

        // the gap really is a gap: the two halves of one long wall stop short of each other by the gap width
        var gapWall = h.Walls.Where(w => w.Y1 == w.Y2 && Math.Abs(w.Y1 - h.Airlock.Y) < 2.0).OrderBy(w => w.X1).ToList();
        Assert.Equal(2, gapWall.Count);
        Assert.Equal(ReturningShuttle.GapWidth, gapWall[1].X1 - gapWall[0].X2, 6);

        // the same ground answers the same way, and a ground that blocks everything answers no
        Assert.Equal(h.CentreX, ReturningShuttle.Park(fx, fy, 8, Field, (_, _, _, _) => false)!.Value.CentreX);
        Assert.Null(ReturningShuttle.Park(fx, fy, 8, Field, (_, _, _, _) => true));
    }

    // ══ THE SITE IS BYTE-IDENTICAL ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// THE STUDY NEVER ENDS: the whole beat — the arrival that fires it, the board row, the phases, the
    /// fixtures — leaves the preserved-site and stop registers exactly as it found them, and the vault it rides
    /// differs from the vault without it ONLY in its own two rows. Compared as serialised bytes.
    ///
    /// </summary>
    [Fact]
    public void ThePreservedSiteIsByteIdenticalBeforeAndAfterTheBeat()
    {
        IReadOnlyList<string> preservedBefore = PreservationZone.Preserved;
        IReadOnlyList<string> stoppedBefore = StopOrder.Stopped;
        PreservationZone.Install([Site]);
        StopOrder.Install([Site]);
        try
        {
            string[] preserved = [.. PreservationZone.Preserved];
            string[] stopped = [.. StopOrder.Stopped];

            // the whole sequence, as the Map runs it
            string? fired = ReturningShuttle.Fires(
                null, [Site], Clipped(MoneyTrail.RailLineItem), 10 * Day, Site, ParentOf, PreservationZone.On);
            Assert.Equal(Site, fired);
            var row = new ReturningShuttle.Row(fired!, 100);
            _ = ReturningShuttle.Parked(new OneRock(), Site);
            foreach (long window in new long[] { 100, 101, 102 })
            {
                _ = ReturningShuttle.PhaseOf(row, window);
                _ = ReturningShuttle.WireIsOwed(row, window);
                _ = ReturningShuttle.Fixtures(row, window, []);
            }

            Assert.Equal(preserved, PreservationZone.Preserved.ToArray());
            Assert.Equal(stopped, StopOrder.Stopped.ToArray());
            Assert.True(PreservationZone.On(Site));

            // the vault: identical but for the beat's own two properties
            var plain = new ProgressSection { HallsPreserved = [Site], HallsStopped = [Site] };
            var after = new ProgressSection
            {
                HallsPreserved = [Site], HallsStopped = [Site],
                ShuttleSeen = [Site], Shuttle = row with { Wired = true },
            };
            JsonElement a = JsonSerializer.SerializeToElement(plain);
            JsonElement b = JsonSerializer.SerializeToElement(after);
            var onlyB = b.EnumerateObject().Where(p => !a.TryGetProperty(p.Name, out JsonElement v)
                || v.GetRawText() != p.Value.GetRawText()).Select(p => p.Name).OrderBy(n => n).ToArray();
            Assert.Equal(["shuttle", "shuttleseen"], onlyB.Select(n => n.ToLowerInvariant()).OrderBy(n => n).ToArray());
            foreach (JsonProperty p in a.EnumerateObject())
            {
                Assert.Equal(p.Value.GetRawText(), b.GetProperty(p.Name).GetRawText());
            }
        }
        finally
        {
            PreservationZone.Install(preservedBefore);
            StopOrder.Install(stoppedBefore);
        }
    }

    /// <summary>
    /// NULL WHILE EMPTY: a vault in which the beat has not fired carries neither row, so no old save's
    /// checksum moves.
    /// </summary>
    [Fact]
    public void AVaultThatNeverSawTheBeatCarriesNeitherRow()
    {
        string json = JsonSerializer.Serialize(new ProgressSection());
        Assert.DoesNotContain("huttle", json, StringComparison.OrdinalIgnoreCase);
    }
}

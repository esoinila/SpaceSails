using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// ROUND 2 / U03 (#1417) · the navigator's findings, each driven on the shipping page (or, where the fault is a
/// stylesheet or a row of markup that no off-browser renderer can lay out, read from the source that draws it).
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRoundTwoNavigatorTests
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static string Src(params string[] parts) =>
        File.ReadAllText(Path.Combine(SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client", Path.Combine(parts)));

    // ── F03 · no ETA to a body the coast never reaches ──────────────────────────────────────────────

    [Fact]
    public async Task AnEtaIsQuotedOnlyForAPassInsideCaptureRange()
    {
        DeskBench b = await DeskBench.BootAsync("/map?dest=saturn");
        double now = (double)b.Peek("SimTime")!;
        ClosestApproach.Pass Pass(double distance) =>
            new("saturn", "Saturn", 5.8e7, distance, now + (30 * 86400.0), Vector2d.Zero);

        b.Poke("_destinationPass", Pass(1.3e12));   // 8.9 AU off: the coast misses the ring by a continent
        Assert.Null(b.Call("DestinationEta"));

        b.Poke("_destinationPass", Pass(1.0e6));    // inside the ring: a real arrival
        Assert.StartsWith("ETA ", (string?)b.Call("DestinationEta"));
    }

    // ── F06 · the "+ Add … at scrub" buttons name a place the ship can really be brought to ──────────

    [Fact]
    public async Task TheAddAtScrubButtonsDoNotOfferABodyThirtyFiveAuAway()
    {
        DeskBench b = await DeskBench.BootAsync("/map?sling=jupiter");
        double now = (double)b.Peek("SimTime")!;
        Type kindType = typeof(ArrivalStepRule.ArrivalKind);
        object orbit = Enum.Parse(kindType, "Orbit"), dock = Enum.Parse(kindType, "Dock");

        b.Poke("_passes", new List<ClosestApproach.Pass>
        {
            new("triton", "Triton", 1.35e6, 5.27e12, now + (6 * 86400.0), Vector2d.Zero),   // 35 AU: the ribbon's far end
        });
        Assert.Null(b.Call("ArriveCandidate", orbit));
        Assert.Null(b.Call("ArriveCandidate", dock));

        b.Poke("_passes", new List<ClosestApproach.Pass>
        {
            new("triton", "Triton", 1.35e6, 5.27e12, now + (6 * 86400.0), Vector2d.Zero),
            new("jupiter", "Jupiter", 7.0e7, 1.0e7, now + (20 * 86400.0), Vector2d.Zero),
        });
        var offered = (ClosestApproach.Pass?)b.Call("ArriveCandidate", orbit);
        Assert.Equal("jupiter", offered?.BodyId);
    }

    // ── F08 · the drill's complications belong to the bore ─────────────────────────────────────────

    [Fact]
    public void ADrillBitCannotSnapInAHoleThatWasNeverBegun()
    {
        int snapsAtZero = 0, snapsWithDepth = 0;
        foreach (RockComposition composition in Enum.GetValues<RockComposition>())
        {
            var type = new RockType(composition);
            for (int ordinal = 0; ordinal < 400; ordinal++)
            {
                ulong seed = DeflectionGig.Seed(1_000.0 + ordinal, $"rock-{composition}", ordinal);
                if (DeflectionGig.Roll(seed, type, ordinal, drillProgress: 0.0).Band == DeflectionBand.DrillSnap)
                {
                    snapsAtZero++;
                }
                if (DeflectionGig.Roll(seed, type, ordinal, drillProgress: 0.4).Band == DeflectionBand.DrillSnap)
                {
                    snapsWithDepth++;
                }
            }
        }

        Assert.True(snapsWithDepth > 0, "premise: with depth banked the pool still snaps bits");
        Assert.Equal(0, snapsAtZero);
    }

    private static readonly Type ExType = typeof(Map).GetNestedType("SurfaceExcursion", Any)!;

    private static Map MapOf(DeskBench b) =>
        (Map)typeof(DeskBench).GetField("_map", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(b)!;

    private static object Get(object o, string prop) => o.GetType().GetProperty(prop, Any)!.GetValue(o)!;

    private static void Set(object o, string prop, object? v) => o.GetType().GetProperty(prop, Any)!.SetValue(o, v);

    /// <summary>The rock's excursion, built the way the benches build one (the parameterless constructor), the
    /// captain set down at the DRILL POINT's square or back at the shuttle, and the clock far enough along that
    /// several beats have come due.</summary>
    private static async Task<(DeskBench B, object Ex)> OnTheRockAsync(bool atTheBore)
    {
        DeskBench b = await DeskBench.BootAsync("/map?deflection=1");
        Assert.NotNull(b.Peek("_deflection"));   // premise: the gig is accepted
        object ex = Activator.CreateInstance(ExType, nonPublic: true)!;
        Set(ex, "Deflection", true);
        var ephemeris = (SpaceSails.Core.ICelestialEphemeris)b.Peek("_ephemeris")!;
        CelestialBody rock = System.Linq.Enumerable.First(ephemeris.Bodies, x => x.Id == DeflectionGig.BodyId);
        Set(ex, "Stop", Activator.CreateInstance(typeof(Map).GetNestedType("ShuttleStop", Any)!, rock, 1e6, 600.0, false, true, false));
        Set(ex, "RestoreHavenId", "ringside");
        Set(ex, "DeflectionOnSiteSeconds", DeflectionGig.EventCadenceSeconds * 6.5);
        b.Poke("_surface", ex);
        var spot = ((float X, float Y))typeof(Map).GetMethod("DrillPointSpot", Any)!.Invoke(null, null)!;
        b.Poke("_avatarX", atTheBore ? (double)spot.X : 0.0);
        b.Poke("_avatarY", atTheBore ? (double)spot.Y : 5.0);
        Assert.Equal(atTheBore, !(bool)typeof(Map).GetProperty("CaptainBeyondReach", Any)!.GetValue(MapOf(b))!);
        return (b, ex);
    }

    [Fact]
    public async Task NothingIsRolledOnTheRockUntilABoreIsUnderway()
    {
        var (b, ex) = await OnTheRockAsync(atTheBore: true);
        b.CallOnTheDispatcher("StepDeflection", 0.016);
        Assert.Equal(0.0, (double)Get(ex, "DrillProgress"));
        Assert.Null(((PulseSlot)b.Peek("_pulse")!).Message);
        Assert.Equal(5, (int)Get(ex, "DeflectionLastOrdinal"));   // the six due beats were passed over, not stored up
    }

    [Fact]
    public async Task NothingIsRolledWhileTheCaptainIsBackAtTheShuttleEvenWithDepthBanked()
    {
        var (b, ex) = await OnTheRockAsync(atTheBore: false);
        Set(ex, "DrillProgress", 0.5);
        b.CallOnTheDispatcher("StepDeflection", 0.016);
        Assert.Equal(0.5, (double)Get(ex, "DrillProgress"));
        Assert.Null(((PulseSlot)b.Peek("_pulse")!).Message);
    }

    [Fact]
    public async Task OnceTheBoreIsUnderwayTheBeatsRollAsTheyAlwaysDid()
    {
        var (b, ex) = await OnTheRockAsync(atTheBore: true);
        Set(ex, "DrillProgress", 0.5);
        b.CallOnTheDispatcher("StepDeflection", 0.016);
        Assert.True((int)Get(ex, "DeflectionLastOrdinal") >= 0);
        Assert.NotNull(((PulseSlot)b.Peek("_pulse")!).Message);   // a complication was told
    }

    // ── F07 · the rock's HUD line points at the DRILL POINT ────────────────────────────────────────

    [Fact]
    public async Task TheRocksHudLineNamesTheDrillPointAndWhichWayItLies()
    {
        var (b, _) = await OnTheRockAsync(atTheBore: false);
        var line = ((string Line, int Severity)?)b.Call("DeflectionComms");
        Assert.NotNull(line);
        Assert.Contains("DRILL POINT", line!.Value.Line);
        Assert.Matches(@"DRILL POINT [0-9]+ du (N|S|E|W|NE|NW|SE|SW) ", line.Value.Line);
        Assert.DoesNotContain("land and drill", line.Value.Line);
    }

    // ── F19 · a clamped ship has no orbit readout to freeze ─────────────────────────────────────────

    [Theory]
    [InlineData(true, false)]    // clamped at a berth: the teaching waits
    [InlineData(false, true)]    // free-flying: it is said
    public async Task TheFirstLossNoticeIsForAFreeFlyingHull(bool clamped, bool announced)
    {
        var (b, ex) = await OnTheRockAsync(atTheBore: false);
        b.Poke("_dockedHavenId", clamped ? "ringside" : null);
        Set(ex, "CommsActive", true);
        Set(ex, "CommsEpisodeStart", 0.0);
        Set(ex, "CommsEpisodeDuration", 1000.0);
        Set(ex, "CommsEpisodeDeepens", true);
        Set(ex, "CommsSeconds", 500.0);
        Set(ex, "CommsNextOnset", 0.0);
        b.CallOnTheDispatcher("StepComms", 0.016);
        Assert.NotEqual(CommsLink.Phase.Nominal, (CommsLink.Phase)Get(ex, "CommsPhase"));   // premise: the link is down
        Assert.Equal(announced, (bool)Get(ex, "CommsFirstLossAnnounced"));
    }

    // ── F13 / F17 / F14 · what only a stylesheet or a row of markup can say ────────────────────────

    [Fact]
    public void ALongNowLineWrapsInsideTheBannerInsteadOfWideningIt()
    {
        string css = Src("Pages", "Map", "TopBannerStack.razor.css");
        int at = css.IndexOf(".pilot-banner-mode {", StringComparison.Ordinal);
        Assert.True(at >= 0, "premise: the banner's mode rule exists");
        string rule = css[at..css.IndexOf('}', at)];
        Assert.DoesNotContain("nowrap", rule);
    }

    [Fact]
    public void TheRevealCardsCloseIsNotPaintedOverByShowMesScrim()
    {
        string css = Src("Pages", "Map", "MissionMomentCard.razor.css");
        int at = css.IndexOf("::deep .mission-moment > button:last-child", StringComparison.Ordinal);
        Assert.True(at >= 0, "the moment card must take its last button out of the celebration's pinned foot");
        string rule = css[at..css.IndexOf('}', at)];
        Assert.Contains("box-shadow: none", rule);
        Assert.Contains("position: static", rule);
    }

    [Fact]
    public void AnInvalidArriveVerdictIsPrintedOnceNotOnceOnTheRowAndOnceInTheEditor()
    {
        string row = Src("Pages", "Map", "NavHud", "ArriveStepRow.razor");
        Assert.Contains("@if (_ac is { Valid: true } _ace)", row);   // the open editor repeats only the green line
        Assert.Contains("@if (_ac is { } _acx && !_acx.Valid)", row); // the row keeps the red one
    }
}

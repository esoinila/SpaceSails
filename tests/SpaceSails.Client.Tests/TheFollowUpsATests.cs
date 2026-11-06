using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// Follow-ups A · #1440 (a load leaves the old life's clocks alive, Esc on Kolt's card, the Trade tile's count), #1458
/// (the door sentry cannot be lifted), #1459 (no destination, no Mercury) and #1432 (the pass is filed when the tide
/// took him). Each is driven on the shipping page; the red proof for each is in the test's own summary.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFollowUpsATests
{
    private const string Luna = "/map?dock=selene-gate&body=luna&site=0&land=1";

    // ── #1440 · a load folds the old life's clocks away ─────────────────────────────────────────────

    /// <summary><b>Proven RED</b> by removing the #1440 block from ForgetThePageSessionRiders: the charges stay armed (and run
    /// the loaded ship out), the derelict clock stays set, and the panels and sweepers stand.</summary>
    [Fact]
    public void ALoadDoesNotLeaveTheOldLifesChargesAndClocksRunning()
    {
        Pages.Map map = Boot("followups-a-load");
        Set(map, "_shipChargesSeconds", 12.0);
        Set(map, "_scuttleSecondsLeft", 30.0);
        Set(map, "_showScuttlePanel", true);
        Set(map, "_showShipScuttlePanel", true);
        Set(map, "_shipScuttleWordGiven", true);
        Set(map, "_airSupplyNoted", SuitAir.Supply.Room);
        var sweepers = (IList)Read(map, "_sweepers")!;
        sweepers.Add(Activator.CreateInstance(sweepers.GetType().GetGenericArguments()[0], nonPublic: true));
        Assert.True((bool)Read(map, "SomethingElseIsCountingDown")!, "control: the countdown is up");

        Invoke(map, "ApplyVault", new Vault());

        Assert.Null(Read(map, "_shipChargesSeconds"));
        Assert.Null(Read(map, "_scuttleSecondsLeft"));
        Assert.False((bool)Read(map, "_showScuttlePanel")!);
        Assert.False((bool)Read(map, "_showShipScuttlePanel")!);
        Assert.False((bool)Read(map, "_shipScuttleWordGiven")!);
        Assert.Null(Read(map, "_airSupplyNoted"));
        Assert.Empty(sweepers);
        Assert.False((bool)Read(map, "SomethingElseIsCountingDown")!);

        // And the clock does not wake under the loaded life: run past the old overload and she is still here.
        RunFrames(map, 20);
        Assert.Null(Read(map, "_shipChargesSeconds"));
        Assert.Null(Read(map, "_busted"));
    }

    /// <summary><b>Proven RED</b> by deleting the _hardcaseCard rung from TryDismissTopOverlay: Esc returns false and the card stays.</summary>
    [Fact]
    public void EscapeIsKoltsNo()
    {
        Pages.Map map = Boot("followups-a-kolt");
        Set(map, "_hardcaseCard", HardcaseRep.OffersFor(InsuranceTier.None));
        Assert.NotNull(Read(map, "_hardcaseCard"));

        Assert.True((bool)Invoke(map, "TryDismissTopOverlay")!);

        Assert.Null(Read(map, "_hardcaseCard"));
        Assert.True((bool)Read(map, "_hardcaseRefused")!);
    }

    // ── #1458 · the door sentry is the building's ───────────────────────────────────────────────────

    /// <summary><b>Proven RED</b> by removing the IsDoorSentry skip from DeployOrRetrieveSentry: GATE-1 goes back in the sling.</summary>
    [Fact]
    public async Task TWithTheDoorSentryUnderfootLeavesItStandingEvenWithBotsInTheSling()
    {
        DeskBench b = await DeskBench.BootAsync(Luna);
        var ex = (Pages.Map.SurfaceExcursion)b.Peek("_surface")!;
        Pages.Map.SurfaceBot gate = ex.Bots.First(x => SurfaceArrival.IsDoorSentry(x.Unit));
        Assert.Contains(ex.Bots, x => !x.Deployed);   // control: the sling carries a bot
        b.Poke("_avatarX", gate.X);
        b.Poke("_avatarY", gate.Y);

        b.Call("DeployOrRetrieveSentry", false);

        Assert.True(gate.Deployed, "GATE-1 was shouldered");
        Assert.DoesNotContain("shouldered", b.Pulse, StringComparison.Ordinal);
    }

    /// <summary><b>Proven RED</b> by removing the IsDoorSentry term from both prompt checks: the keybar offers "pick up the sentry".</summary>
    [Fact]
    public async Task TheKeybarDoesNotOfferToPickUpTheDoorSentry()
    {
        DeskBench b = await DeskBench.BootAsync(Luna);
        var ex = (Pages.Map.SurfaceExcursion)b.Peek("_surface")!;
        Pages.Map.SurfaceBot gate = ex.Bots.First(x => SurfaceArrival.IsDoorSentry(x.Unit));
        ex.Bots.RemoveAll(x => !SurfaceArrival.IsDoorSentry(x.Unit));   // nothing in the sling: the other prompt arm
        b.Poke("_avatarX", gate.X);
        b.Poke("_avatarY", gate.Y);
        Assert.DoesNotContain("pick up the sentry", (string)b.Call("BuildSurfaceKeyHints", ex)!, StringComparison.Ordinal);

        // control: a bot the captain set down IS offered.
        ex.Bots.Add(new Pages.Map.SurfaceBot { Unit = "BOT-9", Rounds = 5, Deployed = true, X = gate.X, Y = gate.Y });
        Assert.Contains("pick up the sentry", (string)b.Call("BuildSurfaceKeyHints", ex)!, StringComparison.Ordinal);
    }

    // ── #1459 · no destination, no Mercury ──────────────────────────────────────────────────────────

    /// <summary><b>Proven RED</b> by restoring the bare <c>_destinationBodyId is null ||</c> arm: the far edge pass listed first wins the tie.</summary>
    [Fact]
    public async Task WithNoDestinationOnlyTheNearestEdgePassIsOffered()
    {
        DeskBench b = await DeskBench.BootAsync("/map?start=wreck");
        Assert.Null(b.Peek("_destinationBodyId"));
        var samples = (IList)b.Peek("_samples")!;
        Assert.True(samples.Count > 2, "control: there is a ribbon");
        double edge = ((TrajectorySample)samples[^1]!).SimTime;
        var eph = (ICelestialEphemeris)b.Peek("_ephemeris")!;
        CelestialBody far = eph.Bodies.First(x => x.Id == "uranus"), near = eph.Bodies.First(x => x.Id == "mercury");
        object orbit = Enum.Parse(typeof(ArrivalStepRule.ArrivalKind), "Orbit");

        // The far body is listed FIRST at the same edge time: only list order would hand it the tie.
        b.Poke("_passes", new List<ClosestApproach.Pass>
        {
            new(far.Id, far.Name, far.BodyRadius, 3.0e12, edge, Vector2d.Zero),
            new(near.Id, near.Name, near.BodyRadius, 1.3e11, edge, Vector2d.Zero),
        });
        Assert.Equal("mercury", ((ClosestApproach.Pass?)b.Call("ArriveCandidate", orbit))?.BodyId);

        b.Poke("_passes", new List<ClosestApproach.Pass>
        {
            new(near.Id, near.Name, near.BodyRadius, 1.3e11, edge, Vector2d.Zero),
            new(far.Id, far.Name, far.BodyRadius, 3.0e12, edge, Vector2d.Zero),
        });
        Assert.Equal("mercury", ((ClosestApproach.Pass?)b.Call("ArriveCandidate", orbit))?.BodyId);   // and the order is not what decided it
    }

    /// <summary>The real scene, real passes: whatever edge pass is offered is the closest-passing arrivable one, for each kind.</summary>
    [Fact]
    public async Task OnTheRealWreckSceneTheEdgeOfferIsTheNearestEdgePass()
    {
        DeskBench b = await DeskBench.BootAsync("/map?start=wreck");
        Assert.Null(b.Peek("_destinationBodyId"));
        b.Poke("_passDirty", true);
        b.CallOnTheDispatcher("ReprojectThePassesOnTheirCadence", 1e12);
        var passes = ((IEnumerable<ClosestApproach.Pass>)b.Peek("_passes")!).ToList();
        Assert.NotEmpty(passes);
        var end = ((double EndSimTime, double SampleStepSeconds)?)b.Call("RibbonEnd");
        Assert.NotNull(end);
        var edgePasses = passes.Where(p => ArrivalStepRule.PassIsOffTheEndOfTheRibbon(p.SimTime, end!.Value.EndSimTime, end.Value.SampleStepSeconds)).ToList();
        var eph = (ICelestialEphemeris)b.Peek("_ephemeris")!;
        object orbit = Enum.Parse(typeof(ArrivalStepRule.ArrivalKind), "Orbit");
        var orbitable = edgePasses.Where(p => eph.Bodies.First(x => x.Id == p.BodyId) is { ParentId: not null, Kind: not BodyKind.Station, Mu: > 0 }).ToList();
        if (b.Call("ArriveCandidate", orbit) is ClosestApproach.Pass offered && orbitable.Any(p => p.BodyId == offered.BodyId))
        {
            Assert.True(orbitable.All(p => p.Distance >= offered.Distance), $"{offered.BodyId} is not the nearest edge pass");
        }
    }

    // ── #1432 · the pass is filed when the tide took him ────────────────────────────────────────────

    /// <summary><b>Proven RED</b> by removing the Taken arm from TheManAtTheDoorSeesYouGo: the ride from the surface to below his
    /// floor files nothing. Controls: with him NOT taken the same ride files nothing (he was not passed, he was not there).</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ARideFromAboveHisFloorToBelowItFilesThePassOnlyWhenTheTideTookHim(bool taken)
    {
        (string body, int floor) = TheManAtTheDoorTests.ASiteWithADoor();
        Pages.Map map = TheManAtTheDoorTests.OnHisFloor(body, floor, GateGuard.Posting.Absent);
        int below = TheManAtTheDoorTests.Rows(map).First(r => r.Level < floor).Level;
        Pages.Map.SurfaceExcursion ex = TheManAtTheDoorTests.Ex(map);
        ex.Gate!.Taken = taken;
        ex.Floor = 0;   // the captain is up on the surface

        Invoke(map, "RideTheLiftTo", ex, below, null, false);

        bool filed = TheManAtTheDoorTests.Register(map).Contains(GateGuard.PastTag(ex.Gate.Ground));
        Assert.Equal(taken, filed);
    }
}

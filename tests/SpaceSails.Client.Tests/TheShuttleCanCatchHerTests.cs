using System;
using System.Threading.Tasks;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #336 · <b>FLY HOME TO ANY SHIP THE SHUTTLE CAN CATCH.</b>
///
/// <para>Owner ruling (2026-07-18), verbatim: <i>"the shuttle ship link should not break even if the ship
/// undocks, because the docking is not requisite for the ship to stay in vicinity. As long as the ship is in
/// shuttle range (shown on map) and is not moving too fast away, we should be able to fly back to it from a
/// landing site."</i></para>
///
/// <para><see cref="SpaceSails.Core.Tests"/>' twin holds the pure law. This file stands a captain on Miranda
/// — the real boot, the real descent, through <see cref="DeskBench"/> — and then MOVES THE MOTHERSHIP, which
/// is the only way to ask the shipping page the four questions that matter: can he go home, what is she
/// saying to him, what is drawn on his instrument, and are those three the same situation.</para>
///
/// <para><b>Proven red on revert</b> (quoted in the PR body): take the <c>TheBoatWillFlyItUp()</c> gate back
/// out of <c>LiftOffFromSurface</c> and the two refusal laws go red while everything else stays green; put
/// the ladder back on the orbit hold and the three range laws go red; drop <c>ShuttleLegs</c> from the HUD
/// and the ring law goes red on its own.</para>
/// </summary>
[SlowGate]
public sealed class TheShuttleCanCatchHerTests
{
    /// <summary>The one world this file works in: clamped at The Tilt, boots on Miranda's canon ground. The
    /// same row <see cref="EveryDeskBootsTests"/> uses for "on the ground at Miranda", so the bench's own
    /// horizon is already measured for it.</summary>
    private const string OnMirandasGround = "/map?dock=the-tilt&site=0&land=1";

    private static async Task<DeskBench> AshoreAsync()
    {
        DeskBench bench = await DeskBench.BootAsync(OnMirandasGround);
        Assert.True(bench.OnSurface, $"{OnMirandasGround} did not put the captain on any ground at all.");
        return bench;
    }

    // ── Moving the mothership, which is the whole instrument of this file ───────────────────────────────

    /// <summary>Where the ground under the captain's boots is, right now.</summary>
    private static Vector2d TheGround(DeskBench bench)
    {
        var ephemeris = (ICelestialEphemeris)bench.Peek("_ephemeris")!;
        object excursion = bench.Peek("_surface")!;
        object stop = excursion.GetType().GetProperty("Stop")!.GetValue(excursion)!;
        var body = (CelestialBody)stop.GetType().GetProperty("Body")!.GetValue(stop)!;
        return ephemeris.Position(body.Id, (double)bench.Peek("SimTime")!);
    }

    /// <summary>Put the mothership <paramref name="hops"/> shuttle-hops off the ground, making
    /// <paramref name="catches"/> catch-speeds relative to it — and, because #336's whole claim is that this
    /// is the only thing that matters, optionally take her clamp away as well.</summary>
    private static void PutHer(DeskBench bench, double hops, double catches, bool undock = false)
    {
        var ephemeris = (ICelestialEphemeris)bench.Peek("_ephemeris")!;
        object excursion = bench.Peek("_surface")!;
        object stop = excursion.GetType().GetProperty("Stop")!.GetValue(excursion)!;
        var body = (CelestialBody)stop.GetType().GetProperty("Body")!.GetValue(stop)!;
        double simTime = (double)bench.Peek("SimTime")!;

        Vector2d ground = TheGround(bench);
        Vector2d groundVelocity = TransferMath.BodyVelocity(ephemeris, body.Id, simTime);

        // Outward from the Sun, so "further away" and "receding" are the same direction and a test that asks
        // for both gets a geometry that really does both.
        Vector2d outward = ground == Vector2d.Zero ? new Vector2d(1, 0) : ground.Normalized();

        Vector2d where = ground + (outward * (ShuttleRange.RangeMeters * hops));
        bench.Poke("_ship", new ShipState(
            where, groundVelocity + (outward * (ShuttleRange.CatchSpeedMps * catches)), simTime));
        bench.Poke("SimTime", simTime);

        if (undock)
        {
            bench.Poke("_dockedHavenId", null);
        }
        else if (bench.Peek("_dockedHavenId") is string berth)
        {
            // A CLAMPED ship is anchored to her berth's rail and not to her own hull (ShipAnchorAt), which is
            // exactly right in the game and would make a poked _ship invisible here. The berth's ARM is the
            // one thing a bench may honestly move, so the standoff is set to put the clamp itself where this
            // method was asked to put her — the same field the docking clamp writes (Map.Docking.Berth).
            bench.Poke("_dockOffset", where - ephemeris.Position(berth, simTime));
        }

        // The window scans cache their promises on the absolute sim time they were taken at, which is exactly
        // right in a running game and exactly wrong for a bench that teleports a ship. Cleared so every
        // question below is asked of the geometry this method just laid down.
        ((System.Collections.IDictionary)bench.Peek("_awayWindowScans")!).Clear();
    }

    private static ShuttleLink.Refusal AskTheBoat(DeskBench bench) =>
        (ShuttleLink.Refusal)bench.Call("AskTheBoatForTheRideHome")!;

    private static string TheShipSays(DeskBench bench)
    {
        object? line = bench.Call("SurfaceOrbitComms");
        return line is null ? "" : (string)((System.ValueTuple<string, int>)line).Item1;
    }

    private static int TheShipSaysHowLoudly(DeskBench bench)
    {
        object? line = bench.Call("SurfaceOrbitComms");
        return line is null ? 0 : ((System.ValueTuple<string, int>)line).Item2;
    }

    private static (double RangeFraction, int Rung)? TheRing(DeskBench bench) =>
        ((double, int)?)bench.Call("TheShuttleLegsRing") is { } ring ? (ring.Item1, ring.Item2) : null;

    // ── 1 · UNDOCKING, DRIFTING AND A DEGRADED ORBIT DO NOT BREAK THE LINK ──────────────────────────────

    [Fact]
    public async Task TheRideHomeSurvivesTheClampBeingLetGo()
    {
        DeskBench bench = await AshoreAsync();

        // Close alongside and course-matched, but nobody is clamped to anything, nobody is keeping the orbit,
        // and the tank that would pay for the keeping is empty. Every one of those used to be the story; none
        // of them is the question.
        PutHer(bench, hops: 0.2, catches: 0.0, undock: true);
        bench.Poke("_orbitKept", false);
        bench.Poke("_reactionMassPulses", 0);

        Assert.False(bench.Docked, "the bench failed to let the clamp go — the law below would prove nothing.");
        Assert.Equal(ShuttleLink.Refusal.None, AskTheBoat(bench));

        bench.CallOnTheDispatcher("LiftOffFromSurface");
        Assert.False(bench.OnSurface, "an undocked, drifting, orbit-degraded ship a fifth of a hop away "
            + "refused to be flown to. That is #336's bug exactly.");
    }

    [Fact]
    public async Task AndItSurvivesTheORBITBeingLostWhileSheStaysAlongside()
    {
        DeskBench bench = await AshoreAsync();

        PutHer(bench, hops: 0.6, catches: 0.05, undock: true);
        bench.Poke("_orbitKept", false);
        bench.Poke("_orbitHoldAtBoarding", 3600.0);   // he boarded down with an hour; the keeper has since given up

        Assert.Equal(ShuttleLink.Refusal.None, AskTheBoat(bench));

        // …and she says so calmly, which is the sentence the old code could not say: "the ship has slipped
        // its orbit — it's adrift" was an alarm about a situation the captain can simply fly out of.
        Assert.Equal(ShuttleLink.Calm, TheShipSays(bench));
        Assert.Equal(0, TheShipSaysHowLoudly(bench));
    }

    // ── 2 · …AND IT BREAKS, WITH A REASON, WHEN SHE CANNOT BE CAUGHT ────────────────────────────────────

    [Fact]
    public async Task BeyondTheLegsTheBoatREFUSESAndSaysWhy()
    {
        DeskBench bench = await AshoreAsync();

        PutHer(bench, hops: 3.0, catches: 0.0, undock: true);

        Assert.Equal(ShuttleLink.Refusal.BeyondRange, AskTheBoat(bench));

        bench.CallOnTheDispatcher("LiftOffFromSurface");
        Assert.True(bench.OnSurface, "the boat flew to a ship three hops away.");
        Assert.Contains(ShuttleLink.RefusalLine(ShuttleLink.Refusal.BeyondRange), bench.Pulse, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AndAboveTheCatchSpeedSheREFUSESForTheOtherReason()
    {
        DeskBench bench = await AshoreAsync();

        // Well inside the legs, and parting company at four times what the boat can fly.
        PutHer(bench, hops: 0.15, catches: 4.0, undock: true);

        Assert.Equal(ShuttleLink.Refusal.TooFastToCatch, AskTheBoat(bench));

        bench.CallOnTheDispatcher("LiftOffFromSurface");
        Assert.True(bench.OnSurface, "the boat came alongside a hull parting company at 32 km/s.");
        Assert.Contains(ShuttleLink.RefusalLine(ShuttleLink.Refusal.TooFastToCatch), bench.Pulse, StringComparison.Ordinal);
        Assert.NotEqual(
            ShuttleLink.RefusalLine(ShuttleLink.Refusal.BeyondRange),
            ShuttleLink.RefusalLine(ShuttleLink.Refusal.TooFastToCatch));
    }

    [Fact]
    public async Task ADOCKEDShipThatCannotBeCaughtIsStillRefused()
    {
        DeskBench bench = await AshoreAsync();

        // The mirror of the first law, and the half that stops "dock state does not matter" from quietly
        // becoming "dock state means yes". A clamp is not a ride: if the berth she is tied to has carried her
        // past the boat's legs, the captain is no better off than if she had drifted there.
        PutHer(bench, hops: 5.0, catches: 0.0);
        Assert.True(bench.Docked, "the bench lost the clamp — this law is specifically about keeping it.");

        Assert.Equal(ShuttleLink.Refusal.BeyondRange, AskTheBoat(bench));
        bench.CallOnTheDispatcher("LiftOffFromSurface");
        Assert.True(bench.OnSurface);
    }

    // ── 3 · THE LADDER SPEAKS RANGE ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheLadderStageFollowsTheRANGEBandAndNotTheTank()
    {
        DeskBench bench = await AshoreAsync();

        // CALM — inside the legs, and the gap is not opening.
        PutHer(bench, hops: 0.3, catches: 0.0, undock: true);
        Assert.Equal(ShuttleLink.Calm, TheShipSays(bench));
        Assert.Equal(0, TheShipSaysHowLoudly(bench));

        // AMBER — inside the legs, and there is now a clock on it. She is receding at a tenth of what the
        // boat can fly, so she is catchable this minute and will not be for ever.
        PutHer(bench, hops: 0.9, catches: 0.1, undock: true);
        Assert.Equal(ShuttleLink.Amber, TheShipSays(bench));
        Assert.Equal(1, TheShipSaysHowLoudly(bench));

        // LOST — past the legs, on a straight coast that brings her back to nobody.
        PutHer(bench, hops: 2.5, catches: 0.1, undock: true);
        Assert.Equal(ShuttleLink.Maroon, TheShipSays(bench));
        Assert.Equal(2, TheShipSaysHowLoudly(bench));
    }

    [Fact]
    public async Task AndAnAMBERRangeOutranksEvenTheDOCKEDLine()
    {
        DeskBench bench = await AshoreAsync();

        // The third named bug class, pointed at this lane: a strip reading "docked — the station holds the
        // ship" while the ground under the captain's boots carries him out of reach is the picture and the
        // sim saying different things about the one fact that decides whether he gets home.
        PutHer(bench, hops: 0.9, catches: 0.1);
        Assert.True(bench.Docked);
        Assert.Equal(ShuttleLink.Amber, TheShipSays(bench));

        // …and the calm case still says the docked line, which is the anti-vacuous half: a strip that had
        // simply stopped reading the clamp would pass the row above and would have deleted #331's own ruling.
        PutHer(bench, hops: 0.1, catches: 0.0);
        Assert.Equal(OrbitHold.DockedComms, TheShipSays(bench));
    }

    [Fact]
    public async Task TheMAROONIsANNOUNCEDAndTheCaptainIsStillAlive()
    {
        DeskBench bench = await AshoreAsync();

        PutHer(bench, hops: 6.0, catches: 0.2, undock: true);

        // Announced — the maroon canon's one forbidden outcome is silence.
        Assert.Equal(ShuttleLink.Maroon, TheShipSays(bench));
        Assert.Equal(2, TheShipSaysHowLoudly(bench));

        // …and survivable. He is still on his feet, on his ground, with his excursion running and nothing
        // resolved against him: the ride is gone, the captain is not.
        Assert.True(bench.OnSurface);
        Assert.Null(bench.Peek("_busted"));

        // Pressing the airlock says WHY in the boat's own voice, and does not repeat the maroon at him.
        bench.CallOnTheDispatcher("LiftOffFromSurface");
        Assert.True(bench.OnSurface);
        Assert.Contains(ShuttleLink.RefusalLine(ShuttleLink.Refusal.BeyondRange), bench.Pulse, StringComparison.Ordinal);
        Assert.DoesNotContain(ShuttleLink.Maroon, bench.Pulse, StringComparison.Ordinal);
    }

    // ── 4 · THE RING IS EXPOSED, AND IT AGREES WITH THE OTHER TWO ───────────────────────────────────────

    [Fact]
    public async Task TheRingIsHandedDownForTheRENDERAndAgreesWithTheLadder()
    {
        DeskBench bench = await AshoreAsync();

        foreach ((double hops, int expectedRung) in new[] { (0.25, 0), (0.85, 1), (4.0, 2) })
        {
            PutHer(bench, hops, catches: 0.05, undock: true);

            (double RangeFraction, int Rung)? ring = TheRing(bench);
            Assert.NotNull(ring);

            // The fraction is the honest gap over one hop, so the drawn radius cannot claim a reach the boat
            // does not have — #212/#253's visible-geometry law, which is about the NUMBER and not only about
            // whether something is on the screen.
            Assert.Equal(hops, ring!.Value.RangeFraction, 3);
            Assert.Equal(expectedRung, ring.Value.Rung);
        }
    }

    [Fact]
    public async Task AndThereIsNoRingToDrawWhenNobodyIsAshore()
    {
        // The anti-vacuous half: a ring that was always non-null would pass every row above and would paint
        // an away-team instrument over a captain sitting at his own Nav desk.
        DeskBench aboard = await DeskBench.BootAsync("/map?dock=the-tilt");
        Assert.False(aboard.OnSurface);
        Assert.Null(TheRing(aboard));
        Assert.Equal(ShuttleLink.Refusal.None, AskTheBoat(aboard));
    }

    [Fact]
    public async Task TheRingTheLadderAndTheBoatAreOneMeasurement()
    {
        DeskBench bench = await AshoreAsync();

        // The whole file in one law. Walk her out in ten steps and require, at every one, that what is DRAWN,
        // what is SAID and what the boat DOES are the same situation. Three instruments reading one number is
        // the only defence against the third named bug class, and a range ring is the most exposed thing in
        // the game to it.
        int calm = 0, amber = 0, lost = 0, waits = 0;
        for (int step = 0; step <= 14; step++)
        {
            // A tenth of a hop a step, and never landing exactly ON the amber fraction: a band's boundary is
            // a floating-point coin toss and this law is about the WALK, not about which side of 0.75 the
            // last bit of a double falls.
            double hops = (step * 0.1) + 0.02;
            PutHer(bench, hops, catches: 0.1, undock: true);

            (double RangeFraction, int Rung)? ring = TheRing(bench);
            Assert.NotNull(ring);
            string said = TheShipSays(bench);
            ShuttleLink.Refusal boat = AskTheBoat(bench);

            switch (ring!.Value.Rung)
            {
                case 0:
                    calm++;
                    Assert.Equal(ShuttleLink.Calm, said);
                    Assert.Equal(ShuttleLink.Refusal.None, boat);
                    break;
                case 1:
                    amber++;
                    Assert.Equal(ShuttleLink.Amber, said);
                    Assert.Equal(ShuttleLink.Refusal.None, boat);
                    break;
                case 2:
                    lost++;
                    Assert.Equal(ShuttleLink.Maroon, said);
                    Assert.Equal(ShuttleLink.Refusal.BeyondRange, boat);
                    break;
                default:
                    // Rung 3 is a CLOSED window — out of reach now, and the rails bring it back. It is not a
                    // maroon and must never be said as one, but the boat still will not fly it.
                    waits++;
                    Assert.NotEqual(ShuttleLink.Maroon, said);
                    Assert.Equal(ShuttleLink.Refusal.BeyondRange, boat);
                    break;
            }

            // At every single step, the boat flies iff the ring says she is inside the legs.
            Assert.Equal(ring.Value.RangeFraction <= 1.0, boat == ShuttleLink.Refusal.None);
        }

        Assert.True(calm > 0 && amber > 0 && lost + waits > 0,
            $"the walk saw calm {calm}, amber {amber}, lost {lost}, waits {waits} — a sweep that never "
            + "changed its answer cannot tell a working ladder from one that is stuck.");
    }
}

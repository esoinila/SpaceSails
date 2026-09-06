using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1151 · <b>THE CLAIM IS THE SCENE — DRIVEN.</b> Owner ruling, 2026-09-06 on #525.
///
/// <para><b>Everything here goes through the game's own doors.</b> The hull is lost by turning both keys and
/// letting the clock run out; the kiosk is found by walking to it and pressing [E] through
/// <c>ViewNearbyObject</c>; the three presses are the counter's own rows, taken off <c>TheClaimAsks</c>; and
/// the payout is Harlan Fess's pitch going up. Nothing writes <c>_claimDesk</c>, <c>_claimOwed</c> or
/// <c>_credits</c> by hand. Reading the source is how the sibling issue got three different answers about
/// whether the castaway was even reachable.</para>
///
/// <para><b>And every guard is asked in a world that could answer the other way.</b> The presence law is
/// proved with a hunter that WOULD catch — parked on the captain's own state, so the aboard arm catches on
/// the first frame it is allowed to — and then the same hunter, in the same world, held by each of the three
/// ways of not being on her. The claim is proved against a purse that does not move until a salesman turns
/// up. The death arm is the same scuttle with the captain still on board.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed partial class TheClaimIsWalkedEndToEndTests
{

    // ══ 1 · THE PRESENCE LAW ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>NOTHING THAT TAKES THE HULL RESOLVES WHILE THE MASTER IS OFF HER</b> — the owner's own sentence,
    /// asked four times in one world.
    ///
    /// <para>A collector is put on her sitting exactly where she is, so on any frame she is allowed to close
    /// she has already closed. Aboard, that is what happens: one catch, one demand panel. Off her — on a
    /// surface, away in the boat, or past the tube on somebody's concourse — a hundred frames go by and she
    /// is still out there, not broken off and not holding anybody, because the process wants a ship and a
    /// captain in one place.</para>
    /// </summary>
    [Theory]
    [InlineData(Posture.Aboard, false)]
    [InlineData(Posture.OnASurface, true)]
    [InlineData(Posture.AwayInTheBoat, true)]
    [InlineData(Posture.AshorePastTheTube, true)]
    public void THE_WRIT_ResolvesOnlyWithTheMasterAboardHer(Posture posture, bool held)
    {
        Pages.Map map = Boot();
        PutHimIn(map, posture);
        PutACollectorOnTopOfHer(map, "GRIMHOLD");

        RunFrames(map, seconds: 10);

        if (!held)
        {
            Assert.NotNull(Read(map, "_busted"));
            Assert.Empty((IEnumerable)Read(map, "_hunters")!);   // caught, and retired off the roster
            return;
        }

        Assert.Null(Read(map, "_busted"));
        var hunters = (IList)Read(map, "_hunters")!;
        Assert.NotEmpty(hunters);
        Assert.False((bool)Get(hunters[0]!, "CaughtPlayer")!, "the writ was served with nobody to serve it on.");
        Assert.False((bool)Get(hunters[0]!, "BrokenOff")!, "the process did not wait, it gave up.");
    }

    /// <summary>
    /// <b>AND WAITING IS VISIBLE.</b> A held writ that nothing on any screen mentions is a pause, not a
    /// process — so the captain's own ledger carries the plate while somebody is out there unable to
    /// proceed, and carries no such row when the master is on his ship.
    /// </summary>
    [Fact]
    public void THE_LEDGER_CarriesThePendingPlateWhileHeIsOffHerAndNotWhenHeIsOnHer()
    {
        Pages.Map map = Boot();
        PutHimIn(map, Posture.OnASurface);
        PutACollectorOnTopOfHer(map, "GRIMHOLD");
        RunFrames(map, seconds: 5);

        object row = TheWritRow(map) ?? throw new InvalidOperationException(
            "a collector is holding station and the ledger says nothing about it.");
        Assert.Equal(NebulaClaims.PendingWritPlate, (string)Get(row, "Title")!);
        Assert.Contains("GRIMHOLD", (IEnumerable<string>)Get(row, "Lines")!);

        // …and the same world with him back on her: no row at all. A plate that was always there would be a
        // standing note, and this is a state of paperwork.
        Set(map, "_surface", null);
        Assert.Null(TheWritRow(map));
    }

    /// <summary>
    /// <b>#1090's BREAK-OFF IS NOT THE END OF THE PROCESS.</b> The castaway ending is untouched — the chase
    /// still stops, the roster still empties, nothing is said — and the contract goes onto the file, waiting
    /// at the port that serves the ground he did it over, which is <c>QuietHands.PortFor</c>'s harbour and
    /// not the ground itself.
    /// </summary>
    [Fact]
    public void THE_PURSUER_StopsChasingAndStartsWaitingAtThePortThatServesTheGround()
    {
        Pages.Map map = Boot();
        PutHimIn(map, Posture.OnASurface);
        PutACollectorOnTopOfHer(map, "GRIMHOLD");
        Assert.Null(Read(map, "_writPending"));

        string ground = TheGroundHeIsOn(map);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody harbour = QuietHands.PortFor(sky, ground)
            ?? throw new InvalidOperationException($"no harbour serves {ground} — the guard has no answer to check.");

        ArmHerCharges(map);
        RunUntilSheGoes(map);

        // The ending itself, unchanged: he lived, and nobody is chasing what is not there.
        Assert.Null(Read(map, "_busted"));
        Assert.NotNull(Read(map, "_shipEpitaph"));
        var hunters = (IList)Read(map, "_hunters")!;
        Assert.All(hunters.Cast<object>(), h => Assert.True((bool)Get(h, "BrokenOff")!));

        object writ = Read(map, "_writPending") ?? throw new InvalidOperationException(
            "the pursuers evaporated — nobody is waiting for him anywhere.");
        Assert.Equal("GRIMHOLD", (string)Get(writ, "Callsign")!);
        Assert.Equal(harbour.Id, (string)Get(writ, "HavenId")!);
        Assert.NotEqual(ground, (string)Get(writ, "HavenId")!);   // a ground has no berths to wait at
    }

    // ══ 2 · A DEATH PAYS AS BEFORE, AND ASKS NOBODY FOR ANYTHING ═════════════════════════════════════════

    /// <summary>
    /// <b>A DEATH IS AUTO-PROCESSED.</b> The same scuttle with the captain still standing on her: the death
    /// machinery runs, the insurance seam is consulted exactly where it always was, and no claim exists —
    /// nothing to lodge, nothing owed, no counter touched. The clause the whole feature hangs off.
    /// </summary>
    [Fact]
    public void A_DEATH_IsProcessedAndNeverBecomesAClaim()
    {
        Pages.Map map = Boot();
        Set(map, "_insurance", NebulaRep.PolicyAfterBuying(InsuranceTier.Premium, (double)Read(map, "SimTime")!));

        ArmHerCharges(map);                 // …and he stays aboard: no surface, no boat, no gangway
        RunUntilSheGoes(map);

        Assert.NotNull(Read(map, "_busted"));
        Assert.Null(Read(map, "_shipEpitaph"));
        Assert.Null(Read(map, "_claimOwed"));
        Assert.Equal(0, (int)Read(map, "_claimsLodged")!);
        Assert.Null(Read(map, "_claimDesk"));
    }
}

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
/// #251 · <b>THE BENCH THE WHOLE SCENE IS DRIVEN ON</b> — the world, the counter, the frame and the
/// plumbing, in one place, so the three test parts next door read as assertions and nothing else.
///
/// <para>Nothing in this file asserts anything about the claim. It boots a live <see cref="Pages.Map"/>
/// over the shipping scenario, puts the captain where a posture says, walks him to the kiosk the port
/// actually built, presses the counter's own rows, and runs frames through the page's own <c>OnTick</c>.
/// Every step goes through a shipping door: that is the file's one rule, and it is why the sibling issue
/// could get three different answers by reading the source instead.</para>
/// </summary>
public sealed partial class TheClaimIsWalkedEndToEndTests
{
    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where the captain is standing, which is the whole of what the presence law asks.</summary>
    public enum Posture
    {
        /// <summary>On her own deck, in the dark, with nothing between him and the controls.</summary>
        Aboard,

        /// <summary>Walking a moon.</summary>
        OnASurface,

        /// <summary>Away in the boat.</summary>
        AwayInTheBoat,

        /// <summary>Past the tube, on somebody's concourse, with her clamped to their collar.</summary>
        AshorePastTheTube,
    }

    /// <summary>A live component over the shipping scenario, walking her own deck — the same boot the berth
    /// scuttle's own guards use, so the two files are asking one game (<see cref="CastawayBench"/>).</summary>
    private static Pages.Map Boot() => CastawayBench.Boot("claims-canvas");

    /// <summary>Put the captain where the posture says, through the same doors the game uses to get him
    /// there — a real excursion, a real shuttle launch, a real walk past the tube.</summary>
    private static void PutHimIn(Pages.Map map, Posture posture)
    {
        switch (posture)
        {
            case Posture.Aboard:
                Assert.True((bool)Invoke(map, "TheMasterIsAboardHer")!);
                return;

            case Posture.OnASurface:
                PutHimOnAGround(map);
                break;

            case Posture.AwayInTheBoat:
                PutHimInTheShuttle(map);
                break;

            default:
                ClampAtThePort(map);
                WalkHimAshore(map);
                break;
        }

        Assert.False((bool)Invoke(map, "TheMasterIsAboardHer")!,
                     $"{posture} did not actually take the captain off his ship.");
    }

    /// <summary>Tie her up at <see cref="Port"/> through the clamp the game uses, and assert the port has an
    /// interior to walk — a port with no concourse has no machine on it and no gangway to be past.</summary>
    private static void ClampAtThePort(Pages.Map map)
    {
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == Port);
        Assert.True(HavenInterior.HasInterior(Port), $"{Port} has no interior, so it has no concourse.");

        double simTime = (double)Read(map, "SimTime")!;
        Invoke(map, "ClampOntoHaven", dock, sky.Position(Port, simTime), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
    }

    /// <summary>The ground the excursion arms of this file put the captain down on. Luna, because it is a
    /// real moon of a real planet in the shipping scenario and therefore has a harbour that serves it —
    /// which is what the waiting writ is filed against.</summary>
    private const string Ground = "luna";

    /// <summary>On a ground, built the way the frame guards build one: the page's own excursion record and
    /// its own <c>RebuildSurfaceDeck</c>, so the deck under his feet is a deck the game made.</summary>
    private static void PutHimOnAGround(Pages.Map map)
    {
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Ground, Ground, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Invoke(map, "RebuildSurfaceDeck");
        Assert.NotNull(Read(map, "_surface"));
    }

    /// <summary>Which ground the excursion put him on.</summary>
    private static string TheGroundHeIsOn(Pages.Map map)
    {
        object surface = Read(map, "_surface") ?? throw new InvalidOperationException("he is not on a ground.");
        return (string)Get(Get(Get(surface, "Stop")!, "Body")!, "Id")!;
    }

    /// <summary>A collector sitting exactly where she is, already fitted out. On any frame she is allowed to
    /// close she has closed — which is what makes the held arms of the presence law mean something.</summary>
    private static void PutACollectorOnTopOfHer(Pages.Map map, string callsign)
    {
        var ship = (ShipState)Read(map, "_ship")!;
        ((IList)Read(map, "_hunters")!).Add(new HunterState(
            Id: callsign.ToLowerInvariant(),
            Callsign: callsign,
            OriginBodyId: Port,
            SpawnedAtSimTime: (double)Read(map, "SimTime")!,
            ActivationSimTime: 0,
            State: ship,
            CaughtPlayer: false,
            BrokenOff: false));
    }

    /// <summary>The captain's word, the crew's second key, both keys together — the panel's own three verbs,
    /// in the order the panel makes the player press them. Nothing writes the clock.</summary>
    private static void ArmHerCharges(Pages.Map map)
    {
        Invoke(map, "OpenShipScuttlePanel");
        Invoke(map, "GiveTheWordAgainstHer");
        Invoke(map, "AskTheCrewForTheSecondKey");
        Invoke(map, "TurnBothKeys");
        Assert.Equal(Scuttle.OverloadSeconds, (double)Read(map, "_shipChargesSeconds")!);
        Invoke(map, "CloseShipScuttlePanel");
    }

    // ── THE COUNTER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Stand at the machine: the console the port built, found on the deck plan and walked to.</summary>
    private static void WalkToTheKiosk(Pages.Map map)
    {
        var plan = (DeckPlan)Read(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot kiosk = plan.Consoles.First(c => c.Label == NebulaClaims.KioskPlate);

        Set(map, "_avatarX", (double)kiosk.X);
        Set(map, "_avatarY", (double)kiosk.Y);
        Set(map, "_viewObject", null);
    }

    private static IReadOnlyList<NebulaClaims.Ask> TheRows(Pages.Map map) =>
        (IReadOnlyList<NebulaClaims.Ask>)Invoke(map, "TheClaimAsks")!;

    private static void Press(Pages.Map map, NebulaClaims.Ask ask, PirateInsurance withPolicy = default) =>
        Invoke(map, "PressTheClaim", ask);

    private static int PressesTaken(Pages.Map map) =>
        Read(map, "_claimDesk") is { } desk ? (int)Get(desk, "Presses")! : 0;

    /// <summary>Three presses, all correct, through the counter's own rows — the whole lodging, for the
    /// guards that are about what happens AFTER one.</summary>
    private static void LodgeAWholeClaim(Pages.Map map)
    {
        Invoke(map, "ViewNearbyObject");
        Assert.True((bool)Read(map, "TheClaimDeskIsUp")!);

        for (int press = 0; press < NebulaClaims.Presses; press++)
        {
            IReadOnlyList<NebulaClaims.Ask> rows = TheRows(map);
            Assert.NotEmpty(rows);
            NebulaClaims.Ask take = press == 1
                ? rows.Single(r => r.Offer == (string)Invoke(map, "ShipNameNow")!)
                : rows[0];
            Press(map, take);
            Assert.Equal(press + 1, PressesTaken(map));
        }
    }

    /// <summary>The pending-writ row on the captain's ledger, or null when there is none.</summary>
    private static object? TheWritRow(Pages.Map map) => Invoke(map, "PendingWritTip");

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    private static void RunUntilSheGoes(Pages.Map map)
    {
        for (int i = 0; i < 4000; i++)
        {
            Frame(map);
            if (Read(map, "_shipChargesSeconds") is null)
            {
                return;
            }
        }

        throw new InvalidOperationException("her ninety-second overload never reached zero in four hundred seconds.");
    }
}

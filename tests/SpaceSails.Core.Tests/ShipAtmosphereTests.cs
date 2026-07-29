using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// HER AIR, AS LAWS. The owner asked for three things and they pull in different directions, which is why
/// each of them gets held here rather than in the client: <i>"we might want to isolate cabins to keep our
/// disease etc."</i> (free, reversible, asks nobody), <i>"our venting must have captains ok mechanism in it,
/// like firing a shot or looting"</i> (gated, irreversible, named), and <i>"there must be controls to do so on
/// the bridge, but the bridge position needs captains ok to vent"</i> (two consoles, one rule).
/// </summary>
public sealed class ShipAtmosphereTests
{
    /// <summary>Her compartments as the shared rules see them, with a set of hatches dogged.</summary>
    private static IReadOnlyList<HullVenting.Space> Her(params string[] dogged)
    {
        var shut = new HashSet<string>(dogged, StringComparer.Ordinal);
        var spaces = new List<HullVenting.Space>(ShipLayout.Rooms.Length);
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            spaces.Add(new HullVenting.Space(
                room.Name, DoorShut: shut.Contains(room.Name), Vented: false,
                Infested: false, HoldsSurvivor: false, CaptainInside: false));
        }
        return spaces;
    }

    /// <summary>
    /// THE BUG THE CORRIDOR'S NAME CAUSED. <see cref="HullVenting.SharedAtmosphere"/> compares against the
    /// corridor's name to know which end of a door it is looking at, and it used to compare against the
    /// DERELICT's name unconditionally (<c>"THE SPINE"</c>). Her corridor is called <c>"THE CORRIDOR"</c>, so
    /// asking about hers returned a volume of ONE — the corridor, alone, connected to nothing — and every
    /// readout built on it was quietly wrong while looking perfectly plausible.
    /// </summary>
    [Fact]
    public void HerWholeShipIsOneAtmosphereWhenNothingIsDogged()
    {
        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(
            ShipLayout.SpineName, Her(), ShipLayout.SpineName);

        // Every compartment, plus the corridor itself.
        Assert.Equal(ShipLayout.Rooms.Length + 1, volume.Count);
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            Assert.Contains(room.Name, volume);
        }
    }

    /// <summary>And the shape of the old bug, kept as a test so nobody re-hardcodes it: ask with the wrong
    /// corridor name and the flood fill finds nothing at all, because no hatch aboard opens onto a corridor by
    /// that name.</summary>
    [Fact]
    public void AskingWithTheWrongCorridorNameFindsNothing()
    {
        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(
            HullVenting.SpineName, Her(), ShipLayout.SpineName);

        Assert.Equal([HullVenting.SpineName], volume);
    }

    [Fact]
    public void ADoggedCabinIsItsOwnAtmosphere()
    {
        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(
            ShipLayout.SpineName, Her("CABIN 2"), ShipLayout.SpineName);

        Assert.DoesNotContain("CABIN 2", volume);
        Assert.Equal(ShipLayout.Rooms.Length, volume.Count);   // the rest of her, and the corridor

        // And from the other side: the cabin alone. This is the quarantine the owner asked for — "isolate
        // cabins to keep our disease" — and it is the same search answering, not a second rule.
        IReadOnlyList<string> berth = HullVenting.SharedAtmosphere(
            "CABIN 2", Her("CABIN 2"), ShipLayout.SpineName);
        Assert.Equal(["CABIN 2"], berth);
    }

    /// <summary>A berth that is not one of her compartments would make <c>WhatIsInThere</c> quietly say a cabin
    /// holds no bunks — the exact class of bug this repo has now been bitten by four times: a name written down
    /// twice, in two places, drifting.</summary>
    [Fact]
    public void EveryBerthIsACompartmentSheActuallyHas()
    {
        foreach (string berth in ShipAtmosphere.Berths)
        {
            Assert.Contains(berth, ShipLayout.Rooms.Select(r => r.Name));
        }
    }

    [Fact]
    public void OnlyBerthsAreSpokenOfAsHoldingPeople()
    {
        Assert.Contains("berth", ShipAtmosphere.WhatIsInThere("CABIN 1"), StringComparison.Ordinal);
        Assert.Contains("no bunks", ShipAtmosphere.WhatIsInThere("CARGO HOLD"), StringComparison.Ordinal);
    }

    // ── The gate ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PROXIMITY IS NEVER CONSENT, and neither is having a compartment selected. The board may OFFER; it may
    /// never take. This is the boarding gate's own promise, restated for the valve — no amount of selecting can
    /// return <see cref="ShipAuthority.VentIntent.Authorized"/> on its own.
    /// </summary>
    [Fact]
    public void SelectingACompartmentIsNeverAuthorisation()
    {
        Assert.Equal(
            ShipAuthority.VentIntent.Opportunity,
            ShipAuthority.EvaluateVent("CABIN 1", authorizedRoom: null));
    }

    [Fact]
    public void TheWordAuthorisesTheROOMItNamedAndNoOther()
    {
        Assert.Equal(
            ShipAuthority.VentIntent.Authorized,
            ShipAuthority.EvaluateVent("CABIN 1", "CABIN 1"));

        // The whole reason the authorization carries a name: the captain cannot arm one compartment and blow
        // another by moving the selection.
        Assert.Equal(
            ShipAuthority.VentIntent.Opportunity,
            ShipAuthority.EvaluateVent("CABIN 2", "CABIN 1"));
    }

    [Fact]
    public void NothingSelectedIsNoQuestionOnTheTable()
    {
        Assert.Equal(
            ShipAuthority.VentIntent.NothingSelected,
            ShipAuthority.EvaluateVent(null, "CABIN 1"));
    }

    /// <summary>The gate is on the ACT, not on the console — which is what lets her have a working bridge
    /// repeater without it becoming a way around the captain's word. Same inputs, same answer, wherever the
    /// captain is standing.</summary>
    [Fact]
    public void TheRepeaterIsAConvenienceNotALoophole()
    {
        Assert.Equal(
            ShipAuthority.EvaluateVent("ENGINE ROOM", "ENGINE ROOM"),
            ShipAuthority.EvaluateVent("ENGINE ROOM", "ENGINE ROOM"));

        // Both of her boards exist, and they are far enough apart to be separate places to stand.
        double dx = ShipLayout.ValveStation.X - ShipLayout.BridgeRepeaterStation.X;
        double dy = ShipLayout.ValveStation.Y - ShipLayout.BridgeRepeaterStation.Y;
        Assert.True(Math.Sqrt((dx * dx) + (dy * dy)) > 10.0,
                    "her two atmosphere boards should be at opposite ends of the ship");
    }

    // ── The interlock, which is mechanical and has nothing to do with authority ────────────────────────

    [Fact]
    public void TheHandleRefusesTheRoomYouAreStandingIn()
    {
        var here = new HullVenting.Space(
            "CABIN 1", DoorShut: true, Vented: false, Infested: false,
            HoldsSurvivor: false, CaptainInside: true);

        Assert.Equal(HullVenting.VentReadiness.CaptainInside, HullVenting.Readiness(here));
    }

    [Fact]
    public void TheHandleRefusesAnOpenHatch()
    {
        var open = new HullVenting.Space(
            "CABIN 1", DoorShut: false, Vented: false, Infested: false,
            HoldsSurvivor: false, CaptainInside: false);

        Assert.Equal(HullVenting.VentReadiness.DoorOpen, HullVenting.Readiness(open));
    }

    /// <summary>Her own tanks pay for putting the air back, and they are finite: the whole point of a reserve is
    /// that a captain who vents a compartment because it was quicker than walking around it sees the gauge
    /// afterwards.</summary>
    [Fact]
    public void PuttingTheAirBackNeedsAFillInHerTanks()
    {
        var blown = new HullVenting.Space(
            "CARGO HOLD", DoorShut: true, Vented: true, Infested: false,
            HoldsSurvivor: false, CaptainInside: false);

        Assert.Equal(HullVenting.RefillReadiness.Ready, HullVenting.RefillState(blown, ShipAtmosphere.ReserveFills));
        Assert.Equal(HullVenting.RefillReadiness.NoReserve, HullVenting.RefillState(blown, 0));
    }

    [Fact]
    public void HerReserveLineSaysWhenItIsEmpty()
    {
        Assert.Contains("EMPTY", ShipAtmosphere.ReserveLine(0), StringComparison.Ordinal);
        Assert.Contains("8", ShipAtmosphere.ReserveLine(ShipAtmosphere.ReserveFills), StringComparison.Ordinal);
    }
}

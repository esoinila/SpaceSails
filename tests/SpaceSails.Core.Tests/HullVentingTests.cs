namespace SpaceSails.Core.Tests;

/// <summary>#488 · Blowing a compartment. Owner: <i>"we could vent the infested boarded vessel into space
/// … there might be a small risk that we kill possible survivors with that also … we might use dice throw
/// … that would make the decision hard :-D"</i> These pin what makes it hard.</summary>
public class HullVentingTests
{
    private static HullVenting.Space Space(
        bool doorShut = true, bool vented = false, bool infested = true, bool survivor = false) =>
        new("DEEP HOLD", doorShut, vented, infested, survivor);

    // ── The door is the safety ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AnOpenCompartmentCannotBeBlown()
    {
        // Venting a compartment still open to the spine empties the corridor the captain is standing in.
        HullVenting.VentOutcome o = HullVenting.Vent(Space(doorShut: false));

        Assert.False(o.Blown);
        Assert.Equal(HullVenting.VentReadiness.DoorOpen, HullVenting.Readiness(Space(doorShut: false)));
        Assert.Contains("shut the door", o.Line, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ASealedCompartmentIsReady_AndBlowingItOpensTheRoomButDoesNotFinishIt()
    {
        // CHANGED by the soak (owner, mid-playtest: "there might be a counter on how long the room has been
        // in vacuum … so it needs certain time for certain infestations"). The handle opens the room; the
        // VACUUM does the killing, on its own clock. So the panel is no longer allowed to promise a result
        // in the same breath as the pull — that promise was the whole reason venting felt like a button.
        HullVenting.VentOutcome o = HullVenting.Vent(Space(infested: true));

        Assert.True(o.Blown);
        Assert.False(o.InfestationCleared);
        Assert.False(o.SurvivorKilled);
        Assert.Contains("vacuum now", o.Line);
        Assert.Contains("not the same as dead", o.Line);
    }

    [Fact]
    public void BlowingTheSameCompartmentTwiceDoesNothing()
    {
        HullVenting.VentOutcome o = HullVenting.Vent(Space(vented: true));

        Assert.False(o.Blown);
        Assert.Equal(HullVenting.VentReadiness.AlreadyVented, HullVenting.Readiness(Space(vented: true)));
    }

    // ── The hard part ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BlowingACompartmentWithSomeoneInItKillsThem_AndSaysSoWithoutCertainty()
    {
        // The captain never gets told cleanly that they murdered someone; they get told what it felt like.
        HullVenting.VentOutcome o = HullVenting.Vent(Space(infested: true, survivor: true));

        Assert.True(o.Blown);
        Assert.True(o.SurvivorKilled);
        Assert.Contains("beating on the door from the inside", o.Line);
        Assert.Contains("will not know for certain", o.Line);
    }

    [Fact]
    public void TheSensorCannotTellASurvivorFromTheInfestation()
    {
        // THE WHOLE DESIGN. If the read could distinguish them, there would be no decision — you would
        // simply vent the empty rooms. Both are warm, both move, and the panel reports only "alive".
        var withSurvivor = new HullVenting.Space("DEEP HOLD", true, false, Infested: true, HoldsSurvivor: true);
        var without = new HullVenting.Space("DEEP HOLD", true, false, Infested: true, HoldsSurvivor: false);

        // Sweep seeds until a confident read lands, then compare what the two report.
        for (ulong seed = 0; seed < 200; seed++)
        {
            (DiceRoll roll, HullVenting.LifeSign a) = HullVenting.Read(seed, withSurvivor);
            if (roll.Face < HullVenting.ConfidentRead)
            {
                continue;
            }
            (_, HullVenting.LifeSign b) = HullVenting.Read(seed, without);

            Assert.Equal(HullVenting.LifeSign.SomethingAlive, a);
            Assert.Equal(a, b); // indistinguishable — that is the point
            return;
        }
        Assert.Fail("no confident read in 200 seeds — the die is not behaving");
    }

    [Fact]
    public void AConfidentReadOnAnEmptySealedRoomSaysCold()
    {
        var empty = new HullVenting.Space("BRIDGE", true, false, Infested: false, HoldsSurvivor: false);

        for (ulong seed = 0; seed < 200; seed++)
        {
            (DiceRoll roll, HullVenting.LifeSign sign) = HullVenting.Read(seed, empty);
            if (roll.Face >= HullVenting.ConfidentRead)
            {
                Assert.Equal(HullVenting.LifeSign.Empty, sign);
                return;
            }
        }
        Assert.Fail("no confident read in 200 seeds");
    }

    [Fact]
    public void APoorRollTellsYouNothingAtAll()
    {
        var space = Space(survivor: true);
        for (ulong seed = 0; seed < 200; seed++)
        {
            (DiceRoll roll, HullVenting.LifeSign sign) = HullVenting.Read(seed, space);
            if (roll.Face < HullVenting.ConfidentRead)
            {
                Assert.Equal(HullVenting.LifeSign.Unreadable, sign);
                return;
            }
        }
        Assert.Fail("no poor read in 200 seeds");
    }

    [Fact]
    public void TheReadIsSeeded_SoAReloadCannotReRollIt()
    {
        var space = Space(survivor: true);
        Assert.Equal(HullVenting.Read(4242, space), HullVenting.Read(4242, space));
    }

    [Fact]
    public void EveryReadingHasWordsThatDoNotOverclaim()
    {
        foreach (HullVenting.LifeSign s in System.Enum.GetValues<HullVenting.LifeSign>())
        {
            Assert.False(string.IsNullOrWhiteSpace(HullVenting.ReadLine(s)));
        }
        // The positive read must never assert WHAT is alive.
        Assert.Contains("cannot tell you what", HullVenting.ReadLine(HullVenting.LifeSign.SomethingAlive));
    }

    // ── Who is aboard ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnlyAnInfestedHullHasSurvivorsToFind()
    {
        // A drive failure forty years ago has nobody left alive behind a door.
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            if (c == Derelict.WreckCause.Infested)
            {
                continue;
            }
            foreach ((string name, float _, float _, bool _) in WreckLayout.Compartments)
            {
                Assert.False(HullVenting.HidesSurvivor("wreck-x", name, c));
            }
        }
    }

    [Fact]
    public void AnInfestedHullHidesSomeoneSomewhere_ButNotEverywhere()
    {
        // Rare enough that venting is usually clean; common enough that it is never a free action.
        int hiding = 0;
        for (int i = 0; i < 60; i++)
        {
            foreach ((string name, float _, float _, bool _) in WreckLayout.Compartments)
            {
                if (HullVenting.HidesSurvivor($"wreck-{i}", name, Derelict.WreckCause.Infested))
                {
                    hiding++;
                }
            }
        }

        Assert.True(hiding > 0, "no wreck ever hides anyone — the decision is not a decision");
        Assert.True(hiding < 60 * WreckLayout.Compartments.Length, "every room holds someone — venting is never clean");
    }

    [Fact]
    public void TheSameWreckAlwaysHidesThemInTheSamePlaces()
    {
        // A reload must not re-roll who is alive.
        foreach ((string name, float _, float _, bool _) in WreckLayout.Compartments)
        {
            Assert.Equal(
                HullVenting.HidesSurvivor("kestrel-3", name, Derelict.WreckCause.Infested),
                HullVenting.HidesSurvivor("kestrel-3", name, Derelict.WreckCause.Infested));
        }
    }

    // ── The valves are not on the bridge ──────────────────────────────────────────────────────────────

    [Fact]
    public void TheValvesAreAft_InATechnicalSpace()
    {
        // Owner, borrowing from BSG: the bridge panel is dead, so you walk TOWARD the thing to get the tool
        // that kills it. A bridge switch would let the captain clear the ship from the doorway they came in.
        Assert.Contains(WreckLayout.Compartments, c => c.Name == HullVenting.ValveCompartment);

        (string _, float x0, float x1, bool _) = System.Array.Find(
            WreckLayout.Compartments, c => c.Name == HullVenting.ValveCompartment);
        Assert.True((x0 + x1) / 2 < WreckLayout.SpawnX - 20, "the valves must be a real walk from the airlock");
    }

    [Fact]
    public void TheDeadBridgePanelPointsAft_RatherThanJustRefusing()
    {
        // Nobody should have to guess that the answer is aft.
        Assert.Contains(HullVenting.ValveCompartment, HullVenting.DeadBridgePanelLine);
    }

    [Fact]
    public void SavingSomeoneIsWorthMoreThanFilingAnOpinion()
    {
        // The reward for the careful road: a living witness beats a finder's fee.
        Derelict.Wreck w = Derelict.SeededWithCause(Derelict.WreckCause.Infested)!.Value;
        int fee = Derelict.Resolve(w, Derelict.SalvageChoice.FileTheReport, w.Cause).CreditsNow;

        Assert.True(HullVenting.SurvivorRescueCr > fee);
    }

    // ── The soak: vacuum kills, but not instantly ─────────────────────────────────────────────────────

    [Fact]
    public void VacuumTakesTime_AndTheHardyOnesTakeLonger()
    {
        Assert.Equal(0.0, HullVenting.SoakRequired(HullVenting.Infestation.None));
        Assert.True(HullVenting.SoakRequired(HullVenting.Infestation.Motile) > 0);
        Assert.True(HullVenting.SoakRequired(HullVenting.Infestation.Fibrous)
                    > HullVenting.SoakRequired(HullVenting.Infestation.Motile));
        Assert.True(HullVenting.SoakRequired(HullVenting.Infestation.Encysted)
                    > HullVenting.SoakRequired(HullVenting.Infestation.Fibrous));
    }

    [Fact]
    public void TheSoakIsLongEnoughThatYouHaveToGoAndDoSomethingElse()
    {
        // The point of the counter is that blowing a hold and standing there watching a clock is not a
        // game. The long soak has to outlast the patience of a captain with a log and a manifest still to
        // read — otherwise the mechanic collapses back into a button with a delay.
        Assert.True(HullVenting.SoakRequired(HullVenting.Infestation.Encysted) >= 120.0);
    }

    [Fact]
    public void AnUnfinishedCompartmentIsNotClear_AndAFinishedOneIs()
    {
        var s = new HullVenting.Space("DEEP HOLD", DoorShut: true, Vented: true, Infested: true,
            HoldsSurvivor: false, CaptainInside: false,
            VacuumSeconds: 5.0, Kind: HullVenting.Infestation.Fibrous);

        Assert.False(HullVenting.SoakComplete(s));
        Assert.True(HullVenting.SoakComplete(s with { VacuumSeconds = HullVenting.FibrousSoakSeconds }));
    }

    [Fact]
    public void ARoomWithAirInItIsNeverSoaking()
    {
        var s = new HullVenting.Space("BRIDGE", true, Vented: false, Infested: true, false, false,
            VacuumSeconds: 9999, Kind: HullVenting.Infestation.Motile);

        Assert.False(HullVenting.SoakComplete(s));
    }

    [Fact]
    public void TheCounterSaysHowLongItHasBeenOpen_NeverHowLongItNeeds()
    {
        // The second number does not exist for the captain. That is the decision.
        Assert.Equal("00:00", HullVenting.SoakLabel(0));
        Assert.Equal("00:45", HullVenting.SoakLabel(45.9));
        Assert.Equal("02:05", HullVenting.SoakLabel(125));
        Assert.Equal("00:00", HullVenting.SoakLabel(-3));
    }

    [Fact]
    public void WhatIsGrowingInThereIsSeeded_SoAReloadCannotRerollItIntoSomethingEasier()
    {
        for (int i = 0; i < 20; i++)
        {
            HullVenting.Infestation first = HullVenting.InfestationIn($"hull-{i}", "DEEP HOLD", true);
            Assert.Equal(first, HullVenting.InfestationIn($"hull-{i}", "DEEP HOLD", true));
        }

        Assert.Equal(HullVenting.Infestation.None, HullVenting.InfestationIn("hull-1", "BRIDGE", false));
    }

    [Fact]
    public void AllThreeKindsOccur_ButTheHardyOneIsTheMinority()
    {
        var counts = new Dictionary<HullVenting.Infestation, int>();
        for (int i = 0; i < 300; i++)
        {
            HullVenting.Infestation k = HullVenting.InfestationIn($"hull-{i}", "DEEP HOLD", true);
            counts[k] = counts.GetValueOrDefault(k) + 1;
        }

        Assert.True(counts.GetValueOrDefault(HullVenting.Infestation.Motile) > 0);
        Assert.True(counts.GetValueOrDefault(HullVenting.Infestation.Fibrous) > 0);
        Assert.True(counts.GetValueOrDefault(HullVenting.Infestation.Encysted) > 0);
        Assert.True(counts[HullVenting.Infestation.Encysted] < counts[HullVenting.Infestation.Motile]);
    }

    // ── Refill: the other half of the board ───────────────────────────────────────────────────────────

    [Fact]
    public void AVentedRoomCanBeBroughtBack_ButOnlyWithTheDoorShutAndAChargeToSpend()
    {
        var vented = new HullVenting.Space("BRIDGE", DoorShut: true, Vented: true, Infested: false,
            HoldsSurvivor: false);

        Assert.Equal(HullVenting.RefillReadiness.Ready, HullVenting.RefillState(vented, 1));
        Assert.Equal(HullVenting.RefillReadiness.NoReserve, HullVenting.RefillState(vented, 0));
        Assert.Equal(HullVenting.RefillReadiness.DoorOpen,
                     HullVenting.RefillState(vented with { DoorShut = false }, 1));
        Assert.Equal(HullVenting.RefillReadiness.NotVented,
                     HullVenting.RefillState(vented with { Vented = false }, 1));
    }

    [Fact]
    public void AirComesBack_NobodyDoes()
    {
        // THE RULE THE WHOLE FEATURE IS BALANCED ON. A refill that undid a vent would gut the decision the
        // panel exists for: you would blow every compartment and restore the ones that squealed.
        var s = new HullVenting.Space("CREW SPACES", DoorShut: true, Vented: true, Infested: false,
            HoldsSurvivor: false, CaptainInside: false, VacuumSeconds: 500);

        HullVenting.RefillOutcome o = HullVenting.Refill(s, 1);

        Assert.True(o.Filled);
        Assert.False(o.SomethingSurvived);
        Assert.Contains("Nothing that went out with the air comes back in with it", o.Line);
    }

    [Fact]
    public void RefillingBeforeTheVacuumIsFinishedSavesTheThingYouWereKilling()
    {
        // The mistake the counter exists to make available. Impatience is the cost, and the captain is told
        // plainly — this is never a silent failure.
        var s = new HullVenting.Space("DEEP HOLD", DoorShut: true, Vented: true, Infested: true,
            HoldsSurvivor: false, CaptainInside: false,
            VacuumSeconds: 10, Kind: HullVenting.Infestation.Encysted);

        HullVenting.RefillOutcome early = HullVenting.Refill(s, 1);
        Assert.True(early.Filled);
        Assert.True(early.SomethingSurvived);
        Assert.Contains("takes the first breath", early.Line);

        HullVenting.RefillOutcome patient = HullVenting.Refill(
            s with { VacuumSeconds = HullVenting.EncystedSoakSeconds }, 1);
        Assert.False(patient.SomethingSurvived);
    }

    [Fact]
    public void RefillingIsScarce_SoItIsASpendAndNotAToggle()
    {
        Assert.True(HullVenting.RefillChargesPerBoarding > 0);
        Assert.True(HullVenting.RefillChargesPerBoarding < WreckLayout.Compartments.Length,
            "if you can refill every compartment, blow-everything-and-restore-the-squealers is free");
    }

    [Fact]
    public void BeingImpatientCostsLessThanKillingSomebody()
    {
        // You have not killed anyone — you have been impatient in front of something patient.
        Assert.True(HullVenting.RefilledTooSoonNerveCost > 0);
        Assert.True(HullVenting.RefilledTooSoonNerveCost < HullVenting.VentedSurvivorNerveCost);
    }

    // ── Pressure locks ────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ADoorIsHeldOnlyWhenThereIsAirOnExactlyOneSideOfIt()
    {
        var withAir = new HullVenting.Space("BRIDGE", true, Vented: false, Infested: false, HoldsSurvivor: false);
        var vacuum = withAir with { Vented = true };

        // The case the owner hit: he blew the room and walked straight into it.
        Assert.True(HullVenting.DoorHeldByPressure(vacuum, spinePressurised: true));

        // Both sides the same — nothing to hold it either way.
        Assert.False(HullVenting.DoorHeldByPressure(vacuum, spinePressurised: false));
        Assert.False(HullVenting.DoorHeldByPressure(withAir, spinePressurised: true));

        // And the differential the other way round: the last breathable room on a vacuum hull.
        Assert.True(HullVenting.DoorHeldByPressure(withAir, spinePressurised: false));
    }

    [Fact]
    public void AHullThatHasBeenOpenForDecadesHasNoLockedDoorsAtAll()
    {
        // Every compartment on the vented hull is vacuum and so is her spine, so nothing fights you —
        // except the one room somebody kept air in, which is the room the valve board is in.
        foreach ((string name, float _, float _, bool _) in WreckLayout.Compartments)
        {
            bool preVented = HullVenting.StartsVented(Derelict.WreckCause.VentedByOneOfTheirOwn, name);
            var s = new HullVenting.Space(name, DoorShut: preVented, Vented: preVented,
                Infested: false, HoldsSurvivor: false);

            bool held = HullVenting.DoorHeldByPressure(s, spinePressurised: false);
            Assert.Equal(name == HullVenting.ValveCompartment, held);
        }
    }

    [Fact]
    public void TheGaugeReadsHardOverExactlyWhenTheDoorIsHeld()
    {
        var vacuum = new HullVenting.Space("DEEP HOLD", true, true, false, false);

        Assert.Equal(1.0, HullVenting.PressureGauge(vacuum, true));
        Assert.Equal(0.0, HullVenting.PressureGauge(vacuum, false));
    }

    [Fact]
    public void BothSidesOfTheLockGetTheirOwnExplanation()
    {
        Assert.Contains("ten tonnes", HullVenting.PressureLockLine("DEEP HOLD", true));
        Assert.Contains("last breathable room", HullVenting.PressureLockLine("ENGINEERING", false));
        Assert.NotEqual(HullVenting.PressureLockLine("X", true), HullVenting.PressureLockLine("X", false));
    }

    [Fact]
    public void EqualisingIsAlwaysAvailable_SoAPressureLockCanNeverStrandACaptain()
    {
        // The reason this mechanic is allowed to wall a doorway at all: every real pressure door has an
        // equalisation valve, it costs nothing, and it costs the ship her air. There is no state in which
        // a captain is on the wrong side of a door with no way through it.
        Assert.False(string.IsNullOrWhiteSpace(HullVenting.EqualiseLine));
        Assert.Contains("both sides read nothing", HullVenting.EqualiseLine);
        Assert.Contains("stops", HullVenting.EqualiseWarnsLine);
    }

    [Fact]
    public void YouCanNeverBeSealedIntoTheRoomYouAreStandingIn()
    {
        // A pressure lock only ever forms on a VENTED compartment, and the board will not vent the room the
        // captain is in. Those two rules together are what make the wall safe.
        var here = new HullVenting.Space("DEEP HOLD", DoorShut: true, Vented: false, Infested: false,
            HoldsSurvivor: false, CaptainInside: true);

        Assert.Equal(HullVenting.VentReadiness.CaptainInside, HullVenting.Readiness(here));
        Assert.False(HullVenting.DoorHeldByPressure(here, spinePressurised: true));
    }

    [Fact]
    public void EveryRefusalSaysWhy()
    {
        foreach (HullVenting.RefillReadiness r in System.Enum.GetValues<HullVenting.RefillReadiness>())
        {
            if (r == HullVenting.RefillReadiness.Ready)
            {
                continue;
            }
            Assert.False(string.IsNullOrWhiteSpace(HullVenting.RefillRefusalLine(r, "DEEP HOLD")));
        }
    }

    [Fact]
    public void ThePumpRunsUnderYourFeetEvenThoughTheValveWillNot()
    {
        // The board is IN engineering, so applying the vent interlock to the pump made one compartment
        // permanently un-emptiable. Owner: "I can not pump down to vacuum in engineering?"
        var here = new HullVenting.Space("ENGINEERING", DoorShut: true, Vented: false, Infested: true,
                                         HoldsSurvivor: false, CaptainInside: true);

        Assert.Equal(HullVenting.VentReadiness.CaptainInside, HullVenting.Readiness(here));
        Assert.Equal(HullVenting.VentReadiness.Ready, HullVenting.PumpReadiness(here));
        Assert.False(string.IsNullOrWhiteSpace(
            HullVenting.PumpUnderfootLine("ENGINEERING", HullVenting.PumpDownSeconds)));
    }

    [Fact]
    public void ThePumpStillNeedsAShutHatchAndSomethingLeftToPump()
    {
        // Everything else about the interlock is unchanged — only the captain's own feet were exempted.
        var open = new HullVenting.Space("DEEP HOLD", DoorShut: false, Vented: false, Infested: true,
                                         HoldsSurvivor: false, CaptainInside: true);
        var dead = new HullVenting.Space("DEEP HOLD", DoorShut: true, Vented: true, Infested: true,
                                         HoldsSurvivor: false, CaptainInside: true);

        Assert.Equal(HullVenting.VentReadiness.DoorOpen, HullVenting.PumpReadiness(open));
        Assert.Equal(HullVenting.VentReadiness.AlreadyVented, HullVenting.PumpReadiness(dead));
    }

    [Fact]
    public void TheWholeShipOrderNamesTheRoomTheCaptainIsStandingIn()
    {
        // An order that quietly excluded their own compartment would be a board deciding what they meant —
        // and would leave the corridor permanently un-pumpable with no way to find out why.
        string underfoot = HullVenting.WholeShipOrderLine(8, "ENGINEERING");
        Assert.Contains("ENGINEERING", underfoot, System.StringComparison.Ordinal);
        Assert.Contains("STANDING IN", underfoot, System.StringComparison.Ordinal);

        // Out in the spine there is no room to warn about, so it does not invent one.
        string fromSpine = HullVenting.WholeShipOrderLine(8, null);
        Assert.DoesNotContain("STANDING IN", fromSpine, System.StringComparison.Ordinal);
        Assert.Contains("corridor", fromSpine, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TheSpinesRoughMarkIsPastARoomsWholeRunAndThatIsWhyItMustBePerPump()
    {
        // The exact geometry that hid the leak: a room's pump counts down from 50 and banks at 32, while
        // the corridor banks at 72. Measure a room against the corridor's mark and the room can never
        // cross it — its clock starts BELOW it — so the charge is silently never paid.
        double roomTotal = HullVenting.PumpTotalSeconds(spine: false);
        double roomMark = HullVenting.PumpRoughMark(spine: false);
        double spineMark = HullVenting.PumpRoughMark(spine: true);

        Assert.True(roomMark < roomTotal, "a room must be able to reach its own mark");
        Assert.True(spineMark > roomTotal,
                    "this is the trap: the corridor's mark is past a room's entire run, so a room measured " +
                    "against it never banks and nothing ever looks wrong");
    }

    [Fact]
    public void EveryPumpPaysAndTheCorridorPaysMore()
    {
        Assert.Equal(HullVenting.PumpDownYieldsCharges, HullVenting.PumpYield(spine: false));
        Assert.Equal(HullVenting.SpinePumpYieldsCharges, HullVenting.PumpYield(spine: true));
        Assert.True(HullVenting.PumpYield(spine: true) > HullVenting.PumpYield(spine: false));
    }

    [Fact]
    public void PumpingHerDownPaysForFloodingHerBack()
    {
        // The closed loop the whole economy rests on: air you took the slow road on is air you can put
        // back. Pump every compartment and the corridor, and the flood is exactly affordable.
        int rooms = WreckLayout.Compartments.Length;
        int banked = (rooms * HullVenting.PumpYield(spine: false)) + HullVenting.PumpYield(spine: true);

        Assert.True(banked >= HullVenting.WholeShipRefillCost(rooms),
                    "a captain who pumped the whole ship down must be able to bring the whole ship back");
    }

    // ── One atmosphere, however many rooms it is standing in ─────────────────────────────────────────

    private static HullVenting.Space Room(string name, bool shut, bool vented = false) =>
        new(name, DoorShut: shut, Vented: vented, Infested: false, HoldsSurvivor: false);

    [Fact]
    public void ADoggedHatchIsItsOwnAtmosphere()
    {
        // Owner: "if we only want to evacuate it then the doors to it need to be sealed."
        HullVenting.Space[] ship = [Room("DEEP HOLD", shut: true), Room("BRIDGE", shut: false)];

        Assert.Equal(["DEEP HOLD"], HullVenting.SharedAtmosphere("DEEP HOLD", ship));
    }

    [Fact]
    public void AnOpenHatchPutsARoomInOneVolumeWithTheCorridorAndEveryOtherOpenRoom()
    {
        // "But if we evacuate multiple spaces then doors between those do not need to be sealed."
        HullVenting.Space[] ship =
        [
            Room("BRIDGE", shut: false),
            Room("CREW SPACES", shut: false),
            Room("DEEP HOLD", shut: true),
        ];

        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere("BRIDGE", ship);

        Assert.Contains("BRIDGE", volume);
        Assert.Contains("CREW SPACES", volume);
        Assert.Contains(HullVenting.SpineName, volume);
        Assert.DoesNotContain("DEEP HOLD", volume);   // dogged, and therefore spared
    }

    [Fact]
    public void ReachingIsSymmetric()
    {
        // Whichever end you press, the same atmosphere answers — the property that makes this a volume
        // rather than a direction.
        HullVenting.Space[] ship = [Room("BRIDGE", shut: false), Room("CREW SPACES", shut: false)];

        Assert.Equal(HullVenting.SharedAtmosphere("BRIDGE", ship),
                     HullVenting.SharedAtmosphere("CREW SPACES", ship));
        Assert.Equal(HullVenting.SharedAtmosphere("BRIDGE", ship),
                     HullVenting.SharedAtmosphere(HullVenting.SpineName, ship));
    }

    [Fact]
    public void TheCorridorAloneIsTheCorridorWhenEveryHatchIsDogged()
    {
        HullVenting.Space[] ship = [Room("BRIDGE", shut: true), Room("CREW SPACES", shut: true)];

        Assert.Equal([HullVenting.SpineName], HullVenting.SharedAtmosphere(HullVenting.SpineName, ship));
    }

    [Fact]
    public void ABiggerVolumeTakesLongerAndPaysMore()
    {
        HullVenting.Space[] open = [Room("BRIDGE", shut: false), Room("CREW SPACES", shut: false)];

        (double bigSeconds, int bigCharges) = HullVenting.PumpJob(HullVenting.SharedAtmosphere("BRIDGE", open));
        (double oneSeconds, int oneCharges) = HullVenting.PumpJob(["BRIDGE"]);

        Assert.True(bigSeconds > oneSeconds);
        Assert.True(bigCharges > oneCharges);
    }

    [Fact]
    public void TheBoardNamesEveryRoomAPumpWillReachBeyondTheOnePressed()
    {
        // The only check the owner wanted: "make sure we don't evacuate a room by accident of leaving its
        // door open." It warns, naming names; it never refuses.
        HullVenting.Space[] ship = [Room("BRIDGE", shut: false), Room("CREW SPACES", shut: false)];
        IReadOnlyList<string> volume = HullVenting.SharedAtmosphere("BRIDGE", ship);

        string line = HullVenting.PumpReachesFurtherLine("BRIDGE", volume);

        Assert.Contains("CREW SPACES", line, System.StringComparison.Ordinal);
        Assert.Contains(HullVenting.SpineName, line, System.StringComparison.Ordinal);

        // The room you PRESSED is named once, as the thing being exceeded — never again in the list of
        // what else is going, which is the only part the captain is being warned about.
        string listed = line[(line.IndexOf(": ", System.StringComparison.Ordinal) + 2)..];
        Assert.DoesNotContain("BRIDGE", listed, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ASealedRoomWarnsAboutNothingBecauseItReachesNothing()
    {
        HullVenting.Space[] ship = [Room("DEEP HOLD", shut: true)];

        Assert.Equal(string.Empty,
                     HullVenting.PumpReachesFurtherLine("DEEP HOLD",
                         HullVenting.SharedAtmosphere("DEEP HOLD", ship)));
    }

    [Fact]
    public void EveryShipHerHatchesCanMakeObeysThePartitionSymmetryAndIsolationLaws()
    {
        // LAB 42 · the 256-ship sweep, held in CI. Eight compartments with one hatch each is 2^8 ships —
        // small enough that these three properties are not sampled, they are EXHAUSTED.
        //
        //   PARTITION  every space is in the volume it names, so a pump can never empty a room twice nor
        //              leave one unaccounted for while the charge arithmetic quietly drifts.
        //   SYMMETRY   press any member and the same atmosphere answers. This is the property that makes it
        //              a VOLUME rather than a direction, and the first one a hand-written pair of ifs breaks.
        //   ISOLATION  a dogged hatch is alone, whatever the rest of her is doing.
        string[] names = new string[WreckLayout.Compartments.Length];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = WreckLayout.Compartments[i].Name;
        }

        for (int mask = 0; mask < 1 << names.Length; mask++)
        {
            var ship = new List<HullVenting.Space>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                ship.Add(new HullVenting.Space(names[i], DoorShut: (mask & (1 << i)) != 0, Vented: false,
                                               Infested: false, HoldsSurvivor: false));
            }

            var everySpace = new List<string>(names) { HullVenting.SpineName };
            foreach (string space in everySpace)
            {
                IReadOnlyList<string> volume = HullVenting.SharedAtmosphere(space, ship);

                Assert.Contains(space, volume);
                foreach (string member in volume)
                {
                    Assert.Equal(volume, HullVenting.SharedAtmosphere(member, ship));
                }
            }

            for (int i = 0; i < names.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    Assert.Single(HullVenting.SharedAtmosphere(names[i], ship));
                }
            }
        }
    }

    [Fact]
    public void TheNumberOfAtmospheresIsExactlyOnePerDoggedHatchPlusWhateverStillBreathesWithTheCorridor()
    {
        // The binomial that falls out of the sweep — C(8,k) ships have k+1 volumes — is the model checking
        // its own arithmetic. If this ever drifts, the door graph has grown an edge nobody meant to add.
        string[] names = new string[WreckLayout.Compartments.Length];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = WreckLayout.Compartments[i].Name;
        }

        for (int mask = 0; mask < 1 << names.Length; mask++)
        {
            var ship = new List<HullVenting.Space>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                ship.Add(new HullVenting.Space(names[i], DoorShut: (mask & (1 << i)) != 0, Vented: false,
                                               Infested: false, HoldsSurvivor: false));
            }

            var distinct = new HashSet<string>(System.StringComparer.Ordinal);
            var everySpace = new List<string>(names) { HullVenting.SpineName };
            foreach (string space in everySpace)
            {
                distinct.Add(string.Join("+", HullVenting.SharedAtmosphere(space, ship)));
            }

            int dogged = System.Numerics.BitOperations.PopCount((uint)mask);
            Assert.Equal(dogged + 1, distinct.Count);
        }
    }
}

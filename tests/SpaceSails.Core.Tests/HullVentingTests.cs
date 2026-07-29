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
}

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
    public void ASealedCompartmentIsReady_AndBlowingItClearsTheNest()
    {
        HullVenting.VentOutcome o = HullVenting.Vent(Space(infested: true));

        Assert.True(o.Blown);
        Assert.True(o.InfestationCleared);
        Assert.False(o.SurvivorKilled);
        Assert.Contains("nest goes with it", o.Line);
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
}

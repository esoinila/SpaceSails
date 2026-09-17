using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1225 · <b>THE VOID WATCH ASKS THE CLAMP.</b>
///
/// <para><b>What was found, and by whom.</b> #1223's audit walked every reader of the three dock fields and
/// found one it deliberately did not patch: <c>RunTheVoidWatch</c>'s own sentence reads <i>"A ship with fuel,
/// <b>a clamp on a berth</b>, or one plotted burn left in her is not adrift"</i> — and the argument it handed
/// <see cref="VoidRule.IsAdrift"/> was <c>_docked</c>, which is not the clamp. <c>_docked</c> is
/// <c>UpdateDockStatus</c>'s port-zone flag: 0.067 AU of earth/mars/venus, and FALSE at four of the seven
/// berths in <c>sol.json</c>. A dry, plan-less ship tied up at The Tilt was kept off the twenty-day death
/// clock by the THIRD arm (<c>AHavenStillTakesHer</c>, which cannot help seeing the berth it is sitting on)
/// and never by the flag the sentence named.</para>
///
/// <para><b>The audit's verdict, which this file is the record of.</b> The port zone is NOT a mistake and is
/// not removed: this page already treats "inside a planetary market's catchment" as <i>somewhere, and not the
/// void</i> — the ship's own <c>Adrift</c> flag is <c>_reactionMassPulses == 0 &amp;&amp; !_docked</c>, #266's
/// rescue offer stands up on exactly that, and a tow is one press away there. Passing the clamp INSTEAD would
/// have hung a death clock on a dry ship drifting inside Earth's catchment, which is a world the game really
/// builds. So the fix adds the clamp as its own arm and keeps the port zone as its own arm, and
/// <see cref="APortZoneAloneStillKeepsHerOffTheClock"/> is here to make sure a future tidy-up cannot quietly
/// drop the half that was never wrong.</para>
///
/// <para><b>This is a latent hazard, not a live bug, and the guards say which is which.</b>
/// <see cref="AClampedDryPlanlessShipNeverStartsTheClockAtAnyBerth"/> is the BEHAVIOUR PIN: it is green on
/// the old body too, because the third arm gets there first at every berth in the scenario. The two guards
/// that go RED on the old body are the ones that blind the haven sweep first
/// (<see cref="TheClampAloneKeepsTheClockStoppedWhenTheSweepCannotSee"/> and the port-zone twin), which is
/// deliberately a FAULT INJECTION rather than a shipping world: it asks whether the arm the sentence names is
/// load-bearing on its own, and before this lane it was not. That is the whole claim — a captain with a steel
/// arm on his hull should not owe his life to a twenty-day trajectory sweep agreeing with the berth he is
/// bolted to.</para>
///
/// <para><b>No dock field is written by hand anywhere in this file</b> (#1223's law,
/// <c>NoGuardHandsThePageAWorldItsOwnCodeCannotBuild</c>). Every berth is reached through
/// <c>ClampOntoHaven</c> — the one door the ⚓ press, the honest auto-dock and the <c>?dock=</c> boot all go
/// through — and the port-zone flags are always whatever <c>UpdateDockStatus</c>, their one writer, says.</para>
/// </summary>
public sealed class TheVoidWatchAsksTheClampTests
{
    /// <summary>Every berth the game can clamp onto in the shipping sky, off the same registry read #1223's
    /// sweep uses — so a haven added to a scenario is covered here for free and no berth id is typed into
    /// this file.</summary>
    private static IReadOnlyList<CelestialBody> TheBerths
    {
        get
        {
            ICelestialEphemeris sky = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
            IReadOnlyList<CelestialBody> berths = DockableHavens.All(sky);
            Assert.True(berths.Count > 0, "sol.json has no dockable haven, so this whole file sweeps nothing.");
            return berths;
        }
    }

    // ── (a) THE BEHAVIOUR PIN: what is true today, and stays true ─────────────────────────────────────

    /// <summary>
    /// #1225 · <b>A CLAMPED, DRY, PLAN-LESS SHIP NEVER STARTS THE VOID CLOCK — AT EVERY BERTH.</b>
    ///
    /// <para>The whole roster, not the berth that was noticed, because the bug was never about The Tilt: it
    /// was about which question the watch asked. Each berth is named rather than stopping at the first, since
    /// "one of seven" and "seven of seven" are different diagnoses.</para>
    ///
    /// <para><b>This one is green on the old body as well</b>, and that is reported rather than hidden: it
    /// pins the outcome a captain actually gets. The guard that can tell the two bodies apart is the next
    /// one down.</para>
    /// </summary>
    [Fact]
    public void AClampedDryPlanlessShipNeverStartsTheClockAtAnyBerth()
    {
        var doomed = new List<string>();
        foreach (CelestialBody berth in TheBerths)
        {
            Pages.Map map = ADryPlanlessShipTiedUpAt(berth.Id);
            LetTheWatchLook(map, days: 2, theSweepIsBlind: false);

            if (TheClockIsRunning(map))
            {
                doomed.Add($"{berth.Id} — declared on day {Read(map, "_voidDeclaredDay")}");
            }
        }

        Assert.True(doomed.Count == 0,
            "the void's twenty-day death clock started on a ship with a steel arm on her hull, tied to a "
            + "berth with a floor to stand on. The watch's own sentence says a clamp on a berth is not "
            + "adrift:\n  " + string.Join("\n  ", doomed));
    }

    // ── (b) THE LAW: which arm is actually holding her ────────────────────────────────────────────────

    /// <summary>
    /// #1225 · <b>THE CLAMP HOLDS HER ON ITS OWN.</b> The same roster, with the haven sweep blinded — the
    /// third arm made to answer <i>"nothing out there takes her"</i> — so the only thing left between a
    /// berthed captain and a death clock is the arm the watch's sentence names.
    ///
    /// <para><b>A fault injection, said plainly.</b> No shipping path produces a blind sweep at a berth: a
    /// clamped ship sits on the haven it would be swept against, so the projection finds it. That is exactly
    /// why this cannot be a live bug and exactly why it is worth pinning — the sentence's own arm was doing
    /// no work, and one refactor of <c>AHavenStillTakesHer</c> (a tightened threshold, an edge-of-ribbon
    /// rule, a sweep that returns early) would have turned a berth into a death clock with every other guard
    /// in the tree still green.</para>
    ///
    /// <para><b>Proven RED</b> against the pre-#1225 body of the watch (<c>VoidRule.IsAdrift</c> handed
    /// <c>_docked</c> instead of the clamp): the four OUTER berths fail — The Tilt, The Deep, the Red Eye and
    /// Ringside — while the three inner ones pass, because they happen to sit inside their planet's
    /// catchment. That split is the bug's own shape and nothing else produces it.</para>
    /// </summary>
    [Fact]
    public void TheClampAloneKeepsTheClockStoppedWhenTheSweepCannotSee()
    {
        var doomed = new List<string>();
        foreach (CelestialBody berth in TheBerths)
        {
            Pages.Map map = ADryPlanlessShipTiedUpAt(berth.Id);
            LetTheWatchLook(map, days: 2, theSweepIsBlind: true);

            if (TheClockIsRunning(map))
            {
                doomed.Add($"{berth.Id} ({berth.Name}) — _docked={Read(map, "_docked")}, "
                    + $"clock declared on day {Read(map, "_voidDeclaredDay")}");
            }
        }

        Assert.True(doomed.Count == 0,
            "with the twenty-day haven sweep blinded, a ship BOLTED TO A BERTH started the void's death "
            + "clock — so the arm the watch's own sentence names (\"a clamp on a berth\") is not the arm "
            + "holding her. This is what #1225 exists to end; the berths that fall through are the ones "
            + "outside a planet's 0.067 AU market catchment:\n  " + string.Join("\n  ", doomed));
    }

    /// <summary>
    /// #1225 · <b>…AND SO DOES THE PORT ZONE, WHICH WAS NEVER THE WRONG HALF.</b> The audit's other finding,
    /// made into a law: a dry, plan-less ship free-flying inside a planetary market's catchment — no clamp,
    /// no arm, nothing to stand on — is still not adrift, exactly as she was before this lane touched
    /// anything.
    ///
    /// <para>This is the guard that stops the tempting "fix" — passing the clamp INSTEAD of the port zone.
    /// That would read beautifully against the sentence and would start a death clock on a captain the rest
    /// of the page already calls stranded-but-reachable: the ship's own <c>Adrift</c> flag is
    /// <c>_reactionMassPulses == 0 &amp;&amp; !_docked</c> and #266's tow is one press away in there. The
    /// sweep is blinded here for the same reason as above — otherwise the third arm answers first and this
    /// asks nothing.</para>
    ///
    /// <para>She is put in the catchment the way #1223's law says to: the ship is placed where it means and
    /// <c>UpdateDockStatus</c> — the one writer of those two fields — is left to answer.</para>
    /// </summary>
    [Fact]
    public void APortZoneAloneStillKeepsHerOffTheClock()
    {
        Pages.Map map = ADryPlanlessShipInsideEarthsCatchment();

        Assert.True((bool)Read(map, "_docked")!, "the ship is not in a port zone, so this guard asks nothing.");
        Assert.Null(Read(map, "_dockedHavenId"));

        LetTheWatchLook(map, days: 0, theSweepIsBlind: true);

        Assert.False(TheClockIsRunning(map),
            "a dry ship inside a planetary market's catchment started the void's twenty-day death clock. "
            + "That is not what the port-zone arm meant before #1225 and it is not what it means after: "
            + "#266's rescue tow is one press away in there, and the ship's own ADRIFT banner says "
            + "stranded, not doomed.");
    }

    // ── (c) THE ANTI-VACUOUS HALF ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1225 · <b>AND A SHIP THAT REALLY IS ADRIFT STILL GETS HER CLOCK.</b> Everything above is a guard
    /// that says "the clock does NOT start", and a watch that had been wired to a constant <c>false</c>
    /// would pass every one of them. So: out past everything, dry tank, nothing plotted, no berth within a
    /// twenty-day projection — and the void declares, through the real sweep with nothing blinded.
    ///
    /// <para>The pair below it is the same world with one pulse in the tank, which is the first arm of the
    /// law doing its job: proof that what declared the clock was the state and not the bench.</para>
    /// </summary>
    [Fact]
    public void AGenuinelyAdriftShipStillStartsTheClock()
    {
        Pages.Map lost = ADryPlanlessShipOutPastEverything();
        LetTheWatchLook(lost, days: 0, theSweepIsBlind: false);

        Assert.True(TheClockIsRunning(lost),
            "a dry, plan-less ship sixty AU from anything, with no berth on any plotted future, did NOT "
            + "start the void clock — so every 'the clock stays stopped' guard in this file is vacuous.");
        Assert.Equal(VoidRule.NothingToldYet + 1, (int)Read(lost, "_voidLastToldDay")!);   // day 0 was told

        Pages.Map fuelled = ADryPlanlessShipOutPastEverything();
        Set(fuelled, "_reactionMassPulses", 1);
        LetTheWatchLook(fuelled, days: 0, theSweepIsBlind: false);

        Assert.False(TheClockIsRunning(fuelled),
            "one pulse in the tank is the law's first arm, and it no longer keeps her out of the void.");
    }

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A ship tied up at a berth with a dry tank and nothing plotted — the exact state the void clock is
    /// written for, minus the one thing the watch's sentence says saves her.
    ///
    /// <para>The berthing goes through <c>ClampOntoHaven</c>, the one door every berthing in the game shares
    /// (<c>ClampOntoHaven</c> itself stales the plan's future nodes and disarms any pending insert, so the
    /// second arm is emptied by the clamp rather than by this bench). Only the tank is written, and a tank
    /// at zero is a state the game reaches by flying.</para>
    /// </summary>
    private static Pages.Map ADryPlanlessShipTiedUpAt(string berthId)
    {
        Pages.Map map = AWokenPage();
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody dock = sky.Bodies.First(b => b.Id == berthId);

        Invoke(map, "ClampOntoHaven", dock, sky.Position(berthId, (double)Read(map, "SimTime")!), null);
        Set(map, "_reactionMassPulses", 0);
        Frame(map);

        Assert.Equal(berthId, (string?)Read(map, "_dockedHavenId"));
        Assert.False((bool)Invoke(map, "APlanStepCanStillFire")!, "the clamp left a plan step able to fire.");
        return map;
    }

    /// <summary>
    /// A dry ship inside Earth's market catchment and clamped to nothing — placed by position and then left
    /// to <c>UpdateDockStatus</c> to classify, which is #1223's law about how a port zone may be built in a
    /// test. Half the catchment radius out, read off the page's own constant so the number is not typed a
    /// second time; her velocity is Earth's so she rides along in it rather than falling out of it.
    /// </summary>
    private static Pages.Map ADryPlanlessShipInsideEarthsCatchment()
    {
        Pages.Map map = AWokenPage();
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        double simTime = (double)Read(map, "SimTime")!;
        var catchment = (double)typeof(Pages.Map).GetField("DockRadiusMeters", Hidden)!.GetValue(null)!;

        Vector2d earth = sky.Position("earth", simTime);
        const double h = 1.0;
        Vector2d earthVel = (sky.Position("earth", simTime + h) - sky.Position("earth", simTime - h)) / (2 * h);

        Set(map, "_ship", new ShipState(earth + new Vector2d(catchment / 2, 0), earthVel, simTime));
        Set(map, "_reactionMassPulses", 0);
        Frame(map);
        return map;
    }

    /// <summary>Out past everything, at rest, with a dry tank — nothing the twenty-day projection can find,
    /// which is the void's own definition of the word.</summary>
    private static Pages.Map ADryPlanlessShipOutPastEverything()
    {
        Pages.Map map = AWokenPage();
        Set(map, "_ship", new ShipState(new Vector2d(9e12, 9e12), Vector2d.Zero, 0.0));
        Set(map, "_reactionMassPulses", 0);
        Frame(map);
        return map;
    }

    /// <summary>The castaway bench's page with the world-ready gate thrown — the one flag <c>BuildWorld</c>
    /// sets on its far side, and the first thing <c>RunTheVoidWatch</c> refuses to run without.</summary>
    private static Pages.Map AWokenPage()
    {
        Pages.Map map = Boot("void-watch");
        Set(map, "_worldReady", true);
        Assert.True((bool)Invoke(map, "CaptainWasAboardHer")!, "the watch will not run: he is not aboard her.");
        return map;
    }

    /// <summary>
    /// Whole sim-days rolling past a ship that is not flying anywhere, in the tick's own order and through
    /// the page's own verbs: the clock advances, <c>HoldAtDock</c> re-pins a berthed hull onto the haven's
    /// rail, <c>UpdateDockStatus</c> (the one writer) re-answers the port-zone flags, and then the watch
    /// looks. That is <c>Map.Sim.Tick.Step</c>'s sequence with everything that cannot move a void input left
    /// out — and the watch is built to be asked this way, since it resolves whole days as they roll past
    /// rather than counting frames.
    ///
    /// <para><c>days: 0</c> is one look and no time passing, which is the whole question for "does the clock
    /// START": the watch declares on the first look that finds her adrift.</para>
    /// </summary>
    private static void LetTheWatchLook(Pages.Map map, int days, bool theSweepIsBlind)
    {
        for (int day = 0; day <= days; day++)
        {
            if (day > 0)
            {
                var ship = (ShipState)Read(map, "_ship")!;
                Set(map, "_ship", ship with { SimTime = ship.SimTime + VoidRule.DaySeconds });
                Invoke(map, "HoldAtDock");
                Invoke(map, "UpdateDockStatus");
            }

            if (theSweepIsBlind)
            {
                TheSweepGoesBlind(map);
            }

            Invoke(map, "RunTheVoidWatch");
        }
    }

    /// <summary>
    /// The third arm, made to see nothing. <c>_voidHavenInReach</c> is the sweep's cached answer and
    /// <c>_voidSweptDay</c> is the day it was taken, so stamping today's date on a FALSE is exactly the
    /// state the watch is in when its projection finds no berth — and the sweep, already answered for today,
    /// does not run and overwrite it.
    /// </summary>
    private static void TheSweepGoesBlind(Pages.Map map)
    {
        Set(map, "_voidHavenInReach", false);
        Set(map, "_voidSweptDay", VoidRule.DayIndex((double)Read(map, "SimTime")!));
    }

    private static bool TheClockIsRunning(Pages.Map map) =>
        (long)Read(map, "_voidDeclaredDay")! != VoidRule.ClockNotRunning;
}

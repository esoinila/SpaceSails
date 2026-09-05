using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #417 slice 1 · <b>ONE WHOLE CASE, WALKED ON A REAL PAGE.</b>
///
/// <para>Core's guards hold the GRAPH — that it invents nothing, that the record clears the older papers,
/// that the two settlings add up. None of that says the captain can actually get to the end of it, and this
/// repository's own lesson about the lift that only went down is that reachability is the half no arithmetic
/// can see. So this file stages a world and walks the whole thing: a bar with a floor, a top the captain
/// takes, a woman who crosses to it, a case taken, three leads answered through the three seams the game
/// already had, a red herring cleared, a berth reached, a choice made, and a purse and a ledger that moved
/// by exactly what Core said they would.</para>
///
/// <h3>What is driven and what is proved by reading the source, and why the split is where it is</h3>
///
/// <para>Two of the three leads live behind a piece of world this bench cannot stand up: the paper is picked
/// up out of a ruin on a moon (which wants a whole excursion, a lattice and a searched building), and the
/// witness is a console press on a bar patron (which wants the rota to have seated that particular regular
/// on that particular watch). Their CASE HALVES are driven directly and their WIRING is asserted off the
/// shipped source — <see cref="TheThreeLeadsAreWiredToTheSeamsTheGameAlreadyHad"/> — because a lead that is
/// answered by a method nobody calls is the #603 shape: a control that quietly does nothing.</para>
///
/// <para>Everything else is the real page: the real deck, the real walker band, the real approach, the real
/// scrim arbiter, the real frame.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheFinderWalksTheWholeCaseTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string TheRedEye = "red-eye";
    private const string TheOtherPort = "ringside-exchange";
    private const string AMoon = "miranda";
    private const string ThreadId = "417d1e5c0a9b4f27836ea1c40b5d9e28";

    /// <summary>How many hulls the staged wave carries. The former-name pool is twenty-four and each hull
    /// draws nought to three, so a wave this wide has two of them answering to one name with near certainty
    /// — and <see cref="TheStagedWorldReallyHasACaseInIt"/> asserts it rather than hoping.</summary>
    private const int WaveSize = 28;

    // ══ THE WHOLE WALK ═══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <b>SHE CROSSES THE FLOOR, THE CASE IS TAKEN, AND IT ENDS AT A BERTH WITH A CHOICE ON IT.</b>
    ///
    /// <para>The one drive, in the order a player would live it. Each step asserts the thing that step is
    /// FOR, and the walk is a walk — her route has more than one point in it, which is the difference
    /// between crossing a room and teleporting with a plate on.</para>
    ///
    /// <para><b>Watched RED</b> by raising her card in <c>AdvanceTheFinder</c> instead of in
    /// <c>SheReachesTheFindersTable</c> — <i>"her card was up before she had crossed the floor"</i>; and by
    /// dropping <c>TheRevealAtTheBerth</c> out of the walked frame — <i>"the captain stood at the
    /// confrontation berth with the whole trail walked and nothing happened"</i>.</para>
    /// </summary>
    [Fact]
    public void TheCaseIsWalkedFromABarTableToTheBerthItEndsAt()
    {
        Pages.Map map = SheIsAtTheTable();
        FinderCase.Case offered = TheCardsCase(map);

        // (1) She really walked, and the hook she says names the port she is sitting in.
        Assert.True(RoutePoints(map) > 1, "she arrived without crossing the floor.");
        Assert.Equal(TheRedEye, offered.ClientPortId);
        Assert.Contains("The Red Eye", offered.TheHook, StringComparison.Ordinal);
        Assert.Null(Field(map, "_finderCase"));   // nothing is owned until it is taken

        // (2) TAKEN — and the lead card lands in the field book under the case's own subjects.
        Invoke(map, "AnswerTheFinder", true);
        FinderCase.Case c = (FinderCase.Case)Field(map, "_finderCase")!;
        Assert.Equal(offered, c);
        Assert.True(Progress(map).Taken);
        Assert.Null(Field(map, "_finderCard"));

        FieldNote lead = Notes(map).Last();
        Assert.Equal(FinderCase.LeadBody, lead.Text);
        Assert.Contains(CaseSubjects.On(lead), s => s.Name == FinderCase.DisplayName);
        Assert.Contains(CaseSubjects.On(lead), s => s.Name == c.ClientPortName);

        // (3) LEAD ONE — the witness, and only at the port her own rota favours.
        Set(map, "_dockedHavenId", AnyPortBut(c.WitnessPortId));
        Invoke(map, "TheWitnessMayHaveSeenIt", c.WitnessId);
        Assert.False(Progress(map).WitnessHeard, "the witness answered at a port she does not drink at.");

        Set(map, "_dockedHavenId", c.WitnessPortId);
        Invoke(map, "TheWitnessMayHaveSeenIt", "SOMEBODY ELSE ENTIRELY");
        Assert.False(Progress(map).WitnessHeard, "a stranger answered the case's own lead.");

        Invoke(map, "TheWitnessMayHaveSeenIt", c.WitnessId);
        Assert.True(Progress(map).WitnessHeard);
        Assert.Equal(c.TheHook, Notes(map).Last().Text);
        Assert.Contains(CaseSubjects.On(Notes(map).Last()), s => s.Name == c.WitnessId);

        // (4) LEAD TWO — the paper, and only on the ground the case names. Every other ground files what it
        // has always filed, which is the clause that stops this feature swallowing every paper in the game.
        Assert.Equal("", Invoke(map, "ThePapersSubjectsAt", "callisto"));
        Assert.False(Progress(map).PaperFound);

        Assert.Equal(c.SubjectLine, Invoke(map, "ThePapersSubjectsAt", c.PaperSiteBodyId));
        Assert.True(Progress(map).PaperFound);

        // (5) LEAD THREE — the hull, read off the press that puts a ledger of names on the screen.
        Invoke(map, "SetInterestTarget", c.HullId);
        Assert.True(Progress(map).HullRead);
        Assert.Equal(ShipHistories.For(c.HullId).FormerNamesLine, Notes(map).Last().Text);
        Assert.Contains(CaseSubjects.On(Notes(map).Last()), s => s.Name == c.HullCallsign);

        // FOUR ENTRIES, FOUR DIFFERENT SENTENCES, ONE HEADING. The book is where a detective's work
        // accumulates, and a lead whose entry was a duplicate of the last one would be silently dropped by
        // the field book's own dedupe — which is exactly what happened on this guard's first run.
        Assert.Equal(3, Notes(map).Count(n => CaseSubjects.On(n).Any(s => s.Name == FinderCase.DisplayName)));
        Assert.Equal(3, Notes(map).Select(n => n.Text).Distinct(StringComparer.Ordinal).Count());

        // (6) THE RED HERRING — and she says the canon pass's own sentence when her record clears her.
        Assert.False(Progress(map).HerringCleared);
        Invoke(map, "SetInterestTarget", c.HerringHullId);
        Assert.True(Progress(map).HerringCleared);
        Assert.Equal(FinderCase.HerringCleared, TheLineOnScreen(map));

        // (7) THE TRAIL IS WALKED, and the confrontation is at the berth and nowhere else.
        Assert.True(Progress(map).TrailWalked);
        Set(map, "_dockedHavenId", AnyPortBut(c.BerthPortId));
        Invoke(map, "TheRevealAtTheBerth");
        Assert.Null(Field(map, "_finderReveal"));

        Set(map, "_dockedHavenId", c.BerthPortId);
        Invoke(map, "TheRevealAtTheBerth");
        Assert.NotNull(Field(map, "_finderReveal"));
        Assert.True(Progress(map).Revealed);
        Assert.Null(Field(map, "_finderOutcome"));   // the two verbs first; the sentence after
    }

    /// <summary>
    /// <b>THE BRIBE: THE PURSE MOVES AND SO DOES THE PORT'S MEMORY OF YOU</b> — and the band is the meter's
    /// own step, banked against whoever runs this berth through the one call every crossing goes through.
    ///
    /// <para><b>Watched RED:</b> the <c>BankTheCrossing</c> call dropped out of <c>SettleTheCase</c> —
    /// <i>"Assert.Equal() Failure · Expected: 4 · Actual: 0"</i>, the bribe becoming free.</para>
    /// </summary>
    [Fact]
    public void TakingTheBribePaysTheManAndBurnsThePort()
    {
        Pages.Map map = AtTheReveal();
        FinderCase.Case c = (FinderCase.Case)Field(map, "_finderCase")!;
        int before = (int)Field(map, "_credits")!;
        string outfit = SiteOperator.Of(c.BerthPortId).Id;
        Assert.Equal(0, IllegalHeat.HeatAt(Contacts(map), outfit));

        Invoke(map, "SettleTheCase", FinderCase.Outcome.Bribed);

        Assert.Equal(before + c.PayCredits + c.BribeCredits, (int)Field(map, "_credits")!);
        Assert.Equal(IllegalHeat.ABand, IllegalHeat.HeatAt(Contacts(map), outfit));
        Assert.Equal(c.PayReputation, Contacts(map).For(FinderCase.ContactId).Goodwill);
        Assert.Equal(FinderCase.AfterTheBribe, (string?)Field(map, "_finderOutcome"));

        // …and a second press settles nothing further. The choice, once made, is made.
        Invoke(map, "SettleTheCase", FinderCase.Outcome.TurnedIn);
        Assert.Equal(before + c.PayCredits + c.BribeCredits, (int)Field(map, "_credits")!);
        Assert.Equal(FinderCase.AfterTheBribe, (string?)Field(map, "_finderOutcome"));
    }

    /// <summary>
    /// <b>TURNING HIM IN: A RUNG OF STANDING, AND NOBODY REMEMBERS ANYTHING.</b> The other half of the
    /// choice, and the clause that says the two arms really are different — a settling that burned the port
    /// either way would pass the bribe's guard perfectly.
    ///
    /// <para><b>Watched RED:</b> <c>FinderCase.PayFor</c>'s turn-in arm handed <c>IllegalHeat.ABand</c> —
    /// <i>"turning a man in burned the port"</i>.</para>
    /// </summary>
    [Fact]
    public void TurningHimInEarnsStandingAndBurnsNobody()
    {
        Pages.Map map = AtTheReveal();
        FinderCase.Case c = (FinderCase.Case)Field(map, "_finderCase")!;
        int before = (int)Field(map, "_credits")!;

        Invoke(map, "SettleTheCase", FinderCase.Outcome.TurnedIn);

        Assert.Equal(before + c.PayCredits, (int)Field(map, "_credits")!);
        Assert.Equal(0, IllegalHeat.HeatAt(Contacts(map), SiteOperator.Of(c.BerthPortId).Id));
        Assert.Equal(c.PayReputation + FinderCase.ReputationForTurningHimIn,
                     Contacts(map).For(FinderCase.ContactId).Goodwill);
        Assert.Equal(FinderCase.AfterTurningIn, (string?)Field(map, "_finderOutcome"));
    }

    /// <summary>
    /// <b>SHE COMES BACK TO PAY, ONCE, AND THEN SHE IS DONE WITH IT.</b> A settled case is a debt she owes,
    /// and the next bar she finds the captain sitting alone in is where she pays it — with the payoff line
    /// and no second case behind it, because slice 1 is one case.
    ///
    /// <para><b>Watched RED:</b> the <c>PaidOff</c> write dropped out of <c>AnswerTheFinder</c> —
    /// <i>"she crossed the floor to say it a second time"</i>.</para>
    /// </summary>
    [Fact]
    public void TheNextTimeSheFindsYouSittingAloneSheIsPaying()
    {
        Pages.Map map = AtTheReveal();
        Invoke(map, "SettleTheCase", FinderCase.Outcome.TurnedIn);
        Invoke(map, "CloseTheReveal");

        // A different berth is a different evening, and she is at this one to settle up.
        MoveTo(map, TheOtherPort);
        Assert.True(SitAtAFreeTop(map));
        RunUntilHerCardIsUp(map);

        (FinderCase.Case Case, bool Paying) card = TheCard(map);
        Assert.True(card.Paying, "she came back with a case instead of with the money.");
        Assert.False(Progress(map).PaidOff);

        Invoke(map, "AnswerTheFinder", false);
        Assert.True(Progress(map).PaidOff);

        // …and she does not do it twice. A third evening has nothing for her to cross a floor about.
        MoveTo(map, TheRedEye);
        Assert.True(SitAtAFreeTop(map));
        for (int i = 0; i < 400; i++)
        {
            RunFrames(map, 1);
        }

        Assert.Null(Field(map, "_finderCard"));
    }

    /// <summary>
    /// <b>SHE ASKS ONCE AN EVENING, WHATEVER THE ANSWER WAS.</b> A finder who comes back after a no is a
    /// different, worse scene — the walk-in's own law, and it is kept the same way, by a visit fold.
    ///
    /// <para><b>Watched RED:</b> <c>_finderAskedThisVisit</c> never written in <c>HerCaseGoesUp</c> —
    /// <i>"she came back to the same table in the same evening"</i>.</para>
    /// </summary>
    [Fact]
    public void SheAsksOncePerBerthAndTheCaseGoesWithHerOnANo()
    {
        Pages.Map map = SheIsAtTheTable();

        // THE FOLD IS SET THE MOMENT SHE ARRIVES, and it is asserted on its own rather than through the
        // silence afterwards: TWO latches keep her off the floor after an answer (this one, and the
        // answered flag the approach's own gate reads), so a guard that only watched for a second card
        // would stay green with either of them deleted. This is the one that says "once an evening".
        Assert.True((bool)Field(map, "_finderAskedThisVisit")!,
                    "she arrived without the evening remembering that she had.");

        Invoke(map, "AnswerTheFinder", false);
        Assert.True((bool)Field(map, "_finderAnswered")!);

        Assert.Null(Field(map, "_finderCase"));   // a graph nobody took is not a graph the captain owns
        Assert.False(Progress(map).Taken);

        for (int i = 0; i < 400; i++)
        {
            RunFrames(map, 1);
        }

        Assert.Null(Field(map, "_finderCard"));

        // …and the NEXT port is a new evening, which deals its own case.
        MoveTo(map, TheOtherPort);
        Assert.True(SitAtAFreeTop(map));
        RunUntilHerCardIsUp(map);
        Assert.Equal(TheOtherPort, TheCardsCase(map).ClientPortId);
    }

    /// <summary>
    /// <b>THE CASE AND THE TRAIL RIDE THE VAULT.</b> A captain halfway along is halfway along after a
    /// reload, and the graph he comes back to is the graph he was given — not one re-derived off a traffic
    /// wave that has been dealt again since.
    ///
    /// <para><b>Watched RED:</b> <c>BuildFinderSection</c> made to write only the progress row —
    /// <i>"Assert.Equal() Failure · the captain woke up with a different hull to look for"</i>.</para>
    /// </summary>
    [Fact]
    public void TheCaseComesBackOffTheVaultUnchanged()
    {
        Pages.Map map = SheIsAtTheTable();
        Invoke(map, "AnswerTheFinder", true);
        FinderCase.Case c = (FinderCase.Case)Field(map, "_finderCase")!;
        Invoke(map, "TheCaseReadsThisHull", c.HullId);

        var section = (FinderSection?)Invoke(map, "BuildFinderSection");
        Assert.NotNull(section);

        // A whole other life, loaded over this one.
        Pages.Map fresh = AshoreAt(TheRedEye);
        Assert.Null(Field(fresh, "_finderCase"));
        Invoke(fresh, "RestoreFinderSection", section);

        Assert.Equal(c, (FinderCase.Case)Field(fresh, "_finderCase")!);
        Assert.True(Progress(fresh).Taken);
        Assert.True(Progress(fresh).HullRead);
        Assert.False(Progress(fresh).PaperFound);

        // …and a file from before the finder existed wakes with no case, which is what it had.
        Invoke(fresh, "RestoreFinderSection", (FinderSection?)null);
        Assert.Null(Field(fresh, "_finderCase"));
        Assert.False(Progress(fresh).Taken);
    }

    // ══ THE WIRING THE BENCH CANNOT STAND A WORLD UP FOR ═════════════════════════════════════════════════

    /// <summary>
    /// <b>EVERY LEAD IS ANSWERED BY A SEAM THE GAME ALREADY HAD, AND EVERY ONE OF THOSE SEAMS CALLS IT.</b>
    ///
    /// <para>The half of the trail this bench cannot drive: the paper is a press inside a ruin on a moon and
    /// the witness is a press on a seated regular. Their case halves are driven above; what is asserted here
    /// is that the shipping code really reaches them — because a lead answered by a method nobody calls is a
    /// control that quietly does nothing, which is this repository's #603 shape.</para>
    ///
    /// <para><b>Watched RED:</b> the <c>ThePapersSubjectsAt</c> argument replaced with <c>""</c> in the
    /// shelter's papers arm — <i>"the papers arm no longer asks the case whose ground it is standing
    /// on"</i>.</para>
    /// </summary>
    [Fact]
    public void TheThreeLeadsAreWiredToTheSeamsTheGameAlreadyHad()
    {
        Assert.Contains("ThePapersSubjectsAt(body)", Page("Map.Surface.Shelter.cs"), StringComparison.Ordinal);
        Assert.Contains("TheWitnessMayHaveSeenIt(giver)", Page("Map.Quests.Offers.cs"), StringComparison.Ordinal);
        Assert.Contains("TheCaseReadsThisHull(", Page("Map.Combat.Boarding.cs"), StringComparison.Ordinal);
        Assert.Contains("TheCaseReadsThisHull(", Page("Map.Alerts.cs"), StringComparison.Ordinal);

        // …and she is stepped by the room's own metabolism, and the reveal by the walked frame.
        Assert.Contains("AdvanceTheFinder(bar)", Page("Map.BarWalkers.cs"), StringComparison.Ordinal);
        Assert.Contains("TheRevealAtTheBerth()", Page("Map.Sim.Tick.cs"), StringComparison.Ordinal);

        // …and the vault carries both halves.
        Assert.Contains("Finder = BuildFinderSection()", Page("Map.Vault.cs"), StringComparison.Ordinal);
        Assert.Contains("RestoreFinderSection(vault.Finder)", Page("Map.Vault.cs"), StringComparison.Ordinal);
    }

    // ══ THE BENCH IS A WORLD AND NOT A WISH ══════════════════════════════════════════════════════════════

    /// <summary>
    /// THE STAGED WAVE REALLY HAS TWO HULLS THAT HAVE ANSWERED TO ONE NAME, and the ports it stages really
    /// keep a berth to be next to. Without this every drive above could be passing on a world where the
    /// bench's own <c>Assert.NotNull</c> is doing all the work.
    /// </summary>
    [Fact]
    public void TheStagedWorldReallyHasACaseInIt()
    {
        ICelestialEphemeris world = TheWorld();
        Assert.True(DockRoster.BerthsAt(world, TheRedEye) >= FinderCase.BerthsAConfrontationNeeds);
        Assert.True(DockRoster.BerthsAt(world, TheOtherPort) >= FinderCase.BerthsAConfrontationNeeds);
        Assert.Contains(world.Bodies, b => ShuttleExcursion.IsLandableSurface(b.Kind));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        bool shared = false;
        foreach (NpcShip ship in TheTraffic(world))
        {
            foreach (string name in ShipHistories.For(ship.Id).BareFormerNames)
            {
                shared |= !seen.Add(name);
            }
        }

        Assert.True(shared,
            $"a wave of {WaveSize} hulls and no two of them ever answered to one name — this bench has no "
            + "case in it, so nothing above it is proving anything.");
    }

    // ══ THE BENCH ════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>A page clamped onto a berth, standing in its bar, with a real world and a real traffic wave
    /// under it and the salesman and the walk-in both kept off the floor — so the only body that can be
    /// crossing it is hers.</summary>
    private static Pages.Map AshoreAt(string berth)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris world = TheWorld();
        Set(map, "_ephemeris", world);
        Set(map, "_npcStates", TheWave(world));
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", (bool?)false);
        Set(map, "_walkInCheat", (bool?)false);
        Set(map, "_finderCheat", (bool?)true);
        MoveTo(map, berth);
        return map;
    }

    /// <summary>Cast off and clamp on somewhere else — the fold every one of these files keeps, driven
    /// through the page's own docked-deck build rather than by writing fields behind it.</summary>
    private static void MoveTo(Pages.Map map, string berth)
    {
        Set(map, "_dockedHavenId", berth);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
    }

    /// <summary>Sit down and run the room until she is standing at the table with her card up.</summary>
    private static Pages.Map SheIsAtTheTable()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        Assert.True(SitAtAFreeTop(map), "no free top in this bar — the captain cannot sit alone anywhere.");
        RunUntilHerCardIsUp(map);
        return map;
    }

    /// <summary>…and the same, walked all the way to the confrontation with the choice still open.</summary>
    private static Pages.Map AtTheReveal()
    {
        Pages.Map map = SheIsAtTheTable();
        Invoke(map, "AnswerTheFinder", true);
        FinderCase.Case c = (FinderCase.Case)Field(map, "_finderCase")!;

        Set(map, "_dockedHavenId", c.WitnessPortId);
        Invoke(map, "TheWitnessMayHaveSeenIt", c.WitnessId);
        Invoke(map, "ThePapersSubjectsAt", c.PaperSiteBodyId);
        Invoke(map, "TheCaseReadsThisHull", c.HullId);

        Set(map, "_dockedHavenId", c.BerthPortId);
        Invoke(map, "TheRevealAtTheBerth");
        Assert.NotNull(Field(map, "_finderReveal"));
        return map;
    }

    private static void RunUntilHerCardIsUp(Pages.Map map)
    {
        for (int i = 0; i < 900 && Field(map, "_finderCard") is null; i++)
        {
            RunFrames(map, 1);
        }

        Assert.True(Field(map, "_finderCard") is not null, "she never reached the table.");
    }

    /// <summary>A world with two busy ports (so both keep a ring of berths) and a moon under one of them for
    /// a paper to be lying on. The tonnage is the scenario's own language — <see cref="ArrivalTube"/> reads
    /// routes, so the bench states routes rather than asserting a tier.</summary>
    private static ICelestialEphemeris TheWorld() =>
        new CircularOrbitEphemeris(
        [
            new CelestialBody("sol", "Sol", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("jupiter", "Jupiter", "sol", 1.267e17, 6.99e7, 7.78e11, 3.7e5, 0),
            new CelestialBody("saturn", "Saturn", "sol", 3.79e16, 5.82e7, 1.43e12, 2.2e5, 0),
            new CelestialBody(AMoon, "Miranda", "saturn", 8.0e10, 2.35e5, 1.3e8, 1.2e5, 0, BodyKind.Moon),
            new CelestialBody("callisto", "Callisto", "jupiter", 7.2e12, 2.4e6, 1.88e9, 1.4e6, 0, BodyKind.Moon),
            new CelestialBody(TheRedEye, "The Red Eye", "jupiter", 0, 0, 5e8, 4e4, 0,
                BodyKind.Station, IsHaven: true),
            new CelestialBody(TheOtherPort, "Ringside Exchange", "saturn", 0, 0, 5e8, 4e4, 0,
                BodyKind.Station, IsHaven: true),
        ],
        new TrafficDefinition
        {
            Routes =
            [
                new RouteDefinition
                {
                    From = TheRedEye, To = TheOtherPort, Cargo = "ore",
                    Weight = ArrivalTube.GreatPortTonnage,
                },
                new RouteDefinition
                {
                    From = TheOtherPort, To = TheRedEye, Cargo = "grain",
                    Weight = ArrivalTube.GreatPortTonnage,
                },
            ],
        });

    /// <summary>The traffic actually flying, generated the way the game generates it — so the hulls the case
    /// picks between carry the service records <see cref="ShipHistories.For"/> really deals them.</summary>
    private static IReadOnlyList<NpcShip> TheTraffic(ICelestialEphemeris world) =>
        TrafficSchedule.Generate(world, 0x417u, WaveSize, world.Traffic);

    private static Pages.Map.NpcState[] TheWave(ICelestialEphemeris world) =>
        [.. TheTraffic(world).Select(s => new Pages.Map.NpcState { Ship = s, State = s.InitialState })];

    /// <summary>Sit the captain at the first top in THIS bar that answers [E] — the bar's own tops, never
    /// the boat's cantina, which travels into every docked deck on the captain's own plan.</summary>
    private static bool SitAtAFreeTop(Pages.Map map)
    {
        var deck = (DeckPlan)Field(map, "_deckPlan")!;
        string berth = (string)Field(map, "_dockedHavenId")!;
        IReadOnlyList<DeckReachability.Point> tops = HavenInterior.BarBand(berth)!.Value.Tops;

        foreach (DeckPlan.ConsoleSpot spot in deck.Consoles.Where(
                     c => c.Kind == DeckPlan.ConsoleKind.BarTop
                          && tops.Any(t => Math.Abs(t.X - c.X) < 0.5 && Math.Abs(t.Y - c.Y) < 0.5)))
        {
            Set(map, "_avatarX", (double)spot.X);
            Set(map, "_avatarY", (double)spot.Y);
            if ((bool)Invoke(map, "TryTakeBarTop")!)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Run the bar's own frame, the way the walked view runs it.</summary>
    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    /// <summary>A port in this world that is NOT this one — so "only there" is asserted against somewhere
    /// real rather than against a made-up id. One clause at a time, because the witness's port and the
    /// confrontation's are two different questions and a single "wrong port" that happened to satisfy one of
    /// them would silently answer the other with the right one.</summary>
    private static string AnyPortBut(string port) => port == TheRedEye ? TheOtherPort : TheRedEye;

    /// <summary>How many points her route has. One is a teleport with a plate on it.</summary>
    private static int RoutePoints(Pages.Map map)
    {
        foreach (object body in (IEnumerable)Field(map, "_barAfoot")!)
        {
            object walk = Get(body, "Walk")!;
            if (string.Equals((string)Get(walk, "Plate")!, FinderCase.Plate, StringComparison.Ordinal))
            {
                return ((ICollection)Get(walk, "Route")!).Count;
            }
        }

        return 0;
    }

    private static (FinderCase.Case Case, bool Paying) TheCard(Pages.Map map) =>
        ((FinderCase.Case, bool))Field(map, "_finderCard")!;

    private static FinderCase.Case TheCardsCase(Pages.Map map) => TheCard(map).Case;

    private static FinderCase.Progress Progress(Pages.Map map) =>
        (FinderCase.Progress)Field(map, "_finderProgress")!;

    private static ContactLedger Contacts(Pages.Map map) => (ContactLedger)Field(map, "_contacts")!;

    private static IReadOnlyList<FieldNote> Notes(Pages.Map map) =>
        (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!;

    private static string? TheLineOnScreen(Pages.Map map) =>
        (string?)Get(Field(map, "_pulse")!, "Message");

    // ── Reading the shipped source ───────────────────────────────────────────────────────────────────────

    private static string Page(string relative)
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
        {
            at = at.Parent;
        }

        return File.ReadAllText(Path.Combine(
            at?.FullName ?? throw new DirectoryNotFoundException("no repo root above the test binary"),
            "src", "SpaceSails.Client", "Pages", relative));
    }

    // ── Reflection plumbing ──────────────────────────────────────────────────────────────────────────────

    private static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name."))
        .GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        if (typeof(Pages.Map).GetField(name, Hidden) is { } field)
        {
            field.SetValue(map, value);
            return;
        }

        (typeof(Pages.Map).GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}`.")).SetValue(map, value);
    }

    private static object? Get(object o, string name) =>
        (o.GetType().GetField(name, Hidden)?.GetValue(o))
        ?? (o.GetType().GetProperty(name, Hidden)
            ?? throw new InvalidOperationException($"{o.GetType().Name} has no `{name}`.")).GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name."))
        .Invoke(map, args);
}

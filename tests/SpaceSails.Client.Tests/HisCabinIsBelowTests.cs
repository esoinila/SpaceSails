using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1253 slice 2 · <b>HIS CABIN IS BELOW — the route grew three legs and a floor, and the captain has to
/// decide to follow him onto it.</b>
///
/// <para>Owner, 2026-09-20: <i>"could we add a basement level to the observation deck station, so the tailing
/// task could start from the basement cabin and end at the observation deck? <b>Otherwise the followed
/// distance is easily very short.</b> The main hall could have multiple elevators… good for tailing."</i></para>
///
/// <para>The laws here are the ruling's own clauses, one per case: the route is deterministic, both legs are
/// walkable, the cabin leaf never opens, a captain who takes his car and hangs back at the band arrives to
/// the vanish exactly as before, and a captain on the wrong car simply loses him — no card, nothing spent,
/// and nothing anywhere that tells him he guessed wrong.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class HisCabinIsBelowTests
{
    private static string Berth => ObservationWalk.HavenId;

    private static string Person => TheTail.ThePersonOfInterest(Berth);

    private static int Cars => HavenInterior.TheCagesAt(Berth).Count;

    // ── (a) THE ROUTE IS DECIDED ONCE, AND IT IS THE SAME EVERY TIME ─────────────────────────────────────

    /// <summary>
    /// <b>WHICH CABIN, WHICH CAR DOWN, WHICH CAR UP — SEEDED, NOT ROLLED.</b> Which car a man takes is the
    /// whole of the craft the owner asked for, and a choice re-rolled per visit would be a coin flip in a
    /// mechanic's clothes: there would be nothing to learn and nothing to be wrong about.
    ///
    /// <para>Asked a hundred times, and the answers do not move. Asked of a DIFFERENT person at the same
    /// berth and of the same person at a different berth, and they do — a route that answered the same for
    /// everybody would be green and would pin nothing (the fifth bug class).</para>
    ///
    /// <para><b>Proven RED</b> by seeding the three choices on the sim clock: the first assertion fails on
    /// the second call.</para>
    /// </summary>
    [Fact]
    public void HisCabinAndHisTwoCarsAreSeededAndNeverRolled()
    {
        int cabin = TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins);
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        int up = TheTailsNight.TheCarHeTakesUp(Berth, Person, Cars);

        Assert.InRange(cabin, 0, HavenLevels.Cabins - 1);
        Assert.InRange(down, 0, Cars - 1);
        Assert.InRange(up, 0, Cars - 1);

        for (int again = 0; again < 100; again++)
        {
            Assert.Equal(cabin, TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins));
            Assert.Equal(down, TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars));
            Assert.Equal(up, TheTailsNight.TheCarHeTakesUp(Berth, Person, Cars));
        }

        // …and it is a fact about THIS person at THIS berth. A route that answered the same for everybody
        // would be a constant with an argument list.
        var seen = new HashSet<(int, int, int)>();
        foreach (string who in PatronRota.Roster)
        {
            foreach (string port in HavenInterior.InteriorBodyIds)
            {
                seen.Add((
                    TheTailsNight.HisCabin(port, who, HavenLevels.Cabins),
                    TheTailsNight.TheCarHeTakesDown(port, who, Cars),
                    TheTailsNight.TheCarHeTakesUp(port, who, Cars)));
            }
        }

        Assert.True(seen.Count > 1, "every person at every port takes the same car to the same cabin.");

        // A station with nothing under it has no cabin and no car, and says so rather than answering 0.
        Assert.Equal(-1, TheTailsNight.HisCabin(Berth, Person, 0));
        Assert.Equal(-1, TheTailsNight.TheCarHeTakesDown(Berth, Person, 0));
        Assert.Equal(-1, TheTailsNight.TheCarHeTakesUp(Berth, Person, 0));
    }

    /// <summary>
    /// <b>THE WAIT BEHIND THE LEAF IS THE ESCORT'S FICTION, READ FROM THE OTHER SIDE — AND IT IS THREE
    /// MINUTES.</b>
    ///
    /// <para><see cref="Interior.Escort.PatienceFraction"/> is the ceiling on the shape this beat is: a leaf
    /// a captain is refused at, somebody on the far side of it, and a captain deciding how long to stand
    /// there. A twentieth of it is what the owner already ruled once, on the walk's own wait
    /// (<i>"YES, shorten the wait."</i>) — and the two derivations landing on the same number is the check
    /// that the length is right rather than a coincidence anybody typed.</para>
    /// </summary>
    [Fact]
    public void TheCabinWaitIsDerivedFromTheEscortsPatienceAndLandsOnTheWalksOwnWait()
    {
        Assert.Equal(Escort.PatienceFraction / 20.0, TheTailsNight.CabinWaitFraction, 12);
        Assert.Equal(ObservationWalk.TheWaitSeconds, TheTailsNight.CabinWaitSeconds, 6);
        Assert.True(
            TheTailsNight.CabinWaitSeconds < Escort.PatienceSeconds,
            "the wait behind the cabin leaf is the escort's whole hour — the thing the owner shortened.");

        // …and a leg the captain is not watching takes as long as a man walking it. Never a shortcut and
        // never a drag: either one is the world arranging itself round who happens to be looking.
        Assert.Equal(10.0 / NpcWalk.PaceDu, TheTailsNight.LegSeconds(10.0), 9);
        Assert.Equal(0, TheTailsNight.LegSeconds(0));
    }

    // ── (b) BOTH LEGS ARE WALKABLE ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY LEG OF HIS NIGHT IS A WALK A BODY CAN MAKE.</b> Five points, four legs, on the two decks he
    /// actually crosses — his chair, the doors he takes down, his own cabin, the doors he comes up on, and
    /// the rail.
    ///
    /// <para>A route that could not be walked would not fail loudly: the planner answers null, nothing is
    /// placed, and the beat quietly does not happen — which is the honest refusal in play and completely
    /// invisible to a player who has never seen it work. So it is asserted leg by leg, on the real decks,
    /// through the same A* the walkers use.</para>
    ///
    /// <para><b>Proven RED</b> by seeding his cabin outside the row (<c>HisCabin</c> made to answer
    /// <c>HavenLevels.Cabins</c>): <c>his cabin has no doorstep on the service level.</c></para>
    /// </summary>
    [Fact]
    public void EveryLegOfHisNightIsWalkable()
    {
        DeckPlan above = HavenInterior.DockedDeck(Berth)!;
        DeckPlan below = HavenInterior.DockedDeck(Berth, level: HavenLevels.ServiceLevel)!;

        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        int up = TheTailsNight.TheCarHeTakesUp(Berth, Person, Cars);
        int cabin = TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins);

        DeckReachability.Point downDoors = HavenInterior.TheCageLandingAt(Berth, down)!.Value;
        DeckReachability.Point upDoors = HavenInterior.TheCageLandingAt(Berth, up)!.Value;
        DeckReachability.Point? doorstep = HavenInterior.TheCabinDoorstepAt(Berth, cabin);
        Assert.True(doorstep is not null, "his cabin has no doorstep on the service level.");
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;

        // LEG 1 · his chair → the doors he takes down, on the concourse.
        DeckReachability.Point chair = HisChair(above);
        Walks(above, chair, downDoors, "his chair to the car he takes down");

        // LEG 2 · those doors, one floor down → his own cabin.
        Walks(below, downDoors, doorstep!.Value, "the car's doors to his cabin");

        // LEG 3 · his cabin → the doors he comes back up on.
        Walks(below, doorstep.Value, upDoors, "his cabin to the car he comes up on");

        // LEG 4 · those doors, back on the concourse → the rail. #1254's own leg.
        Walks(above, upDoors, rail, "the car he came up on to the rail");
    }

    private static void Walks(DeckPlan deck, DeckReachability.Point a, DeckReachability.Point b, string leg)
    {
        // The whole complex WITH the T's own reach on the end of it — the gallery hangs a long way west of
        // the ring, and a lattice that stopped at the hall would call the last leg unwalkable because it had
        // stopped looking. Measured off the room's own published box, never typed.
        (double gx0, double gy0, _, double gy1) = HavenInterior.TheGalleryBox(Berth)!.Value;
        (double, double, double, double) whole =
            (gx0 - 4, Math.Min(gy0 - 4, -10), 40, Math.Max(gy1 + 4, 90));
        Assert.True(
            DeckReachability.Standable(a.X, a.Y, DeckPlan.AvatarRadius, deck.CollisionField),
            $"{leg}: he cannot stand where the leg begins.");
        Assert.True(
            DeckReachability.Standable(b.X, b.Y, DeckPlan.AvatarRadius, deck.CollisionField),
            $"{leg}: he cannot stand where the leg ends.");
        Assert.True(
            DeckReachability.CanReach(a, b, deck.CollisionField, DeckPlan.AvatarRadius, whole),
            $"{leg}: the floor will not let him walk it, so the beat quietly does not happen.");
    }

    /// <summary>Where his chair is, as a place a body stands beside it — the room's own sounding, and the
    /// rota's own seat. The rota is asked at the watch the bench docks at below, so this and the page agree
    /// about which chair is his.</summary>
    private static DeckReachability.Point HisChair(DeckPlan above)
    {
        foreach (HavenInterior.SeatedRegular r in HavenInterior.ResolveRegulars(Berth, TheWatch))
        {
            if (r.Present && string.Equals(r.Id, Person, StringComparison.Ordinal))
            {
                DeckReachability.Point? beside = HavenInterior.BesideATop(
                    new DeckReachability.Point(r.X, r.Y), DeckPlan.AvatarRadius, above.CollisionField);
                Assert.True(beside is not null, "the stone allows no side of his own top.");
                return beside!.Value;
            }
        }

        // The rota does not seat him at this watch, so the leg starts nowhere. The bench below docks at a
        // watch that does; this arm exists so a rota that ever stops seating him fails loudly rather than
        // proving nothing.
        throw new InvalidOperationException(
            $"{Person} is not seated at {Berth} on the watch these laws are stated at.");
    }

    /// <summary>The watch every case in this file docks at — the one the bench freezes, so the rota, the
    /// route and the guards are all reading one evening.</summary>
    private const double TheWatch = 0.0;

    // ── (c) THE LEAF ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HIS CABIN DOOR NEVER OPENS FOR THE CAPTAIN, AND NOTHING SAYS IT IS HIS.</b>
    ///
    /// <para>#563's ruling is the whole of the lock — the door is TIME, not a key — and here there is not
    /// even time: he is behind it, and the only thing on the far side of that leaf is the fact that he is
    /// there. The plate is a number. Nothing in the deck, the plates or the labels names him.</para>
    /// </summary>
    [Fact]
    public void TheCabinLeafIsLockedAndCarriesNoName()
    {
        DeckPlan below = HavenInterior.DockedDeck(Berth, level: HavenLevels.ServiceLevel)!;
        int cabin = TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins);

        Assert.Equal(HavenLevels.Cabins, below.Doors.Count(d => d.Locked));
        Assert.Equal(HavenLevels.Cabins, below.Doors.Length);

        string plate = HavenInterior.CabinPlatesAt(Berth)[cabin];
        Assert.Equal(HavenLevels.CabinPlate(cabin + 1), plate);

        var everyString = new List<string>();
        everyString.AddRange(below.Consoles.Select(c => c.Label));
        everyString.AddRange(below.RoomLabels.Select(l => l.Text));
        foreach (string who in PatronRota.Roster)
        {
            Assert.DoesNotContain(everyString, s => s.Contains(who, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ── (d) THE TAIL, ON A LIVE PAGE ────────────────────────────────────────────────────────────────────

    /// <summary>A live page clamped on at Selene Gate, ashore in the bar, past last call — the moment his
    /// night begins.</summary>
    private static Pages.Map PastLastCall(string canvasId)
    {
        Pages.Map map = Boot(canvasId);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Berth);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Berth, (double)Read(map, "SimTime")!), null);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!, "the ashore boot refused this berth.");

        Set(map, "_dockVisitSimTime", TheWatch);
        Set(map, "SimTime", PatronRota.WatchSeconds * (Egress.LastCallFraction + 0.05));

        // ── AN EVENING THE ROOM HAS NOBODY SCHEDULED OUT OF ─────────────────────────────────────────────
        //
        // #731's hours and #1199's tail both want the same man out of the same chair, and on a watch whose
        // schedule happens to name him the hours get there first: the room walks him out through a cellar
        // leaf, `_barLeft` has him, and the tail finds no chair to start a route from. That is the SHIPPED
        // interaction (the walk simply does not happen that evening, silently) and it is not this lane's to
        // change — but a law about his route cannot be stated on a night the route was never walked.
        //
        // So the schedules are given the answer they are allowed to give: nobody is going and nobody is
        // coming. Empty is an ANSWER this room gave (null is a question it has not been asked — the room's
        // own distinction, in Map.BarWalkers), and a shift with no scheduled churn on it is an ordinary
        // evening rather than a contrivance.
        Set(map, "_barGoing", (IReadOnlyList<Egress.Move>)[]);
        Set(map, "_barComing", (IReadOnlyList<Egress.Move>)[]);
        return map;
    }

    private static IList<Pages.Map.Walker> Afoot(Pages.Map map) =>
        (IList<Pages.Map.Walker>)Read(map, "_barAfoot")!;

    private static string Leg(Pages.Map map) => Read(map, "_nightLeg")!.ToString()!;

    private static int Floor(Pages.Map map) => (int)Read(map, "_havenFloor")!;

    private static void Frames(
        Pages.Map map, double seconds, Func<bool>? until = null, Action<Pages.Map>? posture = null)
    {
        const double dt = 1.0 / 30.0;
        for (double t = 0; t < seconds; t += dt)
        {
            posture?.Invoke(map);
            Set(map, "SimTime", (double)Read(map, "SimTime")! + dt);
            Invoke(map, "AdvanceBarWalkers", dt);
            if (until is not null && until())
            {
                return;
            }
        }
    }

    /// <summary>
    /// #1253 · <b>THE CAPTAIN WALKS AHEAD OF HIM</b>, which is a posture the owner has already ruled on:
    /// <i>"They should act normal even if I tail from ahead."</i>
    ///
    /// <para>It is the posture these corridor laws are stated from on purpose. A captain standing BEHIND him
    /// and inside the legibility band is somebody a man stands aside for (#1245's <c>LettingYouPass</c>, and
    /// the shipped rule) — he stops, turns, and waits until the captain has gone past, which is a beat and
    /// not a stall, and it is #1245's law rather than this lane's. Standing the captain in front of him
    /// exercises exactly the thing these cases are about: <b>the route</b>.</para>
    /// </summary>
    private static void WalkAheadOfHim(Pages.Map map, double du)
    {
        Pages.Map.Walker? him = Afoot(map).FirstOrDefault(w => w.Who == Person);
        if (him is null)
        {
            return;
        }

        double dx = him.Walk.For.X - him.Walk.X, dy = him.Walk.For.Y - him.Walk.Y;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 0.001)
        {
            return;
        }

        Set(map, "_avatarX", him.Walk.X + (dx / len * du));
        Set(map, "_avatarY", him.Walk.Y + (dy / len * du));
    }

    private static bool Ride(Pages.Map map, int level, int cage) =>
        (bool)Invoke(map, "RideTheHavenLiftTo", level, cage)!;

    /// <summary>Stand the captain a fixed distance back along the line the man is walking, which is the
    /// notice band's own axis — behind him, in his road, and inside the range a body is legible at.</summary>
    private static void HangBackBehindHim(Pages.Map map, double du)
    {
        Pages.Map.Walker? him = Afoot(map).FirstOrDefault(w => w.Who == Person);
        if (him is null)
        {
            return;
        }

        double dx = him.Walk.For.X - him.Walk.X, dy = him.Walk.For.Y - him.Walk.Y;
        double len = Math.Sqrt((dx * dx) + (dy * dy));
        if (len < 0.001)
        {
            return;
        }

        Set(map, "_avatarX", him.Walk.X - (dx / len * du));
        Set(map, "_avatarY", him.Walk.Y - (dy / len * du));
    }

    /// <summary>
    /// <b>HE GOES DOWN, AND THE ROUTE IS FIVE LEGS.</b> The owner's whole complaint about the shipped beat
    /// was that the followed distance is short; this is the measurement that it is not any more. He gets up
    /// after last call, crosses to a car, is gone off the concourse, and the night's clock has him on the
    /// SERVICE LEVEL — a floor the captain is not on and has to decide to follow him onto.
    ///
    /// <para><b>Proven RED</b> by making <c>BeginHisNight</c> always take the shipped one-leg road: the leg
    /// after he leaves his chair is the walk, not a car, and this names it.</para>
    /// </summary>
    [Fact]
    public void HeLeavesHisChairForACarAndTheNextLegIsAFloorDown()
    {
        Pages.Map map = PastLastCall("his-night-begins");

        Frames(map, 30, () => Leg(map) != "NotYet");
        Assert.Equal("ToTheCarDown", Leg(map));
        Assert.Contains(Afoot(map), w => w.Who == Person);
        Assert.Contains(Person, (IReadOnlySet<string>)Read(map, "_barLeft")!);

        // …and the leg he is walking ends at the doors of HIS car, not at the rail.
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        DeckReachability.Point doors = HavenInterior.TheCageLandingAt(Berth, down)!.Value;
        Pages.Map.Walker him = Afoot(map).First(w => w.Who == Person);
        Assert.Equal(doors.X, him.Walk.For.X, 1);
        Assert.Equal(doors.Y, him.Walk.For.Y, 1);

        // Walk him to those doors. He rides, so he comes off the concourse — and the night says why.
        Frames(map, 600, () => Leg(map) != "ToTheCarDown");
        Assert.Equal("ToHisCabin", Leg(map));
        Assert.Equal(HavenLevels.ServiceLevel, (int)Read(map, "_nightLegFloor")!);
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);

        // Nothing has been spent and nothing has been said. A man walked across a room and got into a lift.
        Assert.Null(Read(map, "_observationWalkSpentOn"));
    }

    /// <summary>
    /// <b>FOLLOW HIM ONTO THE RIGHT CAR AND HE IS THERE, WALKING TO HIS OWN DOOR — AND THEN HE IS BEHIND
    /// IT.</b> The second leg, played: the captain rides the car he watched the man take, arrives on the
    /// service level, and there is a body in the corridor. He walks to his cabin, goes in, and the wait
    /// begins.
    ///
    /// <para><b>Proven RED</b> by placing him at the leg's START whatever the clock says: he is standing at
    /// the car's doors a minute after he walked away from them.</para>
    /// </summary>
    [Fact]
    public void RidingHisCarPutsTheCaptainOnTheFloorHeIsWalking()
    {
        Pages.Map map = PastLastCall("follow-him-down");
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);

        Frames(map, 600, () => Leg(map) == "ToHisCabin");
        Assert.Equal("ToHisCabin", Leg(map));

        Assert.True(Ride(map, HavenLevels.ServiceLevel, down), "his own car refused to go down.");
        Assert.Equal(HavenLevels.ServiceLevel, Floor(map));

        // One frame of the room down here, and there he is.
        Frames(map, 1.0);
        Assert.Contains(Afoot(map), w => w.Who == Person);

        int cabin = TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins);
        DeckReachability.Point doorstep = HavenInterior.TheCabinDoorstepAt(Berth, cabin)!.Value;
        Pages.Map.Walker him = Afoot(map).First(w => w.Who == Person);
        Assert.Equal(doorstep.X, him.Walk.For.X, 1);
        Assert.Equal(doorstep.Y, him.Walk.For.Y, 1);

        // Let him reach it. He goes in; the leaf shuts; there is nobody in the corridor. The captain walks
        // ahead of him down the corridor rather than on his heels — the owner's own "tail from ahead", and
        // the posture that leaves #1245's standing-aside beat out of a law about a route.
        Frames(map, 600, () => Leg(map) == "Inside", m => WalkAheadOfHim(m, 6));
        Assert.Equal("Inside", Leg(map));
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);
        Assert.Null(Read(map, "_observationWalkSpentOn"));
    }

    /// <summary>
    /// <b>TAIL HIM ALL THE WAY AND THE ENDING IS THE ONE THAT SHIPPED.</b> Down on his car, through the
    /// wait, up on the car he comes up on, and then the captain hangs back at the notice band and lets him
    /// walk out over the drop — and on a look nobody is watching him, he is not there. The card comes at the
    /// rail after the wait, the note files under his name, and the beat is spent.
    ///
    /// <para>Every clause of that is #1254's and not a line of it was touched. What this case proves is that
    /// a route with three new legs in front of it still arrives at the same ending.</para>
    /// </summary>
    [Fact]
    public void TailingBothLegsArrivesAtTheVanishExactlyAsBefore()
    {
        Pages.Map map = PastLastCall("tail-both-legs");
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        int up = TheTailsNight.TheCarHeTakesUp(Berth, Person, Cars);

        Frames(map, 600, () => Leg(map) == "ToHisCabin");
        Assert.True(Ride(map, HavenLevels.ServiceLevel, down));
        Frames(map, 600, () => Leg(map) == "Inside", m => WalkAheadOfHim(m, 6));

        // The wait behind the leaf. The captain stands in the corridor and nothing happens, which is the
        // whole of what happens.
        Frames(map, TheTailsNight.CabinWaitSeconds + 5, () => Leg(map) == "ToTheCarUp");
        Assert.Equal("ToTheCarUp", Leg(map));

        Frames(map, 600, () => Leg(map) == "ToTheWalk", m => WalkAheadOfHim(m, 6));
        Assert.Equal("ToTheWalk", Leg(map));

        // Up on the car he came up on, and there he is, crossing the concourse.
        Assert.True(Ride(map, HavenLevels.Concourse, up));
        Frames(map, 1.0);
        Assert.Contains(Afoot(map), w => w.Who == Person);

        // Hang back at the band and let him go. He is off the floor on a look nobody is watching him.
        for (int frame = 0; frame < 60_000 && Afoot(map).Any(w => w.Who == Person); frame++)
        {
            HangBackBehindHim(map, FootTail.LegibleDu * 1.5);
            Frames(map, 1.0 / 30.0);
        }

        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);
        Assert.False(double.IsNaN((double)Read(map, "_walkGoneSince")!), "he went, and nothing recorded when.");

        // Walk out to the rail after the wait, and the card comes up — #1254's own ending, unchanged.
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;
        Set(map, "_avatarX", rail.X);
        Set(map, "_avatarY", rail.Y);
        Set(map, "SimTime", (double)Read(map, "SimTime")! + ObservationWalk.TheWaitSeconds + 1);
        Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);

        Assert.Equal(
            ObservationWalk.Key(Berth, Person), (string?)Read(map, "_observationWalkSpentOn"));
    }

    /// <summary>
    /// <b>HE IS ONLY EVER DRAWN ON THE FLOOR HIS LEG IS ON.</b> A station has two floors and one deck — the
    /// one the captain is standing on — so a man whose leg is in the service corridor must not be a body on
    /// the concourse, and a man crossing the concourse must not be one below.
    ///
    /// <para>It is the load-bearing half of the whole slice: it is what makes riding a car a DECISION. A
    /// walker drawn wherever the captain happens to be would hand him the man for free on both floors, and
    /// there would be nothing to guess, nothing to follow and nothing to lose.</para>
    ///
    /// <para>And it is the half a law about the wrong car cannot see, because both cars land on the SAME
    /// floor — watched happen: with the floor test replaced by <c>true</c>, every other case in this file
    /// stayed green. So it is stated on its own, across the one transition where the floors differ.</para>
    ///
    /// <para><b>Proven RED</b> by drawing him on whichever floor the captain is on
    /// (<c>bool hisFloor = true</c>): <c>Assert.DoesNotContain() Failure</c> — there is a man in the bar's
    /// concourse who is, by his own route, in a corridor one floor under it.</para>
    /// </summary>
    [Fact]
    public void HeIsOnlyEverDrawnOnTheFloorHisLegIsOn()
    {
        Pages.Map map = PastLastCall("one-floor-one-body");

        // His leg is the service corridor and the captain has NOT followed him down.
        Frames(map, 600, () => Leg(map) == "ToHisCabin");
        Assert.Equal("ToHisCabin", Leg(map));
        Assert.Equal(HavenLevels.Concourse, Floor(map));

        Frames(map, 5.0);
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);

        // Follow him down and there he is; the SAME leg, the same second, a different floor under the
        // captain's feet.
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        Assert.True(Ride(map, HavenLevels.ServiceLevel, down));
        Frames(map, 1.0);
        Assert.Contains(Afoot(map), w => w.Who == Person);

        // …and ride back up, and he is not up here either.
        Assert.True(Ride(map, HavenLevels.Concourse, down));
        Frames(map, 1.0);
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);
    }

    /// <summary>
    /// <b>THE WRONG CAR LOSES HIM, AND NOTHING TELLS YOU SO.</b> The losing rule, and it is the reason there
    /// are three cars at all: a captain who rides a car the man did not take arrives in a corridor with
    /// nobody in it, the beat goes on without him, and <b>no card is raised and nothing is spent</b>.
    ///
    /// <para>The walk is still there on a later visit, which is the other half of the rule: guessing wrong is
    /// a way to not get the scene today, never a way to lose it for ever.</para>
    ///
    /// <para><b>Proven RED</b> by materialising him on whichever floor the captain is standing on rather than
    /// on the one his leg is: there is a man in the corridor the captain has no business finding.</para>
    /// </summary>
    [Fact]
    public void TheWrongCarLosesHimAndCostsNothing()
    {
        Pages.Map map = PastLastCall("the-wrong-car");
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        int wrong = (down + 1) % Cars;

        Frames(map, 600, () => Leg(map) == "ToHisCabin");
        Assert.True(Ride(map, HavenLevels.ServiceLevel, wrong));

        // Walk about down here for the whole of his errand. He is somewhere on this floor by the clock, and
        // the captain is at the wrong end of it — so what a captain who guessed wrong sees is an empty
        // corridor and, in time, an empty one again.
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, wrong)!.Value;
        Set(map, "_avatarX", landing.X);
        Set(map, "_avatarY", landing.Y);
        Frames(map, 5.0);

        // He is NOT dealt onto the captain's own doorstep — the clock has him where his leg is.
        Assert.Null(Read(map, "_observationWalkSpentOn"));

        // The night runs on without him: the wait passes, he comes up, and the concourse leg finishes with
        // nobody watching it. Nothing is spent by any of that.
        Frames(map, TheTailsNight.CabinWaitSeconds + 600, () => Leg(map) == "ToTheWalk");
        Assert.Equal("ToTheWalk", Leg(map));
        Assert.Null(Read(map, "_observationWalkSpentOn"));
        Assert.Null(Read(map, "_storyCard"));

        // …and when the captain finally comes up and walks out to the rail, the room is empty and the card
        // is there to be earned. Losing him is not losing the beat.
        Frames(map, 120);
        Assert.True(Ride(map, HavenLevels.Concourse, wrong));
        DeckReachability.Point rail = HavenInterior.TheRailAt(Berth)!.Value;
        Set(map, "_avatarX", rail.X);
        Set(map, "_avatarY", rail.Y);
        Set(map, "SimTime", (double)Read(map, "SimTime")! + ObservationWalk.TheWaitSeconds + 1);
        Invoke(map, "AdvanceBarWalkers", 1.0 / 30.0);

        Assert.Equal(
            ObservationWalk.Key(Berth, Person), (string?)Read(map, "_observationWalkSpentOn"));
    }
}

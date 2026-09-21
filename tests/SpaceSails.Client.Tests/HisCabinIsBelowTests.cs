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

        // ── #1277 · AN ORDINARY EVENING, WITH THE ROOM'S OWN HOURS RUNNING ──────────────────────────────
        //
        // This bench used to hand the two schedules an empty answer, because #731's hours and #1199's tail
        // both wanted the same man out of the same chair and on a watch whose schedule named him the hours
        // got there first — `_barLeft` had him, the tail found no chair, and the route these laws are about
        // was never walked. The bench NAMED that silence and left it; #1277 ruled on it (the tail wins) and
        // fixed it in `TheWatchDecidesWhoGoes`, which now defers to the man the walk has claimed.
        //
        // So the contrivance is gone and these nine laws are stated on the room as it runs: whoever the shift
        // has going is going, whoever it has coming is coming, and the one chair the walk needs is the one
        // chair the hours will not touch. `TheTailWinsTheChairTests` is where that clause is stated; here it
        // is simply spent.
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

        // A MOMENT of it, and not a lifetime. This wait was five seconds and the corridor leg is only
        // about four (the landing to his own door is 8.6 du at NpcWalk.PaceDu), so the case used to ride
        // down onto the leg AFTER this one — and found a body anyway, because #1287's wait re-dealt him
        // onto the corridor to walk it a second time. It was green on the bug. Now the wait is a shut
        // leaf with nobody in front of it, so the case has to ask its question while its own leg is
        // running, and it says so out loud on the next line.
        Frames(map, 1.0);
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);

        // Follow him down and there he is; the SAME leg, the same second, a different floor under the
        // captain's feet.
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        Assert.True(Ride(map, HavenLevels.ServiceLevel, down));
        Assert.Equal("ToHisCabin", Leg(map));
        Frames(map, 1.0);
        Assert.Contains(Afoot(map), w => w.Who == Person);

        // …and ride back up, and he is not up here either.
        Assert.True(Ride(map, HavenLevels.Concourse, down));
        Frames(map, 1.0);
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);
    }

    /// <summary>
    /// #1281 · <b>A CAPTAIN STANDING AT THE DOORS HE IS WALKING TO DOES NOT STOP HIS NIGHT.</b>
    ///
    /// <para><b>What was played</b> (owner's QA, 2026-09-21, on the service level): ride up, wait, ride down,
    /// and about eighteen seconds later a body appears at the captain's own elbow at the car's landing —
    /// <i>"still standing in exactly the same spot after four more minutes"</i>. It is HIM, on the leg that
    /// ends at the car he rides back up on, and the night behind him had stopped dead with him: no ride, no
    /// concourse leg, no walk, no card. A captain who followed him properly was the one thing that could
    /// prevent the beat he followed him for.</para>
    ///
    /// <para><b>Why.</b> <see cref="NpcWalk"/>'s courtesy stops a walker before any step that would bring it
    /// inside one body-width of the captain, says <c>Doing.Waiting</c> and keeps its route — <i>"so the walk
    /// finishes itself the moment the doorway clears"</i>. A car's landing is the one square in this building
    /// that never clears: it is where a ride sets the captain down and where <c>[E]</c> finds the panel, so
    /// he is standing on it exactly when somebody else's leg ends there. So a leg of the night is over where
    /// it ENDS, at the courtesy's own width, and not where the route object gives up.</para>
    ///
    /// <para>The captain is stood at the car <b>he comes up on</b> and kept there for the whole errand — the
    /// posture that puts a body on the far end of a leg — and his notice is latched on, so this case also
    /// says that a man walking TOWARDS a captain is not a man letting him past.</para>
    ///
    /// <para><b>Proven RED</b> by taking the leg's own far end back out (a leg ending only when the route
    /// object stops being afoot): <c>Expected: ToTheWalk — Actual: ToTheCarUp</c>, with a body still standing
    /// one body-width off the captain when the frames run out.</para>
    /// </summary>
    [Fact]
    public void ACaptainStandingAtTheDoorsHeIsWalkingToDoesNotStopHisNight()
    {
        Pages.Map map = PastLastCall("standing-on-his-doorstep");
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);
        int up = TheTailsNight.TheCarHeTakesUp(Berth, Person, Cars);

        Frames(map, 600, () => Leg(map) == "ToHisCabin");
        Assert.Equal("ToHisCabin", Leg(map));
        Assert.True(Ride(map, HavenLevels.ServiceLevel, down), "his own car refused to go down.");

        // He has clocked the captain — the latch every en-route rule is gated on. Set rather than rolled:
        // no law here is about #436's eye.
        Set(map, "_walkNoticed", true);

        // …and the captain waits at the car he will come up on, which is the far end of his last corridor
        // leg. That is the whole of the posture: a captain who guessed right, standing where the doors are.
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, up)!.Value;
        void AtTheCar(Pages.Map m)
        {
            Set(m, "_avatarX", landing.X);
            Set(m, "_avatarY", landing.Y);
        }

        AtTheCar(map);

        Frames(map, 600, () => Leg(map) == "Inside", AtTheCar);
        Assert.Equal("Inside", Leg(map));
        Frames(map, TheTailsNight.CabinWaitSeconds + 60, () => Leg(map) == "ToTheCarUp", AtTheCar);
        Assert.Equal("ToTheCarUp", Leg(map));

        // The anti-vacuity clause: the leg this law is about has to actually END where the captain is
        // standing, or the case is a man walking somewhere else while somebody loiters.
        Frames(map, 1.0, posture: AtTheCar);
        Pages.Map.Walker onTheLeg = Assert.Single(Afoot(map), w => w.Who == Person);
        Assert.Equal(landing.X, onTheLeg.Walk.For.X, 1);
        Assert.Equal(landing.Y, onTheLeg.Walk.For.Y, 1);

        // …and he gets there, comes off the floor, rides, and the night is on its last leg.
        Frames(map, 600, () => Leg(map) == "ToTheWalk", AtTheCar);
        Assert.Equal("ToTheWalk", Leg(map));
        Assert.DoesNotContain(Afoot(map), w => w.Who == Person);
    }

    // ── (e) THE NIGHT RUNS FOR WHOEVER WATCHES — #1285, #1286, #1287 ────────────────────────────────────
    //
    // Three stalls the QA crew played on 2026-09-21, and every one of them is invisible to the nine cases
    // above because of HOW they drive the room: `Frames` calls `AdvanceBarWalkers` straight, so the frame
    // clock (`SurfaceSeconds`, off `_lastTimestampMs`) never moves, the notice roll is never asked, and the
    // stand-aside courtesy those cases are written around can never fire. These five drive the page through
    // its own `OnTick` — the shipping frame — and stand the captain exactly where the documented link stands
    // him, which is the whole of what was wrong.

    /// <summary>
    /// #1285 · <b>THE LINK THE TESTING DOC HANDS A PLAYER</b>, built the way the boot builds it: the clock is
    /// jumped BEFORE the clamp (<c>JumpTheClockBeforeSheArrives</c>, #1213), so the berth, the frozen watch,
    /// the rota and the schedule are one evening — and the captain is stood at the bar threshold by the same
    /// <c>StandAtTheBarThreshold</c> the cheat calls, which is the square this lane is about.
    /// </summary>
    private static Pages.Map AtTheDocumentedLink(string canvasId)
    {
        Pages.Map map = Boot(canvasId);
        Set(map, "SimTime", TheDocumentedHour * 3600.0);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Berth);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Berth, (double)Read(map, "SimTime")!), null);
        Assert.True((bool)Invoke(map, "StandAtTheBarThreshold")!, "the ashore boot refused this berth.");

        // The link is only worth booting if it really is past last call — the whole night hangs on that one
        // comparison, and a link that had drifted inside the fraction would prove nothing about anything.
        Assert.True(
            (double)Read(map, "IntoTheBarsWatch")! > PatronRota.WatchSeconds * Egress.LastCallFraction,
            $"?simhours={TheDocumentedHour} is no longer past last call, so there is no night to watch.");
        return map;
    }

    /// <summary>The hour the testing doc's §1 link boots at. Named once, because all three of these laws are
    /// stated at the link a tester is actually handed.</summary>
    private const double TheDocumentedHour = 7.5;

    /// <summary>Real frames, through the page's own <c>OnTick</c>, until the room answers or the clock runs
    /// out. The frame clock matters: it is what the notice roll and the courtesy are asked on.</summary>
    private static bool RunUntil(Pages.Map map, Func<bool> until, double seconds)
    {
        for (double t = 0; t < seconds; t += FrameSeconds)
        {
            Frame(map);
            if (until())
            {
                return true;
            }
        }

        return until();
    }

    private static Pages.Map.Walker? HimOnTheFloor(Pages.Map map) =>
        Afoot(map).FirstOrDefault(w => w.Who == Person);

    /// <summary>How long a man takes to walk between two points of this building, by the night's own
    /// arithmetic — never a number typed into a guard.</summary>
    private static double WalkSeconds(DeckReachability.Point a, DeckReachability.Point b) =>
        TheTailsNight.LegSeconds(Math.Sqrt(
            ((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))));

    /// <summary>
    /// #1285 · <b>THE NIGHT RUNS FOR A CAPTAIN WHO ONLY WATCHES.</b>
    ///
    /// <para><b>What was played</b> (QA, 2026-09-21): boot the documented link and touch nothing.
    /// <i>"He never gets up."</i> Seven frames over five minutes with the whole room frozen behind him — and
    /// one step of the captain, any step, and the entire two-leg night ran to the second.</para>
    ///
    /// <para><b>Why.</b> He DOES get up; he takes four strides and stops. <c>?ashore=1</c> stands the captain
    /// on the bar's own threshold, which is inside <see cref="ObservationWalk.OnHisHeelsDu"/> of the chair he
    /// rises from and squarely in his line to the cars — so the stand-aside courtesy (#1201/#1283) fires on
    /// his first stride, and it had no clock on it. A captain who WATCHES rather than walks never clears it,
    /// and the whole evening is behind that one body.</para>
    ///
    /// <para>The courtesy is a beat now and a beat ends
    /// (<see cref="ObservationWalk.StandAsideSeconds"/>). This case asserts BOTH halves: he really does stand
    /// aside (or the law would be about something else entirely), and then he gets on with his night without
    /// the captain moving a deck unit.</para>
    ///
    /// <para><b>Proven RED</b> on the shipped courtesy: <c>he never got off the concourse … still standing
    /// 4.2 du from a captain who has not moved</c>.</para>
    /// </summary>
    [Fact]
    public void TheNightRunsForACaptainWhoOnlyWatches()
    {
        Pages.Map map = AtTheDocumentedLink("he-only-watches");
        double x0 = (double)Read(map, "_avatarX")!, y0 = (double)Read(map, "_avatarY")!;

        // The room's own hours and the tail both run on the frame clock, so this is the shipping frame and
        // not `AdvanceBarWalkers` — see the note above this section.
        bool heStoodAside = false;
        double nearest = double.MaxValue;
        double lastSeenX = double.NaN, lastSeenY = double.NaN;
        bool offTheConcourse = RunUntil(
            map,
            () =>
            {
                if (HimOnTheFloor(map) is { } w)
                {
                    heStoodAside |= w.For.ToString() == "LettingYouPass";
                    lastSeenX = w.Walk.X;
                    lastSeenY = w.Walk.Y;
                    nearest = Math.Min(
                        nearest,
                        Math.Sqrt(((w.Walk.X - x0) * (w.Walk.X - x0)) + ((w.Walk.Y - y0) * (w.Walk.Y - y0))));
                }

                return Leg(map) is "ToHisCabin" or "Inside" or "ToTheCarUp" or "ToTheWalk";
            },
            TheWholeNightIsWatchedFor);

        // ── THE ANTI-VACUITY CLAUSES ──────────────────────────────────────────────────────────────────
        // Without these two, a link that had drifted the captain out of his line would be green about a
        // courtesy that never fired — the fifth bug class, in the one place this lane could be caught by it.
        Assert.True(
            heStoodAside,
            "he never stood aside at all, so this case is not about the courtesy it is a law about.");
        Assert.True(
            nearest <= ObservationWalk.OnHisHeelsDu,
            $"he never came within the {ObservationWalk.OnHisHeelsDu:0.#} du band of the captain's own "
            + $"spawn (nearest {nearest:0.0} du), so nothing here could have held him.");

        // …and the captain has not moved a deck unit, which is the whole posture.
        Assert.Equal(x0, (double)Read(map, "_avatarX")!, 9);
        Assert.Equal(y0, (double)Read(map, "_avatarY")!, 9);

        Assert.True(
            offTheConcourse,
            $"he never got off the concourse in {TheWholeNightIsWatchedFor:0} s — leg {Leg(map)}, "
            + $"standing at {lastSeenX:0.0},{lastSeenY:0.0}, "
            + $"{nearest:0.0} du from a captain who has not moved. That is the documented link booted and "
            + "watched, which is what the row tells a tester to do.");

        // …and the ROOM ran too, which is the other half of what was frozen: the hours emptied a chair of
        // their own beside the one the walk claimed.
        Assert.True(
            ((IReadOnlySet<string>)Read(map, "_barLeft")!).Count > 1,
            "the room's own hours never dealt anybody but the man the walk claimed.");
    }

    /// <summary>How long the room is watched for before a case gives up on it, in sim seconds — the minute
    /// the QA crew watched the documented link for before filing #1285, and comfortably more than the
    /// concourse's own longest leg takes a body to walk.</summary>
    private const double TheWholeNightIsWatchedFor = 60.0;

    /// <summary>
    /// #1286 · <b>FOLLOWING HIM ONTO HIS OWN CAR DOES NOT LAND THE CAPTAIN ON HIS SQUARE.</b>
    ///
    /// <para><b>What was played</b> (QA, 2026-09-21): note which car he took, walk to it, ride down — the
    /// row's own instruction — and stand still, which is what a man tailing somebody does.
    /// <i>"A man standing exactly where the car put you, as though he had waited."</i> Twelve screenshots
    /// over 245 seconds, one md5.</para>
    ///
    /// <para><b>Why.</b> A car's landing is the one square in this building that is guaranteed to be shared:
    /// the ride sets the captain down on it, <c>[E]</c> finds the panel on it, and it is where the clock has
    /// this man standing on the frame the captain arrives. So the body was dealt onto the captain's own feet
    /// — separation a third of a du — and the route planned from there put its first lattice node back on
    /// him. Every sub-step was inside the berth and pointed at him; <c>NpcWalk.Step</c> refused it and kept
    /// the route, and nothing was ever going to grow the gap.</para>
    ///
    /// <para><b>Proven RED</b> by putting the placement back on the clock's bare point: <c>he is still at the
    /// landing … 0.4 du from the captain, Waiting</c>, with the leg unchanged when the frames run out.</para>
    /// </summary>
    [Fact]
    public void FollowingHimOntoHisOwnCarDoesNotLandTheCaptainOnHisSquare()
    {
        Pages.Map map = AtTheDocumentedLink("onto-his-own-car");
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);

        Assert.True(
            RunUntil(map, () => Leg(map) == "ToHisCabin", TheWholeNightIsWatchedFor),
            $"he never reached his car on the concourse — leg {Leg(map)} (#1285).");
        Assert.True(Ride(map, HavenLevels.ServiceLevel, down), "his own car refused to go down.");

        // ── THE ANTI-VACUITY CLAUSE ───────────────────────────────────────────────────────────────────
        // The case is only about a shared square if the ride really set the captain down on the one the
        // leg starts from. Both facts asked of the room, never typed.
        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, down)!.Value;
        var from = (ValueTuple<double, double>)Read(map, "_nightLegFrom")!;
        Assert.Equal(landing.X, (double)Read(map, "_avatarX")!, 1);
        Assert.Equal(landing.Y, (double)Read(map, "_avatarY")!, 1);
        Assert.Equal(landing.X, from.Item1, 1);
        Assert.Equal(landing.Y, from.Item2, 1);

        // Stand still. He walks his own corridor, and the leaf shuts behind him inside the time the leg
        // takes a man to walk — the night's own arithmetic, with a body's worth of slack on the end of it.
        int cabin = TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins);
        DeckReachability.Point doorstep = HavenInterior.TheCabinDoorstepAt(Berth, cabin)!.Value;
        double hisCorridor = WalkSeconds(landing, doorstep) + TheTailsNight.LegSeconds(ObservationWalk.OnHisHeelsDu);

        bool heGotThere = RunUntil(map, () => Leg(map) is "Inside" or "ToTheCarUp", hisCorridor);
        Pages.Map.Walker? stuck = HimOnTheFloor(map);
        Assert.True(
            heGotThere,
            $"he never reached his own door in the {hisCorridor:0} s that corridor takes to walk: leg "
            + $"{Leg(map)}, "
            + (stuck is null
                ? "and there is nobody in the corridor at all"
                : $"he is at {stuck.Walk.X:0.0},{stuck.Walk.Y:0.0} ({stuck.Walk.State}), "
                  + $"{Math.Sqrt((((double)Read(map, "_avatarX")! - stuck.Walk.X) * ((double)Read(map, "_avatarX")! - stuck.Walk.X)) + (((double)Read(map, "_avatarY")! - stuck.Walk.Y) * ((double)Read(map, "_avatarY")! - stuck.Walk.Y))):0.0} du "
                  + "from a captain who has not moved since the doors opened")
            + " — which is the row's own broken, and the rest of the night is behind it.");
    }

    /// <summary>
    /// #1287 · <b>THE CABIN WAIT IS ONE CLOCK, WHOEVER IS WATCHING THE LEAF.</b>
    ///
    /// <para><b>What was played</b> (QA, 2026-09-21): <i>"Wait in the corridor. About three minutes at warp 1,
    /// and then the leaf opens and he walks out."</i> He never comes out — watched to +570 s on his own car
    /// and to +302 s on the WRONG one, where the captain is never within twenty du of him, so it is not a
    /// courtesy freeze. Stay on the CONCOURSE instead and the same night runs to the second.</para>
    ///
    /// <para><b>Why.</b> <c>Inside</c> is a leg on the service level, so a captain in the corridor made
    /// <c>hisFloor</c> true and took the branch that deals a BODY — which has no arm for a man who is not
    /// one. Every frame re-dealt him onto the corridor at the car's landing to walk to his own door a second
    /// time, and the leg's own clock was compared only in the branch that floor never reaches. <b>The wait
    /// was ticked by exactly the one observer who could not see it.</b></para>
    ///
    /// <para>So the law is the pair, measured: the wait is the same length of sim time from the corridor and
    /// from the concourse, and it is <see cref="TheTailsNight.CabinWaitSeconds"/> both times.</para>
    ///
    /// <para><b>Proven RED</b> on the shipped branch: the corridor's wait comes out at about a tenth of the
    /// concourse's, with a body walking the cabin corridor a second time inside it.</para>
    /// </summary>
    [Fact]
    public void TheCabinWaitIsOneClockWhoeverIsWatchingTheLeaf()
    {
        double fromTheCorridor = TheWaitBehindTheLeaf("waiting-in-the-corridor", followHimDown: true);
        double fromTheConcourse = TheWaitBehindTheLeaf("waiting-upstairs", followHimDown: false);

        Assert.True(
            Math.Abs(fromTheConcourse - TheTailsNight.CabinWaitSeconds) <= 1.0,
            $"the wait is {TheTailsNight.CabinWaitSeconds:0} s and the captain who stayed upstairs measured "
            + $"{fromTheConcourse:0.0} s of it, so this pair is not about the floor at all.");
        Assert.True(
            Math.Abs(fromTheCorridor - TheTailsNight.CabinWaitSeconds) <= 1.0,
            $"the leaf opened after {fromTheCorridor:0.0} s for a captain standing in the corridor and after "
            + $"{fromTheConcourse:0.0} s for one who stayed upstairs. The wait is "
            + $"{TheTailsNight.CabinWaitSeconds:0} s and it is ONE clock — a man behind a door does not wait "
            + "longer or shorter for who happens to be on his floor.");
    }

    /// <summary>#1287 · How long the leaf stays shut, in sim seconds, with the captain either following him
    /// down onto the service level or staying on the concourse. Everything else about the two runs is the
    /// same evening at the same link.</summary>
    private static double TheWaitBehindTheLeaf(string canvasId, bool followHimDown)
    {
        Pages.Map map = AtTheDocumentedLink(canvasId);
        int down = TheTailsNight.TheCarHeTakesDown(Berth, Person, Cars);

        Assert.True(
            RunUntil(map, () => Leg(map) == "ToHisCabin", TheWholeNightIsWatchedFor),
            $"he never reached his car on the concourse — leg {Leg(map)} (#1285).");

        if (followHimDown)
        {
            Assert.True(Ride(map, HavenLevels.ServiceLevel, down), "his own car refused to go down.");
        }

        DeckReachability.Point landing = HavenInterior.TheCageLandingAt(Berth, down)!.Value;
        int cabin = TheTailsNight.HisCabin(Berth, Person, HavenLevels.Cabins);
        DeckReachability.Point doorstep = HavenInterior.TheCabinDoorstepAt(Berth, cabin)!.Value;
        double hisCorridor = WalkSeconds(landing, doorstep) + TheTailsNight.LegSeconds(ObservationWalk.OnHisHeelsDu);

        Assert.True(
            RunUntil(map, () => Leg(map) == "Inside", hisCorridor),
            $"the leaf never shut behind him — leg {Leg(map)} (#1286).");
        Assert.Equal(
            followHimDown ? HavenLevels.ServiceLevel : HavenLevels.Concourse, Floor(map));

        double shut = (double)Read(map, "SimTime")!;

        // Nothing is in the corridor while the leaf is shut. There is no body behind a closed door, and a
        // man dealt back onto the floor in front of a captain who is standing there watching it is the bug.
        bool somebodyInTheCorridorDuringTheWait = false;
        RunUntil(
            map,
            () =>
            {
                somebodyInTheCorridorDuringTheWait |= Leg(map) == "Inside" && HimOnTheFloor(map) is not null;
                return Leg(map) != "Inside";
            },
            TheTailsNight.CabinWaitSeconds * 2);

        Assert.False(
            somebodyInTheCorridorDuringTheWait,
            "there was a body on the floor while he was behind his own shut leaf.");
        Assert.Equal("ToTheCarUp", Leg(map));
        return (double)Read(map, "SimTime")! - shut;
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

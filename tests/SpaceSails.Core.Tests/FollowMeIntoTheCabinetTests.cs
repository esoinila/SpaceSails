using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #731 v2 · FOLLOW ME INTO THE CABINET — the other kind of door, driven over the buildings that ship.
///
/// <para><b>Owner, 2026-08-06, on #751's cabinets:</b> <i>"Also it is dramatic telling when our contact wants
/// us to follow them into kabinetti :-D"</i></para>
///
/// <h3>What is being proved, and why it is not the same guard as v1's</h3>
///
/// <para>#731 v1's door law is that the leaf somebody leaves through is one the captain's own TRY is
/// <b>refused</b> at — a type (<see cref="UndergroundComplex.LockedDoor"/>), never a flag, and
/// <c>THE_DOOR_TheyLeaveThroughIsOneTheCaptainsTryRefuses</c> is what keeps it. <b>This lane's door is the
/// exact opposite and must be shown to be.</b> A cabinet's opening is a gap cut in a wall; it opens for her
/// as it opens for you; and the whole beat is that she stands in it and waits for you to come through it. A
/// guard that could not tell those two apart would pass on a build in which a contact led the captain to a
/// door the captain cannot open — which is a scene that simply ends, in silence, with the player standing in
/// a hall wondering what they did wrong.</para>
///
/// <para>So the three clauses below are, in order: the door is HELD OPEN (there is a walk from where she
/// waits to the table she is taking you to, and her leaf is on nobody's locked list); the WALK is a real
/// route over the captain's own lattice to a free booth; and the CONTACT is one the scene itself marks as
/// having something worth a room to say.</para>
/// </summary>
public sealed class FollowMeIntoTheCabinetTests
{
    /// <summary>The captain's own body, which is every walker's body: one law, one width.</summary>
    private const double Radius = 0.7;

    /// <summary>One frame, at the ceiling the shipping walker band spends — the game's own worst frame.</summary>
    private const double Frame = 0.25;

    /// <summary>How many frames a crossing gets before the guard calls it stuck. At the frame above and
    /// <see cref="NpcWalk.PaceDu"/> this is 800 du of walking, which is more than twice the width of the
    /// whole field.</summary>
    private const int FrameCeiling = 1600;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The sites the sweep walks — the named ones the rest of Core's floor guards use, plus a band
    /// of seeded moons, so the law is asked of authored geometry and of generated geometry alike.</summary>
    private static IEnumerable<string> Sweep() =>
        new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        }.Concat(Enumerable.Range(0, 12).Select(i => $"probe-moon-{i}"));

    private static IEnumerable<long> Watches() => Enumerable.Range(0, 8).Select(i => (long)i);

    /// <summary>One canteen floor, with everything the beat needs: the stone, the booths, the tops and who
    /// the shift put at them.</summary>
    private sealed record Hall(
        string Body,
        int Level,
        long Watch,
        UndergroundComplex.FloorPlan Floor,
        UndergroundComplex.Hall Room,
        IReadOnlyList<SurfaceCollision.Segment> Walls,
        IReadOnlyList<CanteenRegulars.TableSeat> Tops);

    /// <summary>Every canteen floor WITH BOOTHS in the sweep, on every watch — built by the real generator
    /// and seated by the real rota. <see cref="CanteenRegulars.PeopleSitHere"/> is asked rather than
    /// re-derived: a test with its own opinion about which room people sit in would be a second rota.</summary>
    private static IEnumerable<Hall> EveryHall()
    {
        foreach (string body in Sweep())
        {
            if (UndergroundComplex.TopPressurisedFloor(body) is not { } level)
            {
                continue;
            }

            UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
            var lines = new List<SurfaceCollision.Segment>();
            foreach (SurfaceLayout.Wall w in floor.Walls)
            {
                lines.Add(new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2));
            }
            SurfaceCollision.WallIndex walls = SurfaceCollision.WallIndex.Build(lines);

            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (!CanteenRegulars.PeopleSitHere(body, level, a) || a.Hall is not { } hall
                    || hall.Cabinets.Count == 0)
                {
                    continue;
                }
                foreach (long watch in Watches())
                {
                    yield return new Hall(
                        body, level, watch, floor, hall, walls,
                        CanteenRegulars.Tables(body, level, a, watch));
                }
            }
        }
    }

    // ── (a) THE DOOR IS HELD OPEN, AND IT IS NOT ONE SHUT IN YOUR FACE ────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>A CABINET IS A ROOM SOMEBODY LEADS YOU TO, NOT A DOOR THEY DISAPPEAR THROUGH.</b>
    ///
    /// <para>The load-bearing law of this lane, and the mirror image of v1's. Four clauses, on every booth of
    /// every canteen floor in the sweep:</para>
    ///
    /// <list type="number">
    /// <item><b>There is somewhere to wait.</b> <see cref="Escort.WhereSheWaits"/> answers, and the point it
    /// answers with is standable at the captain's own width — the same predicate the A* lattice asks, so a
    /// spot it returns is a spot a walk can reach by construction.</item>
    /// <item><b>It is on the HALL side.</b> Inside no cabinet on the floor — a body waiting in the booth next
    /// door is waiting in somebody else's meeting, and a body waiting INSIDE the booth is not holding a door
    /// open, it is simply in a room.</item>
    /// <item><b>THE DOOR OPENS FOR THE CAPTAIN.</b> There is a route, planned by
    /// <see cref="AutoWalk.Plan"/> at the captain's own radius over the floor's own stone, from where she is
    /// standing to the top she is taking you to. That is the whole difference between this beat and #731 v1's
    /// and the only way to state it that a build cannot fake.</item>
    /// <item><b>And her leaf is on nobody's locked list.</b> No <see cref="UndergroundComplex.LockedDoor"/>
    /// on this floor stands on the cabinet's own opening, and no locked plate reads as this cabinet's — so
    /// the room she is holding the door of is not one the captain's TRY would be refused at.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Deal the waiting place against the floor's LOCKED doors instead — the v1
    /// function, <c>Egress.StandingPlaceAt(floor.Locked[…])</c>, which is the shape somebody reaches for when
    /// the two beats look like one function with a flag. Clause (3) goes red on every booth: a body waiting
    /// at a poured-shut leaf has no route to any cabinet table at all. The verbatim run is in the pull
    /// request.</para>
    /// </summary>
    [Fact]
    public void THE_CABINET_IsADoorHeldOpenAndNotOneShutInYourFace()
    {
        var complaints = new List<string>();
        int booths = 0;

        foreach (Hall hall in EveryHall().Where(h => h.Watch == 0))
        {
            foreach (UndergroundComplex.Cabinet cab in hall.Room.Cabinets)
            {
                booths++;

                DeckReachability.Point? spot =
                    Escort.WhereSheWaits(in cab, hall.Room.Cabinets, Radius, hall.Walls);
                if (spot is not { } at)
                {
                    complaints.Add(Say(hall, cab,
                        "there is nowhere on this floor for anybody to stand and hold that door open."));
                    continue;
                }

                if (SurfaceCollision.Blocked(at.X, at.Y, Radius, hall.Walls))
                {
                    complaints.Add(Say(hall, cab, string.Create(CultureInfo.InvariantCulture,
                        $"she waits at ({at.X:F1},{at.Y:F1}), which is inside the building's own stone.")));
                }

                // (2a) …AND IT IS THIS BOOTH'S OWN OPENING. The clause that makes the whole guard mean
                // something: a spot somewhere out in the hall is standable, is inside no cabinet, and has a
                // route to every table in the building — so without this, handing the guard v1's own
                // locked-door function passes it. She is standing at ONE DOOR, and it is HERS.
                double gap = NearestOpening(in cab, at) ?? double.NaN;
                if (!(Math.Abs(gap - Escort.DoorStandoffDu) <= DeckReachability.DefaultStep))
                {
                    complaints.Add(Say(hall, cab, string.Create(CultureInfo.InvariantCulture,
                        $"she waits {gap:F1} du from the nearest of this booth's own openings, and a body holding a door open stands {Escort.DoorStandoffDu:F1} du from THAT door — she is not at this cabinet at all.")));
                }

                if (Escort.InsideAnyOf(hall.Room.Cabinets, at.X, at.Y))
                {
                    complaints.Add(Say(hall, cab,
                        "she waits INSIDE a booth. Holding a door open is a thing you do from the hall — a "
                        + "body already in the room is not an invitation, it is an occupant."));
                }

                // (3) THE DOOR OPENS FOR YOU. The captain's own planner, at the captain's own width.
                var table = new DeckReachability.Point(cab.Table.X, cab.Table.Y);
                AutoWalk.Attempt follow = AutoWalk.Plan(
                    true, at, table, hall.Walls, Radius, AutoWalk.BoundsFor(hall.Walls, at, table));
                if (follow.Route is null)
                {
                    complaints.Add(Say(hall, cab,
                        "there is no way from where she is standing to the table she is taking you to. She is "
                        + "not holding a door open — she is standing at one that does not open."));
                }

                // (4) …and it is not one of the leaves the captain's TRY is refused at.
                foreach (UndergroundComplex.LockedDoor locked in hall.Floor.Locked)
                {
                    if (StandsOn(locked, cab))
                    {
                        complaints.Add(Say(hall, cab,
                            $"`{locked.Sign}` is poured across this cabinet's own opening — the contact is "
                            + "leading the captain to a door that refuses them, which is #731 v1's beat "
                            + "wearing v2's name."));
                    }
                    if (string.Equals(locked.Sign, cab.Plate, StringComparison.Ordinal))
                    {
                        complaints.Add(Say(hall, cab,
                            "this cabinet's own plate is on the floor's LOCKED list."));
                    }
                }
            }
        }

        Assert.True(booths > 0, "the sweep found no booths at all — this guard asserts nothing.");
        Assert.True(complaints.Count == 0,
            $"{complaints.Count} of {booths} cabinet(s) are not doors held open:\n  "
            + string.Join("\n  ", complaints.Take(12))
            + (complaints.Count > 12 ? "\n  …" : ""));
    }

    // ── (b) THE WALK IS A REAL ROUTE TO A FREE BOOTH ─────────────────────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>SHE CROSSES THE HALL ON REAL LEGS.</b>
    ///
    /// <para>The premise of the whole lane, restated for the third errand: <i>"the walk across the hall IS
    /// the beat."</i> Every occupied top on every canteen floor in the sweep is walked to the free booth
    /// <see cref="Escort.AFreeCabinet"/> picks for it, over <see cref="NpcWalk"/>, and two things are
    /// asserted on every frame of every one of them:</para>
    ///
    /// <list type="number">
    /// <item><b>The route is the captain's lattice.</b> More than one point (a one-point route is a position
    /// handed over, not a walk), consecutive points one lattice step apart, and both ends AND the midpoint of
    /// every leg standable at the captain's own width — the no-corner-cutting rule restated where a straight
    /// line across a hall full of tables would fail it.</item>
    /// <item><b>The body is never inside stone</b>, at the captain's own predicate, and the walk ends
    /// <see cref="NpcWalk.Doing.Arrived"/> rather than grinding against something.</item>
    /// </list>
    ///
    /// <para>And the booth she picks is FREE and is a booth: <see cref="CanteenRegulars.TableSeat.Cabinet"/>
    /// above zero and <see cref="CanteenRegulars.TableSeat.Taken"/> false, off the room's own roster.</para>
    ///
    /// <para><b>The RED case.</b> The "it's only an NPC, let it pass" shortcut <see cref="NpcWalk"/>'s own
    /// docs forbid by name — <c>AutoWalk.Along([to])</c> in <c>Plan</c>. Every walk goes red on (1) with a
    /// one-point route. The verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_ESCORT_WalksARealRouteOverTheLatticeToAFreeCabinet()
    {
        var complaints = new List<string>();
        int walks = 0;

        foreach (Hall hall in EveryHall())
        {
            foreach (CanteenRegulars.TableSeat top in hall.Tops)
            {
                if (top is not { Taken: true, Cabinet: 0 } || StandingUpFrom(in top, hall.Walls) is not { } from)
                {
                    continue;
                }

                if (Escort.AFreeCabinet(hall.Tops, from.X, from.Y) is not { } booth)
                {
                    continue;
                }

                Assert.True(booth.Cabinet > 0 && !booth.Taken,
                    $"{hall.Body} B{hall.Level}: the booth she picked is top {booth.Index}, which is "
                    + $"cabinet {booth.Cabinet} and {(booth.Taken ? "occupied" : "free")}.");

                if (TheBooth(hall.Room, booth.Cabinet) is not { } cab
                    || Escort.WhereSheWaits(in cab, hall.Room.Cabinets, Radius, hall.Walls) is not { } at)
                {
                    continue;
                }

                NpcWalk? planned = NpcWalk.Plan(
                    top.Plate ?? "", new NpcWalk.Bound(cab.Plate, at.X, at.Y), from, hall.Walls,
                    Radius, SurfaceCollision.Gait.Person, NpcWalk.PaceDu, NpcWalk.NoPersonalSpace);
                if (planned is not { } walk)
                {
                    continue;   // this floor does not connect the two; a null is skipped, never passed.
                }

                walks++;

                if (walk.Route.Count <= 1)
                {
                    complaints.Add(Say(hall, top,
                        $"the route is {walk.Route.Count} point(s) long — that is a position handed over, "
                        + "not a walk."));
                    continue;
                }

                for (int i = 1; i < walk.Route.Count; i++)
                {
                    DeckReachability.Point a = walk.Route[i - 1], b = walk.Route[i];
                    double leg = Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
                    if (leg > DeckReachability.DefaultStep * 1.5)
                    {
                        complaints.Add(Say(hall, top, string.Create(CultureInfo.InvariantCulture,
                            $"leg {i} is {leg:F2} du long, which is not one lattice step — a route that strides is a route that was not planned.")));
                        break;
                    }
                    if (SurfaceCollision.Blocked((a.X + b.X) / 2, (a.Y + b.Y) / 2, Radius, hall.Walls))
                    {
                        complaints.Add(Say(hall, top, $"leg {i} cuts a corner through the stone."));
                        break;
                    }
                }

                // …and the ground has the last word: walk it, with the captain far away so nothing yields.
                int spent = 0;
                while (walk.Afoot && spent < FrameCeiling)
                {
                    walk.Step(Frame, hall.Walls, -9999, -9999);
                    spent++;
                    if (SurfaceCollision.Blocked(walk.X, walk.Y, Radius, hall.Walls))
                    {
                        complaints.Add(Say(hall, top, string.Create(CultureInfo.InvariantCulture,
                            $"she is inside the room's own stone at ({walk.X:F1},{walk.Y:F1}) on frame "
                            + $"{spent}.")));
                        break;
                    }
                }
                if (walk.State != NpcWalk.Doing.Arrived)
                {
                    complaints.Add(Say(hall, top,
                        $"she ended the crossing {walk.State} rather than at the door of cabinet "
                        + $"{cab.Number}."));
                }
            }
        }

        Assert.True(walks > 0, "the sweep walked nobody anywhere — this guard asserts nothing.");
        Assert.True(complaints.Count == 0,
            $"{complaints.Count} of {walks} escort(s) did not cross the hall on the captain's floor:\n  "
            + string.Join("\n  ", complaints.Take(12))
            + (complaints.Count > 12 ? "\n  …" : ""));
    }

    // ── (c) WHO DOES THIS, AND IT COMES OFF THE SCENE'S OWN STATE ────────────────────────────────────

    /// <summary>
    /// #731 v2 · <b>NOBODY WALKS YOU ANYWHERE TO SAY NOTHING.</b>
    ///
    /// <para>The first clause of <see cref="Escort.LeadsYouIn"/> is not a roll and must not be reachable by
    /// one: a scene with nothing in it worth a private room can never produce this beat, however the dice
    /// fall, because there would be nothing to take the captain anywhere FOR. So the deal move is read off
    /// the content files that ship, and then the refusal is swept across every seed the room has.</para>
    ///
    /// <list type="number">
    /// <item>The haulier's scene (#757) has one, and it is <c>hear-them-out</c> — the ask about her brother,
    /// which is the one rung of that ladder the field book keeps.</item>
    /// <item>The Hand's table (#746) has one, and it is <c>work</c> — the one ROLLED move at a canteen table,
    /// which is the owner's own "the Hand's papers".</item>
    /// <item>A table you took alone, a stranger with a cup, and a booked room nobody came to have NONE — and
    /// <see cref="Escort.LeadsYouIn"/> is false for them on every site, floor and watch in the sweep.</item>
    /// <item>…and it is not vacuously false: the haulier's scene DOES produce the beat on some of them, and
    /// the answer is the same both times it is asked (a coin, never a clock).</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Drop the deal-move clause — <c>return DiceRule.Roll(…).Face == 1;</c> — and
    /// row (3) goes red with the empty booked room walking a captain into a cabinet to say nothing. The
    /// verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_DEAL_MoveIsTheOneTheSceneItselfMarksAndNobodyElseLeadsYouAnywhere()
    {
        Encounter.Move? hers = Escort.TheDealMoveIn(SittingAlone.TheVisitor());
        Assert.True(hers is not null,
            "#757's haulier crossed a hall to say something the field book keeps, and this cannot find it.");
        Encounter.Move haulier = hers!.Value;
        Assert.Equal(SittingAlone.HearThemOut, haulier.Id);
        Assert.Equal(SittingAlone.TheAskNote, haulier.Note);

        Encounter.Move? his = Escort.TheDealMoveIn(CanteenTable.SceneFor(CanteenTable.Who.Hand));
        Assert.True(his is not null, "#746's Hand has the one rolled move at a canteen table, and this "
            + "cannot find it.");
        Encounter.Move hand = his!.Value;
        Assert.Equal(CanteenTable.Work, hand.Id);
        Assert.True(hand.Rolled, "#746's ask about work is the one rolled move at a canteen table.");

        (string What, Encounter.Scene Scene)[] nothingToSay =
        [
            ("a table you took alone", SittingAlone.TheTable()),
            ("a stranger with a cup", CanteenTable.StrangerScene("◈ SOMEBODY")),
            ("a booked room nobody came to", RoomBooking.TheEmptyRoom()),
            ("the delegation's gesture", RoomBooking.TheDelegationTable("◈ THREE OF THEM")),
        ];
        foreach ((string what, Encounter.Scene scene) in nothingToSay)
        {
            Assert.True(Escort.TheDealMoveIn(scene) is null,
                $"{what} has a deal move in it, which is not what that scene is.");
        }

        var led = new List<string>();
        int asked = 0, some = 0;
        foreach (Hall hall in EveryHall())
        {
            foreach (CanteenRegulars.TableSeat top in hall.Tops)
            {
                asked++;
                foreach ((string what, Encounter.Scene scene) in nothingToSay)
                {
                    if (Escort.LeadsYouIn(
                            hall.Body, hall.Level, hall.Watch, top.Index, SittingAlone.VisitorPlate, scene))
                    {
                        led.Add($"{hall.Body} B{hall.Level} watch {hall.Watch} top {top.Index}: {what} "
                            + "walked the captain into a cabinet to say nothing at all.");
                    }
                }

                bool once = Escort.LeadsYouIn(
                    hall.Body, hall.Level, hall.Watch, top.Index, SittingAlone.VisitorPlate,
                    SittingAlone.TheVisitor());
                Assert.Equal(once, Escort.LeadsYouIn(
                    hall.Body, hall.Level, hall.Watch, top.Index, SittingAlone.VisitorPlate,
                    SittingAlone.TheVisitor()));
                if (once)
                {
                    some++;
                }
            }
        }

        Assert.True(asked > 0, "the sweep asked nobody — this guard asserts nothing.");
        Assert.True(led.Count == 0,
            $"{led.Count} scene(s) with nothing to say led the captain out of the hall:\n  "
            + string.Join("\n  ", led.Take(8)) + (led.Count > 8 ? "\n  …" : ""));
        Assert.True(some > 0,
            "not one seat on one watch of one station produced the beat, so the row above is green because "
            + "nothing ever happens — a guard that asserts nothing.");
        Assert.True(some < asked,
            $"every one of {asked} seats produced it. A contact who ALWAYS takes you into a booth is a "
            + "corridor with a cutscene in it, which is the thing the seeded coin exists to refuse.");
    }

    // ── (d) SHE WAITS A WATCH, AND THE WATCH IS THE SHIFT'S ─────────────────────────────────────────

    /// <summary>#731 v2 · How long she stands there is a fraction of the ROTA'S OWN SHIFT and never a number
    /// somebody typed — a patience written as seconds would stop meaning "a quarter of a watch" the day a
    /// watch changes length, and the room forgetting (which ends the wait regardless) is keyed to that same
    /// shift. Also: it is long enough to walk the longest hall several times over, which is what makes the
    /// beat a grace rather than a timer.</summary>
    [Fact]
    public void THE_PATIENCE_IsAQuarterOfTheRotasOwnShift()
    {
        Assert.Equal(PatronRota.WatchSeconds * Escort.PatienceFraction, Escort.PatienceSeconds, 6);
        Assert.InRange(Escort.PatienceFraction, 0.05, 0.9);
        Assert.True(Escort.PatienceSeconds * NpcWalk.PaceDu > 1000,
            "she gives up before a captain could cross the field at a walker's pace, which is a timer rather "
            + "than a grace.");
        Assert.Equal(Egress.DoorStandoffDu, Escort.DoorStandoffDu);
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where a body standing up from a top actually stands — the client's own chair ring, never a
    /// second geometry.</summary>
    private static DeckReachability.Point? StandingUpFrom(
        in CanteenRegulars.TableSeat top, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        for (int c = 0; c < top.Seats; c++)
        {
            (double x, double y) = top.Chair(c);
            if (!SurfaceCollision.Blocked(x, y, Radius, walls))
            {
                return new DeckReachability.Point(x, y);
            }
        }
        return null;
    }

    /// <summary>How far this point is from the nearest of a booth's own openings, or null when it has
    /// none. Measured to the leaf's MIDPOINT, which is where a standoff is measured from.</summary>
    private static double? NearestOpening(in UndergroundComplex.Cabinet cab, DeckReachability.Point at)
    {
        double? best = null;
        foreach (SurfaceLayout.Doorway way in cab.Ways)
        {
            double mx = (way.X1 + way.X2) / 2, my = (way.Y1 + way.Y2) / 2;
            double d = Math.Sqrt(((at.X - mx) * (at.X - mx)) + ((at.Y - my) * (at.Y - my)));
            if (best is null || d < best)
            {
                best = d;
            }
        }
        return best;
    }

    private static UndergroundComplex.Cabinet? TheBooth(UndergroundComplex.Hall hall, int number)
    {
        foreach (UndergroundComplex.Cabinet c in hall.Cabinets)
        {
            if (c.Number == number)
            {
                return c;
            }
        }
        return null;
    }

    /// <summary>Does this locked leaf stand on one of the cabinet's own openings? Compared by geometry —
    /// midpoint within a lattice step — because a plate is a name and a wall is a place.</summary>
    private static bool StandsOn(in UndergroundComplex.LockedDoor door, in UndergroundComplex.Cabinet cab)
    {
        double dx = (door.X1 + door.X2) / 2, dy = (door.Y1 + door.Y2) / 2;
        foreach (SurfaceLayout.Doorway way in cab.Ways)
        {
            double wx = (way.X1 + way.X2) / 2, wy = (way.Y1 + way.Y2) / 2;
            if (Math.Abs(dx - wx) < DeckReachability.DefaultStep
                && Math.Abs(dy - wy) < DeckReachability.DefaultStep)
            {
                return true;
            }
        }
        return false;
    }

    private static string Say(Hall hall, in UndergroundComplex.Cabinet cab, string what) =>
        $"{hall.Body} B{hall.Level}, cabinet {cab.Number}: {what}";

    private static string Say(Hall hall, in CanteenRegulars.TableSeat top, string what) =>
        $"{hall.Body} B{hall.Level} watch {hall.Watch}, `{top.Plate}` off top {top.Index}: {what}";
}

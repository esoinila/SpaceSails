using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #731 · THE EXIT IS THE FULL STOP — the laws of the walker, asked of the floors that ship.
///
/// <para><b>Owner, 2026-08-06, streamed during the smoke run:</b> <i>"The NPCs but not reevers could also use
/// the A* if we want to show them leaving a scene etc. If they go behind a door that is locked to us, we use
/// that as 'I guess that concludes the conversation' point in the plot / situation."</i> And the limitation
/// it fixes, in the same breath: <i>"Like on the bar now they have to wait for us to leave before they can sit
/// up… or leave the bar."</i></para>
///
/// <para><b>And the other direction, the same day:</b> <i>"In the space bars there are lot of cases where we
/// can have npcs arrive at bar from locked place or go to a locked place. Now it is possible to have NPC ask
/// to sit down at our table and offer a quest! This is the classic TTRPG event."</i></para>
///
/// <h3>Why these are driven and not read</h3>
///
/// <para>Every guard below builds REAL floors with the shipping generator, seats them with the shipping rota,
/// deals the shift's own departures with <see cref="Egress"/>, and then WALKS them — frame by frame, through
/// <see cref="NpcWalk.Step"/>, over the floor's own stone. Nothing here types in a wall, a door or a
/// schedule. A guard that read a field saying "this door is locked" would be this repo's fifth named bug
/// class wearing the feature that caused it: the assertion would be right and the world could not tell pass
/// from fail.</para>
///
/// <para>The client's own collision field carries the furniture as well as the walls; a Core test may not
/// reference <c>DeckPlan</c>, so these walk the floor's STONE. That is the strictly weaker world — a route
/// legal here could still be refused by a chair — which is why the client's own guards
/// (<c>TheExitIsTheFullStopTests</c> over there) drive the shipping deck as well.</para>
/// </summary>
public sealed class TheExitIsTheFullStopTests
{
    /// <summary>The captain's own width. <c>DeckPlan.AvatarRadius</c> lives in the client and a Core test may
    /// not reference it, so it is written here for the same reason
    /// <c>NoRoomHasOnlyOneWayOutTests.Radius</c> is: it is the number the live movement collides on, and a
    /// sweep run at a smaller one would pass a gap the captain cannot fit through.</summary>
    private const double Radius = 0.7;

    /// <summary>One frame, in seconds. A quarter — a long frame, deliberately, because the sweep walks three
    /// hundred people across a building 310 du wide and a 20 Hz sample of that is millions of steps.
    ///
    /// <para>It is still far finer than the law needs. At <see cref="NpcWalk.PaceDu"/> a frame is half a deck
    /// unit, spent in up to <see cref="NpcWalk.SubStepsPerFrame"/> sub-steps of at most
    /// <c>AutoWalk.MaxSubStepDu</c> each — so the body moves at most 0.2 du between two collision queries and
    /// cannot step OVER a wall it is 0.7 du wide against.</para></summary>
    private const double Frame = 0.25;

    /// <summary>How many frames a walk is given before the guard calls it stuck. At the frame above this is
    /// 800 du of walking — nearly three times the width of the whole field — so a walk that hits it is a walk
    /// that is going round in circles rather than one that had a long way to go.</summary>
    private const int FrameCeiling = 1600;

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    /// <summary>The sites the sweep walks. The named ones the rest of Core's floor guards use, plus a band of
    /// seeded moons, so the law is asked of authored geometry and of generated geometry alike.</summary>
    private static IEnumerable<string> Sweep() =>
        new[]
        {
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        }.Concat(Enumerable.Range(0, 12).Select(i => $"probe-moon-{i}"));

    /// <summary>Which watches the sweep asks about. Eight shifts is a day and a third of this station's
    /// time — enough that the seeded third who leave are a different third several times over.</summary>
    private static IEnumerable<long> Watches() => Enumerable.Range(0, 8).Select(i => (long)i);

    // ── THE ROOM, AS IT SHIPS ─────────────────────────────────────────────────────────────────────────

    /// <summary>One canteen floor, with everything a walk needs: the stone, the doors that do not open, the
    /// tops and who the shift put at them.</summary>
    private sealed record Hall(
        string Body,
        int Level,
        long Watch,
        UndergroundComplex.FloorPlan Floor,
        IReadOnlyList<SurfaceCollision.Segment> Walls,
        IReadOnlyList<CanteenRegulars.TableSeat> Tops);

    /// <summary>Every canteen floor in the sweep, on every watch — built by the real generator, seated by the
    /// real rota. <see cref="CanteenRegulars.PeopleSitHere"/> is ASKED rather than re-derived: a test with its
    /// own opinion about which room people sit in would be a second rota.</summary>
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
            // #858's index, for the same reason the shipping deck holds one: a walk is thousands of
            // collision queries and a bare list makes every one of them a walk over every wall in the
            // building. It is the same stone either way — the index answers the identical predicate.
            SurfaceCollision.WallIndex walls = SurfaceCollision.WallIndex.Build(lines);

            foreach (UndergroundComplex.Amenity a in floor.Amenities)
            {
                if (!CanteenRegulars.PeopleSitHere(body, level, a))
                {
                    continue;
                }
                foreach (long watch in Watches())
                {
                    yield return new Hall(
                        body, level, watch, floor, walls,
                        CanteenRegulars.Tables(body, level, a, watch));
                }
            }
        }
    }

    /// <summary>Where a body standing up from a top actually stands — the client's own
    /// <c>WhereABodyStandsAt</c>, which is the top's own chair ring and never a second geometry.</summary>
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

    /// <summary>The walk one dealt move actually becomes, or null when this floor could not offer it — no
    /// chair to stand up from, nowhere to stand at the door, or no way through. A null is a case the sweep
    /// skips and never a case it passes.</summary>
    private static NpcWalk? TheWalk(Hall hall, Egress.Move move, out UndergroundComplex.LockedDoor door)
    {
        door = default;
        CanteenRegulars.TableSeat top = default;
        bool found = false;
        foreach (CanteenRegulars.TableSeat t in hall.Tops)
        {
            if (t.Index == move.TableIndex)
            {
                (top, found) = (t, true);
                break;
            }
        }
        if (!found || move.Door < 0 || move.Door >= hall.Floor.Locked.Count)
        {
            return null;
        }

        if (StandingUpFrom(in top, hall.Walls) is not { } from)
        {
            return null;
        }

        door = hall.Floor.Locked[move.Door];
        if (Egress.StandingPlaceAt(in door, Radius, hall.Walls, from.X, from.Y) is not { } at)
        {
            return null;
        }

        return NpcWalk.Plan(
            move.Plate, new NpcWalk.Bound(door.Sign, at.X, at.Y), from, hall.Walls,
            Radius, SurfaceCollision.Gait.Person);
    }

    // ── (a) THE WALKER PLANS OVER THE CAPTAIN'S LATTICE, AND NEVER CLIPS A WALL ───────────────────────

    /// <summary>
    /// #731 · A PERSON CROSSING A ROOM THE CAPTAIN IS STANDING IN IS WALKING ON THE FLOOR THE CAPTAIN IS
    /// STANDING ON.
    ///
    /// <para>The whole premise of the lane. <see cref="NpcWalk"/>'s own docs put it plainly — <i>"if it did,
    /// this whole class would be a cartoon"</i> — and this is the sentence made checkable. Every departure
    /// the shift deals on every canteen floor in the sweep is planned and then WALKED to its end, and two
    /// things are asserted on every single frame of every single one of them:</para>
    ///
    /// <list type="number">
    /// <item><b>The route is the captain's lattice.</b> Consecutive route points are one lattice step apart,
    /// both ends of every leg are standable at the captain's own radius, and so is the middle of it — which
    /// is the no-corner-cutting rule <see cref="DeckReachability"/> plans under, restated where a straight
    /// line to the door would fail it.</item>
    /// <item><b>The body is never inside stone.</b> <see cref="SurfaceCollision.Blocked"/> — the captain's own
    /// predicate, at the captain's own width — is false at the walker's position on every frame, and the walk
    /// ends <see cref="NpcWalk.Doing.Arrived"/> rather than grinding.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Give the walker the straight line and let it spend the frame without the
    /// stepper — <c>AutoWalk.Along([to])</c> in <see cref="NpcWalk.Plan"/> and <c>(X + dx, Y + dy)</c> in
    /// place of <see cref="SurfaceCollision.Slide"/>, which is the "it's only an NPC, let it pass" shortcut
    /// the class docs forbid by name. All 312 walks go red on (1). The verbatim run is in the pull
    /// request.</para>
    ///
    /// <para><b>And what the green does NOT say, written down so nobody believes more than it does.</b>
    /// Taking the stepper out ON ITS OWN leaves this guard green, and that was checked rather than assumed.
    /// The reason is the lattice: <see cref="DeckReachability"/> plans under a no-corner-cutting rule, so a
    /// body that follows an A* route exactly never enters stone whether or not anything is enforcing it.
    /// Clause (2) is therefore a BACKSTOP — it fires when the plan and the body PART, which is what a route
    /// that is not the lattice's does, and what a future mover pushing a walker off its own path would do.
    /// Clause (1) is what carries this guard, and it is the one the RED above reddens.</para>
    /// </summary>
    [Fact]
    public void THE_WALKER_PlansOverTheCaptainsLatticeAndNeverClipsAWall()
    {
        var wrong = new List<string>();
        int walked = 0, frames = 0;

        foreach (Hall hall in EveryHall())
        {
            foreach (Egress.Move move in
                Egress.Departures(hall.Body, hall.Level, hall.Watch, hall.Tops, hall.Floor.Locked))
            {
                if (TheWalk(hall, move, out _) is not { } walk)
                {
                    continue;
                }
                walked++;

                // (1) THE ROUTE IS THE LATTICE'S. Asked of the plan before a single frame is spent.
                IReadOnlyList<DeckReachability.Point> route = walk.Route;
                if (route.Count < 2)
                {
                    wrong.Add(Say(hall, move,
                        $"the route is {route.Count} point(s) long — that is a position handed over, not a "
                        + "walk."));
                    continue;
                }
                for (int i = 1; i < route.Count; i++)
                {
                    DeckReachability.Point a = route[i - 1], b = route[i];
                    double leg = Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
                    if (leg > DeckReachability.DefaultStep * 1.5)
                    {
                        wrong.Add(Say(hall, move,
                            $"leg {i} of the route is {leg:F2} du long, which is further than one lattice "
                            + $"step ({DeckReachability.DefaultStep:F2}) — this is not an A* route."));
                        break;
                    }
                    if (SurfaceCollision.Blocked(b.X, b.Y, Radius, hall.Walls)
                        || SurfaceCollision.Blocked((a.X + b.X) / 2, (a.Y + b.Y) / 2, Radius, hall.Walls))
                    {
                        wrong.Add(Say(hall, move,
                            $"leg {i} of the route passes through stone at the captain's own width — the "
                            + "plan cut a corner the captain's own walk may not."));
                        break;
                    }
                }

                // (2) AND THE BODY. Every frame, at the captain's own predicate and the captain's own width.
                int spent = 0;
                while (walk.Afoot && spent < FrameCeiling)
                {
                    // The captain is nowhere near: this guard is about STONE, and the yield has its own.
                    walk.Step(Frame, hall.Walls, double.MaxValue / 4, double.MaxValue / 4);
                    spent++;
                    frames++;
                    if (SurfaceCollision.Blocked(walk.X, walk.Y, Radius, hall.Walls))
                    {
                        wrong.Add(Say(hall, move,
                            "the body is INSIDE STONE at "
                            + string.Create(CultureInfo.InvariantCulture,
                                $"({walk.X:F2},{walk.Y:F2}) on frame {spent}")
                            + " — it clipped a wall the captain would have been stopped by."));
                        break;
                    }
                }

                if (walk.State != NpcWalk.Doing.Arrived && spent < FrameCeiling)
                {
                    wrong.Add(Say(hall, move,
                        $"the walk ended {walk.State} rather than at the door it was planned to — the "
                        + "planner and the ground disagree about this floor."));
                }
                else if (spent >= FrameCeiling)
                {
                    wrong.Add(Say(hall, move,
                        $"the walk was still going after {FrameCeiling} frames — a body grinding forever is "
                        + "a bug wearing a feature's clothes."));
                }
            }
        }

        Assert.True(walked >= 40,
            $"only {walked} departure(s) could be walked across every canteen floor of every site on eight "
            + "watches. This guard would be asserting about a room the shift never empties, which is a green "
            + "number never asked of the world.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {walked} walk(s) ({frames} frames) did not stay on the captain's floor:"
            + Environment.NewLine + string.Join(Environment.NewLine, wrong.Take(20)));
    }

    // ── (b) THE DOOR IS ONE THE CAPTAIN'S OWN TRY IS REFUSED AT ──────────────────────────────────────

    /// <summary>
    /// #731 · <i>"If they go behind a door that is locked to us, we use that as 'I guess that concludes the
    /// conversation' point."</i> — so the door had better be locked to us.
    ///
    /// <para>This is the load-bearing law of the lane and the one most easily faked. The claim is NOT read off
    /// a flag on the walker: the walker's <see cref="NpcWalk.Bound.Sign"/> is matched back to the building's
    /// own <c>Locked</c> list by GEOMETRY, and then the captain's own offer is made at it — every kind in the
    /// satchel, through <see cref="SatchelTry.Offer"/>, at the target the client's own [E] press uses
    /// (<see cref="Map"/>'s <c>HiveSignInteract</c> picks <c>SealedWay</c> for a sealed way and
    /// <c>RoomDoor</c> for everything else, and this asks the same question the same way). Every one of them
    /// must come back <c>Worked: false</c>.</para>
    ///
    /// <para>And the other half, which is what makes it a law rather than a tautology: the leaf the walker is
    /// bound for is NOT one of the floor's public doorways. A generator that started hanging locked signs on
    /// open mouths would pass the refusal check and break the beat.</para>
    ///
    /// <para><b>The RED case.</b> Deal the departures against <c>floor.Doorways</c> instead of
    /// <c>floor.Locked</c> — walk them out a PUBLIC door, which is what the beat must never be. The verbatim
    /// run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_DOOR_TheyLeaveThroughIsOneTheCaptainsTryRefuses()
    {
        var wrong = new List<string>();
        int checkedDoors = 0;

        foreach (Hall hall in EveryHall())
        {
            foreach (Egress.Move move in
                Egress.Departures(hall.Body, hall.Level, hall.Watch, hall.Tops, hall.Floor.Locked))
            {
                if (TheWalk(hall, move, out UndergroundComplex.LockedDoor door) is not { } walk)
                {
                    continue;
                }
                checkedDoors++;

                if (!walk.For.IsADoor || !string.Equals(walk.For.Sign, door.Sign, StringComparison.Ordinal))
                {
                    wrong.Add(Say(hall, move,
                        $"the walk is bound for `{walk.For.Sign}` and the door dealt was `{door.Sign}` — the "
                        + "figure is going somewhere the shift did not send it."));
                    continue;
                }

                // The building's own list, matched by GEOMETRY. A sign is a name; this is the leaf.
                if (!hall.Floor.Locked.Any(l => Same(l, door)))
                {
                    wrong.Add(Say(hall, move,
                        $"the leaf at ({door.X1:F1},{door.Y1:F1})–({door.X2:F1},{door.Y2:F1}) is not on this "
                        + "floor's locked list at all."));
                }

                // …and it is not a mouth anybody may simply walk through.
                if (hall.Floor.Doorways.Any(w => Overlaps(w, door)))
                {
                    wrong.Add(Say(hall, move,
                        $"`{door.Sign}` stands on one of this floor's PUBLIC doorways — the captain could "
                        + "follow them through it, and the full stop is a comma."));
                }

                // THE CAPTAIN'S OWN OFFER, at the target the client's own press uses.
                SatchelTry.Target target = UndergroundComplex.IsSealedWay(door.Sign)
                    ? SatchelTry.Target.SealedWay
                    : SatchelTry.Target.RoomDoor;
                foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
                {
                    SatchelTry.Outcome tried = SatchelTry.Offer(
                        new Satchel.Item(kind, "test-item"), target, hall.Body);
                    if (tried.Worked)
                    {
                        wrong.Add(Say(hall, move,
                            $"the captain's own {kind} OPENS `{door.Sign}` — the door somebody left through "
                            + "is a door the captain may follow them through, and the whole beat is gone."));
                    }
                }
            }
        }

        Assert.True(checkedDoors >= 40,
            $"only {checkedDoors} door(s) were reached by a dealt departure across the whole sweep — this "
            + "guard would be a green number never asked of the world.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {checkedDoors} departure(s) do not leave through a door that is shut to the "
            + "captain:" + Environment.NewLine + string.Join(Environment.NewLine, wrong.Take(20)));
    }

    // ── (c) STANDING IN THE DOORWAY IS CONTENT, NOT A CLIP ───────────────────────────────────────────

    /// <summary>
    /// #731 · <b>The captain blocks the door and the person waits, and looks at you.</b>
    ///
    /// <para>The issue's own proposal, ruled: <i>"they stop, wait, and look at you, and that IS content; they
    /// never clip through."</i> Three facts, each able to fail on its own, driven over every departure the
    /// sweep can deal:</para>
    ///
    /// <list type="number">
    /// <item><b>Never inside you.</b> On no frame does the body come within one body-width of the captain —
    /// <see cref="NpcWalk.PersonalSpaceInRadii"/>, which is the distance at which two people of this radius
    /// are touching.</item>
    /// <item><b>It stops, and it says so.</b> With the captain parked on the standing place at the door, the
    /// walk ends its frames <see cref="NpcWalk.Doing.Waiting"/> — not Arrived (it never got there) and not
    /// Snagged (the ground did not refuse it; a person did).</item>
    /// <item><b>And it finishes when you move.</b> The route is still live: step the captain away and the same
    /// walker completes the same walk. A yield that dropped the route would be a body that gives up on the
    /// door because you walked past it.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Take the pre-step yield out of <see cref="NpcWalk.Step"/> — let the body
    /// take the move and check the captain afterwards — and (1) and (2) both go red with the figure standing
    /// inside the captain. The verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_BLOCKED_DoorwayIsABodyThatWaitsAndNeverOneThatClips()
    {
        var wrong = new List<string>();
        int blocked = 0, resumed = 0;

        foreach (Hall hall in EveryHall())
        {
            foreach (Egress.Move move in
                Egress.Departures(hall.Body, hall.Level, hall.Watch, hall.Tops, hall.Floor.Locked))
            {
                if (TheWalk(hall, move, out _) is not { } walk)
                {
                    continue;
                }

                // The captain, standing exactly where the walker is trying to get to.
                double capX = walk.For.X, capY = walk.For.Y;
                double keepOut = Radius * NpcWalk.PersonalSpaceInRadii;
                bool touched = false;
                int spent = 0;
                while (walk.Afoot && walk.State != NpcWalk.Doing.Waiting && spent < FrameCeiling)
                {
                    walk.Step(Frame, hall.Walls, capX, capY);
                    spent++;
                    double dx = walk.X - capX, dy = walk.Y - capY;
                    if ((dx * dx) + (dy * dy) < keepOut * keepOut)
                    {
                        touched = true;
                        wrong.Add(Say(hall, move,
                            "the body is "
                            + string.Create(CultureInfo.InvariantCulture,
                                $"{Math.Sqrt((dx * dx) + (dy * dy)):F2} du from the captain on frame {spent}")
                            + string.Create(CultureInfo.InvariantCulture,
                                $", closer than the {keepOut:F2} du at which two people of this width are ")
                            + "already touching. It walked through you."));
                        break;
                    }
                }
                if (touched)
                {
                    continue;
                }

                if (walk.State == NpcWalk.Doing.Arrived)
                {
                    // The captain was standing ON the goal and the walker reached it anyway.
                    wrong.Add(Say(hall, move,
                        "the walk ARRIVED at a door the captain is standing in. Somebody went through "
                        + "somebody."));
                    continue;
                }
                if (walk.State != NpcWalk.Doing.Waiting)
                {
                    // Snagged before it ever met the captain — a floor fact, not a yield fact. Not this
                    // guard's case, and never counted as one.
                    continue;
                }
                blocked++;

                // …AND THE ROUTE IS STILL LIVE. Step out of the doorway; they finish.
                spent = 0;
                while (walk.Afoot && spent < FrameCeiling)
                {
                    walk.Step(Frame, hall.Walls, double.MaxValue / 4, double.MaxValue / 4);
                    spent++;
                }
                if (walk.State == NpcWalk.Doing.Arrived)
                {
                    resumed++;
                }
                else
                {
                    wrong.Add(Say(hall, move,
                        $"the captain stepped out of the doorway and the walk ended {walk.State} instead of "
                        + "finishing — the yield threw the route away, so a person waiting for you to move "
                        + "gives up on the door the moment you do."));
                }
            }
        }

        // THE FINDING FIRST, and the anti-vacuity count after it. The other way round, a build that clips
        // through every captain reports "nobody was ever blocked" — which is true, and is the least useful
        // sentence available about it.
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} blocked doorway(s) were a clip rather than a beat:"
            + Environment.NewLine + string.Join(Environment.NewLine, wrong.Take(20)));
        Assert.True(blocked >= 20,
            $"only {blocked} departure(s) were ever actually blocked by a captain standing on their door — "
            + "this guard would be a green number never asked of the world.");
        Assert.Equal(blocked, resumed);
    }

    // ── (d) THE SHIFT DECIDES, AND NOTHING ELSE DOES ─────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>SCHEDULED MEANS A FUNCTION OF THE WATCH.</b>
    ///
    /// <para>The issue asks the question and proposes the answer: <i>scheduled for ambience, triggered for
    /// plot beats; both through one walker.</i> Scheduled here means a pure function of (site, floor, watch)
    /// and NEVER of a wall clock — the frozen-watch law (#709), because the room is drawn at one instant and
    /// the walk is stepped at another, and a schedule that read the clock twice would put a figure on screen
    /// and answer questions about somebody else.</para>
    ///
    /// <para>The law is a PINNED DIGEST of the whole sweep: every canteen floor of every site on eight
    /// watches, every dealt move — plate, top, moment and door — written to a deterministic text and hashed.
    /// A pinned number is what makes this able to fail at all: a schedule that read
    /// <c>DateTime.UtcNow</c>, <c>Environment.TickCount</c> or an unseeded <c>Random</c> would produce a
    /// different digest on the next run, on the next machine, or on CI.</para>
    ///
    /// <para>Two cheaper facts ride along, because a digest alone cannot say the schedule is not simply
    /// EMPTY: the sweep must deal a real number of moves, every moment must land inside the first
    /// <see cref="Egress.LastCallFraction"/> of the shift, and at least two different watches must produce
    /// different schedules — the shift has to actually turn over.</para>
    ///
    /// <para><b>The RED case.</b> Salt <see cref="Egress.Departures"/>' seed with
    /// <c>DateTime.UtcNow.Ticks</c> — one wall clock, exactly the way this bug always arrives — and the
    /// digest is wrong on the very next run. The verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void THE_SCHEDULE_IsTheShiftsAndNotTheClocks()
    {
        var text = new StringBuilder();
        var perWatch = new Dictionary<long, StringBuilder>();
        int dealt = 0;

        foreach (Hall hall in EveryHall())
        {
            IReadOnlyList<Egress.Move> moves =
                Egress.Departures(hall.Body, hall.Level, hall.Watch, hall.Tops, hall.Floor.Locked);

            // Asked TWICE, in one breath: a schedule that moved between two reads of the same shift would be
            // the frozen-watch law broken in the smallest possible way.
            IReadOnlyList<Egress.Move> again =
                Egress.Departures(hall.Body, hall.Level, hall.Watch, hall.Tops, hall.Floor.Locked);
            Assert.Equal(moves, again);

            if (!perWatch.TryGetValue(hall.Watch, out StringBuilder? row))
            {
                perWatch[hall.Watch] = row = new StringBuilder();
            }

            foreach (Egress.Move m in moves)
            {
                dealt++;
                Assert.True(m.AtSecondsIntoWatch >= 0
                    && m.AtSecondsIntoWatch < Egress.LastCallFraction * PatronRota.WatchSeconds,
                    $"{hall.Body} B{-hall.Level} watch {hall.Watch}: `{m.Plate}` is scheduled to leave "
                    + $"{m.AtSecondsIntoWatch:F1}s into a shift whose last call is at "
                    + $"{Egress.LastCallFraction * PatronRota.WatchSeconds:F1}s — the room would rebuild "
                    + "under a body halfway across it.");

                string line = string.Create(CultureInfo.InvariantCulture,
                    $"{hall.Body}|B{-hall.Level}|w{hall.Watch}|{m.Plate}|t{m.TableIndex}|"
                    + $"{m.AtSecondsIntoWatch:F6}|d{m.Door}");
                text.Append(line).Append('\n');
                row.Append(hall.Body).Append('|').Append(m.Plate).Append('|').Append(m.TableIndex)
                   .Append('\n');
            }
        }

        Assert.True(dealt >= 40,
            $"the whole sweep deals {dealt} departure(s) — nobody leaves these rooms, and this digest would "
            + "be a hash of nothing.");

        // The shift turns over: two watches of the same rooms are not the same evening.
        Assert.True(perWatch.Values.Select(v => v.ToString()).Distinct().Count() > 1,
            "every watch in the sweep deals exactly the same departures — the schedule is not a function of "
            + "the shift at all, and the room has one evening it repeats forever.");

        string digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToUpperInvariant();
        Assert.True(string.Equals(PinnedSchedule, digest, StringComparison.Ordinal),
            $"the shift's schedule has moved.{Environment.NewLine}"
            + $"  pinned: {PinnedSchedule}{Environment.NewLine}"
            + $"  now:    {digest}{Environment.NewLine}"
            + $"  ({dealt} departures over {Watches().Count()} watches of every canteen floor in the sweep)"
            + Environment.NewLine + Environment.NewLine
            + "If a lane MEANT to change who leaves when — a new site, a different LeaversPerWatch, a "
            + "re-seeded rota — re-pin it here in that lane's own commit and say so in the PR body. If "
            + "nothing was supposed to move, something in this path is reading a clock.");
    }

    /// <summary>The whole sweep's schedule, hashed. Captured on the code this lane ships and committed with
    /// it; git says which commit this number came from. See the guard above for why a PIN is the only shape
    /// this law can take.</summary>
    private const string PinnedSchedule =
        "E6B7FA68FE09EBF0BF81302C4C4BA520DC0FE11AAA183DAED1883C84AB7D4CE6";

    // ── (f) NEVER REEVERS ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #731 · <b>THE OLD ONES KEEP THEIR STAGGER.</b>
    ///
    /// <para>Owner, twice: <i>"let's not make them walk too sensibly… they have their own reever-mind-issues,
    /// the kind of way they walk is nice and spooky now"</i>, and on #724 <i>"Lets not help reevers move in
    /// any easier if possible."</i> An Old One that could find a doorway would stop being the thing you shed
    /// on an obstacle when you are out of rounds.</para>
    ///
    /// <para>So the refusal is asserted where it is WRITTEN — in the constructor — rather than by reading the
    /// callers. Every gait the collision knows is handed to <see cref="NpcWalk.Plan"/>: the person's is
    /// planned, and every other one throws. The day somebody adds a third gait, this guard fails until they
    /// have decided whether it walks.</para>
    ///
    /// <para><b>The RED case.</b> Drop the gait check out of <see cref="NpcWalk.Plan"/> and the stagger is
    /// planned a route like anybody else. The verbatim run is in the pull request.</para>
    /// </summary>
    [Fact]
    public void NEVER_REEVERS_TheWalkerRefusesEveryGaitButAPersons()
    {
        // A room with a floor in it, so a refusal cannot be mistaken for "there was no way through".
        IReadOnlyList<SurfaceCollision.Segment> walls =
        [
            new(-20, -20, 20, -20), new(20, -20, 20, 20),
            new(20, 20, -20, 20), new(-20, 20, -20, -20),
        ];
        var from = new DeckReachability.Point(-10, 0);
        var to = new NpcWalk.Bound("LONG STORAGE", 10, 0);

        Assert.NotNull(NpcWalk.Plan("A REGULAR", to, from, walls, Radius, SurfaceCollision.Gait.Person));

        foreach (SurfaceCollision.Gait gait in Enum.GetValues<SurfaceCollision.Gait>())
        {
            if (gait == SurfaceCollision.Gait.Person)
            {
                continue;
            }
            ArgumentException threw = Assert.Throws<ArgumentException>(
                () => NpcWalk.Plan("AN OLD ONE", to, from, walls, Radius, gait));
            Assert.Contains("#731", threw.Message, StringComparison.Ordinal);
        }
    }

    // ── SAYING WHERE IT WENT WRONG ───────────────────────────────────────────────────────────────────

    private static string Say(Hall hall, Egress.Move move, string what) =>
        string.Create(CultureInfo.InvariantCulture,
            $"  {hall.Body} B{-hall.Level} watch {hall.Watch}, `{move.Plate}` off top {move.TableIndex}: ")
        + what;

    private static bool Same(UndergroundComplex.LockedDoor a, UndergroundComplex.LockedDoor b) =>
        Math.Abs(a.X1 - b.X1) < 1e-9 && Math.Abs(a.Y1 - b.Y1) < 1e-9
        && Math.Abs(a.X2 - b.X2) < 1e-9 && Math.Abs(a.Y2 - b.Y2) < 1e-9;

    /// <summary>Does a public mouth stand on this leaf? Middles compared, at less than a body's width, which
    /// is the resolution at which "the same opening" means anything.</summary>
    private static bool Overlaps(SurfaceLayout.Doorway way, UndergroundComplex.LockedDoor door)
    {
        double wx = (way.X1 + way.X2) / 2, wy = (way.Y1 + way.Y2) / 2;
        double dx = (door.X1 + door.X2) / 2, dy = (door.Y1 + door.Y2) / 2;
        return Math.Abs(wx - dx) < Radius && Math.Abs(wy - dy) < Radius;
    }
}

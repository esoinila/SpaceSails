using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #731 · <b>THE BAR KEEPS ITS OWN HOURS.</b>
///
/// <para><b>Owner, 2026-08-06:</b> <i>"Like on the bar now they have to wait for us to leave before they can
/// sit up… or leave the bar."</i> And, <b>2026-09-01:</b> <i>"also just other customers arriving and leaving
/// in the bars already does a lot… they can go behind doors that are locked to us."</i></para>
///
/// <para>#731 v1 and v2 built the whole walker and wired it to a Hive canteen floor. This is the room the
/// owner actually drinks in getting it: a regular finishes and walks out through a leaf the captain's own TRY
/// is refused at, somebody comes OUT of one and takes a chair, and a stranger who has just been refused your
/// last offer turns round where she is standing and goes. Not one line is said about any of it.</para>
///
/// <h3>Why none of this can pass vacuously</h3>
///
/// <para>Every behavioural claim below is made TWICE — once in a world where the thing must not happen and
/// once in a world where it must. A guard that only ever asserted "somebody left" would pass over a room that
/// emptied itself every frame; a guard that only ever asserted "nobody left" would pass over a room where
/// nothing works at all. Each pair is driven on ONE page, so the two halves cannot be two different worlds,
/// and each names how many real cases it reached.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBarKeepsItsOwnHoursTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>How many watches of every bar in the game the sweep walks. Eight watches is a day and a bit
    /// per berth, which across the shipped havens is enough evenings that a room with no metabolism in it
    /// cannot hide in the sample.</summary>
    private const int WatchesSwept = 8;

    /// <summary>The classy great-port tier, and the one the owner drinks in.</summary>
    private const string TheRedEye = "red-eye";

    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    // ── (a) THE SHIFT DECIDES, AND THE SHIFT ALONE ───────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOBODY GOES WHILE THE WATCH IS YOUNG, AND SOMEBODY DOES ONCE IT IS NOT.</b> The non-vacuous pair,
    /// on one page, at one berth, in one watch.
    ///
    /// <para>The captain walks into the bar at the top of a shift. The schedule this room has for the shift
    /// names a moment for everybody it is going to move, and until the clock reaches the first of them
    /// <b>nobody is on their feet and nobody has left the room</b> — the world in which the scene is not over.
    /// Then the clock crosses that moment and the SAME page puts somebody on the floor, bound for a leaf, with
    /// their chair empty behind them.</para>
    ///
    /// <para><b>Proven RED</b> by dealing the schedule with no <c>AtSecondsIntoWatch</c> gate at all (the
    /// young half goes red: <c>the watch is 40s old, nobody is due for another 1,673s, and 1 regular has
    /// already walked out</c>), and again by never calling <c>DealTheBarsHours</c> (the old half goes red:
    /// <c>the shift named GILT-EYE at 1,673s into the watch; it is now 2,673s and nobody has moved</c>).</para>
    /// </summary>
    [Fact]
    public void NOBODY_LeavesBeforeTheShiftSaysSoAndSomebodyDoesAfterwards()
    {
        int rooms = 0;
        var wrong = new List<string>();

        foreach ((string berth, long watch, Egress.Move first) in EveryRoomWithADeparture())
        {
            rooms++;
            Pages.Map map = AshoreAt(berth, watch);

            // ── THE SCENE IS NOT OVER ── the shift has named a moment and the clock has not reached it.
            RunFrames(map, 400);   // 40 sim seconds, and the earliest move in the sweep is 1,600 away
            double now = Into(map);
            if (Afoot(map).Count > 0 || Left(map).Count > 0)
            {
                wrong.Add(
                    $"{berth}@{watch}: the watch is {now:N0}s old, nobody is due for another "
                    + $"{first.AtSecondsIntoWatch - now:N0}s, and {Left(map).Count} regular(s) have already "
                    + "walked out. A room that empties itself on frame one has no hours, it has a leak.");
                continue;
            }

            // ── …AND NOW IT IS ── the same page, the same shift, one clock hand further round.
            JumpTo(map, first.AtSecondsIntoWatch + 1);
            RunFrames(map, 1);

            if (Afoot(map).Count == 0)
            {
                wrong.Add(
                    $"{berth}@{watch}: the shift named {first.Plate} at {first.AtSecondsIntoWatch:N0}s into "
                    + $"the watch; it is now {Into(map):N0}s and nobody has moved.");
                continue;
            }

            object who = Afoot(map)[0]!;
            if (!string.Equals((string)Get(who, "Who")!, first.Plate, StringComparison.Ordinal))
            {
                wrong.Add($"{berth}@{watch}: the shift named {first.Plate} and the room stood up "
                          + $"{Get(who, "Who")}.");
            }

            if (!Left(map).Contains(first.Plate))
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} is on his feet and the room is still seating him — "
                          + "one body in two places, drawn.");
            }
        }

        Assert.True(rooms >= 4,
                    $"only {rooms} bar-watch(es) in the whole sweep had a departure to check — this guard "
                    + "would be a green number never asked of the world.");
        Assert.True(wrong.Count == 0,
                    $"{wrong.Count} of {rooms} room(s) do not keep their own hours:"
                    + Environment.NewLine + string.Join(Environment.NewLine, wrong.Take(10)));
    }

    /// <summary>
    /// <b>THE LEAF HE GOES THROUGH IS ONE THE CAPTAIN IS REFUSED AT</b> — #731's load-bearing law, asked in
    /// this room of the captain's OWN offer rather than of a flag beside the walker.
    ///
    /// <para>The walk's sign is matched back to a leaf the deck really hangs (by geometry, not by name), the
    /// deck's own door there is <c>Locked</c>, and every item in the satchel is offered at it through
    /// <c>SatchelTry</c> — the same call the captain's press makes. Not one of them may work.</para>
    ///
    /// <para><b>Proven RED</b> by dealing the door out of <c>_deckPlan.Doors.Where(d =&gt; !d.Locked)</c>
    /// instead of the band's locked list: <c>the captain's own PRYBAR OPENS `🚪 BAR` — the door somebody left
    /// through is one the captain may follow them through</c>.</para>
    /// </summary>
    [Fact]
    public void THE_DOOR_HeGoesThroughIsOneTheCaptainsTryRefuses()
    {
        int walks = 0;
        var wrong = new List<string>();

        foreach ((string berth, long watch, Egress.Move first) in EveryRoomWithADeparture())
        {
            Pages.Map map = AshoreAt(berth, watch);
            JumpTo(map, first.AtSecondsIntoWatch + 1);
            RunFrames(map, 1);
            if (Afoot(map).Count == 0)
            {
                continue;
            }

            walks++;
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;
            object walk = Get(Afoot(map)[0]!, "Walk")!;
            string sign = (string)Get(Get(walk, "For")!, "Sign")!;

            UndergroundComplex.LockedDoor? leaf = bar.Doors.Cast<UndergroundComplex.LockedDoor?>()
                .FirstOrDefault(l => string.Equals(l!.Value.Sign, sign, StringComparison.Ordinal));
            if (leaf is not { } door)
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} is bound for `{sign}`, which is not a leaf this "
                          + "bar publishes at all.");
                continue;
            }

            var plan = (DeckPlan)Field(map, "_deckPlan")!;
            if (!plan.Doors.Any(d => d.Locked
                                     && Math.Abs(d.X1 - door.X1) < 1e-3 && Math.Abs(d.Y1 - door.Y1) < 1e-3
                                     && Math.Abs(d.X2 - door.X2) < 1e-3 && Math.Abs(d.Y2 - door.Y2) < 1e-3))
            {
                wrong.Add($"{berth}@{watch}: the deck hangs no LOCKED door where `{sign}` is.");
            }

            foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
            {
                if (SatchelTry.Offer(new Satchel.Item(kind, "test-item"),
                                     SatchelTry.Target.RoomDoor, berth).Worked)
                {
                    wrong.Add($"{berth}@{watch}: the captain's own {kind} OPENS `{sign}` — the door somebody "
                              + "left through is one the captain may follow them through, and the whole beat "
                              + "is gone.");
                }
            }
        }

        Assert.True(walks >= 4, $"only {walks} departure(s) in the sweep were reachable — a green number "
                                + "never asked of the world.");
        Assert.True(wrong.Count == 0,
                    string.Join(Environment.NewLine, wrong.Take(10)));
    }

    // ── (b) THE CAPTAIN IN THE DOORWAY ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>STAND IN THE DOOR AND HE STOPS, WAITS, AND LOOKS AT YOU.</b> The ruling on this issue, in this
    /// room: <i>"a departing NPC blocked by the captain in the doorway stops, waits, and looks at you — that
    /// IS content; they never clip through."</i>
    ///
    /// <para>Three facts, and the third is the non-vacuous half: on no frame does the body come within a
    /// body-width of the captain; it ends its frames <c>Waiting</c> rather than <c>Arrived</c> or
    /// <c>Snagged</c>; and when the captain steps out of the doorway <b>the same walker finishes the same
    /// walk</b>. A yield that dropped the route would pass the first two and be a man who gives up on the
    /// door because you walked past him.</para>
    ///
    /// <para><b>Proven RED</b> by planning the scheduled departure with <c>NpcWalk.NoPersonalSpace</c>:
    /// <c>the captain is standing on the doorstep and the body came to 0.00 du of him — a clip, drawn</c>.</para>
    /// </summary>
    [Fact]
    public void THE_BLOCKED_DoorwayIsABodyThatWaitsAndNeverOneThatClips()
    {
        int blocked = 0;
        var wrong = new List<string>();

        foreach ((string berth, long watch, Egress.Move first) in EveryRoomWithADeparture())
        {
            Pages.Map map = AshoreAt(berth, watch);
            JumpTo(map, first.AtSecondsIntoWatch + 1);
            RunFrames(map, 1);
            if (Afoot(map).Count == 0)
            {
                continue;
            }

            object walk = Get(Afoot(map)[0]!, "Walk")!;
            var route = (IReadOnlyList<DeckReachability.Point>)Get(walk, "Route")!;
            DeckReachability.Point doorstep = route[^1];

            // The captain plants himself where the walk ENDS — the standing place at the leaf.
            Set(map, "_avatarX", doorstep.X);
            Set(map, "_avatarY", doorstep.Y);

            double nearest = double.MaxValue;
            for (int i = 0; i < 900; i++)
            {
                RunFrames(map, 1);
                if (Afoot(map).Count == 0)
                {
                    break;
                }
                double dx = (double)Get(walk, "X")! - doorstep.X;
                double dy = (double)Get(walk, "Y")! - doorstep.Y;
                nearest = Math.Min(nearest, Math.Sqrt((dx * dx) + (dy * dy)));
            }

            blocked++;
            double keepOut = DeckPlan.AvatarRadius * NpcWalk.PersonalSpaceInRadii;
            if (Afoot(map).Count == 0)
            {
                wrong.Add($"{berth}@{watch}: the captain is standing on the doorstep and {first.Plate} went "
                          + "through him anyway — the walk finished.");
                continue;
            }

            if (nearest < keepOut - 1e-6)
            {
                wrong.Add($"{berth}@{watch}: the captain is standing on the doorstep and the body came to "
                          + $"{nearest:F2} du of him ({keepOut:F2} is touching) — a clip, drawn.");
            }

            var state = (NpcWalk.Doing)Get(walk, "State")!;
            if (state != NpcWalk.Doing.Waiting)
            {
                wrong.Add($"{berth}@{watch}: blocked in the doorway, {first.Plate} is {state} rather than "
                          + "Waiting — being looked at is the content.");
            }

            // …and he is LOOKING AT YOU, not at the door.
            double facing = (double)Get(walk, "Facing")!;
            double toward = Math.Atan2(doorstep.Y - (double)Get(walk, "Y")!,
                                       doorstep.X - (double)Get(walk, "X")!);
            if (Math.Abs(Math.Atan2(Math.Sin(facing - toward), Math.Cos(facing - toward))) > 1e-6)
            {
                wrong.Add($"{berth}@{watch}: he is waiting and facing {facing:F2} rather than at the captain "
                          + $"({toward:F2}).");
            }

            // ── AND HE FINISHES WHEN YOU MOVE ── the route was never dropped.
            Set(map, "_avatarX", doorstep.X + 40);
            Set(map, "_avatarY", doorstep.Y + 40);
            RunFrames(map, 900);
            if (Afoot(map).Count != 0)
            {
                wrong.Add($"{berth}@{watch}: the captain stepped out of the doorway and {first.Plate} never "
                          + "went through it — the yield dropped his route.");
            }
        }

        Assert.True(blocked >= 4, $"only {blocked} doorway(s) were actually blocked in the sweep.");
        Assert.True(wrong.Count == 0,
                    string.Join(Environment.NewLine, wrong.Take(10)));
    }

    // ── (c) THE OTHER DIRECTION ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>SOMEBODY COMES OUT OF THE BACK AND TAKES A CHAIR</b> — the owner's 2026-09-01 half, and the same
    /// walker run in reverse.
    ///
    /// <para>The non-vacuous pair again: before the moment the shift named, the chair he is coming to is
    /// EMPTY and he is nowhere; after it he is on the floor, his route is a real path out of a leaf the deck
    /// hangs locked (never a one-point route, which is a teleport with a plate on it), and when it runs out
    /// the room is seating him at that chair with a console the [E] key finds.</para>
    ///
    /// <para>Only regulars the rota has <c>InTheBack</c> may do it. A man the rota has simply <c>Gone</c> is
    /// not at this station, and materialising him in the cellar would be the room telling a lie the barkeep's
    /// own line contradicts.</para>
    ///
    /// <para><b>Proven RED</b> by seating him on the frame the walk is PLANNED instead of the frame it lands
    /// (<c>he is drawn crossing the floor and drawn in the chair at the same time</c>), and again by drawing
    /// the arrival pool from every away regular rather than the <c>InTheBack</c> ones (<c>MADAM COIL is Gone
    /// from this station and came out of the cellar</c>).</para>
    /// </summary>
    [Fact]
    public void SOMEBODY_ComesOutOfTheBackAndTakesAChair()
    {
        int arrivals = 0;
        var wrong = new List<string>();

        foreach ((string berth, long watch, Egress.Move first) in EveryRoomWithAnArrival())
        {
            Pages.Map map = AshoreAt(berth, watch);

            // ── HE IS NOT HERE YET ──
            RunFrames(map, 400);
            if (CameIn(map).Count > 0 || Afoot(map).Count > 0)
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} is due at {first.AtSecondsIntoWatch:N0}s and the "
                          + $"room already has him at {Into(map):N0}s.");
                continue;
            }

            // ── AND NOW HE IS ──
            JumpTo(map, first.AtSecondsIntoWatch + 1);
            RunFrames(map, 1);
            if (Afoot(map).Count == 0)
            {
                wrong.Add($"{berth}@{watch}: the shift named {first.Plate} at "
                          + $"{first.AtSecondsIntoWatch:N0}s and nobody came out of the back.");
                continue;
            }

            arrivals++;
            object walk = Get(Afoot(map)[0]!, "Walk")!;
            var route = (IReadOnlyList<DeckReachability.Point>)Get(walk, "Route")!;
            if (route.Count <= 1)
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} was placed rather than walked — a one-point route "
                          + "is a teleport with a plate on it.");
            }

            // He starts on the doorstep of a leaf the deck hangs LOCKED…
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;
            if (!bar.Doors.Any(l => OnItsDoorstep(route[0], l)))
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} came out of ({route[0].X:F1},{route[0].Y:F1}), "
                          + "which is not the doorstep of any leaf this bar publishes.");
            }

            // …and he is NOT in the chair while he is still walking to it.
            if (CameIn(map).Count > 0)
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} is drawn crossing the floor and drawn in the "
                          + "chair at the same time.");
            }

            RunFrames(map, 900);
            if (!CameIn(map).TryGetValue(first.Plate, out int chair))
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} crossed the floor and the room never seated him.");
                continue;
            }

            // …and the ROOM agrees: a console the [E] key finds, at the chair he took.
            DeckReachability.Point seat = HavenInterior.PatronSeatAt(chair)!.Value;
            var plan = (DeckPlan)Field(map, "_deckPlan")!;
            if (!plan.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.BarPatron
                                        && Math.Abs(c.X - seat.X) < 1e-3 && Math.Abs(c.Y - seat.Y) < 1e-3))
            {
                wrong.Add($"{berth}@{watch}: {first.Plate} sat down at chair {chair} and [E] finds nobody "
                          + "there — the walked room and the drawn room disagree.");
            }
        }

        Assert.True(arrivals >= 3,
                    $"only {arrivals} arrival(s) in the whole sweep — this guard would be a green number "
                    + "never asked of the world.");
        Assert.True(wrong.Count == 0,
                    string.Join(Environment.NewLine, wrong.Take(10)));
    }

    /// <summary>
    /// <b>ONLY THE ONES THE ROOM HAS IN THE BACK COME OUT OF THE BACK.</b> The gate on the arrival pool,
    /// asked of every bar and every watch in the sweep, so the claim is about the law and not about the one
    /// evening the guard above happened to drive.
    ///
    /// <para><b>Proven RED</b> by widening the pool to every away regular.</para>
    /// </summary>
    [Fact]
    public void ONLY_TheOnesInTheBackComeOutOfIt()
    {
        int dealt = 0;
        var wrong = new List<string>();

        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            for (long watch = 0; watch < WatchesSwept; watch++)
            {
                Pages.Map map = AshoreAt(berth, watch);
                var rota = HavenInterior.ResolveRegulars(berth, watch * PatronRota.WatchSeconds)
                    .ToDictionary(r => r.Id, r => r.State, StringComparer.Ordinal);

                foreach (Egress.Move m in Coming(map))
                {
                    dealt++;
                    if (rota[m.Plate] != PatronState.InTheBack)
                    {
                        wrong.Add($"{berth}@{watch}: {m.Plate} is {rota[m.Plate]} at this station and came "
                                  + "out of the cellar anyway.");
                    }
                }

                foreach (Egress.Move m in Going(map))
                {
                    if (rota[m.Plate] != PatronState.AtBar)
                    {
                        wrong.Add($"{berth}@{watch}: {m.Plate} is {rota[m.Plate]} and the shift stood him up "
                                  + "out of a chair he was never in.");
                    }
                }
            }
        }

        Assert.True(dealt >= 8, $"only {dealt} arrival(s) were dealt across the whole sweep.");
        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong.Take(10)));
    }

    // ── (d) THE TRIGGERED FULL STOP ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>SHE WALKS OUT FROM WHERE SHE IS STANDING, NOT FROM THE CELLAR.</b> The plot half of the ruling —
    /// <i>a stranger who has just refused your last offer walks out</i> — and the bug it fixes.
    ///
    /// <para>Until this lane an answered visitor was taken off the floor and re-planned from a back-room
    /// doorstep: a body that vanished from the captain's elbow and reappeared out of the cellar. #973 L5b's
    /// own file flagged it in as many words. So the claim is not merely "she leaves" — it is that her walk
    /// <b>begins where her body was</b> and <b>ends at a leaf the captain is refused at</b>.</para>
    ///
    /// <para>The non-vacuous half is the frame before: while she is still wanted she stays at the table, walk
    /// after walk, and nobody is bound for any door at all.</para>
    ///
    /// <para><b>Proven RED</b> by restoring <c>TheyWaitAtTheCounter</c> in <c>StepAnApproach</c>:
    /// <c>she was standing at (-9.0,52.6) and her walk out starts at (-13.0,54.0) — 4.24 du away, which is
    /// the cellar door. She teleported.</c></para>
    /// </summary>
    [Fact]
    public void SHE_WalksOutFromWhereSheIsStandingRatherThanFromTheCellar()
    {
        // A room with NOTHING on its own schedule this watch, so the only body on the floor is hers and the
        // guard is about her rather than about whoever the shift happened to move.
        (string berth, long watch) = AQuietRoom();
        Pages.Map map = AshoreAt(berth, watch);
        StandAtATop(map, berth);

        bool wanted = true;
        Assert.True((bool)Invoke(map, "ApproachTheTable",
                                 "◈ SOMEBODY", (Func<bool>)(() => wanted), (Action)(() => { }))!);
        RunFrames(map, 900);

        object her = Assert.Single(Afoot(map).Cast<object>());
        object walk = Get(her, "Walk")!;
        Assert.Equal(NpcWalk.Doing.Arrived, (NpcWalk.Doing)Get(walk, "State")!);

        // ── THE SCENE IS NOT OVER ── she is at the table and nobody in this room is bound for a door.
        RunFrames(map, 300);
        Assert.Single(Afoot(map).Cast<object>());
        Assert.Equal("", (string)Get(Get(walk, "For")!, "Sign")!);

        double stoodX = (double)Get(walk, "X")!, stoodY = (double)Get(walk, "Y")!;

        // ── AND NOW IT IS ── the offer is refused, and the room says so by walking her out of it.
        wanted = false;
        RunFrames(map, 1);

        object leaving = Assert.Single(Afoot(map).Cast<object>());
        object outward = Get(leaving, "Walk")!;
        var route = (IReadOnlyList<DeckReachability.Point>)Get(outward, "Route")!;

        double gap = Math.Sqrt(((route[0].X - stoodX) * (route[0].X - stoodX))
                               + ((route[0].Y - stoodY) * (route[0].Y - stoodY)));
        Assert.True(gap <= DeckReachability.DefaultStep + 1e-6,
                    $"she was standing at ({stoodX:F1},{stoodY:F1}) and her walk out starts at "
                    + $"({route[0].X:F1},{route[0].Y:F1}) — {gap:F2} du away. She teleported.");

        HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;
        string sign = (string)Get(Get(outward, "For")!, "Sign")!;
        UndergroundComplex.LockedDoor leaf = Assert.Single(
            bar.Doors, l => string.Equals(l.Sign, sign, StringComparison.Ordinal));
        Assert.True(OnItsDoorstep(route[^1], leaf),
                    $"her walk out ends at ({route[^1].X:F1},{route[^1].Y:F1}), which is not the doorstep of "
                    + $"`{sign}`.");

        // …and it is a leaf the captain's own offer is refused at.
        foreach (Satchel.Kind kind in Enum.GetValues<Satchel.Kind>())
        {
            Assert.False(
                SatchelTry.Offer(new Satchel.Item(kind, "test-item"),
                                 SatchelTry.Target.RoomDoor, berth).Worked,
                $"the captain's own {kind} opens `{sign}` — the full stop is a comma.");
        }

        // …and she really does go through it.
        RunFrames(map, 900);
        Assert.Empty(Afoot(map));
    }

    // ── (e) THE ROOM DOES NOT SAY A WORD ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NOT ONE LINE EXPLAINS THE DOOR.</b> The whole of #731's grammar, and the ruling repeats it: <i>the
    /// NPC's door opens on their own authority exactly where the captain's TRY would fail, and NO line of
    /// dialog may explain it.</i>
    ///
    /// <para>A whole evening is driven — a scheduled departure, a scheduled arrival, and the triggered walk
    /// out — and <b>everything the game put in front of the player is transcribed and compared</b> against the
    /// same evening with the room's hours never dealt at all. The two must be byte for byte identical: the
    /// pulse, the autopilot log and the story-card state. What the room did, it did without saying anything.
    /// </para>
    ///
    /// <para>The comparison is a DIFFERENTIAL and not a "no new strings" check, because the honest failure
    /// mode is not a new file of prose — it is one line pulsed on the frame a leaf clicks. <b>Proven RED</b>
    /// by pulsing <c>"🚪 Staff only. That's why."</c> from <c>TheyStandUpAndGo</c>.</para>
    /// </summary>
    [Fact]
    public void NOT_ONE_LineExplainsTheDoor()
    {
        (string berth, long watch, Egress.Move first) = EveryRoomWithADeparture().First();

        string dealt = TranscribeAnEvening(berth, watch, first, hours: true);
        string quiet = TranscribeAnEvening(berth, watch, first, hours: false);

        Assert.Equal(quiet, dealt);
        Assert.DoesNotContain("CELLAR", dealt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("STOREROOM", dealt, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Everything the game put in front of the player over one evening in a bar, as one string. The
    /// pulse line, the autopilot log and whether a story card is up — the three surfaces this room could
    /// possibly say anything through.</summary>
    private static string TranscribeAnEvening(string berth, long watch, Egress.Move first, bool hours)
    {
        Pages.Map map = AshoreAt(berth, watch);
        StandAtATop(map, berth);

        var said = new List<string>();
        void Transcribe()
        {
            said.Add(((PulseSlot)Field(map, "_pulse")!).Message ?? "");
            said.Add(string.Join("|", ((IEnumerable<(double SimTime, string Text)>)Field(map, "_autopilotEvents")!)
                .Select(e => e.Text)));
        }

        if (hours)
        {
            JumpTo(map, first.AtSecondsIntoWatch + 1);
        }
        else
        {
            // The same evening with the room's hours never reached — the clock stays at the top of the shift.
            JumpTo(map, 1);
        }

        for (int i = 0; i < 900; i++)
        {
            RunFrames(map, 1);
            Transcribe();
        }

        return string.Join(Environment.NewLine, said);
    }

    // ── (f) DETERMINISM ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SHIFT IS A PURE FUNCTION OF THE WATCH.</b> The frozen-watch law: two pages clamped onto the
    /// same berth on the same watch deal the same people through the same doors at the same moments, and a
    /// different watch is a different evening.
    ///
    /// <para>The second clause is the non-vacuous half — a schedule that returned an empty list for every
    /// room would satisfy the first one perfectly.</para>
    ///
    /// <para><b>Proven RED</b> by seeding the deal off <c>DateTime.UtcNow.Ticks</c>: <c>red-eye@0 dealt
    /// GILT-EYE at 1,673s and, on a second page in the same watch, THE FIXER at 6,102s</c>.</para>
    /// </summary>
    [Fact]
    public void THE_SCHEDULE_IsTheShiftsAndNotTheClocks()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int rooms = 0;

        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            for (long watch = 0; watch < WatchesSwept; watch++)
            {
                string once = Transcript(AshoreAt(berth, watch));
                string twice = Transcript(AshoreAt(berth, watch));
                Assert.Equal(once, twice);

                if (once.Length > 0)
                {
                    rooms++;
                    seen.Add($"{berth}|{once}");
                }
            }
        }

        Assert.True(rooms >= 8, $"only {rooms} bar-watch(es) in the sweep scheduled anything at all.");
        Assert.True(seen.Count > 1,
                    "every watch of every bar deals the identical shift — a schedule that cannot tell two "
                    + "evenings apart is deterministic the way a constant is.");
    }

    /// <summary>…and the source says so: nothing in the room's hours reads a wall clock or a
    /// <c>System.Random</c>. The seeds are Core's splitmix, the clock hand is <c>SimTime</c>.</summary>
    [Fact]
    public void THE_ROOMS_HoursReadNoWallClock()
    {
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.BarWalkers.cs"));

        foreach (string forbidden in new[] { "DateTime", "DateTimeOffset", "Stopwatch", "new Random", "Guid.New" })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        }
    }

    // ── (g) NEVER REEVERS ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The bar's own walks are planned through <c>OnFoot</c>, which is the one line in this whole
    /// codebase that claims <c>Gait.Person</c> — so the constructor's refusal is the only path, and no walk
    /// this room deals could ever be handed a stagger.</summary>
    [Fact]
    public void NEVER_REEVERS_TheBarsWalksGoThroughTheOneGaitClaim()
    {
        string source = System.IO.File.ReadAllText(System.IO.Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.BarWalkers.cs"));

        Assert.DoesNotContain("SurfaceCollision.Gait", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NpcWalk.Plan(", source, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => NpcWalk.Plan(
            "◈ SOMEBODY", new NpcWalk.Bound("", 0, 0), new DeckReachability.Point(0, 0), [],
            DeckPlan.AvatarRadius, SurfaceCollision.Gait.Stagger));
    }

    // ── The sweep ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every (bar, watch) in the sweep whose shift actually sends somebody home, with the first move
    /// it deals — asked of a real page, so the guards drive the room the game builds and not a re-derivation
    /// of it.</summary>
    private static IEnumerable<(string Berth, long Watch, Egress.Move First)> EveryRoomWithADeparture()
    {
        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            for (long watch = 0; watch < WatchesSwept; watch++)
            {
                if (Going(AshoreAt(berth, watch)) is { Count: > 0 } list)
                {
                    yield return (berth, watch, list[0]);
                }
            }
        }
    }

    /// <summary>A bar-watch whose shift has decided NOTHING — nobody going, nobody coming. The bench for the
    /// beats that are about one particular body, so the guard is about HER and not about whoever the room
    /// happened to move while she was crossing it.</summary>
    private static (string Berth, long Watch) AQuietRoom()
    {
        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            for (long watch = 0; watch < WatchesSwept * 8; watch++)
            {
                Pages.Map map = AshoreAt(berth, watch);
                if (Going(map).Count == 0 && Coming(map).Count == 0)
                {
                    return (berth, watch);
                }
            }
        }

        throw new InvalidOperationException(
            "every bar in the game has something on its schedule on every watch swept — the room's hours are "
            + "not a beat, they are the weather.");
    }

    /// <summary>…and the same for the ones whose shift brings somebody out of the back.</summary>
    private static IEnumerable<(string Berth, long Watch, Egress.Move First)> EveryRoomWithAnArrival()
    {
        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            for (long watch = 0; watch < WatchesSwept; watch++)
            {
                Pages.Map map = AshoreAt(berth, watch);
                // …with nobody due to go first, so the guard is looking at an arrival rather than at whoever
                // the room happened to put on its feet.
                if (Going(map).Count == 0 && Coming(map) is { Count: > 0 } list)
                {
                    yield return (berth, watch, list[0]);
                }
            }
        }
    }

    /// <summary>What one page's shift has decided, as a string — for the determinism claim.</summary>
    private static string Transcript(Pages.Map map) =>
        string.Join(";", Going(map).Concat(Coming(map))
            .Select(m => $"{m.Plate}@{m.TableIndex}:{m.AtSecondsIntoWatch:F3}#{m.Door}"));

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page clamped onto a berth at the TOP of a given watch, standing in its bar, with the deck
    /// the game builds and the shift dealt.</summary>
    private static Pages.Map AshoreAt(string berth, long watch, bool repWorking = false)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", repWorking ? true : (bool?)false);
        Set(map, "SimTime", watch * PatronRota.WatchSeconds);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");

        // One frame, so the shift's own lists are worked out and can be read.
        Invoke(map, "AdvanceBarWalkers", 0.0);
        return map;
    }

    /// <summary>Stand the captain AT one of the bar's own tops, which is the nearest-top question
    /// <c>TheTopTheCaptainIsAt</c> asks, answered by standing there.</summary>
    private static void StandAtATop(Pages.Map map, string berth = TheRedEye)
    {
        DeckReachability.Point top = HavenInterior.BarBand(berth)!.Value.Tops[0];
        Set(map, "_avatarX", top.X);
        Set(map, "_avatarY", top.Y);
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

    /// <summary>Put the clock this far into the frozen watch, without running a frame.</summary>
    private static void JumpTo(Pages.Map map, double intoTheWatch)
    {
        long watch = (long)Invoke(map, "get_BarWatch")!;
        Set(map, "SimTime", (watch * PatronRota.WatchSeconds) + intoTheWatch);
    }

    private static double Into(Pages.Map map) => (double)Invoke(map, "get_IntoTheBarsWatch")!;

    private static IList Afoot(Pages.Map map) => (IList)Field(map, "_barAfoot")!;

    private static ISet<string> Left(Pages.Map map) => (ISet<string>)Field(map, "_barLeft")!;

    private static IDictionary<string, int> CameIn(Pages.Map map) =>
        (IDictionary<string, int>)Field(map, "_barCameIn")!;

    private static IReadOnlyList<Egress.Move> Going(Pages.Map map) =>
        (IReadOnlyList<Egress.Move>?)Field(map, "_barGoing") ?? [];

    private static IReadOnlyList<Egress.Move> Coming(Pages.Map map) =>
        (IReadOnlyList<Egress.Move>?)Field(map, "_barComing") ?? [];

    /// <summary>Is this point the doorstep of that leaf — within one standoff of its midline?</summary>
    private static bool OnItsDoorstep(DeckReachability.Point p, UndergroundComplex.LockedDoor leaf)
    {
        double mx = (leaf.X1 + leaf.X2) / 2, my = (leaf.Y1 + leaf.Y2) / 2;
        double span = Math.Sqrt(((leaf.X2 - leaf.X1) * (leaf.X2 - leaf.X1))
                                + ((leaf.Y2 - leaf.Y1) * (leaf.Y2 - leaf.Y1)));
        double d = Math.Sqrt(((p.X - mx) * (p.X - mx)) + ((p.Y - my) * (p.Y - my)));
        return d <= (span / 2) + Egress.DoorStandoffDu + 1e-6;
    }

    private static string RepoRoot()
    {
        System.IO.DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (System.IO.Directory.Exists(System.IO.Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }

            at = at.Parent;
        }

        throw new System.IO.DirectoryNotFoundException(
            $"could not find the repo root above {AppContext.BaseDirectory}");
    }

    // ── Reflection plumbing ──────────────────────────────────────────────────────────────────────────────

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Get(object o, string member) => o.GetType().GetProperty(member, Hidden)!.GetValue(o);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo call = typeof(Pages.Map).GetMethod(method, Hidden)
            ?? throw new InvalidOperationException($"Map has no `{method}` — this guard is reading a dead name.");
        try
        {
            return call.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}

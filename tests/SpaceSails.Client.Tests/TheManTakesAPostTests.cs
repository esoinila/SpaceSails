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
/// #1229 · <b>THE MAN TAKES A POST</b> — the second of the two behaviours #1062's brief named and only the
/// first of which shipped.
///
/// <para>The brief, verbatim: <i>"enters the room after the captain, keeps a distance band, TAKES A
/// SEAT/STANDING SPOT WITH A SIGHTLINE TO HIM, orders nothing."</i> The shipped walker only ever kept a
/// BAND — and a 9–30 du band does not fit inside a station bar, so the stone pushed him out through the one
/// doorway and he lost a captain he had never been in the room with. #1231 measured it frame by frame at
/// Selene Gate: dealt on the captain's own boot spot, out through the door on the first bearing the stone
/// allowed, wall closed at t+3 s, given up at t+12 s, silently.</para>
///
/// <h3>What this file drives, and how</h3>
///
/// <para>Everything here is reached the REAL way: the page is booted ashore through <c>SetDeckForDock</c> and
/// <c>StandAtTheBarThreshold</c>, the captain WALKS at his own <c>AvatarSpeed</c> one frame at a time, and he
/// sits through the shipped <c>TryTakeBarTop</c> — the same <c>[E]</c> a player presses. Nothing about the
/// man is set by hand: where he is dealt, where he goes and what he does when he gets there are all the
/// page's own answers, read off the walker list afterwards.</para>
///
/// <para>And it is swept over <b>every haven bar in sol.json</b>, not Selene Gate alone
/// (<see cref="HavenInterior.InteriorBodyIds"/>), because a beat proved in one room is a beat about one
/// room.</para>
///
/// <h3>Red proof — the reverts these were watched under</h3>
///
/// <list type="bullet">
///   <item><b>THE SHIPPED SPOT-FINDER, EXACTLY</b> — <c>ThisRoomCanHoldHisBand</c> written as <c>true</c>
///   (he only ever keeps a band, indoors and out) with the room clause taken back out of
///   <c>HeCouldStandAt</c>, so the only place clause left is the gangway line, which is what shipped.
///   <b>4 RED:</b> <see cref="TheChairReadingFiresFromEveryTopThatSeesTheDoorAtEveryHaven"/> at every haven
///   in sol.json, <see cref="HisPostIsAgainstTheStoneAndNeverTheCounterOrAChair"/>,
///   <see cref="HeDoesNotShuffleWhileHisLineHolds"/>, and <c>TheTailBehindYouTests.HeComesInAfterYouTakes
///   APlaceInTheRoomAndNeverGoesToTheCounter</c>.</item>
///   <item><b>The deal put back on <c>HavenInterior.BarThreshold</c></b> — the spot <c>?ashore=1</c> stands
///   the CAPTAIN on. <b>2 RED:</b> <see cref="HeIsNeverDealtOnTheCaptainsFeet"/> at every haven, and the
///   same #1062 guard.</item>
///   <item><b>The follow-out branch dropped</b> (he never goes to the doorway between them). <b>1 RED:</b>
///   <c>TheTailBehindYouTests.TheSameCoatThroughTwoDoorwaysIsTheTell</c>, which is the point of
///   re-grounding it.</item>
/// </list>
///
/// <para><b>And one clause that could NOT be made to go red, reported rather than dressed up.</b> The room
/// clause in <c>HeCouldStandAt</c> on its own — taken out while the post branch stays — reddens nothing at
/// today's geometry: indoors the post branch already keeps him in the room, and out on the ring the sounding's
/// own bearing order never happens to pick a spot through the bar's doorway, even from a captain standing on
/// its own doorstep. It is kept because it is the law and because it is reddened by the shipped-body revert
/// above, not because a guard here is watching it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheManTakesAPostTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;
    private const string ThreadId = "b47c2f1a08d94e6cb1f37a55d0e29c31";

    /// <summary>The captain's own pace, quoted from the page rather than chosen here — a guard that walked
    /// him at somebody else's speed would be a guard about a captain this game does not have.</summary>
    private static readonly double AvatarSpeed =
        (double)typeof(Pages.Map).GetField("AvatarSpeed", Hidden)!.GetRawConstantValue()!;

    // ── THE ROOM IS SMALLER THAN HIS BAND, AND THAT IS MEASURED ─────────────────────────────────────────

    /// <summary>
    /// #1229 · <b>THE MEASUREMENT THE WHOLE BEHAVIOUR RESTS ON, and it is not the one the brief guessed.</b>
    ///
    ///
    /// <para>Measured on the room's own stone, from the room's own doorway, along the bearings Core
    /// publishes: <b>the bar's walkable reach is shorter than the band's far edge and the ring's is not.</b>
    /// So the room really is smaller than his band and the ring really is not, which is the whole of why
    /// there are two behaviours and not a preference between them.</para>
    ///
    /// <para><b>Where reality differed from the brief.</b> The design says <i>"a room whose walkable extent
    /// cannot hold the band's INNER edge"</i>, and that reading is not true of a station bar: 9 du fits
    /// easily, and so does the 19.5 du reach a band is actually kept at, in several directions from most of
    /// the room's tops. What does not fit is the band — its far edge is 30 du and the bar has about
    /// twenty-three in its longest walkable line. A man told to keep a 9–30 du band in a room that size
    /// cannot hold it as his subject crosses the floor, which is exactly what #1231 watched happen.</para>
    /// </summary>
    [Fact]
    public void TheBarsWalkableReachIsShorterThanHisBandAndTheRingsIsNot()
    {
        var wrong = new List<string>();
        int berths = 0;

        foreach (string berth in EveryHavenBar())
        {
            berths++;
            Pages.Map map = Tailed(berth);
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;
            var deck = (DeckPlan)Field(map, "_deckPlan")!;
            (double inX, double inY, _) = HavenInterior.BarThreshold;
            (double outX, double outY) = HavenInterior.TheDoorstepOutsideTheBar;

            double inTheRoom = TheRoomsReachFrom(inX, inY, deck, y => y > bar.FloorY);
            double onTheRing = TheRoomsReachFrom(outX, outY, deck, y => y < bar.FloorY && y > 22);

            if (inTheRoom >= TheTailBehindYou.LosesYouBeyondDu)
            {
                wrong.Add($"{berth}: the BAR reaches {inTheRoom:F1} du from its own doorway, which holds a " +
                    $"band that loses you past {TheTailBehindYou.LosesYouBeyondDu:F1} — it is not a room too " +
                    "small for one, and #1229's premise is wrong here.");
            }

            if (onTheRing < TheTailBehindYou.LosesYouBeyondDu)
            {
                wrong.Add($"{berth}: the RING reaches only {onTheRing:F1} du from the same doorway, so the " +
                    "band does not fit out there either and 'out on the ring he keeps the band' is a " +
                    "sentence about nowhere.");
            }
        }

        Assert.True(berths > 5, $"only {berths} haven(s) swept.");
        Assert.True(wrong.Count == 0, string.Join("\n  ", wrong.Take(8)));
    }

    /// <summary>How far the room reaches from a spot, along the bearings Core publishes: the longest straight
    /// walkable line in it that does not leave it. Stepped at a body's width, which is the only unit a room
    /// like this has.</summary>
    private static double TheRoomsReachFrom(double x, double y, DeckPlan deck, Func<double, bool> inTheRoom)
    {
        double step = TheTailBehindYou.StandsOffTheWallBy(DeckPlan.AvatarRadius);
        double furthest = 0;

        foreach (double bearing in TheTailBehindYou.TheSidesHeSounds)
        {
            double dx = Math.Cos(bearing), dy = Math.Sin(bearing);
            for (double reach = step; reach <= 60; reach += step)
            {
                double px = x + (dx * reach), py = y + (dy * reach);
                if (!inTheRoom(py) || SurfaceCollision.Blocked(px, py, DeckPlan.AvatarRadius, deck.CollisionField))
                {
                    break;
                }

                furthest = Math.Max(furthest, reach);
            }
        }

        return furthest;
    }

    // ── HE COMES IN AFTER YOU ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1229 · <b>HE IS NEVER DEALT ON THE CAPTAIN'S FEET.</b> <c>HavenInterior.BarThreshold</c> is where
    /// <c>?ashore=1</c> stands the CAPTAIN — so a man dealt there is dealt on his toes, which is not a tail,
    /// it is a collision, and it is what shipped. He is dealt on the CONCOURSE side of the doorway and walks
    /// in.
    /// </summary>
    [Fact]
    public void HeIsNeverDealtOnTheCaptainsFeet()
    {
        var onTheFeet = new List<string>();
        int berths = 0;

        foreach (string berth in EveryHavenBar())
        {
            berths++;
            Pages.Map map = Tailed(berth);
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;

            // the boot spot itself, which is the case #1231 measured
            RunFrames(map, 1);
            object coat = TheCoat(map) ?? throw new InvalidOperationException($"{berth}: nobody came in.");

            double range = RangeToCoat(map, coat);
            if (range < 2 * DeckPlan.AvatarRadius || CoatY(coat) > bar.FloorY)
            {
                onTheFeet.Add($"{berth}: dealt at ({CoatX(coat):F2},{CoatY(coat):F2}), {range:F2} du from a " +
                    $"captain at ({Ax(map):F2},{Ay(map):F2}) and the room's wall is y={bar.FloorY:F2}.");
            }
        }

        Assert.True(berths > 5, $"only {berths} haven(s) swept.");
        Assert.True(onTheFeet.Count == 0,
            $"{onTheFeet.Count} haven(s) deal him inside the room or on the captain's own square:\n  " +
            string.Join("\n  ", onTheFeet));
    }

    // ── THE CHAIR READING, FROM EVERY TOP THAT SEES THE DOOR ────────────────────────────────────────────

    /// <summary>
    /// #1229 · <b>THE READING FIRES FROM EVERY TOP THE CAPTAIN CAN TAKE THAT SEES THE DOOR</b> — at every
    /// haven, driven the way a player drives it: walk there at the captain's own pace, press the shipped
    /// <c>[E]</c>, sit. The bug this guard is grounded on lost the captain three seconds after the boot and
    /// said nothing for ever.
    ///
    /// <para>Tops a regular is already sitting at are skipped — <c>TryTakeBarTop</c> refuses them, and a
    /// guard that asserted a reading from a chair the captain cannot have is a guard about a world the game
    /// does not build.</para>
    /// </summary>
    [Fact]
    public void TheChairReadingFiresFromEveryTopThatSeesTheDoorAtEveryHaven()
    {
        var silent = new List<string>();
        int sat = 0;

        foreach (string berth in EveryHavenBar())
        {
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;
            (double doorX, double doorY, _) = HavenInterior.BarThreshold;

            foreach (DeckReachability.Point top in bar.Tops)
            {
                Pages.Map map = Tailed(berth);
                var deck = (DeckPlan)Field(map, "_deckPlan")!;
                if (HavenInterior.BesideATop(top, DeckPlan.AvatarRadius, deck.CollisionField) is not { } chair
                    || !TheTailBehindYou.ThisChairSeesTheDoor(
                        chair.X, chair.Y, doorX, doorY, deck.CollisionField))
                {
                    continue;   // this top has no line to the room's one doorway; the beat is not about it
                }

                RunFrames(map, 1);
                WalkCaptainTo(map, top.X, top.Y);
                if (!(bool)Invoke(map, "TryTakeBarTop")!)
                {
                    continue;   // somebody is in it; the shipped press refuses, so there is no sit to watch
                }

                sat++;
                Set(map, "_pulse", default(PulseSlot));

                int frames = 0;
                for (; frames < 1200 && !(bool)Field(map, "_coatSeen")!; frames++)
                {
                    RunFrames(map, 1);
                }

                if (!(bool)Field(map, "_coatSeen")!)
                {
                    object? coat = TheCoat(map);
                    silent.Add($"{berth}: sat at the top at ({top.X:F1},{top.Y:F1}) with a clear line to the " +
                        $"door and nothing was said in {frames / 10.0:F0} s — the man is " +
                        (coat is null ? "gone from the floor" : $"at ({CoatX(coat):F2},{CoatY(coat):F2})") +
                        " (#1229).");
                    continue;
                }

                Assert.Equal(TheTailBehindYou.FromThisChairLine, PulseSaying(map));
            }
        }

        Assert.True(sat > 20,
            $"only {sat} sit(s) in the whole sweep — a sweep that seats nobody proves nothing.");
        Assert.True(silent.Count == 0,
            $"{silent.Count} of {sat} sits paid for the reading and got nothing:\n  " +
            string.Join("\n  ", silent.Take(8)));
    }

    // ── THE POST ITSELF ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1229 · <b>WHERE HE STANDS, AND WHERE HE NEVER STANDS.</b> Inside the room, against the room's own
    /// stone, with a line to the captain — and never at the counter and never at a top, because a seat would
    /// make him a patron and the canon says he has not ordered.
    ///
    /// <para>"Against the stone" is asked the way the post was found: one body clear of some piece of the
    /// deck's own collision field. It is asserted at the ROOM's own resolution rather than to a coordinate,
    /// because where exactly the stone puts him is the stone's business.</para>
    /// </summary>
    [Fact]
    public void HisPostIsAgainstTheStoneAndNeverTheCounterOrAChair()
    {
        var wrong = new List<string>();
        int posts = 0;

        foreach (string berth in EveryHavenBar())
        {
            Pages.Map map = Tailed(berth);
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;
            var deck = (DeckPlan)Field(map, "_deckPlan")!;

            RunFrames(map, 1);
            WalkCaptainTo(map, bar.Tops[2].X, bar.Tops[2].Y);
            Settle(map);

            object coat = TheCoat(map) ?? throw new InvalidOperationException($"{berth}: nobody is on the floor.");
            posts++;
            double px = CoatX(coat), py = CoatY(coat);

            if (!(bool)Field(map, "_coatPosted")!)
            {
                wrong.Add($"{berth}: he is keeping a band inside a room that cannot hold one.");
                continue;
            }

            if (py <= bar.FloorY)
            {
                wrong.Add($"{berth}: posted at ({px:F2},{py:F2}), outside the room (wall y={bar.FloorY:F2}).");
            }

            if (!SurfaceCollision.HasLineOfSight(px, py, Ax(map), Ay(map), deck.CollisionField))
            {
                wrong.Add($"{berth}: posted at ({px:F2},{py:F2}) with no line to the captain.");
            }

            foreach (DeckReachability.Point service in bar.Fixtures)
            {
                if (Near(service, px, py))
                {
                    wrong.Add($"{berth}: posted AT THE COUNTER ({px:F2},{py:F2}) — he has not ordered.");
                }
            }

            foreach (DeckReachability.Point top in bar.Tops)
            {
                if (Near(top, px, py))
                {
                    wrong.Add($"{berth}: posted at a TOP ({px:F2},{py:F2}) — a seat makes him a patron.");
                }
            }

            if (!AgainstTheStone(px, py, deck))
            {
                wrong.Add($"{berth}: posted at ({px:F2},{py:F2}), which is not against any wall in the room.");
            }
        }

        Assert.True(posts > 5, $"only {posts} post(s) measured.");
        Assert.True(wrong.Count == 0, $"{wrong.Count} bad post(s):\n  " + string.Join("\n  ", wrong.Take(8)));
    }

    /// <summary>#1229 · <b>A POST IS KEPT UNTIL THE LINE BREAKS</b>, and not re-taken every time the captain
    /// crosses the room. A man who shuffled along the wall after his subject would not be a man standing by
    /// the door; he would be a man following somebody round a bar, which is a different and much worse
    /// scene.</summary>
    [Fact]
    public void HeDoesNotShuffleWhileHisLineHolds()
    {
        foreach (string berth in EveryHavenBar())
        {
            Pages.Map map = Tailed(berth);
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;

            RunFrames(map, 1);
            WalkCaptainTo(map, bar.Tops[2].X, bar.Tops[2].Y);
            Settle(map);

            object coat = TheCoat(map)!;
            (double wasX, double wasY) = (CoatX(coat), CoatY(coat));

            // the captain crosses his own room, at his own pace, and keeps his line to the man the whole way
            WalkCaptainTo(map, bar.Tops[1].X, bar.Tops[1].Y);
            Settle(map);

            coat = TheCoat(map)!;
            Assert.True(
                Math.Abs(CoatX(coat) - wasX) < 1e-6 && Math.Abs(CoatY(coat) - wasY) < 1e-6,
                $"{berth}: he moved from ({wasX:F2},{wasY:F2}) to ({CoatX(coat):F2},{CoatY(coat):F2}) while " +
                "his line held — a post is kept until the line breaks (#1229).");
        }
    }

    /// <summary>
    /// #1229 · <b>AND ON THE RING HE KEEPS HIS BAND — ON THE RING.</b> The other half of the room clause, and
    /// the half the post branch cannot cover for: a captain standing on the concourse a few paces from the
    /// bar's doorway has a whole room visible through it, and the shipped sounding would happily stand his
    /// tail INSIDE the bar, through a door, in a room the captain is not in. Out here the band is unchanged;
    /// what is new is that the room is.
    /// </summary>
    [Fact]
    public void OutOnTheRingHeKeepsHisBandAndNeverThroughTheDoorwayIntoTheBar()
    {
        var wrong = new List<string>();
        int berths = 0;

        foreach (string berth in EveryHavenBar())
        {
            berths++;
            Pages.Map map = Tailed(berth);
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;

            // in first, so there is a man on the floor at all — then back out onto the ring and along it
            RunFrames(map, 1);
            WalkCaptainTo(map, bar.Tops[2].X, bar.Tops[2].Y);
            Settle(map);
            WalkCaptainTo(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y - 6);
            WalkCaptainTo(map, 2.5, 44);
            (double outX, double outY) = HavenInterior.TheDoorstepOutsideTheBar;
            WalkCaptainTo(map, outX, outY);   // …and back to the doorway, where the whole room is in view
            Settle(map);

            if (TheCoat(map) is not { } coat)
            {
                continue;   // he gave up on the way; the losing rule is somebody else's guard
            }

            if (CoatY(coat) > bar.FloorY)
            {
                wrong.Add($"{berth}: the captain is on the ring at ({Ax(map):F1},{Ay(map):F1}) and his tail " +
                    $"is at ({CoatX(coat):F2},{CoatY(coat):F2}) — inside the BAR, through the doorway, in a " +
                    "room the captain is not in (#1229).");
            }
        }

        Assert.True(berths > 5, $"only {berths} haven(s) swept.");
        Assert.True(wrong.Count == 0, string.Join("\n  ", wrong.Take(8)));
    }

    // ── AND HE STILL WILL NOT WALK DOWN THE GANGWAY ─────────────────────────────────────────────────────

    /// <summary>#1062/#1229 · The one clause in the spot-finder that is about WHO he is rather than about the
    /// stone, kept through the rewrite: a man paid to put a face to a hull does not follow the captain down
    /// his own gangway. It is also the timed close the audit left this feature in place of #1062's lift.</summary>
    [Fact]
    public void HeStillWillNotWalkDownTheGangway()
    {
        foreach (string berth in EveryHavenBar())
        {
            Pages.Map map = Tailed(berth);
            HavenInterior.BarFloor bar = HavenInterior.BarBand(berth)!.Value;

            RunFrames(map, 1);
            WalkCaptainTo(map, bar.Tops[2].X, bar.Tops[2].Y);
            Settle(map);

            // down the umbilical, which is the one part of a berth he does not follow anybody into
            WalkCaptainTo(map, HavenInterior.BarThreshold.X, HavenInterior.BarThreshold.Y - 4);
            WalkCaptainTo(map, 2.5, 26);
            WalkCaptainTo(map, 2.5, 16);

            for (int i = 0; i < 400; i++)
            {
                RunFrames(map, 1);
                if (TheCoat(map) is { } coat)
                {
                    Assert.True(CoatY(coat) > 22,
                        $"{berth}: he is at ({CoatX(coat):F2},{CoatY(coat):F2}) — down the gangway, which is " +
                        "a boarding and not a tail (#1062).");
                }
            }
        }
    }

    // ── PLUMBING ────────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<string> EveryHavenBar()
    {
        foreach (string berth in HavenInterior.InteriorBodyIds)
        {
            if (HavenInterior.BarBand(berth) is not null)
            {
                yield return berth;
            }
        }
    }

    private static bool Near(DeckReachability.Point what, double x, double y)
    {
        double dx = what.X - x, dy = what.Y - y;
        return (dx * dx) + (dy * dy) <= DeckPlan.InteractRadius * DeckPlan.InteractRadius;
    }

    /// <summary>Is he one body clear of some piece of the room's own stone — which is what a POST is?</summary>
    private static bool AgainstTheStone(double x, double y, DeckPlan deck)
    {
        double reach = (2 * TheTailBehindYou.StandsOffTheWallBy(DeckPlan.AvatarRadius))
            * (2 * TheTailBehindYou.StandsOffTheWallBy(DeckPlan.AvatarRadius));
        foreach (SurfaceCollision.Segment wall in deck.CollisionSegments)
        {
            foreach ((double px, double py) in TheTailBehindYou.PostsAlong(
                         wall.X1, wall.Y1, wall.X2, wall.Y2, DeckPlan.AvatarRadius, x, y))
            {
                double dx = px - x, dy = py - y;
                if ((dx * dx) + (dy * dy) <= reach)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Pages.Map Tailed(string berth)
    {
        var map = new Pages.Map();
        Set(map, "SimTime", 0.0);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_ashore", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", (bool?)false);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
        Set(map, "_tailedCheat", (bool?)true);
        return map;
    }

    /// <summary>WALK him there, at the captain's own pace, one frame at a time — never a teleport, which
    /// would break the man's line for him and turn every guard here into a test of the losing rule.</summary>
    private static void WalkCaptainTo(Pages.Map map, double x, double y, double dt = 0.1)
    {
        double step = AvatarSpeed * dt;
        for (int guard = 0; guard < 4000; guard++)
        {
            double atX = Ax(map), atY = Ay(map);
            double dx = x - atX, dy = y - atY;
            double left = Math.Sqrt((dx * dx) + (dy * dy));
            if (left <= step)
            {
                StandCaptainAt(map, x, y);
                RunFrames(map, 1, dt);
                return;
            }

            StandCaptainAt(map, atX + (dx / left * step), atY + (dy / left * step));
            RunFrames(map, 1, dt);
        }
    }

    private static void StandCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        Set(map, "_lookPrevAvatarX", x);
        Set(map, "_lookPrevAvatarY", y);
    }

    /// <summary>Let him reach wherever he set off for, so a claim about what he does STANDING is not asked of
    /// a body that is still walking.</summary>
    private static void Settle(Pages.Map map)
    {
        for (int i = 0; i < 900 && TheCoat(map) is { } coat && Afoot(coat); i++)
        {
            RunFrames(map, 1);
        }
    }

    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Set(map, "_lastTimestampMs", (double?)(((double?)Field(map, "_lastTimestampMs") ?? 0) + (dt * 1000)));
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static double Ax(Pages.Map map) => (double)Field(map, "_avatarX")!;

    private static double Ay(Pages.Map map) => (double)Field(map, "_avatarY")!;

    private static object? TheCoat(Pages.Map map)
    {
        foreach (object who in (IList)Field(map, "_barAfoot")!)
        {
            if (Get(who, "For")!.ToString() is "BehindYou" or "AskingTheWrongFloor")
            {
                return who;
            }
        }

        return null;
    }

    private static double CoatX(object coat) => (double)Get(Get(coat, "Walk")!, "X")!;

    private static double CoatY(object coat) => (double)Get(Get(coat, "Walk")!, "Y")!;

    private static bool Afoot(object coat) => (bool)Get(Get(coat, "Walk")!, "Afoot")!;

    private static double RangeToCoat(Pages.Map map, object coat)
    {
        double dx = CoatX(coat) - Ax(map), dy = CoatY(coat) - Ay(map);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static string? PulseSaying(Pages.Map map)
    {
        object pulse = Field(map, "_pulse")!;
        return pulse.GetType().GetProperty("Message", Hidden)!.GetValue(pulse) as string;
    }

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

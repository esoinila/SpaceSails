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
/// #1199 / #1062 slice 1 · <b>THE ROOM, THE TAIL AND THE ABSENCE</b>, driven.
///
/// <para>Owner, 2026-09-13: <i>"we shadow somebody into a dead end and once we get there there is nothing
/// there … even better if we preclude the cliché hidden door by having that place be like an observation
/// tube … with only one entry / exit."</i></para>
///
/// <para>The geometry claims are made against the built deck rather than against the numbers that built it,
/// so a carve that moved would not quietly agree with a guard that moved with it. The behavioural ones stand
/// a real page at a real berth and run the frame the way the game runs it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheObservationWalkTests
{
    private const BindingFlags Hidden = TestTree.AnythingAtAll;
    private const string ThreadId = "c81e4a0c39d24e5ba8027c6f1d3e54ff";

    // ── THE ROOM ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · THE WALK IS A REAL, NAMED ROOM — at Selene Gate, and at no other berth in the game. Asked of
    /// the DECK: the plate the map layer stencils, the place name the deck answers with when a body is
    /// standing out at the rail, and the canvas laid under the glass.
    ///
    /// <para>The "nowhere else" half is swept over <see cref="HavenInterior.InteriorBodyIds"/> rather than a
    /// list somebody wrote down, so a ninth haven added next month cannot quietly grow one.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the walk's own clause deleted from the <c>location:</c> lambda —
    /// <i>the deck called the rail "LUNA IMMIGRATION"</i>.</para>
    /// </summary>
    [Fact]
    public void TheWalkIsARealNamedRoomAtSeleneGateAndAtNoOtherBerth()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            bool shouldHaveOne = body == ObservationWalk.HavenId;
            Assert.Equal(shouldHaveOne, HavenInterior.HasObservationWalk(body));

            DeckPlan deck = HavenInterior.DockedDeck(body)!;
            bool plated = deck.RoomLabels.Any(l => l.Text == ObservationWalk.Plate);
            Assert.Equal(shouldHaveOne, plated);

            DeckReachability.Point? rail = HavenInterior.TheRailAt(body);
            Assert.Equal(shouldHaveOne, rail is not null);
            if (!shouldHaveOne)
            {
                continue;
            }

            // The deck's own answer to "which room am I in", at the blind end.
            Assert.Equal(ObservationWalk.Plate, deck.Location(rail!.Value.X, rail.Value.Y));
            Assert.True(HavenInterior.InTheObservationWalk(body, rail.Value.X, rail.Value.Y));

            // …and the glass floor is the drop: one canvas, laid over the walk.
            Assert.Contains(deck.Backdrops, b => b.Url == ObservationWalk.ArtUrl);
        }
    }

    /// <summary>
    /// #1199 · <b>ONE WAY IN AND NO OTHER</b>, counted on the built deck. Every door the plan hangs is
    /// measured against the walk's own box, grown by one doorway's depth so the mouth itself is inside the
    /// count — and exactly one of them is in there.
    ///
    /// <para>This is the room's defining fact and the whole of the precaution against the cliché the owner
    /// named. Anti-vacuity: the count must be exactly one, so a carve that opened nothing (zero) fails as
    /// loudly as one that opened a back door (two).</para>
    ///
    /// <para><b>Revert that reddened it:</b> a second auto-door added across the blind end —
    /// <i>"2 ways into the walk"</i>.</para>
    /// </summary>
    [Fact]
    public void TheWalkHasExactlyOneDoorwayAndNoOther()
    {
        DeckPlan deck = HavenInterior.DockedDeck(ObservationWalk.HavenId)!;
        (double x0, double y0, double x1, double y1) = HavenInterior.TheWalksBox(ObservationWalk.HavenId)!.Value;

        // The room's own box, grown by one body's width so an opening cut in ANY of its four sides — the
        // mouth, either flank, or a back door through the rail — is inside the count. A count that only
        // looked strictly inside would miss the very door this law exists to forbid.
        const double Reach = DeckPlan.AvatarRadius;

        int ways = 0;
        foreach (DeckPlan.Door door in deck.Doors)
        {
            double mx = (door.X1 + door.X2) / 2.0, my = (door.Y1 + door.Y2) / 2.0;
            if (mx < x0 - Reach || mx > x1 + Reach || my < y0 - Reach || my > y1 + Reach)
            {
                continue;
            }

            ways++;
            Assert.False(door.Locked, "the one way into the walk is not a leaf anybody is refused at.");
        }

        Assert.Equal(ObservationWalk.Doorways, ways);
    }

    /// <summary>#1199 · …AND IT IS A PLACE A BODY CAN GET TO AND STAND IN. The rail is standable and the
    /// concourse reaches it on the captain's own lattice, because a dead end nobody can walk into is not a
    /// room, it is a hole in the wall.
    ///
    /// <para><b>Revert that reddened it:</b> the walk's two side walls laid at the ring's vertices instead of
    /// the doorway's jambs — the tube then had no mouth and nothing could route into it.</para></summary>
    [Fact]
    public void TheConcourseReachesTheRail()
    {
        DeckPlan deck = HavenInterior.DockedDeck(ObservationWalk.HavenId)!;
        IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;
        DeckReachability.Point rail = HavenInterior.TheRailAt(ObservationWalk.HavenId)!.Value;

        Assert.False(SurfaceCollision.Blocked(rail.X, rail.Y, DeckPlan.AvatarRadius, walls),
            "nobody can stand at the rail.");

        (double x, double y, _) = HavenInterior.BarThreshold;
        Assert.NotNull(NpcWalk.Plan(
            "◈ SOMEBODY", new NpcWalk.Bound("", rail.X, rail.Y),
            new DeckReachability.Point(x, y), walls, DeckPlan.AvatarRadius, SurfaceCollision.Gait.Person));
    }

    // ── THE TAIL ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1062 slice 1 · <b>THEY LEAD ON WHEN NOBODY IS LOOKING</b>, and then there is nobody there. Driven:
    /// the captain is aboard his own boat with the whole station between them, so the walls answer NO to
    /// every sightline and the notice question never casts a die.
    ///
    /// <para>Three claims: a body got on the floor at all, it WALKED (a one-point route is a teleport with a
    /// plate on it), and it came off the floor at the far end with the wait clock started.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>SendThemOutOntoTheWalk</c> made to return before adding the
    /// walker — <i>"six hundred frames and nobody ever set off"</i>.</para>
    /// </summary>
    [Fact]
    public void TheTailLeadsToTheWalkWhenNobodyNoticesYou()
    {
        DeckReachability.Point rail = HavenInterior.TheRailAt(ObservationWalk.HavenId)!.Value;
        string person = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);
        Pages.Map map = ATailAfoot(person, out int evenings);
        Assert.True(evenings < 48, "the tail is not reachable at Selene Gate at all.");

        DeckReachability.Point? from = null;
        int routePoints = 0;
        for (int i = 0; i < 900 && double.IsNaN(WaitStartedAt(map)); i++)
        {
            RunFrames(map, 1);
            if (ThePersonAfoot(map, person) is not { } who)
            {
                continue;
            }

            object walk = Get(who, "Walk")!;
            var route = (IReadOnlyList<DeckReachability.Point>)Get(walk, "Route")!;
            from ??= route[0];
            routePoints = Math.Max(routePoints, route.Count);
        }

        Assert.True(from is not null, "six hundred frames and nobody ever set off.");
        Assert.True(routePoints > 1, "they were placed rather than walked.");
        Assert.False(Noticed(map), "nobody could see anybody — no look should have clocked the captain.");

        // They are no longer anywhere, and the wait is counting.
        Assert.Null(ThePersonAfoot(map, person));
        Assert.False(double.IsNaN(WaitStartedAt(map)), "they went, and the wait never started.");

        // …and where they went is the rail: the last place the sim had them.
        Assert.True(HavenInterior.InTheObservationWalk(ObservationWalk.HavenId, rail.X, rail.Y));
    }

    /// <summary>
    /// #1062 slice 1 · <b>THEY STOP, TURN, AND WAIT FOR YOU TO GO PAST.</b> The captain is stood in the
    /// concourse in plain sight of them and does not move — which the observation roll prices as the one
    /// thing that is NOT cover at close range, because a man standing still behind you is the one a person
    /// notices.
    ///
    /// <para>Two claims, and the second is the one that matters: they are clocked, and the body then STOPS —
    /// its position does not change again while the captain stands there. And nothing is said: no card, no
    /// pulse, nothing filed.</para>
    ///
    /// <para><b>Revert that reddened it:</b> the <c>holding</c> branch of
    /// <c>StepThePersonOfInterest</c> deleted so a noticed person walks on anyway — <i>"they were clocked and
    /// kept walking"</i>.</para>
    /// </summary>
    [Fact]
    public void TheyStopAndWaitWhenTheyNoticeYou()
    {
        string person = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);

        // Get them on the floor first, from a place with no sightline at all.
        Pages.Map map = ATailAfoot(person, out _);

        object who = ThePersonAfoot(map, person)
            ?? throw new InvalidOperationException("nobody got on the floor to be noticed.");
        object walk = Get(who, "Walk")!;

        // …then stand right behind them, in the open, and stay there. Pinned three du off their back every
        // frame rather than dropped once: the point of the law is a man who stops for somebody ON HIS HEELS,
        // and a captain left standing where they used to be is a captain thirty du back by the time the
        // notice latches.
        //
        // #1199 (2026-09-19) · …AND ON THE CONCOURSE, which is where standing aside means anything. Inside
        // the walk he does not hold at all — there is nothing for the captain to be let past, the room has
        // one end, and a man who stopped there would stop for ever (the owner's own stall, twice watched).
        for (int i = 0; i < 400 && !Noticed(map); i++)
        {
            object onFoot = ThePersonAfoot(map, person)
                ?? throw new InvalidOperationException("they came off the floor while the captain was looking.");
            object theirLegs = Get(onFoot, "Walk")!;
            StandCaptainAt(map, (double)Get(theirLegs, "X")! + 3.0, (double)Get(theirLegs, "Y")!);
            RunFrames(map, 1);
        }

        Assert.True(Noticed(map), "four hundred looks at a man standing still three deck units behind them.");

        object still = ThePersonAfoot(map, person)
            ?? throw new InvalidOperationException("they came off the floor while the captain was looking.");
        object theirWalk = Get(still, "Walk")!;
        double x = (double)Get(theirWalk, "X")!, y = (double)Get(theirWalk, "Y")!;
        Assert.False(
            SpaceSails.Client.Rendering.HavenInterior.InTheObservationWalk(ObservationWalk.HavenId, x, y),
            "they were noticed inside the walk itself, where this law does not apply — the bench has to catch "
            + "them on the concourse for it to be about anything.");
        StandCaptainAt(map, x + 3.0, y);

        RunFrames(map, 60);

        object after = ThePersonAfoot(map, person)
            ?? throw new InvalidOperationException("they vanished in plain sight — the one thing they must not do.");
        object afterWalk = Get(after, "Walk")!;
        Assert.Equal(x, (double)Get(afterWalk, "X")!, 9);
        Assert.Equal(y, (double)Get(afterWalk, "Y")!, 9);
        Assert.Equal("LettingYouPass", Get(after, "For")!.ToString());

        // NOTHING IS SAID. The inference is the whole telling.
        Assert.Null(Field(map, "_storyCard"));
        Assert.Null(Field(map, "_observationWalkSpentOn"));
        Assert.Empty((IEnumerable<FieldNote>)Field(map, "_fieldNotes")!);
    }

    // ── THE BEAT ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>THE CARD FIRES AT THE BLIND END, ONCE, AND THE BOOK FILES THE NOTE.</b> The captain waits
    /// out a whole watch-fraction, walks in, and goes all the way to the rail.
    ///
    /// <para>Four claims: the card is the beat's own (<see cref="StoryBeats.Beat.TheObservationWalk"/>), the
    /// spend is written, the book has the authored note filed under the person, and a second walk out to the
    /// rail raises nothing at all.</para>
    ///
    /// <para><b>Revert that reddened it:</b> <c>WouldSpend</c> ignoring <c>spentOn</c> — the card came back
    /// the second time the captain stood at the rail.</para>
    /// </summary>
    [Fact]
    public void TheCardFiresAtTheBlindEndAndTheBookFilesTheNote()
    {
        string person = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);
        Pages.Map map = ATailAfoot(person, out _);

        for (int i = 0; i < 900 && double.IsNaN(WaitStartedAt(map)); i++)
        {
            RunFrames(map, 1);
        }

        Assert.False(double.IsNaN(WaitStartedAt(map)), "nobody ever went, so there is nothing to walk in on.");

        // Standing at the mouth is not the beat: the captain has to go and look.
        DeckReachability.Point mouth = HavenInterior.TheWalksMouthAt(ObservationWalk.HavenId)!.Value;
        StandCaptainAt(map, mouth.X, mouth.Y);
        Set(map, "SimTime", (double)Field(map, "SimTime")! + ObservationWalk.TheWaitSeconds + 1);
        RunFrames(map, 2);
        Assert.Null(Field(map, "_storyCard"));

        // …and then he walks out to the rail.
        DeckReachability.Point rail = HavenInterior.TheRailAt(ObservationWalk.HavenId)!.Value;
        StandCaptainAt(map, rail.X, rail.Y);
        RunFrames(map, 2);

        object card = Field(map, "_storyCard")
            ?? throw new InvalidOperationException("the captain stood at the rail of an empty walk and nothing happened.");
        Assert.Contains("TheObservationWalk", card.ToString(), StringComparison.Ordinal);

        Assert.Equal(ObservationWalk.Key(ObservationWalk.HavenId, person), Field(map, "_observationWalkSpentOn"));

        var book = (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!;
        FieldNote note = Assert.Single(book);
        Assert.Equal(ObservationWalk.NoteLine(person), note.Text);
        Assert.Equal(ObservationWalk.Subjects(person), note.Subjects);
        Assert.Equal(ObservationWalk.Glyph, note.Glyph);

        // ONCE. Close it, walk out and back, and the walk is a walk.
        Invoke(map, "CloseStoryCard");
        Assert.Null(Field(map, "_storyCard"));
        RunFrames(map, 60);
        Assert.Null(Field(map, "_storyCard"));
        Assert.Single(book = (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!);
    }

    /// <summary>
    /// #1199 · <b>AND LATER, AT A COUNTER, THERE THEY ARE — ONCE.</b> The pulse is owed after the spend and
    /// is paid at the first berth the person's own rota would have put them at anyway.
    ///
    /// <para><b>Revert that reddened it:</b> <c>_observationWalkSightingAt</c> left unwritten — the line
    /// played again on the very next frame, and then for ever.</para>
    /// </summary>
    [Fact]
    public void ThePulseAtTheCounterFiresOnceAndNeverAgain()
    {
        string person = TheTail.ThePersonOfInterest(ObservationWalk.HavenId);

        // Somewhere else in the system, with the beat already spent and the sighting still owed.
        string elsewhere = HavenInterior.InteriorBodyIds
            .First(b => b != ObservationWalk.HavenId
                        && PatronRota.Resolve(person, b, 0) == PatronState.AtBar);

        Pages.Map map = AshoreAt(elsewhere);
        PastLastCall(map);
        Set(map, "_observationWalkSpentOn", ObservationWalk.Key(ObservationWalk.HavenId, person));
        StandCaptainAt(map, 2.5, 6);

        RunFrames(map, 1);

        Assert.Equal(elsewhere, Field(map, "_observationWalkSightingAt"));
        Assert.Equal(ObservationWalk.CounterLine(person), PulseSaying(map));

        // …and never again. The HUD is wiped and a hundred frames say nothing back into it.
        Set(map, "_pulse", default(PulseSlot));
        RunFrames(map, 100);
        Assert.Null(PulseSaying(map));
    }

    // ── PLUMBING ─────────────────────────────────────────────────────────────────────────────────────────

    private static Pages.Map AshoreAt(string berth, long watch = 0)
    {
        var map = new Pages.Map();
        Set(map, "SimTime", watch * PatronRota.WatchSeconds);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", (bool?)false);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
        return map;
    }

    /// <summary>#731 · Wind the room's clock past LAST CALL, which is where the walk is walked from: nobody
    /// gets out of a chair in this bar before the shift says so, and the point after which nothing is
    /// scheduled to happen any more is the room's own <see cref="Egress.LastCallFraction"/>. Every test below
    /// that wants somebody on their feet has to get there honestly.</summary>
    private static void PastLastCall(Pages.Map map)
    {
        var watch = (long)Invoke(map, "get_BarWatch")!;
        Set(map, "SimTime",
            (watch * PatronRota.WatchSeconds) + (PatronRota.WatchSeconds * Egress.LastCallFraction) + 1);
    }

    /// <summary>
    /// #1199 · A PAGE WITH THE TAIL ACTUALLY AFOOT, found by walking the station's own evenings until one of
    /// them produces it — never by picking a watch that happens to work.
    ///
    /// <para>The tail does not happen every evening and must not: the rota has to have the person in the room
    /// this watch, and the room's own hours must not already have walked them out through a leaf (one body,
    /// one place). Both are facts about a watch, so the honest thing for a guard to do is sweep watches and
    /// assert that the beat is REACHABLE — which is itself the claim that matters, and goes red if the tail
    /// becomes impossible.</para>
    /// </summary>
    private static Pages.Map ATailAfoot(string person, out int evenings)
    {
        for (evenings = 0; evenings < 48; evenings++)
        {
            Pages.Map map = AshoreAt(ObservationWalk.HavenId, evenings);
            PastLastCall(map);
            StandCaptainAt(map, 2.5, 6);   // aboard, in the airlock corridor: no line to anything ashore

            for (int i = 0; i < 40; i++)
            {
                RunFrames(map, 1);
                if (ThePersonAfoot(map, person) is { } who
                    && Get(who, "For")!.ToString() == "WalkingTheRoute")
                {
                    return map;
                }
            }
        }

        throw new InvalidOperationException(
            "forty-eight evenings at Selene Gate and the tail never once set off — the beat is unreachable.");
    }

    /// <summary>Put the captain somewhere, and tell the motion rule he has been there a while — so a
    /// placement is never read as a sprint (#436's own <c>TeleportSpeedDu</c> clause).</summary>
    private static void StandCaptainAt(Pages.Map map, double x, double y)
    {
        Set(map, "_avatarX", x);
        Set(map, "_avatarY", y);
        Set(map, "_lookPrevAvatarX", x);
        Set(map, "_lookPrevAvatarY", y);
    }

    /// <summary>One frame the way the game runs it — and BOTH clocks, because the look cadence is measured in
    /// real seconds off the frame stamp while the room's own hours are measured in sim seconds. A harness
    /// that advanced only one of them would freeze the notice question at a single look.</summary>
    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Set(map, "_lastTimestampMs", (double?)(((double?)Field(map, "_lastTimestampMs") ?? 0) + (dt * 1000)));
            Invoke(map, "AdvanceBarWalkers", dt);
        }
    }

    private static IList BarAfoot(Pages.Map map) => (IList)Field(map, "_barAfoot")!;

    private static object? ThePersonAfoot(Pages.Map map, string person)
    {
        foreach (object who in BarAfoot(map))
        {
            if (string.Equals((string)Get(who, "Who")!, person, StringComparison.Ordinal))
            {
                return who;
            }
        }

        return null;
    }

    private static bool Noticed(Pages.Map map) => (bool)Field(map, "_walkNoticed")!;

    private static double WaitStartedAt(Pages.Map map) => (double)Field(map, "_walkGoneSince")!;

    private static string? PulseSaying(Pages.Map map)
    {
        object pulse = Field(map, "_pulse")!;
        object? said = pulse.GetType().GetProperty("Message", Hidden)!.GetValue(pulse);
        return said as string;
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

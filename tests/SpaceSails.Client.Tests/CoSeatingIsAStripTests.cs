using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #865 · ONE STRIP, WITH COMPANY — sitting down is a docked moment, co-seating is the same strip with the
/// social moves added, and nothing modal covers the chair being taken.
///
/// <para>Owner, 2026-08-13, sitting at a canteen table on B1: <i>"I sat down the table but the pop ups
/// blocked my view of my avatar sitting down. I kind of like the wait and keep sitting option we have
/// elsewhere…"</i> — then, on the retry, with the docked strip up and the dot visibly on the chair: <i>"Oh
/// now it picked the chair and it worked."</i> And, of the two attempts together: <i>"That was different than
/// last time."</i></para>
///
/// <para>Then, at the decisive case — asked to join a WEIGHBRIDGE CLERK ON A BREAK and given the full modal
/// takeover, backdrop dim, whole hall invisible: <i>"This … is what I mean … see now … what if I just sit and
/// eat here… now I am kind of blinded of the surrounding here because somebody else sits in the same
/// table"</i>. Then the target: <i>"It should somehow UI wise be same style as the sitting alone case."</i>
/// And the seal: <i>"Just the social functions as additional options."</i></para>
///
/// <h3>What raced</h3>
///
/// <para>The first-entry cards down here are POSITION POLLS. <c>CheckCantinaHallUnderfoot</c> and its three
/// siblings run every surface tick, ask whether the captain's coordinates are inside a room that still owes a
/// card, and raise a centred <c>_viewObject</c> when they are. <b>#820's snap writes those coordinates</b>:
/// pressing [E] at a top puts the dot on the chair, so the tick after a sit is exactly when the hall's card
/// came up — over the half-second the snap exists to be watched. Every one of those latches is one-shot per
/// excursion, so it could only ever happen ONCE: the first sit of a session wore a card and the second wore
/// the strip. From the chair, that is nondeterminism, and the owner read it as exactly that.</para>
///
/// <h3>Why these guards are driven and not read</h3>
///
/// <para>Every fact below is asked of a REAL <see cref="Pages.Map"/> on a REAL generated floor, the harness
/// <c>MustStandUpBeforeWalkingTests</c> established one file over: the seats are taken with the shipping
/// verbs, the frames are spent in the shipping <c>StepSurface</c>, and the story beats go through the
/// shipping <c>RaiseStoryBeat</c>. A guard that read Map.razor for the word <c>seated-dock</c> would pass on a
/// build where a card came up on top of it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class CoSeatingIsAStripTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to sit down on.");

    /// <summary>How long one watched frame is. Short on purpose: the law is about what the player SEES on
    /// the frames between the press and the strip's first draw, and a guard that stepped a second at a time
    /// would step straight over them.</summary>
    private const double FrameSeconds = 0.05;

    /// <summary>How many frames are INSIDE the sit beat — deliberately one short of spending it. The frame
    /// that pays out the last of the debt is the frame the room is allowed to speak on again, and a guard
    /// that asserted silence through it would be asserting that the beat never ends. Derived from
    /// <see cref="SeatedPosture.SitBeatSeconds"/> so the owner can tune the beat without this file
    /// developing an opinion about it.</summary>
    private static int BeatFrames =>
        (int)Math.Floor((SeatedPosture.SitBeatSeconds - FrameSeconds) / FrameSeconds);

    // ── THE FLOOR ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor, with nothing running but the deck. The one piece of
    /// theatre is the render handle — see the docblock on <c>MustStandUpBeforeWalkingTests.OnTheFloor</c>,
    /// which this is the same bench as.</summary>
    private static Pages.Map OnTheFloor()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the seat verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, TheFloor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_fpMode", false);

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    // ── THE SEATS, TAKEN WITH THE GAME'S OWN VERBS ────────────────────────────────────────────────────

    private sealed record Seat(string Name, Action<Pages.Map> SitDown);

    /// <summary>Every seat kind the sit beat has to hold for. The joined top is LAST and is the one this
    /// issue is named after — it is also the only one of the five that used to raise a card of its own.</summary>
    private static IReadOnlyList<Seat> EverySeatKind() =>
    [
        new("a stool at the counter", m => Invoke(m, "TakeAStool")),
        new("a park bench", m => TakeSeat(m, DeckPlan.ConsoleKind.HiveBench, "TryTakeBench")),
        new("an office chair", m => TakeSeat(m, DeckPlan.ConsoleKind.HiveOfficeChair, "TryTakeOfficeChair")),
        new("a free canteen top", m => TakeSeat(m, DeckPlan.ConsoleKind.HiveTable, "TryTakeTable")),
        new("a top somebody else is already at", JoinAnOccupiedTop),
    ];

    private static void TakeSeat(Pages.Map map, DeckPlan.ConsoleKind kind, string verb)
    {
        StandAtAConsole(map, kind);
        Assert.True((bool)Invoke(map, verb)!, $"the press at the {kind} was not taken.");
        Assert.True(Get(map, "_table") is not null, $"nobody sat down at the {kind}.");
    }

    private static void StandAtAConsole(Pages.Map map, DeckPlan.ConsoleKind kind)
    {
        DeckPlan.ConsoleSpot[] spots = [.. ThePlan(map).Consoles.Where(c => c.Kind == kind)];
        Assert.True(spots.Length > 0,
            $"this floor carves no {kind} — the guard below would be asserting about a seat that is not there.");
        Set(map, "_avatarX", (double)spots[0].X);
        Set(map, "_avatarY", (double)spots[0].Y);
    }

    /// <summary>
    /// #865 · SIT DOWN AT A TOP SOMEBODY IS ALREADY AT — the weighbridge clerk's own case, taken with the
    /// press a captain takes it with.
    ///
    /// <para>Every HiveRegular console on the floor is tried in turn, because not every one of them is a
    /// table you may join: #709's cast keep their one breath (Who.None, no scene at all) and a top with no
    /// chair left refuses out loud (#842). The first press that actually seats the captain is the case.</para>
    /// </summary>
    private static void JoinAnOccupiedTop(Pages.Map map)
    {
        foreach (DeckPlan.ConsoleSpot spot in
            ThePlan(map).Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular))
        {
            Set(map, "_avatarX", (double)spot.X);
            Set(map, "_avatarY", (double)spot.Y);
            Invoke(map, "TryOpenTable");
            if (Get(map, "_table") is not null)
            {
                return;
            }
        }

        Assert.Fail(
            "no HiveRegular console on this floor seats the captain at all — every press either found " +
            "somebody who is not a scene or a top with no chair left, so the co-seating guards below would " +
            "be asserting about a state the game cannot reach.");
    }

    // ── (a) NOTHING MODAL COVERS THE SIT BEAT, AT ANY SEAT KIND ───────────────────────────────────────

    /// <summary>
    /// #865 · THE CAPTAIN GETS TO WATCH THEMSELVES TAKE THE CHAIR — at every seat in the game.
    ///
    /// <para>Owner: <i>"I sat down the table but the pop ups blocked my view of my avatar sitting down."</i>
    /// So: from the press until the sit beat has run out, no centred card of any family is on the screen. The
    /// frames are the shipping surface tick, which is where the first-entry polls live and which is therefore
    /// the only place this can be honestly asked.</para>
    ///
    /// <para>Five seats, because the law is about a POSTURE and not about a room, and because the fifth one —
    /// the top somebody else is at — is the one that used to raise a card by itself.</para>
    /// </summary>
    [Fact]
    public void NO_CARD_ComesUpOverTheChairBeingTaken()
    {
        foreach (Seat seat in EverySeatKind())
        {
            Pages.Map map = OnTheFloor();
            seat.SitDown(map);

            for (int frame = 0; frame < BeatFrames; frame++)
            {
                Invoke(map, "StepSurface", FrameSeconds);

                Assert.True(Get(map, "_viewObject") is null,
                    $"a card came up over the captain taking {seat.Name}, {(frame + 1) * FrameSeconds:0.00}s " +
                    "after the press — the snap onto the seat is the whole payoff of #820/#846 and the " +
                    "player never saw it happen.");
                Assert.True(Get(map, "_storyCard") is null,
                    $"a story card took the screen while the captain was sitting down at {seat.Name}.");
            }
        }
    }

    /// <summary>
    /// #865 · …AND THE CARD THAT WAS WAITING RAISES AFTERWARDS. This is the half that keeps the guard above
    /// from being a test that asserts nothing: it proves a card was genuinely DUE on those frames and that
    /// what held it was the beat.
    ///
    /// <para>The room's own hall card is the one used, because it is the card that actually did this: its
    /// poll reads the captain's coordinates, and sitting down is what writes them. The latch is checked
    /// unspent before the sit and spent after the beat, so a floor that stopped owing the card would fail
    /// this guard loudly instead of passing it silently.</para>
    ///
    /// <para><b>The RED case.</b> Take the sit-beat gate off the four polls in <c>StepSurface</c> and the
    /// first assertion in the loop below fires on frame one, with the card up over the chair.</para>
    /// </summary>
    [Fact]
    public void THE_HELD_CARD_IsDelayedAndNotDropped()
    {
        Pages.Map map = OnTheFloor();
        object ex = Get(map, "_surface")!;

        Assert.False(TheHallCardIsSpent(ex),
            "the hall's first-entry card is already spent on a fresh excursion — this guard would be " +
            "watching for a card that was never coming.");

        TakeSeat(map, DeckPlan.ConsoleKind.HiveTable, "TryTakeTable");

        for (int frame = 0; frame < BeatFrames; frame++)
        {
            Invoke(map, "StepSurface", FrameSeconds);
            Assert.True(Get(map, "_viewObject") is null,
                $"the hall's card came up {(frame + 1) * FrameSeconds:0.00}s after the captain sat down — " +
                "the poll reads the coordinates #820's snap has just written, so sitting at a table is " +
                "itself what raises it, over the one beat the sit exists to be seen in.");
        }

        // …and now it lands, on a frame where the strip is up and the captain is visibly in the chair.
        for (int frame = 0; frame < 40 && Get(map, "_viewObject") is null; frame++)
        {
            Invoke(map, "StepSurface", FrameSeconds);
        }

        Assert.True(Get(map, "_viewObject") is not null && TheHallCardIsSpent(ex),
            "the sit beat did not delay the hall's card, it SWALLOWED it — a room the captain has walked " +
            "into now owes a card that will never be raised, which is a worse bug than the one this issue " +
            "was filed on.");

        // …and the strip is still under it. The rule's second half: the seated scene is established by the
        // time anything is allowed on top of it.
        Assert.True((bool)Get(map, "SeatedIsDocked")!,
            "the deferred card raised onto a captain who is no longer sitting down.");
    }

    // ── (b) CO-SEATING IS A STRIP STATE ───────────────────────────────────────────────────────────────

    /// <summary>
    /// #865 · JOINING AN OCCUPIED TOP LEAVES THE ROOM VISIBLE, and puts the neighbour's moves in the strip.
    ///
    /// <para>Owner, at the weighbridge clerk's table: <i>"what if I just sit and eat here… now I am kind of
    /// blinded of the surrounding here because somebody else sits in the same table"</i> — and the target,
    /// one comment later: <i>"It should somehow UI wise be same style as the sitting alone case."</i></para>
    ///
    /// <para>Four facts, and each can fail on its own:</para>
    /// <list type="number">
    /// <item>the frame is the DOCKED one — which is the razor's own backdrop predicate, so no scrim is
    /// written into the DOM at all;</item>
    /// <item>their moves are reachable AS STRIP BUTTONS — Core's own three (small talk, the round, the
    /// leave), asked through the same call the strip's button row renders;</item>
    /// <item>the room is still drawing — the deck the strip is docked over still carries this floor's
    /// fixtures and the people in them;</item>
    /// <item>the strip still carries every law it had alone — the customer line, and the spread REFUSING out
    /// loud, which is the privacy ladder working identically with a stranger opposite.</item>
    /// </list>
    ///
    /// <para><b>The RED case.</b> Put the frame law back on <c>Solo</c> and (1) goes red: the joined top has
    /// <c>Solo == false</c>, which is what raised the takeover.</para>
    /// </summary>
    [Fact]
    public void CO_SEATING_IsTheSameStripWithTheSocialMovesAdded()
    {
        Pages.Map map = OnTheFloor();
        int fixturesBefore = ThePlan(map).Consoles.Count();

        JoinAnOccupiedTop(map);

        // (1) THE FRAME. The razor forks on these two and nothing else, so this IS the absence of the scrim.
        Assert.True((bool)Get(map, "SeatedIsDocked")!,
            "joining an occupied top did not dock the frame — the captain is sitting at a table with a " +
            "backdrop over the whole hall, which is the screenshot on the issue.");
        Assert.False((bool)Get(map, "SeatedIsAConversation")!,
            "the joined top still counts as a conversation, so the card branch and its backdrop render.");

        // …and the occupancy fact is UNCHANGED, which is the distinction the fix rests on: somebody IS in
        // the chair opposite. A frame law that had simply been forced true would take this with it.
        object sitting = Get(map, "_table")!;
        Assert.False((bool)sitting.GetType().GetProperty("Solo")!.GetValue(sitting)!,
            "the joined top reports the captain as sitting ALONE — the frame was fixed by lying about the " +
            "room, and the privacy ladder now licenses a case spread across a stranger's table.");

        // (2) THEIR MOVES, in the strip's own button row. Core's list, through the client's own call.
        var moves = (IReadOnlyList<Encounter.Move>)Invoke(map, "TableMovesOnTheTable")!;
        string[] ids = [.. moves.Select(m => m.Id)];
        foreach (string social in new[] { CanteenTable.SmallTalk, CanteenTable.Round, CanteenTable.Leave })
        {
            Assert.True(ids.Contains(social),
                $"the strip offers no `{social}` at a top you are sharing — the owner's ruling is 'just the " +
                $"social functions as additional options', and this one is not on the row. Offered: " +
                $"{string.Join(", ", ids)}");
        }

        // (3) THE ROOM IS STILL DRAWING. The strip docks over a live deck; the card replaced it with a scrim.
        Assert.Equal(fixturesBefore, ThePlan(map).Consoles.Count());
        Assert.True(ThePlan(map).Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.HiveRegular),
            "the people are gone off the deck the strip is docked over.");

        // (4) EVERY LAW THE STRIP ALREADY CARRIED, working identically with company at the top.
        Assert.True(Invoke(map, "SeatedCustomerLine") is string { Length: > 0 },
            "the shared top draws no customer line at all — this is a second component after all.");
        Assert.False((bool)Get(map, "CanSpreadTheCaseHere")!,
            "the case may be spread out on a top a stranger is eating at. The privacy ladder reads Solo, " +
            "and it has to go on reading it whatever the frame does.");
        Assert.True(Get(map, "SpreadRefusal") is string { Length: > 0 },
            "…and it refuses in silence, which #603 forbids.");

        // …and the company is SAID. The plate and the bark are the strip's own lines now (the owner's
        // "the neighbour's plate and bark folded in"), not something a card was carrying.
        Assert.True(Invoke(map, "SeatedCompanyLine") is string { Length: > 0 },
            "nothing on the strip says who the captain is sharing the top with.");
    }

    // ── (c) ONE VERB, ONE SHAPE, WHATEVER IS IN THE QUEUE ─────────────────────────────────────────────

    /// <summary>
    /// #865 · THE SAME SIT PRESENTS THE SAME WAY EVERY TIME. Owner: <i>"That was different than last
    /// time."</i>
    ///
    /// <para>The nondeterminism was never in the sit verb: it was in what the WORLD happened to have unspent
    /// at the moment of the press. So the same press is driven under four different queue states — nothing
    /// pending, a story card raised on the sit frame, the room's first-entry latches unspent, and the same
    /// latches already spent — and the presentation is asserted identical in all four.</para>
    ///
    /// <para><b>The RED case.</b> Take the sit-beat arm out of <c>RaiseStoryBeat</c> and the story-card state
    /// diverges from the other three on the very first frame.</para>
    /// </summary>
    [Fact]
    public void THE_SAME_SIT_PresentsTheSameWayUnderEveryQueueState()
    {
        var shapes = new List<(string World, string Shape)>();

        foreach ((string world, Action<Pages.Map> before, Action<Pages.Map> after) in QueueStates())
        {
            Pages.Map map = OnTheFloor();
            before(map);

            TakeSeat(map, DeckPlan.ConsoleKind.HiveTable, "TryTakeTable");
            after(map);

            // The shape of the presentation, frame by frame, for the whole beat. A string on purpose: what
            // the owner compared between his two attempts was what the screen LOOKED LIKE, and this is that
            // in the smallest honest form — which frame had a strip on it, and which had a card.
            var shape = new List<string>();
            for (int frame = 0; frame < BeatFrames; frame++)
            {
                Invoke(map, "StepSurface", FrameSeconds);
                shape.Add(
                    (bool)Get(map, "SeatedIsDocked")! ? "strip"
                    : Get(map, "_table") is null ? "standing"
                    : "card");
                if (Get(map, "_viewObject") is not null || Get(map, "_storyCard") is not null)
                {
                    shape[^1] += "+MODAL";
                }
            }

            shapes.Add((world, string.Join(" ", shape)));
        }

        string first = shapes[0].Shape;
        Assert.DoesNotContain("MODAL", first, StringComparison.Ordinal);
        foreach ((string world, string shape) in shapes)
        {
            Assert.True(string.Equals(shape, first, StringComparison.Ordinal),
                $"the same sit presented two different ways. With {shapes[0].World} the frames after the " +
                $"press read:\n  {first}\nand with {world} they read:\n  {shape}\nOne verb has to have one " +
                "shape, whatever the world happened to have unspent when the captain pressed E.");
        }
    }

    /// <summary>The four worlds the same press is driven in. Each is a thing that could genuinely be true at
    /// the moment a captain sits down, and none of them is allowed to change what sitting down looks
    /// like.</summary>
    private static IEnumerable<(string World, Action<Pages.Map> Before, Action<Pages.Map> After)> QueueStates()
    {
        yield return ("nothing pending", NothingPending, _ => { });
        yield return ("a story card raised on the sit frame", NothingPending,
            // #664 · the seam's third parameter is #736's outcome; reflection does not fill optional
            // arguments, and a crew deputation settles nothing arithmetical, so it goes in as null.
            m => Invoke(m, "RaiseStoryBeat", StoryBeats.Beat.CrewDeputation, null, null));
        yield return ("the room's first-entry cards unspent", _ => { }, _ => { });
        yield return ("the room's first-entry cards already spent", NothingPending, _ => { });
    }

    /// <summary>Spend the room's own first-entry latches, so a world with nothing left to raise can be
    /// compared against one that has everything.</summary>
    private static void NothingPending(Pages.Map map)
    {
        object ex = Get(map, "_surface")!;
        ex.GetType().GetProperty("HiveCantinaHallShown")!.SetValue(ex, true);
        ex.GetType().GetProperty("HiveCabinetShown")!.SetValue(ex, true);
    }

    // ── (d) THE ROUND STOPPING AT YOU KEEPS ITS CARD ──────────────────────────────────────────────────

    /// <summary>
    /// #865 · SOMEBODY WHO COMES TO YOU IS STILL AN ENCOUNTER. The owner narrowed the card, he did not
    /// abolish it: <i>"The modal card keeps only what genuinely needs focus: the round stopping at you (the
    /// guard's face IS the point), story/outcome cards, the named cast's deep scenes."</i>
    ///
    /// <para>So the line is WHO CHOSE IT. A seat the captain took is a posture change and docks; a stranger
    /// who crosses the hall and takes the chair opposite has arrived, and her arrival raises the card over a
    /// room the player was already watching — which is exactly the seam #784 wrote the approach beat for.</para>
    ///
    /// <para>It is driven through the shipping wait beat with #757's own cheat forcing the approach, so the
    /// scene that comes up is the one a captain would get.</para>
    ///
    /// <para><b>#731 · AND SHE WALKS THERE NOW, so the guard waits for her.</b> The rule below is untouched
    /// and is the whole point of this row: somebody who comes to YOU raises the card. What changed is the
    /// sentence in front of it — the beat no longer makes her true where she stands, it sends her out of a
    /// door that does not open for the captain and across the real floor, and the card comes up on the frame
    /// she reaches the chair. So the frames between are SPENT, in the shipping surface tick, and then the same
    /// three assertions are made. A guard that still asked on the press frame would be asserting that the walk
    /// does not exist.</para>
    /// </summary>
    [Fact]
    public void SOMEBODY_WHO_COMES_TO_YOU_StillGetsTheCard()
    {
        Pages.Map map = OnTheFloor();
        TakeSeat(map, DeckPlan.ConsoleKind.HiveTable, "TryTakeTable");

        Assert.True((bool)Get(map, "SeatedIsDocked")!, "the sit did not dock in the first place.");

        // #757's own force-the-approach cheat: WHETHER, never who or what.
        Set(map, "_approachCheat", (bool?)true);
        Invoke(map, "TableMove", SittingAlone.Wait);

        // #731 · …and now she crosses the room. Whether she WALKS at all, over what, and with what said about
        // it, is TheExitIsTheFullStopTests' business; this row only needs her to have arrived before it asks
        // #865's question. A floor with no locked door to come out of has no walk to spend and the loop is a
        // no-op, which is the same honest fallback the arrival itself takes.
        for (int frame = 0; frame < 4000 && !(bool)Get(map, "SeatedIsAConversation")!; frame++)
        {
            Invoke(map, "StepSurface", 0.1);
        }

        Assert.True((bool)Get(map, "SeatedIsAConversation")!,
            "somebody crossed the hall, took the chair opposite and got a HUD strip. Her face is the point " +
            "of the beat; the card is what a face is for, and #865 narrowed the card to exactly this.");
        Assert.False((bool)Get(map, "SeatedIsDocked")!,
            "the strip and the card are both up — the frame forks on one question and it has two answers.");

        // …and she goes, and the frame comes back DOWN to the strip. One occupation of one table.
        Invoke(map, "TableMove", SittingAlone.WaveOff);
        Assert.True((bool)Get(map, "SeatedIsDocked")!,
            "she was waved off and the card stayed up over the room.");
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static bool TheHallCardIsSpent(object ex) =>
        (bool)ex.GetType().GetProperty("HiveCantinaHallShown")!.GetValue(ex)!
        || (bool)ex.GetType().GetProperty("HiveCabinetShown")!.GetValue(ex)!;

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    /// <summary>A field OR a property, by name — the seated frame law is a property and the state it reads
    /// is a field, and a guard about one law should not care which the author chose.</summary>
    /// <summary>#870 lane 6b - ...nor where the author put it: the five seat fields live on the page's
    /// <c>_seating</c> object now, so the lookup follows them there (<see cref="SeatState"/>) and every
    /// assertion below still asks for the state by the name it was written with.</summary>
    private static object? Get(object o, string name)
    {
        if (SeatState.TryFollow(o, name, out object? seated))
        {
            return seated;
        }

        FieldInfo? field = o.GetType().GetField(name, Hidden);
        if (field is not null)
        {
            return field.GetValue(o);
        }
        PropertyInfo? prop = o.GetType().GetProperty(name, Hidden);
        Assert.True(prop is not null, $"the component has no `{name}` — this guard is reading a dead name.");
        return prop!.GetValue(o);
    }

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    /// <summary>#870 lane 6c · THE LOOKUP FOLLOWS THE VERB, and not one assertion moved. The seat's verbs
    /// live on <c>Map.Seating</c> now, behind <c>ISeatHost</c>, and the page keeps a forwarder only where
    /// something outside the seat family still calls one by name. So a verb that is not on the page is asked
    /// of the seat object hanging off it — the same live page, the same real handler, the same arguments.
    /// It is deliberately NOT a fallback for any name: a verb that has simply been DELETED still fails
    /// loudly right here, which is what keeps this harness's anti-vacuous property.</summary>
    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        object target = map;
        if (call is null && SeatState.Seat(map) is { } seat)
        {
            call = seat.GetType().GetMethod(method, Hidden);
            target = seat;
        }
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(target, args);
    }
}

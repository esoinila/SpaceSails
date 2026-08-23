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
/// #973 L5b · THE EIGHTH SEAT, AND THE WOMAN WHO CROSSES THE ROOM TO IT.
///
/// <para>#973 L0 shipped a bar with a floor in it and wrote down what was still missing: <i>"There is no way
/// to sit down in a docked bar. Every seat in this game is opened through <c>Seating.TakeThisSeat</c>, all
/// seven sites of it are gated on a <c>SurfaceExcursion</c>, and a docked berth has none — the bar's seven
/// tops are drawn dressing with no chairs and no console."</i> Its own approach hook therefore answered the
/// honest thing forever: nobody is sitting alone, so nobody is crossed to.</para>
///
/// <para>Everything below is that sentence being paid off, driven end to end on a real page at a real berth:
/// a top that answers [E], a sitting opened through the ONE method, a predicate that starts telling the truth,
/// and a woman who comes out of a leaf, crosses the floor and asks for something found.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheEighthSeatIsInTheDockedBarTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The classy great-port tier, and the one the owner drinks in.</summary>
    private const string TheRedEye = "red-eye";

    /// <summary>A second berth, so the job she asks for has somewhere in the world to point at.</summary>
    private const string TheOtherPort = "ringside-exchange";

    private const string ThreadId = "5c1e90fb2a7d41c6b3f80e274d9a6153";

    // ── THE ROOM OFFERS A CHAIR ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY BAR IN THE GAME HAS A TOP THE CAPTAIN CAN TAKE — claimed over
    /// <see cref="HavenInterior.InteriorBodyIds"/>, so a ninth haven added next month either grows a seat or
    /// turns this red rather than quietly shipping a room you can only stand in.
    ///
    /// <para>And every one of those consoles is <b>on</b> one of the room's own published tops, never
    /// somewhere a table is not: the console the press lands on and the table the walker is sent to have to
    /// be the same piece of furniture, or the captain sits down at a top nobody can cross to.</para>
    /// </summary>
    [Fact]
    public void EveryBarPublishesATopTheCaptainCanTake()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            DeckPlan deck = HavenInterior.DockedDeck(body)!;
            IReadOnlyList<DeckReachability.Point> tops = HavenInterior.BarBand(body)!.Value.Tops;

            DeckPlan.ConsoleSpot[] takeable =
                [.. deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.BarTop)];

            Assert.True(takeable.Length > 0,
                        $"{body}'s bar has {tops.Count} tops and not one of them can be taken — [E] there "
                        + "answers nothing, which is an absence and not a refusal.");

            foreach (DeckPlan.ConsoleSpot spot in takeable)
            {
                Assert.Contains(tops, t => Math.Abs(t.X - spot.X) < 0.5 && Math.Abs(t.Y - spot.Y) < 0.5);
            }
        }
    }

    /// <summary>
    /// A TOP SOMEBODY IS ALREADY AT IS NOT OFFERED — the room's regulars, the Magpie and the oracle keep
    /// their own consoles, and no two consoles in this bar are within an [E] reach of each other.
    ///
    /// <para>This is the clause that stops the press grabbing the wrong fixture, which is the fault every
    /// seat placement in this file's neighbourhood has been authored against since #410. Asserted over every
    /// haven AND over a spread of docking watches, because WHICH regular is where is a function of the
    /// watch — a rule that held at sim-time zero and nowhere else would be no rule at all.</para>
    /// </summary>
    [Fact]
    public void NoTakeableTopSitsOnTopOfSomebodyElsesConsole()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            for (int watch = 0; watch < 12; watch++)
            {
                double simTime = watch * 4 * 3600.0;
                DeckPlan deck = HavenInterior.DockedDeck(body, null, simTime)!;
                DeckPlan.ConsoleSpot[] all = [.. deck.Consoles];

                foreach (DeckPlan.ConsoleSpot top in all.Where(c => c.Kind == DeckPlan.ConsoleKind.BarTop))
                {
                    foreach (DeckPlan.ConsoleSpot other in all)
                    {
                        if (other.Kind == DeckPlan.ConsoleKind.BarTop
                            && Math.Abs(other.X - top.X) < 1e-6 && Math.Abs(other.Y - top.Y) < 1e-6)
                        {
                            continue;   // itself
                        }

                        double dx = other.X - top.X;
                        double dy = other.Y - top.Y;
                        Assert.True((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius,
                                    $"{body} watch {watch}: a takeable top at ({top.X:0.##},{top.Y:0.##}) is "
                                    + $"within an [E] reach of `{other.Label}` — the press cannot tell them "
                                    + "apart, so sitting down and talking to somebody are one keystroke.");
                    }
                }
            }
        }
    }

    // ── THE PRESS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// [E] AT A BAR TOP SITS THE CAPTAIN DOWN, and the sitting it opens is a real one: the seat's own field
    /// is filled, the body is snapped onto the chair the room published, and the seated ladder answers.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>ConsoleKind.BarTop</c> arm from
    /// <c>Map.Deck.Interact.InteractAtConsole</c> — the press falls through to the ground verb and the
    /// captain stays on his feet.</para>
    /// </summary>
    [Fact]
    public void PressingATopInTheDockedBarOpensASitting()
    {
        Pages.Map map = AshoreAt(TheRedEye);

        Assert.Null(Seated(map));
        Assert.True(SitAtAFreeTop(map), "no top in The Red Eye's bar answered [E].");

        object seat = Seated(map)!;
        Assert.True((bool)Get(seat, "Solo")!, "a top nobody else is at is not a top you are sharing.");
        Assert.True((bool)Get(seat, "Joined")!, "there is nobody to ask — the table is simply taken.");
        Assert.False((bool)Get(seat, "TheyCameToYou")!,
                     "the captain crossed the room and pulled the chair out, which is a posture change and "
                     + "presents in the docked strip with the room still lit behind it (#865).");
        Assert.False((bool)Get(seat, "Bench")!);
        Assert.False((bool)Get(seat, "Quiet")!, "a station bar is one loud room with a window in it.");

        // …AND THE SITTING SAYS WHERE IT IS. FOUND BY LOOKING: the strip's company clause is built out of the
        // scene's own Setting, and the canteen's is a constant three hundred thousand kilometres from this
        // room — a woman standing at a top in The Stormwatch Bar was announced as being at "a table in the
        // upper canteen". This repository's "the sim doing one thing while a SENTENCE reports another" class.
        string setting = (string)Get(Get(seat, "Scene")!, "Setting")!;
        Assert.Equal(SittingAlone.BarSetting(HavenInterior.BarNameOf(TheRedEye)), setting);
        Assert.NotEqual(SittingAlone.Setting, setting);
        Assert.Contains(HavenInterior.BarNameOf(TheRedEye)!, setting, StringComparison.Ordinal);

        // The key is the BAR'S own — a berth has no excursion and no canteen watch, so it cannot be, and must
        // never collide with, the Hive's "watch:floor:index".
        string key = (string)Get(seat, "Key")!;
        Assert.StartsWith("bar:", key, StringComparison.Ordinal);
        Assert.Contains(TheRedEye, key, StringComparison.Ordinal);

        // #820's snap: the body is ON the chair, at the place the ROOM sounded, never one this test measured.
        IReadOnlyList<SurfaceCollision.Segment> walls = ((DeckPlan)Field(map, "_deckPlan")!).CollisionField;
        DeckReachability.Point top = TopOf(seat);
        DeckReachability.Point chair = HavenInterior.BesideATop(top, DeckPlan.AvatarRadius, walls)!.Value;
        Assert.Equal(chair.X, (double)Field(map, "_avatarX")!, 6);
        Assert.Equal(chair.Y, (double)Field(map, "_avatarY")!, 6);
    }

    /// <summary>
    /// SITTING DOWN IS WHAT MAKES THE PREDICATE TRUE — the one #973 L0 shipped answering <i>false at every
    /// berth in the game</i>, with its own docblock saying so and saying why.
    ///
    /// <para>It is the hinge of the whole lane: the salesman's crossing, the walk-in's crossing and the
    /// approach hook's gate are all one question, and until this press existed the answer was a constant.</para>
    /// </summary>
    [Fact]
    public void TheCaptainSittingAloneInTheBarIsTrueOnlyOnceHeHasSatDown()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        Assert.False((bool)Invoke(map, "TheCaptainIsSittingAloneInTheBar")!);

        Assert.True(SitAtAFreeTop(map));
        Assert.True((bool)Invoke(map, "TheCaptainIsSittingAloneInTheBar")!);

        // …and standing up closes it exactly as it does at every other seat in the game.
        Assert.True((bool)Invoke(map, "StandUpBeforeWalking")!);
        Assert.Null(Seated(map));
        Assert.False((bool)Invoke(map, "TheCaptainIsSittingAloneInTheBar")!);
    }

    /// <summary>A second press at the same top is CONSUMED and changes nothing — [E] is not how you stand up,
    /// and re-opening would wipe the outcome line somebody is in the middle of reading (#680).</summary>
    [Fact]
    public void PressingAgainDoesNotReopenTheSitting()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        Assert.True(SitAtAFreeTop(map));

        object first = Seated(map)!;
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!);
        Assert.Same(first, Seated(map));
    }

    /// <summary>The press is nobody's on a floor with no bar under it. The clause that catches a verb that
    /// answered true off a stale field rather than off the room the captain is standing in.</summary>
    [Fact]
    public void ThereIsNoBarTopToTakeWhenThereIsNoBar()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        Set(map, "_dockedHavenId", null);

        Assert.False((bool)Invoke(map, "TryTakeBarTop")!);
        Assert.Null(Seated(map));
    }

    // ── THE WALK-IN ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE COMES OUT OF A LEAF, CROSSES THE FLOOR, AND STOPS AT THE TABLE — and her card is up when she gets
    /// there, not before.
    ///
    /// <para>This is the showcase the owner asked for: <i>"that story is never found sitting down… she makes
    /// an entrance at some classy place, crosses the room, and comes to our table when we are alone."</i> So
    /// the route is asserted to be a ROUTE (more than one point is the difference between a walk and a
    /// teleport with a plate on it), its first point is asserted to be one of the bar's own back-room
    /// doorsteps, and the card is asserted absent until the body has actually arrived.</para>
    ///
    /// <para><b>Proven RED</b> by raising the card in <c>AdvanceTheWalkIn</c> instead of in
    /// <c>SheReachesYourTable</c> — the panel is up on the frame she steps out of the cellar.</para>
    /// </summary>
    [Fact]
    public void SheComesThroughALeafAndReachesTheTableBeforeHerCardIsUp()
    {
        Pages.Map map = AshoreAtWithAWalkIn();
        HavenInterior.BarFloor bar = HavenInterior.BarBand(TheRedEye)!.Value;

        DeckReachability.Point? doorstep = null;
        bool arrived = false;

        for (int i = 0; i < 600 && !arrived; i++)
        {
            RunFrames(map, 1);
            if (HerWalk(map) is not { } walk)
            {
                Assert.Null(Field(map, "_walkInCard"));
                continue;
            }

            var route = (IReadOnlyList<DeckReachability.Point>)Get(walk, "Route")!;
            Assert.True(route.Count > 1,
                        "she was placed rather than walked — a one-point route is a teleport with a plate on it.");
            doorstep ??= route[0];
            arrived = Field(map, "_walkInCard") is not null;
        }

        Assert.True(doorstep is not null, "six hundred frames and nobody ever crossed the floor.");
        Assert.Contains(bar.Doors, leaf => Near(doorstep!.Value, leaf));
        Assert.True(arrived, "she never reached the table, so her card never came up.");

        // The seam's own gate agrees the canvas is really on the screen — a HOSTED beat counted as told with
        // nothing showing is what #777's follow-up refuses.
        Assert.True((bool)Invoke(map, "TheHostIsUp", StoryBeats.Beat.WalkIn)!);

        // She has joined the sitting rather than opening one. The captain's own chair is still the captain's.
        object seat = Seated(map)!;
        Assert.False((bool)Get(seat, "Solo")!, "somebody is at the top now.");
        Assert.Equal(WalkIn.Plate(WhoCame(map)), (string)Get(seat, "Plate")!);

        // ONE OF HER, AND EXACTLY ONE. FOUND BY LOOKING: the strip read "seats 4, 0 chairs free" at a top with
        // one woman standing at it, because `_walkInAskedThisVisit` is set on ARRIVAL — hundreds of frames
        // after she sets off — so the plan gate re-sent her every frame in between and every arrival took a
        // chair. Two chairs are spoken for at this table: the captain's and hers.
        Assert.Equal((int)Get(seat, "Seats")! - 2, (int)Get(seat, "Free")!);
        Assert.Single(
            ((IEnumerable)Field(map, "_barAfoot")!).Cast<object>(),
            b => (string)Get(Get(b, "Walk")!, "Plate")! == WalkIn.Plate(WhoCame(map)));
        Assert.False((bool)Get(seat, "TheyCameToYou")!,
                     "HER card is the card; a second modal with the same woman on it is #777's stacked card.");
    }

    /// <summary>SHE DOES NOT COME TO A CAPTAIN ON HIS FEET. The gate is the whole of the scene's manners, and
    /// it is asked every frame — a walk-in raised at somebody standing at the counter would be the beat and
    /// the room disagreeing about what the captain is doing.</summary>
    [Fact]
    public void NobodyCrossesTheFloorToACaptainWhoIsNotSittingDown()
    {
        Pages.Map map = AshoreAtWithAWalkIn(sitDown: false);

        RunFrames(map, 400);

        Assert.Null(HerWalk(map));
        Assert.Null(Field(map, "_walkInCard"));
    }

    /// <summary>
    /// YES YIELDS THE JOB AND THE NOTE — a FIND tagged love with no coin in it, and a held memory marked
    /// <i>hers</i> carrying Fable's line verbatim.
    /// </summary>
    [Fact]
    public void SayingYesTakesHerJobAndLeavesHerNoteOnTheTable()
    {
        Pages.Map map = SheIsAtTheTable();
        WalkIn.Who who = WhoCame(map);

        Invoke(map, "AnswerTheWalkIn", true);

        object job = Assert.Single(Quests(map).Cast<object>());
        Assert.Equal("WalkIn", Get(job, "Kind")!.ToString());
        Assert.Equal(WalkIn.Name(who), (string)Get(job, "Giver")!);
        Assert.Equal(0, (int)Get(job, "Reward")!);
        Assert.Equal(HeldMemory.Theory.Love, (HeldMemory.Theory)Get(job, "Theory")!);

        // Two berths, and neither of them is nowhere: go and find it, then come back and tell her.
        Assert.Equal(TheRedEye, (string?)Get(job, "DestBodyId"));
        Assert.False(string.IsNullOrEmpty((string?)Get(job, "SourceBodyId")));
        Assert.NotEqual(TheRedEye, (string?)Get(job, "SourceBodyId"));

        // #972's plain block, off the live world: FIND, and a payout line that is a dash.
        var plain = (IReadOnlyList<string>)Invoke(map, "JobPlainBlock", job)!;
        Assert.StartsWith("FIND — ", plain[0], StringComparison.Ordinal);
        Assert.Equal($"{JobTerms.NoPayout} · {JobTerms.ForHerWord}", plain[3]);

        // Her note, marked hers, tagged love, in her own words.
        HeldMemory.Sheet note = HeldMemory.Find(Book(map), WalkIn.NoteId(who))
            ?? throw new InvalidOperationException("she left no note on the table.");
        Assert.Equal(HeldMemory.Mark.Hers, note.Mark);
        Assert.Equal(HeldMemory.Theory.Love, note.Tag);
        Assert.Equal(WalkIn.NoteText(who), note.Text);

        // …and the table is the captain's own again.
        Assert.Null(Field(map, "_walkInCard"));
        Assert.True((bool)Get(Seated(map)!, "Solo")!);
    }

    /// <summary>
    /// NO YIELDS HER LINE AND NOTHING ELSE — no note, no job, and she does not ask again this visit. The
    /// clause that catches a refusal that quietly filed the job anyway, which is the shape a "cancel" button
    /// takes when nobody checks it.
    /// </summary>
    [Fact]
    public void SayingNoLeavesHerLineAndNoJobAtAll()
    {
        Pages.Map map = SheIsAtTheTable();
        WalkIn.Who who = WhoCame(map);

        Invoke(map, "AnswerTheWalkIn", false);

        Assert.Empty(Quests(map).Cast<object>());
        Assert.Null(HeldMemory.Find(Book(map), WalkIn.NoteId(who)));
        Assert.Null(Field(map, "_walkInCard"));

        // Her line was said where the captain is looking, and it is HERS.
        Assert.Equal(WalkIn.IfNo(who), TheLineOnScreen(map));

        // …and four hundred more frames do not bring her back to ask a second time.
        RunFrames(map, 400);
        Assert.Null(Field(map, "_walkInCard"));
        Assert.Empty(Quests(map).Cast<object>());
    }

    // ── THE BENCH ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page clamped onto a berth, standing in its bar, with a world under it and the salesman
    /// kept off the floor so the only body that can be crossing it is hers.</summary>
    private static Pages.Map AshoreAt(string berth)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_ephemeris", TheWorld());
        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", (bool?)false);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
        return map;
    }

    /// <summary>…and with the walk-in forced on, which is what <c>?walkin=1</c> is for: her cadence is rare
    /// ON TOP OF a classy-venue gate and a captain who has to already be sitting alone, so without the lever
    /// this scene is reachable only by luck.</summary>
    private static Pages.Map AshoreAtWithAWalkIn(bool sitDown = true)
    {
        Pages.Map map = AshoreAt(TheRedEye);
        Set(map, "_walkInCheat", (bool?)true);
        Invoke(map, "EnsureWalkInVisit", (string?)null);   // re-read the cheat for this berth
        if (sitDown)
        {
            Assert.True(SitAtAFreeTop(map));
        }

        return map;
    }

    /// <summary>Run the room until she is standing at the table with her card up.</summary>
    private static Pages.Map SheIsAtTheTable()
    {
        Pages.Map map = AshoreAtWithAWalkIn();
        for (int i = 0; i < 600 && Field(map, "_walkInCard") is null; i++)
        {
            RunFrames(map, 1);
        }

        Assert.True(Field(map, "_walkInCard") is not null, "she never reached the table.");
        return map;
    }

    /// <summary>Two great ports and a planet to hang them off — enough of a world for the postings and for a
    /// job that has to point somewhere that is not here.</summary>
    private static ICelestialEphemeris TheWorld() =>
        new CircularOrbitEphemeris(
        [
            new CelestialBody("sol", "Sol", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("jupiter", "Jupiter", "sol", 1.267e17, 6.99e7, 7.78e11, 3.7e5, 0),
            new CelestialBody("saturn", "Saturn", "sol", 3.79e16, 5.82e7, 1.43e12, 2.2e5, 0),
            new CelestialBody(TheRedEye, "The Red Eye", "jupiter", 0, 0, 5e8, 4e4, 0,
                BodyKind.Station, IsHaven: true),
            new CelestialBody(TheOtherPort, "Ringside Exchange", "saturn", 0, 0, 5e8, 4e4, 0,
                BodyKind.Station, IsHaven: true),
        ]);

    /// <summary>Sit the captain at the first top in this bar that answers [E]. Which one it is depends on the
    /// rota, so the bench asks the room rather than naming a chair.</summary>
    private static bool SitAtAFreeTop(Pages.Map map)
    {
        var deck = (DeckPlan)Field(map, "_deckPlan")!;
        foreach (DeckPlan.ConsoleSpot spot in deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.BarTop))
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

    private static object? Seated(Pages.Map map) => Invoke(map, "get_SeatedTable");

    private static IList Quests(Pages.Map map) => (IList)Field(map, "_quests")!;

    private static IReadOnlyList<HeldMemory.Sheet> Book(Pages.Map map) =>
        (IReadOnlyList<HeldMemory.Sheet>)Field(map, "_heldMemories")!;

    private static WalkIn.Who WhoCame(Pages.Map map) => (WalkIn.Who)Field(map, "_walkInCard")!;

    /// <summary>Her walk, if she is on the floor — found by her plate, because her errand is the room's
    /// ordinary approach and the salesman is the one told apart by his.</summary>
    private static object? HerWalk(Pages.Map map)
    {
        if (Field(map, "_walkInWho") is not WalkIn.Who who)
        {
            return null;
        }

        string plate = WalkIn.Plate(who);
        foreach (object body in ((IEnumerable)Field(map, "_barAfoot")!))
        {
            object walk = Get(body, "Walk")!;
            if (string.Equals((string)Get(walk, "Plate")!, plate, StringComparison.Ordinal))
            {
                return walk;
            }
        }

        return null;
    }

    /// <summary>What the HUD is saying right now — the one slot the pulse keeps, read off the page's own
    /// <see cref="PulseSlot"/> rather than off a second field nothing writes.</summary>
    private static string? TheLineOnScreen(Pages.Map map) =>
        (string?)Get(Field(map, "_pulse")!, "Message");

    /// <summary>Which top a sitting is at, off the room's own published list and the sitting's own ordinal.</summary>
    private static DeckReachability.Point TopOf(object seat) =>
        HavenInterior.BarBand(TheRedEye)!.Value.Tops[(int)Get(seat, "Index")!];

    /// <summary>Is this point the doorstep of that leaf — within one standoff of its midline?</summary>
    private static bool Near(DeckReachability.Point p, UndergroundComplex.LockedDoor leaf)
    {
        double mx = (leaf.X1 + leaf.X2) / 2, my = (leaf.Y1 + leaf.Y2) / 2;
        double span = Math.Sqrt(((leaf.X2 - leaf.X1) * (leaf.X2 - leaf.X1))
                                + ((leaf.Y2 - leaf.Y1) * (leaf.Y2 - leaf.Y1)));
        double d = Math.Sqrt(((p.X - mx) * (p.X - mx)) + ((p.Y - my) * (p.Y - my)));
        return d <= (span / 2) + Egress.DoorStandoffDu + 1e-6;
    }

    // ── Reflection plumbing ──────────────────────────────────────────────────────────────────────────────

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

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

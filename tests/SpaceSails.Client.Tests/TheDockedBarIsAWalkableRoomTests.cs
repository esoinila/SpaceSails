using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #973 L0 · THE OWNER'S FAVOURITE ROOM CAN BE WALKED ACROSS.
///
/// <para>The Red Eye's bar off Jupiter — the menu card, the offer of a drink to One-Eye Silas, the regulars at
/// their tops, the PIRATE INSURANCE poster on the starboard wall — was the one room in this game where nobody
/// could move. Eleven droid slots, every one a stateless function of sim time. #731 built a room's whole
/// metabolism and wired it to a Hive canteen floor; #976 put Harlan Fess on that floor and its own file wrote
/// down why it could go nowhere else: <i>"A docked station's bar has posters and a barkeep and no seating and
/// no walkers at all."</i></para>
///
/// <para>Everything below needs a ROOM. The geometry claims are made against every haven that has one, so a
/// tier added later cannot quietly ship a bar with no floor in it; the behavioural ones stand a real page at a
/// real berth and run the frame the way the game runs it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDockedBarIsAWalkableRoomTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The classy great-port tier, and the one the owner drinks in.</summary>
    private const string TheRedEye = "red-eye";

    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    // ── The room, as geometry ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY BAR IN THE GAME HAS A BAND, and it is not a list somebody remembered to write: the claim is made
    /// over <see cref="HavenInterior.InteriorBodyIds"/>, so a ninth haven added next month either publishes a
    /// walkable bar or turns this red.
    /// </summary>
    [Fact]
    public void EveryHavenWithAnInteriorPublishesABarWithDoorsAndTops()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            HavenInterior.BarFloor bar = HavenInterior.BarBand(body)
                ?? throw new InvalidOperationException($"{body} has an interior and no bar band.");

            Assert.True(bar.Doors.Count > 0, $"{body}'s bar has no leaf anybody could come out of.");
            Assert.True(bar.Tops.Count > 0, $"{body}'s bar has no top anybody could be walked to.");
            Assert.True(bar.Fixtures.Count > 0, $"{body}'s bar has nowhere for a body with nothing to do to stand.");
        }
    }

    /// <summary>
    /// THE LEAVES ARE ONES THE CAPTAIN IS REFUSED AT — #731's load-bearing law, and the reason
    /// <see cref="Egress"/> takes a <see cref="UndergroundComplex.LockedDoor"/> and not a doorway. Every door
    /// the band publishes is matched back to a <c>Locked</c> door the deck actually hangs on a wall, by
    /// coordinates, so a walker cannot be sent out through a leaf that opens for the player.
    ///
    /// <para><b>Proven RED</b> by publishing the bar's wide hall auto-door in <c>BarBackRoomLeaves</c>.</para>
    /// </summary>
    [Fact]
    public void EveryLeafSomebodyComesOutOfIsOneTheCaptainIsRefusedAt()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            HavenInterior.BarFloor bar = HavenInterior.BarBand(body)!.Value;
            DeckPlan deck = HavenInterior.DockedDeck(body)!;

            foreach (UndergroundComplex.LockedDoor leaf in bar.Doors)
            {
                Assert.True(
                    deck.Doors.Any(d => d.Locked
                                        && Math.Abs(d.X1 - leaf.X1) < 1e-3 && Math.Abs(d.Y1 - leaf.Y1) < 1e-3
                                        && Math.Abs(d.X2 - leaf.X2) < 1e-3 && Math.Abs(d.Y2 - leaf.Y2) < 1e-3),
                    $"{body}: the band publishes `{leaf.Sign}` at ({leaf.X1:0.##},{leaf.Y1:0.##})–"
                    + $"({leaf.X2:0.##},{leaf.Y2:0.##}), and the deck hangs no LOCKED door there. #731's whole "
                    + "beat is a door that does not open for you opening for somebody else.");

                // …and the plate on the walker's door is the plate the captain reads on the panel beside it.
                Assert.Contains(deck.Consoles, c => c.Kind == DeckPlan.ConsoleKind.Hatch && c.Label == leaf.Sign);
            }
        }
    }

    /// <summary>
    /// THE DOOR → TABLE PATH SOLVES, in every bar, through every leaf, to every top. This is the claim the
    /// whole lane rests on: a band with no route through it is a room that looks walkable and refuses every
    /// walk, which is exactly how a body ends up placed at the far end of a walk nobody could walk.
    ///
    /// <para><b>Proven RED</b> by moving the cellar leaf outside the bar's side wall (the doorstep then sounds
    /// onto ground with no route back into the room).</para>
    /// </summary>
    [Fact]
    public void EveryLeafReachesEveryTopInEveryBar()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            HavenInterior.BarFloor bar = HavenInterior.BarBand(body)!.Value;
            DeckPlan deck = HavenInterior.DockedDeck(body)!;
            IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;

            foreach (UndergroundComplex.LockedDoor leaf in bar.Doors)
            {
                foreach (DeckReachability.Point top in bar.Tops)
                {
                    DeckReachability.Point beside = BesideThisTop(top, walls)
                        ?? throw new InvalidOperationException(
                            $"{body}: nothing can stand at the top ({top.X:0.##},{top.Y:0.##}).");

                    DeckReachability.Point doorstep =
                        Egress.StandingPlaceAt(in leaf, DeckPlan.AvatarRadius, walls, beside.X, beside.Y)
                        ?? throw new InvalidOperationException(
                            $"{body}: no body can stand in front of `{leaf.Sign}`.");

                    NpcWalk? walk = NpcWalk.Plan(
                        "◈ SOMEBODY", new NpcWalk.Bound("", beside.X, beside.Y), doorstep, walls,
                        DeckPlan.AvatarRadius, SurfaceCollision.Gait.Person);

                    Assert.True(walk is not null,
                                $"{body}: no route from `{leaf.Sign}` at ({doorstep.X:0.##},{doorstep.Y:0.##}) "
                                + $"to the top at ({beside.X:0.##},{beside.Y:0.##}). Nobody can cross this bar.");
                }
            }
        }
    }

    /// <summary>…and the counter is a place a body can stand, in every bar. The one fixture a person with
    /// nothing to do is drifting between, so a blocked one is a beat with nowhere to go.</summary>
    [Fact]
    public void EveryBarsCounterHasSomewhereToStand()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            HavenInterior.BarFloor bar = HavenInterior.BarBand(body)!.Value;
            IReadOnlyList<SurfaceCollision.Segment> walls = HavenInterior.DockedDeck(body)!.CollisionField;

            Assert.Contains(bar.Fixtures,
                            p => !SurfaceCollision.Blocked(p.X, p.Y, DeckPlan.AvatarRadius, walls));
        }
    }

    // ── The band, as a buffer ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BAND AND THE BUFFER AGREE — the mirrored constant that has thrown <c>IndexOutOfRangeException</c>
    /// at the renderer twice already (#731, then #973 L2), stated here for the third room to ask for slots.
    ///
    /// <para><b>Proven RED</b> by leaving <c>droidCount</c> at <c>SeatedFigureCount</c> while still passing a
    /// filler.</para>
    /// </summary>
    [Fact]
    public void TheDockedDeckReservesTheBandAndTheBufferHoldsIt()
    {
        foreach (string body in HavenInterior.InteriorBodyIds)
        {
            DeckPlan bare = HavenInterior.DockedDeck(body)!;
            Assert.Equal(HavenInterior.SeatedFigureCount, bare.DroidCount);

            DeckPlan walked = HavenInterior.DockedDeck(body, null, 0, false, (_, _) => { })!;
            Assert.Equal(HavenInterior.SeatedFigureCount + Egress.BandSlots, walked.DroidCount);
            Assert.True(walked.DroidCount <= DeckPlan.MaxDroids,
                        $"{body}'s walked bar draws {walked.DroidCount} figures out of a "
                        + $"{DeckPlan.MaxDroids}-long buffer.");
        }
    }

    /// <summary>A deck built with a page's own filler is NEVER handed back out of the shared cache — the
    /// delegate is closed over one page and xUnit runs test classes in parallel. Two pages sharing one
    /// filler is one buffer written by two rooms.</summary>
    [Fact]
    public void AWalkedDeckIsNeverSharedOutOfTheCache()
    {
        DeckPlan one = HavenInterior.DockedDeck(TheRedEye, null, 0, false, (_, _) => { })!;
        DeckPlan two = HavenInterior.DockedDeck(TheRedEye, null, 0, false, (_, _) => { })!;
        Assert.NotSame(one, two);

        // …and the plain geometry still is, because that is what the cache is for.
        Assert.Same(HavenInterior.DockedDeck(TheRedEye), HavenInterior.DockedDeck(TheRedEye));
    }

    /// <summary>The band's width is Core's one answer and not a second sum spelled out on the page.</summary>
    [Fact]
    public void TheBandsWidthIsStatedOnce()
    {
        int band = (int)typeof(Pages.Map)
            .GetField("WalkerBand", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetRawConstantValue()!;

        Assert.Equal(Egress.BandSlots, band);
        Assert.True(Egress.BandSlots > Egress.MostAtOnce,
                    "a room's own leavers can hold every slot there is, so a visitor needs one of their own.");
    }

    // ── The room, driven ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE COMES OUT OF A DOOR AND CROSSES THE BAR. The whole point of the lane, driven: a page clamped onto
    /// The Red Eye, the rota forced on, the frame run the way the game runs it — and a body that started at a
    /// leaf's doorstep is somewhere else in the room, having got there on a real route.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>ApproachTheTable</c>/beat branch from
    /// <c>SendTheRepIntoTheBar</c> — nobody ever gets on the floor.</para>
    /// </summary>
    [Fact]
    public void TheSalesmanComesOutOfALeafAndWalksTheBar()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        HavenInterior.BarFloor bar = HavenInterior.BarBand(TheRedEye)!.Value;

        DeckReachability.Point? doorstep = null;
        bool everMoved = false;

        // Frame by frame, because he is a man with a working day: he lands, he stands for a while, and then
        // he comes in again. Asked at one arbitrary instant this would pass or fail on his dwell timer.
        for (int i = 0; i < 400; i++)
        {
            RunFrames(map, 1);

            // #731 · HIS body, by his plate, and no longer "whoever is on the floor". The bar keeps its own
            // hours now — a regular finishing, somebody coming out of the back — so a guard about the
            // SALESMAN that read the first walker it found would be a guard about whoever happened to be
            // crossing the room.
            if (TheWalkerPlated(map, NebulaRep.Plate) is not { } who)
            {
                continue;
            }

            object walk = Get(who, "Walk")!;
            var route = (IReadOnlyList<DeckReachability.Point>)Get(walk, "Route")!;
            Assert.True(route.Count > 1,
                        "he was placed rather than walked — a one-point route is a teleport with a plate on it.");

            doorstep ??= route[0];
            if (Math.Abs((double)Get(walk, "X")! - route[0].X) > 1e-6
                || Math.Abs((double)Get(walk, "Y")! - route[0].Y) > 1e-6)
            {
                everMoved = true;
            }
        }

        Assert.True(doorstep is not null, "four hundred frames and the salesman never got on the floor at all.");
        Assert.Contains(bar.Doors, leaf => Near(doorstep!.Value, leaf));
        Assert.True(everMoved, "he stood on the doorstep for four hundred frames — nobody crossed this room.");
    }

    /// <summary>…and the drawn room agrees with the walked one: his body is written into the slots the deck
    /// reserved for the band, so the figure the captain sees is the figure the sim is stepping.</summary>
    [Fact]
    public void HisBodyIsDrawnOutOfTheBandTheDeckReserved()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        for (int i = 0; i < 400 && TheWalkerPlated(map, NebulaRep.Plate) is null; i++)
        {
            RunFrames(map, 1);
        }

        object him = TheWalkerPlated(map, NebulaRep.Plate)
            ?? throw new InvalidOperationException("four hundred frames and the salesman never got afoot.");
        var plan = (DeckPlan)Field(map, "_deckPlan")!;
        var buffer = new DeckPlan.Droid[DeckPlan.MaxDroids];
        plan.FillDroids((double)Field(map, "SimTime")!, buffer);

        object walk = Get(him, "Walk")!;
        Assert.True(HavenInterior.SeatedFigureCount + Egress.BandSlots <= plan.DroidCount,
                    $"the band is written into slots {HavenInterior.SeatedFigureCount}.."
                    + $"{HavenInterior.SeatedFigureCount + Egress.BandSlots - 1} and the plan draws only "
                    + $"{plan.DroidCount} figures — the renderer will never look at him.");

        // #731 · …at HIS slot in the band, which is his place in the room's own list of feet. The bar has
        // more than one person on the floor since the room grew hours.
        int slot = HavenInterior.SeatedFigureCount + BarAfoot(map).Cast<object>().ToList().IndexOf(him);
        DeckPlan.Droid drawn = buffer[slot];
        Assert.Equal(NebulaRep.Plate, drawn.Name);
        Assert.Equal((double)Get(walk, "X")!, drawn.X, 6);
        Assert.Equal((double)Get(walk, "Y")!, drawn.Y, 6);

        // …and every slot the room is NOT using is off-map, so the buffer is always fully written.
        for (int i = BarAfoot(map).Count; i < Egress.BandSlots; i++)
        {
            Assert.True(buffer[HavenInterior.SeatedFigureCount + i].X < -9000);
        }
    }

    /// <summary>CASTING OFF IS THE ROOM FORGETTING. A body left on a list across a re-dock would be drawn
    /// walking through a station it was never in.</summary>
    [Fact]
    public void CastingOffEmptiesTheBar()
    {
        Pages.Map map = AshoreAt(TheRedEye);
        for (int i = 0; i < 400 && BarAfoot(map).Count == 0; i++)
        {
            RunFrames(map, 1);
        }

        Assert.NotEmpty(BarAfoot(map));

        Set(map, "_dockedHavenId", null);
        Invoke(map, "AdvanceBarWalkers", 0.1);
        Assert.Empty(BarAfoot(map));
    }

    // ── The hook ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE APPROACH FIRES ONLY IF IT IS STILL WANTED WHEN IT LANDS — the one law
    /// <c>ApproachTheTable</c> keeps, and the one L5b will be standing on.
    ///
    /// <para><b>Proven RED</b> by firing <c>OnArrive</c> before asking <c>StillWanted</c> again.</para>
    /// </summary>
    [Fact]
    public void TheApproachFiresWhenItLandsOnSomebodyWhoIsStillThere()
    {
        Pages.Map map = AshoreAt(TheRedEye, repWorking: false);
        StandAtATop(map);

        bool wanted = true;
        int arrived = 0;
        Assert.True((bool)Invoke(map, "ApproachTheTable",
                                 "◈ SOMEBODY", (Func<bool>)(() => wanted), (Action)(() => arrived++))!);

        RunFrames(map, 600);
        Assert.Equal(1, arrived);

        // …and arriving is the BEGINNING: they stand at the top and are still there a hundred frames later.
        RunFrames(map, 100);
        Assert.Equal(1, arrived);
        Assert.NotNull(TheWalkerPlated(map, "◈ SOMEBODY"));
    }

    /// <summary>
    /// …AND NOT AT ALL WHEN THE CAPTAIN HAS GONE. The scene they were walking into ended mid-stride; the
    /// honest answer is that nothing happens and they stand at the bar like anybody else.
    ///
    /// <para><b>Proven RED</b> by dropping the second <c>StillWanted</c> call from <c>StepAnApproach</c>.</para>
    /// </summary>
    [Fact]
    public void TheApproachDeliversNothingToACaptainWhoHasGone()
    {
        Pages.Map map = AshoreAt(TheRedEye, repWorking: false);
        StandAtATop(map);

        bool wanted = true;
        int arrived = 0;
        Assert.True((bool)Invoke(map, "ApproachTheTable",
                                 "◈ SOMEBODY", (Func<bool>)(() => wanted), (Action)(() => arrived++))!);

        RunFrames(map, 5);          // on their feet, mid-floor
        wanted = false;             // …and the captain stands up
        RunFrames(map, 900);

        Assert.Equal(0, arrived);
    }

    /// <summary>
    /// A ROOM FULL OF FEET NEVER STARVES AN APPROACH — the #973 L2 bug, restated for this room. The band is
    /// the room's own allowance PLUS the visitor's slot, so departures cannot hold every slot there is.
    ///
    /// <para><b>Proven RED</b> by setting <c>WalkerBand = Egress.MostAtOnce</c>.</para>
    /// </summary>
    [Fact]
    public void TheRoomsOwnLeaversCannotHoldEverySlot()
    {
        Pages.Map map = AshoreAt(TheRedEye, repWorking: false);
        StandAtATop(map);

        for (int i = 0; i < Egress.MostAtOnce; i++)
        {
            Assert.True((bool)Invoke(map, "ApproachTheTable",
                                     $"◈ REGULAR {i}", (Func<bool>)(() => false), (Action)(() => { }))!);
        }

        Assert.Equal(Egress.MostAtOnce, BarAfoot(map).Count);
        Assert.True((bool)Invoke(map, "ApproachTheTable",
                                 "◈ SOMEBODY", (Func<bool>)(() => true), (Action)(() => { }))!,
                    $"{Egress.MostAtOnce} of the room's own people are afoot and the visitor was refused a "
                    + "slot — which is exactly the bug #973 L2 was fixed for, in a different room.");
    }

    /// <summary>Nobody is sitting alone, so nobody is crossed to — they come in and wait at the counter, which
    /// is what a person in a bar does. The honest answer today, and the branch that turns into the walk-in the
    /// moment a docked bar can seat anybody.</summary>
    [Fact]
    public void WithNobodyToComeToTheyWaitAtTheCounter()
    {
        Pages.Map map = AshoreAt(TheRedEye, repWorking: false);
        Set(map, "_avatarX", 0.0);
        Set(map, "_avatarY", (double)HavenInterior.BarBand(TheRedEye)!.Value.FloorY + 3);

        int arrived = 0;
        Assert.True((bool)Invoke(map, "ApproachTheTable",
                                 "◈ SOMEBODY", (Func<bool>)(() => true), (Action)(() => arrived++))!);
        RunFrames(map, 900);

        Assert.Equal(0, arrived);
        object who = TheWalkerPlated(map, "◈ SOMEBODY")!;
        object walk = Get(who, "Walk")!;
        HavenInterior.BarFloor bar = HavenInterior.BarBand(TheRedEye)!.Value;
        Assert.Contains(bar.Fixtures,
                        p => Math.Abs(p.X - (double)Get(walk, "X")!) < 1
                             && Math.Abs(p.Y - (double)Get(walk, "Y")!) < 1);
    }

    /// <summary>
    /// …AND THE WALKED FRAME ACTUALLY STEPS IT. Everything above drives <c>AdvanceBarWalkers</c> by hand,
    /// which proves the room works and proves nothing about whether the game ever asks it to. It cannot be
    /// driven the other way — the walked frame paints to a renderer this bench has none of — so the wiring is
    /// read out of the source, once, beside the surface step it sits next to.
    ///
    /// <para><b>Proven RED</b> by deleting the call from <c>TheWalkedViewOwnsThisFrame</c>.</para>
    /// </summary>
    [Fact]
    public void TheWalkedFrameStepsTheBar()
    {
        string tick = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Sim.Tick.cs"));
        int walked = tick.IndexOf("private bool TheWalkedViewOwnsThisFrame", StringComparison.Ordinal);
        Assert.True(walked >= 0, "Map.Sim.Tick.cs no longer has a walked frame — this guard reads a dead name.");

        string body = tick[walked..];
        Assert.Contains("AdvanceBarWalkers(dtRealSeconds);", body, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    // ── The bench ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A page clamped onto a berth, standing in its bar, with the deck the game builds.</summary>
    private static Pages.Map AshoreAt(string berth, bool repWorking = true)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Set(map, "_dockedHavenId", berth);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", repWorking ? true : (bool?)false);
        Invoke(map, "SetDeckForDock", berth);
        Invoke(map, "StandAtTheBarThreshold");
        return map;
    }

    /// <summary>Stand the captain AT one of the bar's own tops — the nearest-top question
    /// <c>TheTopTheCaptainIsAt</c> asks, answered by standing there. (Sitting down in a docked bar is not
    /// possible in this build; that is L5b's, and the approach's gate is a delegate for exactly that reason.)</summary>
    private static void StandAtATop(Pages.Map map)
    {
        DeckReachability.Point top = HavenInterior.BarBand(TheRedEye)!.Value.Tops[0];
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

    private static IList BarAfoot(Pages.Map map) => (IList)Field(map, "_barAfoot")!;

    /// <summary>#731 · The walker wearing this plate, or null. Named rather than "whoever is on the floor",
    /// because the bar keeps its own hours now and a room with a metabolism has more than one person in
    /// it.</summary>
    private static object? TheWalkerPlated(Pages.Map map, string plate)
    {
        foreach (object who in BarAfoot(map))
        {
            if (string.Equals((string)Get(Get(who, "Walk")!, "Plate")!, plate, StringComparison.Ordinal))
            {
                return who;
            }
        }

        return null;
    }

    /// <summary>Is this point the doorstep of that leaf — within one standoff of its midline?</summary>
    private static bool Near(DeckReachability.Point p, UndergroundComplex.LockedDoor leaf)
    {
        double mx = (leaf.X1 + leaf.X2) / 2, my = (leaf.Y1 + leaf.Y2) / 2;
        double span = Math.Sqrt(((leaf.X2 - leaf.X1) * (leaf.X2 - leaf.X1))
                                + ((leaf.Y2 - leaf.Y1) * (leaf.Y2 - leaf.Y1)));
        double d = Math.Sqrt(((p.X - mx) * (p.X - mx)) + ((p.Y - my) * (p.Y - my)));
        return d <= (span / 2) + Egress.DoorStandoffDu + 1e-6;
    }

    /// <summary>The page's own answer, asked through the page, so this guard cannot be a second geometry.</summary>
    private static DeckReachability.Point? BesideThisTop(
        DeckReachability.Point top, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        (DeckReachability.Point?)typeof(Pages.Map)
            .GetMethod("BesideThisTop", Hidden)!
            .Invoke(null, [top, walls]);

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

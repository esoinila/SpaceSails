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
/// #1061 beat 2 · <b>THE HARDCASE ON THE MOON</b>, driven.
///
/// <para><b>Owner, 2026-09-01:</b> <i>"Maybe some hardcode salesman might be on moon also and runs away from
/// reevers in despair :-D"</i></para>
///
/// <para>Core owns the arithmetic (<c>TheHardcaseOnTheMoonTests</c>, Core side: where he may be, how often,
/// what he says, what he drops). What Core cannot see is whether the PAGE ever walks any of it — a perfect
/// rota nobody spends is exactly the shape of the bug that has put this repository in a browser more than
/// once (a salesman who never got on the floor at all, a lift that only ever went down). So every guard below
/// stands a real moon up, runs the frame the way the game runs it, and reads the answer off the ground.</para>
///
/// <h3>The one guard that asserts an absence</h3>
///
/// <para><see cref="THE_FLIGHT_IsToldWithNothingAtAll"/> is the beat: he runs and the game says nothing. It is
/// driven (no pulse, no card, no plate, no entry in the book, across the whole run) AND read off the shipping
/// source, because an absence can be broken by a line added later in a place the driver's world never
/// reaches.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHardcaseOnTheMoonTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";
    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    // ── HE IS OUT THERE, AND HE COMES OVER ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE WALKS UP OFF THE GROUND AND PITCHES.</b> The captain steps out of the tube, the frames run, and
    /// a body with his plate crosses the regolith and raises the card — carrying the two authored sentences,
    /// verbatim, and a row of the firm's own buttons.
    ///
    /// <para>Everything is read off the FLOOR: the plate on the walker, the errand it carries, the route it
    /// planned. He is never placed at the captain's elbow — he is walked there.</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>false</c> from <c>HeCrossesToTheCaptain</c> before it asks
    /// anything: he stands at his post for the whole run and no card is ever raised.</para>
    /// </summary>
    [Fact]
    public void HE_WalksUpOffTheGroundAndPitches()
    {
        Pages.Map map = OnTheRegolith();

        bool everWalked = false;
        for (int i = 0; i < 3000 && Card(map) is null; i++)
        {
            RunFrames(map, 1);
            foreach (object who in Walkers(map))
            {
                Assert.Equal(HardcaseRep.Plate, (string)Get(Get(who, "Walk")!, "Plate")!);
                everWalked |= !Arrived(who);
            }
        }

        Assert.True(everWalked, "nobody ever crossed a metre of this ground — he was placed, not walked.");
        IReadOnlyList<NebulaRep.RepOffer> offers = Card(map)
            ?? throw new InvalidOperationException("he never reached the captain at all.");

        Assert.NotEmpty(offers);
        Assert.Contains(offers, o => o.Move == NebulaRep.RepMove.NotToday);
        Assert.DoesNotContain(offers, o => o.Move == NebulaRep.RepMove.AlreadyHaveAPolicy);
    }

    /// <summary>
    /// <b>HE IS NEVER ON A GROUND THAT IS NOT A MOON'S SURFACE.</b> The same world, one floor down — the
    /// canteen deck, where beat 1's round already lives — and he never gets on it, cheat or no cheat.
    ///
    /// <para>The bench's own precondition is asserted first: the identical world on floor 0 DOES put him on
    /// the ground. Without that half this guard would pass on a page that had never heard of him.</para>
    ///
    /// <para><b>Proven RED</b> by relaxing <c>HardcaseRep.GroundLikeThis</c> to ignore the floor.</para>
    /// </summary>
    [Fact]
    public void HE_IsNeverOnAnyGroundButTheRegolith()
    {
        Pages.Map onTop = OnTheRegolith();
        RunFrames(onTop, 200);
        Assert.NotEmpty(Walkers(onTop));

        Pages.Map below = OnTheRegolith(floor: TheCanteenFloor);
        RunFrames(below, 200);
        Assert.Empty(Walkers(below));

        // …and there is no ground at all in a berth, which is the other half of the same law.
        Pages.Map berth = OnTheRegolith();
        Set(berth, "_surface", null);
        RunFrames(berth, 50);
        Assert.Null(Card(berth));
    }

    // ── THE BREAK ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>ONE COMES INTO HIS SIGHT AND HE RUNS.</b> An Old One is put on the ground inside his range with a
    /// clear line to him, the frames run, and the body on the floor is on the FLEEING errand at the despair
    /// pace, going somewhere else.
    ///
    /// <para>Asserted as MOTION and not as a flag: he is measurably further from the thing that frightened him
    /// at the end of the run than he was at the start of it. A guard that only read the errand would pass on a
    /// man who changed his mind and stood still.</para>
    ///
    /// <para><b>Proven RED</b> by returning <c>false</c> from <c>OneIsInHisSight</c>: he finishes his round
    /// and pitches with an Old One four metres away.</para>
    /// </summary>
    [Fact]
    public void HE_BreaksAndRunsWhenOneComesIntoHisSight()
    {
        Pages.Map map = OnTheRegolith();
        RunFrames(map, 300);
        object standing = TheOneOnTheGround(map);
        (double fromX, double fromY) = Where(standing);

        double reeverX = fromX + (HardcaseRep.SeesOneAtDu / 2);
        PutAnOldOneAt(map, reeverX, fromY);

        double pace = 0;
        string errand = "";
        for (int i = 0; i < 200 && errand != "HardcaseFleeing"; i++)
        {
            RunFrames(map, 1);
            foreach (object who in Walkers(map))
            {
                errand = Errand(who);
                pace = (double)Get(Get(who, "Walk")!, "Pace")!;
            }
        }

        Assert.Equal("HardcaseFleeing", errand);
        Assert.Equal(HardcaseRep.DespairPaceDu, pace);

        RunFrames(map, 400);
        double now = Walkers(map).Count > 0 ? Where(Walkers(map)[0]!).X : double.NaN;
        double moved = double.IsNaN(now)
            ? double.PositiveInfinity          // he ran clean off the ground, which is further than anywhere
            : Math.Abs(now - reeverX) - Math.Abs(fromX - reeverX);

        Assert.True(moved > 5,
                    $"he broke and then travelled {moved:0.##} du away from the thing he broke at. A man who "
                    + "runs on the spot is a flag, not a flight.");
    }

    /// <summary>
    /// <b>THE FLEEING MAN IS ON THE PATHFINDER AND THE PACK IS NOT.</b> The asymmetry IS the scene, and it is
    /// asserted on both sides in one world: his walk carries a planned route, and nothing in the whole run
    /// ever plans one for an Old One.
    ///
    /// <para>The pack's side is not a hopeful reading of a list — <see cref="NpcWalk.Plan"/> refuses any gait
    /// but a person's at the door, and the Core guard drives that. What is asserted here is the thing a page
    /// could still get wrong: that nothing wearing a Reever's name ever appears among the bodies that DO have
    /// routes.</para>
    /// </summary>
    [Fact]
    public void THE_RunnerIsOnTheLatticeAndThePackNeverIs()
    {
        Pages.Map map = OnTheRegolith();
        RunFrames(map, 300);
        (double x, double y) = Where(TheOneOnTheGround(map));
        PutAnOldOneAt(map, x + (HardcaseRep.SeesOneAtDu / 2), y);

        bool sawARoute = false;
        for (int i = 0; i < 600; i++)
        {
            RunFrames(map, 1);
            foreach (object who in Walkers(map))
            {
                var route = (IReadOnlyList<DeckReachability.Point>)Get(Get(who, "Walk")!, "Route")!;
                string plate = (string)Get(Get(who, "Walk")!, "Plate")!;
                Assert.DoesNotContain("Reever", plate, StringComparison.OrdinalIgnoreCase);
                sawARoute |= Errand(who) == "HardcaseFleeing" && route.Count > 0;
            }
        }

        Assert.True(sawARoute, "the run planned nothing at all — there is no flight here to be asymmetric to.");
    }

    /// <summary>
    /// <b>THE FLIGHT IS TOLD WITH NOTHING AT ALL.</b> No pulse, no story card, no plate, no view-object, and
    /// not one new line in the field book, from the frame he breaks to the frame he is off the ground.
    ///
    /// <para>The sheet in the dust is not a caption: it is dropped by him and says nothing about him, and the
    /// book only ever gets an entry when the captain walks over and picks it up. So the book is asserted
    /// UNCHANGED across the whole run.</para>
    ///
    /// <para><b>Proven RED</b> by adding a single <c>ShowPulseMessage</c> to <c>HeBreaksAndRuns</c>.</para>
    /// </summary>
    [Fact]
    public void THE_FLIGHT_IsToldWithNothingAtAll()
    {
        Pages.Map map = OnTheRegolith();
        RunFrames(map, 300);
        (double x, double y) = Where(TheOneOnTheGround(map));

        int notesBefore = Notes(map).Count;
        Set(map, "_pulse", default(PulseSlot));
        PutAnOldOneAt(map, x + (HardcaseRep.SeesOneAtDu / 2), y);

        bool ranAtAll = false;
        for (int i = 0; i < 600; i++)
        {
            RunFrames(map, 1);
            ranAtAll |= Walkers(map).Cast<object>().Any(w => Errand(w) == "HardcaseFleeing");

            Assert.Null(ThePulse(map));
            Assert.Null(Field(map, "_storyCard"));
            Assert.Null(Field(map, "_viewObject"));
            Assert.Null(Card(map));
            Assert.Equal(notesBefore, Notes(map).Count);
        }

        Assert.True(ranAtAll, "nobody ran, so nothing had a chance to be said about it.");
    }

    /// <summary>…and the same absence, read off the SOURCE, because a line added later lands in a place no
    /// driven world is guaranteed to walk through. Every method of the flight is swept for anything that puts
    /// words on a screen or in the book.</summary>
    [Fact]
    public void THE_FLIGHT_SaysNothingInTheSourceEither()
    {
        string source = TheHardcasesSource();
        string flight = Between(source, "private void HeBreaksAndRuns(", "private DeckReachability.Point? TheFarEndOfTheGround(");

        foreach (string voice in new[]
        {
            "ShowPulseMessage", "ShowAndFile", "HoldAndFile", "HoldSaying", "FileNote",
            "RaiseStoryBeat", "_storyCard", "ApplyNerveShock", "LogAutopilotEvent",
        })
        {
            Assert.DoesNotContain(voice, flight, StringComparison.Ordinal);
        }

        // …and the guard is reading the right window: the method it swept really is the one that runs.
        Assert.Contains("TheScheduleFalls", flight, StringComparison.Ordinal);
        Assert.Contains("Errand.HardcaseFleeing", flight, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A LIFT RIDE IS NOT LEAVING THE CRATER.</b> He breaks and runs, the captain rides down to B1 and
    /// comes back up, and he is not standing at his post again — because the same trip to the same ground is
    /// one visit, whatever floor of it the captain has been on in the meantime.
    ///
    /// <para><b>Proven RED</b> by keying the visit on the FLOOR as well as the ground, which is what the
    /// first cut of this lane did: the ride down forgets him and the ride up puts a man who has already
    /// bolted back on the apron.</para>
    /// </summary>
    [Fact]
    public void HE_IsNotForgottenByALiftRide()
    {
        Pages.Map map = OnTheRegolith();
        RunFrames(map, 300);
        (double x, double y) = Where(TheOneOnTheGround(map));
        PutAnOldOneAt(map, x + (HardcaseRep.SeesOneAtDu / 2), y);
        RunFrames(map, 600);
        Assert.Empty(Walkers(map));

        RideTheLiftTo(map, TheCanteenFloor);
        RunFrames(map, 200);
        RideTheLiftTo(map, 0);

        // Asked on EVERY frame of the ride back up, not once at the end of it: a man who came back, walked
        // his whole flight again and happened to be off the far edge by the last frame would satisfy a
        // guard that only looked when the run was over — which is a world that cannot tell pass from fail.
        for (int i = 0; i < 600; i++)
        {
            RunFrames(map, 1);
            Assert.Empty(Walkers(map));
            Assert.Null(Card(map));
        }
    }

    // ── THE SHEET ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SHEET IS LYING WHERE HE STOOD, AND PICKING IT UP FILES IT UNDER THE COMPANY.</b> The mark goes
    /// on the deck at the square he broke on; the captain's own [E] takes it; it is in the sleeve, the card is
    /// up with the authored words on it, and the book has an entry whose subjects are the letterhead and the
    /// ground.
    ///
    /// <para><b>Proven RED</b> twice: by returning before <c>ex.HardcaseDropX</c> is written (no mark on the
    /// deck at all), and by filing the note with <c>FileNote</c> instead of <c>FileNoteAbout</c> (an entry
    /// with no subjects, which joins no thread and is the whole point of the find).</para>
    /// </summary>
    [Fact]
    public void THE_SHEET_LiesWhereHeStoodAndFilesUnderTheCompany()
    {
        Pages.Map map = OnTheRegolith();
        RunFrames(map, 300);
        (double x, double y) = Where(TheOneOnTheGround(map));
        PutAnOldOneAt(map, x + (HardcaseRep.SeesOneAtDu / 2), y);
        RunFrames(map, 60);

        var plan = (DeckPlan)Field(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot mark = Assert.Single(
            plan.Consoles, c => c.Label == HardcaseRep.ScheduleLabel);

        // Stand on it and press the key the game presses.
        Set(map, "_avatarX", (double)mark.X);
        Set(map, "_avatarY", (double)mark.Y);
        Invoke(map, "ViewNearbyObject");

        IReadOnlyList<Satchel.Item> sleeve = (IReadOnlyList<Satchel.Item>)Field(map, "_satchel")!;
        Assert.Contains(sleeve, i => HardcaseRep.IsTheSchedule(i.Id) && i.Kind == Satchel.Kind.Paper);

        var card = (DeckPlan.ConsoleSpot?)Field(map, "_viewObject");
        Assert.Equal(HardcaseRep.ScheduleLabel, card!.Value.Label);
        Assert.Equal(HardcaseRep.ScheduleBody, card.Value.Caption);

        FieldNote entry = Assert.Single(Notes(map), n => n.Text == HardcaseRep.ScheduleBody);
        IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(entry);
        Assert.Contains(on, s => s.Of == CaseSubjects.Kind.Office && s.Name == HardcaseRep.Company);
        Assert.Contains(on, s => s.Of == CaseSubjects.Kind.Place);

        // …and the mark comes off the ground with it, because it is not lying there any more.
        var after = (DeckPlan)Field(map, "_deckPlan")!;
        Assert.DoesNotContain(after.Consoles, c => c.Label == HardcaseRep.ScheduleLabel);
    }

    // ── The world ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The canteen floor of this body — asked of the building, never typed in.</summary>
    private static int TheCanteenFloor => UndergroundComplex.TopPressurisedFloor(Body)
                                          ?? throw new InvalidOperationException($"{Body} has no floor.");

    /// <summary>A captain standing on the open regolith of a real landing site, with Kolt forced on to it.
    /// The bench the salesman's own guards stand their worlds on, one floor up.</summary>
    private static Pages.Map OnTheRegolith(int floor = 0)
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Public)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Public)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, floor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_hardcaseCheat", (bool?)true);

        // Out of the tube, on the ground — the one posture the whole beat is about.
        Set(map, "_avatarX", (double)MoonSurface.SpawnX);
        Set(map, "_avatarY", MoonSurface.SpawnY);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Ride the car. The floor is the only thing that changes — the excursion, the site and the
    /// crater are the trip the captain is still on.</summary>
    private static void RideTheLiftTo(Pages.Map map, int floor)
    {
        object ex = Field(map, "_surface")!;
        ex.GetType().GetProperty("Floor", Hidden)!.SetValue(ex, floor);
    }

    /// <summary>Put a real Old One on the ground, as the engine's own list holds them: awake, not dormant,
    /// having already laid eyes on the captain, so nothing about it is a special case.</summary>
    private static void PutAnOldOneAt(Pages.Map map, double x, double y)
    {
        Type reever = typeof(Pages.Map).GetNestedType("Reever", Hidden | BindingFlags.Public)!;
        object one = Activator.CreateInstance(reever, nonPublic: true)!;
        reever.GetField("X", Hidden)!.SetValue(one, x);
        reever.GetField("Y", Hidden)!.SetValue(one, y);
        reever.GetField("EverSeen", Hidden)!.SetValue(one, true);
        ((IList)Field(map, "_reevers")!).Add(one);
    }

    /// <summary>Run his own frame, the way the surface tick runs it.</summary>
    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Invoke(map, "AdvanceTheHardcase", dt);
        }
    }

    /// <summary>The one body on this ground, as an object the guards can read. Asserted single, because a
    /// world with two men in it would make every "he" below ambiguous.</summary>
    private static object TheOneOnTheGround(Pages.Map map)
    {
        IList afoot = Walkers(map);
        Assert.Single(afoot);
        return afoot[0] ?? throw new InvalidOperationException("an empty slot in the walker band.");
    }

    private static IList Walkers(Pages.Map map)
    {
        object? ex = Field(map, "_surface");
        return ex is null ? new List<object>() : (IList)ex.GetType().GetProperty("Walkers", Hidden)!.GetValue(ex)!;
    }

    private static (double X, double Y) Where(object walker) =>
        ((double)Get(Get(walker, "Walk")!, "X")!, (double)Get(Get(walker, "Walk")!, "Y")!);

    private static string Errand(object walker) => Get(walker, "For")!.ToString()!;

    private static bool Arrived(object walker) =>
        Get(Get(walker, "Walk")!, "State")!.ToString() == nameof(NpcWalk.Doing.Arrived);

    private static IReadOnlyList<NebulaRep.RepOffer>? Card(Pages.Map map) =>
        (IReadOnlyList<NebulaRep.RepOffer>?)Field(map, "_hardcaseCard");

    private static IReadOnlyList<FieldNote> Notes(Pages.Map map) =>
        (IReadOnlyList<FieldNote>)Field(map, "_fieldNotes")!;

    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Field(map, "_pulse")!).Message;

    /// <summary>The shipping source of the beat, for the one guard that has to read it. Walked up from the
    /// test binary the way every source-reading law in this suite finds the tree.</summary>
    private static string TheHardcasesSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "SpaceSails.Client")))
        {
            dir = dir.Parent;
        }

        string path = Path.Combine(
            dir?.FullName ?? "", "src", "SpaceSails.Client", "Pages", "Map.Hardcase.cs");
        Assert.True(File.Exists(path), $"this guard cannot find the source it is about ({path}).");
        return File.ReadAllText(path);
    }

    private static string Between(string source, string from, string to)
    {
        int start = source.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"`{from}` is not in the source — this guard is reading a dead name.");
        int end = source.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"`{to}` is not after `{from}` — this guard is reading a dead window.");
        return source[start..end];
    }

    // ── Reflection plumbing ────────────────────────────────────────────────────────────────────────────

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

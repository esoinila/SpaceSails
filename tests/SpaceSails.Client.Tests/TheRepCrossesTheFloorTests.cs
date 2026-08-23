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
/// #973 L2 · HE ACTUALLY CROSSES THE FLOOR — the half no source-shape guard and no reflection on a bare
/// component can see.
///
/// <para>Everything else about Harlan Fess is arithmetic (<c>TheRepNeverRemembersYourFaceTests</c>) or a
/// card (<c>TheRepPitchesAndWithdrawsTests</c>). This is the one that needed a ROOM: a real hive canteen,
/// carved by <c>UndergroundComplex</c>, with real walls, real doors and real tops — and the frame stepped
/// the way the game steps it, until a body that started at a doorstep is standing at the captain's table.</para>
///
/// <h3>Why this file exists, in one sentence</h3>
///
/// <para>Because the first build of him never got on the floor at all, and nothing said so. The walker band
/// was <c>Egress.MostAtOnce</c> — the room's own law about how many REGULARS may be crossing at a time — and
/// on a heaving watch the room's two leavers held both slots for an entire shift, so every attempt he made
/// was refused by a check that was doing exactly what it was written to do. No test failed, no line was
/// logged, and the only symptom was a salesman who never turned up. Watched in a browser and then fixed by
/// giving him a slot of his own; this is what would have caught it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRepCrossesTheFloorTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";
    private const string ThreadId = "b71f4a0c39d24e5ba8027c6f1d3e5490";

    /// <summary>The one floor of this moon that people sit on — asked of the building, never typed in.</summary>
    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
                                  ?? throw new InvalidOperationException($"{Body} has no pressurised floor.");

    // ── The room ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A captain standing on a real canteen floor of a real building, with Fess on the rota.
    ///
    /// <para>The excursion and the deck are built by the component's own <c>RebuildSurfaceDeck</c> off a
    /// stop this test only names — the same bench <c>EveryFrameLeavesTheSameFingerprintTests</c> stands its
    /// worlds on, and for the same reason: a guard handed a world it invented itself cannot tell pass from
    /// fail.</para></summary>
    private static Pages.Map InTheCanteen()
    {
        var map = new Pages.Map();
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
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
        Set(map, "_activeThreadId", ThreadId);
        Set(map, "_threadList", (IReadOnlyList<GameThreadInfo>)[new GameThreadInfo { Id = ThreadId }]);
        Set(map, "_repCheat", true);
        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Sit the captain down alone at one of the room's own tops, through the shipped press.</summary>
    private static void TakeATopAlone(Pages.Map map)
    {
        var plan = (DeckPlan)Field(map, "_deckPlan")!;
        DeckPlan.ConsoleSpot[] tops =
            [.. plan.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.HiveTable)];
        Assert.True(tops.Length > 0, $"{Body} B{-TheFloor} carves no tops — this room has nowhere to sit alone.");

        foreach (DeckPlan.ConsoleSpot top in tops)
        {
            Set(map, "_avatarX", (double)top.X);
            Set(map, "_avatarY", (double)top.Y);
            if (Invoke(map, "TryTakeTable") is true)
            {
                Assert.True((bool)Read(map, "CaptainIsSeated")!, "the press took no chair.");
                Assert.True((bool)Read(map, "SeatedAlone")!, "the captain sat down with company.");
                return;
            }
        }

        Assert.Fail("no free top on this watch — this world has nobody sitting alone in it.");
    }

    /// <summary>Run the room's own frame, the way the surface tick runs it, for a while.</summary>
    private static void RunFrames(Pages.Map map, int frames, double dt = 0.1)
    {
        for (int i = 0; i < frames; i++)
        {
            Set(map, "SimTime", (double)Field(map, "SimTime")! + dt);
            Invoke(map, "AdvanceWalkers", dt);
            Invoke(map, "AdvanceTheRep", dt);
        }
    }

    private static IList Walkers(Pages.Map map)
    {
        object ex = Field(map, "_surface")!;
        return (IList)ex.GetType().GetProperty("Walkers", Hidden)!.GetValue(ex)!;
    }

    private static string[] Errands(Pages.Map map) =>
        [.. Walkers(map).Cast<object>().Select(w => Get(w, "For")!.ToString()!)];

    // ── The claims ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE BAND LEAVES ROOM FOR HIM — the bug this file was written after, as the one line that fixes it.
    ///
    /// <para>The band was <c>Egress.MostAtOnce</c>: the room's own law about how many REGULARS may be
    /// crossing the floor at a time. On a heaving watch the room's leavers held every slot and the salesman
    /// was refused by a check doing exactly what it was written to do — no test failed, nothing was logged,
    /// and the only symptom was a man who never turned up. It reached a browser, which is where it was
    /// found.</para>
    ///
    /// <para>Stated as a CONSTANT law rather than as a staged crowded room, and that is not the easy way
    /// out — it is the only honest one. Whether two of Luna's leavers are ever on their feet at the same
    /// instant is a fact about one moon's rota, so a behavioural version of this claim would pass or fail on
    /// which body the test happened to pick, and a guard that only sometimes guards is worse than none.
    /// What the fix actually was is this inequality, and the five tests below are what say he really walks.</para>
    ///
    /// <para><b>Proven RED</b> by setting <c>WalkerBand = Egress.MostAtOnce</c> again.</para>
    /// </summary>
    [Fact]
    public void TheBandKeepsASlotThatIsNotTheRoomsToFill()
    {
        int band = (int)typeof(Pages.Map)
            .GetField("WalkerBand", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetRawConstantValue()!;

        Assert.True(band > Egress.MostAtOnce,
                    $"the walker band is {band} and the room's own allowance is {Egress.MostAtOnce}. They are "
                    + "the same number, so the room's leavers can hold every slot there is and the salesman "
                    + "has nowhere to stand — which is exactly the bug this file exists for.");
        Assert.Equal(Egress.MostAtOnce + NebulaRep.OnTheFloorAtOnce, band);
    }

    /// <summary>…and the room's own law is still the room's own law: no more than
    /// <see cref="Egress.MostAtOnce"/> REGULARS are ever on their feet at once, whatever the band is.</summary>
    [Fact]
    public void AndTheRoomsOwnLawIsUntouched()
    {
        Pages.Map map = InTheCanteen();

        for (int i = 0; i < 900; i++)
        {
            RunFrames(map, 1);
            int regulars = Errands(map).Count(e => e is not ("RepRounds" or "RepPitching"));
            Assert.True(regulars <= Egress.MostAtOnce,
                        $"{regulars} of the room's own people are crossing the floor at once — "
                        + $"{nameof(Egress.MostAtOnce)} is {Egress.MostAtOnce}");
        }
    }

    /// <summary>
    /// HE CROSSES TO A CAPTAIN SITTING ALONE, AND STANDS THERE WITH A CARD. The whole beat, driven: the
    /// captain takes a top, the frames run, and a body that began at a doorstep ends at the table with the
    /// pitch up.
    ///
    /// <para><b>Proven RED</b> by dropping the sitting-alone branch from <c>SendTheRepIn</c> — he then
    /// drifts round his beat forever and no card is ever raised.</para>
    /// </summary>
    [Fact]
    public void HeCrossesToACaptainSittingAloneAndPitches()
    {
        Pages.Map map = InTheCanteen();
        TakeATopAlone(map);
        RunFrames(map, 2000);

        Assert.NotNull((NebulaRep.RepPitch?)Field(map, "_repCard"));
        Assert.Contains(Errands(map), e => e == "RepPitching");
    }

    /// <summary>
    /// …AND NEVER TO A CAPTAIN WHO IS NOT SITTING DOWN. Standing in the middle of the room is not asking, and
    /// the whole of #757's premise is that sitting alone is the asking.
    ///
    /// <para><b>Proven RED</b> by having <c>TheCaptainIsSittingAlone</c> return true unconditionally.</para>
    /// </summary>
    [Fact]
    public void HeNeverPitchesAtACaptainWhoIsOnTheirFeet()
    {
        Pages.Map map = InTheCanteen();
        RunFrames(map, 2000);

        Assert.Null((NebulaRep.RepPitch?)Field(map, "_repCard"));
        Assert.DoesNotContain(Errands(map), e => e == "RepPitching");
    }

    /// <summary>
    /// HE DOES NOT SIT. There are seven ways to open a sitting in this codebase and a pinned count that says
    /// so; the behavioural half is here, because a source count cannot see a page that seated somebody
    /// through a chair it did not construct. After the full crossing the captain's own seat is untouched and
    /// the room's tops are as free as they were.
    /// </summary>
    [Fact]
    public void HeStandsAtYourTableAndNeverTakesTheChair()
    {
        Pages.Map map = InTheCanteen();
        TakeATopAlone(map);
        object? seatBefore = Read(map, "SeatedTable");
        RunFrames(map, 2000);

        Assert.Same(seatBefore, Read(map, "SeatedTable"));
        Assert.True((bool)Read(map, "SeatedAlone")!,
                    "the captain is no longer alone at the table — the salesman took a chair he was not offered.");
    }

    /// <summary>
    /// TOLD NO, HE GOES — and the floor is what says so, not a flag. After the refusal he is either off the
    /// table or back on his rounds, and no pitch comes back for the rest of the visit.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>_repMemory.MayApproach</c> check from
    /// <c>SendTheRepIn</c>: he crosses back and raises the card again within the same visit.</para>
    /// </summary>
    [Fact]
    public void ToldNoHeWithdrawsAndTheVisitIsOver()
    {
        Pages.Map map = InTheCanteen();
        TakeATopAlone(map);
        RunFrames(map, 2000);
        Assert.NotNull((NebulaRep.RepPitch?)Field(map, "_repCard"));

        Invoke(map, "AnswerTheRep", NebulaRep.RepMove.NotToday);
        Assert.Null((NebulaRep.RepPitch?)Field(map, "_repCard"));

        RunFrames(map, 3000);
        Assert.Null((NebulaRep.RepPitch?)Field(map, "_repCard"));
        Assert.DoesNotContain(Errands(map), e => e == "RepPitching");
    }

    // ── Reflection plumbing ────────────────────────────────────────────────────────────────────────────

    private static FieldInfo FieldOf(string name) =>
        typeof(Pages.Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no `{name}` — this guard is reading a dead name.");

    private static object? Field(Pages.Map map, string name) => FieldOf(name).GetValue(map);

    private static void Set(Pages.Map map, string name, object? value) => FieldOf(name).SetValue(map, value);

    private static object? Get(object o, string member) => o.GetType().GetProperty(member, Hidden)!.GetValue(o);

    private static object? Read(Pages.Map map, string member) =>
        typeof(Pages.Map).GetProperty(member, Hidden)!.GetValue(map);

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

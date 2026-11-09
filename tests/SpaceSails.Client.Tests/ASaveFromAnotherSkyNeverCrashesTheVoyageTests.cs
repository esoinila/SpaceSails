using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1388 · <b>A SOL SAVE CONTINUED FROM ANOTHER SKY'S LOGBOOK IS REFUSED WITH A SENTENCE, NEVER A THROW.</b>
/// A sky without a <c>selene-gate</c>; the save's resume berth does. <c>ApplyResumeBerth</c> fell back to
/// <c>ApplyStart("earth")</c>, whose docked start maps to the missing body, and <c>BerthState.CoMoving</c>
/// raised <c>KeyNotFoundException</c> onto the red error page. Two halves are pinned: the placement can no
/// longer throw on a sky lacking its anchor, and the Continue row is not offered for a save the sky cannot hold.
/// </summary>
public sealed class ASaveFromAnotherSkyNeverCrashesTheVoyageTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Only Sol ships now, so "another sky" is Sol with its Selene Gate taken out of the ephemeris —
    /// what a save from a since-retired world (Miners' Folly, the Wheel) looks like to the one sky left.</summary>
    private static async Task<PastTheGateBench.Boot> BootASkyWithoutSeleneGateAsync()
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync("/map");
        ICelestialEphemeris sol = booted.Read<ICelestialEphemeris>("_ephemeris");
        typeof(Map).GetField("_ephemeris", Hidden)!.SetValue(booted.Page,
            new CircularOrbitEphemeris([.. sol.Bodies.Where(b => b.Id != "selene-gate")]));
        return booted;
    }

    private static Vault ASolSaveAtSeleneGate() => new()
    {
        SavedSimTime = 1000,
        Resume = new ResumeSection { HavenId = "selene-gate", HavenName = "Selene Gate", WasDocked = true },
    };

    private static object? Call(Map page, string name, params object?[] args) =>
        typeof(Map).GetMethod(name, Hidden)!.Invoke(page, args);

    /// <summary>The refusal half of #1216 case 2, restored after the Electric sky it used to be proved in was
    /// retired: a start whose anchor body the loaded sky lacks is refused (the boot then shows the picker),
    /// and the placement beneath it lays the ship at the standard spawn instead of throwing. Both halves are
    /// asked of the same sky with and without its Selene Gate, so a check that said yes to everything fails.</summary>
    [Fact]
    public async Task AStartThisSkyCannotAnchorIsRefusedToThePickerAndPlacedWithoutThrowing()
    {
        PastTheGateBench.Boot sol = await PastTheGateBench.BootAsync("/map");
        Assert.Null(sol.Threw);
        Assert.True((bool)Call(sol.Page, "ThisSkyCanHonourTheStart", "earth")!,
            "Sol holds every body the start registry names, so nothing may be refused there.");

        PastTheGateBench.Boot bare = await BootASkyWithoutSeleneGateAsync();
        Assert.False((bool)Call(bare.Page, "ThisSkyCanHonourTheStart", "earth")!,
            "this sky has no Selene Gate, so the start that hangs off it must be refused to the picker.");
        Exception? threw = null;
        try { Call(bare.Page, "PlaceShipForStart", "earth"); }
        catch (TargetInvocationException ex) { threw = ex.InnerException; }
        Assert.Null(threw);
    }

    [Fact]
    public async Task ASolSaveAppliedInAnotherSkyDoesNotThrow()
    {
        PastTheGateBench.Boot booted = await BootASkyWithoutSeleneGateAsync();
        Assert.Null(booted.Threw);
        Assert.DoesNotContain("selene-gate",
            booted.Read<ICelestialEphemeris>("_ephemeris").Bodies.Select(b => b.Id)); // the premise

        Exception? threw = null;
        try { Call(booted.Page, "ApplyVault", ASolSaveAtSeleneGate()); }
        catch (TargetInvocationException ex) { threw = ex.InnerException; }

        Assert.Null(threw);
    }

    [Fact]
    public async Task TheContinueRowIsOfferedOnlyForASaveThisSkyCanHold()
    {
        foreach ((string url, bool expected) in new[] { ("a sky without Selene Gate", false), ("/map", true) })
        {
            PastTheGateBench.Boot booted = expected
                ? await PastTheGateBench.BootAsync(url)
                : await BootASkyWithoutSeleneGateAsync();
            Assert.Null(booted.Threw);
            typeof(Map).GetField("_resumeAvailable", Hidden)!.SetValue(booted.Page, true);
            typeof(Map).GetField("_pendingResumeVault", Hidden)!.SetValue(booted.Page, ASolSaveAtSeleneGate());

            bool offered = (bool)typeof(Map).GetProperty("ContinueIsOffered", Hidden)!.GetValue(booted.Page)!;
            Assert.True(offered == expected, $"{url}: Continue offered = {offered}, expected {expected}");
        }
    }

    private sealed class MemStore : ISlotStore
    {
        private readonly System.Collections.Generic.Dictionary<string, string> _map = new(StringComparer.Ordinal);
        public string? Read(string key) => _map.TryGetValue(key, out string? v) ? v : null;
        public void Write(string key, string value) => _map[key] = value;
        public void Clear(string key) => _map.Remove(key);
    }

    private static void Set(Map page, string field, object? value) =>
        typeof(Map).GetField(field, Hidden)!.SetValue(page, value);

    private static void Bank(Map page, string slotId, Vault vault, long ticks)
    {
        var book = (SaveSlotBook)typeof(Map).GetProperty("Slots", Hidden)!.GetValue(page)!;
        var meta = (SaveSlotMeta)Call(page, "BuildSlotMeta", vault, SaveSlotKind.Manual)!;
        book.Save(slotId, VaultSerializer.Save(vault), meta with { SavedRealTicks = ticks });
    }

    private static async Task<Map> ASkyWithoutSeleneGateAndAnEmptyShelf()
    {
        PastTheGateBench.Boot booted = await BootASkyWithoutSeleneGateAsync();
        Assert.Null(booted.Threw);
        Set(booted.Page, "_activeThreadId", "t1");
        Set(booted.Page, "_slots", new SaveSlotBook(new MemStore(), "t1"));
        Set(booted.Page, "_slotsBoundThreadId", "t1");
        return booted.Page;
    }

    private static Vault ASaveThisSkyCanHold(Map page) => new()
    {
        SavedSimTime = 2000,
        Resume = new ResumeSection
        {
            HavenId = ((ICelestialEphemeris)typeof(Map).GetField("_ephemeris", Hidden)!.GetValue(page)!).Bodies[0].Id,
            HavenName = "Somewhere this sky holds", WasDocked = true,
        },
    };

    [Fact]
    public async Task TheDrawerOffersContinueOnceANewVoyageHasAutosavedOverTheSolSave()
    {
        // Boot peek found the Sol save; then a voyage this sky can hold autosaved; then the logbook opens from the desk
        // (or the death card). Continue loads the NEWEST save - so it is judged on the newest save.
        Map page = await ASkyWithoutSeleneGateAndAnEmptyShelf();
        Bank(page, "auto", ASolSaveAtSeleneGate(), ticks: 1);
        Call(page, "OpenSaveDrawer");
        Assert.False((bool)typeof(Map).GetProperty("ContinueIsOffered", Hidden)!.GetValue(page)!, "the Sol save is newest");

        Bank(page, "auto", ASaveThisSkyCanHold(page), ticks: 2);
        Call(page, "OpenSaveDrawer");
        Assert.True((bool)typeof(Map).GetProperty("ContinueIsOffered", Hidden)!.GetValue(page)!,
            "the newest save is a save this sky can hold");
    }

    [Fact]
    public async Task ARefusedLoadSaysSoLeavesTheWorldAloneAndKeepsThePickerUp()
    {
        Map page = await ASkyWithoutSeleneGateAndAnEmptyShelf();
        Bank(page, "auto", ASolSaveAtSeleneGate(), ticks: 1);
        Set(page, "_showStartPicker", true);
        object shipBefore = typeof(Map).GetField("_ship", Hidden)!.GetValue(page)!;

        await (Task)Call(page, "LoadSlot", "auto")!;

        Assert.True((bool)typeof(Map).GetField("_showStartPicker", Hidden)!.GetValue(page)!, "the picker was closed by a refused load");
        Assert.Equal(shipBefore, typeof(Map).GetField("_ship", Hidden)!.GetValue(page));
        object pulse = typeof(Map).GetField("_pulse", Hidden)!.GetValue(page)!;
        string? message = (string?)pulse.GetType().GetProperty("Message")!.GetValue(pulse);
        Assert.Contains("does not hold", message);
    }
}

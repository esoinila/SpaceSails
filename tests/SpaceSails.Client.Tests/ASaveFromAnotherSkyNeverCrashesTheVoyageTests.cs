using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1388 · <b>A SOL SAVE CONTINUED FROM THE WHEEL'S LOGBOOK IS REFUSED WITH A SENTENCE, NEVER A THROW.</b>
/// The Wheel sky holds no <c>selene-gate</c>; the save's resume berth does. <c>ApplyResumeBerth</c> fell back to
/// <c>ApplyStart("earth")</c>, whose docked start maps to the missing body, and <c>BerthState.CoMoving</c>
/// raised <c>KeyNotFoundException</c> onto the red error page. Two halves are pinned: the placement can no
/// longer throw on a sky lacking its anchor, and the Continue row is not offered for a save the sky cannot hold.
/// </summary>
public sealed class ASaveFromAnotherSkyNeverCrashesTheVoyageTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static Vault ASolSaveAtSeleneGate() => new()
    {
        SavedSimTime = 1000,
        Resume = new ResumeSection { HavenId = "selene-gate", HavenName = "Selene Gate", WasDocked = true },
    };

    private static object? Call(Map page, string name, params object?[] args) =>
        typeof(Map).GetMethod(name, Hidden)!.Invoke(page, args);

    [Fact]
    public async Task ASolSaveAppliedInTheWheelsSkyDoesNotThrow()
    {
        PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync("/map?scenario=wheel");
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
        foreach ((string url, bool expected) in new[] { ("/map?scenario=wheel", false), ("/map", true) })
        {
            PastTheGateBench.Boot booted = await PastTheGateBench.BootAsync(url);
            Assert.Null(booted.Threw);
            typeof(Map).GetField("_resumeAvailable", Hidden)!.SetValue(booted.Page, true);
            typeof(Map).GetField("_pendingResumeVault", Hidden)!.SetValue(booted.Page, ASolSaveAtSeleneGate());

            bool offered = (bool)typeof(Map).GetProperty("ContinueIsOffered", Hidden)!.GetValue(booted.Page)!;
            Assert.True(offered == expected, $"{url}: Continue offered = {offered}, expected {expected}");
        }
    }
}

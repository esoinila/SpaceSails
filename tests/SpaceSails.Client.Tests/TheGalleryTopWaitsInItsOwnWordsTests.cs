using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// U09-F05 · <b>SIT A WHILE AT A SELENE GATE OBSERVATION-WALK TOP</b> says the gallery's own silence, and the
/// bar's tops keep the hall's. Driven on a live page clamped on at the Gate: [E] at a table, the card's own
/// WAIT press, the sitting's outcome read off the panel.
///
/// <para><b>Proven RED</b> by deleting the <c>gallery:</c> arm in <c>Seating.Table.Wait.cs</c>: the gallery top
/// answers from the hall's pool and the first guard fails.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGalleryTopWaitsInItsOwnWordsTests
{
    private static Pages.Map Clamped(string canvas)
    {
        Pages.Map map = Boot(canvas);
        var sky = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        CelestialBody berth = sky.Bodies.First(b => b.Id == Port);
        Invoke(map, "ClampOntoHaven", berth, sky.Position(Port, (double)Read(map, "SimTime")!), null);
        Assert.Equal(Port, (string?)Read(map, "_dockedHavenId"));
        return map;
    }

    private static async System.Threading.Tasks.Task<string> WaitAt(Pages.Map map, DeckReachability.Point top)
    {
        Set(map, "_avatarX", top.X);
        Set(map, "_avatarY", top.Y);
        Assert.True((bool)Invoke(map, "TryTakeBarTop")!, "[E] at the table was refused.");
        var t = (Pages.Map.TableTalk)Read(map, "SeatedTable")!;
        await (System.Threading.Tasks.Task)Invoke(map, "TableMoveClicked", SittingAlone.Wait)!;
        return t.Outcome ?? "";
    }

    [Fact]
    public async System.Threading.Tasks.Task TheGalleryTopWaitsInTheGalleryPool()
    {
        Pages.Map map = Clamped("gallery-wait");
        string said = await WaitAt(map, HavenInterior.GalleryTops(Port)[0]);
        Assert.Contains(SittingAlone.NobodyCameGalleryTop, l => said.Contains(l, StringComparison.Ordinal));
    }

    [Fact]
    public async System.Threading.Tasks.Task ABarTopKeepsTheHallsPoolAndNeverTheGalleries()
    {
        Pages.Map map = Clamped("gallery-wait-bar");
        object bar = Invoke(map, "TheDockedBar")!;
        var tops = (IReadOnlyList<DeckReachability.Point>)bar.GetType().GetProperty("Tops")!.GetValue(bar)!;
        string said = "";
        foreach (DeckReachability.Point top in tops)
        {
            Set(map, "_avatarX", top.X);
            Set(map, "_avatarY", top.Y);
            if (!(bool)Invoke(map, "TryTakeBarTop")!)
            {
                continue;
            }

            var t = (Pages.Map.TableTalk)Read(map, "SeatedTable")!;
            await (System.Threading.Tasks.Task)Invoke(map, "TableMoveClicked", SittingAlone.Wait)!;
            said = t.Outcome ?? "";
            break;
        }

        Assert.NotEqual("", said);
        Assert.DoesNotContain(SittingAlone.NobodyCameGalleryTop, l => said.Contains(l, StringComparison.Ordinal));
    }

    [Fact]
    public void TheGalleryPoolRotatesByBeatAndIsInTheCanonSweep()
    {
        Assert.NotEqual(SittingAlone.NobodyCameAtTheGalleryTop(0), SittingAlone.NobodyCameAtTheGalleryTop(1));
        Assert.Equal(SittingAlone.NobodyCameAtTheGalleryTop(0), SittingAlone.NobodyCameAtTheGalleryTop(4));
        foreach (string l in SittingAlone.NobodyCameGalleryTop)
        {
            Assert.Contains(l, SittingAlone.AllProse());
        }
    }
}

using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>#1385 / #1393 · Where the man at the door stands, and what keeps him there. Each fact names the
/// revert that turned it RED.</summary>
public sealed partial class TheManAtTheDoorTests
{
    /// <summary>
    /// #1393 · <b>NO CAR TAKES THE CAPTAIN BELOW HIS FLOOR WITHOUT PASSING HIM — NOT EVEN FROM THE SURFACE.</b>
    /// Standing at SURFACE with the door unmet, the panel is SURFACE and his floor and nothing below; once he is
    /// known absent, passed or gone, the whole band is back.
    ///
    /// <para><b>Proven RED</b> by deleting the above-his-floor branch of <c>PastTheManAtTheDoor</c>: the surface
    /// panel listed every floor of the band.</para>
    /// </summary>
    [Fact]
    public void TheSurfacePanelStopsAtHisFloorUntilHeIsPassed()
    {
        (string body, int floor) = ASiteWithADoor();
        int deepest = UndergroundComplex.FloorsOf(body).Min();
        Assert.True(deepest < floor, "the premise: the band runs below his floor");

        Pages.Map map = OnHisFloor(body, floor, GateGuard.Posting.Posted);
        Ex(map).Floor = 0;     // back at the lid
        Ex(map).Gate = null;   // …before he has ever been met

        var unmet = Rows(map).Select(r => r.Level).ToList();
        Assert.Contains(0, unmet);
        Assert.Contains(floor, unmet);
        Assert.DoesNotContain(unmet, l => l < floor);

        // Met and posted, not passed: still his.
        Ex(map).Gate = new Pages.Map.ManAtTheDoor
        {
            Ground = body, Window = Watch, Visit = new GateGuard.Visit(GateGuard.Posting.Posted, false),
            Floor = floor, Post = new DeckReachability.Point(0, 0),
        };
        Assert.DoesNotContain(Rows(map).Select(r => r.Level), l => l < floor);

        // Passed: the surface panel lists the whole band again.
        Man(map).Passed = true;
        Assert.Contains(Rows(map), r => r.Level < floor);

        // …and an empty chair never held it in the first place.
        Ex(map).Gate = new Pages.Map.ManAtTheDoor
        {
            Ground = body, Window = Watch, Visit = new GateGuard.Visit(GateGuard.Posting.Absent, false),
            Floor = floor, Post = new DeckReachability.Point(0, 0),
        };
        Assert.Contains(Rows(map), r => r.Level < floor);
    }

    /// <summary>
    /// #1385 · <b>THE HEAD OFFICE KEEPS NO MAN AT THE DOOR.</b> B1 of the head office is RECEPTION: nobody is
    /// seeded, and the lift offers the whole building from there and from the lid.
    ///
    /// <para><b>Proven RED</b> by removing the <c>IsHeadOffice</c> line from <c>MeetTheDoorIfThisIsHisFloor</c>:
    /// <c>ex.Gate</c> was a man and the B1 panel stopped at SURFACE and B1.</para>
    /// </summary>
    [Fact]
    public void TheHeadOfficeKeepsNoManAtTheDoor()
    {
        string hq = UndergroundComplex.IsHeadOffice("enceladus") ? "enceladus" : KaamosLore.IceMoonBodyId;
        int top = UndergroundComplex.TopPressurisedFloor(hq)!.Value;
        Assert.True(UndergroundComplex.FloorsOf(hq).Any(f => f < top), "the premise: the head office runs below B1");

        var map = new Pages.Map();
        typeof(Microsoft.AspNetCore.Components.ComponentBase)
            .GetField("_hasPendingQueuedRender", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(map, true);
        Set(map, "_deckMode", true);
        var ex = new Pages.Map.SurfaceExcursion
        {
            Stop = new Pages.Map.ShuttleStop(
                new CelestialBody(hq, hq, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false),
            RestoreHavenId = null,
            Site = new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""),
        };
        ex.Floor = top;
        ex.CanteenWatch = Watch;
        Set(map, "_surface", ex);
        Invoke(map, "RebuildSurfaceDeck");

        Assert.Null(ex.Gate);
        Assert.Contains(Rows(map), r => r.Level < top);
        ex.Floor = 0;
        Assert.Contains(Rows(map), r => r.Level < top); // and from the lid, no trim either
    }
}

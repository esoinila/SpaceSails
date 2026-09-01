using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #650 · THE ✗ IS DRAWN ON THE GROUND THE CHEST IS ACTUALLY UNDER.
///
/// <para>The surface's coordinates are LOCAL to the deck, and #320 gave every body 2–4 landing sites that all
/// rebuild that same local frame. The cache marks were filtered by body alone, so a chest buried out on the
/// Wild Plain planted a red ✗ — and a live "🗺 DIG AT THE X" console — at the identical x/y on the Ridge Camp,
/// on ground the captain had never walked, over whatever the seeded generator had put there.</para>
///
/// <para>This is the client half of the ruling: the Core seam is guarded in <c>ACacheIsBuriedOnAGroundTests</c>;
/// here the projection the deck is actually built from (<see cref="MoonSurface.OwnCacheMarks"/>, which
/// <c>Map.OwnCachePositionsAt</c> is now nothing but a call to) is run against two REAL, DIFFERENT grounds of
/// the same moon and the deck is inspected for the dig console it grows.</para>
///
/// <para><b>Both directions, always.</b> "No ✗ on the wrong site" is a sentence that a broken build satisfies
/// by drawing no ✗ anywhere, so every assertion below is paired with its opposite on the right ground, and the
/// first test proves the two grounds are not the same ground.</para>
/// </summary>
public class TheXIsOnTheGroundItIsUnderTests
{
    // Phobos offers four sites, and site 2 is literally "The Ridge Camp" — the ground the owner's ruling names.
    private const string Body = "phobos";
    private const int WildPlain = 0;
    private const int RidgeCamp = 2;

    private static DeckPlan DeckFor(CacheLedger caches, int siteIndex)
    {
        LandingSite site = LandingSites.At(Body, siteIndex);
        return MoonSurface.SurfaceDeck(
            Body, Body, MoonSurface.OwnCacheMarks(caches, Body, siteIndex),
            droidCount: 0, fillDroids: static (_, _) => { },
            siteSalt: site.LayoutSalt, siteName: site.Name);
    }

    private static List<DeckPlan.ConsoleSpot> DigSites(DeckPlan deck) =>
        deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.DigSite).ToList();

    /// <summary>The premise, asserted rather than assumed: these are two different places on one moon, with
    /// different names and different walls. Without this every "absent" assertion below is worth nothing.</summary>
    [Fact]
    public void TheTwoSitesAreTwoDifferentGrounds()
    {
        Assert.True(LandingSites.Count(Body) > RidgeCamp);
        Assert.Equal("The Wild Plain", LandingSites.At(Body, WildPlain).Name);
        Assert.Equal("The Ridge Camp", LandingSites.At(Body, RidgeCamp).Name);

        DeckPlan plain = DeckFor(new CacheLedger(), WildPlain);
        DeckPlan ridge = DeckFor(new CacheLedger(), RidgeCamp);
        Assert.NotEqual(
            string.Join('|', plain.Walls.Select(w => $"{w.X1:F2},{w.Y1:F2},{w.X2:F2},{w.Y2:F2}")),
            string.Join('|', ridge.Walls.Select(w => $"{w.X1:F2},{w.Y1:F2},{w.X2:F2},{w.Y2:F2}")));

        // …and with an empty hoard neither ground grows a dig console, so a stray ✗ below is ours.
        Assert.Empty(DigSites(plain));
        Assert.Empty(DigSites(ridge));
    }

    /// <summary>THE BUG. Bury on the Ridge Camp; the ✗ and its "DIG AT THE X" console stand on the Ridge Camp
    /// and are absent from the Wild Plain — where, before this, they stood at the very same coordinates.</summary>
    [Fact]
    public void AChestBuriedOnOneSiteDrawsItsXThereAndNowhereElseOnTheMoon()
    {
        var caches = new CacheLedger();
        TreasureCache chest = caches.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: 4.5, digY: -30.0, siteIndex: RidgeCamp);

        // On the ground it is under: one mark, at the spot the shovel bit in.
        List<(string Id, double X, double Y, int ReeverLevel)> here = MoonSurface.OwnCacheMarks(caches, Body, RidgeCamp);
        (string Id, double X, double Y, int ReeverLevel) mark = Assert.Single(here);
        Assert.Equal(chest.Id, mark.Id);
        Assert.Equal(4.5, mark.X);
        Assert.Equal(-30.0, mark.Y);

        DeckPlan ridge = DeckFor(caches, RidgeCamp);
        DeckPlan.ConsoleSpot dig = Assert.Single(DigSites(ridge));
        Assert.Equal(4.5f, dig.X, 3);
        Assert.Equal(-30.0f, dig.Y, 3);

        // On the other ground of the same moon: no mark, and no console to press [E] on.
        Assert.Empty(MoonSurface.OwnCacheMarks(caches, Body, WildPlain));
        Assert.Empty(DigSites(DeckFor(caches, WildPlain)));
    }

    /// <summary>Two chests, two grounds, one moon: each site shows its own and only its own. A filter that
    /// merely dropped the marks would fail this; so would one that showed both everywhere.</summary>
    [Fact]
    public void EachGroundShowsItsOwnChest()
    {
        var caches = new CacheLedger();
        TreasureCache onPlain = caches.Bury(
            Body, coin: 400, [], simTime: 10000, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: -12.0, digY: -20.0, siteIndex: WildPlain);
        TreasureCache onRidge = caches.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: 4.5, digY: -30.0, siteIndex: RidgeCamp);

        Assert.Equal([onPlain.Id], MoonSurface.OwnCacheMarks(caches, Body, WildPlain).Select(m => m.Id));
        Assert.Equal([onRidge.Id], MoonSurface.OwnCacheMarks(caches, Body, RidgeCamp).Select(m => m.Id));
        Assert.Single(DigSites(DeckFor(caches, WildPlain)));
        Assert.Single(DigSites(DeckFor(caches, RidgeCamp)));
    }

    /// <summary>A chest already in the ground before #650 has no site, so it keeps today's deal exactly: its ✗
    /// stands on every ground of its body. Nothing a captain buried last week becomes undiggable.</summary>
    [Fact]
    public void ABodyWideChestStillDrawsItsXOnEveryGround()
    {
        var caches = new CacheLedger();
        TreasureCache old = caches.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true,
            reeverLevel: 0, digX: 4.5, digY: -30.0);
        Assert.False(old.HasSite);

        for (int site = 0; site < LandingSites.Count(Body); site++)
        {
            Assert.Equal([old.Id], MoonSurface.OwnCacheMarks(caches, Body, site).Select(m => m.Id));
            Assert.Single(DigSites(DeckFor(caches, site)));
        }
    }

    /// <summary>A rival's chest — a rumour map we hold — never draws OUR ✗, on any ground. The older half of
    /// this projection, kept honest while the site filter was welded into it.</summary>
    [Fact]
    public void ARivalsChestDrawsNoMarkOfOurs()
    {
        var caches = new CacheLedger();
        caches.Learn(CacheMint.Bury(
            "rumor-x", Body, mintIndex: 3, coin: 700, [], buriedSimTime: 500,
            owner: "Old Vane", playerOwned: false, siteIndex: RidgeCamp));

        Assert.Empty(MoonSurface.OwnCacheMarks(caches, Body, RidgeCamp));
        Assert.Empty(DigSites(DeckFor(caches, RidgeCamp)));
    }
}

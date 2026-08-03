using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #649 · SHOW ITS SIZE. Owner, 2026-08-03: <i>"The Phobos one's dimensions were huge and it should not
/// live in a boxed backyard but show more of its size and not having been built by us at least."</i>
///
/// <para>The game has held the object's real size since #164 — <see cref="Landmarks.PhobosMonolith"/>
/// records 85 m — and drew it at six deck units. Four metres. The fact and the picture disagreeing by a
/// factor of twenty, in the open, for months: that is bug class 1 with the wrongness baked in before the
/// literal was even typed, and the reason none of these guards asserts a number.</para>
///
/// <para>These walk the REAL surface deck the captain's boots collide with. Core can prove the arithmetic;
/// only this project can prove that what got DRAWN is what the arithmetic said.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheMonolithShowsItsSizeTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    private static DeckPlan DeckFor(string body, LandingSite site, bool hasSecretSite = false) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name,
            hasSecretSite: hasSecretSite);

    private static DeckPlan TheMonolithsGround(bool hasSecretSite = false) =>
        DeckFor(Monolith.BodyId, LandingSites.At(Monolith.BodyId, 0), hasSecretSite);

    /// <summary>THE TWO PROJECTS AGREE HOW BIG A CAPTAIN IS. Core cannot see <c>DeckPlan</c>, so
    /// <see cref="SurfaceScale"/> mirrors the avatar's width — and a mirror nobody checks is exactly how the
    /// field envelope drifted sixteenfold while every audit went on measuring the old world and passing
    /// (#573). Everything the monolith measures hangs off this one number.</summary>
    [Fact]
    public void TheCaptainIsTheSameWidthInBothProjects()
    {
        Assert.Equal(DeckPlan.AvatarRadius * 2, SurfaceScale.CaptainWidthDu, 9);

        // And the conversion is the honest one: a suited human is about a metre across, so the walked field
        // is a couple of hundred metres of regolith rather than a car park or a county.
        Assert.InRange(SurfaceScale.Metres(SurfaceScale.CaptainWidthDu), 0.8, 1.2);
        Assert.InRange(
            SurfaceScale.Metres(SurfaceLayout.DefaultField.RightX - SurfaceLayout.DefaultField.LeftX),
            120.0, 400.0);
    }

    /// <summary>IT IS DRAWN AS ONE UNBROKEN MASS, which is the "not having been built by us" in the picture.
    ///
    /// <para>Every other solid object on every moon is drawn as a hatched outline — the established idiom
    /// for piled regolith, and the reason a plinth reads as mass rather than as a suspiciously thick room.
    /// At the monolith's real size that hatch is forty-nine parallel strokes across the one object in the
    /// game whose own card says <i>"No seam"</i>: masonry, drawn onto the thing nobody built. So the mass
    /// still collides through and none of it is painted; the picture is a single filled rectangle.</para></summary>
    [Fact]
    public void TheMonolithIsDrawnAsOneUnbrokenMassAndNothingElseIs()
    {
        DeckPlan deck = TheMonolithsGround();

        DeckPlan.Structure slab = Assert.Single(deck.Structures);
        Assert.Equal(MoonSurface.AnchorX - Monolith.HalfWidth, Math.Min(slab.X0, slab.X1), 3);
        Assert.Equal(MoonSurface.AnchorX + Monolith.HalfWidth, Math.Max(slab.X0, slab.X1), 3);
        Assert.Equal(MoonSurface.AnchorY - Monolith.HalfHeight, Math.Min(slab.Y0, slab.Y1), 3);
        Assert.Equal(MoonSurface.AnchorY + Monolith.HalfHeight, Math.Max(slab.Y0, slab.Y1), 3);

        // No JOIN anywhere in it: not one drawn segment lies strictly inside the face. The collision hatch is
        // still there (the next guard proves it), it is simply never painted.
        double x0 = MoonSurface.AnchorX - Monolith.HalfWidth + 0.2;
        double x1 = MoonSurface.AnchorX + Monolith.HalfWidth - 0.2;
        double y0 = MoonSurface.AnchorY - Monolith.HalfHeight + 0.2;
        double y1 = MoonSurface.AnchorY + Monolith.HalfHeight - 0.2;
        Assert.DoesNotContain(deck.Walls, w =>
            !w.Unseen
            && w.X1 > x0 && w.X1 < x1 && w.X2 > x0 && w.X2 < x1
            && w.Y1 > y0 - 0.3 && w.Y1 < y1 + 0.3);

        // And no other ground in the game has a filled mass, because no other ground has one of these.
        foreach ((string body, LandingSite site) in EverySite())
        {
            if (Monolith.StandsOn(body, site.LayoutSalt))
            {
                continue;
            }
            Assert.Empty(DeckFor(body, site).Structures);
        }
    }

    /// <summary>AND YOU STILL CANNOT STAND INSIDE IT. The hatch that stops a body being held inside a solid
    /// is a collision property; not drawing it is a picture property. Dropping the picture must not drop the
    /// collision — that would be 99 cells of sealed ground the reachability flood would find, or worse, a
    /// captain wedged inside the monolith.</summary>
    [Fact]
    public void TheMassStillCollidesThroughEvenThoughNoneOfItIsPainted()
    {
        DeckPlan deck = TheMonolithsGround();

        // Step across the whole face at closer than a captain's width and count the hatch strokes crossed.
        int strokes = deck.Walls.Count(w =>
            Math.Abs(w.X1 - w.X2) < 0.001
            && w.X1 > MoonSurface.AnchorX - Monolith.HalfWidth + 0.1
            && w.X1 < MoonSurface.AnchorX + Monolith.HalfWidth - 0.1
            && Math.Abs(Math.Abs(w.Y1 - w.Y2) - (Monolith.HalfHeight * 2)) < 0.1);

        Assert.True(strokes >= (int)(Monolith.HalfWidth * 2 / SurfaceScale.CaptainWidthDu),
            $"only {strokes} strokes hatch a slab {Monolith.HalfWidth * 2:0.0} du wide — there is a gap in it "
            + "big enough to hold a captain.");
        Assert.All(deck.Walls.Where(w => w.Unseen && w.IsStone), w =>
            Assert.True(Math.Abs(w.X1 - w.X2) < 0.001, "an unpainted stone segment that is not a hatch stroke"));
    }

    /// <summary>THE SHADOW, which is the only way a top-down plan can say TALL. Owner: it must <i>"read as
    /// large from a long way off, and keep getting larger as you walk."</i> A footprint cannot do that — the
    /// camera holds about 64 du, so from the landing band even a slab this big is off-screen. What reaches
    /// every square of the field is what a 121 du object does to the light.</summary>
    [Fact]
    public void TheShadowReachesFromTheStoneToTheGroundYouLandOn()
    {
        DeckPlan deck = TheMonolithsGround();

        double face = MoonSurface.AnchorY + Monolith.HalfHeight;
        var shade = deck.Scenery.Where(m =>
            Math.Abs(Math.Min(m.Y1, m.Y2) - face) < 0.001 && Math.Max(m.Y1, m.Y2) > face).ToList();

        Assert.True(shade.Count >= 5, "the shadow is too thin a scatter of strokes to read as shade.");

        // It starts at the lit face and it runs all the way to the ground the shuttle sets you down on:
        // there is nowhere on this moon you can stand and not be in it.
        Assert.Contains(shade, m => Math.Max(m.Y1, m.Y2) >= MoonSurface.LandingBandY - 0.001);

        // It is a REGION, not a channel. A rille is two banks; this must never be read as one.
        double widest = shade.Max(m => Math.Abs(m.X1 - MoonSurface.AnchorX)) * 2;
        Assert.True(widest >= Monolith.HalfWidth * 2,
            "the shadow is narrower than the thing casting it.");

        // And only this ground has one.
        foreach ((string body, LandingSite site) in EverySite())
        {
            if (Monolith.StandsOn(body, site.LayoutSalt))
            {
                continue;
            }
            DeckPlan other = DeckFor(body, site);
            Assert.DoesNotContain(other.Scenery, m =>
                Math.Abs(Math.Max(m.Y1, m.Y2) - Math.Min(m.Y1, m.Y2)) > 100);
        }
    }

    /// <summary>THE CARD MEETS YOU ON THE SIDE YOU WALK FROM. The console sat DEEP of the slab, which was
    /// harmless at six deck units and is a fifty-du walk around solid rock at the real one — an affordance
    /// you can see and cannot reach is worse than none (#212), and this is the one at the end of the longest
    /// walk in the game.</summary>
    [Fact]
    public void EverythingYouCameForIsOnTheApproachSide()
    {
        // A window with something at the foot, so the offering console is built too. Epoch 8 is one; the
        // guard picks it by asking, never by assuming.
        long window = Enumerable.Range(0, 64)
            .First(e => Monolith.AtTheFoot(Monolith.BodyId, "", e) != Monolith.Offering.Nothing);

        DeckPlan deck = MoonSurface.SurfaceDeck(
            Monolith.BodyId, Monolith.BodyId, [], 0, (_, _) => { },
            "", LandingSites.At(Monolith.BodyId, 0).Name, window);

        var mine = deck.Consoles.Where(c =>
            c.Kind == DeckPlan.ConsoleKind.MonolithFoot
            || (c.Kind == DeckPlan.ConsoleKind.ViewObject && c.Label == Monolith.ConsoleLabel)).ToList();
        Assert.Equal(2, mine.Count);

        foreach (DeckPlan.ConsoleSpot c in mine)
        {
            Assert.True(c.Y > MoonSurface.AnchorY + Monolith.HalfHeight,
                $"{c.Label} is behind the monolith — you would have to walk round "
                + $"{Monolith.HalfWidth:0} du of solid rock to reach it.");
            Assert.True(Math.Abs(c.X - MoonSurface.AnchorX) <= Monolith.HalfWidth,
                $"{c.Label} is off the end of the stone it belongs to.");
        }
    }

    /// <summary>NOTHING IS BUILT INSIDE IT. Four placers used to hold the deep landmark at arm's length with
    /// a number each, all of them sized for a six-du fixture: a flat 40 × 30 box in <see cref="SurfaceShelter"/>,
    /// a flat 26 in the outlying-structure loop, the lift head's own clash list, and the seeded ledger.
    /// Growing the stone twenty-fold under four constants that had never heard of it puts a pressure drum —
    /// the building a captain walks to when their air runs out — inside a wall.</summary>
    [Fact]
    public void NoBuildingOnThisMoonStandsInsideTheStone()
    {
        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        (double X, double Y, double R) claim = Monolith.KeepOutOn(Monolith.BodyId, "", field)!.Value;

        bool Inside(double x, double y) =>
            Math.Sqrt(((x - claim.X) * (x - claim.X)) + ((y - claim.Y) * (y - claim.Y))) < claim.R;

        var bad = new List<string>();
        foreach (SurfaceStructure.Spec shelter in SurfaceShelter.SpecsFor(Monolith.BodyId, "", field))
        {
            if (Inside(shelter.CentreX, shelter.CentreY))
            {
                bad.Add($"a SHELTER at ({shelter.CentreX:0.0}, {shelter.CentreY:0.0})");
            }
        }

        SurfaceLayout.Plan plan = SurfaceLayout.For(Monolith.BodyId, field, "");
        foreach ((double x, double y) in plan.BuildingCentres ?? [])
        {
            if (Inside(x, y))
            {
                bad.Add($"a BUILDING at ({x:0.0}, {y:0.0})");
            }
        }

        MoonSurface.LiftHeadBox head = MoonSurface.LiftHead(Monolith.BodyId, "", field);
        if (Inside(head.CentreX, head.CentreY))
        {
            bad.Add($"the LIFT HEAD at ({head.CentreX:0.0}, {head.CentreY:0.0})");
        }

        // And the doors on the real deck — the thing a captain actually walks up to.
        DeckPlan deck = TheMonolithsGround(hasSecretSite: true);
        foreach (DeckPlan.Door d in deck.Doors)
        {
            double dx = (d.X1 + d.X2) / 2, dy = (d.Y1 + d.Y2) / 2;
            if (Inside(dx, dy))
            {
                bad.Add($"a DOOR at ({dx:0.0}, {dy:0.0})");
            }
        }

        Assert.True(bad.Count == 0,
            "standing inside the monolith's claim: " + string.Join("; ", bad));
    }
}

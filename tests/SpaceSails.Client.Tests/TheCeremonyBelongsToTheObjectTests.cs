using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #649 · THE CEREMONY BELONGS TO THE OBJECT, AND THE OBJECT IS ON ONE MOON.
///
/// <para>Owner's ruling (2026-08-03, <c>docs/worldbuilding-notes.md</c> §8): <i>"There is ONE monolith… If
/// two grounds both call something 'the monolith', one of them is wrong and has to be renamed — the word is
/// reserved."</i></para>
///
/// <para>#648 unified the PREDICATE — the card, the slab, the nerve hit and the selfie now all ask
/// <see cref="Monolith.StandsOn"/> — and the picture was never audited the same way. The <b>swept apron</b>,
/// the ring of cleared ground whose entire job is to say SOMEBODY CARED ABOUT THIS SPOT, was laid
/// unconditionally: every landing site on every moon in the game was drawn standing inside the monolith's
/// ceremony. That is the borrowed-prose bug (#574) in scenery instead of in a sentence, and it is invisible
/// to every Core test because scenery is not geometry — it does not collide, does not seal, does not crowd a
/// console, and never reaches a reachability flood.</para>
///
/// <para>These walk the REAL surface deck the captain's boots collide with, for every landing site on every
/// landable body, and ask what is drawn.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheCeremonyBelongsToTheObjectTests
{
    // The same ten the site-spec audit walks. A hand-kept mirror for the same stated reason: the scenario is
    // a wwwroot JSON the test host cannot reliably locate.
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    private static DeckPlan DeckFor(string body, LandingSite site) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name);

    /// <summary>How many marks on this ground lie exactly on a circle of <paramref name="radius"/> about the
    /// deep anchor — i.e. are apron segments. Seeded terrain lands on that circle to within a thousandth of a
    /// deck unit at both endpoints essentially never, so this counts the apron and nothing else.</summary>
    private static int ApronSegmentsOn(DeckPlan deck, double radius)
    {
        bool OnRing(double x, double y) =>
            Math.Abs(Math.Sqrt(
                ((x - MoonSurface.AnchorX) * (x - MoonSurface.AnchorX)) +
                ((y - MoonSurface.AnchorY) * (y - MoonSurface.AnchorY))) - radius) < 0.001;

        return deck.Scenery.Count(m => OnRing(m.X1, m.Y1) && OnRing(m.X2, m.Y2));
    }

    /// <summary>THE APRON IS SWEPT AROUND SOMETHING, OR IT IS NOT SWEPT. Exactly two grounds in the game have
    /// a made object at the anchor with ceremony around it; every other ground — Luna's mass-driver muzzle,
    /// every seeded ancient spur and slag field, every second and third site on every moon — gets bare
    /// regolith, because that is what is there.</summary>
    [Fact]
    public void OnlyTheGroundsWithSomethingToSweepAroundGetASweptApron()
    {
        var bad = new List<string>();
        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan deck = DeckFor(body, site);

            int want;
            double radius;
            if (Monolith.StandsOn(body, site.LayoutSalt))
            {
                (want, radius) = (Monolith.ApronSegments, Monolith.ApronRadius);
            }
            else if (FalseSlab.StandsOn(body, site.LayoutSalt))
            {
                (want, radius) = (FalseSlab.ApronSegments, FalseSlab.ApronRadius);
            }
            else
            {
                // Nothing stands here, so nothing was swept. Ask at BOTH ceremony radii — a ground that
                // borrowed either object's apron is equally wrong.
                foreach (double r in new[] { Monolith.ApronRadius, FalseSlab.ApronRadius })
                {
                    int found = ApronSegmentsOn(deck, r);
                    if (found > 0)
                    {
                        bad.Add($"  {body}/{site.Name}: {found} swept-apron segments at r={r} " +
                                "around a landmark that has no ceremony");
                    }
                }
                continue;
            }

            int got = ApronSegmentsOn(deck, radius);
            if (got != want)
            {
                bad.Add($"  {body}/{site.Name}: {got} swept-apron segments, expected {want}");
            }
        }

        Assert.True(bad.Count == 0,
            "The swept apron is drawn on ground that has nothing to sweep around:\n" + string.Join("\n", bad));
    }

    /// <summary>EXACTLY ONE GROUND CARRIES THE MONOLITH'S CARD. The picture and the four-paragraph lore are
    /// the strongest statement the game makes about this object; a second ground showing them would be two
    /// monoliths whatever the label said.</summary>
    [Fact]
    public void OnlyOneGroundInTheGameShowsTheMonolithsCard()
    {
        var showing = new List<string>();
        foreach ((string body, LandingSite site) in EverySite())
        {
            if (DeckFor(body, site).Consoles.Any(c =>
                    c.Caption == Monolith.Lore || c.ImageUrl == Monolith.ArtUrl ||
                    c.Label == Monolith.ConsoleLabel))
            {
                showing.Add($"{body}/{site.Name}");
            }
        }

        Assert.Equal([$"{Monolith.BodyId}/{LandingSites.At(Monolith.BodyId, 0).Name}"], showing);
    }

    /// <summary>AND MIRANDA DID NOT LOSE ITS CARD IN THE PROCESS. Taking the word back off the canon ground
    /// must not take the content with it: the maze centre keeps a picture and a card of its own — a
    /// different one, because reusing the monolith's plate is the borrowed-prose bug the whole ruling is
    /// about.</summary>
    [Fact]
    public void MirandasMazeStillHasSomethingAtTheEndOfTheLongWalk()
    {
        DeckPlan miranda = DeckFor(FalseSlab.BodyId, LandingSites.At(FalseSlab.BodyId, 0));

        DeckPlan.ConsoleSpot card = Assert.Single(miranda.Consoles,
            c => c.Kind == DeckPlan.ConsoleKind.ViewObject && c.Label == FalseSlab.ConsoleLabel);
        Assert.Equal(FalseSlab.Lore, card.Caption);
        Assert.Equal(FalseSlab.ArtUrl, card.ImageUrl);
        Assert.NotEqual(Monolith.ArtUrl, card.ImageUrl);

        // It stands at the deep anchor, where the long walk ends.
        Assert.True(Math.Abs(card.X - MoonSurface.AnchorX) < 4);
        Assert.True(Math.Abs(card.Y - MoonSurface.AnchorY) < 8);
    }
}

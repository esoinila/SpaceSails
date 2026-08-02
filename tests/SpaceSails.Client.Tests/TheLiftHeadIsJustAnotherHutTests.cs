using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #606 · THE LIFT HEAD IS NOT THE SPECIAL BUILDING. Owner, twice, while playing:
///
/// <para><i>"I think the lift could also be a little more hidden on the surface, since up there there are no
/// guards... it could be in an ordinary hut, with 2 doors .. we have those. The expensive doors would be the
/// clue... a clue we can get tipped about or find it in papers."</i> — and then, after a fresh look at the
/// ground, the sentence that is the acceptance criterion: <i>"the elevator still stands out on surface like
/// a sore thumb."</i></para>
///
/// <para><b>Why this needs a guard at all.</b> #585 answered the same complaint with COLOUR — a violet door —
/// and then, when colour turned out to mark three unrelated things, with a caption: a maintenance plate above
/// the door and <i>THE CAR IS STILL HERE</i> below it. Neither touched the reason the building was obvious,
/// which is that it was five thin lines in a 10 x 8 rectangle standing on a moon where every other structure
/// is <see cref="SurfaceStructure"/>'s hatched, rotated, piled regolith. It was the only building on the
/// ground drawn in a different hand, and that is legible from anywhere, to anybody.</para>
///
/// <para>So these are measured off the DRAWN GROUND — the difference between the deck with the facility and
/// the deck without it — rather than off the code that draws it. A guard that asked the generator what it
/// meant to build would have passed on every version of this bug.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheLiftHeadIsJustAnotherHutTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>Everything the facility adds to a site, and nothing else: the same ground built twice, once
    /// with it and once without. Asked this way because the head has no other honest handle — it is meant to
    /// be indistinguishable, so the only thing that can name its parts is their absence.</summary>
    private static (List<DeckPlan.Wall> Walls, List<DeckPlan.Door> Doors,
                    List<(float X, float Y, string Text)> Labels, List<DeckPlan.ConsoleSpot> Consoles)
        WhatTheFacilityAdds(string body, string salt, string name)
    {
        DeckPlan bare = MoonSurface.SurfaceDeck(
            body, body, [], 0, (_, _) => { }, siteSalt: salt, siteName: name, hasSecretSite: false);
        DeckPlan full = MoonSurface.SurfaceDeck(
            body, body, [], 0, (_, _) => { }, siteSalt: salt, siteName: name, hasSecretSite: true);

        return (Extra(bare.Walls, full.Walls), Extra(bare.Doors, full.Doors),
                Extra(bare.RoomLabels, full.RoomLabels), Extra(bare.Consoles, full.Consoles));
    }

    private static List<T> Extra<T>(IReadOnlyList<T> before, IReadOnlyList<T> after) where T : notnull
    {
        var had = new Dictionary<T, int>();
        foreach (T t in before)
        {
            had[t] = had.TryGetValue(t, out int n) ? n + 1 : 1;
        }
        var extra = new List<T>();
        foreach (T t in after)
        {
            if (had.TryGetValue(t, out int n) && n > 0)
            {
                had[t] = n - 1;
            }
            else
            {
                extra.Add(t);
            }
        }
        return extra;
    }

    [Fact]
    public void TheHeadIsPiledRegolithLikeEveryOtherBuildingOnTheGround()
    {
        // A building on this ground is MASS: two parallel faces with hatching between them, closer together
        // than the captain is wide (SurfaceStructure's Greenland-longhouse rule), which comes out as dozens of
        // segments. The old head was five. You do not need to know what a lift looks like to see that.
        var thin = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                (List<DeckPlan.Wall> walls, _, _, _) =
                    WhatTheFacilityAdds(body, site.LayoutSalt, site.Name);

                if (walls.Count < 30)
                {
                    thin.Add($"  {body} · {site.Name}: the head is drawn with {walls.Count} segments — " +
                             "a sketch of a building, not a building.");
                }
            }
        }

        Assert.True(thin.Count == 0,
            "the lift head is not built out of what its neighbours are built out of:\n" + string.Join("\n", thin));
    }

    [Fact]
    public void TheHeadIsTheSameSIZEAsTheHutsAroundIt()
    {
        // The other half of "sore thumb", and the one a screenshot shows fastest: it was 10 x 8 on a ground
        // whose smallest building is nearly twice that. A small strange box among big familiar ones is a
        // pointer, however it is painted.
        //
        // Both numbers are read off the ground rather than named here: the head's reach is the furthest its
        // own drawn walls get from its centre, and the range it has to fall in is what the plan PUBLISHES
        // about the buildings it laid (#585's BuildingFootprints, which exists so a guard can ask instead of
        // guess).
        double smallest = double.PositiveInfinity, largest = 0;
        var heads = new List<(string Where, double Reach)>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                foreach ((double _, double _, double r) in
                         SurfaceLayout.For(body, Field, site.LayoutSalt).BuildingFootprints ?? [])
                {
                    smallest = Math.Min(smallest, r);
                    largest = Math.Max(largest, r);
                }

                MoonSurface.LiftHeadBox head = MoonSurface.LiftHead(body, site.LayoutSalt, Field);
                (List<DeckPlan.Wall> walls, _, _, _) =
                    WhatTheFacilityAdds(body, site.LayoutSalt, site.Name);

                double reach = 0;
                foreach (DeckPlan.Wall w in walls)
                {
                    reach = Math.Max(reach, Dist(w.X1, w.Y1, head.CentreX, head.CentreY));
                    reach = Math.Max(reach, Dist(w.X2, w.Y2, head.CentreX, head.CentreY));
                }
                heads.Add(($"{body} · {site.Name}", reach));
            }
        }

        Assert.True(smallest < largest, "no site published a building footprint — this guard is measuring nothing.");

        var odd = new List<string>();
        foreach ((string where, double reach) in heads)
        {
            if (reach < smallest || reach > largest)
            {
                odd.Add($"  {where}: the head reaches {reach:F1} du where the buildings on this ground run " +
                        $"{smallest:F1}..{largest:F1}.");
            }
        }

        Assert.True(odd.Count == 0,
            "the lift head is not the size of a building on this ground:\n" + string.Join("\n", odd));
    }

    [Fact]
    public void NothingDrawnOnTheGroundSAYSWhatItIs()
    {
        // The house rule, applied to the one place it was being broken (docs/art-manifest-hive.md): the object
        // is mundane, the implication is not. A caption is the game announcing itself, and #585 shipped two —
        // "▤ MAINTENANCE — NO ADMITTANCE" and "🛗 THE CAR IS STILL HERE" — plus a console that said CALL THE
        // CAR through the wall from outside, because console labels draw whether you are in the room or not.
        //
        // The head may add no ground label at all, and nothing it does add may name a lift.
        string[] tells = ["🛗", "LIFT", "ELEVATOR", "THE CAR", "FACILITY", "HIVE", "ADMITTANCE"];
        var said = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                (_, _, List<(float X, float Y, string Text)> labels, List<DeckPlan.ConsoleSpot> consoles) =
                    WhatTheFacilityAdds(body, site.LayoutSalt, site.Name);

                foreach ((float _, float _, string text) in labels)
                {
                    said.Add($"  {body} · {site.Name}: the head writes \"{text}\" on the regolith.");
                }
                foreach (DeckPlan.ConsoleSpot c in consoles)
                {
                    foreach (string tell in tells)
                    {
                        if (c.Label.ToUpperInvariant().Contains(tell))
                        {
                            said.Add($"  {body} · {site.Name}: \"{c.Label}\" announces the way down.");
                        }
                    }
                }
            }
        }

        Assert.True(said.Count == 0,
            "the ground tells the captain what the hut is instead of letting them notice:\n"
            + string.Join("\n", said));
    }

    [Fact]
    public void TheDoorsAreTheOnlyTELL()
    {
        // What is left once the caption is gone, and the whole of the owner's design: "The expensive doors
        // would be the clue." Two of them, both flown here, both drawn as a different KIND of object — and
        // the only two on the moon like it, because a signal that fires on three unrelated things is what
        // #585 already learned is not a signal.
        var wrong = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                (_, List<DeckPlan.Door> added, _, _) =
                    WhatTheFacilityAdds(body, site.LayoutSalt, site.Name);

                if (added.Count < 2)
                {
                    wrong.Add($"  {body} · {site.Name}: the hut has {added.Count} door(s); he asked for two.");
                }
                foreach (DeckPlan.Door d in added)
                {
                    if (!d.Imported || !d.Machined)
                    {
                        wrong.Add($"  {body} · {site.Name}: a door on the head is local stonework " +
                                  $"(imported {d.Imported}, machined {d.Machined}) — there is no tell left.");
                    }
                }

                DeckPlan full = MoonSurface.SurfaceDeck(
                    body, body, [], 0, (_, _) => { },
                    siteSalt: site.LayoutSalt, siteName: site.Name, hasSecretSite: true);
                int machined = 0;
                foreach (DeckPlan.Door d in full.Doors)
                {
                    if (d.Machined)
                    {
                        machined++;
                    }
                }
                if (machined != added.Count)
                {
                    wrong.Add($"  {body} · {site.Name}: {machined} machined doors on the site but only " +
                              $"{added.Count} on the head — the clue points at more than one place.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "the doors are not carrying the clue:\n" + string.Join("\n", wrong));
    }

    private static double Dist(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

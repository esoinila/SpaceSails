using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #723 · THE SHOVEL DOES NOT COME INDOORS.
///
/// <para>Found by playing, on <c>/map?secretlab=deep&amp;land=1&amp;floor=1</c> — B1 · ADMINISTRATION,
/// pressurised, 150 m under the regolith. [E] with empty hands in the spine corridor ran the beach-comber
/// probe and left the dug-square mark on the rockcrete; at the canteen's west face it said <i>"The shovel
/// rings off bedrock a foot down — too hard to dig here. Try another square."</i> There is no bedrock under
/// a floor somebody invoiced, and "try another square" is an invitation to a search that can never end,
/// because nothing was ever buried on any square of any corridor of the building.</para>
///
/// <para>The cause is the one thing that makes the Hive cheap to build: <b>a floor reuses the surface's own
/// coordinate envelope</b> — "it is not beside the field, it is under it" — so the spine corridor's (x, y)
/// is also a perfectly good square of open regolith, and a diggability test made of x and y alone had no
/// way to tell them apart. That is why this file asks BOTH questions of the same points: the corridor must
/// not dig at its own level, and it must dig at level 0. A guard that only checked the first would pass just
/// as happily on a world where the corridors had drifted outside the field rim and the shovel was refusing
/// them for the wrong reason — the fifth named bug class, a guard handed a world that cannot tell pass from
/// fail.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShovelStaysOnTheSurfaceTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",

        // The rock the report was filed from: ?secretlab=deep parks a 20-floor clinic with an unlisted
        // laboratory under it, which is the deepest and most awkward site the generator can produce. The id
        // IS the seed (Map.Sim's SecretLabDeepCheatBodyId), so naming it here audits the exact building the
        // owner was standing in.
        "secret-lab-site-unlisted",

        // …and the one rock in the system with GALLERIES under it, so the found band is not a topology this
        // sweep has never visited.
        UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    private static DeckPlan DeckFor(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    /// <summary>The places a captain actually stands and presses [E] on a floor: where the car puts them
    /// down, and every fixture the floor offers. Real spots off the real deck — the same arrays the renderer
    /// draws and the same ones <c>NearestConsoleSpot</c> dispatches through.</summary>
    private static List<(double X, double Y)> PlacesYouPressEOn(DeckPlan deck)
    {
        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        var spots = new List<(double X, double Y)> { (sx, sy) };
        spots.AddRange(deck.Consoles.Select(c => ((double)c.X, (double)c.Y)));
        return spots;
    }

    private static void AuditEveryFloor(Func<string, int, DeckPlan, string?> check, string what)
    {
        var bad = new List<string>();
        foreach (string body in Bodies)
        {
            // FloorsOf, not "−1 down to the depth": a site with an unlisted band has a GAP in the middle
            // where nothing was dug, so counting from a depth floods four floors that do not exist.
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (check(body, level, DeckFor(body, level)) is { } complaint)
                {
                    bad.Add($"  {body} B{-level}: {complaint}");
                }
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} floor(s) fail: {what}");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void NoSquareOfAnyUndergroundFloorIsDiggableGround()
    {
        // The law, stated over every floor of every clandestine site in the system rather than over the one
        // corridor the report came from. A single floor would only prove that B1 of one rock was fixed.
        AuditEveryFloor((_, level, deck) =>
        {
            var digs = PlacesYouPressEOn(deck)
                .Where(p => MoonSurface.IsDiggableGround(p.X, p.Y, level))
                .ToList();
            return digs.Count == 0 ? null
                : $"{digs.Count} spot(s) take a shovel, e.g. ({digs[0].X:F0}, {digs[0].Y:F0}).";
        }, "spec — nothing underground is diggable ground");
    }

    [Fact]
    public void TheSameSpotsWOULDTakeAShovelIfTheyWereOutOnTheRegolith()
    {
        // ── THE HALF THAT MAKES THE OTHER HALF MEAN ANYTHING ──
        //
        // These exact coordinates are diggable at level 0. That is not a curiosity, it is the whole bug: the
        // building sits INSIDE the digging field, so the only thing that has ever separated a corridor from
        // open regolith is the floor number. Asserting it here means the test above cannot go green because
        // the geometry wandered off the field, only because the floor is being asked about.
        AuditEveryFloor((_, _, deck) =>
        {
            var dead = PlacesYouPressEOn(deck)
                .Where(p => !MoonSurface.IsDiggableGround(p.X, p.Y, level: 0))
                .ToList();
            return dead.Count == 0 ? null
                : $"{dead.Count} spot(s) would not dig even up on the surface, e.g. "
                    + $"({dead[0].X:F0}, {dead[0].Y:F0}) — this floor cannot tell pass from fail.";
        }, "control — the same spots are open regolith one level up");
    }

    [Fact]
    public void EveryCorridorSquareOfThePressurisedFloorInTheReportRefusesTheShovel()
    {
        // The report's own floor, and not a sample of it: a flood of EVERY square the captain can stand on
        // and walk to from the lift car, each asked the question the [E] key asks. "Nothing can be buried on
        // any square of any corridor of the building" is a claim about all of them, so it is checked over all
        // of them — including the canteen's west face, where the bedrock line was actually read.
        const string body = "secret-lab-site-unlisted";
        const int level = -1;
        Assert.True(UndergroundComplex.HoldsPressure(body, level),
            "B1 of the report's rock is meant to be the pressurised refuge floor — if it is not, this test is "
            + "walking a different building from the one in the bug.");

        DeckPlan deck = DeckFor(body, level);
        (double sx, double sy) = HiveInterior.SpawnOn(Field);
        Assert.True(DeckReachability.Standable(sx, sy, DeckPlan.AvatarRadius, deck.CollisionField),
            "the flood starts inside a wall — its verdict would mean nothing.");

        IReadOnlyCollection<DeckReachability.Point> floor = DeckReachability.Reachable(
            new DeckReachability.Point(sx, sy),
            deck.CollisionField, DeckPlan.AvatarRadius,
            (Field.LeftX, Field.BottomY, Field.RightX, Field.LandingBandY),
            step: 2.0);

        Assert.True(floor.Count > 200,
            $"only {floor.Count} standable squares came back — a flood that small is not this facility, and a "
            + "sweep over nothing passes every law ever written.");

        var diggable = floor.Where(p => MoonSurface.IsDiggableGround(p.X, p.Y, level)).ToList();
        Assert.True(diggable.Count == 0,
            $"{diggable.Count} of {floor.Count} squares on a pressurised poured floor take a shovel, e.g. "
            + $"({diggable.FirstOrDefault().X:F0}, {diggable.FirstOrDefault().Y:F0}).");

        // …and the same flood one level up is almost entirely diggable, which is exactly the trap: the
        // corridors ARE in the digging field. Without this line the assertion above could be satisfied by a
        // facility that had quietly moved off the map.
        int wouldDig = floor.Count(p => MoonSurface.IsDiggableGround(p.X, p.Y, level: 0));
        Assert.True(wouldDig > floor.Count * 0.9,
            $"only {wouldDig} of {floor.Count} of these squares would dig on the open regolith — the floor is "
            + "not sitting in the digging field, so this test proves nothing about the shovel.");
    }

    [Fact]
    public void TheShovelIsAVerbTheSURFACEHasAndNoFloorBelowIt()
    {
        // The one fact the [E] handler, the standing prompt, the key bar and the tracker caption are all
        // gated on. Said plainly here so a change to it is a change somebody had to mean.
        Assert.True(MoonSurface.ShovelWorksOnThisFloor(0), "the surface is where the beach-comber kit works.");
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Assert.False(MoonSurface.ShovelWorksOnThisFloor(level),
                    $"{body} B{-level} offers the shovel, and it is a poured floor inside a building.");
            }
        }
    }
}

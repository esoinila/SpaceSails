using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>#563 · The outpost hut — the first enclosed space an ordinary landing site has ever had. These
/// pin the two things that could quietly ruin it: geometry that bottles the field, and consoles that cannot
/// be reached as the answering console (the #520 failure that shipped four times on the wreck lane).</summary>
public class SurfaceOutpostTests
{
    private static readonly SurfaceLayout.Field Env = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    private const double AvatarRadius = 0.7;

    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "titan", "ganymede", "callisto", "enceladus", "triton"];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    [Fact]
    public void EnoughSitesCarryOne_ThatAPlayerActuallyMeetsThem()
    {
        // The whole reason this exists: the owner had NEVER seen a landing site expand, because the only
        // mechanisms that could grow one were gated behind body ids no moon can have and a 1-in-40 roll
        // every moon fails. A mechanic nobody meets is dead code with good manners.
        var all = EverySite().ToList();
        int withHut = all.Count(t => SurfaceOutpost.Present(t.Body, t.Site.LayoutSalt));

        Assert.True(withHut >= all.Count / 3,
            $"only {withHut} of {all.Count} sites carry a hut — a captain could play for hours and never meet one.");
        // ...and not EVERY site, or the surprise becomes furniture.
        Assert.True(withHut < all.Count, "every single site has a hut — there is no discovery left in finding one.");
    }

    [Fact]
    public void TwoSitesOnTheSameMoon_CanDiffer()
    {
        // Seeded per (body, SITE salt), not per body — which is the entire point of having sites at all.
        // If this ever collapses to per-body, every ground on a moon becomes the same ground again.
        var perSite = LandingSites.For("miranda")
            .Select(s => (s.Name, Has: SurfaceOutpost.Present("miranda", s.LayoutSalt),
                          Cover: SurfaceOutpost.CoverFor("miranda", s.LayoutSalt)))
            .ToList();

        Assert.True(perSite.Select(t => (t.Has, t.Cover)).Distinct().Count() > 1,
            "every Miranda site answers identically — the salt is not reaching the seed.");
    }

    [Fact]
    public void ItIsDeterministic_SoARevisitFindsItWhereItWas()
    {
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceOutpost.Placement a = SurfaceOutpost.For(body, site.LayoutSalt, Env);
            SurfaceOutpost.Placement b = SurfaceOutpost.For(body, site.LayoutSalt, Env);
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void TheHutStandsInsideTheField_NeverOverhangingIt()
    {
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceOutpost.Placement p = SurfaceOutpost.For(body, site.LayoutSalt, Env, forcePresent: true);
            SurfaceOutpost.Region r = SurfaceOutpost.Build(body, site.LayoutSalt, p);

            Assert.True(r.MinX >= Env.LeftX - 0.001, $"{body}/{site.Name}: hut pokes out past the port edge");
            Assert.True(r.MaxX <= Env.RightX + 0.001, $"{body}/{site.Name}: hut pokes out past the starboard edge");
            Assert.True(r.MinY >= Env.BottomY, $"{body}/{site.Name}: hut hangs below the deep rim");
            Assert.True(r.MaxY <= Env.TopY, $"{body}/{site.Name}: hut rides up into the ship");
        }
    }

    [Fact]
    public void TheHutSitsDeep_SoItIsAWalkAndNotAnAmenity()
    {
        // If a hut could seed next to the tube it would be a vending machine, not a destination. The
        // supply-line design (#562) needs the reward to be OUT there.
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceOutpost.Placement p = SurfaceOutpost.For(body, site.LayoutSalt, Env, forcePresent: true);
            Assert.True(p.DoorY < Env.LandingBandY - 5,
                $"{body}/{site.Name}: the hut is seeded in the landing band, a few steps from the shuttle");
        }
    }

    [Fact]
    public void TheDoorwayIsWiderThanTheCaptain_AndTheConsolesAreReachableThroughIt()
    {
        // The #520 law, applied before it can bite: not merely "walkable ground exists near the console",
        // but "the captain can walk from the doorway to each console". A room whose loot is behind its own
        // furniture is the bug this project has already paid for four times.
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceOutpost.Placement p = SurfaceOutpost.For(body, site.LayoutSalt, Env, forcePresent: true);
            SurfaceOutpost.Region r = SurfaceOutpost.Build(body, site.LayoutSalt, p);

            var walls = r.Walls
                .Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))
                .ToList();

            // Stand just outside the hatch, on the open-ground side.
            var doorstep = new DeckReachability.Point(p.DoorX - (p.ExtendDir * 1.5), p.DoorY);
            var bounds = (Env.LeftX, Env.BottomY, Env.RightX, Env.TopY);

            IReadOnlyList<string> missed = DeckReachability.Unreachable(
                doorstep,
                [.. r.Consoles.Select(c => ($"{body}/{site.Name}: {c.Label}", new DeckReachability.Point(c.X, c.Y)))],
                walls, AvatarRadius, bounds);

            Assert.True(missed.Count == 0, "unreachable inside the hut:\n  " + string.Join("\n  ", missed));
        }
    }

    [Fact]
    public void NoTwoConsolesShareADoorstep()
    {
        // 3 du is the interact radius; two consoles closer than that means one of them can never be the
        // ANSWERING console anywhere, and its [E] prompt is a lie. Exactly the wreck's placard bug.
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceOutpost.Placement p = SurfaceOutpost.For(body, site.LayoutSalt, Env, forcePresent: true);
            SurfaceOutpost.Region r = SurfaceOutpost.Build(body, site.LayoutSalt, p);

            for (int i = 0; i < r.Consoles.Count; i++)
            {
                for (int j = i + 1; j < r.Consoles.Count; j++)
                {
                    double dx = r.Consoles[i].X - r.Consoles[j].X;
                    double dy = r.Consoles[i].Y - r.Consoles[j].Y;
                    double d = Math.Sqrt((dx * dx) + (dy * dy));
                    Assert.True(d > 3.0,
                        $"{body}/{site.Name}: {r.Consoles[i].Label} and {r.Consoles[j].Label} are {d:F2} du apart");
                }
            }
        }
    }

    [Fact]
    public void TheCacheIsPartial_SoTheTubeStaysTheAnchor()
    {
        // The single most important number in the feature. If found ammo could fill a magazine, a captain
        // becomes self-sufficient out in the field, the tube stops being an anchor, and the supply-line
        // design in #562 quietly evaporates. A cache buys ONE more fight, never independence.
        foreach ((string body, LandingSite site) in EverySite())
        {
            int rounds = SurfaceOutpost.CacheRounds(body, site.LayoutSalt);
            Assert.InRange(rounds, SurfaceOutpost.CacheRoundsMin, SurfaceOutpost.CacheRoundsMax);
            Assert.True(rounds < SentryBot.MaxMagazine,
                "a cache can fill a magazine — the tube has stopped being the anchor.");
            // And never more than one pack's worth of grinding (6 Old Ones = 84 rounds).
            Assert.True(rounds < SentryBot.RoundsForPack(ReeverRaid.MaxReevers),
                "a cache covers a whole bad-roll pack — that is independence, not a reprieve.");
        }
    }

    [Fact]
    public void EveryCoverStory_IsFullyWritten()
    {
        foreach (SurfaceOutpost.OutpostCover cover in Enum.GetValues<SurfaceOutpost.OutpostCover>())
        {
            Assert.False(string.IsNullOrWhiteSpace(SurfaceOutpost.DoorLabel(cover)));
            Assert.False(string.IsNullOrWhiteSpace(SurfaceOutpost.InsideLabel(cover)));
            Assert.True(SurfaceOutpost.ForcedLine(cover).Length > 40);
            Assert.True(SurfaceOutpost.EffectsLine(cover).Length > 120,
                $"{cover}'s effects barely say anything — this is the only story the hut tells.");
        }
    }

    [Fact]
    public void TheEffectsNeverStateTheCanon()
    {
        // Owner's ruling: the Old Ones are failed restores, and that is NEVER said in a card and never
        // confirmed by a sensor. A wallet may carry a badge, a payslip, a photograph — texture that lets a
        // player assemble it. The moment one of them explains what is outside, the best reveal in the game
        // is spent on a pickup.
        string[] forbidden = ["REEVER", "OLD ONE", "RESTORE", "BACKUP", "REVIV", "CLONE"];
        foreach (SurfaceOutpost.OutpostCover cover in Enum.GetValues<SurfaceOutpost.OutpostCover>())
        {
            string line = SurfaceOutpost.EffectsLine(cover).ToUpperInvariant();
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TheCoverStoryNeverMatchesWhatIsInside()
    {
        // The whole fiction in one assertion: a place that was pretending. If the outside label and the
        // inside label ever agreed, the hut would just be a shed and the world would have said nothing.
        foreach (SurfaceOutpost.OutpostCover cover in Enum.GetValues<SurfaceOutpost.OutpostCover>())
        {
            Assert.NotEqual(SurfaceOutpost.DoorLabel(cover), SurfaceOutpost.InsideLabel(cover));
        }
    }
}

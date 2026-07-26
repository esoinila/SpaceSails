using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #320 · Miranda is a world, not a level. Pins the seeded landing-site set: deterministic per body (same
/// body → same sites, so re-landing re-offers the identical board), the 2–4 count bound, site 0 as the
/// canon Wild Plain (empty salt → today's ground), distinct kinds, and — the mechanical bite — that a
/// chosen site's salt PARAMETERIZES the surface deck-plan (a different site grows a visibly different
/// ground) while an empty salt reproduces the body's canon layout exactly.
/// </summary>
public class LandingSiteTests
{
    // The same shared field envelope MoonSurface hands SurfaceLayout (mirrors SurfaceLayoutTests.Env).
    private static readonly SurfaceLayout.Field Env = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -84, LandingBandY: -27, AnchorX: -6, AnchorY: -70);

    private static readonly string[] Bodies =
        ["miranda", "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "triton", "the-clinker"];

    // ── Determinism: same body → same seeded set (the persistence / re-offer guarantee at the data layer) ──

    [Fact]
    public void For_IsDeterministic_PerBody()
    {
        foreach (string id in Bodies)
        {
            IReadOnlyList<LandingSite> a = LandingSites.For(id);
            IReadOnlyList<LandingSite> b = LandingSites.For(id);
            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i], b[i]); // record equality: index, kind, name, flavor, salt all match
            }
        }
    }

    // ── The 2–4 count bound (owner: "2–4 per landed body") ──

    [Fact]
    public void Count_IsWithinTwoToFour_ForEveryBody()
    {
        foreach (string id in Bodies)
        {
            int count = LandingSites.For(id).Count;
            Assert.InRange(count, LandingSites.MinSites, LandingSites.MaxSites);
            Assert.Equal(count, LandingSites.Count(id));
        }
    }

    // ── Site 0 is always the Wild Plain on the canon ground (empty salt) ──

    [Fact]
    public void SiteZero_IsAlwaysWildPlain_OnCanonGround()
    {
        foreach (string id in Bodies)
        {
            LandingSite first = LandingSites.For(id)[0];
            Assert.Equal(0, first.Index);
            Assert.Equal(LandingSiteKind.WildPlain, first.Kind);
            Assert.Equal("", first.LayoutSalt); // empty salt → SurfaceLayout routes to the canon ground
            Assert.False(string.IsNullOrWhiteSpace(first.Name));
            Assert.False(string.IsNullOrWhiteSpace(first.Flavor));
        }
    }

    // ── Kinds are distinct within a body, and every site carries name + flavor + a stable index ──

    [Fact]
    public void Sites_AreDistinctKinds_AndFullyPopulated()
    {
        foreach (string id in Bodies)
        {
            IReadOnlyList<LandingSite> sites = LandingSites.For(id);
            Assert.Equal(sites.Count, sites.Select(s => s.Kind).Distinct().Count());
            Assert.Equal(sites.Count, sites.Select(s => s.LayoutSalt).Distinct().Count()); // salts distinct too
            for (int i = 0; i < sites.Count; i++)
            {
                Assert.Equal(i, sites[i].Index);
                Assert.False(string.IsNullOrWhiteSpace(sites[i].Name));
                Assert.False(string.IsNullOrWhiteSpace(sites[i].Flavor));
            }
        }
    }

    // ── Different bodies generally get different boards (the seeding actually varies by id) ──

    [Fact]
    public void DifferentBodies_GetVariedBoards()
    {
        // Compare the (count, secondary-kind-set) signature across bodies — at least two distinct signatures.
        var signatures = Bodies
            .Select(id => (LandingSites.For(id).Count, string.Join(",", LandingSites.For(id).Skip(1).Select(s => s.Kind))))
            .Distinct()
            .ToList();
        Assert.True(signatures.Count >= 2, "landing-site boards should vary across bodies");
    }

    // ── At() clamps an out-of-range (stale or cheat-forced) index into the real set ──

    [Fact]
    public void At_ClampsOutOfRange()
    {
        IReadOnlyList<LandingSite> set = LandingSites.For("miranda");
        Assert.Equal(set[0], LandingSites.At("miranda", -5));
        Assert.Equal(set[^1], LandingSites.At("miranda", 999));
        Assert.Equal(set[0], LandingSites.At("miranda", 0));
    }

    // ── The mechanical bite: the picked site's salt parameterizes the surface deck-plan ──

    [Fact]
    public void SiteSalt_ParameterizesTheGround_Site0IsCanon()
    {
        foreach (string id in new[] { "miranda", "luna", "phobos", "titan" })
        {
            // Empty salt (site 0) reproduces the body's canon layout byte-for-byte.
            long canon = SurfaceLayout.WallHash(SurfaceLayout.For(id, Env));
            long site0 = SurfaceLayout.WallHash(SurfaceLayout.For(id, Env, ""));
            Assert.Equal(canon, site0);

            IReadOnlyList<LandingSite> sites = LandingSites.For(id);
            if (sites.Count < 2)
            {
                continue;
            }

            // Every secondary site's ground differs from the canon ground AND from each other.
            var hashes = new List<long> { canon };
            for (int i = 1; i < sites.Count; i++)
            {
                long h = SurfaceLayout.WallHash(SurfaceLayout.For(id, Env, sites[i].LayoutSalt));
                Assert.DoesNotContain(h, hashes); // distinct from every prior site's ground
                hashes.Add(h);
            }
        }
    }

    // ── A non-empty salt re-seeds even an AUTHORED body (Miranda's site 1 is NOT the monolith maze) ──

    [Fact]
    public void SecondarySite_ReSeedsEvenAuthoredBodies()
    {
        long maze = SurfaceLayout.WallHash(SurfaceLayout.For("miranda", Env));
        LandingSite second = LandingSites.For("miranda").FirstOrDefault(s => s.Index == 1);
        // Miranda always has ≥2 sites here only if count≥2 (guaranteed by MinSites=2), so site 1 exists.
        long variant = SurfaceLayout.WallHash(SurfaceLayout.For("miranda", Env, second.LayoutSalt));
        Assert.NotEqual(maze, variant);
        // …but the canon monolith maze is still reachable at site 0.
        Assert.Equal("THE MONOLITH MAZE", SurfaceLayout.For("miranda", Env, "").Scheme);
    }

    // ── An away-expedition rock keeps its authored ground regardless of any salt (single gig site) ──

    [Fact]
    public void ExpeditionSite_IgnoresSalt()
    {
        foreach (ExpeditionSiteKind kind in System.Enum.GetValues<ExpeditionSiteKind>())
        {
            string id = ExpeditionSite.BodyIdFor(kind);
            long authored = SurfaceLayout.WallHash(SurfaceLayout.ForExpedition(kind, Env));
            long salted = SurfaceLayout.WallHash(SurfaceLayout.For(id, Env, "DepotApron"));
            Assert.Equal(authored, salted); // the gig's ground never varies by a landing-site salt
        }
    }

    // ── The deck cache keys sites apart: same salt → equal key, different salt → different key ──

    [Fact]
    public void SurfaceDeckKey_DistinguishesSites()
    {
        var a0 = SurfaceDeckKey.For("miranda", "Miranda", null, "");
        var b0 = SurfaceDeckKey.For("miranda", "Miranda", null, "");
        var s1 = SurfaceDeckKey.For("miranda", "Miranda", null, "CraterShelf");
        var s2 = SurfaceDeckKey.For("miranda", "Miranda", null, "IceFissure");

        Assert.Equal(a0, b0);          // same salt → equal key (revisit reuses the cached deck)
        Assert.NotEqual(a0, s1);       // different salt → different key (a distinct ground, never a collision)
        Assert.NotEqual(s1, s2);
        // And the no-salt overload still equals the empty-salt key (back-compat with existing callers).
        Assert.Equal(SurfaceDeckKey.For("miranda", "Miranda", null), a0);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1058 · EACH GROUND HAS ITS OWN STONE.
///
/// <para>#650 made a cache belong to a SITE, but the landmark it paced off stayed per-body: a chest on
/// Phobos' Wild Plain and a chest on its Ridge Camp both read <i>"from the monolith, 40 paces
/// anti-spinward"</i> — pacing from a stone that stands on neither ground in particular, and which the
/// captain on the Ridge Camp cannot see, because <see cref="Monolith.StandsOn"/> is false everywhere but
/// the canon salt. Vague rather than wrong, which is how it survived two lanes.</para>
///
/// <para>Canon pass (Fable, 2026-09-02): each seeded site draws its landmark deterministically from the kind
/// pool by a (body, site) deal, DEALT WITHOUT REPLACEMENT per body so two grounds on one moon never share a
/// kind — a captain who knows his grounds can tell them apart by the stone alone. Four new kinds join the
/// pool, worded verbatim; existing kinds keep their exact wording; the card grammar is unchanged.</para>
///
/// <para>Site 0 is not in the deal, and that is the load-bearing part rather than an exemption: the Wild
/// Plain IS the body's canon ground, so it keeps the authored landmark, which is what keeps the drawn slab
/// and the card in the pocket one truth (#649) and what lets every chest minted before this lane read back
/// character for character.</para>
/// </summary>
public class EachGroundHasItsOwnStoneTests
{
    private const string Body = "phobos";
    private const int WildPlain = 0;
    private const int RidgeCamp = 2;

    /// <summary>The bodies a captain can put a shovel into — the same roster
    /// <c>TheMonolithIsAPlaceTests</c> sweeps, so the two guards answer for the same world.</summary>
    private static readonly string[] Landable =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    /// <summary>The canon table, transcribed from the ruling comment. If a kind's wording ever drifts, this
    /// is where it fails — the pool is worldbuilding, not a crew's to reword.</summary>
    private static readonly (string Name, string Note)[] CanonKinds =
    [
        ("the split boulder", "one rock in two halves, the gap wide enough to walk."),
        ("the fallen pylon", "a survey mast on its side, older than the survey it is not filed in."),
        ("the dead crawler", "a hauler that stopped mid-track; the track is still there, both directions."),
        ("the old cairn", "stacked by hand, height of a man's chest; nobody stacks the first stone twice."),
    ];

    /// <summary>THE TWO GROUNDS ARE ACTUALLY DIFFERENT PLACES. Stated first and separately because every
    /// assertion below is worthless without it: "the two sites name different stones" passes gloriously if
    /// the two sites are the same ground, or if the second one does not exist. Named, seeded, and with
    /// demonstrably different walls under them.</summary>
    [Fact]
    public void TheTwoGroundsAreDifferentGround()
    {
        LandingSite plain = LandingSites.At(Body, WildPlain);
        LandingSite ridge = LandingSites.At(Body, RidgeCamp);

        Assert.Equal("The Wild Plain", plain.Name);
        Assert.Equal("The Ridge Camp", ridge.Name);
        Assert.NotEqual(plain.LayoutSalt, ridge.LayoutSalt);
        Assert.NotEqual(
            SurfaceLayout.WallHash(SurfaceLayout.For(Body, SurfaceLayout.DefaultField, plain.LayoutSalt)),
            SurfaceLayout.WallHash(SurfaceLayout.For(Body, SurfaceLayout.DefaultField, ridge.LayoutSalt)));
    }

    /// <summary>THE BUG ITSELF, in the ruling's own example. Two chests, two grounds, one moon: the card no
    /// longer paces both of them off the same stone, and the Ridge Camp's chest paces off something that is
    /// actually on the Ridge Camp rather than off the slab over the horizon.</summary>
    [Fact]
    public void TwoChestsOnTwoGroundsOfOneMoonPaceFromDifferentStones()
    {
        var ledger = new CacheLedger();
        TreasureCache plain = ledger.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true, siteIndex: WildPlain);
        TreasureCache ridge = ledger.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true, siteIndex: RidgeCamp);

        Assert.Equal("the monolith", plain.LandmarkName);
        Assert.NotEqual(plain.LandmarkName, ridge.LandmarkName);
        Assert.Contains(ridge.LandmarkName, CanonKinds.Select(k => k.Name));

        // …and the card grammar is untouched — body · SITE — from <stone>, N paces <bearing>.
        Assert.Equal(
            $"PHOBOS · THE RIDGE CAMP — from {ridge.LandmarkName}, {ridge.Paces} paces {ridge.Bearing}",
            ridge.Caption("Phobos"));
    }

    /// <summary>THE DEAL IS WITHOUT REPLACEMENT, ON EVERY MOON. The law is not "the Ridge Camp differs from
    /// the Wild Plain" — it is that no two grounds anywhere on one body ever name the same stone. Swept
    /// pairwise over every landable body's whole board, which is what makes this fail on a
    /// with-replacement deal (three draws from a pool of four collide well over half the time per body,
    /// so a hash-modulo pick reddens this on the first moon that repeats).</summary>
    [Fact]
    public void NoTwoGroundsOnOneBodyEverNameTheSameStone()
    {
        foreach (string body in Landable)
        {
            int sites = LandingSites.Count(body);
            Assert.InRange(sites, LandingSites.MinSites, LandingSites.MaxSites);

            string[] stones = Enumerable.Range(0, sites)
                .Select(i => Landmarks.At(body, i).Name)
                .ToArray();

            Assert.Equal(stones.Length, stones.Distinct(StringComparer.Ordinal).Count());
        }
    }

    /// <summary>The pool is deep enough that the deal above CAN be made without replacement. A modulo would
    /// have hidden a shortage behind a silent repeat; this says the arithmetic, out loud.</summary>
    [Fact]
    public void ThePoolIsDeepEnoughToDealEverySeededGround()
    {
        Assert.True(
            Landmarks.SeededSiteKinds >= LandingSites.MaxSites - 1,
            $"{Landmarks.SeededSiteKinds} kinds cannot cover {LandingSites.MaxSites - 1} seeded grounds.");
    }

    /// <summary>THE CANON GROUND KEEPS ITS AUTHORED STONE. Site 0 is the Wild Plain, the ground the body's
    /// signature stands on, and it resolves to exactly what <see cref="Landmarks.For(string)"/> has returned
    /// since #164 — the flagship on Phobos, the generic beacon elsewhere. Existing kinds, existing wording.</summary>
    [Fact]
    public void TheCanonGroundKeepsTheBodysAuthoredStone()
    {
        Assert.Equal("the monolith", Landmarks.At(Body, WildPlain).Name);
        Assert.Equal(Landmarks.PhobosMonolith, Landmarks.At(Body, WildPlain));

        foreach (string body in Landable)
        {
            Assert.Equal(Landmarks.For(body), Landmarks.At(body, 0));
            Assert.Equal(Landmarks.For(body), Landmarks.At(body, null));
        }

        Assert.Equal("the landing beacon", Landmarks.At("miranda", 0).Name);
    }

    /// <summary>THE FOUR NEW KINDS, VERBATIM. Every stone a seeded ground can draw is one of the canon four,
    /// worded character for character — name AND the composition note the art lane briefs from — and all
    /// four are actually reachable somewhere in the world, so none of them is a table entry nothing deals.</summary>
    [Fact]
    public void TheSeededKindsAreWordedExactlyAsCanon()
    {
        var drawn = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string body in Landable)
        {
            for (int i = 1; i < LandingSites.Count(body); i++)
            {
                Landmark stone = Landmarks.At(body, i);
                drawn[stone.Name] = stone.Note;

                // No invented measurement: the canon comment records no height for these, and this repo's
                // bug class 1 is a plausible number nothing derives (Monolith reads 85 m because the real
                // boulder is 85 m).
                Assert.Equal(0.0, stone.HeightMeters);
                Assert.Equal(body, stone.BodyId);
            }
        }

        Assert.Equal(
            CanonKinds.Select(k => k.Name).OrderBy(n => n, StringComparer.Ordinal),
            drawn.Keys.OrderBy(n => n, StringComparer.Ordinal));
        foreach ((string name, string note) in CanonKinds)
        {
            Assert.Equal(note, drawn[name]);
        }
    }

    /// <summary>THE REGISTER, BINDING. The pylon and the crawler are MUNDANE WRECKAGE — nothing on them hints
    /// at the fourth world; most stones are stones. The cairn's line is texture, not lore: the hands are
    /// unknown and no card asks whose. So no seeded kind may borrow the vocabulary the one genuinely alien
    /// object owns, and none of them may pose the question.</summary>
    [Fact]
    public void TheSeededKindsStayMundane()
    {
        string[] reserved =
        [
            "monolith", "not ours", "not natural", "no seam", "reever", "old one", "ancient", "alien",
            "whose", "who left", "who stacked",
        ];

        foreach (string body in Landable)
        {
            for (int i = 1; i < LandingSites.Count(body); i++)
            {
                Landmark stone = Landmarks.At(body, i);
                string text = $"{stone.Name} {stone.Note}";
                foreach (string word in reserved)
                {
                    Assert.DoesNotContain(word, text, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    /// <summary>ONE TRUTH: the stone on the card and the thing on the ground. #649 settled that the moon the
    /// cards pace from is the moon the slab stands on; per-site landmarks re-open the same question one level
    /// down, because the slab stands on ONE GROUND of that moon (the canon salt) and the cards now name a
    /// ground each. Stated as a biconditional over every ground of every landable body: the set of grounds
    /// whose card says "the monolith" IS the set of grounds the deck plan actually lays the slab on, and it
    /// has exactly one member. This is the assertion the pre-lane code fails — it said "the monolith" on all
    /// four of Phobos' grounds while the slab was drawn on one.</summary>
    [Fact]
    public void TheGroundTheCardPacesFromIsTheGroundTheSlabStandsOn()
    {
        var cardSays = new List<string>();
        var groundSays = new List<string>();

        foreach (string body in Landable)
        {
            for (int i = 0; i < LandingSites.Count(body); i++)
            {
                LandingSite site = LandingSites.At(body, i);
                string where = $"{body}#{i}";

                if (Landmarks.At(body, i).Name.Contains("monolith", StringComparison.OrdinalIgnoreCase))
                {
                    cardSays.Add(where);
                }

                // Not the predicate's own word — the walls the generator actually builds.
                bool drawnSlab = SurfaceLayout.For(body, SurfaceLayout.DefaultField, site.LayoutSalt)
                    .Landmarks.Any(m => m.Label.Contains("MONOLITH", StringComparison.Ordinal));
                if (drawnSlab)
                {
                    groundSays.Add(where);
                }
                Assert.Equal(Monolith.StandsOn(body, site.LayoutSalt), drawnSlab);
            }
        }

        Assert.Equal(cardSays, groundSays);
        Assert.Equal(["phobos#0"], groundSays);
    }

    /// <summary>The deal never moves. Same moon, same ground, same stone — this session, next session, and
    /// in a test, because a map card that re-mints differently is a map card that lies.</summary>
    [Fact]
    public void TheDealIsStableAcrossCalls()
    {
        foreach (string body in Landable)
        {
            for (int i = 0; i < LandingSites.Count(body); i++)
            {
                Assert.Equal(Landmarks.At(body, i), Landmarks.At(body, i));
            }
        }
    }

    /// <summary>A stale or cheat-forced ordinal lands on a ground that exists, exactly the way
    /// <see cref="LandingSites.At"/> clamps — it never throws and never invents a fifth stone.</summary>
    [Fact]
    public void AStaleSiteOrdinalClampsOntoARealGround()
    {
        int last = LandingSites.Count(Body) - 1;
        Assert.Equal(Landmarks.At(Body, last), Landmarks.At(Body, 99));
        Assert.Equal(Landmarks.At(Body, 0), Landmarks.At(Body, -4));
    }

    // ── THE LEGACY BYTE PROOF ───────────────────────────────────────────────────────────────────────────
    //
    // Every string below was minted by the code as it stood BEFORE this lane and is pasted in whole. It is
    // not "what the new code says", it is what a captain's saved chest said, and it must go on saying it.

    /// <summary>A BODY-WIDE CHEST MINTS THE EXACT TEXT IT ALWAYS DID. A legacy save, a bought rumour map and
    /// the hoard cheat all mint with no ground under them; each of these captions is the pre-lane output,
    /// character for character, including the bearing and the pace count that the same seed has always
    /// produced.</summary>
    [Fact]
    public void ABodyWideChestMintsTheExactWordingItAlwaysDid()
    {
        TreasureCache phobos = CacheMint.Bury(
            "cache-you-7", Body, mintIndex: 7, coin: 900, [], buriedSimTime: 40000,
            owner: "you", playerOwned: true);
        Assert.Null(phobos.SiteIndex);
        Assert.Equal("the monolith", phobos.LandmarkName);
        Assert.Equal("PHOBOS — from the monolith, 77 paces spinward", phobos.Caption("Phobos"));
        Assert.Equal("77 paces spinward of the monolith", phobos.BearingLine);

        TreasureCache miranda = CacheMint.Bury(
            "cache-you-3", "miranda", mintIndex: 3, coin: 200, [], buriedSimTime: 900,
            owner: "you", playerOwned: true);
        Assert.Equal("the landing beacon", miranda.LandmarkName);
        Assert.Equal("MIRANDA — from the landing beacon, 34 paces sunward", miranda.Caption("Miranda"));
    }

    /// <summary>…and the barfly's map is one of them. A rumour is minted with no ground (#650: a barfly does
    /// not know which shelf the ghost dug on), so it keeps the body-wide stone and the body-wide caption.</summary>
    [Fact]
    public void ARumourMapKeepsItsBodyWideStone()
    {
        RumorMaps.Rumor rumour = RumorMaps.Generate("legacy-proof|ceres-station|day-12");

        Assert.Null(rumour.Cache.SiteIndex);
        Assert.Equal("the monolith", rumour.Cache.LandmarkName);
        Assert.Equal(
            "PHOBOS — from the monolith, 57 paces spinward",
            rumour.Cache.Caption("Phobos"));
    }

    /// <summary>A CHEST ALREADY IN THE GROUND IS NOT RE-PACED. The landmark name is SAVED, not re-derived, so
    /// a vault record written before this lane loads, reads and re-writes with its own wording — and the
    /// round trip does not grow a site key it never had.</summary>
    [Fact]
    public void ALegacyVaultRecordKeepsItsOwnStoneThroughTheRoundTrip()
    {
        var section = new CachesSection
        {
            NextMintIndex = 1,
            Caches =
            [
                new CacheRecord
                {
                    Id = "cache-you-0", BodyId = Body, LandmarkName = "the monolith",
                    Bearing = "anti-spinward", Paces = 40, Coin = 900, Owner = "you", PlayerOwned = true,
                    // SiteIndex deliberately unset — the pre-#650 shape, which is also the pre-#1058 shape.
                },
            ],
        };

        var restored = new CacheLedger();
        VaultMapper.Apply(section, restored);
        TreasureCache c = restored.Caches.Single();

        Assert.Null(c.SiteIndex);
        Assert.Equal("the monolith", c.LandmarkName);
        Assert.Equal("PHOBOS — from the monolith, 40 paces anti-spinward", c.Caption("Phobos"));

        CacheRecord written = VaultMapper.ToSection(restored).Caches.Single();
        Assert.Null(written.SiteIndex);
        Assert.Equal("the monolith", written.LandmarkName);
    }

    /// <summary>The dig path is untouched: which ground a chest is UNDER is still #650's question, answered by
    /// the site ordinal, never by the stone the card happens to name.</summary>
    [Fact]
    public void TheStoneNeverDecidesWhereAChestCanBeDugUp()
    {
        var ledger = new CacheLedger();
        TreasureCache ridge = ledger.Bury(
            Body, coin: 900, [], simTime: 40000, owner: "you", playerOwned: true,
            digX: 4.5, digY: -30.0, siteIndex: RidgeCamp);

        Assert.Equal([ridge.Id], ledger.CachesAt(Body, RidgeCamp).Select(c => c.Id));
        Assert.Empty(ledger.CachesAt(Body, WildPlain));
    }
}

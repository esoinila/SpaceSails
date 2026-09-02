namespace SpaceSails.Core;

/// <summary>
/// A named surface site on a landable body — the fixed point a treasure map paces off from
/// (#164, #223). The flagship is the Phobos monolith: a genuine ~85 m boulder near the Stickney
/// rim, photographed by Mars Global Surveyor. The house rule (lesson-11/12 framing) holds — the
/// PLACE is real, the deals struck in its shadow are the fiction we build there.
///
/// <para>Landmarks are deliberately a small Core datum, NOT ephemeris bodies: a map needs a name
/// to pace from ("from the monolith, 40 paces anti-spinward"), not another thing on rails that the
/// picker, depots and traffic pipelines must all learn about. Every landable body has one via the
/// generic fallback, so a cache buried anywhere always mints a legible map.</para>
/// </summary>
/// <param name="BodyId">The body this site sits on (e.g. "phobos").</param>
/// <param name="Name">The site's map-facing name, article included ("the monolith").</param>
/// <param name="HeightMeters">The landmark's height for flavor, and — for the monolith — the ONE fact its
/// whole drawn geometry is derived from (<see cref="Monolith.HeightMetres"/>). Zero when the height is not
/// a recorded fact about a real thing: bug class 1 in this repo is a number typed into a renderer that
/// nothing derives or checks, so a landmark whose canon gives no measurement carries no measurement rather
/// than a plausible invention. The seeded per-site kinds (#1058) are all zero for exactly that reason.</param>
/// <param name="Note">A one-line composition note — doubles as the image-manifest brief for the
/// grok art lane.</param>
public readonly record struct Landmark(string BodyId, string Name, double HeightMeters, string Note);

/// <summary>The catalogue of named landing sites, keyed by body id (#164). One flagship today —
/// the Phobos monolith — plus a generic fallback so every landable body yields a legible map. A
/// plain static registry, the same "one table, not a system" shape as the other Core data rules.</summary>
public static class Landmarks
{
    /// <summary>#649 · THE moon. One word, one object, one ground: the map cards, the drawn slab, the
    /// once-in-a-life nerve hit and the selfie all read this constant through
    /// <see cref="Monolith.BodyId"/>, so the card in your pocket and the thing on the horizon can never
    /// again name two different moons.</summary>
    public const string MonolithBodyId = "phobos";

    /// <summary>The Phobos monolith (#164): the 85 m boulder by the Stickney rim, the outer-system's
    /// quiet meeting place for deals struck away from station security.</summary>
    public static readonly Landmark PhobosMonolith = new(
        MonolithBodyId, "the monolith", 85.0,
        "an 85 m monolith boulder on grey regolith near the Stickney crater rim, long shadow, deals done in its shade");

    private static readonly Dictionary<string, Landmark> ByBody = new()
    {
        [PhobosMonolith.BodyId] = PhobosMonolith,
    };

    /// <summary>The best named site on a body, or a generic landing-beacon fallback so a map paced
    /// off any landable body still reads honestly. The fallback carries the body id so its map text
    /// and image brief still name the place.</summary>
    public static Landmark For(string bodyId) =>
        ByBody.TryGetValue(bodyId, out Landmark l)
            ? l
            : new Landmark(bodyId, "the landing beacon", 0.0, "the survey landing beacon on open regolith");

    /// <summary>True when a body has a hand-authored flagship landmark (not the generic fallback) —
    /// the fetch-a-cache giver prefers to seed rumours at these. Deliberately a question about a BODY and
    /// not about a ground (#1058): it asks whether this moon is storied enough to hang a rumour on, which is
    /// answered by its canon ground, not by whichever seeded shelf the captain happens to set down at.</summary>
    public static bool HasNamedSite(string bodyId) => ByBody.ContainsKey(bodyId);

    // ── #1058 · THE PER-SITE DEAL ───────────────────────────────────────────────────────────────────────
    //
    // #650 made a cache belong to a GROUND; the landmark it paced off stayed per-body, so a chest on the
    // Wild Plain and a chest on the Ridge Camp both read "from the monolith" — pacing from a stone that
    // stands on neither ground in particular (the Ridge Camp cannot see the monolith; Monolith.StandsOn is
    // false everywhere but the canon salt). Canon pass (Fable, 2026-09-02) settles it: each seeded site
    // draws its own landmark, deterministically, and no two grounds on one body ever draw the same kind.
    //
    // SITE 0 IS NOT IN THE DEAL. The Wild Plain is the body's canon ground — the one the authored signature
    // stands on (Phobos' monolith, Miranda's maze) — so it keeps the body's authored landmark exactly as
    // For() has always returned it. That is not a special case bolted on: it is what makes the drawn slab
    // and the card in the pocket one truth, and it is what lets every chest minted before this lane read
    // back byte-identical, because a body-wide chest and a site-0 chest resolve through the same call.

    /// <summary>#1058 · The kinds a seeded (non-canon) ground draws from — Fable-authored canon, verbatim.
    ///
    /// <para>Register, binding: the pylon and the crawler are MUNDANE WRECKAGE. Nothing on them hints at the
    /// fourth world — most stones are stones, and the Scully law does not need every one of them to be a
    /// mystery. The cairn's line is TEXTURE, not lore: the hands are unknown and no card ever asks whose.</para></summary>
    private static readonly (string Name, string Note)[] SitePool =
    [
        ("the split boulder", "one rock in two halves, the gap wide enough to walk."),
        ("the fallen pylon", "a survey mast on its side, older than the survey it is not filed in."),
        ("the dead crawler", "a hauler that stopped mid-track; the track is still there, both directions."),
        ("the old cairn", "stacked by hand, height of a man's chest; nobody stacks the first stone twice."),
    ];

    /// <summary>#1058 · How many seeded grounds the deal must cover — every site on a body's board except
    /// site 0, which keeps the body's authored landmark. The pool must be at least this deep or the deal
    /// could not be made WITHOUT REPLACEMENT, which is the law; a guard asserts it rather than a modulo
    /// quietly re-dealing a kind that is already on the ground next door.</summary>
    public static int SeededSiteKinds => SitePool.Length;

    /// <summary>#1058 · The landmark a captain paces from on ONE GROUND of a body.
    ///
    /// <para>Site 0 (and a body-wide chest, <paramref name="siteIndex"/> null) resolves to
    /// <see cref="For(string)"/> — the canon ground's authored landmark, unchanged wording, forever. Every
    /// other seeded site draws from <see cref="SitePool"/> by a (body, site) deal: the pool is shuffled once
    /// per body off the shared seeded dice and the sites take from it in order, so the draw is WITHOUT
    /// REPLACEMENT — two grounds on one moon can never name the same stone, and a captain who knows his
    /// grounds can tell them apart by the landmark alone.</para>
    ///
    /// <para>The index is clamped into the body's real board the same way <see cref="LandingSites.At"/>
    /// clamps, so a stale or cheat-forced ordinal names a ground that actually exists rather than throwing
    /// or inventing a fifth stone.</para></summary>
    public static Landmark At(string? bodyId, int? siteIndex)
    {
        string id = bodyId ?? "";
        if (siteIndex is not { } raw)
        {
            return For(id);
        }

        int site = LandingSites.At(id, raw).Index;
        if (site <= 0)
        {
            return For(id);
        }

        (string name, string note) = SitePool[DealOrder(id)[site - 1]];
        return new Landmark(id, name, 0.0, note);
    }

    /// <summary>The per-body deal order: a Fisher–Yates shuffle of the pool driven by the shared seeded
    /// dice (never <see cref="System.Random"/>, never the clock), so the same moon deals the same stones to
    /// the same grounds in every session and in every test. The same shape <see cref="LandingSites"/> uses
    /// to hand out its kinds, for the same reason.</summary>
    private static int[] DealOrder(string bodyId)
    {
        int[] order = new int[SitePool.Length];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = DiceRule.Roll(DiceRule.Seed($"landmarks:{bodyId}:deal:{i}"), i + 1).Face - 1; // 0..i
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }
}

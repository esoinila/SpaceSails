using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — the dev-cheat bodies a ?query appends to the world before the ephemeris is built.
public partial class Map
{

    // Kepler rails (PR-B) dev cheat body for /map?ellipse=1: a sun-orbiting rock on a strongly
    // eccentric ellipse (e = 0.6) with periapsis tilted 40° off +X, semi-major axis ~1.4 AU, ~500-day
    // period. Not shipped in any scenario — appended only when the cheat is set — so it exists purely
    // to eyeball the elliptical ring and the non-uniform (fast at periapsis) tracking in-browser.
    private static BodyDefinition KeplerDemoBody() => new()
    {
        Id = "kepler-demo",
        Name = "Kepler Demo",
        ParentId = "sun",
        Mu = 0,
        BodyRadiusM = 4e9,          // oversized so it reads as a clear dot at system zoom
        OrbitRadiusM = 2.1e11,      // semi-major axis
        OrbitPeriodS = 4.32e7,      // ~500 days
        InitialPhaseRad = 0.0,      // mean anomaly at epoch — starts at periapsis
        Eccentricity = 0.6,
        ArgPeriapsisRad = 0.7,      // ~40° periapsis tilt so the ellipse is obviously not axis-aligned
        Kind = "planet",
    };

    // #370: the away-expedition site as a runtime BodyDefinition, appended before the ephemeris is built
    // (the ellipse-cheat idiom — the body list is immutable after). It co-orbits the berth <paramref
    // name="berthId"/> at a fixed small radius comfortably inside one shuttle hop, so a docked ship always
    // reads it as an in-range LANDABLE surface (a Moon-kind body with a parent). Its id carries the site
    // KIND, so the surface routes straight to the authored ground. The passing-asteroid flavor is narrated.
    private static BodyDefinition ExpeditionSiteBody(SiteSpawn spawn, string berthId) => new()
    {
        Id = spawn.BodyId,
        Name = spawn.DisplayName,
        ParentId = berthId,
        Mu = 0,
        BodyRadiusM = spawn.BodyRadiusMeters,
        OrbitRadiusM = ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters * 0.6, // ~1.5e8 m: well inside one 5e8 m hop
        OrbitPeriodS = 1.0e9,       // effectively a static co-orbiting offset — the rock just hangs alongside
        InitialPhaseRad = 0.0,
        Kind = "moon",
    };

    // A fixed, reproducible seed per flavor so the cheat always spawns the same rock (same kind + name).
    private static ulong ExpeditionCheatSeed(ExpeditionFlavor flavor) => flavor == ExpeditionFlavor.MiningSurvey ? 3702UL : 3701UL;

    // #394: the inbound rock as a runtime BodyDefinition, appended before the ephemeris is built (the
    // ellipse-cheat idiom). A Moon-kind body (landable surface) with the DeflectionGig collision RAIL —
    // an eccentric orbit around <paramref name="parentId"/> whose periapsis kisses the Ringside orbit at
    // T-impact. Its id is the DeflectionGig family id so the surface + landing route by id alone.
    private static BodyDefinition DeflectionRockBody(DeflectionGig.RockRail rail, string parentId) => new()
    {
        Id = DeflectionGig.BodyId,
        Name = "Inbound rock",
        ParentId = parentId,
        Mu = 0,
        BodyRadiusM = DeflectionGig.RockBodyRadiusMeters,
        OrbitRadiusM = rail.SemiMajorAxis,
        OrbitPeriodS = rail.OrbitPeriod,
        InitialPhaseRad = rail.InitialPhase,
        Eccentricity = rail.Eccentricity,
        ArgPeriapsisRad = rail.ArgPeriapsis,
        Kind = "moon",
    };

    // A fixed, reproducible seed so the deflection cheat always spawns the same rock (same type + name + spin).
    private const ulong DeflectionCheatSeed = 3940UL;

    // #488: the ?wreck=1 cheat's derelict — a boardable SITE co-orbiting the berth, well inside one shuttle
    // hop. She is a Moon-kind body so the whole board/land rail already knows what to do with her; her id
    // carries the wreck id (Derelict.BodyIdFor), so the deck builder and the excursion route by id alone.
    // The ship holds on her for free while the away team is inside — see LoiterKeeping / Lab 40.
    private const string WreckCheatId = "kestrel-3";

    /// <summary>The hull a wreck cheat boots you onto: the first seeded ship that died THAT way, or the
    /// default hull when no cause was asked for (and when the search finds none, which cannot happen for a
    /// cause the generator can produce, but a null here would be a crash for a typo).
    ///
    /// <para>ONE function rather than an expression at the parse site, because <c>?archive=1</c>'s guard has
    /// to be able to ask "does the hull this cheat produces actually carry a node?" — and a guard that asks a
    /// re-typed copy of the expression is guarding the copy.</para></summary>
    private static Derelict.Wreck CheatWreck(Derelict.WreckCause? cause) =>
        cause is { } forced
            ? Derelict.SeededWithCause(forced) ?? Derelict.Seeded(WreckCheatId)
            : Derelict.Seeded(WreckCheatId);

    private static BodyDefinition WreckSiteBody(string berthId, in Derelict.Wreck wreck) => new()
    {
        Id = Derelict.BodyIdFor(wreck.Id),
        Name = wreck.ShipName,
        ParentId = berthId,
        Mu = 0,
        BodyRadiusM = ExpeditionSite.BodyRadiusMeters,
        OrbitRadiusM = ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters * 0.6, // well inside one hop
        OrbitPeriodS = 1.0e9,       // effectively a static co-orbiting offset — she just hangs there
        InitialPhaseRad = 2.2,      // her own bearing off the berth, clear of the other cheat sites
        Kind = "moon",
    };

    // #409: the ?secretlab=1 cheat's landable rock — a plain Moon-kind body co-orbiting the berth, well inside
    // one shuttle hop, whose surface ResolveSecretLab forces to hide a Vantar lab with the door pre-revealed.
    private const string SecretLabCheatBodyId = "secret-lab-site";

    /// <summary>#592 · The ?secretlab=deep rock. A site's whole shape — how deep it goes, what kind of place
    /// it is, and whether it has a band nobody listed — is seeded off its BODY ID, so reaching the unlisted
    /// band from a URL is a matter of parking a rock with the right name rather than of overriding a Core
    /// fact from the client.
    ///
    /// <para>This one is a 20-floor clinic with an unlisted LABORATORY under it, down to the generator's own
    /// performance guard — which makes it the deepest, most awkward site the game can produce and therefore
    /// the right one to test on. Pinned by <c>TheUnlistedBandTests</c>: if a change to the seeding ever
    /// stopped it having a hidden band, the cheat would quietly stop reaching the feature it exists for.</para></summary>
    private const string SecretLabDeepCheatBodyId = "secret-lab-site-unlisted";

    /// <summary>#677 · The <c>?found=1</c> rock, chosen the same way <see cref="SecretLabDeepCheatBodyId"/>
    /// was: a site's whole shape is seeded off its BODY ID, so reaching the halls from a URL is a matter of
    /// parking a rock with the right name rather than of overriding a Core fact from the client.
    ///
    /// <para>This one is a seven-floor LABORATORY with an unlisted CLINIC under it (B9–B12) and a band of
    /// galleries under a whole band of nothing (B17–B20) — the full chain, in one rock, which is what makes
    /// it the right one to test on. The suffix is the search that found it and is not decoration: the
    /// generator's own dice decide which ids hide anything, and about one in fifty does.</para>
    ///
    /// <para>Read from Core rather than typed here, unlike its two older siblings: five places have to agree
    /// about this id (the cheat and four sweeps that would otherwise audit a universe with no galleries in
    /// it), and five copies of a magic string is the habit this repo's spec opens with a table of.</para></summary>
    private const string SecretLabFoundCheatBodyId = UndergroundComplex.FoundBandCheatSiteId;

    private static BodyDefinition SecretLabSiteBody(string berthId, string id, string name) => new()
    {
        Id = id,
        Name = name,
        ParentId = berthId,
        Mu = 0,
        BodyRadiusM = ExpeditionSite.BodyRadiusMeters,
        OrbitRadiusM = ExpeditionSite.SpawnFraction * ShuttleRange.RangeMeters * 0.6, // ~1.5e8 m: well inside one 5e8 m hop
        OrbitPeriodS = 1.0e9,       // effectively a static co-orbiting offset — the rock just hangs alongside
        InitialPhaseRad = 1.0,      // a different bearing off the berth than the expedition rock
        Kind = "moon",
    };

    // The rock's seeded slow tumble — a spin period (30..90 s of on-site time) and a phase, so the firing
    // window comes around on its own schedule. Pure of the given seed.
    private static (double Period, double Phase) DeflectionSpin(ulong seed)
    {
        var rng = new DeterministicRandom(DiceRule.Seed(seed, "rock-spin"));
        return (rng.NextDouble(30.0, 90.0), rng.NextDouble(0.0, Math.Tau));
    }

    // #370: the cheat's resolved gig spec, stashed at build time (the site body is appended pre-ephemeris)
    // and consumed by InjectExpeditionCheat AFTER the berth clamp, so the accepted plan lands on a live world.
    private (ExpeditionFlavor Flavor, ExpeditionSiteKind Kind, string SiteBodyId, string SiteName)? _pendingExpeditionCheat;
}

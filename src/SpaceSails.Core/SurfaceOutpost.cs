using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #563 · THE OUTPOST — a hut on the regolith with a door you can force, and the first enclosed space an
/// ordinary landing site has ever had.
///
/// <para>Owner, playtesting Miranda 2026-07-31: <i>"the map is kind of boring... no door or enclosed
/// places"</i>, <i>"There should be huts . i.e lockable spaces there not just U shapes"</i>, and — the part
/// that decided what a hut IS — <i>"there could be an overrun illegal space there that was abandoned ... some
/// story that tells the player about the game universe."</i></para>
///
/// <para><b>The fiction does four jobs at once.</b> An illegal operation, overrun, abandoned: it explains why
/// the door is dogged from the INSIDE; it explains why there is ammunition in there that fits your guns
/// (theirs, human calibre, no hand-waving needed); it says the industry was hiding, which implies the
/// regulation, which implies the state — the world's story told by arrangement rather than by a card; and it
/// puts something worth the walk at the far end of a field that had nothing in it.</para>
///
/// <para><b>Why it is VISIBLE.</b> Unlike <see cref="SecretLab"/>'s concealed door, an outpost reads as a
/// landmark from across the field. That is deliberate: the owner's design is that a captain plans a route
/// (<i>"the reload forces the player to plan their routes"</i>, #562), and you cannot plan toward something
/// you cannot see. The hut is the reason to go out; the walk back is the price.</para>
///
/// <para><b>Why the ammunition is PARTIAL.</b> <see cref="CacheRoundsMin"/>..<see cref="CacheRoundsMax"/>,
/// never a full magazine. If found ammo made a captain self-sufficient the tube would stop being an anchor
/// and the whole supply-line design (#562) would evaporate. A cache buys one more fight, not independence.</para>
///
/// <para>Pure and deterministic per (body id, site salt), so a given site always has — or has not — the same
/// hut in the same place, and a revisit finds it where it was.</para>
/// </summary>
public static class SurfaceOutpost
{
    /// <summary>How often a landing site carries one, out of four. Deliberately common: the owner had
    /// <i>never once</i> seen a landing site expand, because the only mechanisms that could grow one were
    /// gated behind body ids no moon can have and a 1-in-40 roll every moon fails. A mechanic nobody meets
    /// is not a rare treat, it is dead code with good manners.
    ///
    /// <para>Set by measurement rather than by taste: a nominal one-in-two put huts on only 7 of the 25
    /// sites the game actually offers, because 25 seeds is a small sample and the dice do not care about
    /// your intentions. Three-in-four is what makes a hut something a captain reliably meets while still
    /// leaving bare sites, so finding one is a small event rather than furniture.</para></summary>
    public const int PresentInFour = 3;

    /// <summary>The seconds of held [E] a dogged hatch costs. Matches the expedition door
    /// (<c>ExpeditionRegions.DoorForceSeconds</c>) so "forcing something" has one feel across the game — and
    /// the tracker keeps sweeping the whole time, which is the real price.</summary>
    public const double ForceSeconds = 5.0;

    /// <summary>The rounds a shelter's ammunition locker holds. Never a full magazine — see the class note.</summary>
    public const int CacheRoundsMin = 20;
    public const int CacheRoundsMax = 40;

    /// <summary>The hut's dimensions in deck units. <see cref="RoomDepth"/> is measured along the door axis
    /// and is deliberately SHORTER than <see cref="SurfaceLayout.EdgeMargin"/>: the hut is built into the far
    /// edge lane, and what is left of that lane (10 − 6 = 4 du) has to stay wide enough for a captain of
    /// radius 0.7 to walk past. Any deeper and the hut would bottle the field it was meant to decorate —
    /// which is exactly the failure the reachability flood exists to catch, and it caught it here first.
    ///
    /// <para>Width runs across the lane, where there is room to spare, so the room is wide and shallow: a
    /// shed against a boundary, which is also what an operation hiding out at the edge of a survey would
    /// actually build.</para></summary>
    private const double RoomDepth = 6.0;
    private const double RoomWidth = 10.0;

    /// <summary>Half the doorway gap — the captain (radius 0.7) walks through comfortably, matching every
    /// other forced doorway in the game.</summary>
    private const double DoorwayHalf = 1.6;

    /// <summary>What a shelter was pretending to be, which is never quite what it was. The label a captain
    /// reads on the outside is the cover; what is inside is the business. None of them says what the Old
    /// Ones are, and none of them ever will — that stays unstated (owner's canon ruling).</summary>
    public enum OutpostCover
    {
        /// <summary>A survey hut. Filed paperwork, real instruments, and a second room's worth of crates.</summary>
        Survey,

        /// <summary>A relay shack. Licensed to repeat traffic; the log shows it repeating rather more.</summary>
        Relay,

        /// <summary>A "sample store" for a mining claim that was never registered at this latitude.</summary>
        Claim,

        /// <summary>A medical station, stocked far past what a crew of four could need.</summary>
        Clinic,
    }

    /// <summary>The kind of thing you can press [E] on inside. A Core enum — the client maps each onto its
    /// own console kind.</summary>
    public enum OutpostConsoleKind
    {
        /// <summary>The ammunition locker — a partial magazine's worth of rounds for your sentries.</summary>
        AmmoCache,

        /// <summary>Somebody's personal effects, left where they dropped them. The story, such as it is.</summary>
        Effects,
    }

    /// <summary>One interactable inside the hut: kind, stable id (the claim-state key), position, label.</summary>
    public readonly record struct OutpostConsole(
        OutpostConsoleKind Kind, string Id, double X, double Y, string Label);

    /// <summary>Where a hut stands, if that tile has one: presence, the dogged hatch's position, which way
    /// the room extends from it (+1 toward increasing x, −1 toward decreasing) — and WHICH TILE it is on.
    ///
    /// <para>#563 · The tile is the identity. Everything a captain does to a hut (forcing it, emptying the
    /// locker, reading the effects) is keyed on this address rather than on "the site's hut", because a site
    /// no longer has one hut — it has one per tile, out as far as anybody walks, and a state keyed on the
    /// site would have every hut in the world open the moment you forced the first.</para></summary>
    public readonly record struct Placement(
        bool HasOutpost, double DoorX, double DoorY, double ExtendDir,
        SurfaceTiles.Address Tile = default);

    /// <summary>The hut a forced hatch appends: the walls, the landmark inside, the interactables, its
    /// axis-aligned bounds (for the born-dark overlay and the reachability audit's interior exemption), and
    /// the point the captain first sees through the doorway.</summary>
    public readonly record struct Region(
        OutpostCover Cover,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<SurfaceLayout.Landmark> Landmarks,
        IReadOnlyList<OutpostConsole> Consoles,
        double MinX, double MinY, double MaxX, double MaxY,
        double RevealX, double RevealY);

    /// <summary>Does this site's HOME tile carry a hut? Kept for every caller that still asks the old
    /// question; <see cref="PresentOn"/> is the one the world asks now.</summary>
    public static bool Present(string bodyId, string siteSalt) =>
        PresentOn(bodyId, siteSalt, SurfaceTiles.Home);

    /// <summary>#563 · Does THIS TILE carry a hut? Seeded per (body, site salt, tile) so the answer is a
    /// fact about a patch of ground rather than about a visit.
    ///
    /// <para>The RATE is unchanged and deliberately so: it was three-in-four per SITE, and a tile is exactly
    /// the old field (<see cref="SurfaceTiles.TileWidthDu"/> × <see cref="SurfaceTiles.TileHeightDu"/>), so
    /// three-in-four per TILE is the same huts per square deck unit the game shipped yesterday. What changes
    /// is that the count no longer stops at one — walk far enough and you meet another, which is the whole
    /// reason to keep walking.</para></summary>
    public static bool PresentOn(string bodyId, string siteSalt, SurfaceTiles.Address tile) =>
        DiceRule.Roll(Seed(bodyId, siteSalt, Tagged("has", tile)), 4).Face <= PresentInFour;

    /// <summary>A seed tag for one tile. The HOME tile keeps the bare tag it has always had, so every roll
    /// the ground under the tube has ever made comes out the same — a site that had a hut still has one, of
    /// the same cover, holding the same rounds. The lattice is new ground, not a reshuffle of old ground.</summary>
    private static string Tagged(string tag, SurfaceTiles.Address tile) =>
        tile == SurfaceTiles.Home ? tag : $"{tag}:{tile.X}_{tile.Y}";

    /// <summary>Which cover story this site's hut wore. Seeded, so a moon's Ridge Camp always holds the same
    /// kind of place.</summary>
    public static OutpostCover CoverFor(string bodyId, string siteSalt) =>
        CoverFor(bodyId, siteSalt, SurfaceTiles.Home);

    /// <inheritdoc cref="CoverFor(string, string)"/>
    public static OutpostCover CoverFor(string bodyId, string siteSalt, SurfaceTiles.Address tile)
    {
        int face = DiceRule.Roll(Seed(bodyId, siteSalt, Tagged("cover", tile)), 4).Face; // 1..4
        return (OutpostCover)(face - 1);
    }

    /// <summary>How many rounds this site's locker holds — <see cref="CacheRoundsMin"/>..<see cref="CacheRoundsMax"/>.</summary>
    public static int CacheRounds(string bodyId, string siteSalt) =>
        CacheRounds(bodyId, siteSalt, SurfaceTiles.Home);

    /// <inheritdoc cref="CacheRounds(string, string)"/>
    public static int CacheRounds(string bodyId, string siteSalt, SurfaceTiles.Address tile)
    {
        int span = CacheRoundsMax - CacheRoundsMin + 1;
        return CacheRoundsMin + DiceRule.Roll(Seed(bodyId, siteSalt, Tagged("rounds", tile)), span).Face - 1;
    }

    /// <summary>#563 · THE STANDOFF — how close to the tube mouth a hut on the home tile may ever stand.
    ///
    /// <para>A hut is a thing you plan a route TO (owner, #562: <i>"the reload forces the player to plan
    /// their routes"</i>), and you cannot plan toward something already standing in the landing lights. The
    /// surface camera shows a field some 64 du across, so about 32 du of ground reaches the eye in any
    /// direction from where the boots land; twice that is the nearest a hut is allowed to be, which makes
    /// every one of them a walk rather than a doorstep.</para>
    ///
    /// <para>It applies to the HOME tile only, because that is the only tile with a tube on it.</para></summary>
    public const double TubeStandoffDu = 64.0;

    /// <summary>How many seeded spots a tile tries before it accepts that it has no room for a hut. A tile
    /// dense enough to reject all of them simply has none — an emptier tile beats a hut built through
    /// somebody's wall, which is the same trade the ground's own generator makes (#563 / #585).</summary>
    private const int PlacementAttempts = 24;

    /// <summary>The clear ground kept between a hut and anything already standing. Matches the elbow room the
    /// ground generator keeps between its own features.</summary>
    private const double Elbow = 1.5;

    /// <summary>Resolve where the HOME tile's hut stands — the old question, unchanged for every caller that
    /// still asks it in field terms.</summary>
    public static Placement For(string bodyId, in SurfaceLayout.Field field, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return For(bodyId, "", field, forcePresent);
    }

    /// <inheritdoc cref="For(string, in SurfaceLayout.Field, bool)"/>
    public static Placement For(string bodyId, string siteSalt, in SurfaceLayout.Field field, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return ForTile(bodyId, siteSalt, SurfaceTiles.Home, forcePresent);
    }

    /// <summary>#563 · WHERE THE HUT ON ONE TILE STANDS — seeded somewhere out on that tile, not in a lane.
    ///
    /// <para>It used to be built INTO the far edge lane: the half-lane <see cref="SurfaceLayout.EdgeMargin"/>
    /// keeps clear at each side, which was the one strip of a landing site guaranteed not to collide with
    /// whatever geography the body had seeded. That was a sound answer to a real problem and it goes with its
    /// premise — <b>an unbounded world has no edge lane</b>, and a hut pinned to one would have been a hut
    /// pinned to a line the captain can no longer see and is no longer stopped by.</para>
    ///
    /// <para>So the hut places itself the way everything else on this ground does: it asks the tile what is
    /// already standing there (<see cref="SurfaceLayout.StandingClaims"/> and the tile's own laid walls) and
    /// takes a seeded spot that clashes with none of it, trying a handful before giving the tile up as full.
    /// Nothing is squeezed and nothing is built through — the audit (<c>SurfaceReachabilityTests</c>) is what
    /// proves that claim rather than this comment.</para>
    ///
    /// <para>Pure and deterministic in <c>(bodyId, siteSalt, tile)</c>: walk away, come back, and the hut is
    /// where you left it, because it was never anywhere else.</para></summary>
    public static Placement ForTile(
        string bodyId, string siteSalt, SurfaceTiles.Address tile, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        bool present = forcePresent || PresentOn(bodyId, siteSalt, tile);
        SurfaceLayout.Field field = SurfaceTiles.GenerationField(bodyId, siteSalt, tile);
        bool home = tile == SurfaceTiles.Home;

        // The span a hatch may stand in, so the whole room lands inside its own tile.
        double half = RoomWidth / 2.0;
        double minX = field.LeftX + RoomDepth + Elbow, maxX = field.RightX - RoomDepth - Elbow;
        double minY = field.BottomY + half + 3.0, maxY = field.LandingBandY - 8.0;
        if (maxX <= minX || maxY <= minY)
        {
            return new Placement(false, field.AnchorX, field.AnchorY, 1.0, tile);
        }

        IReadOnlyList<(double X, double Y, double R)> claims =
            home ? SurfaceLayout.StandingClaims(bodyId, siteSalt, field) : [];
        IReadOnlyList<SurfaceLayout.Wall> laid = SurfaceTiles.Ground(bodyId, siteSalt, tile).Walls;
        (double tubeX, double tubeY) = (field.HomeX, field.LandingBandY);

        for (int attempt = 0; attempt < PlacementAttempts; attempt++)
        {
            bool onPort = DiceRule.Roll(Seed(bodyId, siteSalt, Tagged($"side:{attempt}", tile)), 2).Face == 1;
            double dir = onPort ? -1.0 : 1.0;
            double cx = Lerp(minX, maxX, Frac(bodyId, siteSalt, Tagged($"x:{attempt}", tile)));
            double cy = Lerp(minY, maxY, Frac(bodyId, siteSalt, Tagged($"depth:{attempt}", tile)));

            // Never on the doorstep of the way home.
            if (home && Distance(cx, cy, tubeX, tubeY) < TubeStandoffDu)
            {
                continue;
            }

            double farCx = cx + (dir * RoomDepth);
            (double x0, double y0, double x1, double y1) = (
                Math.Min(cx, farCx) - Elbow, cy - half - Elbow,
                Math.Max(cx, farCx) + Elbow, cy + half + Elbow);

            if (HitsAClaim(x0, y0, x1, y1, claims) || HitsAWall(x0, y0, x1, y1, laid))
            {
                continue;
            }
            return new Placement(present, cx, cy, dir, tile);
        }

        // Nowhere clear on this tile. It has no hut, whatever the presence roll said — a hut that cannot be
        // built is not a hut, and pretending otherwise is how a room ends up inside a building.
        return new Placement(false, field.AnchorX, field.AnchorY, 1.0, tile);
    }

    private static bool HitsAClaim(
        double x0, double y0, double x1, double y1,
        IReadOnlyList<(double X, double Y, double R)> claims)
    {
        foreach ((double cx, double cy, double r) in claims)
        {
            if (x0 < cx + r && x1 > cx - r && y0 < cy + r && y1 > cy - r)
            {
                return true;
            }
        }
        return false;
    }

    private static bool HitsAWall(
        double x0, double y0, double x1, double y1, IReadOnlyList<SurfaceLayout.Wall> walls)
    {
        foreach (SurfaceLayout.Wall w in walls)
        {
            double wx0 = Math.Min(w.X1, w.X2), wx1 = Math.Max(w.X1, w.X2);
            double wy0 = Math.Min(w.Y1, w.Y2), wy1 = Math.Max(w.Y1, w.Y2);
            if (x0 < wx1 && x1 > wx0 && y0 < wy1 && y1 > wy0)
            {
                return true;
            }
        }
        return false;
    }

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>Build the hut a forced hatch appends. Pure and deterministic in (body, salt, placement).</summary>
    public static Region Build(string bodyId, string siteSalt, in Placement p)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        double cx = p.DoorX, cy = p.DoorY, dir = p.ExtendDir;
        double half = RoomWidth / 2.0;
        double farCx = cx + (dir * RoomDepth);
        double hiY = cy + half, loY = cy - half;

        // The shell: two side walls, a near face split around the doorway, and a solid far face. IsHull —
        // this one IS a made thing with a real outside, unlike the field's own envelope (#563).
        var walls = new List<SurfaceLayout.Wall>
        {
            new(cx, hiY, farCx, hiY, true),
            new(cx, loY, farCx, loY, true),
            new(cx, hiY, cx, cy + DoorwayHalf, true),
            new(cx, loY, cx, cy - DoorwayHalf, true),
            new(farCx, hiY, farCx, loY, true),
        };

        // NO inner partition. A shed this shallow does not need dressing, and every stub I sketched for it
        // landed within a console's 3 du interact radius — the exact crowding the wreck lane paid for three
        // times over (#517/#520). The room's texture is what is IN it, not what divides it.
        OutpostCover cover = CoverFor(bodyId, siteSalt, p.Tile);

        // The two interactables are separated ACROSS the room rather than along it, because the room is
        // shallow: ~6 du apart, comfortably outside the 3 du radius, so each one is reachable as the
        // ANSWERING console rather than merely standing near walkable ground (the #520 law).
        var consoles = new List<OutpostConsole>
        {
            new(OutpostConsoleKind.AmmoCache, ConsoleId(bodyId, siteSalt, p.Tile, "ammo"),
                cx + (dir * 2.2), cy - 2.8, "🔫 AMMUNITION LOCKER"),
            new(OutpostConsoleKind.Effects, ConsoleId(bodyId, siteSalt, p.Tile, "effects"),
                cx + (dir * 4.2), cy + 2.8, "🧳 PERSONAL EFFECTS"),
        };

        var landmarks = new List<SurfaceLayout.Landmark>
        {
            new(cx + (dir * 3.0), cy + 4.2, InsideLabel(cover)),
        };

        double minX = Math.Min(cx, farCx), maxX = Math.Max(cx, farCx);
        return new Region(
            cover, walls, landmarks, consoles,
            minX, loY, maxX, hiY,
            RevealX: cx + (dir * RoomDepth * 0.5), RevealY: cy);
    }

    /// <summary>The label on the OUTSIDE — the cover story, painted on by people who wanted to be boring.</summary>
    public static string DoorLabel(OutpostCover cover) => cover switch
    {
        OutpostCover.Survey => "⌂ SURVEY HUT — SEALED",
        OutpostCover.Relay => "⌂ RELAY SHACK — SEALED",
        OutpostCover.Claim => "⌂ SAMPLE STORE — SEALED",
        _ => "⌂ MEDICAL STATION — SEALED",
    };

    /// <summary>The landmark INSIDE, once the hatch is off — the first thing that does not match the sign.</summary>
    public static string InsideLabel(OutpostCover cover) => cover switch
    {
        OutpostCover.Survey => "▨ CRATES — NO SURVEY MARKINGS",
        OutpostCover.Relay => "▨ RACKS — MORE THAN A RELAY NEEDS",
        OutpostCover.Claim => "▨ SAMPLES — NONE OF THEM ROCK",
        _ => "▨ STOCK — FOR RATHER MORE THAN FOUR",
    };

    /// <summary>What the door says as it gives. Names the cover, so the mismatch lands the moment you are in.</summary>
    public static string ForcedLine(OutpostCover cover) => cover switch
    {
        OutpostCover.Survey =>
            "⚙ The hatch gives. Dogged from the INSIDE — whoever did that is still in here or never left.",
        OutpostCover.Relay =>
            "⚙ The hatch gives. A relay shack with a blast door: somebody expected to be found.",
        OutpostCover.Claim =>
            "⚙ The hatch gives. No claim marker outside, and a door this heavy on a store shed.",
        _ =>
            "⚙ The hatch gives. Stale air, and the cold-store light still drawing off a dead cell.",
    };

    /// <summary>The line the personal effects read out. Texture, never testimony: a badge, a payslip, a
    /// photograph. It implies an industry that went dark and a state worth hiding from, and it says NOTHING
    /// about what is walking around outside — that stays unstated, always (owner's canon ruling).</summary>
    public static string EffectsLine(OutpostCover cover) => cover switch
    {
        OutpostCover.Survey =>
            "🧳 A wallet, a folded chit, a site badge on a snapped lanyard. The badge is for a facility with a " +
            "number and no name, and the chit is a transit voucher — one way, paid by the employer, issued the " +
            "week the licences got harder.",
        OutpostCover.Relay =>
            "🧳 A wallet, and inside it a photograph of a work crew squinting in somebody else's sunlight. " +
            "Eleven faces. The payslip folded behind it is drawn on a holding company you have never heard of, " +
            "for hours nobody logs on a relay.",
        OutpostCover.Claim =>
            "🧳 A wallet. A site badge, a hand-drawn map on the back of an inventory sheet, and a letter begun " +
            "and not finished: they had moved the whole operation out past the survey line, and the pay was " +
            "good precisely because nobody would be coming to look.",
        _ =>
            "🧳 A wallet with a clinic badge and a dosing card, both for a facility that is a number. Whoever " +
            "carried it had signed for far more consumables than a station this size could ever use, and had " +
            "started keeping a private tally on the back.",
    };

    /// <summary>The receipt for lifting the locker. Reads like the sentries' own rearm chit (#119 voice).</summary>
    public static string CacheLine(int rounds) =>
        $"🔫 Their locker: {rounds} rounds, the right calibre, still greased. Racked across your sentries.";

    /// <summary>
    /// SOMEBODY'S EFFECTS, ON A FLOOR THEY DID NOT WALK OFF — one plate, all four covers.
    ///
    /// <para>This is the console in the game most obviously ABOUT a person, and until now the only picture
    /// it had was the dossier's. It is also the whole story the hut tells: a wallet, a badge for a facility
    /// with a number and no name, a chit paid by an employer. It implies an industry that went dark and a
    /// state worth hiding from, and it says NOTHING about what is walking around outside — that stays
    /// unstated, always (owner's canon ruling).</para>
    ///
    /// <para><b>One canvas for all four of <see cref="OutpostCover"/>, on the wrecks' anti-tell law.</b> The
    /// cover is already announced on the door, so the DIFFERENCE is not a secret — but the effects are the
    /// same four objects on every hut and painting four of them would only invite a fifth reading that
    /// isn't there. <see cref="EffectsLine"/> carries the whole of it, as it already did.</para>
    ///
    /// <para>Every surface in the painting is blank. The words are the code's job.</para>
    /// </summary>
    public static readonly RevealPlate EffectsPlate = new(
        "WHAT WAS LEFT ON THE FLOOR",
        "art/outpost-effects.jpg",
        "A wallet fallen open and emptied, a laminated card on a snapped lanyard, a sheet folded into "
        + "quarters, a photograph lying face down, and some small change nobody came back for. Whoever set "
        + "these down in a sealed hut on an airless moon did not set them down expecting to be leaving.");

    /// <summary>#563 law 2 · A hut interactable's STABLE ID, and it is keyed on the TILE.
    ///
    /// <para>What the captain spends is not recomputed from a seed — an emptied locker is a thing that
    /// happened, not a thing the ground knows — so it is remembered against this string. It carries the tile
    /// address because a site has many huts now; without it, emptying one locker would empty every locker on
    /// the moon, which is the exact "the world quietly becomes wallpaper" failure this issue is about. The
    /// home tile keeps the id it has always had, so nothing already remembered is forgotten.</para></summary>
    public static string ConsoleId(string bodyId, string siteSalt, SurfaceTiles.Address tile, string what) =>
        tile == SurfaceTiles.Home
            ? $"outpost:{bodyId}:{siteSalt}:{what}"
            : $"outpost:{bodyId}:{siteSalt}:{tile.X}_{tile.Y}:{what}";

    // ── seeding ────────────────────────────────────────────────────────────────────────────────────────
    private const int Resolution = 4096;

    private static ulong Seed(string bodyId, string siteSalt, string tag) =>
        DiceRule.Seed($"outpost:{bodyId}:{siteSalt}:{tag}");

    private static double Frac(string bodyId, string siteSalt, string tag)
    {
        int face = DiceRule.Roll(Seed(bodyId, siteSalt, tag), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}

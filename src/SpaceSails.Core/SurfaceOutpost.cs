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

    /// <summary>Where a site's hut stands, if it has one: presence, the dogged hatch's position, and which
    /// way the room extends from it (+1 toward increasing x, −1 toward decreasing).</summary>
    public readonly record struct Placement(bool HasOutpost, double DoorX, double DoorY, double ExtendDir);

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

    /// <summary>Does this site carry a hut? Seeded per (body, site salt) so two sites on the same moon differ
    /// — which is the point of having sites at all.</summary>
    public static bool Present(string bodyId, string siteSalt) =>
        DiceRule.Roll(Seed(bodyId, siteSalt, "has"), 4).Face <= PresentInFour;

    /// <summary>Which cover story this site's hut wore. Seeded, so a moon's Ridge Camp always holds the same
    /// kind of place.</summary>
    public static OutpostCover CoverFor(string bodyId, string siteSalt)
    {
        int face = DiceRule.Roll(Seed(bodyId, siteSalt, "cover"), 4).Face; // 1..4
        return (OutpostCover)(face - 1);
    }

    /// <summary>How many rounds this site's locker holds — <see cref="CacheRoundsMin"/>..<see cref="CacheRoundsMax"/>.</summary>
    public static int CacheRounds(string bodyId, string siteSalt)
    {
        int span = CacheRoundsMax - CacheRoundsMin + 1;
        return CacheRoundsMin + DiceRule.Roll(Seed(bodyId, siteSalt, "rounds"), span).Face - 1;
    }

    /// <summary>Resolve where this site's hut stands.
    ///
    /// <para>It is placed in the FAR EDGE LANE — the half-lane <see cref="SurfaceLayout.EdgeMargin"/> keeps
    /// clear at each side, where <i>"no generated feature ever intrudes"</i>. That is the one strip of a
    /// landing site guaranteed not to collide with whatever geography the body seeded, which is what makes
    /// dropping a room onto an arbitrary seeded ground safe at all. The walk-around lane surviving is not
    /// assumed: <c>SurfaceReachabilityTests</c> floods every body × every site and fails if the hut ever
    /// bottles the captain in.</para>
    ///
    /// <para>Depth is seeded between the landing band and the deep rim, so some huts are a short stroll and
    /// others are a genuine commitment — which is the decision the whole excursion is made of.</para></summary>
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

        // Port or starboard edge lane, seeded. The hut is built INTO the lane — its back against the field's
        // boundary — and the hatch faces INWARD, so a captain walking the open ground meets the door rather
        // than the back wall. ExtendDir therefore points OUTWARD, toward the edge.
        bool onPort = DiceRule.Roll(Seed(bodyId, siteSalt, "side"), 2).Face == 1;
        double dir = onPort ? -1.0 : 1.0;
        double doorX = onPort
            ? field.LeftX + RoomDepth
            : field.RightX - RoomDepth;

        // Deep enough to be a walk, never so deep it overhangs the bottom rim.
        double loY = field.BottomY + (RoomWidth / 2.0) + 3.0;
        double hiY = field.LandingBandY - 8.0;
        double doorY = Lerp(loY, hiY, Frac(bodyId, siteSalt, "depth"));

        return new Placement(forcePresent || Present(bodyId, siteSalt), doorX, doorY, dir);
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
        OutpostCover cover = CoverFor(bodyId, siteSalt);

        // The two interactables are separated ACROSS the room rather than along it, because the room is
        // shallow: ~6 du apart, comfortably outside the 3 du radius, so each one is reachable as the
        // ANSWERING console rather than merely standing near walkable ground (the #520 law).
        var consoles = new List<OutpostConsole>
        {
            new(OutpostConsoleKind.AmmoCache, $"outpost:{bodyId}:{siteSalt}:ammo",
                cx + (dir * 2.2), cy - 2.8, "🔫 AMMUNITION LOCKER"),
            new(OutpostConsoleKind.Effects, $"outpost:{bodyId}:{siteSalt}:effects",
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

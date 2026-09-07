using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #409 · THE SECRET LABS BEHIND HIDDEN DOORS (owner, 2026-07-20, 😎: "Do we have hidden doors at landing
/// sites? Secret Dr Soong Labs."). We ship VISIBLE sealed-door consoles on expedition sites (#393 forces
/// one → a region appends). This is the darker cousin: a door that is CONCEALED — not on the ground at all
/// until DISCOVERED — hiding the sealed lab of <b>Dr. Mielos Vantar</b>, a disgraced reclusive
/// cyberneticist who vanished into the deep field (an ORIGINAL homage, never "Dr Soong" — trademark).
///
/// <para>This is the pure, deterministic Core spine (repo law §9 — determinism is law in Core):</para>
/// <list type="bullet">
/// <item><b>Seeded presence</b> — most bodies hide nothing; a lab is rare (a veterans'-rumor payoff),
/// rarer still on an ordinary dig moon than in the deep field of an away-expedition site. Pure of the body
/// id off the ONE shared <see cref="DiceRule"/> engine (never <see cref="System.Random"/> or the clock).</item>
/// <item><b>A hidden door</b> seeded to one beach-comber square (<see cref="BeachComber.SquareOf"/>) in the
/// deep field — a metal-detector probe on that square PINGS and reveals it; adjacent squares shriek a
/// proximity hint. (Hooks for a bought rumor naming a moon or a seeded scan reuse the same
/// <see cref="Placement"/>.)</item>
/// <item><b>The lab region</b> a forced door appends: a distinct inner scheme — lab benches, stasis pods, a
/// server spine — laid inside the shared <see cref="SurfaceLayout.Field"/> envelope exactly like every other
/// scheme so the edge lanes stay open. Contents: a fat one-time discovery cache, Vantar's log consoles
/// (<see cref="VantarLore"/>), a brain-in-a-jar backup rig that winks at the game's own brain-backup fiction
/// (labelled DO NOT REVIVE), and a bounded risk (a dormant synthetic).</item>
/// <item><b>The reveal</b> — reading the core log is a nerve hit (<see cref="RevealShock"/>, the #391 reveal
/// idiom + <see cref="NerveModel.Shock"/>) with a DICED outcome (<see cref="RollReveal"/>): salvage the tech
/// for heroic pay, or it salvages you (a bigger nerve hit + a limited pack rouses). Dice shown — house law.</item>
/// </list>
///
/// <para>KAAMOS cross-link (#411): Vantar's vanished work MAY be the ice-moon project, or the project that
/// made and hid him. That lane owns its own <c>KaamosLore</c> pool + docs; we keep our fragments to Vantar's
/// OWN logs and leave the wiring to a follow-up — see the comment hook in <see cref="VantarLore"/>.</para>
///
/// <para>#251 · This is the opening file of a six-part family, and the other five are named for the step
/// they own: <c>.Placement</c> (whether a body hides one, where its door is, and the footprint that keeps
/// out), <c>.HeadHut</c> (#606's lift head, which is an ORDINARY hut), <c>.Detector</c> (#585's readings
/// that get warmer, and the lead that names a moon), <c>.Build</c> (the region a forced door appends), and
/// <c>.Reveal</c> (the roll, the two plates, and the family's shared pure builders).</para>
///
/// <para>Four static fields live in this family — <c>ChamberNames</c>, <c>WallChamberContents</c>,
/// <c>DoorPlate</c> and <c>TheyStandPlate</c> — and every one of them reads nothing but literals, so no
/// initializer chain can be broken by the cut. <c>TotalDepth</c> reads two other sizes and is a
/// <c>const</c>, which is folded at compile time and cannot be ordered wrongly at all. See
/// <c>NoPartialClassSpreadsItsStaticFieldsTests</c>. No member is renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class SecretLab
{
    // ── Seeded presence. Low odds, big payoff — the thing veterans chase and tell stories about. ──

    /// <summary>The deep field of an away-expedition site hides a lab about 1 in this many — Vantar hid his
    /// work where charter crews rarely dig deep. Higher than an ordinary moon (that is where the rumors
    /// point). OWNER-TUNABLE.</summary>
    public const int ExpeditionOneInN = 5;

    /// <summary>An ordinary dig moon hides a lab about 1 in this many — genuinely rare, the veterans'
    /// once-a-career find. OWNER-TUNABLE.</summary>
    public const int OrdinaryOneInN = 40;

    // ── The payoff + the risk (all FLAGGED for the owner's tuning). ──

    /// <summary>The fat one-time discovery cache the lab banks — Vantar's tech is worth a career's coin.
    /// Far above an expedition chamber's <see cref="ExpeditionRegions.DiscoveryBonusDepth2"/> (1800): this is
    /// the rare find, not a routine chamber. OWNER-TUNABLE.</summary>
    public const int DiscoveryCacheCredits = 5000;

    /// <summary>Reading the core log / the first sight of what shouldn't exist — the nerve hit, a lump not a
    /// rate, the #391 reveal idiom. On a par with the monolith's first-sight shock. OWNER-TUNABLE.</summary>
    public const double RevealShock = 22.0;

    /// <summary>The EXTRA nerve the "it salvages you" branch costs on top of <see cref="RevealShock"/> — the
    /// dormant synthetic's eyes come open. OWNER-TUNABLE.</summary>
    public const double CostBranchExtraShock = 12.0;

    /// <summary>A D20 at or above this salvages the tech for heroic pay; below it, the reveal costs you. So
    /// the player has the better odds — but the downside is real. House law: the die is shown. OWNER-TUNABLE.</summary>
    public const int SalvageMinRoll = 9;

    /// <summary>The fewest / most credits the salvaged tech pays when the reveal goes the captain's way — a
    /// heroic haul on top of the discovery cache. OWNER-TUNABLE.</summary>
    public const int SalvagePayMin = 2500;
    public const int SalvagePayMax = 7000;

    /// <summary>The dormant synthetic wakes as a LIMITED pack on the bad branch — never the endless Miranda
    /// stream (the owner's hard line, mirrored from the expedition's cap). OWNER-TUNABLE.</summary>
    public const int WakePackMin = 2;
    public const int WakePackMax = 4;

    /// <summary>Half the hidden doorway's width in deck units — the gap left in the near wall the captain
    /// walks through once the door is forced, matching the expedition doorway (~3.2 du).</summary>
    private const double DoorwayHalf = 1.6;

    /// <summary>The lab chamber's depth (along the door axis) and full width (across it), in deck units — a
    /// roomy vault so the benches, spine and pods dress it without ever sealing the walk from the door to the
    /// consoles.</summary>
    private const double RoomDepth = 16.0;
    private const double RoomWidth = 14.0;

    /// <summary>How far each chamber past the first runs into the rock. Shorter than the antechamber: they were
    /// cut later, by people whose budget had run out, and a captain should feel the ceiling coming down.</summary>
    private const double DeepChamberDepth = 11.0;

    /// <summary>How far the whole lab runs into the rock — what the placement has to reserve, and the number
    /// that has to be a SINGLE number, because a placement that reserves less than the build uses is the
    /// map-disagrees-with-the-ground bug in its purest form.</summary>
    private const double TotalDepth = RoomDepth + (2 * DeepChamberDepth);

    /// <summary>The kind of interactable inside a forced lab. A Core enum (no client dependency); the client
    /// maps each onto its own <c>DeckPlan.ConsoleKind</c>.</summary>
    public enum LabConsoleKind
    {
        /// <summary>The fat one-time discovery cache — press E to bank <see cref="DiscoveryCacheCredits"/>.</summary>
        DiscoveryCache,

        /// <summary>A log console — read a Vantar fragment (<see cref="VantarLore"/>). The CORE log (the
        /// deepest one) is the reveal trigger: reading it deals the nerve hit + rolls the diced outcome.</summary>
        LoreLog,

        /// <summary>The brain-in-a-jar backup rig — a view/lore prop labelled DO NOT REVIVE, winking at the
        /// game's own brain-backup fiction.</summary>
        BrainJar,

        /// <summary>The dormant synthetic on its bench — the bounded risk. Interacting with it (or reading the
        /// core log) is what may rouse the limited pack.</summary>
        DormantSynth,

        /// <summary>#409+ · The mimic DOOR BOARD, in the clean room. Owner: <i>"Surely some control panels based
        /// on the vent panel can be added 🤠"</i> — the same idiom as the atmosphere board, drawn from the same
        /// chamber rectangles the walls are, so a switch on the board IS a door on the ground by construction.
        /// Throwing a door from here is what makes a lock a tool rather than a walk.</summary>
        DoorBoard,

        /// <summary>#409+ · The alarm panel — <i>"something to try to hack"</i>. A shown die, a named modifier
        /// stack, and a countdown that a wrong answer makes shorter.</summary>
        AlarmPanel,

        /// <summary>#409+ · Vantar's card, in the deepest chamber. The only thing that opens a lockdown, kept
        /// where a captain who ran at the first alarm will not have been.</summary>
        KeyCard,
    }

    /// <summary>One interactable inside the lab — its kind, a stable id (the claim/read-state key), where it
    /// sits, its house-voice label, and (for a <see cref="LabConsoleKind.LoreLog"/>) which lore fragment it
    /// reads and whether it is the CORE log (the reveal trigger).</summary>
    public readonly record struct LabConsole(
        LabConsoleKind Kind, string Id, double X, double Y, string Label, int LoreIndex, bool IsCoreLog);

    /// <summary>The lab's ground: a scheme name, the chamber walls (collision law for everyone), landmark
    /// label(s), the interactables, the discovery bonus, the axis-aligned bounds (for a born-dark overlay and
    /// the tests), and the reveal sample point (the chamber's heart — seen only through the doorway).</summary>
    public readonly record struct Region(
        string Scheme,
        IReadOnlyList<SurfaceLayout.Wall> Walls,
        IReadOnlyList<SurfaceLayout.Landmark> Landmarks,
        IReadOnlyList<LabConsole> Consoles,
        int DiscoveryBonus,
        double MinX, double MinY, double MaxX, double MaxY,
        double RevealX, double RevealY,
        IReadOnlyList<LabDoor> Doors,
        // #822 · The ways out of here that are not on any board. Appended, so every caller that builds a
        // region positionally still means the same mountain.
        IReadOnlyList<HiddenWay>? Hidden = null)
    {
        /// <summary>#822 · The lab's hidden ways out, never null.</summary>
        public IReadOnlyList<HiddenWay> TheHidden => Hidden ?? [];
    }

    /// <summary>
    /// #822 · A WAY OUT OF THE MOUNTAIN THAT IS ITSELF HIDDEN.
    ///
    /// <para>Owner's ruling on the fire-code sweep, 2026-08-11: <i>"the second exit is itself hidden — a
    /// service crawl or second hidden door, found the way the first one is. The lab obeys the code the
    /// building pretends to follow: two doors nobody can see."</i></para>
    ///
    /// <para>THE HEART was the one room in this game you could be sealed into: three chambers in a line,
    /// each behind a door the board can throw, and the deepest of them ended in solid rock. A captain who
    /// let the alarm lock the run down was in a box. Now the rock at the back of it is a plate, and behind
    /// the plate is a crawl out into the mountain — and the fire code is satisfied by a facility that would
    /// never have admitted to owning one.</para>
    ///
    /// <para><b>Shut is a WALL</b>, exactly as a shut <see cref="LabDoor"/> is (#465): the region's own wall
    /// list leaves the gap, and whoever is drawing the ground lays <see cref="HiddenWay.Plug"/> across it
    /// until the way has been forced. So nothing is walkable that has not been found, and nothing about the
    /// sealed lab changes by one segment.</para>
    /// </summary>
    /// <param name="Id">Stable key for its state, and what the force channel calls it. Never shown.</param>
    /// <param name="Chamber">Which chamber it lets out of.</param>
    /// <param name="X">The middle of the gap — the rock you set your shoulder to, going out.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Plug">The segment that stands in the gap while it is still rock.</param>
    /// <param name="Line">What a captain standing at it is told. It names no destination and no department:
    /// a plate would make it a door, and the whole of the ruling is that this is not one.</param>
    public readonly record struct HiddenWay(
        string Id, string Chamber, double X, double Y, SurfaceLayout.Wall Plug, string Line);

    /// <summary>
    /// One door between chambers. Owner: <i>"a secret lab that extends into a mountain … Doors that lock is a
    /// cool feature for doing a secret lab."</i> Each is a real gap in a real wall with a
    /// <see cref="LockedDoor.State"/> the client owns, and the mimic board can throw any of them from anywhere
    /// in the lab — which is what makes a lock a TOOL rather than a walk.
    /// </summary>
    /// <param name="Id">Stable key for its state, and what the board calls it.</param>
    /// <param name="X">Centre of the gap, on the chamber axis.</param>
    /// <param name="Y">…and across it.</param>
    /// <param name="Deeper">The chamber it leads INTO, going in.</param>
    public readonly record struct LabDoor(string Id, double X, double Y, string Deeper);

    /// <summary>The chambers, shallow to deep. Named so the board can label them and the captain can say where
    /// they are — "the heart" is a place, not a coordinate.</summary>
    public static IReadOnlyList<string> ChamberNames { get; } =
        ["THE ANTECHAMBER", "THE CLEAN ROOM", "THE HEART"];

    /// <summary>
    /// #537 + #409 · A CHAMBER CUT INTO THE ROCK BEHIND A CHAMBER. Owner: <i>"I love mountain labs as there is
    /// endless places for secret chambers in the outer walls."</i>
    ///
    /// <para>He is right, and the reason it costs almost nothing to give him is that a lab in a MOUNTAIN has the
    /// one thing a ship spent two PRs acquiring: unlimited unaudited depth. A hull had to be given a shielding
    /// band and a machinery space before a void had anywhere to be — the rock was already there. Cut a room and
    /// there is more rock behind it, for as far as anyone cares to dig.</para>
    ///
    /// <para><b>And it needs no second search mechanic.</b> The captain already knocks (<see cref="HullSounding"/>):
    /// same two gears, same clock, same noise, same three readings. Which is the best possible outcome — a verb
    /// built for hulls turns out to work on a mountain unchanged, and the pack hears it there too.</para>
    /// </summary>
    /// <param name="Chamber">Which chamber's outboard wall it is behind.</param>
    /// <param name="PlateX">The false rock face — what you knock on and what comes away.</param>
    /// <param name="PlateY">…on the chamber wall, so a captain stands inside and reaches it.</param>
    /// <param name="Holds">What is in there, in the captain's own words.</param>
    public readonly record struct WallChamber(string Chamber, double PlateX, double PlateY, string Holds);

    /// <summary>How many of the lab's walls hide something. Two of three chambers on a hull that has a lab at
    /// all — far commoner than a wreck's one-in-five, because the whole point of a mountain is that there is
    /// always more rock, and because a captain who has got this deep has earned a reason to keep knocking.</summary>
    public const int WallChambersPerLab = 2;

    /// <summary>What Vantar kept in the walls rather than on the benches. Each is a fact about him, not a prize:
    /// the man walled things up, and what he chose to wall up is the characterisation.</summary>
    private static readonly string[] WallChamberContents =
    [
        "A second backup rig, smaller, running on its own cell. The jar is empty and the log says it was not.",
        "Forty-one identical notebooks, hand-numbered, all of them log 44. The handwriting drifts across the run.",
        "A cot, a lamp, a water line, and a door that bolts from the INSIDE. He was not hiding this from us.",
        "Nine sets of restraints, sized for something with the shape of a person and not the patience of one.",
    ];

    /// <summary>
    /// The hidden chambers behind a given lab's walls. Seeded off the body so a captain who comes back finds the
    /// same rock — the same law the hull voids follow, and for the same reason: a secret that re-rolls is a
    /// lottery rather than a place.
    /// </summary>
    public static IReadOnlyList<WallChamber> WallChambersOf(string bodyId, in Region region)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        var found = new List<WallChamber>();
        if (region.Consoles is null || region.Consoles.Count == 0)
        {
            return found;
        }

        // One per chamber, at most WallChambersPerLab of them, on alternating sides so a captain cannot learn
        // "always the high wall" and stop looking at the other one.
        double span = region.MaxX - region.MinX;
        for (int i = 0; i < WallChambersPerLab && i < ChamberNames.Count; i++)
        {
            ulong seed = DiceRule.Seed(0UL, $"lab-wall:{bodyId}:{i}");

            // Placed along that chamber's own stretch of the lab, clear of its doors.
            double t0 = (i + 0.5) / (ChamberNames.Count + 0.0);
            double jitter = (DiceRule.Roll(DiceRule.Seed(seed, "along"), 9).Face - 5) / 40.0;
            double plateX = region.MinX + (span * System.Math.Clamp(t0 + jitter, 0.08, 0.92));
            double plateY = i % 2 == 0 ? region.MaxY : region.MinY;

            found.Add(new WallChamber(
                ChamberNames[i], plateX, plateY,
                WallChamberContents[DiceRule.Roll(DiceRule.Seed(seed, "holds"),
                                                  WallChamberContents.Length).Face - 1]));
        }

        return found;
    }
}

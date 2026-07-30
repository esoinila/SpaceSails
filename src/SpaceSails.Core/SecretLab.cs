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
/// </summary>
public static class SecretLab
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
        IReadOnlyList<LabDoor> Doors);

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

    /// <summary>The seeded placement of a body's hidden door: whether the body hides a lab at all, the door's
    /// ground position, and the beach-comber square a probe must ping to reveal it. Pure of (body id, field).</summary>
    public readonly record struct Placement(
        bool HasLab, double DoorX, double DoorY, int DoorSquareX, int DoorSquareY);

    /// <summary>Resolve a body's hidden-door placement inside its field. <paramref name="forcePresent"/> lets
    /// the client's <c>?secretlab=1</c> cheat guarantee a lab on the test body regardless of the seed (Core
    /// stays deterministic; only the cheat overrides). Pure — the same body always answers the same way.</summary>
    public static Placement For(string bodyId, in SurfaceLayout.Field field, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // The door's ground spot: a seeded pocket in the DEEP field (a committed walk from the tube), kept clear
        // of the far edges so the whole lab has room to grow inside the safe span.
        //
        // THE RESERVATION IS THE FULL DEPTH NOW, AND THE SIDE IS SEEDED. When the lab was one chamber it fitted
        // whichever way it grew, so the old rule reserved RoomDepth on BOTH sides and let the direction fall out
        // of which half the door landed in. Three chambers run 38 du into the rock — more than half the field —
        // and reserving that on both sides inverts the range: there is no spot with 38 du spare in each
        // direction. `Region_Bounds_StayInsideTheFieldsSafeSpan` caught it on the first run, which is exactly
        // the audit doing its job.
        //
        // So the side is drawn from the seed and the position is drawn from that side's own valid range. The
        // lab still lands anywhere along the field; it simply no longer telegraphs which way it runs from where
        // its door is, which is a small improvement thrown in for free.
        double lo = field.LeftX + SurfaceLayout.EdgeMargin;
        double hi = field.RightX - SurfaceLayout.EdgeMargin;
        bool growsRight = Frac(bodyId, "door-side") < 0.5;

        double loX = growsRight ? lo : lo + TotalDepth;
        double hiX = growsRight ? hi - TotalDepth : hi;

        double loY = field.BottomY + (RoomWidth / 2.0) + 2.0;
        double hiY = field.AnchorY + 12.0; // deep, well below the landing band
        double doorX = Lerp(loX, hiX, Frac(bodyId, "door-x"));
        double doorY = Lerp(loY, hiY, Frac(bodyId, "door-y"));

        bool has = forcePresent || Present(bodyId);
        (int sqX, int sqY) = BeachComber.SquareOf(doorX, doorY);
        return new Placement(has, doorX, doorY, sqX, sqY);
    }

    /// <summary>Whether the seed alone (no cheat) hides a lab on this body — 1 in <see cref="ExpeditionOneInN"/>
    /// in the deep field of an away-expedition site, 1 in <see cref="OrdinaryOneInN"/> on an ordinary moon.</summary>
    public static bool Present(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int oneInN = ExpeditionSite.TryParseKind(bodyId, out _) ? ExpeditionOneInN : OrdinaryOneInN;
        return DiceRule.Roll(DiceRule.Seed($"secretlab:has:{bodyId}"), oneInN).Face == 1;
    }

    /// <summary>Whether a probe of (<paramref name="squareX"/>, <paramref name="squareY"/>) is close enough to
    /// the hidden door to shriek a PROXIMITY hint (the detector "very close") — the door's own square, or any
    /// of the eight around it. The exact-square case (a reveal) is <see cref="IsDoorSquare"/>.</summary>
    public static bool IsProximitySquare(in Placement p, int squareX, int squareY) =>
        System.Math.Abs(squareX - p.DoorSquareX) <= 1 && System.Math.Abs(squareY - p.DoorSquareY) <= 1;

    /// <summary>Whether a probe of this square lands exactly on the hidden door — the reveal.</summary>
    public static bool IsDoorSquare(in Placement p, int squareX, int squareY) =>
        squareX == p.DoorSquareX && squareY == p.DoorSquareY;

    // ── The lab region a forced door appends. Distinct inner scheme (benches / stasis pods / server spine),
    //    clamped inside the field's safe span so the edge lanes stay open, and hand-verified to leave the
    //    door→console lane walkable (a test pins that no wall crowds a console). ──
    /// <summary>Build the lab chamber the hidden door at (<paramref name="doorX"/>, <paramref name="doorY"/>)
    /// appends, laid inside <paramref name="field"/>. Pure and deterministic in (body id, door position).</summary>
    public static Region Build(string bodyId, in SurfaceLayout.Field field, double doorX, double doorY)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Which way the placement RESERVED for. Read from the same seed rather than re-derived from the door's
        // position: a chamber that grew the other way from the one the margin was reserved on would run out
        // through the field edge, and it would do it silently.
        double dir = Frac(bodyId, "door-side") < 0.5 ? 1.0 : -1.0;
        double cx = doorX, cy = doorY;
        double half = RoomWidth / 2.0;
        double farCx = cx + (dir * RoomDepth);

        double nearHiY = cy + half, nearLoY = cy - half;
        var walls = new List<SurfaceLayout.Wall>();

        // Two side walls (hull — solid, opaque cover), running door → far along the extend axis.
        walls.Add(new(cx, nearHiY, farCx, nearHiY, true));
        walls.Add(new(cx, nearLoY, farCx, nearLoY, true));
        // The near face, split into two stubs leaving the doorway gap at the door centre.
        walls.Add(new(cx, nearHiY, cx, cy + DoorwayHalf, true));
        walls.Add(new(cx, nearLoY, cx, cy - DoorwayHalf, true));
        // The far face is NOT solid any more — it has a doorway, and the mountain keeps going. Owner: "a
        // secret lab that extends into a mountain". One chamber was a vault; three chambers with doors between
        // them is a place you go INTO, which is what makes a locked door behind you mean anything.
        walls.Add(new(farCx, nearHiY, farCx, cy + DoorwayHalf, true));
        walls.Add(new(farCx, nearLoY, farCx, cy - DoorwayHalf, true));

        // ── The inner scheme, distinct from henge/wreck/tunnel: a SERVER SPINE + LAB BENCHES + STASIS PODS,
        //    all tucked to the sides so the central door→console lane (around cy) stays clear. ──
        double d3 = cx + (dir * 3.0), d6 = cx + (dir * 6.0), d10 = cx + (dir * 10.0), d13 = cx + (dir * 13.0);
        // The server spine: a long low wall run high of centre (the racks), broken by one maintenance gap.
        double spineY = cy + (half * 0.55);
        walls.Add(new(d3, spineY, d6, spineY, true));
        walls.Add(new(cx + (dir * 8.0), spineY, d13, spineY, true)); // gap at d6..d8 (walk between racks)
        // Lab benches: two short perpendicular stubs off the LOW wall (the work counters).
        double benchY = cy - half;
        walls.Add(new(d3, benchY, d3, benchY + 2.5, false));
        walls.Add(new(cx + (dir * 7.0), benchY, cx + (dir * 7.0), benchY + 2.5, false));
        // Stasis pods: two tiny solid boxes tucked into the far corners (the sleepers).
        AddBox(walls, System.Math.Min(d13, farCx - 1.0), nearHiY - 2.0, farCx - 0.5, nearHiY - 0.5, true);
        AddBox(walls, System.Math.Min(d13, farCx - 1.0), nearLoY + 0.5, farCx - 0.5, nearLoY + 2.0, true);

        // ── The interactables, down the OPEN central lane (around cy), never inside a wall. ──
        double laneY = cy - (half * 0.15);
        var consoles = new List<LabConsole>
        {
            // A log at the threshold — the first fragment, no reveal (it draws you in).
            new(LabConsoleKind.LoreLog, "lab-log-1", d3, laneY, "🖥 VANTAR — FIELD LOG", 0, false),
            // The brain-in-a-jar backup rig, mid-room — reads the DO NOT REVIVE log.
            new(LabConsoleKind.BrainJar, "lab-brainjar", d6, cy + (half * 0.15), "🧠 BACKUP RIG · DO NOT REVIVE", 2, false),
            // The dormant synthetic on its bench — the bounded risk, off to the low side (reads log 3).
            new(LabConsoleKind.DormantSynth, "lab-synth", d10, cy - (half * 0.35), "🦿 DORMANT SYNTHETIC", 3, false),
            // The fat discovery cache at the heart.
            new(LabConsoleKind.DiscoveryCache, "lab-cache", d10, laneY, "🗝 VANTAR'S CACHE", 0, false),
            // The CORE log at the deep end — reading it is the reveal (nerve hit + the diced outcome).
            new(LabConsoleKind.LoreLog, "lab-log-core", d13, laneY, "🖥 VANTAR — THE CORE LOG", VantarLore.CoreIndex, true),
        };

        // ── INTO THE MOUNTAIN. Two more chambers past the first, each a little narrower than the last, each
        //    behind a door. The narrowing is the fiction doing the work: this was cut into rock by people who
        //    kept going after the budget ran out.
        var doors = new List<LabDoor>
        {
            new("lab-door-1", farCx, cy, ChamberNames[1]),
        };

        double c2Near = farCx, c2Far = farCx + (dir * DeepChamberDepth);
        double c2Half = half * 0.8;
        walls.Add(new(c2Near, cy + c2Half, c2Far, cy + c2Half, true));
        walls.Add(new(c2Near, cy - c2Half, c2Far, cy - c2Half, true));
        walls.Add(new(c2Near, cy + c2Half, c2Near, cy + DoorwayHalf, true));
        walls.Add(new(c2Near, cy - c2Half, c2Near, cy - DoorwayHalf, true));
        walls.Add(new(c2Far, cy + c2Half, c2Far, cy + DoorwayHalf, true));
        walls.Add(new(c2Far, cy - c2Half, c2Far, cy - DoorwayHalf, true));
        doors.Add(new("lab-door-2", c2Far, cy, ChamberNames[2]));

        double c3Far = c2Far + (dir * DeepChamberDepth);
        double c3Half = half * 0.62;
        walls.Add(new(c2Far, cy + c3Half, c3Far, cy + c3Half, true));
        walls.Add(new(c2Far, cy - c3Half, c3Far, cy - c3Half, true));
        walls.Add(new(c3Far, cy + c3Half, c3Far, cy - c3Half, true));

        // The clean room carries the two control panels — the owner's own asks, and they belong TOGETHER in the
        // middle: "Surely some control panels based on the vent panel can be added 🤠" and "Alarm system panel
        // maybe … something to try to hack." A captain who reaches the middle can throw every door in the
        // mountain from one wall, and can argue with the thing that is counting.
        double c2Mid = c2Near + (dir * (DeepChamberDepth / 2.0));
        consoles.Add(new(LabConsoleKind.DoorBoard, "lab-doorboard", c2Mid, cy + (c2Half * 0.45),
                         "🎛 DOOR BOARD — VANTAR LABS", 0, false));
        consoles.Add(new(LabConsoleKind.AlarmPanel, "lab-alarm", c2Mid, cy - (c2Half * 0.45),
                         LabSecurity.PanelTitle, 0, false));

        // …and the heart carries the card, which is the only thing that opens a lockdown. It is in the DEEPEST
        // room on purpose: a captain who ran at the first alarm never had it, and now needs it.
        double c3Mid = c2Far + (dir * (DeepChamberDepth / 2.0));
        consoles.Add(new(LabConsoleKind.KeyCard, "lab-keycard", c3Mid, cy,
                         "🗝 VANTAR'S CARD", 0, false));

        double heartX = cx + (dir * (RoomDepth / 2.0)), heartY = cy;
        var marks = new List<SurfaceLayout.Landmark> { new(heartX, heartY, "⧉ VANTAR'S LAB") };

        double minX = System.Math.Min(cx, c3Far), maxX = System.Math.Max(cx, c3Far);
        return new Region("VANTAR'S SECRET LAB", walls, marks, consoles, DiscoveryCacheCredits,
            minX, nearLoY, maxX, nearHiY, heartX, heartY, doors);
    }

    // ── The reveal roll (house law: the die is shown). ──

    /// <summary>Which way the reveal broke.</summary>
    public enum RevealOutcome
    {
        /// <summary>You keep your head and strip the lab for the good stuff — a heroic pay.</summary>
        SalvageTech,

        /// <summary>It salvages YOU — the dormant thing stirs, a bigger nerve hit and a limited pack rouses.</summary>
        ItSalvagesYou,
    }

    /// <summary>A settled reveal: the raw D20 face (shown, house law), which way it broke, the salvage pay
    /// (0 unless <see cref="RevealOutcome.SalvageTech"/>), the nerve hit dealt, and the limited pack size the
    /// bad branch rouses (0 on the good branch).</summary>
    public readonly record struct RevealRoll(int Face, RevealOutcome Outcome, int PayCredits, double NerveHit, int PackSize);

    /// <summary>Roll the reveal for reading the core log (owner: "salvage the tech for pay, or it salvages
    /// you"). A single D20 (≥ <see cref="SalvageMinRoll"/> salvages), so the die reads cleanly on-screen.
    /// Fully deterministic in <paramref name="seed"/> — the client seeds it off the body + sim time.</summary>
    public static RevealRoll RollReveal(ulong seed)
    {
        int d20 = DiceRule.Roll(seed, 20).Face; // 1..20
        if (d20 >= SalvageMinRoll)
        {
            int pay = DiceRule.RollAmount(DiceRule.Seed(seed, "salvage-pay"), SalvagePayMin, SalvagePayMax).Face;
            return new RevealRoll(d20, RevealOutcome.SalvageTech, pay, RevealShock, 0);
        }
        int pack = DiceRule.RollAmount(DiceRule.Seed(seed, "wake-pack"), WakePackMin, WakePackMax).Face;
        return new RevealRoll(d20, RevealOutcome.ItSalvagesYou, 0, RevealShock + CostBranchExtraShock, pack);
    }

    // ── Builders + seeded sampling (pure, off the shared dice engine). ──

    private static void AddBox(List<SurfaceLayout.Wall> walls, double x1, double y1, double x2, double y2, bool hull)
    {
        double lox = System.Math.Min(x1, x2), hix = System.Math.Max(x1, x2);
        double loy = System.Math.Min(y1, y2), hiy = System.Math.Max(y1, y2);
        walls.Add(new(lox, loy, hix, loy, hull));
        walls.Add(new(lox, hiy, hix, hiy, hull));
        walls.Add(new(lox, loy, lox, hiy, hull));
        walls.Add(new(hix, loy, hix, hiy, hull));
    }

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"secretlab:{bodyId}:{tag}"), Resolution).Face; // 1..Resolution
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}

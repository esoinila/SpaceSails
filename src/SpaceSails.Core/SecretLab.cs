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
        double RevealX, double RevealY);

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

        // The door's ground spot: a seeded pocket in the DEEP field (a committed walk from the tube), kept
        // clear of the far edges so the appended chamber always has room to grow inside the safe span. The
        // chamber extends toward the field's horizontal centre (the side with the most room).
        double loX = field.LeftX + SurfaceLayout.EdgeMargin + RoomDepth;
        double hiX = field.RightX - SurfaceLayout.EdgeMargin - RoomDepth;
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

        // Extend toward the field's horizontal centre — the side with the most open regolith.
        double midX = (field.LeftX + field.RightX) / 2.0;
        double dir = doorX <= midX ? 1.0 : -1.0;
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
        // The far face: solid.
        walls.Add(new(farCx, nearHiY, farCx, nearLoY, true));

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

        double heartX = cx + (dir * (RoomDepth / 2.0)), heartY = cy;
        var marks = new List<SurfaceLayout.Landmark> { new(heartX, heartY, "⧉ VANTAR'S LAB") };

        double minX = System.Math.Min(cx, farCx), maxX = System.Math.Max(cx, farCx);
        return new Region("VANTAR'S SECRET LAB", walls, marks, consoles, DiscoveryCacheCredits,
            minX, nearLoY, maxX, nearHiY, heartX, heartY);
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

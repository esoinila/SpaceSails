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

    /// <summary>#1119 item 2 · <b>THE PLACEMENT AS THIS SITE ACTUALLY BUILT IT — the one every client reader
    /// should hold.</b>
    ///
    /// <para><see cref="For"/> answers a question about a BODY: the seeded pocket the door was rolled into,
    /// which knows nothing about the shelters, the outpost hut and the monolith standing on that particular
    /// site. <see cref="HeadSpot"/> is that spot after the site has had its say, and it is where the shed is
    /// drawn (<see cref="HeadHut"/>), where the ground is kept clear (<see cref="ChamberFootprint"/>), where
    /// #625 points the tracker's ring and its rumour wash, and where the lift car sets the captain down.</para>
    ///
    /// <para>The hidden-door CONSOLE was the one thing still reading the raw spot — measured up to 235 du
    /// from the hut on 21 of 34 body × site pairs, because the raw spot is seeded per BODY and the clamp
    /// re-seeds per SITE, so when it fires it does not nudge, it RELOCATES. The instrument and the ground
    /// disagreeing is the #573 family, and #584 is the map lying; this was both at once.</para>
    ///
    /// <para>Rather than teach eight call sites to remember a second function, the excursion holds a
    /// placement that is ALREADY the resolved one — door spot and beach-comber square alike — so the console,
    /// the chamber the force appends, the alarm's doors, the plate on the card, the detector's needle and the
    /// reveal square are one fact by construction. <see cref="HasLab"/> is still the honest roll.</para></summary>
    /// <param name="siteSalt">This landing site's layout salt — the reason the answer is per-site.</param>
    public static Placement OnThisSite(
        string bodyId, string? siteSalt, in SurfaceLayout.Field field, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        Placement seeded = For(bodyId, field, forcePresent);
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        (int sqX, int sqY) = BeachComber.SquareOf(hx, hy);
        return new Placement(seeded.HasLab, hx, hy, sqX, sqY);
    }

    /// <summary>Whether the seed alone (no cheat) hides a lab on this body — 1 in <see cref="ExpeditionOneInN"/>
    /// in the deep field of an away-expedition site, 1 in <see cref="OrdinaryOneInN"/> on an ordinary moon.</summary>
    public static bool Present(string bodyId)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int oneInN = ExpeditionSite.TryParseKind(bodyId, out _) ? ExpeditionOneInN : OrdinaryOneInN;
        return DiceRule.Roll(DiceRule.Seed($"secretlab:has:{bodyId}"), oneInN).Face == 1;
    }

    /// <summary>
    /// #1052 (L1) · <b>THE SEAM: what does this place read?</b> Whether the news a captain raises HERE is
    /// the facility's own <see cref="NewsWire.NewsScope.CompanyIntranet"/> rather than the port's rag —
    /// the one thing the wire needs to know about a hidden lab, and the whole of what this file lends it.
    ///
    /// <para>Two facts, and no more. <paramref name="insideLab"/> is the caller's own honest position: a lab
    /// canteen table stands inside a FORCED region, and the wire has no business re-deriving the geometry
    /// the client is already standing in. <see cref="Present"/> is then asked as a cross-check so a body
    /// that hides nothing can never print a company paper, with <paramref name="forcePresent"/> carrying
    /// the <c>?secretlab=1</c> cheat exactly as <see cref="For"/> does — otherwise the cheat's lab would
    /// exist on the ground and not on the noticeboard.</para>
    ///
    /// <para>L2 consumes this through <see cref="NewsWire.ScopeAt"/> (never directly): the seat verb builds a
    /// <see cref="NewsWire.NewsPlace"/> from the seated context it already holds and asks the wire for its
    /// masthead. Nothing else about a place is allowed to leak into Core.</para>
    /// </summary>
    public static bool ReadsCompanyIntranet(string bodyId, bool insideLab, bool forcePresent = false)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return insideLab && (forcePresent || Present(bodyId));
    }

    /// <summary>Whether a probe of (<paramref name="squareX"/>, <paramref name="squareY"/>) is close enough to
    /// the hidden door to shriek a PROXIMITY hint (the detector "very close") — the door's own square, or any
    /// of the eight around it. The exact-square case (a reveal) is <see cref="IsDoorSquare"/>.</summary>
    /// <summary>#585 · The ground the hidden chamber will occupy once it is forced open — centre and a
    /// rotation-proof radius, in the shape every other placer on this ground speaks.
    ///
    /// <para>The lab is APPENDED at runtime, from the door outward toward the field's centre. Nothing that
    /// lays buildings knew that, so once the grounds gained real structures a hut could be standing exactly
    /// where the chamber grows — and the lab would open into somebody else's wall. That is the identical
    /// failure the away-expedition rooms hit, reported by their guard as "a region wall crosses the base
    /// geography"; this is the same fix, applied before the owner goes looking for a lab rather than after
    /// he finds one wedged inside a ruin.</para>
    ///
    /// <para>Reserved on EVERY body, whether or not this one hides a lab: the door spot is seeded the same
    /// way regardless, so keeping that patch of deep field clear costs one building's worth of ground and
    /// removes the whole class.</para></summary>
    public static (double X, double Y, double R) ChamberFootprint(
        string bodyId, in SurfaceLayout.Field field, string? siteSalt = null)
    {
        // #585: reserved around the RESOLVED entrance (HeadSpot), not the raw seed — otherwise the ledger
        // keeps a patch of ground clear that the shed has already been nudged away from, and leaves the
        // ground it actually stands on unprotected. That is how the maintenance shed ended up inside an
        // outpost hut.
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        double midX = (field.LeftX + field.RightX) / 2.0;
        double dir = hx <= midX ? 1.0 : -1.0;

        // Wide enough to cover the hut at the door AND the chamber that grows away from it — and NO wider.
        //
        // #587: this was RoomDepth + RoomWidth/2 (23 du), which was generous to the point of being a bug. On
        // top of nine shelter reservations it rejected so many seeded features that different bodies started
        // producing the same sparse ground: SeededBodies_AllDifferFromEachOther went from 8 distinct wall
        // hashes to 5, and SiteSalt_ParameterizesTheGround found two salts generating an identical field.
        // A keep-out is a claim on ground, and an over-claim quietly costs the whole world its variety.
        //
        // #606 · So when the shed grew into a full-sized hut, the answer was NOT a bigger circle. Two circles
        // are being covered — the hut standing ON the door, and the chamber whose own bounding circle sits
        // half its depth out from it — and the smallest disc round both is the one whose diameter is their
        // span. Recentring it buys almost all of the extra reach for almost none of the extra ground: the hut
        // roughly doubled and the claim went up by about a du.
        double hut = SurfaceStructure.EnvelopeOf(HeadHutAt(bodyId, siteSalt, 0, 0)).Reach;
        double chamber = Math.Sqrt(((RoomDepth / 2.0) * (RoomDepth / 2.0)) + ((RoomWidth / 2.0) * (RoomWidth / 2.0)));
        double lo = -hut, hi = (RoomDepth / 2.0) + chamber;
        return (hx + (dir * ((lo + hi) / 2.0)), hy, (hi - lo) / 2.0);
    }

    /// <summary>#606 · THE LIFT HEAD IS AN ORDINARY HUT. Owner, twice, while playing:
    /// <i>"I think the lift could also be a little more hidden on the surface, since up there there are no
    /// guards... it could be in an ordinary hut, with 2 doors .. we have those. The expensive doors would be
    /// the clue"</i> — and then, after another look at the ground, <i>"the elevator still stands out on
    /// surface like a sore thumb"</i>.
    ///
    /// <para>The second sentence is the one that matters, because the first fix was colour and colour was
    /// never the problem. The head was a 10 x 8 box of five thin lines while every other building on the moon
    /// is <see cref="SurfaceStructure"/>'s piled regolith — hatched mass, real thickness, a seeded angle. It
    /// was not a camouflaged lift head, it was the only building on the ground drawn in a different hand. A
    /// captain does not have to know what a lift head looks like to pick that out; they only have to be able
    /// to see.</para>
    ///
    /// <para>So it is built by the same function as its neighbours, at a size drawn from the same range, and
    /// what is left to notice is what the owner asked to be the clue: the DOORS were flown here. Every hatch
    /// on a landing site is swaged out of the hill it is set in; two machined pressure doors on a survey shack
    /// are a receipt, and a receipt is the only thing this facility has ever been careless with (#601).</para>
    ///
    /// <para><b>Rectangular, always.</b> The one property that is not seeded, and it earns the exception: a
    /// lift car is a box, and a rotated box is the shape everything downstream — the car's return spot, the
    /// keep-out, the audit — can answer <i>"is the captain inside this"</i> about without inventing a second
    /// geometry to be wrong in. A third of the huts on any site are rectangles, so it hides in plain sight.</para></summary>
    public static SurfaceStructure.Spec HeadHut(string bodyId, string? siteSalt, in SurfaceLayout.Field field)
    {
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        return HeadHutAt(bodyId, siteSalt, hx, hy);
    }

    /// <summary>The hut's SHAPE, which is pure of where it ends up standing — so <see cref="HeadSpot"/> may
    /// ask how much room it needs without asking itself where it is.</summary>
    private static SurfaceStructure.Spec HeadHutAt(string bodyId, string? siteSalt, double x, double y)
    {
        string salt = siteSalt ?? "";
        return SurfaceStructure.Ordinary(
            x, y,
            size: 10.0 + (4.0 * Frac(bodyId, $"head-size:{salt}")),
            thickFrac: Frac(bodyId, $"head-thick:{salt}"),
            angleFrac: Frac(bodyId, $"head-angle:{salt}"),
            // Two, because he asked for two and because a facility that put a car in a shack would want a way
            // out of it that is not the way in.
            doors: 2,
            shapeFace: (int)SurfaceStructure.Footprint.Rectangular);
    }

    /// <summary>#585 · WHERE THE LIFT HEAD ACTUALLY STANDS, once everything else on this site has had its say.
    ///
    /// <para>Owner, looking at a screen with the maintenance shed buried inside an outpost hut which was
    /// itself overlapping a shelter drum: <i>"is it this that I cannot get into?"</i> He could not, and it was
    /// not his fault.</para>
    ///
    /// <para>The cause is the oldest one in this file's neighbourhood, with a new twist: <b>the lab is seeded
    /// PER BODY and everything it collides with is seeded PER SITE.</b> <see cref="For"/> cannot see the
    /// shelters or the hut, they cannot see it, and the shared claim ledger only ever protected the CHAMBER
    /// (which is offset from the door) and never the shed standing on the door itself. Three placers, three
    /// answers, one patch of ground.</para>
    ///
    /// <para>So the entrance is resolved HERE, against this site's real furniture, and everything downstream —
    /// the shed, the tracker beacon, the hidden-door console, the chamber's own reservation — reads this one
    /// function. Seeded nudges, so it is still the same spot every visit.</para></summary>
    public static (double X, double Y) HeadSpot(string bodyId, string? siteSalt, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string salt = siteSalt ?? "";

        Placement seeded = For(bodyId, field, forcePresent: true);
        double x = seeded.DoorX, y = seeded.DoorY;

        // Everything on this site that a shed must not be inside. The hut is built into an edge lane, so it
        // is the likeliest collision by a distance.
        //
        // #606 · The clearance is the head's OWN reach plus a berth, not a flat 12 du. That constant was
        // written when the head was a 10 x 8 box whose half-diagonal was 6.4, so it happened to hold; the
        // moment the head became a full-sized hut it would have been a number that no longer described
        // anything, quietly letting a pressure drum and a lift share a wall. Two footprints do not overlap
        // when the gap between their centres beats the sum of their reaches — that sentence, and no constant.
        double reach = SurfaceStructure.EnvelopeOf(HeadHutAt(bodyId, salt, 0, 0)).Reach;
        var taken = new List<(double X, double Y, double R)>();
        foreach (SurfaceStructure.Spec shelter in SurfaceShelter.SpecsFor(bodyId, salt, field))
        {
            taken.Add((shelter.CentreX, shelter.CentreY,
                SurfaceStructure.KeepOutRadius(shelter) + reach + HeadBerth));
        }

        // #563 · AND NOT THE HUT ANY MORE — the precedence between these two is REVERSED, deliberately.
        //
        // The hut used to be pinned to the far edge lane, which no ordinary generator ever touched, so it was
        // the fixed thing and the lift head moved around it. An unbounded ground has no edge lane, so the hut
        // now places itself against this site's real furniture the same way this does — and the two asking
        // each other "where are you?" is a cycle that recurses until the stack gives out (it did, in one run
        // of the suite). Somebody has to go first.
        //
        // The lab goes first, and it should: a lift head is the mouth of a whole facility and cannot be
        // anywhere else, while a hut is one shed and the owner's own rule for this ledger is that the thing
        // which costs nothing to move is the thing that moves. SurfaceOutpost.ForTile reads
        // SurfaceLayout.StandingClaims, which carries the chamber this head reserves, so the invariant that
        // used to be enforced from here is enforced from there — same law, one direction.

        // #649 · And the monolith, on the one ground that carries one. The lift head is seeded down the deep
        // field and the deep field is where the stone is; without this the camouflaged shed could be seeded
        // inside 54 du of solid rock, which is a captain riding a lift up into somewhere they cannot stand —
        // the #602 report, wearing a landmark. Asked of the object, so its size and this clearance cannot
        // drift apart.
        if (Monolith.KeepOutOn(bodyId, salt, field) is { } slab)
        {
            taken.Add((slab.X, slab.Y, slab.R + reach));
        }

        // A handful of seeded retries along the deep field, then give up and take the last one rather than
        // loop: a shed slightly close to a hut is a cosmetic problem, and no shed at all is a dead feature.
        for (int attempt = 0; attempt < 24 && Clashes(x, y, taken); attempt++)
        {
            double loX = field.LeftX + SurfaceLayout.EdgeMargin + RoomDepth;
            double hiX = field.RightX - SurfaceLayout.EdgeMargin - RoomDepth;
            double loY = field.BottomY + (RoomWidth / 2.0) + 2.0;
            double hiY = field.AnchorY + 12.0;

            x = Lerp(loX, hiX, Frac(bodyId, $"head-x:{salt}:{attempt}"));
            y = Lerp(loY, hiY, Frac(bodyId, $"head-y:{salt}:{attempt}"));
        }

        return (x, y);
    }

    /// <summary>The bare air the head wants between its own wall and a neighbour's, on top of both
    /// footprints. Enough that the two never read as one complex; small, because every du of it is ground
    /// claimed away from the ordinary buildings (#587).</summary>
    private const double HeadBerth = 4.0;

    private static bool Clashes(double x, double y, List<(double X, double Y, double R)> taken)
    {
        foreach ((double tx, double ty, double r) in taken)
        {
            double dx = x - tx, dy = y - ty;
            if (Math.Sqrt((dx * dx) + (dy * dy)) < r)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#585 · THE DETECTOR GETS WARMER. Owner: <i>"the detector should also give detecting readings
    /// near it."</i>
    ///
    /// <para>The probe was all-or-nothing: stand on the exact beach-comber square and it PINGS, stand on one
    /// of the eight around it and it shrieks, stand anywhere else on a 310 x 260 field and it says nothing at
    /// all. That is not a search, it is a lottery with 4 000 tickets — which is why the owner could only find
    /// a lab by already knowing it was there.</para>
    ///
    /// <para>A real detector is a gradient. This is the hot-and-cold every treasure hunt has run on since
    /// they were invented, and it turns a field into something you can WORK: pick a bearing, walk it, watch
    /// the reading, turn when it cools. The exact square still pings — that is still the moment — but now
    /// there is a way to steer toward it.</para>
    ///
    /// <para>It only wakes on a moon you have a reason to search (see <see cref="MoonWorthLookingAt"/>),
    /// because a detector that hums on every world would hand the player every lab in the system for free and
    /// make the clue chain pointless.</para></summary>
    public enum Reading { Silent, Faint, Steady, Strong, Screaming }

    /// <summary>How far out the detector says anything at all.</summary>
    public const double DetectorRange = 62.0;

    /// <summary>What the needle is doing at this distance from the hidden door.</summary>
    public static Reading ReadingAt(double distance) => distance switch
    {
        <= 7.0 => Reading.Screaming,
        <= 18.0 => Reading.Strong,
        <= 34.0 => Reading.Steady,
        <= DetectorRange => Reading.Faint,
        _ => Reading.Silent,
    };

    /// <summary>What the captain hears. Written so the DIRECTION of change is the information — "it climbs",
    /// "it falls away" — because a number would be a map and this is meant to be a search.</summary>
    public static string ReadingLine(Reading reading, bool warmer) => reading switch
    {
        Reading.Faint => warmer
            ? "📻 The detector finds something to say — one slow tick, then another. Not nothing. Not yet anything."
            : "📻 The ticking thins out and stops. Whatever it was, it is behind you now.",
        Reading.Steady => warmer
            ? "📻 A steady return under your boots — metal, buried, and far too regular to be ore. It gets no quieter as you walk."
            : "📻 The return drops back to a tick. You have walked past the shoulder of it.",
        Reading.Strong => warmer
            ? "📻 The needle stops pretending. Something big and made is under this ground, and it is close."
            : "📻 The needle eases off. Close, still — but not as close as you were.",
        Reading.Screaming => warmer
            ? "📻 The detector screams and will not stop. You are standing on it. Sweep the squares here and probe."
            : "📻 It screams on, quieter by a hair. Do not wander — it is within a few paces of your boots.",
        _ => "",
    };

    /// <summary>#585 · WHICH MOON A CLUE NAMES. Owner, having found a lab only because he knew it was there:
    /// <i>"We will be needing some kind of clue in the plot arc to the radar to really find it in reasonable
    /// time in the game :-D ... now we kind of found it by just knowing it is here somewhere :-D"</i>
    ///
    /// <para>He is right, and the loop was half-built: the tracker already draws a wide vague wash for a lab
    /// (a tip narrows a search, it never ends one) — but that wash was gated on having ALREADY found the
    /// place, so it helped on a return visit and did nothing on the first. The clue had no way in.</para>
    ///
    /// <para>This closes it. A clue found anywhere in the gumshoe chain — a file in a facility, a docket in a
    /// ruin, something a dead specialist's family knows — names a MOON THAT ACTUALLY HAS ONE. Never a moon
    /// that does not: a game that sends you three days out on a false lead is not being mysterious, it is
    /// wasting your evening.</para></summary>
    public static string? MoonWorthLookingAt(IReadOnlyList<string> candidates, ulong seed)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return null;
        }

        var withLabs = new List<string>();
        foreach (string body in candidates)
        {
            if (Present(body))
            {
                withLabs.Add(body);
            }
        }
        if (withLabs.Count == 0)
        {
            return null;
        }
        return withLabs[(int)(seed % (ulong)withLabs.Count)];
    }

    /// <summary>How a clue reads when it finally names somewhere. It gives a MOON, never a spot — the walk is
    /// still yours, and the tracker will only ever wash the general area.</summary>
    public static string LeadLine(string moonName) =>
        $"🔎 A place name, in among the rest of it, in a context that makes no sense unless somebody was " +
        $"running something there: {moonName}. Written the way people write a thing they are not supposed " +
        "to have written down. Your tracker will know roughly where to wash when you are standing on it.";

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

        // #822 · …and the heart's back wall is TWO STUBS AND A GAP now, with the gap standing plugged. See
        // HiddenWay: the deepest room in the mountain had one door and a lockdown board that could throw it,
        // which is the only genuinely sealed box in the game. The crawl is cut to the same DoorwayHalf every
        // other opening in here is cut to, so a captain never has to judge one by eye.
        walls.Add(new(c3Far, cy + c3Half, c3Far, cy + DoorwayHalf, true));
        walls.Add(new(c3Far, cy - c3Half, c3Far, cy - DoorwayHalf, true));
        var hidden = new List<HiddenWay>
        {
            new("lab-crawl", ChamberNames[2], c3Far, cy,
                new SurfaceLayout.Wall(c3Far, cy - DoorwayHalf, c3Far, cy + DoorwayHalf, true),
                "The rock at the back of the heart rings HOLLOW - and it is dressed rock, not a face. "
                + "Somebody cut a way out of here and then made it look like the end of the world."),
        };

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
            minX, nearLoY, maxX, nearHiY, heartX, heartY, doors, hidden);
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

    // ── The two plates (#528's card, this lane's turnings) ────────────────────────────────────────────

    /// <summary>
    /// A SEALED DOOR WHERE NO DOOR HAS ANY RIGHT TO BE — the moment the ground stops being ground.
    ///
    /// <para>It is the only find in the beach-comber lane that is not a thing you pick up, and it was a
    /// pulse line. What the captain has actually done here is discover that somebody, generations ago, went
    /// to the trouble of putting a blast door under a moon and then covering it over; the decision that
    /// follows ("force it, or walk away and pretend you never found it") is one of the sharpest in the
    /// game, and it was being offered over a sentence that fades in a second and a half.</para>
    ///
    /// <para>The plate shows the door and NOTHING about what is behind it. There is no marking on it,
    /// which is the point: an unmarked door is what you get when the marking would have been the crime.
    /// </para>
    /// </summary>
    public static readonly RevealPlate DoorPlate = new(
        "A SEALED DOOR, BURIED FLUSH WITH THE REGOLITH",
        "art/lab-door-regolith.jpg",
        "Machined steel under a hand's depth of undisturbed dust, a ring of locking dogs the size of your "
        + "forearm, and not one mark, plate or stencil anywhere on it. It was not lost out here. It was put "
        + "here, and then it was covered over.");

    /// <summary>
    /// THEY ARE STANDING OFF THEIR BENCHES — the Hive's loudest moment, and it had no frame at all.
    ///
    /// <para>Raised only on <see cref="RevealOutcome.ItSalvagesYou"/>: the other branch already ends in a
    /// selfie against this room, and a card on both would be a card on a card. Because it fires strictly
    /// after the D20 has resolved and been shown, it can never be a tell — the captain already knows which
    /// way it went before the picture arrives.</para>
    ///
    /// <para>The caption obeys the ground's standing law (<c>TheHiveTests.NothingDownHereEXPLAINSAnything</c>):
    /// what you find is benches, restraints and a count. It never says what they are, and it never will.
    /// </para>
    /// </summary>
    public static readonly RevealPlate TheyStandPlate = new(
        "THEY ARE STANDING OFF THEIR BENCHES",
        "art/lab-they-stand.jpg",
        "Two rows of low steel benches with restraint cradles bolted to them, most still occupied and still "
        + "frosted over. The cradles nearest the door are open, their straps hanging, and the things that "
        + "were lying in them are on the floor with their backs to you. Nobody down here ever wrote down "
        + "what these were for.");

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

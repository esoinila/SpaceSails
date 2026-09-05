namespace SpaceSails.Core;

// Subject: the masked hull — which fat merchants are not merchants, what the instruments measure of them
// against what their papers claim, and the one thing they do differently when a captain closes.

/// <summary>
/// #534 slice 1 · <b>SOME FAT MERCHANTS ARE NOT MERCHANTS, AND EVERY WAY OF KNOWING IS A NUMBER.</b>
///
/// <para>Owner: <i>"Some ships are posing as Rocinante but under came are masked war ships etc. 😎"</i> —
/// filed as #534, which extends #533's breadcrumb idiom from archaeology to the living. The design there is
/// a single sentence and it governs every line below: <b>this is a READ, not a dice roll on boarding.</b>
/// The difference is decidable before the captain commits, out of instruments the ship already carries, and
/// <b>the game never states it.</b> No plate, no colour, no icon, no sentence anywhere says what she is. She
/// shows the same fields every hauler shows; the numbers in them are a warship's.</para>
///
/// <para><b>Why this class holds no strings at all.</b> The whole mechanic is the arithmetic not closing —
/// the captain reads two instruments and finds they disagree, and nobody aboard is left to ask. A verdict
/// written anywhere in the pipe would delete the mechanic, so the rule may only ever return NUMBERS, and
/// <c>TheMaskedHullIsReadBeforeItIsMetTests</c> sweeps this type for a string member and reddens on the
/// first one. The hail exchange — the one tell that is genuinely prose — is slice 2 and is deliberately
/// absent here.</para>
///
/// <para><b>Nothing below is a new warship.</b> The masked hull's real drive is
/// <see cref="EncounterRule.HunterAccelMps2"/>, the same fixed-thrust number the heat-hunters chase on
/// (owner's standing ruling: the collector is thrust-only by design). Her papers claim
/// <see cref="NpcShip.ManeuverBudget"/>, the acceleration the schedule already says a hull of her declared
/// tonnage can produce. Every tell is one of those two numbers, or a count derived from one of them, so
/// there is no third set of figures anywhere that a card and a sim could disagree about.</para>
///
/// <para><b>The tells, and where each is read.</b></para>
/// <list type="bullet">
///   <item><b>Burn profile against claimed load</b> — <see cref="MeasuredTrimAccelMps2"/> beside
///   <see cref="ClaimedTrimAccelMps2"/>: what she is measured trimming an orbit at, against what her
///   declared tonnage allows. A hauler at her manifest weight cannot trim like a corvette.</item>
///   <item><b>Radiator area</b> — <see cref="MeasuredRadiatorPanels"/> beside
///   <see cref="ClaimedRadiatorPanels"/>: waste heat cannot be hidden, and a drive that can push her that
///   hard has to dump it somewhere. Counted in panels rather than square metres because a count is what a
///   telescope actually resolves off a hull.</item>
///   <item><b>Comms fit</b> — <see cref="MeasuredGuardedChannels"/> beside
///   <see cref="ClaimedGuardedChannels"/>: how many bands she keeps a receiver on. The same crumb #533's
///   wreck version uses, asked of the living.</item>
///   <item><b>She does not run the way prey runs</b> — <see cref="EvadeHeadingRad"/>. Prey jinks abeam of
///   its own course and its bearing swings off the captain's bow. She opens the range down the sightline,
///   which holds the bearing constant: she is backing off while keeping you in front of her.</item>
/// </list>
///
/// <para><b>Each is individually deniable</b> (riding light this run; over-built cooling, common on old
/// hulls; surplus radio; a cautious captain). The set is the answer, and the set is never assembled for
/// anybody — see #533's own discipline note: an anomaly that becomes a dropdown option stops being one.</para>
/// </summary>
public static class QShip
{
    // ── WHO CAN BE ONE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A hold this full is a hull big enough to hide a warship inside. Below it the silhouette is simply
    /// too small for the deception to be worth anybody's money — and, mechanically, it keeps the read
    /// pointed where the issue points it: at the FAT merchants, the ones a pirate actually wants.
    /// <para>The schedule draws <see cref="NpcShip.CargoUnits"/> from [5, 21), so this sits above the
    /// midpoint: a minority of honest traffic is fat, and a minority of THAT is masked.</para>
    /// </summary>
    public const int FatHoldUnits = 15;

    /// <summary>
    /// #534's rarity law, and it is the same law #533 states: <i>"if one in three haulers is a wolf, the read
    /// is worthless and everyone shoots first."</i> One fat merchant in this many is masked — rare enough
    /// that the tells are worth learning, common enough that a careless captain eventually meets one.
    /// </summary>
    public const int MaskedInEveryFatMerchants = 12;

    /// <summary>Salt for the per-hull draw, so the masking question never shares a stream with
    /// <see cref="EncounterRule.ComplianceOf"/>'s — two independent facts about the same hull must not be
    /// two reads of one coin.</summary>
    private const ulong MaskSalt = 0xA3D1F07B5C2E6941UL;

    /// <summary>A hull fat enough to be worth wearing. Pods have no papers and no hold to speak of.</summary>
    public static bool IsFatMerchant(NpcShip ship) =>
        !ship.IsPod && ship.CargoUnits >= FatHoldUnits;

    /// <summary>
    /// Is this hull a warship under a merchant's transponder? Deterministic per ship id — hashed rather than
    /// drawn from any live stream, exactly as <see cref="EncounterRule.ComplianceOf"/> is, so asking twice
    /// (or asking on a client and a server) always agrees and nothing about the answer depends on when it
    /// was asked. <b>Nothing in the game ever renders this bool.</b> It is what the numbers are derived FROM.
    /// </summary>
    public static bool IsMasked(NpcShip ship) =>
        IsFatMerchant(ship)
        && new DeterministicRandom(HashSeed(ship.Id) ^ MaskSalt).NextInt(0, MaskedInEveryFatMerchants) == 0;

    // ── TELL (a) · THE BURN PROFILE AGAINST THE CLAIMED LOAD ──────────────────────────────────────────

    /// <summary>What her papers allow: the acceleration the schedule already carries for a hull of her
    /// declared tonnage (<see cref="NpcShip.ManeuverBudget"/> — "equivalent acceleration a pilot could
    /// plausibly hide", which is the same quantity read the other way round).</summary>
    public static double ClaimedTrimAccelMps2(NpcShip ship) => ship.ManeuverBudget;

    /// <summary>What she is measured at on an observed trim. For an honest hauler this is her claim, to the
    /// bit — which is the anti-vacuous half of the whole feature: the readout is not a Q-ship detector with
    /// two settings, it is one measurement that happens to agree with the paperwork on almost every hull.
    /// For a masked one it is <see cref="EncounterRule.HunterAccelMps2"/>, the hunter class's own thrust.</summary>
    public static double MeasuredTrimAccelMps2(NpcShip ship) =>
        IsMasked(ship) ? EncounterRule.HunterAccelMps2 : ClaimedTrimAccelMps2(ship);

    // ── TELL (b) · THE RADIATORS ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// How much drive one radiator panel is rated to cool. Waste heat scales with the thrust the drive
    /// makes, and panels come in panels — so a hull's cooling fit is a COUNT, and the count is the thing a
    /// telescope can actually resolve against a hull. The quantisation is also what keeps the tell deniable:
    /// a count rounds up, so an honestly over-cooled hauler and a lightly-worked warship can read the same.
    /// </summary>
    public const double Mps2CooledByOnePanel = 0.1;

    /// <summary>Panels a drive of this thrust has to carry: rounded UP, because you cannot fit most of a
    /// panel and a hull that cannot dump its heat does not fly. A drive that makes no thrust needs no
    /// radiator, so the floor is zero rather than one.
    /// <para>An earlier cut carried a <c>- 1e-9</c> floating-point guard band against a division landing a
    /// hair over its own integer. No such division exists here — nothing in [0.001, 2.000] over a tenth
    /// overshoots — so the band could not be proven red and came out rather than ship as a claim nothing
    /// owns. If <see cref="Mps2CooledByOnePanel"/> ever moves, ask that question again before assuming.</para></summary>
    public static int PanelsFor(double trimAccelMps2) =>
        (int)Math.Ceiling(Math.Max(0, trimAccelMps2) / Mps2CooledByOnePanel);

    /// <summary>The cooling her claimed drive needs.</summary>
    public static int ClaimedRadiatorPanels(NpcShip ship) => PanelsFor(ClaimedTrimAccelMps2(ship));

    /// <summary>The cooling the telescope counts on her hull.</summary>
    public static int MeasuredRadiatorPanels(NpcShip ship) => PanelsFor(MeasuredTrimAccelMps2(ship));

    // ── TELL (c) · THE COMMS FIT ──────────────────────────────────────────────────────────────────────

    /// <summary>Bands a hauler keeps a receiver on: the port she is going to, and the distress band
    /// everybody guards. Two is a merchant's whole radio life.</summary>
    public const int MerchantGuardedChannels = 2;

    /// <summary>Bands a warship keeps a receiver on. Channels a merchant has no business monitoring — the
    /// same crumb #533's wreck version reads off a dead hull, read here off a live one.</summary>
    public const int WarshipGuardedChannels = 6;

    /// <summary>What a hull with her papers ought to be guarding.</summary>
    public static int ClaimedGuardedChannels(NpcShip ship) => MerchantGuardedChannels;

    /// <summary>What the receiver actually hears her guarding.</summary>
    public static int MeasuredGuardedChannels(NpcShip ship) =>
        IsMasked(ship) ? WarshipGuardedChannels : ClaimedGuardedChannels(ship);

    // ── TELL (d) · SHE DOES NOT RUN THE WAY PREY RUNS ─────────────────────────────────────────────────

    /// <summary>
    /// The world-space heading (0° = +X, counter-clockwise, the convention <see cref="BurnMode.Vector"/>
    /// flies) a hull opens the range on when the captain closes — <b>one law, two branches</b>.
    ///
    /// <para><b>Prey</b> jinks ABEAM of its own course, away from the captain: the idiom the game already
    /// ships in <c>TrafficSchedule.StarterFreighter</c>'s escape jink, whose whole job is to swing her off a
    /// matched velocity and slam the boarding window. Its side effect is the tell: a hull running square
    /// across the sightline sweeps its own bearing off the captain's bow, fast.</para>
    ///
    /// <para><b>She</b> opens the range DOWN the sightline instead. Backing straight off along the line
    /// between the two hulls is the one retreat that leaves the bearing where it was — she is widening the
    /// gap while keeping the captain in front of her, which is what a ship with a gun does and a ship with a
    /// hold does not. The ordinary explanation is on the table as always: a cautious captain.</para>
    ///
    /// <para>Degenerate geometry (the two hulls on top of each other, or a hull with no way on) falls back
    /// to her own heading, so the law never returns a NaN into a maneuver plan.</para>
    /// </summary>
    public static double EvadeHeadingRad(Vector2d hers, Vector2d herVelocity, Vector2d captain, bool masked)
    {
        Vector2d away = hers - captain;
        if (masked)
        {
            return away.LengthSquared > 0
                ? Math.Atan2(away.Y, away.X)
                : Math.Atan2(herVelocity.Y, herVelocity.X);
        }

        if (herVelocity.LengthSquared <= 0)
        {
            return away.LengthSquared > 0 ? Math.Atan2(away.Y, away.X) : 0;
        }

        Vector2d along = herVelocity.Normalized();
        var abeam = new Vector2d(-along.Y, along.X);
        // Both beams are square to her course; take the one that is not toward the captain.
        Vector2d chosen = abeam.Dot(away) >= 0 ? abeam : new Vector2d(along.Y, -along.X);
        return Math.Atan2(chosen.Y, chosen.X);
    }

    /// <summary>
    /// The burn itself, in the shape the sim already flies: <b>one X-Pilot pulse</b> along
    /// <see cref="EvadeHeadingRad"/>, at the same strength and mode the starter freighter's escape jink
    /// uses. A Vector burn rather than a prograde Factor bolt on purpose, for the reason
    /// <c>TrafficSchedule.StarterFreighter</c> already gives: her SPEED stays matchable, so a captain who
    /// reads her right and closes anyway still has a ship to catch rather than a runaway.
    /// </summary>
    public const double EvadePercent = 20;

    /// <summary>Her one break for open water, at <paramref name="atSimTime"/>.</summary>
    public static ManeuverNode EvadeBurn(NpcShip ship, ShipState hers, Vector2d captain, double atSimTime) =>
        new(atSimTime, ManeuverAction.Accelerate, Pulses: 1, Percent: EvadePercent,
            Mode: BurnMode.Vector,
            HeadingDegrees: EvadeHeadingRad(hers.Position, hers.Velocity, captain, IsMasked(ship)) * 180.0 / Math.PI);

    // ── the hash ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>FNV-1a over the hull's id — the same fold <see cref="EncounterRule"/> uses for its own
    /// per-ship facts, so a masked hull is masked in every build on every machine.</summary>
    private static ulong HashSeed(string id)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (char c in id)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash;
    }
}

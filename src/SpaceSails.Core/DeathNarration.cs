namespace SpaceSails.Core;

/// <summary>
/// #380 item 1 · WHAT KILLED THE CAPTAIN — the place-dependent death classification, so the resurrection
/// card can finally explain its own fiction at the moment it matters (owner cruise ruling, 2026-07-19:
/// "I love the fail forward idea.. we should give the dead captain explanation with gen ai images. Eaten
/// by reevers etc place dependent or joined them"). The read-only audit's item 1 gap is "the resurrection
/// fiction arrives too late"; this names the cause up front.
///
/// <para>A single pure enum carried into the rebirth flow. The client maps it to one of the Grok-generated
/// death images and one seeded house-voice line. Two causes are LIVE today — <see cref="DeathCause.Collector"/>
/// (the BUSTED last stand) and <see cref="DeathCause.Impact"/> (periapsis under the surface). The surface
/// causes — <see cref="DeathCause.Reevers"/>, <see cref="DeathCause.Joined"/> — are wired READY (art + tested
/// lines + the <see cref="DeathNarration.SurfaceEnd"/>
/// classifier): a surface Reever catch does NOT kill today (it prices heat + a nerve shock, see
/// Map.Surface ReeverCatch) and nerve overdraw does not kill either, so nothing routes to them yet. When the
/// surface-death lane lands it only has to set the cause — this rule already narrates it. The deep-space
/// <see cref="DeathCause.Void"/> stood in that same list for two years and does not any more: #638 gave it
/// <see cref="VoidRule"/>, and it is the only cause in the set whose lane is a clock.</para>
/// </summary>
public enum DeathCause
{
    /// <summary>A heat-hunter's collector ran you down and the last stand went to the volley — the BUSTED
    /// FreezeFrame. The most common death, and the one the game ships live.</summary>
    Collector,

    /// <summary>You put the ship into a body at speed — periapsis went under the surface (TriggerImpact).
    /// Live today.</summary>
    Impact,

    /// <summary>The Old Ones took you on a surface — a Reever laid hands and this time did not let go. The
    /// chest is still out there. (Wired ready; the surface-death lane sets it.)</summary>
    Reevers,

    /// <summary>
    /// THE OVERLOAD YOU SET YOURSELF ran out with you still aboard. Owner: <i>"that ship also has the
    /// scuttling charges... it is the last defence against the Borg in Star Trek"</i> — and the whole point
    /// of the charges is the THREAT, so this cause only ever fires when the threat did not work and the
    /// captain stayed aboard anyway.
    ///
    /// <para>#525 · It was filed as <i>built</i> and was not: <c>AdvanceScuttleClock</c> called
    /// <c>TriggerSurfaceOverdrawDeath</c> with no known cause, so the card rolled
    /// <see cref="DeathNarration.SurfaceEnd"/> and told the captain an Old One's hand was the last straw —
    /// aboard a drive-failure hull with nothing living in her, ninety seconds after they turned two keys
    /// themselves. The same failure this project has now paid for four times (#545's card blaming Reevers
    /// for three men with rifles), on the one death the captain unambiguously chose.</para>
    ///
    /// <para><b>#633 · TWO PLACES, ONE CAUSE.</b> The branches built the two halves of #525 apart and each
    /// named the cause <c>Scuttled</c>: <c>our-own-ship-has-compartments</c> built the derelict's panel
    /// (bolted to somebody else's reactor) and wrote here that her own ship <i>"has no such switch"</i>;
    /// <c>main</c> built exactly that switch (<see cref="ShipScuttle"/>, <c>Map.ShipScuttleBoard</c>, the
    /// crew's second key). Both are true now, so this is ONE cause legal in exactly TWO places — see
    /// <see cref="DeathNarration.CanHappen"/> — and every word of the card is chosen by
    /// <see cref="DeathPlace"/>: the fireball is hers, the quiet inward collapse is the wreck's. The tail
    /// aboard a wreck is already right — <i>"Her log will not mention it."</i></para>
    /// </summary>
    Scuttled,

    /// <summary>The eerie variant of a surface death: nerves shot to a sliver, and the last anyone saw, the
    /// captain walked TOWARD the crowd, not away — "joined them" (owner's cruise ruling). Chosen sparingly by
    /// <see cref="DeathNarration.SurfaceEnd"/> so it stays chilling. (Wired ready.)</summary>
    Joined,

    /// <summary>Lost to the void — no body to name, no ground to hit.
    ///
    /// <para>#638 · <b>It has a lane now, and it took two years.</b> The line above used to end "wired ready
    /// for whatever void death lands; none routes here today" — a cause with a painting, three lines of prose,
    /// a headline and a <see cref="DeathNarration.CanHappen"/> law that nothing in the client had ever set.
    /// The ruling picked the one candidate that was a trigger on state the sim already tracks rather than a
    /// new scene: reaction mass at zero, no plan step left that can fire, and a plotted course that touches no
    /// haven's capture — twenty consecutive sim-days of it. See <see cref="VoidRule"/>. Of the three
    /// candidates on the issue, EVA was a scene the game does not have and a slipped orbit is
    /// <see cref="Impact"/>'s ground under another name.</para></summary>
    Void,

    /// <summary>#564 · The tank ran out. Not a creature, not a fall, not nerve — the captain walked further
    /// than their air and knew it, because the suit said so out loud when they crossed the line.
    ///
    /// <para>It exists as its own cause for one reason: a suffocation narrated as "an Old One's hand is the
    /// last straw" would be the sim doing one thing while a SENTENCE reports another — the failure this
    /// project has paid for repeatedly (#545's death card blaming Reevers for three men with rifles). The
    /// caller PASSES this rather than rolling for it, because it is the one thing it knows for certain.</para></summary>
    Suffocated,

    /// <summary>
    /// #538 · A PROFESSIONAL SHOT YOU. The black-ops sweep team's challenge ran out with the captain still in
    /// the lamp — which is nothing like being run down by the pack, and used to narrate as if it were.
    ///
    /// <para>Added because the first playtest of the sweep scene ended with the card saying <i>"the Old Ones
    /// took you… they ran you down on Quiet Sister's regolith short of the tube"</i> after three men with
    /// rifles shot a captain standing in a corridor. The sim did one thing and the card reported another,
    /// which in this codebase is the bug, not a wording nit.</para>
    /// </summary>
    Inspected,
}

/// <summary>
/// #574 · WHERE a captain died, which turns out to matter as much as what killed them.
///
/// <para>Owner: <i>"let's make sure that landed death reasons and on ship death reasons are dealt correctly
/// also. Like they are two separate categories. Maybe even 3 ... salvage ship, own ship, and landing
/// party."</i> He was right, and there was a live bug under it: <c>TriggerSurfaceOverdrawDeath</c> serves
/// BOTH a regolith death and a death aboard a derelict (the scuttle, the fifth blow, the nerve running out),
/// while the ground prose says things like <i>"they ran you down on {body}'s REGOLITH short of the tube"</i>.
/// Die on a steel hull in space and the game described the dust you were not standing in.</para>
///
/// <para>The same class of failure as #572's collector card, one level up: the words were not wrong about
/// the CAUSE, they were wrong about the PLACE.</para>
/// </summary>
public enum DeathPlace
{
    /// <summary>Aboard the captain's own ship, or in open space with her under them.</summary>
    OwnShip,

    /// <summary>Inside somebody else's hull — a salvage run on a derelict. Steel, vacuum, no sky and no
    /// dust; the way out is a lock, not a tube up to a shuttle.</summary>
    Derelict,

    /// <summary>An away team on a surface. Regolith, a suit, and a long walk back to the tube.</summary>
    LandingParty,

    /// <summary>#609 · Inside a clandestine facility, under a moon. Owner, having suffocated on B2 and been
    /// handed the surface card: <i>"now we have the suffocated on surface one :-D"</i>.
    ///
    /// <para>He was 150 m down in a poured corridor and the card said regolith, a suit and a long walk back
    /// to the tube — the sim knowing one thing and the SENTENCE reporting another, which is a named bug class
    /// on this ground and has now cost three cards. There was no value here meaning "underground", so every
    /// death in the Hive inherited the away team's, and every word of it was wrong: no ground to keep you,
    /// no sky, and a way out that somebody has to call a car for.</para></summary>
    Underground,

    /// <summary>#653 · Aboard a dead station. Steel and no air like a derelict, and read as one everywhere
    /// (the tail, the art, which deaths can happen) except the suffocation words: a station is not a ship, and the
    /// canon gives her a pool of her own.</summary>
    Station,
}

/// <summary>
/// The pure narration seam for <see cref="DeathCause"/>: the art file each cause shows, the seeded
/// house-voice line pool that explains the death place-dependently, the WHAT-HAPPENED headline, and the
/// "joined them" trigger rule. All deterministic — a test pins an exact line for an exact seed — because
/// determinism is law in Core.
/// </summary>
public static partial class DeathNarration
{
    // ── The "joined them" trigger (owner cruise ruling, 2026-07-19) ──────────────────────────────────
    //
    // On a surface Reever death, the narration is USUALLY that the Old Ones took you (Reevers). But when the
    // captain's nerve was shot to a sliver at the very end, a seeded MINORITY of those deaths tell the eerie
    // story instead — the captain walked into the crowd, not away from it. Rare-ish and gated on a shattered
    // nerve so it stays chilling, exactly as the ruling asks ("or joined them" — used sparingly).

    /// <summary>Nerve at/under this sliver of the 0..100 gauge makes a death ELIGIBLE for the "joined them"
    /// variant — you have to be shattered to walk the wrong way. FLAGGED for the owner's tuning.</summary>
    public const double JoinedNerveSliver = 8.0;

    /// <summary>Of the sliver-nerve surface deaths, roughly one in this many "joins them" — seeded, so it is
    /// reproducible, and rare-ish so the variant stays rare. FLAGGED for the owner's tuning.</summary>
    public const int JoinedChanceInN = 3;

    // ── The house-voice line pools (place-dependent) ─────────────────────────────────────────────────
    //
    // One to two sentences, in the house voice, that narrate the death WHERE it happened. {body} is filled by
    // the caller — the moon the Old Ones took you on, the body you flew into. A null/empty body reads with a
    // generic place ("out there" / "open space") so the line is always whole. Seeded so the variant is
    // reproducible; small pools kept deliberately tight so every line stays quotable.

    private static readonly string[] CollectorLines =
    [
        "The collectors' boarding volley caught you {where} — they don't come to collect twice.",
        "One massive volley {where}, and the last stand was over before the echo. The debt collected itself.",
        "They ran you down {where} and settled the account in lead. The purse was never the point.",
    ];

    // #583 · The same people, on foot, on a moon. The ship lines above are all about a boarding volley and a
    // last stand at the controls, and reading those over a captain taken walking across regolith would be
    // the same borrowed prose #574 was filed about. No volley out here: a writ, a hand, and a long walk to
    // somebody else's boat.
    private static readonly string[] CollectorLinesOnFoot =
    [
        "They walked you down on {body} — no burn to make, no gun to reach, just a gloved hand on the carry loop and the writ read out over your own suit channel.",
        "The repo crew took you on foot on {body}. You were carrying more air than argument, and they had all day.",
        "It ended on {body}'s ground, at walking pace. They never even ran — they did not have to; the tank did their work.",
    ];

    private static readonly string[] ImpactLines =
    [
        "You put the ship into {body} at speed — the periapsis said 'under the surface', and the surface won.",
        "The hull met {body} at speed. No corridor, no atmosphere to catch you — just rock, and then nothing.",
        "You flew {body}'s periapsis under its own surface. The ground was where the orbit said it would be.",
    ];

    /// <summary>Her own charges, with the captain still standing in her. Every line has to carry the same
    /// fact: this was a decision, taken twice, by two hands — and then not walked away from.</summary>
    private static readonly string[] ScuttledLines =
    [
        "You turned the keys {where} and then did not leave. The charges kept their end of the bargain.",
        "Both keys, ninety seconds, and a way out you had already dogged shut {where}. She went exactly as advertised.",
        "The overload ran to zero {where} with the captain still aboard. Whatever you were threatening, you meant it.",
    ];

    private static readonly string[] ReeverLines =
    [
        "The Old Ones took you on {body} — the chest is still out there, for anyone fool enough to go back for it.",
        "A Reever laid hands on you on {body} and this time did not let go. They wanted no loot — only you.",
        "They ran you down on {body}'s regolith short of the tube. The helmet's out there yet; you are not.",
    ];

    private static readonly string[] JoinedLines =
    [
        "They found no body on {body}. The last anyone saw, you walked TOWARD the crowd, not away from it.",
        "On {body} your nerve went to nothing, and then so did you. No struggle in the regolith — just footprints, leading in.",
        "The tracker on {body} still shows you moving, some nights. You didn't run from the Old Ones at the end. You joined them.",
    ];

    /// <summary>#538 · Shot by somebody who was working. Every line has to carry the thing that makes this
    /// death different from all the others: it was ADMINISTRATIVE. Nobody was angry, nobody wanted your cargo,
    /// and the whole exchange was over in three seconds because you were seen.</summary>
    private static readonly string[] InspectedLines =
    [
        "They found you aboard {body} and did what they were sent to do. Nobody raised their voice and nobody hurried.",
        "The lamp stopped on you {where} and three seconds later the sweep went on down the corridor. You were a line in somebody's report.",
        "You were seen {where}, told to stand still, and did not. They were never there to negotiate — they were there to make sure she stopped existing.",
    ];

    private static readonly string[] VoidLines =
    [
        "Lost to the void — no beacon, no body, just the long dark and a brain-backup that remembers the cold.",
        "The orbit slipped and the void kept you. There was no ground to hit and no one to hear the carrier fade.",
        "You went adrift past every well and the dark closed over the transponder. The backup is all that came home.",
    ];

    private static readonly string[] SuffocationLines =
    [
        "The tank went dry on {body}, a long way from the tube. The suit had told you where the line was, " +
        "and you had walked past it with your eyes open.",
        "You ran out of air on {body} with the way home still ahead of you. Nothing hunted you down; " +
        "you simply spent more than you were carrying.",
        "On {body} the gauge reached nothing. It had been counting honestly the entire walk out — the last " +
        "thing you heard was your own suit stop pretending.",
    ];

    // ── #574 · The same causes, told for the place they happened in. A derelict has no regolith, no tube
    //    and no sky; a landing party has all three. Sharing one pool made the game confidently describe the
    //    wrong world.
    private static readonly string[] ReeverLinesAboardAWreck =
    [
        "They took you deep in {body}'s hull, a long way from the lock. Nothing out there heard it.",
        "An Old One had you against a bulkhead aboard {body}. No dust to leave a mark in — just a corridor, " +
        "and then not you.",
        "You died in somebody else's ship. {body} kept her cargo and took you as well.",
    ];

    private static readonly string[] JoinedLinesAboardAWreck =
    [
        "The away team's beacons show you stopped moving deep inside {body}, and then moving again, wrong.",
        "Aboard {body} your nerve gave out in the dark, and you went further IN rather than back toward the " +
        "lock. Nobody followed to see why.",
        "They never recovered you from {body}. The hull is still logged as empty, which is the part that " +
        "should worry somebody.",
    ];

    /// <summary>#653 · CANON (Fable, #653 addendum 2): the wreck pool's lines 1 and 3 VERBATIM, with the station's own
    /// line in place of line 2. Pinned byte for byte.</summary>
    public static readonly string[] SuffocationLinesAboardAStation =
    [
        "The tank went dry inside {body}, in vacuum she has held for years. Her air went out a long time " +
        "before yours did.",
        "The station does not notice. Her books closed years ago, and they do not reopen for breath.",
        "On {body} the gauge reached nothing between one bulkhead and the next. She had nothing to give you.",
    ];

    private static DeathPlace AsAHull(DeathPlace place) => place == DeathPlace.Station ? DeathPlace.Derelict : place;

    private static readonly string[] SuffocationLinesAboardAWreck =
    [
        "The tank went dry inside {body}, in vacuum she has held for years. Her air went out a long time " +
        "before yours did.",
        "You ran out of air aboard {body} with the lock still ahead of you. A dead ship is patient about it.",
        "On {body} the gauge reached nothing between one bulkhead and the next. She had nothing to give you.",
    ];

    /// <summary>#525 · The overload ran out with the away team still aboard. The ship announced it herself,
    /// four times, in a voice recorded by somebody long dead — so the one thing these lines may never say is
    /// that the captain was surprised.</summary>
    private static readonly string[] ScuttleLinesAboardAWreck =
    [
        "You set it yourself, aft, next to the thing you were setting it on, and then you did not get " +
        "forward in time. {body} goes all at once and mostly inward, and you go with her.",
        "The last thing aboard {body} still able to power a speaker spent ninety seconds telling all hands " +
        "to muster at their assigned boats. You had already counted those boats. You were still counting " +
        "doors when the note came up half a tone and stopped.",
        "Somewhere between the reactor spaces and the lock you ran out of ship you had left open. {body} " +
        "was patient about the doors you dogged on the way aft, and precise about the ones you did not.",
    ];

    /// <summary>#609 · Suffocating on a floor of a clandestine facility. The tank is the clock everywhere on
    /// a surface, but down here the walk back is a walk to a LIFT — a machine, with a panel, that somebody
    /// has to still be paying for. None of these say what the place was for.</summary>
    private static readonly string[] SuffocationLinesBelow =
    [
        "The readout went amber somewhere on the stairs down and you told yourself you had counted right. " +
        "The corridor is poured concrete, lit on a circuit nobody has paid for in decades, and the car is " +
        "three hundred metres of shaft above you. It does not come when you are not there to call it.",

        "You sit down against a wall that was cast in a mould and then finished by hand, under {body}, in a " +
        "building with floors it does not count. The tank stops. The lights stay on, because the lights " +
        "were never the thing that was going to run out.",

        "There was air on the top floor. There was air on the top floor of every band, which is a fact you " +
        "understood perfectly and used to plan a route that turned out to be eleven metres too long.",
    ];

    /// <summary>#609 · Anything else that ends a captain down there. Deliberately spare: nothing is supposed
    /// to be alive on these floors yet, so this pool exists to be CORRECT rather than to be used.</summary>
    private static readonly string[] ReeverLinesBelow =
    [
        "It ends in a corridor under {body} that is on no plan anybody ever filed, and the only thing that " +
        "will ever know is a building which stopped being told things a long time ago.",
    ];
}

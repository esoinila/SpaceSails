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
/// causes — <see cref="DeathCause.Reevers"/>, <see cref="DeathCause.Joined"/> — and the deep-space
/// <see cref="DeathCause.Void"/> are wired READY (art + tested lines + the <see cref="DeathNarration.SurfaceEnd"/>
/// classifier): a surface Reever catch does NOT kill today (it prices heat + a nerve shock, see
/// Map.Surface ReeverCatch) and nerve overdraw does not kill either, so nothing routes to them yet. When the
/// surface-death lane lands it only has to set the cause — this rule already narrates it.</para>
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

    /// <summary>The eerie variant of a surface death: nerves shot to a sliver, and the last anyone saw, the
    /// captain walked TOWARD the crowd, not away — "joined them" (owner's cruise ruling). Chosen sparingly by
    /// <see cref="DeathNarration.SurfaceEnd"/> so it stays chilling. (Wired ready.)</summary>
    Joined,

    /// <summary>Lost to the void — adrift / EVA / an orbit that slipped, no body to name. (Wired ready for
    /// whatever void death lands; none routes here today.)</summary>
    Void,

    /// <summary>#564 · The tank ran out. Not a creature, not a fall, not nerve — the captain walked further
    /// than their air and knew it, because the suit said so out loud when they crossed the line.
    ///
    /// <para>It exists as its own cause for one reason: a suffocation narrated as "an Old One's hand is the
    /// last straw" would be the sim doing one thing while a SENTENCE reports another — the failure this
    /// project has paid for repeatedly (#545's death card blaming Reevers for three men with rifles). The
    /// caller PASSES this rather than rolling for it, because it is the one thing it knows for certain.</para></summary>
    Suffocated,

    /// <summary>#525 · THE OVERLOAD YOU SET YOURSELF ran out with you still aboard.
    ///
    /// <para>#525's outcome table records this row as <i>built</i>. It was not: <c>AdvanceScuttleClock</c>
    /// called <c>TriggerSurfaceOverdrawDeath</c> with no known cause, so the card rolled
    /// <see cref="DeathNarration.SurfaceEnd"/> and told the captain an Old One's hand was the last straw —
    /// aboard a drive-failure hull with nothing living in her, ninety seconds after they turned two keys
    /// themselves. The same failure this project has now paid for four times (#545's card blaming Reevers
    /// for three men with rifles), on the one death the captain unambiguously chose.</para>
    ///
    /// <para>Only ever <see cref="DeathPlace.Derelict"/>: the panel is bolted to somebody else's reactor,
    /// and your own ship has no such switch (that lane is #525's second half, and the CASTAWAY outcome it
    /// wants is still unbuilt). The tail is already right — <i>"Her log will not mention it."</i></para></summary>
    Scuttled,
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
}

/// <summary>
/// The pure narration seam for <see cref="DeathCause"/>: the art file each cause shows, the seeded
/// house-voice line pool that explains the death place-dependently, the WHAT-HAPPENED headline, and the
/// "joined them" trigger rule. All deterministic — a test pins an exact line for an exact seed — because
/// determinism is law in Core.
/// </summary>
public static class DeathNarration
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

    /// <summary>Classify a surface Reever death place-dependently: normally the Old Ones TOOK you
    /// (<see cref="DeathCause.Reevers"/>), but with the nerve shot to a sliver a seeded minority JOINED them
    /// (<see cref="DeathCause.Joined"/>). Pure and seeded so a test pins the split. This is the one law the
    /// surface-death lane calls when it lands.</summary>
    /// <summary>#564 · What the ground says when the air runs out. Deliberately NOT a roll and NOT
    /// place-dependent: the captain was warned, once and plainly, at the point of no return, and then went
    /// on. There is nothing to be mysterious about.</summary>
    public static string SuffocationHeadline(string bodyName) =>
        $"🫁 The tank read empty on {bodyName}. The suit had said so while there was still a walk back in it.";

    public static DeathCause SurfaceEnd(double nerveAtDeath, ulong seed)
    {
        if (nerveAtDeath > JoinedNerveSliver)
        {
            return DeathCause.Reevers; // steady enough hands — you ran the right way
        }

        return seed % (ulong)JoinedChanceInN == 0 ? DeathCause.Joined : DeathCause.Reevers;
    }

    /// <summary>
    /// The freeze-frame caption for a surface death — the quoted line under the headline.
    ///
    /// <para>This used to be a hardcoded ternary in the razor: <c>Joined</c> got the footprints line and
    /// EVERYTHING ELSE got <i>"…the nerve was already gone. The hand was only the last of it."</i> That was
    /// true while nerve was the only thing that ever actually killed anyone (#469) — but #480 made the five
    /// blows decide, so a captain can now be plainly MAULED to death with a steady gauge, and the card was
    /// still blaming their nerve. A death must not narrate the wrong cause.</para>
    ///
    /// <para><paramref name="nerveRanOut"/> is the honest discriminator the trigger already knows: the
    /// overdraw path passes true, the five-blows path passes false.</para>
    /// </summary>
    public static string SurfaceCaption(DeathCause cause, bool nerveRanOut)
    {
        if (cause == DeathCause.Suffocated)
        {
            return "\"…the gauge had been honest the whole way out.\"";
        }

        if (cause == DeathCause.Joined)
        {
            return "\"…and the footprints only lead one way — in.\"";
        }

        return nerveRanOut
            ? "\"…the nerve was already gone. The hand was only the last of it.\""
            : "\"…they simply kept coming, and the guard did not hold forever.\"";
    }

    /// <summary>The Grok-generated death image (under <c>art/</c>) a cause shows on the resurrection card.
    /// The two live causes reuse the existing BUSTED frames; the surface + void causes use the death-* set.</summary>
    /// <summary>#574 · The art, told for the place. A landing party dies in a suit on a surface and gets its
    /// own frame — the owner asked for the joke and it is the right one: <i>"We could have the gen AI image
    /// on landing party show that I was wearing the red shirt :-D"</i>. Star Trek's away-team red shirt is
    /// exactly the register the away-team death should have, and this game has always been willing to be
    /// funny about death (the parrot, the insurance, "there are worse epitaphs").</summary>
    // #583 adds Collector to the landing-party art: a captain taken on foot on a moon died in a SUIT on the
    // ground, whoever's hand it was — so it is the red-shirt card, not the gun-camera freeze-frame off a
    // ship's nose. The place decides the picture; the cause decides the words.
    public static string ArtFile(DeathCause cause, DeathPlace place)
    {
        // #609 · A death UNDER a moon is not a death ON one. The red-shirt card is a figure on regolith with
        // a sky over it, and down here there is neither. Owner asked for the picture by name: "let's make a
        // died in a secret lab photo also :-D"
        if (place == DeathPlace.Underground)
        {
            return "death-underground.jpg";
        }

        // #621 · AND A DEATH INSIDE A HULL IS NOT A DEATH ON A MOON EITHER. #574 gave the derelict its own
        // prose and its own tail and then left it the away team's PICTURE, which is the same bug one line
        // down the card: `death-reevers.jpg` is boot prints in regolith with a chest and an Earth in the
        // sky, and it was being shown under a sentence that reads "No dust to leave a mark in — just a
        // corridor". `death-joined.jpg` is a crowd of Old Ones on a moon, shown under "deep inside {body}".
        // And the third one, Suffocated, resolved to `death-suffocated.jpg`, WHICH THE GAME DOES NOT SHIP —
        // a broken image on a death card, invisible until #621 made the tank able to run out in there at
        // all. The place decides the picture; the derelict finally has one.
        if (place == DeathPlace.Derelict)
        {
            return "death-derelict.jpg";
        }

        return place == DeathPlace.LandingParty
            && cause is DeathCause.Reevers or DeathCause.Suffocated or DeathCause.Collector
            ? "death-landing-party.jpg"
            : ArtFile(cause);
    }

    public static string ArtFile(DeathCause cause) => cause switch
    {
        DeathCause.Collector => "busted-freeze-frame.jpg",
        DeathCause.Impact => "busted-ship-explosion.jpg",
        DeathCause.Reevers => "death-reevers.jpg",
        DeathCause.Joined => "death-joined.jpg",
        DeathCause.Void => "death-void.jpg",

        // #621 · This said `death-suffocated.jpg` for a year and THE GAME HAS NEVER SHIPPED THAT FILE. It
        // was not caught because a suffocation can only happen away from her deck and every away place —
        // ground, hull, Hive — is answered above before it ever reaches here, except one: a derelict, where
        // the tank could not run out (see AwayTeamSide). The name was a promise nothing had to keep.
        // The placeless answer is the away team's card, because a suffocation with no place named is a
        // captain out of their ship on the ground; the placed overload is the one the card actually calls.
        DeathCause.Suffocated => "death-landing-party.jpg",

        // The placeless answer for a cause with exactly ONE legal place is that place's card — the same
        // reasoning that gives a placeless suffocation the away team's. `CanHappen` makes Scuttled a
        // derelict-only cause, so there is no version of this death that happens anywhere else.
        //
        // It said `busted-ship-explosion.jpg` for one commit, on the grounds that a scuttling IS a hull
        // coming apart, and `EveryCause_HasItsOwnArt` caught it in CI: that frame is the captain's own ship
        // going up, and lending it to a death aboard somebody else's is the picture disagreeing with the sim
        // — which is exactly as bad as the words doing it, and is what this whole cause was added to stop.
        DeathCause.Scuttled => "death-derelict.jpg",
        _ => "busted-ship-explosion.jpg",
    };

    /// <summary>The short WHAT-HAPPENED headline for the block above the brain-backup copy.</summary>
    public static string Headline(DeathCause cause) => cause switch
    {
        DeathCause.Collector => "WHAT HAPPENED — the collectors got you",
        DeathCause.Impact => "WHAT HAPPENED — you hit the ground",
        DeathCause.Reevers => "WHAT HAPPENED — the Old Ones took you",
        DeathCause.Joined => "WHAT HAPPENED — you walked into the crowd",
        DeathCause.Void => "WHAT HAPPENED — lost to the void",
        DeathCause.Suffocated => "WHAT HAPPENED — the air ran out",
        // Not "the reactor got you". You set it.
        DeathCause.Scuttled => "WHAT HAPPENED — you were still aboard when she went",
        _ => "WHAT HAPPENED",
    };

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

    private static string[] PoolFor(DeathCause cause) => cause switch
    {
        DeathCause.Collector => CollectorLines,
        DeathCause.Impact => ImpactLines,
        DeathCause.Reevers => ReeverLines,
        DeathCause.Joined => JoinedLines,
        DeathCause.Void => VoidLines,
        DeathCause.Suffocated => SuffocationLines,
        // Scuttled can only ever be a derelict (CanHappen), so the derelict branch of Line always catches
        // it first. Named anyway: a cause absorbed by a `_ =>` default is exactly the shape #609 was filed
        // about, and the fallback here is the COLLECTOR pool — a boarding volley, over a captain who turned
        // two keys alone on a dead ship.
        DeathCause.Scuttled => ScuttleLinesAboardAWreck,
        _ => CollectorLines,
    };

    /// <summary>The seeded, place-dependent house-voice line for a death — the WHAT-HAPPENED sentence(s) the
    /// resurrection card reads before the existing brain-backup copy. <paramref name="bodyName"/> names the
    /// place (the moon, the body flown into); null/blank reads with a generic place so the line is whole.
    /// Pure: same cause + same seed + same body → same line.</summary>
    /// <summary>#574 · The line for a death, told for WHERE it happened. A derelict's dead have no regolith
    /// to lie in and no tube to have been short of.</summary>
    /// <summary>#574 · Can this cause honestly happen in this place? Owner: <i>"the debt collector deaths
    /// should also only happen in those situations never in any other :-D"</i>.
    ///
    /// <para>A collector is a PERSON who came for you — a heat-hunter with a ship and a boarding volley.
    /// There is nobody aboard a dead hull and nobody on an empty moon, so that death cannot occur there, and
    /// a card claiming it would be inventing a character. Likewise an impact is the ship meeting a world at
    /// speed, which cannot happen to somebody standing on one.</para>
    ///
    /// <para>Stated here rather than trusted to callers, so it can be TESTED and so any future death lane
    /// has one place to check itself against.</para></summary>
    public static bool CanHappen(DeathCause cause, DeathPlace place) => cause switch
    {
        // You have to be at the controls to fly a ship into something.
        DeathCause.Impact => place == DeathPlace.OwnShip,

        // #583 · AND NOW THE COLLECTORS COME TO THE GROUND. This used to be OwnShip-only, and it was right
        // for as long as heat could only ever be delivered to a deck — the owner's own ruling that a
        // collector death "should only happen in those situations, never in any other". What changed is the
        // situation: a repo boat now sets down on the regolith and a crew walks you down on foot, because
        // "FBI does not arrest cars ... they look for the driver". Still never on a derelict — boarding a
        // wreck they are already inside is its own arrival and is not built yet.
        // #609 · nor 150 m under a moon. Same reasoning as the derelict, one shaft further: a collector is a
        // person who came for you, and nobody rides a lift they would have to call a car for to collect a
        // debt in a building that is not on any register.
        DeathCause.Collector => place is not (DeathPlace.Derelict or DeathPlace.Underground),
        // The Old Ones and the tank reach you anywhere you are out of the ship.
        DeathCause.Reevers or DeathCause.Joined or DeathCause.Suffocated => place != DeathPlace.OwnShip,

        // #621 · The void is where there is NO ground and no hull — "no beacon, no body, just the long dark",
        // "there was no ground to hit". It was falling through the `_ => true` default and so was legal on a
        // moon, inside a wreck and a hundred and fifty metres under a moon, where its own prose is nonsense.
        // That default absorbing a gap is precisely the shape #609 was filed about; the cause is stated.
        DeathCause.Void => place == DeathPlace.OwnShip,

        // #525 · The scuttling panel is bolted to somebody else's reactor. Your own ship has no such switch
        // yet (that is #525's second half, with the CASTAWAY outcome still unbuilt), and a moon has no
        // reactor at all — so this cause is legal in exactly one place and says so, rather than falling
        // through a default that would make it legal 150 m under a moon.
        DeathCause.Scuttled => place == DeathPlace.Derelict,
        _ => true,
    };

    /// <summary>#574 · The closing beat of a death card. <i>"The freeze-frame holds"</i> was hard-appended in
    /// the markup to EVERY death — it is BUSTED's own language, about a collector's gun-camera still, and it
    /// was being read out over a captain who suffocated alone on a moon with nobody watching. Same failure
    /// as the borrowed prose, one line further down the card.</summary>
    public static string Tail(DeathCause cause, DeathPlace place) => (cause, place) switch
    {
        (DeathCause.Collector, _) => " The freeze-frame holds.",
        (_, DeathPlace.Derelict) => " Her log will not mention it.",
        (DeathCause.Suffocated, _) => " The gauge is still counting down in the dark.",
        (_, DeathPlace.LandingParty) => " The ground keeps what it is given.",
        _ => "",
    };

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

    public static string Line(DeathCause cause, DeathPlace place, ulong seed, string? bodyName)
    {
        if (place == DeathPlace.Derelict)
        {
            string[]? aboard = cause switch
            {
                DeathCause.Reevers => ReeverLinesAboardAWreck,
                DeathCause.Joined => JoinedLinesAboardAWreck,
                DeathCause.Suffocated => SuffocationLinesAboardAWreck,
                DeathCause.Scuttled => ScuttleLinesAboardAWreck,
                _ => null,
            };
            if (aboard is not null)
            {
                string t = aboard[(int)(seed % (ulong)aboard.Length)];
                return t.Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that hull" : bodyName!);
            }
        }

        // #609 · A death in the facility gets facility words. The away-team pool talks about regolith, a
        // suit and the walk back to the tube — none of which is where the captain is standing.
        if (place == DeathPlace.Underground)
        {
            string[] below = cause == DeathCause.Suffocated ? SuffocationLinesBelow : ReeverLinesBelow;
            return below[(int)(seed % (ulong)below.Length)]
                .Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that moon" : bodyName!);
        }

        // #583 · A collector catch on the GROUND gets ground words. The ship pool talks about a boarding
        // volley and a last stand, which is a different death entirely.
        if (place == DeathPlace.LandingParty && cause == DeathCause.Collector)
        {
            string onFoot = CollectorLinesOnFoot[(int)(seed % (ulong)CollectorLinesOnFoot.Length)];
            return onFoot.Replace("{body}", string.IsNullOrWhiteSpace(bodyName) ? "that ground" : bodyName!);
        }

        return Line(cause, seed, bodyName);
    }

    public static string Line(DeathCause cause, ulong seed, string? bodyName)
    {
        string[] pool = PoolFor(cause);
        string template = pool[(int)(seed % (ulong)pool.Length)];
        string body = string.IsNullOrWhiteSpace(bodyName) ? "that world" : bodyName!;
        string where = string.IsNullOrWhiteSpace(bodyName) ? "in open space" : $"off {bodyName}";
        return template.Replace("{body}", body).Replace("{where}", where);
    }
}

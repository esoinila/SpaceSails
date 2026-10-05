namespace SpaceSails.Core;

/// <summary>
/// #251 · THE CARD'S OWN WORDS — the suffocation headline, the surface ending and its caption, the art for a
/// cause and a place, the headline, and the one word a later captain gets (#563).
///
/// <para>Split out of <c>DeathNarration.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Its one field is a <c>const</c>.</para>
/// </summary>
public static partial class DeathNarration
{
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

        // #538 · AND THE THIRD PIECE OF THE SAME CARD. Adding DeathCause.Inspected fixed the headline and the
        // narration line and left THIS one still reading "…they simply kept coming, and the guard did not hold
        // forever" — a pack quote, under a headline that says you were found aboard, after three professionals
        // shot a captain who was standing still. Caught by booting the scene and reading the card, which is the
        // owner's method and the reason it keeps being right: the parts were all there and one of them was
        // pointed at the wrong death.
        if (cause == DeathCause.Inspected)
        {
            return "\"…nobody raised their voice, and it was over before the echo.\"";
        }

        // #638 · AND THE FOURTH PIECE OF THE SAME CARD, on the lane that finally reaches the void. Without
        // this arm a captain who ran out of reaction mass three weeks from anywhere fell through to the pack
        // quote below — "…they simply kept coming, and the guard did not hold forever" — under a headline
        // that says he was lost to the void, over a painting of a parted tether. Nothing came for him at all,
        // which is the whole point of the cause; the words are the issue's own ("nothing hunted you, you
        // simply could not get home") because that sentence is why option 1 was the honest void death.
        if (cause == DeathCause.Void)
        {
            return "\"…nothing hunted you. You simply could not get home.\"";
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
        place = AsAHull(place);   // #653 · a station's card is the derelict's
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
        // And the third one, Suffocated, resolved to `death-suffocated.jpg`, WHICH THE GAME DID NOT SHIP
        // UNTIL #915 — a broken image on a death card, invisible until #621 made the tank able to run out in
        // there at all. The place decides the picture; the derelict finally has one.
        if (place == DeathPlace.Derelict)
        {
            // #636 gave the derelict a card of its own and then handed all four of its causes the SAME one,
            // which was the right first move and is one move short. The Joined death aboard a hull is not
            // the same picture as the other three: it is the only one where the captain did not stop. The
            // prose says so — "you stopped moving deep inside {body}, and then moving again, wrong", "you
            // went further IN rather than back toward the lock" — and `death-derelict.jpg` is a hull with a
            // captain who has come to a halt in it. A card whose picture ends the sentence differently from
            // the words is this project's most expensive recurring bug, and it does not stop being one when
            // the two are merely at different volumes.
            //
            // The frame it gets instead is a dropped lamp and a figure walking AWAY from the lock, small and
            // already half swallowed. Nothing in it explains anything, which is the rule for this cause.
            return cause == DeathCause.Joined ? "death-joined-derelict.jpg" : "death-derelict.jpg";
        }

        // #633 · HER OWN CHARGES GET HER OWN FIREBALL. The two branches built the two halves of #525 apart;
        // reunited, a scuttling is legal in two places and the picture has to say which. Aboard a wreck it
        // is the derelict card above (a hull going inward, quietly, with the log that will not mention it);
        // on HER deck it is the same frame the impact death already earns, because that is literally what
        // the player just watched happen to their own ship.
        if (place == DeathPlace.OwnShip && cause == DeathCause.Scuttled)
        {
            return "busted-ship-explosion.jpg";
        }

        // #633 · #538's sweep team only ever finds you on an away leg (ChallengeRunsOut requires `_surface`),
        // so on the ground it is the away-team card — a captain in a suit, whoever's hand it was, exactly the
        // reasoning #583 used to move a foot-caught collector off the gun-camera frame.
        return place == DeathPlace.LandingParty
            && cause is DeathCause.Reevers or DeathCause.Suffocated or DeathCause.Collector
                or DeathCause.Inspected
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

        // #621 · This said `death-suffocated.jpg` for a year and THE GAME HAD NEVER SHIPPED THAT FILE, so it
        // was pointed at the away team's card instead — honest, because a suffocation with no place named is
        // a captain out of their ship on the ground, and invisible either way, since every away place the
        // tank can run out in is answered above before it ever reaches here.
        //
        // #664 · AND THEN #915 PAINTED IT. The borrow was the right call for exactly as long as there was
        // nothing to borrow from; the canvas that shipped is this slot's own — "a suit slumped seated
        // against anonymous grey plating, visor dark, a glove fallen open beside a chest gauge whose needle
        // sits at the bottom of its arc… no window, no landscape, no doorway: a suffocation with no place
        // named at all." A red-shirt on regolith under that sentence is the picture disagreeing with the
        // sim again, one file later, and this time with the right file sitting unreferenced in the folder.
        // The PLACED overload is untouched: on the ground it is still the away-team card, because there the
        // place IS named and the ground is what is in the frame.
        DeathCause.Suffocated => "death-suffocated.jpg",

        // It said `busted-ship-explosion.jpg` for one commit, on the grounds that a scuttling IS a hull
        // coming apart, and `EveryCause_HasItsOwnArt` caught it in CI: that frame is the captain's own ship
        // going up, and lending it to a death aboard somebody else's is the picture disagreeing with the sim
        // — which is exactly as bad as the words doing it, and is what this whole cause was added to stop.
        //
        // #633 · The cause is no longer derelict-only (main built her own charges), so the PLACED overload
        // above now answers both: her deck gets the fireball, a wreck gets this. What is left here is the
        // placeless answer, and a scuttling with no place named is the one the game staged first.
        DeathCause.Scuttled => "death-derelict.jpg",

        // #538 · Placeless, this is a captain out of their ship and away — the sweep team cannot reach her
        // own deck (see `CanHappen`) — so it takes the away-team card, exactly as a placeless suffocation
        // does, rather than the gun-camera freeze-frame that belongs to the collectors. A canvas of its own
        // is filed, not built.
        DeathCause.Inspected => "death-landing-party.jpg",
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
        // Not "the reactor got you". You set it — and "her" reads true for both the hull you were stripping
        // and your own ship, which is why one headline can serve the cause in both of its legal places.
        DeathCause.Scuttled => "WHAT HAPPENED — you scuttled her, and stayed aboard",
        DeathCause.Inspected => "WHAT HAPPENED — you were found aboard",
        _ => "WHAT HAPPENED",
    };

    // ── #563 · THE ONE WORD A LATER CAPTAIN GETS ──────────────────────────────────────────────────────
    //
    // Owner ruling, 2026-09-13, on the third open question of #563: "I love the own lineage. If not enough
    // material, fill in with strangers, preferably NPCs we know something about."
    //
    // A breadcrumb note about a predecessor has to say WHAT HAPPENED TO HIM, and there was exactly one place
    // in this game that has ever answered that question — the block above the brain-backup copy. So the note
    // does not get a sentence of its own: it gets THAT one, with the plate's own lead stripped off it. A
    // projection, deliberately, and not a second pool — the day somebody rewrites a headline the field book's
    // entry moves with it, which is the whole of law 5 in this repository ("two places computing one fact is
    // the bug, even while they agree"). The guard asserts by CALLING Headline, so the two can never drift.

    /// <summary>The plate's own lead, stripped to leave the clause. Lives here rather than at the caller so
    /// there is one spelling of it in the game.</summary>
    private const string HeadlineLead = "WHAT HAPPENED — ";

    /// <summary>#563 · The short WHAT-HAPPENED clause on its own, sentence-cased and WITHOUT a full stop —
    /// the caller's template supplies that. Read straight off <see cref="Headline"/>, so this is the death
    /// card's own words about that cause and never a second authoring of them.</summary>
    public static string CauseWord(DeathCause cause)
    {
        string plate = Headline(cause);
        string clause = plate.StartsWith(HeadlineLead, StringComparison.Ordinal)
            ? plate[HeadlineLead.Length..]
            : plate;
        return clause.Length == 0 ? clause : char.ToUpperInvariant(clause[0]) + clause[1..];
    }
}

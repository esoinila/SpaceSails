using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #603 · OFFERING SOMETHING TO SOMETHING. The satchel's one verb, resolved in Core so the answer is the
/// same wherever it is asked and so every refusal can be pinned by a test.
///
/// <para>The law all of this obeys, and the reason it lives here rather than in a razor file: <b>a refusal
/// always names its reason.</b> A control that does nothing and says nothing is indistinguishable from a
/// bug, and this ground has shipped that mistake twice in a week — the lift that only went down, and the
/// return that put the captain in a wall.</para>
/// </summary>
public static class SatchelTry
{
    /// <summary>What the captain is holding it up to.</summary>
    public enum Target
    {
        /// <summary>The lift panel's gated button — the shaft below this car's band (#590).</summary>
        ShaftGate,

        /// <summary>A rib's far end: <c>⟶ SECTOR n · d.d km</c>. Has no reader and never will (#590 call 2).</summary>
        SealedWay,

        /// <summary>A room door that will not open, with a department painted on it.</summary>
        RoomDoor,

        /// <summary>A sentry the captain has SET DOWN (#562's supply line).
        ///
        /// <para>#803 · It was <c>DrySentry</c> while the only bot that would take a handful was one reading
        /// 00, and that was the honest name then. The put verb loads any drum that is not full — a gun you
        /// are about to point at a lock wants six rounds whether it has eleven in it or none — so the name
        /// says what it now is. Nothing about the dry case changed; it is the same target, asked the same
        /// way.</para></summary>
        Sentry,

        /// <summary>The motion tracker — offering a paper as a lead rather than as reading matter.</summary>
        Tracker,
    }

    /// <summary>What happened. <paramref name="Worked"/> false is a refusal, and <paramref name="Line"/> is
    /// never empty in either case.</summary>
    public readonly record struct Outcome(bool Worked, string Line);

    /// <summary>#688 · Is this thing a way through that somebody shut? The three targets a captain meets
    /// standing in front of something they cannot walk past.</summary>
    public static bool IsDoor(Target target) =>
        target is Target.ShaftGate or Target.SealedWay or Target.RoomDoor;

    /// <summary>
    /// #688 · DOORS SUGGEST KEYS, NOT PAPERWORK. Owner, live on the deep site: <i>"Let's make a bigger story
    /// point about finding any kind of key or keycard and only suggest those at doors. Or tools, but not just
    /// like some papers."</i>
    ///
    /// <para>This changes what the satchel OFFERS, and nothing about what it says. Every refusal in this file
    /// stays exactly as it is — the wrong-shaft and wrong-site readings (#679) are the best storytelling the
    /// Hive has, and they are earned by holding up an authority that turns out to be for somewhere else. What
    /// was never storytelling was a shipping manifest carrying a live <b>try it →</b> at a bulkhead: the game
    /// dangling forty offers at a wall to hide the one that matters.</para>
    ///
    /// <para><b>Rounds stay inert at a door, and now for a better reason.</b> This used to say that shooting
    /// a lock open was an owner call nobody had made. It has been made (#803) — and the answer is not that a
    /// captain holds a handful of rounds up to a hasp. It is that a captain <b>walks the rounds to a gun</b>,
    /// sets the gun down where it can see the door, and points it with the handset. So the pocket still has
    /// nothing to say to a door; what changed is that the gun does (<see cref="ShootTheLock"/>).</para>
    ///
    /// <para>Away from a door nothing narrows: a paper offered to the tracker, rounds tipped into a sentry
    /// you have set down. This is a rule about DOORS, not a new law about pockets.</para>
    /// </summary>
    public static bool CanOffer(Satchel.Kind kind, Target target) =>
        !IsDoor(target) || kind == Satchel.Kind.Authority;

    /// <summary>
    /// Offer <paramref name="item"/> to <paramref name="target"/>.
    ///
    /// <para><paramref name="context"/> is whatever the target needs to judge it — for a shaft gate, the id
    /// of the authority that opens it. Null where the target does not care.</para>
    /// </summary>
    public static Outcome Offer(Satchel.Item item, Target target, string? context = null) => target switch
    {
        Target.ShaftGate => AtShaftGate(item, context),
        Target.SealedWay => AtSealedWay(item),
        Target.RoomDoor => AtRoomDoor(item),
        Target.Sentry => AtSentry(item),
        _ => AtTracker(item),
    };

    // ── #697 · THE WALLET IS ONE THING, AND IT COMES OUT ALL AT ONCE ────────────────────────────────────
    //
    // Owner: "Let's also add option to try all ID cards ... by grouping them into a folder in the inventory."
    // And on the register the answer had to be written in: "It is a little throw at the movie ... where he had
    // this wallet with zillion different contradictory IDs :-D"
    //
    // The comedy was already native and nobody had staged it. Every card down here is countersigned, current,
    // and issued by an office with no standing, and a captain who has worked three sites carries several that
    // disagree about who they work for. What was missing was the GESTURE — the wallet came out one card at a
    // time, and four authorities meant four presses producing four sentences of which one was worth reading.
    //
    // This FOLDS. It writes no new prose about cards: every sentence a fan can produce at a gate is a sentence
    // a single try already produced, because the wrong-shaft and wrong-site readings (#679/#683) are the best
    // storytelling the Hive has and restating them here would be two answers to one question. What the fold
    // decides is WHICH of them is worth saying.

    /// <summary>
    /// #697 · Offer the whole wallet at once, and answer with one line.
    ///
    /// <para>Three laws, in this order:</para>
    /// <list type="number">
    /// <item><b>Something works and the fan is over.</b> The outcome IS that card's own outcome — same
    /// sentence, same <see cref="Outcome.Worked"/> — so the caller does exactly what a single successful try
    /// does and there is never a second answer to keep in step with.</item>
    /// <item><b>Nothing works and the ladder decides.</b> The most informative refusal is the one said, once,
    /// rather than the first one met: another shaft of THIS site beats another site, which beats a card this
    /// build cannot read at all.</item>
    /// <item><b>A door with no reader answers once.</b> Fanning six authorities at a sealed way prints one
    /// honest sentence, not six, and it still names no card, no shaft and no site — #590 call 2 is not
    /// something a new control gets to renegotiate.</item>
    /// </list>
    ///
    /// <para>Anything in <paramref name="cards"/> that is not an authority is not in the wallet and is
    /// ignored, so a caller may hand over the whole pocket and can never have a paper narrated as a card.</para>
    /// </summary>
    public static Outcome OfferWallet(IReadOnlyList<Satchel.Item>? cards, Target target, string? context = null)
        => ReadTheWallet(cards, target, context).Outcome;

    /// <summary>#684 · The wallet's answer, AND the card it was answered with.
    ///
    /// <para><see cref="OfferWallet"/> is this call with the second half thrown away, and it stayed that way
    /// while the only caller was a dialog that already knew what it had pressed. The lift panel does not: it
    /// reads the whole wallet without being asked (owner's ruling on #684 — <i>the unprompted read IS the
    /// panel's character</i>), so the seam that tells the story of that read has to be told WHICH card the
    /// gate settled on, or the picture on the story card is a stranger's photograph (#695's own fault, one
    /// layer up).</para>
    ///
    /// <para><see cref="WalletRead.Read"/> is deliberately null at the two FINAL doors even when the wallet
    /// is full. Nothing there is allowed to name a card, a shaft or a site (#590 call 2), and handing a
    /// caller the card the fan happened to end on would let a new seam paint a face onto a sentence that
    /// exists to say the door has no opinion.</para></summary>
    /// <param name="Outcome">Exactly what <see cref="OfferWallet"/> returns — same sentence, same
    /// <see cref="Outcome.Worked"/>.</param>
    /// <param name="Read">The card the gate actually read: the one that worked, or the most informative
    /// refusal on #683's ladder. Null when the wallet was empty, and null at a door with no reader.</param>
    public readonly record struct WalletRead(Outcome Outcome, Satchel.Item? Read);

    /// <summary>#684 · <see cref="OfferWallet"/>'s three laws, with the chosen card kept rather than
    /// dropped. Every sentence in here is the one a single try already produced.</summary>
    public static WalletRead ReadTheWallet(
        IReadOnlyList<Satchel.Item>? cards, Target target, string? context = null)
    {
        var wallet = new List<Satchel.Item>();
        foreach (Satchel.Item c in cards ?? [])
        {
            if (c.Kind == Satchel.Kind.Authority)
            {
                wallet.Add(c);
            }
        }

        if (wallet.Count == 0)
        {
            return new(NothingToFan(target, context), null);
        }

        // A wallet of one is not a fan and must not read like one. The dialog never offers the folder for a
        // single card either — a folder of one is bureaucracy about bureaucracy — and this is the same ruling
        // one layer down, so the two can never disagree about what the captain just did.
        if (wallet.Count == 1)
        {
            return new(Offer(wallet[0], target, context), IsFinalDoor(target) ? null : wallet[0]);
        }

        Satchel.Item best = wallet[0];
        int bestRung = -1;
        foreach (Satchel.Item card in wallet)
        {
            Outcome one = Offer(card, target, context);
            if (one.Worked)
            {
                return new(one, card);
            }

            // Strictly greater: a tie goes to the card the captain found first, which is the only tie-break
            // that is not a re-sort of somebody's pocket.
            int rung = Rung(card, target, context);
            if (rung > bestRung)
            {
                (best, bestRung) = (card, rung);
            }
        }

        return target switch
        {
            // The two FINAL doors. Every card gives the same answer there, so the fold has nothing to rank
            // and something else to do instead: say once, in the deadpan the thing deserves, that the whole
            // wallet came out and the door has no organ to have an opinion with.
            Target.SealedWay => new(SealedWayLine(
                $"You go through the wallet a card at a time — {HowMany(wallet.Count)}, every one " +
                "countersigned, every one current, and no two of them agreeing about who you work for. "),
                null),
            Target.RoomDoor => new(RoomDoorLine(
                $"You hold them up one after another, {HowMany(wallet.Count)}, which is " +
                $"{Word(wallet.Count)} more than this door can read. "),
                null),
            _ => new(Offer(best, target, context), best),
        };
    }

    /// <summary>#684 · A door with no reader and nothing to say about a card. The two places a wallet read
    /// must come back holding nothing, however full the wallet was.</summary>
    private static bool IsFinalDoor(Target target) => target is Target.SealedWay or Target.RoomDoor;

    /// <summary>#697 · The whole wallet, counted the way a person counts a small thing. <i>"All 2"</i> reads
    /// like a receipt; a wallet is never big enough to need digits.</summary>
    private static string HowMany(int n) => n == 2 ? "both of them" : $"all {Word(n)}";

    /// <summary>A small number in words. Past twelve it is a figure again, which is the point at which a
    /// captain has stopped carrying a wallet and started carrying a filing cabinet.</summary>
    private static string Word(int n) => n switch
    {
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        6 => "six",
        7 => "seven",
        8 => "eight",
        9 => "nine",
        10 => "ten",
        11 => "eleven",
        12 => "twelve",
        _ => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>#697 · HOW MUCH A REFUSAL TEACHES — #683's ladder, as a number the fold can sort by. Higher
    /// wins.
    ///
    /// <para>The ranking is about INFORMATION and nothing else. A card for another shaft of this site tells
    /// the captain which floor of the building they are standing in is worth going back for; a card for
    /// another site tells them the wallet is for elsewhere and worth keeping (#613); a card this build cannot
    /// parse tells them only that this is not the thing. That is the order.</para></summary>
    private static int Rung(Satchel.Item card, Target target, string? wanted)
    {
        if (target != Target.ShaftGate || card.Kind != Satchel.Kind.Authority
            || !UndergroundComplex.AuthorityCard.TryParse(card.Id, out UndergroundComplex.AuthorityCard held))
        {
            return 0;
        }

        return UndergroundComplex.AuthorityCard.TryParse(wanted, out UndergroundComplex.AuthorityCard gate)
            && string.Equals(held.BodyId, gate.BodyId, StringComparison.Ordinal)
                ? 2     // Wrong shaft, THIS site — the nearest miss there is, and it names the shaft it runs.
                : 1;    // Somebody else's building.
    }

    /// <summary>#697 · An empty wallet. The dialog never shows the folder with nothing in it, which is exactly
    /// why this has to answer: a control reachable by a save, a keybind or a later caller that says nothing is
    /// indistinguishable from a bug.
    ///
    /// <para>At the two doors that will never open it is the door's OWN sentence, word for word — an empty
    /// wallet that suddenly started discussing wallets at a sealed way would be hinting that a full one
    /// matters, and none does.</para></summary>
    private static Outcome NothingToFan(Target target, string? context) => target switch
    {
        Target.SealedWay => SealedWayLine(""),
        Target.RoomDoor => RoomDoorLine(""),
        // #684 · …and at a shaft gate it carries the sentence the LIFT PANEL used to keep in a second set of
        // words of its own (UndergroundComplex.WrongCardLine, deleted with this). The panel reads the wallet
        // unprompted, so an empty one is the first refusal most captains ever meet, and what it has to leave
        // behind is not the fact that the wallet is empty — it is that somebody once held the thing.
        Target.ShaftGate => new(false,
            "🔒 The wallet is empty. The gate reads authorities and there is not one in it — not for this " +
            "hole, not for anybody else's. Somebody who worked these floors was carrying one. They did not " +
            "take it with them."),
        _ => new(false, "🎫 The wallet is empty. There is nothing in it to hold up to anything."),
    };

    private static Outcome AtShaftGate(Satchel.Item item, string? wanted)
    {
        if (item.Kind != Satchel.Kind.Authority)
        {
            return new(false, item.Kind switch
            {
                Satchel.Kind.Rounds => "🔒 The gate is not a thing you can shoot open, and you would not " +
                    "want to be standing here if it were.",
                Satchel.Kind.Paper => "🔒 The gate wants an authority, not a document. Nothing you are " +
                    "holding has a countersignature on it.",
                _ => "🔒 The gate has no interest in what somebody else did. It reads authorities.",
            });
        }

        if (wanted is { Length: > 0 } id
            && string.Equals(item.Id, id, StringComparison.Ordinal))
        {
            return new(true, "🎫 The gate reads it without hesitating.");
        }

        // ── #679 · A REFUSAL THAT SORTS THE WALLET ──
        //
        // Owner: "We need story telling about whether cards etc work or not." The old sentence —
        // "countersigned, current, and for another shaft" — was true of every wrong card there is, which
        // means the second card a captain tried taught them nothing the first had not. TRY is a verb, and
        // an offer that leaves you knowing exactly what you knew before it is a slot machine.
        //
        // So the gate reads the card OUT LOUD, the way a gate would: it says which shaft this one runs, and
        // when the card belongs to another building it says which site issued it. Both facts are printed on
        // the thing in the captain's hand (#679's CardTitle) — the gate is not telling them anything they
        // could not read themselves, it is telling them what it CHECKED.
        //
        // The #590 law is untouched: the wallet is not a skeleton key, and nothing here hints that a
        // different card would open a sealed way.
        if (UndergroundComplex.AuthorityCard.TryParse(item.Id, out UndergroundComplex.AuthorityCard held))
        {
            if (UndergroundComplex.AuthorityCard.TryParse(wanted, out UndergroundComplex.AuthorityCard gate)
                && string.Equals(held.BodyId, gate.BodyId, StringComparison.Ordinal))
            {
                return new(false,
                    $"🔒 The gate reads it, and reads it correctly: this card runs shaft {held.Band + 1} of " +
                    "this site — deeper paper than this gate wants, or shallower, but either way not the " +
                    "authority for the hole in front of you. The countersignatures are fine. The shaft " +
                    "number is not.");
            }

            return new(false,
                $"🔒 The gate reads it, and it is somebody else's business: this one was issued for " +
                $"{BodyNames.Designation(held.BodyId)} SITE. The same office, the same two hands on the " +
                "countersignatures, a different hole under a different moon — and this gate has no opinion " +
                "about any of that. It is looking for its own site code. It does nothing at all.");
        }

        // A card the gate cannot even parse — an edited save, or a future build's authority. It still says
        // why, because a silent nothing is indistinguishable from a bug.
        return new(false,
            "🔒 Countersigned, current, and for another shaft. The gate reads it, decides it is somebody " +
            "else's business, and does nothing at all.");
    }

    private static Outcome AtSealedWay(Satchel.Item item) => SealedWayLine(
        item.Kind == Satchel.Kind.Authority ? "You hold the card up to it anyway. " : "");

    /// <summary>The sealed way's one answer, with whatever the captain just did to it standing in front of it.
    ///
    /// <para>#590 call 2, held: these doors exist to be walls with a world behind them, and the moment one can
    /// be opened every one of them becomes a puzzle. The refusal is honest and FINAL — it does not hint that a
    /// different card would do it, because none would.</para>
    ///
    /// <para>#697 · <paramref name="held"/> is the only thing a fanned wallet is allowed to change here. The
    /// body is written once so a new control can never grow a second sealed way with different silences.</para></summary>
    private static Outcome SealedWayLine(string held) => new(false,
        $"🔒 {held}There is no reader on this. No slot, no plate, no panel — the bolts go through the " +
        "frame and into the rock, and they were tightened from a side you are not on. Nothing you " +
        "could be carrying is the thing this is waiting for, because it is not waiting.");

    private static Outcome AtRoomDoor(Satchel.Item item) => RoomDoorLine(
        item.Kind == Satchel.Kind.Authority ? "The card means nothing to it. " : "");

    /// <summary>The mechanical lock's one answer. Same shape as <see cref="SealedWayLine"/> and for the same
    /// reason: one body, and a preamble for what was just held up to it.</summary>
    private static Outcome RoomDoorLine(string held) => new(false,
        $"🔒 {held}The lock is mechanical and it was turned by somebody who then walked away with the " +
        "key. It is not refusing you; it has simply been shut for longer than you have been alive.");

    /// <summary>#803 · The sentry you are standing over. The kind refusal is unchanged; what the ROUNDS
    /// answer is now decided by <see cref="SentryHandLoad.Offer"/>, because the drum has a ceiling, a kind
    /// already in it and a state (slung or set down) — and a sentence written here could only guess at all
    /// three. This returns the yes; the caller that owns the magazine gets the numbers.</summary>
    private static Outcome AtSentry(Satchel.Item item)
    {
        if (item.Kind != Satchel.Kind.Rounds)
        {
            return new(false, "🔫 It takes rounds. That is the only thing it wants from a pocket.");
        }
        return new(true, $"🔫 {item.Count} round{(item.Count == 1 ? "" : "s")} into the hopper. It is not a " +
            "magazine, but it is not nothing.");
    }

    private static Outcome AtTracker(Satchel.Item item)
    {
        if (item.Kind != Satchel.Kind.Paper)
        {
            return new(false,
                "📡 The tracker reads movement and places. There is nothing on this it could plot.");
        }

        // #603 · How much it is worth is a property of the DOCUMENT, and the instrument shows the
        // difference — a mention washes half the ground, a position earns a dot.
        return new(true, FieldClue.Line(FieldClue.CertaintyOf(item.Id)));
    }
}

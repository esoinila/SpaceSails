namespace SpaceSails.Core;

/// <summary>
/// #251 · THE SENTENCES THE PLAYER READS (#411, #635) — the key's derivation and resolution, the ledger's
/// lines, the reach notice, the supply run and the consignment the board keeps sending back.
///
/// <para>Split out of <c>KaamosLore.cs</c> under #251 as a pure move: one contiguous run, no member
/// renamed, re-scoped or re-ordered. Every field here is a <c>const</c>; <c>Fragments</c>,
/// <c>PlatesById</c> and <c>BouncePlate</c>, the class's initialised statics, stay in the opening file in
/// their original order (#1163).</para>
/// </summary>
public static partial class KaamosLore
{
    // ── The sentences the player actually reads (#411 story pass, 2026-08-02). ────────────────────────────
    //
    // These used to be built in the client (Map.Kaamos), and two of them LIED about the sim — the house's
    // third and commonest bug class:
    //
    //   · the ledger's countdown printed "N more shards to see it" using the size of the whole intel pool
    //     (5) instead of the threshold that actually opens the capstone (IntelNeededToUnlock, 4), so it was
    //     always exactly one shard pessimistic — the gate opened while the card still asked for more;
    //   · the capstone's prose named FOUR specific shards ("the held pod's cycler window, Vantar's dates,
    //     the holder's tick, the bought coordinate") although the gate takes ANY four of five, so a captain
    //     who had never bought a coordinate was told the coordinate they never bought was in the answer.
    //
    // Both are fixed by moving the sentence to where the predicate lives: the number the ledger prints is
    // now computed from the same constant the gate reads, and the capstone credits exactly the shards this
    // progress holds. One source of truth, and the tests can hold the SENTENCE to the SIM.

    /// <summary>How many MORE intel shards this progress needs before the shape is clear and the capstone can
    /// be earned — zero once <see cref="HasEnoughIntelToEarnTheKey"/> is true. This is THE number the ledger
    /// prints, derived from <see cref="IntelNeededToUnlock"/> (the constant the gate itself reads) rather
    /// than from the size of the pool, so the countdown and the gate cannot drift apart.</summary>
    public static int IntelStillNeeded(KaamosProgress progress) =>
        Math.Max(0, IntelNeededToUnlock - IntelAssembled(progress));

    /// <summary>The clauses of the shards this progress actually holds, joined into the half-sentence the
    /// berth code is derived from ("the held pod's cycler window, Vantar's dates and the holder's tick").
    /// Only held shards appear — the capstone may never credit a piece the captain never found. Empty only
    /// for a progress holding no intel at all, which the gate never lets reach the capstone.</summary>
    public static string KeyDerivation(KaamosProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var clauses = IntelFragments
            .Where(f => progress.Has(f.Id) && f.KeyClause.Length > 0)
            .Select(f => f.KeyClause)
            .ToList();

        return clauses.Count switch
        {
            0 => string.Empty,
            1 => clauses[0],
            _ => string.Join(", ", clauses.Take(clauses.Count - 1)) + " and " + clauses[^1],
        };
    }

    /// <summary>The capstone as the player reads it: the pieces THEY hold answering each other, then the
    /// authored berth-code text. Used both by the bar seam that resolves it and by the ledger that re-reads
    /// it, so the two can never tell different stories about the same number.</summary>
    public static string KeyResolution(KaamosProgress progress)
    {
        string derivation = KeyDerivation(progress);
        string opening = derivation.Length > 0
            ? $"The pieces answer each other — {derivation}. "
            : "The pieces answer each other. ";
        return opening + KeyFragment.Lore;
    }

    /// <summary>The lore text to SHOW for a held fragment: the capstone reads as its resolution (the shards
    /// this captain actually assembled), everything else reads as authored.</summary>
    public static string LedgerLoreFor(KaamosFragment fragment, KaamosProgress progress)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        return fragment.IsKey ? KeyResolution(progress) : fragment.Lore;
    }

    /// <summary>The Captain's-ledger headline for the arc — the shard count and whether the key is held.
    ///
    /// <para>#635: a captain whose only KAAMOS is a returned filing has no shards to count, and
    /// <i>"0 of 5 shards assembled"</i> is a progress bar for a quest nobody has been given. The card in
    /// that state names the thing they are holding instead — which is the only thing they know.</para></summary>
    public static string LedgerHeadline(KaamosProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        int intel = IntelAssembled(progress);
        int pool = IntelFragments.Count();
        if (progress.Count == 0 && progress.BerthFilingBounced)
        {
            return BounceHeadline;
        }

        return progress.Has(KeyFragment.Id)
            ? $"❄ PROJEKTI KAAMOS — {intel} of {pool} shards · berth-code in hand"
            : $"❄ PROJEKTI KAAMOS — {intel} of {pool} shards assembled";
    }

    /// <summary>The ledger's state line — where this thread stands and what would move it. The countdown
    /// counts down to the GATE, not to the pool.</summary>
    public static string LedgerProgressLine(KaamosProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (progress.Count == 0 && progress.BerthFilingBounced)
        {
            return BounceLedgerLine;   // #635 · the front door, and nothing gathered behind it yet
        }

        if (CanReachEnceladus(progress))
        {
            return ReachLedgerLine;
        }

        if (HasEnoughIntelToEarnTheKey(progress))
        {
            return "❄ Enough intel to earn the berth-code. Ask around the bars — the pieces resolve into " +
                   "one number the sealed berth still listens for.";
        }

        int more = IntelStillNeeded(progress);
        return $"❄ The shape isn't clear yet — {more} more shard{(more == 1 ? "" : "s")} to see it. " +
               "A plaque line alone is never enough; one lone rumor is never enough.";
    }

    /// <summary>The loud one-time line the world says on the single edge that opens the reach. It tells the
    /// captain, in fiction, that the code is entered and that there is nothing further to do until the
    /// window comes round — the route lane is a later lane, and the waiting is the honest way to say so.
    /// (It used to end "For now: route pending", which is a production note wearing a parenthesis.)</summary>
    public const string ReachNotice =
        "   ❄❄ THE BERTH-CODE RESOLVES — you say the number once, to nobody, and the sealed berth is listed " +
        "to your hull. The cycler window is real. It is not open yet. Keep the code and keep the berth: when " +
        "the window comes round, a ship that is on the board rides it all the way in.";

    // ── #411 · THE RUN: the berth code buys a listed supply run ──────────────────────────────────────────
    //
    // The arc's own promise, kept in the arc's own register. The captain does not charter anything and is
    // not offered an adventure: a job appears on the board, filed the way any job is filed, and the dread
    // is that it is ROUTINE. The bible's sentence is "filing for it answers it" — so the player files.
    //
    // What the manifest says is the pod's manifest, verbatim, because it is the same consignment: the one
    // that was packed and then HELD, generations ago, and never sent. That is the only joining of the
    // pieces this text does, and it does it by repeating a slug the player has already read rather than by
    // explaining anything.

    /// <summary>The run's quest id — stable, so the accept path, the ledger and the route gate all name
    /// one thing. There is only ever one of these per universe.</summary>
    public const string SupplyRunQuestId = "kaamos-supply-run";

    /// <summary>What the job is called in the ledger.</summary>
    public const string SupplyRunTitle = "The KAAMOS supply run";

    /// <summary>The pitch. Whoever hands it over has no idea what it is; that is the whole point. To them
    /// it is a dormant berth that has come back onto the listing and a consignment nobody has moved in
    /// decades, and the fee is generous because the haul is absurd.</summary>
    public static string SupplyRunBlurb(int reward) =>
        $"“Berth came back onto the listing this week — first time in longer than anybody on this deck has been in the trade — and it " +
        $"came back with a standing consignment against it. CONSUMABLES, WINTERING CREW, FORTY SOULS. " +
        $"Nobody's moved it because nobody could file for the berth. You can, apparently. {reward:N0} cr, " +
        $"and the arc does the flying; you just have to be on it when it comes round. Park up alongside " +
        $"when you get there and the berth signs for it.”";

    /// <summary>The receipt line, said at the counter the moment the run is in hand. The manifest slug is
    /// the pod's, word for word — the cold pod in the regolith was carrying this, and was set down instead
    /// of sent. Nobody remarks on it.</summary>
    public const string SupplyRunAccepted =
        "❄ The slip comes across the counter face-down, the way slips do, and reads exactly as the one in " +
        "the regolith read: CONSUMABLES, WINTERING CREW, 40 SOULS · DEST. KAAMOS · HOLD FOR CYCLER WINDOW. " +
        "Somebody has struck HOLD through with a single line and written a date beside it. The date is soon.";

    // ── #635 · THE FRONT DOOR: a consignment the board will not take ─────────────────────────────────────
    //
    // The issue's four options, and why this is the one built: a bar RUMOUR (option 1) adds another line to
    // bars #410 already calls too chatty; a GLINTING PLAQUE (option 2) is the game announcing where to
    // look, which is the opposite of this house's grain; LEAVING IT (option 4) costs most players six beats
    // and the best line in the game. Option 3 — a mission-desk contract that bounces off the sealed berth —
    // is the most in-genre because the arc is about LOGISTICS, and it is the one the owner's 2026-08-03
    // ruling points at: a hook made of grammar the player already reads.
    //
    // The discipline it is held to: it may hand over NO shard (the pool is what the gate counts) and it may
    // state NOTHING of §2. Everything below is a docket. A docket may say a berth is HELD and it may say a
    // window is not open, because that is what a returned filing says; it may not say who is holding it.

    /// <summary>What the freight agent's card is titled in the ledger receipt.</summary>
    public const string BounceOfferTitle = "File a consignment the board keeps sending back";

    /// <summary>The agent's pitch, in their own voice. It names the price on the same card that takes it —
    /// the #634 lesson, learned the hard way by a button that spent 1,200 cr the instant it was clicked —
    /// and it is honest about what the captain is buying, which is an attempt and a piece of paper.</summary>
    public static string BounceOfferBlurb(int fee) =>
        $"“Fourth time this docket's come back at me and I've stopped asking the clerk why. Manifest's clean, " +
        $"consignee's listed, the berth is listed. The board just won't take the filing off my hull. You've " +
        $"got a hull. Put your number on it, I pay you {fee:N0} cr whichever way it falls, and if it bounces " +
        $"off you as well then it isn't me. It's out at the ice, if that means anything to you. Means nothing " +
        $"to me and I've been doing this thirty years.”";

    /// <summary>What the board answers. The whole hook, and it is four words of docket vocabulary: HELD, not
    /// closed; a window that is not open; a consignee that cannot be raised; and a berth with an address.
    /// Naming Ringside is not a signpost bolted on — a returned filing names the berth it was returned by,
    /// and that IS how a bounce receipt reads. What it never says is who is holding it, or why.</summary>
    public static string BounceReceipt(int fee) =>
        "❄ You put your own hull's number on the docket and the board answers before your hand is off the " +
        "plate: RETURNED — CONSIGNEE CANNOT BE RAISED — BERTH HELD, AWAITING CYCLER WINDOW. Held. Not closed, " +
        "not lapsed, not struck: held, at Ringside Exchange, for a window the board declines to date. The " +
        $"agent shrugs, counts out your {fee:N0} cr and takes the parcel back to wherever it lives between " +
        "attempts. Nobody asks for the receipt, so you keep it.";

    /// <summary>The ledger's headline while the returned filing is all this captain has. It names what is in
    /// the pocket rather than counting shards nobody has been asked for yet — <i>"0 of 5 assembled"</i> is a
    /// progress bar for a quest that has not been given.</summary>
    public const string BounceHeadline = "❄ A BERTH THAT WILL NOT TAKE A FILING";

    /// <summary>And the line under it. It points at nothing the world does not already do out loud: an
    /// exchange that has been running long enough to have a dedication puts it on the concourse wall, where
    /// everyone walks past it. The captain is left with a place and a habit, not an instruction.</summary>
    public const string BounceLedgerLine =
        "❄ A returned filing — held, not closed — for a berth at Ringside Exchange that answers nobody and " +
        "has not been struck off in a lifetime of windows. Ringside is old enough to have a dedication, and " +
        "old houses hang those where the concourse can read them.";

    /// <summary>The same fact, at rest, in the ledger.</summary>
    public const string ReachLedgerLine =
        "❄ The berth-code is entered and the ice-moon berth is listed to your hull. The cycler window is " +
        "real and not yet open — there is nothing to do now but hold the berth and wait for it.";
}

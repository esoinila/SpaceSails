namespace SpaceSails.Core;

/// <summary>Where a KAAMOS fragment surfaces in the living world — one delivery vector per existing
/// system (issue #411). The value is canon: it is how the design doc and the client agree on which
/// system is responsible for handing the player which piece, without either owning the other's code.
/// </summary>
public enum KaamosSource
{
    /// <summary>The dedication plaque — the shipped seed (#392). Ringside names PROJEKTI KAAMOS once;
    /// The Deep echoes the sealed berth unnamed. Read a plate, hold the piece.</summary>
    Plaque,

    /// <summary>A derelict supply pod found on a dig (the beach-comber/expedition surface, #346/#386).
    /// A cargo run that never arrived, still cold in the regolith with its manifest half-legible.</summary>
    DerelictPod,

    /// <summary>A log recovered from one of Dr. Vantar's sealed secret labs (#409) — the deep-field
    /// find. His own hand, or a machine's transcript of it, on what the ice moon was really for.</summary>
    LabLog,

    /// <summary>A rumor from a rare KAAMOS-holder contact at the bar, once the roving-contacts rota
    /// (#410) can seat one — someone who ran the berth, or knew a crew that wintered, and drinks alone.</summary>
    BarRumor,

    /// <summary>A tip bought with a round, through the drink-offer / overheard system (#308/#347). Coin
    /// on the counter buys a coordinate, a date, a name a sober tongue would keep.</summary>
    BoughtTip,

    /// <summary>The berth code itself — the capstone. Not a rumor but the KEY: the one-time cycler
    /// window and the string that answers the sealed berth. It surfaces ONLY once enough of the rest is
    /// in hand (see <see cref="KaamosLore.CanReachEnceladus"/>), the earned last piece.</summary>
    BerthCode,
}

/// <summary>
/// One assembled piece of PROJEKTI KAAMOS — a fragment of the sealed ice-moon mystery. Pure authored
/// Core data (repo agreement §9), the plaque/souvenir idiom: evocative, partial, never the whole
/// exposition. Assembling enough of them is the quest state (<see cref="KaamosProgress"/>), and the
/// last one earned is the way to reach the unreachable.
/// </summary>
/// <param name="Id">Stable canon id — the vault stores these, tests pin them, order is authored.</param>
/// <param name="Title">The short shelf-label the ledger shows for the piece.</param>
/// <param name="Source">Which living system hands this piece over (issue #411's fragment map).</param>
/// <param name="IsKey">True for the single capstone (the berth code) that turns intel into a route.</param>
/// <param name="Lore">The fragment text. A shard of the truth, never the truth entire.</param>
/// <param name="KeyClause">What THIS shard contributes when the berth code resolves — the half-sentence the
/// capstone names it by ("the held pod's cycler window"). Empty for the capstone itself, which is the thing
/// being derived. The capstone's prose is built from the clauses of the shards actually in hand
/// (<see cref="KaamosLore.KeyDerivation"/>), so it can never credit a piece the player never found.</param>
public sealed record KaamosFragment(
    string Id, string Title, KaamosSource Source, bool IsKey, string Lore, string KeyClause = "");

/// <summary>
/// A reveal PLATE for a KAAMOS beat — the house's title/image/caption card (#528), the same recipe the
/// vented-room card uses: a title that names the place and the verb, one painted image of a CONSEQUENCE,
/// and a caption that describes EVIDENCE and stops. It never says what any of it means; the arc's own
/// prose (the fragment <see cref="KaamosFragment.Lore"/>, which still goes out on the pulse line and into
/// the ledger) supplies the words, and the plate supplies the dread.
/// </summary>
/// <param name="Title">The stamp across the top of the card.</param>
/// <param name="ArtFile">The painting, under <c>wwwroot/</c>. A missing file degrades to no image at all
/// (the house <c>onerror</c>-hide law) — never a broken frame. <c>RevealPlatesArePaintedTests</c> holds
/// every one of these to a file that actually exists.</param>
/// <param name="Caption">What is in front of you, and nothing about what it implies.</param>
public sealed record RevealPlate(string Title, string ArtFile, string Caption);

/// <summary>
/// PROJEKTI KAAMOS — "the polar night" — the seeded lore-fragment pool and the reach logic (issue
/// #411). The arc is a slow-burn mystery: scattered fragments, each surfaced by a system that already
/// exists, assemble into enough intel to earn the one thing the world has always denied — a way to
/// REACH the canonically-unreachable ice moon (Enceladus, ~1.11e9 m, far past the shuttle's
/// <see cref="ShuttleRange.RangeMeters"/> 5e8 m hop). This class is the north star the sibling lanes
/// (secret labs #409, roving contacts #410, the eventual Enceladus route) build against; it holds the
/// authored text and the pure predicates and touches no world code.
///
/// <para><b>The truth (invented, original, homage-not-reproduction).</b> The ice moon has a sunless
/// ocean under kilometres of ice — a permanent polar night. KAAMOS was Dr. Mielos Vantar's terminal
/// work (#409): not backups in jars but one continuous mind grown across a wintering crew, kept lucid
/// through decades of dark in the cold water below. It was moved to Enceladus <i>because</i> it was
/// unreachable — a place to keep something alive that should not have kept living. It worked. It is
/// still down there, awake, wintering, and it has been filing for a supply run that stopped coming.
/// The berth is still on the board because, from beneath the ice, someone is still asking for it. The
/// runs stopped when the last ship in reported not the crew but one voice using all of their names.
/// That is why it is sealed; that is why nobody files for the berth — filing for it answers it. The
/// reveal (the biggest #391 sanity-throw, wired later by the sanity lane, not here) is that you reach
/// it, and it is glad you came, and it remembers Vantar, and it has kept a berth warm for you.</para>
///
/// <para><b>Kept mysterious by construction.</b> No single fragment states the truth; each is a shard.
/// Only assembly implies the shape, and only the earned capstone opens the door — the payoff is the
/// reveal at Enceladus itself, delivered by a later lane, never spoiled in this text.</para>
/// </summary>
public static class KaamosLore
{
    /// <summary>The seeded fragment pool, in authored (canonical) order. Deterministic — no wall clock,
    /// no RNG: the same pieces exist in every universe, and only WHICH the player has ASSEMBLED differs
    /// (that lives per-thread in <see cref="KaamosProgress"/>). Five intel shards from five systems, and
    /// one capstone key.</summary>
    public static readonly IReadOnlyList<KaamosFragment> Fragments =
    [
        // ── Intel shards — the mystery, gathered. Each is one existing system's piece. ──

        new("listed-berth", "The listed berth", KaamosSource.Plaque, false,
            "Ringside's dedication says it plainly, if you read the whole plate: her first commission was " +
            "the KAAMOS supply run out to the ice moon, and the berth for it is still on the board, still " +
            "listed, and nobody has filed for it in a long time. A berth kept open is a berth someone " +
            "expects a ship to fill. The name means the polar night. Nobody at the Exchange will say whose.",
            "the plate's still-listed berth"),

        new("cold-pod", "The cold supply pod", KaamosSource.DerelictPod, false,
            "Half-buried in the regolith, a supply pod that never made its run — hull frost-cracked, its " +
            "manifest slug still readable: CONSUMABLES, WINTERING CREW, 40 SOULS · DEST. KAAMOS · HOLD FOR " +
            "CYCLER WINDOW. The seals were never broken. Whatever it was carrying to the ice moon, the ice " +
            "moon went without it — and the pod was logged HELD, not lost. Someone chose not to send it.",
            "the held pod's cycler window"),

        new("vantar-log", "Vantar's wintering log", KaamosSource.LabLog, false,
            "A log salvaged from a sealed lab, the hand disciplined and then, later, not: \"The dark below " +
            "the ice is total and it is patient, and I have taught them to be the same. They do not sleep " +
            "through the night — they hold it, together, as one held breath. The winter does not kill what " +
            "refuses to be many.\" The last entries are dated long after his listed death. They are calm.",
            "Vantar's dates"),

        new("holders-tell", "The berth-holder's tell", KaamosSource.BarRumor, false,
            "The one who used to run the KAAMOS berth drinks alone and answers only sideways: \"You don't " +
            "file for that berth, spacer. You keep it. There's a difference, and I learned it late.\" Pressed, " +
            "quieter: \"It still calls the manifest in. Every window, right on the tick. Same forty names. " +
            "I stopped reading who was speaking them.\" Then the glass is empty and the conversation with it.",
            "the holder's tick"),

        new("bought-coordinate", "The bought coordinate", KaamosSource.BoughtTip, false,
            "A round on the counter buys the rest of it: a coordinate off the ephemeris where the charts " +
            "just say ICE MOON — UNREACHABLE, and a date, and the word CYCLER. \"The window's real,\" they " +
            "say, pocketing the coin. \"Comes round rare. A ship that's on the board when it opens can ride " +
            "it in. Getting back out — that's not the part they sell tickets for.\" You have the where and the when.",
            "the bought coordinate"),

        // ── The capstone. Not a rumor: the earned KEY. Surfaces only once the rest is in hand. Its prose
        //    names NO shard: which pieces answered each other depends on which the player actually holds,
        //    and that sentence is built at read time by KeyDerivation/KeyResolution. ──

        new("berth-code", "The KAAMOS berth code", KaamosSource.BerthCode, true,
            "One number falls out of them, the string the sealed berth still listens for. It is not a " +
            "password so much as a name the dark already knows. Enter it on the board when the window opens " +
            "and the berth stops being a place nobody files for. It becomes a place expecting you. You " +
            "could go to the ice moon now. That was always the danger."),
    ];

    /// <summary>How many INTEL shards (non-key fragments) must be assembled before the capstone can be
    /// earned and the reach opens. Set below the full intel count on purpose: the mystery does not
    /// demand a completionist sweep — enough pieces to see the shape is enough to be let (or lured) in.
    /// The plaque line alone is never enough; a lone rumor is never enough.</summary>
    public const int IntelNeededToUnlock = 4;

    /// <summary>The intel shards — every fragment that is not the capstone key. These are what
    /// <see cref="IntelNeededToUnlock"/> counts.</summary>
    public static IEnumerable<KaamosFragment> IntelFragments => Fragments.Where(f => !f.IsKey);

    /// <summary>The single capstone key (the berth code) — the earned last piece that turns intel into a
    /// route. There is exactly one; the constructor of the pool guarantees it and a test pins it.</summary>
    public static KaamosFragment KeyFragment => Fragments.Single(f => f.IsKey);

    /// <summary>The fragment with this id, or null if unknown — the tolerant lookup a loader uses so a
    /// vault carrying a since-renamed id simply drops it rather than throwing.</summary>
    public static KaamosFragment? ById(string id) => Fragments.FirstOrDefault(f => f.Id == id);

    /// <summary>True once every id in this pool is unique and exactly one fragment is the key — an
    /// authoring invariant the tests assert, exposed so any future editor of the pool can self-check.</summary>
    public static bool PoolIsWellFormed =>
        Fragments.Select(f => f.Id).Distinct().Count() == Fragments.Count &&
        Fragments.Count(f => f.IsKey) == 1;

    // ── The reach logic (pure predicates — the unlock HOOK, not the route). ──

    /// <summary>How many intel shards this progress has assembled (the key never counts as intel).</summary>
    public static int IntelAssembled(KaamosProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return IntelFragments.Count(f => progress.Has(f.Id));
    }

    /// <summary>True once enough intel is assembled that the shape is visible and the capstone can be
    /// earned — the moment the world should offer the way to the berth code (a later lane surfaces it as
    /// the <see cref="KaamosSource.BerthCode"/> piece). Does NOT itself mean you can go: see
    /// <see cref="CanReachEnceladus"/>.</summary>
    public static bool HasEnoughIntelToEarnTheKey(KaamosProgress progress) =>
        IntelAssembled(progress) >= IntelNeededToUnlock;

    /// <summary>
    /// The unlock predicate (issue #411, deliverable 3): can this thread's captain reach the
    /// canonically-unreachable ice moon? True only when the capstone berth code is in hand AND the intel
    /// that legitimises it is assembled — the key alone, pasted from a cheat, is not enough; the code has
    /// to be the one the pieces implied. This is the WHOLE gate. It is deliberately PURE and world-blind:
    /// it decides <i>whether</i> the route may exist, never spawns it.
    ///
    /// <para><b>The fiction of HOW (documented; the route itself is a follow-up, not wired here).</b>
    /// Reaching Enceladus is not a longer shuttle hop — the gap (~1.11e9 m) is more than twice the
    /// shuttle's proven reach and always will be. It is a ONE-TIME CYCLER WINDOW: a slow free-return
    /// arc that comes round rarely and, for a ship that is "on the board" (berth code entered) when it
    /// opens, rides all the way in. The berth code is what puts you on the board. A later lane turns a
    /// true return of this predicate into an actual navigable route/scenario beat; until then this is a
    /// tested hook and nothing more, so it cannot collide with the shuttle/scenario code the other lanes
    /// and labs use.</para>
    /// </summary>
    public static bool CanReachEnceladus(KaamosProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return progress.Has(KeyFragment.Id) && HasEnoughIntelToEarnTheKey(progress);
    }

    /// <summary>The body id the reach opens toward — the canon ice-moon id used across scenarios, kept
    /// here so the eventual route lane binds to one agreed string rather than a fresh literal.</summary>
    public const string IceMoonBodyId = "enceladus";

    /// <summary>The reveal's sanity cost, as a HOOK value only (issue #411: "the biggest one"). Larger
    /// than <see cref="NerveModel.MonolithSightShock"/> by design — reaching the wintering mind is the
    /// heaviest #391 throw in the game. NOT wired here (the sanity/#226 lane owns <c>NerveModel</c> and
    /// will consume this when the reveal is built); named in this lane's own file so the number is
    /// authored where the fiction lives and nothing in the sanity Core is touched.</summary>
    public const double RevealSanityShockHook = 40.0;

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

    // ── The plates (#528 · the reveal-card audit, 2026-08-02). ────────────────────────────────────────
    //
    // The arc's three most loaded beats — the pod that was held, the one who kept the berth, and the berth
    // answering — used to arrive as a pulse line: a toast that fades in a second and a half. That is the
    // systemic failure #528 names. They now raise the house reveal card as well, and because the sentence
    // belongs beside the predicate (the #634 lesson: a comment that names a second source of truth is a
    // TODO with no owner), the titles and captions live HERE, keyed by the same fragment ids the pool and
    // the gate use, rather than as literals in the client.
    //
    // Caption discipline, inherited from the vented-room card: EVIDENCE, then stop. Not one of the three
    // says what it means. The pod's seals are latched; nobody says who chose not to send it. The stools
    // beside the drinker are empty; nobody says why. The board has one line lit; nobody says who is asking.

    /// <summary>The reveal plate for a fragment id, keyed by the same ids <see cref="Fragments"/> uses.
    /// Only the beats that EARN a card are here — a shard that is a line on a plaque or a coordinate bought
    /// over a counter is the right size as prose, and over-carding cheapens the ones that are not.</summary>
    private static readonly Dictionary<string, RevealPlate> PlatesById = new(StringComparer.Ordinal)
    {
        ["cold-pod"] = new(
            "❄ THE POD THAT WAS HELD",
            "art/kaamos-cold-pod.jpg",
            "Frost has split her along three seams and the dust has scoured her manifest plate down to " +
            "nothing you can read. Every clamp seal is still latched exactly as it left the yard. She was " +
            "not lost on the way out — she never went. Somebody stood here, in a shed, and set her down."),

        ["holders-tell"] = new(
            "🌑 THE ONE WHO KEPT THE BERTH",
            "art/kaamos-berth-holder.jpg",
            "The stools either side of them are empty, in a bar where nothing is empty. Two glasses down " +
            "before you sat and neither of them was looked at. They answer the bulkhead rather than you, " +
            "and every answer stops one word before the part you asked about."),

        ["berth-code"] = new(
            "❄❄ ONE LINE STILL LIT",
            "art/kaamos-berth-resolves.jpg",
            "Rows and rows of dead slots going up into the dark of the concourse, and low on the board one " +
            "line still burning — burning since before the characters on it wore away. Nothing has filed " +
            "against it in a lifetime. Nobody ever told the board to stop asking, and it has not."),
    };

    /// <summary>#635 · The front door's plate. It is NOT in <see cref="PlatesById"/> on purpose: that
    /// dictionary is keyed by fragment id and every key in it must be a real pool fragment (a test says so),
    /// and the returned filing is deliberately not a fragment. It earns a card anyway — it is the first
    /// thing this arc ever says to most captains, and #528's whole finding is that the beats which turn a
    /// story get a picture. Caption discipline as everywhere: evidence, then stop. Three return stamps and
    /// nobody throwing it away; not one word about who is not answering.</summary>
    public static readonly RevealPlate BouncePlate = new(
        "❄ RETURNED TO SENDER",
        "art/kaamos-returned-filing.jpg",
        "The parcel is back on the counter with four return stamps overlapping on the same corner of the " +
        "docket, each one fainter than the last. The consignee line is filled in and the delivery line is " +
        "blank. Nobody behind the counter looks at it, and nobody has thrown it away.");

    /// <summary>The reveal plate this beat earns, or null for the beats that are the right size as prose.
    /// Asked by the client at the single seam where a shard is assembled, so a plate can never be shown for
    /// a shard the captain did not just find.</summary>
    public static RevealPlate? PlateFor(string fragmentId) =>
        fragmentId is not null && PlatesById.TryGetValue(fragmentId, out RevealPlate? plate) ? plate : null;

    /// <summary>Every plate in the arc, with the fragment id it belongs to — the tests' handle, so a plate
    /// keyed to a shard that does not exist, or pointed at art nobody painted, fails the build.</summary>
    public static IEnumerable<KeyValuePair<string, RevealPlate>> AllPlates => PlatesById;

    /// <summary>The label on the bar seam's button for the step this bar can take. The bought coordinate
    /// COSTS, so its button says so and says how much, like every other counter at that bar that takes
    /// coin — a free step never wears a price, and a paid step is never disguised as a question.</summary>
    public static string BarSeamLabel(string? stepFragmentId) => stepFragmentId switch
    {
        "berth-code" => "❄ Put the KAAMOS pieces together",
        "bought-coordinate" => $"🌑 Buy the KAAMOS coordinate · {KaamosFind.BoughtCoordinateCredits:N0} cr",
        _ => "🌑 Ask about KAAMOS",
    };

    /// <summary>The hover line behind <see cref="BarSeamLabel"/> — what this particular step actually is.</summary>
    public static string BarSeamTitle(string? stepFragmentId) => stepFragmentId switch
    {
        "berth-code" => "Spread what you have gathered on the table and let the numbers answer each other",
        "bought-coordinate" =>
            $"Stand a round for the where and the when — {KaamosFind.BoughtCoordinateCredits:N0} cr off the purse",
        _ => "Ask around about the sealed ice-moon berth — PROJEKTI KAAMOS",
    };
}

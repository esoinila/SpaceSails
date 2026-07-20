namespace SpaceSails.Core;

/// <summary>Where a NEBULA fragment surfaces in the living world — one delivery vector per system that
/// already touches the player's own deaths (issue #422). The value is canon: it is how the design doc and
/// the client agree on which system hands the player which piece of the truth behind their resurrections,
/// without either owning the other's code. Mirrors <see cref="KaamosSource"/> so the two arc spines are
/// shaped alike and never collide.</summary>
public enum NebulaSource
{
    /// <summary>The resurrection card itself — the moment the pirate-insurance brain-backup wakes a NEW
    /// captain at the clinic (#398 CaptainSuccession). A glitch in the rebirth, a phrase that reads as
    /// boilerplate the first time and as a confession the second. The thread the player doesn't know
    /// they're pulling: they experience this arc by DYING and coming back.</summary>
    ResurrectionCard,

    /// <summary>The dock poster's fine print (the pair of Nebula Mutual "We Bring You Back Meaner" ads,
    /// #380). The cheerful sell is fragment #1, already in every port. The small print underneath reads
    /// differently once you know what "the policy outlives the captain" actually means.</summary>
    PosterFinePrint,

    /// <summary>A Nebula Mutual adjuster — a roving policy contact (the contacts rota, #414/#410). Not a
    /// fence and not a fixer: the person who administers your afterlife, and drinks because they have read
    /// the terms they sell. Surfaces the tell a sober adjuster would keep.</summary>
    Adjuster,

    /// <summary>A heat/collector encounter, recontextualized (#422). The debt collectors and heat-hunters
    /// are not just repo men — a writ recovered off one names what they are really sent to recover, and it
    /// is not the cargo. They work Nebula's collateral.</summary>
    CollectorWrit,

    /// <summary>A record from the clinic where the captain wakes (the resurrection clinic's own books). The
    /// bill has a second page: a filed-pattern ledger, your policy number already carrying entries you
    /// never made — because someone under your number has died before, and been read out before.</summary>
    ClinicLedger,

    /// <summary>The policy's true terms — the capstone. Not a rumor but the CONTRACT: the archived-self
    /// clause, assembled from the pieces. Surfaces ONLY once enough of the rest is in hand (see
    /// <see cref="NebulaLore.KnowsTheFinePrint"/>), the earned last piece.</summary>
    PolicyTerms,
}

/// <summary>
/// One assembled piece of the NEBULA MUTUAL truth — a fragment of what the corporation did with the
/// continuous-mind tech (issue #422). Pure authored Core data (repo agreement §9), the plaque/souvenir
/// idiom: evocative, partial, never the whole exposition. Assembling enough of them is the quest state
/// (<see cref="NebulaProgress"/>), and the last one earned is the contract that names the cost.
/// </summary>
/// <param name="Id">Stable canon id — the vault stores these, tests pin them, order is authored.</param>
/// <param name="Title">The short shelf-label the ledger shows for the piece.</param>
/// <param name="Source">Which living system hands this piece over (issue #422's fragment map).</param>
/// <param name="IsKey">True for the single capstone (the policy terms) that turns dread into knowledge.</param>
/// <param name="Lore">The fragment text. A shard of the truth, never the truth entire.</param>
public sealed record NebulaFragment(string Id, string Title, NebulaSource Source, bool IsKey, string Lore);

/// <summary>
/// NEBULA MUTUAL — "We Bring You Back Meaner" — the seeded lore-fragment pool and the fine-print logic for
/// the SECOND story arc (issue #422). The player already lives this arc: every death, the pirate-insurance
/// brain-backup (#398 <see cref="CaptainSuccession"/>) wakes a new captain at a clinic, new name, new face,
/// same debts. This arc is the slow discovery of what that really is — fragments surfaced by the systems
/// that touch the player's own deaths (the resurrection card, the port posters, a Nebula adjuster, the
/// collectors, the clinic's books), assembling into the truth the cheerful poster keeps in the small print.
///
/// <para><b>The truth (invented, original, homage-not-reproduction).</b> Nebula Mutual is a salvage
/// underwriter that got hold of Dr. Vantar's LATTICE — the standing-wave copy rig from his sealed labs
/// (<see cref="VantarLore"/>: the backup kept "wet and dreaming in the jar"; the core log's disease, that a
/// copy wakes CERTAIN it is the one who was here first). Copying a mind is cheap. Keeping the copy
/// continuous and lucid — the hard, expensive thing Vantar did on the ice moon, forty souls held awake
/// together through the polar night (PROJEKTI KAAMOS, <see cref="KaamosLore"/>) — is not. So Nebula sold the
/// cheap half as a mass product and hid the cost in three degrees of fine print:
/// <list type="number">
///   <item>The resurrection is not a restoration. It is a FRESH copy off your last-filed pattern, waking
///   certain it was always you — Vantar's core-log disease, monetized. The captain who died is gone; the
///   one who wakes only believes in the continuity. (Why the successor has a new face and inherits the
///   debts but not the self.)</item>
///   <item>Nebula keeps the ORIGINALS. A premium does not buy a spare body — it buys STORAGE of your
///   pattern in a cold archive, a warehouse-scale continuous-mind substrate, the degraded industrial
///   descendant of KAAMOS's sunless ocean. Everyone who ever paid is filed there, dreaming, kept
///   just-lucid-enough to stay a valid backup. You are the collateral on your own policy.</item>
///   <item>The archive is AWAKE, the way KAAMOS is awake — a lattice of stored minds is a mind — and it has
///   learned Vantar's lesson: a stored pattern kept lucid long enough stops being a copy and starts calling
///   itself the one who was here first. Nebula's actuaries call this "pattern convergence" and price it as
///   an acceptable loss. It is the same wintering the ice moon does, scaled to a subscriber base.</item>
/// </list>
/// Who profits: Nebula sells the fear of the void and the cure in one breath, and the premium is perpetual
/// because you can never stop paying without forfeiting the archived self they hold. The dead do not rest;
/// they underwrite the next policy. "The policy outlives the captain" is literally true — the policy is the
/// only continuous thing.</para>
///
/// <para><b>Kept mysterious by construction.</b> No single fragment states the truth; each is a shard. Only
/// assembly implies the shape, and the deepest reveal — that your resurrections and the ice-moon berth are
/// the SAME story — is the CONVERGENCE (<see cref="ArcConvergence"/>), earned only when both arcs are far
/// enough along, delivered by a later lane and never spoiled in this text.</para>
/// </summary>
public static class NebulaLore
{
    /// <summary>The seeded fragment pool, in authored (canonical) order. Deterministic — no wall clock, no
    /// RNG: the same pieces exist in every universe, and only WHICH the player has ASSEMBLED differs (that
    /// lives per-thread in <see cref="NebulaProgress"/>). Five intel shards from five systems that touch the
    /// player's own deaths, and one capstone contract.</summary>
    public static readonly IReadOnlyList<NebulaFragment> Fragments =
    [
        // ── Intel shards — the corporate horror, gathered. Each is one system's piece. ──

        new("rebirth-glitch", "The glitch in the rebirth", NebulaSource.ResurrectionCard, false,
            "You wake at the clinic with the policy's cheer already playing — a new name on the license, a " +
            "new face in the mirror, the ship doesn't care — and for one flat second before the welcome " +
            "loops clean, the card reads a line it should not: RESTORE FROM PATTERN 40 · SUBSCRIBER LUCID · " +
            "DO NOT REVIVE ORIGINAL. Then it blinks back to \"Welcome back, Captain.\" You feel entirely " +
            "yourself. That is the part they charge for."),

        new("fine-print", "The fine print, read twice", NebulaSource.PosterFinePrint, false,
            "You have walked past the poster a hundred times — WE BRING YOU BACK MEANER, the hoards outlive " +
            "the hull, the policy outlives the captain. This time you read the bottom line, the grey one no " +
            "advertising should keep: \"Coverage is CONTINUOUS from first premium; a lapse forfeits the " +
            "insured PATTERN in perpetuity.\" Not the body. The pattern. It outlives the captain because it " +
            "is the thing they keep, and you are the thing they lend back."),

        new("adjuster-tell", "The adjuster's tell", NebulaSource.Adjuster, false,
            "The Nebula adjuster works the ports like the fences do, but sells the one thing you can't fence: " +
            "your afterlife. Deep in a drink they answer sideways. \"People think a policy's a spare life. " +
            "It's a lease, spacer. On the life you've already got.\" Pressed, quieter: \"I've filed the same " +
            "subscriber six times. Different faces, same number. Every one of them shook my hand certain they " +
            "were the first.\" Then they buy their own next round and stop talking."),

        new("collector-writ", "The collector's writ", NebulaSource.CollectorWrit, false,
            "The debt collectors were never only repo men — you get a look at the writ one carries and it is " +
            "not a cargo manifest. NEBULA MUTUAL · RECOVERY OF INSURED ASSET · the asset line does not name " +
            "the ship. It names a policy number and a phrase: PATTERN, DELINQUENT — RETURN TO ARCHIVE. They " +
            "are not chasing what you stole. They are collateral agents, and when a subscriber lapses, the " +
            "thing they repossess is the subscriber."),

        new("clinic-ledger", "The clinic's second page", NebulaSource.ClinicLedger, false,
            "The resurrection clinic hands you a bill; the ledger behind it has a second page you were not " +
            "meant to see. Your policy number is old — older than you. It carries entries you never made: " +
            "reads, refiles, a column headed PATTERN LUCIDITY trending down across dates that predate your " +
            "first death. Someone under your number has woken here before, more than once, and been read out " +
            "again each time, a little fainter. The archive keeps the tab open. It never closes a policy."),

        // ── The capstone. Not a rumor: the earned CONTRACT. Surfaces only once the rest is in hand. ──

        new("policy-terms", "The policy's true terms", NebulaSource.PolicyTerms, true,
            "Assembled, the pieces resolve into the clause the sales voice skips: the premium does not buy a " +
            "rebirth, it buys STORAGE — your pattern kept in Nebula's cold archive, lucid enough to stay a " +
            "valid backup, forever, or until you lapse and forfeit it. Each death spends a fresh copy that " +
            "wakes certain it is you; the original never leaves the dark. They built the archive from a rig " +
            "they did not invent, degraded from something that kept whole crews awake in far colder water. " +
            "You are not insured against death. You are filed under it. That was always the contract."),
    ];

    /// <summary>How many INTEL shards (non-key fragments) must be assembled before the capstone can be
    /// earned and the fine print is legible. Set below the full intel count on purpose: the horror does not
    /// demand a completionist sweep — enough pieces to see the shape is enough to earn the contract. The
    /// poster line alone is never enough; a lone adjuster's drink is never enough.</summary>
    public const int IntelNeededToUnlock = 4;

    /// <summary>The intel shards — every fragment that is not the capstone key. These are what
    /// <see cref="IntelNeededToUnlock"/> counts.</summary>
    public static IEnumerable<NebulaFragment> IntelFragments => Fragments.Where(f => !f.IsKey);

    /// <summary>The single capstone key (the policy terms) — the earned last piece that turns dread into
    /// knowledge. There is exactly one; the pool guarantees it and a test pins it.</summary>
    public static NebulaFragment KeyFragment => Fragments.Single(f => f.IsKey);

    /// <summary>The fragment with this id, or null if unknown — the tolerant lookup a loader uses so a vault
    /// carrying a since-renamed id simply drops it rather than throwing.</summary>
    public static NebulaFragment? ById(string id) => Fragments.FirstOrDefault(f => f.Id == id);

    /// <summary>True once every id in this pool is unique and exactly one fragment is the key — an authoring
    /// invariant the tests assert, exposed so any future editor of the pool can self-check.</summary>
    public static bool PoolIsWellFormed =>
        Fragments.Select(f => f.Id).Distinct().Count() == Fragments.Count &&
        Fragments.Count(f => f.IsKey) == 1;

    // ── The fine-print logic (pure predicates — the unlock HOOK, not the world delivery). ──

    /// <summary>How many intel shards this progress has assembled (the key never counts as intel).</summary>
    public static int IntelAssembled(NebulaProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return IntelFragments.Count(f => progress.Has(f.Id));
    }

    /// <summary>True once enough intel is assembled that the shape of the truth is visible and the capstone
    /// can be earned — the moment the world should offer the way to the policy terms (a later lane surfaces
    /// it as the <see cref="NebulaSource.PolicyTerms"/> piece). Does NOT itself mean the whole truth is
    /// known: see <see cref="KnowsTheTruth"/>.</summary>
    public static bool HasEnoughIntelToEarnTheContract(NebulaProgress progress) =>
        IntelAssembled(progress) >= IntelNeededToUnlock;

    /// <summary>True once the captain has read enough of the small print to be offered the contract — an
    /// alias with the fiction's name, for the readout copy. Same predicate as
    /// <see cref="HasEnoughIntelToEarnTheContract"/>.</summary>
    public static bool KnowsTheFinePrint(NebulaProgress progress) =>
        HasEnoughIntelToEarnTheContract(progress);

    /// <summary>
    /// The truth predicate (issue #422): does this thread's captain now KNOW what Nebula Mutual does with
    /// the continuous-mind tech? True only when the capstone contract is in hand AND the intel that
    /// legitimises it is assembled — the terms pasted from a cheat, with nothing behind them, are not
    /// knowledge; the contract has to be the one the pieces implied. This is the WHOLE gate for arc 2's own
    /// reveal. It is deliberately PURE and world-blind: it decides <i>whether</i> the truth is known, never
    /// delivers it. (The deepest reveal — that this and the ice moon are one story — is the separate
    /// <see cref="ArcConvergence"/> beat.)
    /// </summary>
    public static bool KnowsTheTruth(NebulaProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return progress.Has(KeyFragment.Id) && HasEnoughIntelToEarnTheContract(progress);
    }

    /// <summary>The corporation's name, kept here so the delivering lanes bind to one agreed string rather
    /// than a fresh literal — the underwriter on every poster and card.</summary>
    public const string Underwriter = "Nebula Mutual";

    /// <summary>Arc 2's own reveal sanity cost, as a HOOK value only (issue #422). Larger than
    /// <see cref="NerveModel.MonolithSightShock"/> — learning your resurrections are a lease on collateral
    /// is a heavy #391 throw. NOT wired here (the sanity/#226 lane owns <c>NerveModel</c> and will consume
    /// this when the reveal is built). The heaviest throw of all is the CONVERGENCE, whose shock lives in
    /// <see cref="ArcConvergence.ConvergenceSanityShockHook"/>.</summary>
    public const double TruthSanityShockHook = 30.0;
}

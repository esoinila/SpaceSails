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
public sealed record KaamosFragment(string Id, string Title, KaamosSource Source, bool IsKey, string Lore);

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
            "expects a ship to fill. The name means the polar night. Nobody at the Exchange will say whose."),

        new("cold-pod", "The cold supply pod", KaamosSource.DerelictPod, false,
            "Half-buried in the regolith, a supply pod that never made its run — hull frost-cracked, its " +
            "manifest slug still readable: CONSUMABLES, WINTERING CREW, 40 SOULS · DEST. KAAMOS · HOLD FOR " +
            "CYCLER WINDOW. The seals were never broken. Whatever it was carrying to the ice moon, the ice " +
            "moon went without it — and the pod was logged HELD, not lost. Someone chose not to send it."),

        new("vantar-log", "Vantar's wintering log", KaamosSource.LabLog, false,
            "A log salvaged from a sealed lab, the hand disciplined and then, later, not: \"The dark below " +
            "the ice is total and it is patient, and I have taught them to be the same. They do not sleep " +
            "through the night — they hold it, together, as one held breath. The winter does not kill what " +
            "refuses to be many.\" The last entries are dated long after his listed death. They are calm."),

        new("holders-tell", "The berth-holder's tell", KaamosSource.BarRumor, false,
            "The one who used to run the KAAMOS berth drinks alone and answers only sideways: \"You don't " +
            "file for that berth, spacer. You keep it. There's a difference, and I learned it late.\" Pressed, " +
            "quieter: \"It still calls the manifest in. Every window, right on the tick. Same forty names. " +
            "I stopped reading who was speaking them.\" Then the glass is empty and the conversation with it."),

        new("bought-coordinate", "The bought coordinate", KaamosSource.BoughtTip, false,
            "A round on the counter buys the rest of it: a coordinate off the ephemeris where the charts " +
            "just say ICE MOON — UNREACHABLE, and a date, and the word CYCLER. \"The window's real,\" they " +
            "say, pocketing the coin. \"Comes round rare. A ship that's on the board when it opens can ride " +
            "it in. Getting back out — that's not the part they sell tickets for.\" You have the where and the when."),

        // ── The capstone. Not a rumor: the earned KEY. Surfaces only once the rest is in hand. ──

        new("berth-code", "The KAAMOS berth code", KaamosSource.BerthCode, true,
            "Assembled, the pieces answer each other: the held pod's cycler window, Vantar's dates, the " +
            "holder's tick, the bought coordinate — one number falls out of them, the string the sealed " +
            "berth still listens for. It is not a password so much as a name the dark already knows. Enter " +
            "it on the board when the window opens and the berth stops being a place nobody files for. It " +
            "becomes a place expecting you. You could go to the ice moon now. That was always the danger."),
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
}

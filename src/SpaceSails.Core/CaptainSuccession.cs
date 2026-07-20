namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// THE INSURANCE CAPTAIN (Evening wind #20, owner 2026-07-18): "nerve already empty + more sanity
// damage → the captain goes crazy and dramatically exits the scene, and the piracy insurance issues a
// new captain :-D. Fail Forward, not game over — the run continues under a fresh name; the ledger, the
// ship and the hoards persist."
//
// This is the pure spine of that ruling — everything the client only DISPLAYS around:
//   • the OVERDRAW predicate — was the captain already empty when a qualifying sanity hit landed;
//   • the NEW-IDENTITY roll — a fresh seeded name + a face that is guaranteed to DIFFER from the one
//     that just walked into the dark (owner's "a new face in the mirror");
//   • the HISTORY append — the retired captain is kept on the thread so the roster still remembers who
//     held the license before ("under Capt. <name> until day N").
//
// Deterministic (determinism is law in Core): the successor is seeded off the thread's own id and the
// count of prior retirements, so the SAME universe always rebirths the SAME sequence of captains — no
// RNG, no clock. The registry persists the rolled identity onto the thread row (the #368 fields are
// editable data), and a later Touch preserves it, exactly like the born-on stamp.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>The pure succession rule (Evening wind #20): the overdraw predicate, the repeat-avoiding
/// new-identity roll, the retired-history append, and the house-voice copy the resurrection card and the
/// roster read. All deterministic — a test pins an exact successor for an exact thread.</summary>
public static class CaptainSuccession
{
    // ── The overdraw predicate ───────────────────────────────────────────────────────────────────────
    //
    // Owner's shape (Evening wind #20): the break is "nerve already EMPTY + MORE sanity damage". So the
    // gate is read on the nerve as it stood BEFORE the qualifying hit lands — a captain who bottoms out on
    // one touch survives to RUN; it is the NEXT qualifying hit taken while already empty that breaks them.
    // The catch-cadence debounce (client) gives a window to flee between the two. "Qualifying" is a big
    // FLAT shock (a Reever's hand, the monolith reveal, a horror beat) — the #384 seams — never a
    // diminishing sighting, so a wall of blips can never overdraw you; only skin and revelation do.

    /// <summary>Nerve at or under this sliver of the 0..<see cref="NerveModel.Max"/> gauge counts as
    /// "already empty" for the overdraw — a further qualifying hit at/under this breaks the captain. Sits
    /// under <see cref="DeathNarration.JoinedNerveSliver"/> so an overdraw death is ALWAYS eligible for the
    /// eerie "joined them" reading. FLAGGED for the owner's tuning.</summary>
    public const double EmptyThreshold = 2.0;

    /// <summary>Was the captain ALREADY empty when a qualifying sanity hit arrived — the Evening-wind #20
    /// overdraw? Read on the nerve BEFORE the hit is applied. Pure: bands the gauge into "empty → true"
    /// (bottomed out, another blow breaks you) versus "steady/mid → false" (the blow only frays or floors,
    /// never breaks). The client routes a true result into the shared BUSTED resurrection.</summary>
    public static bool OverdrawQualifies(double nerveBeforeQualifyingHit)
        => nerveBeforeQualifyingHit <= EmptyThreshold;

    // ── The new-identity roll ────────────────────────────────────────────────────────────────────────

    /// <summary>Roll a fresh captain identity for a succession seed, guaranteeing the FACE differs from the
    /// one just retired (owner's "a new face in the mirror"). The name comes from the same seeded roster as
    /// a minted captain (<see cref="Captains"/>), so successors stay house-flavoured; the avatar is stepped
    /// one off its derived value only if the seed happened to land on the previous face. Pure and
    /// deterministic — same previous-avatar + same seed → same successor.</summary>
    public static (string Name, int Avatar) NewIdentity(int previousAvatar, string successionSeed)
    {
        string name = Captains.Name(successionSeed);
        int avatar = Captains.AvatarIndex(successionSeed);
        if (avatar == previousAvatar)
        {
            avatar = (avatar % Captains.AvatarCount) + 1; // step to the next face so the mirror truly changes
        }

        return (name, avatar);
    }

    // ── The whole succession: roll + append, as one pure transform ───────────────────────────────────

    /// <summary>The seed a succession rolls from: the thread's own id plus which generation of captain this
    /// is (1st successor, 2nd, …), so each death in a universe rebirths a DIFFERENT deterministic captain,
    /// reproducibly. Pure — no clock, no RNG.</summary>
    public static string SeedFor(string threadId, int generation) => $"{threadId}|succ{generation}";

    /// <summary>Issue a new captain onto a thread (Evening wind #20): roll a fresh name + a differing face,
    /// and append the retiree to the thread's history so the roster still remembers them. Pure — returns the
    /// updated row; the registry persists it. The successor is seeded off the thread id and the generation
    /// (prior retirements + 1), so a universe's rebirth sequence is fully deterministic.</summary>
    public static GameThreadInfo Succeed(GameThreadInfo current, int retiredSimDay)
    {
        ArgumentNullException.ThrowIfNull(current);
        (string retiredName, int retiredAvatar) = Captains.For(current);
        int generation = current.Retired.Count + 1;
        (string newName, int newAvatar) = NewIdentity(retiredAvatar, SeedFor(current.Id, generation));

        var history = new List<RetiredCaptain>(current.Retired)
        {
            new(retiredName, Math.Max(0, retiredSimDay)),
        };

        return current with
        {
            CaptainName = newName,
            AvatarIndex = newAvatar,
            Retired = history,
        };
    }

    // ── The house voice ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The resurrection card's succession line (owner Evening wind #20 — the house voice): read
    /// under the brain-backup copy when the policy pays out with a new name on the license.</summary>
    public const string PolicyPayoutLine =
        "The policy pays out. A new name on the license, a new face in the mirror — the ship doesn't care.";

    /// <summary>The roster's compact "retired" line for one former captain — "under Capt. Mabel Vane until
    /// day 42". Strips the stored "Captain " title so the "Capt." abbreviation reads cleanly.</summary>
    public static string RetiredLine(RetiredCaptain retired)
    {
        ArgumentNullException.ThrowIfNull(retired);
        string bare = retired.Name.StartsWith("Captain ", StringComparison.Ordinal)
            ? retired.Name["Captain ".Length..]
            : retired.Name;
        return $"under Capt. {bare} until day {Math.Max(0, retired.SimDay)}";
    }
}

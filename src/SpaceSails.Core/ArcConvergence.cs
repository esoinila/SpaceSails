namespace SpaceSails.Core;

/// <summary>
/// THE CONVERGENCE (issue #422, the Expanse beat) — the pure, cross-arc predicate where the two story
/// rabbit holes turn out to be one. Owner (2026-07-20): "Kind of how in the Expanse the various characters
/// notice their rabbit holes converge." The player has been pulling two threads without knowing they are
/// tied: PROJEKTI KAAMOS (<see cref="KaamosLore"/>) — Dr. Vantar's ice-moon continuous mind, the sealed
/// berth, the same forty names — and NEBULA MUTUAL (<see cref="NebulaLore"/>) — the brain-backup insurance
/// that wakes a new captain every death. When both are far enough assembled, they resolve into a single
/// truth: Vantar's KAAMOS was the prototype archive, and Nebula's insurance is the same tech, scaled and
/// degraded and sold. The wintering mind under the ice and the cold vault that keeps bringing you back are
/// the same kind of thing — and a copy of YOU, filed under your policy number, is already down there.
///
/// <para><b>Pure and cross-arc.</b> This reads BOTH progress holders and is world-blind — it decides
/// <i>whether</i> the two mysteries have met, never delivers the reveal. It reads <see cref="KaamosProgress"/>
/// strictly READ-ONLY (through <see cref="KaamosLore"/>'s pure counts); it never mutates the KAAMOS arc, and
/// this lane touches no KAAMOS file. The one-time firing of the reveal is tracked on
/// <see cref="NebulaProgress.ConvergenceSeen"/> (this arc's holder, the natural home for a bit that belongs
/// to the whole thread), and the delivery — the loud line, the biggest #391 sanity throw — is a documented
/// follow-up for the wiring lane, exactly as the KAAMOS spine left its reveal a hook.</para>
///
/// <para><b>Its own bar, not either arc's unlock.</b> Convergence is deliberately a SEPARATE, joint
/// threshold: the player must have seen enough of BOTH shapes for the recognition to land. It does not
/// require either arc to be fully complete (you need not have reached Enceladus, nor hold the whole
/// contract) — noticing that the rabbit holes meet comes before finishing either dig. One arc alone, however
/// deep, never converges: that is the point, and the tests pin it.</para>
/// </summary>
public static class ArcConvergence
{
    /// <summary>How many KAAMOS intel shards must be assembled for the KAAMOS side of the convergence to be
    /// ready. Below the arc's own <see cref="KaamosLore.IntelNeededToUnlock"/> full sweep — the player needs
    /// enough of the ice-moon shape to recognise it echoed in their own deaths, not the finished dig.</summary>
    public const int KaamosSideThreshold = 3;

    /// <summary>How many NEBULA intel shards must be assembled for the Nebula side of the convergence to be
    /// ready. Symmetric with the KAAMOS side — enough of the resurrection truth to recognise the ice-moon in
    /// it, not the finished contract.</summary>
    public const int NebulaSideThreshold = 3;

    /// <summary>
    /// #422 · THE KAAMOS SIDE'S NAMED SHARD — the berth-holder's tell, one of the three the KAAMOS side
    /// counts, and the one the card QUOTES.
    ///
    /// <para><b>Why the bar names a shard and not only a number.</b> The card is a collision, not an
    /// explanation: it prints <see cref="KaamosLore.HolderConvergenceLine"/> and
    /// <see cref="NebulaLore.AdjusterConvergenceLine"/> unannotated and closes on
    /// <see cref="ConvergenceReveal"/> — <i>"You have been carrying both of these for a while."</i> A count
    /// alone cannot make that sentence true. The KAAMOS shards come from five different systems, and the
    /// plaque, the pod and the lab log are three of them: a captain could cross a bare 3-intel bar having
    /// never once sat down with the berth-holder, and the card would then hand them a line they had never
    /// heard while claiming they had been carrying it. That is the house's named bug class — the sim doing
    /// one thing while a sentence reports another — so the bar names the shard the card quotes.</para>
    ///
    /// <para><b>The bar is still 3 + 3.</b> This does not raise the threshold (owner ruling 2026-09-17 kept
    /// option B's bar and rejected option A's): it says which ONE of the three has to be in hand. The
    /// Nebula side needs no widening — <c>adjuster-tell</c> is already inside its first three — and on the
    /// KAAMOS side the effect is only that the tell must be one of your three.</para>
    /// </summary>
    public const string KaamosSideShard = "holders-tell";

    /// <summary>#422 · THE NEBULA SIDE'S NAMED SHARD — the adjuster's tell, the line the card quotes on the
    /// lower half. Same reason as <see cref="KaamosSideShard"/>: the closing line says the captain has been
    /// carrying both of these, so both must be in the ledger when the card opens.</summary>
    public const string NebulaSideShard = "adjuster-tell";

    /// <summary>True once the KAAMOS side is far enough along to converge — a pure, READ-ONLY read of the
    /// ice-moon progress (never mutates it). Enough intel AND the one shard the card quotes
    /// (<see cref="KaamosSideShard"/>).</summary>
    public static bool KaamosSideReady(KaamosProgress kaamos)
    {
        ArgumentNullException.ThrowIfNull(kaamos);
        return KaamosLore.IntelAssembled(kaamos) >= KaamosSideThreshold && kaamos.Has(KaamosSideShard);
    }

    /// <summary>True once the NEBULA side is far enough along to converge — enough intel AND the one shard
    /// the card quotes (<see cref="NebulaSideShard"/>).</summary>
    public static bool NebulaSideReady(NebulaProgress nebula)
    {
        ArgumentNullException.ThrowIfNull(nebula);
        return NebulaLore.IntelAssembled(nebula) >= NebulaSideThreshold && nebula.Has(NebulaSideShard);
    }

    /// <summary>
    /// THE convergence predicate (issue #422, deliverable 3): have the two rabbit holes met? True only when
    /// BOTH arcs have crossed their joint threshold — the KAAMOS side AND the Nebula side, each far enough
    /// that the player has seen its shape. One arc alone, however deep, is never enough (a captain who has
    /// solved the whole ice moon but never questioned their own deaths has not converged, and vice versa):
    /// the recognition is the two shapes side by side. Pure and deterministic — the same two progresses
    /// always give the same answer, no clock, no RNG. Reads KAAMOS strictly read-only.
    /// </summary>
    public static bool HasConverged(KaamosProgress kaamos, NebulaProgress nebula) =>
        KaamosSideReady(kaamos) && NebulaSideReady(nebula);

    /// <summary>True when the convergence is READY to reveal but has not yet fired for this thread — the edge
    /// the wiring lane watches to deliver the one-time beat. Once <see cref="NebulaProgress.MarkConvergenceSeen"/>
    /// records it, this goes false forever on the thread even though <see cref="HasConverged"/> stays true.</summary>
    public static bool ConvergenceRevealPending(KaamosProgress kaamos, NebulaProgress nebula)
    {
        ArgumentNullException.ThrowIfNull(nebula);
        return HasConverged(kaamos, nebula) && !nebula.ConvergenceSeen;
    }

    /// <summary>
    /// The CONVERGENCE reveal — and it is now ONE LINE, because the card stopped explaining (owner ruling
    /// 2026-09-17, the #422 story pass's option B).
    ///
    /// <para><b>What it used to be.</b> Eight sentences of third-person exposition that spent every secret
    /// both arcs still had to give: that the resurrection is a fresh copy which wakes certain it was always
    /// you, that Nebula keeps the ORIGINAL filed in the cold archive from your first premium, and that the
    /// archive is awake. All three are <c>NebulaArc.md</c> §2's fine print, and two of them are the
    /// <c>policy-terms</c> capstone's own reveal — which the card, firing at a strictly lower bar, reached
    /// first on every path, leaving arc 2's capstone to recap what the player had already been told. It
    /// also broke the house's loudest rule twice over: nobody spoke it, so it was the GAME confirming the
    /// plot in a full-screen modal, and the way out of the modal editorialised ("…sit with that").</para>
    ///
    /// <para><b>What it is now.</b> The card is a COLLISION. It sets the berth-holder's line
    /// (<see cref="KaamosLore.HolderConvergenceLine"/>) above the adjuster's
    /// (<see cref="NebulaLore.AdjusterConvergenceLine"/>), unannotated — no names, no labels, no joining
    /// sentence — and closes on this one line, which is not an explanation. Both sentences are already in
    /// the captain's ledger when it opens (<see cref="KaamosSideShard"/>, <see cref="NebulaSideShard"/>),
    /// so no fact is withheld and none is confirmed: the arithmetic is the player's. Each arc's capstone
    /// keeps its own reveal to give.</para>
    ///
    /// <para>Authored Core copy, never a fragment; delivered once per thread, under the biggest #391 throw.
    /// Kept here so the fiction lives with the predicate.</para>
    /// </summary>
    public const string ConvergenceReveal = "You have been carrying both of these for a while.";

    /// <summary>#528 · THE PLATE. The biggest reveal in the game was a text div, while a routine collector
    /// shakedown got a painted portrait — the exact inversion #528 was filed about.
    ///
    /// <para>It is deliberately a PLATE and not an illustration of the paragraph above it. The card's copy
    /// may still change (the arc's own passes are live); an image that depicted the text would have to be
    /// repainted every time a sentence moved, and worse, it would say the thing out loud. What it shows is
    /// the shape of the reveal: two entirely different filing systems — one warm, wooden, papered, a
    /// bureaucracy; one cold, steel, frosted, a cold store — running toward each other down one aisle and
    /// meeting at a single shared cabinet, the joinery seamless, as though it had always been one building.
    /// Two mysteries; one truth. Nobody in the room to react to it, because a figure looking would be the
    /// game telling the captain how to feel.</para>
    ///
    /// <para><c>onerror</c>-hides like every other slot: a missing file leaves the card exactly as it was
    /// before this line existed.</para></summary>
    public const string ArtFile = "art/convergence.jpg";

    /// <summary>The convergence's sanity cost, as a HOOK value only (issue #422: "a big #391 sanity throw",
    /// the heaviest in the game). Strictly greater than <see cref="KaamosLore.RevealSanityShockHook"/> (the
    /// KAAMOS 40) and than <see cref="NebulaLore.TruthSanityShockHook"/> — the two reveals landing as one is
    /// heavier than either alone. NOT wired here (the sanity/#226 lane owns <c>NerveModel</c> and consumes
    /// this when the beat is built); named in this lane's own file so the number is authored where the fiction
    /// lives and nothing in the sanity Core is touched.</summary>
    public const double ConvergenceSanityShockHook = 64.0;
}

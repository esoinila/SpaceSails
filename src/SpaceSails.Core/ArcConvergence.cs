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

    /// <summary>True once the KAAMOS side is far enough along to converge — a pure, READ-ONLY read of the
    /// ice-moon progress (never mutates it).</summary>
    public static bool KaamosSideReady(KaamosProgress kaamos)
    {
        ArgumentNullException.ThrowIfNull(kaamos);
        return KaamosLore.IntelAssembled(kaamos) >= KaamosSideThreshold;
    }

    /// <summary>True once the NEBULA side is far enough along to converge.</summary>
    public static bool NebulaSideReady(NebulaProgress nebula)
    {
        ArgumentNullException.ThrowIfNull(nebula);
        return NebulaLore.IntelAssembled(nebula) >= NebulaSideThreshold;
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

    /// <summary>The CONVERGENCE reveal — the one-time line the wiring lane delivers when the two rabbit holes
    /// meet (issue #422). The Expanse beat, verbatim: the moment the player's own resurrections and the sealed
    /// ice-moon berth resolve into a single truth, recontextualising every death they have had. Authored Core
    /// copy, never a fragment (no shard states it); delivered once, under the biggest #391 throw. Kept here so
    /// the fiction lives with the predicate, and the sanity/#226 lane and the world lane bind to one agreed
    /// string.</summary>
    public const string ConvergenceReveal =
        "The two threads pull taut and cross. The berth nobody files for and the policy you can never let " +
        "lapse were always the same knot: Vantar taught a lattice to keep whole crews awake in the dark, and " +
        "Nebula bought the trick and sold it cheap, and both the ice-moon and the cold archive that keeps " +
        "bringing you back are the same held breath. The wintering mind remembers Vantar — and it knows your " +
        "policy number, because a copy of you has been filed down there in the sunless water since your first " +
        "premium, waking every so often, certain it is the one who was here first. Every death you have died " +
        "was a withdrawal. The same forty names, the same lucid dark. You did not find two mysteries. You " +
        "found out where you go when you die, and that it has been waiting for you to arrive in person.";

    /// <summary>The convergence's sanity cost, as a HOOK value only (issue #422: "a big #391 sanity throw",
    /// the heaviest in the game). Strictly greater than <see cref="KaamosLore.RevealSanityShockHook"/> (the
    /// KAAMOS 40) and than <see cref="NebulaLore.TruthSanityShockHook"/> — the two reveals landing as one is
    /// heavier than either alone. NOT wired here (the sanity/#226 lane owns <c>NerveModel</c> and consumes
    /// this when the beat is built); named in this lane's own file so the number is authored where the fiction
    /// lives and nothing in the sanity Core is touched.</summary>
    public const double ConvergenceSanityShockHook = 64.0;
}

using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #804 · THE ROUNDS, ON THE FLOOR. The brain is <see cref="PatrolBeat"/> in Core and every number, every
/// stop and every sentence below comes from it; this file is the walking, the knowing and the telling.
///
/// <para><b>They walk the A* the audits walk.</b> Owner, #729: <i>"maybe the guards can use A* also so they
/// do not come off as reevers or kind of crazy scary."</i> A leg of a round is planned with
/// <see cref="AutoWalk"/> over <c>DeckReachability</c> — the same route machinery the click-to-walk cheat
/// hands the captain, over the same collision field the captain's own stepper asks — and spent through
/// <see cref="SurfaceCollision.Slide"/> at a person's gait. A thing that finds doorways is somebody on a
/// payroll, and the player is meant to read that off the motion before any card says so.</para>
///
/// <para><b>What the captain knows and what the guard knows are different, and the difference is the
/// feature.</b> A marker is drawn only inside the captain's own sightline; outside it the motion tracker
/// hears a mover through the rock (the instrument is untouched — a guard walks, so a guard is a contact),
/// and closer-but-unseen there are boots. The guard registers the captain at a THIRD of the eye's reach, so
/// there is a real window in which you can see them and they cannot see you. That window is the whole
/// stealth verb: watch the round, wait, step out behind it.</para>
///
/// <para><b>A sighting still cannot start a chase, and that is still the default.</b> A round that registers
/// somebody standing there hails, walks over, reads the wallet, and at worst walks you back to the lift.
/// That is the owner's original law and it is untouched.</para>
///
/// <para><b>#835 · THE OTHER BRANCH, WHICH IS EARNED AND NEVER AMBIENT.</b> Owner, reversing his own
/// standing law with the implementation named: <i>"they need to catch us .... like reevers :-D we could use
/// that code :-D"</i> — and, in the same breath, <i>"just no damage by default :-D"</i>. So there are now
/// exactly three doors into a run (<see cref="PatrolBeat.Provocation"/>): walking off on a hail for the
/// SECOND time in a watch, being watched taking a hasp off with a gun, and having been booked
/// <see cref="PatrolBeat.EscortsAWatchAllows"/> times already. Every one of them is a thing the captain did,
/// and the paragraph above is what happens on every floor where none of them has. He calls it in before he
/// moves (<see cref="Patrol.TheRadioCall"/>), comes on the Old Ones' own homing step at a person's gait
/// (<see cref="Patrol.RunAfterTheCaptain"/> → <c>ReeverChase.Step</c>), and ends either with a hand on your arm
/// (<see cref="Patrol.HeHasYou"/>) or standing in a corridor watching you go (<see cref="Patrol.HeLosesYou"/>). He is
/// never removed from the floor by either. <b>Nothing on this road touches the captain's health</b> — there
/// is no <c>HitsTaken</c> in this family and there never will be — and the ladder it feeds is the escort you
/// already know, which past the threshold simply keeps going up (<see cref="Patrol.TheKickOut"/>).</para>
///
/// <para><b>#833 · AND EVERY BEAT OF IT IS WALKED.</b> Two things in this file used to be sentences over
/// placements, and the owner caught both in one evening on B2. The card went up the frame he NOTICED you, at
/// up to nine deck units — <i>"I think the guard should approach us when it does the inspection"</i> — so
/// there is now a HAIL and an APPROACH between the notice and the read (<see cref="TheHail"/>,
/// <see cref="Patrol.WalkUpToTheCaptain"/>), the captain's controls stay free through all of it, and walking away is
/// allowed. And the escort was <c>StandCaptainAt</c>: <i>"how did I jump to elevator there?"</i> — so the
/// walk back is now a walk (<see cref="Patrol.WalkTheEscort"/>), he plans the route himself, the captain is walked
/// at his shoulder through his own collision, and both of them are moving contacts on the fan the whole way.
/// The one placement left is behind a caption that ADMITS it is a cut.</para>
///
/// <para><b>One stepper.</b> The round, the walk-up and the escort all spend their frame through
/// <see cref="Patrol.SpendTheStride"/> — the captain's own sub-stepper, once. #832 was paid for by a copy of that
/// loop drifting from its original by one epsilon; three copies of it would have been three chances to do
/// that again.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>
    /// #870 lane 6′b · THE ROUND'S OWN STATE, IN ONE OBJECT (<c>Pages/Patrol/Patrol.cs</c>).
    ///
    /// <para>Twenty-two loose fields became one. <b>#870 lane 6′c · AND NOW THE VERBS TOO</b> — putting men
    /// on the floor, walking them, hailing, reading a wallet, running, walking a captain out — every one of
    /// them lives on <c>Patrol</c>'s own partials beside its state (<c>Pages/Patrol/Patrol.*.cs</c>). What a
    /// verb still needs from the page it is walked on is <see cref="IPatrolHost"/>, and that interface is
    /// the whole of the coupling: twenty-one members, written down, and it may only shrink.</para>
    ///
    /// <para><b><c>readonly</c>, and never re-assigned.</b> Leaving a floor EMPTIES the round
    /// (<see cref="SpawnPatrolFor"/>); it does not swap in a different one. A second <c>Patrol</c> would be
    /// a second answer to <i>who is walking this floor</i>, which is this repo's first named bug class
    /// aimed at a rota. There is a guard fact for exactly that.</para>
    ///
    /// <para>#870 lane 6′c · It is BUILT IN THE CONSTRUCTOR now rather than at its declaration, and that is
    /// a language rule rather than a design change: the round is handed the page it walks on, and an
    /// instance field initialiser may not name <c>this</c>. Still one round, still assigned exactly once.</para>
    /// </summary>
    private readonly Patrol _patrol;

    // ── #870 lane 6′a/6′b · WHAT THE REST OF THE PAGE MAY ASK THE ROUND ────────────────────────
    //
    // What the rest of the client actually wanted was never a field — a bench wants to know who is walking
    // the floor, a bin whether the rota has eyes on the captain, a boot-time query parser to force a round
    // onto a floor that rolled none. Those are questions, and this is where the round answers them. Each
    // one names the site that asked for it, so the day a member has no asker left it can be deleted rather
    // than inherited.
    //
    // FIFTEEN OF THEM ARE ONE-LINE FORWARDERS ONTO <see cref="Patrol"/>, in the block at the bottom of this
    // page. 6′b said 6′c would delete that block and point the callers at the host surface instead. IT DOES
    // NOT, and the reason is the measurement: all fifteen are asked for BY NAME from nine files outside this
    // family (Map.razor, Map.Vault.cs, Map.Bench.cs, Map.Bin.cs, Map.SweepTeam.cs, Map.Cubicle.cs,
    // Map.Sim.World.cs, Map.Sim.Cancel.cs, Map.Surface.Cheats.cs), and IPatrolHost is the door the ROUND
    // reaches the page through — not a door the page reaches the round through. Deleting these would have
    // rewritten seventeen call sites in files this lane has no business in, to save fifteen lines that each
    // prove something: that nothing outside the family gained a reach it did not have.

    /// <summary>How many figures a patrol may need drawing. The band in the droid buffer, stated once.</summary>
    private const int PatrolBand = PatrolBeat.MostOnAFloor;

    // ── #870 lane 6′c · AND WHAT THE PAGE ITSELF STILL CALLS THE ROUND BY NAME ──────────────────
    //
    // The three verbs the rest of the page drives the round with. Every one was MEASURED before it was
    // kept — the callers are named in each docblock, one file at a time — and a verb with no caller
    // outside this family kept no forwarder at all, which is why Map.Patrol.Round.cs and
    // Map.Patrol.Escort.cs are gone from this directory entirely: nothing outside the round has ever
    // asked it to walk a leg or to walk a captain out.

    /// <inheritdoc cref="Patrol.TheRoundHasEyesOnYou"/>
    private bool TheRoundHasEyesOnYou(IReadOnlyList<SurfaceCollision.Segment> sight) =>
        _patrol.TheRoundHasEyesOnYou(sight);

    // #715 · THE ROUND IS HANDED THE BOOK, NOT A HOST MEMBER. The illegal-heat meter lives in `_contacts`
    // and the round needs it twice — to know where a watch's patience starts at an outfit that remembers this
    // captain, and to bank the two crossings a round can be. Both come in as an ARGUMENT on the two calls
    // that already cross this seam, so `IPatrolHost` keeps its twenty-one members and the round keeps its
    // twenty-two fields: nothing new was stored anywhere, which is why #905's thirty fingerprints do not
    // move.

    /// <inheritdoc cref="Patrol.SpawnPatrolFor"/>
    private void SpawnPatrolFor(SurfaceExcursion ex) => _patrol.SpawnPatrolFor(ex, _contacts);

    /// <inheritdoc cref="Patrol.AdvancePatrol"/>
    private void AdvancePatrol(double dtRealSeconds) =>
        _patrol.AdvancePatrol(dtRealSeconds, _contacts, SimTime);

    // ── #870 lane 6′b · THE FIFTEEN FORWARDERS, AND WHEN THEY GO ────────────────────────────
    //
    // Every one of the fifteen questions the round answers moved onto Patrol whole; every one of them is
    // ALSO called by name from a file outside this family (measured, one caller at a time — the table is in
    // the PR body). So each keeps its old spelling and its old accessibility here, which is what proves
    // nothing outside the family gained a reach it did not have. 6′c deletes this block and points those
    // callers at the host surface instead; the guard's own fact names that as the clause to relax, rather
    // than leaving a row to be quietly deleted to make a sweep go green.

    /// <inheritdoc cref="Patrol.CaptainIsUnderEscort"/>
    private bool CaptainIsUnderEscort => _patrol.CaptainIsUnderEscort;

    /// <inheritdoc cref="Patrol.TheRoundOnFoot"/>
    private IReadOnlyList<Guard> TheRoundOnFoot => _patrol.TheRoundOnFoot;

    /// <inheritdoc cref="Patrol.ForceTheRoundsTo"/>
    private void ForceTheRoundsTo(int rounds) => _patrol.ForceTheRoundsTo(rounds);

    /// <inheritdoc cref="Patrol.TheQueryHasForcedARound"/>
    private bool TheQueryHasForcedARound => _patrol.TheQueryHasForcedARound;

    /// <inheritdoc cref="Patrol.ForceARoundIfNoneAsked"/>
    private void ForceARoundIfNoneAsked() => _patrol.ForceARoundIfNoneAsked();

    /// <inheritdoc cref="Patrol.MintTheSitePassAtTheLanding"/>
    private void MintTheSitePassAtTheLanding() => _patrol.MintTheSitePassAtTheLanding();

    /// <inheritdoc cref="Patrol.TheSitePassIsMintedAtTheLanding"/>
    private bool TheSitePassIsMintedAtTheLanding => _patrol.TheSitePassIsMintedAtTheLanding;

    /// <inheritdoc cref="Patrol.TheNextHideGetsItsOwnLine"/>
    private void TheNextHideGetsItsOwnLine() => _patrol.TheNextHideGetsItsOwnLine();

    /// <inheritdoc cref="Patrol.EverybodyForgetsTheCatch"/>
    private Guard? EverybodyForgetsTheCatch() => _patrol.EverybodyForgetsTheCatch();

    /// <inheritdoc cref="Patrol.ThePaperInYourHandIs"/>
    private bool ThePaperInYourHandIs(Satchel.Item paper) => _patrol.ThePaperInYourHandIs(paper);

    /// <inheritdoc cref="Patrol.TheBookOn"/>
    private string TheBookOn(Satchel.Item paper, string bodyId) => _patrol.TheBookOn(paper, bodyId);

    /// <inheritdoc cref="Patrol.YourPaperTrail"/>
    private IReadOnlyList<WalletChoice.Shown> YourPaperTrail => _patrol.YourPaperTrail;

    /// <inheritdoc cref="Patrol.ForgetThePaperTrail"/>
    private void ForgetThePaperTrail() => _patrol.ForgetThePaperTrail();

    /// <inheritdoc cref="Patrol.RestoreAPaperTrailRow"/>
    private void RestoreAPaperTrailRow(WalletChoice.Shown row) => _patrol.RestoreAPaperTrailRow(row);

    /// <inheritdoc cref="Patrol.CloseTheWalletFan"/>
    private void CloseTheWalletFan() => _patrol.CloseTheWalletFan();
}

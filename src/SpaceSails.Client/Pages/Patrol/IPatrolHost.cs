using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #870 lane 6′c — WHAT A ROUND NEEDS FROM THE PAGE, WRITTEN DOWN.
///
/// <para>6′a made the rest of the client stop reading the round's twenty-two fields raw. 6′b gathered the
/// twenty-two, and every question that is a pure function of them, onto <c>Map.Patrol</c>. This lane moves
/// the VERBS — putting men on a floor, walking them, hailing, reading a wallet, running, walking a captain
/// out, throwing one back at the sky — onto the same object, and <b>this interface is the whole of what they
/// still reach back to the page for</b>.</para>
///
/// <h3>The interface IS the finding</h3>
///
/// <para>The round used to be six partials of <see cref="Map"/>, which meant it could reach anything the
/// page had: every field, every private verb, every dev cheat, with nothing written down and nothing to
/// argue with. Six issues in a fortnight (#804/#821/#831/#833/#835/#836) landed in it, and each crew had to
/// read all six files to know what one frame did. <b>The coupling was real and it was invisible.</b> It is
/// twenty-one members, and now it is a list.</para>
///
/// <para><b>It may only shrink.</b> <c>ThePatrolKeepsItsOwnStateTests</c> asserts the count and writes it
/// into its own failure message — a ratchet, exactly like #870's size gate and the seat's before it: taking
/// a member off is a good day and the number comes down with it; adding one is a lane of its own, argued for
/// in a PR body, because it is the round asking the page for something new.</para>
///
/// <h3>What is deliberately NOT here</h3>
///
/// <para><b>The renderer's cue.</b> <c>RendererInterop.PlayCue</c> looks like a page service and is not one:
/// it is an <c>internal static</c> of this assembly, so every moved <c>PlayCue("blip")</c> travels
/// character-for-character and the round does not owe the page a sound. 6c found this one building along.</para>
///
/// <para><b>Three <c>const</c>s of the page, and a constant is not a collaborator.</b>
/// <c>MaxSurfaceStepSeconds</c> (the frame clamp every surface stepper obeys), <c>AutoWalkSubStepsPerFrame</c>
/// (the captain's own sub-stepper's bound, which #833's one stepper is a copy of) and <c>PatrolBand</c> (how
/// many figures a round may need drawing, which the surface's slot arithmetic also reads) are compile-time
/// literals the compiler inlines: there is no page object involved at runtime and no state anybody can reach
/// through them. They are in scope inside <c>Patrol</c> for exactly the reason <see cref="Guard"/> and
/// <see cref="SurfaceExcursion"/> are — the round is nested — and their declarations did not move, so the
/// lines that read them are byte-identical to the lines that always read them.</para>
///
/// <para><b>The two questions the round is a CALLER of rather than a part of.</b>
/// <see cref="TheCubicleTheCaptainIsShutIn"/> is a door — it reads the ring's stalls and the excursion's own
/// set of shut ones — and <see cref="NameOnYourOwnPapers"/> is an identity, read off the captain thread the
/// game is being played on. Both stayed on <see cref="Map"/> and are asked for as ANSWERS, which is why each
/// is one member here instead of the two page members and the geometry sweep it is made of. That technique
/// is the whole of how this number is kept small, and it is the thing the next crew will need.</para>
///
/// <para><b>Anything with an opinion about a round.</b> Nothing on here decides who walks, what a hail costs
/// or where an escort ends. Every one of these is a thing the PAGE owns and the round borrows: a coordinate,
/// a pocket, a sentence, a ride.</para>
///
/// <h3>Why it is nested, like <c>Patrol</c> before it</h3>
///
/// <para><see cref="SurfaceExcursion"/> is a <c>private sealed</c> nested type of <see cref="Map"/>, and
/// three members here name it — a round is a thing in a building and there is no building without a landing.
/// A top-level interface would have had to widen the excursion to <c>internal</c>, publishing the whole of a
/// landing to the client assembly on the lane whose point is narrowing what one round can reach. So the
/// interface lives inside the page exactly as the round does, and <see cref="Map"/> implements it by name:
/// <c>public partial class Map : Map.IPatrolHost</c>. Nothing about the surface changes with where it is
/// declared, and the only thing that can see it is the family it was written for.</para>
/// </summary>
public partial class Map
{
    /// <inheritdoc cref="IPatrolHost"/>
    private interface IPatrolHost
    {
        // ── WHERE THE CAPTAIN IS, AND WHAT HE IS STANDING ON ──────────────────────────────────────────

        /// <summary>The excursion under way, or null on the ship. <c>AdvancePatrol</c> opens with it and so
        /// does the run's own gate, the wallet fan and the droid filler: a round is a thing on a floor of a
        /// building, and there is no floor without a landing.</summary>
        SurfaceExcursion? Surface { get; }

        /// <summary>Where the captain's body is. Read by nearly every verb here — the notice, the hail, the
        /// approach, the run's homing step, the sightline, the knock's reach — and WRITTEN by exactly one:
        /// #833's escort, which walks the captain a pace ahead of the guard (#804) through
        /// <see cref="DeckPlan"/>'s own <c>Move</c> rather than placing him.</summary>
        double AvatarX { get; set; }

        /// <inheritdoc cref="AvatarX"/>
        double AvatarY { get; set; }

        /// <summary>Which way the captain is facing — SET only, and by one verb: the escort turns him along
        /// the walk he is being taken on. The round never asks which way he is looking; that is the page's
        /// business and a guard's eye is answered by <c>PatrolBeat.Notices</c> instead.</summary>
        double AvatarHeading { set; }

        /// <summary>The deck the captain is walking on, for two things and two only: the collision field
        /// every leg, approach, run and escort is spent against (<c>CollisionField</c> — the captain's own
        /// walls, so a door shut for him is a wall for the man behind him by construction), and
        /// <c>Move</c>, the one primitive a body is ever stepped by.</summary>
        DeckPlan DeckPlan { get; }

        /// <summary>What a sightline is stopped by on this floor. Asked once a frame by <c>AdvancePatrol</c>
        /// and again by #835's <c>SomebodySawThat</c> — the same list the captain's own markers are drawn
        /// through, so what a guard can see and what a player is shown cannot come apart.</summary>
        IReadOnlyList<SurfaceCollision.Segment> SightBlockers();

        /// <summary>#793 · Is the captain sat on a bench out in the open? The one question #793's hold is
        /// asked with (<c>FootTail.MustHold</c>): a tail that has been made by somebody stopping cannot walk
        /// on past them. A seat is the SEAT family's business, so the round asks for the answer.</summary>
        bool SeatedOnABenchInTheOpen { get; }

        /// <summary>#821 · Which cubicle the captain is shut into, or null — the ANSWER, not the machinery.
        ///
        /// <para>It is a door's question: the ring's own stalls (<c>CubicleAround</c>) crossed with the
        /// excursion's own set of shut ones, and a second opinion about either would be a second answer to
        /// whether the captain is hidden at all. Asking for it costs one member here instead of the stall
        /// sweep and the page's cubicle cache the round would otherwise have to reach for.</para></summary>
        (RingOffice.Stall Cell, string Key)? TheCubicleTheCaptainIsShutIn(SurfaceExcursion ex);

        // ── WHAT THE CAPTAIN HAS ON HIM, AND WHO HE SAYS HE IS ────────────────────────────────────────

        /// <summary>#836 · The satchel, which is the wallet: what papers there are to fan, which one is
        /// still held at the read (<c>WalletChoice.StillHeld</c>), where the site's own pass is put when it
        /// is issued and where it is taken from when the captain is thrown out. Settable for those two,
        /// because Core's satchel is a value and adding to it makes a new one.</summary>
        List<Satchel.Item> Satchel { get; set; }

        /// <summary>#836 · The name on your own papers, spelled the way the rota spells it — the ANSWER
        /// again. It is read off the captain THREAD the game is being played on, which is the page's own
        /// identity system and two page members deep; a renamed captain's wallet renames itself with them,
        /// and the round never has to know how.</summary>
        string NameOnYourOwnPapers { get; }

        // ── WHAT IS IN FRONT OF THE CAPTAIN ───────────────────────────────────────────────────────────

        /// <summary>The card standing in front of the captain, or null. Read as a GATE by everything that
        /// wants to say something (#777: a challenge behind a backdrop is a challenge nobody read), and
        /// written by the two verbs that raise one — the wallet read and the hand on your arm.</summary>
        DeckPlan.ConsoleSpot? ViewObject { get; set; }

        // ── SAYING IT, AND WRITING IT DOWN ────────────────────────────────────────────────────────────

        /// <summary>The one line slot (#774). Every sentence this family has goes through it: the boots, the
        /// hail, the knock, the pumps, the walked-away, the lost-you, the doors closing.</summary>
        void ShowPulseMessage(string message, PulseRank rank = PulseRank.Status);

        /// <summary>…and the same sentence into the event log, beside it, every time.</summary>
        void LogAutopilotEvent(string text);

        /// <summary>The field book. #836's paper trail writes a line per read, and the escort, the kick-out
        /// and the revoked pass each write their own.</summary>
        void FileNote(string text, string glyph);

        /// <summary>The nerve, spent through the system that owns it: one pip for being asked and unable to
        /// answer, one TOUCH pip for a hand closing on your arm. Nothing in this family reads or writes
        /// health, and there is no member here that would let it.</summary>
        void ApplyNerveShock(double rawAmount, string label);

        /// <summary>Mark the rolling autosave dirty — every arm of a challenge ends in one.</summary>
        void RequestVaultSave();

        // ── MOVING THE CAPTAIN, AND THE FLOOR UNDER HIM ───────────────────────────────────────────────

        /// <summary>The one placement the sim ever puts the captain through (#681) — used by exactly one
        /// verb, #833's honest jump-cut, which is behind a caption that ADMITS it is a cut.</summary>
        void StandCaptainAt(double x, double y, string where);

        /// <summary>Take the captain's hands off the controls for the walk out — including any route he had
        /// clicked, which would otherwise walk him out from under the escort.</summary>
        void CancelAutoWalk(bool tellThem);

        /// <summary>…and keep the page's own ashore flag honest while the escort walks him, since the escort
        /// steps the body itself rather than going through the captain's stepper.</summary>
        void RefreshAshore();

        /// <summary>#835 · The ONE transition this game has between a floor and the regolith, the same one
        /// the panel's SURFACE row presses. The kick-out is a ride, not a placement.</summary>
        void RideTheLiftTo(SurfaceExcursion ex, int level);

        /// <summary>…and one rebuild of the surface deck, which is what takes the KICKED OUT plate down
        /// again when its clock runs out.</summary>
        void RebuildSurfaceDeck();
    }
}

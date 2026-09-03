using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #1074 beat 2 · THE PRESERVATION ZONE — the client half. Core carries the whole argument (see
// PreservationZone.cs and the fence in MoonSurface.BuildLayout); this file owns two things and nothing else:
// the register that rides the vault, and the ONE moment the event is evaluated.
//
// THERE IS NOTHING ELSE TO OWN, and less than beat 1 had. The zone is told entirely by a rail round a shed
// and one notice at its gate, both drawn by the deck builder that has drawn every wall and every ground
// label on a landing site since #313. No card, no pulse of its own, no beat, no nerve shock, no marker,
// nothing on the wire — and no new mechanic at all: the halls stay where they are, the shaft stays sealed by
// the order, and the site simply never comes back out of care.
public partial class Map
{
    /// <summary>#1074 · The grounds the Authority has fenced, signed and put under study. Persisted
    /// per-universe in the vault's ProgressSection beside <c>_hallsOpened</c>, <c>_hallsBuried</c>,
    /// <c>_hallsDeclined</c> and <c>_hallsStopped</c>.
    ///
    /// <para>It rides the same file as <c>_hallsStopped</c> for a reason harder than either of them alone:
    /// a zone stands on a CLOSED working, so a save that remembered the fence and forgot the order would
    /// come back to a site fenced against a shaft that was open again — the picture and the building
    /// disagreeing about the same place, which is this project's third named bug class.</para></summary>
    private IReadOnlyList<string> _hallsPreserved = [];

    /// <summary>#1074 · <c>/map?preserved=1</c> — see the query parser. It is <c>?stopped=1</c>'s next
    /// shift: the same rock, the same ground handed to the office by the split, opened far enough back that
    /// the order and the fence both land on the way down.</summary>
    private bool _preservedCheat;

    /// <summary>#1074 · The register as the vault's own rows, or null while nothing is under study.
    ///
    /// <para>Null-while-empty is the #1057/#1072/#1066/#677/#1063/#1068/#1074 law and it is here for their
    /// exact reason: the checksum is taken over the payload, so an eager <c>"hallsPreserved": []</c> on
    /// every save would change the digest of every vault ever written and hang the 📛 tampered marker on an
    /// honest voyage. No window rides along, for <c>StopRows</c>'s reason — nothing about a zone is CHOSEN,
    /// so there is nothing here for a number to keep stable.</para></summary>
    private IReadOnlyList<string>? PreserveRows() =>
        _hallsPreserved.Count > 0 ? [.. _hallsPreserved] : null;

    /// <summary>#1074 · …and back off them on load, <b>and installed in the same breath</b>.
    ///
    /// <para>The two halves are one method on purpose. The register has exactly ONE writer into Core, and a
    /// load is one of the two moments it is written; a restore that handed the rows to the field and left
    /// telling Core to a line somewhere else is a pair that can be separated by an edit, and the separated
    /// version wakes a captain on a fenced ground with no fence on it. A pre-#1074 file simply lacks the
    /// field and wakes with nothing under study, which is the truth about every voyage played before the
    /// office got there.</para></summary>
    private void RestorePreserve(ProgressSection? progress)
    {
        if (progress?.HallsPreserved is { } preserved)
        {
            _hallsPreserved = [.. preserved];
        }
        InstallPreserveRegister();
    }

    /// <summary>#1074 · Hand Core the world's care state — the ONE writer. Everything downstream (the rail
    /// round the shed, the notice at its gate) reads this and nothing else.</summary>
    private void InstallPreserveRegister() => PreservationZone.Install(_hallsPreserved);

    /// <summary>
    /// #1074 beat 2 · <b>THE EVENT.</b> A working the office closed passes into official care.
    ///
    /// <para><b>The threshold, and its reason, per <see cref="DisclosureClock"/>'s own contract</b>: <b>two
    /// whole world windows</b> since the opening — one more than the order took, because the extra shift IS
    /// the beat. The order closed the working pending a structural review with no published schedule; a
    /// shift later there is still no schedule, because the review that was never scheduled has become a
    /// study that never ends, and a study needs a fence round it. Nobody decided anything in between; time
    /// passed and the paperwork hardened. <b>And the working must already be closed</b>, which is the whole
    /// of "never on an unstopped ground" and, because a ground is stopped or buried and never both, the
    /// whole of "never on a buried one" as well.</para>
    ///
    /// <para><b>Called from the descent, in the same breath as the burial, the decline, the quiet hands and
    /// the order, and for their reason</b> (see <see cref="BuryWhatWasOpened"/>): it is the only moment in
    /// the game when the world is about to be rebuilt and no excursion is standing on it, so "not while he is
    /// there" is true by construction rather than by a check somebody has to remember. It runs AFTER the
    /// order in that breath, and it must: the two are one ground's paperwork a shift apart, and a captain who
    /// stayed away two shifts should come back to both at once rather than to a fence with no order behind
    /// it.</para>
    /// </summary>
    private void TheSitePassesIntoCare()
    {
        IReadOnlyList<string> next = PreservationZone.Note(
            _hallsOpened, _hallsStopped, _hallsPreserved, _surface?.Stop.Body.Id, SimTime);

        if (!ReferenceEquals(next, _hallsPreserved))
        {
            _hallsPreserved = next;
            RequestVaultSave();
        }

        // Installed on every descent and not only on a change, for the reason InstallStopRegister is: a
        // fresh voyage, a loaded save and a captain nobody has fenced anything on all share one static, and a
        // world that inherited the last one's register would be the worst bug this feature could have.
        InstallPreserveRegister();
    }
}

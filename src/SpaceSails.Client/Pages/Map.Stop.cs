using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #1074 · THE STOP ORDER AT THE DIG — the client half. Core carries the whole argument (see StopOrder.cs and
// UndergroundComplex.Stop.cs); this file owns two things and nothing else: the register that rides the vault,
// and the ONE moment the event is evaluated.
//
// THERE IS NOTHING ELSE TO OWN. No card, no pulse of its own, no beat, no nerve shock, no marker, no wire
// line. The closure is told by a lift panel that has nothing under it any more, a leaf at the end of a
// corridor with an office's stamp on it, a valve-book that goes terse for one line, and a roster that still
// lists the shift. Every one of those is drawn or dealt by code that has been in the building for months.
public partial class Map
{
    /// <summary>#1074 · The grounds whose deep working the Authority has closed. Persisted per-universe in the
    /// vault's ProgressSection beside <c>_hallsOpened</c>, <c>_hallsBuried</c> and <c>_hallsDeclined</c>, and
    /// for the hardest version of the burial's own reason: the stop and the burial are ONE trigger's two
    /// outcomes, so a save that remembered the fills and forgot the closures would let a ground the office had
    /// already closed be filled in by the neighbours on the very next descent — two things that happened to
    /// one place, neither of which can be true if the other is.</summary>
    private IReadOnlyList<string> _hallsStopped = [];

    /// <summary>#1074 · <c>/map?stopped=1</c> — see the query parser. It is <c>?buried=1</c>'s twin: the same
    /// rock, the same already-opened ground, and a window chosen so the split hands this one to the office
    /// instead of to the neighbours. It seeds the disclosure clock's register and then gets out of the way;
    /// the closure itself runs through the ordinary <see cref="StopOrder.Note"/> on the ordinary descent.
    /// </summary>
    private bool _stoppedCheat;

    /// <summary>#1074 · Hand Core the world's closure state — the ONE writer. Everything downstream (the gate
    /// the panel stops offering, the seal at the blind end of the spine, the valve-book, the roster on the
    /// board) reads this and nothing else.</summary>
    private void InstallStopRegister() => StopOrder.Install(_hallsStopped);

    /// <summary>
    /// #1074 · <b>THE EVENT.</b> Between two visits, the working the captain opened is closed by order.
    ///
    /// <para><b>The threshold, and its reason, per <see cref="DisclosureClock"/>'s own contract</b>: <b>one
    /// whole world window</b> since the opening — the burial's number and #1068's number, and deliberately
    /// the same one, because this is the same trigger read a second way — <b>and the captain not on that
    /// body</b>, because an order posted while he stood on the floor would be a thing that happened TO him.
    /// <b>And the split</b>: this ground is the office's rather than the neighbours'
    /// (<see cref="StopOrder.TheOfficeGetsThisOne"/>). All three live in <see cref="StopOrder.Note"/>; this is
    /// the moment they are asked.</para>
    ///
    /// <para><b>Called from the descent, in the same breath as the burial and the decline and for the same
    /// reason</b> (see <see cref="BuryWhatWasOpened"/>): it is the only moment in the game when the world is
    /// about to be rebuilt and no excursion is standing on it, so "not while he is there" is true by
    /// construction rather than by a check somebody has to remember. It is also why the closure LANDS on a
    /// return — he comes back down, rides to the bottom the building admits to, and the panel has nothing
    /// under it.</para>
    /// </summary>
    private void TheOfficeClosesTheWorking()
    {
        IReadOnlyList<string> next = StopOrder.Note(
            _hallsOpened, _hallsStopped, _surface?.Stop.Body.Id, SimTime);

        if (!ReferenceEquals(next, _hallsStopped))
        {
            _hallsStopped = next;
            RequestVaultSave();
        }

        // Installed on every descent and not only on a change, for the reason InstallBurialRegister is: a
        // fresh voyage, a loaded save and a captain nobody has closed anything on all share one static, and a
        // world that inherited the last one's register would be the worst bug this feature could have.
        InstallStopRegister();
    }
}

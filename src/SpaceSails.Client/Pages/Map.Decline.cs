using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #1068 · THE WORLD DECLINES POLITELY — the client half of the watchers' two non-human channels. Core carries
// the whole argument (see PoliteDecline.cs and UndergroundComplex.Decline.cs); this file owns two things and
// nothing else: the register that rides the vault, and the ONE moment the event is evaluated.
//
// THERE IS NOTHING ELSE TO OWN. No card, no pulse, no beat, no nerve shock, no marker, no line — the two acts
// are a leaf that is drawn shut by the same code that has drawn every locked door in the building since #585,
// and a telescope pass that does not land. Both are told entirely by the absence of something that was there,
// which is the only channel #672 leaves open.
public partial class Map
{
    /// <summary>#1068 · The grounds the world has declined on, and the window each declined in. Persisted
    /// per-universe in the vault's ProgressSection beside <c>_hallsOpened</c> and <c>_hallsBuried</c>, and
    /// the window is persisted with the id for the reason the vault field says: the door is chosen against
    /// that number, so a world that forgot it would shut a different door on the next reload — and a lock
    /// that moves is an event, which is the one thing a declined door may never be.</summary>
    private IReadOnlyList<PoliteDecline.Decline> _hallsDeclined = [];

    /// <summary>#1068 · Hand Core the world's decline state — the ONE writer. Everything downstream (the leaf
    /// the generator shuts, the pass the scanning desk does not land) reads this and nothing else.</summary>
    private void InstallDeclineRegister() => PoliteDecline.Install(_hallsDeclined);

    /// <summary>
    /// #1068 · <b>THE EVENT.</b> Between two visits, the grounds this captain opened decline.
    ///
    /// <para><b>The threshold, and its reason, per <see cref="DisclosureClock"/>'s own contract</b> (<i>"every
    /// beat that reads it chooses its own threshold and writes that threshold's reason down beside its own
    /// words"</i>): <b>one whole world window</b> since the opening — <b>the burial's own number, on purpose,
    /// because the watchers act on the schedule the neighbours do</b> — <b>and never on the visit that opened
    /// the ground</b>, because a door that had stopped opening by the time the captain climbed back out of
    /// the seam he had just crossed would be an answer to what he had just done, delivered inside the hour,
    /// by something that was watching him do it. That is a sensor return by another name, and #672 forbids
    /// exactly that. Both conditions live in <see cref="PoliteDecline.Note"/>; this is the moment they are
    /// asked.</para>
    ///
    /// <para><b>Called from the descent, in the same breath as the burial and for the same reason</b>
    /// (see <see cref="BuryWhatWasOpened"/>): it is the only moment in the game when the world is about to be
    /// rebuilt and no excursion is standing on it, so "not while he is there" is true by construction rather
    /// than by a check somebody has to remember. It is also why the decline LANDS on a return — he comes back
    /// down, walks the floor he knows, and one leaf on it is shut.</para>
    /// </summary>
    private void TheWorldDeclines()
    {
        IReadOnlyList<PoliteDecline.Decline> next = PoliteDecline.Note(
            _hallsOpened, _hallsDeclined, _surface?.Stop.Body.Id, SimTime);

        if (!ReferenceEquals(next, _hallsDeclined))
        {
            _hallsDeclined = next;
            RequestVaultSave();
        }

        // Installed on every descent and not only on a change, for the reason InstallBurialRegister is: a
        // fresh voyage, a loaded save and a captain the world has declined nothing to all share one static,
        // and a world that inherited the last one's register would be the worst bug this feature could have.
        InstallDeclineRegister();
    }
}

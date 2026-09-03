using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// #1068 · THROUGH PEOPLE WHO DO NOT KNOW WHY — the client half of the watchers' third channel. Core carries
// the whole argument (see QuietHands.cs and DockRoster.cs); this file owns two things and nothing else: the
// register that rides the vault, and the ONE moment the event is evaluated.
//
// THERE IS NOTHING ELSE TO OWN. No card, no pulse, no beat, no nerve shock, no marker, NOT ONE LINE — the
// two acts are a berth number that is not the one he had and a pump price that is not the one he paid, both
// printed by code that has been printing them since #269 and #157 respectively. The rag line that belongs to
// this channel was spent on the burial (#1063) and is not repeated here; this half of the channel says
// nothing at all, which is what makes it the channel it is.
public partial class Map
{
    /// <summary>#1068 · The grounds the harbour has done its ordinary paperwork about, the window each was
    /// filed in, and whether the reassigned berth has been handed over yet. Persisted per-universe in the
    /// vault's ProgressSection beside <c>_hallsOpened</c>, <c>_hallsBuried</c> and <c>_hallsDeclined</c>.
    /// </summary>
    private IReadOnlyList<QuietHands.Hand> _hallsHandled = [];

    /// <summary>#1068 · Hand Core the harbour's state — the ONE writer. Everything downstream (the slot the
    /// roster gives at the clamp, the credit the pump has moved) reads this and nothing else.</summary>
    private void InstallQuietHandsRegister() => QuietHands.Install(_hallsHandled);

    /// <summary>#1068 · The register as the vault carries it — null while nothing has been filed, the
    /// #1057/#1072/#1066/#677/#1063 law, because the checksum is taken over the payload and an eager empty
    /// list would change the digest of every vault ever written.</summary>
    private IReadOnlyList<QuietHandRecord>? QuietHandRows() =>
        _hallsHandled.Count > 0
            ? [.. _hallsHandled.Select(h => new QuietHandRecord(h.BodyId, h.Window, h.BerthGiven))]
            : null;

    /// <summary>#1068 · …and back the other way, on load, with Core told at once rather than at the next
    /// descent: a save resumed straight onto a berth must tie up in the slot the clamp tied up in.
    ///
    /// <para>All three fields are restored rather than re-derived. The window is what the slot and the price
    /// move are chosen against, and the spent flag is the only thing standing between "the berth moved once"
    /// and "the berth moves every time you reload" — which is the farmable trigger this whole channel is
    /// written to avoid.</para></summary>
    private void RestoreQuietHands(ProgressSection? progress)
    {
        if (progress?.HallsHandled is { } handled)
        {
            _hallsHandled = [.. handled.Select(h => new QuietHands.Hand(h.BodyId, h.Window, h.BerthGiven))];
        }
        InstallQuietHandsRegister();
    }

    /// <summary>
    /// #1068 · <b>THE EVENT.</b> Between two visits, the harbour that serves the grounds this captain opened
    /// moves its paperwork.
    ///
    /// <para><b>The threshold, and its reason, per <see cref="DisclosureClock"/>'s own contract:</b> one
    /// whole world window since the opening — the burial's number and the decline's number, on purpose,
    /// because the watchers act on the schedule the neighbours do — and never on the visit that opened the
    /// ground, because a roster retyped inside the hour would be a decision taken about the captain by an
    /// office that watched him take it. Both conditions live in <see cref="QuietHands.Note"/>; this is the
    /// moment they are asked.</para>
    ///
    /// <para><b>Called from the descent, in the same breath as the burial and the decline and for their
    /// reason:</b> it is the only moment in the game when the world is about to be rebuilt and no excursion
    /// is standing on it, so "not while he is there" is true by construction. It is also why both deliveries
    /// LAND on a return — he flies back in, and the harbour ties him up somewhere else and charges him a
    /// credit more for the fill.</para>
    /// </summary>
    private void TheQuietHandsMove()
    {
        IReadOnlyList<QuietHands.Hand> next = QuietHands.Note(
            _hallsOpened, _hallsHandled, _surface?.Stop.Body.Id, SimTime);

        if (!ReferenceEquals(next, _hallsHandled))
        {
            _hallsHandled = next;
            RequestVaultSave();
        }

        // Installed on every descent and not only on a change, for the reason InstallBurialRegister is: a
        // fresh voyage, a loaded save and a captain nobody has filed anything about all share one static,
        // and a world that inherited the last one's register would be the worst bug this feature could have.
        InstallQuietHandsRegister();
    }

    /// <summary>
    /// #1068 · <b>DELIVERY 1 — THE BERTH IS HANDED OVER.</b> Called from the clamp, once the ship is tied
    /// up: the reassignment this port owed is now spent, so the next clamp here is the ordinary slot and so
    /// is every clamp after it.
    ///
    /// <para>Nothing is said. There is no fault to log, no fee to change, no plate to repaint, and the berth
    /// KIND is exactly what it was a moment ago — #1066's shore-leave tally and #1078's establishing shot
    /// both read <see cref="ArrivalTube.TierFor"/>, which this never goes near.</para>
    /// </summary>
    private void TakeTheBerthTheRosterGave(string havenId)
    {
        if (_ephemeris is null)
        {
            return;
        }

        IReadOnlyList<QuietHands.Hand> next = QuietHands.GiveTheBerth(_hallsHandled, _ephemeris, havenId);
        if (ReferenceEquals(next, _hallsHandled))
        {
            return;   // nothing was owed here, which is the answer at every berth in almost every world
        }

        _hallsHandled = next;
        InstallQuietHandsRegister();
        RequestVaultSave();
    }
}

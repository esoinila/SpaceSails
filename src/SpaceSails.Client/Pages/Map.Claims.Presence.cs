using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Claims.Presence — #1151 · THE PRESENCE LAW.
//
// Owner ruling, 2026-09-06 on #525: collectors must prove a ship and a captain are one, their process is
// super strict, and "the action never happens where the captain is not there to experience or pay it".
//
// ── WHAT WAS ALREADY HERE, AND WHY IT WAS NOT THE LAW ──────────────────────────────────────────────────
//
// #580 shipped half of this and did not know it was half. `UpdateEncounters` already held every pursuer on
// station while `_surface is not null`, with the right argument written beside it — heat is the CAPTAIN's,
// and a good long excursion should not mean coming home to a boarding party. What it could not see is that
// an excursion is only ONE of the three ways a captain is not on his ship. The shuttle run is a second, and
// #1138 found the third the hard way: a captain past the tube on a station's own floor, with three
// kilometres of ring and a mated gangway between him and her, is not aboard her by any reading a person
// would give the word. On the old code a hunter could reach a hull whose master was standing in a bar.
//
// So the predicate is written ONCE, here, and both readers ask it: the encounter loop, and the scuttle
// board's own `CaptainWasAboardHer`. Two copies of "is he on her" that could disagree is this ground's
// mirrored-constant bug class said about a fact instead of a number, and the scuttle board's copy was the
// better one — it is the one that moved.
//
// ── AND WHAT WAITING LOOKS LIKE ────────────────────────────────────────────────────────────────────────
//
// A held writ that nothing on any screen mentions is not a process, it is a pause. The ledger carries the
// plate (`NebulaClaims.PendingWritPlate`) for as long as somebody is out there unable to proceed, and it
// carries it for the two shapes of waiting the game now has: a pursuer holding station in the sky, and a
// pursuer taken off the sky entirely by #1090's break-off and put onto the file at a port.
public sealed partial class Map
{
    /// <summary>
    /// #1151 · <b>IS THE MASTER ABOARD HER?</b> The one predicate, and the three signals that say no.
    ///
    /// <para>He is on a surface; the boat is away with him in it; or he is past the tube on somebody's
    /// concourse while she is clamped to their collar. Everything else — walking her own deck, sitting at his
    /// own nav board with the gangway mated — is aboard, because guessing generously about presence is how a
    /// mechanic quietly stops having stakes, and <see cref="_ashore"/> rather than
    /// <see cref="_dockedHavenId"/> is the signal for exactly that reason.</para>
    ///
    /// <para>This is <c>CaptainWasAboardHer</c>'s body, moved. That method now asks this one, so the scuttle
    /// board and the collectors cannot come to two different answers about the same captain.</para>
    /// </summary>
    private bool TheMasterIsAboardHer() =>
        _surface is null && _shuttleRun is null && !(HerChargesAreAtABerth && _ashore);

    /// <summary>#1151 · The writ nobody can serve until the captain is back on his ship — filed when a
    /// pursuer is taken off the sky by an ending that did not end the process. Null on almost every
    /// voyage.</summary>
    private PendingWritRecord? _writPending;

    /// <summary>
    /// #1151 · <b>THE PURSUERS CANNOT PROCEED WITHOUT YOU.</b> Called where #1090 used to simply empty the
    /// roster: she has stopped existing, the arithmetic of chasing her has stopped working, and every hunter
    /// breaks off — which is right about the CHASE and wrong about the PROCESS. A collector's process wants a
    /// ship and a captain in one place. It has neither now, and it does not therefore stop; it waits.
    ///
    /// <para>It waits at a BERTH, because a ground has no market of its own and no berths to let — the same
    /// sentence <see cref="QuietHands.PortFor"/> was written for, asked here rather than re-derived, so the
    /// harbour a pursuer waits at for a captain and the harbour that reassigns his berth cannot disagree
    /// about which port serves a moon. In open space, with no ground under any of it, there is nothing to ask
    /// and nobody is filed: a pursuer who lost a hull in the dark lost it in the dark.</para>
    ///
    /// <para><b>One writ, not a roster.</b> Whoever was closest to serving it is the one the file keeps; a
    /// list of people waiting at ports would be a feature this slice does not have and a save format the next
    /// one would have to migrate.</para>
    /// </summary>
    private void TheWritWaitsForHim(string? groundBodyId, string? berthHavenId, string callsign)
    {
        if (_writPending is not null || string.IsNullOrEmpty(callsign))
        {
            return;
        }

        string? port = berthHavenId;
        if (port is null && _ephemeris is { } sky && !string.IsNullOrEmpty(groundBodyId))
        {
            port = QuietHands.PortFor(sky, groundBodyId!)?.Id;
        }

        if (string.IsNullOrEmpty(port))
        {
            return;
        }

        _writPending = new PendingWritRecord(callsign, port!, SimTime);
    }

    /// <summary>
    /// #1151 · <b>THE PLATE, ON THE CAPTAIN'S OWN LEDGER.</b> A row for as long as something is waiting on
    /// the master, and no row at all otherwise — this is a state of paperwork, not a standing note.
    ///
    /// <para>Two ways to be waiting, one plate. A pursuer holding station in the sky is only pending while
    /// the master is off her; a pursuer on the file is pending until somebody serves it, which is a later
    /// slice's scene. The lines are the two names the world can be asked for — whose writ it is, and where —
    /// and nothing on the row is a sentence, because nobody authored one.</para>
    /// </summary>
    private Stations.Captain.LedgerTip? PendingWritTip()
    {
        List<string> lines = [];

        if (!TheMasterIsAboardHer())
        {
            foreach (HunterState held in _hunters)
            {
                if (!held.BrokenOff && !held.CaughtPlayer)
                {
                    lines.Add(held.Callsign);
                }
            }
        }

        if (_writPending is { } filed)
        {
            lines.Add($"{filed.Callsign} · {BodyName(filed.HavenId)}");
        }

        return lines.Count == 0
            ? null
            : new Stations.Captain.LedgerTip(
                NebulaClaims.PendingWritPlate, lines, "the process wants you and the ship in one place",
                ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null);
    }
}

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
    /// one would have to migrate. <see cref="ThereIsRoomOnTheFile"/> is that sentence said once, so the
    /// filing door and the boarding door cannot come to two different answers about how many writs a captain
    /// can owe at a time.</para>
    ///
    /// <para><b>#1151 slice 4 · AND THE TERMS GO ON THE FILE WITH IT</b>, off the pursuer this is being filed
    /// for and the gauge as it stands now — see <see cref="PendingWritRecord"/> for why they are stored
    /// rather than asked again on the day it is served.</para>
    /// </summary>
    private void TheWritWaitsForHim(string? groundBodyId, string? berthHavenId, HunterState pursuer)
    {
        if (!ThereIsRoomOnTheFile || string.IsNullOrEmpty(pursuer.Callsign))
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

        _writPending = new PendingWritRecord(
            pursuer.Callsign, port!, SimTime, Math.Max(1, _heat.Level), pursuer.Id);
    }

    /// <summary>#1151 slice 4 · <b>ONE WRIT, NOT A QUEUE.</b> A captain owes one process at a time, and while
    /// one is out there unserved there is no room on the file for a second — whether the second would arrive
    /// by an ending (<see cref="TheWritWaitsForHim"/>) or by somebody running him down
    /// (<see cref="TheProcessMayProceed"/>).</summary>
    private bool ThereIsRoomOnTheFile => _writPending is null;

    /// <summary>
    /// #1151 slice 4 · <b>MAY ANYBODY IN THE SKY ACTUALLY PROCEED AGAINST HIM RIGHT NOW?</b> Two questions,
    /// and a pursuer needs a yes to both.
    ///
    /// <para>The first is the presence law: the master has to be aboard her, or there is no ship-and-captain
    /// for anybody's process to be about.</para>
    ///
    /// <para>The second is the one this slice adds. <b>A writ already on the file stands</b>, and a second
    /// collector who catches the same hull does not open a second one over the top of it — he DEFERS, which
    /// in this sim is a thing that already has a shape: he holds station, exactly as every pursuer does when
    /// the process cannot proceed (<see cref="EncounterRule.HoldStation"/>, position and velocity untouched,
    /// the clock carried forward). Nothing new is invented for him, and nothing is said, because the second
    /// man on the ramp has no line and never had one.</para>
    ///
    /// <para>It is deliberately asked BEFORE the pursuit is stepped rather than after a catch has latched: a
    /// catch that has to be un-caught is a state the rest of the game can already read on a frame it should
    /// not have existed on, and <c>CaughtPlayer</c> is exactly the kind of latch three other files ask about.
    /// A pursuer who is never allowed to close never has to be talked out of it.</para>
    /// </summary>
    private bool TheProcessMayProceed() => TheMasterIsAboardHer() && ThereIsRoomOnTheFile;

    /// <summary>
    /// #1151 slice 4 · <b>THE WAITING WRIT IS SERVED.</b> The scene the file was written for: he comes back
    /// to the port he was never going to be able to avoid forever, and the man who has been standing there
    /// since the ending walks up the ramp.
    ///
    /// <para><b>Three conditions, and every one of them is the ruling.</b> The collector is still on station
    /// — the file IS that, there is nobody else to be. The captain is at THAT port, clamped to the berth the
    /// writ was filed against, because a writ does not follow a man about the system: leave without ever
    /// coming back aboard where he is standing and it simply keeps waiting. And the master is ABOARD HER, the
    /// #525 law entire — walking that same concourse three hundred metres from the machine is not being
    /// aboard, and the writ waits through it with the plate still on the ledger.</para>
    ///
    /// <para><b>On the terms it had on the day.</b> It opens the demand a catch has always opened, and the
    /// number on it is the number it would have carried at the moment of filing — the heat that bought the
    /// contract and the seed that moment cut — so waiting is neither a discount nor a fine. A writ priced at
    /// service time would let a captain choose his own bill by loitering until the gauge cooled, and would
    /// bill him for a heat he earned afterwards doing something else.</para>
    ///
    /// <para>The file is cleared BEFORE the demand opens, because at that instant the writ is not pending any
    /// more, it is being served: the ledger row is gone and the card is up, which is the flip the whole slice
    /// is about. Nothing is authored — a served writ shows what a served writ has always shown.</para>
    ///
    /// <para><b>AND NEVER OVER THE TOP OF THE ENDING THAT FILED IT.</b> The castaway's own wake sets him down
    /// at the nearest haven, and for a moon whose harbour is also its nearest haven that is THIS berth: he is
    /// aboard a rustbucket at their port on the very frame the epitaph card goes up. They are still there and
    /// he is still served — the writ was never going to be outrun, and the tug delivering him to their door
    /// is the scene rather than a bug — but not with the ending's own card still on the screen. That is the
    /// same law <c>_busted is not null</c> states one line up, said about the other card, and it costs the
    /// writ nothing: the terms are on the file, so a beat's wait is not a discount.</para>
    /// </summary>
    private void TheWaitingWritIsServed()
    {
        if (_busted is not null || _shipEpitaph is not null || _writPending is not { } filed)
        {
            return;   // one card at a time, and nothing to serve
        }

        if (_dockedHavenId != filed.HavenId || !TheMasterIsAboardHer())
        {
            return;   // he is not back at their berth, or he is not on her
        }

        _writPending = null;

        TheDemandGoesUp(
            new HunterState(
                Id: filed.HunterId ?? filed.Callsign,
                Callsign: filed.Callsign,
                OriginBodyId: filed.HavenId,
                SpawnedAtSimTime: filed.FiledAtSimTime,
                ActivationSimTime: filed.FiledAtSimTime,
                State: _ship,                 // alongside: they were here before he was
                CaughtPlayer: true,
                BrokenOff: false),
            onTheTermsOf: filed);

        RequestVaultSave();
    }

    /// <summary>
    /// #1151 · <b>THE PLATE, ON THE CAPTAIN'S OWN LEDGER.</b> A row for as long as something is waiting on
    /// the master, and no row at all otherwise — this is a state of paperwork, not a standing note.
    ///
    /// <para>Two ways to be waiting, one plate. A pursuer holding station in the sky is pending for exactly
    /// as long as the sim is actually holding him — which after slice 4 is two reasons and not one, so the
    /// row asks <see cref="TheProcessMayProceed"/>, the same question the pursuit loop asks, rather than a
    /// copy of half of it. A pursuer on the file is pending until <see cref="TheWaitingWritIsServed"/> serves
    /// him. The lines are the two names the world can be asked for — whose writ it is, and where — and
    /// nothing on the row is a sentence, because nobody authored one.</para>
    /// </summary>
    private Stations.Captain.LedgerTip? PendingWritTip()
    {
        List<string> lines = [];

        if (!TheProcessMayProceed())
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

using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #525 · <b>THE BERTH SCENE — the client half.</b> Core carries the whole argument
/// (<see cref="BerthScuttle"/>); this file owns the four moments it happens in and nothing else.
///
/// <para><b>Everything here is reuse, and that is the design rather than an economy.</b> The PA is the pulse
/// the charges already talk through. The neighbours are moved by the roster <see cref="DockRoster"/> already
/// keeps and #1092 already reassigns with. The concourse leaves through #731's egress — the same
/// <c>TheyStandUpAndGo</c>, the same leaves the captain's own TRY is refused at, the same two-at-a-time cap,
/// so nobody had to write a second way for a person to cross a room. The heat is the one
/// <see cref="BankTheCrossing"/> seam. The wire is <see cref="PushNewsEvent"/>. The fugitive on foot is the
/// meter's own top rung and not a new flag: <c>PatrolBeat.Chase</c> refuses a floor-wide alert by name, and
/// it is right to.</para>
///
/// <para><b>Not one of these moments fires away from a berth.</b> A scuttle in open space runs the code it
/// ran before this file existed, which is what <c>SheGoesWhetherHeIsAboardOrNotTests</c> goes on proving.</para>
/// </summary>
public partial class Map
{
    // ── WHICH SLOT SHE IS ACTUALLY IN ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>The slot the roster gave her at THIS clamp</b>, or null when she is not tied up anywhere.
    ///
    /// <para><b>Held rather than re-derived, and the reason is a bug that would otherwise be invisible.</b>
    /// <see cref="QuietHands.GiveTheBerth"/> marks a reassignment SPENT the instant the clamp completes, so
    /// asking <see cref="DockRoster.BerthGiven"/> a second later answers with the ordinary slot — while the
    /// hull is pinned on the reassigned one. The PA would then read out a berth number the ship is not in,
    /// which is this repo's named class of the sim doing one thing and a sentence reporting another. Written
    /// where the bearing is computed, from the same call, and cleared when she leaves.</para>
    /// </summary>
    private int? _berthSlot;

    /// <summary>#525 · The slot this port is about to tie her up in — the exact body of
    /// <see cref="DockRoster.BearingAt"/>, split in two so the caller can keep the number as well as the
    /// bearing. A Core guard holds the two forms byte for byte.</summary>
    private int TheSlotTheRosterGives(string havenId) =>
        DockRoster.BerthGiven(
            havenId,
            DockRoster.BerthsAt(_ephemeris!, havenId),
            QuietHands.BerthOwedAt(_ephemeris!, havenId));

    /// <summary>#525 · …and which way that slot points, so the one caller that needs both asks once.</summary>
    private double TheBearingOfSlot(string havenId, int slot) =>
        DockRoster.BearingOf(slot, DockRoster.BerthsAt(_ephemeris!, havenId));

    /// <summary>#525 · Is she clamped to a port's collar right now? The one question that makes this a
    /// different scene — <see cref="BerthScuttle.AtABerth"/>'s, never re-asked in this file.</summary>
    private bool HerChargesAreAtABerth => BerthScuttle.AtABerth(_dockedHavenId, OnWreck);

    // ── THE COLLAR, AND WHAT IS ON THE RECORD ABOUT IT ──────────────────────────────────────────────────

    /// <summary>#525 · One port's collar, cleared by a declared overload: the slot the ship that declared it
    /// is in, the neighbouring slots the roster emptied, and <b>why</b> — which is the whole difference
    /// between this and the quiet hands' own reassignment.</summary>
    /// <param name="HavenId">The port.</param>
    /// <param name="Berth">His own slot, which is never one of the cleared ones.</param>
    /// <param name="Neighbours">The slots reassigned away, ascending.</param>
    /// <param name="Reason">On the record.</param>
    private sealed record ClearedCollar(
        string HavenId, int Berth, IReadOnlyList<int> Neighbours, BerthScuttle.Why Reason);

    /// <summary>
    /// #525 · The collar this captain has had cleared, or null — which is the answer in every voyage where
    /// nobody has declared an overload at a berth, which is almost every voyage.
    ///
    /// <para><b>It is not cleared by the abort, and that is the ruling rather than an oversight</b> — see
    /// <c>BackTheKeysOut</c>. It rides the vault for the same reason: a harbour that had retyped its roster
    /// and then forgot it on the next reload would be a consequence the player could undo with F5.</para>
    /// </summary>
    private ClearedCollar? _collarCleared;

    /// <summary>#525 · The collar as the vault carries it — null while nothing has been declared anywhere,
    /// the #1057/#1072/#1066/#1063 law: the checksum is taken over the payload, so an eager empty row would
    /// change the digest of every vault ever written.</summary>
    private ClearedCollarRecord? ClearedCollarRow() =>
        _collarCleared is { } c
            ? new ClearedCollarRecord(c.HavenId, c.Berth, [.. c.Neighbours], c.Reason.ToString())
            : null;

    /// <summary>#525 · …and back the other way on load. An unknown reason is dropped rather than guessed:
    /// a record whose cause this build cannot read is a record with no reason on it, and a reassignment with
    /// no reason is #1092's, not this one's.</summary>
    private void RestoreClearedCollar(ProgressSection? progress)
    {
        _collarCleared = progress?.CollarCleared is { } row
                         && System.Enum.TryParse(row.Reason, out BerthScuttle.Why why)
            ? new ClearedCollar(row.HavenId, row.Berth, [.. row.Neighbours], why)
            : null;
    }

    // ── 1 · ARMING AT A BERTH IS PUBLIC ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>THE KEYS TURNED, AND SHE IS CLAMPED ON.</b> Three things happen at once and none of them is
    /// reversible, because a declared overload is a thing a harbour has now heard.
    ///
    /// <list type="number">
    /// <item><b>The port's PA</b>, once, on the pulse — the charges' own announcement said by the harbour
    /// instead of by the hull. It names the slot and nobody at all.</item>
    /// <item><b>The neighbouring slots go</b>, on the record, with a reason on them.</item>
    /// <item><b>The concourse leaves</b>, through the leaves that do not open for the captain.</item>
    /// </list>
    ///
    /// <para>Called from <c>TurnBothKeys</c> and nowhere else, so there is exactly one moment in the game at
    /// which a harbour finds out.</para>
    /// </summary>
    private void TheCollarIsCleared()
    {
        if (!HerChargesAreAtABerth || _ephemeris is null || _dockedHavenId is not { } haven)
        {
            return;
        }

        int slot = _berthSlot ?? TheSlotTheRosterGives(haven);
        int berths = DockRoster.BerthsAt(_ephemeris, haven);

        // THE ONE LINE. Through the pulse, because #761's law wants it on the surface the captain is looking
        // at and there is no card here — the card is ninety seconds away. Logged as the same string rather
        // than a second sentence about it: the board is a transcript, not a commentary.
        string pa = BerthScuttle.PaCall(BerthScuttle.BerthNumber(slot));
        ShowPulseMessage(pa);
        ShipBoardLog(pa);

        // THE ROSTER. #1092 hands a captain a different slot for reasons nobody in the office could tell
        // you; this empties the slots either side of him and the reason is in the row.
        _collarCleared = new ClearedCollar(
            haven, slot, BerthScuttle.CollarCleared(slot, berths), BerthScuttle.Why.DeclaredOverload);
        RequestVaultSave();

        // …AND THE ROOM. Nothing is said about this at all — a bar emptying is something a captain watches
        // happen, and #731 built the whole of it already.
        if (TheDockedBar() is { } bar)
        {
            TheConcourseClearsOut(in bar);
        }
    }

    /// <summary>
    /// #525/#731 · <b>THE CONCOURSE GOES OUT THROUGH THE BACK.</b> Not a new walk: the evening's schedule is
    /// REPLACED, and the room's own machinery does the rest.
    ///
    /// <para><b>Everybody still seated becomes due at once</b> (<c>AtSecondsIntoWatch</c> zero), and nobody
    /// is coming out of the back into a collar that is being cleared. <c>DealTheBarsHours</c> then paces them
    /// through <see cref="Egress.MostAtOnce"/>, which is why they leave in a file rather than in a crowd —
    /// the cap was written because four at once is a fire drill, and this is the one evening it is.</para>
    ///
    /// <para>The door each takes is <see cref="Egress.DoorFor"/>'s, so a regular goes out of the same leaf on
    /// this evening that he would have gone out of at the end of it. A regular with no leaf to reach is left
    /// where he is: no route, no walk, and never a body placed at the far end of a walk that could not be
    /// walked.</para>
    /// </summary>
    private void TheConcourseClearsOut(in HavenInterior.BarFloor bar)
    {
        IReadOnlyList<HavenInterior.SeatedRegular> rota =
            HavenInterior.ResolveRegulars(bar.BodyId, _dockVisitSimTime);

        var going = new List<Egress.Move>(rota.Count);
        for (int i = 0; i < rota.Count; i++)
        {
            HavenInterior.SeatedRegular who = rota[i];
            if (!who.Present || _barLeft.Contains(who.Id))
            {
                continue;   // already gone, or never in the room this watch
            }

            int door = Egress.DoorFor(bar.BodyId, BarIsNotAFloor, BarWatch, who.Id, bar.Doors);
            if (door < 0)
            {
                continue;   // a room with no leaf in it has nowhere to send anybody
            }

            going.Add(new Egress.Move(who.Id, i, 0, door));
        }

        _barGoing = going;
        _barComing = [];
    }

    // ── 2 · THE STATION FILES IT ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #525 · <b>SHE WENT AT THE BERTH.</b> Two things the station does about it, and one it does NOT.
    ///
    /// <list type="bullet">
    /// <item><b>The wire carries it at once</b> — the witness that cannot forget. This is the exact seam
    /// #1126 guarded the other way round: a black-ops key is an encounter that never happened and pushes
    /// nothing, so there is nothing to delete afterwards. Here the whole point is that something is filed
    /// before the captain has crossed the concourse.</item>
    /// <item><b>The port's operator remembers it at the top of their meter</b> — one whole
    /// <see cref="IllegalHeat.Ceiling"/>, through the one <see cref="BankTheCrossing"/> seam. That is the
    /// fugitive on foot: their round starts every watch at the end of its patience
    /// (<see cref="BerthScuttle.AFugitiveOnTheirFloor"/>), which is what being wanted on a floor looks like
    /// in a building that refuses to keep an alert state.</item>
    /// <item><b>Nobody breaks off.</b> #1090's deterrent — every pursuer still on her lets go at zero,
    /// because there is no prize in a ship that has stopped existing — is suppressed here, and the
    /// suppression is the canon: the prize was never only the hull. He did it on their concourse and he is
    /// still standing on it.</item>
    /// </list>
    ///
    /// <para><b>THE JUDGEMENT CALL, AND IT IS FLAGGED.</b> The wire has no headline of its own for a hull
    /// that went at a station, and this lane may author only its two sentences, so the entry goes on as the
    /// one existing kind that is TRUE of what just happened: somebody is now coming for you, named as the
    /// outfit that runs this port and dated at this port. See the marker below for the line this beat would
    /// take if the owner would rather it had one.</para>
    /// </summary>
    private void TheStationFilesIt(string havenId)
    {
        // FABLE: line needed — the wire's own headline for a hull that went off inside a harbour, in the
        // flat clerical voice NewsWire.NewsEventKind.ArcBeatBreaks takes (the subject IS the sentence). Until
        // there is one, the entry rides the existing "somebody is now coming for you" kind, which is true.
        PushNewsEvent(
            NewsWire.NewsEventKind.HunterDispatched,
            SiteOperator.Of(havenId).Name,
            BodyName(havenId) ?? _havenName);

        BankTheCrossing(BerthScuttle.Charge(havenId));
    }
}

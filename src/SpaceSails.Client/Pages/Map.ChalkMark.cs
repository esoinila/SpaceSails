using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #794 · <b>THE CHALK MARK — the page's half.</b> Core decides everything (<see cref="ChalkMark"/>: which
/// table, which watches, what the stone says); this file is the five moments Core cannot reach: the payment
/// that earns the return, the gallery the captain walks into, the chalk on the plan, the move on the table's
/// card, and the dev start.
///
/// <h3>Slice 2 · the drop is at the gallery now</h3>
///
/// <para>Owner's ruling on #794, 2026-09-28: the drop moves to where the captain already goes. Slice 1 kept
/// it under a park bench on a Hive floor, which no sol ground's delivery could ever name; it is under one of
/// the two steel tables at the end of Selene Gate's observation walk now (<see cref="HavenInterior.GalleryTops"/>),
/// with the cross on the back-wall stone beside the machines. The park lost the drop — its bench card is its
/// own two moves again — and nothing is duplicated. Every poll here runs from the docked room's own frame
/// (<c>AdvanceBarWalkers</c>, the concourse path), because the gallery is on the concourse and a berth has no
/// surface frame.</para>
///
/// <h3>No field on this page, on purpose</h3>
///
/// <para>Every fact rides <c>_roomsTurnedOver</c>, the durable register #711's quiet watches already write
/// into, as tags; the dev start is read off the address bar (<see cref="ChalkMark.CheatIn"/>), the way
/// <c>?perf=1</c> and the held-beats flag are. #905's frame ledger walks every instance field of this page,
/// so a new one — even a <c>bool</c> that is false in every scene — would move thirty fingerprints for a
/// feature that is not there. With no paid delivery on record, nothing here writes, draws or changes a
/// card.</para>
/// </summary>
public sealed partial class Map
{
    // ── THE PAYMENT EARNS THE RETURN ────────────────────────────────────────────────────────────────────

    /// <summary>How many tables the gallery at this berth stands — the room's own published list, counted.
    /// Zero at every berth without the walk, which is every berth but one.</summary>
    private static int TheGallerysTables(string? berth) =>
        berth is null ? 0 : HavenInterior.GalleryTops(berth).Count;

    /// <summary>
    /// #794 · Called at the desk the moment a delivery is PAID, at any haven. The return is owed at the
    /// gallery (one tag, keyed on the haven the gallery is in) and the payment pulse gets the design's
    /// sentence appended; a world with no gallery standing gets nothing at all — no drop, no sentence.
    /// </summary>
    private string TheReturnIsOwed(ParcelDrop.Payment paid)
    {
        if (ChalkMark.For(paid.ParcelId, ChalkMark.Haven, PatronRota.WatchIndex(SimTime),
                TheGallerysTables(ChalkMark.Haven)) is not { } mark)
        {
            return "";
        }

        _roomsTurnedOver.Add(mark.Owed);
        return " " + mark.ThePaymentLine();
    }

    /// <summary>The returns owed at the berth the captain is clamped to, or none — asked of the register
    /// alone, so a berth with nothing owed builds nothing.</summary>
    private IReadOnlyList<ChalkMark> TheReturnsOwedHere() =>
        _dockedHavenId is { } berth
            ? ChalkMark.OwedOn(_roomsTurnedOver, berth, TheGallerysTables(berth))
            : [];

    // ── THE GALLERY ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #794 · <b>THE STONE BY THE MACHINES.</b> Polled from the docked room's frame, on the concourse.
    /// Standing in the gallery while the mark is up files the mark's line once per window; after a wipe the
    /// captain SAW go up, the wipe's line once. The told-ness is a tag in the register, so a reload does not
    /// tell it twice.
    /// </summary>
    private void CheckTheChalkMark()
    {
        if (_dockedHavenId is not { } berth || !OnTheConcourse || _viewObject is not null
            || !HavenInterior.InTheGallery(berth, _avatarX, _avatarY, _havenFloor))
        {
            return;
        }

        foreach (ChalkMark mark in TheReturnsOwedHere())
        {
            if (mark.InTheGallery(SimTime, _roomsTurnedOver) is { } beat)
            {
                _roomsTurnedOver.Add(beat.Tag);
                ShowAndFile(beat.Line, ChalkMark.Glyph, PulseRank.Beat);
                RequestVaultSave();
                return;
            }
        }
    }

    /// <summary>#794 · Where the chalk is on the plan this frame, or null — drawn only while the mark is up,
    /// only on the concourse (the floor below is laid in the same coordinates), and only on the stone the
    /// room built.</summary>
    private (double X, double Y)? TheChalkOnTheStone()
    {
        if (_dockedHavenId is not { } berth || !OnTheConcourse)
        {
            return null;
        }

        foreach (ChalkMark mark in TheReturnsOwedHere())
        {
            if (mark.MarkIsUpAt(SimTime)
                && HavenInterior.TheThroatAt(berth) is { } throat
                && HavenInterior.TheVendingMachineBlocks(berth) is { } machines
                && mark.Table < machines.Count)
            {
                return ChalkMark.WhereOnTheStone(machines[mark.Table], throat.Y);
            }
        }
        return null;
    }

    // ── THE TABLE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Is this sitting at one of the gallery's tables at this berth? The seat's own identity
    /// (<c>TheGallerysOwnSeatUnderfoot</c> keys it <c>gallery:{berth}:…</c>), never a position.</summary>
    private bool AtTheGallerysTable(TableTalk t) =>
        _dockedHavenId is { } berth
        && t.Key.StartsWith($"gallery:{berth}:", System.StringComparison.Ordinal);

    /// <summary>The return under the table the captain is sitting at, if the move is on offer right now.</summary>
    private ChalkMark? TheDropUnderThisLip(TableTalk t)
    {
        if (!AtTheGallerysTable(t))
        {
            return null;
        }

        bool alone = t.Solo && !t.SharedSeat;
        foreach (ChalkMark mark in TheReturnsOwedHere())
        {
            if (mark.IsOnOffer(t.Index, alone, SimTime)
                && Satchel.CanTake(_satchel, mark.TheParcelUnderTheSlat(TheLandableGround())))
            {
                return mark;
            }
        }
        return null;
    }

    /// <summary>
    /// #794 · <b>THE CARD SAYS WHAT IS UNDER THE TABLE, AND NOTHING ELSE DOES.</b> Each frame while the
    /// captain is at a gallery table on his own: the card carries FEEL UNDER THE LIP exactly while there is
    /// something to feel for — alone, at the named table, in the window or the watch after it — and is the
    /// plain table's card otherwise. With nothing owed it never touches the card at all.
    ///
    /// <para>A pocket too full to take the packet is a state the design wrote no line for, so it is left
    /// silent: the move is simply absent.</para>
    /// </summary>
    private void KeepTheLipHonest()
    {
        if (_seating.Table is not { } t || !AtTheGallerysTable(t)
            || t.Scene.Id != SittingAlone.TheTable().Id)
        {
            return;
        }

        bool goods = TheDropUnderThisLip(t) is not null;
        if (ChalkMark.Offers(t.Scene) != goods)
        {
            t.Scene = ChalkMark.TheTable(t.Scene, goods);
            StateHasChanged();
        }
    }

    /// <summary>
    /// #794 · <b>FEEL UNDER THE LIP.</b> Taken ahead of the seat's own dispatch because it moves things the
    /// seat does not own: the packet into the satchel, the collection into the register (once — it is what
    /// ends the return), the entry into the field book. The line is said on the card, where the captain is
    /// looking (#680). True when the press was this move, whatever came of it.
    /// </summary>
    private bool TheLipIsFelt(string moveId)
    {
        if (moveId != ChalkMark.FeelUnderTheLip)
        {
            return false;
        }

        if (_seating.Table is not { } t || !AtTheGallerysTable(t))
        {
            return true;
        }

        if (TheDropUnderThisLip(t) is not { } mark)
        {
            // The watch turned while the hand was on its way: the move goes, and nothing is said. A drop that
            // is not there is not a sentence.
            t.Scene = ChalkMark.TheTable(t.Scene, goodsUnderThisLip: false);
            StateHasChanged();
            return true;
        }

        _satchel = [.. Satchel.Add(_satchel, mark.TheParcelUnderTheSlat(TheLandableGround()))];
        _roomsTurnedOver.Add(mark.CollectedOn(PatronRota.WatchIndex(SimTime)));
        FileNote(ChalkMark.CollectedEntry, ChalkMark.Glyph);

        t.Scene = ChalkMark.TheTable(t.Scene, goodsUnderThisLip: false);
        t.Outcome = ChalkMark.FeltLine;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #794 QA · <c>?dock=selene-gate&amp;ashore=1&amp;chalk=1</c> — a paid delivery on record, the clock at a
    /// window, the mark up, the captain standing in the gallery; <c>…&amp;chalk=wiped</c> — one watch later,
    /// the mark seen and wiped, the goods still under the table. Called right after <c>?ashore=1</c> has
    /// walked the captain into the hall.
    ///
    /// <para>What it PLANTS is the record a real payment writes (the owed tag, and for the wiped row the
    /// seen tag), for a parcel minted the way the desk mints them. Everything after it is the shipped path:
    /// the gallery's poll, the chalk on the plan, the move on the card, the satchel, the tag, the book. It
    /// stands the captain at the island machine's own published spot (<see cref="HavenInterior.TheVendorsAt"/>)
    /// — inside the gallery, never a coordinate typed here — and says its line LAST, so the ashore row's own
    /// pulse cannot write over the one sentence that tells a tester which table (#1296's lesson).</para>
    /// </summary>
    private void PlantTheChalkIfAsked()
    {
        ChalkMark.Cheat cheat = Navigation is { } address ? ChalkMark.CheatIn(address.Uri) : ChalkMark.Cheat.None;
        if (cheat == ChalkMark.Cheat.None)
        {
            return;
        }

        string? berth = _dockedHavenId;
        IReadOnlyList<DeckReachability.Point> vendors = berth is null ? [] : HavenInterior.TheVendorsAt(berth);
        long now = PatronRota.WatchIndex(SimTime);
        string parcel = UnlistedParcel.FromTheDesk("chalk-dev", now).Id;
        if (berth is null || vendors.Count == 0
            || ChalkMark.For(parcel, berth, ChalkMark.PaidWatchFor(cheat, parcel, berth, now),
                TheGallerysTables(berth)) is not { } mark)
        {
            ShowPulseMessage(
                "🧪 DEV ?chalk= — this berth has no gallery with a table a drop could be left under. "
                + "Try &dock=selene-gate&ashore=1.");
            return;
        }

        _roomsTurnedOver.Add(mark.Owed);
        if (cheat == ChalkMark.Cheat.Wiped && mark.LastWindowBefore(now) is { } seen)
        {
            _roomsTurnedOver.Add(mark.SeenOn(seen));
        }

        if (OnTheConcourse)
        {
            DeckReachability.Point island = vendors[^1];
            StandCaptainAt(island.X, island.Y, "you come out of the tube into the gallery");
        }

        // The canon sentence says "first" or "second" and nothing about which end of the room. The TESTER is
        // told which machine the table stands in front of — the one the cross is chalked beside, which the
        // wiped row cannot show (the stone is clean) and the up row at the same watch names the same table.
        ShowPulseMessage(
            $"🧪 DEV ?chalk={(cheat == ChalkMark.Cheat.Wiped ? "wiped" : "1")}: {mark.ThePaymentLine()} "
            + $"(table {mark.Table} of HavenInterior.GalleryTops — the one in front of the machine the cross "
            + (cheat == ChalkMark.Cheat.Wiped ? "was beside)" : "is beside)"));
    }
}

using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #794 slice 1 · <b>THE CHALK MARK — the page's half.</b> Core decides everything (<see cref="ChalkMark"/>:
/// which bench, which watches, what the wall says); this file is the five moments Core cannot reach: the
/// payment that earns the return, the notice the captain walks up to, the chalk on the plan, the move on the
/// bench's card, and the dev start.
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

    /// <summary>
    /// #794 · Called at the desk the moment a delivery is PAID. When the ground that was dug keeps a park,
    /// the return is owed (one tag) and the payment pulse gets the design's sentence appended; anywhere
    /// else, nothing at all — no drop, no sentence.
    /// </summary>
    private string TheReturnIsOwed(ParcelDrop.Payment paid)
    {
        if (ChalkMark.TheParkUnder(paid.Where.BodyId, MoonSurface.ExpeditionField()) is not { } park
            || ChalkMark.For(paid.ParcelId, paid.Where.BodyId, PatronRota.WatchIndex(SimTime), in park)
                is not { } mark)
        {
            return "";
        }

        _roomsTurnedOver.Add(mark.Owed);
        return " " + mark.ThePaymentLine();
    }

    /// <summary>The returns owed on the floor the captain is standing on, with its park.</summary>
    private IReadOnlyList<ChalkMark> TheReturnsOwedHere(SurfaceExcursion ex, in UndergroundComplex.Park green) =>
        ChalkMark.OwedOn(_roomsTurnedOver, ex.Stop.Body.Id, in green);

    // ── THE NOTICE ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #794 · <b>THE WALL BY THE NOTICE.</b> Polled beside the park's attendance line. Standing at the notice
    /// while the mark is up files the mark's line once per window; after a wipe the captain SAW go up, the
    /// wipe's line once. The told-ness is a tag in the register, so a reload does not tell it twice.
    /// </summary>
    private void CheckTheChalkMark()
    {
        if (_surface is not { Floor: < 0 } ex || _viewObject is not null
            || TheGreenOnThisFloor(ex) is not { } green
            || !ChalkMark.AtTheNotice(in green, _avatarX, _avatarY))
        {
            return;
        }

        foreach (ChalkMark mark in TheReturnsOwedHere(ex, in green))
        {
            if (mark.AtTheGate(SimTime, _roomsTurnedOver) is { } beat)
            {
                _roomsTurnedOver.Add(beat.Tag);
                ShowAndFile(beat.Line, ChalkMark.Glyph, PulseRank.Beat);
                RequestVaultSave();
                return;
            }
        }
    }

    /// <summary>#794 · Where the chalk is on the plan this frame, or null — drawn only while the mark is up.</summary>
    private (double X, double Y)? TheChalkOnTheWall()
    {
        if (_surface is not { Floor: < 0 } ex || TheGreenOnThisFloor(ex) is not { } green)
        {
            return null;
        }

        foreach (ChalkMark mark in TheReturnsOwedHere(ex, in green))
        {
            if (mark.MarkIsUpAt(SimTime))
            {
                return ChalkMark.WhereOnTheWall(in green);
            }
        }
        return null;
    }

    // ── THE BENCH ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The return under the bench the captain is sitting on, if the move is on offer right now.</summary>
    private ChalkMark? TheDropUnderThisSlat(SurfaceExcursion ex, TableTalk t)
    {
        if (t.SharedSeat || TheGreenOnThisFloor(ex) is not { } green)
        {
            return null;
        }

        foreach (ChalkMark mark in TheReturnsOwedHere(ex, in green))
        {
            if (mark.IsOnOffer(t.BenchIndex, t.SharedSeat, SimTime)
                && Satchel.CanTake(_satchel, mark.TheParcelUnderTheSlat(TheLandableGround())))
            {
                return mark;
            }
        }
        return null;
    }

    /// <summary>
    /// #794 · <b>THE CARD SAYS WHAT IS UNDER THE PLANK, AND NOTHING ELSE DOES.</b> Each frame while the
    /// captain is on a bench: the card carries FEEL UNDER THE SLAT exactly while there is something to feel
    /// for — alone, on the named bench, in the window or the watch after it — and is the plain bench's card
    /// otherwise. With nothing owed it never touches the card at all.
    ///
    /// <para>A pocket too full to take the packet is a state the design wrote no line for, so it is left
    /// silent: the move is simply absent.</para>
    /// </summary>
    private void KeepTheSlatHonest()
    {
        if (_seating.Table is not { Bench: true } t || _surface is not { Floor: < 0 } ex
            || t.Scene.Id != ParkBenches.TheBench(t.SharedSeat).Id)
        {
            return;
        }

        bool goods = TheDropUnderThisSlat(ex, t) is not null;
        if (ChalkMark.Offers(t.Scene) != goods)
        {
            t.Scene = ChalkMark.TheBench(t.SharedSeat, goods);
            StateHasChanged();
        }
    }

    /// <summary>
    /// #794 · <b>FEEL UNDER THE SLAT.</b> Taken ahead of the seat's own dispatch because it moves things the
    /// seat does not own: the packet into the satchel, the collection into the register (once — it is what
    /// ends the return), the entry into the field book. The line is said on the card, where the captain is
    /// looking (#680). True when the press was this move, whatever came of it.
    /// </summary>
    private bool TheSlatIsFelt(string moveId)
    {
        if (moveId != ChalkMark.FeelUnderTheSlat)
        {
            return false;
        }

        if (_seating.Table is not { Bench: true } t || _surface is not { Floor: < 0 } ex)
        {
            return true;
        }

        if (TheDropUnderThisSlat(ex, t) is not { } mark)
        {
            // The watch turned while the hand was on its way: the move goes, and nothing is said. A drop that
            // is not there is not a sentence.
            t.Scene = ChalkMark.TheBench(t.SharedSeat, goodsUnderThisSlat: false);
            StateHasChanged();
            return true;
        }

        _satchel = [.. Satchel.Add(_satchel, mark.TheParcelUnderTheSlat(TheLandableGround()))];
        _roomsTurnedOver.Add(mark.CollectedOn(PatronRota.WatchIndex(SimTime)));
        FileNote(ChalkMark.CollectedEntry, ChalkMark.Glyph);

        t.Scene = ChalkMark.TheBench(t.SharedSeat, goodsUnderThisSlat: false);
        t.Outcome = ChalkMark.FeltLine;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #794 QA · <c>?park=1&amp;chalk=1</c> — a paid delivery on record, the clock at a window, the mark up;
    /// <c>?park=1&amp;chalk=wiped</c> — one watch later, the mark seen and wiped, the goods still under the
    /// slat. Called where <c>?park=1</c> stands the captain in the park.
    ///
    /// <para>What it PLANTS is the record a real payment writes (the owed tag, and for the wiped row the
    /// seen tag), for a parcel minted the way the desk mints them. The ground in the record is the ground
    /// the dev route landed on — <c>?park=1</c>'s cheat rock, which no desk's parcel can be addressed to — so
    /// that half is stated rather than earned. Everything after it is the shipped path: the notice, the
    /// chalk on the plan, the move on the card, the satchel, the tag, the book.</para>
    /// </summary>
    private void PlantTheChalkIfAsked(SurfaceExcursion ex, in UndergroundComplex.Park green)
    {
        ChalkMark.Cheat cheat = Navigation is { } address ? ChalkMark.CheatIn(address.Uri) : ChalkMark.Cheat.None;
        if (cheat == ChalkMark.Cheat.None)
        {
            return;
        }

        long now = PatronRota.WatchIndex(SimTime);
        string body = ex.Stop.Body.Id;
        string parcel = UnlistedParcel.FromTheDesk("chalk-dev", now).Id;
        long paid = ChalkMark.PaidWatchFor(cheat, parcel, body, now);
        if (ChalkMark.For(parcel, body, paid, in green) is not { } mark)
        {
            ShowPulseMessage("🧪 DEV ?chalk= — this park has no bench a drop could be left under.");
            return;
        }

        _roomsTurnedOver.Add(mark.Owed);
        if (cheat == ChalkMark.Cheat.Wiped && mark.LastWindowBefore(now) is { } seen)
        {
            _roomsTurnedOver.Add(mark.SeenOn(seen));
        }

        ShowPulseMessage(
            $"🧪 DEV ?chalk={(cheat == ChalkMark.Cheat.Wiped ? "wiped" : "1")}: {mark.ThePaymentLine()}");
    }
}

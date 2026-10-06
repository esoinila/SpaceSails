using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1151 slice 1 · <b>THE LOSS, THE FORM, THE BOOKING — ON THE PAGE.</b> Three presses and one clock, every word
/// Core's (<see cref="HullClaim"/>, Fable canon verbatim) and every fact a tag in the durable register
/// (<c>_roomsTurnedOver</c>, #711/#794's idiom — the vault carries it, so a reload can neither forget a claim nor claim
/// a loss twice).
///
/// <list type="number">
/// <item><b>The loss.</b> The sail's mend window is the one the page already keeps (<c>_sailHoled</c>,
/// <c>_sailRepairedAtSimTime</c>); this file adds the FIRST-HOLE time beside it and, on the frame the window completes,
/// files the loss line and the loss tag. No credit moves (the amended cut: the game prices no repair).</item>
/// <item><b>The form.</b> On the ship's own desk (seat-tied, the 2026-08-30 law — never place-tied) a verb copies the
/// newest unclaimed loss onto the blank form.</item>
/// <item><b>The booking.</b> A console in the adjuster's room, on her shipped watch, with a filled form held.</item>
/// </list>
///
/// <para>Nothing here is a popup and nothing needs closing: the verb is a strip button, the console is the room's own
/// fixture, the lines are pulses and the book's entries.</para>
/// </summary>
public partial class Map
{
    /// <summary>The window the crew is sewing through — its first hole and its end — or null with the sail whole.</summary>
    private HullClaim.Mend? _sailMend;

    /// <summary>#1151 · The sail has just been holed (the cloud-top dip, or a bad roll on the haze pass): open the mend
    /// window — or, inside one already open, extend the same loss — and keep the page's own clear time in step.</summary>
    /// <summary>#1151 · A load or a new voyage begins a life with a whole sail: the mend window of the life before is
    /// not this one's. (The timer leaked across lives before the claim existed — a holed sail applied over a loaded
    /// save cleared in the new life — and the claim machinery is what made it consequential: it would have filed the
    /// last life's loss in this one.)</summary>
    private void ForgetTheMend()
    {
        _sailMend = null;
        _sailHoled = false;
        _sailRepairedAtSimTime = 0;
    }

    private void TheSailIsHoled()
    {
        HullClaim.Mend mend = HullClaim.Hole(_sailMend, _ship.SimTime, SailRepairSeconds);
        _sailMend = mend;
        _sailRepairedAtSimTime = mend.ClearsAt;
    }

    /// <summary>#1151 · <b>THE MEND IS DONE.</b> The loss line goes into the book — Nebula Mutual and the place she is
    /// at when the crew finishes — and the loss is entered in the register, once per completed window, with the days the
    /// mend actually took (first hole to the frame it cleared).</summary>
    private void TheMendIsDone()
    {
        if (_sailMend is not { } mend)
        {
            return;
        }

        _sailMend = null;
        RecordTheLoss(HullClaim.LossOf(mend, _ship.SimTime));
    }

    private void RecordTheLoss(HullClaim.Loss loss)
    {
        if (!_roomsTurnedOver.Add(HullClaim.LossTag(loss)))
        {
            return;
        }

        FileNoteAbout(HullClaim.LossLine(loss.Tenths), HullClaim.LossGlyph, HullClaim.SubjectsFor(TheBooksNameForHere()));
        RequestVaultSave();
    }

    // ── THE DESK VERB ───────────────────────────────────────────────────────────────────────────────────

    /// <summary><b>IS "✍ COPY THE LOSS ONTO THE CLAIM FORM" ON OFFER?</b> Seat-tied: only while the captain is sitting at
    /// the ship's own desk — and only with the blank form held and an unclaimed loss in the book.</summary>
    private bool ClaimFormFillable =>
        SeatedTable is { Plate: SittingAlone.OwnDeskPlate } && HullClaim.CanFillTheForm(_satchel, _roomsTurnedOver);

    /// <summary>#1151 · The press: the newest unclaimed loss is copied onto the form (the paper changes, the loss is
    /// filed) and the desk says <see cref="HullClaim.FilledLine"/>. One loss per form; a stale press changes nothing and is
    /// silent.</summary>
    private void FillTheClaimForm()
    {
        if (!ClaimFormFillable || HullClaim.Fill(_satchel, _roomsTurnedOver) is not { } filled)
        {
            return;
        }

        _satchel = [.. filled.Satchel];
        _roomsTurnedOver.Add(HullClaim.FiledTag(filled.Loss.DoneAt));
        RequestVaultSave();
        ShowPulseMessage(HullClaim.FilledLine);
        StateHasChanged();
    }

    // ── THE ADJUSTER'S ROOM ─────────────────────────────────────────────────────────────────────────────

    private static bool IsTheAdjusters(SideOffice office) => ReferenceEquals(office, SideOffices.Adjuster);

    /// <summary>#1151 · Is there a sheet on this office's desk? As #1365 shipped it (one paper, once per run) — and, in
    /// the adjuster's room only, a fresh blank whenever there is something to claim (an unclaimed loss) and the captain
    /// holds no blank: paper appears only when there is something to put on it, one blank at a time ("a second blank form
    /// can be fetched from the cold rooms the way the first was"). A captain who never lost a day finds the room as
    /// #1365 shipped it.</summary>
    private bool TheDeskHasASheet(SideOffice office) =>
        !_roomsTurnedOver.Contains(office.SheetTakenTag)
        || (IsTheAdjusters(office) && HullClaim.NewestUnclaimed(_roomsTurnedOver) is not null
            && !HullClaim.HoldsTheBlank(_satchel));

    /// <summary>#1151 · The console that books the claim: the adjuster's room, her watch, a filled form held.</summary>
    private bool TheBookingConsoleIsOnOffer(SideOffice office) =>
        IsTheAdjusters(office) && HullClaim.HoldsAFilledForm(_satchel) && ItIsTheOfficesWatch(office);

    /// <summary>#1151 · What the shut door says: the shipped line — and, in the adjuster's room with a filled form in
    /// the satchel, the extra line after it.</summary>
    private string TheShutDoorSays(SideOffice office) =>
        IsTheAdjusters(office) && HullClaim.HoldsAFilledForm(_satchel)
            ? office.ShutLine + " " + HullClaim.ShutExtraLine
            : office.ShutLine;

    /// <summary>#1151 · <b>[E] AT THE BOOKING CONSOLE.</b> Recognised by its own plate, the way the sheet is. The first
    /// press for a filled form writes the vault row (the BOOKED tag) and the book's 📍 entry, once; every press says the
    /// booked line.</summary>
    private bool TryTheBookingConsole(string label)
    {
        if (!string.Equals(label, HullClaim.BookLabel, StringComparison.Ordinal)
            || _dockedHavenId is not { } berth || HavenInterior.TheOfficeAt(berth) is not { } office
            || !IsTheAdjusters(office))
        {
            return false;
        }

        if (!TheBookingConsoleIsOnOffer(office))
        {
            return true;
        }

        foreach (Satchel.Item held in _satchel)
        {
            if (held.Kind == Satchel.Kind.Paper && HullClaim.TryReadFilled(held.Id, out HullClaim.Loss loss)
                && _roomsTurnedOver.Add(HullClaim.BookedTag(loss.DoneAt)))
            {
                FileNoteAbout(HullClaim.BookEntryLine, HullClaim.BookGlyph, office.Subjects(_havenName));
                RequestVaultSave();
                break;
            }
        }

        ShowPulseMessage(HullClaim.BookedLine);
        StateHasChanged();
        return true;
    }

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1151 QA · <c>?claim=1</c> — a COMPLETED mend is in the book and the blank form is in the sleeve, at The
    /// Deep's hotel level with the adjuster's door ajar (the <c>office=open</c> idiom). Staged through the same writers
    /// the real path uses, never a typed-in tag.</summary>
    private void StageTheClaimIfAsked()
    {
        if (!HullClaim.CheatIn(Navigation?.Uri))
        {
            return;
        }

        double now = SimTime;
        RecordTheLoss(HullClaim.LossOf(HullClaim.Hole(null, now - SailRepairSeconds, SailRepairSeconds), now));
        SideOffice office = SideOffices.Adjuster;
        if (!HullClaim.HoldsTheBlank(_satchel) && Satchel.CanTake(_satchel, office.TheSheet))
        {
            _satchel = [.. Satchel.Add(_satchel, office.TheSheet)];
        }

        _roomsTurnedOver.Add(office.SheetTakenTag);
        RequestVaultSave();
    }
}

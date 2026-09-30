using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1332 C · <b>THE PRESERVATION OFFICE, ON THE PAGE</b> — the door on Ringside Exchange's hotel level with the cost
/// centre on it: whether it stands open this frame, what a press at it says, the book's line the first time the
/// plate is read, the one thing told the first time the captain is inside while it is open, the sheet on the desk,
/// and the dev start. The clerk who keeps its hours is <c>Map.PreservationOffice.Clerk.cs</c>.
///
/// <para>Every word is Core's (<see cref="PreservationOffice"/>, Fable canon, verbatim) and so is every clock: the
/// clerk's watch is <see cref="PreservationOffice.IsTheClerksWatch"/> of the run's own seed (<see cref="WorldSeed"/>)
/// and the watch the sim is on. The room is <c>HavenInterior.Office.cs</c>'s.</para>
///
/// <para><b>No new page field.</b> "The plate has been read", "the sheet is gone" and "the warm line was told" are
/// tags on <c>_roomsTurnedOver</c>, the durable register the chalk mark and the man at the door already write
/// into; whether the door stands open is asked of the clock (and read back off the plan that is drawn); the dev
/// start is read off the address bar, the man at the door's way.</para>
/// </summary>
public partial class Map
{
    /// <summary>#1332 C · The dev latch, read off the address — <see cref="PreservationOffice.CheatIn"/>.</summary>
    private PreservationOffice.Cheat TheOfficeCheat =>
        Navigation is { } address ? PreservationOffice.CheatIn(address.Uri) : PreservationOffice.Cheat.None;

    /// <summary>#1332 C · <b>IS THIS THE CLERK'S WATCH?</b> The run's own answer (one watch in four, seeded on the
    /// run), unless the dev latch forces it.</summary>
    private bool ItIsTheClerksWatch() => TheOfficeCheat switch
    {
        PreservationOffice.Cheat.Open => true,
        PreservationOffice.Cheat.Shut => false,
        _ => PreservationOffice.IsTheClerksWatch(WorldSeed, PatronRota.WatchIndex(SimTime)),
    };

    /// <summary>
    /// #1332 C · <b>HOW THE OFFICE'S DOOR STANDS, THIS FRAME.</b> Shut every watch but the clerk's; ajar on his; and
    /// the desk bare once the sheet has been taken this run.
    ///
    /// <para><b>…and never shut on a man inside.</b> A captain still in the office when the clerk's watch turns
    /// keeps the doorway until he steps out of it: a leaf locked with the captain behind it is a room with no way
    /// out, which the fire code (#822) exists to forbid. The watch is not lengthened for anybody else — the next
    /// frame he is in the corridor, the door is shut.</para>
    /// </summary>
    private HavenInterior.OfficeDoor TheOfficeAsItStands()
    {
        if (_dockedHavenId is not { } berth || !HavenInterior.HasTheOffice(berth)
            || (!ItIsTheClerksWatch() && !HavenInterior.InTheOffice(berth, _avatarX, _avatarY, _havenFloor)))
        {
            return HavenInterior.OfficeDoor.Shut;
        }

        return _roomsTurnedOver.Contains(PreservationOffice.SheetTakenTag)
            ? HavenInterior.OfficeDoor.AjarDeskBare
            : HavenInterior.OfficeDoor.Ajar;
    }

    /// <summary>#1332 C · …and how the plan under the captain's feet DRAWS it, read off the plan itself.</summary>
    private HavenInterior.OfficeDoor TheOfficeAsDrawn() =>
        !HavenInterior.TheOfficeStandsOpenIn(_deckPlan) ? HavenInterior.OfficeDoor.Shut
        : HavenInterior.TheSheetLiesIn(_deckPlan) ? HavenInterior.OfficeDoor.Ajar
        : HavenInterior.OfficeDoor.AjarDeskBare;

    /// <summary>
    /// #1332 C · <b>ONE FRAME OF THE OFFICE'S HOURS</b>, on the hotel level of the station that has it and nowhere
    /// else. The door follows the clock (a rebuild only on the frame the two disagree — the watch turning, or the
    /// sheet going), the clerk walks his leg, and the first step inside while it stands open is told once.
    /// </summary>
    private void KeepTheOfficeHours(in HavenInterior.BarFloor bar)
    {
        if (_havenFloor != HavenLevels.ServiceLevel || !HavenInterior.HasTheOffice(bar.BodyId))
        {
            return;
        }

        if (TheOfficeAsItStands() != TheOfficeAsDrawn())
        {
            RebuildDockedDeck();
            StateHasChanged();
        }

        AdvanceTheClerk(bar);
        TellTheWarmChairOnce(bar.BodyId);
    }

    /// <summary>
    /// #1332 C · <b>INSIDE, WHILE IT STANDS OPEN — TOLD ONCE PER RUN.</b> Fable canon, verbatim
    /// (<see cref="PreservationOffice.WarmStillLine"/>). On a free slot only, at Status rank, filed nowhere; the
    /// register keeps that it was said.
    /// </summary>
    private void TellTheWarmChairOnce(string berth)
    {
        if (!HavenInterior.InTheOffice(berth, _avatarX, _avatarY, _havenFloor)
            || !HavenInterior.TheOfficeStandsOpenIn(_deckPlan)
            || _pulse.Message is not null
            || _roomsTurnedOver.Contains(PreservationOffice.WarmToldTag))
        {
            return;
        }

        _roomsTurnedOver.Add(PreservationOffice.WarmToldTag);
        ShowPulseMessage(PreservationOffice.WarmStillLine, PulseRank.Status);
        RequestVaultSave();
    }

    /// <summary>
    /// #1332 C · <b>[E] AT THE OFFICE'S DOOR.</b> Recognised by its own plate, the way the dropped schedule is, so the
    /// ring's knock adds no dispatch. While the door is shut the press is told the office's line, every press, as
    /// the Hive's locked leaves are; while it stands open there is nothing to knock on and nothing is said. Either
    /// way, the first time the plate is read the book files one line under the Authority and the haven.
    /// </summary>
    /// <returns>Whether this press was the office door's.</returns>
    private bool TryTheOfficeDoor(in DeckPlan.ConsoleSpot hatch)
    {
        if (!string.Equals(hatch.Label, PreservationOffice.DoorPlate, StringComparison.Ordinal)
            || _dockedHavenId is not { } berth || !HavenInterior.HasTheOffice(berth))
        {
            return false;
        }

        if (!HavenInterior.TheOfficeStandsOpenIn(_deckPlan))
        {
            ShowPulseMessage(PreservationOffice.ShutLine);
        }

        if (_roomsTurnedOver.Add(PreservationOffice.PlateReadTag))
        {
            FileNoteAbout(
                PreservationOffice.PlateReadLine, PreservationOffice.PlateReadGlyph,
                PreservationOffice.SubjectsFor(_havenName));
            RequestVaultSave();
        }

        StateHasChanged();
        return true;
    }

    /// <summary>
    /// #1332 C · <b>THE SHEET ON THE DESK.</b> The building's own paper pick-up, the dropped schedule's (#1061): the
    /// sleeve is asked first, and a sleeve that will not take it leaves the sheet where it lies and says so in
    /// Core's words; once it has gone in, the card goes up with the sheet read as papers read away from their room,
    /// the book files the document once under the Authority and the haven, and the desk is bare for the run.
    /// </summary>
    /// <returns>Whether this press was the sheet's.</returns>
    private bool TryTheDisbursementSheet(string label)
    {
        if (!string.Equals(label, PreservationOffice.SheetTitle, StringComparison.Ordinal)
            || _dockedHavenId is not { } berth || !HavenInterior.HasTheOffice(berth)
            || _roomsTurnedOver.Contains(PreservationOffice.SheetTakenTag))
        {
            return false;
        }

        Satchel.Item sheet = PreservationOffice.TheSheet;
        if (!Satchel.CanTake(_satchel, sheet))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine.Trim());
            return true;
        }

        _satchel = [.. Satchel.Add(_satchel, sheet)];
        _roomsTurnedOver.Add(PreservationOffice.SheetTakenTag);

        CarriedObject.Reveal read = CarriedObject.PaperReveal(PreservationOffice.SheetId);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, read.ArtUrl, read.Story, UndergroundComplex.PaperPocketLine.Trim());

        FileNoteAbout(
            PreservationOffice.SheetDocument, PreservationOffice.SheetGlyph,
            PreservationOffice.SubjectsFor(_havenName));

        RebuildDockedDeck();
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    /// <summary>
    /// #1332 C QA · <c>?dock=ringside-exchange&amp;ashore=1&amp;havenfloor=-1&amp;office=shut|open</c>. Called right
    /// after <c>?havenfloor=-1</c> has ridden the first car down. <c>shut</c> stands the captain at the office's
    /// doorstep, under its plate; <c>open</c> stands him two doors west along the corridor, looking at it, with the
    /// door ajar and the clerk a few seconds from stepping out and walking to his car. Says its line LAST.
    /// </summary>
    private void StandAtTheOfficeIfAsked()
    {
        PreservationOffice.Cheat cheat = TheOfficeCheat;
        if (cheat == PreservationOffice.Cheat.None)
        {
            return;
        }

        if (_havenFloor != HavenLevels.ServiceLevel
            || HavenInterior.TheOfficeDoorstepAt(_dockedHavenId) is not { } door)
        {
            ShowPulseMessage(
                "🏛 Test: ?office= needs Ringside Exchange's hotel level. Try "
                + "/map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut.");
            return;
        }

        if (cheat == PreservationOffice.Cheat.Open)
        {
            StandCaptainAshoreAt(door.X - (2 * OfficeDevStandOffDu), door.Y);
            ShowPulseMessage(
                "🏛 Test: the clerk's watch at Ringside Exchange — the door two along stands ajar; in a few seconds "
                + "a walker plated Clerk steps out and walks to his car. The sheet is on the desk inside.");
            return;
        }

        StandCaptainAshoreAt(door.X, door.Y);
        ShowPulseMessage("🏛 Test: at the Preservation office's door, shut. [E] reads the plate and the office's line.");
    }

    /// <summary>#1332 C QA · About one cabin's frontage — how far along the corridor the open dev start stands the
    /// captain per door, so he is two doors down and not in the clerk's way.</summary>
    private const double OfficeDevStandOffDu = 2.95;
}

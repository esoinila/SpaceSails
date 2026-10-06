using System;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #1332 C/D/E · <b>THE SIDE OFFICES, ON THE PAGE</b> — the door on a haven's hotel level with an office's plate on it
/// (Ringside Exchange's Preservation office, Cinder Roost's forwarding desk, The Deep's adjuster's room — one kit,
/// <see cref="SideOffice"/>): whether it stands open this frame, what a press at it says, the book's line the first
/// time the plate is read, the one thing told the first time the captain is inside while it is open, the paper on the
/// desk, and the dev start. The clerk who keeps the Preservation office's hours (and only his) is
/// <c>Map.PreservationOffice.Clerk.cs</c>.
///
/// <para>Every word is Core's (<see cref="PreservationOffice"/>, <see cref="ForwardingDesk"/>,
/// <see cref="AdjustersRoom"/>, Fable canon, verbatim) and so is every clock: an office's ajar watch is
/// <see cref="SideOffice.IsAjarOn"/> of the run's own seed (<see cref="WorldSeed"/>) and the watch the sim is on. The
/// room is <c>HavenInterior.Office.cs</c>'s.</para>
///
/// <para><b>No new page field.</b> "The plate has been read" and "the paper is gone" are tags on
/// <c>_roomsTurnedOver</c>, the durable register the chalk mark and the man at the door already write into; "the
/// ajar line was told" is a key in the page's one told-once memory (<c>_toldOnce</c>, #653 slice 2); whether the door
/// stands open is asked of the clock (and read back off the plan that is drawn); the dev start is read off the
/// address bar, the man at the door's way.</para>
/// </summary>
public partial class Map
{
    /// <summary>#1332 C · The dev latch, read off the address — <see cref="PreservationOffice.CheatIn"/>.</summary>
    private PreservationOffice.Cheat TheOfficeCheat =>
        Navigation is { } address ? PreservationOffice.CheatIn(address.Uri) : PreservationOffice.Cheat.None;

    /// <summary>#1332 C/D/E · <b>IS THIS THE WATCH THE OFFICE STANDS AJAR?</b> The run's own answer (the office's
    /// one watch in <see cref="SideOffice.WatchesPerTurn"/>, seeded on the run), unless the dev latch forces it.</summary>
    private bool ItIsTheOfficesWatch(SideOffice office) => TheOfficeCheat switch
    {
        PreservationOffice.Cheat.Open => true,
        PreservationOffice.Cheat.Shut => false,
        _ => office.IsAjarOn(WorldSeed, PatronRota.WatchIndex(SimTime)),
    };

    /// <summary>#1332 C · <b>IS THIS THE CLERK'S WATCH?</b> The Preservation office's own hours.</summary>
    private bool ItIsTheClerksWatch() => ItIsTheOfficesWatch(SideOffices.Preservation);

    /// <summary>
    /// #1332 C/D/E · <b>HOW THE OFFICE'S DOOR STANDS, THIS FRAME.</b> Shut every watch but the office's own; ajar on
    /// it; and the desk bare once the paper has been taken this run.
    ///
    /// <para><b>…and never shut on a man inside.</b> A captain still in the office when the watch turns keeps the
    /// doorway until he steps out of it: a leaf locked with the captain behind it is a room with no way out, which
    /// the fire code (#822) exists to forbid. The watch is not lengthened for anybody else — the next frame he is in
    /// the corridor, the door is shut.</para>
    /// </summary>
    private HavenInterior.OfficeDoor TheOfficeAsItStands()
    {
        if (_dockedHavenId is not { } berth || HavenInterior.TheOfficeAt(berth) is not { } office
            || (!ItIsTheOfficesWatch(office) && !HavenInterior.InTheOffice(berth, _avatarX, _avatarY, _havenFloor)))
        {
            return HavenInterior.OfficeDoor.Shut;
        }

        return _roomsTurnedOver.Contains(office.SheetTakenTag)
            ? HavenInterior.OfficeDoor.AjarDeskBare
            : HavenInterior.OfficeDoor.Ajar;
    }

    /// <summary>#1332 C · …and how the plan under the captain's feet DRAWS it, read off the plan itself.</summary>
    private HavenInterior.OfficeDoor TheOfficeAsDrawn() =>
        !HavenInterior.TheOfficeStandsOpenIn(_deckPlan) ? HavenInterior.OfficeDoor.Shut
        : HavenInterior.TheSheetLiesIn(_deckPlan) ? HavenInterior.OfficeDoor.Ajar
        : HavenInterior.OfficeDoor.AjarDeskBare;

    /// <summary>
    /// #1332 C/D/E · <b>ONE FRAME OF THE OFFICE'S HOURS</b>, on the hotel level of the station that has one and
    /// nowhere else. The door follows the clock (a rebuild only on the frame the two disagree — the watch turning, or
    /// the paper going), the Preservation clerk (and nobody else: D and E are empty rooms) walks his leg, and the
    /// first step inside while it stands open is told once.
    /// </summary>
    private void KeepTheOfficeHours(in HavenInterior.BarFloor bar)
    {
        if (_havenFloor != HavenLevels.ServiceLevel || HavenInterior.TheOfficeAt(bar.BodyId) is not { } office)
        {
            return;
        }

        if (TheOfficeAsItStands() != TheOfficeAsDrawn())
        {
            RebuildDockedDeck();
            StateHasChanged();
        }

        if (office.HasAClerk)
        {
            AdvanceTheClerk(bar);
        }

        TellTheAjarLineOnce(bar.BodyId, office);
    }

    /// <summary>
    /// #1332 C/D/E · <b>INSIDE, WHILE IT STANDS OPEN — TOLD ONCE PER RUN.</b> Fable canon, verbatim
    /// (<see cref="SideOffice.AjarLine"/>: the warm chair, the cold room, the two chairs). On a free slot only, at
    /// Status rank, filed nowhere; the page's one told-once memory keeps that it was said (<see cref="ToldOnce"/>,
    /// keyed by berth — one office to a haven), added on the frame the line reaches the slot and never before.
    /// </summary>
    private void TellTheAjarLineOnce(string berth, SideOffice office)
    {
        if (!HavenInterior.InTheOffice(berth, _avatarX, _avatarY, _havenFloor)
            || !HavenInterior.TheOfficeStandsOpenIn(_deckPlan)
            || _pulse.Message is not null
            || _toldOnce.Has(ToldOnce.Key(ToldOnce.OfficeAjar, berth)))
        {
            return;
        }

        _toldOnce.Tell(ToldOnce.Key(ToldOnce.OfficeAjar, berth));
        ShowPulseMessage(office.AjarLine, PulseRank.Status);
    }

    /// <summary>
    /// #1332 C/D/E · <b>[E] AT THE OFFICE'S DOOR.</b> Recognised by its own plate, the way the dropped schedule is, so
    /// the ring's knock adds no dispatch. While the door is shut the press is told the office's line, every press, as
    /// the Hive's locked leaves are; while it stands open there is nothing to knock on and nothing is said. Either
    /// way, the first time the plate is read the book files one line under the office's subjects and the haven.
    /// </summary>
    /// <returns>Whether this press was an office door's.</returns>
    private bool TryTheOfficeDoor(in DeckPlan.ConsoleSpot hatch)
    {
        if (_dockedHavenId is not { } berth || HavenInterior.TheOfficeAt(berth) is not { } office
            || !string.Equals(hatch.Label, office.DoorPlate, StringComparison.Ordinal))
        {
            return false;
        }

        if (!HavenInterior.TheOfficeStandsOpenIn(_deckPlan))
        {
            ShowPulseMessage(office.ShutLine);
        }

        if (_roomsTurnedOver.Add(office.PlateReadTag))
        {
            FileNoteAbout(office.PlateReadLine, PreservationOffice.PlateReadGlyph, office.Subjects(_havenName));
            RequestVaultSave();
        }

        StateHasChanged();
        return true;
    }

    /// <summary>
    /// #1332 C/D/E · <b>THE PAPER ON THE DESK.</b> The building's own paper pick-up, the dropped schedule's (#1061):
    /// the sleeve is asked first, and a sleeve that will not take it leaves the paper where it lies and says so in
    /// Core's words; once it has gone in, the card goes up with the paper read as papers read away from their room,
    /// the book files the document once under the office's subjects and the haven (every paper but the adjuster's
    /// blank form, which is a form and not evidence: it files nothing), and the desk is bare for the run.
    /// </summary>
    /// <returns>Whether this press was a desk paper's.</returns>
    private bool TryTheSheetOnTheDesk(string label)
    {
        if (_dockedHavenId is not { } berth || HavenInterior.TheOfficeAt(berth) is not { } office
            || !string.Equals(label, office.SheetTitle, StringComparison.Ordinal)
            || _roomsTurnedOver.Contains(office.SheetTakenTag))
        {
            return false;
        }

        Satchel.Item sheet = office.TheSheet;
        if (!Satchel.CanTake(_satchel, sheet))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine.Trim());
            return true;
        }

        _satchel = [.. Satchel.Add(_satchel, sheet)];
        _roomsTurnedOver.Add(office.SheetTakenTag);

        CarriedObject.Reveal read = CarriedObject.PaperReveal(office.SheetId);
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, read.ArtUrl, read.Story, UndergroundComplex.PaperPocketLine.Trim());

        if (office.FilesTheSheet)
        {
            FileNoteAbout(office.SheetDocument, PreservationOffice.SheetGlyph, office.Subjects(_havenName));
        }

        RebuildDockedDeck();
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    /// <summary>
    /// #1332 C/D/E QA · <c>?dock=ringside-exchange|cinder-roost|the-deep&amp;ashore=1&amp;havenfloor=-1&amp;office=shut|open</c>.
    /// Called right after <c>?havenfloor=-1</c> has ridden the first car down. <c>shut</c> stands the captain at the
    /// office's doorstep, under its plate. <c>open</c> forces the door ajar: at Ringside Exchange it stands him two
    /// doors west along the corridor, looking at it, with the clerk a few seconds from stepping out and walking to
    /// his car; at Cinder Roost and The Deep (empty rooms, no walker) it stands him at the doorstep itself, the door
    /// ajar before him and the paper on the desk inside. Says its line LAST.
    /// </summary>
    private void StandAtTheOfficeIfAsked()
    {
        PreservationOffice.Cheat cheat = TheOfficeCheat;
        if (cheat == PreservationOffice.Cheat.None)
        {
            return;
        }

        if (_havenFloor != HavenLevels.ServiceLevel
            || HavenInterior.TheOfficeAt(_dockedHavenId) is not { } office
            || HavenInterior.TheOfficeDoorstepAt(_dockedHavenId) is not { } door)
        {
            ShowPulseMessage(
                "🏛 Test: ?office= needs a hotel level with an office: Ringside Exchange, Cinder Roost or The Deep. "
                + "Try /map?dock=ringside-exchange&ashore=1&havenfloor=-1&office=shut.");
            return;
        }

        if (cheat == PreservationOffice.Cheat.Open)
        {
            if (!office.HasAClerk)
            {
                StandCaptainAshoreAt(door.X, door.Y);
                ShowPulseMessage(
                    $"🏛 Test: the door under {office.DoorPlate} stands ajar before you, whatever the watch; the room "
                    + "is empty and the paper is on the desk inside. [E] takes it; the first step in says one line, once.");
                return;
            }

            StandCaptainAshoreAt(door.X - (2 * OfficeDevStandOffDu), door.Y);
            ShowPulseMessage(
                "🏛 Test: the clerk's watch at Ringside Exchange — the door two along stands ajar; in a few seconds "
                + "a walker plated Clerk steps out and walks to his car. The sheet is on the desk inside.");
            return;
        }

        StandCaptainAshoreAt(door.X, door.Y);
        ShowPulseMessage($"🏛 Test: at the office's door, shut. [E] reads the plate ({office.DoorPlate}) and the office's line.");
    }

    /// <summary>#1332 C QA · About one cabin's frontage — how far along the corridor the open dev start stands the
    /// captain per door, so he is two doors down and not in the clerk's way.</summary>
    private const double OfficeDevStandOffDu = 2.95;
}

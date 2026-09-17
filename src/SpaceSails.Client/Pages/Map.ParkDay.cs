using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map (#870's family split) — #759's last named remainder, THE LIGHT KEEPS ITS OWN DAY:
// the one beat a captain who lingers in the park is told, and the three-field memory of what the two
// clocks said when they walked in. The light itself is Core's (SpaceSails.Core.ParkDay) and the drawing of
// it is the renderer's (DeckView.Frame.Ground); this file is only the noticing.

public sealed partial class Map
{
    /// <summary>
    /// #759 · <b>ANYONE WHO LINGERS NOTICES.</b> Owner, filing the park: <i>"The light keeps its own day —
    /// a grow-cycle that matches no watch of the building above or below. Anyone who lingers notices the
    /// park's morning arriving at the wrong time. Subtly wrong is the register: never broken, never
    /// right."</i>
    ///
    /// <para><b>What it takes to be told.</b> You have to still be in the room when the morning comes up —
    /// standing on the gravel or sat on a bench, it makes no difference which, because what is being
    /// noticed is a thing about the ROOM and not about the chair. Walk out and back and you have come in
    /// again, on whatever the lamps are doing then. And the building has to have been on some other part of
    /// its own day when you walked in, or there would be nothing wrong with this morning. Those three
    /// conditions are <see cref="ParkDay.WouldNotice"/>, stated in Core, exhaustively tested over its
    /// hundred and twenty-eight inputs, and read off the authored line rather than invented beside it.</para>
    ///
    /// <para><b>Once per captain per site</b>, in the register that rides the vault
    /// (<c>_roomsTurnedOver</c>) — this is a thing you work out about a place, and a place cannot be worked
    /// out twice. It is a PULSE and not a card for #761's reason: it is one sentence the captain is meant
    /// to catch out of the corner of an eye while doing something else, and a card would stop the room to
    /// announce it, which is the one register #759 forbids.</para>
    ///
    /// <para><b>And it never says why.</b> The park's second purpose is reserved arc material. The line
    /// reports two observations and puts them beside each other; the space between them is the whole
    /// beat.</para>
    /// </summary>
    private void CheckTheParksOwnDay()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            _parkDayCameInAt = null;
            return;
        }

        string site = ex.Stop.Body.Id;

        // Not in it — and the next time you are is a coming-in of its own. A captain who steps out of the
        // gate and back has not lingered through anything, and the sentence claims they did.
        if (TheGreenOnThisFloor(ex) is not { } green || !green.Contains(_avatarX, _avatarY))
        {
            _parkDayCameInAt = null;
            return;
        }

        // The moment of coming in, which is the half of the line the captain cannot see for themselves:
        // BOTH clocks are read here, and nothing is said on this tick, because nothing has changed yet.
        if (_parkDayCameInAt != site)
        {
            _parkDayCameInAt = site;
            _parkDayCameInOn = ParkDay.PhaseAt(SimTime, site);
            _parkDayBuildingWas = ParkDay.BuildingPhaseAt(SimTime);
            return;
        }

        string tag = ParkDay.NoticeTag(site);
        if (!ParkDay.WouldNotice(
                _parkDayCameInOn, _parkDayBuildingWas, ParkDay.PhaseAt(SimTime, site),
                _roomsTurnedOver.Contains(tag)))
        {
            return;
        }

        // Spent BEFORE it is said. The register is the whole of "once", and a line said first and written
        // down second is a line that says itself twice the day something throws between the two.
        _roomsTurnedOver.Add(tag);
        SayItWhereTheyAreLooking(ParkDay.LingerLine, PulseRank.Beat);
        FileNoteAbout(ParkDay.NoteLine, ParkDay.Glyph, ParkDay.Subjects(TheBooksNameForHere()));
        RequestVaultSave();
    }

    /// <summary>#759 · Which site's park the captain is currently standing in, or null. The latch that makes
    /// "came in" mean something: it is cleared the moment they are not on the gravel.</summary>
    private string? _parkDayCameInAt;

    /// <summary>#759 · What the lamps were doing when they walked in.</summary>
    private ParkDay.Phase _parkDayCameInOn;

    /// <summary>#759 · …and what the rest of the building was doing at that same moment, which is the claim
    /// the second half of the line makes and the reason it is read at the gate rather than at the beat.</summary>
    private ParkDay.Phase _parkDayBuildingWas;
}

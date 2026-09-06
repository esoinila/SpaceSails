using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #1149's inspection tag: the one
// paper in the building that is not evidence of anything, hanging on the valve of every pressure refuge.
public partial class Map
{
    // ── #1149 · THE COVERT ORGANISATION'S PARADOX, ON PAPER ──────────────────────────────────────────────
    //
    // Owner, ruling on #608: a secret lab "needs its own trusted criminals: the eternal struggle of covert
    // organizations — not to asphyxiate from unmaintained safety equipment, to keep secrets, to trust
    // employees to bend the law only as much as the company approves, while avoiding traceable bureaucracy
    // that could prove complicity if leaked."
    //
    // There is no sentence in that. There is a PAPER: a safety inspection, done on time, twice, a year
    // apart, on a room in a building nobody admits to owning — complete, current, and with nobody's name on
    // it. The tag says so itself, flatly, as a house rule: "No signature — none required."
    //
    // IT IS ON EVERY REFUGE, and that is the design rather than an omission. A tag that appeared only on the
    // interesting room would be the game pointing at the interesting room. A captain reads two or three of
    // these on ordinary floors, learns the form by heart, and then — on the one refuge in the building whose
    // seal went — finds the same form with one line too many in it, and no date on the extra line.
    //
    // WHY THE RACK'S OWN PRESS TAKES IT, and not a console of its own: the refuge is ONE room with ONE
    // console at its centre, NearestConsoleSpot picks exactly one, and a second spot at the same coordinates
    // would either be unreachable or would steal the press that reads the rack. The tag is on the valve; the
    // press that reads the valve takes it. Once, and then the press goes back to reading the gauge.
    //
    // WHY THIS FILE ADDS TO THE SATCHEL WHEN Map.Surface.Hive.cs MAY NOT: that law (#615/#678) is about
    // HAULS — every room a captain turns over goes through the keep-or-leave decision, so that no room can
    // quietly hand something over. This is not a haul and there is no room: it is an authored placement on a
    // fixture, exactly as #1061's dropped schedule and #804's false pass are, and both of those do their own
    // add and their own strike-off in their own files for this reason.

    /// <summary>#1149 · Take the inspection tag off this refuge's valve, if it is still there. Returns true
    /// when the press was spent on the tag — on the take, and on the refusal when the sleeve is full, which
    /// is a real answer and not a no-op.
    ///
    /// <para><b>The once-flag is the register that already exists.</b> <c>TheRoomHasBeenGoneThrough</c>
    /// writes both the live set the deck reads and the durable set the vault carries, keyed by body, floor
    /// and room index — and the tag's index is <see cref="UndergroundComplex.RefugeTagRoom"/>, which is
    /// negative and therefore an index no floor's room list can ever hold. So the tag is struck off by the
    /// one method in the client that may strike anything off, it survives the flight home like every other
    /// emptied room, and it cannot collide with a room.</para></summary>
    private bool TryTheInspectionTag(SurfaceExcursion ex)
    {
        if (ex.Floor >= 0 || !UndergroundComplex.RefugeOnThePlan(ex.Stop.Body.Id, ex.Floor))
        {
            return false;
        }

        int room = UndergroundComplex.RefugeTagRoom;
        if (ex.HiveRoomsEmptied.Contains(HiveInterior.RoomKey(ex.Floor, room)))
        {
            return false;
        }

        // The id the whole paper seam is built on: FindId mints it, AuthoredPaperOf round-trips it, and
        // PaperHeads names it. Nothing here knows what the tag is CALLED — that is the sleeve's question and
        // it is answered in one place for all six authored papers.
        string findId = UndergroundComplex.FindId(ex.Stop.Body.Id, ex.Floor, room);
        var tag = new Satchel.Item(Satchel.Kind.Paper, findId);
        if (!Satchel.CanTake(_satchel, tag))
        {
            ShowPulseMessage(UndergroundComplex.PocketFullLine.Trim());
            return true;
        }

        _satchel = [.. Satchel.Add(_satchel, tag)];
        TheRoomHasBeenGoneThrough(ex, ex.Floor, room);
        ShowAndFile(
            UndergroundComplex.InspectionTagLine(ex.Stop.Body.Id, ex.Floor)
            + UndergroundComplex.PaperPocketLine,
            "📋");
        RequestVaultSave();
        return true;
    }
}

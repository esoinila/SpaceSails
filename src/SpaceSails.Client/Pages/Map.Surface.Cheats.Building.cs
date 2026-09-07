using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// INSIDE THE BUILDING — <c>?ringoffice=1</c> (booted inside a room on the ring, facing its own glass,
/// #813), the front door, and the goods hoist.
///
/// <para>Split out of <c>Map.Surface.Cheats.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    /// <summary>#813 QA · <c>?ringoffice=1</c> — booted INSIDE a room on the ring, facing its own glass.
    /// Set in Map.Sim's cheat parse.</summary>
    private bool _ringOfficeCheat;

    /// <summary>
    /// #813 QA · Stand the captain INSIDE a ring office, a few paces back from its window wall.
    ///
    /// <para>Every other park row in this file stands a tester on the GRAVEL, which is the park's side of
    /// the glass and the only side anybody has ever been shown. The Manhattan ruling's claim is about the
    /// OTHER side of it — <i>"make sure the park prime real estate is not wasted and not unused"</i> — and
    /// prime real estate is a thing you check by standing in the room that paid for the view and looking
    /// out of it. Until this row there was no URL in the game that put you in one.</para>
    ///
    /// <para>Off the ring's own published frontage (<see cref="UndergroundComplex.Park.Frontage"/>): the
    /// first room with a view on the SPINE side, which is the premium band, falling back to any room with a
    /// view at all. Nothing in that list is ever the hall — the bar is published as this floor's Amenity
    /// and as the park's own Window, and Core keeps it out of the ring rather than mirror it — so this
    /// cannot quietly boot a tester into the room four other URLs already reach.</para>
    ///
    /// <para>The spot is measured off the room's OWN two walls rather than typed: back from the view wall
    /// toward the room's middle, by a few paces or by half the room's depth where the room is shallower
    /// than that. A number typed here would be right for the fifty-du near band and inside the back wall of
    /// a thirteen-du store.</para></summary>
    private void StandInARingOfficeIfAsked(SurfaceExcursion ex)
    {
        if (!_ringOfficeCheat || ex.Floor >= 0)
        {
            return;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green)
        {
            return;
        }

        UndergroundComplex.RingRoom? chosen = null;
        foreach (UndergroundComplex.RingRoom room in green.Frontage)
        {
            if (!room.HasView)
            {
                continue;   // a corner room stands past the end of the park's wall and has nothing to see
            }
            if (room.Side == UndergroundComplex.RingSide.Near)
            {
                chosen = room;
                break;
            }
            chosen ??= room;
        }
        if (chosen is not { } office || office.View is not { } glass)
        {
            return;
        }

        // A FEW PACES BACK FROM THE GLASS, toward the room's own middle — so the pane is in front of the
        // captain and the green is through it. WHICH WAY back is comes from the room and not from the side:
        // the near band looks down at the park and the far band looks up at it, and this asks rather than
        // assumes, which is the same reason the front-door row asks the shaft which face the hall is on.
        const double PacesBackFromTheGlass = 6.0;
        bool horizontal = Math.Abs(glass.Y1 - glass.Y2) < 0.001;
        double depth = horizontal ? office.Y1 - office.Y0 : office.X1 - office.X0;
        double back = Math.Min(PacesBackFromTheGlass, depth / 2.0);
        double standX = horizontal ? office.X : glass.X1 + (Math.Sign(office.X - glass.X1) * back);
        double standY = horizontal ? glass.Y1 + (Math.Sign(office.Y - glass.Y1) * back) : office.Y;

        StandCaptainAt(standX, standY, "you step in off the street and the whole front wall is green");
        ShowPulseMessage(
            "🧪 DEV ?ringoffice=1: INSIDE a room on the ring, looking OUT through its glass at the park — "
            + $"{office.Plate}. The door behind you is on a street; the wall in front of you is a window "
            + "and it still stops you. Then walk the block: every side of the green is somebody's front "
            + "wall now.");
    }

    /// <summary>#775 QA · Stand the captain OUT ON THE SPINE, at the hall's front door.
    ///
    /// <para>The captain is put on the corridor side of the room's FIRST published opening on the spine —
    /// the entrance, the one the carve placed at the lift — so the walk that proves the feature is the one
    /// the owner could not make: face the wall, read the plate, step through. Off the hall's own published
    /// door list, never a coordinate measured off a screenshot.</para></summary>
    private void StandAtTheFrontDoorIfAsked(SurfaceExcursion ex)
    {
        if (!_frontDoorCheat || ex.Floor >= 0)
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        (_, double shaftY) = UndergroundComplex.ShaftAt(field);

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, field).Amenities)
        {
            if (a.Hall is not { } hall)
            {
                continue;
            }
            foreach (SurfaceLayout.Doorway d in hall.Openings)
            {
                if (Math.Abs(d.Y1 - d.Y2) > 0.001)
                {
                    continue;   // a gap in the rib's face, which is the way in this issue is about NOT using
                }

                // Two du out of the doorway, on whichever side the spine is — asked of the shaft rather
                // than assumed, because half the floors in the game hang their hall off the other face.
                double standY = d.Y1 + (d.Y1 < shaftY ? 2.0 : -2.0);
                StandCaptainAt((d.X1 + d.X2) / 2.0, standY,
                    "you come along the main corridor and stop at the door");
                ShowPulseMessage(
                    "🧪 DEV ?frontdoor=1: out on the MAIN CORRIDOR at the canteen's own entrance — the "
                    + "plate is on the wall beside you. Walk in. Then walk the corridor and count the "
                    + "others: a hall this size is required to have them.");
                return;
            }
        }
    }

    /// <summary>#775 QA · Stand the captain in front of the GOODS HOIST, on the hall floor.
    ///
    /// <para>On the fixture's own published plate spot. Pressing the shutter reads the refusal; there is
    /// nothing else to do with it, and that is the whole of the feature.</para></summary>
    private void StandAtTheGoodsHoistIfAsked(SurfaceExcursion ex)
    {
        if (!_freightCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (a.Hall is not { Freight: { } hoist })
            {
                continue;
            }
            StandCaptainAt(hoist.PlateX, hoist.PlateY, "you cross to the shutter at the end of the counter");
            ShowPulseMessage(
                "🧪 DEV ?freight=1: THE GOODS HOIST, at the end of the counter's own service band. Press E "
                + "on the shutter: it tells you whose side of it you are on.");
            return;
        }
    }

    /// <summary>
    /// #803 QA · THE WHOLE MANUAL-FIRE LOOP, AT THE ONE SHUTTER IT WAS WRITTEN FOR.
    ///
    /// <para>Owner's own scenario: a hut with a few rounds in it, and a lock worth spending them on. The
    /// pieces are two rooms and one lift ride apart on a real run, so the row assembles them: the captain is
    /// standing at the GOODS HOIST (the <c>?freight=1</c> boot), one sentry is SET DOWN beside them with
    /// four rounds in it — one short of a hasp, deliberately, so the put verb is not optional — and a hut's
    /// find is loose in the pocket.</para>
    ///
    /// <para>Nothing here is a different world from the one a captain reaches by walking. The shutter is the
    /// shutter, the rounds are ordinary rounds, and the six that come off the drum are the six the law
    /// charges: a cheat that shows a tester a different scene is worse than no cheat at all.</para></summary>
}

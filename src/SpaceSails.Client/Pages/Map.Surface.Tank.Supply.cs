using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #612 · WHERE THE AIR IS COMING FROM — the last answer the predicate gave, kept ONLY so the crossing
/// can be said once.
///
/// <para>Split out of <c>Map.Surface.Tank.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── #612 · WHERE THE AIR IS COMING FROM. ────────────────────────────────────────────────────────────
    //
    // The last answer the predicate gave, kept ONLY so the crossing can be said once. It is deliberately not
    // what anything DISPLAYS — a cached copy of a fact is a second source of that fact, and this is the one
    // fact in the game that must not have two. Null before an excursion has ticked, which is also what stops
    // the crossing line firing on the frame a captain lands.
    private SuitAir.Supply? _airSupplyNoted;

    /// <summary>#612 · THE CROSSING, SAID ONCE. Owner: <i>"maybe pop-up about you have air or you are in
    /// vacuum type ... it is vital info :-D"</i>.
    ///
    /// <para>It fires only where the tank STARTS or STOPS, never on Room→Ship (both are free, and a line
    /// about a change that costs nothing is the nag that turns a vital fact into wallpaper), and never on
    /// the first tick of an excursion. A room with a DOOR is left to say it in its own voice —
    /// <c>SurfaceShelter.BreathingLine</c> and <c>UndergroundComplex.RefugeEntryLine</c> are already the
    /// better sentences for those thresholds, and two lines for one door is exactly the noise the tank
    /// mechanic was told not to become. What is left is the crossing nothing else narrates: stepping out of
    /// the car onto a floor that holds or does not, and leaving her tube for the regolith.</para>
    ///
    /// <para>#608 · A refuge speaks for itself in all THREE of its states — the room that holds, the room
    /// that holds and has nothing in it, and the door that will not cycle each have their own sentence — so
    /// <paramref name="roomSpeaksForItself"/> is still simply <i>is the captain in one</i>. The failed one
    /// suppresses no crossing in any case: standing in it does not change what the tank is doing.</para></summary>
    private void AnnounceAirSupply(SuitAir.Supply supply, bool roomSpeaksForItself)
    {
        SuitAir.Supply? was = _airSupplyNoted;
        _airSupplyNoted = supply;

        if (was is null || SuitAir.Drawing(was.Value) == SuitAir.Drawing(supply) || roomSpeaksForItself)
        {
            return;
        }

        RendererInterop.PlayCue("blip");
        ShowPulseMessage(SuitAir.SupplyChangedLine(supply));
    }
}

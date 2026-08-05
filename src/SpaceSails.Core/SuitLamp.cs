namespace SpaceSails.Core;

/// <summary>
/// #708 · THE SUIT'S HEADLIGHTS. Owner: <i>"For dark levels our suit should have forward facing headlights
/// … the pre-existing tunnels would be scary as dark ones and totally different style."</i>
///
/// <para>On a floor that is DARK (see <see cref="UndergroundComplex.IsDark(string,int,bool)"/>) this cone is
/// the whole of a captain's seeing. Everything it does not reach is not dimmed, it is gone — so turning the
/// body becomes an act of LOOKING, on a top-down plan that has never asked anybody to look at anything. It is
/// the same trick the monolith's shadow plays with height (§10): the drawing carries the fact, and no prose
/// has to.</para>
///
/// <para><b>NOTHING HERE IS TYPED.</b> A lamp is a lamp, and this game already owns one number for what a
/// hand-lamp reaches down a corridor and how wide it opens: the black-ops sweepers carry it
/// (<see cref="InspectionTeam.LampRange"/>, <see cref="InspectionTeam.LampConeHalfAngleDegrees"/> — 20 du
/// inside a 70° cone, #538). Inventing a second pair of numbers for the captain's lamp would mean the two
/// lights in this game were different pieces of kit for no reason anybody could name, and the first time a
/// sweeper's cone and a captain's cone were drawn on the same deck the difference would read as a bug. So the
/// suit's headlights are the sweepers' lamp, said once, in one place.</para>
///
/// <para>The <i>arm's-reach ring</i> is deliberately NOT here: it is not a lamp at all, it is the radius in
/// which a captain can put their hands on something, and that number belongs to whatever is doing the
/// reaching. The renderer hands it in. (Its own law is #212's: an affordance the game will let you use must
/// never be invisible.)</para>
///
/// <para>Walls do not stop this cone yet — the top-down plan has always drawn a room you cannot see into, and
/// cutting the light at every bulkhead is a raycast per frame per wall for a floor that today only exists
/// behind a cheat. Filed, not forgotten.</para>
/// </summary>
public static class SuitLamp
{
    /// <summary>How far the headlights throw, in deck units. The sweepers' lamp — see the class note.</summary>
    public const double RangeDu = InspectionTeam.LampRange;

    /// <summary>…and how wide, as a half-angle in degrees, so 35 is a 70° cone. The sweepers' lamp again.
    /// Narrow on purpose: at 70° a captain standing in a hall can see about a fifth of it at once, which is
    /// what makes turning round a decision instead of a camera move.</summary>
    public const double ConeHalfAngleDegrees = InspectionTeam.LampConeHalfAngleDegrees;

    /// <summary>
    /// Whether the suit lights a point: inside the cone and its reach, OR inside <paramref name="touchRingDu"/>
    /// of the captain — the faint spill off your own body, which is why you can always see the thing you are
    /// standing on.
    /// </summary>
    /// <param name="headingRad">The captain's facing, world frame, radians — the same
    /// <c>Math.Atan2(dy, dx)</c> convention every facing in this game uses.</param>
    /// <param name="touchRingDu">The arm's-reach ring: whatever the caller's own "you can put your hands on
    /// this" radius is. Zero is legal and means the cone alone.</param>
    public static bool Lights(double ax, double ay, double headingRad,
                              double x, double y, double touchRingDu)
    {
        double dx = x - ax, dy = y - ay;
        double r2 = (dx * dx) + (dy * dy);

        if (r2 <= touchRingDu * touchRingDu)
        {
            return true;    // close enough to touch — the light off your own suit
        }

        if (r2 > RangeDu * RangeDu)
        {
            return false;
        }

        // The same off-axis measure the sweepers' cone is CHECKED with, so the two lights can never come to
        // disagree about what "inside the cone" means (#538's own lesson, pointed at us this time).
        return InspectionTeam.OffAxisDegrees(headingRad, dx, dy) <= ConeHalfAngleDegrees;
    }
}

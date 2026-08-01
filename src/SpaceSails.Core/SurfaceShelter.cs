using System;

namespace SpaceSails.Core;

/// <summary>
/// #573 · THE SHELTER — one building, out in the deep, with a door that works and air inside it.
///
/// <para>Owner, walking a landing site: <i>"just make one building into the middle there with working door.
/// Start with that on that front"</i>, then <i>"or lets put that near the bottom there"</i>, and — the part
/// that makes it a mechanic rather than scenery — <i>"there needs to be explorable space around that
/// building we go to refill."</i></para>
///
/// <para>So it is not decoration and it is not loot. It is the <b>second place on a moon where a suit can be
/// refilled</b>, and the only one that is not the ship. That single fact reshapes an excursion: until now
/// the tube was the one anchor and every walk was a loop around it (#562), and a shelter out in the deep
/// turns the map into two anchors with a decision between them — press on to the shelter, or turn back for
/// the tube, on whatever the gauge says.</para>
///
/// <para><b>Guaranteed, exactly one, always in the deep.</b> Not seeded-present like the outpost huts: this
/// is the thing the field is built around, and a site that rolled a 4 and had none would simply be a site
/// where the air mechanic has no answer.</para>
/// </summary>
public static class SurfaceShelter
{
    /// <summary>How much air the shelter's tanks hand over, in seconds. Deliberately NOT a full tank — the
    /// same rule the ammunition caches live under (#563): a refill out in the world must extend a trip, not
    /// make a captain self-sufficient. Enough to get home comfortably from the deep, plus a margin to be
    /// interesting with; not enough to simply live out here.</summary>
    public const double RefillSeconds = 150.0;

    /// <summary>How long the charge takes to transfer. Long enough that a captain standing in a shelter with
    /// something walking toward it has a real decision, short enough that it is never a chore.</summary>
    public const double RefillSecondsToTransfer = 4.0;

    /// <summary>Where the shelter stands: deep, and off to one side so it is a WALK rather than a straight
    /// line down from the tube. Seeded per site, so two grounds on the same moon put it in different places.
    ///
    /// <para>Kept clear of the deep anchor — the monolith is what you go out there to SEE, and a building
    /// leaning on it would spoil both.</para></summary>
    public static (double X, double Y) PlaceOn(string bodyId, string siteSalt, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        double margin = SurfaceLayout.EdgeMargin + 24;
        double x = Lerp(field.LeftX + margin, field.RightX - margin, Frac(bodyId, siteSalt, "x"));
        double y = Lerp(field.BottomY + 24, field.BottomY + ((field.LandingBandY - field.BottomY) * 0.45),
            Frac(bodyId, siteSalt, "y"));

        // Never on the monolith's toes.
        if (Math.Abs(x - field.AnchorX) < 40 && Math.Abs(y - field.AnchorY) < 30)
        {
            x = x < field.AnchorX ? field.AnchorX - 55 : field.AnchorX + 55;
            x = Math.Clamp(x, field.LeftX + margin, field.RightX - margin);
        }
        return (x, y);
    }

    /// <summary>The shelter's shape. A drum — because on a cold world with in-situ materials, a wall that is
    /// also a pressure vessel would rather not have corners (the owner's Greenland longhouse reasoning), and
    /// because it should read as different from the rubble around it at a glance.</summary>
    public static SurfaceStructure.Spec SpecFor(string bodyId, string siteSalt, in SurfaceLayout.Field field)
    {
        (double x, double y) = PlaceOn(bodyId, siteSalt, field);
        return new SurfaceStructure.Spec(
            CentreX: x, CentreY: y,
            Width: 26, Height: 21,
            AngleRad: Frac(bodyId, siteSalt, "angle") * Math.Tau,
            Doors: 1,
            WallThickness: 2.6,
            Shape: SurfaceStructure.Footprint.Rounded);
    }

    /// <summary>What the sign outside says. It is a survival shelter and it says so — this is the one thing
    /// on the ground that is NOT a mystery, because a captain who cannot find air is not being teased.</summary>
    public const string DoorLabel = "⛺ SHELTER — PRESSURISED";

    /// <summary>The console inside.</summary>
    public const string TankLabel = "🫁 CHARGING RACK";

    /// <summary>The other half of a survival cache. Owner: <i>"In Andy Weir's moon book there were these
    /// emergency shelters on Moon surface and indoor spaces. I think those should exist on the surface and
    /// they should also contain reload to guns."</i> Right — a shelter stocked with air and nothing else is
    /// a tap, not a refuge. Somebody who put a pressure vessel out here for strangers to find would have put
    /// something in it to defend the place with.</summary>
    public const string LockerLabel = "🔫 EMERGENCY LOCKER";

    /// <summary>Rounds in the shelter's locker. Partial, like every cache out here (#563): it buys one more
    /// fight, never independence, or the tube stops being the anchor.</summary>
    public const int LockerRounds = 45;

    /// <summary>The receipt for the locker.</summary>
    public static string LockerLine(int rounds) =>
        $"🔫 An emergency locker, sealed and dated by somebody long gone: {rounds} rounds, the standard " +
        "calibre. Whoever stocked this place expected the people using it to need them.";

    /// <summary>When the locker has been emptied.</summary>
    public const string LockerEmptyLine = "🔫 The locker is bare. You emptied it yourself.";

    /// <summary>What the ground says as the door gives way to proximity.</summary>
    public const string ArrivalLine =
        "⛺ The shelter's door reads your suit and cycles. Cold, dark, and holding pressure after all this " +
        "time — somebody built this to outlast them.";

    /// <summary>The receipt for a charge.</summary>
    public static string RefillLine(double seconds) =>
        $"🫁 The rack still has charge in it — {(int)(seconds / 60)}:{(int)(seconds % 60):00} into your tank. " +
        "Not a full one. Enough to get you somewhere.";

    /// <summary>When the rack is spent.</summary>
    public const string EmptyLine =
        "🫁 The rack is dry. Whatever was in it, you are carrying now.";

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string siteSalt, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"shelter:{bodyId}:{siteSalt}:{tag}"), Resolution).Face;
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}

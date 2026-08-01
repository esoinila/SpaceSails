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
    /// <summary>How full a shelter will fill a suit, as a fraction of a full tank — and, crucially, THE
    /// POINT IT REFUSES TO GO FURTHER.
    ///
    /// <para>Owner: <i>"let's make it give more air like 66 % but it refuses to give more so there is
    /// something left for next needy person also."</i> That refusal is the best thing in the mechanic,
    /// because it is not a resource cap — it is CHARACTERISATION. Somebody built a pressure vessel out here
    /// for strangers and set its regulator to stop short, on purpose, for whoever comes next. The rule is a
    /// sentence about them.</para>
    ///
    /// <para>It also keeps the tube the real anchor (#562): two thirds gets you home from almost anywhere
    /// and lets you push on a little, and never lets you simply live out here.</para></summary>
    public const double FillToFraction = 0.66;

    /// <summary>Sim-seconds for an emptied shelter to come back to full on its own. Owner: <i>"those
    /// shelters should replenish themselves automatically."</i>
    ///
    /// <para>The consequence is the good bit and it is free: if they refill on their own, then <b>finding
    /// one that is NOT full means somebody was there</b> — a fact about the world told by state rather than
    /// by a card, which is the register this game keeps reaching for.</para></summary>
    public const double RechargeSeconds = 6.0 * 60.0 * 60.0;

    /// <summary>How much air a shelter hands over at full charge, in seconds — the ceiling on a single
    /// draw before <see cref="FillToFraction"/> is applied.</summary>
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

    /// <summary>Is the captain INSIDE the shelter — that is, standing in its atmosphere?
    ///
    /// <para>Owner: <i>"and the shelter has air in it."</i> Its own sign has said PRESSURISED from the
    /// start, so a suit standing in it should not be spending anything. That single line turns the building
    /// from a tap into a REFUGE — somewhere to stop, breathe, reload and think — which is what was asked for
    /// much earlier in the same conversation (<i>"rooms with doors we can hide behind while we reload our
    /// guns safe from reevers"</i>).</para>
    ///
    /// <para>Tested against the INNER face: the walls are metres of piled regolith, and standing in the
    /// doorway is not standing inside. Modelled as the inscribed ellipse, which is right for the drum this
    /// always is and forgiving in the corners of anything else.</para></summary>
    public static bool Contains(in SurfaceStructure.Spec spec, double x, double y)
    {
        double c = Math.Cos(-spec.AngleRad), s = Math.Sin(-spec.AngleRad);
        double dx = x - spec.CentreX, dy = y - spec.CentreY;
        double lx = (dx * c) - (dy * s), ly = (dx * s) + (dy * c);

        double halfW = (spec.Width / 2) - spec.WallThickness;
        double halfH = (spec.Height / 2) - spec.WallThickness;
        if (halfW <= 0 || halfH <= 0)
        {
            return false;
        }
        return ((lx * lx) / (halfW * halfW)) + ((ly * ly) / (halfH * halfH)) <= 1.0;
    }

    /// <summary>What the shelter says as a captain steps into its air.</summary>
    public const string BreathingLine =
        "🫁 Pressure. The suit stops drawing and the readout holds where it is — you can stand here as long " +
        "as you like. Nothing outside can work that door.";

    /// <summary>A shelter's charge, 0..1, given when it was last drawn down and what time it is now. Pure,
    /// so a captain who leaves and comes back gets exactly the arithmetic they should.</summary>
    public static double ChargeAt(double lastDrawnSimTime, double nowSimTime)
    {
        if (double.IsNaN(lastDrawnSimTime))
        {
            return 1.0;
        }
        double since = Math.Max(0, nowSimTime - lastDrawnSimTime);
        return Math.Clamp(since / RechargeSeconds, 0.0, 1.0);
    }

    /// <summary>Was this shelter drawn on by SOMEBODY ELSE, before this captain ever found it? Seeded per
    /// site, so the story is a fact about the place rather than a random event.
    ///
    /// <para>This is what makes "finding one not full means somebody was there" land on FIRST contact
    /// instead of only after your own second visit. Roughly one shelter in three has been used by a
    /// stranger recently enough to still show it.</para></summary>
    public static bool SomebodyWasHere(string bodyId, string siteSalt)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return DiceRule.Roll(DiceRule.Seed($"shelter:{bodyId}:{siteSalt}:used"), 3).Face == 1;
    }

    /// <summary>How much air this rack will actually put into a suit right now: capped by its own charge,
    /// and hard-stopped at <see cref="FillToFraction"/> of a tank so there is something left for the next
    /// person. Returns 0 when the suit is already at or above the line — the rack does not top up a captain
    /// who does not need it, which is the same courtesy pointing the other way.</summary>
    public static double Quote(double airLeftSeconds, double charge, double tankSeconds)
    {
        double ceiling = tankSeconds * FillToFraction;
        if (airLeftSeconds >= ceiling)
        {
            return 0;
        }
        double available = RefillSeconds * Math.Clamp(charge, 0, 1);
        return Math.Max(0, Math.Min(ceiling - airLeftSeconds, available));
    }

    /// <summary>The receipt for a charge.</summary>
    public static string RefillLine(double seconds) =>
        $"🫁 The rack gives you {(int)(seconds / 60)}:{(int)(seconds % 60):00} and then stops itself, well " +
        "short of a full tank. Whoever set that regulator meant the next person through that door to find " +
        "something in it too.";

    /// <summary>What the rack says when the suit is already past the line it will fill to.</summary>
    public const string AlreadyFullEnoughLine =
        "🫁 The rack reads your suit, decides you do not need it, and stays shut. It is not for you right now.";

    /// <summary>What a partly-charged rack says as you find it — the story told by state.</summary>
    public static string PartialLine(double charge) => charge switch
    {
        < 0.15 => "🫁 The rack is all but empty and still cycling. Somebody was here, and not long ago.",
        < 0.5 => "🫁 The rack is well down. Somebody drew on it recently — it has not had time to come back.",
        < 0.9 => "🫁 The rack is short of full. Somebody has been through here since it last topped off.",
        _ => "",
    };

    /// <summary>When the rack has nothing to give at all.</summary>
    public const string EmptyLine =
        "🫁 The rack is dry and slowly refilling itself. Come back to it, or find another.";

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string siteSalt, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"shelter:{bodyId}:{siteSalt}:{tag}"), Resolution).Face;
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}

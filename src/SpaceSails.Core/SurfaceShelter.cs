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
    /// by a card, which is the register this game keeps reaching for.</para>
    ///
    /// <para><b>TWO MINUTES, not the six hours this first shipped as.</b> Owner, sitting in one with a spent
    /// rack: <i>"the rack should replenish way faster now I am stranded here."</i> He is right, and it was a
    /// design error rather than a number: a recharge measured in hours means STRANDED EQUALS DEAD, and the
    /// building stops being a refuge at the exact moment it is needed as one. At two minutes, waiting it out
    /// is a real option - grim, boring, entirely valid, with the pack still walking toward you throughout.
    /// That is a scene. Six hours was a loading screen you could not skip.</para>
    ///
    /// <para>It does not become a hiding place: the drain stops inside, but nothing is GAINED by sitting
    /// there, the rack still refuses past <see cref="FillToFraction"/>, and the Old Ones do not get bored.</para></summary>
    public const double RechargeSeconds = 120.0;

    /// <summary>How much air a rack can hold in its reservoir, in suit-seconds.</summary>
    public const double ReservoirSeconds = 150.0;

    /// <summary>How fast the rack MAKES air, in suit-seconds per real second. Slow, and always running.
    ///
    /// <para>Owner: <i>"it should always give some more air... like a steady production rate... we could
    /// just note that it is not full ... so somebody has used it. The time it takes to pump air is good
    /// incentive to not take too much."</i></para>
    ///
    /// <para>This replaced a one-shot draw that could be SPENT, which had a nasty failure the owner walked
    /// straight into: a captain stranded beside an empty rack had nothing to do but die. A cracker that
    /// always produces cannot strand anybody — and the pumping TIME does the balancing the empty state was
    /// badly trying to do. Topping right up means standing in a shed while the Old Ones keep walking, so
    /// taking "enough" and leaving is a decision the player makes, every time, rather than a rule.</para></summary>
    public const double ProductionPerSecond = 2.0;

    /// <summary>How fast a full reservoir transfers into a suit, in suit-seconds per real second. Quick, so
    /// arriving at an untouched rack is a relief rather than an errand; once it is drained you are down to
    /// <see cref="ProductionPerSecond"/> and the clock is the price.</summary>
    public const double TransferPerSecond = 26.0;

    /// <summary>How long the charge takes to transfer. Long enough that a captain standing in a shelter with
    /// something walking toward it has a real decision, short enough that it is never a chore.</summary>
    public const double RefillSecondsToTransfer = 4.0;

    /// <summary>How many shelters a site carries. Owner, walking the enlarged field: <i>"we should have
    /// more emergency shelters here."</i>
    ///
    /// <para>One was right for a 78 x 64 du field and thin for a 310 x 260 one — and thematically wrong too:
    /// Andy Weir's are scattered across the surface precisely BECAUSE a single refuge is not a safety net.
    /// Scaled to the field's area so this holds if the ground is ever resized again.</para>
    ///
    /// <para>It does not dissolve the tether, and the reason is the refusal: <see cref="FillToFraction"/>
    /// means no shelter ever tops a suit off, so however many you string together you are always working
    /// down from two thirds and must eventually go home. More shelters buy RANGE, never independence.</para></summary>
    public static int CountFor(in SurfaceLayout.Field field)
    {
        double area = (field.RightX - field.LeftX) * (field.LandingBandY - field.BottomY);
        return Math.Clamp((int)Math.Round(area / 20_000.0), 1, 6);
    }

    /// <summary>How far apart shelters must stand. Close enough to chain in an emergency, far enough that
    /// finding one is still finding something.</summary>
    public const double MinSeparationDu = 70.0;

    /// <summary>Every shelter on this site, in a stable order. Seeded per site, spaced, and kept off the
    /// deep anchor — the monolith is what you walk out there to SEE, and a building leaning on it would
    /// spoil both.</summary>
    public static IReadOnlyList<(double X, double Y)> PlacesOn(
        string bodyId, string siteSalt, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);

        double margin = SurfaceLayout.EdgeMargin + 24;
        double loX = field.LeftX + margin, hiX = field.RightX - margin;
        double loY = field.BottomY + 24, hiY = field.LandingBandY - 30;
        double anchorX = field.AnchorX, anchorY = field.AnchorY;

        int want = CountFor(field);
        var placed = new List<(double X, double Y)>();

        for (int attempt = 0; attempt < want * 12 && placed.Count < want; attempt++)
        {
            double x = Lerp(loX, hiX, Frac(bodyId, siteSalt, $"x:{attempt}"));
            double y = Lerp(loY, hiY, Frac(bodyId, siteSalt, $"y:{attempt}"));

            if (Math.Abs(x - anchorX) < 40 && Math.Abs(y - anchorY) < 30)
            {
                continue;   // never on the monolith's toes
            }

            bool crowded = false;
            foreach ((double px, double py) in placed)
            {
                crowded |= Math.Sqrt(((x - px) * (x - px)) + ((y - py) * (y - py))) < MinSeparationDu;
            }
            if (!crowded)
            {
                placed.Add((x, y));
            }
        }

        // A site ALWAYS has at least one, whatever the seeding did — it is the answer to the air mechanic,
        // and a ground without one is a ground where that mechanic has nothing to say.
        if (placed.Count == 0)
        {
            placed.Add((Lerp(loX, hiX, 0.5), Lerp(loY, hiY, 0.35)));
        }
        return placed;
    }

    /// <summary>The first shelter — kept for callers that only want somewhere to point.</summary>
    public static (double X, double Y) PlaceOn(string bodyId, string siteSalt, in SurfaceLayout.Field field) =>
        PlacesOn(bodyId, siteSalt, field)[0];

    /// <summary>The shelter's shape. A drum — because on a cold world with in-situ materials, a wall that is
    /// also a pressure vessel would rather not have corners (the owner's Greenland longhouse reasoning), and
    /// because it should read as different from the rubble around it at a glance.</summary>
    public static SurfaceStructure.Spec SpecFor(string bodyId, string siteSalt, in SurfaceLayout.Field field) =>
        SpecsFor(bodyId, siteSalt, field)[0];

    /// <summary>Every shelter on this site as a buildable spec, in the same stable order as
    /// <see cref="PlacesOn"/> — so an index means the same building everywhere it is used.</summary>
    public static IReadOnlyList<SurfaceStructure.Spec> SpecsFor(
        string bodyId, string siteSalt, in SurfaceLayout.Field field)
    {
        var specs = new List<SurfaceStructure.Spec>();
        IReadOnlyList<(double X, double Y)> places = PlacesOn(bodyId, siteSalt, field);
        for (int i = 0; i < places.Count; i++)
        {
            specs.Add(new SurfaceStructure.Spec(
                CentreX: places[i].X, CentreY: places[i].Y,
                Width: 26, Height: 21,
                AngleRad: Frac(bodyId, siteSalt, $"angle:{i}") * Math.Tau,
                Doors: 1,
                WallThickness: 2.6,
                Shape: SurfaceStructure.Footprint.Rounded));
        }
        return specs;
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
    public static bool SomebodyWasHere(string bodyId, string siteSalt, int index = 0)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(siteSalt);
        return DiceRule.Roll(DiceRule.Seed($"shelter:{bodyId}:{siteSalt}:used:{index}"), 3).Face == 1;
    }

    /// <summary>How much air moves from a rack into a suit over <paramref name="dt"/> real seconds, given
    /// what the reservoir is holding. Limited by the transfer rate, by what the reservoir actually has, and
    /// hard-stopped at <see cref="FillToFraction"/> of a tank so there is always something left for the next
    /// person. Zero when the suit is already at or above the line — the rack does not top up a captain who
    /// does not need it, which is the same courtesy pointing the other way.</summary>
    public static double Transfer(double airLeftSeconds, double reservoirSeconds, double tankSeconds, double dt)
    {
        double ceiling = tankSeconds * FillToFraction;
        if (airLeftSeconds >= ceiling || reservoirSeconds <= 0 || dt <= 0)
        {
            return 0;
        }
        double wanted = Math.Min(TransferPerSecond * dt, ceiling - airLeftSeconds);
        return Math.Max(0, Math.Min(wanted, reservoirSeconds));
    }

    /// <summary>The reservoir after <paramref name="dt"/> seconds of production, never past its capacity.</summary>
    public static double Produce(double reservoirSeconds, double dt) =>
        Math.Clamp(reservoirSeconds + (ProductionPerSecond * Math.Max(0, dt)), 0, ReservoirSeconds);

    /// <summary>Said once as the rack starts feeding a suit.</summary>
    public const string PumpingLine =
        "🫁 The rack finds your fitting and starts pumping. It is not fast, and it will stop itself well " +
        "short of a full tank — whoever set that regulator meant the next person through the door to find " +
        "something in it too. Stay as long as you dare.";

    /// <summary>Said once when the rack reaches the line it refuses to pass.</summary>
    public const string PumpDoneLine =
        "🫁 The rack clicks off at two thirds and will not be argued with. That is your lot; the rest is " +
        "for whoever comes next.";

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

    /// <summary>When the reservoir is drained and the captain is down to the trickle it makes.</summary>
    public const string TrickleLine =
        "🫁 The reservoir is down to what the cracker can make. It is still giving — just slowly now, a " +
        "breath at a time, for as long as you are willing to stand here.";

    private const int Resolution = 4096;

    private static double Frac(string bodyId, string siteSalt, string tag)
    {
        int face = DiceRule.Roll(DiceRule.Seed($"shelter:{bodyId}:{siteSalt}:{tag}"), Resolution).Face;
        return (face - 1) / (double)Resolution;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}

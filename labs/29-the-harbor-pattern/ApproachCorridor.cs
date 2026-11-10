namespace SpaceSails.Core;

/// <summary>
/// The safe-approach corridor for a harbor — the measured speed-vs-range envelope that turns "arrive
/// somewhere near the berth and hope" into a coached glideslope (Lab 29, issues #160/#175/#180). Two
/// harbor classes, two utterly different doors:
/// <list type="bullet">
/// <item><b>Station clamp</b> (μ=0): a huge, gravity-free bubble — coast within
/// <see cref="DockRule.EnvelopeMeters"/> (500,000 km) matched to <see cref="DockRule.MatchSpeed"/>
/// (8 km/s) and throw the arm (⚓). A station never truly refuses you; arriving hot only costs a fat
/// pulse bill to null the excess first (Lab 29 §B).</item>
/// <item><b>Moon park</b> (μ>0): a tiny door — the Hill sphere is often under 1,000 km — where an
/// approach that is too fast for its range genuinely FAILS: it punches through the moon or blows past
/// the Hill sphere before the autopilot can bend the fall into a tide-stable orbit (Lab 29 §C, the
/// owner's "in orbit by luck").</item>
/// </list>
///
/// <para>The corridor is a <b>constant-time-to-go glideslope</b>: keep the closing speed under
/// <c>range / τ</c>, capped at the door's shear/window limit, where τ is anchored so that at the door
/// the on-pattern speed equals the autopilot's own terminal closing speed
/// (<see cref="OrbitRule.ApproachClosingSpeed"/>). Every number here is derived from the SAME Core
/// constants the autopilot flies with (<see cref="DockRule"/>, <see cref="OrbitRule"/>) — Lab 29's
/// sweep measured the pulse cost and failure boundary that make these gates the right ones, but the
/// gates themselves are the machinery's own thresholds, not a parallel model.</para>
///
/// <para><b>Guidance seam.</b> <see cref="Read"/> answers "am I on the pattern, and what is the next
/// gate?" for a live (range, relative speed). It is pure and UI-free: the banner NOW/NEXT row (#159)
/// and the #160 tutorial narration consume the verdict and <see cref="CorridorReading.NextGate"/> —
/// this type computes, it does not present.</para>
/// </summary>
public sealed class ApproachCorridor
{
    /// <summary>Which kind of door this harbor has.</summary>
    public CorridorClass Class { get; }

    /// <summary>The outer edge of the door: the clamp bubble (<see cref="DockRule.EnvelopeMeters"/>)
    /// for a station, the Hill sphere for a moon. Inside this the on-pattern ceiling drops toward the
    /// berth; at exactly this range it equals <see cref="TerminalCloseMps"/>.</summary>
    public double DoorRangeMeters { get; }

    /// <summary>The hard capture cap at the door — the fastest the ship may ever be and still be
    /// grabbed/inserted here: <see cref="DockRule.MatchSpeed"/> (8 km/s clamp shear) for a station,
    /// <see cref="OrbitRule.MaxRelativeSpeed"/> (5 km/s insertion window) for a moon. Over this, inside
    /// the door, is <see cref="CorridorVerdict.Missed"/>.</summary>
    public double DoorSpeedMps { get; }

    /// <summary>The autopilot's terminal closing speed at this harbor
    /// (<see cref="OrbitRule.ApproachClosingSpeed"/>) — the on-pattern speed the glideslope aims for at
    /// the door (4 km/s at a station, a few hundred m/s at a deep-well moon). Lab 29 measured the pulse
    /// cost floor sitting right along this line.</summary>
    public double TerminalCloseMps { get; }

    /// <summary>The berth: where the arrival ends. The station body radius (the clamp point) for a
    /// station; the tide-stable parking radius (<see cref="OrbitRule.ParkingRadius"/>) for a moon.</summary>
    public double BerthRangeMeters { get; }

    /// <summary>The range from which the armed autopilot takes over (<see cref="OrbitRule.CaptureRange"/>)
    /// — the outermost gate, "be under the cap by the time the ship flies itself."</summary>
    public double HandoverRangeMeters { get; }

    /// <summary>The glideslope time-to-go constant τ (seconds): <c>DoorRange / TerminalClose</c>. On the
    /// pattern the ship always has at least τ seconds before it would reach the door at its current
    /// closing speed — the reaction margin the discrete autopilot needs to null and circularize.</summary>
    public double GlideslopeSeconds { get; }

    /// <summary>The named coaching gates, OUTERMOST first (descending range). The last is the berth.</summary>
    public IReadOnlyList<CorridorGate> Gates { get; }

    private ApproachCorridor(
        CorridorClass klass, double doorRange, double doorSpeed, double terminalClose,
        double berthRange, double handoverRange, IReadOnlyList<CorridorGate> gates)
    {
        Class = klass;
        DoorRangeMeters = doorRange;
        DoorSpeedMps = doorSpeed;
        TerminalCloseMps = terminalClose;
        BerthRangeMeters = berthRange;
        HandoverRangeMeters = handoverRange;
        GlideslopeSeconds = doorRange / terminalClose;
        Gates = gates;
    }

    /// <summary>The on-pattern speed ceiling at a given range: the constant-time-to-go glideslope
    /// <c>range / τ</c>, capped at the door's shear/window limit and never below the berth's circular
    /// crawl. Flat at the cap far out, gliding down to <see cref="TerminalCloseMps"/> at the door and
    /// lower still toward the berth.</summary>
    public double MaxSpeedAt(double rangeMeters)
    {
        double glide = rangeMeters / GlideslopeSeconds;
        return Math.Min(DoorSpeedMps, Math.Max(0.0, glide));
    }

    /// <summary>
    /// Classify a live approach. <see cref="CorridorVerdict.OnPattern"/> when the closing speed is at or
    /// under the glideslope ceiling for the current range; <see cref="CorridorVerdict.Missed"/> when the
    /// ship is inside the door yet over the hard capture cap (overshooting / too hot to grab — the
    /// #175 dead-end); <see cref="CorridorVerdict.Hot"/> everywhere else over the pattern — recoverable,
    /// bleed speed before the next gate. Also returns the ceiling here, the signed margin (negative =
    /// over), and the next gate the ship must satisfy.
    /// </summary>
    public CorridorReading Read(double rangeMeters, double relSpeedMps)
    {
        double ceiling = MaxSpeedAt(rangeMeters);
        CorridorVerdict verdict =
            relSpeedMps <= ceiling ? CorridorVerdict.OnPattern
            : rangeMeters <= DoorRangeMeters && relSpeedMps > DoorSpeedMps ? CorridorVerdict.Missed
            : CorridorVerdict.Hot;
        return new CorridorReading(verdict, ceiling, ceiling - relSpeedMps, NextGate(rangeMeters));
    }

    /// <summary>The next gate the ship will cross going inward: the OUTERMOST gate strictly inside the
    /// current range (gates are descending, so the first one below the current range is the nearest
    /// ahead), or the innermost gate when already at/inside it.</summary>
    public CorridorGate NextGate(double rangeMeters)
    {
        for (int i = 0; i < Gates.Count; i++)
        {
            if (Gates[i].RangeMeters < rangeMeters)
            {
                return Gates[i];
            }
        }
        return Gates[^1];
    }

    /// <summary>Build the corridor for any haven, dispatching on μ: a mass-less body is a station clamp,
    /// a massive one is a moon park.</summary>
    public static ApproachCorridor For(CelestialBody haven, double parentMu) =>
        haven.Mu <= 0 ? ForStation(haven) : ForMoon(haven, parentMu);

    /// <summary>The station clamp corridor: door = the <see cref="DockRule"/> envelope, cap = the clamp
    /// shear speed, terminal closing = the autopilot's global 4 km/s stand-in speed.</summary>
    public static ApproachCorridor ForStation(CelestialBody station)
    {
        double door = DockRule.EnvelopeMeters;
        double cap = DockRule.MatchSpeed;
        double close = OrbitRule.ApproachClosingSpeed(station, 0);   // hill = 0 → global 4 km/s
        double handover = OrbitRule.CaptureRange(0);                 // 3e9 floor
        double berth = station.BodyRadius;
        double tau = door / close;
        // No separate berth gate: a station's berth IS the clamp bubble — the arm reaches the whole
        // envelope, so "clamp window" is the innermost gate.
        var gates = new List<CorridorGate>
        {
            new("handover", handover, Math.Min(cap, handover / tau)),
            new("clamp window", door, close),
        };
        return new ApproachCorridor(CorridorClass.StationClamp, door, cap, close, berth, handover, gates);
    }

    /// <summary>The moon park corridor: door = the Hill sphere, cap = the insertion-window speed limit,
    /// terminal closing = the deep-well-capped approach speed, berth = the tide-stable park radius.</summary>
    public static ApproachCorridor ForMoon(CelestialBody moon, double parentMu)
    {
        double hill = OrbitRule.HillRadius(moon, parentMu);
        double door = hill;
        double cap = OrbitRule.MaxRelativeSpeed;
        double close = OrbitRule.ApproachClosingSpeed(moon, hill);
        double handover = OrbitRule.CaptureRange(hill);
        double berth = OrbitRule.ParkingRadius(moon, hill);
        double tau = door / close;
        var gates = new List<CorridorGate>
        {
            new("handover", handover, Math.Min(cap, handover / tau)),
            new("Hill sphere", door, close),
            new("park", berth, Math.Min(cap, Math.Max(0.0, berth / tau))),
        };
        return new ApproachCorridor(CorridorClass.MoonPark, door, cap, close, berth, handover, gates);
    }
}

/// <summary>Which kind of door a harbor has — a mass-less station you clamp onto, or a moon you park at.</summary>
public enum CorridorClass
{
    /// <summary>μ=0 station: coast into the <see cref="DockRule"/> clamp bubble and throw the arm.</summary>
    StationClamp,

    /// <summary>μ&gt;0 moon: bend the fall into a tide-stable orbit (<see cref="OrbitRule"/>) to deliver.</summary>
    MoonPark,
}

/// <summary>Verdict on a live approach against its corridor.</summary>
public enum CorridorVerdict
{
    /// <summary>At or under the glideslope ceiling — a clean, cheap arrival is on track.</summary>
    OnPattern,

    /// <summary>Over the pattern but still outside the door (or under the hard cap): recoverable, bleed
    /// speed before the next gate. Costs pulses; at a moon it courts the overshoot.</summary>
    Hot,

    /// <summary>Inside the door and over the hard capture cap — overshooting the berth / too hot to grab
    /// or insert. The #175 dead-end: "there but no way in." Slow down and come around again.</summary>
    Missed,
}

/// <summary>One coaching gate on the corridor: at <paramref name="RangeMeters"/> be under
/// <paramref name="MaxSpeedMps"/>.</summary>
public readonly record struct CorridorGate(string Name, double RangeMeters, double MaxSpeedMps);

/// <summary>The answer <see cref="ApproachCorridor.Read"/> hands the guidance seam.</summary>
/// <param name="Verdict">On-pattern / hot / missed.</param>
/// <param name="MaxSpeedHere">The glideslope ceiling at the current range (m/s).</param>
/// <param name="MarginMps">Ceiling minus current closing speed — negative means over the pattern.</param>
/// <param name="NextGate">The gate the ship must satisfy next.</param>
public readonly record struct CorridorReading(
    CorridorVerdict Verdict, double MaxSpeedHere, double MarginMps, CorridorGate NextGate);

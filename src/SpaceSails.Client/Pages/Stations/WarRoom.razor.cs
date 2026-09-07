using Microsoft.AspNetCore.Components;
using System.Globalization;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages.Stations;

// WarRoom — the code-behind for WarRoom.razor.
//
// #1107 · MOVED HERE BY PURE MOTION, because the razor generator's output is NOT ANALYSED. Every line
// that lives inside an `@code { … }` block is invisible to the .NET analysers this repo runs with
// `TreatWarningsAsErrors`, so a finding in there is a finding nobody is ever shown. The moment the same
// characters sit in a `.cs` file beside the component, the analysers see them.
//
// Nothing below was rewritten: it is the `@code` block of WarRoom.razor, character for character, in the
// order it was in. The razor keeps its directives (`@namespace`, `@inject`, `@using`, `@implements`) and
// its markup; a code-behind partial is the same class.
public partial class WarRoom
{
    /// <summary>A boardable/rob-able ship the war-room can aim at — a thin projection of
    /// Map.razor's own NPC state, mirroring TrackingPost's TrackingCandidate.</summary>
    public readonly record struct Contact(NpcShip Ship, ShipState State, bool WarningShotFired, bool Bribed);

    /// <summary>Hired muscle, hunting. Map.razor's HunterState roster, thinned to what the
    /// display needs.</summary>
    public readonly record struct HunterContact(
        string Id, string Callsign, ShipState State,
        // #962: what her contract says — composed in EncounterRule beside the rules they describe, so this
        // desk and the dossier card cannot quote different numbers at the captain.
        string Warrant = "", string HidingTerm = "", string NerveTerm = "");

    /// <summary>M27: a tracking-post ledger entry, thinned for the war room — the sensor room
    /// screens the data, this desk consumes it.</summary>
    public readonly record struct SensorTrack(string Id, string Callsign, ShipState State, double Quality, bool IsThreat);

    private readonly record struct RenderTrack(SensorTrack Track, double X, double Y);

    private readonly record struct RenderContact(Contact Contact, ComplianceState Compliance, double Distance, double X, double Y);

    private readonly record struct NearestHunterInfo(string Callsign, double Distance, double BearingDeg);

    /// <summary>A hunter, ready to draw: screen position, bearing, distance, closing speed and
    /// whether it's close enough (within 2x weapon range) to earn a threat line on the circle.</summary>
    private readonly record struct RenderHunter(string Id, string Callsign, double Distance, double BearingDeg, double ClosingSpeed, double X, double Y, bool ThreatLine, string Warrant, string HidingTerm, string NerveTerm);

    [Parameter, EditorRequired] public double SimTime { get; set; }
    [Parameter, EditorRequired] public Vector2d ShipPosition { get; set; }
    [Parameter, EditorRequired] public Vector2d ShipVelocity { get; set; }
    [Parameter] public IReadOnlyList<Contact> Contacts { get; set; } = [];
    [Parameter] public IReadOnlyList<HunterContact> Hunters { get; set; } = [];
    [Parameter] public int HeatLevel { get; set; }
    [Parameter] public bool CoolingAtHaven { get; set; }
    [Parameter] public int Credits { get; set; }
    [Parameter] public bool Visible { get; set; }
    [Parameter] public bool FullScreen { get; set; }
    [Parameter] public IReadOnlyList<SensorTrack> SensorTracks { get; set; } = [];
    [Parameter] public string? InterestId { get; set; }
    [Parameter] public InterceptEstimate.Result? Intercept { get; set; }
    [Parameter] public string? InterceptTargetName { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<string> OnWarningShot { get; set; }
    [Parameter] public EventCallback<string> OnBribe { get; set; }
    [Parameter] public EventCallback<string> OnSetInterest { get; set; }

    // M28: gun-deck fire control — Map.razor owns the state, this desk renders the drama.
    [Parameter] public double FireAimOffsetSeconds { get; set; } = 3600;
    [Parameter] public double MaxAimOffsetSeconds { get; set; } = 86400;
    [Parameter] public double MaxMuzzleSpeedMps { get; set; } = OrdnanceRule.MassDriverMuzzleSpeedMps;
    [Parameter] public double? InterestDistanceMeters { get; set; }
    [Parameter] public OrdnanceKind FireKind { get; set; }
    [Parameter] public FireControl.Solution? FireSolution { get; set; }
    [Parameter] public double FireDispersionMeters { get; set; }
    [Parameter] public int RevealedIterations { get; set; }
    [Parameter] public double? FireCountdownSeconds { get; set; }
    [Parameter] public string? FireTip { get; set; }
    [Parameter] public int FirePulseCost { get; set; }
    [Parameter] public int PulsesAvailable { get; set; }
    [Parameter] public EventCallback<double> OnAimOffsetChanged { get; set; }
    [Parameter] public EventCallback<OrdnanceKind> OnFireKindChanged { get; set; }
    [Parameter] public EventCallback OnComputeSolution { get; set; }
    [Parameter] public EventCallback OnCancelFire { get; set; }
    [Parameter] public EventCallback OnScanWindows { get; set; }
    [Parameter] public EventCallback OnAutoAim { get; set; }
    [Parameter] public EventCallback OnArmFire { get; set; }
    [Parameter] public string? FireBlockedBy { get; set; }
    [Parameter] public string? StraightWindowText { get; set; }
    [Parameter] public bool WeaponsAuthorized { get; set; }
    [Parameter] public bool FireAtWill { get; set; }
    [Parameter] public int SlugAmmo { get; set; }
    [Parameter] public int MissileAmmo { get; set; }
    [Parameter] public bool Docked { get; set; }
    [Parameter] public bool CanWarnInterest { get; set; }
    [Parameter] public EventCallback<OrdnanceKind> OnBuyAmmo { get; set; }
    [Parameter] public IReadOnlyList<LiveRound> RoundsInFlight { get; set; } = [];
    [Parameter] public double? PlannedImpactInSeconds { get; set; }

    /// <summary>One live round for the in-flight tracker.</summary>
    public readonly record struct LiveRound(string Kind, string Target, double ExpiresIn);

    /// <summary>What the NO SHOT banner appends: the honest kinematic window, so the verdict
    /// teaches the fix instead of just refusing (the naive distance/muzzle hint was 25× off
    /// against a receding target).</summary>
    private string NeedsAimHint() =>
        StraightWindowText is not null
            ? $" — the straight window is {StraightWindowText}"
            : " — no straight window at any flight time; 🔭 scan for a gravity assist";

    // The aim slider is logarithmic: position 0..1000 maps exponentially over
    // [MinAimSeconds, MaxAimOffsetSeconds] so ten-minute knife-fights and forty-five-day
    // orbital shots share one usable control (same trick as the warp and horizon sliders).
    private const double MinAimSeconds = 600;

    private double AimToSlider(double seconds) =>
        1000 * Math.Log(Math.Clamp(seconds, MinAimSeconds, MaxAimOffsetSeconds) / MinAimSeconds)
            / Math.Log(Math.Max(MaxAimOffsetSeconds / MinAimSeconds, 1.0001));

    private double SliderToAim(double position) =>
        MinAimSeconds * Math.Exp(position / 1000 * Math.Log(Math.Max(MaxAimOffsetSeconds / MinAimSeconds, 1.0001)));

    private Task OnAimInput(ChangeEventArgs e) =>
        double.TryParse(e.Value?.ToString(), out double v)
            ? OnAimOffsetChanged.InvokeAsync(SliderToAim(v))
            : Task.CompletedTask;

    // The desk's range-scale selector (StationDesks.md PR-13): how far out the tactical circle
    // reaches. Purely a display choice — weapon/catch rings are always drawn at their true
    // physical radius, scaled to whichever of these the player picked.
    private static readonly double[] RangePresets = [1e8, 5e8, 1e9, 5e9];
    private double _viewRangeMeters = 1e9; // matches the pre-PR-13 fixed view range by default
    private const double CenterPx = 100;
    private const double MaxRadiusPx = 90;

    private readonly Dictionary<string, string> _hailLines = [];

    private double WeaponRingPx => MetersToPx(EncounterRule.WeaponRangeMeters);

    private double MetersToPx(double meters) => Math.Clamp(meters / _viewRangeMeters * MaxRadiusPx, 0, MaxRadiusPx);

    private (double X, double Y) ScreenPoint(Vector2d worldPosition)
    {
        Vector2d rel = worldPosition - ShipPosition;
        double scale = MaxRadiusPx / _viewRangeMeters;
        // Screen Y grows downward; flip so "+Y in world" reads as "up" on the tactical circle.
        return (CenterPx + rel.X * scale, CenterPx - rel.Y * scale);
    }

    private IReadOnlyList<RenderContact> VisibleContacts()
    {
        var list = new List<RenderContact>();
        foreach (Contact c in Contacts)
        {
            double distance = (c.State.Position - ShipPosition).Length;
            if (distance > _viewRangeMeters)
            {
                continue;
            }

            ComplianceState compliance = EncounterRule.ComplianceOf(c.Ship, HeatLevel);
            (double x, double y) = ScreenPoint(c.State.Position);
            list.Add(new RenderContact(c, compliance, distance, x, y));
        }

        list.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return list;
    }

    /// <summary>Wolf-aim: is the interest target hired muscle? The intercept clock then talks
    /// in HIS catch envelope (he runs us down) and admits the pursuit-law estimate.</summary>
    private bool InterestIsHunter
    {
        get
        {
            foreach (HunterContact h in Hunters)
            {
                if (h.Id == InterestId)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Every hunter, closing-speed and bearing computed once — regardless of view
    /// range, so the side list and the "closing" warning line always reflect true geometry even
    /// when a hunter is beyond the tactical circle's current zoom.</summary>
    private IReadOnlyList<RenderHunter> RenderHunters()
    {
        var list = new List<RenderHunter>();
        foreach (HunterContact h in Hunters)
        {
            Vector2d rel = h.State.Position - ShipPosition;
            double distance = rel.Length;
            double bearingDeg = Math.Atan2(rel.Y, rel.X) * 180.0 / Math.PI;
            if (bearingDeg < 0)
            {
                bearingDeg += 360;
            }

            double closingSpeed = ClosingSpeedMps(rel, distance, h.State.Velocity);
            (double x, double y) = ScreenPoint(h.State.Position);
            bool threatLine = distance <= EncounterRule.WeaponRangeMeters * 2;
            list.Add(new RenderHunter(h.Id, h.Callsign, distance, bearingDeg, closingSpeed, x, y, threatLine,
                h.Warrant, h.HidingTerm, h.NerveTerm));
        }

        list.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return list;
    }

    /// <summary>Positive = the range is shrinking (the hunter is closing); negative = opening.
    /// The radial component of relative velocity along the ship→hunter line.</summary>
    private double ClosingSpeedMps(Vector2d shipToHunter, double distance, Vector2d hunterVelocity)
    {
        if (distance <= 0)
        {
            return 0;
        }

        Vector2d relVel = hunterVelocity - ShipVelocity;
        return -relVel.Dot(shipToHunter / distance);
    }

    /// <summary>M27: sensor tracks inside the current view range, projected to the circle.</summary>
    private IReadOnlyList<RenderTrack> RenderTracks()
    {
        var list = new List<RenderTrack>();
        foreach (SensorTrack track in SensorTracks)
        {
            double distance = (track.State.Position - ShipPosition).Length;
            if (distance > _viewRangeMeters)
            {
                continue;
            }

            (double x, double y) = ScreenPoint(track.State.Position);
            list.Add(new RenderTrack(track, x, y));
        }

        return list;
    }

    private static string FormatDuration(double seconds) =>
        seconds < 3600 ? $"{seconds / 60:F0} min"
        : seconds < 86400 ? $"{seconds / 3600:F1} h"
        : seconds / 86400 >= 365 ? $"{seconds / 86400 / 365:F1} yr"
        : $"{seconds / 86400:F1} d";

    private static string FormatClosingSpeed(double metersPerSecond)
    {
        double kms = metersPerSecond / 1000.0;
        return kms >= 0 ? $"closing {kms:F2} km/s" : $"opening {-kms:F2} km/s";
    }

    private NearestHunterInfo? NearestHunter()
    {
        HunterContact? best = null;
        double bestDistance = double.MaxValue;
        foreach (HunterContact h in Hunters)
        {
            double d = (h.State.Position - ShipPosition).Length;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = h;
            }
        }

        if (best is not { } hunter)
        {
            return null;
        }

        Vector2d rel = hunter.State.Position - ShipPosition;
        double bearingDeg = Math.Atan2(rel.Y, rel.X) * 180.0 / Math.PI;
        if (bearingDeg < 0)
        {
            bearingDeg += 360;
        }

        return new NearestHunterInfo(hunter.Callsign, bestDistance, bearingDeg);
    }

    private void Hail(RenderContact rc) =>
        _hailLines[rc.Contact.Ship.Id] = EncounterRule.ThreatOutcome(rc.Contact.Ship, rc.Compliance);

    private static string StatusBadge(RenderContact rc)
    {
        if (rc.Contact.Bribed)
        {
            return "🤝 bribed";
        }

        if (rc.Contact.Ship.IsPod)
        {
            return "— unmanned pod";
        }

        return rc.Compliance == ComplianceState.Stubborn ? "⚔ stubborn" : "🏳 compliant";
    }

    /// <summary>Bootstrap badge classes for the desk's roomier contact side-list — same three
    /// states as <see cref="StatusBadge"/>, colored so the list reads at a glance.</summary>
    private static string StatusBadgeClass(RenderContact rc)
    {
        if (rc.Contact.Bribed)
        {
            return "badge bg-warning text-dark";
        }

        if (rc.Contact.Ship.IsPod)
        {
            return "badge bg-secondary";
        }

        return rc.Compliance == ComplianceState.Stubborn ? "badge bg-danger" : "badge bg-success";
    }

    private static string ContactDotClass(RenderContact rc)
    {
        if (rc.Contact.Bribed)
        {
            return "war-room-dot-bribed";
        }

        return rc.Compliance switch
        {
            ComplianceState.Stubborn => "war-room-dot-stubborn",
            ComplianceState.NothingToComply => "war-room-dot-pod",
            _ => "war-room-dot-compliant",
        };
    }

    private string HeatGaugeText()
    {
        // 0-3 flames; unlit levels shown dim via the CSS class on the whole span (simplest —
        // no per-glyph markup needed for four discrete states).
        return HeatLevel switch
        {
            <= 0 => "◌◌◌",
            1 => "🔥◌◌",
            2 => "🔥🔥◌",
            _ => "🔥🔥🔥",
        };
    }

    private string CoolingLabel()
    {
        if (HeatLevel <= 0)
        {
            return "Heat: none. Quiet lanes.";
        }

        double days = CoolingAtHaven ? EncounterRule.HeatDecayDays / EncounterRule.HavenDecayMultiplier : EncounterRule.HeatDecayDays;
        string where = CoolingAtHaven ? " (haven — 4× faster)" : "";
        return $"Cooling at 1 level / {days:F0}d{where}";
    }

    // Razor reserves the bare <text> tag name for literal markup sections and refuses attributes
    // on it, even inside an <svg> — so the SVG <text> label for a hunter's wolf glyph has to be
    // built as raw markup instead of written as a normal element.
    private static Microsoft.AspNetCore.Components.MarkupString HunterLabelMarkup(RenderHunter h) =>
        new($"<text x=\"{Fmt(h.X + 7)}\" y=\"{Fmt(h.Y - 6)}\" class=\"war-room-hunter-label\">🐺</text>");

    /// <summary>A ring's name, sitting just inside its top edge (same raw-markup workaround).
    /// Styles are INLINE: Blazor's scoped CSS can't tag MarkupString-injected elements, so a
    /// stylesheet class would silently not match and the label renders at giant default size.</summary>
    private static Microsoft.AspNetCore.Components.MarkupString RingLabelMarkup(double radiusPx, string label) =>
        new($"<text x=\"100\" y=\"{Fmt(100 - radiusPx + 7)}\" text-anchor=\"middle\" style=\"font-size:6px;fill:rgba(180,200,220,0.55)\">{System.Net.WebUtility.HtmlEncode(label)}</text>");

    /// <summary>#962 · the collector's catch envelope, named where it is drawn. Same inline-style
    /// workaround as <see cref="RingLabelMarkup"/>, but centred on HER rather than on us — the owner had
    /// nothing on any map telling him "the debt collector catch distance", so nothing told him when to
    /// react.</summary>
    private static Microsoft.AspNetCore.Components.MarkupString HunterRingLabelMarkup(double x, double y) =>
        new($"<text x=\"{Fmt(x)}\" y=\"{Fmt(y + 6)}\" text-anchor=\"middle\" style=\"font-size:6px;fill:rgba(255,120,120,0.75)\">"
            + $"{System.Net.WebUtility.HtmlEncode($"catch {FormatRange(EncounterRule.CatchRadiusMeters)}")}</text>");

    private static string Fmt(double v) => v.ToString("F1", CultureInfo.InvariantCulture);

    // Same scale as Map.razor's FormatDistance — kept local since components stay decoupled.
    private static string FormatRange(double meters)
    {
        const double metersPerAu = 1.495978707e11;
        if (meters >= metersPerAu / 10)
        {
            return $"{meters / metersPerAu:F2} AU";
        }

        if (meters >= 1e9)
        {
            return $"{meters / 1e9:F2} M km";
        }

        return $"{meters / 1000:F0} km";
    }
}

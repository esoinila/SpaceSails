using System.Globalization;

namespace SpaceSails.Core;

/// <summary>How a captain arrives at a harbor — the split every arrival label keys off (#203).
/// A real gravitating body (μ&gt;0) is <see cref="Orbit"/>: the autopilot inserts into orbit. A
/// mass-less dock haven (μ≤0 station) is <see cref="Dock"/>: there is no orbit and no insert — the
/// autopilot flies the #155 dock-envelope arrival and the captain throws the ⚓ clamp.</summary>
public enum HarborClass
{
    /// <summary>A gravitating body the autopilot circularizes around ("orbit-insert").</summary>
    Orbit,

    /// <summary>A μ≤0 dock haven you clamp onto ("dock envelope"), never orbit.</summary>
    Dock,
}

/// <summary>ONE voice for the words a captain reads about an armed arrival (#203). The banner rows,
/// the Nav-panel insert lines, the plan-card step, the arm buttons and the map context menu all
/// speak through here so a station can never be handed moon vocabulary ("orbit-insert at Cinder
/// Roost (0 km)") and an orbit can never lose it. Pure text — cheap to unit-test; no ship, no
/// ephemeris. Distance is passed in already unit-labelled ("alt 313 km") so the client keeps its
/// single distance formatter.</summary>
public static class HarborVocabulary
{
    /// <summary>The clamp-match ceiling a dock arrival slows to, in km/s (from <see cref="DockRule.MatchSpeed"/>).</summary>
    public static double DockMatchKms => DockRule.MatchSpeed / 1000.0;

    private static string DockMatchText =>
        DockMatchKms.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>The armed-arrival STEP label — the banner NEXT/THEN row and the plan-card step. An
    /// orbit reads "orbit-insert at Enceladus (alt 313 km)"; a dock haven reads "dock envelope at
    /// Cinder Roost — slow to ≤8 km/s" (no orbit, no insert, no "(0 km)"). <paramref name="altitudeLabel"/>
    /// is the already-formatted, unit-labelled altitude above the surface (e.g. "alt 313 km"), or
    /// null/empty when it isn't cheaply known; it is ignored for a dock haven.</summary>
    public static string ArrivalStep(HarborClass harbor, string bodyName, string? altitudeLabel = null) =>
        harbor == HarborClass.Dock
            ? $"dock envelope at {bodyName} — slow to ≤{DockMatchText} km/s"
            : string.IsNullOrWhiteSpace(altitudeLabel)
                ? $"orbit-insert at {bodyName}"
                : $"orbit-insert at {bodyName} ({altitudeLabel})";

    /// <summary>The arm/autopilot ACTION label — the map context menu and the nav-target arm button.
    /// It answers "what will the ship DO", not which machinery runs (#203 comment item 4): a dock
    /// haven reads "✈ Autopilot: dock at Rusty's", an orbit reads "✈ Autopilot: orbit Titan".</summary>
    public static string ArmAction(HarborClass harbor, string bodyName) =>
        harbor == HarborClass.Dock
            ? $"✈ Autopilot: dock at {bodyName}"
            : $"✈ Autopilot: orbit {bodyName}";

    /// <summary>#244 item 3 (canon pass, Fable, 2026-09-05) · <b>THE ARM-MENU VERB AT A BERTH WITH NO CLAMP.</b>
    ///
    /// <para>#938 D3a took the dock vocabulary off the three μ=0 stations that carry no haven flag, because
    /// every clamp sentence at one of them named a button that cannot exist. What it left them speaking was
    /// the ORBIT vocabulary — "🛰 Arm auto-orbit at Derelict Roadster" — at three metres of dead car with no
    /// μ to orbit. Two wrong verbs is not a fix; it is the second one.</para>
    ///
    /// <para>A wreck is neither docked at nor orbited: it is CLOSED WITH, and the errand that happens there
    /// is a pickup. The verb says what the ship will do, which is the same rule <see cref="ArmAction"/> was
    /// written to (#203 comment item 4).</para></summary>
    public const string PickupArmVerb = "Close to pickup";

    /// <summary>#244 item 1 (canon pass, Fable, 2026-09-05) · <b>THE ARRIVAL, ON THE AUTOPILOT'S OWN
    /// CHANNEL.</b> Owner, arrived at the roadster: <i>"I think we dropped out of autopilot… did we miss the
    /// dock button press while warping?"</i> — because the arrival said nothing a wreck's arrival could say.
    ///
    /// <para>Three sentences' worth of work in one: the ship is <b>alongside</b> (the distance is honest now,
    /// #1121), the errand is a <b>pickup</b> and not a berthing, and the silence is the fact — nothing is
    /// promised, no clamp is named, and nobody is coming out to meet you.</para></summary>
    public const string PickupArrival = "Alongside. Inside pickup range, and nobody is answering.";

    /// <summary>The collapsed armed-step verb, e.g. "Insert at" for an orbit vs "Dock at" for a
    /// haven — the flight-plan step line's leading phrase before the body name.</summary>
    public static string ArmedStepVerb(HarborClass harbor) =>
        harbor == HarborClass.Dock ? "Dock at" : "Insert at";
}

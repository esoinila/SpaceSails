using System;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #242 · <b>"WHY AREN'T WE MOVING?"</b> — the one line that connects two numbers the panel already prints.
///
/// <para>Owner, post-undock: <i>"I was wondering why the ship don't move... the origin was set to Mars...
/// after setting to Sun we got going. We should have some tip about that in burn planning."</i></para>
///
/// <para><b>What happened, and why it is not a bug in the sim.</b> In the Mars frame the map subtracts the
/// heliocentric motion the ship SHARES with Mars. A ship cruising at 33 km/s helio but separating from Mars
/// at 3 looks parked. The ship was never stuck; the picture was. Third member of the frame-honesty family
/// (#206 every-body origins, #209 the frame-cropped ribbon) — and the mildest of the three, because both
/// numbers are already on the glass in the frame row ("v rel Mars: 5.9 · v helio: 33.5"). Nobody has to be
/// TOLD anything new here. They have to be told that those two numbers are about each other, at the one
/// moment it matters: while a burn is being planned, or while the engine is lit and nothing appears to be
/// happening.</para>
///
/// <para><b>The threshold lives here and only here.</b> The owner's own figure — <i>"say &lt;15%"</i> — is
/// <see cref="QuietFraction"/>, and both the tip and the guard that proves the tip read it from this one
/// place. A tip whose condition is typed in the razor and whose test types the same number is a test that
/// cannot fail for the reason it claims to.</para>
///
/// <para><b>One voice.</b> <see cref="Line"/> is the sentence, with the frame body's name substituted, and it
/// is the only copy of it: the Plot panel prints it and the plotting card's standing explanation
/// (<see cref="StandingExplanation"/>, the #119 idiom) sits beside it in the same words the owner wrote.</para>
/// </summary>
public static class FrameMotionTip
{
    /// <summary>
    /// How small the ship's speed in the DISPLAY frame has to be, as a fraction of its heliocentric speed,
    /// before the frame is judged to be eating the motion. The owner's own number: <i>"the ship's speed IN
    /// THE DISPLAY FRAME is a small fraction of its heliocentric speed (say &lt;15%)"</i>.
    ///
    /// <para>It is a FRACTION and not a speed on purpose — the same 15 % is the right call at Mercury's
    /// 47 km/s and at Neptune's 5.4, where any absolute cut-off would be wrong at one end or the other.</para>
    /// </summary>
    public const double QuietFraction = 0.15;

    /// <summary>
    /// True when the picture is hiding the cruise: the ship is moving in the Sun's frame, and what the
    /// captain is being shown is under <see cref="QuietFraction"/> of it.
    ///
    /// <para>A ship that genuinely is not moving (helio speed at or near zero — nothing in the scenario, but
    /// the arithmetic has to answer anyway) gets NO tip, because then the frame is not lying: it really is
    /// parked, and a line blaming the frame would be the game explaining a fact it invented.</para>
    /// </summary>
    /// <param name="frameSpeedMps">Ship speed in the display frame (m/s) — what the map is showing.</param>
    /// <param name="heliocentricSpeedMps">Ship speed in the Sun / inertial frame (m/s) — the cruise.</param>
    public static bool FrameIsEatingTheMotion(double frameSpeedMps, double heliocentricSpeedMps) =>
        heliocentricSpeedMps > 0
        && !double.IsNaN(frameSpeedMps)
        && frameSpeedMps < heliocentricSpeedMps * QuietFraction;

    /// <summary>
    /// The whole gate: the tip appears only while the captain is DOING something about motion — a burn is
    /// being planned, or the engine is lit — and the frame is eating it. Outside those two moments the same
    /// arithmetic is true half the time (every ship parked at a berth reads zero in its host's frame) and a
    /// line that is on the glass half the time is furniture, not a tip.
    /// </summary>
    /// <param name="planningABurn">The plotting panel is open — the owner's "in burn planning".</param>
    /// <param name="underThrust">The engine is lit this instant.</param>
    /// <param name="frameSpeedMps">Ship speed in the display frame (m/s).</param>
    /// <param name="heliocentricSpeedMps">Ship speed in the Sun frame (m/s).</param>
    /// <param name="frameBodyId">The display frame's body, or null for the Sun — the Sun frame cannot be
    /// hiding motion you share with the Sun, so it never raises the tip about itself.</param>
    public static bool ShouldShow(
        bool planningABurn, bool underThrust,
        double frameSpeedMps, double heliocentricSpeedMps, string? frameBodyId) =>
        (planningABurn || underThrust)
        && frameBodyId is not null
        && FrameIsEatingTheMotion(frameSpeedMps, heliocentricSpeedMps);

    /// <summary>
    /// The sentence, in the owner's own words with the frame body's name in it — the one copy, so the Plot
    /// panel and anywhere this hint ever appears again say it identically (#242's "one-voice" clause).
    /// </summary>
    public static string Line(string frameBodyName) =>
        $"Barely moving? That's the {frameBodyName} frame — it hides motion you share with {frameBodyName}. "
        + "Sun frame shows the cruise.";

    /// <summary>The face of the one-press way out, beside the line that raised it.</summary>
    public const string SwitchChip = "☀ switch to Sun";

    /// <summary>The chip's hint — what pressing it does, not what it is.</summary>
    public const string SwitchChipTitle =
        "Read the plan in the Sun's frame — the cruise you actually have, instead of the motion you do not share";

    /// <summary>
    /// The standing explanation, for the plotting card (#119's inventory idiom): the general law behind the
    /// contextual line, in the owner's words. Verbatim from the issue, because the issue is the spec.
    /// </summary>
    public const string StandingExplanation =
        "Frames hide shared motion — pick the frame of the thing you're steering RELATIVE to.";

    /// <summary>
    /// The two numbers that made the case, said the way the frame row already says them — used by the guard
    /// so the threshold it tests is the threshold the line is raised on, and available to any surface that
    /// wants to show the sum rather than assert it.
    /// </summary>
    public static string SharedMotionReadout(double frameSpeedMps, double heliocentricSpeedMps) =>
        $"{(frameSpeedMps / 1000).ToString("N1", CultureInfo.InvariantCulture)} of "
        + $"{(heliocentricSpeedMps / 1000).ToString("N1", CultureInfo.InvariantCulture)} km/s is showing";
}

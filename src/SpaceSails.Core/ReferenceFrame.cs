namespace SpaceSails.Core;

/// <summary>
/// Rendering-only reference-frame transform for the plot/nav map (#135). Near a gas giant a
/// moon-to-moon plot is useless in the solar (heliocentric) frame: the primary's ~10 km/s solar
/// orbit drowns the interesting motion. Re-expressing each plotted sample co-moving with a chosen
/// body makes the true relative motion — loops, approaches, escapes — legible.
///
/// <para>This is a pure DISPLAY transform. The projection stays inertial; nothing here re-runs
/// physics. It is one ephemeris lookup + subtraction per sample, applied at draw time only.</para>
/// </summary>
public static class ReferenceFrame
{
    /// <summary>
    /// Re-express a world-space <paramref name="sample"/> (taken at some sim time) in a frame
    /// co-moving with a body. The sample's offset from the body AT THE SAMPLE TIME is preserved and
    /// re-pinned at the body's position AT THE ANCHOR ("now") TIME, so a future path shows its true
    /// motion RELATIVE to the body while staying anchored where the body actually is on screen now.
    ///
    /// <para>An inertial/stationary frame is the special case
    /// <paramref name="bodyAtSampleTime"/> == <paramref name="bodyAtAnchorTime"/>, which is the
    /// identity — the caller should short-circuit that path so the Sun frame is byte-identical.</para>
    /// </summary>
    /// <param name="sample">The world position to re-express.</param>
    /// <param name="bodyAtSampleTime">The frame body's ephemeris position at the sample's sim time.</param>
    /// <param name="bodyAtAnchorTime">The frame body's ephemeris position at the anchor ("now") time.</param>
    public static Vector2d CoMoving(Vector2d sample, Vector2d bodyAtSampleTime, Vector2d bodyAtAnchorTime)
        => sample - bodyAtSampleTime + bodyAtAnchorTime;
}

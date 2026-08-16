namespace SpaceSails.Core;

/// <summary>
/// #825 · THE MACHINE STALLED, AND THE CONTROLS SAY SO. Owner, 2026-08-11, standing on B1 near the goods
/// car with a background build eating the CPU: <b>"why does the click to walk no longer work?"</b> — and it
/// did work, in the only sense the code could see. The click was taken, the route was planned, and then the
/// frame that should have walked it spent <see cref="SpentPerFrameSeconds"/> of legs on sixteen seconds of
/// wall clock. The dot did not move. Nothing anywhere said a word, so the only thing left to conclude was
/// that the CONTROL was broken.
///
/// <para><b>Two different facts, two different sentences.</b> <see cref="CommsLink"/>'s
/// <c>⚠ SIGNAL BREAKING UP — last update 16s ago</c> was on the glass at the same moment and is NOT this:
/// that is the scripted comms-loss episode, a fiction about the mothership's downlink, measured in the
/// excursion's own on-site clock, and it has never touched anybody's legs (a comms fault does not stop you
/// walking, and #825's first fork was checked before this file existed — no input path in the game consults
/// the comms phase). What THIS is, is the machine underneath the game failing to hand out frames. The two
/// can be true at once and they read as two lines, because a player who is told the SHIP cannot be heard
/// has been told nothing at all about why his own boots will not move.</para>
///
/// <para><b>Why the walk is clamped, and why the clamp is not the bug.</b> A frame that spans sixteen
/// seconds must not be spent as sixteen seconds of movement: the stepper resolves a move axis-separately
/// against the walls, the air bill, the nerve, the tracker and the Old Ones all ride the same frame, and a
/// hundred and forty deck units of "catch-up" would put the captain through a bulkhead with the whole world
/// none the wiser. So the walk buys <see cref="SpentPerFrameSeconds"/> and no more, and the rest of the gap
/// is <see cref="SecondsDropped"/> — time the machine owed and could not pay. That is the honest clamp. The
/// bug was that it was a SILENT one.</para>
///
/// <para><b>One clock, one threshold.</b> The banner the HUD paints and the acknowledgement an input gets
/// are the same fact asked twice, so they are the same number asked twice: <see cref="StallSeconds"/> is the
/// only threshold, derived from the only clamp, and both the picture and the verb are gated on
/// <see cref="IsStalling"/> over the caller's one staleness clock. A HUD that said the world was live while
/// the input path quietly believed otherwise would be this repo's third-named bug class (the sim doing one
/// thing while a sentence reports another) aimed squarely at the HUD itself.</para>
///
/// <para>Pure, allocation-free on the calm path, and dependent on nothing: it is arithmetic over one double
/// and three strings.</para>
/// </summary>
public static class FrameGap
{
    /// <summary>How much walking ONE frame may ever buy, in real seconds, however long that frame actually
    /// spanned. The client's <c>MoveAvatar</c> clamps to exactly this — it is named here rather than typed
    /// there so the clamp and the stall threshold below cannot drift apart, which is the whole of what "the
    /// banner and the input path read the same clock" means when it is written down.</summary>
    public const double SpentPerFrameSeconds = 0.1;

    /// <summary>How many frames' worth of time may go missing before it is a STALL rather than a hitch. Five
    /// is the judgement call: at four-fifths of a second the picture has visibly stopped and a hand on the
    /// controls has already started wondering, and anything tighter would put a banner up for the ordinary
    /// garbage-collection stutter of a Debug WASM build.</summary>
    public const double StallFrames = 5.0;

    /// <summary>The one threshold. Above this the machine is not keeping up, the frame's clamp is throwing
    /// real time away, and both the HUD and the input path must say so.</summary>
    public const double StallSeconds = SpentPerFrameSeconds * StallFrames;

    /// <summary>Is the sim behind the wall clock by enough to matter? <paramref name="gapSeconds"/> is the
    /// caller's ONE staleness clock — how long, in real seconds, since the sim last serviced a frame.</summary>
    public static bool IsStalling(double gapSeconds) => gapSeconds > StallSeconds;

    /// <summary>The walking time a stalled frame could not pay: everything past the one clamped step. Zero
    /// on a healthy frame. This is the number that makes the difference honest rather than rhetorical — at
    /// a sixteen-second gap it is 15.9 seconds of legs the captain asked for and did not get.</summary>
    public static double SecondsDropped(double gapSeconds) =>
        gapSeconds <= SpentPerFrameSeconds ? 0.0 : gapSeconds - SpentPerFrameSeconds;

    /// <summary>The banner for a REAL stall, painted across the top of the walked deck beside (never inside)
    /// the comms fiction's own line. It names the gap, because "how far behind am I" is the only question a
    /// player has once the picture stops, and it ends in the phrase the issue asked for — the controls are
    /// HELD, not lost. Empty while the machine is keeping up, so the calm path paints nothing.</summary>
    public static string StallBanner(double gapSeconds) => IsStalling(gapSeconds)
        ? $"⏱ FRAMES DROPPED — {Humanize(gapSeconds)} behind · controls held"
        : string.Empty;

    /// <summary>What a control is told when it is used inside the gap. It is a receipt, not a refusal: the
    /// order is NOT thrown away — a clicked route is a queued target that survives the stall and is walked
    /// the moment frames come back — and this is the sentence that says so, once, so the captain stops
    /// clicking a control he has been given no reason to trust.</summary>
    public const string HeldLine =
        "⏱ Order held — the machine is behind your hands. It walks the moment the frames come back.";

    /// <summary>A compact "how far behind" for the banner: tenths under a second (a stall you can only just
    /// see), whole seconds up to a minute, then minutes. Deliberately its own copy rather than
    /// <see cref="CommsLink.Humanize"/>'s — that one answers "moments" under a second, which is the right
    /// word for a downlink and the wrong one for a frame budget.</summary>
    public static string Humanize(double seconds)
    {
        if (seconds < 1.0)
        {
            return $"{System.Math.Max(0.0, seconds):0.0}s";
        }
        if (seconds < 60.0)
        {
            return $"{(int)System.Math.Round(seconds)}s";
        }
        int mins = (int)System.Math.Round(seconds / 60.0);
        return $"{System.Math.Max(1, mins)} min";
    }
}

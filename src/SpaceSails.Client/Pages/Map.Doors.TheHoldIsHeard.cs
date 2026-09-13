using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #563 · <b>FORCING A DOOR IS HEARD.</b> Owner ruling, 2026-09-13, on why a locked door needs no key:
/// <i>"We have firepower and tools. We can create the same effect as needing a key by making it slow, too
/// noisy, or dangerous in other ways."</i> SLOW was already priced — four force channels, each with its own
/// constant. This is NOISY, and it is the one of the three prices that was missing entirely: a captain could
/// put a shoulder to a ten-thousand-year seal, in a field with thirty Old Ones on it, in total silence.
///
/// <para><b>The cadence is audited, not invented.</b> <c>Map.Surface.Dig.StepDigChannel</c> emits
/// <c>ReeverHearing.Noise.Digging</c> <b>once per tick of the channel, at the channel's anchor</b>, and that
/// is the only sustained noise the game had. This does the same thing with the same call at the same
/// frequency, and the only thing that differs is the loudness: a door is <c>Noise.Clatter</c> (12 du, "sharp
/// but brief") where a shovel is <c>Noise.Digging</c> (22 du) — because a man leaning on a frame is not a man
/// swinging a shovel, and the ranges were the owner's own dial before this lane touched anything.</para>
///
/// <para><b>What that buys, in play.</b> A hold is now a decision with a radius on it. Anything dormant
/// inside twelve deck units of the door learns a PLACE — not you, the DOOR — and walks to it, arriving while
/// you still have both hands on the leaf. Outside twelve du nothing moves, so forcing a hut on the far side
/// of a site is still a quiet thing to do. And a shut leaf goes on hiding you from SIGHT the whole time
/// (#1154/#1161), which is the tension this repo has stated three times and never applied to a door:
/// <i>stone hides you from eyes, never from ears.</i></para>
///
/// <para><b>Nothing is said about it.</b> No banner, no "THEY HEAR YOU", no number. The pack turning up at
/// the doorway you are leaning on is the entire telling (#453/#456 — the game does not announce).</para>
/// </summary>
public partial class Map
{
    /// <summary>One tick of one force hold, heard. Called from every <c>Step…Channel</c> that forces a door,
    /// so there is ONE answer to "what does forcing sound like" rather than four copies of it that drift.
    /// The noise is put at the CHANNEL'S ANCHOR — the door — and never at the captain: what a noise buys an
    /// Old One is a place to walk to (#456), and the place is the frame that is talking.</summary>
    private void TheHoldIsHeard(DoorChannel ch) =>
        MakeNoise(ch.AnchorX, ch.AnchorY, ReeverHearing.Noise.Clatter);
}

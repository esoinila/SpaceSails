using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1199 · <b>THE NOTICE IS A BAND, AND SINCE TODAY IT IS ALSO A SIDE.</b>
///
/// <para><b>The band</b> (inspector, live on Selene Gate, 2026-09-18): GILT-EYE held at the bar doorway for
/// as long as the captain was ANYWHERE in his line, and Selene Gate's concourse is one open hall his doorway
/// looks straight down. Standing forty du away in plain view is not <i>tailing too close</i>; it is what a
/// stranger in a station does. The en-route hold has read <c>FootTail.LegibleDu</c> since slice 1, and
/// <see cref="TheEnRouteHoldWasALREADYABandAndStillIs"/> pins that as the standing fact it is rather than
/// claiming it — a guard that cannot fail is this repository's own named hazard.</para>
///
/// <h3>AND THE SIDE (2026-09-19)</h3>
///
/// <para>Owner, live on the T: <i>"They should act normal even if I tail from ahead."</i> A hold that reads
/// only line-and-range stops him for a captain standing where he is GOING as readily as for one coming up
/// behind him — and a man who stops dead to let past somebody in front of him is not acting normal, he is a
/// beat a player stalls by accident. So the hold reads a SIDE as well:
/// <see cref="TheCaptainInFrontOfHimDoesNotHoldHimAtAll"/> below, and its second half is RED on the shipped
/// rule.</para>
///
/// <para>What was here and is not any more — the two-pace hold at the THROAT and the leg he walked back out
/// on afterwards — went with <c>ObservationWalk.TooCloseToGoInDu</c>, which no longer exists. Everything the
/// vanish does now is one rule, and its laws are next door in
/// <see cref="TheNewspaperWithEyeHolesTests"/>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheNoticeIsABandTests
{
    private const string CanvasId = "notice-band-canvas";

    [Fact]
    public void TheEnRouteHoldWasALREADYABandAndStillIs()
    {
        // Named as the standing fact it is. Slice 1 shipped this; no lane since has added it, and a law
        // claiming otherwise would be taking credit for green.
        var walk = new ObservationWalkBench(CanvasId);

        walk.StandTheCaptain(behindHimDu: FootTail.LegibleDu * 2);
        walk.OneFrame();
        Assert.False(walk.HeIsHolding, "in plain view at twice the legibility band he must walk his errand.");

        walk.StandTheCaptain(behindHimDu: FootTail.LegibleDu / 2);
        walk.Notice();
        walk.OneFrame();
        Assert.True(walk.HeIsHolding, "on his heels in plain view he must stop.");
    }

    /// <summary>
    /// #1199 (2026-09-19) · <b>AND A CAPTAIN IN FRONT OF HIM IS NOT SOMEBODY TO LET PAST.</b> The owner's
    /// <i>"act normal even if I tail from ahead"</i>, as a measurement: the same range, the same clear line,
    /// the same latched notice, and the only thing that differs is which side of him the captain stands on.
    ///
    /// <para><b>RED on the shipped rule.</b> Until today the hold was
    /// <c>_walkNoticed &amp;&amp; clearLine &amp;&amp; rangeDu &lt;= FootTail.LegibleDu</c> with no side in it
    /// at all, so the second half of this law was false on the shipped tree and the beat could be stalled for
    /// ever by walking in first and standing still.</para>
    /// </summary>
    [Fact]
    public void TheCaptainInFrontOfHimDoesNotHoldHimAtAll()
    {
        var walk = new ObservationWalkBench(CanvasId);
        walk.Notice();

        // Behind him — the half the shipped law held for, and it still holds.
        walk.StandTheCaptain(behindHimDu: FootTail.LegibleDu / 2);
        walk.OneFrame();
        Assert.True(walk.HeIsHolding, "on his heels from behind he must stop and let the captain past.");

        // …and in FRONT of him, at the very same range, he walks on.
        walk.StandTheCaptain(behindHimDu: -(FootTail.LegibleDu / 2));
        walk.OneFrame();
        Assert.False(
            walk.HeIsHolding,
            "he stopped dead for a captain standing between him and where he was going — which is the stall "
            + "the owner watched, and it is not a man acting normal.");
    }
}

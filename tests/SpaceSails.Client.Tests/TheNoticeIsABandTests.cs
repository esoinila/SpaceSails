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

        // #1283 · …and the band is TWO PACES now, not half the range a face is legible at. See
        // TheBandIsTwoPacesAndNotTheRangeAFaceIsLegibleAt below for the measurement and the ruling.
        walk.StandTheCaptain(behindHimDu: ObservationWalk.OnHisHeelsDu / 2);
        walk.Notice();
        walk.OneFrame();
        Assert.True(walk.HeIsHolding, "on his heels in plain view he must stop.");
    }

    /// <summary>
    /// #1283 · <b>TWO PACES BEHIND IS ON HIS HEELS; TEN PACES BEHIND IN A LIT HALL IS A STRANGER.</b>
    ///
    /// <para><b>What was played</b> (#1282, 2026-09-21): after last call GILT-EYE left the bar for a car and
    /// then <b>stopped at the landing and stayed stopped for nine and a half minutes</b> while a scripted
    /// captain crossed the concourse behind him. It is the shipped #1062 notice latch doing exactly what it
    /// says, and it is a defect by measurement: the hold read <see cref="FootTail.LegibleDu"/>, which is 30
    /// du, and Selene Gate's hall is about 34 du across — so <b>any captain following in his line anywhere on
    /// that concourse holds him</b>, and the two-leg night #1276 built can never be seen past its first leg by
    /// anybody who follows at all.</para>
    ///
    /// <para><b>The ruling:</b> the en-route band is <see cref="ObservationWalk.OnHisHeelsDu"/> — the
    /// small-room constant the throat used, stated once in the room's own file. He acts normal beyond it,
    /// which is the owner's standing ruling (<i>"They should act normal even if I tail from ahead"</i>), and
    /// #1201's <i>he waits for you to go past</i> is kept at two paces only.</para>
    ///
    /// <para><b>RED on the shipped 30 du:</b> the twelve-du half fails —
    /// <c>he stopped for a captain ten paces back across a lit hall</c> — and he never reaches the tube.</para>
    /// </summary>
    [Fact]
    public void TheBandIsTwoPacesAndNotTheRangeAFaceIsLegibleAt()
    {
        // The two distances the ruling names, in deck units. TEN PACES is the one that was broken and it is
        // inside the legibility band on purpose: a case outside it would have been green on the shipped rule
        // and would have pinned nothing (§"a green test that asserts nothing").
        const double TenPacesDu = 12.0;
        const double TwoPacesDu = 4.0;

        Assert.True(
            TenPacesDu <= FootTail.LegibleDu,
            $"{TenPacesDu:0.#} du is outside the {FootTail.LegibleDu:0.#} du band the hold used to read, so "
            + "this case could never have been red on the shipped rule.");
        Assert.True(
            TwoPacesDu <= ObservationWalk.OnHisHeelsDu && ObservationWalk.OnHisHeelsDu < TenPacesDu,
            $"the en-route band ({ObservationWalk.OnHisHeelsDu:0.#} du) no longer sits between the two "
            + "distances this law is stated at.");

        // ── AT FOUR DU HE HOLDS ───────────────────────────────────────────────────────────────────────
        var close = new ObservationWalkBench(CanvasId);
        close.Notice();
        Assert.True(
            close.StandTheCaptainAlongHisLine(-TwoPacesDu),
            "the bench put the captain behind him somewhere with no line — this law would be about a wall.");
        close.OneFrame();
        Assert.True(close.HeIsHolding, "two paces behind him, on his heels, he must stand aside.");

        // ── AT TWELVE DU HE WALKS THE WHOLE LEG ───────────────────────────────────────────────────────
        var back = new ObservationWalkBench(CanvasId);
        back.Notice();
        Assert.True(
            back.StandTheCaptainAlongHisLine(-TenPacesDu),
            "the bench put the captain behind him somewhere with no line — this law would be vacuous.");
        back.OneFrame();
        Assert.False(
            back.HeIsHolding,
            $"he stopped dead for a captain {TenPacesDu:0.#} du back across a lit hall — which is the "
            + "nine-and-a-half-minute freeze the QA crew watched at the L-06 landing, and it is not a man "
            + "acting normal.");

        // …not merely unheld for one frame: he gets the whole way off the concourse with the captain held
        // at that distance every frame of it. That is the half the beat is unblocked by.
        back.RunTheWalkWithTheCaptainOnHisHeels(600.0, TenPacesDu, () => back.HeIsInTheTube);
        Assert.True(
            back.HeIsInTheTube,
            "with a captain following ten paces back he never got off the concourse, so the two-leg night "
            + "cannot be seen past its first leg by anybody who follows at all (#1283).");
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

        // Three du along his own line of travel, each way. The SAME axis, the same distance and the same
        // floor — so the only thing that differs between the two halves of this law is the sign, and the
        // oracle is asked both times so the law cannot quietly become a law about a wall.
        const double ThreeDu = 3.0;

        Assert.True(
            walk.StandTheCaptainAlongHisLine(-ThreeDu),
            "the bench put the captain behind him somewhere with no line — this law would be about a wall.");
        walk.OneFrame();
        Assert.True(walk.HeIsHolding, "on his heels from behind he must stop and let the captain past.");

        Assert.True(
            walk.StandTheCaptainAlongHisLine(ThreeDu),
            "the bench put the captain in front of him somewhere with no line — this law would be vacuous.");
        walk.OneFrame();
        Assert.False(
            walk.HeIsHolding,
            "he stopped dead for a captain standing between him and where he was going — which is the stall "
            + "the owner watched, and it is not a man acting normal.");
    }
}

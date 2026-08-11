using System.Collections.Generic;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #830 + #832 · THE PATROL INSTRUMENTS, after the evening of 2026-08-11 — the fan, the eye's edge, and the
/// sentence that is allowed to say a corridor is empty.
///
/// <para>Two failures were watched live and both were the instruments lying by omission rather than by
/// arithmetic. <b>#830:</b> <i>"the patrol should be visible in the motion detector … if the guard is still
/// then it would be a blurry blob"</i> — a man standing at a beat stop returned nothing, which was honest
/// about his velocity and wrong about him. <b>#832:</b> <i>"Now the guard just vanishes into thin air .. that
/// is like huge magic trick"</i> — the marker was cut on an invisible circle in open air.</para>
///
/// <para>The law they share is a LADDER, and the point of this file is that it has no missing rung: crisp
/// marker → distant figure → moving blip → still blob → boots in earshot → silence only past the fan. At no
/// range may a walking man be neither seen nor heard while the instruments claim to be working.</para>
///
/// <para>Every guard below was run RED against the shipped behaviour — see the PR body for the runs.</para>
/// </summary>
public class AStandingGuardIsNotSilenceTests
{
    /// <summary>A wall square across the sightline, borrowed from the rounds' own file: the one thing that is
    /// still allowed to cut a figure instantly.</summary>
    private static readonly SurfaceCollision.Segment[] AWallBetween =
        [new SurfaceCollision.Segment(5, -20, 5, 20)];

    /// <summary>The fan's real reach on B2 — a floor that actually has a round on it, so no number here is a
    /// number this test made up.</summary>
    private static double FanOnB2 => MotionTracker.UndergroundRange(MotionTracker.DetectionRange(32.0), -2);

    /// <summary>One guard, as the client's one accessor builds them: position, this frame's velocity, and
    /// the register Core declares for the kind of thing they are.</summary>
    private static MotionTracker.Entity Guard(double x, double vx) =>
        new(x, 0, vx, 0, PatrolBeat.FanRegister);

    // ── #830 · THE TWO KINDS OF RETURN ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A GUARD MID-LEG, INSIDE REACH, IS EXACTLY ONE CRISP RETURN. This is the half that was wired and never
    /// once eyeballed — #832 filed a whole session in which it never appeared — so it is pinned before the
    /// new half is.
    ///
    /// <para><b>Proven RED</b> by zeroing the walk (the issue's own suggestion): the sweep drops to a blob
    /// and the crisp assertion names it.</para>
    /// </summary>
    [Fact]
    public void AGuardWalkingInsideTheFanIsOneCrispReturn()
    {
        IReadOnlyList<MotionTracker.Blip> fan =
            MotionTracker.Sweep(0, 0, [Guard(PatrolBeat.MarkerSightDu + 5.0, PatrolBeat.WalkSpeed)], FanOnB2);

        Assert.Single(fan);
        Assert.Equal(MotionTracker.BlipKind.Crisp, fan[0].Kind);
        Assert.Equal(PatrolBeat.MarkerSightDu + 5.0, fan[0].Range, 3);

        // …and at that range the EYE has nothing, which is the handoff the ladder is made of: the fan is
        // carrying him for the whole stretch the marker cannot.
        Assert.False(PatrolBeat.DrawnFor(0, 0, PatrolBeat.MarkerSightDu + 5.0, 0, []));
    }

    /// <summary>
    /// A GUARD AT A STOP IS A BLOB, AND NEVER CLEAN SILENCE — the whole of #830.
    ///
    /// <para><b>Proven RED</b> against the shipped instrument, which returned an empty sweep for exactly this
    /// entity: the owner watched it do so with the man in front of him.</para>
    /// </summary>
    [Fact]
    public void AGuardStandingAtAStopReturnsTheUnsureBlob()
    {
        IReadOnlyList<MotionTracker.Blip> fan = MotionTracker.Sweep(0, 0, [Guard(12.0, 0)], FanOnB2);

        Assert.Single(fan);
        Assert.Equal(MotionTracker.BlipKind.Blob, fan[0].Kind);
        Assert.Equal(12.0, fan[0].Range, 3);

        // It is UNSURE, and the instrument says so in the only two ways a fan can: it draws wider than a
        // body and it paints fainter than a mover would at the same range.
        // Wider than the tolerance the game itself calls "standing at that spot" — i.e. wider than a person
        // is placed to. Core's own number, so a re-tuned round re-tunes the claim with it.
        Assert.True(MotionTracker.BlobSpreadDu(12.0) > PatrolBeat.AtTheStopDu,
            "a blob that is no wider than a body is a dot claiming to be a region.");
        Assert.True(MotionTracker.BlobSpreadDu(40.0) > MotionTracker.BlobSpreadDu(12.0),
            "a crude fan is less sure about a far return, not more.");
        Assert.InRange(MotionTracker.BlobFaintness, 0.05, 0.95);
    }

    /// <summary>
    /// A MACHINE SET DOWN RETURNS NOTHING, and that is what makes the blob mean something.
    ///
    /// <para>This is the control the fifth bug class demands: a still-return that fired for everything would
    /// be a fan that has stopped distinguishing anything. The owner's own words are the spec —
    /// <i>"a set-down sentry, a parked droid — genuinely inert — return nothing. The fan distinguishing
    /// 'machine-still' from 'alive-and-holding-still' is the whole horror of the instrument."</i></para>
    /// </summary>
    [Fact]
    public void AnInertMachineIsStillSilence()
    {
        // A sentry set down at the captain's feet: no velocity, no register.
        var sentry = new MotionTracker.Entity(2.0, 0, 0, 0);
        Assert.Null(MotionTracker.Read(0, 0, in sentry));
        Assert.Empty(MotionTracker.Sweep(0, 0, [sentry], FanOnB2));

        // …and so is a contact whose owner has declared that it is holding on purpose, which is the tide's
        // own shipped ruling (ReeverIdle): stillness is EARNED, and an Old One in its deep has earned it.
        Assert.Empty(MotionTracker.Sweep(0, 0, [new MotionTracker.Entity(2.0, 0, 0, 0,
            MotionTracker.AtRest.Quiet)], FanOnB2));
    }

    /// <summary>The blob carries a shorter distance than a walker — a smaller signal honestly drawn — and
    /// still, on the floor this feature lives on, far past anything the captain's eye can do.</summary>
    [Fact]
    public void TheBlobCarriesLessFarThanAWalkerAndStillFurtherThanTheEye()
    {
        double blobReach = FanOnB2 * MotionTracker.StillReachFraction;

        Assert.True(blobReach < FanOnB2, "a body holding still is not as loud as one walking.");
        Assert.True(blobReach > PatrolBeat.MarkerSightDu,
            $"the still-blob only reaches {blobReach:F0} du — inside the eye, which makes it pointless.");

        Assert.Single(MotionTracker.Sweep(0, 0, [Guard(blobReach - 1.0, 0)], FanOnB2));
        Assert.Empty(MotionTracker.Sweep(0, 0, [Guard(blobReach + 1.0, 0)], FanOnB2));

        // The walker is heard across that same gap, which is what "less far" has to mean.
        Assert.Single(MotionTracker.Sweep(0, 0, [Guard(blobReach + 1.0, PatrolBeat.WalkSpeed)], FanOnB2));
    }

    // ── #830 law 4 · THE SENTENCE AND THE SWEEP READ THE SAME LIST ────────────────────────────────────

    /// <summary>
    /// "no movement — for now" MAY ONLY PRINT ON AN EMPTY FAN.
    ///
    /// <para><b>Proven RED</b> by pointing the readout back at the nearest MOVER (the shipped call), which
    /// prints the cold line over a fan holding a blob — which is precisely the screen the owner was looking
    /// at when he filed #830.</para>
    /// </summary>
    [Fact]
    public void TheNoMovementLineNeverCoexistsWithAReturn()
    {
        string cold = MotionTracker.Readout(null, closing: false);

        IReadOnlyList<MotionTracker.Blip> nothing = MotionTracker.Sweep(0, 0, [], FanOnB2);
        Assert.Equal(cold, MotionTracker.ReadoutOf(nothing, closing: false));
        Assert.Equal(MotionTracker.Cadence.Silent, MotionTracker.CadenceOf(nothing));

        // A standing guard: the fan is holding something, so the cold line is not available to it.
        IReadOnlyList<MotionTracker.Blip> blob = MotionTracker.Sweep(0, 0, [Guard(20.0, 0)], FanOnB2);
        Assert.NotEqual(cold, MotionTracker.ReadoutOf(blob, closing: false));
        Assert.Equal(MotionTracker.StillReadout(20.0), MotionTracker.ReadoutOf(blob, closing: false));
        Assert.NotEqual(MotionTracker.Cadence.Silent, MotionTracker.CadenceOf(blob));

        // A walker outranks a blob in the sentence: it is the more urgent fact, and the only one "closing"
        // is about.
        IReadOnlyList<MotionTracker.Blip> both =
            MotionTracker.Sweep(0, 0, [Guard(20.0, 0), Guard(35.0, PatrolBeat.WalkSpeed)], FanOnB2);
        Assert.Equal(2, both.Count);
        Assert.Equal(MotionTracker.Readout(35.0, closing: true), MotionTracker.ReadoutOf(both, closing: true));
        Assert.Equal(35.0, MotionTracker.NearestMoving(both));
        Assert.Equal(20.0, MotionTracker.NearestReturn(both));
    }

    // ── #832 · THE EYE'S EDGE ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE MARKER TIER HAS NO JUMP FROM FULL TO NOTHING. Walked out along an open corridor a tenth of a deck
    /// unit at a time: the tier only ever falls, it falls one rung at a time, and every rung is actually
    /// visited — a smear band that existed in the enum and never in the arithmetic would be the fifth bug
    /// class with a silhouette on it.
    ///
    /// <para><b>Proven RED</b> against the shipped <c>DrawnFor</c> hard cutoff, which steps Plain → None with
    /// no rung between them.</para>
    /// </summary>
    [Fact]
    public void TheMarkerFadesThroughADistantFigureAndNeverPops()
    {
        var seen = new HashSet<PatrolBeat.Sighting>();
        PatrolBeat.Sighting previous = PatrolBeat.Sighting.Plain;

        for (double at = 0.1; at < PatrolBeat.MarkerSightDu + 6.0; at += 0.1)
        {
            PatrolBeat.Sighting now = PatrolBeat.SightingFor(0, 0, at, 0, []);
            seen.Add(now);

            Assert.True(now <= previous, $"the figure came BACK at {at:F1} du: {previous} → {now}.");
            Assert.True((int)previous - (int)now <= 1,
                $"the figure jumped {previous} → {now} at {at:F1} du — that is the magic trick (#832).");
            previous = now;
        }

        Assert.Contains(PatrolBeat.Sighting.Plain, seen);
        Assert.Contains(PatrolBeat.Sighting.Smear, seen);
        Assert.Contains(PatrolBeat.Sighting.None, seen);

        // The band is where the owner put it: the outer fifth, and nowhere near most of the reach.
        Assert.Equal(PatrolBeat.Sighting.Plain, PatrolBeat.SightingFor(0, 0, PatrolBeat.SmearFromDu - 0.5, 0, []));
        Assert.Equal(PatrolBeat.Sighting.Smear, PatrolBeat.SightingFor(0, 0, PatrolBeat.SmearFromDu + 0.5, 0, []));
        Assert.Equal(PatrolBeat.Sighting.None, PatrolBeat.SightingFor(0, 0, PatrolBeat.MarkerSightDu + 0.5, 0, []));
        Assert.InRange(PatrolBeat.SmearFromFraction, 0.5, 0.95);
    }

    /// <summary>
    /// A WALL STILL CUTS INSTANTLY, and it gets no smear tier. That is the one popping the owner did not
    /// complain about, because a body stepping behind shotcrete IS gone — softening it would hand back the
    /// cover the whole timing game is played against.
    /// </summary>
    [Fact]
    public void AWallTakesTheFigureAtOnceAndAtEveryRange()
    {
        // Point-blank, mid-reach and out at the smear band: behind stone there is nothing at all, ever.
        foreach (double at in new[] { 6.0, 15.0, PatrolBeat.SmearFromDu + 1.0 })
        {
            Assert.NotEqual(PatrolBeat.Sighting.None, PatrolBeat.SightingFor(0, 0, at, 0, []));
            Assert.Equal(PatrolBeat.Sighting.None, PatrolBeat.SightingFor(0, 0, at, 0, AWallBetween));
        }

        // …and DrawnFor is the same question asked once, so the two can never disagree about it.
        Assert.False(PatrolBeat.DrawnFor(0, 0, 15.0, 0, AWallBetween));
        Assert.True(PatrolBeat.DrawnFor(0, 0, 15.0, 0, []));
    }

    // ── #832 · THE RETIRED-COP LAW ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A GUARD IS NEVER THE QUIET ONE. Owner: <i>"the guard looks like retired cop, not ninja, let's make
    /// them easy to detect :-D … Guards get the GENEROUS end of every instrument."</i>
    ///
    /// <para>Measured against the ordinary contact class rather than against a number, so the law survives
    /// somebody re-tuning the fan: at the same range and the same speed a guard's return is at least as far
    /// and at least as certain as anybody else's, and the guard is the one class that also returns something
    /// while standing.</para>
    /// </summary>
    [Fact]
    public void TheGuardGetsTheGenerousEndOfEveryInstrument()
    {
        // Well out — twice the eye's own reach, and a range at which every class of contact is inside the
        // fan whether it is walking or not.
        double wellOut = PatrolBeat.MarkerSightDu * 2.0;
        var anybody = new MotionTracker.Entity(wellOut, 0, PatrolBeat.WalkSpeed, 0);
        MotionTracker.Entity guard = Guard(wellOut, PatrolBeat.WalkSpeed);

        IReadOnlyList<MotionTracker.Blip> theirs = MotionTracker.Sweep(0, 0, [anybody], FanOnB2);
        IReadOnlyList<MotionTracker.Blip> his = MotionTracker.Sweep(0, 0, [guard], FanOnB2);
        Assert.Equal(theirs.Count, his.Count);
        Assert.Equal(theirs[0].Kind, his[0].Kind);
        Assert.Equal(theirs[0].Range, his[0].Range, 6);

        // …and standing, where every other class of contact goes silent, he does not.
        Assert.Empty(MotionTracker.Sweep(0, 0, [anybody with { Vx = 0 }], FanOnB2));
        Assert.Single(MotionTracker.Sweep(0, 0, [guard with { Vx = 0 }], FanOnB2));

        // The ear is generous the same way: his boots carry as far as the captain's own eye (#832), so no
        // band exists in which a walking man is neither seen nor heard.
        Assert.True(PatrolBeat.EarshotDu >= PatrolBeat.MarkerSightDu);
        for (double at = 1.0; at <= PatrolBeat.MarkerSightDu; at += 0.5)
        {
            Assert.True(
                PatrolBeat.DrawnFor(0, 0, at, 0, AWallBetween)
                || PatrolBeat.Heard(0, 0, at, 0, AWallBetween),
                $"at {at:F1} du behind a wall a guard is neither seen nor heard (#832).");
        }
    }
}

using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #538 · THE SWEEP TEAM, AS LAWS. Owner: <i>"One of the salvages could have a black ops inspection team go
/// through them where we take our guns and hide to let them pass"</i>, and on its character: <i>"That place would
/// be really cut-throat as they want to keep their secrets, but the rewards could be big also"</i>, and on its best
/// moment: <i>"It might be sweet if they fought off some reevers etc while the pirates hide."</i>
/// </summary>
public sealed class InspectionTeamTests
{
    private static InspectionTeam.Member Sweeper(
        double x = 0, double y = 0, double facing = 0,
        InspectionTeam.Awareness state = InspectionTeam.Awareness.Sweeping, double seconds = 0) =>
        new("SWEEP-1", x, y, facing, state, seconds);

    /// <summary>A wall down the middle, for testing that they are not exempt from the maze.</summary>
    private static IReadOnlyList<SurfaceCollision.Segment> WallAcross() =>
        [new SurfaceCollision.Segment(5, -10, 5, 10)];

    // ── What they can perceive, and the counter-play it creates ───────────────────────────────────────

    /// <summary>
    /// THE CHARACTER OF THE THREAT, AS ONE PAIR OF ASSERTIONS. They see FAR and only where the lamp points — the
    /// exact inverse of the pack, which notices whatever is close in any direction. That inversion is the reason
    /// this scene plays differently from every other threat aboard: you do not out-run a professional, you stand
    /// somewhere the cone is not.
    /// </summary>
    [Fact]
    public void TheySeeFurtherThanThePackAndOnlyWhereTheLampPoints()
    {
        InspectionTeam.Member m = Sweeper(facing: 0);   // looking along +X

        Assert.True(InspectionTeam.Sees(m, 18, 0, null), "far, and straight down the lamp");
        Assert.False(InspectionTeam.Sees(m, 30, 0, null), "past the lamp's reach");
        Assert.False(InspectionTeam.Sees(m, 0, 6, null), "close, and off to the side — the whole counter-play");
        Assert.False(InspectionTeam.Sees(m, -18, 0, null), "directly behind them");
    }

    /// <summary>Standing STILL beside them beats running past them. Pinned because it is the instruction the scene
    /// silently gives the player, and if the arithmetic ever inverted the advice would become a lie.</summary>
    [Fact]
    public void ThreeMetresToTheSideIsSaferThanTwentyDownTheirAxis()
    {
        InspectionTeam.Member m = Sweeper(facing: 0);

        Assert.False(InspectionTeam.Sees(m, 1.0, 3.0, null), "beside them, well inside the lamp's reach");
        Assert.True(InspectionTeam.Sees(m, 19.0, 0.0, null), "and nearly twenty du away down the axis");
    }

    /// <summary>WALLS ARE LAW FOR THEM TOO (#324). The one thing that never gets fudged in this codebase: an
    /// inspector genuinely cannot see through a bulkhead, which is what makes hiding a mechanic rather than a
    /// dice roll.</summary>
    [Fact]
    public void ABulkheadStopsAProfessionalExactlyAsItStopsEverybodyElse()
    {
        InspectionTeam.Member m = Sweeper(facing: 0);

        Assert.True(InspectionTeam.Sees(m, 10, 0, null));
        Assert.False(InspectionTeam.Sees(m, 10, 0, WallAcross()));
    }

    /// <summary>
    /// THEIR EARS DO NOT CARE WHERE THE LAMP IS POINTED, and that is why noise discipline is the actual verb of
    /// this scene. If sound respected the cone, hiding would collapse into standing in the right corner and the
    /// pumps, hatches and guns would stop mattering.
    /// </summary>
    [Fact]
    public void NoiseReachesThemFromBehindAndThroughWalls()
    {
        InspectionTeam.Member m = Sweeper(facing: 0);

        Assert.True(InspectionTeam.Hears(m, -20, 0), "behind them");
        Assert.True(InspectionTeam.Hears(m, 0, 30), "abeam");
        Assert.False(InspectionTeam.Hears(m, 0, 40), "and there is still a limit");

        // Hearing outreaches seeing, deliberately: they find you by what you did, not by looking at you.
        Assert.True(InspectionTeam.EarshotDeckUnits > InspectionTeam.LampRange);
    }

    /// <summary>The cone the renderer draws and the cone the rule checks are the same cone. A cone drawn wider
    /// than it is tested is a lie the player learns the expensive way.</summary>
    [Fact]
    public void TheDrawnConeAndTheCheckedConeAreOneNumber()
    {
        Assert.Equal(0, InspectionTeam.OffAxisDegrees(0, 1, 0), 6);
        Assert.Equal(90, InspectionTeam.OffAxisDegrees(0, 0, 1), 6);
        Assert.Equal(180, InspectionTeam.OffAxisDegrees(0, -1, 0), 6);

        // …and the boundary is exactly where the constant says it is.
        double edge = InspectionTeam.LampConeHalfAngleDegrees * Math.PI / 180.0;
        InspectionTeam.Member m = Sweeper(facing: 0);
        Assert.True(InspectionTeam.Sees(m, 10 * Math.Cos(edge * 0.95), 10 * Math.Sin(edge * 0.95), null));
        Assert.False(InspectionTeam.Sees(m, 10 * Math.Cos(edge * 1.05), 10 * Math.Sin(edge * 1.05), null));
    }

    // ── The beat that makes it a stealth scene rather than a coin flip ────────────────────────────────

    /// <summary>
    /// THE MOST IMPORTANT LAW IN THE FILE. Being seen must not be the same event as being dead, or hiding becomes
    /// a coin flip nobody can play well. A professional challenges first, and the captain gets that beat to spend.
    /// </summary>
    [Fact]
    public void BeingSeenIsNotYetBeingShot()
    {
        InspectionTeam.Member m = Sweeper(state: InspectionTeam.Awareness.Challenging, seconds: 0);
        Assert.False(InspectionTeam.ChallengeExpired(m));

        Assert.False(InspectionTeam.ChallengeExpired(m with { StateSeconds = InspectionTeam.ChallengeSeconds - 0.1 }));
        Assert.True(InspectionTeam.ChallengeExpired(m with { StateSeconds = InspectionTeam.ChallengeSeconds }));
    }

    /// <summary>…and only a challenge can expire. A sweeper who has merely heard something never times out into
    /// shooting somebody, however long they search.</summary>
    [Theory]
    [InlineData(InspectionTeam.Awareness.Sweeping)]
    [InlineData(InspectionTeam.Awareness.Investigating)]
    [InlineData(InspectionTeam.Awareness.Hunting)]
    // #731 v2 · …including the one that walks them off the ship. A team going home is the safest thing in
    // this scene and a state that timed out into a kill would be the least readable trap in the game.
    [InlineData(InspectionTeam.Awareness.Leaving)]
    public void NoOtherStateTimesOutIntoKillingAnybody(InspectionTeam.Awareness state) =>
        Assert.False(InspectionTeam.ChallengeExpired(Sweeper(state: state, seconds: 9_999)));

    /// <summary>
    /// THE BEAT HAS TO BE WORTH SPENDING. A captain runs at 9 du/s (the WASD law the UI gate measures distances
    /// with); a sweeper closes at <see cref="InspectionTeam.SweepSpeed"/>. Over the challenge the captain must
    /// cover meaningfully more ground than the sweeper does, or "run for cover" is advice that does not work.
    /// </summary>
    [Fact]
    public void TheChallengeIsLongEnoughToEscapeAndShortEnoughToCost()
    {
        const double captainSpeed = 9.0;   // du/s, the deck's own walk speed
        double captainCovers = captainSpeed * InspectionTeam.ChallengeSeconds;
        double sweeperCloses = InspectionTeam.SweepSpeed * InspectionTeam.ChallengeSeconds;

        Assert.True(captainCovers > sweeperCloses * 2, "the escape must be real");
        Assert.True(captainCovers < InspectionTeam.LampRange * 2, "and it must not be a stroll");
        Assert.InRange(InspectionTeam.ChallengeSeconds, 1.5, 6.0);
    }

    /// <summary>They are slower than the captain in both gears — they do not need to catch you, they need to
    /// corner you — and hunting is faster than sweeping, because a decision changes how you walk.</summary>
    [Fact]
    public void TheyNeverOutrunACaptainAndTheyDoSpeedUpOnceTheyKnow()
    {
        Assert.True(InspectionTeam.SweepSpeed < InspectionTeam.HuntSpeed);
        Assert.True(InspectionTeam.HuntSpeed < 9.0, "a professional corners; they do not sprint you down");
    }

    /// <summary>Being seen has a cost that outlives the escape: they do not shrug and go back to the checklist.</summary>
    [Fact]
    public void HavingBeenSeenGoesOnCostingAfterYouGetAway()
    {
        Assert.True(InspectionTeam.HuntPersistenceSeconds > InspectionTeam.SearchSeconds);
        Assert.True(InspectionTeam.SearchSeconds > InspectionTeam.ChallengeSeconds);
    }

    /// <summary>Nerve is spent in the right order: a lamp on your face is worse than footsteps somewhere, and a
    /// sweep that has not noticed you costs nothing at all.</summary>
    [Fact]
    public void FearIsPricedInTheOrderItActuallyArrives()
    {
        Assert.Equal(0, InspectionTeam.NervePerSecond(InspectionTeam.Awareness.Sweeping));
        // #731 v2 · …and a team WALKING OUT costs nothing either. They are not looking for you any more, and
        // the whole read of that beat is that nothing about it is aimed at the captain.
        Assert.Equal(0, InspectionTeam.NervePerSecond(InspectionTeam.Awareness.Leaving));
        Assert.True(InspectionTeam.NervePerSecond(InspectionTeam.Awareness.Investigating)
                    < InspectionTeam.NervePerSecond(InspectionTeam.Awareness.Hunting));
        Assert.True(InspectionTeam.NervePerSecond(InspectionTeam.Awareness.Hunting)
                    < InspectionTeam.NervePerSecond(InspectionTeam.Awareness.Challenging));
    }

    // ── The three-body rule ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PACK WINS THE ARGUMENT — and not as a courtesy to the player. A Reever is charging them; a captain is a
    /// compliance problem down a corridor. A team that engaged the compliance problem first would be a team that
    /// died aboard, so this is characterisation and mercy at the same time.
    /// </summary>
    [Theory]
    [InlineData(false, false, 5, InspectionTeam.Priority.Neither)]
    [InlineData(true, false, 5, InspectionTeam.Priority.TheCaptain)]
    [InlineData(false, true, 5, InspectionTeam.Priority.ThePack)]
    [InlineData(true, true, 5, InspectionTeam.Priority.ThePack)]       // the owner's favourite outcome
    [InlineData(true, true, 999, InspectionTeam.Priority.TheCaptain)]  // …unless the pack is nowhere near
    public void ThePackOutranksTheCaptainWheneverItIsActuallyThere(
        bool captain, bool pack, double packRange, InspectionTeam.Priority expected) =>
        Assert.Equal(expected, InspectionTeam.WhoTheyDealWith(captain, pack, packRange));

    // ── What is said ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE TELL STATES FACTS AND DRAWS NO CONCLUSION — the #533 discipline, so choosing this hull is a decision
    /// rather than an ambush. If it ever starts telling the captain what to do, the whole breadcrumb idiom leaks.
    /// </summary>
    [Fact]
    public void ThePreKnowledgeWarnsWithoutAdvising()
    {
        string tell = InspectionTeam.PreKnowledgeLine;

        Assert.Contains("transponder", tell, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("advisory", tell, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("should", tell, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("do not", tell, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("danger", tell, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The challenge is quiet. A professional who shouts is a professional the player can misread as a
    /// thug, and the flatness is the characterisation.</summary>
    [Fact]
    public void TheChallengeIsFlatAndNotShouted()
    {
        string line = InspectionTeam.ChallengeLine("SWEEP-2");

        Assert.Contains("SWEEP-2", line, StringComparison.Ordinal);
        Assert.Contains("stand still", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nobody is shouting", line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The three-body line offers the captain nothing and asks them for nothing — the scene's whole
    /// appeal is that helping either side is a decision with no correct answer.</summary>
    [Fact]
    public void TheThreeBodyLineRecommendsNothing()
    {
        string line = InspectionTeam.ThreeBodyLine("SWEEP-3");

        Assert.Contains("not required to help", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("now is your chance", line, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AND THE ONE THAT TIES THE TWO SYSTEMS TOGETHER. This scene is the reason
    /// <see cref="SilentRunning"/> exists — until now, going dark cost three things and bought nothing. So the
    /// warning names both pieces of the captain's own automation that will give them away.
    /// </summary>
    [Fact]
    public void TheWarningNamesBothOfYourOwnGunsByJob()
    {
        string warning = InspectionTeam.YourGunsWillGiveYouAwayLine;

        Assert.Contains("bots", warning, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tube gun", warning, StringComparison.OrdinalIgnoreCase);
        // …and it says the general law rather than only listing the two current offenders.
        Assert.Contains("still making decisions", warning, StringComparison.OrdinalIgnoreCase);
    }

    // ── What the first playtest caught ────────────────────────────────────────────────────────────────

    /// <summary>
    /// BEING SHOT BY A PROFESSIONAL IS NOT BEING RUN DOWN BY THE PACK. The first playtest of this scene ended
    /// with three men with rifles shooting a captain standing in a corridor, and a death card that read
    /// <i>"the Old Ones took you… they ran you down on Quiet Sister's regolith short of the tube"</i> — because
    /// the only surface death that existed was the pack's, and the sweep team borrowed it.
    ///
    /// <para>In this codebase that is the bug and not a wording nit: the sim did one thing and the panel reported
    /// another. So <see cref="DeathCause.Inspected"/> exists, and these are the properties that make it honest.</para>
    /// </summary>
    [Fact]
    public void BeingFoundAboardNarratesAsItselfAndNotAsThePack()
    {
        Assert.NotEqual(DeathNarration.Headline(DeathCause.Reevers), DeathNarration.Headline(DeathCause.Inspected));
        Assert.Contains("found aboard", DeathNarration.Headline(DeathCause.Inspected), StringComparison.OrdinalIgnoreCase);

        // Three seeds, because the pool is picked by roll and every line in it has to be about the right death.
        foreach (ulong seed in new ulong[] { 1, 7, 99 })
        {
            string line = DeathNarration.Line(DeathCause.Inspected, seed, "Quiet Sister");

            Assert.DoesNotContain("Old One", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Reever", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("regolith", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("nerve", line, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(line));
        }
    }

    /// <summary>
    /// …AND ALL THREE PIECES OF THE CARD AGREE. The first fix caught the headline and the narration line and
    /// left the PULL-QUOTE still reading <i>"…they simply kept coming, and the guard did not hold forever"</i>
    /// — a pack quote, under a headline that says you were found aboard, after three professionals shot a
    /// captain standing still. Booting the scene found it; this stops it coming back.
    /// </summary>
    [Fact]
    public void EveryPartOfTheDeathCardNarratesTheSameDeath()
    {
        string quote = DeathNarration.SurfaceCaption(DeathCause.Inspected, nerveRanOut: false);

        Assert.DoesNotContain("kept coming", quote, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guard", quote, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(DeathNarration.SurfaceCaption(DeathCause.Reevers, nerveRanOut: false), quote);

        // …and it does not blame the nerve either, whichever way the trigger flags it.
        foreach (bool ranOut in new[] { true, false })
        {
            Assert.DoesNotContain("nerve", DeathNarration.SurfaceCaption(DeathCause.Inspected, ranOut),
                                  StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>And it has a canvas, like every other cause — even a borrowed one — so the card never renders a
    /// hole where the picture goes.</summary>
    [Fact]
    public void BeingFoundAboardStillHasSomethingToShow() =>
        Assert.False(string.IsNullOrWhiteSpace(DeathNarration.ArtFile(DeathCause.Inspected)));

    /// <summary>Every sweeper has a callsign and the team is the size the file says it is.</summary>
    [Fact]
    public void TheTeamIsLegibleEnoughToHideFrom()
    {
        Assert.Equal(InspectionTeam.TeamSize, InspectionTeam.Callsigns.Count);
        Assert.Equal(InspectionTeam.TeamSize, InspectionTeam.Callsigns.Distinct(StringComparer.Ordinal).Count());
        Assert.InRange(InspectionTeam.TeamSize, 2, 4);
    }
}

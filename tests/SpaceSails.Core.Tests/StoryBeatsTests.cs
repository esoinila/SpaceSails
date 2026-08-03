using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #528 · THE OWNER'S LAW, AS LAWS. <i>"The pattern something of importance to story telling happens we get image
/// pop up should happen universally in the game as long as it does not block the playing too much or be too
/// repetitive."</i>
///
/// <para>Two constraints, and both of them are the sort of thing that rots quietly: a cadence that slips to
/// "every time" teaches players to dismiss cards unread, and a modal that lands mid-fight is how a story card
/// gets somebody killed. So both are held here rather than in a habit.</para>
/// </summary>
public sealed class StoryBeatsTests
{
    private static readonly StoryBeats.Beat[] All = Enum.GetValues<StoryBeats.Beat>();

    /// <summary>Every beat has a picture, a title and a caption. A beat that reaches the player half-dressed is
    /// worse than one that never fires.</summary>
    [Fact]
    public void EveryBeatIsFullyWritten()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            Assert.False(string.IsNullOrWhiteSpace(StoryBeats.ArtFile(beat)), $"{beat} has no art file");
            Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Title(beat)), $"{beat} has no title");
            Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Caption(beat)), $"{beat} has no caption");
        }
    }

    /// <summary>The art path is a real repo path shape, so a typo shows up here and not as a missing picture in
    /// front of the owner.</summary>
    [Fact]
    public void EveryArtPathPointsIntoTheArtFolder()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            string art = StoryBeats.ArtFile(beat);
            Assert.StartsWith("art/", art, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", art, StringComparison.Ordinal);
        }
    }

    /// <summary>Captions read whole with or without a subject — the caller may not have a name to give, and the
    /// prose must never come out with a hole in it.</summary>
    [Fact]
    public void CaptionsReadWholeWithoutASubject()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            string bare = StoryBeats.Caption(beat);
            string named = StoryBeats.Caption(beat, "THE QUIET SISTER");

            Assert.DoesNotContain("{", bare, StringComparison.Ordinal);
            Assert.DoesNotContain("  ", bare, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(named));
        }
    }

    // ── "Not too repetitive" ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A COOLED BEAT MUST ACTUALLY HAVE A COOLDOWN, and an every-time beat must not pretend to. This is the test
    /// that stops the cadence system decaying into "everything fires always", which is how a game teaches players
    /// to dismiss its cards without reading them.
    /// </summary>
    [Fact]
    public void ACooledBeatHasACooldownAndTheOthersDoNotNeedOne()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            if (StoryBeats.CadenceOf(beat) == StoryBeats.Cadence.Cooled)
            {
                Assert.True(StoryBeats.CooldownSeconds(beat) > 0,
                            $"{beat} is cooled but would speak again immediately");
            }
            else
            {
                Assert.Equal(0.0, StoryBeats.CooldownSeconds(beat));
            }
        }
    }

    /// <summary>The first round you ever fire is a milestone, so it is once per captain and nothing else.</summary>
    [Fact]
    public void TheFirstShotIsOnceInACaptainsLife() =>
        Assert.Equal(StoryBeats.Cadence.OnceEver, StoryBeats.CadenceOf(StoryBeats.Beat.FirstShotFired));

    /// <summary>
    /// Only moments that are rare BY THEIR OWN NATURE may fire every time. If this list ever grows a routine
    /// event, the instrument is being spent on wallpaper.
    ///
    /// <para>The list is asserted whole rather than by property on purpose: adding a beat to it should require
    /// editing this test and saying why. Each of these is once-in-a-run by its own physics — a collector
    /// actually catching you, the crew meeting without you, an arc breaking, and (#524) finding a hull still
    /// burning, which is one cause in ten and guarded to one card per boarding by the wreck itself.</para>
    /// </summary>
    [Fact]
    public void OnlyRareMomentsFireEveryTime()
    {
        StoryBeats.Beat[] everyTime = [.. All.Where(b => StoryBeats.CadenceOf(b) == StoryBeats.Cadence.EveryTime)];

        Assert.Equal(
            [
                StoryBeats.Beat.CollectorHail,
                StoryBeats.Beat.CrewMeeting,
                StoryBeats.Beat.ArcNewsBreaks,
                StoryBeats.Beat.FireAboard,
            ],
            everyTime);
    }

    /// <summary>
    /// #541 · ONCE PER SUBJECT, and the same whole-list discipline: adding a beat here should require editing this
    /// test and saying why. The arrival tube is what this cadence was invented for, and the reason is worth writing
    /// down — the beat is about a PLACE rather than about the captain. <c>OnceEver</c> would have shown one berth's
    /// gangway and silently swallowed every other berth in the system; <c>EveryTime</c> would have made docking at
    /// a place you live at annoying. So: each berth's establishing shot, once, for that berth.
    /// </summary>
    [Fact]
    public void OnlyBeatsAboutAPlaceFireOncePerSubject()
    {
        StoryBeats.Beat[] perSubject =
            [.. All.Where(b => StoryBeats.CadenceOf(b) == StoryBeats.Cadence.OncePerSubject)];

        Assert.Equal(
            [
                StoryBeats.Beat.BerthGreatPort,
                StoryBeats.Beat.BerthWorkingBerth,
                StoryBeats.Beat.BerthOutpost,
            ],
            perSubject);
    }

    /// <summary>An arrival must never hold up a docking, so every tube is a plate — and each one carries the
    /// tube's own words rather than a second copy of them, so the tier rule and the picture cannot disagree.</summary>
    [Theory]
    [InlineData(ArrivalTube.Tier.GreatPort)]
    [InlineData(ArrivalTube.Tier.WorkingBerth)]
    [InlineData(ArrivalTube.Tier.Outpost)]
    public void TheArrivalTubeSpeaksThroughThisSeamAndNeverTakesTheScreen(ArrivalTube.Tier tier)
    {
        StoryBeats.Beat beat = ArrivalTube.BeatFor(tier);

        Assert.Equal(StoryBeats.Presentation.Plate, StoryBeats.PresentationOf(beat));
        Assert.Equal(ArrivalTube.ArtFile(tier), StoryBeats.ArtFile(beat));
        Assert.Equal(ArrivalTube.Title(tier), StoryBeats.Title(beat));
        Assert.Contains(ArrivalTube.WalkLine(tier), StoryBeats.Caption(beat), StringComparison.Ordinal);
    }

    /// <summary>
    /// LAB 43 CORRECTED THIS CAPTION, and the law keeps it corrected. The discharge card used to end
    /// <i>"Everything with a telescope just watched that happen"</i> — but a discharge is 85,514× dimmer than her
    /// own reflected sunlight, so nobody watches it through anything. She is not brighter; she is LOUDER.
    /// </summary>
    [Fact]
    public void TheDischargeCardSaysHeardAndNotSeen()
    {
        string caption = StoryBeats.Caption(StoryBeats.Beat.ChargeLetGo);

        Assert.DoesNotContain("telescope", caption, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("receiver", caption, StringComparison.OrdinalIgnoreCase);
    }

    // ── "Does not block the playing too much" ─────────────────────────────────────────────────────────

    /// <summary>
    /// THE RULE THE WRECK LANE PAID FOR. A full-screen card once let a pack of Reevers kill the captain behind
    /// it. So a beat that fires while the game is still moving must be a PLATE, and the two combat beats are
    /// exactly that.
    /// </summary>
    [Theory]
    [InlineData(StoryBeats.Beat.FirstShotFired)]
    [InlineData(StoryBeats.Beat.SailHoled)]
    [InlineData(StoryBeats.Beat.ChargeLetGo)]
    public void MomentsThatHappenMidActionNeverTakeTheScreen(StoryBeats.Beat beat) =>
        Assert.Equal(StoryBeats.Presentation.Plate, StoryBeats.PresentationOf(beat));

    /// <summary>A plate is up long enough to read and short enough not to become furniture.</summary>
    [Fact]
    public void APlateIsReadableAndThenGone()
    {
        Assert.True(StoryBeats.PlateSeconds >= 4.0);
        Assert.True(StoryBeats.PlateSeconds <= 12.0);
    }

    /// <summary>Cards may wait for a calm moment; plates never need to, because they never blocked anything. And
    /// the one card that must NOT wait is the one that IS the danger — deferring a collector's grapples until
    /// things calm down would be absurd.</summary>
    [Fact]
    public void OnlyCardsDeferAndTheDangerItselfNeverDoes()
    {
        Assert.False(StoryBeats.DeferrableWhileInDanger(StoryBeats.Beat.CollectorHail));

        foreach (StoryBeats.Beat beat in All)
        {
            if (StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Plate)
            {
                Assert.False(StoryBeats.DeferrableWhileInDanger(beat),
                             $"{beat} is a plate; it has nothing to defer");
            }
        }
    }
}

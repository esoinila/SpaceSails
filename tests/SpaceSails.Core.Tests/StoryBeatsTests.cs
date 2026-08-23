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

    /// <summary>
    /// #664 · THE BEATS WHOSE PICTURE IS CHOSEN BY THEIR SUBJECT, read off the seam rather than listed here.
    /// A KAAMOS or NEBULA shard's plate is picked by the fragment the captain just assembled, so asking one
    /// of them what it looks like <i>in general</i> is asking a question the world does not have an answer
    /// to: it resolves to nothing at all, which is the honest degrade and not a hole in the prose.
    ///
    /// <para>The three sweeps below therefore ask <see cref="StoryBeats.Canvases"/> and hand a real key out
    /// of the arc's own pool, rather than taking the empty string for an answer. That is strictly MORE than
    /// the one-picture-per-beat sweep they did before — the whole authored pool now has to be painted and
    /// spelled, so a twelfth KAAMOS plate means painting it or being told.</para>
    /// </summary>
    private static readonly StoryBeats.Beat[] KeyedBySubject =
        [.. All.Where(b => string.IsNullOrWhiteSpace(StoryBeats.ArtFile(b)))];

    /// <summary>A subject that reaches a real plate. The two arc keys come out of the arcs' own pools and are
    /// never typed in here — a shard id invented for a test is a world that cannot tell pass from fail.</summary>
    private static string ASubjectFor(StoryBeats.Beat beat) => beat switch
    {
        StoryBeats.Beat.KaamosShardFound => KaamosLore.AllPlates.First().Key,
        StoryBeats.Beat.NebulaShardFound => NebulaLore.AllPlates.First().Key,

        _ => "THE QUIET SISTER",
    };

    /// <summary>
    /// AND EXACTLY TWO BEATS ANSWER "NOTHING" WITHOUT A SUBJECT — the same whole-list discipline this file
    /// keeps for the cadences, and for the same reason: a third one appearing should require editing this
    /// test and saying why, because the alternative is a beat that quietly went captionless and was excused
    /// by an exemption its neighbours earned.
    ///
    /// <para>It is also the anti-vacuous half of the three sweeps below. If this list ever swallowed the
    /// whole enum, every one of them would be sweeping a pool nobody has to paint.</para>
    /// </summary>
    [Fact]
    public void OnlyTheTwoArcShardsAreKeyedByTheirSubject()
    {
        Assert.Equal(
            [
                StoryBeats.Beat.KaamosShardFound,
                StoryBeats.Beat.NebulaShardFound,
            ],
            KeyedBySubject);

        // …and each of them names a POOL rather than one picture, which is what makes it keyed rather than
        // merely unpainted. A pool of one would be a fixed beat wearing the exemption.
        foreach (StoryBeats.Beat beat in KeyedBySubject)
        {
            Assert.True(StoryBeats.Canvases(beat).Count() > 1,
                        $"{beat} resolves to no canvas without a subject and names only one with — that is an "
                        + "unpainted beat, not a keyed one.");
        }
    }

    /// <summary>Every beat has a picture, a title and a caption. A beat that reaches the player half-dressed is
    /// worse than one that never fires.</summary>
    [Fact]
    public void EveryBeatIsFullyWritten()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            string subject = ASubjectFor(beat);

            Assert.NotEmpty(StoryBeats.Canvases(beat));
            foreach (string art in StoryBeats.Canvases(beat))
            {
                Assert.False(string.IsNullOrWhiteSpace(art), $"{beat} has no art file");
            }

            Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Title(beat, subject)), $"{beat} has no title");
            Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Caption(beat, subject)), $"{beat} has no caption");
        }
    }

    /// <summary>The art path is a real repo path shape, so a typo shows up here and not as a missing picture in
    /// front of the owner.</summary>
    [Fact]
    public void EveryArtPathPointsIntoTheArtFolder()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            Assert.NotEmpty(StoryBeats.Canvases(beat));
            foreach (string art in StoryBeats.Canvases(beat))
            {
                Assert.StartsWith("art/", art, StringComparison.Ordinal);
                Assert.EndsWith(".jpg", art, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>Captions read whole with or without a subject — the caller may not have a name to give, and the
    /// prose must never come out with a hole in it.
    ///
    /// <para>#664 · The bare caption is now asserted WHOLE OR NOTHING rather than merely brace-free, which the
    /// old sweep never said at all: a fixed beat must read complete with no subject, and a subject-keyed one
    /// must come out empty rather than as half a sentence about a shard nobody found.</para></summary>
    [Fact]
    public void CaptionsReadWholeWithoutASubject()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            string bare = StoryBeats.Caption(beat);
            string named = StoryBeats.Caption(beat, ASubjectFor(beat));

            Assert.DoesNotContain("{", bare, StringComparison.Ordinal);
            Assert.DoesNotContain("  ", bare, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(named));

            if (KeyedBySubject.Contains(beat))
            {
                Assert.True(bare.Length == 0,
                            $"{beat}'s plate is chosen by its subject, so with none it must resolve to nothing "
                            + "at all — a partial line here is a card about a shard the captain never found.");
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(bare),
                             $"{beat} has no caption without a subject, and its caller may not have one to give.");
            }
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
    ///
    /// <para>#664 · And the fifth, which is the edit-and-say-why this list was written to demand.
    /// <c>CollectorsSetDown</c> is the ONLY WARNING THE PLAYER GETS — after it the only information in the
    /// world is a tracker fan — and it is rare by its own nature twice over: a heat threshold has to be
    /// crossed, and at most one boat lands per excursion. A warning rationed for being repetitive is not a
    /// warning, so it is the one adopted beat that keeps EveryTime.</para>
    ///
    /// <para>#973 · And the sixth, saying why in its turn. <c>Flashback</c> is rare by its own nature and
    /// cannot be made repetitive by trying: a grey page may be read at ONCE PER LIFE and never again
    /// (<see cref="FilingLine.PageState.Refused"/> is the latch), and a captain who has never died has no
    /// grey pages to read at all. <see cref="StoryBeats.Cadence.OncePerSubject"/> was the near miss and it
    /// is wrong for one reason: a rebirth RE-GREYS the book, so the same page read by a later captain is a
    /// different captain reaching for a different stranger's afternoon — and filing under (beat, page id)
    /// would silently un-illustrate every flashback after the first death.</para>
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
                StoryBeats.Beat.CollectorsSetDown,
                StoryBeats.Beat.Flashback,
            ],
            everyTime);
    }

    /// <summary>
    /// #541 · ONCE PER SUBJECT, and the same whole-list discipline: adding a beat here should require editing this
    /// test and saying why. The arrival tube is what this cadence was invented for, and the reason is worth writing
    /// down — the beat is about a PLACE rather than about the captain. <c>OnceEver</c> would have shown one berth's
    /// gangway and silently swallowed every other berth in the system; <c>EveryTime</c> would have made docking at
    /// a place you live at annoying. So: each berth's establishing shot, once, for that berth.
    ///
    /// <para>#973 L5b · AND THE NINTH IS ABOUT A PERSON, which widens the sentence above rather than breaking
    /// it. The clause that ever mattered was not "a place" — it was <i>not about the captain</i>: a beat whose
    /// subject is somebody else has to be able to happen again about somebody else. Two women who each walk
    /// into a bar are two moments; the same woman twice is not one, and she is not coming back to ask a second
    /// time. <c>OnceEver</c> would have shown the fling's entrance and silently swallowed the stranger's
    /// forever, which is precisely the failure #541 widened the seen-key to stop.</para>
    ///
    /// <para>#664 · Five more, and every one of them passes the same test the tube did: it is about a PLACE
    /// or a THING and not about the captain. A second KAAMOS or NEBULA shard is a different painting and
    /// different words; a second moon's buried door is a different moon; a second hut's last effects are
    /// somebody else's; and the cradles nearest the door are a different lab's. <c>OnceEver</c> on any of
    /// them would show one and silently swallow every other one in the game, which is precisely the failure
    /// #541 widened the seen-key to stop — and it is worth saying that the arcs' two are the strongest case
    /// of all, because for them the PICTURE is chosen by the subject too.</para>
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
                StoryBeats.Beat.KaamosShardFound,
                StoryBeats.Beat.NebulaShardFound,
                StoryBeats.Beat.OutpostEffectsRead,
                StoryBeats.Beat.SecretLabDoorFound,
                StoryBeats.Beat.TheDormantThingWakes,
                StoryBeats.Beat.WalkIn,
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

    // ── #777 · "…and sometimes the best surface is one that is already up" ────────────────────────────

    /// <summary>
    /// THE THIRD ANSWER. #776's audit found the one beat this seam could not serve: the collector's hail is
    /// shown by the BUSTED demand panel, which has rendered its painting since #528, so raising it as a CARD
    /// would put a second modal on top of the first with the identical picture on it — and it is the one beat
    /// that may not wait for a better moment, because it IS the moment.
    ///
    /// <para>Hosted is that answer, and the two facts that make it honest are here: the presentation says the
    /// caller's card is the canvas, and <see cref="StoryBeats.HostCard"/> says WHOSE. A hosted beat with no
    /// named host is a beat nobody can find, which is the orphan #663 was filed about wearing the fix's
    /// clothes.</para>
    /// </summary>
    [Fact]
    public void AHostedBeatNamesTheCardThatIsItsCanvas()
    {
        Assert.Equal(StoryBeats.Presentation.Hosted, StoryBeats.PresentationOf(StoryBeats.Beat.CollectorHail));

        foreach (StoryBeats.Beat beat in All)
        {
            bool hosted = StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Hosted;
            bool names = !string.IsNullOrWhiteSpace(StoryBeats.HostCard(beat));

            Assert.True(hosted == names,
                hosted
                    ? $"{beat} is presented HOSTED and names no host — nothing would ever show it"
                    : $"{beat} names a host card and is not presented HOSTED — the seam will raise its own "
                      + "surface as well, which is the stacked card this presentation exists to prevent");
        }
    }

    /// <summary>
    /// A HOSTED BEAT NEVER WAITS, and the reason is different from the danger rule: there is nothing to hold.
    /// Its surface belongs to a card the caller is raising right now, so "later" would mean showing the words
    /// after the picture they belong to has gone. Stated over the whole enum so a second hosted beat inherits
    /// the law instead of rediscovering it.
    /// </summary>
    [Fact]
    public void AHostedBeatNeverDefersBecauseThereIsNothingToHold()
    {
        foreach (StoryBeats.Beat beat in All)
        {
            if (StoryBeats.PresentationOf(beat) == StoryBeats.Presentation.Hosted)
            {
                Assert.False(StoryBeats.DeferrableWhileInDanger(beat),
                             $"{beat} is hosted by a card that is on screen NOW; there is nothing to defer");
            }
        }
    }

    /// <summary>
    /// …and it is still fully written. Hosting moves the CANVAS to the caller, never the words: the host is
    /// obliged to render this caption (the client guards that), and the seam writes it into the log either
    /// way. A hosted beat allowed to go captionless would be a picture with no sentence anywhere — which is
    /// precisely the state the hail was in before #777.
    /// </summary>
    [Fact]
    public void HostingMovesTheCanvasAndNeverTheWords()
    {
        string caption = StoryBeats.Caption(StoryBeats.Beat.CollectorHail, "THE QUIET SISTER");

        Assert.Contains("Grapples", caption, StringComparison.Ordinal);
        Assert.Contains("THE QUIET SISTER", caption, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(StoryBeats.Title(StoryBeats.Beat.CollectorHail)));
    }
}

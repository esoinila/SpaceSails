using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 L5b · THE WALK-IN, AS LAWS.
///
/// <para>Six things in this lane are rules rather than habits: who crosses the floor (the world decides, not a
/// die), how often a setup happens (one in three, and it must really be one in three over many universes),
/// what the job IS in #972's vocabulary (FIND, a dash, <i>for her</i>, tagged love), when the unfinished
/// sentence is allowed to end, what the card may say about a setup before anybody has found it out, and the
/// arc's own law that no surface ever says <i>copy</i> or names what was in the pods.</para>
///
/// <para>Every one of them is the sort that rots quietly. A cast that stopped consulting the world would
/// silently ship the weaker scene forever; a setup rate that drifted to 1-in-4 would still look right in a
/// playthrough; a card that started warning for free would delete the choice the owner asked for and nobody
/// would ever notice it had been deleted.</para>
/// </summary>
public sealed class TheWalkInAsksForSomethingFoundTests
{
    private static readonly WalkIn.Who[] BothOfThem = [WalkIn.Who.Ilse, WalkIn.Who.Nadia];

    private static IEnumerable<string> ManyThreads(int count) =>
        Enumerable.Range(0, count).Select(i => $"walkin-thread-{i}");

    // ── WHO CROSSES THE FLOOR ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FLING ONLY WHEN SHE IS ACTUALLY POSTED HERE — and the stranger every other time.
    ///
    /// <para>The strongest version of this scene is the one where the woman crossing the room already knew
    /// the face, and whether she is at this port is a fact the thread decided when it posted the old crew
    /// (L5a). A cast that rolled on top of that could only ever take the better story away, so this must stay
    /// a function and not a die — proven by asking it both ways round and getting two different women.</para>
    /// </summary>
    [Fact]
    public void SheIsTheFlingWhenTheFlingIsPostedHereAndAStrangerOtherwise()
    {
        Assert.Equal(WalkIn.Who.Ilse, WalkIn.Cast(flingIsPostedHere: true));
        Assert.Equal(WalkIn.Who.Nadia, WalkIn.Cast(flingIsPostedHere: false));

        // …and the two of them are two people, all the way down: different names, plates, portraits, subjects,
        // books and every line in the scene. A "variant" that shared any of those would be one woman.
        Assert.NotEqual(WalkIn.Name(WalkIn.Who.Ilse), WalkIn.Name(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.Plate(WalkIn.Who.Ilse), WalkIn.Plate(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.Subject(WalkIn.Who.Ilse), WalkIn.Subject(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.ContactId(WalkIn.Who.Ilse), WalkIn.ContactId(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.PortraitArt(WalkIn.Who.Ilse), WalkIn.PortraitArt(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.AtTheTable(WalkIn.Who.Ilse), WalkIn.AtTheTable(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.TheStory(WalkIn.Who.Ilse), WalkIn.TheStory(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.IfNo(WalkIn.Who.Ilse), WalkIn.IfNo(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.NoteText(WalkIn.Who.Ilse), WalkIn.NoteText(WalkIn.Who.Nadia));
        Assert.NotEqual(WalkIn.Finished(WalkIn.Who.Ilse), WalkIn.Finished(WalkIn.Who.Nadia));

        // The fling's book is the old crew's, because she is an old shipmate first and a walk-in second.
        Assert.Equal(OldCrew.LedgerId(OldCrew.FlingId), WalkIn.ContactId(WalkIn.Who.Ilse));
        Assert.Equal(OldCrew.FlingId, WalkIn.Subject(WalkIn.Who.Ilse));
    }

    /// <summary>THE SAME UNIVERSE TELLS THE SAME STORY TWICE. Every seeded answer in this file is asked twice
    /// off one thread and must not move — the clause that catches a roll made off a clock, a cached
    /// dictionary with memory in it, or a hash that folds in something that is not the seed.</summary>
    [Fact]
    public void EverySeededAnswerIsTheSameAnswerTwice()
    {
        foreach (string thread in ManyThreads(40))
        {
            foreach (WalkIn.Who who in BothOfThem)
            {
                Assert.Equal(WalkIn.IsASetup(thread, who), WalkIn.IsASetup(thread, who));
            }

            for (int visit = 0; visit < 20; visit++)
            {
                Assert.Equal(
                    WalkIn.CouldWalkInThisVisit(thread, "red-eye", visit),
                    WalkIn.CouldWalkInThisVisit(thread, "red-eye", visit));
            }
        }
    }

    // ── THE RARITY ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SHE IS RARE, AND THE RARITY IS A ROTA RATHER THAN A MOOD — one visit in
    /// <see cref="WalkIn.VisitsBetweenWalkIns"/>, at a per-thread-and-berth offset so two universes do not
    /// put her on the same evening and a player cannot set a watch by her.
    ///
    /// <para>Asserted as a COUNT over a long run of visits rather than as a pattern, because the offset is
    /// the point: the shape is "exactly one in seven", wherever in the seven this thread's berth starts.</para>
    /// </summary>
    [Fact]
    public void OneVisitInSevenCouldHaveAWalkInInIt()
    {
        foreach (string thread in ManyThreads(25))
        {
            foreach (string berth in new[] { "red-eye", "ringside-exchange", "selene-gate" })
            {
                int window = WalkIn.VisitsBetweenWalkIns * 9;
                int eligible = Enumerable.Range(0, window)
                    .Count(v => WalkIn.CouldWalkInThisVisit(thread, berth, v));

                Assert.Equal(window / WalkIn.VisitsBetweenWalkIns, eligible);
            }
        }
    }

    /// <summary>A visit index that does not exist is not an evening. The clause that catches a page asking
    /// before it has ever docked anywhere.</summary>
    [Fact]
    public void NobodyWalksInBeforeTheFirstVisit()
    {
        Assert.False(WalkIn.CouldWalkInThisVisit("thread", "red-eye", -1));
        Assert.False(WalkIn.CouldWalkInThisVisit("thread", "red-eye", -8));
    }

    // ── FEMME FATALE BY RULE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONE IN THREE IS A SETUP — the owner's own fraction (ruling 17), measured over many universes rather
    /// than asserted about one.
    ///
    /// <para>A rate is exactly the kind of law that rots without ever failing a playthrough: a die that
    /// drifted to one-in-four, or a hash that folded the woman in so badly that Ilse was ALWAYS the setup,
    /// would read as "unlucky" for months. The band is deliberately tight (a real 1/3 over 900 samples sits
    /// well inside it) and the per-woman split is checked too, so a rate that is right in total and wrong
    /// about who cannot pass.</para>
    /// </summary>
    [Fact]
    public void OneWalkInInThreeIsASetup()
    {
        var thread = new List<string>(ManyThreads(450));
        int setups = 0;
        int ilse = 0;
        int nadia = 0;

        foreach (string t in thread)
        {
            if (WalkIn.IsASetup(t, WalkIn.Who.Ilse))
            {
                setups++;
                ilse++;
            }

            if (WalkIn.IsASetup(t, WalkIn.Who.Nadia))
            {
                setups++;
                nadia++;
            }
        }

        double rate = setups / (double)(thread.Count * 2);
        Assert.InRange(rate, 0.28, 0.39);
        Assert.InRange(ilse / (double)thread.Count, 0.25, 0.42);
        Assert.InRange(nadia / (double)thread.Count, 0.25, 0.42);

        // …and it is not the same answer for both of them in every universe, which is what a seed that
        // forgot to fold the woman in would look like.
        Assert.Contains(thread, t => WalkIn.IsASetup(t, WalkIn.Who.Ilse) != WalkIn.IsASetup(t, WalkIn.Who.Nadia));
    }

    /// <summary>
    /// THE CARD SAYS NOTHING UNTIL THE SPREAD HAS FOUND IT OUT — and then it says the owner's line.
    ///
    /// <para>This is the honesty of the whole beat. A grey warning that appeared for free would tell the
    /// player about a scene the design exists to let them walk into knowingly; a warning that never appeared
    /// would make L3's reconcile worth nothing. Both halves are asserted, and so is the third state that must
    /// stay silent forever: a walk-in that simply is not a setup.</para>
    /// </summary>
    [Fact]
    public void TheSetupLineIsSilentUntilItIsRevealedAndNeverLiesAboutAnHonestOne()
    {
        Assert.Null(WalkIn.SetupCardLine(isASetup: true, revealed: false));
        Assert.Null(WalkIn.SetupCardLine(isASetup: false, revealed: false));
        Assert.Null(WalkIn.SetupCardLine(isASetup: false, revealed: true));
        Assert.Equal("a setup — you can still go", WalkIn.SetupCardLine(isASetup: true, revealed: true));
    }

    // ── THE JOB ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE JOB IS <b>FIND</b>, THE PAYOUT LINE IS A DASH, AND THE SIZE WORD IS <i>for her</i>.
    ///
    /// <para>#972's plain block is the ONE vocabulary a player learns jobs in, so a favour that spoke its own
    /// dialect would be a second grammar for the sake of one scene. What it may do is refuse to price itself
    /// — which is the fifth size word, and the only one in the vocabulary that is not about money.</para>
    /// </summary>
    [Fact]
    public void HerJobIsAFindThatPaysInADash()
    {
        Assert.Equal(JobVerb.Find, JobTerms.Verb(ContractKind.WalkIn));

        var facts = new JobFacts(
            ContractKind.WalkIn, OldCrew.TheDecentShip, JobTargetNature.Haven,
            WhereName: "SATURN system", DistanceMeters: 3.6e9, LaneSeconds: 6 * 86400.0,
            Reward: 0, PurseCredits: 8000, ForHer: true);

        IReadOnlyList<string> block = JobTerms.PlainBlock(facts);
        Assert.Equal(4, block.Count);
        Assert.StartsWith("FIND — ", block[0], StringComparison.Ordinal);
        Assert.Equal($"{JobTerms.NoPayout} · {JobTerms.ForHerWord}", block[3]);
        Assert.Equal("—", JobTerms.NoPayout);
        Assert.Equal("for her", JobTerms.ForHerWord);

        // The effort line is MEASURED, exactly like every other job's — a favour is not estimated.
        Assert.Contains("by the lanes", block[2], StringComparison.Ordinal);

        // …and the size word joins the vocabulary rather than replacing it: every money answer is unmoved.
        Assert.Equal("for her", JobTerms.SizeWord(0, 8000, forHer: true));
        Assert.Equal("for her", JobTerms.SizeWord(50_000, 8000, forHer: true));
        Assert.Equal("nothing", JobTerms.SizeWord(0, 8000));
        Assert.Equal("small", JobTerms.SizeWord(764, 8000));
        Assert.Equal("fair", JobTerms.SizeWord(3000, 8000));
        Assert.Equal("good", JobTerms.SizeWord(9000, 8000));
        Assert.Equal("fortune", JobTerms.SizeWord(30_000, 8000));
    }

    /// <summary>A job that is not hers is priced the way it always was — the clause that catches a
    /// <c>ForHer</c> flag defaulted the wrong way round, which would silently make every job in the game pay
    /// in a dash.</summary>
    [Fact]
    public void EveryOtherJobStillSaysWhatItPays()
    {
        var run = new JobFacts(
            ContractKind.CargoRun, "The Rusty Roadstead", JobTargetNature.Haven,
            Reward: 764, PurseCredits: 8000);

        Assert.Equal("764 cr · small", JobTerms.PayLine(run));
        Assert.False(run.ForHer);
    }

    /// <summary>Her row is tagged LOVE by construction — the first job in the game that is, and the reason
    /// the owner asked for the second axis at all.</summary>
    [Fact]
    public void HerRowIsTaggedLove()
    {
        Assert.Equal(HeldMemory.Theory.Love, WalkIn.Theory);
        Assert.Equal("love", HeldMemory.Label(WalkIn.Theory));
    }

    // ── THE UNFINISHED LINE ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SENTENCE DOES NOT END UNTIL THE JOB OR THE NOTE ENDS IT (ruling 18) — and it really is either, not
    /// only the one that was easy to build.
    /// </summary>
    [Fact]
    public void TheSinceLineEndsOnTheJobOrOnTheNoteAndNotBefore()
    {
        Assert.False(WalkIn.SinceFinishes(jobCompleted: false, noteReconciled: false));
        Assert.True(WalkIn.SinceFinishes(jobCompleted: true, noteReconciled: false));
        Assert.True(WalkIn.SinceFinishes(jobCompleted: false, noteReconciled: true));
        Assert.True(WalkIn.SinceFinishes(jobCompleted: true, noteReconciled: true));

        // …and it is unfinished on purpose: the em-dash is the whole of the owner's line.
        Assert.EndsWith("—", WalkIn.Unfinished.TrimEnd(), StringComparison.Ordinal);
        foreach (WalkIn.Who who in BothOfThem)
        {
            Assert.StartsWith("—", WalkIn.Finished(who).TrimStart(), StringComparison.Ordinal);
        }
    }

    /// <summary>WHAT L3 LAYS HER NOTE BESIDE, named once so the two lanes cannot come to two views of one
    /// pair: the fleet-day page for the fling (the one page the filing line cannot grey), this job's own
    /// first slip for the stranger.</summary>
    [Fact]
    public void HerNoteReconcilesAgainstThePageItIsAbout()
    {
        Assert.Equal(OldCrewScene.SummerPartyId, WalkIn.ReconcilesAgainst(WalkIn.Who.Ilse, "walkin-3"));
        Assert.Equal(WalkIn.FirstSlipId("walkin-3"), WalkIn.ReconcilesAgainst(WalkIn.Who.Nadia, "walkin-3"));

        // Two jobs are two slips: a shared id would have the second walk-in overwrite the first's evidence.
        Assert.NotEqual(WalkIn.FirstSlipId("walkin-3"), WalkIn.FirstSlipId("walkin-4"));

        // …and two women are two notes.
        Assert.NotEqual(WalkIn.NoteId(WalkIn.Who.Ilse), WalkIn.NoteId(WalkIn.Who.Nadia));
    }

    /// <summary>Only the <c>since</c> subject moves the flashback plate's caption. Every other memory in the
    /// game keeps the signing's sentence, which is the clause that catches a fork keyed on the wrong
    /// thing.</summary>
    [Fact]
    public void OnlyTheSinceSubjectChangesTheFlashbacksCaption()
    {
        Assert.Equal(WalkIn.Unfinished, StoryBeats.Caption(StoryBeats.Beat.Flashback, WalkIn.SinceSubject));
        Assert.NotEqual(WalkIn.Unfinished, StoryBeats.Caption(StoryBeats.Beat.Flashback, "some-ledger-entry"));
        Assert.NotEqual(WalkIn.Unfinished, StoryBeats.Caption(StoryBeats.Beat.Flashback, null));
        Assert.Null(WalkIn.FlashbackCaption("some-ledger-entry"));
        Assert.Null(WalkIn.FlashbackCaption(null));
    }

    // ── THE BEAT ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ONCE PER SUBJECT, AND HOSTED ON HER OWN CARD.
    ///
    /// <para>The subject is the WOMAN, which is what makes "two women is two moments, the same woman twice is
    /// not" true rather than merely intended. And the presentation is hosted, so the seam raises nothing —
    /// a plate alongside her portrait would be #777's stacked card, the same face twice on one screen.</para>
    /// </summary>
    [Fact]
    public void HerBeatSpeaksOncePerWomanAndPutsNothingOnTheScreenItself()
    {
        Assert.Equal(StoryBeats.Cadence.OncePerSubject, StoryBeats.CadenceOf(StoryBeats.Beat.WalkIn));
        Assert.Equal(StoryBeats.Presentation.Hosted, StoryBeats.PresentationOf(StoryBeats.Beat.WalkIn));
        Assert.NotEqual("", StoryBeats.HostCard(StoryBeats.Beat.WalkIn));

        // Her portrait is the canvas, and it is HER portrait — chosen by the subject the beat was raised with.
        Assert.Equal(
            WalkIn.PortraitArt(WalkIn.Who.Ilse),
            StoryBeats.ArtFile(StoryBeats.Beat.WalkIn, WalkIn.Subject(WalkIn.Who.Ilse)));
        Assert.Equal(
            WalkIn.PortraitArt(WalkIn.Who.Nadia),
            StoryBeats.ArtFile(StoryBeats.Beat.WalkIn, WalkIn.Subject(WalkIn.Who.Nadia)));

        // Both canvases are enumerable, so the one nobody is looking at is still a file somebody has to paint.
        Assert.Equal(2, StoryBeats.Canvases(StoryBeats.Beat.WalkIn).Distinct(StringComparer.Ordinal).Count());

        // The caption is the room's own line, whole, and it does not name her — the seam does not introduce
        // somebody the player is about to be introduced to.
        Assert.Equal(WalkIn.TheRoomLooks, StoryBeats.Caption(StoryBeats.Beat.WalkIn, WalkIn.Subject(WalkIn.Who.Ilse)));
        foreach (WalkIn.Who who in BothOfThem)
        {
            Assert.DoesNotContain(
                WalkIn.Name(who),
                StoryBeats.Caption(StoryBeats.Beat.WalkIn, WalkIn.Subject(who)),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── THE ARC'S OWN LAW ────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// NO SURFACE OF HERS SAYS <i>copy</i>, AND NONE OF THEM NAMES WHAT WAS IN THE PODS.
    ///
    /// <para>The same sweep L5a and L3 keep, over this lane's own surfaces — she talks about a renamed hull
    /// and a brother who stopped writing, and the arc's deepest fact is nowhere near either sentence. Every
    /// line is also asserted non-empty, because a law about absence passes gloriously over nothing.</para>
    /// </summary>
    [Fact]
    public void NothingSheSaysNamesTheThing()
    {
        string[] everySurface = [.. WalkIn.EveryLine()];

        Assert.NotEmpty(everySurface);
        Assert.All(everySurface, line => Assert.False(string.IsNullOrWhiteSpace(line)));

        Assert.All(everySurface, line =>
            Assert.DoesNotContain("copy", line, StringComparison.OrdinalIgnoreCase));

        foreach (string forbidden in new[] { "restore", "clone", "backup", "cadaver", "corpse", "body double" })
        {
            Assert.All(everySurface, line =>
                Assert.DoesNotContain(forbidden, line, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>The lines the issue authored, verbatim, pinned where a rewording has to be a deliberate act.
    /// Fable is the author of every in-fiction sentence in this game; a crew that "improved" one of these
    /// would be rewriting the owner's own scene.</summary>
    [Fact]
    public void HerWordsAreTheOnesThatWereWritten()
    {
        Assert.Equal("The room looks at the door before you do.", WalkIn.TheRoomLooks);
        Assert.Equal("Yes — I'll find it", WalkIn.Yes);
        Assert.Equal("No", WalkIn.No);
        Assert.Equal("I haven't felt like this since —", WalkIn.Unfinished);
        Assert.Equal("Her hand and the desk's hand are the same hand.", WalkIn.SameHandLine);
        Assert.Equal("We still have the fleet-day.", WalkIn.NoteText(WalkIn.Who.Ilse));
        Assert.Equal("You said yes before I finished. Thank you for that. — N.", WalkIn.NoteText(WalkIn.Who.Nadia));
        Assert.Equal("Then I was wrong about the walk.", WalkIn.IfNo(WalkIn.Who.Ilse));
        Assert.Equal("No. Of course. — Sorry to have sat.", WalkIn.IfNo(WalkIn.Who.Nadia));
        Assert.Equal("— since the rail went cold.", WalkIn.Finished(WalkIn.Who.Ilse));
        Assert.Equal(
            "— since somebody last asked me for something and meant it.", WalkIn.Finished(WalkIn.Who.Nadia));
        Assert.Equal(
            "Don't get up. I won't stay. — I need something found, and I can't be the one who asks for it.",
            WalkIn.AtTheTable(WalkIn.Who.Ilse));
        Assert.Equal(
            "You're the one they said would be alone. — I have a small thing and nobody to take it to.",
            WalkIn.AtTheTable(WalkIn.Who.Nadia));
        Assert.Equal(
            "A berth listing under a name that isn't hers any more. The REACH — they renamed her. I want to "
            + "know where she's tied up and who holds the paper. Not for the company. For me.",
            WalkIn.TheStory(WalkIn.Who.Ilse));
        Assert.Equal(
            "My brother filed a claim and then stopped writing. The desk says the file is in order. I want "
            + "somebody to find the man, not the file.",
            WalkIn.TheStory(WalkIn.Who.Nadia));

        // …and the blurb L3 left a marker on, answered.
        Assert.Equal(
            "Two sheets on the table at a time. See whether they were written about the same world.",
            SeatedSpread.LayThemTogetherBlurb);
    }
}

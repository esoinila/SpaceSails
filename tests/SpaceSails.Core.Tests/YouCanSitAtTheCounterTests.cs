using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #756 · THE HIGH CHAIRS — the Core half.
///
/// <para>Owner, live playtest 2026-08-08: <i>"Also there should be high chairs so sitting at the bar desk is
/// also possible."</i> And, filing the design: <i>"one E, pick-or-default a free stool… the neighbor may open
/// the approach ladder from the NEXT STOOL without asking to sit — they are already seated, proximity IS the
/// invitation."</i></para>
///
/// <para>Everything below is measured against real rolls on the watches the game actually has, never against
/// the arithmetic in a comment — this repo's fifth named bug class is a threshold that selects everything or
/// nothing while its assertions read perfectly.</para>
/// </summary>
public sealed class YouCanSitAtTheCounterTests
{
    private const string Body = "europa";
    private const int Level = -1;

    /// <summary>Every watch the hall has. The fill list is the rota's own, so this is the whole day.</summary>
    private static IEnumerable<long> AllWatches() =>
        Enumerable.Range(0, CanteenRegulars.WatchFill.Count).Select(i => (long)i);

    // ── THE ROW ───────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThereIsAlwaysASeatToTake_AndTheRowIsNeverAllOneThing()
    {
        // PICK-OR-DEFAULT has to actually pick something. The owner's grammar is one press, so a watch on
        // which the answer is null would be a button that refuses on a room nobody can see is full.
        var taken = new List<int>();
        foreach (long watch in AllWatches())
        {
            int? free = TheStools.FirstFreeStool(Body, Level, watch);
            Assert.NotNull(free);
            Assert.False(TheStools.Taken(Body, Level, free!.Value, watch),
                $"watch {watch}: the 'free' stool has somebody on it.");

            int n = Enumerable.Range(0, TheStools.Count).Count(s => TheStools.Taken(Body, Level, s, watch));
            Assert.Equal(TheStools.TakenCount(watch), n);
            taken.Add(n);
        }

        // ANTI-VACUITY, both ways. A row that was always empty would pass every occupancy assertion in this
        // file and make HasNeighbour dead code; a row that was always full would make FirstFreeStool a
        // constant. The day has both a busy counter and a bare one.
        Assert.True(taken.Max() >= 4, $"the busiest watch seats only {taken.Max()} of {TheStools.Count} stools.");
        Assert.True(taken.Min() <= 1, $"the quietest watch still seats {taken.Min()} — no watch is quiet.");
    }

    [Fact]
    public void THE_ROW_IsTheSameRowEveryTimeYouLookAtIt()
    {
        // #709's third-bug-class law at a counter: the drawn room and the pressed room are one room. The
        // occupancy is seeded on (site, floor, stool, watch) and nothing else, so asking twice is asking
        // once — and a DIFFERENT floor or a different site is a different bar.
        foreach (long watch in AllWatches())
        {
            for (int s = 0; s < TheStools.Count; s++)
            {
                Assert.Equal(
                    TheStools.Taken(Body, Level, s, watch),
                    TheStools.Taken(Body, Level, s, watch));
            }
        }

        bool anyDifference = false;
        foreach (long watch in AllWatches())
        {
            for (int s = 0; s < TheStools.Count; s++)
            {
                anyDifference |= TheStools.Taken(Body, Level, s, watch)
                    != TheStools.Taken("titan", Level, s, watch);
            }
        }
        Assert.True(anyDifference, "two different sites seat the identical row — the seed ignores the site.");
    }

    // ── PROXIMITY IS THE INVITATION ───────────────────────────────────────────────────────────────────

    [Fact]
    public void NOBODY_TURNS_WhenTheSeatsBesideYouAreEmpty_HoweverBusyTheWatchIs()
    {
        // THE LAW THIS FIXTURE HAS AND THE TABLES DO NOT, and the direction of it that is easiest to lose:
        // at a table anybody in the room might walk over, so a busy watch is enough. At a counter the only
        // person who can turn to you is the person within arm's reach. An empty seat either side is a
        // silence the dice cannot break.
        //
        // RED PROOF: delete the HasNeighbour gate at the top of SomebodyTurns and this fails on the first
        // busy watch it reaches.
        int provedOn = 0;
        foreach (long watch in AllWatches())
        {
            for (int s = 0; s < TheStools.Count; s++)
            {
                if (TheStools.HasNeighbour(Body, Level, s, watch))
                {
                    continue;
                }
                for (int beat = 0; beat < 40; beat++)
                {
                    Assert.False(TheStools.SomebodyTurns(Body, Level, s, watch, beat),
                        $"watch {watch}, stool {s}, beat {beat}: somebody turned to you from an empty seat.");
                }
                provedOn++;
            }
        }

        // Anti-vacuity: there ARE lonely stools to prove it on. Without this the loop above could be
        // measuring a row where every seat always has a neighbour.
        Assert.True(provedOn >= 4,
            $"only {provedOn} (watch, stool) pairs had nobody beside them — this guard swept nothing.");
    }

    [Fact]
    public void SHE_TURNS_OnTheRightWatchAndStaysQuietOnTheWrong()
    {
        // BOTH DIRECTIONS, PROVABLE, on the watches the game has — and rolled rather than reasoned about.
        // The odds are SittingAlone's own (one law for "does this room have anything for you tonight"), so
        // the heaving watch has to speak and the small one has to be near-silent.
        var spoke = new Dictionary<long, int>();
        foreach (long watch in AllWatches())
        {
            int hits = 0, tries = 0;
            for (int s = 0; s < TheStools.Count; s++)
            {
                if (!TheStools.HasNeighbour(Body, Level, s, watch))
                {
                    continue;
                }
                for (int beat = 0; beat < 60; beat++)
                {
                    tries++;
                    if (TheStools.SomebodyTurns(Body, Level, s, watch, beat))
                    {
                        hits++;
                    }
                }
            }
            spoke[watch] = tries == 0 ? -1 : (int)Math.Round(100.0 * hits / tries);
        }

        long busiest = AllWatches().OrderByDescending(SittingAlone.Fill).First();
        long emptiest = AllWatches().OrderBy(SittingAlone.Fill).First();

        Assert.True(spoke[busiest] > 20,
            $"on the heaving watch the neighbour turned {spoke[busiest]}% of beats — that threshold selects "
            + "almost nothing, and a guard over it would assert nothing.");
        Assert.True(spoke[busiest] < 80,
            $"on the heaving watch the neighbour turned {spoke[busiest]}% of beats — that threshold selects "
            + "almost everything, which is the other half of the same dead guard.");

        // And the emptiest watch really is emptier. It may have no neighbours at all (-1), which is itself
        // the answer; where it has some, they speak less often than the heaving watch's do.
        Assert.True(spoke[emptiest] < spoke[busiest],
            $"the quietest watch ({spoke[emptiest]}%) is no quieter than the busiest ({spoke[busiest]}%).");
    }

    [Fact]
    public void THE_ODDS_AreTheHallsOwnAndNotASecondOpinion()
    {
        // ONE LAW, ASKED ONCE. If TheStools ever grows its own fill curve, the counter and the tops start
        // disagreeing about how busy the same room is on the same shift — this project's third named bug
        // class, wearing a bar apron.
        foreach (long watch in AllWatches())
        {
            Assert.Equal(
                CanteenRegulars.WatchFill[(int)(watch % CanteenRegulars.WatchFill.Count)],
                SittingAlone.Fill(watch));
            Assert.InRange(TheStools.TakenCount(watch), 0, TheStools.Count - 1);
        }
    }

    // ── THE LADDER ────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SHE_NEVER_ASKS_TO_SIT_BecauseSheIsAlreadySitting()
    {
        // The owner's rule, as data. #757's table ladder spends its FIRST rung on a chair (WaveIn/WaveOff);
        // this one opens at a remark, because the chair was never in question. So the first two moves are
        // both FREE and neither of them is about sitting down, and the ladder is one rung shorter in
        // ceremony while being the same three beats deep.
        Encounter.Scene her = TheStools.TheNeighbour();

        IReadOnlyList<Encounter.Move> opening = Encounter.OnTheTable(her, []);
        Assert.Contains(opening, m => m.Id == TheStools.AnswerHer);
        Assert.Contains(opening, m => m.Id == TheStools.LetItLie);
        Assert.DoesNotContain(opening, m => m.Id == SittingAlone.WaveIn);

        foreach (string word in new[] { "sit down", "pull the chair", "take a seat", "is anyone" })
        {
            Assert.DoesNotContain(word, TheStools.NeighbourOpening, StringComparison.OrdinalIgnoreCase);
        }

        // #749 · The rungs above the first do not EXIST until the sentence they answer has been spoken —
        // a reply to nothing is not a disabled control, it is not a control.
        Assert.DoesNotContain(opening, m => m.Id == TheStools.StandHerOne);
        Assert.DoesNotContain(opening, m => m.Id == TheStools.AskHerWeek);

        IReadOnlyList<Encounter.Move> afterAnswer = Encounter.OnTheTable(her, [TheStools.AnswerHer]);
        Assert.Contains(afterAnswer, m => m.Id == TheStools.StandHerOne);
        Assert.Contains(afterAnswer, m => m.Id == TheStools.KeepYourCoin);
        Assert.DoesNotContain(afterAnswer, m => m.Id == TheStools.AskHerWeek);

        // …and the ask needs the drink ANSWERED, either way: turning a round down is still having heard
        // yourself decline it.
        foreach (string reply in new[] { TheStools.StandHerOne, TheStools.KeepYourCoin })
        {
            Assert.Contains(
                Encounter.OnTheTable(her, [TheStools.AnswerHer, reply]),
                m => m.Id == TheStools.AskHerWeek);
        }
    }

    [Fact]
    public void EVERY_SCENE_HERE_LetsYouGetDownForFree_AndOneRungGoesInTheBook()
    {
        foreach (Encounter.Scene scene in new[] { TheStools.TheStool(0), TheStools.TheNeighbour() })
        {
            Assert.True(Encounter.CanAlwaysLeave(scene), $"{scene.Id} grew a price on getting up.");
        }

        // Exactly one rung is worth writing down, and it is the one she came for. A notebook full of
        // manners is a notebook nobody reads.
        var noted = TheStools.TheNeighbour().Moves.Where(m => m.Note is { Length: > 0 }).ToList();
        Assert.Single(noted);
        Assert.Equal(TheStools.AskHerWeek, noted[0].Id);
        Assert.Equal(TheStools.HerWeekNote, noted[0].Note);

        // …and the round she is stood costs what the card under the glass charges for a pour. One number,
        // asked of the counter, so the button, the debit and the menu cannot come to three answers.
        Encounter.Move round = TheStools.TheNeighbour().Moves.Single(m => m.Id == TheStools.StandHerOne);
        Assert.Equal(CounterService.HouseRate, round.Credits);
        Assert.Contains(round.Credits.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TheStools.LabelOf(TheStools.StandHerOne));

        // The stool you took has TWO moves and no third: ordering is not a move here because the menu is
        // already on this card, which is what "the keep serves you seated" had to mean.
        Assert.Equal(2, TheStools.TheStool(0).Moves.Count);
        Assert.Contains(TheStools.TheStool(0).Moves, m => m.Id == TheStools.Wait);
    }

    // ── THE SILENCES ──────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NOTHING_HAPPENING_IsToldInWords_AndTheWrongSilenceIsAWrongAnswer()
    {
        // #757's law at a counter, and BOTH DIRECTIONS PROVABLE — which is the whole of the ask. The pool a
        // silence comes from is decided by the ROOM (is anybody actually beside you, and how full is the
        // hall), so a wait that fell into the wrong pool is a wrong answer rather than a missing one.
        //
        // RED PROOF: swap the `neighbour` arm of NobodySpoke for the empty-seat pool and this fails naming
        // the line it got.
        long busiest = AllWatches().OrderByDescending(SittingAlone.Fill).First();
        long emptiest = AllWatches().OrderBy(SittingAlone.Fill).First();

        for (int beat = 0; beat < 12; beat++)
        {
            Assert.Contains(TheStools.NobodySpoke(busiest, beat, neighbour: true),
                TheStools.NeighbourSaysNothing);
            Assert.Contains(TheStools.NobodySpoke(busiest, beat, neighbour: false),
                TheStools.NobodyBesideYou);
            Assert.Contains(TheStools.NobodySpoke(emptiest, beat, neighbour: false),
                TheStools.CounterIsQuiet);
        }

        // The three pools share no line, or the paragraph above would be measuring nothing.
        var all = TheStools.NeighbourSaysNothing
            .Concat(TheStools.NobodyBesideYou).Concat(TheStools.CounterIsQuiet).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());

        // …and four waits in a row are four different sentences. A room that looped would read as a control
        // that stopped responding, which is the bug the told outcome exists to prevent (#603).
        var said = Enumerable.Range(0, 4)
            .Select(b => TheStools.NobodySpoke(busiest, b, neighbour: true)).Distinct().ToList();
        Assert.True(said.Count >= 3, $"four waits produced only {said.Count} different sentences.");

        // Negative index safety: a beat counter is a room's memory and it is never asked to be positive.
        Assert.False(string.IsNullOrWhiteSpace(TheStools.NobodySpoke(0, -3, neighbour: true)));
    }

    // ── THE VIEW, AND THE CANON ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void SITTING_DOWN_ChangesWhatYouAreLookingAt()
    {
        // #759 · Owner: "I do not see the park through the bar windows?" Standing you are looking at the
        // desk; up on a stool you are looking through the window wall at the green. Two pictures, and they
        // are not the same picture — a posture that changed the plate and not the view would be the game
        // saying you sat down and showing you that you had not.
        Assert.NotEqual(UndergroundComplex.CounterArtUrl, TheStools.SeatedArtUrl);
        Assert.EndsWith(".jpg", TheStools.SeatedArtUrl, StringComparison.Ordinal);
        Assert.StartsWith("art/", TheStools.SeatedArtUrl, StringComparison.Ordinal);

        // The caption says what it IS and not one word about why. The park is worth more unexplained, and
        // a caption that explained the window would spend the picture's whole effect.
        foreach (string word in new[] { "because", "why", "in order", "so that", "built to" })
        {
            Assert.DoesNotContain(word, TheStools.SeatedArtCaption, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>§13.8 at the counter. Nothing anybody says here explains what the facility is for — the same
    /// grep the rest of this room's prose walks, over THIS file's own list, so a line added tomorrow is
    /// checked tomorrow.</summary>
    [Fact]
    public void NOTHING_SAID_AT_THE_COUNTER_ExplainsAnything()
    {
        string[] forbidden =
        [
            "old one", "reever", "restore", "backup", "kaamos", "minister",
            "alien", "ancient", "experiment", "specimen", "archive",
        ];

        int lines = 0;
        foreach (string line in TheStools.AllProse())
        {
            Assert.False(string.IsNullOrWhiteSpace(line), "an empty string is in the prose list.");
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
            lines++;
        }

        Assert.True(lines > 15, $"only {lines} lines walked — this grep is not reading the counter.");
    }

    [Fact]
    public void SHE_POINTS_AT_SOMETHING_THE_GAME_CAN_ALREADY_SHOW_YOU()
    {
        // #701's register test and #761's told-clearly law at once: she is a person with an ordinary week,
        // what she says is legible, and what it points at exists TODAY — the hand upstairs who files
        // everything. A lead that promised a scene this game cannot play would be the worst kind of prose.
        foreach (string said in new[] { TheStools.HerWeekLine, TheStools.HerWeekNote })
        {
            Assert.Contains("slip", said, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("files", said, StringComparison.OrdinalIgnoreCase);
        }

        // And she is not the plot: she is named by her trade and her rest days, and by nothing else.
        Assert.Contains("SCAFFOLDER", TheStools.NeighbourPlate, StringComparison.Ordinal);
    }
}

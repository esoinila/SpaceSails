using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 · THE VOID'S WEATHER — the eight lines about the walking insurance men, and the law that decides
/// when one of them is in the air.
///
/// <para>OWNER RULING 2026-08-25: <i>"a great way to have fun and sell the story, it could be the thing
/// people talk about in the bars that unites them, a bit like talking about the weather on planet side."</i></para>
///
/// <para>Every claim below is made against the SHIPPING function, never against a re-implementation of it
/// beside the assertion — and every one of them is written so it can fail. The red proofs are named at each
/// test, because a guard whose author never watched it go red is this repo's fifth named bug class.</para>
/// </summary>
public class TheVoidsWeatherTests
{
    private const string Thread = "thread-weather";
    private const string Station = "phobos";

    /// <summary>An unheard book — every line still in the bag.</summary>
    private static Dictionary<string, int> Fresh() => new(System.StringComparer.Ordinal);

    // ── THE WORDS ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The eight, as Fable wrote them on #973. Transcribed here rather than read out of the pool,
    /// because a guard that asked the pool for its own words would agree with any edit ever made to it.
    ///
    /// <para>Red proof: change one character of one line in <c>InsuranceWeather.Lines</c>.</para></summary>
    [Fact]
    public void TheEightAreFablesWordsVerbatim()
    {
        string[] authored =
        [
            "Fess get to you yet? He got to me. Basic. Don't tell my mother.",
            "The insurance man walked the whole concourse twice today. Somebody's dying, mark me.",
            "I told him no three times and he wrote something down. What is he writing down?",
            "Premium remembers, he says. Remembers what, I ask. He smiled like a filing cabinet.",
            "My cousin lapsed. Woke up meaner and broker, just like the poster says. Still owes me forty.",
            "He never forgets a file and he never remembers a face. That's the job, I suppose.",
            "You can set your watch by that man's rounds. The void takes appointments after all.",
            "Somebody stood him a drink once. He drank it and pitched the glass a policy.",
        ];

        Assert.Equal(authored.Length, InsuranceWeather.Lines.Count);
        Assert.Equal<IEnumerable<string>>(authored, [.. InsuranceWeather.Lines.Select(l => l.Text)]);

        // The ninth line of the feature: what the room does when somebody says one of the eight out loud.
        Assert.Equal("Half the room nods. The other half checks a pocket.", InsuranceWeather.RoomsReaction);

        // The ids are stable and unique — they are what the save carries and what retires.
        Assert.Equal(
            InsuranceWeather.Lines.Count,
            InsuranceWeather.Lines.Select(l => l.Id).Distinct(System.StringComparer.Ordinal).Count());

        // …and the lapsed cousin is the fifth, which is the one the arc-touching rule is written about.
        Assert.Equal(InsuranceWeather.LapsedCousinId, InsuranceWeather.Lines[4].Id);
    }

    /// <summary>The house register law: the word this repo does not print at a player. Red proof: put it in
    /// any of the nine sentences.</summary>
    [Fact]
    public void NoGameTextInTheWeatherSaysCopy()
    {
        foreach (string text in InsuranceWeather.Lines.Select(l => l.Text)
                     .Append(InsuranceWeather.RoomsReaction))
        {
            Assert.DoesNotContain("copy", text, System.StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── THE LAW ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>ONE LINE PER VISIT, and the same one however many times it is asked for. Red proof: fold the
    /// clock or a call counter into the seed and this goes red on the second ask.</summary>
    [Fact]
    public void AVisitHasOneConversationHoweverOftenYouLeanOnTheCounter()
    {
        var heard = Fresh();
        string? first = InsuranceWeather.Draw(Thread, Station, 7, fessIsHere: true, heard, lastSaidAtVisit: -1);
        for (int again = 0; again < 20; again++)
        {
            Assert.Equal(first, InsuranceWeather.Draw(Thread, Station, 7, true, heard, -1));
        }
    }

    /// <summary>NEVER TWO VISITS RUNNING AT THE SAME STATION — the rule that keeps small talk from being a
    /// jukebox. Walked as a captain actually walks it: every visit that surfaces a line is followed by a
    /// visit that cannot.
    ///
    /// <para>Red proof: delete the <c>lastSaidAtVisit == stationVisit - 1</c> gate and this fails at the
    /// first back-to-back pair, which on the fess-is-here weighting arrives within a handful of visits.</para></summary>
    [Fact]
    public void ALineIsNeverInTheAirTwoVisitsRunningAtOneStation()
    {
        var heard = Fresh();
        int lastSaid = -1;
        int surfaced = 0;

        for (int visit = 0; visit < 200; visit++)
        {
            string? line = InsuranceWeather.Draw(Thread, Station, visit, true, heard, lastSaid);
            if (line is null)
            {
                continue;
            }

            // …and never on the visit straight after one it was in the air on (−1 is "never yet").
            Assert.True(lastSaid < 0 || lastSaid != visit - 1,
                $"the weather blew through visit {lastSaid} and visit {visit} running");
            lastSaid = visit;
            surfaced++;
            heard[line] = (heard.TryGetValue(line, out int times) ? times : 0) + 1;
        }

        Assert.True(surfaced > 0, "no line ever surfaced in 200 visits — this law would pass on silence");
    }

    /// <summary>…and the station is the KEY, not the calendar: two calls at one port stay consecutive
    /// however many other ports were visited between them. Red proof: key the rule on a global visit counter
    /// and a captain shuttling between two bars hears it every single time at both.</summary>
    [Fact]
    public void TheGateIsThisStationsOwnVisitOrdinal()
    {
        // Visit 4 at Phobos said something; the next call at Phobos is visit 5 whatever happened elsewhere.
        Assert.Null(InsuranceWeather.Draw(Thread, Station, 5, true, Fresh(), lastSaidAtVisit: 4));

        // …and a different station is a different room with its own memory.
        Assert.NotNull(InsuranceWeather.Draw(Thread, Station, 5, true, Fresh(), lastSaidAtVisit: -1)
                       ?? InsuranceWeather.Draw(Thread, Station, 6, true, Fresh(), -1)
                       ?? InsuranceWeather.Draw(Thread, Station, 7, true, Fresh(), -1));
    }

    /// <summary>THE MAN IN THE ROOM IS WORTH ×3, proved statistically over many seeds rather than asserted
    /// off one. The room talks about the man who walked through it.
    ///
    /// <para>Red proof: set <c>FessIsHereWeight = 1</c> and the two rates come out equal.</para></summary>
    [Fact]
    public void FessInTheRoomWeightsTheWeatherUpThreefold()
    {
        int quiet = 0;
        int walked = 0;
        const int Seeds = 20000;

        for (int i = 0; i < Seeds; i++)
        {
            ulong seed = DiceRule.Seed("weather-weighting", i);
            if (InsuranceWeather.RoomWouldTalk(seed, fessIsHere: false))
            {
                quiet++;
            }

            if (InsuranceWeather.RoomWouldTalk(seed, fessIsHere: true))
            {
                walked++;
            }
        }

        double quietRate = quiet / (double)Seeds;
        double walkedRate = walked / (double)Seeds;

        // The shipped tuning: one visit in four, three in four where he walked.
        Assert.InRange(quietRate, 0.22, 0.28);
        Assert.InRange(walkedRate, 0.72, 0.78);
        Assert.InRange(walkedRate / quietRate, 2.7, 3.3);
    }

    /// <summary>A LINE HEARD THREE TIMES RETIRES FOR THE THREAD, and the draw is WITHOUT REPLACEMENT from
    /// what is left. Red proof: raise <c>RetireAfterHearings</c> in the rule but not here, or drop the pool
    /// filter, and a retired sentence comes back.</summary>
    [Fact]
    public void ASentenceHeardThreeTimesIsNeverDrawnAgain()
    {
        var heard = Fresh();
        foreach (InsuranceWeather.Line line in InsuranceWeather.Lines.Take(7))
        {
            heard[line.Id] = InsuranceWeather.RetireAfterHearings;
        }

        // Seven retired: whatever surfaces can only ever be the eighth.
        string only = InsuranceWeather.Lines[7].Id;
        Assert.Equal(1, InsuranceWeather.Unretired(heard));

        bool everSaid = false;
        for (int visit = 0; visit < 100; visit += 2)   // even visits only, so the never-twice gate is idle
        {
            if (InsuranceWeather.Draw(Thread, Station, visit, true, heard, -1) is { } drawn)
            {
                Assert.Equal(only, drawn);
                everSaid = true;
            }
        }

        Assert.True(everSaid, "the last unretired line never surfaced — this law would pass on silence");
    }

    /// <summary>WHEN ALL EIGHT RETIRE THE WEATHER IS OVER — the room has said what it has to say, and
    /// nothing rolls again for this thread. Red proof: return a line from an empty pool (index a modulus of
    /// the full list instead of the unretired one) and this goes red at once.</summary>
    [Fact]
    public void WhenEveryLineHasRetiredTheRoomIsQuietForGood()
    {
        var heard = Fresh();
        foreach (InsuranceWeather.Line line in InsuranceWeather.Lines)
        {
            heard[line.Id] = InsuranceWeather.RetireAfterHearings;
        }

        Assert.Equal(0, InsuranceWeather.Unretired(heard));
        for (int visit = 0; visit < 500; visit++)
        {
            Assert.Null(InsuranceWeather.Draw(Thread, "any-station", visit, true, heard, -1));
        }
    }

    /// <summary>A whole thread, walked: eight lines × three tellings is the most the weather can ever say,
    /// and it says exactly that and then stops. The feature's total budget, stated once.</summary>
    [Fact]
    public void TheWeatherHasExactlyTwentyFourTellingsInIt()
    {
        var heard = Fresh();
        int said = 0;
        int lastSaid = -1;

        for (int visit = 0; visit < 5000 && InsuranceWeather.Unretired(heard) > 0; visit++)
        {
            if (InsuranceWeather.Draw(Thread, Station, visit, true, heard, lastSaid) is not { } line)
            {
                continue;
            }

            heard[line] = (heard.TryGetValue(line, out int times) ? times : 0) + 1;
            lastSaid = visit;
            said++;
        }

        Assert.Equal(InsuranceWeather.Lines.Count * InsuranceWeather.RetireAfterHearings, said);
        Assert.Equal(0, InsuranceWeather.Unretired(heard));
    }

    // ── THE BLOCK ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE BLOCK STAYS THREE LINES. An insurance line takes a slot from whatever this counter said
    /// longest ago; it never adds a fourth row.
    ///
    /// <para>Red proof: append the weather instead of replacing, and the full-book case comes back four.</para></summary>
    [Fact]
    public void TheOverheardBlockNeverGrowsPastThree()
    {
        string weather = InsuranceWeather.Overheard("A REGULAR", InsuranceWeather.Lines[0].Text);

        for (int book = 0; book <= 5; book++)
        {
            IReadOnlyList<string> counter = [.. Enumerable.Range(0, book).Select(i => $"line {i}")];

            IReadOnlyList<string> without = InsuranceWeather.Block(counter, null, 1UL);
            Assert.Equal(System.Math.Min(book, InsuranceWeather.BlockLines), without.Count);

            IReadOnlyList<string> with = InsuranceWeather.Block(counter, weather, 1UL);
            Assert.InRange(with.Count, 1, InsuranceWeather.BlockLines);
            Assert.Contains(weather, with);

            // One line and one only, and the newest of the counter's own are the ones it keeps company with.
            Assert.Equal(1, with.Count(l => l == weather));
            for (int kept = 0; kept < System.Math.Min(book, InsuranceWeather.BlockLines - 1); kept++)
            {
                Assert.Contains($"line {kept}", with);
            }
        }
    }

    /// <summary>…and where in the three it lands is seeded, not fixed — a room whose small talk is always the
    /// top line is a room reading from a rota. Red proof: hard-code the insert at 0.</summary>
    [Fact]
    public void TheWeatherDoesNotAlwaysSitAtTheTopOfTheBlock()
    {
        string weather = InsuranceWeather.Overheard("A REGULAR", InsuranceWeather.Lines[1].Text);
        IReadOnlyList<string> counter = ["a", "b", "c"];

        var slots = new HashSet<int>();
        for (int seed = 0; seed < 200; seed++)
        {
            slots.Add(InsuranceWeather.Block(counter, weather, DiceRule.Seed("slot", seed)).ToList().IndexOf(weather));
        }

        Assert.True(slots.Count > 1, "the weather always landed in the same slot");
        Assert.All(slots, s => Assert.InRange(s, 0, InsuranceWeather.BlockLines - 1));
    }

    /// <summary>THE ROUND'S SHARED TOPIC: the whole block becomes the one line, said by a named regular, and
    /// what the room does about it. Red proof: drop the reaction and the block is one line.</summary>
    [Fact]
    public void AStoodRoundTurnsTheBlockIntoTheOneLineAndTheRoomsAnswer()
    {
        IReadOnlyList<string> shared =
            InsuranceWeather.SharedTopic("GALE CORBIN", InsuranceWeather.Lines[6].Text);

        Assert.Equal(2, shared.Count);
        Assert.Contains("GALE CORBIN", shared[0], System.StringComparison.Ordinal);
        Assert.Contains(InsuranceWeather.Lines[6].Text, shared[0], System.StringComparison.Ordinal);
        Assert.Equal(InsuranceWeather.RoomsReaction, shared[1]);
    }

    /// <summary>One rendering of one overheard sentence, wherever it is drawn — the block, the round's topic
    /// and the sheet the cousin files all say it the same way.</summary>
    [Fact]
    public void ALineIsRenderedTheSameWayEverywhereItIsSaid()
    {
        string text = InsuranceWeather.Lines[2].Text;
        Assert.Equal($"A REGULAR: “{text}”", InsuranceWeather.AsHeard("A REGULAR", text));
        Assert.EndsWith(InsuranceWeather.AsHeard("A REGULAR", text), InsuranceWeather.Overheard("A REGULAR", text),
            System.StringComparison.Ordinal);
    }

    // ── THE ONE PLACE IT TOUCHES THE ARC ───────────────────────────────────────────────────────────────

    /// <summary>THE COUSIN WHO LAPSED IS THE ONLY LINE THAT WRITES ANYTHING, and only for a captain who is
    /// already holding the fleet-day page. Both branches, and all seven of the others in both branches.
    ///
    /// <para>Red proof: drop the <c>holdsTheFleetDayPage</c> conjunct and half of these go red; drop the id
    /// comparison and the other seven do.</para></summary>
    [Fact]
    public void OnlyTheLapsedCousinFilesAnything_AndOnlyWithTheFleetDayPageInHand()
    {
        Assert.True(InsuranceWeather.FilesANote(InsuranceWeather.LapsedCousinId, holdsTheFleetDayPage: true));
        Assert.False(InsuranceWeather.FilesANote(InsuranceWeather.LapsedCousinId, holdsTheFleetDayPage: false));

        foreach (InsuranceWeather.Line line in InsuranceWeather.Lines
                     .Where(l => l.Id != InsuranceWeather.LapsedCousinId))
        {
            Assert.False(InsuranceWeather.FilesANote(line.Id, true), $"{line.Id} wrote something down");
            Assert.False(InsuranceWeather.FilesANote(line.Id, false), $"{line.Id} wrote something down");
        }

        Assert.False(InsuranceWeather.FilesANote(null, true));
        Assert.False(InsuranceWeather.FilesANote("not-a-line", true));

        // The sheet it files is HIS and about money — the man telling it was there and the captain was not,
        // and a premium that ran out is a story about money whatever else it is about.
        Assert.Equal(HeldMemory.Mark.His, InsuranceWeather.LapsedCousinMark);
        Assert.Equal(HeldMemory.Theory.Money, InsuranceWeather.LapsedCousinTag);
    }

    /// <summary>The words are asked of the pool off the id and never carried beside it — a sentence stored
    /// next to the id that names it is two answers to one question.</summary>
    [Fact]
    public void AnIdThisBuildDoesNotKnowIsDroppedRatherThanInvented()
    {
        Assert.Equal(InsuranceWeather.Lines[3].Text, InsuranceWeather.TextOf(InsuranceWeather.Lines[3].Id));
        Assert.Null(InsuranceWeather.TextOf("a-line-from-a-rolled-back-build"));
        Assert.Null(InsuranceWeather.TextOf(null));
    }

    // ── THE KEEPING ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The weather's section reaches the file and comes back whole, through REAL JSON. The
    /// reflected law next door governs every section's presence; this one proves the ROWS this lane
    /// invented survive, which a generically-filled section cannot say.</summary>
    [Fact]
    public void TheWeatherSurvivesTheFile()
    {
        var vault = new Vault
        {
            InsuranceWeather = new InsuranceWeatherSection
            {
                Heard = [$"{InsuranceWeather.LapsedCousinId}|2", "set-your-watch-by-him|3"],
                Stations = ["phobos|4|3", "the-space-bar|11|-1"],
            },
        };

        Vault back = VaultSerializer.Load(VaultSerializer.Save(vault));

        Assert.False(back.Tampered);
        Assert.NotNull(back.InsuranceWeather);
        Assert.Equal(vault.InsuranceWeather.Heard, back.InsuranceWeather!.Heard);
        Assert.Equal(vault.InsuranceWeather.Stations, back.InsuranceWeather.Stations);
    }
}

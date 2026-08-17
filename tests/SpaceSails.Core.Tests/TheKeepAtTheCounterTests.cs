using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #781 · THE KEEP AT CANTEEN 1 — the Core half.
///
/// <para>Owner, live at the counter 2026-08-08: <i>"There should be a bar keep there (really like hidden
/// security guard)."</i></para>
///
/// <para>Four laws, and one of them is a law about SILENCE. He works the living watches and the counter
/// serves itself on the dead ones; the tell rotates by the WATCH and never by a clock; a rumour bought is an
/// entry in his book with the right subject on it; and the room can only say it back on a LATER watch, and
/// only about something you actually asked. The fifth guard is the one this whole design lives or dies on:
/// not one string anywhere may say what he is.</para>
///
/// <para>The purchase math is not re-tested here — <c>BarkeepTests</c> and <c>TheCounterTakesOrdersTests</c>
/// own it, and the keep pours off the very same card, which is the point of the fork being a
/// <c>with</c> expression rather than a second literal.</para>
/// </summary>
public sealed class TheKeepAtTheCounterTests
{
    private const string Site = "europa";
    private const UndergroundComplex.Comfort Canteen = UndergroundComplex.Comfort.UpperCanteen;

    /// <summary>Two full days of watches — long enough that both readings have to turn up, short enough to
    /// name every one of them in a failure message.</summary>
    private static IEnumerable<long> ADayAndAnother() => Enumerable.Range(0, 12).Select(i => (long)i);

    // ── (a) WHO IS BEHIND IT IS A FACT ABOUT THE WATCH ──────────────────────────────────────────────────

    [Fact]
    public void TheLivingWatchesAreServedAndTheDeadOnesServeThemselves()
    {
        // #772's reading was never the whole truth, only the small-hours one. Both halves have to be
        // REACHABLE on the watches the game actually has, or this is a fork with one road.
        //
        // Red proof: make TheKeep.KeptWatch return true (or false) for every watch and the count below that
        // went to zero names itself.
        Barkeep selfService = CounterService.For(Site, Canteen)
            ?? throw new InvalidOperationException("the branch counter stopped serving");

        int kept = 0;
        int alone = 0;
        var wrong = new List<string>();

        foreach (long watch in ADayAndAnother())
        {
            Barkeep serving = CounterService.For(Site, Canteen, watch)
                ?? throw new InvalidOperationException($"nobody serves at all on watch {watch}");

            if (TheKeep.KeptWatch(watch))
            {
                kept++;
                if (!CounterService.ServedByTheKeep(serving))
                {
                    wrong.Add($"  watch {watch}: the hall is full and nobody is behind the counter.");
                }
                if (serving.SelfService)
                {
                    wrong.Add($"  watch {watch}: the keep is working and the card still says serve yourself.");
                }
            }
            else
            {
                alone++;
                // BYTE FOR BYTE. Not "looks like" #772's counter — IS it, the very instance, so a field
                // added to the keep tomorrow cannot quietly land on the dead-watch reading too.
                Assert.Same(selfService, serving);
                if (CounterService.ServedByTheKeep(serving))
                {
                    wrong.Add($"  watch {watch}: somebody is tending bar on a watch this hall is empty on.");
                }
            }
        }

        Assert.True(kept > 0, "nobody works any watch — the keep never appears in the game at all.");
        Assert.True(alone > 0, "every watch is kept — #772's self-service counter is unreachable.");
        Assert.True(wrong.Count == 0,
            $"the counter is served by the wrong side of the fork:{Environment.NewLine}{string.Join(Environment.NewLine, wrong)}");
    }

    [Fact]
    public void TheKeepPoursTheCountersOwnCardAndBringsNoNewDrink()
    {
        // Canon: "DrinkName/DrinkFlavor/prices: reuse the counter's existing house card — no new drink."
        // Structural rather than promised: the kept record is BranchCounter `with` four fields changed, so
        // everything else has to be the same object's, and a guard can say which four.
        Barkeep counter = CounterService.For(Site, Canteen)!;
        Barkeep keep = CounterService.For(Site, Canteen, TheKeptWatch())!;

        Assert.Same(counter.House, keep.House);
        Assert.Equal(CounterService.Card.Count, DrinkMenu.For(keep).Count);
        Assert.Equal(counter.DrinkName, keep.DrinkName);
        Assert.Equal(counter.DrinkFlavor, keep.DrinkFlavor);
        Assert.Equal(counter.DrinkPrice, keep.DrinkPrice);
        Assert.Equal(counter.RoundPrice, keep.RoundPrice);
        Assert.Equal(counter.BarName, keep.BarName);
        Assert.Equal(counter.DeskArtUrl, keep.DeskArtUrl);

        // …and the four that DO differ are the whole of the change.
        Assert.Equal(TheKeep.Name, keep.Name);
        Assert.Equal(TheKeep.Welcome, keep.Welcome);
        Assert.Equal(TheKeep.Welcome, keep.Greeting);
        Assert.Same(TheKeep.Rumors, keep.Rumors);
        Assert.False(keep.SelfService);

        // The counter's own plate is untouched: the keep has no plate that says what he is (canon), and the
        // haven bars are untouched by a fork about one fixture.
        Assert.Equal("THE COUNTER", counter.Name);
        foreach (Barkeep haven in Barkeeps.AllBarkeeps)
        {
            Assert.Same(haven, CounterService.OnWatch(haven, TheKeptWatch()));
        }
    }

    [Fact]
    public void ThePersonVerbsOpenOnlyWhereThereIsAPerson()
    {
        // #772's gate LIFTS rather than disappears: a round for the room and asking what he has heard are
        // exactly the two the card drops when nobody is back there, and they must not appear on a dead watch.
        Barkeep keep = CounterService.For(Site, Canteen, TheKeptWatch())!;
        Barkeep alone = CounterService.For(Site, Canteen, TheDeadWatch())!;

        Assert.NotEmpty(keep.Rumors);
        Assert.Equal(3, keep.Rumors.Count);
        Assert.Empty(alone.Rumors);
        Assert.True(alone.SelfService);
    }

    // ── (b) THE TELL ROTATES BY THE WATCH, AND BY NOTHING ELSE ──────────────────────────────────────────

    [Fact]
    public void OneTellPerWatchRotatedByTheWatchAndNeverByAClock()
    {
        // Red proof: seed it off the sim clock instead — TellAt(double simTime) — and the signature claim
        // below fails before the rotation claims are even reached.
        MethodInfo tellAt = typeof(TheKeep).GetMethod(nameof(TheKeep.TellAt))!;
        ParameterInfo[] takes = tellAt.GetParameters();
        Assert.Single(takes);
        Assert.Equal(typeof(long), takes[0].ParameterType);
        Assert.Equal("watch", takes[0].Name);

        // ONE PER WATCH, and the same one every time it is asked — a captain standing at the counter is not
        // reading a sentence that changes under them.
        foreach (long watch in ADayAndAnother())
        {
            string said = TheKeep.TellAt(watch);
            Assert.Contains(said, TheKeep.Tells);
            Assert.Equal(said, TheKeep.TellAt(watch));
            Assert.NotEqual(said, TheKeep.TellAt(watch + 1));   // it ROTATES; a constant is not a rotation
        }

        // …and all three are reachable on watches somebody is actually working, which is the only kind of
        // watch this line is ever drawn on. Two of the three showing would be content that never ships.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (long watch in ADayAndAnother())
        {
            if (TheKeep.KeptWatch(watch))
            {
                seen.Add(TheKeep.TellAt(watch));
            }
        }
        Assert.Equal(TheKeep.Tells.Count, seen.Count);

        // Negative watches too — defensive, the way every other watch-indexed answer in this repo is.
        Assert.Contains(TheKeep.TellAt(-1), TheKeep.Tells);
        Assert.Contains(TheKeep.TellAt(-7), TheKeep.Tells);
    }

    // ── (c) ASKING IS BEING SEEN ASKING ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ARumourBoughtIsOneEntryInHisBookWithTheRightSubjectOnIt()
    {
        // The exact three-line write the counter makes, done here on Core's own objects: the topic is read
        // off the SAME index the sentence came from, so the leak can never name a subject the captain was
        // not told about. Red proof: hand AskedEntry a topic taken off a different index.
        Barkeep keep = CounterService.For(Site, Canteen, TheKeptWatch())!;
        Assert.Equal(keep.Rumors.Count, TheKeep.Topics.Count);

        var ledger = new ContactLedger();
        var wanted = new List<int>();

        // One sim-hour per rumour, walked far enough round to hit each of the three at least once.
        for (int hour = 0; hour < 6; hour++)
        {
            double simTime = hour * 3600.0 + 60.0;
            int index = keep.RumorIndexAt(simTime);
            Assert.Equal(keep.Rumors[index], keep.RumorAt(simTime));

            string? topic = TheKeep.TopicOf(index);
            Assert.NotNull(topic);

            string entry = TheKeep.AskedEntry(topic, 3);

            // THE SUBJECT IS THE ONE THE CAPTAIN WAS TOLD ABOUT. Read straight back out of the entry and
            // matched to the index the SENTENCE came off — not merely "some topic", which is what a guard
            // that asked whether Topics[i] is in Topics would have been checking. Red proof: shift the
            // index AskedEntry writes by one and every hour below names itself.
            Assert.True(TheKeep.TryReadAsk(entry, out int wrote, out long watch));
            Assert.Equal(index, wrote);
            Assert.Equal(3, watch);
            Assert.Equal(topic, TheKeep.Topics[wrote]);

            ledger.RecordKnownTell(TheKeep.LedgerId, TheKeep.Name, entry);
            if (!wanted.Contains(index))
            {
                wanted.Add(index);
            }
        }

        // …and the walk really did go round all three, or the claim above was made about one rumour.
        Assert.Equal(TheKeep.Topics.Count, wanted.Count);

        // ONE ENTITY, and its book holds one entry per DISTINCT thing asked — three rumours, three subjects,
        // three entries, however many times the captain went round the hour.
        ContactHistory his = ledger.For(TheKeep.LedgerId);
        Assert.Equal(TheKeep.Topics.Count, his.KnownTells.Length);
        Assert.Single(ledger.Entries);

        var parsed = new List<int>();
        foreach (string entry in his.KnownTells)
        {
            Assert.True(TheKeep.TryReadAsk(entry, out int topicIndex, out long watch));
            Assert.Equal(3, watch);
            parsed.Add(topicIndex);
        }
        parsed.Sort();
        Assert.Equal([0, 1, 2], parsed);

        // …and the entry is a MACHINE ID and not a sentence: nothing in it could be printed at somebody.
        Assert.All(his.KnownTells, e => Assert.StartsWith(TheKeep.AskTag + ":", e, StringComparison.Ordinal));
        Assert.All(his.KnownTells, e => Assert.DoesNotContain(' ', e));

        // Nobody else's tells are ours: #306's slips over a drink share this very list.
        Assert.False(TheKeep.TryReadAsk("the hot ORE in your hold", out _, out _));
        Assert.False(TheKeep.TryReadAsk(null, out _, out _));
        Assert.False(TheKeep.TryReadAsk("counter-ask:9:1", out _, out _));   // no such topic
    }

    // ── (d) AND A SHIFT LATER, THE ROOM SAYS IT BACK ────────────────────────────────────────────────────

    [Fact]
    public void NothingLeaksBeforeARumourWasBoughtOrOnTheWatchItWasBoughtOn()
    {
        // Red proof, both ways: drop the `askedOn < watch` clause and the SAME-watch sweep goes red; drop
        // the empty-list guard and the nothing-asked sweep goes red.
        foreach (long watch in Watches(0, 60))
        {
            Assert.Null(TheKeep.LeakedTopic([], Site, watch));
            Assert.Null(TheKeep.LeakedTopic(null, Site, watch));
            Assert.Null(TheKeep.LeakedTopic(["a tell that is not ours"], Site, watch));
        }

        // …AND NOT ON THE WATCH IT WAS ASKED ON, which is the whole of "later" and the clause a one-character
        // slip turns off. Measured rather than sampled: a single (site, watch) pair would have been green on
        // `askedOn <= watch` too, because the room is silent on most watches anyway — so this walks a spread
        // AND counts, on the very same pairs, how many of them WOULD have spoken had the ask been one watch
        // older. That count is the guard's own proof it can tell pass from fail.
        string[] sites = ["europa", "miranda", "titan", "callisto", "triton"];
        int couldHaveSpoken = 0;

        foreach (string site in sites)
        {
            foreach (long asked in Watches(0, 40))
            {
                Assert.Null(TheKeep.LeakedTopic([TheKeep.AskedEntry(TheKeep.Topics[0], asked)], site, asked));
                for (long earlier = asked - 3; earlier < asked; earlier++)
                {
                    Assert.Null(TheKeep.LeakedTopic([TheKeep.AskedEntry(TheKeep.Topics[0], asked)], site, earlier));
                }
                if (TheKeep.LeakedTopic([TheKeep.AskedEntry(TheKeep.Topics[0], asked - 1)], site, asked) is not null)
                {
                    couldHaveSpoken++;
                }
            }
        }

        Assert.True(couldHaveSpoken > 0,
            "the room is silent on every watch in the sweep, so the same-watch claim above proves nothing.");
    }

    [Fact]
    public void OnALaterWatchItCanBeSaidBackAndOnlyAboutWhatWasAsked()
    {
        const long asked = 7;
        string[] book = [TheKeep.AskedEntry(TheKeep.Topics[0], asked)];

        int said = 0;
        int silent = 0;
        foreach (long watch in Watches(asked + 1, 60))
        {
            string? leak = TheKeep.LeakedTopic(book, Site, watch);
            if (leak is null)
            {
                silent++;
                continue;
            }
            said++;
            Assert.Equal(TheKeep.Topics[0], leak);
            Assert.Equal(TheKeep.LeakedTopic(book, Site, watch), leak);   // deterministic per (site, watch)
        }

        // #587's lesson: a leak that never surfaces is content that does not ship, and one that surfaces on
        // every watch is a receipt rather than a thing that happens to you.
        Assert.True(said > 0, "the room never once says it back — the consequence is unreachable.");
        Assert.True(silent > 0, "the room says it back on every single watch, which is not a rumour.");
    }

    [Fact]
    public void TwoSubjectsAskedAboutAreTwoSubjectsTheRoomCanUseAndTheOrderDoesNotDecide()
    {
        // FOURTH NAMED BUG CLASS, paid up front: the ledger hands its tells over in the order they were
        // appended, so a leak that consumed them unsorted would say a different sentence on two saves
        // holding exactly the same facts.
        string[] inOneOrder =
        [
            TheKeep.AskedEntry(TheKeep.Topics[0], 2),
            TheKeep.AskedEntry(TheKeep.Topics[2], 3),
        ];
        string[] intTheOther = [inOneOrder[1], inOneOrder[0]];

        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (long watch in Watches(4, 80))
        {
            string? one = TheKeep.LeakedTopic(inOneOrder, Site, watch);
            Assert.Equal(one, TheKeep.LeakedTopic(intTheOther, Site, watch));
            if (one is not null)
            {
                Assert.NotEqual(TheKeep.Topics[1], one);   // never a subject nobody asked about
                used.Add(one);
            }
        }

        Assert.Equal(2, used.Count);   // both asked-about subjects are reachable
        Assert.Contains(TheKeep.Topics[0], used);
        Assert.Contains(TheKeep.Topics[2], used);
    }

    [Fact]
    public void TheLeakCarriesTheSubjectAndNeverQuotesTheKeep()
    {
        foreach (string topic in TheKeep.Topics)
        {
            string bark = TheKeep.LeakBark(topic);
            Assert.Contains(topic, bark, StringComparison.Ordinal);
            Assert.Equal("“Word at the counter is you were asking about " + topic + ".”", bark);
            Assert.DoesNotContain(TheKeep.Name, bark, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── (e) AND NOT ONE STRING SAYS WHAT HE IS ──────────────────────────────────────────────────────────

    [Fact]
    public void NothingAnywhereSaysTheKeepIsSecurity()
    {
        // THE LAW THIS WHOLE DESIGN LIVES ON (§13.8, #649's discipline). The owner's own direction names it
        // — "really like hidden security guard" — and the game may never once say so. Tells, never
        // statements: a player who notices the hands, the rag and the break has been told everything and
        // shown nothing.
        //
        // Red proof: plant the word "security" in any line of TheKeep's and this names the line.
        string[] neverSaidOfHim =
        [
            "security", "guard", "surveill", "informant", "informer", "watchman", "spy", "snitch",
            "company man", "keeps an eye", "watching you", "reports to", "police", "bouncer", "on the payroll",
        ];

        var swept = new List<string>();
        swept.AddRange(TheKeep.AllProse());
        swept.Add(CounterService.For(Site, Canteen)!.Welcome!);
        swept.Add(CounterService.For(Site, Canteen, TheKeptWatch())!.Welcome!);
        foreach (Drink d in CounterService.Card)
        {
            swept.Add($"{d.Name} {d.Flavor}");
        }

        // #587's lesson again: a sweep with nothing in it passes forever.
        Assert.True(swept.Count >= 20, $"only {swept.Count} strings swept — the canon guard went empty.");

        foreach (string line in swept)
        {
            foreach (string leak in neverSaidOfHim)
            {
                Assert.False(
                    line.Contains(leak, StringComparison.OrdinalIgnoreCase),
                    $"“{line}” says “{leak}” — the keep is a thing the game shows and never states (#781/§13.8).");
            }
        }

        // …and he has no name and no plate that says what he is. The card's line is the two words below and
        // nothing else has ever been written for it.
        Assert.Equal("the keep", TheKeep.Name);
        foreach (string line in TheKeep.AllProse())
        {
            Assert.DoesNotContain("barkeep", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("bartender", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheWordsAreTheOnesThatWereWritten()
    {
        // Canon, VERBATIM (issue #781, "CANON — the keep's words"). Pinned character for character, because
        // prose in this repo is authored once and a paraphrase in a later edit is a silent loss.
        Assert.Equal("“Coffee's on. Whatever else you're after, say it once.”", TheKeep.Welcome);

        Assert.Equal(
            "He is looking at your hands when you put them on the counter, and then he is not.",
            TheKeep.Tells[0]);
        Assert.Equal(
            "The rag goes over the same dry patch it went over when the hauliers came in.",
            TheKeep.Tells[1]);
        Assert.Equal(
            "He has not taken a break since the cage started moving. You notice because you were counting.",
            TheKeep.Tells[2]);

        Assert.Equal(
            "“Somebody upstairs is paying overtime on the freight side. Nobody upstairs is paying for coffee.”",
            TheKeep.Rumors[0]);
        Assert.Equal(
            "“The round comes past here twice an hour, near enough. I don't set my watch by it. I could.”",
            TheKeep.Rumors[1]);
        Assert.Equal(
            "“There's a fellow in the ring offices who's been in the same chair since Thursday. Somebody "
            + "should tell him the park doesn't close.”",
            TheKeep.Rumors[2]);

        Assert.Equal(
            "You asked the keep. He answered without looking up, and without needing to.",
            TheKeep.FieldNote);

        Assert.Equal(["the freight side", "the round", "the ring offices"], TheKeep.Topics);

        // #778's haulier is untouched, and it is now literally true. Nothing joins the two.
        Assert.Contains("they know my face at the counter", SittingAlone.TheAskLine, StringComparison.Ordinal);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────────────────────────────

    private static long TheKeptWatch()
    {
        foreach (long watch in ADayAndAnother())
        {
            if (TheKeep.KeptWatch(watch))
            {
                return watch;
            }
        }
        throw new InvalidOperationException("no watch in a whole day is worked — the fork has one road.");
    }

    private static long TheDeadWatch()
    {
        foreach (long watch in ADayAndAnother())
        {
            if (!TheKeep.KeptWatch(watch))
            {
                return watch;
            }
        }
        throw new InvalidOperationException("every watch is worked — #772's counter is unreachable.");
    }

    private static IEnumerable<long> Watches(long from, int count) =>
        Enumerable.Range(0, count).Select(i => from + i);
}

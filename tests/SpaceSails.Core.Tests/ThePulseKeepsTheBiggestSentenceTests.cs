using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #693 · ONE PULSE SLOT, LAST WRITE WINS — AND THE CLIMAX LOST.
///
/// <para>Owner's issue: <i>"#592's <c>UnlistedArrivalLine</c> — the CLIMAX, the first words on the floor that
/// does not exist — is overwritten by the routine pressurisation/air line on the same arrival. The biggest
/// sentence in the feature has been losing to boilerplate since it shipped."</i></para>
///
/// <para>It was never a bug in any one call site. It was a contract — <i>whoever writes last is heard</i> —
/// that lived in comments at three call sites and nowhere else, so every new saying added to an arrival was
/// one more chance to silence an older, bigger one by being written above it. #692 had to encode "last of the
/// arrival's sayings" as a comment plus an ordering test to ship at all, which is what a missing law looks
/// like from the inside.</para>
///
/// <para>The law is <see cref="PulseSlot"/>: <b>a lower-ranked line may not displace a higher-ranked one that
/// is still held; among equals the last written wins.</b> These sweep it over every floor arrival the Hive's
/// generator admits and assert the biggest sentence is the one on screen — and, because the point of a law is
/// that it is not an ordering, that shuffling the arrival's own list changes nothing.</para>
///
/// <para><b>Red proof.</b> Take the rank clause out of <c>PulseSlot.Write</c> (i.e. put "last write wins"
/// back) and <see cref="TheBiggestSentenceOfEveryArrivalTheGeneratorAdmitsIsTheOneOnScreen"/> and
/// <see cref="TheUnlistedBandsClimaxIsNotEatenByTheRoutineAirLine"/> both go red, naming #592's floor.</para>
/// </summary>
public sealed class ThePulseKeepsTheBiggestSentenceTests
{
    /// <summary>The scenario's own sites, plus the two cheat rocks. Without the last two every sweep here
    /// audits a universe where the band nobody listed and the halls nobody dug do not exist, and passes for
    /// the wrong reason — the fifth named bug class.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
        KaamosLore.IceMoonBodyId,
    ];

    /// <summary>Every authority this site ever issued, which is what <c>?found=1</c> puts in the wallet and
    /// what a captain who has done the whole hunt is carrying.</summary>
    private static HashSet<string> WholeWallet(string body)
    {
        var wallet = new HashSet<string>();
        int last = UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor);
        for (int band = 0; band <= last; band++)
        {
            if (UndergroundComplex.SiteHasBand(body, band))
            {
                wallet.Add(new UndergroundComplex.AuthorityCard(body, band).Id);
            }
        }
        return wallet;
    }

    /// <summary>Play a composed arrival through the shipping slot, all in one frame, exactly as the doors
    /// opening does. Returns what is left on the screen.</summary>
    private static string? OnScreen(IReadOnlyList<UndergroundComplex.Saying> sayings, double nowMs = 0.0)
    {
        PulseSlot slot = PulseSlot.Empty;
        foreach (UndergroundComplex.Saying s in sayings)
        {
            slot = slot.Write(s.Text, s.Rank, nowMs);
        }
        return slot.Message;
    }

    /// <summary>What the law says SHOULD be on the screen: the highest rank present, and among equals the
    /// last one written. Derived from the list itself rather than from a rank typed in here, so this cannot
    /// quietly agree with a mistake.</summary>
    private static string Expected(IReadOnlyList<UndergroundComplex.Saying> sayings)
    {
        PulseRank top = sayings.Max(s => s.Rank);
        return sayings.Last(s => s.Rank == top).Text;
    }

    /// <summary>One arrival: what the doors opening on <paramref name="toLevel"/> has to say, for a captain
    /// with <paramref name="wallet"/> in their pocket who pressed the button for it (when the panel offers
    /// one) on a floor they have not stood on before. The excursion is FRESH — every beat unspent — which is
    /// the state the bug lives in: it is the first arrival on a floor that has anything to say.</summary>
    private static IReadOnlyList<UndergroundComplex.Saying> Arrival(
        string body, int fromLevel, int toLevel, HashSet<string> wallet,
        IReadOnlyList<Satchel.Item>? carried = null)
    {
        UndergroundComplex.LiftStop? via = null;
        foreach (UndergroundComplex.LiftStop stop in
                 UndergroundComplex.LiftPanel(body, fromLevel, wallet, carried))
        {
            if (stop.Level == toLevel && !stop.IsCurrent && stop.Refusal is null)
            {
                via = stop;
            }
        }

        return UndergroundComplex.ArrivalSayings(
            body, fromLevel, toLevel,
            new UndergroundComplex.ArrivalMemory(WasUnderground: fromLevel < 0),
            via, wallet, carried);
    }

    /// <summary>Every ride the panel actually offers, on every site, both with the wallet and without it —
    /// which is the difference between a gate that opens and a floor that is not on any panel at all.</summary>
    private static IEnumerable<(string Body, int From, int To, HashSet<string> Wallet)> EveryRide()
    {
        foreach (string body in Bodies)
        {
            HashSet<string> whole = WholeWallet(body);
            foreach (HashSet<string> wallet in new[] { new HashSet<string>(), whole })
            {
                foreach (int from in UndergroundComplex.FloorsOf(body).Append(0))
                {
                    foreach (UndergroundComplex.LiftStop stop in
                             UndergroundComplex.LiftPanel(body, from, wallet))
                    {
                        if (stop.IsCurrent || stop.Refusal is not null || stop.Level >= 0)
                        {
                            continue;
                        }
                        yield return (body, from, stop.Level, wallet);
                    }
                }
            }
        }
    }

    // ── The sweep ────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheBiggestSentenceOfEveryArrivalTheGeneratorAdmitsIsTheOneOnScreen()
    {
        // The issue's own ask, verbatim: "for every floor-arrival combination the generator admits, the
        // highest-priority saying is the one on screen."
        var wrong = new List<string>();
        int arrivals = 0, contested = 0;

        foreach ((string body, int from, int to, HashSet<string> wallet) in EveryRide())
        {
            IReadOnlyList<UndergroundComplex.Saying> sayings = Arrival(body, from, to, wallet);
            if (sayings.Count == 0)
            {
                continue;
            }

            arrivals++;
            if (sayings.Select(s => s.Rank).Distinct().Count() > 1)
            {
                contested++;
            }

            string expected = Expected(sayings);
            string? shown = OnScreen(sayings);
            if (shown != expected)
            {
                UndergroundComplex.Saying winner = sayings.First(s => s.Text == shown);
                UndergroundComplex.Saying should = sayings.Last(s => s.Text == expected);
                wrong.Add(
                    $"{body} B{-from}→B{-to}: the screen kept {winner.Beat} ({winner.Rank}) and ate " +
                    $"{should.Beat} ({should.Rank}).");
            }
        }

        // A sweep that met no arrival with two ranks in it would pass for the wrong reason: the law is only
        // load-bearing where something contests the slot.
        Assert.True(arrivals > 200, $"only {arrivals} arrival(s) had anything to say — this proves little.");
        Assert.True(contested > 20,
            $"only {contested} of {arrivals} arrivals had lines of different rank racing for the slot — " +
            "the sweep is not exercising the law it is here to guard.");
        Assert.True(wrong.Count == 0,
            $"{wrong.Count} of {arrivals} arrivals put the wrong sentence on screen:\n  " +
            string.Join("\n  ", wrong.Take(12)));
    }

    [Fact]
    public void TheUnlistedBandsClimaxIsNotEatenByTheRoutineAirLine()
    {
        // #592's case, named. The first words on a floor that does not exist, against "the doors part on
        // warm air and standing lights" — and for two years the fan won.
        var sites = new List<string>();
        foreach (string body in Bodies.Concat(Enumerable.Range(0, 400).Select(i => $"probe-moon-{i}")))
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.IsUnlisted(body, level))
                {
                    sites.Add($"{body}:{level}");
                }
            }
        }
        Assert.True(sites.Count > 20, $"only {sites.Count} unlisted floor(s) in the sweep — this proves little.");

        int checkedFloors = 0;
        foreach (string where in sites)
        {
            string body = where[..where.LastIndexOf(':')];
            int level = int.Parse(where[(where.LastIndexOf(':') + 1)..], System.Globalization.CultureInfo.InvariantCulture);

            // Arrived at from the floor above, on a fresh excursion, with the whole wallet — the ride a
            // captain who has done the hunt actually makes.
            IReadOnlyList<UndergroundComplex.Saying> sayings =
                Arrival(body, UndergroundComplex.BandTop(UndergroundComplex.BandOf(level) - 1), level,
                    WholeWallet(body));
            if (!sayings.Any(s => s.Beat == UndergroundComplex.ArrivalBeat.Unlisted))
            {
                continue;   // reached some other way; the sweep above covers those
            }

            checkedFloors++;
            string? shown = OnScreen(sayings);
            Assert.True(
                shown == sayings.First(s => s.Beat == UndergroundComplex.ArrivalBeat.Unlisted).Text,
                $"{body} B{-level}: the floor that is not on the plan says " +
                $"'{sayings.First(s => s.Beat == UndergroundComplex.ArrivalBeat.Unlisted).Text[..48]}…' and " +
                $"the screen kept '{shown?[..Math.Min(shown.Length, 64)]}…' instead (#592/#693).");
        }

        Assert.True(checkedFloors > 5,
            $"only {checkedFloors} unlisted arrival(s) actually raised the climax — the guard is blind.");
    }

    [Fact]
    public void TheWinnerDoesNotDependOnTheORDERTheSayingsWereWrittenIn()
    {
        // The house bug class this fix exists to kill: "a list built by appending is not a list in order."
        // The whole claim of a priority is that the composing order is free — so every permutation of an
        // arrival's own sayings must leave the same sentence on screen. If this ever fails, the ordering
        // discipline has crept back in wearing a rank.
        int permuted = 0;
        foreach ((string body, int from, int to, HashSet<string> wallet) in EveryRide())
        {
            IReadOnlyList<UndergroundComplex.Saying> sayings = Arrival(body, from, to, wallet);
            if (sayings.Count < 2)
            {
                continue;
            }

            // Among EQUALS the last written wins, which is a real part of the law and not a loophole: the
            // seam is said before the arrival because that is the order they happen in. So the expectation
            // is recomputed per permutation, and what must not change is the RANK on screen.
            PulseRank top = sayings.Max(s => s.Rank);
            foreach (IReadOnlyList<UndergroundComplex.Saying> order in Permutations(sayings))
            {
                permuted++;
                string? shown = OnScreen(order);
                Assert.Equal(top, order.First(s => s.Text == shown).Rank);
            }
        }

        Assert.True(permuted > 100, $"only {permuted} permutation(s) tried — this proves little.");
    }

    [Fact]
    public void TheAuthoredCLIMAXESAreRankedAsClimaxes()
    {
        // The ranks are the other half of the law, and a rank is a judgement about a sentence — so the four
        // that a whole feature was built to say are pinned by name. A future edit that demotes one of these
        // to make room for something routine has to come through here and say so out loud.
        var climaxes = new HashSet<UndergroundComplex.ArrivalBeat>
        {
            UndergroundComplex.ArrivalBeat.Unlisted,
            UndergroundComplex.ArrivalBeat.Seam,
            UndergroundComplex.ArrivalBeat.Found,
        };

        var seen = new HashSet<UndergroundComplex.ArrivalBeat>();
        foreach ((string body, int from, int to, HashSet<string> wallet) in EveryRide())
        {
            foreach (UndergroundComplex.Saying s in Arrival(body, from, to, wallet))
            {
                seen.Add(s.Beat);
                if (climaxes.Contains(s.Beat))
                {
                    Assert.Equal(PulseRank.Climax, s.Rank);
                }
                else if (s.Beat == UndergroundComplex.ArrivalBeat.Air)
                {
                    // The weather. It has never been anything else, and the whole issue is that it was
                    // winning anyway.
                    Assert.Equal(PulseRank.Status, s.Rank);
                }
            }
        }

        foreach (UndergroundComplex.ArrivalBeat beat in climaxes)
        {
            Assert.Contains(beat, seen);
        }
        Assert.Contains(UndergroundComplex.ArrivalBeat.Air, seen);
    }

    // ── The hold is a breath, not a lockout ──────────────────────────────────────────────────────────────

    [Fact]
    public void ALesserLineIsRefusedInTheSameBreathAndAcceptedAfterIt()
    {
        // The hold has to be SHORT. A climax can dwell eight seconds, and eight seconds in which a pressed
        // button answers nothing on screen is #686's bug wearing this fix as a disguise — the lines that race
        // for the slot race in the same frame or the tick after it, and nothing else should ever be refused.
        PulseSlot slot = PulseSlot.Empty.Write(UndergroundComplex.SeamLine, PulseRank.Climax, 0.0);

        Assert.Equal(UndergroundComplex.SeamLine, slot.Write("Vent recharging…", PulseRank.Status, 0.0).Message);
        Assert.Equal(UndergroundComplex.SeamLine,
            slot.Write("Vent recharging…", PulseRank.Status, PulseSlot.MinDwellMs - 1).Message);

        // …and a breath later the instruments have the screen back, while the climax is still readable in
        // the book and would still have been readable on screen had nobody pressed anything.
        Assert.Equal("Vent recharging…",
            slot.Write("Vent recharging…", PulseRank.Status, PulseSlot.MinDwellMs + 1).Message);
        Assert.True(slot.ExpiresMs > PulseSlot.MinDwellMs, "the hold is the whole dwell — that is a lockout.");
    }

    [Fact]
    public void AnEmptySlotOutranksNothing()
    {
        // The hold must not leak into the next scene: once a line has faded there is nothing left to protect,
        // whatever rank it had.
        PulseSlot slot = PulseSlot.Empty
            .Write(UndergroundComplex.FoundArrivalLine, PulseRank.Climax, 0.0)
            .Expire(PulseSlot.MaxDwellMs + 1.0);

        Assert.Null(slot.Message);
        Assert.Equal("Vent recharging…", slot.Write("Vent recharging…", PulseRank.Status, 0.0).Message);
    }

    [Fact]
    public void RidingToTheSurfaceSaysNothingHereBecauseTheSHEDIsTheClientsOwnSentence()
    {
        // Coming up is the shed, the moon and what you are carrying out of it — one line composed out of a
        // satchel Core does not have. Pinned so nobody adds a second answer to it here.
        Assert.Empty(UndergroundComplex.ArrivalSayings(
            "miranda", -1, 0, new UndergroundComplex.ArrivalMemory(WasUnderground: true), null, []));
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every ordering of a short list. Arrivals top out at four sayings, so this is 24 at worst.</summary>
    private static IEnumerable<IReadOnlyList<UndergroundComplex.Saying>> Permutations(
        IReadOnlyList<UndergroundComplex.Saying> items)
    {
        if (items.Count <= 1)
        {
            yield return items;
            yield break;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var rest = new List<UndergroundComplex.Saying>(items);
            UndergroundComplex.Saying head = rest[i];
            rest.RemoveAt(i);
            foreach (IReadOnlyList<UndergroundComplex.Saying> tail in Permutations(rest))
            {
                var one = new List<UndergroundComplex.Saying> { head };
                one.AddRange(tail);
                yield return one;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #768 · THE ARRIVAL THAT RAISES A CARD EATS ITS OWN LINE.
///
/// <para>Owner's residual off #693/PR #766, verbatim: <i>"on a from-the-surface ride straight through a gate,
/// #585's first-descent CARD raises on the same arrival, and the gate-accepted pulse line plays UNDER its
/// backdrop."</i> <see cref="PulseRank"/> cannot help — this is not a pulse losing to a pulse, it is a pulse
/// losing to the whole HUD, and by the time the card has been read the dwell has run out and the sentence is
/// gone. #680's family, arising from an ARRIVAL rather than from a press on a pop-up.</para>
///
/// <para>The fix is <see cref="PulseHold"/>: an event that raises a card holds its ranked sayings and the
/// card's dismissal plays the winner. These guards pin three things — the issue's own case survives; the held
/// winner is the SAME sentence the slot would have kept, so the hold is the pulse law and not a second one;
/// and a released line is an ordinary line, with an ordinary dwell, that anything may outrank afterwards.</para>
///
/// <para><b>Red proof.</b> Make <see cref="PulseHold.Hold"/> a no-op (<c>return this;</c>) — which is exactly
/// the shipped behaviour, nothing surviving the card — and every guard below except
/// <see cref="TheOldRoadLosesTheLineWhichIsWhyTheHoldExists"/> goes red, the first one naming the site and the
/// band. Take the rank clause out of it instead and the CLIMAX/Status guards go red on their own.</para>
/// </summary>
public sealed class TheHeldSayingOutlivesTheCardTests
{
    /// <summary>The scenario's own sites plus the cheat rocks — without the last three the sweep audits a
    /// universe where the deep bands, the floor nobody listed and the halls do not exist, and passes for the
    /// wrong reason (the house's fifth named bug class).</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted", UndergroundComplex.FoundBandCheatSiteId,
        KaamosLore.IceMoonBodyId,
    ];

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

    /// <summary>What the arrival puts on the screen the OLD way: every saying written into the slot as it is
    /// composed. #766's law, unchanged and still right — for an arrival that raises nothing.</summary>
    private static PulseSlot Pulsed(IReadOnlyList<UndergroundComplex.Saying> sayings, double nowMs = 0.0)
    {
        PulseSlot slot = PulseSlot.Empty;
        foreach (UndergroundComplex.Saying s in sayings)
        {
            slot = slot.Write(s.Text, s.Rank, nowMs);
        }
        return slot;
    }

    /// <summary>What the arrival puts on the screen through the HOLD: every saying kept back while the card
    /// is up, and the winner released when it comes down at <paramref name="dismissedAtMs"/>.</summary>
    private static PulseSlot HeldThenReleased(
        IReadOnlyList<UndergroundComplex.Saying> sayings, double dismissedAtMs)
    {
        PulseHold held = PulseHold.Empty;
        foreach (UndergroundComplex.Saying s in sayings)
        {
            held = held.Hold(s.Text, s.Rank);
        }

        // The card was up for all of it, so the slot the release writes into is whatever the world had — and
        // on an arrival that is nothing, expired long ago behind the backdrop.
        (PulseSlot slot, PulseHold left) = held.ReleaseInto(PulseSlot.Empty, dismissedAtMs);
        Assert.False(left.Any, "the hold still has something in it after a release — it leaks into the next card.");
        return slot;
    }

    /// <summary>How long a card takes to read, expressed as the one number the pulse itself owns: LONGER than
    /// the longest a line is ever on screen. Past this, a line pulsed under a backdrop is not merely
    /// unreadable — it is gone, and no dismissal brings it back.</summary>
    private const double CardReadMs = PulseSlot.MaxDwellMs + 1.0;

    /// <summary>The issue's own ride: from the SURFACE, straight through a gate the wallet opens, on a fresh
    /// excursion — which is the state the first-descent card (#585) is raised in, because the card's own
    /// condition is "the car started above ground and this excursion has stood on no floor yet", and the
    /// arrival says <see cref="UndergroundComplex.ArrivalBeat.Descending"/> for exactly the first half of
    /// that. Every site that offers such a ride, so this is not one hand-built world.</summary>
    private static IEnumerable<(string Body, int To, IReadOnlyList<UndergroundComplex.Saying> Sayings)>
        EveryRideFromTheSurfaceThroughAGate()
    {
        foreach (string body in Bodies)
        {
            HashSet<string> wallet = WholeWallet(body);
            foreach (UndergroundComplex.LiftStop stop in UndergroundComplex.LiftPanel(body, 0, wallet))
            {
                if (stop.IsCurrent || stop.Refusal is not null || stop.Level >= 0 || stop.OpenedBy is null)
                {
                    continue;
                }

                IReadOnlyList<UndergroundComplex.Saying> sayings = UndergroundComplex.ArrivalSayings(
                    body, 0, stop.Level,
                    new UndergroundComplex.ArrivalMemory(WasUnderground: false),
                    stop, wallet);
                if (sayings.Any(s => s.Beat == UndergroundComplex.ArrivalBeat.CardAccepted))
                {
                    yield return (body, stop.Level, sayings);
                }
            }
        }
    }

    // ── The issue's case ─────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheGateAcceptedBeatIsOnThePulseAfterTheFirstDescentCardIsDismissed()
    {
        var lost = new List<string>();
        int rides = 0, gateWins = 0;

        foreach ((string body, int to, IReadOnlyList<UndergroundComplex.Saying> sayings) in
                 EveryRideFromTheSurfaceThroughAGate())
        {
            rides++;

            // The card's own condition, asserted rather than assumed: the car came from above ground, which
            // is the saying that only a from-the-surface arrival has. A world where this ride were somehow
            // underground would not raise the card at all and would prove nothing about the bug.
            Assert.Contains(sayings, s => s.Beat == UndergroundComplex.ArrivalBeat.Descending);

            // What the arrival is owed is its BIGGEST sentence, which on a handful of these rides is not the
            // gate at all — europa's carded descent lands straight on the floor nobody listed, and #592's
            // climax outranks the paper that opened the door to it. The bug is that neither was ever heard.
            PulseRank top = sayings.Max(s => s.Rank);
            string owed = sayings.Last(s => s.Rank == top).Text;
            bool gateIsTheWinner =
                sayings.Last(s => s.Rank == top).Beat == UndergroundComplex.ArrivalBeat.CardAccepted;
            if (gateIsTheWinner)
            {
                gateWins++;
            }

            PulseSlot after = HeldThenReleased(sayings, CardReadMs).Expire(CardReadMs);
            if (after.Message != owed)
            {
                string kept = after.Message is { } m ? $"'{m[..Math.Min(m.Length, 40)]}…'" : "nothing at all";
                lost.Add($"{body} SURFACE→B{-to}: the captain closes the card and the pulse says {kept} — " +
                    $"the arrival's own {top} said '{owed[..Math.Min(owed.Length, 40)]}…' under the " +
                    "backdrop and was never heard (#768).");
            }
        }

        // A ride from the surface straight through a gate is the whole subject of the issue, and on most of
        // them the gate IS the biggest thing that happened — which is the case the owner filed. If no site
        // offered one, this guard would be an assertion about an empty list.
        Assert.True(rides > 5,
            $"only {rides} from-the-surface carded ride(s) in the sweep — the guard is looking at nothing.");
        Assert.True(gateWins > 5,
            $"only {gateWins} of {rides} carded descents have the GATE as their biggest saying — the issue's " +
            "own case is barely in this sweep.");
        Assert.True(lost.Count == 0,
            $"{lost.Count} of {rides} carded descents lost their beat to their own card:\n  " +
            string.Join("\n  ", lost.Take(8)));
    }

    [Fact]
    public void TheOldRoadLosesTheLineWhichIsWhyTheHoldExists()
    {
        // The bug, written down as arithmetic so nobody has to take the issue's word for it. The arrival
        // pulses; the card goes up over it; the captain reads the card for longer than any pulse is ever on
        // screen — and there is nothing left when the backdrop comes down.
        int checkedRides = 0;
        foreach ((string _, int _, IReadOnlyList<UndergroundComplex.Saying> sayings) in
                 EveryRideFromTheSurfaceThroughAGate())
        {
            checkedRides++;
            Assert.Null(Pulsed(sayings).Expire(CardReadMs).Message);
        }

        Assert.True(checkedRides > 5, $"only {checkedRides} ride(s) — this proves little.");
    }

    // ── The hold is the pulse law, not a second one ──────────────────────────────────────────────────────

    [Fact]
    public void TheHeldWinnerIsTheSAMESentenceTheSlotWouldHaveKept()
    {
        // The claim the whole fix rests on: deferring changes WHEN a line is said and never WHICH. If these
        // two ever disagree there are two laws about the one slot, which is the state #693 was filed against.
        var disagreed = new List<string>();
        int arrivals = 0, contested = 0;

        foreach ((string body, int from, int to, HashSet<string> wallet) in EveryRide())
        {
            IReadOnlyList<UndergroundComplex.Saying> sayings = UndergroundComplex.ArrivalSayings(
                body, from, to, new UndergroundComplex.ArrivalMemory(WasUnderground: from < 0),
                Via(body, from, to, wallet), wallet);
            if (sayings.Count == 0)
            {
                continue;
            }

            arrivals++;
            if (sayings.Select(s => s.Rank).Distinct().Count() > 1)
            {
                contested++;
            }

            string? pulsed = Pulsed(sayings).Message;
            string? released = HeldThenReleased(sayings, 0.0).Message;
            if (pulsed != released)
            {
                disagreed.Add($"{body} B{-from}→B{-to}: the slot keeps '{Snip(pulsed)}…' and the hold " +
                    $"releases '{Snip(released)}…'.");
            }
        }

        Assert.True(arrivals > 200, $"only {arrivals} arrival(s) had anything to say — this proves little.");
        Assert.True(contested > 20,
            $"only {contested} of {arrivals} arrivals had lines of different rank racing — the sweep is not " +
            "exercising the law it is here to guard.");
        Assert.True(disagreed.Count == 0,
            $"{disagreed.Count} arrival(s) hold a different sentence than they would have pulsed:\n  " +
            string.Join("\n  ", disagreed.Take(8)));
    }

    [Fact]
    public void AHeldCLIMAXIsNotDisplacedByAHeldStatusHOWEVERLateItIsHeld()
    {
        // The owner's own case, one layer down: the biggest sentence in the feature, held behind a card
        // alongside the weather. There is no clock inside a breath, so unlike the slot's hold there is no
        // "and a moment later the instruments get it back" — a lesser line composed in the same arrival never
        // wins, whenever it was appended.
        string climax = TheUnlistedFloorsOwnWords();
        PulseHold held = PulseHold.Empty
            .Hold(climax, PulseRank.Climax)
            .Hold(UndergroundComplex.PressurisedLine, PulseRank.Status)
            .Hold(UndergroundComplex.DeadAirLine, PulseRank.Status);

        (PulseSlot slot, PulseHold _) = held.ReleaseInto(PulseSlot.Empty, 0.0);
        Assert.Equal(PulseRank.Climax, held.Rank);
        Assert.Equal(climax, slot.Message);

        // …and the other way round, because a law that only refuses is a law that never lets anything past:
        // the climax held LAST still takes the slot from the weather held first.
        (PulseSlot other, PulseHold _) = PulseHold.Empty
            .Hold(UndergroundComplex.PressurisedLine, PulseRank.Status)
            .Hold(UndergroundComplex.SeamLine, PulseRank.Climax)
            .ReleaseInto(PulseSlot.Empty, 0.0);
        Assert.Equal(UndergroundComplex.SeamLine, other.Message);

        // Among EQUALS the last held wins — the slot's own tie-break, so the reading order an author wrote
        // two climaxes in is still the order that decides (#677's seam then arrival).
        (PulseSlot tied, PulseHold _) = PulseHold.Empty
            .Hold(UndergroundComplex.SeamLine, PulseRank.Climax)
            .Hold(UndergroundComplex.FoundArrivalLine, PulseRank.Climax)
            .ReleaseInto(PulseSlot.Empty, 0.0);
        Assert.Equal(UndergroundComplex.FoundArrivalLine, tied.Message);
    }

    // ── A released line is an ordinary line ──────────────────────────────────────────────────────────────

    [Fact]
    public void AReleasedLineTakesItsOrdinaryDwellAndMayBeOutrankedAfterwards()
    {
        // #766's dwell semantics, which the deferral must not quietly rewrite: the freed line is written
        // through the shipping slot, so it lingers exactly as long as its length earns and is held against
        // lesser lines for exactly one breath — never for the length of the card that delayed it.
        const double dismissed = 12_345.0;
        (PulseSlot slot, PulseHold _) = PulseHold.Empty
            .Hold(UndergroundComplex.SeamLine, PulseRank.Climax)
            .ReleaseInto(PulseSlot.Empty, dismissed);

        Assert.Equal(dismissed + PulseSlot.DwellFor(UndergroundComplex.SeamLine), slot.ExpiresMs);
        Assert.Equal(dismissed + PulseSlot.MinDwellMs, slot.HeldUntilMs);
        Assert.Equal(UndergroundComplex.SeamLine,
            slot.Write("Vent recharging…", PulseRank.Status, dismissed).Message);
        Assert.Equal("Vent recharging…",
            slot.Write("Vent recharging…", PulseRank.Status, dismissed + PulseSlot.MinDwellMs + 1).Message);
    }

    [Fact]
    public void NothingHeldIsNotAnEventAndAnEmptySayingIsNotOne()
    {
        // A card dismissed on a quiet screen must not blank or re-write whatever the world has said since —
        // the release is a no-op, not a write of null.
        PulseSlot standing = PulseSlot.Empty.Write("Vent recharging…", PulseRank.Status, 0.0);
        (PulseSlot after, PulseHold held) = PulseHold.Empty.ReleaseInto(standing, 500.0);
        Assert.Equal(standing, after);
        Assert.False(held.Any);

        // And an empty saying never displaces a real one that is waiting — the same refusal FileNote makes.
        Assert.Equal(UndergroundComplex.SeamLine,
            PulseHold.Empty.Hold(UndergroundComplex.SeamLine, PulseRank.Climax).Hold("  ", PulseRank.Climax)
                .Message);
        Assert.False(PulseHold.Empty.Hold("", PulseRank.Beat).Any);
    }

    [Fact]
    public void TheORDERTheSayingsWereHeldInDoesNotDecideTheWinnersRANK()
    {
        // The house bug class, asked of the hold: "a list built by appending is not a list in order." An
        // arrival composes its sayings in whatever order reads best, and the sentence that survives the card
        // must not depend on it.
        int permuted = 0;
        foreach ((string body, int from, int to, HashSet<string> wallet) in EveryRide())
        {
            IReadOnlyList<UndergroundComplex.Saying> sayings = UndergroundComplex.ArrivalSayings(
                body, from, to, new UndergroundComplex.ArrivalMemory(WasUnderground: from < 0),
                Via(body, from, to, wallet), wallet);
            if (sayings.Count < 2)
            {
                continue;
            }

            PulseRank top = sayings.Max(s => s.Rank);
            foreach (IReadOnlyList<UndergroundComplex.Saying> order in Permutations(sayings))
            {
                permuted++;
                string? shown = HeldThenReleased(order, 0.0).Message;
                Assert.Equal(top, order.First(s => s.Text == shown).Rank);
            }
        }

        Assert.True(permuted > 100, $"only {permuted} permutation(s) tried — this proves little.");
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#592's climax, taken from a site that really has a floor nobody listed rather than typed in
    /// here — a sentence composed for this test would be a sentence no arrival ever says.</summary>
    private static string TheUnlistedFloorsOwnWords()
    {
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!UndergroundComplex.IsUnlisted(body, level))
                {
                    continue;
                }
                UndergroundComplex.Saying said = UndergroundComplex.ArrivalSayings(
                        body, UndergroundComplex.BandTop(UndergroundComplex.BandOf(level) - 1), level,
                        new UndergroundComplex.ArrivalMemory(WasUnderground: true), null, WholeWallet(body))
                    .First(s => s.Beat == UndergroundComplex.ArrivalBeat.Unlisted);
                return said.Text;
            }
        }

        throw new InvalidOperationException(
            "no site in the sweep has a floor the lobby does not list — #592's climax cannot be reached, " +
            "so this guard would be asserting about a world the game does not build.");
    }

    private static string Snip(string? line) =>
        line is null ? "nothing at all" : line[..Math.Min(line.Length, 24)];

    private static UndergroundComplex.LiftStop? Via(
        string body, int from, int to, IReadOnlyCollection<string> wallet)
    {
        UndergroundComplex.LiftStop? via = null;
        foreach (UndergroundComplex.LiftStop stop in UndergroundComplex.LiftPanel(body, from, wallet))
        {
            if (stop.Level == to && !stop.IsCurrent && stop.Refusal is null)
            {
                via = stop;
            }
        }
        return via;
    }

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

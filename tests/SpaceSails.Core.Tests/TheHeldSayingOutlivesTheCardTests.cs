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
///
/// <para><b>#1230 · the hold became a QUEUE, and three of these guards were re-grounded on it.</b> Nothing
/// here got weaker: what changed is that the questions they could ask got bigger. The hold used to keep ONE
/// sentence, so "the winner is the same sentence the slot would have kept" was a text comparison; a queue
/// keeps them all, so the thing that must still agree with the slot is the RANK — the biggest thing that
/// happened is the first thing the captain reads — and what used to be silently dropped is now asserted to be
/// SAID (<see cref="EverySayingIsEventuallySaidAndNoneTwice"/>). The tie-break moved for a reason rather than
/// by accident: "the last held wins" was the SLOT's rule, and it was the slot's rule because a later write
/// physically overwrote an earlier one. A queue overwrites nothing, so two climaxes are read in the order
/// their author composed them. The law's own five clauses and their reverts live in
/// <c>TheHoldIsAQueueTests</c>.</para>
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

    /// <summary>An arrival composes its sayings IN ONE BREATH, so every one of them is raised on the same
    /// frame — which is the only situation #1230's law lets rank re-order at all.</summary>
    private const double OneBreath = 0.0;

    /// <summary>What the arrival holds while the card is up: every saying kept back, and #1230 drops none of
    /// them.</summary>
    private static PulseHold TheWholeBreath(IReadOnlyList<UndergroundComplex.Saying> sayings)
    {
        PulseHold held = PulseHold.Empty;
        foreach (UndergroundComplex.Saying s in sayings)
        {
            held = held.Hold(s.Text, s.Rank, OneBreath);
        }
        return held;
    }

    /// <summary>What the arrival puts on the screen FIRST through the hold: every saying kept back while the
    /// card is up, and the head of the queue released when it comes down at <paramref name="dismissedAtMs"/>.
    ///
    /// <para>#1230 · the others are not gone any more — they are still waiting, and the frame check drips them
    /// out as the slot frees up. This helper is about the FIRST sentence the captain reads, which is the whole
    /// of what #768's guards were ever about.</para></summary>
    private static PulseSlot HeldThenReleased(
        IReadOnlyList<UndergroundComplex.Saying> sayings, double dismissedAtMs)
    {
        PulseHold held = TheWholeBreath(sayings);
        int waiting = held.Count;

        // The card was up for all of it, so the slot the release writes into is whatever the world had — and
        // on an arrival that is nothing, expired long ago behind the backdrop.
        (PulseSlot slot, PulseHold left) = held.ReleaseInto(PulseSlot.Empty, dismissedAtMs);
        Assert.Equal(Math.Max(waiting - 1, 0), left.Count);
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
        int rides = 0, gateAfterTheHead = 0;

        foreach ((string body, int to, IReadOnlyList<UndergroundComplex.Saying> sayings) in
                 EveryRideFromTheSurfaceThroughAGate())
        {
            rides++;

            // The card's own condition, asserted rather than assumed: the car came from above ground, which
            // is the saying that only a from-the-surface arrival has. A world where this ride were somehow
            // underground would not raise the card at all and would prove nothing about the bug.
            Assert.Contains(sayings, s => s.Beat == UndergroundComplex.ArrivalBeat.Descending);

            // What the arrival is owed FIRST is its BIGGEST sentence, which on a handful of these rides is not
            // the gate at all — europa's carded descent lands straight on the floor nobody listed, and #592's
            // climax outranks the paper that opened the door to it. The bug is that neither was ever heard.
            //
            // #1230 · FIRST rather than Last among equals. The old slot's tie-break was "last written wins",
            // because a later write physically overwrote an earlier one; a queue has nothing to overwrite, so
            // among lines of the same rank raised in the same breath the order the author composed them in is
            // the order they are said. Rank only ever ORDERS now — it never drops one.
            PulseRank top = sayings.Max(s => s.Rank);
            string owed = sayings.First(s => s.Rank == top).Text;
            string gate = sayings.First(s => s.Beat == UndergroundComplex.ArrivalBeat.CardAccepted).Text;

            PulseSlot after = HeldThenReleased(sayings, CardReadMs).Expire(CardReadMs);
            if (after.Message != owed)
            {
                string kept = after.Message is { } m ? $"'{m[..Math.Min(m.Length, 40)]}…'" : "nothing at all";
                lost.Add($"{body} SURFACE→B{-to}: the captain closes the card and the pulse says {kept} — " +
                    $"the arrival's own {top} said '{owed[..Math.Min(owed.Length, 40)]}…' under the " +
                    "backdrop and was never heard (#768).");
                continue;
            }

            // #1230 · AND THE ISSUE'S OWN SENTENCE IS NOW HEARD ON EVERY ONE OF THESE RIDES, not only on the
            // ones where it happens to be the biggest thing that happened. On these ten it never is: the
            // descent's own line is composed ahead of it at the same rank, so under the one-slot hold the
            // gate — *the paper opened the door* — was thrown away on every carded descent in the game.
            List<string> said = DrainedOntoTheGlass(TheWholeBreath(sayings));
            if (!said.Contains(gate))
            {
                lost.Add($"{body} SURFACE→B{-to}: the gate-accepted beat is never said at all — the card " +
                    $"ate it (#768/#1230). What the captain read: '{Snip(said.FirstOrDefault())}…'.");
                continue;
            }

            if (said[0] != gate)
            {
                gateAfterTheHead++;
            }
        }

        // A ride from the surface straight through a gate is the whole subject of the issue. If no site
        // offered one, this guard would be an assertion about an empty list.
        Assert.True(rides > 5,
            $"only {rides} from-the-surface carded ride(s) in the sweep — the guard is looking at nothing.");

        // …and on these rides the gate is never the FIRST thing said, which is exactly why the queue matters
        // here: a guard that only ever watched the head of the queue would pass on a hold that still threw
        // the issue's own sentence away.
        Assert.True(gateAfterTheHead > 5,
            $"only {gateAfterTheHead} of {rides} carded descents say the gate beat behind another line — if " +
            "the gate were always the head, this sweep would prove nothing about the queue.");
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
    public void TheFirstSentenceSaidIsTheRANKTheSlotWouldHaveKept()
    {
        // The claim the fix rests on, as #1230 leaves it: deferring changes WHEN a line is said and never what
        // KIND of line comes first. The old form of this guard compared the TEXT, and could, because the hold
        // kept exactly one sentence; a queue keeps them all, so what has to agree with the slot is the rank —
        // the biggest thing that happened is still the first thing the captain reads. If these two ever
        // disagree there are two laws about the one slot, which is the state #693 was filed against.
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

            PulseSlot pulsed = Pulsed(sayings);
            PulseSlot released = HeldThenReleased(sayings, 0.0);
            if (pulsed.Rank != released.Rank)
            {
                disagreed.Add($"{body} B{-from}→B{-to}: the slot keeps a {pulsed.Rank} " +
                    $"('{Snip(pulsed.Message)}…') and the hold says a {released.Rank} first " +
                    $"('{Snip(released.Message)}…').");
            }

            // …and the sentence it says first is one the arrival actually composed, at that top rank — never
            // a line the queue invented, re-ordered across a rank, or carried in from a previous arrival.
            Assert.Contains(sayings, s => s.Text == released.Message && s.Rank == released.Rank);
        }

        Assert.True(arrivals > 200, $"only {arrivals} arrival(s) had anything to say — this proves little.");
        Assert.True(contested > 20,
            $"only {contested} of {arrivals} arrivals had lines of different rank racing — the sweep is not " +
            "exercising the law it is here to guard.");
        Assert.True(disagreed.Count == 0,
            $"{disagreed.Count} arrival(s) say a different KIND of line first than they would have pulsed:\n  " +
            string.Join("\n  ", disagreed.Take(8)));
    }

    [Fact]
    public void EverySayingIsEventuallySaidAndNoneTwice()
    {
        // #1230's own half, swept over the same world: the hold is a QUEUE, so the weather composed in the
        // same breath as the climax is no longer annihilated by it — it is said afterwards. Every saying, once
        // each, in the queue's own order, and the queue ends empty.
        int arrivals = 0, multi = 0;

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
            if (sayings.Count > 1)
            {
                multi++;
            }

            List<string> said = DrainedOntoTheGlass(TheWholeBreath(sayings));

            // One breath composes no sentence twice, so every one of them must come out — and each exactly
            // once, which is what a queue that leaks or re-says its head would fail.
            Assert.Equal(sayings.Select(s => s.Text).Distinct().Count(), said.Count);
            foreach (UndergroundComplex.Saying s in sayings)
            {
                Assert.Single(said, line => line == s.Text);
            }

            // …and in rank order, highest first, because the whole breath was raised on one frame.
            var ranks = said.Select(line => sayings.First(s => s.Text == line).Rank).ToList();
            Assert.Equal(ranks.OrderByDescending(r => r).ToList(), ranks);
        }

        Assert.True(arrivals > 200, $"only {arrivals} arrival(s) had anything to say — this proves little.");
        Assert.True(multi > 50,
            $"only {multi} of {arrivals} arrivals composed more than one saying — the sweep never exercises " +
            "a queue with anything in it behind the head.");
    }

    [Fact]
    public void AHeldCLIMAXIsSaidFirstAndTheWeatherBehindItIsNoLongerLost()
    {
        // The owner's own case, one layer down: the biggest sentence in the feature, held behind a card
        // alongside the weather. There is no clock inside a breath, so rank orders the three — and, since
        // #1230, orders is ALL it does: the two status lines follow the climax instead of dying under it.
        string climax = TheUnlistedFloorsOwnWords();
        PulseHold held = PulseHold.Empty
            .Hold(climax, PulseRank.Climax, OneBreath)
            .Hold(UndergroundComplex.PressurisedLine, PulseRank.Status, OneBreath)
            .Hold(UndergroundComplex.DeadAirLine, PulseRank.Status, OneBreath);

        (PulseSlot slot, PulseHold _) = held.ReleaseInto(PulseSlot.Empty, 0.0);
        Assert.Equal(PulseRank.Climax, held.Rank);
        Assert.Equal(climax, slot.Message);
        Assert.Equal(
            [climax, UndergroundComplex.PressurisedLine, UndergroundComplex.DeadAirLine],
            DrainedOntoTheGlass(held));

        // …and the other way round, because a law that only orders forwards is no law: the climax held LAST
        // still goes in FRONT of the weather held first, when both were raised in the one breath.
        PulseHold late = PulseHold.Empty
            .Hold(UndergroundComplex.PressurisedLine, PulseRank.Status, OneBreath)
            .Hold(UndergroundComplex.SeamLine, PulseRank.Climax, OneBreath);
        (PulseSlot other, PulseHold _) = late.ReleaseInto(PulseSlot.Empty, 0.0);
        Assert.Equal(UndergroundComplex.SeamLine, other.Message);
        Assert.Equal([UndergroundComplex.SeamLine, UndergroundComplex.PressurisedLine],
            DrainedOntoTheGlass(late));

        // Among EQUALS the order composed is the order said. #768's tie-break was "the last held wins",
        // because a later write physically overwrote an earlier one in the slot; a queue overwrites nothing,
        // so the reading order an author wrote two climaxes in survives intact (#677's seam then arrival).
        PulseHold tied = PulseHold.Empty
            .Hold(UndergroundComplex.SeamLine, PulseRank.Climax, OneBreath)
            .Hold(UndergroundComplex.FoundArrivalLine, PulseRank.Climax, OneBreath);
        Assert.Equal([UndergroundComplex.SeamLine, UndergroundComplex.FoundArrivalLine],
            DrainedOntoTheGlass(tied));
    }

    /// <summary>#1230 · every sentence a hold is carrying, said onto a real slot in the order and at the pace
    /// the law gives them: one at a time, the next only once the one before it has had its whole dwell. The
    /// clock is stepped by the pulse's own dwell rather than by a number chosen here, so this helper cannot
    /// disagree with <see cref="PulseSlot.DwellFor"/> about when the glass is free.</summary>
    private static List<string> DrainedOntoTheGlass(PulseHold held)
    {
        var said = new List<string>();
        PulseSlot slot = PulseSlot.Empty;
        double nowMs = 0.0;

        for (int guard = 0; held.Any && guard <= PulseHold.TheBound + 2; guard++)
        {
            (PulseSlot next, PulseHold left) = held.ReleaseInto(slot, nowMs);
            Assert.NotEqual(held.Count, left.Count);   // a drain that made no progress would loop for ever
            said.Add(next.Message!);
            slot = next;
            held = left;
            nowMs = next.ExpiresMs;
        }

        Assert.False(held.Any, "the queue would not drain — something is stuck at its head.");
        return said;
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
            .Hold(UndergroundComplex.SeamLine, PulseRank.Climax, OneBreath)
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
            PulseHold.Empty.Hold(UndergroundComplex.SeamLine, PulseRank.Climax, OneBreath)
                .Hold("  ", PulseRank.Climax, OneBreath)
                .Message);
        Assert.False(PulseHold.Empty.Hold("", PulseRank.Beat, OneBreath).Any);
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

                // #1230 · and the SET of sentences the captain ends up reading does not depend on it either:
                // the order composed decides the reading order inside a rank and nothing else at all.
                Assert.Equal(
                    sayings.Select(s => s.Text).OrderBy(t => t, StringComparer.Ordinal).ToList(),
                    DrainedOntoTheGlass(TheWholeBreath(order)).OrderBy(t => t, StringComparer.Ordinal).ToList());
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

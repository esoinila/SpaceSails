using SpaceSails.Core.Interior;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Issue #410 (owner 2026-07-20, "Are the contacts moving and not in same seats in same bars?"): the
/// four bar regulars are no longer one shared roster at fixed chairs. <see cref="PatronRota"/> roves
/// them — present at a port only some watches, in a different seat when they are, biased per place —
/// the same "people cannot be static furniture" ruling that already sends the Magpie roaming. The rota
/// resolution is pure Core (repo agreement §9), so it is pinned here without a browser: determinism,
/// not-everyone-every-bar, seats-vary-between-visits, distinct seats, and the per-place skews.
/// </summary>
public class PatronRotaTests
{
    private const int Seats = 7; // HavenInterior.PatronSeatCount — the bar's chair pool

    // ---- Rota determinism (same clock, same answer) -----------------------------------------------

    [Fact]
    public void Resolve_IsDeterministic_SameStationClockSameState()
    {
        foreach (string regular in PatronRota.Roster)
        {
            for (double t = 0; t < 5 * PatronRota.WatchSeconds; t += PatronRota.WatchSeconds / 3)
            {
                Assert.Equal(PatronRota.Resolve(regular, "ringside-exchange", t),
                             PatronRota.Resolve(regular, "ringside-exchange", t));
            }
        }
    }

    [Fact]
    public void Resolve_HoldsForAWholeWatch_ThenMayReRoll()
    {
        // A regular's state is constant across a single watch (the seed is the watch index).
        foreach (string regular in PatronRota.Roster)
        {
            PatronState atStart = PatronRota.Resolve(regular, "the-tilt", 0);
            Assert.Equal(atStart, PatronRota.Resolve(regular, "the-tilt", PatronRota.WatchSeconds - 1));
        }
    }

    [Fact]
    public void ResolveSeating_IsDeterministic()
    {
        for (double t = 0; t < 10 * PatronRota.WatchSeconds; t += PatronRota.WatchSeconds)
        {
            var a = PatronRota.ResolveSeating("cinder-roost", t, Seats);
            var b = PatronRota.ResolveSeating("cinder-roost", t, Seats);
            Assert.Equal(a, b);
        }
    }

    // ---- Not everyone, every bar ------------------------------------------------------------------

    [Fact]
    public void Presence_IsNotEveryoneEveryWatch()
    {
        // Across many watches at one bar, the room is sometimes short of the full four — empty chairs
        // happen (the whole point of the issue). It's also not permanently empty.
        int watchesWithAbsentee = 0, watchesWithSomeone = 0;
        for (int w = 0; w < 200; w++)
        {
            double t = w * PatronRota.WatchSeconds;
            var seated = PatronRota.ResolveSeating("ringside-exchange", t, Seats);
            int present = seated.Count(s => s.Present);
            if (present < PatronRota.Roster.Count) { watchesWithAbsentee++; }
            if (present > 0) { watchesWithSomeone++; }
        }
        Assert.True(watchesWithAbsentee > 0, "expected watches where at least one regular is away");
        Assert.True(watchesWithSomeone > 100, "expected the bar to be peopled most watches");
    }

    [Fact]
    public void EachRegular_IsSometimesPresent_AndSometimesAway_AtADefaultPort()
    {
        // At a port with no authored skew, every regular both shows and drifts off over time.
        foreach (string regular in PatronRota.Roster)
        {
            bool sawPresent = false, sawAway = false;
            for (int w = 0; w < 200; w++)
            {
                if (PatronRota.Resolve(regular, "the-tilt", w * PatronRota.WatchSeconds) == PatronState.AtBar)
                {
                    sawPresent = true;
                }
                else
                {
                    sawAway = true;
                }
            }
            Assert.True(sawPresent, $"{regular} was never present");
            Assert.True(sawAway, $"{regular} was never away");
        }
    }

    // ---- Seats vary between visits, and never collide ---------------------------------------------

    [Fact]
    public void Seats_VaryBetweenWatches_ForAReturningRegular()
    {
        // The Fixer haunts Cinder Roost (high affinity), so they're present most watches — and across
        // those visits they are not always nailed to the same chair.
        var seatsSeen = new HashSet<int>();
        for (int w = 0; w < 60; w++)
        {
            var seated = PatronRota.ResolveSeating("cinder-roost", w * PatronRota.WatchSeconds, Seats);
            PatronSeating fixer = seated.Single(s => s.Regular == "THE FIXER");
            if (fixer.Present) { seatsSeen.Add(fixer.SeatIndex); }
        }
        Assert.True(seatsSeen.Count > 1, "a regular should sit in more than one chair across visits");
    }

    [Fact]
    public void PresentRegulars_TakeDistinctSeatsInRange()
    {
        for (int w = 0; w < 300; w++)
        {
            var seated = PatronRota.ResolveSeating("selene-gate", w * PatronRota.WatchSeconds, Seats);
            var occupied = seated.Where(s => s.Present).Select(s => s.SeatIndex).ToList();
            Assert.Equal(occupied.Count, occupied.Distinct().Count()); // no two share a chair
            Assert.All(occupied, i => Assert.InRange(i, 0, Seats - 1));
        }
    }

    [Fact]
    public void AwayRegulars_CarryNoSeat()
    {
        for (int w = 0; w < 50; w++)
        {
            var seated = PatronRota.ResolveSeating("red-eye", w * PatronRota.WatchSeconds, Seats);
            Assert.All(seated, s => Assert.Equal(s.Present, s.SeatIndex >= 0));
        }
    }

    [Fact]
    public void ResolveSeating_ReturnsOnePerRegular_InRosterOrder()
    {
        var seated = PatronRota.ResolveSeating("the-space-bar", 12345, Seats);
        Assert.Equal(PatronRota.Roster, seated.Select(s => s.Regular).ToList());
    }

    [Fact]
    public void ResolveSeating_NoSeats_ParksEveryoneAway()
    {
        var seated = PatronRota.ResolveSeating("the-space-bar", 0, 0);
        Assert.All(seated, s => Assert.False(s.Present));
        Assert.All(seated, s => Assert.Equal(-1, s.SeatIndex));
    }

    [Fact]
    public void ResolveSeating_RejectsNegativeSeatCount() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PatronRota.ResolveSeating("the-space-bar", 0, -1));

    // ---- Per-place skew ---------------------------------------------------------------------------

    [Fact]
    public void Affinity_TheFixer_HauntsGreyMarketCinderRoost_MoreThanTheStraightLacedGate()
    {
        double greyMarket = PatronRota.Affinity("THE FIXER", "cinder-roost");
        double oldGate = PatronRota.Affinity("THE FIXER", "selene-gate");
        Assert.True(greyMarket > PatronRota.DefaultAffinity);
        Assert.True(oldGate < PatronRota.DefaultAffinity);
        Assert.True(greyMarket > oldGate);
    }

    [Fact]
    public void Affinity_UnbiasedPair_FallsBackToDefault() =>
        Assert.Equal(PatronRota.DefaultAffinity, PatronRota.Affinity("MADAM COIL", "ringside-exchange"));

    [Fact]
    public void Skew_ShowsUpInPresenceCounts_HighAffinityPortSeatsThemMoreOften()
    {
        // Over the long run the Fixer is present far more watches at Cinder Roost than at Selene Gate —
        // the weight is not just data, it changes who's in the room.
        int atRoost = PresentWatches("THE FIXER", "cinder-roost");
        int atGate = PresentWatches("THE FIXER", "selene-gate");
        Assert.True(atRoost > atGate + 100, $"expected a big skew: roost={atRoost}, gate={atGate} of 400");
    }

    [Fact]
    public void Affinities_ExposesTheAuthoredWeights()
    {
        Assert.NotEmpty(PatronRota.Affinities);
        Assert.True(PatronRota.Affinities.ContainsKey("THE FIXER|cinder-roost"));
        Assert.All(PatronRota.Affinities.Values, w => Assert.InRange(w, 0.0, 1.0));
    }

    private static int PresentWatches(string regular, string station)
    {
        int n = 0;
        for (int w = 0; w < 400; w++)
        {
            if (PatronRota.Resolve(regular, station, w * PatronRota.WatchSeconds) == PatronState.AtBar) { n++; }
        }
        return n;
    }
}

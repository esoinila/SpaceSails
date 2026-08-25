namespace SpaceSails.Core.Tests;

/// <summary>
/// #1006 · THE WHOLE ROOM PAINTED THE SAME COLOUR. When a round for the room handed out receipts, every
/// regular spoke the IDENTICAL vague-colour line — the pick was read off the sim-hour alone, so Madam Coil
/// and The Fixer raised their glasses and read the same card in the same breath.
///
/// <para>These pin the fixed law: the pick is salted PER SPEAKER on the shared <see cref="DiceRule"/>, the
/// per-sim-hour stability the old code claimed is kept (a regular says the same line all hour), and a round
/// hands out distinct lines while the four-line pool lasts. Bigger room than pool: the wheel turns over, and
/// a repeat is allowed but never forced.</para>
/// </summary>
public class RoomColorTests
{
    private const double NoonIsh = 600.0;      // sim-minute 600 — hour 10
    private const string Coil = "MADAM COIL";
    private const string Fixer = "THE FIXER";

    [Fact]
    public void ThePoolIsFourLines_AndTheRoomCanOutgrowIt()
    {
        // The number the whole guarantee is phrased against: distinct WHILE THE POOL LASTS, not always.
        Assert.Equal(4, RoomColor.PoolSize);
        Assert.Equal(RoomColor.PoolSize, RoomColor.Lines.Count);
        Assert.Equal(RoomColor.PoolSize, RoomColor.Lines.Distinct().Count());
    }

    [Fact]
    public void TwoRegularsInTheSameRound_DoNotReadTheSameCard()
    {
        // THE BUG, pinned: this is the exact pair from the issue, in the same breath at the same hour.
        // RED on the unfixed code, where both lines came from VagueColor[(SimTime / 60) % 4].
        var round = new RoomColor.Round();
        string coil = round.LineFor(Coil, NoonIsh);
        string fixer = round.LineFor(Fixer, NoonIsh);

        Assert.NotEqual(coil, fixer);
        Assert.Contains(coil, RoomColor.Lines);
        Assert.Contains(fixer, RoomColor.Lines);
    }

    [Fact]
    public void AFullRoomUpToThePool_HearsEveryLineOnce()
    {
        var round = new RoomColor.Round();
        string[] said = [.. new[] { Coil, Fixer, "GILT-EYE", "THE MAGPIE" }
            .Select(who => round.LineFor(who, NoonIsh))];

        Assert.Equal(RoomColor.PoolSize, said.Distinct().Count()); // four regulars, four different cards
    }

    [Fact]
    public void OneRegularSpeakingTwiceInTheSameHour_SaysTheSameLine()
    {
        // The stability the old code claimed, kept: the hour is still the only clock in the seed, so the
        // line a regular reaches for holds steady across two rounds inside that hour.
        string first = new RoomColor.Round().LineFor(Coil, NoonIsh);
        string again = new RoomColor.Round().LineFor(Coil, NoonIsh + 45); // still hour 10

        Assert.Equal(first, again);
        Assert.Equal(first, RoomColor.LineFor(Coil, NoonIsh)); // …and the roomless read agrees
    }

    [Fact]
    public void ARegularsLineHoldsForTheHour_AndTheHourIsInTheSeed()
    {
        // Constant across every minute of hour 10 …
        var withinTheHour = new HashSet<int>();
        for (double t = 600; t < 660; t += 1)
        {
            withinTheHour.Add(RoomColor.IndexFor(Coil, RoomColor.HourOf(t)));
        }

        Assert.Single(withinTheHour);

        // … and the hour genuinely moves it, or the salt would be the speaker alone and the line eternal.
        var acrossHours = new HashSet<int>();
        for (long h = 0; h < 100; h++)
        {
            acrossHours.Add(RoomColor.IndexFor(Coil, h));
        }

        Assert.Equal(RoomColor.PoolSize, acrossHours.Count);
    }

    [Fact]
    public void TheSaltIsTheSpeaker_SoTheRoomIsNotOneStream()
    {
        // Without a round's memory at all, different regulars still reach for different cards — the walk
        // inside Round is a tie-breaker, not the whole fix. Every line in the pool is somebody's first pick.
        var picks = new HashSet<int>();
        for (int i = 0; i < 40; i++)
        {
            picks.Add(RoomColor.IndexFor($"REGULAR-{i}", RoomColor.HourOf(NoonIsh)));
        }

        Assert.Equal(RoomColor.PoolSize, picks.Count);
    }

    [Fact]
    public void ARoomBiggerThanThePool_TurnsTheWheelOverRatherThanStalling()
    {
        // Eight regulars, four lines: somebody must repeat. The guarantee is that each pass of the wheel
        // is itself repeat-free, so the repeats spread out instead of piling onto one card.
        var round = new RoomColor.Round();
        string[] said = [.. Enumerable.Range(0, 8).Select(i => round.LineFor($"REGULAR-{i}", NoonIsh))];

        Assert.Equal(RoomColor.PoolSize, said.Take(4).Distinct().Count());
        Assert.Equal(RoomColor.PoolSize, said.Skip(4).Distinct().Count());
        Assert.All(said, line => Assert.Contains(line, RoomColor.Lines));
    }

    [Fact]
    public void ARepeatAcrossTheWheelsTurn_IsAllowedNotForced()
    {
        // The fifth speaker draws from the FULL pool on their own salted pick — so across a spread of
        // fifth-speakers some do echo the fourth and some do not. A forced-rotation fix would make the
        // echo impossible; a broken one would make it certain.
        int echoed = 0;
        int fresh = 0;
        for (int room = 0; room < 60; room++)
        {
            var round = new RoomColor.Round();
            string[] said = [.. Enumerable.Range(0, 5)
                .Select(i => round.LineFor($"ROOM{room}-REGULAR-{i}", NoonIsh))];
            if (said[4] == said[3])
            {
                echoed++;
            }
            else
            {
                fresh++;
            }
        }

        Assert.True(echoed > 0, "a repeat after the wheel turns must be POSSIBLE");
        Assert.True(fresh > 0, "…and must not be FORCED");
    }

    [Fact]
    public void ThePickIsDeterministic_TheOneEngineAndNoPrivateRandom()
    {
        // Determinism is law in Core: same speaker, same hour, same index — every time, no clock read.
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(RoomColor.IndexFor(Fixer, 10), RoomColor.IndexFor(Fixer, 10));
        }

        // …and it is the shared DiceRule doing the folding, not a random of its own.
        Assert.Equal(
            (int)(DiceRule.Seed($"room-colour:{Fixer}", 10) % (ulong)RoomColor.PoolSize),
            RoomColor.IndexFor(Fixer, 10));
    }
}

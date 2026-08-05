using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #698 · THE STORE CAN BE ASKED WHERE THINGS ARE. Owner, on B12 of the clinic, within the hour of #691:
/// <i>"I dropped 3 files on somebody here but there was nothing marked onto the map?"</i>
///
/// <para>The store already held every spot. What it had no way of answering was the deck's own question —
/// <b>what is lying about on THIS floor</b> — and the keybar's — <b>would a press right here give something
/// back</b>. Both were unanswerable, so both were unasked, and the mark and the offer could not exist.</para>
///
/// <para><b>The ring is the load-bearing half.</b> #691's recovery scanned the captain's square and the ring
/// around it in an inline loop inside the client. Drawing an offer off a second transcription of that loop
/// is precisely how this repo has shipped a sentence that disagreed with the sim four times over, so the ring
/// moved into <see cref="LeftBehind.SpotInReach"/> and the key, the prompt and the mark all ask it. These
/// tests pin the ring's exact edge: inside it gives, one square further out it does not.</para>
/// </summary>
public sealed class TheGroundKnowsWhereYouLeftItTests
{
    private static Satchel.Item Paper(int i) => new(Satchel.Kind.Paper, $"hive:luna:-12:{i}");

    /// <summary>The world centre of a square, which is where the captain would be standing on it.</summary>
    private static (double X, double Y) On(int squareX, int squareY) =>
        BeachComber.SquareCenter(squareX, squareY);

    [Fact]
    public void AFloorListsOnlyTheSquaresThatHaveSomethingOnThem()
    {
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-12, 4, 7), Paper(1));
        ground.Leave(LeftBehind.SpotKey(-12, -2, 0), Paper(2));

        Assert.Equal([(-2, 0), (4, 7)], ground.SpotsOn(-12));
    }

    [Fact]
    public void OneSpotIsONEMarkHoweverMuchIsLyingOnIt()
    {
        // The owner dropped THREE files and the deck should show ONE mark. A mark per item would be an
        // inventory panel bolted to the regolith — and it would count, which the plate must never do.
        var ground = new LeftBehind();
        string spot = LeftBehind.SpotKey(-12, 4, 7);
        ground.Leave(spot, Paper(1));
        ground.Leave(spot, Paper(2));
        ground.Leave(spot, Paper(3));

        Assert.Equal(3, ground.At(spot).Count);
        Assert.Equal([(4, 7)], ground.SpotsOn(-12));
    }

    [Fact]
    public void AFloorNeverShowsAnotherFloorsSpots()
    {
        // The reason SpotKey carries the floor at all. B12's paperwork must not draw itself on B11, and the
        // regolith must not draw the whole facility's worth of it.
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-12, 4, 7), Paper(1));
        ground.Leave(LeftBehind.SpotKey(-11, 4, 7), Paper(2));
        ground.Leave(LeftBehind.SpotKey(0, 4, 7), Paper(3));

        Assert.Equal([(4, 7)], ground.SpotsOn(-12));
        Assert.Equal([(4, 7)], ground.SpotsOn(-11));
        Assert.Equal([(4, 7)], ground.SpotsOn(0));
        Assert.Empty(ground.SpotsOn(-13));
    }

    [Fact]
    public void TheMarkIsGoneTheMomentTheSpotEmpties()
    {
        var ground = new LeftBehind();
        string spot = LeftBehind.SpotKey(-12, 4, 7);
        ground.Leave(spot, Paper(1));
        Assert.NotEmpty(ground.SpotsOn(-12));

        LeftBehind.Recovery back = ground.PickUp(spot, []);

        Assert.Single(back.Taken);
        Assert.Empty(back.StillThere);
        Assert.Empty(ground.SpotsOn(-12));
    }

    [Fact]
    public void WhatWouldNotFitKeepsItsMark()
    {
        // #615's law, drawn. A recovery that could not take everything leaves the rest lying there, so the
        // mark has to stay — a deck that cleared it would be telling the captain their relic was gone.
        var ground = new LeftBehind();
        string spot = LeftBehind.SpotKey(0, 1, 1);
        var bulky = new List<Satchel.Item>();
        for (int i = 0; i < 20; i++)
        {
            Satchel.Item pallet = new(Satchel.Kind.Relic, $"pallet-{i}");
            ground.Leave(spot, pallet);
            bulky.Add(pallet);
        }

        LeftBehind.Recovery back = ground.PickUp(spot, []);

        Assert.NotEmpty(back.StillThere);
        Assert.Equal([(1, 1)], ground.SpotsOn(0));
    }

    [Fact]
    public void TheRingGivesFromTheSquareYouStandOnAndTheEightAroundIt()
    {
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-12, 4, 7), Paper(1));

        for (int dy = -LeftBehind.ReachSquares; dy <= LeftBehind.ReachSquares; dy++)
        {
            for (int dx = -LeftBehind.ReachSquares; dx <= LeftBehind.ReachSquares; dx++)
            {
                (double x, double y) = On(4 + dx, 7 + dy);
                Assert.True(ground.AnythingInReach(-12, x, y),
                    $"standing on square ({4 + dx},{7 + dy}) the ground should give it back");
            }
        }
    }

    [Fact]
    public void OneSquareFurtherOutTheGroundOffersNothing()
    {
        // The edge, in both axes and on the diagonal. A prompt that lit up here would be an offer the key
        // does not honour — the exact half of #698 the owner cannot see until he presses E and nothing
        // happens.
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-12, 4, 7), Paper(1));

        int beyond = LeftBehind.ReachSquares + 1;
        foreach ((int dx, int dy) in new[]
                 { (beyond, 0), (-beyond, 0), (0, beyond), (0, -beyond), (beyond, beyond) })
        {
            (double x, double y) = On(4 + dx, 7 + dy);
            Assert.False(ground.AnythingInReach(-12, x, y),
                $"square ({4 + dx},{7 + dy}) is outside the ring and must offer nothing");
        }
    }

    [Fact]
    public void TheRingIsFloorScopedToo()
    {
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-12, 4, 7), Paper(1));
        (double x, double y) = On(4, 7);

        Assert.True(ground.AnythingInReach(-12, x, y));
        Assert.False(ground.AnythingInReach(-11, x, y));
        Assert.False(ground.AnythingInReach(0, x, y));
    }

    [Fact]
    public void TheRingNamesTheSameSpotEveryTimeItIsAsked()
    {
        // Determinism, because the mark, the prompt and the key ask this separately and within one frame.
        // Two spots in reach at once is the ordinary case for a captain who has been shedding weight.
        var ground = new LeftBehind();
        ground.Leave(LeftBehind.SpotKey(-12, 4, 7), Paper(1));
        ground.Leave(LeftBehind.SpotKey(-12, 5, 7), Paper(2));
        (double x, double y) = On(4, 7);

        string? first = ground.SpotInReach(-12, x, y);
        Assert.NotNull(first);
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal(first, ground.SpotInReach(-12, x, y));
        }
    }

    [Fact]
    public void ThePlateIsAFactAndNeverAManifest()
    {
        // Owner's own framing of what the label may say: it marks the place, it does not itemise it. If this
        // ever grows a count or a name it stops being scenery and starts doing the captain's remembering.
        Assert.False(LeftBehind.MarkerLabel.Any(char.IsDigit),
            $"the plate must never count what is under it: \"{LeftBehind.MarkerLabel}\"");
        Assert.Contains("WHAT YOU LEFT", LeftBehind.MarkerLabel, StringComparison.Ordinal);
        Assert.StartsWith("E — ", LeftBehind.ReachPrompt, StringComparison.Ordinal);
    }
}

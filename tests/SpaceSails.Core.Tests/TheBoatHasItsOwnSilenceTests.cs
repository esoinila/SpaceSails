using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1016 · WHAT WAITING SOUNDS LIKE ON YOUR OWN BOAT.
///
/// <para><b>Owner, on 7 Deck:</b> <i>"Why no table here to sit at?"</i>, <i>"Why no table in cabin
/// either?"</i>, <i>"I expect to have a bar table like this in this ships galley also.... feature
/// complete."</i></para>
///
/// <para>Sitting down aboard is the same posture as sitting down ashore and it is not the same ROOM, so it
/// does not borrow the hall's words. Nobody ever crosses the captain's own cantina — his crew is three
/// droids on a fixed patrol — so a wait aboard is always the silence, and the silence has to be the boat's:
/// a line about eighty chairs and a card machine, read from a cabin with a bunk in it, would be the sentence
/// reporting a different building than the one the player is looking at, which is this repository's own
/// named prose bug class.</para>
/// </summary>
public sealed class TheBoatHasItsOwnSilenceTests
{
    /// <summary>
    /// BOTH POOLS ARE IN <see cref="SittingAlone.AllProse"/>, so the canon sweeps walk them.
    ///
    /// <para>A pool the greps cannot see is a pool nobody checks: the linen slip (#783/#941) survived exactly
    /// that way, in a line the first sweep's list did not reach. The plate goes in for the same reason.</para>
    ///
    /// <para><b>Proven RED</b> by dropping either <c>foreach</c> out of <c>AllProse</c>.</para>
    /// </summary>
    [Fact]
    public void TheShipsOwnPoolsAreWalkedByTheCanonSweep()
    {
        List<string> swept = [.. SittingAlone.AllProse()];

        foreach (string line in SittingAlone.NobodyCameShipCantina)
        {
            Assert.Contains(line, swept);
        }
        foreach (string line in SittingAlone.NobodyCameShipCabin)
        {
            Assert.Contains(line, swept);
        }

        Assert.Contains(SittingAlone.OwnDeskPlate, swept);
        Assert.Contains(SittingAlone.ShipCantinaSetting, swept);
        Assert.Contains(SittingAlone.ShipCabinSetting, swept);
    }

    /// <summary>
    /// THE TWO ROOMS SAY DIFFERENT THINGS, and neither of them says the hall's.
    ///
    /// <para>Anti-vacuous: a pool that answered the same line on every beat, or two pools that were one pool
    /// with a flag on it, would make the <c>Quiet</c> fork in the wait beat's ladder decorative.</para>
    /// </summary>
    [Fact]
    public void ACantinaAndACabinAreNotTheSameSilence()
    {
        var cantina = new List<string>();
        var cabin = new List<string>();
        for (int beat = 0; beat < 6; beat++)
        {
            cantina.Add(SittingAlone.NobodyCameAboard(cabin: false, beat));
            cabin.Add(SittingAlone.NobodyCameAboard(cabin: true, beat));
        }

        Assert.Equal(SittingAlone.NobodyCameShipCantina.Count, cantina.Distinct().Count());
        Assert.Equal(SittingAlone.NobodyCameShipCabin.Count, cabin.Distinct().Count());
        Assert.Empty(cantina.Intersect(cabin));

        // And not one of them is the hall's, on any watch the hall has.
        var hall = new List<string>();
        for (long watch = 0; watch < CanteenRegulars.WatchFill.Count; watch++)
        {
            for (int beat = 0; beat < 8; beat++)
            {
                hall.Add(SittingAlone.NobodyCame(watch, beat));
                hall.Add(SittingAlone.NobodyCame(watch, beat, quiet: true));
            }
        }

        Assert.Empty(cantina.Concat(cabin).Intersect(hall));
    }

    /// <summary>The plate is built out of the family's own chair glyph, like the table's — so the room
    /// cannot end up with two seats that are drawn differently.</summary>
    [Fact]
    public void TheDeskWearsTheFamilysOwnChair()
    {
        Assert.StartsWith(SittingAlone.Glyph, SittingAlone.OwnDeskPlate, System.StringComparison.Ordinal);
        Assert.StartsWith(SittingAlone.Glyph, SittingAlone.OwnTablePlate, System.StringComparison.Ordinal);
        Assert.NotEqual(SittingAlone.OwnTablePlate, SittingAlone.OwnDeskPlate);
    }

    /// <summary>
    /// AND THE DESK IS IN A BERTH THIS SHIP ACTUALLY HAS, derived from that berth's own bounds rather than
    /// typed — §13.15, and this ship's own named console bug (four collisions, every one of them a client
    /// literal beside a Core constant).
    /// </summary>
    [Fact]
    public void TheDeskStandsInsideTheCabinItNames()
    {
        DeckReachability.Point desk = ShipLayout.CabinDeskStation;
        Assert.Equal(ShipLayout.DeskCabin, ShipLayout.CompartmentAt(desk.X, desk.Y));

        // …and clear of the bunk, which stands in the middle of the same berth. The deck audit's own label
        // law, asserted here too because the reason the corner was chosen is a Core fact.
        DeckReachability.Point bunk = default;
        foreach (ShipLayout.Room r in ShipLayout.Rooms)
        {
            if (r.Name == ShipLayout.DeskCabin)
            {
                bunk = ShipLayout.Inside(r);
            }
        }

        double dx = desk.X - bunk.X, dy = desk.Y - bunk.Y;
        Assert.True(System.Math.Sqrt((dx * dx) + (dy * dy)) >= 2.0,
            "the desk and the bunk are one illegible smear of two labels.");
    }
}

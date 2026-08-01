using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #586 · THE MONOLITH IS A PLACE, NOT A BOX.
///
/// <para>Owner, standing at it: <i>"let's have gen AI image at the monolith and some items appearing there
/// now and then ... it is supposed to be impressive... now it looks like a box in closet."</i></para>
///
/// <para>He was measuring something real. The canon slab was <b>2.4 × 5 deck units</b> — the captain is 1.4
/// across — sitting at the heart of a field 310 × 260. The deep commitment anchor of the whole site, the
/// thing the long walk is FOR, was about two captains tall.</para>
/// </summary>
public sealed class TheMonolithIsAPlaceTests
{
    [Fact]
    public void TheSlabIsBigEnoughToBeAWallRatherThanACrate()
    {
        // Four captains wide is the floor. Below that it is furniture, whatever the label says.
        const double captain = 1.4;
        Assert.True(Monolith.HalfWidth * 2 >= captain * 4,
            "the monolith is narrower than four captains — it is a crate again.");

        // And the apron has to be big enough to read as a cleared APPROACH from a distance, not a kerb.
        Assert.True(Monolith.ApronRadius > Monolith.HalfWidth * 4,
            "the swept apron is barely wider than the stone, so there is no approach to see.");
        Assert.True(Monolith.MarkerRing < Monolith.ApronRadius,
            "the approach stubs stand outside the apron they are supposed to be on.");
        Assert.True(Monolith.MarkerRing > Monolith.HalfWidth + 2,
            "the approach stubs are jammed against the slab.");
    }

    [Fact]
    public void ItStaysINSIDETheCanonMazeCell()
    {
        // The maze is canon and stays exactly as authored: rows at anchor +12, +6 and −4. A slab that grew
        // into them would be "improving" the one piece of ground the owner has never asked to change.
        Assert.True(Monolith.HalfHeight < 4.0,
            "the slab now crosses the canon maze row at anchor −4.");
    }

    [Fact]
    public void MostVisitWindowsHaveNOTHINGAtTheFoot()
    {
        // Load-bearing emptiness, the same law as the ruins (#573): if there were always something, the walk
        // would be a shopping trip instead of a pilgrimage, and finding something would stop meaning anything.
        int found = 0;
        const int windows = 400;
        for (long epoch = 0; epoch < windows; epoch++)
        {
            if (Monolith.AtTheFoot("miranda", "", epoch) != Monolith.Offering.Nothing)
            {
                found++;
            }
        }

        Assert.True(found > windows / 8, "nothing is ever left here — the mechanic is dead.");
        Assert.True(found < windows * 3 / 4, "there is always something here — it is a vending machine.");
    }

    [Fact]
    public void EveryOfferingThatEXISTSHasALabelAndALine()
    {
        // The bug this is really guarding: a console that says SOMETHING LEFT HERE and then has nothing to
        // say. That is the map lying, which is the one thing this ground is not allowed to do.
        foreach (Monolith.Offering what in System.Enum.GetValues<Monolith.Offering>())
        {
            if (what == Monolith.Offering.Nothing)
            {
                Assert.Equal("", Monolith.FootLabel(what));
                Assert.Equal("", Monolith.FootLine(what, "miranda", "", 0));
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(Monolith.FootLabel(what)),
                $"{what} has no console label.");
            Assert.True(Monolith.FootLine(what, "miranda", "", 0).Length > 60,
                $"{what} has no line worth walking out there for.");
        }
    }

    [Fact]
    public void EveryLineIsSOMEBODYELSESVisitAndNeverTheStoneReacting()
    {
        // The register, stated as a law. The Old Ones' origin is canon and is never confirmed by a card or a
        // sensor; the monolith is older than the question and does not answer it. If a line ever has the
        // stone move, glow, hum or respond, the game has explained itself and the whole thing deflates.
        string[] forbidden = ["hums", "glows", "pulses", "responds", "opens", "speaks", "warm to the touch"];

        foreach (Monolith.Offering what in System.Enum.GetValues<Monolith.Offering>())
        {
            for (long epoch = 0; epoch < 12; epoch++)
            {
                string line = Monolith.FootLine(what, "miranda", "", epoch);
                foreach (string bad in forbidden)
                {
                    Assert.DoesNotContain(bad, line, System.StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void TheLoreExplainsNOTHING()
    {
        // Same law, on the card that comes with the picture. It may describe; it may not account for.
        string[] forbidden = ["reever", "old one", "built by", "made by", "in order to", "because the"];
        foreach (string bad in forbidden)
        {
            Assert.DoesNotContain(bad, Monolith.Lore, System.StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(Monolith.Lore.Length > 400, "the card is too thin to be worth the walk.");
        Assert.EndsWith("art/monolith.jpg", Monolith.ArtUrl, System.StringComparison.Ordinal);
    }

    [Fact]
    public void AWindowHoldsStillForAWholeExcursion()
    {
        // Object persistence, the owner's law: "the long walk should walk with expected object persistence."
        // A captain who walks out, walks back and returns the same afternoon must find the same thing.
        Assert.Equal(Monolith.EpochAt(1_000), Monolith.EpochAt(1_000 + 900));

        // And an excursion cannot outlast a window by so much that the ground changes under a captain who is
        // standing on it — a full tank is 1200 s (SuitAir), so the window must comfortably exceed that.
        Assert.True(Monolith.EpochSeconds > SuitAir.TankSeconds * 2,
            "a visit window is short enough that the foot could change mid-excursion.");
    }

    [Fact]
    public void WhatIsAtTheFootIsDeterministicPerSiteAndWindow()
    {
        // Determinism is law in Core. Two sites on the same body must also be able to differ, or the salt is
        // doing nothing and every ground tells the same story.
        for (long epoch = 0; epoch < 50; epoch++)
        {
            Assert.Equal(
                Monolith.AtTheFoot("miranda", "ShadowedRille", epoch),
                Monolith.AtTheFoot("miranda", "ShadowedRille", epoch));
        }

        var canon = new List<Monolith.Offering>();
        var rille = new List<Monolith.Offering>();
        for (long epoch = 0; epoch < 80; epoch++)
        {
            canon.Add(Monolith.AtTheFoot("miranda", "", epoch));
            rille.Add(Monolith.AtTheFoot("miranda", "ShadowedRille", epoch));
        }
        Assert.True(canon.Where((c, i) => c != rille[i]).Any(),
            "two sites on one body show the identical sequence — the salt is not reaching the seed.");
    }
}

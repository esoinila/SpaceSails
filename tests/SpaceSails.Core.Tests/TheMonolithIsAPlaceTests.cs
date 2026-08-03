using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #586 / #649 · THE MONOLITH IS A PLACE, NOT A BOX — and there is exactly ONE of it.
///
/// <para>Owner, standing at it: <i>"let's have gen AI image at the monolith and some items appearing there
/// now and then ... it is supposed to be impressive... now it looks like a box in closet."</i></para>
///
/// <para>He was measuring something real. The canon slab was <b>2.4 × 5 deck units</b> — the captain is 1.4
/// across — sitting at the heart of a field 310 × 260. The deep commitment anchor of the whole site, the
/// thing the long walk is FOR, was about two captains tall.</para>
///
/// <para>#649 · And it was on the wrong moon. Every treasure map minted since #164 paces off
/// <i>"the monolith"</i> on PHOBOS; the slab was drawn on MIRANDA. Owner's ruling: <i>"There is ONE
/// monolith… If two grounds both call something 'the monolith', one of them is wrong and has to be renamed
/// — the word is reserved."</i> The word stays with the object the cards name; Miranda's maze centre is
/// <see cref="FalseSlab"/>, which is a made thing and a different class of object entirely.</para>
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

    /// <summary>THE MONOLITH IS ONE OBJECT, ON ONE GROUND. The slab's geometry is only laid where
    /// <c>SurfaceLayout.For</c> routes to the authored plan that lays it — the canon empty-salt site of
    /// <see cref="Monolith.BodyId"/> — yet the once-in-a-life nerve hit and the FirstMonolith selfie keyed
    /// off the DEEP ANCHOR's coordinates, which every ground has. Luna's mass-driver muzzle sits on that
    /// exact anchor. So a captain who walked up to a broken launch machine was told "the monolith resolves
    /// out of the dark", paid 24 nerve for it, and — the flag being kept for life — could never be shown the
    /// real one afterwards.</summary>
    [Theory]
    [InlineData("phobos", "", true)]                         // #649: the ground the map cards have always named
    [InlineData("phobos", "RidgeCamp", false)]               // same moon, seeded ground: no slab is built there
    [InlineData("miranda", "", false)]                       // the canon maze centre is the FALSE SLAB now
    [InlineData("miranda", "ShadowedRille", false)]
    [InlineData("luna", "", false)]                          // the mass-driver muzzle stands on the same anchor
    [InlineData("europa", "QuietBasin", false)]
    public void TheSlabStandsOnExactlyOneGround(string bodyId, string siteSalt, bool expected)
    {
        Assert.Equal(expected, Monolith.StandsOn(bodyId, siteSalt));

        // And the predicate must agree with the ground the layout generator actually builds: the authored
        // plan that lays the slab is exactly where StandsOn is true.
        bool authoredSlab = SurfaceLayout.For(bodyId, SurfaceLayout.DefaultField, siteSalt)
            .Landmarks.Any(m => m.Label.Contains("MONOLITH", System.StringComparison.Ordinal));
        Assert.Equal(expected, authoredSlab);
    }

    /// <summary>#649 · THE CARD IN YOUR POCKET AND THE THING ON THE HORIZON NAME THE SAME MOON.
    ///
    /// <para>This is the bug the owner's ruling was written to settle, and it is older than the ruling. Since
    /// #164 every treasure map has been minted by <see cref="Landmarks.For"/> and every one of them paced off
    /// <i>"the monolith"</i> on <b>Phobos</b> — <c>"PHOBOS — from the monolith, 40 paces anti-spinward"</c>.
    /// The drawn slab stood on <b>Miranda</b>. So the game handed you a map to a landmark that was not on that
    /// moon, and #648 could only unify the PREDICATE — one function, asked by everybody — not the FACT it
    /// answered with.</para>
    ///
    /// <para>Stated as a biconditional on purpose. "The slab is on the moon the cards name" would pass if the
    /// cards ever started naming two moons; this says the set of moons whose map cards pace off the monolith
    /// and the set of moons the monolith stands on are the SAME SET, and that both have one member.</para></summary>
    [Fact]
    public void TheMoonTheMapCardsPaceFromIsTheMoonTheSlabStandsOn()
    {
        string[] landable =
        [
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        ];

        var cardSays = new List<string>();
        var groundSays = new List<string>();
        foreach (string body in landable)
        {
            if (Landmarks.For(body).Name.Contains("monolith", System.StringComparison.OrdinalIgnoreCase))
            {
                cardSays.Add(body);
            }
            if (Monolith.StandsOn(body, ""))
            {
                groundSays.Add(body);
            }
        }

        Assert.Equal(cardSays, groundSays);
        Assert.Single(groundSays);
        Assert.Equal(Monolith.BodyId, groundSays[0]);
    }

    /// <summary>#649 · THE WORD IS RESERVED. Owner: <i>"If two grounds both call something 'the monolith',
    /// one of them is wrong and has to be renamed."</i> Every landable body, every landing site it offers:
    /// exactly one ground may print the word, on its landmark label or in the location line its scheme
    /// reads.</summary>
    [Fact]
    public void ExactlyOneGroundInTheWholeGameSaysTheWord()
    {
        string[] landable =
        [
            "luna", "phobos", "europa", "ganymede", "callisto",
            "titan", "enceladus", "miranda", "triton", "the-clinker",
        ];

        var says = new List<string>();
        foreach (string body in landable)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SurfaceLayout.Plan plan = SurfaceLayout.For(body, SurfaceLayout.DefaultField, site.LayoutSalt);
                bool named = plan.Scheme.Contains("MONOLITH", System.StringComparison.OrdinalIgnoreCase)
                    || plan.Landmarks.Any(m =>
                        m.Label.Contains("MONOLITH", System.StringComparison.OrdinalIgnoreCase));
                if (named)
                {
                    says.Add($"{body}/{site.Name}");
                }
            }
        }

        Assert.Equal([$"{Monolith.BodyId}/{LandingSites.At(Monolith.BodyId, 0).Name}"], says);
    }

    /// <summary>#649 · MIRANDA'S CENTRE IS A DIFFERENT KIND OF OBJECT, not a second nameless slab. The maze
    /// itself is canon and must not have moved a deck unit while the word was taken back off it.</summary>
    [Fact]
    public void MirandasMazeKeepsItsGeometryAndLosesOnlyTheName()
    {
        Assert.False(Monolith.StandsOn(FalseSlab.BodyId, ""));
        Assert.True(FalseSlab.StandsOn(FalseSlab.BodyId, ""));
        Assert.False(FalseSlab.StandsOn(Monolith.BodyId, ""));
        Assert.False(FalseSlab.StandsOn(FalseSlab.BodyId, "ShadowedRille"));

        // The numbers the maze was authored around, unchanged — the canon rows sit at anchor +12, +6 and −4,
        // and a slab that grew into them would be "improving" the one ground the owner never asked to change.
        Assert.Equal(3.0, FalseSlab.HalfWidth);
        Assert.Equal(3.5, FalseSlab.HalfHeight);
        Assert.True(FalseSlab.HalfHeight < 4.0, "the false slab now crosses the canon maze row at anchor −4.");
        Assert.Equal(11.0, FalseSlab.MarkerRing);

        // And it never borrows the reserved word — not on the ground, not in the location line, not on the
        // card, and not in the art it reaches for.
        foreach (string text in new[]
                 { FalseSlab.ConsoleLabel, FalseSlab.Scheme, FalseSlab.Lore, FalseSlab.ArtUrl })
        {
            Assert.DoesNotContain("monolith", text, System.StringComparison.OrdinalIgnoreCase);
        }

        // Its card describes and does not account for — the same law as the monolith's, because a card that
        // explained THIS one would have explained the other one by subtraction.
        foreach (string bad in new[] { "reever", "old one", "built by", "made by", "in order to", "copy of" })
        {
            Assert.DoesNotContain(bad, FalseSlab.Lore, System.StringComparison.OrdinalIgnoreCase);
        }
        Assert.True(FalseSlab.Lore.Length > 400, "the card is too thin to be worth the walk.");
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
            if (Monolith.AtTheFoot(Monolith.BodyId, "", epoch) != Monolith.Offering.Nothing)
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
                Assert.Equal("", Monolith.FootLine(what, Monolith.BodyId, "", 0));
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(Monolith.FootLabel(what)),
                $"{what} has no console label.");
            Assert.True(Monolith.FootLine(what, Monolith.BodyId, "", 0).Length > 60,
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
                string line = Monolith.FootLine(what, Monolith.BodyId, "", epoch);
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
                Monolith.AtTheFoot(Monolith.BodyId, "RidgeCamp", epoch),
                Monolith.AtTheFoot(Monolith.BodyId, "RidgeCamp", epoch));
        }

        var canon = new List<Monolith.Offering>();
        var rille = new List<Monolith.Offering>();
        for (long epoch = 0; epoch < 80; epoch++)
        {
            canon.Add(Monolith.AtTheFoot(Monolith.BodyId, "", epoch));
            rille.Add(Monolith.AtTheFoot(Monolith.BodyId, "RidgeCamp", epoch));
        }
        Assert.True(canon.Where((c, i) => c != rille[i]).Any(),
            "two sites on one body show the identical sequence — the salt is not reaching the seed.");
    }
}

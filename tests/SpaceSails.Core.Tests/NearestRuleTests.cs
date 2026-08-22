namespace SpaceSails.Core.Tests;

/// <summary>
/// QA gates for #954 — 🎯 "Nearest" flickers between Mars and The Rusty Roadstead every orbit.
///
/// <para>The world the bug lived in, taken from <c>scenarios/sol.json</c>: The Rusty Roadstead is a station
/// on a 12,000 km rail around Mars with a 7,200 s period. The owner's HUD read <i>0.16 AU</i> — 2.4e10 m —
/// and at that range the station's whole orbit moves the two distances apart by five parts in ten thousand.
/// The old code took the literal minimum every frame, so twice per station orbit — several times a second
/// at warp — the readout swapped, and the scope's AUTO lock swapped with it.</para>
///
/// <para>These gates hold the law from both sides: the tiny difference must NOT swap, and a real change of
/// neighbourhood must still swap. A band that never swaps is as broken as one that always does.</para>
/// </summary>
public class NearestRuleTests
{
    // The owner's actual geometry, in metres.
    private const double ShipToMars = 0.16 * 1.495978707e11;       // 0.16 AU, off the screenshot
    private const double RoadsteadOrbitRadius = 12_000_000.0;      // sol.json: the-space-bar orbitRadiusM

    [Fact]
    public void Unseats_DoesNotSwap_WhenAStationsOwnOrbitIsTheWholeDifference()
    {
        // #954 REGRESSION, at the exact numbers the owner watched flicker. As the Roadstead swings around
        // Mars, its distance to the ship runs from (Mars - 12,000 km) to (Mars + 12,000 km). Neither end
        // may unseat the other body — that swing IS the flicker, and it must be swallowed whole.
        double marsSide = ShipToMars - RoadsteadOrbitRadius;
        double farSide = ShipToMars + RoadsteadOrbitRadius;

        Assert.False(NearestRule.Unseats(incumbentDistance: ShipToMars, challengerDistance: marsSide));
        Assert.False(NearestRule.Unseats(incumbentDistance: marsSide, challengerDistance: ShipToMars));
        Assert.False(NearestRule.Unseats(incumbentDistance: farSide, challengerDistance: ShipToMars));

        // And they are, correspondingly, "in the same breath" — the pair the readout must name together.
        Assert.True(NearestRule.InTheSameBreath(ShipToMars, marsSide));
        Assert.True(NearestRule.InTheSameBreath(ShipToMars, farSide));
    }

    [Fact]
    public void Unseats_StillSwaps_WhenTheNeighbourhoodGenuinelyChanges()
    {
        // The band must not weld the readout shut. A challenger that is a third closer is plainly the new
        // nearest — the ship crossed into another body's part of the sky, and the HUD must say so.
        Assert.True(NearestRule.Unseats(incumbentDistance: 3.0e11, challengerDistance: 2.0e11));

        // Even a modest but real approach clears it: 10% closer is well past the 3% band.
        Assert.True(NearestRule.Unseats(incumbentDistance: 1.0e10, challengerDistance: 0.9e10));
        Assert.False(NearestRule.InTheSameBreath(1.0e10, 0.9e10));
    }

    [Fact]
    public void Unseats_SitsExactlyOnItsOwnBand()
    {
        // The threshold is the stated fraction and nothing else — one part in a thousand either side of it
        // decides the contest, so the constant is the law rather than a comment about the law.
        const double incumbent = 1.0e10;
        double onTheBand = incumbent * (1.0 - NearestRule.SwapFraction);

        Assert.False(NearestRule.Unseats(incumbent, onTheBand));            // exactly on it does not unseat
        Assert.True(NearestRule.Unseats(incumbent, onTheBand * 0.999));     // a hair inside does
    }

    [Fact]
    public void UnseatsSquared_AgreesWithTheDistanceForm_AcrossTheBand()
    {
        // The per-frame sweep compares squared distances (no square roots). The two forms must be the same
        // law, or the flicker comes back on the path that actually runs.
        double[] distances = [ShipToMars - RoadsteadOrbitRadius, ShipToMars, ShipToMars + RoadsteadOrbitRadius,
                              0.9e10, 1.0e10, 2.0e11, 3.0e11];

        foreach (double a in distances)
        {
            foreach (double b in distances)
            {
                Assert.Equal(NearestRule.Unseats(a, b), NearestRule.UnseatsSquared(a * a, b * b));
                Assert.Equal(NearestRule.InTheSameBreath(a, b), NearestRule.InTheSameBreathSquared(a * a, b * b));
            }
        }
    }

    [Fact]
    public void Unseats_IsNeverTrueBothWays()
    {
        // Two candidates can never each unseat the other — that is what a swap loop would look like, and it
        // is the shape of the original bug. Asserted over the flicker pair and a genuine swap alike.
        double[] distances = [ShipToMars - RoadsteadOrbitRadius, ShipToMars, ShipToMars + RoadsteadOrbitRadius,
                              0.9e10, 1.0e10, 2.0e11];

        foreach (double a in distances)
        {
            foreach (double b in distances)
            {
                Assert.False(NearestRule.Unseats(a, b) && NearestRule.Unseats(b, a));
            }
        }
    }

    [Fact]
    public void Hierarchy_SpeaksTheContainmentInOneLine()
    {
        // The owner's ask, verbatim: "Mars is closest and it contains (in its Hill sphere) The Rusty
        // Roadstead." One line, both names, the chevron carrying the containment.
        string line = NearestRule.Hierarchy("Mars", "The Rusty Roadstead");
        Assert.Equal("Mars › The Rusty Roadstead", line);
        Assert.Contains("Mars", line);
        Assert.Contains("The Rusty Roadstead", line);
    }

    [Fact]
    public void OrbitsNote_NamesTheSphereWithoutRenamingTheTarget()
    {
        // The scope's sub-line. It says whose sphere the locked object is in; it must NOT be the object's
        // name, because the box draws the object and the words may never disagree with the picture.
        string note = NearestRule.OrbitsNote("Mars");
        Assert.Equal("orbits Mars", note);
        Assert.DoesNotContain("›", note);
    }
}

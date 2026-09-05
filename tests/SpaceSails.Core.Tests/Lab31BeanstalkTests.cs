using SpaceSails.Contracts;
using SpaceSails.Labs.Lab31;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #234 · THE LAB CANNOT DRIFT FROM ITS OWN ARITHMETIC.
///
/// <para>Lab 31 prints five graded verdicts — Earth is <i>beyond any real material</i>, Luna is
/// <i>buildable with fibre you can buy today</i>, Phobos is <i>a long rope with a rock on the end</i> — and a
/// verdict is exactly the kind of sentence that outlives the numbers that produced it. Somebody widens a
/// threshold, somebody swaps a material's datasheet value, and the README goes on saying the memorable thing
/// while the table underneath it has stopped agreeing. This suite exists so that cannot happen quietly.</para>
///
/// <para>The lab's <c>Beanstalk.cs</c> is LINKED into this project (see the csproj), so the function that
/// printed a sentence is the same function tested here — there is exactly one copy of the arithmetic. Three
/// independent anchors hold it: <see cref="EveryVerdictIsWhatItsOwnTaperNumbersSay"/> checks each label
/// against a PROPERTY of the taper column re-stated here with this file's own literal thresholds (not the
/// lab's constants, or flipping one would move the guard along with the claim);
/// <see cref="EveryVerdictIsTheOneTheLabPublished"/> pins the published grade per body; and
/// <see cref="TheReadmeCarriesTheVerdictLinesTheProbePrinted"/> reads the shipped README and finds each line
/// verbatim, because labs/README.md's ironclad rule is that every number in a lesson came from running it.</para>
///
/// <para>The constants get the same treatment. Lab 31 copies Earth, Mars, Luna and Phobos out of
/// <c>scenarios/sol.json</c> and says so in a column; <see cref="TheGameBodiesConstantsAreTheGamesOwn"/> opens
/// the real scenario file and proves the copy has not gone stale — a lab that claims the game's ephemeris and
/// carries a fork of it is telling a small lie in a loud voice.</para>
/// </summary>
public sealed class Lab31BeanstalkTests
{
    // This file's OWN thresholds, written out rather than read from Beanstalk. If the guard imported the
    // lab's constants, moving one would move the verdict AND the expectation together and the test would
    // stay green through the change it exists to catch.
    private const double PracticalTaper = 10.0;
    private const double RoundingErrorTaper = 1.1;

    /// <summary>The grades the lab publishes — in its README, in its PR, and in every citation that will
    /// ever be made of it. Changing one of these is changing what the lab says, and should cost a test edit.</summary>
    private static readonly (string Body, string Verdict)[] Published =
    [
        ("Earth", Beanstalk.VerdictBeyond),
        ("Mars", Beanstalk.VerdictOnlyBest),
        ("Luna", Beanstalk.VerdictFibreToday),
        ("Ceres", Beanstalk.VerdictSteel),
        ("Phobos", Beanstalk.VerdictRope),
        ("Deimos", Beanstalk.VerdictRope),
    ];

    private static Climb ClimbFor(string body) => Beanstalk.MeasureAll().First(c => c.Body.Name == body);

    /// <summary>Walk up to the repo's labs folder — the same trick <c>LabNumbersAreUniqueTests</c> uses, and
    /// for the same reason: cheaper than threading a path through MSBuild, and it fails legibly instead of
    /// sweeping an empty directory and passing everything.</summary>
    private static string LabsDirectory()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "labs");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "README.md")))
            {
                return candidate;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find labs/ above {AppContext.BaseDirectory}");
    }

    /// <summary>A sweep that finds nothing proves every claim below trivially. Pin the world first: six
    /// bodies, each with a tension peak above its own ground and a climb that actually climbs.</summary>
    [Fact]
    public void TheSweepActuallySeesSixBodiesWithRealClimbs()
    {
        IReadOnlyList<Climb> climbs = Beanstalk.MeasureAll();

        Assert.Equal(6, climbs.Count);
        Assert.Equal(["Earth", "Mars", "Luna", "Ceres", "Phobos", "Deimos"], climbs.Select(c => c.Body.Name));

        foreach (Climb c in climbs)
        {
            Assert.True(c.AnchorRadius > c.Body.Radius,
                $"{c.Body.Name}: the maximum-tension point is at or below the ground, so there is no cable to taper.");
            Assert.True(c.DeltaPotential > 0,
                $"{c.Body.Name}: the potential climb is {c.DeltaPotential} J/kg — a cable that costs nothing to " +
                "stand up would make every verdict below meaningless.");
            Assert.True(c.LaunchDeltaV > 0, $"{c.Body.Name}: the rocket alternative priced at zero.");
        }
    }

    /// <summary>The lab's column says "sol.json". Prove it. Every body the game actually carries must match
    /// the shipped scenario exactly — mu, radius, and for the tidally locked ones the orbit radius the
    /// three-body frame is built on.</summary>
    [Fact]
    public void TheGameBodiesConstantsAreTheGamesOwn()
    {
        ScenarioDefinition sol = ScenarioLoader.LoadFile(
            Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json"));

        (string Lab, string Scenario)[] fromTheGame =
            [("Earth", "earth"), ("Mars", "mars"), ("Luna", "luna"), ("Phobos", "phobos")];

        foreach ((string labName, string id) in fromTheGame)
        {
            TetherBody lab = Beanstalk.Bodies.First(b => b.Name == labName);
            BodyDefinition game = sol.Bodies.First(b => b.Id == id);

            Assert.Equal(game.Mu, lab.Mu);
            Assert.Equal(game.BodyRadiusM, lab.Radius);
            Assert.Contains("sol.json", lab.Source, StringComparison.Ordinal);

            if (lab.Anchor == AnchorKind.ThroughL1)
            {
                Assert.Equal(game.OrbitRadiusM, lab.PrimaryDistance);
                BodyDefinition primary = sol.Bodies.First(b => b.Id == game.ParentId);
                Assert.Equal(primary.Mu, lab.PrimaryMu);
            }
        }

        // …and the two the game does NOT carry must not be claiming that it does.
        foreach (string outside in new[] { "Ceres", "Deimos" })
        {
            TetherBody lab = Beanstalk.Bodies.First(b => b.Name == outside);
            Assert.DoesNotContain("sol.json", lab.Source, StringComparison.Ordinal);
            Assert.DoesNotContain(sol.Bodies, b => b.Id.Equals(outside, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>The arithmetic against the world, not against itself. These four are published numbers with
    /// a century of independent derivations behind them; if the lab's geometry ever drifts, it drifts here
    /// first and every taper downstream is wrong by the same factor.</summary>
    [Fact]
    public void TheGeometryAgreesWithThePublishedFigures()
    {
        Climb earth = ClimbFor("Earth");
        Assert.InRange(earth.AnchorAltitude, 35.70e6, 35.90e6);          // geostationary, ~35,786 km
        Assert.InRange(earth.DeltaPotential, 4.80e7, 4.90e7);            // Pearson's ~48.5 MJ/kg
        Assert.InRange(earth.CharacteristicLength, 4.85e6, 5.00e6);      // the classic ~4,960 km

        Climb mars = ClimbFor("Mars");
        Assert.InRange(mars.AnchorAltitude, 16.9e6, 17.2e6);             // areostationary, ~17,032 km

        // The Earth-Moon L1 is quoted at ~58,000 km from the Moon's centre; the lab bisects for it rather
        // than using the cube-root approximation, so it should land on the published figure, not near it.
        Climb luna = ClimbFor("Luna");
        Assert.InRange(luna.AnchorRadius, 57.5e6, 58.5e6);

        // Phobos's whole finding is geometric: its Hill sphere barely clears its own surface, so the tether
        // is a handful of kilometres long and there is nowhere else for it to go.
        TetherBody phobos = Beanstalk.Bodies.First(b => b.Name == "Phobos");
        Assert.InRange(Beanstalk.HillRadius(phobos), 16.0e3, 17.5e3);
        Assert.InRange(ClimbFor("Phobos").AnchorAltitude, 4.0e3, 7.0e3);
    }

    /// <summary>THE LAW, HALF ONE. Every printed verdict is a property of that body's own taper column —
    /// re-stated here from this file's literal thresholds, so moving the lab's threshold moves the claim
    /// away from the guard rather than dragging the guard along with it.</summary>
    [Fact]
    public void EveryVerdictIsWhatItsOwnTaperNumbersSay()
    {
        TetherMaterial steel = Beanstalk.Materials[0];
        IReadOnlyList<TetherMaterial> commercial =
            [.. Beanstalk.Materials.Where(m => m.Tier == MaterialTier.Commercial)];
        IReadOnlyList<TetherMaterial> real = [.. Beanstalk.Materials.Where(m => m.IsReal)];

        Assert.Equal("steel wire", steel.Name);
        Assert.True(commercial.Count >= 2 && real.Count > commercial.Count,
            "the material list has stopped having both a buyable tier and a laboratory tier, and the grades " +
            "between them can no longer mean anything.");

        foreach (Climb c in Beanstalk.MeasureAll())
        {
            double steelTaper = Beanstalk.Taper(c.DeltaPotential, steel);
            double bestReal = real.Min(m => Beanstalk.Taper(c.DeltaPotential, m));
            double bestCommercial = commercial.Min(m => Beanstalk.Taper(c.DeltaPotential, m));
            string verdict = Beanstalk.Verdict(c.DeltaPotential);
            string where = $"{c.Body.Name} (dPhi {c.DeltaPotential:F0} J/kg, steel taper {steelTaper:G4}, " +
                           $"best real {bestReal:G4}, best commercial {bestCommercial:G4}) says \"{verdict}\"";

            switch (verdict)
            {
                case Beanstalk.VerdictRope:
                    Assert.True(steelTaper <= RoundingErrorTaper,
                        $"{where}, but steel wire does not even come in under {RoundingErrorTaper}.");
                    break;

                case Beanstalk.VerdictSteel:
                    Assert.True(steelTaper > RoundingErrorTaper && steelTaper <= PracticalTaper,
                        $"{where}, but steel's taper is outside the band that grade names.");
                    break;

                case Beanstalk.VerdictFibreToday:
                    Assert.True(steelTaper > PracticalTaper,
                        $"{where}, but steel wire already builds it — the lab is under-selling its own body.");
                    Assert.True(bestCommercial <= PracticalTaper,
                        $"{where}, but no material you can buy gets under taper {PracticalTaper}.");
                    break;

                case Beanstalk.VerdictOnlyBest:
                    Assert.True(bestCommercial > PracticalTaper,
                        $"{where}, but a commercial fibre builds it — that is a weaker claim than the truth.");
                    Assert.True(bestReal <= PracticalTaper,
                        $"{where}, but nothing anyone has made gets under taper {PracticalTaper}.");
                    break;

                case Beanstalk.VerdictBeyond:
                    Assert.True(bestReal > PracticalTaper,
                        $"{where}, but the best real material tapers {bestReal:G4} — it is not beyond anything.");
                    break;

                default:
                    Assert.Fail($"{c.Body.Name} printed a verdict this guard has never heard of: \"{verdict}\".");
                    break;
            }
        }
    }

    /// <summary>THE LAW, HALF TWO. …and the grade is the one the lab actually published.</summary>
    [Fact]
    public void EveryVerdictIsTheOneTheLabPublished()
    {
        foreach ((string body, string expected) in Published)
        {
            Assert.Equal(expected, Beanstalk.Verdict(ClimbFor(body).DeltaPotential));
        }
    }

    /// <summary>The README is the product, and labs/README.md's ironclad rule is that its numbers came from
    /// running the probe. So the shipped README must carry the verdict lines VERBATIM as the probe prints
    /// them — the sentence and the two numbers it was read off, together, or not at all.</summary>
    [Fact]
    public void TheReadmeCarriesTheVerdictLinesTheProbePrinted()
    {
        string readme = File.ReadAllText(Path.Combine(LabsDirectory(), "31-the-beanstalk", "README.md"));

        foreach (Climb c in Beanstalk.MeasureAll())
        {
            string line = Beanstalk.VerdictLine(c);
            Assert.True(readme.Contains(line, StringComparison.Ordinal),
                $"labs/31-the-beanstalk/README.md no longer carries the line the probe prints for {c.Body.Name}. " +
                $"Rerun the probe and re-paste; never hand-edit a table. Expected verbatim:\n  {line}");
        }
    }

    /// <summary>The flagship is named by the rule, not by taste: of the bodies a cable could be spun for out
    /// of stock material, the one that saves the most propellant. Re-derived here, and then held to the
    /// shape the lab's whole argument depends on — that the biggest prize is NOT the flagship.</summary>
    [Fact]
    public void TheFlagshipIsTheOneTheNumbersName()
    {
        IReadOnlyList<Climb> climbs = Beanstalk.MeasureAll();
        IReadOnlyList<TetherMaterial> commercial =
            [.. Beanstalk.Materials.Where(m => m.Tier == MaterialTier.Commercial)];

        Climb expected = climbs
            .Where(c => commercial.Any(m => Beanstalk.Taper(c.DeltaPotential, m) <= PracticalTaper))
            .MaxBy(c => c.PropellantPerTonneKg)!;

        Assert.Equal(expected.Body.Name, Beanstalk.Flagship().Body.Name);
        Assert.Equal("Luna", Beanstalk.Flagship().Body.Name);

        // The finding, as a shape: the richest body on the board is unbuildable, and the buildable ones get
        // cheaper to serve exactly as the cable gets easier. If that ever inverts, the lab has a new lesson.
        Climb richest = climbs.MaxBy(c => c.PropellantPerTonneKg)!;
        Assert.Equal("Earth", richest.Body.Name);
        Assert.Equal(Beanstalk.VerdictBeyond, Beanstalk.Verdict(richest.DeltaPotential));
        Assert.True(richest.PropellantPerTonneKg > 10 * Beanstalk.Flagship().PropellantPerTonneKg,
            "Earth is supposed to be the prize nobody can take — if the flagship is within an order of " +
            "magnitude of it, section E has stopped making its point.");
    }

    /// <summary>The exponential, sanity-checked in both directions: a harder climb always tapers worse, and
    /// a stronger material always tapers better. Cheap, and it catches an inverted sign the day it lands.</summary>
    [Fact]
    public void TaperRisesWithTheClimbAndFallsWithStrength()
    {
        IReadOnlyList<Climb> byClimb = [.. Beanstalk.MeasureAll().OrderBy(c => c.DeltaPotential)];
        foreach (TetherMaterial m in Beanstalk.Materials)
        {
            double previous = 0;
            foreach (Climb c in byClimb)
            {
                double taper = Beanstalk.Taper(c.DeltaPotential, m);
                Assert.True(taper >= previous, $"{m.Name} tapers less at {c.Body.Name} than at an easier body.");
                Assert.True(taper >= 1.0, $"{m.Name} at {c.Body.Name} tapers below 1 — the cable is thinner at " +
                                          "the waist than at the anchor, which is not what tension does.");
                previous = taper;
            }
        }

        Climb earth = ClimbFor("Earth");
        double weak = Beanstalk.Taper(earth.DeltaPotential, Beanstalk.Materials[0]);
        double strong = Beanstalk.Taper(earth.DeltaPotential, Beanstalk.BestReal);
        Assert.True(strong < weak, "the stronger material tapers worse than steel wire.");
        Assert.Equal("CNT fibre, best spun", Beanstalk.BestReal.Name);
    }
}

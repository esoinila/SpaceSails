using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #541 · THE DOCKING TUBE, AS LAWS. Owner, photographing a long glazed gangway while boarding at Tallinn:
/// <i>"This kind of tunnel when docking the ship could be used in gen-ai to tell how big the docked place is."</i>
///
/// <para>These run against the REAL <c>sol.json</c> rather than a fixture, because the whole claim of this module
/// is that the tier is derived from traffic somebody already wrote down. A fixture would let the rule and the
/// scenario drift apart, and the interesting result — that the tube says the economy is not Earth-centred — is
/// only interesting if it comes out of the shipped numbers.</para>
/// </summary>
public sealed class ArrivalTubeTests
{
    private static ICelestialEphemeris Sol() =>
        CircularOrbitEphemeris.FromScenario(ScenarioLoader.LoadFile(
            Path.Combine(AppContext.BaseDirectory, "scenarios", "sol.json")));

    /// <summary>
    /// THE HEADLINE RESULT, and the reason this rule was worth deriving rather than authoring: the tube comes out
    /// saying exactly what the owner's own worldbuilding note in <c>sol.json</c> says — <i>"the busiest lanes …
    /// are the scoop worlds feeding Jupiter's energy-hungry habitat mega-works at Europa, not commutes from Earth.
    /// Earth is one node among several."</i>
    ///
    /// <para>So the grand gangways are at Saturn and Jupiter, and <b>Selene Gate does not get one.</b> Had this
    /// been authored by hand, Earth would have got the big tube out of habit and quietly contradicted the setting.</para>
    /// </summary>
    [Theory]
    [InlineData("ringside-exchange", ArrivalTube.Tier.GreatPort)]    // Saturn: 23 scheduled
    [InlineData("red-eye", ArrivalTube.Tier.GreatPort)]              // Jovian space: 19 — the mega-works
    [InlineData("selene-gate", ArrivalTube.Tier.WorkingBerth)]       // Earth: 10, and that is the point
    [InlineData("the-space-bar", ArrivalTube.Tier.WorkingBerth)]     // Mars: 10
    [InlineData("cinder-roost", ArrivalTube.Tier.WorkingBerth)]      // Venus: 2
    [InlineData("the-tilt", ArrivalTube.Tier.Outpost)]               // Uranus: discreet haulers only
    [InlineData("the-deep", ArrivalTube.Tier.Outpost)]               // Neptune: discreet haulers only
    public void EveryBerthInSolGetsTheTubeItsTrafficEarned(string havenId, ArrivalTube.Tier expected) =>
        Assert.Equal(expected, ArrivalTube.TierFor(Sol(), havenId));

    /// <summary>
    /// THE FLAG IS THE RULE. Owner's scenario marks the outer-reaches He3 runs
    /// <c>publishesTimetable: false</c> — discreet haulers out of pirate country. That flag turns out to be a
    /// better answer than any new "size" field: <b>a place served only by ships that are not on a board has no
    /// queue, so it has no gangway.</b> The Tilt and The Deep carry real tonnage and still get a soft collar.
    /// </summary>
    [Theory]
    [InlineData("the-tilt")]
    [InlineData("the-deep")]
    public void ADiscreetlyBusyBerthIsStillAnOutpost(string havenId)
    {
        ICelestialEphemeris sol = Sol();

        Assert.Equal(0, ArrivalTube.ScheduledTonnage(sol, havenId));
        Assert.True(ArrivalTube.DiscreetTonnage(sol, havenId) > 0,
                    "these are not empty berths — they are unlisted ones");
        Assert.Equal(ArrivalTube.Tier.Outpost, ArrivalTube.TierFor(sol, havenId));
    }

    /// <summary>
    /// A BERTH IS BUSY BECAUSE ITS NEIGHBOURHOOD IS. No route in <c>sol.json</c> names the Red Eye at all — every
    /// Jovian run terminates at Europa or Ganymede, because that is where the work is. A rule that only counted
    /// routes naming the station itself would have called the busiest place in the outer system a backwater.
    /// </summary>
    [Fact]
    public void TheNeighbourhoodCountsAndNotJustTheBerthsOwnName()
    {
        ICelestialEphemeris sol = Sol();

        Assert.True(ArrivalTube.ScheduledTonnage(sol, "red-eye") >= ArrivalTube.GreatPortTonnage);
        // …and it is genuinely the moons doing it: Jupiter itself appears on exactly one scheduled run.
        Assert.True(ArrivalTube.ScheduledTonnage(sol, "red-eye") > 3);
    }

    /// <summary>Both ends count. A berth things ARRIVE at is as busy as one they leave from, and Selene Gate is
    /// mostly an arrival.</summary>
    [Fact]
    public void ArrivingTonnageCountsAsMuchAsDepartingTonnage() =>
        Assert.True(ArrivalTube.ScheduledTonnage(Sol(), "selene-gate") > 0);

    /// <summary>A scenario with no traffic section makes no claim about anybody's importance — everything is an
    /// outpost, which is the honest default rather than a crash.</summary>
    [Fact]
    public void AScenarioWithNoTrafficSaysNothingAboutAnybody()
    {
        ICelestialEphemeris bare = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sun", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("nowhere", "Nowhere", "sun", 0, 500, 1e11, 3.15e7, 0, IsHaven: true),
        ]);

        Assert.Equal(0, ArrivalTube.ScheduledTonnage(bare, "nowhere"));
        Assert.Equal(ArrivalTube.Tier.Outpost, ArrivalTube.TierFor(bare, "nowhere"));
    }

    /// <summary>And an id nobody has heard of is an outpost too, rather than an exception thrown at a docking.</summary>
    [Fact]
    public void AnUnknownBerthDoesNotThrowAtTheWorstPossibleMoment() =>
        Assert.Equal(ArrivalTube.Tier.Outpost, ArrivalTube.TierFor(Sol(), "no-such-berth"));

    // ── What it shows ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every tier has a canvas, a heading and a caption, and no two tiers share any of them — three
    /// identical plates would be worse than none.</summary>
    [Fact]
    public void EachTierIsDistinctInEveryColumnItFills()
    {
        ArrivalTube.Tier[] tiers = Enum.GetValues<ArrivalTube.Tier>();

        Assert.Equal(tiers.Length, tiers.Select(ArrivalTube.ArtFile).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(tiers.Length, tiers.Select(ArrivalTube.Title).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(tiers.Length, tiers.Select(ArrivalTube.Caption).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(tiers.Length, tiers.Select(ArrivalTube.WalkLine).Distinct(StringComparer.Ordinal).Count());

        foreach (ArrivalTube.Tier t in tiers)
        {
            Assert.StartsWith("art/", ArrivalTube.ArtFile(t), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(ArrivalTube.Caption(t)));
        }
    }

    /// <summary>
    /// THE PART THAT IS A RULE RATHER THAN A PICTURE. The tube is how far you walk with contraband before anybody
    /// can ask about it, so each tier's walk line has to state the exposure plainly — a great port's queue is
    /// something you cannot step out of, and an outpost has nobody to run one.
    /// </summary>
    [Fact]
    public void TheWalkLineNamesTheExposureAndNotTheArchitecture()
    {
        Assert.Contains("queue", ArrivalTube.WalkLine(ArrivalTube.Tier.GreatPort), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no one to run it", ArrivalTube.WalkLine(ArrivalTube.Tier.Outpost), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A docking must never be held up by scene-setting: it is a PLATE, at the seam's own length.</summary>
    [Fact]
    public void ItIsAPlateAndNotADecision() =>
        Assert.Equal(StoryBeats.PlateSeconds, ArrivalTube.PlateSeconds);

    /// <summary>The threshold sits where it actually separates the set — above Earth and Mars (10) and below
    /// Jovian space (19). If someone moves it outside that gap the whole distinction collapses to one tier, so the
    /// gap itself is pinned rather than the number.</summary>
    [Fact]
    public void TheThresholdSitsInTheGapItIsMeantToSplit()
    {
        ICelestialEphemeris sol = Sol();
        double earth = ArrivalTube.ScheduledTonnage(sol, "selene-gate");
        double jovian = ArrivalTube.ScheduledTonnage(sol, "red-eye");

        Assert.True(earth < ArrivalTube.GreatPortTonnage, $"Earth ({earth}) must fall below the bar");
        Assert.True(jovian >= ArrivalTube.GreatPortTonnage, $"Jovian space ({jovian}) must clear it");
    }
}

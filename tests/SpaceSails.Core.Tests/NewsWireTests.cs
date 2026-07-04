namespace SpaceSails.Core.Tests;

public class NewsWireTests
{
    private const double Day = NewsWire.SecondsPerDay;

    private static CircularOrbitEphemeris SolEphemeris() => CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    // ---- Ambient: determinism ----

    [Fact]
    public void Ambient_SameScenarioSameDay_ProducesIdenticalHeadline()
    {
        var ephemeris = SolEphemeris();

        var first = NewsWire.Ambient(ephemeris, 10 * Day, 1);
        var second = NewsWire.Ambient(ephemeris, 10 * Day + 12345, 1); // still day 10 — same headline

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(first[0].Headline, second[0].Headline);
        Assert.Equal(first[0].SimTime, second[0].SimTime);
    }

    [Fact]
    public void Ambient_DifferentDays_TypicallyDiffer()
    {
        var ephemeris = SolEphemeris();

        // Across a wide spread of days, at least some headlines should differ — otherwise the
        // "wire" is frozen, which would defeat the point of a rotating feed.
        var headlines = new HashSet<string>();
        for (int day = 0; day < 30; day++)
        {
            headlines.Add(NewsWire.Ambient(ephemeris, day * Day, 1)[0].Headline);
        }

        Assert.True(headlines.Count > 1, "Expected the ambient wire to vary across 30 different sim-days.");
    }

    [Fact]
    public void Ambient_NewestFirst_OneItemPerDay()
    {
        var ephemeris = SolEphemeris();

        var items = NewsWire.Ambient(ephemeris, 100 * Day + 500, 5);

        Assert.Equal(5, items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            Assert.Equal((100 - i) * Day, items[i].SimTime);
        }
    }

    [Fact]
    public void Ambient_ZeroOrNegativeCount_ReturnsEmpty()
    {
        var ephemeris = SolEphemeris();

        Assert.Empty(NewsWire.Ambient(ephemeris, 0, 0));
        Assert.Empty(NewsWire.Ambient(ephemeris, 0, -3));
    }

    [Fact]
    public void Ambient_DifferentScenarios_CanProduceDifferentHeadlines()
    {
        // Same sim-day, different ephemeris content (one body vs. the full Sol scenario) — the
        // function must be sensitive to the scenario, not just the day, without ever throwing.
        var solHeadline = NewsWire.Ambient(SolEphemeris(), 5 * Day, 1)[0].Headline;

        var tiny = new CircularOrbitEphemeris([
            new CelestialBody("center", "Center", null, 0, 0, 0, 0, 0),
            new CelestialBody("outpost", "Lonely Outpost", "center", 0, 0, 1000, 400, 0),
        ]);
        var tinyHeadline = NewsWire.Ambient(tiny, 5 * Day, 1)[0].Headline;

        Assert.False(string.IsNullOrWhiteSpace(solHeadline));
        Assert.False(string.IsNullOrWhiteSpace(tinyHeadline));
    }

    [Fact]
    public void Ambient_NoNamedBodies_StillProducesHeadline()
    {
        // Only the parentless "sun" — no body template can fire, but Ambient must degrade
        // gracefully to the flat/cargo pools rather than throwing.
        var sunOnly = new CircularOrbitEphemeris([
            new CelestialBody("sun", "Sol", null, 0, 0, 0, 0, 0),
        ]);

        var item = NewsWire.Ambient(sunOnly, 42 * Day, 1)[0];

        Assert.False(string.IsNullOrWhiteSpace(item.Headline));
    }

    [Fact]
    public void Ambient_SingleNamedBody_NeverUsesRouteTemplate()
    {
        // Only one named body — a two-body route template would need to reference the same body
        // twice or throw a divide/mod-by-zero; this must never happen with exactly one body.
        var oneBody = new CircularOrbitEphemeris([
            new CelestialBody("center", "Center", null, 0, 0, 0, 0, 0),
            new CelestialBody("outpost", "Lonely Outpost", "center", 0, 0, 1000, 400, 0),
        ]);

        for (int day = 0; day < 60; day++)
        {
            string headline = NewsWire.Ambient(oneBody, day * Day, 1)[0].Headline;
            Assert.False(string.IsNullOrWhiteSpace(headline));
        }
    }

    // ---- Event headlines ----

    [Fact]
    public void Headline_RobberyCommitted_NamesTheShip()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.RobberyCommitted, 1000, "SS Meridian");

        Assert.Contains("SS Meridian", NewsWire.Headline(evt));
    }

    [Fact]
    public void Headline_HunterDispatched_NamesHunterAndOrigin()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.HunterDispatched, 1000, "RSS Reprisal", "Mars");

        string headline = NewsWire.Headline(evt);
        Assert.Contains("RSS Reprisal", headline);
        Assert.Contains("Mars", headline);
    }

    [Fact]
    public void Headline_HunterDispatched_NoDetail_StillProducesHeadline()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.HunterDispatched, 1000, "RSS Reprisal");

        Assert.False(string.IsNullOrWhiteSpace(NewsWire.Headline(evt)));
    }

    [Fact]
    public void Headline_IntelPurchased_NamesTheShip()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.IntelPurchased, 1000, "Nightfarer");

        Assert.Contains("Nightfarer", NewsWire.Headline(evt));
    }

    [Fact]
    public void Headline_OrbitEnteredHaven_NamesTheHaven()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.OrbitEnteredHaven, 1000, "Enceladus");

        Assert.Contains("Enceladus", NewsWire.Headline(evt));
    }

    [Fact]
    public void Headline_IsDeterministic_SameEventSameText()
    {
        var evt = new NewsWire.NewsEvent(NewsWire.NewsEventKind.RobberyCommitted, 1000, "SS Meridian");

        Assert.Equal(NewsWire.Headline(evt), NewsWire.Headline(evt));
    }
}

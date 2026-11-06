using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Round-2 test user U02 (Dov) · the Core-side findings of checklist #1416, each asked of the real
/// generator and swept over the seed space (a guard over a seeded draw tested with one sample proves nothing).
/// </summary>
public class U02RoundTwoCoreTests
{
    private const double Day = NewsWire.SecondsPerDay;

    private static CircularOrbitEphemeris Sol() => CircularOrbitEphemeris.FromScenario(TestTree.Sol);

    /// <summary>U02-F12 · "A Ice shipment", "A Alloys shipment", "the The Tilt-Triton run" - on every masthead,
    /// on every day of a long sweep, for several sites.</summary>
    [Fact]
    public void TheWireNeverMisspellsAnArticle()
    {
        var wrongArticle = new Regex(@"\bA [AEIOU]\w* shipment|\bthe The\b|\bThe The\b", RegexOptions.CultureInvariant);
        int shipments = 0, theBodies = 0;
        foreach ((NewsWire.NewsScope scope, string? salt) in new (NewsWire.NewsScope, string?)[]
                 { (NewsWire.NewsScope.SystemWire, null), (NewsWire.NewsScope.PortRag, "ceres"), (NewsWire.NewsScope.PortRag, "pallas") })
        {
            foreach (NewsWire.NewsItem item in NewsWire.Ambient(Sol(), 2000 * Day, 2000, scope, salt))
            {
                Assert.DoesNotMatch(wrongArticle, item.Headline);
                if (item.Headline.Contains("shipment", StringComparison.Ordinal)) shipments++;
                if (item.Headline.Contains("the Tilt", StringComparison.Ordinal)
                    || item.Headline.Contains("the Clinker", StringComparison.Ordinal)) theBodies++;
            }
        }

        // The premise: the sweep really met the shapes the rule is about.
        Assert.True(shipments > 50, $"only {shipments} shipment lines - the sweep proved little.");
        Assert.True(theBodies > 0, "no line ever named a 'The ...' body after 'the' - the sweep proved little.");
        Assert.Contains(NewsWire.Ambient(Sol(), 2000 * Day, 2000).Select(i => i.Headline),
            h => h.Contains("An Ice shipment", StringComparison.Ordinal) || h.Contains("An Alloys shipment", StringComparison.Ordinal));
    }

    /// <summary>U02-F12 · One ticker never prints the same line twice, and keeps the newest copy.</summary>
    [Fact]
    public void ATickerNeverRepeatsALine()
    {
        int blendsWithARepeat = 0;
        for (int start = 20; start < 400; start++)
        {
            var blend = NewsWire.Ambient(Sol(), start * Day, 26, NewsWire.NewsScope.PortRag, "cinder-roost").ToList();
            if (blend.Select(b => b.Headline).Distinct().Count() < blend.Count)
            {
                blendsWithARepeat++;
            }

            List<NewsWire.NewsItem> kept = NewsWire.WithoutRepeats(blend);
            Assert.Equal(kept.Count, kept.Select(k => k.Headline).Distinct().Count());
            foreach (IGrouping<string, NewsWire.NewsItem> g in blend.GroupBy(b => b.Headline))
            {
                Assert.Contains(g.First(), kept); // newest-first input: the newest copy survives
            }
        }

        Assert.True(blendsWithARepeat > 0, "no blend in the sweep ever repeated - the guard proved nothing.");
    }

    /// <summary>U02-F22 · A hull that is not holding an orbit never says "the orbit's holding steady".</summary>
    [Fact]
    public void ACoastingShipIsNeverToldTheOrbitHolds()
    {
        for (ulong seed = 0; seed < 500; seed++)
        {
            Assert.DoesNotContain("orbit", DrinkExcuses.LineFor(seed, holdingOrbit: false), StringComparison.OrdinalIgnoreCase);
        }

        // The premise: in orbit the line still exists and is reachable.
        Assert.Contains(Enumerable.Range(0, 500).Select(s => DrinkExcuses.LineFor((ulong)s)),
            l => l.Contains("orbit", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>U02-F21 · The locked-door card prints the sign as its title, so the body never opens with it.</summary>
    [Fact]
    public void TheLockedDoorBodyDoesNotRepeatItsTitle()
    {
        int signs = 0;
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            foreach (string sign in UndergroundComplex.SignsFor(kind))
            {
                signs++;
                string body = UndergroundComplex.LockedLine(sign);
                Assert.DoesNotContain(sign, body, StringComparison.Ordinal);
                Assert.False(body.StartsWith("🔒", StringComparison.Ordinal), $"“{sign}”: the body opens with the title's lock glyph.");
            }
        }

        Assert.True(signs > 10, "the sweep met too few signs to prove anything.");
        string plated = UndergroundComplex.LockedLine(StopOrder.Plate);
        Assert.Equal(StopOrder.OrderLine, plated);
    }

    /// <summary>U02-F04 · The park's FLOOR wears the lawn band, not the whole eye-level photograph: the
    /// asset's own shape is the constant's, and the garden's canvas keeps the full picture.</summary>
    [Fact]
    public void TheParkFloorWearsTheLawnBandNotTheWholePhotograph()
    {
        Assert.NotEqual(UndergroundComplex.ParkArtUrl, UndergroundComplex.ParkFloorArtUrl);
        Assert.Equal(UndergroundComplex.ParkFloorArtUrl, UndergroundComplex.ParkArtFor("luna", UndergroundComplex.Comfort.UpperCanteen));

        string root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "SpaceSails.slnx")))
        {
            root = Path.GetDirectoryName(root)!;
        }

        (int w, int h) = JpegSize(Path.Combine(root!, "src", "SpaceSails.Client", "wwwroot", UndergroundComplex.ParkFloorArtUrl));
        Assert.Equal(UndergroundComplex.ParkArtAspect, (double)w / h, 2);
        (int fw, int fh) = JpegSize(Path.Combine(root!, "src", "SpaceSails.Client", "wwwroot", UndergroundComplex.ParkArtUrl));
        Assert.True((double)fw / fh > 1.7 && (double)w / h < 3.5, "the floor strip must be the cropped band, the canvas the full frame.");
    }

    private static (int W, int H) JpegSize(string path)
    {
        byte[] b = File.ReadAllBytes(path);
        int i = 2;
        while (i + 9 < b.Length)
        {
            if (b[i] != 0xFF) { i++; continue; }
            byte marker = b[i + 1];
            int len = (b[i + 2] << 8) | b[i + 3];
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                return ((b[i + 7] << 8) | b[i + 8], (b[i + 5] << 8) | b[i + 6]);
            }

            i += 2 + len;
        }

        throw new InvalidOperationException("no SOF marker in " + path);
    }
}

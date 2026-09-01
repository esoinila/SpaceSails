using System.Text.RegularExpressions;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1052 (slice L1) · THE TWO SHIPPED READERS DID NOT MOVE.
///
/// <para><c>NewsFeed(...)</c> gained a masthead. Two consumers already ship against it — the Galley
/// desk's long news card (key 6) and the Comms desk's ticker — and the slice's whole promise is that
/// their output is byte-identical to what it was before. Core's
/// <c>NewsScopeTests.AmbientSystemWire_ReadsExactlyAsItDidBefore</c> pins the ambient stream itself
/// against forty pre-#1052 headlines; that only matters if these two callers are still ASKING for the
/// system wire, which is what this file checks.</para>
///
/// <para>A source scan, in this project's shipped idiom (see <c>BothArcsBreakOnTheWireTests</c>): the
/// client test project has no renderer, so "this seam is wired the way it claims" is read off the
/// source. Two halves, both required — the default must be <c>SystemWire</c>, and neither call site
/// may override it.</para>
/// </summary>
public sealed class TheGalleyAndTheTickerStillReadTheSystemWireTests
{
    private static string ClientDir()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            at = at.Parent;
        }

        throw new DirectoryNotFoundException($"could not find the client above {AppContext.BaseDirectory}");
    }

    private static string Source(string relative) =>
        File.ReadAllText(Path.Combine(ClientDir(), "Pages", relative));

    /// <summary>Half one: the parameter's default is the anonymous system-wide wire, so a caller that
    /// passes nothing gets exactly the pre-#1052 feed.</summary>
    [Fact]
    public void NewsFeedDefaultsToTheSystemWire()
    {
        string src = Source("Map.Alerts.cs");

        Assert.Matches(
            new Regex(@"NewsWire\.NewsScope\s+scope\s*=\s*NewsWire\.NewsScope\.SystemWire", RegexOptions.CultureInvariant),
            src);
    }

    /// <summary>
    /// Half two: neither shipped call site asks for anything else. Every <c>NewsFeed(</c> call in the
    /// rendered page must be the bare one-argument form — the day one of them starts passing a masthead is
    /// the day the galley card quietly changes what the player reads, and that is a decision, not a
    /// refactor.
    ///
    /// <para>#1052 (L2) · <b>A THIRD READER JOINED, and it is the one this guard was written to admit.</b>
    /// The seated news panel asks for a masthead ON PURPOSE — that is the whole of the seat verb — and it
    /// asks through the page's own <c>SeatedNewsFeed()</c>, which is where the scope and the salt are
    /// composed (Map.Seated.News.cs). The pattern is anchored with <c>(?&lt;![A-Za-z])</c> so that named
    /// call is not miscounted as a bare <c>NewsFeed(</c>: without the anchor this guard read
    /// <c>SeatedNewsFeed()</c> as a third shipped consumer calling with no arguments, which is the
    /// opposite of what it does. The two shipped readers are still exactly two, and still bare.</para>
    /// </summary>
    [Fact]
    public void BothShippedConsumersCallNewsFeedWithNoScope()
    {
        string page = Source("Map.razor");

        var calls = Regex.Matches(
            page, @"(?<![A-Za-z])NewsFeed\((?<args>[^)]*)\)", RegexOptions.CultureInvariant);
        Assert.Equal(2, calls.Count); // the Galley card and the Comms ticker — the seat verb has its own

        var offenders = new List<string>();
        foreach (Match call in calls)
        {
            string args = call.Groups["args"].Value;
            if (args.Contains(',', StringComparison.Ordinal) ||
                args.Contains("NewsScope", StringComparison.Ordinal))
            {
                offenders.Add(call.Value);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "A shipped news consumer started asking for a masthead (#1052 L1 promises both are unchanged):\n" +
            string.Join("\n", offenders));
    }

    /// <summary>And the two readers are still the two we think they are: the Galley's long scrollback
    /// and the Comms ticker's short slice, each on its own ambient-day budget.</summary>
    [Fact]
    public void TheTwoReadersAreStillTheGalleyCardAndTheCommsTicker()
    {
        string page = Source("Map.razor");

        Assert.Contains("NewsFeed(GalleyFeedAmbientDays)", page, StringComparison.Ordinal);
        Assert.Contains("NewsFeed(CommsTickerAmbientDays)", page, StringComparison.Ordinal);
    }

    /// <summary>
    /// #1052 (L2) · <b>…AND THE THIRD READER IS THE ONE THAT ASKS.</b>
    ///
    /// <para>The counterpart to the two guards above, and it exists so that "nobody passes a masthead" can
    /// never be satisfied by nobody passing one AT ALL. The seat verb's whole point is that it asks the
    /// place what it prints; a seated panel that quietly fell back to the bare call would read the system
    /// wire at a secret lab's canteen table and nothing in this file would have noticed.</para>
    /// </summary>
    [Fact]
    public void TheSeatedReaderAsksForTheMastheadAndTheSalt()
    {
        Assert.Contains("SeatedNewsFeed()", Source("Map.razor"), StringComparison.Ordinal);

        string seat = Source("Map.Seated.News.cs");
        Assert.Contains(
            "NewsFeed(SeatedNewsAmbientDays, NewsWire.ScopeAt(place), NewsWire.SaltFor(place))",
            seat, StringComparison.Ordinal);
    }
}

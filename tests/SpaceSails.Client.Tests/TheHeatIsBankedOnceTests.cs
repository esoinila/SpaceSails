using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #715 · <b>THE CLIENT HALF: FOUR THINGS PUBLISH A CROSSING, ONE THING BANKS IT, AND THE LINE IS SAID AT
/// THEIR DOORS ONLY.</b>
///
/// <para>#929/#931 left the game publishing heat charges that were banked nowhere. The failure mode this lane
/// has to be guarded against is not "the meter is wrong" — Core's own guards cover that — it is <b>a source
/// quietly dropped, or a crossing counted twice</b>, which is invisible to reasoning and invisible to a
/// playtest until an evening's heat is double what it should be.</para>
///
/// <para>So the sweep here is over the SOURCE: every publication of a charge is named, and each one must
/// reach <c>BankTheCrossing</c>/<c>IllegalHeat.Bank</c> exactly once. That is the same instrument
/// <c>EveryStoryBeatHasACallerTests</c> uses on the story beats, pointed at the same class of bug — a thing
/// the game computes and nothing consumes.</para>
/// </summary>
public sealed class TheHeatIsBankedOnceTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    private static int Count(string haystack, string needle)
    {
        int n = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal);
             i >= 0;
             i = haystack.IndexOf(needle, i + 1, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    /// <summary>Every file in the client, with the build leftovers left out.</summary>
    private static IEnumerable<(string Path, string Text)> ClientFiles()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client");
        char sep = Path.DirectorySeparatorChar;
        foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) is not (".cs" or ".razor")
                || file.Contains($"{sep}obj{sep}", StringComparison.Ordinal)
                || file.Contains($"{sep}bin{sep}", StringComparison.Ordinal))
            {
                continue;
            }
            yield return (file, File.ReadAllText(file));
        }
    }

    /// <summary>
    /// #715 · <b>EACH PUBLISHED CROSSING IS BANKED, AND BANKED ONCE.</b> Four publications and the round's
    /// two arms; one banking call each, and no seventh call anywhere in the client.
    ///
    /// <para><b>Proven RED</b> by dropping a source (deleting <c>BankTheCrossing(pressed.Charge)</c> from
    /// <c>PressTheHit</c>, which is exactly how #929 shipped):</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 1
    /// Actual:   0
    /// the SDR press publishes a charge that nothing banks — #929's own gap, re-opened.
    /// </code>
    ///
    /// <para>…and <b>RED the other way</b> by double-banking (adding a second
    /// <c>BankTheCrossing(sent.Charge)</c> beside the first in <c>SendTheStanding</c>):</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 1
    /// Actual:   2
    /// the refused send is charged twice for one press.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryPublishedCrossingIsBankedExactlyOnce()
    {
        string remote = Read("src", "SpaceSails.Client", "Pages", "Map.Combat.Remote.cs");
        string scan = Read("src", "SpaceSails.Client", "Pages", "Map.Scan.cs");
        string hive = Read("src", "SpaceSails.Client", "Pages", "Map.Surface.Hive.cs");
        string floor = Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Floor.cs");

        // #760 · the refused SEND.
        Assert.Equal(1, Count(remote, "RemoteSend.Send("));
        Assert.Equal(1, Count(remote, "BankTheCrossing(sent.Charge)"));

        // #763 · the refused PRESS.
        Assert.Equal(1, Count(scan, "SdrScanner.Press("));
        Assert.Equal(1, Count(scan, "BankTheCrossing(pressed.Charge)"));

        // #684/#929 · the lift panel's refused card-read. Banked inside the SAME latch the field note and the
        // story card ride (`HiveShaftsRefused.Add`), because pressing one refusing gate eleven times is one
        // refusal somebody wrote down.
        Assert.Equal(1, Count(hive, "BankTheCrossing(UndergroundComplex.RefusedAtTheGate(ex.Stop.Body.Id))"));
        int latch = hive.IndexOf("ex.HiveShaftsRefused.Add(refusedBand)", StringComparison.Ordinal);
        int banked = hive.IndexOf("BankTheCrossing(UndergroundComplex.RefusedAtTheGate", StringComparison.Ordinal);
        Assert.True(latch >= 0 && banked > latch,
            "the gate's charge is banked outside the once-per-shaft latch — eleven presses, eleven charges.");

        // #804/#835 · the round's two, banked on the ONE frame each is decided on.
        Assert.Equal(1, Count(floor, "TheHeatOfBeingWalkedOut(ex, book, simTime, IllegalHeat.Crossing.TheEscort)"));
        Assert.Equal(1, Count(floor, "TheHeatOfBeingWalkedOut(ex, book, simTime, IllegalHeat.Crossing.TheKickOut)"));
        Assert.Equal(1, Count(floor, "IllegalHeat.Bank("));

        // …and there is no seventh banker anywhere in the client. One seam, or the count above proves nothing.
        var bankers = new List<string>();
        foreach ((string path, string text) in ClientFiles())
        {
            int calls = Count(text, "IllegalHeat.Bank(") + Count(text, "BankTheCrossing(");
            if (calls > 0)
            {
                bankers.Add($"{Path.GetFileName(path)}×{calls}");
            }
        }
        bankers.Sort(StringComparer.Ordinal);
        Assert.Equal(
            ["Map.Combat.Remote.cs×1", "Map.IllegalHeat.cs×1", "Map.Scan.cs×1", "Map.Surface.Hive.cs×1",
             "Patrol.Floor.cs×1"],
            bankers);
    }

    /// <summary>
    /// #715 · <b>THE LINE IS DRAWN AT THAT OUTFIT'S SITES AND NOWHERE ELSE.</b> Both directions: it is
    /// gated on an excursion AND on the outfit underfoot having a memory of this captain, and the sentence
    /// itself is Core's constant rather than one typed into the markup.
    ///
    /// <para><b>Proven RED</b> by dropping the per-site clause (<c>@if (_surface is not null)</c> alone) —
    /// the line then shows at every site of every company:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "TheyRememberYouHere)"
    /// </code>
    ///
    /// <para>…and <b>RED the other way</b> by dropping the excursion clause, which would hang a ground line
    /// over the flight deck:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// Not found: "_surface is not null"
    /// </code>
    /// </summary>
    [Fact]
    public void TheLineIsSaidAtTheirDoorsOnly_AndInCoresOwnWords()
    {
        string razor = Read("src", "SpaceSails.Client", "Pages", "Map.razor");
        int at = razor.IndexOf("heat-remembered", StringComparison.Ordinal);
        Assert.True(at > 0, "the illegal-heat line is not drawn anywhere.");

        string block = razor[Math.Max(0, at - 400)..Math.Min(razor.Length, at + 200)];
        Assert.Contains("_surface is not null", block, StringComparison.Ordinal);
        Assert.Contains("TheyRememberYouHere)", block, StringComparison.Ordinal);
        Assert.Contains("IllegalHeat.TheyRememberYouHere", block, StringComparison.Ordinal);

        // §13.8 · it never prints a number, and it never prints the company.
        Assert.DoesNotContain("HeatHere", block, StringComparison.Ordinal);
        foreach (SiteOperator.Operator op in SiteOperator.All)
        {
            Assert.DoesNotContain(op.Name, IllegalHeat.TheyRememberYouHere, StringComparison.Ordinal);
        }
        Assert.DoesNotContain("%", IllegalHeat.TheyRememberYouHere, StringComparison.Ordinal);

        // The page's own question is the two-clause one, so no caller can accidentally widen it.
        string page = Read("src", "SpaceSails.Client", "Pages", "Map.IllegalHeat.cs");
        Assert.Contains(
            "_surface is { } ex && IllegalHeat.TheyRememberYouAt(_contacts, ex.Stop.Body.Id)",
            page, StringComparison.Ordinal);
    }

    /// <summary>
    /// #715 · <b>NO FIELD WAS INVENTED, AND NO HOST MEMBER EITHER.</b> #905's frame ledger sweeps every
    /// instance field of the page into a pinned hash, and <c>ISeatHost</c>/<c>IPatrolHost</c> are pinned at
    /// twenty-eight and twenty-one. The meter lives in the contacts book the page already had, and the round
    /// is handed that book as an ARGUMENT.
    ///
    /// <para><b>Proven RED</b> by giving the page an <c>_illegalHeat</c> field of its own, or the round a
    /// <c>ContactLedger Contacts { get; }</c> on its host:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 21
    /// Actual:   22
    /// </code>
    /// </summary>
    [Fact]
    public void TheMeterInventedNoFieldAndNoHostMember()
    {
        string page = Read("src", "SpaceSails.Client", "Pages", "Map.IllegalHeat.cs");
        Assert.DoesNotContain("private ContactLedger", page, StringComparison.Ordinal);
        Assert.DoesNotContain("private readonly", page, StringComparison.Ordinal);
        Assert.DoesNotContain("{ get; set; }", page, StringComparison.Ordinal);

        // #870 · the two host contracts, counted the way ThePatrolKeepsItsOwnStateTests counts them: the
        // member declarations inside the interface body.
        Assert.Equal(21, InterfaceMembers("src", "SpaceSails.Client", "Pages", "Patrol", "IPatrolHost.cs"));
        Assert.Equal(28, InterfaceMembers("src", "SpaceSails.Client", "Pages", "Seating", "ISeatHost.cs"));

        // The round holds nothing: the book crosses the seam as an argument on the two calls that already
        // crossed it, which is why not one of #905's thirty fingerprints moves.
        string floor = Read("src", "SpaceSails.Client", "Pages", "Patrol", "Patrol.Floor.cs");
        Assert.Contains("SpawnPatrolFor(SurfaceExcursion ex, ContactLedger book)", floor, StringComparison.Ordinal);
        Assert.Contains(
            "AdvancePatrol(double dtRealSeconds, ContactLedger book, double simTime)",
            floor, StringComparison.Ordinal);
        foreach ((string path, string text) in ClientFiles())
        {
            if (Path.GetFileName(path).StartsWith("Patrol.", StringComparison.Ordinal)
                || Path.GetFileName(path) == "Patrol.cs")
            {
                Assert.DoesNotContain("ContactLedger _", text, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>The member declarations inside an interface body — a line ending in <c>;</c> or in
    /// <c>{ get; }</c>/<c>{ get; set; }</c>, at the interface's own indent.</summary>
    private static int InterfaceMembers(params string[] parts)
    {
        string text = Read(parts);
        int open = text.IndexOf("interface I", StringComparison.Ordinal);
        Assert.True(open > 0, $"{parts[^1]} has no interface in it.");
        string body = text[open..];
        return Regex.Matches(body, @"^\s{8}[A-Za-z<>?\[\]\(\), \.]+\s+\w+(\(.*\)\s*;|\s*\{\s*get;.*\}|;)\s*$",
            RegexOptions.Multiline).Count;
    }
}

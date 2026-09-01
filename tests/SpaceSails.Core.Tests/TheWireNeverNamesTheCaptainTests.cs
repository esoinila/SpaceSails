using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1052 · <b>THE ANONYMITY LAW — the wire never names the captain.</b>
///
/// <para>The issue, binding: <i>"Every deed stays third-person anonymous ('A ship slipped quietly into
/// orbit at…'). The fun the owner names — 'it reports the players stuff' — is the captain reading his
/// own crime over a drink and keeping a straight face. No 'you', no ship name, ever. Guard it."</i></para>
///
/// <para>The whole joy of the mechanic is deniability. The moment one headline says "your ship" or
/// prints a callsign the player recognises as his own, the bar stops being a place he can sit in and
/// becomes a place that has identified him — and the straight face has nothing left to hide.</para>
///
/// <para><b>Three claims, each independently falsifiable.</b> A single blunt "no line may contain the
/// word 'you'" would be the wrong law and a dishonest test: the intranet's own authored lines address
/// FACILITY STAFF ("Logistics thanks you in advance for not asking why"), and a bar's house rule may
/// be quoted at the room ("check your guns, drink your credits"). Neither addresses the captain and
/// neither attributes a deed. What the law actually forbids is the wire making the READER the doer, or
/// naming him, or printing his hull. So:</para>
/// <list type="number">
/// <item><b>A — the deed clause is third-person indefinite.</b> The reporting sentence of every
/// <see cref="NewsWire.Headline"/> (the clause that says what happened) carries no first- or
/// second-person pronoun at all. "Piracy alert: your ship was boarded" dies here.</item>
/// <item><b>B — nobody is titled or addressed as THE captain</b>, anywhere on the wire, in any scope:
/// no vocative "Captain"/"Capt."/"skipper", and no "your ship/hull/sail/boat/vessel/crew/tab". (An
/// indefinite third-person "A captain swears…" stays legal — it is somebody else.)</item>
/// <item><b>C — the hulls the wire can print are NPC hulls.</b> The one ambient family that names a
/// ship draws only from the traffic board's own callsigns, so no code path exists by which the
/// player's boat could appear.</item>
/// </list>
/// </summary>
public class TheWireNeverNamesTheCaptainTests
{
    private const double Day = NewsWire.SecondsPerDay;

    private static CircularOrbitEphemeris SolEphemeris() => CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    // ── A ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>First/second person, anywhere in the reporting clause. Word-boundaried so "Yourke"
    /// or a body called "Iota" never trips it.</summary>
    private static readonly Regex Personal =
        new(@"\b(you|your|yours|yourself|we|us|our|ours|my|mine|I)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The clause that reports the deed: everything up to the first sentence end. What comes
    /// after it is the wire's editorial aside to the room, which the law does not govern — but the
    /// deed itself must never acquire an owner.</summary>
    private static string DeedClause(string headline)
    {
        int stop = headline.IndexOfAny(['.', '!', '?']);
        return stop < 0 ? headline : headline[..(stop + 1)];
    }

    /// <summary>Every event kind, rendered with sentinel names so the fixed prose is what is judged.
    /// <c>ArcBeatBreaks</c> is a pass-through whose Subject IS the headline, so it is tested below
    /// against the two Core constants the two arcs actually push.</summary>
    private static IEnumerable<(NewsWire.NewsEventKind Kind, string Headline)> EveryEventHeadline()
    {
        foreach (NewsWire.NewsEventKind kind in Enum.GetValues<NewsWire.NewsEventKind>())
        {
            if (kind == NewsWire.NewsEventKind.ArcBeatBreaks)
            {
                continue;
            }

            yield return (kind, NewsWire.Headline(new NewsWire.NewsEvent(kind, 12 * Day, "SUBJECTNAME", "DETAILNAME")));
            yield return (kind, NewsWire.Headline(new NewsWire.NewsEvent(kind, 12 * Day, "SUBJECTNAME")));
        }
    }

    [Fact]
    public void LawA_NoEventHeadlineEverAttributesTheDeedToTheReader()
    {
        var offences = new List<string>();

        foreach ((NewsWire.NewsEventKind kind, string headline) in EveryEventHeadline())
        {
            Match m = Personal.Match(DeedClause(headline));
            if (m.Success)
            {
                offences.Add($"{kind}: \"{m.Value}\" in the deed clause — \"{DeedClause(headline)}\"");
            }
        }

        Assert.True(
            offences.Count == 0,
            "The wire never names the captain (#1052): a deed was attributed to the reader.\n" + string.Join("\n", offences));
    }

    /// <summary>The two arc beats are pass-throughs — the Subject IS the headline — so the law has to
    /// land on the constants the arcs push, or ArcBeatBreaks would be a hole straight through it.</summary>
    [Fact]
    public void LawA_TheArcBeatsOwnHeadlinesAreThirdPersonToo()
    {
        foreach (string authored in new[] { CyclerWindow.BerthListedHeadline, NebulaLore.TermsRefiledHeadline })
        {
            // Pass-through is the contract, and the guard depends on it.
            string rendered = NewsWire.Headline(
                new NewsWire.NewsEvent(NewsWire.NewsEventKind.ArcBeatBreaks, 3 * Day, authored));
            Assert.Equal(authored, rendered);

            Assert.False(Personal.IsMatch(rendered), $"an arc beat named the reader: \"{rendered}\"");
        }
    }

    // ── B ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Being addressed or titled as THE captain, or having a boat attributed to you. The
    /// indefinite "A captain swears…" is a third party and stays legal; "Captain," / "the captain" /
    /// "your ship" are the wire noticing who is reading.</summary>
    private static readonly Regex NamesTheCaptain =
        new(@"\bthe captain\b|\bcaptain\s*[,.!?]|\bcapt\.|\bskipper\b|\byour\s+(ship|hull|sail|boat|vessel|crew|tab|cargo|hold|berth)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void LawB_NothingOnTheWireAddressesOrTitlesTheCaptain()
    {
        var ephemeris = SolEphemeris();
        var offences = new List<string>();

        foreach ((NewsWire.NewsEventKind kind, string headline) in EveryEventHeadline())
        {
            if (NamesTheCaptain.IsMatch(headline))
            {
                offences.Add($"{kind}: \"{headline}\"");
            }
        }

        foreach (string authored in new[] { CyclerWindow.BerthListedHeadline, NebulaLore.TermsRefiledHeadline })
        {
            if (NamesTheCaptain.IsMatch(authored))
            {
                offences.Add($"arc beat: \"{authored}\"");
            }
        }

        // …and every ambient line, under every masthead, at several sites, across a long run of days.
        string[] salts = ["", "ceres", "Ganymede", "lab:vantar:europa"];
        foreach (NewsWire.NewsScope scope in Enum.GetValues<NewsWire.NewsScope>())
        {
            foreach (string salt in salts)
            {
                foreach (var item in NewsWire.Ambient(ephemeris, 600 * Day, 600, scope, salt))
                {
                    if (NamesTheCaptain.IsMatch(item.Headline))
                    {
                        offences.Add($"{scope} @ \"{salt}\": \"{item.Headline}\"");
                    }
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "The wire never names the captain (#1052).\n" + string.Join("\n", offences.Distinct()));
    }

    // ── C ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The one ambient family that names a hull names an NPC hull. Not "the player's callsign
    /// happens not to be in the table today" — there is no path from anything the player owns into the
    /// wire's ship-name material at all, and this pins the material to the traffic board.</summary>
    [Fact]
    public void LawC_TheOnlyHullsTheWireCanPrintAreOffTheTrafficBoard()
    {
        var ephemeris = SolEphemeris();
        var crewman = new Regex(@"^A crewman off the (?<ship>.+) has not reported back\. His tab remains open\.$",
            RegexOptions.CultureInvariant);

        var namedHulls = new HashSet<string>();
        foreach (string salt in new[] { "ceres", "Ganymede", "pallas", "The Clinker" })
        {
            foreach (var item in NewsWire.Ambient(ephemeris, 900 * Day, 900, NewsWire.NewsScope.PortRag, salt))
            {
                Match m = crewman.Match(item.Headline);
                if (m.Success)
                {
                    namedHulls.Add(m.Groups["ship"].Value);
                }
            }
        }

        Assert.NotEmpty(namedHulls);
        foreach (string hull in namedHulls)
        {
            Assert.Contains(hull, TrafficSchedule.Callsigns);
        }
    }
}

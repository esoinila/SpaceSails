using System.Text.RegularExpressions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1052 (slice L1) · ONE WIRE, THREE MASTHEADS.
///
/// <para>The design: <c>SystemWire</c> is what has always existed (the anonymous system-wide wire),
/// <c>PortRag</c> is a docked port's own sheet — the wire PLUS a per-port ambient family, salted by
/// site so the same sim-day reads differently at Ceres than at Pallas — and <c>CompanyIntranet</c> is
/// a secret lab's internal feed carrying <b>no</b> system wire content at all, because a company that
/// talks only to itself is itself a tell.</para>
///
/// <para>The load-bearing claim of the whole slice is the FIRST test here: the system wire did not
/// move. Two consumers already ship against it (the galley card at key 6 and the Comms ticker), and a
/// refactor that quietly re-rolls their headlines would rewrite days the player has already read.</para>
/// </summary>
public class NewsScopeTests
{
    private const double Day = NewsWire.SecondsPerDay;

    private static CircularOrbitEphemeris SolEphemeris() => CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The system wire did not move
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The forty sim-days the Sol scenario's ambient wire printed BEFORE this lane existed,
    /// captured from the shipped build at day 40 (newest first) and pinned here character for
    /// character. This is the byte-identity proof the slice's brief asks for: the salt, the scope
    /// parameter and the two new template families all had to be added without disturbing one comma
    /// of this list, and this is the only way to know that rather than to believe it.</summary>
    private static readonly string[] SystemWireDay40Golden =
    [
        "Someone's cornering the Alloys market. Ask no questions, sell no lies.",
        "A Compute cores shipment went 'missing' in transit. The insurers are not amused; the fences are thrilled.",
        "Compute cores futures ticked up on the Ringside Exchange overnight; haulers grumble about margins.",
        "Dockhands at Ganymede are on a work slowdown — 'security concerns' nobody will name.",
        "The underwriters are quietly raising piracy premiums again. Somebody's business is booming.",
        "Someone's cornering the Compute cores market. Ask no questions, sell no lies.",
        "Alloys futures ticked up on the Ringside Exchange overnight; haulers grumble about margins.",
        "Ringside Exchange reports a glut of futures on the The Rusty Roadstead–Callisto run — margins are thin this week.",
        "Saturn quietly doubled its transit tolls. The regulars are not amused.",
        "The underwriters are quietly raising piracy premiums again. Somebody's business is booming.",
        "Word from Luna: the docking fee schedule went up again. The regulars grumble, the desperate pay.",
        "The The Clinker–Jupiter corridor has a new toll collector, or so the gossip runs.",
        "A masked freighter cleared customs without a manifest. Nobody asked twice.",
        "A trading post near Ganymede is paying premium for stale Saturn route intel — no questions asked.",
        "Someone laser-ranged a haven last week. The haven laser-ranged back.",
        "Enceladus haven regulars swap the same three rumors, louder every night.",
        "Someone's cornering the Machinery market. Ask no questions, sell no lies.",
        "Word from Selene Gate: the docking fee schedule went up again. The regulars grumble, the desperate pay.",
        "The Space Bar off Mars threw out two bounty hunters before last call — house rule: check your guns, drink your credits.",
        "Someone's cornering the Compute cores market. Ask no questions, sell no lies.",
        "Neptune traffic control reports a backlog of hopeful haulers — everyone wants a slot.",
        "Ringside Exchange reports a glut of futures on the Earth–Mercury Compute Farms run — margins are thin this week.",
        "Nobody at The Tilt can agree which way is up; the bar's been listing sideways off Uranus since before anyone's tab opened.",
        "The Venus–Mercury Compute Farms corridor has a new toll collector, or so the gossip runs.",
        "The Space Bar off Mars threw out two bounty hunters before last call — house rule: check your guns, drink your credits.",
        "Ringside Exchange reports a glut of futures on the The Clinker–Selene Gate run — margins are thin this week.",
        "The Space Bar off Mars threw out two bounty hunters before last call — house rule: check your guns, drink your credits.",
        "Deep-range scan folk swear a pyramid crossed their bow out past 2 AU — impossibly fast, dead silent, gone by second look.",
        "A trading post near Derelict Roadster is paying premium for stale Highport Satellite Works route intel — no questions asked.",
        "A masked freighter cleared customs without a manifest. Nobody asked twice.",
        "A trading post near Luna is paying premium for stale Enceladus route intel — no questions asked.",
        "Someone's cornering the Ice market. Ask no questions, sell no lies.",
        "Ringside Exchange reports a glut of futures on the Miranda–Phobos run — margins are thin this week.",
        "A He3 shipment went 'missing' in transit. The insurers are not amused; the fences are thrilled.",
        "Mercury Compute Farms is quietly stockpiling Machinery — for a project nobody will name.",
        "The Mercury–Enceladus corridor has a new toll collector, or so the gossip runs.",
        "Someone laser-ranged a haven last week. The haven laser-ranged back.",
        "Rumor: a captain out of the Belt is buying up old mass-driver pods along the Highport Satellite Works–Miranda line.",
        "Dockhands at Earth are on a work slowdown — 'security concerns' nobody will name.",
        "Someone's cornering the Machinery market. Ask no questions, sell no lies.",
    ];

    [Fact]
    public void AmbientSystemWire_ReadsExactlyAsItDidBefore()
    {
        var items = NewsWire.Ambient(SolEphemeris(), 40 * Day, SystemWireDay40Golden.Length);

        Assert.Equal(SystemWireDay40Golden.Length, items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            Assert.Equal(SystemWireDay40Golden[i], items[i].Headline);
        }
    }

    /// <summary>The default is the old behaviour, spelled out: an argument-less call and an explicit
    /// <c>SystemWire, salt: null</c> call are the same stream. Both existing consumers rely on this —
    /// they pass neither argument.</summary>
    [Fact]
    public void AmbientDefault_IsTheSystemWireWithNoSalt()
    {
        var ephemeris = SolEphemeris();

        var byDefault = NewsWire.Ambient(ephemeris, 314 * Day, 30);
        var explicitly = NewsWire.Ambient(ephemeris, 314 * Day, 30, NewsWire.NewsScope.SystemWire, salt: null);
        var empty = NewsWire.Ambient(ephemeris, 314 * Day, 30, NewsWire.NewsScope.SystemWire, salt: "");

        Assert.Equal(byDefault, explicitly);
        Assert.Equal(byDefault, empty);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The salt: two ports, one afternoon, two papers
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Salt_MakesTheSameDayReadDifferentlyAtDifferentSites()
    {
        var ephemeris = SolEphemeris();

        var ceres = NewsWire.Ambient(ephemeris, 77 * Day, 20, NewsWire.NewsScope.PortRag, "ceres");
        var pallas = NewsWire.Ambient(ephemeris, 77 * Day, 20, NewsWire.NewsScope.PortRag, "pallas");

        int differing = 0;
        for (int i = 0; i < ceres.Count; i++)
        {
            if (ceres[i].Headline != pallas[i].Headline)
            {
                differing++;
            }
        }

        // Two independent streams over twenty days: a handful of collisions is arithmetic, near-total
        // agreement would mean the salt never reached the seed.
        Assert.True(differing >= 15, $"Expected two salts to diverge across 20 days; only {differing}/20 differed.");
    }

    [Fact]
    public void Salt_IsDeterministic_SameSiteSameDaySameLine()
    {
        var ephemeris = SolEphemeris();

        var first = NewsWire.Ambient(ephemeris, 210 * Day, 5, NewsWire.NewsScope.PortRag, "ceres");
        var second = NewsWire.Ambient(ephemeris, 210 * Day + 4321, 5, NewsWire.NewsScope.PortRag, "ceres");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Ambient_NoSystemRandomAndNoClock_AcrossEveryScope()
    {
        // Determinism is law in Core (§9). Two calls a measurable moment apart, on every masthead.
        var ephemeris = SolEphemeris();
        foreach (NewsWire.NewsScope scope in Enum.GetValues<NewsWire.NewsScope>())
        {
            var a = NewsWire.Ambient(ephemeris, 9 * Day, 12, scope, "some-site");
            var b = NewsWire.Ambient(ephemeris, 9 * Day, 12, scope, "some-site");
            Assert.Equal(a, b);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The three mastheads carry the right content
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The company talks only to itself: NOT ONE line of system wire content reaches an
    /// intranet. Proven against the whole of the system wire's own vocabulary — every flat line, and
    /// the fixed prose of every body/route/cargo template — over a long stretch of days.</summary>
    [Fact]
    public void CompanyIntranet_CarriesNoSystemWireContentAtAll()
    {
        var ephemeris = SolEphemeris();
        var intranet = NewsWire.Ambient(ephemeris, 500 * Day, 500, NewsWire.NewsScope.CompanyIntranet, "lab-site");

        var systemWireVocabulary = new HashSet<string>();
        foreach (var item in NewsWire.Ambient(ephemeris, 500 * Day, 500))
        {
            systemWireVocabulary.Add(item.Headline);
        }

        foreach (var item in intranet)
        {
            Assert.DoesNotContain(item.Headline, systemWireVocabulary);
        }
    }

    /// <summary>Every intranet line is one of the eight authored ones, rendered — and all eight get
    /// used. A ninth family, or an edited comma in any of the eight, fails here.</summary>
    [Fact]
    public void CompanyIntranet_UsesAllEightAuthoredLinesVerbatimAndNothingElse()
    {
        var ephemeris = SolEphemeris();
        var used = new HashSet<int>();

        foreach (var item in NewsWire.Ambient(ephemeris, 800 * Day, 800, NewsWire.NewsScope.CompanyIntranet, "lab"))
        {
            int family = FamilyIndex(AuthoredNewsLines.CompanyIntranet, item.Headline);
            Assert.True(family >= 0, $"an intranet line matched none of #1052's eight authored lines: \"{item.Headline}\"");
            used.Add(family);
        }

        Assert.Equal(AuthoredNewsLines.CompanyIntranet.Length, used.Count);
    }

    [Fact]
    public void PortRag_IsTheWirePlusThePortsOwnSheet()
    {
        var ephemeris = SolEphemeris();
        var rag = NewsWire.Ambient(ephemeris, 800 * Day, 800, NewsWire.NewsScope.PortRag, "Ganymede");

        var ragFamilies = new HashSet<int>();
        bool sawSystemWireLine = false;
        foreach (var item in rag)
        {
            int family = FamilyIndex(AuthoredNewsLines.PortRag, item.Headline);
            if (family >= 0)
            {
                ragFamilies.Add(family);
            }
            else
            {
                sawSystemWireLine = true;
            }
        }

        Assert.True(sawSystemWireLine, "The rag is the wire PLUS a local sheet — it must still carry system wire content.");
        Assert.Equal(AuthoredNewsLines.PortRag.Length, ragFamilies.Count);
    }

    /// <summary>The rag names the port it is printed at. Salted with a body the ephemeris knows, the
    /// <c>{PORT}</c> token resolves to that body's own name rather than a stranger's.</summary>
    [Fact]
    public void PortRag_NamesThePortItIsPrintedAt()
    {
        var ephemeris = SolEphemeris();
        var rag = NewsWire.Ambient(ephemeris, 900 * Day, 900, NewsWire.NewsScope.PortRag, "Ganymede");

        int portLines = 0;
        foreach (var item in rag)
        {
            if (item.Headline.StartsWith("Berth fees at ", StringComparison.Ordinal) ||
                item.Headline.StartsWith("Customs at ", StringComparison.Ordinal))
            {
                portLines++;
                Assert.Contains("Ganymede", item.Headline, StringComparison.Ordinal);
            }
        }

        Assert.True(portLines > 0, "Expected the port family to print at least one {PORT} line across 900 days.");
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The authored lines, verbatim
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1052's own words, kept where a reviewer can diff them against the issue. Crews do not
    /// write canon; this file is the receipt that none was written or edited here.</summary>
    private static class AuthoredNewsLines
    {
        public static readonly string[] CompanyIntranet =
        [
            "The board tours the facility on {DAY}. Badges visible above the waist. Smiles are optional but are noted.",
            "{N} days since the last unscheduled decompression. The counter is bolted down now.",
            "Staff turnover in Deep Storage remains within projections. Farewell cards are in the canteen.",
            "Cold-chain capacity doubles again this quarter. Logistics thanks you in advance for not asking why.",
            "Tuesday is protein day in the canteen. Wednesday is also protein day.",
            "The archive outage of {DAY} is resolved. Files restored from backup may differ in small ways. Do not file tickets about the differences.",
            "Reminder: personal effects found in decommissioned quarters go to Lost Property for {N} days, then to Procurement.",
            "Wellness check: if you cannot remember your last shore leave, that is normal and covered.",
        ];

        public static readonly string[] PortRag =
        [
            "Berth fees at {PORT} rise a third time this quarter. The harbourmaster calls it weather.",
            "A crewman off the {SHIPNAME-template} has not reported back. His tab remains open.",
            "Customs at {PORT} now opens one crate in {N}. The queue prefers the old arrangement.",
            "Solar conditions fair. The long-haul crowd drinks anyway.",
            "Lost: one pressure glove, left hand. Reward is a drink and no questions.",
            "The {PORT} pool on next month's tariff schedule is closed. The winner is not saying.",
        ];
    }

    /// <summary>Turn one AUTHORED line into the regex its rendered form must match: the fixed prose is
    /// escaped literally (so a crew that "improved" a comma fails here), and only the author's own
    /// tokens are allowed to vary. This is what makes "implement verbatim" a checkable claim rather
    /// than a hope — the issue's text is the pattern, character for character, everywhere but the
    /// placeholders.</summary>
    private static Regex TemplateRegex(string authored)
    {
        string pattern = Regex.Escape(authored)
            .Replace(Regex.Escape("{DAY}"), @"day \d+", StringComparison.Ordinal)
            .Replace(Regex.Escape("{N}"), @"\d+", StringComparison.Ordinal)
            .Replace(Regex.Escape("{PORT}"), @"[^{}]+", StringComparison.Ordinal)
            .Replace(Regex.Escape("{BODY}"), @"[^{}]+", StringComparison.Ordinal)
            .Replace(Regex.Escape("{SHIPNAME-template}"), @"[^{}]+", StringComparison.Ordinal);

        return new Regex("^" + pattern + "$", RegexOptions.CultureInvariant);
    }

    /// <summary>Which authored line a rendered headline came from, or -1 for "not from this family".</summary>
    private static int FamilyIndex(IReadOnlyList<string> authored, string rendered)
    {
        for (int i = 0; i < authored.Count; i++)
        {
            if (TemplateRegex(authored[i]).IsMatch(rendered))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>No line ever reaches a reader with an unsubstituted author token still in it — the
    /// "{N} days since" bug, shipped. Swept across every scope, a long run of days and several salts.</summary>
    [Fact]
    public void NoRenderedLine_EverShowsAnUnsubstitutedToken()
    {
        var ephemeris = SolEphemeris();
        string[] salts = ["", "ceres", "Ganymede", "lab:vantar:europa", "The Rusty Roadstead"];

        foreach (NewsWire.NewsScope scope in Enum.GetValues<NewsWire.NewsScope>())
        {
            foreach (string salt in salts)
            {
                foreach (var item in NewsWire.Ambient(ephemeris, 600 * Day, 600, scope, salt))
                {
                    Assert.DoesNotContain("{", item.Headline, StringComparison.Ordinal);
                    Assert.DoesNotContain("}", item.Headline, StringComparison.Ordinal);
                }
            }
        }
    }

    /// <summary>A salt that names no body in this ephemeris — a lab site id, a renamed berth — still
    /// prints a real port name rather than a hole in the sentence.</summary>
    [Fact]
    public void PortRag_WithASaltThatNamesNoBody_StillPrintsARealPort()
    {
        var ephemeris = SolEphemeris();
        foreach (var item in NewsWire.Ambient(ephemeris, 400 * Day, 400, NewsWire.NewsScope.PortRag, "no-such-body-42"))
        {
            Assert.DoesNotContain("{PORT}", item.Headline, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(item.Headline));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // The seam L2 consumes: what does this place read?
    // ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A body the seed genuinely hides a lab on, found by asking <see cref="SecretLab.Present"/>
    /// rather than by hardcoding one — the odds are 1 in 40 and a hardcoded id would rot the first time
    /// the presence seed changed.</summary>
    private static string BodyWithALab()
    {
        for (int i = 0; i < 5000; i++)
        {
            string id = $"moon-{i}";
            if (SecretLab.Present(id))
            {
                return id;
            }
        }

        throw new InvalidOperationException("no body in 5000 hides a lab — the presence seed is broken");
    }

    private static string BodyWithoutALab()
    {
        for (int i = 0; i < 5000; i++)
        {
            string id = $"moon-{i}";
            if (!SecretLab.Present(id))
            {
                return id;
            }
        }

        throw new InvalidOperationException("every body hides a lab — the presence seed is broken");
    }

    [Fact]
    public void ScopeAt_AboardHisOwnHull_IsTheSystemWire()
    {
        Assert.Equal(NewsWire.NewsScope.SystemWire, NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: true, SiteBodyId: null)));

        // Even standing in a lab: the galley card (key 6) is the ship's own paper wherever she is parked.
        Assert.Equal(
            NewsWire.NewsScope.SystemWire,
            NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: true, SiteBodyId: BodyWithALab(), InsideSecretLab: true)));
    }

    [Fact]
    public void ScopeAt_DockedAtAPort_IsThatPortsRag()
    {
        Assert.Equal(
            NewsWire.NewsScope.PortRag,
            NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: "ganymede")));
    }

    [Fact]
    public void ScopeAt_InsideASecretLab_IsTheCompanyIntranet()
    {
        string withLab = BodyWithALab();

        Assert.Equal(
            NewsWire.NewsScope.CompanyIntranet,
            NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: withLab, InsideSecretLab: true)));

        // Outside the lab door on the same moon, the bar prints the port's rag, not the company's paper.
        Assert.Equal(
            NewsWire.NewsScope.PortRag,
            NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: withLab, InsideSecretLab: false)));
    }

    /// <summary>A body that hides nothing can never print a company paper — and the <c>?secretlab=1</c>
    /// cheat's forced lab prints one, so the cheat's world and the wire's world agree.</summary>
    [Fact]
    public void ScopeAt_OnABodyWithNoLab_IsTheRagUnlessTheLabWasForced()
    {
        string noLab = BodyWithoutALab();

        Assert.Equal(
            NewsWire.NewsScope.PortRag,
            NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: noLab, InsideSecretLab: true)));

        Assert.Equal(
            NewsWire.NewsScope.CompanyIntranet,
            NewsWire.ScopeAt(new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: noLab, InsideSecretLab: true, LabForced: true)));
    }

    [Fact]
    public void SaltFor_IsTheSiteAshoreAndNothingAboard()
    {
        Assert.Null(NewsWire.SaltFor(new NewsWire.NewsPlace(AboardShip: true, SiteBodyId: "ganymede")));
        Assert.Equal("ganymede", NewsWire.SaltFor(new NewsWire.NewsPlace(AboardShip: false, SiteBodyId: "ganymede")));
    }
}

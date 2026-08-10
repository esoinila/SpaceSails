using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #728 · WHAT THE CAPTAIN ACTUALLY WALKS UP TO, AND WHAT THE CAPTAIN ACTUALLY READS.
///
/// <para>The Core half of this ticket pins the SENTENCES (<c>TheShelterSaysWhatItDoesTests</c>). This half
/// pins that they reach a screen — which is a different question and, in this repository, the one that keeps
/// costing money: #573's shelter beacons pointed at buildings nobody built, and the plates and the readout
/// here are two more places where a correct constant can sit in Core with nothing on the ground carrying
/// it.</para>
///
/// <para>So it walks the REAL surface deck of every landing site the scenario has, and it reads the REAL
/// on-foot instrument column out of the page that builds it. A guard that asserted the constants alone would
/// have passed on the day this was filed, because the constants were fine and the ground was silent.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheShelterFixturesSayWhatTheyDoTests
{
    // The same ten landable bodies ShelterBeaconsTellTheTruthTests walks, and for the same reason: a
    // procedural ground audited on one moon is a ground audited on one seed.
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    private static DeckPlan DeckFor(string body, LandingSite site) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name);

    /// <summary>A plate as it is read at a run: the glyph gone, because a 🫁 and a 🔫 drawn at 9px over grey
    /// regolith are the first two things to become the same smudge — which is the owner's whole complaint.</summary>
    private static string WithoutTheGlyph(string plate) =>
        new string([.. plate.Where(char.IsAscii)]).Trim();

    /// <summary>What each shelter fixture must be able to say without leaning on its glyph.</summary>
    private static readonly Dictionary<DeckPlan.ConsoleKind, string> MustName = new()
    {
        [DeckPlan.ConsoleKind.ShelterTank] = "TANK",
        [DeckPlan.ConsoleKind.ShelterLocker] = "MAGAZINE",
    };

    [Fact]
    public void EveryShelterFixtureOnEveryGroundNamesWhatItFills()
    {
        // THE REPRO, walked. Owner, live on THE WILD PLAIN, standing between the two consoles he specified:
        // "the air fill and the reload functionality are not marked quite so clearly as they could be… on
        // shelters I always forget which is which."
        //
        // Asked of the shipping plan rather than of the constants, because these labels travel: MoonSurface
        // lays them, DeckView draws them, and a fixture whose plate was hard-typed at the lay site would pass
        // any test written against SurfaceShelter and still be mute on the ground.
        var bad = new List<string>();
        int fixtures = 0;

        foreach ((string body, LandingSite site) in EverySite())
        {
            foreach (DeckPlan.ConsoleSpot console in DeckFor(body, site).Consoles)
            {
                if (!MustName.TryGetValue(console.Kind, out string? resource))
                {
                    continue;
                }

                fixtures++;
                string plate = WithoutTheGlyph(console.Label);
                if (!plate.Contains(resource, StringComparison.OrdinalIgnoreCase))
                {
                    bad.Add($"  {body}/{site.Name}: {console.Kind} reads \"{console.Label}\" — " +
                            $"strip the glyph and nothing in it says {resource}.");
                }
            }
        }

        // A sweep over a ground with no shelters on it would pass for the wrong reason — the fifth named bug
        // class. Every site carries at least one shelter with two fixtures in it (SurfaceShelter.CountFor
        // never returns under four), so this floor is far below anything the generator can produce.
        Assert.True(fixtures >= 2 * 4 * EverySite().Count(),
            $"only {fixtures} shelter fixtures were audited across {EverySite().Count()} sites — " +
            "the sweep is not seeing the buildings it is meant to be reading.");

        Report(bad, "shelter fixtures whose plate does not say what the fixture does");
    }

    [Fact]
    public void TheTwoPlatesInsideOneShelterAreNeverTheSameSentence()
    {
        // The failure was not "a plate is wrong", it was "two plates a metre apart are indistinguishable
        // without their glyphs". So this asks the pair, on real ground, which is the shape the owner's eye
        // was actually asked to resolve.
        var bad = new List<string>();

        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan deck = DeckFor(body, site);
            var tanks = deck.Consoles
                .Where(c => c.Kind == DeckPlan.ConsoleKind.ShelterTank).ToList();
            var lockers = deck.Consoles
                .Where(c => c.Kind == DeckPlan.ConsoleKind.ShelterLocker).ToList();

            foreach (DeckPlan.ConsoleSpot tank in tanks)
            {
                if (lockers.Count == 0)
                {
                    bad.Add($"  {body}/{site.Name}: a charging rack at ({tank.X:F1},{tank.Y:F1}) with no locker anywhere.");
                    continue;
                }

                // The locker belonging to THIS drum — the nearest one, which on a 26 x 21 du shelter is the
                // one 5.5 du above it and never a neighbour's (shelters stand MinSeparationDu = 52 apart).
                DeckPlan.ConsoleSpot locker = lockers
                    .OrderBy(l => ((l.X - tank.X) * (l.X - tank.X)) + ((l.Y - tank.Y) * (l.Y - tank.Y)))
                    .First();

                if (string.Equals(WithoutTheGlyph(tank.Label), WithoutTheGlyph(locker.Label),
                        StringComparison.OrdinalIgnoreCase))
                {
                    bad.Add($"  {body}/{site.Name}: both fixtures read \"{WithoutTheGlyph(tank.Label)}\" " +
                            "once the glyph is gone.");
                }
            }
        }

        Report(bad, "shelters whose two fixtures cannot be told apart without their glyphs");
    }

    // ── the readout, on the instrument column that actually draws it ──────────────────────────────────

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

    [Fact]
    public void TheOnFootInstrumentColumnActuallyPRINTSTheMagazines()
    {
        // #728's first head, and the one no assertion about Core can reach: SentryBot.MagazinesReadout can be
        // perfect and unreferenced, which is exactly the state the ammunition roster was in on 2026-08-06 —
        // a number the sim kept faithfully and the screen never showed.
        //
        // BuildTrackerCaptions is a private method on a Blazor page with a client-only excursion type for an
        // argument, so there is nothing to call from here; the honest seam is the source, which is the same
        // seam EveryStoryBeatHasACallerTests uses to prove a beat has a caller. It asks one question — does
        // the method that composes the on-foot instrument column emit the readout — and it goes red the
        // moment somebody deletes the call, which is the failure it exists for.
        string page = Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.Surface.cs");
        Assert.True(File.Exists(page), $"the on-foot page has moved: {page}");

        string source = File.ReadAllText(page);

        Match builder = Regex.Match(
            source,
            @"private\s+List<string>\s+BuildTrackerCaptions\s*\([^)]*\)\s*\{",
            RegexOptions.None,
            TimeSpan.FromSeconds(5));
        Assert.True(builder.Success,
            "BuildTrackerCaptions — the on-foot instrument column — is no longer where this guard looks for it.");

        // WITHOUT ITS COMMENTS, and this is not fastidiousness — the first cut of this guard passed on a
        // deliberately broken tree because the comment ABOVE the deleted call still named the method. That is
        // this house's fifth bug class exactly: a guard handed a world that cannot tell pass from fail. Only
        // code counts.
        string body = CodeOnly(BodyOf(source, builder.Index + builder.Length - 1));

        Assert.Contains("SentryBot.MagazinesReadout", body, StringComparison.Ordinal);

        // And it is fed from the EXCURSION's own bots — the same list the counters over deployed sentries
        // and the shelter's press both work on — through the one projection both of them share. A readout
        // fed from anywhere else would be a second roster, which is this house's most expensive shape and
        // the reason the composition sits in Core at all.
        Assert.Contains("TheSlingAsTheInstrumentReadsIt", body, StringComparison.Ordinal);

        Match projection = Regex.Match(
            source,
            @"TheSlingAsTheInstrumentReadsIt\s*\(\s*SurfaceExcursion\s+ex\s*\)\s*=>[^;]*;",
            RegexOptions.None,
            TimeSpan.FromSeconds(5));
        Assert.True(projection.Success, "the sling projection the instrument and the press share has moved.");
        Assert.Contains("ex.Bots", projection.Value, StringComparison.Ordinal);
    }

    /// <summary>The same text with every <c>//</c> comment struck out, so a sentence about a call can never
    /// stand in for the call. This file's own comments are dense enough that it would have, and did.</summary>
    private static string CodeOnly(string body) =>
        string.Join('\n', body.Split('\n').Select(l =>
        {
            int slashes = l.IndexOf("//", StringComparison.Ordinal);
            return slashes < 0 ? l : l[..slashes];
        }));

    /// <summary>The text between a method's opening brace and its match — brace-counted, because a regex that
    /// tried to span a body would either stop at the first nested block or run to the end of the file.</summary>
    private static string BodyOf(string source, int openBrace)
    {
        int depth = 0;
        for (int i = openBrace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[openBrace..(i + 1)];
                }
            }
        }
        throw new InvalidOperationException("unbalanced braces reading the tracker-caption builder");
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad.Take(30))
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;
using Xunit.Abstractions;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #533 · <b>TWO INSTRUMENTS THAT DISAGREE</b> — the guards on the derived anomaly.
///
/// <para>Owner: <i>"The story ones are exceptional in some sense that we are left to wonder. Like what was
/// such a rich ship doing there-kind of things 😎"</i></para>
///
/// <para>Most of these are guards about PROVENANCE and about SILENCE rather than about a value, because
/// that is where this feature can actually go wrong. An anomaly that was dealt to a hull whose numbers do
/// not support it is a lie the captain can check; an anomaly that some later system explains is the one
/// thing the issue's discipline forbids outright; and a threshold that selects every hull is this repo's
/// own fifth named bug class. So: the inequality is re-derived on every hull that was dealt one, the world
/// the thresholds are stated against is proved able to tell pass from fail, and the repository's source is
/// swept for anybody at all reading this file outside the reading and the book.</para>
/// </summary>
public sealed class TwoInstrumentsDisagreeTests(ITestOutputHelper output)
{
    // ── THE POPULATION ──────────────────────────────────────────────────────────────────────────────────
    //
    // The hulls the game actually deals: Derelict.SeededWithCause walks "lost-0", "lost-1", … to find a hull
    // that died a given way, and the cheat boots "kestrel-3". Sweeping the same sequence is sweeping the
    // fleet rather than a sample somebody chose.

    private static IEnumerable<Derelict.Wreck> TheFleet(int hulls = 400)
    {
        for (int i = 0; i < hulls; i++)
        {
            yield return Derelict.Seeded($"lost-{i}");
        }
    }

    /// <summary>A road nobody lists: The Tilt and The Deep are served only by discreet haulers, which
    /// <c>ArrivalTubeTests</c> already pins (<c>ScheduledTonnage == 0</c>). This is the fact, not a
    /// stand-in for one.</summary>
    private static WreckAnomaly.Facts PoorRoad =>
        new(ArrivalTube.ScheduledTonnage(Sol, "the-tilt"));

    /// <summary>…and a road that is on every board in the system.</summary>
    private static WreckAnomaly.Facts BusyRoad =>
        new(ArrivalTube.ScheduledTonnage(Sol, "ringside-exchange"));

    private static ICelestialEphemeris Sol { get; } = CircularOrbitEphemeris.FromScenario(TestTree.Sol);

    // ── THE WORLD CAN TELL PASS FROM FAIL ───────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ANTI-VACUOUS HALF, first because everything below leans on it. The two roads these guards are
    /// stated against are really different roads in the shipped sky, and the value threshold really does cut
    /// the fleet in two — a guard whose world is empty, or whose threshold selects everything, is green and
    /// asserting nothing.
    /// </summary>
    [Fact]
    public void TheWorldTheseGuardsAreStatedAgainstCanTellPassFromFail()
    {
        Assert.Equal(0, PoorRoad.ListedTonnageOnHerRoad);
        Assert.True(BusyRoad.ListedTonnageOnHerRoad > 0,
            "Ringside is the busiest berth in the shipped sky; if it lists nothing, this suite is testing a "
            + "world that no longer exists");

        Derelict.Wreck[] fleet = [.. TheFleet()];
        int rich = fleet.Count(w => w.AssessedValueCr >= WreckAnomaly.RichFromCr);

        Assert.InRange(WreckAnomaly.RichFromCr, Derelict.AssessedFloorCr + 1, Derelict.AssessedCeilingCr - 1);
        Assert.True(rich > 0 && rich < fleet.Length,
            $"\"rich\" selects {rich} of {fleet.Length} hulls — a threshold that takes all or none of the "
            + "fleet is not a fact about a ship");

        output.WriteLine($"rich hulls: {rich}/{fleet.Length} at {WreckAnomaly.RichFromCr:N0} cr");
    }

    // ── DETERMINISM ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SAME HULL, FOREVER. A rumour that names her can be trusted, leaving and coming back is not a
    /// re-roll, and a test can pin her. Asked a hundred times per hull and off a freshly re-seeded copy of
    /// the same ship, which is what catches a seed taken through <c>string.GetHashCode</c> (randomised per
    /// process) rather than through <see cref="DiceRule.Seed(string, long[])"/>.
    /// </summary>
    [Fact]
    public void TheSameHullCarriesTheSameAnomalyForever()
    {
        foreach (Derelict.Wreck wreck in TheFleet(120))
        {
            WreckAnomaly.Reading? first = WreckAnomaly.For(wreck, PoorRoad);
            for (int again = 0; again < 100; again++)
            {
                Assert.Equal(first, WreckAnomaly.For(Derelict.Seeded(wreck.Id), PoorRoad));
            }
        }
    }

    /// <summary>The road is the other half of the answer, and it is a fact about the WORLD rather than about
    /// the clock — the route table and the body tree, nothing else. So a hull read on the way out and read
    /// again an hour later is the same hull with the same anomaly, and a second ephemeris built from the same
    /// sky agrees with the first.</summary>
    [Fact]
    public void TheRoadIsAFactAboutTheWorldAndNotAboutTheClock()
    {
        var anotherSky = CircularOrbitEphemeris.FromScenario(TestTree.Sol);
        foreach (string berth in new[] { "the-tilt", "the-deep", "selene-gate", "ringside-exchange" })
        {
            Assert.Equal(
                ArrivalTube.ScheduledTonnage(Sol, berth),
                ArrivalTube.ScheduledTonnage(anotherSky, berth));
        }
    }

    // ── ZERO OR ONE ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A hull carries no anomaly or exactly one. Not a list, not a stack, not a second one on the
    /// way out: the whole design is one verifiable fact you cannot explain, and two of them on one ship is a
    /// puzzle with pieces.</summary>
    [Fact]
    public void AHullCarriesAtMostOneAnomaly()
    {
        foreach (Derelict.Wreck wreck in TheFleet())
        {
            foreach (WreckAnomaly.Facts road in new[] { PoorRoad, BusyRoad })
            {
                WreckAnomaly.Kind? dealt = WreckAnomaly.Dealt(wreck, road);
                if (dealt is { } one)
                {
                    Assert.Contains(one, WreckAnomaly.SupportedBy(wreck, road));
                    Assert.Equal(one, WreckAnomaly.For(wreck, road)!.Value.Of);
                }
                else
                {
                    Assert.Null(WreckAnomaly.For(wreck, road));
                }
            }
        }
    }

    // ── ONLY WHAT HER NUMBERS SUPPORT ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY ANOMALY DEALT IS TRUE OF THE HULL IT WAS DEALT TO</b> — the honesty rule, re-derived here
    /// from the hull's own numbers rather than asked of the same predicate that dealt it. A captain who
    /// walks to the manifest finds the value the line quoted, and a captain who looks up what calls at the
    /// berth she was found off finds nothing on a board.
    /// </summary>
    [Fact]
    public void EveryAnomalyDealtIsReallyTrueOfThatHull()
    {
        int dealt = 0;
        foreach (Derelict.Wreck wreck in TheFleet())
        {
            foreach (WreckAnomaly.Facts road in new[] { PoorRoad, BusyRoad })
            {
                if (WreckAnomaly.For(wreck, road) is not { } reading)
                {
                    continue;
                }

                dealt++;
                switch (reading.Of)
                {
                    case WreckAnomaly.Kind.RichHullPoorRoad:
                        Assert.True(wreck.AssessedValueCr >= WreckAnomaly.RichFromCr,
                            $"{wreck.ShipName} ({wreck.Id}) was called a rich hull at "
                            + $"{wreck.AssessedValueCr:N0} cr");
                        Assert.Equal(0, road.ListedTonnageOnHerRoad);
                        Assert.Contains($"{wreck.AssessedValueCr:N0} cr", reading.Line, StringComparison.Ordinal);
                        break;
                    default:
                        Assert.Fail($"an anomaly kind with no support rule: {reading.Of}");
                        break;
                }
            }
        }

        Assert.True(dealt > 0, "nothing was dealt at all — this guard would pass on a feature that is off");
    }

    /// <summary>And the other end of the same law: a hull on a road the world lists is never dealt the
    /// anomaly that is ABOUT nobody listing her road, however rich she is.</summary>
    [Fact]
    public void NoHullOnAListedRoadIsEverCalledPoorRoad()
    {
        foreach (Derelict.Wreck wreck in TheFleet())
        {
            Assert.Null(WreckAnomaly.For(wreck, BusyRoad));
        }
    }

    // ── HOW OFTEN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE RARITY, MEASURED RATHER THAN CLAIMED. The issue's first rule is a rarity budget: <i>"if every
    /// wreck is a mystery, none of them is."</i> Two rates matter and they are different numbers — how often
    /// a hull's own facts support anything at all, and how often a supported hull is actually dealt one.
    /// </summary>
    [Fact]
    public void TheRarityIsTheOneThatWasMeasured()
    {
        Derelict.Wreck[] fleet = [.. TheFleet()];
        int supported = fleet.Count(w => WreckAnomaly.SupportedBy(w, PoorRoad).Count > 0);
        int carried = fleet.Count(w => WreckAnomaly.For(w, PoorRoad) is not null);

        output.WriteLine($"on an unlisted road: {supported}/{fleet.Length} hulls support an anomaly, "
            + $"{carried} carry one ({100.0 * carried / fleet.Length:F1}% of the fleet, "
            + $"{100.0 * carried / Math.Max(1, supported):F1}% of the supported)");

        // One in CarriesOneInN of the supported hulls, inside the band a 400-hull sample can honestly claim.
        double ofSupported = (double)carried / Math.Max(1, supported);
        Assert.InRange(ofSupported, 0.20, 0.47);
        Assert.True(carried < fleet.Length / 3,
            "an anomaly on a third of all hulls is not an anomaly, it is weather");
    }

    // ── IT NEVER BECOMES A CAUSE ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE PAPERWORK GAINS NO CAUSE.</b> The issue's first discipline: a report names what happened, and
    /// an anomaly is not a thing that happened. The ten causes are the ten causes, the dropdown is built
    /// from that enum, and <see cref="Derelict"/> does not so much as know this file exists.
    /// </summary>
    [Fact]
    public void ThePaperworkGainsNoCause()
    {
        Assert.Equal(10, Enum.GetValues<Derelict.WreckCause>().Length);
        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            Assert.DoesNotContain("anomaly", cause.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("WreckAnomaly", CoreSource("Derelict.cs"), StringComparison.Ordinal);
    }

    // ── IT CONCLUDES NOTHING ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>TWO FACTS, SIDE BY SIDE, AND NOTHING ELSE.</b> No causal word may stand between them — the moment
    /// one does, the game has done the wondering for the captain — and no reserved word may name what the
    /// hull was or who was behind her, because an anomaly that hints at an arc is an announcement.
    ///
    /// <para>Whole words, not substrings: a guard that matched "so that" inside a longer word would go red
    /// on its own prose, which <c>TheHiveCardsTests</c> learned the expensive way.</para>
    /// </summary>
    [Fact]
    public void NoAnomalyStringConcludesAnything()
    {
        string[] causal =
            ["because", "why", "must", "so that", "explains", "explain", "means", "therefore", "clearly",
             "suggests", "proves", "evidently", "obviously"];
        string[] reserved =
            ["kaamos", "nebula", "mutual", "reever", "old one", "old ones", "monolith", "restore", "backup",
             "smuggler", "fraud", "pirate", "warship", "q-ship", "milspec", "military", "navy"];

        int said = 0;
        foreach (Derelict.Wreck wreck in TheFleet(60))
        {
            foreach (string line in WreckAnomaly.EveryLine(wreck))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                said++;
                foreach (string bad in causal)
                {
                    Assert.False(SaysTheWords(line, bad),
                        $"an anomaly line reasons out loud (\"{bad}\"): {line}");
                }

                foreach (string bad in reserved)
                {
                    Assert.False(SaysTheWords(line, bad),
                        $"an anomaly line names what she was (\"{bad}\"): {line}");
                }
            }
        }

        Assert.True(said > 0, "no anomaly prose was swept at all");
    }

    /// <summary>Whole-word (or whole-phrase) containment, punctuation-insensitive.</summary>
    private static bool SaysTheWords(string line, string phrase)
    {
        string plain = new([.. line.ToLowerInvariant().Select(c => char.IsLetter(c) || c == '-' ? c : ' ')]);
        string[] words = plain.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] wanted = phrase.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i + wanted.Length <= words.Length; i++)
        {
            bool all = true;
            for (int j = 0; j < wanted.Length; j++)
            {
                if (!string.Equals(words[i + j], wanted[j], StringComparison.Ordinal))
                {
                    all = false;
                    break;
                }
            }

            if (all)
            {
                return true;
            }
        }

        return false;
    }

    // ── NOTHING EVER RESOLVES ONE ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>NO RESOLUTION, EVER</b> — and it is a SOURCE law, because no behavioural test can prove that an
    /// explanation will not be written next month. Exactly two files in the shipping tree may name this
    /// feature: the one that computes it, and the one that reads it out at the station and files it in the
    /// book. A contact who explains one, an arc card that resolves one, a later note that answers it — each
    /// of those has to name <c>WreckAnomaly</c> somewhere, and each of them turns this red.
    ///
    /// <para><b>Proven able to fail:</b> name <c>WreckAnomaly</c> in any third file and this reports it.</para>
    /// </summary>
    [Fact]
    public void NothingOutsideTheReadingAndTheBookReadsAnAnomaly()
    {
        string[] mayName =
        [
            Path.Combine("src", "SpaceSails.Core", "WreckAnomaly.cs"),
            Path.Combine("src", "SpaceSails.Client", "Pages", "Map.Wreck.cs"),
        ];

        var swept = new List<string>();
        var names = new List<string>();
        foreach (string file in Directory.EnumerateFiles(
                     Path.Combine(TestTree.RepoRoot(), "src"), "*.*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || (!file.EndsWith(".cs", StringComparison.Ordinal)
                    && !file.EndsWith(".razor", StringComparison.Ordinal)))
            {
                continue;
            }

            swept.Add(file);
            if (File.ReadAllText(file).Contains("WreckAnomaly", StringComparison.Ordinal))
            {
                names.Add(Path.GetRelativePath(TestTree.RepoRoot(), file));
            }
        }

        Assert.True(swept.Count > 200, $"the sweep found {swept.Count} files — it is not reading the tree");
        Assert.Equal(mayName.OrderBy(p => p, StringComparer.Ordinal).ToArray(),
                     names.OrderBy(p => p, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// THE FITTINGS STAY IDENTICAL. A hull that carries an anomaly must be indistinguishable from one that
    /// does not until she is read: no extra console, no mark on the deck, nothing countable from the
    /// doorway. The layout does not know this feature exists, and the eight fittings are the eight fittings
    /// on every cause.
    /// </summary>
    [Fact]
    public void AHullCarryingOneIsFittedExactlyLikeAHullThatIsNot()
    {
        Assert.DoesNotContain("WreckAnomaly", CoreSource("WreckLayout.Stations.cs"), StringComparison.Ordinal);
        Assert.Equal(8, WreckLayout.StandardFittings.Count);

        foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
        {
            Assert.Equal(
                WreckLayout.StandardFittings.Select(f => f.Name),
                WreckLayout.Stations(cause).Take(8).Select(f => f.Name));
        }
    }

    // ── WHAT IT SAYS, AND WHO IT IS ABOUT ───────────────────────────────────────────────────────────────

    /// <summary>The canon line, word for word, on a hull that really carries one — the numbers in the
    /// house's own <c>N0 cr</c>, so the value on this line and the value on her manifest are the same number
    /// in the same clothes.</summary>
    [Fact]
    public void TheCanonLineIsWordForWord()
    {
        Derelict.Wreck carrier = AHullThatCarriesOne();
        WreckAnomaly.Reading reading = WreckAnomaly.For(carrier, PoorRoad)!.Value;

        Assert.Equal(
            $"Assessed at {carrier.AssessedValueCr:N0} cr. Traffic on this road, listed, this year: none.",
            reading.Line);
        Assert.Contains($"{carrier.AssessedValueCr:N0} cr", Derelict.ManifestCaption(carrier), StringComparison.Ordinal);

        output.WriteLine($"{carrier.Id} · {carrier.ShipName} · {carrier.Cause}: {reading.Line}");
        output.WriteLine($"book: {reading.Gist}");
    }

    /// <summary>
    /// #741's law, from the author's end: the gist PRINTS the hull's name, and the subject the author
    /// declares is that hull, as a PLACE — somewhere the captain went and stood. Nothing reads the prose
    /// back to work it out, and the name on the heading is a name the game printed.
    /// </summary>
    [Fact]
    public void TheBookFilesItUnderTheHullItIsAbout()
    {
        Derelict.Wreck carrier = AHullThatCarriesOne();
        WreckAnomaly.Reading reading = WreckAnomaly.For(carrier, PoorRoad)!.Value;

        var note = new FieldNote(reading.Gist, 0, "somewhere", WreckAnomaly.Glyph, reading.Subjects);
        IReadOnlyList<CaseSubjects.Subject> on = CaseSubjects.On(note);

        Assert.Single(on);
        Assert.Equal(CaseSubjects.Kind.Place, on[0].Of);
        Assert.Equal(carrier.ShipName, on[0].Name);
        Assert.Contains(on[0].Name, reading.Gist, StringComparison.Ordinal);
        Assert.EndsWith("and nobody aboard to ask", reading.Gist, StringComparison.Ordinal);
    }

    /// <summary>The first hull in the fleet that actually carries one — found by sweeping rather than typed,
    /// so a retune moves this suite's example instead of breaking it.</summary>
    private static Derelict.Wreck AHullThatCarriesOne()
    {
        foreach (Derelict.Wreck wreck in TheFleet())
        {
            if (WreckAnomaly.For(wreck, PoorRoad) is not null)
            {
                return wreck;
            }
        }

        throw new InvalidOperationException("no hull in the fleet carries an anomaly");
    }

    private static string CoreSource(string file) =>
        File.ReadAllText(Path.Combine(TestTree.RepoRoot(), "src", "SpaceSails.Core", file));
}

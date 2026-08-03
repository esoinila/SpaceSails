namespace SpaceSails.Core.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SpaceSails.Core;

/// <summary>
/// #525 · THE ONE DEATH THE CAPTAIN CHOSE, told as something that happened to them.
///
/// <para>#525's outcome table records the row <i>"captain still aboard → she goes with him — built,
/// <c>DeathCause.Scuttled</c> → brain-backup"</i>. There was no <c>DeathCause.Scuttled</c>.
/// <c>AdvanceScuttleClock</c> called <c>TriggerSurfaceOverdrawDeath</c> with no known cause, so the card
/// rolled <see cref="DeathNarration.SurfaceEnd"/> and printed <i>"Nerves shot past empty on the Long Shrift
/// — an Old One's hand is the last straw"</i> — reachable on <c>?wreck=drivefailure&amp;land=1</c>, a hull
/// with nothing living in her, ninety seconds after the captain turned two keys themselves.</para>
///
/// <para>The fourth time this project has paid for a sentence that disagreed with its sim (#545's card
/// blaming Reevers for three men with rifles; #609's regolith prose 150 m underground; #574's away-team
/// words on a steel deck), and the worst beat it could land on: the game telling you that the thing you
/// decided was done to you.</para>
/// </summary>
public class TheOverloadYouSetYourselfTests
{
    /// <summary>#633 · A SCUTTLING NEEDS A REACTOR, AND THERE ARE NOW TWO OF THEM.
    ///
    /// <para>This test used to be called <c>ScuttlingIsOnlyEverAboardSomebodyElsesShip</c> and asserted
    /// <c>OwnShip</c> was illegal, on the stated grounds that <i>"your own ship has no such switch yet — that
    /// is #525's second half, with the CASTAWAY outcome still unbuilt."</i> While this branch was writing
    /// that sentence, <c>main</c> was building the switch: <c>ShipScuttle</c>, the charges in the machinery
    /// space, and the crew's second key off <c>CrewTemp.StandingOf</c>. Reuniting the branches makes the old
    /// assertion false, so it is restated rather than deleted — the law it protects is not "one place", it is
    /// <b>a place with a reactor in it</b>, which is why a moon and a suit are still nos.</para></summary>
    [Fact]
    public void ScuttlingOnlyEverHappensWhereThereIsAReactorToOverload()
    {
        // Somebody else's, aft on a wreck (#525) — or her own, in the machinery space (main's half of #525).
        Assert.True(DeathNarration.CanHappen(DeathCause.Scuttled, DeathPlace.Derelict));
        Assert.True(DeathNarration.CanHappen(DeathCause.Scuttled, DeathPlace.OwnShip));

        // A landing party carries a suit and a tank, and a moon has no reactor at all.
        Assert.False(DeathNarration.CanHappen(DeathCause.Scuttled, DeathPlace.LandingParty));
        Assert.False(DeathNarration.CanHappen(DeathCause.Scuttled, DeathPlace.Underground));
    }

    /// <summary>#633 · …and the two places must not borrow each other's words or each other's picture. The
    /// wreck goes inward and quietly, with a log that will not mention it; her own deck is the fireball the
    /// player just watched. One cause, two cards — which is the whole reason <c>DeathPlace</c> exists.</summary>
    [Fact]
    public void HerOwnScuttlingIsNotNarratedAsSomebodyElsesWreck()
    {
        Assert.NotEqual(
            DeathNarration.ArtFile(DeathCause.Scuttled, DeathPlace.Derelict),
            DeathNarration.ArtFile(DeathCause.Scuttled, DeathPlace.OwnShip));

        for (ulong seed = 0; seed < 12; seed++)
        {
            Assert.NotEqual(
                DeathNarration.Line(DeathCause.Scuttled, DeathPlace.Derelict, seed, "Long Shrift"),
                DeathNarration.Line(DeathCause.Scuttled, DeathPlace.OwnShip, seed, "Long Shrift"));
        }
    }

    [Fact]
    public void TheScuttleCardNeverSaysSomethingElseKilledYou()
    {
        // Every seeded line in the pool, against the vocabulary of the death it was wearing.
        string[] borrowed = ["Old One", "took you", "nerve gave out", "nerves", "regolith", "the tube", "the crowd"];

        var wrong = new List<string>();
        for (ulong seed = 0; seed < 24; seed++)
        {
            string line = DeathNarration.Line(DeathCause.Scuttled, DeathPlace.Derelict, seed, "Long Shrift");
            foreach (string word in borrowed)
            {
                if (line.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    wrong.Add($"  seed {seed}: \"{word}\" — {line}");
                }
            }
            Assert.Contains("Long Shrift", line, StringComparison.Ordinal);
        }

        Report(wrong, "scuttle-death lines wearing another death's language");
    }

    [Fact]
    public void EveryDeathCauseHasAHeadlineOfItsOwn()
    {
        // The default arm is "WHAT HAPPENED" with nothing after it. A cause absorbed by a `_ =>` default is
        // exactly how `death-suffocated.jpg` stayed a promise nothing kept for a year (#621).
        var bare = new List<string>();
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            if (DeathNarration.Headline(cause) == "WHAT HAPPENED")
            {
                bare.Add($"  {cause} — the card's headline is the bare fallback, with no what after the happened.");
            }
        }

        Report(bare, "death causes with no headline of their own");
    }

    [Fact]
    public void TheScuttleDeathShowsAPictureTheGameActuallyShips()
    {
        Assert.Equal("death-derelict.jpg", DeathNarration.ArtFile(DeathCause.Scuttled, DeathPlace.Derelict));
        Assert.Equal(" Her log will not mention it.", DeathNarration.Tail(DeathCause.Scuttled, DeathPlace.Derelict));
    }

    /// <summary>
    /// AND THE CLOCK HAS TO ACTUALLY SAY SO. Every assertion above is about a seam the caller is free not to
    /// use — which is the whole bug: the pool, the tail and the picture would all have been correct the
    /// moment anybody passed the cause, and for a year nobody did. So this reads the live source, the same
    /// idiom <c>CssZBandSyncTests</c> and <c>TheDeathCardReadsTheNarrationSeamTests</c> use.
    /// </summary>
    [Fact]
    public void TheOverloadTellsTheDeathWhatKilledTheCaptain()
    {
        string source = ReadRepoFile("src/SpaceSails.Client/Pages/Map.Scuttle.cs");
        string code = Regex.Replace(source, @"//.*$", string.Empty, RegexOptions.Multiline);

        Match trigger = Regex.Match(code, @"TriggerSurfaceOverdrawDeath\((?<args>[^;]*)\);", RegexOptions.Singleline);
        Assert.True(trigger.Success, "Map.Scuttle.cs no longer kills anybody when the overload runs out");

        string args = trigger.Groups["args"].Value;
        Assert.True(args.Contains("known:", StringComparison.Ordinal),
            "the overload runs out and hands the death card NO cause, so it rolls DeathNarration.SurfaceEnd "
            + "and blames the Old Ones for a reactor the captain set themselves. Pass "
            + $"known: DeathCause.Scuttled. Got: TriggerSurfaceOverdrawDeath({args.Trim()})");
        Assert.Contains("DeathCause.Scuttled", args, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(string rel)
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException(rel);
    }

    private static void Report(List<string> bad, string what)
    {
        if (bad.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{bad.Count} {what}:");
        foreach (string line in bad)
        {
            sb.AppendLine(line);
        }
        Assert.Fail(sb.ToString());
    }
}

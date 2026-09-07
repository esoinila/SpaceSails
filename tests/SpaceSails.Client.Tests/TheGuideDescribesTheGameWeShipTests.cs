using System;
using System.Globalization;
using System.IO;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #380 item 4 · <b>THE GUIDE IS A REPORTER, AND A REPORTER CAN GO STALE.</b>
///
/// <para>Owner ruling (2026-07-19) behind the whole issue: <i>big events must be EXPLAINED when they
/// happen, and every event that mystifies new players needs the same treatment.</i> The read-only audit's
/// item 4 found the worst instance of the opposite — the user guide's war-room section was still teaching
/// the <b>pre-BUSTED</b> catch: <i>"it seizes your hold plus a 500 cr fine"</i>. That flow had not existed
/// since PR-BUSTED replaced it with <see cref="BustedRule"/>'s submit / bribe / resist ladder, and
/// <c>docs/features/war-room.md</c> was saying the same wrong thing. A new player who read the guide met a
/// boarding, three dice-shown options and a resurrection that the guide had never mentioned.</para>
///
/// <h3>Why these guards read the CODE and not a typed-in number</h3>
/// <para>A doc test that asserts <c>Contains("20%")</c> is the fifth bug class in this repo: it is green on
/// a guide that has drifted and green on a guide that has not, because nothing in it can tell the two
/// apart. So every number and every word asserted below is <b>rendered from the Core constant the game
/// actually runs on</b> — <see cref="BustedRule.CoinFraction"/>, <see cref="BustedRule.MinBerthFeeCr"/>,
/// <see cref="BustedRule.InsuranceCredits"/>, <see cref="CacheSafety.Word"/>. Change the law and the
/// documentation reddens; leave the law alone and re-word the prose around the figure and it stays green,
/// which is the correct sensitivity for a guide.</para>
///
/// <para>The claims are made over the SECTION, sliced by its own heading, rather than over the whole file.
/// "Does this document mention resurrection somewhere" and "does the getting-caught section explain it" are
/// two different questions, and only the second one is item 4.</para>
/// </summary>
public sealed class TheGuideDescribesTheGameWeShipTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "docs")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Doc(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "docs", .. parts]));

    /// <summary>The text between one heading and the next one at or above its level — the section as a
    /// reader meets it. Sliced rather than grepped for the reason in the class summary.</summary>
    private static string Section(string doc, string heading, string nextHeading)
    {
        int from = doc.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(from >= 0, $"the guide no longer has a section headed \"{heading}\"");

        int to = doc.IndexOf(nextHeading, from + heading.Length, StringComparison.Ordinal);
        Assert.True(to > from, $"\"{heading}\" is no longer followed by \"{nextHeading}\"");

        return doc[from..to];
    }

    /// <summary>A section with its markdown emphasis and its line breaks taken out, so a claim about the
    /// SENTENCE is not defeated by a bold marker or a wrap. The stale model this file exists to keep out
    /// was written both as <c>a 500 cr fine</c> and as <c>a **500 cr** fine</c>, and a guard that could be
    /// satisfied by re-bolding it would be no guard.</summary>
    private static string Flattened(string section) =>
        string.Join(' ', section.Replace("*", "", StringComparison.Ordinal)
                                .Replace("`", "", StringComparison.Ordinal)
                                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The pre-BUSTED consequence, as either guide phrased it: a flat cash penalty on top of the
    /// seizure. Asserted as the PHRASE and not as the number 500, because "1,500 cr" is a live bribe band
    /// and a guard that reddens on the right answer is worse than no guard (this one did, on first run).
    /// </summary>
    private static void CarriesNoFlatFine(string section)
    {
        string flat = Flattened(section);
        Assert.DoesNotContain("cr fine", flat, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credit fine", flat, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cr penalty", flat, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The coin share as a guide would write it: <c>BustedRule.CoinFraction(2)</c> → "35%".</summary>
    private static string PurseShare(int heat) =>
        (BustedRule.CoinFraction(heat) * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    private static string WarRoom() => Section(
        Doc("user-guide.md"), "## 12. The War room", "## 13. The scope");

    // ── THE CATCH ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FLAT FINE IS GONE FROM THE CODE, SO IT IS GONE FROM THE GUIDE. <c>EncounterRule</c> carried a
    /// <c>CatchFineCredits = 500</c> that nothing had read since PR-BUSTED; both guides went on quoting it.
    /// The constant is deleted and this is the claim that keeps the sentence from growing back.
    ///
    /// <para><b>Proven RED</b> by restoring the shipped sentence ("it seizes your hold plus a 500 cr
    /// fine") to §12: the assertion below fails on the fine.</para>
    /// </summary>
    [Fact]
    public void TheGuidesCatchSectionDoesNotStillTeachTheFlatFine()
    {
        string section = WarRoom();

        CarriesNoFlatFine(section);

        // …and it says what a catch IS, which is the half of item 4 a deletion alone would not fix.
        Assert.Contains("boarding, not a fine", section, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AND IT QUOTES THE LADDER THE CODE ACTUALLY RUNS. Every figure is rendered from the Core constant, so
    /// re-tuning the confiscation reddens the documentation instead of silently outdating it.
    ///
    /// <para><b>Proven RED</b> twice: by restoring the shipped §12 (none of the three shares appear), and by
    /// changing <see cref="BustedRule.CoinFraction"/>'s heat-2 rung from 0.35 to 0.30 with the new §12 in
    /// place — the guide's "35%" no longer answers the code and the heat-2 claim fails.</para>
    /// </summary>
    [Fact]
    public void TheGuidesCatchSectionQuotesTheConfiscationTheCodeRuns()
    {
        string section = WarRoom();

        // The three answers on the BUSTED card (Map.Combat.Busted: BustedSubmit / BustedBribe / BustedResist).
        Assert.Contains("Submit", section, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Bribe", section, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Resist", section, StringComparison.OrdinalIgnoreCase);

        // The heat-scaled purse share, rendered from BustedRule itself.
        for (int heat = 1; heat <= EncounterRule.MaxHeatLevel; heat++)
        {
            Assert.Contains(PurseShare(heat), section, StringComparison.Ordinal);
        }

        // The mercy floor, and the resurrection stake — the two numbers that decide whether a busted or dead
        // captain is stranded, which is exactly what a new player is trying to find out. Asserted WITH their
        // unit attached, so a bare "100" occurring somewhere else in the section cannot satisfy the claim.
        Assert.Contains($"~{BustedRule.MinBerthFeeCr.ToString(CultureInfo.InvariantCulture)} cr", section,
            StringComparison.Ordinal);
        Assert.Contains($"{BustedRule.InsuranceCredits.ToString(CultureInfo.InvariantCulture)} cr", section,
            StringComparison.Ordinal);

        // The heat-3 rung is not a roll but the last stand, and the last stand is not the end.
        Assert.Contains("Bolivia", section, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resurrection", section, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// THE FEATURE NOTE IS A GUIDE TOO. <c>docs/features/war-room.md</c> carried the identical stale
    /// sentence, and it is the document a future lane would read before touching the catch.
    ///
    /// <para><b>Proven RED</b> by restoring its shipped paragraph: the fine reappears and the ladder does
    /// not.</para>
    /// </summary>
    [Fact]
    public void TheWarRoomFeatureNoteDescribesTheBustedLadderAndNotTheOldToll()
    {
        string hunters = Section(Doc("features", "war-room.md"), "## Hunters", "## Hiding at havens");

        CarriesNoFlatFine(hunters);
        Assert.Contains(nameof(BustedRule), hunters, StringComparison.Ordinal);
        Assert.Contains(nameof(BoliviaEncounter), hunters, StringComparison.Ordinal);

        // The whole ladder as one token — "20/35/50%" — assembled from the Core fractions. Asserted joined
        // rather than one rung at a time, because a bare "50" is also the tail of a bribe band and a claim a
        // wrong document could satisfy by accident is not a claim.
        string ladder = string.Join('/', [PurseShare(1)[..^1], PurseShare(2)[..^1], PurseShare(3)]);
        Assert.Contains(ladder, hunters, StringComparison.Ordinal);
    }

    // ── THE HOARD ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #380 item 6, AS THE GUIDE TELLS IT. The bury flow has said since #455 that a cache can be dug up by
    /// rivals and named the rung it was buried at; the guide still said only that no confiscation can touch
    /// it, which is true and, on its own, reads as a promise the dice do not make.
    ///
    /// <para>The three words are read out of <see cref="CacheSafety.Word"/> — the same oracle the bury line
    /// and the ledger row use — so renaming a rung reddens the guide rather than quietly desynchronising it
    /// from the sentence the player is shown on the regolith.</para>
    ///
    /// <para><b>Proven RED</b> by restoring the shipped bullet (no rung word appears), and again by renaming
    /// <c>CacheSafetyRung.Considered</c>'s word to "Buried" with the new bullet in place.</para>
    /// </summary>
    [Fact]
    public void TheGuideSaysACacheCanBeDugUpAndNamesTheRungsCoreNames()
    {
        string ashore = Section(
            Doc("user-guide.md"),
            "## 16. Going ashore",
            "## 17. Going below");

        Assert.Contains("rivals", ashore, StringComparison.OrdinalIgnoreCase);

        foreach (CacheSafetyRung rung in Enum.GetValues<CacheSafetyRung>())
        {
            Assert.Contains(CacheSafety.Word(rung), ashore, StringComparison.Ordinal);
        }
    }
}

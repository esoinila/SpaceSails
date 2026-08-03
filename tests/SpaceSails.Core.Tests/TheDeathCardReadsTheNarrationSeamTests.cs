using System;
using System.IO;
using System.Text.RegularExpressions;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #621 · A GUARD ON THE SEAM ITSELF, because the last one was walked around.
///
/// <para>#574 pulled <i>"The freeze-frame holds."</i> — a sentence about a collector's gun-camera still —
/// out of every death and put it behind <see cref="DeathNarration.Tail"/>, which answers it for a collector
/// and nothing else. <c>OnlyACOLLECTORDeathTalksAboutTheFreezeFrame</c> has pinned that law ever since, and
/// it was green the whole time the IMPACT card was printing the line anyway, because the markup did not call
/// <c>Tail</c>: it had the sentence typed into it, appended to a hand-copy of <c>ImpactLines[1]</c>.</para>
///
/// <para>A rule enforced on a function that a screen is free not to call is not enforced. So this reads the
/// LIVE razor — the same idiom <see cref="CssZBandSyncTests"/> uses to hold the stylesheets to
/// <c>OverlayBands</c> — and asks whether the death cards go through the seam at all.</para>
/// </summary>
public class TheDeathCardReadsTheNarrationSeamTests
{
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

    private static string Markup => ReadRepoFile("src/SpaceSails.Client/Pages/Map.razor");

    /// <summary>The markup with its <c>@* … *@</c> comments removed. A comment EXPLAINING why a sentence
    /// must not be printed is not the sentence being printed, and a guard that cannot tell the two apart
    /// would forbid the note that keeps the rule from being undone.</summary>
    private static string Printed =>
        Regex.Replace(Markup, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);

    [Fact]
    public void TheGunCameraSentenceAppearsOnceAndOnlyInTheCollectorsOwnFreeze()
    {
        // The collector's FreezeFrame stage may say it in plain markup: it IS the collector's death, the
        // one the phrase was written for, and that stage is only ever reached by losing the Bolivia.
        // Anywhere else in this file it is a death wearing somebody else's language.
        int said = Regex.Matches(Printed, "freeze-frame holds", RegexOptions.IgnoreCase).Count;
        Assert.True(said <= 1,
            $"\"the freeze-frame holds\" is written into Map.razor {said} times. It belongs to a collector's "
            + "gun camera and to DeathNarration.Tail, which decides who gets it (#574).");

        // …and the law it is supposed to be behind still holds, for every cause and place.
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            foreach (DeathPlace place in Enum.GetValues<DeathPlace>())
            {
                if (cause == DeathCause.Collector)
                {
                    continue;
                }
                Assert.DoesNotContain("freeze-frame", DeathNarration.Tail(cause, place),
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryDeathStageOfTheCard_AsksDeathNarrationForItsWords()
    {
        // Three stages can hold a dead captain — Impact, SurfaceEnd and the resurrection panel — and each
        // one must read the seeded, place-dependent line rather than carry a copy. A copy is not merely
        // duplication here: it cannot vary by place, which is the whole mechanism #574, #583 and #609 built.
        int lines = Regex.Matches(Printed, @"DeathNarration\.Line\(").Count;
        Assert.True(lines >= 3,
            $"only {lines} of the death card's stages call DeathNarration.Line — one of them is telling its "
            + "own story again.");

        // And the impact body's name is still only ever spoken through a narrated line, never re-glued to a
        // hand-written sentence beside it.
        Assert.DoesNotContain("The hull meets @bust.ImpactBodyName", Printed, StringComparison.Ordinal);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1332 A · <b>EVERY HUB IS A LOBBY — THE WORDS.</b> Six plates and one line, Fable canon, verbatim; the lift
/// button reads a plate's first word(s) exactly as Selene Gate's SERVICE LEVEL reads SERVICE LEVEL — NO PUBLIC
/// ACCESS; and nothing here spends a reserved word.
/// </summary>
public sealed class EveryHubIsALobbyProseTests
{
    /// <summary>
    /// <b>THE CANON, TO THE BYTE.</b> Crews do not write canon: every string is asserted as the brief wrote it.
    ///
    /// <para><b>Proven RED</b> by changing one middle dot in The Deep's plate to a hyphen.</para>
    /// </summary>
    [Fact]
    public void ThePlatesAndTheLineAreFablesVerbatim()
    {
        Assert.Equal("BERTH HOTEL · RESIDENTS ONLY", HavenLevels.CinderRoostPlate);
        Assert.Equal("LONG-STAY · KEYS AT THE BAR", HavenLevels.SpaceBarPlate);
        Assert.Equal("CREW QUARTERS · NO PUBLIC ACCESS", HavenLevels.RedEyePlate);
        Assert.Equal("MEMBERS' ROOMS", HavenLevels.RingsidePlate);
        Assert.Equal("ROOMS · MIND THE FLOOR", HavenLevels.TiltPlate);
        Assert.Equal("COLD ROOMS · BOOK AT THE DESK", HavenLevels.DeepPlate);
        Assert.Equal(
            "The car stops where the public map does not go. Somebody lives here, and it is not you.",
            HavenLevels.FirstRideLine);

        // Selene Gate's words are #1253's and did not move.
        Assert.Equal("LOWER CONCOURSE", HavenLevels.LowerConcoursePlate);
        Assert.Equal("SERVICE LEVEL — NO PUBLIC ACCESS", HavenLevels.NoPublicAccessPlate);

        Assert.Equal(7, HavenLevels.AllProse().Count());
        Assert.Equal(7, HavenLevels.AllProse().Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>THE BUTTON IS THE PLATE'S FIRST WORD(S).</b> Selene Gate's button is reproduced, not restated; every
    /// other station's is read off its own plate; a plate with no separator is its own button.
    ///
    /// <para><b>Proven RED</b> by cutting only at <c> · </c>: Selene Gate's button would read the whole of
    /// SERVICE LEVEL — NO PUBLIC ACCESS.</para>
    /// </summary>
    [Fact]
    public void TheButtonIsThePlatesFirstWords()
    {
        Assert.Equal(HavenLevels.ServiceLevelPlate, HavenLevels.StopNameOf(HavenLevels.NoPublicAccessPlate));
        Assert.Equal("BERTH HOTEL", HavenLevels.StopNameOf(HavenLevels.CinderRoostPlate));
        Assert.Equal("LONG-STAY", HavenLevels.StopNameOf(HavenLevels.SpaceBarPlate));
        Assert.Equal("CREW QUARTERS", HavenLevels.StopNameOf(HavenLevels.RedEyePlate));
        Assert.Equal("MEMBERS' ROOMS", HavenLevels.StopNameOf(HavenLevels.RingsidePlate));
        Assert.Equal("ROOMS", HavenLevels.StopNameOf(HavenLevels.TiltPlate));
        Assert.Equal("COLD ROOMS", HavenLevels.StopNameOf(HavenLevels.DeepPlate));

        // …and the panel carries it, with Selene Gate's panel the one it always was.
        Assert.Equal(
            HavenLevels.Panel(HavenLevels.ServiceLevel).Select(s => s.Name),
            HavenLevels.Panel(HavenLevels.ServiceLevel, null).Select(s => s.Name));
        Assert.Contains(
            HavenLevels.Panel(HavenLevels.Concourse, "COLD ROOMS"),
            s => s.Level == HavenLevels.ServiceLevel && s.Name == "COLD ROOMS");
        Assert.Equal("", HavenLevels.BookSuffix(HavenLevels.Concourse, "COLD ROOMS"));
        Assert.Equal("COLD ROOMS", HavenLevels.BookSuffix(HavenLevels.ServiceLevel, "COLD ROOMS"));
    }

    /// <summary><b>NOT ONE WORD SPENDS A RESERVED WORD</b> (worldbuilding-notes §8, and the fifteen beside
    /// it) and not one of them names anybody — a plate that knew a name would be the building confirming
    /// something about someone.</summary>
    [Fact]
    public void NotOneWordSpendsAReservedWord()
    {
        string[] reserved =
        [
            "monolith", "reever", "old one", "old ones", "ancient", "alien", "not ours", "not natural",
            "restore", "backup", "kaamos", "minister", "donor", "they were people", "whose", "who made",
        ];

        foreach (string line in HavenLevels.AllProse())
        {
            foreach (string word in reserved)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

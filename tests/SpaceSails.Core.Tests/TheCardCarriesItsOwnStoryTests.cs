using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #679 · THE CARD CARRIES ITS OWN STORY. Owner, live playtest 2026-08-04: <i>"We need story telling about
/// whether cards etc work or not."</i>
///
/// <para>Two gaps, one chain. A carried card never named its building — so a captain holding three
/// authorities from three moons saw three identical shapes and could not plan a wallet. And the gate's
/// refusal answered the same sentence every time, so a try left the player knowing exactly what they knew
/// before it. TRY is a verb, not a slot machine: every offer has to leave you knowing more.</para>
///
/// <para>Canon is untouched and is pinned twice over here: the card says WHERE it works and never once what
/// the building was for (§13.8).</para>
/// </summary>
public sealed class TheCardCarriesItsOwnStoryTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static Satchel.Item Held(string body, int band) =>
        new(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(body, band).Id);

    [Fact]
    public void ACardNamesTheSITEItRunsAndTheSHAFTItRuns()
    {
        // Owner: "a captain holding three cards from three moons sees three identical shapes and cannot plan
        // a wallet." The card is a durable possession that crosses worlds by design (#613's foreign card),
        // and the one thing a pass has always had printed on it is where its holder worked.
        //
        // It is said in the office's OWN register — a site designation in caps, the way everything else that
        // office stamps is in caps — rather than as a position, a distance or anything a tracker could act on.
        foreach (string body in Bodies)
        {
            for (int band = 0; band < 4; band++)
            {
                string title = UndergroundComplex.CardTitle(new UndergroundComplex.AuthorityCard(body, band));

                Assert.Contains($"SHAFT {band + 1}", title, StringComparison.Ordinal);
                Assert.Contains(BodyNames.Designation(body), title, StringComparison.Ordinal);

                // Never a nav fix. Naming the site is a wallet you can sort; a coordinate is a quest marker.
                foreach (string forbidden in new[] { " du", "bearing", "coordinate", "tracker" })
                {
                    Assert.DoesNotContain(forbidden, title, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void TwoCardsFromTwoSitesNeverReadTheSameOnTheRow()
    {
        // The failure the owner actually saw: a pocketful of shapes that cannot be told apart. Same shaft
        // number, different building, must be a different row — and that is exactly the case the old title
        // could not distinguish, because it printed the shaft and an office and nothing else.
        for (int band = 0; band < 4; band++)
        {
            var titles = new HashSet<string>(StringComparer.Ordinal);
            foreach (string body in Bodies)
            {
                Assert.True(
                    titles.Add(UndergroundComplex.CardTitle(new UndergroundComplex.AuthorityCard(body, band))),
                    $"two sites print the same card row at band {band} — the wallet cannot be sorted.");
            }
        }
    }

    [Fact]
    public void TheSatchelRowIsTheCardsOwnTitleAndNotASecondCopyOfIt()
    {
        // The prose half of #679 lives on a row of a razor list, which no Core test can call. What it CAN do
        // is hold the client to reading the one source: a hand-written row in Map.Surface would be a second
        // answer to "what is printed on this card", and this repo's table of expensive bugs is that shape
        // top to bottom. Same idiom as TheDeathCardReadsTheNarrationSeamTests, for the same reason.
        string surface = ReadRepoFile("src/SpaceSails.Client/Pages/Map.Surface.cs");
        int at = surface.IndexOf("private static string SatchelLabel", StringComparison.Ordinal);
        Assert.True(at > 0, "SatchelLabel has moved — this guard is reading a file that no longer says it.");

        string label = surface[at..(at + 900)];
        Assert.Contains("UndergroundComplex.CardTitle", label, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryWayACardCanBeWrongSaysSomethingDIFFERENT()
    {
        // THE OUTCOME MATRIX. The #590 lift refusal already meets this standard — it names what you ARE
        // carrying — and the gate did not: "countersigned, current, and for another shaft" answered the wrong
        // shaft and the wrong MOON with one sentence, so the second card in a wallet taught the player
        // nothing the first had not.
        //
        // Four answers, and a player must be able to tell them apart from the words alone.
        string wanted = new UndergroundComplex.AuthorityCard("luna", 2).Id;

        var said = new Dictionary<string, SatchelTry.Outcome>(StringComparer.Ordinal)
        {
            ["right card"] = SatchelTry.Offer(Held("luna", 2), SatchelTry.Target.ShaftGate, wanted),
            ["wrong shaft, this site"] = SatchelTry.Offer(Held("luna", 3), SatchelTry.Target.ShaftGate, wanted),
            ["wrong site"] = SatchelTry.Offer(Held("titan", 2), SatchelTry.Target.ShaftGate, wanted),
            ["sealed way"] = SatchelTry.Offer(Held("luna", 2), SatchelTry.Target.SealedWay),
            ["room door"] = SatchelTry.Offer(Held("luna", 2), SatchelTry.Target.RoomDoor),
        };

        foreach ((string a, SatchelTry.Outcome first) in said)
        {
            foreach ((string b, SatchelTry.Outcome second) in said)
            {
                if (a == b)
                {
                    continue;
                }
                Assert.False(string.Equals(first.Line, second.Line, StringComparison.Ordinal),
                    $"'{a}' and '{b}' are answered with the same sentence — trying taught the player nothing.");
            }
        }

        Assert.True(said["right card"].Worked);
        Assert.False(said["wrong shaft, this site"].Worked);
        Assert.False(said["wrong site"].Worked);

        // The wrong shaft, in this building: it names the shaft the card DOES run, so the captain learns
        // which floor of the building they are standing in is worth going back to.
        Assert.Contains("4", said["wrong shaft, this site"].Line, StringComparison.Ordinal);

        // The wrong site entirely: it names the site on the card, which is the line that turns a wallet of
        // foreign authorities into a reason to keep flying (#613).
        Assert.Contains(BodyNames.Designation("titan"), said["wrong site"].Line, StringComparison.Ordinal);
        Assert.DoesNotContain(BodyNames.Designation("titan"), said["wrong shaft, this site"].Line,
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheSEALEDWayAndTheRoomDoorAnswerExactlyAsTheyDidBefore()
    {
        // #590 call 2, held — and deliberately NOT improved. A sealed way's refusal is FINAL: it may not
        // name a card, a shaft or a site, because doing so would hint that some card somewhere opens it and
        // none does. The mechanical room door is the same call one size down: it is not refusing you, it has
        // simply been shut for longer than you have been alive.
        SatchelTry.Outcome sealedWay = SatchelTry.Offer(Held("titan", 1), SatchelTry.Target.SealedWay);
        Assert.False(sealedWay.Worked);
        Assert.Contains("it is not waiting", sealedWay.Line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(BodyNames.Designation("titan"), sealedWay.Line, StringComparison.Ordinal);
        Assert.DoesNotContain("shaft", sealedWay.Line, StringComparison.OrdinalIgnoreCase);

        SatchelTry.Outcome door = SatchelTry.Offer(Held("titan", 1), SatchelTry.Target.RoomDoor);
        Assert.False(door.Worked);
        Assert.Contains("mechanical", door.Line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(BodyNames.Designation("titan"), door.Line, StringComparison.Ordinal);
    }

    [Fact]
    public void SayingWHEREItWorksNeverBecomesSayingWHATItWasFor()
    {
        // §13.8, at the one place in the building most tempting to break it. A card may now name its site;
        // it still never says what was bought there, and no gate refusal does either.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave", "subject"];

        var text = new List<string>();
        foreach (string body in Bodies)
        {
            for (int band = 0; band < 5; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                text.Add(UndergroundComplex.CardTitle(card));
                text.Add(UndergroundComplex.CardAcceptedLine(card));
                text.Add(CarriedObject.WhatItRuns(card, "luna"));
                foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
                {
                    text.Add(SatchelTry.Offer(Held(body, band), target,
                        new UndergroundComplex.AuthorityCard("luna", 2).Id).Line);
                }
            }
        }

        foreach (string line in text)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }

            // And the sealed doors keep their silence in every one of those sentences (#590 call 2).
            Assert.DoesNotContain("SECTOR", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Walk up from the test binary until the repo file turns up — the same reader
    /// <see cref="TheDeathCardReadsTheNarrationSeamTests"/> and <c>CssZBandSyncTests</c> use.</summary>
    internal static string ReadRepoFile(string rel)
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
}

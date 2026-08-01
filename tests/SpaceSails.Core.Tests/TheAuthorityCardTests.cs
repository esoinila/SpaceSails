using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #590 · THE CARD THAT OPENS SOMETHING. Owner: <i>"could there be like a keycode etc that allows us access
/// to the lab"</i>.
///
/// <para><see cref="UndergroundComplex.Haul.Key"/> shipped saying <i>"Something down here will open for
/// this"</i> and opening nothing — an affordance you can see and cannot use, which is worse than not
/// offering one. These pin the promise now that it is kept, and pin the three calls made in keeping it.</para>
/// </summary>
public sealed class TheAuthorityCardTests
{
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    [Fact]
    public void AKeyRoomHoldsTheCardForTheShaftBelowTheBandItIsFoundIn()
    {
        // The rule the whole mechanic rests on, and the reason it is legible: the card you need for the next
        // shaft is always somewhere in the band you are standing in. Work the floors and you get deeper.
        foreach (string body in Bodies)
        {
            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                UndergroundComplex.AuthorityCard? card = UndergroundComplex.CardInRoom(body, level);
                if (card is null)
                {
                    continue;
                }

                Assert.Equal(body, card.Value.BodyId);
                Assert.Equal(UndergroundComplex.BandOf(level) + 1, card.Value.Band);
                Assert.True(UndergroundComplex.SiteHasBand(body, card.Value.Band),
                    $"{body} B{-level}: a card was issued for shaft band {card.Value.Band}, which this " +
                    "site does not have — an authority for a hole nobody dug.");
            }
        }
    }

    [Fact]
    public void TheBottomBandIssuesNoCardAtAll()
    {
        // There is nothing under the last band to authorise. Handing out a card there would be the same
        // broken promise this issue exists to close, one level further down.
        foreach (string body in Bodies)
        {
            int bottom = UndergroundComplex.DepthOf(body);
            int lastBand = UndergroundComplex.BandOf(bottom);

            for (int level = -1; level >= bottom; level--)
            {
                bool inLastBand = UndergroundComplex.BandOf(level) == lastBand;
                UndergroundComplex.AuthorityCard? card = UndergroundComplex.CardInRoom(body, level);
                Assert.Equal(!inLastBand, card is not null);
            }
        }
    }

    [Fact]
    public void ACardIsAFactAboutTheWorldAndNotARollAtTheMomentOfUse()
    {
        // Determinism is law in Core. Which card opens which shaft has to be the same on every visit and in
        // every session, or a save that carries the id would be carrying a lie.
        foreach (string body in Bodies)
        {
            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                Assert.Equal(UndergroundComplex.CardInRoom(body, level), UndergroundComplex.CardInRoom(body, level));
                Assert.Equal(UndergroundComplex.KeyLine(body, level), UndergroundComplex.KeyLine(body, level));
            }
        }

        var one = new UndergroundComplex.AuthorityCard("luna", 1);
        Assert.Equal(UndergroundComplex.CardTitle(one), UndergroundComplex.CardTitle(one));
        Assert.NotEqual(
            UndergroundComplex.CardTitle(one).Split('·')[^1],
            UndergroundComplex.CardTitle(new UndergroundComplex.AuthorityCard("titan", 1)).Split('·')[^1]);
    }

    [Fact]
    public void OneSitesCardNeverRunsAnothersShaft()
    {
        // A wallet full of authorities from five moons must not turn into a skeleton key. The identity
        // carries the BODY, which is what makes the refusal line ("every one of them for another shaft")
        // honest rather than a fiction over a global unlock.
        var seen = new HashSet<string>();
        foreach (string body in Bodies)
        {
            for (int band = 0; band < 6; band++)
            {
                Assert.True(seen.Add(new UndergroundComplex.AuthorityCard(body, band).Id),
                    $"two different shafts share one card id — {body} band {band}.");
            }
        }
    }

    [Fact]
    public void ACardSurvivesBeingWrittenDownAndReadBack()
    {
        // The save carries the id and nothing else, so this round trip IS the persistence contract.
        foreach (string body in Bodies)
        {
            for (int band = 0; band < 6; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                Assert.True(UndergroundComplex.AuthorityCard.TryParse(card.Id, out UndergroundComplex.AuthorityCard back));
                Assert.Equal(card, back);
            }
        }

        // And an edited or future save is dropped rather than crashing a load — the vault is tolerant
        // everywhere else and a mystery key is not worth losing a game over.
        foreach (string junk in new[] { null!, "", "luna", "#3", "luna#", "luna#x", "luna#-1", "#" })
        {
            Assert.False(UndergroundComplex.AuthorityCard.TryParse(junk, out _), $"'{junk}' parsed as a card.");
        }
    }

    [Fact]
    public void ACardNeverOpensASEALEDSECTORDoor()
    {
        // #590's option (2), declined ON PURPOSE and pinned here so it stays declined.
        //
        // The SECTOR doors exist to be walls with a world behind them — LockedLine deliberately never
        // teases, because the moment ONE of them can open, every one of them becomes a puzzle and the
        // owner's cheap illusion of scale turns into a lock hunt. The only way that call can be broken
        // silently is in the prose, so the prose is what is pinned: nothing a card says points at a sealed
        // door, and nothing a sealed door says points at a card.
        var cardWords = new List<string>();
        foreach (string body in new[] { "luna", "miranda", "titan" })
        {
            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                cardWords.Add(UndergroundComplex.KeyLine(body, level));
            }
            for (int band = 0; band < 4; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                cardWords.Add(UndergroundComplex.CardTitle(card));
                cardWords.Add(UndergroundComplex.CardAcceptedLine(card));
                cardWords.Add(UndergroundComplex.WrongCardLine(band * 4, [card]));
            }
            cardWords.Add(UndergroundComplex.WrongCardLine(2, []));
        }

        foreach (string line in cardWords)
        {
            Assert.DoesNotContain("SECTOR", line, StringComparison.OrdinalIgnoreCase);
        }

        // And from the other side: a door that will not open never hints that something might open it.
        foreach (UndergroundComplex.Kind kind in Enum.GetValues<UndergroundComplex.Kind>())
        {
            foreach (string sign in UndergroundComplex.SignsFor(kind))
            {
                string said = UndergroundComplex.LockedLine(sign);
                foreach (string tease in new[] { "card", "authority", "shaft", "code", "pass" })
                {
                    Assert.DoesNotContain(tease, said, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void RefusingToDescendAlwaysSaysWHY()
    {
        // "It must be discoverable that you have it" — a gate that just sits there is indistinguishable
        // from a bug, and this game has shipped exactly that mistake before (the map lying).
        string empty = UndergroundComplex.WrongCardLine(4, []);
        Assert.Contains("shaft", empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("B4", empty, StringComparison.Ordinal);

        var held = new[]
        {
            new UndergroundComplex.AuthorityCard("titan", 2),
            new UndergroundComplex.AuthorityCard("europa", 1),
        };
        string wrong = UndergroundComplex.WrongCardLine(8, held);

        // It NAMES what is in the pocket, so "I am carrying a card and it did nothing" can never happen
        // without an explanation attached to it.
        foreach (UndergroundComplex.AuthorityCard c in held)
        {
            Assert.Contains(UndergroundComplex.CardTitle(c), wrong, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheCardExplainsNothingEITHER()
    {
        // Canon, at the one place in the building most tempting to break it: a card is countersigned by an
        // office that denies existing, and that office never says what it was buying.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var text = new List<string>();
        foreach (string body in Bodies)
        {
            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                text.Add(UndergroundComplex.KeyLine(body, level));
            }
            for (int band = 0; band < 6; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                text.Add(UndergroundComplex.CardTitle(card));
                text.Add(UndergroundComplex.CardAcceptedLine(card));
                text.Add(UndergroundComplex.WrongCardLine(band * 4, [card]));
            }
        }

        foreach (string line in text)
        {
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void EveryCardYouCanEVERBeIssuedIsOneYouCanEVERUse()
    {
        // The closing of the whole loop, said as one assertion: for every site, every card the generator can
        // hand out is the card some shaft on that same site is waiting for. A card that authorises nothing
        // is the bug this issue was filed about.
        foreach (string body in Bodies)
        {
            var issued = new HashSet<int>();
            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                if (UndergroundComplex.CardInRoom(body, level) is { } card)
                {
                    issued.Add(card.Band);
                }
            }

            // The bands a captain could actually be standing at the bottom of, wanting the next one.
            var wanted = new HashSet<int>();
            for (int level = -1; level >= UndergroundComplex.DepthOf(body); level--)
            {
                if (UndergroundComplex.IsBandBottom(body, level))
                {
                    wanted.Add(UndergroundComplex.BandOf(level) + 1);
                }
            }

            Assert.Equal(wanted.OrderBy(b => b), issued.OrderBy(b => b));
        }
    }
}

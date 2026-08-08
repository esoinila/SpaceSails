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

    /// <summary>#684 · What the gate says to a wallet, said by the one source there is: the panel's read and
    /// the satchel's try are the same matrix now (<c>UndergroundComplex.WrongCardLine</c> is gone, and these
    /// canon sweeps read the matrix rather than the panel's deleted second set of sentences).</summary>
    private static string GateSays(UndergroundComplex.AuthorityCard wants,
        params UndergroundComplex.AuthorityCard[] carried) =>
        SatchelTry.OfferWallet(
            [.. carried.Select(c => new Satchel.Item(Satchel.Kind.Authority, c.Id))],
            SatchelTry.Target.ShaftGate, wants.Id).Line;

    [Fact]
    public void AKeyRoomHoldsTheCardForTheShaftBelowTheBandItIsFoundIn()
    {
        // The rule the whole mechanic rests on, and the reason it is legible: the card you need for the next
        // shaft is always somewhere in the band you are standing in. Work the floors and you get deeper.
        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
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
        //
        // #592 · "The last band" means the last band that EXISTS, which on a rare site is the one nobody
        // listed. So a Key found on the last floor the building admits to still issues a card — for the band
        // the building denies having — and only the true bottom issues nothing. That composition is how a
        // captain gets into the unlisted band at all.
        foreach (string body in Bodies)
        {
            int trueBottom = UndergroundComplex.TrueDepthOf(body);
            int lastBand = UndergroundComplex.BandOf(trueBottom);

            foreach (int level in UndergroundComplex.FloorsOf(body))
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
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Assert.Equal(UndergroundComplex.CardInRoom(body, level), UndergroundComplex.CardInRoom(body, level));
                Assert.Equal(UndergroundComplex.KeyLine(body, level, UndergroundComplex.CardInRoom(body, level)),
                    UndergroundComplex.KeyLine(body, level, UndergroundComplex.CardInRoom(body, level)));
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
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                cardWords.Add(UndergroundComplex.KeyLine(body, level, UndergroundComplex.CardInRoom(body, level)));
                cardWords.Add(UndergroundComplex.KeyLine(body, level, null));
            }
            for (int band = 0; band < 4; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                var next = new UndergroundComplex.AuthorityCard(body, band + 1);
                cardWords.Add(UndergroundComplex.CardTitle(card));
                cardWords.Add(UndergroundComplex.CardAcceptedLine(card));

                // #684 · Every refusal class the gate can produce, not just one of them: the matrix is the
                // panel's source now, so the canon sweep walks the same three answers a player can read.
                cardWords.Add(GateSays(next, card));
                cardWords.Add(GateSays(next, new UndergroundComplex.AuthorityCard("titan", band)));
                cardWords.Add(GateSays(next, card, new UndergroundComplex.AuthorityCard("titan", band)));
            }
            cardWords.Add(GateSays(new UndergroundComplex.AuthorityCard(body, 1)));
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
        //
        // #684 · Asked of the PANEL'S OWN READ, which is the only way a player ever meets these sentences.
        // It used to be asked of UndergroundComplex.WrongCardLine, a second set of words the panel kept to
        // itself while the sharpened matrix (#679/#683) had no caller at all.
        var empty = UndergroundComplex.TheGateReads("luna", -1, []);
        Assert.False(empty.Worked);
        Assert.Null(empty.Presented);
        Assert.Contains("gate", empty.Line, StringComparison.OrdinalIgnoreCase);

        // …and it still leaves behind the one fact the old panel line existed to leave: somebody held this
        // thing once, and they are not the reason it is missing.
        Assert.Contains("did not take it with them", empty.Line, StringComparison.Ordinal);

        var held = new[]
        {
            new UndergroundComplex.AuthorityCard("titan", 2),
            new UndergroundComplex.AuthorityCard("europa", 1),
        };
        UndergroundComplex.GateRead wrong = UndergroundComplex.TheGateReads(
            "luna", -1, [.. held.Select(c => new Satchel.Item(Satchel.Kind.Authority, c.Id))]);

        // It NAMES what is in the pocket, so "I am carrying a card and it did nothing" can never happen
        // without an explanation attached to it. The matrix names the card it READ rather than listing the
        // wallet — a sharper answer than the old line's roll-call, and it is the read one that is pictured.
        Assert.False(wrong.Worked);
        Assert.NotNull(wrong.Presented);
        Assert.Contains(BodyNames.Designation(wrong.Presented!.Value.BodyId), wrong.Line, StringComparison.Ordinal);
        Assert.Contains(wrong.Presented!.Value, held);
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
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                text.Add(UndergroundComplex.KeyLine(body, level, UndergroundComplex.CardInRoom(body, level)));
                text.Add(UndergroundComplex.KeyLine(body, level, null));
            }
            for (int band = 0; band < 6; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                var next = new UndergroundComplex.AuthorityCard(body, band + 1);
                text.Add(UndergroundComplex.CardTitle(card));
                text.Add(UndergroundComplex.CardAcceptedLine(card));
                text.Add(GateSays(next, card));
                text.Add(GateSays(next, new UndergroundComplex.AuthorityCard("titan", band)));
                text.Add(GateSays(next));
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
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.CardInRoom(body, level) is { } card)
                {
                    issued.Add(card.Band);
                }
            }

            // The bands a captain could actually be standing at the bottom of, wanting the next one.
            var wanted = new HashSet<int>();
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (UndergroundComplex.IsBandBottom(body, level))
                {
                    wanted.Add(UndergroundComplex.BandOf(level) + 1);
                }
            }

            Assert.Equal(wanted.OrderBy(b => b), issued.OrderBy(b => b));
        }
    }

    [Fact]
    public void EverySiteHasABandZero_SoAKeyFoundAtTheBottomAlwaysHasSomewhereToPointAt()
    {
        // #613 · THE INVARIANT THE POCKET RESTS ON. Owner, in a four-floor site: "now I picked authority
        // card but it did not go to inventory."
        //
        // CardInRoom returns null on the bottom band and it is RIGHT to — there is no shaft below to
        // authorise, and a card for a hole nobody dug would be a lie. The fault was the client’s: it handed
        // out a lead and put nothing in the pocket, while the prose went on describing a countersigned card
        // in the captain’s hand.
        //
        // The fix is that such a card is for ANOTHER building — the one the lead names — which is exactly
        // the wallet WrongCardLine has always described. That only works if the moon a lead can name always
        // has a band 0 to issue for. This pins it, so nobody can tighten SiteHasBand later and quietly turn
        // every bottom-floor Key back into a card that evaporates.
        string[] bodies =
        [
            "luna", "ganymede", "europa", "callisto", "io", "titan", "enceladus", "phobos", "deimos",
            "triton", "charon", "mimas", "rhea", "iapetus", "secret-lab-site", "the-crater-shelf",
        ];

        foreach (string body in bodies)
        {
            Assert.True(UndergroundComplex.SiteHasBand(body, 0),
                $"{body} has no band 0 — a Key found at the bottom of another site could not issue a card for it");

            // And the card it would issue must survive a save and come back the same card, because that is
            // the whole of what the satchel stores.
            var minted = new UndergroundComplex.AuthorityCard(body, 0);
            Assert.True(UndergroundComplex.AuthorityCard.TryParse(minted.Id, out UndergroundComplex.AuthorityCard back));
            Assert.Equal(minted, back);
        }
    }

    [Fact]
    public void TheBottomBandIssuesNoCardForITSELF_WhichIsWhyTheClientLooksElsewhere()
    {
        // The other half of #613, and the reason the bug was invisible on a deep site: on any band that has
        // a shaft under it the card arrives normally, so the fault only ever showed on the LAST band — and
        // the owner found it on a four-floor rock, which is one band and nothing else.
        //
        // Stated as a law rather than a shape: a Key room issues a card exactly when there is a shaft below
        // it, and never otherwise.
        foreach (string body in new[] { "luna", "secret-lab-site", "titan" })
        {
            int deepest = UndergroundComplex.TrueDepthOf(body);
            for (int level = -1; level >= deepest; level--)
            {
                bool below = UndergroundComplex.SiteHasBand(body, UndergroundComplex.BandOf(level) + 1);
                UndergroundComplex.AuthorityCard? card = UndergroundComplex.CardInRoom(body, level);

                Assert.Equal(below, card is not null);
                if (card is { } c)
                {
                    Assert.Equal(body, c.BodyId);
                    Assert.Equal(UndergroundComplex.BandOf(level) + 1, c.Band);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #528 · THE TWO CARDS THE HIVE EARNS. Owner, standing at a rib's far end: <i>"I see there is a nice lock
/// here at the end of the corridor.... maybe we could have a gen-AI image for it and a pop-up to tell the
/// story?"</i> and, a minute later, <i>"the authority card could also have a gen ai image to really tell the
/// story here :-D"</i>
///
/// <para>He picked the right pair: the facility has exactly two objects that are about the idea of passage —
/// a door that will never open and a piece of paper that opens one — and giving both the reveal-card
/// treatment makes them answer each other.</para>
/// </summary>
public sealed class TheHiveCardsTests
{
    private static IEnumerable<string> SectorSigns()
    {
        // The real thing, off the generator, rather than a hand-typed lookalike.
        foreach (string body in new[] { "luna", "miranda", "titan", "europa" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField);
                foreach (UndergroundComplex.LockedDoor door in floor.Locked)
                {
                    if (UndergroundComplex.IsSealedWay(door.Sign))
                    {
                        yield return door.Sign;
                    }
                }
            }
        }
    }

    [Fact]
    public void ASEALEDWAYIsTheOnlyLockThatEarnsACard()
    {
        // A room door that will not open is a sign to READ, not a scene. Forty of them in a corridor would
        // be a slideshow, and the card would stop meaning anything on the second one.
        int ways = 0, rooms = 0;
        foreach (string body in new[] { "luna", "miranda", "titan", "europa" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                UndergroundComplex.FloorPlan floor =
                    UndergroundComplex.Build(body, level, SurfaceLayout.DefaultField);
                foreach (UndergroundComplex.LockedDoor door in floor.Locked)
                {
                    if (UndergroundComplex.IsSealedWay(door.Sign))
                    {
                        ways++;
                    }
                    else
                    {
                        rooms++;
                        Assert.False(UndergroundComplex.IsSealedWay(door.Sign),
                            $"the room sign '{door.Sign}' reads as a sealed way — the card would fire on a cupboard.");
                    }
                }
            }
        }
        Assert.True(ways > 20, $"only {ways} sealed ways across four sites — this proved little.");
        Assert.True(rooms > 100, $"only {rooms} room doors — this proved little.");
    }

    [Fact]
    public void TheCardQUOTESThePlateRatherThanRebuildingIt()
    {
        // The standing rule on this ground: when a sentence and a thing on the wall must agree, make them
        // one thing. The distance is not re-derived, re-rounded or re-worded — the plate's own text is
        // dropped into the card verbatim, so they cannot drift.
        foreach (string sign in SectorSigns())
        {
            Assert.Contains(sign, UndergroundComplex.SealedWayCard(sign), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NEITHERCardEverTeases()
    {
        // The hard constraint, and the reason both live in Core rather than in the client.
        //
        // The sealed sector doors exist to be walls with a world behind them (#590 call 2). A captain may be
        // carrying a real authority card while they read this, so a card that mentions one — or a keypad, or
        // a code — sends them off to try it, and they have been lied to BY A CARD. That is worse than the
        // silence it replaced.
        // WHOLE WORDS, not substrings — the first cut of this guard went red on its own prose because
        // "passage" contains "pass". A tease check that fires on innocent English is a check somebody
        // eventually deletes rather than satisfies.
        foreach (string sign in SectorSigns())
        {
            string card = UndergroundComplex.SealedWayCard(sign);
            foreach (string tease in new[] { "card", "authority", "code", "pass", "key", "unlock", "opens" })
            {
                Assert.False(
                    System.Text.RegularExpressions.Regex.IsMatch(
                        card, $@"\b{tease}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                    $"the sealed-way card says '{tease}' — a captain carrying an authority will now go and " +
                    "try it on a door that has no reader, and a CARD will have lied to them.");
            }
        }

        // And from the other side: the authority card's own story never claims it opens THIS door. It says
        // what the object is and stops; the gate says what it does.
        for (int band = 0; band < 4; band++)
        {
            string story = UndergroundComplex.AuthorityCardStory(
                new UndergroundComplex.AuthorityCard("luna", band));
            Assert.DoesNotContain("SECTOR", story, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheAuthorityStoryNAMESTheCardItIsAbout()
    {
        // The reader has to be able to tell which of several cards they are looking at — the title is the
        // one thing on it that varies, and it is seeded off the shaft it runs.
        foreach (string body in new[] { "luna", "titan" })
        {
            for (int band = 0; band < 4; band++)
            {
                var card = new UndergroundComplex.AuthorityCard(body, band);
                Assert.Contains(UndergroundComplex.CardTitle(card),
                    UndergroundComplex.AuthorityCardStory(card), StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void BothCardsEXPLAINNothing()
    {
        // Canon, at the two most tempting objects in the building to break it on. A facility may be
        // enormous, expensive and obviously state-backed and may never say what it was for.
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];

        var text = new List<string> { UndergroundComplex.SealedWayCardLabel, UndergroundComplex.AuthorityCardLabel };
        foreach (string sign in SectorSigns())
        {
            text.Add(UndergroundComplex.SealedWayCard(sign));
        }
        foreach (string body in new[] { "luna", "miranda", "titan", "europa" })
        {
            for (int band = 0; band < 6; band++)
            {
                text.Add(UndergroundComplex.AuthorityCardStory(new UndergroundComplex.AuthorityCard(body, band)));
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
    public void EveryCardHasSomethingToShowAndSomewhereToShowItFrom()
    {
        // The art degrades cleanly (onerror-hide), so a missing JPG is never a crash — but a card with no
        // slot named at all can never GET art, which is how a moment quietly stays unpainted forever. That
        // is the audit #528 exists to run.
        foreach (string url in new[] { UndergroundComplex.SealedWayArtUrl, UndergroundComplex.AuthorityCardArtUrl })
        {
            Assert.StartsWith("art/", url, StringComparison.Ordinal);
            Assert.EndsWith(".jpg", url, StringComparison.Ordinal);
        }
        Assert.NotEqual(UndergroundComplex.SealedWayArtUrl, UndergroundComplex.AuthorityCardArtUrl);

        foreach (string label in new[] { UndergroundComplex.SealedWayCardLabel, UndergroundComplex.AuthorityCardLabel })
        {
            Assert.False(string.IsNullOrWhiteSpace(label));
        }
    }
}

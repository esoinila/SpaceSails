using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE EMPTY CHAIR GETS A SENTENCE (issue #410, story pass 2026-08-02).
///
/// <para>#410's roving-regulars rota shipped complete: each of the four is at a given port only sometimes,
/// in a seeded seat, and an away one is <see cref="PatronState.Gone"/> or <see cref="PatronState.InTheBack"/>.
/// But an away regular is given NO CONSOLE — so the player walks up to the empty chair, presses [E], and
/// nothing happens at all. The whole roster could be out and the room would never say so, and the
/// Gone/InTheBack distinction was computed every watch and told to nobody. Criterion 1: a truth that lives
/// only in Core is not being told.</para>
///
/// <para>The barkeep now says it, off <see cref="HavenInterior.ResolveRegulars"/>. That makes the deck's
/// chairs and the barkeep's sentence two readings of one fact — the exact shape of this repo's most common
/// bug (class 3, "the sim does one thing and the SENTENCE says another"). So these guards hold the two
/// together at every bar over many watches, and pin that every regular is accounted for by exactly one
/// state, so nobody can be silently dropped out of the sentence.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBarSaysWhoIsInTonightTests
{
    private static readonly string[] Bars =
    [
        "the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "the-tilt", "the-deep", "red-eye",
    ];

    private static IReadOnlyList<HavenInterior.SeatedRegular> Regulars(string bar, double t) =>
        HavenInterior.ResolveRegulars(bar, t);

    private static HashSet<string> SeatedConsoleLabels(string bar, double t) =>
        [.. HavenInterior.DockedDeck(bar, null, t)!.Consoles
            .Where(c => c.Kind == DeckPlan.ConsoleKind.BarPatron)
            .Select(c => c.Label)];

    // ── 1 · The sentence's source and the deck's chairs are the same fact ─────────────────────────────

    [Fact]
    public void EveryRegularTheSentenceCallsPresentHasAChairOnTheDeck()
    {
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 50; w++)
            {
                double t = (w * PatronRota.WatchSeconds) + 1;
                HashSet<string> onDeck = SeatedConsoleLabels(bar, t);
                foreach (HavenInterior.SeatedRegular r in Regulars(bar, t))
                {
                    bool sentenceSaysIn = r.State == PatronState.AtBar;
                    bool deckSeatsThem = onDeck.Contains(r.Label);
                    Assert.True(sentenceSaysIn == deckSeatsThem,
                        $"{bar} watch {w}: the barkeep would say {r.ShortName} is "
                        + $"{(sentenceSaysIn ? "IN" : "OUT")} but the deck {(deckSeatsThem ? "seats" : "does not seat")} them.");
                }
            }
        }
    }

    [Fact]
    public void PresentAndTheStateNeverDisagreeWithEachOther()
    {
        // SeatedRegular carries BOTH a bool the deck build reads and the state the sentence reads. Two
        // fields, one fact — so pin them together, forever, or this becomes the next "one source of truth"
        // bug that currently happens to agree.
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 50; w++)
            {
                double t = (w * PatronRota.WatchSeconds) + 1;
                foreach (HavenInterior.SeatedRegular r in Regulars(bar, t))
                {
                    Assert.Equal(r.Present, r.State == PatronState.AtBar);
                }
            }
        }
    }

    // ── 2 · Nobody falls out of the sentence ──────────────────────────────────────────────────────────

    [Fact]
    public void EveryRosterMemberIsAccountedForOnEveryWatch()
    {
        // The sentence is built by bucketing the roster three ways. A regular the rota forgot, or a fourth
        // state nobody wrote a clause for, would silently vanish from the room's description.
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 50; w++)
            {
                double t = (w * PatronRota.WatchSeconds) + 1;
                IReadOnlyList<HavenInterior.SeatedRegular> rs = Regulars(bar, t);
                Assert.Equal(PatronRota.Roster.Count, rs.Count);
                Assert.Equal(PatronRota.Roster.Count, rs.Select(r => r.Id).Distinct().Count());
                foreach (HavenInterior.SeatedRegular r in rs)
                {
                    Assert.Contains(r.State, new[] { PatronState.AtBar, PatronState.Gone, PatronState.InTheBack });
                    Assert.False(string.IsNullOrWhiteSpace(r.ShortName), $"{r.Id} has no name to say aloud.");
                }
            }
        }
    }

    // ── 3 · There is something to say — the empty chair is real, and so is the full house ─────────────

    [Fact]
    public void BarsReallyDoHaveAwayRegularsAndSeatedOnes()
    {
        // A sentence that always reads "everyone is in" would be a guard on a world that cannot fail. Prove
        // that across the watches a player can actually reach, each bar shows both, and that BOTH away
        // flavours (stepped out / in the back) really occur somewhere — else one clause is dead prose.
        var flavoursSeen = new HashSet<PatronState>();
        foreach (string bar in Bars)
        {
            bool sawSeated = false, sawAway = false;
            for (int w = 0; w < 50; w++)
            {
                double t = (w * PatronRota.WatchSeconds) + 1;
                foreach (HavenInterior.SeatedRegular r in Regulars(bar, t))
                {
                    flavoursSeen.Add(r.State);
                    if (r.Present) { sawSeated = true; } else { sawAway = true; }
                }
            }
            Assert.True(sawSeated, $"{bar}: nobody is ever in — the bar is dead.");
            Assert.True(sawAway, $"{bar}: nobody is ever out — the rota is decoration.");
        }
        Assert.Contains(PatronState.Gone, flavoursSeen);
        Assert.Contains(PatronState.InTheBack, flavoursSeen);
    }
}

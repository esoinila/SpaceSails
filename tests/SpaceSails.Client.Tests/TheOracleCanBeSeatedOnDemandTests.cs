using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// THE ORACLE, ON A STOOL SOMEBODY CAN ACTUALLY WALK UP TO (issue #428).
///
/// <para>Solenne "Static" Marsh shipped in #427 with eleven green Core tests and was never live-verified,
/// for one reason: she is a corner fixture only <see cref="OracleRant.PresenceChance"/> of watches, so
/// booting into a bar and finding her was a coin flip. <c>?kaamos=N</c> and <c>?nebula=N</c> at least
/// GRANT their shards; nothing ever granted a rant. "A scene nobody can reach on demand is a scene that
/// ships broken" (<c>Map.Sim</c>'s own rule) — so <c>?oracle=1</c> seats her.</para>
///
/// <para>The trap this file exists to close is the fifth bug class: <b>a guard handed a world that cannot
/// tell pass from fail</b>. Forcing her on a watch she was already present for proves nothing, so every
/// test below first HUNTS a watch the rota genuinely leaves her stool empty, asserts such a watch exists,
/// and only then forces. And it walks the SHIPPING <see cref="DeckPlan"/> the renderer draws and the [E]
/// key dispatches through — a Core-only guard would stay green with the cheat wired to nothing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheOracleCanBeSeatedOnDemandTests
{
    // The bars ?oracle=1 is meant to work at — every station with a walkable deck the cheat can default to
    // or be pointed at with ?dock=.
    private static readonly string[] Bars =
    [
        "the-space-bar", "cinder-roost", "ringside-exchange", "selene-gate", "the-tilt", "the-deep", "red-eye",
    ];

    private const double Watch = OracleRant.WatchSeconds;

    // A sim-time inside a watch the rota genuinely leaves her stool EMPTY at this bar — the only world in
    // which forcing her proves anything. Null if she is somehow never away (which the test then fails on).
    private static double? AWatchSheIsAwayFor(string bar)
    {
        for (int w = 0; w < 400; w++)
        {
            double t = (w * Watch) + 1;
            if (!OracleRant.PresentAt(bar, t))
            {
                return t;
            }
        }
        return null;
    }

    private static DeckPlan.ConsoleSpot[] OracleConsoles(DeckPlan deck) =>
        [.. deck.Consoles.Where(c => c.Kind == DeckPlan.ConsoleKind.BarPatron && OracleRant.IsOracle(c.Label))];

    // ── 0 · The world this file hands the guards is honest ────────────────────────────────────────────

    [Fact]
    public void EveryBarHasWatchesTheOracleIsAwayFor()
    {
        // If she were present on every watch of every bar, every "forced" assertion below would pass on a
        // completely unwired cheat. Prove the empty stool is real before proving the seat is.
        foreach (string bar in Bars)
        {
            Assert.True(AWatchSheIsAwayFor(bar) is not null,
                $"{bar}: the oracle is never away — the forced-seat guards would be vacuous.");
        }
    }

    // ── 1 · The rota still says no when nobody forced it ──────────────────────────────────────────────

    [Fact]
    public void UnforcedThePresenceRotaIsUnchanged()
    {
        // The cheat must not quietly become the default: an unforced call is the same coin flip #427 shipped.
        foreach (string bar in Bars)
        {
            double away = AWatchSheIsAwayFor(bar)!.Value;
            Assert.False(OracleRant.PresentAt(bar, away));
            Assert.False(HavenInterior.OraclePresent(bar, away));
            Assert.Empty(OracleConsoles(HavenInterior.DockedDeck(bar, null, away)!));
        }
    }

    // ── 2 · Forced, she is on the deck — at every bar, on a watch she had drifted off ──────────────────

    [Fact]
    public void ForcedTheOracleIsSeatedAtEveryBarOnAWatchSheWouldHaveMissed()
    {
        foreach (string bar in Bars)
        {
            double away = AWatchSheIsAwayFor(bar)!.Value;

            Assert.True(OracleRant.PresentAt(bar, away, forced: true), $"{bar}: Core rota ignored the force.");
            Assert.True(HavenInterior.OraclePresent(bar, away, forced: true), $"{bar}: the client gate ignored the force.");

            DeckPlan deck = HavenInterior.DockedDeck(bar, null, away, forceOracle: true)!;
            DeckPlan.ConsoleSpot[] hers = OracleConsoles(deck);
            Assert.True(hers.Length == 1, $"{bar}: expected exactly one oracle console on the forced deck, got {hers.Length}.");
            Assert.Equal(OracleRant.ConsoleLabel, hers[0].Label);
        }
    }

    // ── 3 · The console the deck plants and the gate [E] reads never disagree ──────────────────────────

    [Fact]
    public void TheDeckAndTheInteractionGateAgreeOnEveryWatch()
    {
        // Bug class 3 in its purest form: a drawn console the interaction refuses, or a gate that says yes
        // where nothing is drawn. Walk 40 watches per bar, forced and unforced, and hold them together.
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 40; w++)
            {
                double t = (w * Watch) + 1;
                foreach (bool forced in new[] { false, true })
                {
                    bool gate = HavenInterior.OraclePresent(bar, t, forced);
                    bool drawn = OracleConsoles(HavenInterior.DockedDeck(bar, null, t, forced)!).Length == 1;
                    Assert.True(gate == drawn,
                        $"{bar} watch {w} forced={forced}: gate says {gate} but the deck draws {drawn}.");
                }
            }
        }
    }

    // ── 4 · The forced deck is not the unforced deck handed back out of the cache ──────────────────────

    [Fact]
    public void TheForcedDeckIsNotServedFromTheUnforcedCacheEntry()
    {
        // DockedDeck memoises per (station, wings, watch). If the force is not part of that key, a boot that
        // built the plain deck first would keep getting the plain deck — a cheat that works only if you
        // never docked before, which is exactly the kind of bug a browser smoke-test catches and a unit
        // test usually does not.
        const string bar = "the-space-bar";
        double away = AWatchSheIsAwayFor(bar)!.Value;

        Assert.Empty(OracleConsoles(HavenInterior.DockedDeck(bar, null, away)!));                    // plain first
        Assert.Single(OracleConsoles(HavenInterior.DockedDeck(bar, null, away, forceOracle: true)!)); // then forced
        Assert.Empty(OracleConsoles(HavenInterior.DockedDeck(bar, null, away)!));                    // and back
    }

    // ── 5 · Her corner is still hers — the seat cheat must not make [E] ambiguous ──────────────────────

    [Fact]
    public void TheForcedCornerStaysClearOfEveryOtherConsole()
    {
        // #427 placed her > DeckPlan.InteractRadius from every other console so E never grabs the wrong one.
        // Forcing her onto a watch the rota seated a FULL house is the case that could break that, so check
        // the busiest watch of every bar, not a quiet one.
        foreach (string bar in Bars)
        {
            for (int w = 0; w < 40; w++)
            {
                double t = (w * Watch) + 1;
                DeckPlan deck = HavenInterior.DockedDeck(bar, null, t, forceOracle: true)!;
                DeckPlan.ConsoleSpot hers = OracleConsoles(deck).Single();
                foreach (DeckPlan.ConsoleSpot other in deck.Consoles)
                {
                    if (other.Label == hers.Label)
                    {
                        continue; // herself
                    }
                    double dx = other.X - hers.X, dy = other.Y - hers.Y;
                    Assert.True((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius,
                        $"{bar} watch {w}: '{other.Label}' is within E's reach of the oracle's corner.");
                }
            }
        }
    }

    // ── 6 · Seating her does not GRANT anything — she still has to be listened to ──────────────────────

    [Fact]
    public void TheSeatCheatHandsYouThePersonNotTheTruth()
    {
        // The KAAMOS/Nebula seat idiom's whole point (#634/#641): ?kaamos=N grants a shard and proves
        // nothing about the scene that hands it over. ?oracle=1 must be the other kind — a person, not a
        // payout. Her opening line at a forced bar is the SAME line the rota would have given, and it is
        // overwhelmingly nonsense at zero drinks.
        const string bar = "the-space-bar";
        double away = AWatchSheIsAwayFor(bar)!.Value;
        long watch = OracleRant.WatchIndex(away);

        int trueLines = 0;
        for (int draw = 0; draw < 200; draw++)
        {
            if (OracleRant.Speak(bar, watch, draw, drinksBought: 0).IsTrue)
            {
                trueLines++;
            }
        }
        Assert.True(trueLines > 0, "a forced oracle can never leak a true line — the scene is a dead end.");
        Assert.True(trueLines < 200 * 0.5,
            $"a forced oracle leaked {trueLines}/200 true lines — the cheat is granting truth, not seating a person.");
    }
}

using System;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #572 · EVERY DEATH TELLS ITS OWN STORY.
///
/// <para>Owner, having suffocated alone on Miranda and been shown a heat-hunter's boarding volley over
/// exploding-ship art: <i>"there was no collector I was on site ... how can it say anything like that"</i>.</para>
///
/// <para>Because <see cref="DeathNarration"/> switches on <see cref="DeathCause"/> in four places and every
/// one of them ended <c>_ =&gt; &lt;the collector's version&gt;</c>. The cause reaching the card was correct;
/// there was simply no entry for it, so it inherited somebody else's death — art, caption, headline and
/// prose — and stated it with complete confidence.</para>
///
/// <para><b>This file is the guard that makes the defaults safe to keep.</b> It walks every value of the
/// enum, so the next cause anybody adds cannot quietly borrow a story. The previous test file pinned the
/// headline I had written; it did not pin the card the player reads, which is the whole lesson — and the
/// third time this project has paid for a sentence that disagreed with the sim (#545, #563).</para>
/// </summary>
public class EveryDeathTellsItsOwnStoryTests
{
    private static readonly DeathCause[] All = Enum.GetValues<DeathCause>();

    [Fact]
    public void EveryCause_HasItsOwnHeadline()
    {
        foreach (DeathCause cause in All)
        {
            string headline = DeathNarration.Headline(cause);
            Assert.False(string.IsNullOrWhiteSpace(headline));
            Assert.NotEqual("WHAT HAPPENED", headline);   // the bare default = no entry was written
            Assert.Contains("—", headline, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EveryCause_HasItsOwnArt()
    {
        // Two causes legitimately SHARE the BUSTED frames (Collector and Impact predate the death-* set).
        // Everything else must have art of its own: a suffocation illustrated with an exploding ship is the
        // picture disagreeing with the sim, which is exactly as bad as the words doing it.
        foreach (DeathCause cause in All.Where(c => c is not (DeathCause.Collector or DeathCause.Impact)))
        {
            string art = DeathNarration.ArtFile(cause);
            Assert.False(string.IsNullOrWhiteSpace(art));
            Assert.EndsWith(".jpg", art, StringComparison.Ordinal);
            Assert.NotEqual("busted-ship-explosion.jpg", art);
            Assert.NotEqual("busted-freeze-frame.jpg", art);
        }
    }

    [Fact]
    public void EveryCause_HasItsOwnLines_NotTheCollectorsBorrowed()
    {
        // The one that would have caught the shipped bug. Collector prose showing up under any other cause
        // means PoolFor fell through.
        string[] collector =
        [
            .. Enumerable.Range(0, 24).Select(i => DeathNarration.Line(DeathCause.Collector, (ulong)i, "Miranda")),
        ];

        foreach (DeathCause cause in All.Where(c => c != DeathCause.Collector))
        {
            for (ulong seed = 0; seed < 24; seed++)
            {
                string line = DeathNarration.Line(cause, seed, "Miranda");
                Assert.False(string.IsNullOrWhiteSpace(line));
                Assert.DoesNotContain(line, collector);
            }
        }
    }

    [Fact]
    public void NoDeathOnTheGROUND_EverMentionsCollectorsOrBoarding()
    {
        // The specific sentence the owner was shown. A captain alone on a moon is never collected, never
        // boarded, and never volleyed — there is nobody out there to do it.
        foreach (DeathCause cause in new[] { DeathCause.Reevers, DeathCause.Joined, DeathCause.Suffocated })
        {
            for (ulong seed = 0; seed < 24; seed++)
            {
                string line = DeathNarration.Line(cause, seed, "Miranda").ToUpperInvariant();
                foreach (string wrong in new[] { "COLLECTOR", "BOARDING", "VOLLEY", "DEBT", "FREEZE-FRAME" })
                {
                    Assert.DoesNotContain(wrong, line, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void TheCausesThatNeedTheirOwnCaption_HaveOne()
    {
        // The italic pull-quote above the prose. It defaulted too, which is how "…they simply kept coming,
        // and the guard did not hold forever" ended up over a lone suffocation.
        //
        // Note the generic line is CORRECT for a Reever death with the nerve intact — they did keep coming.
        // What must never carry it is a death nothing was coming for.
        const string generic = "kept coming";
        Assert.DoesNotContain(generic,
            DeathNarration.SurfaceCaption(DeathCause.Suffocated, nerveRanOut: false), StringComparison.Ordinal);
        Assert.DoesNotContain(generic,
            DeathNarration.SurfaceCaption(DeathCause.Suffocated, nerveRanOut: true), StringComparison.Ordinal);
        Assert.DoesNotContain(generic,
            DeathNarration.SurfaceCaption(DeathCause.Joined, nerveRanOut: false), StringComparison.Ordinal);

        // ...and the suffocation's own caption is about the gauge, because that is what the story is.
        Assert.Contains("gauge",
            DeathNarration.SurfaceCaption(DeathCause.Suffocated, nerveRanOut: false), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSuffocationCard_SaysAirAndNamesTheMoon()
    {
        // What the owner should have been shown.
        Assert.Contains("air", DeathNarration.Headline(DeathCause.Suffocated), StringComparison.OrdinalIgnoreCase);
        for (ulong seed = 0; seed < 12; seed++)
        {
            string line = DeathNarration.Line(DeathCause.Suffocated, seed, "Miranda");
            Assert.Contains("Miranda", line, StringComparison.Ordinal);
        }
    }

    // ── #574 · WHERE it happened. Owner: "landed death reasons and on ship death reasons ... are two
    //    separate categories. Maybe even 3 ... salvage ship, own ship, and landing party." ──

    [Fact]
    public void ADeathOnADERELICT_NeverMentionsRegolithOrTheTube()
    {
        // The live bug this found. TriggerSurfaceOverdrawDeath serves BOTH a regolith death and a death
        // aboard a salvage hull (the scuttle, the fifth blow, the nerve running out), and the ground prose
        // says things like "they ran you down on {body}'s REGOLITH short of the tube". Die on a steel hull
        // in space and the game described dust that is not there.
        foreach (DeathCause cause in new[] { DeathCause.Reevers, DeathCause.Joined, DeathCause.Suffocated })
        {
            for (ulong seed = 0; seed < 24; seed++)
            {
                string line = DeathNarration.Line(cause, DeathPlace.Derelict, seed, "the Quiet Sister")
                    .ToUpperInvariant();
                // Only words that ASSERT a place that is not there. "No dust to leave a mark in" is
                // correct writing about a steel deck — a banned-word list that cannot tell a claim from its
                // negation would forbid the best line in the pool.
                foreach (string wrong in new[] { "REGOLITH", "THE TUBE", "SHORT OF THE" })
                {
                    Assert.DoesNotContain(wrong, line, StringComparison.Ordinal);
                }
            }
        }
    }

    [Fact]
    public void ADerelictAndALandingParty_AreToldDifferently()
    {
        // Same cause, same seed, same name — different world, so different words. If these ever collapse to
        // one string the place has stopped reaching the narration.
        foreach (DeathCause cause in new[] { DeathCause.Reevers, DeathCause.Joined, DeathCause.Suffocated })
        {
            for (ulong seed = 0; seed < 12; seed++)
            {
                Assert.NotEqual(
                    DeathNarration.Line(cause, DeathPlace.Derelict, seed, "Miranda"),
                    DeathNarration.Line(cause, DeathPlace.LandingParty, seed, "Miranda"));
            }
        }
    }

    [Fact]
    public void OnlyACOLLECTORDeathTalksAboutTheFreezeFrame()
    {
        // "The freeze-frame holds" was hard-appended in the markup to EVERY death. It is BUSTED's own
        // language — about a collector's gun-camera still — and it was read out over a captain who
        // suffocated alone on a moon with nobody watching.
        foreach (DeathCause cause in All)
        {
            foreach (DeathPlace place in Enum.GetValues<DeathPlace>())
            {
                string tail = DeathNarration.Tail(cause, place);
                if (cause == DeathCause.Collector)
                {
                    continue;
                }
                Assert.DoesNotContain("freeze-frame", tail, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TheLandingPartyWearsTheRedShirt()
    {
        // Owner: "We could have the gen AI image on landing party show that I was wearing the red shirt :-D"
        // — "(can not resist the red shirt always die nod to Star Trek the OG series)". An away-team death
        // gets its own frame; the same cause aboard a hull does not.
        Assert.Equal("death-landing-party.jpg",
            DeathNarration.ArtFile(DeathCause.Reevers, DeathPlace.LandingParty));
        Assert.Equal("death-landing-party.jpg",
            DeathNarration.ArtFile(DeathCause.Suffocated, DeathPlace.LandingParty));
        Assert.NotEqual("death-landing-party.jpg",
            DeathNarration.ArtFile(DeathCause.Reevers, DeathPlace.Derelict));
        Assert.NotEqual("death-landing-party.jpg",
            DeathNarration.ArtFile(DeathCause.Collector, DeathPlace.OwnShip));
    }

    [Fact]
    public void ACollectorDeathCanOnlyHappenWhereACollectorCouldBE()
    {
        // Owner: "the debt collector deaths should also only happen in those situations never in any other".
        // A collector is a PERSON who came for you, with a ship and a boarding volley. There is nobody
        // aboard a dead hull and nobody on an empty moon, so a card claiming one would be inventing a
        // character out of nothing — which is how this whole thread started.
        Assert.True(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.OwnShip));
        Assert.False(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.Derelict));
        Assert.False(DeathNarration.CanHappen(DeathCause.Collector, DeathPlace.LandingParty));

        // And the mirror: an impact is the SHIP meeting a world at speed, which cannot happen to somebody
        // already standing on one.
        Assert.True(DeathNarration.CanHappen(DeathCause.Impact, DeathPlace.OwnShip));
        Assert.False(DeathNarration.CanHappen(DeathCause.Impact, DeathPlace.LandingParty));

        // ...and the ground's own deaths cannot happen on her deck, where there is air and no Old Ones.
        foreach (DeathCause ground in new[] { DeathCause.Reevers, DeathCause.Joined, DeathCause.Suffocated })
        {
            Assert.False(DeathNarration.CanHappen(ground, DeathPlace.OwnShip));
            Assert.True(DeathNarration.CanHappen(ground, DeathPlace.LandingParty));
            Assert.True(DeathNarration.CanHappen(ground, DeathPlace.Derelict));
        }
    }

    [Fact]
    public void TheSurfaceRoll_CanNeverProduceADeathThatNeedsSomebodyThere()
    {
        // Belt and braces on the roll itself, swept wide: whatever SurfaceEnd answers must be a death that
        // can actually occur away from her deck.
        for (ulong seed = 0; seed < 400; seed++)
        {
            foreach (double nerve in new[] { 0.0, 8.0, 50.0, 100.0 })
            {
                DeathCause rolled = DeathNarration.SurfaceEnd(nerve, seed);
                Assert.True(DeathNarration.CanHappen(rolled, DeathPlace.LandingParty));
                Assert.True(DeathNarration.CanHappen(rolled, DeathPlace.Derelict));
            }
        }
    }
}

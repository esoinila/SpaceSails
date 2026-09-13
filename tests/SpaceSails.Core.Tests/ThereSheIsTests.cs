using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #238 · <b>THERE SHE IS, AND GOT IT — the event family's second and third members.</b>
///
/// <para>Owner, mid-car-hunt: <i>"Is there a big pop-up when we find the car in the scans? It is a kind of
/// mission event like when we get paid?"</i> — and again at the other end of the same job, proven live:
/// he prised the roadster's wallet mid-coast and had to ASK whether the loot was aboard.</para>
///
/// <para>What Core owns, and therefore what this file measures: the EVENT SHAPE. That the beat composes out
/// of a name and a bearing rather than out of a typed sentence, that the two headlines are the owner's own
/// words when given his own arguments, that the bird only speaks where an arc owns a line for it, and that
/// a reveal knows where to look while a pickup knows there is nowhere to. Whether the client raises it once
/// per target, pulls the warp and files the receipt is next door in the client suite, because none of that
/// is a pure value.</para>
/// </summary>
public sealed class ThereSheIsTests
{
    // ── THE OWNER'S OWN LINES ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE HEADLINE IS HIS, VERBATIM — and it is COMPOSED, not typed. The template is handed the roadster's
    /// name and the roadster's bearing phrase and produces the sentence he wrote in the issue; nothing in
    /// the builder knows there is a roadster.
    ///
    /// <para><b>Proven RED</b> by typing the roadster's headline into <c>RevealHeadline</c> as a literal
    /// under an <c>if</c> on the name: the composition assertions below still pass, and this one fails on
    /// the SECOND target — which is the whole point of the parameterisation and the thing a verbatim-only
    /// assertion cannot see.</para>
    /// </summary>
    [Fact]
    public void TheRevealHeadlineIsTheOwnersOwnSentenceAndItComesOutOfTheTemplate()
    {
        Assert.Equal("🔭 THERE SHE IS — the roadster, sunward of Mars",
            MissionMoments.RevealHeadline(Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase));

        // …and the SAME template, with somebody else's two facts in it, is somebody else's headline.
        Assert.Equal("🔭 THERE SHE IS — Cinder Roost, SATURN system",
            MissionMoments.RevealHeadline("Cinder Roost", "SATURN system"));
    }

    /// <summary>
    /// THE PICKUP HEADLINE, the same way. The wallet's is his sentence word for word; anything else names
    /// itself through the identical template.
    ///
    /// <para><b>Proven RED</b> by changing the em dash to a hyphen — every one of these four fails, which is
    /// what says the assertion is reading the composed string and not a substring of it.</para>
    /// </summary>
    [Fact]
    public void ThePickupHeadlineIsTheOwnersOwnSentenceAndItComesOutOfTheTemplate()
    {
        Assert.Equal("💾 GOT IT — the wallet, from between the seats",
            MissionMoments.PickupHeadline(Derelict.WalletBetweenTheSeats));

        Assert.Equal("💾 GOT IT — a crate of nobody's business",
            MissionMoments.PickupHeadline("a crate of nobody's business"));
    }

    /// <summary>
    /// THE TWIN'S OWN ITEM, BUILT AND NOT TYPED. One car in four has photographs in it instead of a wallet,
    /// and the prise is the same hand in the same gap in the same seat — so the beat closes on the wallet's
    /// own four words, taken FROM the wallet's phrase, and names the object by its canon name.
    ///
    /// <para><b>Proven RED</b> twice: rename <c>CompromisingChip.Name</c> and the first assertion fails
    /// (the item stopped being the object's name); re-type the tail as a literal and then change
    /// <c>WalletBetweenTheSeats</c>, and the second fails (the two accounts of one seat have parted).</para>
    /// </summary>
    [Fact]
    public void TheChipsPickupItemNamesTheObjectAndBorrowsTheWalletsOwnWordsForWhereItWas()
    {
        Assert.Equal("the data chip, from between the seats", CompromisingChip.PickupItem);

        Assert.EndsWith(
            Derelict.WalletBetweenTheSeats[Derelict.WalletBetweenTheSeats.IndexOf(',', StringComparison.Ordinal)..],
            CompromisingChip.PickupItem, StringComparison.Ordinal);
        Assert.Contains(CompromisingChip.Name[2..], CompromisingChip.PickupItem, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE BIRD SEES HER FIRST, and says the owner's line. Owner, endorsing his own idea: <i>"Love that
    /// thought of Parrot reacting to the glint from telescope."</i>
    ///
    /// <para>ONE line, like its two siblings and for their reason — a three-beat gag whose set-up rotated
    /// would be a different joke every hunt.</para>
    ///
    /// <para><b>Proven RED</b> by dropping a second line into the CarGlimpsed row: the rotation loop catches
    /// it at counter 1.</para>
    /// </summary>
    [Fact]
    public void TheParrotsGlintLineIsTheOwnersAndItDoesNotRotate()
    {
        Assert.Equal("DUDE. THERE. Is. The CAR!", Parrot.Line(Parrot.Squawk.CarGlimpsed, 0));

        for (int counter = 0; counter < 8; counter++)
        {
            Assert.Equal("DUDE. THERE. Is. The CAR!", Parrot.Line(Parrot.Squawk.CarGlimpsed, counter));
        }
    }

    // ── THE EVENT SHAPE ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A REVEAL KNOWS WHERE TO LOOK; A PICKUP KNOWS THERE IS NOWHERE TO. That single nullable is what the
    /// card's second way out is drawn on, so it is the difference between the owner's <c>show me</c> press
    /// and a button that moves nothing.
    ///
    /// <para><b>Proven RED</b> by having <c>Pickup</c> pass the source body through as its
    /// <c>ShowMeBodyId</c>: the second assertion fails, and the card would then offer to fly the camera to a
    /// wreck the ship is parked against.</para>
    /// </summary>
    [Fact]
    public void OnlyTheRevealCarriesSomewhereToLook()
    {
        MissionMoment found = MissionMoments.Reveal(
            Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase, Derelict.RoadsterBodyId);
        MissionMoment got = MissionMoments.Pickup(Derelict.WalletBetweenTheSeats);

        Assert.Equal(Derelict.RoadsterBodyId, found.ShowMeBodyId);
        Assert.Null(got.ShowMeBodyId);

        Assert.Equal(MissionMomentKind.Reveal, found.Kind);
        Assert.Equal(MissionMomentKind.Pickup, got.Kind);
    }

    /// <summary>
    /// THE BIRD IS OPTIONAL, AND THE WORDS RIDE WITH THE KIND. A reveal of something that is not the car
    /// carries no squawk and no line — the payoff belongs to the car gag and nothing else — and a beat that
    /// DOES carry one carries the parrot channel's own sentence rather than a second copy of it.
    ///
    /// <para><b>Proven RED</b> by defaulting <c>parrot</c> to <c>Parrot.Squawk.CarGlimpsed</c> instead of
    /// null: the silent case starts squawking "DUDE. THERE. Is. The CAR!" at a secret station.</para>
    /// </summary>
    [Fact]
    public void TheBirdSpeaksOnlyWhereAnArcOwnsALineAndAlwaysInTheParrotsOwnWords()
    {
        MissionMoment quiet = MissionMoments.Reveal("Cinder Roost", "SATURN system", "cinder-roost");
        Assert.Null(quiet.ParrotSquawk);
        Assert.Null(quiet.ParrotLine);

        MissionMoment loud = MissionMoments.Reveal(
            Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase, Derelict.RoadsterBodyId,
            Parrot.Squawk.CarGlimpsed, parrotCounter: 3);
        Assert.Equal(Parrot.Squawk.CarGlimpsed, loud.ParrotSquawk);
        Assert.Equal(Parrot.Line(Parrot.Squawk.CarGlimpsed, 3), loud.ParrotLine);

        MissionMoment prised = MissionMoments.Pickup(
            Derelict.WalletBetweenTheSeats, Parrot.Squawk.CarFound, parrotCounter: 2);
        Assert.Equal(Parrot.Line(Parrot.Squawk.CarFound, 2), prised.ParrotLine);
    }

    /// <summary>
    /// PURE, AND THEREFORE THE SAME BEAT EVERY TIME. Core's own law: no clock, no <c>Random</c>. Composed
    /// twice with the same arguments, a moment is equal to itself — which is what lets a save-and-reload,
    /// or a second machine, tell the captain the same thing about the same find.
    /// </summary>
    [Fact]
    public void TheSameFactsComposeTheSameBeat()
    {
        MissionMoment first = MissionMoments.Reveal(
            Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase, Derelict.RoadsterBodyId,
            Parrot.Squawk.CarGlimpsed);
        MissionMoment second = MissionMoments.Reveal(
            Derelict.RoadsterRevealName, Derelict.RoadsterBearingPhrase, Derelict.RoadsterBodyId,
            Parrot.Squawk.CarGlimpsed);

        Assert.Equal(first, second);
    }

    // ── ONE SOURCE OF TRUTH, AND THE CANON SWEEP ──────────────────────────────────────────────────────

    /// <summary>
    /// WHERE SHE IS IS SPELLED ONCE. #238 wanted a fifth copy of "sunward of Mars" for its headline; §5 says
    /// a fact lives in one place, so the four sentences that already said it read the constant now. This is
    /// the assertion that the phrase did not quietly change shape while being hoisted — the transponder fix,
    /// the ledger's job line, the Fixer's blurb and the test start all still say what they said.
    ///
    /// <para><b>Proven RED</b> by changing the constant to "sunward of Luna": the client's four sentences
    /// change with it and this fails, which is exactly the coupling the hoist was for.</para>
    /// </summary>
    [Fact]
    public void TheRoadstersBearingPhraseIsTheOwnersAndItIsSpelledOnce()
    {
        Assert.Equal("sunward of Mars", Derelict.RoadsterBearingPhrase);
        Assert.Equal("the roadster", Derelict.RoadsterRevealName);
        Assert.Equal("the wallet, from between the seats", Derelict.WalletBetweenTheSeats);
    }

    /// <summary>
    /// THE CANON SWEEP. Every sentence this feature can put on a screen, walked: none is blank, none carries
    /// §8's reserved word, and the sweep is wide enough to still be reading the file (a template that stops
    /// being listed stops being swept, silently, which is the failure mode a count catches).
    /// </summary>
    [Fact]
    public void EveryLineTheBeatCanSayIsRealProseAndKeepsOffTheReservedWord()
    {
        List<string> prose = MissionMoments.AllProse().ToList();

        Assert.Equal(6, prose.Count);
        foreach (string line in prose)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.DoesNotContain("monolith", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cyclopean", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// THE LABEL ON THE PRESS IS THE OWNER'S, lower-case and all: <c>show me</c>. It is quoted in the issue
    /// as the thing the card offers, so it is canon and not chrome.
    /// </summary>
    [Fact]
    public void TheShowMePressSaysWhatTheOwnerSaidItSays()
    {
        Assert.Equal("show me", MissionMoments.ShowMeFace);
    }
}

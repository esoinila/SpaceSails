using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #760 · STANDING TRAVELS. Owner, 2026-08-08: <i>"same-company labs on different sites may accept the same
/// cards for access … the ship remote can use it over the air."</i>
///
/// <para>Five laws, and every one of them was watched going RED before it was allowed to go green. The REDs
/// are written on each guard, verbatim, because a guard nobody has seen fail is a guard that proves the
/// shape of the symptom and not the law (#587's lesson, paid for twice).</para>
/// </summary>
public sealed class TheStandingTravelsTests
{
    // ── THE SWEEP ───────────────────────────────────────────────────────────────────────────────────────
    //
    // Generated site ids rather than the moons, because what is being audited is a rule about OUTFITS and a
    // handful of named rocks cannot be relied on to supply two of one and one of another. Every site here is
    // a real site: the same generator, the same seeds, the same depth arithmetic the game runs on.

    private const int SweepSize = 120;

    private static IEnumerable<string> SweepSites() =>
        Enumerable.Range(0, SweepSize).Select(i => $"sweep-site-{i}");

    /// <summary>Sites with an ordinary gate off B1 — the commonest door in the game, and the one every
    /// assertion below is made at.</summary>
    private static IReadOnlyList<string> GatedSites() =>
        [.. SweepSites().Where(b => UndergroundComplex.NextShaftBelow(b, -1) == 1)];

    /// <summary>Two sites of ONE outfit, and a third of another. The whole fixture this file needs, found by
    /// walking the world rather than by typing three ids in and hoping.</summary>
    private static (string A, string B, string Stranger) TwoOfOneAndOneOfAnother()
    {
        IReadOnlyList<string> gated = GatedSites();
        Assert.True(gated.Count >= 3, "the sweep found fewer than three sites with a gate off B1.");

        foreach (IGrouping<string, string> outfit in gated.GroupBy(b => SiteOperator.Of(b).Id))
        {
            if (outfit.Count() < 2)
            {
                continue;
            }
            string? stranger = gated.FirstOrDefault(
                b => !string.Equals(SiteOperator.Of(b).Id, outfit.Key, StringComparison.Ordinal));
            if (stranger is not null)
            {
                return (outfit.ElementAt(0), outfit.ElementAt(1), stranger);
            }
        }

        throw new InvalidOperationException(
            "the sweep never found two sites of one outfit and one of another — every assertion in this " +
            "file would be about a world with one company in it.");
    }

    private static UndergroundComplex.AuthorityCard Card(string body, int band) => new(body, band);

    private static Satchel.Item Held(UndergroundComplex.AuthorityCard card) =>
        new(Satchel.Kind.Authority, card.Id);

    // ── (a) THE ONE PREDICATE ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #760 · A card is standing with an OUTFIT: honoured at every site of theirs, refused at everybody
    /// else's. The whole issue, in one assertion, over a real sweep of real sites.
    ///
    /// <para><b>Proven RED</b> by putting the body-id equality back into <c>Honours</c> (<c>held.BodyId ==
    /// gate.BodyId</c>, which is what the gate did before this issue):</para>
    /// <code>
    /// Assert.True() Failure
    /// sweep-site-1's card is refused at sweep-site-2, and both of those are HOLBEIN &amp; SONS (MINERALS).
    /// </code>
    /// </summary>
    [Fact]
    public void OneOutfitsCardIsHonouredAtEverySiteOfTheirs_AndNobodyElses()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        UndergroundComplex.AuthorityCard fromA = Card(a, 1);

        Assert.True(UndergroundComplex.Honours(fromA, Card(a, 1)),
            $"{a}'s own card is refused at its own gate.");
        Assert.True(UndergroundComplex.Honours(fromA, Card(b, 1)),
            $"{a}'s card is refused at {b}, and both of those are {SiteOperator.Of(a).Name}.");
        Assert.False(UndergroundComplex.Honours(fromA, Card(stranger, 1)),
            $"{a}'s card opened {stranger}, which answers to {SiteOperator.Of(stranger).Name} instead.");

        // The fixture is not degenerate: the two that agree really are one outfit and the third really is
        // another, or every line above could be true of a world with one company in it.
        Assert.Equal(SiteOperator.Of(a).Id, SiteOperator.Of(b).Id);
        Assert.NotEqual(SiteOperator.Of(a).Id, SiteOperator.Of(stranger).Id);

        // And the whole sweep obeys it, not just the three sites the fixture picked.
        foreach (string site in GatedSites())
        {
            bool same = string.Equals(SiteOperator.Of(site).Id, SiteOperator.Of(a).Id, StringComparison.Ordinal);
            Assert.Equal(same, UndergroundComplex.Honours(fromA, Card(site, 1)));
        }
    }

    /// <summary>
    /// #760 · …and the band still has to match, everywhere. Standing travels; a shaft number does not, and
    /// #679's sharpest refusal is the thing that would be lost by making an outfit's paper a skeleton key.
    ///
    /// <para><b>Proven RED</b> by dropping the band clause from <c>Honours</c>:</para>
    /// <code>
    /// Assert.False() Failure
    /// a card for shaft 1 opened shaft 2 of the same outfit — standing is not a skeleton key.
    /// </code>
    /// </summary>
    [Fact]
    public void StandingTravelsBetweenSites_NeverBetweenShafts()
    {
        (string a, string b, _) = TwoOfOneAndOneOfAnother();

        Assert.False(UndergroundComplex.Honours(Card(a, 1), Card(b, 2)),
            "a card for shaft 1 opened shaft 2 of the same outfit — standing is not a skeleton key.");
        Assert.False(UndergroundComplex.Honours(Card(a, 1), Card(a, 0)),
            "a card for shaft 1 opened shaft 0 of its own site.");
    }

    /// <summary>
    /// #760 · A VENDOR'S REACH: honoured at the shafts, refused at the head office. Fable's default for v1,
    /// and it lives in one line (<c>AcceptsVendors</c>).
    ///
    /// <para><b>Proven RED</b> by making <c>AcceptsVendors</c> true for every kind:</para>
    /// <code>
    /// Assert.False() Failure
    /// a contractor's paper opened the head office.
    /// </code>
    /// <para>…and RED the other way, by making it false for every kind:</para>
    /// <code>
    /// Assert.True() Failure
    /// a contractor's paper was refused at a shaft, which contractors dug.
    /// </code>
    /// </summary>
    [Fact]
    public void AVendorsPaperOpensAShaftAndNotTheHeadOffice()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        var vendor = new UndergroundComplex.AuthorityCard(
            a, 1, new UndergroundComplex.Standing(SiteOperator.Of(a).Id, UndergroundComplex.Reach.Vendor));

        Assert.True(UndergroundComplex.Honours(vendor, Card(a, 1), UndergroundComplex.GateKind.Shaft),
            "a contractor's paper was refused at a shaft, which contractors dug.");
        Assert.True(UndergroundComplex.Honours(vendor, Card(b, 1), UndergroundComplex.GateKind.Shaft),
            "a contractor's standing stopped at the site it was signed for.");
        Assert.False(UndergroundComplex.Honours(vendor, Card(a, 1), UndergroundComplex.GateKind.HeadOffice),
            "a contractor's paper opened the head office.");

        // The prime's own paper is the control: the same card, the same gates, and the head office is the
        // only door that tells the two apart. Without this the guard above would also pass on a build where
        // the head office refused everybody.
        var prime = new UndergroundComplex.AuthorityCard(
            a, 1, new UndergroundComplex.Standing(SiteOperator.Of(a).Id, UndergroundComplex.Reach.Prime));
        Assert.True(UndergroundComplex.Honours(prime, Card(a, 1), UndergroundComplex.GateKind.HeadOffice));

        // And a vendor's standing is still standing with SOMEBODY: a stranger's shaft is a stranger's shaft.
        Assert.False(UndergroundComplex.Honours(vendor, Card(stranger, 1)));
    }

    /// <summary>#760 · The lift panel's automatic read is the SAME predicate — not a string comparison that
    /// happens to agree with it. The panel is the one surface a captain meets this rule on, so a build where
    /// <c>Honours</c> was right and the panel was not would ship the bug with the fix in the box.
    ///
    /// <para><b>Proven RED</b> by restoring <c>heldCardIds.Contains(gateCard.Id)</c> on its own:</para>
    /// <code>
    /// Assert.Null() Failure: Value is not null
    /// Expected: null
    /// Actual:   "This car does not go lower. The shaft that does is"···
    /// </code></summary>
    [Fact]
    public void ThePanelReadsTheSameStandingTheGateDoes()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        UndergroundComplex.LiftStop Gate(string site, params string[] cards) => Assert.Single(
            UndergroundComplex.LiftPanel(site, -1, cards),
            s => UndergroundComplex.BandOf(s.Level) == 1 && s.Level < 0);

        Assert.Null(Gate(b, Card(a, 1).Id).Refusal);
        Assert.NotNull(Gate(b, Card(stranger, 1).Id).Refusal);
        Assert.NotNull(Gate(b).Refusal);

        // …and the row NAMES the card the gate actually read, which on a standing is a card issued somewhere
        // else. A row printing this site's designation over another site's card is the sim doing one thing
        // and the sentence reporting another.
        Assert.Equal(UndergroundComplex.CardTitle(Card(a, 1)), Gate(b, Card(a, 1).Id).OpenedBy);
    }

    /// <summary>#760 · …and the satchel's TRY answers what the panel answers, in the sentence the matrix
    /// owns. One predicate, four callers (#684's law about two answers to one question).</summary>
    [Fact]
    public void TheSatchelsTryHonoursTheSameStanding()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        SatchelTry.Outcome travelled = SatchelTry.Offer(
            Held(Card(a, 1)), SatchelTry.Target.ShaftGate, Card(b, 1).Id);
        Assert.True(travelled.Worked, "the gate refused an outfit's own paper from another of their sites.");
        Assert.Equal(UndergroundComplex.StandingHonouredLine(Card(a, 1)), travelled.Line);

        // Its own gate keeps its own sentence, untouched: the card that belongs here is not a card that
        // travelled, and telling the player it was would be a sentence about a world that is not running.
        Assert.Equal("🎫 The gate reads it without hesitating.",
            SatchelTry.Offer(Held(Card(b, 1)), SatchelTry.Target.ShaftGate, Card(b, 1).Id).Line);

        // A stranger's card keeps #679's refusal, to the syllable.
        SatchelTry.Outcome refused = SatchelTry.Offer(
            Held(Card(stranger, 1)), SatchelTry.Target.ShaftGate, Card(b, 1).Id);
        Assert.False(refused.Worked);
        Assert.Contains("somebody else's business", refused.Line, StringComparison.Ordinal);

        // And the rung this issue added: OUR paper, WRONG hole — which is neither of the two above.
        SatchelTry.Outcome ourWrongHole = SatchelTry.Offer(
            Held(Card(a, 0)), SatchelTry.Target.ShaftGate, Card(b, 1).Id);
        Assert.False(ourWrongHole.Worked);
        Assert.NotEqual(refused.Line, ourWrongHole.Line);
        Assert.Contains("Standing is not the problem", ourWrongHole.Line, StringComparison.Ordinal);
    }

    // ── (b) THE OLD IDS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #760 · EVERY CARD IN EVERY SAVE STILL PARSES, AND STILL BEHAVES. The old id is <c>body#band</c> and it
    /// reads back as the card it has always been: the site's own operator, prime reach, the same id out as
    /// went in. The world still mints exactly that form, so nothing already in a pocket has two spellings.
    ///
    /// <para><b>Proven RED</b> by making <c>TryParse</c> require the standing suffix (returning false where
    /// no <c>@</c> is present):</para>
    /// <code>
    /// Assert.True() Failure
    /// 'sweep-site-1#1' — a card written by every build before this one — no longer parses.
    /// </code>
    /// </summary>
    [Fact]
    public void TheOldCardIdsStillParseAndStillBehave()
    {
        foreach (string site in GatedSites().Take(20))
        {
            const string form = "{0}#{1}";
            string old = string.Format(System.Globalization.CultureInfo.InvariantCulture, form, site, 1);

            Assert.True(UndergroundComplex.AuthorityCard.TryParse(old, out UndergroundComplex.AuthorityCard card),
                $"'{old}' — a card written by every build before this one — no longer parses.");
            Assert.Equal(site, card.BodyId);
            Assert.Equal(1, card.Band);
            Assert.Null(card.Standing);
            Assert.Equal(SiteOperator.Of(site).Id, card.OperatorId);
            Assert.Equal(UndergroundComplex.Reach.Prime, card.ReachOfIt);
            Assert.Equal(old, card.Id);

            // What the world mints is still the short form — so an old save and a new find are the same
            // string for the same card, and nothing in a pocket can drift into a second spelling.
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (UndergroundComplex.CardInRoom(site, level) is { } minted)
                {
                    Assert.DoesNotContain('@', minted.Id);
                    Assert.Equal($"{minted.BodyId}#{minted.Band}", minted.Id);
                }
            }

            // And it opens what it always opened.
            Assert.True(SatchelTry.Offer(Held(card), SatchelTry.Target.ShaftGate, Card(site, 1).Id).Worked);
        }
    }

    /// <summary>#760 · The new form round-trips exactly, and nothing else parses. An older build meeting one
    /// of these drops it (it wants an integer where the operator key starts) rather than misreading it, which
    /// is the tolerance the vault has everywhere else.</summary>
    [Fact]
    public void AStandingWrittenOnACardComesBackOffIt()
    {
        foreach (UndergroundComplex.Reach reach in Enum.GetValues<UndergroundComplex.Reach>())
        {
            var written = new UndergroundComplex.AuthorityCard(
                "sweep-site-0", 2, new UndergroundComplex.Standing("northfield", reach));

            Assert.True(UndergroundComplex.AuthorityCard.TryParse(written.Id, out UndergroundComplex.AuthorityCard back));
            Assert.Equal(written, back);
            Assert.Equal("northfield", back.OperatorId);
            Assert.Equal(reach, back.ReachOfIt);
        }

        foreach (string junk in new[] { "sweep-site-0#2@", "sweep-site-0#2@northfield", "sweep-site-0#2@/P",
                     "sweep-site-0#2@northfield/X", "sweep-site-0#2@northfield/PP", "@northfield/P" })
        {
            Assert.False(UndergroundComplex.AuthorityCard.TryParse(junk, out _), $"'{junk}' parsed.");
        }
    }

    // ── (c) THE REMOTE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #760 · A SEND OPENS EXACTLY WHAT THE CARD WOULD OPEN, AND NOT ONE THING MORE. The gate it names is the
    /// gate the panel reads, and a panel shown the sent id on top of the wallet that sent it is the panel it
    /// already was — byte for byte, every row.
    ///
    /// <para><b>Proven RED</b> by having <c>Send</c> hand back the band below as well (<c>gate.Band + 1</c>
    /// added to the opened set):</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    /// Expected: "sweep-site-2#1"
    /// Actual:   "sweep-site-2#2"
    /// </code>
    /// </summary>
    [Fact]
    public void ASendOpensTheONEGateTheWalletWouldHaveOpened()
    {
        (string a, string b, _) = TwoOfOneAndOneOfAnother();

        List<Satchel.Item> wallet = [Held(Card(a, 1))];
        RemoteSend.Sent sent = RemoteSend.Send(b, -1, wallet);

        Assert.True(sent.Worked, sent.Line);
        Assert.Equal(Card(b, 1).Id, sent.OpenedGateId);

        string[] held = [.. wallet.Select(i => i.Id)];

        // THE DOOR STATE, and not the label on it. What a send may change is which ways through this building
        // are open; WHICH paper a row names is a different fact and it is asserted where it belongs
        // (ThePanelReadsTheSameStandingTheGateDoes — the row names the card the gate actually read, which on
        // a standing is a card issued somewhere else).
        static IReadOnlyList<(int, bool, bool, string?, bool)> Doors(
            IReadOnlyList<UndergroundComplex.LiftStop> panel) =>
            [.. panel.Select(s => (s.Level, s.Pressurised, s.IsCurrent, s.Refusal, s.OpenedByChit))];

        // (1) The send opens what the wallet already opens in person: standing IS the standing, wherever the
        // captain happens to be holding the handset.
        IReadOnlyList<UndergroundComplex.LiftStop> walked = UndergroundComplex.LiftPanel(b, -1, held);
        Assert.Equal(
            Doors(walked),
            Doors(UndergroundComplex.LiftPanel(b, -1, [sent.OpenedGateId!])));

        // (2) …and adding it on top of the wallet that sent it changes nothing at all — which is what "and
        // not one thing more" means when the thing being asked for is a door you could already walk to.
        Assert.Equal(Doors(walked), Doors(UndergroundComplex.LiftPanel(b, -1, [.. held, sent.OpenedGateId!])));

        // The fixture is not vacuous: this panel really does have a gate on it, it really is open, and there
        // really is a band below for an over-eager send to have opened one too many of.
        UndergroundComplex.LiftStop gate = Assert.Single(
            walked, s => UndergroundComplex.BandOf(s.Level) == 1 && s.Level < 0);
        Assert.Null(gate.Refusal);
        Assert.DoesNotContain(walked, s => UndergroundComplex.BandOf(s.Level) > 1 && s.Level < 0);
    }

    /// <summary>
    /// #760 · WHERE THERE IS NO NET THERE IS NO SEND — and nothing is owed for having tried. The head
    /// office's outfit publishes none (#411's rank difference in radio), and neither do the halls nobody dug
    /// (#677).
    ///
    /// <para><b>Proven RED</b> by giving <c>TheParentUndertaking</c> <c>PublishesNetwork: true</c>:</para>
    /// <code>
    /// Assert.True() Failure
    /// the head office's outfit publishes a network — the watchers emit nothing and so does whoever files them.
    /// </code>
    /// </summary>
    [Fact]
    public void TheHeadOfficeAndTheHallsAnswerNothingAndChargeNothing()
    {
        string hq = KaamosLore.IceMoonBodyId;
        Assert.False(SiteOperator.Of(hq).PublishesNetwork,
            "the head office's outfit publishes a network — the watchers emit nothing and so does whoever "
            + "files them.");

        // A full wallet, so the silence cannot be mistaken for an empty pocket.
        List<Satchel.Item> wallet = [Held(Card(hq, 1)), Held(Card("sweep-site-0", 1))];
        RemoteSend.Sent atHq = RemoteSend.Send(hq, -1, wallet);
        Assert.False(atHq.Worked);
        Assert.Equal(RemoteSend.NoNetworkLine, atHq.Line);
        Assert.Null(atHq.OpenedGateId);
        Assert.Equal(0, atHq.Charge.Points);

        // …and the band nobody dug, which is a different silence with the same answer.
        string halls = UndergroundComplex.FoundBandCheatSiteId;
        Assert.True(UndergroundComplex.HasFoundBand(halls), "the cheat site has no halls — see #677.");
        int found = UndergroundComplex.FoundBandOf(halls);
        RemoteSend.Sent inTheHalls =
            RemoteSend.Send(halls, UndergroundComplex.BandTop(found), [Held(Card(halls, found))]);
        Assert.False(inTheHalls.Worked);
        Assert.Equal(RemoteSend.NoNetworkLine, inTheHalls.Line);
        Assert.Equal(0, inTheHalls.Charge.Points);

        // …and a send from the floor ABOVE them cannot reach into them either: the gate a send would open
        // there is the way into the halls, and nothing wired that.
        RemoteSend.Sent intoTheHalls = RemoteSend.Send(
            halls, UndergroundComplex.BandTop(found - 1), [Held(Card(halls, found))]);
        Assert.False(intoTheHalls.Worked);
        Assert.Equal(RemoteSend.NoNetworkLine, intoTheHalls.Line);
        Assert.Equal(0, intoTheHalls.Charge.Points);
    }

    /// <summary>
    /// #760/#715 · A REFUSED SEND COSTS WHAT A REFUSED CARD COSTS, and it is owed to the OUTFIT — never to
    /// the moon, and never to the world (the named Vegas anti-pattern). An accepted one costs nothing, and so
    /// does a handset with nothing in it to send.
    ///
    /// <para><b>Proven RED</b> by keying the charge to the body id (<c>new HeatCharge(bodyId, …)</c>):</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    /// Expected: "holbein"
    /// Actual:   "sweep-site-2"
    /// </code>
    /// <para>…and RED the other way by charging on success (<c>RefusedAtTheGate</c> on the accepted
    /// branch):</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 0
    /// Actual:   1
    /// </code>
    /// </summary>
    [Fact]
    public void ARefusedSendChargesWhatARefusedCardCharges_ToTheOutfit()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        // Addressed them and was told no: a stranger's paper, sent at this outfit's gate.
        RemoteSend.Sent refused = RemoteSend.Send(b, -1, [Held(Card(stranger, 1))]);
        Assert.False(refused.Worked);
        Assert.Null(refused.OpenedGateId);
        Assert.Equal(UndergroundComplex.RefusedAtTheGate(b), refused.Charge);
        Assert.Equal(SiteOperator.Of(b).Id, refused.Charge.OperatorId);
        Assert.NotEqual(b, refused.Charge.OperatorId);
        Assert.True(refused.Charge.Points > 0, "a refused transmission cost nothing at all.");

        // The reason is the matrix's own, verbatim — one source for what a no says (#684).
        Assert.Equal(
            RemoteSend.RefusedPreamble
                + SatchelTry.OfferWallet(
                    [Held(Card(stranger, 1))], SatchelTry.Target.ShaftGate, Card(b, 1).Id).Line,
            refused.Line);

        // Accepted: nothing owed to anybody.
        RemoteSend.Sent accepted = RemoteSend.Send(b, -1, [Held(Card(a, 1))]);
        Assert.True(accepted.Worked);
        Assert.Equal(0, accepted.Charge.Points);

        // Nothing to send is not a refusal: you cannot cross an outfit you never addressed.
        RemoteSend.Sent nothing = RemoteSend.Send(b, -1, [new Satchel.Item(Satchel.Kind.Paper, "x")]);
        Assert.False(nothing.Worked);
        Assert.Equal(RemoteSend.NothingToSendLine, nothing.Line);
        Assert.Equal(0, nothing.Charge.Points);

        // And CanSend agrees with Send about all three, which is the whole reason it is published.
        Assert.True(RemoteSend.CanSend(b, -1, [Held(Card(a, 1))]));
        Assert.False(RemoteSend.CanSend(b, -1, [Held(Card(stranger, 1))]));
        Assert.False(RemoteSend.CanSend(KaamosLore.IceMoonBodyId, -1, [Held(Card(a, 1))]));
    }

    /// <summary>#760 · Every send says something, in all four cases. A control that answers with silence is
    /// indistinguishable from a bug (#603's founding law), and the remote's three switches all order
    /// something that is somewhere else — the sentence IS the feedback (#736).</summary>
    [Fact]
    public void EverySendSaysSomething()
    {
        (string a, string b, _) = TwoOfOneAndOneOfAnother();
        List<Satchel.Item>[] wallets =
        [
            [],
            [Held(Card(a, 1))],
            [new Satchel.Item(Satchel.Kind.Rounds, "r", 6)],
        ];

        foreach (string site in new[] { b, KaamosLore.IceMoonBodyId, UndergroundComplex.FoundBandCheatSiteId })
        {
            foreach (List<Satchel.Item> wallet in wallets)
            {
                foreach (int level in UndergroundComplex.FloorsOf(site).Append(0).Take(8))
                {
                    RemoteSend.Sent sent = RemoteSend.Send(site, level, wallet);
                    Assert.False(string.IsNullOrWhiteSpace(sent.Line),
                        $"a send at {site} B{-level} answered with nothing.");
                    Assert.True(sent.Worked == (sent.OpenedGateId is not null),
                        "a send claimed to work and opened nothing, or opened something quietly.");
                }
            }
        }
    }

    // ── (d) THE ACCESSES, GROUPED ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #760 · The satchel groups the wallet under its outfits WHEN THERE ARE TWO OR MORE, and does not when
    /// there is one. A heading over a list that is entirely one thing is a label that cannot be read for
    /// information (#697's folder-of-one, one size up).
    ///
    /// <para><b>Proven RED</b> both ways. Grouping at one outfit (<c>order.Count &lt; 1</c>):</para>
    /// <code>
    /// Assert.Empty() Failure: Collection was not empty
    /// Collection: [Folder { Operator = Operator { Id = holbein, … }, Heading = 🏢 HOLBEIN &amp; SONS (MINERALS), … }]
    /// </code>
    /// <para>…and never grouping (<c>return []</c> unconditionally):</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 2
    /// Actual:   0
    /// </code>
    /// </summary>
    [Fact]
    public void TheWalletGetsHeadingsAtTwoOutfitsAndNotAtOne()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        // One outfit, two sites, three cards: no headings.
        Assert.Empty(SiteOperator.Accesses([Held(Card(a, 1)), Held(Card(b, 1)), Held(Card(a, 2))]));

        // Two outfits: one heading each, named, with every card accounted for and none duplicated.
        IReadOnlyList<SiteOperator.Folder> folders =
            SiteOperator.Accesses([Held(Card(a, 1)), Held(Card(stranger, 1)), Held(Card(b, 2))]);

        Assert.Equal(2, folders.Count);
        Assert.Equal(3, folders.Sum(f => f.Cards.Count));
        Assert.Contains(folders, f => f.Heading.Contains(SiteOperator.Of(a).Name, StringComparison.Ordinal));
        Assert.Contains(folders, f => f.Heading.Contains(SiteOperator.Of(stranger).Name, StringComparison.Ordinal));

        // The two sites of one outfit are under ONE heading — the grouping is by company and not by rock.
        SiteOperator.Folder theirs =
            Assert.Single(folders, f => f.Operator.Id == SiteOperator.Of(a).Id);
        Assert.Equal(2, theirs.Cards.Count);

        // Anything that is not an authority is not in the wallet.
        Assert.Empty(SiteOperator.Accesses(
            [new Satchel.Item(Satchel.Kind.Paper, "p"), new Satchel.Item(Satchel.Kind.Dirt, "d")]));
        Assert.Empty(SiteOperator.Accesses(null));

        // …and a card this build cannot read is GROUPED, never dropped. A pocket that showed three cards
        // flat and two grouped would be the dialog eating a possession to tidy a heading (#678's law).
        IReadOnlyList<SiteOperator.Folder> withJunk = SiteOperator.Accesses(
            [Held(Card(a, 1)), new Satchel.Item(Satchel.Kind.Authority, "not-a-card-this-build-wrote")]);
        Assert.Equal(2, withJunk.Count);
        Assert.Equal(2, withJunk.Sum(f => f.Cards.Count));
        Assert.Contains(withJunk, f => f.Heading.Contains(
            SiteOperator.UnplaceableStandingName, StringComparison.Ordinal));
    }

    // ── (e) THE CANON SWEEP ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #760 · NOTHING DOWN THERE EMITS ANYTHING. The head office publishes no network and the halls have
    /// none, and no string this feature ships may say — or imply by a word of radio craft — that anybody
    /// answered.
    ///
    /// <para><b>Proven RED</b> by planting <i>"a carrier locks up for a moment and something acknowledges,
    /// then drops"</i> into <c>NoNetworkLine</c>:</para>
    /// <code>
    /// Assert.DoesNotContain() Failure: Sub-string found
    /// String: ···"oment and something acknowledges, then dr"···
    /// Found:  "acknowledg"
    /// </code>
    /// </summary>
    [Fact]
    public void NothingSaysTheSilenceAnswered()
    {
        string[] banned =
        [
            "acknowledg", "transponder", "beacon", "handshake", "reply", "replies",
            "listening", "watcher", "kaamos", "projekti",
        ];

        List<string> shipped =
        [
            RemoteSend.NoNetworkLine, RemoteSend.NothingToSendLine, RemoteSend.NothingBelowLine,
            RemoteSend.RefusedPreamble, RemoteSend.Blurb, RemoteSend.OpenLabel,
            SiteOperator.UnplaceableStandingName,
            .. SiteOperator.All.Select(o => o.Name),
        ];

        foreach (string line in shipped)
        {
            foreach (string word in banned)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // …and the one outfit that must never publish one, never does — however the roll is re-seeded.
        Assert.False(SiteOperator.TheParentUndertaking.PublishesNetwork);
        Assert.Equal(SiteOperator.TheParentUndertaking, SiteOperator.Of(KaamosLore.IceMoonBodyId));
        Assert.All(SweepSites(), b => Assert.True(SiteOperator.Of(b).PublishesNetwork,
            "a branch office stopped answering its radio — which is the head office's own absence, spent."));
    }
}

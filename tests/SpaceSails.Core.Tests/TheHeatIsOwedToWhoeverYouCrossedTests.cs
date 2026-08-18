using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #715 · <b>ILLEGAL HEAT IS OWED TO WHOEVER YOU CROSSED.</b> Owner, 2026-08-05: <i>"the illegal heat should
/// be targeted at the entity we crossed … so not like the Casinos that distribute cheaters lists in Vegas"</i>
/// and <i>"we need to get out and let the heat of discovery to the site cool down"</i>.
///
/// <para>Six guards, each watched going RED before it was allowed to go green, with the failure written on
/// the guard verbatim — copied out of the run, not out of an expectation. A guard nobody has seen fail proves
/// the shape of a symptom rather than a law (#587's lesson, paid for twice in this repository).</para>
///
/// <para>The sweep is generated site ids rather than the named moons, for <c>TheStandingTravelsTests</c>'
/// reason: what is under audit is a rule about OUTFITS, and a handful of rocks cannot be relied on to supply
/// two of one company and one of another.</para>
/// </summary>
public sealed class TheHeatIsOwedToWhoeverYouCrossedTests
{
    private const int SweepSize = 120;

    private static IEnumerable<string> SweepSites() =>
        Enumerable.Range(0, SweepSize).Select(i => $"sweep-site-{i}");

    /// <summary>Sites with an ordinary gate off B1 — the commonest door in the game.</summary>
    private static IReadOnlyList<string> GatedSites() =>
        [.. SweepSites().Where(b => UndergroundComplex.NextShaftBelow(b, -1) == 1)];

    /// <summary>Two sites of ONE outfit and a third of another, found by walking the world rather than by
    /// typing three ids in and hoping.</summary>
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

    // ── (a) ONE OUTFIT REMEMBERS, AND NOBODY ELSE HEARS ABOUT IT ────────────────────────────────────────

    /// <summary>
    /// #715 · <b>THE VEGAS ANTI-PATTERN, AS A RED LINE.</b> A crossing at one of an outfit's sites is owed to
    /// THAT OUTFIT — felt at every site of theirs, and at not one site of anybody else's, over a sweep of a
    /// hundred and twenty real sites. Every effect the meter has is asked the same way, so an effect that
    /// leaked would fail here rather than in a playtest.
    ///
    /// <para><b>Proven RED</b> by keying the charge to the body instead of the outfit
    /// (<c>new HeatCharge(bodyId, WeightOf(why))</c>) — the rock is charged, so the company that owns it, and
    /// every site of theirs, stays cold:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 4
    /// Actual:   0
    /// </code>
    ///
    /// <para>…and <b>RED the other way</b> by making the bank a shared list — the Vegas one — (<c>foreach (var
    /// op in SiteOperator.All) if (op.Id != charge.OperatorId) book.ApplyHeat(LedgerId(op.Id), …)</c>), which
    /// leaves the crossed outfit's own total right and burns a stranger:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 0
    /// Actual:   4
    /// </code>
    /// </summary>
    [Fact]
    public void ACrossingIsFeltAtEverySiteOfThatOutfit_AndAtNobodyElses()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        // The fixture is not degenerate: two really are one company and the third really is another.
        Assert.Equal(SiteOperator.Of(a).Id, SiteOperator.Of(b).Id);
        Assert.NotEqual(SiteOperator.Of(a).Id, SiteOperator.Of(stranger).Id);

        var book = new ContactLedger();
        IllegalHeat.Bank(book, IllegalHeat.Charge(a, IllegalHeat.Crossing.TheKickOut), 0);

        int owed = IllegalHeat.WeightOf(IllegalHeat.Crossing.TheKickOut);
        Assert.True(owed > 0, "the dearest crossing in the game costs nothing.");

        Assert.Equal(owed, IllegalHeat.HeatAtSite(book, a));
        Assert.True(IllegalHeat.HeatAtSite(book, b) == owed,
            $"{b} is cold, and it is a {SiteOperator.Of(a).Name} site exactly like {a}.");
        Assert.Equal(0, IllegalHeat.HeatAtSite(book, stranger));

        // The whole sweep obeys it, and so does every effect derived from it. THIS is the anti-Vegas law:
        // one company's memory, asked of a hundred and twenty doors.
        string crossed = SiteOperator.Of(a).Id;
        int strangers = 0;
        foreach (string site in GatedSites())
        {
            bool theirs = string.Equals(SiteOperator.Of(site).Id, crossed, StringComparison.Ordinal);
            int heat = IllegalHeat.HeatAtSite(book, site);

            if (!theirs)
            {
                strangers++;
                Assert.True(heat == 0,
                    $"{site} answers to {SiteOperator.Of(site).Name}, who were never crossed and were " +
                    "never told.");
            }
            else
            {
                Assert.Equal(owed, heat);
            }

            // THE GATE. It wants a face only where the outfit remembers you; a stranger's identical gate,
            // at the identical band, with the identical wallet, opens.
            Assert.Equal(theirs, UndergroundComplex.TheGateWantsAFaceHere(site, [], heat));
            Assert.Equal(
                theirs,
                !UndergroundComplex.TheGateReads(site, -1, [Held(Card(site, 1))], heat).Worked);

            // THE ROUND. Its patience starts lower only at their sites.
            Assert.Equal(theirs, IllegalHeat.StartingRung(heat) > 0);

            // THE LINE. Said at their doors and nowhere else.
            Assert.Equal(theirs, IllegalHeat.TheyRememberYouAt(book, site));
        }

        Assert.True(strangers > 0, "every site in the sweep answered to one company — the sweep is vacuous.");
    }

    /// <summary>
    /// #715 · <b>AND THE SEND, WHICH IS THE ONE EFFECT THAT TAKES A VERB AWAY.</b> Above the top rung an
    /// outfit's net has nothing to say to that ship's name; at a stranger's site the same wallet, the same
    /// band and the same total open the gate over the air.
    ///
    /// <para><b>Proven RED</b> by the same shared list — the Vegas shape, one crossing told to everybody —
    /// which stops a stranger's net answering a ship that never went near them:</para>
    /// <code>
    /// the send was refused at sweep-site-3, whose outfit has never heard of this ship.
    /// </code>
    /// </summary>
    [Fact]
    public void TheNetStopsAnsweringAtTheirSites_AndAnswersEverywhereElse()
    {
        (string a, string b, string stranger) = TwoOfOneAndOneOfAnother();

        var book = new ContactLedger();
        for (int i = 0; i < 6; i++)
        {
            IllegalHeat.Bank(book, IllegalHeat.Charge(a, IllegalHeat.Crossing.RefusedSend), i);
        }
        Assert.True(IllegalHeat.TheNetStopsAnswering(IllegalHeat.HeatAtSite(book, a)),
            "six refusals did not reach the rung the net stops answering at.");

        // Their other site: a good card, refused over the air, because it is the SHIP they have stopped
        // answering and not the door.
        RemoteSend.Sent theirs = RemoteSend.Send(
            b, -1, [Held(Card(b, 1))], IllegalHeat.HeatAtSite(book, b));
        Assert.False(theirs.Worked);
        Assert.Contains(IllegalHeat.TheNetWillNotAnswerLine, theirs.Line, StringComparison.Ordinal);
        Assert.StartsWith(RemoteSend.RefusedPreamble, theirs.Line, StringComparison.Ordinal);
        Assert.Equal(SiteOperator.Of(b).Id, theirs.Charge.OperatorId);

        // A stranger's site, same wallet, same band: it opens.
        RemoteSend.Sent elsewhere = RemoteSend.Send(
            stranger, -1, [Held(Card(stranger, 1))], IllegalHeat.HeatAtSite(book, stranger));
        Assert.True(elsewhere.Worked,
            $"the send was refused at {stranger}, whose outfit has never heard of this ship.");
        Assert.Equal(0, elsewhere.Charge.Points);
    }

    // ── (b) IT COOLS IN ABSENCE, AND IN NOTHING ELSE ────────────────────────────────────────────────────

    /// <summary>
    /// #715 · <b>"WE NEED TO GET OUT AND LET THE HEAT OF DISCOVERY COOL DOWN."</b> Standing on their ground
    /// cools nothing, for any length of time; the same hours spent anywhere else cool it point by point.
    ///
    /// <para><b>Proven RED</b> by cooling on-site (dropping the underfoot clause from <c>Cool</c>) — a
    /// hundred hours under their own lights then cools the whole of it away:</para>
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: 4
    /// Actual:   0
    /// </code>
    ///
    /// <para>…and <b>RED the other way</b> by never cooling at all (returning at the top of <c>Cool</c>):</para>
    /// <code>
    /// an hour away from them cooled nothing — the meter is a ratchet.
    /// </code>
    /// </summary>
    [Fact]
    public void ItCoolsOnlyWhileYouAreOffTheirGround()
    {
        (string a, _, _) = TwoOfOneAndOneOfAnother();
        string outfit = SiteOperator.Of(a).Id;

        var book = new ContactLedger();
        IllegalHeat.Bank(book, IllegalHeat.Charge(a, IllegalHeat.Crossing.TheKickOut), 0);
        int charged = IllegalHeat.HeatAt(book, outfit);
        Assert.True(charged >= 2, "the kick-out is too cheap for this guard to be able to fail.");

        // A hundred hours standing on their own ground. The clock runs; nothing is banked.
        for (int hour = 1; hour <= 100; hour++)
        {
            IllegalHeat.Cool(book, outfit, hour * IllegalHeat.CoolsOnePointEverySeconds);
        }
        Assert.Equal(charged, IllegalHeat.HeatAt(book, outfit));

        // …and then somewhere else. The same hours, and now they count.
        double now = 100 * IllegalHeat.CoolsOnePointEverySeconds;
        IllegalHeat.Cool(book, null, now + IllegalHeat.CoolsOnePointEverySeconds);
        Assert.True(IllegalHeat.HeatAt(book, outfit) < charged,
            "an hour away from them cooled nothing — the meter is a ratchet.");

        // Long enough away and it is gone entirely: pressure, never a permanent mark.
        IllegalHeat.Cool(book, null, now + (200 * IllegalHeat.CoolsOnePointEverySeconds));
        Assert.Equal(0, IllegalHeat.HeatAt(book, outfit));

        // And a captain standing on ANOTHER outfit's ground is away from this one — that is the whole of
        // "burn one entity, work the other".
        IllegalHeat.Bank(book, IllegalHeat.Charge(a, IllegalHeat.Crossing.TheKickOut), 0);
        IllegalHeat.Cool(book, "somebody-else", 500 * IllegalHeat.CoolsOnePointEverySeconds);
        Assert.Equal(0, IllegalHeat.HeatAt(book, outfit));
    }

    // ── (c) TWO METERS, TWO HOLDERS, NO SHARED TRUTH ────────────────────────────────────────────────────

    /// <summary>
    /// #715/#618 · <b>THE AXIS QUESTION, ANSWERED AS ARITHMETIC.</b> The ship's heat
    /// (<see cref="EncounterRule"/> — what the LAW thinks of a hull, and the meter #582 will extend
    /// per-place) and an outfit's memory of a captain are different numbers with different keys and
    /// different holders. Raising either leaves the other exactly where it was, and — the sharp end —
    /// <b>they cool on different clocks in different places</b>: the ship's cools wherever you are, and this
    /// one does not cool at all where you are standing.
    ///
    /// <para><b>Proven RED</b> by aliasing their CLOCKS — letting the outfit's memory decay on the ship's own
    /// rule, which cools wherever you happen to be (the underfoot clause dropped from <c>Cool</c>). Sixty days
    /// standing in their own lobby then forgets the whole of it, for no reason but that the law forgot the
    /// hull in the same sixty days:</para>
    /// <code>
    /// the outfit forgot you because the LAW did.
    /// </code>
    /// </summary>
    [Fact]
    public void TheShipsHeatAndAnOutfitsMemoryAreNotTheSameNumber()
    {
        (string a, _, _) = TwoOfOneAndOneOfAnother();
        string outfit = SiteOperator.Of(a).Id;
        var book = new ContactLedger();

        // Raise the LAW's opinion of the hull. Nobody's company memory moves.
        HeatState ship = EncounterRule.RaiseHeat(HeatState.None, 2, 0);
        Assert.Equal(2, ship.Level);
        foreach (SiteOperator.Operator op in SiteOperator.All)
        {
            Assert.Equal(0, IllegalHeat.HeatAt(book, op.Id));
        }

        // Raise the OUTFIT's. The law's opinion of the hull does not move — nothing was reported, because
        // reporting it means admitting the basement exists.
        IllegalHeat.Bank(book, IllegalHeat.Charge(a, IllegalHeat.Crossing.TheKickOut), 0);
        int owed = IllegalHeat.HeatAt(book, outfit);
        Assert.True(owed > 0);
        Assert.Equal(2, ship.Level);

        // …and now the clocks. Sixty days of standing on THIS outfit's ground: the law forgets the hull
        // entirely (twenty days a level, so two levels are gone well inside it), and the outfit forgets
        // nothing at all, because none of those days was a day away from them.
        double sixtyDays = 60 * 86400.0;
        HeatState cooled = EncounterRule.DecayHeat(ship, sixtyDays, atHavenOrbit: false);
        for (double t = 3600; t <= sixtyDays; t += 3600)
        {
            IllegalHeat.Cool(book, outfit, t);
        }

        Assert.Equal(0, cooled.Level);
        Assert.True(IllegalHeat.HeatAt(book, outfit) == owed, "the outfit forgot you because the LAW did.");
    }

    // ── THE TABLE, AND THAT IT IS NOT VACUOUS ───────────────────────────────────────────────────────────

    /// <summary>
    /// #715 · <b>EVERY CROSSING COSTS SOMETHING, AND EVERY ONE OF THEM IS OWED TO THE OUTFIT.</b> The
    /// anti-vacuous half of the sweep: at least two outfits appear in it and at least one charge of every
    /// kind is produced, so no assertion above is passing because nothing happened.
    ///
    /// <para>The three machine refusals are the SAME number the gate has published since #929
    /// (<c>UndergroundComplex.RefusedCardHeat</c>), read rather than re-typed — the day it is tuned there is
    /// one number to change and no second spelling to find.</para>
    ///
    /// <para><b>Proven RED</b> by giving a crossing no weight (<c>Crossing.RefusedPress =&gt; 0</c>):</para>
    /// <code>
    /// RefusedPress costs nothing — a crossing nobody is charged for is not a crossing.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryCrossingCostsSomething_OwedToTheOutfitAndNeverToTheMoon()
    {
        IReadOnlyList<string> gated = GatedSites();
        var outfits = new HashSet<string>(StringComparer.Ordinal);
        var kinds = new HashSet<IllegalHeat.Crossing>();

        foreach (string site in gated)
        {
            outfits.Add(SiteOperator.Of(site).Id);
            foreach (IllegalHeat.Crossing why in Enum.GetValues<IllegalHeat.Crossing>())
            {
                UndergroundComplex.HeatCharge charge = IllegalHeat.Charge(site, why);
                Assert.True(charge.Points > 0,
                    $"{why} costs nothing — a crossing nobody is charged for is not a crossing.");
                Assert.Equal(SiteOperator.Of(site).Id, charge.OperatorId);
                Assert.NotEqual(site, charge.OperatorId);
                kinds.Add(why);
            }
        }

        Assert.True(outfits.Count >= 2, $"the sweep only ever met {outfits.Count} outfit(s).");
        Assert.Equal(Enum.GetValues<IllegalHeat.Crossing>().Length, kinds.Count);

        // The three machine refusals read the gate's own published number rather than a second copy of it.
        Assert.Equal(
            UndergroundComplex.RefusedCardHeat,
            IllegalHeat.WeightOf(IllegalHeat.Crossing.RefusedCardAtAGate));
        Assert.Equal(
            UndergroundComplex.RefusedCardHeat, IllegalHeat.WeightOf(IllegalHeat.Crossing.RefusedSend));
        Assert.Equal(
            UndergroundComplex.RefusedCardHeat, IllegalHeat.WeightOf(IllegalHeat.Crossing.RefusedPress));

        // A refused card at a gate is the charge #929 has been publishing all along, verbatim.
        Assert.Equal(
            UndergroundComplex.RefusedAtTheGate(gated[0]),
            IllegalHeat.Charge(gated[0], IllegalHeat.Crossing.RefusedCardAtAGate));

        // And the round's two are dearer than a machine's no, because a person was there.
        Assert.True(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.TheEscort) > UndergroundComplex.RefusedCardHeat);
        Assert.True(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.TheKickOut)
                > IllegalHeat.WeightOf(IllegalHeat.Crossing.TheEscort));
    }

    /// <summary>
    /// #715 · <b>PRESSURE, NEVER A LOCKOUT — and the way out is a paper the game already issues.</b> The hot
    /// gate wants the site's own pass (#804's badge) with the card, and takes it; and the head office is
    /// never in this at all, because there is no gate there to ask anything (#411).
    ///
    /// <para><b>Proven RED</b> by leaving the badge out of the predicate:</para>
    /// <code>
    /// the gate refused a captain carrying this site's own pass — that is a lockout, not pressure.
    /// </code>
    /// </summary>
    [Fact]
    public void TheHotGateTakesTheSitesOwnPass_AndTheHeadOfficeNeverAsks()
    {
        (string a, _, _) = TwoOfOneAndOneOfAnother();
        int hot = IllegalHeat.TheGateWantsAFaceAt;

        Assert.True(UndergroundComplex.TheGateWantsAFaceHere(a, [Held(Card(a, 1))], hot));
        Assert.False(
            UndergroundComplex.TheGateWantsAFaceHere(a, [Held(Card(a, 1)), PatrolBeat.Badge(a)], hot),
            "the gate refused a captain carrying this site's own pass — that is a lockout, not pressure.");

        // …and the card is read as it always was once the pass is in the wallet.
        Assert.True(
            UndergroundComplex.TheGateReads(a, -1, [Held(Card(a, 1)), PatrolBeat.Badge(a)], hot).Worked);

        // #411 · The head office asks the captain for nothing on any floor, at any temperature.
        string head = KaamosLore.IceMoonBodyId;
        Assert.False(UndergroundComplex.TheGateWantsAFaceHere(head, [], IllegalHeat.Ceiling));
    }
}

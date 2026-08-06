using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #752 · THE CHIT MUST OPEN THE CAGE. Owner, playing the #748 table scene to its promised end: the Hand
/// hands over the DAY-LABOUR CHIT with <i>"take this to the lift and don't be clever near the counter"</i>,
/// and at the lift the sealed row answered beautifully — and never once looked at the wallet.
///
/// <para>The job was <i>"a job that gets us downstairs with cover"</i>. It stopped one door short of the
/// door it was about, and nothing was red, because every guard this ground has asks what the panel says to
/// a captain carrying a COUNTERSIGNATURE. Nobody had ever asked it what it says to a captain carrying a
/// timesheet.</para>
///
/// <para>The site under test is the one the cheat URL boots (<c>?tablescene=1</c> ⇒
/// <c>?secretlab=deep&amp;land=1&amp;floor=1</c>): the twenty-floor clinic with a band nobody listed under
/// it. Every floor number in here is DERIVED from Core's own band arithmetic first and only then stated,
/// because a typed floor number is a second opinion about the shape of a building (§13.15).</para>
/// </summary>
public sealed class TheChitOpensTheCageTests
{
    /// <summary>The rock the table scene boots on, and the rock the owner played this on.</summary>
    private const string DeepSite = "secret-lab-site-unlisted";

    /// <summary>B1 — the canteen's own floor, where the Hand said to take it.</summary>
    private const int B1 = -1;

    private static readonly IReadOnlyList<Satchel.Item> Chit = [CanteenTable.Chit(underAnotherName: false)];

    private static IReadOnlyList<UndergroundComplex.LiftStop> Panel(
        string body, int level, IReadOnlyList<Satchel.Item>? carried, params string[] cards) =>
        UndergroundComplex.LiftPanel(body, level, cards, carried);

    /// <summary>The gate off THIS car's band, whatever the panel decided to call it today.</summary>
    private static UndergroundComplex.LiftStop OtherShaft(
        IReadOnlyList<UndergroundComplex.LiftStop> panel, int level) =>
        Assert.Single(panel, s => s.Level < level && UndergroundComplex.BandOf(s.Level) != UndergroundComplex.BandOf(level));

    [Fact]
    public void TheCageBandsTopFloorIsWHERECoreSaysTheNextShaftOpens()
    {
        // The destination, derived — and then, only then, stated. B1's car serves band 0; the shaft under it
        // is the next band that EXISTS (#677: never `band + 1`, which can name a hole), and a car opens at
        // its band's top floor. On the twenty-floor clinic that comes to B5, and if the generator's shape
        // ever changes, this is the line that says so out loud rather than the feature quietly moving.
        int? next = UndergroundComplex.NextShaftBelow(DeepSite, B1);
        Assert.NotNull(next);
        int top = UndergroundComplex.BandTop(next.Value);

        Assert.Equal(1, next.Value);
        Assert.Equal(-5, top);   // B5 — derived above, written here so a change to it is a diff and not a surprise
    }

    [Fact]
    public void WithTheChitTheSealedRowBECOMESTheAffordanceAndItRidesToTheCageBandsTop()
    {
        // (a) · The whole issue. Same floor, same panel, one piece of paper in the wallet.
        int top = UndergroundComplex.BandTop(UndergroundComplex.NextShaftBelow(DeepSite, B1)!.Value);

        UndergroundComplex.LiftStop row = OtherShaft(Panel(DeepSite, B1, Chit), B1);

        Assert.Equal(top, row.Level);
        Assert.Null(row.Refusal);                       // it is a button that WORKS, not a button that explains
        Assert.Equal("↓ THE OTHER SHAFT", row.Name);    // and it has stopped calling itself SEALED
        Assert.True(row.OpenedByChit);
        Assert.Equal($"{CanteenTable.ChitGlyph} {CanteenTable.ChitTitle}", row.OpenedBy);

        // …and the row names the paper the player is actually carrying, in its own printed words, which is
        // #689's law: the panel can never promise a reading the gate will not give.
        Assert.Contains("DAY-LABOUR CHIT", row.OpenedBy);
    }

    [Fact]
    public void WithoutTheChitTheREFUSALStandsWordForWord()
    {
        // (b) · The beautiful answer stays exactly the beautiful answer. This string is quoted in the issue
        // and shipped since #590; a feature that improves a refusal nobody asked to have improved is a
        // feature that broke it.
        UndergroundComplex.LiftStop row = OtherShaft(Panel(DeepSite, B1, carried: null), B1);

        Assert.Equal("↓ THE OTHER SHAFT — SEALED", row.Name);
        Assert.Equal(
            "This car does not go lower. The shaft that does is on this floor, and its gate wants an " +
            "authority this building has not issued in a long time.",
            row.Refusal);
        Assert.Null(row.OpenedBy);
        Assert.False(row.OpenedByChit);

        // An empty satchel is the same world as no satchel — the panel asks the pocket, and an empty pocket
        // is an answer rather than a missing question.
        Assert.Equal(row, OtherShaft(Panel(DeepSite, B1, []), B1));
    }

    [Fact]
    public void TheCoverIsREADFromTheSatchelAndNotReDerivedFromOneId()
    {
        // #746 records the cover as the chit's PRESENCE, and there are two chits: the one you talked your way
        // into and the one written under a name the Hand chose (#718). Both are cover. A panel that had
        // taught itself the id string would open for one of them and refuse the other, and the refused one
        // is the outcome a YES-BUT gives — the likelier half of the scene.
        IReadOnlyList<Satchel.Item> otherName = [CanteenTable.Chit(underAnotherName: true)];

        Assert.True(CanteenTable.Cover.Held(otherName));
        Assert.Null(OtherShaft(Panel(DeepSite, B1, otherName), B1).Refusal);
    }

    [Fact]
    public void TheChitOpensTheCAGEAndNothingDeeper()
    {
        // It is not a clearance. A day-labour chit is a name on the cage crew's list, worth the trip the cage
        // makes; the gates below answer to an office, and no amount of shift work is a countersignature. So
        // on every band under the first one the row is sealed exactly as it is today.
        foreach (int level in UndergroundComplex.FloorsOf(DeepSite).Where(f => UndergroundComplex.BandOf(f) > 0))
        {
            IReadOnlyList<UndergroundComplex.LiftStop> panel = Panel(DeepSite, level, Chit);
            Assert.DoesNotContain(panel, s => s.OpenedByChit);
        }
    }

    [Fact]
    public void TheChitNEVERTellsYouAboutTheBandNobodyListed()
    {
        // #592's silence is the one thing a new affordance could break without anybody noticing: a refusal
        // that NAMES the unlisted shaft announces the secret in the sentence it cannot survive. The chit
        // cannot reach that branch — but "cannot" is a claim, and this is the test of it. On the last listed
        // floor, a captain carrying cover sees the panel of an ordinary site's bottom, same as today.
        int lastListed = UndergroundComplex.DepthOf(DeepSite);
        Assert.True(UndergroundComplex.HasUnlistedBand(DeepSite), "the deep cheat rock stopped hiding a band");

        Assert.Equal(
            Panel(DeepSite, lastListed, carried: null),
            Panel(DeepSite, lastListed, Chit));
    }

    [Fact]
    public void TheCardKeepsEverythingItEverOpenedAndTheTwoPapersDoNotBLEND()
    {
        // Two papers, two doors, one gate. With the countersignature carried the row is the card's row —
        // the card's own title, the card's ride, and NOT flagged as a chit crossing, whatever else is in the
        // satchel. The card is the deeper permission and it wins the row it shares.
        int next = UndergroundComplex.NextShaftBelow(DeepSite, B1)!.Value;
        var card = new UndergroundComplex.AuthorityCard(DeepSite, next);

        UndergroundComplex.LiftStop row = OtherShaft(Panel(DeepSite, B1, Chit, card.Id), B1);

        Assert.Null(row.Refusal);
        Assert.False(row.OpenedByChit);
        Assert.Equal(UndergroundComplex.CardTitle(card), row.OpenedBy);
    }

    [Fact]
    public void ARideTheCHITOpenedIsNotAnOfficeVouchingForYou()
    {
        // The arrival's discriminator, in Core where the test can reach it. `GateOpenedByRidingTo` answers
        // "which gate did this trip cross" and the client narrates a countersignature being read off it. A
        // chit ride must answer NULL there — an office that stopped existing did not vouch for anybody; a
        // man with a clipboard ticked a box.
        int top = UndergroundComplex.BandTop(UndergroundComplex.NextShaftBelow(DeepSite, B1)!.Value);

        Assert.Null(UndergroundComplex.GateOpenedByRidingTo(DeepSite, B1, top, [], Chit));

        // …while the card's ride still answers with the card, which is #689 undisturbed.
        var card = new UndergroundComplex.AuthorityCard(DeepSite, UndergroundComplex.BandOf(top));
        Assert.Equal(
            card,
            UndergroundComplex.GateOpenedByRidingTo(DeepSite, B1, top, [card.Id], Chit));
    }
}

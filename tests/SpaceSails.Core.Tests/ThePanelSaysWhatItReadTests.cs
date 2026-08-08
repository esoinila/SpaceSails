using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #684 · THE BEST REFUSALS IN THE GAME HAD NO READER.
///
/// <para>Surfaced by #683's storytelling pass: <c>SatchelTry.Target.ShaftGate</c> — taught by #679/#683 to
/// tell <i>another shaft of THIS site</i> from <i>somebody else's building</i>, each named — had no client
/// caller at all. The lift panel reads the wallet itself and answered out of a second set of sentences of
/// its own (<c>UndergroundComplex.WrongCardLine</c>), which said the same flat thing to every wrong card.
/// Two answers to one question, and the better one was Core-only prose nobody could reach.</para>
///
/// <para>Owner's ruling: the unprompted read STAYS — it is the panel's character and a TRY verb at the gate
/// would make the building polite — but the read has to be TOLD. So the panel routes through the matrix,
/// the matrix is the one source, and the answer goes up as a card in the house idiom with the presented
/// card's own face on it.</para>
///
/// <para>These pin the seam that makes that true: <see cref="UndergroundComplex.TheGateReads"/> is a
/// pass-through, not a paraphrase, and its picture is the office that issued the card the gate ACTUALLY
/// read.</para>
/// </summary>
public sealed class ThePanelSaysWhatItReadTests
{
    /// <summary>A site with more than one band, and a floor of its first band to stand on. Asserted rather
    /// than assumed — a fixture whose gate does not exist would make every case below vacuously equal.</summary>
    private const string Site = "luna";

    private const int Standing = -1;

    private static Satchel.Item Card(string body, int band) =>
        new(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard(body, band).Id);

    private static UndergroundComplex.AuthorityCard Gate()
    {
        int? next = UndergroundComplex.NextShaftBelow(Site, Standing);
        Assert.True(next is not null,
            $"{Site} has no shaft below B{-Standing} — this fixture has no gate in it and every assertion " +
            "below would be about a refusal nobody can stand in front of.");
        return new UndergroundComplex.AuthorityCard(Site, next!.Value);
    }

    /// <summary>The four things a captain can be holding when the panel goes through the wallet: nothing, a
    /// card for another shaft of this site, a card for another site, and both at once.</summary>
    private static IEnumerable<(string Class, List<Satchel.Item> Wallet)> EveryRefusalClass()
    {
        int otherBand = Gate().Band == 0 ? 1 : 0;
        yield return ("empty wallet", []);
        yield return ("wrong shaft, this site", [Card(Site, otherBand)]);
        yield return ("wrong site", [Card("titan", Gate().Band)]);
        yield return ("both", [Card("titan", Gate().Band), Card(Site, otherBand)]);
    }

    /// <summary>
    /// The panel's read is the MATRIX'S read — the same sentence, word for word, for every class of refusal
    /// there is.
    ///
    /// <para><b>Proven RED</b> by putting the old panel prose back into <c>TheGateReads</c> (the deleted
    /// <c>WrongCardLine</c>, restored and returned instead of <c>read.Outcome.Line</c>), which is exactly the
    /// shipped behaviour this issue was filed on:</para>
    /// <code>
    /// Assert.Equal() Failure: Strings differ
    ///                ↓ (pos 7)
    /// Expected: "🔒 The wallet is empty. The gate reads au"···
    /// Actual:   "🔒 The second shaft is here, below B1, an"···
    ///                ↑ (pos 7)
    /// </code>
    /// </summary>
    [Fact]
    public void ThePanelsReadIsTheMatrixsRead_WordForWord()
    {
        UndergroundComplex.AuthorityCard gate = Gate();

        foreach ((string cls, List<Satchel.Item> wallet) in EveryRefusalClass())
        {
            UndergroundComplex.GateRead read = UndergroundComplex.TheGateReads(Site, Standing, wallet);
            SatchelTry.Outcome matrix =
                SatchelTry.OfferWallet(wallet, SatchelTry.Target.ShaftGate, gate.Id);

            Assert.False(read.Worked, $"[{cls}] the gate opened for a wallet that does not hold its card.");
            Assert.Equal(matrix.Line, read.Line);
            Assert.False(read.Worked, $"[{cls}] the panel and the matrix disagree about whether it worked.");
        }
    }

    /// <summary>
    /// …and each class is still tellable apart FROM THE WORDS ALONE, which is the whole of #679's ask and the
    /// only reason routing the panel through the matrix is worth doing.
    ///
    /// <para>The pairwise inequality is also what stops this file being a world that cannot tell pass from
    /// fail: if the matrix ever collapsed back to one sentence, every equality above would still hold and
    /// nothing would have been gained.</para>
    ///
    /// <para><b>Proven RED</b> against the same restored panel prose, which answered every wrong card with
    /// the same roll-call:</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// String:    "🔒 The second shaft's gate reads what you"···
    /// Not found: "shaft 1 of this site"
    /// </code>
    /// </summary>
    [Fact]
    public void EachRefusalClassSaysSomethingTheOthersDoNot()
    {
        UndergroundComplex.AuthorityCard gate = Gate();
        int otherBand = gate.Band == 0 ? 1 : 0;

        string empty = UndergroundComplex.TheGateReads(Site, Standing, []).Line;
        string thisSite = UndergroundComplex.TheGateReads(Site, Standing, [Card(Site, otherBand)]).Line;
        string elsewhere = UndergroundComplex.TheGateReads(Site, Standing, [Card("titan", gate.Band)]).Line;

        Assert.NotEqual(empty, thisSite);
        Assert.NotEqual(empty, elsewhere);
        Assert.NotEqual(thisSite, elsewhere);

        // And each one carries its OWN fact: the shaft it runs, the site it was issued for, the empty pocket.
        Assert.Contains($"shaft {otherBand + 1} of this site", thisSite, StringComparison.Ordinal);
        Assert.Contains($"{BodyNames.Designation("titan")} SITE", elsewhere, StringComparison.Ordinal);
        Assert.Contains("wallet is empty", empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The nearest miss is the one the panel answers with, and it is the one PICTURED. #683's ladder decides
    /// the sentence; the face has to follow it, or the card shows one office while the words name another.
    /// </summary>
    [Fact]
    public void TheFaceOnTheCardIsTheOfficeThatIssuedTheCardTheGateREAD()
    {
        UndergroundComplex.AuthorityCard gate = Gate();
        int otherBand = gate.Band == 0 ? 1 : 0;

        // A wallet holding both: #683's ladder says another shaft of THIS site beats another site, because it
        // is the one that tells the captain which floor to go back for.
        UndergroundComplex.GateRead read = UndergroundComplex.TheGateReads(
            Site, Standing, [Card("titan", gate.Band), Card(Site, otherBand)]);

        Assert.NotNull(read.Presented);
        Assert.Equal(new UndergroundComplex.AuthorityCard(Site, otherBand), read.Presented!.Value);
        Assert.Equal(UndergroundComplex.AuthorityCardArtUrl(read.Presented!.Value), read.ArtUrl);
        Assert.Contains(read.ArtUrl, UndergroundComplex.CardOffices.Select(o => o.ArtUrl));

        // The fixture is not degenerate: the two cards in that wallet really do have different faces to get
        // wrong. (Same office for both would make the assertion above true whichever one was picked.)
        Assert.NotEqual(
            UndergroundComplex.AuthorityCardArtUrl(new UndergroundComplex.AuthorityCard("titan", gate.Band)),
            UndergroundComplex.AuthorityCardArtUrl(new UndergroundComplex.AuthorityCard(Site, otherBand)));
    }

    /// <summary>An empty wallet is a card with no card in it, and it must never wear one of the five
    /// letterheads — that would be the game showing the captain a pass they do not have.</summary>
    [Fact]
    public void AnEmptyWalletIsPicturedWithNobodysOffice()
    {
        UndergroundComplex.GateRead read = UndergroundComplex.TheGateReads(Site, Standing, []);

        Assert.Null(read.Presented);
        Assert.Equal(UndergroundComplex.AuthorityCardFallbackArtUrl, read.ArtUrl);
        Assert.DoesNotContain(read.ArtUrl, UndergroundComplex.CardOffices.Select(o => o.ArtUrl));
    }

    /// <summary>The other end of the same read. It is the RIDE's beat (#592/#689's sentence, untouched), it
    /// wears the same title as the refusal, and its face is the card the gate agreed with.</summary>
    [Fact]
    public void TheAcceptedReadIsTheRidesOwnBeatAndWearsTheSameTitle()
    {
        var card = new UndergroundComplex.AuthorityCard(Site, 1);
        UndergroundComplex.GateRead read = UndergroundComplex.TheGateAccepted(card);

        Assert.True(read.Worked);
        Assert.Equal(UndergroundComplex.CardAcceptedLine(card), read.Line);
        Assert.Equal(card, read.Presented);
        Assert.Equal(UndergroundComplex.AuthorityCardArtUrl(card), read.ArtUrl);
        Assert.Equal(UndergroundComplex.GateReadLabel, read.Label);
        Assert.Equal(UndergroundComplex.GateReadLabel, UndergroundComplex.TheGateReads(Site, Standing, []).Label);
    }

    /// <summary>#590 call 2, one layer down: a wallet read at a door with no reader comes back holding
    /// NOTHING, however full the wallet was. A caller handed the card the fan ended on could paint a face
    /// onto the one sentence in the game whose whole job is to have no opinion.</summary>
    [Fact]
    public void ADoorWithNoReaderNamesNoCardEvenWithAFullWallet()
    {
        List<Satchel.Item> wallet = [Card(Site, 0), Card("titan", 1), Card("europa", 2)];

        foreach (SatchelTry.Target final in new[] { SatchelTry.Target.SealedWay, SatchelTry.Target.RoomDoor })
        {
            SatchelTry.WalletRead read = SatchelTry.ReadTheWallet(wallet, final);
            Assert.Null(read.Read);
            Assert.False(read.Outcome.Worked);

            // …and a wallet of ONE goes the same way. It takes the single-card branch, which is exactly where
            // a "the fan did not run, so keep the card" shortcut would leak a face out of a final door.
            Assert.Null(SatchelTry.ReadTheWallet([Card(Site, 0)], final).Read);
        }

        // The sweep can find a card when there is one to find, so the nulls above are findings rather than a
        // blind assertion (the house's fifth bug class).
        Assert.NotNull(SatchelTry.ReadTheWallet(wallet, SatchelTry.Target.ShaftGate, Gate().Id).Read);
    }

    /// <summary>And the fold's own answer is unchanged by the split: <see cref="SatchelTry.OfferWallet"/> is
    /// <see cref="SatchelTry.ReadTheWallet"/> with the card thrown away, at every target, so #697's callers
    /// cannot have drifted.</summary>
    [Fact]
    public void TheFoldsAnswerIsUnchangedByKeepingTheCard()
    {
        List<Satchel.Item> wallet = [Card(Site, 0), Card("titan", 1)];

        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            foreach (List<Satchel.Item> pocket in new[] { wallet, [], new List<Satchel.Item> { Card(Site, 0) } })
            {
                SatchelTry.Outcome folded = SatchelTry.OfferWallet(pocket, target, Gate().Id);
                SatchelTry.WalletRead read = SatchelTry.ReadTheWallet(pocket, target, Gate().Id);
                Assert.Equal(folded.Line, read.Outcome.Line);
                Assert.Equal(folded.Worked, read.Outcome.Worked);
            }
        }
    }
}

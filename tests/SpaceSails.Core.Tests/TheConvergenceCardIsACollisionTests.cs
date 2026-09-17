namespace SpaceSails.Core.Tests;

/// <summary>
/// #422 · THE CONVERGENCE CARD IS A COLLISION, NOT A REVEAL (owner ruling 2026-09-17, option B of the
/// story pass).
///
/// <para>The card used to be eight sentences of third-person exposition that spent every secret both arcs
/// still had to give — the fresh copy that wakes certain it was always you, the ORIGINAL Nebula keeps filed
/// since your first premium, the archive that is awake — and it fired at a strictly lower bar than either
/// arc's own capstone, so arc 2's capstone became a recap of a card the player had already read. It is now
/// two sentences the captain is already carrying, one above the other, unannotated, and one closing line
/// that explains nothing.</para>
///
/// <para>These guards hold the three things that can quietly undo that: the card must quote the two voices
/// FROM THE SHARDS THAT AUTHOR THEM (one source of truth, so the card can never say a thing the world does
/// not), it must state none of the claims the old reveal stated, and each arc's capstone must still be
/// holding the reveal that was handed back to it.</para>
/// </summary>
public class TheConvergenceCardIsACollisionTests
{
    // ── 1 · One source of truth: the card's two voices are the shards' own sentences. ──

    [Fact]
    public void TheHoldersLineIsASubstringOfTheShardThatSpeaksIt()
    {
        KaamosFragment tell = KaamosLore.Fragments.Single(f => f.Id == ArcConvergence.KaamosSideShard);

        Assert.Contains(KaamosLore.HolderConvergenceLine, tell.Lore, StringComparison.Ordinal);
        Assert.False(tell.IsKey); // the bar names an INTEL shard, never a capstone (that would be option A)
    }

    [Fact]
    public void TheAdjustersLineIsASubstringOfTheShardThatSpeaksIt()
    {
        NebulaFragment tell = NebulaLore.Fragments.Single(f => f.Id == ArcConvergence.NebulaSideShard);

        Assert.Contains(NebulaLore.AdjusterConvergenceLine, tell.Lore, StringComparison.Ordinal);
        Assert.False(tell.IsKey);
    }

    [Fact]
    public void NeitherVoiceIsTypedTwiceInTheWorld()
    {
        // The whole point of the two consts. If a second literal copy of either sentence appears anywhere
        // in the authored pools, the card and the bar can drift apart from the line the captain read.
        List<string> everything = [.. KaamosLore.Fragments.Select(f => f.Lore),
                                   .. NebulaLore.Fragments.Select(f => f.Lore)];

        Assert.Single(everything, l => l.Contains(KaamosLore.HolderConvergenceLine, StringComparison.Ordinal));
        Assert.Single(everything, l => l.Contains(NebulaLore.AdjusterConvergenceLine, StringComparison.Ordinal));
    }

    // ── 2 · The card states none of the three facts the old reveal stated. ──

    /// <summary>Every word of authored copy the card puts on screen, in the order it appears.</summary>
    private static string CardCopy() =>
        KaamosLore.HolderConvergenceLine + "\n" +
        NebulaLore.AdjusterConvergenceLine + "\n" +
        ArcConvergence.ConvergenceReveal;

    [Theory]
    // ① the resurrection is a fresh copy that wakes certain it was always you
    [InlineData("copy")]
    [InlineData("fresh")]
    // ② Nebula keeps the ORIGINALS; the premium buys storage; you are the collateral
    [InlineData("original")]
    [InlineData("premium")]
    [InlineData("storage")]
    [InlineData("pattern")]
    [InlineData("backup")]
    [InlineData("withdrawal")]
    // ③ the archive is awake — the same wintering, scaled to a subscriber base
    [InlineData("archive")]
    [InlineData("held breath")]
    [InlineData("lucid")]
    [InlineData("wintering")]
    // …and the two names that would do the joining for the player
    [InlineData("Vantar")]
    [InlineData("KAAMOS")]
    public void TheCardDoesNotStateWhatTheOldRevealStated(string claim)
    {
        Assert.DoesNotContain(claim, CardCopy(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheCardIsShorterThanTheCapstoneItUsedToPreempt()
    {
        // Not a style rule — a shape rule. A card that says less than the answer it precedes cannot be the
        // answer. The old reveal was ~900 characters; policy-terms is the arc's own payoff and must remain
        // the longer read of the two.
        NebulaFragment capstone = NebulaLore.Fragments.Single(f => f.IsKey);
        Assert.True(CardCopy().Length < capstone.Lore.Length,
            $"the convergence card ({CardCopy().Length} chars) is longer than the capstone it fires before " +
            $"({capstone.Lore.Length}) — it is explaining again");
    }

    // ── 3 · Each arc's capstone still holds its own reveal, word for word. ──

    /// <summary>The KAAMOS capstone as it has shipped. Pinned so that "the card stops explaining" can never
    /// be paid for by quietly moving the explanation into the capstone (that is option C, and option C is a
    /// separate, owner-authorised piece of work).</summary>
    private const string BerthCodeAsShipped =
        "One number falls out of them, the string the sealed berth still listens for. It is not a " +
        "password so much as a name the dark already knows. Enter it on the board when the window opens " +
        "and the berth stops being a place nobody files for. It becomes a place expecting you. You " +
        "could go to the ice moon now. That was always the danger.";

    /// <summary>The NEBULA capstone as it has shipped — the one the old card used to reach first.</summary>
    private const string PolicyTermsAsShipped =
        "Assembled, the pieces resolve into the clause the sales voice skips: the premium does not buy a " +
        "rebirth, it buys STORAGE — your pattern kept in Nebula's cold archive, lucid enough to stay a " +
        "valid backup, forever, or until you lapse and forfeit it. Each death spends a fresh copy that " +
        "wakes certain it is you; the original never leaves the dark. They built the archive from a rig " +
        "they did not invent, degraded from something that kept whole crews awake in far colder water. " +
        "You are not insured against death. You are filed under it. That was always the contract.";

    [Fact]
    public void TheKaamosCapstoneTextIsUnchanged()
    {
        Assert.Equal(BerthCodeAsShipped, KaamosLore.KeyFragment.Lore);
    }

    [Fact]
    public void TheNebulaCapstoneTextIsUnchanged()
    {
        Assert.Equal(PolicyTermsAsShipped, NebulaLore.KeyFragment.Lore);
    }

    [Fact]
    public void TheCapstonesStillOutrankTheJointBar()
    {
        // The complaint the ruling answers: the joint card fired BELOW both capstones, so each capstone
        // resolved nothing new. The bar stays below them — that is deliberate, the recognition is mid-dig —
        // but the card no longer carries what the capstones are for, which is what the guards above pin.
        Assert.True(ArcConvergence.KaamosSideThreshold < KaamosLore.IntelNeededToUnlock);
        Assert.True(ArcConvergence.NebulaSideThreshold < NebulaLore.IntelNeededToUnlock);
    }
}

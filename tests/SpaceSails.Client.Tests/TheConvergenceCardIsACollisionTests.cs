using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #422 · WHAT THE CONVERGENCE CARD ACTUALLY PUTS ON THE SCREEN (owner ruling 2026-09-17, option B).
///
/// <para>The Core guards hold the STRINGS; these hold the CARD. Every one of them boots the game at
/// <c>/map?converge=1</c> — the documented cheat, the only way anyone reaches this beat on demand — and
/// reads the painted surface, because the whole complaint the ruling answers was about what a captain sees:
/// a full-screen modal in which the GAME explained the plot in the third person, spending both arcs'
/// capstone reveals at a bar neither capstone could reach, and closing with a button that told the player
/// how to feel about it.</para>
///
/// <para>What it must be instead: the berth-holder's sentence, the adjuster's sentence, one above the
/// other, unannotated — nobody named, nothing joining them — and one closing line that explains nothing.
/// Both sentences are already in the captain's ledger when it opens, which is the thing the bar's named
/// shards (<see cref="ArcConvergence.KaamosSideShard"/>, <see cref="ArcConvergence.NebulaSideShard"/>)
/// exist to make true.</para>
/// </summary>
public class TheConvergenceCardIsACollisionTests
{
    /// <summary>The cheat, as docs/testing-guide.md Appendix A documents it: one URL, cold boot, the
    /// marquee beat on the screen. A scene nobody can reach on demand is a scene that ships broken.</summary>
    private const string TheCheat = "/map?converge=1";

    private static async Task<DeskBench.Painted.Node> TheCardAsync(DeskBench bench)
    {
        DeskBench.Painted painted = await bench.RenderAsync();
        return painted.Root.Descendants().FirstOrDefault(n => n.HasClass("convergence-card") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{TheCheat} booted and drew no .convergence-card. Either the cheat no longer seeds enough "
                + "of both arcs, or the joint bar moved out from under it — and the marquee beat is now "
                + "unreachable on demand, which is how it ships broken.");
    }

    // ── 1 · The card is the two voices the captain is carrying. ──

    [Fact]
    public async Task TheCheatOpensTheCardAndTheCardIsBothVoices()
    {
        using var bench = await DeskBench.BootAsync(TheCheat);
        DeskBench.Painted.Node card = await TheCardAsync(bench);

        Assert.Contains(KaamosLore.HolderConvergenceLine, card.Spoken, StringComparison.Ordinal);
        Assert.Contains(NebulaLore.AdjusterConvergenceLine, card.Spoken, StringComparison.Ordinal);
        Assert.Contains(ArcConvergence.ConvergenceReveal, card.Spoken, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheHoldersLineComesFirstAndTheClosingLineComesLast()
    {
        // The order is the argument. The KAAMOS voice, then the NEBULA voice, then — after both — the one
        // line the card says for itself. A closing line that arrived first would be a headline again.
        using var bench = await DeskBench.BootAsync(TheCheat);
        DeskBench.Painted.Node card = await TheCardAsync(bench);

        int holder = card.Spoken.IndexOf(KaamosLore.HolderConvergenceLine, StringComparison.Ordinal);
        int adjuster = card.Spoken.IndexOf(NebulaLore.AdjusterConvergenceLine, StringComparison.Ordinal);
        int closing = card.Spoken.IndexOf(ArcConvergence.ConvergenceReveal, StringComparison.Ordinal);

        Assert.True(holder < adjuster, "the adjuster speaks before the berth-holder");
        Assert.True(adjuster < closing, "the card closes before both voices have spoken");
    }

    [Fact]
    public async Task NobodyIsNamedOverEitherLine()
    {
        // UNANNOTATED is the word in the ruling. No speaker, no arc, no port, no "meanwhile" — the two
        // sentences and nothing between them. If the card ever labels who is talking it has gone back to
        // doing the player's arithmetic for them.
        using var bench = await DeskBench.BootAsync(TheCheat);
        DeskBench.Painted.Node card = await TheCardAsync(bench);

        foreach (string label in new[]
                 {
                     "berth-holder", "berth holder", "holder", "adjuster", "Nebula", "KAAMOS", "Vantar",
                     "Two mysteries", "THE THREADS CROSS",
                 })
        {
            Assert.DoesNotContain(label, card.Spoken, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── 2 · It states none of the three facts the old reveal stated. ──

    [Theory]
    // ① the resurrection is a fresh copy that wakes certain it was always you
    [InlineData("copy")]
    [InlineData("here first")]
    // ② Nebula keeps the ORIGINALS; the premium buys storage; you are the collateral
    [InlineData("original")]
    [InlineData("premium")]
    [InlineData("storage")]
    [InlineData("pattern")]
    [InlineData("withdrawal")]
    // ③ the archive is awake — the same wintering, scaled to a subscriber base
    [InlineData("archive")]
    [InlineData("held breath")]
    [InlineData("lucid")]
    [InlineData("wintering")]
    // …and the Old Ones are not in this card, were never in this card, and are not coming into it.
    [InlineData("Old One")]
    [InlineData("Reever")]
    public async Task TheCardDoesNotStateWhatTheOldRevealStated(string claim)
    {
        using var bench = await DeskBench.BootAsync(TheCheat);
        DeskBench.Painted.Node card = await TheCardAsync(bench);

        Assert.DoesNotContain(claim, card.Spoken, StringComparison.OrdinalIgnoreCase);
    }

    // ── 3 · Both sentences really are in the ledger when it opens. ──

    [Fact]
    public async Task TheCaptainIsReallyCarryingBothOfThese()
    {
        // THE AUDIT. The card closes on "You have been carrying both of these for a while", so the sim has
        // to agree: both quoted shards assembled, on the very edge the card fires. The bar is what makes
        // this true in real play; the cheat has to honour the same bar or it proves nothing about the beat
        // a captain reaches by playing.
        using var bench = await DeskBench.BootAsync(TheCheat);
        await TheCardAsync(bench);

        var kaamos = (KaamosProgress)bench.Peek("_kaamos")!;
        var nebula = (NebulaProgress)bench.Peek("_nebula")!;

        Assert.True(kaamos.Has(ArcConvergence.KaamosSideShard),
            "the card quotes the berth-holder's tell at a captain who never met the berth-holder");
        Assert.True(nebula.Has(ArcConvergence.NebulaSideShard),
            "the card quotes the adjuster's tell at a captain who never met the adjuster");

        // …and the bar did not quietly grow to pay for it: still exactly 3 + 3.
        Assert.Equal(ArcConvergence.KaamosSideThreshold, kaamos.IntelAssembled);
        Assert.Equal(ArcConvergence.NebulaSideThreshold, nebula.IntelAssembled);
    }
}

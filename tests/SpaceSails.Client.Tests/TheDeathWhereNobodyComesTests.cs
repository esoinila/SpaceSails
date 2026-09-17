using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #640 · THE DEATH WHERE NOBODY COMES, read off the painted card (owner ruling 2026-09-17, option A).
///
/// <para>The Core guards hold the rules; these hold the SCENE, because the whole reason
/// <see cref="ArchiveNode.NoRestoreLine"/> sat authored and unread for two months is that a card saying
/// POLICY CLOSED over a sim that then resurrects you is this project's most expensive bug class. So every
/// test here boots the real page at the documented cheat, presses the real button a player presses, and
/// reads what came back — and the control below presses the SAME button on the SAME death without the
/// closed policy and gets the clinic, which is what makes this guard able to tell pass from fail.</para>
/// </summary>
[SlowGate] // #640 · 23 s over 6 test(s), measured 2026-09-17; see TheSlowGateRosterTests.
public sealed class TheDeathWhereNobodyComesTests
{
    /// <summary>The documented door: a captain whose lineage already pulled the handle on their own jar,
    /// killed on her own deck. <c>docs/testing-guide.md</c> Appendix A and <c>DevStarts</c> both carry it.</summary>
    private const string TheLastLife = "/map?nopattern=1&death=impact";

    /// <summary>The same death, on a captain Nebula still has on file. The control.</summary>
    private const string AnOrdinaryDeath = "/map?death=impact";

    private static DeskBench.Painted.Node TheCard(DeskBench.Painted painted, string url) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass("busted-card") && !n.Hidden)
        ?? throw new Xunit.Sdk.XunitException(
            $"{url} booted and drew no .busted-card — the death did not stage, so nothing below is about "
            + "what it claims to be about.");

    /// <summary>Boot, find the freeze-frame's own way out, and press it. That press is "…wake up", the one
    /// verb every death offers, and it is the exact edge #640 changes: on an ordinary captain it leads to a
    /// clinic, and on this one it leads nowhere.</summary>
    private static async Task<DeskBench.Painted> WakeUpAsync(DeskBench bench, string url)
    {
        DeskBench.Painted.Node card = TheCard(await bench.RenderAsync(), url);
        DeskBench.Painted.Node wake = card.Descendants()
            .FirstOrDefault(n => n.HasClass("busted-close") && !n.Hidden && n.Handlers.ContainsKey("onclick"))
            ?? throw new Xunit.Sdk.XunitException($"{url}: the death card has no way out to press.");

        await bench.PressAsync(wake.Handlers["onclick"]);
        return await bench.RenderAsync();
    }

    // ── 1 · What the card reads. ──

    [Fact]
    public async Task TheCardReadsTheLineThatHadNeverBeenRead()
    {
        using var bench = await DeskBench.BootAsync(TheLastLife);
        DeskBench.Painted.Node card = TheCard(await WakeUpAsync(bench, TheLastLife), TheLastLife);

        // VERBATIM, and from Core's own const — the card may not re-type it or trim it.
        Assert.Contains(ArchiveNode.NoRestoreLine, card.Spoken, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NothingOnTheCardSaysAnybodyCame()
    {
        // The sentence says the clinic's welcome loop does not play and nobody comes. If the panel beside
        // it were still printing a clinic, a bill, a hull or a successor's name, the card would be arguing
        // with itself — which is precisely the shape this beat waited two months to avoid.
        using var bench = await DeskBench.BootAsync(TheLastLife);
        DeskBench.Painted.Node card = TheCard(await WakeUpAsync(bench, TheLastLife), TheLastLife);

        foreach (string absent in new[]
                 {
                     "Clinic bill", "clinic bill", "rustbucket", "Welcome back",
                     "takes the license", "wake up",
                 })
        {
            Assert.DoesNotContain(absent, card.Spoken, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task TheOrdinaryDeathStillWakesUpAtAClinic()
    {
        // THE CONTROL, and the reason the two above mean anything: the same press, the same death, a
        // captain Nebula still has on file. Nothing about this path changed.
        using var bench = await DeskBench.BootAsync(AnOrdinaryDeath);
        DeskBench.Painted.Node card = TheCard(await WakeUpAsync(bench, AnOrdinaryDeath), AnOrdinaryDeath);

        Assert.DoesNotContain(ArchiveNode.NoRestoreLine, card.Spoken, StringComparison.Ordinal);
        Assert.Contains("Clinic", card.Spoken, StringComparison.OrdinalIgnoreCase);
        Assert.False((bool)bench.Peek("_threadIsOver")!);
    }

    // ── 2 · What the sim did, on the same press. ──

    [Fact]
    public async Task ThePenGoesDownAndTheThreadIsClosed()
    {
        using var bench = await DeskBench.BootAsync(TheLastLife);
        await WakeUpAsync(bench, TheLastLife);

        // The autosave stops: nothing written after this death can stamp the registry and hand the front
        // door a run with no captain in it.
        Assert.True((bool)bench.Peek("_threadIsOver")!,
            "the captain died with no pattern on file and the autosave is still running");

        // The other half — that the registry row is marked ended, that Continue stops leading there and
        // that no other universe moves — is pinned in Core, by TheRunEndsWhenThereIsNoPatternOnFileTests,
        // and it is pinned there rather than here for a reason worth writing down: the thread index lives
        // in localStorage behind a JSImport, and this bench has no browser, so a registry call in here
        // would throw rather than assert. What THIS guard is for is that the sim reached the closing at
        // all and does not write another byte afterwards.
    }

    [Fact]
    public async Task NobodyIsIssuedALicence()
    {
        // A succession writes two names onto the encounter — the retiring captain's and the successor's —
        // and the card prints them side by side under a portrait. On this death neither is written, because
        // the succession never runs, and the panel that would draw them is never reached.
        using var bench = await DeskBench.BootAsync(TheLastLife);
        DeskBench.Painted.Node card = TheCard(await WakeUpAsync(bench, TheLastLife), TheLastLife);

        Assert.True(card.Descendants().All(n => !n.HasClass("busted-succession-names")),
            "a successor was named on the card of a captain nobody came for");
        Assert.True(card.Descendants().All(n => !n.HasClass("succ-portrait")),
            "a new face was painted onto the card of a captain nobody came for");
    }

    // ── 3 · The way off the last card. ──

    [Fact]
    public async Task TheLastCardCanBeClosedAndItOpensTheFrontDoor()
    {
        // The general UI law (owner 2026-08-24): no pop-up that cannot be closed. This one has exactly one
        // control, it is not "…wake up" because nobody does, and what it opens is the whole shelf rather
        // than the ship's own drawer — there is no ship to go back to.
        using var bench = await DeskBench.BootAsync(TheLastLife);
        DeskBench.Painted.Node card = TheCard(await WakeUpAsync(bench, TheLastLife), TheLastLife);

        DeskBench.Painted.Node[] ways = [.. card.Descendants()
            .Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick") && n.Name.Length > 0)];
        Assert.Single(ways);

        await bench.PressAsync(ways[0].Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(after.Root.Descendants().All(n => !n.HasClass("busted-card") || n.Hidden),
            "the last card was pressed and is still on the screen");
        Assert.True((bool)bench.Peek("_showStartPicker")!,
            "the run ended and the player was left looking at a map with nobody flying it");
    }
}

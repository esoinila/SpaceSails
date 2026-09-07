using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 · <b>THE COLLECTOR'S DEMAND — THE ByDecision MODE, ON THE SURFACE IT WAS WRITTEN FOR</b>, and the
/// plumbing the whole class draws on.
///
/// <para>What this part owns is the one surface that is allowed to offer no way out but an answer: the
/// busted panel is a <c>ByDecision</c> shell, every answer on it advances the world, and every one of those
/// chains ends in a close — which is how a modal that cannot be dismissed still obeys the owner's
/// 2026-08-24 ruling.</para>
/// </summary>
public sealed partial class TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests
{
    // ── The collector's demand: the ByDecision mode, on the surface it was written for ────────────────

    /// <summary>
    /// NO ✕ ON THE DEMAND, AND THAT IS THE POINT OF THE MODE.
    ///
    /// <para>A captain who has been grappled answers the collector. What the shell adds is that the absence
    /// is DECLARED — <c>ByDecision</c> draws no dismiss and audits the shape — rather than being the state a
    /// card is in because nobody wrote one, which is the difference #992 was written to find.</para>
    /// </summary>
    [Fact]
    public async Task TheCollectorsDemandIsAByDecisionShellAndOffersNoWayOutButAnAnswer()
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        StageTheDemand(bench, "Demand");

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = TheBustedCard(painted)
            ?? throw new Xunit.Sdk.XunitException("staging the demand drew nothing wearing .busted-card.");

        Assert.True(card.HasClass("overlay-shell-bare"),
            "the BUSTED panel is not the shell's Bare frame. #735 pins `.busted-card > .busted-options` and "
            + "the close row; a wrapper between the card and them unsticks both.");

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-dismiss"));

        var answers = card.Descendants()
            .Where(n => n.Handlers.ContainsKey("onclick") && !n.Hidden)
            .Select(n => n.Name)
            .Where(spoken => spoken.Length > 0)
            .ToList();

        Assert.True(answers.Count >= 3,
            $"the demand offers {answers.Count} control(s). SUBMIT, BRIBE and RESIST are the panel, and a "
            + "ByDecision surface with fewer answers than it has is a surface with no way out at all.");
    }

    /// <summary>
    /// EVERY ANSWER TURNS THE PAGE, AND EVERY CHAIN ENDS IN A CLOSE.
    ///
    /// <para><b>This is the finding, proved.</b> #992's register had this panel down as
    /// <c>EveryControlCloses</c> — <i>"allowed no ✕ only because every answer it offers is itself a
    /// close"</i> — and, being undriven, nothing had ever pressed one. Not one of the three closes it.
    /// SUBMIT goes to Confiscated, BRIBE to BribedOff, RESIST to a won roll, a lost one or the Bolivia. What
    /// is true is that each is a STAGE — which is what <c>Restages</c> says by name — and that following any
    /// of them lands on a card whose single control really does end it.</para>
    ///
    /// <para>So the assertion is the honest one and it is stronger than the flag it replaces: press an
    /// answer, the world MOVED (the phase is not the phase it was), and pressing on gets out. A chain that
    /// stopped moving would spin here and fail by the ceiling rather than hang.</para>
    /// </summary>
    [Theory]
    [InlineData("SUBMIT")]
    [InlineData("BRIBE")]
    [InlineData("RESIST")]
    public async Task EveryAnswerOnTheDemandAdvancesTheWorldAndItsChainEndsInAClose(string answer)
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        StageTheDemand(bench, "Demand");

        var offered = TheBustedCard(await bench.RenderAsync())!
            .Descendants()
            .Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick") && n.Name.Length > 0)
            .ToList();

        DeskBench.Painted.Node? button = offered
            .FirstOrDefault(n => n.Name.Contains(answer, StringComparison.Ordinal));

        Assert.True(button is not null,
            $"the demand has no control reading \"{answer}\" — it offers "
            + $"[{string.Join(" · ", offered.Select(n => n.Name.Split('\n')[0]))}]. The three answers ARE "
            + "the panel; if one has been renamed, this guard's name for it must move with it.");

        string before = PhaseOf(bench);
        await bench.PressAsync(button!.Handlers["onclick"]);
        await bench.RenderAsync();

        Assert.NotEqual(before, PhaseOf(bench));

        // …and now out. Every stage past the demand offers a control; press the LAST one on the card (the
        // close row is the foot of every one of them) until the panel is gone.
        for (int press = 0; press < 8 && bench.Field("_busted") is not null; press++)
        {
            DeskBench.Painted.Node? card = TheBustedCard(await bench.RenderAsync());
            if (card is null)
            {
                break;
            }

            DeskBench.Painted.Node? on = card.Descendants()
                .LastOrDefault(n => !n.Hidden
                                    && n.Handlers.ContainsKey("onclick")
                                    && n.Name.Length > 0
                                    && !n.Name.Contains("Load a saved voyage", StringComparison.Ordinal));

            Assert.True(on is not null,
                $"the {PhaseOf(bench)} stage has no control on it at all. It is drawn, it is on top, and "
                + "there is no way out of it — the shape the owner's ruling forbids, on the one panel that "
                + "is allowed no ✕.");

            await bench.PressAsync(on!.Handlers["onclick"]);
            await bench.RenderAsync();
        }

        Assert.True(bench.Field("_busted") is null,
            $"pressing \"{answer}\" started a chain that never ends. A ByDecision surface earns its missing "
            + "✕ by every answer being a way out — through however many stages, but out. This one is still "
            + $"on the screen at the {PhaseOf(bench)} stage after eight presses.");
    }

    private static string PhaseOf(DeskBench bench)
    {
        object busted = bench.Field("_busted")!;
        return busted is null
            ? "(gone)"
            : busted.GetType().GetProperty("Phase")!.GetValue(busted)!.ToString()!;
    }

    private static DeskBench.Painted.Node? TheBustedCard(DeskBench.Painted painted) =>
        painted.Root.Descendants().FirstOrDefault(n => n.HasClass("busted-card") && !n.Hidden);

    /// <summary>
    /// PUT A COLLECTOR'S DEMAND ON THE SCREEN.
    ///
    /// <para>The register's stated reason for leaving this row undriven was that <c>_busted</c> is a staged
    /// record built by the combat lane and that <i>"the stage machine, not the gate, is what would have to be
    /// stood up"</i>. Half of that is right and it is the half that matters here: the STAGE MACHINE is
    /// exactly what this file wants to drive, so it is stood up — the record is built with the same fields
    /// <c>ApplyHunterCatch</c> gives it (a callsign, a heat level, a folded seed and a bribe demand rolled by
    /// Core's own <see cref="BustedRule.BribeDemand"/>), and everything past that press is the shipping
    /// handler, the shipping dice and the shipping markup.</para>
    /// </summary>
    internal static void StageTheDemand(DeskBench bench, string phase)
    {
        Type card = typeof(Map).GetNestedType("BustedEncounter", BindingFlags.NonPublic | BindingFlags.Public)
                    ?? throw new InvalidOperationException(
                        "Map.BustedEncounter is gone. The BUSTED panel's own state used to be called that; "
                        + "this guard cannot stage a demand without it.");

        object bust = Activator.CreateInstance(card, nonPublic: true)!;
        const ulong seed = 0xB0_57_ED_11UL;
        Set(card, bust, "HunterId", "collector-1");
        Set(card, bust, "HunterCallsign", "VULTURE ACTUAL");
        Set(card, bust, "Heat", 2);
        Set(card, bust, "Seed", seed);
        Set(card, bust, "Bribe", BustedRule.BribeDemand(2, seed));
        Set(card, bust, "Phase", Enum.Parse(card.GetNestedType("Stage")!, phase));

        bench.Poke("_showSaveDrawer", false);
        bench.Poke("_busted", bust);
    }

    private static void Set(Type card, object bust, string name, object value) =>
        card.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.SetValue(bust, value);

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private const string FreeFlying = "/map?start=wreck";
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

    private static IEnumerable<string> RazorFiles() =>
        Directory.EnumerateFiles(ClientSource(), "*.razor", SearchOption.AllDirectories);

    private static string ClientSource()
    {
        var at = new DirectoryInfo(AppContext.BaseDirectory);
        while (at is not null)
        {
            string candidate = Path.Combine(at.FullName, "src", "SpaceSails.Client");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            at = at.Parent;
        }

        throw new DirectoryNotFoundException(
            "src/SpaceSails.Client is not above the test binary — this guard reads the markup as typed and "
            + "cannot do its job without it.");
    }
}

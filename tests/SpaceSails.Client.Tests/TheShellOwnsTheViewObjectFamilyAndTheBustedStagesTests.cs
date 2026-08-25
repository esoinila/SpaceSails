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
/// #997 wave 3 · <b>THE SHELL OWNS THE CARD FAMILY, AND THE COLLECTOR'S DEMAND.</b>
///
/// <para>The dismissibility law (<see cref="EveryPopUpCanBeDismissedTests"/>) asks whether a surface can be
/// got rid of. What a MIGRATION can break is narrower and this file asks that instead, in two halves.</para>
///
/// <list type="number">
/// <item><b>The family.</b> Twelve cards in this client are rooted on <c>.view-object</c>, and #735's law
/// pins their action row to the bottom of the scrollport with <c>::deep .view-object &gt;
/// .view-object-close</c> — a DIRECT-child relation. Ten of them hand-rolled that button until this wave.
/// The family guard reads the markup as typed and requires every one of them to be drawn through the shell,
/// so a THIRTEENTH card that hand-rolls the foot fails here with its own file and line rather than shipping
/// a card whose way out has quietly unstuck.</item>
/// <item><b>The demand.</b> <see cref="SpaceSails.Client.Components.OverlayDismiss.ByDecision"/> is the one
/// mode #997 shipped that no shipping surface had ever been drawn through. The BUSTED panel is the surface
/// it was written for, and driving it found what #997 found on the rep's card: the register's claim about it
/// was false. Its three answers do not close it — they turn its page. Every chain still ENDS in a close, and
/// that is proved here by following each one to the end rather than by believing a flag.</item>
/// </list>
/// </summary>
public sealed class TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests
{
    // ── The family, read off the markup as typed ──────────────────────────────────────────────────────

    /// <summary>
    /// EVERY <c>.view-object</c> CARD IN THE CLIENT IS DRAWN THROUGH THE SHELL.
    ///
    /// <para>Read as TYPED, in #992's own idiom and for its reason: a card that named itself through a
    /// parameter would vanish from a guard that reads <c>class="…"</c>, so the rule is that the class stays a
    /// lowercase attribute and this walks them all.</para>
    ///
    /// <para><b>Two questions, because two things can go wrong.</b> A new card can be typed as a plain
    /// <c>&lt;div&gt;</c> (the family grows a thirteenth hand-rolled foot), or a card that HAS the shell can
    /// grow a SECOND, hand-written way out inside it — which leaves two buttons doing one job, only one of
    /// them pinned and only one of them the one the shell's audit watches.</para>
    /// </summary>
    [Fact]
    public void EveryViewObjectCardInTheClientIsDrawnThroughTheShell()
    {
        var handRolled = new List<string>();
        var twoFeet = new List<string>();

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string shortName = Path.GetFileName(file);
            int insideAShell = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                bool opensAShell = lines[i].Contains("<OverlayShell", StringComparison.Ordinal);

                foreach (Match attribute in Regex.Matches(lines[i], "class=\"([^\"]*)\""))
                {
                    string[] classes = attribute.Groups[1].Value
                        .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                    string tag = TagOwning(lines, i, attribute.Index);

                    // The card root: the family's own class, on the element the foot hangs off.
                    if (classes.Contains("view-object", StringComparer.Ordinal)
                        && !string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
                    {
                        handRolled.Add($"{shortName}:{i + 1}  <{tag} class=\"{attribute.Groups[1].Value}\">");
                    }

                    // A foot typed INSIDE a shell. Outside one it belongs to a card of another root — the
                    // vent boards, the satchel, the lift panel — which this wave did not migrate and which
                    // TheOtherCardsWearingTheFamilysFootOnlyEverGetFewer counts instead.
                    if (insideAShell > 0
                        && classes.Contains("view-object-close", StringComparer.Ordinal)
                        && !string.Equals(tag, "OverlayShell", StringComparison.Ordinal))
                    {
                        twoFeet.Add($"{shortName}:{i + 1}  <{tag} class=\"{attribute.Groups[1].Value}\">");
                    }
                }

                insideAShell += opensAShell ? 1 : 0;
                insideAShell -= CountOf(lines[i], "</OverlayShell>");
            }
        }

        Assert.True(handRolled.Count == 0,
            $"{handRolled.Count} card(s) rooted on .view-object are NOT drawn through OverlayShell:\n  - "
            + string.Join("\n  - ", handRolled)
            + "\n\nThe family's action row is pinned by `::deep .view-object > .view-object-close`, which "
            + "needs the way out to be a DIRECT child of the card. The shell guarantees that with "
            + "Frame=\"OverlayFrame.Bare\"; a hand-rolled card guarantees it only until somebody wraps "
            + "something in it. Give it a shell (#997 wave 3) — or, if it genuinely is not a card of this "
            + "family, do not give it the family's class.");

        Assert.True(twoFeet.Count == 0,
            $"{twoFeet.Count} .view-object-close button(s) are typed by hand INSIDE a shell:\n  - "
            + string.Join("\n  - ", twoFeet)
            + "\n\nThe foot is the shell's now (`DismissClass=\"view-object-close\"`). A hand-written one "
            + "beside it is two ways out on one card, and the shell's audit only knows about its own.");
    }

    /// <summary>
    /// THE REST OF THE FAMILY'S FOOT-WEARERS ONLY EVER GET FEWER.
    ///
    /// <para>Fifteen buttons in this client wear <c>.view-object-close</c> on a card that is NOT rooted on
    /// <c>.view-object</c>: the vent boards (the atmosphere panel, the scuttling panel, her own hull, the
    /// charge board, the pressure door, the operating log, the epitaph), the satchel and its wallet fan, the
    /// lift's car panel, the locked door, the treasure map and the bar table. They borrow the family's button
    /// and its wording without borrowing its root, so #735's sticky foot never reached them and the shell has
    /// nothing to hang off yet — each needs its own card class taken through the migration, which is a
    /// separate wave with its own before-and-after.</para>
    ///
    /// <para>Written down rather than left implicit, in this register's own idiom: anybody may make the
    /// number smaller, and making it bigger costs an edit to this line.</para>
    /// </summary>
    [Fact]
    public void TheOtherCardsWearingTheFamilysFootOnlyEverGetFewer()
    {
        int borrowed = RazorFiles()
            .SelectMany(File.ReadAllLines)
            .Sum(line => Regex.Matches(line, "class=\"([^\"]*)\"")
                .Count(m => m.Groups[1].Value
                    .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Contains("view-object-close", StringComparer.Ordinal)));

        Assert.True(borrowed <= 15,
            $"{borrowed} buttons wear .view-object-close on a card of another root, and the written-down "
            + "ceiling is 15. Migrating one of those cards onto the shell makes this number smaller; a NEW "
            + "card borrowing the family's button without its root makes it bigger, and that is the moment "
            + "to ask whether it should have the family's root instead.");
    }

    private static int CountOf(string line, string needle)
    {
        int found = 0;
        for (int at = line.IndexOf(needle, StringComparison.Ordinal);
             at >= 0;
             at = line.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }

    /// <summary>The element a <c>class="…"</c> belongs to: the nearest <c>&lt;</c> at or before it, looking
    /// back up the file when the attribute sits on its own line (this codebase writes long tags across
    /// several).</summary>
    private static string TagOwning(string[] lines, int line, int column)
    {
        string head = lines[line][..column];
        for (int back = line; back >= 0 && line - back < 8; back--)
        {
            int open = head.LastIndexOf('<');
            if (open >= 0)
            {
                Match named = Regex.Match(head[(open + 1)..], @"^[A-Za-z][\w.]*");
                return named.Success ? named.Value : "?";
            }

            if (head.Contains('>'))
            {
                break;   // the tag before this one closed: the attribute is loose text, not an element's
            }

            head = back > 0 ? lines[back - 1] : "";
        }

        return "?";
    }

    // ── The family, read off what was actually drawn ──────────────────────────────────────────────────

    /// <summary>
    /// …AND THE FOOT IS A DIRECT CHILD OF THE CARD, ON EVERY ONE THE BENCH CAN RAISE.
    ///
    /// <para>The guard above proves the SHAPE of the markup; this proves the DOM the shape produces, which is
    /// the thing #735's selector actually reads. A shell drawn <c>Hosted</c> instead of <c>Bare</c> would
    /// pass the guard above word for word and put a <c>display: contents</c> div between the card and its
    /// button — correct-looking markup, a silently unstuck foot (#997 wave 2 §2).</para>
    ///
    /// <para>Five of the twelve, and the other seven are named rather than skipped: the archive vision, the
    /// castaway, the wreck look, the kiosk card, the souvenir, the rep's pitch and the walk-in are gated on
    /// records this bench cannot build off-browser (a dice throw against a wreck, a walk up to a prop, a
    /// crossing's landing frame). They are covered by the source guard above and — for the last two — by
    /// #997 wave 2's own file.</para>
    /// </summary>
    [Theory]
    [InlineData("the story / reveal card", Docked, "_storyCard")]
    [InlineData("the captain's remote", Ashore, "_showCaptainsRemote")]
    [InlineData("the door board", Ashore, "_showDoorBoard")]
    [InlineData("the shape alarm panel", Ashore, "_showAlarmPanel")]
    [InlineData("the selfie", Ashore, "_selfieShot")]
    public async Task EveryViewObjectCardTheBenchCanRaiseWearsTheShellsOwnFoot(
        string name, string world, string gate)
    {
        using DeskBench bench = await DeskBench.BootAsync(world);
        RaiseTheCard(bench, gate);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("view-object") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: setting {gate} drew nothing wearing .view-object. The driver and the markup's gate "
                + "have come apart; one of them has moved.");

        Assert.True(card.HasClass("overlay-shell"),
            $"{name}: its card is on the screen and it is not the shell's — #997 wave 3 put every card in "
            + "this family on OverlayShell, and this one has come back off it.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            $"{name}: the shell drew it as something other than Bare. .view-object is a flex column whose "
            + "foot is pinned by `::deep .view-object > .view-object-close`, so any frame that contributes a "
            + "wrapper (or, worse, Hosted's `display: contents`) unsticks the way out while the markup goes "
            + "on looking right.");

        DeskBench.Painted.Node foot = card.Children.FirstOrDefault(n => n.HasClass("view-object-close"))
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its way out is not a DIRECT child of the card. That is the exact relation #735's "
                + "sticky foot is written against — something has come between them.");

        Assert.True(foot.Handlers.ContainsKey("onclick"),
            $"{name}: the way out is drawn and nothing is wired to it — a control that LOOKS like a way out "
            + "and is not one, which is what the owner's ruling of 2026-08-24 forbids.");

        await bench.PressAsync(foot.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();
        Assert.DoesNotContain(after.Root.Descendants(), n => n.HasClass("view-object") && !n.Hidden);
    }

    private static void RaiseTheCard(DeskBench bench, string gate) => bench.Poke(gate, gate switch
    {
        "_storyCard" => ((StoryBeats.Beat Beat, string? Subject, string? Outcome)?)
            (StoryBeats.Beat.BerthGreatPort, "selene-gate", null),
        "_selfieShot" => new CapturedSelfie(
            "spot-the-tilt", "THE CAPTAIN, HERE", "Nobody will believe it. That is the point.",
            "art/selfie-the-tilt.jpg", 1, 12, "spot"),
        _ => true,
    });

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
        Type card = typeof(Map).GetNestedType("BustedEncounter", BindingFlags.NonPublic)
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

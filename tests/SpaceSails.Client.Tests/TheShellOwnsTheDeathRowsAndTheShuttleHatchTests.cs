using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Components;
using SpaceSails.Client.Pages;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 7 · <b>THE DEATH PANELS' CLOSE ROWS, THE SHUTTLE HATCH, AND ONE ROW MOVED BETWEEN BOOKS.</b>
///
/// <para>Wave 6 spent its care on the start-picker family — the save surface — and left the OTHER door onto
/// it named for this wave: since #951 every one of the four death panels carries <i>📖 Load a saved
/// voyage</i> beside <i>…wake up</i>, in a two-button row typed by hand. The card root has been the shell's
/// since wave 3 (#1001); the row was the last hand-rolled thing on it, and dying is not rare, so this is
/// the way out the most players meet.</para>
///
/// <h3>What the shell had to learn, and what it deliberately did not</h3>
/// <para>#1007 wrote the condition down: a straggler <i>"gets a shell when the shell learns to hand a page's
/// own foot the dismiss it draws"</i>. Both halves of that landed here, and they are ONE idea — the shapes
/// this migration could not reach were the ones whose way out sits in a ROW.</para>
/// <list type="bullet">
///   <item><c>OnBeside</c> + <c>Beside…</c> — a SECOND way out of a Bare foot, with its own verb, its own
///   face and its own class list. Not the dossier's <c>OnClose</c>, which is the second verb on a Minimize
///   card and wired to the same close its dismiss is.</item>
///   <item><c>WaysClass</c> — the page's own name for the row the shell draws them into. Left unnamed with
///   one way out, nothing is drawn around it and the dismiss is still the card's last direct child, which is
///   what twenty-odd sticky feet are pinned to.</item>
/// </list>
///
/// <para><b>Not learned: a per-answer face on <c>Choices</c>.</b> Routing these rows through the ByDecision
/// answer list was the obvious move and it is the wrong one twice over. The busted card is ONE shell across
/// four stages, so its <c>Choices</c> would have to be computed from the phase — which takes the wiring out
/// of the <c>case</c> arm that #970's guards read, and a refactor that has to edit the guards watching it is
/// a refactor nobody is watching. And <c>ChoiceClass</c> is one class list for every answer, while this row
/// wears two (<c>btn-light</c> and <c>btn-outline-light</c>) — repainting five buttons is a change to the
/// screen. #1001 gave that second reason first; wave 7 keeps it.</para>
/// </summary>
[SlowGate] // #251 · 53 s over 18 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheShellOwnsTheDeathRowsAndTheShuttleHatchTests
{
    /// <summary>The four panels a death can end on — #970's own list, and the four rows this wave moved.
    /// Each pairs the stage with the face of its FIRST way out, which is the word on the screen and the
    /// thing a rename would have to move through this file rather than round it.</summary>
    public static TheoryData<string, string> TheDeathPanels => new()
    {
        { "FreezeFrame", "…wake up" },
        { "Impact", "…wake up" },
        { "SurfaceEnd", "…wake up" },
        { "Resurrected", "Board the rustbucket" },
    };

    private const string TheShelf = "📖 Load a saved voyage";

    // ── The rows, read off the markup as typed ────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY <c>.busted-close-row</c> IN THE CLIENT IS AN <c>&lt;OverlayShell&gt;</c>, AND THERE ARE FOUR.
    ///
    /// <para>Read as typed, in #992's idiom and for its reason: the class stays a lowercase attribute so the
    /// dismissibility law can go on seeing it. A fifth death panel typed with a hand-rolled row fails here
    /// by file and line rather than shipping a way out the shell's audit knows nothing about.</para>
    /// </summary>
    [Fact]
    public void EveryDeathPanelsCloseRowIsDrawnThroughTheShell()
    {
        var handRolled = new List<string>();
        int rows = 0;

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (Match attribute in Regex.Matches(lines[i], "class=\"([^\"]*)\""))
                {
                    if (!attribute.Groups[1].Value
                            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
                            .Contains("busted-close-row", StringComparer.Ordinal))
                    {
                        continue;
                    }

                    rows++;
                    if (!lines[i].Contains("<OverlayShell", StringComparison.Ordinal))
                    {
                        handRolled.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");
                    }
                }
            }
        }

        Assert.True(handRolled.Count == 0,
            $"{handRolled.Count} death-panel close row(s) are typed by hand:\n  - "
            + string.Join("\n  - ", handRolled)
            + "\n\nThe row is the shell's now: Frame=\"OverlayFrame.Bare\", the ✕ verb on OnClose and the "
            + "logbook on OnBeside. A hand-rolled one is two ways out on the panel every dying player "
            + "meets, and the shell's audit only knows about its own.");

        Assert.True(rows == 4,
            $"{rows} close rows wear .busted-close-row and #970's four death panels are FreezeFrame, "
            + "Impact, SurfaceEnd and Resurrected. If a fifth panel has been added it needs a case in "
            + "TheDeathPanels below, which is what actually presses it; if one has gone, #970 will say so "
            + "first and this number follows it.");
    }

    // ── The rows, read off what was actually drawn, and pressed ───────────────────────────────────────

    /// <summary>
    /// THE ROW IS THE SHELL'S, AND IT IS STILL THE SAME TWO BUTTONS.
    ///
    /// <para>Every parameter this migration hands the shell is a promise about the screen, so each is read
    /// back off the render tree rather than off the tag: the row wears its own name and the shell's Bare
    /// frame, it holds exactly two controls, the first reads the panel's own verb and the second reads
    /// #951's shelf, and the shelf still carries the tooltip #951 wrote for it.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheDeathPanels))]
    public async Task EachDeathPanelsRowIsTheShellsAndCarriesBothWaysOut(string stage, string face)
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.StageTheDemand(bench, stage);

        DeskBench.Painted.Node row = await TheCloseRow(bench, stage);

        Assert.True(row.HasClass("overlay-shell") && row.HasClass("overlay-shell-bare"),
            $"the {stage} panel's close row is on the screen and it is not the shell's. Wave 7 put all four "
            + "death rows on OverlayShell; this one has come back off it.");

        var controls = row.SelfAndDescendants()
            .Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick") && n.Name.Length > 0)
            .ToList();

        Assert.True(controls.Count == 2,
            $"the {stage} panel's row offers {controls.Count} control(s) — "
            + $"[{string.Join(" · ", controls.Select(n => n.Name))}]. #951's ruling is that a death offers "
            + "two true answers: wake up in the clinic, or board a moment you banked.");

        Assert.Equal(face, controls[0].Name);
        Assert.Equal(TheShelf, controls[1].Name);

        Assert.True(controls[0].HasClass("overlay-shell-dismiss") && controls[0].HasClass("busted-close"),
            "the panel's own verb is the shell's dismiss and it must keep the class the page's CSS reaches "
            + "it by (.busted-close un-sticks itself inside a row; without it the button carries the "
            + "family's sticky foot a second time, over the row that already has it).");

        Assert.True(controls[1].HasClass("overlay-shell-beside") && controls[1].HasClass("busted-logbook"),
            "the shelf is the shell's second way out and must keep .busted-logbook — the rule that makes it "
            + "legible against the sepia backdrop, which btn-outline-light alone was not.");

        Assert.Contains("Open the ship's logbook", controls[1].Attributes.GetValueOrDefault("title") ?? "",
            StringComparison.Ordinal);
    }

    /// <summary>
    /// PRESSING THE PANEL'S OWN VERB STILL TURNS THE PAGE — OR ENDS IT.
    ///
    /// <para>The half of the migration a parameter cannot promise. <c>…wake up</c> is canon and it does NOT
    /// close the card: it hands off to the brain-backup resurrection, which is the next stage. The last page
    /// of the chain is where the verb really is a close. Both are pressed here through the renderer's own
    /// event channel, because the bug this whole wave is exposed to is a control that looks like a way out
    /// and reaches nothing.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheDeathPanels))]
    public async Task PressingTheDismissMovesTheDeathOnAndTheLastOneEndsIt(string stage, string face)
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.StageTheDemand(bench, stage);

        DeskBench.Painted.Node row = await TheCloseRow(bench, stage);
        DeskBench.Painted.Node verb = TheControlReading(row, face, stage);

        await bench.PressAsync(verb.Handlers["onclick"]);
        await bench.RenderAsync();

        if (stage == "Resurrected")
        {
            Assert.True(bench.Field("_busted") is null,
                $"\"{face}\" is the last page of the death chain and pressing it left the card up. The panel "
                + "carries no ✕ only because every answer on it is a way out.");
            return;
        }

        Assert.True(bench.Field("_busted") is not null,
            $"\"{face}\" took the death card off the screen. It is not a close and never was — the captain "
            + "is revived from a brain backup, and the clinic bill and the succession hang off the stage it "
            + "hands to. Wave 7 moved the row onto the shell; it did not change what the verb does.");

        Assert.Equal("Resurrected", PhaseOf(bench));
    }

    /// <summary>
    /// THE SHELF IS DRAWN AND WIRED ON ALL FOUR PANELS — AND THE PRESS ITSELF IS THIS BENCH'S HORIZON,
    /// SAID OUT LOUD RATHER THAN FAKED.
    ///
    /// <para><c>OpenLogbookFromDeath</c> opens the drawer by <b>refreshing the shelf first</b>, and that walk
    /// is <c>SaveSlotBook.List</c> → <c>RendererSlotStore.Read</c> → <c>RendererInterop.VaultRead</c>, a
    /// <c>[JSImport]</c> — DeskBench's own documented browser gate. Off a browser the read throws before
    /// <c>_showSaveDrawer</c> is ever set, so a guard that asserted the drawer opens here would be asserting
    /// something no bench in this repo can see. #1007 met the same wall on the import consent's <i>💾 Bank
    /// current first</i> and did the same thing: assert the theory here, press it in a real Chrome.</para>
    ///
    /// <para>So what is held here is everything short of the gate, which is also everything this refactor
    /// could have broken: the shelf is on every panel, it is the shell's second way out rather than a
    /// hand-rolled button, it is WIRED (a handler id the renderer's own event channel would dispatch to),
    /// and the method behind it is a real one that really raises the drawer — the last of those is #970's
    /// <c>THE_LOGBOOK_DOOR_IsARealHandlerThatRealyOpensTheDrawer</c>, untouched by this wave and re-read
    /// here so the two halves of the claim sit in one place. The press itself is in the PR's browser walk.
    /// </para>
    /// </summary>
    [Theory]
    [MemberData(nameof(TheDeathPanels))]
    public async Task TheShelfIsTheShellsSecondWayOutAndItIsWiredOnEveryPanel(string stage, string face)
    {
        _ = face;
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        TheShellOwnsTheViewObjectFamilyAndTheBustedStagesTests.StageTheDemand(bench, stage);

        DeskBench.Painted.Node row = await TheCloseRow(bench, stage);
        DeskBench.Painted.Node shelf = TheControlReading(row, TheShelf, stage);

        Assert.True(shelf.HasClass("overlay-shell-beside"),
            $"the {stage} panel's shelf is drawn by hand beside a shell that draws the button next to it. "
            + "Two ways out on one row, and only one of them answerable to the shell's audit.");

        Assert.True(shelf.Handlers["onclick"] != 0,
            $"the {stage} panel's shelf has no handler id: the renderer wrote a button the event channel "
            + "cannot dispatch to. That is the control that LOOKS like a way out and is not one.");

        // …and the far side of the gate, read off the shipping page rather than pressed: the razor names a
        // method that exists, and that method really raises the one save surface this game has.
        MethodInfo door = typeof(Map).GetMethod(
                              "OpenLogbookFromDeath", BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? throw new Xunit.Sdk.XunitException(
                              "Map has no OpenLogbookFromDeath — the shell's OnBeside is wired to a method "
                              + "that does not exist, on all four death panels.");
        Assert.Equal("Void", door.ReturnType.Name);
    }

    // ── The shuttle hatch ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE HATCH IS THE SHELL'S AND ITS FOOT IS STILL A ROW THE PAGE NAMED.
    ///
    /// <para>#1007 named <c>.deck-shuttle-card</c> as the last root in #735's capped-and-scrolling block
    /// written without <c>::deep</c>. It is migrated with <c>WaysClass</c> rather than by moving its button:
    /// a Bare shell's dismiss is the card's last DIRECT child, and <i>Close hatch</i> lives inside
    /// <c>.deck-shuttle-actions</c>, a centred flex row. So the row is still drawn, still wears its name,
    /// and the button inside it is the shell's — which is the arrangement the whole guard reads back.</para>
    /// </summary>
    [Fact]
    public async Task TheShuttleHatchIsDrawnThroughTheShellAndItsFootIsStillItsOwnRow()
    {
        using DeskBench bench = await DeskBench.BootAsync(Ashore);
        RaiseTheHatch(bench);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
                                          .FirstOrDefault(n => n.HasClass("deck-shuttle-card") && !n.Hidden)
                                      ?? throw new Xunit.Sdk.XunitException(
                                          "raising the shuttle-bay hatch drew nothing wearing "
                                          + ".deck-shuttle-card.");

        Assert.True(card.HasClass("overlay-shell") && card.HasClass("overlay-shell-bare"),
            "the shuttle-bay hatch is on the screen and it is not the shell's.");

        DeskBench.Painted.Node foot = card.Children.LastOrDefault()
                                      ?? throw new Xunit.Sdk.XunitException("the hatch has no last child.");

        Assert.True(foot.HasClass("overlay-shell-ways") && foot.HasClass("deck-shuttle-actions"),
            "the hatch's way out is no longer inside the foot the page names and centres — it is "
            + $"<{foot.Element} class=\"{foot.ClassList}\">. `.deck-shuttle-card > .deck-shuttle-actions` "
            + "is #735's sticky pin AND the rule that centres the button; a dismiss dropped loose at the end "
            + "of the card takes the card's left margin instead. That is a control moving on the screen, "
            + "which this migration does not do.");

        var controls = foot.SelfAndDescendants()
            .Where(n => !n.Hidden && n.Handlers.ContainsKey("onclick") && n.Name.Length > 0)
            .ToList();

        Assert.True(controls.Count == 1 && controls[0].Name == "Close hatch",
            "the hatch's foot is one button reading \"Close hatch\" and it is now "
            + $"[{string.Join(" · ", controls.Select(n => n.Name))}].");

        Assert.True(controls[0].HasClass("overlay-shell-dismiss"),
            "the button in the foot is not the shell's dismiss, so nothing audits whether it has a verb "
            + "behind it — which is the entire reason the root was migrated.");

        await bench.PressAsync(controls[0].Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.DoesNotContain(after.Root.Descendants(),
            n => n.HasClass("deck-shuttle-card") && !n.Hidden);
    }

    /// <summary>
    /// THE OTHER CARD ON THIS ROOT STAYS NAMED, AND THE REASON IS PINNED RATHER THAN WRITTEN DOWN.
    ///
    /// <para>#1002's satchel branch, per surface: <c>.deck-shuttle-card</c> carries TWO surfaces — the hatch
    /// (#163) and the boarding load-out (#313/#488) — and only one of them is pure chrome. The load-out's
    /// foot is a row of TWO controls whose shape changes with <c>_boardEmptyConfirm</c>, and the second one
    /// is not a way out at all: <i>Load something first</i> goes BACK a step, to the load-out. A shell draws
    /// its ways out; it does not draw a page's step-back button, and calling one a dismiss would be the
    /// false claim #1005 was written about.</para>
    ///
    /// <para>Written down it would drift, so it is read off the markup: the load-out is still a plain div,
    /// its foot really does hold more than one control, and the step-back button really does still say what
    /// it says. The day that stops being true, the wave-8 crew is told rather than left reading an
    /// excuse.</para>
    /// </summary>
    [Fact]
    public void TheBoardingLoadOutIsTheOneShuttleCardLeftNamedAndItsReasonIsPinned()
    {
        string razor = Razor("Map.razor");

        int at = razor.IndexOf("@if (_boardTarget is { } boardStop)", StringComparison.Ordinal);
        Assert.True(at > 0, "Map.razor no longer gates the boarding load-out on _boardTarget.");

        int card = razor.IndexOf("class=\"deck-shuttle-card\"", at, StringComparison.Ordinal);
        Assert.True(card > at,
            "the boarding load-out no longer wears .deck-shuttle-card. If it has been migrated or renamed, "
            + "this straggler's reason is spent and belongs deleted rather than left standing.");

        int line = razor.LastIndexOf('<', card);
        Assert.Equal("<div", razor.Substring(line, 4));

        // …and the reason itself: a foot of more than one control, one of which walks BACK.
        string loadOut = razor[at..razor.IndexOf("@* #223: the treasure-map card", at, StringComparison.Ordinal)];
        Assert.Contains("Load something first", loadOut, StringComparison.Ordinal);
        Assert.Contains("_boardEmptyConfirm = false", loadOut, StringComparison.Ordinal);
        Assert.Contains("CancelBoarding", loadOut, StringComparison.Ordinal);

        int feet = Regex.Matches(loadOut, "class=\"deck-shuttle-actions\"").Count;
        Assert.True(feet >= 2,
            $"the load-out draws {feet} action row(s). Its reason for staying named is that the foot has two "
            + "SHAPES — the ordinary Board/Never mind row and the empty-sling confirm — and a shell draws "
            + "one dismiss. If the card has come down to a single row with a single way out, migrate it and "
            + "delete this guard.");
    }

    // ── Fable's ruling, wave 7 ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE FRONT DOOR IS NOT A POP-UP, AND THE THREE SHEETS OVER IT STILL ARE.
    ///
    /// <para><b>Fable's ruling, wave 7.</b> The front door sat in the dismissibility register as
    /// <c>Exit.EveryControlCloses</c>, undriven — nothing had ever pressed a control on it —
    /// and #1007 disproved the claim from the other side by pressing the ▸ dev-starts chevron and finding
    /// the door still standing. It is not a pop-up that has to earn a way out; it is the threshold. So the
    /// row moved from <c>TheRegister</c> to <c>NotPopUpsAndWhy</c>, and the undriven ceiling went 15 → 14
    /// with it — a deliberate edit to a written-down count, made by the wave that meant to make it rather
    /// than by a refactor that promised to move nothing.</para>
    ///
    /// <para>The ruling has two halves and this guard holds both, because half of it would be worse than
    /// none: the front door is exempt AND the logbook, the bank sheet and the import consent — three real
    /// pop-ups sharing the same <c>.start-picker-backdrop</c> class — are still asked the owner's question.
    /// An exemption that swallowed its three siblings would leave the save surface covered by nothing.</para>
    /// </summary>
    [Fact]
    public void TheFrontDoorIsNotAPopUpAndTheSheetsOverItStillAre()
    {
        var exempt = (IDictionary)TheLaw("NotPopUpsAndWhy")!;

        Assert.True(exempt.Contains("start-picker-backdrop"),
            "Fable's ruling, wave 7: the front door belongs in NotPopUpsAndWhy and it is not there.");

        Assert.Equal(
            "The front door is the game's threshold, not a pop-up over play — there is no game behind it to "
            + "return to, so a way out would be a door to nowhere. Its sheets (the logbook, the bank sheet, "
            + "the consent) are pop-ups and have their ways out.",
            (string)exempt["start-picker-backdrop"]!);

        var register = ((IEnumerable)TheLaw("TheRegister")!).Cast<object>().ToList();
        string NameOf(object row) => (string)row.GetType().GetProperty("Name")!.GetValue(row)!;
        string RootOf(object row) => (string)row.GetType().GetProperty("RootClass")!.GetValue(row)!;

        Assert.DoesNotContain(register, row => NameOf(row).Contains("front door", StringComparison.Ordinal));

        Assert.Contains(register, row =>
            RootOf(row) == "start-picker-backdrop"
            && row.GetType().GetProperty("Raise")!.GetValue(row) is not null);
    }

    // ── The shell's own two new promises ──────────────────────────────────────────────────────────────

    /// <summary>
    /// A SECOND WAY OUT WITH NOTHING BEHIND IT IS REPORTED, AND SO IS ONE WITH NO FACE.
    ///
    /// <para>The shape audit, extended to the control wave 7 taught the shell to draw. It is the same fault
    /// as a ✕ wired to nothing — a control that LOOKS like a way out and is not one, #992's whole subject —
    /// one button further along the row, and it fires at parameter time rather than waiting for a player to
    /// find it. Proved by watching the sink, never by trusting that it would.</para>
    /// </summary>
    [Fact]
    public async Task ASecondWayOutWithNoVerbOrNoFaceIsReported()
    {
        var complaints = new List<string>();
        Action<string> was = OverlayShell.DesignFault;
        OverlayShell.DesignFault = complaints.Add;

        try
        {
            using (ShellBench faceless = ShellBench.Mount(Shell(
                       ("class", "test-plate"), ("Title", "the wake"),
                       ("Frame", OverlayFrame.Bare), ("Dismiss", OverlayDismiss.Close),
                       ("DismissFace", "✕"),
                       ("OnClose", EventCallback.Factory.Create(new object(), () => { })),
                       ("OnBeside", EventCallback.Factory.Create(new object(), () => { })))))
            {
                await faceless.RenderAsync();
            }

            Assert.True(complaints.Count > 0,
                "a shell was given a second way out with no face on it and nothing said. An unlabelled "
                + "button beside a readable one is indistinguishable from a rendering fault.");
            Assert.Contains("the wake", complaints[0], StringComparison.Ordinal);

            complaints.Clear();

            using (ShellBench unwired = ShellBench.Mount(Shell(
                       ("class", "test-plate"), ("Title", "the wake"),
                       ("Frame", OverlayFrame.Bare), ("Dismiss", OverlayDismiss.Close),
                       ("DismissFace", "✕"),
                       ("OnClose", EventCallback.Factory.Create(new object(), () => { })),
                       ("BesideFace", "📖 Load a saved voyage"))))
            {
                await unwired.RenderAsync();
            }

            Assert.True(complaints.Count > 0,
                "a shell named a second way out and wired nothing to it, and nothing said.");
        }
        finally
        {
            OverlayShell.DesignFault = was;
        }
    }

    /// <summary>
    /// A ROW IS DRAWN ONLY WHEN THERE IS A ROW TO DRAW, AND NEVER AROUND A LONE WAY OUT.
    ///
    /// <para>The half of <c>WaysClass</c> that a pixel depends on. Twenty-odd cards in this client pin their
    /// action row with <c>::deep .thing &gt; .thing-close</c> — a DIRECT-child relation — and a wrapper
    /// slipped between the card and its button would quietly unstick every one of them. So a shell that was
    /// given no <c>WaysClass</c> and no second way out draws its dismiss loose, exactly as it has since
    /// #996, and this is asserted rather than assumed.</para>
    /// </summary>
    [Fact]
    public async Task ALoneWayOutIsStillTheCardsLastDirectChild()
    {
        using ShellBench plain = ShellBench.Mount(Shell(
            ("class", "test-plate"), ("Frame", OverlayFrame.Bare), ("Dismiss", OverlayDismiss.Close),
            ("DismissFace", "✕"), ("DismissClass", "test-plate-close"),
            ("OnClose", EventCallback.Factory.Create(new object(), () => { }))));

        DeskBench.Painted.Node card = ShellBench.Wearing(await plain.RenderAsync(), "test-plate")!;
        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-ways"));
        Assert.True(card.Children[^1].HasClass("overlay-shell-dismiss"),
            "a shell with one way out and no named row must leave the dismiss as the card's LAST DIRECT "
            + "CHILD. #735 pins twenty-odd feet by that relation.");

        using ShellBench inARow = ShellBench.Mount(Shell(
            ("class", "test-plate"), ("Frame", OverlayFrame.Bare), ("Dismiss", OverlayDismiss.Close),
            ("DismissFace", "✕"), ("WaysClass", "test-plate-foot"),
            ("OnClose", EventCallback.Factory.Create(new object(), () => { }))));

        DeskBench.Painted.Node named = ShellBench.Wearing(await inARow.RenderAsync(), "test-plate")!;
        DeskBench.Painted.Node foot = named.Children[^1];
        Assert.True(foot.HasClass("overlay-shell-ways") && foot.HasClass("test-plate-foot"));
        Assert.True(foot.Children[0].HasClass("overlay-shell-dismiss"));
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private const string FreeFlying = "/map?start=wreck";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

    /// <summary>The close row of the death panel currently staged, or a failure that names the stage.</summary>
    private static async Task<DeskBench.Painted.Node> TheCloseRow(DeskBench bench, string stage)
    {
        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
                                          .FirstOrDefault(n => n.HasClass("busted-card") && !n.Hidden)
                                      ?? throw new Xunit.Sdk.XunitException(
                                          $"staging the {stage} death drew nothing wearing .busted-card.");

        return card.Descendants().FirstOrDefault(n => n.HasClass("busted-close-row") && !n.Hidden)
               ?? throw new Xunit.Sdk.XunitException(
                   $"the {stage} panel drew no .busted-close-row. Since #951 every death panel ends in one, "
                   + "and since wave 7 the shell draws it — a panel without one has lost both ways out.");
    }

    private static DeskBench.Painted.Node TheControlReading(
        DeskBench.Painted.Node row, string face, string stage)
        => row.SelfAndDescendants()
               .FirstOrDefault(n => !n.Hidden
                                    && n.Handlers.ContainsKey("onclick")
                                    && string.Equals(n.Name, face, StringComparison.Ordinal))
           ?? throw new Xunit.Sdk.XunitException(
               $"the {stage} panel's row has no control reading \"{face}\". These words are on the screen in "
               + "front of a player who has just died; if one has been renamed, this file's name for it must "
               + "move with it rather than the guard being loosened to accept whatever is there.");

    private static string PhaseOf(DeskBench bench)
    {
        object? busted = bench.Field("_busted");
        return busted is null ? "(gone)" : busted.GetType().GetProperty("Phase")!.GetValue(busted)!.ToString()!;
    }

    /// <summary>Put the shuttle-bay hatch up with an EMPTY board — #368's "nothing within shuttle reach"
    /// branch. The register's own row for this surface is undriven because stops in range need a berth this
    /// bench cannot build; the hatch's CHROME needs no stops at all, and the chrome is what wave 7 moved.
    /// </summary>
    private static void RaiseTheHatch(DeskBench bench)
    {
        Type stop = typeof(Map).Assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "ShuttleStop")
                    ?? throw new InvalidOperationException("no ShuttleStop type in the client assembly.");

        bench.Poke("_deckMode", true);
        bench.Poke("_shuttleBayStops",
            Activator.CreateInstance(typeof(List<>).MakeGenericType(stop))!);
    }

    private static object? TheLaw(string member) =>
        typeof(EveryPopUpCanBeDismissedTests)
            .GetField(member, BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null)
        ?? throw new InvalidOperationException(
            $"EveryPopUpCanBeDismissedTests no longer has a static '{member}'. Fable's wave-7 ruling is "
            + "written in that file and read back here; if the law has been restructured, move the ruling "
            + "with it rather than deleting the guard that holds it.");

    private static RenderFragment Shell(params (string Name, object? Value)[] parameters) => builder =>
    {
        builder.OpenComponent<OverlayShell>(0);
        int seq = 1;
        foreach ((string name, object? value) in parameters)
        {
            builder.AddComponentParameter(seq++, name, value);
        }

        builder.CloseComponent();
    };

    private static string Razor(string relative)
        => MapMarkup.Read(Path.Combine(ClientSource(), "Pages", relative));

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

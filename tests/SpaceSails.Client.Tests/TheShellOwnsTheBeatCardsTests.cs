using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpaceSails.Client.Pages;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #997 wave 5 · <b>THE SHELL OWNS THE BEAT CARDS.</b>
///
/// <para>Waves 3 and 4 took the <c>.view-object</c> family and the thirteen cards that borrowed its foot.
/// What was left with a hand-rolled way out were the three remaining MULTI-SURFACE backdrop families in the
/// client — the surfaces that stop the game to tell you something:</para>
///
/// <list type="bullet">
/// <item><b>the celebration family</b> (<c>.mission-celebration-backdrop</c>): the contract-complete
/// fanfare, the treasure map, the wreck's outcome, the research brief, the reveal that contradicts it, and
/// the deflection storyboard;</item>
/// <item><b>the convergence band</b> (<c>.convergence-backdrop</c>): the two mysteries turning out to be one
/// story, and the four things a captain is told once and never again — the first ground, the map growing,
/// the tube feeding them, and the air running out;</item>
/// <item><b>the rescue band</b> (<c>.rescue-backdrop</c>): the tow offer and the loud plan alarm.</item>
/// </list>
///
/// <para>Every one of them was the same three moves typed again — a scrim that closes it, a
/// stopPropagation so a click on the card does not reach that scrim, and one worded verb at the foot — and
/// #735's tall-card block pins that verb on every one of these roots as a DIRECT child. Thirteen of the
/// fourteen are on the shell; the fourteenth is named below with its reason.</para>
///
/// <para>The dismissibility law (<see cref="EveryPopUpCanBeDismissedTests"/>) asks whether a surface can be
/// got rid of at all. What a MIGRATION can break is narrower, and this file asks that instead: is the card
/// still the shell's, is it still <c>Bare</c> (the frame that keeps the way out a direct child), does it
/// still wear its own name, does the button still say the word it always said, and does pressing it take
/// the card down.</para>
/// </summary>
[SlowGate] // #251 · 80 s over 11 test(s) in the 2026-09-02 baseline; see TheSlowGateRosterTests.
public sealed class TheShellOwnsTheBeatCardsTests
{
    /// <summary>The roots of the three families. Named rather than inferred, in the house idiom of #735's own
    /// block: a new beat card has to be added here deliberately, which is the moment somebody asks whether
    /// its way out is the shell's.</summary>
    private static readonly string[] TheBeatCardRoots =
    [
        "convergence-card",        // the band: the Convergence, the first ground, the map, the tube, the air
        "mission-celebration",     // the contract-complete fanfare
        "treasure-map-card",       // the map, the wreck's two roads, its outcome, the wallet fan
        "expedition-brief-card",   // the research brief and the deflection storyboard
        "expedition-reveal-card",  // the reveal that contradicts the brief
        "rescue-card",             // the tow offer and the loud plan alarm
    ];

    // ── Read off the markup as typed ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY BEAT CARD IN THE CLIENT IS DRAWN THROUGH THE SHELL, OR NAMED HERE WITH ITS REASON.
    ///
    /// <para>Read as TYPED, in #992's own idiom and for its reason: the alias law keeps every one of these
    /// roots a lowercase <c>class="…"</c> attribute precisely so a guard that reads the markup can still see
    /// it, and this walks them all. A fifteenth beat card typed as a plain <c>&lt;div&gt;</c> fails here with
    /// its file, its line and its class list.</para>
    ///
    /// <para><b>Keyed on the class list rather than on a line number</b>, which is wave 4's lesson: a line
    /// number moves whenever anything above it is edited, and a written-down reason that drifts onto the
    /// wrong card is worse than no reason at all. It fails the other way too — a straggler that HAS been
    /// migrated must leave the list.</para>
    ///
    /// <para><b>And razor comments are skipped</b>, which is also wave 4's lesson: this page's own migration
    /// comments quote the markup they are about, and a guard that counts text which looks like markup is
    /// counting sentences.</para>
    /// </summary>
    [Fact]
    public void EveryBeatCardInTheClientIsDrawnThroughTheShell()
    {
        var handRolled = new List<(string Where, string Classes)>();
        var twoFeet = new List<string>();

        foreach (string file in RazorFiles())
        {
            string[] lines = File.ReadAllLines(file);
            string shortName = Path.GetFileName(file);
            bool inComment = false;
            int insideAShell = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool opens = line.Contains("@*", StringComparison.Ordinal);
                bool closes = line.Contains("*@", StringComparison.Ordinal);
                bool commented = inComment || opens;
                inComment = inComment ? !closes : (opens && !closes);

                if (!commented)
                {
                    foreach (Match attribute in Regex.Matches(line, "class=\"([^\"]*)\""))
                    {
                        string list = attribute.Groups[1].Value;
                        string[] classes = list.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                        string tag = TagOwning(lines, i, attribute.Index);
                        bool shellDrawn = string.Equals(tag, "OverlayShell", StringComparison.Ordinal);

                        if (classes.Intersect(TheBeatCardRoots, StringComparer.Ordinal).Any() && !shellDrawn)
                        {
                            handRolled.Add(($"{shortName}:{i + 1}  <{tag} class=\"{list}\">", list));
                        }

                        // A way out typed INSIDE a shell: two buttons doing one job on one card, with only
                        // one of them the one the shell's audit watches and only one of them pinned.
                        if (insideAShell > 0 && !shellDrawn
                            && classes.Contains("convergence-close", StringComparer.Ordinal))
                        {
                            twoFeet.Add($"{shortName}:{i + 1}  <{tag} class=\"{list}\">");
                        }
                    }

                    insideAShell += CountOf(line, "<OverlayShell");
                    insideAShell -= CountOf(line, "</OverlayShell>");
                }
            }
        }

        var unexplained = handRolled
            .Where(found => !TheNamedStragglers.ContainsKey(Normalised(found.Classes)))
            .Select(found => found.Where)
            .ToList();

        Assert.True(unexplained.Count == 0,
            $"{unexplained.Count} beat card(s) are NOT drawn through OverlayShell:\n  - "
            + string.Join("\n  - ", unexplained)
            + "\n\n#735's tall-card block pins each of these families' action rows as a DIRECT child of the "
            + "card (`.convergence-card > .convergence-close`, `.expedition-brief-card > button:last-child`, "
            + "`.rescue-card > .rescue-options`). The shell guarantees that relation with "
            + "Frame=\"OverlayFrame.Bare\"; a hand-rolled card guarantees it only until somebody wraps "
            + "something in it. Give it a shell (#997 wave 5) — or name it in TheNamedStragglers with the "
            + "reason, which is the edit that makes somebody say why out loud.");

        var stale = TheNamedStragglers.Keys
            .Where(named => !handRolled.Any(found => Normalised(found.Classes) == named))
            .ToList();

        Assert.True(stale.Count == 0,
            $"{stale.Count} straggler(s) are named here and are no longer hand-rolled: "
            + string.Join(" · ", stale)
            + ". Take them off the list — a written-down reason for a card that has moved on is worse than "
            + "no reason at all.");

        Assert.True(twoFeet.Count == 0,
            $"{twoFeet.Count} .convergence-close button(s) are typed by hand INSIDE a shell:\n  - "
            + string.Join("\n  - ", twoFeet)
            + "\n\nThe way out is the shell's now (`DismissClass=\"btn btn-light convergence-close\"`). A "
            + "hand-written one beside it is two ways out on one card, and the shell's audit only knows "
            + "about its own.");
    }

    /// <summary>
    /// THE STRAGGLER, BY NAME AND WITH THE REASON. One, out of fourteen.
    ///
    /// <para>Keyed on the card's class list exactly as typed, because that list IS the card's identity under
    /// the alias law and it is the one thing about it that a refactor is not allowed to move.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> TheNamedStragglers =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["convergence-card old-crew-card"] =
                "#973's face scene — \"you look different\", the first meeting after a rebirth with somebody "
                + "who knew the old face. Its way out is not the LAST thing in the card, and this time that "
                + "is a fact about a FLOAT rather than about a row. `.convergence-x` says so in its own "
                + "rule: \"the card is position: static, so this floats to the right of the flow it sits at "
                + "the top of\" — a float lands beside the line box it is declared next to, which is why "
                + "#992 typed that ✕ as the FIRST thing in the card. A Bare shell draws its dismiss as the "
                + "card's last direct child (the whole point of the frame, and the reason the other twelve "
                + "could take it), so shelling this one would float the ✕ down beside the card's last line "
                + "instead of into its top-right corner. That is a control moving on the screen, which this "
                + "migration does not do. It is also the only card of the fourteen with TWO ways out — the "
                + "✕, and \"Leave it there\" once he has answered — which is a shape a Bare shell has no "
                + "room for. It gets a shell when the corner ✕ stops depending on source order.",
        };

    private static string Normalised(string classList) =>
        string.Join(' ', classList.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));

    // ── Read off what was actually drawn ──────────────────────────────────────────────────────────────

    /// <summary>
    /// …AND THE WAY OUT IS A DIRECT CHILD OF THE CARD, ON EVERY ONE THE BENCH CAN RAISE.
    ///
    /// <para>The guard above proves the SHAPE of the markup; this proves the DOM that shape produces, which
    /// is what #735's selectors actually read. A shell drawn <c>Hosted</c> instead of <c>Bare</c> would pass
    /// the source guard word for word and put a <c>display: contents</c> div between the card and its
    /// button — correct-looking markup, a silently unstuck foot.</para>
    ///
    /// <para><b>The wording is asserted on purpose.</b> These feet are not ✕: they say "…sit with that",
    /// "Boots on, then.", "Raise a glass 🍻", "Back to the shuttle". The whole claim of the wave is that not
    /// one of those words moved, so the words are here — against Core's own constant where Core owns the
    /// line, and as a literal where the page does — and a change to any of them costs an edit to a test
    /// instead of passing unnoticed.</para>
    ///
    /// <para><b>Each convergence row puts its five siblings down first.</b> Five gates draw a
    /// <c>.convergence-backdrop</c> and a landing raises the first-ground lesson by itself; without the
    /// reset this guard reads whichever card happens to be earliest in the tree and reports it under
    /// another one's name, which is this repository's first named bug class committed inside a test.</para>
    /// </summary>
    [Theory]
    [InlineData("the Convergence", Docked, "convergence-card", "_convergenceRevealOpen", "…sit with that")]
    [InlineData("the first ground", Ashore, "ground-lesson-card", "_groundLessonOpen", GroundLesson.Dismiss)]
    [InlineData("the map just got bigger", Ashore, "ground-grows-card", "_groundGrewOpen",
        GroundGrows.Dismiss)]
    [InlineData("the tube feeds you", Ashore, "ground-grows-card", "_tubeRearmOpen", TubeRearm.Dismiss)]
    [InlineData("the tank is getting low", Ashore, "ground-grows-card", "_airCardOpen", AirCard.Dismiss)]
    [InlineData("the contract-complete fanfare", Docked, "mission-celebration", "_celebration",
        "Raise a glass 🍻")]
    [InlineData("the wreck's outcome", FreeFlying, "treasure-map-card", "_wreckOutcome",
        "Back to the shuttle")]
    [InlineData("the treasure map", FreeFlying, "treasure-map-card", "_treasureMapCard",
        "Into the ledger 🗺")]
    public async Task EveryBeatCardTheBenchCanRaiseWearsTheShellsOwnFoot(
        string name, string world, string root, string gate, string wording)
    {
        using DeskBench bench = await DeskBench.BootAsync(world);
        RaiseTheBeat(bench, gate);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass(root) && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: setting {gate} drew nothing wearing .{root}. The driver and the markup's gate have "
                + "come apart; one of them has moved.");

        Assert.True(card.HasClass("overlay-shell"),
            $"{name}: its card is on the screen and it is not the shell's — #997 wave 5 put every beat card "
            + "on OverlayShell, and this one has come back off it.");
        Assert.True(card.HasClass("overlay-shell-bare"),
            $"{name}: the shell drew it as something other than Bare. Bare is what keeps the way out a "
            + "DIRECT child of the card, which is the relation every pinned foot in #735's block is written "
            + "against — and Hosted's `display: contents` would unstick it while the markup went on looking "
            + "right.");
        Assert.True(card.HasClass(root),
            $"{name}: the card has lost its own class. The shell is the MECHANISM and .{root} is the "
            + "IDENTITY, and #995's completeness guard reads that name off the markup as typed.");

        DeskBench.Painted.Node foot = card.Children
            .FirstOrDefault(n => n.HasClass("overlay-shell-dismiss") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                $"{name}: its way out is not a DIRECT child of the card. Something has come between them, "
                + "and everything #735 pins on these families is written against that exact relation.");

        Assert.Equal(wording, foot.Name);

        Assert.True(foot.Handlers.ContainsKey("onclick"),
            $"{name}: the way out is drawn and nothing is wired to it — a control that LOOKS like a way out "
            + "and is not one, which is what the owner's ruling of 2026-08-24 forbids.");

        await bench.PressAsync(foot.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.True(after.Root.Descendants().All(n => !n.HasClass(root) || n.Hidden),
            $"{name}: \"{wording}\" was pressed and the card is still on the screen. The shell's dismiss runs "
            + "whatever OnClose is wired to and nothing else — a way out wired to something that is not this "
            + "card's own close verb is a control that looks like a way out and is not one.");
    }

    /// <summary>
    /// THE TOW OFFER IS A ByDecision SHELL, AND BOTH OF ITS ANSWERS REALLY DO END IT.
    ///
    /// <para>Its own guard, because it is the one card of the fourteen that is a DECISION rather than a
    /// beat: #992's audit put it in the critical-decision exception — no ✕, because every control it offers
    /// is itself a way out — and the register has never driven it, because <c>Adrift</c> is a
    /// fuel-and-velocity verdict on a live sim rather than a field.</para>
    ///
    /// <para>It is a verdict on TWO fields (<c>_reactionMassPulses == 0 &amp;&amp; !_docked</c>), so it can
    /// be stood up honestly by emptying the tank of a ship that is already flying — and then the claim #992
    /// took from a list is proved the way waves 3 and 4 proved theirs: by pressing. Both answers are
    /// pressed, one per run of the theory, and the offer has to be gone afterwards. That is the check that
    /// caught the rep's card and the collector's demand both claiming an exception they had not earned.</para>
    /// </summary>
    [Theory]
    [InlineData("Accept")]
    [InlineData("Decline")]
    public async Task TheTowOfferOffersNoWayOutButAnAnswerAndBothAnswersAreOne(string answer)
    {
        using DeskBench bench = await DeskBench.BootAsync(FreeFlying);
        bench.Poke("_reactionMassPulses", 0);
        bench.Poke("_showRescueOffer", true);

        DeskBench.Painted painted = await bench.RenderAsync();
        DeskBench.Painted.Node card = painted.Root.Descendants()
            .FirstOrDefault(n => n.HasClass("rescue-card") && !n.Hidden)
            ?? throw new Xunit.Sdk.XunitException(
                "emptying the tank of a flying ship drew no .rescue-card. Adrift is "
                + "`_reactionMassPulses == 0 && !_docked`; one of those two has moved.");

        Assert.True(card.HasClass("overlay-shell-bare"),
            "the tow offer is not the shell's Bare frame. #735 pins `.rescue-card > .rescue-options` as a "
            + "DIRECT child; a wrapper between the card and that row unsticks it.");

        Assert.DoesNotContain(card.SelfAndDescendants(), n => n.HasClass("overlay-shell-dismiss"));

        DeskBench.Painted.Node press = card.Descendants()
            .FirstOrDefault(n => !n.Hidden
                                 && n.Handlers.ContainsKey("onclick")
                                 && n.Name.Contains(answer, StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException(
                $"the tow offer has no control reading \"{answer}\". Its two answers ARE the card — a "
                + "ByDecision surface that has mislaid one of them is a surface with no way out.");

        await bench.PressAsync(press.Handlers["onclick"]);
        DeskBench.Painted after = await bench.RenderAsync();

        Assert.DoesNotContain(after.Root.Descendants(), n => n.HasClass("rescue-card") && !n.Hidden);
    }

    /// <summary>Put one beat on the screen. The five bools are the convergence band — and each of those puts
    /// its siblings down first, because they share a root and the law next door learned the hard way what
    /// reading the wrong one costs. The three records are built out of Core's own types, so everything the
    /// card prints past the gate is the shipping content.</summary>
    private static void RaiseTheBeat(DeskBench bench, string gate)
    {
        if (TheConvergenceBand.Contains(gate, StringComparer.Ordinal))
        {
            foreach (string other in TheConvergenceBand)
            {
                bench.Poke(other, false);
            }

            bench.Poke("_faceScene", null);   // the sixth card on that root, gated on a name, not a bool
            bench.Poke(gate, true);
            return;
        }

        bench.Poke(gate, gate switch
        {
            "_celebration" => (MissionCelebration?)new MissionCelebration(
                "THE ICE RUN", "Madam Coil", Celebrations.GiverThanks("Madam Coil"), 4_200, 3,
                "the bird sings the payday"),
            "_wreckOutcome" => (Derelict.SalvageOutcome?)new Derelict.SalvageOutcome(
                CreditsNow: 12_400, HeatGained: 0, ContactEarned: true, CargoIsHot: false,
                Line: "You filed it straight, and somebody read it."),
            "_treasureMapCard" => (TreasureCache?)new TreasureCache(
                "shell-wave-5", "luna", "THE LEANING MAST", "nor'-nor'-east", 40, 900,
                Array.Empty<CacheCargo>(), 0.0, "a dead man", PlayerOwned: false),
            _ => true,
        });
    }

    /// <summary>The five gates that all draw a <c>.convergence-backdrop</c>.</summary>
    private static readonly string[] TheConvergenceBand =
    [
        "_convergenceRevealOpen", "_groundLessonOpen", "_groundGrewOpen", "_tubeRearmOpen", "_airCardOpen",
    ];

    // ── Plumbing ──────────────────────────────────────────────────────────────────────────────────────

    private const string FreeFlying = "/map?start=wreck";
    private const string Docked = "/map?dock=selene-gate&body=luna&site=1";
    private const string Ashore = "/map?dock=the-tilt&site=0&land=1";

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

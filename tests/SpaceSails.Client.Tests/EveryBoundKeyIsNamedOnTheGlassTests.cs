using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #440 · <b>A KEY THE GAME ANSWERS AND NEVER NAMES IS A BUG, NOT A SECRET.</b>
///
/// <para>Owner, live 2026-07-26, of the shovel: <i>"that feature and button is not clearly told to a new
/// player… Do an issue about all hidden functionalities like that so they are really clearly told to a new
/// player somewhere on the UI."</i> He said it while reaching for the wrong key for a feature he had
/// specified himself. #443 and #444 fixed the shovel; this is the law that stops the next one.</para>
///
/// <para><b>The two halves, and why it takes two.</b></para>
/// <list type="number">
///   <item><b>The census is complete.</b> Every key literal the client's two key tables test against —
///   <c>Map.Sim.Keys.OnKeyDown</c> and <c>Map.Deck.Walk.HandleDeckKey</c> — must be a row below. Bind a new
///   key and this goes red before anybody has to remember to tell a player about it.</item>
///   <item><b>Every row is told on the glass.</b> Each non-exempt row must be NAMED, in one of the shapes
///   this game already names keys in, somewhere in <see cref="HintSources"/>.</item>
/// </list>
///
/// <para><b>The Captain's Guide is deliberately NOT a hint source, and that is the whole point of the
/// law.</b> <c>Pages/Guide.razor</c> and <c>/help/nav</c> name nearly every key in the game, so admitting
/// them would make this guard pass on any world at all — the fifth bug class exactly (a threshold that
/// selects everything). A manual is read before you play; #440 is about the moment it matters. What counts
/// here is a keybar, a caption, a standing prompt, a chip, or the hover of a control already on the
/// screen.</para>
///
/// <para><b>Proved red before it shipped.</b> Each of the fixes in this lane was reverted in turn and this
/// class watched to go red for that key alone: B (the deck keybar's bank entry), M (off the regolith), H
/// (on the regolith), P, V, and the drive keys on the fuel gauge's hover. The census half was proved red by
/// planting a <c>case "j" or "J":</c> in <c>HandleDeckKey</c>, and the corpus reader was proved red by
/// pointing a hint source at a file that does not exist.</para>
/// </summary>
public class EveryBoundKeyIsNamedOnTheGlassTests
{
    // ── THE CENSUS ────────────────────────────────────────────────────────────────────────────────────
    //
    // One row per key the client binds. `Literals` are the spellings the key tables test against (the
    // completeness half matches on these); `Named` are the tokens a hint may use to name it (⇧ and − are
    // the game's own spellings, and "arrows"/"WASD" is how the bars have always said the walk).

    private sealed record BoundKey(
        string Key, string[] Literals, string Where, string[] Named, string? ExemptBecause = null);

    private static readonly BoundKey[] Census =
    [
        new("WASD / arrows", ["w", "W", "a", "A", "s", "S", "d", "D",
                              "ArrowLeft", "ArrowRight"],
            "walk the deck (Map.Deck.Walk) · steer the shuttle (Map.Sim.Keys)", ["WASD", "arrows"]),
        new("Q", ["q", "Q"], "back to the helm · abort the boarding run", ["Q"]),
        new("E", ["e", "E"], "work whatever is in front of you", ["E"]),
        new("F", ["f", "F"], "shoot the lock (#563), ashore only", ["F"]),
        new("B", ["b", "B"], "open the favour bank at a contact's table (Map.Quests.Bank)", ["B"]),
        new("G", ["g", "G"], "drop the chest and sprint (#313), ashore only", ["G"]),
        new("I", ["i", "I"], "the satchel (#603), ashore only", ["I"]),
        new("H", ["h", "H"], "weapons tight — the sentry remote (#538), ashore only", ["H"]),
        new("K", ["k", "K"], "knock / sound the plating (#537), aboard a wreck", ["K"]),
        new("T", ["t", "T"], "set or lift a sentry (#314); ⇧T holds your line home (#326)", ["T"]),
        new("M", ["m", "M"], "mute and unmute every cue (#338) — global", ["M"]),
        new("O", ["o", "O"], "enter orbit / arm the capture", ["O"]),
        new("P", ["p", "P"], "the plotting table", ["P"]),
        new("V", ["v", "V"], "vent the hull charge", ["V"]),
        new("`", ["`", "~"], "peek at the map — hide every panel (#1038)", ["`"]),
        new("/", ["/"], "the nav search box (#406)", ["/"]),
        new("?", ["?"], "raise the plotting card (#949)", ["?"]),
        new("Escape", ["Escape"], "the keyboard CANCEL — shut the top card, else back to Nav (#351)",
            ["Esc", "Escape"]),

        // The drive. ArrowUp/ArrowDown are BOTH a pulse here and a walk above — one literal, two rows, and
        // the completeness half only asks that a literal belong to SOME row.
        new("+ (accelerate)", ["+", "=", "ArrowUp"], "the ±10% pulse", ["+", "↑"]),
        new("− (decelerate)", ["-", "_", "ArrowDown"], "the ±10% pulse", ["−", "↓"]),

        // Not an e.Key at all — it rides the EVENT (#326), so the completeness half can never see it. It is
        // in the census because a modifier that changes what a key does is an affordance like any other.
        new("Shift", [], "±1% fine trim in flight · ⇧T's second sentry stance", ["Shift", "⇧"]),

        // ── THE EXEMPT, EACH WITH ITS REASON ──────────────────────────────────────────────────────────
        new("Enter", ["Enter", "NumpadEnter"], "presses the visible primary action of an open card (#735)",
            [],
            ExemptBecause:
            "#735 bound it to a button that is ALREADY ON THE SCREEN and already says what it does — the "
            + "keyboard YES for a card whose YES you can see and click. Nothing is hidden by not naming it, "
            + "and printing '(Enter)' on the primary of every card in the game would be chrome on fifty "
            + "surfaces to shorten one reach."),

        new("0–7 (the desks)", ["0"], "switch desk — the digit range is pinned by TheTabBarPrintsItsOwnKeys",
            [],
            ExemptBecause:
            "The tab bar prints the digit ON the chip (ShipTabsStrip: @DeskKeyLabel(desk) @DeskLabel(desk)), "
            + "so the telling is the label itself and there is no 'N — verb' sentence to find. Exempt from "
            + "the text search and pinned instead by TheTabBarPrintsItsOwnKeys below, which fails if either "
            + "the chip stops printing the key or the range the handler accepts moves."),
    ];

    // ── WHERE THE GAME NAMES A KEY ────────────────────────────────────────────────────────────────────
    //
    // In-the-moment tellings only. See the class note for why the Captain's Guide is not on this list.

    private static readonly string[] HintSources =
    [
        // The keybars, the instrument column and the standing prompt.
        "src/SpaceSails.Client/Pages/Map.Surface.Hud.Prompts.cs",
        "src/SpaceSails.Client/Pages/Map.Deck.Prompts.cs",
        "src/SpaceSails.Client/Rendering/DeckView.cs",
        "src/SpaceSails.Client/Rendering/ShuttleFlightView.cs",
        // Core's own plates, which the bars quote.
        "src/SpaceSails.Core/LockedDoor.cs",
        "src/SpaceSails.Core/LeftBehind.cs",
        "src/SpaceSails.Core/RipAndBin.cs",
        "src/SpaceSails.Core/SentryDoctrine.cs",
        // The once-per-captain first-ground card.
        "src/SpaceSails.Core/GroundLesson.cs",
        // Controls already on the screen that name their own key in their face or their hover.
        "src/SpaceSails.Client/Pages/Map/ShipTabsStrip.razor",
        "src/SpaceSails.Client/Pages/Map/NavSearchPanel.razor",
        "src/SpaceSails.Client/Pages/Map/NavHud/NavToolbar.razor",
        "src/SpaceSails.Client/Pages/Map/NavHud/OrbitAssistBox.razor",
        "src/SpaceSails.Client/Pages/Map/DeskPanels.razor",
        "src/SpaceSails.Client/Pages/Map/ChargeBoardPanel.razor",
        "src/SpaceSails.Client/Pages/Map.NavToolbar.cs",
    ];

    /// <summary>The two files that decide what a key does. Nothing else in the client reads a raw key.</summary>
    private static readonly string[] KeyTables =
    [
        "src/SpaceSails.Client/Pages/Map.Sim.Keys.cs",
        "src/SpaceSails.Client/Pages/Map.Deck.Walk.cs",
    ];

    // ── HALF ONE · THE CENSUS IS COMPLETE ─────────────────────────────────────────────────────────────

    [Fact]
    public void EveryKeyTheHandlersTestForIsInTheCensus()
    {
        IReadOnlyCollection<string> bound = LiteralsTheHandlersTestFor();

        // The world can tell pass from fail: if the reader ever goes blind — a file renamed, the switch
        // rewritten into a shape it cannot see — this is the assertion that says so rather than passing on
        // an empty set (the fifth bug class, arriving through a parser).
        Assert.True(bound.Count >= 30,
            $"only {bound.Count} key literals found in {string.Join(", ", KeyTables)} — the reader has gone "
            + "blind, or the key tables moved. Fix the reader; do not lower this number.");

        HashSet<string> censused = [.. Census.SelectMany(k => k.Literals)];
        string[] untold = [.. bound.Where(k => !censused.Contains(k)).OrderBy(k => k, StringComparer.Ordinal)];

        Assert.True(untold.Length == 0,
            "a key is bound that the census does not know about — add a row to Census (and a hint to go "
            + "with it, or an exemption with a reason): " + string.Join(", ", untold.Select(k => $"\"{k}\"")));
    }

    // ── HALF TWO · EVERY ROW IS TOLD ON THE GLASS ─────────────────────────────────────────────────────

    [Fact]
    public void EveryBoundKeyIsNamedBySomethingTheGameDraws()
    {
        string corpus = TheHintCorpus();
        var untold = new List<string>();

        foreach (BoundKey key in Census)
        {
            if (key.ExemptBecause is not null)
            {
                continue;
            }

            if (!key.Named.Any(token => IsNamedIn(corpus, token)))
            {
                untold.Add($"{key.Key} ({key.Where})");
            }
        }

        Assert.True(untold.Count == 0,
            "bound and never named at the moment it matters — put it on a keybar, a caption or the hover of "
            + "a control that is already on the screen: " + string.Join(" · ", untold));
    }

    /// <summary>The corpus a hint may live in — and it is never allowed to come back empty or short, for
    /// the reason the census count is not: a guard reading nothing finds no offenders forever.
    ///
    /// <para><b>WITH EVERY COMMENT CUT OUT OF IT, and that is not tidiness.</b> This repository writes its
    /// reasoning in the source, at length, and those paragraphs name keys constantly — the first draft of
    /// this guard passed for <c>P</c> off the sentence <i>"the plotting table has a hotkey (P,
    /// Map.Sim.Keys)"</i>, which is a note to the next programmer and not a word any player will ever read.
    /// A guard satisfied by its own justification is the fifth bug class with the lights on. What is left
    /// after the cut is string literals and markup: the things that reach the glass.</para></summary>
    private static string TheHintCorpus()
    {
        var sb = new System.Text.StringBuilder();
        foreach (string rel in HintSources)
        {
            string path = Path.Combine(TestTree.RepoRoot(), rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"hint source is gone: {rel}. Fix the list; do not drop the file.");
            sb.AppendLine(WithoutComments(File.ReadAllText(path)));
        }

        string corpus = sb.ToString();
        Assert.True(corpus.Length > 10_000, $"the hint corpus is only {corpus.Length} characters — it has "
            + "collapsed, and a guard that reads nothing passes forever.");
        return corpus;
    }

    /// <summary>Razor comments, block comments and line comments, gone. A line comment is cut to the end of
    /// its line, which leaves any hint that stood BEFORE it on that line intact — the shape the keybars
    /// actually have (<c>parts.Add("🔊 M — mute"); // #338 …</c>).</summary>
    private static string WithoutComments(string source) =>
        Regex.Replace(
            Regex.Replace(
                Regex.Replace(source, @"@\*.*?\*@", " ", RegexOptions.Singleline),
                @"/\*.*?\*/", " ", RegexOptions.Singleline),
            @"//[^\r\n]*", " ");

    /// <summary>
    /// The shapes this game names a key in, and no others. Each one is a MARKER beside the key, never the
    /// bare character: a lone "B" occurs in a thousand words, and a guard that took one would be satisfied
    /// by prose that names nothing.
    ///
    /// <list type="bullet">
    ///   <item><c>K — verb</c> · the keybar and caption idiom (#324), em dash or en dash.</item>
    ///   <item><c>(… p / O)</c> · a button face carrying its key after the cost (#963).</item>
    ///   <item><c>(P)</c>, <c>( / )</c>, <c>(? or Esc…)</c> · a key in parentheses on a face or a hover.</item>
    ///   <item><c>press Esc</c> / <c>Press M</c> · said in words.</item>
    ///   <item><c>` toggles</c> · said in words the other way round.</item>
    ///   <item><c>+ / −</c> and <c>/ ↓)</c> · a pair, which is how the drive has always been written.</item>
    ///   <item><c>hold Shift</c> · the modifier.</item>
    ///   <item><c>WASD</c>, <c>arrows</c>, <c>⇧T</c> · tokens that are already the whole name.</item>
    /// </list>
    /// </summary>
    private static bool IsNamedIn(string corpus, string token)
    {
        string k = Regex.Escape(token);
        // A key token is only a key where it is not part of a longer word. Letters are the only tokens this
        // can happen to ("E" inside "EVERY"), so the boundary is asked of letters and digits alone.
        string standalone = char.IsLetterOrDigit(token[0]) ? $"(?<![A-Za-z0-9]){k}(?![A-Za-z0-9])" : k;

        string[] shapes =
        [
            $@"{standalone}\s*[—–]\s",          // K — verb
            $@"/\s*{standalone}\s*\)",           // (… p / O)
            // (P)  ( / )  (? or Esc to close) — the key is the WHOLE parenthetical or the head of one.
            // Never merely "a bracket then the letter": that admitted "(PR-15)" and "(P, Map.Sim.Keys)".
            $@"\(\s*{standalone}\s*\)",
            $@"\(\s*{standalone}\s+or\s",
            $@"[Pp]ress(?:\s+the)?\s+{standalone}",
            $@"{standalone}\s+toggles",
            $@"hold\s+{standalone}",
            $@"{standalone}\s*/\s",              // + / −
            $@"/\s*{standalone}[\s)]",           // ↑ / ↓)
        ];

        // WASD / arrows / ⇧ are whole names in themselves — the bars have never written "WASD — " with a
        // marker, they write "WASD — move", which the first shape catches, and "WASD / arrows — move",
        // which the seventh does. Nothing extra is needed, and nothing broader is allowed.
        return shapes.Any(shape => Regex.IsMatch(corpus, shape));
    }

    /// <summary>
    /// #440 · <b>WHAT THE LAW ABOVE CANNOT SEE, PINNED BY HAND.</b>
    ///
    /// <para>The sweep asks "is this key named ANYWHERE the game draws", which is the honest shape for a law
    /// that has to survive a scene model nobody has written down. Two of this lane's findings slip under it,
    /// and they slip under it for the same reason: the key WAS named — in one scene, and not in the one you
    /// were standing in. <c>M</c> was on the regolith's keybar and on no other, so a captain who never lands
    /// never met the mute. <c>H</c> has been on a derelict's bar since #538 and never on the ground, where
    /// the sentries were invented and where the pack actually comes.</para>
    ///
    /// <para>So the three sentences this lane put on the glass are pinned where they were put. Delete one
    /// and this goes red naming it, which is the most a source guard can honestly promise about a place —
    /// and it is exactly how the three of them were proved red before shipping.</para>
    /// </summary>
    [Fact]
    public void TheKeysThatWereFoundHidingStayNamedWhereTheyWereHiding()
    {
        // B — the favour bank, at the one fixture that has one. The single key in the shipped game that
        // was named in no in-the-moment telling at all.
        string deckBar = Read("src/SpaceSails.Client/Pages/Map.Deck.Prompts.cs");
        Assert.Contains("ConsoleKind.BarPatron", deckBar);
        Assert.Contains("💰 B — open an account at this table", deckBar);
        Assert.Contains("M — mute", deckBar);

        // H — on the REGOLITH branch, not only the wreck's. The branch is the one that reads the floor's
        // own keybar; "🤖 H" has to appear twice in this file or one of the two grounds has lost it.
        string surfaceBar = Read("src/SpaceSails.Client/Pages/Map.Surface.Hud.Prompts.cs");
        Assert.True(Regex.Matches(surfaceBar, "🤖 H — ").Count >= 4,
            "the sentry remote's key is named in ONE of the two surface keybars again — aboard a wreck and "
            + "on the regolith are two grounds, and #538's own reason ('the one whose absence gets a "
            + "captain shot') is truer on the one the pack comes out of.");

        // The drive — the only control in the game with no button anywhere, so its hover is the only place
        // a captain can be told the ship has a throttle at all.
        string fuel = Read("src/SpaceSails.Client/Pages/Map/DeskPanels.razor");
        Assert.Contains("+ / − (or ↑ / ↓) fires one; hold Shift for a ±1% trim", fuel);
    }

    // ── THE DESK DIGITS' EXEMPTION, PINNED RATHER THAN TRUSTED ────────────────────────────────────────

    [Fact]
    public void TheTabBarPrintsItsOwnKeys()
    {
        string strip = Read("src/SpaceSails.Client/Pages/Map/ShipTabsStrip.razor");
        Assert.Contains("@DeskKeyLabel(desk) @DeskLabel(desk)", strip);

        string state = Read("src/SpaceSails.Client/Pages/Map.UiState.cs");
        Assert.Contains("DeskKeyLabel(ShipDesk desk) => desk == ShipDesk.Captain ? \"0\"", state);

        // …and the range the handler accepts, so a chip bar that prints 0–7 can never sit over a handler
        // that answers 0–9. (The digits are the one family the text search is exempt from; this is what it
        // is exempt IN FAVOUR of.)
        string keys = Read("src/SpaceSails.Client/Pages/Map.Sim.Keys.cs");
        Assert.Contains("e.Key[0] is >= '1' and <= '7'", keys);
        Assert.Contains("if (e.Key == \"0\")", keys);
    }

    // ── READING THE KEY TABLES ────────────────────────────────────────────────────────────────────────

    /// <summary>Every string literal the two key tables COMPARE A KEY AGAINST — the <c>case</c> arms of
    /// both switches and the <c>e.Key is</c> / <c>e.Key ==</c> rungs above them. Deliberately anchored on
    /// those four prefixes: an unanchored sweep for quoted single letters would drag in every pulse message
    /// and every comment in the file, which is a world that cannot tell pass from fail.</summary>
    private static IReadOnlyCollection<string> LiteralsTheHandlersTestFor()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (string rel in KeyTables)
        {
            string src = Read(rel);
            foreach (Match m in Regex.Matches(
                src, @"(?:case|e\.Key is|e\.Key ==|key is|key ==)\s*((?:""[^""]*""(?:\s*or\s*)?)+)"))
            {
                foreach (Match lit in Regex.Matches(m.Groups[1].Value, @"""([^""]*)"""))
                {
                    found.Add(lit.Groups[1].Value);
                }
            }
        }
        return found;
    }

    private static string Read(string rel)
    {
        string path = Path.Combine(TestTree.RepoRoot(), rel.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"{rel} is gone — fix the path, do not drop the guard.");
        return File.ReadAllText(path);
    }
}

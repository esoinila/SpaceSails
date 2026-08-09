using System;
using System.IO;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #741 · THE RED PEN, in the hand — the guards on the client half.
///
/// <para><b>Why source-shape.</b> The same reasoning #680, #688 and #690 wrote down: what is under test
/// here is not a value a Core function returns, it is which control the razor draws, which store it reads,
/// what a press is routed into, and — the one that matters most — WHICH CUE the acknowledgment fires and
/// under which condition. Every law with a Core half is pinned in Core too
/// (<c>TheRedPenDrawsTheLineTests</c>), and these guards exist so the client cannot quietly stop asking.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheRedPenIsAnInstrumentTests
{
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Components(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Components", file));

    /// <summary>One method's body, cut at the next member declaration — the idiom #680's guards use.</summary>
    private static string Method(string src, string signature)
    {
        int start = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"the source no longer declares {signature}");
        int end = src.IndexOf("\n    private ", start + signature.Length, StringComparison.Ordinal);
        int alt = src.IndexOf("\n    /// <summary>", start + signature.Length, StringComparison.Ordinal);
        if (alt >= 0 && (end < 0 || alt < end))
        {
            end = alt;
        }
        return end > start ? src[start..end] : src[start..];
    }

    // ── THE GESTURE IS WIRED TO CORE, AND NOWHERE ELSE ────────────────────────────────────────────────

    /// <summary>Every decision the press makes is asked of <see cref="SpaceSails.Core.CaseThreads"/>. A
    /// client that drew, erased or ordered anything itself would be a second answer to a question Core
    /// already answers — the shape of half the bugs in this repository's history.</summary>
    [Fact]
    public void PressingATitle_AsksCoreEverything()
    {
        string press = Method(Pages("Map.RedPen.cs"), "private void NoteTitlePressed(string id)");

        foreach (string call in new[]
                 {
                     "Core.CaseThreads.AreThreaded(",   // draw or erase?
                     "Core.CaseThreads.Erase(",         // the eraser end, same gesture reversed
                     "Core.CaseThreads.Draw(",          // …and the line itself
                 })
        {
            Assert.True(press.Contains(call, StringComparison.Ordinal),
                $"the press no longer goes through {call} — the client has grown its own answer (#741).");
        }

        // The order of the page is Core's, asked of Page and never rebuilt here.
        string page = Method(Pages("Map.RedPen.cs"), "private IReadOnlyList<Core.CaseThreads.Row> NotebookPage(");
        Assert.True(page.Contains("Core.CaseThreads.Page(", StringComparison.Ordinal),
            "the notebook no longer takes its order from Core (#741).");

        // …and the nodes are drawn off that row and never re-derived.
        string nodes = Components("NotebookNodes.razor");
        Assert.True(nodes.Contains("row.ThreadedToNext", StringComparison.Ordinal)
                    && nodes.Contains("row.ClusterSize", StringComparison.Ordinal)
                    && nodes.Contains("row.Threads", StringComparison.Ordinal),
            "the nodes no longer draw the red marks off Core's own row — a drawn shape reporting one thing " +
            "while the sim does another is this repo's third named bug class (#741).");
        Assert.False(nodes.Contains("_caseThreads", StringComparison.Ordinal),
            "the node component has reached into the thread store — it renders rows and decides nothing (#741).");
    }

    /// <summary>THE QUIET ACKNOWLEDGMENT, and it fires ONCE PER THREAD. The cue is inside the branch Core's
    /// own <c>drawn</c> opens, so a pen dragged back over a line that already exists makes no sound — and
    /// it is the warm chime rather than the prize jingle, because the register is the whisky glass set down
    /// (owner's north star), not a slot machine.</summary>
    [Fact]
    public void TheCue_IsSoft_AndOnlyFiresForALineThatWasNotThere()
    {
        string press = Method(Pages("Map.RedPen.cs"), "private void NoteTitlePressed(string id)");

        int draw = press.IndexOf("out bool drawn)", StringComparison.Ordinal);
        int guard = press.IndexOf("if (drawn)", StringComparison.Ordinal);
        int cue = press.IndexOf("PlayCue(", StringComparison.Ordinal);

        Assert.True(draw >= 0, "the press no longer asks Core whether the line was new (#741).");
        Assert.True(guard > draw, "the press no longer gates on Core's own answer (#741).");
        Assert.True(cue > guard,
            "the cue is outside the was-it-new branch — a pen dragged back over an existing line would " +
            "sound the acknowledgment again (#741).");

        Assert.True(press.Contains("PlayCue(\"rum\")", StringComparison.Ordinal),
            "the acknowledgment is no longer the warm chime (#741).");
        Assert.False(press.Contains("PlayCue(\"board\")", StringComparison.Ordinal),
            "the acknowledgment has become the prize jingle a found card gets — the owner ruled the register " +
            "out by name: \"no fanfare, no XP chime\" (#741).");

        // One cue in the whole file. A second one somewhere else would be a second acknowledgment.
        Assert.Equal(1, CountOf(Pages("Map.RedPen.cs"), "PlayCue("));
    }

    private static int CountOf(string src, string needle)
    {
        int n = 0;
        for (int at = src.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = src.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    // ── WHERE THE PEN WORKS ───────────────────────────────────────────────────────────────────────────

    /// <summary>The gate is Core's one predicate, and it is asked AGAIN at the moment of the mark — the
    /// room can change under a captain who picked the pen up a minute ago (somebody takes the chair
    /// opposite, and the papers go back in the sleeve).</summary>
    [Fact]
    public void ThePen_AsksTheSameLadderTwice_AndSaysWhyNot()
    {
        string file = Pages("Map.RedPen.cs");

        Assert.True(file.Contains("Core.CaseThreads.PenRefusal(SeatedIn, SeatedAlone)", StringComparison.Ordinal),
            "the pen no longer asks Core's own ladder with this component's own two facts (#741/#784).");

        string take = Method(file, "private void TakeUpTheRedPen()");
        Assert.True(take.Contains("PenRefusal", StringComparison.Ordinal)
                    && take.Contains("_satchelOutcome", StringComparison.Ordinal),
            "taking the pen out no longer SAYS why not — a control that quietly does nothing is " +
            "indistinguishable from a broken one (#603/#680).");

        string press = Method(file, "private void NoteTitlePressed(string id)");
        Assert.True(press.Contains("PenRefusal", StringComparison.Ordinal),
            "the mark itself no longer re-checks the seat — a captain who picked the pen up before somebody " +
            "took the chair opposite could draw over their shoulder (#741/#784).");

        // The control is drawn and never disabled or hidden: the refusal is how the law is learned.
        string razor = Pages("Map.razor");
        Assert.True(razor.Contains("@onclick=\"TakeUpTheRedPen\"", StringComparison.Ordinal),
            "the razor no longer draws the pen control (#741).");
        int pen = razor.IndexOf("class=\"satchel-filter red-pen", StringComparison.Ordinal);
        Assert.True(pen >= 0, "the pen control has lost its class (#741).");
        Assert.False(razor[pen..(pen + 400)].Contains("disabled", StringComparison.Ordinal),
            "the pen control has been disabled rather than answering — #212/#603's law (#741).");
    }

    // ── THE BOOK, AND WHAT IT CARRIES HOME ────────────────────────────────────────────────────────────

    /// <summary>The lines ride in the vault beside the book they cross, and a drawn or erased line asks for
    /// a save — a case assembled over a month of excursions that did not survive a reload is not a case.</summary>
    [Fact]
    public void TheLines_RideInTheVault()
    {
        string vault = Pages("Map.Vault.cs");
        Assert.True(vault.Contains("new CaseThreadsSection", StringComparison.Ordinal),
            "the save no longer writes the red lines (#741).");
        Assert.True(vault.Contains("vault.CaseThreads?.Threads", StringComparison.Ordinal),
            "the load no longer reads them back (#741).");
        Assert.True(vault.Contains("Core.CaseThreads.Thread.TryParse", StringComparison.Ordinal),
            "the load no longer drops a pair it cannot read — the vault is tolerant everywhere else (#741).");

        Assert.Equal(2, CountOf(
            Method(Pages("Map.RedPen.cs"), "private void NoteTitlePressed(string id)"), "RequestVaultSave()"));
    }

    /// <summary>The node leads with Core's glyph law and never the raw field. Found by booting the demo:
    /// a title that prints <c>Glyph + Text</c> prints the glyph twice, because three of the four sentences
    /// a kit produces are already written with theirs.</summary>
    [Fact]
    public void TheNode_DoesNotWriteTheGlyphTwice()
    {
        string nodes = Components("NotebookNodes.razor");

        Assert.True(nodes.Contains("CaseThreads.GlyphFor(row.Note)", StringComparison.Ordinal),
            "the node no longer asks Core whether the entry already opens with its own mark (#741).");
        Assert.False(nodes.Contains("row.Note.Glyph", StringComparison.Ordinal),
            "the node prints the stored glyph straight, which doubles it on every dossier entry (#741).");
    }

    // ── THE DEMO'S SEEDS ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ?threads=1 IS THREE SEEDS. What they yield — six entries off two grounds with the same door named on
    /// both — is pinned in Core (<c>TheRedPenDrawsTheLineTests.TheDemoCase_…</c>), and this guard holds the
    /// call site to them.
    ///
    /// <para>Because the failure is completely silent: a seed quietly changed still boots six perfectly good
    /// sentences with nothing in common, and a tester would simply conclude the feature is dull. It is also
    /// how a demo can be pointed at one of the seeded rooms where the dead person and the kin waiting for
    /// word come out with the SAME full name, which reads as a bug rather than a rhyme.</para>
    /// </summary>
    [Fact]
    public void TheDemo_BootsTheSeedsCoreHasPinned()
    {
        string table = Pages("Map.Table.cs");

        Assert.True(table.Contains("FileFrom(\"miranda\", 0, 3, 4);", StringComparison.Ordinal),
            "the demo's first seed has moved — Core pins what it yields (#741).");
        Assert.True(table.Contains("FileFrom(\"luna\", 1, 22, 2);", StringComparison.Ordinal),
            "the demo's second seed has moved — Core pins what it yields (#741).");

        // The grounds are asked of Core, never typed: a cheat that writes its own geography is a cheat
        // testing a world that does not ship (§13.15, one room over).
        string prefile = Method(table, "private void PreFileTheCase()");
        Assert.True(prefile.Contains("Core.LandingSites.For(", StringComparison.Ordinal)
                    && prefile.Contains("Core.BodyNames.Display(", StringComparison.Ordinal)
                    && prefile.Contains("Core.FieldNotes.PlaceLabel(", StringComparison.Ordinal),
            "the demo has started typing its own place labels (#741).");

        // …and every word of the case is the dossier's own. The demo invents no lore.
        Assert.True(prefile.Contains("Core.FieldDossier.Debrief(", StringComparison.Ordinal),
            "the demo no longer files shipped dossier prose — it must invent nothing (#741).");

        // The instruction is SAID INSIDE THE DIALOG and never pulsed: a first descent raises its own card
        // over the whole screen on this very boot (#585), and a pulse routed under it is in the DOM and not
        // on the screen — #693's own lesson, found here by taking a screenshot.
        string boot = table[table.IndexOf("_notesView = NotesView.TheCase;", System.StringComparison.Ordinal)..];
        boot = boot[..boot.IndexOf("return;", System.StringComparison.Ordinal)];
        Assert.True(boot.Contains("_satchelOutcome =", System.StringComparison.Ordinal),
            "the demo no longer says what to press inside the dialog (#741, #680/#736).");
        Assert.False(boot.Contains("ShowPulseMessage", System.StringComparison.Ordinal),
            "the demo pulses its instruction again — it plays and dies under the first-descent card (#741).");

        // NOTHING ABOUT THE CASE IS FORCED: no line is drawn for the tester. Spotting is the player's act.
        Assert.False(prefile.Contains("CaseThreads.Draw", StringComparison.Ordinal),
            "the demo draws a line for the tester — that is the one thing this feature must never do (#741).");
        Assert.False(Method(table, "private bool _threadsCheat;").Contains("_caseThreads", StringComparison.Ordinal),
            "the demo seeds threads (#741).");
    }
}

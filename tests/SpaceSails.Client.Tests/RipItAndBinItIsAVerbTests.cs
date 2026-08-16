using System;
using System.IO;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #798 · RIP IT AND BIN IT — the client half, and the one law that must survive every future edit.
///
/// <para>Owner, live in play (2026-08-09): <i>"Now the option is to photograph the papers and leave them
/// there… I would not like to leave them at table in canteen… we need option to destroy them by ripping and
/// binning etc?"</i></para>
///
/// <h3>Why these guards are about SHAPE</h3>
/// <para>The dangerous edit here is not a wrong number, it is a HAND: the day the destroy path grows a line
/// that touches <c>_fieldNotes</c>, <c>_caseThreads</c> or the seated register's own set, the book starts
/// unlearning things and nothing on the screen says so — the entry is simply not there next time somebody
/// opens the notebook, and there is no moment at which a player could catch it. So the guard is written
/// against the METHOD's source: it asserts what the act touches and, far more importantly, what it may never
/// touch.</para>
/// </summary>
public sealed class RipItAndBinItIsAVerbTests
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

    private static string Source(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), "src", "SpaceSails.Client", .. parts]));

    /// <summary>#870 lane 6c · Re-PATHED, never re-asserted. The seat family is TWO files per subject now:
    /// the page's half — the records, the dev rows, the things a seat is a GATE on, and the forwarders — and
    /// the seat's own verbs, which moved onto <c>Map.Seating</c> behind <c>ISeatHost</c>. The source a guard
    /// reads over is BOTH, concatenated in that order, which is exactly the text it read out of one file
    /// before the verbs moved. Concatenated rather than narrowed to one half on purpose: several claims here
    /// are <c>DoesNotContain</c> over the whole subject, and pointing one at a single file would be a silent
    /// weakening.</summary>
    private static string Table() =>
        Source("Pages", "Map.Table.cs") + Source("Pages", "Seating", "Seating.Table.cs");

    private static string Seated() =>
        Source("Pages", "Map.Seated.cs") + Source("Pages", "Seating", "Seating.Seated.cs");


    /// <summary>#870 · The sim page is nine partials by subject now, so "the sim" a guard reads over is all
    /// of them — exactly the text it read out of one file before the split.</summary>
    private static string Sim() => string.Concat(
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages"), "Map.Sim*.cs")
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    private static string Doc(string name) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "docs", name));

    /// <summary>The body of one method in <c>Map.Bin.cs</c>, from its signature to the closing brace at its
    /// own indent. Sliced rather than grepped over the whole file, because "does this FILE mention the
    /// notebook" and "does this ACT touch the notebook" are two different questions and only the second one
    /// is the law.</summary>
    private static string MethodBody(string signature)
    {
        string src = Source("Pages", "Map.Bin.cs");
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at > 0, $"Map.Bin.cs no longer has `{signature}` — this guard cannot see the act.");
        int end = src.IndexOf("\n    }", at, StringComparison.Ordinal);
        Assert.True(end > at, $"`{signature}` has no end this guard can find.");
        return src[at..end];
    }

    /// <summary>#870 lane 6′a · The body of one of the patrol family's own questions, from
    /// <c>Map.Patrol.cs</c>. Cut the same way <see cref="MethodBody"/> cuts the bin's, and for the same
    /// reason: the rota rung of this ladder is asked of the round now, and a claim about what that question
    /// is made of has to be read where the question is.</summary>
    private static string RoundQuestion(string signature)
    {
        string src = Source("Pages", "Map.Patrol.cs");
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at > 0, $"Map.Patrol.cs no longer has `{signature}` — this guard cannot see the rung.");
        int end = src.IndexOf("\n    }", at, StringComparison.Ordinal);
        Assert.True(end > at, $"`{signature}` has no end this guard can find.");
        return src[at..end];
    }

    /// <summary>
    /// #828 · THE WHOLE DESTROY PATH — the methods a sheet passes through on its way out of the game, read
    /// as one body.
    ///
    /// <para>It used to be a single method. The secure rung put a WATCHED BEAT in front of the act (the
    /// press opens <c>Processing.Work.Shred</c>; the far end of the hold calls the act), so the gates and
    /// the act are separate methods now — and the law below is about the PATH, not about whichever half a
    /// future edit happens to leave the old name on.</para>
    /// </summary>
    private static string TheDestroyPath() =>
        MethodBody("private void RipItUp(")
        + "\n" + MethodBody("private void TheDestructionWasWatched(")
        + "\n" + MethodBody("private void TheSheetIsGone(");

    /// <summary>The same body with every comment taken out. The law is about what the code DOES: a comment
    /// naming the three lists this act may not touch is the file explaining itself, and a guard that could
    /// not tell that from a hand on one of them would be unwritable — this one shipped red on its own
    /// documentation the first time it ran.</summary>
    private static string CodeOnly(string body)
    {
        var kept = new System.Text.StringBuilder();
        foreach (string line in body.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("///", StringComparison.Ordinal))
            {
                continue;
            }
            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            kept.AppendLine(slashes >= 0 ? line[..slashes] : line);
        }
        return kept.ToString();
    }

    // ── (a) THE ONE LAW ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #798 · THE BOOK NEVER UNLEARNS.
    ///
    /// <para>Destroying an original destroys the EVIDENCE and nothing else. What the captain had already dug
    /// out of a sheet is in the book in their own hand; a hand-written page does not come apart because the
    /// paper it was copied from did, and neither does a red line somebody drew between two of them (#741).
    /// </para>
    ///
    /// <para>The act touches exactly one list — the sleeve — and this asserts the absence of every other
    /// hand it could grow. It also asserts it never goes to the GROUND: #615's leave verb writes the item
    /// onto the square you are standing on and <c>TryPickUpWhatYouLeft</c> hands it straight back, which is
    /// the precise opposite of what this verb is for.</para>
    /// </summary>
    [Fact]
    public void THE_BOOK_NEVER_UNLEARNS_WhenTheOriginalGoes()
    {
        string act = TheDestroyPath();

        // The sleeve loses the row — through the one funnel every item in the game goes through.
        Assert.Contains("Core.Satchel.Remove(_satchel", act, StringComparison.Ordinal);

        // …and the book GAINS a line, because the act is a fact worth keeping.
        Assert.Contains("FileNote(RipAndBin.DisposalNote(", act, StringComparison.Ordinal);

        // …and nothing in it can take anything away from anywhere else. Read off the CODE, with the
        // commentary stripped: the method's own docs name these three lists, in the sentence that promises
        // never to touch them.
        string code = CodeOnly(act);
        foreach (string hand in new[]
        {
            "_fieldNotes", "_caseThreads", "WrittenUpProperly", "CaseThreads.Erase", ".Ground.Leave",
            "SetItDown", "_notesExpanded", "_penHoldingId",
        })
        {
            Assert.False(code.Contains(hand, StringComparison.Ordinal),
                $"the destroy path touches `{hand}` — the book has grown a way to unlearn something, and "
                + "nothing on the screen would ever say so.");
        }
    }

    // ── (b) THE VERB IS GATED ON A BIN, AND SAYS SO ───────────────────────────────────────────────────

    /// <summary>
    /// #798 · NO BIN, NO RIP — and the refusal is a SENTENCE.
    ///
    /// <para>Owner's design: <i>"ripped-and-pocketed scraps are their own smaller liability."</i> The verb
    /// wants somewhere to put the pieces, and a control that quietly did nothing would be indistinguishable
    /// from a broken one (#603) — so both gates answer out loud, through Core's own words, and the hint on
    /// the control says which bucket it is BEFORE the press.</para>
    /// </summary>
    [Fact]
    public void THE_VERB_IsGatedOnABinAndRefusesOutLoud()
    {
        string act = TheDestroyPath();

        Assert.Contains("RipAndBin.IsEvidence(item.Kind)", act, StringComparison.Ordinal);
        Assert.Contains("RipAndBin.NotEvidenceLine", act, StringComparison.Ordinal);
        Assert.Contains("BinWithinReach() is not { } bin", act, StringComparison.Ordinal);
        Assert.Contains("RipAndBin.NoBinLine", act, StringComparison.Ordinal);

        // Which bin it is comes from Core's one answer, asked with the boots' own coordinates — so the
        // control that is drawn, the hint that explains it and the act that fires cannot disagree.
        string bin = Source("Pages", "Map.Bin.cs");
        Assert.Contains("RipAndBin.NearestWithinReach(_avatarX, _avatarY, BinsHere())", bin,
            StringComparison.Ordinal);

        // …and the TIER is on the filed fact, which is the whole of a tier's mechanical existence today.
        Assert.Contains("DisposalNote(label, bin.Tier)", act, StringComparison.Ordinal);
        Assert.Contains("RippedLine(label, bin.Tier)", act, StringComparison.Ordinal);
    }

    /// <summary>#798 · The witness line is filed ONLY when there was a witness — and the ladder that decides
    /// is Core's, so it can be tested in both directions rather than trusted at a call site.</summary>
    [Fact]
    public void THE_SEEN_FACT_IsFiledOnlyWhenSomebodyWasLooking()
    {
        string act = TheDestroyPath();

        int guard = act.IndexOf("if (WhoIsWatchingYouRip() is { } who)", StringComparison.Ordinal);
        Assert.True(guard > 0, "the witness line is not behind a witness.");

        int filed = act.IndexOf("FileNote(RipAndBin.SeenNote(", StringComparison.Ordinal);
        Assert.True(filed > guard,
            "the seen-fact is filed outside the branch that asks whether anybody saw it — every rip in the "
            + "game would come with a witness.");

        // The ladder itself is Core's law and not a chain of ifs in a client file.
        //
        // #870 lane 6′a · RE-NEEDLED, never re-asserted. The rota rung is one question the patrol family
        // answers about itself now (TheRoundHasEyesOnYou, Map.Patrol.cs) — the bin no longer walks the guard
        // list. The predicate underneath it is unchanged, and it is asserted where it lives, so "the same
        // predicate the challenge runs on" is still a fact this guard can fail on.
        string who = MethodBody("private RipAndBin.Watcher? WhoIsWatchingYouRip(");
        Assert.Contains("RipAndBin.WhoSaw(", who, StringComparison.Ordinal);
        Assert.Contains("TheRoundHasEyesOnYou(", who, StringComparison.Ordinal);
        Assert.Contains(
            "PatrolBeat.Notices(", RoundQuestion("private bool TheRoundHasEyesOnYou("),
            StringComparison.Ordinal);
        Assert.Contains(
            "PatrolBeat.CanBeNoticed(", RoundQuestion("private bool TheRoundHasEyesOnYou("),
            StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.EyesOn(", who, StringComparison.Ordinal);
        Assert.DoesNotContain("return RipAndBin.Watcher.", who, StringComparison.Ordinal);
    }

    /// <summary>#798 · …and the ladder answers NULL as readily as it answers a name. Core's own function,
    /// swept in both directions: nobody looking is the ordinary case, and it must be reachable.</summary>
    [Fact]
    public void THE_LADDER_AnswersNobodyAsReadilyAsSomebody()
    {
        Assert.Null(RipAndBin.WhoSaw(false, false, false, false));

        Assert.Equal(RipAndBin.Watcher.TheRota, RipAndBin.WhoSaw(true, false, false, false));
        Assert.Equal(RipAndBin.Watcher.TheKeep, RipAndBin.WhoSaw(false, true, false, false));
        Assert.Equal(RipAndBin.Watcher.TheChairOpposite, RipAndBin.WhoSaw(false, false, true, false));
        Assert.Equal(RipAndBin.Watcher.TheNextTable, RipAndBin.WhoSaw(false, false, false, true));

        // Top-down, first rung wins: a man whose job is looking outranks a stranger who glanced up.
        Assert.Equal(RipAndBin.Watcher.TheRota, RipAndBin.WhoSaw(true, true, true, true));
        Assert.Equal(RipAndBin.Watcher.TheKeep, RipAndBin.WhoSaw(false, true, true, true));
        Assert.Equal(RipAndBin.Watcher.TheChairOpposite, RipAndBin.WhoSaw(false, false, true, true));
    }

    // ── (c) THE CONTROL IS ON BOTH PAGES, AND NEVER DISABLED ──────────────────────────────────────────

    /// <summary>
    /// #798 · THE VERB IS WHERE THE PAPER IS.
    ///
    /// <para>Owner's loop is <i>sit, dig, book, BIN</i> — so the shredder is on the SPREAD page, beside the
    /// dig it follows, and on the CARRIED page, where a captain standing at a bin in a corridor meets it.
    /// One verb, two places, and no control disabled anywhere (#212/#603): the refusal is how the law about
    /// needing a bin gets learned.</para>
    /// </summary>
    [Fact]
    public void THE_SHREDDER_IsOnTheSpreadAndOnThePocketAndIsNeverDisabled()
    {
        string razor = Source("Pages", "Map.razor");

        int page = razor.IndexOf("@if (_satchelPage == SatchelPage.Spread)", StringComparison.Ordinal);
        Assert.True(page > 0, "the satchel has no spread page.");
        string spread = razor[page..razor.IndexOf(
            "else if (_satchelPage == SatchelPage.Notes)", page, StringComparison.Ordinal)];

        Assert.Contains("RipItUp(dig)", spread, StringComparison.Ordinal);
        Assert.Contains("RipHint(dig)", spread, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=", spread, StringComparison.Ordinal);

        // …and on the pocket's own row template, beside 🫳 and ✍.
        int row = razor.IndexOf("RenderFragment<SpaceSails.Core.Satchel.Item> Row = item =>",
            StringComparison.Ordinal);
        Assert.True(row > 0, "the carried page no longer has a row template.");
        int rowEnd = razor.IndexOf("</div>;", row, StringComparison.Ordinal);
        Assert.True(rowEnd > row, "the row template no longer ends where this guard can find it.");
        string carried = razor[row..rowEnd];
        Assert.Contains("RipItUp(item)", carried, StringComparison.Ordinal);
        Assert.Contains("RipHint(item)", carried, StringComparison.Ordinal);

        // The glyph is Core's, once — a second copy typed into the razor is how a control and its own
        // sentence come to wear two different marks.
        Assert.Contains("SpaceSails.Core.RipAndBin.Glyph", spread, StringComparison.Ordinal);
        Assert.Contains("SpaceSails.Core.RipAndBin.Glyph", carried, StringComparison.Ordinal);
    }

    /// <summary>
    /// #798 · A PAPER YOU HAVE JUST DUG IS STILL ON THE TABLE.
    ///
    /// <para>The spread used to list only what still had something to dig out of it, which was right while
    /// the page had one verb on it — and became a hole the moment it had two: <i>sit, dig, book, BIN</i>
    /// could not be finished without shutting the spread and reopening the pocket, because the paper you had
    /// just worked on had vanished off the page. The list is the sleeve's documents now, and the dig control
    /// says which of them are done.</para>
    /// </summary>
    [Fact]
    public void THE_SPREAD_KeepsThePaperYouHaveJustDug()
    {
        string seated = Seated();
        int at = seated.IndexOf("private List<Core.Satchel.Item> SpreadableFinds()", StringComparison.Ordinal);
        Assert.True(at > 0, "the spread no longer builds its own rows.");
        string body = seated[at..(at + 500)];

        Assert.Contains("CanWriteUp(item) || Core.RipAndBin.IsEvidence(item.Kind)", body,
            StringComparison.Ordinal);

        // …and the row says so rather than going on offering an evening's work that is already done.
        Assert.Contains("CanWriteUp(item) ? \"dig it out →\" : \"already in the book\"", seated,
            StringComparison.Ordinal);
    }

    // ── (d) THE FIXTURES ARE DRAWN WHERE CORE PUT THEM ────────────────────────────────────────────────

    /// <summary>#798 · The renderer plates the bins and MEASURES NOTHING. The box is already in
    /// <c>floor.Walls</c>, so it was drawn and collided with by the wall loop — a renderer laying out its own
    /// bin would be one more caller doing geometry about a room it does not own (§13.15), which is the
    /// mistake that has set this project's captain down inside a wall twice.</summary>
    [Fact]
    public void THE_RENDERER_PlatesTheBinsAndMeasuresNothing()
    {
        string hive = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "SpaceSails.Client", "Rendering", "HiveInterior.cs"));

        int at = hive.IndexOf("foreach (RipAndBin.Bin bin in floor.TheBins)", StringComparison.Ordinal);
        Assert.True(at > 0, "the bins are not drawn at all.");
        int blockEnd = hive.IndexOf("\n        }", at, StringComparison.Ordinal);
        Assert.True(blockEnd > at, "the bin-plating loop has no end this guard can find.");
        string block = hive[at..blockEnd];

        Assert.Contains("labels.Add(", block, StringComparison.Ordinal);
        Assert.Contains("bin.Plate", block, StringComparison.Ordinal);

        // No second rectangle for the eye, and no console that answers nothing (#757's own complaint).
        Assert.DoesNotContain("walls.Add(", block, StringComparison.Ordinal);
        Assert.DoesNotContain("consoles.Add(", block, StringComparison.Ordinal);
    }

    // ── (e) THE ROW A PLAYTESTER PRESSES ──────────────────────────────────────────────────────────────

    /// <summary>#798 · A scene nobody can reach on demand is a scene that ships broken (#693). The row
    /// exists, it walks the captain to the bin's OWN published standing spot rather than to a coordinate the
    /// cheat made up, and the guide says the same thing in prose.</summary>
    [Fact]
    public void THE_DEV_ROW_PutsTheCaptainAtABinWithPapersOnThem()
    {
        Assert.Contains(DevStarts.All, e => e.Url.Contains("rip=1", StringComparison.Ordinal));

        string table = Table();
        Assert.Contains("_ripCheat && TheSlopBinIn(ex, a) is { } bin", table, StringComparison.Ordinal);
        Assert.Contains("StandCaptainAt(bin.StandX, bin.StandY", table, StringComparison.Ordinal);
        Assert.Contains("SeedTheSpreadFinds();", table, StringComparison.Ordinal);

        string sim = Sim();
        Assert.Contains("pair.StartsWith(\"rip=\"", sim, StringComparison.Ordinal);

        Assert.Contains("?rip=1", Doc("testing-guide.md"), StringComparison.Ordinal);
    }
}

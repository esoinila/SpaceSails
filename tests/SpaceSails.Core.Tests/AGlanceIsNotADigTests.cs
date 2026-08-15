using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #828 · TWO WAYS OF READING A SHEET, AND WHAT EACH ONE IS WORTH.
///
/// <para>Owner, naming the semantic the loop already leaned on: <i>"now the read from inventory and dig at
/// it… If we dig at something then we have really read it with care"</i> — and, thinking it through out
/// loud and landing the ruling: <i>"dig at it is like reading with steroids… I guess read is like quick
/// glance and dig at it is carefully reading and comparing the document against other sources and
/// notes."</i></para>
///
/// <list type="number">
/// <item><b>The GLANCE</b> — inventory, standing, anywhere, free, as often as you like. You see the sheet's
/// FACE. It files NOTHING.</item>
/// <item><b>The DIG</b> — spread, seated, the progress bar. Reading with care, which in this game means
/// COMPARING the page against the notes in your own hand. It is the only act that extracts.</item>
/// </list>
///
/// <h3>The completeness law</h3>
/// <para>Owner, settling what a dig is worth: <i>"so after the dig at it we should have all we need from
/// those papers in pics / notes."</i> So the dug entry must carry everything of value the sheet can show —
/// and the consequence is the whole point of the issue this file belongs to: <b>after a proper dig the
/// paper is informationally spent, and binning it can never cost the captain knowledge.</b> It only changes
/// who else might ever hold the pieces, which keeps <i>the bin is a bet</i> a bet about other people's
/// eyes.</para>
///
/// <para>The glance's other half — that it leaves the book and the case byte-identical — is guarded where a
/// glance can actually be PRESSED, in the client's <c>TheDisposalYouWatchTests</c>. A purity law has to be
/// stated against a running world; asserted here it would only be asserting that a pure function is pure.
/// </para>
/// </summary>
public sealed class AGlanceIsNotADigTests
{
    /// <summary>Somewhere to be standing. The disposition clause quotes it and nothing else here reads it.</summary>
    private const string Where = "on the floor of B3";

    /// <summary>The two kinds of paper this game has — the pair <see cref="RipAndBin.IsEvidence"/> names and
    /// the pair <see cref="LeftBehind.GistOf"/> has a gist for. Asked of Core rather than typed out, so a
    /// third kind of document arriving tomorrow is swept by every law below without anybody remembering to
    /// add it.</summary>
    private static IEnumerable<Satchel.Kind> PaperKinds() =>
        Enum.GetValues<Satchel.Kind>().Where(RipAndBin.IsEvidence);

    /// <summary>A wide net of ids per kind. The paper's body, its title and its certainty are all rolled off
    /// the id, so one id proves one paper and nothing about the other five.</summary>
    private static IEnumerable<string> ManyIds() =>
        Enumerable.Range(0, 120).Select(i => $"hive:doc:sweep-{i}");

    /// <summary>One long line: a page as a book entry can carry it. The book files one entry per act and the
    /// notebook splits it by SENTENCE, so a paragraph break inside one is a line the strip prints with a
    /// hole in it — which is why the entry flattens the page and why this guard compares flattened.</summary>
    private static string Flat(string s) =>
        string.Join(" ", s.Split(
            [' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries));

    // ── (a) THE COMPLETENESS LAW ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · A DIG LEAVES NOTHING OF VALUE ON THE SHEET.
    ///
    /// <para>For every kind of paper and a hundred and twenty of each: dig it, then compare the book entry
    /// against the face the sheet can show. Every sentence the glance can put in front of the captain has to
    /// be reachable from the entry afterwards — the pencilled ticks, the torn fold, the second hand that
    /// stops recording and does not say why. Those details are the only things a later arc could ever hang a
    /// connection on, and a captain who dug a sheet and then binned it was losing all of them.</para>
    ///
    /// <para><b>Proven RED</b> by script-reverting <c>LeftBehind.GistOf</c>'s paper arm to the entry it
    /// filed before this issue (heading and verdict, no page):</para>
    /// <code>
    /// 240 of 240 dug sheet(s) lost something the sheet was showing:
    ///   Paper hive:doc:sweep-0: the book entry has lost "Half a shipping manifest, torn on the fold. What
    ///     is left is the ROUTE — where it left, where it called, and the last line, which is a place and
    ///     not a port."
    ///   Paper hive:doc:sweep-0: the book entry has lost "And there is a description: a landform, a bearing
    ///     taken off it, a distance somebody paced rather than measured. Enough to walk to in an afternoon."
    ///   Paper hive:doc:sweep-1: the book entry has lost "There is a place named on it. Only named — the
    ///     way you name somewhere you have never had to find."
    /// </code>
    ///
    /// <para>…and <see cref="THE_TABLE_BuysSomethingTheGroundDoesNot"/> goes red in the same run, on the
    /// page that is supposed to be in the seated entry and is not.</para>
    /// </summary>
    [Fact]
    public void THE_DIG_LeavesNothingOfValueOnTheSheet()
    {
        var lost = new List<string>();
        int dug = 0, sentences = 0;

        foreach (Satchel.Kind kind in PaperKinds())
        {
            foreach (string id in ManyIds())
            {
                var item = new Satchel.Item(kind, id);

                // The DUG entry — the seated register's own disposition (#784: the sheet stays in the
                // sleeve), which is the reading this law binds.
                string? book = LeftBehind.GistOf(item, Where, kept: true);
                Assert.False(string.IsNullOrWhiteSpace(book),
                    $"{kind} {id}: a dig files nothing at all, so there is no completeness to speak of.");
                dug++;
                string flatBook = Flat(book!);

                // …and the FACE: what the glance can show. Null is a true answer for a kind the game has
                // never drawn a page for, and a kind with no face cannot lose one.
                if (CarriedObject.Card(item, "luna") is not { } face)
                {
                    continue;
                }

                foreach (string sentence in face.Story.Split(
                    ['\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    sentences++;
                    if (!flatBook.Contains(Flat(sentence), StringComparison.Ordinal))
                    {
                        lost.Add($"  {kind} {id}: the book entry has lost \"{sentence}\"");
                    }
                }

                // The heading half, which the entry has always had and must go on having: what the paper
                // calls itself, and how well it pins a place.
                if (kind == Satchel.Kind.Paper)
                {
                    Assert.Contains(FieldClue.Title(id), flatBook, StringComparison.Ordinal);
                    Assert.Contains(FieldClue.Label(FieldClue.CertaintyOf(id)), flatBook,
                        StringComparison.Ordinal);
                }
            }
        }

        Assert.True(dug >= 200, $"only {dug} sheet(s) were dug — this net proves nothing.");
        Assert.True(sentences >= 200,
            $"only {sentences} sentence(s) of face were compared — the sheets have stopped showing "
            + "anything and this law has nothing left to be about.");
        Assert.True(lost.Count == 0,
            $"{lost.Count} of {sentences} dug sheet(s) lost something the sheet was showing:\n"
            + string.Join("\n", lost.Take(12)));
    }

    /// <summary>
    /// #828 · …AND THE STANDING READING IS DELIBERATELY POORER, WHICH IS THE TWO TIERS WITH A DIFFERENCE
    /// YOU CAN READ IN THE BOOK.
    ///
    /// <para>#691's photograph-and-leave files a heading and a verdict: you read it, you put it down, and
    /// the book keeps what it was. The seated dig copies the PAGE out. If the two ever file the same entry,
    /// the table has stopped buying anything and the whole second tier is decoration.</para>
    /// </summary>
    [Fact]
    public void THE_TABLE_BuysSomethingTheGroundDoesNot()
    {
        var paper = new Satchel.Item(Satchel.Kind.Paper, "hive:doc:the-manifest");

        string standing = LeftBehind.GistOf(paper, Where)!;
        string seated = LeftBehind.GistOf(paper, Where, kept: true)!;

        Assert.NotEqual(standing, seated);
        Assert.True(seated.Length > standing.Length,
            "the seated entry is not the longer of the two — the dig has stopped copying the page out.");

        // The page is in ONE of them, and it is the one you had to sit down for.
        string page = Flat(FieldClue.Document(paper.Id));
        Assert.Contains(page, Flat(seated), StringComparison.Ordinal);
        Assert.DoesNotContain(page, Flat(standing), StringComparison.Ordinal);
    }

    /// <summary>#828 · One entry, one paragraph. The field book files one note per act and the notebook
    /// reads it back sentence by sentence (<c>CaseThreads.BulletsOf</c>), so a blank line inside a single
    /// entry is a break the notebook and the docked strip disagree about. Found the hard way once already,
    /// one register over.</summary>
    [Fact]
    public void THE_ENTRY_IsOneParagraphHoweverLongThePageIs()
    {
        foreach (Satchel.Kind kind in PaperKinds())
        {
            foreach (string id in ManyIds().Take(30))
            {
                foreach (bool kept in new[] { false, true })
                {
                    string entry = LeftBehind.GistOf(new Satchel.Item(kind, id), Where, kept)!;
                    Assert.DoesNotContain("\n", entry, StringComparison.Ordinal);
                    Assert.DoesNotContain("  ", entry, StringComparison.Ordinal);
                }
            }
        }
    }

    // ── (b) THE DIG'S FICTION IS COMPARISON ───────────────────────────────────────────────────────────

    /// <summary>
    /// #828 · THE BAR SAYS WHAT THE HANDS ARE DOING.
    ///
    /// <para>Owner: <i>"dig at it is carefully reading and COMPARING the document against other sources and
    /// notes."</i> That is why the act happens seated at a spread — the notes are physically out — and why
    /// its output is a mention connected to the case rather than a copy of the sheet. A bar that only said
    /// "digging" was describing attention; these say cross-referencing, which is the act.</para>
    ///
    /// <para>Stated on the SEATED register alone: a captain photographing a sheet on the regolith is not
    /// comparing it against anything, and a guard that demanded the word everywhere would be asking the
    /// standing registers to lie.</para>
    ///
    /// <para><b>Proven RED</b> by script-reverting both dig lines to the wording that shipped before this
    /// issue (<i>"start digging into it for whatever it actually says"</i> / <i>"Digging through"</i>):</para>
    /// <code>
    /// Assert.Contains() Failure: Sub-string not found
    /// String:    "✍ You lay 📋 a pay sheet out on the table"···
    /// Not found: "compar"
    /// </code>
    /// </summary>
    [Fact]
    public void THE_DIGS_FICTION_IsComparisonAndTheBarSaysSo()
    {
        string start = Processing.StartLine(Processing.Work.Write, "📋 a pay sheet", 20);
        string running = Processing.HoldLine(Processing.Work.Write, "📋 a pay sheet", 7);

        foreach (string said in new[] { start, running })
        {
            Assert.Contains("compar", said, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("notes", said, StringComparison.OrdinalIgnoreCase);
        }

        // …and the other registers do not borrow the word. Photographing a sheet against a tracker's map is
        // not cross-referencing it against a book that is in your pocket.
        foreach (Processing.Work work in new[]
        {
            Processing.Work.File, Processing.Work.Read, Processing.Work.Shred,
        })
        {
            Assert.DoesNotContain("comparing it against the notes",
                Processing.StartLine(work, "📋 a pay sheet", 20), StringComparison.OrdinalIgnoreCase);
        }
    }
}

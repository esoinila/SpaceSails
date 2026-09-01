using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1052 (L2) · <b>ONE SCRIM AT A TIME.</b>
///
/// <para>#1052's law, verbatim: <i>"Two <c>.view-object-backdrop</c>s must never stack… A second scrim-card
/// raise while one is up is refused or queued behind the first, never stacked."</i></para>
///
/// <h3>The collision this law is about, which is live today</h3>
///
/// <para>A captain sits at a top in a docked bar and presses <kbd>6</kbd>: the galley card goes up over a
/// full-viewport dim. Harlan Fess is working the berth, crosses the floor on the real A* and arrives — and
/// his pitch went up as a SECOND <c>.view-object-backdrop</c> over the first. Two dims multiply. The room
/// the #784 seated dock exists to keep visible went to near-black, the card underneath became unreadable and
/// unreachable, and nothing in the client refused it because nothing in the client could see it happening.
/// </para>
///
/// <h3>Three guards, three different jobs</h3>
///
/// <list type="number">
/// <item><b><see cref="TheCensusNamesEveryScrimTheMarkupDraws"/> — the completeness guard.</b> The arbiter
/// is only as honest as its census, so the census is checked against the markup AS TYPED: every element in
/// <c>Map.razor</c> wearing <c>view-object-backdrop</c> is behind an <c>@if</c>, and every one of those
/// conditions has to be a row in <c>TheScrimCensus</c>. A new card with a scrim on it joins this law on the
/// day it is typed, with no edit in the client and none here.</item>
/// <item><b><see cref="ASecondScrimIsHeldBehindTheFirstAndArrivesWhenItClears"/> — the law, driven.</b> The
/// live collision, played: galley card up, salesman arrives through his own shipping arrival verb, and the
/// render that follows has exactly ONE scrim in it. Then the card comes down and he speaks.</item>
/// <item><b><see cref="TheQueueIsPumpedByTheWalkedFrame"/> — the wire.</b> A queue nothing empties is a card
/// that never arrives, which would be the #603 class wearing this law's clothes.</item>
/// </list>
/// </summary>
public sealed class OnlyOneScrimAtATimeTests
{
    /// <summary>A free top in a docked berth's bar — the one world where the galley card and the salesman
    /// can both be raised, which is to say the world the collision happens in.</summary>
    private const string SatAtABarTop = "/map?barcase=1";

    private const string TheScrim = "view-object-backdrop";

    // ── (1) THE CENSUS IS COMPLETE ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY SCRIM IN THE MARKUP IS NAMED IN THE CENSUS, AND THE CENSUS NAMES NOTHING ELSE.</b>
    ///
    /// <para>Read off <c>Map.razor</c> as typed, because that is the only reading that stays true for a card
    /// nobody has written a driver for yet. The gate recorded for a scrim written in an <c>else</c> is the
    /// <c>@if</c> it hangs off — there is exactly one (the seated CONVERSATION card, whose scrim is the else
    /// of <c>@if (SeatedIsDocked)</c>, the strip branch drawing no scrim at all).</para>
    ///
    /// <para><b>RED PROOF:</b> delete any row from <c>TheScrimCensus</c> and this fails naming the gate the
    /// markup still has (<i>"a scrim the arbiter cannot see: _galleyCardOpen"</i>); add a row for a gate
    /// that draws no scrim and it fails from the other side.</para>
    /// </summary>
    [Fact]
    public async Task TheCensusNamesEveryScrimTheMarkupDraws()
    {
        HashSet<string> inTheMarkup = TheGatesTheMarkupDrawsAScrimBehind();
        Assert.True(inTheMarkup.Count > 20,
            $"only {inTheMarkup.Count} scrims were found in Map.razor — the reader has stopped seeing them, "
            + "and a completeness guard that finds nothing passes for the wrong reason.");

        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);
        var census = (IEnumerable<(string Gate, bool Up)>)bench.Call("TheScrimCensus")!;
        HashSet<string> named = census.Select(row => Squashed(row.Gate)).ToHashSet(StringComparer.Ordinal);

        string[] missing = [.. inTheMarkup.Where(g => !named.Contains(g)).OrderBy(g => g, StringComparer.Ordinal)];
        Assert.True(missing.Length == 0,
            $"{missing.Length} scrim(s) the arbiter cannot see. #1052's law is that two .{TheScrim}s never "
            + "stack, and a card whose gate is not in TheScrimCensus can stack on anything and be stacked on "
            + "by anything:\n  - " + string.Join("\n  - ", missing));

        string[] stale = [.. named.Where(g => !inTheMarkup.Contains(g)).OrderBy(g => g, StringComparer.Ordinal)];
        Assert.True(stale.Length == 0,
            $"{stale.Length} census row(s) name a gate Map.razor no longer draws a scrim behind. A census "
            + "that has drifted from the page is a census that arbitrates about half a screen:\n  - "
            + string.Join("\n  - ", stale));
    }

    // ── (2) THE LAW, DRIVEN ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE SALESMAN WAITS BEHIND THE GALLEY CARD, AND SPEAKS WHEN IT COMES DOWN.</b>
    ///
    /// <para>Played through the shipping seams: the card is raised through <c>OpenGalleyCard</c> (the one
    /// door all three of its own doors funnel through, #1021), and the salesman arrives through
    /// <c>HeReachesYourTable</c> — the callback his walker fires when his legs land him at the table
    /// (Map.BarWalkers.cs). Nothing here pokes <c>_repCard</c>: a fork that stopped going through the arbiter
    /// fails here rather than nowhere.</para>
    ///
    /// <para>The verdict is read off the GLASS — how many elements wearing the scrim were drawn — and not
    /// off a field, because "two scrims stacked" is a fact about what was painted.</para>
    ///
    /// <para><b>RED PROOF:</b> restore <c>HeReachesYourTable</c>'s old body (raise the pitch directly instead
    /// of through <c>RaiseAScrimCard</c>) and this fails — <i>"2 scrims on the glass at once"</i> — which is
    /// the shipped behaviour before this lane.</para>
    /// </summary>
    [Fact]
    public async Task ASecondScrimIsHeldBehindTheFirstAndArrivesWhenItClears()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);

        bench.CallOnTheDispatcher("OpenGalleyCard");
        Assert.Equal(1, ScrimsOnTheGlass(await bench.RenderAsync()));

        bench.CallOnTheDispatcher("HeReachesYourTable");
        DeskBench.Painted painted = await bench.RenderAsync();

        Assert.Equal(1, ScrimsOnTheGlass(painted));
        Assert.True(bench.Field("_repCard") is null,
            "the salesman's pitch went up over the galley card — two scrims, stacked (#1052).");

        // He is HELD, not turned away: the beat is still owed and the arbiter is still holding it.
        Assert.True(bench.Field("_scrimQueued") is not null,
            "the arrival was dropped rather than queued — a beat thrown away because a card was up is the "
            + "#603 class (a control that quietly does nothing).");

        // The captain shuts the card, the room's own frame lets the held card through, and he speaks.
        bench.CallOnTheDispatcher("CloseGalleyCard");
        bench.CallOnTheDispatcher("PumpTheScrimQueue");
        painted = await bench.RenderAsync();

        Assert.True(bench.Field("_repCard") is not null,
            "the held pitch never arrived once the glass cleared — a queue nothing empties is a card that "
            + "never comes.");
        Assert.Equal(1, ScrimsOnTheGlass(painted));
    }

    /// <summary>
    /// <b>…AND A CAPTAIN WHO GOT UP AND WALKED OFF IS NOT HANDED THE PITCH LATER.</b>
    ///
    /// <para>The other half of holding a card: a held card is a card about a moment, and the moment can end
    /// while the captain is reading something else. Both raisers pass a <c>stillWanted</c> the arbiter asks
    /// again at the far end — the same discipline the room's own approach planner already keeps.</para>
    ///
    /// <para><b>RED PROOF:</b> drop the <c>StillWanted</c> check from <c>PumpTheScrimQueue</c> and this
    /// fails — a salesman pitching at an empty chair.</para>
    /// </summary>
    [Fact]
    public async Task AHeldCardIsDroppedIfNobodyIsSittingThereAnyMore()
    {
        using DeskBench bench = await DeskBench.BootAsync(SatAtABarTop);
        await bench.RenderAsync();   // the page has to be mounted before a verb may ask it to repaint

        bench.CallOnTheDispatcher("OpenGalleyCard");
        bench.CallOnTheDispatcher("HeReachesYourTable");
        Assert.True(bench.Field("_scrimQueued") is not null, "nothing was queued, so nothing can go stale.");

        bench.CallOnTheDispatcher("StandUpFromTable");
        bench.CallOnTheDispatcher("CloseGalleyCard");
        bench.CallOnTheDispatcher("PumpTheScrimQueue");

        Assert.True(bench.Field("_repCard") is null,
            "the held pitch was delivered to a chair the captain had left (#1052).");
        Assert.True(bench.Field("_scrimQueued") is null, "the stale card is still sitting in the queue.");
    }

    // ── (3) THE WIRE ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>THE WALKED FRAME EMPTIES THE QUEUE.</b>
    ///
    /// <para>Read off the source, because the frame itself needs a browser clock this bench does not have —
    /// and because what can go wrong is precisely a line going missing. <c>PumpTheScrimQueue</c> has to be
    /// called from the same block that steps the bar's own walkers, which is the one block that runs in
    /// every room a queued card can be raised in.</para>
    ///
    /// <para><b>RED PROOF:</b> delete the call from <c>Map.Sim.Tick.cs</c> and this fails; the drive above
    /// still passes, because it pumps by hand — which is exactly why this guard is separate.</para>
    /// </summary>
    [Fact]
    public void TheQueueIsPumpedByTheWalkedFrame()
    {
        string tick = File.ReadAllText(Path.Combine(ClientSource(), "Pages", "Map.Sim.Tick.cs"));
        int walkers = tick.IndexOf("AdvanceBarWalkers(dtRealSeconds);", StringComparison.Ordinal);
        Assert.True(walkers >= 0, "the walked frame no longer steps the bar's walkers where this guard reads.");

        int pump = tick.IndexOf("PumpTheScrimQueue();", StringComparison.Ordinal);
        Assert.True(pump > walkers,
            "the walked frame does not empty the scrim queue after stepping the room. A card held behind a "
            + "scrim would then wait forever, which is a worse bug than the stacking it was refusing (#1052).");
    }

    // ── Reading the page and the markup back ──────────────────────────────────────────────────────────

    /// <summary>How many elements wearing the scrim were painted — the family root only, since
    /// <c>rep-backdrop</c> and <c>satchel-backdrop</c> are MODIFIERS worn in the same attribute (#992).
    /// Static-markup blobs are counted too: Razor collapses a run of static HTML into one blob and those
    /// elements never become nodes at all.</summary>
    private static int ScrimsOnTheGlass(DeskBench.Painted painted) =>
        painted.Root.Descendants().Count(n => n.HasClass(TheScrim) && !n.Hidden)
        + painted.MarkupBlobs.Sum(blob => Regex.Matches(blob, $@"class=""[^""]*\b{TheScrim}\b").Count);

    /// <summary>
    /// Every <c>@if</c> condition in <c>Map.razor</c> that a <c>.view-object-backdrop</c> is drawn behind,
    /// read off the file as typed.
    ///
    /// <para>Razor block comments are tracked across lines and skipped, in the idiom
    /// <c>TheGalleyIsACardNotADeskTests</c> established: this repository's comments QUOTE the code they
    /// discuss, so a reader that could not tell a comment from a branch would report the notes beside these
    /// cards as extra surfaces.</para>
    /// </summary>
    private static HashSet<string> TheGatesTheMarkupDrawsAScrimBehind()
    {
        string[] lines = File.ReadAllLines(Path.Combine(ClientSource(), "Pages", "Map.razor"));
        var live = new bool[lines.Length];
        bool insideComment = false;
        for (int i = 0; i < lines.Length; i++)
        {
            bool opens = lines[i].Contains("@*", StringComparison.Ordinal);
            bool closes = lines[i].Contains("*@", StringComparison.Ordinal);
            live[i] = !(insideComment || opens);
            insideComment = (insideComment || opens) && !closes;
        }

        var gates = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!live[i] || !lines[i].Contains($"class=\"{TheScrim}", StringComparison.Ordinal))
            {
                continue;
            }

            string? gate = NearestGateAbove(lines, live, i);
            Assert.True(gate is not null,
                $"Map.razor:{i + 1} draws a .{TheScrim} with no @if above it that this guard can read. The "
                + "census cannot be checked against a scrim nobody can name.");
            gates.Add(Squashed(gate!));
        }

        return gates;
    }

    /// <summary>
    /// The <c>@if</c> a scrim is drawn under, read backwards out of the file BY INDENTATION.
    ///
    /// <para>The naive "nearest <c>@if</c> above" is wrong here and it is wrong in the one place that
    /// matters. The seated conversation card's scrim is the ELSE of <c>@if (SeatedIsDocked)</c>, and a
    /// hundred and fifty lines of the strip branch sit between them — the last <c>@if</c> above it is the
    /// pocket's own <c>@if (_satchel.Count == 0)</c>, a branch that closed long before. (Found by running
    /// it: the first cut of this guard reported the seated card as an unregistered scrim and named that
    /// gate.) This file is consistently indented, so an enclosing branch is one that is indented LESS —
    /// which is the same reading a human does with their eye.</para>
    ///
    /// <para>An <c>else</c> met on the way up is stepped THROUGH rather than stopped at: the scrim belongs
    /// to the <c>@if</c> that else hangs off, and that is the condition its census row records.</para>
    /// </summary>
    private static string? NearestGateAbove(string[] lines, bool[] live, int from)
    {
        int enclosing = Indent(lines[from]);
        for (int i = from - 1; i >= 0; i--)
        {
            if (!live[i] || lines[i].Trim().Length == 0 || Indent(lines[i]) >= enclosing)
            {
                continue;
            }

            Match m = Regex.Match(lines[i], @"^\s*@if\s*\((?<cond>.+)\)\s*$");
            if (m.Success)
            {
                return m.Groups["cond"].Value;
            }

            if (Regex.IsMatch(lines[i], @"^\s*else\s*$"))
            {
                enclosing = Indent(lines[i]) + 1;
            }
        }

        return null;
    }

    private static int Indent(string line) => line.Length - line.TrimStart().Length;

    /// <summary>One spelling of a condition, so a re-wrapped line and a census row cannot differ over
    /// whitespace alone.</summary>
    private static string Squashed(string condition) =>
        Regex.Replace(condition.Trim(), @"\s+", " ");

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

        throw new DirectoryNotFoundException($"could not find the client source above {AppContext.BaseDirectory}");
    }
}

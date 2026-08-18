using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #870 lane 6b · THE SEAT KEEPS ITS OWN STATE — and now it IS one.
///
/// <para>6a made the rest of the client stop reading the seat's five fields raw: eleven call sites across
/// five files started asking the family a named question instead, and this guard's first shape held the
/// line at <i>"nobody outside these seven partials names <c>_table</c>"</i>.</para>
///
/// <para>6b is the lane that shape was protecting. The five fields — the open table, the stool under the
/// captain, the stand-up confirm, the sit beat still owed, and which floor the cubicle cache was read off —
/// are now five properties on <c>Map.Seating</c>, declared in one file, and the page carries ONE field where
/// it carried five. So the guard ratchets with it: the raw names are not "outside the family" any more,
/// they are <b>gone</b>, and the state is declared in exactly one place.</para>
///
/// <h3>The facts, and why each is here</h3>
///
/// <list type="number">
/// <item><b>The five raw names appear nowhere in the shipped client</b> — not in a <c>.cs</c> partial, not in
/// the <c>.razor</c> markup, not in a comment. Strictly stronger than 6a's sweep, which exempted seven
/// files.</item>
/// <item><b>The state is declared in exactly one file.</b> Each of the five property declarations appears
/// once across the whole client, all five in <c>Pages/Seating/Seating.cs</c>, and so does the one
/// <c>_seating</c> field. A second declaration anywhere is a second answer to "where am I sitting", which is
/// this repo's first named bug class.</item>
/// <item><b>Nothing re-seats the page.</b> <c>_seating</c> is written nowhere else: standing up EMPTIES the
/// seat, it does not swap in a different one.</item>
/// <item><b>The anti-vacuous half, and it is asked of the RUNNING TYPE rather than of the text.</b> A sweep
/// for absence passes gloriously on a tree where the state was simply deleted — the world could no longer
/// tell pass from fail, this repo's fifth named bug class. So the last fact reflects over a real
/// <see cref="Pages.Map"/>: it really has a <c>_seating</c>, that object really carries the five with the
/// types they always had, and all fifteen moved reads answer on BOTH the seat object and the page. That last
/// clause is the forwarding proof — it is what says no caller outside this family had to change.</item>
/// </list>
///
/// <para>Proven RED before it was trusted, by leaving one raw field behind on <c>Map.Cubicle.cs</c>: facts 1
/// and 2 both named it, by file, line and text. The verbatim output is in #870 lane 6b's PR body.</para>
///
/// <para>#870's 6c moves the VERBS onto the same object behind an explicit host surface, and deletes the
/// forwarder block the fourth fact currently insists on. When that lands, the Map half of the fifteen is the
/// clause to relax — deliberately, in that lane, and never by deleting a row to make a sweep quiet.</para>
/// </summary>
public sealed class TheSeatKeepsItsOwnStateTests
{
    /// <summary>The one file the seat's state is allowed to be declared in, relative to
    /// <c>src/SpaceSails.Client</c>.</summary>
    private const string TheOneFile = "Pages/Seating/Seating.cs";

    /// <summary>The one field the page keeps, exactly as it is declared. #870 lane 6c re-SPELLED this needle
    /// and changed nothing it claims: the seat is handed the page in the constructor now
    /// (<c>new Seating(this)</c>), and an instance field initialiser may not name <c>this</c>, so the
    /// initialiser moved off the declaration. Still one field, still readonly, still one file.</summary>
    private const string TheOneField = "private readonly Seating _seating;";

    /// <summary>The five, before and after. Raw field name as the family used to declare it, then the
    /// property that replaced it, its declaration, and the type it still has.</summary>
    private static readonly (string Raw, string Property, string Declaration, string Type)[] TheFive =
    [
        ("_table", "Table", "public TableTalk? Table { get; set; }", "TableTalk"),
        ("_stool", "Stool", "public StoolSeat? Stool { get; set; }", "StoolSeat"),
        ("_standUpAsk", "StandUpAsk", "public bool StandUpAsk { get; set; }", "Boolean"),
        ("_sitBeatOwedSeconds", "SitBeatOwedSeconds", "public double SitBeatOwedSeconds { get; set; }", "Double"),
        ("_cubicleFloorKey", "CubicleFloorKey", "public string? CubicleFloorKey { get; set; }", "String"),
    ];

    /// <summary>The fifteen reads that moved. Every one of them is a pure function of the five and nothing
    /// else — no excursion, no avatar, no clock — and every one was already being asked for BY NAME from
    /// outside the seat family, which is why <see cref="Pages.Map"/> still answers all fifteen.</summary>
    private static readonly string[] TheFifteenMovedReads =
    [
        "CaptainIsSeated",
        "SeatedTable",
        "CaptainIsRestingAtATable",
        "SeatedOnABenchInTheOpen",
        "SeatedIsDocked",
        "SeatedIsAConversation",
        "SeatedWithCompany",
        "SeatedCompanyLine",
        "SeatedOverheardLine",
        "CaptainIsOnAStool",
        "SeatedStoolPlate",
        "TableMovesOnTheTable",
        "StoolMovesOnTheTable",
        "TheStandUpConfirmIsUp",
        "TheSitBeatIsSettling",
    ];

    /// <summary>Word-anchored, so <c>_stoolCheat</c> and <c>_tableCloth</c> are not the field. In .NET an
    /// underscore is a word character, so <c>\b_stool\b</c> is exactly "this name and no longer one".</summary>
    private static Regex Needle(string name) =>
        new(@"\b" + Regex.Escape(name) + @"\b", RegexOptions.CultureInvariant);

    // ── (1) THE RAW NAMES ARE GONE ────────────────────────────────────────────────────────────────────

    [Fact]
    public void NoRawSeatFieldIsNamedAnywhereInTheClient()
    {
        var trespass = new List<string>();

        foreach (string path in ClientSources())
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach ((string raw, string property, _, _) in TheFive)
                {
                    if (Needle(raw).IsMatch(lines[i]))
                    {
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} still names {raw} (now _seating.{property}) — " +
                            lines[i].Trim());
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6b · THE SEAT'S STATE IS ONE OBJECT. These are the five loose fields the seat used " +
            "to keep on the page; they no longer exist, because the state is five properties on Map.Seating " +
            $"in {TheOneFile}. Still named raw here:\n" +
            string.Join("\n", trespass) +
            "\n\nInside the seat family, say _seating.Table / _seating.Stool / _seating.StandUpAsk / " +
            "_seating.SitBeatOwedSeconds / _seating.CubicleFloorKey. Outside it, ask the question you " +
            "actually mean — CaptainIsSeated, CaptainIsOnAStool, SeatedTable, SeatedStoolPlate, SeatedIn, " +
            "SeatedIsDocked, SeatedAlone, TheStandUpConfirmIsUp — or tell it to do the thing: OweTheSitBeat, " +
            "LeaveTheStoolBehind. If none of them says what you need, ADD one small named member to Seating " +
            "and say in its docblock which site asked for it. Do not re-declare a field out here.");
    }

    // ── (2) AND THE STATE IS DECLARED IN EXACTLY ONE PLACE ────────────────────────────────────────────

    [Fact]
    public void TheFiveAreDeclaredInExactlyOneFileAndItIsSeating()
    {
        var wrong = new List<string>();
        var sources = ClientSources().ToList();

        foreach (string declaration in TheFive.Select(f => f.Declaration).Append(TheOneField))
        {
            List<string> found = sources
                .Where(p => File.ReadAllText(p).Contains(declaration, StringComparison.Ordinal))
                .Select(Relative)
                .ToList();

            if (found.Count != 1 || found[0] != TheOneFile)
            {
                wrong.Add(
                    $"  `{declaration}` is declared in {found.Count} file(s): " +
                    (found.Count == 0 ? "NONE" : string.Join(", ", found)));
            }
        }

        Assert.True(
            wrong.Count == 0,
            $"#870 lane 6b · THE SEAT IS DECLARED ONCE, AND IT IS DECLARED IN {TheOneFile}:\n" +
            string.Join("\n", wrong) +
            "\n\nZero files means the declaration was reworded and this guard is now reading a dead string — " +
            "fix the row, never delete it. Two files means there are two seats, and a captain sitting on one " +
            "of them is standing up according to the other.");
    }

    [Fact]
    public void NothingEverRESeatsThePage()
    {
        var written = new List<string>();
        int inTheConstructor = 0;

        foreach (string path in ClientSources())
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!Regex.IsMatch(lines[i], @"\b_seating\s*=(?!=)"))
                {
                    continue;
                }

                if (Relative(path) == TheOneConstructor)
                {
                    inTheConstructor++;   // the one assignment allowed to exist
                    continue;
                }

                written.Add($"  {Relative(path)}:{i + 1} — {lines[i].Trim()}");
            }
        }

        Assert.True(
            written.Count == 0 && inTheConstructor == 1,
            "#870 lane 6b · NOTHING RE-SEATS THE PAGE. `_seating` is readonly and assigned ONCE, in the " +
            $"page's own constructor ({TheOneConstructor}), which writes it {inTheConstructor} time(s). " +
            "Written again here:\n" + string.Join("\n", written) +
            "\n\nStanding up EMPTIES the seat (`_seating.Table = null`); it does not swap in a different " +
            "one. A second Seating is a second answer to \"where am I sitting\".");
    }

    /// <summary>#870 lane 6′c · WHERE THE ONE ASSIGNMENT LIVES. 6c put it beside the field in
    /// <see cref="TheOneFile"/>; the moment the ROUND needed the same treatment, the page's single
    /// parameterless constructor had to name a field of the PATROL family from inside a file of the SEAT
    /// family — which is exactly what this file's own "one door" sweep is there to catch. So the constructor
    /// moved to a file that belongs to neither, and this is a re-PATH: the claim (assigned once, and only
    /// there) is word for word what it was, and it now counts the one write rather than merely excusing a
    /// whole file.</summary>
    private const string TheOneConstructor = "Pages/Map.Collaborators.cs";

    // ── (3) AND IT IS NOT VACUOUS — ASKED OF THE RUNNING TYPE ─────────────────────────────────────────

    [Fact]
    public void TheSeatObjectReallyCarriesTheFiveAndAnswersAllFifteen()
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags Any = Hidden | BindingFlags.Public;

        FieldInfo? seating = typeof(Pages.Map).GetField("_seating", Hidden);
        Assert.True(
            seating is not null,
            "#870 lane 6b · ANTI-VACUOUS HALF. Map has no `_seating` field at all, which means the sweeps " +
            "above are passing because there is nothing left to find rather than because the seat kept its " +
            "state to itself. That is this repo's fifth named bug class. If the field was renamed, rename " +
            "it here in the same commit.");

        Type seat = seating!.FieldType;
        var missing = new List<string>();

        foreach ((string raw, string property, _, string type) in TheFive)
        {
            PropertyInfo? on = seat.GetProperty(property, Any);
            if (on is null)
            {
                missing.Add($"  {property} (was {raw}) is not on {seat.Name} at all");
            }
            else if (!on.PropertyType.Name.Contains(type, StringComparison.Ordinal))
            {
                missing.Add(
                    $"  {property} (was {raw}) is a {on.PropertyType.Name} and used to be a {type} — " +
                    "this lane was supposed to keep every type exactly as it was");
            }
        }

        // …and the fifteen answer on BOTH. On the seat object, because that is where they moved; and on the
        // page, because 6b changed no caller — every one of these names is still asked for by the markup,
        // the cancel chain, the surface tick or the deck's own state hand-off, spelled as it always was.
        foreach (string read in TheFifteenMovedReads)
        {
            if (seat.GetProperty(read, Any) is null && seat.GetMethod(read, Any) is null)
            {
                missing.Add($"  {read} does not answer on {seat.Name} — it was supposed to move there");
            }

            if (typeof(Pages.Map).GetProperty(read, Any) is null
                && typeof(Pages.Map).GetMethod(read, Any) is null)
            {
                missing.Add(
                    $"  {read} no longer answers on Map — 6b promised every existing caller keeps its " +
                    "spelling, so this one's forwarder is missing (see Map.Seated.cs's 6b block)");
            }
        }

        Assert.True(
            missing.Count == 0,
            "#870 lane 6b · ANTI-VACUOUS HALF, asked of the running type rather than of the text:\n" +
            string.Join("\n", missing) +
            "\n\nWhen 6c moves the VERBS and deletes the forwarder block, the Map half of the fifteen is " +
            "what relaxes — deliberately, in that lane, with the callers updated in the same commit.");
    }

    // ── (4) #870 lane 6c · AND THE SEAT REACHES THE PAGE THROUGH ONE DOOR ─────────────────────────────
    //
    // 6c moved the VERBS onto the same object — sitting down on six kinds of furniture, getting off them,
    // the wait beat, the moves at a top and at a counter — and the whole claim of the lane is that what they
    // still need from the page is ISeatHost and NOTHING else. These three facts are that claim, asked of the
    // source rather than of a reviewer.

    /// <summary>The one file that writes the coupling down.</summary>
    private const string TheHostFile = "Pages/Seating/ISeatHost.cs";

    /// <summary>The seat's own source: everything under <c>Pages/Seating/</c> whose name begins
    /// <c>Seating</c>. The interface itself and the page's implementation of it are deliberately NOT in here
    /// — they are the door, and a door is allowed to name both rooms.</summary>
    private static IEnumerable<string> SeatSources() =>
        ClientSources().Where(p =>
            Relative(p).StartsWith("Pages/Seating/Seating", StringComparison.Ordinal)
            && p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

    /// <summary>Two of the host's members share a spelling with a TYPE this family names constantly —
    /// <c>DeckPlan.ConsoleKind</c>, <c>Core.Satchel.Item</c> — so a bare occurrence of either is ordinarily a
    /// type reference and not a reach at the page. They are left out of the <i>say <c>_host.</c> it</i> half
    /// and out of that half only: the FIELD half still holds the line, because <c>_deckPlan</c> and
    /// <c>_satchel</c> are page fields and may not be named at all.</summary>
    private static readonly HashSet<string> CollidesWithATypeName =
        new(StringComparer.Ordinal) { "DeckPlan", "Satchel" };

    /// <summary>
    /// HOW MANY THINGS A CHAIR NEEDS FROM THE PAGE — <b>and it may only ever go down</b>.
    ///
    /// <para>A ratchet, exactly like #870's own size gate. The seat family used to be seven partials of
    /// <see cref="Pages.Map"/>, which meant it could reach anything the page had: every field, every private
    /// verb, every dev cheat, with nothing written down and nothing to argue with. The number below is what
    /// that came to when somebody finally counted.</para>
    ///
    /// <para>Taking a member off is a good day — lower the number in the same commit and say in the PR body
    /// which one went. RAISING it is a lane of its own, because it is the chair asking the page for something
    /// new, and that is a design decision rather than a build error.</para>
    ///
    /// <para><b>#731 · 28 → 30, and the lane that argued for it.</b> Owner, 2026-08-06: <i>"Now it is possible
    /// to have NPC ask to sit down at our table and offer a quest! This is the classic TTRPG event."</i> and
    /// <i>"If they go behind a door that is locked to us, we use that as 'I guess that concludes the
    /// conversation' point."</i> The visitor at a table stops being an assignment and becomes a WALK, in both
    /// directions — so the chair asks the page for exactly two new things, and they are the pair:
    /// <c>WalkSomebodyToYourTable</c> and <c>WalkTheVisitorOut</c>.</para>
    ///
    /// <para>They are ANSWERS and not machinery, which is what this docblock warns about: each is a
    /// <c>bool</c> meaning <i>she could be walked</i>, and the chair does not learn what a door, a lattice or
    /// a collision field is. The seat could not own them — a route is planned over the deck the PAGE holds
    /// and stepped in the page's own frame — and the alternative was handing the chair the deck plan, which
    /// is the trade this ratchet exists to refuse.</para>
    /// </summary>
    [Fact]
    public void TheSeatNeedsExactlyThisManyThingsFromThePage()
    {
        const int TheRatchet = 30;

        List<string> members = HostMembers();

        Assert.True(
            members.Count == TheRatchet,
            $"#870 lane 6c · THE SEAT NEEDS {members.Count} THINGS FROM THE PAGE, and the ratchet says " +
            $"{TheRatchet}.\n\n" +
            (members.Count < TheRatchet
                ? "FEWER is a good day — the chair stopped needing something. Lower the number here, in the " +
                  "same commit, and say in the PR body which member went and why."
                : "MORE means the chair asked the page for something new. That does not go in with a passing " +
                  "build; it goes in with a PR body that argues for it. If you are reading this in the middle " +
                  "of a refactor, the answer is almost always that the verb you just moved should have asked " +
                  "for an ANSWER rather than for the machinery under it.") +
            "\n\nWhat is on it today:\n  " + string.Join("\n  ", members));
    }

    /// <summary>
    /// AND NOTHING IN THE SEAT REACHES THE PAGE ANY OTHER WAY.
    ///
    /// <para>Two sweeps over the seat's own source, and between them they are the whole of the claim. No file
    /// of the seat may NAME a field of the page — not <c>_surface</c>, not <c>_avatarX</c>, not a dev cheat —
    /// and every one of the page's verbs it does use must be spelled <c>_host.</c> something, which is the
    /// only door there is.</para>
    ///
    /// <para><b>It reads CODE, not prose.</b> Doc comments and whole-line comments are skipped on purpose:
    /// the moved docblocks travelled byte-identical, which is this lane's own discipline, and several of them
    /// name a page member in a sentence ABOUT the coupling. The coupling is the interface; what this guard is
    /// about is what the code reaches for.</para>
    ///
    /// <para><b>Proven RED</b> by calling one page member directly out of a moved verb — verbatim in #870
    /// lane 6c's PR body.</para>
    /// </summary>
    [Fact]
    public void TheSeatReachesThePageThroughTheHostAndNoOtherWay()
    {
        var trespass = new List<string>();
        List<string> fields = PageFields();
        List<string> host = HostMembers()
            .Select(m => m.Split(' ')[0])
            .Where(n => !CollidesWithATypeName.Contains(n))
            .ToList();

        foreach (string path in SeatSources())
        {
            string[] lines = File.ReadAllLines(path);

            // Where the seat itself begins. Above it is the file's own `public partial class Map { … }`
            // scaffolding and, in the state file, the page's one seat field and its constructor — that is
            // the PAGE talking about the seat, which is allowed and is the only place it happens.
            int body = Array.FindIndex(lines, l => l.Contains("class Seating", StringComparison.Ordinal));

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    continue;   // prose about the coupling is not the coupling
                }

                // AND THE PAGE'S OWN TYPE IS NOT NAMED IN HERE AT ALL. This is the clause that shuts the
                // door rather than merely papering it: every way round the two sweeps below — a second field
                // typed as the page, a cast of the host back to it, a parameter, a local — has to write the
                // word `Map` somewhere, and after that the seat can reach anything again through a receiver
                // no sweep has heard of. FOUND BY TRYING IT: a planted `_page.ShowPulseMessage(…)` walked
                // straight past the host sweep, because that name IS qualified — just not by a door.
                if (body >= 0 && i > body && Regex.IsMatch(line, @"(?<![\w.])Map\b"))
                {
                    trespass.Add(
                        $"  {Relative(path)}:{i + 1} names the PAGE'S OWN TYPE inside the seat — {line.Trim()}");
                }

                foreach (string field in fields)
                {
                    if (Needle(field).IsMatch(line))
                    {
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} names the page's own {field} — {line.Trim()}");
                    }
                }

                foreach (string name in host)
                {
                    foreach (Match m in Regex.Matches(line, @"(?<!\w)" + Regex.Escape(name) + @"\b"))
                    {
                        string before = line[..m.Index];
                        if (before.EndsWith("_host.", StringComparison.Ordinal)
                            || before.EndsWith(".", StringComparison.Ordinal))
                        {
                            continue;   // through the one door, or a member of something else entirely
                        }
                        trespass.Add(
                            $"  {Relative(path)}:{i + 1} says {name} bare — say _host.{name} — {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(
            trespass.Count == 0,
            "#870 lane 6c · THE SEAT REACHES THE PAGE THROUGH ONE DOOR, and these lines go round it:\n" +
            string.Join("\n", trespass) +
            $"\n\nThe door is `_host`, and what is behind it is written down in {TheHostFile}. If the thing " +
            "you need is not on it, do not reach past it: ask the page for the ANSWER rather than for the " +
            "machinery — that is why APourInFrontOfYou, RestOneSeatedBeat, TheTailReading and CubicleIsShut " +
            "are one member each instead of the eight fields and three sweeps they are made of. If it really " +
            "does belong on the interface, add it AND raise the ratchet in " +
            "TheSeatNeedsExactlyThisManyThingsFromThePage, in a PR body that argues for it.");
    }

    /// <summary>The anti-vacuous half of the sweep above, and it is the same shape as this file's other one:
    /// a rule about ABSENCE passes gloriously on a tree where the thing was simply deleted. So the door has
    /// to be really there — the field, typed as the interface — the verbs have to really be in the files the
    /// sweep reads, and the door has to be really USED.</summary>
    [Fact]
    public void TheDoorIsReallyThereAndReallyUsed()
    {
        Assert.True(
            File.Exists(Path.Combine(ClientRoot, TheHostFile.Replace('/', Path.DirectorySeparatorChar))),
            $"#870 lane 6c · this guard names one file by path and it does not exist: {TheHostFile}. A path " +
            "that matches nothing exempts nothing and proves nothing. Re-PATH it; never delete the row.");

        List<string> seat = SeatSources().Select(Relative).ToList();
        Assert.True(
            seat.Count >= 6,
            $"#870 lane 6c · the seat is supposed to be its state plus five partials of verbs, and the sweep " +
            $"can only see {seat.Count} file(s). It is reading almost nothing, which means it is proving " +
            "almost nothing.\n  " + string.Join("\n  ", seat));

        Assert.Contains(
            "private readonly ISeatHost _host;",
            File.ReadAllText(Path.Combine(ClientRoot, TheOneFile.Replace('/', Path.DirectorySeparatorChar))),
            StringComparison.Ordinal);

        int through = SeatSources().Sum(p => Regex.Matches(File.ReadAllText(p), @"\b_host\.").Count);
        Assert.True(
            through >= 60,
            $"#870 lane 6c · the seat goes through its host {through} times, and it landed at well over that. " +
            "Either a whole verb group has left the object, or somebody found another way to the page — and " +
            "the sweep above cannot tell those two apart, which is why this row exists to notice.");
    }

    /// <summary>Every member declared on the host interface, as <c>Name (kind)</c>. Read off the SOURCE
    /// rather than off the running type, so the number in the ratchet is a count of what somebody wrote down
    /// and had to look at.</summary>
    private static List<string> HostMembers()
    {
        var found = new List<string>();
        string path = Path.Combine(ClientRoot, TheHostFile.Replace('/', Path.DirectorySeparatorChar));
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            Match verb = Regex.Match(line, @"^[\w?<>,.\[\]]+(?:\s+[\w?<>,.\[\]]+)*\s+(\w+)\(.*\);$");
            if (verb.Success)
            {
                found.Add(verb.Groups[1].Value + " (verb)");
                continue;
            }

            Match read = Regex.Match(line, @"^[\w?<>,.\[\]]+(?:\s+[\w?<>,.\[\]]+)*\s+(\w+)\s*\{[^}]*\}$");
            if (read.Success)
            {
                found.Add(read.Groups[1].Value + " (read)");
            }
        }
        return found;
    }

    /// <summary>Every field the PAGE keeps, by name, read off every client partial that is not the seat's
    /// own. This is the set the seat may not name: a chair that can reach a page field is a chair with no
    /// surface at all, which is the state 6a, 6b and 6c were written to leave behind.</summary>
    private static List<string> PageFields()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in ClientSources())
        {
            string rel = Relative(path);
            if (rel.StartsWith("Pages/Seating/Seating", StringComparison.Ordinal)
                || !rel.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                // A FIELD and never a member: declared at the class's own indent, ends its statement on its
                // own line, and is not an expression-bodied anything. One line can declare several.
                if (!Regex.IsMatch(line, @"^ {4}(?:private|internal|protected|public)\b")
                    || !line.Contains(';', StringComparison.Ordinal)
                    || line.Contains("=>", StringComparison.Ordinal))
                {
                    continue;
                }
                foreach (Match m in Regex.Matches(line, @"\b(_\w+)\b"))
                {
                    names.Add(m.Groups[1].Value);
                }
            }
        }
        return names.OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    // ── WHERE THE SOURCE IS ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheOneFileIsReallyThere()
    {
        string path = Path.Combine(ClientRoot, TheOneFile.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(
            File.Exists(path),
            $"#870 lane 6b · this guard names one file by path and it does not exist: {TheOneFile}. A path " +
            "that matches nothing exempts nothing and proves nothing. If the seat's state moved again, " +
            "re-PATH this constant — never delete the row to make the sweep quiet.");
    }

    /// <summary>Every shipped client source: the C# partials and the markup. Anything the compiler reads,
    /// this guard reads — <c>obj/</c> and <c>bin/</c> excluded, because a generated copy of a file is not a
    /// second author naming the field.</summary>
    private static IEnumerable<string> ClientSources() =>
        Directory.EnumerateFiles(ClientRoot, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Where(p => !Segments(p).Any(s =>
                s.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || s.Equals("bin", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p => p, StringComparer.Ordinal);

    private static string[] Segments(string path) =>
        Relative(path).Split('/');

    private static string Relative(string path) =>
        Path.GetRelativePath(ClientRoot, path).Replace('\\', '/');

    private static string ClientRoot { get; } =
        Path.Combine(RepoRoot(), "src", "SpaceSails.Client");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SpaceSails.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("no repo root above the test binary.");
    }
}

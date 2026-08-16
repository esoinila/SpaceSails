using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #870 · THE SIZE GATE — no source file over 1,500 lines, except the ones written down by name, and those
/// may only shrink.
///
/// <para>Owner, 2026-08-15: <i>"keep the source file line-counts reasonable"</i>. That sentence had been
/// true and unenforced for months, which is how <c>Map.Surface.cs</c> reached 9,410 lines and
/// <c>UndergroundComplex.cs</c> reached 8,747 before anybody said a word. Lanes 1 and 2 of #870 cut both
/// into fifteen files each; this lane is the thing that makes the cut stay cut.</para>
///
/// <para><b>The line is 1,500.</b> Not a number about compilers — the compiler does not care, and neither
/// does the runtime. It is a number about a reader: a file that fits in one sitting is a file whose SUBJECT
/// you can hold in your head, and every one of the monsters #870 went after got that way by being the
/// obvious place to put the next thing rather than by being about anything. Fifteen hundred lines is roughly
/// where "what is this file about?" stops having an answer. The nearest file under the line today is
/// <c>UndergroundComplex.Block.cs</c> at 1,447, so the line is not sitting on top of a real case: it has
/// 53 lines of daylight beneath it, and it is not a threshold that quietly selects everything.</para>
///
/// <para><b>A ratchet, not a wall.</b> Ten files are over the line today, and a gate that simply banned them
/// would have to be turned off to be committed — so instead each one is written down by PATH with the size
/// it is RIGHT NOW, and three laws hold the list:</para>
///
/// <list type="number">
/// <item><b>No new offenders.</b> A file not on the list may not cross 1,500. This is the law that matters;
/// the other two only stop the list from rotting.</item>
/// <item><b>Listed files may only SHRINK.</b> A file's real size must be at or below its written allowance.
/// Touch <c>Map.Plot.cs</c> and add forty lines and this goes red with both numbers in the message. The fix
/// is never to raise the number — it is to put the forty lines somewhere they belong. (If a genuine,
/// argued-for growth is the right answer, the number is edited in a commit that says WHY, and a reviewer
/// gets to see the allowance move. That is the whole point of writing it down.)</item>
/// <item><b>No stale entries.</b> A listed file that has dropped under the line — or that no longer exists
/// under that path at all — reddens with "remove me". Without this, the list is a graveyard of permissions
/// nobody revoked, and the ratchet quietly loses its teeth one merged split at a time.</item>
/// </list>
///
/// <para><b>Why the exceptions are written down by name.</b> A gate with an unwritten allowance is a gate
/// that never closes. Every "we'll fix it later" that lives in a <c>continue</c> or a <c>&gt; 4000</c> is
/// invisible to the person doing the fixing; a row in this dictionary is a name, a number, and a debt that
/// shows up in every diff that touches it.</para>
///
/// <para><b>How to lower an entry.</b> Split the file, run the suite, and edit ITS ROW — one line, no
/// argument: set the number to the new size, or, if the file has fallen under 1,500, delete the row entirely
/// (law 3 will tell you to, by name, if you forget). Do it in the SAME PR as the split. Anything a splitter
/// has to remember to do in a later PR is a thing that does not get done.</para>
///
/// <para><b>#870 lanes 4 and 5 will make rows here go stale on purpose.</b> The sibling crews splitting
/// <c>Map.Sim.cs</c>, <c>Map.Combat.cs</c>, <c>Map.Plot.cs</c>, <c>Map.Quests.cs</c>, <c>DeckView.cs</c>,
/// <c>Map.Patrol.cs</c> and <c>Map.Venting.cs</c> will each drop their file under the line, and law 3 will
/// go red on their branch naming their row. That is the gate working, not the gate breaking: the red IS the
/// instruction, and the fix is the one-line deletion described above, in their own PR.</para>
///
/// <para><b>Proven able to fail three ways</b> before it was trusted — grow a listed file past its row, push
/// an unlisted file over the line, and list a file that is already under it. The three REDs are quoted
/// verbatim in the PR body.</para>
/// </summary>
public sealed class NoSourceFileIsTooLongTests
{
    /// <summary>A subject fits a reader. See the class docblock for why this number and not another.</summary>
    private const int TheLine = 1500;

    /// <summary>
    /// #870 · THE WRITTEN EXCEPTIONS — path (repo-relative, forward slashes) → the largest this file is
    /// allowed to be. Every one of these was measured off the tree at 060b681, the base of the size gate.
    ///
    /// <para><b>SPLITTING ONE OF THESE? DELETE ITS ROW IN THE SAME PR.</b> Once your file is under 1,500
    /// its row is stale, and <see cref="NoListedFileHasQuietlyFallenUnderTheLine"/> will go red naming it.
    /// If it is still over the line after the split, lower the number to the new size instead. Never raise
    /// a number here without saying why in the commit.</para>
    /// </summary>
    private static readonly IReadOnlyDictionary<string, int> WrittenExceptions =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            // #870 lane 4 — queued for the by-banner split.
            // Map.Sim.cs's row is GONE (lane 4a, #891): the file is 254 lines now. What is left of that
            // 3,788 is one member — `BootTheWorldAsync`, 1,656 lines of it — which a PURE-MOVE lane may not
            // split, because splitting a method is not moving one. So the debt is re-written at its true
            // size under the name of the file that actually carries it, and it is now the only thing between
            // the whole Map.Sim family and the line.
            ["src/SpaceSails.Client/Pages/Map.Sim.World.cs"] = 1680,
            ["src/SpaceSails.Client/Pages/Map.Plot.cs"] = 2725,
            ["src/SpaceSails.Client/Pages/Map.Quests.cs"] = 2503,

            // #870 lane 5 — queued behind lane 4.
            // DeckView.cs's row is GONE (lane 5a): the file is 333 lines now, and the largest of the six
            // partials it became is 1,152 — under the line without an exception of its own.
            ["src/SpaceSails.Client/Pages/Map.Patrol.cs"] = 2074,
            ["src/SpaceSails.Client/Pages/Map.Venting.cs"] = 1842,

            // Not in any lane yet — inherited debt, listed so it cannot grow.
            ["src/SpaceSails.Core/PatrolBeat.cs"] = 1991,
            ["src/SpaceSails.Core/RingOffice.cs"] = 1762,
            ["src/SpaceSails.Client/Pages/Map.Deck.cs"] = 1687,
        };

    /// <summary>
    /// The repo root, found by walking up from the test assembly until <c>src/SpaceSails.Core</c> is under
    /// foot — the same way every other source-shape guard in this project finds it, and the reason none of
    /// them depend on the working directory.
    /// </summary>
    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Core")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// Every hand-written C# file we ship, with its length. <c>obj/</c> and <c>bin/</c> are build output —
    /// generated code is not a thing a reader reads, and holding a generated file to a reader's law would
    /// only teach people to turn the gate off.
    /// </summary>
    private static IEnumerable<(string Path, int Lines)> EverySourceFile()
    {
        string root = RepoRoot();
        string src = Path.Combine(root, "src");

        foreach (string full in Directory
            .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(root, full).Replace('\\', '/');
            if (rel.Contains("/obj/", StringComparison.Ordinal) ||
                rel.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            yield return (rel, File.ReadAllLines(full).Length);
        }
    }

    // ── LAW 1 · NO NEW OFFENDERS ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #870 · A file nobody wrote down may not cross the line. This is the law the gate exists for; laws 2
    /// and 3 only keep the exception list honest.
    ///
    /// <para><b>Proven RED</b> by appending 300 blank lines to <c>src/SpaceSails.Core/SurfaceLayout.cs</c>
    /// (1,207 → 1,507) — the message names the file, both numbers, and what to do about it. Quoted in the PR
    /// body.</para>
    /// </summary>
    [Fact]
    public void NoUnlistedSourceFileIsOverTheLine()
    {
        List<string> offenders = EverySourceFile()
            .Where(f => f.Lines > TheLine && !WrittenExceptions.ContainsKey(f.Path))
            .Select(f => $"  {f.Path} is {f.Lines} lines — {f.Lines - TheLine} over the line of {TheLine}.")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"#870 · {offenders.Count} source file(s) crossed the {TheLine}-line limit and are not written\n" +
            "down as exceptions:\n" +
            string.Join("\n", offenders) + "\n\n" +
            "SPLIT IT. A file this long has stopped being about one subject — take a section out by its own\n" +
            "'// ──' banner into a sibling partial, the way #870 lanes 1 and 2 did. If it genuinely cannot be\n" +
            "split right now, add a row to WrittenExceptions in NoSourceFileIsTooLongTests.cs with today's\n" +
            "size and say WHY in the commit — but the number may never go up again after that.");
    }

    // ── LAW 2 · A LISTED FILE MAY ONLY SHRINK ─────────────────────────────────────────────────────────

    /// <summary>
    /// #870 · The ratchet. Each written exception is a ceiling that only ever comes down: a listed file must
    /// be at or below the size it was on the day it was written down.
    ///
    /// <para><b>Proven RED</b> by appending 200 lines to <c>src/SpaceSails.Client/Pages/Map.Venting.cs</c> —
    /// the message names the file, its allowance, its real size and the overshoot. Quoted in the PR
    /// body.</para>
    /// </summary>
    [Fact]
    public void NoListedFileHasGrownPastItsWrittenAllowance()
    {
        Dictionary<string, int> onDisk = EverySourceFile().ToDictionary(f => f.Path, f => f.Lines, StringComparer.Ordinal);

        List<string> grown = WrittenExceptions
            .Where(e => onDisk.TryGetValue(e.Key, out int lines) && lines > e.Value)
            .OrderByDescending(e => onDisk[e.Key] - e.Value)
            .Select(e => $"  {e.Key} is {onDisk[e.Key]} lines; its written allowance is {e.Value} " +
                         $"— {onDisk[e.Key] - e.Value} too many.")
            .ToList();

        Assert.True(grown.Count == 0,
            $"#870 · {grown.Count} file(s) already over the {TheLine}-line limit GREW:\n" +
            string.Join("\n", grown) + "\n\n" +
            "The exception list is a ratchet — every number in it may only come down. Put what you just added\n" +
            "somewhere it belongs, or take an equal amount of this file out into a sibling partial first. Do\n" +
            "NOT raise the number in WrittenExceptions to make this pass: that is the one edit this guard\n" +
            "exists to make a reviewer look at.");
    }

    // ── LAW 3 · NO STALE ENTRIES ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #870 · An allowance that is no longer needed is an allowance that gets inherited. A listed file that
    /// has fallen under the line — or that no longer exists under that path — must have its row deleted, and
    /// this says so by name so the fix is one line.
    ///
    /// <para>This is the assertion #870's later lanes will meet head-on, BY DESIGN: the crew that splits
    /// <c>Map.Sim.cs</c> drops it under 1,500, this goes red naming that row, and they delete it in the same
    /// PR. See the class docblock.</para>
    ///
    /// <para><b>Proven RED</b> by adding a row for <c>src/SpaceSails.Core/SurfaceLayout.cs</c> (1,207 lines,
    /// well under the line) — the message names the file, its size, the line, and says "remove me". Quoted
    /// in the PR body.</para>
    /// </summary>
    [Fact]
    public void NoListedFileHasQuietlyFallenUnderTheLine()
    {
        Dictionary<string, int> onDisk = EverySourceFile().ToDictionary(f => f.Path, f => f.Lines, StringComparer.Ordinal);

        List<string> stale = WrittenExceptions
            .OrderBy(e => e.Key, StringComparer.Ordinal)
            .Select(e => onDisk.TryGetValue(e.Key, out int lines)
                ? lines <= TheLine
                    ? $"  {e.Key} is {lines} lines, at or under the line of {TheLine} " +
                      $"(its row still allows {e.Value}) — REMOVE ME."
                    : null
                : $"  {e.Key} is not there at all — no such file under src/ " +
                  $"(its row still allows {e.Value}) — REMOVE ME, or fix the path if it moved.")
            .OfType<string>()
            .ToList();

        Assert.True(stale.Count == 0,
            $"#870 · {stale.Count} written exception(s) are STALE:\n" +
            string.Join("\n", stale) + "\n\n" +
            "Good news — somebody did the work. Now delete the row(s) named above from WrittenExceptions in\n" +
            "tests/SpaceSails.Core.Tests/NoSourceFileIsTooLongTests.cs, in the SAME PR that shrank the file.\n" +
            "A list of permissions nobody revokes is how a gate stops being a gate.");
    }

    // ── THE GATE ITSELF ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #870 · The world this guard is stated against can tell pass from fail — the fifth bug class is a
    /// guard whose world is empty or whose threshold selects everything. So: the sweep must actually find
    /// the source tree (hundreds of files, not zero), the line must sit clear of the nearest real file
    /// beneath it, and every listed exception must be a file that is genuinely over the line today.
    /// </summary>
    [Fact]
    public void THE_GATE_CanTellPassFromFail()
    {
        List<(string Path, int Lines)> all = EverySourceFile().ToList();

        // A sweep that found nothing would pass all three laws forever.
        Assert.True(all.Count > 200, $"only {all.Count} source file(s) found under src/ — the sweep is blind.");
        Assert.Contains(all, f => f.Path == "src/SpaceSails.Core/UndergroundComplex.cs");
        Assert.Contains(all, f => f.Path == "src/SpaceSails.Client/Pages/Map.Surface.cs");

        // No build output crept in.
        Assert.DoesNotContain(all, f => f.Path.Contains("/obj/", StringComparison.Ordinal));
        Assert.DoesNotContain(all, f => f.Path.Contains("/bin/", StringComparison.Ordinal));

        // The line is not resting on a real case: there is daylight between it and the largest file under
        // it. A threshold one line above the nearest subject is a threshold that reddens on a typo.
        int nearestUnder = all.Where(f => f.Lines <= TheLine).Max(f => f.Lines);
        Assert.True(TheLine - nearestUnder >= 25,
            $"the line ({TheLine}) is only {TheLine - nearestUnder} lines above the largest file beneath it " +
            $"({nearestUnder}) — too tight to mean anything.");

        // Every row on the list is a real, present, genuinely-over-the-line file. Law 3 says the same thing
        // as a failure; this says it as a shape, so the list can never be padded with harmless names.
        foreach ((string path, int allowed) in WrittenExceptions)
        {
            Assert.Contains(all, f => f.Path == path);
            Assert.True(allowed > TheLine, $"{path}'s allowance of {allowed} is not above the line at all.");
        }
    }
}

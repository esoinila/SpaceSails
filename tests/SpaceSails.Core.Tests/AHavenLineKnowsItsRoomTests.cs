using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #1248 · <b>A HAVEN LINE KNOWS WHICH ROOM IT IS ABOUT.</b>
///
/// <para>Found by #1245's ambient-pool audit and tabled there rather than fixed. <c>HullShudder.HavenLines</c>
/// gated on the whole haven interior — the bar, the concourse, the immigration hall and #1199's observation
/// walk — but its three lines do not describe the same room:</para>
/// <list type="number">
///   <item><i>"…every head at <b>the bar</b> comes up as one… <b>the glasses</b> go back down."</i></item>
///   <item><i>"A shudder walks through <b>the concourse</b>… <b>the noise</b> floods back."</i></item>
///   <item><i>"…<b>the bar</b> decides it was nothing, and goes back to <b>its drinks</b>."</i></item>
/// </list>
///
/// <para>Two of them are made of the bar's own furniture and fired on the concourse; the third names the
/// concourse and is just as wrong said to a captain standing at a bar. Same class as #1215 (the stranger-bond
/// across a concourse), #1222 (its fix) and #1199 (the buzzer) — and the third time on this same floor, which
/// is why the law here is about the LINE rather than about one more gate.</para>
///
/// <para><b>The ruling: scope PER LINE, and no prose changes.</b> The words above are untouched and stay in
/// their authored order; what this lane adds is a partition over them
/// (<see cref="HullShudder.TheBarsOwnHavenLines"/> / <see cref="HullShudder.TheConcoursesOwnHavenLines"/>)
/// and the fact the client already holds (<c>TheCaptainIsInTheDockedBar</c> — #1222's predicate, never a
/// second one).</para>
///
/// <para><b>This file does not trust the partition; it checks it against the FURNITURE.</b> A guard that
/// simply re-stated the two index lists would be grading its own homework — the fifth bug class. The law is
/// read off the words: a line that names the bar's furniture must be in the bar's set, and a line that names
/// the concourse must not be.</para>
///
/// <para><b>RED on the shipped gate:</b> put <c>Setting.Haven =&gt; HavenLines</c> back (every line in both
/// rooms) and <see cref="TheConcourseIsNeverToldAboutTheGlasses"/> and
/// <see cref="AndTheBarIsNeverToldAboutTheConcourse"/> both go red; the partition laws stay green, because
/// the partition is still right — it is the SELECTOR that stopped reading it.</para>
/// </summary>
public sealed class AHavenLineKnowsItsRoomTests
{
    /// <summary>The bar's own things. Deliberately NOT "room" or "haven": those are words any interior can
    /// say, and a sweep that accepted them would pass a line about nowhere.</summary>
    private static readonly string[] TheBarsFurniture = ["the bar", "glasses", "drinks", "counter", "barkeep"];

    /// <summary>…and the concourse's. One word, because the line names it outright.</summary>
    private static readonly string[] TheConcoursesFurniture = ["concourse"];

    private static bool Names(string line, IEnumerable<string> furniture) =>
        furniture.Any(w => line.Contains(w, StringComparison.OrdinalIgnoreCase));

    // ── THE LAW: EACH LINE'S GATE MATCHES ITS FURNITURE ─────────────────────────────────────────────────

    [Fact]
    public void EveryLineTheBAROWNSIsMadeOfTheBarsFurniture()
    {
        IReadOnlyList<string> haven = HullShudder.EveryHavenLine();

        Assert.NotEmpty(HullShudder.TheBarsOwnHavenLines());
        foreach (int i in HullShudder.TheBarsOwnHavenLines())
        {
            Assert.True(Names(haven[i], TheBarsFurniture),
                $"this line is in the BAR's set and names none of the bar's own things, so the scope and "
                + $"the prose have parted company:\n  {haven[i]}");
            Assert.False(Names(haven[i], TheConcoursesFurniture),
                $"this line is in the BAR's set and names the concourse:\n  {haven[i]}");
        }
    }

    [Fact]
    public void AndEveryLineTheCONCOURSEOwnsIsNot()
    {
        IReadOnlyList<string> haven = HullShudder.EveryHavenLine();

        Assert.NotEmpty(HullShudder.TheConcoursesOwnHavenLines());
        foreach (int i in HullShudder.TheConcoursesOwnHavenLines())
        {
            Assert.False(Names(haven[i], TheBarsFurniture),
                $"this line is said OUTSIDE the bar and names the bar's own furniture, which is the whole "
                + $"bug #1248 was filed about:\n  {haven[i]}");
        }
    }

    [Fact]
    public void ThePartitionCOVERSThePoolAndOverlapsNowhere()
    {
        // A line added tomorrow and forgotten is a line nobody ever hears. Guarded rather than trusted.
        int[] bar = [.. HullShudder.TheBarsOwnHavenLines()];
        int[] concourse = [.. HullShudder.TheConcoursesOwnHavenLines()];

        Assert.Empty(bar.Intersect(concourse));
        Assert.Equal(
            Enumerable.Range(0, HullShudder.EveryHavenLine().Count).ToArray(),
            bar.Concat(concourse).OrderBy(i => i).ToArray());
    }

    // ── AND WHAT THE SELECTOR ACTUALLY HANDS BACK ───────────────────────────────────────────────────────

    [Fact]
    public void TheConcourseIsNeverToldAboutTheGlasses()
    {
        IReadOnlyList<string> outside = HullShudder.LinesFor(HullShudder.Setting.Haven, inTheBar: false);

        Assert.NotEmpty(outside);
        Assert.All(outside, line => Assert.False(Names(line, TheBarsFurniture),
            $"a captain out on the concourse was told about the bar's own furniture:\n  {line}"));
    }

    [Fact]
    public void AndTheBarIsNeverToldAboutTheConcourse()
    {
        // The other half, and it is not decoration: a line about the concourse said at a bar top is the same
        // bug pointing the other way, and a one-sided fix would have shipped it.
        IReadOnlyList<string> inside = HullShudder.LinesFor(HullShudder.Setting.Haven, inTheBar: true);

        Assert.NotEmpty(inside);
        Assert.All(inside, line => Assert.False(Names(line, TheConcoursesFurniture),
            $"a captain at a bar top was told a shudder walked through the concourse:\n  {line}"));
    }

    [Fact]
    public void BothRoomsStillHaveAVoiceAndTheyAreDifferentOnes()
    {
        // The anti-vacuous half. A selector that answered an EMPTY pool for one room would pass both "never
        // told about" laws above and would have deleted the beat from that room entirely — and one that
        // answered the same pool for both would pass neither, which is the point of having both.
        IReadOnlyList<string> inside = HullShudder.LinesFor(HullShudder.Setting.Haven, inTheBar: true);
        IReadOnlyList<string> outside = HullShudder.LinesFor(HullShudder.Setting.Haven, inTheBar: false);

        Assert.NotEmpty(inside);
        Assert.NotEmpty(outside);
        Assert.Empty(inside.Intersect(outside, StringComparer.Ordinal));
    }

    [Fact]
    public void AndNOTONEWORDOfTheProseMoved()
    {
        // The ruling was "scope per line, no prose change". The two rooms' pools put back together must be
        // the authored pool, in the authored order, to the character — so this lane cannot have quietly
        // rewritten a line to make the sweep above easier to satisfy.
        IReadOnlyList<string> haven = HullShudder.EveryHavenLine();
        string[] rejoined =
        [
            .. HullShudder.TheBarsOwnHavenLines()
                .Concat(HullShudder.TheConcoursesOwnHavenLines())
                .OrderBy(i => i)
                .Select(i => haven[i]),
        ];

        Assert.Equal(haven.ToArray(), rejoined);
        Assert.Equal(3, haven.Count);
        Assert.Contains(haven, l => l.Contains("the glasses go back down", StringComparison.Ordinal));
        Assert.Contains(haven, l => l.Contains("goes back to its drinks", StringComparison.Ordinal));
        Assert.Contains(haven, l => l.Contains("walks through the concourse", StringComparison.Ordinal));
    }

    [Fact]
    public void NoOTHERSettingReadsTheRoomAtAll()
    {
        // The ship, the regolith, a deep site and pressurised ground have no bar to be in or out of, and a
        // selector that had started varying them would be a second, undocumented scope.
        foreach (HullShudder.Setting setting in Enum.GetValues<HullShudder.Setting>())
        {
            if (setting == HullShudder.Setting.Haven)
            {
                continue;
            }

            Assert.Equal(
                HullShudder.LinesFor(setting, inTheBar: false),
                HullShudder.LinesFor(setting, inTheBar: true));
        }
    }
}

using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #663 · A PAINTED CANVAS WITH NOBODY TO SHOW IT.
///
/// <para>The reunification merge (#633) landed <c>StoryBeats</c> — the cadence law, the card/plate split,
/// the defer-while-in-danger rule and eleven painted canvases — with <b>four beats that nothing raises</b>.
/// The issue's own words: <i>"That guard is worth more than the four fixes above, because it is what stops
/// beat number twelve from being painted and orphaned. It has to be written so it goes RED on the current
/// tree — which it will, four times."</i></para>
///
/// <para>This is that guard, built as a <b>ratchet</b> rather than a wish. The four known orphans are named
/// in <see cref="KnownOrphans"/> with the issue that owns them, and the list can only ever shrink:</para>
///
/// <list type="bullet">
/// <item>a beat that is neither raised nor on the list <b>fails</b> — beat twelve cannot be orphaned quietly;</item>
/// <item>a beat that IS raised and is <i>still</i> on the list <b>fails</b> — so the list cannot rot into a
/// permanent excuse after somebody does the work. That is the "stale mirror" law: a comment naming a second
/// source of truth is a TODO with no owner, and this one is made to produce a CHANGE.</item>
/// </list>
///
/// <para><b>What counts as raised.</b> Only <c>RaiseStoryBeat(…)</c> — the seam every moment must knock on,
/// where Core's cadence and presentation rules are applied. Reading a beat's <c>ArtFile</c> straight out of
/// markup does NOT count, and that distinction is the whole point of the issue's fourth row: the BUSTED card
/// reached for <c>CollectorHail</c>'s painting directly, so the hail had a picture but no cadence and no log
/// line. A picture is not a beat.</para>
///
/// <para>#777 · <b>And that row is now paid, without weakening this rule by a comma.</b> The hail is still
/// not counted for showing a painting; it is counted because the two edges that open the demand panel now
/// knock on the same door every other beat knocks on. What changed is what the door DOES with it: a
/// <c>StoryBeats.Presentation.Hosted</c> beat gets its cadence spent, its seen-set filed and its words
/// logged, and then the seam raises nothing, because the caller's card is already the canvas. So this
/// scanner keeps asking the one question it has always asked — <i>who calls the seam?</i> — and the answer
/// for <c>CollectorHail</c> stopped being "nobody".</para>
///
/// <para>Two shapes of call are honoured, because both exist: a literal
/// <c>RaiseStoryBeat(StoryBeats.Beat.X, …)</c>, and an indirect one where a pure Core chooser picks the beat
/// (<c>RaiseStoryBeat(ArrivalTube.BeatFor(…), …)</c> — the arrival tube's three tiers). For the indirect
/// form every beat named in the chooser's own Core file counts, which is exactly the set that call can
/// produce.</para>
/// </summary>
public sealed class EveryStoryBeatHasACallerTests
{
    /// <summary>
    /// The beats #663 recorded as having a canvas, a cadence and no caller — minus the one this lane wired.
    /// Each entry is a debt, not a decision. Removing one is the fix; adding one needs an issue.
    /// </summary>
    private static readonly Dictionary<StoryBeats.Beat, string> KnownOrphans = new()
    {
        // #663 · CrewDeputation CAME OFF THIS LIST. Its entry read "the CrewTemp Petition edge cannot be
        // crossed yet" and named CrewLost as one of three dormant constants in Map.CrewTemp — an excuse that
        // had already expired, because the deflection gig (#394) loses crew on a falling rock, docks the fee
        // per body and says so out loud, while the sheet on the desk went on answering "Nobody has been
        // lost". The counter is live, the Petition edge is crossable, and the beat is raised where the
        // standing is read. See TheCrewSheetCountsTheDeadTests.
        //
        // CrewMeeting stays, and for the reason the list is FOR. The Ultimatum edge needs a broken promise
        // to the crew or months without shore leave, and those two hooks really are numbers nobody keeps —
        // pinned from both ends: the rules' half in Core
        // (CrewTempTests.ADeadCrewmanIsWhatBuysTheDeputationAndItIsStillNotEnoughForTheMeeting) and the
        // world's half against the shipping page
        // (TheCrewSheetCountsTheDeadTests.TheShippedInputsReachADeputationAndNoFurther, which fails the day
        // the reach passes a Petition). Wiring the meeting today would be a caller that satisfies this
        // scanner and can never fire, which is this house's fifth bug class wearing the fix's clothes.
        [StoryBeats.Beat.CrewMeeting] = "#663 — the CrewTemp Ultimatum edge cannot be crossed yet (see TheCrewSheetCountsTheDeadTests)",

        // #777 · CollectorHail CAME OFF THIS LIST. Its entry read "the BUSTED card IS the hail's card; the
        // seam has no hosted presentation" — a debt against the SEAM rather than against the wiring, and the
        // only one of the four that named a design change as its price. #777 paid it:
        // StoryBeats.Presentation.Hosted exists, the two demand edges raise the beat through the ordinary
        // door, and the seam keeps the books while the panel keeps the canvas. See
        // TheHailIsHostedByTheCardItArrivesOnTests.
    };

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

    /// <summary>Every beat some <c>RaiseStoryBeat(…)</c> call can actually produce.</summary>
    private static HashSet<StoryBeats.Beat> BeatsWithACaller()
    {
        string repo = RepoRoot();
        string clientDir = Path.Combine(repo, "src", "SpaceSails.Client");
        string coreDir = Path.Combine(repo, "src", "SpaceSails.Core");

        var raised = new HashSet<StoryBeats.Beat>();

        // Every argument handed to the one seam. The declaration in Map.StoryCards.cs is skipped by the
        // argument shape (its parameter list is "StoryBeats.Beat beat, string? subject = null").
        // The trailing group is load-bearing: it separates a CALL from the seam's own DECLARATION. Without
        // it, `private void RaiseStoryBeat(StoryBeats.Beat beat, …)` reads as an indirect raise through the
        // type StoryBeats itself, which resolves to StoryBeats.cs and marks every beat in the game as
        // raised — a guard that asserts nothing, on its own first run.
        var callSite = new Regex(@"RaiseStoryBeat\(\s*([A-Za-z0-9_.]+)\s*([(,)])", RegexOptions.Compiled);
        var beatToken = new Regex(@"\bBeat\.([A-Za-z0-9_]+)", RegexOptions.Compiled);

        foreach (string file in Directory.EnumerateFiles(clientDir, "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) is not (".cs" or ".razor") || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            foreach (Match m in callSite.Matches(text))
            {
                string arg = m.Groups[1].Value;
                bool isChooserCall = m.Groups[2].Value == "(";

                // (a) the literal form: RaiseStoryBeat(StoryBeats.Beat.X, …)
                if (arg.StartsWith("StoryBeats.Beat.", StringComparison.Ordinal)
                    && Enum.TryParse(arg["StoryBeats.Beat.".Length..], out StoryBeats.Beat direct))
                {
                    raised.Add(direct);
                    continue;
                }

                // (b) the indirect form: RaiseStoryBeat(SomeCoreType.Chooser(…), …). Every beat that Core
                //     type can name is reachable through that call — the arrival tube's three tiers are the
                //     shipped example, and hard-coding "ArrivalTube" here would be the stale mirror again.
                if (!isChooserCall)
                {
                    continue;   // not a literal beat and not a call — nothing this seam can produce
                }

                string type = arg.Split('.')[0];
                string coreFile = Path.Combine(coreDir, type + ".cs");
                if (!File.Exists(coreFile))
                {
                    continue;
                }

                foreach (Match b in beatToken.Matches(File.ReadAllText(coreFile)))
                {
                    if (Enum.TryParse(b.Groups[1].Value, out StoryBeats.Beat indirect))
                    {
                        raised.Add(indirect);
                    }
                }
            }
        }

        return raised;
    }

    /// <summary>A guard that finds no call sites at all would pass every claim below on an empty set — the
    /// house's fifth bug class exactly. Pin that the scan actually sees the shipped callers.</summary>
    [Fact]
    public void TheScanFindsTheCallersThatAreDefinitelyThere()
    {
        HashSet<StoryBeats.Beat> raised = BeatsWithACaller();

        Assert.Contains(StoryBeats.Beat.FirstShotFired, raised);
        Assert.Contains(StoryBeats.Beat.SailHoled, raised);
        Assert.Contains(StoryBeats.Beat.FireAboard, raised);
        Assert.Contains(StoryBeats.Beat.BerthGreatPort, raised);   // reached through ArrivalTube.BeatFor
    }

    /// <summary>The ratchet, forward: nothing is orphaned that is not on the list.</summary>
    [Fact]
    public void EveryBeatIsEitherRaisedOrAKnownOrphan()
    {
        HashSet<StoryBeats.Beat> raised = BeatsWithACaller();

        List<string> orphaned = [.. Enum.GetValues<StoryBeats.Beat>()
            .Where(b => !raised.Contains(b) && !KnownOrphans.ContainsKey(b))
            .Select(b => $"{b} → {StoryBeats.ArtFile(b)}")];

        Assert.True(orphaned.Count == 0,
            "beats with a canvas, a cadence and nobody to raise them (wire one, or file it in KnownOrphans " +
            $"with the issue that owns it): {string.Join(", ", orphaned)}");
    }

    /// <summary>The ratchet, backward: the excuse list cannot outlive the excuse.</summary>
    [Fact]
    public void TheOrphanListShrinksAndNeverRots()
    {
        HashSet<StoryBeats.Beat> raised = BeatsWithACaller();

        List<string> stale = [.. KnownOrphans.Keys.Where(raised.Contains).Select(b => b.ToString())];

        Assert.True(stale.Count == 0,
            "these beats now HAVE a caller and are still listed as orphans — take them out of KnownOrphans, " +
            $"because a list that keeps a fixed thing on it is how a TODO loses its owner: {string.Join(", ", stale)}");
    }

    /// <summary>
    /// And the one this lane exists for. `QAHandoff-StoryTelling.md` §5: <i>"an arc beat landing on the news
    /// wire is the exact storytelling device the KAAMOS (#411) and Nebula (#422) passes are missing."</i>
    /// It is raised now, on the edge where the ice-moon berth takes its first filing in a lifetime.
    /// </summary>
    [Fact]
    public void TheArcNewsBeatIsRaisedAndIsNotOnTheExcuseList()
    {
        Assert.DoesNotContain(StoryBeats.Beat.ArcNewsBreaks, KnownOrphans.Keys);
        Assert.Contains(StoryBeats.Beat.ArcNewsBreaks, BeatsWithACaller());
    }

    /// <summary>
    /// #777 · And the one #663 could not fix without inventing a presentation. The hail's entry named a
    /// design change as its price — <i>"the seam has no hosted presentation"</i> — so it is off the list for
    /// the only reason that counts here: somebody calls the seam about it.
    ///
    /// <para>The two assertions are deliberately both halves of the ratchet, on one beat, in one place: off
    /// the excuse list AND actually raised. Either alone would pass on a lie.</para>
    /// </summary>
    [Fact]
    public void TheCollectorHailIsRaisedAndIsNotOnTheExcuseList()
    {
        Assert.DoesNotContain(StoryBeats.Beat.CollectorHail, KnownOrphans.Keys);
        Assert.Contains(StoryBeats.Beat.CollectorHail, BeatsWithACaller());
    }

    /// <summary>
    /// #663 · And the third of the four. The deputation's excuse was that the world could not cross the
    /// <c>CrewTemp.Standing.Petition</c> edge, because <c>CrewLost</c> was a dormant constant — an excuse
    /// that had already expired when it was written, since the deflection gig (#394) loses crew on a falling
    /// rock and says so out loud. The counter is live now and the edge is crossable, so the beat is raised
    /// where the crew's standing is read; <see cref="TheCrewSheetCountsTheDeadTests"/> holds the proof that
    /// the world can actually get there, which is the half a scanner cannot see.
    ///
    /// <para>Both halves of the ratchet on one beat, deliberately: off the excuse list AND actually raised.
    /// Either alone would pass on a lie.</para>
    /// </summary>
    [Fact]
    public void TheCrewDeputationIsRaisedAndIsNotOnTheExcuseList()
    {
        Assert.DoesNotContain(StoryBeats.Beat.CrewDeputation, KnownOrphans.Keys);
        Assert.Contains(StoryBeats.Beat.CrewDeputation, BeatsWithACaller());
    }
}

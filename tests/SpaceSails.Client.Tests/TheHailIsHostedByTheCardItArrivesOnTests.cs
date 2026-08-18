using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #777 · A BEAT HOSTED BY THE CARD ITS CALLER ALREADY RAISES.
///
/// <para>#776's audit closed on the one row of #663 it could not wire: <i>"the BUSTED card IS the hail's
/// card: it already renders <c>StoryBeats.ArtFile(Beat.CollectorHail)</c> at the top of the Demand panel.
/// Raising the beat as it stands would put a second modal on top of the modal showing the very same
/// painting… and <c>CollectorHail</c> is the one beat that may not defer, because it IS the danger. The fix
/// is a presentation the seam does not have."</i></para>
///
/// <para>It has one now. <c>StoryBeats.Presentation.Hosted</c> splits a beat's two jobs apart: the
/// BOOKKEEPING — cadence spent, seen-set filed, words written into the log — belongs to the seam and happens
/// for every beat alike; the SURFACE belongs, this once, to the caller's own card. So the hail is raised
/// through the same door as everything else, and nothing is shown twice.</para>
///
/// <para><b>Why these are source-shape guards.</b> This project has no component renderer: the client tests
/// read the shipped source, the way <see cref="TheOutcomeIsOnThePopUpTests"/> and
/// <see cref="TheHallCardsAreRaisedOnceTests"/> do. That is not a weakness here, because every failure this
/// lane can produce is a shape — a demand edge that forgets to raise, a presentation arm that raises a
/// second modal anyway, a caption that lives somewhere other than the panel it belongs to. Each test below
/// cuts a SUBTREE (one method's body, one <c>case</c> block of Map.razor) and states a law about what is and
/// is not inside it; a line that renders elsewhere in a 5,700-line file is a line on a different pop-up,
/// which is the bug and not the fix.</para>
///
/// <para>Each one was proven RED — see the per-test remarks for the exact revert and the exact message.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHailIsHostedByTheCardItArrivesOnTests
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

    private static IEnumerable<(string File, string Text)> ClientSource()
    {
        string dir = Path.Combine(RepoRoot(), "src", "SpaceSails.Client");
        foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            char s = Path.DirectorySeparatorChar;
            if (Path.GetExtension(file) is not (".cs" or ".razor")
                || file.Contains($"{s}obj{s}", StringComparison.Ordinal)
                || file.Contains($"{s}bin{s}", StringComparison.Ordinal))
            {
                continue;
            }

            yield return (file, File.ReadAllText(file));
        }
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    /// <summary>One member's body, from the <c>private</c> that opens it to the next one at the same indent —
    /// the cut <see cref="TheHallCardsAreRaisedOnceTests"/> makes, so a body read here is a body read
    /// there.</summary>
    private static string EnclosingMember(string text, int at)
    {
        int from = text.LastIndexOf("\n    private ", at, StringComparison.Ordinal);
        Assert.True(from >= 0, "could not find the member that opens this encounter — this guard needs re-reading.");
        int to = text.IndexOf("\n    private ", at, StringComparison.Ordinal);
        return text[from..(to < 0 ? text.Length : to)];
    }

    /// <summary>Its signature line, for a failure message that names a method instead of an offset.</summary>
    private static string SignatureOf(string member) =>
        member.Trim().Split('\n')[0].Replace("\r", "", StringComparison.Ordinal).Trim();

    // ── Where the demand panel opens ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Every place in the client that opens a <c>BustedEncounter</c>, with the member that opens it and
    /// whether that encounter starts on the DEMAND stage.
    ///
    /// <para>Demand is <c>BustedEncounter.Stage</c>'s default, so "starts on the demand panel" reads off the
    /// initializer as the ABSENCE of a <c>Phase =</c>. Read rather than listed on purpose: a hard-coded pair
    /// of file names is the stale mirror this house keeps naming, and the sixth place somebody grapples the
    /// captain would not be on it.</para>
    /// </summary>
    private static List<(string File, string Member, bool OpensOnDemand)> EncounterOpenings()
    {
        var found = new List<(string, string, bool)>();

        foreach ((string file, string text) in ClientSource())
        {
            foreach (Match m in Regex.Matches(text, @"_busted = new BustedEncounter\s*\{"))
            {
                int close = text.IndexOf("\n        };", m.Index, StringComparison.Ordinal);
                Assert.True(close > m.Index,
                    $"{Path.GetFileName(file)}: could not find the end of a BustedEncounter initializer.");

                string initializer = text[m.Index..close];
                found.Add((file, EnclosingMember(text, m.Index), !initializer.Contains("Phase =", StringComparison.Ordinal)));
            }
        }

        return found;
    }

    /// <summary>
    /// THE GUARD #777 EXISTS FOR: opening the demand panel raises the hail, from every edge that opens it.
    ///
    /// <para>Two edges do today — a hunter running you down on her deck (<c>ApplyHunterCatch</c>) and a hand
    /// on your carry loop out on a moon (<c>TheWritIsServed</c>, #583) — and the audit's own finding is that
    /// a beat wired at one of its edges is a beat that silently stops being told at the other. So this reads
    /// the edges out of the source rather than naming them.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the raise from <c>ApplyHunterCatch</c> — which is exactly the
    /// shipped state before this PR, when the panel showed the hail's painting and nothing counted it:</para>
    /// <code>
    /// a BUSTED encounter that opens on the DEMAND panel and never raises StoryBeats.Beat.CollectorHail:
    /// Map.Combat.cs · private void ApplyHunterCatch(HunterState hunter) — the demand panel IS that beat's
    /// canvas (StoryBeats.Presentation.Hosted), so an edge that opens the panel without knocking on the seam
    /// shows the picture and counts nothing: no cadence, no seen-set, no log line, and #663's scanner
    /// rightly calls the beat an orphan.
    /// </code>
    /// </summary>
    [Fact]
    public void EveryEdgeThatOpensTheDemandPanelRaisesTheHail()
    {
        List<(string File, string Member, bool OpensOnDemand)> openings = EncounterOpenings();

        List<string> silent =
        [
            .. openings
                .Where(o => o.OpensOnDemand
                            && !o.Member.Contains("RaiseStoryBeat(StoryBeats.Beat.CollectorHail", StringComparison.Ordinal))
                .Select(o => $"{Path.GetFileName(o.File)} · {SignatureOf(o.Member)}")
        ];

        Assert.True(silent.Count == 0,
            "a BUSTED encounter that opens on the DEMAND panel and never raises StoryBeats.Beat.CollectorHail: "
            + string.Join("; ", silent)
            + " — the demand panel IS that beat's canvas (StoryBeats.Presentation.Hosted), so an edge that "
            + "opens the panel without knocking on the seam shows the picture and counts nothing: no cadence, "
            + "no seen-set, no log line, and #663's scanner rightly calls the beat an orphan.");
    }

    /// <summary>
    /// …and the sweep can tell the two kinds of opening apart, which is the whole of its power.
    ///
    /// <para>A classifier that answered "demand" to everything would make the test above fail loudly; one
    /// that answered "not demand" to everything would make it pass on a tree with no raise anywhere — the
    /// house's fifth bug class, a guard whose world cannot tell pass from fail. So: both kinds are found, and
    /// the raise is fenced to the demand ones. The freeze-frame paths (impact, the regolith, the captain who
    /// stayed aboard his own countdown) must NOT raise a collector's hail — nobody grappled anybody.</para>
    /// </summary>
    [Fact]
    public void TheSweepSeesBothKindsOfOpeningAndOnlyDemandRaisesTheHail()
    {
        List<(string File, string Member, bool OpensOnDemand)> openings = EncounterOpenings();

        Assert.True(openings.Count >= 5, $"only {openings.Count} BustedEncounter openings found — the scan is not reading the client.");
        Assert.True(openings.Count(o => o.OpensOnDemand) >= 2,
            "no demand-stage openings found, so the law above is vacuous — the collector catch (Map.Combat) "
            + "and the writ served on foot (Map.Surface, #583) both open on Stage.Demand.");
        Assert.True(openings.Exists(o => !o.OpensOnDemand),
            "no explicit-Phase openings found, so the classifier is answering 'demand' to everything.");

        List<string> wrong =
        [
            .. openings
                .Where(o => !o.OpensOnDemand
                            && o.Member.Contains("RaiseStoryBeat(StoryBeats.Beat.CollectorHail", StringComparison.Ordinal))
                .Select(o => $"{Path.GetFileName(o.File)} · {SignatureOf(o.Member)}")
        ];

        Assert.True(wrong.Count == 0,
            "these open the encounter on a phase that is NOT the demand panel and still raise the collector's "
            + $"hail: {string.Join("; ", wrong)} — the hail's canvas is the demand panel, and a freeze-frame "
            + "is not it. Nobody grappled anybody on an impact.");
    }

    /// <summary>
    /// AND THE PANEL AND THE LOG NAME THE SAME SHIP. The seam writes the beat's caption into the log with the
    /// subject it was raised with; the panel renders the same caption with <c>bust.HunterCallsign</c>. If the
    /// raise is handed a different expression than the encounter's callsign, the book and the screen describe
    /// two different collectors — the sim-says-one-thing-the-sentence-says-another failure this repo has
    /// named three times in one afternoon.
    /// </summary>
    [Fact]
    public void TheHailIsRaisedAboutTheSameCollectorTheEncounterNames()
    {
        foreach ((string file, string member, bool onDemand) in EncounterOpenings())
        {
            if (!onDemand)
            {
                continue;
            }

            Match callsign = Regex.Match(member, @"HunterCallsign = ([^,\r\n]+),");
            Assert.True(callsign.Success, $"{Path.GetFileName(file)} · {SignatureOf(member)} names no HunterCallsign.");

            Match raised = Regex.Match(member, @"RaiseStoryBeat\(StoryBeats\.Beat\.CollectorHail, ([^)\r\n]+)\)");
            Assert.True(raised.Success, $"{Path.GetFileName(file)} · {SignatureOf(member)} does not raise the hail.");

            Assert.True(callsign.Groups[1].Value.Trim() == raised.Groups[1].Value.Trim(),
                $"{Path.GetFileName(file)} · {SignatureOf(member)}: the encounter is opened about "
                + $"`{callsign.Groups[1].Value.Trim()}` and the beat is raised about "
                + $"`{raised.Groups[1].Value.Trim()}` — the caption on the panel and the caption in the log "
                + "would then name two different collectors.");
        }
    }

    // ── What the seam does with a hosted beat ─────────────────────────────────────────────────────────

    /// <summary>The seam's own <c>ShowStoryBeat</c>, cut from its signature to the next member.</summary>
    private static string ShowStoryBeat()
    {
        string src = Pages("Map.StoryCards.cs");
        int at = src.IndexOf("private void ShowStoryBeat(", StringComparison.Ordinal);
        Assert.True(at >= 0, "Map.StoryCards.cs no longer has ShowStoryBeat where this guard can read it.");
        int end = src.IndexOf("\n    /// <summary>", at, StringComparison.Ordinal);
        return src[at..(end > at ? end : src.Length)];
    }

    /// <summary>
    /// NO SECOND MODAL. The hosted arm of the presentation switch raises nothing at all: it must not touch
    /// <c>_storyCard</c> (that is the stacked card #663 refused to ship), must not touch <c>_storyPlate</c>
    /// (a plate over the panel is the same noise at the edge), and must not play a cue (the host arrives on
    /// "board"; a "reveal" chime layered over it is a stacked modal in the one channel a player cannot
    /// close).
    ///
    /// <para><b>Not vacuous, and proven twice.</b> Wiring the naive raise — putting
    /// <c>_storyCard = (beat, subject);</c> into the hosted arm, which is what raising the hail through the
    /// old two-answer seam did — goes RED:</para>
    /// <code>
    /// the HOSTED arm of ShowStoryBeat raises `_storyCard` — a hosted beat's canvas is a card its caller
    /// already raised, so anything this arm puts on the screen is the second modal, showing the same
    /// painting as the first.
    /// </code>
    /// <para>And reverting the seam to its old <c>if (plate) … else { _storyCard = … }</c> — the shape in
    /// which CARD was the answer to every question nobody had asked yet, and therefore the shape in which
    /// the hail's raise stacks a modal — goes RED on the arm being gone.</para>
    /// </summary>
    [Fact]
    public void TheHostedArmOfTheSeamRaisesNothingAtAll()
    {
        string body = ShowStoryBeat();

        int arm = body.IndexOf("case StoryBeats.Presentation.Hosted:", StringComparison.Ordinal);
        Assert.True(arm >= 0,
            "ShowStoryBeat has no `case StoryBeats.Presentation.Hosted:` — without one a hosted beat falls "
            + "through to the card branch and the seam raises the second modal #777 exists to prevent.");

        int ends = body.IndexOf("break;", arm, StringComparison.Ordinal);
        Assert.True(ends > arm, "the hosted arm of ShowStoryBeat has no break — this guard cannot cut it.");
        string hosted = body[arm..ends];

        foreach ((string forbidden, string why) in new[]
                 {
                     ("_storyCard", "a hosted beat's canvas is a card its caller already raised, so anything this "
                                    + "arm puts on the screen is the second modal, showing the same painting as the first"),
                     ("_storyPlate", "a plate riding the edge of the host is the same duplication, moved to the corner"),
                     // #664 · widened from `PlayCue` to `Cue`, because the seam's chime is routed through
                     // `PlayTheBeatsCue` now (Core decides which noise, or none) and a guard that only knew
                     // the old spelling would have waved the new one through this arm.
                     ("Cue", "the host arrives with its own cue; a second one is a stacked modal in the one "
                             + "channel the player cannot close"),
                 })
        {
            Assert.True(!hosted.Contains(forbidden, StringComparison.Ordinal),
                $"the HOSTED arm of ShowStoryBeat raises `{forbidden}` — {why}.");
        }

        // The other two arms DO raise something. Without this the test above would pass on a seam that had
        // stopped showing anything to anybody.
        // #664 · the card arm carries #736's outcome now, arrived with the eleven moments that came off the
        // deleted reveal card. The claim is unchanged: the OTHER two arms really do put something up.
        Assert.Contains("_storyCard = (beat, subject, outcome);", body, StringComparison.Ordinal);
        Assert.Contains("_storyPlate = (beat, subject,", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// …AND IT IS STILL COUNTED AS TOLD, EXACTLY ONCE. The two things a hosted beat comes to the seam FOR are
    /// the seen-set entry (which spends the cadence) and the log line (which keeps the words after the panel
    /// closes). Both must sit outside the presentation switch, so no arm can be written that skips them.
    ///
    /// <para><b>Proven RED</b> by moving <c>_beatsSpoken[SeenKey(beat, subject)] = SimTime;</c> down into the
    /// card and plate arms — the state the tree was in for every beat that had no arm of its own:</para>
    /// <code>
    /// ShowStoryBeat files the seen-set INSIDE the presentation switch — a hosted beat would then be shown
    /// (by its host) and never recorded, so its cadence never spends and #663's whole once-only law is off
    /// for exactly the beats that do not raise their own surface.
    /// </code>
    /// </summary>
    [Fact]
    public void AHostedBeatIsFiledAndLoggedLikeEveryOtherBeat()
    {
        string body = ShowStoryBeat();

        int filed = body.IndexOf("_beatsSpoken[SeenKey(beat, subject)] = SimTime;", StringComparison.Ordinal);
        int opens = body.IndexOf("switch (StoryBeats.PresentationOf(beat))", StringComparison.Ordinal);
        int logged = body.IndexOf("LogAutopilotEvent(", StringComparison.Ordinal);
        int lastArm = body.LastIndexOf("case StoryBeats.Presentation.", StringComparison.Ordinal);

        Assert.True(filed >= 0 && opens > 0 && logged > 0 && lastArm > 0,
            "ShowStoryBeat no longer has the shape this guard reads: a seen-set write, a presentation switch "
            + "and a log line.");

        Assert.True(filed < opens,
            "ShowStoryBeat files the seen-set INSIDE the presentation switch — a hosted beat would then be "
            + "shown (by its host) and never recorded, so its cadence never spends and #663's whole "
            + "once-only law is off for exactly the beats that do not raise their own surface.");

        Assert.True(logged > lastArm,
            "ShowStoryBeat's log line has moved inside the presentation switch — the log is the only place a "
            + "hosted beat's words survive the panel being closed (#761), and an arm that can skip it is an "
            + "arm that unwrites the beat.");

        // One write, not several: "marks it seen exactly once" is a property of there being one filing.
        Assert.Single(Regex.Matches(body, @"_beatsSpoken\[SeenKey\(beat, subject\)\] = SimTime;"));
        Assert.Single(Regex.Matches(body, @"LogAutopilotEvent\("));
    }

    // ── Where the beat's words are readable ───────────────────────────────────────────────────────────

    /// <summary>The Demand panel's own markup: its <c>case</c> in Map.razor, up to the next stage's. Cutting
    /// the subtree is the point — a caption rendered elsewhere in this file is a caption on somebody else's
    /// pop-up, which is the #736 bug rather than the fix.</summary>
    private static string DemandPanel()
    {
        string razor = Pages("Map.razor");
        int start = razor.IndexOf("case BustedEncounter.Stage.Demand:", StringComparison.Ordinal);
        Assert.True(start >= 0, "Map.razor no longer has the Demand panel this guard knows how to find.");
        int end = razor.IndexOf("case BustedEncounter.Stage.", start + 40, StringComparison.Ordinal);
        return razor[start..(end > start ? end : razor.Length)];
    }

    /// <summary>
    /// THE HOSTED BEAT'S WORDS ARE ON THE HOST. #761: <i>"when something plot-significant happens, the player
    /// is told clearly, at the moment it happens, on the surface they are looking at."</i> #736: not in a
    /// banner behind the backdrop. A hosted beat has no card of its own to carry its caption, so the host
    /// owes it the words as well as the picture — otherwise hosting is just a quieter way of losing the
    /// prose, and the shipped sentence about grapples coming across the frame would exist only in the log.
    ///
    /// <para><b>Proven RED</b> by removing the caption line from the panel:</para>
    /// <code>
    /// the BUSTED demand panel shows StoryBeats.Beat.CollectorHail's painting and not its caption — the beat
    /// is HOSTED here, so this panel is the only surface its words can be read on.
    /// </code>
    /// </summary>
    [Fact]
    public void TheDemandPanelCarriesTheHailsCaptionAndNotJustItsPainting()
    {
        string panel = DemandPanel();

        Assert.True(panel.Contains("StoryBeats.ArtFile(StoryBeats.Beat.CollectorHail)", StringComparison.Ordinal),
            "the BUSTED demand panel no longer shows the hail's painting — this guard is reading the wrong block.");

        Assert.True(panel.Contains("StoryBeats.Caption(StoryBeats.Beat.CollectorHail, bust.HunterCallsign)", StringComparison.Ordinal),
            "the BUSTED demand panel shows StoryBeats.Beat.CollectorHail's painting and not its caption — the "
            + "beat is HOSTED here (StoryBeats.Presentation.Hosted), so this panel is the only surface its "
            + "words can be read on (#761, #736).");

        // The caption is quoted from Core, never retyped. A second copy of a shipped sentence is how the card
        // and the log start disagreeing about what happened.
        Assert.True(!panel.Contains("Grapples come across the frame", StringComparison.Ordinal),
            "the demand panel has a hand-typed copy of the hail's caption — it must quote StoryBeats.Caption, "
            + "so the panel and the log line the seam writes cannot drift apart.");
    }

    /// <summary>
    /// …and it is on THAT panel rather than merely somewhere in the file. Owner, on #680: <i>"note that
    /// seeing the text in DOM does not mean user can see it on the screen."</i> The cut above is what makes
    /// the claim about placement, so the cut has to be shown to be a cut: it holds what only the demand panel
    /// holds, and none of what the panels after it hold.
    /// </summary>
    [Fact]
    public void TheCutIsReallyTheDemandPanelAndNotTheWholeFile()
    {
        string panel = DemandPanel();
        string razor = Pages("Map.razor");

        Assert.True(panel.Length < razor.Length / 4, "the 'demand panel' cut is most of Map.razor — it is not a subtree.");
        Assert.Contains("💰 BRIBE", panel, StringComparison.Ordinal);              // only the demand offers one
        Assert.DoesNotContain("🔫 THEY BOARD ANYWAY", panel, StringComparison.Ordinal);  // the ResistLost panel
        Assert.DoesNotContain("busted-receipt", panel, StringComparison.Ordinal);        // the Confiscated panel

        // And the caption is rendered in exactly one place in the whole file, so "in the panel" is the same
        // statement as "not in some other panel".
        Assert.Single(Regex.Matches(razor, @"StoryBeats\.Caption\(StoryBeats\.Beat\.CollectorHail"));
    }

    /// <summary>
    /// The caption's element is STYLED, and styled to be read over a photograph. The hail block is a
    /// left-weighted scrim over art at half opacity with an 800-weight alarm-red heading — prose inheriting
    /// that would be a shout in pink over a picture, which is the DOM-is-not-the-screen failure again, this
    /// time in CSS.
    /// </summary>
    [Fact]
    public void TheCaptionHasItsOwnLegibleStyleOverTheArt()
    {
        string css = File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor.css"));

        int at = css.IndexOf(".busted-hail-caption {", StringComparison.Ordinal);
        Assert.True(at >= 0,
            "the hosted hail's caption has no rule of its own — it would inherit .busted-hail's 800-weight "
            + "alarm red and sit unscrimmed on the artwork.");

        string rule = css[at..css.IndexOf('}', at)];
        Assert.Contains("z-index", rule, StringComparison.Ordinal);     // above .busted-hail-art
        Assert.Contains("background", rule, StringComparison.Ordinal);  // its own scrim
        Assert.Contains("text-align: left", rule, StringComparison.Ordinal);
    }

    // ── The law, as Core states it ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The presentation and the wiring agree. Both halves matter: a beat wired to a host it does not declare
    /// would get a second modal from the seam, and a beat that DECLARES a host nobody hosts would be an
    /// orphan wearing the fix's clothes.
    /// </summary>
    [Fact]
    public void CoreSaysTheHailIsHostedAndNamesTheCardThatHostsIt()
    {
        Assert.Equal(StoryBeats.Presentation.Hosted, StoryBeats.PresentationOf(StoryBeats.Beat.CollectorHail));
        Assert.Contains("BUSTED", StoryBeats.HostCard(StoryBeats.Beat.CollectorHail), StringComparison.Ordinal);

        // Every beat Core calls hosted really is hosted by a demand-panel-shaped thing in the client: it has
        // a named host, and something raises it.
        foreach (StoryBeats.Beat beat in Enum.GetValues<StoryBeats.Beat>())
        {
            if (StoryBeats.PresentationOf(beat) != StoryBeats.Presentation.Hosted)
            {
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(StoryBeats.HostCard(beat)),
                $"{beat} is presented HOSTED and names no host — nothing would ever show it.");

            bool raised = ClientSource().Any(s => s.Text.Contains($"RaiseStoryBeat(StoryBeats.Beat.{beat}", StringComparison.Ordinal));
            Assert.True(raised,
                $"{beat} is presented HOSTED and nothing raises it — hosting moves the CANVAS to the caller, "
                + "not the bookkeeping, so an unraised hosted beat is still an orphan (#663).");
        }
    }
}

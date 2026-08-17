using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #777, the other half · A HOSTED BEAT IS COUNTED AS TOLD ONLY WHILE ITS HOST IS ACTUALLY ON THE SCREEN.
///
/// <para>#777 split a story beat's two jobs apart. The SURFACE went to the caller — the collector's hail
/// arrives as the BUSTED demand panel, which has rendered that beat's painting since #528, so raising it the
/// ordinary way would have stacked a second modal showing the identical picture. The BOOKKEEPING stayed at
/// the seam: cadence spent, seen-set filed, the words written into the ledger, and the beat finally counted
/// by #663's scanner instead of sitting there as a painted orphan.</para>
///
/// <para>That trade left one thing unguarded, and it is the thing the whole issue serves. The seam now
/// writes <i>"this was told"</i> about a surface it does not own. Both shipped edges do it right — the
/// hunter's catch on her own deck and the writ served on foot both set the panel one statement before they
/// knock — and <see cref="TheHailIsHostedByTheCardItArrivesOnTests"/> reads the source of both and says so.
/// But <i>"the two callers that exist today are correct"</i> is a smaller law than <i>"a hosted beat is
/// never counted as told with nothing on the screen"</i>, and the caller that breaks it is the third one,
/// which nobody has reviewed yet. #761: when something plot-significant happens the player is told, on the
/// surface they are looking at. A beat filed as told over an empty screen does not break that law loudly —
/// it deletes the evidence that it was broken, because the seen-set is the only record anybody checks.</para>
///
/// <para>So the seam asks, and this file drives a REAL <see cref="Pages.Map"/> to prove it asks. Nothing
/// here reads source text: the panel is opened by the shipping <c>ApplyHunterCatch</c> with a hunter from
/// <c>EncounterRule.SpawnHunter</c>, the beat goes through the shipping <c>RaiseStoryBeat</c>, and every
/// claim is read off the component's own fields afterwards. A guard that grepped for the word
/// <c>TheHostIsUp</c> would pass on a build where the call sat below the filing.</para>
///
/// <para>Each test names the exact revert that turns it RED in its own remarks.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheHostedBeatIsCountedOnlyWhenItsHostIsUpTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// A live component with nothing running but the seam.
    ///
    /// <para>The one piece of theatre is the render handle, borrowed from
    /// <see cref="MustStandUpBeforeWalkingTests"/>: a <see cref="ComponentBase"/> that was never attached to a
    /// renderer throws out of <c>StateHasChanged</c>, and the seam ends every raise with one. Telling it a
    /// render is already queued hits the framework's own early-out. Nothing else is faked.</para>
    /// </summary>
    private static Pages.Map OnHerDeck()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has "
                + "moved, and the seam will throw instead of running.");
        pending.SetValue(map, true);

        return map;
    }

    /// <summary>The collector who catches you. Spawned by Core's own rule, never hand-built.</summary>
    private static HunterState AGrudge() =>
        EncounterRule.SpawnHunter("hunter-3", "GRUDGE", "earth", Vector2d.Zero, Vector2d.Zero, 0.0);

    private static void Call(Pages.Map map, string method, params object?[] args) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{method}` — this guard needs re-reading."))
        .Invoke(map, args);

    /// <summary>The shipped catch: warp yanked, the panel opened, the hail raised.</summary>
    private static void TheCollectorCatchesYou(Pages.Map map) => Call(map, "ApplyHunterCatch", AGrudge());

    /// <summary>The one door. Called exactly as a feature calls it.</summary>
    private static void Raise(Pages.Map map, StoryBeats.Beat beat, string? subject) =>
        Call(map, "RaiseStoryBeat", beat, subject);

    private static object? Field(Pages.Map map, string name) =>
        (typeof(Pages.Map).GetField(name, Hidden)
         ?? throw new InvalidOperationException($"Map has no `{name}` — this guard needs re-reading."))
        .GetValue(map);

    /// <summary>Every beat the seam has written down, and what each one was about.</summary>
    private static IReadOnlyDictionary<(StoryBeats.Beat Beat, string? Subject), double> Filed(Pages.Map map) =>
        (IReadOnlyDictionary<(StoryBeats.Beat, string?), double>)Field(map, "_beatsSpoken")!;

    /// <summary>The ledger, newest first — where a beat's words survive its picture, and where the seam
    /// shouts when a caller hands it a hosted beat with nothing to host it.</summary>
    private static IReadOnlyList<string> Ledger(Pages.Map map) =>
        [.. ((IEnumerable<(double SimTime, string Text)>)Field(map, "_autopilotEvents")!).Select(e => e.Text)];

    /// <summary>The open BUSTED encounter, or null. Its <c>Phase</c> is reached by name because the type is
    /// private to the page.</summary>
    private static object? ThePanel(Pages.Map map) => Field(map, "_busted");

    private static string PhaseOf(object panel) =>
        panel.GetType().GetProperty("Phase", Hidden)!.GetValue(panel)!.ToString()!;

    private static void SetPhase(object panel, string stage)
    {
        PropertyInfo phase = panel.GetType().GetProperty("Phase", Hidden)!;
        phase.SetValue(panel, Enum.Parse(phase.PropertyType, stage));
    }

    /// <summary>Empty the record without touching the world. Used to ask the seam a SECOND question of the
    /// same live component — the panel stays exactly where the shipped code put it.</summary>
    private static void ClearTheBooks(Pages.Map map)
    {
        ((System.Collections.IDictionary)Field(map, "_beatsSpoken")!).Clear();
        ((System.Collections.IList)Field(map, "_autopilotEvents")!).Clear();
    }

    private static int TimesFiled(Pages.Map map, StoryBeats.Beat beat) =>
        Filed(map).Keys.Count(k => k.Beat == beat);

    private static int LinesSaying(Pages.Map map, string fragment) =>
        Ledger(map).Count(l => l.Contains(fragment, StringComparison.Ordinal));

    // ── The catch, end to end ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SHIPPED EDGE, DRIVEN: the collector catches you, the demand panel opens, the hail is counted
    /// exactly once — and the seam raises no second surface of its own.
    ///
    /// <para>This is the claim #777 was filed to make, asked of the running component instead of of the
    /// source. Four things are true at once afterwards and only all four together mean anything: the panel
    /// is up on its DEMAND stage (the canvas exists), the beat is in the seen-set (it was counted), its
    /// words are in the ledger (they survive the panel closing, #761), and <c>_storyCard</c>,
    /// <c>_storyPlate</c> and <c>_deferredBeat</c> are all still empty (nothing was stacked over the
    /// canvas, nothing was parked at its edge, nothing was queued to arrive after it).</para>
    ///
    /// <para><b>Proven RED</b> by putting <c>_storyCard = (beat, subject);</c> into the seam's hosted arm —
    /// the naive raise, which is what the old two-answer seam did with this beat:</para>
    /// <code>
    /// Assert.Null() Failure: Value is not null
    /// Expected: null
    /// Actual:   (CollectorHail, GRUDGE)
    /// </code>
    /// <para>and RED again by deleting the raise from <c>ApplyHunterCatch</c> — the shipped state before
    /// #777 — on "the collector's hail was not filed as told: 0 entries".</para>
    /// </summary>
    [Fact]
    public void TheCatchOpensThePanelAndCountsTheHailWithoutRaisingASecondSurface()
    {
        Pages.Map map = OnHerDeck();

        TheCollectorCatchesYou(map);

        object panel = ThePanel(map)
            ?? throw new InvalidOperationException("the catch opened no BUSTED encounter — this guard cannot run.");
        Assert.Equal("Demand", PhaseOf(panel));

        Assert.True(TimesFiled(map, StoryBeats.Beat.CollectorHail) == 1,
            "the collector's hail was not filed as told exactly once: "
            + $"{TimesFiled(map, StoryBeats.Beat.CollectorHail)} entries. The demand panel is that beat's "
            + "canvas (StoryBeats.Presentation.Hosted), so the panel opening IS the beat happening, and an "
            + "edge that opens it without the seam counting it leaves the beat an orphan again (#663).");

        Assert.True(LinesSaying(map, StoryBeats.Title(StoryBeats.Beat.CollectorHail)) == 1,
            "the hail's words are not in the ledger exactly once — the ledger is the only place a hosted "
            + "beat's prose outlives the card that carried it (#761).");
        Assert.Contains(StoryBeats.Caption(StoryBeats.Beat.CollectorHail, "GRUDGE"),
            Ledger(map).Single(l => l.Contains(StoryBeats.Title(StoryBeats.Beat.CollectorHail), StringComparison.Ordinal)),
            StringComparison.Ordinal);

        // …and nothing at all was raised over it.
        Assert.Null(Field(map, "_storyCard"));
        Assert.Null(Field(map, "_storyPlate"));
        Assert.Null(Field(map, "_deferredBeat"));

        Assert.True(LinesSaying(map, "⚠ ENGINE") == 0,
            "the seam refused a raise whose host WAS up — the host check is answering no to the one case it "
            + "must answer yes to, and every other test in this file would then be passing on a seam that "
            + "refuses everything.");
    }

    /// <summary>
    /// …and the seam alone does the same thing, asked twice of one live panel.
    ///
    /// <para>The arrangement is the shipped catch and then a cleared record: the BUSTED encounter is left
    /// exactly where <c>ApplyHunterCatch</c> put it, so the host is a real one built by shipping code rather
    /// than a hand-typed stand-in — the world a guard builds for itself is the world that cannot tell pass
    /// from fail. Then the seam is knocked on directly. It files once, logs once, shows nothing.</para>
    ///
    /// <para><b>Proven RED</b> by moving <c>_beatsSpoken[SeenKey(beat, subject)] = SimTime;</c> down into the
    /// card and plate arms of the presentation switch — the shape in which a beat with no arm of its own is
    /// shown by its host and never recorded:</para>
    /// <code>
    /// a hosted raise onto a live host counted nothing: 0 seen-set entries — the panel would show the
    /// grapples and the seam would go on believing the beat had never been told.
    /// </code>
    /// </summary>
    [Fact]
    public void ARaiseOntoALiveHostKeepsTheBooksAndNothingElse()
    {
        Pages.Map map = OnHerDeck();
        TheCollectorCatchesYou(map);
        ClearTheBooks(map);

        Raise(map, StoryBeats.Beat.CollectorHail, "GRUDGE");

        Assert.True(TimesFiled(map, StoryBeats.Beat.CollectorHail) == 1,
            $"a hosted raise onto a live host counted nothing: {Filed(map).Count} seen-set entries — the "
            + "panel would show the grapples and the seam would go on believing the beat had never been told.");
        Assert.True(Ledger(map).Count == 1, $"expected the one hail line in the ledger, found {Ledger(map).Count}.");
        Assert.Contains(StoryBeats.Title(StoryBeats.Beat.CollectorHail), Ledger(map)[0], StringComparison.Ordinal);

        Assert.Null(Field(map, "_storyCard"));
        Assert.Null(Field(map, "_storyPlate"));
        Assert.Null(Field(map, "_deferredBeat"));
    }

    // ── …and with nothing to host it ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE GUARD THIS LANE EXISTS FOR: a hosted raise with no host up is REFUSED, loudly, and counts nothing.
    ///
    /// <para>Refused rather than thrown, because this is an engine mistake and a seam that crashes the page
    /// is worse than the mistake it is objecting to. What must not happen is the quiet version: the beat
    /// filed, the ledger saying the grapples came across the frame, and the player looking at open space.
    /// For a beat with a once-ever cadence that filing is permanent — the moment is spent, unseen, forever.
    /// So: nothing counted, nothing shown, and a line in the ledger that names the beat and the host Core
    /// says should have been carrying it.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>TheHostIsUp</c> gate from the top of <c>ShowStoryBeat</c> —
    /// the state of the tree before this lane:</para>
    /// <code>
    /// a hosted beat was raised with NO host on the screen and the seam counted it as told anyway: 1
    /// seen-set entries. The player saw nothing; the record says otherwise, which is the one failure #761
    /// cannot recover from, because the seen-set is the only place anybody checks.
    /// </code>
    /// </summary>
    [Fact]
    public void ARaiseWithNoHostUpIsRefusedLoudlyAndCountsNothing()
    {
        Pages.Map map = OnHerDeck();
        Assert.Null(ThePanel(map));   // nobody has grappled anybody

        Raise(map, StoryBeats.Beat.CollectorHail, "GRUDGE");

        Assert.True(Filed(map).Count == 0,
            "a hosted beat was raised with NO host on the screen and the seam counted it as told anyway: "
            + $"{Filed(map).Count} seen-set entries. The player saw nothing; the record says otherwise, which "
            + "is the one failure #761 cannot recover from, because the seen-set is the only place anybody "
            + "checks.");

        Assert.Null(Field(map, "_storyCard"));
        Assert.Null(Field(map, "_storyPlate"));
        Assert.Null(Field(map, "_deferredBeat"));

        // Loudly: the ledger names the beat, and quotes Core's own sentence about what should have hosted it.
        Assert.True(LinesSaying(map, "⚠ ENGINE") == 1,
            "the refusal was silent — an engine fault that leaves no line behind is indistinguishable from a "
            + "beat that simply never happened, and this one is a wiring bug somebody has to be able to find.");
        string said = Ledger(map).Single(l => l.Contains("⚠ ENGINE", StringComparison.Ordinal));
        Assert.Contains(nameof(StoryBeats.Beat.CollectorHail), said, StringComparison.Ordinal);
        Assert.Contains(StoryBeats.HostCard(StoryBeats.Beat.CollectorHail), said, StringComparison.Ordinal);

        // And it is a REFUSAL, not a swallow: the beat's own prose is not written as though it had been told.
        Assert.True(LinesSaying(map, StoryBeats.Title(StoryBeats.Beat.CollectorHail)) == 0,
            "the refused raise still wrote the hail's words into the ledger — the log line is the seam saying "
            + "this reached the player, and nothing reached anybody.");
    }

    /// <summary>
    /// …AND THE CHECK IS ABOUT THE PANEL THAT IS SHOWING, NOT ABOUT THE ENCOUNTER OBJECT EXISTING.
    ///
    /// <para>The same <c>BustedEncounter</c> goes on to carry the confiscation receipt, the freeze-frame, and
    /// the clinic where a new captain wakes up. None of those show a collector's grapples. A host check
    /// written as <c>_busted is not null</c> would count the hail as told over a picture of the captain
    /// dying — the sim doing one thing while the record reports another, which is a bug class this repo has
    /// already named.</para>
    ///
    /// <para><b>Proven RED</b> by relaxing <c>TheHostIsUp</c> to <c>_busted is not null</c>:</para>
    /// <code>
    /// the hail was counted as told while the BUSTED encounter was on its FreezeFrame stage: 1 seen-set
    /// entries. That panel shows the captain's death, not a collector's grapples.
    /// </code>
    /// </summary>
    [Fact]
    public void AnEncounterPastTheDemandPanelIsNotTheHailsHost()
    {
        Pages.Map map = OnHerDeck();
        TheCollectorCatchesYou(map);

        object panel = ThePanel(map)!;
        SetPhase(panel, "FreezeFrame");
        ClearTheBooks(map);

        Raise(map, StoryBeats.Beat.CollectorHail, "GRUDGE");

        Assert.True(Filed(map).Count == 0,
            "the hail was counted as told while the BUSTED encounter was on its FreezeFrame stage: "
            + $"{Filed(map).Count} seen-set entries. That panel shows the captain's death, not a collector's "
            + "grapples.");
        Assert.True(LinesSaying(map, "⚠ ENGINE") == 1, "the wrong-stage raise was refused silently.");
    }

    /// <summary>
    /// A REFUSED BEAT IS UNSPENT. The reason the seam refuses instead of filing-and-shrugging: the moment can
    /// still be told properly the next time its host is genuinely up. Asked of one component in one run, so
    /// the second raise is answered by a seam that has already seen the first.
    ///
    /// <para>This is also what stops the two tests above from passing on a seam that has simply stopped
    /// working: a refusal that could never be followed by a real telling would be a beat deleted rather than
    /// a beat held.</para>
    /// </summary>
    [Fact]
    public void ABeatRefusedForWantOfAHostCanStillBeToldWhenTheHostArrives()
    {
        Pages.Map map = OnHerDeck();

        Raise(map, StoryBeats.Beat.CollectorHail, "GRUDGE");
        Assert.Empty(Filed(map));

        TheCollectorCatchesYou(map);

        Assert.True(TimesFiled(map, StoryBeats.Beat.CollectorHail) == 1,
            "a hail refused for want of a host stayed refused after the demand panel actually opened — the "
            + "gate is spending the cadence it is meant to be protecting.");
    }

    // ── The law, over every hosted beat there is ──────────────────────────────────────────────────────

    /// <summary>
    /// AND IT IS A RULE, NOT A ROW. Every beat Core presents as
    /// <see cref="StoryBeats.Presentation.Hosted"/> must have an answer in the client's host check — and the
    /// check must not have a permissive default arm, because the twelfth beat somebody marks hosted and
    /// forgets to answer for is exactly the beat this whole file is about.
    ///
    /// <para>Driven, not read: a fresh component with nothing open at all is asked about each hosted beat in
    /// turn, and must refuse every one. On today's tree that is the hail, and it will be whatever the next
    /// one is without anybody editing this test.</para>
    ///
    /// <para><b>Proven RED</b> by giving <c>TheHostIsUp</c> a <c>_ =&gt; true</c> arm.</para>
    /// </summary>
    [Fact]
    public void EveryHostedBeatIsRefusedOnAScreenWithNothingOnIt()
    {
        StoryBeats.Beat[] hosted =
            [.. Enum.GetValues<StoryBeats.Beat>()
                .Where(b => StoryBeats.PresentationOf(b) == StoryBeats.Presentation.Hosted)];

        Assert.NotEmpty(hosted);   // a claim about an empty set is not a claim

        foreach (StoryBeats.Beat beat in hosted)
        {
            Pages.Map map = OnHerDeck();
            Raise(map, beat, "GRUDGE");

            Assert.True(Filed(map).Count == 0,
                $"{beat} is presented HOSTED and the seam counted it as told on a screen with nothing open on "
                + "it at all. Either its host is missing from Map.StoryCards' TheHostIsUp, or that check has "
                + "grown a permissive default arm — and a beat whose host nobody can name is a beat nobody "
                + "can be shown.");
        }
    }
}

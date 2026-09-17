using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SpaceSails.Client.Pages;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Core;
using Xunit;

// BL0005: this bench sets the tracking post's parameters from outside rather than standing the component up
// in a render tree — the same licence TheDeskReadsTheStateNotThePercentTests takes next door, and for the same
// reason: a harness is not the thing under test.
#pragma warning disable BL0005

namespace SpaceSails.Client.Tests;

/// <summary>
/// #238 item 3 / #239 item 2 · <b>THE DESK SIDE OF "SWEEP HOLDS THE SCOPE."</b>
///
/// <para>Core's <c>SweepHoldsTheScopeTests</c> proves the sentence and the predicate. This proves the two
/// things only the client can get wrong: that the REAL desk, ticked the way the page ticks it, reaches the
/// owner's exact situation and says the owner's exact words — and that the <b>panel and the desk chip print
/// the same string</b>, which is the one failure a line rendered on two surfaces actually has available to it
/// (#203, one voice).</para>
///
/// <para><b>Why the manual sweep is the interesting case.</b> It is not a task. <c>RunScheduledInstrument</c>
/// calls <c>TelescopeSchedule.Interrupt</c> the moment one is running, so the carousel has no active job and
/// NOTHING in the list is RUNNING — which is exactly why #239's four honest words were all true and all
/// silent on the night that cost the owner the hunt.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheSweepSaysWhatItIsHoldingUpTests
{
    private const BindingFlags Hidden = TestTree.PrivateOnAnInstance;

    private static readonly Vector2d ShipAt = new(1.2e11, 0);

    /// <summary>Where the tip says she is, and where the aimed scan is therefore pointed.</summary>
    private static readonly Vector2d RoadsterAt = new(2.1e11, 3e10);

    private const double WreckScanRadiusM = 4e10; // the page's own generous box (Map.Npc.Tasking)

    /// <summary>The owner's sentence, typed out once here and nowhere else in this suite — so a template
    /// quietly reworded in Core has to come and argue with his own words.</summary>
    private const string TheOwnersLine = "sweep holds the scope — 🔭 Roadster fix waits behind it";

    // ── THE REAL DESK, IN THE OWNER'S OWN SITUATION ───────────────────────────────────────────────────

    /// <summary>
    /// A 77 % MANUAL SWEEP WITH THE ROADSTER FIX QUEUED BENEATH IT. Driven through the component's real
    /// parameter tick, with the sweep started through the desk's own <c>StartSweep</c> — not by poking fields
    /// — so what is under test is the state the captain actually puts the instrument in.
    /// </summary>
    [Fact]
    public void TheDeskSaysWhatTheHandFlownSweepIsHoldingUp()
    {
        TrackingPost post = ADeskWithTheRoadsterFixQueued();

        // Nothing has been said yet — and this is the passive watch's frame, the one that looks exactly like
        // the bug: the idle survey is still on the glass with the aimed scan queued behind it. It stands down
        // by itself on the next tick and the captain has nothing to do about it, so it is not a HOLDER.
        Assert.Null(post.ScopeHoldLine);

        TakeTheScopeByHand(post);
        Tick(post, 200);

        // The manual sweep really did take the instrument out from under the carousel — no task is RUNNING,
        // which is the whole reason the four state words could not carry this.
        Assert.All(post.TaskQueue, t => Assert.NotEqual(SensorTaskState.Running, StateOf(post, t.Id)));
        Assert.Equal(TheOwnersLine, post.ScopeHoldLine);
    }

    /// <summary>
    /// AND IT GOES QUIET AGAIN WHEN NOTHING IS LIVE. The same desk, the same sweep, the same queued scan —
    /// with the contract handed in (the page hands down no live targets), the aimed scan is just a patch of
    /// sky and the sweep is in nobody's way. Without this the test above would pass on a desk that printed
    /// the line unconditionally.
    /// </summary>
    [Fact]
    public void TheDeskSaysNothingWhenNoContractIsWaitingOnThatPatchOfSky()
    {
        TrackingPost post = ADeskWithTheRoadsterFixQueued();
        post.LiveQuestTargets = [];

        TakeTheScopeByHand(post);
        Tick(post, 200);

        Assert.Null(post.ScopeHoldLine);
    }

    // ── ONE FACT, TWO SURFACES ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PANEL AND THE CHIP PRINT THE SAME STRING. Read off the page's own <c>SensorsChip()</c> — the method
    /// that builds the strip's Sensors card — with the real desk under it, so this is the composition a captain
    /// glancing at the chip actually gets, compared against the composition the desk hands the task panel.
    /// </summary>
    [Fact]
    public void TheDeskChipsSecondLineIsTheSameSentenceTheTaskPanelPrints()
    {
        TrackingPost post = ADeskWithTheRoadsterFixQueued();
        TakeTheScopeByHand(post);
        Tick(post, 200);

        DeskChips.ChipData chip = SensorsChipOverTheDesk(post);

        Assert.Equal(TheOwnersLine, post.ScopeHoldLine);
        Assert.Equal(post.ScopeHoldLine, chip.Line2);
    }

    /// <summary>
    /// …AND THE CHIP GOES BACK TO ITS TRACK COUNT WHEN THERE IS NO COMPLAINT. The second line is a standing
    /// number that this sentence stands on for as long as the obstruction lasts; a chip that kept the
    /// complaint after the sweep ended would be a stale warning, which is worse than none.
    /// </summary>
    [Fact]
    public void WithNothingHeldUpTheChipsSecondLineIsTheTrackCountAgain()
    {
        TrackingPost post = ADeskWithTheRoadsterFixQueued();
        Tick(post, 200);

        Assert.Null(post.ScopeHoldLine);
        Assert.Equal("0/1 tracks", SensorsChipOverTheDesk(post).Line2);
    }

    // ── THE SHIPPING MARKUP PRINTS IT, AND DOES NOT SPELL IT ──────────────────────────────────────────

    /// <summary>
    /// THE TASK PANEL READS THE SENTENCE, IT DOES NOT WRITE ONE. Both places a running row can be — the
    /// hand-flown holder that has no row of its own, and a scheduled pass that does — render the desk's own
    /// <c>ScopeHoldLine</c>. Read off the live razor through <see cref="TrackingPostMarkup"/>, because a
    /// component bench cannot see markup it never renders.
    /// </summary>
    [Fact]
    public void TheSensorTasksPanelPrintsTheDesksOwnHoldLineAndSpellsNoneOfItItself()
    {
        string box = SensorTasksBlock(TrackingPostMarkup.Read(TrackingPostMarkup.PagePath));

        Assert.Contains("ScopeHoldLine", box, StringComparison.Ordinal);

        // Two render sites, because the holder is sometimes a queue row and sometimes the manual sweep that
        // has none. One of them alone leaves half the cases silent.
        Assert.Equal(2, Occurrences(box, "ScopeHoldLine is { }"));

        // …and neither of them words it. The sentence is Core's.
        Assert.DoesNotContain("holds the scope", box, StringComparison.Ordinal);
        Assert.DoesNotContain("waits behind it", box, StringComparison.Ordinal);
    }

    /// <summary>THE CHIP DOES NOT WORD IT EITHER — it reads the desk's property, which is the only way the
    /// two surfaces can be held to one sentence.</summary>
    [Fact]
    public void TheSensorsChipReadsTheDesksHoldLineRatherThanComposingOne()
    {
        string uiState = File.ReadAllText(Path.Combine(
            SurfaceComposition.RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.UiState.cs"));

        Assert.Contains("_trackingPost?.ScopeHoldLine", uiState, StringComparison.Ordinal);
        Assert.DoesNotContain("holds the scope —", uiState, StringComparison.Ordinal);
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The desk as the 🔭 intel button leaves it: the aimed area scan on the carousel, pointed at where the
    /// tip says she is, and the page holding that same body up as the live contract's target. Enqueued through
    /// <c>EnqueueAndPrioritize</c> — the page's own door — so the task under test is the task the button makes.
    /// </summary>
    private static TrackingPost ADeskWithTheRoadsterFixQueued()
    {
        var post = new TrackingPost
        {
            ShipPosition = ShipAt,
            ShipVelocity = Vector2d.Zero,
            MaxTracks = 1,
            TelescopeSpeedFactor = 1,
            Candidates = [],
            LiveQuestTargets = [new QuestScopeTarget(Derelict.RoadsterBodyId, RoadsterAt)],
        };

        Tick(post, 0);
        post.EnqueueAndPrioritize(
            SensorTask.AreaScan(RoadsterAt, WreckScanRadiusM, "intel fix · 🔭 Roadster orbit fix"));
        return post;
    }

    /// <summary>The captain puts his own hand on the instrument — the desk's own sweep button.</summary>
    private static void TakeTheScopeByHand(TrackingPost post) =>
        (typeof(TrackingPost).GetMethod("StartSweep", Hidden)
            ?? throw new MissingMethodException("TrackingPost has no StartSweep — this bench has drifted"))
        .Invoke(post, null);

    private static SensorTaskState? StateOf(TrackingPost post, string taskId) =>
        ((TelescopeSchedule)(typeof(TrackingPost).GetField("_schedule", Hidden)
            ?? throw new InvalidOperationException("TrackingPost has no _schedule — this bench has drifted"))
        .GetValue(post)!).StateOf(taskId);

    /// <summary>One turn of the ship's clock through the component's REAL parameter tick.</summary>
    private static void Tick(TrackingPost post, double toSimTime)
    {
        MethodInfo tick = typeof(TrackingPost).GetMethod("OnParametersSet", Hidden)
            ?? throw new MissingMethodException("TrackingPost has no OnParametersSet — this bench's tick has moved.");
        tick.Invoke(post, null);
        post.SimTime = toSimTime;
        tick.Invoke(post, null);
    }

    /// <summary>The page's own Sensors chip, built over this desk. <c>SensorsChip()</c> reads three page
    /// members and the tracking post; nothing here stands in for any of them.</summary>
    private static DeskChips.ChipData SensorsChipOverTheDesk(TrackingPost post)
    {
        var page = new Map();
        Field("_trackingPost").SetValue(page, post);
        Field("SimTime").SetValue(page, post.SimTime);

        MethodInfo chip = typeof(Map).GetMethod("SensorsChip", Hidden)
            ?? throw new MissingMethodException("Map has no SensorsChip — this bench has drifted");
        return (DeskChips.ChipData)chip.Invoke(page, null)!;
    }

    private static FieldInfo Field(string name) =>
        typeof(Map).GetField(name, Hidden)
        ?? throw new InvalidOperationException($"Map has no {name} — this bench has drifted");

    private static int Occurrences(string text, string needle)
    {
        int count = 0;
        for (int at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    /// <summary>The Sensor-tasks box out of the shipping razor, sliced structurally between its own container
    /// and the cold-case board that follows it — the same slice
    /// <c>TheDeskReadsTheStateNotThePercentTests</c> takes, and for the same reason.</summary>
    private static string SensorTasksBlock(string razor)
    {
        int start = razor.IndexOf("sensor-tasks-box", StringComparison.Ordinal);
        Assert.True(start > 0, "the Sensor tasks list is not in TrackingPost.razor at all — this bench has drifted");
        int end = razor.IndexOf("sensor-lost-box", start, StringComparison.Ordinal);
        Assert.True(end > start, "the cold-case board no longer follows the task list — this bench has drifted");
        return razor[start..end];
    }
}

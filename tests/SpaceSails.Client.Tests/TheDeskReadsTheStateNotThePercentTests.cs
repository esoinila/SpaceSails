using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Core;
using Xunit;

// BL0005: this bench sets the tracking post's parameters from outside rather than standing the component
// up in a render tree, the same licence TheScopeGoesWhereTheCaptainPointsItTests takes, and for the same
// reason: a harness is not the thing under test.
#pragma warning disable BL0005

namespace SpaceSails.Client.Tests;

/// <summary>
/// #239 · THE DESK SIDE — the chip is READ, and the row for a finished job does not vanish.
///
/// <para>The Core half of this fix (<c>SensorTasksWearTheirStatesTests</c>) proves the schedule now retains
/// where each job stands. That is worth nothing if the desk goes on inferring it. Owner, minutes after
/// filing: <i>"RUNNING-at-zero-progress is visually identical to WAITING — which is precisely why the state
/// chip must be its own signal, not inferred from the number… The percent is progress; the chip is
/// truth."</i></para>
///
/// <para>Two claims, and they fail differently. (a) The <b>real component</b>, ticked the way the page ticks
/// it, holds the states — so a desk whose own schedule forgot its passes trips here even if the markup is
/// perfect. (b) The <b>shipping razor</b> puts the state on every task row through Core's plate — so markup
/// that went back to printing a bare percentage trips here even if the schedule is perfect.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheDeskReadsTheStateNotThePercentTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly Vector2d ShipAt = new(1.2e11, 0);

    // ── (a) THE REAL DESK HOLDS THE STATES ────────────────────────────────────────────────────────────

    /// <summary>
    /// TICKED THE WAY THE PAGE TICKS IT, the desk's own telescope can say where each job stands: the one on
    /// the glass, the one behind it, and the one-shot that finished and left the carousel. Driven through
    /// <c>OnParametersSet</c> — the real clock — not by poking a schedule the desk does not own.
    /// </summary>
    [Fact]
    public void TheDesksOwnTelescopeKnowsWhichJobIsOnTheGlassAndWhichHasFinished()
    {
        TrackingPost post = ADeskWithWork();
        TelescopeSchedule schedule = ScheduleOf(post);

        SensorTask patch = SensorTask.AreaScan(PatchAt, PatchRadius, "the patch");
        Assert.True(post.EnqueueTask(patch));

        // Nothing has run yet: every job is queued and none is on the glass.
        Tick(post, 0);
        Assert.True(post.TaskQueue.Count >= 3, "the bench did not fill the carousel — nothing is being read");
        Assert.All(post.TaskQueue, t => Assert.Equal(SensorTaskState.Queued, schedule.StateOf(t.Id)));

        // Far enough for the one-shot patch to land and the standing pass behind it to be under way.
        Tick(post, 1_000);

        Assert.Equal(SensorTaskState.Done, schedule.StateOf(patch.Id));
        Assert.DoesNotContain(post.TaskQueue, t => t.Id == patch.Id);
        Assert.Contains(schedule.RecentlyDone, p => p.Task.Id == patch.Id);

        var live = post.TaskQueue.Select(t => schedule.StateOf(t.Id)).ToList();
        Assert.Contains(SensorTaskState.Running, live);
        Assert.Contains(SensorTaskState.Queued, live);

        // …and it is still DONE many passes later — the row does not blink out at the tick that finished it.
        Tick(post, 20_000);
        Assert.Equal(SensorTaskState.Done, schedule.StateOf(patch.Id));
        Assert.Contains(schedule.RecentlyDone, p => p.Task.Id == patch.Id);
    }

    /// <summary>
    /// THE NUMBER CANNOT CARRY THE MEANING. At the instant a pass begins, the job holding the telescope and
    /// the jobs behind it print the same percentage — which is the mistake the owner actually made. The
    /// states must differ where the numbers do not.
    /// </summary>
    [Fact]
    public void AtZeroPercentTheStatesStillDiffer()
    {
        TrackingPost post = ADeskWithWork();
        TelescopeSchedule schedule = ScheduleOf(post);

        Tick(post, 0);
        Tick(post, 0.001);   // the glass has just swung onto the first job

        Assert.Equal(0, (int)(schedule.ActiveProgress(post.SimTime) * 100));
        var states = post.TaskQueue.Select(t => schedule.StateOf(t.Id)).ToList();
        Assert.Contains(SensorTaskState.Running, states);
        Assert.Contains(SensorTaskState.Queued, states);
    }

    // ── (b) THE SHIPPING MARKUP PRINTS IT ─────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY TASK ROW WEARS THE CHIP, and the word on it is Core's, not the desk's own spelling (#203, one
    /// voice). Read off the live razor, because what is under test is the surface a captain looks at and a
    /// component test cannot see markup this bench never renders.
    /// </summary>
    [Fact]
    public void TheSensorTasksListPutsTheStateOnEveryRow()
    {
        string razor = File.ReadAllText(TrackingPostRazor);
        string box = SensorTasksBlock(razor);

        Assert.Contains("_schedule.StateOf(", box, StringComparison.Ordinal);
        Assert.Contains("SensorTaskPlates.Chip(", box, StringComparison.Ordinal);

        // The queue row and the finished row both carry it: a chip on the live jobs only would leave the
        // captain exactly where he was about the pass that just landed.
        int chips = Regex.Matches(box, @"SensorTaskPlates\.Chip\(").Count;
        Assert.True(chips >= 2,
            $"only {chips} row(s) in the Sensor tasks list wear a state chip — the DONE row this fix added "
            + "prints nothing, so a finished pass still vanishes from the desk.");

        // …and the finished passes really are rendered from the schedule's retained history.
        Assert.Contains("_schedule.RecentlyDone", box, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE VOCABULARY IS THE FOUR WORDS AND NOTHING ELSE. #239 buys four nouns; a fifth invented on the desk
    /// would be the one-voice rule broken in the same breath it was written.
    /// </summary>
    [Fact]
    public void TheChipSpeaksOnlyTheFourWordsCoreDefines()
    {
        var words = Enum.GetValues<SensorTaskState>().Select(SensorTaskPlates.For).ToList();
        Assert.Equal(4, words.Distinct().Count());
        Assert.All(words, w => Assert.Equal(w.ToUpperInvariant(), w));

        // The desk does not spell any of them itself.
        string box = SensorTasksBlock(File.ReadAllText(TrackingPostRazor));
        foreach (string word in words)
        {
            Assert.DoesNotContain($"\"{word}\"", box, StringComparison.Ordinal);
        }
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private static readonly Vector2d PatchAt = new(3e11, 0);

    private const double PatchRadius = 1e10;

    /// <summary>A desk with the carousel the real one has: two contacts on the telescope ledger, each of
    /// which the desk's own housekeeping turns into a STANDING custody pass. Entered through
    /// <c>ApplyObservation</c> — the door a sweep hit goes through — because a task list assembled by hand
    /// is a bench testing itself: the housekeeping sweeps out any standing pass the ledger does not hold.
    /// </summary>
    private static TrackingPost ADeskWithWork()
    {
        var post = new TrackingPost
        {
            ShipPosition = ShipAt,
            ShipVelocity = Vector2d.Zero,
            MaxTracks = 4,
            TelescopeSpeedFactor = 1,
            Candidates =
            [
                new TrackingPost.TrackingCandidate("a", "Ariadne", new ShipState(new Vector2d(1.3e11, 0), Vector2d.Zero, 0)),
                new TrackingPost.TrackingCandidate("b", "Bellona", new ShipState(new Vector2d(1.1e11, 0), Vector2d.Zero, 0)),
            ],
        };

        // One turn of the real tick first: MaxTracks reaches the ledger through OnParametersSet, and a
        // ledger still on its default of one would refuse the second contact.
        Tick(post, 0);

        Assert.True(post.ApplyObservation(new Observation("a", 0, new Vector2d(1.3e11, 0), Vector2d.Zero)));
        Assert.True(post.ApplyObservation(new Observation("b", 0, new Vector2d(1.1e11, 0), Vector2d.Zero)));
        return post;
    }

    private static TelescopeSchedule ScheduleOf(TrackingPost post) =>
        (TelescopeSchedule)(typeof(TrackingPost).GetField("_schedule", Hidden)
            ?? throw new InvalidOperationException("TrackingPost has no _schedule — this bench has drifted"))
        .GetValue(post)!;

    /// <summary>One turn of the ship's clock through the component's REAL parameter tick — the thing that
    /// runs the schedule and lands the passes.</summary>
    private static void Tick(TrackingPost post, double toSimTime)
    {
        MethodInfo tick = typeof(TrackingPost).GetMethod("OnParametersSet", Hidden)
            ?? throw new MissingMethodException("TrackingPost has no OnParametersSet — this bench's tick has moved.");
        tick.Invoke(post, null);
        post.SimTime = toSimTime;
        tick.Invoke(post, null);
    }

    /// <summary>The Sensor-tasks box out of the shipping razor: from its own container down to the cold-case
    /// board that follows it. Located structurally so this reads the block a captain sees, not a string that
    /// happens to appear somewhere in a 1,000-line file.</summary>
    private static string SensorTasksBlock(string razor)
    {
        int start = razor.IndexOf("sensor-tasks-box", StringComparison.Ordinal);
        Assert.True(start > 0, "the Sensor tasks list is not in TrackingPost.razor at all — this bench has drifted");
        int end = razor.IndexOf("sensor-lost-box", start, StringComparison.Ordinal);
        Assert.True(end > start, "the cold-case board no longer follows the task list — this bench has drifted");
        return razor[start..end];
    }

    private static string TrackingPostRazor
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                string candidate = Path.Combine(
                    dir, "src", "SpaceSails.Client", "Pages", "Stations", "TrackingPost.razor");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = Path.GetDirectoryName(dir);
            }

            throw new FileNotFoundException("TrackingPost.razor not found above " + AppContext.BaseDirectory);
        }
    }
}

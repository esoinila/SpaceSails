using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Pages.Stations;
using SpaceSails.Core;
using Xunit;

// BL0005: this bench SETS the tracking post's parameters from outside, because standing the component up
// in a real render tree just to hand it a ship position would be a guard against the harness. Scoped to
// this file, the same licence #765's telescope guard took for BL0006.
#pragma warning disable BL0005

namespace SpaceSails.Client.Tests;

/// <summary>
/// #962 · 📡 SHARPEN FIX WAS A DEAD BUTTON, AND THE READOUT COVERED FOR IT.
///
/// <para>Owner, playing a whole Debt Collector chase: <i>"I click sharpen fix but the sensors do nothing
/// useful. There should be a visual display of debt collector being scanned. Scanning the debt collector
/// should show on the task list as the only job now. This is a bug."</i> And, two screenshots later, with
/// the card still saying she is not on the ledger: <i>"but really HOW??????"</i>. And: <i>"why is our scan
/// looking at our destination when I press sharpen fix on the debt collector. It is like our telescope
/// pirate is high on drugs."</i></para>
///
/// <para>Three separate faults wore one face. (1) <c>TrackShipFromMenu</c> resolved its subject through
/// <c>FindNpc</c>, which walks <c>_npcStates</c> only — a hunter lives in <c>_hunters</c>, so every
/// collector id fell out of the first guard and the method returned in silence. (2) Even for traffic it
/// never queued a telescope task, so the "Sensor tasks" list — the thing the owner was looking at — could
/// not have shown the job. (3) The video scope's own lock chain skipped <c>_interestTargetId</c> (which is
/// the ONLY way a hunter ever becomes the dossier's subject) and skipped hunters in both its candidate list
/// and its resolver, so the box kept showing the destination no matter what was targeted.</para>
///
/// <para><b>Why this file drives the page instead of reading it.</b> A shape guard that "the handler
/// mentions <c>_hunters</c>" would stay green the day somebody changes what it does with them — the fifth
/// named bug class here is a guard that cannot tell a pass from a fail. So the handler the dossier's button
/// is ACTUALLY wired to is read off the shipping razor and then invoked, on a world with a collector in it,
/// and the telescope's own queue and the scope's own lock are asked what happened.</para>
///
/// <para><b>Red proof (run before shipping).</b> Put <c>NpcState? npc = FindNpc(id); if (npc is null ||
/// _trackingPost is null) { return; }</c> back at the top of <c>Map.Npc.TrackShipFromMenu</c> and the first
/// three tests go red — empty queue, empty ledger, no aim. Delete the <c>TacticalTargetId</c> branch from
/// <c>Map.Deck.Scope.PickScopeTarget</c> and the fourth goes red, naming the destination it fell back to.
/// Restore <c>_centerBearingDeg</c> in <c>TrackingPost.HeadingLine</c> and the fifth goes red at 0°.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheScopeGoesWhereTheCaptainPointsItTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    private const string HunterId = "hunter-0";
    private const string Callsign = "Debt Collector";

    // The collector sits due +Y of us, so the aim we expect is a bearing nothing else in the bench shares:
    // the destination is due +X, and the manual slider's resting place is 0°.
    private static readonly Vector2d ShipAt = new(1.2e11, 0);
    private static readonly Vector2d HunterAt = new(1.2e11, 4e8);

    // ── (a) THE BUTTON THE OWNER PRESSED ──────────────────────────────────────────────────────────────

    /// <summary>THE JOB IS ON THE LIST, AND IT IS THE NEXT ONE. Owner: "Scanning the debt collector should
    /// show on the task list as the only job now."</summary>
    [Fact]
    public void SHARPEN_FIX_OnACollector_PutsHerAtTheHeadOfTheTelescopeQueue()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();

        PressSharpenFixOnTheDossier(map);

        SensorTask queued = Assert.Single(post.TaskQueue);
        Assert.Equal(SensorTaskKind.TrackUpdate, queued.Kind);
        Assert.Equal(HunterId, queued.TargetShipId);
        Assert.Equal(Callsign, queued.Label);
    }

    /// <summary>…AND SHE IS ON THE LEDGER, so "Tracked targets 0/1" stops being the answer and the dossier
    /// stops saying "not on the telescope ledger — track her to sharpen the intel" at a captain who just
    /// did exactly that.</summary>
    [Fact]
    public void SHARPEN_FIX_OnACollector_PutsHerOnTheTelescopeLedger()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();

        Assert.False(post.TryGetTrack(HunterId, out _), "the bench started with her already tracked — this test would pass on a dead button.");
        PressSharpenFixOnTheDossier(map);

        Assert.True(post.TryGetTrack(HunterId, out TrackedTarget held));
        Assert.Equal(HunterId, held.ShipId);
    }

    /// <summary>AND THE INSTRUMENT IS POINTED AT HER. The queue is only half the promise; the other half is
    /// where the glass ends up. <c>JobFor</c> is the aim the telescope actually flies for a task — the same
    /// one the rosette wedge draws — so it is asked directly, and compared against the true bearing to the
    /// collector rather than against a number this test made up.</summary>
    [Fact]
    public void SHARPEN_FIX_OnACollector_AimsTheInstrumentAtHerBearing()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();

        PressSharpenFixOnTheDossier(map);

        ScanJob aim = post.JobFor(post.TaskQueue[0]);
        double toHer = TrackingStation.Bearing(HunterAt - ShipAt);
        Assert.Equal(toHer, aim.CenterBearingRad, 6);

        // …and that is NOT the direction of the destination, which is what the owner watched it do.
        Assert.NotEqual(TrackingStation.Bearing(DestinationAt - ShipAt), aim.CenterBearingRad, 3);
    }

    // ── (b) THE VIDEO BOX ─────────────────────────────────────────────────────────────────────────────

    /// <summary>THE BOOK AND THE BOX LOOK AT ONE CONTACT. A hunter becomes the dossier's subject through
    /// INTEREST (a map click on a collector marks interest, never selection), and interest was the one rung
    /// missing from the scope's lock chain — so the owner could open her book, press every button on it, and
    /// still watch the video box show Mercury.</summary>
    [Fact]
    public void THE_VIDEO_SCOPE_LocksOntoTheTargetOfInterestNotTheDestination()
    {
        (Pages.Map map, _) = AChaseInProgress();

        object locked = typeof(Pages.Map).GetMethod("PickScopeTarget", Hidden)!.Invoke(map, null)!;
        string name = (string)locked.GetType().GetProperty("Name")!.GetValue(locked)!;

        Assert.Equal(Callsign, name);
    }

    /// <summary>…and she rides the manual ▶◀ carousel too, so the captain can put her in the box by hand.
    /// She was absent from the candidate list entirely: the one contact worth watching was the one thing
    /// the scope could not be pointed at, by any route.</summary>
    [Fact]
    public void THE_VIDEO_SCOPE_CarriesHiredMuscleOnItsCarousel()
    {
        (Pages.Map map, _) = AChaseInProgress();

        var ids = (List<string>)typeof(Pages.Map).GetMethod("ScopeCandidates", Hidden)!.Invoke(map, null)!;

        Assert.Contains(HunterId, ids);
    }

    // ── (c) THE READOUT ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SENTENCE REPORTS THE LOOK THE INSTRUMENT IS TAKING. This is the repo's own named bug class —
    /// the sim doing one thing while a sentence reports another — and it was live here:
    /// <c>_centerBearingDeg</c> is written by exactly one thing in the whole codebase, the manual Bearing
    /// slider, while every queued pass aims the instrument through a different variable. So however the
    /// scope swung, "scope 0°" was the slider's last resting place, for ever.
    /// </summary>
    [Fact]
    public void THE_HEADING_LINE_QuotesTheRunningJobsAimAndNotTheSlider()
    {
        (_, TrackingPost post) = AChaseInProgress();
        post.EnqueueAndPrioritize(SensorTask.TrackUpdate(HunterId, Callsign));
        StartTheQueuedPass(post);

        string line = (string)typeof(TrackingPost).GetMethod("HeadingLine", Hidden)!.Invoke(post, null)!;

        int toHerDeg = (int)Degrees(TrackingStation.Bearing(HunterAt - ShipAt));
        Assert.Contains($"scope {toHerDeg}°", line, StringComparison.Ordinal);
        Assert.Contains(Callsign, line, StringComparison.Ordinal);
    }

    // ── (d) …AND WITH EVERY TELESCOPE ALREADY SPOKEN FOR ──────────────────────────────────────────────
    //
    // The three tests above stand the bench up with an EMPTY ledger, which is the one standing in which
    // the button already worked. The owner's screenshot is the other one: "Tracked targets (1 / 1)", the
    // destination depot holding the single slot, "Passive watch — 1 tracked, 1 slipped (telescopes full)",
    // and the Sensor tasks list carrying THE RED EYE DEPOT and nothing else after 📡 sharpen fix was
    // pressed on the collector. The order was placed, and then quietly deleted.

    private const string DepotId = "depot-red-eye";
    private const string DepotCallsign = "The Red Eye Depot";

    /// <summary>
    /// THE ORDER SURVIVES THE NEXT TICK. <c>HandleLostAndColdTracks</c> keeps the custody carousel in step
    /// with the ledger, and removed EVERY <c>TrackUpdate</c> whose subject the ledger does not hold — which
    /// is exactly and only the case a captain presses this button in when the telescopes are full. So the
    /// pass went on the list, the pulse said "she is the next look", and one tick later the list said
    /// nothing about her at all: the sim overruling a sentence, on the very button #962 was filed about.
    ///
    /// <para><b>Red proof (run before shipping).</b> Restore <c>SensorTaskKind.TrackUpdate =&gt;
    /// !_ledger.IsTracked(task.TargetShipId!)</c> in <c>TrackingPost.HandleLostAndColdTracks</c> and this
    /// test goes red on the tick, holding the depot's pass and nothing else.</para>
    /// </summary>
    [Fact]
    public void SHARPEN_FIX_WithEveryTelescopeHeld_KeepsHerLookOnTheSensorTasksList()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();
        TheOneTelescopeIsAlreadyHolding(post, DepotId);

        PressSharpenFixOnTheDossier(map);
        Assert.Contains(post.TaskQueue, t => t.TargetShipId == HunterId);

        ATickOfTheShipsClock(post, 60);

        Assert.Contains(post.TaskQueue, t => t.TargetShipId == HunterId);
    }

    /// <summary>
    /// AND THE PASS DOES SOMETHING WHEN IT LANDS. <c>HandlePass</c> answered a finished custody pass with
    /// <c>TrackedTargetLedger.TryConfirm</c>, which only ever refreshes an entry that ALREADY exists — so
    /// even had the order survived, the look would have completed and changed nothing. Here the captain
    /// frees a slot while the scope is on her (the Drop button on the Sensors desk, which is the answer to
    /// "but really HOW??????"), and the fix the pass earns is expected to land on the ledger.
    ///
    /// <para><b>Red proof.</b> Put the old <c>if (candidate is not null &amp;&amp; Ephemeris is not null)
    /// { _ledger.TryConfirm(…); }</c> back and this goes red with an empty ledger.</para>
    /// </summary>
    [Fact]
    public void SHARPEN_FIX_WithEveryTelescopeHeld_LandsHerOnTheLedgerOnceASlotIsFreed()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();
        TheOneTelescopeIsAlreadyHolding(post, DepotId);

        PressSharpenFixOnTheDossier(map);
        Assert.False(post.TryGetTrack(HunterId, out _), "the full ledger took her anyway — this bench is not testing the full case.");

        PressDropOnTheSensorsDesk(post, DepotId);   // …the captain frees the slot, the glass still on her
        ATickOfTheShipsClock(post, 4 * SensorTaskGeometry.TrackPassSeconds);

        Assert.True(post.TryGetTrack(HunterId, out _),
            "the ordered look completed and left the ledger empty — the pass did nothing at all.");
    }

    /// <summary>
    /// AND WHEN NO SLOT IS FREED, THE DESK SAYS SO — with her name in it, and what to do about it. The
    /// alternative is the job leaving the list mid-chase with no word, which is indistinguishable from the
    /// dead button. (This reads the same <c>_lastSweepMessage</c> the desk renders at
    /// <c>TrackingPost.razor</c> line 164.)
    /// </summary>
    [Fact]
    public void SHARPEN_FIX_WithEveryTelescopeHeld_TheDeskSaysWhyCustodyCouldNotBeKept()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();
        TheOneTelescopeIsAlreadyHolding(post, DepotId);

        PressSharpenFixOnTheDossier(map);
        ATickOfTheShipsClock(post, 4 * SensorTaskGeometry.TrackPassSeconds);

        string desk = (string?)Get(post, "_lastSweepMessage") ?? "";
        Assert.Contains(Callsign, desk, StringComparison.Ordinal);
        Assert.Contains("telescope", desk, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>THE CARD STOPS SAYING THE THING HE JUST DID. With the scope ordered onto her and no slot to
    /// hold her, the dossier's line was "not on the telescope ledger — track her to sharpen the intel",
    /// before the press and after it, unchanged — the sentence the owner answered with "but really
    /// HOW??????".</summary>
    [Fact]
    public void THE_DOSSIER_SaysTheScopeIsOnHerOnceThePassIsOrdered()
    {
        (Pages.Map map, TrackingPost post) = AChaseInProgress();
        TheOneTelescopeIsAlreadyHolding(post, DepotId);

        PressSharpenFixOnTheDossier(map);

        object card = typeof(Pages.Map).GetMethod("DossierFor", Hidden)!.Invoke(map, [HunterId])!;
        Assert.True((bool)card.GetType().GetProperty("ScopeOrdered")!.GetValue(card)!,
            "the dossier does not know the telescope has been ordered onto her, so its line cannot change.");
        Assert.Null(card.GetType().GetProperty("TrackQuality")!.GetValue(card));
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private static readonly Vector2d DestinationAt = new(1.6e11, 0);

    /// <summary>A collector astern, a destination ahead, a telescope with nothing on it yet — the exact
    /// standing the owner's screenshots were taken in.</summary>
    private static (Pages.Map Map, TrackingPost Post) AChaseInProgress()
    {
        var map = new Pages.Map();

        // StateHasChanged's own early-out: told a render is already queued, the framework returns without
        // asking for a render handle this bench does not have. The same piece of theatre TheStallSaysSo and
        // MustStandUpBeforeWalking ride on — the deck verbs would throw on the way out otherwise.
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        // Two bodies so the destination is a REAL thing the old code could (and did) fall back to.
        var ephemeris = new CircularOrbitEphemeris(
        [
            new CelestialBody("sol", "Sol", null, 1.327e20, 6.96e8, 0, 0, 0),
            new CelestialBody("mercury", "Mercury", "sol", 2.2e13, 2.44e6, DestinationAt.X, 7.6e6, 0),
        ]);

        Set(map, "_ephemeris", ephemeris);
        Set(map, "_ship", new ShipState(ShipAt, Vector2d.Zero, 0));
        Set(map, "_destinationBodyId", "mercury");
        Set(map, "_interestTargetId", HunterId);

        var hunters = (List<HunterState>)Get(map, "_hunters")!;
        hunters.Add(EncounterRule.SpawnHunter(HunterId, Callsign, "mercury", HunterAt, Vector2d.Zero, 0));

        var post = new TrackingPost { ShipPosition = ShipAt, ShipVelocity = new Vector2d(0, 3e4), MaxTracks = 1 };
        post.Candidates =
        [
            new TrackingPost.TrackingCandidate(HunterId, Callsign, new ShipState(HunterAt, Vector2d.Zero, 0),
                IsThreat: true, CargoDetail: "hired muscle"),
            // The depot ahead — the thing that was holding the single telescope in the owner's screenshot.
            new TrackingPost.TrackingCandidate(DepotId, DepotCallsign, new ShipState(DestinationAt, Vector2d.Zero, 0)),
        ];
        Set(map, "_trackingPost", post);

        return (map, post);
    }

    /// <summary>"Tracked targets (1 / 1)": the one telescope is already holding something else, which is
    /// the standing the owner pressed 📡 sharpen fix in. Entered through <c>ApplyObservation</c> — the same
    /// door a sweep hit and a laser range go through — so the ledger is full the way play fills it.</summary>
    private static void TheOneTelescopeIsAlreadyHolding(TrackingPost post, string shipId)
    {
        Assert.True(
            post.ApplyObservation(new Observation(shipId, 0, DestinationAt, Vector2d.Zero)),
            "the bench could not fill the ledger — this test would then be measuring the empty case.");
    }

    /// <summary>One turn of the ship's clock through the component's REAL parameter tick — the thing that
    /// runs the schedule, completes passes, and does the custody housekeeping that used to eat the order.
    /// Called twice: the post has to have seen a previous SimTime before any time can have advanced.</summary>
    private static void ATickOfTheShipsClock(TrackingPost post, double seconds)
    {
        MethodInfo tick = typeof(TrackingPost).GetMethod("OnParametersSet", Hidden)
            ?? throw new MissingMethodException("TrackingPost has no OnParametersSet — this bench's tick has moved.");
        tick.Invoke(post, null);
        post.SimTime += seconds;
        tick.Invoke(post, null);
    }

    /// <summary>The Drop button on a track card — private, like every other handler this bench drives.</summary>
    private static void PressDropOnTheSensorsDesk(TrackingPost post, string shipId) =>
        typeof(TrackingPost).GetMethod("Drop", Hidden)!.Invoke(post, [shipId]);

    /// <summary>Press whatever the dossier's 📡 button is wired to — read off the SHIPPING razor, so
    /// rewiring the button to anything else fails here rather than passing on a method a test picked.</summary>
    private static void PressSharpenFixOnTheDossier(Pages.Map map)
    {
        string razor = MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));
        // The button's own LABEL, closing tag and all — not the bare words, which the card's status lines
        // and comments are free to repeat and which would otherwise walk this search onto a neighbour's
        // @onclick (they did, the day the card learned to say the scope was already ordered onto her).
        int at = razor.IndexOf("📡 sharpen fix</button>", StringComparison.Ordinal);
        Assert.True(at >= 0, "the dossier no longer carries a 📡 sharpen fix button — this guard needs re-reading.");

        // The @onclick on that button: the last one before the label.
        int click = razor.LastIndexOf("@onclick=\"() => ", at, StringComparison.Ordinal);
        Assert.True(click >= 0, "no @onclick found on the sharpen-fix button.");
        string call = razor[(click + "@onclick=\"() => ".Length)..razor.IndexOf('(', click + 20)];

        MethodInfo handler = typeof(Pages.Map).GetMethod(call.Trim(), Hidden)
            ?? throw new MissingMethodException($"Map has no `{call.Trim()}` for the dossier's sharpen-fix button.");
        handler.Invoke(map, [HunterId]);
    }

    /// <summary>Let the carousel take up the head of the queue, which is what a sim tick does — the readout
    /// reports the RUNNING job, and there is no running job until the schedule starts one.</summary>
    private static void StartTheQueuedPass(TrackingPost post)
    {
        object schedule = Get(post, "_schedule")!;
        schedule.GetType().GetMethod("Advance")!.Invoke(schedule,
            [60.0, (Func<SensorTask, double>)(_ => 3600)]);
    }

    private static double Degrees(double rad)
    {
        double deg = rad * 180.0 / Math.PI % 360;
        return deg < 0 ? deg + 360 : deg;
    }

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

    private static object? Get(object o, string field) =>
        o.GetType().GetField(field, Hidden)!.GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);
}

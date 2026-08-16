using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #825 · WHEN THE MACHINE STALLS, THE CONTROLS SAY SO.
///
/// <para>Owner, 2026-08-11, on B1 near the goods car with a background build eating the CPU:
/// <i>"why does the click to walk no longer work?"</i> — with <c>⚠ SIGNAL BREAKING UP — last update 16s ago</c>
/// across the top of the glass.</para>
///
/// <h3>THE FORK, ANSWERED BY DRIVING IT</h3>
///
/// <para>Two things could have been true, and the whole point of this file is that only one of them was.</para>
///
/// <para><b>(a) The comms fiction gates input — FALSE.</b> Nothing in the click path, the key handler, the
/// availability gate or the frame's own stepper consults <see cref="CommsLink.Phase"/>. A click placed in a
/// full <see cref="CommsLink.Phase.Blackout"/> plans and walks exactly the route it plans and walks in
/// <see cref="CommsLink.Phase.Nominal"/>, which is what <see cref="ACommsBlackoutDoesNotStopYourLegs"/>
/// pins. The banner was a coincidence of timing — an unrelated scripted episode standing over the scene of
/// a machine-level fault, wearing the only sentence on screen.</para>
///
/// <para><b>(b) A real stall dropped the walk — TRUE, and measured.</b> The click is NOT lost: it is taken,
/// snapped, planned, and the route survives. What is lost is the WALKING. <c>MoveAvatar</c> clamps a frame
/// to <see cref="FrameGap.SpentPerFrameSeconds"/>, so a frame spanning sixteen real seconds buys 0.9 deck
/// units of legs instead of the 144 the captain asked for — measured on this bench, on the base commit:</para>
/// <code>
/// healthy 0.1 s frame walked 0.893 du
/// 16 s gap frame  walked 0.898 du      ← the same frame's worth, for sixteen seconds of wall clock
/// held key over a 16 s gap walked 0.900 du
/// route still active after gap: True   ← nothing was dropped except the time
/// pulse after gap: (the stand-up line)  ← nothing was said about ANY of it
/// </code>
///
/// <para>The clamp is right and stays (see <see cref="FrameGap"/>: a sixteen-second step resolves through a
/// bulkhead with the air, the nerve, the tracker and the Old Ones all skipped). The SILENCE was the bug, and
/// the silence is what these guards close.</para>
///
/// <h3>WHY THESE ARE DRIVEN AND NOT READ</h3>
///
/// <para>Every fact below is asked of a REAL <see cref="Pages.Map"/> on a REAL generated Hive floor: the
/// bench is taken with the shipping verb, the click goes through <c>ClickToWalkAt</c>, the key through
/// <c>HandleDeckKey</c>, and the frames are spent in <c>MoveAvatar</c> — the same methods the browser calls.
/// The stall itself is driven through <c>MarkFrameServiced</c>, the frame loop's own one line into the
/// clock, so a guard cannot pass on a build where the clock has been quietly replaced beside it.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheStallSaysSoTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The floor these guards drive. One site is enough to prove a law about a clock, and it is the
    /// GENERATOR's floor rather than a hand-typed room — a guard handed a world it built itself cannot tell
    /// pass from fail.</summary>
    private const string Body = "luna";

    private static int TheFloor => UndergroundComplex.TopPressurisedFloor(Body)
        ?? throw new InvalidOperationException($"{Body} has no pressurised floor to walk about on.");

    // ── (a) A CLICK IN THE GAP IS HELD OUT LOUD, AND WALKED WHEN THE FRAMES COME BACK ─────────────────

    /// <summary>
    /// #825 · THE GUARD THE ISSUE ASKED FOR. Hold the frames past <see cref="FrameGap.StallSeconds"/>, click
    /// the floor, and both halves of the law must hold at once: the order is QUEUED (the route exists and is
    /// spent the moment frames return) and the hold is SAID (exactly one line, Core's own
    /// <see cref="FrameGap.HeldLine"/>).
    ///
    /// <para><b>Proven RED</b> on today's silent drop by deleting the one <c>AcknowledgeHeldControls()</c>
    /// call from the tail of <c>ClickToWalkAt</c> — which is precisely the shipped behaviour the owner met:</para>
    /// <code>
    /// Failed …TheStallSaysSoTests.AClickInTheGapIsQueuedAndTheHoldIsSaidOutLoud [8 ms]
    ///   Error Message:
    ///    Assert.Equal() Failure: Strings differ
    ///            ↓ (pos 0)
    /// Expected: "⏱ Order held — the machine is behind your"···
    /// Actual:   "You stand, and the chair goes back under "···
    ///            ↑ (pos 0)
    /// </code>
    /// <para>The route IS queued and WILL be walked — the assertion above it passes. What the captain gets
    /// is a motionless dot for sixteen seconds and the last thing anybody happened to say to him, and the
    /// only thing a player can conclude from a control that does nothing and says nothing is that the
    /// control is broken. That is the report this issue was filed on.</para>
    /// </summary>
    [Fact]
    public void AClickInTheGapIsQueuedAndTheHoldIsSaidOutLoud()
    {
        Pages.Map map = OnTheFloor();
        StandOnTheFloor(map);
        Serviced(map, 1.0 / 60.0);   // a healthy frame first, so the hold notice starts un-said

        DeckPlan.ConsoleSpot target = SomewhereToWalkTo(map);
        (double px, double py) = ThePixelFor(map, target);

        // The machine goes away for sixteen seconds — the owner's own number.
        const double TheOwnersGap = 16.0;
        Serviced(map, TheOwnersGap);
        Assert.True(FrameGap.IsStalling((double)Get(map, "_frameGapSeconds")!),
            "the bench failed to put the sim into a stall at all — everything below is asserting about a " +
            "healthy machine and could never fail.");

        Invoke(map, "ClickToWalkAt", px, py);

        // HALF ONE: the order landed. A click in the gap is not thrown away.
        var route = (AutoWalk?)Get(map, "_autoWalk");
        Assert.True(route is { Active: true },
            "the click planned no route at all while the sim was stalled — the order was dropped, not held.");

        // HALF TWO: and the captain was told, in Core's own words.
        Assert.Equal(FrameGap.HeldLine, ThePulse(map));

        // …and it IS walked, on the frames that come back. A "held" order that never resumed would satisfy
        // the sentence above and be a worse bug than the silence it replaced.
        for (int frame = 0; frame < 4000 && route!.Active; frame++)
        {
            Serviced(map, 1.0 / 60.0);
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
        }
        (double ax, double ay) = Where(map);
        Assert.True(target.DistanceFrom(ax, ay) <= DeckPlan.InteractRadius,
            $"the held order never resumed: the captain stopped at ({ax:0.00},{ay:0.00}), which is not " +
            $"within reach of the fixture clicked at ({target.X:0.00},{target.Y:0.00}).");
    }

    /// <summary>
    /// #825 · THE SAME LAW ON THE OTHER GRIP. A held movement key buys the same clamped 0.1 s of legs as a
    /// clicked route does, so a captain leaning on W through a stall watches the same motionless dot. One
    /// stall, one sentence, whichever hand is on the controls.
    ///
    /// <para>And exactly ONE sentence: the notice is latched for the stall, not raised per press, because a
    /// player hammering a control that feels dead would otherwise be shouted at eight times.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the <c>AcknowledgeHeldControls()</c> call from the movement-key
    /// arm of <c>HandleDeckKey</c>:</para>
    /// <code>
    /// Failed …TheStallSaysSoTests.AHeldKeyInTheGapIsAcknowledgedOnceAndOnlyOnce [6 ms]
    ///   Error Message:
    ///    Assert.Equal() Failure: Strings differ
    ///            ↓ (pos 0)
    /// Expected: "⏱ Order held — the machine is behind your"···
    /// Actual:   "You stand, and the chair goes back under "···
    ///            ↑ (pos 0)
    /// </code>
    /// </summary>
    [Fact]
    public void AHeldKeyInTheGapIsAcknowledgedOnceAndOnlyOnce()
    {
        Pages.Map map = OnTheFloor();
        StandOnTheFloor(map);
        Serviced(map, 1.0 / 60.0);

        Serviced(map, 16.0);
        Assert.True((bool)Invoke(map, "HandleDeckKey", "d")!, "the deck did not take the movement key.");
        Assert.Equal(FrameGap.HeldLine, ThePulse(map));

        // Said once. Hammer the key: the stall has already been announced and nothing new is owed.
        Invoke(map, "ShowPulseMessage", "— nothing —", PulseRank.Status);
        for (int press = 0; press < 8; press++)
        {
            Invoke(map, "HandleDeckKey", "d");
        }
        Assert.Equal("— nothing —", ThePulse(map));

        // …and the next stall IS announced afresh, because a frame that arrived on time cleared the latch.
        Serviced(map, 1.0 / 60.0);
        Serviced(map, 16.0);
        Invoke(map, "HandleDeckKey", "d");
        Assert.Equal(FrameGap.HeldLine, ThePulse(map));
    }

    // ── (b) THE FORK'S OTHER ARM: A COMMS EPISODE NEVER TOUCHED ANYBODY'S LEGS ────────────────────────

    /// <summary>
    /// #825 · THE ANSWER TO FORK (a), NAILED DOWN. A comms fault is a fault in a radio. It does not stop you
    /// walking, and in this codebase it never did: the click plans and the frame walks identically in
    /// <see cref="CommsLink.Phase.Nominal"/>, <see cref="CommsLink.Phase.Degraded"/> and
    /// <see cref="CommsLink.Phase.Blackout"/>.
    ///
    /// <para>This one could not have gone red on the base commit, because it was already true — it is the
    /// evidence for the fork's answer written down so it STAYS true, which matters more than usual here: the
    /// fictional banner was the only sentence on screen the afternoon this was filed, and the obvious "fix"
    /// somebody reaches for next time is to make the fiction own the controls it appeared to be holding.</para>
    ///
    /// <para><b>Proven able to fail</b> by writing exactly that fix — the click gate (then
    /// <c>AutoWalkAvailable</c>, since #875 <c>TheCaptainsLegsAreTheirOwn</c>) given
    /// <c>&amp;&amp; _surface is { CommsPhase: CommsLink.Phase.Nominal }</c>:</para>
    /// <code>
    /// Failed …TheStallSaysSoTests.ACommsBlackoutDoesNotStopYourLegs [195 ms]
    ///   Error Message:
    ///    a walk order in CommsLink.Phase.Degraded planned nothing — a comms fault has taken the captain's
    ///    legs, which is a fiction about a radio deciding what a body may do.
    /// </code>
    /// </summary>
    [Fact]
    public void ACommsBlackoutDoesNotStopYourLegs()
    {
        foreach (CommsLink.Phase phase in new[]
                 { CommsLink.Phase.Nominal, CommsLink.Phase.Degraded, CommsLink.Phase.Blackout })
        {
            Pages.Map map = OnTheFloor();
            StandOnTheFloor(map);
            Serviced(map, 1.0 / 60.0);

            object ex = Get(map, "_surface")!;
            ex.GetType().GetProperty("CommsPhase")!.SetValue(ex, phase);

            DeckPlan.ConsoleSpot target = SomewhereToWalkTo(map);
            (double px, double py) = ThePixelFor(map, target);
            Invoke(map, "ClickToWalkAt", px, py);

            var route = (AutoWalk?)Get(map, "_autoWalk");
            Assert.True(route is { Active: true },
                $"a walk order in CommsLink.Phase.{phase} planned nothing — a comms fault has taken the " +
                "captain's legs, which is a fiction about a radio deciding what a body may do.");

            for (int frame = 0; frame < 4000 && route!.Active; frame++)
            {
                Serviced(map, 1.0 / 60.0);
                Invoke(map, "MoveAvatar", 1.0 / 60.0);
            }
            (double ax, double ay) = Where(map);
            Assert.True(target.DistanceFrom(ax, ay) <= DeckPlan.InteractRadius,
                $"in CommsLink.Phase.{phase} the walk stopped at ({ax:0.00},{ay:0.00}) short of the " +
                $"fixture at ({target.X:0.00},{target.Y:0.00}).");

            // …and the machine keeping up means NO stall banner, whatever the ship's downlink is doing.
            Assert.True(string.IsNullOrEmpty(TheStallBanner(map)),
                $"CommsLink.Phase.{phase} raised the MACHINE's stall banner. Two different facts have been " +
                "wired to one clock, which is the conflation this issue exists to keep apart.");
        }
    }

    // ── (c) ONE CLOCK, ONE THRESHOLD, ONE CONSTANT ───────────────────────────────────────────────────

    /// <summary>
    /// #825 · THE BANNER AND THE VERB CANNOT DISAGREE ABOUT WHETHER THE WORLD IS LIVE.
    ///
    /// <para>Driven at the threshold itself, from both sides: a hair under <see cref="FrameGap.StallSeconds"/>
    /// the HUD paints nothing AND a click says nothing extra; a hair over, the HUD paints the banner AND the
    /// same click is acknowledged. One number moves both, which is what "the same staleness clock" means
    /// when it is asserted rather than described.</para>
    ///
    /// <para><b>Proven RED</b> by giving the input path a threshold of its own — <c>ControlsAreHeld</c>
    /// rewritten as <c>SimStalenessSeconds > 2.0</c> (a perfectly reasonable-looking number, and the exact
    /// shape of the bug this law forbids):</para>
    /// <code>
    /// Failed …TheStallSaysSoTests.TheBannerAndTheInputPathReadOneClock [33 ms]
    ///   Error Message:
    ///    Assert.Equal() Failure: Strings differ
    ///            ↓ (pos 0)
    /// Expected: "⏱ Order held — the machine is behind your"···
    /// Actual:   "You stand, and the chair goes back under "···
    ///            ↑ (pos 0)
    /// </code>
    /// <para>The HUD had already painted <c>⏱ FRAMES DROPPED — 0.5s behind · controls held</c> on that very
    /// frame. The picture said the world was stalled and the verb waved the click through without a word:
    /// two clocks, one question.</para>
    /// </summary>
    [Fact]
    public void TheBannerAndTheInputPathReadOneClock()
    {
        // A hair UNDER: nothing is behind, so nobody says anything.
        Pages.Map calm = OnTheFloor();
        StandOnTheFloor(calm);
        Serviced(calm, 1.0 / 60.0);
        Serviced(calm, FrameGap.StallSeconds - 0.01);
        Invoke(calm, "ShowPulseMessage", "— nothing —", PulseRank.Status);
        (double cpx, double cpy) = ThePixelFor(calm, SomewhereToWalkTo(calm));
        Invoke(calm, "ClickToWalkAt", cpx, cpy);

        Assert.True(string.IsNullOrEmpty(TheStallBanner(calm)),
            $"a gap of {FrameGap.StallSeconds - 0.01:0.000}s — under the threshold — raised a stall banner.");
        Assert.Equal("— nothing —", ThePulse(calm));

        // A hair OVER: BOTH speak, off the one number.
        Pages.Map stalled = OnTheFloor();
        StandOnTheFloor(stalled);
        Serviced(stalled, 1.0 / 60.0);
        Serviced(stalled, FrameGap.StallSeconds + 0.01);
        (double spx, double spy) = ThePixelFor(stalled, SomewhereToWalkTo(stalled));
        Invoke(stalled, "ClickToWalkAt", spx, spy);

        string banner = TheStallBanner(stalled);
        Assert.False(string.IsNullOrEmpty(banner),
            $"a gap of {FrameGap.StallSeconds + 0.01:0.000}s — over the threshold — painted no banner.");
        Assert.Equal(FrameGap.HeldLine, ThePulse(stalled));

        // And the two sentences are two sentences: the machine's banner is nobody's comms line.
        Assert.DoesNotContain("SIGNAL", banner, StringComparison.Ordinal);
        Assert.NotEqual(CommsLink.StaleBanner(CommsLink.Phase.Degraded, FrameGap.StallSeconds), banner);
    }

    /// <summary>
    /// #825 · …AND THE THRESHOLD IS DERIVED FROM THE CLAMP THAT CAUSED IT, not typed twice.
    ///
    /// <para>The banner reports a gap; the frame EATS that gap down to one clamped step. If those were two
    /// literals that merely agreed today, the HUD would one day report a stall the walk was quietly
    /// surviving, or the reverse. So the spend is measured off a real frame and checked against the very
    /// constant <see cref="FrameGap.StallSeconds"/> is derived from — the "one source" idiom, asked of a
    /// walking body rather than of the source text.</para>
    ///
    /// <para><b>Proven RED</b> by putting the literal back in <c>MoveAvatar</c> as
    /// <c>Math.Min(dtRealSeconds, 0.25)</c> — a clamp that still looks perfectly sane and no longer has
    /// anything to do with the number the banner is thresholded on:</para>
    /// <code>
    /// Failed …TheStallSaysSoTests.OneFrameSpendsExactlyTheClampTheThresholdIsBuiltOn [5 ms]
    ///   Error Message:
    ///    a frame spanning 16.0s walked 2.250 du, but FrameGap.SpentPerFrameSeconds says one frame buys
    ///    0.900 du. The banner's threshold is derived from a clamp the frame is not using — the HUD and
    ///    the legs are two sources for one fact.
    /// </code>
    /// </summary>
    [Fact]
    public void OneFrameSpendsExactlyTheClampTheThresholdIsBuiltOn()
    {
        Pages.Map map = OnTheFloor();
        StandOnTheFloor(map);

        // The walk speed the frame will spend, asked of the component rather than assumed.
        double speed = (double)Get(map, "CurrentWalkSpeed")!;
        double owed = speed * FrameGap.SpentPerFrameSeconds;

        var held = (HashSet<string>)Get(map, "_deckKeys")!;
        held.Clear();
        held.Add(TheDirectionWithRoomToWalk(map));

        (double x0, double y0) = Where(map);
        Invoke(map, "MoveAvatar", 16.0);
        (double x1, double y1) = Where(map);
        double walked = Math.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));

        Assert.True(Math.Abs(walked - owed) < 1e-6,
            $"a frame spanning 16.0s walked {walked:0.000} du, but FrameGap.SpentPerFrameSeconds says one " +
            $"frame buys {owed:0.000} du. The banner's threshold is derived from a clamp the frame is not " +
            "using — the HUD and the legs are two sources for one fact.");

        // …which is the whole of what the banner is reporting: 15.9 s of legs the machine owed and did not pay.
        Assert.True(Math.Abs(FrameGap.SecondsDropped(16.0) - (16.0 - FrameGap.SpentPerFrameSeconds)) < 1e-9);
        Assert.True(FrameGap.StallSeconds > FrameGap.SpentPerFrameSeconds,
            "the stall threshold has fallen to or below the clamp, so every ordinary frame is now a stall.");
    }

    // ── PLUMBING ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A live component on a real Hive floor with nothing running but the deck — the same bench
    /// <c>MustStandUpBeforeWalkingTests</c> drives, and the one piece of theatre is the same: the component
    /// is told it already has a render queued, which is the framework's own early-out, so the verbs that end
    /// in <c>StateHasChanged</c> are silent no-ops instead of throwing.</summary>
    private static Pages.Map OnTheFloor()
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on " +
                "has moved, and the deck verbs will throw instead of running.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, TheFloor);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_fpMode", false);
        // #875 · #729's ?autowalk=1 was set here once, because click-to-walk did not exist off it. The owner
        // ruled the click always-on; the flag is a retired no-op alias, and this bench boots the shipping
        // control rather than a switched-on one.

        Invoke(map, "RebuildSurfaceDeck");
        return map;
    }

    /// <summary>Put the captain somewhere a walk can start from. The floor's own step-off square, reached the
    /// way the game reaches it: sit on the bench the generator carved, then stand up. Picking a bare
    /// coordinate would be a hand-typed world, and the bench END itself is solid ground the planner rightly
    /// refuses to route out of.</summary>
    private static void StandOnTheFloor(Pages.Map map)
    {
        DeckPlan.ConsoleSpot bench = ThePlan(map).Consoles
            .FirstOrDefault(c => c.Kind == DeckPlan.ConsoleKind.HiveBench);
        Assert.True(bench.Kind == DeckPlan.ConsoleKind.HiveBench,
            $"{Body} B{-TheFloor} carves no bench — this bench has nowhere to start a walk from.");

        Set(map, "_avatarX", (double)bench.X);
        Set(map, "_avatarY", (double)bench.Y);
        Assert.True((bool)Invoke(map, "TryTakeBench")!, "the press at the bench was not taken.");
        Invoke(map, "StandUpBeforeWalking");
        Assert.True(Get(map, "_table") is null, "the captain is still sitting down.");
    }

    /// <summary>Somewhere on this floor worth walking to: the nearest fixture the Hive INVITES you to walk
    /// to — a promise the game already makes, rather than a coordinate a test picked out of a room.</summary>
    private static DeckPlan.ConsoleSpot SomewhereToWalkTo(Pages.Map map)
    {
        (double sx, double sy) = Where(map);
        return ThePlan(map).Consoles
            .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveLift
                or DeckPlan.ConsoleKind.HiveAmenity or DeckPlan.ConsoleKind.HiveRefuge)
            .OrderBy(c => ((c.X - sx) * (c.X - sx)) + ((c.Y - sy) * (c.Y - sy)))
            .First();
    }

    /// <summary>The pixel a captain would have pressed to point at a fixture, through the projection the
    /// renderer is drawing with right now — never arithmetic written down a second time (this repo's first
    /// named bug class is exactly that).</summary>
    private static (double Px, double Py) ThePixelFor(Pages.Map map, DeckPlan.ConsoleSpot at)
    {
        (double sx, double sy) = Where(map);
        DeckView.Placement glass = DeckView.PlacementFor(
            ThePlan(map), (int)Get(map, "_viewportWidth")!, (int)Get(map, "_viewportHeight")!,
            sx, sy, (double)Get(map, "_deckPanX")!, (double)Get(map, "_deckPanY")!);
        return (glass.Ox + (at.X * glass.Scale), glass.Oy - (at.Y * glass.Scale));
    }

    /// <summary>Which way there is floor to walk in from here — tried through the shipping stepper, because
    /// a room is a room and any one heading may be a wall.</summary>
    private static string TheDirectionWithRoomToWalk(Pages.Map map)
    {
        var held = (HashSet<string>)Get(map, "_deckKeys")!;
        foreach (string key in new[] { "w", "a", "s", "d" })
        {
            (double x, double y) = Where(map);
            held.Clear();
            held.Add(key);
            Invoke(map, "MoveAvatar", 1.0 / 60.0);
            (double nx, double ny) = Where(map);
            Set(map, "_avatarX", x);
            Set(map, "_avatarY", y);
            if (Math.Abs(nx - x) > 1e-9 || Math.Abs(ny - y) > 1e-9)
            {
                held.Clear();
                return key;
            }
        }
        held.Clear();
        throw new InvalidOperationException("the captain is walled in on all four sides.");
    }

    /// <summary>Tell the component a frame was serviced and how long it spanned — the frame loop's own one
    /// line into the stall clock (<c>OnTick</c> calls exactly this), so the guard drives the shipping path
    /// rather than poking the fields behind it.</summary>
    private static void Serviced(Pages.Map map, double gapSeconds) =>
        Invoke(map, "MarkFrameServiced", gapSeconds);

    /// <summary>What the deck would paint across the top about the MACHINE — the component's own one
    /// answer, which is what the <c>DeckView.State</c> at the draw call site is handed.</summary>
    private static string TheStallBanner(Pages.Map map) => (string)Invoke(map, "TheStallBanner")!;

    private static (double X, double Y) Where(Pages.Map map) =>
        ((double)Get(map, "_avatarX")!, (double)Get(map, "_avatarY")!);

    private static string? ThePulse(Pages.Map map) => ((PulseSlot)Get(map, "_pulse")!).Message;

    private static DeckPlan ThePlan(Pages.Map map) => (DeckPlan)Get(map, "_deckPlan")!;

    private static object? Get(object o, string member) =>
        o.GetType().GetField(member, Hidden) is { } f
            ? f.GetValue(o)
            : (o.GetType().GetProperty(member, Hidden)
               ?? throw new InvalidOperationException($"the component has no `{member}`.")).GetValue(o);

    private static void Set(object o, string field, object? value) =>
        o.GetType().GetField(field, Hidden)!.SetValue(o, value);

    private static object? Invoke(Pages.Map map, string method, params object?[] args)
    {
        MethodInfo? call = typeof(Pages.Map).GetMethod(method, Hidden);
        Assert.True(call is not null, $"the component has no `{method}` — this guard is reading a dead name.");
        return call!.Invoke(map, args);
    }
}

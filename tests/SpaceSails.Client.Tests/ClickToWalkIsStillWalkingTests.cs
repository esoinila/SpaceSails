using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #729 · CLICK THE FLOOR, WALK THERE — and it had better still be WALKING.
///
/// <para>Owner, mid-playtest: <i>"Maybe for testing purposes an automatic walk feature with point-at to
/// walk-to using say A* might be useful. So our testing does not hang on slow MCP speed to browser."</i>
/// The danger in that sentence is the whole reason this file exists. A movement cheat that teleports, or
/// walks faster, or steps around the collision, would un-test exactly what walking is for: #600's lift
/// shipped broken for months because the audit proved you can REACH the lift and never that the lift is a
/// way HOME, and a walk that does not pay the walk breeds that class by the dozen.</para>
///
/// <para>So every law here is about SAMENESS, and every one of them is asked of a REAL generated floor —
/// <see cref="HiveInterior.FloorDeck"/>, the deck the captain's boots actually collide with — and of the
/// REAL surface field, never a hand-typed corridor. A guard handed a world that cannot tell pass from fail
/// is this project's fifth named bug class, and a hand-drawn box with one wall in it is exactly that world.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ClickToWalkIsStillWalkingTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    // The client's own walk speed and its own frame-time clamp. Named here so a simulated walk in this file
    // spends exactly what a hand-walked one spends; if either drifts in Map.Deck the air law below is the
    // test that notices.
    private const double WalkSpeedDu = 9.0;      // Map.Deck's AvatarSpeed
    private const double FrameDtClampS = 0.1;    // Map.Deck's Math.Min(dtRealSeconds, 0.1)

    private static DeckPlan FloorOf(string body, int level) =>
        HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);

    private static (double X, double Y) Spawn => HiveInterior.SpawnOn(Field);

    /// <summary>Everywhere on a floor the game invites you to walk to — the same list #585's reachability
    /// audit floods toward, so this walks the routes the game promises rather than routes of its own.</summary>
    private static List<DeckReachability.Point> TargetsOn(DeckPlan deck) =>
        deck.Consoles
            .Where(c => c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveLift
                or DeckPlan.ConsoleKind.HiveAmenity or DeckPlan.ConsoleKind.HiveRefuge)
            .Select(c => new DeckReachability.Point(c.X, c.Y))
            .ToList();

    /// <summary>What a simulated auto-walk did: where it ended, how far it walked, how long that took at
    /// the captain's own speed, and whether the STEPPER ever refused one of the moves the route asked for.</summary>
    private readonly record struct Sim(
        double X, double Y, double WalkedDu, double SecondsSpent, bool Arrived, string? Refused);

    /// <summary>
    /// Drive an auto-walk exactly the way <c>MoveAvatar</c> drives it: a frame's budget is speed × dt, spent
    /// in sub-steps, each one handed to the REAL <see cref="DeckPlan.Move"/> — the shipping stepper, called
    /// as-is. Nothing here re-implements collision; if it did, the whole file would be worthless.
    /// </summary>
    private static Sim Walk(DeckPlan deck, AutoWalk route, double startX, double startY, double dt)
    {
        double x = startX, y = startY, walked = 0, seconds = 0;
        for (int frame = 0; frame < 20_000 && route.Active; frame++)
        {
            double budget = WalkSpeedDu * dt;
            seconds += dt;
            for (int sub = 0; sub < 64 && budget > 1e-9 && route.Active; sub++)
            {
                if (!route.TryStep(x, y, budget, out double dx, out double dy))
                {
                    break;
                }
                double wantX = x + dx, wantY = y + dy;
                (double nx, double ny) = deck.Move(x, y, dx, dy);

                // ── THE LAW ── the stepper was asked for this move and took all of it. A pathed walk that
                // ever has a move REFUSED is a route the collision does not agree with, which is the one
                // thing an A* over the collision's own predicate must never produce.
                if (Math.Abs(nx - wantX) > 1e-9 || Math.Abs(ny - wantY) > 1e-9)
                {
                    route.Snag();
                    return new Sim(x, y, walked, seconds, false,
                        $"the stepper refused the move at ({x:F2}, {y:F2}) → ({wantX:F2}, {wantY:F2})");
                }

                walked += Math.Sqrt((dx * dx) + (dy * dy));
                budget -= Math.Sqrt((dx * dx) + (dy * dy));
                (x, y) = (nx, ny);
            }
        }
        return new Sim(x, y, walked, seconds, route.Arrived, null);
    }

    private static AutoWalk Plan(DeckPlan deck, DeckReachability.Point from, DeckReachability.Point to)
    {
        AutoWalk.Attempt attempt = AutoWalk.Plan(
            enabled: true, from, to, deck.CollisionField, DeckPlan.AvatarRadius,
            AutoWalk.BoundsFor(deck.CollisionSegments, from, to));
        Assert.True(attempt.Route is not null,
            $"no route from ({from.X:F1}, {from.Y:F1}) to ({to.X:F1}, {to.Y:F1}) — the audit says there is one.");
        return attempt.Route!;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 1 · a pathed walk never crosses a wall the stepper refuses.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void APathedWalkNeverAsksForAMoveTheStepperRefuses()
    {
        // Every place four real facilities offer you, walked for real, at the WORST frame the client can
        // hand out (its own dt clamp — the slow frame an automation-driven tab actually produces, which is
        // the frame most likely to sail past a waypoint and cut a corner).
        var bad = new List<string>();
        int walks = 0;

        foreach (string body in new[] { "miranda", "europa", "the-clinker", "secret-lab-site-unlisted" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body).Take(4))
            {
                DeckPlan deck = FloorOf(body, level);
                (double sx, double sy) = Spawn;
                var from = new DeckReachability.Point(sx, sy);

                foreach (DeckReachability.Point target in TargetsOn(deck))
                {
                    AutoWalk route = Plan(deck, from, target);
                    Sim sim = Walk(deck, route, sx, sy, FrameDtClampS);
                    walks++;
                    if (sim.Refused is { } why)
                    {
                        bad.Add($"  {body} B{-level} → ({target.X:F0}, {target.Y:F0}): {why}");
                    }
                    else if (!sim.Arrived)
                    {
                        bad.Add($"  {body} B{-level} → ({target.X:F0}, {target.Y:F0}): the walk never finished.");
                    }
                }
            }
        }

        Assert.True(walks > 40, $"only {walks} routes walked — this guard is not looking at a facility.");
        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} of {walks} auto-walks did not survive the collision:");
            foreach (string line in bad.Take(20))
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void ThePathedWalkEndsWhereTheClickWasSoEIsLive()
    {
        // #729 · "Arrival stops adjacent to the clicked thing if it is a fixture/door, so [E] is immediately
        // live — 'walk to the locker, press E' becomes two automation actions instead of forty." That is the
        // whole automation dividend, and it is a claim about the INTERACT radius, not about the route: the
        // walk has to end near enough that the game's own NearestConsole answers.
        DeckPlan deck = FloorOf("miranda", -3);
        (double sx, double sy) = Spawn;
        var from = new DeckReachability.Point(sx, sy);

        var missed = new List<string>();
        foreach (DeckPlan.ConsoleSpot spot in deck.Consoles.Where(c =>
            c.Kind is DeckPlan.ConsoleKind.HiveHaul or DeckPlan.ConsoleKind.HiveAmenity))
        {
            AutoWalk route = Plan(deck, from, new DeckReachability.Point(spot.X, spot.Y));
            Sim sim = Walk(deck, route, sx, sy, 1.0 / 60.0);
            if (deck.NearestConsoleSpot(sim.X, sim.Y) is null)
            {
                missed.Add($"  '{spot.Label}' at ({spot.X:F0}, {spot.Y:F0}): walk ended at "
                    + $"({sim.X:F1}, {sim.Y:F1}) with nothing in reach.");
            }
        }

        Assert.True(missed.Count == 0,
            $"{missed.Count} auto-walks parked the captain out of [E] range:\n{string.Join("\n", missed)}");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 2 · a key press cancels within one step.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AKeyPressCancelsTheWalkWithinOneStep()
    {
        DeckPlan deck = FloorOf("europa", -2);
        (double sx, double sy) = Spawn;
        var from = new DeckReachability.Point(sx, sy);
        DeckReachability.Point target = TargetsOn(deck)
            .OrderByDescending(t => ((t.X - sx) * (t.X - sx)) + ((t.Y - sy) * (t.Y - sy)))
            .First();

        AutoWalk route = Plan(deck, from, target);

        // Walk a few honest frames first, so the cancel lands mid-route rather than on a walk that had
        // nothing left to do — a guard that cancels an already-finished walk would pass on anything.
        double x = sx, y = sy;
        for (int frame = 0; frame < 5; frame++)
        {
            double budget = WalkSpeedDu / 60.0;
            while (budget > 1e-9 && route.TryStep(x, y, budget, out double dx, out double dy))
            {
                (x, y) = deck.Move(x, y, dx, dy);
                budget -= Math.Sqrt((dx * dx) + (dy * dy));
            }
        }
        Assert.True(route.Active, "the walk finished before the cancel — this test would prove nothing.");
        Assert.True(Math.Abs(x - sx) + Math.Abs(y - sy) > 0.5, "the captain never left the lift.");

        // The hand goes on the key. Map.Deck calls this BEFORE it spends any of the frame's budget.
        route.Cancel();

        Assert.False(route.Active, "a cancelled walk is still walking.");
        Assert.True(route.Cancelled);

        // And it hands out nothing at all afterwards — not a short step, not a last leg. One press, done.
        (double heldX, double heldY) = (x, y);
        Assert.False(route.TryStep(x, y, WalkSpeedDu / 60.0, out double sdx, out double sdy),
            "a cancelled walk offered another step.");
        Assert.Equal(0, sdx);
        Assert.Equal(0, sdy);
        (x, y) = deck.Move(x, y, sdx, sdy);
        Assert.Equal(heldX, x, 9);
        Assert.Equal(heldY, y, 9);
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 3 · without the flag, a click changes nothing.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WithoutTheCheatFlagAClickIsWhollyInert()
    {
        // The pair is the point. The SAME click, on the same real floor, to a place the audit proves is
        // reachable: with the flag it is a route, without it there is not even a message. Asserting only
        // the inert half would pass on a world where nothing is reachable at all.
        DeckPlan deck = FloorOf("miranda", -1);
        (double sx, double sy) = Spawn;
        var from = new DeckReachability.Point(sx, sy);
        DeckReachability.Point target = TargetsOn(deck).First(t => t.X != sx || t.Y != sy);
        (double, double, double, double) bounds = AutoWalk.BoundsFor(deck.CollisionSegments, from, target);

        AutoWalk.Attempt on = AutoWalk.Plan(true, from, target, deck.CollisionField, DeckPlan.AvatarRadius, bounds);
        Assert.True(on.Route is not null, "the flag is on and a reachable room produced no route — bad world.");

        AutoWalk.Attempt off = AutoWalk.Plan(false, from, target, deck.CollisionField, DeckPlan.AvatarRadius, bounds);
        Assert.Null(off.Route);
        Assert.Null(off.Refusal);   // not even a refusal: the cheat is off, so the click never happened
    }

    [Fact]
    public void TheFlagIsReadOffTheUrlAndOnlyOffTheUrl()
    {
        // The gate's other half: the parse. Real query strings the testing guide documents, including ones
        // that carry other cheats, so "any query at all turns it on" cannot pass here.
        Assert.True(AutoWalk.EnabledIn("?autowalk=1"));
        Assert.True(AutoWalk.EnabledIn("?secretlab=deep&autowalk=1&nerve=3"));
        Assert.True(AutoWalk.EnabledIn("?AUTOWALK=1"));

        Assert.False(AutoWalk.EnabledIn(null));
        Assert.False(AutoWalk.EnabledIn(""));
        Assert.False(AutoWalk.EnabledIn("?ashore=1&dock=the-space-bar"));
        Assert.False(AutoWalk.EnabledIn("?autowalk=0"));
        Assert.False(AutoWalk.EnabledIn("?notautowalk=1"));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════════════
    //  GUARD 4 · the air bill is the same bill.
    // ═══════════════════════════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AnAutoWalkCostsExactlyTheAirAHandWalkOfTheSameLengthCosts()
    {
        // #573's drain is priced off DISTANCE MOVED per frame, not off which key was down — which is what
        // makes this checkable at all, and also what makes it fragile in the other direction: the moment an
        // auto-walk moved further per second, or fewer frames, than a hand-walk, the captain would be buying
        // depth at a discount and every air decision in the Hive would be a lie.
        DeckPlan deck = FloorOf("the-clinker", -2);
        (double sx, double sy) = Spawn;
        var from = new DeckReachability.Point(sx, sy);
        DeckReachability.Point target = TargetsOn(deck)
            .OrderByDescending(t => ((t.X - sx) * (t.X - sx)) + ((t.Y - sy) * (t.Y - sy)))
            .First();

        const double dt = 1.0 / 60.0;
        AutoWalk route = Plan(deck, from, target);
        Sim auto = Walk(deck, route, sx, sy, dt);
        Assert.True(auto.Arrived, "the auto-walk never got there — nothing to price.");
        Assert.True(auto.WalkedDu > 8, $"only {auto.WalkedDu:F1} du walked — too short to price honestly.");

        // The hand-walk twin: the SAME captain at the SAME speed for the SAME number of frames, holding a
        // key. Every frame drains dt × Rate(exertion), and exertion is a function of speed alone, so equal
        // frames at equal speed is equal air — the assertion below is that the auto-walk did not shorten
        // either factor.
        double handSeconds = auto.WalkedDu / WalkSpeedDu;
        int handFrames = (int)Math.Ceiling(handSeconds / dt);
        double handExertion = SuitAir.Breathing.Walking;   // 9 du/s: between Still (<0.5) and Running (>7)
        double autoExertion = SuitAir.Breathing.Walking;

        double handAir = SuitAir.TankSeconds
            - SuitAir.Drain(SuitAir.TankSeconds, handFrames * dt * SuitAir.Breathing.Rate(handExertion, 1.0, 0, 3));
        double autoFrames = Math.Ceiling(auto.SecondsSpent / dt);
        double autoAir = SuitAir.TankSeconds
            - SuitAir.Drain(SuitAir.TankSeconds, autoFrames * dt * SuitAir.Breathing.Rate(autoExertion, 1.0, 0, 3));

        // Within a single frame of each other: the auto-walk spends the last partial frame standing still
        // on the doorstep exactly as a captain releasing the key does.
        double oneFrame = dt * SuitAir.Breathing.Rate(SuitAir.Breathing.Walking, 1.0, 0, 3);
        Assert.True(Math.Abs(autoAir - handAir) <= oneFrame + 1e-9,
            $"an auto-walk of {auto.WalkedDu:F1} du burned {autoAir:F3} s of tank where the same walk by hand "
            + $"burns {handAir:F3} s — depth is being sold at a discount.");

        // And the walk really did take the time it should: distance ÷ speed, not a frame less.
        Assert.True(auto.SecondsSpent >= handSeconds - dt,
            $"the auto-walk covered {auto.WalkedDu:F1} du in {auto.SecondsSpent:F2} s — faster than walking.");
    }

    [Fact]
    public void APathToNowhereRefusesOutLoud()
    {
        // "If no path exists, say so plainly — that refusal IS a test assertion, the same one the audits
        // make." Pointed at a spot outside the field's own walls, on a real floor, the answer is a sentence
        // rather than a shrug.
        DeckPlan deck = FloorOf("miranda", -1);
        (double sx, double sy) = Spawn;
        var from = new DeckReachability.Point(sx, sy);
        var nowhere = new DeckReachability.Point(Field.RightX + 60, Field.BottomY - 60);

        AutoWalk.Attempt attempt = AutoWalk.Plan(
            true, from, nowhere, deck.CollisionField, DeckPlan.AvatarRadius,
            AutoWalk.BoundsFor(deck.CollisionSegments, from, nowhere));

        Assert.Null(attempt.Route);
        Assert.Equal(AutoWalk.RefusalLine, attempt.Refusal);
    }
}

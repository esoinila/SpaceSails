// Lab 41 — Can you get there from here?
//
// Teaching voice: the owner boarded a derelict and could not walk to the back of it.
//
//   "THERE IS NO WAY TO GO TO THE BACK OF THE SHIP … we need some kind of CI test to spot similar
//    problems. Maybe do a lab with A-star algorithm and rig it up to our CI tests."
//
// He was right twice over. Two mutiny barricades spanned the full width of the wreck's spine corridor and
// cut her in half — the cargo manifest, four compartments and the whole aft end were unreachable — and
// every build was green the entire time. That is the lesson this lab exists to make unmissable:
//
//   A WALL YOU CANNOT PASS HAS NO TEST THAT FAILS.
//
// Type checks do not walk rooms. Unit tests of the pieces do not walk rooms. The only thing that catches a
// room sealed by accident is something that actually tries to walk from A to B — which is A*, and which is
// now wired into CI as WreckLayoutTests.
//
// This lab is the study half:
//   A — WHAT A* IS, on the smallest honest example: a corridor with a barricade, walked with and without a
//       gap, printing the route it found. The algorithm in one readable page.
//   B — WHY THE GRID STEP MATTERS: sweep it and watch a real doorway vanish. Sample too coarsely and you
//       "prove" a ship is sealed that is not; too finely and an audit costs more than it is worth.
//   C — THE AUDIT ITSELF, over every wreck cause: can the captain reach every station, every compartment,
//       the bow and the stern? This is the exact table CI asserts on.
//   D — THE BUG, REPRODUCED: put the old full-height barricades back and watch the audit go red, because a
//       guard that only ever passes guards nothing.
//
// Every number is the SAME Core code the game walks with: DeckReachability over WreckLayout's geometry and
// SurfaceCollision's own blocked-point test, so the audit and the captain agree by construction.
//
// IRONCLAD RULE: every number in labs/41-can-you-get-there/README.md came from running this probe.

using System.Globalization;
using SpaceSails.Core;

const double AvatarRadius = 0.7;   // DeckPlan.AvatarRadius — the captain's own half-width

var ci = CultureInfo.InvariantCulture;
var spawn = new DeckReachability.Point(WreckLayout.SpawnX, WreckLayout.SpawnY);

Console.WriteLine("LAB 41 — CAN YOU GET THERE FROM HERE?");
Console.WriteLine("A* over a deck's own collision geometry, so a room sealed by accident fails a test");
Console.WriteLine("instead of failing a player.");
Console.WriteLine();

// ── A · the algorithm, on the smallest honest example ─────────────────────────────────────────────────
//
// A corridor 6 units tall and 20 long, with a barricade across the middle. Once solid, once with a gap.
// This is the whole idea: the walk expands the cheapest frontier square first (cost so far + straight-line
// guess to the goal), and either arrives or exhausts the reachable set.
Console.WriteLine("A — THE ALGORITHM, ON A CORRIDOR WITH A BARRICADE");

List<SurfaceCollision.Segment> Corridor(bool sealedOff)
{
    var w = new List<SurfaceCollision.Segment>
    {
        new(-10, -3, 10, -3),   // corridor walls
        new(-10, 3, 10, 3),
        new(-10, -3, -10, 3),
        new(10, -3, 10, 3),
    };
    // The barricade: full height (sealed) or leaving a 2 du gap at the top.
    w.Add(sealedOff ? new(0, -3, 0, 3) : new(0, -3, 0, 1));
    return w;
}

var corridorBounds = (-12.0, -5.0, 12.0, 5.0);
var from = new DeckReachability.Point(-8, 0);
var to = new DeckReachability.Point(8, 0);

foreach (bool sealedOff in new[] { true, false })
{
    DeckReachability.Walk walk = DeckReachability.Path(
        from, to, Corridor(sealedOff), AvatarRadius, corridorBounds);
    Console.WriteLine(
        $"  barricade {(sealedOff ? "FULL HEIGHT" : "with a 2 du gap"),-16} → " +
        (walk.Reached ? $"reached in {walk.Steps} steps" : "NO ROUTE — the corridor is sealed"));
}
Console.WriteLine();
Console.WriteLine("  That is the entire failure mode. The same wall, two units shorter, and the ship works.");
Console.WriteLine();

// ── B · why the grid step matters ─────────────────────────────────────────────────────────────────────
//
// The walk samples the deck on a grid. Too coarse and it steps straight over a real doorway and reports a
// ship sealed that is not; too fine and an audit that should be instant is not. Sweep it against the
// wreck's own 6 du doorways.
Console.WriteLine("B — THE GRID STEP (can the walk still find a real doorway?)");
Console.WriteLine("  step   | stern reachable | wall-clock");
Console.WriteLine("  -------|-----------------|-----------");
var stern = new DeckReachability.Point(-30, 0);
IReadOnlyList<SurfaceCollision.Segment> intact = WreckLayout.Walls(Derelict.WreckCause.DriveFailure);
foreach (double step in new[] { 2.0, 1.0, 0.5, 0.25 })
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    bool ok = DeckReachability.CanReach(spawn, stern, intact, AvatarRadius, WreckLayout.Bounds, step);
    sw.Stop();
    Console.WriteLine(
        $"  {step.ToString("0.00", ci),-6} | {(ok ? "yes" : "NO"),-15} | {sw.Elapsed.TotalMilliseconds.ToString("0.0", ci)} ms");
}
Console.WriteLine();
Console.WriteLine($"  The audit runs at {DeckReachability.DefaultStep.ToString("0.00", ci)} — fine enough for a doorway, cheap enough for every cause.");
Console.WriteLine();

// ── C · the audit, over every cause ───────────────────────────────────────────────────────────────────
Console.WriteLine("C — THE AUDIT (what CI asserts on every run)");
Console.WriteLine("  cause                | stations | compartments | bow | stern");
Console.WriteLine("  ---------------------|----------|--------------|-----|------");
foreach (Derelict.WreckCause cause in Enum.GetValues<Derelict.WreckCause>())
{
    IReadOnlyList<SurfaceCollision.Segment> walls = WreckLayout.Walls(cause);

    IReadOnlyList<string> missed = DeckReachability.Unreachable(
        spawn, WreckLayout.Stations(cause), walls, AvatarRadius, WreckLayout.Bounds);

    int sealedRooms = 0;
    foreach ((string _, float x0, float x1, bool top) in WreckLayout.Compartments)
    {
        var middle = new DeckReachability.Point((x0 + x1) / 2.0, top ? -6.0 : 6.0);
        if (!DeckReachability.CanReach(spawn, middle, walls, AvatarRadius, WreckLayout.Bounds))
        {
            sealedRooms++;
        }
    }

    bool bow = DeckReachability.CanReach(spawn, new(20, 0), walls, AvatarRadius, WreckLayout.Bounds);
    bool aft = DeckReachability.CanReach(spawn, stern, walls, AvatarRadius, WreckLayout.Bounds);

    Console.WriteLine(
        $"  {cause,-21}| {(missed.Count == 0 ? "all ok" : $"{missed.Count} SEALED"),-9}| " +
        $"{(sealedRooms == 0 ? $"{WreckLayout.Compartments.Length} ok" : $"{sealedRooms} SEALED"),-13}| " +
        $"{(bow ? "ok" : "NO"),-4}| {(aft ? "ok" : "NO")}");
}
Console.WriteLine();

// ── D · the bug, reproduced ───────────────────────────────────────────────────────────────────────────
//
// A guard that only ever passes guards nothing. Put the original full-height barricades back and prove the
// audit sees exactly what the owner saw.
Console.WriteLine("D — THE ORIGINAL BUG, REPRODUCED (barricades spanning the full corridor)");
List<SurfaceCollision.Segment> broken = [.. WreckLayout.Walls(Derelict.WreckCause.DriveFailure)];
broken.Add(new(-1f, -WreckLayout.SpineHalfHeight, -1f, WreckLayout.SpineHalfHeight));
broken.Add(new(4f, -WreckLayout.SpineHalfHeight, 4f, WreckLayout.SpineHalfHeight));

IReadOnlyList<string> sealedOffNow = DeckReachability.Unreachable(
    spawn, WreckLayout.Stations(Derelict.WreckCause.DriveFailure), broken, AvatarRadius, WreckLayout.Bounds);

Console.WriteLine($"  stern reachable : {(DeckReachability.CanReach(spawn, stern, broken, AvatarRadius, WreckLayout.Bounds) ? "yes" : "NO")}");
Console.WriteLine($"  sealed stations : {(sealedOffNow.Count == 0 ? "none" : string.Join(", ", sealedOffNow))}");
Console.WriteLine();
Console.WriteLine("  That is the ship the owner was standing in, and the red test we now have for it.");

// Lab 45 · What a frame costs — guards vs Reevers vs the furniture.
//
// Owner, 2026-08-12, watching one man walk a corridor: "the guard seems to move somehow much more heavily
// than the reevers who we can have so many on the screen without slowdown. So what do you think, a lab about
// performance with guards, reevers and furniture / walls :-D"
//
// The hypothesis on trial (#852): guards are O(walls) per guard per frame because of line-of-sight sweeps,
// Reevers are O(1) per body. It is a good hypothesis and this lab exists to put a stopwatch on it rather
// than to agree with it.
//
// EVERYTHING TIMED HERE IS THE SHIPPING CODE, on a REAL generated floor: HiveInterior.FloorDeck, its own
// DeckPlan.CollisionField, its own DeckPlan.CollisionSegments, Core's PatrolBeat, Core's SurfaceCollision,
// Core's AutoWalk. Native Release console, no renderer, no browser — see the README for what that does and
// does not transfer.
using System.Diagnostics;
using System.Globalization;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Labs.Lab45;

// The client assembly is browser-targeted, so everything this lab touches carries
// [SupportedOSPlatform("browser")] and CA1416 objects to a console app calling it. The annotation is a
// COMPILE-TIME contract with no runtime effect — none of this code path does JS interop, it reads geometry
// and does arithmetic — and Lab 44 and tests/SpaceSails.Client.Tests already run the same types on the
// desktop runtime for the same reason.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

const double Dt = 1.0 / 60.0;          // one frame at 60 fps, the budget everything below is measured against
const double FrameBudgetMs = 1000.0 / 60.0;
const double ReeverStepDu = 2.4 / 60.0;  // a shamble's worth of deck units in one frame

string? Option(string name)
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

int frames = Option("--frames") is { } f ? int.Parse(f, CultureInfo.InvariantCulture) : 1800;
string body = Option("--body") ?? "luna";

SurfaceLayout.Field env = MoonSurface.ExpeditionField();

// The bodies the guard benches walk. Rebuilt by Reset() before every phase, so no bench ever inherits the
// last one's positions or the last one's place on the round.
GuardBody[] _guards = [];

Console.WriteLine("=== LAB 45 · WHAT A FRAME COSTS ===");
Console.WriteLine(
    $"machine: {Environment.ProcessorCount} logical CPUs · {System.Runtime.InteropServices.RuntimeInformation.OSDescription} · " +
    $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture} · .NET {Environment.Version}");
Console.WriteLine(
    $"build: {(IsDebug() ? "DEBUG — these numbers are NOT the lab's numbers" : "Release")} · " +
    $"server GC {System.Runtime.GCSettings.IsServerGC} · frames per measurement: {frames} (dt = 1/60 s)");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  THE WORLDS
//
//  Both are floors the game really generates. The comparison world is NOT a hand-thinned copy of the first
//  one — #834's desks, screens, kitchenettes and cubicles are laid by CORE, into floor.Walls, so there is no
//  honest way to "switch the furniture off" on a floor that has it. What the building actually offers is a
//  floor WITH the office frontage and a floor WITHOUT it, and those are the two rows below. Section B then
//  varies the wall count and NOTHING else, which is the clean causal test a two-floor comparison cannot be.
//
//  --scan prints the whole building, and the first thing it says is the finding that reframes this issue:
//  the ~465-segment floor is B1, and B1 is the ONE floor of every site that PatrolBeat.IsPatrolled says NO
//  to. The furniture and the round have never yet been on the same floor.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
if (Array.IndexOf(args, "--scan") >= 0)
{
    Console.WriteLine($"{"site",-16} {"floor",6} {"segs",5} {"park",5} {"patrolled",10}");
    foreach (string site in new[]
             {
                 "luna", "phobos", "europa", "ganymede", "callisto",
                 "titan", "enceladus", "miranda", "triton", "the-clinker",
             })
    {
        foreach (int lv in UndergroundComplex.FloorsOf(site))
        {
            DeckPlan d = HiveInterior.FloorDeck(site, lv, env, 0, (_, _) => { }, []);
            Console.WriteLine(
                $"{site,-16} {"B" + -lv,6} {d.CollisionSegments.Length,5} " +
                $"{(UndergroundComplex.HasParkBlock(site, lv) ? "yes" : ""),5} " +
                $"{(PatrolBeat.IsPatrolled(site, lv) ? "yes" : ""),10}");
        }
    }
    return;
}

World heavy = World.Build($"{body} B1", body, -1, env);    // the park block and its office frontage — #834
World light = World.Build($"{body} B2", body, -2, env);    // corridors, ribs and rooms — the shape B1 had before
var worlds = new List<World> { heavy, light };

Console.WriteLine("── THE FLOORS ───────────────────────────────────────────────────────────────────────");
Console.WriteLine($"{"floor",-12} {"segs",5} {"locked",7} {"stops",6} {"standable",10} {"round?",7}  what it is");
foreach (World w in worlds)
{
    string what = ReferenceEquals(w, heavy)
        ? "THE HEAVY WORLD — the #834 offices"
        : "THE LIGHT WORLD — no frontage";
    Console.WriteLine(
        $"{w.Name,-12} {w.SegmentCount,5} {w.ShutDoors,7} {w.Beat.Count,6} {w.Standable.Count,10} " +
        $"{(PatrolBeat.IsPatrolled(w.Body, w.Level) ? "yes" : "NO"),7}  {what}");
}
Console.WriteLine(
    $"({heavy.Name} carries {heavy.SegmentCount - light.SegmentCount} more segments than {light.Name} — " +
    "the frontage, the glazing, the desks, the cubicles and the park's own beds.)");
Console.WriteLine();

// Where the captain stands. THE CAR — stop #1 on every round, and the square a captain actually arrives on
// when the lift doors open. Not cherry-picked: it is the one place the round is guaranteed to come to.
(double capX, double capY) = HiveInterior.SpawnOn(env);

// …and where a captain who has walked away stands: the standable cell furthest from the car. Every sightline
// predicate range-checks BEFORE it sweeps, so this row is what a guard costs when nobody is looking.
(double farX, double farY) = Furthest(heavy, capX, capY);

Console.WriteLine($"captain at the car: ({capX:F1}, {capY:F1}) · captain walked off: ({farX:F1}, {farY:F1}), " +
    $"{Dist(capX, capY, farX, farY):F1} du away (PatrolBeat.MarkerSightDu = {PatrolBeat.MarkerSightDu:F0})");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  SECTION A · STEPPING COST vs COUNT
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── SECTION A · ms PER FRAME, N BODIES ───────────────────────────────────────────────");
Console.WriteLine();
Console.WriteLine("A1 · REEVERS (ReeverChase.Step against DeckPlan.CollisionField, the indexed field)");
Console.WriteLine($"{"floor",-12} {"segs",5} {"N",5} {"ms/frame",10} {"us/reever",10} {"on the stone",13} {"% frame",8}");
foreach (World w in new[] { heavy, light })
{
    foreach (int n in new[] { 10, 50, 200 })
    {
        double ms = ReeverFrame(w, n, frames);
        Console.WriteLine(
            $"{w.Name,-12} {w.SegmentCount,5} {n,5} {ms,10:F4} {ms * 1000 / n,10:F2} " +
            $"{StallRate(w, n, frames),12:P0} {ms / FrameBudgetMs * 100,7:F2}%");
    }
}
Console.WriteLine("(\"on the stone\" — the share of steps the direct run spends on a wall and the handrail has to");
Console.WriteLine(" take over, which is TWO further Slide calls. It is the only thing that moves a Reever's price,");
Console.WriteLine(" and it is a fact about corridors, not about how many walls the floor has.)");
Console.WriteLine();

Console.WriteLine("A2 · GUARDS, captain AT THE CAR (the round comes to him — sweeps happen)");
Console.WriteLine($"{"floor",-12} {"segs",5} {"N",5} {"legs ms",9} {"eye ms",9} {"sight[] ms",10} {"whole ms",9} {"sweeps/fr",10} {"% frame",8}");
foreach (World w in new[] { heavy, light })
{
    foreach (int n in new[] { 1, 2, 4, 8 })
    {
        GuardSplit(w, n, frames, capX, capY);
    }
}
Console.WriteLine();

Console.WriteLine("A3 · GUARDS, captain WALKED OFF (out of eyeshot — the range test rejects before the sweep)");
Console.WriteLine($"{"floor",-12} {"segs",5} {"N",5} {"legs ms",9} {"eye ms",9} {"sight[] ms",10} {"whole ms",9} {"sweeps/fr",10} {"% frame",8}");
foreach (World w in new[] { heavy, light })
{
    foreach (int n in new[] { 1, 2, 4, 8 })
    {
        GuardSplit(w, n, frames, farX, farY);
    }
}
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  SECTION B · THE SIGHTLINE, ISOLATED
//
//  ONE set of query pairs, drawn once off the heavy floor's own wash, asked against wall lists of four
//  different sizes cut as PREFIXES of one pool of real Hive stone. Only the wall count varies, so whatever
//  moves is the wall count moving it — which is the #841 gate's actual question.
//
//  AND THE TABLE IS SPLIT IN TWO, because a mixed set of queries cannot answer it. HasLineOfSight returns
//  the instant it finds a crossing, so a BLOCKED sightline's cost is "how far down the list the blocker
//  happened to be filed" — which is generator order, not wall count, and it is why the first draft of this
//  section printed a 436-wall floor as CHEAPER than a 270-wall one. A CLEAR sightline has no early exit: it
//  reads every segment, every time. The prefixes are nested, so a query clear against the 800-wall pool is
//  clear against all four sizes — that column is the same question asked of four buildings, and it is the
//  one that measures O(walls).
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── SECTION B · HasLineOfSight vs SEGMENT COUNT ──────────────────────────────────────");

var pool = new List<SurfaceCollision.Segment>(heavy.Segments);
for (int lv = -2; pool.Count < 800; lv--)
{
    // Real stone from the floors below, appended until the pool can answer the 800-wall row. It is a
    // "what if the building grows" world and it is made of the building's own walls.
    DeckPlan more = HiveInterior.FloorDeck(body, lv, env, 0, (_, _) => { }, []);
    pool.AddRange(more.CollisionSegments);
}

(double AX, double AY, double BX, double BY)[] mixed = SightQueries(heavy, 2000);
SurfaceCollision.WallIndex biggest = SurfaceCollision.WallIndex.Build([.. pool.GetRange(0, 800)]);
var clear = new List<(double AX, double AY, double BX, double BY)>();
foreach ((double ax, double ay, double bx, double by) in mixed)
{
    if (SurfaceCollision.HasLineOfSight(ax, ay, bx, by, biggest))
    {
        clear.Add((ax, ay, bx, by));
    }
}
(double AX, double AY, double BX, double BY)[] clearOnly = [.. clear];

Console.WriteLine($"{mixed.Length} query pairs drawn from {heavy.Name}'s reachable wash, each ≤ " +
    $"{PatrolBeat.MarkerSightDu:F0} du (the eye's reach); {clearOnly.Length} of them are CLEAR against all " +
    "800 walls and therefore sweep the whole list at every size.");
Console.WriteLine();
Console.WriteLine($"{"segments",9} | {"CLEAR sightline: ns/ask",23} {"ns/segment",11} {"index ns",9} {"speedup",8} | " +
    $"{"as asked in play: ns/ask",24} {"index ns",9}");
int[] sizes = [100, 270, 436, 800];
var plainCurve = new List<double>();
var indexCurve = new List<double>();
foreach (int n in sizes)
{
    var slice = pool.GetRange(0, n);
    SurfaceCollision.WallIndex indexed = SurfaceCollision.WallIndex.Build([.. slice]);

    double clearNs = LosNs(slice, clearOnly);
    double clearIdx = LosNs(indexed, clearOnly);
    double mixedNs = LosNs(slice, mixed);
    double mixedIdx = LosNs(indexed, mixed);
    plainCurve.Add(clearNs);
    indexCurve.Add(clearIdx);
    Console.WriteLine(
        $"{n,9} | {clearNs,23:F1} {clearNs / n,11:F2} {clearIdx,9:F1} {clearNs / clearIdx,7:F1}x | " +
        $"{mixedNs,24:F1} {mixedIdx,9:F1}");
}
Console.WriteLine();

if (Array.IndexOf(args, "--svg") >= 0)
{
    string svgPath = Option("--svg") is { } given && !given.StartsWith("--", StringComparison.Ordinal)
        ? given
        : "sightline-cost.svg";
    File.WriteAllText(svgPath, CostSvg.Draw(
        sizes, plainCurve, indexCurve,
        [(light.Name, light.SegmentCount), (heavy.Name, heavy.SegmentCount)], clearOnly.Length));
    Console.WriteLine($"[svg] wrote {Path.GetFullPath(svgPath)}");
    Console.WriteLine();
}
Console.WriteLine("…and what that is worth per frame, at the worst case the eye can reach (3 sweeps × N guards,");
Console.WriteLine("every sightline clear — the corridor case, which is exactly where a guard is legible):");
Console.WriteLine($"{"segments",9} {"1 guard ms",11} {"2",9} {"4",9} {"8",9}   (plain list)");
foreach (int n in new[] { 100, 270, 436, 800 })
{
    double ns = LosNs(pool.GetRange(0, n), clearOnly);
    Console.WriteLine(
        $"{n,9} {ns * 3 / 1e6,11:F4} {ns * 3 * 2 / 1e6,9:F4} {ns * 3 * 4 / 1e6,9:F4} {ns * 3 * 8 / 1e6,9:F4}");
}
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  SECTION C · THE A* SPIKE
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── SECTION C · AutoWalk.Plan on PatrolBeat.LatticeFor ───────────────────────────────");
Console.WriteLine($"{"floor",-12} {"legs",5} {"best ms",9} {"median ms",10} {"WORST ms",9} {"worst = % of a frame",20} {"lattice cells",14}  worst leg");
foreach (World w in new[] { heavy, light })
{
    PlanBench(w);
}
Console.WriteLine();
Console.WriteLine($"{"floor",-12} {"plans/min/guard",16} {"sim minutes",12}  (counted by running one guard's round)");
foreach (World w in new[] { heavy, light })
{
    const double minutes = 5.0;
    var g = new GuardBody(w, 0, 1);
    int steps = (int)(minutes * 60 / Dt);
    for (int i = 0; i < steps; i++)
    {
        g.WalkTheRound(w, Dt);
    }
    Console.WriteLine($"{w.Name,-12} {g.Plans / minutes,16:F1} {minutes,12:F0}");
}
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  SECTION D · THE DRAW SIDE — NOT MEASURED, AND WHY
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── SECTION D · THE DRAW SIDE ────────────────────────────────────────────────────────");
Console.WriteLine("SKIPPED — and deliberately, not for want of trying. The renderer is Blazor + a 2D canvas");
Console.WriteLine("behind JS interop; there is no headless path to it, it carries no internal frame counter,");
Console.WriteLine("and the standing law of this repo is that a timing taken from an MCP-driven tab is invalid");
Console.WriteLine("(document.hidden ⇒ rAF throttled, timers clamped). A number I could get here would be a");
Console.WriteLine("number I would have to disown. What this lab CAN say about #841 is the sim-side half, and");
Console.WriteLine("Sections A and B say it exactly.");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  SECTION E · THE HARNESS PROVES ITSELF
//
//  A benchmark that cannot see a change is not measuring anything. Same bench, same world, same N — with a
//  deliberately slowed step wired into the loop. If the slowed row does not stand out, every other row in
//  this file is worthless and should be treated as such.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────
Console.WriteLine("── SECTION E · THE CONTROL ──────────────────────────────────────────────────────────");
const int ControlFrames = 200;
const int ControlGuards = 4;
double honest = GuardWhole(heavy, ControlGuards, ControlFrames, capX, capY, slowMs: 0);
double slowed = GuardWhole(heavy, ControlGuards, ControlFrames, capX, capY, slowMs: 1);
double planted = SpinControl(heavy, ControlGuards, ControlFrames, capX, capY);
Console.WriteLine($"{"run",-46} {"ms/frame",10}");
Console.WriteLine($"{$"{ControlGuards} guards on {heavy.Name}, as shipped",-46} {honest,10:F4}");
Console.WriteLine($"{$"…with Thread.Sleep(1) per guard per frame",-46} {slowed,10:F4}");
Console.WriteLine($"{$"…with a 50 us busy-spin per guard per frame",-46} {planted,10:F4}");
Console.WriteLine(
    $"the sleep shows as {slowed - honest:F3} ms/frame over {ControlGuards} guards " +
    $"({(slowed - honest) * 1000 / ControlGuards:F0} us each, asked for 1000 us — Windows' sleep granularity, " +
    "and it is the DETECTION that is on trial here, not the sleep's accuracy).");
Console.WriteLine(
    $"the spin shows as {planted - honest:F3} ms/frame over {ControlGuards} guards " +
    $"({(planted - honest) * 1000 / ControlGuards:F1} us each, planted 50.0 us). " +
    $"The harness resolves a planted cost {(planted - honest) / Math.Max(honest, 1e-9):F0}x " +
    "the size of the thing it is measuring, so it can see the thing it is measuring.");
Console.WriteLine();

Console.WriteLine("── FOR THE README ───────────────────────────────────────────────────────────────────");
Console.WriteLine($"one 60 fps frame = {FrameBudgetMs:F2} ms.");

// ─────────────────────────────────────────────────────────────────────────────────────────────────────
//  THE BENCHES
// ─────────────────────────────────────────────────────────────────────────────────────────────────────

// How often the direct run is refused by stone — replayed untimed over the identical chase, so the number
// beside a timing explains it rather than decorating it. ReeverChase's own StallFraction is private; the
// test here is the one its caller can see: the body moved less than a quarter of the step it asked for.
double StallRate(World w, int n, int f)
{
    (double X, double Y)[] at = w.Spread(n);
    double step = ReeverStepDu;
    long stalled = 0, taken = 0;
    for (int frame = 0; frame < f; frame++)
    {
        for (int i = 0; i < at.Length; i++)
        {
            (double nx, double ny) = ReeverChase.Step(
                at[i].X, at[i].Y, capX, capY, step, double.PositiveInfinity, w.Legs, DeckPlan.AvatarRadius,
                (i & 1) == 0 ? 1 : -1);
            if (Dist(at[i].X, at[i].Y, nx, ny) < step * 0.25)
            {
                stalled++;
            }
            taken++;
            at[i] = (nx, ny);
        }
    }
    return taken == 0 ? 0 : (double)stalled / taken;
}

// One frame of the Reever tide, exactly as Map.Surface.cs steps it: the indexed field, the captain's own
// radius, the fixed per-contact handedness, the crew-only barrier.
double ReeverFrame(World w, int n, int f)
{
    (double X, double Y)[] at = w.Spread(n);
    double barrier = double.PositiveInfinity;   // underground: the tube line is a surface fiction
    double step = ReeverStepDu;

    return Measure(f, () =>
    {
        for (int i = 0; i < at.Length; i++)
        {
            (double nx, double ny) = ReeverChase.Step(
                at[i].X, at[i].Y, capX, capY, step, barrier, w.Legs, DeckPlan.AvatarRadius,
                (i & 1) == 0 ? 1 : -1);
            at[i] = (nx, ny);
        }
    }, () => at = w.Spread(n));
}

// The guard, split into the three things a frame of him costs.
//
// The eye is taken BY DIFFERENCE — the same walking round timed with the three asks and without them —
// rather than by timing the asks against guards standing still. A guard's sightline bill depends on where
// he IS (every predicate range-checks first), so a bench that froze him at his stop would be pricing a
// configuration the round never holds. Same frames, same walk, same seed: the only difference between the
// two runs is the eye.
void GuardSplit(World w, int n, int f, double cx, double cy)
{
    void Walk()
    {
        for (int i = 0; i < _guards.Length; i++)
        {
            _guards[i].WalkTheRound(w, Dt);
        }
    }

    double legs = Measure(f, Walk, () => Reset(w, n));
    double rebuild = Measure(f, w.RebuildSight, () => Reset(w, n));
    double baseline = Measure(f, () => { w.RebuildSight(); Walk(); }, () => Reset(w, n));
    double whole = GuardWhole(w, n, f, cx, cy, slowMs: 0);
    double eye = whole - baseline;

    // …and how many wall sweeps those asks actually ran, replayed untimed over the identical walk.
    Reset(w, n);
    long sweeps = 0;
    for (int i = 0; i < f; i++)
    {
        Walk();
        for (int g = 0; g < _guards.Length; g++)
        {
            sweeps += _guards[g].SweepsFor(cx, cy);
        }
    }

    Console.WriteLine(
        $"{w.Name,-12} {w.SegmentCount,5} {n,5} {legs,9:F4} {eye,9:F4} {rebuild,10:F4} {whole,9:F4} " +
        $"{(double)sweeps / f,10:F2} {whole / FrameBudgetMs * 100,7:F2}%");
}

double GuardWhole(World w, int n, int f, double cx, double cy, int slowMs)
{
    int sink = 0;
    return Measure(f, () =>
    {
        w.RebuildSight();                       // SightBlockers(), once a frame, as the component calls it
        for (int i = 0; i < _guards.Length; i++)
        {
            _guards[i].WalkTheRound(w, Dt);
            sink += _guards[i].TheEye(w, cx, cy);
            if (slowMs > 0)
            {
                System.Threading.Thread.Sleep(slowMs);
            }
        }
        if (sink < 0)
        {
            Console.Write("");
        }
    }, () => Reset(w, n));
}

// The second control: a busy-spin of a KNOWN duration, because Thread.Sleep on Windows is granular and a
// control that can only say "something got slower" is weaker than one that can say how much.
double SpinControl(World w, int n, int f, double cx, double cy)
{
    int sink = 0;
    double ticksPer50Us = Stopwatch.Frequency * 50e-6;
    return Measure(f, () =>
    {
        w.RebuildSight();
        for (int i = 0; i < _guards.Length; i++)
        {
            _guards[i].WalkTheRound(w, Dt);
            sink += _guards[i].TheEye(w, cx, cy);
            long until = Stopwatch.GetTimestamp() + (long)ticksPer50Us;
            while (Stopwatch.GetTimestamp() < until)
            {
                // deliberately burning wall clock
            }
        }
        if (sink < 0)
        {
            Console.Write("");
        }
    }, () => Reset(w, n));
}

double LosNs(IReadOnlyList<SurfaceCollision.Segment> walls, (double AX, double AY, double BX, double BY)[] q)
{
    // Warm.
    int clear = 0;
    for (int r = 0; r < 3; r++)
    {
        foreach ((double ax, double ay, double bx, double by) in q)
        {
            if (SurfaceCollision.HasLineOfSight(ax, ay, bx, by, walls))
            {
                clear++;
            }
        }
    }

    const int Reps = 20;
    var sw = Stopwatch.StartNew();
    for (int r = 0; r < Reps; r++)
    {
        foreach ((double ax, double ay, double bx, double by) in q)
        {
            if (SurfaceCollision.HasLineOfSight(ax, ay, bx, by, walls))
            {
                clear++;
            }
        }
    }
    sw.Stop();
    if (clear < 0)
    {
        Console.Write("");
    }
    return sw.Elapsed.TotalMilliseconds * 1e6 / (Reps * (double)q.Length);
}

void PlanBench(World w)
{
    var times = new List<double>();
    string worstLeg = "";
    double worst = -1, worstCells = 0;

    for (int i = 0; i < w.Beat.Count; i++)
    {
        PatrolBeat.Stop from = w.Beat[i], to = w.Beat[(i + 1) % w.Beat.Count];
        (double MinX, double MinY, double MaxX, double MaxY) box = PatrolBeat.LatticeFor(in from, in to, w.Envelope);

        // warm
        _ = AutoWalk.Plan(true, P(from), P(to), w.Legs, DeckPlan.AvatarRadius, box);

        const int Reps = 20;
        var sw = Stopwatch.StartNew();
        for (int r = 0; r < Reps; r++)
        {
            _ = AutoWalk.Plan(true, P(from), P(to), w.Legs, DeckPlan.AvatarRadius, box);
        }
        sw.Stop();
        double ms = sw.Elapsed.TotalMilliseconds / Reps;
        times.Add(ms);

        if (ms > worst)
        {
            worst = ms;
            worstLeg = $"{from.What} → {to.What}";
            worstCells = (box.MaxX - box.MinX) / DeckReachability.DefaultStep
                * ((box.MaxY - box.MinY) / DeckReachability.DefaultStep);
        }
    }

    times.Sort();
    Console.WriteLine(
        $"{w.Name,-12} {w.Beat.Count,5} {times[0],9:F3} {times[times.Count / 2],10:F3} {worst,9:F3} " +
        $"{worst / FrameBudgetMs * 100,19:F1}% {worstCells,14:F0}  {Trim(worstLeg, 40)}");
}

static DeckReachability.Point P(in PatrolBeat.Stop s) => new(s.X, s.Y);

static string Trim(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";

// The measurement itself. Warm the code and the branch predictor, reset the world, then time F frames and
// divide. `reset` is called before each phase so a bench never inherits the last one's bodies.
double Measure(int f, Action frame, Action? reset = null)
{
    reset?.Invoke();
    int warm = Math.Max(10, f / 10);
    for (int i = 0; i < warm; i++)
    {
        frame();
    }

    reset?.Invoke();
    GC.Collect();
    GC.WaitForPendingFinalizers();

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < f; i++)
    {
        frame();
    }
    sw.Stop();
    return sw.Elapsed.TotalMilliseconds / f;
}

void Reset(World w, int n)
{
    _guards = new GuardBody[n];
    for (int i = 0; i < n; i++)
    {
        _guards[i] = new GuardBody(w, i, n);
    }
}

static double Dist(double ax, double ay, double bx, double by) =>
    Math.Sqrt(((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay)));

static (double X, double Y) Furthest(World w, double fromX, double fromY)
{
    (double X, double Y) best = (fromX, fromY);
    double bestD = -1;
    foreach (DeckReachability.Point p in w.Standable)
    {
        double d = Dist(fromX, fromY, p.X, p.Y);
        if (d > bestD)
        {
            bestD = d;
            best = (p.X, p.Y);
        }
    }
    return best;
}

// Query pairs that look like the ones the game asks: a captain somewhere on the floor and a guard within the
// eye's reach of them. Drawn deterministically off the wash's own order, so every row of Section B is asked
// exactly the same questions.
static (double AX, double AY, double BX, double BY)[] SightQueries(World w, int want)
{
    var q = new List<(double, double, double, double)>(want);
    IReadOnlyList<DeckReachability.Point> wash = w.Standable;
    int n = wash.Count;
    for (int i = 0; q.Count < want && i < n * 4; i++)
    {
        DeckReachability.Point a = wash[i % n];
        DeckReachability.Point b = wash[(int)(((long)i * 7919) % n)];
        double d = Dist(a.X, a.Y, b.X, b.Y);
        if (d is > 1.0 and <= PatrolBeat.MarkerSightDu)
        {
            q.Add((a.X, a.Y, b.X, b.Y));
        }
    }
    return [.. q];
}

static bool IsDebug()
{
#if DEBUG
    return true;
#else
    return false;
#endif
}

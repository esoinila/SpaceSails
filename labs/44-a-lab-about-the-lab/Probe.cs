// Lab 44 · A lab about the lab — can you actually walk the floor we drew you?
//
// Owner, 2026-08-01: "The underground layer checking in CI might be a case for lab in itself (lab about
// lab)" 😄
//
// #587 was a handful of rooms that were drawn, offered 🔦 SEARCH THE ROOM, and could not be entered, on
// 35 of ~130 floors. The A* audit found it and could not explain it: the audit prints coordinates, and a
// coordinate does not tell you which wall put it there. The evening that followed was one build at a time.
//
// This ends that. It renders one floor — walls, mouths, doors, rooms — and washes it with everywhere the
// captain can actually stand, flooded from the lift doors over the CLIENT's own collision field. A sealed
// room is an island nobody coloured in, and you see it from across the room.
//
// It is also a CI-friendly table on its own: rooms reachable / rooms total, per floor, per body.
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Labs.Lab44;

// The client assembly is browser-targeted, so everything this lab touches carries
// [SupportedOSPlatform("browser")] and CA1416 objects to a console app calling it. The annotation is a
// COMPILE-TIME contract with no runtime effect — none of this code path does JS interop, it reads geometry —
// and tests/SpaceSails.Client.Tests already runs the same types on the desktop runtime for the same reason.
// Declared at assembly scope because top-level statements have no class of their own to attribute.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

// The field the client hands the generator — read from Core, never retyped. A lab that copies a constant
// prints confident measurements of a world that stopped shipping (that is #573, and it really happened).
SurfaceLayout.Field field = MoonSurface.ExpeditionField();

string[] defaultBodies =
[
    "luna", "phobos", "europa", "ganymede", "callisto",
    "titan", "enceladus", "miranda", "triton", "the-clinker",
];

string? Option(string name)
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

bool Flag(string name) => Array.IndexOf(args, name) >= 0;

bool wantSvg = Flag("--svg");
string outDir = Option("--svg") is { } d && !d.StartsWith("--", StringComparison.Ordinal) ? d : "labviz";
string[] bodies = Option("--body") is { } b ? [b] : defaultBodies;
bool allFloors = Flag("--all-floors") || Option("--floor") is null;

Console.WriteLine($"FIELD  {field.RightX - field.LeftX:F0} x {field.LandingBandY - field.BottomY:F0} du");
Console.WriteLine();

int floorsSeen = 0, floorsBad = 0;
var complaints = new List<string>();

foreach (string body in bodies)
{
    int bottom = UndergroundComplex.DepthOf(body);
    UndergroundComplex.Kind kind = UndergroundComplex.KindFor(body);
    Console.WriteLine(
        $"=== {body.ToUpperInvariant()} — {UndergroundComplex.TitleOf(kind)}, {-bottom} floors ===");

    // `--floor N` is read the way the game's own dev cheat reads it: a positive number is a DEPTH, so
    // `--floor 4` is B4. A negative level is accepted too, because that is what Core speaks.
    IEnumerable<int> levels;
    if (allFloors)
    {
        var all = new List<int>();
        for (int level = -1; level >= bottom; level--)
        {
            all.Add(level);
        }
        levels = all;
    }
    else
    {
        int asked = int.Parse(Option("--floor")!, System.Globalization.CultureInfo.InvariantCulture);
        levels = [Math.Max(bottom, -Math.Abs(asked))];
    }

    foreach (int level in levels)
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, field);
        FloorReport report = FloorReport.For(body, level, field);
        floorsSeen++;

        string verdict = report.AllReached
            ? "ok"
            : $"** {report.Rooms - report.RoomsReached} SEALED{(report.LiftReached ? "" : " + LIFT")}";
        if (!report.AllReached)
        {
            floorsBad++;
            var where = new List<string>();
            for (int i = 0; i < floor.RoomCentres.Count; i++)
            {
                if (!report.RoomReached[i])
                {
                    where.Add($"({floor.RoomCentres[i].X:F0}, {floor.RoomCentres[i].Y:F0})");
                }
            }
            if (!report.LiftReached)
            {
                where.Add("the lift");
            }
            complaints.Add($"  {body} {floor.Name}: {string.Join(", ", where)}");
        }

        Console.WriteLine(
            $"  {floor.Name,-24} {floor.Walls.Count,4} segs · {floor.Ribs.Count} ribs · " +
            $"{floor.Locked.Count,2} sealed doors · rooms {report.RoomsReached}/{report.Rooms,-2} · " +
            $"{(floor.Pressurised ? "air " : "dead")} · {verdict}");

        if (wantSvg)
        {
            Directory.CreateDirectory(outDir);
            string path = Path.Combine(outDir, $"hive-{body}-b{-level}.svg");
            File.WriteAllText(path, FloorSvg.Draw(body, level, field, report));
            Console.WriteLine($"                           [svg] {path}");
        }
    }
    Console.WriteLine();
}

Console.WriteLine($"{floorsSeen} floor(s) walked · {floorsBad} with something the captain cannot reach.");
foreach (string c in complaints)
{
    Console.WriteLine(c);
}

// #573 · A lab that reports a defect and exits 0 is a lab whose finding nobody notices. This one is
// runnable from a shell script and says so in its exit code.
return floorsBad == 0 ? 0 : 1;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #531 · THE DEAD STATION — a salvage object too big and too broken to walk end to end, where the shuttle
/// is how you get from one part of it to another.
///
/// <para>Owner, 2026-07-31: <i>"lets do the multipart ship where we move the shuttle from one place on huge
/// salvage ship to another on the same salvage ship"</i>, then sharpened it: <i>"A big salvage station is so
/// damaged that we must use shuttle to access it from multiple pre-existing airlocks or self made
/// hatches."</i> And the architectural line underneath both: <i>"It is a big change, instead of always going
/// to or from our ship, we go between two or more away places."</i></para>
///
/// <para><b>Why a station and not a longer ship.</b> A ship is a spine with rooms hung off it — make one
/// four times longer and it is still a corridor, and "walk to the far end" remains a plan, just a boring
/// one. A station is a HUB WITH ARMS: its parts are joined by a handful of thin tubes, and severing two of
/// those does not make it slightly harder to cross, it makes whole modules unreachable on foot. The
/// topology does the work, so the damage can be modest and believable and still force the shuttle.</para>
///
/// <para><b>The blockage is damage, not absence</b> (the owner's earlier ruling on this issue: <i>"The ship
/// can still be in one piece but just blocked indoors due to damage"</i>). She is one silhouette. Every
/// module is still bolted to the hub. What is gone is the ability to walk between them, and you can walk up
/// to a severed tube and read why.</para>
///
/// <para>Pure and deterministic per station id, so a given wreck always has the same modules severed and the
/// same hatches welded — a captain who leaves and comes back finds the problem they left.</para>
/// </summary>
public static class StationWreck
{
    /// <summary>The hub, and the four arms bolted to it. The hub is where a boarding party always starts:
    /// its lock is the one the station's own crew used, and it is the only access guaranteed serviceable.</summary>
    public enum ModuleId
    {
        /// <summary>The central drum: the ring corridor, and the only module joined to every other.</summary>
        Hub,

        /// <summary>Where they lived. Long-abandoned personal spaces — the module that carries the story.</summary>
        Habitat,

        /// <summary>Where they worked the ore. Heavy, hot, and the reason the station was here at all.</summary>
        Foundry,

        /// <summary>Where ships came alongside. Cradles, tugs, and the paperwork of everything that called.</summary>
        Docking,

        /// <summary>Where the power was made. The module most likely to be the reason she is dead.</summary>
        Reactor,
    }

    /// <summary>How a module can be entered from outside.</summary>
    public enum AccessKind
    {
        /// <summary>A working airlock. It cycles, so the module behind it keeps whatever atmosphere it has.</summary>
        ServiceableLock,

        /// <summary>A lock that no longer cycles, or none at all. The way in is to CUT one — and a cut face
        /// is not a lock: it does not cycle, so the module behind it is open to space from the moment you
        /// are through, and every hatch you open from there is a pressure decision. It is also permanent
        /// evidence that somebody came in the side.</summary>
        CutFace,
    }

    // ── The geometry, in deck units. A hub drum with four arms at the compass points. ────────────────────

    /// <summary>Half-width of the central hub drum.</summary>
    public const float HubHalf = 11f;

    /// <summary>How far from the hub's face an arm begins, and how long/wide each arm is.</summary>
    public const float TubeLength = 4f;
    public const float ArmLength = 26f;
    public const float ArmHalfWidth = 13f;

    /// <summary>Half-width of a connecting tube's bore — and therefore of the doorway at each end. Matches
    /// the derelict's own <see cref="WreckLayout.DoorHalfWidth"/> class of gap: wide enough to walk badly.</summary>
    public const float TubeHalfBore = 2.5f;

    /// <summary>A walkable box. Arms are axis-aligned rectangles; so is the hub.</summary>
    public readonly record struct Module(ModuleId Id, string Name, float X0, float Y0, float X1, float Y1)
    {
        public double CentreX => (X0 + X1) / 2.0;
        public double CentreY => (Y0 + Y1) / 2.0;
    }

    /// <summary>The hub plus its four arms. Bow-to-aft has no meaning here; order is Hub first, which is
    /// what every "start from the hub" query relies on.</summary>
    public static IReadOnlyList<Module> Modules { get; } =
    [
        new(ModuleId.Hub, "THE DRUM", -HubHalf, -HubHalf, HubHalf, HubHalf),
        new(ModuleId.Habitat, "HABITAT RING",
            -ArmHalfWidth, HubHalf + TubeLength, ArmHalfWidth, HubHalf + TubeLength + ArmLength),
        new(ModuleId.Foundry, "THE FOUNDRY",
            -ArmHalfWidth, -(HubHalf + TubeLength + ArmLength), ArmHalfWidth, -(HubHalf + TubeLength)),
        new(ModuleId.Docking, "DOCKING ARM",
            HubHalf + TubeLength, -ArmHalfWidth, HubHalf + TubeLength + ArmLength, ArmHalfWidth),
        new(ModuleId.Reactor, "REACTOR HOUSE",
            -(HubHalf + TubeLength + ArmLength), -ArmHalfWidth, -(HubHalf + TubeLength), ArmHalfWidth),
    ];

    /// <summary>The four arms, in a stable order. Everything seeded iterates this, so a station's answer
    /// never depends on enum ordering changing under it.</summary>
    public static IReadOnlyList<ModuleId> Arms { get; } =
        [ModuleId.Habitat, ModuleId.Foundry, ModuleId.Docking, ModuleId.Reactor];

    public static Module ModuleOf(ModuleId id) => Modules.First(m => m.Id == id);

    /// <summary>The whole station's bounding box — what a reachability audit is run inside.</summary>
    public static (double MinX, double MinY, double MaxX, double MaxY) Bounds
    {
        get
        {
            double reach = HubHalf + TubeLength + ArmLength;
            return (-reach, -reach, reach, reach);
        }
    }

    // ── What the damage did ──────────────────────────────────────────────────────────────────────────────

    /// <summary>How many of the four tubes are severed. Two is the floor, because ONE severed tube leaves a
    /// station you can still walk (round the hub to every other arm) and the shuttle would be a formality.
    /// Three is the ceiling: sever all four and the hub becomes an island with nothing to decide, since
    /// every arm needs its own flight regardless of what you do.</summary>
    public const int MinSevered = 2;
    public const int MaxSevered = 3;

    /// <summary>Which arms have lost their tube to the hub. Seeded per station, stable forever.</summary>
    public static IReadOnlyList<ModuleId> SeveredTubes(string stationId)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        int count = MinSevered + DiceRule.Roll(
            DiceRule.Seed($"stationwreck:{stationId}:severed-count"), MaxSevered - MinSevered + 1).Face - 1;

        // Rank the arms by a seeded key and cut the worst `count` of them — a stable, unbiased choice that
        // cannot accidentally pick the same arm twice the way repeated rolls can.
        return [.. Arms
            .OrderBy(a => DiceRule.Roll(DiceRule.Seed($"stationwreck:{stationId}:sever:{a}"), 4096).Face)
            .Take(count)
            .OrderBy(a => (int)a)];   // back into a stable, readable order for anything that prints them
    }

    /// <summary>Is this arm still joined to the hub by a walkable tube?</summary>
    public static bool TubeIntact(string stationId, ModuleId arm) => !SeveredTubes(stationId).Contains(arm);

    /// <summary>Why this tube is impassable — read off the tube itself when you walk up to it, so the way on
    /// being OUTSIDE is something you understand from in here rather than from a menu. Seeded per arm.</summary>
    public static string BlockageLine(string stationId, ModuleId arm)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        int face = DiceRule.Roll(DiceRule.Seed($"stationwreck:{stationId}:blockage:{arm}"), 4).Face;
        return face switch
        {
            1 => "The tube is folded shut. Something hit the station hard enough to bend a structural frame, " +
                 "and the bore closed on itself like a drinking straw. No cutting gear you carry opens that.",
            2 => "Debris, packed the whole bore and fused. Whatever came through here was moving, and it " +
                 "stopped in the one place that mattered.",
            3 => "The tube is open to space and the far hatch is gone. You could see straight through if the " +
                 "wreckage let you — but there is no suit-safe route across, and no lock at the far end to cycle.",
            _ => "Both hatches are shut and the gauges disagree by a full atmosphere. It is not locked, it is " +
                 "LOADED — ten tonnes of held air in a frame nobody has serviced in years.",
        };
    }

    // ── Ways in ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>An outside way into one module: which module, whether it can be cycled or must be cut, and
    /// where the shuttle sets the away team down.</summary>
    public readonly record struct Access(ModuleId Module, AccessKind Kind, double X, double Y, string Name);

    /// <summary>Every way into this station from outside — one per module, on its outboard face.
    ///
    /// <para><b>The hub's lock is ALWAYS serviceable.</b> It is the one the station's own crew used and the
    /// one a boarding party arrives at; a station whose first door needed cutting would open on a puzzle
    /// before it had earned one. Each arm's lock is seeded: some still cycle, some do not, and the ones that
    /// do not are how the cut earns its place in the game.</para>
    ///
    /// <para><b>Every module has SOME way in, always.</b> That is the law from this issue's spec — a module
    /// nobody can enter is content nobody will see — and it is why an arm without a serviceable lock gets a
    /// cut face rather than nothing at all.</para></summary>
    public static IReadOnlyList<Access> Accesses(string stationId)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        var list = new List<Access>
        {
            // The crew's own lock, on the drum's outboard face at the top-left quarter — clear of every tube
            // mouth so arriving never puts you in a doorway.
            new(ModuleId.Hub, AccessKind.ServiceableLock, -HubHalf, HubHalf * 0.45, "THE CREW LOCK"),
        };

        foreach (ModuleId arm in Arms)
        {
            Module m = ModuleOf(arm);
            bool cycles = DiceRule.Roll(DiceRule.Seed($"stationwreck:{stationId}:lock:{arm}"), 2).Face == 1;

            // On the arm's OUTBOARD face — the far end from the hub, so a flight to it is a flight around
            // the station rather than a hop over the tube you could not use.
            (double x, double y) = arm switch
            {
                ModuleId.Habitat => (m.CentreX, (double)m.Y1),
                ModuleId.Foundry => (m.CentreX, (double)m.Y0),
                ModuleId.Docking => ((double)m.X1, m.CentreY),
                _ => ((double)m.X0, m.CentreY),
            };

            list.Add(new(arm, cycles ? AccessKind.ServiceableLock : AccessKind.CutFace, x, y,
                cycles ? $"{m.Name} LOCK" : $"{m.Name} — NO SERVICEABLE LOCK"));
        }

        return list;
    }

    /// <summary>Where the away team stands after coming through a given access — just inboard of it, so the
    /// module is one step away and nobody ever spawns inside a wall.</summary>
    public static (double X, double Y) SpawnFor(in Access access)
    {
        Module m = ModuleOf(access.Module);
        const double inboard = 3.0;
        return access.Module switch
        {
            ModuleId.Hub => (access.X + inboard, access.Y),
            ModuleId.Habitat => (access.X, access.Y - inboard),
            ModuleId.Foundry => (access.X, access.Y + inboard),
            ModuleId.Docking => (access.X - inboard, access.Y),
            _ => (access.X + inboard, access.Y),
        };
    }

    // ── Walls ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The station's collision geometry: every module's shell, with a doorway cut at each end of an
    /// INTACT tube and the tube's own two walls; a SEVERED tube is drawn as tube walls with both ends solid,
    /// so it is visibly a passage and demonstrably not one.</summary>
    public static IReadOnlyList<SurfaceCollision.Segment> Walls(string stationId)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        var walls = new List<SurfaceCollision.Segment>();
        var intact = new HashSet<ModuleId>(Arms.Where(a => TubeIntact(stationId, a)));

        // The hub drum: four faces, each with a gap where an INTACT tube meets it.
        Module hub = ModuleOf(ModuleId.Hub);
        AddFaceWithOptionalGap(walls, hub.X0, hub.Y1, hub.X1, hub.Y1, horizontal: true, gap: intact.Contains(ModuleId.Habitat));
        AddFaceWithOptionalGap(walls, hub.X0, hub.Y0, hub.X1, hub.Y0, horizontal: true, gap: intact.Contains(ModuleId.Foundry));
        AddFaceWithOptionalGap(walls, hub.X1, hub.Y0, hub.X1, hub.Y1, horizontal: false, gap: intact.Contains(ModuleId.Docking));
        AddFaceWithOptionalGap(walls, hub.X0, hub.Y0, hub.X0, hub.Y1, horizontal: false, gap: intact.Contains(ModuleId.Reactor));

        foreach (ModuleId arm in Arms)
        {
            Module m = ModuleOf(arm);
            bool open = intact.Contains(arm);
            bool vertical = arm is ModuleId.Habitat or ModuleId.Foundry;

            // The arm's shell. The face TOWARD the hub carries the doorway when the tube is intact.
            if (vertical)
            {
                bool inboardIsY0 = arm == ModuleId.Habitat;
                float inboardY = inboardIsY0 ? m.Y0 : m.Y1;
                float outboardY = inboardIsY0 ? m.Y1 : m.Y0;
                AddFaceWithOptionalGap(walls, m.X0, inboardY, m.X1, inboardY, horizontal: true, gap: open);
                walls.Add(new(m.X0, outboardY, m.X1, outboardY));
                walls.Add(new(m.X0, m.Y0, m.X0, m.Y1));
                walls.Add(new(m.X1, m.Y0, m.X1, m.Y1));

                // The tube's two side walls, running hub face → arm face.
                float hubY = inboardIsY0 ? hub.Y1 : hub.Y0;
                walls.Add(new(-TubeHalfBore, hubY, -TubeHalfBore, inboardY));
                walls.Add(new(TubeHalfBore, hubY, TubeHalfBore, inboardY));
            }
            else
            {
                bool inboardIsX0 = arm == ModuleId.Docking;
                float inboardX = inboardIsX0 ? m.X0 : m.X1;
                float outboardX = inboardIsX0 ? m.X1 : m.X0;
                AddFaceWithOptionalGap(walls, inboardX, m.Y0, inboardX, m.Y1, horizontal: false, gap: open);
                walls.Add(new(outboardX, m.Y0, outboardX, m.Y1));
                walls.Add(new(m.X0, m.Y0, m.X1, m.Y0));
                walls.Add(new(m.X0, m.Y1, m.X1, m.Y1));

                float hubX = inboardIsX0 ? hub.X1 : hub.X0;
                walls.Add(new(hubX, -TubeHalfBore, inboardX, -TubeHalfBore));
                walls.Add(new(hubX, TubeHalfBore, inboardX, TubeHalfBore));
            }
        }

        return walls;
    }

    /// <summary>Lay one face, leaving a tube-bore gap at its centre when <paramref name="gap"/> is true.
    /// A severed tube gets a SOLID face — the tube is still there to look at, and still will not let you
    /// through, which is the whole point of "blocked, not absent".</summary>
    private static void AddFaceWithOptionalGap(
        List<SurfaceCollision.Segment> walls, float x0, float y0, float x1, float y1, bool horizontal, bool gap)
    {
        if (!gap)
        {
            walls.Add(new(x0, y0, x1, y1));
            return;
        }

        if (horizontal)
        {
            walls.Add(new(x0, y0, -TubeHalfBore, y0));
            walls.Add(new(TubeHalfBore, y0, x1, y1));
        }
        else
        {
            walls.Add(new(x0, y0, x0, -TubeHalfBore));
            walls.Add(new(x1, TubeHalfBore, x1, y1));
        }
    }

    // ── What a captain can reach on foot ─────────────────────────────────────────────────────────────────

    /// <summary>The modules a captain standing in <paramref name="from"/> can walk to, tubes considered.
    /// The hub is joined to every arm whose tube survived; arms are joined to nothing else, ever — an arm's
    /// only neighbour is the hub, which is exactly why severing two tubes strands two modules.</summary>
    public static IReadOnlyList<ModuleId> WalkableFrom(string stationId, ModuleId from)
    {
        ArgumentNullException.ThrowIfNull(stationId);

        if (from == ModuleId.Hub)
        {
            return [ModuleId.Hub, .. Arms.Where(a => TubeIntact(stationId, a))];
        }
        return TubeIntact(stationId, from) ? [from, ModuleId.Hub, .. Arms.Where(a => a != from && TubeIntact(stationId, a))] : [from];
    }

    /// <summary>The station's walk-groups: each set of modules mutually reachable on foot. More than one
    /// group is the premise of the whole feature — if this ever returns a single group, the shuttle hop has
    /// silently become decorative.</summary>
    public static IReadOnlyList<IReadOnlyList<ModuleId>> WalkGroups(string stationId)
    {
        var groups = new List<IReadOnlyList<ModuleId>>
        {
            WalkableFrom(stationId, ModuleId.Hub),
        };
        groups.AddRange(SeveredTubes(stationId).Select(a => (IReadOnlyList<ModuleId>)new[] { a }));
        return groups;
    }
}

using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Labs.Lab45;

/// <summary>
/// #852 · One real generated Hive floor, held exactly the way the game holds it — and that is the whole
/// design of this lab.
///
/// <para>The client keeps TWO views of the same stone and hands them to different callers
/// (<c>Map.Patrol.cs</c>, <c>Map.Surface.cs</c>):</para>
///
/// <list type="bullet">
/// <item><b><see cref="Legs"/></b> — <c>_deckPlan.CollisionField</c>, a <see cref="SurfaceCollision.WallIndex"/>.
/// #448's coarse grid. Everything that WALKS is stepped against this: the captain's boots, a Reever's
/// shamble, a guard's stride.</item>
/// <item><b><see cref="Sight"/></b> — <c>SightBlockers()</c>, a plain <see cref="List{T}"/> rebuilt every
/// frame from <c>CollisionSegments</c> plus whichever doors are shut. Everything that LOOKS is asked
/// against this: <c>PatrolBeat.SightingFor</c>, <c>Heard</c>, <c>Notices</c>.</item>
/// </list>
///
/// <para>A lab that flattened those into one segment list would measure a game nobody ships and would have
/// nothing to say about the owner's question. So both are built here, from the same
/// <see cref="HiveInterior.FloorDeck"/>, and every bench below says which one it is timing.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
internal sealed class World
{
    /// <summary>What the table calls this floor.</summary>
    internal string Name { get; }

    /// <summary>The site and level it really is, so nothing here is a made-up world.</summary>
    internal string Body { get; }

    internal int Level { get; }

    /// <summary>The indexed field the walkers collide against — <c>DeckPlan.CollisionField</c> itself.</summary>
    internal SurfaceCollision.WallIndex Legs { get; }

    /// <summary>The PLAIN list the sightline predicates are asked against — the shape <c>SightBlockers()</c>
    /// returns. Rebuilt by <see cref="RebuildSight"/> exactly as the client rebuilds it per frame.</summary>
    internal List<SurfaceCollision.Segment> Sight { get; }

    /// <summary>The raw segments, in generator order — what <see cref="Sight"/> is refilled from, and what
    /// Section B slices to measure the sweep against N walls.</summary>
    internal SurfaceCollision.Segment[] Segments { get; }

    /// <summary>The doors this floor draws shut whatever the captain does (locked ones). The client's own
    /// <c>SightBlockers</c> also shuts interlocked tube doors by the captain's distance; those are counted
    /// in <see cref="ShutDoors"/> and their handful of segments is the one thing this world approximates.</summary>
    internal int ShutDoors { get; }

    internal UndergroundComplex.FloorPlan Floor { get; }

    /// <summary>The round as this watch actually walks it — Core's own beat, never a made-up loop.</summary>
    internal IReadOnlyList<PatrolBeat.Stop> Beat { get; }

    internal SurfaceLayout.Field Envelope { get; }

    /// <summary>Everywhere the captain can stand, flooded from the lift — Lab 44's wash, reused here to
    /// place Reevers and captains on ground that really exists instead of on coordinates I liked.</summary>
    internal IReadOnlyList<DeckReachability.Point> Standable { get; }

    internal int SegmentCount => Segments.Length;

    private World(
        string name, string body, int level, SurfaceCollision.WallIndex legs,
        SurfaceCollision.Segment[] segments, int shutDoors,
        in UndergroundComplex.FloorPlan floor, IReadOnlyList<PatrolBeat.Stop> beat,
        in SurfaceLayout.Field envelope, IReadOnlyList<DeckReachability.Point> standable)
    {
        Name = name;
        Body = body;
        Level = level;
        Legs = legs;
        Segments = segments;
        ShutDoors = shutDoors;
        Floor = floor;
        Beat = beat;
        Envelope = envelope;
        Standable = standable;
        Sight = new List<SurfaceCollision.Segment>(segments.Length + shutDoors);
        RebuildSight();
    }

    /// <summary>
    /// The per-frame cost the client pays before a single sightline is asked: <c>SightBlockers()</c> clears
    /// its buffer and refills it from every collision segment on the floor, once a frame, every frame.
    /// Timed on its own in Section A because it is O(walls) whether or not anybody is looking at anything.
    /// </summary>
    internal void RebuildSight()
    {
        Sight.Clear();
        foreach (SurfaceCollision.Segment s in Segments)
        {
            Sight.Add(s);
        }
        for (int i = 0; i < ShutDoors; i++)
        {
            // The client appends one segment per shut door. Their geometry is already in Segments for a
            // LOCKED door (HiveInterior draws a wall behind it), so what matters to a benchmark is the
            // count, and the count is real.
            Sight.Add(Segments[i % Segments.Length]);
        }
    }

    /// <summary>Build one floor as the game builds it: Core's plan, the client's deck, both wall views, the
    /// watch's beat and the reachable wash.</summary>
    internal static World Build(string name, string body, int level, in SurfaceLayout.Field envelope, long watch = 0)
    {
        DeckPlan deck = HiveInterior.FloorDeck(body, level, envelope, 0, (_, _) => { }, [], watch);
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, envelope);

        int shut = 0;
        foreach (DeckPlan.Door d in deck.Doors)
        {
            if (d.Locked)
            {
                shut++;
            }
        }

        (double sx, double sy) = HiveInterior.SpawnOn(envelope);
        var bounds = (envelope.LeftX, envelope.BottomY, envelope.RightX, envelope.LandingBandY);
        var wash = new List<DeckReachability.Point>(
            DeckReachability.Reachable(
                new DeckReachability.Point(sx, sy), deck.CollisionField, DeckPlan.AvatarRadius, bounds));

        return new World(
            name, body, level, deck.CollisionField, deck.CollisionSegments, shut,
            in floor, PatrolBeat.BeatFor(body, level, watch, in floor, in envelope), in envelope, wash);
    }

    /// <summary>A deterministic spread of standable spots — every <c>Standable.Count / count</c>th cell of
    /// the wash, so N bodies are spread over the WHOLE floor rather than heaped in the lift lobby. Seeded
    /// only by the wash's own order, so two runs place the same bodies in the same places.</summary>
    internal (double X, double Y)[] Spread(int count)
    {
        var spots = new (double X, double Y)[count];
        int n = Standable.Count;
        for (int i = 0; i < count; i++)
        {
            DeckReachability.Point p = Standable[n == 0 ? 0 : (int)((long)i * n / count) % n];
            spots[i] = (p.X, p.Y);
        }
        return spots;
    }
}

using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #563 law 6 · THE CHUNK — what the captain is carrying of an unbounded ground, and the one thing in this
/// lane with a genuine performance trap in it.
///
/// <para>The trap, named in the decision comment: <c>DeckPlan.CollisionField</c> (#448) is a spatial index
/// over every wall on the ground, and it is REBUILT whenever the walls change. Streaming ground per STEP
/// would rebuild that index sixty times a second, on a game whose owner has already been bitten once by
/// surface geometry cost — the shuttle ride timing out twice, which is why the index exists at all.</para>
///
/// <para>So the ground moves in CHUNKS. The captain's own tile plus the eight around it are loaded
/// (<see cref="SurfaceTiles.ChunkRadius"/>); nothing at all happens until the captain walks off their tile,
/// and then exactly one rebuild happens. A straight walk across four tiles costs four rebuilds, not four
/// thousand — and <c>TheTreadmillTests</c> asserts precisely that equality, because "it feels fine" is not
/// a measurement and this is the number that would rot silently.</para>
///
/// <para>Pure and stateful in the only way a stream can be: it holds WHICH tiles are loaded, and every
/// answer it gives is a function of the captain's position and that. It never holds ground — the ground is
/// regenerated from its address whenever it is wanted (<see cref="SurfaceTiles.Ground"/>), which is what
/// makes eviction safe: what is recycled is the memory, never the content.</para>
/// </summary>
public sealed class SurfaceStream
{
    private readonly int _radius;
    private readonly List<SurfaceTiles.Address> _loaded = [];
    private bool _started;

    /// <summary>A stream carrying <paramref name="radius"/> tiles in each direction around the captain.</summary>
    public SurfaceStream(int radius = SurfaceTiles.ChunkRadius)
    {
        _radius = Math.Max(0, radius);
    }

    /// <summary>The tile the captain was last known to be standing on.</summary>
    public SurfaceTiles.Address Centre { get; private set; }

    /// <summary>Every tile currently carried, in <see cref="SurfaceTiles.Chunk"/> order.</summary>
    public IReadOnlyList<SurfaceTiles.Address> Loaded => _loaded;

    /// <summary>How many times the chunk has been rebuilt — which is how many times the caller has had to
    /// rebuild its collision index. THE number this whole design exists to keep small, so it is published
    /// rather than inferred.</summary>
    public int Rebuilds { get; private set; }

    /// <summary>How many tile boundaries the captain has crossed. Equal to <see cref="Rebuilds"/> by
    /// construction; published separately so a test can prove the equality rather than assume it.</summary>
    public int Crossings { get; private set; }

    /// <summary>The captain has moved. Returns true when the chunk changed and the caller must re-weld its
    /// ground — which happens on the first call, and thereafter only when a tile boundary is crossed.
    ///
    /// <para><paramref name="added"/> and <paramref name="evicted"/> are filled with the tiles that came and
    /// went, so a caller can append and drop rather than rebuild the world. Both are cleared first.</para></summary>
    public bool Step(
        double x, double y,
        List<SurfaceTiles.Address>? added = null,
        List<SurfaceTiles.Address>? evicted = null)
    {
        added?.Clear();
        evicted?.Clear();

        SurfaceTiles.Address now = SurfaceTiles.At(x, y);
        if (_started && now == Centre)
        {
            return false;
        }

        if (_started)
        {
            Crossings++;
        }
        _started = true;
        Centre = now;

        IReadOnlyList<SurfaceTiles.Address> want = SurfaceTiles.Chunk(now, _radius);
        if (evicted is not null)
        {
            foreach (SurfaceTiles.Address had in _loaded)
            {
                if (!Contains(want, had))
                {
                    evicted.Add(had);
                }
            }
        }
        if (added is not null)
        {
            foreach (SurfaceTiles.Address a in want)
            {
                if (!Contains(_loaded, a))
                {
                    added.Add(a);
                }
            }
        }

        _loaded.Clear();
        _loaded.AddRange(want);
        Rebuilds++;
        return true;
    }

    private static bool Contains(IReadOnlyList<SurfaceTiles.Address> list, SurfaceTiles.Address a)
    {
        foreach (SurfaceTiles.Address x in list)
        {
            if (x == a)
            {
                return true;
            }
        }
        return false;
    }
}

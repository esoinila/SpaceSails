namespace SpaceSails.Core.Interior;

/// <summary>
/// One stop on a station NPC's day (Wednesday plan §1, the owner's ruling verbatim: "People cannot
/// be static furniture. They change place and go behind locked doors or move"). A post is a place
/// the NPC can be found — a bar table, a back room — or an <see cref="Present"/>=false slot meaning
/// they've stepped out of reach (gone behind a locked door), so the deck can simply not draw them.
/// Deck units, matching <c>DeckPlan</c>; kept in Core so the schedule resolution is pure and tested.
/// </summary>
public readonly record struct NpcPost(string Location, double X, double Y, double FacingRad, bool Present);

/// <summary>
/// A deterministic, sim-time-driven rota for a walkable-interior NPC (Wednesday plan §3 PR-F, the
/// "people who won't sit still" half). No pathfinding — the NPC simply *is* at post <c>k</c> during
/// the k-th time slice, and swaps to the next on the slice boundary, cycling forever. A player who
/// spoke to them at the bar can come back a slice later to find them elsewhere, or gone.
///
/// Pure and deterministic (repo agreement §9): position is a function of sim time alone — no
/// <see cref="System.DateTime"/>, no RNG — so both interior renderers and the interaction gate read
/// the same answer for the same clock, and it is unit-testable without a browser.
/// </summary>
public sealed class NpcSchedule
{
    private readonly NpcPost[] _posts;

    /// <param name="npcId">Stable id / display name for the NPC this rota belongs to.</param>
    /// <param name="postDurationSeconds">Sim-seconds spent at each post before moving on (&gt; 0).</param>
    /// <param name="posts">The ordered stops, cycled in order (at least one).</param>
    public NpcSchedule(string npcId, double postDurationSeconds, IReadOnlyList<NpcPost> posts)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            throw new ArgumentException("An NPC schedule needs an id.", nameof(npcId));
        }
        if (!(postDurationSeconds > 0) || double.IsNaN(postDurationSeconds) || double.IsInfinity(postDurationSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(postDurationSeconds), postDurationSeconds,
                "Post duration must be a positive, finite number of sim-seconds.");
        }
        if (posts is null || posts.Count == 0)
        {
            throw new ArgumentException("An NPC schedule needs at least one post.", nameof(posts));
        }

        NpcId = npcId;
        PostDurationSeconds = postDurationSeconds;
        _posts = [.. posts];
    }

    public string NpcId { get; }
    public double PostDurationSeconds { get; }
    public int PostCount => _posts.Length;

    /// <summary>The post index active at <paramref name="simTime"/> — floor-divide by the slice
    /// length, wrapped into range. Correct for negative sim times too (defensive; the clock starts at
    /// 0), so the rota never indexes out of bounds.</summary>
    public int SliceIndex(double simTime)
    {
        long slice = (long)Math.Floor(simTime / PostDurationSeconds);
        int idx = (int)(slice % _posts.Length);
        return idx < 0 ? idx + _posts.Length : idx;
    }

    /// <summary>Where the NPC is at <paramref name="simTime"/>.</summary>
    public NpcPost Resolve(double simTime) => _posts[SliceIndex(simTime)];

    /// <summary>The post at a given index (0-based), for callers that bake a console at every stop.</summary>
    public NpcPost PostAt(int index) => _posts[((index % _posts.Length) + _posts.Length) % _posts.Length];
}

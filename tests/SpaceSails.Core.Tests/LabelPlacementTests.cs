namespace SpaceSails.Core.Tests;

using Rect = SpaceSails.Core.LabelPlacement.Rect;
using Candidate = SpaceSails.Core.LabelPlacement.Candidate;

/// <summary>
/// #402 — the nav-map label de-collision resolver. Pins the honest declutter rule: highest priority
/// wins, a colliding lower-priority label is nudged clear or culled (never drawn atop), and the
/// deflection-cluster acceptance bar — the THREAT ROCK and the DOCKED station stay legible while the
/// depots yield.
/// </summary>
public class LabelPlacementTests
{
    private static Rect R(double x, double y, double w = 80, double h = 14) => new(x, y, w, h);

    [Fact]
    public void NonOverlapping_AllDraw()
    {
        var cands = new[]
        {
            new Candidate(1, R(0, 0), Priority: 10, LineHeight: 14),
            new Candidate(2, R(200, 200), Priority: 10, LineHeight: 14),
        };

        var result = LabelPlacement.Resolve(cands);

        Assert.All(result, p => Assert.True(p.Draw));
    }

    [Fact]
    public void Overlap_HigherPriorityWins_LowerCulledWhenNoClearSpace()
    {
        // Two labels stacked on the exact same spot, nudging disabled — the lower must be culled.
        var cands = new[]
        {
            new Candidate(1, R(0, 0), Priority: 5, LineHeight: 0),
            new Candidate(2, R(0, 0), Priority: 9, LineHeight: 0),
        };

        var result = LabelPlacement.Resolve(cands);

        Assert.False(Draw(result, 1)); // low priority yields
        Assert.True(Draw(result, 2));  // high priority survives
    }

    [Fact]
    public void Overlap_YieldingLabelNudgesIntoClearSpace_WhenRoom()
    {
        // A collision that a single line-height nudge clears — the lower label survives, shifted down.
        var cands = new[]
        {
            new Candidate(1, R(0, 0), Priority: 9, LineHeight: 14),
            new Candidate(2, R(0, 4), Priority: 5, LineHeight: 14),
        };

        var result = LabelPlacement.Resolve(cands);

        Assert.True(Draw(result, 1));
        LabelPlacement.Placement low = Find(result, 2);
        Assert.True(low.Draw, "the yielding label should nudge clear rather than be culled");
        Assert.True(low.Rect.Y > 4, "it should have been nudged downward");
        Assert.False(low.Rect.Overlaps(Find(result, 1).Rect));
    }

    [Fact]
    public void ResultOrderMatchesInputOrder()
    {
        var cands = new[]
        {
            new Candidate(7, R(0, 0), Priority: 1, LineHeight: 0),
            new Candidate(8, R(500, 500), Priority: 1, LineHeight: 0),
        };

        var result = LabelPlacement.Resolve(cands);

        Assert.Equal(7, result[0].Key);
        Assert.Equal(8, result[1].Key);
    }

    [Fact]
    public void EqualPriorityCollision_FirstInInputWins()
    {
        var cands = new[]
        {
            new Candidate(1, R(0, 0), Priority: 5, LineHeight: 0),
            new Candidate(2, R(0, 0), Priority: 5, LineHeight: 0),
        };

        var result = LabelPlacement.Resolve(cands);

        Assert.True(Draw(result, 1));  // earlier input wins the tie
        Assert.False(Draw(result, 2));
    }

    [Fact]
    public void Empty_ReturnsEmpty()
    {
        Assert.Empty(LabelPlacement.Resolve(System.Array.Empty<Candidate>()));
    }

    // ── The acceptance bar: the deflection Saturn cluster. Four labels heaped on one spot; the
    //    threat rock and the docked station MUST both stay drawn, the depots yield. ──
    [Fact]
    public void DeflectionCluster_ThreatRockAndDockedStationBothLegible()
    {
        const int ThreatRock = 1, DockedStation = 2, RingsideDepot = 3, SaturnDepot = 4;
        // All four heaped in a tight cluster with real width — they genuinely fight for the same pixels.
        var cands = new[]
        {
            new Candidate(ThreatRock,    R(100, 100, 120), Priority: 1000, LineHeight: 14),
            new Candidate(DockedStation, R(104, 102, 120), Priority: 900,  LineHeight: 14),
            new Candidate(RingsideDepot, R(102, 104, 120), Priority: 100,  LineHeight: 14),
            new Candidate(SaturnDepot,   R(106, 106, 120), Priority: 100,  LineHeight: 14),
        };

        var result = LabelPlacement.Resolve(cands, maxNudgeSteps: 1);

        // The two that carry the money-moment are never dropped.
        Assert.True(Draw(result, ThreatRock), "the threat rock label must always be legible");
        Assert.True(Draw(result, DockedStation), "the docked station label must always be legible");

        // No two DRAWN labels overlap — the smear is gone.
        var drawn = System.Linq.Enumerable.ToList(
            System.Linq.Enumerable.Where(result, p => p.Draw));
        for (int i = 0; i < drawn.Count; i++)
        {
            for (int j = i + 1; j < drawn.Count; j++)
            {
                Assert.False(drawn[i].Rect.Overlaps(drawn[j].Rect),
                    $"drawn labels {drawn[i].Key} and {drawn[j].Key} still overlap");
            }
        }
    }

    // #402 follow-up (live smoke test of the Ringside cluster): the priority cull protected labels
    // ACROSS ranks, but the owner still saw the equal-rank DEPOT pack smear over itself. This pins the
    // stronger invariant the fix routes the depot labels through: within a single rank, colliding
    // labels still nudge clear or cull, so NO TWO DRAWN labels ever overlap — same rank included.
    [Fact]
    public void SameRankPack_NoTwoDrawnLabelsOverlap()
    {
        // Six same-rank depot labels heaped into one tight knot (the Enceladus/Ringside depot pack),
        // every one wide enough to genuinely fight for the same pixels.
        var cands = new[]
        {
            new Candidate(1, R(100, 100, 140), Priority: 100, LineHeight: 14),
            new Candidate(2, R(103, 101, 140), Priority: 100, LineHeight: 14),
            new Candidate(3, R(101, 103, 140), Priority: 100, LineHeight: 14),
            new Candidate(4, R(104, 102, 140), Priority: 100, LineHeight: 14),
            new Candidate(5, R(102, 104, 140), Priority: 100, LineHeight: 14),
            new Candidate(6, R(105, 105, 140), Priority: 100, LineHeight: 14),
        };

        var result = LabelPlacement.Resolve(cands);

        var drawn = System.Linq.Enumerable.ToList(
            System.Linq.Enumerable.Where(result, p => p.Draw));
        for (int i = 0; i < drawn.Count; i++)
        {
            for (int j = i + 1; j < drawn.Count; j++)
            {
                Assert.False(drawn[i].Rect.Overlaps(drawn[j].Rect),
                    $"same-rank drawn labels {drawn[i].Key} and {drawn[j].Key} still overlap");
            }
        }

        // The first-enqueued of the pack is the one kept (nearest-to-camera / first-in wins the tie) —
        // the later colliders stack down or stand down, never the leader.
        Assert.True(Draw(result, 1), "the first-enqueued same-rank label must survive");
    }

    private static LabelPlacement.Placement Find(IReadOnlyList<LabelPlacement.Placement> r, int key) =>
        System.Linq.Enumerable.First(r, p => p.Key == key);

    private static bool Draw(IReadOnlyList<LabelPlacement.Placement> r, int key) => Find(r, key).Draw;
}

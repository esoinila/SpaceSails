namespace SpaceSails.Core.Tests;

/// <summary>
/// #441 · The pack keeps its elbows out. Owner, live 2026-07-26: <i>"Reevers should not pile up"</i> …
/// <i>"multiple reevers on top of each other on same dot"</i> … <i>"reevers merging into a one blob."</i>
/// The acceptance criterion in his own words: a pack of N must read as N bodies on the ground, never as one.
/// </summary>
public class ReeverPackTests
{
    private const double Radius = 0.7;                    // DeckPlan.AvatarRadius — the body being spaced
    private static readonly SurfaceCollision.Segment[] NoWalls = [];

    private static double Gap((double X, double Y) a, (double X, double Y) b) =>
        System.Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    [Fact]
    public void TwoOnTheSameDot_LeaveIt_InDifferentDirections()
    {
        // The blob at its purest: two bodies on EXACTLY one point. There is no "away" to read off the
        // geometry here, so the fixed per-index bearing has to do the work — and it must actually separate
        // them rather than send both the same way.
        (double X, double Y)[] pack = [(10, -50), (10, -50)];
        ReeverPack.KeepApart(pack, NoWalls, Radius);

        Assert.True(Gap(pack[0], pack[1]) >= ReeverPack.PersonalSpace - 1e-6,
            $"two Old Ones still share a dot (gap {Gap(pack[0], pack[1]):0.###})");
    }

    [Fact]
    public void APackConvergingOnOnePoint_HoldsAsManyDotsAsThereAreBodies()
    {
        // The real shape of the bug: the encirclement bias gives the whole pack a SHARED aim point, so they
        // all arrive at the same spot. Every pair must end up readable as its own contact.
        (double X, double Y)[] pack =
        [
            (0, -60), (0.2, -60.1), (-0.1, -59.9), (0.05, -60.05), (-0.2, -60.2),
        ];
        ReeverPack.KeepApart(pack, NoWalls, Radius);

        for (int i = 0; i < pack.Length; i++)
        {
            for (int j = i + 1; j < pack.Length; j++)
            {
                Assert.True(Gap(pack[i], pack[j]) >= ReeverPack.PersonalSpace - 1e-6,
                    $"contacts {i} and {j} are still stacked (gap {Gap(pack[i], pack[j]):0.###})");
            }
        }
    }

    [Fact]
    public void TheEarlierIndexHoldsItsGround_SoTheOutcomeIsStable()
    {
        // Stability matters more than fairness here: if both bodies gave way, two Reevers would shove each
        // other back and forth every frame and the pack would jitter. The first one stands still.
        (double X, double Y)[] pack = [(5, -40), (5.3, -40)];
        ReeverPack.KeepApart(pack, NoWalls, Radius);

        Assert.Equal(5.0, pack[0].X, 6);
        Assert.Equal(-40.0, pack[0].Y, 6);
    }

    [Fact]
    public void Deterministic_SameCrowd_SameSpread_EveryTime()
    {
        // Determinism is law in Core, and a shove decided by chance would make a pack impossible to pin.
        (double X, double Y)[] a = [(0, -60), (0, -60), (0.1, -60), (-0.1, -60.1)];
        (double X, double Y)[] b = [(0, -60), (0, -60), (0.1, -60), (-0.1, -60.1)];
        ReeverPack.KeepApart(a, NoWalls, Radius);
        ReeverPack.KeepApart(b, NoWalls, Radius);
        Assert.Equal(a, b);
    }

    [Fact]
    public void CrowdingNeverShovesAHunterIntoStone()
    {
        // The #324 law survives the shove: a body pushed by its neighbours still bump-and-slides on the
        // walls. A Reever standing inside a monolith is a worse lie than two sharing a dot for one frame.
        SurfaceCollision.Segment[] slab = [new(0, -70, 0, -50)];   // a tall wall at x = 0
        (double X, double Y)[] pack = [(-1.0, -60), (-1.05, -60)]; // both pressed against its left face
        ReeverPack.KeepApart(pack, slab, Radius);

        foreach ((double X, double Y) r in pack)
        {
            Assert.False(SurfaceCollision.Blocked(r.X, r.Y, Radius, slab),
                $"the crowd pushed a Reever into the slab at ({r.X:0.###}, {r.Y:0.###})");
            Assert.True(r.X < 0, "…and nobody was squeezed THROUGH it");
        }
    }

    [Fact]
    public void AlreadySpacedOut_NobodyIsMoved()
    {
        // The push must be a correction, not a constant force — a pack already reading as separate contacts
        // is left exactly alone, so spacing never fights the chase.
        (double X, double Y)[] pack = [(0, -60), (6, -60), (-6, -66)];
        (double X, double Y)[] before = [.. pack];
        ReeverPack.KeepApart(pack, NoWalls, Radius);
        Assert.Equal(before, pack);
    }

    [Fact]
    public void ALonePairIsHandled_AndASingleContactIsANoOp()
    {
        (double X, double Y)[] one = [(3, -30)];
        ReeverPack.KeepApart(one, NoWalls, Radius);
        Assert.Equal((3.0, -30.0), one[0]);
    }
}

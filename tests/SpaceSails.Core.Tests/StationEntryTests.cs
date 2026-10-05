using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #653 slice 1 · THE TWO WAYS IN. A serviceable lock costs TIME and never a key (the 2026-09-13 ruling); a cut
/// face consumes the captain's own #537 <see cref="HullCutter"/> — and a face already cut stands open for free.
///
/// <para>Every band is asked of every access of dozens of seeded stations, so which arms cycle and which must
/// be cut is the world's variety and not one lucky seed.</para>
/// </summary>
public class StationEntryTests
{
    private static readonly string[] Stations =
        [.. Enumerable.Range(1, 40).Select(i => $"audit-station-{i}")];

    private static IEnumerable<StationWreck.Access> Accesses(StationWreck.AccessKind kind) =>
        Stations.SelectMany(StationWreck.Accesses).Where(a => a.Kind == kind);

    private static IReadOnlyList<Satchel.Item> WithTheRig(int cuts = HullCutter.CutsPerCell) =>
        [new Satchel.Item(Satchel.Kind.Tool, HullCutter.ItemId, cuts)];

    [Fact]
    public void TheSweepActuallyHasBothKindsOfAccess()
    {
        // Anti-vacuity: every assertion below is about a population that must exist.
        Assert.True(Accesses(StationWreck.AccessKind.ServiceableLock).Count() >= 40);
        Assert.True(Accesses(StationWreck.AccessKind.CutFace).Count() >= 20);
    }

    [Fact]
    public void ALockCostsTimeAndNothingButTime_WithOrWithoutACutter()
    {
        foreach (StationWreck.Access lockAccess in Accesses(StationWreck.AccessKind.ServiceableLock))
        {
            foreach (IReadOnlyList<Satchel.Item>? pocket in new IReadOnlyList<Satchel.Item>?[] { null, [], WithTheRig() })
            {
                StationEntry.Order order = StationEntry.Enter(lockAccess, null, pocket);

                Assert.True(order.Admitted);
                Assert.Equal(StationAboard.LockCycleSeconds, order.Seconds);
                Assert.False(order.CutMade);
                Assert.Equal(StationAboard.LockLine, order.Line);
                Assert.Equal(HullCutter.CutsLeft(pocket), HullCutter.CutsLeft(order.Carried));   // not one cut spent
                Assert.Equal((pocket ?? []).Count, order.Carried.Count);
            }
        }
    }

    [Fact]
    public void ACutFaceWithNoRigIsRefusedInTheCuttersOwnWordsAndNothingChanges()
    {
        foreach (StationWreck.Access face in Accesses(StationWreck.AccessKind.CutFace))
        {
            StationEntry.Order order = StationEntry.Enter(face, new HashSet<StationWreck.ModuleId>(), []);

            Assert.False(order.Admitted);
            Assert.Equal(0, order.Seconds);
            Assert.False(order.CutMade);
            Assert.Equal(HullCutter.NoCutterLine, order.Line);
            Assert.Empty(order.Carried);
        }
    }

    [Fact]
    public void ACutSpendsExactlyOneCutOffTheCellAndTheCutterOwnsTheClock()
    {
        foreach (StationWreck.Access face in Accesses(StationWreck.AccessKind.CutFace))
        {
            StationEntry.Order order = StationEntry.Enter(face, new HashSet<StationWreck.ModuleId>(), WithTheRig());

            Assert.True(order.Admitted);
            Assert.True(order.CutMade);
            Assert.Equal(HullCutter.CutSeconds, order.Seconds);
            Assert.Equal(StationAboard.CutFaceLine, order.Line);
            Assert.Equal(HullCutter.CutsPerCell - 1, HullCutter.CutsLeft(order.Carried));
            Assert.Equal(HullCutter.CutLine(HullCutter.CutsPerCell - 1), order.CellLine);
        }
    }

    [Fact]
    public void TheLastCutTakesTheRowWithIt()
    {
        StationWreck.Access face = Accesses(StationWreck.AccessKind.CutFace).First();
        StationEntry.Order order = StationEntry.Enter(face, null, WithTheRig(cuts: 1));

        Assert.True(order.CutMade);
        Assert.Equal(0, HullCutter.CutsLeft(order.Carried));
        Assert.Empty(order.Carried);
        Assert.Equal(HullCutter.LastCutLine, order.CellLine);
    }

    [Fact]
    public void AFaceAlreadyCutStandsOpenForFree_AndTheRigIsNotTouched()
    {
        foreach (StationWreck.Access face in Accesses(StationWreck.AccessKind.CutFace))
        {
            var cut = new HashSet<StationWreck.ModuleId> { face.Module };
            StationEntry.Order order = StationEntry.Enter(face, cut, WithTheRig());

            Assert.True(order.Admitted);
            Assert.False(order.CutMade);
            Assert.Equal(0, order.Seconds);
            Assert.Equal("", order.Line);
            Assert.Equal(HullCutter.CutsPerCell, HullCutter.CutsLeft(order.Carried));
        }
    }

    [Fact]
    public void AFaceCutOnAnotherModuleDoesNotOpenThisOne()
    {
        // A cut is permanent for THAT face. Cutting the Foundry must not stand the Reactor open.
        StationWreck.Access face = Accesses(StationWreck.AccessKind.CutFace).First(a => a.Module == StationWreck.ModuleId.Reactor);
        var elsewhere = new HashSet<StationWreck.ModuleId> { StationWreck.ModuleId.Foundry };

        StationEntry.Order order = StationEntry.Enter(face, elsewhere, []);
        Assert.False(order.Admitted);
    }

    [Fact]
    public void TheHubIsAlwaysALock_SoEveryBoardingPaysTheSameLockTime()
    {
        // The Boarding path charges StationAboard.LockCycleSeconds without asking an access, which is only
        // honest because Core guarantees the crew lock is serviceable. This pins the guarantee it leans on.
        foreach (string id in Stations)
        {
            Assert.Equal(StationWreck.AccessKind.ServiceableLock,
                StationAboard.AccessOf(id, StationWreck.ModuleId.Hub).Kind);
        }
    }

    [Fact]
    public void TheLockIsLongerThanACutBecausePatienceIsWhatItAsksFor()
    {
        Assert.True(StationAboard.LockCycleSeconds > HullCutter.CutSeconds);
    }
}

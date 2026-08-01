using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #591 · THE FAN IS A SURFACE INSTRUMENT. Owner: <i>"the motion tracker should be in underground
/// visibility mode when we are deeeeeeeep under surface"</i>.
///
/// <para>Everything about the device was written for standing on a moon under an open sky. Hundreds of
/// metres down inside poured walls it behaved exactly the same, which is nonsense in the plainest way. It
/// also buys the game something: depth already costs air and time, and a fan that hears less the deeper you
/// go gives depth a third cost — the one the player can actually name — without adding a single enemy.</para>
/// </summary>
public sealed class TheTrackerUndergroundTests
{
    private static double Surface => MotionTracker.DetectionRange(32.0);

    [Fact]
    public void OnTheREGOLITHNothingChangesAtAll()
    {
        // The whole feature has to be invisible above ground, or it is not an underground mode, it is a
        // nerf. Level 0 IS the surface, and the tracker there is the instrument it has always been.
        Assert.Equal(Surface, MotionTracker.UndergroundRange(Surface, 0), 9);
        Assert.Equal(Surface, MotionTracker.UndergroundRange(Surface, 3), 9);
    }

    [Fact]
    public void B1HearsEverythingTheSurfaceDoes()
    {
        // Deliberate, and it is a design call rather than an off-by-one. B1 is the floor that still holds
        // pressure, still has standing lights, and is the one place down there the building is pretending to
        // still work. The instrument agreeing with that pretence is the same lie the rest of the floor
        // tells — and the lie is what makes the dark under it land.
        Assert.Equal(Surface, MotionTracker.UndergroundRange(Surface, -1), 9);
    }

    [Fact]
    public void ItGetsSTRICTLYWorseWithEveryFloorDown()
    {
        // Monotonic, all the way to the performance guard. If any floor were quieter than the one below it,
        // a captain could descend to hear BETTER, which is the opposite of everything else about going down.
        double prev = MotionTracker.UndergroundRange(Surface, -1);
        for (int level = -2; level >= UndergroundComplex.DeepestPossibleFloor; level--)
        {
            double here = MotionTracker.UndergroundRange(Surface, level);
            Assert.True(here < prev,
                $"B{-level} hears {here:F2} du, B{-level - 1} heard {prev:F2} — depth got easier.");
            prev = here;
        }
    }

    [Fact]
    public void ItIsNEVERZeroHoweverDeepTheSiteGoes()
    {
        // A dead instrument is not frightening, it is broken: a captain who stops looking at the tracker has
        // simply lost a gauge, and the dread with it. It leans on a floor and never reaches it.
        //
        // Checked far past the generator's own performance guard because "depth is free" means the bound is
        // a guard and not a law — the curve has to still be sane the day a seed rolls past it.
        foreach (int level in new[] { -1, -4, -12, -24, UndergroundComplex.DeepestPossibleFloor, -100, -10000 })
        {
            double reach = MotionTracker.UndergroundRange(Surface, level);
            Assert.True(reach >= Surface * MotionTracker.UndergroundFloorFraction,
                $"B{-level}: {reach:F2} du is under the floor the curve is supposed to lean on.");
            Assert.True(reach > 0 && double.IsFinite(reach), $"B{-level}: {reach} is not a reach.");
        }
    }

    [Fact]
    public void ItIsACurveAndNotATable()
    {
        // "Depth is free" (#585) — DepthOf is unbounded by design, so anything with a bottom written into it
        // would be wrong the first time a seed rolled deeper than the author imagined. The halving constant
        // is the only knob, and it has to behave like one.
        double atTheTop = MotionTracker.UndergroundRange(Surface, -1);
        double oneHalvingDown = MotionTracker.UndergroundRange(
            Surface, -1 - (int)MotionTracker.UndergroundHalvingFloors);

        // Half of what was ABOVE the floor is gone, and the floor itself is untouched.
        double above = atTheTop - (Surface * MotionTracker.UndergroundFloorFraction);
        double stillAbove = oneHalvingDown - (Surface * MotionTracker.UndergroundFloorFraction);
        Assert.Equal(above / 2.0, stillAbove, 6);
    }

    [Fact]
    public void TheSameFloorAlwaysHearsTheSameDistance()
    {
        // Determinism is law in Core — and this one feeds a gauge the player is meant to learn to trust.
        for (int level = -1; level >= -20; level--)
        {
            Assert.Equal(
                MotionTracker.UndergroundRange(Surface, level),
                MotionTracker.UndergroundRange(Surface, level), 12);
        }
    }

    [Fact]
    public void AContactBeyondTheFANSREACHDoesNotPaintAtAll()
    {
        // Shortening the reach has to shorten what the fan SAYS, or it is a shortened fan in the numbers
        // only. On the surface the far contact still paints — pinned to the rim and faint, which is the
        // whole point of a long ear — so the default has to stay unbounded.
        var far = new[] { new MotionTracker.Entity(0, 40, 0, 1.0) };

        Assert.Single(MotionTracker.Sweep(0, 0, far));                 // surface: heard, faint, on the rim
        Assert.Single(MotionTracker.Sweep(0, 0, far, 50.0));           // inside a generous fan
        Assert.Empty(MotionTracker.Sweep(0, 0, far, 30.0));            // beyond a short one: silence

        // And the cut is on the fan's reach, not on the entity: a mover that steps inside is heard again.
        var near = new[] { new MotionTracker.Entity(0, 20, 0, 1.0) };
        Assert.Single(MotionTracker.Sweep(0, 0, near, 30.0));
    }

    [Fact]
    public void ADeepFloorIsMEASURABLYQuieterThanTheTopOne()
    {
        // The feeling, as a number, so a later tuning pass cannot quietly flatten it into nothing. At the
        // bottom of the deepest site the generator can build, the fan must have lost a real share of its
        // reach — not a rounding error somebody has to squint at.
        double top = MotionTracker.UndergroundRange(Surface, -1);
        double bottom = MotionTracker.UndergroundRange(Surface, UndergroundComplex.DeepestPossibleFloor);

        Assert.True(bottom < top * 0.55,
            $"the deepest floor still hears {bottom / top:P0} of what B1 does — depth costs nothing here.");
    }
}

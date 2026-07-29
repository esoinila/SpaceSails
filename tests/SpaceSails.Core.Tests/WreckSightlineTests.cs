namespace SpaceSails.Core.Tests;

/// <summary>
/// #488 · WHAT A GUN ABOARD HER CAN ACTUALLY SEE. Owner, twice, watching the airlock gun work: "I think
/// the gun was just shooting through wall to the hold" / "one reever was still shot through the wall."
///
/// <para>Settled off the wreck's REAL walls rather than off pixels — and the answer is that the engine is
/// right and the GEOMETRY IS DECEPTIVE. A spine doorway is six units wide (<c>DoorHalfWidth</c> × 2) and a
/// compartment is only six deep, so a shot that threads that gap at a shallow angle legitimately reaches
/// most of the room. On screen the round appears to pass through plate, because the doorway it went
/// through is metres away from both the gun and the target.</para>
///
/// <para>Reference frame for every case below: DEEP HOLD is x ∈ [−15, 0], y ∈ [−9, −3]; its spine wall runs
/// at y = −3 and is solid except the doorway gap x ∈ [−10, −4].</para>
/// </summary>
public class WreckSightlineTests
{
    private static IReadOnlyList<SurfaceCollision.Segment> Walls =>
        WreckLayout.Walls(Derelict.WreckCause.Infested);

    [Fact]
    public void StraightThroughTheBULKHEAD_IsBlocked()
    {
        // Gun in the spine at x = −2, target directly below it in the deep hold. The line crosses y = −3 at
        // x = −2, which is solid plate (the gap ends at −4). If this ever passes, the guns have x-ray.
        Assert.False(SurfaceCollision.HasLineOfSight(-2, 0, -2, -6, Walls));
    }

    [Fact]
    public void StraightThroughTheDOORWAY_IsClear()
    {
        // Same room, but through the gap. This MUST work or a gun could never cover a room you opened.
        Assert.True(SurfaceCollision.HasLineOfSight(-7, 0, -7, -6, Walls));
    }

    [Fact]
    public void AShallowAngleThroughTheGapReachesDeepIntoTheRoom_AndThatIsTheOneThatLOOKSWrong()
    {
        // THE EXPLANATION. A gun well down the spine at x = +2 can see a contact at (−8, −4): the sightline
        // crosses the wall at x ≈ −5.5, inside the gap. Nothing passes through steel — but the doorway is
        // nowhere near either end of the line, so on screen the round reads as going through the bulkhead.
        Assert.True(SurfaceCollision.HasLineOfSight(2, 0, -8, -4, Walls));

        // Move the target a little further from the gap and the same gun loses it — crossing at x ≈ −3.75,
        // which is plate. The cone through a six-unit doorway is real, and it is also bounded.
        Assert.False(SurfaceCollision.HasLineOfSight(2, 0, -9.5, -6, Walls));
    }

    [Fact]
    public void TheDoorwayIsSixUnitsWide_WhichIsWhyTheConeIsThatGenerous()
    {
        // Pinned so nobody "fixes" the look of it later by quietly narrowing the geometry — that number is
        // load-bearing for walkability (WreckLayoutTests) long before it is about sightlines.
        Assert.Equal(6f, WreckLayout.DoorHalfWidth * 2);
    }

    [Fact]
    public void TheSentryRuleIsTheSightRule()
    {
        // SentryBot.CanEngage is range AND line of sight on the same segments. If a refactor ever drops the
        // walls argument, this goes red instead of the guns silently gaining x-ray vision.
        Assert.False(SentryBot.CanEngage(-2, 0, -2, -6, Walls));
        Assert.True(SentryBot.CanEngage(-7, 0, -7, -6, Walls));
    }

    [Fact]
    public void ADoggedHatchStopsTheEyeAsWellAsTheBody()
    {
        // A compartment sealed from the board fills its doorway with a wall (WreckInterior does exactly
        // this), so the shot that was clear a moment ago is not. Sealing a room has to protect it from the
        // gun as well as from the captain, or the seal means nothing.
        var sealedDoorway = new List<SurfaceCollision.Segment>(Walls)
        {
            new(-10f, -WreckLayout.SpineHalfHeight, -4f, -WreckLayout.SpineHalfHeight),
        };

        Assert.False(SurfaceCollision.HasLineOfSight(-7, 0, -7, -6, sealedDoorway));
    }
    // ── The regression the owner asked to be closed "once and for all" ────────────────────────────────

    /// <summary>Every doorway on the ship, both rows: with the hatch SHUT, nothing in that compartment can
    /// be engaged from the corridor. Owner: "a reever was shot through a closed door … could we have a CI
    /// test to fix that for all cases?" — so it is every door, not the one that was noticed.</summary>
    public static TheoryData<float, bool> EveryDoorway()
    {
        var data = new TheoryData<float, bool>();
        foreach (float centre in WreckLayout.DoorCentres())
        {
            data.Add(centre, true);    // the compartment above the spine
            data.Add(centre, false);   // and the one below it
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryDoorway))]
    public void AShutHatchStopsEveryShotThroughIt(float doorCentre, bool top)
    {
        float wallY = top ? -WreckLayout.SpineHalfHeight : WreckLayout.SpineHalfHeight;
        double inside = top ? -6 : 6;

        // Open: the gun covers the room through the gap. This is the control — if it ever fails, the test
        // below is passing for the wrong reason (everything blocked is not the same as doors working).
        Assert.True(
            SentryBot.CanEngage(doorCentre, 0, doorCentre, inside, Walls),
            $"doorway at {doorCentre} ({(top ? "upper" : "lower")}): the gun cannot cover an OPEN doorway");

        // Shut: the client fills the gap with a wall segment (WreckInterior does exactly this for a dogged
        // or pressure-loaded hatch, and SightBlockers adds it for an auto-door the captain is not near).
        var shut = new List<SurfaceCollision.Segment>(Walls)
        {
            new(doorCentre - WreckLayout.DoorHalfWidth, wallY, doorCentre + WreckLayout.DoorHalfWidth, wallY),
        };

        Assert.False(
            SentryBot.CanEngage(doorCentre, 0, doorCentre, inside, shut),
            $"doorway at {doorCentre} ({(top ? "upper" : "lower")}): a gun shot through a SHUT hatch");

        // And not at an angle either — the shallow shot is the one that got through in play.
        Assert.False(
            SentryBot.CanEngage(doorCentre + 4, 0, doorCentre - 1, inside, shut),
            $"doorway at {doorCentre}: a shallow shot got through a shut hatch");
    }

    [Theory]
    [MemberData(nameof(EveryCauseData))]
    public void TheRuleHoldsOnEveryHull(Derelict.WreckCause cause)
    {
        // Damage geometry differs per cause (barricades, breaches, a peeled transom) and none of it may
        // punch a hole in this: a shut hatch is a shut hatch on every ship in the taxonomy.
        IReadOnlyList<SurfaceCollision.Segment> hull = WreckLayout.Walls(cause);

        foreach (float centre in WreckLayout.DoorCentres())
        {
            foreach (bool top in new[] { true, false })
            {
                float wallY = top ? -WreckLayout.SpineHalfHeight : WreckLayout.SpineHalfHeight;
                var shut = new List<SurfaceCollision.Segment>(hull)
                {
                    new(centre - WreckLayout.DoorHalfWidth, wallY, centre + WreckLayout.DoorHalfWidth, wallY),
                };

                Assert.False(
                    SentryBot.CanEngage(centre, 0, centre, top ? -6 : 6, shut),
                    $"{cause}: a gun shot through the shut hatch at {centre}");
            }
        }
    }

    public static TheoryData<Derelict.WreckCause> EveryCauseData()
    {
        var data = new TheoryData<Derelict.WreckCause>();
        foreach (Derelict.WreckCause c in System.Enum.GetValues<Derelict.WreckCause>())
        {
            data.Add(c);
        }
        return data;
    }
}

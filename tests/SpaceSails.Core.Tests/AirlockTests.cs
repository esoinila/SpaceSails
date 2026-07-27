namespace SpaceSails.Core.Tests;

/// <summary>
/// #462 · One door at a time. Owner, 2026-07-27: <i>"The idea is that only one door in a tube is open at a
/// time… think of airlock"</i> — <i>"both doors being open at the same time defeats the purpose."</i>
///
/// <para>These matter beyond tidiness: the captain stands AT the threshold whenever a chase reaches it, and
/// two independent auto-doors both opened for them — so the Old Ones piled up against a gap painted wide
/// open, which is the "the door that does not open for them is MISSING" report. The far end being shut is
/// the visible barrier they stop at, and the reason a tailgater is sealed in the tube with the built-in gun
/// (#461) rather than following you aboard.</para>
/// </summary>
public class AirlockTests
{
    private const double Radius = 3.0;

    [Fact]
    public void BothEndsAreNeverOpenAtOnce_ForAnyPositionInTheTube()
    {
        // The law, swept: walk the captain the whole length of a 10-unit tube and assert at every step that
        // the two ends never both stand open. This is the property the owner actually asked for.
        for (double pos = -2; pos <= 12; pos += 0.25)
        {
            double toInner = System.Math.Abs(pos - 0);   // ship end at 0
            double toOuter = System.Math.Abs(pos - 10);  // surface end at 10

            bool inner = Airlock.MayOpen(toInner, toOuter, Radius);
            bool outer = Airlock.MayOpen(toOuter, toInner, Radius);

            Assert.False(inner && outer, $"both ends open with the captain at {pos}");
        }
    }

    [Fact]
    public void TheNearEndOpens_AndTheFarEndStaysShut()
    {
        Assert.True(Airlock.MayOpen(0.5, 9.5, Radius));    // standing at the inner door
        Assert.False(Airlock.MayOpen(9.5, 0.5, Radius));   // the outer end, far away, stays shut
    }

    [Fact]
    public void ExactlyBetweenTheEnds_BOTH_StayShut_AnAirlockFailsClosed()
    {
        // A tie must not open both — an airlock that guesses wrong should fail closed, which is also the
        // safe answer for the thing this exists to stop.
        Assert.False(Airlock.MayOpen(2.0, 2.0, Radius));
    }

    [Fact]
    public void OutOfReach_NothingOpens_HoweverLonelyTheDoor()
    {
        Assert.False(Airlock.MayOpen(Radius + 0.1, double.PositiveInfinity, Radius));
        Assert.False(Airlock.MayOpen(99, 100, Radius));
    }

    [Fact]
    public void ADoorWithNoPartner_KeepsThePlainProximityRule()
    {
        // Every ordinary door in the game is un-grouped and must behave exactly as it always has.
        Assert.True(Airlock.MayOpen(1.0, double.PositiveInfinity, Radius));
        Assert.True(Airlock.MayOpen(Radius, double.PositiveInfinity, Radius));   // exactly at the rim
        Assert.False(Airlock.MayOpen(Radius + 0.01, double.PositiveInfinity, Radius));
    }

    [Fact]
    public void GarbageDistancesNeverHoldADoorOpen()
    {
        Assert.False(Airlock.MayOpen(double.NaN, 5, Radius));
        Assert.True(Airlock.MayOpen(1, double.NaN, Radius)); // an unreadable partner cannot veto the near door
    }
}

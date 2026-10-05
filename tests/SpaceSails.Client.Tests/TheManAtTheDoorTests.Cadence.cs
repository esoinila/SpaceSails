using System;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #653 slice 2 · THE GATE'S THREE TOLD-ONCE LINES, PINNED BY CADENCE BEFORE THE MECHANISM MOVES. Each is told
/// ONCE PER EXCURSION and again on the next one — asked of the shipping frame at the panel the line draws on
/// (the pulse slot, the card's caption), never of the flag that latches it, so the same facts hold whatever the
/// latch is made of. The within-excursion halves of the approach and the empty chair already live in
/// <c>Facts</c>; these add the other half of each cadence, "and a new excursion tells it again".
/// </summary>
public sealed partial class TheManAtTheDoorTests
{
    private static void FreeTheSlot(Pages.Map map) =>
        Set(map, "_pulse", Activator.CreateInstance(Get(map, "_pulse")!.GetType()));

    /// <summary>Run his round until the walk-away line reaches the slot (or fail), freeing the slot as time would.</summary>
    private static void RunTheRoundUntilTold(Pages.Map map)
    {
        FreeTheSlot(map);
        Pages.Map.ManAtTheDoor man = Man(map);
        StandAt(map, man.Post.X + 30, man.Post.Y);
        int limit = (int)((GateGuard.AtTheDoorSeconds + 60) / Dt);
        for (int f = 0; f < limit; f++)
        {
            if (man.Leg != Pages.Map.RoundLeg.AtTheDoor && Pulse(map) != GateGuard.RoundLine)
            {
                FreeTheSlot(map);
            }

            Frames(map, 1);
            if (Pulse(map) == GateGuard.RoundLine)
            {
                return;
            }
        }

        Assert.Fail("he walked off and the round was never told");
    }

    [Fact]
    public void TheApproachLineIsToldOncePerExcursionAndAgainOnTheNextOne()
    {
        (string body, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor();
        Assert.Equal(GateGuard.ApproachLine, Card(map)?.Caption);

        // A new excursion on the same ground: he is met afresh, and the first card of THAT trip carries the line.
        Invoke(map, "CloseViewObject");
        NewExcursionOnHisFloor(map, body, floor, GateGuard.Posting.Posted);
        (double x, double y) = Landing();
        StandAt(map, x, y);
        Frames(map, 3);
        Assert.Equal(GateGuard.CardTitle, Card(map)?.Label);
        Assert.Equal(GateGuard.ApproachLine, Card(map)?.Caption);
    }

    [Fact]
    public void TheEmptyChairIsToldOncePerExcursionAndAgainOnTheNextOne()
    {
        (string body, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor(GateGuard.Posting.Absent);
        Assert.Equal(GateGuard.AbsentLine, Pulse(map));

        // Same excursion, slot free, standing at the chair: never a second time.
        FreeTheSlot(map);
        Frames(map, 30);
        Assert.Null(Pulse(map));

        // A new excursion: the chair is told again on the first reach.
        NewExcursionOnHisFloor(map, body, floor, GateGuard.Posting.Absent);
        FreeTheSlot(map);
        (double x, double y) = Landing();
        StandAt(map, x, y);
        Frames(map, 3);
        Assert.Equal(GateGuard.AbsentLine, Pulse(map));
    }

    [Fact]
    public void TheRoundIsToldOncePerExcursionAndAgainOnTheNextOne()
    {
        (string body, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor(GateGuard.Posting.OnRound);
        Invoke(map, "CloseViewObject");
        RunTheRoundUntilTold(map);
        Assert.Equal(GateGuard.RoundLine, Pulse(map));

        // The rest of the round, slot freed every frame: never a second time this excursion.
        int rest = (int)((GateGuard.AtTheDoorSeconds + GateGuard.AwaySeconds) / Dt);
        for (int f = 0; f < rest; f += 20)
        {
            FreeTheSlot(map);
            Frames(map, 20);
            Assert.NotEqual(GateGuard.RoundLine, Pulse(map));
        }

        // A new excursion: his round is walked again, and told again.
        NewExcursionOnHisFloor(map, body, floor, GateGuard.Posting.OnRound);
        Invoke(map, "CloseViewObject");
        RunTheRoundUntilTold(map);
        Assert.Equal(GateGuard.RoundLine, Pulse(map));
    }
}

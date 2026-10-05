using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>#618 · The facts, on the bench next door. Each one names the revert that turned it RED.</summary>
public sealed partial class TheManAtTheDoorTests
{
    private static (double X, double Y) Landing() =>
        HiveInterior.SpawnOn(MoonSurface.ExpeditionField(), UndergroundComplex.ShaftKind.Cage);

    private static bool AnyRowGoesDown(Pages.Map map, int floor) => Rows(map).Any(r => r.Level < floor);

    private static string? Pulse(Pages.Map map) => (string?)Get(Get(map, "_pulse")!, "Message");

    private static IEnumerable<string> Book(Pages.Map map) =>
        ((IEnumerable<FieldNote>)Get(map, "_fieldNotes")!).Select(n => n.Text);

    /// <summary>Step out of the car beside him and let the frame run until his card is up.</summary>
    private static Pages.Map CardedAtTheDoor(GateGuard.Posting posting = GateGuard.Posting.Posted, bool talk = false)
    {
        (string body, int floor) = ASiteWithADoor();
        Pages.Map map = OnHisFloor(body, floor, posting, talk);
        (double x, double y) = Landing();
        StandAt(map, x, y);
        Frames(map, 3);
        return map;
    }

    /// <summary>
    /// <b>POSTED: HE KEEPS THE WAY DOWN, NEVER THE WAY UP, AND HIS CARD GOES UP WHEN YOU REACH HIM.</b> The
    /// panel's floors below are absent while he stands there — SURFACE and the floor itself stay — and the first
    /// card of the excursion carries the approach line, verbatim; a second approach carries none.
    ///
    /// <para><b>Proven RED</b> by making <c>PastTheManAtTheDoor</c> return the stops unfiltered:
    /// <c>Assert.False() Failure — a car on his floor still goes down while he is at the door</c>.</para>
    /// </summary>
    [Fact]
    public void APostedManKeepsTheWayDownAndHisCardGoesUpOnReach()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor();

        Assert.NotNull(Him(map));
        Assert.Equal(Pages.Map.Errand.GateKeeping, Him(map)!.For);
        Assert.False(AnyRowGoesDown(map, floor), "a car on his floor still goes down while he is at the door");
        Assert.Contains(Rows(map), r => r.Level == 0);

        Assert.Equal(GateGuard.CardTitle, Card(map)?.Label);
        Assert.Equal(GateGuard.ApproachLine, Card(map)?.Caption);
        Assert.True(Waits(map));

        // Walk off, shut the card, come back: a second approach, a card with no line on it.
        Invoke(map, "CloseViewObject");
        Pages.Map.ManAtTheDoor man = Man(map);
        StandAt(map, man.Post.X + GateGuard.LeftHimDu + 4, man.Post.Y);
        Frames(map, 2);
        (double x, double y) = Landing();
        StandAt(map, x, y);
        Frames(map, 2);
        Assert.Equal(GateGuard.CardTitle, Card(map)?.Label);
        Assert.True(string.IsNullOrEmpty(Card(map)?.Caption), "the approach line was told twice in one excursion");
    }

    /// <summary>
    /// <b>THE WORD THAT FAILS IS REMEMBERED, AND THE MOVE IS ABSENT — NOT GREYED.</b> Failure puts his line on the
    /// card, the face tag on the register, and TALK YOUR WAY IN is not in the move list afterwards (the list is
    /// the only thing the card draws, and it draws no disabled control). The way down stays shut.
    ///
    /// <para><b>Proven RED</b> by passing <c>faceRemembered: false</c> in <c>TheManAtTheDoorsMoves</c>:
    /// <c>Assert.DoesNotContain() Failure — gate:talk found</c>.</para>
    /// </summary>
    [Fact]
    public void AFailedWordIsRememberedAndTheMoveIsAbsent()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor(talk: false);
        Assert.Contains(Moves(map), m => m.Id == GateGuard.TalkMove);

        Press(map, GateGuard.TalkMove);

        Assert.Equal(GateGuard.TalkFailedLine, Card(map)?.Outcome);
        Assert.Contains(GateGuard.FaceTag(Man(map).Ground), Register(map));
        Assert.DoesNotContain(Moves(map), m => m.Id == GateGuard.TalkMove);
        Assert.Contains(Moves(map), m => m.Id == GateGuard.NoiseMove);
        Assert.False(AnyRowGoesDown(map, floor));
    }

    /// <summary>
    /// <b>THE WORD THAT WORKS, AND THE BADGE THAT PASSES, EACH OPEN THE WAY DOWN — AND THE DOOR'S OWN ROWS COME
    /// BACK EXACTLY AS THE BUILDING DRAWS THEM.</b> The badge move exists only with a pass the ladder reads as
    /// good here; pressed, it says his line and he is past.
    ///
    /// <para><b>Proven RED</b> by dropping <c>man.Passed = true</c> from the ShowMove arm:
    /// <c>Assert.True() Failure — the badge passed and the way down is still his</c>.</para>
    /// </summary>
    [Fact]
    public void TheWordThatWorksAndThePassThatPassesOpenTheWayDown()
    {
        (string body, int floor) = ASiteWithADoor();

        Pages.Map talked = CardedAtTheDoor(talk: true);
        Assert.DoesNotContain(Moves(talked), m => m.Id == GateGuard.ShowMove);
        Press(talked, GateGuard.TalkMove);
        Assert.Equal(GateGuard.TalkWorkedLine, Card(talked)?.Outcome);
        Assert.True(AnyRowGoesDown(talked, floor), "the word worked and the way down is still his");
        Assert.DoesNotContain(GateGuard.FaceTag(Man(talked).Ground), Register(talked));

        Pages.Map badged = CardedAtTheDoor();
        Satchel.Item pass = PatrolBeat.Badge(body);
        Assert.Equal(WalletChoice.Outcome.Worked, WalletChoice.WhatHappens(body, floor, Watch, pass));
        Set(badged, "_satchel", new List<Satchel.Item> { pass });
        Assert.Contains(Moves(badged), m => m.Id == GateGuard.ShowMove && m.Label == GuardStop.ShowLabel);
        Press(badged, GateGuard.ShowMove);
        Assert.Equal(GateGuard.BadgeLine, Card(badged)?.Outcome);
        Assert.True(AnyRowGoesDown(badged, floor), "the badge passed and the way down is still his");

        // …and past him, the panel is the building's own panel, row for row.
        IReadOnlyList<UndergroundComplex.LiftStop> own = UndergroundComplex.LiftPanel(
            body, floor, UndergroundComplex.ShaftKind.Cage, [], new List<Satchel.Item> { pass });
        Assert.Equal(own.Select(r => r.Name), Rows(badged).Select(r => r.Name));
    }

    /// <summary>
    /// <b>MAKE A NOISE: HE COMES OFF THE WALL AND FOLLOWS, AND THE TIDE THAT IS UP TAKES HIM.</b> The noise line
    /// on the card; he is on his feet behind the captain (the way down still his); the captain rides up under the
    /// lid and he is at the mouth of the tube; one tide Old One on the field and he is gone — the line said once,
    /// from the surface — and on the way back down the door is nobody's for the rest of the excursion.
    ///
    /// <para><b>Proven RED</b> by making <c>TheTideAnswers</c> ignore <c>aTideOneIsUp</c>:
    /// <c>Assert.True() Failure — the tide was up and he was not taken</c>.</para>
    /// </summary>
    [Fact]
    public void TheNoisePullsHimOutAndATideThatIsUpTakesHim()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor();
        Press(map, GateGuard.NoiseMove);
        Assert.Equal(GateGuard.NoiseLine, Card(map)?.Outcome);
        Assert.True(Man(map).Following);
        Invoke(map, "CloseViewObject");

        // Walk off down the corridor: he comes after you, and the way down is still his.
        Pages.Map.ManAtTheDoor man = Man(map);
        StandAt(map, man.Post.X + 20, man.Post.Y);
        Frames(map, 240);
        Assert.Equal(Pages.Map.Errand.GateFollowing, Him(map)?.For);
        Assert.True(Distance(Him(map)!, man.Post.X, man.Post.Y) > GateGuard.MannedWithinDu,
            "he came off the wall and did not move");
        Assert.False(AnyRowGoesDown(map, floor));

        Invoke(map, "RideTheLiftTo", Ex(map), 0, null, false);
        Assert.True(Man(map).AtTheMouthFor >= 0, "he did not follow you into the light");
        Invoke(map, "AdvanceTheManAtTheDoor", Dt);
        Assert.Equal(Pages.Map.Errand.GateAtTheMouth, Him(map)?.For);

        AddATideOneTo(map);
        Invoke(map, "AdvanceTheManAtTheDoor", Dt);
        Assert.True(Man(map).Taken, "the tide was up and he was not taken");
        Assert.Null(Him(map));
        Assert.Equal(GateGuard.TideTakesHimLine, Pulse(map));

        Invoke(map, "RideTheLiftTo", Ex(map), floor, null, false);
        Frames(map, 3);
        Assert.Null(Him(map));
        Assert.True(AnyRowGoesDown(map, floor), "the tide had him and the door is still kept");
    }

    /// <summary>
    /// <b>…AND THE TIDE THAT IS DOWN SENDS HIM BACK.</b> Nothing on the field for the mouth's whole time: his
    /// line, once, and on the way back down he is at his door again with the way down his.
    ///
    /// <para><b>Proven RED</b> by making <c>TheTideAnswers</c> return <c>Taken</c> once the time runs out:
    /// <c>Assert.False() Failure — the tide was down and he was taken</c>.</para>
    /// </summary>
    [Fact]
    public void ATideThatIsDownSendsHimBackToHisDoor()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor();
        Press(map, GateGuard.NoiseMove);
        Invoke(map, "CloseViewObject");

        Invoke(map, "RideTheLiftTo", Ex(map), 0, null, false);
        int frames = (int)Math.Ceiling(GateGuard.AtTheMouthSeconds / Dt) + 10;
        for (int i = 0; i < frames; i++)
        {
            Invoke(map, "AdvanceTheManAtTheDoor", Dt);
        }

        Assert.False(Man(map).Taken, "the tide was down and he was taken");
        Assert.True(Man(map).AtTheMouthFor < 0);
        Assert.Null(Him(map));
        Assert.Equal(GateGuard.TideDownLine, Pulse(map));

        Invoke(map, "RideTheLiftTo", Ex(map), floor, null, false);
        Frames(map, 3);
        Assert.Equal(Pages.Map.Errand.GateKeeping, Him(map)?.For);
        Assert.False(AnyRowGoesDown(map, floor), "he went back and the door is not his again");
    }

    /// <summary>
    /// <b>THE BOOK'S LINE, ON THE FIRST PASS BY ANY ROUTE, ONCE PER GROUND.</b> A car down from his floor while
    /// he does not keep it files the line under 📍 and tags the ground; a second pass — on a second excursion to
    /// the same ground — files nothing.
    ///
    /// <para><b>Proven RED</b> by filing without asking the register (<c>_roomsTurnedOver.Add</c> ignored):
    /// <c>Assert.Single() Failure — 2 entries</c>.</para>
    /// </summary>
    [Fact]
    public void ThePassIsInTheBookOncePerGround()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor(GateGuard.Posting.Absent);
        int below = Rows(map).First(r => r.Level < floor).Level;

        Invoke(map, "RideTheLiftTo", Ex(map), below, null, false);
        Invoke(map, "RideTheLiftTo", Ex(map), floor, null, false);
        Invoke(map, "RideTheLiftTo", Ex(map), below, null, false);

        Assert.Single(Book(map), t => t == GateGuard.BookLine);
        Assert.Contains(GateGuard.PastTag(Man(map).Ground), Register(map));

        // A second excursion to the same ground, the same register: nothing more is filed.
        Ex(map).Gate = null;
        Invoke(map, "RideTheLiftTo", Ex(map), floor, null, false);
        Invoke(map, "RideTheLiftTo", Ex(map), below, null, false);
        Assert.Single(Book(map), t => t == GateGuard.BookLine);
    }

    /// <summary>
    /// <b>ABSENT: THE DOOR IS A DOOR.</b> Nobody on his feet, the panel's own way down, the chair drawn, and the
    /// line told once on reaching it.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>GateAbsent</c> told-once latch: the second reach re-tells the line and
    /// <c>Assert.Null() Failure</c> on the pulse after it was cleared.</para>
    /// </summary>
    [Fact]
    public void AnAbsentWindowIsAChairAndADoor()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor(GateGuard.Posting.Absent);
        Assert.Null(Him(map));
        Assert.True(AnyRowGoesDown(map, floor));
        Assert.Null(Card(map));
        Assert.Equal(GateGuard.AbsentLine, Pulse(map));

        DeckPlan plan = (DeckPlan)Get(map, "_deckPlan")!;
        Assert.Contains(plan.RoomLabels, l => l.Text == "🪑🧥");

        Set(map, "_pulse", Activator.CreateInstance(Get(map, "_pulse")!.GetType()));
        Frames(map, 3);
        Assert.Null(Pulse(map));
    }

    /// <summary>
    /// <b>ON HIS ROUND: THE DOOR IS HIS WHEN HE IS AT IT, AND NOBODY'S WHEN HE IS NOT.</b> A minute and a half at
    /// the door, then the walk to the canteen — told once — and the way down opens; six minutes after he set off
    /// he is back and it is his again.
    ///
    /// <para><b>Proven RED</b> by making <c>HeIsAtTheDoor</c> answer true wherever he stands:
    /// <c>Assert.True() Failure — he is at the canteen and the door is still kept</c>.</para>
    /// </summary>
    [Fact]
    public void OnHisRoundTheDoorIsHisOnlyWhileHeIsAtIt()
    {
        (string _, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor(GateGuard.Posting.OnRound);
        Invoke(map, "CloseViewObject");
        Set(map, "_pulse", Activator.CreateInstance(Get(map, "_pulse")!.GetType()));   // the slot has come free
        Pages.Map.ManAtTheDoor man = Man(map);
        StandAt(map, man.Post.X + 30, man.Post.Y);
        Assert.False(AnyRowGoesDown(map, floor));

        int f = 0;
        for (; f < (int)((GateGuard.AtTheDoorSeconds + 60) / Dt) && !Ex(map).Told.Has(ToldOnce.GateRound); f++)
        {
            if (man.Leg != Pages.Map.RoundLeg.AtTheDoor)
            {
                // The bench hands the pulse no clock, so a slot never expires on its own: free it, as time would.
                Set(map, "_pulse", Activator.CreateInstance(Get(map, "_pulse")!.GetType()));
            }

            Frames(map, 1);
        }

        Assert.True(Ex(map).Told.Has(ToldOnce.GateRound), "he walked off and the round was never told");
        Assert.Equal(GateGuard.RoundLine, Pulse(map));
        Frames(map, (int)((GateGuard.AtTheDoorSeconds + 60) / Dt) - f);
        Assert.NotEqual(Pages.Map.RoundLeg.AtTheDoor, man.Leg);
        Assert.True(AnyRowGoesDown(map, floor), "he is at the canteen and the door is still kept");

        Frames(map, (int)((GateGuard.AwaySeconds - 50) / Dt));   // he set off at 90 s; back by 90 + 360 = 450 s; now 460 s
        Assert.Equal(Pages.Map.RoundLeg.AtTheDoor, man.Leg);
        Assert.False(AnyRowGoesDown(map, floor), "he came back and the door is not his");
    }

    /// <summary>
    /// <b>OFF HIS FLOOR HE IS NOWHERE.</b> On any other floor of the building no figure plated Gate is in the
    /// band and the panel is the building's own, row for row.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>ex.Floor != man.Floor</c> clause from the frame's early
    /// removal: <c>Assert.Null() Failure — Walker</c>.</para>
    /// </summary>
    [Fact]
    public void OffHisFloorHeIsNowhere()
    {
        (string body, int floor) = ASiteWithADoor();
        Pages.Map map = CardedAtTheDoor();
        Invoke(map, "CloseViewObject");
        Man(map).Passed = true;
        int below = Rows(map).First(r => r.Level < floor).Level;
        Invoke(map, "RideTheLiftTo", Ex(map), below, null, false);
        Frames(map, 3);
        Assert.Null(Him(map));
        Assert.Equal(
            UndergroundComplex.LiftPanel(body, below, UndergroundComplex.ShaftKind.Cage, [], []).Select(r => r.Name),
            Rows(map).Select(r => r.Name));
    }

    /// <summary>
    /// <b>HE NEVER SAYS AN UNAUTHORED WORD.</b> Every sentence his card and the pulse carried through every
    /// route above is one of <see cref="GateGuard.AllProse"/>, and his plate is his plate.
    ///
    /// <para><b>Proven RED</b> by appending a word to the approach caption in <c>HisCardGoesUp</c>:
    /// <c>Assert.Contains() Failure</c>.</para>
    /// </summary>
    [Fact]
    public void HeNeverSaysAnUnauthoredWord()
    {
        HashSet<string> canon = [.. GateGuard.AllProse()];
        var said = new List<string?>();

        Pages.Map map = CardedAtTheDoor();
        said.Add(Card(map)?.Caption);
        Press(map, GateGuard.NoiseMove);
        said.Add(Card(map)?.Outcome);
        Assert.Equal(GateGuard.Plate, Him(map)?.Walk.Plate);

        Pages.Map talk = CardedAtTheDoor();
        Press(talk, GateGuard.TalkMove);
        said.Add(Card(talk)?.Outcome);

        Pages.Map absent = CardedAtTheDoor(GateGuard.Posting.Absent);
        said.Add(Pulse(absent));

        Assert.All(said, s => Assert.Contains(s ?? "∅", canon));
    }

    private static double Distance(Pages.Map.Walker w, double x, double y) =>
        Math.Sqrt(((w.Walk.X - x) * (w.Walk.X - x)) + ((w.Walk.Y - y) * (w.Walk.Y - y)));

    private static void AddATideOneTo(Pages.Map map)
    {
        Type reeverType = typeof(Pages.Map).GetNestedType("Reever", Hidden)!;
        object r = Activator.CreateInstance(reeverType, nonPublic: true)!;
        reeverType.GetField("Tide")!.SetValue(r, true);
        reeverType.GetField("X")!.SetValue(r, (double)Get(map, "_avatarX")! + 40);
        reeverType.GetField("Y")!.SetValue(r, (double)Get(map, "_avatarY")! + 40);
        ((System.Collections.IList)Get(map, "_reevers")!).Add(r);
    }
}

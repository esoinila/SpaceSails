using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;
using Xunit;
using static SpaceSails.Client.Tests.CastawayBench;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1199 · <b>THE NEWSPAPER WITH EYE HOLES.</b>
///
/// <para><b>Owner, live on the T (2026-09-19):</b> <i>"wow the tailed one disappeared :-D … I really like the
/// tables there… I think the tailed one should go to the observation deck even if I am there before they
/// arrive. It is the classic sit at a café with a newspaper with eye holes gumshoe cliché :-D"</i> — then,
/// the same afternoon: <i>"They should act normal even if I tail from ahead."</i> and <i>"another vending
/// machine or something else that only MOMENTARILY blocks the view."</i></para>
///
/// <h3>The rule these laws are written against</h3>
///
/// <para>There is no hold at the throat any more and no leg back out of one. He walks his errand into the
/// hat and stands at the rail looking out, and the room asks once a look
/// (<see cref="ReeverObservation.LookIntervalSeconds"/>) whether he is still being watched — SIGHT of him
/// while he is walking, <see cref="ObservationWalk.TheCaptainHasEyesOnHim"/> once he has stopped. The first
/// look the answer is no, he is off the floor. If it never is, he finishes the view and walks back out past
/// the captain with <b>nothing spent</b>.</para>
///
/// <h3>RED on the shipped tree</h3>
///
/// <para>Every law below was RED before the rule changed, and each for its own reason. The seated ones,
/// because #1245's throat held him for a captain inside <c>TooCloseToGoInDu</c> and sent him back out with
/// the beat SPENT and no card — a captain sitting at the north table is 4.6 du from the rail, well inside
/// two paces, so sitting down was the one thing that guaranteed you never saw the vanish. The eyepiece one,
/// because a card in front of the world meant nothing to the old rule. The stare-through one, because the
/// old ending spent the beat and this one must not. And <see cref="TheThirdMachineIsSomethingToWalkBehind"/>
/// is measured off the deck the machine is on: with no island there, the south table sees him all the way to
/// the rail.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheNewspaperWithEyeHolesTests
{
    private const string CanvasId = "newspaper-canvas";

    /// <summary>The hat’s two tables in the room’s own order — north first, exactly as
    /// <c>HavenInterior.GalleryTops</c> publishes them. Named rather than typed twice, because which
    /// table is which is the whole difference between the two seated laws below.</summary>
    private const int NorthTable = 0;

    private const int SouthTable = 1;

    // ── 1 · THE PAPER ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>A CAPTAIN SITTING AT THE NORTH TABLE WATCHES HIM ALL THE WAY TO THE RAIL — AND THEN LOOKS
    /// UP AND HE IS NOT THERE.</b> The owner's beat, whole: he comes in past a customer with a paper (no hold
    /// anywhere on the route), he crosses the hat in plain view, he stands at the rail, and the next look
    /// over the paper is the one he is gone on.
    ///
    /// <para>The north table is the good seat — the one with the clean line to the rail — which is why the
    /// paper is the only thing that can lose him from it. Driven through the seat system's own verb
    /// (<c>TryTakeBarTop</c>, the eighth site), so what the walk reads is the seat's own seated state.</para>
    ///
    /// <para><b>RED on the shipped tree:</b> #1245's throat held him for a captain inside two paces and sent
    /// him back out with the beat spent and no card — and this chair is four and a half du from the rail.</para>
    /// </summary>
    [Fact]
    public void SeatedAtTheNorthTableHeReachesTheRailAndGoesBehindThePaper()
    {
        var walk = new ObservationWalkBench(CanvasId + "-north");
        walk.SitTheCaptainAt(NorthTable);

        walk.RunTheWalk(seconds: 120, until: () => walk.HeIsAtTheRail);
        Assert.True(
            walk.HeIsAtTheRail,
            "he never reached the rail with a captain sitting at the north table — either something held him "
            + "(which the owner ruled out) or the good seat cannot see the rail.");

        int looks = 0;
        while (looks < 3 && walk.HeIsOnTheFloor)
        {
            walk.LetOneLookPass();
            walk.OneFrame();
            looks++;
        }

        Assert.False(walk.HeIsOnTheFloor, $"he was still at the rail after {looks} looks behind the paper.");
        Assert.True(walk.TheGalleryIsEmpty, "somebody is still standing in the gallery.");
        TheCardComesWhereHeIsSitting(walk);
    }

    /// <summary>
    /// #1199 · <b>AND FROM THE SOUTH TABLE HE GOES BEHIND THE THIRD MACHINE AND DOES NOT COME OUT.</b> Owner:
    /// <i>"another vending machine or something else that only MOMENTARILY blocks the view."</i> The island
    /// stands square in this table's line to the rail, so the man is lost on his LEGS rather than at the rail
    /// — the same one rule, a different one of the four ways it can be answered.
    ///
    /// <para>The captain is sitting, so the card comes to the table exactly as it does at the north one.</para>
    /// </summary>
    [Fact]
    public void SeatedAtTheSouthTableHeGoesBehindTheIslandMachine()
    {
        var walk = new ObservationWalkBench(CanvasId + "-south");
        walk.SitTheCaptainAt(SouthTable);

        walk.RunTheWalk(seconds: 120, until: () => !walk.HeIsOnTheFloor);

        Assert.False(walk.HeIsOnTheFloor, "he crossed the whole hat in full view of the south table.");
        Assert.False(walk.HeIsAtTheRail, "he reached the rail — nothing screened him on the way.");
        Assert.True(walk.TheGalleryIsEmpty);
        TheCardComesWhereHeIsSitting(walk);
    }

    /// <summary>#1199 · The half both seated laws end on: the wait runs from the vanish, the card comes where
    /// the captain is SITTING (the reach is the gallery for a captain who was at a table), the note is filed
    /// once, and asking again files nothing twice.</summary>
    private static void TheCardComesWhereHeIsSitting(ObservationWalkBench walk)
    {
        Assert.False(double.IsNaN(walk.GoneSince), "the wait never started, so the card is unreachable.");

        walk.LetTheWaitPass();
        walk.AskForTheCard();

        Assert.True(walk.TheBeatIsSpent, "the walk was empty and the beat was never spent.");
        Assert.True(walk.ACardWasRaised, "nobody was told the walk was empty.");
        Assert.Equal(1, walk.TimesFiled(ObservationWalk.NoteLine(walk.Person)));

        walk.AskForTheCard();
        Assert.Equal(1, walk.TimesFiled(ObservationWalk.NoteLine(walk.Person)));
    }

    /// <summary>#1199 · …and the relaxed reach is exactly that and no wider: a captain who was STANDING when
    /// the man went still has to walk out to the rail. The anti-vacuous half — without it, "the card comes in
    /// the gallery" would be a rule that gave the beat away from the doorway.</summary>
    [Fact]
    public void ButACaptainWhoWasSTANDINGStillHasToWalkOutToTheRail()
    {
        var walk = new ObservationWalkBench(CanvasId + "-standing");

        // Followed at a distance down the concourse, then held at the mouth while he walks the tube — which
        // is how a tail actually arrives here, and the one case #1245 shipped.
        walk.WalkHimToTheTube();
        Assert.True(walk.HeIsInTheTube, "he never got into the tube at all.");
        walk.StandTheCaptainAtTheMouth();
        walk.RunTheWalk(seconds: 120, until: () => !walk.HeIsOnTheFloor);
        Assert.False(walk.HeIsOnTheFloor, "he never went: the tube's corner gives the line break by itself.");

        walk.LetTheWaitPass();

        // In the walk, in the gallery, and not at the rail.
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Port)!.Value;
        walk.StandTheCaptainAt(throat.X - 0.1, throat.Y);
        walk.AskForTheCard();
        Assert.False(walk.ACardWasRaised, "the card came to a captain who never walked out to the rail.");

        walk.StandTheCaptainAtTheRail();
        walk.AskForTheCard();
        Assert.True(walk.ACardWasRaised, "the card never came, even at the rail.");
    }

    // ── 2 · THE EYEPIECE ────────────────────────────────────────────────────────────────────────────────

    /// <summary>#1199 · <b>E ON THE BINOCULARS IS EYES AWAY.</b> A card standing in front of the world is the
    /// captain not looking at the room, whatever his feet are doing — owner: <i>"pay-coin-to-use binoculars…
    /// use with E"</i>. Asked of #1052's own census (<c>AScrimIsUp</c>), so every card in the game counts and
    /// no second list has to be kept in step.
    ///
    /// <para>The captain stands at the north table's chair — standing, not sitting, so there is no paper —
    /// which is the one place in the hat with a clean line to both the throat and the rail.</para></summary>
    [Fact]
    public void WithACardInFrontOfHimHeIsGoneOnThatLook()
    {
        var walk = new ObservationWalkBench(CanvasId + "-glasses");
        StandHimAtTheGoodSeat(walk);

        walk.RunTheWalk(seconds: 120, until: () => walk.HeIsAtTheRail);
        Assert.True(walk.HeIsAtTheRail, "he never reached the rail with a captain standing at the table.");

        walk.LetOneLookPass();
        walk.OneFrame();
        Assert.True(walk.HeIsOnTheFloor, "he went while the captain was looking straight at him.");

        // …and then the captain puts his eyes to the eyepiece.
        walk.PutACardInFrontOfHim();
        walk.LetOneLookPass();
        walk.OneFrame();

        Assert.False(walk.HeIsOnTheFloor, "the captain looked away and he was still there.");
        Assert.False(double.IsNaN(walk.GoneSince));
    }

    // ── 3 · THE STARE-THROUGH: HE LEAVES, AND NOTHING IS SPENT ──────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>STARE AT HIM AND NOTHING HAPPENS.</b> The owner's own ending: he finishes looking out and
    /// walks back out past the captain. No vanish, no card, no note — and <b>the beat is not spent</b>, which
    /// is where this parts company with #1245. Staring somebody down is a way to not get the scene today,
    /// never a way to lose it for ever.
    ///
    /// <para>The captain stands at the good seat, four and a half du off the rail — well inside what used to
    /// be the throat's two-pace refusal, so on the shipped tree this captain got a man who would not come in
    /// at all and a beat spent for nothing.</para>
    /// </summary>
    [Fact]
    public void StaringAtHimForTheWholeWaitBuysAManWhoWalksBackOutAndNothingElse()
    {
        var walk = new ObservationWalkBench(CanvasId + "-stare");
        StandHimAtTheGoodSeat(walk);

        walk.RunTheWalk(seconds: 120, until: () => walk.HeIsAtTheRail);
        Assert.True(walk.HeIsAtTheRail, "he never reached the rail — something is still holding him.");
        Assert.False(double.IsNaN(walk.AtTheRailSince), "the wait at the rail never started.");

        walk.LetTheWaitPass();
        walk.OneFrame();
        Assert.True(walk.HeHasTurnedBack, "he never gave up; he is still standing at the rail.");

        walk.WalkHimOut();

        Assert.False(walk.HeIsOnTheFloor, "he never got out of his own tube.");
        Assert.False(
            walk.TheBeatIsSpent,
            "watching a man look at a view spent the beat. Nothing was shown, so nothing may be taken.");
        Assert.True(double.IsNaN(walk.GoneSince), "the wait clock started for a man who walked out past you.");

        walk.AskForTheCard();
        Assert.False(walk.ACardWasRaised, "a card was raised for a scene that did not happen.");
        Assert.Equal(0, walk.TimesFiled(ObservationWalk.NoteLine(walk.Person)));
    }

    /// <summary>Stand the captain — on his feet, not sitting — at the north table's own chair: the seat with
    /// the clean line to both the throat and the rail, and therefore the only place in the hat from which a
    /// tail can watch the whole thing.</summary>
    private static void StandHimAtTheGoodSeat(ObservationWalkBench walk)
    {
        DeckReachability.Point chair =
            HavenInterior.BesideATop(
                HavenInterior.GalleryTops(Port)[NorthTable], DeckPlan.AvatarRadius, walk.Walls)
            ?? throw new Xunit.Sdk.XunitException("no chair at the north gallery table.");
        walk.StandTheCaptainAt(chair.X, chair.Y);
    }

    // ── 4 · THE ISLAND MACHINE ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #1199 · <b>SOMETHING TO WALK BEHIND.</b> Owner: <i>"another vending machine or something else that
    /// only MOMENTARILY blocks the view."</i> The third machine stands on the room's axis with its back on
    /// the cafeteria line, and the measurement below is the whole of the claim: from the SOUTH stakeout
    /// table, the last stretch of his walk to the rail is behind it for longer than one look
    /// (<see cref="ReeverObservation.LookIntervalSeconds"/> at <see cref="NpcWalk.PaceDu"/>), so a captain
    /// sitting there watches a man go behind a machine and not come out the other side.
    ///
    /// <para><b>The north table is not in this law, and that is measured too.</b> The two tables sit a
    /// quarter of the hat either side of the axis, on opposite sides of his route: the angle between a line
    /// from one and a line from the other, taken at any point on his walk, is between seventy and a hundred
    /// and twenty degrees, and a fitting a body-and-a-half wide can screen about forty. No single machine can
    /// stand between him and both tables at once — <see cref="TheTwoTablesCannotBothBeScreenedByOneFitting"/>
    /// is that arithmetic, written down so nobody re-opens it by adding furniture. The north table keeps its
    /// clean view of the rail, which is what makes it the good seat, and what a captain in it loses him
    /// behind is the paper or the eyepiece.</para>
    ///
    /// <para><b>RED with the island taken out:</b> the occluded stretch goes to zero and the assert names
    /// it.</para>
    /// </summary>
    [Fact]
    public void TheThirdMachineIsSomethingToWalkBehind()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = HavenInterior.DockedDeck(Port)!.CollisionField;
        DeckReachability.Point rail = HavenInterior.TheRailAt(Port)!.Value;
        DeckReachability.Point throat = HavenInterior.TheThroatAt(Port)!.Value;
        DeckReachability.Point table = TheSouthChair(walls);

        // His walk through the hat, sampled finely: throat to rail, which is the leg OnFoot plots.
        const int steps = 400;
        double behindSomething = 0;
        for (int i = 0; i <= steps; i++)
        {
            double t = (double)i / steps;
            double x = throat.X + ((rail.X - throat.X) * t);
            double y = throat.Y + ((rail.Y - throat.Y) * t);
            if (!SurfaceCollision.HasLineOfSight(table.X, table.Y, x, y, walls))
            {
                behindSomething += Math.Sqrt(
                    ((rail.X - throat.X) * (rail.X - throat.X))
                    + ((rail.Y - throat.Y) * (rail.Y - throat.Y))) / steps;
            }
        }

        double aLookIsWorth = NpcWalk.PaceDu * ReeverObservation.LookIntervalSeconds;
        Assert.True(
            behindSomething >= aLookIsWorth,
            $"from the south table his walk is out of sight for {behindSomething:0.00} du, and one look at a "
            + $"walker's pace is {aLookIsWorth:0.00} du. There is nothing in this room to walk behind.");

        // …and the machine has not taken the thing the tables are FOR: they still see the way in.
        foreach (DeckReachability.Point top in HavenInterior.GalleryTops(Port))
        {
            DeckReachability.Point chair =
                HavenInterior.BesideATop(top, DeckPlan.AvatarRadius, walls)
                ?? throw new Xunit.Sdk.XunitException("no chair at a gallery table.");
            Assert.True(
                SurfaceCollision.HasLineOfSight(chair.X, chair.Y, throat.X, throat.Y, walls),
                "the island machine took a stakeout table's view of the throat.");
        }
    }

    /// <summary>#1199 · The arithmetic behind <i>one island and not two</i>, written down. A fitting can only
    /// screen a body from a viewpoint through the angle it subtends there, and the two tables are much
    /// further apart than that — so a guard demanding both would be a guard demanding furniture that cannot
    /// exist.</summary>
    [Fact]
    public void TheTwoTablesCannotBothBeScreenedByOneFitting()
    {
        IReadOnlyList<DeckReachability.Point> tops = HavenInterior.GalleryTops(Port);
        DeckReachability.Point rail = HavenInterior.TheRailAt(Port)!.Value;

        double AngleTo(DeckReachability.Point p) => Math.Atan2(p.Y - rail.Y, p.X - rail.X);
        double spread = Math.Abs(AngleTo(tops[0]) - AngleTo(tops[1]));

        // The widest a machine can ever screen, from as close as a body may stand to it.
        (double x0, double y0, double x1, double y1) = HavenInterior.TheVendingMachineBlocks(Port)[0];
        double halfWidth = Math.Max(x1 - x0, y1 - y0) / 2.0;
        double closest = 2 * DeckPlan.AvatarRadius;
        double widestScreen = 2 * Math.Atan2(halfWidth, closest);

        Assert.True(
            spread > widestScreen,
            $"the two tables are {spread * 180 / Math.PI:0.0}° apart at the rail and a machine screens at "
            + $"most {widestScreen * 180 / Math.PI:0.0}°. If that has stopped being true, the note on "
            + "TheVendingMachineBlocks about one island rather than two has to be re-argued.");
    }

    private static DeckReachability.Point TheSouthChair(IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        DeckReachability.Point south = HavenInterior.GalleryTops(Port)[0];
        foreach (DeckReachability.Point top in HavenInterior.GalleryTops(Port))
        {
            if (top.Y < south.Y)
            {
                south = top;
            }
        }

        return HavenInterior.BesideATop(south, DeckPlan.AvatarRadius, walls)
            ?? throw new Xunit.Sdk.XunitException("no chair at the south gallery table.");
    }
}

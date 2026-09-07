using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #537 slice 3 · CLIMBING INTO THE HOLE, AS LAWS. Owner: <i>"we smuggle our selves past inspection at that
/// hot ship 😎"</i>, and, filing the lane, <i>"the search tool could create an E-interaction to break into the
/// place or hide."</i>
///
/// <para>Two halves, and each is written so the OTHER one cannot carry it. A rule that hid a captain from
/// everything would pass a hiding test vacuously; a rule that hid nobody would pass a caught test vacuously.
/// So every assertion about the void is paired with the same captain, the same sweeper and the same walls,
/// differing in exactly one fact.</para>
/// </summary>
public sealed class HullStowageTests
{
    // ── The hull under test, and one void cut into her ────────────────────────────────────────────────
    //
    // Constructed rather than seeded: a law about hiding should not also be a law about which hull rolls a
    // void, and a reader should be able to see the numbers the geometry is being asked about. The INSURANCE
    // JOB is the hull #538's sweep team boards, and she carries no damage walls, so nothing near the plate
    // belongs to a cause rather than to this feature.

    private const double PlateX = -8.0;
    private const double VoidX0 = PlateX - 3;
    private const double VoidX1 = PlateX + 3;
    private const Derelict.WreckCause Hull = Derelict.WreckCause.InsuranceJob;

    /// <summary>The middle of the shielding band on the top side — where a captain stands once he is in.</summary>
    private const double InsideY = (WreckLayout.TopY + WreckLayout.OuterTopY) / 2.0;

    /// <summary>…and a spot in DEEP HOLD, a body's length inboard of the same plate.</summary>
    private const double CorridorY = -6.5;

    private static HullSounding.HiddenVoid TheVoid() =>
        new("DEEP HOLD", Outboard: true, VoidX0, VoidX1, Top: true, PlateX, WreckLayout.TopY,
            HullSounding.VoidFrames * WreckLayout.ShieldingDepth, "A rack of code keys.");

    private static HullStowage.OpenVoid Pocket(bool plateShut) =>
        new(VoidX0, VoidX1, Top: true, PlateX, plateShut);

    private static IReadOnlyList<SurfaceCollision.Segment> WallsWith(HullStowage.OpenVoid? opened) =>
        WreckLayout.Walls(Hull, opened);

    /// <summary>A sweeper standing in DEEP HOLD with the lamp pointed straight at the plate.</summary>
    private static InspectionTeam.Member Sweeper() =>
        new("SWEEP-1", PlateX, -5.0, System.Math.Atan2(-1, 0), InspectionTeam.Awareness.Sweeping, 0);

    // A small bounds box round the void, so the A* walks are about this feature and cost nothing.
    private static (double, double, double, double) Around =>
        (PlateX - 12, WreckLayout.OuterTopY - 1, PlateX + 12, -2.0);

    private static bool CanWalk(double toX, double toY, IReadOnlyList<SurfaceCollision.Segment> walls) =>
        DeckReachability.Path(
            new DeckReachability.Point(PlateX, CorridorY), new DeckReachability.Point(toX, toY),
            walls, DeckPlanRadius, Around, step: 0.25).Reached;

    /// <summary>The captain's own body, the number <c>DeckPlan.AvatarRadius</c> holds in the client. Written
    /// down here rather than referenced because Core does not know about the renderer — and pinned below, so
    /// the day the body changes this file fails rather than quietly auditing the wrong person.</summary>
    private const double DeckPlanRadius = 0.7;

    // ── The hole has to be a hole, and only when it is one ────────────────────────────────────────────

    /// <summary>
    /// THE VACUITY PAIR AT THE BOTTOM OF EVERYTHING ELSE. A captain can walk into the shielding band when a
    /// plate has been cut out of it and cannot when it has not.
    ///
    /// <para>Both halves matter and both have failed in this repo's history in one direction or the other: a
    /// gap the collision field does not have is a hole nobody can use (<c>DoorHalfWidth</c>'s own first
    /// cut), and a band a captain can stroll into on an untouched hull is every hiding place on the ship
    /// visible from the corridor.</para>
    /// </summary>
    [Fact]
    public void TheBandIsSealedUntilSomebodyCutsAPlateOutOfIt()
    {
        Assert.False(CanWalk(PlateX, InsideY, WallsWith(null)),
                     "an untouched hull must have no way into her shielding");

        Assert.True(CanWalk(PlateX, InsideY, WallsWith(Pocket(plateShut: false))),
                    "a cut plate must leave a hole a captain fits through");
    }

    /// <summary>AND THE PLATE GOES BACK. Pulled to behind a captain, the pressure hull is whole again —
    /// which is the whole of the hiding, because walls are law for everyone (#324).</summary>
    [Fact]
    public void APlatePulledToIsAWallAgain() =>
        Assert.False(CanWalk(PlateX, InsideY, WallsWith(Pocket(plateShut: true))),
                     "a fitted plate must stop a body exactly as the plating either side of it does");

    /// <summary>
    /// A POCKET, NOT A SECOND CORRIDOR. Six frames of shielding is a place to fold into; the whole band is a
    /// passage running the length of the ship outboard of every compartment, and a captain who could walk it
    /// would be able to enter any room through its outer wall. The pocket is closed at both its own ends.
    /// </summary>
    [Fact]
    public void TheCutOpensSixFramesAndNotTheLengthOfHer()
    {
        IReadOnlyList<SurfaceCollision.Segment> walls = WallsWith(Pocket(plateShut: false));

        Assert.True(CanWalk(PlateX, InsideY, walls), "the pocket itself must be enterable");
        Assert.False(CanWalk(VoidX0 - 3, InsideY, walls), "…and it must end where it ends, going aft");
        Assert.False(CanWalk(VoidX1 + 3, InsideY, walls), "…and going forward");
    }

    /// <summary>The band is drawn solid until a captain has been inside it, and then only the stretch he has
    /// been inside is drawn as space. A map that opened the whole run would be the map knowing more than the
    /// man drawing it — and a hidden space drawn as a space is not hidden.</summary>
    [Fact]
    public void OnlyTheStretchHeHasStoodInStopsBeingDrawnSolid()
    {
        var closed = WreckLayout.StructuralFills(null).ToList();
        var opened = WreckLayout.StructuralFills(Pocket(plateShut: false)).ToList();

        Assert.Contains(closed, f => f.X0 == WreckLayout.TransomX && f.X1 == WreckLayout.ShieldingForwardEnd
                                     && f.Y0 == WreckLayout.OuterTopY && f.Y1 == WreckLayout.TopY);
        Assert.DoesNotContain(opened, f => f.X0 == WreckLayout.TransomX
                                           && f.X1 == WreckLayout.ShieldingForwardEnd
                                           && f.Y0 == WreckLayout.OuterTopY && f.Y1 == WreckLayout.TopY);

        // The top band is now two covers with the pocket between them; the BOTTOM band is untouched, because
        // cutting into one side of a ship says nothing whatever about the other.
        Assert.Contains(opened, f => f.Y0 == WreckLayout.BottomY && f.Y1 == WreckLayout.OuterBottomY
                                     && f.X0 == WreckLayout.TransomX
                                     && f.X1 == WreckLayout.ShieldingForwardEnd);
        Assert.Equal(closed.Count + 1, opened.Count);

        foreach ((float x0, float y0, float x1, float y1) in opened)
        {
            Assert.True(x1 > x0, "a fill with no width covers nothing");
            Assert.True(y1 > y0, "a fill with no height covers nothing");
        }
    }

    /// <summary>The body this file audits with is the body the client walks with. If <c>DeckPlan.AvatarRadius</c>
    /// ever moves, this fails rather than every law above quietly auditing a different person.</summary>
    [Fact]
    public void ThePlateIsCutWiderThanTheCaptainIs() =>
        Assert.True(HullStowage.PlateHalfWidth > DeckPlanRadius,
                    "a hole narrower than the body is a hole nobody can use");

    // ── Who fits ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A HAND'S WIDTH OF PIPEWORK IS NOT A HIDING PLACE. The owner's own heuristic — <i>"a room with a wall
    /// to technical space is a good bet on large enough hiding space"</i> — is a BET because the other kind
    /// of void exists: a bulkhead run is <c>BulkheadDepth</c> 1.2 du against a 1.4 du body. Papers go in
    /// there. You do not.
    /// </summary>
    [Fact]
    public void OnlyTheShieldingBandHasRoomForAPerson()
    {
        Assert.Equal(HullStowage.Fit.Fits, HullStowage.RoomForACaptain(TheVoid()));
        Assert.Equal(HullStowage.Fit.TheRunIsTooNarrow,
                     HullStowage.RoomForACaptain(TheVoid() with { Outboard = false }));

        // And the arithmetic behind the ruling, so it cannot drift into a taste: the band takes a body and
        // the run does not.
        Assert.True(WreckLayout.ShieldingDepth > DeckPlanRadius * 2);
        Assert.True(WreckLayout.BulkheadDepth < DeckPlanRadius * 2);
    }

    // ── The hide, and the coverage floor under it ────────────────────────────────────────────────────

    /// <summary>
    /// THE LAW THE WHOLE SLICE EXISTS FOR, AND ITS OWN VACUITY PAIR IN ONE TEST. The same sweeper, at the
    /// same spot, with their lamp on the same wall: a captain standing in the corridor is caught, and a
    /// captain folded into the void behind a cold cut is not.
    ///
    /// <para>Neither half can carry the other. If the hide were a flag that swallowed everything, the first
    /// assertion fails; if hiding did nothing, the second does. And the SEEING is real geometry — one call
    /// to <see cref="InspectionTeam.Sees"/> with the hull's own walls — so this is also a check that the cut
    /// plate went into the collision list rather than into a comment.</para>
    /// </summary>
    [Fact]
    public void ACaptainInTheVoidSurvivesASweepThatCatchesHimInTheCorridor()
    {
        InspectionTeam.Member him = Sweeper();
        IReadOnlyList<SurfaceCollision.Segment> shut = WallsWith(Pocket(plateShut: true));

        // Standing in the room, a body's length from the plate: seen, and given away.
        Assert.True(InspectionTeam.Sees(him, PlateX, CorridorY, shut));
        Assert.Equal(
            HullStowage.Tell.StandingInTheOpen,
            HullStowage.WhatGivesYouAway(
                insideTheVoid: false, plateShut: false, theyWatchedYouGetIn: false,
                secondsSinceTheCut: 0, theirLampIsOnThePlate: true, theyCanSeeYou: true));

        // Folded in behind the same plate, with the cut long cold: not seen, and not given away.
        Assert.False(InspectionTeam.Sees(him, PlateX, InsideY, shut));
        Assert.Equal(
            HullStowage.Tell.None,
            HullStowage.WhatGivesYouAway(
                insideTheVoid: true, plateShut: true, theyWatchedYouGetIn: false,
                secondsSinceTheCut: HullStowage.CutStaysWarmSeconds + 1,
                theirLampIsOnThePlate: InspectionTeam.Sees(him, PlateX, WreckLayout.TopY, shut),
                theyCanSeeYou: InspectionTeam.Sees(him, PlateX, InsideY, shut)));
    }

    /// <summary>
    /// AND THE LAMP REALLY IS ON THE PLATE. The whole warm-cut tell rests on a sweeper being able to LOOK at
    /// a wall they cannot see through — a sightline that ends ON a segment must not be blocked by it, or the
    /// tell can never fire and the hide is unconditional. Pinned because it is a property of the collision
    /// primitive rather than of this feature, and a tightening there would silently gut this rule.
    /// </summary>
    [Fact]
    public void ASweeperCanLookAtAPlateHeCannotSeeThrough()
    {
        InspectionTeam.Member him = Sweeper();
        IReadOnlyList<SurfaceCollision.Segment> shut = WallsWith(Pocket(plateShut: true));

        Assert.True(InspectionTeam.Sees(him, PlateX, WreckLayout.TopY, shut), "the plate is in his lamp");
        Assert.False(InspectionTeam.Sees(him, PlateX, InsideY, shut), "and the man behind it is not");
    }

    /// <summary>
    /// THE GIVE-AWAY, AND WHY IT IS THIS ONE. A cut is bright metal and slag for
    /// <see cref="HullStowage.CutStaysWarmSeconds"/>, and a lamp that lands on it in that window opens the
    /// plate. Deterministic — it is a clock — and it makes the interesting mistake legible: cutting a hole
    /// to hide in AS they come aboard is worse than useless, and cutting one early is the whole play.
    /// </summary>
    [Fact]
    public void AWarmCutOpensThePlateAndAColdOneDoesNot()
    {
        HullStowage.Tell WithAge(double seconds) => HullStowage.WhatGivesYouAway(
            insideTheVoid: true, plateShut: true, theyWatchedYouGetIn: false,
            secondsSinceTheCut: seconds, theirLampIsOnThePlate: true, theyCanSeeYou: false);

        Assert.Equal(HullStowage.Tell.TheCutIsStillWarm, WithAge(0));
        Assert.Equal(HullStowage.Tell.TheCutIsStillWarm, WithAge(HullStowage.CutStaysWarmSeconds - 0.01));
        Assert.Equal(HullStowage.Tell.None, WithAge(HullStowage.CutStaysWarmSeconds));

        // …and a warm cut nobody is looking at is not a tell either. The scar is evidence, not an alarm.
        Assert.Equal(
            HullStowage.Tell.None,
            HullStowage.WhatGivesYouAway(
                insideTheVoid: true, plateShut: true, theyWatchedYouGetIn: false,
                secondsSinceTheCut: 0, theirLampIsOnThePlate: false, theyCanSeeYou: false));
    }

    /// <summary>The cut outlives one investigation on purpose: make a racket, hide, and wait it out, and the
    /// team is still at your wall while the metal is bright. If this ever inverted, the correct play would
    /// become "be loud on purpose", which is the opposite of everything else the search teaches.</summary>
    [Fact]
    public void TheCutStaysWarmLongerThanTheySearchAPlace() =>
        Assert.True(HullStowage.CutStaysWarmSeconds > InspectionTeam.SearchSeconds,
                    "a stowaway must not be able to out-wait a noise he made himself");

    /// <summary>THEY WATCHED IT SHUT — the cubicle's rule (#821), which does not need them to see through
    /// anything. It is why climbing in under a lamp is a mistake and waiting for the cone to pass is a
    /// play.</summary>
    [Fact]
    public void ALampOnThePlateAsItClosesIsWorseThanAWarmCut()
    {
        Assert.Equal(
            HullStowage.Tell.TheyWatchedYouGetIn,
            HullStowage.WhatGivesYouAway(
                insideTheVoid: true, plateShut: true, theyWatchedYouGetIn: true,
                secondsSinceTheCut: HullStowage.CutStaysWarmSeconds * 10,
                theirLampIsOnThePlate: false, theyCanSeeYou: false));
    }

    /// <summary>A HOLE WITH A FACE IN IT IS NOT A HIDING PLACE. The state exists because getting out is a
    /// frame in which the plate is off and the captain is still in the pocket.</summary>
    [Fact]
    public void AnOpenHoleGivesHimAwayWhateverElseIsTrue() =>
        Assert.Equal(
            HullStowage.Tell.TheHoleIsOpenBehindYou,
            HullStowage.WhatGivesYouAway(
                insideTheVoid: true, plateShut: false, theyWatchedYouGetIn: false,
                secondsSinceTheCut: HullStowage.CutStaysWarmSeconds * 10,
                theirLampIsOnThePlate: true, theyCanSeeYou: false));

    /// <summary>
    /// BEING SEEN OUTRANKS EVERY HIDING STATE. The failure mode a stealth rule has to be built against is an
    /// arrangement of its own flags that makes a visible captain invisible, so every combination is walked
    /// and none of them is allowed to answer <see cref="HullStowage.Tell.None"/>.
    /// </summary>
    [Fact]
    public void NoArrangementOfHidingMakesAVisibleCaptainInvisible()
    {
        int walked = 0;
        foreach (bool inside in new[] { true, false })
        {
            foreach (bool shut in new[] { true, false })
            {
                foreach (bool watched in new[] { true, false })
                {
                    foreach (double age in new[] { 0.0, HullStowage.CutStaysWarmSeconds * 10 })
                    {
                        foreach (bool lamp in new[] { true, false })
                        {
                            walked++;
                            Assert.True(HullStowage.Caught(HullStowage.WhatGivesYouAway(
                                inside, shut, watched, age, lamp, theyCanSeeYou: true)));
                        }
                    }
                }
            }
        }

        Assert.Equal(32, walked);
    }

    /// <summary>
    /// AND THE HULL WITH NO HOLE IN HER HIDES NOBODY. The coverage floor for the other side: on an untouched
    /// ship the rule must be exactly the sighting test it replaced, or the sweep quietly stops working on
    /// every hull in the game that has nothing to find.
    /// </summary>
    [Fact]
    public void OnAHullNobodyHasCutTheRuleIsJustBeingSeen()
    {
        Assert.Equal(
            HullStowage.Tell.StandingInTheOpen,
            HullStowage.WhatGivesYouAway(false, false, false, double.PositiveInfinity, false, true));
        Assert.Equal(
            HullStowage.Tell.None,
            HullStowage.WhatGivesYouAway(false, false, false, double.PositiveInfinity, false, false));

        Assert.True(HullStowage.Caught(HullStowage.Tell.StandingInTheOpen));
        Assert.False(HullStowage.Caught(HullStowage.Tell.None));
    }

    // ── What is said ────────────────────────────────────────────────────────────────────────────────

    /// <summary>Every tell has words, they carry the call-sign, and the ones about the hole say what was
    /// SEEN and never what was concluded — a sweeper who announces "there is a stowaway behind this plate"
    /// has done the player's thinking for them (#533).</summary>
    [Fact]
    public void EveryTellHasWordsAndNoneOfThemDrawsTheConclusion()
    {
        foreach (HullStowage.Tell t in Enum.GetValues<HullStowage.Tell>())
        {
            string line = HullStowage.TellLine(t, "SWEEP-2");
            Assert.False(string.IsNullOrWhiteSpace(line));
            Assert.Contains("SWEEP-2", line, StringComparison.Ordinal);
            Assert.DoesNotContain("stowaway", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hiding", line, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("warm", HullStowage.TellLine(HullStowage.Tell.TheCutIsStillWarm, "SWEEP-1"),
                        StringComparison.OrdinalIgnoreCase);
        Assert.Contains("goes on", HullStowage.TellLine(HullStowage.Tell.None, "SWEEP-1"),
                        StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The plate's three faces are three different sentences, so a captain never presses E without
    /// knowing which of the three verbs he is about to spend.</summary>
    [Fact]
    public void ThePlateSaysWhichOfItsThreeLivesItIsIn()
    {
        string found = HullStowage.PlateLabel(opened: false, inside: false);
        string open = HullStowage.PlateLabel(opened: true, inside: false);
        string inside = HullStowage.PlateLabel(opened: true, inside: true);

        Assert.Equal(3, new HashSet<string> { found, open, inside }.Count);
        Assert.All(new[] { found, open, inside }, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    // ── The cutter ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE RIG IS THE CELL, AND THE CELL IS THE COUNT. There is no per-item wear model in this codebase and
    /// this feature deliberately did not invent one: <c>Satchel.Item.Count</c> already is the wear, it is
    /// already in the vault's stored form, and <c>Satchel.Remove</c> already decrements it.
    /// </summary>
    [Fact]
    public void EachPlateSpendsOneCutAndTheLastOneLeavesTheRigInTheHole()
    {
        IReadOnlyList<Satchel.Item> carried = Satchel.Add([], HullCutter.FreshRig);
        Assert.Equal(HullCutter.CutsPerCell, HullCutter.CutsLeft(carried));

        for (int cut = HullCutter.CutsPerCell; cut > 0; cut--)
        {
            HullCutter.Order order = HullCutter.Force(carried);
            Assert.True(order.Cut);
            Assert.Equal(cut - 1, order.CutsLeft);
            Assert.False(string.IsNullOrWhiteSpace(order.Line));
            carried = order.Carried;
        }

        Assert.Equal(0, HullCutter.CutsLeft(carried));
        Assert.DoesNotContain(carried, HullCutter.IsTheCutter);   // the stub stays in the hole
    }

    /// <summary>NO RIG, NO PLATE — and the refusal names its reason, because a silent nothing is
    /// indistinguishable from a bug and this ground has shipped that twice.</summary>
    [Fact]
    public void WithoutARigThePlateStaysWhereItIs()
    {
        HullCutter.Order empty = HullCutter.Force([]);

        Assert.False(empty.Cut);
        Assert.Equal(0, empty.CutsLeft);
        Assert.Equal(HullCutter.NoCutterLine, empty.Line);
        Assert.False(string.IsNullOrWhiteSpace(empty.Line));

        // …and a rig that has been spent is the same answer, which is the point of the count being the item.
        IReadOnlyList<Satchel.Item> spent = Satchel.Add([], new Satchel.Item(Satchel.Kind.Tool,
                                                                            HullCutter.ItemId, 1));
        Assert.False(HullCutter.Force(HullCutter.Force(spent).Carried).Cut);
    }

    /// <summary>The rig is BULKY, deliberately: it rides in the pockets proper, so the honest price of
    /// carrying it is not carrying something else. That is the satchel's own arithmetic (#688) and this
    /// feature does not get to opt out of it.</summary>
    [Fact]
    public void TheRigCostsPocketRoomLikeEveryOtherToolDoes()
    {
        Assert.Equal(Satchel.Kind.Tool, HullCutter.FreshRig.Kind);
        Assert.Equal(Satchel.Compartment.Pocket, Satchel.CompartmentOf(Satchel.Kind.Tool));
        Assert.True(HullCutter.IsTheCutter(HullCutter.FreshRig));
        Assert.False(HullCutter.IsTheCutter(SdrScanner.TheKit));
    }

    /// <summary>The counter's four cases, each with words. A purchase that quietly takes the coin and hands
    /// over nothing is the worst refusal in the game.</summary>
    [Fact]
    public void TheCounterAnswersInWordsWhicheverWayItGoes()
    {
        HullCutter.Bought sold = HullCutter.Buy(HullCutter.PriceCr, []);
        Assert.True(sold.Taken);
        Assert.Equal(HullCutter.PriceCr, sold.Cost);
        Assert.Equal(0, sold.RemainingCredits);

        HullCutter.Bought broke = HullCutter.Buy(HullCutter.PriceCr - 1, []);
        Assert.False(broke.Taken);
        Assert.Equal(0, broke.Cost);
        Assert.Equal(HullCutter.PriceCr - 1, broke.RemainingCredits);

        HullCutter.Bought again = HullCutter.Buy(9999, Satchel.Add([], HullCutter.FreshRig));
        Assert.False(again.Taken);
        Assert.Equal(HullCutter.AlreadyCarryingLine, again.Line);

        // …and a spent rig is not in the pocket at all, so the counter will sell another.
        Assert.True(HullCutter.Buy(9999, []).Taken);

        foreach (HullCutter.Bought b in new[] { sold, broke, again })
        {
            Assert.False(string.IsNullOrWhiteSpace(b.Line));
        }
    }

    /// <summary>The cut is priced against the two gears it sits between, so "you pay in time or you pay in
    /// noise" keeps meaning something: opening a plate costs more standing still than the loud sounding that
    /// found it, and it is the loud one either way.</summary>
    [Fact]
    public void ForcingAPlateCostsMoreStandingStillThanFindingItDid() =>
        Assert.True(HullCutter.CutSeconds > HullSounding.Seconds(HullSounding.Method.Sounder),
                    "a cut that is quicker than a sounding makes finding the thing the expensive half");
}

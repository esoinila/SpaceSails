using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #804 · THE ROUNDS — where they are, what they walk, what each side may know, and what happens when one
/// stops at you.
///
/// <para>Every law here is one of the owner's three sentences turned into something a build can fail on:
/// the beat is learnable, the knowing is asymmetric in both directions, and a sighting is a CHALLENGE and
/// can never be anything else.</para>
///
/// <para>The A* half — that every stop on every round is walkable on the real collision field — lives in
/// the client's <c>TheRoundIsWalkableTests</c>, because the walls a guard obeys are the deck's walls and
/// only the client can build one. This file owns everything that is a rule rather than a room.</para>
/// </summary>
public class TheRoundsOnTheRestrictedFloorsTests
{
    /// <summary>The sites the Hive guards are swept over. The last three are the rare shapes — a site with a
    /// band nobody listed, one with halls past the seam — because a rule that only holds on an ordinary
    /// clinic is a rule that has not met the generator.</summary>
    private static readonly string[] Sites =
    [
        "luna", "phobos", "europa", "ganymede", "callisto", "titan", "enceladus", "miranda", "triton",
        "the-clinker", "secret-lab-site", "secret-lab-site-unlisted",
        UndergroundComplex.FoundBandCheatSiteId,
    ];

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    // ── WHERE THEY ARE, AND WHERE THEY DELIBERATELY ARE NOT ───────────────────────────────────────────

    /// <summary>
    /// The B1 ruling, the directory's own bottom, and the two bands the building denies having.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>level >= DepthOf</c> clause from
    /// <see cref="PatrolBeat.IsPatrolled"/>: the unlisted lobby and every floor past the seam start
    /// rostering guards, and this names them.</para>
    /// </summary>
    [Fact]
    public void NobodyWalksTheBarAndNobodyWalksThePartsTheBuildingDeniesHaving()
    {
        var complaints = new List<string>();
        int patrolled = 0, sitesWithARound = 0;

        foreach (string site in Sites)
        {
            bool any = false;
            Assert.False(PatrolBeat.IsPatrolled(site, 0), $"{site}: somebody is walking the regolith.");

            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                bool walked = PatrolBeat.IsPatrolled(site, level);
                if (walked)
                {
                    patrolled++;
                    any = true;
                }

                if (walked && UndergroundComplex.TopPressurisedFloor(site) == level)
                {
                    complaints.Add($"{site} B{-level}: a round on the floor the bar is on — the plate on that " +
                                   "room says NO PASS REQUIRED.");
                }
                if (walked && UndergroundComplex.IsUnlisted(site, level))
                {
                    complaints.Add($"{site} B{-level}: a round on the band nobody listed.");
                }
                if (walked && UndergroundComplex.IsFound(site, level))
                {
                    complaints.Add($"{site} B{-level}: a round in the halls nobody dug.");
                }
                if (walked && level < UndergroundComplex.DepthOf(site))
                {
                    complaints.Add($"{site} B{-level}: a round deeper than the directory admits to.");
                }
            }

            // The positive half, stated as the building's own arithmetic rather than as a count somebody
            // typed: a site whose DIRECTORY reaches below the bar has floors with a payroll on them, and
            // every one of them must be walked. A site that stops at the bar has nothing to walk and is not
            // a failure — it is a two-floor hole in the ground.
            //
            // …and never the head office, which is in this list on purpose: ENCELADUS is #411's building,
            // and a sweep that quietly skipped it would prove the exclusion nowhere.
            bool hasWorkingFloors =
                !UndergroundComplex.IsHeadOffice(site)
                && UndergroundComplex.TopPressurisedFloor(site) is { } top
                && UndergroundComplex.DepthOf(site) < top;
            Assert.True(
                hasWorkingFloors == any,
                $"{site}: the directory reaches to B{-UndergroundComplex.DepthOf(site)} under a bar on " +
                $"B{-(UndergroundComplex.TopPressurisedFloor(site) ?? 0)}, so working floors " +
                $"{(hasWorkingFloors ? "exist" : "do not exist")} — and somebody is " +
                $"{(any ? "" : "not ")}walking them.");

            if (any)
            {
                sitesWithARound++;
            }
        }

        Assert.True(complaints.Count == 0, string.Join("\n", complaints));

        // …and the guard is not vacuous. A predicate that answered false everywhere would satisfy every
        // clause above and ship a feature nobody can reach (the fifth bug class, exactly).
        Assert.True(patrolled > 40, $"only {patrolled} floors in the whole sweep have a round on them.");
        Assert.True(
            sitesWithARound >= Sites.Length - 1,
            $"only {sitesWithARound} of {Sites.Length} sites have anybody walking them.");
    }

    [Fact]
    public void TheHeadOfficeHasNoContractGuardsInIt()
    {
        // #411's building has no gate, no shafts to card and a fiction of its own. A contract guard in the
        // wintering hall would be this feature deciding something about a place it does not own.
        string hq = KaamosLore.IceMoonBodyId;
        Assert.True(UndergroundComplex.IsHeadOffice(hq), "the head office moved and this guard did not.");
        foreach (int level in UndergroundComplex.FloorsOf(hq))
        {
            Assert.False(PatrolBeat.IsPatrolled(hq, level), $"a round on the head office's B{-level}.");
            Assert.Equal(0, PatrolBeat.GuardsOn(hq, level, 0));
        }
    }

    /// <summary>One or two, never three, and BOTH must actually happen — a roll that always answered one
    /// would make the two-guard watch unreachable content.</summary>
    [Fact]
    public void AFloorCarriesOneOrTwoAndBothWatchesExist()
    {
        var seen = new HashSet<int>();
        int asked = 0;

        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    Assert.Equal(0, PatrolBeat.GuardsOn(site, level, 0));
                    continue;
                }

                for (long watch = 0; watch < 6; watch++)
                {
                    int heads = PatrolBeat.GuardsOn(site, level, watch);
                    asked++;
                    Assert.InRange(heads, 1, PatrolBeat.MostOnAFloor);
                    seen.Add(heads);
                }
            }
        }

        Assert.True(asked > 200, $"only {asked} (floor, watch) pairs were asked.");
        Assert.Equal(new HashSet<int> { 1, 2 }, seen);
    }

    // ── THE BEAT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The circuit is the LIFT and then the ribs in ascending x, each with its far room after it — and the
    /// mouths are sorted, which is #587's own lesson one floor along.
    ///
    /// <para><b>Proven RED</b> by turning the <c>ribs.Sort</c> in <see cref="PatrolBeat.Circuit"/> into a
    /// <c>ribs.Reverse</c>: twelve complaints, naming the site, the floor and both mouths —
    /// <c>secret-lab-site-halls-116 B4: the round goes back on itself — a mouth at x-5.0 after one at
    /// x93.4</c>.</para>
    ///
    /// <para><b>And the honest note about that proof.</b> Merely DELETING the sort is green today, because
    /// the generator happens to publish <c>Ribs</c> in ascending x. That is precisely why the sort is there:
    /// #587 is the story of a builder whose correctness depended on an order its source was not obliged to
    /// keep, and the fix that shipped was <i>"if a builder's correctness depends on order, sort at the point
    /// of use."</i> This guard states the ORDER as a law of the round rather than as a hope about the plan,
    /// and it fails the moment either end of that stops being true.</para>
    /// </summary>
    [Fact]
    public void TheCircuitRunsAlongTheSpineInOrderAndStartsAtTheCar()
    {
        var complaints = new List<string>();
        int walked = 0, stops = 0;

        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(site, level, Field);
                IReadOnlyList<PatrolBeat.Stop> circuit = PatrolBeat.Circuit(floor, Field);
                walked++;
                stops += circuit.Count;

                (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);
                Assert.True(circuit.Count > 0, $"{site} B{-level}: a floor with no round to walk at all.");
                Assert.Equal(shaftX, circuit[0].X, 6);
                Assert.Equal("the car", circuit[0].What);

                // The mouths, in the order the round meets them. Room stops sit between mouths and are not
                // on the spine, so the ordering law is asked of the mouths alone.
                double lastMouth = double.NegativeInfinity;
                foreach (PatrolBeat.Stop stop in circuit)
                {
                    if (!stop.What.StartsWith("the mouth", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    Assert.Equal(shaftY, stop.Y, 6);
                    if (stop.X < lastMouth)
                    {
                        complaints.Add(
                            $"{site} B{-level}: the round goes back on itself — a mouth at x{stop.X:F1} " +
                            $"after one at x{lastMouth:F1}. A list built by appending is not a list in order.");
                    }
                    lastMouth = stop.X;
                }
            }
        }

        Assert.True(complaints.Count == 0, string.Join("\n", complaints));
        Assert.True(walked > 40, $"only {walked} floors were walked.");
        Assert.True(stops > walked * 3, "the rounds are too short to be rounds.");
    }

    [Fact]
    public void NoTwoStOPSOnARoundAreTheSamePlace()
    {
        // Two stops on one square is a guard standing still for two stands in a row, which reads as a
        // frozen NPC and is exactly what the claim ledger in FurthestRoomOn exists to stop.
        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(site, level, Field);
                var seen = new HashSet<(double, double)>();
                foreach (PatrolBeat.Stop stop in PatrolBeat.Circuit(floor, Field))
                {
                    Assert.True(
                        seen.Add((Math.Round(stop.X, 3), Math.Round(stop.Y, 3))),
                        $"{site} B{-level}: the round stops twice at ({stop.X:F1}, {stop.Y:F1}).");
                }
            }
        }
    }

    /// <summary>
    /// Deterministic inside a watch, different across watches. Both halves matter: the first is what makes a
    /// round learnable, the second is what makes learning one not the end of the feature.
    ///
    /// <para><b>Proven RED</b> by dropping <c>watch</c> from the direction and offset seeds — every watch
    /// then walks the identical round and <see cref="TheRoundTurnsOverWithTheShift"/> fails with a turnover
    /// count of zero.</para>
    /// </summary>
    [Fact]
    public void TheSameRoundIsWalkedTwiceInsideOneWatch()
    {
        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(site, level, Field);
                for (long watch = 0; watch < 3; watch++)
                {
                    IReadOnlyList<PatrolBeat.Stop> a = PatrolBeat.BeatFor(site, level, watch, floor, Field);
                    IReadOnlyList<PatrolBeat.Stop> b = PatrolBeat.BeatFor(site, level, watch, floor, Field);
                    Assert.Equal(a, b);
                    Assert.Equal(PatrolBeat.Circuit(floor, Field).Count, a.Count);
                }
            }
        }
    }

    [Fact]
    public void TheRoundTurnsOverWithTheShift()
    {
        int floors = 0, turnedOver = 0;

        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }

                UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(site, level, Field);
                floors++;

                var shapes = new HashSet<string>(StringComparer.Ordinal);
                for (long watch = 0; watch < 6; watch++)
                {
                    shapes.Add(string.Join(
                        "|", PatrolBeat.BeatFor(site, level, watch, floor, Field)
                            .Select(s => $"{s.X:F1},{s.Y:F1}")));
                }
                if (shapes.Count > 1)
                {
                    turnedOver++;
                }
            }
        }

        Assert.True(floors > 40, $"only {floors} floors were measured.");
        // Not "every floor turns over on every watch" — a six-stop round has six rotations and two
        // directions, and a seeded roll may land twice. What must be true is that the shift MOVES the round
        // on the overwhelming majority of floors; a beat nailed to the building would be furniture.
        Assert.True(
            turnedOver > floors * 9 / 10,
            $"only {turnedOver} of {floors} floors walk a different round on a different watch.");
    }

    [Fact]
    public void TwoGuardsOnOneFloorStartOnOppositeSidesOfTheRound()
    {
        // They share ONE route (being hidden from has to be legible) and differ only by leg. If both started
        // on leg 0 they would walk in a queue and the floor would have one round on it wearing two names.
        Assert.Equal(0, PatrolBeat.StartLeg(8, 0, 2));
        Assert.Equal(4, PatrolBeat.StartLeg(8, 1, 2));
        Assert.Equal(0, PatrolBeat.StartLeg(8, 0, 1));

        // …and it never indexes off the end of a short round, which is the shape a red audit trips over.
        for (int legs = 1; legs <= 12; legs++)
        {
            for (int count = 1; count <= PatrolBeat.MostOnAFloor; count++)
            {
                for (int i = 0; i < count; i++)
                {
                    Assert.InRange(PatrolBeat.StartLeg(legs, i, count), 0, legs - 1);
                }
            }
        }
        Assert.Equal(0, PatrolBeat.StartLeg(0, 1, 2));
    }

    // ── WHAT EACH SIDE MAY KNOW ───────────────────────────────────────────────────────────────────────

    /// <summary>A wall across the corridor, for the sightline laws. One segment, long enough that nothing
    /// can be seen round the end of it.</summary>
    private static readonly SurfaceCollision.Segment[] AWallBetween =
        [new(5.0, -50.0, 5.0, 50.0)];

    /// <summary>
    /// THE MARKER IS THE EYE'S, AND A WALL IS A WALL.
    ///
    /// <para><b>Proven RED</b> by making <see cref="PatrolBeat.DrawnFor"/> return a bare range check: the
    /// behind-the-wall case starts drawing a guard through poured concrete, which is precisely the thing
    /// the owner said must not happen.</para>
    /// </summary>
    [Fact]
    public void AGuardBehindAWallIsNotDrawnAndOneInTheCorridorIs()
    {
        // Ten metres down an open corridor: drawn.
        Assert.True(PatrolBeat.DrawnFor(0, 0, 10, 0, []));

        // The same ten metres with a wall across them: not drawn, and it is the WALL doing it — the range
        // is identical in both calls, so this cannot pass by accident.
        Assert.False(PatrolBeat.DrawnFor(0, 0, 10, 0, AWallBetween));

        // …and past the eye's reach, with nothing in the way at all.
        Assert.False(PatrolBeat.DrawnFor(0, 0, PatrolBeat.MarkerSightDu + 1, 0, []));
        Assert.True(PatrolBeat.DrawnFor(0, 0, PatrolBeat.MarkerSightDu - 1, 0, []));
    }

    /// <summary>
    /// THE WINDOW THE WHOLE FEATURE LIVES IN: there is a range at which you can see them and they cannot
    /// see you. Without it, "wait for them to pass" is a sentence nobody can act on.
    /// </summary>
    [Fact]
    public void YouSeeThemBeforeTheySeeYouAndThereIsRoomToStandInBetween()
    {
        Assert.True(
            PatrolBeat.NoticeDu < PatrolBeat.MarkerSightDu,
            "a guard who notices you as far as you can see them leaves nothing to time.");

        // Somewhere strictly between the two reaches, both directions are asked and they disagree.
        double between = (PatrolBeat.NoticeDu + PatrolBeat.MarkerSightDu) / 2.0;
        Assert.True(PatrolBeat.DrawnFor(0, 0, between, 0, []), "you cannot see them at all at mid range.");
        Assert.False(PatrolBeat.Notices(between, 0, 0, 0, []), "they see you the moment you see them.");

        // …and inside their own reach they do notice, or nothing ever challenges anybody.
        Assert.True(PatrolBeat.Notices(PatrolBeat.NoticeDu - 1, 0, 0, 0, []));

        // Their eye obeys the same walls the captain's does. A guard who saw through concrete would be a
        // second wall law, which is the drift this ground keeps paying for.
        //
        // The pair below is deliberately at ONE range, INSIDE their reach — the wall is the only thing that
        // differs, so this cannot pass because the arithmetic happened to say "too far". A first cut asked
        // it at ten metres and stayed green with the wall law deleted, which is the fifth bug class caught
        // in its own guard.
        double closeIn = PatrolBeat.NoticeDu - 2.0;
        Assert.True(PatrolBeat.Notices(closeIn, 0, 0, 0, []));
        Assert.False(PatrolBeat.Notices(closeIn, 0, 0, 0, AWallBetween));
    }

    [Fact]
    public void CanBeNoticedHoldsTheFirstMomentsOffTheCar()
    {
        Assert.False(PatrolBeat.CanBeNoticed(0));
        Assert.False(PatrolBeat.CanBeNoticed(PatrolBeat.OffTheCarSeconds - 0.01));
        Assert.True(PatrolBeat.CanBeNoticed(PatrolBeat.OffTheCarSeconds));
        Assert.False(PatrolBeat.CanBeNoticed(double.NaN));
    }

    /// <summary>
    /// THE BOOTS ARE ONLY EVER FOR SOMEBODY YOU CANNOT SEE. A line about a tread you are looking at would be
    /// the sentence and the picture disagreeing, which is this repo's third named bug class.
    /// </summary>
    [Fact]
    public void TheBootsAreHeardOnlyWhereTheEyeCannotReach()
    {
        // Close and in the open: seen, therefore never heard.
        Assert.True(PatrolBeat.DrawnFor(0, 0, 10, 0, []));
        Assert.False(PatrolBeat.Heard(0, 0, 10, 0, []));

        // The same ten metres behind a wall: not seen, and inside earshot — the band the line exists for.
        Assert.True(PatrolBeat.Heard(0, 0, 10, 0, AWallBetween));

        // …and past earshot, wall or no wall, there is nothing to hear. That is the range the fan owns.
        Assert.False(PatrolBeat.Heard(0, 0, PatrolBeat.EarshotDu + 1, 0, AWallBetween));

        // #832 · THE RETIRED-COP LAW moved this floor. It was 18 du — short of the eye — which left a band
        // from 18 to 30 where a guard behind a wall was neither drawn nor heard, and that band is exactly
        // the owner's <i>"at no range may a walking man be neither seen nor heard"</i>. The boots now carry
        // as far as the eye itself and never further: the marker still owns everything you can see, because
        // Heard() is silent about anybody in plain sight.
        Assert.True(
            PatrolBeat.EarshotDu >= PatrolBeat.MarkerSightDu,
            "a retired cop whose boots carry less far than your own eye is a ninja (#832).");
        Assert.True(
            PatrolBeat.EarshotDu <= PatrolBeat.MarkerSightDu,
            "boots that carry further than the eye would make the marker pointless.");
        Assert.True(
            PatrolBeat.EarshotDu > PatrolBeat.NoticeDu,
            "boots you can only hear once they have already seen you are not a warning.");

        // The band the law was raised FOR: a guard on the far side of a wall, past the old 18 du and inside
        // the eye's reach. Nothing drew him and nothing said he was there.
        Assert.False(PatrolBeat.DrawnFor(0, 0, 24, 0, AWallBetween));
        Assert.True(PatrolBeat.Heard(0, 0, 24, 0, AWallBetween));
    }

    /// <summary>
    /// AND THE FAN HEARS THEM FIRST OF ALL — through the rock, past the eye's reach, which is the owner's
    /// <i>"we need our motion detector to warn us"</i>. Nothing was added to the instrument: a guard walks,
    /// so a guard is a contact.
    /// </summary>
    [Fact]
    public void TheTrackerHearsARoundLongBeforeAnythingIsDrawn()
    {
        // A guard walking at the round's own speed, well past the eye and behind a wall — and carrying the
        // register Core declares for them (#830), which is what the client's one accessor feeds the fan.
        double outThere = PatrolBeat.MarkerSightDu + 12.0;
        var walking = new MotionTracker.Entity(
            outThere, 0, PatrolBeat.WalkSpeed, 0, PatrolBeat.FanRegister);

        Assert.False(PatrolBeat.DrawnFor(0, 0, outThere, 0, AWallBetween), "the eye reaches too far.");
        Assert.False(PatrolBeat.Heard(0, 0, outThere, 0, AWallBetween), "the ear reaches too far.");

        // The fan's reach on B2 — a real floor with a real round on it — still covers them.
        double reach = MotionTracker.UndergroundRange(MotionTracker.DetectionRange(32.0), -2);
        Assert.True(reach > outThere, $"the fan only reaches {reach:F0} du on B2.");

        IReadOnlyList<MotionTracker.Blip> blips = MotionTracker.Sweep(0, 0, [walking], reach);
        Assert.Single(blips);
        Assert.Equal(outThere, blips[0].Range, 3);
        Assert.Equal(MotionTracker.BlipKind.Crisp, blips[0].Kind);

        // #830 · …and a guard STANDING at a stop does NOT drop off it. That was this file's own shipped
        // sentence — "the stand is a hiding place for them as much as a window for the captain" — and the
        // owner overruled it from the chair, watching PATROL 2 stand in plain sight while the fan called the
        // corridor empty: <i>"if the guard is still then it would be a blurry blob"</i>. He is still there,
        // and the instrument says so with less confidence rather than with silence.
        IReadOnlyList<MotionTracker.Blip> standing =
            MotionTracker.Sweep(0, 0, [walking with { Vx = 0 }], reach);
        Assert.Single(standing);
        Assert.Equal(MotionTracker.BlipKind.Blob, standing[0].Kind);
    }

    // ── #833 · THE APPROACH ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A WALLET IS READ AT ARM'S LENGTH AND A NOTICE HAPPENS ACROSS A CORRIDOR — and the two numbers may
    /// never come together. Owner: <i>"I think the guard should approach us when it does the inspection."</i>
    /// The shipped code raised the card the frame <see cref="PatrolBeat.Notices"/> fired, which is a man
    /// reading your pass from nine deck units off.
    /// </summary>
    [Fact]
    public void TheCardsReachIsAnArmAndTheNoticesIsACorridor()
    {
        Assert.True(PatrolBeat.CardReachDu > 0);
        Assert.True(PatrolBeat.CardReachDu < PatrolBeat.NoticeDu / 3,
            $"a card at {PatrolBeat.CardReachDu} du against a notice at {PatrolBeat.NoticeDu} du is not an " +
            "approach, it is a formality.");

        // …and the predicate agrees with the number, in both directions, at the boundary.
        Assert.True(PatrolBeat.AtCardReach(0, 0, PatrolBeat.CardReachDu - 0.01, 0));
        Assert.False(PatrolBeat.AtCardReach(0, 0, PatrolBeat.CardReachDu + 0.01, 0));
        Assert.False(PatrolBeat.AtCardReach(0, 0, PatrolBeat.NoticeDu - 0.01, 0));
    }

    /// <summary>
    /// WALKING AWAY IS ALLOWED, AND IT IS THE ONLY THING THAT EVER HAPPENS TO A CAPTAIN WHO DOES. He stops
    /// coming when the gap opens past <see cref="PatrolBeat.GivesUpBeyondDu"/> or when the walk-up has run
    /// its clock — and there is no third answer in this file, because there is no chase in this file.
    /// </summary>
    [Fact]
    public void HeStopsComingAndThatIsTheWholeOfIt()
    {
        // Standing still at the reach he noticed you from: he is coming.
        Assert.True(PatrolBeat.StillComing(0.0, 0, 0, PatrolBeat.NoticeDu, 0));
        Assert.True(PatrolBeat.StillComing(PatrolBeat.WalkUpSeconds - 0.1, 0, 0, PatrolBeat.NoticeDu, 0));

        // A step or two further than he registered you at: still coming — a man who has said "hold on" does
        // not give up because you shifted your weight.
        Assert.True(PatrolBeat.StillComing(0.0, 0, 0, PatrolBeat.NoticeDu + 1.0, 0));

        // Away down the corridor, or twenty seconds of it: he goes back to work.
        Assert.False(PatrolBeat.StillComing(0.0, 0, 0, PatrolBeat.GivesUpBeyondDu + 0.01, 0));
        Assert.False(PatrolBeat.StillComing(PatrolBeat.WalkUpSeconds + 0.01, 0, 0, 0.5, 0));
        Assert.False(PatrolBeat.StillComing(double.NaN, 0, 0, 0.5, 0));

        // …and he gives up at a range he can still SEE you from. Following somebody you can see is the chase
        // this file does not have.
        Assert.True(PatrolBeat.GivesUpBeyondDu < PatrolBeat.MarkerSightDu);
    }

    /// <summary>#833 · The escort's own numbers hang together: the captain is kept closer than the tether, he
    /// is worked briskly enough to close a lag, and the bound on the whole walk is longer than the longest
    /// walk the floors can ask for (which <c>TheEscortIsAWalkTests</c> measures at 56 seconds).</summary>
    [Fact]
    public void TheEscortsNumbersCanActuallyPutTwoPeopleAtTheSameDoor()
    {
        Assert.True(PatrolBeat.ShoulderDu < PatrolBeat.TetherDu,
            "the captain is held further away than the guard is allowed to let him lag — the escort would " +
            "stand still forever.");
        Assert.True(PatrolBeat.CatchUpFactor > 1.0, "a lag that cannot be closed is a lag that stays.");
        Assert.True(PatrolBeat.AtTheCarDu < PatrolBeat.AtTheStopDu,
            "the captain would be declared home before the guard had arrived with him.");
        Assert.True(PatrolBeat.EscortSecondsCap > 60.0);
        Assert.True(PatrolBeat.PumpsAfterSeconds < PatrolBeat.EscortSecondsCap / 4,
            "the small talk has to land ON the walk, not at the end of it.");
    }

    // ── THE CHALLENGE ─────────────────────────────────────────────────────────────────────────────────

    private const string Plate = "◈ A CONTRACT GUARD, WALKING THE ROUND";

    [Fact]
    public void ThePassSatisfiesAndNothingElseHappens()
    {
        PatrolBeat.Read read = PatrolBeat.TheGuardReads("luna", Plate, PatrolBeat.Badge("luna"));
        Assert.True(read.Satisfied);
        Assert.Equal(PatrolBeat.SatisfiedLine, read.Line);
        Assert.Null(read.Consequence);
        Assert.Equal(read.Line, read.Told);
        Assert.Contains(Plate, read.Card, StringComparison.Ordinal);
    }

    /// <summary>
    /// The other three rungs, and every one of them ends in the same mildest consequence. The ladder is the
    /// storytelling (#679): a pass for another site names it, the cage chit is refused as a cage chit, and
    /// an empty wallet is its own sentence.
    /// </summary>
    [Fact]
    public void EveryOtherWalletEndsAtTheLiftAndSaysWhyFirst()
    {
        // #836 · One paper per case, because a read takes one paper. What was a wallet is now a HAND.
        (string What, Satchel.Item? Handed, string Expect)[] cases =
        [
            ("nothing at all", null, PatrolBeat.NothingLine),
            ("only the cage chit", CanteenTable.Chit(underAnotherName: false), PatrolBeat.WrongPaperLine),
            ("somebody else's site", PatrolBeat.Badge("europa"), PatrolBeat.WrongSiteLine("europa")),
            ("an authority card, which is not a person",
                new Satchel.Item(Satchel.Kind.Authority, new UndergroundComplex.AuthorityCard("luna", 1).Id),
                PatrolBeat.NothingLine),
        ];

        foreach ((string what, Satchel.Item? handed, string expect) in cases)
        {
            PatrolBeat.Read read = PatrolBeat.TheGuardReads("luna", Plate, handed);
            Assert.False(read.Satisfied, $"{what} satisfied a guard.");
            Assert.Equal(expect, read.Line);
            Assert.Equal(PatrolBeat.EscortLine, read.Consequence);

            // #736 · and the sentence the captain acts on is ON the card, under the read, in one string.
            Assert.StartsWith(read.Line, read.Told, StringComparison.Ordinal);
            Assert.Contains(PatrolBeat.EscortLine, read.Told, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A PASS IS FOR ONE SITE. The card grammar's own law (#679), and the reason a second site's pass is
    /// still worth carrying: it is read out loud rather than ignored.
    /// </summary>
    [Fact]
    public void TheBadgeIsSiteScopedInBothDirections()
    {
        Satchel.Item[] luna = [PatrolBeat.Badge("luna")];
        Assert.True(PatrolBeat.BadgeHeld("luna", luna));
        Assert.False(PatrolBeat.BadgeHeld("europa", luna));

        Assert.True(PatrolBeat.TheGuardReads("luna", Plate, luna[0]).Satisfied);
        Assert.False(PatrolBeat.TheGuardReads("europa", Plate, luna[0]).Satisfied);
        Assert.Contains(
            BodyNames.Designation("luna"),
            PatrolBeat.TheGuardReads("europa", Plate, luna[0]).Line,
            StringComparison.Ordinal);

        // The id round-trips, and nothing else in the wallet is mistaken for one.
        Assert.Equal("luna", PatrolBeat.SiteOfBadge(PatrolBeat.BadgeId("luna")));
        Assert.Null(PatrolBeat.SiteOfBadge(CanteenTable.ChitId));
        Assert.Null(PatrolBeat.SiteOfBadge("badge:"));
        Assert.Null(PatrolBeat.SiteOfBadge(null));
    }

    [Fact]
    public void ThePassRidesTheWalletAndTheWalletNeverFills()
    {
        // #688's ruling, third customer: what costs a captain room is BULK, and a card has none. A pass that
        // could be refused because the pockets were full would be the best find in the feature dropped on a
        // floor for a shipping manifest.
        Assert.Equal(Satchel.Compartment.Wallet, Satchel.CompartmentOf(Satchel.Kind.Badge));

        var full = new List<Satchel.Item>();
        for (int i = 0; i < Satchel.PocketCapacity; i++)
        {
            full.Add(new Satchel.Item(Satchel.Kind.Rounds, $"junk-{i}"));
        }
        for (int i = 0; i < Satchel.SleeveCapacity; i++)
        {
            full.Add(new Satchel.Item(Satchel.Kind.Paper, $"paper-{i}"));
        }
        Assert.True(Satchel.CanTake(full, PatrolBeat.Badge("luna")));

        // …and it survives a save, which is what makes it a possession rather than a flag.
        Assert.True(Satchel.Item.TryParse(PatrolBeat.Badge("luna").Stored, out Satchel.Item back));
        Assert.Equal(Satchel.Kind.Badge, back.Kind);
        Assert.Equal(PatrolBeat.BadgeId("luna"), back.Id);
    }

    [Fact]
    public void APassIsNeverOfferedToADoor()
    {
        // #688's law: doors suggest keys, not paperwork. A badge answers a PERSON; a live "try it →" on a
        // shaft gate would be the game dangling an offer that has no reader.
        foreach (SatchelTry.Target target in Enum.GetValues<SatchelTry.Target>())
        {
            Assert.Equal(!SatchelTry.IsDoor(target), SatchelTry.CanOffer(Satchel.Kind.Badge, target));
        }
    }

    // ── THE REGISTER, AND THE CANON ───────────────────────────────────────────────────────────────────

    [Fact]
    public void TheCatalogIsWhatItSaysItIs()
    {
        // A sweep over an empty list is a green test that asserts nothing. Pin the plates, and pin that the
        // rota reaches every one of them rather than always naming the first.
        Assert.Equal(4, PatrolBeat.PlateCount);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string site in Sites)
        {
            foreach (int level in UndergroundComplex.FloorsOf(site))
            {
                if (!PatrolBeat.IsPatrolled(site, level))
                {
                    continue;
                }
                for (long watch = 0; watch < 6; watch++)
                {
                    for (int i = 0; i < PatrolBeat.MostOnAFloor; i++)
                    {
                        seen.Add(PatrolBeat.PlateOf(site, level, watch, i));
                    }
                }
            }
        }
        Assert.Equal(PatrolBeat.PlateCount, seen.Count);

        // Deterministic inside a watch, like everything else down here.
        Assert.Equal(PatrolBeat.PlateOf("luna", -2, 3, 0), PatrolBeat.PlateOf("luna", -2, 3, 0));
    }

    /// <summary>
    /// §13.8, on the one part of this building that has a mouth and a uniform. Nothing a guard says explains
    /// anything, and the grep walks the catalog itself so a line added tomorrow is checked tomorrow.
    /// </summary>
    [Fact]
    public void NothingAGuardSaysExplainsAnything()
    {
        string[] forbidden =
        [
            "old one", "old ones", "reever", "restore", "backup", "revive", "resurrect", "clone", "slave",
            "brain", "kaamos", "minister", "ancient", "alien", "experiment", "specimen",
        ];

        int lines = 0;
        foreach (string line in PatrolBeat.AllProse())
        {
            lines++;
            Assert.False(string.IsNullOrWhiteSpace(line), "an empty sentence in the catalog.");
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
        Assert.True(lines >= PatrolBeat.PlateCount + 9, $"the canon grep only walked {lines} sentences.");
    }

    /// <summary>
    /// THE REGISTER: an employee, not a monster. #618's canon constraint — <i>"the owner's ask is a cover
    /// that can blow, not a detection meter"</i> — and the one word-list that keeps a bored man on a rota
    /// from turning into a horror beat the moment somebody writes the next line.
    /// </summary>
    [Fact]
    public void NobodyOnARoundEverShoutsChasesOrArrests()
    {
        string[] wrongRegister =
        [
            "alarm", "alert", "lockdown", "chase", "chases", "pursue", "pursuit", "arrest", "shoot",
            "shouts", "screams", "runs at", "draws his", "hunting",
        ];

        foreach (string line in PatrolBeat.AllProse())
        {
            foreach (string word in wrongRegister)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // …and the escort is the only consequence the read can hand back. A second one would be the
        // suspicion ladder arriving without a ruling.
        var consequences = new HashSet<string>(StringComparer.Ordinal);
        foreach (Satchel.Item? handed in new Satchel.Item?[]
                 {
                     null,
                     PatrolBeat.Badge("luna"),
                     PatrolBeat.Badge("europa"),
                     CanteenTable.Chit(underAnotherName: true),
                 })
        {
            PatrolBeat.Read read = PatrolBeat.TheGuardReads("luna", Plate, handed);
            consequences.Add(read.Consequence ?? "<nothing at all>");
        }
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "<nothing at all>", PatrolBeat.EscortLine },
            consequences);
    }

    [Fact]
    public void TheTimingWindowIsAWindow()
    {
        // The stand is what a captain waits out, and the walk is slower than theirs. Both are the feature;
        // a round that never stopped, or one that outran the captain, would make timing impossible.
        Assert.True(PatrolBeat.StandSeconds >= 3.0, "the gap is too short to step into.");
        Assert.True(PatrolBeat.WalkSpeed < 9.0, "a round that outruns the captain is not a round.");
        Assert.True(PatrolBeat.AfterTheStopSeconds > PatrolBeat.StandSeconds,
            "a guard who re-challenges inside one stand is a loop at the lift doors.");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #835 · THE OTHER BRANCH, WALKED AND PINNED.
///
/// <para>Owner, evening playtest 2026-08-11, reversing his own standing law with the implementation named:
/// <i>"they need to catch us .... like reevers :-D we could use that code :-D"</i> — and, in the same breath,
/// the two constraints that keep it from being a stealth level: <i>"just no damage by default :-D"</i>, and
/// suspicion still before pursuit.</para>
///
/// <para><b>What this file can and cannot prove.</b> Same split as <see cref="TheEscortIsAWalkTests"/>, for
/// the same reason: the run lives in a partial class on a razor page no test can build. So the sim below is
/// a faithful REPLICA of <c>Map.Patrol.RunAfterTheCaptain</c> over the real deck, the real Core numbers and
/// the real collision field — it proves the floors, the speeds and the walls let a catch and an escape both
/// happen — and the clauses that decide WHEN a run may start, what a catch costs and where the kick-out
/// puts you are pinned in the page itself, in the source-shape idiom, because a replica that drifts from the
/// page is exactly the green test that asserts nothing.</para>
///
/// <para><b>Proven able to fail — all four, by breaking the shipped code and watching them go red.</b> A
/// guard that passes on the broken behaviour is the fifth named bug class on this ground, so each of these
/// was checked rather than reasoned about:</para>
///
/// <list type="bullet">
/// <item>Hoisting <c>TheRadioCall</c> above the clipboard gate — a run wired to a plain sighting —
/// reddens <see cref="ARunIsEarnedAndASightingStillOnlyBuysAHail"/>: <i>"the sighting loop starts a run
/// before it has asked why."</i></item>
/// <item>Swapping the kick-out's <c>RideTheLiftTo(ex, 0)</c> for a <c>StandCaptainAt</c> reddens
/// <see cref="TheKickOutRidesTheCarAndTakesThePassOutLoud"/>.</item>
/// <item>Raising <see cref="PatrolBeat.AfterYouSpeed"/> past the captain's own legs reddens
/// <see cref="OutrunningHimToTheCarEndsIt"/> on 22 floors — <i>"a captain running flat out for the car from
/// 135 du was caught."</i></item>
/// <item>Dropping the radio beat from the run reddens <see cref="HeCallsItInFirstAndThenHeHasYou"/>:
/// <i>"he moved while he was still saying it into the radio."</i></item>
/// </list>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheGuardsCatchYouTests
{
    private static readonly string[] Bodies = ["luna", "miranda", "titan", "europa"];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>A live rAF frame — the case the page actually runs in.</summary>
    private const double Dt = 1.0 / 60.0;

    /// <summary>The captain's own legs on the deck (<c>Map.Deck.AvatarSpeed</c>, which is private to the
    /// page). Named here rather than buried in a literal because the whole of rung five is the gap between
    /// this number and <see cref="PatrolBeat.AfterYouSpeed"/>.</summary>
    private const double CaptainSpeed = 9.0;

    /// <summary>One guard being come after — exactly the fields <c>Map.Patrol</c>'s own carries for a run.</summary>
    private sealed class Runner
    {
        public double X, Y, Vx, Vy, Facing, AfterYouFor, CallingIn = PatrolBeat.CallItInSeconds;
        public int WallSide = 1;
    }

    // ── THE RUN ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A CAPTAIN WHO STANDS THERE IS CAUGHT, AND NOT BEFORE THE RADIO HAS BEEN SAID. On every patrolled floor
    /// of every site: a man who has called it in from his own notice reach closes to a touch — the Old Ones'
    /// own homing step at a person's gait, over the captain's own walls — and the beat of radio he stands
    /// still for is real time the captain had.
    /// </summary>
    [Fact]
    public void HeCallsItInFirstAndThenHeHasYou()
    {
        var bad = new List<string>();
        int floors = 0, quickest = int.MaxValue, seenOnTheFan = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
                IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;

                (double shaftX, double shaftY) = UndergroundComplex.ShaftAt(Field);
                (double cx, double cy) = Clear((shaftX + 4.0, shaftY), walls);
                (double gx, double gy) = Clear((cx + (PatrolBeat.NoticeDu - 0.5), cy), walls);
                if (!PatrolBeat.Notices(gx, gy, cx, cy, walls))
                {
                    continue;   // no line of sight from there on this floor: he would never have called it in
                }
                floors++;

                var g = new Runner { X = gx, Y = gy };
                double fan = MotionTracker.UndergroundRange(MotionTracker.DetectionRange(32.0), level);

                int frames = 0, moving = 0;
                bool caught = false, movedDuringTheRadio = false;
                while (frames < (int)(PatrolBeat.AfterYouSecondsCap / Dt))
                {
                    frames++;
                    bool talking = g.CallingIn > 0;
                    (double wasX, double wasY) = (g.X, g.Y);

                    if (PatrolBeat.HasYou(g.X, g.Y, cx, cy))
                    {
                        caught = true;
                        break;
                    }
                    if (!PatrolBeat.StillAfterYou(g.AfterYouFor, g.X, g.Y, cx, cy))
                    {
                        break;   // he gave up on a captain who never moved: that is a finding
                    }

                    Chase(g, cx, cy, walls);

                    if (talking && (Math.Abs(g.X - wasX) > 1e-9 || Math.Abs(g.Y - wasY) > 1e-9))
                    {
                        movedDuringTheRadio = true;
                    }

                    if (MotionTracker.IsMoving(g.Vx, g.Vy))
                    {
                        moving++;
                        IReadOnlyList<MotionTracker.Blip> sweep = MotionTracker.Sweep(
                            cx, cy,
                            [new MotionTracker.Entity(g.X, g.Y, g.Vx, g.Vy, PatrolBeat.FanRegister)], fan);
                        double range = Math.Sqrt(((g.X - cx) * (g.X - cx)) + ((g.Y - cy) * (g.Y - cy)));
                        if (range <= fan)
                        {
                            seenOnTheFan++;
                            if (sweep.Count != 1 || sweep[0].Kind != MotionTracker.BlipKind.Crisp)
                            {
                                bad.Add($"  {body} B{-level}: a guard running {range:F0} du out, inside a " +
                                        $"{fan:F0} du fan, returned {sweep.Count} return(s) — a running man " +
                                        "is the loudest thing on the floor (#833's Aliens-device law).");
                            }
                        }
                    }
                }

                if (movedDuringTheRadio)
                {
                    bad.Add($"  {body} B{-level}: he moved while he was still saying it into the radio — " +
                            "that beat IS the warning, and spending it is spending the warning.");
                }

                if (!caught)
                {
                    bad.Add($"  {body} B{-level}: a captain who stood perfectly still was never caught — he " +
                            $"stopped {Math.Sqrt(((g.X - cx) * (g.X - cx)) + ((g.Y - cy) * (g.Y - cy))):F1} " +
                            $"du out after {frames} frames.");
                    continue;
                }

                if (moving < frames / 3)
                {
                    bad.Add($"  {body} B{-level}: he was moving on {moving} of {frames} frames of the run.");
                }
                quickest = Math.Min(quickest, frames);
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s) about the run:\n{string.Join("\n", bad)}");
        Assert.True(floors > 8, $"only {floors} restricted floors were run on.");
        Assert.True(seenOnTheFan > 200, $"only {seenOnTheFan} frames had the run inside the fan.");

        // …and the radio is real time. A catch that landed inside the beat he stands still for would be an
        // ambush wearing a warning.
        Assert.True(quickest > PatrolBeat.CallItInSeconds / Dt,
            $"the quickest catch took {quickest} frames against a radio beat of " +
            $"{PatrolBeat.CallItInSeconds / Dt:F0}.");
    }

    /// <summary>
    /// AND A CAPTAIN WHO RUNS FOR THE CAR GETS TO THE CAR. Rung five, measured rather than asserted: from the
    /// stop that stands furthest from the lift on every patrolled floor, a captain who turns and runs the
    /// route home on his own legs arrives without a hand ever reaching him.
    ///
    /// <para>The guard is NOT pathfound here and that is the page's own choice — he comes at you and grazes
    /// the walls (<c>ReeverChase.Step</c>'s handrail), which is why a corner is worth taking. Between that and
    /// nearly three deck units a second of leg, the escape is real on every floor this generator builds.</para>
    /// </summary>
    [Fact]
    public void OutrunningHimToTheCarEndsIt()
    {
        var bad = new List<string>();
        var tally = new List<string>();
        int floors = 0, home = 0;

        foreach (string body in Bodies)
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                if (!PatrolBeat.IsPatrolled(body, level))
                {
                    continue;
                }

                DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
                UndergroundComplex.FloorPlan plan = UndergroundComplex.Build(body, level, Field);
                IReadOnlyList<PatrolBeat.Stop> beat = PatrolBeat.BeatFor(body, level, 0, plan, Field);
                if (beat.Count < 2)
                {
                    continue;
                }

                IReadOnlyList<SurfaceCollision.Segment> walls = deck.CollisionField;
                (double carX, double carY) = HiveInterior.SpawnOn(Field);

                PatrolBeat.Stop far = beat[0];
                double worst = -1;
                foreach (PatrolBeat.Stop s in beat)
                {
                    double d = ((s.X - carX) * (s.X - carX)) + ((s.Y - carY) * (s.Y - carY));
                    if (d > worst)
                    {
                        worst = d;
                        far = s;
                    }
                }

                // He has just said it into the radio, standing where the round left him; the captain is a
                // pace clear of him and about to use the beat he is spending on it. That IS the start of a
                // run — nobody is ever come after from further than his own notice reach.
                (double gx, double gy) = Clear((far.X, far.Y), walls);
                (double cx, double cy) = Clear((gx + 2.0, gy), walls);
                double startOut = Math.Sqrt(((cx - carX) * (cx - carX)) + ((cy - carY) * (cy - carY)));
                if (startOut < 10 || PatrolBeat.HasYou(gx, gy, cx, cy))
                {
                    continue;   // a stop next to the car, or a square too tight to stand two people on
                }

                AutoWalk.Attempt run = AutoWalk.Plan(
                    true, new DeckReachability.Point(cx, cy), new DeckReachability.Point(carX, carY),
                    walls, DeckPlan.AvatarRadius,
                    PatrolBeat.LatticeFor(
                        new PatrolBeat.Stop(cx, cy, "here"), new PatrolBeat.Stop(carX, carY, "the car"),
                        Field));
                if (run.Route is null)
                {
                    bad.Add($"  {body} B{-level}: no route from the far stop to the car at all.");
                    continue;
                }
                floors++;

                var g = new Runner { X = gx, Y = gy };
                bool caught = false, arrived = false, lost = false;
                int frames = 0;

                while (frames < (int)(PatrolBeat.AfterYouSecondsCap / Dt))
                {
                    frames++;

                    if (PatrolBeat.HasYou(g.X, g.Y, cx, cy))
                    {
                        caught = true;
                        break;
                    }
                    if (!PatrolBeat.StillAfterYou(g.AfterYouFor, g.X, g.Y, cx, cy))
                    {
                        lost = true;
                        break;   // also an escape, and an honest one
                    }

                    Chase(g, cx, cy, walls);
                    (cx, cy) = Sprint(deck, run.Route, cx, cy);

                    double adx = carX - cx, ady = carY - cy;
                    if ((adx * adx) + (ady * ady) <= PatrolBeat.AtTheCarDu * PatrolBeat.AtTheCarDu)
                    {
                        arrived = true;
                        break;
                    }
                }

                if (caught)
                {
                    bad.Add($"  {body} B{-level}: a captain running flat out for the car from {startOut:F0} " +
                            $"du was caught after {frames} frames — the escape rung is not real on this " +
                            "floor.");
                    continue;
                }
                if (!arrived && !lost)
                {
                    bad.Add($"  {body} B{-level}: neither caught nor home nor lost after {frames} frames.");
                    continue;
                }
                home++;
                tally.Add($"{body} B{-level}: {startOut:F0} du, {(arrived ? "home" : "lost him")} in " +
                          $"{frames * Dt:F0} s");
            }
        }

        Assert.True(bad.Count == 0, $"{bad.Count} finding(s) about the escape:\n{string.Join("\n", bad)}");
        Assert.True(floors > 6, $"only {floors} floors had a run home worth measuring.");
        Assert.Equal(floors, home);
        Assert.True(tally.Count > 0);
    }

    // ── THE REPLICAS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>One frame of <c>Map.Patrol.RunAfterTheCaptain</c>, after its three exits.</summary>
    private static void Chase(
        Runner g, double cx, double cy, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        g.AfterYouFor += Dt;

        if (g.CallingIn > 0)
        {
            g.CallingIn -= Dt;
            g.Vx = 0;
            g.Vy = 0;
            return;
        }

        double startX = g.X, startY = g.Y;
        (g.X, g.Y) = ReeverChase.Step(
            g.X, g.Y, cx, cy, PatrolBeat.AfterYouSpeed * Dt, double.PositiveInfinity,
            walls, DeckPlan.AvatarRadius, g.WallSide, SurfaceCollision.Gait.Person);

        double mx = g.X - startX, my = g.Y - startY;
        g.Vx = mx / Dt;
        g.Vy = my / Dt;
        if ((mx * mx) + (my * my) > 1e-8)
        {
            g.Facing = Math.Atan2(my, mx);
        }
    }

    /// <summary>One frame of the captain's own auto-walk (<c>Map.Deck</c>'s sub-stepper), at his own speed,
    /// through <c>DeckPlan.Move</c> — the one primitive his body is ever stepped by.</summary>
    private static (double X, double Y) Sprint(DeckPlan deck, AutoWalk route, double x, double y)
    {
        double budget = CaptainSpeed * Dt;
        for (int step = 0; step < 8 && budget > 1e-9; step++)
        {
            if (!route.Active || !route.TryStep(x, y, budget, out double dx, out double dy))
            {
                break;
            }
            (double nx, double ny) = deck.Move(x, y, dx, dy);
            double mx = nx - x, my = ny - y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                route.Snag();
                break;
            }
            budget -= Math.Sqrt((mx * mx) + (my * my));
            (x, y) = (nx, ny);
        }
        return (x, y);
    }

    private static (double X, double Y) Clear(
        (double X, double Y) at, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        SpawnNudge.Result spot = SpawnNudge.Clear(at.X, at.Y, DeckPlan.AvatarRadius, walls);
        return spot.Failed ? at : (spot.X, spot.Y);
    }

    // ── THE PAGE ITSELF ───────────────────────────────────────────────────────────────────────────────

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new DirectoryNotFoundException($"could not find the repo root above {AppContext.BaseDirectory}");
    }

    private static string Pages(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", file));

    private static string Between(string text, string from, string to)
    {
        int start = text.IndexOf(from, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{from}' is no longer in the file this guard reads.");
        int end = text.IndexOf(to, start, StringComparison.Ordinal);
        Assert.True(end > start, $"'{to}' no longer follows '{from}' — this guard needs re-reading.");
        return text[start..end];
    }

    /// <summary>
    /// THE FILE WITH ITS COMMENTS TAKEN OUT — and it is not tidiness, it is the fifth named bug class.
    ///
    /// <para>A source-shape guard reads a text, and this file's texts are heavily doc-commented BY DESIGN:
    /// the prose above a method names the very calls the method makes. So <c>Contains("PatrolBeat.Notices(")</c>
    /// against the raw slice can be satisfied by a SENTENCE ABOUT the call rather than the call — an
    /// assertion that is right, handed a world that cannot tell pass from fail. Every pin below reads
    /// this.</para>
    /// </summary>
    private static string Code(string text)
    {
        var kept = new List<string>();
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                kept.Add(line);
            }
        }
        return string.Join("\n", kept);
    }

    private static int Count(string text, string needle)
    {
        int n = 0;
        for (int i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            n++;
        }
        return n;
    }

    /// <summary>
    /// A SIGHTING STILL ONLY BUYS A HAIL. The clause the old law lived in, pinned where the replica cannot
    /// see it: the sighting loop's road to a run is behind the CLIPBOARD (how many times this watch has
    /// already written you down), never behind <see cref="PatrolBeat.Notices"/> on its own — and every call
    /// into a run in the whole file passes a named <see cref="PatrolBeat.Provocation"/>.
    ///
    /// <para><b>Proven RED</b> by moving <c>TheRadioCall</c> above the <c>BookedTooOften</c> gate: the
    /// ordering assertion fails, which is the exact shape of "wire the chase to plain Notices".</para>
    /// </summary>
    [Fact]
    public void ARunIsEarnedAndASightingStillOnlyBuysAHail()
    {
        string patrol = Pages("Map.Patrol.cs");

        string sighting = Code(Between(
            patrol, "private void StopTheRoundIfAnybodySeesYou(", "private void TheHail("));

        // #833's beat is intact: a notice hails, and the hail is what the loop is for.
        Assert.Contains("PatrolBeat.Notices(", sighting, StringComparison.Ordinal);
        Assert.Contains("TheHail(g);", sighting, StringComparison.Ordinal);

        // …and the ONE road out of it into a run is gated on the clipboard, ABOVE the call.
        int gate = sighting.IndexOf("PatrolBeat.BookedTooOften(_escortsThisWatch)", StringComparison.Ordinal);
        int call = sighting.IndexOf("TheRadioCall(", StringComparison.Ordinal);
        Assert.True(gate >= 0, "the sighting loop no longer asks how many times you have been booked.");
        Assert.True(call > gate,
            "the sighting loop starts a run before it has asked why — that is a chase wired to a plain " +
            "sighting, which is the law #835 did NOT reverse.");

        // Every road into a run, counted: the declaration and exactly three seams. A fourth appearing is a
        // fourth trigger nobody ruled on.
        Assert.Equal(4, Count(Code(patrol), "TheRadioCall("));

        // Two of them name their provocation on the spot…
        Assert.Contains("TheRadioCall(g, PatrolBeat.Provocation.WalkedAwayTwice, index);", Code(patrol),
            StringComparison.Ordinal);
        Assert.Contains("TheRadioCall(g, PatrolBeat.Provocation.BookedTooManyTimes, i);", Code(patrol),
            StringComparison.Ordinal);

        // …and the third is the crime seam, whose one caller in the whole game names its own.
        string seam = Code(Between(patrol, "private void SomebodySawThat(", "// ── #835 · THE TOP RUNG"));
        Assert.Contains("PatrolBeat.EarnsIt(why)", seam, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.Notices(", seam, StringComparison.Ordinal);
        string combat = Code(Pages("Map.Combat.cs"));
        Assert.Contains("SomebodySawThat(Core.PatrolBeat.Provocation.SeenAtTheHasp);", combat,
            StringComparison.Ordinal);
        Assert.Equal(1, Count(combat, "SomebodySawThat("));

        // …and the call itself is gated, so a provocation of None can never move anybody.
        string radio = Code(Between(patrol, "private void TheRadioCall(", "/// #835 · ONE FRAME OF BEING COME"));
        Assert.Contains("if (!PatrolBeat.EarnsIt(why))", radio, StringComparison.Ordinal);

        // …and the first walk-away is still free: the count is taken and TESTED, not merely taken.
        string giveUp = Code(Between(patrol, "private void GiveUpTheHail(", "// ── THE CHALLENGE"));
        Assert.Contains("PatrolBeat.WalkingOffEarnsIt(++_walkedAwayThisWatch)", giveUp, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.WalkedAwayLine", giveUp, StringComparison.Ordinal);
    }

    /// <summary>
    /// BEING CAUGHT COSTS NOTHING THAT A BODY WOULD NOTICE. Owner, verbatim: <i>"just no damage by default
    /// :-D"</i>. The mechanic a hit would have to go through is <c>SurfaceExcursion.HitsTaken</c> /
    /// <c>CaptainCondition</c>, and neither appears anywhere in this file — so the guarantee is the absence of
    /// a spelling rather than a clause that could be edited around.
    /// </summary>
    [Fact]
    public void ACatchNeverTouchesTheCaptainsHealth()
    {
        string patrol = Pages("Map.Patrol.cs");

        // The spellings a hit would have to be written in. (The words themselves appear in this file's own
        // prose, saying that they do not appear in its code — which is the doc-comment the ruling asked for.)
        Assert.DoesNotContain("HitsTaken", Code(patrol), StringComparison.Ordinal);
        Assert.DoesNotContain("CaptainCondition", Code(patrol), StringComparison.Ordinal);

        // What it DOES cost: one pip of nerve, the lump the model already keeps for a hand laid on you.
        string caught = Code(Between(patrol, "private void HeHasYou(", "private void HeLosesYou("));
        Assert.Contains("NervePips.TouchPips * NervePips.PipUnit", caught, StringComparison.Ordinal);
        Assert.Equal(1, NervePips.TouchPips);

        // …and the card is the challenge card's own idiom, with the guard's own face on it (#804 / #684).
        Assert.Contains("PatrolBeat.TheGuardHasYou(g.Plate, why, _escortsThisWatch)", caught,
            StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.ChallengeArtUrl", caught, StringComparison.Ordinal);
        Assert.Contains("_escortDue = g;", caught, StringComparison.Ordinal);
    }

    /// <summary>
    /// A RUN ENDS IN A CATCH OR AN ESCAPE, NEVER IN A DESPAWN — and it never moves the captain. He keeps his
    /// own legs the whole way, which is the only reason "outrun him" is a sentence with keys behind it.
    /// </summary>
    [Fact]
    public void HeIsNeverRubbedOutAndYouAreNeverDragged()
    {
        string patrol = Pages("Map.Patrol.cs");

        string run = Code(Between(patrol, "private void RunAfterTheCaptain(", "private void HeHasYou("));
        Assert.Contains("ReeverChase.Step(", run, StringComparison.Ordinal);
        Assert.Contains("SurfaceCollision.Gait.Person", run, StringComparison.Ordinal);
        Assert.Contains("HeHasYou(ex, g);", run, StringComparison.Ordinal);
        Assert.Contains("HeLosesYou(g);", run, StringComparison.Ordinal);

        // The captain's body is never once written by the run — no placement, no shoulder, no tow.
        Assert.DoesNotContain("_avatarX =", run, StringComparison.Ordinal);
        Assert.DoesNotContain("_avatarY =", run, StringComparison.Ordinal);
        Assert.DoesNotContain("_deckPlan.Move(", run, StringComparison.Ordinal);
        Assert.DoesNotContain("StandCaptainAt(", run, StringComparison.Ordinal);

        // …and the list of guards is only ever emptied where a FLOOR ends, which is the one honest reason a
        // man stops being on it.
        Assert.Equal(1, Count(Code(patrol), "_guards.Clear()"));
        Assert.Equal(0, Count(Code(patrol), "_guards.Remove"));
        Assert.Contains("_guards.Clear()",
            Code(Between(patrol, "private void SpawnPatrolFor(", "// ── THE LOOP")), StringComparison.Ordinal);

        // The controls stay the captain's for a run. They are held for the walk OUT and for nothing else —
        // the three places #833 put them and no fourth.
        string deck = Code(Pages("Map.Deck.cs"));
        Assert.Equal(3, Count(deck, "CaptainIsUnderEscort"));
        Assert.DoesNotContain("AfterYou", deck, StringComparison.Ordinal);
    }

    /// <summary>
    /// THE KICK-OUT RIDES THE CAR THE PANEL RIDES, TAKES THE PASS, AND SAYS SO. No new machinery: the walk to
    /// the car is #833's walk, the ride is <c>RideTheLiftTo(ex, 0)</c> — the one transition between a floor
    /// and the regolith — and the plate over the shed is the descent stack's own three lines.
    ///
    /// <para><b>Proven RED</b> by swapping the ride for a placement: the <c>StandCaptainAt</c> count in this
    /// file goes to two, and the ride assertion fails.</para>
    /// </summary>
    [Fact]
    public void TheKickOutRidesTheCarAndTakesThePassOutLoud()
    {
        string patrol = Pages("Map.Patrol.cs");

        string kick = Code(Between(patrol, "private void TheKickOut(", "/// #835 · THE BIG TEXT"));

        // The pass leaves the satchel, and it is SAID as it goes — and only when there was one to take.
        Assert.Contains("Satchel.Remove(_satchel, Satchel.Kind.Badge, PatrolBeat.BadgeId(bodyId))", kick,
            StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.BadgeHeld(bodyId, _satchel)", kick, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.PassRevokedLine", kick, StringComparison.Ordinal);
        Assert.Contains("PatrolBeat.PassRevokedNote", kick, StringComparison.Ordinal);

        // The ride is the lift's own, and there is no placement anywhere on this road.
        Assert.Contains("RideTheLiftTo(ex, 0);", kick, StringComparison.Ordinal);
        Assert.DoesNotContain("StandCaptainAt(", kick, StringComparison.Ordinal);
        Assert.Equal(1, Count(Code(patrol), "StandCaptainAt("));   // still only #833's admitted cut

        // …and the owner's own last line lands last.
        Assert.Contains("PatrolBeat.DoorsCloseLine", kick, StringComparison.Ordinal);
        Assert.True(
            kick.IndexOf("PatrolBeat.DoorsCloseLine", StringComparison.Ordinal)
            > kick.IndexOf("RideTheLiftTo(ex, 0);", StringComparison.Ordinal),
            "the doors close before the car has gone up.");

        // The big text is the descent plate, in the descent plate's own three sizes, read off the same two
        // functions every other plate in the game reads them off.
        string plate = Code(Between(patrol, "TheKickedOutPlate(", "private void FadeTheKickedOutPlate("));
        Assert.Contains("PatrolBeat.KickedOutBigText", plate, StringComparison.Ordinal);
        Assert.Contains("UndergroundComplex.DepthPaint(0)", plate, StringComparison.Ordinal);
        Assert.Contains("SuitAir.PlateLine(air)", plate, StringComparison.Ordinal);
        Assert.Contains("44f", plate, StringComparison.Ordinal);
        Assert.Contains("MoonSurface.LiftHead(", plate, StringComparison.Ordinal);

        // …and which floor the walk ends on is decided ONCE, off the same predicate the card was composed
        // from — the sentence-vs-sim law, which this feature has already paid for twice.
        Assert.Equal(1, Count(Code(patrol), "_kickOutDue = PatrolBeat.BookedTooOften(_escortsThisWatch);"));
    }

    /// <summary>#835 · The plate is a MOMENT, not wallpaper: one deck carries it, and one rebuild takes it
    /// away again. It rides outside the surface deck's memo (#371) because a sign that is up for fourteen
    /// seconds is not the ground.</summary>
    [Fact]
    public void TheSignComesDownAgain()
    {
        string patrol = Pages("Map.Patrol.cs");
        string fade = Code(Between(patrol, "private void FadeTheKickedOutPlate(", "// ── DRAWING THEM"));
        Assert.Contains("RebuildSurfaceDeck();", fade, StringComparison.Ordinal);
        Assert.Contains("_kickedOutPlateFor = 0;", fade, StringComparison.Ordinal);

        // …and it is asked for on the one rebuild that draws the regolith.
        Assert.Contains("bigLabels: TheKickedOutPlate(ex)", Code(Pages("Map.Surface.cs")), StringComparison.Ordinal);
    }
}

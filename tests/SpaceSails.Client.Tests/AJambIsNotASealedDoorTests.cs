using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #724 · A DOORWAY WITH NO FUNNEL. Owner, playing <c>/map?secretlab=deep&amp;land=1&amp;floor=1</c>: <i>"walk
/// up the first rib to CANTEEN 1's west face, then walk straight at the doorway… I pressed D thirty-five
/// times against the wall with zero movement, and the screen gives no hint the door is half a step north."</i>
///
/// <para>The door was never sealed and the cut was never narrow: a Hive doorway is
/// <see cref="UndergroundComplex.DoorHalf"/> × 2 = 6.4 du of clear opening and a body is
/// <see cref="DeckPlan.AvatarRadius"/> × 2 = 1.4 du across. The captain was simply a body-width off the
/// mouth, and axis-separated collision converted the entire press into nothing. On the deck-plan zoom a jamb
/// and a doorway are a few pixels apart, so that reads as <b>the door is sealed</b> — a false locked-door
/// tell in the one game whose locked doors are load-bearing storytelling.</para>
///
/// <para>THE LAW, in the issue's own words: <i>"a captain one body-width off a doorway cut, pressing only the
/// axis through it, must pass within N steps."</i> Stated here on REAL generated geometry — the same
/// <see cref="UndergroundComplex.Build"/> floors the client walks, through the same
/// <see cref="DeckPlan.Move"/> the arrow keys dispatch — because a hand-typed pair of walls would only ever
/// prove that the arithmetic does what the arithmetic does. The synthetic companion cases (a wall list you
/// can read in one glance, including the ones where the funnel must NOT fire) live beside the primitive, in
/// <c>SurfaceCollisionTests</c>.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class AJambIsNotASealedDoorTests
{
    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>The floor the owner was standing on when they filed it.</summary>
    private const string ReproBody = "secret-lab-site-unlisted";
    private const int ReproLevel = -1;

    /// <summary>Where the walk starts: far enough back that the captain is genuinely walking down a corridor
    /// at the door rather than being placed in its mouth, close enough to stay inside the rib.</summary>
    private const double RunUp = 4.0;

    /// <summary>One press, at the speed the deck actually walks: 9 du/s at a 60 Hz frame. The law must not
    /// depend on it, so every case below is run at this AND at a third of it.</summary>
    private const double PressStep = 0.15;

    /// <summary>How many presses count as "within N steps". 400 × 0.15 du is sixty deck units of walking to
    /// cross four — absurdly generous on purpose, because the failure being guarded is not slowness, it is a
    /// captain who never moves again. The owner gave it thirty-five.</summary>
    private const int Presses = 400;

    /// <summary>How far off the cut's centreline a captain must still be taken through, and it is a
    /// geometric statement rather than a tuned number: <b>if any part of the body is over the opening, the
    /// opening takes it.</b> The body's near edge is still on the cut right up to one radius past the jamb,
    /// so the band is the cut's own half-width plus a radius. Past it the captain is squarely in front of
    /// poured stone, and stone is allowed to stop them — that case is asserted too, below.</summary>
    private static double BandHalf => UndergroundComplex.DoorHalf + DeckPlan.AvatarRadius;

    /// <summary>#817 · How far along a wall the sidestep can see — <c>SurfaceCollision</c>'s own
    /// <c>GapReachInRadii</c> of two radii, restated here because it is private there. Used only to decide
    /// whether a stretch of wall is stone for far enough either side to BE an experiment about a wall; it
    /// never widens or narrows a law.</summary>
    private static double FunnelReach => 2.0 * DeckPlan.AvatarRadius;

    /// <summary>How finely that reach is sounded when asking whether a stretch of wall is really solid.</summary>
    private const int FunnelProbes = 8;

    /// <summary>How many offsets the band is sounded at. Twenty-one samples over 3.9 du, and — because it
    /// divides exactly — the last of them sits ON the edge of the law rather than safely inside it.</summary>
    private const int BandSamples = 20;

    /// <summary>Walks that actually happened. A guard whose every case was skipped is the fifth named bug
    /// class wearing a green tick, and every case here begins with a standability check that COULD skip it.
    /// So the sweeps count, and the count is asserted.</summary>
    private int _walked;

    [Fact]
    public void AtTheCanteenDoor_ThirtyFivePressesOfTheThroughAxis_ActuallyGoSomewhere()
    {
        // The repro floor, doorway by doorway. Every offset across the band, both hands off the cut's
        // centreline, both approach sides, both press sizes.
        var bad = new List<string>();
        Sweep(ReproBody, ReproLevel, bad);
        Report(bad, $"the doorways of {ReproBody} B{-ReproLevel} — the floor #724 was filed from", 300);
    }

    [Fact]
    public void AndEveryOtherDoorwayInEveryOtherHiveToo()
    {
        // A look tells you about the door you happened to walk at; a flood tells you about every doorway of
        // every clandestine site in the system. The bug was never specific to the canteen — it was the
        // collision primitive — so the law is stated where it lives.
        var bad = new List<string>();
        foreach (string body in new[] { "luna", "phobos", "europa", "titan", "miranda", "the-clinker" })
        {
            foreach (int level in UndergroundComplex.FloorsOf(body))
            {
                Sweep(body, level, bad);
            }
        }
        Report(bad, "every doorway of every clandestine site", 5000);
    }

    /// <summary>#724 · THE LAW AS IT STOOD THE MOMENT BEFORE THIS PR, written out by hand.
    ///
    /// <para>Normally a test that re-types a law instead of calling it is this project's mirrored-constant
    /// bug wearing a lab coat. Here it is the entire point: the owner's ruling is that an Old One must move
    /// <b>no easier than it did before</b>, and the only exact way to say "no easier than before" is to keep
    /// a copy of before. This one is frozen on purpose. It must NEVER be updated to track
    /// <see cref="SurfaceCollision.Slide"/>; if a future change makes the shamble diverge from it, that is
    /// the guard doing its job, not the guard going stale.</para></summary>
    private static Stepper AsItWasBefore724(DeckPlan deck) => (x, y, dx, dy) =>
    {
        double nx = deck.Collides(x + dx, y) ? x : x + dx;
        double ny = deck.Collides(nx, y + dy) ? y : y + dy;
        return (nx, ny);
    };

    [Fact]
    public void AnOldOneAtTheSameDoorwaysIsNOTHelpedThrough()
    {
        // ── #724 · THE OWNER'S RULING, ON THE REAL FLOOR ───────────────────────────────────────────────
        //
        // "Lets not help reevers move in any easier if possible, but other than that lets merge away."
        // (2026-08-06.) And, from the same week on #729: "let's not make them walk too sensibly… they have
        // their own reever-mind-issues, the kind of way they walk is nice and spooky now. How they tend to
        // form that big mob."
        //
        // Note what the ruling is NOT. It is not "an Old One may not use a door" — walking through an open
        // hole is just walking, and they have always done it, and a guard that demanded otherwise would be
        // demanding a regression. It is "no easier than before". So that is what is asserted, exactly and
        // without a threshold anywhere in it: over every case of the sweep above, the shamble's position
        // must equal — to the last bit, on every single press — the position the law gave BEFORE this PR
        // existed. Not "roughly the same outcome"; the same walk.
        //
        // And because a guard that can only ever agree with itself proves nothing (the fifth named bug
        // class), the same sweep counts what the captain WON: the presses on which the boots parted from
        // that old law, and the approaches the boots complete that the old law never could. Both must be
        // substantial, or the funnel is not firing and this test is three walks agreeing about nothing.
        //
        // The stagger is not a limitation waiting to be tidied away. It is load-bearing horror, and it is
        // the dodge-skill surface a captain out of rounds plays against: you shed a shambler on an obstacle
        // exactly because the shambler cannot get round it. Green here is the design.
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(ReproBody, ReproLevel, Field);
        DeckPlan deck = HiveInterior.FloorDeck(ReproBody, ReproLevel, Field, 0, (_, _) => { }, []);
        Stepper boots = BootsOn(deck), shamble = ShambleOn(deck), before = AsItWasBefore724(deck);

        var bad = new List<string>();
        int walked = 0, bootsParted = 0, bootsWon = 0;

        foreach (SurfaceLayout.Doorway cut in floor.Doorways)
        {
            bool cutRunsAlongX = Math.Abs(cut.X1 - cut.X2) > Math.Abs(cut.Y1 - cut.Y2);
            double alongCentre = cutRunsAlongX ? (cut.X1 + cut.X2) / 2 : (cut.Y1 + cut.Y2) / 2;
            double wallAt = cutRunsAlongX ? cut.Y1 : cut.X1;

            for (int i = 0; i <= BandSamples; i++)
            {
                double off = BandHalf * i / BandSamples;
                foreach (int hand in new[] { -1, 1 })
                {
                    foreach (int side in new[] { -1, 1 })
                    {
                        double along = alongCentre + (off * hand);
                        double sx = cutRunsAlongX ? along : wallAt - (RunUp * side);
                        double sy = cutRunsAlongX ? wallAt - (RunUp * side) : along;
                        if (deck.Collides(sx, sy))
                        {
                            continue;
                        }
                        walked++;

                        double dx = cutRunsAlongX ? 0 : PressStep * side;
                        double dy = cutRunsAlongX ? PressStep * side : 0;
                        (double bx, double by) = (sx, sy);
                        (double ox, double oy) = (sx, sy);
                        (double wx, double wy) = (sx, sy);
                        bool parted = false, bootsThrough = false, oldLawThrough = false;
                        string? drift = null;

                        for (int p = 0; p < Presses; p++)
                        {
                            (bx, by) = boots(bx, by, dx, dy);
                            (ox, oy) = shamble(ox, oy, dx, dy);
                            (wx, wy) = before(wx, wy, dx, dy);

                            // THE RULING. Not a tolerance: the same numbers.
                            if (drift is null && (ox != wx || oy != wy))
                            {
                                drift = $"press {p}: the shamble stood at ({ox:F4},{oy:F4}) where the old law "
                                    + $"put it at ({wx:F4},{wy:F4})";
                            }

                            parted |= bx != wx || by != wy;
                            bootsThrough |= ((cutRunsAlongX ? by : bx) - wallAt) * side > 0;
                            oldLawThrough |= ((cutRunsAlongX ? wy : wx) - wallAt) * side > 0;
                        }

                        if (parted)
                        {
                            bootsParted++;
                        }
                        if (bootsThrough && !oldLawThrough)
                        {
                            bootsWon++;
                        }
                        if (drift is not null)
                        {
                            bad.Add($"  cut at ({cut.X1:F1},{cut.Y1:F1}) · {off * hand:+0.00;-0.00;0.00} du off "
                                + $"the centreline · from {(side < 0 ? "below" : "above")} — {drift}");
                        }
                    }
                }
            }
        }

        Assert.True(bad.Count == 0,
            $"{bad.Count} approach(es) move an Old One somewhere the pre-#724 law would not have:"
                + Environment.NewLine + string.Join(Environment.NewLine, bad.Take(12)));

        // …and the same sweep proves it was not three identical walks agreeing about nothing.
        Assert.True(walked >= 150, $"only {walked} approach(es) started — this proved nothing.");
        Assert.True(bootsParted >= 50,
            $"the captain's walk parted from the pre-#724 law on only {bootsParted} of {walked} approaches — "
                + "the funnel is barely firing, so agreeing with the old law proves nothing about the shamble.");
        Assert.True(bootsWon >= 20,
            $"the funnel opened only {bootsWon} approach(es) the old law could not walk — too few for "
                + "'the Old Ones did not get this' to be a claim about anything.");
    }

    [Fact]
    public void NoOldOneIsEverHandedThePersonsGait_AndTheCaptainClaimsItInExactlyOnePlace()
    {
        // ── #724 · THE RULING, GUARDED STRUCTURALLY AS WELL AS BEHAVIOURALLY ───────────────────────────
        //
        // The sweep above proves the Old Ones do not have the funnel TODAY. This proves the next crew
        // cannot hand it to them by accident — which is the failure mode a behavioural guard is worst at,
        // because a new Reever mover would arrive with no test of its own and every existing test still
        // green. Owner: "Lets not help reevers move in any easier if possible."
        //
        // Two claims, and they are the whole of the seam:
        //   1. No file that moves an Old One ever names Gait.Person.
        //   2. Exactly ONE line in the shipping source claims it for the captain — DeckPlan.Move — which is
        //      what makes "#738's AutoWalk inherits the funnel" a fact rather than a hope: that guard
        //      (TheAutoWalkIsWiredToTheRealLegsTests) already proves every clicked sub-step is spent
        //      through _deckPlan.Move and nothing else, and this proves Move is where the gait lives.
        string[] oldOneMovers =
        [
            Path.Combine(RepoRoot, "src", "SpaceSails.Core", "ReeverChase.cs"),
            Path.Combine(RepoRoot, "src", "SpaceSails.Core", "ReeverPack.cs"),
        ];
        foreach (string file in oldOneMovers)
        {
            Assert.True(File.Exists(file), $"{file} has moved — this guard cannot see what it guards.");
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                // The doc comments explain the ruling and must be free to name the thing being withheld;
                // it is CODE handing it over that is forbidden.
                string line = lines[i].TrimStart();
                if (line.StartsWith("///", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }
                Assert.False(line.Contains("Gait.Person", StringComparison.Ordinal),
                    $"{Path.GetFileName(file)}:{i + 1} hands an Old One the person's gait — "
                        + "\"lets not help reevers move in any easier\" (owner, 2026-08-06).");
            }
        }

        var claims = new List<string>();
        foreach (string path in Directory.EnumerateFiles(
                     Path.Combine(RepoRoot, "src"), "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;   // generated
            }
            if (Path.GetFileName(path) == "SurfaceCollision.cs")
            {
                continue;   // the seam itself: it DEFINES the gait and dispatches on it, it claims nothing
            }
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimStart();
                if (line.StartsWith("///", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }
                if (line.Contains("Gait.Person", StringComparison.Ordinal))
                {
                    claims.Add($"{Path.GetFileName(path)}:{i + 1}");
                }
            }
        }

        // The captain (DeckPlan.Move) and the #618 black-ops sweepers, who are people on somebody's payroll
        // — owner, #729: "maybe the guards can use A* also so they do not come off as reevers". Anything
        // else appearing here is a new mover that has quietly claimed a talent, and wants a ruling first.
        //
        // #804 · AND THE THIRD ONE, WHICH IS THE RULING THIS GUARD EXISTS TO FORCE. The rounds walking the
        // Hive's restricted floors are the sentence above wearing a uniform: a contract guard is a person on
        // a rota, they walk A* between published stops precisely so they do not read as Old Ones, and the
        // owner's own #804 sentence is the same one — "ideally we could see them move and wait for them to
        // pass". They get the funnel for the sweep team's reason and no other. THE OLD ONES STILL DO NOT,
        // which is the whole law here and is checked file by file above.
        //
        // #835 · AND THE FOURTH CLAIM IS THE SAME MAN RUNNING. Owner, reversing the no-chase law with the
        // implementation named: "they need to catch us .... like reevers :-D we could use that code :-D" —
        // so a guard now spends a run through ReeverChase.Step, the Old Ones' own homing step, and hands it
        // Gait.Person as the ONE thing a uniform changes about it. That is this guard forcing its ruling and
        // getting one, exactly as #804 did: the gait travels with the MOVER (a person on a rota, however
        // fast he is going) and never with the machinery. ReeverChase.cs itself is unchanged and is still
        // checked, line by line, above — it defaults to the stagger, so nothing an Old One steps through was
        // helped by any of this.
        //
        // #831 · AND THE FIFTH IS THE SAME MAN STOPPING. A made tail performs a cover act instead of freezing
        // bare — owner: "turns to the nearest wall fixture, checks a plate, reads a docket" — and the few du
        // he takes to get to one are a slide, not a plan. It is the SAME body that already holds three of
        // these claims, walking two paces at the same gait for the same reason, and it is the third and last
        // of them in the round's own files. Nothing an Old One steps through was touched.
        Assert.Equal(5, claims.Count);
        Assert.Contains(claims, c => c.StartsWith("DeckPlan.cs:", StringComparison.Ordinal));
        Assert.Contains(claims, c => c.StartsWith("Map.SweepTeam.cs:", StringComparison.Ordinal));

        // Three times, and every one of them a contract guard: the round's own stride, #835's run, and #831's
        // two paces to the wall he has decided to read.
        //
        // #870 · The round is six partials by subject now, so the prefix names the FAMILY rather than one
        // file of it — three claims across all of them, exactly the three it counted when they were one.
        //
        // #870 lane 6′c · RE-PATHED. The verbs moved off the page onto the round itself, so the family's
        // own files are `Patrol*.cs` under Pages/Patrol/ and the prefix is `Patrol.` — which is exactly
        // those files and never `Map.Patrol.cs`, whose forwarders claim nothing. Still three, still the
        // round's stride, #835's run and #831's two paces to the wall.
        Assert.Equal(3, claims.FindAll(c => c.StartsWith("Patrol.", StringComparison.Ordinal)).Count);
    }

    private static string RepoRoot
    {
        get
        {
            string? dir = AppContext.BaseDirectory;
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir, "src", "SpaceSails.Client")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException("could not find the repo root from the test binary.");
        }
    }

    [Fact]
    public void ButAWallIsStillAWall_AndPressingIntoOneGetsYouNothing()
    {
        // THE OTHER HALF OF THE LAW, and the one that keeps the fix from being "walk through walls". Beyond
        // the band no part of the captain is over the opening any more — they are square in front of poured
        // stone, and a facility whose walls funnel you sideways from three metres away would be a facility
        // with no walls. This is also the case that would go red first if the reach were ever quietly widened
        // to make some other floor pass.
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(ReproBody, ReproLevel, Field);
        DeckPlan deck = HiveInterior.FloorDeck(ReproBody, ReproLevel, Field, 0, (_, _) => { }, []);
        var through = new List<string>();
        int tried = 0;

        foreach (SurfaceLayout.Doorway cut in floor.Doorways)
        {
            bool cutRunsAlongX = Math.Abs(cut.X1 - cut.X2) > Math.Abs(cut.Y1 - cut.Y2);
            double alongCentre = cutRunsAlongX ? (cut.X1 + cut.X2) / 2 : (cut.Y1 + cut.Y2) / 2;
            double wallAt = cutRunsAlongX ? cut.Y1 : cut.X1;

            // A full body-width clear of the band: the near edge of the body is a whole body off the cut.
            foreach (double off in new[] { BandHalf + (2 * DeckPlan.AvatarRadius), BandHalf + (4 * DeckPlan.AvatarRadius) })
            {
                foreach (int hand in new[] { -1, 1 })
                {
                    double along = alongCentre + (off * hand);
                    double x = cutRunsAlongX ? along : wallAt - RunUp;
                    double y = cutRunsAlongX ? wallAt - RunUp : along;
                    if (deck.Collides(x, y))
                    {
                        continue;
                    }

                    // …AND THERE HAS TO BE A WALL THERE TO BE STOPPED BY.
                    //
                    // #775 put a second opening within a body's width of some of these cuts: the hall's own
                    // front doors are cut into the spine's face a few du from a rib's mouth, and the garden
                    // walk stands near the end of the field. So an approach offset sideways can now start in
                    // front of ANOTHER hole — or off the end of the building altogether — and walking
                    // through THAT proves nothing about walls. Asked of the deck rather than assumed: if the
                    // line is not solid at this offset, this is not an experiment about a wall.
                    if (!deck.Collides(cutRunsAlongX ? along : wallAt, cutRunsAlongX ? wallAt : along))
                    {
                        continue;
                    }

                    // #817 · …AND ONE POINT ON THAT LINE IS NOT ENOUGH TO CALL IT A WALL.
                    //
                    // The paragraph above is right about WHY and was too weak about HOW. It asks whether
                    // the body's own centre is on stone. The funnel asks a strictly wider question — is
                    // there a way through within a sidestep — and it does not care whether that way is
                    // another office's front door, a rib's mouth or the end of the building. So a captain
                    // can be squarely on stone AND one stride from something they are entitled to walk
                    // through, and walking through it proves nothing about walls: it is a captain finding a
                    // door, which is the OTHER half of this file cheering.
                    //
                    // #817 made that case common rather than rare — a room's door count scales with its
                    // street frontage now, and #822 puts a fire-code floor of two under it, so a 20 du
                    // office carries two leaves with a 3.6 du pier between them. Watched go red on six
                    // approaches, every one of them a captain who had correctly found the next way through.
                    //
                    // So the line is sounded across the whole reach the sidestep can see, at the same
                    // radius the body has. If any of it is open, this is not an experiment about a wall.
                    bool solidAcrossTheReach = true;
                    for (int probe = -FunnelProbes; probe <= FunnelProbes; probe++)
                    {
                        double at = along + (FunnelReach * probe / FunnelProbes);
                        solidAcrossTheReach &= deck.Collides(
                            cutRunsAlongX ? at : wallAt, cutRunsAlongX ? wallAt : at);
                    }
                    if (!solidAcrossTheReach)
                    {
                        continue;
                    }

                    tried++;
                    double dx = cutRunsAlongX ? 0 : PressStep, dy = cutRunsAlongX ? PressStep : 0;
                    for (int i = 0; i < Presses; i++)
                    {
                        (x, y) = deck.Move(x, y, dx, dy);
                    }
                    if ((cutRunsAlongX ? y : x) > wallAt)
                    {
                        through.Add($"  {off * hand:+0.00;-0.00} du off the cut at ({cut.X1:F1},{cut.Y1:F1}) — "
                            + "the captain walked out the far side of solid wall.");
                    }
                }
            }
        }

        Assert.True(tried >= 12, $"only {tried} approach(es) started — this proved nothing about walls.");
        Assert.True(through.Count == 0, string.Join(Environment.NewLine, through));
    }

    private void Sweep(string body, int level, List<string> bad)
    {
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(body, level, Field);
        DeckPlan deck = HiveInterior.FloorDeck(body, level, Field, 0, (_, _) => { }, []);
        Stepper step = BootsOn(deck);

        foreach (SurfaceLayout.Doorway cut in floor.Doorways)
        {
            // A cut is a segment in the wall it was taken out of, so the axis it is DRAWN along is the axis
            // you stand off, and the other one is the axis you walk through it on.
            bool cutRunsAlongX = Math.Abs(cut.X1 - cut.X2) > Math.Abs(cut.Y1 - cut.Y2);
            double alongCentre = cutRunsAlongX ? (cut.X1 + cut.X2) / 2 : (cut.Y1 + cut.Y2) / 2;
            double wallAt = cutRunsAlongX ? cut.Y1 : cut.X1;

            for (int i = 0; i <= BandSamples; i++)
            {
                double off = BandHalf * i / BandSamples;
                foreach (int hand in new[] { -1, 1 })
                {
                    foreach (int side in new[] { -1, 1 })
                    {
                        foreach (double press in new[] { PressStep, PressStep / 3 })
                        {
                            Walk(deck, step, cutRunsAlongX, alongCentre + (off * hand), wallAt, side, press,
                                $"{body} B{-level} cut at ({cut.X1:F1},{cut.Y1:F1})-({cut.X2:F1},{cut.Y2:F1})"
                                    + $" · {off * hand:+0.00;-0.00;0.00} du off the centreline"
                                    + $" · approached from {(side < 0 ? "below" : "above")} at {press:F2} du/press",
                                bad);
                        }
                    }
                }
            }
        }
    }

    /// <summary>One mover's step, so a case can be walked by the CAPTAIN (through the real
    /// <see cref="DeckPlan.Move"/> the arrow keys and #738's AutoWalk both dispatch through) or by an Old
    /// One (through the same Core seam its chase steps through), with nothing else about the case changing.
    /// </summary>
    private delegate (double X, double Y) Stepper(double x, double y, double dx, double dy);

    private static Stepper BootsOn(DeckPlan deck) => deck.Move;

    private static Stepper ShambleOn(DeckPlan deck) =>
        (x, y, dx, dy) => SurfaceCollision.Slide(
            x, y, dx, dy, DeckPlan.AvatarRadius, deck.CollisionField, OldOneGait);

    /// <summary>#724 · The gait an Old One is stepped with, named here so the red-proof for the ruling is a
    /// ONE-WORD edit: swap this to <see cref="SurfaceCollision.Gait.Person"/> and
    /// <see cref="AnOldOneAtTheSameDoorwaysIsNOTHelpedThrough"/> must go red.</summary>
    private const SurfaceCollision.Gait OldOneGait = SurfaceCollision.Gait.Stagger;

    private void Walk(
        DeckPlan deck, Stepper step, bool cutRunsAlongX, double along, double wallAt, int side, double press,
        string what, List<string> bad)
    {
        // Stand back from the wall on the chosen side, level with the offset being tested.
        double x = cutRunsAlongX ? along : wallAt - (RunUp * side);
        double y = cutRunsAlongX ? wallAt - (RunUp * side) : along;

        // #600's lesson, both ends of it: a walk that starts inside stone proves nothing either way, and a
        // corridor that has no room to stand at this offset is not this test's business.
        if (deck.Collides(x, y))
        {
            return;
        }
        _walked++;

        // Pure through-axis, exactly as the owner pressed it: no second key, ever.
        double dx = cutRunsAlongX ? 0 : press * side, dy = cutRunsAlongX ? press * side : 0;
        for (int i = 0; i < Presses; i++)
        {
            (x, y) = step(x, y, dx, dy);
            double across = cutRunsAlongX ? y : x;
            if ((across - wallAt) * side > 0)
            {
                return;   // through the door
            }
        }

        double ended = cutRunsAlongX ? y : x;
        bad.Add($"  {what}: {Presses} presses left the captain at {ended.ToString("F2", CultureInfo.InvariantCulture)}, "
            + $"still on the near side of the wall at {wallAt.ToString("F2", CultureInfo.InvariantCulture)}.");
    }

    private void Report(List<string> bad, string what, int atLeast)
    {
        Assert.True(_walked >= atLeast,
            $"only {_walked} walk(s) ever started — this sweep proved nothing about {what}.");
        if (bad.Count == 0)
        {
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"{bad.Count} approach(es) pinned on the jamb — {what}:");
        foreach (string line in bad.Take(12))
        {
            sb.AppendLine(line);
        }
        if (bad.Count > 12)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  …and {bad.Count - 12} more.");
        }
        Assert.Fail(sb.ToString());
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #803 · HAND-LOAD THE FOUND ROUNDS AND POINT THE GUN.
///
/// <para>Owner, 2026-08-09: <i>"we might want to hand-load them into the bots for some special purposes,
/// like shooting a mechanical lock (we will need to use the handheld captain's control to set the guns /
/// gun to fire at something manually, that UI is missing)."</i></para>
///
/// <para>Every guard in here was watched go RED against the behaviour it exists to forbid before it was
/// allowed to go green — this repo has a named bug class for a guard that passes on the broken world
/// (#587), and a second one for a guard whose WORLD cannot tell pass from fail.</para>
/// </summary>
public sealed class PointTheGunTests
{
    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    // ── THE PUT VERB ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HAND-LOADING MOVES REAL ROUNDS, AND NOTHING EVAPORATES ON THE WAY.
    ///
    /// <para>Swept over every drum state and every plausible find, because the interesting arithmetic is at
    /// the ceiling and a single hand-picked pair proves nothing about it. Two laws: what the drum takes plus
    /// what stays in the pocket is EXACTLY what was offered, and the drum never holds a number the two-digit
    /// readout cannot report.</para>
    ///
    /// <para>RED against the old client line (<c>gun.Rounds += fed.Count</c>): a bot at 90 handed 22 rounds
    /// ends at 112, <c>SentryBot.Readout</c> prints 99, and the HUD and the machine disagree about one
    /// number for the rest of the excursion.</para></summary>
    [Fact]
    public void EveryRoundOfferedIsEitherInTheDrumOrStillInThePocket()
    {
        for (int have = 0; have <= SentryBot.MaxMagazine; have++)
        {
            foreach (int offered in new[] { 1, 6, 12, 22, 40, 99, 140 })
            {
                SentryHandLoad.Load load = SentryHandLoad.Offer(
                    "K-77", have, Ammunition.Issue.Id, deployed: true, offered, Ammunition.Issue.Id);

                Assert.Equal(offered, load.Accepted + load.LeftOver);
                Assert.True(load.Magazine <= SentryBot.MaxMagazine,
                    $"{have} + {offered} left {load.Magazine} in a drum the readout tops out at " +
                    $"{SentryBot.MaxMagazine}.");
                Assert.Equal(have + load.Accepted, load.Magazine);
                Assert.True(load.Accepted >= 0 && load.LeftOver >= 0);
            }
        }
    }

    /// <summary>
    /// …AND THE #797 INSTRUMENT AGREES WITH THE DRUM, DIGIT FOR DIGIT.
    ///
    /// <para>One source: the magazines line the on-foot HUD prints is asked of the same roster the hand-load
    /// just moved, so if a load could ever put a number in a drum the readout cannot say, this is where it
    /// shows. (#728's whole disease was a receipt paid into an account with no statement; the account exists
    /// now and this keeps the two in step.)</para></summary>
    [Fact]
    public void TheMagazinesReadoutSaysWhatTheHandLoadPutThere()
    {
        SentryHandLoad.Load load = SentryHandLoad.Offer(
            "K-77", 11, Ammunition.Issue.Id, deployed: true, 6, Ammunition.Issue.Id);
        Assert.True(load.Worked);
        Assert.Equal(17, load.Magazine);

        string line = SentryBot.MagazinesReadout(
            [new SentryBot.Carried("K-77", load.Magazine, Deployed: true)]);
        Assert.Contains($"K-77 {SentryBot.Readout(load.Magazine)}/{SentryBot.MaxMagazine}", line);
        Assert.Contains("set down", line);
    }

    /// <summary>EVERY REFUSAL NAMES ITS REASON, AND THEY ARE FOUR DIFFERENT REASONS. A control that does
    /// nothing and says nothing is indistinguishable from a bug; four controls that say the SAME nothing are
    /// worse, because the captain learns the sentence and stops reading it.</summary>
    [Fact]
    public void TheFourRefusalsAreFourDifferentSentences()
    {
        SentryHandLoad.Load nothing = SentryHandLoad.Offer(
            "K-77", 10, Ammunition.Issue.Id, deployed: true, 0, Ammunition.Issue.Id);
        SentryHandLoad.Load slung = SentryHandLoad.Offer(
            "K-77", 10, Ammunition.Issue.Id, deployed: false, 6, Ammunition.Issue.Id);
        SentryHandLoad.Load full = SentryHandLoad.Offer(
            "K-77", SentryBot.MaxMagazine, Ammunition.Issue.Id, deployed: true, 6, Ammunition.Issue.Id);
        SentryHandLoad.Load mixed = SentryHandLoad.Offer(
            "K-77", 10, Ammunition.Issue.Id, deployed: true, 6, Ammunition.LabTwoStage.Id);

        foreach (SentryHandLoad.Load refused in new[] { nothing, slung, full, mixed })
        {
            Assert.False(refused.Worked);
            Assert.Equal(0, refused.Accepted);
            Assert.False(string.IsNullOrWhiteSpace(refused.Line));
        }

        // Nothing was taken out of the pocket by any of them.
        Assert.Equal(6, slung.LeftOver);
        Assert.Equal(6, full.LeftOver);
        Assert.Equal(6, mixed.LeftOver);

        var said = new HashSet<string>(
            new[] { nothing.Line, slung.Line, full.Line, mixed.Line }, StringComparer.Ordinal);
        Assert.Equal(4, said.Count);
    }

    /// <summary>A DRY DRUM TAKES ANYTHING; A LOADED ONE TAKES MORE OF THE SAME. The mixing rule has real
    /// teeth — #603 hangs one-shot kills, four-deep penetration and a lethal minimum range off what a
    /// magazine is loaded with, and a drum holding two kinds could not answer what the next pull does.</summary>
    [Fact]
    public void ADryDrumTakesTheLabRoundAndThenOnlyMoreOfIt()
    {
        SentryHandLoad.Load first = SentryHandLoad.Offer(
            "R-3B", 0, Ammunition.Issue.Id, deployed: true, 6, Ammunition.LabTwoStage.Id);
        Assert.True(first.Worked);
        Assert.Equal(Ammunition.LabTwoStage.Id, first.AmmoId);

        SentryHandLoad.Load more = SentryHandLoad.Offer(
            "R-3B", first.Magazine, first.AmmoId, deployed: true, 4, Ammunition.LabTwoStage.Id);
        Assert.True(more.Worked);

        SentryHandLoad.Load wrong = SentryHandLoad.Offer(
            "R-3B", first.Magazine, first.AmmoId, deployed: true, 4, Ammunition.Issue.Id);
        Assert.False(wrong.Worked);
    }

    /// <summary>THE OVERFLOW IS A THING YOU OWN. Silent when the guns took everything (the ordinary case
    /// must stay exactly as quiet as it is), and the exact remainder otherwise — the supply the put verb
    /// spends, and the reason #603's satchel rounds are reachable without editing a save.</summary>
    [Fact]
    public void WhatTheDrumsCouldNotHoldGoesInThePocketExactly()
    {
        Assert.Null(SentryHandLoad.IntoThePocket(0, Ammunition.Issue.Id));
        Assert.Null(SentryHandLoad.IntoThePocket(-3, Ammunition.Issue.Id));

        Satchel.Item loose = SentryHandLoad.IntoThePocket(9, Ammunition.LabTwoStage.Id)!.Value;
        Assert.Equal(Satchel.Kind.Rounds, loose.Kind);
        Assert.Equal(Ammunition.LabTwoStage.Id, loose.Id);
        Assert.Equal(9, loose.Count);
    }

    // ── WHICH LOCKS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE SWEEP: MORE THAN ONE SHOOTABLE, AT LEAST ONE THAT REFUSES, AND NOTHING UNRECOGNISED.
    ///
    /// <para>Anti-vacuity, and the reason it is written as a sweep over generated ground rather than over
    /// three strings typed into this file: a classifier that agrees with a hand-picked list is a classifier
    /// tested against itself. Every locked door the building actually lays is read, on five bodies and
    /// twelve floors each, and the third assertion is the one that matters — every sign is recognised
    /// POSITIVELY, so nothing reaches the default and no future door kind can be waved through by a rule
    /// written as a negation.</para>
    ///
    /// <para>RED with <c>UndergroundComplex.IsDoorSign</c> stubbed to false: zero shootable locks in the
    /// whole game, and the feature ships pointing at nothing.</para></summary>
    [Fact]
    public void TheGroundOffersSeveralShootableLocksAndSomeThatRefuse()
    {
        var shootable = new HashSet<string>(StringComparer.Ordinal);
        var refusing = new HashSet<string>(StringComparer.Ordinal);
        var unrecognised = new List<string>();

        foreach (string body in new[] { "miranda", "luna", "phobos", "titan", "triton" })
        {
            for (int level = -1; level >= -12; level--)
            {
                foreach (UndergroundComplex.LockedDoor l in
                    UndergroundComplex.Build(body, level, Field).Locked)
                {
                    bool known = UndergroundComplex.IsSealedWay(l.Sign)
                        || UndergroundComplex.IsFreightShutter(l.Sign)
                        || UndergroundComplex.IsDoorSign(l.Sign);
                    if (!known)
                    {
                        unrecognised.Add(l.Sign);
                        continue;
                    }

                    if (ShootTheLock.IsShootable(l.Sign))
                    {
                        shootable.Add(l.Sign);
                    }
                    else
                    {
                        refusing.Add(l.Sign);
                    }
                }
            }
        }

        Assert.Empty(unrecognised);
        Assert.True(shootable.Count > 1,
            $"only {shootable.Count} shootable lock(s) in the whole building — the feature points at nothing.");
        Assert.True(refusing.Count > 0,
            "nothing on this ground refuses a round, so the honest set is not a set, it is everything.");
    }

    /// <summary>THE DOORS THE STORY NEEDS SHUT STAY SHUT, AND SAY SO. #590 call 2 is not something a new
    /// control gets to renegotiate: a sealed way is a wall with a plate on it, and the refusal never hints
    /// that a key, a card, a bigger round or another angle would do it.</summary>
    [Fact]
    public void ASealedWayRefusesAndPromisesNothing()
    {
        foreach (string body in new[] { "miranda", "kaamos-hq" })
        {
            string sign = UndergroundComplex.SealedMouthSign(body, 0, 2.4);
            Assert.False(ShootTheLock.IsShootable(sign));
            Assert.Equal(ShootTheLock.Verdict.NoLockToBreak, ShootTheLock.Judge(sign));

            string refusal = ShootTheLock.RefusalLine(sign);
            Assert.False(string.IsNullOrWhiteSpace(refusal));
            foreach (string tease in new[] { "card", "key", "code", "try ", "another round", "instead" })
            {
                Assert.DoesNotContain(tease, refusal, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>…AND THE GOODS HOIST IS ONE THAT GOES. The owner named it: a roller shutter with a hasp on
    /// it is hardware, and hardware breaks.</summary>
    [Fact]
    public void TheGoodsHoistShutterIsAMechanicalLock()
    {
        Assert.True(UndergroundComplex.IsFreightShutter(UndergroundComplex.FreightPlate));
        Assert.True(ShootTheLock.IsShootable(UndergroundComplex.FreightPlate));
        Assert.Equal(ShootTheLock.Verdict.MechanicalLock,
            ShootTheLock.Judge(UndergroundComplex.FreightPlate));
    }

    /// <summary>A SIGN NOBODY WROTE IS NOT SHOOTABLE. Default deny — a lock nobody thought about can never
    /// become a target by accident, which is the difference between a rule and a hole.</summary>
    [Fact]
    public void AnUnknownSignIsNeverShootable()
    {
        foreach (string odd in new[] { "", "SOMETHING NOBODY PAINTED", "🔒", "  " })
        {
            Assert.False(ShootTheLock.IsShootable(odd));
        }
    }

    // ── WHICH GUNS THE HANDSET OFFERS ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// DESIGNATE OFFERS SET-DOWN GUNS WITH ROUNDS IN THEM — AND NOTHING ELSE, IN BOTH DIRECTIONS.
    ///
    /// <para>Swept over every combination of the three facts rather than asserted on the happy one, because
    /// the failure that matters is the offer that should not be there: a bot in the sling, a bot reading 00,
    /// or the boat's own tube gun quietly listed among the captain's property.</para>
    ///
    /// <para>RED with any of the three conditions dropped: the sweep names the combination that started
    /// being offered.</para></summary>
    [Fact]
    public void OnlyASetDownGunOfYourOwnWithRoundsInItCanBePointed()
    {
        foreach (bool deployed in new[] { true, false })
        {
            foreach (int rounds in new[] { 0, 1, 99 })
            {
                foreach (bool tubeGun in new[] { true, false })
                {
                    bool expected = deployed && rounds > 0 && !tubeGun;
                    Assert.True(
                        ShootTheLock.MayPoint(deployed, rounds, tubeGun) == expected,
                        $"deployed={deployed} rounds={rounds} tubeGun={tubeGun}");
                }
            }
        }

        // …and the tube gun is recognised by the same Core question the #797 readout asks, not by a second
        // list of names kept next to it.
        Assert.True(SurfaceArrival.IsDoorSentry(SurfaceArrival.DoorSentryUnit));
        Assert.False(ShootTheLock.MayPoint(
            deployed: true, SurfaceArrival.DoorSentryRounds,
            SurfaceArrival.IsDoorSentry(SurfaceArrival.DoorSentryUnit)));
        foreach (string mine in SentryBot.RosterUnits)
        {
            Assert.True(ShootTheLock.MayPoint(true, 12, SurfaceArrival.IsDoorSentry(mine)));
        }
    }

    // ── CAN THIS GUN SEE THAT LOCK ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE LOCK'S OWN DOOR DOES NOT BLIND THE GUN — AND ANOTHER WALL STILL DOES.
    ///
    /// <para>The first half is the trap this feature was one line away from shipping. Every door that will
    /// not open is drawn with a real wall segment lying on it, and the console the captain fires at is that
    /// segment's midpoint — but the two are computed by different paths: Core lays the wall in DOUBLES and
    /// the renderer hands out the console spot in FLOATS. One ulp of disagreement is all it takes, because
    /// the sight test is an exact segment intersection: a target rounded a hair PAST the wall is a target
    /// behind it, and a sight test aimed at the lock's centre then answers "no line" for that lock forever.
    /// Not a theory — the ulp below is the real one <c>(float)0.1</c> produces.</para>
    ///
    /// <para>So the gun is aimed at the FACE of the door, on its own side of it, which is also the honest
    /// description of what it is shooting at.</para>
    ///
    /// <para>RED with <c>ShootTheLock.StandOffDu</c> set to 0 (or <c>AimPointOn</c> returning the lock's
    /// centre): the first assertion fails and the second still passes — exactly the shape of a feature that
    /// looks tested and can never say yes.</para></summary>
    [Fact]
    public void AGunSeesTheFaceOfTheDoorButNotThroughStone()
    {
        // A door lying across x ∈ [-2, 2] at y = 0.1 in Core's doubles, with its console spot at the float
        // the renderer would publish — one ulp beyond the wall, from the gun's side of it.
        const double wallY = 0.1;
        double consoleY = (float)wallY;
        var doorItself = new List<SurfaceCollision.Segment> { new(-2, wallY, 2, wallY) };

        Assert.False(SurfaceCollision.HasLineOfSight(0, -10, 0, consoleY, doorItself),
            "the world has stopped rounding the console spot past its own wall — this guard now proves " +
            "nothing, and the stand-off it exists for can be deleted on purpose rather than by accident.");

        Assert.True(ShootTheLock.CanBreak(0, -10, 0, consoleY, doorItself),
            "the gun cannot see the face of the very door it is standing in front of.");

        // …and a second slab across the corridor between them stops it, exactly the way it stops a sentry.
        var andASlab = new List<SurfaceCollision.Segment>
        {
            new(-2, wallY, 2, wallY), new(-6, -5, 6, -5),
        };
        Assert.False(ShootTheLock.CanBreak(0, -10, 0, consoleY, andASlab));
    }

    /// <summary>ONE RANGE LAW, NOT TWO. The arc a captain may point a gun into is the arc that gun already
    /// holds on its own — a second number would be a second thing to learn and a second thing to drift.</summary>
    [Fact]
    public void TheArcIsTheSentrysOwnArc()
    {
        double justInside = SentryBot.RangeDeckUnits - 0.5;
        double justOutside = SentryBot.RangeDeckUnits + 0.5;
        Assert.True(ShootTheLock.CanBreak(0, 0, 0, justInside, null));
        Assert.False(ShootTheLock.CanBreak(0, 0, 0, justOutside, null));
    }

    // ── THE SHOT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>THE SHOT SPENDS ROUNDS AND BREAKS THE LOCK. One Core answer carries all of it, so the drum,
    /// the door and the record cannot be moved by three conditions that happen to agree today.</summary>
    [Fact]
    public void FiringOnAHaspCostsTheDrumAndOpensTheDoor()
    {
        ShootTheLock.Order order = ShootTheLock.Fire(
            "K-77", UndergroundComplex.FreightPlate, 17, Ammunition.Issue, rangeDu: 9);

        Assert.True(order.Fired);
        Assert.True(order.Broken);
        Assert.Equal(ShootTheLock.RoundsPerHasp, order.RoundsSpent);
        Assert.Equal(17 - ShootTheLock.RoundsPerHasp, order.Magazine);
        Assert.Contains("K-77", order.Line);
    }

    /// <summary>A GUN THAT WILL NOT FIRE DOES NOT PAY FOR THE TRIGGER PULL. Three refusals, none of them
    /// silent and none of them costing a round: the wrong kind of thing, a round that will not arm at that
    /// range, and a drum that is short.</summary>
    [Fact]
    public void EveryRefusalCostsNothingAndSaysWhy()
    {
        ShootTheLock.Order atAWall = ShootTheLock.Fire(
            "K-77", UndergroundComplex.SealedMouthSign("miranda", 1, 2.0), 99, Ammunition.Issue, 9);
        ShootTheLock.Order tooClose = ShootTheLock.Fire(
            "K-77", UndergroundComplex.FreightPlate, 99, Ammunition.LabTwoStage,
            Ammunition.LabTwoStage.MinimumRangeDu - 1);
        ShootTheLock.Order short0 = ShootTheLock.Fire(
            "K-77", UndergroundComplex.FreightPlate, ShootTheLock.RoundsPerHasp - 1, Ammunition.Issue, 9);

        foreach (ShootTheLock.Order refused in new[] { atAWall, tooClose, short0 })
        {
            Assert.False(refused.Fired);
            Assert.False(refused.Broken);
            Assert.Equal(0, refused.RoundsSpent);
            Assert.False(string.IsNullOrWhiteSpace(refused.Line));
        }

        Assert.Equal(99, atAWall.Magazine);
        Assert.Equal(ShootTheLock.RoundsPerHasp - 1, short0.Magazine);
    }

    /// <summary>THE ROUND THAT IS GOOD AT THIS COSTS ONE. Read off the round's OWN property — a two-stage
    /// penetrator does its work on the far side of the first thing it meets, and a hasp is precisely that —
    /// rather than off its id, so a future round with the same character gets the same answer for free.</summary>
    [Fact]
    public void APenetratorTakesAHaspOffWithOneRound()
    {
        Assert.Equal(1, ShootTheLock.RoundsFor(Ammunition.LabTwoStage));
        Assert.Equal(ShootTheLock.RoundsPerHasp, ShootTheLock.RoundsFor(Ammunition.Issue));

        ShootTheLock.Order order = ShootTheLock.Fire(
            "R-3B", UndergroundComplex.FreightPlate, 4, Ammunition.LabTwoStage,
            Ammunition.LabTwoStage.MinimumRangeDu + 2);
        Assert.True(order.Fired);
        Assert.Equal(3, order.Magazine);
    }

    // ── THE NOISE, AS A FACT ──────────────────────────────────────────────────────────────────────────

    /// <summary>THE SHOT IS FILED, IN ORDER, WITH EVERYTHING #804 WILL WANT. A list built by appending is a
    /// list in order (the fourth named bug class on this repo is a source consumed in the wrong one), and
    /// the earshot is the ground's OWN ear rather than a second number written next to it.</summary>
    [Fact]
    public void EveryShotIsFiledAndCarriesItsOwnEarshot()
    {
        IReadOnlyList<GunfireHeard.Shot> log = [];
        log = GunfireHeard.File(log, new GunfireHeard.Shot("K-77", "INTAKE", 10, 10, 100, 6));
        log = GunfireHeard.File(log, new GunfireHeard.Shot("R-3B", "GRADING", 40, 10, 220, 1));

        Assert.Equal(2, GunfireHeard.Count(log));
        Assert.Equal("K-77", log[0].Unit);
        Assert.Equal("R-3B", log[1].Unit);
        Assert.Equal(7, GunfireHeard.RoundsFired(log));

        Assert.Equal(ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire), GunfireHeard.EarshotDu);
        Assert.True(GunfireHeard.WithinEarshot(log[0], 10, 10 + GunfireHeard.EarshotDu - 1));
        Assert.False(GunfireHeard.WithinEarshot(log[0], 10, 10 + GunfireHeard.EarshotDu + 1));

        Assert.Contains("K-77", GunfireHeard.BookLine(log[0]));
        Assert.Contains("INTAKE", GunfireHeard.BookLine(log[0]));
    }

    // ── CANON ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>§13.8 · NOTHING THIS FEATURE SAYS EXPLAINS ANYTHING. A room opened by force is the most
    /// tempting place in the game to finally account for the building — so every sentence this lane adds is
    /// grepped for the words that would turn a description into an account.</summary>
    [Fact]
    public void NoNewLineExplainsWhatThisPlaceWasFor()
    {
        string[] lines =
        [
            ShootTheLock.PanelBlurb,
            ShootTheLock.NothingSetDownLine,
            ShootTheLock.EverythingDryLine,
            ShootTheLock.NothingInViewLine("K-77"),
            ShootTheLock.RefusalLine(UndergroundComplex.FreightPlate),
            ShootTheLock.RefusalLine(UndergroundComplex.SealedMouthSign("miranda", 0, 1.2)),
            ShootTheLock.BehindItLine(UndergroundComplex.FreightPlate),
            ShootTheLock.BehindItLine("LONG STORAGE"),
            ShootTheLock.BrokenLine("K-77", "LONG STORAGE", 6, Ammunition.Issue),
            GunfireHeard.WhatItCostLine,
            GunfireHeard.BookLine(new GunfireHeard.Shot("K-77", "LONG STORAGE", 0, 0, 0, 6)),
            SentryHandLoad.PocketedLine(9),
            SentryHandLoad.LoadedLine("K-77", 6, 11, 17, Ammunition.Issue, 0),
        ];

        string[] forbidden =
        [
            "the Old Ones", "reever", "monolith", "experiment", "prisoner", "test subject",
            "because", "in order to", "which means", "the reason", "so that", "explains", "it was for",
            "turns out", "proves", "confirmed",
        ];

        foreach (string line in lines)
        {
            Assert.False(string.IsNullOrWhiteSpace(line));
            foreach (string bad in forbidden)
            {
                Assert.DoesNotContain(bad, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>THE HANDSET'S OWN WORDS ARE READABLE AT PHONE SIZE (#782): every row says what it is, what
    /// it holds or costs, and nothing on this panel is a bare glyph a captain has to decode before spending
    /// six rounds.</summary>
    [Fact]
    public void EveryRowOnTheHandsetSaysWhatItIs()
    {
        string gun = ShootTheLock.GunRow("K-77", 17, Ammunition.Issue);
        Assert.Contains("K-77", gun);
        Assert.Contains("17/99", gun);
        Assert.Contains(Ammunition.Issue.Name, gun);

        string target = ShootTheLock.TargetRow(UndergroundComplex.FreightPlate, 9.4, 6);
        Assert.Contains(UndergroundComplex.FreightPlate, target);
        Assert.Contains("9 du", target);
        Assert.Contains("6 rounds", target);

        // …and a row for something that will refuse quotes no price, because a tariff on a trade the world
        // is going to turn down is the control misleading the captain rather than teaching them.
        string wall = ShootTheLock.TargetRow(
            UndergroundComplex.SealedMouthSign("miranda", 0, 2.4), 12.0, 6);
        Assert.DoesNotContain("6 rounds", wall);
        Assert.Contains("nothing on it to break", wall);

        Assert.Contains("6 rounds", ShootTheLock.FireLabel(6));
        Assert.Contains("1 round", ShootTheLock.FireLabel(1));
    }
}

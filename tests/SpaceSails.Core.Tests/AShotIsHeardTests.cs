using System;
using System.Collections.Generic;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #618 · <b>A SHOT IS HEARD — the rules half.</b>
///
/// <para>Owner's ruling, 2026-08-05, and the last thread of #618 left hanging when the rest of it landed as
/// #804/#833/#835/#836/#715: <i>"they come if we make a big noise like start to use the special ammo to open
/// a locked door."</i> <c>GunfireHeard</c> has filed every shot since #803 and said in its own docblock that
/// nothing reacted to it; this is the arithmetic that does.</para>
///
/// <para>The rules live here; the man walking, the A*, the heat going into somebody's book and the page's own
/// wiring are <c>AShotOnAPatrolledFloorTests</c>'s, one project along, for the split
/// <c>AGuardOnlyRunsWhenYouGiveHimAReasonTests</c> already keeps.</para>
///
/// <h3>The one thing this file is most careful about</h3>
///
/// <para><b>There is no second acoustics.</b> The whole range law is one number that was already in the tree
/// and had never been asked a question: <c>GunfireHeard.EarshotDu</c>, which IS
/// <c>ReeverHearing.RangeOf(Noise.Gunfire)</c>. <see cref="TheEarIsTheOneTheGroundAlreadyHad"/> asserts the
/// identity rather than the value, so a lane that retunes what a gun sounds like retunes the guards with it
/// and cannot leave the pack and the payroll hearing two different bangs.</para>
/// </summary>
public sealed class AShotIsHeardTests
{
    private static GunfireHeard.Shot At(double x, double y) =>
        new("K-77", "LONG STORAGE", x, y, 100, 6);

    // ── THE RANGE LAW ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE EAR IS THE ONE THE GROUND ALREADY HAD, and it is asserted as an IDENTITY and not as a number.
    ///
    /// <para>A second constant here — <c>GuardHearingDu = 34</c>, say — would read identically today and be
    /// the first named bug class the morning somebody tuned one of them. The Old Ones and the payroll hear
    /// the same gun.</para>
    /// </summary>
    [Fact]
    public void TheEarIsTheOneTheGroundAlreadyHad()
    {
        Assert.Equal(ReeverHearing.RangeOf(ReeverHearing.Noise.Gunfire), GunfireHeard.EarshotDu);

        // …and it carries further than anything a guard can SEE, which is the whole reason this rung exists:
        // a man hears a shot from places he could never have watched you fire it from.
        Assert.True(GunfireHeard.EarshotDu > PatrolBeat.MarkerSightDu);
        Assert.True(GunfireHeard.EarshotDu > PatrolBeat.NoticeDu);
    }

    /// <summary>
    /// WHO HEARS IT: the nearest man inside earshot, and nobody at all outside it.
    ///
    /// <para>The out-of-range half is the one that matters — it is guard (b)'s whole content, and it is what
    /// a floor-wide response would silently take away. RED by making <c>NearestEar</c> ignore
    /// <c>WithinEarshot</c>: the third case below then hands back a man standing a hundred du off.</para>
    /// </summary>
    [Fact]
    public void TheNearestEarInsideEarshotHearsIt_AndNobodyOutsideItDoes()
    {
        GunfireHeard.Shot shot = At(0, 0);
        double reach = GunfireHeard.EarshotDu;

        // Three men. The middle one is nearest, and the answer is his index rather than his distance.
        Assert.Equal(1, GunfireHeard.NearestEar(shot, [(reach - 1, 0), (3, 0), (10, 0)]));

        // …the far one is inside and the near one is outside, so "nearest" is asked of the ones who can hear.
        Assert.Equal(1, GunfireHeard.NearestEar(shot, [(reach + 40, 0), (reach - 0.5, 0)]));

        // …and a floor where nobody is close enough hands back nothing. No walk, and (in the client) no heat.
        Assert.Equal(-1, GunfireHeard.NearestEar(shot, [(reach + 0.5, 0), (0, reach + 0.5), (200, 200)]));

        // An empty floor and a null list are the same answer, said the same way: an unpatrolled level and the
        // FOUND band reach this method with nobody in their hands.
        Assert.Equal(-1, GunfireHeard.NearestEar(shot, []));
        Assert.Equal(-1, GunfireHeard.NearestEar(shot, null));
    }

    /// <summary>THE EDGE IS THE EDGE, on both sides of it, and it is the SAME edge
    /// <see cref="GunfireHeard.WithinEarshot"/> keeps — one predicate, asked of one man or of a list.</summary>
    [Fact]
    public void TheEdgeOfEarshotIsOneEdge()
    {
        GunfireHeard.Shot shot = At(12, -7);

        for (double at = GunfireHeard.EarshotDu - 2; at <= GunfireHeard.EarshotDu + 2; at += 0.25)
        {
            double x = shot.X + at, y = shot.Y;
            bool inside = GunfireHeard.WithinEarshot(shot, x, y);

            Assert.Equal(at <= GunfireHeard.EarshotDu, inside);
            Assert.Equal(inside ? 0 : -1, GunfireHeard.NearestEar(shot, [(x, y)]));
        }
    }

    /// <summary>…AND IT IS A CIRCLE, not a corridor. Asked around the compass, because a range test that only
    /// ever walks one axis passes on a rule that measured <c>|dx|</c>.</summary>
    [Fact]
    public void TheEarIsACircleRoundThePlaceItCameFrom()
    {
        GunfireHeard.Shot shot = At(-30, 18);

        for (int deg = 0; deg < 360; deg += 15)
        {
            double a = deg * Math.PI / 180.0;
            double inX = shot.X + (Math.Cos(a) * (GunfireHeard.EarshotDu - 1));
            double inY = shot.Y + (Math.Sin(a) * (GunfireHeard.EarshotDu - 1));
            double outX = shot.X + (Math.Cos(a) * (GunfireHeard.EarshotDu + 1));
            double outY = shot.Y + (Math.Sin(a) * (GunfireHeard.EarshotDu + 1));

            Assert.Equal(0, GunfireHeard.NearestEar(shot, [(inX, inY)]));
            Assert.Equal(-1, GunfireHeard.NearestEar(shot, [(outX, outY)]));
        }
    }

    // ── ONE BANG IS ANSWERED ONCE ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE CURSOR: a round answers the shot it has not answered, and it answers the NEWEST one.
    ///
    /// <para>Three rounds into one hasp inside a second are one noise from where a man is standing. A reader
    /// that walked the backlog would send him to the first door, then the second, then the third — a queue of
    /// errands nobody fired.</para>
    /// </summary>
    [Fact]
    public void OneBangIsAnsweredOnceAndItIsTheNewestOne()
    {
        IReadOnlyList<GunfireHeard.Shot> log = [];
        Assert.Null(GunfireHeard.SinceLastHeard(log, 0));
        Assert.Null(GunfireHeard.SinceLastHeard(null, 0));

        log = GunfireHeard.File(log, At(1, 1));
        Assert.Equal(At(1, 1), GunfireHeard.SinceLastHeard(log, 0));

        // …and the moment the cursor is brought up to the ledger, the same log says nothing at all.
        Assert.Null(GunfireHeard.SinceLastHeard(log, GunfireHeard.Count(log)));

        // Two more fired while the cursor stood still: he goes to the LAST place it came from, which is where
        // it is still coming from — not to the backlog.
        log = GunfireHeard.File(log, At(2, 2));
        log = GunfireHeard.File(log, At(3, 3));
        Assert.Equal(At(3, 3), GunfireHeard.SinceLastHeard(log, 1));

        // A cursor ahead of the ledger is what a captain who rode a lift looks like — the round brings it up
        // to the length of a log that belongs to the whole excursion. It must be silent, not negative.
        Assert.Null(GunfireHeard.SinceLastHeard(log, GunfireHeard.Count(log)));
        Assert.Null(GunfireHeard.SinceLastHeard(log, 99));
    }

    /// <summary>THE SHOT CARRIES THE PLACE, so the man is walked to somewhere the world already wrote down
    /// rather than to a coordinate anybody re-derived. #803 filed it; this is the read.</summary>
    [Fact]
    public void TheWalkGoesToThePlaceTheRecordNames()
    {
        IReadOnlyList<GunfireHeard.Shot> log = GunfireHeard.File([], At(-11.5, 42.25));
        GunfireHeard.Shot? heard = GunfireHeard.SinceLastHeard(log, 0);

        Assert.NotNull(heard);
        Assert.Equal(-11.5, heard!.Value.X);
        Assert.Equal(42.25, heard.Value.Y);
    }

    // ── HOW LONG HE SPENDS ON IT ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE WALK TO A BANG IS BOUNDED BY A CLOCK AND BY NOTHING ELSE, and the clock is the walk-up's own.
    ///
    /// <para>A place does not walk away from you, which is why <see cref="PatrolBeat.StillComing"/> — which
    /// also gives up past <see cref="PatrolBeat.GivesUpBeyondDu"/> — is the wrong question for this walk and
    /// would have ended every one of them on frame one: a shot carries thirty-four deck units and a hail is
    /// abandoned past thirteen. The bound below is a BACKSTOP: at a guard's own pace it covers further than a
    /// shot can be heard from, which this fact asserts rather than trusts.</para>
    /// </summary>
    [Fact]
    public void HeWalksToItOnAClockAndTheClockIsAlwaysLongEnough()
    {
        Assert.True(PatrolBeat.StillLookingIntoIt(0));
        Assert.True(PatrolBeat.StillLookingIntoIt(PatrolBeat.WalkUpSeconds));
        Assert.False(PatrolBeat.StillLookingIntoIt(PatrolBeat.WalkUpSeconds + 0.001));
        Assert.False(PatrolBeat.StillLookingIntoIt(double.NaN));

        // The backstop never decides: the furthest a shot can be heard from, walked at his own pace, fits
        // inside the clock with room to spare. If a tuning ever breaks this, the guard says which number.
        double reachableDu = PatrolBeat.WalkSpeed * PatrolBeat.WalkUpSeconds;
        Assert.True(
            reachableDu > GunfireHeard.EarshotDu * 1.5,
            $"a man walking at {PatrolBeat.WalkSpeed} du/s covers {reachableDu:0.#} du in " +
            $"{PatrolBeat.WalkUpSeconds}s, and a shot carries {GunfireHeard.EarshotDu:0.#} du. The clock has " +
            "stopped being a backstop and started being the thing that decides.");

        // …and it is deliberately the SAME clock the approach runs on. A walk to a bang and a walk to a
        // person are one man doing one walk; a second bound would be a second set of edge cases.
        Assert.False(PatrolBeat.StillComing(
            PatrolBeat.WalkUpSeconds + 0.001, 0, 0, 0, 0));
    }

    // ── WHAT IT COSTS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WEIGHT 3, AND IT SITS WHERE IT SITS FOR A REASON. Between the escort (a man writing your face down)
    /// and the ejection (a man deciding you are a problem) — asserted as an ORDER over the other rows rather
    /// than as a bare literal, so the table stays a ladder when the owner tunes it.
    ///
    /// <para>FLAGGED for tuning, like every number in that table; what is not for tuning is the order.</para>
    /// </summary>
    [Fact]
    public void AShotCostsMoreThanAnEscortAndLessThanAnEjection()
    {
        Assert.Equal(3, IllegalHeat.WeightOf(IllegalHeat.Crossing.ShotOnTheirFloor));

        Assert.True(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.ShotOnTheirFloor)
                > IllegalHeat.WeightOf(IllegalHeat.Crossing.TheEscort));
        Assert.True(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.ShotOnTheirFloor)
                < IllegalHeat.WeightOf(IllegalHeat.Crossing.TheKickOut));
        Assert.True(
            IllegalHeat.WeightOf(IllegalHeat.Crossing.ShotOnTheirFloor)
                > UndergroundComplex.RefusedCardHeat);
    }

    /// <summary>
    /// IT IS OWED TO THE OUTFIT AND NEVER TO THE ROCK — the same law every other crossing obeys, asked of
    /// this one so it cannot be the row that quietly got its own key.
    ///
    /// <para>Two bodies one company runs answer for each other; a third company's site has heard nothing,
    /// because from where they sit nothing happened.</para>
    /// </summary>
    [Fact]
    public void TheShotIsOwedToWhoeverRunsTheSite()
    {
        string body = "luna";
        UndergroundComplex.HeatCharge charge =
            IllegalHeat.Charge(body, IllegalHeat.Crossing.ShotOnTheirFloor);

        Assert.Equal(SiteOperator.Of(body).Id, charge.OperatorId);
        Assert.NotEqual(body, charge.OperatorId);
        Assert.Equal(3, charge.Points);

        var book = new ContactLedger();
        Assert.Equal(3, IllegalHeat.Bank(book, charge, 0));
        Assert.Equal(3, IllegalHeat.HeatAtSite(book, body));

        // …and it is one line in one book, filed under the outfit's own heading and not under the rock's.
        Assert.Equal(0, book.For(body).HeatOwed);
        Assert.True(IllegalHeat.IsAnOutfitsBook(IllegalHeat.LedgerId(charge.OperatorId)));
    }

    // ── AND NOTHING IS SAID ABOUT ANY OF IT ───────────────────────────────────────────────────────────

    /// <summary>
    /// #603 · NOTHING EXPLAINS IT. The canon differential, asked of Core: this rung authored no sentence, so
    /// the round's whole catalogue (<see cref="PatrolBeat.AllProse"/>) is exactly what it was, and neither
    /// the heat meter's three lines nor the shot's own two grew a fourth.
    ///
    /// <para>The one sentence a captain ever gets about the noise is the one they already had, from their own
    /// gun, the first time they fired it indoors — and this lane is what finally makes it true. It is
    /// asserted here verbatim, because a line whose promise has just been kept is a line no tidy-up may
    /// quietly retire.</para>
    /// </summary>
    [Fact]
    public void NothingNewIsEverSaidAboutIt()
    {
        var prose = new List<string>(PatrolBeat.AllProse());

        foreach (string line in prose)
        {
            Assert.DoesNotContain("gunfire", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gunshot", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("alerted", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("investigat", line, StringComparison.OrdinalIgnoreCase);

            // …and nobody on the payroll ever mentions hearing anything. ("SECURITY ROTA" is a man's own
            // plate and has been since #804, which is why the needle is the VERB and not the word.)
            Assert.DoesNotContain("heard a", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("the noise", line, StringComparison.OrdinalIgnoreCase);
        }

        foreach (string line in IllegalHeat.EveryLine())
        {
            Assert.DoesNotContain("shot", line, StringComparison.OrdinalIgnoreCase);
        }

        // The promise this lane keeps, said by the captain's own gun and by nothing on the payroll.
        Assert.Contains(
            "That is not the same as nothing having heard it.",
            GunfireHeard.WhatItCostLine,
            StringComparison.Ordinal);
    }
}

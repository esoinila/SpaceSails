namespace SpaceSails.Core.Tests;

/// <summary>
/// #520 · The crew's report on the captain. Owner: <i>"the crew satisfaction reports could guide the player
/// into seeking the right balance for his crew's greed/honesty ratio, that keeps him in the captain's
/// seat."</i> These pin the tensions that make it a balance rather than a score to maximise.
/// </summary>
public class CrewTempTests
{
    [Fact]
    public void AnHonestCaptainRunsASourShip()
    {
        // THE WHOLE POINT, made arithmetic. Owner: "too honest a captain takes their winnings." Every honest
        // filing is money the crew did not get, and they can count.
        var honest = new CrewTemp.Voyage(HonestFilings: 6, SharePaid: 0);
        var crooked = new CrewTemp.Voyage(ProfitableLies: 6, SharePaid: 9000);

        CrewTemp.Reading honestPay = Find(honest, CrewTemp.Dimension.Pay);
        CrewTemp.Reading crookedPay = Find(crooked, CrewTemp.Dimension.Pay);

        Assert.True(honestPay.Score < crookedPay.Score,
                    "an honest captain must cost the crew money, or there is no dilemma");
        Assert.True(CrewTemp.BandOf(honestPay.Score) <= CrewTemp.Band.Sour);
    }

    [Fact]
    public void ButTheCrookedShipIsTheDangerousOne()
    {
        // And the other side of the balance: the heat that buys the good share is carried by the crew too.
        // If lying were free the dial would have one correct setting.
        var crooked = new CrewTemp.Voyage(ProfitableLies: 6, SharePaid: 9000, Heat: 8, CrewLost: 1);

        Assert.True(Find(crooked, CrewTemp.Dimension.Safety).Score
                    < Find(crooked, CrewTemp.Dimension.Pay).Score,
                    "the paying ship must be the frightening one");
    }

    [Fact]
    public void NoVoyageScoresWellOnEverything()
    {
        // The load-bearing property: there is no setting of the dial that satisfies every line. Swept over a
        // wide range of captains, nobody comes out devoted across the board.
        for (int lies = 0; lies <= 8; lies++)
        {
            for (int honest = 0; honest <= 8; honest++)
            {
                var v = new CrewTemp.Voyage(
                    HonestFilings: honest, ProfitableLies: lies,
                    SharePaid: lies * 1500, Heat: lies, TotsPoured: 6, DaysSinceShoreLeave: 20);

                bool allDevoted = true;
                foreach (CrewTemp.Reading r in CrewTemp.Readings(v))
                {
                    if (CrewTemp.BandOf(r.Score) != CrewTemp.Band.Devoted)
                    {
                        allDevoted = false;
                        break;
                    }
                }

                Assert.False(allDevoted, $"lies={lies}, honest={honest} satisfies everyone — the balance is fake");
            }
        }
    }

    [Fact]
    public void HonestyTowardTheLawIsNotHonestyTowardTheCrew()
    {
        // Two different virtues, and only one of them is THE WORD. A captain can lie to every depot in the
        // belt and never once lie to the people who sail with him — that is a coherent captain and the
        // report must be able to say so.
        var liarWhoKeepsHisWord = new CrewTemp.Voyage(ProfitableLies: 8, PromisesKept: 5, SharePaid: 12000);

        Assert.Equal(CrewTemp.Band.Devoted,
                     CrewTemp.BandOf(Find(liarWhoKeepsHisWord, CrewTemp.Dimension.TheWord).Score));
    }

    [Fact]
    public void BreakingYourWordToTheCrewIsWhatEndsCaptaincies()
    {
        // Weighted three times a kept promise, because that is how trust actually behaves.
        var kept = new CrewTemp.Voyage(PromisesKept: 3);
        var broken = new CrewTemp.Voyage(PromisesBroken: 3);

        int keptScore = Find(kept, CrewTemp.Dimension.TheWord).Score;
        int brokenScore = Find(broken, CrewTemp.Dimension.TheWord).Score;

        Assert.True(keptScore - 60 < 60 - brokenScore,
                    "a broken promise must cost more than a kept one earns");
    }

    [Fact]
    public void VentingYourOwnCompartmentIsItsOwnFact()
    {
        // Not a shade of unsafe — its own line, with its own comment, and the crew's answer to it is the
        // suit they now keep closer.
        var quiet = new CrewTemp.Voyage();
        var vented = new CrewTemp.Voyage(VentedOwnCompartment: true);

        CrewTemp.Reading before = Find(quiet, CrewTemp.Dimension.Safety);
        CrewTemp.Reading after = Find(vented, CrewTemp.Dimension.Safety);

        // Its own comment, in the crew's voice, about the thing they did in response — and worth more than
        // losing somebody, because losing somebody is the job and this was a choice.
        Assert.Contains("suit", after.Comment, System.StringComparison.OrdinalIgnoreCase);
        Assert.True(after.Score <= before.Score - 25,
                    $"safety went {before.Score} → {after.Score}; opening a berth with a man in it must cost more");
    }

    [Fact]
    public void CohesionBuysToleranceAndNotHappiness()
    {
        // Owner, on soldiers: "they only care about the other people fighting beside them." Shared hardship
        // survived does not raise a single dimension — it makes the crew slower to ACT on being unhappy.
        var bare = new CrewTemp.Voyage(HonestFilings: 8, PromisesBroken: 2, DaysSinceShoreLeave: 90);
        var through = bare with { NearMisses = 5 };

        Assert.Equal(CrewTemp.Overall(bare), CrewTemp.Overall(through));   // no happier
        Assert.True(CrewTemp.Cohesion(through) > CrewTemp.Cohesion(bare)); // slower to move
        Assert.True(CrewTemp.StandingOf(through) < CrewTemp.StandingOf(bare),
                    "a crew that has been through it with him must be further from the island");
    }

    [Fact]
    public void LosingPeopleCutsCohesionRatherThanBuildingIt()
    {
        var survived = new CrewTemp.Voyage(NearMisses: 4);
        var buried = new CrewTemp.Voyage(NearMisses: 4, CrewLost: 3);

        Assert.True(CrewTemp.Cohesion(buried) < CrewTemp.Cohesion(survived));
    }

    [Fact]
    public void TheMarooningIsAlwaysPrecededByWarnings()
    {
        // A mutiny that arrives without warning is a dice roll wearing a story. Every stage below Solid has
        // a line on the sheet, so a surprised captain was not reading it.
        foreach (CrewTemp.Standing s in System.Enum.GetValues<CrewTemp.Standing>())
        {
            Assert.False(string.IsNullOrWhiteSpace(CrewTemp.StandingLine(s)));
        }

        // And the escalation is monotone in satisfaction: you cannot skip from content to the island.
        var awful = new CrewTemp.Voyage(HonestFilings: 8, PromisesBroken: 4, CrewLost: 3,
                                        DaysSinceShoreLeave: 200, Heat: 9, VentedOwnCompartment: true);
        Assert.Equal(CrewTemp.Standing.Marooning, CrewTemp.StandingOf(awful));

        var decent = new CrewTemp.Voyage(ProfitableLies: 4, SharePaid: 8000, PromisesKept: 4,
                                         TotsPoured: 8, NearMisses: 2);
        Assert.Equal(CrewTemp.Standing.Solid, CrewTemp.StandingOf(decent));
    }

    [Fact]
    public void TheIslandComesWithTheRumAndTheRumIsNotAKindness()
    {
        string line = CrewTemp.StandingLine(CrewTemp.Standing.Marooning);

        Assert.Contains("one shot", line, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rum", line, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheSheetPointsAtOneThingToFix()
    {
        // How a temperature survey is actually used: not as a grade, as a pointer. The worst line is the
        // one the captain can act on this week.
        var v = new CrewTemp.Voyage(ProfitableLies: 6, SharePaid: 9000, DaysSinceShoreLeave: 200);

        Assert.Equal(CrewTemp.Dimension.Rest, CrewTemp.WorstOf(v).Dimension);
    }

    [Fact]
    public void EveryReadingSaysSomethingInTheCrewsOwnVoice()
    {
        // The comments are where this lives. A dimension with a number and no sentence is a spreadsheet.
        for (int lies = 0; lies <= 8; lies += 2)
        {
            var v = new CrewTemp.Voyage(ProfitableLies: lies, HonestFilings: 8 - lies,
                                        SharePaid: lies * 1200, TotsPoured: lies, DaysSinceShoreLeave: lies * 12);
            foreach (CrewTemp.Reading r in CrewTemp.Readings(v))
            {
                Assert.False(string.IsNullOrWhiteSpace(r.Comment));
                Assert.False(string.IsNullOrWhiteSpace(CrewTemp.Heading(r.Dimension)));
            }
        }
    }

    [Fact]
    public void TheReportIsPureSoAReloadCannotReRollTheCrewsMood()
    {
        var v = new CrewTemp.Voyage(ProfitableLies: 3, HonestFilings: 2, SharePaid: 4200,
                                    PromisesKept: 1, TotsPoured: 4, DaysSinceShoreLeave: 33, Heat: 2);

        Assert.Equal(CrewTemp.Overall(v), CrewTemp.Overall(v));
        Assert.Equal(CrewTemp.Readings(v)[0].Comment, CrewTemp.Readings(v)[0].Comment);
        Assert.Equal(CrewTemp.StandingOf(v), CrewTemp.StandingOf(v));
    }

    /// <summary>
    /// #663 · WHY <c>CrewDeputation</c> AND <c>CrewMeeting</c> ARE STILL ORPHANS, stated as a computation
    /// instead of as an opinion.
    ///
    /// <para>The issue's plan for those two beats was to raise them on the <see cref="CrewTemp.Standing"/>
    /// edges — a deputation on Petition, a meeting on Ultimatum — and the edges are the right ones. What
    /// nobody had checked is whether the shipped game can CROSS them. It cannot, and a beat wired to an edge
    /// the world cannot reach is a caller that satisfies a scanner and never fires, which is this house's
    /// fifth bug class wearing the fix's clothes.</para>
    ///
    /// <para>The reason is in <c>Map.CrewTemp.CrewVoyage()</c>, where three of the inputs that could push a
    /// crew down are honest dormant constants — <c>PromisesBroken</c>, <c>CrewLost</c> and
    /// <c>DaysSinceShoreLeave</c> — because nothing in the game yet makes a promise TO THE CREW, no crewman
    /// can die, and nothing tracks shore leave. Of the inputs that ARE live, every one either helps the crew
    /// or is capped: heat stops at <see cref="EncounterRule.MaxHeatLevel"/>, credits, tots and near-misses
    /// only ever add, and honest filings pull PAY down while pushing PROSPECTS up by almost as much.</para>
    ///
    /// <para>So this sweeps the whole live space and pins the floor. It is written to go RED the day any one
    /// of those three hooks becomes real — which is the day the two beats become wireable, and the day
    /// somebody should come back to #663 and wire them.</para>
    /// </summary>
    [Fact]
    public void NothingTheShipActuallyTracksCanPushTheCrewPastGrumbling()
    {
        CrewTemp.Standing worst = CrewTemp.Standing.Solid;
        CrewTemp.Voyage worstVoyage = default;

        // Every live input, swept past the point where it stops mattering. The dormant three are held at the
        // constants Map.CrewTemp actually passes, because that is the world this claim is about.
        foreach (int filings in new[] { 0, 1, 3, 6, 7, 10, 20, 40 })
        {
            foreach (int lies in new[] { 0, 1, 3, 10, 40 })
            {
                foreach (long share in new long[] { 0, 400, 5_000, 50_000 })
                {
                    foreach (int tots in new[] { 0, 10 })
                    {
                        foreach (int nearMisses in new[] { 0, 5 })
                        {
                            for (int heat = 0; heat <= EncounterRule.MaxHeatLevel; heat++)
                            {
                                foreach (bool vented in new[] { false, true })
                                {
                                    var v = new CrewTemp.Voyage(
                                        HonestFilings: filings,
                                        ProfitableLies: lies,
                                        SharePaid: share,
                                        PromisesKept: 0,        // dormant in Map.CrewTemp
                                        PromisesBroken: 0,      // dormant in Map.CrewTemp
                                        CrewLost: 0,            // dormant in Map.CrewTemp
                                        NearMisses: nearMisses,
                                        TotsPoured: tots,
                                        DaysSinceShoreLeave: 0, // dormant in Map.CrewTemp
                                        Heat: heat,
                                        VentedOwnCompartment: vented);

                                    CrewTemp.Standing s = CrewTemp.StandingOf(v);
                                    if (s > worst)
                                    {
                                        worst = s;
                                        worstVoyage = v;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.True(worst == CrewTemp.Standing.Grumbling,
            $"the live inputs now reach {worst} ({worstVoyage}) — the crew CAN be pushed past Grumbling, so " +
            "StoryBeats.Beat.CrewDeputation / CrewMeeting are wireable at last. Wire them on the " +
            "CrewTemp.Standing edges (#663) and take them off EveryStoryBeatHasACallerTests.KnownOrphans.");

        // And the other half of the claim, so this cannot pass on a sweep that reached nothing: the crew DO
        // move within the range they have, or the sweep above would be a loop over one answer.
        Assert.Equal(CrewTemp.Standing.Solid, CrewTemp.StandingOf(new CrewTemp.Voyage(SharePaid: 50_000, TotsPoured: 10)));
        Assert.Equal(CrewTemp.Standing.Grumbling, CrewTemp.StandingOf(worstVoyage));

        // …and the edges themselves are real, on a Voyage that the RULES admit even though the client cannot
        // build one yet. This is what says the beats' target is correct and only their world is missing.
        Assert.Equal(CrewTemp.Standing.Petition,
            CrewTemp.StandingOf(new CrewTemp.Voyage(HonestFilings: 6, PromisesBroken: 1, DaysSinceShoreLeave: 90, Heat: 3)));
        Assert.Equal(CrewTemp.Standing.Ultimatum,
            CrewTemp.StandingOf(new CrewTemp.Voyage(HonestFilings: 6, PromisesBroken: 1, CrewLost: 2,
                                                    DaysSinceShoreLeave: 200, Heat: 3)));
    }

    private static CrewTemp.Reading Find(in CrewTemp.Voyage v, CrewTemp.Dimension d)
    {
        foreach (CrewTemp.Reading r in CrewTemp.Readings(v))
        {
            if (r.Dimension == d)
            {
                return r;
            }
        }
        throw new System.InvalidOperationException($"no reading for {d}");
    }
}

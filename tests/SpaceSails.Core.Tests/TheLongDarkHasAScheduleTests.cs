using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #638 · <b>THE VOID GETS ITS LANE.</b> <see cref="DeathCause.Void"/> shipped a painting, three lines of
/// prose, a headline and (from #636) a place law, and for two years nothing in the client could set it. The
/// ruling (Fable, 2026-09-01) picked option 1: reaction mass at zero with no way home, twenty consecutive
/// sim-days, through the death machinery that already exists.
///
/// <para><b>What this file is careful about.</b> This house has a named bug class — <i>a green test that
/// asserts nothing</i> — where a guard is handed a world that cannot tell pass from fail. The adrift
/// predicate is exactly the shape that goes wrong that way (three of its four arms are false in almost any
/// world you happen to build), so every arm below is proved in a PAIR: one world where the thing is true and
/// one where it is not, out of the SAME builder, with the underlying oracle's own verdict asserted in both.
/// A change that made <see cref="VoidRule.AHavenStillTakesHer"/> answer a constant reddens half of these
/// whichever constant it picked.</para>
/// </summary>
public class TheLongDarkHasAScheduleTests
{
    // ── One world, two ways. Every number below is the arrive row's own currency: a distance against
    //    OrbitRule.CaptureRange(hill) and a relative speed against OrbitRule.MaxRelativeSpeed.
    private const double Hill = 1.0e9;                 // capture range = max(5·hill, 3 Gm) = 5 Gm
    private const double RibbonEnd = 1_728_000;        // twenty days of picture
    private const double SampleStep = 3600;
    private const double InteriorPass = 500_000;       // well inside the line

    /// <summary>A pass a haven genuinely takes: inside the capture range and under the window's speed.</summary>
    private static VoidRule.HavenPass ABerthInReach(double passSimTime = InteriorPass) => new(
        "luna", "Luna", ArrivalStepRule.ArrivalKind.Orbit,
        Distance: 1.0e9, RelSpeed: 2_000, HillRadius: Hill, PassSimTime: passSimTime);

    /// <summary>The same body on the same line, ten times too far out and four times too fast.</summary>
    private static VoidRule.HavenPass ABerthOutOfReach(double passSimTime = InteriorPass) => new(
        "luna", "Luna", ArrivalStepRule.ArrivalKind.Orbit,
        Distance: 5.0e10, RelSpeed: 20_000, HillRadius: Hill, PassSimTime: passSimTime);

    private static bool TheOracleSaysValid(VoidRule.HavenPass p) =>
        ArrivalStepRule.Check(p.Kind, p.BodyName, p.Distance, p.RelSpeed, p.HillRadius).Valid;

    // ===== 1 · The predicate, proved in both directions =====

    /// <summary>
    /// <b>The anti-vacuous half, and the reason this file exists.</b> The two worlds differ in exactly one
    /// thing — where the berth is — and the arrive row's own ✓/✗ bit is asserted in each before the void is
    /// asked anything, so a <see cref="VoidRule.AHavenStillTakesHer"/> that had stopped reading the oracle at
    /// all could not be green here.
    /// </summary>
    [Fact]
    public void AHavenTheCourseSTILLREACHES_MeansTheClockDoesNotRun()
    {
        VoidRule.HavenPass reachable = ABerthInReach();
        Assert.True(TheOracleSaysValid(reachable), "the world is broken: this pass is not even a valid arrival");

        Assert.True(VoidRule.AHavenStillTakesHer([reachable], RibbonEnd, SampleStep));

        // Tank empty, plan spent, ship free — every other arm of ADRIFT is TRUE, and she is still not adrift,
        // because a berth is in front of her.
        Assert.False(VoidRule.IsAdrift(
            reactionMassPulses: 0, docked: false, aPlanStepCanStillFire: false,
            aHavenStillTakesHer: VoidRule.AHavenStillTakesHer([reachable], RibbonEnd, SampleStep)));
    }

    /// <summary>The other half of the same pair: same tank, same plan, same ship — the berth moved out of
    /// reach, and now the clock runs.</summary>
    [Fact]
    public void AHavenTheCourseREACHESNOT_MeansTheClockRuns()
    {
        VoidRule.HavenPass beyond = ABerthOutOfReach();
        Assert.False(TheOracleSaysValid(beyond), "the world is broken: this pass is still a valid arrival");

        Assert.False(VoidRule.AHavenStillTakesHer([beyond], RibbonEnd, SampleStep));

        Assert.True(VoidRule.IsAdrift(
            reactionMassPulses: 0, docked: false, aPlanStepCanStillFire: false,
            aHavenStillTakesHer: VoidRule.AHavenStillTakesHer([beyond], RibbonEnd, SampleStep)));
    }

    /// <summary>One berth in reach among a crowd of misses is still a way home. Guards the fold's sense — an
    /// <c>All</c> where an <c>Any</c> belongs would kill a captain in a parking orbit.</summary>
    [Fact]
    public void ONEBerthInReachAmongManyIsEnough()
    {
        List<VoidRule.HavenPass> system =
        [
            ABerthOutOfReach(200_000),
            ABerthOutOfReach(400_000),
            ABerthInReach(900_000),
            ABerthOutOfReach(1_000_000),
        ];

        Assert.True(VoidRule.AHavenStillTakesHer(system, RibbonEnd, SampleStep));
        Assert.False(VoidRule.AHavenStillTakesHer(system.Where(p => !TheOracleSaysValid(p)).ToList(), RibbonEnd, SampleStep));
    }

    /// <summary>
    /// #1041's edge law, honoured on the way in. A "pass" pinned to the ribbon's own last sample is where the
    /// picture stopped, not where the ship comes nearest — so it may not be counted as a rescue, however
    /// green its numbers look. The pair: the identical pass one sample earlier IS counted.
    /// </summary>
    [Fact]
    public void APassOffTheENDOfTheRibbonIsNotARescue()
    {
        VoidRule.HavenPass onTheEdge = ABerthInReach(RibbonEnd);
        VoidRule.HavenPass onTheLine = ABerthInReach(RibbonEnd - (10 * SampleStep));

        // Both are valid arrivals by the oracle — the difference is WHERE the picture put them.
        Assert.True(TheOracleSaysValid(onTheEdge));
        Assert.True(TheOracleSaysValid(onTheLine));
        Assert.True(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(onTheEdge.PassSimTime, RibbonEnd, SampleStep));
        Assert.False(ArrivalStepRule.PassIsOffTheEndOfTheRibbon(onTheLine.PassSimTime, RibbonEnd, SampleStep));

        Assert.False(VoidRule.AHavenStillTakesHer([onTheEdge], RibbonEnd, SampleStep));
        Assert.True(VoidRule.AHavenStillTakesHer([onTheLine], RibbonEnd, SampleStep));
    }

    /// <summary>The other three arms, each proved to hold on its own: from the one world where she IS adrift,
    /// flip exactly one fact and she is not. A predicate that had quietly dropped an arm reddens here.</summary>
    [Fact]
    public void EachOfTheOtherThreeArmsAloneKeepsHerOutOfTheVoid()
    {
        const bool noHaven = false;

        Assert.True(VoidRule.IsAdrift(0, docked: false, aPlanStepCanStillFire: false, aHavenStillTakesHer: noHaven));

        Assert.False(VoidRule.IsAdrift(1, docked: false, aPlanStepCanStillFire: false, aHavenStillTakesHer: noHaven));
        Assert.False(VoidRule.IsAdrift(0, docked: true, aPlanStepCanStillFire: false, aHavenStillTakesHer: noHaven));
        Assert.False(VoidRule.IsAdrift(0, docked: false, aPlanStepCanStillFire: true, aHavenStillTakesHer: noHaven));
    }

    /// <summary>An empty sweep is not a haven. (The list a caller hands over when the system has no arrivable
    /// body at all — the honest "nothing out here".)</summary>
    [Fact]
    public void NoHavenPassesAtAllIsNotAWayHome() =>
        Assert.False(VoidRule.AHavenStillTakesHer([], RibbonEnd, SampleStep));

    // ===== 2 · The clock =====

    /// <summary>The owner's dial is twenty, and the two telling days are derived from it rather than typed
    /// beside it — so the whole feature moves when the dial does.</summary>
    [Fact]
    public void TwentyDaysIsTheDial()
    {
        Assert.Equal(20, VoidRule.DaysAdrift);
        Assert.Equal(10, VoidRule.HalfwayDay);
        Assert.Equal(19, VoidRule.OneDayLeftDay);
        Assert.Equal(VoidRule.DaysAdrift / 2, VoidRule.HalfwayDay);
        Assert.Equal(VoidRule.DaysAdrift - 1, VoidRule.OneDayLeftDay);
        Assert.Equal(VoidRule.DaysAdrift * VoidRule.DaySeconds, VoidRule.LookAheadSeconds);
    }

    /// <summary>Nineteen days is not twenty. The day the card goes up is the day BEFORE the last one, and the
    /// pair proves the boundary is where it says it is rather than one either side.</summary>
    [Fact]
    public void TheTwentiethDayIsTheOneThatEndsIt()
    {
        const long declared = 100;
        double DayN(int n) => (declared + n) * VoidRule.DaySeconds;

        Assert.Equal(0, VoidRule.DaysElapsed(declared, DayN(0)));
        Assert.Equal(19, VoidRule.DaysElapsed(declared, DayN(19)));
        Assert.Equal(20, VoidRule.DaysElapsed(declared, DayN(20)));

        Assert.False(VoidRule.TimeIsUp(declared, DayN(19)));
        Assert.False(VoidRule.TimeIsUp(declared, DayN(19) + VoidRule.DaySeconds - 1));
        Assert.True(VoidRule.TimeIsUp(declared, DayN(20)));
        Assert.True(VoidRule.TimeIsUp(declared, DayN(400)));   // a warp that leapt the ending still ends
    }

    /// <summary>A clock that is not running never runs out — the sentinel every pre-#638 save loads with, and
    /// the value the watch writes back the moment she refuels.</summary>
    [Fact]
    public void AClockThatIsNotRunningNeverRunsOut()
    {
        Assert.False(VoidRule.TimeIsUp(VoidRule.ClockNotRunning, 9_999_999));
        Assert.Equal(0, VoidRule.DaysElapsed(VoidRule.ClockNotRunning, 9_999_999));
    }

    /// <summary>The day index is the game's ONE notion of a day rolling over — the hoard watch's own, not a
    /// second one. A private 86,400 here would be a constant governing the wrong world.</summary>
    [Fact]
    public void ThereIsOnlyOneDayInThisGame()
    {
        Assert.Equal(DiscoveryRule.PeriodSeconds, VoidRule.DaySeconds);
        Assert.Equal(DiscoveryRule.PeriodIndex(1_234_567), VoidRule.DayIndex(1_234_567));
    }

    // ===== 3 · The tellings (#761) =====

    /// <summary>The three lines, verbatim as the ruling authored them. Pinned because a sentence a test types
    /// and a sentence a banner types are two sentences.</summary>
    [Fact]
    public void TheThreeTellingsAreTheRulingsOwnWords()
    {
        Assert.Equal("Reaction mass is spent. Nothing answers the helm.", VoidRule.DeclaredLine);
        Assert.Equal("Half the ledger gone. The sail holds its line; the line goes nowhere.", VoidRule.HalfwayLine);
        Assert.Equal("The long dark has a schedule now. One day remains on it.", VoidRule.OneDayLeftLine);

        Assert.Equal(VoidRule.DeclaredLine, VoidRule.Telling(0));
        Assert.Equal(VoidRule.HalfwayLine, VoidRule.Telling(VoidRule.HalfwayDay));
        Assert.Equal(VoidRule.OneDayLeftLine, VoidRule.Telling(VoidRule.OneDayLeftDay));
    }

    /// <summary>Days with nothing to say say nothing — and the anti-vacuous half is the line above, which
    /// proves the three that do.</summary>
    [Fact]
    public void MostDaysHaveNothingToSay()
    {
        int[] quiet = Enumerable.Range(0, VoidRule.DaysAdrift)
            .Where(d => !VoidRule.TellingDays.Contains(d))
            .ToArray();

        Assert.Equal(VoidRule.DaysAdrift - 3, quiet.Length);
        Assert.All(quiet, d => Assert.Null(VoidRule.Telling(d)));
    }

    /// <summary>
    /// <b>A beat a warp can leap over is not a beat.</b> At 10,000× one frame carries a fortnight, so the
    /// tellings are a SPAN — everything in <c>(lastTold, now]</c> — not an equality on the day number.
    /// </summary>
    [Fact]
    public void AWarpThatLeapsAFortnightStillSaysEveryWordOfIt()
    {
        Assert.Equal([0], VoidRule.TellingsBetween(VoidRule.NothingToldYet, 0));
        Assert.Equal([10, 19], VoidRule.TellingsBetween(0, 25));
        Assert.Equal([0, 10, 19], VoidRule.TellingsBetween(VoidRule.NothingToldYet, VoidRule.DaysAdrift));
    }

    /// <summary>Each telling is said ONCE per run: walk the twenty days one at a time, advancing the bookmark
    /// the way the watch does, and exactly three things get said, in order.</summary>
    [Fact]
    public void EachTellingIsSaidOnceAndOnlyOnce()
    {
        var said = new List<int>();
        int lastTold = VoidRule.NothingToldYet;
        for (int day = 0; day <= VoidRule.DaysAdrift; day++)
        {
            IReadOnlyList<int> due = VoidRule.TellingsBetween(lastTold, day);
            if (due.Count == 0)
            {
                continue;
            }

            said.AddRange(due);
            lastTold = day;
        }

        Assert.Equal([0, VoidRule.HalfwayDay, VoidRule.OneDayLeftDay], said);
    }

    // ===== 4 · The death the lane arrives at, which is entirely already-shipped =====

    /// <summary>Everything the void's card needs was on the shelf: the painting, the headline, the three lines
    /// and the place law. Nothing in this lane authored a death — it only had to name the cause.</summary>
    [Fact]
    public void TheVoidsCardWasDressedAllAlong()
    {
        Assert.Equal("death-void.jpg", DeathNarration.ArtFile(DeathCause.Void, DeathPlace.OwnShip));
        Assert.Equal("WHAT HAPPENED — lost to the void", DeathNarration.Headline(DeathCause.Void));

        for (ulong seed = 0; seed < 3; seed++)
        {
            string line = DeathNarration.Line(DeathCause.Void, DeathPlace.OwnShip, seed, bodyName: null);
            Assert.True(
                line.Contains("void", StringComparison.OrdinalIgnoreCase)
                || line.Contains("dark", StringComparison.OrdinalIgnoreCase),
                $"seed {seed} gave a line that is not about the void at all: {line}");
            Assert.DoesNotContain("collector", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{", line, StringComparison.Ordinal);
        }
    }

    /// <summary>#636's law still stands, and the lane obeys it: the void is legal on her own deck and nowhere
    /// else, which is why the client stages it with <see cref="DeathPlace.OwnShip"/> and never rolls for a
    /// place.</summary>
    [Fact]
    public void TheVoidIsStillTheOnePlaceWithNoGroundInIt()
    {
        Assert.True(DeathNarration.CanHappen(DeathCause.Void, DeathPlace.OwnShip));
        foreach (DeathPlace elsewhere in new[] { DeathPlace.LandingParty, DeathPlace.Derelict, DeathPlace.Underground })
        {
            Assert.False(DeathNarration.CanHappen(DeathCause.Void, elsewhere));
        }
    }

    /// <summary>
    /// <b>The fourth piece of the card, which was pointed at the wrong death.</b> With the lane built, a void
    /// death reaches <see cref="DeathNarration.SurfaceCaption"/> — and without an arm of its own it fell
    /// through to the pack's line, <i>"they simply kept coming, and the guard did not hold forever"</i>, over
    /// a captain nothing came for at all. The pair proves the arm is real: the default is still there, and
    /// the void no longer takes it.
    /// </summary>
    [Fact]
    public void NothingCameForHimAndTheCaptionSaysSo()
    {
        string voidCaption = DeathNarration.SurfaceCaption(DeathCause.Void, nerveRanOut: false);
        string packCaption = DeathNarration.SurfaceCaption(DeathCause.Reevers, nerveRanOut: false);

        Assert.Contains("kept coming", packCaption, StringComparison.Ordinal);   // the default still exists
        Assert.DoesNotContain("kept coming", voidCaption, StringComparison.Ordinal);
        Assert.DoesNotContain("nerve", voidCaption, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("could not get home", voidCaption, StringComparison.Ordinal);

        // …and it does not depend on the nerve gauge, because the nerve had nothing to do with it.
        Assert.Equal(voidCaption, DeathNarration.SurfaceCaption(DeathCause.Void, nerveRanOut: true));
    }

    // ===== 5 · Save compat, in bytes =====

    /// <summary>
    /// <b>THE SAVE-COMPAT PROOF, in bytes</b> — the #1057 recipe, one level up. <see cref="LegacyVault"/> is
    /// not a hand-written fixture: it is the exact output of <see cref="VaultSerializer.Save"/> on the build
    /// immediately before #638 (commit 4ce02fc), checksum and all. This build must load it and write it back
    /// CHARACTER FOR CHARACTER.
    ///
    /// <para>That is a stronger claim than "the field defaults sensibly", and it is why the countdown is a
    /// whole SECTION the client hands over as <c>null</c> while no clock is running rather than two fields on
    /// an existing one: the checksum is taken over the payload, so a written-but-idle <c>"void"</c> key would
    /// change the digest of every save ever made and hang the 📛 tampered marker on an honest voyage.</para>
    /// </summary>
    [Fact]
    public void ALegacyVaultRoundTripsByteForByteAcrossTheVoid()
    {
        // The writer indents with the platform's newline, so the fixture is read in the platform's newline
        // too — the only normalization, and it is the checked-out file's line endings, not anything of ours.
        string legacy = LegacyVault.ReplaceLineEndings();

        Vault loaded = VaultSerializer.Load(legacy);

        Assert.False(loaded.Tampered);        // the digest still validates as written
        Assert.Null(loaded.Void);             // an old voyage has no clock running
        Assert.Equal(4_250, loaded.Purse!.Credits);

        string rewritten = VaultSerializer.Save(loaded);
        Assert.Equal(legacy, rewritten);
        Assert.DoesNotContain("void", rewritten, StringComparison.Ordinal);
    }

    /// <summary>…and the other half, so the section is not merely unwritten but unwritten FOR A REASON: a
    /// vault with a clock running carries it, and it comes back the same.</summary>
    [Fact]
    public void AClockThatISRunningRidesTheVault()
    {
        Vault legacy = VaultSerializer.Load(LegacyVault.ReplaceLineEndings());
        var before = new Vault
        {
            Version = legacy.Version,
            SavedSimTime = legacy.SavedSimTime,
            Purse = legacy.Purse,
            Ship = legacy.Ship,
            Heat = legacy.Heat,
            Nerve = legacy.Nerve,
            Logbook = legacy.Logbook,
            Void = new VoidSection { DeclaredDay = 402, LastToldDay = VoidRule.HalfwayDay },
        };

        string written = VaultSerializer.Save(before);
        Assert.Contains("\"void\"", written, StringComparison.Ordinal);

        Vault after = VaultSerializer.Load(written);
        Assert.False(after.Tampered);
        Assert.Equal(402, after.Void!.DeclaredDay);
        Assert.Equal(VoidRule.HalfwayDay, after.Void.LastToldDay);
    }

    /// <summary>A section that carries neither field — the shape a forward-compatible writer could hand back —
    /// reads as "no clock, nothing said", never as "declared on day zero, twenty days ago".</summary>
    [Fact]
    public void AVoidSectionWithNoFieldsInItIsNotADeathSentence()
    {
        var bare = new VoidSection();
        Assert.Equal(VoidRule.ClockNotRunning, bare.DeclaredDay);
        Assert.Equal(VoidRule.NothingToldYet, bare.LastToldDay);
        Assert.False(VoidRule.TimeIsUp(bare.DeclaredDay, 9_999_999));
    }

    // Captured from the build at 4ce02fc (pre-#638). Verbatim — do not reformat, the whole point is the bytes.
    private const string LegacyVault = """
        {
          "version": 1,
          "savedSimTime": 1728000,
          "sections": {
            "purse": {
              "credits": 4250
            },
            "ship": {
              "reactionMassPulses": 0,
              "slugAmmo": 4,
              "missileAmmo": 1,
              "sentryMagazines": [
                99,
                99
              ]
            },
            "heat": {
              "level": 1,
              "raisedAtSimTime": 900000
            },
            "nerve": {
              "nerve": 61.5,
              "monolithSeen": true
            },
            "logbook": {
              "captainName": "Ines Varga",
              "title": "the long way round",
              "note": "tank dry off Ceres"
            }
          },
          "checksum": "70304a0e36f37145cebd2cff8bd06fbb54e7a2bb6ed1cd4967a7ed7ef3474f7a"
        }
        """;
}

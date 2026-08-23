using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #959 · EVERY JOB SAYS WHAT IT TAKES, BEFORE YOU TAKE IT.
///
/// <para>Owner, 2026-08-18, over a ledger row that read <i>"Bring down Mars Depot — Hunt Mars Depot —
/// hole her sail or board her — On the hook — 764 cr"</i>: <i>"What is this job... should I destroy a
/// ship or rob a place? We should make sure these missions all tell clearly what it takes to complete
/// them. We need to know before we decide if we accept or not."</i> And on the offer card the same
/// evening: <i>"The offer where the price is listed is not very clear about what should be done and how
/// long we must fly to do it."</i></para>
///
/// <para>Five laws, and each of them is a thing that was actually wrong on that card:</para>
/// <list type="number">
///   <item><b>ONE VERB PER KIND, FROM A FIXED VOCABULARY.</b> Not "bring down", not "hole her sail" —
///   those are voice and they keep their line. The top line is one of five words, always.</item>
///   <item><b>THE CARD SAYS WHAT THE TARGET IS.</b> A depot hull is a ship by every rule the sim knows
///   and a place by every instinct a reader has. That sentence is the owner's question, answered.</item>
///   <item><b>THE EFFORT LINE IS ASKED OF THE WORLD.</b> The lane time is a Hohmann half-orbit about the
///   body both ends go round, read off the shipped Sol ephemeris — never a typed cruise speed. This
///   repo's named bug class is a sentence reporting one thing while the sim does another, and an ETA is
///   the easiest place in the game to commit it.</item>
///   <item><b>A NUMBER THAT CANNOT BE MEASURED IS NOT PRINTED.</b> No plausible-looking fallback.</item>
///   <item><b>THE PAY IS SIZED AGAINST THE PURSE.</b> 764 cr is a fortune to a beggar and pocket change
///   to a fleet owner, and only the reader's own purse can settle which.</item>
/// </list>
/// </summary>
public sealed class EveryJobSaysWhatItTakesTests
{
    /// <summary>The whole owner-facing vocabulary. If a sixth word ever appears in the verb slot, it is
    /// either in this list or it is a bug — there is no third option.</summary>
    private static readonly string[] FixedVocabulary = ["DESTROY", "BOARD & ROB", "DELIVER", "ESCORT", "FIND"];

    private static IEnumerable<ContractKind> AllKinds() => Enum.GetValues<ContractKind>();

    private static JobFacts Sample(ContractKind kind, JobTargetNature nature = JobTargetNature.Runner) =>
        new(kind, "Mars Depot", nature, "Mars orbit", DistanceMeters: 3.6e9, LaneSeconds: 6 * 86_400.0,
            Reward: 764, PurseCredits: 8_000);

    // ── LAW 1 · ONE VERB PER KIND, AND IT IS IN THE VOCABULARY ─────────────────────────────────────

    /// <summary>Every contract kind names exactly one verb, that verb's face is one of the five fixed
    /// words, and asking twice gives the same answer. RED PROOF: delete an arm of
    /// <c>JobTerms.Verb</c>'s switch and the default arm throws here instead of quietly inventing one.
    /// </summary>
    [Fact]
    public void EveryContractKindNamesExactlyOneVerbFromTheFixedVocabulary()
    {
        foreach (ContractKind kind in AllKinds())
        {
            JobVerb verb = JobTerms.Verb(kind);
            Assert.Equal(verb, JobTerms.Verb(kind));   // a function, not a roll
            string word = JobTerms.VerbWord(verb);
            Assert.Contains(word, FixedVocabulary);
        }
    }

    /// <summary>Every member of the vocabulary has a face, and the five faces are distinct — a
    /// vocabulary with a duplicate in it is a vocabulary of four.</summary>
    [Fact]
    public void TheVocabularyIsFiveDistinctWords()
    {
        string[] faces = Enum.GetValues<JobVerb>().Select(JobTerms.VerbWord).ToArray();
        Assert.Equal(FixedVocabulary.Length, faces.Length);
        Assert.Equal(faces.Length, faces.Distinct().Count());
        Assert.Equal(FixedVocabulary.OrderBy(w => w, StringComparer.Ordinal),
                     faces.OrderBy(w => w, StringComparer.Ordinal));
    }

    /// <summary>The verb slot carries no poetry: line one is a vocabulary word, then an em-dash, then
    /// the target. Nothing else may occupy the first field. RED PROOF: put "Bring down" back in the slot
    /// and the pattern fails on the first kind tested.</summary>
    [Fact]
    public void TheVerbSlotCarriesNoPoetry()
    {
        foreach (ContractKind kind in AllKinds())
        {
            string line = JobTerms.VerbLine(Sample(kind));
            string head = line.Split(" — ")[0];
            Assert.Contains(head, FixedVocabulary);
        }
    }

    /// <summary>No job's plain block names its kind without also carrying its verb. The Mars Depot row's
    /// failure exactly: the word "Hunt" stood on the card and nothing said what a hunt is for.</summary>
    [Fact]
    public void NoPlainBlockNamesAKindWithoutItsVerb()
    {
        foreach (ContractKind kind in AllKinds())
        {
            IReadOnlyList<string> block = JobTerms.PlainBlock(Sample(kind, JobTargetNature.ParkedHull));
            Assert.Equal(4, block.Count);
            string word = JobTerms.VerbWord(JobTerms.Verb(kind));
            Assert.StartsWith(word, block[0], StringComparison.Ordinal);
            // …and no OTHER vocabulary word may lead the line, which is how "destroy or rob?" happened.
            foreach (string other in FixedVocabulary.Where(w => w != word))
            {
                Assert.False(block[0].StartsWith(other, StringComparison.Ordinal),
                    $"{kind} leads with {other} as well as {word}");
            }
        }
    }

    // ── LAW 2 · THE CARD SAYS WHAT THE TARGET IS ───────────────────────────────────────────────────

    /// <summary>Every kind states a completion condition, and it is a finished sentence. RED PROOF:
    /// blank any arm of <c>Completion</c> and the length assert fires.</summary>
    [Fact]
    public void EveryKindSaysWhatCompletesIt()
    {
        foreach (ContractKind kind in AllKinds())
        {
            string takes = JobTerms.TakesLine(Sample(kind));
            Assert.StartsWith("Takes: ", takes, StringComparison.Ordinal);
            Assert.True(takes.Length > "Takes: ".Length + 20, $"{kind} says almost nothing: {takes}");
            Assert.EndsWith(".", takes.TrimEnd(), StringComparison.Ordinal);
        }
    }

    /// <summary>THE MARS DEPOT SENTENCE. A parked hull is called a hull and told apart from a runner, in
    /// the same breath as the two ways of finishing her. RED PROOF: drop the nature clause and the
    /// "not a runner" assert goes red on the row the owner photographed.</summary>
    [Fact]
    public void AParkedDepotHullIsToldApartFromARunner()
    {
        string parked = JobTerms.TakesLine(Sample(ContractKind.Hunt, JobTargetNature.ParkedHull));
        Assert.Contains("hole her sail OR board her", parked, StringComparison.Ordinal);
        Assert.Contains("parked in Mars orbit", parked, StringComparison.Ordinal);
        Assert.Contains("not a runner", parked, StringComparison.Ordinal);

        string running = JobTerms.TakesLine(Sample(ContractKind.Hunt, JobTargetNature.Runner));
        Assert.Contains("under way", running, StringComparison.Ordinal);
        Assert.DoesNotContain("not a runner", running, StringComparison.Ordinal);
        Assert.NotEqual(parked, running);
    }

    /// <summary>Every target nature has its own clause — no two kinds of thing get the same sentence,
    /// which is the failure mode where the field exists and says nothing.</summary>
    [Fact]
    public void EveryTargetNatureHasItsOwnClause()
    {
        string[] clauses = Enum.GetValues<JobTargetNature>()
            .Select(n => JobTerms.TakesLine(Sample(ContractKind.Hunt, n)))
            .ToArray();
        Assert.Equal(clauses.Length, clauses.Distinct(StringComparer.Ordinal).Count());
    }

    // ── LAW 3 · THE EFFORT LINE IS ASKED OF THE WORLD ──────────────────────────────────────────────

    /// <summary>
    /// The lane time is the Hohmann half-orbit the plotting table itself teaches, over the SHIPPED Sol
    /// ephemeris — Earth's rail to Mars's rail about the Sun. Nothing here is typed: the two radii and
    /// the Sun's μ are read off the scenario, and the expected answer is derived from
    /// <see cref="TransferMath.Hohmann"/>, which the plotter and the labs already fly by.
    /// <para>RED PROOF: replace <c>JobEffort.LaneSeconds</c> with any straight-line coast at any invented
    /// cruise speed and the sanity band below (a real Earth→Mars transfer is eight and a half months, not
    /// weeks and not years) rejects it.</para>
    /// </summary>
    [Fact]
    public void TheLaneTimeIsAHohmannOverTheShippedEphemeris()
    {
        var sol = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        CelestialBody sun = sol.Bodies.Single(b => b.ParentId is null);
        CelestialBody earth = sol.Bodies.Single(b => b.Id == "earth");
        CelestialBody mars = sol.Bodies.Single(b => b.Id == "mars");

        double lane = JobEffort.LaneSeconds(sun.Mu, earth.OrbitRadius, mars.OrbitRadius);

        Assert.Equal(TransferMath.Hohmann(earth.OrbitRadius, mars.OrbitRadius, sun.Mu).TransferSeconds,
                     lane, 3);
        // …and it is the number a captain would recognise: an Earth→Mars sail is months, not weeks.
        double days = lane / 86_400.0;
        Assert.InRange(days, 200.0, 320.0);
    }

    /// <summary>The SAME arithmetic, read about the planet instead of the Sun, gives a Mars-local hop in
    /// DAYS — which is the whole reason the shared primary matters. Read against the Sun those two ends
    /// sit on one rail and the answer would be half a Martian year, a sentence flatly disagreeing with
    /// the sim. RED PROOF: point the caller at the Sun for a local hop and this goes red by two orders
    /// of magnitude.</summary>
    [Fact]
    public void ALocalHopIsReadAboutItsOwnPlanetAndComesOutInDays()
    {
        var sol = CircularOrbitEphemeris.FromScenario(SimulatorTests.LoadSol());
        CelestialBody sun = sol.Bodies.Single(b => b.ParentId is null);
        CelestialBody mars = sol.Bodies.Single(b => b.Id == "mars");

        // Two Mars-orbit rails: a low berth and a depot further out. Both radii are Mars-local.
        double lowBerth = mars.BodyRadius * 3;
        double depot = mars.BodyRadius * 40;

        double local = JobEffort.LaneSeconds(mars.Mu, lowBerth, depot);
        double asIfHeliocentric = JobEffort.LaneSeconds(sun.Mu, mars.OrbitRadius, mars.OrbitRadius);

        Assert.InRange(local / 86_400.0, 0.05, 30.0);
        // The wrong frame is not merely different — it is wrong by a factor of tens.
        Assert.True(asIfHeliocentric > local * 20,
            $"reading a Mars-local hop about the Sun gave {asIfHeliocentric / 86_400.0:F1} d vs the honest {local / 86_400.0:F1} d");
    }

    /// <summary>Two ends on one rail degenerate to half that rail's period — documented, honest, and
    /// exactly what a same-orbit rendezvous costs in phasing. Pinned so nobody "fixes" it into a zero.
    /// </summary>
    [Fact]
    public void TwoEndsOnOneRailCostHalfThatRailsPeriod()
    {
        const double mu = 1.32712440018e20;
        const double r = 1.495978707e11;
        double half = Math.PI * Math.Sqrt(r * r * r / mu);
        Assert.Equal(half, JobEffort.LaneSeconds(mu, r, r), 3);
    }

    // ── LAW 4 · AN UNMEASURABLE NUMBER IS NOT PRINTED ──────────────────────────────────────────────

    /// <summary>A world that cannot be asked returns zero, not a guess. RED PROOF: return any positive
    /// fallback from <c>LaneSeconds</c> and every case below goes red.</summary>
    [Theory]
    [InlineData(0.0, 1e11, 2e11)]
    [InlineData(1e20, 0.0, 2e11)]
    [InlineData(1e20, 1e11, 0.0)]
    [InlineData(1e20, -1.0, 2e11)]
    [InlineData(double.NaN, 1e11, 2e11)]
    public void AnUnaskableWorldYieldsNoLaneTime(double mu, double r1, double r2) =>
        Assert.Equal(0.0, JobEffort.LaneSeconds(mu, r1, r2));

    /// <summary>The effort line drops whichever half it has no number for, and never prints a digit it
    /// was not given. RED PROOF: make the no-distance branch print "0 km" and the digit sweep fires.</summary>
    [Fact]
    public void TheEffortLineNeverPrintsANumberItWasNotGiven()
    {
        JobFacts blind = Sample(ContractKind.Hunt) with { DistanceMeters = double.NaN, LaneSeconds = 0.0 };
        string line = JobTerms.EffortLine(blind);
        Assert.DoesNotMatch(new Regex(@"\d"), line);

        JobFacts distanceOnly = blind with { DistanceMeters = 3.6e9 };
        Assert.Contains("3.60 M km", JobTerms.EffortLine(distanceOnly), StringComparison.Ordinal);
        Assert.DoesNotContain("by the lanes", JobTerms.EffortLine(distanceOnly), StringComparison.Ordinal);

        JobFacts timeOnly = blind with { LaneSeconds = 6 * 86_400.0 };
        Assert.Contains("~6 d by the lanes", JobTerms.EffortLine(timeOnly), StringComparison.Ordinal);
        Assert.DoesNotContain("≈", JobTerms.EffortLine(timeOnly), StringComparison.Ordinal);
    }

    /// <summary>Both halves, when both are known — the owner's "how long we must fly to do it", in the
    /// same ladder the plotting panel speaks in.</summary>
    [Fact]
    public void TheFullEffortLineNamesDistanceAndTime()
    {
        string line = JobTerms.EffortLine(Sample(ContractKind.Hunt));
        Assert.Equal("≈ 3.60 M km · ~6 d by the lanes", line);
    }

    /// <summary>The two kinds with no voyage in them say so, rather than printing a distance of zero.
    /// RED PROOF: delete the Intel arm and the line reads "≈ 3.60 M km" for a tip bought across a table.
    /// </summary>
    [Theory]
    [InlineData(ContractKind.Intel)]
    [InlineData(ContractKind.Crack)]
    public void AJobWithNoVoyageInItSaysSo(ContractKind kind)
    {
        string line = JobTerms.EffortLine(Sample(kind));
        Assert.StartsWith("No flying —", line, StringComparison.Ordinal);
        Assert.DoesNotContain("M km", line, StringComparison.Ordinal);
    }

    // ── LAW 5 · THE PAY IS SIZED AGAINST THE PURSE ─────────────────────────────────────────────────

    /// <summary>The owner's own row: 764 cr against a working purse is <i>small</i>, and the card now
    /// says it before he has to work it out. RED PROOF: drop the size word and the exact-string assert
    /// goes red.</summary>
    [Fact]
    public void TheOwnersRowReadsSevenSixtyFourSmall()
    {
        Assert.Equal("764 cr · small", JobTerms.PayLine(Sample(ContractKind.Hunt)));
    }

    /// <summary>Every word in the size vocabulary is reachable from a real purse, and the ladder rises.
    /// This is the guard against the threshold that selects everything: if any boundary were mis-set,
    /// one of these four words would never be spoken.</summary>
    [Theory]
    [InlineData(100, 10_000, "small")]     // 0.01
    [InlineData(1_400, 10_000, "small")]   // 0.14, just under SmallCeiling
    [InlineData(1_500, 10_000, "fair")]    // 0.15, exactly at it — the ceiling is exclusive below
    [InlineData(5_900, 10_000, "fair")]    // 0.59
    [InlineData(6_000, 10_000, "good")]    // 0.60
    [InlineData(19_900, 10_000, "good")]   // 1.99
    [InlineData(20_000, 10_000, "fortune")]// 2.00
    public void TheSizeWordClimbsWithTheRatio(int reward, int purse, string expected) =>
        Assert.Equal(expected, JobTerms.SizeWord(reward, purse));

    /// <summary>A flat-broke captain does not read every errand as the score of his life: below the floor
    /// the purse is treated as the floor. RED PROOF: remove the floor and 764 cr to a captain holding
    /// nothing becomes a "fortune" — a division by almost zero dressed as advice.</summary>
    [Fact]
    public void ABrokeCaptainStillGetsAnHonestWord()
    {
        Assert.Equal(JobTerms.SizeWord(764, JobTerms.PurseFloorCredits), JobTerms.SizeWord(764, 0));
        Assert.Equal("good", JobTerms.SizeWord(764, 0));
        Assert.Equal("fortune", JobTerms.SizeWord(50_000, 0));
    }

    /// <summary>A job that pays in something other than coin says that, instead of "0 cr · nothing".</summary>
    [Fact]
    public void AJobWithNoCoinInItSaysSo()
    {
        string line = JobTerms.PayLine(Sample(ContractKind.Intel) with { Reward = 0 });
        Assert.Equal("No coin — the job pays in something else.", line);
    }

    // ── The lane-time ladder ───────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(3600.0, "1 h")]
    [InlineData(9 * 3600.0, "9 h")]
    [InlineData(6 * 86_400.0, "6 d")]
    [InlineData(258.8 * 86_400.0, "258.8 d")]
    [InlineData(1200 * 86_400.0, "3.3 y")]
    public void LaneTimeClimbsTheNavDesksOwnLadder(double seconds, string expected) =>
        Assert.Equal(expected, JobTerms.FormatLaneTime(seconds));

    // ── The block, whole ───────────────────────────────────────────────────────────────────────────

    /// <summary>The exact four lines the Mars Depot row should have carried. This is the one test that
    /// reads like the screenshot the owner filed, and the one that goes red if any layer above quietly
    /// re-words itself.</summary>
    [Fact]
    public void TheMarsDepotRowReadsWholeAndPlain()
    {
        var facts = new JobFacts(ContractKind.Hunt, "Mars Depot", JobTargetNature.ParkedHull,
            "Mars orbit", DistanceMeters: 3.6e9, LaneSeconds: 6 * 86_400.0, Reward: 764, PurseCredits: 8_000);

        IReadOnlyList<string> block = JobTerms.PlainBlock(facts);

        Assert.Equal("DESTROY — Mars Depot, Mars orbit", block[0]);
        Assert.Equal(
            "Takes: hole her sail OR board her — either one ends it. "
            + "She is a hull parked in Mars orbit, not a runner — she trades where she sits and will not flee.",
            block[1]);
        Assert.Equal("≈ 3.60 M km · ~6 d by the lanes", block[2]);
        Assert.Equal("764 cr · small", block[3]);
    }

    /// <summary>Culture cannot move a number on a card. The block is invariant-formatted throughout —
    /// a comma decimal separator on a Finnish machine would turn "3.60 M km" into "3,60 M km" and
    /// "764 cr" into something worse.</summary>
    [Fact]
    public void TheBlockReadsTheSameInEveryCulture()
    {
        CultureInfo before = CultureInfo.CurrentCulture;
        try
        {
            // Built by hand rather than by name: CI runs in globalization-invariant mode, where
            // new CultureInfo("fi-FI") throws — but a cloned invariant culture with a comma decimal
            // separator reproduces exactly the hazard this guards against.
            var comma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            comma.NumberFormat.NumberDecimalSeparator = ",";
            comma.NumberFormat.NumberGroupSeparator = " ";
            CultureInfo.CurrentCulture = comma;
            var facts = new JobFacts(ContractKind.Hunt, "Mars Depot", JobTargetNature.ParkedHull,
                "Mars orbit", 3.6e9, 6 * 86_400.0, 1_764, 8_000);
            Assert.Equal("≈ 3.60 M km · ~6 d by the lanes", JobTerms.EffortLine(facts));
            Assert.Equal("1,764 cr · fair", JobTerms.PayLine(facts));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}

using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #973 L2 · HARLAN FESS, AS LAWS. The rep is the practice ground for NPC approach, and the four things he
/// has to get right are all cadence: WHERE he is, WHAT he says to the policy you hold, HOW RARELY he reads
/// the wrong name off the file, and HOW LONG he remembers being told no.
///
/// <para>Every one of them is a pure function of the thread, the station and a count, so every one of them
/// can be swept over a whole run of visits instead of poked at once — which matters, because three of the
/// four are statements about a SEQUENCE ("never two in a row", "never twice running", "per visit") and a
/// single-point assertion cannot see a sequence at all.</para>
/// </summary>
public sealed class TheRepNeverRemembersYourFaceTests
{
    private const string Thread = "9f2c40a1b7e34d5f8c1a6b0d2e934771";

    private static readonly string[] Stations =
        ["ceres-port", "vesta-hub", "phobos-yard", "callisto-gate", "titan-roads", "luna-transfer"];

    /// <summary>One thread's whole run of dock visits, as the answer to "was Fess at this one".</summary>
    private static bool[] TheRun(string threadId, int visits, string[]? stations = null)
    {
        stations ??= Stations;
        bool[] met = new bool[visits];
        for (int v = 0; v < visits; v++)
        {
            met[v] = NebulaRep.IsWorkingThisStation(threadId, stations[v % stations.Length], v);
        }

        return met;
    }

    // ── Where he is ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE IS NEVER AT TWO DOCKINGS RUNNING. The owner's first cadence rule, and the one the joke needs
    /// most: a salesman who is at every port is furniture, and one who is at two in a row is a stalker.
    ///
    /// <para><b>Proven RED</b> by dropping the rota gate from <c>IsWorkingThisStation</c> and leaving only
    /// the desk roll — the sweep then finds adjacent pairs within the first dozen visits of most threads.</para>
    /// </summary>
    [Fact]
    public void HeIsNeverAtTwoDockingsRunning()
    {
        for (int t = 0; t < 40; t++)
        {
            string threadId = $"{Thread}-{t}";
            bool[] met = TheRun(threadId, 90);

            for (int v = 1; v < met.Length; v++)
            {
                Assert.False(met[v] && met[v - 1],
                             $"thread {threadId}: Fess worked visits {v - 1} and {v} back to back");
            }
        }
    }

    /// <summary>
    /// AT MOST ONE DOCKING IN THREE. Stated as a ceiling over the whole run rather than as a frequency,
    /// because a ceiling is what the owner asked for and a frequency is a thing that drifts.
    /// </summary>
    [Fact]
    public void HeWorksAtMostOneDockingInThree()
    {
        for (int t = 0; t < 40; t++)
        {
            string threadId = $"{Thread}-{t}";
            bool[] met = TheRun(threadId, 120);
            int worked = met.Count(x => x);

            Assert.True(worked * NebulaRep.RotaPeriod <= met.Length,
                        $"thread {threadId}: Fess worked {worked} of {met.Length} dockings — over the "
                        + $"one-in-{NebulaRep.RotaPeriod} ceiling");
        }
    }

    /// <summary>…and he is not a rumour either: over a long enough run of ports he does turn up. A cadence
    /// rule that can be satisfied by never appearing is a rule about nothing, and this is the anti-vacuous
    /// half of the two sweeps above.</summary>
    [Fact]
    public void ButHeDoesTurnUp()
    {
        for (int t = 0; t < 40; t++)
        {
            string threadId = $"{Thread}-{t}";
            Assert.Contains(true, TheRun(threadId, 120));
        }
    }

    /// <summary>Deterministic in the thread, the station and the visit count — no clock, no
    /// <c>Random</c>. A reloaded save meets the same man in the same concourse.</summary>
    [Fact]
    public void TheSameVisitAlwaysAnswersTheSame()
    {
        for (int v = 0; v < 60; v++)
        {
            foreach (string station in Stations)
            {
                bool once = NebulaRep.IsWorkingThisStation(Thread, station, v);
                Assert.Equal(once, NebulaRep.IsWorkingThisStation(Thread, station, v));
            }
        }
    }

    /// <summary>And it is a per-UNIVERSE rota: two threads do not put him on the same watch, or the
    /// "seeded per game thread" in the brief would be decoration.</summary>
    [Fact]
    public void TwoThreadsDoNotWalkTheSameRota()
    {
        bool[] a = TheRun($"{Thread}-A", 120);
        bool[] b = TheRun($"{Thread}-B", 120);

        Assert.NotEqual(a, b);
    }

    /// <summary>A visit index before the first docking is not a visit. Total rather than throwing: the
    /// client asks this on frames where nothing has happened yet.</summary>
    [Fact]
    public void ThereIsNoVisitBeforeTheFirstOne()
    {
        Assert.False(NebulaRep.IsWorkingThisStation(Thread, Stations[0], -1));
        Assert.False(NebulaRep.IsWorkingThisStation(Thread, Stations[0], -7));
    }

    // ── What he says ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EVERY TIER HAS A LINE, AND THE BUTTONS MATCH IT. Total over the enum, because the one thing a
    /// salesman may never be is speechless — and because a tier added tomorrow that fell through to a
    /// default arm would sell the wrong policy at the wrong price with a straight face.
    /// </summary>
    [Fact]
    public void ThePitchIsTotalOverTheTiers()
    {
        foreach (InsuranceTier tier in Enum.GetValues<InsuranceTier>())
        {
            NebulaRep.RepPitch pitch = NebulaRep.PitchFor(tier, "Vane");

            Assert.False(string.IsNullOrWhiteSpace(pitch.Line), $"{tier} left him with nothing to say");
            Assert.Contains("Vane", pitch.Line, StringComparison.Ordinal);
            Assert.NotEmpty(pitch.Offers);

            // The captain's line is on every card he ever raises, true or not.
            Assert.Contains(pitch.Offers, o => o.Move == NebulaRep.RepMove.AlreadyHaveAPolicy);

            // Every priced button costs what Core says that tier costs, and every free one is free.
            foreach (NebulaRep.RepOffer offer in pitch.Offers)
            {
                int expected = offer.Move switch
                {
                    NebulaRep.RepMove.BuyBasic => NebulaRep.BasicPremiumCr,
                    NebulaRep.RepMove.BuyPremium => NebulaRep.PremiumPremiumCr,
                    _ => 0,
                };
                Assert.Equal(expected, offer.PriceCr);
                Assert.False(string.IsNullOrWhiteSpace(offer.Label));
            }
        }
    }

    /// <summary>
    /// HE ONLY EVER SELLS YOU UP. An uninsured captain is offered both tiers, a Basic captain only Premium,
    /// and a Premium captain nothing at all — the one moment he is likeable, and the moment a sale button
    /// would ruin.
    ///
    /// <para><b>Proven RED</b> by adding a <c>BuyPremium</c> offer to the Premium arm of
    /// <c>PitchFor</c>: the last assertion names it.</para>
    /// </summary>
    [Fact]
    public void HeNeverSellsATierYouAlreadyHold()
    {
        NebulaRep.RepMove[] none = [.. NebulaRep.PitchFor(InsuranceTier.None, "Vane").Offers.Select(o => o.Move)];
        Assert.Contains(NebulaRep.RepMove.BuyBasic, none);
        Assert.Contains(NebulaRep.RepMove.BuyPremium, none);

        NebulaRep.RepMove[] basic = [.. NebulaRep.PitchFor(InsuranceTier.Basic, "Vane").Offers.Select(o => o.Move)];
        Assert.DoesNotContain(NebulaRep.RepMove.BuyBasic, basic);
        Assert.Contains(NebulaRep.RepMove.BuyPremium, basic);

        NebulaRep.RepMove[] premium = [.. NebulaRep.PitchFor(InsuranceTier.Premium, "Vane").Offers.Select(o => o.Move)];
        Assert.DoesNotContain(NebulaRep.RepMove.BuyBasic, premium);
        Assert.DoesNotContain(NebulaRep.RepMove.BuyPremium, premium);
        Assert.All(premium, m => Assert.Equal(0, NebulaRep.PremiumFor(InsuranceTier.None)));
    }

    /// <summary>The "that's not my name" button exists ONLY when he has just used the wrong one. It is the
    /// player's only handle on the rarest thing in the feature, and an always-present one would advertise
    /// a bleed that had not happened.</summary>
    [Fact]
    public void TheWrongNameButtonOnlyExistsWhenHeUsedTheWrongName()
    {
        foreach (InsuranceTier tier in Enum.GetValues<InsuranceTier>())
        {
            Assert.DoesNotContain(NebulaRep.PitchFor(tier, "Vane").Offers,
                                  o => o.Move == NebulaRep.RepMove.ThatsNotMyName);
            Assert.Contains(NebulaRep.PitchFor(tier, "Roake", bleeding: true).Offers,
                            o => o.Move == NebulaRep.RepMove.ThatsNotMyName);
        }
    }

    /// <summary>The answer to the captain's line changes after the first death — and says nothing whatever
    /// about the face he is looking at.</summary>
    [Fact]
    public void TheReplyToThePolicyLineChangesOnceYouHaveDied()
    {
        string firstLife = NebulaRep.PolicyClaimReply(0);
        string reborn = NebulaRep.PolicyClaimReply(1);

        Assert.NotEqual(firstLife, reborn);
        Assert.Equal(reborn, NebulaRep.PolicyClaimReply(4));
        Assert.Contains("never forget a file", reborn, StringComparison.Ordinal);
        Assert.DoesNotContain("face", reborn, StringComparison.OrdinalIgnoreCase);
    }

    // ── The prices ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE PREMIUMS ARE QUOTED, NOT INVENTED. #227's vendor lane has not landed, so the only numbers in the
    /// tree that know what a policy is WORTH are the ones <see cref="InsuranceRule"/> already spends at the
    /// clinic — and each tier's premium is exactly the bill that tier makes go away. Pinned against
    /// <c>InsuranceRule</c> rather than against a literal, so re-pricing the clinic re-prices the salesman.
    /// </summary>
    [Fact]
    public void ThePremiumsAreQuotedFromTheCoverTheyBuy()
    {
        Assert.Equal(InsuranceRule.BaseClinicBillCr / 2, NebulaRep.BasicPremiumCr);
        Assert.Equal(InsuranceRule.BaseClinicBillCr, NebulaRep.PremiumPremiumCr);
        Assert.Equal(0, NebulaRep.PremiumFor(InsuranceTier.None));
    }

    /// <summary>A sale leaves exactly one kind of policy behind, and it is one the rest of the game already
    /// understands: an active tier with a term on it.</summary>
    [Fact]
    public void BuyingLeavesAPolicyTheClinicCanRead()
    {
        const double now = 12_345.0;

        PirateInsurance basic = NebulaRep.PolicyAfterBuying(InsuranceTier.Basic, now);
        Assert.Equal(InsuranceTier.Basic, basic.Tier);
        Assert.True(basic.IsActiveAt(now));
        Assert.True(basic.IsActiveAt(now + NebulaRep.PremiumTermSeconds));
        Assert.False(basic.IsActiveAt(now + NebulaRep.PremiumTermSeconds + 1));

        // …and the clinic really does read it: the bill halves, which is the whole of what Basic is.
        RebirthOutcome plain = InsuranceRule.DefaultRebirth(100);
        Assert.Equal(InsuranceRule.BaseClinicBillCr / 2,
                     InsuranceRule.ApplyToRebirth(basic, now, plain).ClinicBillCr);

        Assert.Equal(PirateInsurance.Uninsured, NebulaRep.PolicyAfterBuying(InsuranceTier.None, now));
    }

    // ── The bleed ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Which meetings in a long run bleed, for a thread that has buried somebody.</summary>
    private static bool[] TheBleeds(string threadId, int meetings, int retired = 1)
    {
        bool[] bled = new bool[meetings + 1];
        for (int m = 1; m <= meetings; m++)
        {
            bled[m] = NebulaRep.BleedsThePreviousName(threadId, m, retired);
        }

        return bled;
    }

    /// <summary>
    /// NOT IN THE FIRST THREE MEETINGS. The joke only lands on a man you have already learned to expect,
    /// so the rarity is not allowed to fire while he is still being established.
    ///
    /// <para><b>Proven RED</b> by lowering <c>MeetingsBeforeTheBleed</c> to 0 — meeting 1 of several
    /// threads then bleeds and this names the thread.</para>
    /// </summary>
    [Fact]
    public void HeNeverReadsTheWrongNameInTheFirstThreeMeetings()
    {
        for (int t = 0; t < 60; t++)
        {
            string threadId = $"{Thread}-{t}";
            for (int m = 1; m <= NebulaRep.MeetingsBeforeTheBleed; m++)
            {
                Assert.False(NebulaRep.BleedsThePreviousName(threadId, m, retiredCaptains: 3),
                             $"thread {threadId}: bled at meeting {m}, inside the first "
                             + $"{NebulaRep.MeetingsBeforeTheBleed}");
            }
        }
    }

    /// <summary>
    /// AND NEVER TWICE RUNNING. Twice in a row is not a slip, it is a character trait, and it would turn
    /// the rarest line in the feature into his catchphrase.
    ///
    /// <para><b>Proven RED</b> by dropping the <c>!TheRollBleeds(threadId, meetingIndex - 1)</c> clause:
    /// the sweep finds adjacent bleeds within a few hundred meetings.</para>
    /// </summary>
    [Fact]
    public void HeNeverReadsTheWrongNameTwiceRunning()
    {
        for (int t = 0; t < 60; t++)
        {
            string threadId = $"{Thread}-{t}";
            bool[] bled = TheBleeds(threadId, 400);

            for (int m = 2; m < bled.Length; m++)
            {
                Assert.False(bled[m] && bled[m - 1],
                             $"thread {threadId}: bled at meetings {m - 1} and {m} back to back");
            }
        }
    }

    /// <summary>With nobody buried there is no previous name to read, so there is no bleed — ever.</summary>
    [Fact]
    public void AThreadThatHasBuriedNobodyNeverBleeds()
    {
        for (int t = 0; t < 20; t++)
        {
            Assert.DoesNotContain(true, TheBleeds($"{Thread}-{t}", 400, retired: 0));
        }
    }

    /// <summary>…but it is a rarity and not a nullity: across enough threads and enough meetings it does
    /// happen. Without this the two sweeps above pass on a function that returns <c>false</c>.</summary>
    [Fact]
    public void ButOnceInALongWhileHeDoes()
    {
        int bled = 0;
        int meetings = 0;

        for (int t = 0; t < 60; t++)
        {
            bool[] run = TheBleeds($"{Thread}-{t}", 200);
            bled += run.Count(x => x);
            meetings += 200;
        }

        Assert.True(bled > 0, "he never once reads the wrong name — the rarity is a nullity");
        Assert.True(bled * 5 < meetings, $"he read the wrong name {bled} times in {meetings} meetings — "
                                         + "that is a character trait, not a slip");
    }

    /// <summary>The ledger note names both captains and quotes the only thing he ever says about it.</summary>
    [Fact]
    public void TheBleedLeavesOneLineTheBlackBookCanFind()
    {
        string note = NebulaRep.BleedLedgerNote("Roake", "Vane");

        Assert.Contains("Roake", note, StringComparison.Ordinal);
        Assert.Contains("Vane", note, StringComparison.Ordinal);
        Assert.Contains(NebulaRep.BleedApology, note, StringComparison.Ordinal);
    }

    // ── Remember-you-said-no ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HE REMEMBERS NO UNTIL THE DOORS SHUT, AND NOT ONE DOCKING LONGER. The gag in one law: within a visit
    /// he takes the answer; the next visit he is delighted to see you.
    ///
    /// <para><b>Proven RED</b> by making <c>AtVisit</c> return <c>this</c> unconditionally — the second
    /// half of this test then finds him still sulking at the next port.</para>
    /// </summary>
    [Fact]
    public void HeRemembersNoForExactlyOneVisit()
    {
        NebulaRepVisit memory = NebulaRepVisit.Fresh;

        Assert.True(memory.MayApproach(0));

        memory = memory.AtVisit(0).WithNo();
        Assert.False(memory.MayApproach(0));
        Assert.False(memory.AtVisit(0).MayApproach(0), "the same visit re-entered must keep the no");

        memory = memory.AtVisit(1);
        Assert.True(memory.MayApproach(1), "a new docking is a fresh man with your name on the file");
    }

    /// <summary>A memory nobody has touched approaches at any visit — the client asks this before the first
    /// concourse and must not be told no.</summary>
    [Fact]
    public void AFreshMemoryNeverHoldsAGrudge()
    {
        for (int v = 0; v < 10; v++)
        {
            Assert.True(NebulaRepVisit.Fresh.MayApproach(v));
        }
    }

    // ── The two of them are two people ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// FESS SELLS; THE ADJUSTER ASSESSES. The arc's only sober witness is the adjuster over a drink
    /// (<c>adjuster-tell</c>), and the temptation to fold the two into one cheerful man would cost arc 2
    /// the difference between a sales manner and a confession. Pinned so a later lane cannot quietly merge
    /// them by reusing the contact id.
    /// </summary>
    [Fact]
    public void TheRepIsNotTheAdjuster()
    {
        NebulaFragment adjuster = NebulaLore.ById("adjuster-tell")!;

        Assert.Equal(NebulaSource.Adjuster, adjuster.Source);
        Assert.DoesNotContain(NebulaRep.RepName, adjuster.Lore, StringComparison.Ordinal);
        Assert.NotEqual("adjuster-tell", NebulaRep.ContactId);
        Assert.Contains("Nebula Mutual", NebulaRep.DisplayName, StringComparison.Ordinal);
    }
}

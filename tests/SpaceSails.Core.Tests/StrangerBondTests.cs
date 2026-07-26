namespace SpaceSails.Core.Tests;

using System.Collections.Generic;
using System.Linq;
using Scare = SpaceSails.Core.StrangerBond.Scare;
using Bond = SpaceSails.Core.StrangerBond.Bond;

/// <summary>
/// STRANGER-BOND · the WARM twin of the ambient-dread system (#429, owner mid-storm 2026-07-20: "adversity
/// makes the sharers bond… it bonds strangers"). These pin the pure spine: the seeded bond gate (cold bonds
/// rarer than warm, deterministic); the seeded outcome selection given who's co-present (a stranger rolls the
/// warm notes, cold bonds deep, no one eligible bonds no one); the goodwill each outcome books through the
/// EXISTING ledger (comment books none, cold runs deeper than warm); the cold/gallows register DISTINCT from
/// every warm pool; and the line pools (non-blank, unique, scare-tied, the cognac hero named).
/// </summary>
public class StrangerBondTests
{
    private const ulong Seed = 0xB0_1D_C0_FF_EEUL;

    private static readonly Scare[] AllScares = System.Enum.GetValues<Scare>();

    // ── The gate: deterministic, and a cold scare bonds RARER than a warm one. ──

    [Fact]
    public void Opens_IsDeterministic_PerSeedIndexAndCold()
    {
        for (int i = 0; i < 60; i++)
        {
            Assert.Equal(StrangerBond.Opens(Seed, i, cold: false), StrangerBond.Opens(Seed, i, cold: false));
            Assert.Equal(StrangerBond.Opens(Seed, i, cold: true), StrangerBond.Opens(Seed, i, cold: true));
        }
    }

    [Fact]
    public void Opens_WarmFiresNearItsChance_ColdRarer()
    {
        const int n = 2000;
        int warm = 0, cold = 0;
        for (int i = 0; i < n; i++)
        {
            if (StrangerBond.Opens(Seed, i, cold: false)) { warm++; }
            if (StrangerBond.Opens(Seed, i, cold: true)) { cold++; }
        }
        double warmRate = warm / (double)n, coldRate = cold / (double)n;
        Assert.InRange(warmRate, StrangerBond.WarmBondChance - 0.06, StrangerBond.WarmBondChance + 0.06);
        Assert.InRange(coldRate, StrangerBond.ColdBondChance - 0.06, StrangerBond.ColdBondChance + 0.06);
        // The owner's rule that the deep scare bonds seldom (but deeply): cold is rarer than warm.
        Assert.True(StrangerBond.ColdBondChance < StrangerBond.WarmBondChance);
        Assert.True(coldRate < warmRate);
    }

    // ── The bound: a cooldown floor exists and is generous (the client's one-bond-per-scare + no-spam law). ──

    [Fact]
    public void Cooldown_IsGenerous_SoBondsCannotSpam()
    {
        // A run of scares can't churn the room into a friendship mill — the floor is well over a minute.
        Assert.True(StrangerBond.CooldownSeconds >= 60.0);
    }

    // ── The outcome selection, given who is co-present. ──

    [Fact]
    public void Outcome_IsDeterministic()
    {
        for (int i = 0; i < 60; i++)
        {
            Assert.Equal(
                StrangerBond.Outcome(Seed, i, false, true, true),
                StrangerBond.Outcome(Seed, i, false, true, true));
        }
    }

    [Fact]
    public void Outcome_NoOneEligible_BondsNoOne()
    {
        for (int i = 0; i < 40; i++)
        {
            Assert.Equal(Bond.None, StrangerBond.Outcome(Seed, i, cold: false, strangerPresent: false, acquaintanceEligible: false));
            Assert.Equal(Bond.None, StrangerBond.Outcome(Seed, i, cold: true, strangerPresent: false, acquaintanceEligible: false));
        }
    }

    [Fact]
    public void Outcome_OnlyAcquaintance_Deepens()
    {
        for (int i = 0; i < 40; i++)
        {
            Assert.Equal(Bond.Deepen, StrangerBond.Outcome(Seed, i, cold: false, strangerPresent: false, acquaintanceEligible: true));
            Assert.Equal(Bond.Deepen, StrangerBond.Outcome(Seed, i, cold: true, strangerPresent: false, acquaintanceEligible: true));
        }
    }

    [Fact]
    public void Outcome_WarmStranger_RollsTheThreeWarmNotes()
    {
        var seen = new HashSet<Bond>();
        for (int i = 0; i < 400; i++)
        {
            Bond o = StrangerBond.Outcome(Seed, i, cold: false, strangerPresent: true, acquaintanceEligible: false);
            // A warm scare with a stranger near only ever plays comment / cognac / new-contact — never a
            // deepen (that's for a half-known face) and never None (a stranger IS eligible).
            Assert.Contains(o, new[] { Bond.Comment, Bond.Drink, Bond.NewContact });
            seen.Add(o);
        }
        // All three warm notes must actually be reachable across a run (the seeded weights spread).
        Assert.Contains(Bond.Comment, seen);
        Assert.Contains(Bond.Drink, seen);
        Assert.Contains(Bond.NewContact, seen);
    }

    [Fact]
    public void Outcome_ColdStranger_AlwaysBondsDeep_NewContact()
    {
        // The gallows register bonds deep or not at all: a stranger becomes a contact outright (no mere word
        // or single glass out past the edge).
        for (int i = 0; i < 200; i++)
        {
            Assert.Equal(Bond.NewContact, StrangerBond.Outcome(Seed, i, cold: true, strangerPresent: true, acquaintanceEligible: false));
            Assert.Equal(Bond.NewContact, StrangerBond.Outcome(Seed, i, cold: true, strangerPresent: true, acquaintanceEligible: true));
        }
    }

    // ── The goodwill each outcome books (through ContactLedger.AddGoodwill — the effect application). ──

    [Fact]
    public void GoodwillFor_CommentBooksNone_OthersPositive()
    {
        // A comment is warmth without a ledger line (nerve steady only); the rest warm the relationship.
        Assert.Equal(0, StrangerBond.GoodwillFor(Bond.Comment, cold: false));
        Assert.Equal(0, StrangerBond.GoodwillFor(Bond.None, cold: false));
        Assert.True(StrangerBond.GoodwillFor(Bond.Drink, cold: false) > 0);
        Assert.True(StrangerBond.GoodwillFor(Bond.NewContact, cold: false) > 0);
        Assert.True(StrangerBond.GoodwillFor(Bond.Deepen, cold: false) > 0);
    }

    [Fact]
    public void GoodwillFor_ColdBondsDeeperThanWarm()
    {
        // "Rarer but DEEPER": a cold bond books more goodwill than its warm counterpart.
        Assert.True(StrangerBond.GoodwillFor(Bond.NewContact, cold: true) > StrangerBond.GoodwillFor(Bond.NewContact, cold: false));
        Assert.True(StrangerBond.GoodwillFor(Bond.Deepen, cold: true) > StrangerBond.GoodwillFor(Bond.Deepen, cold: false));
        // A new contact is a bigger warming than merely standing a drink, which is bigger than a single notch.
        Assert.True(StrangerBond.GoodwillFor(Bond.NewContact, cold: false) >= StrangerBond.GoodwillFor(Bond.Drink, cold: false));
        Assert.True(StrangerBond.GoodwillFor(Bond.Drink, cold: false) > StrangerBond.GoodwillFor(Bond.Deepen, cold: false));
    }

    [Fact]
    public void AlreadyCloseCap_IsSane_APositiveBound()
    {
        // The cap the client filters acquaintances by — a real friend (at/over it) has nothing left for a
        // shared fright to add. Positive, and above a shared-drink's single warming so a bond can reach it.
        Assert.True(StrangerBond.AlreadyCloseGoodwill > 0);
        Assert.True(StrangerBond.AlreadyCloseGoodwill >= StrangerBond.NewContactGoodwill);
    }

    // ── The line pools: non-blank, unique, scare-tied, the cognac hero named. ──

    public static IEnumerable<object[]> WarmOutcomeScarePairs()
    {
        foreach (Bond o in new[] { Bond.Comment, Bond.Drink, Bond.NewContact, Bond.Deepen })
        {
            foreach (Scare s in System.Enum.GetValues<Scare>())
            {
                yield return new object[] { o, s };
            }
        }
    }

    [Theory]
    [MemberData(nameof(WarmOutcomeScarePairs))]
    public void WarmPool_IsNonBlank_AndUnique(Bond outcome, Scare scare)
    {
        IReadOnlyList<string> pool = StrangerBond.LinesFor(outcome, scare);
        Assert.NotEmpty(pool);
        Assert.All(pool, l => Assert.False(string.IsNullOrWhiteSpace(l)));
        Assert.Equal(pool.Count, pool.Distinct().Count());
    }

    [Theory]
    [InlineData(Scare.Shudder)]
    [InlineData(Scare.Signal)]
    [InlineData(Scare.Caution)]
    public void GallowsPool_IsNonBlank_AndUnique(Scare scare)
    {
        IReadOnlyList<string> pool = StrangerBond.GallowsLinesFor(scare);
        Assert.NotEmpty(pool);
        Assert.All(pool, l => Assert.False(string.IsNullOrWhiteSpace(l)));
        Assert.Equal(pool.Count, pool.Distinct().Count());
    }

    [Fact]
    public void None_HasNoLinePool()
    {
        foreach (Scare s in AllScares)
        {
            Assert.Empty(StrangerBond.LinesFor(Bond.None, s));
        }
    }

    [Fact]
    public void DrinkPool_AlwaysNamesTheHeroCognac()
    {
        // The cognac recommendation is the hero beat — every drink line names it, in every scare register.
        foreach (Scare s in AllScares)
        {
            Assert.All(StrangerBond.LinesFor(Bond.Drink, s),
                l => Assert.Contains(StrangerBond.HeroCognac, l));
        }
        // The label is an INVENTED in-world name, not a real brand.
        Assert.False(string.IsNullOrWhiteSpace(StrangerBond.HeroCognac));
    }

    [Fact]
    public void GallowsPool_KeepsTheCognacHero_InTheDarkRegister()
    {
        // The good bottle survives into the gallows register — at least one gallows line per scare still
        // names the cognac ("while there's a barkeep to pour it").
        foreach (Scare s in AllScares)
        {
            Assert.Contains(StrangerBond.GallowsLinesFor(s), l => l.Contains(StrangerBond.HeroCognac));
        }
    }

    [Fact]
    public void DeepenPool_CarriesTheNamePlaceholder()
    {
        // A deepen line names the half-known acquaintance — it carries the {0} the client fills.
        foreach (Scare s in AllScares)
        {
            Assert.All(StrangerBond.LinesFor(Bond.Deepen, s), l => Assert.Contains("{0}", l));
        }
        // …and the warm stranger pools do NOT (a stranger has no name yet to fill).
        Assert.All(StrangerBond.LinesFor(Bond.Comment, Scare.Signal), l => Assert.DoesNotContain("{0}", l));
        Assert.All(StrangerBond.LinesFor(Bond.Drink, Scare.Signal), l => Assert.DoesNotContain("{0}", l));
    }

    [Fact]
    public void WarmAndGallowsPools_AreDistinct_TheRegisterActuallySplits()
    {
        // The cold/gallows register must not overlap the warm voice — a deep-site scare bonds people like a
        // lifeboat, not over a nice meal.
        foreach (Scare s in AllScares)
        {
            IEnumerable<string> warm = StrangerBond.LinesFor(Bond.Comment, s)
                .Concat(StrangerBond.LinesFor(Bond.Drink, s))
                .Concat(StrangerBond.LinesFor(Bond.NewContact, s));
            Assert.Empty(warm.Intersect(StrangerBond.GallowsLinesFor(s)));
        }
    }

    [Theory]
    [InlineData(Scare.Shudder, "shudder", "deck", "hull", "flex", "wave", "ground", "rock", "floor")]
    [InlineData(Scare.Signal, "buzzer", "tone", "noise", "klaxon", "bulkhead")]
    [InlineData(Scare.Caution, "PA", "announcement", "advisory", "rail", "rough", "deliberately", "ceiling", "'routine'", "not itself")]
    public void CommentAndDrinkPools_NameTheScareThatTriggeredThem(Scare scare, params string[] keywords)
    {
        // "Tie the register to the SAME scare that triggered it" (owner). Each warm comment/drink line reads
        // as a reaction to THIS scare, not a generic aside.
        IEnumerable<string> lines = StrangerBond.LinesFor(Bond.Comment, scare)
            .Concat(StrangerBond.LinesFor(Bond.Drink, scare));
        foreach (string line in lines)
        {
            Assert.True(
                keywords.Any(k => line.Contains(k, System.StringComparison.OrdinalIgnoreCase)),
                $"a {scare} line must name the scare: {line}");
        }
    }

    // ── Line selection: deterministic, in the RIGHT pool (warm vs gallows), and it rotates. ──

    [Fact]
    public void Line_IsDeterministic_AndFromTheWarmPool()
    {
        foreach (Bond o in new[] { Bond.Comment, Bond.Drink, Bond.NewContact })
        {
            foreach (Scare s in AllScares)
            {
                for (int i = 0; i < 30; i++)
                {
                    string line = StrangerBond.Line(o, s, cold: false, Seed, i);
                    Assert.Equal(line, StrangerBond.Line(o, s, cold: false, Seed, i));
                    Assert.Contains(line, StrangerBond.LinesFor(o, s));
                }
            }
        }
    }

    [Fact]
    public void Line_ColdOutcome_DrawsFromTheGallowsPool()
    {
        // A cold NewContact (the stranger-in-the-dark) speaks the gallows voice, not the warm new-contact one.
        foreach (Scare s in AllScares)
        {
            for (int i = 0; i < 30; i++)
            {
                string line = StrangerBond.Line(Bond.NewContact, s, cold: true, Seed, i);
                Assert.Contains(line, StrangerBond.GallowsLinesFor(s));
            }
        }
    }

    [Fact]
    public void Line_ColdDeepen_KeepsItsOwnPool_NotGallows()
    {
        // There is no gallows "deepen" — a cold deepen is still the half-known face, warmed hard; it keeps the
        // deepen pool (with its {0}) so the client can fill the name.
        foreach (Scare s in AllScares)
        {
            string line = StrangerBond.Line(Bond.Deepen, s, cold: true, Seed, 0);
            Assert.Contains(line, StrangerBond.LinesFor(Bond.Deepen, s));
            Assert.Contains("{0}", line);
        }
    }

    [Fact]
    public void Line_RotatesThePool_OverARun()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < 40; i++)
        {
            seen.Add(StrangerBond.Line(Bond.Drink, Scare.Signal, cold: false, Seed, i));
        }
        Assert.True(seen.Count >= 2, "the line pool must rotate across a run of bonds");
    }

    // ── The candidate pick: deterministic and always in range. ──

    [Fact]
    public void Pick_IsDeterministic_AndInRange()
    {
        for (int count = 1; count <= 5; count++)
        {
            for (int i = 0; i < 40; i++)
            {
                int p = StrangerBond.Pick(Seed, i, count);
                Assert.Equal(p, StrangerBond.Pick(Seed, i, count));
                Assert.InRange(p, 0, count - 1);
            }
        }
        Assert.Equal(0, StrangerBond.Pick(Seed, 3, 0));  // safe when the room is empty
    }
}

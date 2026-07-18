namespace SpaceSails.Core.Tests;

/// <summary>
/// #305 — the dice come out for the haze. Pins the flown-pass episode currency: it is seeded and fully
/// deterministic (Core law); a hot pass rolls dramatic while a cool pass rolls clean; the QUOTE never
/// rolls (a menu quote is byte-identical whether episodes are live or not — only the flown pass carries
/// the dice); and every episode raises a <see cref="DiceEvent"/> the shared dice tray can show.
/// </summary>
public class AerobrakeEpisodesTests
{
    private static CelestialBody Uranus(Atmosphere? atm) =>
        new("uranus", "Uranus", "sun", 5.793939e15, 2.5362e7, 2.87246e12, 2.65104e9, 5.4, Atmosphere: atm);

    private static Atmosphere UranusAtm => new(RefDensity: 1.4e-5, ScaleHeight: 1.2e5, TopAltitude: 1.0e6);

    private static Aerobrake.PassCost Cool => Aerobrake.CostOfPass(peakG: 1.0, peakDynamicPressurePa: 800);
    private static Aerobrake.PassCost Hot => Aerobrake.CostOfPass(peakG: 2.9, peakDynamicPressurePa: 6000);
    private static Aerobrake.PassCost OverLine => Aerobrake.CostOfPass(peakG: 3.4, peakDynamicPressurePa: 9000);

    [Fact]
    public void TheEpisodeIsSeededDeterministic_SameSeedSameFacesSameLine()
    {
        ulong seed = AerobrakeEpisodes.Seed(passOrdinal: 3, simStateA: 123456, simStateB: 42);

        AerobrakeEpisodes.Episode a = AerobrakeEpisodes.Roll(Cool, seed);
        AerobrakeEpisodes.Episode b = AerobrakeEpisodes.Roll(Cool, seed);

        Assert.Equal(a.Kind, b.Kind);
        Assert.Equal(a.Event.Faces, b.Event.Faces);          // the exact dice cast
        Assert.Equal(a.Event.Total, b.Event.Total);
        Assert.Equal(a.Event.Headline, b.Event.Headline);
        Assert.Equal(a.Cost, b.Cost);                        // the currency the pass pays, to the bit
    }

    [Fact]
    public void TheEventCarriesTwoD6Faces_AndSpellsItsMath()
    {
        DiceEvent ev = AerobrakeEpisodes.Roll(Cool, AerobrakeEpisodes.Seed(1, 999)).Event;

        Assert.Equal(AerobrakeEpisodes.Source, ev.Source);
        Assert.Equal("2D6", ev.DieLabel);
        Assert.Equal(2, ev.Faces.Count);
        Assert.All(ev.Faces, f => Assert.InRange(f, 1, 6));
        Assert.False(string.IsNullOrWhiteSpace(ev.Headline));
        Assert.Contains("2D6:", ev.DescribeMath());
    }

    [Fact]
    public void AHotPassRollsMoreDramaThanACoolPass_TheDiceColourThePhysics()
    {
        // Same seeds, two heat levels: the "load in the corridor" modifier drags a hot pass toward the
        // dramatic/torn end of the table, so across a sweep the hot pass tears far more sails than the cool.
        int coolTears = 0, hotTears = 0;
        for (int i = 0; i < 400; i++)
        {
            ulong seed = AerobrakeEpisodes.Seed(i, 7);
            if (AerobrakeEpisodes.Roll(Cool, seed).HolesSail) { coolTears++; }
            if (AerobrakeEpisodes.Roll(Hot, seed).HolesSail) { hotTears++; }
        }

        Assert.True(hotTears > coolTears,
            $"a hot pass should tear more often than a cool one ({hotTears} vs {coolTears})");
        Assert.True(coolTears < 60, $"a cool pass rarely tears the sail ({coolTears}/400)");
    }

    [Fact]
    public void AHotPass_CanTearTheSail_OnALowRoll_TheDiceScriptedCurrency()
    {
        // The owner's Q3 made real: a bad roll on a hot-but-under-line pass tears the sail the physics
        // alone would have survived. Find one such seed and assert the currency escalated.
        Assert.False(Hot.HolesSail, "the deterministic hot pass is under the 3 g line");

        bool foundTear = false;
        for (int i = 0; i < 200 && !foundTear; i++)
        {
            AerobrakeEpisodes.Episode e = AerobrakeEpisodes.Roll(Hot, AerobrakeEpisodes.Seed(i, 3));
            if (e.Kind == AerobrakeEpisodes.Kind.TornSail)
            {
                Assert.True(e.HolesSail, "a torn-sail episode holes the sail even under the line");
                foundTear = true;
            }
        }

        Assert.True(foundTear, "a hot pass sweep should surface at least one torn-sail episode");
    }

    [Fact]
    public void AnOverTheLinePass_AlwaysReadsOverTheLine_DiceOrNot()
    {
        // The physics crossed the 3 g line — the dice only colour a survivable margin, never overrule a
        // sail the load already split. Every seed reads over-the-line and holes.
        Assert.True(OverLine.HolesSail);
        for (int i = 0; i < 50; i++)
        {
            AerobrakeEpisodes.Episode e = AerobrakeEpisodes.Roll(OverLine, AerobrakeEpisodes.Seed(i, 1));
            Assert.Equal(AerobrakeEpisodes.Kind.OverTheLine, e.Kind);
            Assert.True(e.HolesSail);
        }
    }

    [Fact]
    public void TheQuoteIsByteIdentical_WhetherEpisodesRollOrNot_OnlyFlownPassesCarryTheDice()
    {
        // The seam law: Price never rolls. Rolling a pile of episodes (and toggling the manual hook)
        // cannot move a menu quote by a single bit.
        Aerobrake.Quote before = Aerobrake.Price(Uranus(UranusAtm), 29800, 32);

        try
        {
            Aerobrake.DiceEpisodeHook = c => c with { HolesSail = true };
            for (int i = 0; i < 100; i++)
            {
                AerobrakeEpisodes.Roll(Hot, AerobrakeEpisodes.Seed(i, i));
            }

            Aerobrake.Quote after = Aerobrake.Price(Uranus(UranusAtm), 29800, 32);
            Assert.Equal(before, after); // readonly-record-struct equality: bit-identical
        }
        finally
        {
            Aerobrake.DiceEpisodeHook = null;
        }
    }

    [Fact]
    public void EveryTableBucketIsReachable_AcrossASeedSweep()
    {
        // A survivable pass should, over enough seeds and a spread of heat, visit every survivable bucket —
        // the table isn't dead code. (OverTheLine is physics-only and covered separately.)
        var seen = new HashSet<AerobrakeEpisodes.Kind>();
        Aerobrake.PassCost[] heats =
        [
            Aerobrake.CostOfPass(0.3, 200),
            Aerobrake.CostOfPass(1.5, 1500),
            Aerobrake.CostOfPass(2.9, 6000),
        ];

        foreach (Aerobrake.PassCost c in heats)
        {
            for (int i = 0; i < 500; i++)
            {
                seen.Add(AerobrakeEpisodes.Roll(c, AerobrakeEpisodes.Seed(i, 11)).Kind);
            }
        }

        foreach (AerobrakeEpisodes.Kind k in new[]
        {
            AerobrakeEpisodes.Kind.GracefulSkim, AerobrakeEpisodes.Kind.CleanPass,
            AerobrakeEpisodes.Kind.HeatSpike, AerobrakeEpisodes.Kind.GWobble,
            AerobrakeEpisodes.Kind.CorridorDrama, AerobrakeEpisodes.Kind.TornSail,
        })
        {
            Assert.Contains(k, seen);
        }
    }

    [Fact]
    public void TheDicePool_RollsTwoIndependentSeededFaces()
    {
        // The 2D6 primitive underneath: two faces in [1,6], deterministic, and the two dice do not lock
        // together (a shared seed would give 3+3, 4+4 forever — the salt breaks that).
        DicePool p = DiceRule.RollPool(seed: 0xABCDEF, count: 2, sides: 6);
        Assert.Equal(2, p.Faces.Count);
        Assert.All(p.Faces, f => Assert.InRange(f, 1, 6));
        Assert.Equal(p.Faces, DiceRule.RollPool(0xABCDEF, 2, 6).Faces); // deterministic

        int distinctPairs = 0;
        for (ulong s = 0; s < 200; s++)
        {
            DicePool pool = DiceRule.RollPool(s, 2, 6);
            if (pool.Faces[0] != pool.Faces[1]) { distinctPairs++; }
        }

        Assert.True(distinctPairs > 100, $"the two dice must not correlate ({distinctPairs}/200 pairs differed)");
    }
}

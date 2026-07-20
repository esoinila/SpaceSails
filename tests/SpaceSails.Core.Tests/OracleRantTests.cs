namespace SpaceSails.Core.Tests;

/// <summary>
/// THE STATION ORACLE (issue #425): the pure, seeded ranting-drunk oracle whose nonsense hides the truth.
/// These pin the contract the client leans on: the pool and every line are deterministic in
/// (station, watch, draw, drinks); the stream is MOSTLY nonsense with truths rare and rising with drinks
/// ("a drunk oracle is more prophetic"); a true-line can carry a REAL arc fragment that the KAAMOS/Nebula
/// spines accept; the "room goes quiet" tell is real but unreliable; and no authored line leaks a brace.
/// </summary>
public class OracleRantTests
{
    private static readonly string[] Stations =
        ["the-space-bar", "cinder-roost", "ringside-exchange", "the-tilt", "selene-gate", "red-eye", "the-deep"];

    // ── Determinism (deliverable: pool determinism) ─────────────────────────────────────────────────

    [Fact]
    public void Speak_IsDeterministic_InAllFourInputs()
    {
        for (long watch = 0; watch < 5; watch++)
        {
            for (int draw = 0; draw < 30; draw++)
            {
                OracleLine a = OracleRant.Speak("cinder-roost", watch, draw, drinksBought: 1);
                OracleLine b = OracleRant.Speak("cinder-roost", watch, draw, drinksBought: 1);
                Assert.Equal(a, b); // same seed → identical line, tell bit and all
            }
        }
    }

    [Fact]
    public void Speak_DiffersByStationAndDraw()
    {
        // Two different ports on the same watch/draw should not lock-step (different visions per place).
        OracleLine roost = OracleRant.Speak("cinder-roost", 3, 7, 0);
        OracleLine tilt = OracleRant.Speak("the-tilt", 3, 7, 0);
        // And advancing the draw changes the line stream.
        OracleLine next = OracleRant.Speak("cinder-roost", 3, 8, 0);
        Assert.False(roost.Text == tilt.Text && roost.Text == next.Text,
            "station and draw must both move the stream");
    }

    [Fact]
    public void PresentAt_IsDeterministic_AndSometimesPresentSometimesNot()
    {
        // Stable per (station, watch).
        Assert.Equal(OracleRant.PresentAt("the-tilt", 9000), OracleRant.PresentAt("the-tilt", 9000));

        // Over many watches at one bar she is present some and absent others — a fixture that roams.
        int present = 0, watches = 400;
        for (int w = 0; w < watches; w++)
        {
            if (OracleRant.PresentAt("ringside-exchange", w * OracleRant.WatchSeconds + 1))
            {
                present++;
            }
        }
        Assert.InRange(present, 1, watches - 1);
    }

    // ── Ratio: mostly nonsense, truths rare, drinks make her more prophetic (deliverable 2 + 3) ─────

    [Fact]
    public void Stream_IsMostlyNonsense_AtZeroDrinks()
    {
        (int trueCount, int total) = CountTrue(drinksBought: 0);
        double frac = (double)trueCount / total;
        // Rare, but present — the signal exists and the noise dominates.
        Assert.InRange(frac, 0.03, 0.30);
    }

    [Fact]
    public void BuyingDrinks_RaisesTheTrueRate()
    {
        double sober = (double)CountTrue(0).trueCount / CountTrue(0).total;
        double sodden = (double)CountTrue(4).trueCount / CountTrue(4).total;
        Assert.True(sodden > sober, $"a drunk oracle is more prophetic (sober {sober:F3} vs sodden {sodden:F3})");
    }

    [Fact]
    public void TrueChance_IsCapped_NeverAFirehose()
    {
        Assert.Equal(OracleRant.MaxTrueChance, OracleRant.TrueChance(1000), 6);
        Assert.True(OracleRant.TrueChance(0) < 0.5, "even the ceiling keeps her mostly noise");
    }

    // Count true-lines across every station and a wide draw sweep — a big, representative sample.
    private static (int trueCount, int total) CountTrue(int drinksBought)
    {
        int t = 0, n = 0;
        foreach (string s in Stations)
        {
            for (long watch = 0; watch < 6; watch++)
            {
                for (int draw = 0; draw < 60; draw++)
                {
                    if (OracleRant.Speak(s, watch, draw, drinksBought).IsTrue)
                    {
                        t++;
                    }
                    n++;
                }
            }
        }
        return (t, n);
    }

    // ── A true-line can deliver a REAL arc fragment (deliverable 2) ──────────────────────────────────

    [Fact]
    public void EveryFragmentTruth_CarriesARealPoolId_ThatTheSpineAssembles()
    {
        bool sawKaamos = false, sawNebula = false;
        foreach (OracleRant.OracleTruth t in OracleRant.Truths)
        {
            switch (t.Kind)
            {
                case OracleTruthKind.KaamosFragment:
                    Assert.NotNull(t.FragmentId);
                    Assert.NotNull(KaamosLore.ById(t.FragmentId!)); // a real shard the pool knows
                    Assert.True(new KaamosProgress().Assemble(t.FragmentId!), "the spine assembles it");
                    sawKaamos = true;
                    break;
                case OracleTruthKind.NebulaFragment:
                    Assert.NotNull(t.FragmentId);
                    Assert.NotNull(NebulaLore.ById(t.FragmentId!));
                    Assert.True(new NebulaProgress().Assemble(t.FragmentId!), "the spine assembles it");
                    sawNebula = true;
                    break;
                default:
                    Assert.Null(t.FragmentId); // non-fragment truths carry no id
                    break;
            }
        }
        Assert.True(sawKaamos && sawNebula, "the oracle can leak from BOTH arcs");
    }

    [Fact]
    public void ADeliveredFragmentTruth_ActuallySurfaces_InTheStream()
    {
        // Walk a generous sweep and confirm at least one true-line that assembles an arc fragment appears —
        // so the delivery path is reachable in play, not merely present in the pool.
        var kaamos = new KaamosProgress();
        var nebula = new NebulaProgress();
        foreach (string s in Stations)
        {
            for (long watch = 0; watch < 8; watch++)
            {
                for (int draw = 0; draw < 80; draw++)
                {
                    OracleLine line = OracleRant.Speak(s, watch, draw, drinksBought: 3);
                    if (line is { IsTrue: true, Truth: OracleTruthKind.KaamosFragment, FragmentId: { } kid })
                    {
                        kaamos.Assemble(kid);
                    }
                    else if (line is { IsTrue: true, Truth: OracleTruthKind.NebulaFragment, FragmentId: { } nid })
                    {
                        nebula.Assemble(nid);
                    }
                }
            }
        }
        Assert.True(kaamos.Count > 0, "the oracle surfaces at least one KAAMOS shard over a play sweep");
        Assert.True(nebula.Count > 0, "the oracle surfaces at least one Nebula shard over a play sweep");
    }

    // ── The tell: real but unreliable (deliverable 4) ───────────────────────────────────────────────

    [Fact]
    public void TheQuietTell_FiresOnMostTruths_ButAlsoOnSomeNonsense()
    {
        int trueQuiet = 0, trueTotal = 0, nonsenseQuiet = 0, nonsenseTotal = 0;
        foreach (string s in Stations)
        {
            for (long watch = 0; watch < 6; watch++)
            {
                for (int draw = 0; draw < 80; draw++)
                {
                    OracleLine line = OracleRant.Speak(s, watch, draw, drinksBought: 2);
                    if (line.IsTrue)
                    {
                        trueTotal++;
                        if (line.RoomGoesQuiet) { trueQuiet++; }
                    }
                    else
                    {
                        nonsenseTotal++;
                        if (line.RoomGoesQuiet) { nonsenseQuiet++; }
                    }
                }
            }
        }

        // The tell leans toward truth...
        Assert.True((double)trueQuiet / trueTotal > 0.5, "the hush usually accompanies a truth");
        // ...but it is NOT a definitive marker — nonsense hushes too, so the player can't just trust it.
        Assert.True(nonsenseQuiet > 0, "the tell must lie sometimes — a hush on nonsense keeps sifting real");
    }

    // ── No brace leaks (deliverable 2) ──────────────────────────────────────────────────────────────

    [Fact]
    public void NoAuthoredLine_LeaksABrace()
    {
        foreach (string line in OracleRant.Nonsense)
        {
            Assert.DoesNotContain('{', line);
            Assert.DoesNotContain('}', line);
        }
        foreach (OracleRant.OracleTruth t in OracleRant.Truths)
        {
            Assert.DoesNotContain('{', t.Perception);
            Assert.DoesNotContain('}', t.Perception);
        }
    }

    [Fact]
    public void PoolsAreAmple_AndTheCharacterIsNamed()
    {
        Assert.True(OracleRant.Nonsense.Count >= 20, "a big nonsense pool (brief: 20+)");
        Assert.True(OracleRant.Truths.Count >= 6, "several true-lines across the flavours");
        Assert.False(string.IsNullOrWhiteSpace(OracleRant.FullName));
        Assert.Contains(OracleRant.Nickname, OracleRant.ConsoleLabel);
        Assert.True(OracleRant.IsOracle(OracleRant.ConsoleLabel));
        Assert.False(OracleRant.IsOracle("◈ THE MAGPIE"));
    }
}

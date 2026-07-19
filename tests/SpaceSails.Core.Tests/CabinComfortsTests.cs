namespace SpaceSails.Core.Tests;

/// <summary>
/// The ship's cabin comforts (owner, live playtest 2026-07-19): a sanity-restoring SLEEP in a bunk and a
/// flavour-rich TOILET visit. These pin the deterministic laws — the sleep restore magnitude and its
/// well-rested satiety, the toilet's usual-vs-scare bands and their determinism, and the local-special
/// line substitution — so the client's wiring rests on one tested truth.
/// </summary>
public class CabinComfortsTests
{
    // ─────────────────────────────── SLEEP ───────────────────────────────

    [Fact]
    public void Sleep_RestoresTheSleepChunk_FlatAndLevelIndependent()
    {
        // A whole night is the biggest single restore — flat, level-independent (real rest steadies the
        // hands at any level), riding the SAME #339 relief seam. Same rise at steady-ish, mid, and shot.
        foreach (double nerve in new[] { 5.0, 40.0, 55.0 })
        {
            CabinComforts.SleepResult r = CabinComforts.Sleep(nerve, secondsSinceSleep: double.PositiveInfinity, simTime: 0);
            Assert.False(r.WasRested);
            Assert.Equal(NerveModel.SleepRestore, r.Restored, 6);
            Assert.Equal(nerve + NerveModel.SleepRestore, r.Nerve, 6);
        }
    }

    [Fact]
    public void Sleep_IsTheBiggestSingleRestore_ButDoesNotFullHealFromShot()
    {
        Assert.True(NerveModel.SleepRestore > NerveModel.SharedDrinkRestore);
        Assert.True(NerveModel.SleepRestore > NerveModel.CalmingPillRestore);
        Assert.True(NerveModel.SleepRestore > NerveModel.BarSpecialBaseRestore);

        // From nerves shot (0), one bunk does NOT reach a full gauge — honest, not a heal button.
        CabinComforts.SleepResult r = CabinComforts.Sleep(NerveModel.Min, double.PositiveInfinity, simTime: 0);
        Assert.True(r.Nerve < NerveModel.Max);
    }

    [Fact]
    public void Sleep_ClampsAtTheFullGauge()
    {
        CabinComforts.SleepResult r = CabinComforts.Sleep(90.0, double.PositiveInfinity, simTime: 0);
        Assert.Equal(NerveModel.Max, r.Nerve, 6);
        Assert.Equal(10.0, r.Restored, 6); // 90 → 100, only the headroom
    }

    [Fact]
    public void Sleep_WellRestedWindow_RefusesAndRestoresNothing()
    {
        // Inside the window: toss and turn — no restore, flagged rested. Just past it: the bunk lands.
        double inWindow = CabinComforts.WellRestedWindowSeconds - 1.0;
        double pastWindow = CabinComforts.WellRestedWindowSeconds + 1.0;

        CabinComforts.SleepResult rested = CabinComforts.Sleep(50.0, inWindow, simTime: 0);
        Assert.True(rested.WasRested);
        Assert.Equal(0.0, rested.Restored, 6);
        Assert.Equal(50.0, rested.Nerve, 6);

        CabinComforts.SleepResult tired = CabinComforts.Sleep(50.0, pastWindow, simTime: 0);
        Assert.False(tired.WasRested);
        Assert.Equal(NerveModel.SleepRestore, tired.Restored, 6);
    }

    [Fact]
    public void StillRested_BandsAtTheWindowEdges()
    {
        Assert.True(CabinComforts.StillRested(0.0));                                        // just slept
        Assert.True(CabinComforts.StillRested(CabinComforts.WellRestedWindowSeconds - 1)); // still inside
        Assert.False(CabinComforts.StillRested(CabinComforts.WellRestedWindowSeconds));    // window closed
        Assert.False(CabinComforts.StillRested(-5.0));                                      // clock jumped back → not rested
    }

    [Fact]
    public void SleepSatiety_OutlastsTheSleepClockCost()
    {
        // The honest-grind guard only bites if the rested window is longer than a single sleep's clock
        // advance — otherwise a bunk would always let you bunk again at once. It must outlast it.
        Assert.True(CabinComforts.WellRestedWindowSeconds > CabinComforts.SleepSimSeconds);
    }

    // ─────────────────────────────── TOILET ───────────────────────────────

    [Fact]
    public void Toilet_IsDeterministicForAGivenVisit()
    {
        for (double t = 0; t < 500; t += 37)
        {
            CabinComforts.ToiletVisit a = CabinComforts.VisitToilet(t, null, null);
            CabinComforts.ToiletVisit b = CabinComforts.VisitToilet(t, null, null);
            Assert.Equal(a, b); // same sim second → same visit, every time
        }
    }

    [Fact]
    public void Toilet_UsualVisit_RestoresASmallDab_SmallerThanADrink()
    {
        // The relief is a small dab — smaller than the weakest drink (a lone galley tot).
        Assert.True(CabinComforts.ToiletReliefNerve > 0);
        Assert.True(CabinComforts.ToiletReliefNerve < NerveModel.GalleyTotBaseRestore);

        // A usual visit hands back exactly the relief; a line; not a scare.
        CabinComforts.ToiletVisit v = FirstVisitWhere(scare: false);
        Assert.Equal(CabinComforts.ToiletReliefNerve, v.NerveDelta, 6);
        Assert.False(v.Scare);
        Assert.False(string.IsNullOrWhiteSpace(v.Line));
    }

    [Fact]
    public void Toilet_ScareVisit_CostsASmallDab_WithAnAlarmedLine()
    {
        CabinComforts.ToiletVisit v = FirstVisitWhere(scare: true);
        Assert.True(v.Scare);
        Assert.Equal(-CabinComforts.ToiletScareNerve, v.NerveDelta, 6); // it COSTS a dab
        Assert.True(v.NerveDelta < 0);
        Assert.False(string.IsNullOrWhiteSpace(v.Line));
    }

    [Fact]
    public void Toilet_ScareIsRare_RoughlyOneInTwelve()
    {
        // Over a long stretch of visits the scare rate tracks the ~1-in-12 band on the seeded roll.
        int scares = 0, total = 0;
        for (double t = 0; t < 24000; t += 1)
        {
            if (CabinComforts.VisitToilet(t, null, null).Scare) scares++;
            total++;
        }

        double rate = (double)scares / total;
        double expected = 1.0 / CabinComforts.ScareOneIn;
        Assert.InRange(rate, expected * 0.8, expected * 1.2); // ~8.3%, within a fair band
    }

    [Fact]
    public void Toilet_Docked_SometimesSwearsYouOffTheLocalSpecial()
    {
        // Docked at a bar, at least one line names the local house special and bar (substituted, no braces).
        const string special = "the Rusted Bolt";
        const string bar = "THE ROADSTEAD BAR";
        bool sawRiff = false;

        for (double t = 0; t < 4000; t += 1)
        {
            CabinComforts.ToiletVisit v = CabinComforts.VisitToilet(t, special, bar);
            Assert.DoesNotContain("{0}", v.Line, StringComparison.Ordinal);
            Assert.DoesNotContain("{1}", v.Line, StringComparison.Ordinal);
            if (v.Line.Contains(special, StringComparison.Ordinal) && v.Line.Contains(bar, StringComparison.Ordinal))
            {
                sawRiff = true;
            }
        }

        Assert.True(sawRiff, "expected at least one docked visit to riff on the local special");
    }

    [Fact]
    public void Toilet_NotDocked_NeverNamesASpecial_AndStaysGeneric()
    {
        // Undocked (no bar): the special-riff is never drawn — no stray "swore off" line without a bar.
        for (double t = 0; t < 4000; t += 1)
        {
            CabinComforts.ToiletVisit v = CabinComforts.VisitToilet(t, null, null);
            Assert.DoesNotContain("swearing off", v.Line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("{0}", v.Line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Toilet_BlankBarNames_TreatedAsNotDocked()
    {
        // A berth with no bar can hand blanks — the riff must not fire on empty names.
        for (double t = 0; t < 2000; t += 1)
        {
            CabinComforts.ToiletVisit v = CabinComforts.VisitToilet(t, "  ", "");
            Assert.DoesNotContain("swearing off", v.Line, StringComparison.OrdinalIgnoreCase);
        }
    }

    // Find the first seeded visit (scanning sim seconds) that is / isn't the scare — both bands occur.
    private static CabinComforts.ToiletVisit FirstVisitWhere(bool scare)
    {
        for (double t = 0; t < 5000; t += 1)
        {
            CabinComforts.ToiletVisit v = CabinComforts.VisitToilet(t, null, null);
            if (v.Scare == scare)
            {
                return v;
            }
        }

        throw new Xunit.Sdk.XunitException($"no toilet visit with Scare={scare} found in range");
    }
}

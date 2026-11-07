using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// Fable's lines, batch B (11-07) — R1-F13 (head-office dead air), R1-F23 (impact into a haven), R1-F26/F40
/// (the dead hull and the regolith chill), R1-F69 (the tank is low, underground) and U03-F07 (the gig rock's
/// boarding card). Each pin was watched going RED with its change reverted.
/// </summary>
public sealed class FablesLinesBatchBTests
{
    private const string HeadOffice = KaamosLore.IceMoonBodyId;
    private const string AHive = "miranda";
    // A dead floor that is not the found gallery (which is told in its own, authored voice).
    private static int ADeadFloorOf(string body) => Enumerable.Range(2, 6).Select(n => -n)
        .First(l => !UndergroundComplex.HoldsPressure(body, l) && !UndergroundComplex.IsFound(body, l));

    private static readonly int ADeadFloor = ADeadFloorOf(HeadOffice);
    private static readonly int ADeadFloorInTheHive = ADeadFloorOf(AHive);

    // ── R1-F13 · the head office's dead floors ────────────────────────────────────────────────────────

    [Fact]
    public void ADeadFloorOfTheHeadOfficeIsToldInTheHeadOfficesVoice()
    {
        Assert.True(UndergroundComplex.IsHeadOffice(HeadOffice));
        Assert.False(UndergroundComplex.HoldsPressure(HeadOffice, ADeadFloor));

        IReadOnlyList<UndergroundComplex.Saying> said = UndergroundComplex.ArrivalSayings(
            HeadOffice, 0, ADeadFloor, new UndergroundComplex.ArrivalMemory(WasUnderground: false), null, []);

        Assert.Equal(UndergroundComplex.HeadOfficeDescendingLine,
            said.Single(s => s.Beat == UndergroundComplex.ArrivalBeat.Descending).Text);
        Assert.Equal(UndergroundComplex.HeadOfficeDeadAirLine,
            said.Single(s => s.Beat == UndergroundComplex.ArrivalBeat.DeadAirFirst).Text);

        // …and the second dead floor is the weather, in the same voice.
        var later = UndergroundComplex.ArrivalSayings(
            HeadOffice, ADeadFloor, ADeadFloor, new UndergroundComplex.ArrivalMemory(WasUnderground: true, VacuumWarned: true), null, []);
        Assert.Equal(UndergroundComplex.HeadOfficeDeadAirLine,
            later.Single(s => s.Beat == UndergroundComplex.ArrivalBeat.Air).Text);
    }

    [Fact]
    public void AHiveFloorKeepsItsOldLines()
    {
        IReadOnlyList<UndergroundComplex.Saying> said = UndergroundComplex.ArrivalSayings(
            AHive, 0, ADeadFloorInTheHive, new UndergroundComplex.ArrivalMemory(WasUnderground: false), null, []);

        Assert.Equal(UndergroundComplex.DescendingLine,
            said.Single(s => s.Beat == UndergroundComplex.ArrivalBeat.Descending).Text);
        Assert.Equal(UndergroundComplex.DeadAirLine,
            said.Single(s => s.Beat == UndergroundComplex.ArrivalBeat.DeadAirFirst).Text);
    }

    [Fact]
    public void TheHeadOfficeCardKeepsTheRuleSentenceByteForByteAndDropsTheBuriedPlant()
    {
        const string rule =
            "THE RULE, because it is the only one down here that can kill you: the TOP FLOOR OF EVERY SHAFT " +
            "BAND holds pressure. Nothing else does.";
        string hq = UndergroundComplex.HeadOfficeVacuumCard(HeadOffice, ADeadFloor, 3600);
        string hive = UndergroundComplex.VacuumCard(AHive, ADeadFloorInTheHive, 3600);

        Assert.StartsWith("The doors part on nothing.\n\nNo pressure. The lamps come up along the corridor", hq,
            StringComparison.Ordinal);
        Assert.Contains(rule, hq, StringComparison.Ordinal);
        Assert.Contains(rule, hive, StringComparison.Ordinal);
        Assert.Contains("under the ice and your tank is now the clock.", hq, StringComparison.Ordinal);
        Assert.Contains("reception floors", hq, StringComparison.Ordinal);
        Assert.EndsWith("The plan marks it. Down here the plan has not been wrong yet.", hq, StringComparison.Ordinal);
        Assert.DoesNotContain("regolith", hq, StringComparison.Ordinal);

        // The Hive's card is the one it always was.
        Assert.Contains("under the regolith and your tank is now the clock.", hive, StringComparison.Ordinal);
        Assert.EndsWith("Whether it still holds is not on the plan.", hive, StringComparison.Ordinal);

        // The sums are the same sums: a floor of air is the same distance up whichever building it is.
        (string name, int up) = UndergroundComplex.NearestAir(HeadOffice, ADeadFloor);
        Assert.Contains($"The nearest floor of air is {name} — {up} floor{(up == 1 ? "" : "s")} up.", hq,
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheHeadOfficeLockedLineSpeaksOfADecisionAndTheStopOrderPlateKeepsItsOrder()
    {
        Assert.Equal("🔒 B4 — STAFF ONLY. Not a lock you can argue with: a decision, kept in good repair.",
            UndergroundComplex.HeadOfficeLockedLine("B4 — STAFF ONLY"));
        Assert.StartsWith("The lock is not a lock you can argue with", UndergroundComplex.LockedLine("B4 — STAFF ONLY"),
            StringComparison.Ordinal);

        string plate = StopOrder.Plate;
        Assert.Equal(StopOrder.OrderLine, UndergroundComplex.HeadOfficeLockedLine(plate));
        Assert.Equal(StopOrder.OrderLine, UndergroundComplex.LockedLine(plate));
    }

    [Fact]
    public void ABelowGroundHeadOfficeFloorShudderIsItsOwnPool()
    {
        // Below ground, the head office, no air: its own setting — before open regolith would claim it.
        Assert.Equal(HullShudder.Setting.HeadOfficeVacuum, HullShudder.SettingOutside(
            groundHoldsPressure: false, onRegolith: true, deepSite: true, haven: false,
            isHeadOffice: true, level: ADeadFloor));

        // A Hive floor is still the sealed site's voice (open regolith is never underground), a head-office floor that breathes is still pressurised, and
        // the head office's surface (level 0) is not under the ice.
        Assert.Equal(HullShudder.Setting.DeepSite, HullShudder.SettingOutside(
            false, false, true, false, isHeadOffice: false, level: ADeadFloorInTheHive));
        Assert.Equal(HullShudder.Setting.Pressurised, HullShudder.SettingOutside(
            true, true, true, false, isHeadOffice: true, level: -1));
        Assert.Equal(HullShudder.Setting.Regolith, HullShudder.SettingOutside(
            false, true, true, false, isHeadOffice: true, level: 0));

        IReadOnlyList<string> pool = HullShudder.LinesFor(HullShudder.Setting.HeadOfficeVacuum, HullShudder.HavenRoom.Concourse);
        Assert.Equal(3, pool.Count);
        Assert.Equal(3, pool.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(pool.Intersect(HullShudder.LinesFor(HullShudder.Setting.Regolith, HullShudder.HavenRoom.Concourse)));
        Assert.StartsWith("The floor moves. There is no air to carry it to you", pool[0], StringComparison.Ordinal);
        Assert.Equal("a cold the lamps do nothing about",
            HullShudder.ChillNerveLabel(HullShudder.Setting.HeadOfficeVacuum));
    }

    /// <summary>The canon rule (the Hive's own guard, extended): the head office's new words may be enormous and
    /// kept and may never say what it is for.</summary>
    [Fact]
    public void NothingTheHeadOfficeSaysExplainsAnything()
    {
        string[] forbidden =
            ["reever", "old one", "restore", "backup", "revive", "resurrect", "clone", "slave"];
        var text = new List<string>
        {
            UndergroundComplex.HeadOfficeDescendingLine,
            UndergroundComplex.HeadOfficeDeadAirLine,
            UndergroundComplex.HeadOfficeVacuumCard(HeadOffice, ADeadFloor, 3600),
            UndergroundComplex.HeadOfficeLockedLine("B4 — STAFF ONLY"),
        };
        text.AddRange(HullShudder.LinesFor(HullShudder.Setting.HeadOfficeVacuum, HullShudder.HavenRoom.Concourse));
        foreach (string line in text)
        {
            foreach (string word in forbidden)
            {
                Assert.DoesNotContain(word, line, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── R1-F26 / R1-F40 · the dead hull and the regolith chill ────────────────────────────────────────

    [Fact]
    public void ADeadHullHasItsOwnShudderAndChillPools()
    {
        Assert.Equal(HullShudder.Setting.DeadHull, HullShudder.SettingOutside(
            groundHoldsPressure: false, onRegolith: false, deepSite: true, haven: false, onADeadHull: true));
        // …and the ground's own air still wins outright.
        Assert.Equal(HullShudder.Setting.Pressurised, HullShudder.SettingOutside(
            groundHoldsPressure: true, onRegolith: false, deepSite: true, haven: false, onADeadHull: true));

        IReadOnlyList<string> pool = HullShudder.LinesFor(HullShudder.Setting.DeadHull, HullShudder.HavenRoom.Concourse);
        IReadOnlyList<string> chill = HullShudder.ChillLinesFor(HullShudder.Setting.DeadHull);
        Assert.Equal(3, pool.Count);
        Assert.Equal(2, chill.Count);
        Assert.All(pool.Concat(chill), l => Assert.False(string.IsNullOrWhiteSpace(l)));
        Assert.Equal(pool.Count, pool.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(pool.Intersect(HullShudder.LinesFor(HullShudder.Setting.DeepSite, HullShudder.HavenRoom.Concourse)));
        Assert.StartsWith("The hull flexes around you with a groan", pool[0], StringComparison.Ordinal);
        Assert.StartsWith("The hull groans and the groan goes on past where a groan ends.", chill[0], StringComparison.Ordinal);

        // The dead hull keeps the vacuum label ("a cold breath through the hull").
        Assert.Equal("a cold breath through the hull", HullShudder.ChillNerveLabel(HullShudder.Setting.DeadHull));
    }

    [Fact]
    public void TheRegolithChillIsNotTheCrowdedHullsChill()
    {
        IReadOnlyList<string> regolith = HullShudder.ChillLinesFor(HullShudder.Setting.Regolith);
        IReadOnlyList<string> crowded = HullShudder.ChillLinesFor(HullShudder.Setting.DeepSite);
        Assert.Equal(3, regolith.Count);
        Assert.Equal(regolith.Count, regolith.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(regolith.Intersect(crowded));
        Assert.Empty(regolith.Intersect(HullShudder.ChillLinesFor(HullShudder.Setting.Pressurised)));
        Assert.All(regolith, l => Assert.DoesNotContain("heads", l, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("a cold that came up through the ground",
            HullShudder.ChillNerveLabel(HullShudder.Setting.Regolith));

        // The seeded draw reads the pool of the setting it is given.
        for (int i = 0; i < 24; i++)
        {
            Assert.Contains(HullShudder.ChillLine(HullShudder.Setting.Regolith, 77UL, i), regolith);
            Assert.Contains(HullShudder.ChillLine(HullShudder.Setting.DeadHull, 77UL, i),
                HullShudder.ChillLinesFor(HullShudder.Setting.DeadHull));
        }
    }

    /// <summary>Every new pool, swept: a line no pool owns is a line nobody hears, and the new pools may not
    /// name a crowd on a floor nobody else is on.</summary>
    [Fact]
    public void TheNewPoolsNameNoCrowd()
    {
        var all = HullShudder.LinesFor(HullShudder.Setting.DeadHull, HullShudder.HavenRoom.Concourse)
            .Concat(HullShudder.ChillLinesFor(HullShudder.Setting.DeadHull))
            .Concat(HullShudder.ChillLinesFor(HullShudder.Setting.Regolith))
            .Concat(HullShudder.LinesFor(HullShudder.Setting.HeadOfficeVacuum, HullShudder.HavenRoom.Concourse));
        foreach (string line in all)
        {
            Assert.DoesNotContain("as one", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("in unison", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("heads come up", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ── R1-F23 · an impact into a haven ───────────────────────────────────────────────────────────────

    [Fact]
    public void AnImpactIntoAStationReadsHullOnHull()
    {
        string[] bodies = ["Ringside Exchange", "Selene Gate"];
        for (ulong seed = 0; seed < 6; seed++)
        {
            foreach (string body in bodies)
            {
                string haven = DeathNarration.Line(DeathCause.Impact, DeathPlace.OwnShip, seed, body, intoAStation: true);
                string ground = DeathNarration.Line(DeathCause.Impact, DeathPlace.OwnShip, seed, body);
                Assert.Contains(body, haven, StringComparison.Ordinal);
                Assert.NotEqual(ground, haven);
                Assert.DoesNotContain("periapsis", haven, StringComparison.OrdinalIgnoreCase);
            }
        }
        Assert.Equal(
            "You put the ship into Ringside Exchange at speed — hull on hull, no ground anywhere in it, and the station's plating did not care which of you was moving.",
            DeathNarration.Line(DeathCause.Impact, DeathPlace.OwnShip, 0UL, "Ringside Exchange", intoAStation: true));
        Assert.Equal(
            "The hull met Ringside Exchange's ring at speed. No atmosphere to catch you and no rock to blame — a berth you were meant to slow for, taken without slowing.",
            DeathNarration.Line(DeathCause.Impact, DeathPlace.OwnShip, 1UL, "Ringside Exchange", intoAStation: true));
        Assert.Equal(
            "You flew into Ringside Exchange. The clamps were waiting; the ship arrived before the speed did. Somebody's berth will need re-plating, and your log ends there.",
            DeathNarration.Line(DeathCause.Impact, DeathPlace.OwnShip, 2UL, "Ringside Exchange", intoAStation: true));
    }

    [Fact]
    public void TheHeadlineForAStationImpactIsPlacedAndEveryOtherIsUntouched()
    {
        Assert.Equal("WHAT HAPPENED — you hit the station", DeathNarration.Headline(DeathCause.Impact, true));
        Assert.Equal("WHAT HAPPENED — you hit the ground", DeathNarration.Headline(DeathCause.Impact, false));
        Assert.Equal("WHAT HAPPENED — you hit the ground", DeathNarration.Headline(DeathCause.Impact));
        Assert.Equal(DeathNarration.Headline(DeathCause.Collector), DeathNarration.Headline(DeathCause.Collector, true));
        // CauseWord derives from the unplaced headline, unchanged.
        Assert.Equal("You hit the ground", DeathNarration.CauseWord(DeathCause.Impact));
    }

    // ── R1-F69 · the tank is getting low, underground ─────────────────────────────────────────────────

    [Fact]
    public void TheLowTankCardHasAnUndergroundFaceAndTheSurfaceKeepsItsOwn()
    {
        AirCard.Face surface = AirCard.For(underground: false);
        Assert.Equal(AirCard.Head, surface.Head);
        Assert.Same(AirCard.Beats, surface.Beats);
        Assert.Equal(AirCard.Dismiss, surface.Dismiss);
        Assert.Equal(AirCard.ArtFile, surface.ArtFile);

        AirCard.Face below = AirCard.For(underground: true, "B1", 2);
        Assert.Equal(
            "Last third of the tank, and nothing on this floor is breathing with you. The air is 2 floors up — B1, the top of this shaft band — or in the PRESSURE REFUGE the plan marks on this floor, if it still holds. Start for one of them now.",
            below.Head);
        Assert.Contains("1 floor up —", AirCard.HeadUnderground("B1", 1), StringComparison.Ordinal);
        Assert.Equal("Noted. Heading for air.", below.Dismiss);
        Assert.Equal(3, below.Beats.Count);
        Assert.Equal(AirCard.Beats[0], below.Beats[0]);
        Assert.Equal(AirCard.Beats[2], below.Beats[2]);
        Assert.Equal(
            "The walk back costs the same air as the walk out, and the car does not refill a tank. The top floor of every shaft band holds pressure; nothing under it does. A PRESSURE REFUGE on a dead floor will put two thirds back in and stop there — whether it still holds is not on the plan.",
            below.Beats[1]);
        Assert.NotEqual(AirCard.Beats[1], below.Beats[1]);
        // The underground art is generated later; until then it is the existing frame.
        Assert.Equal(AirCard.ArtFile, below.ArtFile);
    }

    // ── U03-F07 · the gig rock's boarding card ────────────────────────────────────────────────────────

    [Fact]
    public void TheGigRocksBoardingBlurbAndRowAreTheJobsOwn()
    {
        var type = new RockType(RockComposition.MType);
        string blurb = DeflectionGig.BoardingBlurb("Hull Breaker", type, "Ringside Exchange");
        Assert.StartsWith(
            "Hull Breaker, M-type metallic, inbound on Ringside Exchange. One marked DRILL POINT on open ground: bore the charge to depth, then fire it before T-0. Nothing to bury out here and nothing to buy — the rock is the job, and the clock is the rock's. ",
            blurb, StringComparison.Ordinal);
        Assert.EndsWith(type.BriefLine, blurb, StringComparison.Ordinal);

        Assert.Equal(
            "⏳ T−6:00 to Ringside Exchange · shuttle ETA 12m — the rock does not wait for a window",
            DeflectionGig.BoardRow("6:00", "Ringside Exchange", "12m"));
    }
}

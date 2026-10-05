using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #653 · THE STATION'S PROSE, HELD TO THE CANON BYTE FOR BYTE (Fable, issue #653: "CANON: slice 1's lines" and the
/// two addenda), typed from the issue — full strings, never a prefix and a suffix, because a prefix guard lets the
/// middle drift.
/// </summary>
public class StationCanonTests
{
    [Fact]
    public void EveryLineIsTheCanonVerbatim()
    {
        Assert.Equal(
            "Your lamp is the only thing with an opinion. The station keeps its own counsel: air flat, decks true, every door exactly as somebody left it.",
            StationAboard.FirstStandingLine);
        Assert.Equal(
            "A dead station, tubes severed, books balanced. Nobody home and nothing missing — which is two strange things, not one.",
            StationAboard.FieldBookLine);
        Assert.Equal(
            "The lock is a standard pattern, forty years polite. It will open for patience, and patience is the one thing aboard in quantity.",
            StationAboard.LockLine);
        Assert.Equal(
            "The face comes away clean. You are now somebody who cuts into stations. The station doesn't mind. That's the part you file.",
            StationAboard.CutFaceLine);
        Assert.Equal("HUB — TRANSFERS & TALLY", StationAboard.PlateOf(StationWreck.ModuleId.Hub));
        Assert.Equal("HABITAT — 40 BERTHS, KEEP IT DOWN", StationAboard.PlateOf(StationWreck.ModuleId.Habitat));
        Assert.Equal("FOUNDRY — EAR PROTECTION PAST THIS LINE", StationAboard.PlateOf(StationWreck.ModuleId.Foundry));
        Assert.Equal("DOCKING — DECLARE BEFORE YOU BERTH", StationAboard.PlateOf(StationWreck.ModuleId.Docking));
        Assert.Equal("REACTOR — TWO-MAN RULE, NO EXCEPTIONS", StationAboard.PlateOf(StationWreck.ModuleId.Reactor));
        Assert.Equal("Ledger Point", StationAboard.DevStationName);
        Assert.Equal("Ledger Point — dark these forty years, and still nothing owing.", StationAboard.BoardBlurb);
    }

    [Fact]
    public void AllProseIsTheWholeSetAndNothingInItNamesACauseOrAnyoneRemaining()
    {
        IReadOnlyList<string> prose = StationAboard.AllProse();
        Assert.Equal(11, prose.Count);   // four lines, five plates, the name, the blurb
        Assert.Equal(prose.Count, prose.Distinct().Count());

        // §13.8 for dead infrastructure: no cause for the severing, no statement of who is aboard.
        foreach (string banned in new[] { "because", "died", "killed", "survivor", "alive", "corpse", "murder", "reever", "old one", "monolith" })
        {
            foreach (string line in prose)
            {
                Assert.DoesNotContain(banned, line, StringComparison.OrdinalIgnoreCase);
            }
        }

        // …and the name is never the placeholder it replaced.
        foreach (string line in prose)
        {
            Assert.DoesNotContain("Dead station", line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheStationsSuffocationPoolIsTheWreckPoolsFirstAndThirdLinesWithTheCanonLineBetween()
    {
        string[] pool = DeathNarration.SuffocationLinesAboardAStation;
        Assert.Equal(3, pool.Length);
        Assert.Equal(
            "The tank went dry inside {body}, in vacuum she has held for years. Her air went out a long time before yours did.", pool[0]);
        Assert.Equal("The station does not notice. Her books closed years ago, and they do not reopen for breath.", pool[1]);
        Assert.Equal(
            "On {body} the gauge reached nothing between one bulkhead and the next. She had nothing to give you.", pool[2]);

        // The wreck's own pool keeps its outer lines (the station's are verbatim copies, not edits) — swept over seeds.
        for (ulong seed = 0; seed < 30; seed++)
        {
            string line = DeathNarration.Line(DeathCause.Suffocated, DeathPlace.Station, seed, "Ledger Point");
            Assert.Equal(pool[(int)(seed % 3)].Replace("{body}", "Ledger Point"), line);
        }

        // The card tail and the art stand: a station reads as a hull for both, and for who can die there.
        Assert.Equal(DeathNarration.Tail(DeathCause.Suffocated, DeathPlace.Derelict),
            DeathNarration.Tail(DeathCause.Suffocated, DeathPlace.Station));
        Assert.Equal(DeathNarration.ArtFile(DeathCause.Suffocated, DeathPlace.Derelict),
            DeathNarration.ArtFile(DeathCause.Suffocated, DeathPlace.Station));
        foreach (DeathCause c in Enum.GetValues<DeathCause>())
        {
            Assert.Equal(DeathNarration.CanHappen(c, DeathPlace.Derelict), DeathNarration.CanHappen(c, DeathPlace.Station));
        }
    }

    [Fact]
    public void AStationDeathThatIsNotSuffocationReadsAsAboardAHull()
    {
        for (ulong seed = 0; seed < 12; seed++)
        {
            Assert.Equal(
                DeathNarration.Line(DeathCause.Reevers, DeathPlace.Derelict, seed, "X"),
                DeathNarration.Line(DeathCause.Reevers, DeathPlace.Station, seed, "X"));
        }
    }

    [Fact]
    public void ACutTagRoundTripsThroughTheRegisterAndOnlyForItsOwnStation()
    {
        const string id = "dev-station-6";
        var register = new HashSet<string> { "unrelated:tag", StationAboard.CutTag(id, StationWreck.ModuleId.Reactor) };

        Assert.Equal([StationWreck.ModuleId.Reactor], StationAboard.CutsIn(register, id));
        Assert.Empty(StationAboard.CutsIn(register, "another-station"));
        Assert.Empty(StationAboard.CutsIn(new HashSet<string>(), id));

        register.Add(StationAboard.CutTag(id, StationWreck.ModuleId.Habitat));
        Assert.Equal(2, StationAboard.CutsIn(register, id).Count);

        // Distinct tags per (station, module): no two collide.
        var tags = StationWreck.Modules.SelectMany(m => new[] { "a", "b" }.Select(s => StationAboard.CutTag(s, m.Id))).ToList();
        Assert.Equal(tags.Count, tags.Distinct().Count());
    }
}

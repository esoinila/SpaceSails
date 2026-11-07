using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// U08-F11 / U04-F11 / U10-F02 (Fable ruling 2026-11-07, on the owner's delegation; the owner may veto) · THE ROUND
/// WALKS VACUUM FLOORS, SUITED. Everything the round says is spelled by the floor's pressure. The owner's four lines
/// stay verbatim where the floor breathes; the vacuum set is said on a floor that does not, and each set is never
/// said on the other.
/// </summary>
public sealed class TheRoundIsSuitedInVacuumTests
{
    private const string Body = "luna";
    private const string Plate = "◈ A CONTRACT GUARD, WALKING THE ROUND";

    private static int Air => UndergroundComplex.TopPressurisedFloor(Body)!.Value;

    /// <summary>A patrolled floor that does not breathe and on which the site's own general pass works - so both
    /// arms (refused and satisfied) can be read on it.</summary>
    private static int Vacuum => Enumerable.Range(1, 14).Select(i => -i)
        .First(l => PatrolBeat.IsPatrolled(Body, l) && !UndergroundComplex.HoldsPressure(Body, l)
                    && PatrolBeat.TheGuardReads(Body, l, 0L, Plate, PatrolBeat.Badge(Body), false).Satisfied);

    private static IEnumerable<PatrolBeat.Read> Reads(int level) => new[]
    {
        PatrolBeat.TheGuardReads(Body, level, 0L, Plate, null, false),                      // nothing in the hand
        PatrolBeat.TheGuardReads(Body, level, 0L, Plate, PatrolBeat.Badge(Body), false),    // the site's own pass
        PatrolBeat.TheGuardReads(Body, level, 0L, Plate, PatrolBeat.Badge("europa"), false), // somebody else's site
    };

    private static string Everything(IEnumerable<PatrolBeat.Read> reads) =>
        string.Join("\n----\n", reads.Select(r => r.Card + "\n" + r.Told));

    [Fact]
    public void ThePremise_TheTwoFloorsDifferInAir()
    {
        Assert.True(UndergroundComplex.HoldsPressure(Body, Air));
        Assert.False(UndergroundComplex.HoldsPressure(Body, Vacuum));
    }

    [Fact]
    public void OnAFloorThatBreathesTheOwnersLinesAreSaidVerbatim()
    {
        string said = Everything(Reads(Air));
        foreach (string owners in PatrolBeat.AuthoredLines.Skip(1).Take(3))   // challenge, satisfied, escort
        {
            Assert.Contains(owners, said, StringComparison.Ordinal);
        }

        Assert.Equal(PatrolBeat.ChallengeCard(Plate), PatrolBeat.TheGuardReads(Body, Air, 0L, Plate, null, false).Card);
        Assert.Equal(PatrolBeat.EscortLine, PatrolBeat.TheGuardReads(Body, Air, 0L, Plate, null, false).Consequence);
        Assert.Equal(PatrolBeat.SatisfiedLine, PatrolBeat.TheGuardReads(Body, Air, 0L, Plate, PatrolBeat.Badge(Body), false).Line);
    }

    [Fact]
    public void OnAVacuumFloorTheSuitedSetIsSaid()
    {
        PatrolBeat.Read refused = PatrolBeat.TheGuardReads(Body, Vacuum, 0L, Plate, null, false);
        Assert.Equal(PatrolBeat.VacuumChallengeCard(Plate), refused.Card);
        Assert.Equal(PatrolBeat.VacuumEscortLine, refused.Consequence);

        PatrolBeat.Read passed = PatrolBeat.TheGuardReads(Body, Vacuum, 0L, Plate, PatrolBeat.Badge(Body), false);
        Assert.True(passed.Satisfied, "the premise: the site's own pass works on the vacuum floor too.");
        Assert.Equal(PatrolBeat.VacuumSatisfiedLine, passed.Line);
    }

    [Fact]
    public void EachSetIsNeverSaidOnTheOtherFloor()
    {
        string onAir = Everything(Reads(Air))
            + PatrolBeat.HailLineFor(true) + PatrolBeat.PumpsLineFor(true)
            + GuardStop.TheWork(Encounter.Band.NoAnd, Plate, true).Read.Told
            + GuardStop.TheWork(Encounter.Band.Yes, Plate, true).Read.Card;
        string onVacuum = Everything(Reads(Vacuum))
            + PatrolBeat.HailLineFor(false) + PatrolBeat.PumpsLineFor(false)
            + GuardStop.TheWork(Encounter.Band.NoAnd, Plate, false).Read.Told
            + GuardStop.TheWork(Encounter.Band.Yes, Plate, false).Read.Card;

        foreach (string suited in PatrolBeat.VacuumLines)
        {
            Assert.DoesNotContain(suited, onAir, StringComparison.Ordinal);
        }

        foreach (string owners in new[]
                 {
                     PatrolBeat.HailLine, PatrolBeat.PumpsLine, PatrolBeat.SatisfiedLine, PatrolBeat.EscortLine,
                     PatrolBeat.ChallengeCard(Plate),
                 })
        {
            Assert.DoesNotContain(owners, onVacuum, StringComparison.Ordinal);
        }

        // …and the vacuum card runs caption-only: no pressurised painting is hung in a suited corridor.
        Assert.Equal(PatrolBeat.ChallengeArtUrl, PatrolBeat.ChallengeArtFor(true));
        Assert.Null(PatrolBeat.ChallengeArtFor(false));
    }

    [Fact]
    public void TheVacuumStringsAreFablesVerbatim()
    {
        Assert.Equal(
            "👮 A lamp on the far wall that is not yours, and under your boots the deck carrying a tread you cannot hear. " +
            "It turns toward you. It is in no hurry at all.", PatrolBeat.VacuumHailLine);
        Assert.Equal(
            "👮 \"Lobby's the only floor on this band that breathes,\" he says, to the suit band and nobody in particular. " +
            "\"Been that way since they pulled the pumps.\"", PatrolBeat.VacuumPumpsLine);
        Assert.StartsWith("👮 He reads it through two faceplates", PatrolBeat.VacuumSatisfiedLine, StringComparison.Ordinal);
        Assert.StartsWith("👮 \"No? Then you walk ahead of me to the lift, and we don't make it a thing.\" He walks you back",
            PatrolBeat.VacuumEscortLine, StringComparison.Ordinal);
        string card = PatrolBeat.VacuumChallengeCard("{plate}");
        Assert.StartsWith("{plate} — and his lamp finds you before yours finds him.", card, StringComparison.Ordinal);
        Assert.Contains("\n\n\"Hold there. Floor's restricted. Show me something.\" It comes over the suit band", card, StringComparison.Ordinal);
    }

    [Fact]
    public void TheOwnersFourAreUnchanged() =>
        Assert.Equal(
            [
                "Boots on shotcrete, out of step with yours.",
                "Hold there. Floor's restricted. Show me something.",
                "Right. Keep to the lit side.",
                "No? Then you walk ahead of me to the lift, and we don't make it a thing.",
                "He reads the pass twice. The second time he is reading your face.",
            ],
            PatrolBeat.AuthoredLines);
}

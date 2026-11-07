using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: the round on a floor that does not breathe (part of PatrolBeat).
public static partial class PatrolBeat
{
    // ── U08-F11 / U04-F11 / U10-F02 · THE ROUND WALKS VACUUM FLOORS, SUITED ───────────────────────────────
    //
    // Fable's ruling of 2026-11-07, on the owner's delegation (the owner may veto): yes, the round walks the vacuum
    // floors, in a suit, and everything it SAYS is spelled by the floor's pressure — asked of
    // UndergroundComplex.HoldsPressure and nowhere else. The owner's four lines (AuthoredLines) stay verbatim on the
    // pressurised floors; vacuum gets the set below. The vacuum card runs caption-only until a suited painting exists.

    /// <summary>The hail on a floor with no air: a lamp, and a tread carried through the deck. Said instead of
    /// <see cref="HailLine"/> there.</summary>
    public const string VacuumHailLine =
        "👮 A lamp on the far wall that is not yours, and under your boots the deck carrying a tread you cannot hear. " +
        "It turns toward you. It is in no hurry at all.";

    /// <summary>The stop, on a floor with no air. Said instead of <see cref="ChallengeCard(string)"/> there.</summary>
    public static string VacuumChallengeCard(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        return $"{plate} — and his lamp finds you before yours finds him. No shout, because there is nothing down " +
               "here to carry one: a helmet light held steady on your chest, and a glove coming up into it, palm open.\n\n" +
               "\"Hold there. Floor's restricted. Show me something.\" It comes over the suit band, flat and close, the way " +
               "a voice sounds with no room between it and your ear. The glove is out for the thing everybody who works " +
               "this corridor carries. Nothing about it is urgent. That is the part worth being frightened of — he has " +
               "done this a hundred times, in a suit, in the dark, and it has come to nothing a hundred times.";
    }

    /// <summary>The pass works, on a floor with no air. Said instead of <see cref="SatisfiedLine"/> there.</summary>
    public const string VacuumSatisfiedLine =
        "👮 He reads it through two faceplates — the face, the site code, the tier — a moment longer than he needs to, " +
        "and hands it back glove to glove. \"Right. Keep to the lit side.\" The lamp swings off your chest and onto " +
        "the strip of floor it has been keeping, and the round picks up where it left off.";

    /// <summary>The walk back to the car, on a floor with no air. Said instead of <see cref="EscortLine"/> there.</summary>
    public const string VacuumEscortLine =
        "👮 \"No? Then you walk ahead of me to the lift, and we don't make it a thing.\" He walks you back to the car " +
        "at your pace, half a step behind your shoulder, his lamp on your back and the suit band open between you on " +
        "nothing at all. He presses the button for you and stands there while the doors shut. Nothing is taken, " +
        "nobody is called, and somewhere a line goes into a book with the time on it.";

    /// <summary>The small talk on the walk, on a floor with no air. Said instead of <see cref="PumpsLine"/>
    /// there.</summary>
    public const string VacuumPumpsLine =
        "👮 \"Lobby's the only floor on this band that breathes,\" he says, to the suit band and nobody in particular. " +
        "\"Been that way since they pulled the pumps.\"";

    /// <summary>The card's painting: the round's picture on a floor that breathes, and NONE on a floor that does not —
    /// the vacuum card runs caption-only until a suited painting exists.</summary>
    public static string? ChallengeArtFor(bool holdsPressure) => holdsPressure ? ChallengeArtUrl : null;

    /// <summary>The stop's body, spelled by the floor's air.</summary>
    public static string ChallengeCard(string plate, bool holdsPressure) =>
        holdsPressure ? ChallengeCard(plate) : VacuumChallengeCard(plate);

    /// <summary>The hail, spelled by the floor's air.</summary>
    public static string HailLineFor(bool holdsPressure) => holdsPressure ? HailLine : VacuumHailLine;

    /// <summary>The pass that works, spelled by the floor's air.</summary>
    public static string SatisfiedLineFor(bool holdsPressure) => holdsPressure ? SatisfiedLine : VacuumSatisfiedLine;

    /// <summary>The walk back to the car, spelled by the floor's air.</summary>
    public static string EscortLineFor(bool holdsPressure) => holdsPressure ? EscortLine : VacuumEscortLine;

    /// <summary>The small talk on the walk, spelled by the floor's air.</summary>
    public static string PumpsLineFor(bool holdsPressure) => holdsPressure ? PumpsLine : VacuumPumpsLine;

    /// <summary>Every sentence the vacuum spelling adds, for the canon grep and the "never on the other floor" guard.
    /// <see cref="AuthoredLines"/> is unchanged; this is its suited sibling.</summary>
    public static IReadOnlyList<string> VacuumLines =>
    [
        VacuumHailLine,
        VacuumChallengeCard("◈ A PLATE"),
        VacuumSatisfiedLine,
        VacuumEscortLine,
        VacuumPumpsLine,
    ];
}

using System;
using System.Collections.Generic;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #1332 slice C · <b>THE PRESERVATION OFFICE</b> — the first of the owner's side offices: a door on Ringside
/// Exchange's hotel level (MEMBERS' ROOMS) with the cost centre on it, a clerk who keeps hours, and one paper on
/// the desk.
///
/// <para>Four papers already say <i>Charged to Preservation</i> — #1097's rail, rota and pour, and #1334's
/// editorial services. A cost centre has an office, and an office has a door. The office never does anything to
/// the captain: it is a place the money trail leads, and the clerk's hours are a window a gumshoe can time (the
/// observation walk's discipline, #1199).</para>
///
/// <para><b>What is pure here</b>: every word (Fable canon, 2026-09-30, verbatim — crews do not write canon), which
/// haven and which cabin, the clerk's hours as arithmetic of (run seed, watch) with no <see cref="System.Random"/>,
/// the register's three tags, the paper's id and what the book files it under, and the dev latch. The door, the
/// desk and the walker are the client's.</para>
///
/// <para><b>Laws.</b> The enforcer is an OFFICE and never a name (#1074): the clerk never speaks and is plated by
/// what he does. Kosh: nothing announces the ajar watch; the clerk is a walker and not a card; the paper is a paper.
/// Scully: every line is true as prudence reads it — an office that keeps appointments it does not make is a
/// busy office.</para>
/// </summary>
public static class PreservationOffice
{
    // ── WHERE ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The one haven the office is at: Ringside Exchange, where money lives. Nowhere else has a
    /// door like it (out of scope: a second office).</summary>
    public const string HavenId = "ringside-exchange";

    /// <summary>Which of the hotel level's five cabins became the office — one-based, the way a door number is.
    /// The middle of the row; the other four stay <c>CABIN n</c>.</summary>
    public const int Cabin = 3;

    // ── THE WORDS — Fable canon, 2026-09-30 (#1332 slice C), verbatim. ─────────────────────────────────────

    /// <summary>The door's plate. It replaces <c>CABIN 3</c> on that one leaf and on nothing else; the floor
    /// still carries its one label.</summary>
    public const string DoorPlate = "PRESERVATION · BY APPOINTMENT";

    /// <summary>[E] at the door while it is shut — told on every press, as the Hive's locked leaves are.</summary>
    public const string ShutLine = "Appointments are made by the office. The office does not make appointments.";

    /// <summary>The field book, the first time the captain reads the plate — 📍, once.</summary>
    public const string PlateReadLine = "A door with the cost centre on it. Somebody pays the rent on Preservation.";

    /// <summary>The book's glyph for <see cref="PlateReadLine"/>.</summary>
    public const string PlateReadGlyph = "📍";

    /// <summary>The walker's plate. He is plated by what he does; nothing anywhere names him.</summary>
    public const string ClerkPlate = "Clerk";

    /// <summary>Entering the office while it stands ajar — told once per run.</summary>
    public const string WarmStillLine =
        "Warm still. A chair pushed back the way a chair is pushed back by somebody who means to return.";

    /// <summary>The paper on the desk, as the satchel keeps it.</summary>
    public const string SheetId = "preservation-disbursements";

    /// <summary>…its title, as papers read away from their room.</summary>
    public const string SheetTitle = "A disbursement sheet, quarter to date";

    /// <summary>…and its document. No amount, no signature, no department but the cost centre — #1074 beat 3's
    /// idiom. The four lines are the four papers the captain may already hold, counted by the office that paid
    /// for them, and nothing on the sheet says so.</summary>
    public const string SheetDocument =
        "Perimeter rail. Site watch. Structural remediation. Editorial services. Charged to Preservation.";

    /// <summary>The book's glyph for the sheet: the building's own paper glyph, the receipt's (#1334).</summary>
    public const string SheetGlyph = "📋";

    /// <summary>Is this paper the disbursement sheet?</summary>
    public static bool IsTheSheet(string? paperId) => string.Equals(paperId, SheetId, StringComparison.Ordinal);

    /// <summary>The sheet, as the satchel carries it.</summary>
    public static Satchel.Item TheSheet => new(Satchel.Kind.Paper, SheetId);

    /// <summary>
    /// What the book files the plate and the sheet under: <i>the Authority</i> and the haven, #1074 beat 3's own
    /// door (<see cref="MoneyTrail.SubjectsFor"/>), so both stack under the same office as the rail, the rota, the
    /// pour and the receipt.
    /// </summary>
    /// <param name="havenName">The haven's name as the game prints it — Ringside Exchange.</param>
    public static string SubjectsFor(string? havenName) => MoneyTrail.SubjectsFor(havenName);

    // ── THE CLERK'S HOURS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>One watch in four the door stands ajar.</summary>
    public const int WatchesPerTurn = 4;

    /// <summary>The seed tag the offset is drawn on — its own stream off the run.</summary>
    public const string SeedTag = "preservation:clerk";

    /// <summary>Which watch of every four is the clerk's, seeded on the run: 0 to <see cref="WatchesPerTurn"/> − 1.
    /// The same run always answers the same, so a captain can LEARN it.</summary>
    /// <param name="runSeed">The run's own seed — the page's <c>WorldSeed</c>, the thread id folded.</param>
    public static int OffsetFor(ulong runSeed) =>
        (int)(DiceRule.Seed(runSeed, SeedTag) % WatchesPerTurn);

    /// <summary><b>IS THIS WATCH THE CLERK'S?</b> Pure arithmetic of (run seed, watch): the door stands ajar for
    /// the whole of it and is shut for the other three. Correct for any watch index, negative included.</summary>
    public static bool IsTheClerksWatch(ulong runSeed, long watch)
    {
        long r = (watch - OffsetFor(runSeed)) % WatchesPerTurn;
        return (r < 0 ? r + WatchesPerTurn : r) == 0;
    }

    /// <summary>When the clerk sets off from the door on his watch: the watch's first second. He walks to the car
    /// and rides up, and the office is empty for the rest of it.</summary>
    public static double SetsOffAt(long watch) => watch * PatronRota.WatchSeconds;

    /// <summary>
    /// <b>HOW FAR ALONG HIS WALK THE CLERK IS</b> at <paramref name="simTime"/>, as a fraction of a walk that takes
    /// <paramref name="walkSeconds"/> from <paramref name="setOffAt"/> — or null when he is not on it (before he
    /// set off, or once he has reached the car). The clock is the clock whether or not anybody is on the floor to
    /// see him.
    /// </summary>
    public static double? Along(double setOffAt, double walkSeconds, double simTime)
    {
        if (walkSeconds <= 0 || simTime < setOffAt || simTime >= setOffAt + walkSeconds)
        {
            return null;
        }

        return (simTime - setOffAt) / walkSeconds;
    }

    // ── WHAT THE REGISTER KEEPS ────────────────────────────────────────────────────────────────────────────
    //
    // Three facts, each a tag on the durable register of turned-over ground (the SET #711's quiet watches and
    // #794's chalk already write into), and never a page field. With none of them written, nothing is different.

    /// <summary>The captain has read the plate: the book's line has been filed.</summary>
    public const string PlateReadTag = "preservation:plate-read";

    /// <summary>The sheet has been taken: the desk is bare for the run.</summary>
    public const string SheetTakenTag = "preservation:sheet-taken";

    /// <summary>The captain has been inside while it stood ajar: the warm line has been told.</summary>
    public const string WarmToldTag = "preservation:warm-still";

    // ── THE DEV START ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How the dev latch forces the door.</summary>
    public enum Cheat
    {
        /// <summary>Nothing forced: the clerk's hours are the run's own.</summary>
        None,

        /// <summary><c>office=shut</c> — the door shut whatever the watch.</summary>
        Shut,

        /// <summary><c>office=open</c> — the door ajar and the clerk setting off, whatever the watch.</summary>
        Open,
    }

    /// <summary>How long after the dev boot docks the clerk steps out, on <c>office=open</c> — long enough that a
    /// tester sees the empty doorway first and the man come out of it second.</summary>
    public const double DevSetOffSeconds = 3.0;

    /// <summary>Read <c>office=shut|open</c> off an address — <see cref="GateGuard.CheatIn"/>'s way: a dev latch
    /// that writes no world at parse time and no field on the page.</summary>
    public static Cheat CheatIn(string? uri)
    {
        int q = uri?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (uri is null || q < 0)
        {
            return Cheat.None;
        }

        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (pair.StartsWith("office=", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair["office=".Length..]).ToLowerInvariant() switch
                {
                    "shut" or "0" => Cheat.Shut,
                    "open" or "1" or "ajar" => Cheat.Open,
                    _ => Cheat.None,
                };
            }
        }

        return Cheat.None;
    }

    /// <summary>Every player-facing string this slice publishes — the plate, the two told lines, the book line,
    /// the walker's plate and the sheet's title and document. Seven, and they are the seven the brief wrote.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return DoorPlate;
        yield return ShutLine;
        yield return PlateReadLine;
        yield return ClerkPlate;
        yield return WarmStillLine;
        yield return SheetTitle;
        yield return SheetDocument;
    }
}

using System;
using System.Collections.Generic;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #1332 slices C, D and E · <b>THE SIDE-OFFICE KIT</b> — one office on one haven's hotel level: a plate on one of the
/// five cabins, a door that stands ajar on a seeded watch, one paper on the desk, and nothing that acts on the
/// captain. Slice C (the Preservation office) minted the pattern; D (the forwarding desk) and E (the adjuster's room)
/// are the same pattern with other words, and this class is what the pattern is data of.
///
/// <para><b>Every word is a canon constant of the office's own class</b> (<see cref="PreservationOffice"/>,
/// <see cref="ForwardingDesk"/>, <see cref="AdjustersRoom"/>) — Fable's, verbatim; this class invents none. What it
/// owns is the shape: which haven, which cabin, how often the door stands ajar and on which seeded stream, which
/// register tags remember what the captain has done, and how the book files the paper.</para>
///
/// <para>The hours are pure arithmetic of (run seed, watch) with no <see cref="Random"/>: the same run always
/// answers the same, so a captain can LEARN it. Each office draws its offset off its own seed tag, so the three
/// doors keep three clocks.</para>
/// </summary>
public sealed class SideOffice
{
    /// <summary>The office's short id (<c>preservation</c>, <c>forwarding</c>, <c>adjuster</c>).</summary>
    public required string Id { get; init; }

    /// <summary>The one haven the office is at.</summary>
    public required string HavenId { get; init; }

    /// <summary>Which of the hotel level's five cabins it took — one-based, the way a door number is.</summary>
    public required int Cabin { get; init; }

    /// <summary>The door's plate. It replaces <c>CABIN n</c> on that one leaf and on nothing else.</summary>
    public required string DoorPlate { get; init; }

    /// <summary>[E] at the door while it is shut — told on every press.</summary>
    public required string ShutLine { get; init; }

    /// <summary>The field book, the first time the captain reads the plate — once.</summary>
    public required string PlateReadLine { get; init; }

    /// <summary>The line told once per run on the first step inside while the door stands ajar.</summary>
    public required string AjarLine { get; init; }

    /// <summary>The paper on the desk, as the satchel keeps it.</summary>
    public required string SheetId { get; init; }

    /// <summary>…its title, as papers read away from their room.</summary>
    public required string SheetTitle { get; init; }

    /// <summary>…and its document.</summary>
    public required string SheetDocument { get; init; }

    /// <summary>One watch in this many the door stands ajar.</summary>
    public required int WatchesPerTurn { get; init; }

    /// <summary>The seed tag the offset is drawn on — the office's own stream off the run.</summary>
    public required string SeedTag { get; init; }

    /// <summary>Register tag: the captain has read the plate (the book's line has been filed).</summary>
    public required string PlateReadTag { get; init; }

    /// <summary>Register tag: the paper has been taken (the desk is bare for the run).</summary>
    public required string SheetTakenTag { get; init; }

    /// <summary>Register tag: the ajar line has been told.</summary>
    public required string AjarToldTag { get; init; }

    /// <summary>Is the ajar line owed once EVER (a tag on the vault-persisted register, surviving a reload — slice C's
    /// shipped cadence for the warm chair), or once per run of the page (<see cref="ToldOnce"/> — D's and E's, new
    /// lines whose "entering once" is read as once per run)?</summary>
    public required bool AjarToldPersists { get; init; }

    /// <summary>Does taking the paper file its document in the book (<c>true</c>), or is it a form that is not
    /// evidence of anything (<c>false</c> — the adjuster's blank claim form)?</summary>
    public required bool FilesTheSheet { get; init; }

    /// <summary>Does a walker keep this office's hours (slice C's clerk)? D and E are empty rooms.</summary>
    public required bool HasAClerk { get; init; }

    /// <summary>What the book files the plate's line and the paper under, given the haven's printed name.</summary>
    public required Func<string?, string> Subjects { get; init; }

    /// <summary>Is this paper this office's?</summary>
    public bool IsTheSheet(string? paperId) => string.Equals(paperId, SheetId, StringComparison.Ordinal);

    /// <summary>The paper, as the satchel carries it.</summary>
    public Satchel.Item TheSheet => new(Satchel.Kind.Paper, SheetId);

    /// <summary>Which watch of every <see cref="WatchesPerTurn"/> is the door's, seeded on the run.</summary>
    public int OffsetFor(ulong runSeed) => SideOffices.OffsetOf(runSeed, SeedTag, WatchesPerTurn);

    /// <summary><b>IS THIS WATCH THE ONE THE DOOR STANDS AJAR?</b> Pure arithmetic of (run seed, watch); correct for
    /// any watch index, negative included.</summary>
    public bool IsAjarOn(ulong runSeed, long watch) => SideOffices.AjarOn(runSeed, SeedTag, WatchesPerTurn, watch);

    /// <summary>Every player-facing string the office publishes (the walker's plate, where there is a walker, is
    /// <see cref="PreservationOffice"/>'s own).</summary>
    public IEnumerable<string> Prose()
    {
        yield return DoorPlate;
        yield return ShutLine;
        yield return PlateReadLine;
        yield return AjarLine;
        yield return SheetTitle;
        yield return SheetDocument;
    }
}

/// <summary>
/// #1332 · The catalogue of side offices and the one clock they share the arithmetic of. An office is a haven's
/// data: <see cref="At"/> answers null for every haven that has none, and nothing else about that haven moves.
/// </summary>
public static class SideOffices
{
    /// <summary>The offsets are drawn on <see cref="DiceRule"/> and nothing else.</summary>
    internal static int OffsetOf(ulong runSeed, string seedTag, int watchesPerTurn) =>
        (int)(DiceRule.Seed(runSeed, seedTag) % (ulong)watchesPerTurn);

    /// <summary>One watch in <paramref name="watchesPerTurn"/>, offset seeded on the run.</summary>
    internal static bool AjarOn(ulong runSeed, string seedTag, int watchesPerTurn, long watch)
    {
        long r = (watch - OffsetOf(runSeed, seedTag, watchesPerTurn)) % watchesPerTurn;
        return (r < 0 ? r + watchesPerTurn : r) == 0;
    }

    /// <summary>The Preservation office (slice C), as the kit's data — every field is
    /// <see cref="PreservationOffice"/>'s own constant.</summary>
    public static SideOffice Preservation { get; } = new()
    {
        Id = "preservation",
        HavenId = PreservationOffice.HavenId,
        Cabin = PreservationOffice.Cabin,
        DoorPlate = PreservationOffice.DoorPlate,
        ShutLine = PreservationOffice.ShutLine,
        PlateReadLine = PreservationOffice.PlateReadLine,
        AjarLine = PreservationOffice.WarmStillLine,
        SheetId = PreservationOffice.SheetId,
        SheetTitle = PreservationOffice.SheetTitle,
        SheetDocument = PreservationOffice.SheetDocument,
        WatchesPerTurn = PreservationOffice.WatchesPerTurn,
        SeedTag = PreservationOffice.SeedTag,
        PlateReadTag = PreservationOffice.PlateReadTag,
        SheetTakenTag = PreservationOffice.SheetTakenTag,
        AjarToldTag = PreservationOffice.WarmToldTag,
        AjarToldPersists = true,
        FilesTheSheet = true,
        HasAClerk = true,
        Subjects = PreservationOffice.SubjectsFor,
    };

    /// <summary>The forwarding desk (slice D), Cinder Roost's cabin 2.</summary>
    public static SideOffice Forwarding => ForwardingDesk.Office;

    /// <summary>The adjuster's room (slice E), The Deep's cabin 4.</summary>
    public static SideOffice Adjuster => AdjustersRoom.Office;

    /// <summary>Every office there is, in slice order.</summary>
    public static IReadOnlyList<SideOffice> All { get; } = [Preservation, Forwarding, Adjuster];

    /// <summary>This haven's office, or null — every haven without one.</summary>
    public static SideOffice? At(string? havenId)
    {
        foreach (SideOffice o in All)
        {
            if (string.Equals(o.HavenId, havenId, StringComparison.Ordinal))
            {
                return o;
            }
        }

        return null;
    }

    /// <summary>The office whose paper this is, or null.</summary>
    public static SideOffice? ForSheet(string? paperId)
    {
        foreach (SideOffice o in All)
        {
            if (o.IsTheSheet(paperId))
            {
                return o;
            }
        }

        return null;
    }
}

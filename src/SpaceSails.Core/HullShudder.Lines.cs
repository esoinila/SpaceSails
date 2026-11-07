namespace SpaceSails.Core;

/// <summary>
/// #251 · WHICH LINE IS SAID — the pool for a setting and a room, the haven's per-room share of it, the
/// chill pools, and the seeded draw of one line.
///
/// <para>Split out of <c>HullShudder.cs</c> under #251 as a pure move: two runs of the base file, no member
/// renamed, re-scoped or re-ordered. Every line pool is a <c>static readonly</c> and stays in the opening
/// file, in its original order (#1163); only the members that read them moved.</para>
/// </summary>
public static partial class HullShudder
{
    /// <summary>The house-voice line pool for a <paramref name="setting"/> — exposed so a test can pin that
    /// every line is non-blank and the pool holds no duplicates. The chill pool
    /// (<see cref="ChillLine"/>) is separate; this is the ordinary "it was nothing" voice.</summary>
    /// <param name="room">#1248/#1261 · Which room of the haven the captain is standing in. It is REQUIRED
    /// rather than defaulted, for the reason <see cref="ChillLinesFor"/> takes the ground's own fact the same
    /// way: a caller that can decline to ask is a caller that will, and the whole bug here was a beat raised
    /// with no room in its hand. Only <see cref="Setting.Haven"/> reads it — the ship, the regolith, a deep
    /// site and pressurised ground have no rooms to be in or out of — and there it picks the room's own
    /// lines, which for <see cref="HavenRoom.TheObservationWalk"/> is NONE of them.</param>
    public static System.Collections.Generic.IReadOnlyList<string> LinesFor(Setting setting, HavenRoom room) =>
        setting switch
        {
            Setting.Haven => TheHavensLinesFor(room),
            Setting.Ship => ShipLines,
            Setting.Regolith => RegolithLines,
            Setting.Pressurised => PressurisedLines,
            _ => DeepSiteLines,
        };

    /// <summary>#1248/#1261 · The haven's lines for the room the captain is actually standing in — projected
    /// off <see cref="HavenLines"/> through the partition, so there is one copy of the words and one
    /// statement of which room each belongs to. The walk's share is empty, and an empty pool is a room with
    /// nothing to say rather than a room that was forgotten.</summary>
    private static System.Collections.Generic.IReadOnlyList<string> TheHavensLinesFor(HavenRoom room)
    {
        int[] mine = TheHavenIndicesFor(room);
        var said = new string[mine.Length];
        for (int i = 0; i < mine.Length; i++)
        {
            said[i] = HavenLines[mine[i]];
        }

        return said;
    }

    /// <summary>#1261 · The partition itself, by room — the one switch over it, so the pool, the guards and
    /// the covering law all read the same three shares.</summary>
    private static int[] TheHavenIndicesFor(HavenRoom room) => room switch
    {
        HavenRoom.Bar => HavenLinesTheBarOwns,
        HavenRoom.TheObservationWalk => HavenLinesTheObservationWalkOwns,
        HavenRoom.LowerConcourse => HavenLinesTheLowerConcourseOwns,
        _ => HavenLinesTheConcourseOwns,
    };

    /// <summary>#1248 · The whole haven pool, in its authored order — for the guards that hold the partition
    /// to covering it and the prose to not having moved.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> EveryHavenLine() => HavenLines;

    /// <summary>#1248 · Which of <see cref="EveryHavenLine"/> the bar owns, by index — exposed so the guard
    /// can check the partition against the FURNITURE the lines name rather than against a second opinion
    /// about it.</summary>
    public static System.Collections.Generic.IReadOnlyList<int> TheBarsOwnHavenLines() => HavenLinesTheBarOwns;

    /// <summary>#1248 · …and the concourse's. See <see cref="TheBarsOwnHavenLines"/>.</summary>
    public static System.Collections.Generic.IReadOnlyList<int> TheConcoursesOwnHavenLines() =>
        HavenLinesTheConcourseOwns;

    /// <summary>#1261 · …and the observation walk's, which is empty on purpose. See
    /// <see cref="HavenLinesTheObservationWalkOwns"/>.</summary>
    public static System.Collections.Generic.IReadOnlyList<int> TheWalksOwnHavenLines() =>
        HavenLinesTheObservationWalkOwns;

    /// <summary>#867 · The chill-line pool — exposed for the same non-blank / unique pinning as the ordinary
    /// pools, and it takes the ground's own fact rather than offering a default, so no caller can decline to
    /// ask it. <paramref name="groundHoldsPressure"/> is
    /// <see cref="UndergroundComplex.HoldsPressure"/> of the floor underfoot: on ground that breathes the
    /// chill has air to arrive through, and naming a hull there is the bug this parameter exists to make
    /// impossible.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> ChillLinesFor(bool groundHoldsPressure) =>
        groundHoldsPressure ? PressurisedChillLines : ChillLines;

    // ── Line selection. ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The ordinary house-voice line for a shudder — deterministically drawn from the
    /// <paramref name="setting"/>'s pool per (seed, index), so the same shudder always speaks the same words
    /// and consecutive shudders rotate the pool rather than repeating.</summary>
    /// <param name="room">#1248/#1261 · See <see cref="LinesFor"/>. Required, never defaulted.</param>
    /// <returns>The line, or <b>null</b> where the room this shudder happened in has no share of the pool —
    /// today that is <see cref="HavenRoom.TheObservationWalk"/> and nowhere else. Null is the room having
    /// nothing to say rather than an error: the unison-decide beat is a roomful of people looking up, and the
    /// walk is the one interior in the game built to have nobody in it (#1261). A caller that gets null says
    /// nothing at all.</returns>
    public static string? Line(Setting setting, HavenRoom room, ulong seed, int shudderIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = LinesFor(setting, room);
        return pool.Count == 0 ? null : pool[Index(seed, $"shudder-line:{shudderIndex}", pool.Count)];
    }

    /// <summary>The CHILL line for an escalated deep-site shudder — the one that doesn't land as "just a
    /// wave". Deterministically drawn from the chill pool per (seed, index), salted apart from the ordinary
    /// line stream so the two never lock together.
    ///
    /// <para>#867 · …and from the pool the GROUND earns. Both pools are drawn with the same salt, so a
    /// captain who rides the lift up mid-arc gets the same ordinal of a different register rather than a
    /// second stream that has to be kept in step.</para></summary>
    public static string ChillLine(bool groundHoldsPressure, ulong seed, int shudderIndex)
    {
        System.Collections.Generic.IReadOnlyList<string> pool = ChillLinesFor(groundHoldsPressure);
        return pool[Index(seed, $"shudder-chill-line:{shudderIndex}", pool.Count)];
    }

    /// <summary>#867 · THE WORDS THE NERVE LEDGER KEEPS FOR A CHILL, which is where the owner actually met
    /// this bug: a ledger on a park lawn saying <i>"a cold breath through the hull −1"</i>. The client used
    /// to type this string itself, one call site away from the pool that is chosen here — the same
    /// arrangement this project keeps paying for, two places that must agree and only one getting changed.
    /// The sentence lives beside the pool it belongs to, and the client passes the fact.</summary>
    public static string ChillNerveLabel(bool groundHoldsPressure) =>
        groundHoldsPressure ? PressurisedChillNerveLabel : VacuumChillNerveLabel;

    /// <summary>The chill's ledger line out in vacuum — a hull, and cold on the other side of it. Unchanged
    /// since #480 and pinned byte for byte: #867 forked the sentence, it did not edit this one.</summary>
    public const string VacuumChillNerveLabel = "a cold breath through the hull";

    /// <summary>The chill's ledger line on ground that holds its own air (owner-authored on #867). There is
    /// no hull down here and nothing on the far side of one — the cold that reaches you is the building's
    /// own, out of a corner of it that the plant was never asked to warm.</summary>
    public const string PressurisedChillNerveLabel = "a cold draught from somewhere the fans do not reach";
}

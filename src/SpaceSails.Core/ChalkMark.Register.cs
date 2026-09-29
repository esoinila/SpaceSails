using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

// #251 · Split from ChalkMark.cs, moved verbatim: the register's READS — which returns are owed, which
// marks the stone still carries, and when one was collected. Every const, tag and tag-writer stays in
// ChalkMark.cs.
public readonly partial record struct ChalkMark
{
    /// <summary>Has this return been collected, on any watch?</summary>
    public bool WasCollected(IEnumerable<string>? register)
    {
        string prefix = $"{CollectedTag}:{ParcelId}@";
        string old = AsSliceOneSaidIt(prefix);
        foreach (string tag in register ?? [])
        {
            if (tag.StartsWith(prefix, StringComparison.Ordinal) || tag.StartsWith(old, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// <b>EVERY RETURN OWED AT THIS HAVEN AND NOT YET COLLECTED</b>, in the register's ordinal order (the set
    /// has none of its own). Empty — without building anything — when no delivery was ever paid.
    /// </summary>
    /// <param name="tables">How many tables the gallery stands (see <see cref="For"/>).</param>
    public static IReadOnlyList<ChalkMark> OwedOn(IEnumerable<string>? register, string? havenId, int tables) =>
        Booked(register, havenId, tables, (mark, r) => !mark.WasCollected(r));

    /// <summary>
    /// <b>EVERY MARK THE STONE STILL CARRIES AT THIS HAVEN</b> — the owed returns, and a collected one until
    /// the turnover after it (Fable's ruling on #794 QA, 2026-09-29). The mark is the counterparty's signal,
    /// and the crew that keeps the gallery clean wipes it at the next turnover; nobody wiped the stone when the
    /// captain reached under the table. So the collection takes the move and the goods (<see cref="OwedOn"/>)
    /// and leaves the stone alone: a mark collected in its window stays up to the end of that window, and its
    /// wipe is told like any other seen wipe; the next window after the collection is the counterparty's, and
    /// the return is over, so nothing goes up again.
    /// </summary>
    public static IReadOnlyList<ChalkMark> OnTheStone(
        IEnumerable<string>? register, string? havenId, int tables, double simTime) =>
        Booked(register, havenId, tables, (mark, r) => mark.StoneOutlivesTheCollection(r, simTime));

    /// <summary>The window whose stone this watch belongs to: this one if it is a window, else the last.</summary>
    private long? StoneWindowOf(long watch) => IsWindow(watch) ? watch : LastWindowBefore(watch);

    /// <summary>The watch this return was collected on, or null — the earliest, if a save somehow holds two.</summary>
    public long? CollectedWatch(IEnumerable<string>? register)
    {
        string prefix = $"{CollectedTag}:{ParcelId}@";
        string old = AsSliceOneSaidIt(prefix);
        long? first = null;
        foreach (string tag in register ?? [])
        {
            string? rest = tag.StartsWith(prefix, StringComparison.Ordinal) ? tag[prefix.Length..]
                : tag.StartsWith(old, StringComparison.Ordinal) ? tag[old.Length..]
                : null;
            if (rest is not null
                && long.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out long w)
                && (first is null || w < first))
            {
                first = w;
            }
        }
        return first;
    }

    /// <summary>Does the stone still speak for this return at this moment — never collected, or collected no
    /// earlier than the window this watch's stone belongs to?</summary>
    private bool StoneOutlivesTheCollection(IEnumerable<string>? register, double simTime)
    {
        if (CollectedWatch(register) is not { } collected)
        {
            return true;
        }
        return StoneWindowOf(PatronRota.WatchIndex(simTime)) is { } window && window <= collected;
    }

    private static IReadOnlyList<ChalkMark> Booked(
        IEnumerable<string>? register, string? havenId, int tables, Func<ChalkMark, IEnumerable<string>, bool> keep)
    {
        if (register is null || havenId is null)
        {
            return [];
        }

        string prefix = $"{OwedTag}:{havenId}|";
        string old = AsSliceOneSaidIt(prefix);
        List<string>? owed = null;
        foreach (string tag in register)
        {
            if (tag.StartsWith(prefix, StringComparison.Ordinal))
            {
                (owed ??= []).Add(tag[prefix.Length..]);
            }
            else if (tag.StartsWith(old, StringComparison.Ordinal))
            {
                (owed ??= []).Add(tag[old.Length..]);
            }
        }
        if (owed is null)
        {
            return [];
        }
        owed.Sort(StringComparer.Ordinal);

        var marks = new List<ChalkMark>(owed.Count);
        foreach (string rest in owed)
        {
            int at = rest.LastIndexOf('@');
            if (at <= 0
                || !long.TryParse(rest[(at + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out long paid))
            {
                continue;
            }
            if (For(rest[..at], havenId, paid, tables) is { } mark && keep(mark, register)
                && !marks.Exists(m => m.ParcelId == mark.ParcelId))
            {
                marks.Add(mark);
            }
        }
        return marks;
    }
}

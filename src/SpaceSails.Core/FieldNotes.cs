using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #587 · THE FIELD BOOK — what you found on the ground, kept.
///
/// <para>Owner, walking a rebuilt site: <i>"this site looks good... maybe more tips and items. Also we
/// should maybe collect the tips to ledger if we don't show them again?"</i></para>
///
/// <para>The second half is the bug, and it is one this project has already fixed once in a different room.
/// Everything a captain finds out there — the papers on a ruin's floor, what somebody left at the foot of
/// the monolith, the tell that a stranger drew a shelter down before you got here — is delivered by
/// <c>ShowPulseMessage</c>, which fades after at most eight seconds and is then <b>gone forever</b>. You walk
/// twenty minutes across a vacuum for a sentence you cannot read twice.</para>
///
/// <para>The bar had exactly this problem and the owner ruled on it then (#347): <i>"we should collect the
/// useful rumors and tips into our ledger per contact… Tips, Intel, Rumors :-D"</i>, and the words a player
/// paid for <i>"may not hide"</i>. <see cref="OverheardLog"/> was the answer there. This is the same idiom
/// pointed at the regolith: a durable, capped, vault-persisted book, projected into the captain's ledger
/// <b>grouped by PLACE</b> — because on the ground the thing you want to reconstruct is not who told you, it
/// is <i>where you were standing</i>.</para>
///
/// <para>Pure, capped, oldest-trimmed. A log, not a leak.</para>
/// </summary>
public static class FieldNotes
{
    /// <summary>How many notes the book keeps, newest kept. Deliberately larger than the bar's forty: a
    /// single long excursion across a 310 × 260 field can turn over a dozen rooms, and the whole point is
    /// that a captain can reconstruct a walk they took days ago.</summary>
    public const int Cap = 80;

    /// <summary>Append a note, returning a new capped list (oldest trimmed off the front). Pure — never
    /// mutates the input. Exact duplicates of the most recent note are dropped, so a captain who presses
    /// [E] twice on the same floor does not file the same paper twice.</summary>
    public static IReadOnlyList<FieldNote> Append(IReadOnlyList<FieldNote>? log, FieldNote note)
    {
        var list = new List<FieldNote>(log ?? []);
        if (list.Count > 0
            && string.Equals(list[^1].Text, note.Text, StringComparison.Ordinal)
            && string.Equals(list[^1].Place, note.Place, StringComparison.Ordinal))
        {
            return list;
        }

        list.Add(note);
        if (list.Count > Cap)
        {
            list.RemoveRange(0, list.Count - Cap);
        }
        return list;
    }

    /// <summary>Collect the book into ledger cards, GROUPED BY PLACE — each ground you have searched becomes
    /// one card carrying its finds newest-first, and the grounds themselves come back most-recently-visited
    /// first.
    ///
    /// <para>Grouped by place rather than by kind on purpose. "Three papers, two caches, a locker" is an
    /// inventory; <i>"Miranda · The Ridge Camp"</i> with four lines under it is a MEMORY of an afternoon, and
    /// that is the thing worth being able to re-read.</para></summary>
    public static IReadOnlyList<FieldFinding> PerPlace(IReadOnlyList<FieldNote>? log)
    {
        if (log is null || log.Count == 0)
        {
            return [];
        }

        var order = new List<string>();
        var byPlace = new Dictionary<string, List<FieldNote>>(StringComparer.Ordinal);

        foreach (FieldNote note in log)
        {
            string place = note.Place ?? "";
            if (!byPlace.TryGetValue(place, out List<FieldNote>? lines))
            {
                lines = [];
                byPlace[place] = lines;
                order.Add(place);
            }
            lines.Add(note);
        }

        var findings = new List<FieldFinding>(order.Count);
        foreach (string place in order)
        {
            List<FieldNote> lines = byPlace[place];
            findings.Add(new FieldFinding(
                place,
                [.. Enumerable.Reverse(lines)],
                lines.Max(l => l.SimTime)));
        }

        // Most recently visited ground first — the walk you are most likely to be trying to remember.
        findings.Sort((a, b) => b.LatestSimTime.CompareTo(a.LatestSimTime));
        return findings;
    }

    /// <summary>#690 · One ground's page, read out of the same grouping the ledger renders — the satchel's
    /// NOTES tab standing at a door wants what THIS building has told the captain, not the memoirs.
    ///
    /// <para>Deliberately <see cref="PerPlace"/> filtered rather than a second pass over the log: one grouping
    /// law, one ordering, one place that can be wrong. <paramref name="place"/> is a
    /// <see cref="PlaceLabel"/> — pass anything else and the book will honestly tell you it has nothing.</para></summary>
    public static IReadOnlyList<FieldNote> Here(IReadOnlyList<FieldNote>? log, string? place)
    {
        if (place is null)
        {
            return [];
        }

        foreach (FieldFinding found in PerPlace(log))
        {
            if (string.Equals(found.Place, place, StringComparison.Ordinal))
            {
                return found.Lines;
            }
        }
        return [];
    }

    /// <summary>How a place is named in the book: the body and the site, which together are what a captain
    /// would say out loud. Built here so the client cannot invent a second format.</summary>
    public static string PlaceLabel(string? bodyName, string? siteName)
    {
        string body = string.IsNullOrWhiteSpace(bodyName) ? "somewhere" : bodyName!.Trim();
        string site = siteName?.Trim() ?? "";
        return site.Length == 0 ? body : $"{body} · {site}";
    }
}

/// <summary>One thing found on the ground, kept verbatim. <paramref name="Place"/> is
/// <see cref="FieldNotes.PlaceLabel"/>; <paramref name="Glyph"/> is the one-character hint the ledger card
/// leads with so a long list can be skimmed.</summary>
public readonly record struct FieldNote(string Text, double SimTime, string Place, string Glyph);

/// <summary>One ground you have searched, with everything you found there, newest first.</summary>
public readonly record struct FieldFinding(
    string Place, IReadOnlyList<FieldNote> Lines, double LatestSimTime);

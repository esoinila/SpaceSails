using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpaceSails.Core;

/// <summary>
/// #1151 slice 1 · <b>THE LOSS, THE FORM, THE BOOKING.</b> The owner's 2026-09-06 ruling (every loss but death is a
/// CLAIM, made at a desk) meets the adjuster's room #1365 built. Slice 1 claims the one loss the game already
/// prices without charging anything for it: <b>LOSS OF USE</b> — the days a holed sail took to mend (the amended cut,
/// Fable 2026-10-07: the game prices no sail repair, so no credit moves anywhere in this slice).
///
/// <para><b>Three pieces, one register.</b> A completed mend window writes a loss line into the book and a LOSS tag
/// into the durable register (#711/#794's idiom, <c>_roomsTurnedOver</c>); the ship's desk copies the newest unclaimed
/// loss onto the blank form (the paper becomes <c>claim-form-filled:{when}:{tenths}</c> and the loss is tagged FILED);
/// the adjuster's console books it (a BOOKED tag — the vault row, once per filled form). Every fact is a tag, so the
/// vault carries the whole slice and a reload can neither forget a claim nor claim a loss twice.</para>
///
/// <para>Every player-facing word is Fable canon (the brief cut and its amendment, 2026-10-07), verbatim. The
/// interview, the flashback and the adjuster herself are later slices.</para>
/// </summary>
public static class HullClaim
{
    /// <summary>The loss line's glyph — the 📋 idiom of a filed sheet.</summary>
    public const string LossGlyph = "📋";

    /// <summary>The booking's glyph — the 📍 idiom of a place's own book entry.</summary>
    public const string BookGlyph = "📍";

    /// <summary>The paper the desk makes of the blank form — a prefix; the rest of the id is the loss it names.</summary>
    public const string FilledPrefix = "claim-form-filled:";

    /// <summary>…its title.</summary>
    public const string FilledTitle = "A claim form, filled";

    /// <summary>The desk verb, on the ship's own desk.</summary>
    public const string VerbLabel = "✍ Copy the loss onto the claim form";

    /// <summary>The console in the adjuster's room, on her watch.</summary>
    public const string BookLabel = "🧾 Book the claim — [E]";

    /// <summary>The booking, told (once per filled form it is booked; said again on a repeat press).</summary>
    public const string BookedLine =
        "Booked for her watch. The cold rooms will keep you exactly as patient as you arrive.";

    /// <summary>The 📍 book entry the booking files, once.</summary>
    public const string BookEntryLine =
        "A claim, booked. Nebula Mutual resurrects a man without a form; a sail wants three. Somebody designed that, and it was not the sail.";

    /// <summary>The shut door's second line, said only with a filled form in the satchel.</summary>
    public const string ShutExtraLine = "The form stays warm in the satchel. Nothing else here is.";

    // ── THE LOSS: HOW LONG A MEND TOOK ──────────────────────────────────────────────────────────────────

    /// <summary>One mend window: when the sail was first holed and when the crew's sewing is due to finish.</summary>
    public readonly record struct Mend(double HoledAt, double ClearsAt);

    /// <summary>A holing at <paramref name="now"/>. Inside a window already open it EXTENDS that window (the same
    /// loss — its first-hole time is kept and its end only ever moves later); with no window open it starts one.</summary>
    public static Mend Hole(Mend? open, double now, double repairSeconds) =>
        open is { } m
            ? m with { ClearsAt = Math.Max(m.ClearsAt, now + repairSeconds) }
            : new Mend(now, now + repairSeconds);

    /// <summary>Is the window finished at <paramref name="now"/>?</summary>
    public static bool IsDone(Mend mend, double now) => now >= mend.ClearsAt;

    /// <summary>Days lost to the mend, first hole to the moment it actually cleared — COMPUTED, never assumed.</summary>
    public static double DaysLost(Mend mend, double clearedAt) => Math.Max(0, (clearedAt - mend.HoledAt) / 86400.0);

    /// <summary>The days in tenths, rounded — the figure the register keeps (an integer survives a vault).</summary>
    public static int TenthsOf(double days) => (int)Math.Round(days * 10.0, MidpointRounding.AwayFromZero);

    /// <summary>{n}: tenths of a day as the book reads them — <c>2.0</c>.</summary>
    public static string DaysText(int tenths) => (tenths / 10.0).ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>The loss line, with {n} the days the mend actually took.</summary>
    public static string LossLine(int tenths) =>
        $"Hull holed — {DaysText(tenths)} days under sail-mend, and the sky kept its schedule without you. "
        + "The policy calls lost days claimable. Claimable is not the same as paid.";

    /// <summary>What the book files the loss line under: Nebula Mutual + the place of repair.</summary>
    public static string SubjectsFor(string? place) =>
        CaseSubjects.Line(CaseSubjects.Office(NebulaLore.TermsRefiledOffice), CaseSubjects.Place(place ?? ""));

    // ── THE REGISTER ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One loss: when its mend completed (whole sim seconds — its identity) and the days it took, in tenths.</summary>
    public readonly record struct Loss(long DoneAt, int Tenths);

    private const string LossTagPrefix = "claim:loss:";
    private const string FiledTagPrefix = "claim:filed:";
    private const string BookedTagPrefix = "claim:booked:";

    /// <summary>The tag a completed mend leaves: an unclaimed loss.</summary>
    public static string LossTag(Loss loss) => $"{LossTagPrefix}{loss.DoneAt}:{loss.Tenths}";

    /// <summary>The tag filling the form leaves: this loss has been copied onto a form.</summary>
    public static string FiledTag(long doneAt) => $"{FiledTagPrefix}{doneAt}";

    /// <summary>The tag the booking leaves — the vault row, once per filled form.</summary>
    public static string BookedTag(long doneAt) => $"{BookedTagPrefix}{doneAt}";

    /// <summary>Make the loss out of a completed mend.</summary>
    public static Loss LossOf(Mend mend, double clearedAt) =>
        new((long)Math.Floor(clearedAt), TenthsOf(DaysLost(mend, clearedAt)));

    /// <summary>Every loss in the register that has not been copied onto a form, oldest first.</summary>
    public static IReadOnlyList<Loss> Unclaimed(IEnumerable<string> register)
    {
        ArgumentNullException.ThrowIfNull(register);
        var all = new HashSet<string>(register, StringComparer.Ordinal);
        var losses = new List<Loss>();
        foreach (string tag in all)
        {
            if (TryReadLoss(tag, out Loss loss) && !all.Contains(FiledTag(loss.DoneAt)))
            {
                losses.Add(loss);
            }
        }

        losses.Sort((a, b) => a.DoneAt.CompareTo(b.DoneAt));
        return losses;
    }

    /// <summary>The newest unclaimed loss — the one the desk copies — or null.</summary>
    public static Loss? NewestUnclaimed(IEnumerable<string> register)
    {
        IReadOnlyList<Loss> all = Unclaimed(register);
        return all.Count == 0 ? null : all[^1];
    }

    private static bool TryReadLoss(string tag, out Loss loss)
    {
        loss = default;
        if (!tag.StartsWith(LossTagPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = tag[LossTagPrefix.Length..].Split(':');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long at)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int tenths))
        {
            return false;
        }

        loss = new Loss(at, tenths);
        return true;
    }

    // ── THE FORM ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The filled form's paper id for this loss.</summary>
    public static string FilledId(Loss loss) => $"{FilledPrefix}{loss.DoneAt}:{loss.Tenths}";

    /// <summary>Is this the filled form of some loss?</summary>
    public static bool IsTheFilledForm(string? paperId) => TryReadFilled(paperId, out _);

    /// <summary>Which loss this filled form names.</summary>
    public static bool TryReadFilled(string? paperId, out Loss loss)
    {
        loss = default;
        if (paperId is null || !paperId.StartsWith(FilledPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return TryReadLoss(LossTagPrefix + paperId[FilledPrefix.Length..], out loss);
    }

    /// <summary>The filled form's document, with {n} the days the mend took.</summary>
    public static string FilledDocument(int tenths) =>
        $"Loss: hull, holed. Lost to the mend: {DaysText(tenths)} days under way. Claimant: the captain of record. "
        + "The remaining boxes want codes the desk does not have, and the desk suspects that is their purpose.";

    /// <summary>The filled form's document, read off its own id (empty for any other).</summary>
    public static string DocumentOf(string paperId) =>
        TryReadFilled(paperId, out Loss loss) ? FilledDocument(loss.Tenths) : "";

    /// <summary>Does the satchel hold the blank claim form?</summary>
    public static bool HoldsTheBlank(IEnumerable<Satchel.Item>? carried) =>
        Holds(carried, i => i.Kind == Satchel.Kind.Paper && AdjustersRoom.Office.IsTheSheet(i.Id));

    /// <summary>Does the satchel hold any filled claim form?</summary>
    public static bool HoldsAFilledForm(IEnumerable<Satchel.Item>? carried) =>
        Holds(carried, i => i.Kind == Satchel.Kind.Paper && IsTheFilledForm(i.Id));

    /// <summary>Has any loss ever been copied onto a form (a FILED tag in the register)?</summary>
    public static bool HasFiledAny(IEnumerable<string> register)
    {
        ArgumentNullException.ThrowIfNull(register);
        foreach (string tag in register)
        {
            if (tag.StartsWith(FiledTagPrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The dev start: <c>?claim=1</c> on an address — read the way <see cref="PreservationOffice.CheatIn"/> is,
    /// writing no world at parse time.</summary>
    public static bool CheatIn(string? uri)
    {
        int q = uri?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (uri is null || q < 0)
        {
            return false;
        }

        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (pair.Equals("claim=1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The first filled form the satchel holds, or null.</summary>
    public static Loss? TheFilledFormHeld(IEnumerable<Satchel.Item>? carried)
    {
        foreach (Satchel.Item i in carried ?? [])
        {
            if (i.Kind == Satchel.Kind.Paper && TryReadFilled(i.Id, out Loss loss))
            {
                return loss;
            }
        }

        return null;
    }

    private static bool Holds(IEnumerable<Satchel.Item>? carried, Func<Satchel.Item, bool> which)
    {
        foreach (Satchel.Item i in carried ?? [])
        {
            if (which(i))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary><b>IS THE DESK VERB ON OFFER?</b> A blank form held AND an unclaimed loss in the register — both, or
    /// the verb is not drawn at all.</summary>
    public static bool CanFillTheForm(IEnumerable<Satchel.Item>? carried, IEnumerable<string> register) =>
        HoldsTheBlank(carried) && NewestUnclaimed(register) is not null;

    /// <summary>The satchel after the desk has copied the loss: the blank is replaced by the filled form — or null
    /// when the verb is not on offer. One loss per form.</summary>
    public static (IReadOnlyList<Satchel.Item> Satchel, Loss Loss)? Fill(
        IReadOnlyList<Satchel.Item>? carried, IEnumerable<string> register)
    {
        if (!HoldsTheBlank(carried) || NewestUnclaimed(register) is not { } loss)
        {
            return null;
        }

        IReadOnlyList<Satchel.Item> without = Core.Satchel.Remove(carried, Core.Satchel.Kind.Paper, AdjustersRoom.SheetId);
        return (Core.Satchel.Add(without, new Satchel.Item(Core.Satchel.Kind.Paper, FilledId(loss))), loss);
    }

    /// <summary>Every player-facing string this slice publishes (the two documents are the figure's own — {n} = 2.0).</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return LossLine(20);
        yield return FilledTitle;
        yield return FilledDocument(20);
        yield return VerbLabel;
        yield return BookLabel;
        yield return BookedLine;
        yield return BookEntryLine;
        yield return ShutExtraLine;
    }
}

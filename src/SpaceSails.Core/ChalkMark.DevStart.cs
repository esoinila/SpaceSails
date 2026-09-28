using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

// #251 · Split from ChalkMark.cs, moved verbatim: THE DEV START — the cheat a dev URL names and the paid
// watch it back-dates to. Every const, tag and line of the mark stays in ChalkMark.cs.
public readonly partial record struct ChalkMark
{
    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Which scene <c>?chalk=</c> asked for.</summary>
    public enum Cheat
    {
        /// <summary>No chalk asked for.</summary>
        None,
        /// <summary><c>?dock=selene-gate&amp;ashore=1&amp;chalk=1</c> — a paid delivery on record, the clock at a
        /// window.</summary>
        Up,
        /// <summary><c>…&amp;chalk=wiped</c> — one watch later, the mark seen and wiped.</summary>
        Wiped,
    }

    /// <summary>Read <c>chalk=</c> off an address. Read off the address rather than stored, because a latch
    /// on the page would move the frame ledger for a flag that is false in every other scene.</summary>
    public static Cheat CheatIn(string? uri)
    {
        if (uri is null)
        {
            return Cheat.None;
        }
        int q = uri.IndexOf('?', StringComparison.Ordinal);
        if (q < 0)
        {
            return Cheat.None;
        }
        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (!pair.StartsWith("chalk=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string v = Uri.UnescapeDataString(pair["chalk=".Length..]).ToLowerInvariant();
            return v switch
            {
                "1" or "true" or "yes" or "up" => Cheat.Up,
                "wiped" => Cheat.Wiped,
                _ => Cheat.None,
            };
        }
        return Cheat.None;
    }

    /// <summary>The paid watch that puts <paramref name="now"/> on a window (or one watch past one, for
    /// <see cref="Cheat.Wiped"/>) for this parcel at this haven — the dev start's arithmetic, done by the
    /// clock's own owner.</summary>
    public static long PaidWatchFor(Cheat cheat, string parcelId, string havenId, long now)
    {
        long window = cheat == Cheat.Wiped ? now - 1 : now;
        return window - OffsetFor(parcelId, havenId);
    }
}

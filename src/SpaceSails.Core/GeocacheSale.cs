using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #319 slice 2 · <b>THE GEOCACHE SALE.</b> The owner, in the issue's body: <i>"transact business safely by
/// wiring the money in dark-web and passing the good as geocache."</i>
///
/// <para>Composition, and nothing new stored. The goods are already in the ground (slice 1, #1194, a
/// <see cref="TreasureCache.Deposit"/>). The desk already pays on a watch clock (#711's
/// <see cref="ParcelDrop.WatchesBeforeItLands"/>). Forensics already say <i>somebody dug</i> at a lifted
/// hole. The watchdog economy (#303) already prices a site by the Old Ones standing over it
/// (<see cref="TreasureCache.ReeverLevel"/>, the term <see cref="CacheSafety.Read(TreasureCache, int)"/> reads).
/// What this file adds is a price, a fee, a schedule and a handful of watch-indexed tags in the captain's
/// register of ground gone through, the way <see cref="ParcelDrop.NothingForThisHullOn"/>'s are.</para>
///
/// <h3>What the fence will buy</h3>
///
/// <para>There is no single satchel valuation in this game. The ONE thing the fence buys out of a pocket is
/// the compromising chip, priced off its own contract (<see cref="CompromisingChip.FencePrice"/>); every
/// other row the desk deals in is sold TO the captain (the inspector's card, the key) or paid for by delivery
/// (the parcel). So a cache is sellable exactly when it holds the chip, and the row does not exist for any
/// other deposit. The day the fence prices a second kind, <see cref="WhatTheFenceWouldBuy"/> is where it goes.</para>
///
/// <para>The lines are Fable-authored canon (#319, 2026-09-27) and verbatim.</para>
/// </summary>
public static class GeocacheSale
{
    // ── THE PRICE IS THE GROUND'S ───────────────────────────────────────────────────────────────────────

    /// <summary>The fee on a ground with nothing standing over it, in percent. A judgement call for the
    /// owner.</summary>
    public const int MinimumFeePercent = 10;

    /// <summary>What each watchdog adds to the fee, in percent. Also the owner's.</summary>
    public const int FeePercentPerWatchdog = 8;

    /// <summary>
    /// <b>THE ESCROW FEE, IN PERCENT, FOR A GROUND WITH THIS MANY WATCHDOGS ON IT</b> — stated once. A
    /// dangerous ground is safe from thieves and expensive to lift, and the fee says so: it rises with every
    /// Old One up to <see cref="ReeverRaid.MaxReevers"/> and never falls.
    /// </summary>
    public static int FeeFor(int watchdogs) =>
        MinimumFeePercent + (FeePercentPerWatchdog * Math.Clamp(watchdogs, 0, ReeverRaid.MaxReevers));

    /// <summary>What the captain is paid: the fence's price, less the ground's fee. Never negative.</summary>
    public static int Quote(int fencePrice, int watchdogs) =>
        Math.Max(0, fencePrice - (Math.Max(0, fencePrice) * FeeFor(watchdogs) / 100));

    /// <summary>The watchdogs at a cache's ground: the one Core reader of them, the chest's own
    /// <see cref="TreasureCache.ReeverLevel"/>.</summary>
    public static int WatchdogsAt(TreasureCache cache) => Math.Max(0, cache.ReeverLevel);

    /// <summary>
    /// <b>WHAT IN THIS HOLE THE FENCE WOULD BUY</b>, or null. The chip, whether it went in through the chest's
    /// manifest (#202's hot line, <see cref="CompromisingChip.Manifest"/>) or through the satchel's own
    /// deposit (slice 1). Nothing else has a price at the desk today.
    /// </summary>
    public static string? WhatTheFenceWouldBuy(TreasureCache cache)
    {
        foreach (Satchel.Item item in cache.Deposit ?? [])
        {
            if (CompromisingChip.IsTheChip(item))
            {
                return CompromisingChip.Name;
            }
        }
        foreach (CacheCargo cargo in cache.Cargo ?? [])
        {
            if (string.Equals(cargo.CargoClass, CompromisingChip.Name, StringComparison.Ordinal))
            {
                return CompromisingChip.Name;
            }
        }
        return null;
    }

    /// <summary>Could this cache be offered at all: the captain's own, in the ground, holding something the
    /// fence prices.</summary>
    public static bool IsSellable(TreasureCache cache) =>
        cache.PlayerOwned && WhatTheFenceWouldBuy(cache) is not null;

    // ── THE BUYER KEEPS A SCHEDULE ──────────────────────────────────────────────────────────────────────

    /// <summary>The watch the buyer lifts it: the sale's watch plus the parcel's own seeded 2–4.</summary>
    public static long LiftWatch(string cacheId, long soldWatch) =>
        soldWatch + ParcelDrop.WatchesBeforeItLands(cacheId);

    /// <summary>Has the buyer lifted it by this moment? Exactly at the seeded watch, never before.</summary>
    public static bool IsLiftedAt(string cacheId, long soldWatch, double simTime) =>
        PatronRota.WatchIndex(simTime) >= LiftWatch(cacheId, soldWatch);

    /// <summary>The sim-time the lift happens — the start of its watch, which is what the hole is dated by.</summary>
    public static double LiftMoment(string cacheId, long soldWatch) =>
        LiftWatch(cacheId, soldWatch) * PatronRota.WatchSeconds;

    // ── THE TAGS ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The sale: <c>geocache:sold:{cacheId}@{watch}</c>.</summary>
    public const string SoldTag = "geocache:sold";

    /// <summary>The buyer has lifted it and the escrow is owed: <c>geocache:owed:{cacheId}|{gross}|{net}@{liftWatch}</c>.</summary>
    public const string OwedTag = "geocache:owed";

    /// <summary>The escrow was paid: <c>geocache:paid:{cacheId}@{watch}</c>.</summary>
    public const string PaidTag = "geocache:paid";

    /// <summary>The captain lifted it first and the sale died: <c>geocache:dead:{cacheId}@{watch}</c>.</summary>
    public const string DeadTag = "geocache:dead";

    /// <summary>The port whose desk has nothing for the captain for the rest of this visit:
    /// <c>geocache:shut:{portId}</c>.</summary>
    public const string ShutTag = "geocache:shut";

    /// <summary>Where a sold hole is: <c>geocache:hole:{cacheId}|{body}|{site}|{x}|{y}</c>.</summary>
    public const string HoleTag = "geocache:hole";

    /// <summary>The hole's field-book entry was filed: <c>geocache:hole-read:{cacheId}</c>.</summary>
    public const string HoleReadTag = "geocache:hole-read";

    private static string I(long v) => v.ToString(CultureInfo.InvariantCulture);

    private static string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>The sale's tag.</summary>
    public static string SoldOn(string cacheId, long watch) => $"{SoldTag}:{cacheId}@{I(watch)}";

    /// <summary>The escrow owed, with the two sums the pulse needs, so the payment does not have to find a
    /// chest that is no longer in the ground.</summary>
    public static string OwedOn(string cacheId, int gross, int net, long liftWatch) =>
        $"{OwedTag}:{cacheId}|{I(gross)}|{I(net)}@{I(liftWatch)}";

    /// <summary>The escrow paid.</summary>
    public static string PaidOn(string cacheId, long watch) => $"{PaidTag}:{cacheId}@{I(watch)}";

    /// <summary>The sale killed by the captain's own shovel.</summary>
    public static string DeadOn(string cacheId, long watch) => $"{DeadTag}:{cacheId}@{I(watch)}";

    /// <summary>A port's desk shut to this trade for the rest of the visit.</summary>
    public static string ShutAt(string portId) => $"{ShutTag}:{portId}";

    /// <summary>Where the hole is, so walking onto it can be read.</summary>
    public static string HoleAt(string cacheId, string bodyId, int? siteIndex, double x, double y) =>
        $"{HoleTag}:{cacheId}|{bodyId}|{(siteIndex is { } s ? I(s) : "-")}|{F(x)}|{F(y)}";

    /// <summary>The hole's entry filed.</summary>
    public static string HoleReadOn(string cacheId) => $"{HoleReadTag}:{cacheId}";

    /// <summary>A sale on record.</summary>
    public readonly record struct Sale(string CacheId, long SoldWatch);

    /// <summary>An escrow waiting at the desk.</summary>
    public readonly record struct Owed(string CacheId, int Gross, int Net, long LiftWatch, string Tag);

    /// <summary>A sold hole on some ground.</summary>
    public readonly record struct Hole(string CacheId, string BodyId, int? SiteIndex, double X, double Y);

    /// <summary>Every sale on record, in the register's ordinal order. Empty when nothing was ever sold.</summary>
    public static IReadOnlyList<Sale> SalesIn(IEnumerable<string>? register)
    {
        var sales = new List<Sale>();
        foreach (string tag in register ?? [])
        {
            if (!tag.StartsWith(SoldTag + ":", StringComparison.Ordinal))
            {
                continue;
            }
            string rest = tag[(SoldTag.Length + 1)..];
            int at = rest.LastIndexOf('@');
            if (at > 0 && long.TryParse(rest[(at + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out long w))
            {
                sales.Add(new Sale(rest[..at], w));
            }
        }
        sales.Sort((a, b) => string.CompareOrdinal(a.CacheId, b.CacheId));
        return sales;
    }

    /// <summary>Every escrow owed, in ordinal order.</summary>
    public static IReadOnlyList<Owed> OwedIn(IEnumerable<string>? register)
    {
        var owed = new List<Owed>();
        foreach (string tag in register ?? [])
        {
            if (!tag.StartsWith(OwedTag + ":", StringComparison.Ordinal))
            {
                continue;
            }
            string rest = tag[(OwedTag.Length + 1)..];
            int at = rest.LastIndexOf('@');
            string[] parts = at > 0 ? rest[..at].Split('|') : [];
            if (parts.Length == 3
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gross)
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int net)
                && long.TryParse(rest[(at + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out long lw))
            {
                owed.Add(new Owed(parts[0], gross, net, lw, tag));
            }
        }
        owed.Sort((a, b) => string.CompareOrdinal(a.Tag, b.Tag));
        return owed;
    }

    /// <summary>Every sold hole on record.</summary>
    public static IReadOnlyList<Hole> HolesIn(IEnumerable<string>? register)
    {
        var holes = new List<Hole>();
        foreach (string tag in register ?? [])
        {
            if (!tag.StartsWith(HoleTag + ":", StringComparison.Ordinal))
            {
                continue;
            }
            string[] p = tag[(HoleTag.Length + 1)..].Split('|');
            if (p.Length == 5
                && double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                && double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                int? site = int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : null;
                holes.Add(new Hole(p[0], p[1], site, x, y));
            }
        }
        return holes;
    }

    /// <summary>Does the register say anything about this cache past its sale — lifted, paid, or dead?</summary>
    public static bool IsSettled(IEnumerable<string>? register, string cacheId)
    {
        foreach (string tag in register ?? [])
        {
            if (tag.StartsWith($"{OwedTag}:{cacheId}|", StringComparison.Ordinal)
                || tag.StartsWith($"{PaidTag}:{cacheId}@", StringComparison.Ordinal)
                || tag.StartsWith($"{DeadTag}:{cacheId}@", StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Has this cache been sold (once is all a cache gets)?</summary>
    public static bool WasSold(IEnumerable<string>? register, string cacheId)
    {
        foreach (Sale s in SalesIn(register))
        {
            if (string.Equals(s.CacheId, cacheId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>How close to a sold hole a captain must walk for it to be read.</summary>
    public const double HoleReachDu = 2.5;

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Which scene <c>?geocache=</c> asked for.</summary>
    public enum Cheat
    {
        /// <summary>Nothing asked.</summary>
        None,
        /// <summary><c>&amp;geocache=1</c> — a priced cache in the ground, the desk in reach.</summary>
        Buried,
        /// <summary><c>&amp;geocache=lifted</c> — the location sold and the buyer already been.</summary>
        Lifted,
    }

    /// <summary>Read <c>geocache=</c> off an address, the way <c>?perf=1</c> is read: a dev latch has no
    /// business in a page field the frame ledger walks.</summary>
    public static Cheat CheatIn(string? uri)
    {
        int q = uri?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (uri is null || q < 0)
        {
            return Cheat.None;
        }
        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (pair.StartsWith("geocache=", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair["geocache=".Length..]).ToLowerInvariant() switch
                {
                    "1" or "true" or "yes" => Cheat.Buried,
                    "lifted" => Cheat.Lifted,
                    _ => Cheat.None,
                };
            }
        }
        return Cheat.None;
    }

    // ── THE LINES (verbatim, #319 · Fable, 2026-09-27) ──────────────────────────────────────────────────

    /// <summary>The glyph the hole's entry is filed under.</summary>
    public const string Glyph = "🗺";

    /// <summary>The row.</summary>
    public const string RowLabel = "SELL THE LOCATION";

    /// <summary>The row's sub-line: the thing, and the ground.</summary>
    public const string SubLine = "{0}, under {1}.";

    /// <summary>The quote card, for a ground with watchdogs on it.</summary>
    public const string QuoteLine =
        "Escrow. The buyer lifts it; you are paid when it is lifted. The fee is the ground's: "
        + "{0} watchdogs on it, {1} off.";

    /// <summary>The quote card, for a ground with nothing on it.</summary>
    public const string QuoteLineEmptyGround =
        "Escrow. The buyer lifts it; you are paid when it is lifted. The fee is the ground's: "
        + "nothing on it, the minimum off.";

    /// <summary>On confirming.</summary>
    public const string SentLine = "Coordinates sent. Nobody's name on either end.";

    /// <summary>The payment pulse. <c>{0}</c> is the fence's price before the fee.</summary>
    public const string PaymentLine = "The hole was where you said. {0} cr, less the fee.";

    /// <summary>The captain lifted it first — told once, at the desk.</summary>
    public const string LiftedFirstLine = "The buyer found a hole. The desk has nothing for you, and remembers.";

    /// <summary>The field book, at the lifted spot.</summary>
    public const string HoleEntry =
        "Lifted clean. Whoever it was came, dug, and left nothing but the shape of the hole.";

    /// <summary>The row's sub-line, filled.</summary>
    public static string TheSubLine(string item, string ground) =>
        string.Format(CultureInfo.InvariantCulture, SubLine, item, ground);

    /// <summary>The quote card, filled for this ground.</summary>
    public static string TheQuoteLine(int watchdogs) =>
        watchdogs <= 0
            ? QuoteLineEmptyGround
            : string.Format(CultureInfo.InvariantCulture, QuoteLine, watchdogs,
                FeeFor(watchdogs).ToString(CultureInfo.InvariantCulture) + "%");

    /// <summary>The payment pulse, filled.</summary>
    public static string ThePaymentLine(int gross) =>
        string.Format(CultureInfo.InvariantCulture, PaymentLine, gross.ToString("N0", CultureInfo.InvariantCulture));

    /// <summary>Every sentence this slice can put on a screen.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return RowLabel;
        yield return SubLine;
        yield return QuoteLine;
        yield return QuoteLineEmptyGround;
        yield return SentLine;
        yield return PaymentLine;
        yield return LiftedFirstLine;
        yield return HoleEntry;
    }
}

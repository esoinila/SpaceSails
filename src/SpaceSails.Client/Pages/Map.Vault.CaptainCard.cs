using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Vault.CaptainCard — HOW A CAPTAIN IS SHOWN ON A SAVE ROW, and nothing else.
//
// #563 slice 2 · A PURE MOVE out of Map.Vault.cs. Not a refactor: every line below is the line that was
// there, in the order it was in. The reason is the 1,500-line file law (NoSourceFileIsTooLongTests), which
// wants twenty-five lines of daylight between the line and the largest file beneath it — Map.Vault.cs stood
// at 1,469 and the treadmill's ledger section put it inside that margin.
//
// This region is the cheapest to lift and the most obviously separable: it is PRESENTATION over an identity
// Core already seeded (GameThreadInfo, CaptainSuccession) — a subtitle, a retired line, a monogram, a hue —
// and it reads no vault, writes no slot and knows nothing about saving.
public partial class Map
{
    // ── Captain-card display helpers (presentation over the Core-seeded identity). ──

    // The active universe's registry row — its captain identity — for the in-play captain chip (owner
    // 2026-07-19: "the current captain profile pic could also be at some corner of the screen while
    // playing"). Prefers the cached list; falls back to a direct registry read so the chip is correct even
    // before the first RefreshThreadList of a session. The name is EDITABLE stored data (a later lane's
    // rename UI writes GameThreadInfo.CaptainName); the chip just reads whatever is stored (or seeded).
    private GameThreadInfo? ActiveThreadInfo
        => string.IsNullOrEmpty(_activeThreadId)
            ? null
            : _threadList.FirstOrDefault(t => t.Id == _activeThreadId) ?? Threads.Get(_activeThreadId);

    // The book for a given thread id: the bound active book when it IS the active universe (so the drawer
    // and live writes stay on one instance), else a fresh book over the same store for that other universe.
    private SaveSlotBook BookFor(string threadId)
        => string.IsNullOrEmpty(threadId) || threadId == (_activeThreadId ?? "")
            ? Slots
            : new SaveSlotBook(_slotStore, threadId);

    // A captain's card subtitle: where the voyage sits and when it was last touched — the "which universe is
    // this" line under the name. A thin (pre-#354) thread honestly reads "unknown waters" here.
    private static string CaptainWhen(GameThreadInfo t)
    {
        string place = string.IsNullOrWhiteSpace(t.Where) ? "unknown waters" : t.Where;
        string last = t.LastActiveTicks > 0
            ? new DateTimeOffset(t.LastActiveTicks, TimeSpan.Zero).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "—";
        return $"{place} · day {Math.Max(0, t.SimDay)} · last active {last}";
    }

    // The captain card's "retired" line (Evening wind #20): who held this license before the piracy
    // insurance replaced them, most recent first. Compact — the newest one or two retirees, then a "+N
    // more" tail so a long-lived, oft-killed universe doesn't run the card off the door. Each entry reads
    // "under Capt. <name> until day <N>" (Core CaptainSuccession.RetiredLine).
    private static string CaptainRetiredSummary(GameThreadInfo t)
    {
        IReadOnlyList<RetiredCaptain> retired = t.Retired;
        if (retired.Count == 0)
        {
            return "";
        }

        const int show = 2;
        IEnumerable<RetiredCaptain> newestFirst = retired.Reverse();
        string head = string.Join(" · ", newestFirst.Take(show).Select(CaptainSuccession.RetiredLine));
        int more = retired.Count - show;
        return more > 0 ? $"{head} · +{more} more" : head;
    }

    // The monogram initial for the fallback avatar disc (first letter of the captain's given name).
    private static string CaptainInitial(string captainName)
    {
        string n = captainName.StartsWith("Captain ", StringComparison.Ordinal)
            ? captainName["Captain ".Length..]
            : captainName;
        return n.Length > 0 ? n[..1].ToUpperInvariant() : "?";
    }

    // A stable seeded hue for the fallback disc, so a captain whose portrait fails to load still gets a
    // consistent colour (and two captains rarely share one). Derived from the thread id, deterministic.
    private static string CaptainMonoColor(string id)
    {
        uint h = 2166136261u;
        foreach (char c in id)
        {
            h = (h ^ c) * 16777619u;
        }

        return $"hsl({h % 360} 42% 36%)";
    }
}

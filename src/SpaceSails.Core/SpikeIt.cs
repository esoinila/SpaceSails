using System;
using System.Collections.Generic;
using System.Globalization;
using SpaceSails.Core.Interior;

namespace SpaceSails.Core;

/// <summary>
/// #1202 slice 2 · <b>SPIKE IT.</b> Slice 1 made the wire authored: three sim-days after Rauha Lind pays for her
/// bunk, the wire prints her story. This slice lets a captain get between the sender and the reader. While her
/// story is pending, the dark-web desk grows one row: somebody would rather it did not run. Her pages are where
/// she writes — the second table in Selene Gate's gallery, the room nobody is in. She steps to the machines on
/// her own clock; the pages do not.
///
/// <para>Take them and she cannot file (<see cref="Outcome.Spiked"/>). Leave the client's page on her stack and
/// she files a sentence with the truth written out of it (<see cref="Outcome.Altered"/>). Do nothing before the
/// window and it runs, and the client learns you tried (<see cref="Outcome.Late"/>). Only the wire and the ledger
/// change; nothing in the world ever confirms which version was true (§13.8).</para>
///
/// <h3>What lives here</h3>
///
/// <para>Every word (Fable canon, verbatim, enumerated by <see cref="AllProse"/>), the purse, the table, her
/// cadence at the machine, the two papers, the two moves on her table's card, and the one decision the window
/// makes. The contract's own state rides slice 1's <see cref="CarryThePress.Passage"/> — the same one line in the
/// quest record's free slot — so there is no page field and no vault section for any of it.</para>
/// </summary>
public static class SpikeIt
{
    // ── THE LINES (verbatim, #1202 slice 2 · Fable, 2026-09-28 night) ───────────────────────────────────

    /// <summary>The desk row's label.</summary>
    public const string RowLabel = "SPIKE IT";

    /// <summary>The desk row. <c>{Body}</c> is her story's body; <c>{cr}</c> is the purse.</summary>
    public const string RowLine =
        "Somebody would rather the {Body} story did not run. {cr} if it does not; half if it runs different. Her "
        + "pages are wherever she writes. Nobody said how.";

    /// <summary>Taking it (pulse, once) — the client's page lands in the satchel.</summary>
    public const string TakenLine =
        "A page arrives with the terms: a paragraph in nobody's hand, about {Body}, saying nothing at all in "
        + "perfect grammar.";

    /// <summary>Her at the gallery — told once, the first time the captain is in the gallery while the contract
    /// is active.</summary>
    public const string AtHerTableLine =
        "She is at the far table with the recorder and a stack of pages, writing the way people write when the "
        + "window is closing.";

    /// <summary>The first move's label.</summary>
    public const string TakeThePagesLabel = "TAKE THE PAGES";

    /// <summary>…and what it says, on the card.</summary>
    public const string TakeThePagesLine =
        "Six pages, close-written, the top one still damp. The recorder is in her pocket; the pages are not.";

    /// <summary>…and the field book's entry for it. <c>{N}</c> is the watches until the window.</summary>
    public const string TookThePagesEntry =
        "Took a stringer's pages off her table while she fed the machine. The window is {N} watches off.";

    /// <summary>The second move's label.</summary>
    public const string LeaveYourPageLabel = "LEAVE YOUR PAGE";

    /// <summary>…and what it says, on the card.</summary>
    public const string LeaveYourPageLine =
        "Your page goes on the stack, third from the top, where a tired eye reads without looking.";

    /// <summary>SPIKED: the book, once, under #1063's absence mark.</summary>
    public const string SpikedEntry =
        "The {Body} story did not run. The wire is one line shorter and only you know the shape of the hole.";

    /// <summary>ALTERED: the wire prints this under her byline.</summary>
    public const string AlteredStory =
        "{Body} reports an orderly quarter. Sources close to the site describe the workforce as 'accounted "
        + "for'. — R. Lind, for the wire";

    /// <summary>ALTERED: the book.</summary>
    public const string AlteredEntry = "Her story ran. It is your sentence and her name.";

    /// <summary>LATE: the next desk pulse, no credits.</summary>
    public const string LateLine = "No credits. One line where the money would be: 'Noted that you tried.'";

    /// <summary>After a SPIKED window, entering the gallery (once).</summary>
    public const string GoneLine =
        "She is not at the table. The recorder is. Somebody will come back for it, or nobody will.";

    /// <summary>Every sentence this slice can put on a screen, for the canon sweeps.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return RowLabel;
        yield return RowLine;
        yield return TakenLine;
        yield return AtHerTableLine;
        yield return TakeThePagesLabel;
        yield return TakeThePagesLine;
        yield return TookThePagesEntry;
        yield return LeaveYourPageLabel;
        yield return LeaveYourPageLine;
        yield return SpikedEntry;
        yield return AlteredStory;
        yield return AlteredEntry;
        yield return LateLine;
        yield return GoneLine;
    }

    // ── THE LINES, FILLED ───────────────────────────────────────────────────────────────────────────────

    private static string Cr(int credits) => credits.ToString("N0", CultureInfo.InvariantCulture) + " cr";

    /// <summary>The desk row for a body and a purse.</summary>
    public static string Row(string bodyName, int purse) =>
        RowLine.Replace("{Body}", bodyName, StringComparison.Ordinal).Replace("{cr}", Cr(purse), StringComparison.Ordinal);

    /// <summary>The pulse when the terms arrive.</summary>
    public static string Taken(string bodyName) => TakenLine.Replace("{Body}", bodyName, StringComparison.Ordinal);

    /// <summary>The book's entry when the pages are taken, with the watches until the window.</summary>
    public static string TookThePages(int watches) =>
        TookThePagesEntry.Replace("{N}", watches.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    /// <summary>The book's entry when nothing printed.</summary>
    public static string Spiked(string bodyName) => SpikedEntry.Replace("{Body}", bodyName, StringComparison.Ordinal);

    /// <summary>The sentence the wire prints under her byline when the client's page went in.</summary>
    public static string Altered(string bodyName) => AlteredStory.Replace("{Body}", bodyName, StringComparison.Ordinal);

    /// <summary>How many watches off the window is, rounded up — a window an hour away is one watch off, never
    /// nought, and a passed window is nought.</summary>
    public static int WatchesUntil(double storyAt, double now) =>
        now >= storyAt ? 0 : (int)Math.Ceiling((storyAt - now) / PatronRota.WatchSeconds);

    // ── THE PURSE ───────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How many of her fare the client pays for a story that does not run. Tuning, not canon: the
    /// canon names the purse only as <c>{cr}</c>.</summary>
    public const int FaresInThePurse = 2;

    /// <summary>The purse, {cr}: what somebody pays if her story does not run.</summary>
    public static int Purse(int herFare) => Math.Max(0, herFare) * FaresInThePurse;

    /// <summary>What the desk pays at the next pulse after the window. Full if it did not run; half if it ran
    /// different; nothing at all if it ran as she wrote it.</summary>
    public static int Pays(Outcome outcome, int purse) => outcome switch
    {
        Outcome.Spiked => purse,
        Outcome.Altered => purse / 2,
        _ => 0,
    };

    // ── WHERE SHE WRITES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The berth whose gallery she writes in: the one berth that has one.</summary>
    public const string Haven = ObservationWalk.HavenId;

    /// <summary>Her table, by its ordinal in the gallery's own list — the second of the two.</summary>
    public const int HerTable = 1;

    /// <summary>Seconds on her own clock she writes before she gets up, at the least and the most.</summary>
    public const double WritesAtLeastSeconds = 45.0;
    public const double WritesAtMostSeconds = 90.0;

    /// <summary>Seconds on her own clock she stands at the machine.</summary>
    public const double FeedsAtLeastSeconds = 6.0;
    public const double FeedsAtMostSeconds = 12.0;

    /// <summary>How long she writes between trips — seeded off her contract, so her cadence is her own and the
    /// same on every machine. Real seconds on the walker's own clock: a berth's sim clock does not run while the
    /// captain walks it.</summary>
    public static double WritesFor(string questId) =>
        Seeded($"spike:writes:{questId}", WritesAtLeastSeconds, WritesAtMostSeconds);

    /// <summary>How long she stands at the machine.</summary>
    public static double FeedsFor(string questId) =>
        Seeded($"spike:feeds:{questId}", FeedsAtLeastSeconds, FeedsAtMostSeconds);

    private static double Seeded(string tag, double least, double most)
    {
        int span = (int)(most - least) + 1;
        return least + DiceRule.Roll(DiceRule.Seed(tag), span).Face - 1;
    }

    // ── THE TWO PAPERS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The client's page. The satchel id is this, a colon, and the body it is about — the one word the
    /// canon line needs filled, carried by the fact rather than stored as prose.</summary>
    public const string SwapId = "press-swap";

    /// <summary>Her pages.</summary>
    public const string PagesId = "press-pages";

    /// <summary>Is this paper the client's page?</summary>
    public static bool IsTheSwap(string? paperId) =>
        paperId is not null
        && (string.Equals(paperId, SwapId, StringComparison.Ordinal)
            || paperId.StartsWith(SwapId + ":", StringComparison.Ordinal));

    /// <summary>Is this paper her pages?</summary>
    public static bool IsThePages(string? paperId) => string.Equals(paperId, PagesId, StringComparison.Ordinal);

    /// <summary>The client's page, about this body.</summary>
    public static Satchel.Item TheSwap(string bodyName) => new(Satchel.Kind.Paper, $"{SwapId}:{bodyName}");

    /// <summary>Her pages.</summary>
    public static Satchel.Item ThePages() => new(Satchel.Kind.Paper, PagesId);

    /// <summary>The client's page's body, rebuilt from its id: the line it arrived with.</summary>
    public static string SwapText(string paperId) =>
        Taken(paperId.StartsWith(SwapId + ":", StringComparison.Ordinal) ? paperId[(SwapId.Length + 1)..] : "");

    /// <summary>The client's page's title: the opening words of the line it arrived with, cut at its colon.
    /// FLAGGED in the PR for the canon pass — a title the page was never given.</summary>
    public static string SwapTitle => TakenLine[..TakenLine.IndexOf(':', StringComparison.Ordinal)];

    /// <summary>Her pages' title: the opening words of the move's own line, cut at its second comma. FLAGGED in
    /// the PR, like the tin's.</summary>
    public static string PagesTitle =>
        TakeThePagesLine[..TakeThePagesLine.IndexOf(", the top", StringComparison.Ordinal)];

    /// <summary>Is this one of the two papers this slice authored?</summary>
    public static bool IsAuthored(string? paperId) => IsTheSwap(paperId) || IsThePages(paperId);

    /// <summary>The page's body, for either paper.</summary>
    public static string Document(string paperId) => IsThePages(paperId) ? TakeThePagesLine : SwapText(paperId);

    /// <summary>The page's title, for either paper.</summary>
    public static string Title(string paperId) => IsThePages(paperId) ? PagesTitle : SwapTitle;

    // ── THE TWO MOVES ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>The first move's id.</summary>
    public const string TakeThePages = "gallery:take-the-pages";

    /// <summary>The second move's id.</summary>
    public const string LeaveYourPage = "gallery:leave-your-page";

    /// <summary>
    /// <b>HER TABLE'S CARD, WITH A MOVE OR WITHOUT.</b> The card the captain sat down to, plus one of the two moves
    /// before its last — exactly one, or none. Every other move is left as it was, including a move another
    /// feature put there. Idempotent both ways; absent, never greyed.
    /// </summary>
    public static Encounter.Scene TheTable(Encounter.Scene table, string? move)
    {
        IReadOnlyList<Encounter.Move> had = table.Moves ?? [];
        var moves = new List<Encounter.Move>(had.Count + 1);
        foreach (Encounter.Move m in had)
        {
            if (!IsOurs(m.Id))
            {
                moves.Add(m);
            }
        }

        if (move is not null && moves.Count > 0)
        {
            moves.Insert(moves.Count - 1, move == TakeThePages
                ? new Encounter.Move(TakeThePages, TakeThePagesLabel, Says: TakeThePagesLine)
                : new Encounter.Move(LeaveYourPage, LeaveYourPageLabel, Says: LeaveYourPageLine));
        }

        return table with { Moves = moves };
    }

    /// <summary>Which of the two moves this card carries, or null.</summary>
    public static string? Offers(Encounter.Scene scene)
    {
        foreach (Encounter.Move m in scene.Moves ?? [])
        {
            if (IsOurs(m.Id))
            {
                return m.Id;
            }
        }

        return null;
    }

    private static bool IsOurs(string id) =>
        string.Equals(id, TakeThePages, StringComparison.Ordinal) || string.Equals(id, LeaveYourPage, StringComparison.Ordinal);

    /// <summary>
    /// Which move is on offer at her table, if any: only while she is away from it and the captain sits there
    /// alone; TAKE THE PAGES while they are on her table untouched; LEAVE YOUR PAGE once they are in the
    /// captain's hands and he holds both them and the client's page.
    /// </summary>
    public static string? MoveOnOffer(Pages pages, bool sheIsAway, bool alone, bool holdsTheSwap, bool holdsThePages) =>
        !sheIsAway || !alone ? null
        : pages == SpikeIt.Pages.OnHerTable ? TakeThePages
        : pages == SpikeIt.Pages.Taken && holdsTheSwap && holdsThePages ? LeaveYourPage
        : null;

    // ── THE WINDOW ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Where her pages are.</summary>
    public enum Pages
    {
        /// <summary>On her table, as she left them.</summary>
        OnHerTable = 0,

        /// <summary>Taken: in the captain's satchel, or binned.</summary>
        Taken = 1,

        /// <summary>Back on her table, with the client's page in them.</summary>
        Swapped = 2,
    }

    /// <summary>What the window made of her story.</summary>
    public enum Outcome
    {
        /// <summary>The window has not come, or there is no contract.</summary>
        None = 0,

        /// <summary>She had no pages. Nothing prints.</summary>
        Spiked = 1,

        /// <summary>Her stack held the client's page. The client's sentence prints under her byline.</summary>
        Altered = 2,

        /// <summary>Her pages were on her table as she wrote them. Her story runs.</summary>
        Late = 3,
    }

    /// <summary>EXACTLY ONE OF THREE, decided by where her pages are at the window.</summary>
    public static Outcome AtTheWindow(Pages pages) => pages switch
    {
        SpikeIt.Pages.Swapped => Outcome.Altered,
        SpikeIt.Pages.Taken => Outcome.Spiked,
        _ => Outcome.Late,
    };

    /// <summary>Did a story print? The floor's reaction on the rags is only ever about a story that ran.</summary>
    public static bool Ran(Outcome outcome) => outcome is Outcome.Altered or Outcome.Late;

    // ── THE DEV START ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>What <c>spike=</c> asks for.</summary>
    public enum Cheat
    {
        /// <summary>Nothing asked.</summary>
        None,

        /// <summary><c>&amp;spike=1</c> — her story pending, the contract taken, she is at her table.</summary>
        Pending,

        /// <summary><c>&amp;spike=spiked</c> — the window passed with her pages in the captain's satchel.</summary>
        Spiked,
    }

    /// <summary>Read <c>spike=</c> off an address, the <see cref="CarryThePress.CheatIn"/> way.</summary>
    public static Cheat CheatIn(string? uri)
    {
        int q = uri?.IndexOf('?', StringComparison.Ordinal) ?? -1;
        if (uri is null || q < 0)
        {
            return Cheat.None;
        }

        foreach (string pair in uri[(q + 1)..].Split('&', '#'))
        {
            if (pair.StartsWith("spike=", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair["spike=".Length..]).ToLowerInvariant() switch
                {
                    "1" or "true" or "yes" => Cheat.Pending,
                    "spiked" => Cheat.Spiked,
                    _ => Cheat.None,
                };
            }
        }

        return Cheat.None;
    }
}

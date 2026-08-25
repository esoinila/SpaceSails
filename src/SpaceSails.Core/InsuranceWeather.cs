using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// #973 · THE VOID'S WEATHER — the walking insurance men, as the thing the room talks about.
//
// OWNER RULING 2026-08-25: "a great way to have fun and sell the story, it could be the thing people
// talk about in the bars that unites them, a bit like talking about the weather on planet side."
//
// So this is not a new NPC and not a new card. It is EIGHT SENTENCES and the law that decides when
// one of them is in the air, and it sits beside `BarIntel` because it is the same idiom: what the
// room says when nobody was asked. The lines are Fable's, verbatim, and this file is the only place
// they exist — a second copy in a client file would be the first named bug class with a glass in it.
//
// WHAT THE WEATHER IS NOT. It is not intel: nothing here is worth coin, nothing opens a door, and
// hearing a line writes nothing into any book — with exactly one exception, the lapsed cousin, and
// even that one only writes when the captain already holds the fleet-day page and can therefore
// recognise the shape of what he is being told. Everything else is air.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// #973 · The eight overheard lines about Harlan Fess and the men like him, and the law that puts one of
/// them in a room. Pure and deterministic on the shared <see cref="DiceRule"/> — the thread, the station and
/// the visit — so a reloaded save walks into the same conversation.
/// </summary>
public static class InsuranceWeather
{
    // ── THE EIGHT ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One line of the weather: a stable id and the words. The id is what the retiring rule counts
    /// and what the save carries; the words are never stored, so an edited file can never make the room say
    /// something nobody wrote.</summary>
    /// <param name="Id">Stable across a save, a reload and a re-ordering of the pool.</param>
    /// <param name="Text">Fable's sentence, verbatim. The room adds a speaker and quotation marks around it
    /// and nothing else.</param>
    public readonly record struct Line(string Id, string Text);

    /// <summary>The id of the line about the cousin who let the premium lapse — the one line in the weather
    /// that can touch the arc. Named rather than indexed, because "line 5" is a fact about this list's order
    /// and the rule that files a note is a fact about that sentence.</summary>
    public const string LapsedCousinId = "the-lapsed-cousin";

    /// <summary>The eight, in the order Fable wrote them (OWNER RULING 2026-08-25 on #973).</summary>
    public static readonly IReadOnlyList<Line> Lines =
    [
        new("fess-got-to-me",
            "Fess get to you yet? He got to me. Basic. Don't tell my mother."),
        new("the-whole-concourse-twice",
            "The insurance man walked the whole concourse twice today. Somebody's dying, mark me."),
        new("what-is-he-writing-down",
            "I told him no three times and he wrote something down. What is he writing down?"),
        new("smiled-like-a-filing-cabinet",
            "Premium remembers, he says. Remembers what, I ask. He smiled like a filing cabinet."),
        new(LapsedCousinId,
            "My cousin lapsed. Woke up meaner and broker, just like the poster says. Still owes me forty."),
        new("never-remembers-a-face",
            "He never forgets a file and he never remembers a face. That's the job, I suppose."),
        new("set-your-watch-by-him",
            "You can set your watch by that man's rounds. The void takes appointments after all."),
        new("pitched-the-glass-a-policy",
            "Somebody stood him a drink once. He drank it and pitched the glass a policy."),
    ];

    /// <summary>The ninth line of the feature, and the only one the room says with its body: what happens
    /// after somebody says one of the eight out loud to everybody, over a round the captain stood.</summary>
    public const string RoomsReaction = "Half the room nods. The other half checks a pocket.";

    /// <summary>The sheet the lapsed cousin files under, when it files at all. One per thread — a second
    /// telling of the same story is the same story.</summary>
    public const string LapsedCousinSheetId = "weather:the-lapsed-cousin";

    /// <summary>The line's words, or null for an id this build does not know (an edited save, a rolled-back
    /// pool). A row nobody can read is dropped rather than thrown over — the vault's tolerance everywhere.</summary>
    public static string? TextOf(string? id)
    {
        foreach (Line line in Lines)
        {
            if (string.Equals(line.Id, id, StringComparison.Ordinal))
            {
                return line.Text;
            }
        }

        return null;
    }

    // ── THE TUNING (owner-tunable, named, never a literal at a call site) ───────────────────────────────

    /// <summary>How many times the captain can hear one line before it RETIRES for this thread. Three is the
    /// count at which a sentence stops being weather and starts being a script — the same instinct behind
    /// #664's cooled beats, applied to a room instead of a card.</summary>
    public const int RetireAfterHearings = 3;

    /// <summary>The die the weather rolls. Four faces, so the base chance and the ×3 both land on it exactly
    /// and neither has to be expressed as a fraction somebody could round differently.</summary>
    public const int Faces = 4;

    /// <summary>How many of those four faces bring him up on an ordinary visit: one in four bar visits, which
    /// is often enough to be the room's small talk and rare enough that a captain who docks twice in a row
    /// usually hears nothing.</summary>
    public const int TalkInFour = 1;

    /// <summary>…and the multiplier on a station Fess's rota actually walked this watch (#976,
    /// <see cref="NebulaRep.IsWorkingThisStation"/>). ×3, so three visits in four: the room talks about the
    /// man who walked through it. Clamped at <see cref="Faces"/>, so a future tuning cannot push the chance
    /// past certain and quietly stop being a roll.</summary>
    public const int FessIsHereWeight = 3;

    /// <summary>How many lines the bar card's <i>Overheard here</i> block holds. The block never grows: an
    /// insurance line takes a slot from the oldest thing said at this counter rather than being added to it.</summary>
    public const int BlockLines = 3;

    /// <summary>How many faces the weather has left to show. Zero means the weather is OVER for this
    /// thread — the room has said what it has to say — and the rule below stops rolling entirely.</summary>
    public static int Unretired(IReadOnlyDictionary<string, int>? heard) => UnretiredPool(heard).Count;

    // ── THE LAW ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Does the room bring him up on THIS visit, and which line does it use?
    ///
    /// <para>Four rules, in the order that makes each of them cheap to state:</para>
    /// <list type="number">
    /// <item><b>Never two visits running at the same station.</b> A room that says it every time you walk in
    /// is not weather, it is a jukebox. Decided on the station's OWN visit ordinal, so two calls at Ceres
    /// three months and nine ports apart are still consecutive visits to Ceres.</item>
    /// <item><b>The weather can be over.</b> Draw is WITHOUT REPLACEMENT from the unretired pool, and when the
    /// pool empties nothing rolls again for this thread.</item>
    /// <item><b>One roll for whether.</b> <see cref="TalkInFour"/> in <see cref="Faces"/>, multiplied by
    /// <see cref="FessIsHereWeight"/> on a station his rota walked this watch.</item>
    /// <item><b>One roll for which</b> — over the unretired pool only, so a line the captain has heard three
    /// times cannot be drawn at all.</item>
    /// </list>
    ///
    /// <para>AT MOST ONE LINE PER VISIT is not a rule this function enforces so much as a shape it has: it
    /// returns one line for a (station, visit) pair and the same one every time it is asked, so a caller that
    /// asks four times in one visit gets one conversation rather than four.</para>
    /// </summary>
    /// <param name="threadId">The universe's seed (<c>GameThreadInfo.Id</c>).</param>
    /// <param name="stationId">The body being visited — the same key <see cref="NebulaRep"/> keys his rota
    /// on, so "the room he walked" and "the room talking about him" can never mean two different rooms.</param>
    /// <param name="stationVisit">How many times this thread has visited THIS station, counting from zero.</param>
    /// <param name="fessIsHere">Whether his rota has him working this station this watch.</param>
    /// <param name="heard">How many times each line has been heard, this thread.</param>
    /// <param name="lastSaidAtVisit">The station visit ordinal a line last surfaced at here, or −1 for
    /// never.</param>
    /// <returns>The line's id, or null when the room talks about something else.</returns>
    public static string? Draw(
        string threadId,
        string stationId,
        int stationVisit,
        bool fessIsHere,
        IReadOnlyDictionary<string, int>? heard,
        int lastSaidAtVisit)
    {
        if (stationVisit < 0)
        {
            return null;
        }

        if (lastSaidAtVisit >= 0 && lastSaidAtVisit == stationVisit - 1)
        {
            return null;   // it was in the air last time you were here; today the room is on something else
        }

        IReadOnlyList<string> pool = UnretiredPool(heard);
        if (pool.Count == 0)
        {
            return null;   // the weather is over — the room has said what it has to say
        }

        ulong seed = DiceRule.Seed($"insurance-weather:{threadId}|{stationId}", stationVisit);
        if (!RoomWouldTalk(seed, fessIsHere))
        {
            return null;
        }

        int face = DiceRule.Roll(DiceRule.Seed(seed, "insurance-weather-which"), pool.Count).Face;
        return pool[face - 1];
    }

    /// <summary>The whether-half of <see cref="Draw"/>, on its own so the ×3 can be proved statistically
    /// without a station, a pool or a book anywhere near it.</summary>
    public static bool RoomWouldTalk(ulong seed, bool fessIsHere)
    {
        int weight = Math.Clamp(TalkInFour * (fessIsHere ? FessIsHereWeight : 1), 0, Faces);
        return DiceRule.Roll(DiceRule.Seed(seed, "insurance-weather-talk"), Faces).Face <= weight;
    }

    /// <summary>The lines that have not yet been heard <see cref="RetireAfterHearings"/> times, in pool
    /// order. The draw's whole bag.</summary>
    private static IReadOnlyList<string> UnretiredPool(IReadOnlyDictionary<string, int>? heard)
    {
        var pool = new List<string>(Lines.Count);
        foreach (Line line in Lines)
        {
            int times = 0;
            _ = heard?.TryGetValue(line.Id, out times);
            if (times < RetireAfterHearings)
            {
                pool.Add(line.Id);
            }
        }

        return pool;
    }

    // ── HOW THE ROOM SAYS IT ───────────────────────────────────────────────────────────────────────────

    /// <summary>The line as it is heard: a name, and the sentence in the room's own quotation marks. One
    /// formatter, so the block, the round's shared topic and the sheet the cousin files cannot come to three
    /// different renderings of one overheard sentence.</summary>
    public static string AsHeard(string speaker, string text) => $"{speaker}: “{text}”";

    /// <summary>…and the same with the counter's own listening glyph, which is what the <i>Overheard here</i>
    /// block draws.</summary>
    public static string Overheard(string speaker, string text) => $"\U0001F442 {AsHeard(speaker, text)}";

    /// <summary>
    /// THE BLOCK, WITH THE WEATHER IN IT. <paramref name="book"/> is what this counter has actually said to
    /// the captain, newest first, already cut to <see cref="BlockLines"/>. When a line is in the air it takes
    /// a slot — the oldest one shown is the one it replaces — so the block STAYS three lines and never grows.
    ///
    /// <para>Where in the three it lands is seeded rather than fixed, because a room whose small talk is
    /// always the top line is a room reading from a rota.</para>
    /// </summary>
    public static IReadOnlyList<string> Block(
        IReadOnlyList<string>? book, string? weatherLine, ulong seed)
    {
        var shown = new List<string>(BlockLines);
        foreach (string line in book ?? [])
        {
            if (shown.Count >= BlockLines)
            {
                break;
            }

            shown.Add(line);
        }

        if (weatherLine is null)
        {
            return shown;
        }

        // One slot for the weather: keep the newest two and drop whatever the counter said longest ago.
        while (shown.Count >= BlockLines)
        {
            shown.RemoveAt(shown.Count - 1);
        }

        int slot = DiceRule.Roll(DiceRule.Seed(seed, "insurance-weather-slot"), shown.Count + 1).Face - 1;
        shown.Insert(slot, weatherLine);
        return shown;
    }

    /// <summary>
    /// THE ROOM'S SHARED TOPIC. A round for the room loosens tongues (<see cref="RoundTips"/>), and what a
    /// loosened room talks about is the weather: one named regular says the line out loud to everybody, and
    /// the whole block becomes that and what the room does about it.
    /// </summary>
    public static IReadOnlyList<string> SharedTopic(string speaker, string text) =>
        [Overheard(speaker, text), RoomsReaction];

    // ── THE ONE PLACE THE WEATHER TOUCHES THE ARC ──────────────────────────────────────────────────────

    /// <summary>
    /// Does hearing this line write anything down?
    ///
    /// <para>Once, for one sentence, under one condition. The cousin who lapsed is the only line in the eight
    /// that is about the filing line (#974) rather than about a salesman, and a captain who is holding the
    /// fleet-day page — the one page the service filed and no rebirth can grey — is the only captain who
    /// knows what he is being told. Anybody else hears a man complaining about forty credits.</para>
    ///
    /// <para>Everything else in the weather is air, deliberately: a room whose small talk filled the black
    /// book would stop being small talk by the third glass.</para>
    /// </summary>
    /// <param name="lineId">The line that was heard.</param>
    /// <param name="holdsTheFleetDayPage">Whether <c>OldCrewScene.SummerPartyId</c> is already in the
    /// book.</param>
    public static bool FilesANote(string? lineId, bool holdsTheFleetDayPage) =>
        holdsTheFleetDayPage && string.Equals(lineId, LapsedCousinId, StringComparison.Ordinal);

    /// <summary>Whose memory the cousin's sheet is: the man who told it, not the captain. He was there and
    /// the captain was not, which is the whole of what <see cref="HeldMemory.Mark.His"/> means.</summary>
    public const HeldMemory.Mark LapsedCousinMark = HeldMemory.Mark.His;

    /// <summary>…and which theory it serves. A premium that ran out and left a man meaner and broker is a
    /// story about money, whatever else it is about.</summary>
    public const HeldMemory.Theory LapsedCousinTag = HeldMemory.Theory.Money;
}

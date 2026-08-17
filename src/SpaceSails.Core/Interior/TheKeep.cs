using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #781 · THE KEEP AT CANTEEN 1 — an apron over a post.
///
/// <para>Owner, live at the counter 2026-08-08: <i>"There should be a bar keep there (really like hidden
/// security guard)."</i></para>
///
/// <h3>What he IS, and what is never said</h3>
///
/// <para>The keep is the floor's security. <b>Nothing in this file says so, and nothing anywhere else may.</b>
/// He tends bar on the living watches and the counter serves itself on the dead ones (#772's reading stays
/// word for word — it was never the whole truth, only the small-hours one). He is the door policy in an
/// apron: the cage's real gate is forty metres from the cage, pouring coffee. #778's haulier — <i>"I can't
/// get in the cage — they know my face at the counter"</i> — is now literally true, and this is why. Not one
/// line joins the two.</para>
///
/// <para><b>TELLS, NEVER STATEMENTS</b> (#649's discipline). Three sentences about hands, a rag and a break
/// he does not take. Nobody in-world remarks on them, the game never explains them, and a captain who
/// notices has been told everything and shown nothing. A guard sweeps every string below for the words that
/// would state it (<c>TheKeepAtTheCounterTests</c>), because the one way to spend this whole design is to
/// write the sentence.</para>
///
/// <h3>Asking is being seen asking</h3>
///
/// <para>The rumours are the person-verbs #772 gated away arriving at B1 — and every one you ask is ALSO an
/// entry in somebody's log (#715's per-entity memory getting its B1 hook). Two writes, and both are books
/// rather than meters: the captain's own field book keeps <see cref="FieldNote"/>, and the keep's own
/// entry keeps <see cref="AskedEntry"/> — a machine id, never a sentence, because nothing in the game may
/// show the player the keep's side of the ledger. On a LATER watch the room can say it back
/// (<see cref="LeakBark"/>), in a background patron's mouth, with the topic you asked about. The keep is
/// never quoted saying he talked.</para>
///
/// <para>Pure Core data + pure per-watch arithmetic (repo agreement §9): no clock, no <see cref="Random"/>,
/// no world. The words are here; the fixture that serves them is <see cref="CounterService"/>.</para>
///
/// <para>KAAMOS never states it; the wintering hall never filed it. It is simply how a company town works.</para>
/// </summary>
public static class TheKeep
{
    /// <summary>What the card calls him. Lower case, in prose, and it is not a name — the counter's own
    /// plate stays the counter's, and nothing in this game ever gives him one.</summary>
    public const string Name = "the keep";

    /// <summary>The glyph the field note is filed under. A glass, because the book records where you were
    /// standing and never what the man behind it is.</summary>
    public const string Glyph = "🍺";

    /// <summary>Who the per-entity entry is written against. An id in a book the player never sees — the
    /// ledger keys on it exactly as it keys on a fence or a fixer, and it is deliberately not a display
    /// name a panel could print.</summary>
    public const string LedgerId = "THE KEEP";

    /// <summary>What he says when you lean on the counter on a watch he is working.</summary>
    public const string Welcome = "“Coffee's on. Whatever else you're after, say it once.”";

    /// <summary>
    /// THE THREE TELLS — one per watch, and the whole of the characterisation.
    ///
    /// <para>Rotated by the WATCH and never by a wall clock (<see cref="TellAt"/>), so the sentence a captain
    /// is reading is a fact about the shift they walked into and does not flicker while they stand there.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> Tells =
    [
        "He is looking at your hands when you put them on the counter, and then he is not.",
        "The rag goes over the same dry patch it went over when the hauliers came in.",
        "He has not taken a break since the cage started moving. You notice because you were counting.",
    ];

    /// <summary>
    /// WHAT HE HAS HEARD — rotated on <see cref="Barkeep.RumorAt"/>'s existing hourly idiom, like every
    /// other keep in the game. Three, and each one points at somewhere the captain can already walk.
    /// </summary>
    public static readonly IReadOnlyList<string> Rumors =
    [
        "“Somebody upstairs is paying overtime on the freight side. Nobody upstairs is paying for coffee.”",
        "“The round comes past here twice an hour, near enough. I don't set my watch by it. I could.”",
        "“There's a fellow in the ring offices who's been in the same chair since Thursday. Somebody should "
        + "tell him the park doesn't close.”",
    ];

    /// <summary>What each rumour is ABOUT, in the words the room would use — the subject the leak later
    /// carries. Same order as <see cref="Rumors"/>, and a guard pins that they stay the same length: a
    /// fourth rumour with no topic beside it would file an ask about nothing.</summary>
    public static readonly IReadOnlyList<string> Topics =
    [
        "the freight side",
        "the round",
        "the ring offices",
    ];

    /// <summary>What the captain's own book keeps of having asked. The book records what happened and never
    /// what it might mean — and what happened is that the answer came without a look, which is the tell the
    /// captain wrote down without knowing they had.</summary>
    public const string FieldNote =
        "You asked the keep. He answered without looking up, and without needing to.";

    /// <summary>…and what the room says back on a later watch, in somebody else's mouth. The keep is never
    /// quoted saying he talked, and nothing anywhere says how it got here.</summary>
    public static string LeakBark(string topic) =>
        $"“Word at the counter is you were asking about {topic}.”";

    // ── WHICH WATCHES HE WORKS ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IS THIS A WATCH WITH SOMEBODY BEHIND THE COUNTER?
    ///
    /// <para>THE HALL'S OWN PREDICATE AND NEVER A SECOND ONE: <see cref="SittingAlone.BusyAt"/> over
    /// <see cref="SittingAlone.Fill"/>, which is the same threshold that decides whether a silence at a table
    /// is the room's or the room's indifference. A counter is staffed when there is a room to staff it for,
    /// and the six numbers that say how full the hall is are already authored
    /// (<see cref="CanteenRegulars.WatchFill"/>). Both answers are reachable on the watches the game actually
    /// has — four living, two dead — which a guard measures rather than trusting this sentence.</para>
    /// </summary>
    public static bool KeptWatch(long watch) => SittingAlone.Fill(watch) >= SittingAlone.BusyAt;

    /// <summary>The tell this watch wears. Indexed off the watch alone — never the sim clock, never
    /// <see cref="DateTime"/> — so the second line of the card is the shift and not the second hand.</summary>
    public static string TellAt(long watch) =>
        Tells[(int)(((watch % Tells.Count) + Tells.Count) % Tells.Count)];

    /// <summary>What the rumour at <paramref name="rumorIndex"/> is about, or null where the index names no
    /// rumour of his (a haven bar's keep, whose rumours are somebody else's).</summary>
    public static string? TopicOf(int rumorIndex) =>
        rumorIndex >= 0 && rumorIndex < Topics.Count ? Topics[rumorIndex] : null;

    // ── THE ENTRY IN SOMEBODY ELSE'S BOOK ─────────────────────────────────────────────────────────────
    //
    // #715, arriving as a LINE IN A BOOK rather than as a meter — the same shape #798's rip files and #804's
    // escort files, and for the same reason: a meter would be the announcement the canon rules out. Nothing
    // reacts on the beat. Somebody simply knows, and a shift later the room might say so.
    //
    // AND IT IS AN ID, NOT A SENTENCE. The captain's book gets prose because the captain wrote it; the keep's
    // book gets "counter-ask:1:7" because no panel in this game may ever render the keep's side of it. A
    // string a player could read is a string somebody eventually prints.

    /// <summary>The head of every entry this feature writes, so a reader of a save can see whose it is.</summary>
    public const string AskTag = "counter-ask";

    /// <summary>One ask, as the keep's book keeps it: which topic, and which watch it was asked on. The watch
    /// is IN the entry because "later" is the whole of the consequence — a leak that could surface on the
    /// same shift would be the keep answering you and telling the room in one breath.</summary>
    public static string AskedEntry(string? topic, long watch)
    {
        int at = -1;
        for (int i = 0; i < Topics.Count; i++)
        {
            if (string.Equals(Topics[i], topic, StringComparison.Ordinal))
            {
                at = i;
                break;
            }
        }
        return $"{AskTag}:{at}:{watch}";
    }

    /// <summary>…and reading one back. False for anything that is not one of ours, which is every other tell
    /// the ledger holds (#306's slips over a drink share the same list).</summary>
    public static bool TryReadAsk(string? entry, out int topicIndex, out long watch)
    {
        topicIndex = -1;
        watch = 0;
        if (entry is null)
        {
            return false;
        }
        string[] parts = entry.Split(':');
        return parts.Length == 3
            && string.Equals(parts[0], AskTag, StringComparison.Ordinal)
            && int.TryParse(parts[1], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out topicIndex)
            && topicIndex >= 0 && topicIndex < Topics.Count
            && long.TryParse(parts[2], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out watch);
    }

    /// <summary>How many faces of the house d20 have the room saying it back on a given later watch. Under
    /// half, so it is a thing that happens to you rather than a receipt that prints — <i>can</i> be a bark,
    /// in the canon's own word. FLAGGED for the owner's tuning.</summary>
    public const int FacesThatTalk = 8;

    /// <summary>
    /// DOES THE ROOM SAY IT BACK THIS WATCH, AND ABOUT WHAT?
    ///
    /// <para>Null unless a rumour was bought on an EARLIER watch than this one, and null on most watches even
    /// then. Deterministic in (site, watch) alone, so a captain cannot walk out and back in for a different
    /// answer, and a guard can walk watches to reach both outcomes instead of mocking a die.</para>
    ///
    /// <para>THE ASKS ARE SORTED BEFORE ANYTHING IS DRAWN FROM THEM. The ledger hands them over in the order
    /// they were appended, and this repo's fourth named bug class is one source consumed in the wrong order —
    /// a leak that depended on which rumour a captain happened to buy first would be a different sentence on
    /// two saves holding the same facts.</para>
    /// </summary>
    /// <param name="asked">Every tell the keep's ledger entry holds. Ours are read; everything else is
    /// skipped.</param>
    /// <param name="bodyId">The site, so two branch offices are two rooms.</param>
    /// <param name="watch">The watch being walked into — the frozen one (#709), never a clock.</param>
    public static string? LeakedTopic(IEnumerable<string>? asked, string bodyId, long watch)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (asked is null)
        {
            return null;
        }

        var earlier = new List<int>();
        foreach (string entry in asked)
        {
            if (TryReadAsk(entry, out int topic, out long askedOn) && askedOn < watch && !earlier.Contains(topic))
            {
                earlier.Add(topic);
            }
        }
        if (earlier.Count == 0)
        {
            return null;
        }
        earlier.Sort();

        if (DiceRule.Roll(DiceRule.Seed($"counter:leak:{bodyId}", watch), DiceRule.D20).Face > FacesThatTalk)
        {
            return null;
        }
        int which = DiceRule.Roll(DiceRule.Seed($"counter:leak:which:{bodyId}", watch), earlier.Count).Face - 1;
        return Topics[earlier[which]];
    }

    /// <summary>Every line this file can put on a screen, for the canon grep. A method rather than a list
    /// somebody has to remember to add to — #709's discipline, and the reason the sweep does not go stale.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return Name;
        yield return Welcome;
        foreach (string tell in Tells)
        {
            yield return tell;
        }
        foreach (string rumor in Rumors)
        {
            yield return rumor;
        }
        foreach (string topic in Topics)
        {
            yield return topic;
            yield return LeakBark(topic);
        }
        yield return FieldNote;
    }
}

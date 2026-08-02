using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #603 · THE SATCHEL — what the captain is carrying on foot, and the one verb they can do with it.
///
/// <para>Owner: <i>"since we have authority cards and a locked door, we should have some option to try use
/// those at the locked doors... maybe we need like on-site-carried-items inventory or something we can try
/// different keys against different locks. The captains ledger has the ship stuff but we should have
/// something similar on foot."</i></para>
///
/// <h3>The gap this closes</h3>
/// <para>The captain already picks things up out there — authority cards, files on people, operational paper,
/// ammunition — and none of it was a thing they HELD. The cards lived in a string set the player never saw;
/// papers became a lead the game granted on their behalf; rounds were a number. The game had possessions and
/// no pockets.</para>
///
/// <h3>The verb is TRY</h3>
/// <para>Not "use", not "solve". The captain offers something to whatever is in front of them and the game
/// answers <b>definitely</b>: it worked, or it did not and here is why. That is what keeps this from becoming
/// a puzzle, and it is why it does not overturn #590's third call (<i>you have the card or you do not</i>) —
/// what changes is that trying becomes an act the player performs rather than something the engine does
/// silently on their behalf.</para>
///
/// <para><b>Every refusal names a reason.</b> A silent nothing is indistinguishable from a bug, and this
/// ground has shipped that mistake twice in a week.</para>
///
/// <para>Pure and deterministic, like everything else in Core: the same satchel offered to the same thing
/// gives the same answer, always.</para>
/// </summary>
public static class Satchel
{
    /// <summary>What kind of thing this is. The kind decides what it can be offered TO — a card is not a
    /// clue and a clue is not ammunition, and a satchel that let you try anything on anything would be a
    /// puzzle box rather than a pocket.</summary>
    public enum Kind
    {
        /// <summary>#590 · A countersigned authority. Runs one shaft of one facility.</summary>
        Authority,

        /// <summary>Operational paper worth reading as a lead — the thing that lights the tracker, once the
        /// captain decides it means something.</summary>
        Paper,

        /// <summary>Loose rounds. Owner: <i>"sometimes we find like 6 rounds ... and take them"</i>.</summary>
        Rounds,

        /// <summary>A file on somebody. Leverage, and the one thing here that is spent on a PERSON rather
        /// than on a door — so it is carried, and it is never "tried" against anything down a corridor.</summary>
        Dirt,

        /// <summary>#614 · A record of something too big to lift. Owner: <i>"like finding a massive collar
        /// designed for Cthulhu's neck :D"</i>
        ///
        /// <para>Appended deliberately, never inserted: the ordinal is what a saved satchel stores, so
        /// putting a new kind in the middle would silently reinterpret every item in every existing
        /// vault.</para>
        ///
        /// <para>A relic is not a key and is never offered to a door. It buys nothing and opens nothing. It
        /// is carried because a captain who has seen it is a different captain, and because the day somebody
        /// finally wants to talk about what is down here, this is the thing on the table.</para></summary>
        Relic,
    }

    /// <summary>One thing in the pocket.</summary>
    /// <param name="Kind">What it is.</param>
    /// <param name="Id">The durable identity that rides in the vault. For an authority this is the card id
    /// (<c>UndergroundComplex.AuthorityCard.Id</c>); for the rest it is the seed tag it was minted from, so
    /// the prose can be rebuilt at read time rather than stored.</param>
    /// <param name="Count">How many, for things that stack (rounds). One for everything else.</param>
    public readonly record struct Item(Kind Kind, string Id, int Count = 1)
    {
        /// <summary>Written down as one field so a save carries the FACT and never the words. The prose is a
        /// seeded property of the world and would go stale the day the words changed.
        ///
        /// <para>ALWAYS three parts, even when the count is one. The first cut wrote <c>kind:id</c> for
        /// singles and <c>kind:count:id</c> for stacks, which cannot be told apart — because the ids
        /// themselves contain colons (<c>hive:luna:-3:7</c>), so "is the second field a count or the start of
        /// the id?" has no answer. A fixed shape, split at most twice, lets the id keep every colon it
        /// came with.</para></summary>
        public string Stored => $"{(int)Kind}:{Count}:{Id}";

        /// <summary>Read one back. Anything this build cannot parse is dropped rather than thrown over — the
        /// vault is tolerant everywhere else and a mystery object is not worth losing a game for.</summary>
        public static bool TryParse(string? stored, out Item item)
        {
            item = default;
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            string[] parts = stored.Split(':', 3);
            if (parts.Length != 3
                || !int.TryParse(parts[0], out int kind) || !Enum.IsDefined(typeof(Kind), kind)
                || !int.TryParse(parts[1], out int count) || count < 1
                || parts[2].Length == 0)
            {
                return false;
            }

            item = new Item((Kind)kind, parts[2], count);
            return true;
        }
    }

    /// <summary>#603 · IT IS A POCKET, NOT A WAREHOUSE. Capped so what is in it stays legible at a glance —
    /// the moment it needs sorting it has stopped being a satchel and become inventory management, which is
    /// a different game and not this one.
    ///
    /// <para>Stacks do not count against it: six rounds are one thing you are carrying.</para></summary>
    public const int Capacity = 12;

    /// <summary>Put something in. Stacking things merge; the pocket refuses politely when it is full, and
    /// the caller is expected to say so rather than swallow it.</summary>
    public static IReadOnlyList<Item> Add(IReadOnlyList<Item>? carried, Item item)
    {
        var list = new List<Item>(carried ?? []);
        if (item.Count < 1 || item.Id.Length == 0)
        {
            return list;
        }

        int at = list.FindIndex(i => i.Kind == item.Kind && string.Equals(i.Id, item.Id, StringComparison.Ordinal));
        if (at >= 0)
        {
            // Already carrying it. Stackables add up; a unique thing is simply already yours.
            list[at] = Stacks(item.Kind)
                ? list[at] with { Count = list[at].Count + item.Count }
                : list[at];
            return list;
        }

        if (list.Count >= Capacity)
        {
            return list;   // full. The caller says so; silently dropping a find would be the worse bug.
        }

        list.Add(item);
        return list;
    }

    /// <summary>Is the pocket too full to take one more distinct thing?</summary>
    public static bool IsFull(IReadOnlyList<Item>? carried) => (carried?.Count ?? 0) >= Capacity;

    /// <summary>Only rounds stack. A second copy of somebody's file is still one file to you.</summary>
    public static bool Stacks(Kind kind) => kind == Kind.Rounds;

    /// <summary>Take some out — spent rounds, a paper you have read, a card you handed over.</summary>
    public static IReadOnlyList<Item> Remove(IReadOnlyList<Item>? carried, Kind kind, string id, int count = 1)
    {
        var list = new List<Item>(carried ?? []);
        int at = list.FindIndex(i => i.Kind == kind && string.Equals(i.Id, id, StringComparison.Ordinal));
        if (at < 0)
        {
            return list;
        }

        if (list[at].Count > count)
        {
            list[at] = list[at] with { Count = list[at].Count - count };
        }
        else
        {
            list.RemoveAt(at);
        }
        return list;
    }

    /// <summary>How many of something the captain has.</summary>
    public static int CountOf(IReadOnlyList<Item>? carried, Kind kind, string? id = null)
    {
        int n = 0;
        foreach (Item i in carried ?? [])
        {
            if (i.Kind == kind && (id is null || string.Equals(i.Id, id, StringComparison.Ordinal)))
            {
                n += i.Count;
            }
        }
        return n;
    }

    /// <summary>Everything of one kind, in a stable order so a panel does not reshuffle itself.</summary>
    public static IReadOnlyList<Item> OfKind(IReadOnlyList<Item>? carried, Kind kind) =>
        (carried ?? []).Where(i => i.Kind == kind)
            .OrderBy(i => i.Id, StringComparer.Ordinal).ToList();
}

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

        /// <summary>#746 · A CHIT. Somebody wrote you onto a crew list, and this is the piece of paper that
        /// says so to a door.
        ///
        /// <para>Appended, never inserted, for <see cref="Relic"/>'s reason one line up: the ordinal is what a
        /// saved satchel stores.</para>
        ///
        /// <para>It is deliberately not an <see cref="Authority"/>. An authority is countersigned by an office
        /// and runs a shaft; a chit is a foreman's word, and the whole point of it is that it works on the
        /// strength of somebody vouching for you rather than on the strength of a stamp. They ride the same
        /// flat compartment because a wallet is where a person keeps both.</para></summary>
        Chit,

        /// <summary>#804 · A PERSONNEL PASS — the site's own card, with your face on it, saying you are one
        /// of the people who are supposed to be walking about in here.
        ///
        /// <para>Appended, never inserted, for the two reasons one line up: the ordinal is what a saved
        /// satchel stores.</para>
        ///
        /// <para>The third thing in the wallet and the third grammar. An <see cref="Authority"/> is an office
        /// vouching for a HOLE; a <see cref="Chit"/> is a foreman vouching for a SHIFT; a badge is the site
        /// vouching for a PERSON, which is the only one of the three a guard on a round has any use for.
        /// It runs no shaft and opens no gate — it answers a man, and nothing else.</para></summary>
        Badge,

        /// <summary>#763 · A PIECE OF KIT. Something you WORK, rather than something you show.
        ///
        /// <para>Appended, never inserted, for the third time and the same reason: the ordinal is what a
        /// saved satchel stores.</para>
        ///
        /// <para>The wallet's three grammars are all about being vouched for — an office, a foreman, a site.
        /// This is the fourth and it is the outlaw one: nobody vouches for anything and you brought a
        /// machine. It is BULKY, so it rides in the pockets proper and costs a captain room — that is
        /// <see cref="CompartmentOf"/>'s safe default, taken deliberately here rather than by omission,
        /// because the honest price of carrying a tool is not carrying something else.</para></summary>
        Tool,
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

    // ── #688 · THE SATCHEL HAS COMPARTMENTS, AND ONLY TWO OF THEM CAN FILL ──────────────────────────────
    //
    // Owner, live on the deep site: "Oh I run out of space in the inventory. How do I get Bigger pockets ...
    // lol... I find the good keycard but my pockets are full and I can not pocket it." And seconds later:
    // "I think bigger pockets for little papers."
    //
    // One number used to govern everything a captain could carry, and it was a number chosen for BULK. A
    // card is flat. A sheet of paper is flat. Twelve is right for rounds, crates and relic paperwork, and it
    // was quietly deciding that the best find in the game — the countersigned authority for the shaft under
    // your feet — could be refused because you were already carrying eleven shipping manifests.
    //
    // Nothing here loosens the design #603 was built on. It is still a pocket and not a warehouse; what is
    // in it is still legible at a glance; the pressure that makes "leave it here" a real decision is still
    // there, sitting on the things that actually take up room.

    /// <summary>Which part of the satchel a thing rides in. The compartment, not the kind, decides what it
    /// costs to carry — because what costs a captain room is BULK, and a card and a manifest have none.</summary>
    public enum Compartment
    {
        /// <summary>The wallet. Flat, thin, and never full: an authority always goes in.</summary>
        Wallet,

        /// <summary>The document sleeve — paper and files, both of them paper in the hand.</summary>
        Sleeve,

        /// <summary>The pockets proper: rounds, relic paperwork, and whatever bulky thing comes next.</summary>
        Pocket,
    }

    /// <summary>#603 · IT IS A POCKET, NOT A WAREHOUSE. Twelve bulky things, and it keeps every one of its
    /// teeth — this is the cap the whole "something has to be read, spent or left behind" pressure rests on.
    ///
    /// <para>Stacks do not count against it: six rounds are one thing you are carrying.</para></summary>
    public const int PocketCapacity = 12;

    /// <summary>#688 · The document sleeve. Owner: <i>"I think bigger pockets for little papers."</i> Paper is
    /// the thing this game hands out most and the thing that weighs least, and twenty-four sheets is still a
    /// number — a sleeve you never have to think about would have stopped being a sleeve.</summary>
    public const int SleeveCapacity = 24;

    /// <summary>Where a kind rides. Appending a new <see cref="Kind"/> puts it in the bulky pocket unless it
    /// is explicitly listed, which is the safe default: a new thing costs room until somebody decides it is
    /// flat.</summary>
    public static Compartment CompartmentOf(Kind kind) => kind switch
    {
        // #746 · A chit is a wallet card. Flat, thin, and the wallet never fills — the same ruling #688 made
        // for authorities, and for the same reason: the paper that gets you through a door is never the thing
        // an arithmetic about BULK should be allowed to refuse.
        // #804 · A badge is a wallet card too, and for the third time the same reason: the paper that gets
        // you past a person is never the thing an arithmetic about BULK should be allowed to refuse.
        Kind.Authority or Kind.Chit or Kind.Badge => Compartment.Wallet,
        Kind.Paper or Kind.Dirt => Compartment.Sleeve,
        _ => Compartment.Pocket,
    };

    /// <summary>How much a compartment holds. The wallet's answer is "as many as you find", stated as a
    /// number so every caller can do the same arithmetic and none of them needs a special case.</summary>
    public static int CapacityOf(Compartment where) => where switch
    {
        Compartment.Wallet => int.MaxValue,
        Compartment.Sleeve => SleeveCapacity,
        _ => PocketCapacity,
    };

    /// <summary>How many distinct things are in one compartment right now.</summary>
    public static int Used(IReadOnlyList<Item>? carried, Compartment where)
    {
        int n = 0;
        foreach (Item i in carried ?? [])
        {
            if (CompartmentOf(i.Kind) == where)
            {
                n++;
            }
        }
        return n;
    }

    /// <summary>Room left in one compartment. Never negative — a save written by an older build could be
    /// over a cap this one lowered, and "minus three spaces" is not a sentence.</summary>
    public static int SpaceLeft(IReadOnlyList<Item>? carried, Compartment where) =>
        where == Compartment.Wallet
            ? int.MaxValue
            : Math.Max(0, CapacityOf(where) - Used(carried, where));

    /// <summary>Is the part of the satchel this KIND rides in too full to take one more distinct thing?
    ///
    /// <para>#688 · It takes a kind now, and it has to: after the restructure "is the satchel full" has no
    /// answer. A pocket crammed with twelve pallets of relic paperwork is full for a round and empty for a
    /// manifest, and it is never full for a card.</para></summary>
    public static bool IsFull(IReadOnlyList<Item>? carried, Kind kind) =>
        SpaceLeft(carried, CompartmentOf(kind)) <= 0;

    /// <summary>#688 · What the satchel says about its own room, naming what it counts.
    ///
    /// <para>The old subtitle said <i>"N spaces left"</i> from one subtraction, and after the restructure that
    /// number does not exist: what is left depends on what you are about to pick up. A single figure standing
    /// for three compartments would be this repo's third named bug class in a subtitle — the sim doing one
    /// thing while a sentence reports another.</para></summary>
    public static string SpaceLine(IReadOnlyList<Item>? carried)
    {
        int sleeve = SpaceLeft(carried, Compartment.Sleeve);
        int pocket = SpaceLeft(carried, Compartment.Pocket);

        string papers = sleeve switch
        {
            0 => "The paper sleeve is full",
            1 => "Room for one more sheet in the paper sleeve",
            _ => $"Room for {sleeve} more sheets in the paper sleeve",
        };
        string bulk = pocket switch
        {
            0 => "the pockets will not take another thing",
            1 => "one space left in the pockets",
            _ => $"{pocket} spaces left in the pockets",
        };

        return $"{papers}, {bulk}. The wallet is flat and never fills — a card always goes in.";
    }

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

        if (IsFull(list, item.Kind))
        {
            // Full — the compartment THIS thing rides in, which is the only one that has any say. The caller
            // says so; silently dropping a find would be the worse bug.
            return list;
        }

        list.Add(item);
        return list;
    }

    /// <summary>#678 · WOULD THIS ONE GO IN? Asked BEFORE the room is turned over, because the answer decides
    /// whether the find is consumed at all.
    ///
    /// <para><see cref="Add"/> refuses politely and says so in a comment — <i>"the caller is expected to say
    /// so"</i> — and for a year no caller did: the Hive's haul path printed <i>"Into your pocket: an
    /// authority card"</i>, then called <c>Add</c>, then dropped the result on the floor at capacity with the
    /// room already marked emptied. The sentence claimed a possession the sim never granted, and the find was
    /// destroyed. Owner: <i>"If refused the item should stay where it was investigated last — not disappear
    /// like they do now, or seem to."</i></para>
    ///
    /// <para>It is not merely <c>!IsFull</c>: something you are ALREADY carrying always goes in, because
    /// <see cref="Add"/> merges it into the row that is already there and a full pocket is no obstacle to
    /// six more rounds of the kind you have. One law, so a caller can never ask the cheap question and get
    /// the expensive answer wrong.</para>
    ///
    /// <para>#688 · And an AUTHORITY always goes in, full stop. The owner's exact heartbreak — <i>"I find the
    /// good keycard but my pockets are full and I can not pocket it"</i> — is not a difficulty, it is the game
    /// throwing away its own best find because of an arithmetic that was never about cards.</para></summary>
    public static bool CanTake(IReadOnlyList<Item>? carried, Item item)
    {
        if (item.Count < 1 || item.Id.Length == 0)
        {
            return false;   // Add would refuse this outright, so nothing was ever going to enter the pocket.
        }

        return !IsFull(carried, item.Kind)
            || (carried ?? []).Any(i => i.Kind == item.Kind
                && string.Equals(i.Id, item.Id, StringComparison.Ordinal));
    }

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

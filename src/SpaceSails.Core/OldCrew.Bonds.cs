namespace SpaceSails.Core;

/// <summary>
/// #251 · §2 · THE HISTORY-BETWEEN TABLE, ROLLED FIRST — the seeded bonds for a thread, the bonds on offer
/// about a partner, the symmetry pass, and whether the classic happened.
///
/// <para>Split out of <c>OldCrew.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. Its one field is a <c>const</c>; <c>Pool</c>, the class's one initialised
/// static, stays in the opening file (#1163).</para>
/// </summary>
public static partial class OldCrew
{
    // ── §2 · THE HISTORY-BETWEEN TABLE, ROLLED FIRST ─────────────────────────────────────────────────

    /// <summary>
    /// One seeded shipmate: who they are, the bond they hold to another seeded shipmate, and — filled in by
    /// <see cref="Post"/> afterwards, never before — the berth they are posted to.
    /// </summary>
    /// <param name="Id">Which of the <see cref="Pool"/>.</param>
    /// <param name="ToCaptain">Their bond to the captain. Fixed: it is their role.</param>
    /// <param name="BondToId">The other seeded shipmate this one has history with.</param>
    /// <param name="Bond">What that history is.</param>
    /// <param name="StationId">The berth they work at — empty until the postings are rolled.</param>
    public readonly record struct Seeded(
        string Id, BondKind ToCaptain, string BondToId, BondKind Bond, string StationId)
    {
        /// <summary>The one line the black book shows beside this name from the first meeting — the bond to
        /// the captain and the bond to the other one, in the table's own words. The player, like a Fail
        /// Forward player, reads it BEFORE the scene and plays into it.</summary>
        public string History() =>
            IsTheClassic
                ? $"{OldCrew.Name(ToCaptain)} · now with {ById(BondToId)?.Short ?? BondToId}"
                : $"{OldCrew.Name(ToCaptain)} · {OldCrew.Name(Bond)}: {ById(BondToId)?.Name ?? BondToId}";

        /// <summary>THE CLASSIC (owner, addendum 2): the best friend ended up with the fling. It is the one
        /// bond the table is constrained to produce often, and it is the one the book announces in plain
        /// words rather than in the table's vocabulary — because <i>now with Ilse</i> is what a person would
        /// actually say, and it is what the captain would actually hear.</summary>
        public bool IsTheClassic =>
            (Id == BestFriendId && BondToId == FlingId && Bond == BondKind.TheFling)
            || (Id == FlingId && BondToId == BestFriendId && Bond == BondKind.TheBestFriend);
    }

    /// <summary>How many seedings in eight are forced to be THE CLASSIC — the fling and the best friend cast
    /// together and bound to each other. The owner's constraint is "at least one seeding in three"; this is
    /// the knob that buys it, and it is a deliberate roll rather than a hope, because a uniform cast draw
    /// lands the pair together in exactly three seedings in ten and would sit on the floor of the band.
    /// FLAGGED for the owner's tuning.</summary>
    public const int TheClassicInEight = 3;

    /// <summary>
    /// THE CAST AND THE BONDS, in that order and in one roll. The signer is always in it (owner ruling §8);
    /// the other three are drawn from the five living names beside him; and then every one of them is given
    /// a bond to another one of them.
    ///
    /// <para>Nothing here knows anything about berths. That is the whole of the bonds-before-postings law:
    /// this function cannot be influenced by where anybody works, because it is not told.</para>
    /// </summary>
    public static IReadOnlyList<Seeded> Bonds(string threadId)
    {
        ArgumentNullException.ThrowIfNull(threadId);

        // THE CLASSIC first, because it decides the cast as well as the bond: you cannot bind two people
        // who were not both invited.
        bool classic = DiceRule.Roll(DiceRule.Seed($"oldcrew|classic|{threadId}"), 8).Face <= TheClassicInEight;

        var cast = new List<string> { SignerId };
        var others = new List<string>();
        foreach (Shipmate s in Pool)
        {
            if (s.Living && s.Id != SignerId)
            {
                others.Add(s.Id);
            }
        }

        if (classic)
        {
            cast.Add(FlingId);
            cast.Add(BestFriendId);
            others.Remove(FlingId);
            others.Remove(BestFriendId);
        }

        // Draw the rest without replacement off the shared dice, one face at a time — the house idiom, and
        // the reason two threads with adjacent ids do not cast the same four people.
        int draw = 0;
        while (cast.Count < SeededPerThread && others.Count > 0)
        {
            int face = DiceRule.Roll(DiceRule.Seed($"oldcrew|cast|{threadId}", draw++), others.Count).Face;
            cast.Add(others[face - 1]);
            others.RemoveAt(face - 1);
        }

        // Keep the cast in POOL ORDER so a seeding reads the same however the dice happened to draw it.
        var ordered = new List<string>();
        foreach (Shipmate s in Pool)
        {
            if (cast.Contains(s.Id))
            {
                ordered.Add(s.Id);
            }
        }

        var seeded = new List<Seeded>();
        foreach (string id in ordered)
        {
            Shipmate who = ById(id)!.Value;

            // The classic is written in rather than rolled — that is what forcing it means. Both ends carry
            // it, so the book says the same thing whichever door the captain knocks on first.
            if (classic && id == BestFriendId)
            {
                seeded.Add(new Seeded(id, AsBond(who.Role), FlingId, BondKind.TheFling, ""));
                continue;
            }

            if (classic && id == FlingId)
            {
                seeded.Add(new Seeded(id, AsBond(who.Role), BestFriendId, BondKind.TheBestFriend, ""));
                continue;
            }

            // Everyone else: one bond to another seeded shipmate, both the partner and the kind rolled.
            var candidates = new List<string>();
            foreach (string other in ordered)
            {
                if (other != id)
                {
                    candidates.Add(other);
                }
            }

            string partner = candidates[
                DiceRule.Roll(DiceRule.Seed($"oldcrew|bond-who|{threadId}|{id}"), candidates.Count).Face - 1];
            IReadOnlyList<BondKind> kinds = BondsAvailableAbout(partner);
            BondKind kind = kinds[
                DiceRule.Roll(DiceRule.Seed($"oldcrew|bond-what|{threadId}|{id}"), kinds.Count).Face - 1];
            seeded.Add(new Seeded(id, AsBond(who.Role), partner, kind, ""));
        }

        return Symmetrical(seeded);
    }

    /// <summary>
    /// #973 L3 · <b>THE ONE WHO SIGNED IS ONE MAN.</b> The shipmate-to-shipmate bond names what the OTHER
    /// person is to this one (<see cref="Seeded.History"/> reads <i>the fling · the best friend: Teo</i>), so
    /// the bond <see cref="BondKind.Signed"/> is a sentence about the person it POINTS AT — and there is
    /// exactly one man in this world it can truthfully point at. Corwin Sallis signed the manifest the captain
    /// would not; a second name wearing that bond would be the book telling the player, in its own vocabulary,
    /// that somebody else did the thing the whole arc turns on.
    ///
    /// <para>Carried by the DRAW rather than by a post-hoc fix-up: a rule that rolled the wrong answer and
    /// then corrected it would put a bias nobody wrote into whatever kind it corrected TO. So the kind is
    /// simply not in the bag unless the man it is about is on the other end of the bond — and, because the
    /// bag is only short by one when he is not, a thread whose bond does point at him rolls exactly the roll
    /// it always rolled (the L5a carry-over the crew flagged; every other bond kind is legal about anybody).</para>
    /// </summary>
    public static IReadOnlyList<BondKind> BondsAvailableAbout(string? partnerId)
    {
        var kinds = new List<BondKind>();
        foreach (BondKind k in Enum.GetValues<BondKind>())
        {
            if (k == BondKind.Signed && !string.Equals(partnerId, SignerId, StringComparison.Ordinal))
            {
                continue;
            }

            kinds.Add(k);
        }

        return kinds;
    }

    /// <summary>
    /// THE CLASSIC IS A FACT ABOUT TWO PEOPLE, so it is written on both of them. The forced seeding writes
    /// both ends already; a free roll can land one end of it by luck (his bond comes up <i>the fling ·
    /// Ilse</i> and hers is pointed elsewhere), and a book that said <i>now with Ilse</i> on his page and
    /// something else entirely on hers would be the sim doing one thing while a SENTENCE reported another —
    /// which is a named bug class in this house, not a rough edge.
    /// </summary>
    private static IReadOnlyList<Seeded> Symmetrical(List<Seeded> seeded)
    {
        bool classic = false;
        foreach (Seeded s in seeded)
        {
            classic |= s.IsTheClassic;
        }

        if (!classic)
        {
            return seeded;
        }

        for (int i = 0; i < seeded.Count; i++)
        {
            if (seeded[i].Id == BestFriendId)
            {
                seeded[i] = seeded[i] with { BondToId = FlingId, Bond = BondKind.TheFling };
            }
            else if (seeded[i].Id == FlingId)
            {
                seeded[i] = seeded[i] with { BondToId = BestFriendId, Bond = BondKind.TheBestFriend };
            }
        }

        return seeded;
    }

    /// <summary>True when this thread's table produced the classic — the fling and the best friend cast
    /// together and bound to each other. Read off the rolled bonds rather than re-rolled, so there is one
    /// answer to the question in the whole game.</summary>
    public static bool TheClassicHappened(IReadOnlyList<Seeded> seeded)
    {
        ArgumentNullException.ThrowIfNull(seeded);
        foreach (Seeded s in seeded)
        {
            if (s.IsTheClassic)
            {
                return true;
            }
        }

        return false;
    }
}

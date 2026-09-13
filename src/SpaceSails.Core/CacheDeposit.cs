using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #319 · <b>THE MOON IS A SAFETY-DEPOSIT BOX.</b> What the shovel will take besides a chest.
///
/// <para>Owner, live 2026-07-18: <i>"Let's add another options to hide onto the planet / site. Anything from
/// the inventory that is light enough. I am thinking like a dead-drop-communication, burying compromising
/// evidence against some NPC as an insurance, burying infocard, hiding evidence of our piracy without losing
/// it. Maybe we steal something too important to risk carrying it around, like a superchip prototype
/// etc."</i></para>
///
/// <h3>ONE WEIGHT RULE, AND IT IS THE SATCHEL'S OWN ARITHMETIC</h3>
/// <para>The issue's own words for what this class must be: <i>"One weight threshold ('light enough to carry
/// down the tube'), one honest rule."</i> The tempting shape is a list of kinds — a card yes, a round yes, a
/// piece of kit no — and it is the wrong shape twice over. It would be a SECOND opinion about how heavy a
/// thing is, sitting beside the one the satchel has held since #688 (<i>"what costs a captain room is BULK,
/// and a card and a manifest have none"</i>) and sharpened by #798 into a per-ITEM number; and it would have
/// to be edited by hand every time a <c>Satchel.Kind</c> is appended, which is the maintenance shape of this
/// repo's fifth named bug class — a rule that silently stops covering the world.</para>
///
/// <para>So the threshold is stated in the satchel's own units and read off the satchel's own function:
/// <see cref="Satchel.SpaceCostOf"/>, the answer to <b>"what does this thing cost the compartment it rides
/// in"</b>. A thing the captain could carry down the tube in one place of one pocket is a thing the captain
/// can put in a hole. <b>There is no list of kinds in this file and there must never be one</b> —
/// <c>TheMoonIsASafetyDepositBoxTests</c> sweeps the source for it.</para>
///
/// <para><b>What the threshold excludes today: nothing, and that is the honest answer.</b> Everything this
/// game lets a captain carry on foot costs one place or none, because everything too big to lift is already
/// modelled as paperwork ABOUT the thing (<see cref="Satchel.Kind.Relic"/> — the collar measured for
/// something is measurements, not a collar). The satchel IS the set of things light enough to walk down a
/// tube with; that is what it has always been. The rule earns its keep the day something costs two places,
/// and on that day it refuses it without anybody remembering to.</para>
///
/// <h3>What a buried thing is</h3>
/// <para>It rides the SAME <see cref="TreasureCache"/> a chest rides — see <see cref="TreasureCache.Deposit"/>
/// — which is not an implementation shortcut but the whole of clause 2 and 3 of the issue. One record means
/// one ledger, one vault section, one rebirth, one <see cref="CacheSafety"/> read on the way out and one set
/// of #316 marks when a rival gets there first. A parallel "deposit ledger" would have been four more places
/// for the hole in the ground to disagree with itself.</para>
/// </summary>
public static class CacheDeposit
{
    // ── THE WEIGHT RULE BEGINS ───────────────────────────────────────────────────────────────────────
    //
    // TheMoonIsASafetyDepositBoxTests SLICES THE SOURCE BETWEEN THESE TWO FENCES and forbids the word
    // `Kind` inside them. That is the whole guard against this becoming a list of kinds, so the fences are
    // load-bearing: everything that DECIDES how heavy a thing is lives between them, and everything that
    // merely identifies a row lives below. Do not put a switch in here, and do not move the fence to make
    // room for one.

    /// <summary>#319 · <b>LIGHT ENOUGH TO CARRY DOWN THE TUBE</b>, in the satchel's own units: the most a
    /// thing may cost the compartment it rides in (<see cref="Satchel.SpaceCostOf"/>) and still go in a hole.
    ///
    /// <para>One place. A captain carried it out here in one place of one pocket, and a hole is not fussier
    /// than a pocket. It is a named constant rather than a literal at the call sites because it is the answer
    /// to <b>"how heavy is too heavy to bury"</b>, and this project has paid four times over for a fact
    /// transcribed at the places that ask it.</para></summary>
    public const int HeaviestBuriableSpaceCost = 1;

    /// <summary>#319 · The rule itself, asked of a COST rather than of a thing — which is the form every
    /// guard drives, because it is the only form that can be asked about a weight the world cannot build
    /// yet. A negative cost is not a lighter thing; it is a broken one, and it is refused.</summary>
    public static bool IsLightEnough(int spaceCostInTheSatchel) =>
        spaceCostInTheSatchel >= 0 && spaceCostInTheSatchel <= HeaviestBuriableSpaceCost;

    /// <summary>#319 · …and the same rule asked of a thing in the pocket. The ONE place a satchel row is
    /// weighed for the ground, and it weighs it with <see cref="Satchel.SpaceCostOf"/> and nothing else.</summary>
    public static bool IsLightEnough(Satchel.Item item) => IsLightEnough(Satchel.SpaceCostOf(item));

    /// <summary>#319 · Everything in this satchel the shovel would take, in the satchel's own order so the
    /// chooser does not reshuffle itself between two looks at the same pocket.</summary>
    public static IReadOnlyList<Satchel.Item> LightEnoughIn(IReadOnlyList<Satchel.Item>? carried)
    {
        var rows = new List<Satchel.Item>();
        foreach (Satchel.Item item in carried ?? [])
        {
            if (IsLightEnough(item))
            {
                rows.Add(item);
            }
        }
        return rows;
    }

    // ── THE WEIGHT RULE ENDS ─────────────────────────────────────────────────────────────────────────
    //
    // Below this line nothing decides how heavy anything is. What follows asks WHICH ROW a pick is, which is
    // the satchel's own identity for a row (kind AND id, exactly as Satchel.Remove matches one) and has
    // nothing to do with weight.

    /// <summary>#319 · Is this thing still in that pocket? Asked at the shovel, because the chooser's pick
    /// was made at the shuttle door and a whole excursion happens in between — a paper can be read and
    /// filed, a round can be fired, a folder can come apart. The hole takes what is actually in the hand.
    ///
    /// <para>Matched on kind AND id, which is <see cref="Satchel.Remove"/>'s own identity for a row, so the
    /// thing that goes in the ground is exactly the row that leaves the satchel. Never on COUNT: a stack is
    /// the same row whether it holds six rounds or two.</para></summary>
    public static bool StillCarried(IReadOnlyList<Satchel.Item>? carried, Satchel.Item picked)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind == picked.Kind
                && string.Equals(item.Id, picked.Id, System.StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>#319 · The row as it is actually in the pocket right now — the picked row's live count, so a
    /// captain who picked four rounds at the door and fired two buries two. Null when it is gone.</summary>
    public static Satchel.Item? AsCarried(IReadOnlyList<Satchel.Item>? carried, Satchel.Item picked)
    {
        foreach (Satchel.Item item in carried ?? [])
        {
            if (item.Kind == picked.Kind
                && string.Equals(item.Id, picked.Id, System.StringComparison.Ordinal))
            {
                return item;
            }
        }
        return null;
    }

    // ── WHAT IT SAYS ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>#319 · The chooser's row. A PLATE and not prose — it names the choice the way the coin dial
    /// and the hold row beside it name theirs, because a sentence there would be the boarding panel starting
    /// to have opinions about what the captain is up to, which is the one thing #313 built it not to do.</summary>
    public const string RowLabel = "BURY A THING FROM THE SATCHEL";

    /// <summary>
    /// #319 · <b>THE CAPTAIN'S REGISTER, SAID ONCE — at the DIG HERE press, when the thing going in is not
    /// the chest.</b> Fable-authored canon, reproduced verbatim and pinned character for character.
    ///
    /// <para>It is the whole of what the game says about the act. There is no announcement that evidence has
    /// left the ship, no banner about an inspection that will now find nothing, no number: a captain who
    /// buries a file and is later waved through a search has to work out for himself which of those two
    /// facts caused the other. The line is a register — what a man tells himself while he digs — and not a
    /// report.</para>
    /// </summary>
    public const string NotCoinThisTime =
        "Not coin this time. Something that is safer in the ground than in the hold.";

    /// <summary>#319 · How a manifest names what is in the hole besides coin and cargo. Deliberately a COUNT
    /// and never the thing itself: the ledger row and the map card are read in a bar, over a captain's
    /// shoulder, and a hoard line that spelled out <i>a file on somebody</i> would be the one place in the
    /// game where burying evidence advertises the evidence. The captain knows what he put in.</summary>
    public static string ManifestLine(int things) =>
        things == 1 ? "1 thing from the satchel" : $"{things} things from the satchel";

    /// <summary>#319 · Every sentence this feature can put on a screen, for the audit that reads them all.
    /// Two, and one of them is a plate.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return RowLabel;
        yield return NotCoinThisTime;
    }
}

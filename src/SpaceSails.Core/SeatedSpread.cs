using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #784/#793 · PRIVACY GATES THE SPREAD — where a gumshoe will lay their papers out, and where they will not.
///
/// <para>Owner, over three sittings in one evening. First the venue: <i>"Also sitting down allows to process
/// the inventory at the table"</i> — and then, from a cabinet at his own table, pinning the canonical one:
/// <i>"that is the place I want to process inventory."</i> Then the counter-rule, with a reference:
/// <i>"The gumshoe does NOT organize papers at the bar. They take a shady back-room table — wall behind the
/// back, sight lines to whoever comes close"</i> (Han Solo in the Mos Eisley cantina). And the outdoor rung:
/// <i>"the park bench might be used to go through inventory info loot, if we get the whole bench to
/// ourselves to have some privacy."</i></para>
///
/// <h3>One predicate, four seats</h3>
///
/// <para>The exposure ladder was already priced by the room itself — a cabinet is the top nobody can walk to
/// (<see cref="SittingAlone.SomebodyComes"/> refuses on a quiet table), a counter stool is the seat whose
/// neighbour is by definition within arm's reach
/// (<see cref="Interior.TheStools.HasNeighbour"/>). This file does not invent a second ladder; it states
/// which rungs the SPREAD is allowed on, and it is deliberately one function so the day the answer grows a
/// clause, every caller gets it.</para>
///
/// <para>The refusal is SAID, never silent (#603): a control that quietly does nothing is indistinguishable
/// from a broken one, and this particular law — <i>where</i> you can work the case — is a thing a player has
/// to be able to learn by pressing it once at the wrong seat.</para>
///
/// <para>Pure: a predicate and the words. It has never heard of a table.</para>
/// </summary>
public static class SeatedSpread
{
    /// <summary>
    /// #784 · MAY THE CASE BE SPREAD OUT HERE?
    ///
    /// <para>A cabinet always; a hall table or a park bench only while you have it to yourself; the bar desk
    /// never. <paramref name="alone"/> is the caller's own fact about the seat — the chair opposite being
    /// empty, the far end of the bench being empty, nobody on the next stool — and it is a single flag on
    /// purpose: what "alone" means at each kind of seat is a question about a room, and this file does not
    /// have one.</para>
    /// </summary>
    public static bool CanSpreadTheCase(SeatedHud.Seat seat, bool alone) => seat switch
    {
        // The door is what you paid for. Nothing can walk up, so nothing has to be put away — this is the
        // owner's canonical processing venue and it is unconditional by ruling.
        SeatedHud.Seat.Cabinet => true,

        // The whole bench, or nothing (#793). Somebody sitting down at the other end is not a wall between
        // you; it is a person reading your evidence over their own lunch.
        SeatedHud.Seat.ParkBench => alone,

        // A back table with the chair opposite empty. Revocable the moment somebody takes it, which is the
        // drama the issue is named after rather than an inconvenience.
        SeatedHud.Seat.HallTable => alone,

        // The gumshoe rule. Never, at any hour, with or without a neighbour: a bar top is in full view of
        // the keep — who is security (#781) — and of everyone waiting to be served. Working your case here
        // is working it under the friendliest surveillance on the floor.
        _ => false,
    };

    /// <summary>What each seat is told when the answer is no. One sentence, in the plainest words, and every
    /// one of them names WHAT WOULD FIX IT — a refusal a player cannot act on is a wall with a sign on
    /// it.</summary>
    public const string NotAtTheBarLine =
        "Not at the bar. Everything you put on this counter is read by the keep, by whoever is waiting to be " +
        "served, and by the man on the next stool. Take a table with a wall behind it.";

    /// <summary>…and at a table with somebody in the chair opposite.</summary>
    public const string NotWithCompanyLine =
        "Not with somebody sitting there. The papers stay in the sleeve while there is a face across the " +
        "table — you can work the case when the chair opposite is empty again.";

    /// <summary>…and on a bench you are sharing.</summary>
    public const string NotOnASharedBenchLine =
        "Not while you are sharing the bench. Half a bench is not a desk, and the other half can read.";

    /// <summary>The refusal, or null when the spread may open. Callers ask for the refusal rather than for
    /// permission, so a control that means to explain itself cannot forget to.</summary>
    public static string? RefusalAt(SeatedHud.Seat seat, bool alone)
    {
        if (CanSpreadTheCase(seat, alone))
        {
            return null;
        }
        return seat switch
        {
            SeatedHud.Seat.BarStool => NotAtTheBarLine,
            SeatedHud.Seat.ParkBench => NotOnASharedBenchLine,
            _ => NotWithCompanyLine,
        };
    }

    /// <summary>What the strip calls the act, where the control is drawn. The owner's own metaphor, kept
    /// quiet: <i>"we are digging for info and understanding."</i></summary>
    public const string SpreadLabel = "Work the case";

    /// <summary>The hint on that control while it is live.</summary>
    public const string SpreadHint =
        "Spread the satchel out and dig through it — the book keeps what you finish";

    /// <summary>What the spread page says over the rows, so a captain knows what the list IS before they
    /// press anything on it.</summary>
    public const string SpreadBlurb =
        "Papers out on the table. Pick one and dig: what it actually says goes in the book in your own hand, " +
        "and the sheet stays yours.";

    /// <summary>…and what it says when the sleeve has nothing in it worth digging through.</summary>
    public const string NothingToWorkLine =
        "Nothing in the sleeve to work. Rounds and relics are not evidence — paper is, and dirt on somebody " +
        "is.";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return NotAtTheBarLine;
        yield return NotWithCompanyLine;
        yield return NotOnASharedBenchLine;
        yield return SpreadLabel;
        yield return SpreadHint;
        yield return SpreadBlurb;
        yield return NothingToWorkLine;
    }

    /// <summary>Every seat this ladder knows, private end first — for guards that must sweep BOTH directions
    /// rather than sample one.</summary>
    public static IReadOnlyList<SeatedHud.Seat> Ladder { get; } =
        (SeatedHud.Seat[])Enum.GetValues(typeof(SeatedHud.Seat));
}

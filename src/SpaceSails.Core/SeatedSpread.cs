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
        // #821 · THE TOP RUNG. Owner, in the park: "the cubicle is the one place the spread ([I]) is ALWAYS
        // allowed — smaller than a bench, more private than an office. Half a bench is not a desk; a locked
        // cubicle absolutely is :-D"
        //
        // ALWAYS, and the word is load-bearing: `alone` is not read on this arm, because there is no version
        // of a locked cubicle with somebody else in it. Every other rung on this ladder is a question about
        // who else is in the room; this one is a question about a catch, and the captain has already
        // answered it. (It buys time and not safety — that is CubicleLock's law and it is about the door,
        // never about the papers: a man knocking cannot read them.)
        SeatedHud.Seat.LockedCubicle => true,

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

    /// <summary>
    /// #784 · THE SAME DOOR, ONCE THE CASE HAS BEGUN — the label, hint and blurb the controls switch to
    /// after the first sheet of this sitting is in the book.
    ///
    /// <para>Owner, mid-dig at his own table (2026-08-11): <i>"maybe it could say continue working on the
    /// case (select next item to work the case) etc something that indicates that we change the thing we
    /// look at."</i> He had just finished a sheet and wanted the next one, and the button still spoke as if
    /// nothing had happened — a control that reads the same before and after work is a control that cannot
    /// say the work moved. The words are the state (#603's rule pointed at a label): begun and not-begun
    /// are different facts, so they get different sentences.</para>
    /// </summary>
    public const string SpreadAgainLabel = "Back to the case — next sheet";

    /// <summary>The hint on that control once a sheet of this sitting is already in the book.</summary>
    public const string SpreadAgainHint =
        "The spread again: what you finished is in the book — pick the next sheet and dig";

    /// <summary>…and what the page says over the rows on a return visit, so the list reads as the case
    /// in progress rather than a fresh pile.</summary>
    public const string SpreadAgainBlurb =
        "Papers out again. What you finished is in the book — pick the next sheet and dig, or deal with " +
        "what is left.";

    /// <summary>…and what it says when the sleeve has nothing in it worth digging through.</summary>
    public const string NothingToWorkLine =
        "Nothing in the sleeve to work. Rounds and relics are not evidence — paper is, and dirt on somebody " +
        "is.";

    // ── #973 L3 · THE OTHER VERB THE TABLE HAS ───────────────────────────────────────────────────────
    //
    // Digging turns a sheet into a page of the book. RECONCILING lays two pages of the book against each
    // other, and all three things that can come of it are Fable's (`SpreadReconcile.AgreeLine` and its
    // two siblings). What is written here is only the affordance around them: what the section is called,
    // and what a row's press does.

    // FABLE: line needed — the blurb over the reconcile rows, in the register of SpreadBlurb above: what
    //   laying two papers together IS, in the captain's own voice, stopping before it says what any of it
    //   means. The standing-in sentence below is deliberately the plainest true statement of the act and
    //   nothing more, so nobody mistakes it for authored prose.
    /// <summary>What the reconcile section says over its rows. STANDING IN — see the marker above.</summary>
    public const string LayThemTogetherBlurb =
        "Lay two of them together and see whether they are talking about the same world.";

    /// <summary>What a row's press does, said on the row.</summary>
    public const string LayItWord = "🗂 lay it";

    /// <summary>…and what it says for a paper already lying on the table. Never disabled (#212/#603): the
    /// row says where the paper is, and pressing it again is simply nothing.</summary>
    public const string AlreadyLaidWord = "🗂 on the table";

    /// <summary>The hover behind that press.</summary>
    public const string LayItHint = "Lay this beside the last one and see whether they agree";

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return NotAtTheBarLine;
        yield return NotWithCompanyLine;
        yield return NotOnASharedBenchLine;
        yield return SpreadLabel;
        yield return SpreadHint;
        yield return SpreadBlurb;
        yield return SpreadAgainLabel;
        yield return SpreadAgainHint;
        yield return SpreadAgainBlurb;
        yield return NothingToWorkLine;
        yield return LayThemTogetherBlurb;
        yield return LayItWord;
        yield return AlreadyLaidWord;
        yield return LayItHint;
    }

    /// <summary>Every seat this ladder knows, private end first — for guards that must sweep BOTH directions
    /// rather than sample one.</summary>
    public static IReadOnlyList<SeatedHud.Seat> Ladder { get; } =
        (SeatedHud.Seat[])Enum.GetValues(typeof(SeatedHud.Seat));
}

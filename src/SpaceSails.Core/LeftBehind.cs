using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceSails.Core;

/// <summary>
/// #688 · LEAVE IT HERE. Owner, live on the deep site, one sentence: <i>"The keycard story is already big, but
/// no way to drop stuff."</i>
///
/// <para>The satchel had a verb for offering a thing and a verb for looking at it, and no verb at all for
/// putting one down. Every other pressure in the game pointed at that hole: the pocket refuses politely, the
/// haul line tells you something has to be <i>read, spent or left behind</i> — and there was nothing in the
/// interface that could leave a thing behind. The captain's only way to make room was to spend something.</para>
///
/// <h3>The law it obeys is #615's, and it is the only one that matters here</h3>
/// <para><b>Leaving never destroys.</b> What the captain sets down is a find again, lying where they put it,
/// and searching that spot offers it back. It is the same idiom #678 built for a pocket that could not take a
/// find — the room keeps it — turned from an accident into a decision.</para>
///
/// <h3>What it deliberately is not (v1)</h3>
/// <para>This store lives with the EXCURSION. Fly away from a site without something you set down on it and it
/// is gone with the walk — the world does not keep a ledger of every sheet of paper a captain has ever put on
/// a floor. That is a v1 call and the prose says so out loud rather than implying a permanence the sim does
/// not have; hardening it into the vault is a decision for the owner, not a thing to sneak in under a feel
/// pass. What it is NOT is a trapdoor: within the walk, the thing is exactly where you left it.</para>
///
/// <para>Pure and deterministic apart from the store's own mutation: the same spot, asked twice, answers the
/// same both times.</para>
/// </summary>
public sealed class LeftBehind
{
    private readonly Dictionary<string, List<Satchel.Item>> _at = new(StringComparer.Ordinal);

    /// <summary>Where a thing is lying: which floor (0 is the surface, negative is underground) and which
    /// ground square. A SQUARE and not a room, because a room stops being a place the moment its console is
    /// struck off — and a captain who put something down in a corridor would be told there was nowhere to put
    /// it down.</summary>
    public static string SpotKey(int floor, int squareX, int squareY) => $"{floor}:{squareX}:{squareY}";

    /// <summary>Set one thing down. Stacks merge, and the ground has no cap — it is the ground.</summary>
    public void Leave(string spot, Satchel.Item item)
    {
        ArgumentNullException.ThrowIfNull(spot);
        if (item.Count < 1 || item.Id.Length == 0)
        {
            return;
        }

        if (!_at.TryGetValue(spot, out List<Satchel.Item>? here))
        {
            _at[spot] = here = [];
        }

        int already = here.FindIndex(i => i.Kind == item.Kind
            && string.Equals(i.Id, item.Id, StringComparison.Ordinal));
        if (already >= 0)
        {
            here[already] = Satchel.Stacks(item.Kind)
                ? here[already] with { Count = here[already].Count + item.Count }
                : here[already];
            return;
        }

        here.Add(item);
    }

    /// <summary>Is anything lying here? Asked before the search verb does its own work, because what is at
    /// your feet is answered before what is in the walls.</summary>
    public bool AnythingAt(string spot) => At(spot).Count > 0;

    /// <summary>What is lying here, in the order it was set down.</summary>
    public IReadOnlyList<Satchel.Item> At(string spot) =>
        _at.TryGetValue(spot, out List<Satchel.Item>? here) ? here : [];

    /// <summary>What happened when the captain searched a spot they had left things on.</summary>
    /// <param name="Pocket">The satchel after picking up everything that would go in.</param>
    /// <param name="Taken">What went back into it.</param>
    /// <param name="StillThere">What did not fit and is STILL LYING THERE. Never destroyed — the whole point
    /// of this class, and the case that occurs the instant a captain leaves two things to make room for one.</param>
    public readonly record struct Recovery(
        IReadOnlyList<Satchel.Item> Pocket,
        IReadOnlyList<Satchel.Item> Taken,
        IReadOnlyList<Satchel.Item> StillThere);

    /// <summary>Pick up what is lying here, as much of it as the satchel will take. Whatever does not fit
    /// stays on the ground: this is the one operation in the game whose failure mode must leave the world
    /// exactly as it found it.</summary>
    public Recovery PickUp(string spot, IReadOnlyList<Satchel.Item>? carried)
    {
        ArgumentNullException.ThrowIfNull(spot);

        IReadOnlyList<Satchel.Item> pocket = carried ?? [];
        var taken = new List<Satchel.Item>();
        var stayed = new List<Satchel.Item>();

        foreach (Satchel.Item item in At(spot))
        {
            if (Satchel.CanTake(pocket, item))
            {
                pocket = Satchel.Add(pocket, item);
                taken.Add(item);
            }
            else
            {
                stayed.Add(item);
            }
        }

        if (stayed.Count == 0)
        {
            _at.Remove(spot);
        }
        else
        {
            _at[spot] = stayed;
        }

        return new Recovery(pocket, taken, stayed);
    }

    /// <summary>What the captain is told when they set a thing down. Said INSIDE the satchel dialog (#680):
    /// the satchel stays open, because you are making room for something.
    ///
    /// <para><paramref name="where"/> is the place in the captain's own words — <i>"on the floor of B3"</i>,
    /// <i>"on the regolith at your feet"</i>. The line names it, per the owner's ask, and then it is honest
    /// about the one thing a v1 drop cannot promise.</para></summary>
    public static string LeaveLine(string what, string where, bool gistFiled = false)
    {
        string line = $"🎒 {what} — set down {where}, in plain sight. Search this spot again and it is back " +
            "in your hands. Lift off this rock without it and it stays here with everything else this place " +
            "kept.";

        return gistFiled
            ? line + " You read it through before you put it down, and the gist is in the field book now — " +
                "the paper stays here; what it says comes with you."
            : line;
    }

    /// <summary>
    /// #688 · A DOCUMENT LEAVES ITS GIST BEHIND IN THE BOOK. Owner refinement, on the drop verb: leaving a
    /// paper files what it said before the paper leaves the pocket.
    ///
    /// <para>It is the honest reading of what putting a document down MEANS. A captain does not abandon a
    /// pay sheet without having looked at it; what they are discarding is the physical sheet, and the
    /// pressure it was costing them was always bulk. So the sleeve empties and the knowledge does not —
    /// which is the same law the field book was built on (#587: <i>a find that is shown once is a find that
    /// is lost</i>) applied to the moment the captain lets go of the paper.</para>
    ///
    /// <para>Documents ONLY. Rounds and relic paperwork leave with the plain line: there is no gist to a
    /// handful of ammunition, and a book entry for one would be a receipt.</para>
    ///
    /// <para>Null means "nothing to file", and the caller must not file an empty note.</para>
    /// </summary>
    public static string? GistOf(Satchel.Item item, string where)
    {
        ArgumentNullException.ThrowIfNull(where);

        if (Satchel.CompartmentOf(item.Kind) != Satchel.Compartment.Sleeve)
        {
            return null;
        }

        if (item.Kind == Satchel.Kind.Paper)
        {
            FieldClue.Certainty certainty = FieldClue.CertaintyOf(item.Id);
            return $"📋 {FieldClue.Title(item.Id)} — {FieldClue.Label(certainty)}, read and left {where}. " +
                FieldClue.Line(certainty);
        }

        // A file on somebody has no title of its own — it is a name and a posting and the years around them,
        // and the game has never printed a heading on one. What goes in the book is that you read it.
        return $"🗃 A file on somebody, read through and left {where}. You know what is in it now; the " +
            "folder is what you put down.";
    }

    /// <summary>What the ground says when the captain comes back for it. <paramref name="taken"/> and
    /// <paramref name="stillThere"/> are already in the captain's words.</summary>
    public static string FoundAgainLine(IReadOnlyList<string> taken, IReadOnlyList<string> stillThere)
    {
        ArgumentNullException.ThrowIfNull(taken);
        ArgumentNullException.ThrowIfNull(stillThere);

        if (taken.Count == 0)
        {
            return "🎒 It is exactly where you left it, and there is no room for it — the same pockets that " +
                $"sent it to the floor: {string.Join("; ", stillThere)}.";
        }

        string got = $"🎒 Where you left it, untouched: {string.Join("; ", taken)}. Back in your pockets.";
        return stillThere.Count == 0
            ? got
            : got + $" What will not fit is still lying there: {string.Join("; ", stillThere)}.";
    }
}

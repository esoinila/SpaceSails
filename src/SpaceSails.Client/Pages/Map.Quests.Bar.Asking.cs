using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #781 · ASKING IS BEING SEEN ASKING — owner's design: <i>"every rumor you ask the keep is ALSO an
/// entry in somebody's log."</i> The keep's own tell, and the room saying it back to you later.
///
/// <para>Split out of <c>Map.Quests.Bar.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── #781 · ASKING IS BEING SEEN ASKING ───────────────────────────────────────────────────────────────
    //
    // Owner's design: "every rumor you ask the keep is ALSO an entry in somebody's log."
    //
    // TWO BOOKS AND NO METER, which is #715's own grammar (the shape #798's rip and #804's escort already
    // file in). The captain's field book keeps a sentence the captain wrote — and what the captain wrote down
    // is that the answer came without a look, which is the tell they recorded without knowing they had. The
    // KEEP's book keeps a machine id and nothing a panel could ever print, because the one way to spend this
    // whole design is to show the player the keep's side of it.
    //
    // NOTHING REACTS on this beat. No heat, no nerve, no line about being watched. A shift later the room may
    // say it back in somebody else's mouth (TheRoomSaysItBack, below), and that is the entire consequence.
    //
    // Only at THIS counter: a rumour bought from Rusty Meg is a rumour bought from Rusty Meg.
    private void AskingIsBeingSeenAsking(Core.Interior.Barkeep keep)
    {
        if (!CounterService.ServedByTheKeep(keep) || _surface is not { } ex)
        {
            return;
        }

        FileNote(TheKeep.FieldNote, TheKeep.Glyph);

        // The topic is read off the SAME index the sentence came from (Barkeep.RumorIndexAt), never
        // re-derived beside it: a leak that named a different subject than the rumour the captain actually
        // heard is this repo's fifth bug class with a glass in its hand.
        _contacts.RecordKnownTell(
            TheKeep.LedgerId,
            TheKeep.Name,
            TheKeep.AskedEntry(TheKeep.TopicOf(keep.RumorIndexAt(SimTime)), ex.CanteenWatch));

        RequestVaultSave();   // #225: two books grew
    }

    /// <summary>
    /// #781 · THE TELL THIS WATCH WEARS, or null anywhere the keep is not the one serving.
    ///
    /// <para>Read off the FROZEN watch (#709) and never off the clock, so the second line of the card is a
    /// fact about the shift the captain walked into rather than a sentence that changes while they read it.
    /// Core owns the three sentences and which one this watch gets; this owns only whether there is anybody
    /// there to be told about.</para>
    /// </summary>
    private string? TheKeepsTell() =>
        _surface is { } ex && CounterService.ServedByTheKeep(_barMenu)
            ? TheKeep.TellAt(ex.CanteenWatch)
            : null;

    /// <summary>
    /// #781 · THE ROOM SAYS IT BACK, A WATCH LATER.
    ///
    /// <para>Canon: <i>"a background patron's bark, drawn on a LATER watch after a rumour was bought… the
    /// keep is never quoted saying he talked."</i> So it arrives in a stranger's mouth, in the counter's own
    /// durable overheard book (#119's receipt idiom — a line you were told may not auto-vanish), and nothing
    /// anywhere says how it got there.</para>
    ///
    /// <para>WHETHER, and ABOUT WHAT, are Core's (<see cref="TheKeep.LeakedTopic"/>) off the keep's own
    /// ledger entries and the frozen watch — so it cannot surface on the shift it was asked on, cannot
    /// surface at all before a rumour was bought, and is the same answer every time this card is opened on
    /// that watch. WHO says it is the crowd's own plate list, seeded the same way.</para>
    ///
    /// <para>ONCE, and asked of the durable book itself rather than of a flag beside it — a flag kept next to
    /// a list that already holds the answer is two answers to one question, and this repo has paid for that
    /// twice. A captain who leans on the counter four times is not four people telling them the same thing;
    /// the OTHER topic they asked about is still free to surface on some later shift.</para>
    /// </summary>
    private void TheRoomSaysItBack(Core.Interior.Barkeep counter)
    {
        if (!CounterService.ServedByTheKeep(counter) || _surface is not { } ex)
        {
            return;
        }

        string site = ex.Stop.Body.Id;
        if (TheKeep.LeakedTopic(_contacts.For(TheKeep.LedgerId).KnownTells, site, ex.CanteenWatch)
            is not { } topic)
        {
            return;
        }

        string said = TheKeep.LeakBark(topic);
        foreach (Core.OverheardLine line in _overheard)
        {
            if (line.Text.Contains(said, StringComparison.Ordinal))
            {
                return;   // somebody has already said this one to you
            }
        }

        string who = CanteenRegulars.StrangerPlates[
            DiceRule.Roll(DiceRule.Seed($"counter:leak:who:{site}", ex.CanteenWatch),
                CanteenRegulars.StrangerPlates.Count).Face - 1];
        Overhear($"{who}: {said}", who);
    }
}

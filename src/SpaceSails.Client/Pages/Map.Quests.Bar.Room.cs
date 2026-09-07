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
/// #410 · "WHO'S IN TONIGHT" — the empty chair gets a sentence, and the room keeps what it heard.
///
/// <para>The rota shipped complete — each regular is present at a port only sometimes, in a seeded seat,
/// and an away one is Gone or InTheBack — but an away regular gets NO console, so the player walked up to
/// an empty chair and the game said nothing. This is where it says something.</para>
///
/// <para>Split out of <c>Map.Quests.Bar.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── "Who's in tonight" — the empty chair gets a sentence (issue #410, story pass 2026-08-02) ───────
    //
    // #410's rota shipped complete: each regular is present at a port only sometimes, in a seeded seat, and
    // an away one is Gone or InTheBack. But an away regular gets NO CONSOLE — so the player walks up to an
    // empty chair, presses E, and NOTHING HAPPENS. Three-quarters of the roster could be out and the room
    // would never say so; PatronState.Gone vs InTheBack was computed every watch and told to nobody. That
    // is criterion 1 — "a truth that lives only in Core is not being told" — and the fix is a sentence, in
    // the one voice that would actually know: the barkeep's.
    //
    // Read at _dockVisitSimTime, the SAME frozen watch the deck was welded at (Map.Deck), NOT the live
    // clock. Reading SimTime here would let the line drift out of step with the chairs mid-dock — the sim
    // saying one thing and the sentence another, which is this repo's most common bug by measure.
    private string? WhoIsInTonight()
    {
        if (_dockedHavenId is not { } id)
        {
            return null;
        }

        var seatedHere = new List<string>();
        var steppedOut = new List<string>();
        var inTheBack = new List<string>();
        // #731 · …and the churn with it. A man who stood up and walked out through the cellar door while the
        // captain was standing here has stepped out, and the barkeep is the last person in this game who would
        // still be naming him as in tonight. The consoles, the drawn figures and this sentence read ONE answer
        // (HavenInterior.ResolveRegulars applies the churn), which is the whole reason it is a parameter.
        foreach (HavenInterior.SeatedRegular r in
                 HavenInterior.ResolveRegulars(id, _dockVisitSimTime, TheBarsChurn))
        {
            switch (r.State)
            {
                case PatronState.AtBar: seatedHere.Add(r.ShortName); break;
                case PatronState.InTheBack: inTheBack.Add(r.ShortName); break;
                default: steppedOut.Add(r.ShortName); break;
            }
        }

        var said = new List<string>();
        if (seatedHere.Count > 0)
        {
            said.Add($"{Names(seatedHere)} {(seatedHere.Count == 1 ? "is" : "are")} in tonight.");
        }
        else
        {
            said.Add("Quiet house tonight — none of the usual faces.");
        }
        if (steppedOut.Count > 0)
        {
            said.Add($"{Names(steppedOut)} stepped out.");
        }
        if (inTheBack.Count > 0)
        {
            said.Add($"{Names(inTheBack)} {(inTheBack.Count == 1 ? "is" : "are")} somewhere in the back.");
        }
        return string.Join(" ", said);

        static string Names(IReadOnlyList<string> who) => who.Count switch
        {
            1 => who[0],
            2 => $"{who[0]} and {who[1]}",
            _ => $"{string.Join(", ", who.Take(who.Count - 1))} and {who[^1]}",
        };
    }

    // Append a heard line to the durable "overheard at the bar" book, capped, and persist it. The receipt
    // (#119 idiom) so the words the captain paid for are revisitable, not gone with the toast.
    private void Overhear(string text, string source)
    {
        string bar = _barMenu?.BarName ?? (_dockedHavenId is { } id ? Barkeeps.For(id)?.BarName : null) ?? "THE BAR";
        _overheard = [.. Core.OverheardLog.Append(_overheard, new Core.OverheardLine(text, SimTime, source, bar))];
        RequestVaultSave(); // #225: the book grew
    }

    // The recent lines overheard in THIS bar, newest first — the card's revisitable "overheard here"
    // strip, so a tip you paid a round to hear is still readable when you lean back on the counter.
    private IReadOnlyList<Core.OverheardLine> OverheardHere(int max)
    {
        string? bar = _barMenu?.BarName;
        if (bar is null)
        {
            return [];
        }
        var here = new List<Core.OverheardLine>();
        for (int i = _overheard.Count - 1; i >= 0 && here.Count < max; i--)
        {
            if (string.Equals(_overheard[i].BarName, bar, StringComparison.OrdinalIgnoreCase))
            {
                here.Add(_overheard[i]);
            }
        }
        return here;
    }
}

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
/// WHAT GETS SAID OVER IT — the concrete thing you let slip on a bad roll (chosen from what you are
/// ACTUALLY carrying: hot cargo first, then live heat, then your plan, then a harmless read of your
/// purse), the business a good roll opens, and the small-talk fact a beer hands you.
///
/// <para>Split out of <c>Map.Quests.Bar.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // The concrete thing you let slip on a bad roll, chosen deterministically from what you are ACTUALLY
    // carrying: a hot-cargo hold first (the costliest tell), then live heat, then your current plan,
    // then — with nothing to hide — a harmless read of your purse. Always a real fact they could use.
    private string SlipTell()
    {
        string? hot = _cargoByClass
            .Where(kv => kv.Value > 0 && IsHotClass(kv.Key))
            .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key).FirstOrDefault();
        if (hot is not null)
        {
            return $"the hot {hot} in your hold";
        }
        if (_heat.Level > 0)
        {
            return $"that you're running heat (level {_heat.Level})";
        }
        Quest? plan = _quests.FirstOrDefault(q => q.State is QuestState.Active or QuestState.PickedUp);
        if (plan is { } p)
        {
            string where = BodyById(p.DestBodyId)?.Name ?? p.TargetCallsign;
            if (!string.IsNullOrWhiteSpace(where))
            {
                return $"where you're really bound — {where}";
            }
        }

        return "how thin your purse really runs";
    }

    // The concrete intel a contact hands you when they open up — a rumor made real. Prefer a live
    // off-books ship the public board wouldn't show (name + route), the actionable kind; fall back to a
    // solid heat or price tip. Deterministic per sim-second + berth (OfferIndex), so it never flickers.
    private string OpenIntelLine(string giver) => OpenIntelLine(giver, Core.TellChannel.Business);

    // #5 SundayMorningWind — the tell rides the channel the CHOSEN drink opened. A gin/the hard stuff
    // (Business) hands the sharp, actionable tip — an off-books ghost, a heat warning. A beer (SmallTalk)
    // names one plain trading fact. The local specialty (LocalRumor) loosens the neighbourhood's own
    // gossip, the keep's kind of word. Same live game state, three depths of tell.
    private string OpenIntelLine(string giver, Core.TellChannel channel)
    {
        if (channel == Core.TellChannel.LocalRumor)
        {
            // The house's own pour loosens the house's own gossip — the barkeep's neighbourhood word.
            return CurrentKeep is { } keep ? $"“{keep.RumorAt(SimTime).Trim('“', '”')}”" : SmallTalkFact();
        }
        if (channel == Core.TellChannel.SmallTalk)
        {
            return SmallTalkFact(); // a beer names one plain fact, no more.
        }

        // Business (a gin / the hard stuff): the sharp, actionable tell.
        List<NpcState> ghosts = _npcStates
            .Where(n => n.Active && !n.Arrived && !n.Boarded && !n.Ship.IsPod && !n.Ship.PublishesTimetable)
            .OrderBy(n => n.Ship.Id, StringComparer.Ordinal)
            .ToList();
        if (ghosts.Count > 0)
        {
            NpcShip g = ghosts[OfferIndex(ghosts.Count)].Ship;
            return $"“{g.Callsign} runs dark, {RouteLabel(g)} — carrying, light on guns. Worth more than the drink cost you. You didn't hear it from me.”";
        }
        if (_heat.Level > 0)
        {
            return "“Word on the wire has your face on it — the collectors are asking after you. Lie low a watch before you run anything hot through here.”";
        }

        return "“Prices at the next berth run soft on ice, hard on ore this cycle. Trade accordingly, friend.”";
    }

    // A single plain trading fact — the small-talk tell a beer hands you (a fact, never a proposition).
    private string SmallTalkFact() =>
        _heat.Level > 0
            ? "“Heard the docks are jumpy this cycle — extra eyes at the gate. Just so you know, friend.”"
            : "“Prices at the next berth run soft on ice, hard on ore this cycle. That much I'll say over a beer.”";
}

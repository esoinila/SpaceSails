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

// Map.Alerts — the ship's voice: ShipAlerts and the banner, the parrot's squawks, the comms
// feed and news wire, and the intel provenance behind them. Carved off Map.razor for #251.
public partial class Map
{

    /// <summary>The most recently bought, still-fresh route tip's callsign — now answerable on
    /// any desk, because the ledger lives here rather than inside the Comms-only component.</summary>
    private string? FreshestIntelCallsign()
    {
        RouteIntel? freshest = null;
        foreach (RouteIntel entry in _intelLedger.Entries)
        {
            if (entry.IsFresh(SimTime)
                && (freshest is null || entry.PurchasedAtSimTime > freshest.Value.PurchasedAtSimTime))
            {
                freshest = entry;
            }
        }

        return freshest is { } intel ? FindNpc(intel.ShipId)?.Ship.Callsign ?? intel.ShipId : null;
    }

    // ---- The comms tree (master–detail; Gemini playtest consult 2026-07-05) ----

    private double IntelStaleInDays(string shipId) =>
        _intelLedger.TryGet(shipId, out RouteIntel intel) ? Math.Max(0, intel.SecondsUntilStale(SimTime) / 86400) : 0;

    /// <summary>The contacts tree's groups: scheduled departures, ships en route, and the
    /// permanent orbital fixtures — lean rows, detail on selection.</summary>
    private List<(string Label, List<NpcState> Members)> CommsGroups()
    {
        var scheduled = new List<NpcState>();
        var enRoute = new List<NpcState>();
        var fixtures = new List<NpcState>();
        foreach (NpcState npc in _npcStates)
        {
            if (!npc.Ship.PublishesTimetable && !_intelLedger.Knows(npc.Ship.Id, SimTime))
            {
                continue; // truly off the books — counted by the Off-the-books node instead
            }

            if (npc.Ship.DepotBodyId is not null)
            {
                fixtures.Add(npc);
            }
            else if (!npc.Active && !npc.Arrived && npc.Ship.DepartureTime > SimTime)
            {
                scheduled.Add(npc);
            }
            else
            {
                // Includes mid-flight ships still catching up to their activation tick: they
                // DEPARTED years ago, so "Scheduled" would be the same misread the blind
                // playtest flagged on the raw negative departure times.
                enRoute.Add(npc);
            }
        }

        scheduled.Sort((a, b) => a.Ship.DepartureTime.CompareTo(b.Ship.DepartureTime));
        enRoute.Sort((a, b) => a.Ship.DepartureTime.CompareTo(b.Ship.DepartureTime));
        fixtures.Sort((a, b) => string.CompareOrdinal(a.Ship.Callsign, b.Ship.Callsign));

        var groups = new List<(string, List<NpcState>)>();
        if (scheduled.Count > 0)
        {
            groups.Add(($"Scheduled ({scheduled.Count})", scheduled));
        }

        if (enRoute.Count > 0)
        {
            groups.Add(($"En route ({enRoute.Count})", enRoute));
        }

        if (fixtures.Count > 0)
        {
            groups.Add(($"Depots & fixtures ({fixtures.Count})", fixtures));
        }

        return groups;
    }

    private void SelectCommsShip(string id)
    {
        _commsSelectedId = id;
        TheCaseReadsThisHull(id);   // #417 · the other press that puts a ledger of names in front of him
        _commsHailAnswer = null;
        _commsActionMessage = null;
        if (_selectedTargetId != id)
        {
            SelectTarget(id); // same map/scope selection the old board row click made
        }
    }

    /// <summary>The one badge a lean tree row gets. Careful with the word "tracked": the old
    /// board used it for "seen recently", while the comms actions need a TELESCOPE track — the
    /// live playtest caught the collision, so the badge now says which one it means.</summary>
    private (string Label, string Css) CommsStatusBadge(NpcState npc)
    {
        if (_trackingPost is not null && _trackingPost.TryGetTrack(npc.Ship.Id, out _))
        {
            return ("📡 on ledger", "bg-success");
        }

        if (!npc.Active && npc.Ship.DepartureTime <= SimTime && !npc.Arrived)
        {
            return ("en route", "bg-secondary"); // mid-flight, still catching up to activation
        }

        return StatusLabel(npc) switch
        {
            "Tracked" => ("in sight", "bg-info text-dark"),
            "En route" => ("en route", "bg-secondary"),
            "Lost" => ("lost", "bg-warning text-dark"),
            var other => (other.ToLowerInvariant(), "bg-secondary"),
        };
    }

    /// <summary>"departs in 4d 14h" / "departed 2366d ago" — the blind playtest read the raw
    /// negative departure values as a data bug; phrased time can't be misread.</summary>
    private string DepartureLabel(NpcShip ship)
    {
        if (ship.DepotBodyId is not null)
        {
            return "orbital fixture — always on station";
        }

        double delta = ship.DepartureTime - SimTime;
        var span = TimeSpan.FromSeconds(Math.Abs(delta));
        string amount = span.TotalDays >= 1 ? $"{(int)span.TotalDays}d {span.Hours}h" : $"{span.Hours}h {span.Minutes:D2}m";
        return delta >= 0 ? $"departs in {amount}" : $"departed {amount} ago (mid-flight — outer transfers take years)";
    }

    /// <summary>
    /// #534 tell (e) · <b>THE CAPTAIN KEYS THE TIGHT-BEAM AND SOMETHING ANSWERS.</b> The button was already
    /// here — this is the desk's own 📻 Hail, the one channel the captain has ever used to ask a hull her
    /// intentions — and slice 2 puts <see cref="QShipHail"/>'s two canon answers on it rather than opening a
    /// second way to talk to traffic.
    ///
    /// <para>What she says also goes on her FILE (<see cref="_hailAnswers"/>), because the dossier card is
    /// where the other four tells are read and a fifth tell nobody can put beside them is not a tell. It is
    /// written only when she actually answers, so the card never carries "out of tight-beam range" as though
    /// it were something a hull said.</para>
    ///
    /// <para>The two older sentences below are unchanged and are now what the hulls with nothing to say fall
    /// back to: a pod, which has nobody aboard to key a microphone, and an off-books hauler, whose
    /// destination is the intel economy's goods and is not given away for the price of a hail.</para>
    /// </summary>
    private void CommsHail(NpcState npc)
    {
        if (!ActiveSensors.CanTightBeam(_ship.Position, npc.State.Position))
        {
            _commsHailAnswer = $"{npc.Ship.Callsign} — out of tight-beam range.";
            return;
        }

        if (QShipHail.AnswerTo(npc.Ship, BodyName(npc.Ship.DestinationId)) is { } said)
        {
            _hailAnswers[npc.Ship.Id] = said;
            _commsHailAnswer = said;
            return;
        }

        _commsHailAnswer = npc.Ship.PublishesTimetable
            ? $"{npc.Ship.Callsign} — \"Bound for {BodyName(npc.Ship.DestinationId)}, over.\""
            : $"{npc.Ship.Callsign} — \"No flight plan filed.\"";
    }

    private void CommsLaserRange(NpcState npc)
    {
        LaserRangeTarget(npc.Ship.Id);
        _commsActionMessage = $"Laser ranged {npc.Ship.Callsign} — exact fix, but you're lit up ⚠";
    }

    private int? CommsFencePrice(NpcState npc)
    {
        if (_trackingPost is null || !_trackingPost.TryGetTrack(npc.Ship.Id, out TrackedTarget track))
        {
            return null;
        }

        double quality = track.EffectiveQuality(SimTime);
        return IntelMarket.CanSellTrack(quality)
            ? IntelMarket.SellPrice(quality, npc.Ship.CargoUnits * CargoMarket.UnitValue(npc.Ship.CargoClass))
            : null;
    }

    private bool CommsCanFence(NpcState npc) => DarkWebCanTrade() && CommsFencePrice(npc) is > 0;

    private void CommsSellTrack(NpcState npc)
    {
        if (!CommsCanFence(npc) || CommsFencePrice(npc) is not { } price)
        {
            _commsActionMessage = DarkWebCanTrade()
                ? "Track too shaky to fence — reconfirm it at the Sensors desk (needs ≥50% quality)"
                : DarkWebDisabledReason();
            return;
        }

        _credits += price;
        _commsActionMessage = $"Fenced the {npc.Ship.Callsign} track for {price:N0} cr";
    }
    // The player's bought route intel — owned HERE, not by the DarkWeb component: that
    // component is conditionally rendered with the Comms desk, and when it owned the ledger
    // every purchased tip silently died on desk switch (the tracking post's M27 disease).
    private readonly IntelLedger _intelLedger = new();

    // Comms-tree selection (master–detail; ui-guidelines.md): a ship id, "offbooks", "darkweb".
    private string? _commsSelectedIdValue;

    /// <summary>
    /// The comms tree's selection — and, since #711 slice 2, THE ONE DOOR ONTO THE DARK-WEB DESK.
    ///
    /// <para>A property rather than a field for the reason <c>SwitchDesk</c> is one method: every route in
    /// (the tree node, the ledger's <i>"→ dark web"</i> link, the flow column's own copy of the tree) writes
    /// this one member, so a payment that arrives "the next time the captain opens the desk" cannot be
    /// leaked past by a route that forgot to ask. Nothing new can leak past by forgetting either.</para>
    ///
    /// <para>Only a CHANGE to the desk counts: re-selecting the node you are already on is not opening
    /// anything, and a payment that landed on every re-render would be a payment landing on a frame.</para>
    /// </summary>
    private string? _commsSelectedId
    {
        get => _commsSelectedIdValue;
        set
        {
            bool opening = value is "darkweb" && _commsSelectedIdValue is not "darkweb";
            _commsSelectedIdValue = value;
            if (opening)
            {
                ThePaymentIsThere();
                TheGeocacheEscrowSettles();   // #319 slice 2 · the escrow lands at the same door
                TheSpikeIsSettled();          // #1202 slice 2 · …and the client's purse, or its note, after the window
            }
        }
    }

    private string? _commsHailAnswer;

    // #534 tell (e): what each hull has been heard to say, by hull id. Written by CommsHail when a hull
    // actually answers, read by DossierFor so the sentence sits on her file beside the four numbers. Not on
    // NpcState on purpose — this is what the CAPTAIN has heard, not a fact about the ship, and the world's
    // boot fingerprint has no business moving because a radio was used.
    private readonly Dictionary<string, string> _hailAnswers = [];
    private string? _commsActionMessage;

    // ---- PR-14: the news wire (docs/SaturdayPlan/StationDesks.md #14) ----
    // The one source of truth for both the Comms ticker and the Galley's long feed: a bounded,
    // newest-first ledger of player-triggered NewsWire.NewsEvents, blended on demand with
    // NewsWire.Ambient's rotating scenario flavor. Core stays pure (NewsWire has no state of its
    // own) — the mutable ledger lives here, same pattern as the tracking post's own ledger.
    private const int MaxNewsEvents = 50;
    private const int CommsTickerAmbientDays = 6;   // ambient days blended in behind fresh events
    private const int CommsTickerItemCount = 5;     // the ticker shows only the freshest few
    private const int GalleyFeedAmbientDays = 20;   // the Galley wants the long scrollback
    private const int GalleyFeedItemCount = 25;

    // #1052 (L2) · THE PAPER AT A TABLE. Shorter than the galley card's scrollback on purpose: the galley
    // card owns the screen and this panel stands BESIDE a live room the captain is watching, so it is a
    // paper you skim over a drink rather than an archive you sit down to. The numbers are the only two
    // constants this lane invents, and they are the same pair the other two consumers already name.
    private const int SeatedNewsAmbientDays = 12;
    private const int SeatedNewsItemCount = 12;
    private readonly List<NewsWire.NewsEvent> _newsEvents = [];

    private void PushNewsEvent(NewsWire.NewsEventKind kind, string subject, string? detail = null)
    {
        _newsEvents.Insert(0, new NewsWire.NewsEvent(kind, SimTime, subject, detail));
        if (_newsEvents.Count > MaxNewsEvents)
        {
            _newsEvents.RemoveRange(MaxNewsEvents, _newsEvents.Count - MaxNewsEvents);
        }
    }

    /// <summary>Player events (as headlines) blended with <paramref name="ambientCount"/> days of
    /// rotating ambient flavor, newest first — the Comms ticker takes a short slice of this, the
    /// Galley desk a long one.
    ///
    /// <para>#1052 (L1) · <paramref name="scope"/> picks the masthead. It defaults to
    /// <see cref="NewsWire.NewsScope.SystemWire"/> and both shipped consumers (the galley card at key 6
    /// and the Comms ticker) pass nothing, so their output is byte-identical to what it was before this
    /// lane — <c>TheGalleyAndTheTickerStillReadTheSystemWireTests</c> guards that. A
    /// <see cref="NewsWire.NewsScope.PortRag"/> reader gets the port's own sheet on top; a
    /// <see cref="NewsWire.NewsScope.CompanyIntranet"/> reader gets the facility's paper and, per the
    /// design, NONE of the system wire — which is why the pushed events are dropped too: a lab's
    /// noticeboard does not carry news of a robbery three planets away.</para></summary>
    private IReadOnlyList<NewsWire.NewsItem> NewsFeed(
        int ambientCount,
        NewsWire.NewsScope scope = NewsWire.NewsScope.SystemWire,
        string? salt = null)
    {
        var items = new List<NewsWire.NewsItem>(_newsEvents.Count + ambientCount);
        if (scope != NewsWire.NewsScope.CompanyIntranet)
        {
            foreach (NewsWire.NewsEvent evt in _newsEvents)
            {
                // #1202 · …and a port's own gossip about a stringer's story prints on a port's rag only.
                // Every kind that existed before it prints exactly where it always did.
                if (!NewsWire.PrintsIn(evt.Kind, scope))
                {
                    continue;
                }

                // #1052 (L2) · …AND WHAT THE LINE IS ABOUT TRAVELS WITH IT. The subjects are the event
                // author's own answer (NewsWire.SubjectsFor) rather than anything read back off the
                // headline, and they are carried on every consumer's feed — the ticker and the galley card
                // simply never draw a ✂, so nothing about them changes.
                items.Add(new NewsWire.NewsItem(
                    evt.SimTime, NewsWire.Headline(evt), NewsWire.SubjectsFor(evt)));
            }
        }

        if (_ephemeris is not null)
        {
            items.AddRange(NewsWire.Ambient(_ephemeris, SimTime, ambientCount, scope, salt));
        }

        items.Sort((a, b) => b.SimTime.CompareTo(a.SimTime));
        return items;
    }

    // ---- M28 (Sunday PR-E): the ship's parrot 🦜 — the alarm system with personality ----
    private string? _parrotSquawk;
    private double _parrotBubbleUntilMs;
    private double _parrotCooldownUntilMs;
    private int _parrotCounter;
    private bool _parrotSawWobble, _parrotSawArc, _parrotSawHunter, _parrotSawPrey, _parrotSawPyramid;
    private readonly HashSet<string> _parrotOffBooks = [];

    private readonly List<ScopeIntel> _scopeIntel = [];

    // Route-tip provenance (PR-J), keyed by the ledger's key (ship id): who gave the tip, at which
    // station, and when. Client-side only — Core's RouteIntel stays a pure value. Entries the player
    // bought off the dark web have no provenance here and render unattributed in the ledger.
    private sealed record IntelProvenance(string Giver, string Station, double AcquiredSimTime);
    private readonly Dictionary<string, IntelProvenance> _routeIntelProvenance = new();

    // "<giver> · <station> · day N" for the ledger's provenance line (day = sim day, 0-based like the clock).
    private static string ProvenanceLine(string giver, string station, double simTime) =>
        $"{giver} · {station} · day {(int)(simTime / 86400)}";

    // ---- #166: the ship-wide alert channel. One edge-triggered, acknowledgeable source the banner
    // strip, the ledger, and the 🦜 parrot all read. Three founding conditions raise/clear here:
    // collision (ROCKS AHEAD), fuel (amber at the 18% reserve, red at the reach-a-pump floor), and the
    // #180/#183 orbit-degradation warning (migrated in). Evaluated each tick in UpdateShipAlerts. ----
    private readonly ShipAlerts _shipAlerts = new();

    // #159/#184: which queued step the multi-row banner shows below the pinned NOW row. The ▲▼ arrows
    // page it; the render clamps it to the live queue so a completed step never leaves it out of range.
    private int _bannerRowOffset;
    private void BannerPageUp() => _bannerRowOffset = Math.Max(0, _bannerRowOffset - 1);
    private void BannerPageDown() => _bannerRowOffset++;

    // The orbit estimate the Fixer hands over: numbers grounded in the wreck's real rail, worded
    // with a little imprecision (the scan box does the precise work). Voice + phase window, so the
    // player knows WHERE and roughly WHEN to look (Expanse rules).
    private ScopeIntel BuildWreckIntel(string bodyId, string? giver = null, string? station = null)
    {
        double aimTime = SimTime + IntelScanLeadSeconds;
        double auHere = 0, periodDays = 0, phaseDeg = 0;
        if (_ephemeris is not null)
        {
            foreach (CelestialBody cand in _ephemeris.Bodies)
            {
                if (cand.Id != bodyId)
                {
                    continue;
                }
                auHere = cand.OrbitRadius / 1.495978707e11;
                periodDays = Math.Abs(cand.OrbitPeriod) / 86400.0;
                Vector2d p = _ephemeris.Position(bodyId, SimTime);
                phaseDeg = (Math.Atan2(p.Y, p.X) * 180 / Math.PI + 360) % 360;
                break;
            }
        }
        var lines = new List<string>
        {
            $"Last transponder fix: {Derelict.RoadsterBearingPhrase}, r ≈ {auHere.ToString("F2", CultureInfo.InvariantCulture)} AU, period ≈ {periodDays.ToString("F0", CultureInfo.InvariantCulture)} d.",
            $"She bore ~{phaseDeg.ToString("F0", CultureInfo.InvariantCulture)}° off the sun then, creeping prograde — near-circular, so she hasn't gone far.",
            $"She should cross the predicted phase around {FormatSimTime(aimTime)}. Point the scope there and she'll glint.",
        };
        return new ScopeIntel($"wreck-intel-{bodyId}", bodyId, "🔭 Roadster orbit fix", lines,
            giver, station, SimTime);
    }
}

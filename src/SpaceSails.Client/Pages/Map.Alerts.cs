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

    private void CommsHail(NpcState npc)
    {
        _commsHailAnswer = !ActiveSensors.CanTightBeam(_ship.Position, npc.State.Position)
            ? $"{npc.Ship.Callsign} — out of tight-beam range."
            : npc.Ship.PublishesTimetable
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
    private string? _commsSelectedId;
    private string? _commsHailAnswer;
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
    /// Galley desk a long one.</summary>
    private IReadOnlyList<NewsWire.NewsItem> NewsFeed(int ambientCount)
    {
        var items = new List<NewsWire.NewsItem>(_newsEvents.Count + ambientCount);
        foreach (NewsWire.NewsEvent evt in _newsEvents)
        {
            items.Add(new NewsWire.NewsItem(evt.SimTime, NewsWire.Headline(evt)));
        }

        if (_ephemeris is not null)
        {
            items.AddRange(NewsWire.Ambient(_ephemeris, SimTime, ambientCount));
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

    private void SquawkNow(Parrot.Squawk kind, double nowMs, string? subject = null, bool force = false)
    {
        if (!force && nowMs < _parrotCooldownUntilMs)
        {
            return; // one squawk at a time; the bird sulks between
        }

        _parrotSquawk = Parrot.Line(kind, _parrotCounter++, subject);
        _parrotBubbleUntilMs = nowMs + Parrot.BubbleSeconds * 1000;
        _parrotCooldownUntilMs = nowMs + Parrot.CooldownSeconds * 1000;
        RendererInterop.PlayCue("pulse");
    }

    /// <summary>Rising-edge detectors over live ship state, priority ordered — the parrot
    /// yells once when a thing BECOMES true, never per frame.</summary>
    private void UpdateParrot(double nowMs)
    {
        if (_parrotSquawk is not null && nowMs > _parrotBubbleUntilMs)
        {
            _parrotSquawk = null;
        }

        // #166: the collision squawk now rides the ShipAlerts channel (UpdateShipAlerts) so the banner
        // strip, the ledger, and the parrot all speak from one crossing — see there for the ROCKS AHEAD.

        bool prey = SelectedCaptureTarget() is { } target && CaptureRule.IsInWindow(_ship, target.State);
        if (prey && !_parrotSawPrey)
        {
            SquawkNow(Parrot.Squawk.PreyInGlass, nowMs);
        }

        _parrotSawPrey = prey;

        bool hunterNear = NearestHunterDistance() is { } hunterDistance && hunterDistance < 5e9;
        if (hunterNear && !_parrotSawHunter)
        {
            SquawkNow(Parrot.Squawk.HunterNear, nowMs);
        }

        _parrotSawHunter = hunterNear;

        bool arcing = _plasma is not null && _ship.Charge >= ArcChargeThreshold;
        if (arcing && !_parrotSawArc)
        {
            SquawkNow(Parrot.Squawk.Arcing, nowMs);
        }

        _parrotSawArc = arcing;

        bool wobble = RumWobbleActive;
        if (wobble && !_parrotSawWobble)
        {
            SquawkNow(Parrot.Squawk.DrunkDriver, nowMs);
        }

        _parrotSawWobble = wobble;

        // Off the books: the sweep found a ship that publishes no timetable — once per ship.
        if (_trackingPost is not null)
        {
            foreach (TrackedTarget entry in _trackingPost.Entries)
            {
                if (_parrotOffBooks.Contains(entry.ShipId))
                {
                    continue;
                }

                if (FindNpc(entry.ShipId) is { } secretive && !secretive.Ship.PublishesTimetable)
                {
                    _parrotOffBooks.Add(entry.ShipId);
                    SquawkNow(Parrot.Squawk.OffTheBooks, nowMs);
                    break;
                }
            }
        }

        bool pyramidInSky = false;
        for (int i = 0; i < AncientsRule.PyramidCount; i++)
        {
            if (AncientsRule.Revealed(i, _ship.Position, SimTime))
            {
                pyramidInSky = true;
                break;
            }
        }

        if (pyramidInSky && !_parrotSawPyramid)
        {
            SquawkNow(Parrot.Squawk.PyramidSighted, nowMs);
        }

        _parrotSawPyramid = pyramidInSky;
    }

    // #166: drive the ship-wide alert channel from live sim state each tick. Collision and fuel are
    // evaluated here; the orbit-degradation alert is raised/cleared from UpdateParkStability (its own
    // edge detector) through RaiseOrbitDegrade/ClearOrbitDegrade. On a rising edge the channel returns
    // true — the cue to squawk the parrot, log a receipt, and (for a red) drop warp so it can't be
    // blown past. The banner strip and desk chips read the channel; nothing here re-shouts per tick.
    private void UpdateShipAlerts(double nowMs)
    {
        // Collision: the course has a ballistic impact / sub-surface pass in the horizon — UNLESS the
        // autopilot is armed with a valid rehearsed plan, in which case the alarm trusts the PLAN the
        // rehearsal flew (#196/#148). The insert burn resolves the ballistic impact, so that impact is
        // the plan working, not news; the ballistic alarm returns the instant the plan is gone (disarm/
        // handback → _autopilotPlanClosestPass null). A plan whose OWN path goes subsurface leaves an
        // Impact pass cached and shouts red immediately — a bad plan shouts LOUDER, not softer.
        // #220: while the autopilot HOLDS the park (keeping active AND the next trim is funded), the
        // ballistic projection's between-trim dip toward the surface is the keeper working, not danger —
        // the alarm trusts the kept orbit. `_orbitKept` alone is the "keeping ended" edge (StationKeep
        // clears it on an unbound orbit or a dry tank; a disarm clears it via ResetApproachTracking), so
        // the alarm returns to the ballistic course the instant keeping ends (#183/#193 backstop).
        bool armedWithPlan = _armedOrbitBodyId is not null && _autopilotPlanPath is { Count: >= 2 };
        bool keepingHoldsOrbit = _orbitKept && _keepTrimFunded;
        ClosestApproach.Pass? collision =
            CollisionAlertRule.Evaluate(armedWithPlan, keepingHoldsOrbit, _closestPass, _autopilotPlanClosestPass);
        if (collision is { } cp)
        {
            string body = cp.BodyName;
            if (_shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, $"ROCKS AHEAD! — impact with {body}", SimTime))
            {
                SquawkNow(Parrot.Squawk.Impact, nowMs, body, force: true);
                LogAutopilotEvent($"⚠ collision alarm — impact course with {body}");
                Warp = 1; // an impact at 10,000× is unwatchable
            }
        }
        else
        {
            _shipAlerts.Clear(AlertKind.Collision);
        }

        // Fuel: amber at the 18% autopilot reserve, red at the reach-a-pump floor (FuelAlertRule).
        switch (FuelAlertRule.Evaluate(_reactionMassPulses, ReactionMassCapacity))
        {
            case AlertSeverity.Red:
                if (_shipAlerts.Raise(AlertKind.Fuel, AlertSeverity.Red,
                        $"fuel critical — {_reactionMassPulses} p left, can't reach a pump", SimTime))
                {
                    SquawkNow(Parrot.Squawk.FuelLow, nowMs, force: true);
                    LogAutopilotEvent($"⚠ fuel critical — {_reactionMassPulses} p, below the reach-a-pump floor");
                    Warp = 1;
                }
                break;
            case AlertSeverity.Amber:
                if (_shipAlerts.Raise(AlertKind.Fuel, AlertSeverity.Amber,
                        $"fuel low — {_reactionMassPulses} p left, under the autopilot reserve", SimTime))
                {
                    SquawkNow(Parrot.Squawk.FuelLow, nowMs);
                    LogAutopilotEvent($"fuel low — {_reactionMassPulses} p, under the 18% reserve");
                    // #172: a fuel-amber crossing mid-skip is an event the captain must see (amber never
                    // yanks warp on its own, so cancel skip here). Skip never blows past a fuel warning.
                    EndSkipIfActive("fuel low — under the autopilot reserve");
                }
                break;
            default:
                _shipAlerts.Clear(AlertKind.Fuel);
                break;
        }

        // #266: ADRIFT — the tank is empty and we're not docked. The founding "we're stranded" beat rides
        // this same channel (like ROCKS AHEAD and orbit-decay): the strip says the state, the ledger logs
        // it, the parrot proposes the tow — and the rescue POP-UP opens on the rising edge so the one
        // button the moment exists to offer is in the captain's face, never buried in the masthead's
        // shadow (the #262 stranding). The action lives in the pop-up, not the banner (#236).
        if (Adrift)
        {
            if (_shipAlerts.Raise(AlertKind.Adrift, AlertSeverity.Red,
                    "ADRIFT — out of reaction mass. The parrot's whistling for a tow; open the rescue offer.", SimTime))
            {
                SquawkNow(Parrot.Squawk.Adrift, nowMs, force: true);
                LogAutopilotEvent("⚠ adrift — out of reaction mass; rescue offered");
                Warp = 1; // stranded is not a thing to watch at 10,000×
                _showRescueOffer = true; // the offer pops up the instant we go dry
            }
        }
        else
        {
            // Under way again (rescued, or a burn found a pump): clear the alert and dismiss any stale offer.
            if (_shipAlerts.Clear(AlertKind.Adrift))
            {
                _showRescueOffer = false;
            }
        }
    }

    // #166: the captain silences one alert's shout — it lingers as a dimmed chip while the condition
    // holds, and a fresh crossing (or an escalation) shouts again.
    private void AcknowledgeAlert(AlertKind kind) => _shipAlerts.Acknowledge(kind);
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
            $"Last transponder fix: sunward of Mars, r ≈ {auHere.ToString("F2", CultureInfo.InvariantCulture)} AU, period ≈ {periodDays.ToString("F0", CultureInfo.InvariantCulture)} d.",
            $"She bore ~{phaseDeg.ToString("F0", CultureInfo.InvariantCulture)}° off the sun then, creeping prograde — near-circular, so she hasn't gone far.",
            $"She should cross the predicted phase around {FormatSimTime(aimTime)}. Point the scope there and she'll glint.",
        };
        return new ScopeIntel($"wreck-intel-{bodyId}", bodyId, "🔭 Roadster orbit fix", lines,
            giver, station, SimTime);
    }
}

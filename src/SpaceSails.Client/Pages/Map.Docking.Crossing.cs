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
/// #313 · THE CROSSING ITSELF — the door IS the flight. Taking the shuttle to a berth advances the
/// clock by the crossing, clamps at the far end, welds on its walkable complex, and steps the captain
/// off in the hall.
///
/// <para>And the clock is the dangerous part. <i>"Frozen in place; the shuttle moved, not the
/// mothership"</i> is a claim about the SHIP's frame, while every position in this game is written in
/// the Sun's — a clock that ran while she did not flew the HQ quick start into Enceladus on every
/// single boot. <c>LoiterClock</c> carries the argument; this file is where it is spent.</para>
///
/// <para>Split out of <c>Map.Docking.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // Take the shuttle to a berth in range — the door is the flight. Only interior station havens (μ≤0,
    // clampable) are ever pickable, so arriving reuses the clamp-and-go-ashore path: advance the clock
    // by the crossing, clamp at the destination, weld on its walkable complex, and step off in the hall.
    private void TakeShuttleTo(ShuttleStop stop)
    {
        if (_ephemeris is null)
        {
            return;
        }

        // #538 · A COLD BOAT IS NOT A RIDE. Owner: "Shuttle should also power down to make it less of an
        // anomaly" and then, on the price of it, "Warm up time is a cost there 😎" — so the warm-up is charged
        // HERE, at the moment a captain wants to leave, rather than when they went dark. Asking a sleeping boat
        // for a ride starts waking her, which is the only sensible reading of the request.
        if (!BoatReadyToFly())
        {
            return;
        }
        CelestialBody dest = stop.Body;
        double newT = SimTime + stop.TravelSeconds;

        // The shuttle sets the ship down on the destination's rail — the shared co-moving berth state (#269).
        Vector2d destPos = _ephemeris.Position(dest.Id, newT);
        _ship = BerthState.CoMoving(_ephemeris, dest.Id, newT, BerthState.BerthOffsetMeters, _ship.Charge);
        SimTime = newT;

        _dockedHavenId = dest.Id;
        _dockOffset = _ship.Position - destPos;             // freeze the arm's reach at the new berth
        _nearestBody = dest;
        _nearestBodyPosition = destPos;
        _nearestBodyVelocity = _ship.Velocity;              // the berth rides the destination's drift
        _armedOrbitBodyId = null;                           // a berth disarms any pending auto-insert
        ArrivedAt(dest.Id);                                 // #962: a shuttle hop is an arrival too — see ClampOntoHaven
        TheArrivalIsRemembered(dest.Id);                    // #973 L4: …and a place can finish a grey page
        _autopilotStandDownReason = null; _dockReadyStatus = null;
        ResetAutopilotBudget();
        _matchLedger = _matchLedger.Abort();                // #268: a shuttle hop abandons any match tab, uncharged
        StaleFutureNodes();                                 // a berth cancels any pending burns
        HoldAtDock();                                       // pin the ship to the station's drift now
        SetDeckForDock(dest.Id);                            // weld on the destination's walkable complex

        // Emerge in the destination interior: you stepped off the shuttle past the gangway, into the
        // concourse — not back aboard your own bridge.
        (_avatarX, _avatarY, _avatarHeading) = (2.5, StationFloorY + 6, Math.PI / 2);
        RefreshAshore();
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;

        _shuttleBayStops = null;
        RendererInterop.PlayCue("board");
        ShowPulseMessage($"🚀 Shuttle away — {FormatDuration(stop.TravelSeconds)} later you step off at {dest.Name}.");
        CompleteCargoRunQuests(dest.Id); // arriving at a delivery berth still closes a cargo run (M-Q3)
        PayCompletedQuests();
    }

    // The shuttle round-trip's clock cost. #733: this used to re-stamp the loitering ship at the new time
    // and leave her exactly where she was — "frozen in place; the shuttle moved, not the mothership" — and
    // that freeze is what flew the HQ quick start into Enceladus, seconds after the ground card, on every
    // single boot. The whole argument is in LoiterClock; the short version is that "in place" is a claim
    // about the SHIP's frame while every position in this game is written in the Sun's, so a clock that
    // moved while she did not teleported her backwards along her own rail and let the moon run her down.
    private void AdvanceShuttleClock(double travelSeconds)
    {
        if (AdvanceLoiterClock(travelSeconds))
        {
            return; // the crossing ended in a strike — the freeze-frame owns the moment, nothing else runs
        }
        RunCacheDiscoveryWatch();
    }

    /// <summary>
    /// #733 · The clock cost of something the captain is doing that is NOT flying the ship — the shuttle's
    /// round trip, a night in the bunk — paid by the hull as well as by the calendar. Returns true when the
    /// coast ended in a surface strike (by then the busted freeze-frame is already staged).
    ///
    /// <para>A CLAMPED ship keeps the old shape, and always should have: the berth owns her position, so
    /// the clock moves and <see cref="HoldAtDock"/> re-pins her onto the station's drift — which is exactly
    /// what <c>OnTick</c>'s docked branch does every frame. That branch is why a docked loiter was never
    /// buggy, and it is the whole reason <c>?secretlab=deep&amp;land=1</c> survived where the head office
    /// did not: one cheat leaves you clamped, the other lets you go.</para>
    ///
    /// <para>A FREE-FLYING ship now flies the cost, ballistically, through the shared Core law. If her
    /// conic really does reach a surface in that time the strike is handed to <see cref="TriggerImpact"/>
    /// exactly as the live loop would have handed it over — never tunnelled, and never invented.</para>
    /// </summary>
    private bool AdvanceLoiterClock(double seconds)
    {
        double cost = Math.Max(0.0, seconds);

        if (_dockedHavenId is not null || _simulator is null || _ephemeris is null || cost <= 0.0)
        {
            double stamped = SimTime + cost;
            _ship = new ShipState(_ship.Position, _ship.Velocity, stamped, _ship.Charge);
            SimTime = stamped;
            if (_dockedHavenId is not null)
            {
                HoldAtDock();
            }
            return false;
        }

        LoiterClock.Coast coast = LoiterClock.Advance(_simulator, _ephemeris, _ship, cost);
        _ship = coast.Ship;
        SimTime = _ship.SimTime;

        if (coast.Struck is { } hit && _busted is null)
        {
            TriggerImpact(hit);
            return true;
        }

        return false;
    }
}

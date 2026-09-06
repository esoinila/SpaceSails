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
/// #268 · THE CLAMP'S OWN STATE — what the ship is berthed to, the frozen arm's reach, the deferred
/// fuel tab, and the affordance that decides whether ⚓ is offered at all.
///
/// <para>Owner: <i>"like a dry-dock — some kind of tube/arm/clamps keeps the ship connected; the ship
/// should not need guiding while docked; there the heat lowers."</i> A station haven has no mass to
/// orbit, so you clamp onto it and ride its drift.</para>
///
/// <para>Pay-at-the-pump lives here: a ⚓ Match &amp; clamp burn fires at the press but its pulses ride a
/// tab that settles only when the clamp lands, so an approach that diverges is never charged.</para>
///
/// <para>Split out of <c>Map.Docking.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ---- Berthing at a grey-market dock (owner: "like a dry-dock — some kind of tube/arm/clamps
    // keeps the ship connected; the ship should not need guiding while docked; there the heat
    // lowers"). A station haven (mu = 0) can't be orbited, so you clamp onto it: an arm reaches out,
    // the ship rides the dock's drift, and the heat bleeds off. ----
    private string? _dockedHavenId;   // the station haven we're clamped to, or null
    private Vector2d _dockOffset;     // frozen ship-minus-dock offset while clamped (the arm's reach)

    // #268 pay-at-the-pump: the deferred bill for a ⚓ Match & clamp burn. The redirect impulse fires at the
    // press (instant, #213), but its pulses are NOT taken then — they ride this tab and settle only when the
    // clamp lands (ClampOntoHaven). A diverging/aborted approach that never clamps drops the tab uncharged
    // (UpdateDockAffordance), so a leg that never delivered keeps no fuel it never earned. Pure logic in
    // Core.MatchClampLedger; this is the one live copy the client mutates.
    private MatchClampLedger _matchLedger = MatchClampLedger.Empty;

    // #212/#211/#213: the ONE dock affordance truth — the toolbar ⚓ button and the envelope line both
    // read this, so text and button can never disagree. Recomputed every OnTick (paused or not, so the
    // quote survives pause), latch state carried frame-to-frame so the offer doesn't blink as the
    // orbiting station's relative speed phases in and out.
    private DockAffordance _dockAffordance = DockAffordance.Hidden;
    private bool _dockLatched;

    // Recompute the one-truth dock affordance from the SAME focus-first selection the envelope line uses
    // (destination/armed haven first, then nearest dockable haven). Runs every frame — including paused —
    // so a captain reading the board sees the same affordances and the #213 match quote before anything
    // fires. While clamped there is no affordance (the 🚀 Undock button owns the slot).
    private void UpdateDockAffordance()
    {
        if (_dockedHavenId is not null || _ephemeris is null)
        {
            _dockAffordance = DockAffordance.Hidden;
            _dockLatched = false;
            return;
        }

        List<DockHaven> havens = new();
        const double h = 1.0;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!IsDockableHaven(body) || IsBodyHidden(body.Id))
            {
                continue;
            }

            Vector2d pos = _ephemeris.Position(body.Id, SimTime);
            Vector2d vel = (_ephemeris.Position(body.Id, SimTime + h) - _ephemeris.Position(body.Id, SimTime - h)) / (2 * h);
            bool focus = body.Id == _destinationBodyId || body.Id == _armedOrbitBodyId;
            havens.Add(new DockHaven(body, pos, vel, focus));
        }

        // #268: the affordance reads the EFFECTIVE tank — pulses already on a pending match tab are
        // spoken-for (committed, just not yet settled), so the ⚓ offer and its affordability reflect what's
        // actually free to burn, not the full tank the deferred take hasn't hit yet. #200's focus panel
        // quotes the SAME property, so its "match burn" row is judged against the same mass.
        _dockAffordance = DockAffordanceRule.Evaluate(_ship, havens, EffectiveDockTankPulses, _dockLatched);
        _dockLatched = _dockAffordance.Latched;

        // #268: a match tab whose berth is no longer under the clamp button — the ship diverged out of the
        // envelope, or the affordance now names a different haven — dropped without delivery. Release it
        // UNCHARGED: an aborted or diverging approach keeps no money it never earned. (Settlement is only ever
        // the clamp; while the ship holds the berth the affordance keeps a ⚓ button on this same haven.)
        if (_matchLedger.Owes
            && !(_dockAffordance.HavenId == _matchLedger.HavenId && _dockAffordance.ShowButton))
        {
            ShowPulseMessage($"⚓ match stood down at {BodyName(_matchLedger.HavenId!)} — {_matchLedger.Pulses} p released, unspent (never clamped on).");
            _matchLedger = _matchLedger.Abort();
        }
    }

    // Resolve a haven's live world state straight from the ephemeris — used so docking clamps onto the
    // SELECTED haven (the affordance's one truth), never onto whichever body happens to be _nearestBody
    // (Mars photobombing the Roadstead was exactly #212).
    private (CelestialBody Body, Vector2d Pos, Vector2d Vel)? ResolveDockHaven(string id)
    {
        if (_ephemeris is null || _ephemeris.Bodies.FirstOrDefault(b => b.Id == id) is not { } body)
        {
            return null;
        }

        const double h = 1.0;
        Vector2d pos = _ephemeris.Position(id, SimTime);
        Vector2d vel = (_ephemeris.Position(id, SimTime + h) - _ephemeris.Position(id, SimTime - h)) / (2 * h);
        return (body, pos, vel);
    }

    // The dock envelope lives in Core (DockRule, #155) so the arm-time rehearsal, the live station
    // stand-down and this UI can never quote different numbers. These aliases keep the call sites reading
    // the same way they did when the literals lived here.
    private const double DockReachMeters = DockRule.EnvelopeMeters;    // how close you must coast to throw the clamp on
    private const double DockMatchSpeedMps = DockRule.MatchSpeed;      // and how nearly matched to its drift

    // A station haven you clamp onto (⚓): mass-less, so it can't be orbited — the dock is the only
    // way to lie low there. Moon havens (mu > 0) you hide at by orbiting instead. This is the split
    // the ⚓ marker, the scope's "HAVEN ⚓ DOCK" tag and the Nav hint all key off.
    // The one truth (#288): shared with the Core DockableHavens registry the ?dock cheat and the CI smoke
    // sweep read, so the client's clamp gate and the tested berth list can never drift apart.
    private static bool IsDockableHaven(CelestialBody body) => DockableHavens.IsDockable(body);

    // The station haven we're close enough (and slow enough) to clamp onto right now, if any — read
    // from the one-truth affordance (#212) so it names the SAME haven the envelope line does, never the
    // raw nearest body.
    private CelestialBody? DockableHavenHere() =>
        _dockedHavenId is null && _dockAffordance.CanClampNow && _dockAffordance.HavenId is { } id
            ? BodyById(id)
            : null;

    // ---- The shuttle-bay airlock: "the door you understand as a flight" (#163) ----
    // Walking up to the bay's airlock (ConsoleKind.ShuttleAirlock) opens a pop-up of the places within
    // one shuttle hop (ShuttleRange). Picking a berth and confirming IS the trip: the sim clock advances
    // by the crossing and you step off ashore in the destination's interior — no separate flight
    // minigame. The bay's airlock travels with the ship into every docked complex (HavenInterior seeds
    // its doors from the ship's), so the same door at the destination is the ride home — never stranded.

    // One reachable stop on the destination board (#313): the body, how far / how long, and how the row
    // reads — a berth to step off at, a landable surface to walk, and/or a place with a chest already in
}

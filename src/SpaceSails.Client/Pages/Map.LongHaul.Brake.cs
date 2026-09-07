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
/// #304 · THE ARRIVAL BRAKE ASKS (owner 2026-07-18: <i>"let's have it ask, it is hard to remember in the
/// heat of the moment otherwise"</i>) — the window, the ask, the fire and the decline.
///
/// <para>The #262/#284 brake was fired-by-hand-or-forgotten; here it is classed with the are-you-sure
/// family. When a long haul delivers the ship hot to a destination that owes a brake the window OPENS and
/// the ship ASKS (<c>ArrivalBrake.Advance</c>, the pure timing law); consent fires it once through the
/// clamp/settle path, and decline snoozes and re-raises while the window remains.</para>
///
/// <para>Split out of <c>Map.LongHaul.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ===== #304 — THE ARRIVAL BRAKE ASKS (owner 2026-07-18: "let's have it ask, it is hard to remember in
    // the heat of the moment otherwise"). The #262/#284 arrival brake was fired-by-hand-or-forgotten; now it
    // is classed with the are-you-sure family. When a long haul delivers the ship hot to a destination that
    // owes a brake, the window OPENS and the ship ASKS (ArrivalBrake.Advance, the pure timing law). Consent
    // fires it once through the clamp/settle path; decline snoozes and re-raises while the window remains.
    private ArrivalBrake.Gate _brakeGate = ArrivalBrake.Gate.Closed;

    // The destination body a propulsive arrival brake is owed at (set when a long haul lands hot), or null.
    // The aerobrake path keys off _aerobrakeArmedBodyId instead — the same window, a different flavour.
    private string? _brakeArrivalBodyId;

    // The brake quoted at engage (#262) — the bill the ask speaks and the fire pays. Re-priced from the
    // live arrival state would drift; the engage quote is the one the captain was shown, so it is the bill.
    private int _brakeQuotedPulses;
    private string _brakeDestName = "";
    private bool _brakeShowing; // last frame's Asking state — so the raise squawks on the rising edge only

    // The world-space body the current brake window is owed at (propulsive arrival or armed aerobrake), and
    // whether the armed method is the aerobrake. Aerobrake takes precedence — it is the chosen way to pay —
    // but ONLY while its filed quote is a real trade (#962; see ArmedAerobrakeOffer).
    private CelestialBody? BrakeWindowBody()
    {
        string? id = ArmedAerobrakeOffer() is not null ? _aerobrakeArmedBodyId : _brakeArrivalBodyId;
        return id is null ? null : _ephemeris?.Bodies.FirstOrDefault(b => b.Id == id);
    }

    private bool BrakeIsAerobrake => ArmedAerobrakeOffer() is not null;

    // The ship's speed relative to a body right now (the "how hot am I coming in" the window reads).
    private double RelativeSpeedTo(CelestialBody body) =>
        _ephemeris is null ? 0.0 : (_ship.Velocity - TransferMath.BodyVelocity(_ephemeris, body.Id, SimTime)).Length;

    // Is the brake WINDOW open this frame? The ship is near the destination (inside its Hill sphere — the
    // arrival vicinity), still hot (relative speed above the clamp window, so a brake is genuinely owed),
    // and actually FLYING — a clamped ship is not arriving (#962: the owner's screenshot had the Jupiter
    // card up while the ship lay clamped at The Red Eye, because a berth inside Jupiter's Hill sphere reads
    // as "near Jupiter and moving fast relative to it" for as long as you lie there). The predicate itself
    // is Core (ArrivalBrake.WindowOpen); this reads the world and hands it the numbers. Once the captain
    // sheds by hand, clamps on, or wanders clear, this falls false and the gate resets.
    private bool BrakeWindowOpen(CelestialBody body)
    {
        if (_ephemeris is null)
        {
            return false;
        }

        CelestialBody? parent = body.ParentId is { } pid ? _ephemeris.Bodies.FirstOrDefault(b => b.Id == pid) : null;
        double vicinity = parent is { Mu: > 0 }
            ? OrbitRule.HillRadius(body, parent.Mu)
            : OrbitRule.CaptureRange(OrbitRule.HillRadius(body, 1.0));
        double dist = (_ship.Position - _ephemeris.Position(body.Id, SimTime)).Length;
        return ArrivalBrake.WindowOpen(
            // The CLAMP, not _docked — that other flag is mere market proximity (0.067 AU of a trade body)
            // where the ship still flies freely and a brake still means something. Only NavLockedByDock
            // says the berth owns her, which is the state the owner was in at The Red Eye.
            clamped: NavLockedByDock,
            crossingTheVoid: _jumpInProgress,
            distance: dist,
            vicinityRadius: vicinity,
            relativeSpeed: RelativeSpeedTo(body),
            clampWindowSpeed: LongHaul.InsertionTargetSpeed);
    }

    // The per-frame arrival-brake law (called from OnTick after the alerts sweep). Drives ArrivalBrake.Advance
    // off the live window, squawks once on the rising edge, and clears the propulsive arm when the window shuts.
    private void UpdateArrivalBrakeGate(double nowMs)
    {
        CelestialBody? body = BrakeWindowBody();
        bool open = body is not null && BrakeWindowOpen(body);
        _brakeGate = ArrivalBrake.Advance(_brakeGate, open);

        if (!open)
        {
            _brakeArrivalBodyId = null; // window shut — a later arrival re-arms and asks afresh
        }
        else if (body is not null)
        {
            _brakeDestName = BodyName(body.Id);
        }

        // Rising edge (Dormant → Asking): shout once so the ask isn't missed in the heat of arrival.
        if (_brakeGate.Asking && !_brakeShowing)
        {
            SquawkNow(Parrot.Squawk.LongHaul, nowMs, force: true);
        }
        _brakeShowing = _brakeGate.Asking;
    }

    // The ask's spoken line for the card (propulsive quoted bill, its unfunded variant, or the aerobrake).
    private string ArrivalBrakeAskText()
    {
        // #962 · The aerobrake ask reads the quote the arm was FILED with, not whatever the body menu
        // happens to be caching. The old line asked AerobrakeMenuQuote, which is keyed to the currently-open
        // menu body and is null the moment that menu closes — which is how the owner got an offer to commit
        // "0 passes (≈0 p saved)". The filed quote is the trade the captain accepted; it is the one to speak.
        if (ArmedAerobrakeOffer() is { } q)
        {
            return ArrivalBrake.AskAerobrake(_brakeDestName, q.PassesNeeded, q.PulsesSaved);
        }

        int tank = LongHaulBudgetPulses();
        return _brakeQuotedPulses > tank
            ? ArrivalBrake.AskUnfunded(_brakeDestName, _brakeQuotedPulses, tank)
            : ArrivalBrake.AskPropulsive(_brakeDestName, _brakeQuotedPulses);
    }

    // CONSENT — fire the brake once. The Fired guard makes a double-click a no-op (no double-fire, no
    // double-bill). Propulsive: shed the live relative speed toward the clamp window, paying what the tank
    // holds through the one settle path (the pulses come off HERE, as the burn fires — the #268/#277
    // pay-as-it-delivers discipline, no second billing). Aerobrake: commit the pass; the live drag
    // consequence flies it (no propulsive charge). Idempotent and safe to call from the card button.
    private void FireArrivalBrake()
    {
        if (_brakeGate.HasFired || BrakeWindowBody() is not { } body)
        {
            return;
        }

        _brakeGate = ArrivalBrake.Fire(_brakeGate);

        if (BrakeIsAerobrake)
        {
            // The aerobrake was already armed and filed (#301); consenting just commits the pass to the live
            // skim consequence — no propulsive pulses. The window shuts as the pass bleeds the speed off.
            ShowPulseMessage(ArrivalBrake.AerobrakeCommitted(_brakeDestName));
            LogAutopilotEvent(ArrivalBrake.AerobrakeCommitted(_brakeDestName));
            return;
        }

        Vector2d beforeTheBrake = _ship.Velocity;
        double currentRel = RelativeSpeedTo(body);
        int tank = LongHaulBudgetPulses();
        ArrivalBrake.FireResult fire =
            ArrivalBrake.FireBrake(currentRel, LongHaul.InsertionTargetSpeed, _brakeQuotedPulses, tank);

        // Shed: scale the ship's velocity RELATIVE to the target down to the braked speed, then fold the
        // target's rail velocity back in — a retrograde insertion brake, the burn the quote priced.
        if (_ephemeris is not null && currentRel > 0)
        {
            Vector2d bodyVel = TransferMath.BodyVelocity(_ephemeris, body.Id, SimTime);
            Vector2d rel = _ship.Velocity - bodyVel;
            _ship = _ship with { Velocity = bodyVel + rel * (fire.ResultRelativeSpeed / currentRel) };
        }

        // The one charge, HERE, as the burn fires — the #268 pay-at-the-pump discipline (the departure burn
        // is charged the same way at engage). No ledger tab is opened, so nothing can settle it a second time.
        _reactionMassPulses = Math.Max(0, _reactionMassPulses - fire.PulsesSpent);

        bool hot = fire.PulsesSpent < _brakeQuotedPulses;
        string receipt = hot
            ? ArrivalBrake.FiredHot(_brakeDestName, fire.PulsesSpent)
            : ArrivalBrake.Fired(_brakeDestName, fire.PulsesSpent);
        ShowPulseMessage(receipt);
        LogAutopilotEvent(receipt);
        // #167 BURN KIND 9/9 - THE ARRIVAL BRAKE. Same as the match: it had the sound, it never had the
        // flame, and the sound was the same size whether she shed 2 pulses or 60.
        BurnFired(fire.PulsesSpent, _ship.Velocity - beforeTheBrake);
        _passDirty = true; // the brake changed the trajectory — re-plot
        ReprojectTrajectory();
    }

    // HOLD — the captain answers "I'll shed by hand". Nothing fires, no pulses move: the manual state is
    // exactly as it was. #962: this is an ANSWER, not a snooze — the gate goes to Held and stays there for
    // as long as this window lasts, so the card does not come back at the captain eight seconds later.
    // A genuinely new arrival shuts the window first, which resets the gate and asks afresh.
    private void DeclineArrivalBrake()
    {
        _brakeGate = ArrivalBrake.Hold(_brakeGate);
        _brakeShowing = false;
        ShowPulseMessage(ArrivalBrake.Declined(_brakeDestName));
    }
}

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

// Map.Combat — the War room: the mass driver and fire control, ordnance in flight, the heat
// that draws hunters, boarding and plunder, the BUSTED reckoning, and running dark. No home in
// #251's original twelve — it earned its own cabinet. Motion only.
public partial class Map
{

    private static string HeatFlames(int level) => level switch
    {
        <= 0 => "◌",
        1 => "🔥",
        2 => "🔥🔥",
        _ => "🔥🔥🔥",
    };

    private double? NearestHunterDistance()
    {
        double? best = null;
        foreach (HunterState hunter in _hunters)
        {
            double d = (hunter.State.Position - _ship.Position).Length;
            if (best is null || d < best)
            {
                best = d;
            }
        }

        return best;
    }

    private string? FireChipLine() => FireLocked
        ? $"🎖 FIRING in {Math.Max(0, (int)(_fireAtSimTime - SimTime))}s"
        : null;

    // PR-7: the gun deck — heat, hunters, war-room (vision par. 18).
    private HeatState _heat = HeatState.None;
    private readonly List<HunterState> _hunters = [];
    private int _hunterSeq;

    // PR-BUSTED: the catch economy of consequence (ruling §5). Hot cargo is stamped at theft time
    // when heat > 0 and launders when heat cools to 0; the confiscation reads this book. The parrot
    // quotes the current exposure at each upward heat crossing (_lastAnnouncedHeat is the edge). One
    // purchasable dice helper is shipped to prove the modifier seam (owner: many small helpers later).
    private readonly HotCargoLedger _hotCargo = new();
    private int _lastAnnouncedHeat;
    // #380 item 1 (audit's cheapest half): pre-seed the resurrection fiction one beat EARLIER. The first
    // time heat reaches 1 in a run, a one-time pulse advertises the brain-backup / pirate-insurance premise
    // BEFORE the death card ever needs it. One latch, run-scoped (this component is the game session).
    private bool _heatInsuranceAdvised;
    // #422 arc 2 — how many times this SESSION the captain has woken from a brain-backup. Gates the clinic's
    // "second page" (clinic-ledger surfaces only once you've woken here before — the fragment's own fiction).
    // Run-scoped; the durable "woken before" truth is the thread's retired-captains count, checked alongside.
    private int _rebirthsSeen;
    private bool _hasNetJammer;                       // "Boarding-nets jammer" — +2 on resist initiative
    private const int NetJammerPriceCr = 350;
    private BustedEncounter? _busted;                 // the open BUSTED pop-up, null when free

    // Rebirth taxes & the insurance seam (issues #227 + #225): resurrection CONSULTS this policy through
    // InsuranceRule today, so #227's vendor lane never reopens the catch code. Ships as Uninsured — the
    // rustbucket + full clinic bill. Plain/JSON-friendly for the #225 save vault.
    private PirateInsurance _insurance = PirateInsurance.Uninsured;
    private double _hiddenAtHavenSinceSimTime = double.NaN; // NaN = not currently hidden
    private static readonly RgbaColor HunterColor = new(255, 90, 90);
    private static readonly string[] HunterCallsigns =
        ["Debt Collector", "The Adjuster", "Repo Barque", "Fair Warning", "Lien Enforcer", "Underwriter's Claw"];
    private const int CaptureWarpCap = 10;            // the 60 s window must be holdable

    private double _captureProgress;                   // boarding progress fraction [0,1)
    private bool _captureEngaged;
    private string? _captureTargetCallsign;
    private double _captureRequiredSeconds;            // wall-clock secs for the CURRENT pass geometry

    // ---- M28 (Sunday PR-B): ordnance in flight — slugs and missiles ----
    private sealed class OrdnanceState
    {
        public required OrdnanceRound Round;
        public ShipState State;
        public double RemainingBudget;   // missiles only — Δv left for corrections
        public bool Spent;
    }

    private readonly List<OrdnanceState> _ordnance = [];
    private int _ordnanceSeq;
    private static readonly RgbaColor OrdnanceColor = new(255, 230, 150);

    /// <summary>Launches a round from the player's ship. Direction and speed usually come
    /// from a <see cref="FireControl.Solution"/>; nothing here re-checks them — the gun deck
    /// (PR-C) owns aiming policy, this owns flight and consequences.</summary>
    private void FireOrdnance(OrdnanceKind kind, Vector2d launchDirection, double muzzleSpeed,
        string? targetId, bool acrossTheBow = false)
    {
        var round = new OrdnanceRound($"ord-{_ordnanceSeq++}", kind, targetId, SimTime, acrossTheBow);
        _ordnance.Add(new OrdnanceState
        {
            Round = round,
            State = new ShipState(_ship.Position, _ship.Velocity + launchDirection * muzzleSpeed, SimTime),
            RemainingBudget = kind == OrdnanceKind.Missile ? OrdnanceRule.MissileDeltaVBudget : 0,
        });
        RendererInterop.PlayCue("fire"); // the driver's boom — a shot must SOUND like one (owner)
    }

    /// <summary>Steps every live round to the ship's sim time, guiding missiles and checking
    /// hits per integrator step with the closed-form segment minimum (Lab 06's no-tunneling
    /// rule) — the fast graze cannot slip between steps.</summary>
    private void StepOrdnance()
    {
        if (_ordnance.Count == 0)
        {
            return;
        }

        foreach (OrdnanceState round in _ordnance)
        {
            if (round.Spent)
            {
                continue;
            }

            NpcState? target = round.Round.TargetId is { } tid ? FindNpc(tid) : null;
            while (!round.Spent && round.State.SimTime < _ship.SimTime)
            {
                if (OrdnanceRule.Expired(round.Round, round.State.SimTime))
                {
                    round.Spent = true;
                    if (round.Round.TargetId is not null && !round.Round.AcrossTheBow)
                    {
                        PushNewsEvent(NewsWire.NewsEventKind.SlugMissed, NpcName(round.Round.TargetId));
                        // A miss must be as loud as a hit (owner: "know if we hit or missed").
                        ShowPulseMessage($"MISS — the {(round.Round.Kind == OrdnanceKind.Missile ? "missile" : "slug")} expired without contact ({NpcName(round.Round.TargetId)})");
                        RendererInterop.PlayCue("miss");
                    }

                    break;
                }

                if (round.Round.Kind == OrdnanceKind.Missile && target is { Active: true, Arrived: false })
                {
                    (round.State, double spent) = OrdnanceRule.Guide(
                        round.State, target.State, TrafficSchedule.NpcTimeStep, round.RemainingBudget);
                    round.RemainingBudget -= spent;
                }

                Vector2d before = round.State.Position;
                double tBefore = round.State.SimTime;
                round.State = _npcSimulator!.Step(round.State, null);

                // Hit anything in the way — not just the intended target (honest ballistics).
                foreach (NpcState npc in _npcStates)
                {
                    if (!npc.Active || npc.Arrived || npc.Disabled)
                    {
                        continue;
                    }

                    // The NPC's matching motion over this span, linearly reconstructed.
                    double dt = round.State.SimTime - tBefore;
                    Vector2d npcBefore = npc.State.Position - npc.State.Velocity * Math.Max(0, npc.State.SimTime - tBefore);
                    Vector2d npcAfter = npcBefore + npc.State.Velocity * dt;
                    if (round.Round.AcrossTheBow || !OrdnanceRule.StepHits(before, round.State.Position, npcBefore, npcAfter))
                    {
                        continue;
                    }

                    npc.Disabled = true;
                    round.Spent = true;
                    PushNewsEvent(NewsWire.NewsEventKind.SlugHit, npc.Ship.Callsign,
                        _nearestBody?.Name);
                    ShowPulseMessage($"🎯 DIRECT HIT — {npc.Ship.Callsign}'s sail is gone; she's ADRIFT and boardable");

                    // #528 · the CONSEQUENCE, shown: her hull intact, her windows still lit, her sail in ribbons.
                    // Cooled, so it cannot punctuate the same fight twice.
                    RaiseStoryBeat(StoryBeats.Beat.SailHoled, npc.Ship.Callsign);
                    RendererInterop.PlayCue("hit");
                    CompleteHuntQuests(npc.Ship.Id); // holing her settles a bar hunt contract too (M-Q1)

                    // Second hunt, step 4: holing the stubborn freighter's sail — the burn she'd have
                    // used to bolt never fires, so she drifts, catchable at last.
                    if (npc.Ship.Id == TrafficSchedule.StarterFreighterId)
                    {
                        AdvanceTutorial(StepHoleFreighter);
                    }
                    break;
                }

                // Hunters are fair game too — a holed hunter breaks off the chase for good.
                if (round.Spent || round.Round.AcrossTheBow)
                {
                    continue;
                }

                for (int h = 0; h < _hunters.Count; h++)
                {
                    HunterState hunter = _hunters[h];
                    if (hunter.BrokenOff || hunter.CaughtPlayer)
                    {
                        continue;
                    }

                    double dtH = round.State.SimTime - tBefore;
                    Vector2d hBefore = hunter.State.Position - hunter.State.Velocity * Math.Max(0, hunter.State.SimTime - tBefore);
                    Vector2d hAfter = hBefore + hunter.State.Velocity * dtH;
                    if (!OrdnanceRule.StepHits(before, round.State.Position, hBefore, hAfter))
                    {
                        continue;
                    }

                    _hunters[h] = hunter with { BrokenOff = true };
                    round.Spent = true;
                    PushNewsEvent(NewsWire.NewsEventKind.SlugHit, hunter.Callsign, _nearestBody?.Name);
                    ShowPulseMessage($"🎯 DIRECT HIT — {hunter.Callsign} breaks off, sail holed");
                    SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
                    RendererInterop.PlayCue("hit");
                    break;
                }
            }
        }

        _ordnance.RemoveAll(o => o.Spent);
    }

    // ---- M28 (Sunday PR-C): the Norden moment — gun-deck fire control ----
    private const double MaxMuzzleSpeed = 8000;      // the mass driver's top charge, m/s
    private const int SlugPulseCost = 2;             // reaction mass per shot
    private const int MissilePulseCost = 5;
    private const double FireLockLeadSeconds = 60;   // solution locks T-60 s — the Norden beat

    private double _fireAimOffsetSeconds = 3600;     // where on the prey's track we aim, from now
    private OrdnanceKind _fireKind = OrdnanceKind.Slug;
    private FireControl.Solution? _fireSolution;
    private IReadOnlyList<TrajectorySample> _fireSolutionPath = []; // the planned round's transfer, for the map
    private IReadOnlyList<TrajectorySample> _fireTargetPath = [];   // the prey's predicted track to t_hit
    private double _fireDispersionMeters;
    private Vector2d _fireAimPoint;
    // The captain's word (owner: "captain's panel must authorize pulling the trigger",
    // plus a standing "fire at will"). Warning shots need no authorization — they ARE the
    // way a captain talks without committing.
    private bool _fireAtWill;
    private bool _shotAuthorized;
    private bool WeaponsAuthorized => _fireAtWill || _shotAuthorized;

    private void AuthorizeShot()
    {
        _shotAuthorized = !_shotAuthorized;
        ShowPulseMessage(_shotAuthorized ? "CAPTAIN: next shot authorized" : "CAPTAIN: authorization withdrawn");
        if (_shotAuthorized)
        {
            AdvanceTutorial(StepAuthorizeShot); // second hunt, step 3: the captain's word
        }
        StateHasChanged();
    }

    /// <summary>
    /// WEAPONS TIGHT — the mirror of fire at will, and the order #538's hiding scene cannot work without. A
    /// deployed bot shoots what it sees and the tube gun never runs dry, so concealment is worthless while the
    /// captain's own automation is still making decisions.
    ///
    /// <para>It never disarms the captain: their trigger still works. A captain deciding to shoot and a machine
    /// deciding for them are different acts, which is the distinction the whole authority idiom rests on.</para>
    /// </summary>
    private bool _weaponsTight;

    /// <summary>Whether the captain's remote is open. Owner: <i>"Maybe small button to open the sentry settings
    /// pop-up 😎"</i>, and then the better name for it — <i>"Captain's remote"</i>.</summary>
    private bool _showCaptainsRemote;

    /// <summary>The captain's standing order for the boat. Owner: <i>"Shuttle should also power down to make it
    /// less of an anomaly."</i> A lit shuttle clamped to a dead hull is the anomaly an inspection team notices long
    /// before it notices a man.</summary>
    private bool _boatOrderedCold;

    /// <summary>
    /// Where she actually is between cold (0) and flying (1) — a dial, not a countdown, so that reversing the
    /// order keeps the progress already made. Owner: <i>"A timer on the map about the cool down and warm up"</i>,
    /// which only makes sense if both directions take real time.
    /// </summary>
    private double _boatWarmth = 1.0;

    private SilentRunning.BoatState BoatState => SilentRunning.StateOf(_boatWarmth, _boatOrderedCold);

    private double BoatSecondsLeft => SilentRunning.SecondsLeft(_boatWarmth, _boatOrderedCold);

    /// <summary>Whether her clocks are worth a line on the map — i.e. anything other than a warm boat, which is
    /// the boring default and would only clutter the strip.</summary>
    private bool BoatClockWorthShowing => BoatState != SilentRunning.BoatState.Warm;

    /// <summary>#736 · What the last switch on the remote answered, said on the remote. Three of the four
    /// switches here order something that is somewhere ELSE — bots across the field, a boat on the pad, a
    /// sounder in your own fist — so the sentence IS the feedback, and it was pulsed under the remote's own
    /// backdrop. Cleared whenever the handset is taken out or pocketed.</summary>
    private string? _remoteOutcome;

    private void OpenCaptainsRemote()
    {
        _showCaptainsRemote = true;
        _remoteOutcome = null;
        RendererInterop.PlayCue("board");
    }

    private void CloseCaptainsRemote()
    {
        _showCaptainsRemote = false;
        _remoteOutcome = null;
    }

    /// <summary>
    /// Reverse the standing order for the boat, from the remote, wherever the captain is standing. Owner:
    /// <i>"Shuttle warm up should work from remote"</i> — so this is the ONE place the boat is commanded, and the
    /// lock merely reports what it finds.
    /// </summary>
    private void ToggleBoatPower()
    {
        _boatOrderedCold = !_boatOrderedCold;

        SayItWhereTheyAreLooking(_boatOrderedCold ? SilentRunning.BoatDarkLine : SilentRunning.BoatWakingLine);
        LogAutopilotEvent(_boatOrderedCold
            ? $"🛸 Rigging the boat down — {SilentRunning.SecondsLeft(_boatWarmth, true):0} s to quiet."
            : $"🛸 Bringing the boat up — {SilentRunning.SecondsLeft(_boatWarmth, false):0} s to her hatch.");

        // Said once, the first time a captain orders her up while something else is already counting: this is the
        // scene the owner sketched — "self destruct tics down but shuttle still warms up".
        if (!_boatOrderedCold && !_twoClocksSaid && SomethingElseIsCountingDown)
        {
            _twoClocksSaid = true;
            LogAutopilotEvent(SilentRunning.TwoClocksLine);
        }

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private bool _twoClocksSaid;

    /// <summary>Whether a hull clock is already running against her warm-up — the overload on either keel.</summary>
    private bool SomethingElseIsCountingDown => _scuttleSecondsLeft is not null || _shipChargesSeconds is not null;

    /// <summary>
    /// Run the boat's dial in whichever direction her standing order points. Ticked wherever the captain is,
    /// because a captain who ordered her up and then went back for one more crate should find her open.
    /// </summary>
    private void AdvanceBoatSpinUp(double dtSeconds)
    {
        SilentRunning.BoatState was = BoatState;
        _boatWarmth = SilentRunning.AdvanceWarmth(_boatWarmth, _boatOrderedCold, dtSeconds);
        SilentRunning.BoatState now = BoatState;

        if (now == was)
        {
            return;
        }

        // Arriving at either end is worth saying out loud: one of them opens a hatch, the other closes it.
        ShowPulseMessage(SilentRunning.SpinUpLabel(_boatWarmth, _boatOrderedCold));
        if (now == SilentRunning.BoatState.Warm)
        {
            RendererInterop.PlayCue("door");
        }
    }

    /// <summary>Whether the boat will take the captain anywhere right now — asked by the lock before it offers to
    /// leave. This is where the warm-up is CHARGED, which is the owner's own framing.</summary>
    private bool BoatReadyToFly()
    {
        if (SilentRunning.ReadyToFly(_boatWarmth, _boatOrderedCold))
        {
            return true;
        }

        if (_boatOrderedCold)
        {
            // Asked for a ride while she is going or gone dark: waking her IS the answer, and it starts now.
            ToggleBoatPower();
            return false;
        }

        ShowPulseMessage(SilentRunning.NotARideYetLine(BoatSecondsLeft));
        RendererInterop.PlayCue("block");
        return false;
    }

    private void ToggleWeaponsTight()
    {
        _weaponsTight = !_weaponsTight;

        SayItWhereTheyAreLooking(_weaponsTight ? SentryBot.WeaponsTightLine : SentryBot.WeaponsFreeLine);
        LogAutopilotEvent(_weaponsTight
            ? "🤖 WEAPONS TIGHT ordered — bots and the tube gun stand down."
            : "🤖 Weapons free — the bots have their arcs back.");

        // Said once, on the way in: the order that hides you also stops defending you.
        if (_weaponsTight)
        {
            LogAutopilotEvent(SentryBot.TightIsAlsoUndefendedLine);
        }

        RendererInterop.PlayCue("board");
        StateHasChanged();
    }

    private void ToggleFireAtWill()
    {
        _fireAtWill = !_fireAtWill;
        ShowPulseMessage(_fireAtWill ? "CAPTAIN: weapons free — fire at will" : "CAPTAIN: weapons hold");
        if (_fireAtWill)
        {
            AdvanceTutorial(StepAuthorizeShot); // a standing order satisfies the captain's-word step too
        }
        StateHasChanged();
    }

    // The magazine (owner: rounds are BOUGHT once spent). Warning shots burn a slug too.
    private int _slugAmmo = 12;
    private int _missileAmmo = 4;

    private void BuyAmmo(OrdnanceKind kind)
    {
        (int price, int count) = kind == OrdnanceKind.Missile ? (500, 2) : (300, 6);
        if (!_docked || _credits < price)
        {
            return;
        }

        _credits -= price;
        if (kind == OrdnanceKind.Missile)
        {
            _missileAmmo += count;
        }
        else
        {
            _slugAmmo += count;
        }

        ShowPulseMessage($"Dockside resupply: +{count} {(kind == OrdnanceKind.Missile ? "missiles" : "slugs")} ({price} cr)");
        StateHasChanged();
    }

    private double _fireAtSimTime = double.NaN;      // NaN = nothing locked
    private string? _fireTargetId;
    private string? _fireTip;                        // F6: the solver as flight instructor
    private int _revealedIterations;                 // the CALCULATING… reveal cursor
    private double _lastRevealMs;
    private double _slewBearingRad;                  // cosmetic auto-slew after the shot
    private double _slewUntilSimTime = double.NaN;

    private bool FireLocked => !double.IsNaN(_fireAtSimTime);

    /// <summary>Current straight-line distance to the interest target — the fire panel's honest
    /// "can this round even get there" hint.</summary>
    private double? InterestDistanceNow() =>
        InterestTargetState() is { } state ? (state.Position - _ship.Position).Length : null;

    /// <summary>
    /// The kinematic firing window, closed-form: flight times t where a straight muzzle-speed
    /// shot can cancel the relative drift, |Δr/t + Δv| ≤ v_muzzle. This is the number the
    /// panel must SIGNAL (owner): an 8 km/s gun against 30 km/s orbits means most aims are
    /// infeasible, and the live test showed the naive distance/muzzle hint off by 25×.
    /// </summary>
    private (double MinToF, double MaxToF)? StraightShotWindow()
    {
        if (InterestTargetState() is not { } target)
        {
            return null;
        }

        Vector2d dr = target.Position - _ship.Position;
        Vector2d dv = target.Velocity - _ship.Velocity;
        double a = dr.LengthSquared;
        double b = 2 * dr.Dot(dv);
        double c = dv.LengthSquared - MaxMuzzleSpeed * MaxMuzzleSpeed;
        double disc = b * b - 4 * a * c;
        if (disc < 0 || a <= 0)
        {
            return null; // the drift outruns the muzzle in every direction — no straight shot
        }

        double sq = Math.Sqrt(disc);
        double uHigh = (-b + sq) / (2 * a); // u = 1/t: higher u = shorter flight
        double uLow = (-b - sq) / (2 * a);
        if (uHigh <= 0)
        {
            return null;
        }

        double minToF = 1 / uHigh;
        double maxToF = c < 0 || uLow <= 0 ? double.PositiveInfinity : 1 / uLow;
        return (minToF, maxToF);
    }

    private string? StraightWindowText() => StraightShotWindow() is { } w
        ? $"+{FormatFlightTime(w.MinToF)}{(double.IsPositiveInfinity(w.MaxToF) ? " or later" : $" … +{FormatFlightTime(w.MaxToF)}")}"
        : null;

    /// <summary>Owner: "(unless there is something blocking the shot in between)" — walk the
    /// solved transfer's segments against every body's disc (Sun included: no shooting through
    /// the star). Segment-vs-point with the body at the segment's mid-time; coarse for long
    /// segments, honest enough to name the blocker.</summary>
    private string? _fireBlockedBy;

    private string? FirePlanBlockedBy()
    {
        if (_ephemeris is null || _fireSolutionPath.Count < 2)
        {
            return null;
        }

        for (int i = 1; i < _fireSolutionPath.Count; i++)
        {
            Vector2d a = _fireSolutionPath[i - 1].Position;
            Vector2d b = _fireSolutionPath[i].Position;
            double tMid = (_fireSolutionPath[i - 1].SimTime + _fireSolutionPath[i].SimTime) / 2;
            Vector2d ab = b - a;
            double abLenSq = ab.LengthSquared;
            foreach (CelestialBody body in _ephemeris.Bodies)
            {
                if (body.BodyRadius <= 0)
                {
                    continue;
                }

                Vector2d center = _ephemeris.Position(body.Id, tMid);
                double t = abLenSq > 0 ? Math.Clamp((center - a).Dot(ab) / abLenSq, 0, 1) : 0;
                if ((a + ab * t - center).LengthSquared <= body.BodyRadius * body.BodyRadius)
                {
                    return body.Name;
                }
            }
        }

        return null;
    }

    /// <summary>Live rounds for the war-room tracker (owner: "shots / missiles away is also
    /// good to track").</summary>
    private IReadOnlyList<Stations.WarRoom.LiveRound> LiveRounds()
    {
        if (_ordnance.Count == 0)
        {
            return [];
        }

        var list = new List<Stations.WarRoom.LiveRound>();
        foreach (OrdnanceState round in _ordnance)
        {
            if (round.Spent)
            {
                continue;
            }

            double remaining = OrdnanceRule.LifetimeSeconds(round.Round.Kind)
                - (round.State.SimTime - round.Round.LaunchedAtSimTime);
            list.Add(new Stations.WarRoom.LiveRound(
                round.Round.Kind == OrdnanceKind.Missile ? "missile" : "slug",
                round.Round.TargetId is { } targetId ? NpcName(targetId) : "warning shot",
                Math.Max(0, remaining)));
        }

        return list;
    }

    private bool CanWarnInterest()
    {
        if (_interestTargetId is null)
        {
            return false;
        }

        if (FindNpc(_interestTargetId) is { Active: true, Arrived: false, Disabled: false } npc)
        {
            return !npc.Ship.IsPod && EncounterRule.InWeaponRange(_ship, npc.State);
        }

        // A hunter is a legitimate warning-shot target too — fire near it and its nerve erodes.
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId && !hunter.CaughtPlayer && !hunter.BrokenOff)
            {
                return EncounterRule.InWeaponRange(_ship, hunter.State);
            }
        }

        return false;
    }

    private double? PlannedImpactEta() =>
        _fireSolutionPath.Count > 1 && _fireSolutionPath[^1].SimTime > SimTime
            ? _fireSolutionPath[^1].SimTime - SimTime
            : null;

    /// <summary>The interest target's REAL maneuvering ability, for the dispersion cone — a
    /// depot on rails or a mass-driver pod cannot burn at all, so a 70-day shot at one carries
    /// meters-per-second sigma, not the ±dozens-of-AU the default crewed budget implied (the
    /// long-shot acceptance run caught "±38 AU" on a rails depot).</summary>
    private double InterestManeuverBudget() =>
        _interestTargetId is not null && FindNpc(_interestTargetId) is { } npc
            ? npc.Ship.ManeuverBudget
            : NpcShip.DefaultManeuverBudget;

    private (Vector2d Position, Vector2d Velocity)? InterestTargetState()
    {
        if (_interestTargetId is null)
        {
            return null;
        }

        if (FindNpc(_interestTargetId) is { Active: true, Arrived: false } npc)
        {
            return (npc.State.Position, npc.State.Velocity);
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId)
            {
                return (hunter.State.Position, hunter.State.Velocity);
            }
        }

        return null;
    }

    /// <summary>The longest shot the selected round can FLY: its lifetime, minus the lock
    /// lead. Beyond this the ordnance evaporates mid-flight — the silent guaranteed miss the
    /// old fixed 48 h slider quietly allowed (slug lived 6 h!).</summary>
    private double MaxFireAimOffsetSeconds => OrdnanceRule.LifetimeSeconds(_fireKind) - FireLockLeadSeconds;

    private void SetFireAimOffset(double seconds)
    {
        _fireAimOffsetSeconds = Math.Clamp(seconds, 600, MaxFireAimOffsetSeconds);
        if (!FireLocked)
        {
            _fireSolution = null; // a moved aim point voids an unlocked solution
        }
    }

    private void SetFireKind(OrdnanceKind kind)
    {
        _fireKind = kind;
        // Switching missile → slug with a weeks-long aim must pull the aim back inside what
        // the slug can actually fly.
        _fireAimOffsetSeconds = Math.Clamp(_fireAimOffsetSeconds, 600, MaxFireAimOffsetSeconds);
        if (!FireLocked)
        {
            _fireSolution = null;
        }
    }

    /// <summary>
    /// The aim-solution fork (core-gravity review, 2026-07-06): a freighter's future is gravity
    /// (PathPredictor dead-reckons it through the Simulator — gravity is public knowledge), but a
    /// hunter's future is the PURSUIT LAW: no gravity, +0.5 m/s² toward us every quantum. The old
    /// gravity dead-reckon put a hunter aim point ~½·a·τ² off — 13,000 km on a 2 h flight against
    /// the 5e5 m hit radius, a structural miss beyond point-blank. The fork replays the pursuit law
    /// itself (EncounterRule.PredictHunterPath) against our own plotted course; both arms honor the
    /// same PredictedPath contract, so SOLVE, the window scan, the orrery backdrop and the
    /// dispersion cone all stay target-agnostic. The hunter cone keeps budgets 0 — the pursuit law
    /// is known exactly, pod-thin; what really bends a hunter shot is leaving your own plot
    /// mid-flight (the collector chases the real you, not the plan).
    /// </summary>
    private PredictedPath PredictInterestPath((Vector2d Position, Vector2d Velocity) target, double horizonSeconds)
    {
        var observation = new Observation(_interestTargetId!, SimTime, target.Position, target.Velocity);
        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId)
            {
                return new PredictedPath(observation,
                    EncounterRule.PredictHunterPath(hunter, PlayerPathForPrediction(), horizonSeconds),
                    ManeuverBudget: 0, ImpulseBudget: 0);
            }
        }

        return PathPredictor.Predict(_ephemeris!, observation, null, horizonSeconds,
            maneuverBudget: InterestManeuverBudget());
    }

    /// <summary>What a pursuer will actually chase: the plotted course — except BERTHED, where the
    /// plot shows a gravity coast the clamps will never allow; ride the dock's rails instead. The
    /// no-plot fallback (a beat before the first projection lands) is a straight coast.</summary>
    private IReadOnlyList<TrajectorySample> PlayerPathForPrediction()
    {
        if (_dockedHavenId is { } dockId && _ephemeris is not null)
        {
            double horizon = Math.Max(CurrentPlotHorizonSeconds, 2 * 86400);
            const int knots = 128;
            var docked = new List<TrajectorySample>(knots + 1);
            for (int k = 0; k <= knots; k++)
            {
                double t = SimTime + horizon * k / knots;
                docked.Add(new TrajectorySample(t, _ephemeris.Position(dockId, t) + _dockOffset));
            }

            return docked;
        }

        return _samples.Count >= 2
            ? _samples
            : [new TrajectorySample(SimTime, _ship.Position), new TrajectorySample(SimTime + 3600, _ship.Position + _ship.Velocity * 3600)];
    }

    /// <summary>The Norden beat: predict the mark, coast the shooter to T+60 s, run the
    /// shooting method, and — if it converges — lock, count down, auto-fire.</summary>
    private void ComputeFiringSolution()
    {
        if (_ephemeris is null || _simulator is null || FireLocked || InterestTargetState() is not { } target)
        {
            return;
        }

        _fireAimOffsetSeconds = Math.Clamp(_fireAimOffsetSeconds, 600, MaxFireAimOffsetSeconds);

        PredictedPath predicted = PredictInterestPath(target, _fireAimOffsetSeconds);
        _fireAimPoint = predicted.Samples[^1].Position;
        double tHit = SimTime + _fireAimOffsetSeconds;
        _fireDispersionMeters = predicted.HalfWidthAt(tHit);

        ShipState shooterAtFire = _simulator.RunAdaptive(_ship, FireLockLeadSeconds);
        FireControl.Solution solution = FireControl.Solve(_simulator, shooterAtFire, MaxMuzzleSpeed, _fireAimPoint, tHit);
        _fireSolution = solution;
        _fireTargetId = _interestTargetId;
        _revealedIterations = 0;
        _lastRevealMs = 0;

        if (solution.Converged)
        {
            // Computing is SAFE — nothing flies until the gunner presses FIRE (owner: locking
            // a solution is often the THREAT in a piracy stop; auto-firing it is the "oops").
            _fireAtSimTime = double.NaN;
            _fireTip = FireTip(solution);
            // The orrery view (owner + Gemini consult): fly the solved round once more and keep
            // the samples — the war-room backdrop draws the whole transfer, aim point and
            // dispersion on the live map, because a 100 M km shot cannot live inside a 5 M km
            // tactical circle.
            var round = new ShipState(
                shooterAtFire.Position,
                shooterAtFire.Velocity + solution.LaunchDirection * solution.MuzzleSpeed,
                shooterAtFire.SimTime);
            _fireSolutionPath = _simulator.ProjectAdaptive(round, null, solution.TimeOfFlightSeconds,
                maxSamples: Math.Max(64, (int)(solution.TimeOfFlightSeconds / 3600) + 16));
            _fireTargetPath = predicted.Samples;
            _fireBlockedBy = FirePlanBlockedBy();
            ShowPulseMessage("CALCULATING FIRING SOLUTION…");
            SquawkNow(Parrot.Squawk.FiringSolution, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            _fireAtSimTime = double.NaN;
            _fireTip = null;
            _fireSolutionPath = [];
            _fireTargetPath = [];
            _fireBlockedBy = null;
            ShowPulseMessage("No firing solution — beyond the driver's reach at that moment");
        }

        StateHasChanged();
    }

    /// <summary>
    /// The one-button gunner (owner: "why does it not just point nose at this and fire with
    /// advance?"). Jump the aim to the shortest flight the round can physically make to the
    /// target's current range — switching to the missile when the slug can't live that long —
    /// solve there, and if the geometry refuses, sweep the windows. Computes only; FIRE stays
    /// a separate deliberate press.
    /// </summary>
    private void AutoAim()
    {
        if (FireLocked || InterestDistanceNow() is not { } distance)
        {
            return;
        }

        // The kinematic window bounds everything: outside it no straight shot exists at ANY
        // aim, and solving there just freezes the deck for nothing (the live test burned two
        // minutes escalating through six hopeless solves).
        if (StraightShotWindow() is not { } window)
        {
            ShowPulseMessage("No straight shot at any flight time — the drift outruns the muzzle; 🔭 sweeping for a gravity window");
            ScanFiringWindows();
            return;
        }

        double floorToF = window.MinToF * 1.03 + FireLockLeadSeconds;
        if (_fireKind == OrdnanceKind.Slug && floorToF > OrdnanceRule.SlugLifetimeSeconds - FireLockLeadSeconds)
        {
            _fireKind = OrdnanceKind.Missile;
            ShowPulseMessage("That window needs the missile — switched");
        }

        // A cross-system window means MINUTES of synchronous WASM solving per attempt (the
        // live test froze the deck ~10 min escalating blind). Auto-aim's job is the SIGNAL:
        // put the aim inside the window instantly and let SOLVE be the deliberate press.
        if (floorToF > 20 * 86400)
        {
            _fireAimOffsetSeconds = Math.Clamp(floorToF, 600, MaxFireAimOffsetSeconds);
            _fireSolution = null;
            ShowPulseMessage($"Aim set inside the window (+{FormatFlightTime(_fireAimOffsetSeconds)}) — press SOLVE (cross-system solves take a while)");
            StateHasChanged();
            return;
        }

        double ceiling = Math.Min(
            double.IsPositiveInfinity(window.MaxToF) ? MaxFireAimOffsetSeconds : window.MaxToF * 0.97,
            MaxFireAimOffsetSeconds);
        foreach (double factor in (double[])[1.0, 1.15, 1.5, 2.4])
        {
            double offset = Math.Clamp(floorToF * factor, 600, Math.Max(ceiling, 600));
            _fireAimOffsetSeconds = offset;
            ComputeFiringSolution();
            if (_fireSolution is { Converged: true } && _fireBlockedBy is null)
            {
                return;
            }

            if (offset >= ceiling)
            {
                break;
            }
        }

        // The straight window refused (gravity bends it away) — sweep as a last resort.
        ScanFiringWindows();
    }

    /// <summary>The deliberate FIRE press: re-solve at the current clock (no stale locks) and
    /// only then arm the T−60 s auto-release. Until this, everything upstream is just aiming.</summary>
    private void ArmFire()
    {
        if (FireLocked)
        {
            return;
        }

        if (!WeaponsAuthorized)
        {
            ShowPulseMessage("HOLD — the captain has not authorized the shot (desk 0)");
            return;
        }

        if (_fireKind == OrdnanceKind.Missile ? _missileAmmo <= 0 : _slugAmmo <= 0)
        {
            ShowPulseMessage($"Magazine empty — buy {(_fireKind == OrdnanceKind.Missile ? "missiles" : "slugs")} dockside");
            return;
        }

        ComputeFiringSolution();
        if (_fireSolution is { Converged: true } && _fireBlockedBy is null)
        {
            _fireAtSimTime = SimTime + FireLockLeadSeconds;
            _shotAuthorized = false; // one shot per captain's word (fire-at-will stands)
            ShowPulseMessage("BARREL LOCKED — round away in 60 s (scrub to abort)");

            // #528 · THE FIRST ROUND THIS CAPTAIN EVER FIRED. A smuggler becomes a pirate exactly once, and it
            // used to be a status line that faded in a second and a half. A PLATE rather than a card, because
            // this happens mid-fight and must not take the keyboard.
            RaiseStoryBeat(StoryBeats.Beat.FirstShotFired);
        }
        else if (_fireBlockedBy is not null)
        {
            ShowPulseMessage($"HOLD — {_fireBlockedBy} blocks the transfer; find another window");
        }
    }

    /// <summary>
    /// The porkchop assist (long-shots PR): a FIXED aim time is only feasible in certain launch
    /// windows — orbital mechanics, not the driver, decides when the geometry aligns. Sweep
    /// candidate aim times with the cheap seed probe, jump the slider to the best window and
    /// immediately run the full solve there.
    /// </summary>
    private void ScanFiringWindows()
    {
        if (_ephemeris is null || _simulator is null || FireLocked || InterestTargetState() is not { } target)
        {
            return;
        }

        PredictedPath predicted = PredictInterestPath(target, MaxFireAimOffsetSeconds);
        ShipState shooterAtFire = _simulator.RunAdaptive(_ship, FireLockLeadSeconds);

        double bestOffset = _fireAimOffsetSeconds;
        double bestMiss = double.MaxValue;
        const int probes = 7;
        // Grid floor = the shortest PHYSICAL flight to the target's current range — probing
        // aim times the round can't reach is wasted flights (and made knife-fight scans blind).
        double minOffset = Math.Clamp(
            (target.Position - _ship.Position).Length / MaxMuzzleSpeed * 0.8, 900, MaxFireAimOffsetSeconds / 4);
        double logSpan = Math.Log(MaxFireAimOffsetSeconds * 0.95 / minOffset);
        for (int i = 0; i < probes; i++)
        {
            double offset = minOffset * Math.Exp(logSpan * i / (probes - 1));
            double timeOfFlight = offset - FireLockLeadSeconds;
            if (timeOfFlight <= 0)
            {
                continue;
            }

            Vector2d aim = SamplePositionAtTime(predicted.Samples, SimTime + offset);
            (_, _, double miss) = FireControl.ProbeSeed(_simulator, shooterAtFire, MaxMuzzleSpeed, aim, timeOfFlight);
            if (miss < bestMiss)
            {
                (bestMiss, bestOffset) = (miss, offset);
            }
        }

        _fireAimOffsetSeconds = Math.Clamp(bestOffset, 600, MaxFireAimOffsetSeconds);
        ShowPulseMessage($"Window scan: best geometry at +{FormatFlightTime(bestOffset)} — solving there");
        ComputeFiringSolution();

        if (_fireSolution is { Converged: false } && _fireKind == OrdnanceKind.Slug)
        {
            ShowPulseMessage("No slug window inside its 2-day legs — switch to the MISSILE and scan again");
        }
    }

    private void CancelFiringSolution()
    {
        _fireAtSimTime = double.NaN;
        _fireSolution = null;
        _fireSolutionPath = [];
        _fireTargetPath = [];
        _fireBlockedBy = null;
        ShowPulseMessage("Firing solution scrubbed");
        StateHasChanged();
    }

    /// <summary>Auto-fires the locked solution the moment the countdown expires.</summary>
    private void CheckLockedFire()
    {
        if (!FireLocked || _fireSolution is not { } solution || SimTime < _fireAtSimTime)
        {
            return;
        }

        int cost = _fireKind == OrdnanceKind.Missile ? MissilePulseCost : SlugPulseCost;
        if (cost > _reactionMassPulses)
        {
            ShowPulseMessage($"No mass for the shot ({cost} pulses) — solution scrubbed");
            CancelFiringSolution();
            return;
        }

        if (_fireKind == OrdnanceKind.Missile ? _missileAmmo <= 0 : _slugAmmo <= 0)
        {
            ShowPulseMessage("Magazine empty — the shot is scrubbed");
            CancelFiringSolution();
            return;
        }

        _reactionMassPulses -= cost;
        if (_fireKind == OrdnanceKind.Missile)
        {
            _missileAmmo--;
        }
        else
        {
            _slugAmmo--;
        }

        FireOrdnance(_fireKind, solution.LaunchDirection, solution.MuzzleSpeed, _fireTargetId);
        _slewBearingRad = solution.BearingRad;
        _slewUntilSimTime = SimTime + 120; // the barrel swings back — control returns
        ShowPulseMessage($"ROUND AWAY — flight time {FormatDuration(solution.TimeOfFlightSeconds)} 🎯");
        _fireAtSimTime = double.NaN;
        _fireSolution = null;
        // Keep the planned transfer + aim point drawn while the round flies it — the whole
        // point of a weeks-long shot is watching the real slug ride the calculated line.
        StateHasChanged();
    }

    /// <summary>F6 — the solver as flight instructor: every locked solution teaches the lead.</summary>
    private string FireTip(FireControl.Solution solution)
    {
        Vector2d toAim = _fireAimPoint - _ship.Position;
        double direct = Math.Atan2(toAim.Y, toAim.X);
        double lead = (solution.BearingRad - direct) * 180.0 / Math.PI;
        while (lead > 180) { lead -= 360; }
        while (lead < -180) { lead += 360; }
        return $"Gunner's lesson: the round leads the mark by {Math.Abs(lead):F1}° and flies " +
            $"{FormatDuration(solution.TimeOfFlightSeconds)} — aim where they WILL be, never where " +
            "they are. Flying your own intercepts works exactly the same way.";
    }

    private static readonly string[] PlunderLines =
    [
        "— not a drop of blood spilled",
        "— the crew is singing already",
        "— rum for everyone tonight",
        "— quartermaster's grin says it all",
        "— droids stack it neat as you please",
    ];

    // M29: the fake beacon's ghost, shown to US so the captain always knows what story the
    // beacon is telling — a hollow marker flying the abandoned course.
    private void DrawBeaconGhost()
    {
        if (_transponderMode != TransponderMode.Fake || _beaconGhost is not { } ghost
            || !LayerVisible("traffic.beacons")) // 🗺 Layers (#405): Traffic → Beacons
        {
            return;
        }

        (float gx, float gy) = _camera.WorldToScreen(ghost.Position);
        if (gx < -40 || gy < -40 || gx > _viewportWidth + 40 || gy > _viewportHeight + 40)
        {
            return;
        }

        _renderer!.DrawCircle(gx, gy, 6, null, GhostShipColor, 1.5f);
        _renderer.DrawText(gx + 9, gy + 3, "🎭 beacon ghost", GhostShipColor, "10px monospace", TextAlign.Left);
    }

    // ---- The orrery view (owner + Gemini consult, 2026-07-05): the long shot on the map ----
    // A 100 M km shot cannot live inside a 5 M km tactical circle: when the war room is up, the
    // live map behind it carries the whole geometry — the prey's predicted track to t_hit, the
    // planned round's transfer, the aim point and the honest dispersion circle.

    private static readonly RgbaColor FirePlanColor = new(255, 120, 120, 200);
    private static readonly RgbaColor FirePlanTargetColor = new(200, 120, 255, 120);
    private static readonly RgbaColor FirePlanDispersionColor = new(255, 120, 120, 60);

    private void DrawFirePlan()
    {
        // The interest target gets brackets even before a solution exists — the war room's
        // subject is never invisible on its own backdrop again.
        if (_interestTargetId is not null && InterestTargetState() is { } interest)
        {
            (float ix, float iy) = _camera.WorldToScreen(interest.Position);
            DrawCornerBrackets(ix, iy, 12f, FirePlanColor with { A = 160 });
            _renderer!.DrawText(ix + 15, iy - 8, $"🎯 {InterestTargetName() ?? _interestTargetId}",
                FirePlanColor with { A = 190 }, "11px sans-serif", TextAlign.Left);

            // The owner's ask, verbatim: "a graphical line from my ship to the target… showing
            // distance and direct shot options" — the raw geometry, before any solution.
            (float px, float py) = _camera.WorldToScreen(_ship.Position);
            Span<float> ray = stackalloc float[4];
            ray[0] = px; ray[1] = py; ray[2] = ix; ray[3] = iy;
            _renderer.DrawPolyline(ray, FirePlanColor with { A = 70 }, 1f);
            double distance = (interest.Position - _ship.Position).Length;
            double shortestFlight = distance / MaxMuzzleSpeed;
            string reachNote = shortestFlight > OrdnanceRule.SlugLifetimeSeconds ? "missile territory" : "slug or missile";
            _renderer.DrawText((px + ix) / 2, (py + iy) / 2 - 6,
                $"{FormatDistance(distance)} · shortest flight ≈ {FormatFlightTime(shortestFlight)} · {reachNote}",
                FirePlanColor with { A = 150 }, "11px sans-serif", TextAlign.Center);
        }

        // The fired round rides the drawn plan — mark the LIVE bullet loudly on this desk
        // (owner: "schedule shot… then its position should be tracked on this view").
        foreach (OrdnanceState round in _ordnance)
        {
            if (round.Spent)
            {
                continue;
            }

            (float ox, float oy) = _camera.WorldToScreen(round.State.Position);
            _renderer!.DrawCircle(ox, oy, 6f, null, OrdnanceColor, 1.5f);
            _renderer.DrawText(ox + 9, oy + 4,
                round.Round.Kind == OrdnanceKind.Missile ? "missile" : "slug",
                OrdnanceColor with { A = 190 }, "11px sans-serif", TextAlign.Left);
        }

        if (_fireSolutionPath.Count < 2)
        {
            return;
        }

        DrawWorldPolyline(_fireTargetPath, FirePlanTargetColor, 1f);
        DrawWorldPolyline(_fireSolutionPath, FirePlanColor, 1.6f);

        (float ax, float ay) = _camera.WorldToScreen(_fireAimPoint);
        float dispersionPx = (float)Math.Max(4, _fireDispersionMeters / _camera.MetersPerPixel);
        _renderer!.DrawCircle(ax, ay, dispersionPx, FirePlanDispersionColor with { A = 18 }, FirePlanDispersionColor, 1f);
        Span<float> cross = stackalloc float[4];
        cross[0] = ax - 6; cross[1] = ay - 6; cross[2] = ax + 6; cross[3] = ay + 6;
        _renderer.DrawPolyline(cross, FirePlanColor, 1.5f);
        cross[0] = ax - 6; cross[1] = ay + 6; cross[2] = ax + 6; cross[3] = ay - 6;
        _renderer.DrawPolyline(cross, FirePlanColor, 1.5f);
        double impactIn = _fireSolutionPath[^1].SimTime - SimTime;
        _renderer.DrawText(ax + 10, ay + 14,
            $"impact {(impactIn > 0 ? $"in {FormatFlightTime(impactIn)}" : "point")} · ±{FormatDistance(_fireDispersionMeters)}",
            FirePlanColor with { A = 190 }, "11px sans-serif", TextAlign.Left);
    }

    /// <summary>Clicking a hunter on the map locks it as the war room's interest target — the same
    /// lock as the War Room 🎯 button (corner brackets, a firing solution, a warning shot that
    /// breaks its nerve). Clicking the locked hunter again clears the lock.</summary>
    private void MarkHunterOfInterest(string hunterId)
    {
        bool wasLocked = _interestTargetId == hunterId;
        SetInterestTarget(hunterId); // toggles; also nulls the stale intercept and re-scans the pass
        string name = _hunters.FirstOrDefault(h => h.Id == hunterId).Callsign ?? "the hunter";
        ShowPulseMessage(wasLocked
            ? $"Lock released — {name} is no longer the war room's mark"
            : $"🎯 {name} marked — the war room has the fire-control lock; a warning shot will test its nerve");
    }

    // ---- M29: the transponder (the AIS of the solar lanes) ----
    private TransponderMode _transponderMode = TransponderMode.On; // honest traffic runs lit
    private ShipState? _beaconGhost;

    private void SetTransponder(TransponderMode mode)
    {
        if (mode == _transponderMode)
        {
            return;
        }

        // Entering FAKE snapshots the innocent course at the moment of the lie; the ghost
        // coasts it from here. Leaving FAKE burns the ghost.
        _beaconGhost = mode == TransponderMode.Fake ? _ship : null;
        _transponderMode = mode;
        switch (mode)
        {
            case TransponderMode.Dark:
                SquawkNow(Parrot.Squawk.RunningDark, _lastTimestampMs ?? 0, force: true);
                break;
            case TransponderMode.Fake:
                SquawkNow(Parrot.Squawk.FalseColors, _lastTimestampMs ?? 0, force: true);
                break;
        }

        StateHasChanged();
    }

    /// <summary>The one target the tactical UI is about: the war-room interest first, else the
    /// scope selection. Lives independently of the NAV destination — you can be bound for
    /// Mercury and still keep a dossier open on a hauler.</summary>
    private string? TacticalTargetId => _interestTargetId ?? _selectedTargetId;

    private void InterestFromMenu(string id)
    {
        if (_interestTargetId != id)
        {
            SetInterestTarget(id);
        }

        ShowPulseMessage("Target of interest — the war room runs the intercept clock and firing solutions ⚔");
        CloseShipMenu();
        StateHasChanged();
    }

    // ---- M6: capture, dock, economy, tutorial ----

    // The current boarding candidate: selected, live, observed, not already emptied.
    private NpcState? SelectedCaptureTarget()
    {
        if (_selectedTargetId is null)
        {
            return null;
        }

        NpcState? npc = FindNpc(_selectedTargetId);
        if (npc is null || !npc.Active || npc.Arrived || npc.Boarded || !npc.CurrentlyObserved)
        {
            return null;
        }

        return npc;
    }

    // Boarding shuttles (owner's design): while the window holds, progress accrues at a rate
    // set by the instant's geometry — RequiredSecondsFor grows with stand-off distance and
    // relative speed, so a tight rendezvous boards in ~30 s while a sloppy pass needs a window
    // its own geometry rarely grants. Progress is a fraction; boarding at 1.
    private void UpdateCapture(double dtRealSeconds)
    {
        NpcState? npc = SelectedCaptureTarget();
        bool inWindow = npc is not null && CaptureRule.IsInWindow(_ship, npc.State);

        // #177/#178 — hostile acts are NEVER automatic. The owner got robbed-by-accident when
        // autopilot flew him through a moon and a selected DEPOT slid into the boarding window.
        // The gate is structural: proximity alone can only ever surface an OPPORTUNITY; a felony
        // needs the captain's word (AuthorizePlunder), exactly like the gun needs it to fire.
        CaptureRule.BoardingIntent intent =
            CaptureRule.EvaluateBoarding(inWindow, npc?.Ship.Id, _plunderAuthorizedTargetId);

        if (inWindow)
        {
            AdvanceTutorial(2); // step 3: window first engages (opportunity or authorized)
        }

        // Resolve the plunder OFFER honouring a stand-down: a declined hull stays silent for the
        // rest of this pass (the every-frame tick would otherwise re-raise the prompt immortally);
        // exiting the window or selecting a new hull re-arms it. The pure helper owns the re-arm.
        CaptureRule.PlunderPrompt prompt =
            CaptureRule.ResolvePlunderPrompt(intent, npc?.Ship.Id, _plunderDeclinedTargetId);
        _plunderDeclinedTargetId = prompt.DeclinedTargetId;

        if (intent == CaptureRule.BoardingIntent.Authorized)
        {
            _plunderOpportunityTargetId = null;

            // Boarding shuttles fly in REAL time, warp be damned (M14): passive progress
            // accrues at wall-clock rate, so warping doesn't fast-forward a boarding and the
            // deckhand's wait is real — while the captain who flies the run docks in seconds.
            // PR-7: a compliant (warned or bribed) target has heaved to — shuttles cross in half
            // the time (ComplianceBoardingFactor). Stubborn ships and un-warned ones get no break.
            double requiredSeconds = CaptureRule.RequiredSecondsFor(_ship, npc!.State)
                * (IsCompliantBoarding(npc) ? EncounterRule.ComplianceBoardingFactor : 1.0);
            _captureProgress += Math.Clamp(dtRealSeconds, 0, 0.1) / requiredSeconds;
            _captureEngaged = true;
            _captureTargetCallsign = npc.Ship.Callsign;
            _captureRequiredSeconds = requiredSeconds; // for the HUD's live ETA

            if (_captureProgress >= 1)
            {
                Board(npc);
                _captureProgress = 0;
                _plunderAuthorizedTargetId = null; // one authorization, one boarding
            }
        }
        else if (prompt.Offer)
        {
            // In range, no hostile intent declared, and not stood-down: OFFER it, accrue nothing.
            _plunderOpportunityTargetId = npc!.Ship.Id;
            _captureTargetCallsign = npc.Ship.Callsign;
            _captureProgress = 0;
            _captureEngaged = false;
        }
        else
        {
            // Out of the window, or an opportunity the captain already stood down from: no prompt,
            // no progress, no nag.
            _plunderOpportunityTargetId = null;
            _captureProgress = 0;
            _captureEngaged = false;
            if (intent == CaptureRule.BoardingIntent.NoWindow)
            {
                _captureTargetCallsign = null;
            }
        }

        // #205: the captain's word needs a door the captain can find. The plunder opportunity is not
        // only the Nav capture panel's prompt — it rides the #166 ship-wide channel as the first
        // ACTIONABLE alert, visible AND answerable from every desk. It carries the hull id so the
        // banner's approve/stand-down chips act on the right target. Raised on the same edge the Nav
        // prompt appears (prompt.Offer); cleared the moment the offer is gone (authorized, boarded,
        // stood-down, or out of the window) — the channel's edge semantics stay exact.
        if (prompt.Offer && npc is not null)
        {
            // #172: a boarding opportunity is the first ACTIONABLE alert — skip must not blow past it.
            // The Raise is edge-triggered, so only a NEW offer cancels the skip (not a persisting one).
            if (_shipAlerts.Raise(AlertKind.Boarding, AlertSeverity.Amber,
                    $"🏴 Boarding window open on {npc.Ship.Callsign} — approve or stand down", SimTime,
                    actionTargetId: npc.Ship.Id))
            {
                EndSkipIfActive($"boarding window open on {npc.Ship.Callsign}");
            }
        }
        else
        {
            _shipAlerts.Clear(AlertKind.Boarding);
        }
    }

    // ---- #177/#178: the captain approves the space-crimes. A boarding is a felony (heat, someone
    // else's hull); like the gun (AuthorizeShot), it never fires without the captain's explicit
    // word. _plunderAuthorizedTargetId names the one hull the captain has OK'd; UpdateCapture only
    // accrues against a matching target. Declining is FREE and SILENT (owner ruling). ----
    private string? _plunderAuthorizedTargetId;   // the hull the captain has OK'd to board
    private string? _plunderOpportunityTargetId;  // an in-window target awaiting the captain's word
    private string? _plunderDeclinedTargetId;     // a hull the captain stood down from — silent for this pass

    private void AuthorizePlunder()
    {
        NpcState? npc = SelectedCaptureTarget();
        if (npc is null)
        {
            return;
        }

        _plunderAuthorizedTargetId = npc.Ship.Id;
        _plunderOpportunityTargetId = null;
        ShowPulseMessage($"CAPTAIN: 🏴 boarding {npc.Ship.Callsign} authorized — this is PIRACY, and it'll draw heat");
        StateHasChanged();
    }

    // Declining a plunder opportunity is free and silent (owner ruling): dismiss it, no heat, no
    // fuss. Remembers the hull so UpdateCapture's every-frame tick won't re-raise the prompt while
    // we're still flying past it (the offer re-arms once the pass ends or a new hull is selected).
    // Clears any standing authorization too — a stand-down means stand down.
    private void DeclinePlunder()
    {
        _plunderDeclinedTargetId = _plunderOpportunityTargetId; // silence this hull for the rest of the pass
        _plunderOpportunityTargetId = null;
        _plunderAuthorizedTargetId = null;
        StateHasChanged();
    }

    private void Board(NpcState npc)
    {
        int holdSpace = CargoCapacity - _cargoUnits;
        int take = Math.Max(0, Math.Min(npc.Ship.CargoUnits, holdSpace));
        if (take > 0)
        {
            _cargoUnits += take;
            _cargoValue += take * CargoMarket.UnitValue(npc.Ship.CargoClass);
            _cargoByClass[npc.Ship.CargoClass] = _cargoByClass.GetValueOrDefault(npc.Ship.CargoClass) + take;

            // PR-BUSTED (ruling §5.1): a heist committed WHILE UNDER HEAT stamps the haul hot at theft
            // time — the stolen-under-heat evidence the collectors confiscate in full. The theft's heat
            // is the CURRENT level (before this robbery raises it): a first crime from a cold start is
            // not yet hot. Launders off later when heat cools to 0 (see UpdateEncounters).
            _hotCargo.Stamp(npc.Ship.CargoClass, take, _heat.Level);

            // #202: theft gets books and a voice. (a) A loot line in the SAME Captain's ledger the
            // honest receipts use — what, units, worth, off whom, where, when (LedgerTips projects it).
            // (b) The 🦜 names the haul, once per boarding (Board fires once per capture — an honest
            // edge). (c) The victim goes on the contacts as a NEGATIVE history seam (marked hostile).
            string where = _nearestBody?.Name ?? "open space";
            LootRecord loot = LootRecord.ForHaul(npc.Ship.CargoClass, take, npc.Ship.Callsign, where, SimTime);
            _lootLedger.Insert(0, loot);
            SquawkNow(Parrot.Squawk.Plunder, _lastTimestampMs ?? 0,
                $"{take} units of {npc.Ship.CargoClass} out of the {npc.Ship.Callsign}", force: true);
            _contacts.RecordPlunder(npc.Ship.Id, npc.Ship.Callsign, SimTime);
        }

        npc.Boarded = true; // keeps flying but empty; a second boarding yields nothing
        ShowPulseMessage($"Captured {take} units of {npc.Ship.CargoClass} {PlunderLines[(int)((SimTime / 60) % PlunderLines.Length)]}");
        RendererInterop.PlayCue("board");
        CompleteHuntQuests(npc.Ship.Id); // a bar contract on this ship is now met (M-Q1)
        AdvanceTutorial(3); // step 4: first successful boarding
        if (npc.Ship.Id == TrafficSchedule.StarterFreighterId)
        {
            AdvanceTutorial(StepBoardFreighter); // second hunt, step 5: boarding the holed hulk
        }
        RaiseHeatFromRobbery(npc);
        RequestVaultSave(); // #225: a boarding changed cargo, contacts (plunder) and heat
    }
    private string? _interestTargetId;
    private InterceptEstimate.Result? _intercept;

    private void SetInterestTarget(string id)
    {
        _interestTargetId = _interestTargetId == id ? null : id;
        _intercept = null;
        _passDirty = true; // recompute the intercept clock with the next pass scan
        StateHasChanged();
    }

    private string? InterestTargetName()
    {
        if (_interestTargetId is null)
        {
            return null;
        }

        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == _interestTargetId) { return npc.Ship.Callsign; }
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == _interestTargetId) { return hunter.Callsign; }
        }

        return _interestTargetId;
    }

    // M27: the war room's intercept clock — our plotted course vs the interest target's
    // gravity-only coast (the standard estimate for a freighter between burns). The threshold
    // is the boarding envelope: the "initiative roll" moment of a piracy run.
    private void UpdateInterceptEstimate()
    {
        _intercept = null;
        if (_interestTargetId is null || _simulator is null || _samples.Count < 2)
        {
            return;
        }

        ShipState? target = null;
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == _interestTargetId && npc.Active && !npc.Arrived) { target = npc.State; break; }
        }

        // The pursuit-law fork (see PredictInterestPath): a hunter neither coasts on gravity nor
        // waits to be boarded — the honest clock is HIS catch envelope closing on US, flown under
        // his actual thrust law, not a freighter coast against our boarding radius.
        if (target is null)
        {
            foreach (HunterState hunter in _hunters)
            {
                if (hunter.Id != _interestTargetId)
                {
                    continue;
                }

                double hunterHorizon = _samples[^1].SimTime - hunter.State.SimTime;
                if (hunterHorizon > 0)
                {
                    IReadOnlyList<TrajectorySample> pursuit =
                        EncounterRule.PredictHunterPath(hunter, PlayerPathForPrediction(), hunterHorizon);
                    _intercept = InterceptEstimate.Against(_samples, pursuit, EncounterRule.CatchRadiusMeters);
                }

                return;
            }
        }

        if (target is not { } t)
        {
            return;
        }

        double horizon = _samples[^1].SimTime - t.SimTime;
        if (horizon <= 0)
        {
            return;
        }

        IReadOnlyList<TrajectorySample> theirs = _simulator.ProjectAdaptive(t, null, horizon, maxSamples: 1500);
        _intercept = InterceptEstimate.Against(_samples, theirs, CaptureRule.CaptureRadiusMeters);
    }

    private string? InterceptChipLine()
    {
        if (_intercept is not { } ic || InterestTargetName() is not { } name)
        {
            return null;
        }

        if (ic.FirstWithinThresholdSimTime is { } t0)
        {
            return t0 <= SimTime
                ? $"⚔ {name}: ENCOUNTER WINDOW"
                : $"⏱ {name}: encounter in {FormatDuration(t0 - SimTime)}";
        }

        return $"{name}: min {FormatDistance(ic.MinDistance)} in {FormatDuration(ic.MinSimTime - SimTime)}";
    }

    // ---- PR-7: the gun deck — encounters, heat, hunters ----

    // Whether a boarding gets the compliance speed bonus: bribed always qualifies (an inside job
    // needs no warning shot); otherwise the target must have been warned AND actually be the
    // compliant type — a stubborn ship never heaves to, warned or not. Pods have no crew to
    // comply at all.
    private bool IsCompliantBoarding(NpcState npc)
    {
        if (npc.Ship.IsPod)
        {
            return false;
        }

        if (npc.Bribed)
        {
            return true;
        }

        return npc.WarningShotFired && EncounterRule.ComplianceOf(npc.Ship, _heat.Level) == ComplianceState.Compliant;
    }

    // Robbing a ship is what actually raises heat — the warning shot only narrates the ship's
    // reaction (heave to vs. call for help); a bribed ship pays for silence, so no heat at all.
    private void RaiseHeatFromRobbery(NpcState npc)
    {
        if (npc.Ship.IsPod || npc.Bribed)
        {
            return;
        }

        ComplianceState compliance = EncounterRule.ComplianceOf(npc.Ship, _heat.Level);
        int amount = compliance == ComplianceState.Stubborn ? 2 : 1;
        _heat = EncounterRule.RaiseHeat(_heat, amount, SimTime);
        PushNewsEvent(NewsWire.NewsEventKind.RobberyCommitted, npc.Ship.Callsign);
        SpawnHunterForHeatEvent();
        ShowPulseMessage(compliance == ComplianceState.Stubborn
            ? "Her muscle's already inbound. Heat rising fast."
            : "Word travels. Heat rising.");
    }

    // One hunter per heat event, fitting out at the nearest policed body (Earth/Mars-like —
    // never a haven). A pure outer-reaches scenario with nothing policed in range simply sends
    // no muscle — there's no cavalry to call.
    private void SpawnHunterForHeatEvent()
    {
        if (_ephemeris is null)
        {
            return;
        }

        CelestialBody? origin = EncounterRule.NearestPolicedBody(_ephemeris, _ship.Position, SimTime);
        if (origin is null)
        {
            return;
        }

        Vector2d originPosition = _ephemeris.Position(origin.Id, SimTime);
        const double h = 1.0;
        Vector2d originVelocity = (_ephemeris.Position(origin.Id, SimTime + h) - _ephemeris.Position(origin.Id, SimTime - h)) / (2 * h);

        string callsign = HunterCallsigns[_hunterSeq % HunterCallsigns.Length];
        string id = $"hunter-{_hunterSeq++}";
        _hunters.Add(EncounterRule.SpawnHunter(id, callsign, origin.Id, originPosition, originVelocity, SimTime));
        PushNewsEvent(NewsWire.NewsEventKind.HunterDispatched, callsign, origin.Name);
        // #380 item 5 (owner ruling 2026-07-19: "new players are left mystified") — the robbery bought
        // this hunter, but the fit-out delay meant muscle appeared days later with no causal link. This
        // pulse draws the chain in-voice the moment the collector is spawned; the callsign rides the news
        // headline behind it.
        ShowPulseMessage($"Word's out — your last job bought you a collector ({callsign}). It's fitting out at {origin.Name}; days, not weeks.");
    }

    private void FireWarningShot(string npcId)
    {
        NpcState? npc = FindNpc(npcId);
        if (npc is null)
        {
            // Not a freighter — it's the hunter itself. A warning shot erodes a collector's nerve.
            WarnHunter(npcId);
            return;
        }

        if (npc.Ship.IsPod || !EncounterRule.InWeaponRange(_ship, npc.State))
        {
            return;
        }

        if (_slugAmmo <= 0)
        {
            ShowPulseMessage("No slugs left for a warning shot — buy dockside");
            return;
        }

        _slugAmmo--;
        npc.WarningShotFired = true;

        // M28: the warning shot is a REAL slug now — flung wide on purpose (AcrossTheBow
        // rounds never hit-check) but genuinely in flight on the map. Same reaction rules.
        Vector2d toTarget = (npc.State.Position - _ship.Position).Normalized();
        var wide = new Vector2d(-toTarget.Y, toTarget.X) * 0.03;
        FireOrdnance(OrdnanceKind.Slug, (toTarget + wide).Normalized(), MaxMuzzleSpeed,
            npc.Ship.Id, acrossTheBow: true);

        ComplianceState compliance = EncounterRule.ComplianceOf(npc.Ship, _heat.Level);
        ShowPulseMessage(compliance == ComplianceState.Stubborn
            ? $"WARNING SHOT ACROSS THE BOW — {npc.Ship.Callsign} answers with a tight-beam call for help, not her colours"
            : "WARNING SHOT ACROSS THE BOW — she heaves to");
        RendererInterop.PlayCue("pulse");

        // Second hunt, step 2: the warning shot teaches that a stubborn hull won't heave to — she
        // calls muscle instead, so the soft path is a dead end and the gun is the only way in.
        if (npcId == TrafficSchedule.StarterFreighterId)
        {
            AdvanceTutorial(StepWarnFreighter);
        }
    }

    // A warning shot flung across a Debt Collector's bow: each one erodes its nerve. Most peel off
    // (coast, stop closing) for a stretch that grows with every shot; enough of them and the
    // collector voids the contract for good. A rare "La Dolce Vita" sort quits at the very first.
    private void WarnHunter(string hunterId)
    {
        int index = _hunters.FindIndex(h => h.Id == hunterId);
        if (index < 0)
        {
            return;
        }

        HunterState hunter = _hunters[index];
        if (hunter.CaughtPlayer || hunter.BrokenOff || !EncounterRule.InWeaponRange(_ship, hunter.State))
        {
            return;
        }

        if (_slugAmmo <= 0)
        {
            ShowPulseMessage("No slugs left for a warning shot — buy dockside");
            return;
        }

        _slugAmmo--;

        // A real slug flung wide (AcrossTheBow rounds never hit-check, so its sail is never holed).
        Vector2d toTarget = (hunter.State.Position - _ship.Position).Normalized();
        var wide = new Vector2d(-toTarget.Y, toTarget.X) * 0.03;
        FireOrdnance(OrdnanceKind.Slug, (toTarget + wide).Normalized(), MaxMuzzleSpeed,
            hunter.Id, acrossTheBow: true);
        RendererInterop.PlayCue("pulse");

        bool goodLifeFirstShot = hunter.WarningShotsTaken == 0
            && EncounterRule.PrefersTheGoodLife(hunter.Id, _heat.Level);
        HunterState after = EncounterRule.WarnOff(hunter, _heat.Level, SimTime);

        if (after.BrokenOff)
        {
            // Gave up. Remove it here so the generic "loses your scent" path in StepEncounters
            // doesn't also fire with the wrong flavor.
            _hunters.RemoveAt(index);
            if (_interestTargetId == hunterId)
            {
                _interestTargetId = null;
            }

            ShowPulseMessage(goodLifeFirstShot
                ? $"⚠ {hunter.Callsign} watches the slug drift past, shrugs, and turns for the nearest cantina — la dolce vita 🍸"
                : $"⚠ {hunter.Callsign} has had enough — she sheers off and voids the contract");
            PushNewsEvent(NewsWire.NewsEventKind.HunterBrokeOff, hunter.Callsign, _nearestBody?.Name);
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            _hunters[index] = after;
            double peelDays = (after.PeeledUntilSimTime - SimTime) / 86400.0;
            string nerve = after.WarningShotsTaken switch
            {
                1 => "wavers",
                2 => "is rattled",
                _ => "is losing her nerve",
            };
            ShowPulseMessage($"⚠ WARNING SHOT — {hunter.Callsign} {nerve} and sheers off (peels away ~{peelDays:0.#} d)");
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
    }

    private void BribeShip(string npcId)
    {
        NpcState? npc = FindNpc(npcId);
        if (npc is null || npc.Bribed || npc.Ship.IsPod)
        {
            return;
        }

        int price = EncounterRule.BribePrice(npc.Ship);
        if (_credits < price)
        {
            ShowPulseMessage("Not enough credits to grease this crew.");
            return;
        }

        _credits -= price;
        npc.Bribed = true;
        ShowPulseMessage($"{npc.Ship.Callsign}'s crew take the coin — an inside job, quiet as the void.");
    }

    // Hidden at a haven (vision par. 18): either bound in orbit around a haven MOON, or CLAMPED in
    // the dock of a haven STATION (the mass-less grey-market docks have no Hill sphere to orbit —
    // you berth at them instead). Both cool heat 4x and, held long enough, break a hunter's pursuit.
    private bool IsHiddenAtHaven()
    {
        if (_nearestBody is not { IsHaven: true } haven)
        {
            return false;
        }

        // Clamped in this haven's dock — held fast, lying low (no orbit to bind, so short-circuit).
        if (_dockedHavenId == haven.Id)
        {
            return true;
        }

        // A haven moon: bound in its Hill sphere the ordinary way.
        if (_ephemeris is null || haven.ParentId is null)
        {
            return false;
        }

        CelestialBody? parent = null;
        foreach (CelestialBody candidate in _ephemeris.Bodies)
        {
            if (candidate.Id == haven.ParentId)
            {
                parent = candidate;
                break;
            }
        }

        if (parent is null)
        {
            return false;
        }

        double hill = OrbitRule.HillRadius(haven, parent.Mu);
        return OrbitRule.IsBound(_ship, _nearestBodyPosition, _nearestBodyVelocity, haven, hill);
    }

    // Is a cargo class hot (stolen)? Today the loot ledger is the evidence of a heist — a class we've
    // ever boarded reads as hot in the hold. The BUSTED lane owns the authoritative per-unit flag; this
    // is the honest read HOARD has until then (the seam both agree on: hot = stolen-flagged).
    private bool IsHotClass(string cargoClass) =>
        _lootLedger.Any(l => string.Equals(l.CargoClass, cargoClass, StringComparison.OrdinalIgnoreCase));

    // Hot units currently in the hold (what confiscation would see as evidence — until it's buried).
    private int HotHoldUnits() => _cargoByClass.Where(kv => IsHotClass(kv.Key)).Sum(kv => kv.Value);

    // #380 item 9 (owner ruling 2026-07-19: "new players are left mystified") — the 🔥 hot-cargo flag
    // rode the confiscation and rescue manifests unglossed. Hung as a hover title on the flag wherever it
    // appears (a one-time pulse is awkward inside the manifest markup), so its first sight explains it.
    private const string HotGlossTitle = "🔥 hot = taken under heat — collectors seize it in full, fences launder it.";

    // #202: the crimes' books — a loot line per completed boarding, newest first, projected into the
    // Captain's ledger alongside the honest autopilot receipts (the established tip idiom).
    private readonly List<LootRecord> _lootLedger = [];

    // ---- Pursuit steering by the quantum trail (aim-solution follow-up, 2026-07-06) ----
    // At warp a frame spans hundreds of sim-seconds, and the old catch-up steered EVERY hunter
    // quantum toward the single frame-end player position — so hunter paths depended on frame
    // cadence (not sim-deterministic; against the working agreement) and a long fire-control
    // prediction chased a target no model could reproduce. The trail records the ship's actual
    // integrated positions through the frame at the pursuit cadence; steering looks up the
    // position AT each quantum's time. Residual frame dependence is only interpolation sag
    // between 60 s knots (~km) — was tens of thousands of km at 10000x.
    //
    // ABORT SWITCH: set false to restore the old frame-end steering exactly (one flag, no other
    // code path touched) if playtesting turns up trouble.
    private const bool SteerHuntersByQuantumTrail = true;
    private readonly List<TrajectorySample> _pursuitTrail = [];

    /// <summary>The player state a pursuit quantum steers at: position interpolated on this
    /// frame's trail, falling back to the live ship outside it (or with the switch off). The
    /// velocity stays the frame-end ship's — AdvanceHunter only reads it for the catch check's
    /// relative speed, where a frame of gravity barely moves the needle.</summary>
    private ShipState PlayerStateForPursuit(double stepTime)
    {
        if (!SteerHuntersByQuantumTrail || _pursuitTrail.Count < 2 || stepTime >= _pursuitTrail[^1].SimTime)
        {
            return _ship;
        }

        for (int i = _pursuitTrail.Count - 2; i >= 0; i--)
        {
            if (_pursuitTrail[i].SimTime <= stepTime)
            {
                TrajectorySample a = _pursuitTrail[i], b = _pursuitTrail[i + 1];
                double span = b.SimTime - a.SimTime;
                double f = span > 0 ? (stepTime - a.SimTime) / span : 1;
                return new ShipState(a.Position + (b.Position - a.Position) * f, _ship.Velocity, stepTime);
            }
        }

        return _ship;
    }

    // Heat decay, hunter pursuit and break-off — all in sim time (like NPC stepping), so it
    // scales naturally with warp instead of crawling at wall-clock rate.
    private void UpdateEncounters()
    {
        if (_ephemeris is null)
        {
            return;
        }

        // PR-BUSTED: while a boarding pop-up is open, encounters freeze — the captain is making a
        // choice at 1×, no new hunter runs him down over the top of it.
        if (_busted is not null)
        {
            return;
        }

        // #175: settle any moon-haven cargo run whose ship is parked in orbit — the owner who was
        // ALREADY orbiting Enceladus when the parcel loaded gets paid here, since no dock event fires.
        CompleteBoundCargoRunQuests();

        // #223: resolve the buried-cache discovery roll as sim time rolls past whole days — rivals find
        // our hoards on a slow roll whether we're flying, warping, or docked.
        RunCacheDiscoveryWatch();

        bool wasHidden = !double.IsNaN(_hiddenAtHavenSinceSimTime);
        bool hidden = IsHiddenAtHaven();

        // Rising edge of "hidden at a haven" — whether you orbited a haven moon or clamped onto a
        // dock. Drop the quiet news line the regulars notice, and advance the haven lesson. (Moved
        // here from the orbit-bind loop so a mass-less dock, which never binds, still triggers it.)
        if (hidden && !wasHidden && _nearestBody is { IsHaven: true } arrivedHaven)
        {
            PushNewsEvent(NewsWire.NewsEventKind.OrbitEnteredHaven, arrivedHaven.Name);
            AdvanceTutorial(StepInsertHaven);

            // Easter egg: settle in at The Rusty Roadstead and the bird cracks wise about a break.
            if (arrivedHaven.Id == "the-space-bar")
            {
                SquawkNow(Parrot.Squawk.SpaceBarBreak, _lastTimestampMs ?? 0, force: true);
            }
        }

        _hiddenAtHavenSinceSimTime = hidden
            ? (wasHidden ? _hiddenAtHavenSinceSimTime : SimTime)
            : double.NaN;
        double hiddenDuration = hidden ? SimTime - _hiddenAtHavenSinceSimTime : 0;

        _heat = EncounterRule.DecayHeat(_heat, SimTime, hidden);

        // PR-BUSTED (ruling §5.1): when heat fully cools, the stolen cargo launders — the evidence
        // leaves the books. And at each UPWARD heat crossing the parrot names the confiscation exposure
        // (owner: "Heat two, captain — they'll take a third of the purse if they catch us!"), riding the
        // same #166 alert edges the rest of the ship's voice does.
        if (_heat.Level == 0 && _hotCargo.Any)
        {
            _hotCargo.Launder();
            ShowPulseMessage("The trail's cold — your hot cargo just became honest freight again.");
        }

        if (_heat.Level > _lastAnnouncedHeat)
        {
            SquawkNow(Parrot.Squawk.Busted, _lastTimestampMs ?? 0, BustedRule.ExposurePhrase(_heat.Level), force: true);
        }

        // #380 item 1: the FIRST time heat reaches 1, advertise the safety net one beat before the death card
        // would have to. Fires whatever raised the heat (a robbery, a Reever's hand), once per run.
        if (!_heatInsuranceAdvised && _heat.Level >= 1)
        {
            _heatInsuranceAdvised = true;
            ShowPulseMessage("Word of advice, captain — your brain-backup's current and the pirate-insurance stake is paid. Getting caught is expensive. Getting killed is survivable.");
        }

        _lastAnnouncedHeat = _heat.Level;

        // #580 · NOBODY IS AT THE CONTROLS. While the captain is walking a moon, the ship is a docked hull
        // with the lights on and no one aboard — so the wolves hold station instead of closing, and cannot
        // catch her. See EncounterRule.HoldStation for the owner's ruling; the short of it is that heat is
        // the CAPTAIN's, and a game where a good long excursion means coming home to a boarding party is a
        // game about guarding a parking lot.
        bool captainIsAboard = _surface is null;

        for (int i = _hunters.Count - 1; i >= 0; i--)
        {
            HunterState hunter = _hunters[i];
            if (!captainIsAboard)
            {
                _hunters[i] = EncounterRule.HoldStation(hunter, SimTime);
                continue;
            }

            while (hunter.State.SimTime < SimTime && !hunter.CaughtPlayer && !hunter.BrokenOff)
            {
                double stepTime = Math.Min(SimTime, hunter.State.SimTime + EncounterRule.HunterStepSeconds);
                hunter = EncounterRule.AdvanceHunter(hunter, PlayerStateForPursuit(stepTime), stepTime);
                if (hidden)
                {
                    hunter = EncounterRule.ApplyBreakOff(hunter, hiddenDuration);
                }
            }

            if (hunter.CaughtPlayer)
            {
                ApplyHunterCatch(hunter);
                _hunters.RemoveAt(i);
            }
            else if (hunter.BrokenOff)
            {
                ShowPulseMessage($"{hunter.Callsign} loses your scent — safe at anchor.");
                SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
                _hunters.RemoveAt(i);
            }
            else
            {
                _hunters[i] = hunter;
            }
        }

        // Haven tutorial completes when the heat your piracy earned has fully cooled (the haven's
        // 4x decay is what gets you there in reasonable time — the lesson is "lying low works").
        if (_tutorialStep == StepCoolHeat && _heat.Level == 0)
        {
            AdvanceTutorial(StepCoolHeat);
            ShowPulseMessage("The trail's gone cold — you've learned to lie low. The haven kept you.");
        }
    }

    // PR-BUSTED (owner ruling §5): the catch is no longer an instant tax that leaves the collector
    // inert — it OPENS the boarding pop-up. Warp yanks to 1×, the ship is grappled, and the collector
    // hails with a demand and three options (SUBMIT / BRIBE / RESIST). The seed is folded from the
    // hunter's identity and the sim moment, so every roll in this encounter is reproducible.
    private void ApplyHunterCatch(HunterState hunter)
    {
        Warp = 1;
        _effectiveWarp = 1;
        RendererInterop.PlayCue("board");

        ulong seed = DiceRule.Seed("busted", HunterSeqOf(hunter.Id), (long)SimTime);
        _busted = new BustedEncounter
        {
            HunterId = hunter.Id,
            HunterCallsign = hunter.Callsign,
            Heat = Math.Max(1, _heat.Level),
            Seed = seed,
            Bribe = BustedRule.BribeDemand(Math.Max(1, _heat.Level), seed),
            Cause = DeathCause.Collector,          // #380: a catch that ends in the volley is a collector death
            DeathBodyName = _nearestBody?.Name,    // the place the last stand happened, for the wake card
        };

        // #422 arc 2 — THE COLLECTOR'S WRIT. A heat/collector catch is the moment you get a look at what the
        // repo men are really sent to recover: not the cargo, a PATTERN. The glimpse assembles the shard the
        // first time; the writ line then rides the demand card that once, so the recontextualization lands
        // without cluttering every later stop. They work Nebula's collateral.
        if (AssembleNebulaSilently("collector-writ"))
        {
            _busted.CollectorWrit = NebulaLore.ById("collector-writ")!.Lore;
        }

        SquawkNow(Parrot.Squawk.Busted, _lastTimestampMs ?? 0, BustedRule.ExposurePhrase(Math.Max(1, _heat.Level)), force: true);
        StateHasChanged();
    }

    // #264 — the impact enforcer's consequence. Lab 16's "periapsis under the surface — impact coming"
    // finally arrives: a LIVE-FLOWN step reached a body's surface radius (SurfaceImpact caught the
    // crossing; the ship never flew the interior). Reuse the BUSTED freeze-frame → brain-backup
    // resurrection whole — the death machinery is not duplicated — so you wake at the nearest haven's
    // clinic in the insurance rustbucket, ship and visible cargo gone, banked/buried safe. There is no
    // collector here (the planet collected), no heat, no dice: straight to the freeze. Say-the-state:
    // the ledger logs it, the strip shouts it, the parrot squawks. Docked ships and havens on rails
    // can't reach here — the caller exempts the dock and SurfaceImpact skips zero-radius havens.
    private void TriggerImpact(SurfaceImpact.Crossing hit)
    {
        if (_busted is not null)
        {
            return; // already mid-reckoning — one death at a time
        }

        Warp = 1;
        _effectiveWarp = 1;

        // Pin the ship to the point of contact so nothing coasts on behind the modal; the resurrection
        // resets it onto the clinic haven when the captain wakes.
        _ship = _ship with { Position = hit.Position, Velocity = Vector2d.Zero, SimTime = hit.SimTime };

        RendererInterop.PlayCue("board");    // impact/volley hook (a dedicated cue is a follow-up)
        RendererInterop.PlayCue("gameover"); // game-over-music hook

        _busted = new BustedEncounter
        {
            HunterId = string.Empty,        // no collector — the surface collected
            HunterCallsign = hit.BodyName,
            Heat = 0,
            Seed = DiceRule.Seed("impact", (long)hit.SimTime),
            Bribe = default,                // unused on the impact path (no bribe to a planet)
            Phase = BustedEncounter.Stage.Impact,
            ImpactBodyName = hit.BodyName,
            Cause = DeathCause.Impact,      // #380: the surface collected the ship — a place-dependent death
            DeathBodyName = hit.BodyName,
        };

        string line = $"💥 IMPACT — the ship struck {hit.BodyName}. Periapsis went under the surface, and the surface won.";
        LogAutopilotEvent(line);
        ShowPulseMessage(line);
        _shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, $"IMPACT — struck {hit.BodyName}", SimTime);
        SquawkNow(Parrot.Squawk.Impact, _lastTimestampMs ?? 0, hit.BodyName, force: true);
        StateHasChanged();
    }

    // Evening wind #20 (owner 2026-07-18) — THE OVERDRAW DEATH. Nerves shot past empty and a Reever's hand
    // is the last straw: the captain goes crazy and dramatically exits the scene. Route the SURFACE causes
    // (#380 item 1, wired-ready) live for the first time: classify place-dependently via
    // DeathNarration.SurfaceEnd (the Old Ones TOOK you — or, rarely, you JOINED them) and hand it to the
    // SAME BUSTED freeze-beat → brain-backup resurrection the collector/impact deaths use — the death
    // machinery is shared, never duplicated. No collector, no dice: straight to the surface freeze-beat. The
    // resurrection folds the excursion away as a failed gig and issues a new captain (BustedResurrect).
    /// <param name="nerveRanOut">True when the NERVE overdrew (the gauge hit the floor with a qualifying
    /// hit); false when the FIVE BLOWS ran out and the captain was simply mauled. Drives the freeze-frame
    /// caption — see <see cref="DeathNarration.SurfaceCaption"/>.</param>
    /// <param name="known">#564 · A cause the caller already KNOWS, passed instead of rolled. The roll
    /// below only knows the ground's two answers (the Old Ones took you, or you joined them), so anything
    /// that kills a captain for another reason — running the tank dry — would have been narrated as a
    /// Reever's hand. That is the same sim-says-one-thing-sentence-says-another failure #545 fixed for the
    /// black-ops sweep, and the fix is the same: the caller passes what it knows.
    ///
    /// <para>#633 · Both branches invented this parameter independently, for the same reason, one day apart
    /// — <c>known</c> here and <c>forcedCause</c> on <c>main</c> (#538's sweep team). One name survives, and
    /// it is this one: nothing is being FORCED, the caller simply is not guessing.</para></param>
    private void TriggerSurfaceOverdrawDeath(
        SurfaceExcursion dying, bool nerveRanOut, DeathCause? known = null)
    {
        if (_busted is not null)
        {
            return; // already mid-reckoning — one death at a time
        }

        Warp = 1;
        _effectiveWarp = 1;

        string body = dying.Stop.Body.Name;
        ulong seed = DiceRule.Seed("overdraw", (long)SimTime);
        // #538 · WHO ACTUALLY KILLED YOU. SurfaceEnd only ever answers "the pack, or the rare Joined at a
        // sliver" — which was fine while the pack was the only thing aboard that could end a captain. The
        // first playtest of the sweep team ended with three men with rifles shooting a captain in a corridor
        // and a card that said "the Old Ones took you… ran you down on her regolith short of the tube". A
        // caller that KNOWS the cause now says so, and the roll is only consulted when nobody does.
        DeathCause cause = known ?? DeathNarration.SurfaceEnd(_nerve, seed); // Reevers, or the rare Joined at a sliver

        // #574 · A death away from her deck can never be a COLLECTOR — a collector is a person who came for
        // you, and there is nobody aboard a dead hull or out on an empty moon. Owner: "the debt collector
        // deaths should also only happen in those situations never in any other". Coerced rather than
        // trusted, because this method has four callers and will have more.
        // #609 · AND UNDER A MOON IS ITS OWN PLACE. Owner, having suffocated on B2 and been handed the
        // surface card: "now we have the suffocated on surface one :-D"
        //
        // He was 150 m down in a poured corridor being told about regolith, a suit and the long walk back to
        // the tube. Nothing here knew "underground" existed, so every death in the Hive fell through to the
        // away team's — the sim knowing one thing and the sentence reporting another, which is the named bug
        // class on this ground and the third card it has cost.
        //
        // The floor is the fact, and it is right here on the excursion. It just was not being asked.
        DeathPlace place = dying.Floor < 0
            ? DeathPlace.Underground
            : Derelict.TryParseWreckId(dying.Stop.Body.Id, out _)
                ? DeathPlace.Derelict
                : DeathPlace.LandingParty;
        if (!DeathNarration.CanHappen(cause, place))
        {
            cause = DeathCause.Reevers;
        }

        _busted = new BustedEncounter
        {
            HunterId = string.Empty,     // no collector — the ground and the mind collected
            HunterCallsign = body,
            Heat = 0,
            Seed = seed,
            Bribe = default,             // unused on a surface death (no bribe to your own nerves)
            Phase = BustedEncounter.Stage.SurfaceEnd,
            Cause = cause,
            // #574: a salvage run and an away team are not the same death. Derelict ids parse; a moon does
            // not — so the excursion itself says which world's words the card should use.
            Place = place,
            NerveRanOut = nerveRanOut,
            DeathBodyName = body,
        };

        RendererInterop.PlayCue("alarm");
        RendererInterop.PlayCue("gameover");
        string line = cause switch
        {
            DeathCause.Suffocated => DeathNarration.SuffocationHeadline(body),
            DeathCause.Joined =>
                $"🧠 Nerves gone past empty on {body} — the captain turns, and walks TOWARD the crowd. The insurance will need a new name.",
            // #525 · The one they chose. The pulse must not say an Old One's hand was the last straw over a
            // captain who turned both keys themselves ninety seconds ago.
            DeathCause.Scuttled =>
                $"☢ The overload ran out with the captain still aboard the {body}. She goes all at once and mostly inward. The insurance will need a new name.",
            // Nothing about this one is about nerve, so it must not narrate as if it were.
            DeathCause.Inspected =>
                $"🕶 Found aboard {body}, told to stand still, and not standing still. The sweep goes on down the corridor. The insurance will need a new name.",
            _ =>
                $"🧠 Nerves shot past empty on {body} — an Old One's hand is the last straw. The captain breaks. The insurance will need a new name.",
        };
        LogAutopilotEvent(line);
        ShowPulseMessage(line);
        _shipAlerts.Raise(AlertKind.Collision, AlertSeverity.Red, $"CAPTAIN LOST — {body}", SimTime);
        SquawkNow(Parrot.Squawk.Impact, _lastTimestampMs ?? 0, body, force: true);
        StateHasChanged();
    }

    /// <summary>
    /// #621 dev cheat · <c>/map?death=&lt;cause&gt;</c> — land first (if the URL asked to), THEN die.
    ///
    /// <para><see cref="AutoLandForCheatAsync"/> is fire-and-forget with several early returns, so the death
    /// cannot simply be queued after it in the boot block: it would fire while the shuttle was still coming
    /// down and the excursion's floor and body id — the two facts the place classifier reads — would not
    /// exist yet. Awaited here instead, in the one place that knows the landing is over.</para>
    /// </summary>
    private async Task AutoLandThenStageDeathAsync(DeathCause? cause)
    {
        await AutoLandForCheatAsync();
        if (cause is { } asked)
        {
            StageDeathCheat(asked);
        }
    }

    /// <summary>
    /// #621 dev cheat · KILL THE CAPTAIN NOW, through the real machinery.
    ///
    /// <para>Nothing here builds a card. It calls the same three triggers the game calls
    /// (<see cref="TriggerSurfaceOverdrawDeath"/>, <see cref="TriggerImpact"/>, <see cref="ApplyHunterCatch"/>)
    /// with the same arguments the sim would have handed them, so every downstream fact — the seeded line,
    /// the place classification, the art, the tail, the succession, the clinic bill, the rebirth glitch — is
    /// computed exactly as it is in play. A cheat that painted its own death card would prove that the cheat
    /// works and nothing else, which is the failure this project has named: a green test that asserts
    /// nothing, dressed as a quick start.</para>
    ///
    /// <para>Two arguments are read from the LIVE state rather than invented, for the same reason:
    /// <c>nerveRanOut</c> comes from <see cref="CaptainSuccession.OverdrawQualifies"/> on the real gauge
    /// (so a full-nerve captain gets the mauled caption and a shattered one gets the overdraw caption,
    /// truthfully), and the place is never passed at all — the excursion decides it.</para>
    /// </summary>
    private void StageDeathCheat(DeathCause cause)
    {
        string asked = cause.ToString().ToLowerInvariant();
        if (_busted is not null)
        {
            return; // already mid-reckoning — one death at a time, the same rule the triggers keep
        }

        // AWAY FROM HER DECK. One call, and the excursion answers WHERE by itself.
        if (_surface is { } ex)
        {
            TriggerSurfaceOverdrawDeath(
                ex, nerveRanOut: CaptainSuccession.OverdrawQualifies(_nerve), known: cause);

            // Say so when the law refused the cause — read back off the staged encounter rather than
            // re-deriving it, so the message can never disagree with the card behind it.
            if (_busted is { } staged && staged.Cause != cause)
            {
                ShowPulseMessage(
                    $"🧪 DEV ?death={asked}: a {asked} death cannot happen on a {staged.Place} "
                    + $"(DeathNarration.CanHappen) — the law substituted {staged.Cause}.");
            }
            return;
        }

        switch (cause)
        {
            case DeathCause.Impact:
                // The surface collected the ship. The crossing is synthesised at the ship's own position on
                // the nearest body, which is what SurfaceImpact would have handed over one tick later.
                UpdateNearestBody();
                if (_nearestBody is not { } rock)
                {
                    ShowPulseMessage("🧪 DEV ?death=impact: no body charted yet to fly into.");
                    return;
                }
                TriggerImpact(new SurfaceImpact.Crossing(rock.Id, rock.Name, 1.0, SimTime, _ship.Position));
                return;

            case DeathCause.Collector:
                // Not the freeze-frame — the CATCH, which is where a collector death actually begins. You
                // get the demand card and have to lose your way through SUBMIT / BRIBE / RESIST → THE
                // BOLIVIA to reach it, because that ladder is the thing worth playing and reading.
                _heat = EncounterRule.RaiseHeat(_heat, 2, SimTime);
                SpawnHunterForHeatEvent();
                if (_hunters.Count == 0)
                {
                    ShowPulseMessage(
                        "🧪 DEV ?death=collector: nothing policed within reach of this berth to send muscle "
                        + "from — try &dock=selene-gate or &dock=ringside.");
                    return;
                }
                ApplyHunterCatch(_hunters[^1]);
                return;

            default:
                // Reevers / Joined / Suffocated need somebody out of the ship; Void has no lane at all yet.
                ShowPulseMessage(
                    $"🧪 DEV ?death={asked}: not a death that happens on her deck. Add &land=1 (the ground), "
                    + "&wreck=1&land=1 (a derelict) or &secretlab=1&land=1&floor=2 (the Hive).");
                return;
        }
    }

    // The dice helpers a resist/Bolivia roll carries — the purchasable-modifier seam. One example is
    // shipped (the Boarding-nets jammer); the shop of helpers is a follow-up (owner §5.0).
    private List<DiceModifier> ResistModifiers()
    {
        var mods = new List<DiceModifier>();
        if (_hasNetJammer)
        {
            mods.Add(new DiceModifier("Boarding-nets jammer", +2));
        }

        return mods;
    }

    private static long HunterSeqOf(string hunterId) =>
        int.TryParse(hunterId.AsSpan(hunterId.LastIndexOf('-') + 1), out int n) ? n : 0;

    // ---- The three options ----

    // SUBMIT: confiscate all hot cargo + a heat share of the purse (with the minimum-take fallback and
    // the mercy floors), then clear heat to 0 — the debt is collected.
    private void BustedSubmit(bool harsher = false)
    {
        if (_busted is not { } b)
        {
            return;
        }

        BustedRule.Confiscation c = BustedRule.Confiscate(
            b.Heat, _credits, _hotCargo.BuildLots(_cargoByClass), b.Seed, harsher);
        ApplyConfiscation(c);
        _heat = EncounterRule.RaiseHeat(HeatState.None, 0, SimTime); // clears to 0 — debt collected
        _lastAnnouncedHeat = 0;
        _hotCargo.Launder();
        b.Confiscation = c;
        b.Phase = BustedEncounter.Stage.Confiscated;
        StateHasChanged();
    }

    private void ApplyConfiscation(BustedRule.Confiscation c)
    {
        _credits = c.CoinLeft;
        foreach (BustedRule.CargoSeizure s in c.Seizures)
        {
            int have = _cargoByClass.GetValueOrDefault(s.CargoClass);
            int taken = Math.Min(have, s.Units);
            if (taken <= 0)
            {
                continue;
            }

            int left = have - taken;
            if (left > 0)
            {
                _cargoByClass[s.CargoClass] = left;
            }
            else
            {
                _cargoByClass.Remove(s.CargoClass);
                _hotCargo.Forget(s.CargoClass);
            }

            _cargoUnits -= taken;
            _cargoValue -= taken * CargoMarket.UnitValue(s.CargoClass);
        }

        _cargoUnits = Math.Max(0, _cargoUnits);
        _cargoValue = Math.Max(0, _cargoValue);
        RendererInterop.PlayCue("board");
        RequestVaultSave(); // #225: a boarding resolution changed purse, cargo and heat
    }

    // BRIBE: pay the dice-rolled fee to keep the cargo; the hunter breaks off, heat is UNCHANGED (you
    // bought this patrol, not the law). Can't afford → the button is greyed with the number.
    private void BustedBribe()
    {
        if (_busted is not { } b || _credits < b.Bribe.Total)
        {
            return;
        }

        _credits -= b.Bribe.Total;
        RemoveHunter(b.HunterId);
        b.ResultMessage = $"{b.Bribe.Total:N0} cr changes hands. {b.HunterCallsign} logs a clean sweep and sheers off — heat unchanged. You bought this patrol, not the law.";
        b.Phase = BustedEncounter.Stage.BribedOff;
        SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        StateHasChanged();
    }

    // RESIST: heat 1–2 → one opposed dice check (lose = SUBMIT + harsher cut; win = hunter broken off,
    // heat +1). Heat 3 → the full Bolivia.
    private void BustedResist()
    {
        if (_busted is not { } b)
        {
            return;
        }

        if (b.Heat >= EncounterRule.MaxHeatLevel)
        {
            b.Phase = BustedEncounter.Stage.Bolivia;
            b.BoliviaInitiative = BoliviaEncounter.RollInitiative(b.Seed, b.Heat, ResistModifiers());
            b.BoliviaNet = b.BoliviaInitiative.Value.Margin;
            b.BoliviaBeatIndex = 0;
            SquawkNow(Parrot.Squawk.FiringSolution, _lastTimestampMs ?? 0, force: true);
            StateHasChanged();
            return;
        }

        OpposedRoll roll = BustedRule.ResistCheck(b.Heat, b.Seed, ResistModifiers());
        b.ResistRoll = roll;
        if (roll.ChallengerWins)
        {
            RemoveHunter(b.HunterId);
            _heat = EncounterRule.RaiseHeat(_heat, 1, SimTime); // a win pins the wolves meaner
            b.ResultMessage = $"You break {b.HunterCallsign}'s boarding — they peel off nursing the dent. Heat climbs; the next wave is meaner.";
            b.Phase = BustedEncounter.Stage.ResistWon;
            SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
        }
        else
        {
            b.Phase = BustedEncounter.Stage.ResistLost;
        }

        StateHasChanged();
    }

    // #735 · Stage-guarded for the same reason the wake is (see BustedResurrect): once Enter presses this
    // button too, a keystroke on a focused button can arrive as BOTH a key handler and the browser's own
    // click — and a confiscation applied twice is the collector taking the cut twice off one press.
    private void BustedResistLostConfirm()
    {
        if (_busted is not { Phase: BustedEncounter.Stage.ResistLost })
        {
            return;
        }

        BustedSubmit(harsher: true);
    }

    // A Bolivia beat: fold the choice, roll it, advance. After the last beat, tally and decide.
    private void BustedBoliviaChoose(string choiceId)
    {
        if (_busted is not { } b || b.Phase != BustedEncounter.Stage.Bolivia)
        {
            return;
        }

        EncounterBeat beat = BoliviaEncounter.Script[b.BoliviaBeatIndex];
        EncounterChoice choice = beat.Choices[0];
        foreach (EncounterChoice c in beat.Choices)
        {
            if (c.Id == choiceId) { choice = c; break; }
        }

        OpposedRoll roll = BoliviaEncounter.RollBeat(b.Seed, b.BoliviaBeatIndex, choice, b.Heat, ResistModifiers());
        b.BoliviaNet += roll.Margin;
        b.BoliviaOutcomes.Add(new BoliviaEncounter.BeatOutcome(b.BoliviaBeatIndex, choice.Id, roll));
        b.BoliviaBeatIndex++;

        if (b.BoliviaBeatIndex >= BoliviaEncounter.Script.Count)
        {
            b.BoliviaEnding = BoliviaEncounter.Decide(b.BoliviaNet);
            if (b.BoliviaEnding == BoliviaEncounter.Ending.Flee)
            {
                RemoveHunter(b.HunterId); // left tied up at their own ship
                _heat = new HeatState(BoliviaEncounter.FleeHeat, SimTime);
                _lastAnnouncedHeat = BoliviaEncounter.FleeHeat;
                b.ResultMessage = $"You fight clear — {b.HunterCallsign} left tied up at their own ship. You slip away carrying heat {BoliviaEncounter.FleeHeat}.";
                b.Phase = BustedEncounter.Stage.Fled;
                SquawkNow(Parrot.Squawk.HunterBacksOff, _lastTimestampMs ?? 0, force: true);
            }
            else
            {
                b.Phase = BustedEncounter.Stage.FreezeFrame;
                RendererInterop.PlayCue("board");        // volley hook (audio cue is a follow-up)
                RendererInterop.PlayCue("gameover");     // game-over-music hook
            }
        }

        StateHasChanged();
    }

    // THE FREEZE-FRAME → brain-backup resurrection: wake at the nearest haven's clinic in an insurance
    // rustbucket. Everything VISIBLE aboard is gone; the tank comes up FULL — the same fuel a new voyage
    // starts with (#477), so the wake puts you back in the game instead of into a fuel hunt. Buried/banked
    // survives (it lives off-ship — other lanes). Never a dead save.
    private void BustedResurrect()
    {
        // #735 · …and only OUT OF THE FREEZE. The guard used to be "is there a death open at all", which was
        // true right up until Enter joined the mouse on this button: a keystroke on a focused button fires
        // the keydown handler AND the browser's own click, so the wake could be asked for twice — and a
        // second wake is a second clinic bill, a second succession and a second captain, off one press. The
        // stage is the fact that decides, so the stage is what is asked. Every road in is one of these three.
        if (_busted is not { } b
            || b.Phase is not (BustedEncounter.Stage.FreezeFrame
                or BustedEncounter.Stage.Impact
                or BustedEncounter.Stage.SurfaceEnd))
        {
            return;
        }

        // #422 arc 2 — how many times this THREAD has woken before (durable), read BEFORE the succession adds
        // this death's retiree, so it counts prior lives only. Gates the clinic's second page below.
        int priorDeaths = ActiveThreadInfo?.Retired.Count ?? 0;

        RemoveHunter(b.HunterId);

        // Evening wind #20 — a surface-overdraw death happens mid-excursion, so the away gig ends here as a
        // failed/aborted trip (no payout) and the surface is folded away, before the resurrection flies the
        // brain-backup to the nearest clinic. The buried/banked hoards live off-ship and are untouched.
        bool wasOnSurface = _surface is not null;
        if (wasOnSurface)
        {
            _surface = null;
            _reevers.Clear();
            _lastNearestReeverRange = null;
            LogAutopilotEvent("🛬 The expedition is written off — the away team scrubs the gig and the body path goes to backup.");
        }

        int wakeTank = WakeTankPulses();

        // Consult the insurance policy at rebirth (issue #227 seam): None-tier returns the uninsured
        // rustbucket + full clinic bill untouched; a covered tier would ease it — the pure rule decides.
        RebirthOutcome outcome = InsuranceRule.ApplyToRebirth(
            _insurance, SimTime, InsuranceRule.DefaultRebirth(wakeTank));
        BustedRule.ResurrectionKit kit = outcome.Kit;

        // The rebirth tax (issue #227): the clinic bill is booked against the stake, floored at 0 — a
        // shortfall is a debt the WIRE lane's bank will reconcile against banked/buried funds at merge.
        int afterBill = Math.Max(0, kit.Credits - outcome.ClinicBillCr);
        LogAutopilotEvent($"🏥 Clinic bill: −{outcome.ClinicBillCr:N0} cr (brain-backup wake-up). Insurance: {_insurance.Tier}.");

        _credits = afterBill;
        _cargoUnits = 0;
        _cargoValue = 0;
        _cargoByClass.Clear();
        _hotCargo.Launder();
        _reactionMassPulses = Math.Min(kit.ReactionMassPulses, ReactionMassCapacityFor(kit.MassLevel));
        _slugAmmo = kit.SlugAmmo;
        _missileAmmo = kit.MissileAmmo;
        _massLevel = kit.MassLevel;
        _sensorLevel = kit.SensorLevel;
        _holdLevel = kit.HoldLevel;
        _telescopeLevel = kit.TelescopeLevel;
        _hasNetJammer = false;

        // A NEW CAPTAIN GETS THEIR OWN NERVE. Nothing reset this before, so the brain-backup woke a fresh
        // captain already carrying the dead one's shattered gauge — and since the commonest death IS the
        // nerve running out, the replacement routinely woke at or near the floor, one fright from dying the
        // same way. Found while wiring #480, which makes it plainly visible: without this the LEDGER would
        // hand the new captain someone else's last bad minute too. The beat clocks, the banked sub-pip
        // pressure and the ledger all go with them.
        // …but the DEAD captain's ledger is the one thing worth keeping across the seam: the card is about
        // to ask "what broke you?", and the answer is in the last few pips they spent. Snapshot it here,
        // THEN wipe the live one — the first cut cleared it before the card ever rendered, so the block
        // silently never appeared (caught in the browser, not by a test).
        _deathNerveLedger = _nerveLedger;
        _nerve = NerveModel.Steady;
        _nerveBeats = NervePips.Beats.Fresh;
        _nerveShockCarry = 0;
        _nerveLedger = [];
        _nerveFlash = null;

        RebuildSensor();
        _heat = HeatState.None;
        _lastAnnouncedHeat = 0;
        b.ClinicBillCr = outcome.ClinicBillCr;
        b.StakeCr = kit.Credits;   // #621: the receipt reads the kit that paid, not a constant beside it
        b.HullDescription = outcome.HullDescription;

        // Wake at the nearest haven's clinic: reset the ship state onto that haven, riding its rails.
        string clinicName = WakeAtNearestHaven();
        b.ClinicName = clinicName;

        // A surface death folded the excursion; fold the surface DECK away too, back to the bare ship deck
        // in flight (the clinic haven the wake placed us at), so the map view resumes under the modal.
        if (wasOnSurface)
        {
            SetDeckForDock(null);
        }

        // Evening wind #20 — THE NEW CAPTAIN, on ANY death-resurrection (this overdraw AND the collector /
        // Bolivia / impact paths): the piracy insurance rolls the thread's identity — a fresh seeded name and
        // a differing face — and the roster keeps the retiree. The corner chip and roster re-read the change.
        IssueSuccessorCaptain(b);

        // #422 arc 2 — THE GLITCH IN THE REBIRTH. You experience the Nebula thread by DYING: for one flat
        // second the wake card reads a line it should not (a seeded flash off this death), and reading it
        // assembles the shard. The oracle (#427) may already have leaked it; this is the first-hand vector,
        // delivered ON the card. The flash shows every rebirth; the shard is gathered once.
        b.RebirthGlitch = NebulaGlitchFlash(b.Seed);
        AssembleNebulaSilently("rebirth-glitch");

        // #422 — THE CLINIC'S SECOND PAGE. Surfaces only once you've woken here BEFORE (a prior death this
        // thread or this session): the fragment's own fiction — a policy number "older than you", carrying
        // entries you never made. Shown on the card the once it is first gathered.
        if ((priorDeaths >= 1 || _rebirthsSeen >= 1) && AssembleNebulaSilently("clinic-ledger"))
        {
            b.ClinicLedger = NebulaLore.ById("clinic-ledger")!.Lore;
        }

        _rebirthsSeen++;

        b.Phase = BustedEncounter.Stage.Resurrected;
        StateHasChanged();
    }

    // #422 arc 2 — the one-flat-second glitch the resurrection card flashes before the welcome loops clean.
    // Seeded off the death so it varies per rebirth yet is deterministic; the "DO NOT REVIVE ORIGINAL" tell
    // is the shard's heart. Presentation only — the canonical shard lore lives in NebulaLore.
    private static readonly string[] NebulaGlitchFlashes =
    [
        "RESTORE FROM PATTERN 40 · SUBSCRIBER LUCID · DO NOT REVIVE ORIGINAL",
        "PATTERN 40 · COPY SPUN · ORIGINAL RETAINED · DO NOT WAKE ORIGINAL",
        "SUBSCRIBER RE-INSTANCED · SEE ARCHIVE · DO NOT REVIVE ORIGINAL",
        "CONTINUITY: BELIEVED · ORIGINAL: FILED · DO NOT DISTURB THE DARK",
    ];

    private static string NebulaGlitchFlash(ulong seed) =>
        NebulaGlitchFlashes[(int)(seed % (ulong)NebulaGlitchFlashes.Length)];

    // Issue the successor captain onto the active universe (Evening wind #20). Reads the retiring identity,
    // rolls + persists the new one through the registry (the #368 fields are editable data), and refreshes
    // the cached roster so the in-play chip and the saved-voyages list show the new face at once. A run with
    // no indexed thread (a legacy save) simply keeps its current identity and the card narrates generically.
    private void IssueSuccessorCaptain(BustedEncounter b)
    {
        if (string.IsNullOrEmpty(_activeThreadId))
        {
            return;
        }

        if (Threads.Get(_activeThreadId) is { } before)
        {
            b.RetiredCaptainName = SpaceSails.Core.Captains.For(before).Name;
        }

        int simDay = (int)(SimTime / 86400);
        if (Threads.IssueSuccessor(_activeThreadId, simDay) is { } after)
        {
            b.NewCaptainName = after.CaptainName;
            b.NewCaptainAvatar = after.AvatarIndex;
            RefreshThreadList(); // the chip (ActiveThreadInfo) + the roster now read the new identity
        }
    }

    // The tank a rebirth wakes with: a FULL base tank — the same fuel a new voyage starts with
    // (Map.Sim's _reactionMassPulses seed and ReactionMassCapacityFor(0) agree on the number).
    //
    // #477: this used to be a "mercy floor" priced from FuelReachability, falling back to the flat
    // autopilot reserve — AutopilotRehearsal.ReservePulses(500) = 90 p. But the autopilot refuses any
    // journey that would eat into its reserve, so a tank set TO the reserve is a tank with no usable
    // fare: the new captain woke at Uranus with 90 p, 0 cr and an empty hold, and could not fly, buy
    // or sell. Owner's ruling — give the rebirth what a new game gives. Death costs the hull, the
    // purse, the hold and every upgrade; it does not also cost you the ability to leave.
    private int WakeTankPulses() => ReactionMassCapacityFor(0);

    private int ReactionMassCapacityFor(int massLevel) => 500 + 150 * massLevel;

    // Set the ship down at the nearest haven (the clinic), riding its velocity — a starter-grade parked
    // state. Returns the haven's name for the wake card.
    private string WakeAtNearestHaven()
    {
        if (_ephemeris is null)
        {
            return "a frontier clinic";
        }

        CelestialBody? nearest = null;
        double best = double.MaxValue;
        foreach (CelestialBody body in _ephemeris.Bodies)
        {
            if (!body.IsHaven)
            {
                continue;
            }

            double d = (_ephemeris.Position(body.Id, SimTime) - _ship.Position).Length;
            if (d < best) { best = d; nearest = body; }
        }

        if (nearest is null)
        {
            return "a frontier clinic";
        }

        Vector2d pos = _ephemeris.Position(nearest.Id, SimTime);

        // #478 · WAKE AT A BERTH, NOT INSIDE THE STATION. This used to be
        //     _ship = new ShipState(pos, vel, SimTime);
        // which parked the new captain at the haven's EXACT CENTRE — distance zero. Every projected sample
        // then sat inside the haven's body radius, ClosestApproach reported a permanent subsurface pass, and
        // the alarm channel shouted "ROCKS AHEAD! — impact with The Tilt" at a ship reading "clamped on" at
        // 0.0 km/s rel. It was never a predictor bug: the ship really was inside the station.
        //
        // A dockable haven now rides the ONE TRUE CLAMP every real arrival uses (co-moving berth offset,
        // welded interior, pinned at dock), so the wake state is byte-for-byte an ordinary docking and the
        // collision projection sees the same honest berth separation it sees after any other arrival.
        if (DockableHavens.IsDockable(nearest))
        {
            ClampOntoHaven(nearest, pos);
        }
        else
        {
            // A haven with no berth to clamp to (a bare waypoint): still never inside it — sit off it at the
            // shared berth offset, co-moving, rather than at its centre.
            _ship = BerthState.CoMoving(_ephemeris, nearest.Id, SimTime, BerthState.BerthOffsetMeters);
        }

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
        return nearest.Name;
    }

    private void RemoveHunter(string hunterId)
    {
        for (int i = _hunters.Count - 1; i >= 0; i--)
        {
            if (_hunters[i].Id == hunterId)
            {
                _hunters.RemoveAt(i);
            }
        }
    }

    // #470: dismissed through the Dismiss() seam so the keyboard comes home. This is the last card of the
    // death→rebirth chain, and the new captain takes the helm the instant it drops — without the way back
    // they would sit at the controls with every key dead.
    private void CloseBusted()
    {
        _busted = null;
        StateHasChanged();
    }

    // Buy the one shipped dice helper: a Boarding-nets jammer, a named +2 on resist/Bolivia initiative
    // (the modifier seam; the shop is a follow-up). One-time purchase, lost on resurrection.
    private void BuyNetJammer()
    {
        if (_hasNetJammer || _credits < NetJammerPriceCr)
        {
            return;
        }

        _credits -= NetJammerPriceCr;
        _hasNetJammer = true;
        ShowPulseMessage("Boarding-nets jammer fitted — +2 on any last stand. The collectors hate it.");
        StateHasChanged();
    }

    // The open BUSTED pop-up's state — grappled, at 1×, the captain choosing. The dice are rolled Core-
    // side (BustedRule / BoliviaEncounter); this holds what's been rolled and which panel to show.
    private sealed class BustedEncounter
    {
        // Impact (#264): a body's surface collected the ship — no collector, no dice, straight to the
        // freeze-frame → clinic re-birth. Reuses this whole encounter so the death machinery is shared.
        // SurfaceEnd (Evening wind #20): a nerve-overdraw death out on the regolith — no collector, no dice.
        // Its own freeze-beat (the cause art + the place-dependent line) before the shared resurrection.
        public enum Stage { Demand, Confiscated, BribedOff, ResistWon, ResistLost, Bolivia, Fled, FreezeFrame, Resurrected, Impact, SurfaceEnd }

        public required string HunterId { get; init; }
        public required string HunterCallsign { get; init; }
        public required int Heat { get; init; }
        public required ulong Seed { get; init; }
        public required DiceRoll Bribe { get; init; }
        public Stage Phase { get; set; } = Stage.Demand;
        public OpposedRoll? ResistRoll { get; set; }
        public BustedRule.Confiscation? Confiscation { get; set; }
        public string? ResultMessage { get; set; }
        public string? ClinicName { get; set; }
        public int ClinicBillCr { get; set; }

        /// <summary>#621 · What the policy actually PAID — read off the rebirth outcome's own kit, not
        /// re-quoted from <see cref="BustedRule.InsuranceCredits"/> in the markup. The receipt was printing
        /// the uninsured constant while the purse was set from the kit; the day a policy tier hands a
        /// different stake the card would have gone on stating the old number with complete confidence, and
        /// two places computing one fact is the bug even while they agree.</summary>
        public int StakeCr { get; set; }
        public string? HullDescription { get; set; }
        public string? ImpactBodyName { get; set; } // #264: the body that collected the ship

        // #422 arc 2 (NEBULA MUTUAL) — the fragments this death surfaces, delivered ON this card. The rebirth
        // glitch flashes on every wake (assembled once); the collector writ is glimpsed at a collector catch;
        // the clinic's second page surfaces on a LATER death. Blank when the beat doesn't apply this time.
        public string? RebirthGlitch { get; set; } // the one-flat-second wake-card glitch flash
        public string? CollectorWrit { get; set; } // the writ glimpsed off a collector at the demand
        public string? ClinicLedger { get; set; }  // the clinic ledger's second page (woken here before)

        // #380 item 1: WHAT killed the captain, and WHERE — so the resurrection card explains the death
        // place-dependently (cause art + a seeded house-voice line) before the brain-backup copy. Defaults to
        // the collector (the BUSTED last stand); the impact path sets Impact. Surface causes are wired ready.
        public DeathCause Cause { get; set; } = DeathCause.Collector;

        /// <summary>#574 · WHERE it happened — her own deck, somebody else's hull, or a suit on a surface.
        /// The card reads the same cause differently depending on this, because a derelict has no regolith
        /// to be run down on and an away team is not standing on a deck.</summary>
        public DeathPlace Place { get; set; } = DeathPlace.OwnShip;
        // Which meter actually ran out (#480 follow-up): true = the nerve overdrew, false = the five blows
        // landed. Before #469 was fixed nerve was effectively the ONLY way to die out there, so the card
        // hardcoded the nerve line; now that the condition marker really decides, the caption must not
        // blame a steady captain's nerve for a mauling.
        public bool NerveRanOut { get; set; }

        public string? DeathBodyName { get; set; } // the place the death is narrated off (moon / body flown into)

        // Evening wind #20 — THE NEW CAPTAIN. On any death-resurrection the piracy insurance issues a fresh
        // name + face; these carry the hand-over for the resurrection card (blank when no thread to succeed —
        // a legacy run — so the card falls back to the plain brain-backup copy).
        public string? RetiredCaptainName { get; set; } // the captain who just walked into the dark
        public string? NewCaptainName { get; set; }     // the name the policy put on the license
        public int NewCaptainAvatar { get; set; }       // the new face in the mirror (art/captain-N.jpg)

        // Bolivia progress
        public OpposedRoll? BoliviaInitiative { get; set; }
        public int BoliviaBeatIndex { get; set; }
        public int BoliviaNet { get; set; }
        public List<BoliviaEncounter.BeatOutcome> BoliviaOutcomes { get; } = [];
        public BoliviaEncounter.Ending? BoliviaEnding { get; set; }
    }

    private void DrawHunters()
    {
        foreach (HunterState hunter in _hunters)
        {
            (float sx, float sy) = _camera.WorldToScreen(hunter.State.Position);
            _renderer!.DrawCircle(sx, sy, 5f, HunterColor, HunterColor);
            _renderer!.DrawText(sx + 8, sy - 6, $"🐺 {hunter.Callsign}", HunterColor);

            double distance = (hunter.State.Position - _ship.Position).Length;
            if (distance <= EncounterRule.WeaponRangeMeters * 3)
            {
                float ringPx = (float)Math.Clamp(EncounterRule.CatchRadiusMeters / _camera.MetersPerPixel, 4, 200);
                _renderer!.DrawCircle(sx, sy, ringPx, null, HunterColor, 1.5f);
            }
        }
    }

    // Thin, read-only projections for the war-room — it never sees Map.razor's private NpcState
    // or HunterState, only the NpcShip/live-state pairs it needs to render (mirrors TrackingCandidates()).
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.Contact> WarRoomContacts()
    {
        var contacts = new List<SpaceSails.Client.Pages.Stations.WarRoom.Contact>();
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Active && !npc.Arrived && !npc.Boarded && npc.CurrentlyObserved)
            {
                contacts.Add(new SpaceSails.Client.Pages.Stations.WarRoom.Contact(
                    npc.Ship, npc.State, npc.WarningShotFired, npc.Bribed));
            }
        }

        return contacts;
    }

    private IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.HunterContact> WarRoomHunters()
    {
        var hunters = new List<SpaceSails.Client.Pages.Stations.WarRoom.HunterContact>(_hunters.Count);
        foreach (HunterState hunter in _hunters)
        {
            hunters.Add(new SpaceSails.Client.Pages.Stations.WarRoom.HunterContact(hunter.Id, hunter.Callsign, hunter.State));
        }

        return hunters;
    }
}

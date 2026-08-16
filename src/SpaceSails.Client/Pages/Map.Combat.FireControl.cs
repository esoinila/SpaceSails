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

// Subject: the gun deck — the captain's word, the magazine, the Norden solution and the fire plan on the map. Part of Map.Combat (#870 split; the header note lives in Map.Combat.cs).
public partial class Map
{

    private string? FireChipLine() => FireLocked
        ? $"🎖 FIRING in {Math.Max(0, (int)(_fireAtSimTime - SimTime))}s"
        : null;

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
}

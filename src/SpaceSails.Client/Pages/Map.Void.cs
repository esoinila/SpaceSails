using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: THE VOID (#638) — the lane that finally routes DeathCause.Void: the ADRIFT predicate, the
// twenty-day clock that rides the vault, the three tellings, and the death itself.
//
// The cause has had a painting (art/death-void.jpg), three lines of prose (VoidLines), a headline and — since
// #636 — a CanHappen law of its own for two years, and nothing in this client had ever set it. `?death=void`
// honestly reported "no lane" rather than faking one. This file is the lane.
//
// THE LAW lives in Core (VoidRule), unit-tested without a browser, exactly like ArrivalStepRule's. What lives
// HERE is only the measuring: the tank, the plan's own steps, and the sweep of the plotted course — and the
// sweep is not a new oracle. It projects with the simulator, sweeps with ClosestApproach.Passes and judges
// each haven with ArrivalStepRule.Check, which is the ✓/✗ bit the arrive row already stands on (the same
// three calls Map.Plot.Arrive makes, pointed at every haven instead of at one chosen body).
public partial class Map
{
    // ── The clock's own state. Both ride the vault (VoidSection), and both are written ONLY while a clock is
    //    actually running, which is what keeps a pre-#638 save byte-identical through a load-and-resave.
    private long _voidDeclaredDay = VoidRule.ClockNotRunning;
    private int _voidLastToldDay = VoidRule.NothingToldYet;

    // ── The sweep's cache. Projecting twenty days and sweeping every body is not a per-frame question, and it
    //    is not a per-frame ANSWER either: the ship has no fuel, so the course cannot change under it. Swept
    //    once per sim-day, and only on the days the cheap arms already hold.
    private long _voidSweptDay = long.MinValue;
    private bool _voidHavenInReach = true;

    /// <summary>
    /// <b>The void's watch.</b> Runs beside the hoard's discovery watch (Map.Combat.UpdateEncounters), for the
    /// same reason and on the same cadence: it resolves whole sim-days as they roll past, whether the captain
    /// is flying, warping or sitting still, and it cannot be skipped by a warp that leaps a fortnight.
    ///
    /// <para>Gated on the captain being ABOARD HER (<see cref="CaptainWasAboardHer"/>, the scuttle's own
    /// predicate). <see cref="DeathNarration.CanHappen"/> makes the void legal in exactly one place —
    /// <see cref="DeathPlace.OwnShip"/> — so a clock that could fire while he is standing on a moon would be
    /// a lane pointed at a law that forbids it. The days still count; only the reckoning waits for him.</para>
    /// </summary>
    private void RunTheVoidWatch()
    {
        if (!_worldReady || _ephemeris is null || _simulator is null || _busted is not null)
        {
            return;
        }

        if (!CaptainWasAboardHer())
        {
            return;
        }

        long today = VoidRule.DayIndex(SimTime);
        bool aPlanStepCanFire = APlanStepCanStillFire();

        // The three cheap arms decide whether the expensive one is even worth asking. A ship with fuel, a
        // clamp on a berth, or one plotted burn left in her is not adrift and never sweeps.
        if (_reactionMassPulses <= 0 && !_docked && !aPlanStepCanFire && today != _voidSweptDay)
        {
            _voidHavenInReach = AHavenStillTakesHer();
            _voidSweptDay = today;
        }

        bool adrift = VoidRule.IsAdrift(_reactionMassPulses, _docked, aPlanStepCanFire, _voidHavenInReach);

        if (!adrift)
        {
            // The ruling: refuelling, a rescue landing aboard, or the course falling into a haven's hands
            // cancels the clock WITHOUT CEREMONY. No banner, no card, no "you were nearly dead" — the state
            // simply stops being true and the count is thrown away.
            if (_voidDeclaredDay != VoidRule.ClockNotRunning)
            {
                _voidDeclaredDay = VoidRule.ClockNotRunning;
                _voidLastToldDay = VoidRule.NothingToldYet;
                _shipAlerts.Clear(AlertKind.Void);
                RequestVaultSave();
            }

            return;
        }

        if (_voidDeclaredDay == VoidRule.ClockNotRunning)
        {
            _voidDeclaredDay = today;
            _voidLastToldDay = VoidRule.NothingToldYet;
            RequestVaultSave();
        }

        int days = VoidRule.DaysElapsed(_voidDeclaredDay, SimTime);
        IReadOnlyList<int> due = VoidRule.TellingsBetween(_voidLastToldDay, days);
        if (due.Count > 0)
        {
            foreach (int day in due)
            {
                TellTheVoid(day);
            }

            _voidLastToldDay = days;
            RequestVaultSave();
        }

        if (VoidRule.TimeIsUp(_voidDeclaredDay, SimTime))
        {
            TheVoidTakesHer();
        }
    }

    /// <summary>
    /// Is there a move left in the plan — a burn that has not fired and has not been struck, or an arrival the
    /// autopilot is already holding a promise about? The canonical idiom, borrowed verbatim from the five
    /// other places that ask it (<c>!Stale &amp;&amp; !Executed &amp;&amp; SimTime &gt; now</c>), plus the armed
    /// arrival, which is the plan's terminal step wearing its ARMED face.
    /// </summary>
    private bool APlanStepCanStillFire()
    {
        if (_armedOrbitBodyId is not null)
        {
            return true;
        }

        foreach (PlanNode node in _planNodes)
        {
            if (!node.Stale && !node.Executed && node.SimTime > _ship.SimTime)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// <b>Does any plotted future touch a berth?</b> The measurement half of
    /// <see cref="VoidRule.AHavenStillTakesHer"/>, and deliberately three borrowed calls and no fourth:
    /// <see cref="Simulator.ProjectAdaptive"/> draws the course, <see cref="ClosestApproach.Passes"/> sweeps
    /// it, and <see cref="ArrivalStepRule.Check"/> judges each pass — the identical chain
    /// <c>Map.Plot.Arrive.CheckArrival</c> runs for the arrive row, aimed at every haven at once instead of at
    /// one chosen body. Which bodies count is <see cref="ArrivableAs"/>, the plan's own answer to "where may a
    /// plan end"; the velocities come off the ribbon the one way this page reads them.
    ///
    /// <para>Returns TRUE — "a haven still takes her" — whenever it cannot see: no ephemeris, no simulator, a
    /// projection too short to be a picture. A predicate that kills the captain when it is blind would be the
    /// worst of this repo's named bug classes, and the safe direction is obvious.</para>
    /// </summary>
    private bool AHavenStillTakesHer()
    {
        if (_ephemeris is null || _simulator is null)
        {
            return true;
        }

        // From the ship herself, not PlanStartState(): a clamped ship is excluded from ADRIFT one arm earlier,
        // so there is no berth to hand over from and nothing for the cast-off's start state to say.
        IReadOnlyList<TrajectorySample> samples = _simulator.ProjectAdaptive(
            _ship, _plan, VoidRule.LookAheadSeconds, maxTimeStep: 3 * 3600, maxSamples: 8000);
        if (samples.Count < 2)
        {
            return true;
        }

        double ribbonEnd = samples[^1].SimTime;
        double sampleStep = Math.Max(1.0, samples[^1].SimTime - samples[^2].SimTime);

        var havens = new List<VoidRule.HavenPass>();
        foreach (ClosestApproach.Pass pass in ClosestApproach.Passes(samples, _ephemeris))
        {
            if (BodyById(pass.BodyId) is not { ParentId: not null } body
                || BodyById(body.ParentId) is not { } parent)
            {
                continue;
            }

            ArrivalStepRule.ArrivalKind kind;
            if (ArrivableAs(body, ArrivalStepRule.ArrivalKind.Dock))
            {
                kind = ArrivalStepRule.ArrivalKind.Dock;
            }
            else if (ArrivableAs(body, ArrivalStepRule.ArrivalKind.Orbit))
            {
                kind = ArrivalStepRule.ArrivalKind.Orbit;
            }
            else
            {
                continue;
            }

            Vector2d shipVel = NodeFrame.VelocityAt(samples, pass.SimTime, _ship.Velocity);
            Vector2d bodyVel = PassBodyVelocity(pass.BodyId, pass.SimTime);
            havens.Add(new VoidRule.HavenPass(
                pass.BodyId, body.Name, kind, pass.Distance, (shipVel - bodyVel).Length,
                OrbitRule.HillRadius(body, parent.Mu), pass.SimTime));
        }

        return VoidRule.AHavenStillTakesHer(havens, ribbonEnd, sampleStep);
    }

    // ===== The tellings (#761): the player is told clearly, thrice, in escalating register =====

    /// <summary>Say the day's piece. The words are <see cref="VoidRule"/>'s; what changes between them is the
    /// SURFACE — a banner and a log line, then a banner, then a card that stops the game.</summary>
    private void TellTheVoid(int day)
    {
        if (day == 0)
        {
            Warp = 1;   // being stranded is not a thing to watch at 10,000×
            _shipAlerts.Raise(AlertKind.Void, AlertSeverity.Red, VoidRule.DeclaredLine, SimTime);
            LogAutopilotEvent($"🕯 {VoidRule.DeclaredLine}");
            return;
        }

        if (day == VoidRule.HalfwayDay)
        {
            // Cleared before it is raised so this counts as a fresh crossing: a same-severity re-raise only
            // edits the text in place, and the halfway mark is a thing the strip should say OUT LOUD to a
            // captain who silenced the first one ten days ago.
            _shipAlerts.Clear(AlertKind.Void);
            _shipAlerts.Raise(AlertKind.Void, AlertSeverity.Red, VoidRule.HalfwayLine, SimTime);
            LogAutopilotEvent($"🕯 {VoidRule.HalfwayLine}");
            return;
        }

        if (day == VoidRule.OneDayLeftDay)
        {
            Warp = 1;
            RaiseTheLongDarkCard();
            LogAutopilotEvent($"🕯 {VoidRule.OneDayLeftLine}");
            RendererInterop.PlayCue("alarm");
            StateHasChanged();
        }
    }

    /// <summary>
    /// #684's precedent, on the last day there is: an outcome that matters is TOLD ON A CARD, in the
    /// authority-card idiom, not muttered into a banner behind somebody else's backdrop. It is the shipping
    /// view-object surface — the same root the gate's own read raises — so it closes on its ✕, on the
    /// backdrop, and on Escape like every other pop-up under the #992 law.
    ///
    /// <para>It carries no picture on purpose. The painting is the DEATH's, and spending it a day early would
    /// burn the one image this whole lane exists to reach.</para>
    ///
    /// <para><b>Building the card and nothing else</b> is deliberate: this is the ONE place it is built, and
    /// the #992 dismissibility law drives exactly this verb (rather than poking the gate field) so a fork that
    /// stopped raising it reddens there. Everything around the beat — the warp drop, the log line, the cue —
    /// belongs to the telling, one frame up.</para>
    /// </summary>
    private void RaiseTheLongDarkCard() =>
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, 0f, 0f,
            VoidRule.OneDayLeftCardLabel, null, VoidRule.OneDayLeftLine);

    // ===== The death itself =====

    /// <summary>
    /// <b>The twenty are up.</b> Through the SAME brain-backup death the collector, the impact and the
    /// regolith all use — the death machinery is shared, never duplicated (the scuttle's own note, and its
    /// own shape: <c>SheGoesWithHim</c>). Nothing is authored here: the painting, the three lines, the
    /// headline and the caption are all <see cref="DeathNarration"/>'s already, and all this does is name the
    /// cause and the place.
    ///
    /// <para>No body name is handed over, and that is the cause speaking: <i>"no beacon, no body"</i> — there
    /// is nothing out here to name.</para>
    /// </summary>
    private void TheVoidTakesHer()
    {
        if (_busted is not null)
        {
            return;   // already mid-reckoning; one death at a time
        }

        Warp = 1;
        _effectiveWarp = 1;

        if (_viewObject is { Label: VoidRule.OneDayLeftCardLabel })
        {
            CloseViewObject();   // the schedule it announced has run out; it does not stand under the card
        }

        RendererInterop.PlayCue("alarm");
        RendererInterop.PlayCue("gameover");

        _busted = new BustedEncounter
        {
            HunterId = string.Empty,          // nobody collected her; nothing did
            HunterCallsign = ShipNameNow(),
            Heat = 0,
            Seed = DiceRule.Seed("void", (long)SimTime),
            Bribe = default,                  // there is nobody out here to bribe
            Phase = BustedEncounter.Stage.SurfaceEnd,   // the generic what-happened beat, data-driven by Cause
            Cause = DeathCause.Void,
            Place = DeathPlace.OwnShip,       // the one place CanHappen allows it (#636)
            DeathBodyName = null,
        };

        _voidDeclaredDay = VoidRule.ClockNotRunning;
        _voidLastToldDay = VoidRule.NothingToldYet;
        _shipAlerts.Clear(AlertKind.Void);
        LogAutopilotEvent("🕯 the long dark closed over her — twenty days adrift, and nothing came");
        StateHasChanged();
    }
}

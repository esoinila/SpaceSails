using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the mothership downlink, the orbit hold at boarding, and the first-contact chirp.
public partial class Map
{
    // on the surface). Set in BeginSurfaceExcursion, read by SurfaceOrbitComms.
    private double _orbitHoldAtBoarding;

    // #327: the in-voice orbit line the surface HUD shows — the ship calling down as its hold erodes. The
    // owner's Miranda maroon was LOVED as story; the SILENCE was the bug. While the shuttle is down and
    // the mothership floats FREE (a moon is no dockable berth), the ship reports its hold every tick:
    // steady → slipping → failing → lost, never buried. Null only OFF-surface; on an excursion it always
    // speaks. A docked ship gets its own calm line (#331 follow-up) — the station holds it, no fuel spent
    // — instead of a hold countdown, and the ladder can never fire (this returns before any StageFor).
    private (string Line, int Severity)? SurfaceOrbitComms()
    {
        if (_surface is null)
        {
            return null; // not on a surface — nothing to report
        }

        // #370: on the away-team gig the HUD's ship-line becomes the AWAY CLOCK — time left in shuttle range
        // (owner: "a mission clock at the away site that ticks down the window"). It supersedes the ordinary
        // hold/docked line while the team is on the gig's site.
        if (_surface is { Expedition: true } && ExpeditionComms() is { } away)
        {
            return away;
        }

        // #394: on the deflection rock the ship-line becomes the DOOM CLOCK — T-minus to impact, naming the
        // stakes ("⏱ IMPACT — RINGSIDE EXCHANGE — T-4:32"). It supersedes the ordinary hold/docked line.
        if (_surface is { Deflection: true } && DeflectionComms() is { } doom)
        {
            return doom;
        }

        if (_dockedHavenId is not null)
        {
            // Owner ruling (#331 follow-up): docked at a station, its mass holds the orbit for us — no
            // fuel spent, no hold to count down. Say so plainly rather than a countdown or a false "∞".
            return (OrbitHold.DockedComms, 0);
        }

        if (_orbitKept)
        {
            double remaining = OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay);
            double boarding = _orbitHoldAtBoarding > 0 ? _orbitHoldAtBoarding : remaining;
            OrbitHold.Stage stage = OrbitHold.StageFor(remaining, boarding);
            return (OrbitHold.Comms(stage, remaining), OrbitHold.Severity(stage));
        }

        // Not keeping. If we boarded WITH a hold, the keeper has since given up (the tank ran dry, a loud
        // handback) — the orbit is degrading: the maroon, announced. If we never had a hold, no one was
        // ever trimming it — a standing red the whole excursion. Either way, loud, never silent.
        return _orbitHoldAtBoarding > 0
            ? (OrbitHold.Comms(OrbitHold.Stage.Lost, 0), OrbitHold.Severity(OrbitHold.Stage.Lost))
            : (OrbitHold.NotHoldingComms, 2);
    }

    // ── COMMS-LOSS · the mothership's telemetry downlink (owner, cruise 2026-07-19). ──────────────────
    //
    // THE HONESTY LAW (CommsLink): this loop advances a pure, seeded DISPLAY phase and snapshots the
    // last-known feed. It touches NO game state a consequence rides on — the ship's real orbit hold, the
    // reaction-mass tank, the away/doom clock and everything else keep advancing in their own fields,
    // untouched. All that changes is what the HUD is ALLOWED to show (SurfaceComms). So a blackout can
    // NEVER strand the captain: the truth continues underneath, liftoff stays player-initiated, and on
    // recovery the live true state snaps back with a catch-up pulse. Withheld confirmation, never denied
    // information — the difference between fair dread and a feels-bad bug.
    private void StepComms(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // The link's clock, clamped like the tide's so a background-tab resume can't leap an episode.
        ex.CommsSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxSurfaceStepSeconds);

        ulong seed = ex.ThreatSeed;
        // Schedule the next episode lazily off the current clock. The onset ODDS rise deep in the site /
        // during interference (CommsOnsetBias) — owner: "more likely deep in a site".
        if (!ex.CommsActive && ex.CommsNextOnset < 0)
        {
            ex.CommsNextOnset = ex.CommsSeconds + CommsLink.NextGap(seed, ex.CommsOnsetIndex, CommsOnsetBias());
        }
        // Cross the onset threshold → the episode begins; capture its shape once (deterministic per index).
        if (!ex.CommsActive && ex.CommsSeconds >= ex.CommsNextOnset)
        {
            ex.CommsActive = true;
            ex.CommsEpisodeStart = ex.CommsNextOnset;
            ex.CommsEpisodeDuration = CommsLink.EpisodeDuration(seed, ex.CommsOnsetIndex);
            ex.CommsEpisodeDeepens = CommsLink.EpisodeDeepens(seed, ex.CommsOnsetIndex);
        }

        CommsLink.Phase phase = ex.CommsActive
            ? CommsLink.PhaseAt(ex.CommsEpisodeStart, ex.CommsEpisodeDuration, ex.CommsEpisodeDeepens, ex.CommsSeconds)
            : CommsLink.Phase.Nominal;

        // Snapshot the last-known feed while the link is clean — this is EXACTLY the truth right now, so a
        // later freeze paints an honestly-recent value. The true line is SurfaceOrbitComms (the honest
        // underlying feed); comms-loss never changes it, only whether we're allowed to show it live.
        if (phase == CommsLink.Phase.Nominal)
        {
            if (SurfaceOrbitComms() is { } liveNow)
            {
                ex.CommsLastLine = liveNow.Line;
                ex.CommsLastSeverity = liveNow.Severity;
            }
            ex.CommsLastContactSeconds = ex.CommsSeconds;
        }

        // First-loss teaching notice (once per excursion): the feed just dropped — the frozen readout is
        // stale, the suit instruments still run true.
        if (phase != CommsLink.Phase.Nominal && !ex.CommsFirstLossAnnounced)
        {
            ex.CommsFirstLossAnnounced = true;
            RendererInterop.PlayCue("alarm");
            ShowPulseMessage(CommsLink.FirstLossPulse);
        }

        // Recovery edge: the episode has ended (phase back to Nominal after being active). Fire the
        // catch-up rush against the TRUE current severity — honest, so a hold that went bad while dark is
        // owned out loud, not hidden.
        if (ex.CommsActive && phase == CommsLink.Phase.Nominal)
        {
            ex.CommsActive = false;
            ex.CommsOnsetIndex++;
            ex.CommsNextOnset = -1; // reseed the next quiet gap from here
            int trueSeverity = SurfaceOrbitComms()?.Severity ?? 0;
            // Only speak recovery on the non-away feed (the orbit-hold ladder) — the away/doom clock never
            // went dark (its number stayed live on the suit), so there's nothing to "catch up" there.
            if (ex is not { Expedition: true } and not { Deflection: true })
            {
                RendererInterop.PlayCue("board");
                ShowPulseMessage(CommsLink.RecoveryPulse(trueSeverity));
            }
        }

        ex.CommsPhase = phase;
    }

    // The onset odds multiplier: the link is strong at the ship (a drop there would be silly), and drops
    // grow likelier the deeper the captain wanders into the site (owner: "more likely deep in a site,
    // during solar interference"). 1× at the tube mouth, up to ~2× deep by the monolith.
    //
    // #637 · It used to read the MOON's axis unconditionally — "are you above the regolith's top rim at
    // y = −20" — and a derelict's whole deck runs −9..+9, so aboard a wreck the early return fired at every
    // point of every hull and the link never degraded anywhere. AwayTeamSide is the one place that knows
    // which door you are on the far side of, and now which axis measures the walk away from it.
    private double CommsOnsetBias() =>
        AwayTeamSide.CommsOnsetBias(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius);

    // A scripted onset (a bad expedition beat, solar interference): if no episode is underway, pull the
    // next one forward to NOW. Pure schedule nudge — it changes WHEN the display gate closes, never the
    // ship's real state, so the honesty law holds untouched.
    private void TriggerCommsEpisode()
    {
        if (_surface is { CommsActive: false } ex)
        {
            ex.CommsNextOnset = ex.CommsSeconds;
        }
    }

    // COMMS-LOSS · the DISPLAY gate over the honest feed. Wraps SurfaceOrbitComms (the true, always-honest
    // mothership line) with the live link phase, returning what the HUD is allowed to show plus the comms
    // state for the renderer's static/greyed treatment. NEVER alters a hard deadline the captain reckons
    // locally: on an away/deflection gig the numeric clock is the SUIT's own count (not a downlink), so it
    // stays live and honest and is only TAGGED unconfirmed; only the orbit-hold ladder (the ship's own
    // telemetry) freezes at last-known. Returns null off-surface, exactly like SurfaceOrbitComms.
    private (string Line, int Severity, int CommsState)? SurfaceComms()
    {
        if (_surface is not { } ex)
        {
            return null;
        }
        if (SurfaceOrbitComms() is not { } live)
        {
            return null;
        }
        CommsLink.Phase phase = ex.CommsPhase;
        if (phase == CommsLink.Phase.Nominal)
        {
            return (live.Line, live.Severity, 0);
        }

        // The away/doom clock is the suit's own reckoning, a hard deadline whose closing costs crew — it
        // must NEVER be withheld (that would strand unfairly). Keep the live number, its severity AND its
        // normal colour (CommsState 0 — an honest instrument must never LOOK lost); only append the text
        // tag flagging that the ship can no longer confirm it. Honest by construction.
        if (ex is { Expedition: true } or { Deflection: true })
        {
            return (live.Line + CommsLink.UnconfirmedTag(phase), live.Severity, 0);
        }

        // The orbit-hold ladder IS the mothership's downlink — freeze it at the last-known value, banner it
        // as stale (how long since contact), and carry the LAST-KNOWN severity (not the true one — we can't
        // hear the true one). The true orbit keeps eroding underneath; recovery reveals it.
        string frozen = ex.CommsLastLine ?? live.Line;
        int frozenSeverity = ex.CommsLastLine is null ? live.Severity : ex.CommsLastSeverity;
        double since = Math.Max(0.0, ex.CommsSeconds - ex.CommsLastContactSeconds);
        return (CommsLink.StaleBanner(phase, since) + frozen, frozenSeverity, (int)phase);
    }

    // #338 addendum · THE GAME'S FIRST SOUND: chirp on the tracker's first-contact edge. Counts the movers
    // the long ear actually HEARS this frame (within detection range), advances the pure edge/hysteresis in
    // MotionTracker.StepChirp, and plays the two-tone radar ping on the 0→N transition. Sound only — the
    // fan and the existing tide/raise notices carry the words; this is the "device chirps in the holster"
    // that makes you look even when the device is slung. Muting is a JS-side master switch (respected there).
    private void StepFirstContactChirp(double dtRealSeconds)
    {
        if (_surface is null)
        {
            return;
        }
        double detection = FanReach();   // #591: shorter with every floor down
        // #583 / #538 · The chirp counts everything on its feet down here — the pack, the sweep team, and
        // the repo crew. The holster device does not know or care whose boots they are, and a boat crew
        // walking up on you is exactly the thing it exists to make you look at. ONE accessor, so the ear and
        // the hull can never disagree about who is walking about in it.
        var entities = EverythingThatMoves();
        int heard = MotionTracker.DetectedMovingCount(_avatarX, _avatarY, entities, detection);
        (_chirp, bool chirp) = MotionTracker.StepChirp(_chirp, heard, dtRealSeconds);
        if (chirp)
        {
            RendererInterop.PlayChirp();
        }
    }
}

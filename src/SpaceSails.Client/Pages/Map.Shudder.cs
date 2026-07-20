using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Shudder — #424 HULL-SHUDDER, the ambient-dread mood-setter (owner, at sea in rough weather
// 2026-07-20: "The ship sometimes shakes… as if joint, in unison, people then decide.. probably just a
// wave"). While the captain walks a populated INTERIOR deck (the ship, a haven bar/hall) or a lab/surface
// site, an occasional seeded tremor rolls: the deck shakes a little, every present NPC/patron freezes for a
// shared held breath — heads up as one — then the room resumes, and a house-voice line speaks the
// unison-decide beat. Mostly it IS nothing; the dread is the pause. The pure spine (cadence, lines, shake
// curve, chill gate) is Core HullShudder; this is the thin client wiring: an accumulator, the fire, and the
// render offset / NPC-freeze the walk view reads.
public partial class Map
{
    // The seeded schedule for the current deck session. Reset (unscheduled) whenever we're not on a walked
    // interior, so a fresh arrival gets a fresh, generous first gap (no shudder in the first ~½ minute).
    private ulong _shudderSeed;
    private double _shudderSeconds;         // deck-time accrued this session (real seconds, clamped)
    private double _shudderNextGap = -1;    // _shudderSeconds at which the next shudder fires (-1 = unscheduled)
    private int _shudderIndex;              // the monotonic shudder ordinal (seeds cadence + line + chill)

    // The shudder playing RIGHT NOW (the shake decay + the unison pause are measured in REAL time, so warp
    // never speeds or slows the held breath).
    private bool _shudderActive;
    private double _shudderOnsetMs;         // real-time ms the tremor began
    private ulong _shudderPlaySeed;         // the seed it fired with (drives the seeded shake curve)
    private double _shudderNpcHoldSimTime;  // the frozen sim-time the unison NPC freeze holds at
    private bool _shudderFreezesNpcs;       // ship/haven freeze the patrons; a surface site only shakes

    // How far the deck-shake throws the whole frame, in pixels, at its peak (the Core curve is unitless
    // [−1,1]; this scales it). Subtle by design — a flex of the steel body, never nauseating (owner's law).
    private const double ShudderShakePixels = 6.0;

    // The same per-frame clamp the tide/comms loops use, so a background-tab resume can't leap the schedule.
    private const double MaxShudderStepSeconds = 0.1;

    // Advance the ambient-shudder schedule one frame. Runs every non-jump frame; a no-op (and a schedule
    // reset) whenever we're not walking an interior deck. When the seeded gap is crossed a shudder fires.
    private void StepShudder(double dtRealSeconds, double nowMs)
    {
        // Only on a walked interior deck, and never under the descent door (the shuttle ride is its own beat).
        if (!_deckMode || _shuttleDescending)
        {
            _shudderActive = false;
            _shudderNextGap = -1;
            _shudderSeconds = 0;
            return;
        }

        // A shudder is playing: let its shake + pause run out (real-time), then schedule the next calm.
        if (_shudderActive)
        {
            double elapsed = (nowMs - _shudderOnsetMs) / 1000.0;
            double tail = Math.Max(HullShudder.ShakeDurationSeconds, HullShudder.PauseDurationSeconds);
            if (elapsed >= tail)
            {
                _shudderActive = false;
                _shudderNextGap = -1; // reschedule off the current clock below
            }
            else
            {
                return; // hold — don't accrue toward the next while this one is still being felt
            }
        }

        _shudderSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxShudderStepSeconds);

        // Schedule the next shudder lazily off the current clock, capturing the seed for the whole interval.
        if (_shudderNextGap < 0)
        {
            _shudderSeed = ShudderSeed();
            _shudderNextGap = _shudderSeconds + HullShudder.NextGap(_shudderSeed, _shudderIndex);
        }

        if (_shudderSeconds >= _shudderNextGap)
        {
            FireShudder(nowMs);
        }
    }

    // The tremor lands: pick the context voice, speak it, maybe carry a chill, and arm the shake + unison
    // pause for the render layer to play out.
    private void FireShudder(double nowMs)
    {
        ulong seed = _shudderSeed;
        int index = _shudderIndex;

        bool deepSite = _surface is not null;                       // a surface / lab / secret-lab landing
        bool haven = _dockedHavenId is not null && HavenInterior.HasInterior(_dockedHavenId);
        HullShudder.Setting setting = HullShudder.SettingFor(deepSite, haven);

        // The bounded escalation (owner: "keep bounded — mostly it IS nothing"): only a deep site that is a
        // secret lab, or a captain whose KAAMOS arc has gone deep, can carry the chill — and even then only
        // some of the time (HullShudder.CarriesChill). A haven/ship shudder never chills.
        bool eligible = deepSite && (AtSecretLab() || ArcGoneDeep());
        bool chill = eligible && HullShudder.CarriesChill(seed, index);

        string line = chill ? HullShudder.ChillLine(seed, index) : HullShudder.Line(setting, seed, index);
        if (chill)
        {
            // A hair of real dread — a nerve prickle far smaller than a Reever's touch (NerveModel.Shock,
            // the flat lump path). Mostly it IS nothing; this is the rare time it isn't quite.
            _nerve = NerveModel.Shock(_nerve, HullShudder.ChillNerveTick);
        }

        _shudderActive = true;
        _shudderOnsetMs = nowMs;
        _shudderPlaySeed = seed;
        _shudderNpcHoldSimTime = SimTime;      // the ONE shared timestamp every present NPC freezes at
        _shudderFreezesNpcs = !deepSite;       // freeze the room's patrons/crew; a surface site only shakes
        _shudderIndex++;

        // A run of shudders close together IS the rough patch the caution PA answers (owner: "the passage is
        // rough tonight"). Count them on a populated interior only — a surface site has no PA.
        if (!deepSite)
        {
            _cautionRun++;
            _cautionLastShudderMs = nowMs;
        }

        RendererInterop.PlayCue("board");
        ShowPulseMessage("〰 " + line);
    }

    // A secret lab underfoot: the door's been found/forced this excursion, or this body is a known lab.
    private bool AtSecretLab() =>
        _surface is { } ex && (ex.Lab is not null || ex.SecretLabForced || ex.SecretLabDoorRevealed);

    // The story arc has gone deep (read-only): the KAAMOS trail is far enough along that the ground reads
    // ominous. (#411/#422 — read only; nothing here edits the arc state.)
    private bool ArcGoneDeep() => _kaamos.CanReachEnceladus || _kaamos.Count >= 3;

    // A stable per-context seed for the cadence: the surface's own threat seed on a landing, else a seed
    // folded off the docked haven's id, else the bare ship. Pure of the context, so a given deck replays a
    // like rhythm without ever reaching for System.Random or the clock.
    private ulong ShudderSeed()
    {
        if (_surface is { } ex)
        {
            return DiceRule.Seed(ex.ThreatSeed, "hull-shudder");
        }
        string tag = _dockedHavenId is { } haven ? $"hull-shudder:haven:{haven}" : "hull-shudder:ship";
        return DiceRule.Seed(ShudderDeckSalt, tag);
    }

    // A fixed salt ("HULL_SH") folded with the deck context so the ship and each haven get a stable rhythm.
    private const ulong ShudderDeckSalt = 0x48_55_4C_4C_5F_53_48UL;

    // ── The render layer reads these each walked frame (DrawWalkFrame). ──

    // The transient deck-shake offset (pixels) to add to the render pan this frame — a pure visual throw of
    // the whole frame that never moves any entity anchor. Zero when no shudder is being felt.
    private (double Dx, double Dy) ShudderShakeOffset()
    {
        if (!_shudderActive)
        {
            return (0.0, 0.0);
        }
        double t = (_frameNowMs - _shudderOnsetMs) / 1000.0;
        (double dx, double dy) = HullShudder.ShakeOffset(_shudderPlaySeed, t);
        return (dx * ShudderShakePixels, dy * ShudderShakePixels);
    }

    // The frozen npc-hold time for the unison pause (ship/haven only), or null when no pause is being held —
    // the walk view fills every present NPC at this one shared timestamp so the whole room freezes together.
    private double? ShudderNpcHold()
    {
        if (!_shudderActive || !_shudderFreezesNpcs)
        {
            return null;
        }
        double t = (_frameNowMs - _shudderOnsetMs) / 1000.0;
        return HullShudder.Pausing(t) ? _shudderNpcHoldSimTime : null;
    }

    // ══ THE UNEXPLAINED SIGNAL (#424 companion, owner 2026-07-20) ════════════════════════════════════════
    //
    // The shudder's colder sibling: a faint distant buzzer off-deck that NOBODY explains. No shake — only the
    // tone (the 'buzzer' cue) and the STAFF (barkeep + station/ship crew, never the drinking patrons) going
    // still for a beat to catch each other's eye. Rarer than the shudder; once the story arc has gone deep
    // the glance holds a beat too long (the COLD line + the longer glance window). Its own seeded schedule,
    // the same idiom — and it fires only where there ARE staff to react (the ship deck or a haven), never on
    // a lonely surface site.

    private double _signalSeconds;          // deck-time accrued this session (real seconds, clamped)
    private double _signalNextGap = -1;     // _signalSeconds at which the next signal fires (-1 = unscheduled)
    private int _signalIndex;               // the monotonic signal ordinal (seeds cadence + line)
    private ulong _signalSeed;              // the seed for the current interval

    private bool _signalActive;             // a glance is being held right now
    private double _signalOnsetMs;          // real-time ms the buzzer sounded
    private bool _signalCold;               // the story-deep escalation: the glance lingers, the cold line

    // Advance the unexplained-signal schedule one frame. Only where staff can react — the ship deck or a
    // haven bar/hall (never a surface site, which has no crew and no bar). A no-op / reset otherwise.
    private void StepSignal(double dtRealSeconds, double nowMs)
    {
        bool staffed = _deckMode && !_shuttleDescending && _surface is null; // a populated interior only
        if (!staffed)
        {
            _signalActive = false;
            _signalNextGap = -1;
            _signalSeconds = 0;
            return;
        }

        if (_signalActive)
        {
            double elapsed = (nowMs - _signalOnsetMs) / 1000.0;
            double tail = _signalCold ? HullShudder.ColdGlanceDurationSeconds : HullShudder.GlanceDurationSeconds;
            if (elapsed >= tail)
            {
                _signalActive = false;
                _signalNextGap = -1;
            }
            else
            {
                return; // hold the glance
            }
        }

        _signalSeconds += Math.Clamp(dtRealSeconds, 0.0, MaxShudderStepSeconds);

        if (_signalNextGap < 0)
        {
            _signalSeed = ShudderSeed();
            _signalNextGap = _signalSeconds + HullShudder.SignalNextGap(_signalSeed, _signalIndex);
        }

        // Don't let a buzzer land on top of a hull-shudder's held breath — one ambient beat at a time.
        if (_signalSeconds >= _signalNextGap && !_shudderActive)
        {
            FireSignal(nowMs);
        }
    }

    private void FireSignal(double nowMs)
    {
        // Colder once the KAAMOS arc has gone deep — the crew's glance lingers, and they've heard it before.
        _signalCold = ArcGoneDeep();
        string line = HullShudder.SignalLine(_signalCold, _signalSeed, _signalIndex);

        _signalActive = true;
        _signalOnsetMs = nowMs;
        _signalIndex++;

        RendererInterop.PlayCue("buzzer"); // a faint low buzzer off-deck (renderer.js); silent if audio is off
        ShowPulseMessage("〜 " + line);
    }

    // True while the staff are holding the shared glance — the walk view turns every working crew member to
    // face the nearest other crew member (the caught eye) for the beat, patrons oblivious.
    private bool SignalCrewGlancing()
    {
        if (!_signalActive)
        {
            return false;
        }
        double t = (_frameNowMs - _signalOnsetMs) / 1000.0;
        return HullShudder.Glancing(t, _signalCold);
    }

    // ══ THE CAUTION ANNOUNCEMENT (#424 third sibling, owner 2026-07-20, mid-storm) ═══════════════════════
    //
    // When a RUN of hull-shudders lands close together (the rough patch), a station/ship PA fires: the house
    // voice asking all hands to move deliberately, the deck isn't itself tonight. No shake, no NPC beat —
    // just the pulse and a hair of nerve nuance (the "this is routine" reassurance steadies the hands a
    // touch; at a deep/story site the same words fray them instead). Scoped to the ship deck / a haven (a PA
    // needs a deck to speak over); a lonely surface site has none. Rate-limited so the storm speaks once,
    // not every shudder.

    private int _cautionRun;                 // hull-shudders in the current (un-lapsed) run
    private double _cautionLastShudderMs;     // real-time ms of the last counted shudder (the run's lapse clock)
    private double _cautionLastFiredMs = double.NegativeInfinity; // so the first rough patch can announce at once
    private int _cautionIndex;               // the monotonic PA ordinal (seeds the line)

    // A run lapses if the shudders stop for this long (the storm passed) — reset the count. Generous vs the
    // shudder mean gap so a genuine run of close shudders still counts as one rough patch.
    private const double RoughPatchLapseMs = 150_000.0;
    // Don't let the PA repeat inside one storm — one announcement, then a long quiet before it can speak again.
    private const double CautionCooldownMs = 130_000.0;

    // Advance the caution PA one frame. Fires once when a rough patch (a run of shudders) has built up and
    // the cooldown has elapsed. A no-op / reset off a populated interior.
    private void StepCaution(double nowMs)
    {
        bool staffed = _deckMode && !_shuttleDescending && _surface is null;
        if (!staffed)
        {
            _cautionRun = 0;
            return;
        }

        // The run lapses if the deck has been quiet (no shudder) for a while — the storm blew over.
        if (_cautionRun > 0 && nowMs - _cautionLastShudderMs > RoughPatchLapseMs)
        {
            _cautionRun = 0;
        }

        if (_cautionRun >= HullShudder.RoughPatchShudderRun
            && nowMs - _cautionLastFiredMs > CautionCooldownMs
            && !_shudderActive) // don't step on a shudder's own held breath
        {
            FireCaution(nowMs);
        }
    }

    private void FireCaution(double nowMs)
    {
        // Colder at a deep/lab/story context. On a haven/ship the cold read comes only from an arc gone deep
        // (the surface's own coldness is the shudder's job; the PA is a deck announcement).
        bool cold = ArcGoneDeep();
        string line = HullShudder.CautionLine(cold, ShudderSeed(), _cautionIndex);

        // A hair of nerve nuance: the reassurance steadies the hands a touch — unless it's the cold read,
        // where "this is routine" plainly isn't, and the words fray instead.
        _nerve = cold
            ? NerveModel.Shock(_nerve, HullShudder.CautionColdTick)
            : NerveModel.Clamp(_nerve + HullShudder.CautionSteadyTick);

        _cautionLastFiredMs = nowMs;
        _cautionIndex++;
        _cautionRun = 0; // the storm has been announced; a fresh run must build before the next PA

        RendererInterop.PlayCue("board");
        ShowPulseMessage(line);
    }
}

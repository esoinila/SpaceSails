using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Aerobrake — #290 🪂 the findable, priced arrival brake. On a skim-atmosphere world's context menu
// (Uranus first; the giants, Earth and Mars — never Titan/Venus, which are landing atmospheres), an
// AEROBRAKE row quotes the honest trade: pulses saved vs the propulsive brake, passes needed, the g/heat
// taken — or a refusal-with-reason (already slow, or too hot for the haze to help). The quote is the pure
// Core Aerobrake.Price, flown once per menu-open and cached (it flies real drag passes — too heavy per
// render). The aerobrake is the THIRD way to pay the arrival bill, beside the propulsive insertion (#262)
// and the warn-and-coast; v1 files the plan and speaks the trade, and the multi-pass campaign flies through
// the existing live skim/sail-hole consequence where the captain reads the real numbers on the plot gauge.
public partial class Map
{
    // The quote cached for the currently-open body menu (Aerobrake.Price flies drag passes — priced once on
    // open, in OpenBodyMenuFor, never per render). Null when the open body is not a skim-atmosphere world.
    private Aerobrake.Quote? _aerobrakeQuote;
    private string? _aerobrakeQuoteBodyId;

    // The world an aerobrake arrival is currently filed for (the arm), or null. The arrival campaign flies
    // through the existing live drag consequence; this records the captain's chosen method for the log/UI.
    private string? _aerobrakeArmedBodyId;

    // ===== #305 THE DICE TRAY + the flown-pass episodes ==========================================

    // The dice event currently shown in the tray (null = closed). THE shared tray state seam: any
    // dice-scripted system raises one through RaiseDiceEvent and the <DiceTray> component renders it.
    private DiceEvent? _diceTrayEvent;

    // Live flown-pass tracking. A "pass" is one flight through the haze — drag rising above the entry
    // bite, then falling back below it. We accumulate the pass's peak g across frames, and on exit roll
    // the 2D6 episode (AerobrakeEpisodes.Roll) seeded from the pass ordinal + sim state.
    private bool _inHazePass;              // currently inside a braking dip
    private double _hazePassPeakG;         // peak drag deceleration (g) of the current dip
    private int _hazePassOrdinal;          // how many episodes cast this run (part of the seed)

    // Drag deceleration (in g) below which the ship is out of the effective braking layer — the pass
    // boundary. A whisper below this is not "in the haze".
    private const double HazeEntryG = 0.05;

    // A dip must bite at least this hard to roll an episode — keeps a feather-light graze from spamming
    // the dice tray; a genuine aerobrake pass bites far harder.
    private const double MinEpisodeG = 0.30;

    // THE SHARED TRAY ENTRY (the adoption recipe): any dice system raises its cast dice here and the
    // single <DiceTray> component shows them. Aerobrake episodes call it now; Reevers/BUSTED/drinks next.
    private void RaiseDiceEvent(DiceEvent ev) => _diceTrayEvent = ev;

    // Dismiss the tray (the captain clicks off it or "let the dice lie").
    private void DismissDiceTray() => _diceTrayEvent = null;

    // Detect a completed flown haze pass and roll its 2D6 episode. Called each stepped frame from the sim
    // loop (right after CheckSailHole, which reads the same per-frame drag peak). Deterministic: the roll
    // is seeded from the pass ordinal and the sim state, never the wall clock.
    private void TrackAerobrakePass()
    {
        double frameG = _frameMaxDragDecel / 9.80665;

        if (frameG > HazeEntryG)
        {
            // Inside a braking dip — start it, or raise the running peak.
            _inHazePass = true;
            _hazePassPeakG = Math.Max(_hazePassPeakG, frameG);
            return;
        }

        if (!_inHazePass)
        {
            return; // out of the haze and were never in it this dip
        }

        // The dip just ended — settle it.
        _inHazePass = false;
        double peakG = _hazePassPeakG;
        _hazePassPeakG = 0;

        if (peakG < MinEpisodeG)
        {
            return; // too light to be a pass worth narrating
        }

        _hazePassOrdinal++;
        long bodyHash = _nearestBody is { } nb ? StableHash(nb.Id) : 0;
        Aerobrake.PassCost deterministic = Aerobrake.CostOfPass(peakG, peakDynamicPressurePa: 0.0);
        ulong seed = AerobrakeEpisodes.Seed(_hazePassOrdinal, (long)_ship.SimTime, bodyHash);
        AerobrakeEpisodes.Episode episode = AerobrakeEpisodes.Roll(deterministic, seed);

        RaiseDiceEvent(episode.Event);
        LogAutopilotEvent($"🎲 aerobrake pass {_hazePassOrdinal}: {episode.Event.Headline}");

        // The dice-scripted currency: a bad roll can tear the sail on a pass the physics alone would have
        // survived. The physics-holed passes were already holed by CheckSailHole this frame; only apply an
        // episode-inflicted tear the live check hasn't already.
        if (episode.HolesSail && !_sailHoled)
        {
            _sailHoled = true;
            _sailRepairedAtSimTime = _ship.SimTime + SailRepairSeconds;
            StaleFutureNodes();
            RendererInterop.PlayCue("board");
        }
    }

    // A stable, platform-independent hash of a body id for seeding (string.GetHashCode is randomised
    // per process — never a seed source; determinism is law). FNV-1a, matched to DiceRule's own mixer.
    private static long StableHash(string s)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (char c in s)
        {
            hash ^= c;
            hash *= prime;
        }

        return unchecked((long)hash);
    }

    // Price the aerobrake for a freshly-opened body menu and cache it. Called from OpenBodyMenuFor. Cheap for
    // the common case: AerobrakeQuoteFor returns null without flying anything unless the body is a skim world.
    private void ComputeAerobrakeQuote(CelestialBody body)
    {
        _aerobrakeQuoteBodyId = body.Id;
        _aerobrakeQuote = AerobrakeQuoteFor(body);
    }

    // The cached quote for the menu body, or null (the razor gate for the AEROBRAKE row).
    private Aerobrake.Quote? AerobrakeMenuQuote(CelestialBody body) =>
        _aerobrakeQuoteBodyId == body.Id ? _aerobrakeQuote : null;

    // Price the aerobrake arrival at a clicked body, or null when it is not a skim-atmosphere planet (a moon,
    // station, airless world, or a landing atmosphere never offers). Pure Core physics — no forked model.
    private Aerobrake.Quote? AerobrakeQuoteFor(CelestialBody body)
    {
        if (_ephemeris is null || body.ParentId is null || !Aerobrake.IsSkimAtmosphere(body.Atmosphere))
        {
            return null;
        }

        return Aerobrake.Price(body, AerobrakeArrivalVinf(body), LongHaulBudgetPulses());
    }

    // The hyperbolic-excess speed the aerobrake works on. Inside the planet's Hill sphere (the owner's
    // stranding — already at Uranus, hot) it is the CURRENT ship-vs-planet excess; out in cruise it is the
    // arrival excess of the cheap departure that would take the ship there now (the same speed #262 brakes).
    private double AerobrakeArrivalVinf(CelestialBody body)
    {
        Vector2d bodyPos = _ephemeris!.Position(body.Id, SimTime);
        Vector2d bodyVel = (_ephemeris.Position(body.Id, SimTime + 1.0) - _ephemeris.Position(body.Id, SimTime - 1.0)) / 2.0;
        double r = (_ship.Position - bodyPos).Length;

        CelestialBody? sun = _ephemeris.Bodies.FirstOrDefault(b => b.Id == body.ParentId);
        double hill = sun is { Mu: > 0 } ? OrbitRule.HillRadius(body, sun.Mu) : double.PositiveInfinity;
        if (r < hill && r > 0)
        {
            double vRel = (_ship.Velocity - bodyVel).Length;
            return Math.Sqrt(Math.Max(0.0, vRel * vRel - 2.0 * body.Mu / r)); // hyperbolic excess from the live state
        }

        LongHaul.Departure dep = LongHaul.SolveDeparture(_ship, _ephemeris, body);
        return dep.Ok ? dep.ArrivalRelativeSpeed : 0.0;
    }

    // The context-menu AEROBRAKE press: set the destination and FILE the aerobrake as the arrival method.
    // v1 records the plan (the third way to pay the arrival bill) and speaks the honest trade; the passes fly
    // through the existing live skim/sail-hole consequence, where the captain reads truth on the plot gauge.
    // Refuses-with-reason (in voice) when the open quote is not offered.
    private void EngageAerobrakeFromMenu(string bodyId)
    {
        CloseBodyMenu();
        if (_ephemeris is null || _ephemeris.Bodies.FirstOrDefault(b => b.Id == bodyId) is not { } body)
        {
            return;
        }

        // Already armed for this world → the press disarms it (mirrors the auto-insert row's toggle).
        if (_aerobrakeArmedBodyId == bodyId)
        {
            _aerobrakeArmedBodyId = null;
            ShowPulseMessage($"🪂 aerobrake at {body.Name} disarmed — back to a propulsive arrival");
            return;
        }

        Aerobrake.Quote? quoted = AerobrakeQuoteFor(body);
        if (quoted is not { } quote || !quote.Offered)
        {
            ShowPulseMessage(quoted is { } refused ? Aerobrake.Refusal(body.Name, refused) : $"🪂 no aerobrake at {body.Name}");
            return;
        }

        SetDestination(bodyId);
        _aerobrakeArmedBodyId = bodyId;
        ShowPulseMessage(Aerobrake.MenuAction(body.Name, quote));
        LogAutopilotEvent(Aerobrake.ArmStep(body.Name, quote));
        LogAutopilotEvent("🪂 " + Aerobrake.Trade(body.Name, quote));
    }
}

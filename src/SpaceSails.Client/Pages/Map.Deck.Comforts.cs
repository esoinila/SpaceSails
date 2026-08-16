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

// Subject: part of Map.Deck (#870 split; the header note lives in Map.Deck.cs) — what the ship does for the person walking her: the rum locker and its tot, the med bay's calming pill, the head, and the bunk — four doors onto the one nerve-relief seam.
public partial class Map
{
    /// <summary>The Galley's "Pour a tot" button funnels through the exact same PourRum the deck
    /// cantina console uses (see InteractAtConsole's Cantina case) — one rum ledger, two doors.</summary>
    private void PourRumFromGalley() => ShowPulseMessage(PourRum(null, NerveModel.DrinkKind.GalleyTot, withExcuse: true));

    private bool RumWobbleActive => (_lastTimestampMs ?? 0) < _wobbleUntilMs;

    // ---- M21: the rum locker (PR-11: shared 1:1 by the Galley desk via PourRumFromGalley) ----
    private int _rumTots;
    private double _lastRumMs = double.MinValue;
    private double _wobbleUntilMs = double.MinValue;
    private string? _lastRumLine;

    private static readonly string[] RumLines =
    [
        "Rum, dark as the void. The view is free.",
        "A tot for the helm. K-77 pretends not to count.",
        "The good bottle — saved since Luna. Today qualifies.",
        "Grog ration doubled. Morale follows.",
        "V-1K reports the rum locker 'adequately defended'.",
        "To absent friends and slow freighters. 🍹",
    ];

    private string PourRum(string? overrideLine, NerveModel.DrinkKind kind = NerveModel.DrinkKind.GalleyTot, bool withExcuse = false)
    {
        double now = _lastTimestampMs ?? 0;
        _rumTots = now - _lastRumMs < 90_000 ? _rumTots + 1 : 1;
        _lastRumMs = now;
        RendererInterop.PlayCue("rum");

        // #308/#321 → #226 — the sanity-relief seam, wired: a drink steadies the nerve. The SAME tot
        // count that tilts the deck also diminishes the relief and, once drunk, stops it (NerveModel owns
        // the law; drunk ⇒ zero). Shared drinks work at any level; a lone tot is weak medicine. Rest and
        // the full R&R economy stay #226. The nerve rides the vault, so a steadier captain persists.
        // #480 · the relief seam gives back WHOLE pips and names them, so a recovery reads exactly like a
        // loss. The relief still prices its own magnitude (level curve, diminishing repeat, drunk ⇒ zero);
        // ApplyNerveRelief only rounds that onto the pip lattice and files the event.
        double beforeNerve = _nerve;
        ApplyNerveRelief(NerveModel.RestoreAmount(kind, _nerve, _rumTots));
        double restored = _nerve - beforeNerve;
        string steadying = NerveModel.SteadyingNote(kind, _rumTots, restored);

        string baseLine = _rumTots >= 3
            ? "That was the third tot. The deck feels… tilty. 🍹"
            : overrideLine ?? RumLines[(int)((SimTime / 60) % RumLines.Length)];
        string line = $"{baseLine} — {steadying}";
        // #339 kin (owner 2026-07-19: "Captain needs excuse to drink :-D") — a SELF-poured drink carries
        // the captain's own declared justification, blustery house-voice HOMAGE (Core owns the pool).
        // Seeded per pour, sim-time salted and tot-count varied, so the same pour speaks the same reason
        // and successive tots don't echo. Pure flavour; shared drinks and the clinical pill get none.
        if (withExcuse)
        {
            ulong excuseSeed = DiceRule.Seed("drink-excuse", (long)SimTime, _rumTots);
            line += $" — “{DrinkExcuses.LineFor(excuseSeed)}”";
        }
        if (_rumTots >= 3)
        {
            _wobbleUntilMs = now + 25_000;
        }
        if (restored > 0)
        {
            RequestVaultSave(); // the nerve moved — persist the steadier hands (galley path saves here too)
        }

        _lastRumLine = line;
        return line;
    }

    // ---- MED BAY 💊 (owner's Evening-wind ruling, 2026-07-18: "change one cabin into med bay where
    //      calming pills can be retrieved to help restore sanity to captain"). CABIN 3 is reborn as the
    //      med bay; its MED KIT console dispenses ONE calming pill per press, restoring the captain's
    //      nerve through the SAME #339 relief seam the galley drink rides (NerveModel.DrinkRestore owns
    //      the law — reused, not parallelled). Stock is a finite shipboard supply that starts at 6;
    //      RESTOCKING is a later lane (no resupply seam yet). ----
    private const int MedBayPillStock = 6;
    private int _pills = MedBayPillStock;

    private string TakePill()
    {
        if (_pills <= 0)
        {
            return "MED KIT: the pill cabinet is empty — the calming stock is spent. (Restock is a later lane.)";
        }

        _pills--;
        // A pill rides no rum spree and never makes the deck tilty (tot 1 → full, un-diminished, never
        // "drunk"); its finite stock is the only limiter. The nerve rides the vault, so a steadier
        // captain persists across sessions — same as the galley path (see PourRum).
        double beforeNerve = _nerve;
        ApplyNerveRelief(NerveModel.RestoreAmount(NerveModel.DrinkKind.CalmingPill, _nerve, totNumber: 1));
        double restored = _nerve - beforeNerve;
        string steadying = NerveModel.SteadyingNote(NerveModel.DrinkKind.CalmingPill, totNumber: 1, restored);
        if (restored > 0)
        {
            RequestVaultSave(); // the nerve moved — persist the steadier hands
        }

        string tail = _pills == 1 ? "1 pill left in the cabinet." : $"{_pills} pills left in the cabinet.";
        return $"MED KIT: a calming pill, dry-swallowed. {tail} — {steadying}";
    }

    // ---- The HEAD 🚽 (owner's live playtest, 2026-07-19: "I tested the toilet :-D … randomized comments
    //      on visiting the toilet … toilet visit could also restore sanity … with rare exceptions of you're
    //      scared of what came out :-D"). The flavour + the seeded usual-vs-scare band live pure in Core
    //      (CabinComforts.VisitToilet — seeded per visit off the sim second, so each press differs yet
    //      replays exactly). A visit USUALLY returns a small dab of nerve (smaller than a drink); ~1-in-12
    //      it's the scare that costs a dab instead. Docked at a bar, one line swears you off the local
    //      house special (Barkeeps knows the names); undocked, only the generic lines are drawn. ----
    private string VisitHead()
    {
        (string? special, string? bar) = DockedBarNames();
        CabinComforts.ToiletVisit visit = CabinComforts.VisitToilet(SimTime, special, bar);

        // A tiny nerve nudge, not a drink. #480: far under a whole pip either way, so it goes through the
        // named seams and BANKS until it owes one, instead of nudging the gauge invisibly.
        double beforeNerve = _nerve;
        if (visit.NerveDelta >= 0)
        {
            ApplyNerveRelief(visit.NerveDelta);
        }
        else
        {
            ApplyNerveShock(-visit.NerveDelta, "some things cannot be unseen in a ship's head");
        }
        if (_nerve != beforeNerve)
        {
            RequestVaultSave(); // the nerve moved — persist the steadier (or shakier) hands
        }

        return visit.Line;
    }

    // The docked bar's house-special + bar names for the toilet's local-riff line, or (null, null) when
    // we're not tied up somewhere with a bar. Barkeeps.For owns the names the drink menu already shows.
    private (string? Special, string? Bar) DockedBarNames() =>
        _dockedHavenId is { } id && Barkeeps.For(id) is { } keep
            ? (keep.DrinkName, keep.BarName)
            : (null, null);

    // ---- The BUNK 🛏 (owner's live ruling, 2026-07-19: "Let's have a sanity restoring sleep action in one
    //      of the cabins" — the REST half of Evening-wind #21). CABIN 1's bunk. [E] turns in for a night:
    //      a solid flat chunk of nerve through the SAME #339 relief seam the drink and pill ride
    //      (NerveModel.DrinkKind.Sleep), and a modest sim-clock cost. Free and unlimited-ish, but HONEST —
    //      a short WELL-RESTED satiety (CabinComforts) means you can't lie down twice in a row to grind
    //      steady hands; you have to actually be tired. CabinComforts owns the law; this is the wiring. ----
    private double _lastSleepSimTime = double.NegativeInfinity; // never slept yet → the first bunk always lands

    private string SleepInBunk()
    {
        double sinceSleep = SimTime - _lastSleepSimTime;
        CabinComforts.SleepResult result = CabinComforts.Sleep(_nerve, sinceSleep, SimTime);

        if (result.WasRested)
        {
            return result.Line; // still well-rested — no restore, no clock cost, just the honest refusal
        }

        // #480: a night's bunk gives WHOLE pips back and says so, like every other relief.
        ApplyNerveRelief(result.Nerve - _nerve);

        // A night's rest advances the sim clock a modest, fixed amount — the SHARED loiter-clock idiom
        // (AdvanceShuttleClock's, minus the shuttle cache watch): clamped, she rides the berth's drift;
        // free-flying, she coasts her conic for the hour. Not a cinematic — the clock simply moves on
        // while you sleep. #733: this used to freeze the hull in place for a whole sim-hour, which is the
        // same lie that flew the HQ quick start into Enceladus — the second copy of it, kept here in the
        // one place the comment already admitted it was a copy.
        if (AdvanceLoiterClock(CabinComforts.SleepSimSeconds))
        {
            // She was on a track that reached a surface inside the hour, and now the freeze-frame is up.
            // No chime and no "you wake steadier" line over the top of it: a sentence saying one thing
            // while the sim did another is the bug class this project has paid for most often.
            return "";
        }
        _lastSleepSimTime = SimTime; // well-rested from the moment you wake

        RendererInterop.PlayCue("rum"); // a soft chime to mark the rest (reuses the galley's gentle cue)
        RequestVaultSave();             // the nerve moved and time passed — persist it
        return result.Line;
    }
}

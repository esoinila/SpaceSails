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

// Subject: the piracy run — the interest target, the intercept clock, the boarding window and the captain's word on a felony. Part of Map.Combat (#870 split; the header note lives in Map.Combat.cs).
public partial class Map
{

    private static readonly string[] PlunderLines =
    [
        "— not a drop of blood spilled",
        "— the crew is singing already",
        "— rum for everyone tonight",
        "— quartermaster's grin says it all",
        "— droids stack it neat as you please",
    ];

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
}

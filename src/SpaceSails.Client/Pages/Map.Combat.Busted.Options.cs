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
/// THE THREE OPTIONS, AND THE DICE THEY ARE ROLLED WITH — submit and be confiscated, bribe this patrol
/// (and not the law), resist, and #535's key that makes the encounter never have happened.
///
/// <para>The purchasable-modifier seam lives here too: one example ships (the boarding-nets jammer) and
/// the shop of helpers is a follow-up (owner §5.0). Every roll is reproducible — the seed is folded from
/// the hunter's identity and the sim moment, upstream in <c>Map.Combat.Busted.cs</c>.</para>
///
/// <para>Split out of <c>Map.Combat.Busted.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
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

    /// <summary>
    /// #535 · <b>PRESENT THE KEY — the fifth exit, and the only one that reaches backwards.</b>
    ///
    /// <para>Every other answer on this card leaves a mark. Submit and the hold goes; bribe and the coin
    /// goes; resist and heat climbs whichever way the dice fall. This one costs the key and nothing else,
    /// and what the captain buys is <b>an encounter that never happened</b>: no heat gained, no entry on the
    /// wire, nobody who remembers, and the pursuer simply gone.</para>
    ///
    /// <h4>The scrub is written as four things this method does NOT do</h4>
    /// <list type="bullet">
    /// <item><b>No heat.</b> <c>_heat</c> is not touched at all — not raised, and not cleared either. A catch
    /// that never happened cannot clear a debt any more than it can add to one, so submitting stays the only
    /// way heat goes to zero.</item>
    /// <item><b>No wire entry, and it never forms.</b> There is no <c>PushNewsEvent</c> here — not
    /// <c>HunterBrokeOff</c>, which is the headline this seam would otherwise reach for, because breaking a
    /// pursuer off is exactly what just happened. The scrub is not a deletion after the fact: nothing is ever
    /// pushed, so there is nothing on the wire to find, and <c>TheKeyLeavesNoTraceTests</c> reads the pushed
    /// list rather than the rendered ticker.</item>
    /// <item><b>Nobody remembers.</b> No contact row, no goodwill, no hostility — <c>_contacts</c> is not
    /// opened. A memory that never forms is the whole of what the issue calls the treasure.</item>
    /// <item><b>Nothing is said.</b> <see cref="BlackOpsKey.NoContactLoggedPlate"/> IS the telling (#761's
    /// law, met by the plate this stage renders), and there is no result message under it. A sentence
    /// narrating the silence would be the game filing a report about a report it did not file.</item>
    /// </list>
    ///
    /// <para>The pursuer leaves through <see cref="RemoveHunter"/>, which is the deterrent's own break-off
    /// (#522/#1090) and the same call the bribe makes — a hand-written removal beside it would be this repo's
    /// first named bug class aimed at a state transition.</para>
    /// </summary>
    private void PresentTheBlackOpsKey()
    {
        if (_busted is not { Phase: BustedEncounter.Stage.Demand } b
            || BlackOpsKey.InThePocket(_satchel) is not { } key)
        {
            return;
        }

        _satchel = [.. BlackOpsKey.Spend(_satchel, key)];
        RemoveHunter(b.HunterId);
        b.Phase = BustedEncounter.Stage.NoContactLogged;
        RequestVaultSave();   // #225: the pocket changed, and it is the only thing that did
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
}

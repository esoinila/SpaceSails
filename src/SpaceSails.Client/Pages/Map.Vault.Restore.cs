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

// Map.Vault.Restore — THE SECTION READERS `ApplyVault` HANDS ITS PIECES TO: the hold, the obligations and
// quests, the dice items, and the resume berth that is always a berth and never a stored orbit.
//
// #251 · MOVED HERE BY PURE MOTION out of `Map.Vault.cs` — see the note at the head of
// `Map.Vault.Threads.cs` for why the file was cut and what "pure motion" is holding here. `ApplyVault`
// itself stays where it is: a shelf of source guards reads its body out of `Map.Vault.cs` by name.
public partial class Map
{
    private void ApplyCargo(CargoSection? cargo)
    {
        if (cargo is null)
        {
            return;
        }

        _cargoByClass.Clear();
        foreach (CargoLine line in cargo.Hold)
        {
            if (line.Units > 0)
            {
                _cargoByClass[line.CargoClass] = line.Units;
            }
        }

        VaultMapper.ApplyHot(cargo.Hot, _hotCargo);
        RecomputeCargoTotals();
    }

    private void RecomputeCargoTotals()
    {
        _cargoUnits = _cargoByClass.Values.Sum();
        _cargoValue = _cargoByClass.Sum(kv => kv.Value * CargoMarket.UnitValue(kv.Key));
    }

    private void ApplyObligationsAndQuests(QuestsSection? quests)
    {
        _favorObligations.Clear();
        _quests.Clear();
        if (quests is null)
        {
            return;
        }

        foreach (FavorObligation obligation in VaultMapper.ToObligations(quests.Obligations))
        {
            _favorObligations.Add(obligation);
        }

        int maxSeq = _questSeq;
        foreach (QuestRecord r in quests.Quests)
        {
            if (!Enum.TryParse(r.Kind, out QuestKind kind))
            {
                continue; // an unknown future quest kind — skip it, keep the rest (tolerant by design)
            }

            Enum.TryParse(r.Status, out QuestState state);
            var quest = new Quest(
                r.Id, kind, r.GiverContactId,
                r.Fields.GetValueOrDefault("targetShipId", ""),
                r.Fields.GetValueOrDefault("targetCallsign", ""),
                r.Title, r.Detail, r.RewardCredits,
                r.Fields.GetValueOrDefault("destBodyId"),
                r.Fields.GetValueOrDefault("sourceBodyId"),
                r.Fields.GetValueOrDefault("pin"))
            {
                State = state,
            };
            _quests.Add(quest);
            maxSeq = Math.Max(maxSeq, TrailingInt(r.Id));
        }

        _questSeq = maxSeq; // new quest ids mint beyond every restored one
    }

    private static int TrailingInt(string id)
    {
        int i = id.Length;
        while (i > 0 && char.IsDigit(id[i - 1]))
        {
            i--;
        }

        return i < id.Length && int.TryParse(id.AsSpan(i), out int n) ? n : 0;
    }

    private void ApplyDiceItems(DiceItemsSection? dice)
    {
        _hasNetJammer = dice?.Items.Any(i => i.ItemId == NetJammerItemId) ?? false;
    }

    // Dock the ship at the resume berth: built fresh alongside the haven at the SAVED sim time (bodies
    // are deterministic from time, so this reconstructs the world exactly), zero relative velocity,
    // clamped. No stored orbit ever crosses the save boundary.
    private void ApplyResumeBerth(ResumeSection? resume, double savedSimTime)
    {
        string? havenId = resume?.HavenId;
        if (_ephemeris is null || havenId is null || _ephemeris.Bodies.All(b => b.Id != havenId))
        {
            ApplyStart("earth"); // no berth to resume at — fall back to the docked tutorial home (Selene Gate)
            return;
        }

        // #256: the id is the truth we resume at; the name is a convenience the picker showed. If the
        // vault's two fields disagree (a real export had HavenId 'the-space-bar' with HavenName 'The
        // Rusty Roadstead' — a nearest-haven computation and a display lookup that resolved DIFFERENT
        // bars), prefer the id and mark the ledger rather than wake the captain at the wrong bar. New
        // saves can't disagree — VaultResume.Select derives both from one HavenLocus — but old files can.
        if (resume?.HavenName is { } savedName && BodyName(havenId) is { } trueName && savedName != trueName)
        {
            LogAutopilotEvent($"the vault's memory of where we were is smudged — it named '{savedName}', but the berth is {trueName}; resuming by id");
        }

        _dockedHavenId = null;
        SetDeckForDock(null);

        Vector2d dockPos = _ephemeris.Position(havenId, savedSimTime);
        // The shared berth build (#269), in the slot the roster gives (#1068) — the SAME call the clamp
        // makes, so a resumed voyage ties up where the clamp tied up rather than a slot away from it.
        // #525 · …and the slot is kept as well as the bearing, for ClampOntoHaven's own reason: the port's PA
        // reads the number out, and a resumed berth that could not say which slot it was in would say the
        // ordinary one while sitting on a reassigned collar.
        _berthSlot = TheSlotTheRosterGives(havenId);
        _ship = BerthState.CoMoving(
            _ephemeris, havenId, savedSimTime, BerthState.BerthOffsetMeters, 0,
            TheBearingOfSlot(havenId, _berthSlot.Value));

        SimTime = savedSimTime;

        // #255 — the freeze class: the world was seeded with movers at boot epoch ~0 BEFORE this vault
        // restore runs (traffic is generated once during OnAfterRender, then the start picker offers
        // "Continue"). Jumping the clock to a far savedSimTime (the owner's 8.3-year "the-tilt" save) would
        // leave every scheduled mover a decade behind — StepNpcs would try to integrate the whole gap at the
        // 60 s NpcTimeStep on the first frame and hard-freeze the tab. Re-seed exactly as a long-haul jump
        // does: keep the pure-rails depots, drop the epoch-0 movers, and let RefillTraffic repopulate fresh
        // AT the resume epoch. A vault resume IS a void crossing — the world we left is a decade gone.
        ReseedWorldForJump(savedSimTime);

        _dockedHavenId = havenId;
        _dockOffset = _ship.Position - dockPos;
        SetDeckForDock(havenId);
        (_avatarX, _avatarY, _avatarHeading) = (2.5, 6, Math.PI / 2);
        _deckMode = true;
        _activeDesk = ShipDesk.Deck;

        ReprojectTrajectory();
        _camera.CenterOn(_ship.Position);
    }
}

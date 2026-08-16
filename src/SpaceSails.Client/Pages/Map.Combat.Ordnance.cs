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

// Subject: ordnance in flight — the slug and the missile once they have left the tube. Part of Map.Combat (#870 split; the header note lives in Map.Combat.cs).
public partial class Map
{

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
}

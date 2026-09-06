using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Npc (the header note lives in Map.Npc.cs) — THE ONE CONTACT THE CAPTAIN HAS SELECTED, and
// the desks that read it. Finding a hull by id, selecting and pinning it, rebuilding the sensor model
// behind the telescope's level, the status and route labels the lists are written from, the candidates
// the tracking post offers, the active-radar toggle and the war room's own track list — and the two
// ways in from elsewhere in the ship: the ledger opening a dossier, and the laser desk ranging a hull,
// whose ping is keyed back through `TheBeamIsKeyed` so anything tracking the far end learns it was
// touched.
public partial class Map
{
    private NpcState? FindNpc(string id)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == id)
            {
                return npc;
            }
        }

        return null;
    }

    private void SelectTarget(string id)
    {
        _selectedTargetId = _selectedTargetId == id ? null : id;
        _pinned = false;
        _pinnedPlan = null;
        _predictedPath = null;
        _predictionDirty = true;
        _captureProgress = 0;

        // Tutorial step 1: selecting a Luna pod.
        if (_selectedTargetId is not null && FindNpc(_selectedTargetId) is { Ship.IsPod: true })
        {
            AdvanceTutorial(0);
        }

        // Second hunt, step 1: singling out the stubborn He3 freighter.
        if (_selectedTargetId == TrafficSchedule.StarterFreighterId)
        {
            AdvanceTutorial(StepSelectFreighter);
        }
    }

    private void TogglePin()
    {
        _pinned = !_pinned;
        _predictionDirty = true;
    }

    private void RebuildSensor()
    {
        double range = SensorModel.Default.RangeMeters * Math.Pow(1.4, _sensorLevel);
        _sensor = new SensorModel(range, SensorModel.Default.GlareHalfAngleRad, SensorModel.Default.GlareRangeFactor);
    }

    // The selected target, but only when it carries an observation (the pin toggle needs one).
    private NpcState? SelectedTrackedTarget()
    {
        NpcState? npc = _selectedTargetId is null ? null : FindNpc(_selectedTargetId);
        return npc?.LastObservation is not null ? npc : null;
    }

    // Secretive haulers (He3 out of pirate country) never hit the public board, but they're
    // still out there — a quiet nod that the outer reaches don't run on the same rules (PR-3).
    private int OffBooksCount => _npcStates.Count(n => !n.Ship.PublishesTimetable);

    private string StatusLabel(NpcState npc)
    {
        if (npc.Arrived)
        {
            return "Arrived";
        }
        if (npc.Boarded)
        {
            return "Boarded";
        }
        if (!npc.Active)
        {
            return "Scheduled";
        }
        if (npc.LastObservation is not { } obs)
        {
            return "En route";
        }

        return _ship.SimTime - obs.SimTime <= TrackedWindowSimSeconds ? "Tracked" : "Lost";
    }

    private string RouteLabel(NpcShip ship) => $"{BodyName(ship.OriginId)}→{BodyName(ship.DestinationId)}";

    // SATURDAY-ANCHOR: methods — parallel lanes append their station methods directly below

    // Thin, read-only projection of the live NPC list for the tracking post — it never sees
    // Map.razor's private NpcState, only ids/callsigns/current physical state to sweep against.
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate> TrackingCandidates()
    {
        var candidates = new List<SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate>(_npcStates.Length + _hunters.Count);
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Active && !npc.Arrived)
            {
                candidates.Add(new SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate(
                    npc.Ship.Id, npc.Ship.Callsign, npc.State,
                    npc.Ship.IsPod, $"cargo: {npc.Ship.CargoClass} ({npc.Ship.CargoUnits}u)"));
            }
        }

        // M27: hunters are sweepable and trackable too — THE targets the telescope should mind.
        foreach (HunterState hunter in _hunters)
        {
            candidates.Add(new SpaceSails.Client.Pages.Stations.TrackingPost.TrackingCandidate(
                hunter.Id, hunter.Callsign, hunter.State, IsThreat: true, CargoDetail: "hired muscle"));
        }

        return candidates;
    }

    // ---- M27: the eyes of the ship — active radar, interest target, intercept clock ----

    private bool _activeRadar;

    private void SetActiveRadar(bool on)
    {
        _activeRadar = on;
        if (on)
        {
            // The ping is loud: every ship in earshot learns exactly where we are.
            foreach (NpcState npc in _npcStates)
            {
                if (npc.Active && !npc.Arrived && RadarRule.HearsPing(_ship.Position, npc.State.Position))
                {
                    _trackingPost?.MarkAware(npc.Ship.Id);
                }
            }

            ShowPulseMessage("Active radar ON — exact returns close in; everyone in earshot hears us 📡");
        }
        else
        {
            ShowPulseMessage("Active radar off — back to passive silence");
        }

        StateHasChanged();
    }

    private void ZoomSensorsBackdrop(double factor) =>
        _camera.ZoomBy(factor, _viewportWidth / 2.0, _viewportHeight / 2.0);

    // M27: the sensor room screens the data; the war room consumes it (owner).
    private IReadOnlyList<SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack> WarRoomSensorTracks()
    {
        var tracks = new List<SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack>();
        if (_trackingPost is null)
        {
            return tracks;
        }

        foreach (TrackedTarget entry in _trackingPost.Entries)
        {
            string callsign = entry.ShipId;
            ShipState state = new(entry.LastObservation.Position, entry.LastObservation.Velocity, SimTime);
            bool isThreat = false;
            foreach (NpcState npc in _npcStates)
            {
                if (npc.Ship.Id == entry.ShipId) { callsign = npc.Ship.Callsign; state = npc.State; break; }
            }

            foreach (HunterState hunter in _hunters)
            {
                if (hunter.Id == entry.ShipId) { callsign = hunter.Callsign; state = hunter.State; isThreat = true; break; }
            }

            tracks.Add(new SpaceSails.Client.Pages.Stations.WarRoom.SensorTrack(
                entry.ShipId, callsign, state, entry.EffectiveQuality(SimTime), isThreat));
        }

        return tracks;
    }

    // The ledger's "→ dossier" link (by ship id): switch to Comms and select that contact's detail.
    private void OpenDossierFromLedger(string shipId)
    {
        SwitchDesk(ShipDesk.Comms);
        SelectCommsShip(shipId);
    }

    private void LaserRangeTarget(string shipId)
    {
        if (_trackingPost is null)
        {
            return;
        }

        NpcState? npc = null;
        foreach (NpcState candidate in _npcStates)
        {
            if (candidate.Ship.Id == shipId) { npc = candidate; break; }
        }

        if (npc is null)
        {
            return;
        }

        (Observation obs, PingEvent ping) = ActiveSensors.LaserRange(shipId, _ship.Position, npc.State.Position, npc.State.Velocity, SimTime);
        _trackingPost.ApplyObservation(obs);
        TheBeamIsKeyed(ping);
        ShowPulseMessage($"Laser ranged {npc.Ship.Callsign} — exact fix, but you're lit up ⚠");
    }

    /// <summary>
    /// #1151 · <b>THE BEAM IS THE BEAM, WHOEVER IS ON THE OTHER END</b> — owner ruling, 2026-09-06: <i>"a
    /// claims call from the captain's remote costs the same exposure a laser ping does."</i>
    ///
    /// <para>The one place a keyed tight-beam is ever <b>paid for</b>. A laser ping pays it here, and so does
    /// a claims call raised over the same link from the handset (<c>Map.Claims.Kiosk.cs</c>) — one
    /// implementation, so the exposure cannot be charged twice over, differently, by two bookkeeping paths
    /// that were meant to say the same thing. What the price IS comes from Core
    /// (<see cref="ActiveSensors.Ping"/>); what paying it means is this line: the thing on the other end now
    /// knows where the beam came from, which is the captain's position at the moment he keyed it.</para>
    ///
    /// <para>Nothing here is about a ship in particular. <see cref="Stations.TrackingPost.MarkAware"/> takes
    /// the id of whatever was on the far end — a hull, a hunter, or the port whose machine took the call —
    /// and the ledger paints the ⚠ for the ones it is also tracking.</para>
    /// </summary>
    private void TheBeamIsKeyed(PingEvent ping) => _trackingPost?.MarkAware(ping.TargetId);
}

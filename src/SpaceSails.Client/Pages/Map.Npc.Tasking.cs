using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Npc (the header note lives in Map.Npc.cs) — WHAT THE CAPTAIN ASKS THE SENSORS TO DO, and
// how the ask is drawn while it is running. The scan wedge and its polygon, the search regions a lost
// contact leaves behind, the three lookups every desk reads a hull's name, place and state through
// (`ContactCallsign`/`ContactPosition`/`ContactState` — a live NPC, the player, or a Core contact record,
// in that order), and the sky-menu verbs themselves: track this hull, scan this patch, sweep or stand
// watch on this corridor. Each verb quotes what the pass will cost in sensor-seconds before it goes on
// the tracking post's queue, and `OnAreaScanCovered` is what the post calls back when one completes.
public partial class Map
{
    private const double IntelScanLeadSeconds = 12 * 3600;   // aim a touch ahead of "now" — a prediction, not a snapshot
    private const double WreckScanRadiusM = 4e10;            // generous box: covers the wreck's drift before the pass lands
    private static readonly RgbaColor ScanWedgeFillColor = new(120, 220, 255, 12);
    private static readonly RgbaColor ScanWedgeDoneFillColor = new(120, 220, 255, 32);
    private static readonly RgbaColor ScanWedgeEdgeColor = new(120, 220, 255, 80);
    private static readonly RgbaColor SearchRegionColor = new(255, 150, 100);

    private void DrawScanWedge()
    {
        if (_trackingPost?.CurrentScan is not { } scan)
        {
            return;
        }

        ScanJob job = scan.Job;
        (float shipX, float shipY) = _camera.WorldToScreen(_ship.Position);
        bool fullCircle = job.ArcWidthRad >= Math.Tau - 1e-9;

        // The whole aimed wedge faint, then the swept-so-far portion brighter — the sensors
        // chief literally watches the exposure fill in on the sky.
        DrawWedgePolygon(job.CenterBearingRad - job.ArcWidthRad / 2, job.ArcWidthRad,
            ScanWedgeFillColor, fullCircle, shipX, shipY);
        double sweptArc = job.ArcWidthRad * Math.Clamp(scan.Progress, 0, 1);
        if (sweptArc > 1e-6)
        {
            DrawWedgePolygon(job.CenterBearingRad - job.ArcWidthRad / 2, sweptArc,
                ScanWedgeDoneFillColor, fullCircle && scan.Progress >= 1, shipX, shipY);
        }

        _renderer!.DrawText(shipX, shipY + 26, $"📡 {scan.Label} · {(int)(scan.Progress * 100)}%",
            ScanWedgeEdgeColor with { A = 200 }, "11px sans-serif", TextAlign.Center);
    }

    private void DrawWedgePolygon(double startBearing, double arc, RgbaColor fill, bool fullCircle,
        float shipX, float shipY)
    {
        const int arcSteps = 28;
        Span<float> points = stackalloc float[(arcSteps + 2) * 2];
        int w = 0;
        if (!fullCircle)
        {
            points[w++] = shipX;
            points[w++] = shipY;
        }

        for (int i = 0; i <= arcSteps; i++)
        {
            double bearing = startBearing + arc * i / arcSteps;
            Vector2d look = new(Math.Cos(bearing), Math.Sin(bearing));
            double range = _trackingPost!.TelescopeRangeAlong(look);
            (float x, float y) = _camera.WorldToScreen(_ship.Position + look * range);
            points[w++] = x;
            points[w++] = y;
        }

        _renderer!.DrawPolygon(points[..w], fill, ScanWedgeEdgeColor, 1f);
    }

    private void DrawLostSearchRegions()
    {
        if (_trackingPost is null)
        {
            return;
        }

        foreach (LostTrack lost in _trackingPost.LostTrackEntries)
        {
            Vector2d center = _trackingPost.LostCenter(lost);
            (float cx, float cy) = _camera.WorldToScreen(center);
            float radiusPx = (float)Math.Max(8, lost.SearchRadius(SimTime) / _camera.MetersPerPixel);
            byte pulse = (byte)(120 + 60 * Math.Sin(_frameNowMs / 250.0));
            RgbaColor stroke = SearchRegionColor with { A = pulse };
            _renderer!.DrawCircle(cx, cy, radiusPx, SearchRegionColor with { A = 10 }, stroke, 1.5f);
            _renderer.DrawText(cx, cy - radiusPx - 6, $"🔍 lost — {ContactCallsign(lost.ShipId)}",
                stroke, "11px sans-serif", TextAlign.Center);
        }
    }

    private string ContactCallsign(string shipId)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == shipId)
            {
                return npc.Ship.Callsign;
            }
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == shipId)
            {
                return hunter.Callsign;
            }
        }

        return shipId;
    }

    private Vector2d? ContactPosition(string shipId) => ContactState(shipId)?.Position;

    /// <summary>A contact's live physical state by id — traffic OR hired muscle. The dossier, the
    /// telescope ledger and the map reticle all need the same answer for the same id, and a hunter
    /// is never in <c>_npcStates</c> (see <see cref="TrackShipFromMenu"/>), so the lookup has to walk
    /// both rosters or it silently knows nothing about the one ship that is hunting us.</summary>
    private ShipState? ContactState(string shipId)
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Ship.Id == shipId)
            {
                return npc.State;
            }
        }

        foreach (HunterState hunter in _hunters)
        {
            if (hunter.Id == shipId)
            {
                return hunter.State;
            }
        }

        return null;
    }

    /// <summary>
    /// #962 · PUT THE SCOPE ON HER. Owner, pressing 📡 sharpen fix on the Debt Collector's dossier and
    /// watching nothing at all happen: <i>"I click sharpen fix but the sensors do nothing useful … It says
    /// we are not tracking the debt collector and we should ... but really HOW??????"</i>
    ///
    /// <para>He was pressing a dead button. This method resolved its subject through <c>FindNpc</c>, which
    /// walks <c>_npcStates</c> only — a hunter lives in <c>_hunters</c>, so every collector id fell out of
    /// the very first guard and returned in silence: no ledger entry, no queued pass, not even a pulse
    /// saying why. (The same NPC-shaped assumption had already been caught once, in the click picker —
    /// Map.UiState.cs's "a hunter isn't in _npcStates, so it was never in this list" note. It was fixed
    /// there and not here.)</para>
    ///
    /// <para>So the subject is resolved the way the DOSSIER resolves it — by contact, across both rosters —
    /// and the button now does both halves of what its own tooltip promises: her fix goes on the telescope
    /// ledger, AND a <see cref="SensorTask.TrackUpdate"/> jumps to the front of the carousel so the very
    /// next thing the instrument looks at is her, and the "Sensor tasks" list says so out loud. Owner:
    /// <i>"Scanning the debt collector should show on the task list as the only job now."</i></para>
    ///
    /// <para><b>…and then it was still dead with the telescopes full, which is the standing the owner's own
    /// screenshot was taken in: "Tracked targets (1 / 1)", the destination depot holding the only slot,
    /// "1 tracked, 1 slipped (telescopes full)".</b> The refused ledger entry was reported honestly — but
    /// the queued pass was a <see cref="SensorTask.TrackUpdate"/>, the LEDGER'S standing custody pass, and
    /// the tracking post sweeps those out for any contact the ledger does not hold. So the order was placed
    /// and deleted again within one tick, and the pulse's promise ("she is the next look") was a sentence
    /// the sim overruled a moment later — this repo's own named bug class, on the very button filed for it.
    /// A captain's look at a contact we cannot hold is <see cref="SensorTask.SharpenFix"/> now: one-shot,
    /// nobody's housekeeping, and it stays on the list until the glass has actually taken it.</para>
    /// </summary>
    private void TrackShipFromMenu(string id)
    {
        if (_trackingPost is null)
        {
            return;
        }

        // Traffic has to have been SEEN before we have anything honest to enter (optical truth, M27).
        // A hunter is a contact the gun deck already reads exactly — the dossier quotes her state
        // straight off _hunters — so there is no observation gate to fail for her.
        if (FindNpc(id) is { CurrentlyObserved: false })
        {
            ShowPulseMessage("No live contact — the telescope needs a sweep fix or laser ranging first");
            CloseShipMenu();
            return;
        }

        if (ContactState(id) is not { } state)
        {
            return; // neither traffic nor muscle: nothing out there to point at
        }

        string callsign = ContactCallsign(id);
        bool held = _trackingPost.ApplyObservation(new Observation(id, SimTime, state.Position, state.Velocity));

        // #962 · WHICH pass is ordered depends on whether we got custody, and it has to: a STANDING custody
        // pass is the ledger's, re-queued each tick while the entry lives and swept out when it dies — so
        // ordering one for a contact the full ledger just refused put a job on the list that the very next
        // tick deleted. A captain's one-shot look is his own, and stays until the glass has taken it.
        _trackingPost.EnqueueAndPrioritize(held
            ? SensorTask.TrackUpdate(id, callsign)
            : SensorTask.SharpenFix(id, callsign));
        ShowPulseMessage(held
            ? $"{callsign} on the telescope ledger — the scope swings onto her next 📡"
            : $"Telescopes full — {callsign} is the next look, but we can't hold custody; drop a track on the Sensors desk");

        CloseShipMenu();

        // #973 L4 · …and if the hull the scope just swung onto is the one he would not sign the manifest for,
        // the boat deck comes back. Said AFTER the ledger's own receipt, so the instrument reports first and
        // the memory is the line the captain is left holding.
        TheOldShipIsSeen(id);
    }

    /// <summary>What a scan would cost in telescope time — shown before the player commits.</summary>
    private string ScanCostText(Vector2d center, double radius) => CostText(SensorTaskGeometry.Duration(
        SensorTask.AreaScan(center, radius, "probe"),
        SensorTaskGeometry.WedgeToward(_ship.Position, center, radius), 1 + 0.5 * _telescopeLevel));

    private string SweepCostText(ScanJob job) => CostText(
        Math.Max(SensorTaskGeometry.MinPassSeconds, job.DurationSeconds) / (1 + 0.5 * _telescopeLevel));

    private static string CostText(double seconds) =>
        seconds < 3600 ? $"≈ {seconds / 60:F0} min telescope time" : $"≈ {seconds / 3600:F1} h telescope time";

    private string SkyScanLabel(Vector2d point) =>
        $"sky scan · {FormatDistance((point - _ship.Position).Length)} out";

    private void ScanAreaFromMenu(Vector2d center, double radius, string label)
    {
        if (_trackingPost is null)
        {
            return;
        }

        ShowPulseMessage(_trackingPost.EnqueueTask(SensorTask.AreaScan(center, radius, label))
            ? $"🔭 Queued: {label}"
            : "That patch is already on the sensor tasks queue");
        CloseShipMenu();
        CloseBodyMenu();
        CloseSkyMenu();
    }

    // Tuesday plan PR-A (the reveal): the telescope has swept some of a disc of sky. If a hidden body's
    // TRUE position is inside that disc and the beam has already been over it, the scope resolved it —
    // chart it.
    //
    // #240 · THE GLINT COMES WHEN THE BEAM CROSSES HER, NOT AT 100 %. Owner, watching the roadster scan
    // climb: "Is it randomized now, the point when we find the car, or is it always at 100%? We might get
    // lucky earlier also?" It was always 100 %: PR-A hung the reveal on the pass's COMPLETION instant, so
    // wherever in the swept sky she actually sat, she was found at the end.
    //
    // Luck by geometry, not by dice. The sweep's coverage-over-time was already fully defined — the wedge
    // aims it and the pass's progress times it — so the only thing added is the question this asks of it:
    // has the beam been past her bearing yet? Early in the arc and she glints at 12 %; late and it is 96 %;
    // and it is the same fraction every time, on any machine, because none of it is random. A pass that
    // never covers her still completes empty, honestly.
    private void OnAreaScanCovered(AreaScanCoverage scan)
    {
        if (_ephemeris is null)
        {
            return;
        }
        foreach (string id in _hiddenBodyIds)
        {
            if (_revealedBodyIds.Contains(id))
            {
                continue;
            }
            Vector2d truePos = _ephemeris.Position(id, scan.SimTime);
            if ((truePos - scan.Center).Length > scan.Radius)
            {
                continue;
            }

            // The completing pass reveals everything in the disc exactly as it always did — that is the
            // backstop, and it is why a contact that drifted in late is never lost. Mid-pass, she has to
            // have been swept: her bearing's own moment in the arc must already have gone by.
            if (scan.Covered < 1.0)
            {
                double? crossesAt = scan.Job.CoverageFraction(
                    TrackingStation.Bearing(truePos - scan.Observer));
                if (crossesAt is not { } moment || moment > scan.Covered)
                {
                    continue;
                }
            }

            RevealBody(id, WreckRevealMessage(id));
            _scopeIntel.RemoveAll(si => si.BodyId == id);
        }
    }

    private void SweepCorridorFromMenu(CorridorRegion lane, bool standing)
    {
        if (_trackingPost is null)
        {
            return;
        }

        SensorTask task = SensorTask.CorridorSweep(lane.AId, lane.BId,
            standing ? $"{lane.PairName} lane watch" : $"{lane.PairName} lane sweep", recurring: standing);
        ShowPulseMessage(_trackingPost.EnqueueTask(task)
            ? (standing ? $"🔁 Standing watch on the {lane.Name}" : $"📡 Sweeping the {lane.Name}")
            : $"The {lane.Name} is already on the queue");
        CloseSkyMenu();
    }
}

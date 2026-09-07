using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Npc (the header note lives in Map.Npc.cs) — WHAT THE TRAFFIC PAINTS ON THE MAP. Two
// passes and nothing else: `DrawNpcs`, which gives a currently observed contact a solid dot and its
// callsign and everything ever seen a dim hollow marker at its last known position, and
// `DrawPredictionCone`, which strides the pinned contact's predicted path down to ~300 points and stops
// drawing the moment the cone is wider than a prediction can mean anything at.
public partial class Map
{
    // NPC markers: solid dot + callsign only while currently observed; otherwise a dim hollow marker
    // at the last known position for any ship ever seen.
    private void DrawNpcs()
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Arrived)
            {
                continue;
            }

            // 🗺 Layers (#405): hidden classes neither draw nor answer clicks (the picker checks too).
            // Depots ride ports.depots; a live contact and its last-seen ghost split traffic's leaves,
            // matched to the observed-vs-last-seen branches below.
            bool isDepot = npc.Ship.DepotBodyId is not null;
            string npcLeaf = isDepot
                ? "ports.depots"
                : npc.Active && npc.CurrentlyObserved ? "traffic.live" : "traffic.ghosts";
            if (!LayerVisible(npcLeaf))
            {
                continue;
            }

            // Tracking-post emphasis (vision ¶14): a ship on the ledger gets a brighter marker
            // plus a small ring sized off the ledger's own uncertainty growth (no separate
            // PathPredictor solve here — just the same half-width formula, cheap enough to draw
            // every frame for a handful of tracked contacts).
            TrackedTarget track = default;
            bool tracked = _trackingPost is not null && _trackingPost.TryGetTrack(npc.Ship.Id, out track);

            // PR-5: a subtle ring for anything co-orbiting the body the ship is currently bound to
            // (the same "local space" set CommerceRule.ContactsAt would list) — the proximity
            // affordance made visible on the map itself, not just in the Local Space panel.
            bool coOrbiting = _orbitedBodyId is not null && (npc.Ship.DepotBodyId == _orbitedBodyId
                || (npc.Ship.DepotBodyId is null
                    && (npc.State.Position - _orbitedBodyPosition).LengthSquared <= _orbitedBodyHillRadius * _orbitedBodyHillRadius));

            if (npc.Active && npc.CurrentlyObserved)
            {
                (float sx, float sy) = _camera.WorldToScreen(npc.State.Position);
                RgbaColor color = npc.Disabled ? DisabledNpcColor : tracked ? TrackedNpcColor : NpcColor;
                _renderer!.DrawCircle(sx, sy, 4f, color, color);
                if (npc.Disabled)
                {
                    // M28: a holed sail reads as a hulk — gray dot, small broken ring.
                    _renderer!.DrawCircle(sx, sy, 7f, null, DisabledNpcColor with { A = 140 }, 1f);
                    _renderer!.DrawText(sx + 6, sy + 8, "adrift", DisabledNpcColor);
                }
                if (tracked)
                {
                    double dt = Math.Max(0, SimTime - track.LastObservation.SimTime);
                    double uncertainty = (PredictedPath.BaseHalfWidthMeters + PredictedPath.VelocitySigma * dt)
                        * track.UncertaintyScale(SimTime);
                    float ringPx = (float)Math.Clamp(uncertainty / _camera.MetersPerPixel, 6, 40);
                    _renderer!.DrawCircle(sx, sy, ringPx, null, TrackedNpcColor, 1.5f);
                }

                if (coOrbiting)
                {
                    _renderer!.DrawCircle(sx, sy, 7f, null, LocalContactRingColor, 1f);
                }

                // #402 follow-up: a DEPOT's name (a parked cargo pod) is minor clutter — route it through
                // the frame's de-collision queue so the depot pack around a station knot stops smearing
                // over itself and over the body/threat labels (it yields at LabelPriorityStation). A live
                // CONTACT stays drawn straight to the canvas: the owner's rule is that a ship is never
                // hidden, so its label is never culled.
                if (isDepot)
                {
                    EnqueueNavLabel(sx + 8, sy - 6, npc.Ship.Callsign, color, LabelPriorityStation);
                }
                else
                {
                    _renderer!.DrawText(sx + 8, sy - 6, npc.Ship.Callsign, color);
                }
            }
            else if (npc.LastObservation is { } obs)
            {
                // Anything we've EVER detected stays ON the map, dead-reckoned from its last fix to
                // NOW and LABELLED — a contact you've seen must never silently blink out between
                // sweeps (owner: "I always want to see all ships... never hide one unless there's
                // true interference to visibility, and surely not at this close"). Only a ship we've
                // genuinely never detected stays dark (that IS the interference). It's a coast
                // estimate, not a live fix — dim colour + a "no live fix" tag, plus a growing
                // uncertainty ring for ledger-tracked contacts (whose drift we actually model) — and
                // the sensors keep working to re-acquire it (passive watch / LostSearch).
                double dt = Math.Max(0, SimTime - obs.SimTime);
                Vector2d reckoned = obs.Position + obs.Velocity * dt;
                (float sx, float sy) = _camera.WorldToScreen(reckoned);
                RgbaColor c = tracked ? TrackedNpcLastSeenColor : NpcLastSeenColor;
                _renderer!.DrawCircle(sx, sy, 4f, null, c);
                if (tracked)
                {
                    double uncertainty = (PredictedPath.BaseHalfWidthMeters + PredictedPath.VelocitySigma * dt)
                        * track.UncertaintyScale(SimTime);
                    float ringPx = (float)Math.Clamp(uncertainty / _camera.MetersPerPixel, 6, 40);
                    _renderer!.DrawCircle(sx, sy, ringPx, null, c with { A = 120 }, 1f);
                }
                // #402 follow-up: a last-seen DEPOT ghost de-collides through the queue too (same rule as
                // the live branch); a contact ghost stays drawn straight so it's never culled off the map.
                if (isDepot)
                {
                    EnqueueNavLabel(sx + 8, sy - 6, $"{npc.Ship.Callsign} · no live fix", c, LabelPriorityStation);
                }
                else
                {
                    _renderer!.DrawText(sx + 8, sy - 6, $"{npc.Ship.Callsign} · no live fix", c);
                }
            }
        }
    }

    // The prediction cone: center line plus two boundaries offset ±HalfWidthAt(t) perpendicular to the
    // local path direction. Drawn from the current sim time forward so the near end visibly widens
    // while the target is unobserved (Δt grows) and snaps tight on the next contact.
    private void DrawPredictionCone()
    {
        PredictedPath? path = _predictedPath;
        if (path is null)
        {
            return;
        }

        IReadOnlyList<TrajectorySample> s = path.Samples;
        if (s.Count < 2)
        {
            return;
        }

        int start = 0;
        while (start < s.Count - 1 && s[start].SimTime < _ship.SimTime)
        {
            start++;
        }

        int remaining = s.Count - start;
        if (remaining < 2)
        {
            return;
        }

        int stride = Math.Max(1, remaining / ConeTargetPoints);
        int maxPoints = remaining / stride + 2;
        int maxFloats = maxPoints * 2;
        if (_coneCenter.Length < maxFloats)
        {
            _coneCenter = new float[maxFloats];
            _coneUpper = new float[maxFloats];
            _coneLower = new float[maxFloats];
        }

        int w = 0;
        void Emit(int i)
        {
            TrajectorySample sample = s[i];
            int prev = Math.Max(0, i - stride);
            int next = Math.Min(s.Count - 1, i + stride);
            Vector2d dir = (s[next].Position - s[prev].Position).Normalized();
            Vector2d perp = new(-dir.Y, dir.X);
            double halfWidth = path.HalfWidthAt(sample.SimTime);

            (float cx, float cy) = _camera.WorldToScreen(PlotFrame(sample.Position, sample.SimTime));
            (float ux, float uy) = _camera.WorldToScreen(PlotFrame(sample.Position + perp * halfWidth, sample.SimTime));
            (float lx, float ly) = _camera.WorldToScreen(PlotFrame(sample.Position - perp * halfWidth, sample.SimTime));
            _coneCenter[w] = cx; _coneUpper[w] = ux; _coneLower[w] = lx;
            _coneCenter[w + 1] = cy; _coneUpper[w + 1] = uy; _coneLower[w + 1] = ly;
            w += 2;
        }

        // #145 — in a Hill-sphere frame the cone is clipped to the same local-timescale window as the
        // ship ribbon, so a co-moving NPC track doesn't coil around the giant either. Sun frame: no cap.
        double coneCutoff = FrameDisplayWindowSeconds() is { } win ? _ship.SimTime + win : double.PositiveInfinity;

        int last = start;
        for (int i = start; i < s.Count; i += stride)
        {
            // Beyond ~2 AU of uncertainty the cone means "could be anywhere" — drawing it just
            // fans giant lines across the map. Truncate the whole track there.
            if (path.HalfWidthAt(s[i].SimTime) > ConeMaxHalfWidthMeters || s[i].SimTime > coneCutoff)
            {
                break;
            }

            Emit(i);
            last = i;
        }
        if (last != s.Count - 1 && s[^1].SimTime <= coneCutoff && path.HalfWidthAt(s[^1].SimTime) <= ConeMaxHalfWidthMeters)
        {
            Emit(s.Count - 1);
        }

        if (w < 4)
        {
            return;
        }

        _renderer!.DrawPolyline(_coneUpper.AsSpan(0, w), ConeBoundaryColor);
        _renderer!.DrawPolyline(_coneLower.AsSpan(0, w), ConeBoundaryColor);
        _renderer!.DrawPolyline(_coneCenter.AsSpan(0, w), ConeCenterColor);
    }
}

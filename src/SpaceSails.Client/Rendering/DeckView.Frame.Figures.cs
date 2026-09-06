using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// DeckView.Frame.Figures — WHAT STANDS IN THE ROOM, drawn over the built world and still UNDER the dark.
// The ship's own dressing (crates, the shuttle in its cradle, the reactor), the seats, and every figure on
// the deck — droids, patrons, sweepers, Reevers — together with #424's crew glance, the little answer that
// turns the working staff toward each other for one beat when the off-deck buzzer sounds, and the three
// name tests (`IsCrew`, `IsSweeper`, `IsPatron`) that decide who counts as staff. Pure motion out of the
// 1,393-line DeckView.Frame.cs: not one mark moved, and EveryFrameHashesTheSameTests says so per frame.

public sealed partial class DeckView
{
    /// <summary>#870 lane 7b · The ship's own dressing, and only hers: the crates in the top-port hold,
    /// the shuttle in its cradle or away doing piracy, and the reactor with #295's charge conduit. A bare
    /// haven room has none of it; a docked complex still contains the ship.</summary>
    private void DressTheShip(
        in State state, double simTime, float scale, Func<double, double, (float X, float Y)> project)
    {
        // Cargo crates: one per unit aboard (in the top-port hold now — #295).
        for (int i = 0; i < Math.Min(state.CargoUnits, 12); i++)
        {
            (float cx, float cy) = project(-10 + (i % 4) * 1.9, 5 + (i / 4) * 1.6);
            DrawBox(cx, cy, 0.65f * scale, CrateColor);
        }

        // Shuttle in its cradle (bottom-port bay now — #295) — or away doing piracy.
        if (!state.ShuttleAway)
        {
            DrawShuttle(project(-6.5, -6.5), scale, simTime);
        }
        else
        {
            (float bx, float by) = project(-6.5, -6.5);
            _renderer.DrawText(bx, by, "— AWAY —", new RgbaColor(255, 170, 80, 200), "bold 11px monospace", TextAlign.Center);
            if (Math.Sin(simTime * 0.005) > 0)
            {
                DrawSeg(project(-9, -9.9), project(-5, -9.9), new RgbaColor(255, 120, 80, 220), 3f);
            }
        }

        // Reactor + charge conduit (engine room).
        (float rx, float ry) = project(-19, 2.5);
        _renderer.DrawCircle(rx, ry, 1.6f * scale, null, InnerLine, 2f);
        double throb = 0.5 + 0.5 * Math.Sin(simTime * 0.002);
        var reactor = new RgbaColor(120, 200, 255, (byte)(90 + 70 * throb));
        _renderer.DrawCircle(rx, ry, 0.9f * scale, reactor, reactor);
        if (state.ElectricUniverse)
        {
            var conduit = new RgbaColor(255, 240, 120, (byte)(40 + 180 * state.Charge));
            DrawSeg(project(-19, 1), project(-20, -4), conduit, 3f);
        }
    }

    /// <summary>#870 lane 7b · #792/#793's seats — a top's ring and the chairs round it, the counter's
    /// tall stools, and the park's benches end by end. Free, taken, and who is already talking, in the
    /// two inks this deck has meant those things with since they were drawn.</summary>
    private void DrawTheSeats(
        DeckPlan plan, float scale, Func<double, double, (float X, float Y)> project)
    {
        // Round tables (plan-driven: the ship's cantina, a haven bar) — a ring on the floor, and — where the
        // plan bothered to say — the chairs round it and who is in them (#792).
        foreach (DeckPlan.TableTop top in plan.Tables)
        {
            (float cx2, float cy2) = project(top.X, top.Y);
            _renderer.DrawCircle(cx2, cy2, 0.9f * scale, null, InnerLine, 1.5f);
            DrawSeatsRound(cx2, cy2, top, scale);
        }

        // #792 · The tall seats at a counter — free and taken, in the same two inks the chairs use.
        foreach (DeckPlan.StoolSpot stool in plan.Stools)
        {
            (float sx, float sy) = project(stool.X, stool.Y);
            DrawBacklessSeat(sx, sy, stool.Taken, stool.RowHasSomebody, scale);
        }

        // #793 · …and the park's benches, END BY END, in exactly those two glyphs and no third one. A plank
        // has no back to draw any more than a bar stool has, and a free end beside somebody is the same offer
        // a free chair at an occupied top is. THE WHOLE BENCH is the privacy predicate
        // (SeatedSpread.CanSpreadTheCase at the ParkBench rung), so the deck has to be able to say which half
        // is gone before a captain walks the length of a 278 du park to find out by pressing.
        foreach (DeckPlan.BenchSpot end in plan.BenchSeats)
        {
            (float bx, float by) = project(end.X, end.Y);
            DrawBacklessSeat(bx, by, end.Taken, end.BenchHasSomebody, scale);
        }
    }

    /// <summary>#870 lane 7b · Everybody on the deck who is not the captain — #295's Old Ones, #583's repo
    /// crew, #538's sweep team and its lamp cone, #804's guard on his round, the working crew, #424's
    /// unison pause and crew glance, #793's held figure and #832's smeared one at the far end of the eye.
    /// The last pass of the WORLD: everything after it is drawn over the dark.</summary>
    private void DrawTheFigures(
        DeckPlan plan, double simTime, double? npcHoldTime, bool crewGlance,
        float scale, Func<double, double, (float X, float Y)> project)
    {
        // Droid pirate infantry (the ship's; a haven has none — DroidCount 0).
        // #424 HULL-SHUDDER: during the unison pause the NPCs are filled at the FROZEN onset time (all their
        // simTime-driven idle jitter / patrol / pace stop together — the synchronized held breath), and their
        // heads turn up as one (facing snapped screen-up). A Reever is never a patron, so it keeps its facing.
        bool headsUp = npcHoldTime.HasValue;
        plan.FillDroids(npcHoldTime ?? simTime, _droids);
        // #424 THE UNEXPLAINED SIGNAL: pre-compute each working crew member's glance — the facing toward the
        // NEAREST other crew member — so the barkeep and the dock-hand catch each other's eye as one. Only
        // built when a signal is glancing; a Reever or a drinking patron is never crew (StaffFacing skips them).
        double?[]? glance = crewGlance ? BuildCrewGlance(plan.DroidCount) : null;
        for (int di = 0; di < plan.DroidCount; di++)
        {
            DeckPlan.Droid droid = _droids[di];
            (float dx, float dy) = project(droid.X, droid.Y);
            // #295: the Reevers read hostile — a red mark, not the crew's grey.
            bool reever = droid.Name == "Reever";
            bool collector = droid.Name == "Collector";   // #583: a repo crew on foot, amber not red
            // #538: the sweep team, by callsign. They collide and are seen on the captain's own radius, so
            // they are drawn on it too — the #473 lesson about daylight showing between a body and its
            // picture.
            //
            // #633 · THREE KINDS OF FIGURE ON ONE DECK, and each branch only knew two. The pack is red, the
            // repo crew amber, a professional cold blue: what is walking toward you matters, and two hostiles
            // that read identically on the map are one hostile with two names.
            bool sweeper = IsSweeper(droid.Name);
            // #804 · …and a FIFTH: a guard walking a round on a restricted floor of the Hive. Institutional
            // green, because they are the one figure on this deck that is not a hostile at all — they are
            // an employee, and the mark has to say so before the card does.
            bool guard = SpaceSails.Core.PatrolBeat.IsGuardName(droid.Name);
            RgbaColor mark = reever ? ReeverColor
                : collector ? CollectorColor
                : sweeper ? SweeperColor
                : guard ? GuardColor
                : DroidColor;
            // #832 · THE FAR END OF THE EYE. Owner, watching one wink out mid-stride in an open corridor:
            // "Now the guard just vanishes into thin air .. that is like huge magic trick". A figure at the
            // limit of what a person can resolve is drawn thinner and softer, and (below) wears no name —
            // the same "when unsure, draw LESS" idiom the fan's blob and the on-grid smudge already use. The
            // tier itself is Core's (PatrolBeat.SightingFor); this only spends it.
            bool smeared = droid.Smeared;
            if (smeared)
            {
                mark = mark with { A = (byte)(mark.A * SmearInk) };
            }
            // #473 · AN OLD ONE'S PICTURE IS ITS BODY. The captain is drawn at exactly DeckPlan.AvatarRadius
            // (below), but the Old Ones — who collide, catch, block and get shoved apart on that SAME radius —
            // were drawn a tenth of a deck unit smaller. Every law that reads their body therefore fired with
            // daylight still showing between the dots: a catch at CatchRadius = 1.4 left a 0.2du gap on
            // screen, a pack held at PersonalSpace looked loose rather than shoulder to shoulder, and each one
            // parked against a wall floated just off it. Owner: "check all reever collisions… the radius must
            // be used in every single one" — the drawing is one of them. Crew stay at 0.5: nothing collides
            // with a barkeep, so their mark is free to be a mark.
            // #583: a collector has a body that catches on the same radius as everyone else's, so it is
            // drawn at that radius for the same reason an Old One is — the picture IS the law. Same for a
            // sweeper (#538), and for the same reason.
            float bodyRadius = reever || collector || sweeper || guard ? (float)DeckPlan.AvatarRadius : 0.5f;
            _renderer.DrawCircle(dx, dy, bodyRadius * scale, mark, mark);
            // Heads up as one (hull-shudder pause), or the crew catch each other's eye (unexplained signal),
            // else the droid's own facing. The shudder pause wins if both somehow overlap.
            double facing = headsUp && !reever && !collector && !sweeper && !guard ? Math.PI / 2
                : glance?[di] ?? droid.FacingRad;
            float fx = dx + (float)Math.Cos(facing) * scale * 0.8f;
            float fy = dy - (float)Math.Sin(facing) * scale * 0.8f;
            DrawSeg((dx, dy), (fx, fy), mark, 1.5f);

            // #793 · …AND WHETHER THEY STOPPED WHEN YOU DID. Owner, on the whole point of a park bench:
            // "it is a good gumshoe move to see if anyone is following us by foot, as they would need to
            // stop moving also." A tail that has to hold is drawn holding — a bar struck across their back,
            // in #792's own warm SEATED ink, because that is the ink this deck already uses for a figure who
            // has settled. Handed down on the droid (DeckPlan.Droid.Held); this pen works nothing out.
            //
            // NOTHING SHIPPED SETS IT: no mover in the game today is a tail (a patrol walks a round that was
            // laid before the captain arrived). So this branch is the SEAM, and its drawing is guarded with
            // a test-only held figure rather than with an NPC nobody designed.
            if (droid.Held)
            {
                float bx = dx - ((float)Math.Cos(facing) * scale * HeldBarDu);
                float by = dy + ((float)Math.Sin(facing) * scale * HeldBarDu);
                float px = -(float)Math.Sin(facing) * scale * SeatChairDu;
                float py = -(float)Math.Cos(facing) * scale * SeatChairDu;
                DrawSeg((bx - px, by - py), (bx + px, by + py), SeatTaken, 2.4f);
            }

            // #538 · THE LAMP, DRAWN AT EXACTLY THE ANGLE THE RULE CHECKS. InspectionTeam.LampConeHalfAngleDegrees
            // and LampRange are read straight from Core here rather than eyeballed, because a cone drawn wider than
            // it is tested is a lie the player learns the expensive way — and this cone IS the counter-play, so it
            // has to be trustworthy enough to stand three metres to the side of.
            if (sweeper)
            {
                double half = SpaceSails.Core.InspectionTeam.LampConeHalfAngleDegrees * Math.PI / 180.0;
                double range = SpaceSails.Core.InspectionTeam.LampRange;
                RgbaColor lamp = SweeperColor with { A = 44 };
                for (int e = -1; e <= 1; e += 2)
                {
                    double edge = facing + (e * half);
                    // AND STOPPED AT THE FIRST BULKHEAD, because the RULE stops there. First pass drew both
                    // edges to full reach through steel — cone tested right, cone drawn wrong, which is the
                    // same lie as drawing it too wide and just as expensive to learn from: a captain would
                    // have read light spilling into a compartment nobody could actually see into.
                    double lit = plan.CastRay(droid.X, droid.Y, Math.Cos(edge), Math.Sin(edge),
                                              out double hit, out _, out _, out _)
                        ? Math.Min(range, hit)
                        : range;
                    float reach = (float)lit * scale;
                    DrawSeg((dx, dy),
                            (dx + (float)Math.Cos(edge) * reach, dy - (float)Math.Sin(edge) * reach),
                            lamp, 1f);
                }
            }

            // #832 · …and the DISTANT FIGURE wears no name. That is the whole of the tier: a silhouette
            // without a plate or a round number on it, because a captain who can read "PATROL 2" off a
            // figure has resolved it, and out here they have not. Writing the label anyway would be the
            // picture claiming a certainty the sim just said it did not have.
            if (!smeared)
            {
                _renderer.DrawText(dx, dy - 0.9f * scale, droid.Name,
                    reever ? ReeverColor
                        : collector ? CollectorColor
                        : sweeper ? SweeperColor
                        : guard ? GuardColor
                        : TextDim,
                    "8px monospace", TextAlign.Center);
            }
        }
    }
    // #424 THE UNEXPLAINED SIGNAL · the crew glance. From the freshly-filled _droids, work out each WORKING
    // crew member's facing toward the nearest OTHER crew member — so the barkeep and the dock-hand (and, on
    // the bare ship, the ship's own droids) catch each other's eye as one. A drinking patron (a seated
    // regular, the Magpie) and a Reever are never crew, so their entry stays null (they keep their own
    // facing, oblivious to the buzzer). Returns a per-droid facing override, or null where there's no glance.
    private double?[] BuildCrewGlance(int count)
    {
        var facing = new double?[count];
        // The crew indices + their world positions this frame.
        Span<int> crew = stackalloc int[count];
        int n = 0;
        for (int i = 0; i < count; i++)
        {
            if (IsCrew(_droids[i].Name))
            {
                crew[n++] = i;
            }
        }
        if (n < 2)
        {
            return facing; // a lone crew member has no one to catch eyes with — no glance
        }
        for (int a = 0; a < n; a++)
        {
            DeckPlan.Droid da = _droids[crew[a]];
            double bestSq = double.MaxValue;
            int nearest = -1;
            for (int b = 0; b < n; b++)
            {
                if (b == a)
                {
                    continue;
                }
                DeckPlan.Droid db = _droids[crew[b]];
                double d = (db.X - da.X) * (db.X - da.X) + (db.Y - da.Y) * (db.Y - da.Y);
                if (d < bestSq)
                {
                    (bestSq, nearest) = (d, crew[b]);
                }
            }
            DeckPlan.Droid dn = _droids[nearest];
            facing[crew[a]] = Math.Atan2(dn.Y - da.Y, dn.X - da.X); // world radians toward the caught eye
        }
        return facing;
    }

    // A WORKING crew member (the people who work the deck): the barkeep, the customs officer, the ship's own
    // droids — anyone who is neither a Reever nor a drinking PATRON (a seated bar regular, or the Magpie).
    private static bool IsCrew(string name) =>
        name is not ("Reever" or "Collector") && !IsSweeper(name)
        // #804 · Nor a guard on a round. Nobody on a security rota is going to catch a barkeep's eye during
        // a hull shudder, and the crew's grey would hide the one figure the Hive's floors have.
        && !SpaceSails.Core.PatrolBeat.IsGuardName(name) && !IsPatron(name);

    /// <summary>#538 · A sweeper, by callsign. Never crew: nobody on that team is going to catch a barkeep's eye
    /// during a hull shudder, and giving them the crew's grey would hide the second hostile thing on the deck.</summary>
    private static bool IsSweeper(string name) => name.StartsWith("SWEEP-", StringComparison.Ordinal);

    // The drinking patrons — the regulars' short names (HavenInterior.ShortNameFor) + the roaming Magpie +
    // the station Oracle (a ranting-drunk bar fixture, #425, not working staff) + the empty-chair fallback.
    // They never react to the off-deck buzzer; only the staff do.
    private static bool IsPatron(string name) => name switch
    {
        "Silas" or "Coil" or "Gilt-Eye" or "The Fixer" or "Regular" or "Magpie" or "Oracle" => true,
        _ => false,
    };
}

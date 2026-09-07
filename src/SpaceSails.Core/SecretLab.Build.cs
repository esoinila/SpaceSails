using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// THE LAB REGION A FORCED DOOR APPENDS — a distinct inner scheme (benches, stasis pods, a server spine),
/// clamped inside the field's safe span so the edge lanes stay open, and hand-verified to leave the
/// door→console lane walkable. Pure and deterministic in (body id, door position).
///
/// <para>Split out of <c>SecretLab.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class SecretLab
{
    // ── The lab region a forced door appends. Distinct inner scheme (benches / stasis pods / server spine),
    //    clamped inside the field's safe span so the edge lanes stay open, and hand-verified to leave the
    //    door→console lane walkable (a test pins that no wall crowds a console). ──
    /// <summary>Build the lab chamber the hidden door at (<paramref name="doorX"/>, <paramref name="doorY"/>)
    /// appends, laid inside <paramref name="field"/>. Pure and deterministic in (body id, door position).</summary>
    public static Region Build(string bodyId, in SurfaceLayout.Field field, double doorX, double doorY)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // Which way the placement RESERVED for. Read from the same seed rather than re-derived from the door's
        // position: a chamber that grew the other way from the one the margin was reserved on would run out
        // through the field edge, and it would do it silently.
        double dir = Frac(bodyId, "door-side") < 0.5 ? 1.0 : -1.0;
        double cx = doorX, cy = doorY;
        double half = RoomWidth / 2.0;
        double farCx = cx + (dir * RoomDepth);

        double nearHiY = cy + half, nearLoY = cy - half;
        var walls = new List<SurfaceLayout.Wall>();

        // Two side walls (hull — solid, opaque cover), running door → far along the extend axis.
        walls.Add(new(cx, nearHiY, farCx, nearHiY, true));
        walls.Add(new(cx, nearLoY, farCx, nearLoY, true));
        // The near face, split into two stubs leaving the doorway gap at the door centre.
        walls.Add(new(cx, nearHiY, cx, cy + DoorwayHalf, true));
        walls.Add(new(cx, nearLoY, cx, cy - DoorwayHalf, true));
        // The far face is NOT solid any more — it has a doorway, and the mountain keeps going. Owner: "a
        // secret lab that extends into a mountain". One chamber was a vault; three chambers with doors between
        // them is a place you go INTO, which is what makes a locked door behind you mean anything.
        walls.Add(new(farCx, nearHiY, farCx, cy + DoorwayHalf, true));
        walls.Add(new(farCx, nearLoY, farCx, cy - DoorwayHalf, true));

        // ── The inner scheme, distinct from henge/wreck/tunnel: a SERVER SPINE + LAB BENCHES + STASIS PODS,
        //    all tucked to the sides so the central door→console lane (around cy) stays clear. ──
        double d3 = cx + (dir * 3.0), d6 = cx + (dir * 6.0), d10 = cx + (dir * 10.0), d13 = cx + (dir * 13.0);
        // The server spine: a long low wall run high of centre (the racks), broken by one maintenance gap.
        double spineY = cy + (half * 0.55);
        walls.Add(new(d3, spineY, d6, spineY, true));
        walls.Add(new(cx + (dir * 8.0), spineY, d13, spineY, true)); // gap at d6..d8 (walk between racks)
        // Lab benches: two short perpendicular stubs off the LOW wall (the work counters).
        double benchY = cy - half;
        walls.Add(new(d3, benchY, d3, benchY + 2.5, false));
        walls.Add(new(cx + (dir * 7.0), benchY, cx + (dir * 7.0), benchY + 2.5, false));
        // Stasis pods: two tiny solid boxes tucked into the far corners (the sleepers).
        AddBox(walls, System.Math.Min(d13, farCx - 1.0), nearHiY - 2.0, farCx - 0.5, nearHiY - 0.5, true);
        AddBox(walls, System.Math.Min(d13, farCx - 1.0), nearLoY + 0.5, farCx - 0.5, nearLoY + 2.0, true);

        // ── The interactables, down the OPEN central lane (around cy), never inside a wall. ──
        double laneY = cy - (half * 0.15);
        var consoles = new List<LabConsole>
        {
            // A log at the threshold — the first fragment, no reveal (it draws you in).
            new(LabConsoleKind.LoreLog, "lab-log-1", d3, laneY, "🖥 VANTAR — FIELD LOG", 0, false),
            // The brain-in-a-jar backup rig, mid-room — reads the DO NOT REVIVE log.
            new(LabConsoleKind.BrainJar, "lab-brainjar", d6, cy + (half * 0.15), "🧠 BACKUP RIG · DO NOT REVIVE", 2, false),
            // The dormant synthetic on its bench — the bounded risk, off to the low side (reads log 3).
            new(LabConsoleKind.DormantSynth, "lab-synth", d10, cy - (half * 0.35), "🦿 DORMANT SYNTHETIC", 3, false),
            // The fat discovery cache at the heart.
            new(LabConsoleKind.DiscoveryCache, "lab-cache", d10, laneY, "🗝 VANTAR'S CACHE", 0, false),
            // The CORE log at the deep end — reading it is the reveal (nerve hit + the diced outcome).
            new(LabConsoleKind.LoreLog, "lab-log-core", d13, laneY, "🖥 VANTAR — THE CORE LOG", VantarLore.CoreIndex, true),
        };

        // ── INTO THE MOUNTAIN. Two more chambers past the first, each a little narrower than the last, each
        //    behind a door. The narrowing is the fiction doing the work: this was cut into rock by people who
        //    kept going after the budget ran out.
        var doors = new List<LabDoor>
        {
            new("lab-door-1", farCx, cy, ChamberNames[1]),
        };

        double c2Near = farCx, c2Far = farCx + (dir * DeepChamberDepth);
        double c2Half = half * 0.8;
        walls.Add(new(c2Near, cy + c2Half, c2Far, cy + c2Half, true));
        walls.Add(new(c2Near, cy - c2Half, c2Far, cy - c2Half, true));
        walls.Add(new(c2Near, cy + c2Half, c2Near, cy + DoorwayHalf, true));
        walls.Add(new(c2Near, cy - c2Half, c2Near, cy - DoorwayHalf, true));
        walls.Add(new(c2Far, cy + c2Half, c2Far, cy + DoorwayHalf, true));
        walls.Add(new(c2Far, cy - c2Half, c2Far, cy - DoorwayHalf, true));
        doors.Add(new("lab-door-2", c2Far, cy, ChamberNames[2]));

        double c3Far = c2Far + (dir * DeepChamberDepth);
        double c3Half = half * 0.62;
        walls.Add(new(c2Far, cy + c3Half, c3Far, cy + c3Half, true));
        walls.Add(new(c2Far, cy - c3Half, c3Far, cy - c3Half, true));

        // #822 · …and the heart's back wall is TWO STUBS AND A GAP now, with the gap standing plugged. See
        // HiddenWay: the deepest room in the mountain had one door and a lockdown board that could throw it,
        // which is the only genuinely sealed box in the game. The crawl is cut to the same DoorwayHalf every
        // other opening in here is cut to, so a captain never has to judge one by eye.
        walls.Add(new(c3Far, cy + c3Half, c3Far, cy + DoorwayHalf, true));
        walls.Add(new(c3Far, cy - c3Half, c3Far, cy - DoorwayHalf, true));
        var hidden = new List<HiddenWay>
        {
            new("lab-crawl", ChamberNames[2], c3Far, cy,
                new SurfaceLayout.Wall(c3Far, cy - DoorwayHalf, c3Far, cy + DoorwayHalf, true),
                "The rock at the back of the heart rings HOLLOW - and it is dressed rock, not a face. "
                + "Somebody cut a way out of here and then made it look like the end of the world."),
        };

        // The clean room carries the two control panels — the owner's own asks, and they belong TOGETHER in the
        // middle: "Surely some control panels based on the vent panel can be added 🤠" and "Alarm system panel
        // maybe … something to try to hack." A captain who reaches the middle can throw every door in the
        // mountain from one wall, and can argue with the thing that is counting.
        double c2Mid = c2Near + (dir * (DeepChamberDepth / 2.0));
        consoles.Add(new(LabConsoleKind.DoorBoard, "lab-doorboard", c2Mid, cy + (c2Half * 0.45),
                         "🎛 DOOR BOARD — VANTAR LABS", 0, false));
        consoles.Add(new(LabConsoleKind.AlarmPanel, "lab-alarm", c2Mid, cy - (c2Half * 0.45),
                         LabSecurity.PanelTitle, 0, false));

        // …and the heart carries the card, which is the only thing that opens a lockdown. It is in the DEEPEST
        // room on purpose: a captain who ran at the first alarm never had it, and now needs it.
        double c3Mid = c2Far + (dir * (DeepChamberDepth / 2.0));
        consoles.Add(new(LabConsoleKind.KeyCard, "lab-keycard", c3Mid, cy,
                         "🗝 VANTAR'S CARD", 0, false));

        double heartX = cx + (dir * (RoomDepth / 2.0)), heartY = cy;
        var marks = new List<SurfaceLayout.Landmark> { new(heartX, heartY, "⧉ VANTAR'S LAB") };

        double minX = System.Math.Min(cx, c3Far), maxX = System.Math.Max(cx, c3Far);
        return new Region("VANTAR'S SECRET LAB", walls, marks, consoles, DiscoveryCacheCredits,
            minX, nearLoY, maxX, nearHiY, heartX, heartY, doors, hidden);
    }
}

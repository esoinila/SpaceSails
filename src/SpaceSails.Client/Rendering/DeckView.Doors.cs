using System;
using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: the doors of one frame (part of DeckView).
//
// #563 slice 2 · A PURE MOVE out of DeckView.Frame.cs. Not a refactor: every line below is the line that
// was there, in the order it was in. The reason is the 1,500-line file law (NoSourceFileIsTooLongTests),
// which wants daylight between the line and the largest file under it — Frame.cs was the largest file in
// the tree and the treadmill's off-glass reject pushed it to within twenty-one lines of the bound.
//
// DrawTheDoors is the cleanest region to lift: one pass of the pen, one subject, and it reads nothing of
// the frame's state but the plan, the captain and the projection it is handed.
public sealed partial class DeckView
{
    /// <summary>#870 lane 7b · The doors: #462's airlock interlock (only the leaf nearest the captain may
    /// stand open), #585's locked hatch drawn heaviest of all, #592's imported ink and #606's machined
    /// frame. Drawn after the walls because a door is set INTO one.</summary>
    private void DrawTheDoors(
        DeckPlan plan, in State state, Func<double, double, (float X, float Y)> project)
    {
        // Automatic airlock doors (the docking tube): shut across the passage until you near them,
        // then they retract to a stub at each jamb. Purely visual — the passage is always walkable.
        foreach (DeckPlan.Door d in plan.Doors)
        {
            // #563 slice 2 · OFF THE GLASS IS NOT DRAWN — the same conservative reject slice 1 gave the
            // ground, walls and unseen falloff, and for the same reason: the regolith is a lattice and the
            // frame carries nine tiles, every one of which now hangs doors in its buildings. Eight of those
            // tiles are mostly several hundred deck units off the side of the screen. A door is one segment
            // and cannot cross the view when both its ends are past the same edge, so nothing visible is
            // skipped and no pixel changes.
            (float dsx1, float dsy1) = project(d.X1, d.Y1);
            (float dsx2, float dsy2) = project(d.X2, d.Y2);
            if (OffTheGlass(dsx1, dsy1, dsx2, dsy2))
            {
                continue;
            }

            if (d.Locked)
            {
                // Another berth's sealed hatch — always shut, drawn cold (steel-blue), a real wall behind.
                //
                // #585 · Owner, in the Hive: "the doors should be different color than the walls and say
                // locked on approach." The cold steel-blue already differs from every wall ink in the game;
                // what it lacked was WEIGHT — at 3.5px against hull-bright walls it read as just another
                // line. A door that will never open is the most informative object in a facility, so it is
                // drawn heaviest of all, with a second inner stroke so it looks barred rather than merely
                // shut. (The "say locked on approach" half is the console at its midpoint, which names what
                // is behind it as you come near.)
                DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), DoorLocked, 5.5f);
                DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), new RgbaColor(20, 26, 38, 220), 2.0f);
                continue;
            }
            double mx = (d.X1 + d.X2) / 2.0, my = (d.Y1 + d.Y2) / 2.0;
            double toDoor = Math.Sqrt((state.AvatarX - mx) * (state.AvatarX - mx)
                                    + (state.AvatarY - my) * (state.AvatarY - my));
            // #462 · THE AIRLOCK INTERLOCK. Owner, 2026-07-27: "only one door in a tube is open at a time…
            // think of airlock" — "both doors being open at the same time defeats the purpose". Doors in the
            // same group take turns: only the one NEAREST the captain may stand open, so the far end is
            // always drawn shut. That is the visible barrier the Old Ones stop at (they used to halt at a gap
            // painted open, because the captain standing at the threshold held BOTH ends retracted), and it
            // is what seals a tailgater in the tube with the built-in gun (#461) instead of letting it
            // follow you aboard. The rule itself lives in Core Airlock so CI pins it.
            double nearestPartner = double.PositiveInfinity;
            if (d.Interlock != 0)
            {
                foreach (DeckPlan.Door other in plan.Doors)
                {
                    if (other.Interlock != d.Interlock || other.Locked || other.Equals(d))
                    {
                        continue;
                    }
                    double pmx = (other.X1 + other.X2) / 2.0, pmy = (other.Y1 + other.Y2) / 2.0;
                    double toOther = Math.Sqrt((state.AvatarX - pmx) * (state.AvatarX - pmx)
                                             + (state.AvatarY - pmy) * (state.AvatarY - pmy));
                    nearestPartner = Math.Min(nearestPartner, toOther);
                }
            }
            // #592 · A door is made of the hill it is set in — unless somebody paid to ship it here. The
            // ship and the stations keep the old amber (StoneInk is null there): they ARE steel, and nothing
            // about a bulkhead should start depending on which moon is outside.
            RgbaColor shut = DoorShut, leaf = DoorOpen;
            if (plan.DoorInk is { } local)
            {
                SpaceSails.Core.BodyPalette.Ink di = d.Imported
                    ? SpaceSails.Core.BodyPalette.Imported
                    : local;
                shut = new RgbaColor(di.R, di.G, di.B, 230);
                leaf = new RgbaColor(di.R, di.G, di.B, 95);
            }

            // #606 · A MACHINED DOOR IS A DIFFERENT OBJECT, not a different colour. Owner, hiding the lift
            // head in an ordinary hut: "The expensive doors would be the clue."
            //
            // Colour had already been asked to carry this and could not (#585) — violet means shelter, means
            // one ruin hatch in seven, and means the way down, so it identified nothing. Weight is a second
            // channel: a fat leaf with an inner rail and its frame picked out at the jambs, against the single
            // thin stroke every hatch on the moon is drawn with. That reads at a glance, from close, without a
            // word of copy — which is the whole technique (docs/art-manifest-hive.md).
            //
            // It still retracts. SEALED is what it looks like, not what it does: a door here that refused to
            // open would strand a captain in a lift head, and the reachability audits would be right to say so.
            float weight = d.Machined ? 6f : 3.5f;
            bool open = Airlock.MayOpen(toDoor, nearestPartner, DoorOpenRadius);
            if (open)
            {
                // Retracted: a short leaf at each jamb (25% in from each end).
                DrawSeg(project(d.X1, d.Y1), project(d.X1 + (d.X2 - d.X1) * 0.25f, d.Y1 + (d.Y2 - d.Y1) * 0.25f), leaf, weight - 1f);
                DrawSeg(project(d.X2, d.Y2), project(d.X2 - (d.X2 - d.X1) * 0.25f, d.Y2 - (d.Y2 - d.Y1) * 0.25f), leaf, weight - 1f);
            }
            else
            {
                DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), shut, weight);
                if (d.Machined)
                {
                    DrawSeg(project(d.X1, d.Y1), project(d.X2, d.Y2), new RgbaColor(18, 20, 30, 210), 2f);
                }
            }
            if (d.Machined)
            {
                // The frame: a short stub across the opening at each jamb, the way a plan draws a door that
                // was set into a hole somebody cut rather than built around.
                float jx = d.X2 - d.X1, jy = d.Y2 - d.Y1;
                float jl = MathF.Sqrt((jx * jx) + (jy * jy));
                if (jl > 0.01f)
                {
                    float nx = -jy / jl * 0.9f, ny = jx / jl * 0.9f;
                    DrawSeg(project(d.X1 - nx, d.Y1 - ny), project(d.X1 + nx, d.Y1 + ny), shut, 2.5f);
                    DrawSeg(project(d.X2 - nx, d.Y2 - ny), project(d.X2 + nx, d.Y2 + ny), shut, 2.5f);
                }
            }
        }
    }
}

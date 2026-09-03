using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: what the ONE LIST says about a barrier (part of DeckPlan).
//
// #442 · A BARRIER IS ONE OBJECT. ITS VISIBILITY IS A STYLE, NEVER AN EXISTENCE.
//
// Owner, live 2026-07-26: "There should be a refactor to make sure the visible and physics wall ALWAYS
// are 1 to 1 the same. Now they seem to be very hacky."
//
// The DATA has been 1:1 for a long time — the constructor derives CollisionSegments from Walls and
// AppendRegion grows both together, so nothing can be solid that is not in the list. What kept drifting
// was the LOOK, because two systems drew barriers from records that had to be kept in step by hand:
//
//   · the fog, which dropped a wall from the draw while collision kept it (fixed in DeckView.Frame);
//   · and a DOOR, which is a second record laid over the first. The issue names it by the plan's own
//     comment — a locked leaf is "decoration only, and is backed by a real wall so you can't pass".
//
// This file is where the second one stops being hand-sync. A door's look is now DERIVED from the wall
// list rather than declared beside it: ask the stone whether this doorway is sealed, and draw the leaf
// accordingly. One object, two faces, and nobody to keep in step.
public sealed partial class DeckPlan
{
    /// <summary>How near a wall has to pass a doorway's middle before the doorway counts as walled up. A
    /// hair, deliberately: a SEALED hatch has a wall lying exactly along it (distance 0), and a CARVED
    /// doorway has stubs whose inner ends touch the leaf's ends and nothing at all across its middle —
    /// half a door's width away at the very least. There is no third case in the game, so the threshold
    /// sits where nothing real is near it, which is the opposite of the 34-versus-34.2 mistake.</summary>
    private const double DoorwaySealProbe = 0.05;

    /// <summary>
    /// #442 · <b>IS THIS DOORWAY WALLED UP — ASKED OF THE ONE LIST?</b>
    ///
    /// <para>Three kinds of barrier in this game are a wall PLUS a door drawn over it, and all three are
    /// honest about the physics and were lying about the picture:</para>
    ///
    /// <list type="bullet">
    /// <item><b>The shuttle hatch.</b> While she is docked her own hatch is sealed — the plan lays a wall
    /// straight along it ("the hatch itself — sealed here") and drops that wall again the moment the boat
    /// is away. The Door record beside it is unlocked, so the pen retracted the leaf as the captain walked
    /// up and the boot was refused by the stone behind an opening the player had just watched open.</item>
    /// <item><b>A dogged compartment hatch.</b> <c>ShipWith</c> builds a wall across a shut room's doorway,
    /// for the reason its own summary gives: <i>"a dogged hatch is a WALL, and the walls are what
    /// everything else asks."</i> Everything except the pen, which went on drawing the leaf as an ordinary
    /// automatic door.</item>
    /// <item><b>A haven's sealed berth hatch,</b> the same construct one deck ashore.</item>
    /// </list>
    ///
    /// <para>So the leaf asks the stone. A doorway with a wall across its middle is drawn SHUT — not cold
    /// and locked, because it is not locked and a captain must not be told it needs a card; simply closed,
    /// which is what it is. Nothing about the physics moves: this is a question about the picture, answered
    /// out of the list the physics already reads, which is the whole of what #442 asked for.</para>
    ///
    /// <para>O(1) per door: the probe goes through <see cref="CollisionField"/>, the #448 grid, so a floor
    /// with eight hundred segments measures the handful in the doorway's own cell and no more.</para>
    /// </summary>
    public bool DoorwayIsWalledUp(in Door d) =>
        SurfaceCollision.Blocked(
            (d.X1 + d.X2) / 2.0, (d.Y1 + d.Y2) / 2.0, DoorwaySealProbe, CollisionField);
}

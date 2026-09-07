using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #606 · THE LIFT HEAD IS AN ORDINARY HUT. Owner, twice, while playing: <i>"I think the lift could also
/// be a little more hidden on the surface… it could be in an ordinary hut, with 2 doors"</i>, and then,
/// after another look at the ground, <i>"the elevator still stands out on surface like a sore thumb"</i>.
///
/// <para>The second sentence is the one that mattered, because the first fix was colour and colour was
/// never the problem: the head was a box of five thin lines while every other building on the moon is
/// piled regolith — hatched mass, real thickness, a seeded angle. It was not a camouflaged lift head, it
/// was the only building on the ground drawn in a different hand.</para>
///
/// <para>Split out of <c>SecretLab.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public static partial class SecretLab
{
    /// <summary>#606 · THE LIFT HEAD IS AN ORDINARY HUT. Owner, twice, while playing:
    /// <i>"I think the lift could also be a little more hidden on the surface, since up there there are no
    /// guards... it could be in an ordinary hut, with 2 doors .. we have those. The expensive doors would be
    /// the clue"</i> — and then, after another look at the ground, <i>"the elevator still stands out on
    /// surface like a sore thumb"</i>.
    ///
    /// <para>The second sentence is the one that matters, because the first fix was colour and colour was
    /// never the problem. The head was a 10 x 8 box of five thin lines while every other building on the moon
    /// is <see cref="SurfaceStructure"/>'s piled regolith — hatched mass, real thickness, a seeded angle. It
    /// was not a camouflaged lift head, it was the only building on the ground drawn in a different hand. A
    /// captain does not have to know what a lift head looks like to pick that out; they only have to be able
    /// to see.</para>
    ///
    /// <para>So it is built by the same function as its neighbours, at a size drawn from the same range, and
    /// what is left to notice is what the owner asked to be the clue: the DOORS were flown here. Every hatch
    /// on a landing site is swaged out of the hill it is set in; two machined pressure doors on a survey shack
    /// are a receipt, and a receipt is the only thing this facility has ever been careless with (#601).</para>
    ///
    /// <para><b>Rectangular, always.</b> The one property that is not seeded, and it earns the exception: a
    /// lift car is a box, and a rotated box is the shape everything downstream — the car's return spot, the
    /// keep-out, the audit — can answer <i>"is the captain inside this"</i> about without inventing a second
    /// geometry to be wrong in. A third of the huts on any site are rectangles, so it hides in plain sight.</para></summary>
    public static SurfaceStructure.Spec HeadHut(string bodyId, string? siteSalt, in SurfaceLayout.Field field)
    {
        (double hx, double hy) = HeadSpot(bodyId, siteSalt, field);
        return HeadHutAt(bodyId, siteSalt, hx, hy);
    }

    /// <summary>The hut's SHAPE, which is pure of where it ends up standing — so <see cref="HeadSpot"/> may
    /// ask how much room it needs without asking itself where it is.</summary>
    private static SurfaceStructure.Spec HeadHutAt(string bodyId, string? siteSalt, double x, double y)
    {
        string salt = siteSalt ?? "";
        return SurfaceStructure.Ordinary(
            x, y,
            size: 10.0 + (4.0 * Frac(bodyId, $"head-size:{salt}")),
            thickFrac: Frac(bodyId, $"head-thick:{salt}"),
            angleFrac: Frac(bodyId, $"head-angle:{salt}"),
            // Two, because he asked for two and because a facility that put a car in a shack would want a way
            // out of it that is not the way in.
            doors: 2,
            shapeFace: (int)SurfaceStructure.Footprint.Rectangular);
    }

    /// <summary>#585 · WHERE THE LIFT HEAD ACTUALLY STANDS, once everything else on this site has had its say.
    ///
    /// <para>Owner, looking at a screen with the maintenance shed buried inside an outpost hut which was
    /// itself overlapping a shelter drum: <i>"is it this that I cannot get into?"</i> He could not, and it was
    /// not his fault.</para>
    ///
    /// <para>The cause is the oldest one in this file's neighbourhood, with a new twist: <b>the lab is seeded
    /// PER BODY and everything it collides with is seeded PER SITE.</b> <see cref="For"/> cannot see the
    /// shelters or the hut, they cannot see it, and the shared claim ledger only ever protected the CHAMBER
    /// (which is offset from the door) and never the shed standing on the door itself. Three placers, three
    /// answers, one patch of ground.</para>
    ///
    /// <para>So the entrance is resolved HERE, against this site's real furniture, and everything downstream —
    /// the shed, the tracker beacon, the hidden-door console, the chamber's own reservation — reads this one
    /// function. Seeded nudges, so it is still the same spot every visit.</para></summary>
    public static (double X, double Y) HeadSpot(string bodyId, string? siteSalt, in SurfaceLayout.Field field)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        string salt = siteSalt ?? "";

        Placement seeded = For(bodyId, field, forcePresent: true);
        double x = seeded.DoorX, y = seeded.DoorY;

        // Everything on this site that a shed must not be inside. The hut is built into an edge lane, so it
        // is the likeliest collision by a distance.
        //
        // #606 · The clearance is the head's OWN reach plus a berth, not a flat 12 du. That constant was
        // written when the head was a 10 x 8 box whose half-diagonal was 6.4, so it happened to hold; the
        // moment the head became a full-sized hut it would have been a number that no longer described
        // anything, quietly letting a pressure drum and a lift share a wall. Two footprints do not overlap
        // when the gap between their centres beats the sum of their reaches — that sentence, and no constant.
        double reach = SurfaceStructure.EnvelopeOf(HeadHutAt(bodyId, salt, 0, 0)).Reach;
        var taken = new List<(double X, double Y, double R)>();
        foreach (SurfaceStructure.Spec shelter in SurfaceShelter.SpecsFor(bodyId, salt, field))
        {
            taken.Add((shelter.CentreX, shelter.CentreY,
                SurfaceStructure.KeepOutRadius(shelter) + reach + HeadBerth));
        }

        // #563 · AND NOT THE HUT ANY MORE — the precedence between these two is REVERSED, deliberately.
        //
        // The hut used to be pinned to the far edge lane, which no ordinary generator ever touched, so it was
        // the fixed thing and the lift head moved around it. An unbounded ground has no edge lane, so the hut
        // now places itself against this site's real furniture the same way this does — and the two asking
        // each other "where are you?" is a cycle that recurses until the stack gives out (it did, in one run
        // of the suite). Somebody has to go first.
        //
        // The lab goes first, and it should: a lift head is the mouth of a whole facility and cannot be
        // anywhere else, while a hut is one shed and the owner's own rule for this ledger is that the thing
        // which costs nothing to move is the thing that moves. SurfaceOutpost.ForTile reads
        // SurfaceLayout.StandingClaims, which carries the chamber this head reserves, so the invariant that
        // used to be enforced from here is enforced from there — same law, one direction.

        // #649 · And the monolith, on the one ground that carries one. The lift head is seeded down the deep
        // field and the deep field is where the stone is; without this the camouflaged shed could be seeded
        // inside 54 du of solid rock, which is a captain riding a lift up into somewhere they cannot stand —
        // the #602 report, wearing a landmark. Asked of the object, so its size and this clearance cannot
        // drift apart.
        if (Monolith.KeepOutOn(bodyId, salt, field) is { } slab)
        {
            taken.Add((slab.X, slab.Y, slab.R + reach));
        }

        // A handful of seeded retries along the deep field, then give up and take the last one rather than
        // loop: a shed slightly close to a hut is a cosmetic problem, and no shed at all is a dead feature.
        for (int attempt = 0; attempt < 24 && Clashes(x, y, taken); attempt++)
        {
            double loX = field.LeftX + SurfaceLayout.EdgeMargin + RoomDepth;
            double hiX = field.RightX - SurfaceLayout.EdgeMargin - RoomDepth;
            double loY = field.BottomY + (RoomWidth / 2.0) + 2.0;
            double hiY = field.AnchorY + 12.0;

            x = Lerp(loX, hiX, Frac(bodyId, $"head-x:{salt}:{attempt}"));
            y = Lerp(loY, hiY, Frac(bodyId, $"head-y:{salt}:{attempt}"));
        }

        return (x, y);
    }

    /// <summary>The bare air the head wants between its own wall and a neighbour's, on top of both
    /// footprints. Enough that the two never read as one complex; small, because every du of it is ground
    /// claimed away from the ordinary buildings (#587).</summary>
    private const double HeadBerth = 4.0;

    private static bool Clashes(double x, double y, List<(double X, double Y, double R)> taken)
    {
        foreach ((double tx, double ty, double r) in taken)
        {
            double dx = x - tx, dy = y - ty;
            if (Math.Sqrt((dx * dx) + (dy * dy)) < r)
            {
                return true;
            }
        }
        return false;
    }
}

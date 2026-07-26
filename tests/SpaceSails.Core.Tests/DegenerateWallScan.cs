namespace SpaceSails.Core.Tests;

/// <summary>
/// #442 · No ground may seed a barrier the eye cannot find. The owner has hit an "invisible wall" in play
/// more than once, and one mechanism for it is generative rather than fog: a wall segment whose two ends
/// collapse onto the same point draws NOTHING (a line of zero length paints no pixels) while
/// <see cref="SurfaceCollision.DistanceToSegment"/> still treats it as a point of stone — solid, and
/// invisible, from the same list. <see cref="BarrierInvariantTests"/> pins that the split EXISTS in the
/// primitives; this pins that the game's own level generation never actually produces one.
///
/// <para>Swept across every landable body × every seeded landing site (#320), because a body is no longer
/// one ground: each site re-seeds its own layout off its own salt, so the guarantee has to hold for all of
/// them, not for the canon site alone.</para>
/// </summary>
public class DegenerateWallScan
{
    // Roughly the live surface field (MoonSurface's bounds) — the same shape the client hands the layout.
    private static readonly SurfaceLayout.Field Field = new(
        LeftX: -44, RightX: 34, TopY: -20, BottomY: -96, LandingBandY: -27, AnchorX: -6, AnchorY: -21.5);

    private static readonly string[] Bodies =
        ["miranda", "luna", "europa", "ganymede", "callisto", "titan", "enceladus", "triton", "phobos", "the-clinker"];

    [Fact]
    public void NoSeededGround_EverPlacesAWallTheEyeCannotSee()
    {
        int walls = 0;
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SurfaceLayout.Plan plan = SurfaceLayout.For(body, Field, site.LayoutSalt);
                foreach (SurfaceLayout.Wall w in plan.Walls)
                {
                    walls++;
                    double dx = w.X2 - w.X1, dy = w.Y2 - w.Y1;
                    double length = System.Math.Sqrt((dx * dx) + (dy * dy));

                    // A zero-length wall is the pure form of the bug: stone to a body, nothing to the eye.
                    Assert.True(length > 1e-9,
                        $"{body} · site '{site.Name}' (salt '{site.LayoutSalt}') seeds a ZERO-LENGTH wall at "
                        + $"({w.X1:0.###}, {w.Y1:0.###}) — solid to the boot, invisible on screen.");

                    // Shorter than a body is the practical form: a captain (radius 0.7, so 1.4 across) can be
                    // stopped by a stub too small to read as a barrier. Not currently produced by any ground,
                    // and this keeps it that way.
                    Assert.True(length >= 1.4,
                        $"{body} · site '{site.Name}' seeds a wall {length:0.###} du long — shorter than the "
                        + "body it blocks, so it will read as an invisible wall.");
                }
            }
        }

        // A canary on the sweep itself: if the generator or the site list ever goes quiet, this test would
        // pass by examining nothing at all.
        Assert.True(walls > 100, $"the sweep only saw {walls} walls — the grounds stopped generating");
    }
}

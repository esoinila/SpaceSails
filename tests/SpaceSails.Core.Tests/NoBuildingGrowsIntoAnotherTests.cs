using System.Collections.Generic;
using System.Linq;
using SpaceSails.Core;

namespace SpaceSails.Core.Tests;

/// <summary>
/// #585 · NOTHING IS BUILT ON TOP OF ANYTHING ELSE.
///
/// <para>Owner, looking at a Miranda site: <i>"check this structure out... it functions but is kind of
/// funny"</i>. Funny was generous. A shelter drum, an outlying hut and a maze fixture had grown together
/// into one accidental mega-complex — doorways opening onto another building's solid mass, walls crossing
/// through rooms, and a pressure vessel sharing a face with a ruin.</para>
///
/// <para>Three causes, all the same shape as every expensive bug in this project: <b>separate sources of
/// truth for one fact.</b> The seeded features kept a claim ledger. The outlying buildings kept a different,
/// weaker one. <c>SurfaceShelter</c> kept none at all. And the one real check tested a bare 30 du between
/// CENTRES, which is only honest for a circle — a freely-rotated 20 x 16 box with 3 du walls sweeps a radius
/// near 16, so two buildings 30 du apart overlap by design.</para>
///
/// <para>This walks the real generated plans and requires the ground to be what the ledger claims.</para>
/// </summary>
public sealed class NoBuildingGrowsIntoAnotherTests
{
    // #585 · EVERY LANDABLE BODY IN THE SCENARIO. The first version of this list held eight and the
    // scenario holds TEN — enceladus and the-clinker were simply forgotten, so four grounds were audited by
    // nobody while the file claimed to check "every site". A hand-kept mirror of data that lives somewhere
    // else is the exact bug this whole document is about; it is written down here rather than derived only
    // because the scenario is a wwwroot JSON the test host cannot reliably locate. If a moon is ever added,
    // it must be added here — and the spec's "not yet enforced" list says so out loud.
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
    ];

    private static SurfaceLayout.Field Field => SurfaceLayout.DefaultField;

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    [Fact]
    public void NoSHELTERSharesGroundWithABuilding()
    {
        // The one that has to hold above all others. A shelter is the answer to the air mechanic: a captain
        // walks a long way on a beacon's word to reach it. Finding it fused into somebody's ruin — or worse,
        // its door opening onto another building's mass — turns the game's safety net into a joke.
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceLayout.Plan plan = SurfaceLayout.For(body, Field, site.LayoutSalt);
            IReadOnlyList<SurfaceStructure.Spec> shelters =
                SurfaceShelter.SpecsFor(body, site.LayoutSalt, Field);

            foreach (SurfaceStructure.Spec shelter in shelters)
            {
                double shelterR = SurfaceStructure.KeepOutRadius(shelter);

                // The plan publishes what each building ACTUALLY occupies (#585), so this asks the ground
                // rather than guessing a radius — a guess here would be the same two-sources-of-truth bug
                // wearing a test's clothes, and the first version of this test failed on a building that was
                // correctly placed for exactly that reason.
                foreach ((double bx, double by, double br) in plan.BuildingFootprints ?? [])
                {
                    double gap = Distance(shelter.CentreX, shelter.CentreY, bx, by);

                    Assert.True(gap > shelterR + br,
                        $"{body}/{site.Name}: a shelter at ({shelter.CentreX:F0}, {shelter.CentreY:F0}) and a " +
                        $"building at ({bx:F0}, {by:F0}) are {gap:F1} du apart — they are the same structure.");
                }
            }
        }
    }

    [Fact]
    public void NoTwoBUILDINGSShareGround()
    {
        // The sprawl the owner was actually looking at. Two centres closer than their combined reach are one
        // building with two names, and every door on the shared face opens into solid rock.
        foreach ((string body, LandingSite site) in EverySite())
        {
            SurfaceLayout.Plan plan = SurfaceLayout.For(body, Field, site.LayoutSalt);
            var prints = (plan.BuildingFootprints ?? []).ToList();

            for (int i = 0; i < prints.Count; i++)
            {
                for (int j = i + 1; j < prints.Count; j++)
                {
                    double gap = Distance(prints[i].X, prints[i].Y, prints[j].X, prints[j].Y);

                    Assert.True(gap > prints[i].R + prints[j].R,
                        $"{body}/{site.Name}: buildings at ({prints[i].X:F0}, {prints[i].Y:F0}) and " +
                        $"({prints[j].X:F0}, {prints[j].Y:F0}) are {gap:F1} du apart, needing " +
                        $"{prints[i].R + prints[j].R:F1} — they have merged.");
                }
            }
        }
    }

    [Fact]
    public void TheKeepOutRadiusIsHonestAtEveryAngle()
    {
        // The number the whole fix rests on. A rotated box's reach is its half-diagonal plus its wall — if
        // this ever went back to a half-width, buildings would merge again at 45 degrees and nowhere else,
        // which is the kind of bug that takes an afternoon of playing to see and a second to explain.
        var spec = new SurfaceStructure.Spec(
            CentreX: 0, CentreY: 0, Width: 20, Height: 16, AngleRad: 0.7,
            Doors: 1, WallThickness: 3.0, Shape: SurfaceStructure.Footprint.Rectangular);

        double r = SurfaceStructure.KeepOutRadius(spec);

        Assert.True(r > 10 + 3.0, "a keep-out that only covers the half-WIDTH merges boxes on the diagonal.");
        Assert.Equal(System.Math.Sqrt(100 + 64) + 3.0, r, 6);
    }

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return System.Math.Sqrt((dx * dx) + (dy * dy));
    }
}

/// <summary>#585 · A REFUGE YOU CANNOT BE FOLLOWED INTO. Owner, playing: <i>"lol I saw one reever get into a
/// shelter :-D"</i>, and a minute later <i>"3 reevers waiting in the shelter :-D"</i>. Funny, and fatal to the
/// only promise the building makes — its own arrival line reads <b>"Nothing outside can work that door"</b>,
/// and it exists because he asked for <i>"rooms with doors we can hide behind while we reload our guns safe
/// from reevers"</i>.</summary>
public sealed class NothingFollowsYouInsideTests
{
    private static SurfaceStructure.Spec Drum(double angle) => new(
        CentreX: 40, CentreY: -120, Width: 26, Height: 21, AngleRad: angle,
        Doors: 1, WallThickness: 2.6, Shape: SurfaceStructure.Footprint.Rounded);

    [Fact]
    public void AnythingInsideIsPutBackOutside()
    {
        // Swept over angles because the shelter is a ROTATED drum: a fix that only works axis-aligned would
        // pass a lazy test and still let a pack in through every angled door on the moon.
        foreach (double angle in new[] { 0.0, 0.4, 1.1, 2.7, 4.9 })
        {
            SurfaceStructure.Spec spec = Drum(angle);

            for (double dx = -12; dx <= 12; dx += 1.5)
            {
                for (double dy = -9; dy <= 9; dy += 1.5)
                {
                    (double x, double y) = SurfaceShelter.HoldAtTheThreshold(
                        spec, spec.CentreX + dx, spec.CentreY + dy);

                    Assert.False(SurfaceShelter.Contains(spec, x, y),
                        $"angle {angle:F1}: a body at (+{dx}, +{dy}) is still inside after being held out.");
                }
            }
        }
    }

    [Fact]
    public void ABodyDeadCentreIsStillPutOutside()
    {
        // The degenerate case: no direction to be pushed along. It has to pick one rather than divide by
        // nothing and leave a Reever standing on the charging rack forever.
        SurfaceStructure.Spec spec = Drum(0.9);
        (double x, double y) = SurfaceShelter.HoldAtTheThreshold(spec, spec.CentreX, spec.CentreY);
        Assert.False(SurfaceShelter.Contains(spec, x, y));
    }

    [Fact]
    public void SomethingALREADYOutsideIsNotShoved()
    {
        // It is a threshold, not a repulsor. A hunter crowding the door should stay exactly where it is —
        // that pile-up outside is the good half of this scene and must not be pushed away from the glass.
        SurfaceStructure.Spec spec = Drum(0.3);
        foreach ((double px, double py) in new[] { (80.0, -120.0), (40.0, -60.0), (0.0, 0.0) })
        {
            (double x, double y) = SurfaceShelter.HoldAtTheThreshold(spec, px, py);
            Assert.Equal(px, x, 9);
            Assert.Equal(py, y, 9);
        }
    }

    [Fact]
    public void HoldingIsStableSoNothingJittersOnTheFace()
    {
        // Held out, then held again: the second call must be a no-op. Without the small overshoot past the
        // face, a body driven at the door would land exactly ON it, read as inside next frame, and vibrate
        // there — which reads as a bug even though nothing ever gets in.
        SurfaceStructure.Spec spec = Drum(1.4);
        (double x1, double y1) = SurfaceShelter.HoldAtTheThreshold(spec, spec.CentreX + 2, spec.CentreY - 1);
        (double x2, double y2) = SurfaceShelter.HoldAtTheThreshold(spec, x1, y1);

        Assert.Equal(x1, x2, 9);
        Assert.Equal(y1, y2, 9);
    }

    [Fact]
    public void TheCAPTAINIsNeverHeldOut()
    {
        // Stated as a law because the whole mechanic depends on it: the captain walks in, and the air stops
        // (#573). HoldAtTheThreshold is applied to hostiles only — this pins that Contains() still says YES
        // for the middle of a shelter, which is what the air branch reads.
        SurfaceStructure.Spec spec = Drum(2.0);
        Assert.True(SurfaceShelter.Contains(spec, spec.CentreX, spec.CentreY));
    }
}

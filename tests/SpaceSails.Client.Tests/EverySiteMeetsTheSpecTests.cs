using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #585 · EVERY SITE, NOT JUST THE ONE HE WAS STANDING ON.
///
/// <para>Owner, after two days of playing Miranda: <i>"we should check the other miranda sites are up to
/// spec. Then the other landing sites. We have raised the bar nicely here, now let's take our learnings to
/// the other places."</i></para>
///
/// <para>Every rule below is written down in <c>docs/features/the-landing-site.md</c>. This is that document
/// with teeth: it walks the <b>real</b> surface deck — the same <see cref="MoonSurface.SurfaceDeck"/> the
/// captain's boots collide with — for every landing site on every body, and reports the whole table at once
/// rather than dying on the first bad site, because "which sites fall short" is the actual question.</para>
///
/// <para>It lives in the CLIENT test project on purpose: the deck is where Core's geometry and the
/// renderer's meet, and that seam is where every bug this week actually lived. (That project was itself
/// missing from the solution until #573, so CI had never once run it.)</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class EverySiteMeetsTheSpecTests
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

    private static IEnumerable<(string Body, LandingSite Site)> EverySite() =>
        Bodies.SelectMany(b => LandingSites.For(b).Select(s => (b, s)));

    private static DeckPlan DeckFor(string body, LandingSite site) =>
        MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt, site.Name);

    /// <summary>Report every failing site, then fail once with the table. A guard that stops at the first bad
    /// site turns "audit the other places" into a queue of one-at-a-time surprises.</summary>
    private static void AuditEverySite(Func<string, LandingSite, DeckPlan, string?> check, string what)
    {
        var bad = new List<string>();
        foreach ((string body, LandingSite site) in EverySite())
        {
            string? complaint = check(body, site, DeckFor(body, site));
            if (complaint is not null)
            {
                bad.Add($"  {body}/{site.Name}: {complaint}");
            }
        }

        if (bad.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{bad.Count} site(s) fail: {what}");
            foreach (string line in bad)
            {
                sb.AppendLine(line);
            }
            Assert.Fail(sb.ToString());
        }
    }

    [Fact]
    public void EverySiteCarriesTheShelterItPromises()
    {
        // Spec 3.3 / 3.8. The rule that keeps a captain alive, checked on grounds nobody has walked yet.
        AuditEverySite((body, site, deck) =>
        {
            IReadOnlyList<SurfaceStructure.Spec> promised =
                SurfaceShelter.SpecsFor(body, site.LayoutSalt, MoonSurface.ExpeditionField());

            int racks = deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.ShelterTank);
            int lockers = deck.Consoles.Count(c => c.Kind == DeckPlan.ConsoleKind.ShelterLocker);

            if (promised.Count < 4)
            {
                return $"only {promised.Count} shelters — spec 3.3 says never under four.";
            }
            if (racks != promised.Count)
            {
                return $"{promised.Count} shelters promised to the tracker, {racks} built.";
            }
            if (lockers != promised.Count)
            {
                return $"{promised.Count} shelters, {lockers} lockers — a shelter that cannot reload you.";
            }
            return null;
        }, "spec 3.3/3.8 — the shelters a site promises");
    }

    [Fact]
    public void NoSiteHasADoorThatIsTheRideHome()
    {
        // Spec 6.3, and the bug that produced it: the shelter door was laid as SurfaceAirlock — the SAME
        // console kind as the tube's BOARD THE SHUTTLE — so pressing [E] on it flew the captain home from
        // the middle of the field ("I was at surface shelter and now at ship shuttle bay ... how").
        //
        // A console kind is a VERB. There is exactly ONE way off this ground, so there is exactly one
        // SurfaceAirlock on the plan, and it is at the tube.
        AuditEverySite((body, site, deck) =>
        {
            var boarding = deck.Consoles
                .Where(c => c.Kind == DeckPlan.ConsoleKind.SurfaceAirlock)
                .ToList();

            if (boarding.Count != 1)
            {
                return $"{boarding.Count} consoles claim to be the ride home — there is one shuttle.";
            }
            // And it is at the tube, not out in the field somewhere.
            if (Math.Abs(boarding[0].Y - MoonSurface.SurfaceTopY) > 8)
            {
                return $"the ride home sits at y={boarding[0].Y:F0}, nowhere near the tube mouth.";
            }
            return null;
        }, "spec 6.3 — only the shuttle takes you home");
    }

    [Fact]
    public void EverySiteHasSomethingBuiltOnIt()
    {
        // Spec 2.1. The complaint that started the whole rebuild — "no real buildings and one-thick walls
        // still" — was true of site 0 specifically because it is AUTHORED and bypassed the generator. Any
        // site that is nearly empty has fallen into the same hole.
        AuditEverySite((body, site, deck) =>
        {
            SurfaceLayout.Plan plan =
                SurfaceLayout.For(body, MoonSurface.ExpeditionField(), site.LayoutSalt);

            int buildings = (plan.BuildingFootprints ?? []).Count;
            if (buildings < 3)
            {
                return $"only {buildings} building(s) — the ground is a field with nothing on it.";
            }
            if ((plan.Doorways ?? []).Count < buildings)
            {
                return $"{buildings} buildings and {(plan.Doorways ?? []).Count} doorways — something is sealed.";
            }
            return null;
        }, "spec 2.1/2.3 — real buildings, each with a way in");
    }

    [Fact]
    public void NoSiteBuildsAnythingThroughAnythingElse()
    {
        // Spec 2.4, taken to every ground rather than the one the owner happened to be standing on when he
        // said "check this structure out... it functions but is kind of funny".
        AuditEverySite((body, site, _) =>
        {
            SurfaceLayout.Plan plan =
                SurfaceLayout.For(body, MoonSurface.ExpeditionField(), site.LayoutSalt);
            var prints = (plan.BuildingFootprints ?? []).ToList();

            for (int i = 0; i < prints.Count; i++)
            {
                for (int j = i + 1; j < prints.Count; j++)
                {
                    double gap = Distance(prints[i].X, prints[i].Y, prints[j].X, prints[j].Y);
                    if (gap <= prints[i].R + prints[j].R)
                    {
                        return $"buildings at ({prints[i].X:F0}, {prints[i].Y:F0}) and " +
                            $"({prints[j].X:F0}, {prints[j].Y:F0}) are {gap:F1} du apart — merged.";
                    }
                }
            }

            foreach (SurfaceStructure.Spec shelter in
                     SurfaceShelter.SpecsFor(body, site.LayoutSalt, MoonSurface.ExpeditionField()))
            {
                double sr = SurfaceStructure.KeepOutRadius(shelter);
                foreach ((double bx, double by, double br) in prints)
                {
                    double gap = Distance(shelter.CentreX, shelter.CentreY, bx, by);
                    if (gap <= sr + br)
                    {
                        return $"a shelter at ({shelter.CentreX:F0}, {shelter.CentreY:F0}) has grown into a " +
                            $"building {gap:F1} du away.";
                    }
                }
            }
            return null;
        }, "spec 2.4 — nothing is built on top of anything else");
    }

    [Fact]
    public void NoSiteDrawsItsGeographyInTheSHIPSInk()
    {
        // Spec 1.3. Removing the rectangular fence only got half of the "it looks artificial" complaint: a
        // body's own solid geography was flagged IsHull, which is the ship's cold pressure-hull stroke. On
        // one Miranda site that was 16 of 25 segments drawn as if a moon were a spacecraft.
        AuditEverySite((_, _, deck) =>
        {
            int hull = deck.Walls.Count(w => w.IsHull && !w.Unseen);
            int stone = deck.Walls.Count(w => w.IsStone);

            // The ship's own hull and the tube are legitimately hull-inked and they are a small, fixed set.
            if (hull > 40)
            {
                return $"{hull} segments drawn in pressure-hull ink ({stone} in stone) — a moon is not a ship.";
            }
            return null;
        }, "spec 1.3 — rock draws as rock");
    }

    [Fact]
    public void NoSiteFencesItsGroundAtAll()
    {
        // Spec 1.2, and it is a STRONGER rule than the one it replaces. "the rectangular fence spoils the
        // site feeling" was first answered by making the boundary unseen (it collided and was never drawn)
        // and then by making it wander, and this test used to check that the wandering bound was still there
        // — at least twenty unseen segments.
        //
        // #563 · There is no bound. The ground is an unbounded lattice of addressed tiles (SurfaceTiles) and
        // what stops a captain is the tank and the walk home, so the thing to assert is not that the fence is
        // well hidden but that NOTHING IS BUILT ALONG THE EDGE AT ALL. A wall lying on the field's port,
        // starboard or deep line would be the fence coming back — visible or not, wandering or not — and
        // would stop a captain walking onto the next tile.
        //
        // The TOP is exempt and always has been: that edge is the ship's own underside, a made thing with a
        // real outside, and drawing it as hull is correct (owner: "The space ships come with outside borders
        // but the landing site out-doors should not").
        SurfaceLayout.Field env = MoonSurface.ExpeditionField();
        const double Grazing = 1.0;   // how close to the line counts as lying on it

        AuditEverySite((_, _, deck) =>
        {
            foreach (DeckPlan.Wall w in deck.Walls)
            {
                bool onPort = Math.Abs(w.X1 - env.LeftX) < Grazing && Math.Abs(w.X2 - env.LeftX) < Grazing;
                bool onStarboard = Math.Abs(w.X1 - env.RightX) < Grazing && Math.Abs(w.X2 - env.RightX) < Grazing;
                bool onDeep = Math.Abs(w.Y1 - env.BottomY) < Grazing && Math.Abs(w.Y2 - env.BottomY) < Grazing;

                if (onPort || onStarboard || onDeep)
                {
                    string side = onPort ? "port" : onStarboard ? "starboard" : "deep";
                    return $"a wall lies along the {side} edge at ({w.X1:F1}, {w.Y1:F1})-({w.X2:F1}, {w.Y2:F1}) " +
                        "— the ground is fenced again.";
                }
            }
            return null;
        }, "spec 1.2 — there is no edge to hide");
    }

    [Fact]
    public void EveryAWAYEXPEDITIONGroundHasRealSpacesWithDoorsToo()
    {
        // #585 · Owner, after the Miranda rebuild: "we should take these upgrades to all our outside scenes
        // now. The biggest is the real spaces with doors... that is the place to find stuff. And clues."
        //
        // The three away grounds were exactly where Miranda was two days ago — walls and a landmark, nothing
        // to walk INTO — and they were missed for exactly the reason canon site 0 was missed: they are
        // AUTHORED, so they bypass the generator where all the improvements live. That is the trap this test
        // exists to spring, because it will happen again the next time somebody hand-writes a ground.
        var bad = new List<string>();
        foreach (ExpeditionSiteKind kind in Enum.GetValues<ExpeditionSiteKind>())
        {
            SurfaceLayout.Plan plan = SurfaceLayout.ForExpedition(kind, MoonSurface.ExpeditionField());
            int buildings = (plan.BuildingFootprints ?? []).Count;
            int doorways = (plan.Doorways ?? []).Count;

            if (buildings < 3)
            {
                bad.Add($"  {kind}: {buildings} building(s) — a signature to look at and nowhere to go.");
                continue;
            }
            if (doorways < buildings)
            {
                bad.Add($"  {kind}: {buildings} buildings, {doorways} doorways — something is sealed.");
                continue;
            }

            var prints = (plan.BuildingFootprints ?? []).ToList();
            for (int i = 0; i < prints.Count; i++)
            {
                for (int j = i + 1; j < prints.Count; j++)
                {
                    double gap = Distance(prints[i].X, prints[i].Y, prints[j].X, prints[j].Y);
                    if (gap <= prints[i].R + prints[j].R)
                    {
                        bad.Add($"  {kind}: two buildings {gap:F1} du apart have merged.");
                    }
                }
            }

            // #585 · And the SHELTERS on an away rock. These grounds are keyed on the expedition KIND, and a
            // kind cannot name a body, so ForExpedition cannot reserve a body's shelter spots the way the
            // seeded grounds do — the ground and the shelters are placed by two routines that genuinely
            // cannot see each other. That is precisely the arrangement this project keeps getting bitten by,
            // so it is checked here rather than assumed.
            foreach (string rock in ExpeditionRockIds(kind))
            {
                foreach (SurfaceStructure.Spec shelter in
                         SurfaceShelter.SpecsFor(rock, "", MoonSurface.ExpeditionField()))
                {
                    double sr = SurfaceStructure.KeepOutRadius(shelter);
                    foreach ((double bx, double by, double br) in prints)
                    {
                        double gap = Distance(shelter.CentreX, shelter.CentreY, bx, by);
                        if (gap <= sr + br)
                        {
                            bad.Add($"  {kind}/{rock}: a shelter has grown into a building ({gap:F1} du).");
                        }
                    }
                }
            }
        }

        if (bad.Count > 0)
        {
            Assert.Fail("away grounds fail spec 2.1/2.3/2.4:" + Environment.NewLine
                + string.Join(Environment.NewLine, bad));
        }
    }

    /// <summary>The away-rock body ids that route to a given expedition ground. A rock's id CARRIES its
    /// kind, so this is how a kind-keyed ground and a body-keyed shelter set can be compared at all.</summary>
    private static IEnumerable<string> ExpeditionRockIds(ExpeditionSiteKind kind) =>
        [ExpeditionSite.BodyIdFor(kind)];

    private static double Distance(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx, dy = ay - by;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #573 · THE MAP MUST NOT LIE.
///
/// <para>Owner, standing on bare regolith with a beacon ring dead centre on his motion tracker: <i>"there is
/// nothing on the shelter spot here ... look"</i>, and then the diagnosis in three words — <b>"the map
/// lies"</b>.</para>
///
/// <para>He was right. <c>SurfaceShelter</c> had grown from one shelter to several (<c>SpecsFor</c>), the
/// tracker beacons were switched over to show all of them, and the code that actually BUILDS them was left
/// calling <c>SpecFor</c> and laying exactly one. Two to four rings pointed at buildings that had never been
/// built, including the one he was standing on top of.</para>
///
/// <para>That is the second time in a day that two places had to agree and only one was changed — the field
/// envelope was the first. Reasoning did not catch either; both were found by a person looking at a screen.
/// So this is the guard: <b>walk the real surface deck and require the instrument and the ground to say the
/// same thing.</b> It lives in the CLIENT test project because that is where the duplication was, and a Core
/// test could never have seen it.</para>
/// </summary>
// The client assembly targets the browser, so a test that touches its geometry has to say so — otherwise
// CA1416 (rightly) refuses the call sites. It only affects the analyzer; xunit runs it on the test host.
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class ShelterBeaconsTellTheTruthTests
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

    [Fact]
    public void EveryShelterTheGamePROMISESIsActuallyBuilt()
    {
        // The bug, stated as a law. SurfaceShelter.SpecsFor is what the beacons point at; the deck is what
        // the captain can walk into. If a spec has no charging rack on the plan, the fan is pointing at
        // nothing and a captain will walk a long way on a lie.
        foreach ((string body, LandingSite site) in EverySite())
        {
            IReadOnlyList<SurfaceStructure.Spec> promised =
                SurfaceShelter.SpecsFor(body, site.LayoutSalt, MoonSurface.ExpeditionField());

            DeckPlan deck = DeckFor(body, site);
            var racks = deck.Consoles
                .Where(c => c.Kind == DeckPlan.ConsoleKind.ShelterTank)
                .ToList();

            Assert.True(racks.Count == promised.Count,
                $"{body}/{site.Name}: {promised.Count} shelters promised to the tracker, {racks.Count} built.");

            foreach (SurfaceStructure.Spec spec in promised)
            {
                Assert.True(
                    racks.Any(r => Math.Abs(r.X - spec.CentreX) < 0.5 && Math.Abs(r.Y - spec.CentreY) < 0.5),
                    $"{body}/{site.Name}: a beacon points at ({spec.CentreX:F0}, {spec.CentreY:F0}) " +
                    "and there is no shelter there.");
            }
        }
    }

    [Fact]
    public void EveryBuiltShelterHasItsWallsItsDoorAndItsServices()
    {
        // The other half of the owner's report on these ("missing the services and the doors"): a rack in
        // open regolith would be just as much of a lie as a beacon over nothing.
        foreach ((string body, LandingSite site) in EverySite())
        {
            DeckPlan deck = DeckFor(body, site);

            foreach (SurfaceStructure.Spec spec in
                     SurfaceShelter.SpecsFor(body, site.LayoutSalt, MoonSurface.ExpeditionField()))
            {
                Assert.True(
                    deck.Consoles.Any(c => c.Kind == DeckPlan.ConsoleKind.ShelterLocker
                        && Math.Abs(c.X - spec.CentreX) < 0.5),
                    $"{body}/{site.Name}: a shelter with no emergency locker.");

                // A door hung somewhere on this shelter's shell.
                double reach = Math.Max(spec.Width, spec.Height);
                Assert.True(
                    deck.Doors.Any(d => Math.Abs(((d.X1 + d.X2) / 2) - spec.CentreX) < reach
                        && Math.Abs(((d.Y1 + d.Y2) / 2) - spec.CentreY) < reach),
                    $"{body}/{site.Name}: a shelter with no door.");

                // And walls around it.
                Assert.True(
                    deck.Walls.Count(w => Math.Abs(w.X1 - spec.CentreX) < reach
                        && Math.Abs(w.Y1 - spec.CentreY) < reach) > 8,
                    $"{body}/{site.Name}: a shelter with no walls to speak of.");
            }
        }
    }

    [Fact]
    public void YouCanStandInsideEveryShelterTheGamePointsAt()
    {
        // The last link in the chain: the beacon is honest, the building exists — and its inside is somewhere
        // a captain can actually be, which is what makes the air stop draining (#573).
        foreach ((string body, LandingSite site) in EverySite())
        {
            foreach (SurfaceStructure.Spec spec in
                     SurfaceShelter.SpecsFor(body, site.LayoutSalt, MoonSurface.ExpeditionField()))
            {
                Assert.True(SurfaceShelter.Contains(spec, spec.CentreX, spec.CentreY),
                    $"{body}/{site.Name}: the middle of a shelter does not count as being inside it.");
            }
        }
    }
}

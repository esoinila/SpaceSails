using System;
using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Tests;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #1383 · THE APPENDED LAB IS ONE CONNECTED PLACE, on the REAL ground.
///
/// <para>The chamber a forced door appends grows from the door's own spot, and the lift-head shed is a
/// full-sized hut standing ON that spot (#606), so the hut's wall ring ran straight through the antechamber:
/// the field log, the backup rig and the core log were drawn inside a shed's wall and could not be walked
/// between. Core's own tests build the chamber alone, with no shed round it, which is why they never
/// noticed. This drives <see cref="MoonSurface.SurfaceDeck"/> (the shed and every other seeded wall) with
/// the chamber's walls added, exactly as <c>AppendSecretLabGeometry</c> adds them, and walks from the
/// hidden-door square to EVERY console in the mountain with every lab door standing open (a shut door is
/// time, not a wall, #563).</para>
/// </summary>
[Collection(StopRegisterCollection.Name)]
public sealed class TheAppendedLabIsOneConnectedPlaceTests
{
    private const double AvatarRadius = 0.7;
    private static readonly SurfaceLayout.Field Env = MoonSurface.ExpeditionField();

    private static readonly string[] Bodies =
        [.. new[] { "secret-lab-site", "secret-lab-site-unlisted", "miranda", "luna", "phobos", "europa", "titan",
                    "ganymede", "callisto", "enceladus", "triton" },
         .. Enumerable.Range(0, 40).Select(i => $"seeded-rock-{i}")];

    [Fact]
    public void FromTheHiddenDoorEveryConsoleInTheMountainCanBeWalkedTo()
    {
        var wrong = new List<string>();
        int checkedLabs = 0, checkedConsoles = 0;
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                DeckPlan deck = MoonSurface.SurfaceDeck(body, body, [], 0, (_, _) => { }, site.LayoutSalt,
                    site.Name, monolithEpoch: 0, hasSecretSite: true);
                SecretLab.Placement lab = SecretLab.OnThisSite(body, site.LayoutSalt, Env, forcePresent: true);
                SecretLab.Region region = SecretLab.Build(body, Env, lab.DoorX, lab.DoorY);

                var segments = deck.Walls
                    .Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2))
                    .Concat(region.Walls.Select(w => new SurfaceCollision.Segment(w.X1, w.Y1, w.X2, w.Y2)))
                    .ToArray();
                SurfaceCollision.WallIndex walls = SurfaceCollision.WallIndex.Build(segments);

                var door = new DeckReachability.Point(lab.DoorX, lab.DoorY);
                var bounds = (Math.Min(region.MinX, lab.DoorX) - 30, region.MinY - 30,
                              Math.Max(region.MaxX, lab.DoorX) + 30, region.MaxY + 30);
                checkedLabs++;
                if (!DeckReachability.Standable(door.X, door.Y, AvatarRadius, walls))
                {
                    wrong.Add($"  {body}/{site.Name}: the hidden-door square is not standable");
                    continue;
                }
                foreach (SecretLab.LabConsole c in region.Consoles)
                {
                    checkedConsoles++;
                    var at = new DeckReachability.Point(c.X, c.Y);
                    if (!DeckReachability.Path(door, at, walls, AvatarRadius, bounds).Reached)
                    {
                        wrong.Add($"  {body}/{site.Name}: {c.Id} cannot be walked to from the hidden door");
                    }
                }
            }
        }

        Assert.True(checkedLabs >= 20, $"only {checkedLabs} lab(s) were swept - this proves little.");
        Assert.True(checkedConsoles >= 100, $"only {checkedConsoles} console(s) were swept.");
        Assert.True(wrong.Count == 0, "the appended lab is not one connected walk:\n" + string.Join("\n", wrong));
    }
}

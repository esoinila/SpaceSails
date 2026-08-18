using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #625 · THE INSTRUMENT POINTS AT THE BUILDING, NOT AT THE SEED IT GREW FROM.
///
/// <para>Flagged by the #606 build and deliberately left out of it, so it could be pinned rather than quietly
/// patched: the tracker painted the secret lab at <c>lab.DoorX/DoorY</c> — <see cref="SecretLab.For"/>'s RAW
/// seeded spot — while the hut the captain walks to stands at <see cref="SecretLab.HeadSpot"/>, which is that
/// spot MOVED clear of the shelters, the outpost and the monolith already standing on that ground.</para>
///
/// <h3>Why it was worth a test rather than a one-line commit — and what the test found</h3>
///
/// <para>#625 was filed as a tidy: <i>"It is not wrong on screen today, because the wash is deliberately
/// vague and the nudge is small enough to sit inside it."</i> <b>That was not true.</b> This sweep, written
/// to be the boring proof of a cosmetic fix, disagreed on <b>21 of 34 body × site pairs, by up to 235 du</b>
/// against a wash 45 du wide.</para>
///
/// <para>The seam is one nobody had looked straight at: the raw door spot is seeded PER BODY, and the
/// placement clamp re-seeds PER SITE — so when it fires it does not nudge the head, it RELOCATES it. Miranda
/// painted the identical patch of ground on all three of its sites while the hut stood elsewhere on two of
/// them. That is "the map lies" (#584), already shipping, with a precise ring on it once the door was found.
/// So the guard is not "is it close enough today"; it is <i>does the paint site ask the builder</i>, asserted
/// on every body and every site of the shipped sweep.</para>
///
/// <h3>The tolerance is derived, and it is proven to select</h3>
///
/// <para>The slack is a hundredth of the wash's OWN spread, read off the tuple the shipping code returns —
/// not a number typed into a test, which is the constant-mirroring failure this file exists to stop. And
/// because a tolerance that nothing can fail is a green test that asserts nothing (the fifth bug class), the
/// sweep also measures how far the nudge actually MOVES the head on the shipped seeds and fails if that
/// distance does not comfortably beat the tolerance. If the clamp is ever neutered, this goes red for having
/// stopped covering the case rather than passing on a world that can no longer tell pass from fail.</para>
///
/// <para><b>Proven RED</b> two ways: pointing either paint site back at <c>lab.DoorX/DoorY</c> fails the
/// agreement asserts on the sites where the nudge fires, and tightening the tolerance toward zero keeps them
/// green only because the two coordinates are now literally the same call.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWashPointsAtTheHutThatWasBuiltTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>The shipped sweep — the same set the lift's own guard walks, so the two cannot drift apart
    /// about which grounds count.</summary>
    private static readonly string[] Bodies =
    [
        "luna", "phobos", "europa", "ganymede", "callisto",
        "titan", "enceladus", "miranda", "triton", "the-clinker",
        "secret-lab-site", "secret-lab-site-unlisted",
    ];

    private static SurfaceLayout.Field Field => MoonSurface.ExpeditionField();

    /// <summary>Where the captain is standing while the fan is read. Arbitrary and held FIXED across every
    /// site, because the bearing and range are measured from here: if the painted point moves, only the thing
    /// being painted could have moved it.</summary>
    private const double StandingX = 0.0;
    private const double StandingY = -60.0;

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A shipping page out on the regolith of <paramref name="body"/> at <paramref name="site"/>,
    /// holding a real excursion with a real lab placement, and TIPPED about it — which is what the wash needs
    /// before it will say anything at all.</summary>
    private static Pages.Map Standing(string body, LandingSite site, bool revealed)
    {
        var map = new Pages.Map();

        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on "
                + "has moved.");
        pending.SetValue(map, true);

        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden | BindingFlags.Static)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden | BindingFlags.Static)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(body, body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("Site")!.SetValue(ex, site);
        exType.GetProperty("Floor")!.SetValue(ex, 0);
        // forcePresent, because the sweep is about WHERE the head is painted and not about which bodies the
        // seed happens to give one to. Every body on this list therefore has a lab to be vague about.
        exType.GetProperty("Lab")!.SetValue(ex, SecretLab.For(body, Field, forcePresent: true));
        exType.GetProperty("SecretLabDoorRevealed")!.SetValue(ex, revealed);

        Set(map, "_surface", ex);
        Set(map, "_avatarX", StandingX);
        Set(map, "_avatarY", StandingY);
        Get<HashSet<string>>(map, "_labLeads").Add(body);
        return map;
    }

    private static void Set(object target, string field, object? value) =>
        (typeof(Pages.Map).GetField(field, Hidden)
         ?? throw new InvalidOperationException($"Map has no field {field}."))
        .SetValue(target, value);

    private static T Get<T>(object target, string field) =>
        (T)(typeof(Pages.Map).GetField(field, Hidden)
            ?? throw new InvalidOperationException($"Map has no field {field}."))
        .GetValue(target)!;

    private static object Call(Pages.Map map, string method) =>
        (typeof(Pages.Map).GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"Map has no {method} — the paint site has moved."))
        .Invoke(map, [Get<object>(map, "_surface")])!;

    /// <summary>Undo the polar arithmetic the fan is drawn in and get the GROUND POINT the instrument is
    /// claiming. Read back the way a player reads it — a bearing and a range from where they stand — so the
    /// test is checking the picture and not the variable behind it.</summary>
    private static (double X, double Y) Painted(double bearing, double range) =>
        (StandingX + (range * Math.Cos(bearing)), StandingY + (range * Math.Sin(bearing)));

    private static double Apart((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    // ── The laws ──────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void THE_WASH_IsCentredOnTheHutTheBuilderBuilt()
    {
        var wrong = new List<string>();
        double slack = -1;
        int pairs = 0;

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                pairs++;
                Pages.Map map = Standing(body, site, revealed: false);
                var wash = (List<(double Bearing, double Range, double Spread)>)Call(map, "BuildRumours");

                Assert.True(wash.Count == 1,
                    $"{body} · {site.Name}: a tipped captain with an unfound lab got {wash.Count} washes, "
                    + "so this sweep is not reading the instrument it thinks it is.");

                // The slack is a hundredth of the wash's OWN spread, taken off the shipping tuple — a test
                // that typed 0.45 here would be the mirrored constant this whole file is about.
                slack = wash[0].Spread / 100.0;

                (double X, double Y) painted = Painted(wash[0].Bearing, wash[0].Range);
                // Asked of the BUILDER. MoonSurface.LiftHead is what draws the shed; its hut's centre is the
                // ground the captain walks to, and nothing else in this test is allowed to compute it.
                MoonSurface.LiftHeadBox built = MoonSurface.LiftHead(body, site.LayoutSalt, Field);

                double off = Apart(painted, (built.CentreX, built.CentreY));
                if (off > slack)
                {
                    wrong.Add(
                        $"  {body} · {site.Name}: the wash is centred ({painted.X:F1}, {painted.Y:F1}) and "
                        + $"the hut stands at ({built.CentreX:F1}, {built.CentreY:F1}) — {off:F2} du apart.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "the tracker is washing ground the builder did not build on:\n" + string.Join("\n", wrong));

        // Every body carries at least its Wild Plain, so a sweep that walked fewer pairs than bodies has
        // stopped enumerating the world and is green for the wrong reason.
        Assert.True(pairs >= Bodies.Length,
            $"only {pairs} body × site pairs were swept for {Bodies.Length} bodies.");

        ProveTheSlackCanSelect(slack);
    }

    [Fact]
    public void THE_REVEALED_RING_IsOnTheHutToo()
    {
        // The ring is the harder half: once the door is found the fan stops being vague and paints a MARK, so
        // there is no wash to hide a nudge inside. Same law, no tolerance to lean on.
        var wrong = new List<string>();

        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                Pages.Map map = Standing(body, site, revealed: true);
                var beacons =
                    (List<(double Bearing, double Range, bool IsHome, bool IsLab)>)Call(map, "BuildBeacons");

                var lab = beacons.FindAll(b => b.IsLab);
                Assert.True(lab.Count == 1,
                    $"{body} · {site.Name}: a revealed door drew {lab.Count} lab rings, so this sweep is not "
                    + "reading the instrument it thinks it is.");

                (double X, double Y) painted = Painted(lab[0].Bearing, lab[0].Range);
                MoonSurface.LiftHeadBox built = MoonSurface.LiftHead(body, site.LayoutSalt, Field);

                double off = Apart(painted, (built.CentreX, built.CentreY));
                // A hundredth of a du: the two are the same call now, and the only slack a resolved bearing
                // and range need is the arithmetic's own.
                if (off > 0.01)
                {
                    wrong.Add(
                        $"  {body} · {site.Name}: the ring is at ({painted.X:F1}, {painted.Y:F1}) and the hut "
                        + $"stands at ({built.CentreX:F1}, {built.CentreY:F1}) — {off:F2} du apart.");
                }
            }
        }

        Assert.True(wrong.Count == 0,
            "the found-lab ring is on ground the builder did not build on:\n" + string.Join("\n", wrong));
    }

    /// <summary>ANTI-VACUOUS. A tolerance nothing can trip is a green test that asserts nothing, so the sweep
    /// has to show that the nudge — the whole difference between the raw seed and the built hut — is real and
    /// bigger than the slack on at least one shipped site. If the clamp ever stops moving anything, this
    /// fails for the honest reason: the guard has stopped covering the bug.</summary>
    private static void ProveTheSlackCanSelect(double slack)
    {
        Assert.True(slack > 0, "the wash reported no spread, so the derived tolerance is meaningless.");

        double worst = 0;
        string where = "nowhere";
        foreach (string body in Bodies)
        {
            foreach (LandingSite site in LandingSites.For(body))
            {
                SecretLab.Placement raw = SecretLab.For(body, Field, forcePresent: true);
                (double hx, double hy) = SecretLab.HeadSpot(body, site.LayoutSalt, Field);
                double moved = Apart((raw.DoorX, raw.DoorY), (hx, hy));
                if (moved > worst)
                {
                    worst = moved;
                    where = $"{body} · {site.Name}";
                }
            }
        }

        Assert.True(worst > slack,
            $"the nudge never moved the head further than {worst:F2} du (worst: {where}) and the tolerance is "
            + $"{slack:F2} du — so these asserts would pass on the raw seed too, and they prove nothing. "
            + "Either the placement clamp has stopped working, or this sweep no longer covers #625.");
    }
}

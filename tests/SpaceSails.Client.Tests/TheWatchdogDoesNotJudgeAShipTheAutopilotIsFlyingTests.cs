using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #962 · <b>"WHAT IS THIS … AUTOPILOT CRASHING US?"</b>
///
/// <para>Owner, playing 2026-08-21. The banner reads <i>"🛰 AUTOPILOT HAS THE SHIP — NOW: approaching The
/// Red Eye — autopilot flying · NEXT: auto-dock at The Red Eye in 21 h"</i>, warp 1×, and across it in red:
/// <i>"⚠ orbit degrading at Jupiter — periapsis under the surface — impact coming; re-park (≈48 p) or
/// leave"</i>. A moment later: <i>"Now the collision alert went away"</i>.</para>
///
/// <h3>What this file does</h3>
/// <para>It flies that sighting on the shipping <c>scenarios/sol.json</c> and the shipping frame loop, and
/// then asks the world — not a sentence — whether the ship was ever in danger. A ship free-flying in
/// Jupiter's well arms the autopilot for The Red Eye, the storm-watcher port at 8.5 M km; from there
/// <b>nothing is touched</b> but the clock. Three numbers come out, and they are the whole issue:</para>
/// <list type="bullet">
/// <item>the rehearsed plan's tightest Jupiter pass — <b>1.35 R</b>, clear of the 1.1 R floor;</item>
/// <item>the flown track's own closest approach to Jupiter — <b>1.41 R</b>, clear of it too;</item>
/// <item>and the osculating conic the watchdog was reading, mid-approach — <b>periapsis 0.06 R</b>.</item>
/// </list>
/// <para>The third number is the alarm's whole case, and it is a prediction about a coast that never
/// happens: the terminal approach loop re-points the ship at the station every step, and the trip ends
/// clamped on at The Red Eye. So the ship was never in danger, the alarm was wrong, and the fix is the
/// #196/#220 trust one alarm over — while the autopilot flies a rehearsed path that CLEARED this body, the
/// park watchdog defers to the rehearsal.</para>
///
/// <h3>RED PROOF (watched, this branch, before this shipped)</h3>
/// <para>With <c>UpdateParkStability</c>'s call to <c>OrbitDegradeAlertRule.Evaluate</c> reverted to the
/// old two-line <c>_orbitKept &amp;&amp; TideRisk</c> downgrade, and <c>RaiseOrbitDegrade</c>'s offer put
/// back to the flat <c>"re-park (≈N p) or leave"</c>, three of the four tests below go RED and the fourth
/// stays green:</para>
/// <list type="bullet">
/// <item><c>THE_SIGHTING</c> — <b>RED</b>: the banner shouts on <b>26</b> frames of a flight that ends
/// clamped on at The Red Eye and never touches a surface. That is the owner's screenshot.</item>
/// <item><c>A_KNOCK_THE_AUTOPILOT_FLIES_OUT_OF</c> — <b>RED</b>: 3 frames of shouting at a ship that
/// docks 1.36 R clear.</item>
/// <item><c>THE_OFFER</c> — <b>RED</b>: <i>"re-park (≈39 p) or leave"</i>, to a ship under autopilot.</item>
/// <item><c>KNOCKED_OFF_THE_PLAN</c> — <b>GREEN both ways</b>, and that is the point: the fix takes away
/// the false shout and leaves the true one standing.</item>
/// </list>
///
/// <h3>Anti-vacuity</h3>
/// <para><see cref="THE_SIGHTING_TheAlarmIsSilentOnTheRehearsedPath"/> asserts that the raw
/// <c>OrbitRule.ParkStability</c> verdict really did read <c>Subsurface</c> during the flight — so the
/// silence is a DEFERRAL being exercised, not a bench that never met the condition. And
/// <see cref="KNOCKED_OFF_THE_PLAN_TheAlarmStillFires"/> knocks the same ship off that path with a real
/// retrograde impulse and watches the same banner come back.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheWatchdogDoesNotJudgeAShipTheAutopilotIsFlyingTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public TheWatchdogDoesNotJudgeAShipTheAutopilotIsFlyingTests(Xunit.Abstractions.ITestOutputHelper output) =>
        _out = output;

    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private const double Day = 86400.0;

    /// <summary>Where the ship stands when she arms: 1.5 M km out in Jupiter's well, on a slow, eccentric
    /// Jovian ellipse — 0.35 of the local circular speed, the state a ship is left in after a burn she
    /// made for some other reason. Pinned rather than searched so the bench is deterministic; every
    /// number the test asserts is measured from the flight itself.</summary>
    private const double StartRadiusMetres = 1.5e9;

    private const double StartTangentialFraction = 0.35;

    /// <summary>THE KNOCK: a retrograde impulse about Jupiter — an external shove, the way the world
    /// shoves a ship — fired while she is still falling toward her own periapsis. Three km/s of it takes
    /// her off the rehearsed path for good: she really does reach Jupiter's surface (the #264 impact
    /// enforcer ends the flight there), which is what makes this the case where the alarm is RIGHT.</summary>
    private const double KnockMetresPerSecond = -3000.0;

    /// <summary>…and a knock the autopilot simply flies out of: 200 m/s leaves the flight clear of the
    /// plan's own trust floor and clamped on at the end. The gate is "she left the plan", not "something
    /// touched her".</summary>
    private const double SmallKnockMetresPerSecond = -200.0;

    private const double KnockHour = 40.0;

    // ── (1) THE SIGHTING, FLOWN ───────────────────────────────────────────────────────────────────────

    [Fact]
    public void THE_SIGHTING_TheAlarmIsSilentOnTheRehearsedPath()
    {
        Pages.Map map = AShipInJupitersWell();
        CelestialBody jupiter = Body(map, "jupiter");
        double floor = OrbitRule.SurfaceParkRadii * jupiter.BodyRadius;

        double planPass = ArmForTheRedEye(map);
        _out.WriteLine($"rehearsed plan's tightest Jupiter pass: {planPass / jupiter.BodyRadius:F2} R " +
            $"(floor {OrbitRule.SurfaceParkRadii:F2} R)");
        Assert.True(planPass > floor,
            $"the bench's own plan must clear Jupiter, or it proves nothing: {planPass / jupiter.BodyRadius:F2} R.");

        Flight flight = FlyWithNoFurtherInput(map);
        _out.WriteLine($"flown: {flight}");

        // THE WORLD'S ANSWER: she never went near it. Both the plan and the flight clear the floor.
        Assert.True(flight.ClosestJupiter > floor,
            $"the flown track must clear Jupiter's floor, or the alarm was RIGHT: {flight.ClosestJupiter / jupiter.BodyRadius:F2} R.");
        Assert.Equal("red-eye", Get<string?>(map, "_dockedHavenId"));

        // ANTI-VACUITY: the raw watchdog reading really did go Subsurface mid-approach — this bench meets
        // the condition that used to shout, so the silence below is a deferral, not a bench that missed.
        Assert.True(flight.RawSubsurfaceFrames > 0,
            "the reconstruction must actually produce the Subsurface reading, or it is not the sighting.");
        _out.WriteLine($"raw ParkStability read Subsurface on {flight.RawSubsurfaceFrames} frame(s); " +
            $"tightest osculating periapsis {flight.WorstConicPeriapsis / jupiter.BodyRadius:F2} R");
        Assert.True(flight.WorstConicPeriapsis < floor,
            "…and that reading must be the under-the-surface one the owner was shown.");

        // …and the impact the alarm promised never came. The #264 enforcer ends a flight AT any surface it
        // really touches; across this whole trip it never fired.
        Assert.False(flight.Busted, "the ship must never actually reach Jupiter, or the alarm was RIGHT.");

        // THE FIX: with the plan's own clearance believed, the banner says nothing.
        Assert.Null(flight.FirstWarning);
        Assert.Equal(0, flight.WarningFrames);
    }

    // ── (2) KNOCK HER OFF IT AND THE SAME BANNER COMES BACK ───────────────────────────────────────────

    [Fact]
    public void KNOCKED_OFF_THE_PLAN_TheAlarmStillFires()
    {
        Pages.Map map = AShipInJupitersWell();
        CelestialBody jupiter = Body(map, "jupiter");
        double floor = OrbitRule.SurfaceParkRadii * jupiter.BodyRadius;
        double planPass = ArmForTheRedEye(map);

        Flight flight = FlyWithNoFurtherInput(map, KnockHour * 3600, KnockMetresPerSecond);
        _out.WriteLine($"knocked: {flight}");

        // This time the danger is real, and the world says so on its own: she reaches Jupiter's surface and
        // the #264 impact enforcer ends the flight there.
        Assert.True(flight.Busted, "the knock must really put her into Jupiter, or this is not the true case.");
        Assert.True(flight.ClosestJupiter < OrbitDegradeAlertRule.PlanTrustFloor(planPass, floor),
            $"…and deeper than the plan's own trust floor: {flight.ClosestJupiter / jupiter.BodyRadius:F2} R " +
            $"vs {OrbitDegradeAlertRule.PlanTrustFloor(planPass, floor) / jupiter.BodyRadius:F2} R.");

        // THE BANNER CAME BACK — and it came back BEFORE the surface, which is the whole job of a warning.
        Assert.NotNull(flight.FirstWarning);
        _out.WriteLine($"banner at {flight.FirstWarningHour:F1} h, " +
            $"{flight.FirstWarningDistance / jupiter.BodyRadius:F2} R out: {flight.FirstWarning}");
        Assert.Contains("orbit degrading at Jupiter", flight.FirstWarning!);
        Assert.Contains("periapsis under the surface", flight.FirstWarning!);
        Assert.Equal(AlertSeverity.Red, flight.FirstWarningSeverity);
        Assert.True(flight.FirstWarningDistance > jupiter.BodyRadius,
            "the warning must arrive while she is still above the surface, not as an obituary.");
    }

    /// <summary>A knock the autopilot simply flies out of is not an alarm. The same retrograde shove at
    /// 200 m/s leaves the flight clear of the plan's own trust floor and clamped on at the end, and the
    /// banner rightly says nothing — so the gate is "she left the plan", not "something touched her".</summary>
    [Fact]
    public void A_KNOCK_THE_AUTOPILOT_FLIES_OUT_OF_IsNotAnAlarm()
    {
        Pages.Map map = AShipInJupitersWell();
        CelestialBody jupiter = Body(map, "jupiter");
        double floor = OrbitRule.SurfaceParkRadii * jupiter.BodyRadius;
        double planPass = ArmForTheRedEye(map);

        Flight flight = FlyWithNoFurtherInput(map, KnockHour * 3600, SmallKnockMetresPerSecond);
        _out.WriteLine($"absorbed: {flight}");

        Assert.True(flight.ClosestJupiter > OrbitDegradeAlertRule.PlanTrustFloor(planPass, floor),
            $"the small knock must leave her inside the plan's own trust: {flight.ClosestJupiter / jupiter.BodyRadius:F2} R.");
        Assert.False(flight.Busted);
        Assert.Equal("red-eye", Get<string?>(map, "_dockedHavenId"));
        Assert.Null(flight.FirstWarning);
    }

    // ── (3) THE OFFER, ON THE SHIP THE OWNER WAS ACTUALLY FLYING ──────────────────────────────────────

    /// <summary>The banner's second half. A ship the autopilot has cannot be offered a manual re-park — the
    /// burn would fight the plan still being flown — so the line names what has the helm instead. Raised
    /// directly here because the fixed watchdog will not raise it on that ship at all, which is the point
    /// of the rest of this file.</summary>
    [Fact]
    public void THE_OFFER_ToAShipUnderAutopilot_DoesNotAskHerToRePark()
    {
        Pages.Map map = AShipInJupitersWell();
        ArmForTheRedEye(map);
        CelestialBody jupiter = Body(map, "jupiter");
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        double t = Get<double>(map, "SimTime");

        Invoke(map, "RaiseOrbitDegrade", jupiter,
            ephemeris.Position("jupiter", t),
            (ephemeris.Position("jupiter", t + 1) - ephemeris.Position("jupiter", t - 1)) / 2,
            OrbitRule.ParkStabilityVerdict.Subsurface);

        string warning = Get<string?>(map, "_orbitDegradeWarning")!;
        _out.WriteLine($"banner under autopilot: {warning}");
        Assert.DoesNotContain("re-park (≈", warning);
        Assert.Contains("the autopilot has the ship", warning);
        Assert.Contains("stand it down", warning);

        // …and a ship nobody is flying still gets the bill, because then it IS her choice. (The arm is
        // dropped the way a stand-down drops it — the state, not the click; the click's own #179
        // double-confirm is another file's subject.)
        Invoke(map, "AutopilotStandDown", "test — the captain takes her back");
        Assert.Null(Get<string?>(map, "_armedOrbitBodyId"));
        Invoke(map, "ClearOrbitDegrade");
        Invoke(map, "RaiseOrbitDegrade", jupiter,
            ephemeris.Position("jupiter", t),
            (ephemeris.Position("jupiter", t + 1) - ephemeris.Position("jupiter", t - 1)) / 2,
            OrbitRule.ParkStabilityVerdict.Subsurface);
        string free = Get<string?>(map, "_orbitDegradeWarning")!;
        _out.WriteLine($"banner on a free ship: {free}");
        Assert.Contains("re-park (≈", free);
    }

    // ── The bench ─────────────────────────────────────────────────────────────────────────────────────

    private readonly record struct Flight(
        int Frames, double Hours, double ClosestJupiter, double WorstConicPeriapsis,
        int RawSubsurfaceFrames, int WarningFrames, string? FirstWarning, double FirstWarningHour,
        double FirstWarningDistance, AlertSeverity? FirstWarningSeverity, bool Busted, string Ended)
    {
        public override string ToString() =>
            $"{Frames} frames, {Hours:F1} h, closest Jupiter {ClosestJupiter:E3} m, " +
            $"worst conic periapsis {WorstConicPeriapsis:E3} m, raw-Subsurface on {RawSubsurfaceFrames} frames, " +
            $"warning on {WarningFrames}, busted={Busted}, ended {Ended}";
    }

    /// <summary>
    /// SPEND THE CLOCK, TOUCH NOTHING ELSE — the shipping frame's own two phases, exactly as the #969
    /// arrival bench drives them, plus the once-a-frame <c>UpdateOrbitedBody</c> that owns the #180
    /// watchdog. The chunk of clock each pass buys is sized off BOTH closing geometries — the gap to the
    /// target and the gap to Jupiter — which is what the live frame's near-body warp cap does for a player.
    /// The optional knock is the one and only input, and it is not a command: it is an impulse applied to
    /// the ship, the way an external shove would be.
    /// </summary>
    private Flight FlyWithNoFurtherInput(Pages.Map map, double knockAtSimTime = -1, double knockDeltaV = 0)
    {
        var ephemeris = Get<ICelestialEphemeris>(map, "_ephemeris");
        CelestialBody jupiter = Body(map, "jupiter");
        CelestialBody sun = Body(map, "sun");
        double hill = OrbitRule.HillRadius(jupiter, sun.Mu);
        double start = Get<double>(map, "SimTime");
        bool knocked = knockDeltaV == 0;

        double closestJupiter = double.MaxValue, worstPeriapsis = double.MaxValue;
        int rawSubsurface = 0, warningFrames = 0, frames = 0;
        string? firstWarning = null;
        double firstWarningHour = -1, firstWarningDistance = -1;
        AlertSeverity? firstWarningSeverity = null;

        while (frames < 4000 && Get<double>(map, "SimTime") < start + 20 * Day)
        {
            if (Get<bool>(map, "_orbitKept") || Get<string?>(map, "_dockedHavenId") is not null
                || Get<object?>(map, "_busted") is not null)
            {
                break; // the trip is over — parked, clamped on, or ended at a surface (#264)
            }

            double simTime = Get<double>(map, "SimTime");
            if (!knocked && simTime >= knockAtSimTime)
            {
                knocked = true;
                var before = Get<ShipState>(map, "_ship");
                Vector2d jupiterVel = BodyVelocity(ephemeris, "jupiter", simTime);
                Vector2d retrograde = (before.Velocity - jupiterVel).Normalized();
                Set(map, "_ship", before with { Velocity = before.Velocity + retrograde * knockDeltaV });
                _out.WriteLine($"--- knocked {knockDeltaV:F0} m/s retrograde about Jupiter at {simTime / 3600:F1} h");
            }

            var ship = Get<ShipState>(map, "_ship");
            Vector2d targetPos = ephemeris.Position("red-eye", simTime);
            double targetGap = (ship.Position - targetPos).Length;
            double targetClosing = Math.Max(1.0, (ship.Velocity - BodyVelocity(ephemeris, "red-eye", simTime)).Length);
            double jupiterGap = (ship.Position - ephemeris.Position("jupiter", simTime)).Length;
            double jupiterClosing = Math.Max(1.0, (ship.Velocity - BodyVelocity(ephemeris, "jupiter", simTime)).Length);
            double chunk = Math.Clamp(
                Math.Min(targetGap / targetClosing, jupiterGap / jupiterClosing) / 10.0, 60.0, 20000 * 60.0);

            Set(map, "_effectiveWarp", 10000);
            Set(map, "_simAccumulator", chunk);
            int steps = (int)Invoke(map, "ConsumeTheAccumulator", false)!;
            Invoke(map, "PinHerToTheDockAndDriftTheGhost");
            Invoke(map, "AccountForWhatTheStepsDid", steps);
            Invoke(map, "UpdateOrbitedBody");
            frames++;

            ship = Get<ShipState>(map, "_ship");
            simTime = Get<double>(map, "SimTime");
            Vector2d jupiterPos = ephemeris.Position("jupiter", simTime);
            Vector2d jupiterVelocity = BodyVelocity(ephemeris, "jupiter", simTime);
            closestJupiter = Math.Min(closestJupiter, (ship.Position - jupiterPos).Length);
            worstPeriapsis = Math.Min(worstPeriapsis, ConicPeriapsis(ship, jupiterPos, jupiterVelocity, jupiter));
            if (OrbitRule.ParkStability(ship, jupiterPos, jupiterVelocity, jupiter, hill)
                == OrbitRule.ParkStabilityVerdict.Subsurface)
            {
                rawSubsurface++;
            }

            if (Get<string?>(map, "_orbitDegradeWarning") is { } warning)
            {
                warningFrames++;
                if (firstWarning is null)
                {
                    firstWarning = warning;
                    firstWarningHour = simTime / 3600;
                    firstWarningDistance = (ship.Position - jupiterPos).Length;
                    firstWarningSeverity = Alerts(map).Get(AlertKind.OrbitDegrade)?.Severity;
                }
            }
        }

        bool busted = Get<object?>(map, "_busted") is not null;
        string ended = busted ? "BUSTED on a surface"
            : Get<bool>(map, "_orbitKept") ? "KEPT"
            : Get<string?>(map, "_dockedHavenId") is { } berth ? $"DOCKED at {berth}"
            : Get<string?>(map, "_autopilotStandDownReason") is not null ? "stood down"
            : "still flying";
        return new Flight(frames, (Get<double>(map, "SimTime") - start) / 3600,
            closestJupiter, worstPeriapsis, rawSubsurface, warningFrames, firstWarning,
            firstWarningHour, firstWarningDistance, firstWarningSeverity, busted, ended);
    }

    /// <summary>The two-body periapsis of the ship's osculating conic about the body — the number
    /// <see cref="OrbitRule.ParkStability"/> takes its verdict from. +∞ on an unbound conic (no periapsis
    /// to speak of), so a hyperbolic instant never counts as the worst.</summary>
    private static double ConicPeriapsis(ShipState ship, Vector2d bodyPos, Vector2d bodyVel, CelestialBody body)
    {
        Vector2d r = ship.Position - bodyPos;
        Vector2d v = ship.Velocity - bodyVel;
        double energy = v.LengthSquared / 2 - body.Mu / r.Length;
        if (energy >= 0)
        {
            return double.PositiveInfinity;
        }

        double h = r.X * v.Y - r.Y * v.X;
        double a = -body.Mu / (2 * energy);
        double e = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (body.Mu * body.Mu)));
        return a * (1 - e);
    }

    /// <summary>Arm the autopilot for The Red Eye the way the O-key/Arm button does, and hand back the
    /// rehearsed plan's tightest Jupiter pass — the number the fixed watchdog believes.</summary>
    private double ArmForTheRedEye(Pages.Map map)
    {
        Invoke(map, "ToggleArmedInsertion", "red-eye");
        Assert.Equal("red-eye", Get<string?>(map, "_armedOrbitBodyId"));
        Assert.Null(Get<string?>(map, "_autopilotStandDownReason"));
        var clearance = Get<IReadOnlyDictionary<string, double>?>(map, "_autopilotPlanBodyClearance");
        Assert.NotNull(clearance);
        Assert.True(clearance!.TryGetValue("jupiter", out double jupiterPass),
            "the cached plan must know how close it came to Jupiter — this bench has drifted.");
        _out.WriteLine($"armed: {Get<string?>(map, "_armedTransferSummary")}");
        return jupiterPass;
    }

    /// <summary>A ship free-flying inside Jupiter's Hill sphere — outside every Galilean moon's — on a slow
    /// eccentric Jovian ellipse, with a full tank, nothing plotted and nothing armed.</summary>
    private static Pages.Map AShipInJupitersWell()
    {
        var map = new Pages.Map();
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "ComponentBase has no _hasPendingQueuedRender — the render early-out this bench rides on has moved.");
        pending.SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        Vector2d jupiterPos = ephemeris.Position("jupiter", 0);
        Vector2d jupiterVel = BodyVelocity(ephemeris, "jupiter", 0);
        CelestialBody jupiter = FindBody(ephemeris, "jupiter");

        // Off the Red Eye's own bearing by 0.7 rad, so the station is a real transfer away, not overhead.
        Vector2d toStation = ephemeris.Position("red-eye", 0) - jupiterPos;
        double bearing = Math.Atan2(toStation.Y, toStation.X) + 0.7;
        var outward = new Vector2d(Math.Cos(bearing), Math.Sin(bearing));
        var prograde = new Vector2d(-outward.Y, outward.X);
        double circular = Math.Sqrt(jupiter.Mu / StartRadiusMetres);

        Set(map, "_ship", new ShipState(
            jupiterPos + outward * StartRadiusMetres,
            jupiterVel + prograde * (circular * StartTangentialFraction),
            0));
        Set(map, "SimTime", 0.0);
        Set(map, "_reactionMassPulses", 500);
        return map;
    }

    // ── Reflection plumbing (the #969 arrival bench's idiom) ──────────────────────────────────────────

    private static ShipAlerts Alerts(Pages.Map map) => Get<ShipAlerts>(map, "_shipAlerts");

    private static CelestialBody Body(Pages.Map map, string id) =>
        FindBody(Get<ICelestialEphemeris>(map, "_ephemeris"), id);

    private static CelestialBody FindBody(ICelestialEphemeris ephemeris, string id)
    {
        foreach (CelestialBody body in ephemeris.Bodies)
        {
            if (body.Id == id)
            {
                return body;
            }
        }

        throw new InvalidOperationException($"scenarios/sol.json has no body '{id}' — this bench has drifted.");
    }

    private static Vector2d BodyVelocity(ICelestialEphemeris ephemeris, string id, double simTime) =>
        (ephemeris.Position(id, simTime + 1) - ephemeris.Position(id, simTime - 1)) / 2;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    private static string ScenarioPath(string file)
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "scenarios")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new InvalidOperationException("no scenarios/ directory above the test binary")
            : System.IO.Path.Combine(dir.FullName, "scenarios", file);
    }

    private static void Set(object o, string field, object? value) =>
        (o.GetType().GetField(field, Hidden)
         ?? throw new InvalidOperationException($"no field {field} on {o.GetType().Name} — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string name) =>
        (T)(o.GetType().GetField(name, Hidden)
            ?? throw new InvalidOperationException($"no field {name} on Map — this bench has drifted"))
            .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);
}

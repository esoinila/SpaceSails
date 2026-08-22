using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #962 · THE ARRIVAL-BRAKE CARD, ASKED OF A REAL <see cref="Pages.Map"/>.
///
/// <para>Owner, over a screenshot of the ship <i>docked at The Red Eye — clamped on — lying low, 3 km out,
/// rel 0.0 km/s</i>, with a Jupiter aerobrake card up: <i>"This pop-up still shows when we are docked? …
/// Also the jupiter brake re-appears after I click I'll fly by hand."</i> And on the card's own words —
/// "the aerobrake commits the ship to 0 passes (≈0 p saved)" — an offer to do nothing in exchange for
/// nothing.</para>
///
/// <para><see cref="Core.Tests"/> holds the three laws as pure predicates. THESE are the claims Core cannot
/// make: that the client hands the law the right facts about the world. The geometry is the owner's own —
/// a berth 850,000 km out from Jupiter, well inside a Hill sphere of 5.3e10 m, riding the station's rail,
/// which is fast relative to the planet and therefore looked exactly like a hot arrival forever.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class TheBrakeCardKnowsSheIsClampedTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol =
        new(() => ScenarioLoader.LoadFile(ScenarioPath("sol.json")));

    private static string ScenarioPath(string file)
    {
        // Walk up from the test binary to the repo root, where scenarios/ lives beside src/ and tests/.
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
         ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .SetValue(o, value);

    private static T Get<T>(object o, string field) =>
        (T)(o.GetType().GetField(field, Hidden)
            ?? throw new InvalidOperationException($"no field {field} on Map — this bench has drifted"))
        .GetValue(o)!;

    private static object? Invoke(object o, string method, params object?[] args) =>
        (o.GetType().GetMethod(method, Hidden)
         ?? throw new InvalidOperationException($"no method {method} on Map — this bench has drifted"))
        .Invoke(o, args);

    private static object? Property(object o, string name) =>
        (o.GetType().GetProperty(name, Hidden)
         ?? throw new InvalidOperationException($"no property {name} on Map — this bench has drifted"))
        .GetValue(o);

    /// <summary>
    /// The ship exactly where the owner's screenshot had her: at The Red Eye's berth, deep inside Jupiter's
    /// Hill sphere, moving with the station — i.e. at the station's orbital speed <i>about Jupiter</i>, which
    /// is thousands of metres a second and far above the clamp window. Whether she is CLAMPED is the one
    /// thing the caller varies.
    /// </summary>
    private static Pages.Map AtTheRedEye(bool clamped)
    {
        var map = new Pages.Map();
        FieldInfo pending = typeof(ComponentBase).GetField(
            "_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!;
        pending.SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));

        // The berth: co-moving with The Red Eye, three kilometres off it — the shared berth state (#269),
        // which is what a clamp actually is.
        ShipState berthed = BerthState.CoMoving(ephemeris, "red-eye", 0.0, BerthState.BerthOffsetMeters, 0.0);
        Set(map, "_ship", berthed);
        Set(map, "_dockedHavenId", clamped ? "red-eye" : null);

        // …and the brake armed at Jupiter, exactly as a long haul that landed hot would leave it.
        Set(map, "_brakeArrivalBodyId", "jupiter");
        Set(map, "_brakeQuotedPulses", 40);
        Set(map, "_brakeDestName", "Jupiter");
        Set(map, "_brakeGate", ArrivalBrake.Gate.Closed);
        Set(map, "_reactionMassPulses", 200);
        return map;
    }

    /// <summary>The premise every claim below rests on: from that berth, the ship really does read as
    /// "near Jupiter and far too fast for the clamp window". If this ever stops being true the tests below
    /// would pass for the wrong reason — a guard on a world that cannot tell pass from fail.</summary>
    [Fact]
    public void ThePremise_TheBerthReallyDoesLookLikeAHotArrivalAtJupiter()
    {
        Pages.Map map = AtTheRedEye(clamped: false);
        ICelestialEphemeris eph = Get<ICelestialEphemeris>(map, "_ephemeris");
        ShipState ship = Get<ShipState>(map, "_ship");
        CelestialBody jupiter = Array.Find(eph.Bodies.ToArray(), b => b.Id == "jupiter")!;
        CelestialBody sun = Array.Find(eph.Bodies.ToArray(), b => b.Id == "sun")!;

        double dist = (ship.Position - eph.Position("jupiter", 0)).Length;
        double hill = OrbitRule.HillRadius(jupiter, sun.Mu);
        double rel = (ship.Velocity - TransferMath.BodyVelocity(eph, "jupiter", 0)).Length;

        Assert.True(dist < hill, $"the berth is {dist:e2} m out and Jupiter's Hill sphere is {hill:e2} m — " +
                                 "if the berth were outside it, the window would already be shut for a " +
                                 "reason that has nothing to do with the clamp.");
        Assert.True(rel > LongHaul.InsertionTargetSpeed,
            $"the berth rides Jupiter at {rel:F0} m/s and the clamp window is " +
            $"{LongHaul.InsertionTargetSpeed:F0} m/s — the whole bug is that this reads as HOT.");
    }

    [Fact]
    public void CLAMPED_TheBrakeCardNeverAsks()
    {
        // #962 REGRESSION. RED CASE: hand ArrivalBrake.WindowOpen `clamped: false` in
        // Map.BrakeWindowOpen (or drop the parameter) and this asks, exactly as the screenshot did.
        Pages.Map map = AtTheRedEye(clamped: true);

        for (int frame = 0; frame < 200; frame++)
        {
            Invoke(map, "UpdateArrivalBrakeGate", 16.0 * frame);
            Assert.False(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking,
                "the brake card came up while the ship was clamped on at The Red Eye. She is not arriving; " +
                "the berth is holding her, and the burn the card offers would only fight the clamp.");
        }
    }

    [Fact]
    public void CASTOFF_TheSameGeometryDoesAsk()
    {
        // The guard is the CLAMP and nothing else. Cast off in the same place at the same speed and the
        // brake is genuinely owed again — which is what stops the fix above from being "never ask".
        Pages.Map map = AtTheRedEye(clamped: false);
        Invoke(map, "UpdateArrivalBrakeGate", 0.0);

        Assert.True(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking,
            "cast off and hot inside Jupiter's Hill sphere, the arrival brake must still ask — a fix that " +
            "silenced the card everywhere would be a worse bug than the one it replaced.");
    }

    [Fact]
    public void HOLD_IsAnAnswer_AndTheCardDoesNotComeBack()
    {
        // #962 REGRESSION, the owner's second sentence: "the jupiter brake re-appears after I click I'll
        // fly by hand." RED CASE: put ArrivalBrake.Snooze's re-raise back and this fails inside eight
        // seconds of frames.
        Pages.Map map = AtTheRedEye(clamped: false);
        Invoke(map, "UpdateArrivalBrakeGate", 0.0);
        Assert.True(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking, "the card never came up to be held.");

        Invoke(map, "DeclineArrivalBrake");
        Assert.False(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking, "Hold did not take the card down.");

        // Half a minute of frames with the window still wide open — far past the old 8 s nag.
        for (int frame = 1; frame <= 2_000; frame++)
        {
            Invoke(map, "UpdateArrivalBrakeGate", 16.0 * frame);
            Assert.False(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking,
                $"the brake card came back at frame {frame} ({16.0 * frame / 1000:F1} s after Hold). Hold is " +
                "an answer, not a snooze.");
        }

        // Nothing fired and nothing was billed — Hold leaves the manual state exactly as it was.
        Assert.Equal(200, Get<int>(map, "_reactionMassPulses"));
        Assert.False(Get<ArrivalBrake.Gate>(map, "_brakeGate").HasFired);
    }

    [Fact]
    public void HOLD_ThenARealNewArrival_AsksAfresh()
    {
        // Held is terminal for THIS arrival only. Clamp on (the window shuts, the gate resets), cast off
        // again, and the question comes back — otherwise "Hold once" would silence the brake for the run.
        Pages.Map map = AtTheRedEye(clamped: false);
        Invoke(map, "UpdateArrivalBrakeGate", 0.0);
        Invoke(map, "DeclineArrivalBrake");
        Invoke(map, "UpdateArrivalBrakeGate", 100.0);
        Assert.False(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking);

        Set(map, "_dockedHavenId", "red-eye");        // clamp on — the window shuts
        Invoke(map, "UpdateArrivalBrakeGate", 200.0);
        Assert.Equal(ArrivalBrake.Gate.Closed, Get<ArrivalBrake.Gate>(map, "_brakeGate"));

        Set(map, "_dockedHavenId", null);             // cast off into a fresh hot arrival
        Set(map, "_brakeArrivalBodyId", "jupiter");   // …which re-arms, as LongHaul's arrival does
        Invoke(map, "UpdateArrivalBrakeGate", 300.0);
        Assert.True(Get<ArrivalBrake.Gate>(map, "_brakeGate").Asking,
            "a genuinely new arrival must ask afresh — Held is spent on the arrival that earned it.");
    }

    [Fact]
    public void AEROBRAKE_AnArmWithNoFiledQuote_NeverSpeaksTheZeroPassOffer()
    {
        // #962 REGRESSION, the card's own words: "commits the ship to 0 passes (≈0 p saved)". The ask used
        // to read AerobrakeMenuQuote, which is keyed to whichever BODY MENU is open and is null the moment
        // that menu closes — so it spoke the null's defaults. RED CASE: point ArrivalBrakeAskText back at
        // AerobrakeMenuQuote and this finds "0 passes" on the card again.
        Pages.Map map = AtTheRedEye(clamped: false);
        Set(map, "_aerobrakeArmedBodyId", "jupiter");
        Set(map, "_aerobrakeArmedQuote", null);       // armed, but no trade on file

        Invoke(map, "UpdateArrivalBrakeGate", 0.0);
        string ask = (string)Invoke(map, "ArrivalBrakeAskText")!;

        Assert.DoesNotContain("0 passes", ask, StringComparison.Ordinal);
        Assert.DoesNotContain("≈0 p saved", ask, StringComparison.Ordinal);
        Assert.False((bool)Property(map, "BrakeIsAerobrake")!,
            "an arm with no filed quote must not claim to be an aerobrake offer — it falls back to the " +
            "propulsive brake, which is a real trade with a real bill.");
        Assert.Contains("40 p", ask, StringComparison.Ordinal); // the propulsive quote, spoken instead
    }

    [Fact]
    public void AEROBRAKE_AFiledTradeIsSpokenInFull()
    {
        // …and the trade the captain actually accepted is the one the card carries. Anything else is the
        // picture disagreeing with the sim, which is this repo's third named bug class.
        Pages.Map map = AtTheRedEye(clamped: false);
        Set(map, "_aerobrakeArmedBodyId", "jupiter");
        Set(map, "_aerobrakeArmedQuote", QuoteOf(passes: 6, saved: 11));

        Invoke(map, "UpdateArrivalBrakeGate", 0.0);
        string ask = (string)Invoke(map, "ArrivalBrakeAskText")!;

        Assert.True((bool)Property(map, "BrakeIsAerobrake")!);
        Assert.Contains("6 passes", ask, StringComparison.Ordinal);
        Assert.Contains("≈11 p saved", ask, StringComparison.Ordinal);
    }

    [Fact]
    public void AEROBRAKE_AFiledTradeWorthNothing_IsNotOffered()
    {
        // A quote that flies passes and saves nothing is not an offer. It must fall back to the propulsive
        // brake rather than asking the captain to commit the ship to the air for free.
        Pages.Map map = AtTheRedEye(clamped: false);
        Set(map, "_aerobrakeArmedBodyId", "jupiter");
        Set(map, "_aerobrakeArmedQuote", QuoteOf(passes: 4, saved: 0));

        Invoke(map, "UpdateArrivalBrakeGate", 0.0);

        Assert.False((bool)Property(map, "BrakeIsAerobrake")!);
        Assert.DoesNotContain("commit the pass?", (string)Invoke(map, "ArrivalBrakeAskText")!, StringComparison.Ordinal);
    }

    /// <summary>A quote carrying only the two numbers the card speaks — the rest of the physics is priced
    /// by <see cref="Aerobrake.Price"/> and irrelevant to what the ask says.</summary>
    private static Aerobrake.Quote QuoteOf(int passes, int saved) => new(
        Aerobrake.Outcome.SoloCapture, ArrivalVinf: 5_000, EntrySpeed: 40_000, FreeShedMps: 1_000,
        CaptureDeltaV: 2_000, BridgeMps: 0, PropulsivePulses: 40, AerobrakePulses: 40 - saved,
        PulsesSaved: saved, PassesNeeded: passes, TighteningPasses: passes, PeakG: 2.0,
        PeakDynamicPressurePa: 1_000, PriceBasisSpeed: 5_000);
}

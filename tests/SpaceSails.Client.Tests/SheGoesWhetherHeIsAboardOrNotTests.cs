using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using Xunit;

namespace SpaceSails.Client.Tests;

/// <summary>
/// #525 · <b>THE CASTAWAY, DRIVEN.</b> The issue's outcome table has two rows and the second one — <i>captain
/// clear in the shuttle → CASTAWAY</i> — has been filed as unbuilt, then as built, then as
/// <i>built-but-never-played</i>, across three separate readings of the source. Reading is how it got three
/// answers. This is the drive.
///
/// <para>Every guard below arms her own charges through the panel's own three verbs — the captain's word, the
/// crew's second key, both keys together — and then runs <c>OnTick</c> the way the browser runs it, at a real
/// frame clock, until the ninety seconds are spent. Nothing is written into <c>_shipChargesSeconds</c> and
/// nothing calls <c>SheGoes</c> by hand: the clock is the one the game keeps, and where it does not run, these
/// guards go red.</para>
///
/// <h3>What the drive found (and the source reading had not)</h3>
///
/// <para><c>AdvanceShipCharges</c> was called from ONE place — inside <c>TheWalkedViewOwnsThisFrame</c>. So the
/// overload only counted while the captain was on his feet. Sit down at the helm, which is the entire point of
/// arming them (somebody is closing on her), and the countdown stopped: the PA never called, zero never
/// arrived, and a captain could arm the charges, watch every hunter break off, and then simply never pay for
/// it. It is the same bug #523's own note in <c>AdvanceHerOwnClocks</c> says it was written to prevent —
/// <i>"ticking it only in deck mode was the first cut of this, and it would have made the whole system a
/// curiosity you could only see while parked"</i> — one field over.</para>
///
/// <para>Her overload is now spent where her other clocks are spent: on the ship, in every view, ahead of the
/// shuttle-run stop, because <b>the shuttle being away with him in it is one of the two ways this ending
/// happens</b> and a clock that stops when the boat leaves can never reach zero with the captain off her.</para>
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public sealed class SheGoesWhetherHeIsAboardOrNotTests
{
    private const BindingFlags Hidden =
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private const string Body = "luna";

    // ── ROW 2 · THE CASTAWAY ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE IS STANDING ON A MOON WHEN SHE GOES.</b> The charges are armed on her deck, the captain rides
    /// down to the regolith, and the frames run until the ninety seconds are gone.
    ///
    /// <para>Asserted off the page, not off a flag: the castaway card is up with its three authored lines and
    /// the boat that was painted for it; the captain is ALIVE (no busted encounter, no death cause); the hold
    /// is empty and the hot ledger laundered; the excursion is folded and he is berthed somewhere; and the
    /// frame that ended her asked the vault to write it down, so a reload cannot resurrect her.</para>
    ///
    /// <para><b>Proven RED</b> three ways. By making <c>CaptainWasAboardHer</c> return true — he dies on a
    /// moon a hundred thousand kilometres from the hull that killed him, and there is no card at all:</para>
    ///
    /// <code>
    /// Assert.Null() Failure: Value is not null
    /// Actual: BustedEncounter { BoliviaBeatIndex = 0, … }
    /// </code>
    ///
    /// <para>…by dropping <c>_collectors.Clear()</c> from the fold — <c>Assert.Empty() Failure: Collection was
    /// not empty · [Collector { … }, Collector { … }, Collector { … }]</c> — and by leaving the hold in her:
    /// <c>Assert.Empty() Failure · [["ore"] = 24]</c>.</para>
    /// </summary>
    [Fact]
    public void THE_CASTAWAY_HappensWithHimStandingOnAMoon()
    {
        Pages.Map map = Boot();
        ArmHerCharges(map);
        StandOnLuna(map);
        PutACollectorPartyOnTheGround(map);

        bool askedTheVault = RunUntilSheGoes(map);

        Assert.Null(Read(map, "_shipChargesSeconds"));
        Assert.Null(Read(map, "_busted"));

        object card = Read(map, "_shipEpitaph")
                      ?? throw new InvalidOperationException("the clock ran out and no card was ever raised.");
        Assert.Equal(ShipScuttle.CastawayLine, (string)Get(card, "Went")!);
        Assert.Equal(ShipScuttle.CastawaySurvivesLine, (string)Get(card, "Survives")!);
        Assert.Contains("berth", (string)Get(card, "Rescue")!, StringComparison.OrdinalIgnoreCase);

        // …and the picture that card carries is the one painted for it (#528/#915).
        Assert.Equal("art/castaway.jpg", ShipScuttle.CastawayArt);
        Assert.Contains("ShipScuttle.CastawayArt", TheCastawayMarkup(), StringComparison.Ordinal);

        // The hold went with her.
        Assert.Equal(0, (int)Read(map, "_cargoUnits")!);
        Assert.Equal(0, Convert.ToInt32(Read(map, "_cargoValue")));
        Assert.Empty((IDictionary)Read(map, "_cargoByClass")!);

        // The ground is gone and so is everybody who was on it with him — the same fold the shuttle's own
        // lift-off does (Map.Surface.cs), never a partial one.
        Assert.Null(Read(map, "_surface"));
        Assert.Empty((ICollection)Read(map, "_reevers")!);
        Assert.Empty((ICollection)Read(map, "_collectors")!);

        // AND THE WORLD A RELOAD WOULD COME BACK TO, built by the page's own BuildVault: her hold is not in
        // it, the stamp on what was in it is not in it, and the hull it describes is the one the insurance
        // handed over. This is the falsifiable half — restore any line of the loss and it goes red.
        var saved = (Vault)Invoke(map, "BuildVault", "", "")!;
        CargoSection inTheHold = saved.Cargo ?? throw new InvalidOperationException("the vault has no hold.");
        Assert.Empty(inTheHold.Hold);
        Assert.Empty(inTheHold.Hot);
        UpgradesSection hull = saved.Upgrades ?? throw new InvalidOperationException("the vault has no hull.");
        Assert.Equal((int)Read(map, "_holdLevel")!, hull.HoldLevel);
        Assert.Equal((int)Read(map, "_massLevel")!, hull.MassLevel);

        // …and the frame that ended her left the autosave dirty, so that world is actually written. (Belt
        // and braces: the wake's own clamp onto a berth dirties it too, so this half cannot go red alone.)
        Assert.True(askedTheVault, "the frame that ended her did not ask the vault for anything.");
    }

    /// <summary>
    /// <b>THE SHUTTLE IS AWAY WITH HIM IN IT.</b> The other half of <c>CaptainWasAboardHer</c>, and the half
    /// the owner's own objection was about: <i>"suppose the crew sets the countdown 90 seconds and evacuates
    /// with shuttle."</i>
    ///
    /// <para>Most of the ninety is spent on her own deck — a boarding run is a thirty-second window and the
    /// captain does not get to sit in the boat for a minute and a half — and then a real run goes out, off the
    /// page's own <c>LaunchShuttleRun</c>, at a selected, observed, authorized hull at point-blank range. The
    /// last seconds are crossed with the boat genuinely away, and the run is asserted to be still flying on the
    /// frame before zero, so the ending cannot be the deck's ending wearing this one's name.</para>
    ///
    /// <para><b>Proven RED</b> by putting the tick back where it was, inside the walked view: the seventy-five
    /// seconds on her own corridor still pass, the boat goes out, and then nothing moves at all until the run
    /// is recovered and she takes him with her —</para>
    ///
    /// <code>
    /// the boat was already home when the clock ran out, so this is the deck's ending under another name
    /// and the shuttle half of CaptainWasAboardHer is still untested.
    /// </code>
    /// </summary>
    [Fact]
    public void THE_SHUTTLE_AwayWithHimInItIsTheOtherWayClear()
    {
        Pages.Map map = Boot();
        ArmHerCharges(map);
        RunFrames(map, seconds: 75);                    // …spent walking her own corridor
        Assert.InRange((double)Read(map, "_shipChargesSeconds")!, 10, 20);

        PutHimInTheShuttle(map);
        bool flyingWhenSheWent = false;
        for (int i = 0; i < 400 && Read(map, "_shipChargesSeconds") is not null; i++)
        {
            flyingWhenSheWent = Read(map, "_shuttleRun") is not null;
            Frame(map);
        }

        Assert.True(flyingWhenSheWent,
                    "the boat was already home when the clock ran out, so this is the deck's ending under "
                    + "another name and the shuttle half of CaptainWasAboardHer is still untested.");
        Assert.Null(Read(map, "_busted"));
        Assert.NotNull(Read(map, "_shipEpitaph"));
        Assert.Null(Read(map, "_shuttleRun"));   // the boat has nothing to fly home to
    }

    // ── ROW 1 · SHE GOES WITH HIM (#651) ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>HE NEVER LEFT THE HELM.</b> The charges are armed, the captain sits down at the nav board — the map
    /// view, which is where a captain who has just told a boarder to back off actually sits — and the frames
    /// run. She goes with him, through the shared brain-backup death, under her own cause.
    ///
    /// <para>This is the guard the whole lane turned on. <b>Proven RED</b> on the shipping code before this
    /// lane, where the countdown was spent only inside the walked view:</para>
    ///
    /// <code>
    /// Assert.Equal() Failure: Values differ
    /// Expected: Scuttled
    /// Actual:   (null)
    /// </code>
    ///
    /// <para>…with <c>_shipChargesSeconds</c> still reading 90 after four minutes of frames. A captain could
    /// arm the charges, break off every pursuer, and then never pay for it by doing nothing at all.</para>
    /// </summary>
    [Fact]
    public void SHE_GoesWithHimWhenHeNeverLeftTheHelm()
    {
        Pages.Map map = Boot();
        ArmHerCharges(map);
        Set(map, "_deckMode", false);   // sat down at the nav board, which is not a walked view

        RunUntilSheGoes(map);

        Assert.Null(Read(map, "_shipChargesSeconds"));
        Assert.Null(Read(map, "_shipEpitaph"));   // there is no castaway; there is a funeral

        object busted = Read(map, "_busted")
                        ?? throw new InvalidOperationException("the clock ran out and nobody died on her.");
        Assert.Equal(DeathCause.Scuttled, (DeathCause)Get(busted, "Cause")!);
        Assert.Equal(1, Convert.ToInt32(Read(map, "Warp")));
    }

    /// <summary>
    /// <b>THE CLOCK IS HERS, AND EVERY VIEW SPENDS IT AT THE SAME RATE.</b> The same arming, the same frame
    /// clock, in the three views the captain can be in — the helm, her own corridor, and a moon — and after
    /// the same number of seconds all three read the same number of seconds left.
    ///
    /// <para>The reason this is its own guard and not a corollary: the two above assert an OUTCOME, and an
    /// outcome can be reached by a clock that runs at a different rate in a different view (or in a burst on
    /// the frame the view changes). What is being pinned here is that the ninety seconds mean ninety seconds
    /// wherever the captain is standing, which is the only reading under which the PA's count is honest.</para>
    ///
    /// <para><b>Proven RED</b> the same way as the guard above — thirty seconds at the helm bought nothing at
    /// all, while the same thirty on her corridor and on a moon bought thirty:</para>
    ///
    /// <code>
    /// Assert.All() Failure: 1 out of 3 items in the collection did not pass.
    /// [0]: Item:  90
    ///      Error: Assert.InRange() Failure: Range: (59 - 61)  Actual: 90
    /// </code>
    /// </summary>
    [Fact]
    public void THE_CLOCK_RunsAtTheSameRateInEveryView()
    {
        var left = new List<double>();
        foreach (Action<Pages.Map> whereHeIs in new Action<Pages.Map>[]
        {
            m => Set(m, "_deckMode", false),   // the helm
            m => Set(m, "_deckMode", true),    // her own corridor
            StandOnLuna,                       // a moon
        })
        {
            Pages.Map map = Boot();
            ArmHerCharges(map);
            whereHeIs(map);
            RunFrames(map, seconds: 30);
            left.Add((double)Read(map, "_shipChargesSeconds")!);
        }

        Assert.All(left, l => Assert.InRange(l, Scuttle.OverloadSeconds - 31, Scuttle.OverloadSeconds - 29));
        Assert.True(left.Max() - left.Min() < 1.0,
                    "the same thirty seconds bought different amounts of her countdown depending on where the "
                    + $"captain was standing: {string.Join(", ", left.Select(l => l.ToString("0.0")))}. Her "
                    + "overload is a fact about the ship, not a service the current view provides.");
    }

    // ── AND NOBODY IS CHASING WHAT IS NOT THERE ───────────────────────────────────────────────────────

    /// <summary>
    /// <b>EVERY PURSUER LETS HER GO.</b> The deterrent breaks off whoever is watching at the moment the keys
    /// turn — that is the mechanic, and it is spent once per arming so the line is not said every frame. What
    /// it cannot cover is a hunter who arrives DURING the ninety seconds: he was never deterred, and on the
    /// old code he was still flying his intercept at a hull that no longer existed.
    ///
    /// <para>Both halves are driven here: one hunter on her at the arming (deterred, and told so), one who
    /// turns up forty seconds later (never told anything), and at zero neither of them is chasing anybody.</para>
    ///
    /// <para><b>Proven RED</b> by deleting the sweep from <c>SheGoesWithoutHim</c>: the latecomer is still on
    /// the intercept after the ship he wanted stopped existing.</para>
    /// </summary>
    [Fact]
    public void EVERY_PursuerLetsHerGoWhenSheStopsExisting()
    {
        Pages.Map map = Boot();
        PutAHunterOnHer(map, "WATCHING");
        ArmHerCharges(map);
        StandOnLuna(map);

        RunFrames(map, seconds: 10);
        var deterred = (IList)Read(map, "_hunters")!;
        Assert.All(deterred.Cast<object>(), h => Assert.True((bool)Get(h, "BrokenOff")!));

        PutAHunterOnHer(map, "LATECOMER");
        Assert.Contains(((IList)Read(map, "_hunters")!).Cast<object>(),
                        h => !(bool)Get(h, "BrokenOff")! && (string)Get(h, "Callsign")! == "LATECOMER");

        RunUntilSheGoes(map);

        Assert.NotNull(Read(map, "_shipEpitaph"));
        foreach (object hunter in ((IList)Read(map, "_hunters")!).Cast<object>())
        {
            Assert.True((bool)Get(hunter, "BrokenOff")!,
                        $"{Get(hunter, "Callsign")} is still flying an intercept at a hull that stopped "
                        + "existing. The whole reframe of this issue is that the prize evaporates.");
        }
    }

    // ── THE WORLD ─────────────────────────────────────────────────────────────────────────────────────

    private static readonly Lazy<SpaceSails.Contracts.ScenarioDefinition> Sol = new(() =>
        ScenarioLoader.LoadFile(Path.Combine(RepoRoot(), "scenarios", "sol.json")));

    /// <summary>A live component over the shipping scenario, walking her own deck — the posture the charge
    /// panel is reached from, because it is a console on her deck plan and nowhere else.</summary>
    private static Pages.Map Boot()
    {
        var map = new Pages.Map();
        new ARendererThatDrawsNothing().Attach(map);
        typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(map, true);

        ICelestialEphemeris ephemeris = CircularOrbitEphemeris.FromScenario(Sol.Value);
        Set(map, "_scenarioName", Sol.Value.Name);
        Set(map, "_ephemeris", ephemeris);
        Set(map, "_simulator", new Simulator(ephemeris, timeStepSeconds: 1.0));
        Set(map, "_npcSimulator", new Simulator(ephemeris, TrafficSchedule.NpcTimeStep));
        Set(map, "_ship", Invoke(map, "InitializeShipState")!);

        // The map frame paints into the REAL command buffer (nothing else can be assigned to a field typed
        // to the sealed CanvasRenderer); the walked view and the boat get a pen that stays in managed code.
        // Not decoration: ShuttleFlightView.Draw is inside the frame's OWN try/catch, and a renderer that
        // throws there is read as a shuttle fault and RECOVERS THE BOAT — a world in which no run can be
        // flown for longer than one frame, and every question about a shuttle answers itself.
        Set(map, "_renderer", new CanvasRenderer("castaway-canvas"));
        var pen = new APenThatDrawsNothing();
        Set(map, "_deckView", new DeckView(pen));
        Set(map, "_shuttleView", new ShuttleFlightView(pen));
        Set(map, "_deckMode", true);
        Set(map, "Warp", 1);
        Invoke(map, "ReprojectTrajectory");

        // Something in the hold, so "the hold went with her" is an assertion a world can fail.
        Set(map, "_cargoUnits", 24);
        Set(map, "_cargoValue", 91_000);
        ((IDictionary)Read(map, "_cargoByClass")!)["ore"] = 24;

        // …and it is hot, because "one of the few honest ways to make evidence disappear" is a sentence in
        // the ending's own note and has to be a thing a world can fail to do.
        ((HotCargoLedger)Read(map, "_hotCargo")!).Stamp("ore", 24, heatAtTheft: 3);
        return map;
    }

    /// <summary>The captain's word, the crew's second key, both keys together — the panel's own three verbs,
    /// in the order the panel makes the player press them. Nothing writes the clock.</summary>
    private static void ArmHerCharges(Pages.Map map)
    {
        Invoke(map, "OpenShipScuttlePanel");
        Invoke(map, "GiveTheWordAgainstHer");
        Assert.True((bool)Read(map, "_shipScuttleWordGiven")!, "the captain's own word did not take.");

        Invoke(map, "AskTheCrewForTheSecondKey");
        Assert.NotEqual(ShipScuttle.SecondKey.Refused,
                        (ShipScuttle.SecondKey)Read(map, "_shipScuttleSecondKey")!);

        Invoke(map, "TurnBothKeys");
        Assert.Equal(Scuttle.OverloadSeconds, (double)Read(map, "_shipChargesSeconds")!);
        Invoke(map, "CloseShipScuttlePanel");
    }

    /// <summary>Put the captain down on the regolith the way <c>RebuildSurfaceDeck</c> builds it — the one
    /// posture that already reached this ending before this lane, and the reason it was believed reachable.</summary>
    private static void StandOnLuna(Pages.Map map)
    {
        Type exType = typeof(Pages.Map).GetNestedType("SurfaceExcursion", Hidden)!;
        Type stopType = typeof(Pages.Map).GetNestedType("ShuttleStop", Hidden)!;
        object ex = Activator.CreateInstance(exType, nonPublic: true)!;
        object stop = Activator.CreateInstance(stopType,
            new CelestialBody(Body, Body, "sol", 1, 1, 1, 1, 0), 0.0, 0.0, false, true, false)!;

        exType.GetProperty("Stop")!.SetValue(ex, stop);
        exType.GetProperty("RestoreHavenId")!.SetValue(ex, null);
        exType.GetProperty("Site")!.SetValue(ex,
            new LandingSite(0, LandingSiteKind.WildPlain, "The Wild Plain", "", ""));
        exType.GetProperty("Floor")!.SetValue(ex, 0);

        // The deep keeps its hands to itself for the ninety seconds this lane is about. Not a convenience:
        // an Old One reaching the captain is its OWN death (DeathCause.Reevers), and a world where the
        // regolith kills him before his own charges do cannot tell this guard's pass from its fail.
        exType.GetProperty("TideNextGap")!.SetValue(ex, 1e9);
        exType.GetProperty("LandedAtMs")!.SetValue(ex, 0.0);

        Set(map, "_surface", ex);
        Set(map, "_deckMode", true);
        Set(map, "_avatarX", (double)MoonSurface.SpawnX);
        Set(map, "_avatarY", MoonSurface.SpawnY);
        Invoke(map, "RebuildSurfaceDeck");
    }

    /// <summary>Launch the boat, off the page's own launcher, at a live target with the capture window open —
    /// so the run keeps flying instead of being recovered on the next frame.</summary>
    private static void PutHimInTheShuttle(Pages.Map map)
    {
        Set(map, "_deckMode", false);

        ICelestialEphemeris eph = (ICelestialEphemeris)Read(map, "_ephemeris")!;
        NpcShip hull = TrafficSchedule.Generate(eph, seed: 42, count: 1)[0];
        Type stateType = typeof(Pages.Map).GetNestedType("NpcState", Hidden)!;
        object prey = Activator.CreateInstance(stateType, nonPublic: true)!;
        stateType.GetField("Ship", Hidden)!.SetValue(prey, hull);
        stateType.GetField("State", Hidden)!.SetValue(prey, (ShipState)Read(map, "_ship")!);
        stateType.GetField("Active", Hidden)!.SetValue(prey, true);
        stateType.GetField("CurrentlyObserved", Hidden)!.SetValue(prey, true);

        Array roster = Array.CreateInstance(stateType, 1);
        roster.SetValue(prey, 0);
        Set(map, "_npcStates", roster);

        // The window the run flies inside, held open the way the game holds it open: a selected, observed
        // hull at point-blank range that the captain has said the word over. Without the word this is an
        // OPPORTUNITY, the window shuts on the first frame and the boat is recovered — which is a world in
        // which nothing about a shuttle can be asked.
        Set(map, "_selectedTargetId", hull.Id);
        Set(map, "_plunderAuthorizedTargetId", hull.Id);

        Invoke(map, "LaunchShuttleRun", prey);
        Assert.NotNull(Read(map, "_shuttleRun"));
    }

    /// <summary>A collector party on the ground beside him — the writ that followed his heat down (#583).
    /// Left un-landed on purpose: a working party walks at the captain and catching him is its own death,
    /// which would decide this guard for a reason that has nothing to do with her charges. What is being
    /// pinned is that the ending folds the excursion COMPLETELY, exactly as a lift-off does.</summary>
    private static void PutACollectorPartyOnTheGround(Pages.Map map)
    {
        Type collector = typeof(Pages.Map).GetNestedType("Collector", Hidden)!;
        IList party = (IList)Read(map, "_collectors")!;
        for (int i = 0; i < 3; i++)
        {
            object one = Activator.CreateInstance(collector, nonPublic: true)!;
            collector.GetField("X", Hidden)!.SetValue(one, MoonSurface.SpawnX + 20.0 + i);
            collector.GetField("Y", Hidden)!.SetValue(one, MoonSurface.SpawnY);
            party.Add(one);
        }

        Assert.NotEmpty(party);
    }

    /// <summary>One heat-hunter, flying her own intercept — built the way the roster holds them, off the
    /// player's own state, so nothing about it is a special case.</summary>
    private static void PutAHunterOnHer(Pages.Map map, string callsign)
    {
        var ship = (ShipState)Read(map, "_ship")!;
        ((IList)Read(map, "_hunters")!).Add(new HunterState(
            Id: callsign.ToLowerInvariant(),
            Callsign: callsign,
            OriginBodyId: Body,
            SpawnedAtSimTime: (double)Read(map, "SimTime")!,
            ActivationSimTime: 0,
            State: ship,
            CaughtPlayer: false,
            BrokenOff: false));
    }

    // ── THE FRAME ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Run frames until she goes, and hand back whether the frame that ended her asked the vault to
    /// write it down. Read on that frame and never later, because the NEXT frame's flush clears the flag.</summary>
    private static bool RunUntilSheGoes(Pages.Map map)
    {
        for (int i = 0; i < 4000; i++)
        {
            Frame(map);
            if (Read(map, "_shipChargesSeconds") is null)
            {
                return (bool)Read(map, "_autosaveDirty")!;
            }
        }

        throw new InvalidOperationException(
            "four hundred seconds of frames and her ninety-second overload never reached zero — it reads "
            + $"{Read(map, "_shipChargesSeconds") ?? "null"}. The clock is not being spent in this view.");
    }

    private static void RunFrames(Pages.Map map, double seconds)
    {
        for (int i = 0; i < (int)(seconds / FrameSeconds); i++)
        {
            Frame(map);
        }
    }

    private const double FrameSeconds = 0.1;

    /// <summary>One frame, through the page's own <c>OnTick</c>, on a real frame clock.</summary>
    private static void Frame(Pages.Map map)
    {
        double at = Convert.ToDouble(Read(map, "_lastTimestampMs") ?? 0.0) + FrameSeconds * 1000;
        try
        {
            Invoke(map, "OnTick", at);
        }
        catch (PlatformNotSupportedException)
        {
            // The canvas flush — the one line of the frame that crosses into JavaScript, and the same seam
            // EveryFrameLeavesTheSameFingerprintTests stops at. Everything this lane reads has already run.
        }
    }

    // ── PLUMBING ──────────────────────────────────────────────────────────────────────────────────────

    private static string TheCastawayMarkup() =>
        MapMarkup.Read(Path.Combine(RepoRoot(), "src", "SpaceSails.Client", "Pages", "Map.razor"));

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null)
        {
            if (Directory.Exists(Path.Combine(at.FullName, "src", "SpaceSails.Client")))
            {
                return at.FullName;
            }
            at = at.Parent;
        }
        throw new InvalidOperationException("could not find the repository root from the test assembly.");
    }

    private static object? Get(object owner, string name) =>
        owner.GetType().GetProperty(name, Hidden)?.GetValue(owner)
        ?? owner.GetType().GetField(name, Hidden)?.GetValue(owner);

    private static object? Read(Pages.Map map, string name) =>
        typeof(Pages.Map).GetField(name, Hidden)?.GetValue(map)
        ?? typeof(Pages.Map).GetProperty(name, Hidden)?.GetValue(map);

    private static void Set(Pages.Map map, string name, object? value)
    {
        FieldInfo? field = typeof(Pages.Map).GetField(name, Hidden);
        if (field is not null)
        {
            field.SetValue(map, value);
            return;
        }
        typeof(Pages.Map).GetProperty(name, Hidden)!.SetValue(map, value);
    }

    private static object? Invoke(Pages.Map map, string name, params object?[] args)
    {
        try
        {
            return typeof(Pages.Map).GetMethod(name, Hidden)!.Invoke(map, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>A renderer that records nothing and crosses into no JavaScript — the walked view's and the
    /// boat's canvas, so a frame in either can run to its end.</summary>
    private sealed class APenThatDrawsNothing : IRenderer
    {
        public void BeginFrame(int widthPx, int heightPx, RgbaColor background) { }

        public void DrawCircle(float x, float y, float r, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawPolyline(ReadOnlySpan<float> pointsXY, RgbaColor stroke, float w = 1f) { }

        public void DrawPolygon(ReadOnlySpan<float> pointsXY, RgbaColor? fill, RgbaColor stroke, float w = 1f) { }

        public void DrawText(float x, float y, string text, RgbaColor color,
            string font = "12px sans-serif", TextAlign align = TextAlign.Left) { }

        public int RegisterImage(string url) => 0;

        public void DrawImage(int imageId, float x, float y, float w, float h, float alpha = 1f) { }

        public void DrawImageSlice(int imageId, float sx, float sy, float sw, float sh,
            float dx, float dy, float dw, float dh, float alpha = 1f) { }

        public void EndFrame() { }
    }

#pragma warning disable BL0006 // the framework's own seam: a component needs a renderer to have a dispatcher
    private sealed class ARendererThatDrawsNothing : Microsoft.AspNetCore.Components.RenderTree.Renderer
    {
        public ARendererThatDrawsNothing()
            : base(NoServices.Instance, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance) { }

        public override Dispatcher Dispatcher { get; } = new RightHere();

        public void Attach(IComponent component) => AssignRootComponentId(component);

        protected override void HandleException(Exception exception) =>
            throw new InvalidOperationException("the frame threw inside the renderer", exception);

        protected override System.Threading.Tasks.Task UpdateDisplayAsync(
            in Microsoft.AspNetCore.Components.RenderTree.RenderBatch batch) =>
            System.Threading.Tasks.Task.CompletedTask;

        private sealed class RightHere : Dispatcher
        {
            public override bool CheckAccess() => true;

            public override System.Threading.Tasks.Task InvokeAsync(Action workItem)
            {
                workItem();
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public override System.Threading.Tasks.Task InvokeAsync(Func<System.Threading.Tasks.Task> workItem) =>
                workItem();

            public override System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem) =>
                System.Threading.Tasks.Task.FromResult(workItem());

            public override System.Threading.Tasks.Task<TResult> InvokeAsync<TResult>(
                Func<System.Threading.Tasks.Task<TResult>> workItem) => workItem();
        }

        private sealed class NoServices : IServiceProvider
        {
            public static readonly NoServices Instance = new();

            public object? GetService(Type serviceType) => null;
        }
    }
#pragma warning restore BL0006
}

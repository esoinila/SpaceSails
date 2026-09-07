using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — BootTheWorldAsync, as its own stages (#870 lane 7a): the one pass that reads every ?query and builds the world behind the loading door.
public partial class Map
{

    /// <summary>#870 lane 7a · EVERYTHING THE QUERY STRING SAID, in one place.
    ///
    /// <para>These were thirty locals at the top of a 1,656-line method. They are a holder now because
    /// the readers that fill them and the stages that spend them are separate members — and because
    /// naming them is the only way a guard can pin what the parse ANSWERED (see
    /// <c>TheBootReadsTheSameQueryTests</c>): most of these are consumed after the boot’s last
    /// browser gate, where no test can follow the boot itself.</para>
    ///
    /// <para>Mutable and passed around on purpose. Several of the stages below overwrite
    /// <see cref="DockCheat"/> with the berth their own rock co-orbits, exactly as the one method did,
    /// and the LAST write is the berth the boot clamps onto.</para>
    /// </summary>
    private sealed class BootQuery
    {
        public string ScenarioName = "sol";
        public string? StartId;
        public string? DockCheat; // /map?dock=<haven-id>: boot already clamped onto ANY dockable haven (#288)
        public int? FuelCheat; // /map?fuel=N: boot with N reaction-mass pulses in the tank (#288)
        public int? CreditsCheat; // /map?credits=N: boot with N credits in the purse (#288)
        public string? FetchCheat;
        public string? CrackCheat;
        public string? TipCheat;
        public string? HoardCheat;
        public string? TargetCheat; // #997 wave 10 /map?target=<contact-id>|collector: point the tactical UI at a contact so her dossier is up at boot
        public string? DestCheat; // #956 /map?dest=<body-id>: boot with the NAV DESTINATION already set, so Follow dest is reachable from a URL (?target= points at a CONTACT — a different question)
        public string? SlingCheat;
        public string? SkimCheat;
        public string? BackroomCheat;
        public double? SimHoursCheat;
        public bool EllipseCheat; // /map?ellipse=1 injects a visibly eccentric demo body (Kepler rails, PR-B)
        public string? ExpeditionCheat; // #370 /map?expedition=1|mining: spawn an away-team gig accepted + its site in shuttle range at the berth
        public string? DeflectionCheat; // #394 /map?deflection=1|C|S|M: spawn the deflection gig accepted, rock inbound, ship docked at Ringside
        public bool WreckCheat; // #488 /map?wreck=1: spawn a derelict in shuttle range — board her, read her, then file or strip
        public Derelict.WreckCause? WreckCauseCheat; // #488 /map?wreck=<cause>: board a wreck that died THAT way
        public bool SecretlabDeep; // #592 /map?secretlab=deep: the rock whose site hides a band
        public bool SecretlabCheat; // #409 /map?secretlab=1: spawn a landable rock in shuttle range that hides a Vantar lab, door pre-revealed
        public string? KaamosCheat; // #411 /map?kaamos=N|all: assemble N KAAMOS fragments (or all) so the readout + reach notice are testable; ?kaamos=pod|holder instead SEATS the rare find so it can be EARNED
        public bool BondCheat; // #429 /map?bond=1: dock at a bar with strangers + force the next ambient scare to bond (the cognac beat)
        public bool OracleCheat; // #428 /map?oracle=1: seat the station oracle at whatever bar you dock at (she's a coin-flip fixture otherwise)
        public bool AshoreCheat; // #428 /map?ashore=1: boot docked AND already standing in the bar — the ship→tube→hall walk already walked
        public int? NerveCheat; // #428 /map?nerve=N: seed the nerve gauge at N of NervePips.MaxPips whole pips at boot
        public string? NebulaCheat; // #422 /map?nebula=N|all: assemble N NEBULA fragments (or all) so the readout + truth notice are testable; ?nebula=adjuster instead SEATS the rare bar contact so the tell can be EARNED
        public bool ConvergeCheat; // #422 /map?converge=1: seed enough of BOTH arcs to fire THE CONVERGENCE for a one-URL smoke test
        public DeathCause? DeathCheat; // #621 /map?death=<cause>: stage the REAL death at boot; the world you booted into decides the PLACE
        public readonly List<string> RevealCheats = new List<string>(); // /map?reveal=<bodyId> (repeatable): chart a hidden body at boot
        public bool TableSceneCheat; // #746 /map?tablescene=1: boot the B1 canteen with the table scene in reach
        public bool OldCrewCheat; // #973 L5a /map?oldcrew=1: boot ashore with the four shipmates working THIS berth and one captain already buried, so "you look different" can be played
        public string? CrewCheat; // #663 /map?crew=petition: boot holding the voyage the crew send a deputation over — bodies left on a rock, and an honest ship that is poor because of it
    }

    /// <summary>Read the URL once, key by key. Each reader answers TRUE when the pair was its own and
    /// <c>||</c> stops the chain there — which is exactly what the <c>else if</c> ladder this replaces
    /// did, one seam at a time.
    ///
    /// <para><b>What IS behaviour here, and what is not.</b> The chain's ORDER is not: all sixty-nine
    /// <c>?key=</c> prefixes the eleven readers between them claim are distinct, and not one is a prefix
    /// of another, so no pair can be claimed twice and no reader can be starved by the one above it
    /// (measured on the tree, quoted in #870 lane 7a's PR body). The short-circuit is kept anyway,
    /// because the day somebody adds <c>?dockside=</c> under a reader below <c>?dock=</c> is the day it
    /// starts mattering, and the ladder it replaces would have behaved the same way.</para>
    ///
    /// <para>What IS behaviour is the IMPLICATIONS inside a reader — <c>?found=</c> turns
    /// <c>?secretlab=</c> on, <c>?threads=</c> turns <c>?spread=</c> on which turns <c>?tablescene=</c>
    /// on — and the order of the PAIRS in the URL, since several keys write the same field and the last
    /// write is the one that stands. Both are pinned by <c>TheBootReadsTheSameQueryTests</c>: dropping a
    /// single one of those implication lines reddens it (and the world sweep next door) on exactly the
    /// URLs that key steers, and nothing else.</para></summary>
    private BootQuery ReadEveryQueryKey(Uri uri)
    {
        // #875 · /map?autowalk=1 IS RETIRED — still parsed, deliberately ignored. #729 built click-to-walk
        // behind this flag "until the owner rules on always-on"; the ruling landed on 2026-08-15 ("click to
        // walk should always be on when the arrows for walking are active also. The two should be linked as
        // alternative UI methods for walking"), so a deck click is a CONTROL of the game now and there is
        // nothing left for a URL to switch on. The parse survives as a NO-OP ALIAS so that every old dev
        // URL, every line of the testing guide and the UiGate's own boot still resolve to a world — and the
        // answer goes nowhere, because the only gate left is Map.Deck's TheCaptainsLegsAreTheirOwn, which
        // no query string can reach.
        _ = AutoWalk.EnabledIn(uri.Query);

        var q = new BootQuery();
        foreach (string pair in uri.Query.TrimStart('?').Split('&'))
        {
            _ = ReadTheWorldAndTheJobs(pair, q)
                || ReadTheBodiesACheatSpawns(pair, q)
                || ReadTheClocksAndTheDeath(pair, q)
                || ReadWhatTheSiteIsHiding(pair, q)
                || ReadTheCanteenAndItsTops(pair, q)
                || ReadTheRestrictedFloor(pair, q)
                || ReadTheCounterAndItsStools(pair, q)
                || ReadTheCorridorAndThePark(pair, q)
                || ReadTheRoomsOwnDice(pair, q)
                || ReadWhereTheLandingGoes(pair, q)
                || ReadTheLongArcsAndTheBar(pair, q);
        }

        return q;
    }

    /// <summary>Four cheats need somewhere to HAPPEN — a scare with no room of strangers, an oracle with
    /// no bar to haunt, an ashore boot with nothing to be ashore in, a death with no place to have
    /// happened. Each defaults a berth; anything the caller asked for still wins.</summary>
    private void DefaultABerthForTheCheatsThatNeedOne(BootQuery q)
    {
        if (q.BondCheat)
        {
            // Arm the forced bond and make sure we boot INTO a bar (a scare on the bare ship deck has no room
            // of strangers). Default to The Space Bar; any ?dock=<id> the caller passed wins.
            _bondForce = true;
            q.DockCheat ??= "the-space-bar";
        }

        if (q.OracleCheat)
        {
            // #428 · Arm her seat BEFORE any deck is welded (SetDeckForDock reads _oracleForce and it rides
            // the deck cache key), and make sure we boot INTO a bar — an oracle with no bar to haunt is the
            // same non-scene as a scare with no room of strangers. Default The Space Bar; any ?dock= wins.
            _oracleForce = true;
            q.DockCheat ??= "the-space-bar";
        }

        // #428 · An ashore boot needs a bar to be ashore IN. Same idiom as the bond and the oracle above —
        // default a berth with a walkable interior — but guarded the way #621's death default is: `?dock=`
        // is read before `?start=` below, so defaulting one unconditionally would quietly outrank a
        // `?start=` the caller did pass. Anything the caller asked for still wins.
        if (q.AshoreCheat && q.DockCheat is null && q.StartId is null)
        {
            q.DockCheat = "the-space-bar";
        }

        // #621 · A death needs somewhere to have happened. Without a start this boot ends at the front
        // door, and the death card would open OVER the picker — a modal on top of a menu, which is not a
        // scene anybody can read. Same idiom as the bond above: default a berth, and any ?dock= / ?start=
        // the caller passed still wins. Only for a death staged on her DECK; a landing cheat brings its
        // own ground and its own berth with it.
        // (…and only when NOTHING else chose a start: `?dock=` is read before `?start=` below, so
        // defaulting one unconditionally would quietly outrank a `?start=` the caller did pass.)
        // #973 L5a · The old crew need a bar to be met in and a captain in the ground to be surprised by.
        // The berth is defaulted the same way the bond's and the oracle's are, and being ASHORE is implied:
        // every one of them works behind a counter of some kind, and the walk in is not what is being tested.
        if (q.OldCrewCheat)
        {
            q.AshoreCheat = true;
            q.DockCheat ??= "the-space-bar";
        }

        if (q.DeathCheat is not null && !_landCheat && q.DockCheat is null && q.StartId is null)
        {
            q.DockCheat = "the-tilt";
        }
    }

    /// <summary>#310 · The front door, raised while the reactor is still warming.</summary>
    private void RaiseTheFrontDoorWhileTheReactorWarms(BootQuery q)
    {
        // #310 honest boot state: if this boot will end at the load view (no direct start/dock cheat),
        // raise the front door NOW in its "warming the reactor" state, so the WASM warm-up never reads as
        // a broken, click-eating menu. It flips to the live slots once the front door OPENS — which,
        // since #161, is stages before the world behind it is finished (see OpenTheFrontDoor).
        if (q.DockCheat is null && q.StartId is null && q.SlingCheat is null && q.SkimCheat is null)
        {
            _showStartPicker = true;
            StateHasChanged();
        }
    }

    /// <summary>
    /// #161 · THE FRONT DOOR OPENS BEFORE THE WORLD BEHIND IT IS BUILT.
    ///
    /// <para>Owner, 2026-07-16: <i>"we should find a way to load the game without causing the
    /// timeout-warnings in the browser… or doing it stages perhaps"</i>, and the vault-era follow-up:
    /// <i>"stage the picker EARLY — the scenario list and the vault header are tiny — so Continue is
    /// clickable while the heavy assemblies stream; perceived boot time is picker time."</i></para>
    ///
    /// <para>The measurement (the phase log this lane added, quoted in the PR body) says exactly what was
    /// in the way: fourteen of the boot's fifteen seconds are <c>TrafficSchedule.Generate</c>, and the
    /// door was dark for every one of them because it waited on <c>_worldReady</c> — a flag that means
    /// "the rAF loop is running", which the front door has never needed. What the door DOES need is two
    /// things, and both are already in hand by this line: the berth roster (the ephemeris, five
    /// milliseconds ago) and the vault (localStorage, through the renderer module — a browser read of a
    /// few kilobytes). So they are done here, and the door goes live thirteen seconds early.</para>
    ///
    /// <para><b>Why this is the only browser work that moved forward, and why it is invisible off one.</b>
    /// Everything below is a BROWSER line — <c>JSHost.ImportAsync</c>, then localStorage through it — so
    /// off a browser it is the no-op <c>RendererInterop.PlayCue</c> is (#837). That matters more than
    /// tidiness: the boot's fingerprint sweep (<c>TheBootBuildsTheSameWorldTests</c>) runs off-browser and
    /// its horizon is the FIRST browser-only call, so a module import that ran unguarded here would move
    /// that horizon eleven stages earlier and unpin the whole world. Guarded, the horizon does not move
    /// and all eighty-two hashes stand unchanged.</para>
    ///
    /// <para><b>And it is synchronous on purpose.</b> There is no await between the ephemeris build and
    /// this peek, so no render can land in between — which is what lets the door's own "am I live?"
    /// question be simply <c>_ephemeris is not null</c> (<see cref="FrontDoorReady"/>) instead of another
    /// flag on this page. A door that painted its berths one frame before its Continue would be a worse
    /// front door than the one that waited.</para>
    /// </summary>
    private async Task OpenTheFrontDoorAsync(BootQuery q, CancellationToken abandoned)
    {
        // The two starts that reach the picker branch of ApplyTheStartPoint but never SHOW the door
        // (?sling= / ?skim= suppress it again below) still want their vault peeked — it is where
        // "the tutorial was already played" comes from — so the condition here is the peek's own, not
        // the door's. Off a browser there is no localStorage to peek and no module to reach it through.
        if (q.DockCheat is not null || q.StartId is not null || !OperatingSystem.IsBrowser())
        {
            return;
        }

        await LoadTheBrowsersRendererModuleAsync();
        PeekSavedVault(); // #225: surface a "Continue — docked at <haven>" lead if a vault exists.

        // …and hand the frame back so the door the captain is looking at actually becomes the live one
        // before the fourteen seconds of route planning start. Only when there IS a door to paint.
        if (_showStartPicker)
        {
            await BootPhaseAsync("the sky is still being plotted — the door above is already open", abandoned);
        }
    }

    /// <summary>#161 · <c>renderer.js</c>, imported at the top of the boot instead of eleven stages down.
    ///
    /// <para>It is how this page reaches localStorage, and the front door cannot say "Continue — docked at
    /// &lt;haven&gt;" without it. The import itself is milliseconds (it is a module fetch the browser has
    /// usually already cached), it is idempotent — <c>EnsureModuleLoadedAsync</c> is a
    /// <c>??=</c> over one task — and the renderer stage below still calls it, so nothing downstream
    /// depends on this having happened.</para>
    ///
    /// <para>Browser-guarded for the reason spelled out on <see cref="OpenTheFrontDoorAsync"/>: this is the
    /// call that would otherwise become the off-browser fingerprint's new horizon.</para></summary>
    private static Task LoadTheBrowsersRendererModuleAsync() =>
        OperatingSystem.IsBrowser() ? RendererInterop.EnsureModuleLoadedAsync() : Task.CompletedTask;

    /// <summary>#870 lane 7a · THE BOOT, AS ITS OWN STAGES. Every line below is a stage of the one
    /// pass this used to be, in the one order it has always run: read the URL, default a berth for the
    /// cheats that need one, raise the front door, fetch the scenario, hang the cheats’ bodies off it,
    /// build the ephemeris and everything fed by it, lay the ship down, plan the traffic, point the
    /// camera, wire the renderer — and only then apply the start and the cheats that need a live world
    /// under them. Nothing here reorders a side effect; the world every URL builds is pinned, hash by
    /// hash, by <c>TheBootBuildsTheSameWorldTests</c>.
    ///
    /// <para>#161 · TWO STAGES MOVED FORWARD, and both of them are browser-only so the fingerprint's
    /// horizon (and therefore every one of its hashes) is where it was: the renderer MODULE is imported
    /// at the top — it is how localStorage is reached, and it costs milliseconds — and the vault is
    /// peeked the instant the ephemeris exists, which is what OPENS the front door. Everything after
    /// that line is the world behind a door the captain can already read, choose from, and walk
    /// through.</para></summary>
    private async Task BootTheWorldAsync(CancellationToken abandoned)
    {
        StartTheBootClock();

        var uri = new Uri(Navigation.Uri);
        BootQuery q = ReadEveryQueryKey(uri);
        DefaultABerthForTheCheatsThatNeedOne(q);
        RaiseTheFrontDoorWhileTheReactorWarms(q);
        SayTheBootStageCost("the URL read");

        ScenarioDefinition scenario = await FetchTheScenarioAsync(q, abandoned);
        scenario = AppendTheBodiesTheCheatsAskFor(scenario, q);
        SayTheBootStageCost("the scenario fetched and parsed");

        BuildTheEphemerisAndAnnounceTheBerths(scenario);
        await OpenTheFrontDoorAsync(q, abandoned);
        SayTheBootStageCost("the ephemeris built and the vault read — THE FRONT DOOR IS LIVE");

        BuildWhatTheEphemerisFeeds(scenario);
        SayTheBootStageCost("the mission catalog, the plasma and the two simulators");

        LayTheShipDownWithHerHistory();
        SayTheBootStageCost("the ship laid down and her arc projected");

        await PlanTheTrafficAsync(abandoned);

        PointTheCameraAtHer();
        SayTheBootStageCost("the camera pointed");

        await WireTheRendererToTheBrowserAsync(abandoned);
        SayTheBootStageCost("the renderer wired to the browser");

        ApplyTheStartPoint(q);
        StandTheCaptainWhereTheCheatsAsk(q);
        SeedTheArcsAndTheJobs(q);
        SeedTheApproachesAndThePurse(q);
        SayTheBootStageCost("the start point applied and the cheats seeded");

        await HandThePageToThePlayerAsync(abandoned);
        SayTheBootStageCost("the page handed to the player");
    }
}

using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace SpaceSails.UiGate;

// Lab 34 / issue #293 — "the rescue button must always be pressable."
//
// The owner kept getting bitten by controls that were PRESENT but not PRESSABLE (the out-of-power
// rescue pill buried under the masthead), and — cruising 2026-07-19, approving PRs from his phone —
// asked for the boot itself to be under test: "there would have to be more testing done here."
//
// This is that gate. One headless Chromium boots the published game the whole way a player does —
// front page → Launch Sol → wait out the (slow, interpreted-WASM) boot → start a new voyage — and
// then walks a SMALL, STABLE canary of critical controls, asking Playwright's own actionability
// engine the exact question the owner cares about: would a real click land on this control, or is
// something covering it? Trial clicks answer that without firing the control's side effect.
//
// Kept deliberately narrow (a canary, not an E2E suite): boot + desk switching + the captain's
// "Set course" + the pilot banner. The rescue pill's z-band itself is proven browser-free by the
// companion law (SpaceSails.Core.RescueLifeline, Lab 34 approach (b)); this proves the live boot
// path those laws assume actually stands up.
public sealed class BootAndReachabilityTests : IAsyncLifetime
{
    // Interpreted WASM under a plain publish (no AOT) is CPU-heavy while the sim loop spins, so
    // CDP round-trips can legitimately run long. Generous, signal-keyed waits — never sleeps.
    private static readonly float BootTimeoutMs = 180_000;
    private static readonly float ActionTimeoutMs = 60_000;

    // Console noise that is NOT a boot failure. Anything else on the error channel fails the gate.
    private static readonly string[] BenignConsole =
    [
        "favicon", "Failed to load resource", "net::ERR_", "sourcemap", "source map", "DevTools",
    ];

    // --- Load-speed budget (owner, cruise 2026-07-19: "Maybe add CI test to catch too slow loads.")
    //
    // The gate now TIMES the boot path at the milestones it already awaits and fails if any regresses
    // past an honest budget — so a slow-load regression (e.g. the boot doubling) can never merge
    // silently. The per-milestone timings are logged on EVERY run (pass or fail), giving a free perf
    // time-series in the CI logs.
    //
    // A record, not a flake-trap: budgets are deliberately generous. They are keyed to the AOT build
    // Pages actually ships (issue #371 Phase 2 / #382), sized from a fresh LOCAL measurement and then
    // multiplied for CI-runner variance, so they catch a REAL regression (a milestone roughly
    // doubling), not a slow runner.
    //
    // How the AOT budgets below were chosen. Measured on a dev box (.NET 10.0.301, headless Chromium,
    // AOT publish `-p:SpaceSailsPublishAot=true`), 3 runs, take the WORST:
    //
    //     milestone                     worst local (AOT)   est. CI baseline (×1.9)   chosen budget
    //     front page interactive             1.25 s               ~2.4 s                 10 s
    //     scenario boot complete             4.27 s               ~8.0 s                 20 s
    //     desk switch responsive             0.18 s               ~0.3 s                  8 s
    //     whole canary (total)               5.86 s              ~11.1 s                 30 s
    //
    // Why not a flat local×2.5: the #382 CI run measured the whole AOT canary at ~11 s versus 5.9 s on
    // this box — CI is ~1.9× slower. A literal local×2.5 boot budget (~10.7 s) would sit barely above
    // the ~8 s CI boot BASELINE and flake on any slow runner. So budgets are anchored to that CI
    // baseline (local×1.9) with ~2.5-3× headroom on top — generous enough to never flake on a slow
    // runner, tight enough to catch a real regression (a milestone ballooning well past double). The
    // tiny sub-second milestones (front page, desk switch) get an absolute floor instead of a ratio,
    // since 2.5× of a fraction of a second would be a flake-trap, not a budget.
    //
    // We ship BOTH per-milestone budgets AND a total-canary backstop: the per-milestone numbers
    // localise a regression (which phase got slow), and the total catches drift no single milestone
    // trips. All four are logged every run; a breach names the numbers ("boot took 41.2s, budget 20s")
    // and still lets the existing failure-artifact capture run.
    private sealed record LoadBudget(long FrontPageMs, long BootMs, long DeskSwitchMs, long TotalMs);

    // AOT payload (the shipping artifact — the numbers that gate CI). Tuned from the measurement above.
    private static readonly LoadBudget AotBudget = new(
        FrontPageMs: 10_000,
        BootMs: 20_000,
        DeskSwitchMs: 8_000,
        TotalMs: 30_000);

    // Interpreted payload (a plain local `dotnet publish` with no AOT). Blazor WASM is IL-INTERPRETED
    // here, ~100× slower on the CPU-heavy boot, so the AOT budgets would false-fail. This path is not
    // what CI gates (CI always publishes AOT); the budgets are only a sanity ceiling so a local non-AOT
    // run still exercises the assertions without flaking. Local interpreted measurement was front 1.2s,
    // boot 18.0s, desk 0.2s, total 19.6s — these ceilings are ~2.5-8× that, deliberately loose.
    private static readonly LoadBudget InterpretedBudget = new(
        FrontPageMs: 40_000,
        BootMs: 150_000,
        DeskSwitchMs: 40_000,
        TotalMs: 200_000);

    // AOT compiles managed IL to native wasm, ballooning dotnet.native.*.wasm from ~1.5 MB (the
    // interpreter runtime alone) to ~18 MB. A threshold between the two cleanly tells the payloads
    // apart from the artifact itself — no flag to forget in CI, no game-code touch.
    private const long AotNativeWasmThresholdBytes = 6_000_000;

    private readonly Stopwatch _clock = new();

    // Per-milestone timings (ms). -1 = the milestone was never reached (an earlier step failed);
    // the timing log distinguishes "not reached" from a real 0.
    private long _frontPageMs = -1;
    private long _bootMs = -1;
    private long _deskSwitchMs = -1;
    private long _totalMs = -1;

    private readonly StringBuilder _log = new();
    private readonly List<string> _consoleErrors = new();
    private readonly List<string> _pageErrors = new();

    private ClientHost _host = null!;
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        EnsureBrowsersInstalled();
        _host = await ClientHost.StartAsync(new StringWriter(_log));

        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new() { Headless = true });
        IBrowserContext ctx = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
        });
        _page = await ctx.NewPageAsync();
        _page.SetDefaultTimeout(ActionTimeoutMs);
        _page.SetDefaultNavigationTimeout(BootTimeoutMs);

        _page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                _consoleErrors.Add(msg.Text);
            }
        };
        _page.PageError += (_, err) => _pageErrors.Add(err);
    }

    [Fact]
    public async Task Boots_and_the_critical_controls_are_reachable()
    {
        // The clock spans the whole canary; per-milestone deltas are read off it at each awaited
        // signal (never a sleep). Timings are logged in `finally` so every run records them.
        _clock.Restart();
        try
        {
            // --- 1. Front page → Launch the Sol scenario (the maiden voyage's front door). ------
            await GotoWithRetry(_host.BaseUrl + "/");
            ILocator launch = _page.Locator("a.btn-primary[href*='scenario=sol']");
            await launch.ClickAsync(); // canary #1: the Launch button lands
            // MILESTONE (a): front page interactive — nav + parse + Launch actionable + clicked.
            _frontPageMs = _clock.ElapsedMilliseconds;
            long bootStart = _clock.ElapsedMilliseconds;
            Record("front page: Launch (Sol) is clickable");

            // --- 2. Wait out the boot: the "Rigging the sails…" spinner detaches on world-ready. -
            await _page.WaitForSelectorAsync(".map-loading",
                new() { State = WaitForSelectorState.Detached, Timeout = BootTimeoutMs });
            Record("world-ready: the boot spinner cleared");

            // --- 3. The start-picker front door, then start a new voyage (a docked berth). -------
            await _page.Locator(".start-picker-backdrop").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
            ILocator newVoyage = _page.Locator(".start-picker-newvoyage");
            await newVoyage.ClickAsync(); // canary #2: the new-voyage berth lands
            Record("start picker: New voyage berth is clickable");

            // --- 4. The desk tab bar renders — the spine every desk hangs off. ------------------
            ILocator tabBar = _page.Locator(".desk-tab-bar");
            await tabBar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = BootTimeoutMs });
            Assert.True(await tabBar.IsVisibleAsync(), "desk tab bar did not render after boot");
            // We booted INTO the game, not back onto the picker.
            Assert.False(await _page.Locator(".start-picker-backdrop").IsVisibleAsync(),
                "start picker is still up — the new voyage never started");
            // MILESTONE (b): scenario boot complete — the WASM boot from Launch click to a live desk.
            _bootMs = _clock.ElapsedMilliseconds - bootStart;
            Record("desk tab bar renders");

            // --- 5. Switching to a desk works: click the Captain tab, its room comes up. --------
            long deskStart = _clock.ElapsedMilliseconds;
            await _page.Locator("button.desk-tab", new() { HasTextString = "Captain" }).ClickAsync(); // canary #3
            await _page.Locator(".captain-desk").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            // MILESTONE (c): desk switch responsive — tab click to the room painted.
            _deskSwitchMs = _clock.ElapsedMilliseconds - deskStart;
            Record("desk switch: Captain tab opens the captain's desk");

            // --- 6. The captain's "Set course to a start point…" — REACHABLE (trial, no jump). --
            // A trial click runs Playwright's full actionability battery (visible, stable, enabled,
            // receives-events / not covered) but does NOT fire the handler, so it never reopens the
            // picker. This is the direct "would a real click land, or is it buried?" question #293
            // is about.
            await _page.Locator(".captain-desk button", new() { HasTextString = "Set course to a start point" })
                .ClickAsync(new() { Trial = true }); // canary #4
            Record("captain desk: 'Set course to a start point' is reachable");

            // --- 7. Back to Nav — the flight HUD returns (desk switching both ways). ------------
            await _page.Locator("button.desk-tab", new() { HasTextString = "Nav" }).ClickAsync(); // canary #5
            await _page.Locator(".map-hud").WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Record("desk switch: Nav tab restores the flight HUD");

            // --- 8. The #127 pilot banner — the "who is flying the ship" affordance, on every ----
            //        desk, must never be covered. Trial-click it as a reachability canary.
            await _page.Locator(".pilot-banner").First.ClickAsync(new() { Trial = true }); // canary #6
            Record("nav: the pilot banner (who has the ship) is reachable");

            // --- 9. HER ATMOSPHERE BOARD OPENS, AND THE VALVE IS STILL BEHIND THE CAPTAIN'S WORD. -
            //
            // Owner, standing at her valves: "those panels won't open there, so implementation is not
            // there" — and it was worse than that. The board printed a status line and had no valves;
            // AND both of her atmosphere consoles were unreachable, because NearestConsoleSpot returned
            // the FIRST console in array order rather than the nearest, so an earlier console always
            // took the [E]. Neither fault was visible to any test we had: the geometry audit walks
            // reachability (ConsoleCrowdingTests) but cannot know whether a panel RENDERS, and nothing
            // else opened it. This is the browser saying it does.
            //
            // It is also the gate, proven where a player would meet it: with nothing authorized there is
            // no way to open a compartment to space, whatever is selected.
            await _page.Locator("button.desk-tab", new() { HasTextString = "Deck" }).ClickAsync();
            // #958 · the deck-view toggle used to be the marker that the desk had switched; it went with the
            // walk-in view. The tab going info-blue is the same page saying the same thing.
            await _page.Locator("button.desk-tab.btn-info", new() { HasTextString = "Deck" }).WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Record("desk switch: the Deck tab puts the captain on her own deck");

            // The deck's keys are handled on .map-page (tabindex 0), so the walk needs focus there.
            await _page.Locator(".map-page").FocusAsync();

            // THE WALK AFT, dead-reckoned off the ship's own numbers. Docked, the captain stands at
            // (2.5, 6) in the airlock corridor (HavenInterior's spawn — NOT the bare ship's bridge spawn,
            // which is what the first cut of this assumed and why it walked into nothing). Her atmosphere
            // valves are at ShipLayout.ValveStation (−16, −7), aft in the engine room, and the interact
            // radius is 3 du. WASD walks at 9 du/s, frame-rate independent.
            //
            //   s ~0.7 s : down out of the airlock corridor to the centreline (y ≈ 0)
            //   a ~2.1 s : aft along the corridor to x ≈ −16, straight through the engine bulkhead's
            //              centreline door (x −14, a 4 du gap at y −2…2) — hence the trip to y ≈ 0 first
            //   s ~0.8 s : down to y ≈ −7, arriving within a unit of the board
            //
            // Every leg has ~3 du of slack before the console falls out of reach, so this is not a
            // frame-perfect walk; and nothing else aboard is within reach of the finish (her charge dump,
            // the nearest thing, is 6 du away — which is the separation the deck audit now holds).
            await WalkAsync("s", 700);
            await WalkAsync("a", 2100);
            await WalkAsync("s", 800);
            await _page.Keyboard.PressAsync("e");

            ILocator shipBoard = _page.Locator(".vent-board");
            await shipBoard.WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Assert.Contains("ATMOSPHERE", await shipBoard.InnerTextAsync(), StringComparison.Ordinal);
            string boardText = await shipBoard.InnerTextAsync();
            Assert.Contains("HER OWN HULL", boardText, StringComparison.Ordinal);
            Assert.Contains("Reserve", boardText, StringComparison.Ordinal);
            Record("her atmosphere board opens at the bridge repeater");
            await ProofShotAsync("her-atmosphere-board");

            // THE GATE, at the moment the board comes up: nothing is authorized, so no handle on the
            // panel can open a compartment to space. A button that existed here would be the whole
            // design defeated — "proximity is never consent" is the rule this inherits from the
            // boarding authorization, which exists because the owner once got robbed by accident.
            Assert.Equal(0, await _page.Locator(".vent-board button", new() { HasTextString = "TO SPACE" })
                                        .CountAsync());
            Record("her board offers no way to vent without the captain's word");

            // And she is still a ship at peace when the board is put away.
            await _page.Locator(".vent-board button", new() { HasTextString = "Step away" }).ClickAsync();
            await shipBoard.WaitForAsync(
                new() { State = WaitForSelectorState.Hidden, Timeout = ActionTimeoutMs });
            Record("her board closes");

            // --- 10. HER SCUTTLING CHARGES, AND THE SECOND KEY. -----------------------------------
            //
            // Owner: "that ship also has the scuttling charges... let's have a captains approval mechanic for
            // that also on our ship" — and the rule that makes it more than a button: the charges take TWO
            // hands, and the second one is the CREW'S. So the gate proves the panel opens aft and that TURN
            // BOTH KEYS is refused until both have been given. A self-destruct armable by one press is the one
            // bug in this game nobody would get to report twice.
            //
            // The charges are port-side aft at ShipLayout.ScuttleStation (−17, 7) and the captain is standing
            // at the valves (−16, −7): a straight walk up the engine room, 14 du at 9 du/s.
            await _page.Locator(".map-page").FocusAsync();
            await WalkAsync("w", 1600);
            await _page.Keyboard.PressAsync("e");

            ILocator charges = _page.Locator(".vent-board");
            await charges.WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Assert.Contains("SCUTTLING CHARGES", await charges.InnerTextAsync(), StringComparison.Ordinal);
            Record("her scuttling-charge panel opens aft");
            await ProofShotAsync("her-scuttling-charges");

            // BOTH KEYS OR NEITHER: no word given, nobody asked, so the act is unavailable.
            Assert.True(
                await _page.Locator(".vent-board button", new() { HasTextString = "TURN BOTH KEYS" })
                           .IsDisabledAsync(),
                "her charges could be armed without the captain's word and the crew's second key");
            Record("her charges refuse to arm on one hand");

            await _page.Locator(".vent-board button", new() { HasTextString = "Step away" }).ClickAsync();
            await charges.WaitForAsync(
                new() { State = WaitForSelectorState.Hidden, Timeout = ActionTimeoutMs });
            Record("her charge panel closes cold");

            // --- 11. THE CHARGE BOARD (#523). ------------------------------------------------------
            //
            // Owner: "Now that desk is kind of lame on the ship, so let's add some kind of on automatic there."
            // The console used to halve a hidden number and tell a joke about a cat; it is a board now. From the
            // charges at (−17, 7) the charge dump is at (−21, −3): down the engine room, then a step to port.
            //
            // NOTE the gate boots `scenario=sol`, which does NOT enable the plasma layer — so this proves the
            // board's HONEST branch, the one a standard voyage actually shows: it opens, and it explains why the
            // needles are dead instead of pretending to read something.
            await _page.Locator(".map-page").FocusAsync();
            await WalkAsync("s", 1150);
            await WalkAsync("a", 500);
            await _page.Keyboard.PressAsync("e");

            ILocator chargeBoard = _page.Locator(".vent-board");
            await chargeBoard.WaitForAsync(
                new() { State = WaitForSelectorState.Visible, Timeout = ActionTimeoutMs });
            Assert.Contains("HULL CHARGE", await chargeBoard.InnerTextAsync(), StringComparison.Ordinal);
            Record("her charge board opens at the dump");
            await ProofShotAsync("her-charge-board");

            await _page.Locator(".vent-board button", new() { HasTextString = "Step away" }).ClickAsync();
            await chargeBoard.WaitForAsync(
                new() { State = WaitForSelectorState.Hidden, Timeout = ActionTimeoutMs });
            Record("her charge board closes");

            // --- The console must be clean: no uncaught JS, no unexplained error logs. ----------
            Assert.True(_pageErrors.Count == 0,
                "Uncaught JS errors during boot:\n  " + string.Join("\n  ", _pageErrors));
            string[] realConsoleErrors = _consoleErrors
                .Where(e => !BenignConsole.Any(b => e.Contains(b, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            Assert.True(realConsoleErrors.Length == 0,
                "Console errors during boot:\n  " + string.Join("\n  ", realConsoleErrors));
            Record("console clean: no uncaught JS or unexplained console errors");

            // MILESTONE total: the whole canary's wall clock (nav → all controls proven).
            _totalMs = _clock.ElapsedMilliseconds;

            // --- Load-speed budget (owner 2026-07-19). Numbers already logged in `finally`; here we
            //     turn a regression into a red check. A breach names the numbers and, because it
            //     throws, still runs CaptureFailureArtifactsAsync below. -----------------------------
            AssertWithinBudget();
        }
        catch
        {
            await CaptureFailureArtifactsAsync();
            throw;
        }
        finally
        {
            LogTimings();
            DumpLog();
        }
    }

    // Fail the gate when a milestone (or the whole canary) ran slower than its honest budget — a
    // load-speed REGRESSION, not a slow runner (budgets carry ~2.5× headroom). Collects EVERY breach
    // so one red check reports all of them, each with the measured number vs its budget.
    private void AssertWithinBudget()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("SPACESAILS_UIGATE_NO_BUDGET"), "1",
                StringComparison.Ordinal))
        {
            _log.AppendLine("[budget] SPACESAILS_UIGATE_NO_BUDGET=1 — timings logged, budget NOT enforced.");
            return;
        }

        bool aot = IsAotPayload();
        LoadBudget b = aot ? AotBudget : InterpretedBudget;
        _log.AppendLine($"[budget] payload={(aot ? "AOT (shipping)" : "interpreted (non-AOT local)")} — enforcing this profile.");

        var breaches = new List<string>();
        void Check(string name, long measured, long budget)
        {
            if (measured >= 0 && measured > budget)
            {
                breaches.Add($"{name} took {Fmt(measured)}, budget {Fmt(budget)}");
            }
        }

        Check("front page interactive", _frontPageMs, b.FrontPageMs);
        Check("scenario boot complete", _bootMs, b.BootMs);
        Check("desk switch responsive", _deskSwitchMs, b.DeskSwitchMs);
        Check("whole canary", _totalMs, b.TotalMs);

        Assert.True(breaches.Count == 0,
            "Load-speed budget exceeded — the boot path regressed (or the runner is genuinely "
            + "over-budget):\n  " + string.Join("\n  ", breaches));
    }

    // Append the per-milestone timing table to the step log so EVERY run — pass or fail — records the
    // numbers (a free perf time-series in CI). Runs in `finally`, before the log is dumped.
    private void LogTimings()
    {
        bool aot = IsAotPayload();
        LoadBudget b = aot ? AotBudget : InterpretedBudget;
        _log.AppendLine();
        _log.AppendLine($"=== load-speed timings (payload: {(aot ? "AOT" : "interpreted")}) ===");
        void Row(string name, long measured, long budget) =>
            _log.AppendLine($"[timing] {name,-26} {(measured < 0 ? "(not reached)" : Fmt(measured)),12}   budget {Fmt(budget)}");
        Row("front page interactive", _frontPageMs, b.FrontPageMs);
        Row("scenario boot complete", _bootMs, b.BootMs);
        Row("desk switch responsive", _deskSwitchMs, b.DeskSwitchMs);
        Row("whole canary (total)", _totalMs, b.TotalMs);
    }

    private static string Fmt(long ms) => $"{ms / 1000.0:0.0}s ({ms}ms)";

    // Which load-speed budget applies. AOT (the artifact Pages ships) compiles managed IL to native
    // wasm, so dotnet.native.*.wasm balloons from ~1.5 MB (interpreter runtime only) to ~18 MB; a
    // 6 MB threshold cleanly separates the two straight off the served artifact — nothing to set in
    // CI, no game code touched. On any doubt (file missing) we assume interpreted (the looser budget)
    // so the gate never false-fails on a detection miss.
    private bool IsAotPayload()
    {
        try
        {
            string framework = Path.Combine(_host.WwwrootPath, "_framework");
            string? native = Directory
                .EnumerateFiles(framework, "dotnet.native.*.wasm")
                .OrderByDescending(f => new FileInfo(f).Length)
                .FirstOrDefault();
            return native is not null && new FileInfo(native).Length > AotNativeWasmThresholdBytes;
        }
        catch
        {
            return false;
        }
    }

    // --- helpers -------------------------------------------------------------------------------

    // Load the page, retrying once — a cold CI runner can drop the very first navigation while the
    // just-started Kestrel/WASM assets warm up. One retry, then let the failure stand.
    private async Task GotoWithRetry(string url)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                await _page.GotoAsync(url, new()
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = BootTimeoutMs,
                });
                return;
            }
            catch (Exception ex) when (attempt == 1)
            {
                _log.AppendLine($"[retry] first page load failed ({ex.Message}); retrying once.");
            }
        }
    }

    private void Record(string name) => _log.AppendLine($"PASS — {name}");

    /// <summary>
    /// A PROOF SHOT ON A GREEN RUN. The gate has always saved a screenshot when it FAILS; the owner reviews
    /// this project from a phone, often at sea, and asked for screenshots to comment on — so the boards it
    /// proves are also worth photographing when they work. Same artifacts directory CI already uploads, so a
    /// green check now carries pictures of what green means.
    /// </summary>
    private async Task ProofShotAsync(string name)
    {
        try
        {
            string dir = ArtifactsDir();
            Directory.CreateDirectory(dir);
            await _page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"proof-{name}.png") });
            _log.AppendLine($"SHOT — proof-{name}.png");
        }
        catch (Exception ex)
        {
            // A missing photograph must never fail a green gate.
            _log.AppendLine($"SHOT FAILED — {name}: {ex.Message}");
        }
    }

    /// <summary>Hold a deck key down for a while, the way a captain walks. The deck reads held keys and
    /// integrates real time at a fixed speed (9 du/s), so a duration is a distance.</summary>
    private async Task WalkAsync(string key, int milliseconds)
    {
        await _page.Keyboard.DownAsync(key);
        await Task.Delay(milliseconds);
        await _page.Keyboard.UpAsync(key);
    }

    private async Task CaptureFailureArtifactsAsync()
    {
        string dir = ArtifactsDir();
        Directory.CreateDirectory(dir);
        try
        {
            await _page.ScreenshotAsync(new()
            {
                Path = Path.Combine(dir, "ui-gate-failure.png"),
                FullPage = true,
            });
            _log.AppendLine($"[artifacts] screenshot → {Path.Combine(dir, "ui-gate-failure.png")}");
        }
        catch (Exception ex)
        {
            _log.AppendLine($"[artifacts] screenshot failed: {ex.Message}");
        }

        var console = new StringBuilder();
        console.AppendLine("=== page errors (uncaught JS) ===");
        console.AppendLine(_pageErrors.Count == 0 ? "(none)" : string.Join("\n", _pageErrors));
        console.AppendLine();
        console.AppendLine("=== console errors ===");
        console.AppendLine(_consoleErrors.Count == 0 ? "(none)" : string.Join("\n", _consoleErrors));
        await File.WriteAllTextAsync(Path.Combine(dir, "ui-gate-console.log"), console.ToString());
        _log.AppendLine($"[artifacts] console log → {Path.Combine(dir, "ui-gate-console.log")}");
    }

    // Write the step log where CI can upload it too (not only to xUnit output, which a red mobile
    // check can't expand).
    private void DumpLog()
    {
        Console.WriteLine(_log.ToString());
        try
        {
            string dir = ArtifactsDir();
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "ui-gate-steps.log"), _log.ToString());
        }
        catch
        {
            // Best-effort — the log already went to stdout.
        }
    }

    private static string ArtifactsDir()
    {
        string? fromEnv = Environment.GetEnvironmentVariable("SPACESAILS_UIGATE_ARTIFACTS");
        return string.IsNullOrWhiteSpace(fromEnv)
            ? Path.Combine(AppContext.BaseDirectory, "ui-gate-artifacts")
            : fromEnv;
    }

    // Ensure a Chromium is present so a bare `dotnet test` works with no separate install step.
    // CI installs browsers as an explicit warm step; this is the local-dev safety net.
    private static void EnsureBrowsersInstalled()
    {
        int exit = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exit != 0)
        {
            throw new InvalidOperationException(
                $"`playwright install chromium` exited {exit} — cannot run the UI gate without a browser.");
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
        _pw?.Dispose();
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }
}

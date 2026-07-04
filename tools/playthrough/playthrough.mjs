// Headless Playwright smoke test for PR-8 ("rig for silent running"), PR-11 (the desk
// framework) and PR-14 (the comms room + news wire — docs/SaturdayPlan/StationDesks.md).
//
// Proves the new-stations gameplay loop end-to-end in a real browser:
//   1. Launch the app in Release mode (Debug WASM is ~100x slower on the IL interpreter).
//   2. Load /map?scenario=sol, wait for world-ready ("Rigging the sails..." spinner gone).
//   3. Desk framework (PR-11): number-key switching — '2' opens the Sensors desk full-screen,
//      '1' returns to Nav, '5'/'3'/'4'/'6' open Comms/War room/Trade/Galley in turn, the chip
//      strip is present throughout.
//   4. Sensors desk: run a corridor scan program (using warp to pass sim time) until at least
//      one contact is tracked, then confirm the scope wall (PR-12) shows a live canvas tile per
//      telescope slot and that a filled tile appears once a track lands.
//   5. Comms desk (PR-14): dark web + the departures board + the news ticker render together in
//      one room; note whether the market is reachable from the current position (it requires
//      orbit-bound-at-haven/far-station; see README.md).
//   6. War room desk (PR-13): confirm the heat gauge renders at 0 and the desk's centerpiece —
//      the big tactical circle plus its range-scale selector — renders full-screen.
//   7. Trade desk (PR-13): confirm local space + the dock side panel + the cargo manifest column
//      (the trading floor's three columns) render.
//   8. Galley desk (PR-14): confirm the news feed renders, pour a tot, confirm the rum locker
//      updates.
//   9. Deck (PR-14): confirm the walkable deck loads (bridge seats are canvas-rendered, so this
//      is a smoke check + screenshot, not a per-seat DOM assertion).
//  10. Screenshot every desk into docs/tmp_pics/saturday/.
//
// This is tooling, not product code — it is exempt from the Razor+Bootstrap/renderer.js-only
// rule (repo agreement §9 applies to the game client, not test scripts).
//
// Usage: npm install && node playthrough.mjs
// (run from tools/playthrough/ — paths below are relative to the repo root, resolved from
// this file's location, so it also works if invoked with an absolute path from elsewhere.)

import { chromium } from "playwright";
import { spawn } from "node:child_process";
import { setTimeout as sleep } from "node:timers/promises";
import http from "node:http";
import net from "node:net";
import path from "node:path";
import fs from "node:fs";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const CLIENT_PROJECT = path.join(REPO_ROOT, "src", "SpaceSails.Client");
const SCREENSHOT_DIR = path.join(REPO_ROOT, "docs", "tmp_pics", "saturday");

const results = { steps: [], bugs: [] };
function record(name, ok, detail) {
  results.steps.push({ name, ok, detail });
  console.log(`${ok ? "PASS" : "FAIL"} — ${name}${detail ? `: ${detail}` : ""}`);
}

function isPortFree(port) {
  return new Promise((resolve) => {
    const srv = net.createServer();
    srv.once("error", () => resolve(false));
    srv.once("listening", () => srv.close(() => resolve(true)));
    srv.listen(port, "127.0.0.1");
  });
}

async function findFreePort(start) {
  for (let p = start; p < start + 40; p++) {
    if (await isPortFree(p)) {
      return p;
    }
  }
  throw new Error(`No free port found in [${start}, ${start + 40})`);
}

function waitForHttpOk(url, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  return new Promise((resolve, reject) => {
    const attempt = () => {
      const req = http.get(url, (res) => {
        res.resume();
        resolve(true);
      });
      req.on("error", () => {
        if (Date.now() > deadline) {
          reject(new Error(`Server did not respond at ${url} within ${timeoutMs}ms`));
        } else {
          setTimeout(attempt, 1000);
        }
      });
      req.setTimeout(3000, () => req.destroy());
    };
    attempt();
  });
}

function killTree(child) {
  if (!child || child.killed || child.exitCode !== null) return;
  if (process.platform === "win32") {
    spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"]);
  } else {
    try {
      process.kill(-child.pid, "SIGTERM");
    } catch {
      child.kill("SIGTERM");
    }
  }
}

// Set a Blazor <input type=range> and fire the 'input' event Blazor's @oninput listens on.
async function setRangeValue(page, selector, value) {
  await page.locator(selector).evaluate((el, v) => {
    const proto = Object.getPrototypeOf(el);
    const setter = Object.getOwnPropertyDescriptor(proto, "value").set;
    setter.call(el, String(v));
    el.dispatchEvent(new Event("input", { bubbles: true }));
  }, value);
}

async function main() {
  fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });

  const port = await findFreePort(5078);
  const baseUrl = `http://localhost:${port}`;
  console.log(`Starting server on ${baseUrl} (cwd=${CLIENT_PROJECT})`);

  const server = spawn(
    "dotnet",
    ["run", "-c", "Release", "--project", CLIENT_PROJECT, "--urls", baseUrl],
    { cwd: REPO_ROOT, stdio: ["ignore", "pipe", "pipe"] }
  );
  let serverLog = "";
  server.stdout.on("data", (d) => (serverLog += d.toString()));
  server.stderr.on("data", (d) => (serverLog += d.toString()));
  server.on("exit", (code, sig) => {
    console.log(`[server] exited code=${code} signal=${sig}`);
    console.log(`[server log tail]\n${serverLog.slice(-4000)}`);
  });

  let browser;
  try {
    await waitForHttpOk(baseUrl, 180_000);
    console.log("Server responding — launching headless Chromium.");

    browser = await chromium.launch({ headless: true });
    const page = await browser.newPage({ viewport: { width: 1440, height: 960 } });
    // Blazor WASM under `dotnet run` (no AOT publish) runs on the mono interpreter and gets
    // CPU-heavy once the sim loop is ticking every rAF frame — CDP round-trips (evaluate,
    // screenshot, click) can legitimately take well over Playwright's 30s default while the
    // main thread is busy. Raise the default action timeout generously rather than fail on
    // what is just slow, not stuck.
    page.setDefaultTimeout(150_000);
    page.setDefaultNavigationTimeout(150_000);
    const consoleErrors = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });
    page.on("pageerror", (err) => consoleErrors.push(String(err)));

    await page.goto(`${baseUrl}/map?scenario=sol`, { waitUntil: "load", timeout: 120_000 });

    // world-ready: the "Rigging the sails..." spinner (.map-loading) is removed once _worldReady.
    await page.waitForSelector(".map-loading", { state: "detached", timeout: 120_000 });
    record("world-ready", true, "map-loading spinner cleared");
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "00-map-ready.png") });

    // The desk framework (PR-11): number keys switch full-screen desks. Keystrokes go to the
    // wrapping tabindex=0 div Map.razor listens on (@onkeydown), so give it focus explicitly —
    // clicking the canvas doesn't bubble a DOM focus event up to its parent.
    const mapPage = page.locator(".map-page");
    await mapPage.focus();

    // Chip strip (rule 2): present on every desk, right from the Nav desk we start on. Generous
    // timeout here specifically — the WASM interpreter is still cold right after world-ready
    // (first NPC/ephemeris population is CPU-heavy), unlike the later desk checks below which
    // run after minutes of warped sim time have let the interpreter settle.
    await page.locator(".desk-chip-strip").waitFor({ state: "visible", timeout: 60_000 });
    record("desk chip strip present on Nav", true);

    // Crank warp to max up front — every station check below benefits from fast sim time.
    await setRangeValue(page, ".map-warp-control input[type=range]", 100);
    await mapPage.focus();

    // Most of the 8 generated traffic ships are mid-flight He3 haulers already active at sim
    // start (out at Saturn/Jupiter distances — well past the telescope's 6e11 m base range from
    // near Earth); the remaining short-haul Earth/Mars/Venus runs (the ones actually close
    // enough to find) have a staggered ActivationTime of 3-30 sim-days (TrafficSchedule.cs).
    // Warp forward past that horizon first so the sweep loop below has real candidates in range,
    // rather than sweeping a still-mostly-inactive traffic picture.
    // This is prep, not a pass/fail check in its own right — the real proof is the tracked-count
    // assertion below. 15 days comfortably covers the low end of the 3-30 day activation spread;
    // if the interpreter can't even get there in the time budget, the sweep loop below will
    // simply (and correctly) report zero tracked and fail loudly on its own.
    const reachedDays = await waitForSimDays(page, 15, 240_000);
    console.log(`warped forward — reached the ${reachedDays ? "" : "(partial) "}activation horizon`);

    // ---- Sensors desk (key '2') ----
    await page.keyboard.press("2");
    const trackingCard = page.locator(".tracking-post-card");
    await trackingCard.waitFor({ state: "visible", timeout: 30_000 });
    const trackingFullScreen = await trackingCard.evaluate((el) => el.classList.contains("tracking-post-desk"));
    record("'2' opens the Sensors desk full-screen", trackingFullScreen, "tracking-post-desk class present");
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "01-tracking-post-open.png") });

    const programSelect = trackingCard.locator("select");
    let tracked = false;
    let sweepDetail = "";

    // Try each named corridor program first (skip index 0, the "manual aim" placeholder).
    // Programs rebuild off sim time as it advances under warp, so re-check the option count
    // and clamp the index every iteration rather than trusting a count captured up front.
    for (let i = 1; !tracked; i++) {
      const optionCount = await programSelect.locator("option").count();
      if (i >= optionCount) break;
      const optionText = await programSelect.locator("option").nth(i).innerText();
      await programSelect.selectOption({ index: i });
      await trackingCard.locator('button:has-text("Start sweep")').click();

      const swept = await pollForSweepComplete(trackingCard, 90_000);
      const count = await readTrackedCount(trackingCard);
      sweepDetail = `program "${optionText}" -> ${swept ? "swept" : "timed out"}, tracked=${count}`;
      console.log(`  sweep attempt: ${sweepDetail}`);
      if (count > 0) {
        tracked = true;
      }
    }

    // Fallback: one full 360 degree manual sweep if no corridor program found anything.
    if (!tracked) {
      await setRangeValue(page, ".tracking-post-card input[type=range]:nth-of-type(2)", 360);
      await trackingCard.locator('button:has-text("Start sweep")').click();
      await pollForSweepComplete(trackingCard, 120_000);
      const count = await readTrackedCount(trackingCard);
      sweepDetail += ` | fallback 360 sweep -> tracked=${count}`;
      if (count > 0) tracked = true;
    }

    record("tracking post: at least one target tracked via corridor scan", tracked, sweepDetail);

    // ---- Scope wall (PR-12): a live scope tile per tracked target, not one small inset ----
    const scopeWallTile0 = page.locator("#scope-wall-0");
    await scopeWallTile0.waitFor({ state: "attached", timeout: 10_000 });
    const wallCanvasCount = await page.locator(".scope-wall-canvas").count();
    record(
      "scope wall renders a canvas tile per telescope slot",
      wallCanvasCount > 0,
      `${wallCanvasCount} tile canvas(es) present`
    );
    if (tracked) {
      // At least one tile should have drawn something other than the empty "no track" placeholder
      // once a target is actually held — the filled tile's footer carries a quality bar + buttons
      // the empty tile doesn't.
      const filledTiles = await page.locator(".scope-wall-tile:not(.scope-wall-tile-empty)").count();
      record(
        "at least one scope-wall tile shows a live track (not the empty placeholder)",
        filledTiles > 0,
        `${filledTiles} filled tile(s)`
      );
    }
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "02-tracking-post-tracked.png") });
    // Also the plain filename docs/features/tracking-post.md references directly.
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "tracking-post.png") });

    // ---- Back to Nav (key '1') ----
    await mapPage.focus();
    await page.keyboard.press("1");
    await page.locator(".map-hud").waitFor({ state: "visible", timeout: 30_000 });
    await trackingCard.waitFor({ state: "hidden", timeout: 30_000 });
    record("'1' returns to the Nav desk", true, "map-hud (toolbar/readouts) visible again");
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "00b-nav-desk.png") });

    // ---- Comms desk (key '5'): dark web + the traffic board (moved off the old toolbar) ----
    await mapPage.focus();
    await page.keyboard.press("5");
    const darkWebCard = page.locator(".dark-web-card");
    await darkWebCard.waitFor({ state: "visible", timeout: 30_000 });
    const darkWebFullScreen = await darkWebCard.evaluate((el) => el.classList.contains("dark-web-desk"));
    const darkWebText = await darkWebCard.innerText();
    const marketReachable = darkWebText.includes("Off-the-books ships the market knows about");
    record(
      "'5' opens the Comms desk (dark web full-screen)",
      darkWebFullScreen,
      marketReachable
        ? "market listing reachable from current position"
        : "gated (not orbit-bound at a haven/far station) — shows expected disabled-reason message"
    );
    const trafficBoardOnComms = await page.locator(".desk-comms-grid").getByText("Departures board").isVisible();
    record("departures board renders inside the Comms desk", trafficBoardOnComms);
    const commsTicker = page.locator(".comms-ticker");
    await commsTicker.waitFor({ state: "visible", timeout: 30_000 });
    const tickerText = (await commsTicker.innerText()).trim();
    record("news ticker renders on the Comms desk", tickerText.length > 0, `ticker text: "${tickerText.slice(0, 80)}"`);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "03-dark-web.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "dark-web.png") });

    // ---- War room desk (key '3') ----
    await mapPage.focus();
    await page.keyboard.press("3");
    const warRoomCard = page.locator(".war-room-card");
    await warRoomCard.waitFor({ state: "visible", timeout: 30_000 });
    const heatGaugeText = (await warRoomCard.locator(".war-room-heat-gauge").innerText()).trim();
    record("war room heat gauge renders at 0", heatGaugeText === "◌◌◌", `gauge text = "${heatGaugeText}"`);
    // PR-13: the desk's centerpiece is the ~60%-wide tactical circle (war-room-scope-big) with
    // the range-scale selector above it — confirm both render full-screen, not just the card.
    const tacticalCircleVisible = await page.locator(".war-room-scope-big").isVisible();
    record("war room tactical circle (centerpiece) renders full-screen", tacticalCircleVisible);
    const rangeSelectorVisible = await page.locator(".war-room-range-group").isVisible();
    record("war room range-scale selector renders", rangeSelectorVisible);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "04-war-room.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "war-room.png") });

    // ---- Trade desk (key '4'): local space + the dock side panel + cargo manifest ----
    await mapPage.focus();
    await page.keyboard.press("4");
    const localSpaceCard = page.locator(".local-space-card");
    await localSpaceCard.waitFor({ state: "visible", timeout: 30_000 });
    const dockPanelOnTrade = await page.locator(".desk-trade-grid .desk-side-panel").first().isVisible();
    record("'4' opens the Trade desk (local space + dock side panel)", dockPanelOnTrade);
    // PR-13: the trading floor's third column — the cargo manifest read-model over the hold.
    const manifestPanelVisible = await page.locator(".trade-manifest-panel").isVisible();
    record("trade desk cargo manifest panel (centerpiece column) renders", manifestPanelVisible);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "05-local-space.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "local-space.png") });

    // ---- Galley desk (key '6') ----
    await mapPage.focus();
    await page.keyboard.press("6");
    const galleyDesk = page.locator(".galley-desk");
    await galleyDesk.waitFor({ state: "visible", timeout: 30_000 });
    record("'6' opens the Galley desk", true);
    const galleyNewsText = (await page.locator(".galley-news").innerText()).trim();
    record(
      "Galley news feed renders a headline",
      galleyNewsText.length > 0 && !galleyNewsText.includes("No word from the wire"),
      `first line: "${galleyNewsText.split("\n")[1] ?? galleyNewsText.slice(0, 80)}"`
    );
    const totsBefore = await readRumTots(galleyDesk);
    await galleyDesk.locator('button:has-text("Pour a tot")').click();
    await sleep(500);
    const totsAfter = await readRumTots(galleyDesk);
    record("pouring a tot in the Galley updates the shared rum locker", totsAfter > totsBefore, `${totsBefore} -> ${totsAfter}`);
    const chipsOnGalley = await page.locator(".desk-chip-strip .desk-chip").count();
    record("summary chips render on the Galley desk", chipsOnGalley > 0, `${chipsOnGalley} chips`);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "06-galley.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "galley.png") });

    // ---- Deck (key '7'): bridge seats are canvas-rendered, so this is a smoke check + a
    // screenshot for a human to eyeball the seat layout, not a per-seat DOM assertion. ----
    await mapPage.focus();
    await page.keyboard.press("7");
    await sleep(500);
    const hudHiddenOnDeck = !(await page.locator(".map-hud").isVisible());
    record("'7' leaves the Nav HUD for the walkable deck", hudHiddenOnDeck);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "07-deck.png") });

    // ---- Back to Nav once more, chip strip still present throughout ----
    await mapPage.focus();
    await page.keyboard.press("1");
    await page.locator(".map-hud").waitFor({ state: "visible", timeout: 30_000 });
    await page.locator(".desk-chip-strip").waitFor({ state: "visible", timeout: 30_000 });
    record("desk chip strip present back on Nav", true);

    if (consoleErrors.length > 0) {
      results.bugs.push({ note: "browser console errors observed", detail: consoleErrors.slice(0, 20) });
    }
  } finally {
    if (browser) await browser.close();
    killTree(server);
    await sleep(500);
  }

  console.log("\n=== PLAYTHROUGH SUMMARY ===");
  console.log(JSON.stringify(results, null, 2));
  const allOk = results.steps.every((s) => s.ok);
  if (!allOk) {
    console.error("\nOne or more checks FAILED — see above.");
    process.exitCode = 1;
  }
}

async function pollForSweepComplete(trackingCard, timeoutMs) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const text = await trackingCard.innerText();
    if (/Sweep complete|Sweep aborted/.test(text)) {
      return true;
    }
    await sleep(2000);
  }
  return false;
}

async function waitForSimDays(page, minDays, timeoutMs) {
  const readouts = page.locator(".map-readouts");
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const text = await readouts.innerText();
    const m = text.match(/Sim time:\s*(\d+)d/);
    const days = m ? parseInt(m[1], 10) : 0;
    if (days >= minDays) {
      return true;
    }
    await sleep(2000);
  }
  return false;
}

async function readTrackedCount(trackingCard) {
  const text = await trackingCard.innerText();
  const m = text.match(/Tracked targets \((\d+)\s*\//);
  return m ? parseInt(m[1], 10) : 0;
}

async function readRumTots(galleyDesk) {
  const text = await galleyDesk.innerText();
  const m = text.match(/Tots poured:\s*(\d+)/);
  return m ? parseInt(m[1], 10) : 0;
}

main().catch((err) => {
  console.error(err);
  process.exitCode = 1;
});

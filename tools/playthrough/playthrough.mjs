// Headless Playwright smoke test for PR-8 ("rig for silent running").
//
// Proves the new-stations gameplay loop end-to-end in a real browser:
//   1. Launch the app in Release mode (Debug WASM is ~100x slower on the IL interpreter).
//   2. Load /map?scenario=sol, wait for world-ready ("Rigging the sails..." spinner gone).
//   3. Track post: run a corridor scan program (using warp to pass sim time) until at least
//      one contact is tracked.
//   4. Dark web ("Web"): open the panel, note whether the market is reachable from the
//      current position (it requires orbit-bound-at-haven/far-station; see README.md).
//   5. War room ("Guns"): confirm the heat gauge renders at 0.
//   6. Local space ("Local"): confirm the panel renders.
//   7. Screenshot every station into docs/tmp_pics/saturday/.
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

    // Crank warp to max up front — every station check below benefits from fast sim time.
    await setRangeValue(page, ".map-warp-control input[type=range]", 100);

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

    // ---- Tracking post ----
    await page.click('button:has-text("Track 📡")');
    const trackingCard = page.locator(".tracking-post-card");
    await trackingCard.waitFor({ state: "visible", timeout: 15_000 });
    record("tracking-post panel opens", true);
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
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "02-tracking-post-tracked.png") });
    // Also the plain filename docs/features/tracking-post.md references directly.
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "tracking-post.png") });

    // Close the tracking post before opening the next station so each screenshot below is a
    // clean single-panel shot rather than a stack of overlapping cards.
    await page.click('button:has-text("Track 📡")');
    await trackingCard.waitFor({ state: "hidden", timeout: 15_000 });

    // ---- Dark web ----
    await page.click('button:has-text("Web 🕸")');
    const darkWebCard = page.locator(".dark-web-card");
    await darkWebCard.waitFor({ state: "visible", timeout: 15_000 });
    const darkWebText = await darkWebCard.innerText();
    const marketReachable = darkWebText.includes("Off-the-books ships the market knows about");
    record(
      "dark web panel renders",
      true,
      marketReachable
        ? "market listing reachable from current position"
        : "gated (not orbit-bound at a haven/far station) — shows expected disabled-reason message"
    );
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "03-dark-web.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "dark-web.png") });
    await page.click('button:has-text("Web 🕸")');
    await darkWebCard.waitFor({ state: "hidden", timeout: 15_000 });

    // ---- War room ----
    await page.click('button:has-text("Guns ⚔")');
    const warRoomCard = page.locator(".war-room-card");
    await warRoomCard.waitFor({ state: "visible", timeout: 15_000 });
    const heatGaugeText = (await warRoomCard.locator(".war-room-heat-gauge").innerText()).trim();
    record("war room heat gauge renders at 0", heatGaugeText === "◌◌◌", `gauge text = "${heatGaugeText}"`);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "04-war-room.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "war-room.png") });
    await page.click('button:has-text("Guns ⚔")');
    await warRoomCard.waitFor({ state: "hidden", timeout: 15_000 });

    // ---- Local space ----
    await page.click('button:has-text("Local 🛰")');
    const localSpaceCard = page.locator(".local-space-card");
    await localSpaceCard.waitFor({ state: "visible", timeout: 15_000 });
    record("local space panel renders", true);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "05-local-space.png") });
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, "local-space.png") });

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

main().catch((err) => {
  console.error(err);
  process.exitCode = 1;
});

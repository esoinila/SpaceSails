// Screenshot generator for the Lab Viz doc images (docs/features/pics/lab-viz-*.png).
//
// The labs' `--viz` pop-ups are self-contained HTML files; this renders them headlessly and
// captures the screenshots embedded in labs/README.md and docs/features/lab-viz.md, so the doc
// images can be regenerated from real output whenever the viewer changes (same honesty rule as
// the READMEs' printed numbers — no doctored pictures).
//
// Usage (from tools/playthrough, after generating the HTML files):
//   dotnet run --project ../../labs/01-falling-is-orbiting -c Release -- --viz --viz-no-open
//   dotnet run --project ../../labs/19-the-grand-tour     -c Release -- --viz --viz-no-open
//   node labviz-shots.mjs
//
// This is tooling, not product code.

import { chromium } from "playwright";
import { fileURLToPath, pathToFileURL } from "node:url";
import path from "node:path";
import fs from "node:fs";

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const outDir = path.join(repo, "docs", "features", "pics");
fs.mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });

async function open(labHtml) {
  const file = path.join(repo, "labviz", labHtml);
  if (!fs.existsSync(file)) throw new Error(`missing ${file} — run the lab with -- --viz first`);
  await page.goto(pathToFileURL(file).href);
  await page.waitForSelector("#sky");
  await page.waitForTimeout(300); // first frames: canvas sizing + fit-to-paths framing
}

async function shot(name) {
  await page.waitForTimeout(200); // let the dirty-flag loop repaint after any evaluate()
  await page.screenshot({ path: path.join(outDir, name) });
  console.log(`wrote ${path.join("docs", "features", "pics", name)}`);
}

// 1. The Grand Tour, whole itinerary — scrubbed to the Jupiter flyby so the ghost ship and a
//    populated readout are in frame.
await open("lab19-the-grand-tour.html");
await page.evaluate(() => {
  const flyby = SCENE.markers.find((m) => m.kind === "flyby");
  setSimTime(flyby.t);
});
await shot("lab-viz-grand-tour.png");

// 2. Close-up of the Section B "crank" fan: the sweep arcs spraying past Jupiter at the epoch
//    the sweep was flown. Camera centered on Jupiter at the sweep's mid-time.
await page.evaluate(() => {
  // The arcs pass Jupiter where it sits at each arc's own flyby epoch, so find the tightest
  // closest approach across ALL sweep arcs and center the camera on Jupiter at that moment —
  // that's where the flyby hairpins cluster.
  const jupiter = SCENE.bodies.find((b) => b.id === "jupiter");
  let best = null;
  for (const p of SCENE.paths.filter((q) => q.group === "sweep")) {
    for (const [t, x, y] of p.samples) {
      const [jx, jy] = bodyPosition(jupiter, t);
      const d2 = (x - jx) ** 2 + (y - jy) ** 2;
      if (!best || d2 < best.d2) best = { t, d2 };
    }
  }
  const [jx, jy] = bodyPosition(jupiter, best.t);
  setSimTime(best.t);
  camera.centerX = jx;
  camera.centerY = jy;
  camera.mpp = 8.0e6; // ~1.3e10 m across: the whole hairpin fan plus Jupiter
  invalidate();
});
await shot("lab-viz-flyby-fan.png");

// 3. Lab 01's minimal example: the closed circular orbit, ghost a quarter-orbit in.
await open("lab01-falling-is-orbiting.html");
await page.evaluate(() => {
  const orbit = SCENE.paths[0];
  const tEnd = orbit.samples[orbit.samples.length - 1][0];
  setSimTime(tEnd * 0.25);
});
await shot("lab-viz-lab01-circle.png");

await browser.close();

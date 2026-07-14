// Blind-UI-audit screenshot capture (owner's protocol, 2026-07-14): shoot each desk / new panel
// on the LIVE published build, hand each image cold to Gemini ("what can you do here?"), and
// score intuitiveness by how much context it needs. Companion to docs/ui-guidelines.md's
// AI-playtest protocol from the Comms-tree work.
//
// Usage (from tools/playthrough):  node ui-audit-shots.mjs [baseUrl]
// Default baseUrl: https://esoinila.github.io/SpaceSails-play

import { chromium } from "playwright";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fs from "node:fs";

const base = process.argv[2] ?? "https://esoinila.github.io/SpaceSails-play";
const outDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "ui-audit");
fs.mkdirSync(outDir, { recursive: true });

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1600, height: 1000 } });

async function boot(query) {
  await page.goto(`${base}/map${query}`, { waitUntil: "networkidle" });
  await page.waitForSelector(".map-page", { timeout: 90_000 });
  await page.waitForTimeout(2500); // world-ready settle
}

async function key(k) {
  await page.locator(".map-page").focus();
  await page.keyboard.press(k);
  await page.waitForTimeout(900);
}

async function shot(name) {
  await page.screenshot({ path: path.join(outDir, name) });
  console.log("wrote " + name);
}

// 1. Nav desk, fresh Earth start — the default face of the game.
await boot("?start=earth");
await shot("nav-map.png");

// 2-5. The other map desks.
await key("2"); await shot("sensors.png");
await key("3"); await shot("warroom.png");
await key("4"); await shot("trade.png");
await key("5"); await shot("comms.png");

// 6. The plot desk with the sling panel open (the newest flight tooling).
await boot("?sling=jupiter");
await page.getByRole("button", { name: "Plot" }).click();
await page.waitForTimeout(700);
await page.getByRole("button", { name: /Sling past/ }).click();
await page.waitForTimeout(700);
await shot("plot-sling.png");

// 7. The Captain's ledger with both sections populated (the newest desk content).
// NB: "Ledger" alone is ambiguous — the Sensors desk already says "Ledger empty — the passive
// watch is scanning" for its TRACK ledger. Naming clash logged in the audit notes.
await boot("?start=space-bar&tip=route&fetch=intel");
await key("q");
await key("0");
await page.getByRole("button", { name: /📜/ }).first().click();
await page.waitForTimeout(700);
await shot("ledger.png");

// 8. The walkable deck, docked (the ashore entry point).
await boot("?start=space-bar");
await shot("deck.png");

await browser.close();
console.log("done: " + outDir);

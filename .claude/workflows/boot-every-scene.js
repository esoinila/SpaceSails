export const meta = {
  name: 'boot-every-scene',
  description: 'Boot the game\'s documented dev-start scenes headless, judge each against the testing guide\'s own "what a tester should see", adversarially verify anomalies',
  whenToUse: 'After a release or a big merge — the owner\'s boot-every-scene QA method, automated',
  phases: [
    { title: 'Catalog', detail: 'extract the cheat catalog from docs/testing-guide.md' },
    { title: 'Look', detail: 'one cheap agent per scene: boot headless, screenshot, judge vs the guide', model: 'sonnet' },
    { title: 'Verify', detail: 'skeptics re-boot flagged scenes to refute anomalies', model: 'opus' },
  ],
}

const BASE = (args && args.baseUrl) || 'http://localhost:5073'
const MAX_SCENES = (args && args.maxScenes) || 10

const CATALOG = {
  type: 'object', required: ['scenes'],
  properties: { scenes: { type: 'array', items: {
    type: 'object', required: ['name', 'path', 'expect'],
    properties: {
      name: { type: 'string' },
      path: { type: 'string', description: 'URL path+query starting with /map' },
      expect: { type: 'string', description: 'condensed what-a-tester-should-see, <=120 words' },
      interact: { type: 'string', description: 'one trivial documented step (a key or one click) or empty' },
    } } } },
}

const LOOK = {
  type: 'object', required: ['ok', 'anomalies', 'notes'],
  properties: {
    ok: { type: 'boolean' },
    anomalies: { type: 'array', items: { type: 'object', required: ['what', 'severity'], properties: {
      what: { type: 'string' }, severity: { type: 'string', enum: ['broken', 'wrong', 'cosmetic'] } } } },
    notes: { type: 'string' },
  },
}

const VERDICT = {
  type: 'object', required: ['real', 'reason'],
  properties: { real: { type: 'boolean' }, reason: { type: 'string' } },
}

phase('Catalog')
const cat = await agent(
  `Read D:/repo12/spaceSails/docs/testing-guide.md. It contains a catalog of dev-start cheat URLs (rows like ?barcase=1, ?counter=1, ?tablescene=..., ?spread=1, ?rip=1, ?park=1 and many more), each documenting WHAT A TESTER SHOULD SEE. Extract the ${MAX_SCENES} most substantive scenes, preferring: (1) anything touching the newest features (work-the-case / seats / galley), (2) scenes whose row documents rich visible expectations, (3) coverage spread across ship, haven, and underground. For each: name; the path+query (always starting /map, include scenario=sol if the row implies it, and append &process=4 where a dig/hold is part of the expectation); a <=120-word condensed expectation; at most ONE trivial documented interaction (a single key like "E" or one named button click) or empty string. Also count how many catalog rows you did NOT include.
Return via StructuredOutput. In notes-free fields be terse.`,
  { schema: CATALOG, effort: 'low', model: 'sonnet' })

const scenes = cat.scenes.slice(0, MAX_SCENES)
log(`catalog: sweeping ${scenes.length} scenes against ${BASE} (rows not included this sweep: see agent output)`)

const results = await pipeline(scenes,
  (s, _o, i) => agent(
    `You are a scene QA looker for the game SpaceSails. Use the browser-automation skill (headless browser).
URL: ${BASE}${s.path}
This is a Blazor WASM app served locally in Release: FIRST LOAD CAN TAKE 15-40s — wait, retry the screenshot, and never judge a blank/loading canvas as broken. ${s.interact ? `After it boots, perform exactly this documented step, then screenshot again: ${s.interact}.` : 'Look only; no interaction needed.'}
THE EXPECTATION (from the project's own testing guide — this is your oracle): ${s.expect}
Screenshot the booted scene and judge it against the expectation. Report ok=true when the scene substantially matches. Report anomalies ONLY for things the expectation implies should be otherwise: severity 'broken' (feature absent/dead/error), 'wrong' (present but contradicts the expectation or the sim visibly disagrees with a sentence/shape), 'cosmetic' (overlap, clipping, illegible text). Also check the browser console for errors and failed requests — an error there is an anomaly. Do NOT report style opinions, and do NOT report slow first load. notes: one or two sentences of what you saw.`,
    { label: `look:${s.name}`, phase: 'Look', schema: LOOK, model: 'sonnet' }),
  (r, s) => {
    if (!r) return { scene: s.name, path: s.path, ok: false, anomalies: [{ what: 'looker died', severity: 'broken' }], verified: [], notes: 'looker returned null' }
    const worth = r.anomalies.filter(a => a.severity !== 'cosmetic')
    if (worth.length === 0) return { scene: s.name, path: s.path, ok: r.ok, anomalies: r.anomalies, verified: [], notes: r.notes }
    return parallel(worth.map(a => () => agent(
      `You are a skeptic. A QA looker claims this anomaly in the SpaceSails scene at ${BASE}${s.path}: "${a.what}" (severity ${a.severity}). The documented expectation was: ${s.expect}
Try to REFUTE it. Use the browser-automation skill: boot the same URL headless, wait GENEROUSLY for the WASM app (up to 60s, retry screenshots), ${s.interact ? `perform the documented step (${s.interact}), ` : ''}and look with your own eyes. Common false positives here: judging mid-boot; a hidden/throttled frame; the looker missing a documented interaction; the expectation text describing a different sub-state. real=true ONLY if you reproduce the anomaly yourself; if uncertain or not reproduced, real=false with the reason.`,
      { label: `verify:${s.name}`, phase: 'Verify', schema: VERDICT, model: 'opus' })))
      .then(vs => ({
        scene: s.name, path: s.path, ok: r.ok, notes: r.notes,
        anomalies: r.anomalies,
        verified: worth.map((a, j) => ({ ...a, real: vs[j] ? vs[j].real : null, reason: vs[j] ? vs[j].reason : 'skeptic died' })),
      }))
  })

const flat = results.filter(Boolean)
const confirmed = flat.flatMap(r => (r.verified || []).filter(v => v.real).map(v => ({ scene: r.scene, path: r.path, ...v })))
log(`sweep done: ${flat.length} scenes, ${confirmed.length} confirmed anomalies`)
return { scenes: flat, confirmed }
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

// Subject: part of Map.Sim (#870 split; the header note lives in Map.Sim.cs) — BootTheWorldAsync — the one pass that reads every ?query and builds the world behind the loading door.
public partial class Map
{

    private async Task BootTheWorldAsync(CancellationToken abandoned)
    {
        // /map?scenario=sol-eu loads scenarios/sol-eu.json; default sol. Name is sanitized to a
        // simple slug — it becomes a URL path segment. /map?start=space-bar jumps the freshly-built
        // world straight to a named start point (see StartPoints) — the playtest "skip the set-up"
        // shortcut, and the same registry the boot picker offers. Unknown start id → the picker shows.
        string scenarioName = "sol";
        string? startId = null;
        string? dockCheat = null;      // /map?dock=<haven-id>: boot already clamped onto ANY dockable haven (#288)
        int? fuelCheat = null;         // /map?fuel=N: boot with N reaction-mass pulses in the tank (#288)
        int? creditsCheat = null;      // /map?credits=N: boot with N credits in the purse (#288)
        string? fetchCheat = null;
        string? crackCheat = null;
        string? tipCheat = null;
        string? hoardCheat = null;
        string? slingCheat = null;
        string? skimCheat = null;
        string? backroomCheat = null;
        double? simHoursCheat = null;
        bool ellipseCheat = false; // /map?ellipse=1 injects a visibly eccentric demo body (Kepler rails, PR-B)
        string? expeditionCheat = null; // #370 /map?expedition=1|mining: spawn an away-team gig accepted + its site in shuttle range at the berth
        string? deflectionCheat = null; // #394 /map?deflection=1|C|S|M: spawn the deflection gig accepted, rock inbound, ship docked at Ringside
        bool wreckCheat = false; // #488 /map?wreck=1: spawn a derelict in shuttle range — board her, read her, then file or strip
        Derelict.WreckCause? wreckCauseCheat = null; // #488 /map?wreck=<cause>: board a wreck that died THAT way
        bool secretlabDeep = false;  // #592 /map?secretlab=deep: the rock whose site hides a band
        bool secretlabCheat = false; // #409 /map?secretlab=1: spawn a landable rock in shuttle range that hides a Vantar lab, door pre-revealed
        string? kaamosCheat = null; // #411 /map?kaamos=N|all: assemble N KAAMOS fragments (or all) so the readout + reach notice are testable; ?kaamos=pod|holder instead SEATS the rare find so it can be EARNED
        bool bondCheat = false; // #429 /map?bond=1: dock at a bar with strangers + force the next ambient scare to bond (the cognac beat)
        bool oracleCheat = false; // #428 /map?oracle=1: seat the station oracle at whatever bar you dock at (she's a coin-flip fixture otherwise)
        bool ashoreCheat = false; // #428 /map?ashore=1: boot docked AND already standing in the bar — the ship→tube→hall walk already walked
        int? nerveCheat = null;   // #428 /map?nerve=N: seed the nerve gauge at N of NervePips.MaxPips whole pips at boot
        string? nebulaCheat = null; // #422 /map?nebula=N|all: assemble N NEBULA fragments (or all) so the readout + truth notice are testable; ?nebula=adjuster instead SEATS the rare bar contact so the tell can be EARNED
        bool convergeCheat = false; // #422 /map?converge=1: seed enough of BOTH arcs to fire THE CONVERGENCE for a one-URL smoke test
        DeathCause? deathCheat = null; // #621 /map?death=<cause>: stage the REAL death at boot; the world you booted into decides the PLACE
        var revealCheats = new List<string>(); // /map?reveal=<bodyId> (repeatable): chart a hidden body at boot
        bool tableSceneCheat = false; // #746 /map?tablescene=1: boot the B1 canteen with the table scene in reach
        var uri = new Uri(Navigation.Uri);

        // #875 · /map?autowalk=1 IS RETIRED — still parsed, deliberately ignored. #729 built click-to-walk
        // behind this flag "until the owner rules on always-on"; the ruling landed on 2026-08-15 ("click to
        // walk should always be on when the arrows for walking are active also. The two should be linked as
        // alternative UI methods for walking"), so a deck click is a CONTROL of the game now and there is
        // nothing left for a URL to switch on. The parse survives as a NO-OP ALIAS so that every old dev
        // URL, every line of the testing guide and the UiGate's own boot still resolve to a world — and the
        // answer goes nowhere, because the only gate left is Map.Deck's TheCaptainsLegsAreTheirOwn, which
        // no query string can reach.
        _ = AutoWalk.EnabledIn(uri.Query);

        foreach (string pair in uri.Query.TrimStart('?').Split('&'))
        {
            if (pair.StartsWith("scenario=", StringComparison.OrdinalIgnoreCase))
            {
                string candidate = Uri.UnescapeDataString(pair["scenario=".Length..]);
                if (candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    scenarioName = candidate;
                }
            }
            else if (pair.StartsWith("start=", StringComparison.OrdinalIgnoreCase))
            {
                string candidate = Uri.UnescapeDataString(pair["start=".Length..]);
                if (StartPoints.Any(s => s.Id == candidate))
                {
                    startId = candidate;
                }
            }
            else if (pair.StartsWith("dock=", StringComparison.OrdinalIgnoreCase))
            {
                // #288 dev cheat: /map?dock=<haven-id> boots the ship already CLAMPED ON at that berth —
                // clean state, live services — so every dockable position smoke-tests without the long
                // navigate tax. Any dockable station haven works (DockableHavens; the full id list is
                // console-logged on boot and lives in docs/testing-guide.md), plus the friendly start
                // aliases (e.g. dock=ringside == dock=ringside-exchange). Validated once the world is built.
                string candidate = Uri.UnescapeDataString(pair["dock=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    dockCheat = candidate;
                }
            }
            else if (pair.StartsWith("fuel=", StringComparison.OrdinalIgnoreCase))
            {
                // #288 dev cheat: /map?fuel=N seeds the tank at boot (clamped to capacity), so a low-fuel
                // situation — the #262 "can I reach a pump?" test — is reachable in-situ without burning down.
                string candidate = Uri.UnescapeDataString(pair["fuel=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) && f >= 0)
                {
                    fuelCheat = f;
                }
            }
            else if (pair.StartsWith("credits=", StringComparison.OrdinalIgnoreCase))
            {
                // #288 dev cheat: /map?credits=N seeds the purse at boot, so a can-you-afford-it situation
                // (a fill-up, a bribe, an upgrade) is testable in-situ without grinding a run first.
                string candidate = Uri.UnescapeDataString(pair["credits=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) && c >= 0)
                {
                    creditsCheat = c;
                }
            }
            else if (pair.StartsWith("fetch=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?fetch=intel|active|picked injects the fetch mission at that stage so a
                // playtester can exercise each leg without the flights between. intel = the new first
                // stage (accepted, wreck hidden, tip in the ledger); active = post-scan (wreck charted,
                // backward-compatible); picked = charted + already lifted.
                string candidate = Uri.UnescapeDataString(pair["fetch=".Length..]).ToLowerInvariant();
                if (candidate is "intel" or "active" or "picked")
                {
                    fetchCheat = candidate;
                }
            }
            else if (pair.StartsWith("reveal=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?reveal=<bodyId> charts a hidden body straight away (repeatable).
                string candidate = Uri.UnescapeDataString(pair["reveal=".Length..]);
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    revealCheats.Add(candidate);
                }
            }
            else if (pair.StartsWith("crack=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?start=<station>&crack=active|picked injects the hatch-crack job at that
                // stage so a playtester can exercise the keypad / hand-off without taking the fetch first.
                string candidate = Uri.UnescapeDataString(pair["crack=".Length..]).ToLowerInvariant();
                if (candidate is "active" or "picked")
                {
                    crackCheat = candidate;
                }
            }
            else if (pair.StartsWith("tip=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?tip=route seeds a representative route tip (with provenance) into the
                // ledger so the Captain's-ledger Tips & intel rendering is reachable without walking a bar.
                string candidate = Uri.UnescapeDataString(pair["tip=".Length..]).ToLowerInvariant();
                if (candidate is "route")
                {
                    tipCheat = candidate;
                }
            }
            else if (pair.StartsWith("hoard=", StringComparison.OrdinalIgnoreCase))
            {
                // #223 dev cheat: /map?hoard=mine|rumor|both seeds the ledger's 🗺 section so the map
                // card and dig doors are reachable without flying a full bury run. mine = one of OUR
                // chests on Phobos; rumor = a bought rumour map to an NPC hoard; both = one of each.
                string candidate = Uri.UnescapeDataString(pair["hoard=".Length..]).ToLowerInvariant();
                if (candidate is "mine" or "rumor" or "both")
                {
                    hoardCheat = candidate;
                }
            }
            else if (pair.StartsWith("sling=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-G dev cheat: /map?sling=<bodyId> boots the ship on an inbound arc that already
                // has a close pass by that body ~12 days out, so the plot-desk ⤴ Sling panel is
                // reachable in seconds for testing.
                string candidate = Uri.UnescapeDataString(pair["sling=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    slingCheat = candidate;
                }
            }
            else if (pair.StartsWith("skim=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-I dev cheat: /map?skim=<bodyId> boots a fast hyperbolic inbound whose natural pass
                // grazes that body's cloud tops ~2 days out, so the plot-desk 🔥 Skim gauge is reachable
                // in seconds. Body must have an atmosphere (jupiter, earth, venus, saturn, titan).
                string candidate = Uri.UnescapeDataString(pair["skim=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    skimCheat = candidate;
                }
            }
            else if (pair.StartsWith("backroom=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-F dev cheat: /map?start=cinder-roost&backroom=open welds the V-06 back room open on
                // the spot; &backroom=quest stages the crack job (with its real code) so you can key the
                // pad yourself and watch the room grow. Testing is a feature (owner's rule).
                string candidate = Uri.UnescapeDataString(pair["backroom=".Length..]).ToLowerInvariant();
                if (candidate is "open" or "quest")
                {
                    backroomCheat = candidate;
                }
            }
            else if (pair.StartsWith("simhours=", StringComparison.OrdinalIgnoreCase))
            {
                // PR-F dev cheat: /map?simhours=N jumps the sim clock to N hours at boot, so the roaming
                // Magpie's rota (bar → gone → back room, 4 sim-hours a stop) can be sampled without
                // waiting or warping. e.g. simhours=0 bar, 5 gone, 9 back room.
                string candidate = Uri.UnescapeDataString(pair["simhours=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double h)
                    && h >= 0 && h < 1e6)
                {
                    simHoursCheat = h;
                }
            }
            else if (pair.StartsWith("ellipse=", StringComparison.OrdinalIgnoreCase))
            {
                // Kepler rails (PR-B) dev cheat: /map?ellipse=1 drops one visibly eccentric body onto
                // a sun orbit so the elliptical ring and its non-uniform tracking are checkable in the
                // browser. No effect on any shipped body — it's an extra body appended at load.
                string candidate = Uri.UnescapeDataString(pair["ellipse=".Length..]).ToLowerInvariant();
                ellipseCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("expedition=", StringComparison.OrdinalIgnoreCase))
            {
                // #370 dev cheat: /map?expedition=1 (scientists) or /map?expedition=mining (survey crew)
                // spawns an away-team gig ALREADY ACCEPTED, with its passing-rock site parked in shuttle
                // range at the berth, so the test loop is: spawn → shuttle door → take the team down → see
                // the away clock → come back. Documented in the PR body.
                string candidate = Uri.UnescapeDataString(pair["expedition=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes" or "science" or "mining")
                {
                    expeditionCheat = candidate;
                }
            }
            else if (pair.StartsWith("deflection=", StringComparison.OrdinalIgnoreCase))
            {
                // #394 dev cheat: /map?deflection=1 spawns the ASTEROID DEFLECTION gig ALREADY ACCEPTED — an
                // inbound rock on a collision rail with the Ringside Exchange, parked in shuttle range, ship
                // docked at Ringside. Pin the rock type with deflection=c|s|m (else seeded). The test loop is:
                // see the red threat line → shuttle to the rock → drill the charge → FIRE → watch the rail bend
                // off the station → home. Documented in the PR body.
                string candidate = Uri.UnescapeDataString(pair["deflection=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes" or "c" or "s" or "m")
                {
                    deflectionCheat = candidate;
                }
            }
            else if (pair.StartsWith("wreck=", StringComparison.OrdinalIgnoreCase))
            {
                // #488 dev cheat: /map?wreck=1 hangs a DERELICT in shuttle range off the berth. The test
                // loop is: shuttle door → board her → walk the spine → read the three evidence stations →
                // the cargo console → file the report (naming the cause) or strip her and say nothing.
                // She is seeded, so it is the same ship every time. Documented in docs/testing-guide.md.
                // …and ?wreck=<cause> (e.g. `infested`, `insurancejob`, `mutiny`) boards a wreck that died
                // THAT way on purpose, instead of re-rolling ids until the interesting one turns up.
                string candidate = Uri.UnescapeDataString(pair["wreck=".Length..]).ToLowerInvariant();
                wreckCheat = candidate is "1" or "true" or "yes";
                foreach (Derelict.WreckCause c in Enum.GetValues<Derelict.WreckCause>())
                {
                    if (candidate == c.ToString().ToLowerInvariant())
                    {
                        wreckCheat = true;
                        wreckCauseCheat = c;
                    }
                }
            }
            else if (pair.StartsWith("archive=", StringComparison.OrdinalIgnoreCase))
            {
                // Dev cheat: /map?archive=1&land=1 boards a derelict that is CARRYING A COLD-ARCHIVE NODE.
                // The whole beat — the dwell field, the throw, the visions, the handle — lives in one hold on
                // about one eligible wreck in three, and the house rule written next to these cheats is that
                // "a scene nobody can reach on demand is a scene that ships broken." So this boots the one
                // cause Core guarantees a node on (ArchiveCheatWreck): the ship one of her own opened to
                // space, where the node is the reason she died.
                //
                // It is deliberately NOT a "spawn a node anywhere" switch. The fiction the node belongs to
                // arrives with the hull; a node bolted into a drive failure would be a prop.
                string candidate = Uri.UnescapeDataString(pair["archive=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _archiveCheat = true;
                    wreckCheat = true;
                    wreckCauseCheat = ArchiveCheatCause;
                }
            }
            else if (pair.StartsWith("air=", StringComparison.OrdinalIgnoreCase))
            {
                // #564 dev cheat: /map?air=45 starts the excursion with 45 seconds in the tank instead of a
                // full one. A full tank is six minutes of walking by design — fine to play, useless to TEST,
                // and the owner should not have to stroll for six minutes to see the point-of-no-return
                // warning fire. Combine with dock/site/land:
                //   /map?dock=the-tilt&site=0&land=1&air=45
                string candidate = Uri.UnescapeDataString(pair["air=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double secs))
                {
                    _airCheatSeconds = Math.Clamp(secs, 1, SuitAir.TankSeconds);
                }
            }
            else if (pair.StartsWith("process=", StringComparison.OrdinalIgnoreCase))
            {
                // #696 dev cheat: /map?process=0 makes processing a document INSTANT — the darkroom hold
                // (photographing a sheet so it can be left, reading a paper as a clue) is twenty seconds of
                // standing still by design, which is the mechanic and is exactly the wrong thing to make a
                // story test sit through. Any other value sets the clock in sim seconds, so the feel itself
                // can be tuned from the URL without a rebuild. Combine with dock/site/land:
                //   /map?dock=the-tilt&site=0&land=1&process=0
                //
                // It is the CLOCK and nothing else. There is deliberately no switch here for what the hold
                // costs in air, because nothing in the game computes that: the hold passes sim time and the
                // suit prices sim time, and a cheat able to decouple them would be a second answer to the
                // one question this whole lane exists to leave in one place.
                string candidate = Uri.UnescapeDataString(pair["process=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double hold))
                {
                    _processCheatSeconds = Math.Max(0, hold);
                }
            }
            else if (pair.StartsWith("collectors=", StringComparison.OrdinalIgnoreCase))
            {
                // #583 dev cheat: /map?collectors=20 forces a repo boat to follow you down and puts it on the
                // ground 20 seconds in, whatever the heat gauge reads. The scene is meant to be RARE and
                // mid-mission — which makes it nearly impossible to playtest on purpose, and a scene nobody
                // can reach on demand is a scene that ships broken. Combine with dock/site/land:
                //   /map?dock=the-tilt&site=0&land=1&collectors=20
                string candidate = Uri.UnescapeDataString(pair["collectors=".Length..]);
                if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double eta))
                {
                    _collectorCheatSeconds = Math.Max(0, eta);
                }
            }
            else if (pair.StartsWith("death=", StringComparison.OrdinalIgnoreCase))
            {
                // #621 dev cheat: /map?death=<cause> KILLS THE CAPTAIN AT BOOT, through the real pipeline.
                //
                // The death card is the one screen every player is guaranteed to see, and until now there
                // was no way to reach any of it on demand: the routes were ?floor=2&air=10 (walk until you
                // suffocate), ?reevers=8 (survive long enough to be overdrawn) and ?collectors=20 (lose the
                // Bolivia). Four causes, five stages, four places, one seeded line pool each — verified by
                // reading the source. This project's own rule, written beside these cheats: "a scene nobody
                // can reach on demand is a scene that ships broken."
                //
                // It stages the GENUINE trigger — TriggerSurfaceOverdrawDeath / TriggerImpact / a real
                // collector catch — never a mocked card, so what you see is what a player sees: the real
                // four-stage freeze beat, the real seeded narration, the real resurrection.
                //
                // There is deliberately NO ?place= parameter. WHERE you died is not an opinion the URL gets
                // to hold: the excursion's own floor and body id decide it, which is the classifier #609 was
                // filed about, and a cheat that could override it would be a second source of truth for the
                // exact fact that has now cost three death cards. You choose the place by booting into it:
                //   /map?death=impact                                   own ship
                //   /map?death=collector                                own ship (the BUSTED ladder)
                //   /map?death=suffocated&dock=the-tilt&land=1          landing party
                //   /map?death=reevers&wreck=1&land=1                   derelict
                //   /map?death=suffocated&secretlab=1&land=1&floor=2    underground
                string candidate = Uri.UnescapeDataString(pair["death=".Length..]).ToLowerInvariant();
                foreach (DeathCause c in Enum.GetValues<DeathCause>())
                {
                    if (candidate == c.ToString().ToLowerInvariant())
                    {
                        deathCheat = c;
                    }
                }
            }
            else if (pair.StartsWith("floor=", StringComparison.OrdinalIgnoreCase))
            {
                // #585 dev cheat: /map?secretlab=1&land=1&floor=3 rides you straight down to B3.
                //
                // Owner: "instruct to put the debug cheat start next to the lab so that it can be really
                // tested without playing to find it" / "I mean next to the elevator shaft". ?secretlab= now
                // sets you down AT the shed; this goes the rest of the way, because half the open work on
                // this feature is about what a FLOOR looks like, and riding four cars to reach B4 every time
                // is the same tax one level down.
                //
                // Positive number, read as a depth: floor=3 means B3. Clamped to the site's own bottom, so a
                // shallow facility cannot be asked for a floor it does not have.
                string candidate = Uri.UnescapeDataString(pair["floor=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int deep)
                    && deep > 0)
                {
                    _startingFloorCheat = -deep;
                }
            }
            else if (pair.StartsWith("outpost=", StringComparison.OrdinalIgnoreCase))
            {
                // #563 dev cheat: /map?outpost=1 guarantees the OUTPOST HUT on whatever site the excursion
                // lands on, so the lane can be playtested without hunting for a site that seeded one. Three
                // sites in four carry a hut anyway; this just removes the hunt. Combine with dock/site/land,
                // e.g. /map?dock=the-tilt&site=0&land=1&outpost=1 puts you on the regolith with one out there.
                string candidate = Uri.UnescapeDataString(pair["outpost=".Length..]).ToLowerInvariant();
                _outpostCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("kit=", StringComparison.OrdinalIgnoreCase))
            {
                // #774 dev cheat: /map?kit=1 assembles the FIELD DOSSIER (#588) on the FIRST piece of
                // somebody's kit this excursion turns up, and with every sentence it can carry.
                //
                // It exists because the assembly is the rarest beat on the regolith and its full form is
                // rarer still: three papers rooms inside one excursion at one room in eight, and then two
                // more one-in-three rolls for what the family knows and the in that fell out of the kit.
                // That four-sentence version is the scene #774 is about, and nobody could reach it on demand
                // to look at it — which is this file's own rule about a scene that ships broken.
                //
                // It moves those GATES and nothing else: the stranger, the family, the hint, the in and the
                // moon they name are the seeded ones for the room you actually completed, so what a tester
                // reads is a dossier a captain can genuinely be handed.
                //
                //   /map?dock=the-tilt&site=0&land=1&outpost=1&kit=1
                //   …walk to the hut and press E on SOMEBODY'S EFFECTS.
                string candidate = Uri.UnescapeDataString(pair["kit=".Length..]).ToLowerInvariant();
                _kitCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("dark=", StringComparison.OrdinalIgnoreCase))
            {
                // #708 dev cheat: /map?dark=1 puts the fixtures out on every floor of this excursion, so the
                // suit's headlights are the whole of the seeing. Nothing the game ships declares itself dark
                // yet — the found halls (#677) will be the first, and they are not built — and a feature that
                // nobody can reach on demand is a feature that ships broken. So this is the door to it.
                //
                // It changes ONE fact and nothing else: what UndergroundComplex.IsDark answers. Collision,
                // air, the pack, the sentries, the tracker and every gate down there behave exactly as they
                // do with the lights on, because none of them are told. You can walk into what you cannot
                // see, and something you cannot see can walk into you.
                //
                //   /map?secretlab=deep&land=1&floor=4&dark=1
                string candidate = Uri.UnescapeDataString(pair["dark=".Length..]).ToLowerInvariant();
                _lampsOutCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("book=", StringComparison.OrdinalIgnoreCase))
            {
                // #701 dev cheat: /map?book=N puts THE ODD BOOK in every would-be-empty room this excursion
                // searches, so all ten authored entries can be read on demand instead of hunted for at one
                // would-be-empty room in six. A scene nobody can reach on demand is a scene that ships
                // broken, and this one is deliberately rare.
                //
                //   ?book=1 … ?book=10   force that catalog entry (1 = the oldest sea story, 10 = the fat
                //                        paperback) — the way to read the whole shelf in one walk
                //   ?book=on|all|any     force the SEEDED entry, i.e. the shipped selection with the
                //                        one-in-six gate taken off — the way to see the weighting work
                //
                //   /map?secretlab=deep&land=1&floor=2&book=9
                //
                // What it deliberately does NOT do is invent a book in an OCCUPIED room. The rule is that a
                // book is what a would-be-empty room has instead of the empty line; a cheat that laid one on
                // top of a pallet would have the tester playtesting a room the game cannot produce.
                string candidate = Uri.UnescapeDataString(pair["book=".Length..]).ToLowerInvariant();
                if (candidate is "on" or "all" or "any" or "true" or "yes")
                {
                    _bookCheat = 0;
                }
                else if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture,
                             out int entry)
                         && entry >= 1 && entry <= Core.OddBooks.Catalog.Count)
                {
                    _bookCheat = entry;
                }
            }
            else if (pair.StartsWith("watchers=", StringComparison.OrdinalIgnoreCase))
            {
                // #649 dev cheat: /map?watchers=1 makes the monolith's ground ATTENTIVE this visit and cuts
                // the dwell from forty seconds to two, so the strange-things-happen beat can be watched on
                // demand instead of hunted for. It is rare by design — one visit-window in three, and then
                // only if you stand still at the stone — which makes it precisely the shape of scene this
                // file's own rule is about: "a scene nobody can reach on demand is a scene that ships
                // broken." It changes the GATES and nothing else: the variant roll and the (zero) cost are
                // the ones a captain gets, so what a tester sees is what a captain sees.
                //
                //   /map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1
                //   …and with a pack on the field, for the variant that needs one:
                //   /map?dock=the-space-bar&body=phobos&site=0&land=1&watchers=1&reevers=3
                string candidate = Uri.UnescapeDataString(pair["watchers=".Length..]).ToLowerInvariant();
                _watchersCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("arrivalphase=", StringComparison.OrdinalIgnoreCase))
            {
                // #742 dev cheat: /map?kaamos=hq&arrivalphase=N winds the sim clock to arrival phase N of
                // the ice moon's own orbit BEFORE the head-office park is built, so the ride lets her go on
                // the phase you named instead of whichever one boot-time happened to be.
                //
                // It exists because #742 was a ONE-IN-24 bug: the window opens every 40 days and stands
                // open for two, so the arrival phase is free, and phase 2/24 (epoch 9,866 s) was the one
                // that put the parked hull into Enceladus at +9.54 h while the captain was 23 floors down.
                // Nobody could reach that on demand — you booted, got phase 0, saw nothing wrong, and
                // shipped it. This is the door to the bad one.
                //
                //   /map?kaamos=hq&arrivalphase=2           park on the phase that used to lithobrake
                //   /map?kaamos=hq&arrivalphase=2&land=1&floor=23   …and go down the lift while she holds
                //
                // 0…23; anything else is ignored rather than clamped, because a silently-corrected phase
                // index is a tester reading the wrong row of the sweep.
                string candidate = Uri.UnescapeDataString(pair["arrivalphase=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ph)
                    && ph >= 0 && ph < CyclerWindow.ArrivalPhases)
                {
                    _arrivalPhaseCheat = ph;
                }
            }
            else if (pair.StartsWith("found=", StringComparison.OrdinalIgnoreCase))
            {
                // #677 dev cheat: /map?found=1 parks the one rock in the game whose site has a band nobody
                // dug under a band nobody listed, sets you down at the lift head, and puts every authority
                // this site ever issued in your wallet — including the last one, which is the way past the
                // seam. About one site in fifty has halls, and the way in is a card somebody left in a room
                // eleven floors down; "a scene nobody can reach on demand is a scene that ships broken" has
                // never applied harder than it does here.
                //
                // It implies ?secretlab=1, because there is no other way down. Pair it with ?floor=N to ride
                // straight to a gallery: /map?found=1&land=1&floor=17.
                //
                // What it deliberately does NOT do is invent a band. The rock's whole shape — its depth, its
                // kinds, its hidden band and its halls — is seeded off its body id like every other site in
                // the system, so what a tester walks is exactly what a captain would walk if they found it.
                string candidate = Uri.UnescapeDataString(pair["found=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _foundCheat = true;
                    secretlabCheat = true;
                }
            }
            else if (pair.StartsWith("card=", StringComparison.OrdinalIgnoreCase))
            {
                // #693 dev cheat: /map?card=next puts ONE authority in the wallet — the one the gate in front
                // of you reads.
                //
                // The #692 report ended with the honest note that its own hero row could not be playtested:
                // "reaching the row needs an authority card in the wallet and no dev cheat mints one." The
                // only thing that did was ?found=1, which also parks a different rock and hands over the
                // WHOLE wallet — so the carded lift row, the gate refusal one band lower, and the accepted
                // beat when the doors open have never been reachable on an ordinary site at all. A scene
                // nobody can reach on demand is a scene that ships broken.
                //
                //   ?card=next    the band under wherever you are set down — the gate you are standing at
                //   ?card=N       band N specifically, for the ladder (a card that opens the WRONG gate is
                //                 the refusal line, and it has its own guard and no way to see it)
                //   ?card=all     every band the site has, which is ?found=1's wallet on any rock
                //
                //   /map?secretlab=deep&land=1&floor=1&card=next
                //
                // The issue proposed ?card=<bodyId>#<band>, which is the card's own id — and that is exactly
                // what this must NOT take. A body typed into a URL is a body the landing may not be on, and a
                // band typed for it is one that site may not have; the cheat would mint a card no gate on the
                // ground reads and the tester would be playtesting an empty pocket. So it names a BAND, it is
                // minted for the body actually landed on, and a band the site does not have mints nothing and
                // says so. (Also: '#' ends a URL — the id form cannot survive a query string anyway.)
                //
                // It implies ?secretlab=1, because a wallet is only worth anything where there is a lift, and
                // it never chooses the rock: pair it with ?secretlab=deep or ?found=1 for those grounds.
                string candidate = Uri.UnescapeDataString(pair["card=".Length..]).ToLowerInvariant();
                if (candidate is "next" or "all"
                    || (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out int band) && band >= 0))
                {
                    _cardCheat = candidate;
                    secretlabCheat = true;
                }
            }
            else if (pair.StartsWith("tablescene=", StringComparison.OrdinalIgnoreCase))
            {
                // #746 dev cheat: /map?tablescene=1 boots THE TABLE SCENE — the B1 canteen of a deep site,
                // with people in it, one URL from the front door.
                //
                // "A scene nobody can reach on demand is a scene that ships broken", and this one is behind
                // more doors than anything else we have shipped: find a rock with a lab, land on it, find the
                // shed, ride the lift, walk to the canteen, find a table with one of THREE regulars at it. So
                // it implies the whole route (?secretlab=deep&land=1&floor=1) rather than adding a fourth
                // spelling of it. (#875: it used to turn ?autowalk=1 on too, because the last leg is a walk
                // across a room. Every boot has the click now, so there is nothing left to turn on.)
                //
                // It does NOT force who is at the tables. The rota is seeded off the site and the watch like
                // any other shift (#709) — a cheat that seated the Hand for you would be testing a room that
                // does not ship. If this watch has no Hand in it, that IS the room: come back next shift, or
                // reload for a different one.
                //
                // #757 · …and `?tablescene=free` is the SAME route with a different last step: it stands you
                // at a top with NOBODY at it, which is the table the owner could not sit down at ("I have
                // empty table but I cannot sit down"). Same room, same rota, same watch — the only thing the
                // cheat chooses is which of the room's own tops you are standing at when it lets go.
                string candidate = Uri.UnescapeDataString(pair["tablescene=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes" or "free")
                {
                    tableSceneCheat = true;
                    _freeTableCheat = candidate == "free";
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the top pressurised floor, where the owner put them
                }
            }
            else if (pair.StartsWith("spread=", StringComparison.OrdinalIgnoreCase))
            {
                // #784 dev cheat: /map?spread=1 is ?tablescene=free with the last three legs walked — a
                // CABINET top instead of a hall top, the captain already sat down in it, and three finds
                // already in the sleeve. Owner's own ask: "we probably need a start point where we have
                // things in our inventory we can process (when our HUD UI state is sitting down with enough
                // privacy)."
                //
                // It implies the canteen's whole route rather than spelling a seventh one, exactly as
                // ?tablescene= and ?counter= do. It forces nothing about the room: the watch, the rota and
                // which cabinet is free are whatever the building says.
                string candidate = Uri.UnescapeDataString(pair["spread=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _spreadCheat = true;
                    tableSceneCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the only floor with a hall and cabinets on it
                }
            }
            else if (pair.StartsWith("rip=", StringComparison.OrdinalIgnoreCase))
            {
                // #798 dev cheat: /map?rip=1 is ?spread=1's route with the last leg walked to a BIN rather
                // than into a cabinet — three finds in the sleeve, the slop bin at arm's length, and the
                // whole verb two presses from the front door. Owner: "those trash cans are needed so we get
                // rid of the processed materials without connecting them to us too clearly, like leaving
                // them to the table."
                //
                // It implies the canteen's whole route rather than spelling an eighth one, exactly as
                // ?spread= implies ?tablescene=. It forces nothing about the room: which bin, where it
                // stands and what is stencilled on it are the building's own answers.
                string candidate = Uri.UnescapeDataString(pair["rip=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _ripCheat = true;
                    tableSceneCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the hall, which is the floor the CHOICE is on
                }
            }
            else if (pair.StartsWith("threads=", StringComparison.OrdinalIgnoreCase))
            {
                // #741 dev cheat: /map?threads=1 is ?spread=1 with a CASE ALREADY IN THE BOOK — six entries
                // from two grounds the captain is not standing on, with a rhyme in them a human eye can
                // catch. The pen is worth nothing against an empty book, and the whole feature is unreachable
                // on demand without one: a book with two grounds in it is a real excursion's worth of play.
                //
                // It implies ?spread=1 rather than spelling the route again, exactly as ?spread= implies
                // ?tablescene=. It forces nothing about the case: no line is drawn and nothing is marked,
                // because spotting is the player's act.
                string candidate = Uri.UnescapeDataString(pair["threads=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _threadsCheat = true;
                    _spreadCheat = true;
                    tableSceneCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the only floor with a hall and cabinets on it
                }
            }
            else if (pair.StartsWith("patrol=", StringComparison.OrdinalIgnoreCase))
            {
                // #804 dev cheat: /map?patrol=1 boots ONTO A RESTRICTED FLOOR WITH A ROUND ON IT — B2 of a
                // deep site, which is the shallowest floor below the bar and therefore the shallowest floor
                // anybody walks. ?patrol=2 forces the two-guard watch, which is otherwise a coin flip and is
                // the harder scene to time.
                //
                // It implies the whole route (?secretlab=deep&land=1&floor=2) rather than spelling an eighth
                // one, exactly as ?tablescene= and ?counter= do. It forces nothing else: which stops the
                // round walks, which direction it runs and who is on it are whatever the watch says, because
                // a cheat that pinned the beat would be testing a floor that does not ship.
                string candidate = Uri.UnescapeDataString(pair["patrol=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _patrolCheat = 1;
                }
                else if (candidate == "2")
                {
                    _patrolCheat = 2;
                }
                if (_patrolCheat is not null)
                {
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -2;   // B2 — the first floor under the bar, and the first with a round
                }
            }
            else if (pair.StartsWith("badge=", StringComparison.OrdinalIgnoreCase))
            {
                // #804 dev cheat: /map?badge=1 mints THIS SITE'S OWN PASS into the wallet at boot, so the
                // satisfied arm of the challenge is one URL away instead of behind the whole cage-crew lane
                // (find the bar, find the Hand, roll the ask, ride the cage). It implies ?patrol=1's route,
                // because a pass with nobody to show it to is not a thing anybody can test.
                //
                // The MINTING is the only thing it does. The guard still has to see you, the wallet is still
                // read by Core, and what is said is what would have been said had the pass been earned.
                string candidate = Uri.UnescapeDataString(pair["badge=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _badgeCheat = true;
                    _patrolCheat ??= 1;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -2;
                }
            }
            else if (pair.StartsWith("counter=", StringComparison.OrdinalIgnoreCase))
            {
                // #756 dev cheat: /map?counter=1 boots THE COUNTER — the B1 cantina hall of a deep site,
                // with the captain standing at the service spot, one URL from the front door.
                //
                // Owner's standing rule for every new feature ("testing is a feature"), and this one has the
                // longest walk in the game in front of it: find a rock with a lab, land, find the shed, ride
                // the lift, cross a hall the size of a hangar. So it implies the whole route the same way
                // ?tablescene=1 does rather than inventing a fifth spelling of it — the only difference is
                // which fixture the last leg ends at.
                //
                // It forces nothing about the room. The watch, the rota and the purse are whatever the boot
                // gave you: a cheat that handed you the coin would be testing a counter that does not ship.
                string candidate = Uri.UnescapeDataString(pair["counter=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _counterCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the hall, which is the only floor with a counter on it
                }
            }
            else if (pair.StartsWith("stool=", StringComparison.OrdinalIgnoreCase))
            {
                // #756 dev cheat: /map?stool=1 is ?counter=1 with the last, last leg walked — the card is
                // open AND you are up on a stool, which is the posture the issue is about.
                //
                // It implies the counter's whole route rather than spelling a sixth one, for the same reason
                // ?counter=1 implies ?secretlab=deep&land=1&floor=1: the walk in front of this feature is the
                // longest in the game, and a tester who has to make it by hand will not make it twice.
                string candidate = Uri.UnescapeDataString(pair["stool=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _stoolCheat = true;
                    _counterCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the hall, which is the only floor with a counter on it
                }
            }
            else if (pair.StartsWith("neighbour=", StringComparison.OrdinalIgnoreCase)
                || pair.StartsWith("neighbor=", StringComparison.OrdinalIgnoreCase))
            {
                // #756 dev cheat: /map?neighbour=1 makes the next WAIT on a stool turn the one beside you;
                // /map?neighbour=0 means nobody ever does. ?approach=1's sibling — both spellings taken,
                // because the owner writes one and this codebase writes the other.
                //
                // BOTH HALVES ARE THE FEATURE, exactly as at the tables: a counter where the seat beside you
                // stays quiet is the thing the room is saying. And it is even less reachable by luck here
                // than at a top, because the roll sits behind a seeded OCCUPANCY as well as a seeded die.
                //
                // It forces WHETHER and never WHO or WHAT: her ladder and her lines are the ones a captain
                // would get.
                string candidate =
                    Uri.UnescapeDataString(pair[(pair.IndexOf('=') + 1)..]).ToLowerInvariant();
                _neighbourCheat = candidate switch
                {
                    "1" or "true" or "yes" or "now" => true,
                    "0" or "false" or "no" or "never" => false,
                    _ => null,
                };
            }
            else if (pair.StartsWith("park=", StringComparison.OrdinalIgnoreCase))
            {
                // #759 dev cheat: /map?park=1 boots THE PARK — the same B1 route as ?counter=1, with the
                // last leg walked through the gate at the end of the hall's own corridor instead of to the
                // counter. Owner's standing rule ("testing is a feature"), and this room needs it more than
                // most: the park is on the far side of the largest room in the game, behind a wall you can
                // see through and cannot walk through, and finding it by accident takes a while.
                string candidate = Uri.UnescapeDataString(pair["park=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _parkCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the only floor in the building with a park behind it
                }
            }
            else if (pair.StartsWith("frontdoor=", StringComparison.OrdinalIgnoreCase))
            {
                // #775 dev cheat: /map?frontdoor=1 boots the same B1 route and stops one room SHORT — out
                // on the MAIN CORRIDOR, standing at the hall's own front door. The owner's complaint was
                // that you had to go looking for the way in, so the row that proves the fix has to start
                // OUTSIDE: a cheat that set the tester down inside the bar would be showing the wrong half
                // of it.
                string candidate = Uri.UnescapeDataString(pair["frontdoor=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _frontDoorCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the floor the hall is on
                }
            }
            else if (pair.StartsWith("parkwalk=", StringComparison.OrdinalIgnoreCase))
            {
                // #775 dev cheat: /map?parkwalk=1 boots the same B1 route and stands the captain on the
                // MAIN CORRIDOR at the mouth of the GARDEN WALK. ?park=1 sets a tester down inside the
                // green, which is the wrong half of the owner's ask: "a kind of place people like to walk
                // through on their way" is about the CROSSING, and a crossing has to be started outside.
                string candidate = Uri.UnescapeDataString(pair["parkwalk=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _parkWalkCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;
                }
            }
            else if (pair.StartsWith("ringoffice=", StringComparison.OrdinalIgnoreCase))
            {
                // #813 dev cheat: /map?ringoffice=1 boots the same B1 route and stands the captain INSIDE
                // one of the rooms that FACES the park, a few paces back from its own window wall. Every
                // other park row in the guide puts a tester on the gravel, which is the side of the glass
                // the game has always shown; the Manhattan ruling's claim ("the park prime real estate is
                // not wasted") is a claim about the rooms that paid for the view, and there was no URL that
                // put you in one.
                string candidate = Uri.UnescapeDataString(pair["ringoffice=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _ringOfficeCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;   // B1 — the only floor in the building with a block on it
                }
            }
            else if (pair.StartsWith("goodscar=", StringComparison.OrdinalIgnoreCase))
            {
                // #801 dev cheat: /map?goodscar=1 boots the same B1 route and walks the last leg to the
                // SECOND CAR, at the blind end of the main corridor. A feature whose whole point is that
                // there is another way off this floor is a feature nobody finds unless the route to it is
                // one URL — and the cage's own console is a hundred and seventy du the other way.
                string candidate = Uri.UnescapeDataString(pair["goodscar=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _goodsCarCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;
                }
            }
            else if (pair.StartsWith("parkback=", StringComparison.OrdinalIgnoreCase))
            {
                // #801 dev cheat: /map?parkback=1 boots inside the park and stands the captain on the
                // GRAVEL IN FRONT OF THE FAR WALL, facing the back-of-house doors. ?park=1 sets a tester
                // down at the gate looking down the room, which is the half of the park that was never the
                // problem: the owner's note is about the far side.
                string candidate = Uri.UnescapeDataString(pair["parkback=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _parkBackCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;
                }
            }
            else if (pair.StartsWith("freight=", StringComparison.OrdinalIgnoreCase))
            {
                // #775 dev cheat: /map?freight=1 walks the last leg to the GOODS HOIST instead — the one
                // fixture in the room the captain is refused, and that refusal is a PLATE rather than an
                // absence (#757's lesson), so it has to be stood in front of before it says anything.
                string candidate = Uri.UnescapeDataString(pair["freight=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _freightCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;
                }
            }
            else if (pair.StartsWith("designate=", StringComparison.OrdinalIgnoreCase))
            {
                // #803 dev cheat: /map?designate=1 is the freight boot with the whole manual-fire loop rigged
                // at it — a gun set down beside you one round short of a hasp, and a hut's find in the
                // pocket. The scenario the owner described (a few found rounds, and a lock worth them) is
                // otherwise a lift ride and two rooms apart from itself.
                string candidate = Uri.UnescapeDataString(pair["designate=".Length..]).ToLowerInvariant();
                if (candidate is "1" or "true" or "yes")
                {
                    _designateCheat = true;
                    _freightCheat = true;
                    secretlabCheat = true;
                    secretlabDeep = true;
                    _landCheat = true;
                    _startingFloorCheat = -1;
                }
            }
            else if (pair.StartsWith("approach=", StringComparison.OrdinalIgnoreCase))
            {
                // #757 dev cheat: /map?approach=1 makes the next WAIT at a table you took alone bring
                // somebody over; /map?approach=0 means nobody ever comes.
                //
                // Both halves are the feature. Whether anybody crosses the room is a seeded roll at one top
                // on one shift, so without this the somebody-comes beat is reachable only by luck and the
                // told nobody-came outcome is reachable only by more of it. Owner's own framing, again:
                // "testing is a feature", and #693's rule that a scene nobody can reach on demand is a scene
                // that ships broken.
                //
                // It forces WHETHER and never WHO or WHAT: the ladder, her lines and what she came over for
                // are the ones a captain gets, because a cheat that showed a different scene would be worse
                // than no cheat at all.
                string candidate = Uri.UnescapeDataString(pair["approach=".Length..]).ToLowerInvariant();
                _approachCheat = candidate switch
                {
                    "1" or "true" or "yes" or "now" => true,
                    "0" or "false" or "no" or "never" => false,
                    _ => null,
                };
            }
            else if (pair.StartsWith("hurt=", StringComparison.OrdinalIgnoreCase))
            {
                // #784 dev cheat: /map?hurt=N puts N of CaptainCondition's five blows on the captain when
                // the excursion starts — the OTHER half of the short rest, and the half that is invisible on
                // an unmarked captain for the same reason as above.
                string candidate = Uri.UnescapeDataString(pair["hurt=".Length..]);
                if (int.TryParse(candidate, System.Globalization.NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out int blows)
                    && blows >= 0 && blows < CaptainCondition.MaxHits)
                {
                    _hurtCheat = blows;     // never MaxHits: booting a tester into a death card is not a demo
                }
            }
            else if (pair.StartsWith("shelter=", StringComparison.OrdinalIgnoreCase))
            {
                // #728 dev cheat: /map?shelter=1 sets the boots down AT A SHELTER — the one building on the
                // ground that fills a tank and fills a magazine, and the fixture pair the owner could not
                // tell apart in the smoke run.
                //
                // It exists because the shelter is DEEP in the field by design (SurfaceShelter.PlacesOn keeps
                // it out of the landing band on purpose) so every look at its plates, its receipts and the
                // magazines readout above them cost a two-minute walk across 310 x 260 du of regolith. Same
                // ruling as ?secretlab=1's doorstep drop: the hunt is the game, and it is exactly what must
                // not stand between a developer and the thing under test.
                //
                // It moves ONE fact — where you are standing — and stands you OUTSIDE the door, so the
                // proximity cycle, the arrival line, the pressure crossing and the walk to each console are
                // all exercised the way a captain meets them.
                //
                //   /map?dock=the-tilt&site=0&land=1&shelter=1&mags=12
                string candidate = Uri.UnescapeDataString(pair["shelter=".Length..]).ToLowerInvariant();
                _shelterCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("mags=", StringComparison.OrdinalIgnoreCase))
            {
                // #728 dev cheat: /map?mags=N brings the sling's sentries down holding N rounds each.
                //
                // Every one of them lands full (SentryBot.MaxMagazine) on a fresh ship, so the magazines
                // readout, the shelter press's receipt and the locker's two refusals could only ever be
                // looked at after a real firefight. It sets the ONE number and nothing else: the roster, the
                // ammunition kind, the drain and every law downstream are the shipped ones.
                //
                //   ?mags=0 … ?mags=99   what each sentry is holding when the shuttle sets you down
                string candidate = Uri.UnescapeDataString(pair["mags=".Length..]);
                if (int.TryParse(candidate, System.Globalization.NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out int rounds)
                    && rounds >= 0 && rounds <= SentryBot.MaxMagazine)
                {
                    _magazineCheat = rounds;
                }
            }
            else if (pair.StartsWith("watch=", StringComparison.OrdinalIgnoreCase))
            {
                // #751 dev cheat: /map?watch=N pins which SHIFT the Hive's canteen is on.
                //
                // The whole of #751's watch-density design is a room that heaves at one hour and echoes at
                // another with nothing anywhere announcing which — which is exactly the kind of feature a
                // tester cannot see without waiting four sim-hours per look. Owner's own framing, twice
                // over: "testing is a feature".
                //
                // It pins the WATCH INDEX and nothing else. Who is in the room and where they sat are still
                // the rota's own answer for that shift (#709), so what a tester walks into is the room a
                // captain would get on that shift — never a rigged one.
                string candidate = Uri.UnescapeDataString(pair["watch=".Length..]);
                if (long.TryParse(candidate, System.Globalization.NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out long pinned) && pinned >= 0)
                {
                    _watchCheat = pinned;
                }
            }
            else if (pair.StartsWith("roll=", StringComparison.OrdinalIgnoreCase))
            {
                // #746 dev cheat: /map?roll=hi forces every encounter band to YES, /map?roll=lo to NO-AND.
                // Owner's own framing of it in the issue: "testing is a feature".
                //
                // It overrides the BAND and never the roll. The dice still cast, the modifier stack still
                // reads truthfully on screen, and the scene that plays is the scene a captain would get —
                // a cheat that showed you a different scene would be worse than no cheat at all.
                string candidate = Uri.UnescapeDataString(pair["roll=".Length..]).ToLowerInvariant();
                _rollCheat = candidate switch
                {
                    "hi" or "high" or "yes" => Encounter.Band.Yes,
                    "mid" or "but" => Encounter.Band.YesBut,
                    "lo" or "low" or "no" => Encounter.Band.NoAnd,
                    _ => null,
                };
            }
            else if (pair.StartsWith("secretlab=", StringComparison.OrdinalIgnoreCase))
            {
                // #409 dev cheat: /map?secretlab=1 spawns a plain LANDABLE rock parked in shuttle range at the
                // berth whose surface is GUARANTEED to hide one of Dr. Vantar's secret labs, with the hidden
                // door ALREADY REVEALED (a ⚙ HIDDEN DOOR console on the ground). The test loop is: shuttle
                // door → land → walk to the door → force it → read the logs → hit the core-log reveal.
                // Documented in the PR body. (Ordinary bodies hide labs rarely, off the seed — this is the
                // fast path.)
                string candidate = Uri.UnescapeDataString(pair["secretlab=".Length..]).ToLowerInvariant();
                secretlabCheat = candidate is "1" or "true" or "yes" or "deep";


                // #592 · ?secretlab=deep parks a rock whose site HAS a band nobody listed. The ordinary
                // cheat rock's site is seeded like any other and happens to be four floors of records annex
                // with nothing under it, so #592 could not be reached from a URL at all — which is the exact
                // tax these cheats exist to remove.
                secretlabDeep = candidate is "deep";
            }
            else if (pair.StartsWith("body=", StringComparison.OrdinalIgnoreCase))
            {
                // #585 dev cheat: /map?body=phobos&site=2&land=1 lands on THAT body's site 2, whatever is
                // nearest the berth. Owner: "let's go over all the sites we have not yet tested with the
                // url-arguments" — and until now that was impossible for most of them. ?land=1 takes the
                // first landable body in shuttle reach, so from the-tilt every URL in the world reaches
                // Miranda and nowhere else. Two thirds of the grounds we have just rebuilt had no way to be
                // opened and looked at, which for this project is the same as having no way to be tested:
                // "boot every scene and check all the parts are in the right place".
                string candidate = Uri.UnescapeDataString(pair["body=".Length..]).ToLowerInvariant();
                if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                {
                    _forcedLandingBodyId = candidate;
                }
            }
            else if (pair.StartsWith("site=", StringComparison.OrdinalIgnoreCase))
            {
                // #320 dev cheat: /map?site=N pre-selects landing site N in the boarding panel, so a
                // playtester can board straight onto a specific ground and compare site A vs site B → a
                // visibly different surface deck-plan on the same body. Clamped to the body's real 2–4 set
                // when the panel opens. Documented in docs/testing-guide.md.
                string candidate = Uri.UnescapeDataString(pair["site=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int siteN) && siteN >= 0)
                {
                    _forcedSiteIndex = siteN;
                }
            }
            else if (pair.StartsWith("land=", StringComparison.OrdinalIgnoreCase))
            {
                // #464 dev cheat: /map?land=1 rides the shuttle down as soon as the world is ready, onto the
                // first landable body in reach (honouring ?site=N). The real BeginSurfaceExcursion and the
                // real descent — it skips only the walk to the hatch and the boarding panel, so a surface
                // playtest is one URL instead of two minutes of walking. Owner, 2026-07-27: "It is not ready
                // until it is playtested in the browser."
                // …and ?land=<bodyId> lands on a NAMED body instead of whatever happens to be nearest.
                // Owner: "We should test those sites with direct opens to them via URL parameters to find out
                // the usual issues." He is right that this was the gap: ?land=1 takes the first landable thing
                // in reach, so aiming at a particular ground meant re-rolling berths until it came up — which
                // is exactly the friction that stops scenes being booted, and booting scenes is how the bugs
                // in this repo actually get found.
                string candidate = Uri.UnescapeDataString(pair["land=".Length..]).ToLowerInvariant();
                _landCheat = candidate.Length > 0;
                _landBodyCheat = candidate is "1" or "true" or "yes" ? null : candidate;
            }
            else if (pair.StartsWith("kaamos=", StringComparison.OrdinalIgnoreCase))
            {
                // #411 dev cheat: /map?kaamos=N assembles the first N PROJEKTI KAAMOS fragments (canonical
                // order), /map?kaamos=all assembles every one — so the Captain's-ledger readout, its state
                // transitions, and the one-time reach notice are all reachable without a full playthrough.
                //
                // Those GRANT the fragments. Two of the six could only ever be granted, because their real
                // delivery is deliberately rare: the cold pod is one seeded probe square in seventeen on one
                // of seven outer moons, and the berth-holder drinks at a given bar roughly one watch in four.
                // So /map?kaamos=pod puts the pod under whatever ground this excursion lands on, and
                // /map?kaamos=holder seats the holder at whatever bar this captain docks at — the two beats
                // become playable on demand instead of merely grantable ("a scene nobody can reach on demand
                // is a scene that ships broken", and a granted shard proves nothing about the scene that
                // hands it over). Combine freely: /map?kaamos=holder&dock=ringside-exchange.
                string candidate = Uri.UnescapeDataString(pair["kaamos=".Length..]).ToLowerInvariant();
                if (candidate is "all" or "pod" or "holder" or "bounce" or "hq"
                    || int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    kaamosCheat = candidate;
                }
            }
            else if (pair.StartsWith("bond=", StringComparison.OrdinalIgnoreCase))
            {
                // #429 dev cheat: /map?bond=1 boots docked at a bar (default The Space Bar, override with
                // ?dock=<id>) and FORCES the next ambient scare (shudder/buzzer/PA) to open a STRANGER-BOND —
                // a co-present stranger stands you a cognac (OLD PERIHELION), the hero beat. Documented in
                // docs/testing-guide.md.
                string candidate = Uri.UnescapeDataString(pair["bond=".Length..]).ToLowerInvariant();
                bondCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("oracle=", StringComparison.OrdinalIgnoreCase))
            {
                // #428 dev cheat: /map?oracle=1 boots docked at a bar (default The Space Bar, override with
                // ?dock=<id>) and SEATS the station oracle — Solenne "Static" Marsh (#425/#427) — in her
                // port-back corner, whatever her rota says this watch. She is a fixture only ~55% of watches
                // (OracleRant.PresenceChance), so the whole scene — the rant, the drink that widens the
                // channel, the room-goes-quiet tell, a true-line KAAMOS/Nebula shard landing in the ledger —
                // was a coin-flip to open, and no cheat GRANTED her lines either. The same seat idiom as
                // ?kaamos=holder / ?nebula=adjuster: it does not hand you a truth, it hands you the person.
                // Combine freely: /map?oracle=1&dock=ringside-exchange&credits=5000.
                string candidate = Uri.UnescapeDataString(pair["oracle=".Length..]).ToLowerInvariant();
                oracleCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("ashore=", StringComparison.OrdinalIgnoreCase))
            {
                // #428 dev cheat: /map?ashore=1 boots docked (default The Space Bar, override with ?dock= /
                // ?start=) and ALREADY STANDING IN THE BAR, one step inside the hall's north door, facing in.
                //
                // Every bar beat there is — the oracle (?oracle=1), the stranger-bond (?bond=1), the KAAMOS
                // berth-holder and the Nebula adjuster (?kaamos=holder / ?nebula=adjuster), the Magpie's rota
                // (?simhours=), the barkeep, the gift shop, the insurance poster — made you walk ship →
                // airlock → tube → immigration hall → bar on EVERY boot first. That walk is a pleasure to
                // play and a wall to test: an MCP-driven browser tab is `document.hidden`, so rAF is
                // throttled and WASD never lands, and not one bar beat could be smoke-tested at all.
                //
                // It seats nobody and grants nothing — it moves the captain, exactly as the walk would have.
                // The position is derived from the doorway the real walk crosses (HavenInterior.BarThreshold),
                // never typed in. Combine freely:
                //   /map?oracle=1&ashore=1                      the rant, one URL and one [E]
                //   /map?ashore=1&dock=cinder-roost&backroom=open
                //   /map?ashore=1&nebula=adjuster&simhours=9
                string candidate = Uri.UnescapeDataString(pair["ashore=".Length..]).ToLowerInvariant();
                ashoreCheat = candidate is "1" or "true" or "yes";
            }
            else if (pair.StartsWith("nerve=", StringComparison.OrdinalIgnoreCase))
            {
                // #428 dev cheat: /map?nerve=N seeds the nerve gauge at boot at N WHOLE PIPS — the same ten
                // the corner gauge draws (#480), not points out of a hundred — so N reads straight off the
                // pip row the player looks at. Out-of-range asks clamp to the gauge, the ?air=N idiom.
                //
                // The clamp is NOT applied here, deliberately. NervePips.FromPips already clamps to the
                // model's own MinPips..MaxPips on the way onto the pip lattice, and a second Math.Clamp on
                // this line would be a second place computing the gauge's bounds — the "one source of truth"
                // rule, and the reason a guard on the seed can only be honest if there is one clamp to break.
                //
                // Without it no sanity beat could be reached on demand: nerve only falls by being hunted for
                // minutes, so the overdraw death, the monolith's lump landing on an already-frayed captain
                // and the archive node's dwell were each a long walk away from any boot. One URL each now:
                //   /map?nerve=1&dock=the-tilt&site=0&land=1&reevers=1   one pip left, a hand inbound
                //   /map?nerve=3&dock=the-tilt&site=0&land=1             the monolith, hit at a low gauge
                //   /map?nerve=2&archive=1&land=1                        the dwell, with almost nothing to spend
                //
                // At N=1 the captain is NOT yet overdrawn (CaptainSuccession.EmptyThreshold sits under one
                // pip), so what you watch is the real two-step break — a hand takes the last pip, the NEXT
                // one breaks them — rather than an instant death the cheat invented.
                //
                // #784 · …and three WORDS beside the number, because the short rest's demo link is read by a
                // person rather than by a machine and "nerve=low" says what it means where "nerve=2" needs
                // the pip lattice explained first. Same flag, same clamp, same seed — the words are spellings
                // of the number and never a second parser.
                string candidate = Uri.UnescapeDataString(pair["nerve=".Length..]);
                nerveCheat = candidate.ToLowerInvariant() switch
                {
                    "shot" or "gone" => 0,
                    "low" or "fraying" => 2,
                    "half" or "shaken" => 5,
                    _ => int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out int pips) ? pips : nerveCheat,
                };
            }
            else if (pair.StartsWith("reevers=", StringComparison.OrdinalIgnoreCase))
            {
                // #458 dev cheat: /map?reevers=N drops N Old Ones RIGHT ON the captain the moment they set
                // down, already aware — so the chase, the #441 spacing and the #453 exchange (block roll,
                // blood, the five blows) can be watched in seconds instead of hunted for on a long walk.
                // Owner, 2026-07-27: "don't forget to test that they also really work."
                string candidate = Uri.UnescapeDataString(pair["reevers=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                {
                    _reeverAmbushCheat = Math.Clamp(n, 0, 8);
                }
            }
            else if (pair.StartsWith("sweep=", StringComparison.OrdinalIgnoreCase))
            {
                // #538 dev cheat: /map?sweep=N puts N professionals aboard whatever hull you board — the black-ops
                // inspection team. Mirrors ?reevers=N, because the scene it makes is the same shape of thing to
                // want to watch: "we take our guns and hide to let them pass."
                string candidate = Uri.UnescapeDataString(pair["sweep=".Length..]);
                if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sweepers))
                {
                    _sweepTeamCheat = Math.Clamp(sweepers, 0, InspectionTeam.TeamSize);
                }
            }
            else if (pair.StartsWith("nebula=", StringComparison.OrdinalIgnoreCase))
            {
                // #422 dev cheat: /map?nebula=N assembles the first N NEBULA MUTUAL fragments (canonical
                // order), /map?nebula=all assembles every one — the Captain's-ledger readout, its state
                // transitions, and the one-time truth notice reachable without a full playthrough.
                //
                // Those GRANT the fragments. /map?nebula=adjuster instead SEATS the one that could only ever
                // be granted: the roving Nebula Mutual adjuster drinks at a given bar roughly one watch in
                // five, so the bar scene — the arc's best-written beat — was unopenable on purpose. Seated,
                // the "▓ Ask about NEBULA" seam is on the barkeep card at whatever bar you dock at.
                // Combine freely: /map?nebula=adjuster&dock=the-space-bar. (The KAAMOS twin is ?kaamos=holder.)
                string candidate = Uri.UnescapeDataString(pair["nebula=".Length..]).ToLowerInvariant();
                if (candidate is "all" or "adjuster"
                    || int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    nebulaCheat = candidate;
                }
            }
            else if (pair.StartsWith("converge=", StringComparison.OrdinalIgnoreCase))
            {
                // #422 dev cheat: /map?converge=1 seeds JUST ENOUGH of BOTH arcs (each side's joint
                // threshold) to fire THE CONVERGENCE — the marquee one-time reveal — from a single URL.
                string candidate = Uri.UnescapeDataString(pair["converge=".Length..]).ToLowerInvariant();
                convergeCheat = candidate is "1" or "true" or "yes";
            }
        }

        if (bondCheat)
        {
            // Arm the forced bond and make sure we boot INTO a bar (a scare on the bare ship deck has no room
            // of strangers). Default to The Space Bar; any ?dock=<id> the caller passed wins.
            _bondForce = true;
            dockCheat ??= "the-space-bar";
        }

        if (oracleCheat)
        {
            // #428 · Arm her seat BEFORE any deck is welded (SetDeckForDock reads _oracleForce and it rides
            // the deck cache key), and make sure we boot INTO a bar — an oracle with no bar to haunt is the
            // same non-scene as a scare with no room of strangers. Default The Space Bar; any ?dock= wins.
            _oracleForce = true;
            dockCheat ??= "the-space-bar";
        }

        // #428 · An ashore boot needs a bar to be ashore IN. Same idiom as the bond and the oracle above —
        // default a berth with a walkable interior — but guarded the way #621's death default is: `?dock=`
        // is read before `?start=` below, so defaulting one unconditionally would quietly outrank a
        // `?start=` the caller did pass. Anything the caller asked for still wins.
        if (ashoreCheat && dockCheat is null && startId is null)
        {
            dockCheat = "the-space-bar";
        }

        // #621 · A death needs somewhere to have happened. Without a start this boot ends at the front
        // door, and the death card would open OVER the picker — a modal on top of a menu, which is not a
        // scene anybody can read. Same idiom as the bond above: default a berth, and any ?dock= / ?start=
        // the caller passed still wins. Only for a death staged on her DECK; a landing cheat brings its
        // own ground and its own berth with it.
        // (…and only when NOTHING else chose a start: `?dock=` is read before `?start=` below, so
        // defaulting one unconditionally would quietly outrank a `?start=` the caller did pass.)
        if (deathCheat is not null && !_landCheat && dockCheat is null && startId is null)
        {
            dockCheat = "the-tilt";
        }

        // #310 honest boot state: if this boot will end at the load view (no direct start/dock cheat),
        // raise the front door NOW in its "warming the reactor" state, so the WASM warm-up never reads as
        // a broken, click-eating menu. It flips to the live slots once _worldReady flips below.
        if (dockCheat is null && startId is null && slingCheat is null && skimCheat is null)
        {
            _showStartPicker = true;
            StateHasChanged();
        }

        string json = await Http.GetStringAsync($"scenarios/{scenarioName}.json", abandoned);
        ScenarioDefinition scenario = ScenarioLoader.Parse(json);
        if (ellipseCheat)
        {
            scenario = scenario with { Bodies = [.. scenario.Bodies, KeplerDemoBody()] };
        }
        if (expeditionCheat is not null)
        {
            // #370: the site is a mission-spawned body, and the ephemeris is immutable once built — so the
            // ONLY clean way to a runtime LANDABLE rock is to append it to the scenario BEFORE FromScenario
            // (the ellipse-cheat idiom). Park it co-orbiting the berth we'll clamp onto, a fixed small hop
            // off, so it is always in shuttle range. Default the berth to Selene Gate when none was asked.
            string berthKey = dockCheat ?? "selene-gate";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? mappedBerth) ? mappedBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                ExpeditionFlavor flavor = expeditionCheat == "mining" ? ExpeditionFlavor.MiningSurvey : ExpeditionFlavor.Science;
                // Position/velocity are unused here (the rock rides real orbit rails); Spawn only fixes the
                // seeded KIND + display name deterministically.
                SiteSpawn spawn = ExpeditionSite.Spawn(ExpeditionCheatSeed(flavor), Vector2d.Zero, Vector2d.Zero, flavor);
                scenario = scenario with { Bodies = [.. scenario.Bodies, ExpeditionSiteBody(spawn, berthId)] };
                _pendingExpeditionCheat = (flavor, spawn.Kind, spawn.BodyId, spawn.DisplayName);
                dockCheat = berthId; // clamp onto the berth the rock co-orbits, so it's in reach at spawn
            }
        }
        if (deflectionCheat is not null
            && scenario.Bodies.FirstOrDefault(b => b.Id == "ringside-exchange") is { } ring
            && scenario.Bodies.Any(b => b.Id == ring.ParentId))
        {
            // #394: the inbound rock is a mission-spawned body on a real COLLISION rail with the Ringside
            // Exchange — appended before FromScenario (the immutable-ephemeris idiom), timed so it kisses the
            // station's orbit at T-impact. Ship clamps onto Ringside (in shuttle reach of the rock at spawn).
            RockType type = deflectionCheat switch
            {
                "c" => new RockType(RockComposition.CType),
                "s" => new RockType(RockComposition.SType),
                "m" => new RockType(RockComposition.MType),
                _ => DeflectionGig.RollType(DeflectionCheatSeed),
            };
            double impactRailTime = DeflectionGig.RailLeadSeconds; // fresh boot: accept at sim-time ≈ 0
            DeflectionGig.RockRail rail = DeflectionGig.BuildRail(
                ring.OrbitRadiusM, ring.OrbitPeriodS, ring.InitialPhaseRad, impactRailTime);
            (double spinPeriod, double spinPhase) = DeflectionSpin(DeflectionCheatSeed);
            scenario = scenario with { Bodies = [.. scenario.Bodies, DeflectionRockBody(rail, ring.ParentId!)] };
            _pendingDeflectionCheat = (type, DeflectionGig.RockName(DeflectionCheatSeed), rail,
                ring.Id, "Ringside Exchange", ring.OrbitRadiusM, ring.OrbitPeriodS, ring.InitialPhaseRad,
                ring.ParentId!, impactRailTime, spinPeriod, spinPhase);
            dockCheat = "ringside-exchange"; // clamp onto the port under threat, in reach of the rock
        }
        if (tableSceneCheat)
        {
            // #875 · This is where ?tablescene=1 used to turn ?autowalk=1 on, because the last leg of the
            // scene is a walk across a canteen and clicking where you want to be is how this repo tests a
            // room. It no longer has to: the click is a control on every walked view now, so this boot —
            // and every other boot in DevStarts — arrives with both grips on the same walk.
            _tableSceneCheat = true;
        }


        if (secretlabCheat)
        {
            // #409: append a plain landable Moon-kind rock co-orbiting the berth (the ellipse-cheat idiom),
            // comfortably inside one shuttle hop. Its surface is FORCED to hide a Vantar lab and to pre-reveal
            // the door (see ResolveSecretLab, keyed on _secretLabForceBodyId). Default the berth to Selene Gate.
            string berthKey = dockCheat ?? "selene-gate";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? mappedBerth) ? mappedBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                // #677 · Three rocks now, one cheat shape. `?found=1` is the deepest of them and it implies
                // `?secretlab=1`, because there is no other way down: the halls hang off a band nobody
                // listed, which hangs off a facility, which is reached through a shed.
                (string rockId, string rockName) = _foundCheat
                    ? (SecretLabFoundCheatBodyId, "The Hermit's Deep Rock")
                    : secretlabDeep
                        ? (SecretLabDeepCheatBodyId, "The Deep Hermit's Rock")
                        : (SecretLabCheatBodyId, "The Hermit's Rock");
                scenario = scenario with
                {
                    Bodies = [.. scenario.Bodies, SecretLabSiteBody(berthId, rockId, rockName)],
                };
                _secretLabForceBodyId = rockId;
                dockCheat = berthId; // clamp onto the berth the rock co-orbits, so it's in reach at spawn
            }
        }
        if (wreckCheat)
        {
            // #488: append the derelict as a boardable site co-orbiting the berth — the same ellipse-cheat
            // idiom the expedition rock and the Hermit's Rock use. She is seeded from her id, so this cheat
            // always spawns the SAME ship with the same cause and the same cargo, which is what makes her
            // testable. Default the berth to The Tilt.
            string berthKey = dockCheat ?? "the-tilt";
            string berthId = DockedStarts.TryGetValue(berthKey, out string? wreckBerth) ? wreckBerth : berthKey;
            if (scenario.Bodies.Any(b => b.Id == berthId))
            {
                Derelict.Wreck w = CheatWreck(wreckCauseCheat);
                scenario = scenario with { Bodies = [.. scenario.Bodies, WreckSiteBody(berthId, w)] };
                _wreck = w;
                dockCheat = berthId; // clamp onto the berth she hangs off, so she is in reach at spawn
            }
        }
        _scenarioName = scenario.Name;
        _ephemeris = CircularOrbitEphemeris.FromScenario(scenario);
        // #288: print the enumerable registry of every dockable berth to the browser console on boot, so
        // the bench never guesses an id — /map?dock=<id> boots already clamped on at any of these.
        // #726: this is per-COMPONENT boot, and the router builds a new Map every time you arrive at
        // /map — so walking Home → Map → Home → Map printed the identical list four times over. Say it
        // only when it is news (see BootRegistryAnnouncement); a different scenario still announces its
        // own berths, a second lap round the same world does not.
        string berthRoster = $"[SpaceSails] Dockable berths — /map?dock=<id>: {string.Join(", ", DockableHavens.AllIds(_ephemeris))}";
        if (BerthRosterAnnouncement.IsNews(berthRoster))
        {
            Console.WriteLine(berthRoster);
        }
        // Tuesday plan PR-A: the scenario's off-the-charts bodies (e.g. the derelict roadster). They
        // stay dark until an intel-fed scan (or a dev reveal cheat) charts them.
        _hiddenBodyIds.Clear();
        _revealedBodyIds.Clear();
        foreach (BodyDefinition body in scenario.Bodies)
        {
            if (body.Hidden)
            {
                _hiddenBodyIds.Add(body.Id);
            }
        }
        // PR-15, the captain's position: the mission catalog is scenario data (cargo classes,
        // route pairs, havens), so it's built once per scenario load alongside everything else
        // that reads _ephemeris — never recomputed per frame or per desk switch.
        _missionOptions = MissionCatalog.Build(_ephemeris);
        _plasma = PlasmaEnvironment.FromScenario(scenario, _ephemeris);
        _simulator = new Simulator(_ephemeris, timeStepSeconds: 1.0, _plasma);
        _npcSimulator = new Simulator(_ephemeris, TrafficSchedule.NpcTimeStep); // NPCs chargeless in M7

        _ship = InitializeShipState();
        ReprojectTrajectory();

        // Owner (2026-07-05, after the empty-purse screenshot): an operating ship arrives with
        // history — the last run's takings in the purse and its leftovers in the hold, so
        // buying AND selling are exercisable from minute one.
        _credits = StartingCredits;
        foreach ((string cargoClass, int units) in StartingManifest)
        {
            _cargoUnits += units;
            _cargoValue += units * CargoMarket.UnitValue(cargoClass);
            _cargoByClass[cargoClass] = _cargoByClass.GetValueOrDefault(cargoClass) + units;
        }

        // Generate traffic once from the same deterministic Core planner the server uses. This does a
        // few seconds of planning work — and each coarse step (pods / freighters / depots / mass-driver
        // pods) is its own synchronous block. #318 follow-up: paint an honest phase line and yield to the
        // browser BEFORE each block, so the loading door shows progress and the tab stays paintable
        // instead of one silent multi-second freeze (badly amplified on the Debug/dev bundle). We do NOT
        // parallelise or restructure the planners — just phase-yield around them.
        // Pods first so "the Luna pod" is the top of the board and the obvious tutorial prey.
        await BootPhaseAsync("plotting the traffic lanes — pods…", abandoned);
        IReadOnlyList<NpcShip> pods = TrafficSchedule.GeneratePods(_ephemeris, seed: 43, count: 3);
        await BootPhaseAsync("plotting the traffic lanes — freighters…", abandoned);
        IReadOnlyList<NpcShip> traffic = TrafficSchedule.Generate(_ephemeris, seed: 42, count: 8);
        // The derelict roadster is a dead wreck, not a trading post — it's a station body only so its
        // map label reads at a sane zoom (the fetch-mission target). Drop the depot GenerateDepots
        // would otherwise hang on it (any sun-orbiting body gets one).
        // A depot on a hidden body would leak it (the depot marker/menu would give the wreck away).
        // Filter generically on hidden+unrevealed, not the wreck's id — every future secret body is
        // covered for free (Tuesday plan PR-A).
        await BootPhaseAsync("plotting the traffic lanes — supply depots…", abandoned);
        IReadOnlyList<NpcShip> depots = TrafficSchedule.GenerateDepots(_ephemeris, seed: 44)
            .Where(d => d.DepotBodyId is null || !IsBodyHidden(d.DepotBodyId)).ToList();

        // The tutorial's "first hunt" needs prey the player can actually catch from a standing start.
        // Interplanetary traffic screams past at 80–160 km/s relative — past the 5 km/s boarding
        // limit — so in the Sol family we seed one guaranteed-catchable pod abeam the ship: a fresh
        // Luna launch still co-moving with Earth, a short plotted burn away. (See StarterPod.)
        IEnumerable<NpcShip> initial = pods.Concat(traffic).Concat(depots);
        if (_scenarioName.Contains("Sol", StringComparison.OrdinalIgnoreCase))
        {
            // Luna's mass drivers, lobbing compute-core pods on a steady cadence (worldbuilding §1;
            // Lab 30 "The mass-driver timetable"): a modest run of ballistic pods fired retrograde
            // toward the inner system, half already in flight at world-load, so the "Luna's mass
            // drivers lobbing compute-core pods" the scenario description promises is literally on the
            // map as tiny moving objects. Zero maneuver budget, empty plan — they coast their conic.
            await BootPhaseAsync("plotting the traffic lanes — Luna's mass drivers…", abandoned);
            IReadOnlyList<NpcShip> lunaDriver = MassDriverSchedule.GenerateCadence(
                _ephemeris, MassDriverSchedule.MassDriverRun.LunaMilkRun(), baseSimTime: _ship.SimTime, count: 4);

            // Ruling-2 (2026-07-18): the first-hunt soft catch is NO LONGER seeded at boot. With every
            // start now DOCKED (and never a T=0 Earth cast-off), the tutorial pod would spawn abeam a
            // free-flying Earth ship that is about to be clamped onto a station somewhere else entirely —
            // an orphan. Instead the target "gets going" when the lesson is TAKEN ON: the auto-greet at
            // the Selene Gate tutorial home (ApplyStart) and the Captain's-tab StartTutorial both call
            // SeedFirstHuntTarget, which places the pod relative to where the ship actually is THEN. The
            // stubborn Lark is likewise spawned only when the first hunt ends (SeedSecondHuntTarget).
            initial = lunaDriver.Concat(initial);
        }
        _npcStates = initial.Select(s => new NpcState { Ship = s }).ToArray();

        _scratch = new float[_samples.Count * 2 + 4];

        _camera.MetersPerPixel = 3e8;
        _camera.CenterOn(_ship.Position);

        await RendererInterop.EnsureModuleLoadedAsync();

        // #737 · THE LAST GATE BEFORE THE DOM. Everything from here on names elements by id — the canvas,
        // the scope inset, the focusable page div — and renderer.js throws rather than shrugging when the
        // id resolves to nothing. If the player left during any of the planning phases above, the page
        // those ids belong to is already gone, so this is where an abandoned boot must stop: no module
        // wiring, no FrameTick subscription on a component the router discarded, and above all no rAF loop
        // started against a dead canvas that nothing would ever stop.
        abandoned.ThrowIfCancellationRequested();

        _renderer = new CanvasRenderer(CanvasId);
        RendererInterop.FrameTick += OnTick;
        RendererInterop.CanvasResized += OnCanvasResized;

        RendererInterop.InitCanvas(CanvasId, observeResize: true);
        RendererInterop.InitCanvas(ScopeCanvasId, observeResize: false);
        _scopeView = new ScopeView(new CanvasRenderer(ScopeCanvasId));
        _deckView = new DeckView(_renderer!);
        _fpView = new FirstPersonView(_renderer!);
        _shuttleView = new ShuttleFlightView(_renderer!);
        RendererInterop.StartLoop(CanvasId);
        _renderLoopRunning = true;

        // #371 Phase 1 (perf) · PRE-DECODE the deck/surface backdrop art at boot. RegisterImage fires the
        // JS decode fire-and-forget and caches by id; doing it now means the first deck or surface paint
        // never stalls waiting on an image decode (the study's cheap pre-warm). The surface plan reuses the
        // ship's backdrop set, so warming the ship's covers both.
        PredecodeDeckArt();

        _worldReady = true;

        // Start point: an explicit /map?start=<id> jumps straight there (the renderer is live now, so
        // a docked-&-ashore start's board cue is safe); with no param, offer the boot picker so a
        // playtester (or a player who'd rather not always cast off from Earth) can choose a locale.
        if (dockCheat is not null && ResolveDockStartId(dockCheat) is { } dockHaven)
        {
            StartDockedAtHaven(dockHaven); // #288: boot already clamped on at any dockable berth
        }
        else if (startId is not null)
        {
            ApplyStart(startId);
        }
        else
        {
            PeekSavedVault(); // #225: surface a "Continue — docked at <haven>" lead if a vault exists.
            _showStartPicker = true;
        }

        // #428 · ?ashore=1 — walk the walk for them. AFTER the clamp (the interior is welded by
        // SetDeckForDock, which the start above ran) and BEFORE any landing cheat, which brings its own
        // ground and takes the captain off this deck entirely.
        if (ashoreCheat)
        {
            ShowPulseMessage(StandAtTheBarThreshold()
                ? $"🍸 Test: you are ashore in {_havenName} — the ship → tube → hall walk is already behind you. [E] works the tables, the counter and the corners."
                : "🍸 Test: ?ashore=1 needs a berth with a walkable interior — this one has no bar to stand in. Try &dock=the-space-bar.");
        }

        // #428 · ?nerve=N — seed the gauge BEFORE the landing cheat rides the shuttle down and before any
        // ?death= is staged, because both READ the live nerve: the descent's first frames price the gauge,
        // and the death card asks CaptainSuccession.OverdrawQualifies(_nerve) whether the captain was
        // already empty. Seeding after them would hand the card the default steady gauge and caption a
        // shattered captain as merely mauled — the sentence saying one thing while the sim did another.
        if (nerveCheat is { } seedPips)
        {
            _nerve = NervePips.FromPips(seedPips);
        }

        if (_pendingExpeditionCheat is not null)
        {
            InjectExpeditionCheat(); // #370: after the clamp — the accepted gig lands on a live, docked world
        }

        // #411 — the head-office route seat has to be applied BEFORE ?land= fires, because the shuttle
        // board is computed off where the ship floats and this cheat MOVES the ship to the ice moon. Every
        // other ?kaamos= value is state-only and rides the ordinary block further down.
        if (string.Equals(kaamosCheat, "hq", StringComparison.OrdinalIgnoreCase))
        {
            SeedKaamosHeadOfficeCheat();
            kaamosCheat = null;
        }

        if (_landCheat)
        {
            // #464: ride the shuttle down now that the berth is clamped and the ephemeris is live, so the
            // in-range board is real. Fire-and-forget: BeginSurfaceExcursion narrates its own descent
            // phases and yields between them, exactly as the hatch's own path does.
            // #621: …and ?death= waits for the boots to be on the ground, because the PLACE is read off the
            // live excursion. Killing the captain before the shuttle has landed would classify the death on
            // her deck and hand back the wrong card — which is the whole bug the cheat exists to hunt.
            _ = AutoLandThenStageDeathAsync(deathCheat);
        }
        else if (deathCheat is { } onHerDeck)
        {
            StageDeathCheat(onHerDeck);
        }

        if (_pendingDeflectionCheat is not null)
        {
            InjectDeflectionCheat(); // #394: after the clamp — rock inbound, ship docked at the threatened port
        }

        if (fetchCheat is not null)
        {
            InjectFetchCheat(fetchCheat); // after the start, so the dest can be the station we docked at
        }

        if (crackCheat is not null)
        {
            InjectCrackCheat(crackCheat); // needs the docked station's deck built (a locked hatch to target)
        }

        if (backroomCheat is not null)
        {
            InjectBackroomCheat(backroomCheat); // PR-F: weld the wing open, or stage the crack that opens it
        }

        if (tipCheat is not null)
        {
            InjectTipCheat(); // seed a representative route tip so the ledger's Tips & intel is reachable
        }

        if (hoardCheat is not null)
        {
            InjectHoardCheat(hoardCheat); // #223: seed a buried chest and/or a bought rumour map
        }

        if (kaamosCheat is not null)
        {
            SeedKaamosCheat(kaamosCheat); // #411: assemble N KAAMOS fragments (readout + reach notice), or seat the pod/holder so the find itself can be played
        }

        if (nebulaCheat is not null)
        {
            SeedNebulaCheat(nebulaCheat); // #422: assemble N NEBULA fragments (readout + truth notice), or seat the adjuster so the bar scene itself can be played
        }

        if (convergeCheat)
        {
            SeedConvergeCheat(); // #422: seed both arcs' joint threshold and fire THE CONVERGENCE reveal
        }

        if (_oracleForce)
        {
            // #428: say WHERE she is, not just that she's here — the corner is deliberately clear of every
            // other console, and a captain who can't find her reads the cheat as broken.
            ShowPulseMessage("🌀 Test: Static Marsh has the port-back corner of this bar, whatever the watch. Walk in, head aft along the left wall, and press E on ◈ “STATIC” MARSH.");
        }

        // Tuesday plan PR-A: ?start=wreck drops you 2 km off the roadster — you're on top of her, so
        // chart her quietly (no "found it!" fanfare when you were parked alongside all along). This
        // also keeps ?start=wreck&fetch=active green.
        if (startId == "wreck")
        {
            RevealBody("derelict-roadster", "", announce: false);
        }

        // ?reveal=<bodyId> (repeatable): chart any hidden body at boot for testing every downstream leg.
        foreach (string id in revealCheats)
        {
            RevealBody(id, $"🧪 Test: {BodyName(id)} charted.");
        }

        // ?sling=<bodyId>: boot onto an inbound arc with a close pass by that body (PR-G test hook).
        // Suppress the start picker — picking a berth would overwrite the seeded approach state.
        if (slingCheat is not null)
        {
            _showStartPicker = false;
            SeedSlingCheat(slingCheat);
        }

        // ?skim=<bodyId>: boot onto a hyperbolic inbound grazing that body's atmosphere (PR-I test hook).
        if (skimCheat is not null)
        {
            _showStartPicker = false;
            SeedSkimCheat(skimCheat);
        }

        // ?credits=N / ?fuel=N (#288): seed the purse and tank last, after any start has laid down the
        // defaults, so an in-situ situation (afford a fill-up, reach a pump) is set up straight from boot.
        if (creditsCheat is { } seedCredits)
        {
            _credits = seedCredits;
        }

        if (fuelCheat is { } seedPulses)
        {
            _reactionMassPulses = Math.Clamp(seedPulses, 0, ReactionMassCapacity);
        }

        // ?simhours=N: jump the sim clock at boot so the roaming Magpie's rota can be sampled (PR-F).
        // While docked, HoldAtDock re-pins the ship to the berth at the new time on the next tick.
        if (simHoursCheat is { } jumpHours)
        {
            _ship = _ship with { SimTime = jumpHours * 3600 };
            SimTime = _ship.SimTime;
        }

        StateHasChanged();

        // The captured @ref is only a live element while the page is mounted; focusing a reference whose
        // element has left the DOM throws out of here (#737).
        abandoned.ThrowIfCancellationRequested();
        await _focusableDiv.FocusAsync();

        // #371 Phase 1 (perf) · warm the cold surface DRAW path once, idle-time, now that the map is
        // interactive. Fire-and-forget and yield-fronted so it never lengthens the perceived boot stall;
        // see WarmSurfaceDrawPathAtBootAsync for the never-flash guard.
        _ = WarmSurfaceDrawPathAtBootAsync();
    }
}

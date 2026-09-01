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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — the first three readers of the ?query chain: the world and the jobs, the bodies a cheat spawns, and the clocks.
public partial class Map
{

    /// <summary>Which world, where in it, and the jobs and money you arrive holding — <c>?scenario=</c>,
    /// <c>?start=</c>, <c>?dock=</c>, <c>?fuel=</c>, <c>?credits=</c>, <c>?fetch=</c>, <c>?reveal=</c>,
    /// <c>?crack=</c>, <c>?tip=</c>, <c>?hoard=</c>, <c>?sling=</c>, <c>?skim=</c>, <c>?backroom=</c>,
    /// <c>?target=</c>, <c>?dest=</c> and <c>?simhours=</c>.</summary>
    private bool ReadTheWorldAndTheJobs(string pair, BootQuery q)
    {
        // /map?scenario=sol-eu loads scenarios/sol-eu.json; default sol. Name is sanitized to a
        // simple slug — it becomes a URL path segment. /map?start=space-bar jumps the freshly-built
        // world straight to a named start point (see StartPoints) — the playtest "skip the set-up"
        // shortcut, and the same registry the boot picker offers. Unknown start id → the picker shows.
        if (pair.StartsWith("scenario=", StringComparison.OrdinalIgnoreCase))
        {
            string candidate = Uri.UnescapeDataString(pair["scenario=".Length..]);
            if (candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                q.ScenarioName = candidate;
            }
        }
        else if (pair.StartsWith("start=", StringComparison.OrdinalIgnoreCase))
        {
            string candidate = Uri.UnescapeDataString(pair["start=".Length..]);
            if (StartPoints.Any(s => s.Id == candidate))
            {
                q.StartId = candidate;
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
                q.DockCheat = candidate;
            }
        }
        else if (pair.StartsWith("fuel=", StringComparison.OrdinalIgnoreCase))
        {
            // #288 dev cheat: /map?fuel=N seeds the tank at boot (clamped to capacity), so a low-fuel
            // situation — the #262 "can I reach a pump?" test — is reachable in-situ without burning down.
            string candidate = Uri.UnescapeDataString(pair["fuel=".Length..]);
            if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) && f >= 0)
            {
                q.FuelCheat = f;
            }
        }
        else if (pair.StartsWith("credits=", StringComparison.OrdinalIgnoreCase))
        {
            // #288 dev cheat: /map?credits=N seeds the purse at boot, so a can-you-afford-it situation
            // (a fill-up, a bribe, an upgrade) is testable in-situ without grinding a run first.
            string candidate = Uri.UnescapeDataString(pair["credits=".Length..]);
            if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) && c >= 0)
            {
                q.CreditsCheat = c;
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
                q.FetchCheat = candidate;
            }
        }
        else if (pair.StartsWith("reveal=", StringComparison.OrdinalIgnoreCase))
        {
            // Dev cheat: /map?reveal=<bodyId> charts a hidden body straight away (repeatable).
            string candidate = Uri.UnescapeDataString(pair["reveal=".Length..]);
            if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                q.RevealCheats.Add(candidate);
            }
        }
        else if (pair.StartsWith("target=", StringComparison.OrdinalIgnoreCase))
        {
            // #997 wave 10 dev cheat: /map?target=<contact-id> boots with the tactical UI pointed at a
            // contact and her DOSSIER open on the glass. `?reveal=` charts a body; this points at a ship,
            // and it is the same kind of cheat — what the sky already holds when you arrive.
            //
            // WHY IT HAD TO EXIST. #960's dossier is gated on a tactical target, and the only two roads to
            // one are a contact in sensor reach or a collector bought by a robbery — a sim state with no
            // URL behind it. Waves 7, 8 and 9 each MEASURED that card by hand and each said so out loud;
            // #1010's own §6 named the missing cheat as the reason #735's browser gate could not cover it.
            //
            //   /map?target=npc-0                       a scheduled hauler, her file open
            //   /map?target=collector&dock=selene-gate  send the muscle first, then read her terms
            //
            // Sanitised the same way ?reveal= and ?dock= are — this becomes a lookup key against two live
            // rosters, and an id is a slug.
            string candidate = Uri.UnescapeDataString(pair["target=".Length..]).ToLowerInvariant();
            if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                q.TargetCheat = candidate;
            }
        }
        else if (pair.StartsWith("dest=", StringComparison.OrdinalIgnoreCase))
        {
            // #956 dev cheat: /map?dest=<body-id> boots with the NAVIGATION DESTINATION already set — the
            // thing `Follow dest` follows.
            //
            // WHY IT HAD TO EXIST, and it is the very reason `?target=` above had to. A nav destination has
            // exactly one road: a click on a body's canvas menu. Canvas has no DOM, so no browser gate can
            // reach it, and #956's whole feature — a camera that rides the destination — was therefore
            // provable only in xUnit against fields. That is the #603 class waiting to happen: perfect in
            // the source, dead under a finger. One URL key buys the pixels.
            //
            //   /map?dest=jupiter          the nav target set, Follow dest live
            //   /map?dest=saturn&start=…   …from wherever you like
            //
            // NOT the same key as `?target=`: that points the TACTICAL ui at a contact (a ship), this sets
            // the NAV destination (a body). Two questions, two keys. Sanitised as a slug like its
            // neighbours — it becomes a lookup against the ephemeris.
            string candidate = Uri.UnescapeDataString(pair["dest=".Length..]).ToLowerInvariant();
            if (candidate.Length > 0 && candidate.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            {
                q.DestCheat = candidate;
            }
        }
        else if (pair.StartsWith("crack=", StringComparison.OrdinalIgnoreCase))
        {
            // Dev cheat: /map?start=<station>&crack=active|picked injects the hatch-crack job at that
            // stage so a playtester can exercise the keypad / hand-off without taking the fetch first.
            string candidate = Uri.UnescapeDataString(pair["crack=".Length..]).ToLowerInvariant();
            if (candidate is "active" or "picked")
            {
                q.CrackCheat = candidate;
            }
        }
        else if (pair.StartsWith("tip=", StringComparison.OrdinalIgnoreCase))
        {
            // Dev cheat: /map?tip=route seeds a representative route tip (with provenance) into the
            // ledger so the Captain's-ledger Tips & intel rendering is reachable without walking a bar.
            string candidate = Uri.UnescapeDataString(pair["tip=".Length..]).ToLowerInvariant();
            if (candidate is "route")
            {
                q.TipCheat = candidate;
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
                q.HoardCheat = candidate;
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
                q.SlingCheat = candidate;
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
                q.SkimCheat = candidate;
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
                q.BackroomCheat = candidate;
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
                q.SimHoursCheat = h;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>The keys that append a BODY to the scenario before the ephemeris is built (the immutable-
    /// ephemeris idiom) — <c>?ellipse=</c>, <c>?expedition=</c>, <c>?deflection=</c>, <c>?wreck=</c> and
    /// <c>?archive=</c>.</summary>
    private bool ReadTheBodiesACheatSpawns(string pair, BootQuery q)
    {
        if (pair.StartsWith("ellipse=", StringComparison.OrdinalIgnoreCase))
        {
            // Kepler rails (PR-B) dev cheat: /map?ellipse=1 drops one visibly eccentric body onto
            // a sun orbit so the elliptical ring and its non-uniform tracking are checkable in the
            // browser. No effect on any shipped body — it's an extra body appended at load.
            string candidate = Uri.UnescapeDataString(pair["ellipse=".Length..]).ToLowerInvariant();
            q.EllipseCheat = candidate is "1" or "true" or "yes";
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
                q.ExpeditionCheat = candidate;
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
                q.DeflectionCheat = candidate;
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
            q.WreckCheat = candidate is "1" or "true" or "yes";
            foreach (Derelict.WreckCause c in Enum.GetValues<Derelict.WreckCause>())
            {
                if (candidate == c.ToString().ToLowerInvariant())
                {
                    q.WreckCheat = true;
                    q.WreckCauseCheat = c;
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
                q.WreckCheat = true;
                q.WreckCauseCheat = ArchiveCheatCause;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>The dials that price an excursion, and the one that ends it — <c>?air=</c>,
    /// <c>?process=</c>, <c>?collectors=</c> and <c>?death=</c>.</summary>
    private bool ReadTheClocksAndTheDeath(string pair, BootQuery q)
    {
        if (pair.StartsWith("air=", StringComparison.OrdinalIgnoreCase))
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
                    q.DeathCheat = c;
                }
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}

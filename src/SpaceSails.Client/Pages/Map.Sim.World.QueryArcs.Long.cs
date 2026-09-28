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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — #251 · split from
// Map.Sim.World.QueryArcs.cs, moved verbatim: the ?query reader for the two long arcs and the bar.
public partial class Map
{
    /// <summary>The two long stories and the room they are told in — <c>?kaamos=</c>, <c>?bond=</c>,
    /// <c>?oracle=</c>, <c>?ashore=</c>, <c>?nerve=</c>, <c>?reevers=</c>, <c>?sweep=</c>,
    /// <c>?nebula=</c> and <c>?converge=</c>.</summary>
    private bool ReadTheLongArcsAndTheBar(string pair, BootQuery q)
    {
        if (pair.StartsWith("kaamos=", StringComparison.OrdinalIgnoreCase))
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
                q.KaamosCheat = candidate;
            }
        }
        else if (pair.StartsWith("bond=", StringComparison.OrdinalIgnoreCase))
        {
            // #429 dev cheat: /map?bond=1 boots docked at a bar (default The Space Bar, override with
            // ?dock=<id>) and FORCES the next ambient scare (shudder/buzzer/PA) to open a STRANGER-BOND —
            // a co-present stranger stands you a cognac (OLD PERIHELION), the hero beat. Documented in
            // docs/testing-guide.md.
            string candidate = Uri.UnescapeDataString(pair["bond=".Length..]).ToLowerInvariant();
            q.BondCheat = candidate is "1" or "true" or "yes";
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
            q.OracleCheat = candidate is "1" or "true" or "yes";
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
            q.AshoreCheat = candidate is "1" or "true" or "yes";
        }
        else if (pair.StartsWith("havenfloor=", StringComparison.OrdinalIgnoreCase))
        {
            // #1253 dev cheat: /map?dock=selene-gate&ashore=1&havenfloor=-1 boots ashore and then one floor
            // DOWN — standing at the first cage's landing on the lower concourse, with the service corridor,
            // the row of shut cabin doors and the two other cars all in front of you.
            //
            // A scene nobody can reach on demand is a scene that ships broken, and this one sits behind a
            // walk that cannot be made in an MCP-driven tab at all (the game is `document.hidden` there, rAF
            // is throttled and WASD never lands) and then a press on a console at the far side of a hall. It
            // IMPLIES ?ashore=1, for the reason ?barcase= does: being ashore is not what is being tested,
            // the floor under it is.
            //
            // It grants nothing and forces nothing. It rides a car, exactly as the captain would — and a
            // level this station does not have is simply not read, so a typo is a concourse and never a
            // building at level −4.
            string floor = Uri.UnescapeDataString(pair["havenfloor=".Length..]);
            if (int.TryParse(floor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int level)
                && HavenLevels.IsALevel(level))
            {
                q.HavenFloorCheat = level;
                q.AshoreCheat = true;
            }
        }
        else if (pair.StartsWith("barcase=", StringComparison.OrdinalIgnoreCase))
        {
            // #1016 dev cheat: /map?barcase=1 is ?ashore=1 with the last leg walked — sat down at a free top
            // in the berth's bar, with three finds in the sleeve, which is the exact seat the owner filed
            // this issue from. Owner, 2026-08-30: "Maybe it might be good idea to refactor the working the
            // case etc table options to not be tied to any location?"
            //
            // It implies the ashore walk rather than spelling a route of its own, exactly as ?spread=
            // implies ?tablescene= one room over, and it forces nothing about the bar: which berth, which
            // top and who else is in the room are the station's own answers.
            string bar = Uri.UnescapeDataString(pair["barcase=".Length..]).ToLowerInvariant();
            if (bar is "1" or "true" or "yes")
            {
                _barCaseCheat = true;
                q.AshoreCheat = true;
            }
        }
        else if (pair.StartsWith("oldcrew=", StringComparison.OrdinalIgnoreCase))
        {
            // #973 L5a dev cheat: /map?oldcrew=1 boots ashore (default The Space Bar, override with ?dock=)
            // with the four shipmates this universe cast working THIS berth, and with one captain already
            // buried — so the face scene, the photograph and the three named drink modifiers are all one URL
            // away instead of one death and four voyages away.
            //
            // The same seat idiom as ?kaamos=holder / ?oracle=1 / ?nebula=adjuster, and the same discipline:
            // it grants no sheet, writes no crossing and answers nothing. It hands you the people and the
            // fact that your face is new, and every word after that is played.
            string candidate = Uri.UnescapeDataString(pair["oldcrew=".Length..]).ToLowerInvariant();
            q.OldCrewCheat = candidate is "1" or "true" or "yes";
        }
        else if (pair.StartsWith("crew=", StringComparison.OrdinalIgnoreCase))
        {
            // #663 dev cheat: /map?crew=petition boots holding the voyage the crew send a DEPUTATION over —
            // three of them in the corridor outside your door, hats in hands. That beat shipped with a
            // painted canvas, a cadence and nobody to raise it, and the house rule written beside these
            // readers is that "a scene nobody can reach on demand is a scene that ships broken".
            //
            // It was a long way from any boot. The only thing in the shipped game that kills a crewman is
            // the deflection gig's crew-bolt roll, so crossing the Petition edge honestly means accepting
            // the Ringside gig, drilling a rock, losing that dice two or three times — AND having filed
            // enough wreck causes honestly to be poor while you did it. Both halves are needed, and that is
            // the design rather than a threshold: a captain who lies and pays well can bury people quietly
            // (CrewTempTests).
            //
            // So it grants exactly those two counters and nothing else. It writes no standing and pushes no
            // card: the ship's own clock reads the sheet on the next tick, finds the crew past the edge, and
            // the beat arrives through the one door with its cadence spent and its line in the log — which
            // is the whole point of wiring the deputation at the standing rather than at a cheat.
            //
            // #1066 · …and one landing further down. The Ultimatum edge the crew MEETING sits on needs the
            // two counters above AND a run of berths with no shore leave, and reaching it honestly means
            // losing the rock's dice, filing a dozen wrecks straight, and then working the Mars/Venus/Luna
            // circuit for five berths without once calling at Ringside or the Red Eye. That is most of a
            // session for one card, so it gets a door too — and the door still only grants COUNTERS.
            string candidate = Uri.UnescapeDataString(pair["crew=".Length..]).ToLowerInvariant();
            if (candidate is "petition" or "deputation")
            {
                q.CrewCheat = "petition";
            }
            else if (candidate is "meeting" or "ultimatum")
            {
                q.CrewCheat = "meeting";
            }
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
            q.NerveCheat = candidate.ToLowerInvariant() switch
            {
                "shot" or "gone" => 0,
                "low" or "fraying" => 2,
                "half" or "shaken" => 5,
                _ => int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out int pips) ? pips : q.NerveCheat,
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
                q.NebulaCheat = candidate;
            }
        }
        else if (pair.StartsWith("converge=", StringComparison.OrdinalIgnoreCase))
        {
            // #422 dev cheat: /map?converge=1 seeds JUST ENOUGH of BOTH arcs (each side's joint
            // threshold) to fire THE CONVERGENCE — the marquee one-time reveal — from a single URL.
            string candidate = Uri.UnescapeDataString(pair["converge=".Length..]).ToLowerInvariant();
            q.ConvergeCheat = candidate is "1" or "true" or "yes";
        }
        else
        {
            return false;
        }

        return true;
    }
}

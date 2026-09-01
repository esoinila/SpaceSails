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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — the ?query readers for the room’s own dice, where the landing goes, and the two long arcs.
public partial class Map
{

    /// <summary>The rolls a room makes about you, and the state you are in when it makes them —
    /// <c>?approach=</c>, <c>?rep=</c>, <c>?walkin=</c>, <c>?hurt=</c>, <c>?shelter=</c>, <c>?mags=</c>, <c>?watch=</c> and
    /// <c>?roll=</c>.</summary>
    private bool ReadTheRoomsOwnDice(string pair, BootQuery q)
    {
        if (pair.StartsWith("approach=", StringComparison.OrdinalIgnoreCase))
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
        else if (pair.StartsWith("rep=", StringComparison.OrdinalIgnoreCase))
        {
            // #973 L2 dev cheat: /map?rep=1 puts Harlan Fess on this ground whatever his rota says;
            // /map?rep=0 keeps him off it.
            //
            // Same argument as ?approach= above, and it is the stronger case of the two: his presence is
            // "at most one place in three, never two visits running", so without a lever the whole
            // feature — the walk in, the pitch, the flashback, the withdrawal — is reachable only by
            // docking somewhere three or four times and hoping. It forces WHETHER and never WHO or WHAT:
            // the tier line, the buttons, the rarity of the bleed and the once-per-life page are all the
            // ones a captain gets.
            string candidate = Uri.UnescapeDataString(pair["rep=".Length..]).ToLowerInvariant();
            _repCheat = candidate switch
            {
                "1" or "true" or "yes" or "now" => true,
                "0" or "false" or "no" or "never" => false,
                _ => null,
            };
        }
        else if (pair.StartsWith("walkin=", StringComparison.OrdinalIgnoreCase))
        {
            // #973 L5b dev cheat: /map?walkin=1 lets a walk-in happen at this berth whatever the rota and the
            // tier say; /map?walkin=0 keeps her away.
            //
            // The strongest case of the three on this page. Her cadence is "rare, once per subject" ON TOP OF
            // a classy-venue gate and a captain who has to already be sitting alone at a top — so without a
            // lever the whole scene (the entrance, the crossing, the ask, the note, the setup) is reachable
            // only by docking great ports over and over and sitting down at each of them. It forces WHETHER
            // and never WHO: who crosses the floor is the world's answer (is the fling posted here?), her
            // lines are the ones a captain gets, and whether this one is a setup is the seed's.
            string candidate = Uri.UnescapeDataString(pair["walkin=".Length..]).ToLowerInvariant();
            _walkInCheat = candidate switch
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
        else if (pair.StartsWith("tender=", StringComparison.OrdinalIgnoreCase))
        {
            // #1022 dev cheat: /map?tender=flash makes the tender's rare roll come up on the first beat of
            // every sitting at the galley card.
            //
            // Same philosophy as ?roll= above, and the same reason it is needed: the roll is a 1-in-12 on a
            // card most sessions open twice, so without a lever the beat is reachable only by luck. It
            // forces the ROLL and never the content — which line he reaches for is still his own salted
            // pick, what follows it still follows it, and the once-a-sitting law still holds. What a tester
            // watches play out is the beat a captain would get.
            string candidate = Uri.UnescapeDataString(pair["tender=".Length..]).ToLowerInvariant();
            _tenderFlashCheat = candidate is "flash" or "1" or "true" or "yes";
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>Which rock the shuttle goes down to, and whether it goes at all — <c>?secretlab=</c>,
    /// <c>?body=</c>, <c>?site=</c> and <c>?land=</c>.</summary>
    private bool ReadWhereTheLandingGoes(string pair, BootQuery q)
    {
        if (pair.StartsWith("secretlab=", StringComparison.OrdinalIgnoreCase))
        {
            // #409 dev cheat: /map?secretlab=1 spawns a plain LANDABLE rock parked in shuttle range at the
            // berth whose surface is GUARANTEED to hide one of Dr. Vantar's secret labs, with the hidden
            // door ALREADY REVEALED (a ⚙ HIDDEN DOOR console on the ground). The test loop is: shuttle
            // door → land → walk to the door → force it → read the logs → hit the core-log reveal.
            // Documented in the PR body. (Ordinary bodies hide labs rarely, off the seed — this is the
            // fast path.)
            string candidate = Uri.UnescapeDataString(pair["secretlab=".Length..]).ToLowerInvariant();
            q.SecretlabCheat = candidate is "1" or "true" or "yes" or "deep";


            // #592 · ?secretlab=deep parks a rock whose site HAS a band nobody listed. The ordinary
            // cheat rock's site is seeded like any other and happens to be four floors of records annex
            // with nothing under it, so #592 could not be reached from a URL at all — which is the exact
            // tax these cheats exist to remove.
            q.SecretlabDeep = candidate is "deep";
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
        else
        {
            return false;
        }

        return true;
    }

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
            string candidate = Uri.UnescapeDataString(pair["crew=".Length..]).ToLowerInvariant();
            if (candidate is "petition" or "deputation")
            {
                q.CrewCheat = "petition";
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

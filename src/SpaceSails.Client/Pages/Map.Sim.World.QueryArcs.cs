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
    /// <c>?approach=</c>, <c>?rep=</c>, <c>?kolt=</c>, <c>?walkin=</c>, <c>?finder=</c>, <c>?hurt=</c>, <c>?shelter=</c>, <c>?mags=</c>, <c>?watch=</c>,
    /// <c>?roll=</c>, <c>?tender=</c> and <c>?special=</c>.</summary>
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
        else if (pair.StartsWith("tailed=", StringComparison.OrdinalIgnoreCase))
        {
            // #1062 slice 2 dev cheat: /map?tailed=1 puts a man behind the captain at whatever berth he docks
            // at, whatever the outfit's folder says; /map?tailed=0 keeps the concourse empty.
            //
            // Same argument as ?rep= and ?kolt= above, and the sharpest case of the three. What puts him
            // there is #715 heat at the band where an outfit wants a face, and an honest route to that band
            // is a whole femme-fatale walk-in or a compromising chip sold at a dark-web desk — several
            // voyages, through content that has its own rarity on top. Without a lever the entire half of
            // #1062 (the chair that faces the door, the same coat through two doorways, the run of ticks that
            // shakes him, the line, the note) is unreachable in a session: "a scene nobody can reach on
            // demand is a scene that ships broken".
            //
            // It forces WHETHER and never WHO or WHAT: where he stands, how near he keeps, what makes him
            // findable, what it takes to lose him and every word said about any of it are the ones a captain
            // gets. Combine freely: /map?tailed=1&ashore=1&dock=selene-gate
            string candidate = Uri.UnescapeDataString(pair["tailed=".Length..]).ToLowerInvariant();
            _tailedCheat = candidate switch
            {
                "1" or "true" or "yes" or "now" => true,
                "0" or "false" or "no" or "never" => false,
                _ => null,
            };
        }
        else if (pair.StartsWith("kolt=", StringComparison.OrdinalIgnoreCase))
        {
            // #1061 beat 2 dev cheat: /map?kolt=1 puts Brem Kolt on this ground whatever his rota says;
            // /map?kolt=0 keeps him off it.
            //
            // The same argument as ?rep= above, and a sharper one, because his rota has a CEILING as well as
            // a period: one ground in three, and never more than two grounds in a whole universe. Without a
            // lever the entire beat — the approach, the three lines, the break, the run, the sheet in the
            // dust — is reachable only by landing on moon after moon and hoping, and then only twice ever.
            // It forces WHETHER and never WHO or WHAT: his lines, his prices, what he drops and the fact
            // that he runs are all the ones a captain gets.
            string candidate = Uri.UnescapeDataString(pair["kolt=".Length..]).ToLowerInvariant();
            _hardcaseCheat = candidate switch
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
        else if (pair.StartsWith("finder=", StringComparison.OrdinalIgnoreCase))
        {
            // #417 dev cheat: /map?finder=1 lets Ilse Varga cross this floor whatever else is true;
            // /map?finder=0 keeps her away.
            //
            // It forces WHETHER and never WHAT. Whether this world can furnish a case at all is still Core's
            // answer — a universe whose traffic has never shared a name between two hulls has no case in it,
            // and the lever cannot conjure one — and the witness, the ground, the two hulls, the berth and
            // the pay are all the ones a captain gets.
            string candidate = Uri.UnescapeDataString(pair["finder=".Length..]).ToLowerInvariant();
            _finderCheat = candidate switch
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

            // #746 · …and THE ROUND gets it too, because the checkpoint is an encounter now (GuardStop) and
            // a cheat that reached one scene and not the other would leave a tester unable to walk the four
            // outcomes of the stop this very issue is named after. It is a field of the round rather than a
            // twenty-second member of IPatrolHost, which may only shrink.
            _patrol.RollCheat = _rollCheat;
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
        else if (pair.StartsWith("special=", StringComparison.OrdinalIgnoreCase))
        {
            // #247 dev cheat: /map?special=story makes the bar board's rare outcome come up on the plate.
            //
            // Same philosophy as ?roll= and ?tender= above — and needed for a reason neither of those has.
            // This roll is seeded on the CAPTAIN as well as on the bar and the watch, which is the right
            // design and also means NO single URL can show a tester the rare line: every universe rolls its
            // own. It forces the ROLL and never the content — the dish is still this bar's dish, the price
            // is still this watch's price, the purse still moves, one Special a sitting still holds, and
            // the sentence is the authored one. What a tester watches is the plate a captain would get.
            string candidate = Uri.UnescapeDataString(pair["special=".Length..]).ToLowerInvariant();
            _specialStoryCheat = candidate is "story" or "1" or "true" or "yes";
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
            q.SecretlabCheat = candidate is "1" or "true" or "yes" or "deep" or "sealed";

            // #592 · ?secretlab=deep parks a rock whose site HAS a band nobody listed. The ordinary
            // cheat rock's site is seeded like any other and happens to be four floors of records annex
            // with nothing under it, so #592 could not be reached from a URL at all — which is the exact
            // tax these cheats exist to remove.
            q.SecretlabDeep = candidate is "deep";

            // #619 · ?secretlab=sealed parks a rock whose site carries THE REFUGE THAT FAILED, on B3. The
            // same sentence as the one above, one feature along: the welded room is on one floor of one
            // site in four, none of the other three cheat rocks happens to have one, and a beat nobody can
            // reach on demand is a beat that ships broken.
            q.SecretlabSealed = candidate is "sealed";
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
        else if (pair.StartsWith("parcel=", StringComparison.OrdinalIgnoreCase))
        {
            // #711 slice 2 dev cheat: /map?parcel=1 boots with an UNLISTED PARCEL already in the pocket and
            // rides ?land='s own descent down onto the ground that parcel is actually for — so the job row,
            // the walk, the DIG HERE press and the delivery are one URL instead of a berth re-roll and a
            // watch of waiting. It forges nothing: the parcel is minted the way the DESK mints one and the
            // cheat chooses only WHICH WINDOW, looking for a drop this berth can reach. Documented in
            // docs/testing-links-2026-09-17.md.
            string parcelWanted = Uri.UnescapeDataString(pair["parcel=".Length..]).ToLowerInvariant();
            _parcelCheat = parcelWanted is "1" or "true" or "yes";
        }
        else
        {
            return false;
        }

        return true;
    }
}

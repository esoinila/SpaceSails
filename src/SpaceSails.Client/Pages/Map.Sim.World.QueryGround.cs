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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — the ?query reader for what the ground is hiding — the floor, the hut, the dark, the book and the card.
public partial class Map
{

    /// <summary>What the ground has under it and what is lying about on it — <c>?floor=</c>,
    /// <c>?outpost=</c>, <c>?kit=</c>, <c>?dark=</c>, <c>?book=</c>, <c>?watchers=</c>,
    /// <c>?arrivalphase=</c>, <c>?found=</c> and <c>?card=</c>.</summary>
    private bool ReadWhatTheSiteIsHiding(string pair, BootQuery q)
    {
        if (pair.StartsWith("floor=", StringComparison.OrdinalIgnoreCase))
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
                q.SecretlabCheat = true;
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
                q.SecretlabCheat = true;
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}

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

// Subject: part of Map.Sim.World (#870 lane 7a; the header note lives in Map.Sim.World.cs) — the ?query readers for the Hive itself: the canteen, the restricted floor, the counter, the corridor and the park.
public partial class Map
{

    /// <summary>The B1 canteen and the four ways to be sat down in it — <c>?tablescene=</c>,
    /// <c>?spread=</c>, <c>?rip=</c> and <c>?threads=</c>. Each implies the whole route down rather than
    /// spelling another one, which is why the order these are written in is the order they are read.</summary>
    private bool ReadTheCanteenAndItsTops(string pair, BootQuery q)
    {
        if (pair.StartsWith("tablescene=", StringComparison.OrdinalIgnoreCase))
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
                q.TableSceneCheat = true;
                _freeTableCheat = candidate == "free";
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.TableSceneCheat = true;
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.TableSceneCheat = true;
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.TableSceneCheat = true;
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
                _landCheat = true;
                _startingFloorCheat = -1;   // B1 — the only floor with a hall and cabinets on it
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>B2, the shallowest floor anybody walks a round on — <c>?patrol=</c> and <c>?badge=</c>.</summary>
    private bool ReadTheRestrictedFloor(string pair, BootQuery q)
    {
        if (pair.StartsWith("patrol=", StringComparison.OrdinalIgnoreCase))
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
                ForceTheRoundsTo(1);
            }
            else if (candidate == "2")
            {
                ForceTheRoundsTo(2);
            }
            if (TheQueryHasForcedARound)
            {
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                MintTheSitePassAtTheLanding();
                ForceARoundIfNoneAsked();
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
                _landCheat = true;
                _startingFloorCheat = -2;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>The service side of the hall — <c>?counter=</c>, <c>?stool=</c> and
    /// <c>?neighbour=</c>/<c>?neighbor=</c>.</summary>
    private bool ReadTheCounterAndItsStools(string pair, BootQuery q)
    {
        if (pair.StartsWith("counter=", StringComparison.OrdinalIgnoreCase))
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
        else
        {
            return false;
        }

        return true;
    }

    /// <summary>Everything off the main corridor and behind the glass — <c>?park=</c>,
    /// <c>?frontdoor=</c>, <c>?parkwalk=</c>, <c>?ringoffice=</c>, <c>?goodscar=</c>, <c>?parkback=</c>,
    /// <c>?freight=</c> and <c>?designate=</c>.</summary>
    private bool ReadTheCorridorAndThePark(string pair, BootQuery q)
    {
        if (pair.StartsWith("park=", StringComparison.OrdinalIgnoreCase))
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
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
                q.SecretlabCheat = true;
                q.SecretlabDeep = true;
                _landCheat = true;
                _startingFloorCheat = -1;
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}

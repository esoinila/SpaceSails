using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the ?query cheats that stand the captain where a tester needs him.

/// <summary>
/// #464 · LAND ME, ALREADY. Owner, 2026-07-27: <i>"It is not ready until it is playtested in the
/// browser."</i>
///
/// <para>Every surface playtest began with a two-minute walk from the boot position to the shuttle hatch,
/// and scripted walking wedged on the bay wall often enough that the interesting states were being
/// verified by unit test instead of by eye. So <c>?land=1</c> rides the shuttle down the moment the world
/// is ready — the REAL boarding, the real descent phases, the real ground. It skips only the walk to the
/// hatch and the boarding panel, and nothing that matters.</para>
///
/// <para>#251 · This file keeps the landing itself. The other three are named for where they put the
/// captain once he is down: <c>.Stand</c> (the counter, the shelter, the park and the garden walk),
/// <c>.Building</c> (a ring office, the front door, the goods hoist), and <c>.Demo</c> (the rigs — the
/// designate demo, the goods car, the far side of the green, and the watchers).</para>
///
/// <para>Nothing in this family is a different world from the one a captain reaches by walking: a cheat
/// that shows a tester a different scene is worse than no cheat at all. The family declares no static
/// field. No member is renamed, re-scoped or re-ordered by the cut.</para>
/// </summary>
public partial class Map
{
    // #464 · LAND ME, ALREADY. Owner, 2026-07-27: "It is not ready until it is playtested in the browser."
    // Every surface playtest began with a two-minute walk from the boot position to the shuttle hatch, and
    // scripted walking wedges on the bay wall often enough that the interesting states (a charge, a blow
    // landing, five and down) were being verified by unit test instead of by eye. So: /map?land=1 rides the
    // shuttle down the moment the world is ready — the REAL BeginSurfaceExcursion, the real descent phases,
    // the real ground. It skips only the walk to the hatch and the boarding panel, nothing that matters.
    private bool _landCheat;

    /// <summary>Which body <c>?land=&lt;bodyId&gt;</c> asked for, or null for "the first one in reach". Matched on
    /// id OR name, case-insensitively, because a playtester types what they see on the map.</summary>
    private string? _landBodyCheat;

    private async Task AutoLandForCheatAsync()
    {
        if (!_landCheat || _surface is not null)
        {
            return;
        }
        // The same board the hatch would show, so the cheat can never reach somewhere the player could not.
        // #488: when a DERELICT is in reach she wins the toss — ?wreck=1&land=1 is the one-URL way onto her,
        // the same promise ?land=1 makes for a surface. Without this the cheat lands on whatever moon happens
        // to be nearer and the wreck is unreachable except by walking the deck to the shuttle bay.
        List<ShuttleStop> board = [.. ShuttleDestinationsInRange()];

        // #585 / #633 · A NAMED BODY WINS THE TOSS OUTRIGHT, so any ground can be opened and LOOKED at from
        // one URL. Two spellings reach this line — `?body=<id>` (#585) and `?land=<id|name>` (#464) — because
        // the branches grew one each; BOTH still parse, and they resolve through this ONE lookup rather than
        // two, which is the only way they can be guaranteed to land in the same place.
        //
        // The named body must still be on the board — the cheat may never reach somewhere the player could
        // not — and when it is not, it REFUSES and says so. A cheat that silently lands you somewhere else is
        // worse than one that refuses, because you playtest the wrong scene and trust the result.
        if ((_landBodyCheat ?? _forcedLandingBodyId) is { } wanted)
        {
            ShuttleStop? named = board.FirstOrDefault(
                s => s.IsLandable
                     && (string.Equals(s.Body.Id, wanted, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(s.Body.Name, wanted, StringComparison.OrdinalIgnoreCase)));

            if (named is null)
            {
                string inReach = string.Join(", ", board.Where(s => s.IsLandable).Select(s => s.Body.Id));
                ShowPulseMessage($"🧪 DEV ?land={wanted}: not landable from this berth. In reach: " +
                                 (inReach.Length > 0 ? inReach : "nothing"));
                _landCheat = false;
                return;
            }

            await RideTheShuttleDownForCheatAsync(named);
            return;
        }

        ShuttleStop? target =
            board.FirstOrDefault(s => s.IsLandable && Derelict.TryParseWreckId(s.Body.Id, out _))
            ?? board.FirstOrDefault(s => s.IsLandable);
        if (target is null)
        {
            ShowPulseMessage("🧪 DEV ?land=1: nothing landable in shuttle reach from this berth.");
            return;
        }

        await RideTheShuttleDownForCheatAsync(target);
    }

    /// <summary>
    /// The descent both <c>?land=1</c> and <c>?land=&lt;bodyId&gt;</c> ride. One method on purpose: two copies of
    /// "land, then put the boots somewhere sensible" would drift, and the drift would be invisible until a
    /// playtester landed by name and found themselves standing in vacuum.
    /// </summary>
    private async Task RideTheShuttleDownForCheatAsync(ShuttleStop target)
    {
        LandingSite site = LandingSites.For(target.Body.Id)[
            Math.Clamp(_forcedSiteIndex ?? 0, 0, LandingSites.For(target.Body.Id).Count - 1)];
        // Bring the sling down loaded — a cheat that lands you empty-handed made [T] look broken
        // (owner: "why are there no sentries to plant?" / "Button T stopped working?").
        await BeginSurfaceExcursion(target, ShuttleExcursion.Pack(0, _credits, []), botsToBring: 2, site: site);

        // #470: and put the boots OUT ON THE GROUND, not at the tube mouth. The cheat exists so the surface
        // can be playtested at all; landing at the threshold still left a long walk down-field before
        // anything could reach the captain, which is the walk the cheat was invented to remove. Drop them in
        // the open regolith short of the deep field — far enough out that the pack can actually arrive, close
        // enough that the way home is still a real run.
        // #488: a DERELICT has no regolith and no landing band, so the open-ground drop above is meaningless
        // inside her — MoonSurface's coordinates would put the away team OUTSIDE the hull, standing in
        // vacuum next to the ship they came to search. She keeps her own spawn, just inside her airlock.
        if (_surface is { } landed && Derelict.TryParseWreckId(landed.Stop.Body.Id, out _))
        {
            StandCaptainAt(WreckInterior.SpawnX, WreckInterior.SpawnY,
                "the boarding tube lets you out into her airlock");
            return;
        }

        if (_surface is not { } landedOn)
        {
            return;
        }

        // #585 · ?secretlab=1&land=1 PUTS YOU AT THE SHAFT. Owner, after an evening of walking a 310 x 260
        // field to reach the one thing being tested: "instruct to put the debug cheat start next to the lab
        // so that it can be really tested without playing to find it" — "I mean next to the elevator shaft".
        //
        // The hunt is the GAME (the clue, the wash, the detector gradient), and it is exactly what must not
        // stand between a developer and the feature under test. Every one of the open Hive issues — the
        // sealed rooms, the visualiser, the card, the tracker, the unlisted band — needs the captain standing
        // at the lift head within seconds, repeatedly.
        //
        // Only under the secret-lab cheat: an ordinary landing still drops you on the open regolith, so this
        // can never quietly become how the game plays.
        if (_secretLabForceBodyId == landedOn.Stop.Body.Id && landedOn.Lab is { HasLab: true })
        {
            // A pace outside the shed's door, facing it — not inside, so the walk in is still walked and the
            // door, the label and the console are all exercised the way a player meets them.
            //
            // #681 · ASKED OF THE SHED. This used to take the head spot and subtract 7.5 from its Y, which was
            // a pace clear of the door back when the head was a hand-typed 10 x 8 box — and was the far wall
            // the moment #606 made it an ordinary hut with a seeded angle. The owner landed inside it:
            // "The second url put me into the wall... I cannot move." One building, one answer, and the
            // landing reads it the same way the returning car reads CarFloor.
            (double hx, double hy) = MoonSurface.LiftHead(
                landedOn.Stop.Body.Id, landedOn.Site.LayoutSalt, MoonSurface.ExpeditionField()).DoorStep;

            ShowPulseMessage(_foundCheat
                ? "🧪 DEV ?found=1: set down at the lift head, with the wallet already full. Ride down."
                : "🧪 DEV ?secretlab=1: set down at the lift head. The shed is in front of you.");
            StandCaptainAt(hx, hy, "the shuttle sets you down on the lift head's doorstep");

            // ── #677 · …AND THE PAPERWORK, because the halls are behind two gates and one of them is the
            // rarest object in the game. Every band this site has gets its card, minted through the SAME
            // AuthorityCard the rooms mint, put in the SAME satchel the pockets hold — so the lift panel,
            // the gate, the refusal ladder and the wallet fan all behave exactly as they do for a captain
            // who earned them. A cheat that seeded a private "you may descend" flag would be testing a
            // second mechanism that does not ship.
            if (_foundCheat)
            {
                // Every band index the arithmetic admits, and NOT "until one is missing": there is a whole
                // band with nothing in it between the unlisted floors and the halls, so a loop that stopped
                // at the first absence would hand out every card except the only one this cheat exists for.
                int last = UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor);
                for (int band = 0; band <= last; band++)
                {
                    if (!UndergroundComplex.SiteHasBand(landedOn.Stop.Body.Id, band))
                    {
                        continue;
                    }
                    var card = new UndergroundComplex.AuthorityCard(landedOn.Stop.Body.Id, band);
                    _satchel = [.. Core.Satchel.Add(
                        _satchel, new Core.Satchel.Item(Core.Satchel.Kind.Authority, card.Id))];
                }
            }

            // ── #693 · …OR ONE CARD, WHICH IS THE ROW AND THE BEAT AND THE REFUSAL ────────────────────────
            //
            // #692's own honest note: "reaching the row needs an authority card in the wallet and no dev
            // cheat mints one." ?found=1 above hands over every authority a site issued, but it also parks a
            // particular rock, so the carded lift row and the gate beat could not be seen on an ordinary
            // site at all. Same mint, same satchel, same gate — one card instead of the set.
            //
            // The band the site does not have is deliberately NOT invented: the pocket stays empty and the
            // line says which bands there were, because a cheat that silently gives you nothing is a tester
            // playtesting the wrong scene without knowing it.
            if (_cardCheat is { } asked)
            {
                string cardBody = landedOn.Stop.Body.Id;
                int standingOn = _startingFloorCheat ?? 0;
                var wanted = new List<int>();
                int deepestBand = UndergroundComplex.BandOf(UndergroundComplex.DeepestPossibleFloor);

                if (asked == "all")
                {
                    for (int band = 0; band <= deepestBand; band++)
                    {
                        wanted.Add(band);
                    }
                }
                else if (asked == "next")
                {
                    // ASKED OF THE BUILDING, exactly as the panel asks it: the shaft that EXISTS below where
                    // you are standing, stepping over the band of nothing under the unlisted floors (#677).
                    // Working it out here as "band + 1" is §13.15's second cause, and it has already been
                    // the cause of something three times on this ground.
                    if (UndergroundComplex.NextShaftBelow(cardBody, standingOn) is { } next)
                    {
                        wanted.Add(next);
                    }
                }
                else if (int.TryParse(asked, System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture, out int askedBand))
                {
                    wanted.Add(askedBand);
                }

                var minted = new List<int>();
                foreach (int band in wanted)
                {
                    if (!UndergroundComplex.SiteHasBand(cardBody, band))
                    {
                        continue;
                    }
                    var card = new UndergroundComplex.AuthorityCard(cardBody, band);
                    _satchel = [.. Core.Satchel.Add(
                        _satchel, new Core.Satchel.Item(Core.Satchel.Kind.Authority, card.Id))];
                    minted.Add(band);
                }

                var has = new List<string>();
                for (int band = 0; band <= deepestBand; band++)
                {
                    if (UndergroundComplex.SiteHasBand(cardBody, band))
                    {
                        has.Add(band.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

                ShowPulseMessage(minted.Count > 0
                    ? $"🧪 DEV ?card={asked}: band {string.Join(", ", minted)} authority in the wallet — " +
                      "🎒 I to read it, then ride and watch the row."
                    : $"🧪 DEV ?card={asked}: this site has no such band, so nothing was minted. It has " +
                      $"band {string.Join(", ", has)}.");
            }

            // #804 · …and ?badge=1 mints THIS SITE'S own pass, the cage chit and a FALSE ID, before the car
            // moves, so the guard the ride is about to walk you into has all four rungs of his read
            // available. Site-scoped like the card above and minted the same way — into the real wallet,
            // with the real ids, through the real producers, so what the guard says is what he would have
            // said about papers that were earned and found.
            if (TheSitePassIsMintedAtTheLanding)
            {
                string passBody = landedOn.Stop.Body.Id;
                if (!PatrolBeat.BadgeHeld(passBody, _satchel))
                {
                    _satchel = [.. Core.Satchel.Add(_satchel, PatrolBeat.Badge(passBody))];
                }

                // #836 · …and the paper that makes it a CHOICE. A captain who worked the lane arrives on
                // this floor carrying both — the cage chit is never spent, it just stops being cover once
                // you are off the cage — so the cheat mints the same wallet the earned road produces rather
                // than a wallet nobody could ever have. Two papers is what opens the fan (WalletChoice.Fans).
                if (!CanteenTable.Cover.Held(_satchel))
                {
                    _satchel = [.. Core.Satchel.Add(_satchel, CanteenTable.Chit(underAnotherName: true))];
                }

                // #804 · …and the FALSE ID, which is the fourth rung of the read and the one the wallet has
                // been able to judge since #836 without any world ever dealing it. THROUGH THE PRODUCER, and
                // that is the whole point of this line rather than a badge for some other body composed here:
                // a cheat that minted its own foreign pass would be a second answer to what a false ID is,
                // and the dev path would drift off the real one the first afternoon somebody tuned either.
                // Null on a world with nowhere else to have issued one, and then the cheat simply deals it
                // not — a wallet nobody could ever have is exactly what this cheat is written against.
                Satchel.Item? falseId = AFalseIdFoundAt(passBody, DiceRule.Seed($"false-id:dev:{passBody}"));
                if (falseId is { } elsewhere && !PatrolBeat.BadgeHeld(
                        PatrolBeat.SiteOfBadge(elsewhere.Id) ?? string.Empty, _satchel))
                {
                    _satchel = [.. Core.Satchel.Add(_satchel, elsewhere)];
                }

                ShowPulseMessage(
                    $"🧪 DEV ?badge=1: {PatrolBeat.BadgeGlyph} {PatrolBeat.BadgeTitle(passBody)}, " +
                    (falseId is { } shown ? $"{FoundPass.Plate(shown)}, " : "") +
                    $"{CanteenTable.ChitGlyph} the cage chit are in the wallet — 🎒 I to read them, then " +
                    "let a round find you and pick which one of you he meets.");
            }

            // ...and ?floor=N goes the rest of the way down, because half the open work on this feature is
            // about what a FLOOR looks like rather than about finding the way in.
            if (_startingFloorCheat is { } askedFor)
            {
                // #592/#677 · ASKED OF THE BUILDING, not worked out here. This used to clamp to the true
                // bottom and then snap into the unlisted band's shaft head — correct arithmetic for a
                // building with ONE gap in it, and a captain set down in solid rock the day there were two
                // (the band of nothing under the unlisted band, #677). A caller doing its own geometry about
                // a building it does not own is §13.15's second cause, and this is the third time it has
                // been the cause of something.
                string cheatBody = landedOn.Stop.Body.Id;
                // #801 · …in the cage, because that is the car a boot arrives in and the one every route
                // in the testing guide is written from. A cheat that inherited whichever car was last
                // pressed would put a tester somewhere different depending on their last excursion.
                _liftCar = UndergroundComplex.ShaftKind.Cage;
                RideTheLiftTo(landedOn, UndergroundComplex.NearestFloorTo(cheatBody, askedFor));

                // #746 · …and ?tablescene=1 goes the last leg too, because the scene under test is a
                // conversation at a table and the lift head is the other end of the floor from it.
                StandInTheCanteenIfAsked(landedOn);

                // #756 · …and ?counter=1 goes the same last leg to the other fixture in that room.
                StandAtTheCounterIfAsked(landedOn);

                // #759 · …and ?park=1 goes one room further, through the gate at the end of the corridor.
                StandInTheParkIfAsked(landedOn);

                // #775 · …and these two stop SHORT of the room: out on the MAIN CORRIDOR at the hall's own
                // front door, and in front of the goods hoist that will not open for you.
                StandAtTheFrontDoorIfAsked(landedOn);
                StandAtTheGoodsHoistIfAsked(landedOn);
                StandAtTheGardenWalkIfAsked(landedOn);

                // #813 · …and this one goes the other way through the same glass: INSIDE a room on the
                // ring, with the park out of the window wall. The only row in the game on that side of it.
                StandInARingOfficeIfAsked(landedOn);

                // #803 · …and this one rigs the whole loop at that shutter: a gun set down beside you with
                // a hut's worth of rounds in your pocket and not enough in the drum.
                RigTheDesignateDemoIfAsked(landedOn);

                // #801 · …and the two rows this PR owes a tester: the car at the OTHER end of the corridor,
                // and the far side of the green.
                StandAtTheGoodsCarIfAsked(landedOn);
                StandBehindTheParkIfAsked(landedOn);
            }
            return;
        }

        // #681 · Asked of the field, not retyped. "SpawnX, LandingBandY − 12" was a spot nothing had ever
        // reserved, so the generator built a hut through it on two sites and the drop came down inside a wall
        // — the ordinary landing's version of the same bug the lift head had. SurfaceLayout.LandingApproach
        // is now the ONE answer: this reads it to know where to drop, and the claim ledger reads it to know
        // what to keep clear.
        (double dropX, double dropY, _) = SurfaceLayout.LandingApproach(MoonSurface.ExpeditionField());
        StandCaptainAt(dropX, dropY, "the shuttle sets you down on the open regolith below the pad");

        // #728 · …and ?shelter=1 walks the last leg, which is the longest walk on the ground: the shelters
        // are seeded DEEP by design and the fixtures under test are inside one of them.
        StandAtTheShelterIfAsked(landedOn);
    }
}

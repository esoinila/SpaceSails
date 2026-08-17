using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the ?query cheats that stand the captain where a tester needs him.
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

            // #804 · …and ?badge=1 mints THIS SITE'S own pass, before the car moves, so the guard the ride
            // is about to walk you into has something to read. Site-scoped like the card above and minted
            // the same way — into the real wallet, with the real id, so what the guard says is what he
            // would have said about a pass that was earned.
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

                ShowPulseMessage(
                    $"🧪 DEV ?badge=1: {PatrolBeat.BadgeGlyph} {PatrolBeat.BadgeTitle(passBody)} and " +
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

    // #458: how many Old Ones /map?reevers=N asks for on the first landing. 0 = the cheat is off. They are
    // roused in the DEEP and come to you (#461) — never set down on the landing pad, which read as the Old
    // Ones somehow knowing where the shuttle would touch down.
    private int _reeverAmbushCheat;

    /// <summary>#756 QA · <c>?counter=1</c> — the whole route to a counter that takes orders, booted. Set in
    /// Map.Sim's cheat parse; read here, where the last leg is walked.</summary>
    private bool _counterCheat;

    /// <summary>#774 QA · <c>?kit=1</c> — the field dossier assembles on the FIRST piece of somebody's kit,
    /// and with every saying it can carry. Set in Map.Sim's cheat parse; read in
    /// <see cref="AssembleSomebody"/>, which is the one place both gates are asked about.</summary>
    private bool _kitCheat;

    /// <summary>#756 QA · Stand the captain AT THE COUNTER when <c>?counter=1</c> asked for it.
    ///
    /// <para>The amenity's own published spot, which is the service side of the counter and the very square
    /// the console dot is drawn on — so this walks the captain to the fixture rather than to a coordinate
    /// somebody measured off a picture of it. Through <c>StandCaptainAt</c>, so the pad-crew net (#681) has
    /// its say if the hall ever carves a table onto that square.</para>
    ///
    /// <para>#827 · That spot is the counter's own <c>ServiceRun.StandX/StandY</c> now — published rather
    /// than implied, and in exactly the place it always was. What moved onto the desk is the [E] RUN, which
    /// is the desk's front face, and the rail the deck draws down it.</para></summary>
    private void StandAtTheCounterIfAsked(SurfaceExcursion ex)
    {
        if (!_counterCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (Core.Interior.CounterService.For(ex.Stop.Body.Id, a.Use) is not { } counter)
            {
                continue;
            }

            StandCaptainAt(a.X, a.Y, "you step up to the counter");

            // #756 · …and ?stool=1 walks the last, last leg: the card is opened and a stool is taken, so the
            // posture this issue is about is one URL away rather than one URL and two presses. Through the
            // very handlers [E] and the button reach, so what a tester lands in is what a captain gets —
            // including WHICH seat is free, which is the room's answer off the frozen watch and never the
            // cheat's own.
            if (_stoolCheat)
            {
                // #781 · Through Core's own fork, so ?stool=1&watch=2 seats a tester in front of the KEEP and
                // ?stool=1&watch=5 in front of the machine — which is the difference this issue is about, and
                // it must not be a thing only the real press path knows.
                OpenCounterService(Core.Interior.CounterService.OnWatch(counter, ex.CanteenWatch));
                TakeAStool();
                ShowPulseMessage(
                    "🧪 DEV ?stool=1: up on a stool at THE COUNTER, menu and all. Press Wait — and add "
                    + "?neighbour=1 to make the one beside you turn, or ?neighbour=0 to hear the silence.");
                return;
            }

            ShowPulseMessage(
                Core.Interior.TheKeep.KeptWatch(ex.CanteenWatch)
                    ? "🧪 DEV ?counter=1: THE COUNTER, in reach, on a watch somebody is working it. Press E "
                      + "to open the card — then Hear a rumour, and come back on a later watch."
                    : "🧪 DEV ?counter=1: THE COUNTER, in reach, on a dead watch — it serves itself. Press E "
                      + "to open the card, and add ?watch=2 to find somebody behind it.");
            return;
        }
    }

    /// <summary>#728 QA · <c>?shelter=1</c> — the boots set down at a shelter's door. Set in Map.Sim's cheat
    /// parse; read in <see cref="StandAtTheShelterIfAsked"/>, the last leg of the landing.</summary>
    private bool _shelterCheat;

    /// <summary>#728 QA · <c>?mags=N</c> — what each sentry is holding as it comes down the tube. Set in
    /// Map.Sim's cheat parse; applied in <see cref="BeginSurfaceExcursion"/> at the one place a magazine
    /// crosses into an excursion, and never later — the readout, the receipts and both of the locker's
    /// refusals all read this number, and a cheat that wrote it afterwards would show a tester one captain in
    /// the instrument and a different one at the press.</summary>
    private int? _magazineCheat;

    /// <summary>#728 QA · Stand the captain at a SHELTER when <c>?shelter=1</c> asked for it.
    ///
    /// <para>A pace outside the door of the first shelter in the site's own stable list, facing it — the same
    /// ruling as <c>?secretlab=1</c>'s doorstep drop. Not inside: the proximity cycle, the arrival line and
    /// the pressure crossing are all part of what a tester came to look at, and a drop into the drum would
    /// skip every one of them. The door's spot is asked of the building
    /// (<c>SurfaceStructure.Build</c>) rather than measured off a picture of it, and the step outward is
    /// taken along the door's own outward normal, so a seeded angle cannot put the boots in a wall.</para></summary>
    private void StandAtTheShelterIfAsked(SurfaceExcursion ex)
    {
        if (!_shelterCheat || ex.Floor < 0)
        {
            return;
        }

        foreach (SurfaceStructure.Spec shelter in SheltersOn(ex))
        {
            foreach (SurfaceStructure.Doorway door in SurfaceStructure.Build(shelter).Doorways)
            {
                double outX = door.CentreX - shelter.CentreX, outY = door.CentreY - shelter.CentreY;
                double reach = Math.Sqrt((outX * outX) + (outY * outY));
                if (reach <= 1e-6)
                {
                    continue;
                }

                const double APaceClear = 4.0;
                ShowPulseMessage(
                    "🧪 DEV ?shelter=1: set down at a SHELTER. Walk in — the rack fills your tank, the "
                    + "press fills your magazines, and the readout under the tracker says what is in them.");
                StandCaptainAt(
                    door.CentreX + (outX / reach * APaceClear),
                    door.CentreY + (outY / reach * APaceClear),
                    "the shuttle sets you down at the shelter's door");
                return;
            }
        }

        ShowPulseMessage("🧪 DEV ?shelter=1: this ground has no shelter with a door to stand at.");
    }

    /// <summary>#759 QA · <c>?park=1</c> — the route to the park, booted. Set in Map.Sim's cheat parse.</summary>
    private bool _parkCheat;

    /// <summary>#759 QA · Stand the captain INSIDE THE PARK when <c>?park=1</c> asked for it.
    ///
    /// <para>On the park's own published plate spot — just inside the gate, looking down the first bend of
    /// the gravel — rather than at a coordinate somebody read off a screenshot. The attendance note fires on
    /// the very next tick, which is the point of the row.</para></summary>
    private void StandInTheParkIfAsked(SurfaceExcursion ex)
    {
        if (!_parkCheat || ex.Floor >= 0)
        {
            return;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green)
        {
            return;
        }

        // #793 · …and ?park=1&spread=1 goes one leg further — onto a BENCH, with three finds in the sleeve
        // and the whole plank to yourself. Through the same handler [E] reaches, at one of the room's own
        // benches: a dev row that assembled its own sitting would demonstrate a bench that does not ship.
        if (SitOnAFreeBenchIfAsked(in green))
        {
            return;
        }

        ShowPulseMessage(
            "🧪 DEV ?park=1: THE PARK. Green underfoot, the window wall back to the bar, beds and benches "
            + "down the curve — press E at one to SIT DOWN (#793).");
        StandCaptainAt(green.X, green.Y, "you step through the gate onto the gravel");
    }

    /// <summary>#775 QA · <c>?frontdoor=1</c> — booted on the MAIN CORRIDOR, at the hall's own entrance.
    /// Set in Map.Sim's cheat parse.</summary>
    private bool _frontDoorCheat;

    /// <summary>#775 QA · <c>?freight=1</c> — booted at the GOODS HOIST. Set in Map.Sim's cheat parse.</summary>
    private bool _freightCheat;

    /// <summary>#775 QA · <c>?parkwalk=1</c> — booted on the main corridor at the mouth of the garden
    /// walk. Set in Map.Sim's cheat parse.</summary>
    private bool _parkWalkCheat;

    /// <summary>#775 QA · Stand the captain on the SPINE, at the mouth of a gate down through the near band.
    ///
    /// <para>#813 · This used to hunt for <i>the one gate no rib stands on</i>, because that was what a
    /// garden walk WAS: a passage cut off the spine on the one column the seeded ribs had left free. The
    /// Manhattan carve took the seeding away — a rib on B1 runs DOWN only if it is one of the block's own
    /// gate columns — so "the gate no rib stands on" now selects NOTHING, and a row that selects nothing
    /// leaves the captain wherever the lift left them and tells nobody. That is the sharpest shape a dev
    /// row goes wrong in: the code still runs, the URL still parses, and the loop simply never fires.</para>
    ///
    /// <para>What it asks for now is the LAW rather than the accident: a gate in the park's NEAR wall — one
    /// that is horizontal and stands on the park's own near line (<c>green.Y1</c>), which is exactly the
    /// definition of "you reach it by walking off the spine" — taken off the room's published
    /// <see cref="UndergroundComplex.Park.Ways"/> list, never off a coordinate measured from a picture. The
    /// tester still starts on the main corridor, because the feature is a CROSSING: walk down, over the
    /// gravel, and out of any one of the other five.</para></summary>
    private void StandAtTheGardenWalkIfAsked(SurfaceExcursion ex)
    {
        if (!_parkWalkCheat || ex.Floor >= 0)
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, field);
        if (floor.Park is not { } green)
        {
            return;
        }
        (_, double shaftY) = UndergroundComplex.ShaftAt(field);

        foreach (SurfaceLayout.Doorway gate in green.Ways)
        {
            // A NEAR gate. The two on the back street are horizontal too but stand on the park's FAR line,
            // and the west and east gates are vertical — none of the three is a thing you find by walking
            // the spine, which is the leg this row exists to start.
            if (Math.Abs(gate.Y1 - gate.Y2) > 0.001 || Math.Abs(gate.Y1 - green.Y1) > 0.001)
            {
                continue;
            }

            // Just inside the spine, at the gate column's mouth: the corridor face is CorridorHalf off the
            // shaft line, so a step short of it is a captain standing in the main corridor looking down it.
            double gx = (gate.X1 + gate.X2) / 2.0;
            double standY = shaftY + (green.Y1 < shaftY ? -1.5 : 1.5);
            StandCaptainAt(gx, standY, "you stop on the main corridor at the mouth of the walk");
            ShowPulseMessage(
                "🧪 DEV ?parkwalk=1: a GATE THROUGH THE BLOCK, off the main corridor. Walk down it, cross "
                + "the gravel, and come out somewhere else entirely — the green is the middle of a city "
                + "block now and it is crossed from all FOUR sides: two gates off the spine, two off the "
                + "back street, and one through each end of the block.");
            return;
        }
    }

    /// <summary>#813 QA · <c>?ringoffice=1</c> — booted INSIDE a room on the ring, facing its own glass.
    /// Set in Map.Sim's cheat parse.</summary>
    private bool _ringOfficeCheat;

    /// <summary>
    /// #813 QA · Stand the captain INSIDE a ring office, a few paces back from its window wall.
    ///
    /// <para>Every other park row in this file stands a tester on the GRAVEL, which is the park's side of
    /// the glass and the only side anybody has ever been shown. The Manhattan ruling's claim is about the
    /// OTHER side of it — <i>"make sure the park prime real estate is not wasted and not unused"</i> — and
    /// prime real estate is a thing you check by standing in the room that paid for the view and looking
    /// out of it. Until this row there was no URL in the game that put you in one.</para>
    ///
    /// <para>Off the ring's own published frontage (<see cref="UndergroundComplex.Park.Frontage"/>): the
    /// first room with a view on the SPINE side, which is the premium band, falling back to any room with a
    /// view at all. Nothing in that list is ever the hall — the bar is published as this floor's Amenity
    /// and as the park's own Window, and Core keeps it out of the ring rather than mirror it — so this
    /// cannot quietly boot a tester into the room four other URLs already reach.</para>
    ///
    /// <para>The spot is measured off the room's OWN two walls rather than typed: back from the view wall
    /// toward the room's middle, by a few paces or by half the room's depth where the room is shallower
    /// than that. A number typed here would be right for the fifty-du near band and inside the back wall of
    /// a thirteen-du store.</para></summary>
    private void StandInARingOfficeIfAsked(SurfaceExcursion ex)
    {
        if (!_ringOfficeCheat || ex.Floor >= 0)
        {
            return;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green)
        {
            return;
        }

        UndergroundComplex.RingRoom? chosen = null;
        foreach (UndergroundComplex.RingRoom room in green.Frontage)
        {
            if (!room.HasView)
            {
                continue;   // a corner room stands past the end of the park's wall and has nothing to see
            }
            if (room.Side == UndergroundComplex.RingSide.Near)
            {
                chosen = room;
                break;
            }
            chosen ??= room;
        }
        if (chosen is not { } office || office.View is not { } glass)
        {
            return;
        }

        // A FEW PACES BACK FROM THE GLASS, toward the room's own middle — so the pane is in front of the
        // captain and the green is through it. WHICH WAY back is comes from the room and not from the side:
        // the near band looks down at the park and the far band looks up at it, and this asks rather than
        // assumes, which is the same reason the front-door row asks the shaft which face the hall is on.
        const double PacesBackFromTheGlass = 6.0;
        bool horizontal = Math.Abs(glass.Y1 - glass.Y2) < 0.001;
        double depth = horizontal ? office.Y1 - office.Y0 : office.X1 - office.X0;
        double back = Math.Min(PacesBackFromTheGlass, depth / 2.0);
        double standX = horizontal ? office.X : glass.X1 + (Math.Sign(office.X - glass.X1) * back);
        double standY = horizontal ? glass.Y1 + (Math.Sign(office.Y - glass.Y1) * back) : office.Y;

        StandCaptainAt(standX, standY, "you step in off the street and the whole front wall is green");
        ShowPulseMessage(
            "🧪 DEV ?ringoffice=1: INSIDE a room on the ring, looking OUT through its glass at the park — "
            + $"{office.Plate}. The door behind you is on a street; the wall in front of you is a window "
            + "and it still stops you. Then walk the block: every side of the green is somebody's front "
            + "wall now.");
    }

    /// <summary>#775 QA · Stand the captain OUT ON THE SPINE, at the hall's front door.
    ///
    /// <para>The captain is put on the corridor side of the room's FIRST published opening on the spine —
    /// the entrance, the one the carve placed at the lift — so the walk that proves the feature is the one
    /// the owner could not make: face the wall, read the plate, step through. Off the hall's own published
    /// door list, never a coordinate measured off a screenshot.</para></summary>
    private void StandAtTheFrontDoorIfAsked(SurfaceExcursion ex)
    {
        if (!_frontDoorCheat || ex.Floor >= 0)
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        (_, double shaftY) = UndergroundComplex.ShaftAt(field);

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, field).Amenities)
        {
            if (a.Hall is not { } hall)
            {
                continue;
            }
            foreach (SurfaceLayout.Doorway d in hall.Openings)
            {
                if (Math.Abs(d.Y1 - d.Y2) > 0.001)
                {
                    continue;   // a gap in the rib's face, which is the way in this issue is about NOT using
                }

                // Two du out of the doorway, on whichever side the spine is — asked of the shaft rather
                // than assumed, because half the floors in the game hang their hall off the other face.
                double standY = d.Y1 + (d.Y1 < shaftY ? 2.0 : -2.0);
                StandCaptainAt((d.X1 + d.X2) / 2.0, standY,
                    "you come along the main corridor and stop at the door");
                ShowPulseMessage(
                    "🧪 DEV ?frontdoor=1: out on the MAIN CORRIDOR at the canteen's own entrance — the "
                    + "plate is on the wall beside you. Walk in. Then walk the corridor and count the "
                    + "others: a hall this size is required to have them.");
                return;
            }
        }
    }

    /// <summary>#775 QA · Stand the captain in front of the GOODS HOIST, on the hall floor.
    ///
    /// <para>On the fixture's own published plate spot. Pressing the shutter reads the refusal; there is
    /// nothing else to do with it, and that is the whole of the feature.</para></summary>
    private void StandAtTheGoodsHoistIfAsked(SurfaceExcursion ex)
    {
        if (!_freightCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (a.Hall is not { Freight: { } hoist })
            {
                continue;
            }
            StandCaptainAt(hoist.PlateX, hoist.PlateY, "you cross to the shutter at the end of the counter");
            ShowPulseMessage(
                "🧪 DEV ?freight=1: THE GOODS HOIST, at the end of the counter's own service band. Press E "
                + "on the shutter: it tells you whose side of it you are on.");
            return;
        }
    }

    /// <summary>
    /// #803 QA · THE WHOLE MANUAL-FIRE LOOP, AT THE ONE SHUTTER IT WAS WRITTEN FOR.
    ///
    /// <para>Owner's own scenario: a hut with a few rounds in it, and a lock worth spending them on. The
    /// pieces are two rooms and one lift ride apart on a real run, so the row assembles them: the captain is
    /// standing at the GOODS HOIST (the <c>?freight=1</c> boot), one sentry is SET DOWN beside them with
    /// four rounds in it — one short of a hasp, deliberately, so the put verb is not optional — and a hut's
    /// find is loose in the pocket.</para>
    ///
    /// <para>Nothing here is a different world from the one a captain reaches by walking. The shutter is the
    /// shutter, the rounds are ordinary rounds, and the six that come off the drum are the six the law
    /// charges: a cheat that shows a tester a different scene is worse than no cheat at all.</para></summary>
    private void RigTheDesignateDemoIfAsked(SurfaceExcursion ex)
    {
        if (!_designateCheat)
        {
            return;
        }

        SurfaceBot? gun = ex.Bots.FirstOrDefault(b => !SurfaceArrival.IsDoorSentry(b.Unit));
        if (gun is not null)
        {
            // Where the [T] key would have put it: at the captain's own boots. Not a hand-picked spot two du
            // to one side — that could be inside the car's divider on some floors, and a cheat that stands a
            // machine in a wall is a cheat that playtests a world nobody can reach.
            gun.Deployed = true;
            gun.X = _avatarX;
            gun.Y = _avatarY;
            gun.AimX = gun.X;
            gun.AimY = gun.Y - 1;
            gun.Rounds = DesignateDemoRoundsInDrum;
            gun.AmmoId = Core.Ammunition.Issue.Id;
        }

        _satchel = [.. Core.Satchel.Add(_satchel,
            new Core.Satchel.Item(Core.Satchel.Kind.Rounds, Core.Ammunition.Issue.Id, DesignateDemoRoundsInPocket))];

        RebuildSurfaceDeck();
        ShowPulseMessage(
            $"🧪 DEV ?designate=1: {gun?.Unit ?? "your sentry"} is SET DOWN beside you reading "
            + $"{SentryBot.Readout(DesignateDemoRoundsInDrum)}, and there are {DesignateDemoRoundsInPocket} "
            + "loose rounds in your pocket. Press I over the bot to hand-load it, then 📻 Remote → "
            + $"{ShootTheLock.OpenLabel} and point it at the shutter.");
    }

    /// <summary>#803 QA · One short of a hasp, so the demo cannot be completed without hand-loading.</summary>
    private const int DesignateDemoRoundsInDrum = ShootTheLock.RoundsPerHasp - 1;

    /// <summary>#803 QA · A hut's find, in the pocket — the size <c>SurfaceOutpost.CacheRounds</c> deals in.</summary>
    private const int DesignateDemoRoundsInPocket = 12;

    private bool _designateCheat;

    /// <summary>#801 QA · <c>?goodscar=1</c> — booted at the SECOND CAR. Set in Map.Sim's cheat parse.</summary>
    private bool _goodsCarCheat;

    /// <summary>#801 QA · <c>?parkback=1</c> — booted on the gravel facing the BACK-OF-HOUSE doors. Set in
    /// Map.Sim's cheat parse.</summary>
    private bool _parkBackCheat;

    /// <summary>#801 QA · Stand the captain at the GOODS CAR, at the blind end of the main corridor.
    ///
    /// <para>On the car's own published doorstep (<see cref="UndergroundComplex.Shaft.Landing"/>), never on
    /// a coordinate measured off a screenshot — the two alcoves hang off opposite faces of the spine and a
    /// number typed here would be right for one of them and inside a wall for the other.</para></summary>
    private void StandAtTheGoodsCarIfAsked(SurfaceExcursion ex)
    {
        if (!_goodsCarCheat || ex.Floor >= 0)
        {
            return;
        }

        foreach (UndergroundComplex.Shaft car in
            UndergroundComplex.ShaftsOn(MoonSurface.ExpeditionField()))
        {
            if (car.Kind != UndergroundComplex.ShaftKind.Service)
            {
                continue;
            }
            (double cx, double cy) = car.Landing;
            StandCaptainAt(cx, cy, "you walk the corridor to its blind end and the other car is standing there");
            ShowPulseMessage(
                "🧪 DEV ?goodscar=1: THE SECOND CAR. Press E: it runs these four floors, it does not climb "
                + "out, and the cage is at the other end of the corridor.");
            return;
        }
    }

    /// <summary>#801 QA · Stand the captain on the gravel in front of the park's FAR wall, facing the
    /// back-of-house doors — the far side the owner said should not be the edge.</summary>
    private void StandBehindTheParkIfAsked(SurfaceExcursion ex)
    {
        if (!_parkBackCheat || ex.Floor >= 0)
        {
            return;
        }

        if (UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Park
            is not { } green || green.Rooms.Count == 0)
        {
            return;
        }

        // In front of the middle door, on the park's own side of it — the walk is a good ten du back from
        // this wall, so this is the promenade the masts stand on and not a bed.
        UndergroundComplex.BackRoom middle = green.Rooms[green.Rooms.Count / 2];
        double doorX = (middle.Door.X1 + middle.Door.X2) / 2.0;
        double inward = green.Contains(doorX, middle.Door.Y1 + 4.0) ? 4.0 : -4.0;
        StandCaptainAt(doorX, middle.Door.Y1 + inward,
            "you cross the gravel to the far wall, and it is a row of doors");
        ShowPulseMessage(
            "🧪 DEV ?parkback=1: THE FAR SIDE OF THE GREEN. Doors in the wall that used to be the horizon — "
            + "walk through one.");
    }

    // #649: /map?watchers=1 opens the monolith's attentive window and shortens the dwell to a couple of
    // seconds. It does NOT change what happens — the beat, the variant roll and the (zero) cost are the
    // ones a captain gets — because a cheat that shows you a different scene is worse than no cheat at all.
    private bool _watchersCheat;
}

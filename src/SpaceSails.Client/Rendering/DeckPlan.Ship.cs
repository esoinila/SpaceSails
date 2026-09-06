using SpaceSails.Core;

namespace SpaceSails.Client.Rendering;

// Subject: HER OWN DECK — the pirate ship, the one plan this class ships hardcoded (part of DeckPlan).
//
// Everything else that walks on a DeckPlan is generated: a haven interior, a landing site, a wreck, a
// floor of the Hive. The ship is the exception — she is written down, room by room, in one builder, and
// she is the plan every other plan is compared against.
//
// #251 · MOVED HERE BY PURE MOTION out of `DeckPlan.cs` — see the note at the head of that file for why
// it was cut and what "pure motion" is holding across the family. Not one line below was edited: the
// builder's rooms are in the order they were laid down in, because a deck built in a different order is
// a different deck and every FrameHash in the suite would say so.

public sealed partial class DeckPlan
{
    // =====================================================================================
    //  The pirate ship — the default plan. Roughly 54×20 du. All doors are ≥ 3.5 du wide
    //  (the M12 plan's were too tight).
    // =====================================================================================

    /// <summary>The player's own ship. Reference-compared by the renderers to draw ship-only
    /// dressing (cargo crates, the reactor, the shuttle cradle, cantina tables).</summary>
    /// <summary>Her deck with every hatch standing open — the base ship. Backdrops, droids, tables and the
    /// location lookup all read this; only the live plan the captain walks needs door state.</summary>
    public static DeckPlan Ship { get; } = BuildShip(null);

    /// <summary>Her deck with these hatches dogged. Rebuilt on every door change, because the walls are
    /// built and not inferred.</summary>
    public static DeckPlan ShipWith(IReadOnlyCollection<string> shutRooms) => BuildShip(shutRooms);

    /// <summary>
    /// HER DECK, WITH THE DOORS IN WHATEVER STATE THEY ARE IN. Owner: <i>"we don't even have the doors in
    /// our own ship :-D"</i> — she had gaps in walls, and a gap cannot be shut.
    ///
    /// <para>Built rather than inferred, the same rule the wreck learned the hard way: a dogged hatch is a
    /// WALL, and the walls are what everything else asks. Skip the rebuild and you get a door the player can
    /// see closed that stops neither them, nor a round, nor anything that walks.</para>
    /// </summary>
    /// <param name="shutRooms">Compartments whose hatches are dogged. Null on the base ship.</param>
    private static DeckPlan BuildShip(IReadOnlyCollection<string>? shutRooms)
    {
        List<Wall> walls =
        [
            // --- Hull (bow point at x=30) ---
            new(30, 0, 20, 10, IsWindow: true, IsHull: true),    // bow-port slant: bridge glass
            new(30, 0, 20, -10, IsWindow: true, IsHull: true),   // bow-starboard slant: bridge glass
            new(20, 10, 17, 10, false, true),
            new(17, 10, 6, 10, IsWindow: true, IsHull: true),    // the cantina's panoramic window
            new(-1, 10, -18, 10, false, true),

            // --- Airlock vestibule (port bump-out, x -1..6, y 10..14) ---
            // The airlock moved off the galley (owner, Expanse consult): it sits at the port end of a
            // WIDE airlock corridor (the 7-du slot between shuttle bay and cantina), in a bumped-out
            // vestibule — a defensible kill-box. A 3-du hatch in the port wall; two blast walls flank
            // it for cover, so the crew can repel boarders from behind hard cover on both sides.
            new(-1, 10, -1, 14, false, true),   // vestibule port-side hull
            new(6, 10, 6, 14, false, true),     // vestibule starboard-side hull
            new(-1, 14, 1, 14, false, true),    // outer wall, port of the hatch
            new(6, 14, 4, 14, false, true),     // outer wall, starboard of the hatch
            new(1, 14, 4, 14, false, true),     // the hatch itself — sealed on the bare ship; the docked complex opens it and mates the tube
            new(1, 14, 1, 11, false, false),    // cover: port blast wall
            new(4, 14, 4, 11, false, false),    // cover: starboard blast wall
            new(-18, 10, -24, 7, false, true),
            new(-24, 7, -24, -7, false, true),
            new(-24, -7, -18, -10, false, true),
            // The bottom hull, split around the SHUTTLE-BAY HATCH (#295: the bay moved to the bottom
            // edge — the wild side). Sealed on the bare ship, exactly like the top airlock hatch; a
            // surface excursion opens it and mates the down-tube (see MoonSurface).
            new(-18, -10, ShuttleHatchX1, -10, false, true),
            new(ShuttleHatchX2, -10, 20, -10, false, true),
            new(ShuttleHatchX1, -10, ShuttleHatchX2, -10, false, true), // the hatch itself — sealed here

            // --- #537 · HER SHIELDING, same specs as the derelicts ---
            // Owner: "Our own ship should match these specs also. 👍😎" — and she should, for two reasons that
            // are both his. The fiction one: "the walls that can hold vacuum are not thin and all kinds of tech
            // needs to exist on the ship somewhere", which is as true of the ship you own as of the ones you rob.
            // And the mechanical one, which matters more: the anti-tell rule. If derelicts had shielded hulls and
            // ours did not, the thickness itself would be a salvage-only feature and the player would read it as
            // scenery rather than as ship.
            //
            // Drawn along her PARALLEL SIDES only — the straight runs, broken where her airlock vestibule bumps
            // out to port and where the shuttle hatch opens to starboard. Her bow is glass and her stern tapers;
            // neither carries a band, for the same reason the wreck's bow taper does not.
            new(-18, 10 + ShieldingDepth, -1, 10 + ShieldingDepth, false, true),
            new(-18, 10, -18, 10 + ShieldingDepth, false, true),
            new(-1, 10, -1, 10 + ShieldingDepth, false, true),
            new(6, 10 + ShieldingDepth, 20, 10 + ShieldingDepth, false, true),
            new(6, 10, 6, 10 + ShieldingDepth, false, true),
            new(20, 10, 20, 10 + ShieldingDepth, false, true),

            new(-18, -10 - ShieldingDepth, ShuttleHatchX1, -10 - ShieldingDepth, false, true),
            new(-18, -10, -18, -10 - ShieldingDepth, false, true),
            new(ShuttleHatchX1, -10, ShuttleHatchX1, -10 - ShieldingDepth, false, true),
            new(ShuttleHatchX2, -10 - ShieldingDepth, 20, -10 - ShieldingDepth, false, true),
            new(ShuttleHatchX2, -10, ShuttleHatchX2, -10 - ShieldingDepth, false, true),
            new(20, -10, 20, -10 - ShieldingDepth, false, true),

            // --- Bridge bulkhead (x=18), door on the centerline, 4 du wide ---
            new(18, 10, 18, 2, false, false),
            new(18, -2, 18, -10, false, false),

            // --- Cantina (port, x 6..18, y 3..10): corridor wall with a wide door ---
            // Its port wall pulled back to x=6 to open the wide airlock corridor slot (x -1..6).
            new(18, 3, 13, 3, false, false),
            new(9, 3, 6, 3, false, false),
            new(6, 10, 6, 3, false, false),

            // #1040 · HER COUNTER, AND IT IS A REAL WALL. Owner: "Our on ship bar can be upgraded to match
            // the other bars... the UI represents code long time ago." A haven bar's counter has been a wall
            // segment since #247 — "you belly up, you don't walk through it" — and this room, whose own
            // backdrop is a photograph of a counter with a row of stools down it, had no counter at all.
            //
            // The two points are Core's (ShipLayout.CantinaCounter) and are read by the three things that
            // must never disagree: this wall, the fill the pen draws over it, and the standoff the stool row
            // is measured at. A counter drawn where nothing collides is a bar you walk through.
            new(ShipLayout.CantinaCounter.X1, ShipLayout.CantinaCounter.Y1,
                ShipLayout.CantinaCounter.X2, ShipLayout.CantinaCounter.Y2, false, false),

            // --- Cabins + HEAD (starboard, x 4..18, y -10..-3): corridor wall with four doors.
            //     3D-reno Phase 3 split the old three-cabin block into three cabins + a space HEAD 🚽.
            //     Stern-to-bow: CABIN 3 [4,7.5], CABIN 2 [7.5,11], CABIN 1 [11,14.5], HEAD [14.5,18];
            //     each berth is 3.5 du with a 2.5 du door (jamb stubs are the ~0.5 du wall bits). ---
            new(18, -3, 17.5f, -3, false, false), new(15, -3, 14.5f, -3, false, false),   // HEAD door
            new(14.5f, -3, 14, -3, false, false), new(11.5f, -3, 11, -3, false, false),   // CABIN 1 door
            new(11, -3, 10.5f, -3, false, false), new(8, -3, 7.5f, -3, false, false),     // CABIN 2 door
            new(7.5f, -3, 7, -3, false, false), new(4.5f, -3, 4, -3, false, false),       // CABIN 3 door
            new(4, -3, 3, -3, false, false),          // stern corner stub
            new(4, -10, 4, -3, false, false),         // cabin-block stern wall
            // Berth dividers (full depth, corridor to hull)
            new(14.5f, -3, 14.5f, -10, false, false), // CABIN 1 / HEAD
            new(11, -3, 11, -10, false, false),       // CABIN 2 / CABIN 1
            new(7.5f, -3, 7.5f, -10, false, false),   // CABIN 3 / CABIN 2

            // --- Cargo hold (port, x -12..-1, y 3..10): corridor wall, wide bay door (#295: the hold
            //     and the shuttle bay swapped sides so the bay meets the bottom hull — geography intact,
            //     walls unchanged, only the room's identity + fixtures moved). ---
            // Its starboard wall pulled back to x=-1 to open the wide airlock corridor slot (x -1..6).
            new(-1, 3, -3, 3, false, false),
            new(-7, 3, -12, 3, false, false),
            new(-1, 10, -1, 3, false, false),
            new(-12, 10, -12, 3, false, false),

            // --- Shuttle bay (bottom-port, x -12..2, y -10..-3): the wild-side bay, its hatch on the
            //     bottom hull. K-77 and R-3B are stationed here; the down-tube grows from the hatch. ---
            new(2, -3, -3, -3, false, false),
            new(-7, -3, -12, -3, false, false),
            new(2, -10, 2, -3, false, false),
            new(-12, -10, -12, -3, false, false),

            // --- Engine bulkhead (x=-14), centerline door 4 du wide ---
            new(-14, 10, -14, 2, false, false),
            new(-14, -2, -14, -10, false, false),
        ];

        // A DOGGED HATCH IS A WALL. Every compartment's opening is filled in when its door is shut, from
        // Core's own door segment — the same list that draws it, so the player can never see a door closed
        // that does not stop them.
        if (shutRooms is { Count: > 0 })
        {
            foreach (ShipLayout.Room room in ShipLayout.Rooms)
            {
                if (!shutRooms.Contains(room.Name))
                {
                    continue;
                }
                (float dx1, float dy1, float dx2, float dy2) = ShipLayout.DoorSegment(room);
                walls.Add(new Wall(dx1, dy1, dx2, dy2, false, false));
            }
        }

        List<ConsoleSpot> consoles =
        [
            new(ConsoleKind.Helm, 24, 2.5f, "HELM"),
            new(ConsoleKind.NavPost, 24, -2.5f, "NAV POST"),
            new(ConsoleKind.Scope, 20, 7, "SCOPE"),
            // #1040 · THE GALLEY CARD'S OWN DESK, off the middle of the floor at last. It stood at (11, 7.5)
            // with a drawn table 1.5 du under it, which is the owner's "not at the middle of the empty
            // floor" complaint about the havens' barkeeps, unenforced in the one room he owns. Read from
            // Core, like the charge dump and the cabin desks, and for the reason written at those: two
            // numbers for one console is the exact shape of every console collision this ship has had.
            new(ConsoleKind.Cantina,
                (float)ShipLayout.CantinaGalleyStation.X, (float)ShipLayout.CantinaGalleyStation.Y,
                "CANTINA"),
            new(ConsoleKind.Cargo, -5, 6.5f, "CARGO"),        // #295: the hold is now the top-port room
            new(ConsoleKind.Shuttle, -10, -6.5f, "SHUTTLE BAY"), // #295: the bay is now bottom-port

            // The shuttle-bay airlock (#163; moved to the bottom hull hatch for #295): walk up and it
            // opens the "places in shuttle range" pop-up — the door you understand as a flight, and now
            // the hinge the surface excursion grows a down-tube from. Kept clear of the SHUTTLE BAY
            // console (−10, −6.5) so [E] doesn't grab the wrong one. Drawn as the amber airlock door.
            new(ConsoleKind.ShuttleAirlock, -6.5f, -8.7f, "🚀 SHUTTLE AIRLOCK"),
            // RENAMED, because it was never the atmosphere. This dumps the CAPACITOR — a ship-systems
            // action that happens to share a verb with the thing the whole weekend was about. Two
            // panels in one engine room both called VENT PANEL would be a trap of my own making.
            //
            // POSITION READ FROM CORE, not typed here. It was a literal in this list AND a constant in
            // ShipLayout at the same time — two numbers for one console, which is the exact shape of every
            // console collision this ship has had. One of them has to be the truth; a test can only walk
            // the one in Core.
            new(ConsoleKind.Vent, (float)ShipLayout.ChargeDumpStation.X, (float)ShipLayout.ChargeDumpStation.Y,
                "⚡ CHARGE DUMP"),

            // The ship's BUILDER'S PLATE — bolted to the engine-room bulkhead by the keel, where a
            // builder's plate belongs (owner's cruise ruling, 2026-07-19, photographing their ship's Aker
            // Finnyards plate: "We could gen-AI the ships and docks some space-dock plaques … add some
            // depth to the world"). Walk up and [E] pops the plate + her service history (Koski &
            // Daughters, Hull No. 77; the Victoria-I "she used to be something" beat). Kept clear of the
            // VENT PANEL (−20, −4.5) so E never grabs the wrong console. Text/art from Core Plaques.Ship.
            new(ConsoleKind.ViewObject,
                (float)ShipLayout.BuildersPlateStation.X, (float)ShipLayout.BuildersPlateStation.Y,
                Core.Interior.Plaques.Ship.ConsoleLabel,
                Core.Interior.Plaques.Ship.ArtUrl, Core.Interior.Plaques.Ship.Lore),
            new(ConsoleKind.Head, 16.25f, -6.5f, "HEAD 🚽"), // the space toilet (3D-reno Phase 3)

            // MED BAY (owner's Evening-wind ruling, 2026-07-18: "change one cabin into med bay where
            // calming pills can be retrieved to help restore sanity to captain"). CABIN 3 [x 4..7.5] is
            // reborn as the med bay; its MED KIT console sits mid-berth (mirrors the HEAD's y), and [E]
            // there takes one calming pill (see InteractAtConsole's MedKit case). The [E] hint is drawn
            // automatically when the captain is near, so the label stays clean.
            new(ConsoleKind.MedKit, 5.75f, -6.5f, "MED KIT 💊"),

            // BUNK 🛏 (owner's live ruling, 2026-07-19: "Let's have a sanity restoring sleep action in one of
            // the cabins" — the REST half of Evening-wind #21). CABIN 1 [x 11..14.5], the tidy berth, keeps
            // its bunk; its console sits mid-berth (mirrors the HEAD's and MED KIT's y), and [E] there turns
            // in for a night's sleep (see InteractAtConsole's Bunk case → Sleep). Free but honest — a short
            // WELL-RESTED satiety stops it being the steady-hands grind (CabinComforts owns that law).
            new(ConsoleKind.Bunk, 12.75f, -6.5f, "BUNK 🛏"),

            // The gangway to a docked haven (go-ashore, 2026-07-07; moved to the airlock vestibule
            // 2026-07-08). In the docked complex you walk the tube; on the bare ship, pressing E here
            // just teaches "clamp on first" (see InteractAtConsole's Airlock case).
            new(ConsoleKind.Airlock, (float)ShipLayout.PlacardStation.X, (float)ShipLayout.PlacardStation.Y,
                "⚓ GANGWAY"),

            // --- Bridge seats (PR-14, StationDesks.md #14): pressing E opens the matching desk
            // without leaving the ship's own deck plan — three free spots on the bridge (x > 18),
            // clear of the helm/nav-post/scope trio, each other, and (importantly) the avatar's own
            // spawn point (SpawnX/SpawnY below) by a comfortable margin — nobody should see an [E]
            // prompt before they've taken a single step.
            new(ConsoleKind.CommsSeat, 20, -7, "COMMS SEAT"),      // mirrors Scope (20, 7) to starboard
            new(ConsoleKind.TacticalSeat, 19.5f, 4, "TACTICAL SEAT"), // port side, near the bridge door
            new(ConsoleKind.TradeSeat, 27, 0, "TRADE SEAT"),       // the bow-tip nook between helm and nav post
        ];

        (float X, float Y, string Text)[] roomLabels =
        [
            (22, -7, "BRIDGE"),
            // #1040 · …and the room writes its name somewhere the furniture is not. It read (11, 5), which
            // is now half a du off the second stool in the row.
            ((float)ShipLayout.CantinaLabelStation.X, (float)ShipLayout.CantinaLabelStation.Y, "CANTINA"),
            (2.5f, 12f, "⚓ AIRLOCK"),
            (12.75f, -9f, "CABIN 1"), (9.25f, -9f, "CABIN 2"), (5.75f, -9f, "MED BAY"), // CABIN 3 → MED BAY (owner 2026-07-18)
            (-6, 8.5f, "CARGO HOLD"),
            (-6, -8.5f, "SHUTTLE BAY"),
            (-19, 5, "ENGINE ROOM"),
        ];

        // Room backdrops (3D-reno Phases 1 & 3): the cantina wears The Space Bar; each starboard
        // berth wears its own art so the crew reads as individuals; the HEAD wears a grimy toilet.
        Backdrop[] backdrops =
        [
            new("art/the-space-bar.jpg", 6, 10, 12, 7, 0.9f),   // CANTINA, zone x∈[6,18] y∈[3,10]
            new("art/cabin-tidy.jpg", 11, -3, 3.5f, 7, 0.9f),   // CABIN 1
            new("art/cabin-messy-a.jpg", 7.5f, -3, 3.5f, 7, 0.9f), // CABIN 2
            // CABIN 3 → MED BAY (owner 2026-07-18): the med-bay backdrop replaces the old messy-cabin art,
            // wired exactly as the cabin arts are (Grok-generated ship-med-bay.jpg, same zone/alpha).
            new("art/ship-med-bay.jpg", 4, -3, 3.5f, 7, 0.9f), // MED BAY (was CABIN 3)
            new("art/space-head.jpg", 14.5f, -3, 3.5f, 7, 0.9f), // HEAD 🚽
        ];

        // Cantina tables: three tops, FORWARD of the counter and under the panoramic window — which is
        // where a bar art puts its patron tables and where the one view aboard actually is. They read
        // (8, 7.5), (11, 6), (14, 7.5) — laid across the middle of the room with the galley console
        // standing on the middle one, which is why #1016 could only seat two of the three. Off Core now
        // (ShipLayout.CantinaTops), read off the cantina's own bounds. Seat count still 0: they draw the
        // ring they have always drawn, and #792's chairs belong to a room that has patrons.
        TableTop[] tables = [.. ShipLayout.CantinaTops.Select(p => new TableTop((float)p.X, (float)p.Y))];

        // The shuttle-bay airlock door (#163; #295 moved it to the bottom hull hatch): an amber
        // auto-door across the SHUTTLE-BAY HATCH on the bottom hull. On the bare ship it sits on the
        // sealed hatch (the hull stays closed, the raycaster never escapes) — walking through it is the
        // shuttle flight, resolved by the "places in shuttle range" pop-up. A surface excursion opens
        // the hatch and grows a down-tube through it (see MoonSurface).
        List<Door> doors =
        [
            new(ShuttleHatchX1, -10, ShuttleHatchX2, -10),
        ];

        // HER OWN HATCHES, drawn at last. One per compartment, from Core's door segment, so what is drawn
        // and what blocks are the same list.
        foreach (ShipLayout.Room room in ShipLayout.Rooms)
        {
            (float dx1, float dy1, float dx2, float dy2) = ShipLayout.DoorSegment(room);
            doors.Add(new Door(dx1, dy1, dx2, dy2));

            // And its control, standing in the CORRIDOR rather than the room — the captain shutting a door
            // is almost never the one who wants to be sealed in behind it.
            DeckReachability.Point at = ShipLayout.DoorConsolePoint(room);
            bool shut = shutRooms is not null && shutRooms.Contains(room.Name);
            consoles.Add(new ConsoleSpot(
                ConsoleKind.ShipDoor, (float)at.X, (float)at.Y,
                shut ? $"🔒 {room.Name}" : $"🔓 {room.Name}"));
        }

        // HER DAMAGE-CONTROL BOARD, aft with the machinery — the owner's own placement ("I like that the
        // vent is in engineering"), and the same room the wreck keeps hers in. The difference is that this
        // one has a live bridge repeater, which is most of what owning a ship means.
        consoles.Add(new ConsoleSpot(
            ConsoleKind.ShipValves,
            (float)ShipLayout.ValveStation.X, (float)ShipLayout.ValveStation.Y,
            "⚙ ATMOSPHERE VALVES"));
        consoles.Add(new ConsoleSpot(
            ConsoleKind.ShipValves,
            (float)ShipLayout.BridgeRepeaterStation.X, (float)ShipLayout.BridgeRepeaterStation.Y,
            "⚙ ATMOSPHERE (bridge repeater)"));

        // #1016/#1040 · DESK ✍ — the other half of a berth. Owner, on 7 Deck: "Why no table in cabin
        // either?", and on #1040: "CABIN 2 could take a desk like CABIN 1's." A bunk is where you stop being
        // awake; a desk is where you work, and a berth is the one kind of room aboard with a DOOR between it
        // and the corridor, which is what makes it the ship's cabinet rung (the case may be spread there
        // unconditionally).
        //
        // POSITIONS READ FROM CORE, exactly like the CHARGE DUMP above and for the reason written there: two
        // numbers for one console is the exact shape of every console collision this ship has had.
        // ShipLayout.CabinDeskStationIn derives each from ITS OWN berth's bounds and explains why the corner
        // is the right one — it is the placement that puts the CHAIR nearer the desk than the bunk, so [E]
        // from the seat never turns in for the night. Written as a loop over ShipLayout.DeskCabins so a
        // third berth is furnished, audited and seatable on one line.
        foreach (string cabin in ShipLayout.DeskCabins)
        {
            DeckReachability.Point at = ShipLayout.CabinDeskStationIn(cabin);
            consoles.Add(new ConsoleSpot(ConsoleKind.ShipDesk, (float)at.X, (float)at.Y, "DESK ✍"));
        }

        // HER SCUTTLING CHARGES, port side aft with the machinery. The derelicts have carried a scuttling
        // panel since #488; the owner's point is that a ship is a ship — and that this one is the last
        // argument a captain has when something is already aboard.
        consoles.Add(new ConsoleSpot(
            ConsoleKind.ShipScuttle,
            (float)ShipLayout.ScuttleStation.X, (float)ShipLayout.ScuttleStation.Y,
            "☢ SCUTTLING CHARGES"));

        // ── #1016 · A TOP THE CAPTAIN CAN TAKE, IN HIS OWN CANTINA ───────────────────────────────────
        //
        // Owner, on 7 Deck with the three tops drawn in front of him: "Why no table here to sit at?" — and
        // the ruling that names the lane, "I expect to have a bar table like this in this ships galley
        // also.... feature complete." They were dressing: drawn furniture with no console over them, so [E]
        // there answered nothing at all. That is an ABSENCE rather than a refusal, and it is the one kind
        // of no a player cannot read (#757's lesson in the Hive, #973 L0's in a station bar, and now here).
        //
        // OFF THE SAME LIST THE PEN DRAWS, never a retyped coordinate: `tables` above IS the furniture, so
        // a top that moves takes its seat with it. §13.15 — two numbers for one fixture is this ship's own
        // named console bug, and she has had four of them.
        //
        // AND NOT ON A TOP THAT WOULD SMEAR A LABEL. The room's own audit law decides which tops get a seat
        // — asked of the console list ITSELF, after everything else is in it, exactly as the haven bar asks
        // it — rather than a hand-picked set, so a fixture that moves tomorrow is re-judged tomorrow.
        //
        // #1040 · IT USED TO REFUSE ONE OF THE THREE, and that was honest rather than desirable: the galley
        // console stood at (11, 7.5) with the middle top 1.5 du under it, so that top's chair would have
        // been inside the galley desk's own prompt. This lane moved the console onto the forward window
        // corner (where a bar art puts its machines) and the tops under the window, so all three clear every
        // fixture in the room and all three are takeable. The LAW did not change; the room did.
        //
        // ── #1040 · AND A STOOL ROW AT THE COUNTER, WHICH IS ONE FIXTURE AND THEREFORE ONE CONSOLE ─────
        //
        // Owner: "Our on ship bar can be upgraded to match the other bars... the UI represents code long
        // time ago." The row is Core's (ShipLayout.CantinaStools, one body-width off the counter's own
        // face); what is added here is the PRESS, and it is a single console with a RUN down the counter —
        // #791's own idiom, from the owner's B1 complaint that "the Bar desk is really long now, but there
        // is only one spot to get service on it… we would need an E-bus of the bar desk length". Four
        // consoles a stool apart would fail the deck audit's own label law and read as four fixtures; a
        // counter is one fixture you walk up to anywhere along, and which stool you get is which stool you
        // were standing at (Map.ShipSeats).
        //
        // Added BEFORE the tops below, so the label law judges them against it rather than the other way
        // round — the order in this method is the order the room was built in, and a console that arrives
        // after a judgement is a console nobody judged.
        (float cx, float cy0, float _, float cy1) = ShipLayout.CantinaCounter;
        var stoolRow = new List<StoolSpot>(ShipLayout.CantinaStoolCount);
        foreach (DeckReachability.Point stool in ShipLayout.CantinaStools)
        {
            // Never taken and the row never has anybody on it, and that is the honest answer rather than a
            // default nobody thought about: her crew is three droids on a fixed patrol and none of them
            // drinks. The captain's own body is drawn by the figure pass, not by this list.
            stoolRow.Add(new StoolSpot((float)stool.X, (float)stool.Y));
        }

        consoles.Add(new ConsoleSpot(
            ConsoleKind.ShipStool,
            (float)ShipLayout.CantinaCounterService.X, (float)ShipLayout.CantinaCounterService.Y,
            SpaceSails.Core.SittingAlone.FreeStoolPlate,
            Run: (cx, cy0, cx, cy1)));

        foreach (TableTop top in tables)
        {
            if (ALabelFitsAt(consoles, top.X, top.Y))
            {
                consoles.Add(new ConsoleSpot(
                    ConsoleKind.BarTop, top.X, top.Y, SpaceSails.Core.SittingAlone.FreeTablePlate));
            }
        }

        // ── #1040 · AND THE ROOM IS DRAWN AS FURNITURE RATHER THAN AS EMPTY FLOOR ───────────────────────
        //
        // #868's primitive, first used on a Hive back-office and never on the boat the player owns. Owner,
        // reading a room off the plan: "The graphics kind of does not show there being a table" — and his
        // own fix, "could the table just be a different color rectangle". A counter drawn as a single line
        // reads as a space you could stand in, which is exactly what it is not.
        //
        // Two pieces, in the two tones this deck has meant those things with since #868: the counter's own
        // top is a surface you work at, and the back-bar behind it is something you keep things in.
        (float bx0, float by0, float bx1, float by1) = ShipLayout.CantinaBackBar;
        (float tx0, float ty0, float tx1, float ty1) = ShipLayout.CantinaCounterTop;
        FurnitureSpot[] shipFurniture =
        [
            new(tx0, ty0, tx1, ty1, 0),
            new(bx0, by0, bx1, by1, 1),
        ];

        // #537 · AND HER OWN SHIELDING IS FILLED TOO. Owner: "we should cover those narrow spaces … all of
        // them." All of them means hers as well — and on the ship it matters for a second reason: she is the
        // deck a player reads most, so a narrow black strip down her sides teaches them what a hiding place
        // looks like before they ever board a wreck.
        Structure[] shipStructures =
        [
            new(-18, 10, -1, 10 + ShieldingDepth),
            new(6, 10, 20, 10 + ShieldingDepth),
            new(-18, -10 - ShieldingDepth, ShuttleHatchX1, -10),
            new(ShuttleHatchX2, -10 - ShieldingDepth, 20, -10),
        ];

        return new DeckPlan([.. walls], [.. consoles], roomLabels, backdrops,
            spawnX: 21, spawnY: 0, // on the bridge, facing the bow glass
            droidCount: 3, fillDroids: FillShipDroids, location: ShipLocation,
            doors: [.. doors], shipFixtures: true, tables: tables,
            structures: shipStructures,
            // #1040 · her counter's seats and her counter's own fill, so the room a captain sees and the
            // room he presses [E] in are one room.
            stools: [.. stoolRow], furniture: shipFurniture);
    }

    // --- Droid pirate infantry 🤖🏴‍☠️ ---
    // Positions are pure functions of sim time: no state, deterministic, free.
    private static void FillShipDroids(double simTime, Droid[] droids)
    {
        // Two boarding troopers at parade rest in the shuttle bay (now bottom-port, #295), idle-swaying.
        double sway = 0.08 * Math.Sin(simTime * 0.0011);
        droids[0] = new Droid(-4.5 + sway, -7.5, Math.PI / 2, "K-77");
        droids[1] = new Droid(-2.5 - sway, -7.5, Math.PI / 2, "R-3B");

        // One patrolling the corridor, bow to stern and back (triangle wave, ~40 s loop).
        double phase = simTime % 40000 / 40000.0;
        double tri = phase < 0.5 ? phase * 2 : 2 - phase * 2;
        double x = -10 + tri * 26; // corridor x from -10 to 16
        droids[2] = new Droid(x, 0.6, phase < 0.5 ? 0 : Math.PI, "V-1K");
    }

    private static string ShipLocation(double x, double y)
    {
        if (x > 18) return "BRIDGE";
        if (x < -14) return "ENGINE ROOM";
        if (x is > -1 and < 6 && y > 3) return y > 10 ? "AIRLOCK" : "AIRLOCK CORRIDOR";
        if (x > 6 && y > 3) return "CANTINA";
        if (x > 4 && y < -3) return x > 14.5 ? "HEAD" : x < 7.5 ? "MED BAY" : "CABINS"; // CABIN 3 → MED BAY (owner 2026-07-18)
        if (x > 4) return "CORRIDOR";
        if (x > -12) return y > 3 ? "CARGO HOLD" : y < -3 ? "SHUTTLE BAY" : "CORRIDOR";
        return "CORRIDOR";
    }
}

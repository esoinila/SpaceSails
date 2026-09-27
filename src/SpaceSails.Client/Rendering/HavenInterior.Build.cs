using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>Part of <see cref="HavenInterior"/> (the header note lives in HavenInterior.cs) — THE WELD.
/// Every station shares this one geometry: the ship, the umbilical, the twelve-sided immigration hall
/// with ten other berths' hatches sealed, the wide door, and the bar — assembled into a single
/// <see cref="DeckPlan"/> in one coordinate space, with whatever wings are active welded on at runtime.
/// <c>FillComplexDroids</c> closes it with the eleven figures the docked complex draws before any walker,
/// each placed off the same constants and the same rota the rest of this class answers with. Two methods
/// and no fields at all, which is why this is the half that could be moved.</summary>
public static partial class HavenInterior
{
    private static DeckPlan BuildComplex(StationSpec spec, IReadOnlyList<DeckWing> activeWings, double simTime,
        bool forceOracle = false, System.Action<DeckPlan.Droid[], int>? fillWalkers = null,
        RoomChurn? churn = null, ArrivalTube.Tier? tier = null)
    {
        DeckPlan ship = DeckPlan.Ship;
        bool backRoomOpen = activeWings.Count > 0; // the Magpie's back-room stop is reachable once a wing is welded on

        // Hatch ids whose edge has grown a wing — carve a doorway there instead of a sealed wall.
        var openHatchIds = new HashSet<string>(activeWings.Select(w => w.UnlockHatchId));

        // The bare ship seals its airlock hatch (x 1..4); the complex opens it and mates the tube.
        var hatch = new DeckPlan.Wall(1, ShipHatchY, 4, ShipHatchY, false, true);

        var walls = new List<DeckPlan.Wall>(ship.Walls.Where(w => !w.Equals(hatch)));
        // Seed from the ship's own doors so the shuttle-bay airlock (#163) travels with the ship into
        // every docked complex — that is the captain's ride home, so the return hop is never stranded.
        var doors = new List<DeckPlan.Door>(ship.Doors);
        var labels = new List<(float X, float Y, string Text)>(ship.RoomLabels);

        WeldTheTube(walls, doors);

        var hatches = new List<DeckPlan.ConsoleSpot>();
        CutTheRing(spec, openHatchIds, walls, doors, hatches);

        LayTheImmigrationDesk(spec, walls, labels);

        LayTheBar(spec, walls, labels);

        LayTheObservationWalk(spec, walls, labels);

        // #247 — the bar counter, and the BARKEEP behind it. Owner ashore at the Rusty Roadstead: "How
        // do I get a drink at the Rusty bar here? Did we forget to add the bar-keep :-D". The counter is
        // a real wall (you belly up, you don't walk through it); the barkeep console sits on the players'
        // side of it, so E leans in for the house special. The keep's name + drink come from Core.
        //
        // 2026-07-18 ("Evening wind" plan) — the per-image correction. The first pass shared ONE counter
        // for all four bars and pinned it three du off the far wall (BarTopY − 3), which dropped the keep
        // and the pacing droid up in the window/ceiling band of every backdrop. The owner ruled per-image:
        // "the bar-keep service position … needs to be AT that desk … not the middle of the empty floor …
        // Not on top of a window — and the bar to be on top of the bar in the picture." So each bar now
        // reads its desk off its OWN art (Core BarDesks), and the counter is placed there — down the LEFT,
        // mid-depth, where every backdrop actually draws it. The service point (S) is the [E] spot on the
        // players' side; the counter wall sits just BEHIND it (toward the window) and the droid paces
        // behind that (see FillComplexDroids). A safe fallback keeps any unlisted bar sane.
        BarDesk desk = BarDesks.For(spec.BodyId) ?? DefaultBarDesk(spec.BodyId);
        float serviceX = desk.ServiceX;
        float serviceY = HallTopY + desk.ServiceYOffset;   // mid-depth on the desk, clear of the window
        float counterY = serviceY + 1f;                     // the counter wall, one du behind the service line
        walls.Add(new(serviceX - desk.CounterHalfWidth, counterY, serviceX + desk.CounterHalfWidth, counterY, false, false)); // waist-high bar counter, on the pictured desk
        Barkeep? keep = Barkeeps.For(spec.BodyId);
        string keepLabel = keep is { } bk ? $"🍺 BARKEEP · {bk.Name}" : "🍺 BARKEEP";

        HangTheBackRoomLeaves(spec, doors, hatches);

        // The bar's regulars (issue #410): no longer four names nailed to four fixed chairs in every bar.
        // The rota (ResolveRegulars → PatronRota) decides, for THIS station and THIS docking watch, which
        // of the four are drinking here and which chair each took — so a present regular gets a BarPatron
        // console at their seeded seat, and an absent one leaves an empty chair (no console: E finds
        // nothing, they've drifted off — opportunity/dread, not a bug). Contacts stay keyed by the ◈ label
        // id, never by seat, so the drink/rumor/pick systems work whichever chair fills. Drop the ship's ⚓.
        // …and #731's churn over the top of it: a regular who stood up and walked out of the cellar door has
        // no console at his chair any more, and one who came out of it and sat down has one at his. Asked
        // once, here, so the consoles, the droids and the barkeep's line cannot come to three views.
        IReadOnlyList<SeatedRegular> regulars = ResolveRegulars(spec.BodyId, simTime, churn);
        var consoles = new List<DeckPlan.ConsoleSpot>(ship.Consoles.Where(c => c.Kind != DeckPlan.ConsoleKind.Airlock));
        foreach (SeatedRegular r in regulars)
        {
            if (r.Present)
            {
                consoles.Add(new(DeckPlan.ConsoleKind.BarPatron, (float)r.X, (float)r.Y, r.Label));
            }
        }

        // The station oracle (issue #425), if she's tuned to this bar this watch. A BarPatron console in
        // the port-back corner; the client's E-router matches her by name (OracleRant.Nickname) and hands
        // off to the oracle flow, never the generic quest-giver path. Absent watches leave the stool empty.
        bool oracleHere = OraclePresent(spec.BodyId, simTime, forceOracle);
        if (oracleHere)
        {
            consoles.Add(new(DeckPlan.ConsoleKind.BarPatron, OracleCorner.X, OracleCorner.Y,
                SpaceSails.Core.OracleRant.ConsoleLabel));
        }
        SetTheBarsOwnFixtures(spec, consoles, serviceX, serviceY, keepLabel);

        consoles.AddRange(hatches); // the ring departments + bar back-rooms, as knockable locked hatches

        HangTheConcourse(spec, tier, consoles, labels);

        var tables = SetTheTables(spec, ship);

        var backdrops = HangTheBackdrops(spec, ship);

        WeldTheWings(activeWings, walls, doors, consoles, labels);

        OfferTheFreeTops(consoles);

        var furniture = FitTheGallery(spec, ship, consoles);

        return new DeckPlan(walls.ToArray(), consoles.ToArray(), labels.ToArray(), backdrops.ToArray(),
            spawnX: 2.5, spawnY: 6, // aboard, in the airlock corridor, facing up the tube
            // #973 L0 · …and the WALKER BAND after the room's own seated figures, when somebody is walking this
            // deck. The offset is stated once (SeatedFigureCount) and the width once (Egress.BandSlots); the
            // two times this game threw IndexOutOfRangeException at the renderer, it was because a band's
            // width and a buffer's length were two opinions about one number.
            droidCount: SeatedFigureCount + (fillWalkers is null ? 0 : Egress.BandSlots),
            fillDroids: (simTime, buffer) =>
            {
                FillComplexDroids(simTime, buffer, backRoomOpen, serviceX, serviceY, regulars, oracleHere);
                fillWalkers?.Invoke(buffer, SeatedFigureCount);
            },
            // #1199 · …and the walk answers FIRST, because it is the only room on this deck that reaches
            // outside the hall ring on a line the immigration half-plane below would otherwise claim. A haven
            // deck has no room objects — this lambda IS what a haven means by "which room am I in" — so the
            // walk being a real, named place before and after the beat is exactly this clause existing.
            location: (x, y) => InTheObservationWalk(spec.BodyId, x, y) ? ObservationWalk.Plate
                              : x < -14.5 && y is > 15 and < 37 ? "BONDED STORES · BACK ROOM"
                              : y > HallTopY ? spec.BarName
                              : y > HallBottomY ? $"{spec.Authority} IMMIGRATION"
                              : y > ShipHatchY ? "GANGWAY"
                              : DeckPlan.Ship.Location(x, y),
            doors: doors.ToArray(), shipFixtures: true, followCam: true, tables: tables.ToArray(),
            // #1040 · …AND THE SHIP'S OWN COUNTER TRAVELS WITH HER. A docked complex is her plan with a
            // station welded onto it, and her walls, doors, consoles, labels, backdrops and tops are all
            // seeded from it above. Her stool row and her counter's fill were the two she would have arrived
            // without — so the moment she clamped on, the seats [E] still answers at would have stopped
            // being drawn: the walked room and the drawn room disagreeing, which is this repository's third
            // named bug class with a bar stool under it.
            stools: ship.Stools, furniture: furniture.ToArray());
    }

    // Ship's three droids, the immigration officer, the four seated bar regulars (issue #410, roved by the
    // rota — each at their seeded seat this watch, or parked off-frame when they've drifted off), and —
    // index 8 — the roaming Magpie, placed by their sim-time rota. Shared across every station (one
    // geometry); deterministic in sim time, stateless. The <paramref name="regulars"/> seating is captured
    // at build time (fixed for the visit), so the droids sit exactly where their consoles do; only the
    // thermal jitter and the Magpie/barkeep pace read the live clock.
    private static void FillComplexDroids(double simTime, DeckPlan.Droid[] buffer, bool backRoomOpen,
        double barkeepX, double barkeepServiceY, IReadOnlyList<SeatedRegular> regulars, bool oracleHere)
    {
        DeckPlan.Ship.FillDroids(simTime, buffer); // fills [0..3)
        double sway = 0.05 * System.Math.Sin(simTime * 0.0009);
        buffer[3] = new DeckPlan.Droid(CustomsDesk.X, CustomsDesk.Y, -System.Math.PI / 2, "Customs"); // officer beside the gate

        // The four regulars sit at [4..8). A present one gets a tiny seeded thermal shuffle around their
        // seated anchor + a look-around facing twitch (ReeverIdle, #390) so they read alive, not carved;
        // an away one is parked far off-frame (their chair is simply empty this watch). Roster order is
        // stable, so index 4+i is the i-th regular whether or not they're here.
        for (int i = 0; i < 4; i++)
        {
            int slot = 4 + i;
            if (i < regulars.Count && regulars[i].Present)
            {
                SeatedRegular r = regulars[i];
                (double jx, double jy) = SpaceSails.Core.ReeverIdle.JitterAt(r.Seed, simTime);
                double face = r.Facing + SpaceSails.Core.ReeverIdle.FacingTwitchAt(r.Seed, simTime);
                buffer[slot] = new DeckPlan.Droid(r.X + jx, r.Y + jy, face, r.ShortName);
            }
            else
            {
                buffer[slot] = new DeckPlan.Droid(-9999, -9999, 0, i < regulars.Count ? regulars[i].ShortName : "Regular");
            }
        }

        NpcPost m = ResolveMagpie(simTime, backRoomOpen);
        buffer[8] = m.Present
            ? new DeckPlan.Droid(m.X + sway, m.Y, m.FacingRad, "Magpie")
            : new DeckPlan.Droid(-9999, -9999, 0, "Magpie"); // out of reach this watch — off-frame

        // #247 — the barkeep, pacing their patch BEHIND the counter (owner: "a barkeep pacing their bar
        // area is fine"; and 2026-07-18, "Evening wind": "in all bars that have a bar-desk in their
        // graphics the barkeep is positioned behind the bar desk"). No rota (they don't leave the bar): a
        // deterministic sine sweep, the same idiom as the seated regulars' sway. Centred on THIS bar's
        // service point (BarDesks), one du further back than the counter wall — so the keep works the far
        // side of the desk drawn in the art, never the window band the first pass parked them in. Facing
        // south (−π/2), across the bar toward the captain.
        double pace = 1.5 * System.Math.Sin(simTime * 0.00035);
        buffer[9] = new DeckPlan.Droid(barkeepX + pace, barkeepServiceY + 2, -System.Math.PI / 2, "Barkeep");

        // #425 — the station oracle, hunched over her corner drink when the rota has her here this watch.
        // A seeded thermal shuffle + facing twitch (ReeverIdle) so she reads alive, muttering at the wall;
        // parked far off-frame on the watches she's drifted off (her stool simply empty, no console). Index
        // 10, the buffer's last complex slot (droidCount 11).
        if (oracleHere)
        {
            ulong oseed = RegularSeed("STATION-ORACLE", PatronRota.WatchIndex(simTime));
            (double ojx, double ojy) = SpaceSails.Core.ReeverIdle.JitterAt(oseed, simTime);
            double oface = -System.Math.PI / 2 + SpaceSails.Core.ReeverIdle.FacingTwitchAt(oseed, simTime);
            buffer[10] = new DeckPlan.Droid(OracleCorner.X + ojx, OracleCorner.Y + ojy, oface, "Oracle");
        }
        else
        {
            buffer[10] = new DeckPlan.Droid(-9999, -9999, 0, "Oracle");
        }
    }
}

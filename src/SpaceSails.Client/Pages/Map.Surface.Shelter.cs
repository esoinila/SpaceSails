using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the beacons, the ruins, the emergency locker and the shelter rack.
public partial class Map
{
    // ── #573 · THE SHELTER'S CHARGING RACK [E]. The only place outside her tube that refills a suit, and
    //    therefore the only reason the deep field is worth crossing rather than merely looking at. ──
    /// <summary>#573 · The fixed places the fan should point at: the way home, and every shelter. Bearings
    /// and ranges from the captain, so the tracker answers "which way" for somewhere that does not move.
    ///
    /// <para><b>What the three flags mean, because one of them is not named for what it draws.</b>
    /// <c>IsHome</c> is the way back — the ship's tube, or the lift cars underground. <c>IsDead</c> is a
    /// place that is on the plan and is not answering. <c>IsLab</c> is neither: it is the ring in the
    /// IMPORTED VIOLET a door itself wears (#592) and it means <i>a way in that somebody made</i> — the lift
    /// head when the hidden door is known (#585), and, since #584, the mouth of any ground that has JOINED
    /// THE PLAN this excursion. Both are the same claim and the same ink, which is why they share the flag
    /// rather than growing a fourth colour nobody could tell from the other three.</para></summary>
    private List<(double Bearing, double Range, bool IsHome, bool IsLab, bool IsDead)> BuildBeacons(
        SurfaceExcursion ex)
    {
        var list = new List<(double, double, bool, bool, bool)>();
        if (Derelict.TryParseWreckId(ex.Stop.Body.Id, out _) || StationAboard.TryParseStationId(ex.Stop.Body.Id, out _))
        {
            return list;   // a hull has neither a tube mouth nor a shelter
        }

        void Add(double x, double y, bool home, bool lab = false, bool dead = false)
        {
            double dx = x - _avatarX, dy = y - _avatarY;
            list.Add((Math.Atan2(dy, dx), Math.Sqrt((dx * dx) + (dy * dy)), home, lab, dead));
        }

        // ── #584 · AND THE GROUND THAT JUST GREW, FOR THE REST OF THE EXCURSION ────────────────────────
        //
        // Owner, after forcing a door: "I was left totally un-aware about what that did and where?"
        //
        // The card names the place once. This is what keeps answering after it is dismissed, and it is the
        // half that makes the notification ACTIONABLE — a chamber is appended at a seeded spot that is
        // routinely off the current view, so a captain who read the card, closed it and turned round had
        // nothing left to walk toward. The instrument they are already watching has it now.
        //
        // ONLY THIS FLOOR'S. A room forced on B2 is not a place on B3, and a beacon that ignored the floor
        // would be the map lying (#573) in the register this fan has already been burned by twice — #591's
        // surface huts painted underground, #608's shelters painted on a dead floor.
        //
        // It is called from both branches below rather than once at the end, because the underground branch
        // returns early and a captain who forces a door down there is the captain who most needs the ring.
        void AddNewGround()
        {
            foreach ((double gx, double gy, int floor) in ex.NewGround)
            {
                if (floor == ex.Floor)
                {
                    Add(gx, gy, home: false, lab: true);
                }
            }
        }

        // ── #591 · UNDERGROUND, THE BEACONS ARE DIFFERENT PLACES ──
        //
        // Owner, on B1: "now that we are underground the elevator would be nice to be on the motion detector,
        // the surface hut's are really not that relevant down here".
        //
        // He is right, and it is the same fault as the reach: these are SURFACE beacons. The tube mouth and
        // the shelters are up a lift shaft and several hundred metres of rock away, so painting them on the
        // fan down here is not merely useless — it is actively wrong, because the way home ring is the one
        // the captain reads when the air gets short and it would be pointing at a hut they cannot reach.
        //
        // Down here the places worth a ring are the CARS, and they are the same ones every floor. They are
        // the way home in the only sense that matters underground, so they take the HOME flag and the calm
        // colour that goes with it — a place, not a thing that moves.
        //
        // #801 · Plural, and off the one list. This said "there is exactly ONE place worth a ring" and drew
        // it from ShaftAt; a captain being hunted toward the goods car would have had the instrument telling
        // them the way home was a hundred and seventy du behind them, which is the map lying (#573) in the
        // one place it costs a life.
        if (ex.Floor < 0)
        {
            // ── #719 slice 2 · …AND A STOPPED CAR IS NOT ONE OF THEM ──
            //
            // The break takes the cars off the fan altogether. Two readings were available and only one of
            // them is honest. Painting them NOT-home would put them in the refuge's ink, and that ring means
            // AIR YOU CAN REACH (#608) — a car has none. Painting them home would have the instrument
            // offering a way out that will not come, which is the map lying (#573) in the one place it costs
            // a life. So the fan is silent about them, exactly as it is silent about every other bit of
            // fabric down here, and the panel's own plate is where a captain learns why.
            //
            // What that leaves is the stair, alone and FIRST, which is how "the HOME ring moves from the cage
            // to the stair door" is said in this loop's own vocabulary: everything that asks the fan for the
            // way home takes the first home ring it finds.
            if (!TheCarIsStopped)
            {
                foreach (UndergroundComplex.Shaft car in
                    UndergroundComplex.ShaftsOn(MoonSurface.ExpeditionField()))
                {
                    (double carX, double carY) = car.Landing;
                    Add(carX,
                        carY + ((car.Kind == UndergroundComplex.ShaftKind.Cage ? 1 : -1)
                            * (UndergroundComplex.CorridorHalf + 1.5)),
                        home: true);
                }
            }

            // ── #719 · AND THE STAIR, WHICH IS A WAY OFF THIS FLOOR AND SO PAINTS LIKE ONE ──
            //
            // It goes on the fan for the reason the cars do, in this loop's own words one screen up: down
            // here the places worth a ring are the ways out, "the way home in the only sense that matters
            // underground". The stair is one — the only one besides the cage that actually climbs out — and
            // a captain who has to hunt for the second way out in the dark has not got one.
            //
            // IT IS NOT PAINTED IN THE REFUGE'S INK, and that is the decision. The not-home ring means AIR
            // YOU CAN REACH on this floor (#608), and a stairwell has none: two rings in one colour meaning
            // two different promises is the map lying (#573) in exactly the way the refuge ring was built
            // not to.
            //
            // HOME STAYS THE CAGE while the car runs, because the cage is FIRST in this list and stays
            // first — ShaftsOn puts it there and everything that asks the fan for "the way home" takes the
            // first home ring it finds. The stair is appended after the cars, so on an ordinary afternoon
            // nothing that has ever meant the cage by HOME now means the stair.
            //
            // #719 slice 2 · …and the day the car stops, the cars are not added at all, so the stair is the
            // first home ring and therefore IS home. That is the owner's "the HOME ring moves from the cage
            // to the stair door", achieved by the list being shorter rather than by a second rule about
            // which ring counts.
            //
            // #719 slice 2 · …and the spot is Core's own now (StairRingAt), because the tank measures to it
            // as well the moment the car stops. One journey, one function: the ring that says WHICH WAY and
            // the readout that says WHAT IT COSTS cannot come to two answers about where the door is.
            if (UndergroundComplex.HasStairOn(ex.Stop.Body.Id, ex.Floor)
                && UndergroundComplex.StairRingAt(MoonSurface.ExpeditionField()) is { } stair)
            {
                Add(stair.X, stair.Y, home: true);
            }

            // ── #608 · AND THE REFUGES, WHICH ARE THIS FLOOR'S SHELTERS ──
            //
            // Owner, exactly: "and those need to show in the motion detector, not the surface ones, when you
            // are 150 meters below surface."
            //
            // That sentence is the whole rule and both halves of it are load-bearing. The surface shelters
            // are up a shaft and several hundred metres of rock away, so painting them here would be the map
            // lying (#573) in its most expensive form — a ring on the instrument a captain would spend the
            // last of a tank walking toward. They are already gone, and they stay gone: this branch never
            // touches SheltersOn.
            //
            // What replaces them is the thing the ring MEANS. On the regolith a not-home ring is "air you
            // can reach that is not the ship"; on a dead floor that is the refuge, and it deserves the same
            // colour because it is the same promise. It also answers #608's hardest requirement — "a refuge
            // you discover AFTER you needed it is a cruelty" — without a map, a paper or a tutorial: the
            // instrument the captain already watches simply has it on it.
            //
            // Nothing is painted on a floor that holds pressure, because there is nothing to point at: the
            // whole floor is the refuge, and a ring saying "air, 40 du that way" while you are standing in
            // air is an instrument disagreeing with the room.
            //
            // #608 · AND A DEAD ONE STILL PAINTS, IN A DEAD RING. Owner, in the same comment: "A refuge
            // whose seal has failed must still paint, and must read as failed. Walking to one and finding it
            // dead is a real beat; walking to one that was never marked is just a bad map." Both halves are
            // enforced here — the ring is drawn, and it is drawn in an ink that is not the promise the calm
            // ring makes, because a tracker that painted a room with no air in it in the same colour as one
            // with air in it would be the map lying (#573) in the one place it costs a tank.
            // #619 · AND IT IS ASKED PER ROOM, NOT PER FLOOR. This read `RefugeSealHere(ex)` — the FLOOR's
            // one answer — and painted every ring on the floor with it, which was right while a floor had
            // one refuge and nothing else. The moment the failed one became a SECOND room standing beside a
            // working one, a per-floor flag would have greyed out the good refuge too: the map lying (#573)
            // in the one direction that costs a tank, an instrument telling a captain there is no air on a
            // floor that has some.
            //
            // Two lists, two answers, and neither can be got wrong by a later hand: a room is in one or the
            // other by its console KIND, never by a state test somebody has to remember to write. The dead
            // ring is painted at the welded DOOR, which is as close to that room as anybody will get.
            foreach ((double rx, double ry) in RefugesOn())
            {
                Add(rx, ry, home: false);
            }
            foreach ((double wx, double wy) in DarkRefugesOn())
            {
                Add(wx, wy, home: false, dead: true);
            }
            AddNewGround();   // #584
            return list;
        }

        Add(MoonSurface.SpawnX, MoonSurface.SpawnY, home: true);

        // ── #573/#563 slice 3 · EVERY SHELTER THE FAN CAN HEAR, AND THEN THE NEAREST ONE IT CANNOT ──────
        //
        // The shelters are per tile now, so the ground the captain is carrying holds several times what one
        // field did. Painting all of them would have handed the owner back the instrument #585 already
        // complained about — "a beacon that cannot be told apart from its neighbours is decoration" — in its
        // worst form: everything past the fan's reach clamps to the RIM (DeckView.Hud), so eighty rings
        // become a fence of circles around the edge with the way home somewhere inside it.
        //
        // So the rule is the one the instrument already keeps for a mover: what it can HEAR, it places; what
        // it cannot, it points at. Beyond the reach exactly ONE ring is drawn, the nearest roof — which is
        // the honest answer to the only question a captain asks at that distance ("which way is air that is
        // not the ship?") and is strictly more instrument than the old fence of nine.
        //
        // The way home is untouched by any of this. It is painted unconditionally, at every distance, on
        // every frame — #563 slice 2 guarded exactly that, and a range gate that ever reached it would be
        // the one lie this fan may not tell.
        double reach = MotionTracker.DetectionRange(SurfaceVisualHalfWidthDu);
        double nearestBeyond = double.MaxValue;
        (double X, double Y)? farthestWorthAsking = null;
        foreach ((ShelterSpot _, SurfaceStructure.Spec shelter) in SheltersInReach(ex))
        {
            double dx = shelter.CentreX - _avatarX, dy = shelter.CentreY - _avatarY;
            double range = Math.Sqrt((dx * dx) + (dy * dy));
            if (range <= reach)
            {
                Add(shelter.CentreX, shelter.CentreY, home: false);
            }
            else if (range < nearestBeyond)
            {
                nearestBeyond = range;
                farthestWorthAsking = (shelter.CentreX, shelter.CentreY);
            }
        }
        if (farthestWorthAsking is { } outThere)
        {
            Add(outThere.X, outThere.Y, home: false);
        }

        // #585/#584 · AND THE LIFT HEAD, once the door is known. Owner, standing in a ruin that happened to
        // have a violet door: "it should be this space? but how do I get in this has purple door and is not
        // emergency shelter?"
        //
        // Two failures behind that one sentence. First, the HUD has been saying "E at the ⊙ HIDDEN DOOR —
        // force the secret lab open" while nothing anywhere says WHERE it is (#584, filed before he hit it and
        // then hit anyway). A prompt you cannot act on is worse than silence.
        //
        // Second, mine and worse: I gave imported violet to shelters (always), to about one ruin door in
        // seven, AND to the lift head — so a colour that was supposed to mean "somebody shipped this here"
        // now means "some doors", and the one door it most needed to distinguish was lost among them. A
        // signal that fires on three unrelated things is not a signal.
        //
        // The beacon is the honest fix: the ground can carry as many violet doors as the fiction wants, and
        // the INSTRUMENT says which one is the way down.
        //
        // #625 · AND IT POINTS AT THE HUT THAT WAS BUILT, not at the seed the hut grew from. This read
        // lab.DoorX/DoorY — SecretLab.For's RAW spot — while the shed the captain walks to stands at
        // SecretLab.HeadSpot, which is that spot moved clear of the shelters, the outpost and the monolith
        // already standing on that ground. The #602 bug with a beacon instead of a lift car.
        //
        // THE ISSUE UNDERSTATED IT, AND THE GUARD IS WHY WE KNOW. #625 was filed as a tidy — "not wrong on
        // screen today, because the wash is vague and the nudge is small enough to sit inside it" — and the
        // sweep written to go with the fix disagreed on 21 of 34 body × site pairs, by up to 235 du. The
        // reason is a seam nobody had looked straight at: the raw spot is seeded PER BODY and the clamp
        // re-seeds PER SITE, so when it fires it does not nudge, it RELOCATES. Miranda painted the same patch
        // of ground on all three of its sites while the hut stood somewhere else on two of them. That is #584
        // — the map lying — and it was already shipping, precise ring and all.
        //
        // So it asks the same function the builder asks, and there is nothing left to keep in sync.
        if (ex.SecretLabDoorRevealed && ex.Lab is { HasLab: true })
        {
            (double headX, double headY) = SecretLabHeadSpot(ex);
            Add(headX, headY, home: false, lab: true);
        }

        AddNewGround();   // #584
        return list;
    }

    /// <summary>#573 · Your own buried caches, as marks on the fan — but ONLY once they are inside its
    /// reach. The range gate is the entire design: a field this size made finding your own ✗ a real task,
    /// and an instrument that always knew where it was would take that task straight back off you.</summary>
    private List<(double Bearing, double Range)> BuildCacheBeacons()
    {
        var list = new List<(double, double)>();
        foreach ((double mx, double my, bool _) in _hudMarks)
        {
            double dx = mx - _avatarX, dy = my - _avatarY;
            double range = Math.Sqrt((dx * dx) + (dy * dy));
            if (range <= CacheDetectRangeDu)
            {
                list.Add((Math.Atan2(dy, dx), range));
            }
        }
        return list;
    }

    /// <summary>How close a buried cache has to be before the fan admits to it.</summary>
    private const double CacheDetectRangeDu = 55.0;

    /// <summary>#573 · What the captain has been TOLD, as opposed to what they can see — a wide, soft wash
    /// rather than a mark. A tip narrows a search; it does not end one, and a dot would claim a precision
    /// the information does not have.</summary>
    private List<(double Bearing, double Range, double Spread)> BuildRumours(SurfaceExcursion ex)
    {
        var list = new List<(double, double, double)>();
        if (ex.Lab is not { HasLab: true } || ex.SecretLabDoorRevealed)
        {
            return list;
        }
        // #585: a tip you were GIVEN counts, not only a place you have already been. This used to read only
        // _secretLabsFound, so the wash helped on a return visit and did nothing on the first — the one visit
        // where a captain actually needs help. The clue chain had no way into the instrument at all.
        if (!_labLeads.Contains(ex.Stop.Body.Id) && !_secretLabsFound.Contains(ex.Stop.Body.Id))
        {
            return list;   // nobody has tipped you about this one; there is nothing to be vague about
        }

        // #625 · Centred on the SHED, not on the seed it grew from — the same fix as the revealed ring above,
        // and the one the issue was actually filed about. A tip that is vague about the right ground is a tip;
        // a tip that is vague about the wrong ground is a lie with a wide brush, and this brush is 45 du wide
        // while the ground it was missing by ran to 235. A captain who walked a bearing off this wash on
        // Titan's Quiet Basin arrived at empty regolith with a tank half gone and no reason to doubt the
        // instrument, which is the exact failure #573 built the fan to prevent.
        (double headX, double headY) = SecretLabHeadSpot(ex);
        double dx = headX - _avatarX, dy = headY - _avatarY;
        list.Add((Math.Atan2(dy, dx), Math.Sqrt((dx * dx) + (dy * dy)), RumourSpreadDu));
        return list;
    }

    /// <summary>How wide a rumour reads on the fan, in deck units. Big — it is a tip, not a fix.</summary>
    private const double RumourSpreadDu = 45.0;
}

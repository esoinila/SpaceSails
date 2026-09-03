using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #608 · THE REFUGES — A DEAD FLOOR IS A FLOOR OF SUIT-WORK, AND SUITS RUN OUT ─────────────────────
    //
    // Owner, in the order he said it, after suffocating on B2: "I thought there is air in the base?" ...
    // "there should be a warning or something :-D ... the rooms should have airlocks etc ... some havens
    // :-D" ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are
    // emergency shelters :-D" ... "Still for safety there would need to be a couple of places with air lock
    // and air refilling, because otherwise the elevator being busy could kill employees, and those honest
    // criminal scientists are hard to recruit :-D" ... and finally, deciding it:
    //
    //     "there should be like at least one air replenish station in each of the airless labs
    //      underground... for pure safety"
    //
    // AT LEAST ONE, ON EVERY AIRLESS FLOOR. Not "most floors", not "a rare one" — a regulation, in-world and
    // in code, and RefugesAreOnEveryAirlessFloor walks every floor of every band on every body to say so.
    //
    // THE REASON IT IS RIGHT, which is the owner's and is better than the mechanic it costs. He also ruled
    // on why any floor down here is pressurised at all: "the thought about the dead floors is that it is
    // very difficult to work in the suit. So all work would happen out of it. So any room that would house
    // like office work would be pressurized by that constraint" — "like writing with a pen ... reading
    // documents etc.... that kind of thing would not happen at all in vacuum as a working environment" —
    // "or any kind of fine motor skill stuff".
    //
    // So an airless floor is not an ABANDONED floor. It is a floor of SUIT-WORK: storage, hauling, plant,
    // hard-vacuum process. It had people in it, in suits, all day, every day — and a building that staffs a
    // vacuum floor and gives its staff nowhere to go when a tank runs short is a building that is one busy
    // lift away from killing somebody. Whoever inspected this place made them pay for the refuge. That the
    // pressure vessels are still holding decades after the last invoice is the same sentence the surface
    // shelter tells (#573): somebody built this for a stranger and it outlasted them.
    //
    // WHAT IT DOES NOT DO IS CANCEL #585. Depth is still paid for in air, because a refuge is not a floor:
    //
    //   * it is NEVER beside the lift (MinRefugeDetourDu) — reaching one is a decision to detour, which is
    //     the verb #608 asked for: not "how long dare I stay" but "can I get from the car to the refuge to
    //     the room I want and back";
    //   * its rack is the SURFACE rack, law for law — SurfaceShelter.Produce/Transfer and the two-thirds
    //     ceiling somebody set on purpose for the next person through the door. More refuges buy RANGE,
    //     never independence, exactly as more shelters do;
    //   * it holds pressure and nothing else. There is no locker down here, no reload, no bunk.
    //
    // Canon holds: the plate says what the room is FOR and never what the building was for. A safety sign is
    // the one thing on this ground that is allowed to be plain — a captain who cannot find air is not being
    // teased (#573) — and it is still an inspectorate's sign, not an explanation.

    /// <summary>Half the breathable width of a refuge, in deck units — the room's own box, inset by the
    /// poured wall. <see cref="RefugeHolds"/> is the one place that reads it.</summary>
    public const double RefugeHalfWidth = 6.3;

    /// <summary>Half the breathable height of a refuge.</summary>
    public const double RefugeHalfHeight = 4.8;

    /// <summary>How far a refuge must stand from the lift before it counts as one worth having.
    ///
    /// <para>#608: <i>"Never on the way. If it sits beside the lift it is decoration; it earns its existence
    /// by being somewhere you have to decide to detour to."</i> Measured from the shaft, so this is the
    /// smallest walk a captain can ever be asked for — and it is a floor plan, so the real one is longer.</para>
    ///
    /// <para><b>Why 70 and not 34.</b> This shipped for an hour as 34, which was chosen by eye and was
    /// WORTHLESS: the nearest room to the shaft that this generator can produce, measured over 808 dead
    /// floors, is 34.2 du out — so every room on every floor qualified, the constraint selected nothing, and
    /// the guard that was supposed to enforce it passed happily on a build deliberately rigged to put the
    /// refuge in the closest room there is. That is the house rule this repo names out loud (revert the fix
    /// and watch the guard go RED), and it caught a threshold that meant nothing.</para>
    ///
    /// <para>At 70 it is twice the nearest possible room and still satisfiable on every floor the generator
    /// makes, so the detour is real AND the fallback below never has to fire.</para></summary>
    public const double MinRefugeDetourDu = 70.0;

    /// <summary>Is (<paramref name="x"/>, <paramref name="y"/>) inside the air of the refuge centred at
    /// (<paramref name="cx"/>, <paramref name="cy"/>)?
    ///
    /// <para><b>The one containment law</b>, so Core, the audit and the live suit cannot disagree about
    /// whether the captain is breathing. Rectangular rather than the shelter's inscribed ellipse
    /// (<c>SurfaceShelter.Contains</c>) for the one reason that matters: a shelter is a regolith drum and
    /// its corners are metres of piled dirt, while this is a POURED ROOM with square corners — an ellipse
    /// here would leave a captain standing plainly inside a sealed room watching their tank tick down, which
    /// is precisely the kind of instrument-disagrees-with-the-world lie this ground keeps paying for.</para></summary>
    public static bool RefugeHolds(double cx, double cy, double x, double y) =>
        Math.Abs(x - cx) <= RefugeHalfWidth && Math.Abs(y - cy) <= RefugeHalfHeight;

    // ── #608 · AND WHAT DECADES DID TO IT ───────────────────────────────────────────────────────────────
    //
    // The regulation above says what was BUILT, and it is not in question: every airless floor has a refuge
    // and the plan still marks it. What the plan does not carry is whether the thing still works, and the
    // owner said so in the same breath he asked for them: "their state after decades is the story. The ones
    // that still hold are the ones somebody maintained; the ones that do not are the ones somebody stopped
    // being paid to" — and, sharper, the warning against the version of this feature that costs nothing:
    // "If every ADMINISTRATION floor is safe, deep ADMINISTRATION floors stop costing anything. The state of
    // the seal is what keeps it honest."
    //
    // So the room is a fact and the SEAL is a story, in three states:
    //
    //   HOLDING · the door cycles, the room holds, and the rack still has bottles in it. This is what the
    //             whole feature shipped as, and it is now the minority case.
    //   EMPTY   · the door cycles and the room holds. Nothing to refill from. It is still the thing the
    //             owner asked for — "otherwise the elevator being busy could kill employees" is answered by
    //             a room you can wait in, not by a rack — so it buys TIME and never range.
    //   FAILED  · the door will not cycle. It is on the plan, the tracker paints it, and it is dead.
    //             Owner, on the fan (#604): "A refuge whose seal has failed must still paint, and must read
    //             as failed. Walking to one and finding it dead is a real beat; walking to one that was
    //             never marked is just a bad map."
    //
    // WHICH FLOORS KEPT THEIRS is #601's answer and not a roll: the refuge that works is on the floor whose
    // department could still get a maintenance line approved. A captain who has learnt the livery (#605) has
    // therefore learnt where the air is — gold and blue — which is the colour language doing real work
    // instead of decorating, and it is knowledge earned off the lift panel before the ride rather than off a
    // corpse afterwards.

    /// <summary>What a refuge's seal has done with the decades since anybody paid for it.</summary>
    public enum RefugeState
    {
        /// <summary>The door cycles, the room holds, and there is air in the rack.</summary>
        Holding,

        /// <summary>The door cycles and the room holds. The fill line is empty: shelter, never resupply.</summary>
        Empty,

        /// <summary>The seal went. The room is on the plan and holds nothing.</summary>
        Failed,
    }

    /// <summary>#601 · The departments whose maintenance line survived — the ones whose refuge still has air
    /// in it. Read out of <see cref="Departments"/> rather than typed as new words, so a floor's plate, its
    /// livery and its air can never come to mean three different things.
    ///
    /// <para>Two, and not four. ARCHIVE and ISOLATION are just as much fine-motor work by the owner's own
    /// test, and adding them would put working air on half of every site's dead floors — which is precisely
    /// the "deep floors stop costing anything" he warned about in the same comment. Two of eight is a
    /// quarter, which is often enough to be worth learning and rare enough to still be a plan.</para></summary>
    public static readonly string[] DepartmentsThatKeptTheLine = ["ADMINISTRATION", "LABORATORIES"];

    /// <summary>Is a pressure refuge marked on this floor's plan? The LAW, not a count taken off a built
    /// floor — the lift panel asks it about floors it has not generated and the card asks it about the one
    /// underfoot, and a panel that answered by building twenty floors would be a panel nobody presses twice.
    ///
    /// <para><c>EveryAirlessFloorHasARefuge</c> and <c>APressurisedFloorCarriesNoRefuge</c> are what keep
    /// this honest: they walk every floor of a hundred sites and check that the generator agrees with the
    /// sentence written here.</para></summary>
    public static bool RefugeOnThePlan(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return level < 0 && !HoldsPressure(bodyId, level);
    }

    /// <summary>What state the refuge on this floor is in — null where the plan marks none.
    ///
    /// <para>A fact about the FLOOR rather than about the room, which is why the client may ask it directly
    /// instead of carrying it down through the deck plan: there is one refuge per floor, its state is decided
    /// here, and one ask is one answer.</para>
    ///
    /// <para>The head office keeps every one of its seals, and that is the rank difference again (#411)
    /// rather than a kindness — its livery "is still being kept up, by nobody, on a schedule", and a building
    /// that repaints its corridors has not let its compressors go. The band nobody listed (#592) keeps none
    /// of them for the mirror-image reason: a maintenance line is a budget code, and there is no budget code
    /// for a floor the building refuses to admit it has.</para></summary>
    public static RefugeState? StateOfTheRefugeOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (!RefugeOnThePlan(bodyId, level))
        {
            return null;
        }
        if (IsHeadOffice(bodyId))
        {
            return RefugeState.Holding;
        }
        if (!IsUnlisted(bodyId, level)
            && Array.IndexOf(DepartmentsThatKeptTheLine, DepartmentOf(bodyId, level)) >= 0)
        {
            return RefugeState.Holding;
        }

        // The rest is what an unpaid invoice does over decades, and it is a coin rather than a rule because
        // there is nothing left to read it off: the department that stopped being funded stopped keeping
        // records too. Seeded, so a captain who learns a building learns it for good.
        return DiceRule.Roll(DiceRule.Seed($"hive:refuge-seal:{bodyId}:{level}"), 2).Face == 1
            ? RefugeState.Empty
            : RefugeState.Failed;
    }

    /// <summary>Does the refuge on this floor hold pressure at all — the one question the suit asks. Empty
    /// still counts: the door cycles and the room holds, and that is the difference between a wait and a
    /// death.</summary>
    public static bool RefugeStillHolds(RefugeState state) => state != RefugeState.Failed;

    /// <summary>What is said once, at the door, by state. Three sentences for three worlds, and none of them
    /// says what to do about it.</summary>
    public static string RefugeEntryLine(RefugeState state) => state switch
    {
        RefugeState.Holding =>
            "🫁 The door cycles and the gauge climbs. A rack of bottles on the wall, and the meter on the " +
            "fill line still turns.",
        RefugeState.Empty =>
            "🫁 The door cycles and the room holds. The fill line reads empty, and the tag on the valve is " +
            "dated years ago.",
        _ =>
            "🫁 The door will not cycle. The seal went a long time ago, and somebody wrote the date on the " +
            "frame.",
    };

    /// <summary>One pressure refuge: a room somebody kept the seals on, with an air cracker in it.</summary>
    /// <param name="State">#608 · What the decades did to it — <see cref="StateOfTheRefugeOn"/>, carried on
    /// the room so the renderer that draws the plate has the answer the suit is using.</param>
    public readonly record struct Refuge(
        double X, double Y, string Sign, RefugeState State = RefugeState.Holding)
    {
        /// <summary>Is the captain in its air? <see cref="RefugeHolds"/>, so there is only ever one answer.
        /// Geometry only — whether that air EXISTS is <see cref="State"/>'s business.</summary>
        public bool Contains(double x, double y) => RefugeHolds(X, Y, x, y);
    }

    /// <summary>What is stencilled beside a refuge door. An inspectorate's plate: a number, an occupancy and
    /// a date somebody stopped renewing — which is the whole story of this building told by a form.</summary>
    public static string RefugeSign(string bodyId, int level, int index)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ulong seed = DiceRule.Seed($"hive:refuge-sign:{bodyId}:{level}:{index}");
        int number = (int)(seed % 40) + 1;
        int occupancy = 4 + (int)((seed / 11) % 9);
        return $"🫁 PRESSURE REFUGE {number} · OCCUPANCY {occupancy} · KEEP CLEAR";
    }

    /// <summary>The refuges on a floor, taken out of the rooms it had already built.
    ///
    /// <para>A refuge IS one of the floor's rooms — three poured walls and a doorway cut in its corridor
    /// face — and that is deliberate rather than lazy. A room is already audited walkable from the lift
    /// (13.1), already has a door the captain can find, and already sits down a rib rather than on the
    /// spine. Inventing a second kind of chamber would be a second thing to keep reachable, and a refuge you
    /// cannot walk to is a refuge that does not exist.</para>
    ///
    /// <para>It stops being a haul room when it becomes one: a pressure vessel somebody maintained is not a
    /// drawer to turn over, and the air is what it pays.</para></summary>
    private static List<Refuge> CarveRefuges(
        string bodyId, int level, List<Room> rooms,
        in SurfaceLayout.Field field)
    {
        var refuges = new List<Refuge>();
        if (HoldsPressure(bodyId, level) || rooms.Count == 0)
        {
            return refuges;   // a pressurised floor IS the refuge — and every gallery is one (#677)
        }

        // #592 · The one room that may never be taken. On a site with a band nobody listed, room 0 of the
        // last listed floor is the card that reaches it (KeyRoomFor) — designated exactly because a rolled
        // index would sometimes miss and strand the whole feature forever. Turning it into a refuge would
        // do the same thing by a different route.
        int reserved = KeyRoomFor(bodyId) is { } key && key.Level == level ? key.RoomIndex : -1;

        var faraway = new List<int>();
        var anywhere = new List<int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == reserved)
            {
                continue;
            }
            anywhere.Add(i);

            // #801 · A DETOUR FROM EVERY CAR, not from the cage. This measured one shaft, and the day the
            // building grew a second one at the other end of the corridor it went on passing while the
            // sentence it exists to protect died: a third of the refuges in the game were four steps from
            // the goods car. The guard found it (332 of 1130 floors); the fix is that the carve asks the
            // same list the guard does.
            bool far = true;
            foreach (Shaft car in ShaftsOn(field))
            {
                double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                far &= (dx * dx) + (dy * dy) >= MinRefugeDetourDu * MinRefugeDetourDu;
            }
            if (far)
            {
                faraway.Add(i);
            }
        }

        // The detour is the design, so it is preferred — but it is NOT allowed to cost the guarantee. On a
        // floor whose rooms all happen to crowd the shaft, a near refuge beats no refuge, every time: the
        // owner's line is "at least one ... for pure safety", and a safety regulation that a seed can talk
        // out of is not one.
        // #801 · …and when NOTHING qualifies, the fallback takes the FURTHEST room rather than a rolled one.
        // With two cars at opposite ends of the spine there are floors whose every chamber is inside the
        // detour of one car or the other, and on those the old fallback rolled a room at random — which on
        // the sweep put a refuge twenty-nine du from a car on floors that had a sixty-du one going spare.
        // A safety regulation a seed can talk out of is not one, and neither is one it can shrug at.
        List<int> pool = faraway;
        if (pool.Count == 0 && anywhere.Count > 0)
        {
            int best = anywhere[0];
            double bestNear = -1;
            foreach (int i in anywhere)
            {
                double near = double.MaxValue;
                foreach (Shaft car in ShaftsOn(field))
                {
                    double dx = rooms[i].X - car.X, dy = rooms[i].Y - car.Y;
                    near = Math.Min(near, (dx * dx) + (dy * dy));
                }
                if (near > bestNear)
                {
                    (best, bestNear) = (i, near);
                }
            }
            pool = [best];
        }
        if (pool.Count == 0)
        {
            return refuges;
        }

        int pick = pool[DiceRule.Roll(DiceRule.Seed($"hive:refuge:{bodyId}:{level}"), pool.Count).Face - 1];
        Room chosen = rooms[pick];
        rooms.RemoveAt(pick);
        refuges.Add(new Refuge(
            chosen.X, chosen.Y, RefugeSign(bodyId, level, 0),
            // The seal is decided by the FLOOR, not by the carve, and asked here rather than worked out
            // again: the panel, the card, the tracker and the suit all read StateOfTheRefugeOn, and a room
            // that carried a second opinion about its own door is this repo's oldest and dearest bug.
            StateOfTheRefugeOn(bodyId, level) ?? RefugeState.Holding));
        return refuges;
    }

    /// <summary>What the console inside is called.</summary>
    public const string RefugeTankLabel = "🫁 REFUGE RACK";

    /// <summary>What the plate over the door says at signage size — short enough to read at a run, because
    /// that is how it will be read.
    ///
    /// <para>It names the ROOM, not the floor, and that word is load-bearing (#612). The plate by the lift
    /// is simultaneously shouting NO ATMOSPHERE about the level; a sign forty du away reading only AIR
    /// would be a second instrument appearing to contradict the first, which is the one thing #612 says is
    /// worse than saying nothing. <c>REFUGE ·</c> makes the scope of the claim part of the claim.</para></summary>
    public const string RefugeGlyph = "🫁 REFUGE · AIR";

    /// <summary>The same plate on a room whose seal went. It is the stencil and nothing else: a plate does
    /// not change when a compressor dies, and the whole tell is the WORD THAT IS MISSING — a captain who has
    /// read <c>REFUGE · AIR</c> on two floors reads this one and knows before the walk.
    ///
    /// <para>Owner (#604): <i>"A refuge whose seal has failed must still paint, and must read as failed."</i>
    /// So the map does not quietly drop it, and it does not go on promising air either.</para></summary>
    // FABLE: line needed — a failed refuge's map glyph, if the bare plate is too quiet to read as failed.
    public const string RefugeFailedGlyph = "🫁 PRESSURE REFUGE";

    /// <summary>Which plate a refuge in this state wears. One place, so the deck plan and the tracker cannot
    /// come to disagree about what the room claims.</summary>
    public static string RefugeGlyphFor(RefugeState state) =>
        state == RefugeState.Failed ? RefugeFailedGlyph : RefugeGlyph;

    /// <summary>#608 · What the lift panel prints on a floor whose plan carries a refuge. It says a refuge is
    /// THERE and never what state it is in — the plan is a drawing made when the building was new, and no
    /// drawing knows which compressors are still turning. Finding that out is the walk.</summary>
    public const string RefugeRowTag = "REFUGE";

    // ── #609 · THE ONE THING YOU MUST NOT MISS ──────────────────────────────────────────────────────────
    //
    // Owner, after suffocating on B2: "I thought there is air in the base?" ... "there should be a warning
    // or something :-D" ... "maybe pop-up about you have air or you are in vacuum type ... it is vital info"
    // ... "like the basement is more dangerous than the surface now :-D" ... "on surface there are emergency
    // shelters :-D"
    //
    // He is right on every count, and the last two are the argument. The surface gives a captain a visible
    // building to run to; a dead floor gives them a number they have to have been told. The rule itself is
    // good and stays exactly as it is — the top of each shaft band holds pressure and the rest costs air —
    // but it was being announced in a pulse that fades in eight seconds, between one about bench hardware
    // and one about dust.
    //
    // So the first dead floor of an excursion stops the world and says it properly, WITH THE ARITHMETIC:
    // which floors have air, how far the nearest one is, and how long the tank has. After that the pulse
    // line is enough, because by then it is knowledge rather than news.

    public const string VacuumArtUrl = "art/the-dead-air.jpg";

    public const string VacuumCardLabel = "🫁 DEAD AIR";

    /// <summary>What the first dead floor says. It states the rule and does the sum — a warning that makes
    /// the captain work out their own margin is a warning delivered too late.
    ///
    /// <para>#740 · And it does the sum in the SUIT'S units, off <see cref="SuitAir.Clock"/>, because the
    /// card and the gauge are describing one tank and a captain compares them by eye.</para></summary>
    public static string VacuumCard(string bodyId, int level, double airSeconds)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        int band = BandOf(level);
        int refuge = BandTop(band);          // the top of this band always holds pressure
        int floorsUp = -level - -refuge;     // how many floors between here and breathable

        // #740 · THE CARD READS THE GAUGE, it does not do its own sum. This used to format the raw play
        // budget as minutes and seconds — "you have 21 min 01 s" — while the HUD two seconds later on the
        // same floor said AIR 8h09. Both sentences were about the tank and both were honest about the number
        // they held; they were simply holding it in different units, because the card was the one surface in
        // the game that had never gone through SuitAir. A captain cannot be expected to know which of two
        // instruments is quoting the designer's stopwatch, so there is now one clock and the card asks for it.
        //
        // …and the sentence OWNS the quantity: it names the instrument the figure came off, so that a captain
        // who glances at their wrist a second later reads the same characters back, and so that the next hand
        // to edit this copy cannot quietly re-derive the number from something else.
        string margin = airSeconds > 0
            ? $"Your gauge reads {SuitAir.Clock(airSeconds)}, and that is the figure it will go on counting " +
              "down the whole way up."
            : "Your gauge is already reading empty, which is its own instruction.";

        string upstairs = floorsUp == 0
            ? "this floor"
            : $"{floorsUp} floor{(floorsUp == 1 ? "" : "s")} up";

        return
            "The doors part on nothing.\n\n" +
            "No pressure, no lights but yours, and the dust has not been disturbed since it settled. You " +
            $"are {MetresDown(level):F0} m under the regolith and your tank is now the clock.\n\n" +
            "THE RULE, because it is the only one down here that can kill you: the TOP FLOOR OF EVERY SHAFT " +
            "BAND holds pressure. Nothing else does. That is where the lobbies were, and the fans on those " +
            "floors are still turning on somebody's account.\n\n" +
            $"The nearest floor of air is {NameOf(bodyId, refuge)} — {upstairs}. {margin}\n\n" +
            // #608 · AND THE OTHER HALF, now that it is true. This card used to end "there are no shelters
            // down here", which was honest when it was written and is now the most dangerous sentence in the
            // game: a captain who believes it will ration a tank they did not have to ration. Owner: "there
            // should be like at least one air replenish station in each of the airless labs underground...
            // for pure safety". So the card says where the exception is, and says the two things about it
            // that decide whether it is any use — it is not beside the lift, and the instrument finds it.
            "There is a PRESSURE REFUGE on this floor. Every vacuum floor in this building has one: staff " +
            "worked these levels in suits all day, and somebody with a clipboard made the owners pay for " +
            "somewhere to go when a tank ran short. It is not beside the lift — it never is — and your " +
            "tracker paints it as a ring like any shelter on the surface.\n\n" +
            // #608 · AND THE SENTENCE THAT STOPS THE CARD PROMISING SOMETHING THE BUILDING CANNOT KEEP.
            // The paragraph above is about what was BUILT and every word of it is still true. What the card
            // may not do is let a captain read "somewhere to go when a tank ran short" as "air, forty du
            // that way" — because a quarter of these rooms have air, some hold and have nothing, and some
            // will not open at all. The plan is a drawing; the seal is decades of nobody paying for it.
            "The plan marks a refuge on this band. Whether it still holds is not on the plan.";
    }

    /// <summary>Said on stepping out on the top floor — the lie that makes the rest work.</summary>
    public const string PressurisedLine =
        "🫁 The doors part on warm air and standing lights. Your suit stops drawing and the readout holds. " +
        "Somewhere a fan is still turning, on somebody's account, decades after the last invoice.";

    /// <summary>And on every floor below it.</summary>
    public const string DeadAirLine =
        "🫁 The doors part on nothing. No pressure, no lights but yours, and the dust on the floor has not " +
        "been disturbed since it settled. Your tank starts counting again. From here down, depth costs air.";

    /// <summary>What a locked door says when the captain tries it. It never opens, and the game never pretends
    /// it might — a door that teases is a puzzle, and this is meant to be a WALL with a world behind it.
    ///
    /// <para>#1074 · <b>One door in the building answers with what is POSTED on it instead</b>, because one
    /// door in the building has something posted on it: the order at the seal. The plate is the heading and
    /// the sentence under it is the order, verbatim (<see cref="StopOrder.OrderLine"/>) — which is what a
    /// stop order IS, a piece of paper somebody stuck to a door, and it is the whole of what the world ever
    /// says about the closure. Nothing is composed here: the two strings are set side by side and no third
    /// sentence explains either of them.</para></summary>
    public static string LockedLine(string sign)
    {
        ArgumentNullException.ThrowIfNull(sign);
        return StopOrder.IsPlate(sign)
            ? $"🔒 {sign}. {StopOrder.OrderLine}"
            : $"🔒 {sign}. The lock is not a lock you can argue with — it is a decision somebody made, and "
                + "it is still being enforced by a building whose owners stopped answering a long time ago.";
    }

    /// <summary>#600 · How far under the regolith the shed's floor a given level sits, in metres.
    ///
    /// <para>Owner: <i>"we can use seriously large numbers there :-D ... or depths (in meters)"</i>. He is
    /// right that the depth is the better number — <c>B4</c> is an index and <c>−76 m</c> is a fact about
    /// where you are standing, and it is the one that makes the walk back up mean something.</para>
    ///
    /// <para>The first floor is far down because the facility is BURIED — the shed on the surface is a lid
    /// over a shaft, and the descent card earns that ("service lamps go past in the wall at first, then a
    /// rhythm, and you find you have been counting them and have lost count"). After that a floor is a
    /// floor plus its slab, its services and the rock somebody left between levels.</para>
    ///
    /// <para>Owner, reading the paint on B1: <i>"also we could make it deeper like 150 meters :-D"</i> — and
    /// he is right, 40 m was a car park. The overburden is the number that has to sell the lid, because it
    /// is the whole ride down before the first door opens, and the descent card has always described a shaft
    /// long enough to lose count in. At 150 m it does.</para></summary>
    public const double OverburdenMetres = 150.0;

    /// <summary>Floor to floor, including the slab and the rock between.</summary>
    public const double MetresPerFloor = 12.0;

    /// <summary>Metres below the surface for a level. 0 on the surface, positive going down.</summary>
    public static double MetresDown(int level) =>
        level >= 0 ? 0 : OverburdenMetres + ((-level - 1) * MetresPerFloor);

    /// <summary>What is painted on the wall beside the lift, big enough to read on the way past.</summary>
    public static string DepthPaint(int level) =>
        level >= 0 ? "SURFACE" : $"−{MetresDown(level):F0} m";
}

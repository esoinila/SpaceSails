using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// Part of <see cref="HiveInterior"/> (the header note lives in HiveInterior.cs) - THE PASSES THAT WRITE
/// ON THE WALLS: the incident board and the conference posters, the watchclock stations, the two cars and
/// the stair that is not one, the floor's landmarks, the big plate over the car's mouth and the refuge
/// plates that answer it in the same language.
///
/// <para>#1164 - Each was a <c>// -- banner --</c> section inside <see cref="FloorDeck"/> and is now a
/// named pass called from it in the same order. <see cref="PlateTheWatchclocks"/> and
/// <see cref="CallTheCars"/> are ADJACENT here on purpose: a guard cuts the block between the #831 banner
/// and the cars' first comment and asserts on what is inside it, so a pass slipped between them would
/// change what that guard reads without changing what it claims.</para>
///
/// <para>ONE PASS RETURNS ITS ACCUMULATOR RATHER THAN BEING HANDED ONE.
/// <see cref="PlateTheFloorByTheLift"/> starts the big-label list with the three lines it paints
/// over the car, and <see cref="PlateTheRefuges"/> then adds to it - which is exactly the shape the
/// original had, where the list was declared by a collection initialiser in the middle of the method and
/// appended to twenty lines later.</para>
/// </summary>
public static partial class HiveInterior
{
    /// <summary>
    /// #864 - HANGS THE INCIDENT BOARD on the one lab chamber wall that carries one. A ViewObject and not a
    /// new verb: stop at a thing on a wall, look at it, read the card. Lays down: consoles.
    /// </summary>
    private static void HangTheIncidentBoard(List<DeckPlan.ConsoleSpot> consoles,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #864 · THE INCIDENT BOARD, ON THE ONE LAB CHAMBER WALL THAT CARRIES ONE ────────────────────
        //
        // Owner, from a room he ran an AFM and a transmission electron microscope in: "This lab has survived
        // X days without sarcasm on the wall. That kind gags are such lab humor. :-D"
        //
        // A ViewObject, exactly as the posters below are, and for the same reason: it is not a new verb —
        // stop at a thing on a wall, look at it, and read the card. It carries NO art slot, because it is a
        // text board and the plate idiom carries it whole; there is nothing here to degrade.
        if (floor.TheBoard is { } sarcasmBoard)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.ViewObject,
                (float)sarcasmBoard.X, (float)sarcasmBoard.Y,
                sarcasmBoard.Plate, null, sarcasmBoard.Card));
        }

    }

    /// <summary>
    /// #853 - HANGS THE CONFERENCE POSTERS on the one department they are about, on the monolith's own press:
    /// a ViewObject whose art slot degrades, so they can ship with the copy today and the pictures whenever
    /// they are shot. Lays down: consoles.
    /// </summary>
    private static void HangThePosters(List<DeckPlan.ConsoleSpot> consoles,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #853 · AND THE CONFERENCE POSTERS, ON THE ONE DEPARTMENT THEY ARE ABOUT ────────────────────
        //
        // Owner, a postdoc's own gag: "as gags we could have gen-ai conference posters … jokes about how
        // hydrogen is the new promising tech (still 100 years in future)".
        //
        // A ViewObject and NOT a new console kind, because it is not a new verb: stop at a thing on a wall,
        // look at it, and read the card. That is the same press the monolith, the false slab and the ports'
        // own PIRATE INSURANCE poster have taken since #380 — and the art slot degrades exactly as theirs
        // do, which is why these can ship with the copy today and the pictures whenever they are shot.
        //
        // The kicker hangs crooked. The deck draws its labels straight, so the crookedness is TOLD — Core
        // puts it in the plate (LabPosters.Poster.Plate) rather than this file inventing a rotation nobody
        // asked for. A renderer that can tilt a label cheaply has the flag published and waiting.
        foreach (LabPosters.Poster poster in floor.TheWalls)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.ViewObject,
                (float)poster.X, (float)poster.Y,
                poster.Plate, poster.ArtUrl, poster.Card));
        }

    }

    /// <summary>
    /// #831 - PLATES THE WATCHCLOCK STATIONS on the floors that have a round. A small plate on a wall and
    /// nothing else - no console, no verb, no card - because it answers a question the player asks with their
    /// eyes, and asking it with [E] would turn an answer into an errand. Lays down: labels.
    /// </summary>
    private static void PlateTheWatchclocks(
        List<(float X, float Y, string Text)> labels, in UndergroundComplex.FloorPlan floor,
        in SurfaceLayout.Field field, string bodyId, int level)
    {
        // ── #831 · AND THE WATCHCLOCK STATIONS, ON THE FLOORS THAT HAVE A ROUND ────────────────────────
        //
        // Owner: "they actually in real life like have these check points they electronically sign on rounds
        // to prove they did their round." A small plate on a wall, and nothing else — no console, no verb, no
        // card. It answers a question the player asks with their eyes ("why is he standing there") and asking
        // it of a plate with [E] would turn an answer into an errand.
        //
        // Core says where every one of them is (PatrolBeat.CheckpointsOn) and what is stencilled on it; this
        // measures nothing.
        if (PatrolBeat.IsPatrolled(bodyId, level))
        {
            foreach (PatrolBeat.Checkpoint point in PatrolBeat.CheckpointsOn(floor, field))
            {
                labels.Add(((float)point.X, (float)point.Y, point.Plate));
            }
        }
    }

    /// <summary>
    /// #801 - CALLS THE CARS, both of them, off one list and each with the sign Core paints on it. Lays down:
    /// consoles.
    /// </summary>
    private static void CallTheCars(List<DeckPlan.ConsoleSpot> consoles, in SurfaceLayout.Field field)
    {
        // The cars, on every floor, in the same places. #801 · Both of them, off one list, each with the
        // sign Core paints on it — a renderer choosing which console kind goes on which car would be a
        // second opinion about a machine it does not own.
        foreach (UndergroundComplex.Shaft car in UndergroundComplex.ShaftsOn(field))
        {
            bool cage = car.Kind == UndergroundComplex.ShaftKind.Cage;
            consoles.Add(new(
                cage ? DeckPlan.ConsoleKind.HiveLift : DeckPlan.ConsoleKind.HiveServiceLift,
                (float)car.X,
                (float)(car.Y + ((cage ? 1 : -1) * (UndergroundComplex.CorridorHalf + 2.5))),
                car.Sign));
        }
    }

    /// <summary>
    /// #719 - OPENS THE WAY OUT THAT IS NOT A CAR, on every floor the building admits to and on no ground
    /// with no blind end to cut one into - the same silence the goods car keeps where the field will not take
    /// it. Lays down: consoles.
    /// </summary>
    private static void OpenTheStair(
        List<DeckPlan.ConsoleSpot> consoles, in SurfaceLayout.Field field, string bodyId, int level)
    {
        // #719 · …AND THE WAY OUT THAT IS NOT A CAR, on every floor the building admits to. Same idiom, same
        // arithmetic, same source for its sign: the pocket hangs off the upper face like the cage's, so the
        // console stands the cage's own way into it. Nothing is drawn on a floor the building never declared
        // (UndergroundComplex.HasStairOn) and nothing on a ground with no blind end to cut one into, which is
        // the same silence the goods car keeps where the field will not take it.
        if (UndergroundComplex.HasStairOn(bodyId, level)
            && UndergroundComplex.StairOn(field) is { } stair)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.HiveStair,
                (float)stair.X,
                (float)(stair.Y + UndergroundComplex.CorridorHalf + 2.5),
                stair.Sign));
        }
    }

    /// <summary>
    /// STENCILS THE FLOOR'S OWN LANDMARKS - Core's list, copied onto the plan and nothing else. Lays down:
    /// labels.
    /// </summary>
    private static void StencilTheLandmarks(List<(float X, float Y, string Text)> labels,
        in UndergroundComplex.FloorPlan floor)
    {
        foreach (SurfaceLayout.Landmark m in floor.Labels)
        {
            labels.Add(((float)m.X, (float)m.Y, m.Label));
        }
    }

    /// <summary>
    /// #600/#605/#612 - PAINTS THE PLATE BY THE CAR, the largest thing drawn on the floor: the depth, the
    /// department under it, and whether you can breathe here - three lines in one plate, directly over the
    /// car's mouth, in the eye-line of somebody who has just turned round. RETURNS the big-label list it
    /// starts, which the refuge plates below then add to.
    /// </summary>
    private static List<(float X, float Y, string Text, float Px, int Tone)> PlateTheFloorByTheLift(
        string bodyId, int level, double shaftX, double shaftY)
    {
        // #600 · THE DEPTH, PAINTED BY THE LIFT. Owner, riding between floors built from the same bones:
        // "something different in every floor so we visually spot some difference" / "we can use seriously
        // large numbers there :-D" / "or depths (in meters)".
        //
        // Two lines, stencilled on the wall beside the car the way a stairwell or a car park marks a level:
        // the depth, which is a fact about where you are standing, and the department, which is what this
        // floor was for. Together they are the glance that says which floor you stepped out on — and the
        // depth is the number that makes the walk back up mean something.
        // Owner, seeing the first cut: "Let's put the elevation next to the elevator... now it is too far
        // from it." It was 30 du off to one side, which is most of a screen — a number that far from the
        // thing it describes is not signage, it is litter. It sits just above the car's own mouth now, over
        // the 🛗 LIFT plate, which is where a building paints a level: on the wall you face when the doors
        // open.
        // #605 · THE PLATE BY THE CAR — depth over department, both painted at signage size.
        //
        // Owner, twice: "Let's put the elevation next to the elevator... now it is too far from it", then
        // "the name of the floor should read next to the elevator... we have them in the buttons let's have
        // them on the level also" and "it is way too small and too far from the elevator".
        //
        // Both complaints are the same fault. The name was pinned 26 du off down the spine at caption size,
        // which is neither next to the lift nor readable at a glance — so it was information the captain had
        // to go and look for, about the one thing they most need to know without looking.
        //
        // They are one plate now, directly over the car's mouth: the depth big because it is the number that
        // decides whether you can walk back up, the department under it because that is what the floor was
        // FOR — and it is what the panel's own buttons promised on the way in.
        // Owner, on why it has to dominate: "It is the where-am-I question answer when you come with the
        // elevator so it is like the most important thing to see."
        //
        // That is the whole brief. A captain steps out of a car onto one of twenty floors cut from identical
        // bones, and the first thing they need is not a console or a corridor — it is WHICH ONE. So the plate
        // sits directly over the car's mouth, in the eye-line of somebody who has just turned around, and it
        // is the largest thing drawn on the floor.
        //
        // And it is modelled on a real reflex, which is why it belongs here rather than in the HUD — owner:
        // "sometimes people get off the elevator at wrong floor so there is this instinct to always check
        // that the floor is correct." A number on the instrument panel would answer the question; a plate on
        // the WALL is the thing you actually look at, because looking at it is what people do.
        // #612 · AND WHETHER YOU CAN BREATHE HERE. Owner, reading the plate: "it should say if the floor is
        // pressurized also" — and, of the gauge: "where here does it say if I consume tanks or have air?"
        //
        // It is the same question twice, and the plate is the right place to answer it: a captain stepping
        // out of a car needs WHERE AM I and CAN I BREATHE in one glance, and the second one decides whether
        // everything they were about to do is affordable. Three lines, one plate, and the air line carries
        // the colour so it reads before it is read.
        //
        // THE WORDS AND THE VERDICT ARE BOTH SuitAir'S. This line first shipped calling
        // UndergroundComplex.HoldsPressure itself and spelling its own two strings — which made it the THIRD
        // place in the game deciding whether a tank is running, after the drain and the hud. Three places
        // that must agree is not redundancy, it is a countdown to a disagreement, and #608 proved it inside a
        // day by adding a fourth way to breathe that only the drain heard about. The plate asks
        // SuitAir.SourceOf of this level and prints SuitAir.PlateLine, so the sign on the wall and the gauge
        // on the suit are physically incapable of saying different things about the same floor.
        double signX = shaftX;
        double signY = shaftY + UndergroundComplex.CorridorHalf;
        SuitAir.Supply floorAir = SuitAir.SourceOf(bodyId, level, insideShelter: false, aboard: false);
        var bigLabels = new List<(float X, float Y, string Text, float Px, int Tone)>
        {
            ((float)signX, (float)(signY + 10.6), UndergroundComplex.DepthPaint(level), 44f, 0),
            ((float)signX, (float)(signY + 7.8), UndergroundComplex.NameOf(bodyId, level), 19f, 0),
            ((float)signX, (float)(signY + 5.4), SuitAir.PlateLine(floorAir), 17f,
                SuitAir.Drawing(floorAir) ? 2 : 1),
        };

        return bigLabels;
    }

    /// <summary>
    /// #694 - NAMES THE FACILITY on the floors you enter it by and NOWHERE ELSE. The law is Core's: a
    /// building says its name where you arrive, and this drew unconditionally until a name that should have
    /// landed once had landed thirteen times and become wallpaper. Lays down: labels.
    /// </summary>
    private static void NameTheFacility(
        List<(float X, float Y, string Text)> labels, string bodyId, int level,
        UndergroundComplex.Kind kind, double shaftX, double shaftY)
    {
        // ── #694 · AND THE FACILITY'S OWN NAME, ON THE FLOORS YOU ENTER IT BY AND NOWHERE ELSE ────────────
        //
        // Owner, standing on B11 of a thirteen-floor site: "every floor has the text 'The Clinic' on it.
        // Some kind of artifact?"
        //
        // It was not an artifact and it was not a leak — this line drew unconditionally, so a name that
        // should have landed once landed thirteen times, and by the third floor it had stopped being a name
        // and become part of the wallpaper. His question IS the finding: a sign a player asks about because
        // they suspect the RENDERER is doing something wrong is a sign that is no longer saying anything.
        //
        // A building says its name where you ENTER it. That is B1, and — where the site has one — the
        // unlisted band's own shaft head, which is the single place in the game where this plate names a
        // different Kind from everything above it: ▣ THE CLINIC under twelve floors of RETENTION 40 YR is
        // #592's whole arithmetic delivered by one sign, and it was being spent on every floor and therefore
        // on none. Everywhere else the plate over the car (B11 · LONG STORAGE) and the department livery
        // already answer which floor this is, which is what they are for.
        //
        // THE LAW IS CORE'S, NOT THIS FILE'S. Which floors you arrive on is a fact about the building — it
        // is BandTop and HasUnlistedBand, the same two calls the shafts and the cards are cut from — and a
        // renderer that answered it here would be one more caller reasoning about a shaft it does not own.
        // HiveInterior asks and draws.
        if (UndergroundComplex.ShowsFacilityPlate(bodyId, level))
        {
            labels.Add(((float)shaftX - 30f, (float)(shaftY + 4.5), UndergroundComplex.TitleOf(kind)));
        }
    }

    /// <summary>
    /// #608 - PLATES THE REFUGES in the plate-by-the-lift's own lettering, TONE 1 for a door you can breathe
    /// behind and TONE 2 where the seal went - the same ink the plate over the lift uses to say your tank is
    /// running, so the two instruments cannot read as a contradiction. Lays down: bigLabels.
    /// </summary>
    private static void PlateTheRefuges(
        List<(float X, float Y, string Text, float Px, int Tone)> bigLabels,
        in UndergroundComplex.FloorPlan floor)
    {
        // ── #608 · AND THE REFUGE'S OWN PLATE, IN THE PLATE-BY-THE-LIFT'S OWN LANGUAGE ───────────────────
        //
        // Smaller than the depth over the car, because the depth is the where-am-I question and this is the
        // where-is-the-air one — but the same KIND of lettering, so a captain crossing a dead floor reads it
        // the way they read a fire exit: without meaning to.
        //
        // TONE 1, WHICH IS THE WHOLE OF THE RECONCILIATION WITH #612. The plate over the lift now answers
        // "can I breathe here" in colour — StencilAir for PRESSURISED, StencilDead for NO ATMOSPHERE — and
        // #612's own rule is that the instruments may never disagree about air. On a dead floor that plate
        // is shouting NO ATMOSPHERE in the dead ink while this one says AIR forty du away, so the two would
        // read as a contradiction unless they are plainly speaking about different things in one shared
        // language. They are: tone 1 means YOU CAN BREATHE HERE, wherever "here" is, and the word REFUGE
        // says the "here" is this room and not this floor. The plate describes the level; this describes a
        // door. Same ink, same claim, different scope — and the hud's AIR: TANKS/ROOM agrees with both,
        // because all three now read TankIsDrawing.
        foreach (UndergroundComplex.Refuge refuge in floor.Refuges)
        {
            // #608 · …AND TONE 2 WHERE THE SEAL WENT. A failed refuge is still drawn — owner, on the fan:
            // "a refuge whose seal has failed must still paint, and must read as failed" — and the two
            // things that change are the two that carry the claim: the word AIR comes off the plate, and the
            // ink becomes the one the plate by the lift is already using to say your tank is running. A room
            // that will not cycle drawn in the relief green would be the exact instrument-lies fault #612
            // exists to prevent, said at the one door on the floor a captain would spend a tank reaching.
            bigLabels.Add(((float)refuge.X, (float)(refuge.Y + UndergroundComplex.RefugeHalfHeight + 3.2),
                UndergroundComplex.RefugeGlyphFor(refuge.State), 26f,
                UndergroundComplex.RefugeStillHolds(refuge.State) ? 1 : 2));
        }
    }
}

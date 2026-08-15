using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #818 · NEVER AN EMPTY FLOOR — what is standing in an ORDINARY room, laid off that room's own walls.
///
/// <para>Owner ruling, 2026-08-11 evening, generalising #817 past the ring: <i>"Same for labs etc spaces…
/// they have chairs and desks and equipment … never ever empty floor"</i>. And then, with the kit spelled
/// out by somebody who has run these rooms for a living: <i>"chairs / tables / vacuum chambers (cough !!),
/// chemical test ventilation boxes [fume hoods], etc where do I put my test tube? Etc furnaces … let's not
/// have any empty storage space unless the space is actually an empty storage."</i></para>
///
/// <h3>The law, and its one stated escape hatch</h3>
///
/// <para><b>A room's floor is evidence of what the room is for.</b> Every carved room a captain can stand in
/// publishes at least one piece of furnishing appropriate to its plate. Bare deck is legal only where the
/// plate SAYS bare — a corridor, a street, the landing pad, a fire recess, and the one room the owner carved
/// the exception for himself: a store that is actually an empty store, which says so on its own plate
/// (<see cref="EmptyStorePlate"/>). An empty floor is a FACT about a room down here, never a default.</para>
///
/// <h3>Why this is a file and not forty lines inside the carve</h3>
///
/// <para>The same reason <see cref="RingOffice"/> is one: the obvious repair is to drop rectangles into
/// <c>AddRoomsAlong</c> at coordinates that look right on the one floor somebody was standing on, and that is
/// this repo's oldest named bug class — <b>unaudited geometry literals</b>. Not one number below is a
/// position. Every one of them is a CLEARANCE or a LENGTH, and the room is handed in, so a chamber and a
/// gallery are furnished by the same sentence and neither was measured off a screenshot.</para>
///
/// <h3>The one law that makes the guards cheap</h3>
///
/// <para>A chamber is not a ring suite: <b>any</b> of its four walls can carry an opening — the corridor door
/// in its face, a fire recess (#822) through either end wall, an en-suite's own leaf (#707) through its back.
/// So this file cannot buy safety with two fixed aisles the way the ring does. It buys it by MEASURING: every
/// fitting stands <see cref="WallClearDu"/> inside a wall, and the free stretches of that inset line are what
/// is left after every published opening has claimed <see cref="OpeningClearDu"/> either side of itself. A
/// wall whose openings eat all of it simply carries nothing, and the next wall round is tried. That is why
/// nothing here has to know which side a chamber's door is on.</para>
///
/// <para>…and <b>the room's own centre stays standable</b>, which is the law #817 learned the expensive way:
/// the A* audit walks to <c>room.X, room.Y</c>, so a fitting laid across that square does not report as
/// furniture, it reports as <i>"something was built through this room"</i> on every floor in the game.</para>
///
/// <h3>Every SOLID is a segment, and that is deliberate</h3>
///
/// <para>Each fitting puts exactly ONE wall segment on the floor, laid on the wall-clear line, collided with
/// exactly as the en-suite's pan and the ring's bench are. That is what keeps this feature affordable: a
/// facility floor carries a few hundred segments and Lab 45 says the sightline is O(walls). Four segments per
/// fitting in fifteen hundred rooms would be a frame budget spent on drawing cupboards.</para>
///
/// <para>#869 · <b>What is DRAWN is a box.</b> The desks and the benches grew a depth
/// (<see cref="FittingDepthDu"/>) because #868 found out the hard way what a fixture without one looks like:
/// the owner stood in a room whose bench was a single stroke and said <i>"the bench is a line"</i>, three
/// paces from a run of shelving he called clear as furniture goes. So a fitting's box reaches into the room
/// for the pen and its solid stays on the line for the body, and the two are published separately for that
/// reason. Nothing about the collision field, the walkability audits or the sightline cost changed when the
/// desks got their 90 cm.</para>
///
/// <para>Pure, in the shape <see cref="RingOffice.Fit"/> is pure: handed a room the generator carved and the
/// holes it cut, it answers what is in it. It has no clock and no opinion about where a room goes.</para>
/// </summary>
public static class ChamberFitting
{
    // ── THE CLEARANCES ────────────────────────────────────────────────────────────────────────────────

    /// <summary>How far inside a wall a fitting stands. The ring's own pier gap
    /// (<see cref="RingOffice.PierClearDu"/>) and not a second number: a bench against a wall is a bench
    /// against a wall, whichever room it is in.</summary>
    public const double WallClearDu = RingOffice.PierClearDu;

    /// <summary>How much of that line an opening keeps clear either side of itself — the ring's own door
    /// clearance (<see cref="RingOffice.DoorClearDu"/>), which every guard in this house about furniture and
    /// doors is already written against. A doorway, a fire recess and an en-suite leaf are all measured with
    /// it, because a body coming through any of them needs the same square.</summary>
    public const double OpeningClearDu = RingOffice.DoorClearDu;

    /// <summary>How much clear floor is kept round the room's own centre — the square the A* audit stands a
    /// body on to say the room was reached at all. <see cref="RingOffice.RoomCentreClearDu"/>, for the reason
    /// that constant exists.</summary>
    public const double CentreClearDu = RingOffice.RoomCentreClearDu;

    /// <summary>The shortest stretch of wall worth standing anything against. Below this a fitting is a
    /// stub, and a stub is worse than an honest bare wall.</summary>
    public const double MinFittingDu = 2.0;

    /// <summary>How far a seat stands off the worktop it belongs to. <see cref="RingOffice.ChairSetbackDu"/>
    /// — one number for every chair in the building.</summary>
    public const double SeatSetbackDu = RingOffice.ChairSetbackDu;

    /// <summary>
    /// #869 · HOW DEEP A DESK IS DOWN HERE — and why the depth grows INWARD and nothing else moves.
    ///
    /// <para>Owner, 2026-08-13, from his own electric desk: <i>"my table (electric) is about 160 cm wide,
    /// about 90 cm deep"</i>. Ninety centimetres is <see cref="RingOffice.WorktopDepthDu"/>, the number #868
    /// gave the ring's own worktop, and it is the same number here because a desk is a fact about a BODY
    /// rather than about a module.</para>
    ///
    /// <para>The box is grown from the wall-clear line <b>into the room</b>, so the fitting's BACK stays
    /// exactly where the segment always stood. That is not tidiness, it is the whole safety of this change:
    /// every clearance in this file was measured to that line, and a depth grown the other way would push a
    /// bench 0.65 du nearer the very doorway it was laid clear of. The SOLID stays one segment on that same
    /// line, so not one wall in the building moves and the collision field, the sightline cost and every
    /// walkability audit are untouched. The depth is a PICTURE (<c>DeckPlan.FurnitureSpot</c>), and #868's
    /// finding was that a picture is the thing furniture down here never had.</para>
    /// </summary>
    public const double FittingDepthDu = RingOffice.WorktopDepthDu;

    // ── THE LENGTHS ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>#869 · How much frontage ONE PERSON'S DESK gets — <see cref="RingOffice.WorktopRunDu"/>, the
    /// owner's own 160 cm. The one length in this file not derived from the room module, because a desk is
    /// not a fact about the room it is standing in.</summary>
    public static double DeskRunDu => RingOffice.WorktopRunDu;

    /// <summary>How long a laboratory bench runs, at most. The room module's own width
    /// (<see cref="UndergroundComplex.RoomWidthDu"/>) less the two clearances it stands between, so a bench
    /// grows with the chamber and is never measured off one floor's screenshot.</summary>
    public static double BenchRunDu => UndergroundComplex.RoomWidthDu - (2 * WallClearDu) - MinFittingDu;

    /// <summary>How long a run of racking is. The same, because a bay of shelving and a bench are the same
    /// object at this scale: a run of solid against a wall.</summary>
    public static double RackRunDu => BenchRunDu;

    /// <summary>A machine, a fume hood, a furnace, a vacuum vessel, a filing bank — the SHORT fittings, the
    /// ones you stand at rather than sit along. Half a bench, so the two read as different objects on a plan
    /// without a second number being invented.</summary>
    public static double UnitRunDu => BenchRunDu / 2.0;

    /// <summary>How many fittings one chamber gets, at most. A cap and not a target, and the number is
    /// small on purpose — see the class summary on segments. Three is enough for a room to answer <i>what
    /// is this for</i> at a glance and cheap enough to do it in fifteen hundred rooms.</summary>
    public const int MaxFittingsPerRoom = 3;

    // ── WHAT KIND OF ROOM THIS IS ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #818 · Which trade's kit a room gets. Off the room's PLATE first, then the FLOOR's department, then
    /// the site's own register — the ladder <see cref="RingOffice.DressingFor"/> walks one scale up, and for
    /// its reason: the plate is the only thing that says what a room is, and a room plated COLD STORE with
    /// four workstations in it is the sim and the sentence disagreeing.
    /// </summary>
    public enum Kit
    {
        /// <summary>Nothing at all — and it is a fact about the room rather than an omission. A gallery
        /// nobody built, and a store that is actually an empty store.</summary>
        None,

        /// <summary>Benches with racked glassware, a fume hood against a wall, a vacuum chamber, a furnace.
        /// The owner's own list, from his own decade of running these rooms.</summary>
        Laboratory,

        /// <summary>Racking. Bays of it, which is the whole of what a long store is.</summary>
        Store,

        /// <summary>Machinery. A plant floor is a floor with machines bolted to it.</summary>
        Plant,

        /// <summary>Desks, filing and shelving. What an administration chamber has.</summary>
        Office,

        /// <summary>An examination bench, a worktop, shelving. The clinic's own kit — a back room with the
        /// trade's tools in it, which is the general form of this whole feature.</summary>
        Clinic,

        /// <summary>Shelving and a bench, and that is the whole of it. #817's <c>Plain</c> dressing, one
        /// scale up: cheap is fine, empty is not.</summary>
        Trade,
    }

    /// <summary>
    /// #818 · WHAT A STORE THAT IS ACTUALLY AN EMPTY STORE SAYS ON ITS DOOR.
    ///
    /// <para>The owner's own escape hatch, quoted: <i>"let's not have any empty storage space unless the
    /// space is actually an empty storage."</i> So the bare floor stops being a default and becomes a
    /// CLAIM — one the building makes, in its own stencil voice, on the one wall a captain reads before
    /// they walk in.</para>
    ///
    /// <para>§13.8 holds: it says what the room is (a store, with nothing in it) and not one word about what
    /// the facility is for.</para>
    /// </summary>
    public const string EmptyStorePlate = "STORE — EMPTY";

    /// <summary>Is this the room the law lets off? Off the plate, which is the only thing that can say so —
    /// asked here so the carve, the sweep and the guard read one sentence.</summary>
    public static bool IsEmptyStore(string plate) =>
        string.Equals(plate, EmptyStorePlate, StringComparison.Ordinal);

    /// <summary>#818 · How often a store floor's chamber is actually empty. Rare enough that a bare room is
    /// a small event and common enough that the plate is not a curiosity nobody ever meets.</summary>
    public const double EmptyStoreChance = 0.18;

    /// <summary>Which department this floor belongs to, or null where it belongs to none — the galleries
    /// nobody built and the band nobody listed, both of which answer <c>NO PLATE</c>
    /// (<see cref="UndergroundComplex.NameOf"/>) and neither of which has a department to take a kit
    /// from.</summary>
    public static string? DepartmentOn(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        if (level >= 0
            || UndergroundComplex.IsFound(bodyId, level)
            || UndergroundComplex.IsUnlisted(bodyId, level))
        {
            return null;
        }
        return UndergroundComplex.DepartmentOf(bodyId, level);
    }

    /// <summary>Does this floor keep stock? The one question the empty-store clause is allowed to be asked
    /// of, so the exception cannot leak onto a laboratory.</summary>
    public static bool StoresOn(string? department) =>
        department is "LONG STORAGE" or "DEEP STORAGE";

    /// <summary>#818 · Is this a LABORATORIES floor — the one the owner was standing on, and the one the
    /// posters hang on (#853)? Asked here rather than compared against a literal at three call sites.</summary>
    public static bool LabsOn(string? department) => department is "LABORATORIES";

    /// <summary>
    /// Which kit this room takes. The plate first (it is the only thing that says what THIS room is), then
    /// the floor's department, then the site's own trade — so a room keeps its character on a floor whose
    /// department nobody wrote down, which is every floor of the found band's neighbours and the whole of
    /// the band nobody listed.
    /// </summary>
    /// <param name="plate">What is stencilled on it. Empty in the galleries.</param>
    /// <param name="department">The floor's own plate, or null where it has none.</param>
    /// <param name="kind">What kind of place the site is, for the rooms neither of the above can answer.</param>
    public static Kit KitFor(string plate, string? department, UndergroundComplex.Kind kind)
    {
        ArgumentNullException.ThrowIfNull(plate);

        // The room the owner carved the exception for. Asked FIRST, and before anything else can dress it:
        // a store that says it is empty and then shows you three bays of racking is the plate and the floor
        // disagreeing, which is the bug class this ladder was written to be incapable of.
        if (IsEmptyStore(plate))
        {
            return Kit.None;
        }

        // #677 · A GALLERY IS NOT A ROOM WITH A PLATE ON IT. Nothing down there was ever labelled by
        // anybody, and furniture is the loudest possible statement that somebody fitted this place out.
        // The doors were refused on exactly this reasoning and the benches are refused on it too.
        if (plate.Length == 0)
        {
            return Kit.None;
        }

        // ── THE TWO PLATES THAT OVERRULE THE FLOOR THEY ARE ON ────────────────────────────────────────
        //
        // A room is stocked or it is plant however its department is organised: a COLD STORE on a
        // laboratories floor is a store, and POWER — LOCKED OUT is a machine room wherever it stands. These
        // two are asked first because they are the two the owner named himself, and because they are the two
        // whose furniture would be an outright lie if the floor won the argument — a bay of racking is not
        // something you mistake for a bench.
        if (Says(plate, "STORE") || Says(plate, "STORAGE") || Says(plate, "HOLD")
            || Says(plate, "BONDED") || Says(plate, "LOADING") || Says(plate, "DUPLICATES")
            || Says(plate, "MICROFORM") || Says(plate, "RETENTION") || Says(plate, "INDEX"))
        {
            return Kit.Store;
        }
        if (Says(plate, "POWER") || Says(plate, "PLANT"))
        {
            return Kit.Plant;
        }

        // ── …AND THEN THE FLOOR, WHICH IS WHAT THE OWNER STATED THE LAW IN ────────────────────────────
        //
        // "LABORATORIES floors' chambers get benches with racked glassware, fume hoods against a wall,
        // vacuum chambers, furnaces." The department is what a FLOOR is for and the plate register is what a
        // SITE calls its rooms, and the two are seeded independently — so a laboratories floor of a clinic
        // has rooms plated RECOVERY 2 on it, and dressing those as a ward would leave the department the
        // owner named as the one department in the building with no laboratory in it.
        //
        // §13.8 is untouched by this: not one plate changes, and a fume hood says what a fixture is and
        // nothing about what the facility is for.
        Kit byFloor = department switch
        {
            "LABORATORIES" => Kit.Laboratory,
            "LONG STORAGE" or "DEEP STORAGE" or "ARCHIVE" => Kit.Store,
            "PLANT" => Kit.Plant,
            "ADMINISTRATION" => Kit.Office,
            "ISOLATION" => Kit.Clinic,
            "UNMARKED" => Kit.Trade,
            _ => Kit.None,
        };
        if (byFloor != Kit.None)
        {
            return byFloor;
        }

        // ── …THEN THE ROOM'S OWN PLATE ────────────────────────────────────────────────────────────────
        //
        // Reached on every floor that has no department at all: the band nobody listed (#592), whose rooms
        // DO carry plates. There the plate is the only thing in the building that says what a room is for,
        // and a back room down there still gets the tools of whatever is done in it.
        if (Says(plate, "ASSAY") || Says(plate, "PATTERN") || Says(plate, "CONTINUITY")
            || Says(plate, "CALIBRATION") || Says(plate, "SUBJECT PREP") || Says(plate, "QUARANTINE"))
        {
            return Kit.Laboratory;
        }
        if (Says(plate, "MEDICAL") || Says(plate, "THEATRE") || Says(plate, "RECOVERY")
            || Says(plate, "MORTUARY") || Says(plate, "AFTERCARE") || Says(plate, "PHARMACY")
            || Says(plate, "REHABILITATION") || Says(plate, "HOLDING"))
        {
            return Kit.Clinic;
        }
        if (Says(plate, "RECORDS") || Says(plate, "ARCHIVE") || Says(plate, "OFFICE")
            || Says(plate, "PAYROLL") || Says(plate, "SCHEDULING")
            || Says(plate, "CLERKS") || Says(plate, "AUDIT") || Says(plate, "LEDGER")
            || Says(plate, "REGISTRAR") || Says(plate, "MINUTES") || Says(plate, "COMMITTEE")
            || Says(plate, "SIGNATURES") || Says(plate, "RETURNS") || Says(plate, "BOARD")
            || Says(plate, "ORDERS") || Says(plate, "APPROPRIATIONS") || Says(plate, "DEPUTATIONS")
            || Says(plate, "ATTENDANCE") || Says(plate, "LIST") || Says(plate, "REVIEW")
            || Says(plate, "REGISTER") || Says(plate, "MANIFEST") || Says(plate, "CUSTOMS"))
        {
            return Kit.Office;
        }

        // …and then the site's own trade, for a room whose plate names nothing this ladder knows: a back
        // room in a clandestine site still gets the tools of whatever is done there.
        return kind switch
        {
            UndergroundComplex.Kind.Laboratory => Kit.Laboratory,
            UndergroundComplex.Kind.BlackClinic => Kit.Clinic,
            UndergroundComplex.Kind.RecordsAnnex => Kit.Store,
            UndergroundComplex.Kind.TransitStation => Kit.Store,
            UndergroundComplex.Kind.ProcessingDepot => Kit.Office,
            UndergroundComplex.Kind.HeadOffice => Kit.Office,
            _ => Kit.Trade,
        };
    }

    private static bool Says(string plate, string word) =>
        plate.Contains(word, StringComparison.Ordinal);

    // ── WHAT IT SAYS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The laboratory bench, and the answer to <i>where do I put my test tube?</i> — the rack is
    /// part of the bench, because on this plan it is.</summary>
    public const string BenchPlate = "🧪 BENCH · RACKED GLASSWARE";

    /// <summary>The chemical test ventilation box, in the words a building writes on one.</summary>
    public const string FumeHoodPlate = "🌬 FUME HOOD · SASH DOWN";

    /// <summary>…and the vessel the owner asked for by name, with a cough.</summary>
    public const string VacuumChamberPlate = "🛢 VACUUM CHAMBER · VENT BEFORE OPENING";

    /// <summary>The furnace.</summary>
    public const string FurnacePlate = "🔥 FURNACE · HOT WORK PERMIT REQUIRED";

    /// <summary>A run of racking in a store.</summary>
    public const string RackingPlate = "🗄 RACKING · BAYS THIS SIDE";

    /// <summary>A machine on a plant floor. It says what it is and nothing about what it is running.</summary>
    public const string MachineryPlate = "⚙ MACHINERY · DO NOT LEAN";

    /// <summary>The bank of filing in an administration chamber.</summary>
    public const string FilingPlate = "🗃 FILING · A–M";

    /// <summary>The clinic's own bench.</summary>
    public const string ExaminationPlate = "🛏 EXAMINATION BENCH";

    /// <summary>…and its worktop.</summary>
    public const string WorktopPlate = "🧼 WORKTOP · SCRUB DOWN AFTER USE";

    /// <summary>What a seat at one of these is labelled, with the verb on it (#783). The office chair's own
    /// wording is a fact about an office; this is a stool at a worktop.</summary>
    public const string StoolPlate = RingOffice.Glyph + " A STOOL AT THE BENCH — SIT DOWN";

    /// <summary>
    /// #869 · WHAT A LABORATORY SEATS INSTEAD — the Salli of the future.
    ///
    /// <para>Owner, 2026-08-13, listing what his own desk is set up to be worked at from: <i>"to work either
    /// with office chair, Salli standing (lab) chair or by standing while using the table."</i> A lab does
    /// not seat you the way an office does, and the difference is one word on one plate: a bench is worked
    /// at from a saddle, perched rather than sat, which is why the plate keeps #783's verb and changes the
    /// NOUN and nothing else.</para>
    ///
    /// <para>Nobody in-world remarks on it, here as everywhere in this issue. It is simply what the room has.</para>
    /// </summary>
    public const string SaddleStoolPlate = RingOffice.Glyph + " A SADDLE STOOL AT THE BENCH — SIT DOWN";

    /// <summary>
    /// #869 · WHICH SEAT THIS ROOM'S TRADE SITS YOU ON — one sentence, so the renderer, the press and the
    /// guard cannot each answer it their own way.
    ///
    /// <para>A laboratory gets the saddle (<see cref="SaddleStoolPlate"/>); an administration chamber gets
    /// the OFFICE CHAIR the ring has been publishing since #817 (<see cref="RingOffice.FreeChairPlate"/>) and
    /// not a second wording of it, because a chair at a desk is a chair at a desk whichever building it is
    /// in; everything else keeps the stool it has had since #818.</para>
    /// </summary>
    public static string SeatPlateFor(Kit kit) => kit switch
    {
        Kit.Laboratory => SaddleStoolPlate,
        Kit.Office => RingOffice.FreeChairPlate,
        _ => StoolPlate,
    };

    /// <summary>Where you are, when you are sitting on one.</summary>
    public const string Setting = "A stool at a worktop, somewhere in the building";

    /// <summary>#818 · Sitting down at a bench in a room you do not work in. #783's law kept — the FIRST
    /// clause confirms the state change, and the room names itself, because the plate beside the door is the
    /// only thing that tells one poured box from another.</summary>
    /// <param name="plate">The room's own stencil.</param>
    public static string TookAStoolLine(string plate) =>
        string.IsNullOrEmpty(plate)
            ? "You pull a stool up to the worktop and sit down. Nothing on this floor is signed, and the "
                + "surface in front of you has not been wiped in a very long time."
            : $"You pull a stool up to the worktop in {plate} and sit down. The bench is laid out for work "
                + "somebody expected to come back and finish.";

    /// <summary>Who you are, on the docked strip, while you are sitting on one.</summary>
    public const string SeatPlate = RingOffice.Glyph + " AT A WORKTOP";

    /// <summary>The stool, as an <see cref="Encounter.Scene"/>. The move IDS are
    /// <see cref="SittingAlone.Wait"/> and <see cref="SittingAlone.Stand"/> and deliberately not new ones —
    /// the ring chair and the park bench made the same choice for the same reason (a saved game and a guard
    /// both key on the id). Only the labels and the words are the workshop's.</summary>
    /// <param name="plate">The room's own stencil, which the opening line names.</param>
    public static Encounter.Scene TheStool(string plate) => new(
        "chamber:stool",
        SeatPlate,
        Setting,
        TookAStoolLine(plate),
        [
            new(SittingAlone.Wait, RingOffice.WaitLabel),
            new(SittingAlone.Stand, RingOffice.StandLabel, Says: RingOffice.StoodUpLine),
        ]);

    /// <summary>Where a chamber stool's watch-scoped state is keyed from, so a stool in a laboratory and a
    /// chair in a park-view suite on the same floor can never share a wait counter.
    /// <see cref="RingOffice.ApproachOrdinalBase"/>'s own reason, one building along, and far enough past
    /// the ring's block that no floor will ever grow into the gap.</summary>
    public const int ApproachOrdinalBase = 20000;

    /// <summary>This stool's ordinal for the approach roll.</summary>
    /// <param name="roomIndex">Which published room it stands in.</param>
    /// <param name="seatIndex">Its ordinal in that room.</param>
    public static int ApproachOrdinal(int roomIndex, int seatIndex) =>
        ApproachOrdinalBase + (roomIndex * 64) + seatIndex;

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return EmptyStorePlate;
        yield return BenchPlate;
        yield return FumeHoodPlate;
        yield return VacuumChamberPlate;
        yield return FurnacePlate;
        yield return RackingPlate;
        yield return MachineryPlate;
        yield return FilingPlate;
        yield return ExaminationPlate;
        yield return WorktopPlate;
        yield return StoolPlate;
        yield return SaddleStoolPlate;
        yield return SeatPlate;
        yield return Setting;
        yield return TookAStoolLine("");
    }

    // ── THE LAYOUT ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>One piece of the kit, before it has been given a wall to stand against.</summary>
    /// <param name="DepthDu">#869 · How far it reaches INTO the room from the wall-clear line, for the pen.
    /// Zero is the segment this file has laid since #818 — see <see cref="FittingDepthDu"/> for why a depth
    /// is a picture and never a wall.</param>
    private readonly record struct Piece(
        RingOffice.Fitting Kind, double RunDu, string Plate, bool Seats, double DepthDu = 0.0);

    /// <summary>
    /// #864 · ONE FREE STRETCH OF ONE WALL, in the room's own reading — what is left of an inset line once
    /// every published opening and the audit's own square have taken their clearance out of it.
    ///
    /// <para>Published (rather than kept private to <see cref="Fit"/>) because the incident board
    /// (<see cref="IncidentBoard"/>) has to hang on a chamber wall under exactly the law the furniture is
    /// laid under, and a second author for "where is there wall left" is the shape of bug this house has
    /// paid for repeatedly: two placers that agree today and a doorway that moves tomorrow.</para>
    /// </summary>
    /// <param name="Edge">0 the bottom wall, 1 the top, 2 the left, 3 the right.</param>
    /// <param name="Lo">Where the free stretch starts, on the wall's own axis.</param>
    /// <param name="Hi">…and where it ends.</param>
    /// <param name="Fixed">The inset line's across-coordinate — <see cref="WallClearDu"/> inside the wall.</param>
    /// <param name="Inward">+1 or −1: which way is into the room from that line.</param>
    public readonly record struct WallRun(int Edge, double Lo, double Hi, double Fixed, double Inward)
    {
        /// <summary>Does this wall run along X? True of the bottom and top walls.</summary>
        public bool Horizontal => Edge < 2;

        /// <summary>How much wall there is.</summary>
        public double Length => Hi - Lo;

        /// <summary>One point on the line, in the surface's own coordinates.</summary>
        public (double X, double Y) At(double along) =>
            Horizontal ? (along, Fixed) : (Fixed, along);

        /// <summary>The middle of it — the furthest point of this stretch from whatever took the ends.</summary>
        public (double X, double Y) Middle => At((Lo + Hi) / 2.0);
    }

    /// <summary>
    /// #818/#864 · EVERY STRETCH OF THIS ROOM'S OWN WALLS LONG ENOUGH TO STAND SOMETHING AGAINST, longest
    /// first.
    ///
    /// <para>The measured-wall law of the class summary, as one function: every fitting stands
    /// <see cref="WallClearDu"/> inside a wall, and the free stretches of that inset line are what is left
    /// after every published opening has claimed <see cref="OpeningClearDu"/> either side of itself and the
    /// audit's own square (<see cref="CentreClearDu"/>) has claimed its. A wall whose openings eat all of it
    /// simply carries nothing, which is why nothing in this file has to know which side a chamber's door is
    /// on.</para>
    /// </summary>
    /// <param name="room">The room as published.</param>
    /// <param name="openings">Every hole a body can pass, including the ones that are not ways out. A caller
    /// that hands over more than this room's own loses nothing: an opening in somebody else's wall is too far
    /// away to claim any of this line.</param>
    /// <param name="depthDu">#869 · How far whatever stands here will reach INTO the room. Zero for a thing
    /// bolted flat to the wall (the incident board); <see cref="FittingDepthDu"/> for the furniture.
    ///
    /// <para>It is a parameter and not an assumption because it changes the answer, and getting that wrong
    /// is the one way this issue could have moved a bench into a doorway: the clearance to an opening in
    /// THIS wall is unaffected (the box grows away from it), and the clearance to an opening in a
    /// NEIGHBOURING wall shrinks by exactly the depth. Measured box-to-opening rather than line-to-opening,
    /// so the number the guard asks for and the number the placer honoured are the same number.</para></param>
    public static IReadOnlyList<WallRun> FreeWallRuns(
        in UndergroundComplex.Room room, IReadOnlyList<SurfaceLayout.Doorway> openings,
        double depthDu = 0.0)
    {
        ArgumentNullException.ThrowIfNull(openings);

        double x0 = room.X0 + WallClearDu, x1 = room.X1 - WallClearDu;
        double y0 = room.Y0 + WallClearDu, y1 = room.Y1 - WallClearDu;
        if (x1 - x0 < MinFittingDu || y1 - y0 < MinFittingDu)
        {
            return [];
        }

        // Edge 0 is the bottom wall's line and points INTO the room (+y); 1 the top (−y); 2 the left (+x);
        // 3 the right (−x). Everything is written on (along, fixed, inward) so no layout in this file ever
        // asks which wall it is standing against — the same trick RingOffice.Frame plays, at the one scale
        // where a chamber has no privileged side to play it about.
        var free = new List<WallRun>();
        for (int edge = 0; edge < 4; edge++)
        {
            bool horizontal = edge < 2;
            double fixedAt = edge switch { 0 => y0, 1 => y1, 2 => x0, _ => x1 };
            double inward = edge is 0 or 2 ? +1.0 : -1.0;
            double lo = horizontal ? x0 : y0, hi = horizontal ? x1 : y1;

            // #869 · The ACROSS-RANGE of whatever will stand here: the line itself where the thing is flat
            // against the wall, and the line plus its depth INTO the room where it is furniture.
            double frontAt = fixedAt + (inward * depthDu);
            double boxLo = Math.Min(fixedAt, frontAt), boxHi = Math.Max(fixedAt, frontAt);

            var blocked = new List<(double Lo, double Hi)>();
            foreach (SurfaceLayout.Doorway hole in openings)
            {
                double hx0 = Math.Min(hole.X1, hole.X2), hx1 = Math.Max(hole.X1, hole.X2);
                double hy0 = Math.Min(hole.Y1, hole.Y2), hy1 = Math.Max(hole.Y1, hole.Y2);

                // How far the DEEPEST part of that box is from the opening, ACROSS the line, and how much of
                // the line that leaves inside the clearance circle. Exact for an axis-aligned opening against
                // an axis-aligned box, which is every opening and every fitting in this building.
                double across = horizontal
                    ? Math.Max(Math.Max(hy0 - boxHi, boxLo - hy1), 0.0)
                    : Math.Max(Math.Max(hx0 - boxHi, boxLo - hx1), 0.0);
                if (across >= OpeningClearDu)
                {
                    continue;
                }
                double reach = Math.Sqrt((OpeningClearDu * OpeningClearDu) - (across * across));
                blocked.Add(horizontal ? (hx0 - reach, hx1 + reach) : (hy0 - reach, hy1 + reach));
            }

            // …and the square the audit stands on, which is an obstacle of exactly the same shape.
            double centre = horizontal ? room.Y : room.X;
            double centreAcross = Math.Max(Math.Max(centre - boxHi, boxLo - centre), 0.0);
            if (centreAcross < CentreClearDu)
            {
                double reach = Math.Sqrt((CentreClearDu * CentreClearDu) - (centreAcross * centreAcross));
                double at = horizontal ? room.X : room.Y;
                blocked.Add((at - reach, at + reach));
            }

            foreach ((double Lo, double Hi) span in Remaining(lo, hi, blocked))
            {
                if (span.Hi - span.Lo >= MinFittingDu)
                {
                    free.Add(new WallRun(edge, span.Lo, span.Hi, fixedAt, inward));
                }
            }
        }

        // The longest stretch first, so the piece that wants a wall gets the best one there is. Ties break on
        // the edge's own ordinal, which is deterministic and therefore reproducible on every visit.
        free.Sort((a, b) =>
        {
            int byLength = b.Length.CompareTo(a.Length);
            return byLength != 0 ? byLength : a.Edge.CompareTo(b.Edge);
        });
        return free;
    }

    /// <summary>
    /// #818 · FURNISH ONE ROOM. Handed the box the generator carved and every hole it cut in it, it answers
    /// what is standing in there.
    /// </summary>
    /// <param name="room">The room as published, with its own ways out.</param>
    /// <param name="kit">Which trade's kit — see <see cref="KitFor"/>.</param>
    /// <param name="openings">Every hole in this room's walls a body can pass, INCLUDING the ones that are
    /// not ways out: an en-suite's leaf is a door into a cell and a captain still has to be able to reach it.
    /// A caller that hands over more than this room's own openings loses nothing — an opening in somebody
    /// else's wall is too far away to claim any of this line.</param>
    /// <param name="ordinal">Which room of THIS KIT this is on the floor, counted up as the floor is
    /// furnished. It is what walks the laboratory's specialist rotation round (see <see cref="KitOf"/>), and
    /// it is a COUNT rather than a seed for one reason worth the parameter: a seeded pick leaves the whole
    /// kit's appearance on a floor to chance, and a laboratories floor of nine chambers with no furnace on it
    /// anywhere is exactly the finding this issue exists to prevent. Watched happen — fifteen floors, one
    /// piece missing each. Counting guarantees that any three furnished lab chambers show the whole kit
    /// between them.</param>
    public static RingOffice.Furnishing Fit(
        in UndergroundComplex.Room room, Kit kit,
        IReadOnlyList<SurfaceLayout.Doorway> openings, int ordinal)
    {
        ArgumentNullException.ThrowIfNull(openings);

        if (kit == Kit.None)
        {
            return RingOffice.Furnishing.Empty;
        }

        // ── THE FOUR INSET LINES, AND WHAT IS LEFT OF THEM ────────────────────────────────────────────
        //
        // One function since #864 published it (so the incident board hangs under the very law the furniture
        // is laid under), longest stretch first, so the piece that wants a wall gets the best one there is.
        // An empty answer is a box with nothing left in it after the clearances, or a room whose every wall
        // is a doorway's clearance — reported by the sweep as the violation it is, rather than quietly
        // wedging a bench into a doorway.
        // …and measured for the DEEPEST piece any kit holds, so one span list serves every piece in it. A
        // shallow fitting laid on a span cut for a deep one loses a few du of wall it could have had; a deep
        // one laid on a span cut for a line would be standing in a doorway.
        IReadOnlyList<WallRun> free = FreeWallRuns(in room, openings, FittingDepthDu);
        if (free.Count == 0)
        {
            return RingOffice.Furnishing.Empty;
        }

        var fixtures = new List<RingOffice.Fixture>();
        var chairs = new List<RingOffice.Chair>();
        var solids = new List<SurfaceLayout.Wall>();

        IReadOnlyList<Piece> pieces = KitOf(kit, ordinal);
        int slot = 0;
        for (int p = 0; p < pieces.Count && slot < free.Count && fixtures.Count < MaxFittingsPerRoom; p++)
        {
            Piece piece = pieces[p];
            WallRun wall = free[slot++];
            (double spanLo, double spanHi, double fixedAt, double inward) =
                (wall.Lo, wall.Hi, wall.Fixed, wall.Inward);

            double run = Math.Min(piece.RunDu, spanHi - spanLo);
            double mid = (spanLo + spanHi) / 2.0;
            double a = mid - (run / 2.0), b = mid + (run / 2.0);
            bool horizontal = wall.Horizontal;

            // ── #869 · THE BOX IS A PICTURE; THE SOLID IS STILL ONE SEGMENT ───────────────────────────
            //
            // A fitting with a DEPTH is drawn as the rectangle it is — #868's finding, said one building
            // along: an outline round a dark gap reads as somewhere you could stand, and only a filled box
            // reads as furniture. The depth grows INTO the room off the wall-clear line, so the fitting's
            // back stays exactly where the segment always stood and not one clearance this file already
            // measured moves (see FittingDepthDu), and it is dropped outright where the deeper box would
            // reach a doorway or the audit's own square.
            //
            // The SOLID stays the single segment #818 has laid since this feature shipped — the en-suite
            // pan's own idiom, and the reason the feature is affordable: four segments per fitting in
            // fifteen hundred rooms is a frame budget spent drawing cupboards (Lab 45: sightline is O(walls)).
            double frontAt = fixedAt + (inward * piece.DepthDu);
            double acrossLo = Math.Min(fixedAt, frontAt), acrossHi = Math.Max(fixedAt, frontAt);

            (double fx0, double fy0, double fx1, double fy1) = horizontal
                ? (a, acrossLo, b, acrossHi)
                : (acrossLo, a, acrossHi, b);

            fixtures.Add(new RingOffice.Fixture(
                piece.Kind, fx0, fy0, fx1, fy1, piece.Plate,
                piece.Seats ? RingOffice.Seating.OneSide : RingOffice.Seating.None));
            solids.Add(horizontal
                ? new SurfaceLayout.Wall(a, fixedAt, b, fixedAt, true)
                : new SurfaceLayout.Wall(fixedAt, a, fixedAt, b, true));

            if (!piece.Seats || chairs.Count > 0)
            {
                continue;
            }

            // ── THE STOOL, a pace off the worktop on the room's side of it.
            //
            // Owner's kit opens with the word "chairs", and a bench you cannot sit at is a bench in a
            // catalogue. It is laid clear of the segment by more than a body's radius so #820's snap can put
            // the captain ON it, and shifted along the run if the room's own centre is where it wanted to
            // stand — the audit's square is defended for a seat exactly as it is for a solid, because a body
            // parked on it is the same report.
            double seatAlong = mid;
            double seatAcross = fixedAt + (inward * SeatSetbackDu);
            (double sx, double sy) = horizontal ? (seatAlong, seatAcross) : (seatAcross, seatAlong);
            if (Near(sx, sy, room.X, room.Y, CentreClearDu))
            {
                seatAlong = mid + (((spanHi - mid) > (mid - spanLo) ? +1.0 : -1.0) * CentreClearDu * 2.0);
                seatAlong = Math.Clamp(seatAlong, spanLo, spanHi);
                (sx, sy) = horizontal ? (seatAlong, seatAcross) : (seatAcross, seatAlong);
            }
            if (Near(sx, sy, room.X, room.Y, CentreClearDu))
            {
                continue;   // nowhere in this room to sit that is not the square the audit walks to
            }

            chairs.Add(new RingOffice.Chair(
                chairs.Count, sx, sy,
                horizontal ? 0.0 : -inward, horizontal ? -inward : 0.0,
                room.Plate));
        }

        return new RingOffice.Furnishing(fixtures, chairs, solids, []);
    }

    /// <summary>How far a box is from a point. Shared with <see cref="IncidentBoard"/>, which has to keep a
    /// board off the very furniture this file stood against the wall.</summary>
    internal static double BoxToPoint(double x0, double y0, double x1, double y1, double px, double py)
    {
        double dx = Math.Max(Math.Max(x0 - px, px - x1), 0.0);
        double dy = Math.Max(Math.Max(y0 - py, py - y1), 0.0);
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>Is this point inside a keep-out square's radius of that one?</summary>
    private static bool Near(double ax, double ay, double bx, double by, double radius) =>
        ((ax - bx) * (ax - bx)) + ((ay - by) * (ay - by)) < radius * radius;

    /// <summary>What is left of a line once every blocked stretch has been taken out of it. Sorted and
    /// swept with a cursor that only ever moves forward — #587's law, which this house has paid for once and
    /// does not intend to pay for again on a new list.</summary>
    private static List<(double Lo, double Hi)> Remaining(
        double lo, double hi, List<(double Lo, double Hi)> blocked)
    {
        blocked.Sort((a, b) => a.Lo.CompareTo(b.Lo));
        var open = new List<(double Lo, double Hi)>();
        double cursor = lo;
        foreach ((double bLo, double bHi) in blocked)
        {
            if (bLo > cursor)
            {
                open.Add((cursor, Math.Min(bLo, hi)));
            }
            cursor = Math.Max(cursor, bHi);
            if (cursor >= hi)
            {
                return open;
            }
        }
        open.Add((cursor, hi));
        return open;
    }

    /// <summary>
    /// #818 · THE KIT — what a room of this trade actually holds, in the order it wants a wall.
    ///
    /// <para>The laboratory's is a ROTATION and not a fixed three, and that is the owner's list being taken
    /// seriously: <i>"chairs / tables / vacuum chambers … fume hoods … where do I put my test tube? Etc
    /// furnaces"</i> is a description of a FLOOR, not of one room. Every lab chamber gets its bench and a
    /// stool; which two of the specialist pieces stand beside it is walked round the list ONE ROOM AT A TIME,
    /// so a laboratories floor shows the whole kit across its chambers and no single room is a showroom.</para>
    /// </summary>
    private static IReadOnlyList<Piece> KitOf(Kit kit, int ordinal)
    {
        switch (kit)
        {
            case Kit.Laboratory:
            {
                Piece[] specialists =
                [
                    new(RingOffice.Fitting.FumeHood, UnitRunDu, FumeHoodPlate, false),
                    new(RingOffice.Fitting.VacuumChamber, UnitRunDu, VacuumChamberPlate, false),
                    new(RingOffice.Fitting.Furnace, UnitRunDu, FurnacePlate, false),
                ];
                int start = ((ordinal % specialists.Length) + specialists.Length) % specialists.Length;
                return
                [
                    // #869 · A bench is 90 cm deep because a person is. It keeps its RUN — a bench runs the
                    // wall, and that is what makes it a bench rather than a desk — and takes the human-true
                    // depth, so what the pen fills is the surface somebody actually stands at.
                    new(RingOffice.Fitting.LabBench, BenchRunDu, BenchPlate, true, FittingDepthDu),
                    specialists[start],
                    specialists[(start + 1) % specialists.Length],
                ];
            }

            case Kit.Store:
                return
                [
                    new(RingOffice.Fitting.Racking, RackRunDu, RackingPlate, false),
                    new(RingOffice.Fitting.Racking, RackRunDu, "", false),
                    new(RingOffice.Fitting.Racking, UnitRunDu, "", false),
                ];

            case Kit.Plant:
                return
                [
                    new(RingOffice.Fitting.Machinery, UnitRunDu, MachineryPlate, false),
                    new(RingOffice.Fitting.Machinery, UnitRunDu, "", false),
                    new(RingOffice.Fitting.Shelving, UnitRunDu, "", false),
                ];

            case Kit.Office:
                return
                [
                    // #869 · ONE PERSON'S DESK, at the size the owner measured his own with a tape:
                    // 160 x 90 cm, which is DeskRunDu x FittingDepthDu. It ran the whole wall until this
                    // issue — ten du of worktop for one clerk — and a desk sized like a bench is exactly the
                    // half of "reads like furniture" that a plate cannot fix.
                    new(RingOffice.Fitting.DeskBank, DeskRunDu, "", true, FittingDepthDu),
                    new(RingOffice.Fitting.FilingCabinet, UnitRunDu, FilingPlate, false),
                    new(RingOffice.Fitting.Shelving, UnitRunDu, "", false),
                ];

            case Kit.Clinic:
                return
                [
                    new(RingOffice.Fitting.Bench, BenchRunDu, ExaminationPlate, true),
                    new(RingOffice.Fitting.Counter, UnitRunDu, WorktopPlate, false),
                    new(RingOffice.Fitting.Shelving, UnitRunDu, "", false),
                ];

            default:
                return
                [
                    new(RingOffice.Fitting.Shelving, UnitRunDu, RingOffice.StorePlate, false),
                    new(RingOffice.Fitting.Bench, BenchRunDu, RingOffice.BenchPlate, true),
                ];
        }
    }
}

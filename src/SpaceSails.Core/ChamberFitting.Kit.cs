using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #818 · WHICH TRADE'S KIT A ROOM GETS — off the room's PLATE first, then the FLOOR's department, then
/// the site's own register.
///
/// <para>That is the ladder <c>RingOffice.DressingFor</c> walks one scale up, and for its reason: the
/// plate is the only thing that says what a room IS. An empty store gets its own plate and its own odds,
/// because a building where every store is full is a building nobody has ever worked in.</para>
///
/// <para>Split out of <c>ChamberFitting.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class ChamberFitting
{
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
}

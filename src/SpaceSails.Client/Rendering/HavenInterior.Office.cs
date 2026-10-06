using System.Linq;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Rendering;

/// <summary>
/// #1332 C/D/E · Part of <see cref="HavenInterior"/> (the header note lives in HavenInterior.cs) — <b>THE SIDE
/// OFFICES</b>: one of the five cabins on a hotel level, become an office with a plate on its door — Ringside
/// Exchange's Preservation office (C), Cinder Roost's forwarding desk (D), The Deep's adjuster's room (E). The three
/// are one kit (<see cref="SideOffice"/>) and this file draws whichever the station has.
///
/// <para>Every word it says is Core's (<see cref="PreservationOffice"/>) and every clock it keeps is Core's too;
/// this file owns only where it stands and how it is drawn. The office is DATA on the station's
/// <see cref="LowerSpec"/> (<c>Office</c>, the cabin it took) — a station without one builds the floor it always
/// built, to the byte.</para>
///
/// <h3>Three ways the office is drawn</h3>
///
/// <list type="bullet">
/// <item><b>Shut</b> — every watch but the clerk's: the cabin's own locked leaf, the corridor face unbroken, and
/// the only thing that differs from any other cabin is the plate over the door.</item>
/// <item><b>Ajar</b> — the clerk's watch: the corridor face is cut for a doorway, the leaf hangs part-way over
/// (the leaf-hauled idiom #563 drew for a door somebody left), and inside there is a desk with the sheet on it.</item>
/// <item><b>Ajar, the desk bare</b> — the same, once the sheet has been taken: the office keeps its hours.</item>
/// </list>
///
/// <h3>WHY THIS FILE DECLARES NO STATIC FIELD (#1163)</h3>
///
/// <para>Everything is measured off the cabin row, which is measured off <c>HallApothem</c> — a
/// <c>static readonly</c> of the opening file. So every number below is a <c>const</c> or a computed member, and
/// nothing can be evaluated early; <c>NoPartialClassSpreadsItsStaticFieldsTests</c> enforces it.</para>
/// </summary>
public static partial class HavenInterior
{
    /// <summary>#1332 C · How the office's door is drawn.</summary>
    public enum OfficeDoor
    {
        /// <summary>Locked, like its four neighbours. Every watch but the clerk's.</summary>
        Shut,

        /// <summary>Standing ajar on the clerk's watch, the sheet on the desk.</summary>
        Ajar,

        /// <summary>Standing ajar, the desk bare: the sheet has been taken this run.</summary>
        AjarDeskBare,
    }

    /// <summary>#1332 C · The station's spec, or null for a berth with no interior.</summary>
    private static StationSpec? SpecOf(string? bodyId) => System.Array.Find(Specs, s => s.BodyId == bodyId);

    /// <summary>#1332 C · Which cabin (one-based) is this station's office, or null — every station but three.</summary>
    private static int? TheOfficeCabinOf(StationSpec spec) => spec.Lower?.Office?.Cabin;

    /// <summary>#1332 C/D/E · This berth's side office — Ringside Exchange's Preservation office, Cinder Roost's
    /// forwarding desk, The Deep's adjuster's room — or null at every other haven.</summary>
    public static SideOffice? TheOfficeAt(string? bodyId) => SpecOf(bodyId)?.Lower?.Office;

    /// <summary>#1332 C · Does this berth have an office on its hotel level? Three havens do — asked of the
    /// catalogue so a guard can sweep every haven for it rather than trust an id.</summary>
    public static bool HasTheOffice(string? bodyId) => TheOfficeAt(bodyId) is not null;

    /// <summary>#1332 C · What is PAINTED over cabin <paramref name="n"/> (one-based) at this station: the
    /// office's plate on the office's door, <c>CABIN n</c> on every other.</summary>
    private static string CabinDoorPlateOf(StationSpec spec, int n) =>
        spec.Lower?.Office is { } office && office.Cabin == n ? office.DoorPlate : HavenLevels.CabinDoorPlate(n);

    /// <summary>#1332 C · …and the leaf's whole name in the register (what <see cref="Egress"/> seeds a door roll
    /// on). The office's door has one name, painted and registered alike.</summary>
    private static string CabinRegisterPlateOf(StationSpec spec, int n) =>
        spec.Lower?.Office is { } office && office.Cabin == n ? office.DoorPlate : HavenLevels.CabinPlate(n);

    /// <summary>#1332 C · The five painted plates of this station's row, in the doors' order — for the guards,
    /// which hold every haven but one to <c>CABIN n</c> and that one to the office's plate on one door.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> CabinDoorPlatesAt(string bodyId)
    {
        if (SpecOf(bodyId) is not { Lower: not null } spec)
        {
            return [];
        }

        var plates = new string[HavenLevels.Cabins];
        for (int i = 0; i < HavenLevels.Cabins; i++)
        {
            plates[i] = CabinDoorPlateOf(spec, i + 1);
        }

        return plates;
    }

    /// <summary>#1332 C · How far in from each party wall the ajar doorway's jambs stand. The cabin's own leaf is
    /// half its frontage — about a body and a half — which a captain could just about squeeze through; the office
    /// door is cut to the frontage less a jamb each side, so walking in is walking in.</summary>
    private const float OfficeJambDu = 0.3f;

    /// <summary>#1332 C · Half the ajar doorway's width.</summary>
    private static float OfficeDoorwayHalf => (CabinWidth / 2f) - OfficeJambDu;

    /// <summary>#1332 C · How far in from the office's back wall the desk's front edge stands, and how deep it is.</summary>
    private const float OfficeDeskOffBackDu = 0.35f;

    private const float OfficeDeskDepthDu = 1.1f;

    /// <summary>#1332 C · The office, as a box: its frontage across and the row's depth — <c>(west, front, east,
    /// back)</c>. Null at every berth without one.</summary>
    public static (double X0, double Y0, double X1, double Y1)? TheOfficeBox(string? bodyId)
    {
        if (SpecOf(bodyId) is not { } spec || TheOfficeCabinOf(spec) is not { } n)
        {
            return null;
        }

        float x = CabinDoorX(n - 1);
        return (x - (CabinWidth / 2f), CabinRowSouthY, x + (CabinWidth / 2f), CabinRowNorthY);
    }

    /// <summary>#1332 C · <b>IS THIS POINT INSIDE THE OFFICE?</b> In its box, past the corridor face, on the hotel
    /// level — the concourse is laid in the same coordinates and has no office in it.</summary>
    public static bool InTheOffice(string? bodyId, double x, double y, int level) =>
        level == HavenLevels.ServiceLevel
        && TheOfficeBox(bodyId) is { } box
        && x > box.X0 && x < box.X1 && y > box.Y0 && y < box.Y1;

    /// <summary>#1332 C · The office door's doorstep in the corridor — where its plate hangs and where the clerk
    /// steps out onto. Null at every berth without one.</summary>
    public static DeckReachability.Point? TheOfficeDoorstepAt(string? bodyId) =>
        SpecOf(bodyId) is { } spec && TheOfficeCabinOf(spec) is { } n
            ? TheCabinDoorstepAt(spec.BodyId, n - 1)
            : null;

    /// <summary>#1332 C · Where the sheet lies: the middle of the desk against the office's back wall.</summary>
    public static DeckReachability.Point? TheOfficeDeskAt(string? bodyId) =>
        TheOfficeBox(bodyId) is { } box
            ? new DeckReachability.Point(
                (box.X0 + box.X1) / 2, box.Y1 - OfficeDeskOffBackDu - (OfficeDeskDepthDu / 2))
            : null;

    /// <summary>#1332 C · <b>THE CAR THE CLERK RIDES UP IN</b> — the one whose landing is nearest his door, by the
    /// room's own landings. One answer, so the walk the page deals and the guard that holds it are one route.</summary>
    public static int? TheClerksCarAt(string? bodyId)
    {
        if (TheOfficeDoorstepAt(bodyId) is not { } door)
        {
            return null;
        }

        int? best = null;
        double nearest = double.MaxValue;
        for (int cage = 0; cage < HavenLevels.Cages; cage++)
        {
            if (TheCageLandingAt(bodyId!, cage) is not { } landing)
            {
                continue;
            }

            double dx = landing.X - door.X, dy = landing.Y - door.Y;
            double d = (dx * dx) + (dy * dy);
            if (d < nearest)
            {
                (nearest, best) = (d, cage);
            }
        }

        return best;
    }

    /// <summary>#1332 C · Does this plan draw the office standing open? Read off the plan itself — the one leaf on
    /// a hotel level that is not locked — so the page asks what it is looking at rather than remembering what it
    /// asked for.</summary>
    public static bool TheOfficeStandsOpenIn(DeckPlan plan) =>
        System.Array.Exists(plan.Doors, d => !d.Locked);

    /// <summary>#1332 C · …and is the sheet on the desk in it?</summary>
    public static bool TheSheetLiesIn(DeckPlan plan) =>
        System.Array.Exists(plan.Consoles, c =>
            c.Kind == DeckPlan.ConsoleKind.ViewObject
            && SideOffices.All.Any(o => string.Equals(c.Label, o.SheetTitle, System.StringComparison.Ordinal)));

    /// <summary>#1332 C · The corridor face of the cabin row: one unbroken wall, exactly as every station has
    /// always drawn it — or, at the office's station on the clerk's watch, the same wall with the office's
    /// doorway cut out of it.</summary>
    private static void TheCorridorFace(StationSpec spec, OfficeDoor office, System.Collections.Generic.List<DeckPlan.Wall> walls)
    {
        if (office == OfficeDoor.Shut || TheOfficeCabinOf(spec) is not { } n)
        {
            walls.Add(new(CabinRowWestX, CabinRowSouthY, CabinRowEastX, CabinRowSouthY, false, false));
            return;
        }

        float x = CabinDoorX(n - 1);
        walls.Add(new(CabinRowWestX, CabinRowSouthY, x - OfficeDoorwayHalf, CabinRowSouthY, false, false));
        walls.Add(new(x + OfficeDoorwayHalf, CabinRowSouthY, CabinRowEastX, CabinRowSouthY, false, false));
    }

    /// <summary>#1332 C · Cabin <paramref name="i"/>'s (zero-based) leaf as the build draws it: locked, or — the
    /// office on the clerk's watch — the doorway's own leaf, unlocked.</summary>
    private static DeckPlan.Door TheCabinLeafAsDrawn(
        StationSpec spec, OfficeDoor office, int i, in UndergroundComplex.LockedDoor leaf)
    {
        if (office != OfficeDoor.Shut && TheOfficeCabinOf(spec) == i + 1)
        {
            float x = CabinDoorX(i);
            return new DeckPlan.Door(x - OfficeDoorwayHalf, CabinRowSouthY, x + OfficeDoorwayHalf, CabinRowSouthY);
        }

        return new DeckPlan.Door((float)leaf.X1, (float)leaf.Y1, (float)leaf.X2, (float)leaf.Y2, Locked: true);
    }

    /// <summary>#1332 C · What stands inside the office while its door is open: the desk against the back wall (a
    /// work surface, #868's tone 0 — furniture, not a wall) and, until it is taken, the sheet on it as the
    /// building's own paper pick-up (a <c>ViewObject</c> console plated with the sheet's title, the dropped
    /// schedule's idiom). Null when the office is shut or not here: nothing is added to anybody's plan.</summary>
    private static DeckPlan.FurnitureSpot[]? FurnishTheOffice(
        StationSpec spec, OfficeDoor office, System.Collections.Generic.List<DeckPlan.ConsoleSpot> consoles)
    {
        if (office == OfficeDoor.Shut || spec.Lower?.Office is not { } side || TheOfficeBox(spec.BodyId) is not { } box
            || TheOfficeDeskAt(spec.BodyId) is not { } desk)
        {
            return null;
        }

        float back = (float)box.Y1 - OfficeDeskOffBackDu;
        float half = (CabinWidth / 2f) - OfficeJambDu;
        if (office == OfficeDoor.Ajar)
        {
            consoles.Add(new(
                DeckPlan.ConsoleKind.ViewObject, (float)desk.X, (float)desk.Y, side.SheetTitle));
        }

        return
        [
            new DeckPlan.FurnitureSpot(
                (float)desk.X - half, back - OfficeDeskDepthDu, (float)desk.X + half, back, Tone: 0),
        ];
    }

    /// <summary>#1332 C · The leaf hangs part-way over while the door stands ajar — half a haul of #563's own
    /// idiom (<see cref="DeckPlan.HaulLeaf"/>), which the pen draws as a leaf slid half across its doorway. It
    /// retracts the whole way as the captain comes up to it, the way every unlocked door does.</summary>
    private static void HangTheOfficeLeafAjar(StationSpec spec, OfficeDoor office, DeckPlan plan)
    {
        if (office == OfficeDoor.Shut || TheOfficeCabinOf(spec) is not { } n)
        {
            return;
        }

        plan.HaulLeaf(n - 1, 0.5, 1.0);
    }
}

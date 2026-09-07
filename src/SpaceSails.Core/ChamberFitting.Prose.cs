using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// WHAT IT SAYS — every plate a fitting wears, the seat plate that goes with each kit, the stool scene
/// the captain sits down into, and the approach ordinal that keeps two stools in one building apart.
///
/// <para><c>AllProse</c> is here too, which is what lets the prose sweeps read this file's whole voice
/// without knowing how many plates there are.</para>
///
/// <para>Split out of <c>ChamberFitting.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public static partial class ChamberFitting
{
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
}

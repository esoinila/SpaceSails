using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

// Subject: what it says — the plates, the lines, and the scene of sitting down.
public static partial class RingOffice
{
    // ── WHAT IT SAYS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The chair, as every seat in this game wears it.</summary>
    public const string Glyph = SittingAlone.Glyph;

    /// <summary>What a free office chair is labelled on the deck, with the verb on it (#783).</summary>
    public const string FreeChairPlate = Glyph + " AN OFFICE CHAIR — SIT DOWN";

    /// <summary>The long table in a negotiation room.</summary>
    public const string TablePlate = "🪑 THE LONG TABLE";

    /// <summary>The reading room's shelving.</summary>
    public const string ReadingShelfPlate = "📚 BOUND COPIES · DO NOT REMOVE";

    /// <summary>A plain room's shelving — the back of house and the corner offices.</summary>
    public const string StorePlate = "🗄 SHELVING";

    /// <summary>…and its bench.</summary>
    public const string BenchPlate = "🪑 A BENCH";

    /// <summary>
    /// #868 · …and the surface the bench is a bench AT.
    ///
    /// <para>Owner, sealing the fix: <i>"I think table should be similar just say table."</i> — one idiom for
    /// all plan furniture, the shelving's own recipe (a filled rectangle with a plate on it), and the
    /// plainest possible noun on the plate.</para>
    ///
    /// <para>It was written WORKTOP for one round, off a parenthetical in the issue's draft text
    /// (<i>"or the kit's honest noun — WORKTOP where the narration says worktop"</i>) — which was the crew
    /// reading a DRAFT over a RULING. The seal is the last word. Nothing is left disagreeing: the sit line
    /// keeps <i>the worktop is clear</i>, because a table's top is its worktop.</para>
    /// </summary>
    public const string WorktopPlate = "🪑 TABLE";

    /// <summary>The counter a reception is.</summary>
    public const string ReceptionPlate = "📇 THE COUNTER · APPOINTMENTS HELD";

    /// <summary>The kitchenette in a big suite. The building's own register: it says what the fixture is and
    /// nothing about what the facility is for (§13.8).</summary>
    public const string KitchenettePlate = "🍵 KITCHENETTE · STAFF ONLY";

    /// <summary>One of the two WCs.</summary>
    public static string WcPlate(int n) => $"🚻 WC {n}";

    /// <summary>One of the privacy booths.</summary>
    public static string BoothPlate(int n) => $"🕻 PRIVACY BOOTH {n}";

    /// <summary>#828 · How deep the secure disposal stands down the service strip. A machine and not a room:
    /// deliberately shallower than a cell (<see cref="CellDu"/>), so it fits in the depth every big suite on
    /// every site already had spare after the kitchenette, the two WCs and both booths.</summary>
    public const double SecureDisposalDu = 3.0;

    /// <summary>#828 · What is stencilled on it — <see cref="RipAndBin.PlateFor"/>'s own word for the rung,
    /// never a second one. The plan's plate and the verb's plate are the same string because they are the
    /// same object seen from two ends of the building.</summary>
    public static string SecureDisposalPlate => RipAndBin.PlateFor(RipAndBin.Tier.SecureDisposal);

    /// <summary>#821 · One of the public washroom's cubicles, with the verb on it (#783) — a door you can
    /// shut is the whole of what this room offers and the plate should say so.</summary>
    public static string PublicWcPlate(int n) => $"🚻 CUBICLE {n} · STEP IN";

    /// <summary>#821 · The basin run. The building's own register — it names the fixture and nothing about
    /// what the facility is for (§13.8) — and it carries the verb, because [E] here washes your hands.</summary>
    public const string BasinRunPlate = "🚰 THE BASIN RUN · WASH YOUR HANDS";

    /// <summary>Where you are, when you are sitting in one of these.</summary>
    public const string Setting = "A chair in one of the park-view suites";

    /// <summary>Sitting down. #783's law kept: the FIRST clause confirms the state change, and the room names
    /// itself — the plate beside the door is the only thing that tells one poured box from another, and a
    /// captain who has walked in off a corridor deserves to be told which one they are sitting in.</summary>
    /// <param name="plate">The room's own stencil.</param>
    /// <param name="garden">#868 · Is this a room that LOOKS AT THE GREEN — see
    /// <see cref="LooksAtTheGarden"/>. The clause about the wall of glass is reachable through this
    /// parameter and nowhere else, which is the whole of the third finding on this issue.</param>
    /// <param name="desk">#868 · Is there actually a worktop in front of this seat — see
    /// <see cref="WorksAtASurface"/>. Every sentence that says <i>the worktop is clear</i> is reachable
    /// through this parameter and nowhere else, so a bench outside a row of cubicles cannot be narrated as
    /// somebody's desk.</param>
    public static string TookAChairLine(string plate, bool garden, bool desk)
    {
        if (!desk)
        {
            return WaitingSeatLine(plate);
        }
        if (string.IsNullOrEmpty(plate))
        {
            return "You pull out a chair and sit down. Nothing on this floor is signed, and nobody has "
                + "been at this desk in a very long time.";
        }
        if (garden)
        {
            return $"You pull out a chair and sit down at somebody's desk in {plate}. The worktop is "
                + "clear, the screen is dark, and the whole wall in front of you is the garden.";
        }
        return IsColdStore(plate) ? ColdStoreChairLine : BackRoomChairLine(plate);
    }

    /// <summary>
    /// #868 · SITTING DOWN IN THE COLD ROOM — the owner's own sentence, kept verbatim.
    ///
    /// <para>He wrote it standing in <c>❄ COLD ROOM · TO CANTEEN 1</c>, having just been told by the game
    /// that the whole wall in front of him was the garden: <i>"You pull out a chair and sit down at
    /// somebody's desk in the cold store. The worktop is clear, the air bites, and the door is the only
    /// view."</i> Not a word of it is the crew's, and it is a const rather than a branch inside the general
    /// line so that it cannot be reworded by somebody tidying a format string.</para>
    /// </summary>
    public const string ColdStoreChairLine =
        "You pull out a chair and sit down at somebody's desk in the cold store. The worktop is clear, the "
        + "air bites, and the door is the only view.";

    /// <summary>#868 · Is this room the one the owner sat in? Off the PLATE, which is the only thing that
    /// says what a room is — the same one question <see cref="IsWashroom"/> asks, published so the sit line
    /// and any guard about it read one sentence.</summary>
    public static bool IsColdStore(string plate) =>
        plate.Contains("COLD ROOM", StringComparison.Ordinal)
        || plate.Contains("COLD STORE", StringComparison.Ordinal);

    /// <summary>
    /// #868 · …and sitting down in every OTHER room with no garden in front of it — the rest of the back of
    /// house, the corner offices, the block's own washroom.
    ///
    /// <para><b>NEW PROSE, and flagged as such for the owner to bless.</b> The ruling asked for <i>"one
    /// neutral non-view line for other windowless rooms — in the same voice"</i>: the cold room's own line
    /// is his and this one is the crew's, written to the same three-clause shape and naming no garden. It
    /// keeps <i>the worktop is clear</i>, because that clause is now TRUE — there is a worktop in front of
    /// the chair (see <see cref="Plain"/>), which there was not when this issue was opened.</para>
    /// </summary>
    /// <param name="plate">The room's own stencil.</param>
    public static string BackRoomChairLine(string plate) =>
        $"You pull out a chair and sit down at somebody's desk in {plate}. The worktop is clear, the room "
        + "is quiet, and there is nothing in front of you but the wall it was poured against.";

    /// <summary>
    /// #868 · DOES A CHAIR IN THIS ROOM LOOK AT THE GARDEN?
    ///
    /// <para>Owner, on being told it did while sitting in a cold store: the view-suite line was firing in the
    /// back of house. The clause is a fact about a DRESSING and not about a wall — the far band's park face
    /// is glass with the gravel gate cut through it, so a test on the geometry alone would keep saying yes in
    /// the very room the complaint came from. What is actually being asked is <i>is this a room somebody
    /// rented for the aspect</i>, and <see cref="DressingFor"/> is the one place in the building that
    /// answers it: everything it dresses PLAIN is a corner office or a back room, and the washroom is not a
    /// room anybody sits in to look out of.</para>
    /// </summary>
    public static bool LooksAtTheGarden(in UndergroundComplex.RingRoom room) =>
        DressingFor(in room) is not (Dressing.Plain or Dressing.Washroom);

    /// <summary>
    /// #868 · …AND SITTING SOMEWHERE THAT IS NOT A DESK AT ALL — the bench outside the block's cubicles, the
    /// seat inside a privacy booth.
    ///
    /// <para><b>NEW PROSE, flagged for the owner to bless.</b> It exists because fixing the garden clause
    /// exposed the same fault one room along: the ring publishes seats that are places to WAIT rather than
    /// places to work, and every one of them was being told <i>the worktop is clear</i> in a room with no
    /// worktop in it. The clause is now reachable only where a surface is actually within reach of the seat
    /// (<see cref="WorksAtASurface"/>), so this is what is left to say — and what is left to say about
    /// waiting in somebody else's building is #603's own note: being uninteresting is the cover.</para>
    /// </summary>
    /// <param name="plate">The room's own stencil.</param>
    public static string WaitingSeatLine(string plate) =>
        string.IsNullOrEmpty(plate)
            ? "You sit down and wait. Nobody has been through here in a long time, and nobody comes through "
                + "now."
            : $"You sit down in {plate} with nothing in front of you, and wait. Nobody looks twice at "
                + "somebody who is only waiting, which is most of what this seat is for.";

    /// <summary>#868 · Is this the kind of fitting somebody WORKS AT — the thing a chair is a lie without?
    /// Asked of the published KIND and never of a plate's wording, so a fixture renamed tomorrow keeps its
    /// answer.</summary>
    public static bool IsWorkSurface(Fitting kind) =>
        kind is Fitting.DeskBank or Fitting.Table or Fitting.Counter or Fitting.LabBench;

    /// <summary>
    /// #868 · HAS THIS SEAT GOT A WORKTOP IN FRONT OF IT?
    ///
    /// <para>Measured off the published boxes, because that is the only honest way to ask it. The owner sat
    /// in a chair that told him the worktop in front of him was clear, in a room that published no worktop at
    /// all — the sentence was reading the PLATE and the plate says nothing about what is standing on a floor.
    /// A seat is at a desk when a desk is within the setback a chair is laid at
    /// (<see cref="ChairSetbackDu"/>), which is the same number the placer used to lay it there.</para>
    /// </summary>
    public static bool WorksAtASurface(in UndergroundComplex.RingRoom room, in Chair chair)
    {
        foreach (Fixture f in room.Furniture)
        {
            if (!IsWorkSurface(f.Kind))
            {
                continue;
            }
            double dx = Math.Max(Math.Max(f.X0 - chair.X, chair.X - f.X1), 0.0);
            double dy = Math.Max(Math.Max(f.Y0 - chair.Y, chair.Y - f.Y1), 0.0);
            if (Math.Sqrt((dx * dx) + (dy * dy)) <= ChairSetbackDu + 0.01)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Standing up off it.</summary>
    public const string StoodUpLine = "You push the chair back and stand up.";

    /// <summary>What sitting a while at a desk is FOR. You are a person at a workstation in an office you do
    /// not work in, which is a cover and not a rest.</summary>
    public const string WaitLabel = "SIT A WHILE — look like you work here";

    /// <summary>…and the way off it.</summary>
    public const string StandLabel = "Stand up";

    /// <summary>Who you are sitting as, on the docked strip.</summary>
    public const string SeatPlate = Glyph + " AT A DESK";

    /// <summary>What nothing happening at a desk in somebody else's office sounds like.</summary>
    public static readonly IReadOnlyList<string> NobodyCameLines =
    [
        "A while goes by. Somewhere down the row a chair creaks and settles, and the lamps over the garden "
        + "carry on being four in the afternoon.",
        "Nobody comes. A ventilation register above the glass ticks over to its next setting and the whole "
        + "room gets half a degree cooler.",
        "A while goes by. Somebody walks past the door without slowing down, which is the whole of what you "
        + "came here to be.",
        "Nothing. The screen in front of you wakes at a movement somewhere behind you, shows a login field, "
        + "and goes dark again.",
    ];

    /// <summary>What nothing happening sounds like, told rather than left to be inferred from a control that
    /// went quiet (#603/#757).</summary>
    /// <param name="beat">Which wait this was.</param>
    public static string NobodyCame(int beat)
    {
        IReadOnlyList<string> pool = NobodyCameLines;
        return pool[((beat % pool.Count) + pool.Count) % pool.Count];
    }

    /// <summary>
    /// THE CHAIR, as an <see cref="Encounter.Scene"/> — two moves and no third, on the machine every other
    /// seat in this game already runs on.
    ///
    /// <para>The move IDS are <see cref="SittingAlone.Wait"/> and <see cref="SittingAlone.Stand"/> and
    /// deliberately not new ones: the park bench made the same choice for the same reason (a saved game and
    /// a guard both key on the id). Only the labels and the words are the office's.</para>
    /// </summary>
    /// <param name="plate">The room's own stencil, which the opening line names.</param>
    /// <param name="garden">#868 · See <see cref="LooksAtTheGarden"/>.</param>
    /// <param name="desk">#868 · See <see cref="WorksAtASurface"/>.</param>
    public static Encounter.Scene TheChair(string plate, bool garden, bool desk) => new(
        "ring:chair",
        SeatPlate,
        Setting,
        TookAChairLine(plate, garden, desk),
        [
            new(SittingAlone.Wait, WaitLabel),
            new(SittingAlone.Stand, StandLabel, Says: StoodUpLine),
        ]);

    /// <summary>Where a chair's watch-scoped state is keyed from, so an office chair and a canteen top with
    /// the same ordinal on the same floor can never share a wait counter. The park bench's own reason
    /// (<see cref="ParkBenches.ApproachOrdinalBase"/>), one room along, and far enough past it that no ring
    /// will ever grow into the gap.</summary>
    public const int ApproachOrdinalBase = 5000;

    /// <summary>This chair's ordinal for the approach roll.</summary>
    /// <param name="roomNumber">The ring room it stands in.</param>
    /// <param name="chairIndex">Its ordinal in that room.</param>
    public static int ApproachOrdinal(int roomNumber, int chairIndex) =>
        ApproachOrdinalBase + (roomNumber * 64) + chairIndex;

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string s in NobodyCameLines)
        {
            yield return s;
        }
        yield return FreeChairPlate;
        yield return TablePlate;
        yield return ReadingShelfPlate;
        yield return StorePlate;
        yield return BenchPlate;
        yield return ReceptionPlate;
        yield return KitchenettePlate;
        yield return WcPlate(1);
        yield return BoothPlate(1);
        yield return PublicWcPlate(1);
        yield return BasinRunPlate;
        yield return Setting;
        yield return WorktopPlate;
        yield return TookAChairLine("", true, true);
        yield return TookAChairLine("A BACK ROOM", true, true);
        yield return ColdStoreChairLine;
        yield return BackRoomChairLine("A BACK ROOM");
        yield return WaitingSeatLine("");
        yield return WaitingSeatLine("A BACK ROOM");
        yield return StoodUpLine;
        yield return WaitLabel;
        yield return StandLabel;
        yield return SeatPlate;
    }
}

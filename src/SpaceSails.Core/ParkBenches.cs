using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #793 · A STEEL BENCH IS A SEAT WITH TWO ENDS — and which of them is free decides what you may do on it.
///
/// <para>Owner, from play in the new park: <i>"the benches take the sit verb"</i> · <i>"a park bench under a
/// painted sky is the best breather in the base"</i> · and the rule the whole rung hangs off: <i>"the park
/// bench might be used to go through inventory info loot, <b>if we get the whole bench to ourselves</b> to
/// have some privacy."</i></para>
///
/// <h3>Why a bench is not a table</h3>
///
/// <para>A hall table is a place with chairs round it, and somebody taking one is a CONVERSATION — a face
/// opposite, a scene, a card. A bench is one plank: somebody who sits down on it sits down <i>beside</i>
/// you, says nothing, and is not talking to you at all. They are simply close enough to read what is on your
/// knee. So a shared bench keeps you sitting, keeps you resting and keeps you findable, and takes away
/// exactly one thing — the spread (<see cref="SeatedSpread.CanSpreadTheCase"/>'s
/// <see cref="SeatedHud.Seat.ParkBench"/> rung, which has been in the ladder since #799 waiting for the
/// occupancy fact this file makes real).</para>
///
/// <h3>The occupancy is not invented here</h3>
///
/// <para>#790 already put somebody on the furthest bench — <c>Park.FigureX/FigureY/FigurePlate</c>, seeded
/// off the site so the same park is the same park every visit. That figure was scenery with nothing to
/// press. It is the same figure, it still has nothing to say, and it is now the reason one bench in the room
/// is a rest and not a desk. Nothing new was populated to make the predicate bite: the room already
/// disagreed with itself about whether that plank was empty, and this is the half that noticed.</para>
///
/// <para>Pure: it is handed the park the generator carved and answers questions about it. It has no clock,
/// no dice and no opinion about where a bench goes.</para>
/// </summary>
public static class ParkBenches
{
    /// <summary>How far from a bench's centre each end's seat is — half of the half, so the two ends sit
    /// where a person sits rather than out on the arm-rests. A fraction of Core's own
    /// <see cref="UndergroundComplex.ParkBenchHalfDu"/> and never a du typed twice.</summary>
    public const double EndOffsetDu = UndergroundComplex.ParkBenchHalfDu / 2.0;

    /// <summary>How close two coordinates have to be to be the same bench. The carve lays benches whole du
    /// apart down a 278 du room, so this is a rounding tolerance and not a search radius.</summary>
    public const double SameBenchDu = 0.5;

    /// <summary>How many a bench seats. Two, and the number is load-bearing rather than decorative: the
    /// privacy predicate is <i>the whole bench</i>, and a bench that seated three would make "alone" a
    /// different question.</summary>
    public const int Ends = 2;

    /// <summary>Which end somebody already on the bench has taken. The FIRST, matching #792's own drawing
    /// convention at a table ("the party sits in the first chair") so the deck and the sim cannot come to
    /// two views of which half is free.</summary>
    public const int TakenEnd = 0;

    /// <summary>
    /// One bench, as a seat.
    /// </summary>
    /// <param name="Index">Its ordinal in <c>Park.Benches</c> — the room's own, never a pair of doubles.</param>
    /// <param name="X">Its centre, where the carve bolted it down.</param>
    /// <param name="Y">The same.</param>
    /// <param name="Taken">Somebody is already on it. #790's lone figure, and nobody else — a park with a
    /// crowd on every plank would be a park that had noticed you.</param>
    /// <param name="Plate">Who they read as, if anybody is there. The park's own
    /// <c>FigurePlate</c>; null on an empty bench.</param>
    public readonly record struct Bench(int Index, double X, double Y, bool Taken, string? Plate)
    {
        /// <summary>Where one end of it is. Ends run along the bench's own axis, which is the axis the carve
        /// laid the plank on — a bench in this park is a horizontal segment, so the ends differ in X.</summary>
        public (double X, double Y) End(int end) =>
            (X + ((end == TakenEnd ? -1.0 : 1.0) * EndOffsetDu), Y);

        /// <summary>Which end the captain gets. The free one — there is only ever one choice, which is why
        /// pressing [E] does not ask.</summary>
        public (double X, double Y) YourEnd => End(Taken ? 1 : TakenEnd);

        /// <summary>Which end of this plank is nearest a body standing at (<paramref name="fromX"/>,
        /// <paramref name="fromY"/>). The ORDINAL, so a caller who wants the coordinate asks
        /// <see cref="End"/> for it and no second author measures a bench.</summary>
        public int NearestEnd(double fromX, double fromY)
        {
            (double ax, double ay) = End(TakenEnd);
            (double bx, double by) = End(1);
            double a = ((ax - fromX) * (ax - fromX)) + ((ay - fromY) * (ay - fromY));
            double b = ((bx - fromX) * (bx - fromX)) + ((by - fromY) * (by - fromY));
            return b < a ? 1 : TakenEnd;
        }

        /// <summary>
        /// #820 · WHICH END THE SIT PUTS YOU ON — the whole of what a bench has to decide when the captain
        /// presses [E] from wherever they happened to be standing.
        ///
        /// <para>Owner, on a bench in the evening: <i>"I would move the avatar on top of the bench when I
        /// sit… just snap it into the correct position."</i> A plank is two seats, so the snap has to pick
        /// one, and there are exactly two honest answers. Somebody already on it and there is no choice at
        /// all — you get the end they are not on, which is <see cref="YourEnd"/>. A bench to yourself and
        /// you sit down on the end you WALKED UP TO: a captain who approached the left end and landed on the
        /// right one would have been slid a plank's length sideways by the very key that was supposed to
        /// stop them resting their legs from a metre away.</para>
        /// </summary>
        public (double X, double Y) EndYouTake(double fromX, double fromY) =>
            Taken ? YourEnd : End(NearestEnd(fromX, fromY));

        /// <summary>What is drawn over it, and it says the VERB when there is one to say (#783's own ruling
        /// on a free table: <i>"why not use words like SIT DOWN here if it means sitting down?"</i>). A
        /// shared bench still takes the press — half a bench is a rest — and the plate says which half you
        /// are getting before you press anything.</summary>
        public string DeckPlate => Taken ? SharedBenchPlate : FreeBenchPlate;
    }

    /// <summary>
    /// EVERY BENCH IN THIS PARK, with who is on it — the one list the deck, the press and the privacy gate
    /// all read.
    ///
    /// <para>The occupancy is matched against the park's published figure rather than re-seeded: a second
    /// roll for "is anybody on this plank" would be a second answer to a question #790 already answered, and
    /// the two would part company on the first watch that turned over.</para>
    /// </summary>
    public static IReadOnlyList<Bench> On(in UndergroundComplex.Park park)
    {
        IReadOnlyList<(double X, double Y)> planks = park.Benches ?? [];
        var found = new List<Bench>(planks.Count);
        for (int i = 0; i < planks.Count; i++)
        {
            (double bx, double by) = planks[i];
            bool taken = Math.Abs(bx - park.FigureX) <= SameBenchDu
                && Math.Abs(by - park.FigureY) <= SameBenchDu;
            found.Add(new Bench(i, bx, by, taken, taken ? park.FigurePlate : null));
        }
        return found;
    }

    /// <summary>The bench at this spot, or null. A LOOKUP against the room's own list and never a decision —
    /// the press landed on a console the renderer put on a bench, and this says which one.</summary>
    public static Bench? At(in UndergroundComplex.Park park, double x, double y)
    {
        foreach (Bench b in On(in park))
        {
            if (Math.Abs(b.X - x) <= SameBenchDu && Math.Abs(b.Y - y) <= SameBenchDu)
            {
                return b;
            }
        }
        return null;
    }

    // ── THE ORDINAL THE APPROACH IS ROLLED ON ─────────────────────────────────────────────────────────

    /// <summary>
    /// Where a bench's ordinals start, so a bench and a table can never share one.
    ///
    /// <para><see cref="SittingAlone.SomebodyComes"/> is seeded on (site, floor, ORDINAL, watch, beat). Bench
    /// 0 and table 0 are two different seats in two different rooms, and handing them the same ordinal would
    /// deal them the same answer on the same shift — one source consumed as if it were two, which is a bug
    /// this repo has a name for. The offset is large enough that no hall will ever grow into it.</para>
    /// </summary>
    public const int ApproachOrdinalBase = 4000;

    /// <summary>This bench's ordinal for the approach roll.</summary>
    public static int ApproachOrdinal(int benchIndex) => ApproachOrdinalBase + benchIndex;

    /// <summary>Is there room for anybody else on this bench? Two ends, and the captain is on one of them —
    /// so a bench you are already sharing can never grow a third person, and the wait beat must not ask.</summary>
    public static bool TheOtherEndIsFree(bool shared) => !shared;

    // ── WHAT IT SAYS ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The chair, as every seat in this game wears it.</summary>
    public const string Glyph = SittingAlone.Glyph;

    /// <summary>What an empty bench is labelled on the deck. #790's plate with the verb on it — the plain
    /// word for sitting down is sitting down (#783).</summary>
    public const string FreeBenchPlate = UndergroundComplex.ParkBenchPlate + " — SIT DOWN";

    /// <summary>…and one with somebody already on it. It still takes the press, and the plate says what you
    /// are getting: the far half, and no privacy with it.</summary>
    public const string SharedBenchPlate =
        UndergroundComplex.ParkBenchPlate + " — SOMEBODY ON THE FAR END";

    /// <summary>Who you are sitting with, when it is nobody. The strip's counterpart line.</summary>
    public const string OwnBenchPlate = Glyph + " A BENCH TO YOURSELF";

    /// <summary>…and when it is not.</summary>
    public const string SharedPlate = Glyph + " HALF A BENCH";

    /// <summary>Where you are. The park's own name for itself.</summary>
    public const string Setting = "The park behind the canteen glass";

    /// <summary>Taking the whole plank. #783's law kept: the FIRST clause confirms the state change, before
    /// anything else is said.</summary>
    public const string TookTheBenchLine =
        "You sit down. The bench is yours, both ends of it. Grow-lamps overhead doing their best impression "
        + "of four in the afternoon, gravel in front of you, and a clear run of walk in either direction.";

    /// <summary>…and taking the free end of one that is not.</summary>
    public const string SharedTheBenchLine =
        "You sit down on the free end. Somebody is on the other one, close enough that the plank moves when "
        + "they move, and they do not look up.";

    /// <summary>Somebody takes the end you left. NOT a conversation and not an approach: they sat down
    /// beside you the way strangers sit down on benches, which is the whole reason it costs you the
    /// spread.</summary>
    public const string SomebodyTookTheOtherEndLine =
        "The plank shifts. Somebody has sat down on the other end with a cup and a folded coat, close enough "
        + "to read anything you are holding, and says nothing at all.";

    /// <summary>Standing up off it.</summary>
    public const string StoodUpLine =
        "You get up, and the bench is a bench again.";

    /// <summary>What sitting a while is FOR, on a bench, in the owner's own words. The label carries the
    /// gumshoe reading because the button IS the instrument — a bench that said "see who comes" would be
    /// promising the hall's verb in a room where the point is the opposite.</summary>
    public const string WaitLabel = "SIT A WHILE — see who else stops";

    /// <summary>…and the way off it.</summary>
    public const string StandLabel = "Stand up";

    /// <summary>What waiting on a bench is like when nothing comes of it. The park's own silence and not the
    /// hall's: a line about trays and eighty chairs, read on gravel under grow-lamps, would be the room and
    /// the sentence disagreeing.</summary>
    public static readonly IReadOnlyList<string> NobodyCameLines =
    [
        "A while goes by. A lamp somewhere up in the gantry ticks as it warms, and the beds smell like beds.",
        "Somebody walks the length of the far curve without looking at anything, the way people walk when "
        + "the walk is the point.",
        "A while goes by. Water goes on somewhere under the planting, runs for a count of thirty, and stops.",
        "Nothing comes down the walk. The glass at your back has the hall in it, small and warm and busy, "
        + "with the sound off.",
    ];

    /// <summary>What nothing happening on a bench sounds like — the told outcome, so a wait that produced
    /// nobody reads as an answer rather than as a control that did not respond (#603/#757).</summary>
    /// <param name="beat">Which wait this was, so four of them are four different sentences.</param>
    public static string NobodyCame(int beat)
    {
        IReadOnlyList<string> pool = NobodyCameLines;
        return pool[((beat % pool.Count) + pool.Count) % pool.Count];
    }

    /// <summary>
    /// THE BENCH, as an <see cref="Encounter.Scene"/> — two moves and no third, the same shape a table you
    /// took alone has, on the same machine.
    ///
    /// <para>The move IDS are <see cref="SittingAlone.Wait"/> and <see cref="SittingAlone.Stand"/> and
    /// deliberately not new ones: a saved game and a guard both key on the id, the panel's dispatch already
    /// answers them, and a bench that invented its own vocabulary for sitting still would be a second engine
    /// for one verb. Only the LABELS and the words are the park's.</para>
    /// </summary>
    /// <param name="shared">Whether somebody is on the other end.</param>
    public static Encounter.Scene TheBench(bool shared) => new(
        "park:bench",
        shared ? SharedPlate : OwnBenchPlate,
        Setting,
        shared ? SharedTheBenchLine : TookTheBenchLine,
        [
            new(SittingAlone.Wait, WaitLabel),
            new(SittingAlone.Stand, StandLabel, Says: StoodUpLine),
        ]);

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        foreach (string s in NobodyCameLines)
        {
            yield return s;
        }
        yield return FreeBenchPlate;
        yield return SharedBenchPlate;
        yield return OwnBenchPlate;
        yield return SharedPlate;
        yield return Setting;
        yield return TookTheBenchLine;
        yield return SharedTheBenchLine;
        yield return SomebodyTookTheOtherEndLine;
        yield return StoodUpLine;
        yield return WaitLabel;
        yield return StandLabel;
    }
}

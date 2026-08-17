using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #758 · THE CURTAIN AND THE DOOR — what a cabinet is when nobody has decided anything yet, and what it
/// becomes when somebody does.
///
/// <para>Owner, 2026-08-06 night: <i>"the cabinets could have some kind of privacy curtain effect ...
/// something that prevents the sound or sight catching what happens there too easily but is not a real
/// door."</i></para>
///
/// <h3>The card is untouched, and it is now a CHOICE</h3>
///
/// <para>#751 shipped the cabinets with a padded vault door and a card that says it out loud — <i>"a door
/// padded like a vault that dogs shut from inside"</i> — and that sentence stands exactly as written. What
/// this file adds is that the door <b>normally stands open</b>, with a heavy curtain drawn across the way,
/// and that dogging it is a thing a captain DOES rather than a thing the room came with.</para>
///
/// <para><b>Nothing in the card had to change.</b> #751 wrote the sentence this issue turns into a verb, and
/// the only prose that moved anywhere is the one line of <see cref="SittingAlone.NobodyCameCabinet"/> that
/// asserted a shut door on a room whose door is now usually open — a sentence and a sim disagreeing about
/// the same leaf is this repository's third named bug class, and it would have shipped inside the feature
/// that created the disagreement.</para>
///
/// <list type="number">
/// <item><b>Stage one, the curtain.</b> Soft, probabilistic privacy. #751's quiet rule holds whole — a file
/// on the table in here is not LOUD, because the counter has no eyes in this room — but a beat behind cloth
/// has a small seeded chance of LEAKING: a silhouette read through the weave, half a sentence caught by
/// somebody walking past to the washroom. The odds scale with how full the hall is
/// (<see cref="SittingAlone.Fill"/>), which makes WHEN you take the meeting a tactical question
/// nobody ever states: an echoing hall at the small watches leaks a third of what a roaring one does.</item>
/// <item><b>Stage two, the door.</b> Dogged shut, the cabinet is absolute — <see cref="LeakChance"/> is not
/// consulted at all and <see cref="Leaks"/> is false on every seed there is. The cost is that the ACT is
/// public: a padded leaf coming out of a wall is the loudest thing in a canteen, and the counter's long
/// memory writes down who was inside when it happened.</item>
/// </list>
///
/// <h3>A leak is never announced</h3>
///
/// <para>Nothing is said, no meter moves, and no card comes up on the beat that leaked. It surfaces LATER,
/// as #715's per-entity memory has surfaced since <see cref="RipAndBin.SeenNote"/>: somebody who should not
/// know says a thing that only makes sense if they do (<see cref="BarkThatKnows"/>). That is this game's
/// grammar and the reason the mechanic is worth having — a captain who is told they were overheard is
/// playing a stealth meter, and a captain who hears their cabinet number in somebody else's mouth is playing
/// this.</para>
///
/// <h3>Pure, world-blind, and it owns the numbers</h3>
///
/// <para>Nothing in here knows what a deck plan is, who is escorting anybody, or which watch it is — the
/// caller freezes the watch and hands it down, exactly as #709's rota is handed down, because a room drawn
/// on one shift and pressed on another disagreeing about who could hear you is this repo's third named bug
/// class aimed at a curtain.</para>
/// </summary>
public static class CabinetPrivacy
{
    /// <summary>Which of the two a cabinet is standing at. There is no third: an OPEN cabinet is a table in
    /// a hall, and #751 already has one of those.</summary>
    public enum Stage
    {
        /// <summary>The default, and the state every cabinet in the building is in until somebody touches
        /// it. Cloth across an open doorway: most of what happens behind it is unseen and unheard, and
        /// <i>most</i> is the whole of the mechanic.</summary>
        Curtain = 0,

        /// <summary>The card's own sentence, taken up. Absolute, and everybody in the hall watched you
        /// decide it.</summary>
        Door = 1,
    }

    /// <summary>#758 · Where a cabinet's stage is kept, keyed by the floor and the cabinet's own plate
    /// number — the same shape as <c>HiveInterior.CubicleKey</c>, and for its reason: two floors of one
    /// building must never share a door.</summary>
    public static string Key(int level, int cabinet) => $"cab:{level}:{cabinet}";

    // ── WHAT THE PLAN SHOWS. A GLYPH, AND NEVER A SENTENCE ────────────────────────────────────────────

    /// <summary>The weave, at console size — a cabinet standing as the building left it.</summary>
    public const string CurtainGlyph = "▨";

    /// <summary>The leaf, out of the wall and dogged. Solid where the other is woven, which is the whole of
    /// what a glance across a hall has to be able to tell apart.</summary>
    public const string DoorGlyph = "▮";

    /// <summary>Which glyph this cabinet wears.</summary>
    public static string GlyphFor(Stage stage) => stage == Stage.Door ? DoorGlyph : CurtainGlyph;

    /// <summary>#758 · What is stencilled beside a cabinet's door, with the one glyph that says what state
    /// it is in. The plate itself is <see cref="UndergroundComplex.CabinetPlate"/>'s and is not rewritten
    /// here: the room did not change its name when somebody pulled a curtain.</summary>
    public static string PlateFor(string cabinetPlate, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(cabinetPlate);
        return $"{cabinetPlate} {GlyphFor(stage)}";
    }

    // ── THE STRIP'S OWN BUTTON ────────────────────────────────────────────────────────────────────────

    /// <summary>#865 · The strip button at a cabinet table, when the door is dogged and the way back is the
    /// cloth.</summary>
    public const string DrawTheCurtainLabel = "Draw the curtain";

    /// <summary>…and when it is not, and the card's sentence is on offer.</summary>
    public const string DogTheDoorLabel = "Dog the door";

    /// <summary>Which of the two this cabinet offers. One button, because the stage it is IN is the one
    /// thing the button never has to say.</summary>
    public static string LabelFor(Stage stage) =>
        stage == Stage.Door ? DrawTheCurtainLabel : DogTheDoorLabel;

    /// <summary>The tooltip on the cloth. It says what cloth is, and nothing about odds.</summary>
    public const string DrawTheCurtainHint =
        "Let the leaf back into the wall and pull the weave across. Cloth is not steel.";

    /// <summary>…and on the leaf. It says the cost out loud, because the cost is the mechanic and a captain
    /// is allowed to know it before they press.</summary>
    public const string DogTheDoorHint =
        "Padded, and it dogs from inside. Everybody out there will watch it shut.";

    /// <summary>Which tooltip goes with <see cref="LabelFor"/>.</summary>
    public static string HintFor(Stage stage) =>
        stage == Stage.Door ? DrawTheCurtainHint : DogTheDoorHint;

    // ── WHAT IS SAID WHEN A HAND MOVES ────────────────────────────────────────────────────────────────

    /// <summary>Drawing it. The hall is still audible through the cloth, which is the honest description of
    /// the state and the only warning this mechanic ever gives.</summary>
    public const string CurtainDrawnLine =
        "🧵 You pull the weave across the opening. The hall does not stop — you can hear it through the " +
        "cloth, and it can hear you if it leans.";

    /// <summary>Dogging it. Two sentences: what the room becomes, and who saw it become that.</summary>
    public const string DoorDoggedLine =
        "🚪 The padded leaf comes out of the wall and dogs, and the hall goes off like a light. It went off " +
        "for the hall too, and every face in it looked up at the same time.";

    /// <summary>Which line the press says.</summary>
    public static string SaidOn(Stage stage) =>
        stage == Stage.Door ? DoorDoggedLine : CurtainDrawnLine;

    // ── THE COUNTER'S LONG MEMORY ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #758 · Does THIS change write a line in the counter's book? Only the leaf, and only on the way shut.
    ///
    /// <para>The curtain writes nothing anywhere — it is what the room already was — and letting the door
    /// back open writes nothing either, because the line that matters was written the moment it closed and a
    /// book does not un-write. One transition, one fact, and a caller that dogs the same cabinet twice in an
    /// evening is asking about a door that is already shut.</para>
    /// </summary>
    public static bool TheCounterWritesItDown(Stage from, Stage to) =>
        from == Stage.Curtain && to == Stage.Door;

    /// <summary>
    /// What the captain's own book keeps of it — the fact, never the mechanic, in the flat register
    /// <see cref="RipAndBin.SeenNote"/> established. It is the second thing this game has filed about being
    /// KNOWN somewhere.
    ///
    /// <para><b>#917 · AND IT ASKS WHETHER THERE IS A MAN THERE TO LOOK UP.</b> The keep tends this bar on
    /// the LIVING watches only (<see cref="Interior.TheKeep.KeptWatch"/>); on the dead ones the counter is
    /// self-service and #618's "nobody is back there" is literally true of the front as well. So the
    /// original sentence — <i>the keep behind the counter looked up</i> — named a man who is not there on
    /// two watches in six, which is a sentence and a sim disagreeing about a person: this repository's third
    /// named bug class, and the same fault the strip's cabinet clause was carrying about a leaf.</para>
    ///
    /// <para><b>THE FACT IS WRITTEN ON BOTH WATCHES.</b> Only the sentence forks. The counter's long memory
    /// is a ledger and not a witness statement — a door coming out of a wall is written down whether or not
    /// anybody was standing at the till when it happened, and the dead-watch line says exactly that and
    /// declines to say who wrote it. Owner-authored, 2026-08-17.</para>
    /// </summary>
    /// <param name="cabinet">Which cabinet, as the plate reads.</param>
    /// <param name="watch">The frozen watch — <c>Interior.PatronRota.WatchIndex</c>, the same number the
    /// hall was drawn on, never a live clock.</param>
    public static string WhoWasInsideNote(int cabinet, long watch) =>
        Interior.TheKeep.KeptWatch(watch)
            ? $"Dogged the door of cabinet {cabinet} from inside. The keep behind the counter looked up at "
                + "the sound and wrote something down without hurrying."
            : $"Dogged the door of cabinet {cabinet} from inside. There was nobody behind the counter to "
                + "look up. It was written down anyway.";

    // ── THE LEAK LAW ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The faces on the die the weave is read off. A thousand, so a chance stated in tenths of a
    /// percent is a chance the die can actually express — a d20 would round every band in this table onto
    /// the same two faces and the guard measuring them would be measuring nothing.</summary>
    public const int LeakSides = 1000;

    /// <summary>#758 · What a beat behind cloth leaks in an ECHOING hall — the small watches, when the room
    /// is a handful of tables and a man mopping. FLAGGED for the owner's tuning.</summary>
    public const double QuietestChance = 0.04;

    /// <summary>…and in a ROARING one, the middle of the day, when there is somebody standing at every wall
    /// waiting for a chair. FLAGGED for the owner's tuning.</summary>
    public const double LoudestChance = 0.28;

    /// <summary>
    /// #758 · THE ODDS ONE SENSITIVE BEAT BEHIND THE CURTAIN LEAKS, given how full the hall is.
    ///
    /// <para>Straight-line between <see cref="QuietestChance"/> and <see cref="LoudestChance"/> across
    /// <see cref="CanteenRegulars.WatchFill"/>'s own range, because the fiction is a straight line: more
    /// people walking past the cloth is more chances one of them hears the half sentence. There is no
    /// cleverness in here to be wrong about.</para>
    ///
    /// <para>A DOGGED door does not consult this at all — see <see cref="Leaks"/>, where the stage is
    /// answered before any die is cast, so "absolute" is a shape of the code and not a very small
    /// number.</para>
    /// </summary>
    public static double LeakChance(double watchFill) =>
        QuietestChance + ((LoudestChance - QuietestChance) * Math.Clamp(watchFill, 0.0, 1.0));

    /// <summary>The faces that count as a leak at this density — the chance, on the thousand-face die the
    /// roll is actually made with. Published so a guard can measure what the seed produces against what the
    /// law says, rather than against a second copy of the arithmetic.</summary>
    public static int LeakFaces(double watchFill) =>
        (int)Math.Round(LeakChance(watchFill) * LeakSides, MidpointRounding.AwayFromZero);

    /// <summary>
    /// #758 · DID THIS BEAT GET OUT?
    ///
    /// <para>Seeded on the site, the cabinet, the frozen watch and which beat this is — so the same beat in
    /// the same cabinet on the same shift always answers the same, a captain who walks out and back in has
    /// not re-rolled anything, and a guard can sweep it.</para>
    ///
    /// <para><b>Nothing is said on a true.</b> The caller files the fact and shuts up; see
    /// <see cref="BarkThatKnows"/> for where it comes back.</para>
    /// </summary>
    /// <param name="bodyId">The site whose hall this is.</param>
    /// <param name="cabinet">Which cabinet, as the plate reads.</param>
    /// <param name="watch">The frozen watch — <c>Interior.PatronRota.WatchIndex</c>, never a live clock.</param>
    /// <param name="beat">Which sensitive beat of this sitting it is. Two beats in one cabinet must not
    /// share an answer, or a captain would put down two files and get one coin flip.</param>
    /// <param name="stage">Cloth or leaf.</param>
    public static bool Leaks(string bodyId, int cabinet, long watch, int beat, Stage stage)
    {
        ArgumentNullException.ThrowIfNull(bodyId);

        // The absolute answer, before any die exists. A dogged cabinet is not a cabinet with long odds.
        if (stage == Stage.Door)
        {
            return false;
        }

        int face = DiceRule.Roll(
            DiceRule.Seed($"hive:cabinet:leak:{bodyId}:{cabinet}:{beat}", watch), LeakSides).Face;
        return face <= LeakFaces(SittingAlone.Fill(watch));
    }

    /// <summary>
    /// #758 · …AND HOW IT COMES BACK. A stranger, later, somewhere else in the building, saying a number
    /// they have no way of knowing and then declining to go on.
    ///
    /// <para>It never explains itself, it is never labelled a consequence, and nothing anywhere connects it
    /// to the afternoon it came from. A player who has been careless twice will work it out, and a player
    /// who has not will hear a man being incurious about somebody else's business, which is the most
    /// ordinary thing anybody says in this room.</para>
    /// </summary>
    public static string BarkThatKnows(int cabinet) =>
        $"Cabinet {cabinet}, was it? No — none of my business. It never is.";

    // ── WHO BRINGS YOU, AND WHAT THEY DO WITH THE DOOR ────────────────────────────────────────────────

    /// <summary>
    /// #758 · WHEN SOMEBODY ELSE WALKS YOU INTO A CABINET, THEY DECIDE — and which they decide is the
    /// cheapest characterisation in this game, because it costs one line and no dialogue at all.
    ///
    /// <para>Seeded on WHO they are and nothing else, so it is a fact ABOUT the person rather than about the
    /// evening: the fitter who dogs the door dogs it every single time, and the driver who leaves the cloth
    /// leaves it every single time. A captain who meets both learns which of them is frightened without
    /// either of them saying a word.</para>
    ///
    /// <para>#731's walkers are the ones who will call this. It ships ahead of them deliberately: the law
    /// belongs beside the two stages it chooses between, not inside whichever file eventually learns to
    /// walk somebody across a room.</para>
    /// </summary>
    /// <param name="who">Their plate — the string the hall already knows them by.</param>
    public static Stage EscortsStage(string who)
    {
        ArgumentNullException.ThrowIfNull(who);
        return DiceRule.Roll(DiceRule.Seed($"hive:cabinet:escort:{who}", 0), 2).Face == 1
            ? Stage.Curtain
            : Stage.Door;
    }

    /// <summary>#917 · Every watch the rota has, so a sweep of this feature's prose cannot miss the half of
    /// it that is only said when nobody is behind the counter.</summary>
    private static IEnumerable<long> EveryWatchTheRotaHas()
    {
        for (long watch = 0; watch < CanteenRegulars.WatchFill.Count; watch++)
        {
            yield return watch;
        }
    }

    /// <summary>Every authored sentence this feature owns, for the canon sweep — a method rather than a
    /// list, #709's discipline, so a line added below cannot be a line the sweep never sees.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return DrawTheCurtainLabel;
        yield return DogTheDoorLabel;
        yield return DrawTheCurtainHint;
        yield return DogTheDoorHint;
        yield return CurtainDrawnLine;
        yield return DoorDoggedLine;
        // Both watches, because the keep is only behind the counter on four of the six and the sweep must
        // walk the sentence that is said when he is not.
        foreach (long watch in EveryWatchTheRotaHas())
        {
            yield return WhoWasInsideNote(1, watch);
        }
        yield return BarkThatKnows(1);
    }
}

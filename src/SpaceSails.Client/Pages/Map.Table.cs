using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #746 · THE TABLE SCENE — sitting down with somebody, and the moves you have once you have.
///
/// <para>Owner, 2026-08-06 (work-break brief): <i>"bar interaction is the next big lane. Asking to sit is
/// missing... sit-down and drink-offer — in both directions — are how contact with new people begins."</i>
/// And the proto, in his own words: <i>"with all the charm we can muster, get the job that takes us
/// downstairs — and politely dodge the jobs that don't."</i></para>
///
/// <h3>What lives here and what deliberately does not</h3>
///
/// <para>This file opens a panel, presses buttons and applies the answers Core hands back. It decides
/// NOTHING. Which moves exist, what they cost, which are rolled, what a band grants, what anybody says —
/// all of it is <see cref="Encounter"/> and <see cref="CanteenTable"/>, because the day the guard stop
/// arrives it has to be a content file rather than a second copy of this method.</para>
///
/// <h3>#680's law, applied from birth</h3>
///
/// <para>Every outcome is stored on the open scene and rendered INSIDE the panel's own subtree. Not one of
/// them is pulsed: the pulse HUD renders under the modal backdrop and its blur, which is where #680 was
/// filed from, and this panel is up for the whole conversation. The field book still gets its notes through
/// <c>FileNote</c> — the #686 half — because the book must remember what the pulse never said.</para>
/// </summary>
public partial class Map
{
    /// <summary>The open table, or null. One at a time: you are sitting at it.</summary>
    private TableTalk? _table;

    /// <summary>#746 QA · <c>?roll=hi|lo</c> — force every band this session. It overrides the BAND and
    /// never the roll, so the dice still cast and the on-screen math still reads truthfully; what a tester
    /// watches play out is the scene a captain would get.</summary>
    private Encounter.Band? _rollCheat;

    /// <summary>#746 QA · <c>?tablescene=1</c> — the whole route, booted. Set in Map.Sim's cheat parse; read
    /// once, by the landing, to walk the last leg into the canteen.</summary>
    private bool _tableSceneCheat;

    /// <summary>#757 QA · <c>?tablescene=free</c> — the same route, but the last leg lands you at a top with
    /// NOBODY at it, which is the table this whole issue is named after.</summary>
    private bool _freeTableCheat;

    /// <summary>
    /// #784 QA · <c>?spread=1</c> — THE WHOLE PHASE-TWO LOOP, in thirty seconds.
    ///
    /// <para>Owner's own ask, filing the demo: <i>"We probably need a start point where we have things in our
    /// inventory we can process (when our HUD UI state is sitting down with enough privacy)."</i></para>
    ///
    /// <para>It boots the canteen route, walks to a CABINET top — the owner's canonical processing venue
    /// (<i>"that is the place I want to process inventory"</i>) and the one rung of the ladder that is
    /// private unconditionally — sits the captain down through the very handler [E] reaches, and puts three
    /// real finds in the sleeve. From there: [I], pick a paper, watch the dig bar, read the entry in the
    /// book. Nothing about the room, the watch or the rota is forced: which cabinet is free is the
    /// building's answer, and the papers are ordinary sleeve items with ordinary gists.</para>
    /// </summary>
    private bool _spreadCheat;

    /// <summary>
    /// #798 QA · <c>?rip=1</c> — THE DISPOSAL LOOP, in thirty seconds.
    ///
    /// <para>Everything <c>?spread=1</c> boots (the canteen route, three real finds in the sleeve) with the
    /// last leg walked somewhere else: to the standing spot the hall's own SLOP BIN publishes. Press I, press
    /// 🗑 on a paper, and the sheet is gone from the sleeve with the act filed in the book — and the CHUTE is
    /// at the other end of the same room, which is what makes the bin a choice.</para>
    ///
    /// <para>It forces nothing. Which bin, where it stands and what is stencilled on it are the building's
    /// answers, and whether anybody was watching is whatever the watch and the rota actually produce.</para>
    /// </summary>
    private bool _ripCheat;

    /// <summary>
    /// #741 QA · <c>?threads=1</c> — THE RED PEN, with a case already in the book.
    ///
    /// <para>Everything <c>?spread=1</c> boots (the cabinet, the docked strip, three finds in the sleeve),
    /// and then the thing the pen actually needs: a BOOK WITH SOMETHING IN IT. Six entries are pre-filed
    /// from two grounds the captain is not standing on, and there is a real rhyme running through them for
    /// a human eye to catch — see <see cref="PreFileTheCase"/>, which invents not one word of it.</para>
    ///
    /// <para>It forces nothing about the case. No line is drawn, no entry is marked and nothing is
    /// highlighted: spotting is the player's act, and a demo that pointed at the answer would be
    /// demonstrating the one thing this feature must never do.</para>
    /// </summary>
    private bool _threadsCheat;

    /// <summary>#751 QA · <c>?watch=N</c> — pin which shift the hall is on, so a tester can walk into the
    /// heaving one and the empty one without waiting four sim-hours between looks. Null off the cheat, in
    /// which case the watch is <see cref="PatronRota.WatchIndex"/> of the sim clock exactly as before.</summary>
    private long? _watchCheat;

    /// <summary>#757 QA · <c>?approach=1|0</c> — force the answer to WAITING at a table you took alone.
    ///
    /// <para>1 brings somebody over on the very next wait; 0 means nobody ever comes, which is the OTHER
    /// half of the feature and just as much a scene: an empty room on the wrong watch is the event. Without
    /// this the approach is a seeded roll at one top on one shift, and "sit down and press wait until the
    /// dice agree" is not a demo. #693's own rule, written in the file that then could not follow it: <i>a
    /// scene nobody can reach on demand is a scene that ships broken.</i></para>
    ///
    /// <para>It forces WHETHER, never WHO or WHAT — the ladder, her lines and what she wants are the ones a
    /// captain would get, because a cheat that showed a different scene is worse than no cheat.</para></summary>
    private bool? _approachCheat;

    /// <summary>
    /// #746 QA · Stand the captain IN the upper canteen when <c>?tablescene=1</c> asked for it.
    ///
    /// <para>ASKED OF THE BUILDING, not retyped. The room's centre is the amenity Core carved (#707) — the
    /// same coordinate the fixture console sits on, which is by construction clear of the counter it laid
    /// against the back wall. A cheat that typed its own spot in a room it does not own is §13.15's second
    /// cause, and this project has been set down inside a wall by exactly that mistake twice.</para>
    /// </summary>
    private void StandInTheCanteenIfAsked(SurfaceExcursion ex)
    {
        if (!_tableSceneCheat || ex.Floor >= 0)
        {
            return;
        }

        // #793 · …and ?park=1 WINS. ?spread=1 implies the canteen route (it was written when the only
        // private seat in the game was a cabinet), so `?park=1&spread=1` asks for both rooms at once and
        // somebody has to be first. The park is the more specific ask — it names a room — and the bench row
        // sits the captain down itself, so this one stands aside rather than sitting them in a cabinet the
        // next line would then walk them out of.
        if (_parkCheat)
        {
            return;
        }

        foreach (UndergroundComplex.Amenity a in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).Amenities)
        {
            if (a.Use != UndergroundComplex.Comfort.UpperCanteen)
            {
                continue;
            }

            // #798 · …or AT A BIN, on your feet, with papers in the sleeve — the disposal loop's own row.
            // FIRST, because it is the shortest walk and because it must not be able to be swallowed by the
            // cabinet branch below. Which bin, and where a captain stands to use it, are both the carve's
            // own published answers (RipAndBin.Bin.StandX/StandY) — a cheat that typed its own spot beside a
            // fixture it did not place is §13.15's second cause, and this project has been set down inside a
            // wall by exactly that mistake twice.
            if (_ripCheat && TheSlopBinIn(ex, a) is { } bin)
            {
                StandCaptainAt(bin.StandX, bin.StandY, "you stop beside the bin at the end of the counter");
                SeedTheSpreadFinds();
                ShowPulseMessage(
                    "🧪 DEV ?rip=1: standing at the canteen's slop bin with three finds in the sleeve. "
                    + "Press I and then 🗑 on a paper — the sheet goes, the book keeps what you dug. The "
                    + "waste chute is at the other end of the same room, and the paper bin is by the lift.");
                return;
            }

            // #784 · …or IN A CABINET, sat down, with papers in the sleeve — the phase-two loop's own row.
            // Same call, same frozen watch, same [E] handler; the only thing the cheat chooses is which of
            // the room's own free tops it walks to, and it asks for the QUIET one because privacy is the
            // gate the spread is about.
            if (_spreadCheat && FirstFreeTop(ex, a, quietOnly: true) is { } cabinet)
            {
                StandCaptainAt(cabinet.X, cabinet.Y, "you step into the cabinet and shut the door");
                SeedTheSpreadFinds();

                // #741 · …and, for ?threads=1, a book with a case already in it. Before the sit, so the
                // notebook is furnished by the time the strip is up.
                if (_threadsCheat)
                {
                    PreFileTheCase();
                }

                // Through TryTakeTable and not a hand-written sit: a cheat that assembled its own TableTalk
                // would be testing a table that does not ship (and would be this repo's first named bug
                // class, one posture over).
                TryTakeTable();

                if (_threadsCheat)
                {
                    // The pocket is opened straight onto the notebook's case reading. A dev row is allowed
                    // the two presses an honest open insists on (#690's reset lands every open on the
                    // pocket and on THIS GROUND) — and this ground has nothing in it, because the whole
                    // point of the case reading is entries from grounds you are not standing on.
                    _satchelPage = SatchelPage.Notes;
                    _notesView = NotesView.TheCase;
                    _showSatchel = true;

                    // SAID INSIDE THE DIALOG, not pulsed — #680/#736's law, and this row learned it the way
                    // this repo learns everything: by being looked at. A first descent raises its own card
                    // (#585) over the whole screen on this very boot, and the instruction pulse played and
                    // died under it, so the demo opened onto a notebook with nothing telling a tester what
                    // to press. The satchel's own outcome line is the one layer a card cannot cover, and it
                    // is still there when the card is closed.
                    _satchelOutcome =
                        "🧪 DEV ?threads=1: six entries PRE-FILED from two grounds you are not standing on "
                        + "(the ride down filed its own lines too). Take the 🖊 RED PEN, press one title, "
                        + "then another — a line goes between them and the list reorders around it. The same "
                        + "two presses take it off again.";
                    return;
                }

                ShowPulseMessage(
                    "🧪 DEV ?spread=1: sat down in a CABINET with three finds in the sleeve. The panel is a "
                    + "HUD strip now, not a card — press I, pick a paper, and watch the dig bar fill.");
                return;
            }

            // #757 · …or AT A FREE TOP, for ?tablescene=free. Which top that is comes off the very same
            // call the deck was drawn with, off the same frozen watch — the cheat picks one of the room's
            // own empty tables, and never a coordinate of its own (§13.15: the two times this project set
            // the captain down inside a wall, it was a caller typing geometry about a room it did not own).
            if (_freeTableCheat && FirstFreeTop(ex, a) is { } free)
            {
                ShowPulseMessage(
                    "🧪 DEV ?tablescene=free: a table with nobody at it. Press E to SIT DOWN, then SIT A WHILE.");
                StandCaptainAt(free.X, free.Y, "you step into the canteen");
                return;
            }

            ShowPulseMessage(
                "🧪 DEV ?tablescene=1: the upper canteen. Walk to a table with somebody at it and press E.");
            StandCaptainAt(a.X, a.Y, "you step into the canteen");
            return;
        }
    }

    /// <summary>
    /// #784 QA · Three things worth digging through, put in the sleeve for <c>?spread=1</c>.
    ///
    /// <para>Two papers and a file on somebody: those are the only two kinds
    /// <see cref="LeftBehind.GistOf"/> has a gist for, and the row exists to demonstrate the loop rather than
    /// to invent content, so it uses ordinary <see cref="Core.Satchel"/> items with ordinary ids. Added
    /// through <see cref="Core.Satchel.Add"/>, which is the one funnel every find in the game goes through
    /// — a cheat that pushed straight onto the list would be testing a sleeve with no capacity law in it.
    /// </para>
    /// </summary>
    private void SeedTheSpreadFinds()
    {
        IReadOnlyList<Core.Satchel.Item> sleeve = _satchel;
        sleeve = Core.Satchel.Add(sleeve, new Core.Satchel.Item(Core.Satchel.Kind.Paper, "spread-demo-1"));
        sleeve = Core.Satchel.Add(sleeve, new Core.Satchel.Item(Core.Satchel.Kind.Paper, "spread-demo-2"));
        sleeve = Core.Satchel.Add(sleeve, new Core.Satchel.Item(Core.Satchel.Kind.Dirt, "spread-demo-3"));
        _satchel = [.. sleeve];
    }

    /// <summary>
    /// #741 QA · A CASE ALREADY IN THE BOOK, with a rhyme in it a human eye can catch.
    ///
    /// <para>Owner's north star: <i>"we spot connections in the data… that is the gumshoe moment."</i> The
    /// pen is worth nothing against an empty book, and it is worth nothing against six unrelated lines
    /// either — so this row files a case whose entries genuinely rhyme.</para>
    ///
    /// <h3>The rhyme, and not one word of it is invented</h3>
    ///
    /// <para>Every sentence here is <see cref="FieldDossier.Debrief"/>'s, shipped since #588/#774, for two
    /// ordinary rooms on two ordinary grounds. The catch is the one the dossier has always quietly held:
    /// <b>the in that fell out of somebody's kit is that same dead person's own name</b> — the file says so
    /// in its own comment, <i>"nothing in the game ever remarks on this"</i> — and the next of kin still
    /// waiting for word shares their family name. So the captain reading the titles has a specialist, a
    /// family, and a phrase to drop at a door somewhere else, and one name is standing in all three.
    /// Nothing labels it. Nothing connects it.</para>
    ///
    /// <para>Two grounds, because a rhyme inside one place group is a rhyme the LAYOUT found rather than
    /// the captain. They are filed straight rather than through <see cref="FileNote"/> for the same reason:
    /// that door files to the ground underfoot, and the ground underfoot is a canteen twenty floors
    /// down.</para>
    ///
    /// <para>The bodies' sites are asked of Core (<see cref="Core.LandingSites.For"/>,
    /// <see cref="Core.BodyNames.Display"/>) rather than typed here — §13.15's rule one room over: a cheat
    /// that writes its own geography is a cheat testing a world that does not ship.</para>
    /// </summary>
    private void PreFileTheCase()
    {
        // NOT the clock. Nothing in this file may read the sim clock at all — the watch every table fact hangs off is
        // ex.CanteenWatch, frozen when the deck was welded, and a guard bans the clock from the whole file
        // rather than from one method (#746/#757). Caught by that guard, and the fixed base is the better
        // answer anyway: the demo's handles are then IDENTICAL on every boot, so a tester who draws lines,
        // reloads the same URL and finds them still there has learned something true about the vault.
        double at = 0.0;
        IReadOnlyList<Core.FieldNote> book = _fieldNotes;

        void FileFrom(string bodyId, int siteIndex, int roomIndex, int howMany)
        {
            Core.LandingSite site = Core.LandingSites.For(bodyId)[siteIndex];
            string place = Core.FieldNotes.PlaceLabel(Core.BodyNames.Display(bodyId), site.Name);

            int said = 0;
            foreach (Core.FieldDossier.Saying one in
                Core.FieldDossier.Debrief(bodyId, site.LayoutSalt, roomIndex, everySaying: true))
            {
                if (said++ >= howMany)
                {
                    break;
                }

                // Minutes apart, oldest first, the way an afternoon of turning rooms over reads.
                at += 240.0;
                book = Core.FieldNotes.Append(book, new Core.FieldNote(one.Text, at, place, one.Glyph));
            }
        }

        // A whole kit assembled in one room on the canon ground, and two lines out of another room a moon
        // away. Six entries: enough to have to look, few enough to read at phone size (#782).
        //
        // THESE THREE SEEDS ARE THE DEMO. What they yield is pinned in Core
        // (TheRedPenDrawsTheLineTests.TheDemoCase_...), and a source-shape guard holds this call site to
        // them — because a seed quietly changed is a demo that boots six lines with nothing in common, and
        // that failure is completely silent.
        FileFrom("miranda", 0, 3, 4);
        FileFrom("luna", 1, 22, 2);

        _fieldNotes = [.. book];
    }

    /// <summary>#798 QA · The slop bin standing inside THIS hall, or null. Asked of the floor plan and
    /// filtered by the hall's own box (<see cref="UndergroundComplex.Hall.Contains"/>), so the row cannot
    /// walk the captain to the paper bin by the lift and call it the canteen.</summary>
    private static RipAndBin.Bin? TheSlopBinIn(SurfaceExcursion ex, UndergroundComplex.Amenity a)
    {
        if (a.Hall is not { } hall)
        {
            return null;
        }
        foreach (RipAndBin.Bin bin in
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField()).TheBins)
        {
            if (bin.Tier == RipAndBin.Tier.SlopBin && hall.Contains(bin.X, bin.Y))
            {
                return bin;
            }
        }
        return null;
    }

    /// <summary>#757 QA · The first top in this room that nobody is at, this watch — Core's own list, so the
    /// cheat cannot disagree with the room about which tables are free.</summary>
    /// <param name="quietOnly">#784 · Only a CABINET top will do — the private end of the exposure ladder,
    /// which is what <c>?spread=1</c> is booting into.</param>
    private static CanteenRegulars.TableSeat? FirstFreeTop(
        SurfaceExcursion ex, UndergroundComplex.Amenity a, bool quietOnly = false)
    {
        foreach (CanteenRegulars.TableSeat top in
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
        {
            if (!top.Taken && (!quietOnly || top.Quiet))
            {
                return top;
            }
        }
        return null;
    }

    /// <summary>One conversation, with everything the panel needs to draw itself.</summary>
    private sealed class TableTalk
    {
        /// <summary>"watch:floor:tableIndex" — what every watch-scoped fact about this table is keyed on.</summary>
        public required string Key { get; init; }

        /// <summary>#757 · Which top this is, as Core's own ordinal. The key already carries it, and this
        /// carries it as a NUMBER because the approach roll is seeded on it — parsing an ordinal back out of
        /// a string we built ourselves is a second answer to a question we already had.</summary>
        public required int Index { get; init; }

        /// <summary>Which of the three is in the chair.</summary>
        public required CanteenTable.Who Who { get; init; }

        /// <summary>#757 · Their plate, exactly as it is drawn over them on the deck — or, at a table you
        /// took alone, whose table it is. Settable because ONE SITTING CAN CHANGE COUNTERPART: you sit down
        /// on your own, you wait, and somebody crosses the room and takes the chair opposite. That is one
        /// continuous occupation of one table, not two panels, and the alternative — closing this one and
        /// opening another — would blink the answer the player is reading off the screen (#680).</summary>
        public required string Plate { get; set; }

        /// <summary>The scene, straight off the content file. Settable for the same reason
        /// <see cref="Plate"/> is: waiting is a scene that can turn into a different scene at the same
        /// table.</summary>
        public required Encounter.Scene Scene { get; set; }

        /// <summary>#757 · Whether this is a table you took ALONE — nobody opposite, and WAIT is the whole
        /// of the verb. It is the one fact the panel needs that is not on the scene.</summary>
        public bool Solo { get; set; }

        /// <summary>#783 · Whether this sitting READS AS RELAXED — boots up on the spare chair, which is a
        /// different sentence, a different goodbye and a different picture.
        /// <see cref="SittingAlone.SitReadsAsRelaxed"/> decides it; this carries the answer, so the prose and
        /// the art cannot come to two different views of the same minute.
        ///
        /// <para>NOT the same question as #784's <see cref="CaptainIsRestingAtATable"/>, and deliberately
        /// named apart from it: every solo sit is a short REST for the body (that is #784's mechanic), while
        /// this is whether the sit reads relaxed in WORDS AND PICTURES. A back-to-the-wall watch still gives
        /// your breath back; it is simply not the sentence about boots.</para></summary>
        public bool Relaxed { get; set; }

        /// <summary>#783 · …and whether there is a bought pour in it, which adds the drink's own line.</summary>
        public bool DrinkInHand { get; set; }

        /// <summary>#783 · The picture the panel wears, or null for a table that is somebody else's. Owner,
        /// live at a taken table: <i>"the pop up could have Gen AI here."</i> Two states, two images, off the
        /// one <see cref="Relaxed"/> answer above.</summary>
        /// <para>#793 · A BENCH WEARS NEITHER. Both pictures are photographs of a canteen table — an empty
        /// chair pulled out opposite, or boots up on that same chair — and a park bench has no chair
        /// opposite and no table to put anything on. A panel drawing one of them over a sit on gravel would
        /// be the picture and the sentence disagreeing, which is the fault this very field was added to
        /// avoid. A bench is always the DOCKED strip anyway (it is never a conversation), and the strip
        /// draws no art at all.</para>
        public string? ArtUrl =>
            !Bench && Who == CanteenTable.Who.None ? SittingAlone.ArtFor(Relaxed) : null;

        /// <summary>How many the top seats, and how many chairs are still empty — the fact that let you
        /// ask to join in the first place, kept so the panel can say it.</summary>
        public required int Seats { get; init; }

        /// <summary>Chairs nobody is in. #757 · Settable, because the captain sitting down is one of them
        /// and somebody joining them is another — occupancy the player can count.</summary>
        public required int Free { get; set; }

        /// <summary>#751 · Their bark, for a background patron — drawn by Core per patron per watch, and
        /// carried here because it is the only thing a stranger has to say.</summary>
        public string? Bark { get; init; }

        /// <summary>#751 · Whether this table is in a CABINET, which is the one fact the quiet rule reads.
        /// Core's own (<see cref="CanteenRegulars.TableSeat.Quiet"/>) — a client flag would be a second
        /// answer to a question about a room the client does not own.</summary>
        public bool Quiet { get; init; }

        /// <summary>
        /// #793 · Whether this seat is a PARK BENCH rather than a canteen top.
        ///
        /// <para>The same scene machinery holds a bench — one sitting, one panel, one WAIT beat, one short
        /// rest — because it IS the same posture, and a second seated panel beside this one would be a second
        /// answer to "is the captain sitting down". What the flag buys is the three places a bench is
        /// genuinely a different seat: which rung of the exposure ladder it is
        /// (<c>Map.Seated.SeatedIn</c>), what the room says when nobody comes (a park is not a hall), and
        /// what somebody arriving DOES — on a bench they sit down beside you and say nothing, which is not a
        /// conversation and must not raise one.</para>
        /// </summary>
        public bool Bench { get; init; }

        /// <summary>#793 · Which bench, as the park's own ordinal — for the deck and for the identity of the
        /// seat. <see cref="Index"/> carries the APPROACH ordinal instead, which is deliberately a different
        /// number (see <see cref="ParkBenches.ApproachOrdinal"/>).</summary>
        public int BenchIndex { get; init; }

        /// <summary>
        /// #817 · Whether this seat is a CHAIR AT A DESK in one of the park-view suites.
        ///
        /// <para>The same machinery again, and for the owner's own stated reason — an office table is the
        /// restaurant's table <i>"just more rectangular"</i> with <i>"the functionality … about the same
        /// otherwise"</i>. What the flag buys is the two places an office genuinely differs: NOBODY EVER
        /// COMES OVER (the staff of this building are somewhere else, and the canteen's recruiter walking
        /// across an empty office would be the scene and the room disagreeing), and the silence is described
        /// in an office's own words rather than a hall's.</para>
        /// </summary>
        public bool Office { get; init; }

        /// <summary>
        /// #820 · WHERE STANDING UP PUTS THE BODY — the square this seat is stepped off onto.
        ///
        /// <para>Worked out from published geometry at the moment the captain sat down and carried here, so
        /// that standing up does not have to go and look the furniture up a second time (and cannot come to
        /// a different answer if the watch has turned over in between). It is the seat's own square wherever
        /// a seat is a place you could have been standing anyway — a ring-office chair
        /// (<see cref="RingOffice.Chair.StandAt"/>), a chair round a canteen top — and the WALK SIDE of the
        /// plank at a park bench, which is solid and would otherwise close over the dot the moment the
        /// sitting ended.</para>
        ///
        /// <para>Null at a sitting that never moved the body, which is no sitting that ships today; the
        /// standing simply leaves the captain where they are rather than inventing a square for them.</para>
        /// </summary>
        public (double X, double Y)? StepOff { get; init; }

        /// <summary>
        /// #793 · SOMEBODY IS ON THE OTHER END OF THIS BENCH.
        ///
        /// <para>Deliberately NOT <see cref="Solo"/>, and the distinction is the whole of the bench rung.
        /// <c>Solo</c> means <i>this is not a conversation</i> — it is what decides whether the frame docks
        /// or the full card comes up, and whether the wait beat and the short rest run at all. A stranger who
        /// sits down at the far end of a plank has started no conversation with anybody; you are still
        /// sitting, still resting, still findable. What you have lost is PRIVACY, and this is the flag that
        /// says so — the one <c>SeatedAlone</c> reads for the spread.</para>
        ///
        /// <para>Settable, because one sitting can change occupancy in both directions exactly as a table's
        /// chair opposite can.</para>
        /// </summary>
        public bool SharedSeat { get; set; }

        /// <summary>#680 · What the last move answered, said HERE and nowhere else.</summary>
        public string? Outcome { get; set; }

        /// <summary>The dice, spelled out, when a roll decided it — §5.0's whole homage is that the player
        /// watches the numbers add up.</summary>
        public string? Math { get; set; }

        /// <summary>Whether the satchel sub-list is fanned open under "put something on the table".</summary>
        public bool Showing { get; set; }

        /// <summary>#746 · Whether the captain has actually ASKED yet.
        ///
        /// <para>The press puts you at the table; asking to join is its own beat, because that is the beat
        /// the owner said was missing (<i>"asking to sit is missing"</i>). No roll — sitting is cheap in bar
        /// culture and most working people wave you in — but it is a thing you DO, and the wave-in is the
        /// answer to it rather than a caption that was always there. A panel that opened straight into the
        /// moves would have skipped the only part of this scene the issue is named after.</para></summary>
        public bool Joined { get; set; }

        /// <summary>#749 · What has been said in THIS sitting, by move id.
        ///
        /// <para>Deliberately beside <c>ex.TableMoves</c> rather than instead of it, because they are two
        /// different facts and the bug was reading one for the other. The excursion's set is what the ROOM
        /// remembers for the watch — a round stood, an ask fumbled, a file put down — and it must outlive
        /// standing up. This one is the CONVERSATION, and it dies with the panel: you cannot answer an offer
        /// that was made before you left the table, because the man made it to somebody who then stood up.</para></summary>
        public HashSet<string> Said { get; } = [];
    }

    /// <summary>What a table's watch-scoped state is keyed on. An ORDINAL and never a position: two doubles
    /// compared with a tolerance is a guess, and Core hands the ordinal over for free.</summary>
    private static string TableKey(SurfaceExcursion ex, int tableIndex) =>
        $"{ex.CanteenWatch}:{ex.Floor}:{tableIndex}";

    /// <summary>Whether a move has already been made at this table this watch.</summary>
    private static string MoveKey(TableTalk t, string moveId) => $"{t.Key}:{t.Who}:{moveId}";

    // ── OPENING THE SCENE ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #746 · Stand at a table with a free seat and ask to join.
    ///
    /// <para>WHICH TABLE AND WHO IS AT IT COME FROM CORE — <see cref="CanteenRegulars.Tables"/>, the same
    /// call the renderer drew the room with, off the same frozen watch. Matching by position against the
    /// console the press landed on is the only geometry here, and it is a lookup rather than a decision.</para>
    ///
    /// <para>Only the three wired regulars are a scene. The rest of #709's cast keep their one breath: they
    /// are the room being a room, and a canteen where every stranger has a conversation tree is a corridor
    /// with quest-givers in it.</para>
    /// </summary>
    private bool TryOpenTable()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        // Already sitting there. The press is CONSUMED and nothing happens — re-opening would wipe the
        // outcome line the captain is in the middle of reading, and E is not how you stand up: "Take your
        // leave" is, because leaving a table right is a thing this scene has an opinion about.
        if (_table is not null)
        {
            return true;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveRegular } spot)
        {
            return false;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            foreach (CanteenRegulars.TableSeat top in
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                // #751 · WHICH TIER, off Core's own list. A background patron is a Stranger and gets the
                // thin scene; one of the ten named regulars is matched by their plate exactly as before.
                CanteenTable.Who who = top.Stranger
                    ? CanteenTable.Who.Stranger
                    : CanteenTable.WhoIs(top.Plate);
                if (who == CanteenTable.Who.None || top.Free <= 0 || top.Plate is not { } plate)
                {
                    return false;   // somebody who is not a scene, or a top with nowhere left to sit.
                }

                // #820 · WHICH CHAIR, off Core's own ring, read before the body moves. The nearest one the
                // party is not already in — a captain waved into a seat that had somebody in it would be
                // the drawn room and the pressed room disagreeing about a lap (#823's own complaint).
                (double X, double Y)? chair = top.ChairYouTake(_avatarX, _avatarY);
                if (chair is { } sit)
                {
                    SitCaptainOn(sit.X, sit.Y);
                }

                _table = new TableTalk
                {
                    Key = TableKey(ex, top.Index),
                    Index = top.Index,
                    // …and standing up leaves the captain on the chair's own square. A canteen top is drawn
                    // and does not collide, so the seat is floor and nothing has to be stepped off — the
                    // square is carried all the same, because it is StandCaptainAt that gets the nudge's
                    // opinion on whether the room agrees.
                    StepOff = chair,
                    Who = who,
                    Plate = plate,
                    Scene = who == CanteenTable.Who.Stranger
                        ? CanteenTable.StrangerScene(plate)
                        : CanteenTable.SceneFor(who, ex.TableTempOverheard),
                    Seats = top.Seats,
                    Free = top.Free,
                    Bark = top.Line,
                    Quiet = top.Quiet,
                };
                RendererInterop.PlayCue("reveal");
                StateHasChanged();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// #757 · TAKE A FREE TABLE — the normal way to operate in a bar, and the one the room refused.
    ///
    /// <para>Owner, live in the hall: <i>"I have empty table but I cannot sit down."</i> #746's press needs a
    /// counterpart because its whole verb is <b>ask to join</b>; an empty top had no console over it at all,
    /// so [E] there answered nothing — an absence rather than a refusal, which is the one kind of "no" a
    /// player cannot read.</para>
    ///
    /// <para>SAME POSTURE, SAME GEOMETRY, and deliberately no new ones. Which table it is comes off Core's
    /// own list (<see cref="CanteenRegulars.Tables"/>), off the frozen watch, matched to the console the
    /// press landed on — a lookup rather than a decision — and #820's snap puts the captain in one of that
    /// top's own published chairs, exactly as it does at an occupied table. Not one coordinate below was
    /// measured here, which is §13.15's whole point: this project has set a captain down inside a wall twice
    /// by letting a caller do arithmetic about a room it did not carve.</para>
    /// </summary>
    private bool TryTakeTable()
    {
        if (_surface is not { } ex || ex.Floor >= 0)
        {
            return false;
        }

        // Already sitting. The press is CONSUMED — E is not how you stand up, "take your leave" is.
        if (_table is not null)
        {
            return true;
        }

        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not
            { Kind: DeckPlan.ConsoleKind.HiveTable } spot)
        {
            return false;
        }

        UndergroundComplex.FloorPlan floor =
            UndergroundComplex.Build(ex.Stop.Body.Id, ex.Floor, MoonSurface.ExpeditionField());
        foreach (UndergroundComplex.Amenity a in floor.Amenities)
        {
            foreach (CanteenRegulars.TableSeat top in
                CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch))
            {
                if (Math.Abs(top.X - spot.X) >= 0.5 || Math.Abs(top.Y - spot.Y) >= 0.5)
                {
                    continue;
                }

                if (top.Taken)
                {
                    return false;   // somebody is there after all: that is #746's press, not this one.
                }

                // #783 · WHICH REGISTER THIS SIT IS IN, decided once, by Core, off the room and the glass.
                // Owner: "with a bought drink in hand, or on a quiet watch, the sit becomes the other thing."
                // #783/#784 · ONE reading of the counter's pour, and it is #784's — Map.Seated.cs owns the
                // window, excludes a drunk captain and is the same fact the short rest doubles its rate on.
                // A second window here would let the panel say "cold glass" on a beat the rest engine had
                // already decided there was no pour, which is the fault canon review caught in this scene.
                bool drink = APourInFrontOfYou;
                bool relaxed = SittingAlone.SitReadsAsRelaxed(drink, ex.CanteenWatch);
                Encounter.Scene sat = SittingAlone.TheTable(relaxed, drink);

                // #820 · …and the same snap as at an occupied top, which is the point of it being one law:
                // an empty table has every chair free, so this is simply the one the captain walked up to.
                (double X, double Y)? chair = top.ChairYouTake(_avatarX, _avatarY);
                if (chair is { } sit)
                {
                    SitCaptainOn(sit.X, sit.Y);
                }

                _table = new TableTalk
                {
                    Key = TableKey(ex, top.Index),
                    Index = top.Index,
                    StepOff = chair,
                    Who = CanteenTable.Who.None,
                    Plate = SittingAlone.OwnTablePlate,
                    Scene = sat,
                    Seats = top.Seats,
                    // One of them is yours now. The room can see you sitting alone, which is the whole
                    // premise (#757: "sitting alone is STATE"), and the panel says it in chairs.
                    Free = Math.Max(0, top.Seats - 1),
                    Quiet = top.Quiet,
                    Solo = true,
                    Relaxed = relaxed,
                    DrinkInHand = drink,
                    // Nobody to ask. #746's ask-to-join beat is the answer to a person, and there is not one
                    // here — the table is simply taken, and the taking is the scene's opening line.
                    Joined = true,
                    // #783 · …and it is the SCENE's opening, never a constant this method reached for. The
                    // owner's first-line law ("the panel's FIRST line must confirm the state change") is
                    // kept by the content file, and a client that pinned one of the two registers here
                    // would print the wary line over a picture of somebody's boots.
                    Outcome = sat.Opening,
                };
                RendererInterop.PlayCue("reveal");
                StateHasChanged();
                return true;
            }
        }

        return false;
    }

    /// <summary>Stand up. Free, always, and it is the only way the panel shuts — the backdrop click and the
    /// Close button both come through here, so leaving a table is one act however you do it.
    ///
    /// <para>#820 · …and it also STEPS THE CAPTAIN OFF THE SEAT: Core's own published square, carried on the
    /// sitting (<see cref="TableTalk.StepOff"/>) rather than worked out here, and gone to through
    /// <c>StandCaptainAt</c> so the nudge has its say. That is the whole reason a solid seat — a park bench
    /// is a segment in the collision field — cannot close over the dot when the sitting ends.</para>
    ///
    /// <para>THE ORDER OF THE THREE STATEMENTS BELOW IS THE WHOLE OF THIS COMMENT. The abandon line needs
    /// the strip to land on, so the table may not go first; <c>StandCaptainAt</c> rebuilds the deck and can
    /// put a line of its own on the screen, so it may not run while the strip is still up. Watched go red as
    /// <c>THE_DIG … the table is gone before the abandon line has a strip to land on</c>.</para></summary>
    private void CloseTable()
    {
        // #784 · A SPREAD IS A SPREAD ON A TABLE. Standing up with a write-up half dug abandons it, out loud
        // and with nothing filed — the same promise every other interruption of #696's hold makes, spoken in
        // the seated register (Processing.Interruption.StoodUp). Done BEFORE the table goes, so the line
        // still has the strip to land on.
        if (_surface is { Processing.Work: Core.Processing.Work.Write } ex)
        {
            AbandonProcessing(ex, Core.Processing.Interruption.StoodUp);
        }

        // #820 · Read, then the table goes, then the body moves. See the summary.
        (double X, double Y)? step = _table?.StepOff;

        _table = null;

        if (step is { } spot)
        {
            StandCaptainAt(spot.X, spot.Y, "you push the seat back and stand up");
        }
    }

    /// <summary>
    /// #746 · A move, pressed with the MOUSE — and the way home when it shuts the panel.
    ///
    /// <para>The seam <c>Dismiss</c> documents one file over: <i>"only the mouse needs the way home."</i> A
    /// keyboard path already owns focus; a click leaves it on the button that has just stopped existing, and
    /// the deck goes deaf — the captain presses W and nothing walks. Found by playing it: after "Take your
    /// leave" the map took no keys at all until it was clicked.</para>
    ///
    /// <para>Only when the panel actually went away. A move that keeps it up must NOT steal focus back, or
    /// tabbing through the moves would fight the map for every press.</para>
    /// </summary>
    private async Task TableMoveClicked(string moveId)
    {
        TableMove(moveId);
        if (_table is null)
        {
            await RefocusMap();
        }
    }

    // ── THE SITUATION THE DICE SEE ────────────────────────────────────────────────────────────────────

    /// <summary>The last ten minutes, as facts. Every one of them is something the player did and could
    /// narrate back — which is the difference between this and a character sheet.</summary>
    private Encounter.Situation TableSituation(SurfaceExcursion ex, TableTalk t) => new(
        RoundBought: ex.TableRounds.Contains(t.Key),
        PaperShown: ex.TableMoves.Contains(MoveKey(t, CanteenTable.Show + ":relevant")),
        HouseWaysLearned: ex.TableHouseWays,
        NerveMarked: Encounter.NerveReadsAcrossATable(_nerve),
        Fumbled: ex.TableHardened.Contains(t.Key));

    /// <summary>Is this move on offer right now? Core's own requirement check, plus the one fact that is
    /// about the ROOM rather than about the move: a LOUD file shuts ask-about-work at this table for the
    /// watch, and a shut ask is disabled with a reason rather than quietly missing.</summary>
    private bool TableMoveAvailable(SurfaceExcursion ex, TableTalk t, Encounter.Move move)
    {
        if (move.Id == CanteenTable.Work && ex.TableAskShut.Contains(t.Key))
        {
            return false;
        }
        var made = new List<string>();
        foreach (Encounter.Move m in t.Scene.Moves)
        {
            if (ex.TableMoves.Contains(MoveKey(t, m.Id)))
            {
                made.Add(m.Id);
            }
        }
        // #749 · Both sets, and they are not the same set. The watch's is what the room remembers; the
        // sitting's is what has actually been SAID in front of you, which is the only thing an answer can be
        // an answer to.
        return Encounter.Available(move, _credits, _satchel, made, t.Said);
    }

    /// <summary>Why a move is not on offer. #603's founding law one layer up: a control that does nothing
    /// and says nothing is indistinguishable from a bug.</summary>
    private string TableMoveWhyNot(SurfaceExcursion ex, TableTalk t, Encounter.Move move)
    {
        if (move.Id == CanteenTable.Work && ex.TableAskShut.Contains(t.Key))
        {
            return "Not after what you just put on the table. Not at this table, not this shift.";
        }
        return move.Needs switch
        {
            Encounter.Requirement.Credits => $"You are short of the {move.Credits} cr.",
            Encounter.Requirement.SatchelItem => "Your pockets are empty.",
            Encounter.Requirement.PriorMoveThisWatch when move.Id == CanteenTable.SmallTalkAgain
                => "They have not got that far with you yet.",
            _ => "Not yet.",
        };
    }

    // ── WHAT THE PANEL ASKS ───────────────────────────────────────────────────────────────────────────
    //
    // Three one-liners so the razor never has to hold a SurfaceExcursion or a TableTalk in a local. Same
    // discipline the rest of this page uses: the markup asks questions, it does not compute answers.

    /// <summary>#749 · The moves that are ON THE TABLE — the ones that exist to be pressed at all. Core's
    /// own call, so the day a checkpoint renders through this same block it cannot have a different idea of
    /// when an answer exists. Everything the captain has simply not earned is still HERE and still refused
    /// out loud (#603); what is missing is only what nobody has said yet.</summary>
    private IReadOnlyList<Encounter.Move> TableMovesOnTheTable() =>
        _table is { } t ? Encounter.OnTheTable(t.Scene, t.Said) : [];

    /// <summary>Is this move on offer?</summary>
    private bool TableMoveOnOffer(Encounter.Move move) =>
        _surface is { } ex && _table is { } t && TableMoveAvailable(ex, t, move);

    /// <summary>Why not, said out loud on the disabled control.</summary>
    private string TableMoveRefusal(Encounter.Move move) =>
        _surface is { } ex && _table is { } t ? TableMoveWhyNot(ex, t, move) : "Not yet.";

    /// <summary>#746 · Is the game NUDGING you at this move? Exactly one does: the fitter's ask, after the
    /// hand has waved you toward it. That is the NO-AND's "another door opens in the conversation" made
    /// visible — the scene moved, and the panel should look like it moved.</summary>
    private bool TableMoveIsUrged(Encounter.Move move) =>
        _surface is { } ex && _table is { Who: CanteenTable.Who.Fitter } t
        && ex.TableFitterOpen
        && move.Id == CanteenTable.Work
        && !ex.TableMoves.Contains(MoveKey(t, CanteenTable.Work));

    // ── MAKING A MOVE ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #746 · One move at the table.
    ///
    /// <para>Every branch ends at <see cref="TableAnswered"/>, which is the one place an outcome becomes
    /// words on the screen — so there is exactly one method that can put #680's law back in the pulse.</para>
    /// </summary>
    private void TableMove(string moveId)
    {
        if (_surface is not { } ex || _table is not { } t)
        {
            return;
        }

        // Leaving is free and never penalised. First, so nothing below can ever grow a price on it.
        if (moveId == CanteenTable.Leave)
        {
            // #757 · …and the SCENE says what leaving it looks like. Standing up from a table you took
            // alone is not the same sentence as standing up from somebody else's — one is a courtesy, the
            // other is just a chair going back under a table — and which it is belongs to the content file
            // rather than to a constant this method reached for.
            CanteenTable.Answer bye = TheLeaveMove(t) is { } goodbye
                ? CanteenTable.SaidPlainly(goodbye)
                : CanteenTable.TookTheirLeave();
            CloseTable();
            // The ONE pulse in this scene, and it is correct precisely because the panel has just gone:
            // there is no dialog subtree left to say it in. #680 is about which surface the player is
            // looking at, never about pulses being wrong.
            ShowPulseMessage(bye.Line);
            return;
        }

        // Asking for the chair. No roll, no cost — and the wave-in is the ANSWER to it, said inside the
        // panel like every other answer at this table (#680). Nothing durable changed, so nothing is saved:
        // a captain who reloads is standing at the table again, which is where they were.
        if (moveId == CanteenTable.Join)
        {
            t.Joined = true;
            t.Outcome = t.Scene.Opening;
            return;
        }

        if (moveId == CanteenTable.Show)
        {
            t.Showing = !t.Showing;
            t.Math = null;
            return;
        }

        // #757 · WAIT — the passive verb, and the only one a table you took alone has. Its own branch and
        // not a fixed outcome, because what it says is decided by the ROOM at the moment it is pressed.
        if (moveId == SittingAlone.Wait)
        {
            TableWaited(ex, t);
            return;
        }

        Encounter.Move move = default;
        bool found = false;
        foreach (Encounter.Move m in t.Scene.Moves)
        {
            if (string.Equals(m.Id, moveId, StringComparison.Ordinal))
            {
                (move, found) = (m, true);
                break;
            }
        }
        if (!found || !TableMoveAvailable(ex, t, move))
        {
            return;
        }

        switch (moveId)
        {
            case CanteenTable.SmallTalk:
            case CanteenTable.SmallTalkAgain:
                // #751 · A stranger says the one thing they were dealt this watch. Core drew it (per
                // patron, per watch); this only hands it back.
                TableAnswered(ex, t, moveId,
                    t.Who == CanteenTable.Who.Stranger
                        ? CanteenTable.StrangerSaid(t.Bark ?? "")
                        : CanteenTable.MadeSmallTalk(t.Who, moveId == CanteenTable.SmallTalkAgain));
                return;

            case CanteenTable.Round:
                // Bought BEFORE the answer, because the answer is the glasses arriving. The +1 it buys is
                // read off ex.TableRounds by every ask afterwards at THIS table — the drink walks over from
                // the counter fixture Core already put in this room, not from a menu in the abstract.
                _credits -= move.Credits;
                ex.TableRounds.Add(t.Key);
                TableAnswered(ex, t, moveId, CanteenTable.BoughtTheRound());
                return;

            case CanteenTable.TakeScaffold:
                TableAnswered(ex, t, moveId, CanteenTable.ScaffoldTaken());
                return;

            case CanteenTable.Work:
                TableAsksAboutWork(ex, t, move);
                return;

            default:
                // #749/#680 · A FIXED OUTCOME IS STILL AN ANSWER, and it speaks because the move CARRIES a
                // line — never because somebody wrote its id a case down here.
                //
                // This is the path the dodge falls down, and it is the whole of the fix: the switch above
                // used to enumerate ids and drop everything else off the end in silence, so
                // Encounter.Move.Says — the framework's own "the outcome is FIXED" field, the one a guard
                // stop's content will be written on — reached the screen for exactly the moves a client
                // author had remembered. THE DODGE IS STILL FREE: the answer built here has every field but
                // the line at its default, which is the nothing the owner smoke-tests by hand.
                if (move.Says is { Length: > 0 })
                {
                    TableAnswered(ex, t, moveId, CanteenTable.SaidPlainly(move));
                }

                // #757 · …and one of those fixed outcomes ENDS A VISIT rather than the sitting. Waving
                // somebody off is free (it is a refusal, and refusals are free here), it says what it says
                // through the one ending above like everything else, and then the table is yours again —
                // the panel never blinks, because it was one occupation of one table all along.
                if (moveId == SittingAlone.WaveOff)
                {
                    BackToYourOwnTable(ex, t);
                }
                return;
        }
    }

    /// <summary>
    /// #746 · The ask. The Fitter's is offered plainly; the Hand's is the one rolled move at this table.
    /// </summary>
    private void TableAsksAboutWork(SurfaceExcursion ex, TableTalk t, Encounter.Move move)
    {
        if (!move.Rolled)
        {
            TableAnswered(ex, t, move.Id, CanteenTable.FitterAsksAboutWork());
            return;
        }

        // The deep card on the table settles it without a roll: fear, not friendship. Core decides that this
        // is what that card does — the client only notices it is down.
        Encounter.Situation situation = TableSituation(ex, t);
        if (situation.PaperShown)
        {
            TableAnswered(ex, t, move.Id, CanteenTable.HandAsksAboutWork(Encounter.Band.YesBut));
            return;
        }

        // ATTEMPT INDEX, so a second ask at a different table this watch is a different roll and pressing the
        // same button twice is not. The counter is the count of asks already made this watch, which is what
        // the fumble set already remembers.
        int attempt = ex.TableHardened.Contains(t.Key) ? 1 : 0;
        DiceRoll roll = Encounter.Roll(
            ex.Stop.Body.Id, ex.Floor, t.Who.ToString(), move.Id, attempt, situation);
        Encounter.Band band = Encounter.Settle(roll, _rollCheat);

        t.Math = roll.Describe();
        TableAnswered(ex, t, move.Id, CanteenTable.HandAsksAboutWork(band));
    }

    // ── #757 · WAITING, AND WHO COMES OF IT ───────────────────────────────────────────────────────────
    //
    // Owner: "Suppose I just want to sit down and wait to be disturbed?" — so this is not a filler button.
    // Sitting at a table on your own is a choice to be FINDABLE, and the wait is the game asking the room
    // whether it has anything for you. On the right watch it does. On the wrong one it does not, and being
    // told so in an eighty-seat hall that used to be loud IS the event.

    /// <summary>The scene's own leave move, if it has one. <see cref="Encounter.CanAlwaysLeave"/>'s law says
    /// every scene must, so this is a lookup rather than a doubt.</summary>
    private static Encounter.Move? TheLeaveMove(TableTalk t)
    {
        foreach (Encounter.Move m in t.Scene.Moves ?? [])
        {
            if (string.Equals(m.Id, Encounter.Leave, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    /// <summary>
    /// #757 · One beat of holding a table. Core decides whether anybody crosses the room; this counts the
    /// beats and applies the answer.
    ///
    /// <para>THE BEAT COUNTER IS THE ROOM'S, not the sitting's (<c>ex.TableWaits</c>), and that is the whole
    /// anti-abuse law: the approach is seeded on (site, floor, top, watch, beat), so a captain who stood up
    /// and sat down again to get a different answer would simply carry on from the beat they were on. There
    /// is no way to re-press your way into company, which is the same rule every other roll in this game
    /// keeps.</para>
    ///
    /// <para>AND THE WATCH IS NOT TOUCHED. A wait is a beat inside the frozen shift (#709), never a nudge to
    /// the clock — a wait that re-dated the room would make the drawn room and the pressed room two rooms,
    /// which is this project's third named bug class.</para>
    /// </summary>
    private void TableWaited(SurfaceExcursion ex, TableTalk t)
    {
        ex.TableWaits.TryGetValue(t.Key, out int beat);
        ex.TableWaits[t.Key] = beat + 1;

        // #784 · THE BEAT IS ALSO A SHORT REST. Owner: "Sitting down relaxes and heals" / "it is like short
        // rest in TTRPG." The wait already IS the seated watch-beat, so the recovery hangs off it rather
        // than off a second clock — Map.Seated.cs owns the arithmetic and the ceiling.
        string? rested = RestOneSeatedBeat(ex, beat);

        // #793 · …AND ON A BENCH THE BEAT IS ALSO A LOOK. Owner: "it is a good gumshoe move to see if anyone
        // is following us by foot, as they would need to stop moving also." Sitting still is what makes the
        // reading possible, so the reading is taken on the beat you spend sitting still — never on the press
        // that sat you down, which would be an answer to a question nobody had asked yet.
        string? seen = t.Bench ? TheTailReading() : null;

        // One approach per top per watch. She came over, and whichever way that went, it went.
        //
        // #793 · …and a bench with somebody already on the far end has nowhere to put a third person. Core's
        // own arithmetic (two ends, one of them yours), asked rather than assumed — a wait that dealt an
        // arrival onto a full plank would be the panel claiming an occupancy the room does not have.
        //
        // #817 · …and NOBODY COMES INTO AN OFFICE. The staff of this building are somewhere else on a shift
        // this facility no longer runs; a stranger crossing a private suite to offer the captain work would
        // be the canteen's own scene played in a room whose whole tell is that it is empty.
        bool comes = !t.Office
            && (!t.Bench || ParkBenches.TheOtherEndIsFree(t.SharedSeat))
            && !ex.TableApproached.Contains(t.Key)
            && (_approachCheat
                ?? SittingAlone.SomebodyComes(
                    ex.Stop.Body.Id, ex.Floor, t.Index, ex.CanteenWatch, beat, t.Quiet));

        if (!comes)
        {
            // #680/#736 · NOTHING HAPPENING IS AN ANSWER, and it is said on the panel the captain pressed,
            // through the one ending every other answer at this table uses. A wait that produced silence
            // and no words would be indistinguishable from a control that is broken (#603).
            // #784 · …with the body's footnote after it, when the beat gave something back. The silence is
            // the EVENT and it keeps the first sentence; the rest is one clause added to it, and never a
            // second panel line competing with the room's own answer.
            //
            // Composed INSIDE the call rather than hoisted into a local, deliberately: #778's own guard
            // reads the ordering here to prove the nobody-came line goes through the one ending, and a local
            // computed above it flips that reading while changing nothing about the behaviour. The guard is
            // right about the law, so this stays shaped the way the law is checked.
            //
            // #793 · …and a PARK is not a hall. The room's answer comes from the room you are sitting in:
            // trays and eighty chairs read on gravel under grow-lamps would be #740's fault with a bench
            // under it. The tail reading rides in front of the body's footnote because it is what the beat
            // was SPENT on — you sat still to look, and what you saw is the answer.
            TableAnswered(ex, t, SittingAlone.Wait,
                new CanteenTable.Answer(WithTheBodysFootnote(
                    WithTheBodysFootnote(
                        t.Office
                            ? RingOffice.NobodyCame(beat)
                            : t.Bench
                                ? ParkBenches.NobodyCame(beat)
                                : SittingAlone.NobodyCame(ex.CanteenWatch, beat, t.Quiet),
                        seen),
                    rested)));
            return;
        }

        ex.TableApproached.Add(t.Key);

        // #793 · …and WHAT ARRIVING MEANS depends on the furniture. A chair opposite is a conversation; the
        // far end of a plank is a stranger with a cup who says nothing. One branch, because they are two
        // different events and collapsing them would raise a full card over a park.
        if (t.Bench)
        {
            SomebodyTakesTheOtherEnd(ex, t);
            return;
        }

        SomebodyTakesTheChair(ex, t);
    }

    /// <summary>
    /// #757 · SOMEBODY CROSSES THE ROOM — and the roles are the other way round.
    ///
    /// <para>Owner: <i>"a stranger may approach me and 1. ask to sit down, 2. maybe offer to buy me a drink,
    /// 3. tell me what they have in mind… think Gandalf knocking on Bilbo's door."</i> #746's table is the
    /// captain talking their way into somebody else's business; this is somebody walking across a hall to
    /// recruit the captain into theirs, and the ladder is Core's, on the same machine.</para>
    ///
    /// <para>THE SAME PANEL, not a second one. One table, one continuous sitting — swapping the scene keeps
    /// the outcome the captain is mid-way through reading on the screen, where closing and re-opening would
    /// blink it (#680).</para>
    /// </summary>
    private void SomebodyTakesTheChair(SurfaceExcursion ex, TableTalk t)
    {
        // #784 · THE PAPERS GO AWAY WHEN SOMEBODY SITS DOWN. Owner: "your papers are OUT when somebody walks
        // up… putting things away is a beat, not an instant." The privacy predicate that licensed the spread
        // reads Solo, and Solo is about to become false — so the hold ends HERE, before the flag flips, and
        // it ends the way privacy ending should end it: sleeve shut, book blank, nothing filed.
        if (ex.Processing is { Work: Core.Processing.Work.Write })
        {
            AbandonProcessing(ex, Core.Processing.Interruption.CompanyArrived);
        }

        t.Solo = false;
        t.Plate = SittingAlone.VisitorPlate;
        t.Scene = SittingAlone.TheVisitor();
        t.Said.Clear();     // #749 · a new conversation: nothing has been said to HER yet.
        t.Showing = false;
        t.Math = null;
        t.Free = Math.Max(0, t.Free - 1);

        // #761 · Told, clearly, on the surface the captain is looking at. She is standing there; the game
        // says so in words and does not leave the arrival to be inferred from a changed button row.
        t.Outcome = t.Scene.Opening;
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        StateHasChanged();
    }

    /// <summary>#757 · She goes, and the table is yours again. The outcome line stays exactly as it was —
    /// what she said on the way out is the last thing that happened, and it must not be wiped by the state
    /// change that follows it.
    ///
    /// <para>#783 · The register is asked AGAIN rather than remembered: a glass goes warm while somebody is
    /// standing over you, and a table that stayed "resting" because it was resting ten minutes ago would be
    /// a picture of boots up on a chair she has just got out of.</para></summary>
    private void BackToYourOwnTable(SurfaceExcursion ex, TableTalk t)
    {
        t.Solo = true;
        t.DrinkInHand = APourInFrontOfYou;
        t.Relaxed = SittingAlone.SitReadsAsRelaxed(t.DrinkInHand, ex.CanteenWatch);
        t.Plate = SittingAlone.OwnTablePlate;
        t.Scene = SittingAlone.TheTable(t.Relaxed, t.DrinkInHand);
        t.Said.Clear();
        t.Showing = false;
        t.Free = Math.Min(t.Seats, t.Free + 1);
        StateHasChanged();
    }

    /// <summary>
    /// #746 · Put something on the table. The satchel as a conversational move: papers make hands nervous,
    /// an authority card makes a table quiet, a file on somebody is LOUD.
    /// </summary>
    private void TableShow(Core.Satchel.Item item)
    {
        if (_surface is not { } ex || _table is not { } t)
        {
            return;
        }

        // #751 · …and WHERE. The LOUD closure is a fact about the room, not about the paper: in a cabinet
        // the counter has no eyes, so nothing closes. Core decides that; this hands it the one bit.
        CanteenTable.Answer said = CanteenTable.PutOnTheTable(item, t.Who, t.Quiet);
        t.Showing = false;

        // The one thing on this table that counts as a MODIFIER gets remembered, keyed like every other
        // watch fact. It is the flag TableSituation reads for the +1 and the auto-resolve alike, so the
        // paper the Hand cannot look away from can never be two different facts.
        if (CanteenTable.CountsAsPaperOnTheTable(item, t.Who))
        {
            ex.TableMoves.Add(MoveKey(t, CanteenTable.Show + ":relevant"));
        }

        TableAnswered(ex, t, CanteenTable.Show, said);
    }

    /// <summary>
    /// #746/#680 · THE ONE PLACE AN OUTCOME BECOMES WORDS — and it puts them INSIDE the panel.
    ///
    /// <para>Applies everything the answer carries: the chit into the wallet, the name in somebody's book,
    /// the ask shutting, the table hardening, the fitter opening, the temp overhearing, the pips. Nothing
    /// about WHAT a band grants is decided here; a guard forces a band and pins the state that appears.</para>
    /// </summary>
    private void TableAnswered(SurfaceExcursion ex, TableTalk t, string moveId, CanteenTable.Answer said)
    {
        ex.TableMoves.Add(MoveKey(t, moveId));
        // #749 · …and the conversation's own memory, which is what an ANSWER is allowed to answer. The room
        // keeps the first for the watch; this one stands up when you do.
        t.Said.Add(moveId);

        if (said.GrantsChit)
        {
            _satchel = [.. Core.Satchel.Add(_satchel, CanteenTable.Chit(said.UnderAnotherName))];
        }
        if (said.ClosesTheAsk)
        {
            ex.TableAskShut.Add(t.Key);
        }
        if (said.HardensTable)
        {
            ex.TableHardened.Add(t.Key);
        }
        if (said.OpensFitter)
        {
            ex.TableFitterOpen = true;
        }
        if (said.ArmsTheTemp)
        {
            ex.TableTempOverheard = true;
        }
        if (said.TeachesTheHouse)
        {
            ex.TableHouseWays = true;
        }
        if (said.NervePips > 0)
        {
            // Through the ordinary nerve system and nothing else — the same gauge the ground bleeds, which
            // is the sanity system's own lineage saying a scene going wrong down here is frightening.
            ApplyNerveShock(said.NervePips * NervePips.PipUnit, "a table you cannot read");
        }
        if (said.Note is { Length: > 0 } note)
        {
            // FileNote and NOT ShowAndFile: the saying happens in the panel, and a pulse under an open
            // modal's blur is the exact bug #680 was filed on. The book still remembers (#686).
            FileNote(note, CanteenRegulars.Glyph);
        }

        // #680 · Said HERE, in the one layer the backdrop cannot blur.
        t.Outcome = said.Line;
        RequestVaultSave();
        StateHasChanged();
    }

    // ── THE SATCHEL, AS SEEN FROM A TABLE ─────────────────────────────────────────────────────────────

    /// <summary>What the captain could put on the table. Everything they are carrying — the gesture is
    /// "put something down", and a pocket that hid the wrong answers would be hinting at the right one.</summary>
    private IReadOnlyList<Core.Satchel.Item> TableShowables() => _satchel;

    // ── #743/#746 · THE MESS, WITH THE CHIT IN YOUR HAND ──────────────────────────────────────────────
    //
    // The chit is a wallet card, and a wallet card that only ever satisfied a future guard would be a token
    // in a ledger nobody sees. So it has ONE visible payoff now, in the room the pass exists for: B17's staff
    // mess (#743), which says PASS TO BE SHOWN on the door and has nobody behind it to show anything to.
    //
    // ADDITIVE, never a replacement: #743's own room card still fires first on first entry, because the find
    // is the room and this is what you do in it. Once per excursion, in the DEAD AIR family.

    /// <summary>Raise the chit beat if the captain is carrying cover and standing in the mess. Called by the
    /// staff-mess poll, after that room's own card has had its turn.</summary>
    private void ShowMessChitBeat(SurfaceExcursion ex)
    {
        if (ex.MessChitBeatShown || !CanteenTable.Cover.Held(_satchel))
        {
            return;
        }

        ex.MessChitBeatShown = true;
        ShowAndFile(CanteenTable.MessBeatLine, CanteenTable.ChitGlyph);
        ApplyNerveRelief(CanteenTable.MessBeatPips * NervePips.PipUnit);
        RequestVaultSave();
    }
}

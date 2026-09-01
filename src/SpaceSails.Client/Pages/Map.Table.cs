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
                //
                // #741 v1 · The saying's own SUBJECTS ride across too, so the demo start boots with the
                // THREADS page already showing the stack the whole cheat exists to demonstrate — the same
                // dead person named in three of these six entries, which the dossier has always quietly
                // held and nothing has ever said out loud.
                at += 240.0;
                book = Core.FieldNotes.Append(
                    book, new Core.FieldNote(one.Text, at, place, one.Glyph, one.Subjects));
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
            CanteenRegulars.Tables(ex.Stop.Body.Id, ex.Floor, a, ex.CanteenWatch, ex.HallStoodUp, ex.HallCameIn))
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
        /// of the verb. It is the one fact the panel needs that is not on the scene.
        ///
        /// <para>#865 · IT IS STILL THE OCCUPANCY FACT AND IT IS NO LONGER THE FRAME FACT. It is what
        /// <c>SeatedAlone</c> reads for the privacy ladder — a top with the weighbridge clerk's tray on it is
        /// not a seat you lay evidence out on, whoever chose to sit there — but which FRAME the sitting wears
        /// is <see cref="TheyCameToYou"/> now, because the owner ruled that co-seating is a strip state.</para></summary>
        public bool Solo { get; set; }

        /// <summary>
        /// #865 · DID THIS PERSON COME TO YOU? — the one fact the seated FRAME forks on.
        ///
        /// <para>Owner's ruling, live at a weighbridge clerk's table: <i>"what if I just sit and eat here…
        /// now I am kind of blinded of the surrounding here because somebody else sits in the same
        /// table"</i>, then <i>"It should somehow UI wise be same style as the sitting alone case"</i>, and
        /// sealing it: <i>"Just the social functions as additional options."</i></para>
        ///
        /// <para><b>Posture changes are a strip; people who come to you are a card.</b> A seat you CHOSE —
        /// an empty top, a bench, an office chair, a cubicle, and now the clerk's own top joined through
        /// [E] — is a posture change, and it presents in the docked strip with the room still lit behind it.
        /// Somebody crossing the hall and taking the chair opposite (#757's approach, #731's walker when she
        /// arrives) is an ENCOUNTER: her face is the point, and the card is what a face is for.</para>
        ///
        /// <para>This is deliberately NOT <see cref="Solo"/>, which is what it used to be. Solo asks <i>is
        /// the chair opposite empty</i>, and the two questions came apart the moment the owner ruled that
        /// joining a stranger keeps the room visible: at a top you joined, Solo is false and the frame is
        /// still the strip. A frame law read off an occupancy flag is one fact answering two questions, which
        /// is how the hall came to go black behind one small card.</para>
        /// </summary>
        public bool TheyCameToYou { get; set; }

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
        /// <para>#1040 · A STOOL WEARS NEITHER, for the bench's reason exactly: both pictures are
        /// photographs of a table with a chair pulled out opposite, and a counter has no chair opposite and
        /// no table. It is always the docked strip anyway (nobody ever comes aboard), and the strip draws no
        /// art — but a field that would answer wrongly if it were ever asked is a lie waiting for a
        /// caller.</para>
        public string? ArtUrl =>
            !Bench && !Stool && Who == CanteenTable.Who.None ? SittingAlone.ArtFor(Relaxed) : null;

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

        /// <summary>#758 · WHICH cabinet, as the plate beside its door reads — 0 anywhere else.
        ///
        /// <para><see cref="Quiet"/> is the room CLASS and this is the ROOM. They were one bool while a
        /// cabinet's only question was whether the counter has eyes in here, and they came apart the moment a
        /// cabinet acquired a state of its own: a curtain is drawn per LEAF, the keep writes down which one
        /// you were in, and the bark that knows too much says the number. Core's own
        /// (<see cref="CanteenRegulars.TableSeat.Cabinet"/>), carried rather than re-derived, so the top the
        /// captain is sitting at and the leaf they are dogging are one room.</para></summary>
        public int Cabinet { get; init; }

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
        /// #1016 · Whether this seat is on the captain's OWN SHIP — a top in her cantina, or the desk in
        /// CABIN 1.
        ///
        /// <para>Owner, on 7 Deck: <i>"Why no table here to sit at?"</i>, <i>"Why no table in cabin
        /// either?"</i>, <i>"I expect to have a bar table like this in this ships galley also.... feature
        /// complete."</i> The same machinery a third time, and for the third time the flag buys exactly the
        /// places a boat genuinely differs from a hall — <see cref="Office"/>'s own two, in fact. NOBODY EVER
        /// COMES OVER: her crew is three droids on a fixed patrol, and a haulier crossing the captain's own
        /// cantina to ask about her brother would be the scene and the ship disagreeing. And the silence is
        /// described in the BOAT's words (<see cref="SittingAlone.NobodyCameAboard"/>) rather than in an
        /// eighty-seat canteen's.</para>
        ///
        /// <para>It is deliberately not <see cref="Office"/> reused: an office is a room in somebody else's
        /// building on a shift that no longer runs, and its silence says so. Two rooms that agree about one
        /// mechanic and disagree about every word are two flags.</para>
        /// </summary>
        public bool Aboard { get; init; }

        /// <summary>
        /// #1040 · Whether this seat is a STOOL AT A COUNTER — today, the row bolted along the ship's own
        /// cantina counter.
        ///
        /// <para>Owner, on 7 Deck: <i>"Our on ship bar can be upgraded to match the other bars... the UI
        /// represents code long time ago."</i> The same machinery a fourth time, and the flag buys exactly
        /// the one place a stool genuinely differs from a top: <b>the rung</b>. <c>SeatedIn</c> reads it and
        /// answers <see cref="SeatedHud.Seat.BarStool"/>, which is where the gumshoe rule lives — the case
        /// does not come out at a bar, and it does not come out at your own bar either
        /// (<see cref="SeatedSpread.NotAtTheBarLine"/>, said out loud, never silently).</para>
        ///
        /// <para>It is deliberately NOT the counter's own <c>StoolSeat</c> (<c>Seating.Stool.cs</c>) reused:
        /// that seat is a fact about a Hive canteen's frozen watch, its floor and its neighbour, and every
        /// one of those is a question a boat cannot be asked. What the two share is the RUNG, and the rung
        /// is the thing this flag reaches.</para>
        /// </summary>
        public bool Stool { get; init; }

        /// <summary>
        /// #1016 · HOW MANY BEATS HAVE BEEN WAITED OUT AT THIS SEAT — <b>aboard only</b>, and null anywhere
        /// else in the sense that nothing reads it there.
        ///
        /// <para>Ashore the beat counter is the ROOM's (<c>ex.TableWaits</c>) and that is a law rather than a
        /// convenience: the approach is seeded on (site, floor, top, watch, beat), so a captain who stood up
        /// and sat down again to reroll the dice simply carries on from the beat they were on. <b>There is no
        /// such dice aboard</b> — nobody ever comes to either of the ship's seats — so there is nothing to
        /// reroll and nothing to abuse, and the counter's only job is to stop the two silence lines looping
        /// on the first one. It lives on the sitting because the ship has no excursion to keep a ledger on,
        /// and inventing one for a number that decides which of two sentences you read would be a second
        /// ledger for a fact nothing else asks about.</para>
        /// </summary>
        public int Waits { get; set; }

        /// <summary>
        /// #1016 · WHICH SHIFT THIS SITTING IS ON, when there is no excursion to ask.
        ///
        /// <para>Ashore the watch is the room's and is frozen on the excursion (<c>ex.CanteenWatch</c>,
        /// #709), and this stays 0 there and is never read — a second copy of a fact the room already holds
        /// is one source consumed in the wrong order waiting to happen. It is written only by the sittings
        /// that HAVE no excursion behind them (a top in a docked station's bar, and the ship's own two), for
        /// the one question the silence has to ask a clock: which of the hall's two pools a fruitless wait
        /// comes out of.</para>
        /// </summary>
        public long Watch { get; init; }

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
        /// #821 · WHICH WC CUBICLE THIS SEAT IS IN, by <c>HiveInterior.CubicleKey</c>, or null anywhere else.
        ///
        /// <para>An office chair and a cubicle's pan are the same POSTURE and the same panel — the seam
        /// <see cref="Office"/> already opened — and what the cubicle adds is one question the ladder has to
        /// be able to ask: <b>is the catch over right now?</b> The key is carried rather than the answer,
        /// because the answer changes while you are sitting on it: a captain can sit down in an open cubicle,
        /// reach back, turn the catch, and the spread has to become allowed on that very frame.</para>
        /// </summary>
        public string? CubicleKey { get; init; }

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
        /// that was made before you left the table, because the man made it to somebody who then stood up.</para>
        ///
        /// <para>#731 v2 · <c>init</c>, and only init: a sitting may be OPENED with things already said in it
        /// — the conversation she stood up in the middle of, carried across the hall and resumed in a booth —
        /// and that has to happen in the initializer, because the one construction method is the whole of
        /// #870 lane 6d's law. It is still never reassigned after that; the set is added to and cleared in
        /// place, and this accessor cannot be reached again.</para></summary>
        public HashSet<string> Said { get; init; } = [];
    }

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

    // -- #870 lane 6c · THE FORWARDERS, AND EVERY ONE OF THEM HAS A CALLER OUTSIDE THIS FAMILY --------
    //
    // The scene lives on Seating now (Seating.Table.cs). These nine are the spellings the rest of the page
    // still asks for BY NAME, measured rather than assumed: five presses and three question-askers in
    // Map.razor, the [E] arm in Map.Deck.Interact.cs, the Esc chain in Map.Sim.Cancel.cs, and the ?spread=1
    // row above, which sits the captain down through the very handler a captain's own press reaches.
    //
    // Everything else the scene does -- the wait beat, the dice, the arrival, the one place an outcome
    // becomes words -- is reached from inside the seat and kept no forwarder at all. Do not add one for a
    // NEW question: add the question to Seating and ask it through _seating.

    /// <inheritdoc cref="Seating.TryOpenTable"/>
    private bool TryOpenTable() => _seating.TryOpenTable();

    /// <inheritdoc cref="Seating.TryTakeTable"/>
    private bool TryTakeTable() => _seating.TryTakeTable();

    /// <inheritdoc cref="Seating.CloseTable"/>
    private void CloseTable() => _seating.CloseTable();

    /// <inheritdoc cref="Seating.TableMoveClicked"/>
    private Task TableMoveClicked(string moveId) => _seating.TableMoveClicked(moveId);

    /// <inheritdoc cref="Seating.TableMoveOnOffer"/>
    private bool TableMoveOnOffer(Encounter.Move move) => _seating.TableMoveOnOffer(move);

    /// <inheritdoc cref="Seating.TableMoveRefusal"/>
    private string TableMoveRefusal(Encounter.Move move) => _seating.TableMoveRefusal(move);

    /// <inheritdoc cref="Seating.TableMoveIsUrged"/>
    private bool TableMoveIsUrged(Encounter.Move move) => _seating.TableMoveIsUrged(move);

    /// <inheritdoc cref="Seating.TableShow"/>
    private void TableShow(Core.Satchel.Item item) => _seating.TableShow(item);

    /// <inheritdoc cref="Seating.TableShowables"/>
    private IReadOnlyList<Core.Satchel.Item> TableShowables() => _seating.TableShowables();

    // ── #758 · THE CURTAIN AND THE DOOR, FROM THE STRIP ───────────────────────────────────────────────
    //
    // Three of these are the ordinary forwarders the rule above allows for a press and the two things its
    // button has to draw. The fourth is the one that could not live in the seat: working a leaf changes what
    // the DECK says about that leaf, and rebuilding a deck is a page's job — a seat that could do it would
    // need a twenty-ninth member on ISeatHost, and that interface may only shrink.

    /// <inheritdoc cref="Seating.ACabinetLeafToWork"/>
    private bool ACabinetLeafToWork => _seating.ACabinetLeafToWork;

    /// <inheritdoc cref="Seating.CabinetLeafLabel"/>
    private string CabinetLeafLabel => _seating.CabinetLeafLabel;

    /// <inheritdoc cref="Seating.CabinetLeafHint"/>
    private string CabinetLeafHint => _seating.CabinetLeafHint;

    /// <summary>#758 · Draw the curtain, or dog the door. The seat decides and remembers
    /// (<see cref="Seating.DrawOrDogTheCabinet"/>); this puts the answer back on the plan, because the
    /// cabinet's glyph is drawn from the set that press just changed and a plan still showing cloth over a
    /// dogged leaf is the picture and the sim disagreeing about a room the captain is sitting in.</summary>
    private void WorkTheCabinetLeaf()
    {
        if (!ACabinetLeafToWork)
        {
            return;
        }
        _seating.DrawOrDogTheCabinet();
        RebuildSurfaceDeck();
    }
}

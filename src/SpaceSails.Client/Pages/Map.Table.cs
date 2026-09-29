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
}

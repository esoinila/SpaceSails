using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

public static partial class UndergroundComplex
{
    // ── #725 · THE TWO LOUDEST SILENT FINDS ──────────────────────────────────────────────────────────────
    //
    // Owner's audit question: "are we giving enough attention to plot-significant finds? They should have a
    // Gen-AI image and their own dialog by our standards." Walking the four handoff floors, two were met
    // (THE SHAFT, DEAD AIR) and the two the handoff doc actually sends playtesters to were SILENT — a wall
    // stencil and a room of furniture, both missable at deck-plan zoom by a player who has just walked past
    // the reveal of the arc.
    //
    // Both cards are the allowed shape and not the other one: they SHOW HARDER AND REFUSE TO CONCLUDE. The
    // plate card describes two coats of paint and a screwed-on sign and ends without naming what was under
    // the first coat; the mess card describes squared chairs and warm machines and ends on the machines. No
    // subtitle, no hint, no verb. TheHiveTests.NothingDownHereEXPLAINSAnything is one deck up, and neither of
    // these goes round it.
    //
    // NEITHER MAY NAME THE PLATE'S TEXT. It varies by site kind (TitleOf/KindOn) and a card that quoted it
    // would be prose transcribing a sign the renderer draws — the same fact in two places, one of which never
    // hears about a change.

    public const string UnlistedLobbyArtUrl = "art/the-plate.jpg";

    public const string UnlistedLobbyLabel = "▣ THE PLATE";

    /// <summary>The first arrival on the unlisted band's own lobby floor. #592's whole arithmetic delivered
    /// by one sign, and the sign is never quoted. Authored, verbatim.</summary>
    public const string UnlistedLobbyCard =
        "The car opens on a lobby with no department and no livery — bare pour, one lamp, somebody's chair. " +
        "Beside the shaft there is a plate, and the plate has been done twice: a wide patch of newer paint " +
        "first, laid over something larger, and then the small name screwed on over that. Good work, both " +
        "times — a crew that stencils for a living, sent down here to change an answer. It is not the name " +
        "of anything you rode down through. You read it again. It says what it said. Above you, twenty " +
        "floors file and grade and answer to one name; the wall down here has been corrected.";

    public const string StaffMessArtUrl = "art/the-staff-mess.jpg";

    public const string StaffMessLabel = "🍽 THE STAFF MESS";

    /// <summary>The first entry into the staff canteen's room — a ROOM beat and not a floor beat, because
    /// the floor it is on is an ordinary floor and the room is the find. Authored, verbatim.</summary>
    public const string StaffMessCard =
        "A mess for the staff: machines on the wall still holding their temperature, chairs squared to the " +
        "tables the way a crew squares them at the end of a shift it expects to repeat. The door wanted a " +
        "pass shown. Inside there is nobody to show it to, and nothing out of place — no tray abandoned, no " +
        "chair shoved back, no note. Whatever ended here was not sudden, or it was tidied. The machines hum " +
        "and keep their hours. The shift has not come, and the machines are not the kind that wonder.";

    // ── #751 · THE TWO STORY-GRADE ROOMS THE HALL RULE ADDS ──────────────────────────────────────────────
    //
    // Owner: these rooms are story-grade — they get first-entry CARDS with gen-AI art, the same one-shot
    // pattern as THE PLATE and THE STAFF MESS (#725/#743). Prose authored, wired VERBATIM, and neither of
    // them says what the building is for: the hall's card is about MONEY (a company that feeds contractors
    // like a hotel), and the cabinet's is about MEMORY (a room that has none). §13.8 holds.

    /// <summary>#751/#759 · The B1 cantina hall, painted.
    ///
    /// <para>#759 · …and repainted, because the room grew a wall. Owner's pinned requirement: <i>"the
    /// restaurant scene must have a) A VIEW TO THE PARK and b) A WINDOW WALL BETWEEN"</i>, and the canonical
    /// mapping he filed with it makes <c>b1-restaurant-park-view.jpg</c> the hall's own establishing shot —
    /// the same room, the same steel tables, and the floor-to-ceiling riveted glass with the green behind
    /// it. It supersedes <c>b1-cantina-hall.jpg</c> everywhere the hall's art shows (the floor it wears and
    /// the card it raises are the same picture on purpose, #755), because a card that shows a room with no
    /// window in it, hung over a plan whose far wall is a window, is two pictures of one room disagreeing
    /// about the room.</para></summary>
    public const string CantinaHallArtUrl = "art/b1-restaurant-park-view.jpg";

    /// <summary>#756 · How opaque a hall's floor art is drawn. A shade under the ship's own 0.9f: the hall
    /// is thirty times the floor area of a cabin, so the same alpha that reads as texture behind a 12×7
    /// cantina reads as a photograph the deck grid is lost in. Legibility first — the walls, the plates,
    /// the tops and the captain all draw OVER this.</summary>
    public const float HallArtAlpha = 0.72f;

    /// <summary>#756 · Which picture a hall's floor wears, or null for a bare deck.
    ///
    /// <para>Owner, on walking into the biggest social room in the game and finding bare grid: <i>"let's
    /// put todo to have gen-AI Bar image on the background like we have in space ports."</i> One row per
    /// painted hall, asked of the building the same way its plates and its seats are — so the park, the
    /// mess and the head office's dining room each take a row here when their art lands, and nothing else
    /// anywhere has to learn a new idea to wear one.</para>
    ///
    /// <para>The branch office's UPPER canteen is the one painted so far, and the art is the very frame
    /// #755's card already shows: the same room, from the door, so walking in and reading the card are two
    /// looks at one place rather than two places.</para></summary>
    public static string? HallArtFor(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.UpperCanteen && !IsHeadOffice(bodyId) ? CantinaHallArtUrl : null;
    }

    // ── #756/#780 · THE PICTURE OF THE THING, AT THE THING ────────────────────────────────────────────
    //
    // Owner, live playtest 2026-08-08: "see how in the space bars we have the image of bar desk at the spot
    // where the bar desk is."
    //
    // He is describing the ship, and he is right that the hall was only half doing it. #756 gave the floor a
    // HALL-WIDE backdrop — one picture stretched over the whole room, which is ambience — while the bar-desk
    // art it shipped in the same PR was only ever on the service card. So the room you walked was a wide
    // room with a wall drawn across the end of it, and the counter was a line.
    //
    // A SPOT IS THE SECOND KIND OF ROOM ART, and the difference is not size, it is what it claims. Hall art
    // says WHERE YOU ARE. Spot art says WHAT IS THERE — a piece of furniture, drawn over its own box, hard
    // enough at the edges to read as an object you could walk up to. The ship has said this since the 3D
    // renovation and never needed a word for it; the hall needs one because a hall has furniture in it that
    // Core carves and a renderer must not measure.
    //
    // PUBLISHED, NOT DERIVED. The box comes out of CarveHall at the moment the counter's walls are laid, off
    // the very same (u, v) the wall segments are built from — so a hall carved a du wider moves its picture
    // with its bar and no caller anywhere re-measures a room it does not own (§13.15's second cause, which
    // this project has paid for four times). #759's park windows and any fixture after them wear one by
    // adding a Spot where the fixture is carved, and nothing else at all.

    /// <summary>#780 · One piece of a hall's furniture, painted over its own carved box.</summary>
    /// <param name="Url">The picture. Degrades like every other art slot — no file, no frame.</param>
    /// <param name="X0">Left edge, in the surface's own coordinates.</param>
    /// <param name="Y0">Bottom edge.</param>
    /// <param name="X1">Right edge.</param>
    /// <param name="Y1">Top edge.</param>
    public readonly record struct SpotArt(string Url, double X0, double Y0, double X1, double Y1);

    /// <summary>#780 · The bar desk itself: long polished top, brass rail, the backlit shelves behind it and
    /// the stools bolted down along the front. The very picture the counter's own service card wears
    /// (<c>Interior.CounterService</c>), now also standing where the counter stands.</summary>
    public const string CounterArtUrl = "art/b1-bar-desk.jpg";

    /// <summary>#827 · What is stencilled on the floor at the counter's cashier gap. The bureaucracy's own
    /// voice would have written PAYMENT POINT 1; a bar writes TILL.</summary>
    public const string CounterTillPlate = "🧾 TILL";

    /// <summary>#827 · …and at the far end, where the tray comes back over the desk.</summary>
    public const string CounterCollectionPlate = "🍽 COLLECT";

    /// <summary>#780 · How opaque a FIXTURE's picture is drawn — deliberately harder than
    /// <see cref="HallArtAlpha"/>, and a shade harder than the ship's own 0.9f room backdrops.
    ///
    /// <para>One constant, and the reasoning is the whole of the owner's note. The hall's 0.72 is right for
    /// a floor: it is ambience under a grid, and the grid has to win. A counter is not ambience. It is the
    /// object you walked across the room to stand at, it is five deck-units deep in an eighty-seat hall, and
    /// at 0.72 over hall art it would have read as a slightly different patch of wallpaper — a second
    /// picture where the eye wanted a piece of furniture. So the spot is nearly solid: it has EDGES, it
    /// occludes the floor art under it, and it stops at the exact line the counter's wall is drawn on. The
    /// vector overlay still goes over the top of it, which is the one thing this alpha may never cost — the
    /// walls, the plates, the console dot and the captain are all still legible on the counter.</para></summary>
    public const float SpotArtAlpha = 0.96f;

    /// <summary>#780 · Which picture stands where a hall's counter stands, or null where the room's bar is
    /// not a bar anybody is served at.
    ///
    /// <para>ONE QUESTION, ASKED THE SAME WAY <see cref="HallArtFor"/> is asked. It answers for exactly the
    /// halls whose counter <c>Interior.CounterService.For</c> serves at — the branch office's upper canteen
    /// — because a bar desk drawn on the floor of a room where nothing is poured would be the deck telling a
    /// story the card refuses to tell. The head office's dining room has a SIDEBOARD and not a bar, and its
    /// picture has not been shot; a mess has neither.</para></summary>
    public static string? CounterArtFor(string bodyId, Comfort use)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return use == Comfort.UpperCanteen && !IsHeadOffice(bodyId) ? CounterArtUrl : null;
    }

    /// <summary>#751 · What the card is called.</summary>
    public const string CantinaHallLabel = "🍸 THE HALL";

    /// <summary>#751 · First entry into the B1 cantina hall. Authored, verbatim.
    ///
    /// <para>The register is #601's funding trail said as a room: a suspiciously nice company canteen on a
    /// nowhere rock is money that does not mind being SEEN feeding contractors, only being asked. Nobody in
    /// the frame finds it strange, which is the whole horror technique of this set.</para>
    ///
    /// <para>#783/#941 · The opening clause said <i>linen on the tables</i>; the owner's no-tablecloths
    /// ruling on #759 is an ART spec — bare steel under a riveted window wall — so the prose follows the
    /// picture and the shine is the steel's, owner-authored and lifted verbatim.</para></summary>
    public const string CantinaHallCard =
        "Carriers' canteen, the sign says, and the room says something else: steel tables wiped to a " +
        "shine, brass on the pillars, light somebody chose. On a rock with no name on any chart, the " +
        "company feeds its contractors like a hotel feeds guests it wants to keep — and nobody at the " +
        "tables finds that strange, because the pay is on the nail, the coffee is real, and questions are " +
        "the one thing on the menu that costs. Along the back wall, a row of doors. Cabinets, by " +
        "arrangement. The hall is loud. The doors are why.";

    /// <summary>#751 · The cabinet, painted.</summary>
    public const string CabinetArtUrl = "art/b1-cabinet.jpg";

    /// <summary>#751 · What the card is called.</summary>
    public const string CabinetLabel = "🚪 THE CABINET";

    /// <summary>#751 · The glyph the cabinet's filed line wears — the door, because a door with nothing
    /// written on it is the whole of what one of these rooms is from the hall side.</summary>
    public const string CabinetGlyph = "🚪";

    /// <summary>#751 · First entry into ANY cabinet — once total, never once per door. Authored, verbatim.
    ///
    /// <para>The telephone with no dial is canon furniture of a cabinet from here on: it receives and never
    /// dials, it has no mechanics, and nothing anywhere explains it.</para></summary>
    public const string CabinetCard =
        "Six chairs, a table wiped past clean, and a door padded like a vault that dogs shut from inside. " +
        "The hall outside is loud the way a sea is loud — a noise you can hide a sentence in, but every " +
        "face out there sits in the counter's long memory. In here there is no memory: whatever crosses " +
        "this table crosses it once and leaves in the pockets it came in. There is a telephone on the wall. " +
        "It has no dial. Rooms like this are not on the menu — you arrange them, or you are brought.";

    /// <summary>#751 · What the field book keeps of a cabinet. The card is the moment; this is the book's
    /// compressed record of it, and it is the only place the MECHANIC is ever stated — by observation, never
    /// by tooltip.</summary>
    public const string CabinetNote =
        "A cabinet off the hall: six chairs, one door, and no line of sight to the counter. Rooms like " +
        "this are why the hall is loud.";

    /// <summary>#751 · Does THIS floor's hall earn the cantina card? The card's first four words name the
    /// sign on the door — <c>CANTEEN 1 · CARRIERS &amp; CONTRACTORS</c> — so it belongs to the branch
    /// office's bar and never to the head office's dining room, which has a plate, a register and an
    /// arrival card of its own (#411). Asked here, so no client ever decides it.</summary>
    public static bool ShowsCantinaHallCard(string bodyId, int level)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return !IsHeadOffice(bodyId) && TopPressurisedFloor(bodyId) == level;
    }

    // ── #677 · WHAT THE HALLS SAY, WHICH IS ALMOST NOTHING ───────────────────────────────────────────────
    //
    // EVERY STRING BELOW IS THE OWNER'S, LIFTED VERBATIM. Nothing in this file may reword one, and nothing
    // anywhere may add to them: the prose down here is the whole of the feature's voice, and a sentence
    // written to fill a gap is the sentence that explains something. Where the generator has nothing
    // authored to say — the room line of a gallery that holds a record, for instance — it says NOTHING, on
    // purpose, and the card does the describing.
    //
    // The three canon walls these are written under (§10, §13.20), checked by grep in TheFoundBandTests:
    //
    //   1. the word §8 reserves never appears down here, in any string, ever;
    //   2. nothing names a builder, an age, a purpose, or the Old Ones and the Reevers;
    //   3. BOTH readings survive every line — the mundane one (better instruments, a better resurvey team)
    //      and the other one (this was always here and is being SHOWN to us). The moment one sentence
    //      settles which, the horror dies, and that is the Reever law applied to archaeology.
    //
    // The register, in the owner's own four words: HORROR SERVED AS SMOOTH COMFY PILLOW. Nothing down there
    // threatens; everything accommodates. The dread is entirely in the implication — a pillow means you were
    // expected.

    /// <summary>#677 · Said once per excursion, on the ride that crosses out of the poured shaft. The one
    /// sentence in the game about the boundary between the two worlds, and it describes a MATERIAL and stops.
    ///
    /// <para>Authored, verbatim. It is deliberately not decorated with a glyph the way the pulse lines around
    /// it are: the book's own column carries one, and the sentence is the owner's.</para></summary>
    public const string SeamLine =
        "The pour stops. Not at a wall — at a line, clean as a tide mark, and past it the tunnel keeps " +
        "going in a material the light does not grip.";

    /// <summary>#677 · Said once per excursion, stepping out onto the first gallery. Four sentences, three
    /// of them facts a suit could measure and the fourth an absence.
    ///
    /// <para>Placed LAST of the arrival's sayings, which is #693's open problem worked around rather than
    /// solved: the pulse has one slot and the last write wins, so the climax goes last. Authored,
    /// verbatim.</para></summary>
    public const string FoundArrivalLine =
        "The car has no button for this floor. It stops anyway. The air is good. Nothing here says why.";

    /// <summary>#677 · What a gallery says when there is nothing in it, which is almost every gallery.
    /// It REPLACES the facility's stripped line, which must never be said down here — somebody clearing a
    /// room in a hurry is a sentence about staff, and there was no staff. Authored, verbatim.</summary>
    public const string FoundEmptyRoomLine =
        "Nothing. Not stripped — nothing was ever here. The room is clean the way a prepared room is clean.";

    /// <summary>#677 · How many galleries in this many hold a record worth carrying out. The rest are the
    /// line above, and the ratio is the point: the emptiness is load-bearing squared down here.</summary>
    public const int FoundRecordOneInN = 9;

    /// <summary>#677 · The pickup line for a record find — #614's law exactly: what goes in the pocket is the
    /// RECORD of a thing that stays where it is, because a satchel claiming to hold a wall would be the third
    /// named bug class one size up. Authored, verbatim, and it carries no leading indent because the room it
    /// belongs to has nothing of its own to say first.</summary>
    public const string FoundRecordFindLine =
        "🎒 Into your pocket: measurements, a photograph, a rubbing. The wall keeps the rest.";

    /// <summary>#677 · What the casebook keeps. #603's law — looking is free, knowledge is one-shot — so the
    /// BOOK gets this and the pulse gets the find line, and one wall never appears in the book twice in two
    /// registers (#701's rule, learned on the shelves). Authored, verbatim.</summary>
    public const string FoundRecordGist =
        "a wall with no seam, faintly warm — the tape measure fails to give it scale";

    /// <summary>#677 · The look-card's title: the authored gist inside the house frame, exactly the way
    /// <c>OddBooks.CardTitle</c> puts an authored shelf fragment inside its own. No new prose is written for
    /// a caption that would otherwise have to invent one.</summary>
    public static string FoundRecordCardLabel => $"⭕ {FoundRecordGist}";

    /// <summary>#677 · The card body. Caption-only, in the #528 idiom the odd book and the lifeboat muster
    /// already keep: there is no painted art for this and a wired-but-unpainted image is a card claiming a
    /// picture it does not have. Authored, verbatim — evidence, and then it stops.</summary>
    public const string FoundRecordCard =
        "A section of wall, recorded because it cannot be brought back: continuous, seamless, faintly warm. " +
        "The tape measure in the photograph is there to give it a scale, and fails.";

    /// <summary>#677 · THE DURABLE ID OF ONE FIND, minted in one place.
    ///
    /// <para>It carries which kind of place it came out of, in its prefix, and that is what lets the two
    /// relic-class objects in the game tell themselves apart wherever they are met — in the pocket line, in
    /// the satchel row, and on the look-card — without any of those three re-deriving a floor's band for
    /// itself. A carried thing is asked what it IS, once, and the answer travels with it.</para></summary>
    public static string FindId(string bodyId, int level, int roomIndex)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        return $"{(IsFound(bodyId, level) ? HallFindPrefix : "hive")}:{bodyId}:{level}:{roomIndex}";
    }

    /// <summary>The prefix a find out of the halls wears. Not "hive": the whole point is that it is not one.</summary>
    public const string HallFindPrefix = "hall";

    /// <summary>#677 · Did this find come out of a gallery nobody dug? Asked of the id rather than of a body
    /// and a level, because the satchel keeps the id and nothing else — a row that had to re-derive a band
    /// from a parsed level would be the same fact computed in a second place, which is what this file's own
    /// spec opens with a table of.</summary>
    public static bool IsHallRecord(string? findId) =>
        findId is not null && findId.StartsWith(HallFindPrefix + ":", StringComparison.Ordinal);
}

using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #813 · THE BLOCK'S OWN NUMBERS AND ITS OWN REGISTERS — the Manhattan ruling and every dimension it
/// decides, the fire code, the block as it is measured off a field, and the lists of plates and sizes the
/// rest of the family reads off it. What a room IS lives in <c>UndergroundComplex.Block.Rooms.cs</c>, what
/// a ring room is in <c>…Block.Ring.cs</c>, the carve that cuts a side into them in <c>…Block.Carve.cs</c>,
/// and the amenity gradient in <c>…Block.Amenities.cs</c>.
///
/// <para><b>#1163's static-class law is why this file is a little wider than that one concern.</b> Static
/// field initializers of a partial class run in the order the compiler reads the FILES, not the order a
/// reader sees, so a <c>static readonly</c> moved into a new partial can initialise against a zero it was
/// never written to see. Every declaration of one — <see cref="ParkViewPlates"/>,
/// <see cref="HallTopSizes"/>, <see cref="PrincipalPlates"/> — therefore stays HERE, in its original
/// order, along with the run of members it sits among; #251 took only the sections that declare none. The
/// cut is where the initializers allow rather than where the concerns are, which is the ruling
/// <c>HavenInterior</c> paid for with 33 moved frames.</para>
/// </summary>
public static partial class UndergroundComplex
{
    // ── #813 · THE MANHATTAN RULING — THE PARK IS THE MIDDLE OF THE BLOCK ────────────────────────────────
    //
    // Owner, 2026-08-09 evening: "The central park needs to be in the center of all the other rooms… not on
    // the side. Think of New York, is the park on one side or is it in the center?" And the clause that
    // decides every number below: "make sure the park prime real estate is not wasted and not unused, not on
    // any side. It is the best real estate."
    //
    // WHAT WAS WRONG WITH THE SHIPPED PARK. #759 bought its ground out of the strip beyond the ribs' far
    // ends — the one band no placer had ever used — so the green ran the whole width of the field with the
    // hall's glass on one long side and PAINTED ROCK on the other three. #801 put a row of doors in the far
    // wall, which was the owner noticing the same thing from inside: "walking through the park is fun, it
    // should not be the edge." A room with three dead sides is a room on the side of the map however big it
    // is, and three quarters of the best frontage in the building was frontage onto nothing.
    //
    // WHAT A BLOCK IS. The park is now the middle of a city block, and everything else here is the block:
    //
    //   ─────────────── THE SPINE ─────────────────   the block's own near street, with the cage on it
    //   │ suite │ G │ suite │   T H E   H A L L   │   NEAR RING · doors on the spine, glass on the park
    //   ├───────┴───┴───────┴─────────────────────┤
    //  W│                                         │E  WEST/EAST RING · doors on the street, glass on the
    //  e│            T H E   P A R K              │a  park; one gate through each
    //  s│                                         │s
    //  t├─────────────────────────────────────────┤t
    //   │ store │ G │ potting │ cold │ G │ office │   FAR RING · the back of house, now ring fabric:
    //   ─────────── THE BACK STREET ──────────────    a door on the street AND its old door on the gravel
    //
    // FOUR LAWS, and each of them is a guard in TheParkIsTheCentreOfTheBlockTests:
    //
    //   1. CENTRALITY. Every one of the park's four walls is faced by carved fabric. No side of it is the
    //      field's edge and none of it is rock.
    //   2. THE RING IS COMPLETE. Every du of the park's perimeter that is not a gate is a room's park-facing
    //      wall, and that wall is GLASS (published in FloorPlan.Windows, blocking like any other wall). The
    //      owner's "not unused, not on any side", in the one unit a guard can measure.
    //   3. A CORRIDOR ON EVERY SIDE. The near street is the spine itself; the far street is the back street;
    //      the west and east streets join them into one loop. Every ring room's door is on a street and
    //      never on the park, so nobody walks through an office to reach an office.
    //   4. THE CAR GOES WHERE THE BUILDING IS THINNEST. The ring's own frontage decides which end of the
    //      block the goods car stands at — the density decides the shaft's side and never the other way
    //      round (see ServiceShaftAt).
    //
    // WHICH SIDE OF THE SPINE. Always the lower one. The cage's alcove hangs off the spine's UPPER face, and
    // a block that took that side would have to be carved around the captain's own way home — a notch in the
    // best frontage in the building, on the one column nothing may ever be laid across (#585). The block
    // takes the side the cage does not, which is a reason and not a coin toss.

    /// <summary>#813 · How far the block's two service streets stand in from the spine's own end caps. What
    /// is left outside them is the rock the goods car's alcove is cut into, which is why this is a number
    /// and not the edge itself.</summary>
    public const double BlockStreetInsetDu = 14.0;

    /// <summary>#813 · How deep the ring is on the SPINE side — the premium band, and the one the hall
    /// stands in. It is the hall's own length (the old <c>RibReachDu + HallRibExtraDu</c> less the corridor
    /// half it was measured from), because the room that has to fit is the one with eighty seats in it: a
    /// band shallower than this would make the bar wider than the ground it stands on.</summary>
    public const double RingNearDepthDu = 51.5;

    /// <summary>#813 · How deep the ring is on the BACK STREET side. The facility's own chamber module
    /// exactly (<see cref="ParkBackDepthDu"/>) and not a number of its own — these are #801's back of house
    /// re-anchored, and #801's law is that they are ordinary rooms that happen to be entered off a garden.
    /// A ring band a du deeper than the module would have made that sentence false by one du, which is how
    /// a law quietly stops being one.</summary>
    public static double RingFarDepthDu => ParkBackDepthDu;

    /// <summary>#813 · How deep the ring is on the two ends of the block. Deeper than a chamber and far
    /// shallower than the near band: a corner office with the green out of one wall.</summary>
    public const double RingSideDepthDu = 20.0;

    /// <summary>#813 · How wide a ring room WANTS to be. A band is cut into
    /// <c>max(1, round(span / this))</c> rooms of equal width, so the pier between two suites is their
    /// shared wall and no frontage is ever left over — the owner's "not unused" said as arithmetic rather
    /// than as an intention.</summary>
    public const double RingRoomTargetDu = 40.0;

    /// <summary>#813 · The narrowest a ring room may be. Also the clearance a gate must leave at each end of
    /// a band: a corridor cut so close to the corner that the room beside it is a cupboard is a corridor
    /// that has eaten the frontage it was there to serve.</summary>
    public const double RingRoomMinDu = 16.0;

    /// <summary>
    /// #817 · HOW MUCH STREET FACE ONE DOOR SERVES.
    ///
    /// <para>Owner, live in a 40 du landscape office with one leaf in it: <i>"Oh just one door in a landscape
    /// office?"</i> and, the same evening, <i>"bigger spaces must have much more doors."</i> That overrode
    /// <see cref="RingRoom.Door"/>'s documented "exactly one", and this is the number the override is stated
    /// as: a room's street frontage divided by this, rounded, is how many leaves it gets.</para>
    ///
    /// <para>Eighteen du is the precedent already standing in the building rather than a figure somebody
    /// liked — the hall's own corridor face carries four to five doors over its run (#775/#812), which is
    /// this ratio to within a leaf. It is also comfortably more than twice
    /// <see cref="DoorHalf"/>, so two doors on the narrowest frontage the ring will ever cut
    /// (<see cref="RingRoomMinDu"/>) still leave a pier of wall between them.</para>
    /// </summary>
    public const double RingStreetFaceDuPerDoor = 18.0;

    /// <summary>
    /// #822 · THE FIRE CODE — no space may have only one way out, except the bedroom-small ones.
    ///
    /// <para>Owner's standing law, issued mid-build: <i>"no space may have only one door except bedroom-small
    /// rooms."</i> Every ring room takes at least this many exits, counting its street doors and its gate
    /// onto the green, however little frontage it has. The exemption is
    /// <see cref="FireCodeSmallRoomDu"/>.</para>
    /// </summary>
    public const int FireCodeMinExits = 2;

    /// <summary>
    /// #822 · How big a room may be and still be let off with one door. A space whose LONGEST side is no
    /// longer than this is bedroom-small: a WC cubicle, a privacy booth, a cell you can cross in two paces
    /// and whose one door you are always within reach of.
    ///
    /// <para>Named here rather than inside <see cref="RingOffice"/> because the sweep this law is really
    /// about is building-wide (#818 will walk every carved room in the facility), and a threshold that lived
    /// in the ring's own furniture file would be re-typed the moment a laboratory needed it.</para>
    /// </summary>
    public const double FireCodeSmallRoomDu = 8.0;

    /// <summary>
    /// #822 · <b>THE EXEMPTION LIST — every room the fire code lets off with one way out, BY NAME, with the
    /// reason beside the name.</b>
    ///
    /// <para>It was one exemption and it was dimensional: a space you can cross in two paces
    /// (<see cref="FireCodeSmallRoomDu"/>). That answered every room in the building, because every room in
    /// the building that had one door was small. #1199 brought the first room that has one door <b>on
    /// purpose and is not small</b> — an observation walk, a straight glass-floored tube out over the drop
    /// with a rail at the blind end — and the honest way to let it off is not to widen the number. Widening
    /// the number would exempt every oversized cupboard in the game along with it, which is how a standing
    /// law quietly becomes a default.</para>
    ///
    /// <para>So the law grows a list instead. Each member is a room the owner's sentence was never about,
    /// each carries its reason in its own doc, and <see cref="ReasonFor"/> hands that reason back so a guard
    /// can hold the list to being REASONED rather than merely long. Adding a member is a deliberate act with
    /// a sentence attached; nothing is ever let off by arithmetic again.</para>
    /// </summary>
    public enum FireCodeExemption
    {
        /// <summary>No exemption. The standing law applies in full: at least
        /// <see cref="FireCodeMinExits"/> ways out.</summary>
        None = 0,

        /// <summary>#822 · The original, and the only one the owner's own sentence names: <i>"no space may
        /// have only one door except bedroom-small rooms."</i> A WC cubicle, a privacy booth, a cell you can
        /// cross in two paces and whose one door you are never out of reach of. Measured and not declared —
        /// <see cref="FireCodeSmallRoomDu"/> is the whole of it.</summary>
        BedroomSmall = 1,

        /// <summary>#1199 · An observation walk. <b>A dead end by design is the whole point of the room</b>:
        /// a straight tube out from a concourse over the drop, lit the whole way, glass underfoot, a rail at
        /// the blind end — and a second way out of it would have to go somewhere, which is the one thing
        /// there is nowhere for it to go. Declared and never measured: it is long
        /// (<see cref="ObservationWalk.LengthDu"/> is three times the bedroom-small threshold), so no number
        /// could ever let it off, and that is precisely why it has to be named.
        ///
        /// <para>#1199 (2026-09-18) · <b>The reason is re-stated for the T, because the room grew.</b> The
        /// walk is a tube AND the gallery across the end of it now — <see cref="ObservationWalk.GalleryWidthDu"/>
        /// of glass with a rail along it, a vending cafeteria against its back wall, and ten people's worth
        /// of standing room. That is a bigger room and a busier one, and the honest question is whether
        /// growing it has quietly turned one exemption into a licence. It has not, and the reason is
        /// unchanged in every word: <b>the whole T is ONE space with ONE doorway</b> — the tube opens into
        /// the gallery across its full width, with no leaf and no jambs, the way a corridor opens into the
        /// room it belongs to — and a second way out would still have to go somewhere. There is nowhere for
        /// it to go, because the other three walls are the drop. What the crossbar adds is DISTANCE from
        /// that one door, and distance is what a fire code is normally about; the answer is the same as it
        /// was at 24 du of bare tube, because the objection was never the length. It is a structure hung off
        /// the side of a station over vacuum, and the way out of it is the way in.</para>
        ///
        /// <para>ONE member covers the whole T on purpose. A second member for the gallery would be the
        /// list growing by arithmetic — one more name every time the same room gets bigger — which is the
        /// exact drift <see cref="ReasonFor"/> exists to stop.</para></summary>
        ObservationWalk = 2,
    }

    /// <summary>#822 · Why the law lets this kind of room off — the sentence that belongs beside the name,
    /// handed back rather than left in a comment so the list can be held to being reasoned. Not
    /// player-facing: the fire code is a building law and the building never explains itself to a
    /// captain.</summary>
    public static string ReasonFor(FireCodeExemption exemption) => exemption switch
    {
        FireCodeExemption.BedroomSmall =>
            "a space you can cross in two paces is never out of reach of its one door",
        FireCodeExemption.ObservationWalk =>
            "a dead end by design is the whole point of the room",
        _ => "",
    };

    /// <summary>#822 · <b>DOES A ROOM SATISFY THE STANDING LAW?</b> The whole of the fire code in one
    /// sentence: two ways out, unless it is on the list by name. Written here rather than on the room type
    /// so the carve, the sweep, the guards and the havens' own one-doorway room are all reading the same
    /// one, and so the day a third exemption is argued for there is exactly one place it lands.</summary>
    public static bool MeetsFireCode(int exits, FireCodeExemption exemption) =>
        exemption != FireCodeExemption.None || exits >= FireCodeMinExits;

    /// <summary>
    /// #822 · How many doors a run of street frontage this long is served by, fire code included. The whole
    /// of the door law in one function, so the carve, the guards and any later sweep are reading one
    /// sentence.
    ///
    /// <para>The fire code is about WAYS OUT and not about street doors, so a room that already has another
    /// one — a suite with a gate onto the green — is not made to cut a second leaf in a face that has no
    /// room for it. That is not a softening: it is #724's jamb law and this one meeting. A 19 du end block
    /// forced to carry two 6.4 du leaves has 2 du of pier between them and 1.5 du at each end, and a captain
    /// standing anywhere on that face is within a sidestep of two different openings — which the funnel
    /// reads as <i>standing in a doorway</i> and answers by holding still. Watched go red exactly there:
    /// <c>+3.90 du off the centreline … 400 presses left the captain on the near side of the wall</c>. The
    /// room needs two ways out; it does not need both of them in the same wall.</para>
    /// </summary>
    /// <param name="frontageDu">The room's street face.</param>
    /// <param name="hasAnotherWayOut">Whether the room has an exit that is not in this face — today, a gate
    /// onto the park.</param>
    public static int DoorsForFrontage(double frontageDu, bool hasAnotherWayOut = false)
    {
        int wanted = Math.Max(1, (int)Math.Round(
            frontageDu / RingStreetFaceDuPerDoor, MidpointRounding.AwayFromZero));
        return frontageDu <= FireCodeSmallRoomDu || hasAnotherWayOut
            ? wanted
            : Math.Max(FireCodeMinExits, wanted);
    }

    /// <summary>
    /// #813 · WHAT IS STENCILLED ON A ROOM WITH THE VIEW — the six plates the block hangs on its park-facing
    /// frontage, and nowhere else in the building.
    ///
    /// <para>#775's amenity gradient says amenities follow rank. The Manhattan ruling is where that becomes
    /// a MAP: the rooms on the green are the expensive ones, so they get a vocabulary the corridors do not.
    /// Every other plate down here is drawn from <see cref="SignFor"/>'s own register of departments and
    /// refusals — <c>QUOTA OFFICE</c>, <c>DO NOT ADMIT UNESCORTED</c> — and those still go on the block's
    /// CORNER rooms, which stand past the end of the park's wall and have nothing to look at. The gradient
    /// is legible without a word being said about it: read along one wall and the rooms get better as the
    /// green comes into view.</para>
    ///
    /// <para>§13.8 holds, and this row is a soft place to break it exactly as the back of house is. Every
    /// one of these says what a ROOM is — a booking, a signature, an appointment — and not one of them says
    /// what the facility is for. The nearest any of them comes is #770's negotiation room, and all it names
    /// is where you book it: at the counter, which is the same sentence a cabinet's plate has carried since
    /// #751 (<see cref="CabinetPlate"/>). The building rents rooms with a view of a garden it built to
    /// squeeze morale out of a workforce, and it advertises the aspect.</para>
    /// </summary>
    public static readonly IReadOnlyList<string> ParkViewPlates =
    [
        "REGISTERED OFFICE · GARDEN ASPECT",
        "NEGOTIATION ROOM · BOOK AT THE COUNTER",
        "SIGNATORY SUITE · TWO KEYS",
        "SENIOR ROTA · GREEN SIDE",
        "PRIVILEGED RECORDS · READING ROOM",
        "RECEPTION · APPOINTMENTS HELD",
    ];

    /// <summary>
    /// #821 · THE ONE ROOM ON THE BLOCK THAT IS NOT AN OFFICE.
    ///
    /// <para>Owner, standing in the park on the evening of 2026-08-11: <i>"let's add toilets there.. we
    /// might want to hide from guards in one toilet cubicle we lock from inside :-D"</i>. The park is the
    /// building's one public ground and it had nowhere to wash your hands.</para>
    ///
    /// <para>It is a NEAR-band room re-plated — the premium band, which is where the hall is and therefore
    /// where the public already are — and it is exactly one per block, chosen off the ground rather than
    /// rolled (see <c>WashroomFrontageOn</c>). In practice that lands it on the band's NARROW end block: a
    /// building does not give its garden aspect to the WCs, which is #775's amenity gradient arriving one
    /// more time as plumbing, and it is the reason the plate below claims no view.</para>
    ///
    /// <para>§13.8 holds. It says what the room is and nothing about what the facility is for, and NO PASS
    /// REQUIRED is the canteen's own clause (<see cref="AmenitySigns"/>) — a fact about band 0 the building
    /// has been advertising since #590 and still never explains.</para>
    /// </summary>
    public const string ParkWashroomPlate = "🚻 PUBLIC WASHROOMS · NO PASS REQUIRED";

    /// <summary>#751 · The three sizes of round top a caterer buys, smallest first. The owner's own three
    /// (#746, <i>"tables should seat 2/4/more, not all pairs"</i>), stated as a list so a guard can pin them
    /// without knowing the arithmetic that fills a hall with them.</summary>
    public static readonly IReadOnlyList<int> HallTopSizes = [2, 4, 6];

    /// <summary>
    /// #707 · WHICH DOOR PLATES BELONG TO SOMEBODY RATHER THAN TO SOMETHING — the rooms that get an
    /// en-suite.
    ///
    /// <para>The criterion, so it can be argued with instead of guessed at: <b>a plate is principal when it
    /// names an OFFICE or an AUTHORITY — somewhere a decision gets signed — rather than a process, a store,
    /// or a room where work is done TO somebody.</b> COLD STORE 2 is a place things are kept; SUBJECT PREP
    /// is a place things are done; QUOTA OFFICE is a place a person sits and rules on other people, and
    /// that person had a door of their own and did not queue for the cubicles on B1.</para>
    ///
    /// <para>And the RATIO is the rank difference, emergent and never stated: one plate in eight at a
    /// branch office, five in twelve at the head office. A captain who has crawled a Hive and then walks a
    /// head-office corridor sees private washrooms on half the doors, and nothing anywhere tells them what
    /// that means.</para>
    ///
    /// <para>Written as a list of plates taken verbatim out of <see cref="SignsFor"/> rather than as a
    /// keyword match on the string. A match on "OFFICE" would silently collect MANIFEST OFFICE and QUOTA
    /// OFFICE and then, the day somebody writes a plate reading POST OFFICE, that too — a rule that selects
    /// by accident is this repo's fifth bug class wearing a clever hat. Every entry here is proved to exist
    /// in some kind's vocabulary by <c>EveryPrincipalPlateIsAPlateThisBuildingActuallyHangs</c>.</para></summary>
    public static bool IsPrincipalRoom(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        foreach (string p in PrincipalPlates)
        {
            if (string.Equals(p, plate, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The plates a person sat behind. See <see cref="IsPrincipalRoom"/> for the criterion.</summary>
    public static readonly string[] PrincipalPlates =
    [
        "CONTINUITY — AUTHORISED ONLY",                       // Laboratory: the one plate that grants
        "OCCUPATIONAL REVIEW", "QUOTA OFFICE",                 // ProcessingDepot: a panel, and a desk
        "AUDIT — NO ADMITTANCE",                               // RecordsAnnex
        "CONSENT FILES",                                       // BlackClinic: somebody countersigned those
        "MANIFEST OFFICE",                                     // TransitStation
        // #411 · The head office is mostly people who sign things, and it shows in the plumbing.
        "OFFICE OF THE REGISTRAR", "ESTABLISHMENT BOARD", "COMMITTEE ROOM 2", "APPROPRIATIONS",
        "DEPUTATIONS",
    ];
}

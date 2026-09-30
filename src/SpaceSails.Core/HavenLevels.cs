using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #1253 · <b>DOWN BELOW — A HAVEN HAS FLOORS NOW, AND THIS IS THE ARITHMETIC OF THEM.</b>
///
/// <para>Owner, 2026-09-20: <i>"could we add a basement level to the observation deck station, so the
/// tailing task could start from the basement cabin and end at the observation deck? Otherwise the followed
/// distance is easily very short. The main hall could have multiple elevators… good for tailing."</i></para>
///
/// <para>The audit on #1253 answered the portability question before the design was written: <b>the stone is
/// portable and the Hive's floor machinery is not</b>. Every shaft call underground takes
/// <c>in SurfaceLayout.Field</c> — the regolith envelope — and the whole floor-change path is gated on an
/// excursion object that a berth does not have. So a haven cannot borrow <see cref="UndergroundComplex"/>'s
/// SHAFTS; what it borrows is the part of that file which is <i>travel</i> and not <i>moon</i>: the
/// <see cref="UndergroundComplex.LiftStop"/> row and the panel that draws a list of them.</para>
///
/// <h3>What is here</h3>
///
/// <para>Two floors, their names, the plates painted on the level nobody is invited onto, and the stop list a
/// cage's panel offers. Pure, and in Core for the reason every other law in this game is: the panel a captain
/// presses, the ride that answers it and the guards that hold both to the same answer must all be reading one
/// sentence. WHICH haven has a lower level is the client's (a <c>StationSpec</c> carries it or does not) —
/// that is a fact about a station's dressing, and this file knows nothing about any station.</para>
///
/// <h3>Why there is no air column</h3>
///
/// <para>A moon's panel asks <see cref="UndergroundComplex.HoldsPressure"/> of every row it draws, because
/// underground the answer differs per floor and #802's scar is a row that TYPED <c>true</c> over airless
/// ground. A station is the other case entirely: a haven's whole interior is the pressurised envelope a
/// captain walks in shirtsleeves, and a service level under a concourse is inside the same hull as the
/// concourse. It is said once, here, with the reason beside it — not typed nine times into a row builder.</para>
/// </summary>
public static class HavenLevels
{
    /// <summary>The floor a docked captain walks off the tube onto — the immigration hall, the bar, and
    /// (at the one station that has one) the observation walk. Zero, because every coordinate in a haven
    /// was written against it and the level term is an addition rather than a re-basing.</summary>
    public const int Concourse = 0;

    /// <summary>#1253 · The service level under it. NEGATIVE, so that "down" reads the same way here as it
    /// does underground and a page that holds one integer for "which floor" cannot mean two things by
    /// it.</summary>
    public const int ServiceLevel = -1;

    /// <summary>Every floor a haven with a lower level has, top down — the order a panel draws and a sweep
    /// walks. A list rather than a range, because a building's floors are a fact about the building.</summary>
    public static IReadOnlyList<int> Levels { get; } = [Concourse, ServiceLevel];

    /// <summary>Is this a floor a haven can be on at all? Asked by the boot cheat and by the deck cache, so
    /// a typo in a URL is a refusal rather than a station built at level −4.</summary>
    public static bool IsALevel(int level) => level is Concourse or ServiceLevel;

    /// <summary>What is written on the button, and on the plate over the car. The concourse says what a
    /// concourse says; the level below says what the station is willing to call it in public.</summary>
    public static string NameOf(int level) => level == ServiceLevel ? ServiceLevelPlate : ConcoursePlate;

    /// <summary>#1332 A · …and the same question at a station whose lower stop has a name of its own. The
    /// lower stop is <paramref name="below"/> (what <see cref="StopNameOf"/> reads off that station's plate) and
    /// the concourse is the concourse everywhere. Null is Selene Gate's answer and every older caller's:
    /// <see cref="ServiceLevelPlate"/>, to the byte.</summary>
    public static string NameOf(int level, string? below) =>
        level == ServiceLevel ? below ?? ServiceLevelPlate : ConcoursePlate;

    /// <summary>#1253 · The upper stop's name.</summary>
    public const string ConcoursePlate = "CONCOURSE";

    /// <summary>#1253 · …and the lower one's. The button says only this; the DOORS down there say the rest
    /// (<see cref="NoPublicAccessPlate"/>).</summary>
    public const string ServiceLevelPlate = "SERVICE LEVEL";

    /// <summary>#1253 · What the level is called on the one label the floor itself carries. The design
    /// issue's own word for it, and the only name this place is ever given.</summary>
    public const string LowerConcoursePlate = "LOWER CONCOURSE";

    /// <summary>
    /// #1253 · <b>THE PLATE ON EVERY DOOR DOWN THERE</b>, in the inspectorate register every maintenance
    /// sign in this game is stencilled in.
    ///
    /// <para>It obeys #563 — the door is TIME and never a key. There is no lock to pick and no card to find:
    /// the level is reached by calling a car, waiting for it, and riding it in front of whoever is watching,
    /// which is the lowest common standard of access a station has (staff ride it, so a captain with a
    /// laminate rides it). Nothing anywhere explains what is down there, and this sentence is the whole of
    /// what the building is willing to say.</para>
    /// </summary>
    public const string NoPublicAccessPlate = "SERVICE LEVEL — NO PUBLIC ACCESS";

    /// <summary>#1253 · How many crew cabins the row carries. Five, and the number is here rather than in the
    /// geometry for the reason every count in this game is stated once: the plates, the leaves, the fire-code
    /// sweep and the beat that seeds one of them a tenant are four readers of one fact.</summary>
    public const int Cabins = 5;

    /// <summary>#1253 · The plate over one of them — numbered, and NOBODY'S. Owner's standing rule about this
    /// station's people is that nothing confirms anything about anyone, and a name on a door would be the
    /// building doing the confirming. <paramref name="n"/> is one-based, the way a door number is.
    ///
    /// <para>#1280 · This is the leaf's FULL plate — what the door is called in the building's register, what
    /// <see cref="Interior.Egress"/> seeds a body's door roll on, and what a captain who walks up to it is
    /// refused at. What is PAINTED over it, where five of them stand in a row at one door's spacing, is
    /// <see cref="CabinDoorPlate"/>, and the two can never disagree: one is the other's opening.</para></summary>
    public static string CabinPlate(int n) => $"{CabinDoorPlate(n)} · {CabinTenancy}";

    /// <summary>
    /// #1279 · <b>WHAT IS PAINTED OVER ONE LEAF IN A ROW OF FIVE — the NUMBER, and nothing else.</b>
    ///
    /// <para>Owner's QA, played on the lower concourse: <i>"three of five door numbers cannot be read"</i>. The
    /// deck plan's caption band (#1218) does its job and stacks the five plates into rows rather than letting
    /// them print through each other — but a plate that is WIDER THAN THE DOOR IT NAMES has to take a row from
    /// its neighbour, and five plates interleaved across two rows ten pixels apart is a jumble a captain reads
    /// as one smeared line. Measured: the row is fourteen deck units across, so a cabin's frontage is about
    /// three, which is fifty-five screen pixels at the deck's own scale — and
    /// <c>CABIN n · CREW</c> is seventy-eight. <c>CABIN n</c> is thirty-nine, and thirty-nine fits.</para>
    ///
    /// <para><b>The number is what the beat is made of.</b> #1276's tail is a captain learning WHICH leaf a
    /// man walks into by watching him do it, and #563's ruling rests on his being able to read it. The
    /// tenancy is not: it is said once by the floor, on the plate over every car's door
    /// (<see cref="NoPublicAccessPlate"/>), and a word repeated five times at one door's spacing is what took
    /// the number away.</para>
    ///
    /// <para>A row of doors carries what is DIFFERENT between them. What they have in common belongs to the
    /// floor.</para>
    /// </summary>
    public static string CabinDoorPlate(int n) => $"CABIN {n}";

    /// <summary>#1279 · …and whose the row is, said once here rather than typed into two plates. It is the
    /// half of <see cref="CabinPlate"/> that <see cref="CabinDoorPlate"/> drops.</summary>
    public const string CabinTenancy = "CREW";

    /// <summary>#1253 · How many cages the concourse has. THREE, and it is the whole of the owner's ask:
    /// <i>"The main hall could have multiple elevators… good for tailing."</i> One car is a choke point and
    /// a come-back-here point — the same argument #801 made about the Hive's second car, one building along —
    /// and three that land in three different places turn a ride into a decision the captain has to read off
    /// somebody else.</summary>
    public const int Cages = 3;

    /// <summary>
    /// #1253 · <b>WHAT A HAVEN CAGE'S PANEL OFFERS, STANDING ON <paramref name="level"/>.</b>
    ///
    /// <para>The same <see cref="UndergroundComplex.LiftStop"/> rows a moon's cage draws, through the same
    /// <c>LiftPanel.razor</c>, and that reuse is the point: a second lift surface would be a second set of
    /// buttons to keep in step with the first. What is deliberately NOT reused is
    /// <see cref="UndergroundComplex.LiftPanel"/> itself — every line of it is about a moon (bands, gates,
    /// authority cards, dead air, a keypad), and a berth has none of those. <c>ShaftsOn(field)</c> is not
    /// asked at all: there is no field.</para>
    ///
    /// <para>Both stops, always, from either floor. There is no gate on this car and nothing to earn: #600's
    /// scar is a car that only went down, and the honest way not to repeat it is a panel that cannot.</para>
    /// </summary>
    public static IReadOnlyList<UndergroundComplex.LiftStop> Panel(int level) => Panel(level, null);

    /// <summary>#1332 A · <b>THE SAME PANEL AT A STATION WHOSE LOWER STOP HAS A NAME OF ITS OWN.</b> The rows,
    /// the order and the air are <see cref="Panel(int)"/>'s; only the lower button's word is the station's —
    /// <paramref name="below"/>, which is <see cref="StopNameOf"/> of that station's plate. Null is
    /// <see cref="ServiceLevelPlate"/>, which is Selene Gate's panel to the byte.</summary>
    public static IReadOnlyList<UndergroundComplex.LiftStop> Panel(int level, string? below)
    {
        var stops = new List<UndergroundComplex.LiftStop>(Levels.Count);
        foreach (int stop in Levels)
        {
            // The air is the hull's, and it is said ONCE. A haven is the pressurised envelope a captain
            // walks in shirtsleeves; a service level under a concourse is inside that same envelope, and a
            // row that asked a moon's HoldsPressure about a space station would be answering a question
            // about regolith. #802's law is about a row TYPING an answer nobody checked — this row's answer
            // is the reason the whole building is walkable, and it is stated here with its reason.
            stops.Add(new UndergroundComplex.LiftStop(
                stop, NameOf(stop, below), Pressurised: true, IsCurrent: stop == level, Refusal: null));
        }

        return stops;
    }

    /// <summary>#1253 · Which floor a press on this panel is asking for, or null when the row is the one the
    /// car is already on. One reader, so the panel's disabled row and the ride's refusal are one rule.</summary>
    public static int? RideFrom(int level, in UndergroundComplex.LiftStop stop) =>
        stop.IsCurrent || stop.Level == level || !IsALevel(stop.Level) ? null : stop.Level;

    /// <summary>#1253 · How the field book names a floor that is not the one a berth's name already implies.
    /// Empty at the concourse — a note filed in the bar must go on reading exactly as it did — and the level's
    /// own plate below it, so one berth's notes group into two drawers rather than one.</summary>
    public static string BookSuffix(int level) =>
        level == Concourse ? "" : NameOf(level);

    /// <summary>#1332 A · …at a station whose lower stop has a name of its own: the drawer is that name.</summary>
    public static string BookSuffix(int level, string? below) =>
        level == Concourse ? "" : NameOf(level, below);

    // ── #1332 A · EVERY HUB IS A LOBBY ─────────────────────────────────────────────────────────────────────
    //
    // Owner, 2026-09-29: "The big round immigration points already look like elevator lobbies, so we might as
    // well have those hubs have elevators that take to apartment-hotel-like, usually locked, spaces down
    // below." So they are. Every haven's hub gets Selene Gate's lower level in Selene Gate's shape, and what
    // differs per station is ONE plate — the floor's single label, painted over its cars upstairs too — and
    // the word its panel's lower button carries, which is that plate's first word(s) exactly as Selene
    // Gate's SERVICE LEVEL is the first words of SERVICE LEVEL — NO PUBLIC ACCESS. Fable canon, verbatim;
    // the cabin doors keep the shipped CABIN n everywhere, because the hotel is a word on the floor and not
    // on the doors.

    /// <summary>#1332 A · Cinder Roost's lower level. Fable canon, verbatim.</summary>
    public const string CinderRoostPlate = "BERTH HOTEL · RESIDENTS ONLY";

    /// <summary>#1332 A · The Rusty Roadstead's (the-space-bar). Fable canon, verbatim.</summary>
    public const string SpaceBarPlate = "LONG-STAY · KEYS AT THE BAR";

    /// <summary>#1332 A · The Red Eye's. Fable canon, verbatim.</summary>
    public const string RedEyePlate = "CREW QUARTERS · NO PUBLIC ACCESS";

    /// <summary>#1332 A · Ringside Exchange's. Fable canon, verbatim.</summary>
    public const string RingsidePlate = "MEMBERS' ROOMS";

    /// <summary>#1332 A · The Tilt's. Fable canon, verbatim.</summary>
    public const string TiltPlate = "ROOMS · MIND THE FLOOR";

    /// <summary>#1332 A · The Deep's. Fable canon, verbatim.</summary>
    public const string DeepPlate = "COLD ROOMS · BOOK AT THE DESK";

    /// <summary>#1332 A · <b>THE FIRST RIDE DOWN, TOLD ONCE AT EACH HAVEN</b> — a pulse at Status rank, on a
    /// free slot, and never again at that station. Fable canon, verbatim. It says nothing about who, and it
    /// is the only sentence the lower levels have besides their plates.</summary>
    public const string FirstRideLine =
        "The car stops where the public map does not go. Somebody lives here, and it is not you.";

    /// <summary>#1332 A · <b>WHAT A LOWER STOP IS CALLED ON THE BUTTON</b> — a plate's first word(s): what
    /// stands before its first <c> · </c> or <c> — </c>, or the whole plate when it has neither. So
    /// <c>SERVICE LEVEL — NO PUBLIC ACCESS</c> is <c>SERVICE LEVEL</c> (Selene Gate's button, which this
    /// reproduces rather than restates) and <c>MEMBERS' ROOMS</c> is itself. Read, never typed: a button
    /// that kept its own word would be a second name for one floor.</summary>
    public static string StopNameOf(string plate)
    {
        ArgumentNullException.ThrowIfNull(plate);
        int cut = plate.Length;
        foreach (string mark in (string[])[" · ", " — "])
        {
            int at = plate.IndexOf(mark, StringComparison.Ordinal);
            if (at > 0 && at < cut)
            {
                cut = at;
            }
        }

        return plate[..cut];
    }

    /// <summary>#1332 A · Every sentence this slice can put on a screen: the six new plates and the one
    /// line. (Selene Gate's plates are #1253's and are held where they were written.)</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return CinderRoostPlate;
        yield return SpaceBarPlate;
        yield return RedEyePlate;
        yield return RingsidePlate;
        yield return TiltPlate;
        yield return DeepPlate;
        yield return FirstRideLine;
    }
}

using System;
using System.Collections.Generic;

namespace SpaceSails.Core;

/// <summary>
/// #869 · THE DESK THAT STILL RISES — the sit-stand beat, the memory buttons, and what they remember.
///
/// <para>Owner, 2026-08-13, speaking from his own 160 x 90 cm electric desk: <i>"It has the displays on lamp
/// legs so I use the full surface under the monitors. And it got up- and down buttons to move the table to
/// work either with office chair, Salli standing (lab) chair or by standing while using the table. Surely in
/// futuristic lab they would have similar spec tables to ensure good ergonomics :-D"</i></para>
///
/// <h3>Why a motor is a noir beat</h3>
///
/// <para>It is the fans-still-turning beat, furniture-sized. The company that feeds contractors like hotel
/// guests and builds a park to squeeze morale out of a workforce bought ergonomic desks, line-item by
/// line-item — good equipment as a management technique is the whole building — and the motor is still doing
/// what it was specified to do for people who are not here. <b>Nobody in-world ever remarks that the desks
/// are nice.</b> The desks are simply nice; the one sentence about why is the whole of the gag, and it is
/// said to the captain and to nobody else.</para>
///
/// <h3>…and why the memory buttons are EVIDENCE</h3>
///
/// <para>A desk's memorised heights are a statement about the ABSENT person who worked at it — how many of
/// them there were, how tall, sitter or stander. Canon discipline holds absolutely (the 2026-07-30 ruling):
/// nothing is stated, nothing is confirmed, no sensor agrees. The furniture quietly deposes a witness and
/// the building never corroborates it. That is the #601-era ammo-as-evidence family, at furniture scale.</para>
///
/// <h3>The prank</h3>
///
/// <para>Owner: <i>"love the gag of messing with somebodys desks memorized height options :-D"</i>. Nothing
/// files, nothing scores, no one remarks on it, ever. It costs and pays nothing, which IS the joke — the
/// pettiest verb in the game.</para>
///
/// <h3>Where the copy lives, and why not one number does</h3>
///
/// <para>The four sentences below are CANONICAL and owner-authored, implemented verbatim; a guard hashes
/// them for that reason. The two points on a desk this file publishes (<see cref="TheEdge"/> and
/// <see cref="TheButtons"/>) are derived off the fixture box Core already laid and the seat Core already
/// published, so the renderer hangs a console on them and measures nothing — §13.15, and the reason this
/// project has twice put a captain in a wall.</para>
/// </summary>
public static class SitStandDesk
{
    // ── THE COPY, VERBATIM AND OWNER-AUTHORED ─────────────────────────────────────────────────────────

    /// <summary>#869 · The desk goes up. Canonical; changing a character of it changes canon.</summary>
    public const string RaisedLine =
        "🔼 The desk hums and rises to meet you. Somebody specified these once, line-item by line-item, "
        + "for people whose backs mattered to the schedule.";

    /// <summary>#869 · …and back down.</summary>
    public const string LoweredLine =
        "🔽 It sinks back to chair height without complaint. The motor has outlived the schedule, the "
        + "people, and the line-item.";

    /// <summary>#869 · THE PRANK. Nothing files, nothing scores, nobody remarks.</summary>
    public const string PrankLine =
        "You lean on the memory buttons and move preset two down a knuckle's width. Somewhere in the "
        + "future, somebody will stand here feeling slightly wrong about the world and never know why.";

    /// <summary>#869 · THE READ — the gumshoe half. Evidence about an absent person, confirmed by
    /// nothing.</summary>
    public const string ReadLine =
        "Two presets. One for sitting, and one set for somebody who stands a head taller than the chair "
        + "suggests. The desk remembers its people better than the rota does.";

    /// <summary>Which line one press of the edge says. Asked here so the press, the guard and any future
    /// renderer read one sentence rather than each choosing a string.</summary>
    /// <param name="raised">Is the desk UP after this press?</param>
    public static string LineFor(bool raised) => raised ? RaisedLine : LoweredLine;

    // ── WHAT IS DRAWN ON THE DECK ─────────────────────────────────────────────────────────────────────

    /// <summary>What the desk's own up/down control is labelled, with the verb on it (#783: <i>"why not use
    /// words like SIT DOWN here if it means sitting down?"</i>). NOT owner copy — see the PR body.</summary>
    public const string EdgePlate = "🔼 THE DESK EDGE · RAISE OR LOWER";

    /// <summary>…and the preset paddle at the other end of the same desk. NOT owner copy.</summary>
    public const string ButtonsPlate = "🎛 THE MEMORY BUTTONS · TWO PRESETS";

    // ── WHICH FITTINGS HAVE A MOTOR UNDER THEM ────────────────────────────────────────────────────────

    /// <summary>
    /// #869 · Is this the kind of fitting that goes up and down? Asked of the published KIND and never of a
    /// plate's wording, so a fixture renamed tomorrow keeps its answer (<see cref="RingOffice.IsWorkSurface"/>
    /// is written the same way and for the same reason).
    ///
    /// <para>The owner's sentence names two rooms — <i>"office chair, Salli standing (lab) chair"</i> — and
    /// these are those two rooms' surfaces. A canteen top, a reception counter and a negotiation table are
    /// not sit-stand desks; they are furniture a building bought once and bolted down.</para>
    /// </summary>
    public static bool HasAMotor(RingOffice.Fitting kind) =>
        kind is RingOffice.Fitting.DeskBank or RingOffice.Fitting.LabBench;

    /// <summary>
    /// #869 · WHICH DESK THIS SEAT BELONGS TO — the nearest motorised surface within the setback its own
    /// chair was laid at (<see cref="RingOffice.ChairSetbackDu"/>), or −1 where the seat is not at one.
    ///
    /// <para>Measured off the published boxes, which is the only honest way to ask it —
    /// <see cref="RingOffice.WorksAtASurface"/>'s own lesson, one building along: a seat is at a desk when a
    /// desk is within the setback the placer used to lay it there, and never when a plate says so.</para>
    /// </summary>
    /// <param name="fittings">The room's own furniture, as published.</param>
    /// <param name="seat">One of its seats.</param>
    public static int DeskFor(IReadOnlyList<RingOffice.Fixture> fittings, in RingOffice.Chair seat)
    {
        ArgumentNullException.ThrowIfNull(fittings);

        int best = -1;
        double nearest = double.MaxValue;
        for (int i = 0; i < fittings.Count; i++)
        {
            RingOffice.Fixture f = fittings[i];
            if (!HasAMotor(f.Kind))
            {
                continue;
            }
            double dx = Math.Max(Math.Max(f.X0 - seat.X, seat.X - f.X1), 0.0);
            double dy = Math.Max(Math.Max(f.Y0 - seat.Y, seat.Y - f.Y1), 0.0);
            double gap = Math.Sqrt((dx * dx) + (dy * dy));
            if (gap <= RingOffice.ChairSetbackDu + 0.01 && gap < nearest)
            {
                (best, nearest) = (i, gap);
            }
        }
        return best;
    }

    // ── THE TWO PLACES ON A DESK YOU PUT A HAND ───────────────────────────────────────────────────────

    /// <summary>
    /// #869 · WHERE THE UP/DOWN BUTTONS ARE — one end of the desk's own front face, on the side its chair
    /// is on.
    ///
    /// <para>The front face is the side of the box the seat is on, which is a fact the two published shapes
    /// answer between them: nothing here is told which wall the desk stands against, exactly as nothing in
    /// <see cref="ChamberFitting"/> is. The two controls take opposite ends of that face so a captain can
    /// stand at one without the other being nearer — see the PR body for the grip, which is the one thing
    /// about this feature the owner may want moved.</para>
    /// </summary>
    public static (double X, double Y) TheEdge(in RingOffice.Fixture desk, in RingOffice.Chair seat) =>
        EndOfTheFace(in desk, in seat, first: true);

    /// <summary>#869 · …and where the PRESET paddle is: the other end of the same face.</summary>
    public static (double X, double Y) TheButtons(in RingOffice.Fixture desk, in RingOffice.Chair seat) =>
        EndOfTheFace(in desk, in seat, first: false);

    private static (double X, double Y) EndOfTheFace(
        in RingOffice.Fixture desk, in RingOffice.Chair seat, bool first)
    {
        bool alongX = (desk.X1 - desk.X0) >= (desk.Y1 - desk.Y0);
        if (alongX)
        {
            double face = seat.Y >= desk.Y ? desk.Y1 : desk.Y0;
            return (first ? desk.X0 : desk.X1, face);
        }
        double faceX = seat.X >= desk.X ? desk.X1 : desk.X0;
        return (faceX, first ? desk.Y0 : desk.Y1);
    }

    /// <summary>Every sentence this file can put on a screen, for the canon sweep.</summary>
    public static IEnumerable<string> AllProse()
    {
        yield return RaisedLine;
        yield return LoweredLine;
        yield return PrankLine;
        yield return ReadLine;
        yield return EdgePlate;
        yield return ButtonsPlate;
    }
}

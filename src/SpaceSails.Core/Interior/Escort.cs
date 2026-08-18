using System;
using System.Collections.Generic;

namespace SpaceSails.Core.Interior;

/// <summary>
/// #731 v2 · FOLLOW ME — the other kind of door, and the other thing a walk can mean.
///
/// <para><b>Owner, 2026-08-06, on #751's cabinets:</b> <i>"Also it is dramatic telling when our contact wants
/// us to follow them into kabinetti :-D"</i></para>
///
/// <h3>Two doors, and they are opposites</h3>
///
/// <para><see cref="Egress"/> is about the door that is SHUT to you. Somebody crosses the hall, a plate the
/// captain's own TRY is refused at opens for them, and the conversation is over — <i>"I guess that concludes
/// the conversation"</i>, which is a full stop. Every function in that file takes a
/// <see cref="UndergroundComplex.LockedDoor"/> and nothing else, so the refusal is a TYPE.</para>
///
/// <para><b>This file is the mirror of it.</b> A cabinet's leaf is not a door that refuses anybody: it is a
/// gap in a wall, cut to the same <c>DoorHalf</c> the corridors are, and it opens for her exactly as it
/// opens for you. So the beat is not a door shutting in your face, it is a door <b>held open</b> — she stands
/// in it and looks back across the hall at you, and whether the scene continues is your legs' business. The
/// two are deliberately different types and different files, because the day they are one function with a
/// flag is the day a contact leads the captain into a room the captain cannot enter.</para>
///
/// <para>Which is why <see cref="WhereSheWaits"/> takes an <see cref="UndergroundComplex.Cabinet"/> — the
/// building's own booth, whose ways out are <see cref="SurfaceLayout.Doorway"/>s, which is the type of a
/// hole — and nothing in this file will accept a locked door at all.</para>
///
/// <h3>Why a cabinet and not a quiet corner</h3>
///
/// <para>#751 gave the cabinets the one mechanical fact that makes this beat mean something: <b>evidence is
/// not LOUD in there</b>, because the counter has no eyes in that room. #758 gave them a state — cloth by
/// default, a padded leaf if somebody decides — and it shipped <see cref="CabinetPrivacy.EscortsStage"/>
/// ahead of this lane, saying so out loud: <i>"#731's walkers are the ones who will call this."</i> So the
/// contact who will not say her business in a hall with eighty people in it has somewhere to say it, the
/// room already knows what it costs, and the character note — which of them dogs the door — is already
/// written and seeded on WHO she is.</para>
///
/// <h3>What is here and what is deliberately not</h3>
///
/// <para>Pure and world-blind, exactly as <see cref="Egress"/> is. It answers four questions — which move in
/// a scene is the one worth a private room, whether this counterpart is the sort who takes you to one, which
/// cabinet, and where a body stands to hold its door — and it knows nothing about frames, panels, walkers or
/// clocks. <b>And it authors not one sentence.</b> The whole beat is a person standing up and walking, and
/// the game says nothing about it; there is no prose in this file to sweep, which is the strongest form
/// §13.8 comes in.</para>
/// </summary>
public static class Escort
{
    /// <summary>How long she will stand in that doorway looking at you, as a fraction of a watch.
    ///
    /// <para>A quarter, and it is argued from both ends. <b>Long</b>, because the whole beat is that the game
    /// does not tell you to follow: a captain who has not understood yet is allowed to finish their drink,
    /// walk the hall, and work it out from a woman standing in a doorway looking at them — an hour of
    /// station time is that grace. <b>Bounded</b>, because a body that waits forever is a statue, and the
    /// owner's own answer to never following is the one this lane already ships: she goes, through a door
    /// that does not open for you, and that is the full stop arriving from the other direction.</para>
    ///
    /// <para>The watch turning over ends it regardless — the room forgetting is a rule this lane inherited
    /// and does not get to opt out of — so this is a ceiling inside a shift and never a second clock.</para>
    /// </summary>
    public const double PatienceFraction = 0.25;

    /// <summary>…in seconds, off the rota's own shift. Derived and never a second number: a patience written
    /// as "3600" would be a constant that stops meaning a quarter of a watch the day a watch changes
    /// length.</summary>
    public static double PatienceSeconds => PatronRota.WatchSeconds * PatienceFraction;

    /// <summary>How far outside a cabinet's leaf she stands while she waits. <see cref="Egress.DoorStandoffDu"/>
    /// and not a number of its own — where a body stands to use a door is one fact about a body and a door,
    /// and two opinions about it is the mirrored-constant bug with somebody's shoulder in a wall.</summary>
    public const double DoorStandoffDu = Egress.DoorStandoffDu;

    /// <summary>
    /// WHICH MOVE IS WORTH A PRIVATE ROOM — asked of the scene itself, and never of a list kept here.
    ///
    /// <para>The rule is the FIELD BOOK's. <see cref="Encounter.Move.Note"/> is what a move leaves in the
    /// captain's own book, and #757 put it on the move rather than in the client precisely so that "this
    /// sentence was worth writing down" is a fact the content file states. A move with a note is a move that
    /// changes what the captain knows; a move without one is a chair being pulled out or a glass arriving.
    /// So <b>the deal move is the last noted move in the scene</b> — last, because the ladder is written in
    /// the order it is climbed and the thing somebody crossed a room to say is at the top of it.</para>
    ///
    /// <para>Read that way, this needs no register of which scenes have deals in them: the haulier's ask
    /// (#757), the Hand's chit (#746) and the delegation's <c>Put it to them</c> (#770) are all already
    /// marked, by the authors who wrote them, for a different reason that happens to be the same reason.
    /// A scene with nothing noted in it — a stranger with a cup, a room you paid for and nobody came — has
    /// no deal move and nobody in it will ever lead you anywhere.</para>
    /// </summary>
    /// <returns>The move, or null when this scene has nothing in it worth a room.</returns>
    public static Encounter.Move? TheDealMoveIn(Encounter.Scene scene)
    {
        Encounter.Move? deal = null;
        IReadOnlyList<Encounter.Move> moves = scene.Moves ?? [];
        foreach (Encounter.Move move in moves)
        {
            if (move.Note is { Length: > 0 } && !string.Equals(move.Id, Encounter.Leave, StringComparison.Ordinal))
            {
                deal = move;
            }
        }
        return deal;
    }

    /// <summary>
    /// IS THIS ONE THE SORT WHO TAKES YOU SOMEWHERE PRIVATE?
    ///
    /// <para>Two conditions, and the first is not a roll. <b>The scene must HAVE a deal move</b>
    /// (<see cref="TheDealMoveIn"/>) — somebody with nothing to say cannot ask you to come and hear it said
    /// somewhere else, and a beat that walked the captain across a hall for a glass of something would be
    /// this game telling a lie with its legs. That clause is a fact about the scene's own state, which is
    /// where the answer was asked to come from.</para>
    ///
    /// <para>The second is the ordinary seeded coin, on (site, floor, watch, top, who), so the same shift
    /// always produces the same evening and a captain who stands up and sits down again has not re-rolled
    /// anybody's nerve. Not everybody does it: a contact who ALWAYS takes you into a cabinet is a corridor
    /// with a cutscene in it, and the beat is worth having because most of the time she just tells you.</para>
    /// </summary>
    public static bool LeadsYouIn(
        string bodyId, int level, long watch, int tableIndex, string who, Encounter.Scene scene)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(who);

        return TheDealMoveIn(scene) is not null
            && DiceRule.Roll(
                DiceRule.Seed($"hive:escort:leads:{bodyId}:{level}:{tableIndex}:{who}", watch), 2).Face == 1;
    }

    /// <summary>
    /// WHICH CABINET SHE TAKES YOU INTO — the nearest FREE one, and free means nobody is in it.
    ///
    /// <para>Nearest rather than seeded, because it is not a matter of taste: somebody who wants a word out
    /// of the hall's earshot walks to the closest empty room, and a contact who crosses the whole back wall
    /// past two empty booths to use the third is a body obeying a die instead of a reason. Ties go to the
    /// lower ordinal, so the answer is total and the same on every machine.</para>
    ///
    /// <para>Occupancy is <see cref="CanteenRegulars.TableSeat.Taken"/>'s — the room's own roster off the
    /// frozen watch — so a booth with somebody's meeting already in it is not on offer. A hall whose three
    /// cabinets are all busy simply has nowhere to take you, and the answer is null: she says it at the
    /// table like anybody else.</para>
    /// </summary>
    /// <param name="tops">The room's tops, as <see cref="CanteenRegulars.Tables"/> published them.</param>
    /// <param name="fromX">Where she is standing — the table she is walking away from.</param>
    /// <param name="fromY">…and the other half.</param>
    public static CanteenRegulars.TableSeat? AFreeCabinet(
        IReadOnlyList<CanteenRegulars.TableSeat> tops, double fromX, double fromY)
    {
        ArgumentNullException.ThrowIfNull(tops);

        CanteenRegulars.TableSeat? best = null;
        double nearest = double.PositiveInfinity;
        foreach (CanteenRegulars.TableSeat top in tops)
        {
            if (top is not { Cabinet: > 0, Taken: false })
            {
                continue;
            }
            double d = ((top.X - fromX) * (top.X - fromX)) + ((top.Y - fromY) * (top.Y - fromY));
            if (d < nearest)
            {
                nearest = d;
                best = top;
            }
        }
        return best;
    }

    /// <summary>
    /// WHERE A BODY STANDS TO HOLD A CABINET'S DOOR OPEN — on the HALL side of it, and the hall is the floor
    /// that is not inside anybody's booth.
    ///
    /// <para>A cabinet has more than one way out (#822 put a leaf in each party wall so the run is not three
    /// traps), and only one of them opens on the hall — the others open into the cabinet next door. Telling
    /// them apart by geometry would be a second opinion about a wall this file did not build, so it is asked
    /// the only way that cannot be wrong: <b>a standing place is on the hall side when it is inside no
    /// cabinet on this floor.</b> A spot inside the neighbouring booth is a body waiting in somebody else's
    /// meeting, and it is refused for the same reason.</para>
    ///
    /// <para>Sounded with <see cref="SurfaceCollision.Blocked"/> at the captain's own width — the same
    /// predicate the A* lattice asks — so a point this returns is a point the walk can actually reach, and a
    /// null is the honest answer rather than a woman standing in a wall.</para>
    /// </summary>
    /// <param name="cabinet">The booth she is taking you into.</param>
    /// <param name="all">Every cabinet on this floor, for the "inside nobody's booth" test.</param>
    /// <param name="radius">Her body, which is the captain's body: one law, one width.</param>
    /// <param name="walls">The floor's own stone.</param>
    public static DeckReachability.Point? WhereSheWaits(
        in UndergroundComplex.Cabinet cabinet,
        IReadOnlyList<UndergroundComplex.Cabinet> all,
        double radius,
        IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        ArgumentNullException.ThrowIfNull(all);
        ArgumentNullException.ThrowIfNull(walls);

        foreach (SurfaceLayout.Doorway way in cabinet.Ways)
        {
            double mx = (way.X1 + way.X2) / 2, my = (way.Y1 + way.Y2) / 2;
            double ax = way.X2 - way.X1, ay = way.Y2 - way.Y1;
            double len = Math.Sqrt((ax * ax) + (ay * ay));
            if (len < 1e-9)
            {
                continue;
            }

            double hx = -ay / len * DoorStandoffDu, hy = ax / len * DoorStandoffDu;
            (double X, double Y)[] sides = [(mx + hx, my + hy), (mx - hx, my - hy)];
            foreach ((double X, double Y) side in sides)
            {
                if (SurfaceCollision.Blocked(side.X, side.Y, radius, walls) || InsideAnyOf(all, side.X, side.Y))
                {
                    continue;
                }
                return new DeckReachability.Point(side.X, side.Y);
            }
        }
        return null;
    }

    /// <summary>Is this point inside one of the floor's booths? The cabinet's own box and nothing else — the
    /// same <see cref="UndergroundComplex.Cabinet.Contains"/> the room was carved with.</summary>
    public static bool InsideAnyOf(
        IReadOnlyList<UndergroundComplex.Cabinet> all, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(all);

        foreach (UndergroundComplex.Cabinet c in all)
        {
            if (c.Contains(x, y))
            {
                return true;
            }
        }
        return false;
    }
}

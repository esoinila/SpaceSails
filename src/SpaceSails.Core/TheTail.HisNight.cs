using System;

namespace SpaceSails.Core;

/// <summary>
/// #1253 slice 2 · <b>HIS NIGHT — the route the person of interest walks once the room is done with him,
/// and it starts a floor down now.</b>
///
/// <para>Owner, 2026-09-20: <i>"could we add a basement level to the observation deck station, so the tailing
/// task could start from the basement cabin and end at the observation deck? <b>Otherwise the followed
/// distance is easily very short.</b> The main hall could have multiple elevators… good for tailing."</i></para>
///
/// <para>#1199's route was one leg: up from a chair, across the concourse, out over the drop. It is five
/// now, and the three new ones are a floor the captain has to decide to follow him onto:</para>
///
/// <list type="number">
/// <item>the bar → a car (<see cref="TheCarHeTakesDown"/>);</item>
/// <item>the service level → his own cabin (<see cref="HisCabin"/>), and he goes IN — the leaf shuts and it
/// does not open for a captain (#563: the door is time, not a key, and he is behind it);</item>
/// <item>a wait inside (<see cref="TheCabinWaitSeconds"/>);</item>
/// <item>out, and a car back up (<see cref="TheCarHeTakesUp"/>), <b>which may not be the one he came down
/// on</b>;</item>
/// <item>the hall, the tube, the gallery, the vanish — exactly as #1254 ships it, not a line of it
/// touched.</item>
/// </list>
///
/// <h3>Why every choice here is a SEED and not a roll</h3>
///
/// <para>Because the captain is supposed to be able to learn them. Which car a man takes is the whole of the
/// craft the owner asked for — <i>which lift did he take, and where did it put him</i> — and a choice
/// re-rolled per visit would be a coin-flip wearing a mechanic's clothes. Seeded on the berth and his own
/// name (<see cref="TheTail.SeedFor"/>'s own shape), so one universe always answers the same and two
/// captains comparing notes agree.</para>
///
/// <para>Pure and world-blind, like everything else in Core. <b>It authors not one sentence</b>: a man goes
/// downstairs, is behind a door for a while, and comes back up, and the game says nothing about any of
/// it.</para>
/// </summary>
public static class TheTailsNight
{
    /// <summary>#1253 · Which of the row's cabins is his, zero-based — or −1 where the station has no row to
    /// pick from, which is the honest answer and never cabin 0 by default.
    ///
    /// <para>Seeded and NEVER stated: no plate, no line, no card and nothing anywhere in the game that says
    /// whose door it is. The captain learns it by watching a man walk up to it, which is the only way
    /// anybody learns anything at this station.</para></summary>
    public static int HisCabin(string bodyId, string person, int cabins)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(person);
        return cabins <= 0 ? -1 : (int)(TheTail.SeedFor(bodyId, person) % (ulong)cabins);
    }

    /// <summary>
    /// #1253 · <b>WHICH CAR HE TAKES DOWN.</b> Seeded on the berth and his name, so the answer is the same
    /// every visit and the captain can learn it — which is the entire point of there being three.
    /// </summary>
    public static int TheCarHeTakesDown(string bodyId, string person, int cars)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(person);
        return cars <= 0 ? -1 : (int)((TheTail.SeedFor(bodyId, person) >> 8) % (ulong)cars);
    }

    /// <summary>
    /// #1253 · <b>…AND WHICH ONE HE COMES BACK UP ON, WHICH MAY BE A DIFFERENT ONE.</b>
    ///
    /// <para>A different bit of the same seed, so a captain who has learnt where he goes down has learnt
    /// exactly half of it. That asymmetry is the beat: you can be waiting at the right door and still be at
    /// the wrong one, and nothing tells you which — <b>you find out by there being nobody there</b>, which
    /// is the same sentence this whole feature is built out of.</para>
    ///
    /// <para>It is allowed to be the same car, and on a three-car station it is about one time in three.
    /// Forcing it to differ would be the world arranging itself so the captain always loses him once, which
    /// is a mechanic rather than a place.</para>
    /// </summary>
    public static int TheCarHeTakesUp(string bodyId, string person, int cars)
    {
        ArgumentNullException.ThrowIfNull(bodyId);
        ArgumentNullException.ThrowIfNull(person);
        return cars <= 0 ? -1 : (int)((TheTail.SeedFor(bodyId, person) >> 24) % (ulong)cars);
    }

    /// <summary>
    /// #1253 · <b>HOW LONG HE IS BEHIND THAT DOOR, as a fraction of the room's own watch.</b>
    ///
    /// <para>Derived from <see cref="Interior.Escort.PatienceFraction"/>, because the FICTION is the
    /// escort's, seen from the other side of it: a leaf a captain is refused at, with somebody on the far
    /// side of it, and a captain in a corridor with nothing to do but decide how long to stand there. That
    /// quarter-watch is the ceiling on that shape, and it is stated once, over there, where the beat it
    /// belongs to lives.</para>
    ///
    /// <para><b>A twentieth of it, and the reason is the one the owner already ruled on.</b> #1199's own wait
    /// began as the escort's whole quarter and was played: <i>"YES, shorten the wait."</i> A wait the captain
    /// spends WATCHING is not a wait he spends being waited FOR — there is nothing for him to do but stand
    /// there, and an hour of the player's evening in a service corridor is the same hour this project already
    /// took out of an empty tube. The arithmetic lands on <see cref="ObservationWalk.TheWaitSeconds"/> to the
    /// second, which is a CHECK rather than the source: two clocks in one beat, each derived from the fiction
    /// it belongs to, agreeing about how long a man is out of sight.</para>
    /// </summary>
    public const double CabinWaitFraction = Interior.Escort.PatienceFraction / 20.0;

    /// <summary>#1253 · …in seconds, off the rota's own shift. Derived and never a second number, for
    /// <see cref="Interior.Escort.PatienceSeconds"/>'s reason exactly.</summary>
    public static double CabinWaitSeconds =>
        Interior.PatronRota.WatchSeconds * CabinWaitFraction;

    /// <summary>
    /// #1253 · <b>HOW LONG A LEG THE CAPTAIN IS NOT WATCHING TAKES.</b>
    ///
    /// <para>The world does not stop because nobody is looking at it. When the captain is on the other floor,
    /// the man is not simulated as a body — there is no deck under him to collide with — so his leg is
    /// counted off the distance it covers at the pace every walker in this game walks at
    /// (<see cref="Interior.NpcWalk.PaceDu"/>), and he arrives when a man walking it would have.</para>
    ///
    /// <para><b>The same pace, and never a shortcut.</b> A leg that ran faster off-screen would be a man who
    /// beats a captain who followed him — the world arranging itself around who is watching, which is the one
    /// thing a tail cannot survive. A leg that ran slower would hold him for a captain who took the long way
    /// round, which is the same lie the other way.</para>
    /// </summary>
    public static double LegSeconds(double distanceDu) =>
        distanceDu <= 0 ? 0 : distanceDu / Interior.NpcWalk.PaceDu;
}

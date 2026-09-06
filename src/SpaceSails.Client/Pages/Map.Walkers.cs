using System;
using System.Collections.Generic;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #731 · THE EXIT IS THE FULL STOP — the band of the deck's figure buffer that belongs to people leaving
/// and people arriving, and the frame that walks them.
///
/// <para><b>Owner, 2026-08-06, streamed during the smoke run:</b> <i>"The NPCs but not reevers could also
/// use the A* if we want to show them leaving a scene etc. If they go behind a door that is locked to us, we
/// use that as 'I guess that concludes the conversation' point in the plot / situation."</i> And the
/// limitation it fixes: <i>"Like on the bar now they have to wait for us to leave before they can sit up… or
/// leave the bar."</i></para>
///
/// <para><b>And the other direction, the same day:</b> <i>"In the space bars there are lot of cases where we
/// can have npcs arrive at bar from locked place or go to a locked place. Now it is possible to have NPC ask
/// to sit down at our table and offer a quest! This is the classic TTRPG event."</i></para>
///
/// <h3>What this file is, and what it deliberately is not</h3>
///
/// <para>It is a BAND and a STEP, and nothing else. The walking is <see cref="NpcWalk"/>'s, in Core, over
/// <c>AutoWalk</c>'s route and <c>SurfaceCollision.Slide</c>'s stone — the same two primitives the captain's
/// boots and the guard's round already spend. Who leaves and out of which door is <see cref="Egress"/>'s, in
/// Core, off the frozen watch. This file owns the two things that can only be owned here: which slots of the
/// figure buffer the walkers are written into, and when the frame steps them.</para>
///
/// <para><b>Nothing here explains anything.</b> A regular gets up, crosses the hall, and goes through a door
/// the captain's own TRY is refused at — and not one line of prose is filed, pulsed or raised about it. That
/// is the whole beat and it is §13.8 in its purest form: the room told you something and the game did not.
/// The canon sweep on this lane exists to keep it that way.</para>
/// </summary>
public partial class Map
{
    /// <summary>#731 · How many walkers the surface's figure buffer keeps room for.
    ///
    /// <para><see cref="Egress.MostAtOnce"/> and not a number of its own: the room's own law about how many
    /// people may be on their feet at once is the same law as how many slots the buffer needs, and two
    /// opinions about it is the mirrored-constant bug with a body walking through it.</para>
    ///
    /// <para>#973 L2 · …PLUS THE SALESMAN'S OWN SLOT, and he needs one of his own because he is not one of
    /// the room's people. <see cref="Egress.MostAtOnce"/> is a law about how many REGULARS may be crossing
    /// the floor at a time, and on a heaving watch it is satisfied constantly — which, while the band was
    /// exactly that number, meant Harlan Fess could never get on the floor at all. Watched in a browser:
    /// the room's two leavers held both slots for a whole shift and the rep's every attempt was refused.
    /// So the band is the room's law plus his one body; the room's own departures are still capped at
    /// <see cref="Egress.MostAtOnce"/> by <see cref="TheRoomsOwnFeet"/>, which counts the REGULARS on their
    /// feet and not the visitor working them.</para>
    ///
    /// <para>#973 L0 · …and it is <see cref="Egress.BandSlots"/> rather than that sum spelled out again,
    /// because a docked station's bar now asks the same question and got the same answer. The arithmetic is
    /// Core's, once, for the reason it has thrown twice.</para></summary>
    private const int WalkerBand = Egress.BandSlots;

    /// <summary>#973 L2 · How many of the ROOM'S OWN people are on their feet. The salesman is not one of
    /// them: he does not live here, he did not get up from a top, and he is not who
    /// <see cref="Egress.MostAtOnce"/> is a law about. Counted rather than tracked, because the walker list
    /// is the truth about who is afoot and a second tally of it would be a second opinion.</summary>
    private static int TheRoomsOwnFeet(SurfaceExcursion ex)
    {
        int feet = 0;
        foreach (Walker w in ex.Walkers)
        {
            if (w.For is not (Errand.RepRounds or Errand.RepPitching or Errand.RepLeaving))
            {
                feet++;
            }
        }

        return feet;
    }

    /// <summary>#731 · One person on their feet: the walk, and what the walk is FOR.
    ///
    /// <para>The errand is kept here rather than on <see cref="NpcWalk"/> on purpose. Core's walker knows how
    /// to cross a floor and nothing about bars, chairs or quests; what a particular walk means when it ends
    /// is this component's business, and the day a sweep team walks out of an airlock (#731 v2) it will mean
    /// something else again without Core learning a third word.</para></summary>
    public sealed class Walker
    {
        public required NpcWalk Walk { get; init; }

        /// <summary>The top they got up from, the top they are walking TO for an arrival, or — for an
        /// escort — the CABINET top whose door they are about to hold open.</summary>
        public required int Table { get; init; }

        /// <summary>What this walk is for. Three errands, one walker.</summary>
        public required Errand For { get; init; }

        /// <summary>#731 v2 · Which cabinet they are leading you into, as the plate reads — 0 on every other
        /// errand.</summary>
        public int Cabinet { get; init; }

        /// <summary>#731 · WHOSE EVENING THIS IS, in the room's own id — <c>PatronRota.Roster</c>'s shout-name
        /// for a bar regular, and empty for everybody who is not one of a room's own people.
        ///
        /// <para>It is not <see cref="NpcWalk.Plate"/>, and the difference is load-bearing: the plate is what
        /// the deck DRAWS over their head (the short name, "Silas"), and this is what the room FILES them
        /// under. The bar's churn — who has walked out, who has come in and sat down — is keyed on the id,
        /// because that is what the rota, the consoles and the barkeep's own line are all keyed on. One
        /// person filed under two names is this repository's oldest bug class wearing a hat.</para></summary>
        public string Who { get; init; } = "";

        /// <summary>#973 L0 · IS THIS WALK STILL WANTED? Asked again on the frame the route runs out, and
        /// never trusted from the frame it was planned on — a body that crossed a room to a captain who has
        /// stood up in the meantime must not deliver anything. Null on every errand that answers this for
        /// itself (see <see cref="ApproachTheTable"/>).</summary>
        public Func<bool>? StillWanted { get; init; }

        /// <summary>#973 L0 · What happens on the frame they land, and only if <see cref="StillWanted"/> still
        /// says so. The whole of what an APPROACH means is the caller's; this component owns the walking and
        /// knows nothing about what somebody has come to say.</summary>
        public Action? OnArrive { get; init; }

        /// <summary>#731 · WHERE THE GESTURE IS AIMED — the chair they got up from, so the pass is held up to
        /// the room they are leaving rather than at the wall they are leaving through. NaN on every walk with
        /// no gesture in it, which is every walk but one.
        ///
        /// <para>Carried from the frame the walk was PLANNED rather than looked up when the doorstep is
        /// reached, because looking it up needs the whole floor plan and <c>UndergroundComplex.Build</c> lays
        /// a building out from scratch on every call — #731 v1 paid for that lesson once already, with a floor
        /// plan per frame.</para></summary>
        public double PassToX { get; init; } = double.NaN;

        /// <summary>#731 · …and the other half of where the gesture is aimed.</summary>
        public double PassToY { get; init; } = double.NaN;

        /// <summary>#731 · How long they have been holding it up, in seconds of the SAME <c>dt</c> the walk
        /// itself is stepped on. One clock for the walk and for the pause, so no frame can fall between
        /// them.</summary>
        public double PassHeld { get; set; }
    }

    /// <summary>#731 · WHY SOMEBODY IS ON THEIR FEET. Three answers, and they are three different ENDINGS,
    /// which is why the errand is a small closed enum rather than the bool it started as: two of them take
    /// the figure off the floor when the route runs out, and the third is the one where arriving is the
    /// beginning of the beat rather than the end of it.</summary>
    public enum Errand
    {
        /// <summary>Finished, and going — out through a door the captain's own TRY is refused at. The
        /// scheduled ambience and the triggered full stop are both this.</summary>
        Leaving,

        /// <summary>Coming to the captain's table out of one of those doors; #865's <c>TheyCameToYou</c> is
        /// raised on the frame they reach the chair.</summary>
        Arriving,

        /// <summary>#731 v2 · Walking you into a cabinet. <i>"It is dramatic telling when our contact wants
        /// us to follow them into kabinetti."</i> The one errand that does not end when the walk does: she
        /// gets to the door, and then she stands in it and looks back at you across the hall.</summary>
        LeadingYouIn,

        /// <summary>#973 L2 · The Nebula rep drifting between the fixtures of his beat — a standing place
        /// at the counter, the ends of the room's own tops — with nothing to do until somebody sits down
        /// alone. Arriving is not an ending here either: he stands beside the thing he walked to.</summary>
        RepRounds,

        /// <summary>#973 L2 · The rep crossing to a captain sitting alone. He STANDS at the table — he is
        /// not invited, and there is no eighth way to open a sitting in this codebase — and the pitch card
        /// goes up on the frame he lands on.</summary>
        RepPitching,

        /// <summary>#1061 · …AND THE SHIFT ENDING. The room is worked, and the salesman goes out through a
        /// leaf the captain's own TRY is refused at, exactly like a regular who has finished a drink. It is
        /// his own errand and not <see cref="Leaving"/> for one reason: he is not one of the room's people,
        /// so he must not eat a slot of <see cref="Egress.MostAtOnce"/> on his way out of a room he does not
        /// live in.</summary>
        RepLeaving,

        /// <summary>
        /// #731 · <b>THE EXIT THAT IS A GESTURE.</b> The issue's second customer, in its own words: <i>"the
        /// agency temp leaving at watch change through the staff door, showing the pass nobody inside asks
        /// for."</i>
        ///
        /// <para>The same walk as <see cref="Leaving"/>, to the same kind of leaf, on the same legs — and then
        /// one thing more. At the doorstep they stop, turn back to the room they are leaving, and hold the
        /// pass up to it for <see cref="CanteenRegulars.PassHeldSeconds"/>. <b>Nobody looks.</b> No console
        /// appears, nothing is pulsed, no card is raised, nobody's facing changes, and the leaf refuses the
        /// captain exactly as it did before — the gesture happens, and the room's answer to it is the whole
        /// beat. It is an errand of its own rather than a flag on <see cref="Leaving"/> because it is a
        /// different ENDING, which is what this enum is for.</para></summary>
        ShowingThePass,

        /// <summary>#973 L0 · SOMEBODY CROSSING A DOCKED STATION'S BAR TO YOUR TABLE. The sixth errand, and
        /// the first that belongs to a room the Hive did not build — see <see cref="ApproachTheTable"/>. Like
        /// the escort's and the rep's, arriving is not an ending: they stand at the top and look at you until
        /// whatever brought them there is over.</summary>
        Approaching,

        /// <summary>#1061 beat 2 · BREM KOLT STANDING ABOUT ON THE REGOLITH, waiting for the captain to come
        /// out of their own airlock. The rep's <see cref="RepRounds"/> shape on open ground: arriving is not
        /// an ending, he is simply THERE until something moves him — see <c>Map.Hardcase.cs</c>.</summary>
        HardcaseWaiting,

        /// <summary>#1061 beat 2 · …and crossing the ground to the captain with the card. He does not sit
        /// down and he is not invited: there are no chairs on a moon.</summary>
        HardcasePitching,

        /// <summary>
        /// #1061 beat 2 · <b>HE HAS SEEN ONE, AND HE IS RUNNING.</b> The one errand in this enum whose whole
        /// content is the walking — no card, no pulse, no plate, nothing said at the far end of it. The route
        /// running out is the end of him for this excursion, exactly as a leaf clicking is the end of a
        /// regular who has finished a drink, and the game says nothing about either.
        ///
        /// <para>It is its own errand and not <see cref="Leaving"/> because there is no DOOR: a moon has no
        /// leaf that refuses the captain, so what ends this walk is the far edge of the ground and the fact
        /// that he is no longer between the captain and it.</para></summary>
        HardcaseFleeing,
    }

    /// <summary>#731 · Every walker's slot is off-map when nobody is in it — the same idiom an unseen guard
    /// and a behind-cover Old One already use, so the buffer is always fully written and the renderer never
    /// has to know how many people are afoot.</summary>
    /// <param name="afoot">Whose feet these are — the excursion's on a Hive floor, the docked bar's ashore
    /// (#973 L0). Handed in rather than read off <c>_surface</c>, because there are two rooms with a
    /// metabolism now and a filler that reached for one of them would draw an empty bar.</param>
    private void FillWalkerDroids(DeckPlan.Droid[] buffer, int firstSlot, IReadOnlyList<Walker> afoot)
    {
        for (int i = 0; i < WalkerBand; i++)
        {
            int slot = firstSlot + i;
            if (slot >= buffer.Length)
            {
                return;
            }
            // The EXISTING NPC pen and no new one: a plate that is not a guard's, a sweeper's or an Old
            // One's falls through DrawTheFigures to the ordinary grey, which is exactly right — the person
            // crossing the hall is one of the people who were sitting in it a minute ago.
            buffer[slot] = i < afoot.Count
                ? new DeckPlan.Droid(afoot[i].Walk.X, afoot[i].Walk.Y, afoot[i].Walk.Facing, afoot[i].Walk.Plate)
                : new DeckPlan.Droid(-9999, -9999, 0, WalkerSlotName(i));
        }
    }

    /// <summary>What an EMPTY walker slot is called. Stable per slot so the buffer's shape does not change
    /// with how many people happen to be walking — the fingerprint tests read this buffer.</summary>
    private static string WalkerSlotName(int index) => $"WALKER {index + 1}";
}

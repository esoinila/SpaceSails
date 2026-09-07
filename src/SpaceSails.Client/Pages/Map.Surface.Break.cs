using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #719 slice 2's maintenance break.
public partial class Map
{
    // ── #719 slice 2 · ONE RADIO CALL, AND THE CAR IS SOMEBODY ELSE'S AGAIN ─────────────────────────────
    //
    // Owner, 2026-08-05: "what if the elevator needs maintenance break :-)" / "just stopping the elevator by
    // remote of radio message would stop all escape way too easy :-d" / "going up would use more air".
    //
    // FOUR THINGS ABOUT WHAT IS HERE, and all four are about how little of it is new:
    //
    //   1. NOTHING ROLLS. The break is the challenge's own outcome arriving at the panel — the wallet that
    //      could not answer (#804/#836) and the run (#835), which are the two ways the round goes against a
    //      captain, plus #602's third miss, which summons the same man to the same read and therefore comes
    //      through one of those two. No new security kind, no alert state, no Provocation member: #618 still
    //      owes the owner a ruling on what a second security body would be and this lane leaves it owing.
    //   2. NOTHING IS SAID. No pulse, no banner, no line in the book, and nothing anywhere names who called
    //      it. The captain walks back to the car and the floor list is gone — that is #603's inference
    //      horror, and the plate is the whole of the telling (§13.8, #761: the moment is told where the
    //      captain is looking, and the panel is exactly what they are looking at).
    //   3. IT MAY ONLY HAPPEN WHERE THE STAIR IS. UndergroundComplex.ACallCanStopTheCarOn is HasStairOn with
    //      the hulls taken out, so the floors a break may land on and the floors with a second way out cut in
    //      them are ONE list. #719's ordering law stops being a promise and becomes a condition.
    //   4. THE ESCAPE IS THE STAIR AND IT IS UNTOUCHED. Its doors were already one-way — into the shaft on
    //      every listed floor, out only at the top — so nothing here has to be added to make the break
    //      survivable, and the refuges are still on the way (#608). What changes is the PRICE: a free ride
    //      becomes a climb the tank pays for, which is the owner's "going up would use more air" arriving as
    //      the consequence of a thing the captain did.

    /// <summary>#719 slice 2 · Is the car stopped under this captain right now? Asked by the panel, the pad,
    /// the fan and the readout, so that four instruments cannot come to four opinions about one machine.
    /// False on the ship and on the surface, where there is nothing to stop.</summary>
    private bool TheCarIsStopped => _surface is { CarStopped: true, Floor: < 0 };

    /// <summary>
    /// #719 slice 2 · <b>SOMEBODY SAYS THE FLOOR INTO A RADIO.</b> Called from the two places the challenge
    /// goes against the captain — the read that is not satisfied, and the one gate every run comes through —
    /// and from nowhere else, so a third trigger invented tomorrow has to arrive here to exist.
    ///
    /// <para>Core decides whether this floor may have it at all
    /// (<see cref="UndergroundComplex.ACallCanStopTheCarOn"/>), which is where the surface, the wrecks and
    /// the floors below the listed bottom are refused. Idempotent by being a flag: a captain read and
    /// refused three times has had one maintenance break, because there is one car.</para>
    ///
    /// <para><b>Static, and that is what makes it one rule.</b> The round is a nested class holding the page
    /// through <c>IPatrolHost</c> — an interface with a ratchet on its size (<c>ThePatrolKeepsItsOwnStateTests</c>)
    /// — so a break the patrol asked the page for would have cost a member on it, and a break the patrol
    /// worked out for itself would be a second author of the one condition the ordering law rests on. A
    /// private static of the enclosing type is reachable from inside the family and from nowhere outside it,
    /// which is exactly the shape this wants.</para>
    /// </summary>
    private static void TheCarIsStoppedForMaintenance(SurfaceExcursion ex)
    {
        if (ex.Floor >= 0 || !UndergroundComplex.ACallCanStopTheCarOn(ex.Stop.Body.Id, ex.Floor))
        {
            return;
        }
        ex.CarStopped = true;
    }

    /// <summary>
    /// #719 slice 2 · <b>THE WAY HOME, WITH THE CAR GONE.</b> The walk to the stair's door and then the
    /// climb, in the deck units <see cref="SuitAir"/> prices everything in — Core's own arithmetic
    /// (<see cref="UndergroundComplex.WayOutByStairDu"/>), which is #1115's <c>ClimbAirSeconds</c> read
    /// backwards rather than a second opinion about what a flight of stairs costs.
    ///
    /// <para>This is the half of the feature that turns <i>you lose</i> into <i>you pay</i>. Every threshold
    /// the suit already owns is written against one number — the crossing line, the reserve, the low-air
    /// card, the on-grid countdown — so moving that number is the whole of how the tank learns that the way
    /// out got expensive. Nothing new warns anybody; the instruments that were already watching simply start
    /// telling the truth about a longer journey.</para>
    /// </summary>
    private double TheClimbHomeDu(SurfaceExcursion ex) =>
        UndergroundComplex.WayOutByStairDu(
            MoonSurface.ExpeditionField(), ex.Floor, _avatarX, _avatarY);
}

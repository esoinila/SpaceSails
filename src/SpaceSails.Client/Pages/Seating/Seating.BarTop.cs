using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #973 L5b · <b>THE EIGHTH SEAT: THE CAPTAIN CAN SIT DOWN IN A DOCKED BAR.</b>
///
/// <para><b>The gap, in #973 L0's own words:</b> <i>"There is no way to sit down in a docked bar. Every seat
/// in this game is opened through <c>Seating.TakeThisSeat</c>, all seven sites of it are gated on a
/// <c>SurfaceExcursion</c>, and a docked berth has none — the bar's seven tops are drawn dressing with no
/// chairs and no console."</i> The owner's favourite room in this game is The Red Eye's bar, and until this
/// file the one thing a captain could not do in it was pull out a chair.</para>
///
/// <para>It cost the room a console and this family one verb. <b>Not a second kind of sitting</b>: the record
/// built below is the same <c>TableTalk</c> the canteen's own free top builds, through the same one method,
/// so the strip, the exposure ladder, the spread's privacy gate, the sit beat, the stand-up confirm and the
/// step-off square are all the shipped ones and not one of them had to learn about a berth. The law in
/// <c>EverySeatTheCaptainTakesFingerprintsTheSameTests.ThereIsOnePlaceASittingIsOpened</c> moves <b>7 → 8</b>
/// and says so.</para>
///
/// <h3>What is different about a berth, and it is exactly three things</h3>
///
/// <para><b>There is no excursion.</b> Every other verb in this family opens on <c>_host.Surface</c>; this one
/// opens on <c>_host.TheBarTopUnderfoot()</c>, the one member #973 L5b added to <see cref="ISeatHost"/>. What
/// an excursion was giving the other seven — which top, keyed how, on which watch, and where a body sits at
/// it — is what that answer carries, and nothing more.</para>
///
/// <para><b>There is nobody to ask.</b> A bar top with somebody at it wears their own <c>BarPatron</c> console
/// (the rota's, the Magpie's, the oracle's) and the room does not put a takeable top under one, so this verb
/// only ever meets an EMPTY table. That is why there is no join-an-occupied-top counterpart here and no
/// <c>Who</c> but <c>None</c>: #746's press is a question you ask a person, and the person in this room is
/// already a different console.</para>
///
/// <para><b>And it is the whole point of the lane.</b> Sitting down alone at a top is a choice to be
/// FINDABLE (#757's own words), and in a classy room it is what a walk-in is looking for:
/// <c>TheCaptainIsSittingAloneInTheBar</c> — the predicate #973 L0 shipped answering <i>false at every berth
/// in the game</i> — starts answering true on the frame this method returns.</para>
/// </summary>
public partial class Map
{
    private sealed partial class Seating
    {
        /// <summary>
        /// #973 L5b · TAKE A TOP IN A DOCKED STATION'S BAR.
        ///
        /// <para>#757's verb, one room over, and deliberately built out of its parts: the same register
        /// decision (<see cref="SittingAlone.SitReadsAsRelaxed"/> over the pour and the watch), the same scene
        /// (<see cref="SittingAlone.TheTable"/>), the same snap (<c>_host.SitCaptainOn</c>), the same
        /// chair arithmetic, the same opening line off the scene rather than pinned here.</para>
        ///
        /// <para>THE PRESS IS CONSUMED when the captain is already sitting, exactly as it is at a canteen top:
        /// [E] is not how you stand up — "take your leave" is — and re-opening would wipe the outcome line
        /// somebody is in the middle of reading.</para>
        /// </summary>
        /// <returns>Whether this press was a bar top's. False means it was not one, and the dispatch does what
        /// it does with any press that is not this verb's: nothing.</returns>
        public bool TryTakeBarTop()
        {
            if (_host.TheBarTopUnderfoot() is not { } top)
            {
                return false;
            }

            if (Table is not null)
            {
                return true;
            }

            // #783 · WHICH REGISTER THIS SIT IS IN, decided once, by Core, off the room and the glass — and
            // the counter's pour is asked through the one member the rest engine runs on, so the panel cannot
            // say "cold glass" on a beat the short rest had already called dry.
            bool drink = _host.APourInFrontOfYou;
            bool relaxed = SittingAlone.SitReadsAsRelaxed(drink, top.Watch);
            Encounter.Scene sat = SittingAlone.TheTable(relaxed, drink);

            // #820 · the snap, at the room's own published place beside this top. Never measured here.
            _host.SitCaptainOn(top.ChairX, top.ChairY);

            // #870 lane 6d · THE EIGHTH CONSTRUCTION SITE, AND IT GOES THROUGH THE ONE METHOD LIKE THE OTHER
            // SEVEN. The reveal cue and the draw live in `TakeThisSeat` and nowhere else, so the record is
            // built in the argument — a sitting assembled into a local and handed over afterwards is exactly
            // what `ThereIsOnePlaceASittingIsOpened` exists to refuse.
            TakeThisSeat(new TableTalk
            {
                Key = top.Key,
                Index = top.Index,
                // A bar top is drawn and does not collide, so the seat is floor and nothing has to be stepped
                // off — the square is carried all the same, because it is StandCaptainAt that gets the
                // nudge's opinion on whether the room agrees.
                StepOff = (top.ChairX, top.ChairY),
                Who = CanteenTable.Who.None,
                Plate = SittingAlone.OwnTablePlate,
                Scene = sat,
                Seats = top.Seats,
                // One of them is yours now. The room can see you sitting alone, which is the whole premise —
                // and in a classy room it is what somebody crossing the floor is looking for.
                Free = System.Math.Max(0, top.Seats - 1),
                // A station bar is one loud room with a window in it: no cabinets, no curtains, nothing to
                // dog. Both flags are the room's honest answer and not a default nobody thought about.
                Quiet = false,
                Cabinet = 0,
                Solo = true,
                Relaxed = relaxed,
                DrinkInHand = drink,
                // Nobody to ask. #746's ask-to-join beat is the answer to a person, and the room does not put
                // a takeable top under one — the table is simply taken, and the taking is the opening line.
                Joined = true,
                Outcome = sat.Opening,
            });
            return true;
        }
    }
}

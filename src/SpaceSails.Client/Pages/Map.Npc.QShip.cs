using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of Map.Npc — what the instruments measure of a hull beside what her papers claim, and the
// one thing a hull that is not a merchant does when the captain closes on her.

public sealed partial class Map
{
    /// <summary>
    /// #534 slice 1 · <b>THE SAME FIELDS EVERY HAULER SHOWS, WITH DIFFERENT NUMBERS IN THEM.</b>
    ///
    /// <para>Three readings, each a measurement beside the figure her own papers imply, and <b>no verdict
    /// anywhere</b>. Every hull in the sky carries this block; on almost every one of them the two columns
    /// agree, and the captain does the arithmetic or does not. That is #534's whole mechanic and the reason
    /// nothing here is a flag, a colour or a plate: <i>"the scope reports a burn, the telescope reports a
    /// radiator, and the captain does the arithmetic or does not."</i></para>
    ///
    /// <para><see cref="FixHeld"/> is the scan gate. The burn is read off her observed motion and needs
    /// nothing but a contact; the radiator count and the comms fit are things the GLASS resolves, so they
    /// arrive only once the telescope actually holds a fix on her — the same "a tell may need a completed
    /// pass" idiom #1121's reveal already runs on. The card simply does not draw the two rows it has not
    /// earned, rather than drawing them as blanks, because a row that appears empty is itself a tell about
    /// which rows matter.</para>
    /// </summary>
    public readonly record struct HullReading(
        double MeasuredTrimAccelMps2,
        double ClaimedTrimAccelMps2,
        int MeasuredRadiatorPanels,
        int ClaimedRadiatorPanels,
        int MeasuredGuardedChannels,
        int ClaimedGuardedChannels,
        bool FixHeld);

    /// <summary>Every number the instruments have of this hull, straight out of Core. The page contributes
    /// exactly one fact Core cannot know — whether the telescope is holding her.</summary>
    private static HullReading ReadingOf(NpcShip ship, bool fixHeld) => new(
        QShip.MeasuredTrimAccelMps2(ship), QShip.ClaimedTrimAccelMps2(ship),
        QShip.MeasuredRadiatorPanels(ship), QShip.ClaimedRadiatorPanels(ship),
        QShip.MeasuredGuardedChannels(ship), QShip.ClaimedGuardedChannels(ship),
        fixHeld);

    /// <summary>
    /// #534 · <b>SHE DOES NOT RUN THE WAY PREY RUNS.</b> The one live branch this slice adds to traffic:
    /// when the captain has closed to inside the boarding envelope, a hull that is not a merchant breaks —
    /// once — along <see cref="QShip.EvadeHeadingRad"/>, which opens the range without letting the captain
    /// off her bow.
    ///
    /// <para><b>Honest traffic is untouched.</b> No merchant in this game has ever reacted to the player and
    /// none starts now: the branch is gated on <see cref="QShip.IsMasked"/>, so the sky a captain has flown
    /// in for a hundred passes is bit-for-bit the sky he flew in before, and the ONE hull that behaves
    /// differently is the one the tells were about.</para>
    ///
    /// <para><b>A holed sail still ends it</b>, exactly as it ends the tutorial's escape jink: a disabled
    /// hull is stepped with no plan at all, and this never gives her one to fly. The gun is what makes an
    /// evasive ship catchable, and that stays true of her too.</para>
    ///
    /// <para>Once only, tracked on the NPC's own state rather than on a page field — a hull that re-broke
    /// every frame the captain held the window would be a thruster, not a captain.</para>
    /// </summary>
    private void LetTheMaskedHullsRun()
    {
        foreach (NpcState npc in _npcStates)
        {
            if (npc.Broke || npc.Disabled || npc.Boarded || !npc.Active || npc.Arrived)
            {
                continue;
            }

            if (!QShip.IsMasked(npc.Ship) || !CaptureRule.IsInWindow(_ship, npc.State))
            {
                continue;
            }

            npc.Broke = true;
            npc.Ship = npc.Ship with
            {
                Plan = new ManeuverPlan(
                [
                    .. npc.Ship.Plan.Nodes,
                    QShip.EvadeBurn(npc.Ship, npc.State, _ship.Position, npc.State.SimTime),
                ]),
            };
        }
    }
}

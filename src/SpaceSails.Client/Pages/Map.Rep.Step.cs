using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// STEPPING HIM — and #973 L2's rule that his errand does not end the way the others do.
///
/// <para>A haulier's walk ends with her in a chair and a sweeper's with him through the lock; Fess
/// arrives and then STAYS, beside a fixture, with nothing to do but be seen working.</para>
///
/// <para>Split out of <c>Map.Rep.cs</c> under #251 with no member renamed, re-scoped or re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    // ── Stepping him, and the two errands whose arrival is not an ending ────────────────────────────────

    /// <summary>
    /// #973 L2 · HIS TWO ERRANDS BOTH END STANDING UP. A haulier's walk ends with her in a chair and a
    /// sweeper's with him through the lock; Fess arrives and then STAYS — beside a fixture with nothing to
    /// do, or at your elbow with a card. Called from <c>AdvanceWalkers</c>, which owns the clock.
    /// </summary>
    /// <returns>Whether anything happened that the page should redraw for.</returns>
    /// <param name="afoot">The room's own feet — the excursion's underground, the docked bar's ashore
    /// (#973 L0). One stepper, because he is the same man in both rooms.</param>
    private bool StepTheRep(
        IList<Walker> afoot, Walker who, double dt, IReadOnlyList<SurfaceCollision.Segment> walls, int slot)
    {
        // #1061 · …AND ONE THAT ENDS THE ORDINARY WAY. His shift is over and he is walking out through a leaf
        // that does not open for the captain: the route running out is the end of him, exactly as it is for a
        // regular who has finished a drink, and nothing is said about it.
        if (who.For == Errand.RepLeaving)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            afoot.RemoveAt(slot);
            _repStandingAt = null;
            return true;
        }

        if (who.Walk.State != NpcWalk.Doing.Arrived)
        {
            who.Walk.Step(dt, walls, _avatarX, _avatarY);
            if (who.Walk.Afoot)
            {
                return false;
            }

            if (who.Walk.State != NpcWalk.Doing.Arrived)
            {
                // The floor refused him somewhere between the door and the table. He is standing wherever it
                // stopped him, which is honest — and the mark he could not reach is one he does not queue for.
                afoot.RemoveAt(slot);
                HeIsStandingHere(who);
                if (who.For == Errand.RepRounds && who.Table >= 0)
                {
                    _repMarksWorked++;
                }

                _repMoveOnAt = SimTime + RepDwellSeconds;
                return true;
            }

            // The frame he lands on is the frame he speaks on, and only that frame.
            if (who.For == Errand.RepPitching)
            {
                HeReachesYourTable();
            }
            else
            {
                // #1061 · …and at somebody ELSE'S table he says nothing at all. The pause is the patter, and
                // its length is the round's own — a fact about this watch and never a clock reading.
                _repMoveOnAt = SimTime + HisBeatAt(who.Table);
            }

            who.Walk.LookTowards(_avatarX, _avatarY);
            return true;
        }

        // Standing. A man on his rounds moves on when he has stood long enough; a man mid-pitch waits for
        // an answer however long that takes, because the answer is the whole of the scene.
        if (who.For == Errand.RepRounds && SimTime >= _repMoveOnAt)
        {
            afoot.RemoveAt(slot);

            // #1061 · A pause at the table, and then on — FROM HERE. He is taken off the list and put back on
            // it in the same frame by the planner, so the room never draws him vanishing off a top and coming
            // back out of a cellar.
            HeIsStandingHere(who);
            if (who.Table >= 0)
            {
                _repMarksWorked++;
            }

            return true;
        }

        who.Walk.LookTowards(_avatarX, _avatarY);
        return false;
    }

    /// <summary>#1061 · Remember where his feet are, so the next leg of his round begins at them.</summary>
    private void HeIsStandingHere(Walker who) =>
        _repStandingAt = new DeckReachability.Point(who.Walk.X, who.Walk.Y);
}

using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: part of the patrol (#870 lane 6′c; the header note lives in Map.Patrol.cs) — #602's one new caller:
// a building that has called its own security to a keypad, and the man on the rota who walks over to it.
public sealed partial class Map
{
    private sealed partial class Patrol
    {
        /// <summary>
        /// #602 · <b>THE PAD CALLED SECURITY, AND SOMEBODY ON THE ROTA IS SENT TO IT.</b>
        ///
        /// <para>Owner's ruling: three wrong entries inside the window and <i>"the security patrol comes"</i>.
        /// This is the whole of the summons, and the load-bearing fact about it is <b>how little of it is
        /// new</b>. There is no new kind of security, no sweep team, no alert state, no floor-wide hunt and
        /// no <see cref="PatrolBeat.Provocation"/> member — #618 still owes the owner the ruling on what a
        /// second security body would even be, and this lane leaves that owing rather than spending it.</para>
        ///
        /// <para><b>It is #618's own walk to a place, pointed at a keypad.</b> A man leaves his round, crosses
        /// the floor to the spot, and what happens when he gets there is decided by the identical rule that
        /// decides it for a gunshot: <see cref="TheNoiseTurnsIntoAPerson"/> asks his own eye
        /// (<see cref="PatrolBeat.Notices"/>), and a captain still standing at the pad is hailed and read
        /// exactly as a captain standing anywhere else is — the GENERAL HANDS challenge (#804/#833/#836),
        /// a pass that works and a pass that does not, and the challenge's own outcomes. A captain who
        /// walked away gets <see cref="TheNoiseWasNothing"/>: a man looks at a keypad, and men on rotas do
        /// not narrate that.</para>
        ///
        /// <para><b>NO EARSHOT AND NO EYE ON THE WAY IN,</b> and that is the one thing that differs from a
        /// bang. A gunshot is heard, so the nearest EAR answers and only if it is close enough
        /// (<c>GunfireHeard.NearestEar</c>). A pad does not make a noise — it makes a CALL, on the
        /// building's own wiring, to whoever is on the floor. So the nearest man answers it whatever the
        /// distance, because he was told rather than because he heard. If nobody is on the rota down here
        /// nobody comes, and nothing anywhere says so: that is #618's rule three, unchanged, and it is the
        /// register (§13.8) — no line explains the patrol, on either end of it.</para>
        ///
        /// <para><b>NOTHING IS SAID AND NOTHING IS BANKED.</b> No pulse, no banner, no heat crossing. The
        /// captain has already been told, once, by the sticker on the wall before the first press, and the
        /// pad has already said <c>SECURITY CALLED</c>. A second sentence here would be the building
        /// explaining its own consequence to the person it is happening to.</para>
        /// </summary>
        /// <param name="x">Where the pad is — the console the captain is standing at.</param>
        /// <param name="y">The same.</param>
        /// <returns>Whether anybody was actually sent. False is an empty rota or a floor already busy with
        /// one of these, and the caller does not narrate either.</returns>
        public bool SecurityWasCalledTo(double x, double y)
        {
            if (Guards.Count == 0 || _host.Surface is not { Floor: < 0 })
            {
                return false;
            }

            // Not while somebody is already walking you out, already coming, or already crossing the floor:
            // two men doing one job is #777's stacked card with legs, and it is the identical gate
            // TheRoundHearsAShot keeps.
            if (Escort is not null || EscortDue is not null || KickOutRideDue || LookingIntoIt is not null)
            {
                return false;
            }
            foreach (Guard man in Guards)
            {
                if (man.AfterYou || man.WalkingUp)
                {
                    return false;
                }
            }

            int who = -1;
            double nearest = double.MaxValue;
            for (int i = 0; i < Guards.Count; i++)
            {
                double dx = Guards[i].X - x;
                double dy = Guards[i].Y - y;
                double d2 = (dx * dx) + (dy * dy);
                if (d2 < nearest)
                {
                    nearest = d2;
                    who = i;
                }
            }

            Guard g = Guards[who];
            LookingIntoIt = g;
            TheNoise = (x, y);

            // #833's own transition, unchanged and unwrapped — the same one a bang gets. The round is
            // suspended and resumes from wherever the walk leaves him, which is what a detour is.
            g.HeStartsWalkingUp();
            g.Vx = 0;
            g.Vy = 0;
            g.Facing = System.Math.Atan2(y - g.Y, x - g.X);
            return true;
        }
    }
}

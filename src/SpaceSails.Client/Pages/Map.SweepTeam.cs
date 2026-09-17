using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #538 slice 3 · SOMEBODY ELSE'S TEAM, ON THE DECK. The brain is <see cref="InspectionTeam"/> in Core and every
/// number below comes from it; this file is the walking, the looking and the telling.
///
/// <para><b>Where they land.</b> On the INSURANCE JOB, whose own fiction already says
/// <i>"she was LOST ON PURPOSE… the most valuable thing aboard is the evidence"</i>. That is the perfect host and
/// it needed nothing invented: they are here to remove exactly what the captain came to take, which is the whole of
/// the owner's <i>"they want to keep their secrets, but the rewards could be big also"</i> in one existing cause.
/// Also reachable on any hull with <c>?sweep=N</c>, the way <c>?reevers=N</c> works.</para>
///
/// <para><b>What they do that the pack cannot.</b> They walk a route, they look where they are going, and they
/// hear better than anything else aboard. So the captain's counter-play is not the maze and not speed — it is
/// standing still off their axis and not making a sound, which is the one thing the wreck lane has never asked
/// for.</para>
///
/// <para>#251 · THIS FILE IS THE TEAM ITSELF — one sweeper's body, the three of them aboard, the
/// route they share, and the one HUD line the ship gets out of them. Also what the rest of the deck
/// sees of them: the droid buffer the renderer fills and the rows the motion tracker reads.</para>
///
/// <para>The rest of the family is three siblings, each named for the thing it does: <c>.Loop</c>
/// (the once-a-frame advance, the hatch they work and the filing out through it), <c>.Walk</c> (the
/// route, the wall law and the body's step), and <c>.Pack</c> (holding and fighting an Old One, and
/// the noise that costs).</para>
///
/// <para><b>Every field stays here, in the order the one file declared it</b> — including
/// <c>_sweepFireSeconds</c>, which the pack fight is the only reader of. The family declares no
/// static field at all; the four <c>static</c> members that moved are METHODS. Only method bodies
/// moved, and no member is renamed, re-scoped or re-ordered.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>One sweeper, walking. Mutable and client-side for the same reason the Reevers are: the rules are
    /// pure in Core and the list is the client's business.</summary>
    public sealed class Sweeper
    {
        public required string Callsign { get; init; }

        public double X;
        public double Y;
        public double Facing;

        /// <summary>How fast they are travelling this frame. A MOTION tracker hears travel and nothing else, so
        /// without this the fan would report an empty hull while three people walked it — the panel disagreeing
        /// with the sim, which is the one thing this codebase does not allow.</summary>
        public double Vx;
        public double Vy;

        public InspectionTeam.Awareness State = InspectionTeam.Awareness.Sweeping;
        public double StateSeconds;

        /// <summary>Where they are walking to — a route waypoint, or the place a noise came from.</summary>
        public double GoalX;
        public double GoalY;

        /// <summary>Which leg of the patrol they are on. They resume it after a search, which is what makes a
        /// diversion a diversion rather than a permanent change of plan.</summary>
        public int RouteLeg;

        /// <summary>Where they last laid eyes on the captain — the only thing they know once sight is broken.
        /// Same idiom as the pack: a PLACE, never a target.</summary>
        public double LastSeenX;
        public double LastSeenY;

        /// <summary>Whether they have said their line for the state they are in. Keeps a challenge from
        /// re-announcing itself sixty times a second.</summary>
        public bool Announced;

        /// <summary>#731 v2 · Which leg of the route they STARTED on. The three of them are staggered a third
        /// of the way round the hull from each other, so "back where I began" is the only spelling of
        /// <i>a lap</i> that means the same thing for all three.</summary>
        public int StartLeg;

        /// <summary>#731 v2 · How many times round the hull they have been. At
        /// <see cref="InspectionTeam.LapsBeforeTheyGo"/> they go home.</summary>
        public int Laps;

        /// <summary>#731 v2 · Their walk to the lock, planned over the captain's own lattice by the same
        /// <c>NpcWalk</c> the canteen's people are walked with (#731). Null on every other state — a body
        /// sweeping a hull is following a checklist, and a body going home is following a route.</summary>
        public NpcWalk? Walk;

        /// <summary>#731 v2 · How long they have been standing at the head of the file working the hatch.
        /// At <see cref="InspectionTeam.ThroughTheLockSeconds"/> they are through it and gone.</summary>
        public double AtTheLock;
    }

    private readonly List<Sweeper> _sweepers = [];

    /// <summary>The patrol: compartment centres, aft to forward. Built once at spawn so all three share one route
    /// and a captain can learn it — being hidden from has to be legible.</summary>
    private readonly List<(double X, double Y)> _sweepRoute = [];

    /// <summary>Dev cheat: <c>?sweep=N</c> puts N sweepers aboard whatever hull you board.</summary>
    private int _sweepTeamCheat;

    private bool SweepersAboard => _sweepers.Count > 0;

    /// <summary>Whether any of them is actively after the captain — read by the story seam so a card cannot land
    /// over a challenge, and by the HUD.</summary>
    private bool AnySweeperOnTheCaptain =>
        _sweepers.Exists(s => s.State is InspectionTeam.Awareness.Challenging or InspectionTeam.Awareness.Hunting);

    /// <summary>The worst state anybody aboard is in, for the one HUD line they get.</summary>
    private InspectionTeam.Awareness WorstSweeperState
    {
        get
        {
            InspectionTeam.Awareness worst = InspectionTeam.Awareness.Sweeping;
            foreach (Sweeper s in _sweepers)
            {
                if (InspectionTeam.NervePerSecond(s.State) > InspectionTeam.NervePerSecond(worst))
                {
                    worst = s.State;
                }
            }
            return worst;
        }
    }

    // ── Putting them aboard ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Their boat mates on and they start work. Placed at the AFT end and routed forward, so they come up the
    /// spine toward the lock — which puts them between the captain and the way out by the time they matter, the
    /// same geometry that makes the pack frightening.
    /// </summary>
    private void SpawnSweepTeam(int count)
    {
        _sweepers.Clear();
        _sweepRoute.Clear();

        // The route: every compartment centre, aft to forward, then the spine at the lock. They work the ship the
        // way anybody would who has done it before — from the far end back to their own boat.
        foreach ((string name, float x0, float x1, bool _) in WreckLayout.Compartments.OrderBy(c => c.Item2))
        {
            _ = x1;
            _ = name;
            _sweepRoute.Add(RoomCentre(name));
        }
        _sweepRoute.Add((WreckLayout.ShuttleLockX, 0));

        for (int i = 0; i < count && i < InspectionTeam.Callsigns.Count; i++)
        {
            // Staggered along the aft spine, each starting a third of the way round the route, so the three of
            // them cover the hull instead of walking in a queue.
            int leg = _sweepRoute.Count > 0 ? i * _sweepRoute.Count / System.Math.Max(1, count) : 0;
            _sweepers.Add(new Sweeper
            {
                Callsign = InspectionTeam.Callsigns[i],
                X = -30 + (i * 3),
                Y = 0,
                Facing = 0,
                RouteLeg = leg,
                StartLeg = leg,
                GoalX = _sweepRoute.Count > 0 ? _sweepRoute[leg].X : 0,
                GoalY = _sweepRoute.Count > 0 ? _sweepRoute[leg].Y : 0,
            });
        }

        ShowPulseMessage(InspectionTeam.TheyArriveLine);
        LogAutopilotEvent(InspectionTeam.TheyArriveLine);
        RendererInterop.PlayCue("alarm");

        // Said once, on the way in, because a captain who has not silenced their own kit has already lost and
        // deserves to be told why rather than discovering it when the tube gun opens up.
        if (!_weaponsTight)
        {
            LogAutopilotEvent(InspectionTeam.YourGunsWillGiveYouAwayLine);
            ShowPulseMessage(InspectionTeam.YourGunsWillGiveYouAwayLine);
        }

        ApplyNerveShock(NervePips.SightingPips * (int)NervePips.PipUnit, "somebody else is aboard");
        _wreckTrackerLive = true;   // an ear does not un-hear: three moving contacts light the fan
    }

    private double _sweepFireSeconds;

    /// <summary>Fill the sweepers into the droid buffer. Named by callsign, which is how the renderer knows to
    /// draw them cold rather than red — and how a captain reading the deck knows which one is which.</summary>
    private void FillSweeperDroids(DeckPlan.Droid[] buffer, int firstSlot)
    {
        for (int i = 0; i < InspectionTeam.TeamSize; i++)
        {
            int slot = firstSlot + i;
            if (slot >= buffer.Length)
            {
                return;
            }

            // #804 · …and NEVER on a floor of the Hive. This band used to fall off the end of the Hive
            // deck's droid count, which parked the sweepers by accident; the count is the whole buffer now
            // (the rounds are the last band), so the accident has to become a rule. A black-ops team hired
            // to strip a derelict is not walking a clandestine basement, and drawing them on one would be
            // the picture claiming something the sim never said.
            buffer[slot] = i < _sweepers.Count && _surface is not { Floor: < 0 }
                ? new DeckPlan.Droid(_sweepers[i].X, _sweepers[i].Y, _sweepers[i].Facing, _sweepers[i].Callsign)
                : new DeckPlan.Droid(-9999, -9999, 0, InspectionTeam.Callsigns[i]);
        }
    }

    /// <summary>
    /// EVERYTHING ABOARD THAT MOVES, for the motion fan. Added because the first playtest of this scene showed
    /// the fan reading "no movement — for now" while three professionals walked the length of her: the tracker
    /// read the pack list directly, and the pack was not who was aboard. One accessor now, so the ear and the
    /// hull can never disagree again about who is walking about in it.
    /// </summary>
    private IEnumerable<MotionTracker.Entity> EverythingThatMoves()
    {
        foreach (Reever r in _reevers)
        {
            yield return new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy);
        }
        foreach (Sweeper s in _sweepers)
        {
            yield return new MotionTracker.Entity(s.X, s.Y, s.Vx, s.Vy);
        }

        // #583 / #633 · AND THE REPO CREW, who were on the other branch's fan and would otherwise have
        // fallen off this one. They WALK, and a motion-only ear hears walking louder than anything else on
        // this ground — which is the whole warning the player gets that the boat that came down is now
        // spread out and coming. Same instrument, no special case: they are contacts. This accessor exists
        // precisely so the ear and the hull can never disagree about who is walking about, so every kind of
        // figure that moves has to be listed HERE and nowhere else.
        foreach (Collector c in _collectors)
        {
            yield return new MotionTracker.Entity(c.X, c.Y, c.Vx, c.Vy);
        }

        // #804 · AND THE ROUNDS, WHICH ARE THE FIRST THING THE FAN HAS EVER HAD TO HEAR UNDERGROUND. Owner:
        // "We need our motion detector to warn us or that we hear a noise they make before they spot us."
        //
        // Listed HERE and nowhere else, exactly as the comment above demands — and note what it buys for
        // free: the fan hears them through poured wall at #591's degraded reach, the smudge path already
        // draws a wall-blocked return as a smear, and none of that needed a line of new instrument code. A
        // guard walks, so a guard is a contact. There is no special case anywhere.
        // #830 · …AND WITH THEIR REGISTER ON THEM, which is the other half of what a contact is. A guard
        // walking is a mover like anything else; a guard at a beat stop is a LIVING body that has stopped
        // travelling, and the fan owes the captain the unsure blob for it rather than clean silence. The
        // register is Core's own answer (PatrolBeat.FanRegister) and not a flag typed here — the day a
        // second kind of figure walks these floors it declares its own, in Core, beside its own rules.
        // #870 lane 6′a · TheRoundOnFoot is the patrol family's own name for the men walking this floor. The
        // CONTACT is still declared here, exactly as the comment above demands — every kind of figure that
        // moves is listed in this one method — and only the reaching-in is gone.
        foreach (Guard g in TheRoundOnFoot)
        {
            yield return new MotionTracker.Entity(g.X, g.Y, g.Vx, g.Vy, PatrolBeat.FanRegister);
        }
    }
}

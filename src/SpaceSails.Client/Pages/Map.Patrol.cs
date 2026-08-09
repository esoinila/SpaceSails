using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #804 · THE ROUNDS, ON THE FLOOR. The brain is <see cref="PatrolBeat"/> in Core and every number, every
/// stop and every sentence below comes from it; this file is the walking, the knowing and the telling.
///
/// <para><b>They walk the A* the audits walk.</b> Owner, #729: <i>"maybe the guards can use A* also so they
/// do not come off as reevers or kind of crazy scary."</i> A leg of a round is planned with
/// <see cref="AutoWalk"/> over <c>DeckReachability</c> — the same route machinery the click-to-walk cheat
/// hands the captain, over the same collision field the captain's own stepper asks — and spent through
/// <see cref="SurfaceCollision.Slide"/> at a person's gait. A thing that finds doorways is somebody on a
/// payroll, and the player is meant to read that off the motion before any card says so.</para>
///
/// <para><b>What the captain knows and what the guard knows are different, and the difference is the
/// feature.</b> A marker is drawn only inside the captain's own sightline; outside it the motion tracker
/// hears a mover through the rock (the instrument is untouched — a guard walks, so a guard is a contact),
/// and closer-but-unseen there are boots. The guard registers the captain at a THIRD of the eye's reach, so
/// there is a real window in which you can see them and they cannot see you. That window is the whole
/// stealth verb: watch the round, wait, step out behind it.</para>
///
/// <para><b>Nothing in this file can start a chase.</b> A sighting raises a card, the card reads the wallet,
/// and the worst outcome is a walk back to the lift. That is the owner's law, and it is enforced by there
/// being no other branch.</para>
/// </summary>
public sealed partial class Map
{
    /// <summary>One guard, walking. Mutable and client-side for the reason the Reevers and the sweep team
    /// are: the rules are pure in Core and the list is the client's business.</summary>
    private sealed class Guard
    {
        /// <summary>What is drawn over them — the ROUND's number, from Core.</summary>
        public required string DeckName { get; init; }

        /// <summary>Who they read as when the round stops at you.</summary>
        public required string Plate { get; init; }

        public double X;
        public double Y;
        public double Facing;

        /// <summary>How fast they are travelling this frame. A MOTION tracker hears travel and nothing else,
        /// so without this the fan would report an empty floor while two people walked it — the panel
        /// disagreeing with the sim, which is the one thing this codebase does not allow.</summary>
        public double Vx;
        public double Vy;

        /// <summary>Which stop of the shared beat they are heading for.</summary>
        public int Leg;

        /// <summary>Seconds left standing at the stop they have reached. THE GAP a captain times.</summary>
        public double Standing;

        /// <summary>Seconds since this one last stopped the round at the captain. Starts far in the past so
        /// the first challenge of a floor is never held back.</summary>
        public double SinceStop = PatrolBeat.AfterTheStopSeconds * 4;

        /// <summary>The A* leg they are spending, or null when the next one is due.</summary>
        public AutoWalk? Route;

        /// <summary>Whether the captain could see them on the frame just drawn. Read by the droid filler,
        /// written by the step — one answer per frame, so the marker and the challenge cannot disagree about
        /// whether there is anybody there.</summary>
        public bool Drawn;
    }

    private readonly List<Guard> _guards = [];

    /// <summary>The beat: the stops, in the order this watch walks them. Built once when the floor is
    /// entered so every guard on it shares ONE route and a captain can learn it — the sweep team's rule,
    /// for its reason: being hidden from has to be legible.</summary>
    private readonly List<PatrolBeat.Stop> _patrolBeat = [];

    /// <summary>How long the captain has been on this floor. Feeds <see cref="PatrolBeat.CanBeNoticed"/>, so
    /// stepping out of the car into somebody's face is a beat rather than an instant.</summary>
    private double _patrolFloorSeconds;

    /// <summary>How long since the boots were last mentioned. One line, cooled — a warning, not a
    /// narrator.</summary>
    private double _patrolHeardAgo;

    /// <summary>Dev cheat: <c>?patrol=N</c> forces N rounds onto whatever restricted floor you boot onto,
    /// so the scene is reachable without waiting for a watch that rolled two.</summary>
    private int? _patrolCheat;

    /// <summary>Dev cheat: <c>?badge=1</c> mints this site's own pass at the landing, so the satisfied arm
    /// of the challenge is reachable without working the whole cage-crew lane first.</summary>
    private bool _badgeCheat;

    /// <summary>How long between mentions of the boots. Long enough that it is an event; short enough that
    /// a captain who has walked away and come back is told again.</summary>
    private const double HeardAgainSeconds = 20.0;

    /// <summary>How many figures a patrol may need drawing. The band in the droid buffer, stated once.</summary>
    private const int PatrolBand = PatrolBeat.MostOnAFloor;

    // ── PUTTING THEM ON THE FLOOR ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Build the round for the floor the captain has just arrived on, or clear it if this floor has none.
    /// Called from the lift ride — the one place a floor changes — and never from the deck rebuild, which
    /// happens every time a room is searched and would restart the round under a captain who was timing it.
    /// </summary>
    private void SpawnPatrolFor(SurfaceExcursion ex)
    {
        _guards.Clear();
        _patrolBeat.Clear();
        _patrolFloorSeconds = 0;
        _patrolHeardAgo = HeardAgainSeconds;

        string bodyId = ex.Stop.Body.Id;
        int level = ex.Floor;
        if (!PatrolBeat.IsPatrolled(bodyId, level))
        {
            return;
        }

        SurfaceLayout.Field field = MoonSurface.ExpeditionField();
        UndergroundComplex.FloorPlan floor = UndergroundComplex.Build(bodyId, level, field);

        _patrolBeat.AddRange(PatrolBeat.BeatFor(bodyId, level, ex.CanteenWatch, floor, field));
        if (_patrolBeat.Count < 2)
        {
            // A floor whose plan yields nothing to walk gets nobody. It is not a failure worth a sentence —
            // an empty corridor is what this building is mostly made of — but it must not put a guard on a
            // round with one stop in it, standing still forever at the car.
            _patrolBeat.Clear();
            return;
        }

        int heads = _patrolCheat is { } forced
            ? System.Math.Clamp(forced, 0, PatrolBeat.MostOnAFloor)
            : PatrolBeat.GuardsOn(bodyId, level, ex.CanteenWatch);

        for (int i = 0; i < heads; i++)
        {
            int leg = PatrolBeat.StartLeg(_patrolBeat.Count, i, System.Math.Max(1, heads));
            PatrolBeat.Stop at = _patrolBeat[leg];
            _guards.Add(new Guard
            {
                DeckName = PatrolBeat.DeckName(i),
                Plate = PatrolBeat.PlateOf(bodyId, level, ex.CanteenWatch, i),
                X = at.X,
                Y = at.Y,
                Leg = (leg + 1) % _patrolBeat.Count,
                Standing = PatrolBeat.StandSeconds,
            });
        }
    }

    /// <summary>Is anybody walking this floor right now? Read by the HUD wiring and the tests.</summary>
    private bool PatrolOnThisFloor => _guards.Count > 0;

    // ── THE LOOP ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Walk them, decide what each side can know about the other, and let a sighting raise the
    /// card. Called once a frame from <c>StepSurface</c>.</summary>
    private void AdvancePatrol(double dtRealSeconds)
    {
        if (_guards.Count == 0 || _surface is not { Floor: < 0 })
        {
            return;
        }

        double dt = System.Math.Min(dtRealSeconds, MaxSurfaceStepSeconds);
        _patrolFloorSeconds += dt;
        _patrolHeardAgo += dt;

        IReadOnlyList<SurfaceCollision.Segment> walls = _deckPlan.CollisionField;
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();

        bool anythingHeard = false;
        foreach (Guard g in _guards)
        {
            g.SinceStop += dt;
            WalkTheRound(g, dt, walls);

            // WHAT THE CAPTAIN MAY KNOW. One call, one answer, used by the marker and by nothing else — so
            // a guard behind a wall is off the deck by construction rather than by a renderer's opinion.
            g.Drawn = PatrolBeat.DrawnFor(_avatarX, _avatarY, g.X, g.Y, sight);
            anythingHeard |= PatrolBeat.Heard(_avatarX, _avatarY, g.X, g.Y, sight);
        }

        // …and the ear, once, cooled. It is deliberately said only for somebody the captain CANNOT see: a
        // line about boots over a marker you are looking at is the picture and the sentence disagreeing.
        if (anythingHeard && _patrolHeardAgo >= HeardAgainSeconds)
        {
            _patrolHeardAgo = 0;
            ShowPulseMessage(PatrolBeat.HeardLine);
            LogAutopilotEvent(PatrolBeat.HeardLine);
        }

        StopTheRoundIfAnybodySeesYou(sight);
    }

    /// <summary>
    /// One leg of the round. The route is planned with A* between two published stops and spent through the
    /// captain's own collision at a person's gait, so a guard finds doorways and slides off walls exactly as
    /// a person does. Arriving starts the STAND — the gap the whole feature is about.
    /// </summary>
    private void WalkTheRound(Guard g, double dt, IReadOnlyList<SurfaceCollision.Segment> walls)
    {
        if (g.Standing > 0)
        {
            g.Standing -= dt;
            g.Vx = 0;   // a stopped mover drops off a motion fan, honestly
            g.Vy = 0;
            return;
        }

        PatrolBeat.Stop target = _patrolBeat[g.Leg % _patrolBeat.Count];

        if (g.Route is not { Active: true })
        {
            var from = new DeckReachability.Point(g.X, g.Y);
            var to = new DeckReachability.Point(target.X, target.Y);
            AutoWalk.Attempt planned = AutoWalk.Plan(
                true, from, to, walls, DeckPlan.AvatarRadius,
                AutoWalk.BoundsFor(_deckPlan.CollisionSegments, from, to));

            if (planned.Route is null)
            {
                // Nothing connects. The audit says this cannot happen on a floor this generator builds
                // (§13.1) and a guard is not the place to find out otherwise — so the round simply drops
                // the stop and carries on rather than standing in a corridor forever.
                g.Leg = (g.Leg + 1) % _patrolBeat.Count;
                return;
            }
            g.Route = planned.Route;
        }

        double budget = PatrolBeat.WalkSpeed * dt;
        double startX = g.X, startY = g.Y;
        for (int step = 0; step < AutoWalkSubStepsPerFrame && budget > 0; step++)
        {
            if (!g.Route.TryStep(g.X, g.Y, budget, out double dx, out double dy))
            {
                break;
            }

            (double nx, double ny) = SurfaceCollision.Slide(
                g.X, g.Y, dx, dy, DeckPlan.AvatarRadius, walls, SurfaceCollision.Gait.Person);

            double mx = nx - g.X, my = ny - g.Y;
            if ((mx * mx) + (my * my) < 1e-18)
            {
                g.Route.Snag();
                break;
            }

            budget -= System.Math.Sqrt((mx * mx) + (my * my));
            g.X = nx;
            g.Y = ny;
        }

        double tx = g.X - startX, ty = g.Y - startY;
        g.Vx = dt > 0 ? tx / dt : 0;
        g.Vy = dt > 0 ? ty / dt : 0;
        if ((tx * tx) + (ty * ty) > 1e-8)
        {
            // Face the way they are actually travelling, not the way they wanted to — truer, and it is what
            // makes a round readable from the far end of a corridor.
            g.Facing = System.Math.Atan2(ty, tx);
        }

        double gx = target.X - g.X, gy = target.Y - g.Y;
        bool there = (gx * gx) + (gy * gy) <= PatrolBeat.AtTheStopDu * PatrolBeat.AtTheStopDu;
        if (there || g.Route is not { Active: true })
        {
            g.Route = null;
            g.Standing = PatrolBeat.StandSeconds;
            g.Leg = (g.Leg + 1) % _patrolBeat.Count;
        }
    }

    // ── THE CHALLENGE ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The first guard who registers the captain stops the round. One at a time, and never while a card is
    /// already up: two of these on one screen would be the stacked-card mistake #777 named, and a challenge
    /// behind a backdrop is a challenge nobody read.
    /// </summary>
    private void StopTheRoundIfAnybodySeesYou(IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        if (_viewObject is not null || !PatrolBeat.CanBeNoticed(_patrolFloorSeconds) || _surface is not { } ex)
        {
            return;
        }

        foreach (Guard g in _guards)
        {
            if (g.SinceStop < PatrolBeat.AfterTheStopSeconds
                || !PatrolBeat.Notices(g.X, g.Y, _avatarX, _avatarY, sight))
            {
                continue;
            }

            TheRoundStopsAtYou(ex, g);
            return;
        }
    }

    /// <summary>
    /// He stops, reads what is in your wallet, and tells you the answer. The judgement is Core's and only
    /// Core's; this raises the card, spends the nerve and — when it comes to that — walks you back.
    /// </summary>
    private void TheRoundStopsAtYou(SurfaceExcursion ex, Guard g)
    {
        g.SinceStop = 0;
        g.Standing = PatrolBeat.StandSeconds;
        g.Vx = 0;
        g.Vy = 0;
        g.Facing = System.Math.Atan2(_avatarY - g.Y, _avatarX - g.X);

        PatrolBeat.Read read = PatrolBeat.TheGuardReads(ex.Stop.Body.Id, g.Plate, _satchel);

        // #684's idiom, one building along: the read is TOLD on a card, with the outcome in the card's own
        // amber row (#736) rather than pulsed under a backdrop nobody can see through. No picture yet — the
        // markup renders caption-only when none is wired, which is the house's degradation law, and the
        // painting drops in behind it.
        _viewObject = new DeckPlan.ConsoleSpot(
            DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
            read.Label, null, read.Card, read.Told);
        RendererInterop.PlayCue("reveal");

        LogAutopilotEvent($"{read.Label} — {read.Told}");

        // A pass that works is a non-event and files nothing: a notebook full of manners is a notebook
        // nobody reads. Being walked off a floor is not manners.
        if (read.Satisfied)
        {
            ApplyNerveShock(NervePips.PipUnit, "somebody on this floor knows your face now");
            return;
        }

        ApplyNerveShock(NervePips.SightingPips * NervePips.PipUnit, "you were asked and could not answer");
        FileNote(PatrolBeat.EscortNote, "👮");

        // The mildest honest consequence, and the whole of it: back to the car. Placed through the one door
        // the sim ever puts the captain through (#681), so an escort can never end inside a wall.
        (double sx, double sy) = HiveInterior.SpawnOn(MoonSurface.ExpeditionField());
        StandCaptainAt(sx, sy, "the guard walks you back to the lift");
        RequestVaultSave();
    }

    // ── WHERE THE PASS COMES FROM ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// #804 · The site puts the captain on its books. Called from the ARRIVAL the day-labour chit opened
    /// (#752) — the moment the gig is not a promise any more — and once per site, because a wallet with two
    /// identical passes in it would be the pocket saying something the sim did not.
    /// </summary>
    private void IssueTheSitePass(SurfaceExcursion ex)
    {
        string bodyId = ex.Stop.Body.Id;
        if (PatrolBeat.BadgeHeld(bodyId, _satchel))
        {
            return;
        }

        Satchel.Item pass = PatrolBeat.Badge(bodyId);
        if (!Satchel.CanTake(_satchel, pass))
        {
            return;   // the wallet never fills, so this cannot happen — and a silent grant that did not land
        }             // is exactly the third named bug class, so it is asked rather than assumed (§13.9).

        _satchel = [.. Satchel.Add(_satchel, pass)];
        ShowPulseMessage(PatrolBeat.BadgeIssuedLine);
        LogAutopilotEvent(PatrolBeat.BadgeIssuedLine);
        FileNote(PatrolBeat.BadgeGist, PatrolBeat.BadgeGlyph);
    }

    // ── DRAWING THEM, AND HEARING THEM ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fill the guards into the droid buffer — and ONLY the ones the captain can actually see. An unseen
    /// round is parked off-map at the same coordinates an empty slot uses, the #371 idiom the Old Ones
    /// already take, so a guard behind a wall is not on the deck at all rather than drawn dim.
    /// </summary>
    private void FillPatrolDroids(DeckPlan.Droid[] buffer, int firstSlot)
    {
        for (int i = 0; i < PatrolBand; i++)
        {
            int slot = firstSlot + i;
            if (slot >= buffer.Length)
            {
                return;
            }

            buffer[slot] = i < _guards.Count && _guards[i].Drawn
                ? new DeckPlan.Droid(_guards[i].X, _guards[i].Y, _guards[i].Facing, _guards[i].DeckName)
                : new DeckPlan.Droid(-9999, -9999, 0, PatrolBeat.DeckName(i));
        }
    }
}

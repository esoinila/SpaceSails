using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #313 · THE MOTION TRACKER — a crude corner sweep of MOVING contacts, and the prowl that makes them
/// worth sweeping for.
///
/// <para>Motion only: a wall-blocked, momentarily-still Old One drops off the fan, which is what makes
/// the instrument frightening rather than merely informative. The smudges, the ghosts that settle where a
/// contact was last heard, the nest's churn, and the ambush range are all here, with the per-frame refill
/// of the bot marks.</para>
///
/// <para>Split out of <c>Map.Surface.Hud.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── The motion tracker HUD (#313): a crude corner sweep of MOVING contacts, built for the renderer.
    //    Motion only — a wall-blocked, momentarily-still Old One drops off the fan. ──

    /// <summary>The fuzzy returns painted on the deck for contacts the fan hears through steel. Held here
    /// and refilled per frame, like every other HUD buffer.</summary>
    private readonly List<(double X, double Y, double Radius)> _hudSmudges = [];

    /// <summary>How wide a return is at point-blank, in deck units — already a REGION rather than a spot,
    /// because a crude fan never knew better than that.</summary>
    private const double SmudgeBaseRadius = 2.6;

    /// <summary>And how much wider per unit of range: the further off the contact, the vaguer the ear.</summary>
    private const double SmudgeRangeSpread = 0.12;

    /// <summary>Where the fan last heard each contact, and when — the raw material for the ghosts.</summary>
    private readonly Dictionary<Reever, (double X, double Y, double HeardAtMs)> _ghosts = [];

    /// <summary>The fading "movement was here" marks handed to the renderer.</summary>
    private readonly List<(double X, double Y, double Fade)> _hudGhosts = [];

    /// <summary>The nest's own motion, for the fan's benefit. It goes nowhere; it is never still. Anything
    /// above <see cref="MotionTracker.StillSpeed"/> reads as a live return, which is the truth about it.</summary>
    private const double NestChurn = 0.6;

    /// <summary>How wide the nest reads. Deliberately larger than any body smudge — the captain should be
    /// able to tell "something is in there" from "THAT is what is in there" at a glance.</summary>
    private const double NestSmudgeRadius = 4.2;

    /// <summary>Where the nest is, while it is still producing. Null once her room has been blown — a vented
    /// nest is off the tracker and off the map, and that silence is the reward for the soak.</summary>
    private (double X, double Y)? LiveNestPosition()
    {
        if (_wreck is not { Cause: Derelict.WreckCause.Infested })
        {
            return null;
        }
        if (!_ventSpaces.TryGetValue(WreckLayout.NestCompartment, out HullVenting.Space nest)
            || nest.Vented || !nest.Infested)
        {
            return null;
        }

        DeckReachability.Point at = WreckLayout.CauseStation(Derelict.WreckCause.Infested);
        return (at.X, at.Y);
    }

    /// <summary>How long a fresh return takes to settle from bright to its resting glow — the phosphor
    /// cooling, not the memory expiring. FLAGGED for tuning.</summary>
    private const double GhostSettleSeconds = 5.0;

    /// <summary>And the glow it never drops below. THE TRACKER REMEMBERS: a mark stays until the same
    /// contact is heard somewhere else. It is only wiped by better information, never by time.</summary>
    private const double GhostFloor = 0.45;

    /// <summary>How close a contact has to appear, with no warning, to land the ambush fright. Tight on
    /// purpose: this is "it was already in the room", not "I can see it down the corridor". FLAGGED.</summary>
    private const double AmbushRange = 7.0;

    /// <summary>
    /// #488 · THE PROWL — how a woken Old One that has not found you yet moves about a dead ship.
    ///
    /// <para>Deliberately NOT the regolith behaviour: out on the ground an unaware contact keeps its own
    /// deep and holds still, by the owner's own ruling, and that is untouched. Aboard, stillness would mean
    /// a motion tracker that never hears anything until the moment something is on top of you — which
    /// defeats the instrument the corridors were built around.</para>
    ///
    /// <para>Slow, aimless, and honest: it picks somewhere to be, walks there obeying the walls, and picks
    /// again. It is not searching for the captain — it does not know there is one. It is just awake.</para>
    /// </summary>
    private void Prowl(Reever r, IReadOnlyList<SurfaceCollision.Segment> walls, double radius,
                       double step, double now)
    {
        r.Idle = false;

        if (now >= r.ProwlUntilMs)
        {
            // Somewhere else on this deck, chosen off its own seed so each one wanders its own way and the
            // pack does not migrate as a blob.
            ulong pick = r.JitterSeed + (ulong)(now / ProwlLegMs);
            r.ProwlX = WreckLayout.AftX + 2 + ((pick % 53UL) / 53.0 * (WreckLayout.BowX - 8 - WreckLayout.AftX));
            r.ProwlY = ((pick / 53UL) % 3UL) switch { 0 => -6.0, 1 => 0.0, _ => 6.0 };
            r.ProwlUntilMs = now + ProwlLegMs;
        }

        double prowlStep = step * ProwlSpeedFraction;
        (double nx, double ny) = ReeverChase.Step(
            r.X, r.Y, r.ProwlX, r.ProwlY, prowlStep, double.PositiveInfinity, walls, radius,
            (r.JitterSeed & 1) == 0 ? 1 : -1);

        // Real velocity, because that is the entire point: the fan hears MOTION.
        r.Vx = prowlStep > 0 ? (nx - r.X) / (step / ReeverSpeed) : 0;
        r.Vy = prowlStep > 0 ? (ny - r.Y) / (step / ReeverSpeed) : 0;
        r.X = nx;
        r.Y = ny;
        r.Facing = System.Math.Atan2(r.ProwlY - ny, r.ProwlX - nx);

        // Wedged against something, or arrived: take a new bearing next frame rather than grinding.
        if (System.Math.Abs(r.Vx) + System.Math.Abs(r.Vy) < 0.01)
        {
            r.ProwlUntilMs = 0;
        }
    }

    /// <summary>How long a prowler holds one bearing before picking another.</summary>
    private const double ProwlLegMs = 7_000;

    /// <summary>A prowl is a wander, not a hunt — well under the chase so a contact that has actually SEEN
    /// you is unmistakably faster. FLAGGED for tuning.</summary>
    private const double ProwlSpeedFraction = 0.42;

    /// <summary>Gather the deployed sentries for the renderer. Pulled out of the full HUD build so the
    /// WRECK path can have them too: a bot on a steel deck is drawn exactly like a bot on regolith, and it
    /// was only ever invisible aboard because the whole hud was suppressed to get rid of the tracker.</summary>
    private void RefreshHudBots(SurfaceExcursion ex)
    {
        double nowMs = _lastTimestampMs ?? 0;
        _hudBots.Clear();
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed)
            {
                continue;
            }
            _hudBots.Add((b.X, b.Y, SentryBot.Readout(b.Rounds), b.Rounds <= 0, b.FiringUntilMs > nowMs, b.AimX, b.AimY));
        }

        // #316 law 1, second half · AND THE ONES SOMEBODY ELSE LEFT STANDING. A rival crew that walked into
        // a full pack over one hole did not stroll back for their hardware, and what is left is a sentry
        // frozen at 00 on ground the captain never fought on — which is the most eloquent husk in the game
        // and one #314 already knows how to draw. So it is not a new mark and not a new list: it comes
        // through here as the dry bot it is, with no rounds, never firing, aiming at nothing.
        foreach (GroundMemory.Scar scar in ex.Scars)
        {
            if (scar.What == GroundMemory.ScarKind.DryBot)
            {
                _hudBots.Add((scar.X, scar.Y, SentryBot.Readout(0), true, false, scar.X, scar.Y));
            }
        }
    }
}

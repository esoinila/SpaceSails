namespace SpaceSails.Core;

/// <summary>
/// #251 · FILLING ONE AND FIRING IT — carrying one home to fill it, the dry and the fire, range and
/// engagement, the deployed/target/husk/volley records, and the step that fires a volley.
///
/// <para>Split out of <c>SentryBot.cs</c> under #251 as a pure move: one contiguous run, no member renamed,
/// re-scoped or re-ordered. Its one field is a <c>const</c>; <c>RosterUnits</c>, the class's one
/// initialised static, stays in the opening file (#1163).</para>
/// </summary>
public static partial class SentryBot
{
    // ── Carrying one home to fill it ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// CARRY IT BACK TO THE LOCK AND IT FILLS. Owner, thinking about a hull too big to run home from:
    /// <i>"Carrying the autogun to our shuttle air-lock should reload it ( might ve needed for big ship) 😎"</i>
    ///
    /// <para>The boat carries the belts; the bot does not. So a drained sentry is not scrap and it is not a
    /// resource problem — it is a WALK, and the walk is the price. On a small hull that is a stroll and the
    /// mechanic barely registers; on the 4× hauler of #531 it is a decision with a pack somewhere behind you,
    /// which is exactly where a logistics rule earns its keep.</para>
    ///
    /// <para>Deliberately free of any other currency. The cost is time and exposure, the same way the pump's
    /// cost is time rather than credits — and a captain who has already carried the thing the length of a
    /// wreck has paid enough.</para>
    /// </summary>
    public static bool NeedsFilling(int rounds) => rounds < MaxMagazine;

    /// <summary>What the lock says when the belts go in.</summary>
    public static string FilledLine(string unit, int wasCarrying) =>
        $"🤖 {unit} back on the belts at the lock — {Readout(wasCarrying)} → {Readout(MaxMagazine)}. The boat " +
        "carries the ammunition; the bot only carries what you last gave it.";

    /// <summary>…and when there was nothing to do.</summary>
    public static string AlreadyFullLine(string unit) =>
        $"🤖 {unit} is already full at {Readout(MaxMagazine)}. Nothing to give it.";

    /// <summary>A dry bot: 00 on the readout, frozen and silent (fires nothing, drains nothing).</summary>
    public static bool IsDry(int rounds) => rounds <= 0;

    /// <summary>One trigger pull: drain a round if any remain. At 00 it stays 00 — the counter freezes,
    /// the bot goes quiet. This is the whole ammo law in one line.</summary>
    public static int Fire(int rounds) => rounds > 0 ? rounds - 1 : 0;

    /// <summary>The rounds a pack of <paramref name="reevers"/> costs to clear — the siege-math read the
    /// UI can show ("a 6-pack is 84 rounds; you carry 99").</summary>
    public static int RoundsForPack(int reevers) => System.Math.Max(0, reevers) * RoundsPerReever;

    /// <summary>Is a target inside a bot's <see cref="RangeDeckUnits"/> engagement arc?</summary>
    public static bool InRange(double botX, double botY, double targetX, double targetY)
    {
        double dx = targetX - botX, dy = targetY - botY;
        return (dx * dx) + (dy * dy) <= RangeDeckUnits * RangeDeckUnits;
    }

    /// <summary>#437 · Can this bot actually ENGAGE the target — in the arc AND with a clear line to it?
    /// Owner, live 2026-07-26: "Now the cannons shot though the walls." #324 made the maze law for movers
    /// (a Reever can neither walk through stone nor see through it); a gun that shoots through the same
    /// stone quietly undoes it — a sentry in a walled pocket would clear ground it cannot even see, and the
    /// captain's cornering geometry would stop mattering. Same sight primitive the Old Ones use, so there
    /// is one source of truth. No walls passed → range alone, exactly as before.</summary>
    public static bool CanEngage(
        double botX, double botY, double targetX, double targetY,
        IReadOnlyList<SurfaceCollision.Segment>? walls) =>
        InRange(botX, botY, targetX, targetY)
            && SurfaceCollision.HasLineOfSight(botX, botY, targetX, targetY, walls);

    /// <summary>A deployed sentry standing on the surface: its unit name, position, and the rounds left
    /// on its magazine. Value data — the client owns the live list and its motion.</summary>
    public readonly record struct Deployed(string Unit, double X, double Y, int Rounds)
    {
        /// <summary>00 — frozen and silent.</summary>
        public bool Dry => Rounds <= 0;

        /// <summary>The two-digit readout glyphs at this bot.</summary>
        public string Readout => SentryBot.Readout(Rounds);
    }

    /// <summary>A live Old One the sentries can shoot: where it stands and how many rounds it has already
    /// soaked (a bot grinds it down over <see cref="RoundsPerReever"/> hits before it drops).</summary>
    public readonly record struct Target(double X, double Y, int HitsTaken);

    /// <summary>A downed Old One's HUSK — the mark it leaves where it fell. Carries ONLY a position (the
    /// forensic evidence #316 will read); a husk is never loot, never touches the purse or the hold.</summary>
    public readonly record struct Husk(double X, double Y);

    /// <summary>The settled result of one fire-tick volley: bots with rounds drained, the surviving
    /// Reevers with their new hit counts, the husks minted this volley, and how many shots were fired.
    /// There is deliberately NO coin/cargo output — engagement can never touch loot (mirrors
    /// <see cref="ReeverRaid"/>'s no-loot law).</summary>
    public readonly record struct Volley(
        IReadOnlyList<Deployed> Bots,
        IReadOnlyList<Target> Reevers,
        IReadOnlyList<Husk> Husks,
        int Shots);

    /// <summary>Resolve ONE fire-tick: every bot with rounds fires a single round at the nearest live
    /// Reever inside its arc, draining the magazine and adding one hit; a Reever reaching
    /// <see cref="RoundsPerReever"/> hits goes down and leaves a <see cref="Husk"/> where it stood. Dry
    /// bots (00) and bots with nothing in the arc fire nothing. A target downed earlier in the volley is
    /// off the board for the remaining bots, so no shot is wasted on a corpse. Deterministic: nearest by
    /// distance, ties broken by index — the client calls this once per <see cref="FireIntervalSeconds"/>.
    ///
    /// <para>#437: a bot only engages what it can SEE. <paramref name="walls"/> are the same segments the
    /// captain and the Old Ones collide and sight against — a slab between gun and target breaks the shot,
    /// so the nearest target is the nearest VISIBLE one, and a bot with nothing it can see holds fire and
    /// drains nothing (the no-shot/no-drain law). Pass none for the open-ground behaviour.</para></summary>
    /// <summary>#603 · How far off the line of fire a second target may stand and still be caught by the same
    /// round. About a body's width — a pack queued down a corridor is caught, a pack fanned out is not.</summary>
    public const double PenetrationCorridorDu = 1.6;

    /// <summary>#603 · The next target standing behind <paramref name="first"/> on the same line of fire —
    /// further from the gun, and within a hand's width of the shot's own bearing. Returns -1 when the pack is
    /// not queued up, which is most of the time and is exactly the point.</summary>
    private static int BehindTheFirst(
        Deployed bot, IReadOnlyList<Target> reevers, bool[] alive, int first)
    {
        double fx = reevers[first].X - bot.X, fy = reevers[first].Y - bot.Y;
        double firstDist = System.Math.Sqrt((fx * fx) + (fy * fy));
        if (firstDist <= 0.001)
        {
            return -1;
        }
        double ux = fx / firstDist, uy = fy / firstDist;

        int best = -1;
        double bestAlong = double.MaxValue;
        for (int j = 0; j < reevers.Count; j++)
        {
            if (!alive[j] || j == first)
            {
                continue;
            }
            double dx = reevers[j].X - bot.X, dy = reevers[j].Y - bot.Y;
            double along = (dx * ux) + (dy * uy);                        // down the line of fire
            if (along <= firstDist)
            {
                continue;                                                // beside or in front, not behind
            }
            double across = System.Math.Abs((dx * -uy) + (dy * ux));     // off the line
            if (across > PenetrationCorridorDu)
            {
                continue;
            }
            if (along < bestAlong)
            {
                bestAlong = along;
                best = j;
            }
        }
        return best;
    }

    /// <param name="ammo">#603 · What each bot is loaded with, in the same order as <paramref name="bots"/>.
    /// Null — and any missing entry — means issue ball, so every existing caller and every existing test is
    /// unchanged by construction.
    ///
    /// <para>Owner: <i>"some special ammo that only uses one round per reever"</i> and <i>"those rounds would
    /// go through several reevers if in group also"</i>. Both are the same fact about a round that arms after
    /// travel and does its work on the far side of the first thing it meets.</para></param>
    /// <param name="line">#326 · The captain→home corridor, when there is a captain on this ground. Anything
    /// standing in it outranks anything that is not, at any range — <see cref="SentryDoctrine.Pick"/> owns
    /// that comparison, and this method owns nothing about it. Null is the pre-#326 world exactly: no
    /// corridor, so the pick is the nearest visible target, which is what every existing caller and every
    /// existing test gets by construction.</param>
    public static Volley Step(
        IReadOnlyList<Deployed> bots, IReadOnlyList<Target> reevers,
        IReadOnlyList<SurfaceCollision.Segment>? walls = null,
        IReadOnlyList<Ammunition.Kind>? ammo = null,
        SentryDoctrine.RetreatLine? line = null)
    {
        System.ArgumentNullException.ThrowIfNull(bots);
        System.ArgumentNullException.ThrowIfNull(reevers);

        var botRounds = new int[bots.Count];
        for (int i = 0; i < bots.Count; i++)
        {
            botRounds[i] = bots[i].Rounds;
        }

        var hits = new int[reevers.Count];
        var alive = new bool[reevers.Count];
        for (int j = 0; j < reevers.Count; j++)
        {
            hits[j] = reevers[j].HitsTaken;
            alive[j] = true;
        }

        var husks = new System.Collections.Generic.List<Husk>();
        int shots = 0;

        for (int i = 0; i < bots.Count; i++)
        {
            if (botRounds[i] <= 0)
            {
                continue; // 00 — the readout is frozen, the bot silent
            }

            // #326 · WHICH ONE. Not the nearest any more — the one in the corridor between the captain and
            // the way home, if there is one, and only then the nearest. The whole comparison lives in
            // SentryDoctrine.Pick so there is one author of it: this loop used to BE the target rule, and a
            // second copy of a priority is how a bot comes to shoot one thing while the doctrine says
            // another. With `line` null it picks exactly what this loop picked.
            int best = SentryDoctrine.Pick(bots[i], reevers, alive, line, walls);
            if (best < 0)
            {
                continue; // nothing it can SEE in the arc — hold fire, no drain (#437)
            }

            Ammunition.Kind loaded = ammo is not null && i < ammo.Count ? ammo[i] : Ammunition.Issue;
            int toKill = System.Math.Max(1, loaded.HitsToKill);

            botRounds[i] = Fire(botRounds[i]);
            shots++;

            hits[best]++;
            if (hits[best] >= toKill)
            {
                alive[best] = false;
                husks.Add(new Husk(reevers[best].X, reevers[best].Y));
            }

            // #603 · AND IT KEEPS GOING. A round that arms after travel does not stop at the first thing it
            // meets — anything standing BEHIND that, on the same line, is in the same shot. Owner: "those
            // rounds would go through several reevers if in group also".
            //
            // Strictly behind and close to the bearing, so this rewards a pack that has queued itself down a
            // corridor and does nothing at all for one that has spread out. That is the round's whole
            // character: devastating in a corridor, unremarkable in the open, lethal to the firer up close.
            for (int through = 1; through < loaded.Penetrates; through++)
            {
                int next = BehindTheFirst(bots[i], reevers, alive, best);
                if (next < 0)
                {
                    break;
                }
                hits[next]++;
                if (hits[next] >= toKill)
                {
                    alive[next] = false;
                    husks.Add(new Husk(reevers[next].X, reevers[next].Y));
                }
            }
        }

        var outBots = new System.Collections.Generic.List<Deployed>(bots.Count);
        for (int i = 0; i < bots.Count; i++)
        {
            outBots.Add(bots[i] with { Rounds = botRounds[i] });
        }

        var survivors = new System.Collections.Generic.List<Target>();
        for (int j = 0; j < reevers.Count; j++)
        {
            if (alive[j])
            {
                survivors.Add(reevers[j] with { HitsTaken = hits[j] });
            }
        }

        return new Volley(outBots, survivors, husks, shots);
    }
}

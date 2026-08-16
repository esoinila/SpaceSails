namespace SpaceSails.Core;

/// <summary>
/// PR-314 · The ship's pirate sentries in the regolith (owner, live 2026-07-18): "We have those pirate
/// bots on the ship maybe they could protect from Reevers there ... and run low on ammo / power as they
/// keep coming... a little more of that Aliens movie threat... of running out of ammo. :-D"
///
/// <para>The captain loads real ship units — the two boarding troopers <b>K-77</b> and <b>R-3B</b>
/// (<see cref="RosterUnits"/>, the <c>DeckPlan</c> gun-deck lane) — as surface escorts. Deployed, a bot
/// pins and grinds down the Old Ones (Reevers) shambling within a modest arc: a zap line, the target
/// stopped, then downed to a HUSK left where it fell (the forensic mark #316 will read). But a bot is a
/// <b>timer wearing a number</b>: it carries a crude two-digit magazine — <see cref="MaxMagazine"/> max —
/// that ticks down one round per shot and freezes at 00, dim and silent. The many-law means a siege
/// ALWAYS outlasts the magazine; bots buy TIME, never safety.</para>
///
/// <para><b>The siege math.</b> <see cref="RoundsPerReever"/> is set so a full magazine downs roughly
/// one bad-roll pack (<see cref="ReeverRaid.MaxReevers"/> = 6) with almost nothing to spare for the
/// linger trickle: 99 ÷ 14 = 7 downs, then the counter reads 00 and the wall of slow signal keeps
/// coming. Pure and deterministic (nearest-target, stable index tie-break) so the drain, the down, the
/// husk and the restock receipt all pin in a Core test — the client owns only the real-time list.</para>
/// </summary>
public static class SentryBot
{
    /// <summary>The magazine depth — 99 crude digital letters, the owner's homage. A two-digit
    /// seven-segment readout maxes here; every shot ticks it down toward a frozen 00.</summary>
    public const int MaxMagazine = 99;

    /// <summary>Rounds to down one Old One. Chosen so a full <see cref="MaxMagazine"/> handles one bad
    /// roll's pack (6 Reevers = 84 rounds) with a single trickle's-worth to spare (99 − 84 = 15, one
    /// more down at 14), then runs dry. The magazine is a timer: 99 ÷ 14 ≈ 7 downs, no more.</summary>
    public const int RoundsPerReever = 14;

    /// <summary>The engagement arc, deck units. A bot fires on the nearest mover inside this radius —
    /// modest (a hair past the tracker's ≤18 du "closing" band) so bots hold a line, not the whole
    /// field. Reevers inside the arc are pinned (the client stops their advance) while they're ground down.</summary>
    public const double RangeDeckUnits = 22.0;

    /// <summary>Seconds between trigger pulls — the readable tick cadence. At five shots a second a full
    /// magazine empties in ~20 seconds of sustained fire, so the last dozen digits are readable from
    /// across the map (the addendum's intended glance-loop between the tracker and the dwindling number).</summary>
    public const double FireIntervalSeconds = 0.2;

    /// <summary>How many bots the ship musters — the two named boarding troopers, no bespoke soldier
    /// class. The captain brings 0..this many down at boarding.</summary>
    public const int RosterCap = 2;

    /// <summary>One honest price: credits per round to rearm a magazine, wherever the racking happens — a
    /// haven's service line or the ship's own down-tube (#562). A full two-bot refill from empty is ~198 cr.
    ///
    /// <para>#562 · Halved from 2 cr on the owner's ruling: <i>"let's keep the ammo cheap"</i>, because
    /// <i>"we want to encourage exploration and that takes ammo."</i> The cost of going deep is meant to be
    /// the WALK BACK and the rounds you spend getting there — the supply line, which he called <i>"the
    /// invisible tether to players distance"</i> — never a purse decision made at a desk. A six-pack of Old
    /// Ones costs 84 rounds to clear (<see cref="RoundsPerReever"/> = 14), so a hard fight is ~84 cr against
    /// a 1,500 cr starting purse: a chore you pay without thinking. If this price ever makes a captain
    /// ration rounds and stay aboard, it has broken the thing it exists to serve.</para></summary>
    public const int RestockPricePerRound = 1;

    /// <summary>#562 · How long one magazine takes to rack, in seconds. Owner's pick: <i>"a couple of
    /// seconds each"</i>, one bot after the other, each with its own progress bar — long enough to feel the
    /// ship working, short enough never to become a chore between runs.</summary>
    public const double RearmSecondsPerMagazine = 2.0;

    /// <summary>The ship's real armed units (the shuttle-bay boarding troopers, <c>DeckPlan.FillShipDroids</c>).
    /// These are the escorts the captain loads — the roster, not an invention.</summary>
    public static IReadOnlyList<string> RosterUnits { get; } = new[] { "K-77", "R-3B" };

    /// <summary>PR-324 · Rebuild the full roster's magazines from a save's stored list, padding any entry
    /// the save doesn't carry (a pre-#314/#322 vault has none, or an old save that lacked
    /// <c>ShipSection.SentryMagazines</c>) up to a FULL magazine. A load never permanently shrinks the
    /// roster: an old captain always finds K-77 and R-3B standing ready with 99 rounds, never a phantom
    /// empty rack. Deterministic and pure so the migration is a pinned law, not a client accident.</summary>
    public static IReadOnlyList<int> RosterFromSave(IReadOnlyList<int>? saved)
    {
        var mags = new int[RosterUnits.Count];
        for (int i = 0; i < mags.Length; i++)
        {
            mags[i] = saved is not null && i < saved.Count
                ? System.Math.Clamp(saved[i], 0, MaxMagazine)
                : MaxMagazine;
        }
        return mags;
    }

    /// <summary>The crude two-digit readout for a magazine: "99".."00", clamped. The digits ARE the
    /// homage; the client renders them seven-segment on the grid, dimmed once <see cref="IsDry"/>.</summary>
    public static string Readout(int rounds) => System.Math.Clamp(rounds, 0, MaxMagazine).ToString("D2");

    // ── #728 · THE MAGAZINES, ON THE SCREEN THE CAPTAIN IS ACTUALLY LOOKING AT ────────────────────────

    /// <summary>One sentry as the on-foot instrument reads it: who it is, what it is holding, and whether it
    /// is riding your sling or standing out there holding a line.</summary>
    public readonly record struct Carried(string Unit, int Rounds, bool Deployed);

    /// <summary>#728 · What the on-foot HUD says about your ammunition — the line the shelter's receipt was
    /// paying into and nothing on screen could show.
    ///
    /// <para>Owner, in the 2026-08-06 smoke run, pressing the shelter's wall press and reading <i>"70 rounds
    /// into your magazines"</i>: the sentence was TRUE, the rounds went where it said, and there was nowhere
    /// on the ground a captain could look to see it. A receipt into an account with no statement reads exactly
    /// like theft even when it is not — which is how a working feature comes to be filed as a bug.</para>
    ///
    /// <para><b>The register is the AIR line's</b> (#740): a NAME, a figure that says what it is, and then
    /// plain words. <c>MAGAZINES</c> heads it, each drum is printed against its own ceiling in the same two
    /// digits the counter over a deployed bot wears (<see cref="Readout"/>), and the state — slung or set
    /// down — is said rather than left to a glyph. There is no bare percentage anywhere in it, for the same
    /// reason the tank has none: a fraction is a number you have to convert before you can act on it.</para>
    ///
    /// <para><b>It never goes quiet.</b> A captain who brought no sentry down still gets a line, because
    /// "there is nothing here to fill" is the fact that explains the locker's whole answer — and an
    /// instrument that vanishes when the news is bad is the one shape #212 forbids outright.</para></summary>
    public static string MagazinesReadout(IReadOnlyList<Carried>? down)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (Carried bot in down ?? [])
        {
            // …AND THE TUBE'S OWN GUN IS NOT YOURS. GATE-1 hangs in the shuttle door, is topped back up after
            // every volley and never runs dry (SurfaceArrival.DoorSentryUnit) — the boat's fixture, and
            // "never counted against the sling" by its own law. A ledger of what you are carrying that listed
            // a permanent 99/99 you neither bought nor can spend would misreport both halves at once, and the
            // one figure in it that never moves is the one an eye learns to skip.
            //
            // Found by BOOTING THE SCENE rather than by reasoning (the owner's method): the first cut of this
            // line read "K-77 12/99 in the sling · R-3B 12/99 in the sling · GATE-1 99/99 set down" on the
            // very first screenshot of the shelter it was written for.
            if (!SurfaceArrival.IsDoorSentry(bot.Unit))
            {
                parts.Add($"{bot.Unit} {MagazineCell(bot.Rounds)} {WhereItIs(bot.Deployed)}");
            }
        }

        return parts.Count == 0 ? NoMagazinesLine : $"🔫 MAGAZINES · {string.Join(" · ", parts)}";
    }

    /// <summary>#837 · ONE DRUM, AS EVERY INSTRUMENT PRINTS IT — <c>"12/99"</c>.
    ///
    /// <para>The MAGAZINES line above is built out of these, and so is every row of the satchel's load
    /// chooser. That is the whole content of the issue's <i>no second arithmetic</i> clause: the picker and
    /// the readout cannot come to two views of one drum, because there is exactly one function in the build
    /// that turns a magazine into a number a captain reads. A chooser that formatted its own
    /// <c>rounds + "/" + cap</c> would agree with the instrument until the day one of them was edited, which
    /// is this repo's third named bug class waiting with a gun in its hand.</para></summary>
    public static string MagazineCell(int rounds) => $"{Readout(rounds)}/{MaxMagazine}";

    /// <summary>#837 · …and where the thing holding it is, in the readout's own two words. Said rather than
    /// left to a glyph (#728's ruling), and said in ONE place so the chooser's row and the instrument's line
    /// describe the same bot the same way.</summary>
    public static string WhereItIs(bool deployed) => deployed ? "set down" : "in the sling";

    /// <summary>#728 · Is there anything of the CAPTAIN'S on this ground with a magazine in it?
    ///
    /// <para>The one question the shelter's press must ask before it chooses between its two nothings, and it
    /// is the same question <see cref="MagazinesReadout"/> answers — asked in one place so the instrument and
    /// the press can never come to different conclusions about the same sling. The tube's own gun is not an
    /// answer to it: it is always full, so it silently made "nothing to fill" look like "everything is
    /// full", which is the exact lie this ticket is about wearing a different hat.</para></summary>
    public static bool AnythingToFill(IReadOnlyList<Carried>? down)
    {
        foreach (Carried bot in down ?? [])
        {
            if (!SurfaceArrival.IsDoorSentry(bot.Unit))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>What the readout says when nothing you own has a magazine on this ground. Not silence: this
    /// is the sentence that makes a locker press that fills nothing make sense.</summary>
    public const string NoMagazinesLine =
        "🔫 MAGAZINES · none down here — no sentry came with you";

    // ── WEAPONS TIGHT ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// THE ORDER THAT MAKES YOUR OWN GUNS STOP VOLUNTEERING. Owner, designing a scene where a black-ops team
    /// sweeps a hull the captain is hiding inside (#538): <i>"where we take our guns and hide to let them pass.
    /// One more berthed shuttle should be set to close off and don't shoot mode during that."</i>
    ///
    /// <para>He is naming a real hole. A deployed bot shoots what it sees, and the tube gun at the shuttle lock
    /// <b>never runs dry and holds the threshold</b> (#461) — so the first professional through that hatch gets
    /// shot by a machine the captain forgot they owned, and the fight that follows is not one anybody wins by
    /// hiding. Concealment is worthless while your own automation is still making decisions.</para>
    ///
    /// <para>It is the exact mirror of <c>fire at will</c>, which the captain's desk has carried since the
    /// Expanse consult, and it belongs beside it: a captain who can free the guns must be able to tie them. And
    /// note what it does NOT do — it never disarms the captain. Their own trigger still works, because a captain
    /// deciding to shoot is a different act from a machine deciding for them, and that distinction is the whole
    /// authority idiom this game runs on.</para>
    /// </summary>
    public static bool MayOpenFire(bool weaponsTight) => !weaponsTight;

    /// <summary>What the ship says when the order goes out.</summary>
    public const string WeaponsTightLine =
        "🤖 WEAPONS TIGHT — every bot safes its magazine and the tube gun stands down. Nothing of yours fires " +
        "unless you fire it. Your own trigger still works; theirs does not.";

    /// <summary>…and when it is lifted.</summary>
    public const string WeaponsFreeLine =
        "🤖 Weapons free — the bots have their arcs back, and the tube gun is holding the threshold again.";

    /// <summary>The reminder worth having, because forgetting this is the mistake the order exists to prevent:
    /// a tight gun is a gun that will not save you either.</summary>
    public const string TightIsAlsoUndefendedLine =
        "Tight means tight: while the order stands, nothing on your side shoots anything — including whatever " +
        "comes down the spine behind you.";

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
    public static Volley Step(
        IReadOnlyList<Deployed> bots, IReadOnlyList<Target> reevers,
        IReadOnlyList<SurfaceCollision.Segment>? walls = null,
        IReadOnlyList<Ammunition.Kind>? ammo = null)
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

            int best = -1;
            double bestSq = double.MaxValue;
            for (int j = 0; j < reevers.Count; j++)
            {
                if (!alive[j])
                {
                    continue;
                }
                double dx = reevers[j].X - bots[i].X, dy = reevers[j].Y - bots[i].Y;
                double d2 = (dx * dx) + (dy * dy);
                if (d2 <= RangeDeckUnits * RangeDeckUnits && d2 < bestSq
                    && SurfaceCollision.HasLineOfSight(bots[i].X, bots[i].Y, reevers[j].X, reevers[j].Y, walls))
                {
                    bestSq = d2;
                    best = j;
                }
            }
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

    // ── The restock economy: one honest price at a haven's service line (#119 receipts). ──

    /// <summary>Credits to top a single bot from <paramref name="rounds"/> back to a full magazine.</summary>
    public static int RestockCost(int rounds) =>
        System.Math.Max(0, MaxMagazine - System.Math.Clamp(rounds, 0, MaxMagazine)) * RestockPricePerRound;

    /// <summary>A rearm quote: the magazines after buying what the purse affords (filled in order), the
    /// rounds bought, and the total cost.</summary>
    public readonly record struct RestockQuote(int RoundsBought, int Cost, IReadOnlyList<int> Magazines);

    /// <summary>Quote a whole-roster rearm against the purse: buy every missing round the captain can
    /// afford, filling bots in order, and report the filled magazines + the receipt figures. A pure
    /// clamp — the client applies the magazines, spends <see cref="RestockQuote.Cost"/>, and prints
    /// <see cref="RestockReceiptLine"/>.</summary>
    public static RestockQuote QuoteRestock(IReadOnlyList<int> magazines, int credits)
    {
        System.ArgumentNullException.ThrowIfNull(magazines);
        var filled = new int[magazines.Count];
        int spent = 0, bought = 0;
        int budget = System.Math.Max(0, credits);
        for (int i = 0; i < magazines.Count; i++)
        {
            int cur = System.Math.Clamp(magazines[i], 0, MaxMagazine);
            int need = MaxMagazine - cur;
            int canAfford = (budget - spent) / RestockPricePerRound;
            int take = System.Math.Min(need, System.Math.Max(0, canAfford));
            filled[i] = cur + take;
            bought += take;
            spent += take * RestockPricePerRound;
        }
        return new RestockQuote(bought, spent, filled);
    }

    /// <summary>The armorer's chit — the #119 receipt voice for a sentry rearm.</summary>
    public static string RestockReceiptLine(int roundsBought, int cost) =>
        roundsBought <= 0
            ? "🧾 Sentry rearm — nothing to top off; the magazines already read full."
            : $"🧾 Sentry rearm — {roundsBought} rounds racked, {cost:N0} cr. The armorer stamps the chit and waves you on.";

    /// <summary>The ledger line for a sentry left behind on liftoff — a write-off (#119 voice). A dry
    /// bot's frozen 00 is exactly the forensic evidence the husks issue (#316) reads.</summary>
    public static string AbandonLedgerLine(string unit, int roundsLeft) =>
        IsDry(roundsLeft)
            ? $"🤖 {unit} abandoned on the regolith, counter frozen at 00 — written off. A sentry, run dry, left where it stood."
            : $"🤖 {unit} abandoned on the regolith, counter at {Readout(roundsLeft)} — written off. It still had rounds; nobody came back for it.";
}

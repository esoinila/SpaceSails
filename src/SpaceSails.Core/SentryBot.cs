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

    /// <summary>One honest price: credits per round to rearm/recharge at a haven's service line. A full
    /// two-bot refill from empty is ~396 cr — a purchasable margin, cheap against a cache but never free.</summary>
    public const int RestockPricePerRound = 2;

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
    /// distance, ties broken by index — the client calls this once per <see cref="FireIntervalSeconds"/>.</summary>
    public static Volley Step(IReadOnlyList<Deployed> bots, IReadOnlyList<Target> reevers)
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
                if (d2 <= RangeDeckUnits * RangeDeckUnits && d2 < bestSq)
                {
                    bestSq = d2;
                    best = j;
                }
            }
            if (best < 0)
            {
                continue; // nothing in the arc — hold fire, no drain
            }

            botRounds[i] = Fire(botRounds[i]);
            shots++;
            hits[best]++;
            if (hits[best] >= RoundsPerReever)
            {
                alive[best] = false;
                husks.Add(new Husk(reevers[best].X, reevers[best].Y));
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

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
public static partial class SentryBot
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

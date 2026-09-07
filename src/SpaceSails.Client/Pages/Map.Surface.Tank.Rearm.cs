using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #562 · THE TUBE REARMS YOU — the refill, the magazines racked back into the sentries, and the three
/// cards that say what just happened (the ground grew, the tube rearmed, the air changed).
///
/// <para>Her tube refills a suit several times faster than real time, because standing in an airlock
/// watching a gauge is not the game: getting home is the achievement and the top-up is a formality.</para>
///
/// <para>Split out of <c>Map.Surface.Tank.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    /// <summary>How fast her tube refills a suit — several times real time, because standing in an airlock
    /// watching a gauge is not the game. Getting home is the achievement; the top-up is a formality.</summary>
    private const double TubeRefillRate = 12.0;

    // ── #562 · THE TUBE REARMS YOU. ────────────────────────────────────────────────────────────────────
    //
    // Owner, playtesting Miranda with both sentries shouldered and dry: "The gun reload at airlock is not
    // working here now… I carry both guns but they are not being reloaded." He was right twice over.
    //
    // The bug: boarding the shuttle REMOVES the bots from _shipBots and puts them in ex.Bots, and they only
    // come back on liftoff. So for the whole excursion the roster is empty, and every rearm affordance — all
    // of which read _shipBots — reported "No bots aboard… they're deployed on a surface, or written off."
    // That is false in the one state it matters: the captain is carrying both of them, shouldered, in his own
    // airlock. Worse, it was a trap. A dry bot could not be fed until liftoff, and the reason you walked back
    // was that it went dry.
    //
    // The fix he asked for: "I expect them to be reloaded at that tube I was at." So the down-tube feeds
    // them — automatically, cheaply, one magazine at a time, with a bar you can watch and a receipt that
    // says what it cost.
    //
    // WHY A PLACE AND NOT A BUTTON — this is the design, in his words: "the reload forces the player to plan
    // their routes … and keep their supply line safe for retreat to reload", and the tube is therefore "the
    // invisible tether to players distance". Every excursion becomes a loop with a known anchor, and the
    // interesting question is how far out you dare go before the walk back costs more than the rounds would.
    // The retreat is the price; the credits deliberately are not (SentryBot.RestockPricePerRound, halved).
    private void StepTubeRearm(double dtRealSeconds)
    {
        if (_surface is not { } ex)
        {
            return;
        }

        // Standing anywhere but inside the tube ends it. No penalty and nothing lost: rounds already racked
        // are already in the magazine, and the bar simply starts over next time you come back.
        if (!MoonSurface.IsInDownTube(_avatarX, _avatarY))
        {
            ex.RearmBotIndex = null;
            ex.RearmProgress = 0;
            return;
        }

        // Nothing to feed, or nothing to feed it with. Both are quiet — a captain walks through this tube on
        // every single trip, and a tube that nags on the way out would be worse than one that never spoke.
        if (ex.RearmBotIndex is not { } idx)
        {
            idx = NextBotWantingRounds(ex);
            if (idx < 0 || _credits < SentryBot.RestockPricePerRound)
            {
                return;
            }
            ex.RearmBotIndex = idx;
            ex.RearmProgress = 0;
        }

        // The bot may have been planted (or the list rebuilt) since the clock started.
        if (idx >= ex.Bots.Count || ex.Bots[idx].Deployed)
        {
            ex.RearmBotIndex = null;
            ex.RearmProgress = 0;
            return;
        }

        ex.RearmProgress += dtRealSeconds / SentryBot.RearmSecondsPerMagazine;
        if (ex.RearmProgress < 1.0)
        {
            return;
        }

        RackOneMagazine(ex, idx);
        ex.RearmBotIndex = null;
        ex.RearmProgress = 0;
    }

    /// <summary>The first SHOULDERED bot that is short of a full magazine, or -1. Deployed bots are skipped
    /// on purpose: one standing out on the regolith is not in the tube being handed rounds, and pretending
    /// otherwise would be exactly the sim-says-one-thing-sentence-says-another bug this whole lane fixes.
    /// Fills in roster order, one at a time — a magazine is a timer, and one whole timer beats two short.</summary>
    private static int NextBotWantingRounds(SurfaceExcursion ex)
    {
        for (int i = 0; i < ex.Bots.Count; i++)
        {
            if (!ex.Bots[i].Deployed && ex.Bots[i].Rounds < SentryBot.MaxMagazine)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>Rack one magazine as full as the purse allows, spend the credits, and say so. The quote is
    /// the same pure Core law the haven armory uses (<see cref="SentryBot.QuoteRestock"/>) over a one-bot
    /// list — the same price seen from another door, never a second economy.</summary>
    private void RackOneMagazine(SurfaceExcursion ex, int idx)
    {
        SurfaceBot bot = ex.Bots[idx];
        SentryBot.RestockQuote quote = SentryBot.QuoteRestock([bot.Rounds], _credits);
        if (quote.RoundsBought <= 0)
        {
            return; // the purse ran dry between starting the clock and finishing it
        }

        bot.Rounds = quote.Magazines[0];
        _credits -= quote.Cost;
        RendererInterop.PlayCue("board");
        RequestVaultSave();   // rounds and purse both moved — durable before the next thing happens

        // The first time this ever happens to a captain, the card explains the tether. After that the
        // receipt is the right register: you know where the ammo comes from now.
        if (!ShowTubeRearmCardOnce())
        {
            ShowPulseMessage(
                $"🔫 {bot.Unit} racked to {SentryBot.Readout(bot.Rounds)} — {quote.Cost:N0} cr. " +
                (NextBotWantingRounds(ex) >= 0 ? "Feeding the next one." : "Both full. Back out you go."));
        }
    }

    // #563 · The world grew and the captain has read why. Same seam as CloseGroundLesson — Dismiss() hands
    // the keyboard back to the map div, which matters doubly here: this card can open mid-excursion with a
    // pack already walking toward you, and a swallowed keypress would be a death.
    private void CloseGroundGrew()
    {
        _groundGrewOpen = false;
    }

    /// <summary>
    /// #584 · <b>THE GROUND GREW, AND HERE IS WHERE — the one writer every reveal goes through.</b>
    ///
    /// <para>Owner, mid-tour: <i>"I got like one 'you expanded the map' notification in one map but I was
    /// left totally un-aware about what that did and where?"</i> Three call sites appended real ground to the
    /// live plan and every one of them raised the same card, which said WHAT at length and WHERE not at all.
    /// They go through here now, and they hand over the one fact they each already had in hand: the mouth of
    /// the ground that just joined — the forced door, the hatch, the seal that cracked.</para>
    ///
    /// <para>Two things happen with it, and they are deliberately different in lifetime. The CARD is told
    /// once, in the plate idiom the game uses for a place (<see cref="GroundGrows.Where"/>); the FAN is told
    /// for the rest of the excursion (<c>ex.NewGround</c> → <c>BuildBeacons</c>), because a card is gone in
    /// four seconds and the walk is not. That split is the whole of this fix: the sentence answers the
    /// question and the instrument keeps answering it.</para>
    ///
    /// <para>Returns whatever <see cref="ShowGroundGrewCardOnce"/> returned, so a caller that keeps a toast
    /// for every later time goes on keeping it.</para>
    /// </summary>
    /// <param name="ex">The live excursion — the fan's mark is filed on it.</param>
    /// <param name="mouthX">Where the ground joined: the door/hatch that gave, in field coordinates.</param>
    /// <param name="mouthY">The same.</param>
    private bool TheGroundJustGrew(SurfaceExcursion ex, double mouthX, double mouthY)
    {
        ArgumentNullException.ThrowIfNull(ex);

        // The instrument first, so it is already pointing when the card comes down — and unconditionally,
        // because the card is once per CAPTAIN and the second door a captain ever forces is the one they are
        // most likely to walk away from without noticing.
        ex.NewGround.Add((mouthX, mouthY, ex.Floor));

        _groundGrewWhere = GroundGrows.Where(
            ex.Stop.Body.Id, ex.Floor, mouthX - _avatarX, mouthY - _avatarY);

        return ShowGroundGrewCardOnce();
    }

    /// <summary>#563 · Raise the map-just-grew card, but only ever once per captain. Reached through
    /// <see cref="TheGroundJustGrew"/> from every path that appends real ground to the live plan (a forced
    /// expedition door, an outpost hatch, Vantar's concealed lab door).
    ///
    /// <para>Returns true when the card went up, so the caller can keep its toast for every later time —
    /// the card explains the rule to someone who has never seen it, and the toast is exactly right for
    /// someone who has. Saving immediately is deliberate: the one-time bit must be durable the instant it
    /// is spent, the same habit the convergence reveal uses.</para></summary>
    private bool ShowGroundGrewCardOnce()
    {
        if (_groundGrewSeen)
        {
            return false;
        }
        _groundGrewSeen = true;
        _groundGrewOpen = true;
        RequestVaultSave();
        StateHasChanged();
        return true;
    }

    // #562 · The captain has read what the tube does. Same Dismiss() seam — the keyboard goes back to the
    // map div, which matters here because the card fires INSIDE the tube, i.e. the moment before a captain
    // means to walk back out into whatever they retreated from.
    private void CloseTubeRearm()
    {
        _tubeRearmOpen = false;
    }

    // #573 · The captain has read what the tank is doing. Dismiss() hands the keyboard back — and here that
    // matters more than anywhere: this card opens while the air is already going, so a swallowed keypress
    // is spent air.
    private void CloseAirCard()
    {
        _airCardOpen = false;
    }

    /// <summary>#585 · Walk a body out of solid mass it has ended up inside — a wall that was built around
    /// it rather than one it walked into. Tries short steps outward on a ring of bearings and takes the first
    /// that is open ground; gives up rather than loop, because a contact stuck in stone is a curiosity and a
    /// frame that never ends is a crash.</summary>
    private static (double X, double Y) ExtricateFromStone(
        double x, double y, IReadOnlyList<SurfaceCollision.Segment> walls, double radius)
    {
        if (!SurfaceCollision.Blocked(x, y, radius, walls))
        {
            return (x, y);
        }

        for (double reach = 1.5; reach <= 18.0; reach += 1.5)
        {
            for (int i = 0; i < 12; i++)
            {
                double a = i / 12.0 * Math.Tau;
                double tx = x + (Math.Cos(a) * reach), ty = y + (Math.Sin(a) * reach);
                if (!SurfaceCollision.Blocked(tx, ty, radius, walls))
                {
                    return (tx, ty);
                }
            }
        }
        return (x, y);
    }
}

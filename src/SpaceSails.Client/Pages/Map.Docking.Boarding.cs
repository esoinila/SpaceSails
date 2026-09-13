using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using SpaceSails.Client;
using SpaceSails.Client.Layout;
using SpaceSails.Client.Rendering;
using SpaceSails.Contracts;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #313 · THE BOARDING PANEL — destination first, then the OPTIONAL chest. Picking a landable surface
/// opens this panel: choose the site, choose the coin, muster the sentries, and walk down.
///
/// <para>Boarding empty-handed is a complete visit — the chest is cargo, not a confession — and the
/// hold is packed by the one Core <c>Pack</c> the quick shortcut shares, so the two roads cannot
/// disagree about what went into the crate.</para>
///
/// <para>Split out of <c>Map.Docking.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // ── #313: destination-first boarding. Picking a landable surface opens the boarding panel: an
    //    OPTIONAL 'load a chest' step (cargo, not a confession), then walk down. Boarding empty-handed is
    //    a complete visit. The chest is the chosen coin + the whole (small) hold, packed by the one Core
    //    Pack the shortcut shares. ──
    private ShuttleStop? _boardTarget;
    private int _boardCoin;
    private int _boardBots;   // #314: how many ship sentries to load as surface escorts (0..roster)

    // #348 (owner, 2026-07-18 playtest): the Board button commits to whatever the dialog shows — one
    // button, no "empty-handed" twin. Boarding with a literally empty sling (no coin, no cargo) is a
    // legitimate fishing expedition, so we don't block it — we just ask once. True while that "are you
    // sure?" prompt is up, replacing the normal action row in the same card.
    private bool _boardEmptyConfirm;

    // #320 · the destination's seeded landing sites (2–4) and the captain's pick, live while the boarding
    // panel is open. The picked site's LayoutSalt parameterizes the surface ground and its Name rides the
    // header. Default is site 0 (the Wild Plain, the canon ground) — or the cheat-forced index.
    private IReadOnlyList<LandingSite> _boardSites = [];
    private int _boardSiteIndex;

    // #320 dev cheat (/map?site=N): force the boarding panel to pre-select landing site N so a playtester
    // can jump straight to a specific ground (site A vs B → visibly different deck-plan). Clamped to the
    // body's real set in OpenBoardingPanel. Null = no override (default to site 0). See docs/testing-guide.md.
    private int? _forcedSiteIndex;

    private void OpenBoardingPanel(ShuttleStop stop)
    {
        _boardTarget = stop;
        _boardCoin = 0;               // #313: presume NOTHING — you are not declaring a plan
        // #348: droids default to ALL aboard (owner: "Why would I ever go without both droids to keep me
        // safe. It should be so by default"). The −/+ stepper stays for the odd case, but every available
        // sentry rides down unless the captain drops one.
        _boardBots = AvailableBots;
        _boardEmptyConfirm = false;
        // #320: the destination's seeded landing-site board. An away-expedition site or the deflection rock
        // is a single AUTHORED ground (its gig owns the site), so it offers only site 0 — no chooser; every
        // ordinary landable body offers its full 2–4 seeded set.
        _boardSites = IsSpecialSiteBody(stop.Body.Id)
            ? [LandingSites.At(stop.Body.Id, 0)]
            : LandingSites.For(stop.Body.Id);
        _boardSiteIndex = _forcedSiteIndex is { } forced ? Math.Clamp(forced, 0, _boardSites.Count - 1) : 0;
        _shuttleBayStops = null;      // the panel replaces the board
    }

    // #320: a body whose ground is a single authored gig site (an away-expedition rock or the inbound
    // deflection rock) — those never present the multi-site board (the gig chose the ground).
    private bool IsSpecialSiteBody(string bodyId) =>
        (_expedition is { } gig && gig.SiteBodyId == bodyId)
        || (_deflection is { } dgig && dgig.RockBodyId == bodyId);

    private void SelectBoardSite(int index) =>
        _boardSiteIndex = Math.Clamp(index, 0, _boardSites.Count - 1);

    private void CancelBoarding()
    {
        _boardTarget = null;
        _boardEmptyConfirm = false;
        // #319 · …and the pick goes back in the coat. A chooser reopened on tomorrow's satchel holding
        // yesterday's answer would be the dialog remembering a world that has walked off — CloseSatchel's
        // own reason for putting the load chooser away, one panel over.
        _boardDeposit = null;
    }

    // ── #319 · THE SECOND CHOICE ─────────────────────────────────────────────────────────────────────────
    //
    // Owner, live 2026-07-18: "Let's add another options to hide onto the planet / site. Anything from the
    // inventory that is light enough … hiding evidence of our piracy without losing it. Maybe we steal
    // something too important to risk carrying it around, like a superchip prototype etc."
    //
    // It goes on THIS panel — the one that already packs the chest — and not on a chooser of its own, because
    // the whole issue is that there is no second flow: the same walk, the same DIG HERE press, the same 2D6,
    // the same ✗, the same return dig under the same odds. What the panel gains is a row. What the ground
    // gains is nothing at all, which is the point.

    /// <summary>#319 · The one thing out of the satchel this trip is carrying down to bury, or null. A PICK,
    /// not a removal — the row stays in the pocket for the whole walk and only the shovel spends it.</summary>
    private Core.Satchel.Item? _boardDeposit;

    /// <summary>#319 · What the chooser may offer: everything in the pocket light enough to carry down the
    /// tube, weighed by the ONE rule (<see cref="CacheDeposit.LightEnoughIn"/>, which reads the satchel's own
    /// <c>SpaceCostOf</c> and never a list of kinds). Empty pocket, empty row — the panel simply does not
    /// grow one.</summary>
    private IReadOnlyList<Core.Satchel.Item> BuriableInTheSatchel => CacheDeposit.LightEnoughIn(_satchel);

    /// <summary>#319 · Is this row the one the trip is carrying down? Matched on kind and id — the satchel's
    /// own identity for a row — so a stack that is spent down from six rounds to two is still the pick.</summary>
    private bool BoardDepositIs(Core.Satchel.Item item) =>
        _boardDeposit is { } picked && picked.Kind == item.Kind
        && string.Equals(picked.Id, item.Id, StringComparison.Ordinal);

    /// <summary>#319 · Pick a thing to bury, or un-pick the one already picked. ONE at a time and deliberately
    /// so: the hole is a decision about a single object the captain has decided is safer in the ground than on
    /// him, and a multi-select would turn the panel into a packing exercise. Pressing the picked row again is
    /// how it is taken back — the general UI law's "nothing you cannot undo", inside a row.</summary>
    private void ChooseBoardDeposit(Core.Satchel.Item item) =>
        _boardDeposit = BoardDepositIs(item) ? null : item;

    private void AdjustBoardCoin(int delta) => _boardCoin = Math.Clamp(_boardCoin + delta, 0, _credits);

    private void SetBoardCoin(int amount) => _boardCoin = Math.Clamp(amount, 0, _credits);

    // #314: the sentries in the roster right now (each with its magazine), and the load-count stepper.
    private int AvailableBots => _shipBots.Count;

    // #324: the muster line, in-voice, so the boarding panel plainly SAYS the bots stand ready (the
    // owner's "where are my bots" — they were aboard, just never named). Names the actual roster.
    private string BotMusterLine => _shipBots.Count switch
    {
        0 => "Sentry bots — none aboard (rearm at a haven)",
        1 => $"{_shipBots[0].Unit} rides escort by default",
        _ => $"{string.Join(" and ", _shipBots.Select(b => b.Unit))} ride escort by default",
    };
    private void AdjustBoardBots(int delta) => _boardBots = Math.Clamp(_boardBots + delta, 0, AvailableBots);

    // A short read of what would go down: e.g. "K-77 (99), R-3B (07)". Empty when none loaded.
    private string LoadedBotsSummary() => _boardBots <= 0
        ? ""
        : string.Join(", ", _shipBots.Take(_boardBots).Select(b => $"{b.Unit} ({SentryBot.Readout(b.Rounds)})"));

    // #348: ONE Board button. Board with whatever the dialog shows — coin + the whole small hold, if any.
    // If the sling is literally empty (no coin, no cargo) it's a fishing expedition, still a first-class
    // trip, so we don't forbid it — we raise the one-time "are you certain?" prompt and let the captain
    // confirm. Anything loaded boards straight away.
    private async Task TryBoard(ShuttleStop stop)
    {
        // #319 · …and a thing picked out of the satchel is a LOADED sling. The "are you certain?" prompt is
        // about walking down with nothing to do, and a captain carrying a file he has decided is safer under
        // a rock than in his coat has the most deliberate errand in the game — being asked whether he is sure
        // he did not mean to bring money would be the panel reading his trip back to him wrong.
        if (_boardCoin <= 0 && _cargoUnits <= 0 && _boardDeposit is null)
        {
            _boardEmptyConfirm = true; // empty sling — ask once before the walk down
            return;
        }
        await ConfirmBoarding(stop);
    }

    // Land: pack whatever (if anything) was loaded and grow the tube in place. The hold rides as cargo
    // and only leaves the ship's books if it actually goes into the ground at a dig. An empty pack is a
    // valid fishing expedition (the caller already confirmed it).
    private async Task ConfirmBoarding(ShuttleStop stop)
    {
        var hold = _cargoByClass.Where(kv => kv.Value > 0)
            .Select(kv => new CacheCargo(kv.Key, kv.Value, IsHotClass(kv.Key)))
            .ToList();
        // #319 · …and the thing out of the coat rides down on the SAME load. Pack weighs it against the one
        // rule on the way past (CacheDeposit.IsLightEnough), so the pure builder both boarding routes go
        // through is where a thing too heavy for the tube stops being a thing that can be buried.
        ShuttleExcursion.ChestLoad chest = ShuttleExcursion.Pack(_boardCoin, _credits, hold, _boardDeposit);
        _boardDeposit = null;
        _boardEmptyConfirm = false;
        // #320: carry the picked landing site down — its salt seeds the ground, its name the header.
        LandingSite site = _boardSites.Count > 0
            ? _boardSites[Math.Clamp(_boardSiteIndex, 0, _boardSites.Count - 1)]
            : LandingSites.At(stop.Body.Id, 0);
        await BeginSurfaceExcursion(stop, chest, _boardBots, site); // #314: bring the loaded sentries down too
    }

    // #327 the quote before you board DOWN: the ship states its hold honestly from the live tank — "can
    // hold this orbit ~N on the tank" — so boarding down with a short clock is the captain's INFORMED
    // choice (the owner's Miranda maroon, warned instead of silent). A ship riding a berth carries no
    // orbit risk; a kept orbit quotes pulses ÷ the Lab-25 trim rate; an unkept one says so plainly.
    private string BoardingHoldQuote()
    {
        if (_dockedHavenId is not null)
        {
            return OrbitHold.DockedComms; // #331 follow-up: the station holds it, no fuel spent on keeping
        }
        double hold = _orbitKept ? OrbitHold.HoldSeconds(_reactionMassPulses, _keepTrimPulsesPerDay) : 0;
        return OrbitHold.BoardingQuote(_orbitKept, hold);
    }

    // #327: how loudly the boarding panel paints the hold quote — red when the ship isn't holding this
    // orbit (it will drift), calm when it's berthed or truly holding. The captain sees the risk before
    // committing to the walk down.
    private int BoardingHoldSeverity() => _dockedHavenId is null && !_orbitKept ? 2 : 0;
}

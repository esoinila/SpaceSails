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

// Subject: part of Map.Quests — #223 buried treasure: the rumour map you buy, the chests in the ground, the watch that loses them, and the parcel off a back-room shelf.
public partial class Map
{

    // The standalone rumour-map purchase (deliverable 5): a barfly sells a map to some NPC's forgotten
    // hoard, dice-priced. Buying LEARNS the cache (the same dig path as our own chests) — no delivery
    // obligation, the loot is ours to keep. Skips the buy if the purse can't cover the asking price.
    private bool BuyRumorMap(string drawKey)
    {
        RumorMaps.Rumor rumor = RumorMaps.Generate(drawKey);
        if (_credits < rumor.PriceCredits)
        {
            ShowPulseMessage($"🗺 A rumour map's on the table — {rumor.PriceCredits:N0} cr — but the purse won't cover it.");
            return false;
        }
        _credits -= rumor.PriceCredits;
        _caches.Learn(rumor.Cache);
        RendererInterop.PlayCue("reveal");
        ShowPulseMessage($"🗺 Bought a rumour map for {rumor.PriceCredits:N0} cr — {rumor.Cache.Owner}'s hoard on {BodyName(rumor.Cache.BodyId)}. Dig it up before someone else does.");
        return true;
    }

    // ---- #223 buried treasure: the shuttle door's second life ----
    // A landable moon/asteroid grows two doors on the shuttle-bay pop-up: ⛏ Bury (sink coin/cargo off
    // the ship) and 🗺 Dig (lift a known cache). Both fly the shuttle DOWN and back — the mothership
    // loiters, the clock advances by the crossing (heat bleeds, traffic drifts), the ship never
    // relocates. Buried loot lives in _caches, never in _credits/_cargoByClass, so a confiscation that
    // reads only carried goods can never see it. X always marks the spot.

    // A body you can put a chest on: a moon (a surface to walk), never a station or planet. (Now also
    // encoded as the pure ShuttleExcursion.IsLandableSurface, which the destination board uses.)
    private bool IsLandableForCache(CelestialBody b) => b.Kind == BodyKind.Moon;

    // #313 retired the intent-first bury/dig chooser (OpenBuryChooser/ConfirmBury/DigAt and the old
    // LandToBury/LandToDig). The single door now is destination-first: board a surface (Map.Docking's
    // OpenBoardingPanel), walk down, and the intentions live on the ground, contextually (Map.Surface).

    // 2026-07-18 playtest: "Into the ledger" (and the backdrop click) closed the card but left focus on the
    // button, so the desk hotkeys went dead. Close AND hand the keyboard back to the map div (the one idiom).
    private async Task DismissMapCard()
    {
        _treasureMapCard = null;
        await RefocusMap();
    }

    // The map card's big image slot (Map.razor → .tm-art, behind the red .tm-x). When the grok image lane
    // has delivered a per-body asset (docs/FridaySecondPlan/hoard-image-manifest.md) we point at
    // art/treasure-<bodyId>.jpg; the deterministic per-body gradient stays layered UNDER it as the fallback
    // (so a missing/404 asset — or any body without art yet — still reads as a tinted card, Phobos always
    // the same tint). background-size: cover lives in .tm-art and applies to both layers.
    //
    // #528: WHICH bodies are painted now lives in Core (TreasureMapArt) rather than in a private set here.
    // It was a client-only literal whose own comment named miranda as the example of a body with no art —
    // while art/treasure-miranda.jpg sat finished in wwwroot the whole time. The gradient fallback is what
    // made that invisible, the same way the onerror-hide law hides a missing plate; the list has to be
    // somewhere a test can read it, and TreasureMapArtIsWiredTests now checks it in BOTH directions.
    private static string TreasureMapArtCss(string bodyId)
    {
        int h = Math.Abs(bodyId.GetHashCode());
        int hue = h % 360;
        string gradient = $"radial-gradient(circle at 38% 32%, hsl({hue}, 40%, 34%), hsl({(hue + 28) % 360}, 45%, 12%) 70%)";
        return TreasureMapArt.ArtFile(bodyId) is { Length: > 0 } art
            ? $"url('{art}'), {gradient}"
            : gradient;
    }

    // ---- The discovery watch (ruling 4): rivals find our hoards on a slow roll ----

    // Start the watch at the current day the first time we bury, so a just-dug chest isn't rolled for
    // the partial current day. Also called after a vault load, which is how a save written before the
    // watch was persisted (LastCheckedPeriod = WatchNotStarted) starts watching again from the moment
    // the captain wakes — never from the epoch, which would resolve every day at once.
    private void SeedDiscoveryWatch()
    {
        if (_caches.LastCheckedPeriod < 0)
        {
            _caches.LastCheckedPeriod = DiscoveryRule.PeriodIndex(SimTime);
        }
    }

    // Resolve the per-cache discovery roll across every whole day elapsed since the last check (so a
    // warp that skips days can't skip a roll). A found cache is GONE — a ledger squawk marks the loss.
    private void RunCacheDiscoveryWatch()
    {
        long lastChecked = _caches.LastCheckedPeriod;
        if (lastChecked < 0)
        {
            return; // nothing buried yet
        }
        long nowPeriod = DiscoveryRule.PeriodIndex(SimTime);
        if (nowPeriod <= lastChecked)
        {
            return;
        }
        foreach (TreasureCache c in _caches.Caches.Where(c => c.PlayerOwned).ToList())
        {
            // Never roll a cache for days before it was in the ground: start its scan at the later of
            // the global last-check and its own burial day.
            long from = Math.Max(lastChecked, DiscoveryRule.PeriodIndex(c.BuriedSimTime));
            // #295: a Reever-haunted stash is harder for rivals to work — the watchdogs guard it too.
            if (DiscoveryRule.DiscoveredWithin(c.Id, from, SimTime, c.ReeverLevel) is not null)
            {
                _caches.Remove(c.Id);
                RendererInterop.PlayCue("alarm");
                ShowPulseMessage($"🏴‍☠️ Someone dug up our chest on {BodyName(c.BodyId)} — {c.ContentsLine()} gone. Split the hoards next time.");
            }
        }
        _caches.LastCheckedPeriod = nowPeriod;
    }

    // Knock on a locked station hatch (a ring department or a bar back-room). Nobody answers — for now.
    // Each hatch carries an id in its label (e.g. "🔒 MEDBAY · M-05") so a mission can name one to
    // open. PIN entry / mission unlock is the next layer; today every knock goes unanswered.
    // Lift the fence's package off the back-room shelf (PR-F, the indoor quest that uses the grown
    // room). The stash console only exists once the wing is welded on, so reaching it proves the room
    // exists — the quest gates on the room, as the room gated on the crack. Advances the crack job
    // from Active to PickedUp (the pickup that a plain lockup did at the keypad); hand-off is unchanged.
    private void LiftStash()
    {
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.Stash })
        {
            return;
        }
        Quest? job = _quests.FirstOrDefault(q =>
            q.Kind == QuestKind.Crack && q.SourceBodyId == _dockedHavenId
            && _dockedHavenId is { } st && HavenInterior.HatchGrowsWing(st, q.TargetShipId));
        if (job is { State: QuestState.Active })
        {
            RendererInterop.PlayCue("board");
            // #727/#736 · A step the CHAIR issued, finished on foot at a shelf in a back room — so it goes
            // through the one writer, and the beat is told wherever the captain's eye actually is rather
            // than on a HUD banner that a card or an open satchel would be sitting on top of.
            AdvanceMission(job, QuestState.PickedUp,
                "You peel the package from behind the shelf and pocket it. Now get it back to the Fixer. 📦");
        }
        else if (job is not null)
        {
            ShowPulseMessage("The shelf's bare now — you already lifted what was here.");
        }
        else
        {
            ShowPulseMessage("Dusty shelving and a cold draught. Nothing here worth pocketing — unless someone sent you for it.");
        }
    }

    // #223: the captain's hoards — every buried chest we know of, ours and any rival's whose map we
    // hold. Buried loot lives HERE, never in _credits / _cargoByClass, so a boarding confiscation
    // (which reads only carried goods) can never see it. The map card and the ledger's 🗺 section
    // read this book; the discovery watch prunes it.
    private readonly CacheLedger _caches = new();

    // The discovery watch's bookmark (ruling 4) — the last whole day we resolved the per-cache discovery
    // roll, so a warp that skips days can't skip a roll — now lives ON THE LEDGER
    // (CacheLedger.LastCheckedPeriod), because a hoard and the clock that threatens it are ONE fact and
    // the vault has to carry both. As a private field beside the ledger it was never saved: reload the
    // page, Resume a voyage with chests in the ground, and the watch came back at -1 —
    // RunCacheDiscoveryWatch bailed on "nothing buried yet" and no rival ever dug anything up again,
    // however many days you flew. The line the game prints at the shovel ("rivals may dig it up over the
    // coming days") stopped being true the moment you saved.

    // The treasure-map card currently on screen (the full-screen artifact), or null. Shown on burying
    // a fresh chest and any time the captain opens a map from the ledger's 🗺 section.
    private TreasureCache? _treasureMapCard;
}

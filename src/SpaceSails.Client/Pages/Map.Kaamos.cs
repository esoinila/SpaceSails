using System.Globalization;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Kaamos — #411 DELIVER THE FRAGMENTS. The spine (KaamosLore / KaamosProgress, #416) is pure Core;
// this partial is the world-side wiring that lets a player actually ASSEMBLE each shard, see the mystery
// build in the Captain's ledger, and hear the one-time notice when the berth-code resolves. Each delivery
// SITE lives with its own system (the plaque in Map.Deck, the cold pod in Map.Surface, Vantar's log in
// Map.SecretLab, the holder/coordinate/capstone at the bar in Map.Quests); everything they share — the
// per-thread progress holder, the assemble-and-narrate helper, the ledger readout, the reach notice, and
// the ?kaamos test cheat — lives here.
public partial class Map
{
    // The per-game-thread assembly of PROJEKTI KAAMOS (the CacheLedger/ContactLedger idiom): which shards
    // this universe's captain has gathered. Wiped on a new voyage (ResetLiveStateForNewGame), round-tripped
    // through the vault's KaamosSection (BuildVault / ApplyVault), all in Map.Vault.
    private readonly KaamosProgress _kaamos = new();

    // The loud, one-time notice appended the instant the reach opens — the berth-code AND the legitimising
    // intel both in hand (KaamosLore.CanReachEnceladus). The ACTUAL route is a separate lane
    // (feat/kaamos-route); here we announce the window and gate a "route pending" line, nothing more.
    private const string KaamosReachNotice =
        "   ❄❄ THE BERTH-CODE RESOLVES — Enceladus can be reached. The window is real. " +
        "(The cycler route is still being charted; when it opens, a ship that's on the board rides it in. " +
        "For now: route pending.)";

    /// <summary>Assemble a KAAMOS fragment, persist it, and narrate the find. Returns true only on the
    /// first time this shard is held, so a caller narrates a genuinely NEW find and a re-read stays quiet.
    /// The reach-opens notice is appended on the single edge that flips CanReachEnceladus (assembling the
    /// capstone with enough intel behind it), so it fires exactly once per universe and never on reload
    /// (a load re-hydrates the set without re-assembling).</summary>
    private bool TryAssembleKaamos(string fragmentId, string foundMessage)
    {
        if (_kaamos.Has(fragmentId))
        {
            return false; // already gathered — the caller falls through to its own quiet re-read line
        }

        bool couldReachBefore = _kaamos.CanReachEnceladus;
        if (!_kaamos.Assemble(fragmentId))
        {
            return false; // not a real pool id — refused by the spine, never a phantom in the set
        }

        RequestVaultSave(); // a shard gathered is durable — save on the change (Map.Vault autosave)
        string tail = !couldReachBefore && _kaamos.CanReachEnceladus ? KaamosReachNotice : "";
        RendererInterop.PlayCue(tail.Length > 0 ? "reveal" : "board");
        ShowPulseMessage(foundMessage + tail);
        MaybeFireConvergence(); // #422: an arc-1 shard may be the edge that crosses the JOINT threshold too
        return true;
    }

    // ── The intel readout (deliverable: make progress VISIBLE). Reuses the Captain's-ledger tip idiom:
    //    a single evergreen "PROJEKTI KAAMOS — N of 5 shards" card whose lines are the assembled fragment
    //    texts, so the mystery builds as the player collects and the shards stay re-readable. Returns null
    //    until the first shard is in hand (nothing to show on a fresh universe). ──
    private Stations.Captain.LedgerTip? KaamosLedgerTip()
    {
        if (_kaamos.Count == 0)
        {
            return null;
        }

        int intel = _kaamos.IntelAssembled;
        int need = KaamosLore.IntelFragments.Count();
        bool hasKey = _kaamos.Has(KaamosLore.KeyFragment.Id);

        var lines = new List<string>();
        foreach (KaamosFragment f in _kaamos.Assembled)
        {
            lines.Add($"◆ {f.Title} — {f.Lore}");
        }

        if (_kaamos.CanReachEnceladus)
        {
            lines.Add("❄ The berth-code resolves. Enceladus can be reached — the cycler window is real. (Route pending: the way in is still being charted.)");
        }
        else if (_kaamos.HasEnoughIntelToEarnTheKey)
        {
            lines.Add("❄ Enough intel to earn the berth-code. Ask around the bars — the pieces resolve into one number the sealed berth still listens for.");
        }
        else
        {
            lines.Add($"❄ The shape isn't clear yet — {need - intel} more shard{(need - intel == 1 ? "" : "s")} to see it. A plaque line alone is never enough; one lone rumor is never enough.");
        }

        string headline = hasKey
            ? $"❄ PROJEKTI KAAMOS — {intel} of {need} shards · berth-code in hand"
            : $"❄ PROJEKTI KAAMOS — {intel} of {need} shards assembled";

        return new Stations.Captain.LedgerTip(
            headline, lines.ToArray(), "the sealed ice-moon mystery",
            ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null);
    }

    // ── The bar seam (holders-tell · bought-coordinate · the berth-code capstone). One discoverable action
    //    on the barkeep card (Map.razor) that walks the KAAMOS ladder a step at a time. The button only
    //    shows when there's a step to take (KaamosBarSeamAvailable), so it never clutters an ordinary bar. ──

    // Which KAAMOS step this bar can offer right now, or null if none. Priority: the rare holder's tell
    // first (it's seeded to THIS bar this watch and would otherwise slip past), then the earned capstone,
    // then the buyable coordinate (available at any bar once the thread has begun).
    private string? KaamosBarNextStep()
    {
        if (!_deckMode || CurrentKeep is null || _dockedHavenId is null)
        {
            return null;
        }

        string bar = _dockedHavenId;
        int watchDay = (int)(SimTime / 86400);

        if (!_kaamos.Has("holders-tell") && KaamosFind.HolderAtBar(bar, watchDay))
        {
            return "holders-tell";
        }

        if (_kaamos.HasEnoughIntelToEarnTheKey && !_kaamos.Has(KaamosLore.KeyFragment.Id))
        {
            return "berth-code";
        }

        if (_kaamos.Count > 0 && !_kaamos.Has("bought-coordinate"))
        {
            return "bought-coordinate";
        }

        return null;
    }

    private bool KaamosBarSeamAvailable() => KaamosBarNextStep() is not null;

    // The barkeep-card "🌑 Ask about KAAMOS" action: advance the thread by one step. The holder shares the
    // tell for free (a nod across the room); the coordinate costs a round on the counter; the capstone is
    // the pieces answering each other at the table — no coin, the earned last piece.
    private void AskAboutKaamos()
    {
        switch (KaamosBarNextStep())
        {
            case "holders-tell":
                TryAssembleKaamos("holders-tell",
                    "🌑 The barkeep tips their chin at a lone drinker in the corner — the one who used to run that berth. " +
                    KaamosLore.ById("holders-tell")!.Lore);
                Overhear("👂 The KAAMOS berth-holder: \"You don't file for that berth. You keep it.\"", "kaamos-holder");
                break;

            case "berth-code":
                TryAssembleKaamos("berth-code",
                    "❄ You spread what you've gathered across the table and the numbers answer each other. " +
                    KaamosLore.KeyFragment.Lore);
                break;

            case "bought-coordinate":
                int price = KaamosFind.BoughtCoordinateCredits;
                if (_credits < price)
                {
                    ShowPulseMessage($"🌑 There's a coordinate for sale — {price:N0} cr for the where and the when — but the purse won't cover it.");
                    return;
                }

                _credits -= price;
                TryAssembleKaamos("bought-coordinate",
                    $"🌑 A round on the counter (−{price:N0} cr) buys the rest of it. " +
                    KaamosLore.ById("bought-coordinate")!.Lore);
                break;

            default:
                ShowPulseMessage("🌑 You put the word out, quiet as you can. Nobody's talking about the ice moon today.");
                break;
        }
    }

    // ── The test cheat: /map?kaamos=N assembles the first N fragments (canonical order), /map?kaamos=all
    //    assembles every one — so the readout, its state transitions, and the reach notice are all reachable
    //    without a full playthrough. Documented in docs/features/KaamosPlotline.md and the testing guide. ──
    private void SeedKaamosCheat(string spec)
    {
        int count = string.Equals(spec, "all", StringComparison.OrdinalIgnoreCase)
            ? KaamosLore.Fragments.Count
            : int.TryParse(spec, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? Math.Clamp(n, 0, KaamosLore.Fragments.Count) : 0;

        if (count <= 0)
        {
            return;
        }

        bool couldReachBefore = _kaamos.CanReachEnceladus;
        foreach (KaamosFragment f in KaamosLore.Fragments.Take(count))
        {
            _kaamos.Assemble(f.Id);
        }

        RequestVaultSave();
        string tail = !couldReachBefore && _kaamos.CanReachEnceladus ? KaamosReachNotice : "";
        ShowPulseMessage($"🧪 Test: assembled {_kaamos.Count} KAAMOS fragment{(_kaamos.Count == 1 ? "" : "s")} ({_kaamos.IntelAssembled} intel). See the Captain's ledger.{tail}");
        MaybeFireConvergence(); // #422: a big ?kaamos= may itself cross the joint bar if NEBULA is already up
    }
}

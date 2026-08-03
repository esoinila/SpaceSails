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
    // (feat/kaamos-route); the copy lives in Core with the rest of the arc's authored prose
    // (KaamosLore.ReachNotice), so the loud line and the ledger's resting line say one thing, and the
    // "no route yet" fact is told in fiction — a window that has not come round — instead of as a
    // production note ("For now: route pending", which is what it used to say out loud).

    // #411 story pass · the /map?kaamos=holder dev seat: the rare KAAMOS berth-holder is drinking at
    // WHATEVER bar this captain docks at, this watch. Session-scoped, set once at boot from the query.
    private bool _kaamosHolderCheat;

    // #411 story pass · the /map?kaamos=pod dev seat: the cold supply pod is under the ground you land on,
    // so the one fragment with no direct cheat is EARNED with a shovel rather than granted. The probe site
    // asks only while cold-pod is NOT yet held, so a forced ground yields exactly one pod, not a field.
    private bool _kaamosPodCheat;

    // #635 story pass · the /map?kaamos=bounce dev seat: the freight agent with the docket the board keeps
    // returning is at WHATEVER bar this captain docks at, this watch. Session-scoped, set once at boot.
    private bool _kaamosBounceCheat;

    /// <summary>#411 story pass · is the cold pod under THIS square? The seeded answer, or the dev seat's
    /// yes — asked in one place so the probe site (Map.Surface) stays a one-liner and the cheat cannot
    /// drift out of step with the find.</summary>
    private bool KaamosPodHere(string bodyId, int squareX, int squareY) =>
        KaamosFind.IsColdPodSquare(bodyId, squareX, squareY, forced: _kaamosPodCheat);

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
        string tail = !couldReachBefore && _kaamos.CanReachEnceladus ? KaamosLore.ReachNotice : "";
        RendererInterop.PlayCue(tail.Length > 0 ? "reveal" : "board");
        ShowPulseMessage(foundMessage + tail);

        // #528 · the beats that earn a picture get one. Three of this arc's turnings — the pod that was
        // held, the one who kept the berth, and the berth answering — used to be a toast that faded in a
        // second and a half; they are now the house reveal card as well, raised at the single seam where a
        // shard is actually assembled so it can never be shown for a shard nobody found. The other shards
        // (a line on a plaque, a log in a drawer, a coordinate bought over a counter) stay prose on
        // purpose: over-carding cheapens the ones that are not. Core owns the words (KaamosLore.PlateFor).
        if (KaamosLore.PlateFor(fragmentId) is { } plate)
        {
            ShowRevealCard(plate.Title, plate.ArtFile, plate.Caption);
        }

        MaybeFireConvergence(); // #422: an arc-1 shard may be the edge that crosses the JOINT threshold too
        return true;
    }

    // ── The intel readout (deliverable: make progress VISIBLE). Reuses the Captain's-ledger tip idiom:
    //    a single evergreen "PROJEKTI KAAMOS — N of 5 shards" card whose lines are the assembled fragment
    //    texts, so the mystery builds as the player collects and the shards stay re-readable. Returns null
    //    until the first shard is in hand (nothing to show on a fresh universe). ──
    private Stations.Captain.LedgerTip? KaamosLedgerTip()
    {
        // #635 · THE CARD USED TO WAIT FOR A SHARD. That is the front-door bug stated as code: the only
        // place in the game that says PROJEKTI KAAMOS is a thing you could be doing appeared strictly after
        // you had already started doing it. A returned filing is now enough to raise it — the captain is
        // holding a piece of paper, and a piece of paper in the pocket belongs in the ledger.
        if (_kaamos.Count == 0 && !_kaamos.BerthFilingBounced)
        {
            return null;
        }

        // Every sentence on this card is built in Core against the same predicates the GATE reads
        // (KaamosLore.LedgerHeadline / LedgerProgressLine / LedgerLoreFor). The countdown used to be
        // computed here off the size of the intel pool instead of the unlock threshold — always one shard
        // too many — and the capstone used to re-read as a fixed list of four shards whether or not this
        // captain had found them. Neither can drift now: the ledger asks the arc.
        var lines = new List<string>();
        foreach (KaamosFragment f in _kaamos.Assembled)
        {
            lines.Add($"◆ {f.Title} — {KaamosLore.LedgerLoreFor(f, _kaamos)}");
        }

        lines.Add(KaamosLore.LedgerProgressLine(_kaamos));

        return new Stations.Captain.LedgerTip(
            KaamosLore.LedgerHeadline(_kaamos), lines.ToArray(), "the sealed ice-moon mystery",
            ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null);
    }

    // ── #635 · THE FRONT DOOR. The one beat in this arc that a captain who knows nothing can meet, and the
    //    only one that asks for nothing first. A freight agent at a bar is holding a docket that keeps
    //    coming back; they will pay a small fee for you to try filing it under your hull, because a
    //    fourth-hand attempt is cheaper than an answer. It bounces. The bounce is the hook.
    //
    //    Why it rides the CONTRACT card rather than a new modal: the offer/accept/pass machinery already
    //    exists, is already how this game hands a captain a piece of work, and puts the price on the same
    //    card as the button (the #634 lesson). And it is the shape #635's option 3 asked for — "a
    //    mission-desk contract points a delivery AT the sealed berth, which bounces" — with the important
    //    difference that the captain is told, before they agree, that the thing has bounced four times
    //    already. A job that evaporates after you take it is a bug wearing a story; a filing you are paid
    //    to attempt is a job that did exactly what it said.
    //
    //    It is NOT a fragment and hands over no shard: the pool is what the gate counts. What it hands over
    //    is the ledger card, which is the thing the arc has never had. ──

    /// <summary>The stable id the accept path watches for. One string, in one place, because a contract
    /// that is intercepted by its id and minted somewhere else is two sources of truth for one fact.</summary>
    private const string KaamosBounceOfferId = "kaamos-bounce";

    /// <summary>The returned-filing offer for this bar and watch, or null if there is nobody holding one.
    /// Offered only to a captain who has NOTHING of the arc yet: it is a door, and a door in the middle of
    /// a corridor you are already walking is furniture.</summary>
    private Quest? MakeKaamosBounceOffer(string giver)
    {
        if (_kaamos.BerthFilingBounced || _kaamos.Count > 0 || _dockedHavenId is not { } bar)
        {
            return null;
        }

        int watchDay = (int)(SimTime / 86400);
        if (!KaamosFind.BounceAtBar(bar, watchDay, forced: _kaamosBounceCheat))
        {
            return null;
        }

        int fee = KaamosFind.BounceFilingFee;
        return new Quest(KaamosBounceOfferId, QuestKind.CargoRun, giver,
            "", "Ringside Exchange", KaamosLore.BounceOfferTitle, KaamosLore.BounceOfferBlurb(fee), fee);
    }

    /// <summary>Put the captain's hull number on the docket and let the board answer. Pays the agreed fee,
    /// marks the thread open, and raises the card. No quest is added — there is nothing to go and do, which
    /// is the point: what the captain leaves the counter with is a returned filing and a question.</summary>
    private void TakeKaamosBounceFiling(Quest offer)
    {
        int fee = KaamosFind.BounceFilingFee;
        _credits += fee;

        if (!_kaamos.MarkBerthFilingBounced())
        {
            return; // already bounced this thread — the offer should never have been minted; stay quiet
        }

        RequestVaultSave();
        RendererInterop.PlayCue("board");
        ShowPulseMessage(KaamosLore.BounceReceipt(fee));
        ShowRevealCard(KaamosLore.BouncePlate.Title, KaamosLore.BouncePlate.ArtFile, KaamosLore.BouncePlate.Caption);

        // Durable, and in somebody's voice — the ledger's 👂 section keeps it where the captain will meet it
        // again a week later, which is when a front door has to still be standing open.
        Overhear($"👂 {GiverDisplay(offer.Giver)}: \"Held, it says. Not closed. Thirty years and I've never seen held.\"",
            "kaamos-bounce");
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

        if (!_kaamos.Has("holders-tell") && KaamosFind.HolderAtBar(bar, watchDay, forced: _kaamosHolderCheat))
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

    // What the seam's button SAYS — and, for the step that takes coin, what it costs. The button used to
    // read "🌑 Ask about KAAMOS" for every step, including the one that quietly took 1,200 cr out of the
    // purse the instant it was clicked, while every other button on the same counter printed its own price.
    private string KaamosBarSeamLabel() => KaamosLore.BarSeamLabel(KaamosBarNextStep());

    private string KaamosBarSeamTitle() => KaamosLore.BarSeamTitle(KaamosBarNextStep());

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
                // The resolution names the shards THIS captain actually holds (KaamosLore.KeyResolution).
                // The old line pasted the capstone's authored text, which credited a fixed four — so a
                // captain who reached the threshold without ever buying a coordinate was told, in the
                // biggest sentence in the arc, that the coordinate they never bought was in the answer.
                TryAssembleKaamos("berth-code",
                    "❄ You spread what you've gathered across the table. " + KaamosLore.KeyResolution(_kaamos));
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

    // ── The test cheats. /map?kaamos=N assembles the first N fragments (canonical order) and
    //    /map?kaamos=all assembles every one — so the readout, its state transitions, and the reach notice
    //    are reachable without a full playthrough. Those GRANT the shards. The two seats below let the two
    //    fragments that were otherwise unreachable on purpose be EARNED instead:
    //
    //      /map?kaamos=pod      the cold supply pod is under this landing site's ground (dig it up)
    //      /map?kaamos=holder   the rare berth-holder is drinking at whatever bar you dock at
    //
    //    (Owner's rule, written next to these cheats in Map.Sim: "a scene nobody can reach on demand is a
    //    scene that ships broken.") All four documented in docs/testing-guide.md Appendix A and
    //    docs/features/KaamosPlotline.md. ──
    private void SeedKaamosCheat(string spec)
    {
        if (string.Equals(spec, "pod", StringComparison.OrdinalIgnoreCase))
        {
            _kaamosPodCheat = true;
            ShowPulseMessage("🧪 Test: the cold KAAMOS supply pod is under this ground — land, take the metal detector out and probe any square.");
            return;
        }

        if (string.Equals(spec, "bounce", StringComparison.OrdinalIgnoreCase))
        {
            _kaamosBounceCheat = true;
            ShowPulseMessage("🧪 Test: a freight agent with the docket the board keeps returning is at every bar this run — dock, walk to a patron and press [E]. (#635, the arc's front door.)");
            return;
        }

        if (string.Equals(spec, "holder", StringComparison.OrdinalIgnoreCase))
        {
            _kaamosHolderCheat = true;
            ShowPulseMessage("🧪 Test: the KAAMOS berth-holder is drinking at every bar this run — dock, walk to the counter, and the seam offers the tell.");
            return;
        }

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
        string tail = !couldReachBefore && _kaamos.CanReachEnceladus ? KaamosLore.ReachNotice : "";
        ShowPulseMessage($"🧪 Test: assembled {_kaamos.Count} KAAMOS fragment{(_kaamos.Count == 1 ? "" : "s")} ({_kaamos.IntelAssembled} intel). See the Captain's ledger.{tail}");
        MaybeFireConvergence(); // #422: a big ?kaamos= may itself cross the joint bar if NEBULA is already up
    }
}

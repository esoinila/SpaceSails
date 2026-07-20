using System.Globalization;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Nebula — #422 DELIVER THE FRAGMENTS + WIRE THE CONVERGENCE. The spine (NebulaLore / NebulaProgress /
// ArcConvergence, #423) is pure Core; this partial is the world-side wiring that lets a player ASSEMBLE each
// NEBULA MUTUAL shard, see arc 2 build in the Captain's ledger, and — the marquee moment — feel the two
// rabbit holes CONVERGE. It mirrors Map.Kaamos exactly: the per-thread progress holder, the assemble-and-
// narrate helper, the ledger readout, the adjuster/capstone bar seam, and the ?nebula test cheat all live
// here. Each delivery SITE lives with its own system (the glitch on the resurrection card + the collector
// writ + the clinic ledger in Map.Combat; the poster fine-print in Map.Deck; the oracle leak in Map.Oracle);
// everything they share lives here.
//
// THE CONVERGENCE (issue #422's payoff) is wired here too: every arc assemble edge (KAAMOS and NEBULA both)
// funnels through MaybeFireConvergence, which watches ArcConvergence.ConvergenceRevealPending and, on the one
// edge it first flips true, fires the loud one-time reveal card and marks it seen so it never re-fires.
public partial class Map
{
    // The per-game-thread assembly of NEBULA MUTUAL (the CacheLedger/ContactLedger idiom): which shards this
    // universe's captain has gathered about what their resurrections really are. Wiped on a new voyage
    // (ResetLiveStateForNewGame), round-tripped through the vault's NebulaSection (BuildVault / ApplyVault),
    // all in Map.Vault. Born in #425 (the oracle was the first hand to give a shard); this #422 lane adopts it
    // as the shared holder, exactly as the oracle lane invited.
    private readonly NebulaProgress _nebula = new();

    // The one-time notice the instant the whole truth resolves — the capstone contract AND enough intel behind
    // it (NebulaLore.KnowsTheTruth). Announced once per universe; a reload rehydrates without re-firing.
    private const string NebulaTruthNotice =
        "   ▓▓ THE POLICY'S TRUE TERMS RESOLVE — you know what Nebula Mutual files you under now. " +
        "Not insured against death: filed under it. The premium buys STORAGE, and the original never leaves the dark.";

    /// <summary>Assemble a NEBULA fragment, persist it, narrate the find, and check the convergence — the
    /// _kaamos idiom (TryAssembleKaamos) for arc 2. Returns true only the first time a shard is held, so a
    /// caller narrates a genuinely NEW leak and a re-read stays quiet. The truth-resolves notice fires on the
    /// single edge that flips KnowsTheTruth, once per universe, never on reload; the convergence fires on the
    /// single edge BOTH arcs first cross their joint threshold.</summary>
    private bool TryAssembleNebula(string fragmentId, string foundMessage)
    {
        if (_nebula.Has(fragmentId))
        {
            return false;
        }

        bool knewBefore = _nebula.KnowsTheTruth;
        if (!_nebula.Assemble(fragmentId))
        {
            return false; // not a real pool id — refused by the spine
        }

        RequestVaultSave();
        string tail = !knewBefore && _nebula.KnowsTheTruth ? NebulaTruthNotice : "";
        RendererInterop.PlayCue(tail.Length > 0 ? "reveal" : "board");
        ShowPulseMessage(foundMessage + tail);
        MaybeFireConvergence(); // the marquee edge — checked on every arc assemble
        return true;
    }

    /// <summary>Assemble a NEBULA fragment WITHOUT the pulse/cue, for a site that renders the delivery on its
    /// own host card (the resurrection card's glitch, the collector's writ, the clinic ledger — all shown IN
    /// the BUSTED modal, where a floating pulse would hide behind it). Still persists and still checks the
    /// convergence. Returns true only on the first-held edge.</summary>
    private bool AssembleNebulaSilently(string fragmentId)
    {
        if (_nebula.Has(fragmentId) || !_nebula.Assemble(fragmentId))
        {
            return false;
        }

        RequestVaultSave();
        MaybeFireConvergence();
        return true;
    }

    // ── The intel readout (deliverable: make progress VISIBLE). Mirrors KaamosLedgerTip: a single evergreen
    //    "NEBULA MUTUAL — N of 5 clauses" card whose lines are the assembled fragment texts, so the corporate
    //    horror builds as the player collects and the shards stay re-readable. Returns null until the first
    //    shard is in hand (nothing to show on a fresh universe). Consumed in Map.Quests' Captain-ledger tips. ──
    private Stations.Captain.LedgerTip? NebulaLedgerTip()
    {
        if (_nebula.Count == 0)
        {
            return null;
        }

        int intel = _nebula.IntelAssembled;
        int need = NebulaLore.IntelFragments.Count();
        bool hasKey = _nebula.Has(NebulaLore.KeyFragment.Id);

        var lines = new List<string>();
        foreach (NebulaFragment f in _nebula.Assembled)
        {
            lines.Add($"▓ {f.Title} — {f.Lore}");
        }

        if (_nebula.KnowsTheTruth)
        {
            lines.Add("▓ The policy's true terms resolve. You are not insured against death — you are filed under it.");
        }
        else if (_nebula.HasEnoughIntelToEarnTheContract)
        {
            lines.Add("▓ Enough of the small print to earn the contract. Ask around the bars — the pieces resolve into the clause the sales voice skips.");
        }
        else
        {
            lines.Add($"▓ The shape isn't clear yet — {need - intel} more shard{(need - intel == 1 ? "" : "s")} to read it. One poster line is never enough; one adjuster's drink is never enough.");
        }

        string headline = hasKey
            ? $"▓ NEBULA MUTUAL — {intel} of {need} clauses · the contract in hand"
            : $"▓ NEBULA MUTUAL — {intel} of {need} clauses assembled";

        return new Stations.Captain.LedgerTip(
            headline, lines.ToArray(), "what your resurrections really are",
            ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null);
    }

    // ── The bar seam (adjuster-tell · the policy-terms capstone). One discoverable action on the barkeep
    //    card (Map.razor), the exact mirror of the KAAMOS seam: it only shows when there's a step to take
    //    (NebulaBarSeamAvailable), so it never clutters an ordinary bar. ──

    // Which NEBULA step this bar can offer right now, or null if none. Priority: the rare roving adjuster
    // first (they're seeded to THIS bar this watch and would otherwise slip past), then the earned capstone.
    private string? NebulaBarNextStep()
    {
        if (!_deckMode || CurrentKeep is null || _dockedHavenId is null)
        {
            return null;
        }

        string bar = _dockedHavenId;
        int watchDay = (int)(SimTime / 86400);

        if (!_nebula.Has("adjuster-tell") && NebulaFind.AdjusterAtBar(bar, watchDay))
        {
            return "adjuster-tell";
        }

        if (_nebula.HasEnoughIntelToEarnTheContract && !_nebula.Has(NebulaLore.KeyFragment.Id))
        {
            return "policy-terms";
        }

        return null;
    }

    private bool NebulaBarSeamAvailable() => NebulaBarNextStep() is not null;

    // The barkeep-card "▓ Ask about NEBULA" action: advance arc 2 by one step. The adjuster shares the tell
    // over their own drink (no coin — they're the one talking); the capstone is the pieces answering each
    // other at the table (no coin — the earned last piece).
    private void AskAboutNebula()
    {
        switch (NebulaBarNextStep())
        {
            case "adjuster-tell":
                TryAssembleNebula("adjuster-tell",
                    "▓ The barkeep nods at a tired sort nursing a policy folio in the corner — a Nebula Mutual adjuster. " +
                    NebulaLore.ById("adjuster-tell")!.Lore);
                Overhear("👂 A Nebula adjuster, deep in a drink: \"A policy's not a spare life. It's a lease — on the one you've got.\"", "nebula-adjuster");
                break;

            case "policy-terms":
                TryAssembleNebula("policy-terms",
                    "▓ You spread the small print you've gathered across the table and the clauses answer each other. " +
                    NebulaLore.KeyFragment.Lore);
                break;

            default:
                ShowPulseMessage("▓ You ask, quiet, about the fine print on the pirate-insurance. Nobody at this bar is selling policies today.");
                break;
        }
    }

    // ── THE CONVERGENCE (issue #422, the payoff). Watched on EVERY arc assemble edge (both TryAssembleKaamos
    //    and the Nebula assemble helpers call this). On the single edge ConvergenceRevealPending first turns
    //    true — enough of BOTH the ice-moon and the resurrection truth in hand — fire the loud one-time reveal
    //    card and mark it seen (persisted) so the biggest beat in the game plays once per universe and never
    //    re-fires on reload. The sanity throw is a documented hook (ArcConvergence.ConvergenceSanityShockHook)
    //    the #226 lane owns; here we deliver the WORLD beat. ──

    // The open convergence reveal card, non-null while the marquee beat is up (rendered in Map.razor as a full
    // staged reveal — the BUSTED/expedition-reveal idiom). A run/thread bit; the ONE-TIME guarantee lives in
    // the persisted NebulaProgress.ConvergenceSeen, not here.
    private bool _convergenceRevealOpen;

    /// <summary>Fire the convergence beat if it is pending and not yet seen — the one edge both rabbit holes
    /// meet. Idempotent via NebulaProgress.MarkConvergenceSeen (returns true only on the first-ever edge), so
    /// even if two assembles land in one frame the reveal shows once. Safe to call after any arc assemble.</summary>
    private void MaybeFireConvergence()
    {
        if (!ArcConvergence.ConvergenceRevealPending(_kaamos, _nebula))
        {
            return;
        }

        if (!_nebula.MarkConvergenceSeen())
        {
            return; // already seen this thread (defensive — the pending check already guards it)
        }

        RequestVaultSave();          // the one-time bit is durable — a reload never re-fires the beat
        RendererInterop.PlayCue("reveal");
        _convergenceRevealOpen = true;
        StateHasChanged();
    }

    private void CloseConvergenceReveal()
    {
        _convergenceRevealOpen = false;
        StateHasChanged();
    }

    // ── The test cheats. /map?nebula=N assembles the first N fragments (canonical order), /map?nebula=all
    //    assembles every one — so the readout, its state transitions and the truth notice are reachable
    //    without a full playthrough. /map?converge=1 seeds ENOUGH OF BOTH arcs to fire the convergence, the
    //    marquee moment, for a single-URL smoke test. Documented in docs/testing-guide.md. ──
    private void SeedNebulaCheat(string spec)
    {
        int count = string.Equals(spec, "all", StringComparison.OrdinalIgnoreCase)
            ? NebulaLore.Fragments.Count
            : int.TryParse(spec, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? Math.Clamp(n, 0, NebulaLore.Fragments.Count) : 0;

        if (count <= 0)
        {
            return;
        }

        bool knewBefore = _nebula.KnowsTheTruth;
        foreach (NebulaFragment f in NebulaLore.Fragments.Take(count))
        {
            _nebula.Assemble(f.Id);
        }

        RequestVaultSave();
        string tail = !knewBefore && _nebula.KnowsTheTruth ? NebulaTruthNotice : "";
        ShowPulseMessage($"🧪 Test: assembled {_nebula.Count} NEBULA fragment{(_nebula.Count == 1 ? "" : "s")} ({_nebula.IntelAssembled} intel). See the Captain's ledger.{tail}");
        MaybeFireConvergence(); // a big ?nebula= may itself cross the joint bar if KAAMOS is already up
    }

    // /map?converge=1 — assemble just enough of BOTH arcs to fire the convergence and nothing more, so Fable
    // (and any smoke test) can verify the marquee beat from one URL. Uses each side's joint threshold exactly.
    private void SeedConvergeCheat()
    {
        foreach (KaamosFragment f in KaamosLore.IntelFragments.Take(ArcConvergence.KaamosSideThreshold))
        {
            _kaamos.Assemble(f.Id);
        }

        foreach (NebulaFragment f in NebulaLore.IntelFragments.Take(ArcConvergence.NebulaSideThreshold))
        {
            _nebula.Assemble(f.Id);
        }

        RequestVaultSave();
        ShowPulseMessage($"🧪 Test: seeded {_kaamos.IntelAssembled} KAAMOS + {_nebula.IntelAssembled} NEBULA intel — the rabbit holes are about to cross.");
        MaybeFireConvergence();
    }
}

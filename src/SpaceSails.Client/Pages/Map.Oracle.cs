using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Oracle — #425 THE STATION ORACLE. Solenne "Static" Marsh, the ranting-drunk oracle whose nonsense
// hides the truth (owner 2026-07-20, the BSG "Base Star Hybrid" key: "among that rant they could say
// things that sound nuts but are true"; and the tone ruling: she perceives reality on extra channels — she
// SEES x-rays, TASTES cosmic dust, HEARS the archived dead — so a true line lands because she literally
// perceives it on a channel the sane can't). The pure rant generator + true-line + fragment mechanism is
// Core (OracleRant); this partial is the world-side wiring: her corner interaction (routed here from the
// BarPatron E-path in Map.Quests, exactly as the Magpie is), buying her a drink to loosen the channel
// ("a drunk oracle is more prophetic"), and DELIVERING an arc shard when a true-line surfaces one.
//
// She leaks from BOTH arcs. KAAMOS (#411) already has its client holder (_kaamos, Map.Kaamos); NEBULA
// (#422) is fully built in Core + Vault but had no client delivery vector yet — the oracle is its first,
// so this file also owns the minimal client-side _nebula holder, its assemble-and-narrate helper, its
// ledger readout and its vault round-trip. A later dedicated Nebula-delivery lane can adopt/extend these.
public partial class Map
{
    // ── The oracle's corner interaction ──────────────────────────────────────────────────────────────

    // A tiny slice of per-visit session state (no new persistence — the coordinator's "trivially cheap
    // through existing session state", the same footing as the bar-visit round/tongues state). A new berth
    // (or undock) starts a fresh reading; re-docking the same bar keeps it, which is fine.
    private string? _oracleStation;   // which docked bar this reading belongs to
    private int _oracleDraw;          // advancing line index — every listen turns the dial
    private int _oracleDrinks;        // drinks stood the oracle this visit — raises the true chance
    private OracleLine? _oracleLine;  // the line currently on the card
    private bool _oracleOpen;         // her corner card is up
    private string? _oracleNotice;    // the last aside (a refused glass, an empty purse), shown on the card

    // Fold the reading to the current berth: a new (or no) bar wipes the dial, the drinks and the last line,
    // so a drunk oracle at one port doesn't stay prophetic at the next.
    private void EnsureOracleVisit()
    {
        if (_oracleStation != _dockedHavenId)
        {
            _oracleStation = _dockedHavenId;
            _oracleDraw = 0;
            _oracleDrinks = 0;
            _oracleLine = null;
            _oracleNotice = null;
        }
    }

    // Routed here from TalkToStranger when the BarPatron console is the oracle's (OracleRant.IsOracle). Like
    // the Magpie, presence is gated on her sim-time rota — walking up to a stool she's left tells you she's
    // drifted off, not opens a card.
    private void TalkToOracle()
    {
        if (_dockedHavenId is not { } id)
        {
            return;
        }
        if (!HavenInterior.OraclePresent(id, SimTime))
        {
            ShowPulseMessage("The corner stool's empty — a half-finished drink still fizzing at the wrong frequency. Static's drifted off this watch. Try another. 🌀");
            return;
        }

        EnsureOracleVisit();
        // One doorway open at a time — leaning into the oracle's corner shuts the counter/table cards.
        CloseBarkeep();
        ClosePatronTable();
        _oracleOpen = true;
        if (_oracleLine is null)
        {
            NextOracleLine(); // first lean-in draws her opening rant
        }
    }

    // Turn the dial one line on — the "listen" beat, and what buying a drink does after it's poured. Pulls a
    // seeded line from Core (stable in station + watch + draw + drinks), advances the dial, and lets a true
    // line deliver whatever it points at.
    private void NextOracleLine()
    {
        if (_dockedHavenId is not { } id)
        {
            return;
        }
        OracleLine line = OracleRant.Speak(id, OracleRant.WatchIndex(SimTime), _oracleDraw, _oracleDrinks);
        _oracleDraw++;
        _oracleLine = line;
        DeliverOracleLine(line);
    }

    // A true line "sounds nuts but IS true" — act on what she perceives. A fragment shard is assembled into
    // its arc (KAAMOS/Nebula) and narrated whole (her perception + the canonical shard Lore); a secret-lab
    // tell or a collector warning is a LEAD, filed to the durable overheard book so it doesn't vanish. Pure
    // nonsense lives on the card only — no ledger spam, the sifting is the point.
    private void DeliverOracleLine(OracleLine line)
    {
        if (!line.IsTrue)
        {
            return;
        }

        const string who = "Static Marsh";
        switch (line.Truth)
        {
            case OracleTruthKind.KaamosFragment when line.FragmentId is { } kid:
                string kLore = KaamosLore.ById(kid)?.Lore ?? string.Empty;
                if (TryAssembleKaamos(kid, $"🌀 Static Marsh, tuned to a channel you can't hear: {line.Text} {kLore}"))
                {
                    Overhear($"👂 Static Marsh — a true one: {line.Text}", who);
                }
                else
                {
                    // Already in hand — the perception still lands, no new shard.
                    ShowPulseMessage($"🌀 Static Marsh: {line.Text}");
                }
                break;

            case OracleTruthKind.NebulaFragment when line.FragmentId is { } nid:
                string nLore = NebulaLore.ById(nid)?.Lore ?? string.Empty;
                if (TryAssembleNebula(nid, $"🌀 Static Marsh, tuned to a channel you can't hear: {line.Text} {nLore}"))
                {
                    Overhear($"👂 Static Marsh — a true one: {line.Text}", who);
                }
                else
                {
                    ShowPulseMessage($"🌀 Static Marsh: {line.Text}");
                }
                break;

            case OracleTruthKind.SecretLab:
            case OracleTruthKind.Collector:
                // A lead, not an unlock — she points, you still have to walk it out. Durable so it's followable.
                Overhear($"👂 Static Marsh — a true one: {line.Text}", who);
                ShowPulseMessage($"🌀 {line.Text}");
                break;
        }
    }

    // Buy the oracle a drink — the drink-offer path (#347), reused: extending the glass is the CAPTAIN's own
    // idea, so the OFFER is rolled first (she may wave off a vintage that's "ticking wrong"); only an accepted
    // glass is bought. A poured drink loosens the channel — one more line, and a higher chance it's a true one
    // ("a drunk oracle is more prophetic", OracleRant.TrueChancePerDrink). She's the one drinking, so no rum-
    // tot wobble lands on the captain.
    private void BuyOracleDrink()
    {
        if (!_oracleOpen || _dockedHavenId is not { } id)
        {
            return;
        }
        if (!HavenInterior.OraclePresent(id, SimTime))
        {
            _oracleNotice = "Her stool's empty now — she's drifted off mid-sentence.";
            return;
        }
        if (CurrentKeep is not { } keep)
        {
            _oracleNotice = "No one's behind the bar to pour her one.";
            return;
        }
        if (_credits < keep.DrinkPrice)
        {
            _oracleNotice = $"“Can't stand the oracle a {keep.DrinkName} on {_credits:N0} cr — it's {keep.DrinkPrice} cr a glass.”";
            return;
        }

        // OFFER FIRST (#347): she decides. Deterministic from seed; goodwill 0 (she's no ledger contact), no
        // held secret in play here — a drunk mostly takes the glass, but now and then the vintage reads wrong.
        ulong offerSeed = DiceRule.Seed($"oracle-drink:{id}", (long)SimTime + _oracleDrinks);
        DrinkOfferResult offered = ContactDrink.OfferDrink(offerSeed, currentGoodwill: 0, holdingSecret: false);
        if (!offered.Accepted)
        {
            _oracleNotice = $"Static waves the glass off — “not thirsty for THAT vintage, it's ticking wrong, can't you hear it?” No charge. 🎲 {offered.Describe()}";
            return;
        }

        _credits -= keep.DrinkPrice;
        _oracleDrinks++;
        RequestVaultSave(); // the purse moved
        _oracleNotice = $"You stand Static a {keep.DrinkName} (−{keep.DrinkPrice:N0} cr). She drinks it like water finding a crack. “...clearer now. The channel's wider when the glass is fuller.”";
        NextOracleLine(); // a drunker oracle, a new — likelier-true — line
    }

    private void CloseOracle()
    {
        _oracleOpen = false;
        _oracleNotice = null;
    }

    // ── The NEBULA MUTUAL client holder (#422): assembled per game-thread, wiped on a new voyage, round-
    //    tripped through the vault's NebulaSection (Map.Vault). The Core spine + Vault mapper were built with
    //    #423; the oracle is the first hand that actually gives a Nebula shard, so the client holder is born
    //    here. Mirrors _kaamos exactly. ──
    private readonly NebulaProgress _nebula = new();

    // The one-time notice the instant the whole truth resolves — the capstone contract AND enough intel
    // behind it (NebulaLore.KnowsTheTruth). Announced once per universe; a reload rehydrates without re-firing.
    private const string NebulaTruthNotice =
        "   ▓▓ THE POLICY'S TRUE TERMS RESOLVE — you know what Nebula Mutual files you under now. " +
        "Not insured against death: filed under it. The premium buys STORAGE, and the original never leaves the dark.";

    /// <summary>Assemble a NEBULA fragment, persist it, and narrate the find — the _kaamos idiom
    /// (TryAssembleKaamos) for arc 2. Returns true only the first time a shard is held, so a caller narrates
    /// a genuinely NEW leak and a re-perception stays quiet. The truth-resolves notice fires on the single
    /// edge that flips KnowsTheTruth, once per universe, never on reload.</summary>
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
        return true;
    }

    // The NEBULA intel readout for the Captain's ledger — mirrors KaamosLedgerTip so a shard the oracle
    // leaked is visible and re-readable (the assembled shard texts build the corporate-horror as you collect).
    // Null until the first shard is in hand.
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
            lines.Add("▓ Enough of the small print to earn the contract. The pieces resolve into the clause the sales voice skips.");
        }
        else
        {
            lines.Add($"▓ The shape isn't clear yet — {need - intel} more shard{(need - intel == 1 ? "" : "s")} to read it. One poster line is never enough; one adjuster's drink is never enough.");
        }

        string headline = hasKey
            ? $"▓ NEBULA MUTUAL — {intel} of {need} shards · the contract in hand"
            : $"▓ NEBULA MUTUAL — {intel} of {need} shards assembled";

        return new Stations.Captain.LedgerTip(
            headline, lines.ToArray(), "what your resurrections really are",
            ScopeTipId: null, ShowDarkWeb: false, DossierShipId: null);
    }
}

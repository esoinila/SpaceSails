using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Map.Bond — #429 STRANGER-BOND, the WARM twin of the ambient-dread system (owner mid-storm 2026-07-20:
// "This all made unknown people talk to each other and they recommended cognac.. I agreed… adversity makes
// the sharers bond… it bonds strangers. Let's use that."). The SAME ambient scares that unsettle you — a
// hull-shudder, an unexplained buzzer, a caution PA (all #430 HullShudder, surfaced in Map.Shudder) — can,
// when a not-yet-known patron is co-present in the docked bar, OPEN them to you instead of only chilling the
// room. This is the thin client wiring: after the dread beat is spoken, offer the bond beat when the pure
// Core StrangerBond says so, and apply the goodwill/contact effect through the EXISTING ContactLedger
// methods (AddGoodwill / the drink pour) — never re-implemented here. Surfaced as a warm toast in the house
// voice; the cognac recommendation is the hero.
public partial class Map
{
    // The bond's own thin real-time layer (the seeded decision lives in Core StrangerBond).
    private int _bondIndex;                                    // monotonic bond ordinal (seeds gate/outcome/line)
    private double _bondLastMs = double.NegativeInfinity;      // real-time ms of the last bond (the cooldown clock)
    private bool _bondForce;                                   // /map?bond=1 dev cheat: force the next scare to bond

    // Called at the END of a scare's dread beat (FireShudder / FireSignal / FireCaution). If the captain is
    // walking a docked bar with a co-present stranger (or a not-yet-close acquaintance), a seeded chance —
    // bounded by a cooldown, one bond per scare — turns the shared fright into a warm moment. A no-op when
    // there's no bar, no one eligible, or the seeded gate holds. The cognac beat is the hero.
    private void TryBond(StrangerBond.Scare scare, bool cold, double nowMs)
    {
        // Only in a docked bar/haven — a scare on the bare ship deck or a lonely surface site has no room of
        // strangers to bond. Keyed to the docked bar's keep (the same gate PresentBarContacts uses).
        if (_dockedHavenId is not { } haven || CurrentKeep is null)
        {
            return;
        }

        // The cooldown floor — one bond, then the room settles before the next scare can open another (the
        // cheat bypasses it, so a forced demo fires at once).
        if (!_bondForce && nowMs - _bondLastMs < StrangerBond.CooldownSeconds * 1000.0)
        {
            return;
        }

        ulong seed = DiceRule.Seed(ShudderSeed(), "stranger-bond");

        // The seeded gate: does this scare open a bond at all? A cold/deep scare bonds rarer (but deeper).
        if (!_bondForce && !StrangerBond.Opens(seed, _bondIndex, cold))
        {
            return;
        }

        // Who is co-present: the not-yet-known patrons (candidates for a comment / cognac / new contact) and
        // the KNOWN-but-not-yet-close acquaintances (candidates to deepen a notch).
        IReadOnlyList<string> strangers = PresentBarStrangers();
        IReadOnlyList<string> acquaintances = PresentBarAcquaintancesBelowClose();

        // The cheat guarantees a stranger to bond even on a watch the rota seated no regular — so the hero
        // cognac beat is always demoable (see docs/testing-guide.md).
        if (_bondForce && strangers.Count == 0)
        {
            string? fallback = PatronRota.Roster.FirstOrDefault(r => !_contacts.For(r).HasHistory);
            if (fallback is not null)
            {
                strangers = [fallback];
            }
        }

        StrangerBond.Bond outcome = StrangerBond.Outcome(
            seed, _bondIndex, cold, strangers.Count > 0, acquaintances.Count > 0);

        // The cheat forces the HERO beat: a stranger stands you a cognac (the thing the owner verifies).
        if (_bondForce && strangers.Count > 0)
        {
            outcome = StrangerBond.Bond.Drink;
        }

        if (outcome == StrangerBond.Bond.None)
        {
            return; // opened, but no one eligible was near — nothing to play
        }

        // Pick the one it bonds (a stranger for a/b/c, an acquaintance for a deepen).
        bool deepen = outcome == StrangerBond.Bond.Deepen;
        IReadOnlyList<string> poolOfPeople = deepen ? acquaintances : strangers;
        string giver = poolOfPeople[StrangerBond.Pick(seed, _bondIndex, poolOfPeople.Count)];
        string display = GiverDisplay(giver);

        string line = StrangerBond.Line(outcome, scare, cold, seed, _bondIndex);
        if (deepen)
        {
            line = string.Format(line, display); // the {0} the deepen pool carries
        }

        // Apply the effect through the EXISTING systems — goodwill via ContactLedger.AddGoodwill (which
        // creates the record on first dealing, so a stranger becomes a findable KNOWN contact), and a shared
        // glass via PourRum (the #226 sanity-relief seam) on the cognac beat. Nothing here re-implements
        // goodwill or contacts.
        int goodwill = StrangerBond.GoodwillFor(outcome, cold);
        if (goodwill != 0)
        {
            _contacts.AddGoodwill(giver, giver, goodwill);
        }

        switch (outcome)
        {
            case StrangerBond.Bond.Comment:
                // Warmth without a ledger line: the company steadies the nerve a hair against the dread the
                // same scare dealt (NerveModel owns the law — the caution-PA steady path, reused).
                _nerve = NerveModel.Clamp(_nerve + StrangerBond.CommentNerveSteady);
                break;

            case StrangerBond.Bond.Drink:
                // The stranger stands you the cognac — you drink it. A shared glass is the strongest sanity
                // relief (SharedWithContact), riding the one wobble/tot law via PourRum.
                PourRum($"{StrangerBond.HeroCognac} with {display} — on the fright", NerveModel.DrinkKind.SharedWithContact);
                break;
        }

        RendererInterop.PlayCue("rum"); // a warm cue under the toast (silent if audio is off)
        ShowPulseMessage("🥂 " + line);

        _bondLastMs = nowMs;
        _bondIndex++;
        _bondForce = false; // a forced bond is a one-shot — reload to arm the cognac beat again
        RequestVaultSave();  // goodwill/nerve moved — persist the warmed relationship
    }

    // The not-yet-known patrons drinking here right now — a BarPatron console whose giver we have NO
    // ContactLedger history with (the inverse of PresentBarContacts). Empty when the room holds only faces we
    // already know, so a bond only ever opens a genuine stranger. Mirrors the BuyRoundForRoom / PresentBar-
    // Contacts scan (the roaming Magpie's rota gate, the oracle skip, the two-console dedupe).
    private IReadOnlyList<string> PresentBarStrangers()
    {
        if (!_deckMode || CurrentKeep is null)
        {
            return [];
        }
        var found = new List<string>();
        foreach (string giver in PresentBarPatrons())
        {
            if (!_contacts.For(giver).HasHistory)
            {
                found.Add(giver);
            }
        }
        return found;
    }

    // The KNOWN contacts drinking here who are NOT yet already-close (goodwill below StrangerBond's cap) —
    // the candidates a shared scare can DEEPEN a notch. A true friend (at/over the cap) has nothing left for
    // the shared fright to add, so they're excluded (owner: "never fires for already-close contacts").
    private IReadOnlyList<string> PresentBarAcquaintancesBelowClose()
    {
        if (!_deckMode || CurrentKeep is null)
        {
            return [];
        }
        var found = new List<string>();
        foreach (string giver in PresentBarPatrons())
        {
            ContactHistory h = _contacts.For(giver);
            if (h.HasHistory && h.Goodwill < StrangerBond.AlreadyCloseGoodwill)
            {
                found.Add(giver);
            }
        }
        return found;
    }

    // The distinct bar patrons actually present this watch (the shared scan the bond's stranger/acquaintance
    // splits read). Skips the oracle (her own corner flow) and the roaming Magpie when their rota has them
    // away; dedupes a contact who holds two consoles.
    private IEnumerable<string> PresentBarPatrons()
    {
        bool backOpen = _dockedHavenId is { } st
            && UnlockedHatchesFor(st).Any(h => HavenInterior.HatchGrowsWing(st, h));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DeckPlan.ConsoleSpot c in _deckPlan.Consoles)
        {
            if (c.Kind != DeckPlan.ConsoleKind.BarPatron)
            {
                continue;
            }
            string giver = c.Label.Replace("◈", "").Trim();
            if (OracleRant.IsOracle(c.Label))
            {
                continue; // the oracle is not a ledger contact — no bond with her
            }
            if (!seen.Add(giver))
            {
                continue; // one contact, two consoles (the roaming Magpie) — count once
            }
            if (giver.Contains("MAGPIE", StringComparison.OrdinalIgnoreCase)
                && !HavenInterior.ResolveMagpie(SimTime, backOpen).Present)
            {
                continue; // the Magpie only drinks with the room when their rota has them in it
            }
            yield return giver;
        }
    }
}

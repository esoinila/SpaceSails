using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Map.Custody — #426 CHAIN-OF-CUSTODY DREAD, the thought a tremor on HER OWN DECK sets going.
//
// Owner, 2026-07-20, sailing through a storm: "The word 'storm' was spoken... makes one think the ship's
// long chain of owners and maintenance organizations and insurers.. hope it all was checked through the
// whole chain. 😅"
//
// The pure spine is Core ChainOfCustody (which of the three worries, composed from HER ShipHistory's own
// facts); this is the whole client half: one question asked inside FireShudder, and no state of its own.
//
// WHY THIS SPEAKS *AS* THE SHUDDER RATHER THAN AFTER IT. The pulse is one slot (#693). A second
// ShowPulseMessage in the same breath does not add a line, it DELETES the shudder's own — so the opening
// tremor of a storm window says the chain-of-custody line INSTEAD of a pool line, and every other tremor
// in that window speaks the pool exactly as before. One line per window, no queue, no second beat to
// schedule, and nothing the player was being told stops being told.
public partial class Map
{
    // The thought this tremor sets going, or null when it is only weather.
    //
    // Three gates, and all three are the issue's own:
    //   · HER OWN DECK. The dread is about THIS hull's chain of owners. A haven's concourse settling on its
    //     clamps is not her paperwork, and a moon is not a hull at all — so it is Setting.Ship or nothing.
    //   · ONCE PER STORM WINDOW. The window is the rough patch the caution PA already counts (#424): a run
    //     of shudders close together, closed by a lapse or by the PA. FireShudder has just counted this one
    //     into the run, so a run of exactly one is the tremor that OPENED the window — and there is exactly
    //     one of those per window, which is the "once" without a flag to keep in sync.
    //   · A HULL WITH A CHAIN. Core declines for a hull with no former name and no yard record; hers has
    //     both (ShipHistories.Hers — her plate's yard and year, the rest off the same pools every hull is
    //     dealt from), so in the shipping game this speaks, and the null path is the law rather than dead
    //     code.
    private string? ChainOfCustodyThought(HullShudder.Setting setting)
    {
        if (setting != HullShudder.Setting.Ship || _cautionRun != 1)
        {
            return null;
        }

        // Deterministic per (hull, window): the deck's own shudder seed carries the hull, and the shudder
        // ordinal — already advanced past this tremor — names the window. No clock, no System.Random.
        ulong windowSeed = DiceRule.Seed(ShudderSeed(), $"chain-of-custody:{_shudderIndex}");
        return ChainOfCustody.Line(ShipHistories.Hers, windowSeed);
    }
}

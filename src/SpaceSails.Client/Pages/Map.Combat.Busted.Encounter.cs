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
/// THE ENCOUNTER'S OWN RECORD — every field the busted card is drawn from, and the two presses that are
/// not one of the three options: dismissing the last card of the death→rebirth chain (through the #470
/// seam, so the keyboard comes home) and buying the boarding-nets jammer.
///
/// <para>Split out of <c>Map.Combat.Busted.cs</c> under #251 with no member renamed, re-scoped or
/// re-ordered.</para>
/// </summary>
public partial class Map
{
    // #470: dismissed through the Dismiss() seam so the keyboard comes home. This is the last card of the
    // death→rebirth chain, and the new captain takes the helm the instant it drops — without the way back
    // they would sit at the controls with every key dead.
    private void CloseBusted()
    {
        _busted = null;
        StateHasChanged();
    }

    // Buy the one shipped dice helper: a Boarding-nets jammer, a named +2 on resist/Bolivia initiative
    // (the modifier seam; the shop is a follow-up). One-time purchase, lost on resurrection.
    private void BuyNetJammer()
    {
        if (_hasNetJammer || _credits < NetJammerPriceCr)
        {
            return;
        }

        _credits -= NetJammerPriceCr;
        _hasNetJammer = true;
        ShowPulseMessage("Boarding-nets jammer fitted — +2 on any last stand. The collectors hate it.");
        StateHasChanged();
    }

    // The open BUSTED pop-up's state — grappled, at 1×, the captain choosing. The dice are rolled Core-
    // side (BustedRule / BoliviaEncounter); this holds what's been rolled and which panel to show.
    public sealed class BustedEncounter
    {
        // Impact (#264): a body's surface collected the ship — no collector, no dice, straight to the
        // freeze-frame → clinic re-birth. Reuses this whole encounter so the death machinery is shared.
        // SurfaceEnd (Evening wind #20): a nerve-overdraw death out on the regolith — no collector, no dice.
        // Its own freeze-beat (the cause art + the place-dependent line) before the shared resurrection.
        // #535 · NoContactLogged is the fifth exit: a key was presented and the encounter never happened.
        // Appended, like every stage that has joined this list before it — the markup switches on these arms
        // and several guards name one by hand, so a stage slipped into the middle would re-page all of them.
        public enum Stage { Demand, Confiscated, BribedOff, ResistWon, ResistLost, Bolivia, Fled, FreezeFrame, Resurrected, Impact, SurfaceEnd, NoContactLogged }

        public required string HunterId { get; init; }
        public required string HunterCallsign { get; init; }
        public required int Heat { get; init; }
        public required ulong Seed { get; init; }
        public required DiceRoll Bribe { get; init; }
        public Stage Phase { get; set; } = Stage.Demand;
        public OpposedRoll? ResistRoll { get; set; }
        public BustedRule.Confiscation? Confiscation { get; set; }
        public string? ResultMessage { get; set; }
        public string? ClinicName { get; set; }
        public int ClinicBillCr { get; set; }

        /// <summary>#621 · What the policy actually PAID — read off the rebirth outcome's own kit, not
        /// re-quoted from <see cref="BustedRule.InsuranceCredits"/> in the markup. The receipt was printing
        /// the uninsured constant while the purse was set from the kit; the day a policy tier hands a
        /// different stake the card would have gone on stating the old number with complete confidence, and
        /// two places computing one fact is the bug even while they agree.</summary>
        public int StakeCr { get; set; }
        public string? HullDescription { get; set; }
        public string? ImpactBodyName { get; set; } // #264: the body that collected the ship

        // #422 arc 2 (NEBULA MUTUAL) — the fragments this death surfaces, delivered ON this card. The rebirth
        // glitch flashes on every wake (assembled once); the collector writ is glimpsed at a collector catch;
        // the clinic's second page surfaces on a LATER death. Blank when the beat doesn't apply this time.
        public string? RebirthGlitch { get; set; } // the one-flat-second wake-card glitch flash
        public string? CollectorWrit { get; set; } // the writ glimpsed off a collector at the demand
        public string? ClinicLedger { get; set; }  // the clinic ledger's second page (woken here before)

        // #380 item 1: WHAT killed the captain, and WHERE — so the resurrection card explains the death
        // place-dependently (cause art + a seeded house-voice line) before the brain-backup copy. Defaults to
        // the collector (the BUSTED last stand); the impact path sets Impact. Surface causes are wired ready.
        public DeathCause Cause { get; set; } = DeathCause.Collector;

        /// <summary>#574 · WHERE it happened — her own deck, somebody else's hull, or a suit on a surface.
        /// The card reads the same cause differently depending on this, because a derelict has no regolith
        /// to be run down on and an away team is not standing on a deck.</summary>
        public DeathPlace Place { get; set; } = DeathPlace.OwnShip;
        // Which meter actually ran out (#480 follow-up): true = the nerve overdrew, false = the five blows
        // landed. Before #469 was fixed nerve was effectively the ONLY way to die out there, so the card
        // hardcoded the nerve line; now that the condition marker really decides, the caption must not
        // blame a steady captain's nerve for a mauling.
        public bool NerveRanOut { get; set; }

        public string? DeathBodyName { get; set; } // the place the death is narrated off (moon / body flown into)

        // Evening wind #20 — THE NEW CAPTAIN. On any death-resurrection the piracy insurance issues a fresh
        // name + face; these carry the hand-over for the resurrection card (blank when no thread to succeed —
        // a legacy run — so the card falls back to the plain brain-backup copy).
        /// <summary>#973 L1 · The one line the wake says about the FILING LINE — which pages of the ledger came
        /// back with this captain and which did not. Decided by the policy in force at the death
        /// (<c>FilingLine.WakeNotice</c>), never by counting greyed rows. Blank on a legacy run with no thread
        /// to succeed, exactly like the two names below it.</summary>
        public string? FilingNotice { get; set; }

        public string? RetiredCaptainName { get; set; } // the captain who just walked into the dark
        public string? NewCaptainName { get; set; }     // the name the policy put on the license
        public int NewCaptainAvatar { get; set; }       // the new face in the mirror (art/captain-N.jpg)

        // Bolivia progress
        public OpposedRoll? BoliviaInitiative { get; set; }
        public int BoliviaBeatIndex { get; set; }
        public int BoliviaNet { get; set; }
        public List<BoliviaEncounter.BeatOutcome> BoliviaOutcomes { get; } = [];
        public BoliviaEncounter.Ending? BoliviaEnding { get; set; }
    }
}

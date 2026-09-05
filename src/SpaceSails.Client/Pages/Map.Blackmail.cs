using System.Collections.Generic;
using System.Linq;
using SpaceSails.Client.Rendering;
using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Subject: #233 · THE BLACKMAIL TWIN — the roadster fetch that has photographs in it instead of a wallet,
// the three doors the job can go out of, and the parrot's running gag over the top of all of it.
//
// Everything here hangs off machinery that already shipped. The fetch is #241's roadster errand, unchanged
// from the Fixer's tip to the coast alongside; the find is #614's carried-object idiom; the endings are the
// pay-at-counter hand-off, the dark-web desk's own fence seam, and #223's hole in the ground. What is new is
// one bit of world state — WHICH CAR THIS IS — and the three arms it grows.
//
// THE BRIEF STAYS THE FIXER'S. The ledger card, the checklist and the compass go on saying "wallet",
// because that is what the client said was in the car and the client is not going to correct himself. Ending
// one is him asking you not to correct him either.
public partial class Map
{
    // ── WHICH CAR IS THIS ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The fetch jobs whose roadster has the chip between the seats rather than the wallet. Keyed by
    /// quest id and dealt ONCE, at the moment the offer is built — never re-rolled at the pickup, because a
    /// car that is a different car depending on when you look at it is not a car.</summary>
    private readonly HashSet<string> _chipFetches = [];

    /// <summary>The key this deal rides under in a saved quest's open field bag (Map.Vault). Named for the
    /// question it answers rather than for the answer, so a second cargo one day needs no second key.</summary>
    private const string CarCargoField = "carCargo";

    /// <summary>The bird has already asked where the car is on this hunt. One gag, one asking.</summary>
    private bool _parrotHuntedTheCar;

    /// <summary>Deal this roadster its cargo — the wallet, or one car in
    /// <see cref="CompromisingChip.OneInEvery"/>, the chip. The seed is the booth's own idiom, the same two
    /// numbers the hand-off address is picked with one line up in <c>MakeFetchOffer</c>: a slow sim-time
    /// rotation and a stable char-sum of the berth. So the card on the table does not flicker between two
    /// cars while the captain reads it, and two docks on the same watch deal differently.</summary>
    private void DealWhatIsBetweenTheSeats(Quest offer)
    {
        if (CompromisingChip.BetweenTheSeats(
                (long)(SimTime / 1000), (_dockedHavenId ?? "").Sum(ch => ch)))
        {
            _chipFetches.Add(offer.Id);
        }
    }

    /// <summary>Is THIS job's car the blackmail twin?</summary>
    private bool TheCarHasTheChip(Quest q) => _chipFetches.Contains(q.Id);

    /// <summary>The chip in the captain's own pocket, or null. Every ending asks this first.</summary>
    private Satchel.Item? TheChipInThePocket => CompromisingChip.InThePocket(_satchel);

    // ── THE FIND · BETWEEN THE SEATS ──────────────────────────────────────────────────────────────────

    /// <summary>The prise, when the car is the twin. Same moment, same range, same cue as the wallet — what
    /// differs is that something comes ABOARD, and that the sentence is the object's own look-card line
    /// rather than a report about it. The card itself is not raised here: this is the chair, half a million
    /// kilometres from anywhere, and a modal over the helm at warp is the one thing the general UI law is
    /// about. It is in the satchel, and 🔍 is where a captain looks at what he is carrying.</summary>
    private void TheChipComesAboard(Quest q)
    {
        _satchel = [.. Satchel.Add(_satchel, CompromisingChip.Found())];

        // #727 · through the one writer, like the wallet beside it.
        AdvanceMission(q, QuestState.PickedUp, CompromisingChip.LookCardLine);
        RendererInterop.PlayCue("board");
        RequestVaultSave();
    }

    // ── ENDING ONE · THE COUNTER ──────────────────────────────────────────────────────────────────────

    /// <summary>Give it back. The contract pays what the contract said — this is not a negotiation and the
    /// chip does not make the job worth more to the man who commissioned it. He says one sentence and it is
    /// the only sentence: he does not name the two people in the photographs, and he does not ask whether
    /// you looked, because he already knows the answer to that and is telling you what to say instead.
    ///
    /// <para>Shares the whole hand-off with <c>DeliverFetch</c> — the purse, the quiet history, the one
    /// writer — and differs only in the sentence and in the chip leaving the pocket.</para></summary>
    private void HandTheChipBack(Quest q)
    {
        _satchel = [.. CompromisingChip.Spend(_satchel)];
        _credits += q.Reward;
        _contacts.RecordCompletion(q.Giver, q.Giver, q.Reward, SimTime);
        AdvanceMission(q, QuestState.TurnedIn, CompromisingChip.ClientLine);
        RequestVaultSave();
    }

    // ── ENDING TWO · THE DESK ─────────────────────────────────────────────────────────────────────────

    /// <summary>The fetch job the chip in the pocket belongs to, while it is still open. Null once the job
    /// has gone out of one of its three doors — which is what stops a captain fencing a chip he already
    /// handed back, and what stops the Fixer paying for one that is in a hole.</summary>
    private Quest? TheOpenChipJob =>
        TheChipInThePocket is null
            ? null
            : _quests.FirstOrDefault(q => q is { Kind: QuestKind.Fetch, State: QuestState.PickedUp }
                                          && TheCarHasTheChip(q));

    /// <summary>What the dark-web desk will pay for the chip right now, or null when there is nothing to
    /// sell or nowhere to sell it. The price is <see cref="CompromisingChip.FencePrice"/> — derived from the
    /// contract's own pay, so a repriced job reprices the betrayal with it.</summary>
    private int? ChipFencePrice() =>
        DarkWebCanTrade() && TheOpenChipJob is { } job ? CompromisingChip.FencePrice(job.Reward) : null;

    /// <summary>Sell it. More coin than the contract, and the difference is not free: one band of heat lands
    /// on the book of whoever runs the ground the desk was worked from (#715's step, taken through the one
    /// banking seam every other crossing goes through). The fence names an appetite and never a buyer.</summary>
    private void SellTheChipToTheFence()
    {
        if (TheOpenChipJob is not { } job || ChipFencePrice() is not { } price
            || DarkWebCurrentBody() is not { } where)
        {
            return;
        }

        _satchel = [.. CompromisingChip.Spend(_satchel)];
        _credits += price;
        BankTheCrossing(new UndergroundComplex.HeatCharge(SiteOperator.Of(where.Id).Id, IllegalHeat.ABand));
        AdvanceMission(job, QuestState.TurnedIn, CompromisingChip.FenceLine);
        RequestVaultSave();
        StateHasChanged();
    }

    // ── ENDING THREE · THE HOLE ───────────────────────────────────────────────────────────────────────

    /// <summary>Put it in the ground with the rest of what nobody is going to ask about. The chest's manifest
    /// lists it by the chip's own name and flags it hot (#202's flag), and NOTHING IS SAID — there is no
    /// counterparty to a hole, so the beat is deliberately null rather than a sentence somebody wrote to fill
    /// the silence. Called from inside the bury, after the hold has been settled and before the chest is
    /// minted, so the manifest that goes into the ground is the one the captain gets back.</summary>
    private void TheChipGoesInTheChest(List<CacheCargo> manifest)
    {
        if (TheChipInThePocket is null)
        {
            return;
        }

        manifest.Add(CompromisingChip.Manifest());
        _satchel = [.. CompromisingChip.Spend(_satchel)];
        if (_quests.FirstOrDefault(q => q is { Kind: QuestKind.Fetch, State: QuestState.PickedUp }
                                        && TheCarHasTheChip(q)) is { } job)
        {
            AdvanceMission(job, QuestState.TurnedIn);
        }
    }

    // ── THE BIRD ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Beat one of the gag: the scope swings onto the Fixer's fix and the bird wants to know where
    /// the car is. Fires once per hunt, at the press that starts the scan, and only for the roadster —
    /// every other intel fix is somebody's timetable and the joke does not survive being general.</summary>
    private void SquawkTheCarHunt(string bodyId)
    {
        if (_parrotHuntedTheCar || bodyId != Derelict.RoadsterBodyId)
        {
            return;
        }

        _parrotHuntedTheCar = true;
        SquawkNow(Parrot.Squawk.CarHunt, _lastTimestampMs ?? 0, force: true);
    }

    /// <summary>Beat three: alongside. Owner's own framing — <i>"when the ship comes alongside the wreck (the
    /// moment the fetch pickup unlocks)"</i> — so it rides the pickup's own edge and needs no latch of its
    /// own: the state pattern that lets the prise happen once lets this happen once with it.</summary>
    private void SquawkTheCarFound() =>
        SquawkNow(Parrot.Squawk.CarFound, _lastTimestampMs ?? 0, force: true);
}

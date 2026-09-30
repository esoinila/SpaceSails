using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

/// <summary>
/// #251 · THE OFFER (#697, #603, #680) — the wallet as one thing, the fanned try and the single try, the one
/// resolution both presses share, and the rounds that go into a gun by hand.
///
/// <para>Split out of <c>Map.Surface.Darkroom.cs</c> under #251 as a pure move: one contiguous run, no
/// member renamed, re-scoped or re-ordered. The #697 banner and <c>_walletOpen</c> stay in the opening
/// file with every other field of the family.</para>
/// </summary>
public partial class Map
{
    /// <summary>#697 · What is in the wallet, which is Core's own grouping of the pocket and never a filter
    /// written in the dialog: <see cref="Core.Satchel.CompartmentOf"/> is the law about which things are flat,
    /// and a second answer here would drift the first time a kind changes compartment (#688).</summary>
    private IReadOnlyList<Core.Satchel.Item> Wallet() =>
        Core.Satchel.OfKind(_satchel, Core.Satchel.Kind.Authority);

    /// <summary>#697 · Where the whole wallet can be offered at once: whatever the satchel is open AT, when
    /// that thing reads authorities. The question is Core's — the same <see cref="SatchelTry.CanOffer"/> the
    /// rows ask (#688) — so the folder can never carry a live offer at something the rows have gone inert
    /// for.</summary>
    private (SatchelTry.Target Target, string? Context, string Label)? WalletTarget() =>
        _satchelTarget is { } at && SatchelTry.CanOffer(Core.Satchel.Kind.Authority, at.Target) ? at : null;

    /// <summary>#697 · FAN THE WALLET AT THE READER. One press, every card, one line.
    ///
    /// <para>It performs no state transition of its own. A success ends through exactly the resolution a
    /// single successful try ends through (<see cref="TheOfferIsAnswered"/>), because a hand-written copy of
    /// that ending is this repo's first named bug class aimed at a state transition — and the day one of them
    /// learns to spend something, the other will not.</para></summary>
    private void TryTheWholeWallet()
    {
        if (WalletTarget() is not { } at)
        {
            return;
        }

        IReadOnlyList<Core.Satchel.Item> wallet = Wallet();
        if (wallet.Count == 0)
        {
            return;
        }

        // No item is charged to the fan: a wallet's success has no single row to spend, and an authority is
        // never consumed by being read anyway. Core has already decided which card answered.
        TheOfferIsAnswered(SatchelTry.OfferWallet(wallet, at.Target, at.Context), null, at);
    }

    /// <summary>#603 · Offer one carried thing to whatever the satchel is open at. The outcome is always
    /// SAID — a control that does nothing and says nothing is indistinguishable from a bug.</summary>
    private void TryItem(Core.Satchel.Item item)
    {
        if (TargetFor(item) is not { } at)
        {
            return;
        }

        // ── #696 · DECIDING A PAPER IS A MAP TAKES THE SAME SECONDS AS PHOTOGRAPHING ONE ──
        //
        // Owner's ruling covers both halves of the detective loop in one sentence, and it has to: a game
        // that charged for filing a document and handed the clue reading away free would be teaching the
        // captain to read everything on the spot and file nothing, which is the exact behaviour the cost
        // model exists to make a decision.
        //
        // The hold is started here and not inside TheOfferIsAnswered, because that method is the ENDING both
        // presses share (#697) — putting a clock inside it would put a clock in front of a wallet fan too.
        // Nothing else about the ending moves: the far end calls it with the same three arguments this line
        // would have.
        if (_surface is not null && item.Kind == Core.Satchel.Kind.Paper && at.Target == SatchelTry.Target.Tracker)
        {
            BeginProcessing(Core.Processing.Work.Read, item, WhereYouAreStanding(), at);
            return;
        }

        TheOfferIsAnswered(SatchelTry.Offer(item, at.Target, at.Context), item, at);
    }

    /// <summary>#603 · What an offer DOES once it has an answer — the one ending both presses share.
    ///
    /// <para><paramref name="item"/> is the thing that was held up, or null when the whole wallet was fanned
    /// (#697): a fan has no single row to charge, and the two consuming branches below can only ever fire for
    /// a paper or a handful of rounds, neither of which is ever in a wallet. That is what makes "no double
    /// effects" structural rather than a promise.</para></summary>
    private void TheOfferIsAnswered(
        SatchelTry.Outcome outcome,
        Core.Satchel.Item? item,
        (SatchelTry.Target Target, string? Context, string Label) at)
    {
        // ── #680 · THE ANSWER IS SAID WHERE THE PLAYER IS LOOKING ──
        //
        // Owner, live, in caps: "pressing Try IT on item produces a text that is IMPOSSIBLE to read" /
        // "it is behind the blurring effect... so we don't tell the story."
        //
        // A refusal keeps the satchel open (#614 — a captain comparing three cards should not have to
        // reopen their pockets), and this method used to pulse the line FIRST and branch after — so every
        // refusal, the exact sentences #603's law exists for, played to the HUD under the backdrop's blur.
        // The sim told the story; the z-order ate it. In the DOM is not on the screen (the owner's own
        // formulation): the one layer the backdrop cannot blur is the dialog's own subtree, so a failed
        // offer is stored for the dialog to say, and only a success — which closes the modal — pulses.
        // ── #803 · THE PUT VERB ANSWERS FOR ITSELF, BEFORE ANYTHING IS SAID ──
        //
        // Core's yes at a sentry is a yes about the KIND of thing being offered ("it takes rounds"). Whether
        // THESE rounds go into THAT drum is a question about a ceiling, a kind already loaded and whether the
        // machine is on your back or on the ground — three facts SatchelTry cannot see. So the hand-load
        // decides, here, and its answer REPLACES the generic one rather than following it: two sentences for
        // one act is how a captain ends up reading "rounds into the hopper" immediately above "there is
        // nowhere for them to go".
        if (outcome.Worked && item is { Kind: Core.Satchel.Kind.Rounds } fed
            && at.Target == SatchelTry.Target.Sentry && _surface is { } loadEx)
        {
            outcome = TheRoundsGoInByHand(loadEx, fed, at.Context, fed.Count);
        }

        if (!outcome.Worked)
        {
            _satchelOutcome = outcome.Line;
            return;
        }

        // #1341 · …except the certainty line over a sheet somebody WROTE. The tracker's yes on a paper is FieldClue's
        // "how well it pins a place" — true of a mention, a description, a position, and nonsense over a line
        // item or a rate schedule, which pin no place at all. The sheet itself is shown below; nothing is said
        // over it.
        bool theReaderSpeaks = item is not { Kind: Core.Satchel.Kind.Paper } sheet
                               || at.Target != SatchelTry.Target.Tracker
                               || Core.FieldClue.ReadsAsAClue(sheet.Id);
        if (theReaderSpeaks)
        {
            ShowPulseMessage(outcome.Line);
        }

        // ── #603 · READING A PAPER NEVER SPENDS IT ──
        //
        // Owner: "press I ... inventory opens... select paper and see what the clue is." / "it should be
        // viewable many times."
        //
        // The first cut consumed it, which was wrong twice over. It conflated LOOKING with DECIDING — one
        // click both read the document and burned it — and it broke the field book's own law (#587: "a find
        // that is shown once is a find that is lost"). A paper is a thing you own; you can take it out and
        // read it again in a year.
        //
        // So the document is always shown, in full, every time. The tracker gets plotted on the first read
        // and GrantLabLead no-ops on every one after, which is the honest shape: the knowledge is what is
        // one-shot, not the paper.
        //
        // #1345 · …and A WRITTEN SHEET IS A PAPER, NOT A CLUE (ruled 2026-09-30). A sheet somebody wrote is headed
        // with its own title — the name on its row in the sleeve — not with the certainty word, which is how the
        // dice's sheets are told apart; and reading it names no moon: a paper says what it says and points nowhere
        // by itself (the book's threads do the pointing, #741). A composed sheet is exactly as before.
        if (item is { Kind: Core.Satchel.Kind.Paper } read && at.Target == SatchelTry.Target.Tracker)
        {
            bool aClue = Core.FieldClue.ReadsAsAClue(read.Id);
            _viewObject = new DeckPlan.ConsoleSpot(
                DeckPlan.ConsoleKind.ViewObject, (float)_avatarX, (float)_avatarY,
                aClue
                    ? $"📋 {Core.FieldClue.Label(Core.FieldClue.CertaintyOf(read.Id)).ToUpperInvariant()}"
                    : Core.FieldClue.Title(read.Id),
                "",
                theReaderSpeaks
                    ? Core.FieldClue.Document(read.Id) + "\n\n" + outcome.Line
                    : Core.FieldClue.Document(read.Id));

            if (aClue)
            {
                GrantLabLead(DiceRule.Seed($"clue:{read.Id}"));
            }
        }

        CloseSatchel();
    }

    /// <summary>
    /// #603 case 2, grown up into #803's PUT VERB · THE ROUNDS GO IN BY HAND, AND THE GUN REMEMBERS WHAT
    /// THEY WERE.
    ///
    /// <para>Six rounds is not a resupply, it is a decision — which gun, and with what. A sentry loaded with
    /// the lab round clears a line with one shot and refuses anything already on top of it; one loaded with
    /// issue ball does neither. Both facts live on the magazine, so the bot carries the kind.</para>
    ///
    /// <para><b>What #803 changed.</b> The old cut took any handful into any dry bot and wrote
    /// <c>gun.Rounds += fed.Count</c> — no ceiling, because a drum at 00 plus a hut's twenty-two could not
    /// reach ninety-nine and the bug was unreachable rather than absent. The put verb loads bots that are
    /// already part full, so the ceiling is now the whole point: a magazine the two-digit readout cannot
    /// report is the sim and the #797 instrument disagreeing about one number, which is this repo's third
    /// named bug class with a gun in its hand. <see cref="SentryHandLoad.Offer"/> owns all of it; what is
    /// left here is applying the answer.</para>
    ///
    /// <para><b>#837 · AND IT IS NOW THE WHOLE OF BOTH GRIPS.</b> The [I]-over-the-bot press and the satchel
    /// row's chooser end HERE, in this method, with a unit and a count — which is what makes the issue's law
    /// ("both grips end in the identical magazine mutation and pocket remainder for the same rounds and
    /// target") structural rather than a promise. <paramref name="rounds"/> arrived as <c>fed.Count</c> when
    /// there was only one grip and no way to load less than everything; the stepper made it a decision, so
    /// it is a parameter. Nothing else about the act moved.</para>
    /// </summary>
    private SatchelTry.Outcome TheRoundsGoInByHand(
        SurfaceExcursion ex, Core.Satchel.Item fed, string? unit, int rounds)
    {
        SurfaceBot? gun = ex.Bots.FirstOrDefault(b => b.Unit == unit);
        if (gun is null)
        {
            return new(false, "🔫 That gun is not down here any more.");
        }

        // The pocket as it stands, not as the row remembered it: a chooser stays open across a press, and a
        // stack whose Count was read two presses ago is a figure about a pocket that has since changed.
        int pocket = Core.Satchel.CountOf(_satchel, fed.Kind, fed.Id);
        int offering = Math.Clamp(rounds, 0, pocket);

        SentryHandLoad.Load load = SentryHandLoad.Offer(
            gun.Unit, gun.Rounds, gun.AmmoId, WithinHandsOf(gun), offering, fed.Id);
        if (!load.Worked)
        {
            return new(false, load.Line);
        }

        // Only what the drum TOOK leaves the pocket. The rest is still yours — the arithmetic is Core's and
        // is asserted there, so nothing here can quietly round a captain out of four rounds.
        gun.Rounds = load.Magazine;
        gun.AmmoId = load.AmmoId;
        _satchel = [.. Core.Satchel.Remove(_satchel, fed.Kind, fed.Id, load.Accepted)];
        RendererInterop.PlayCue("board");
        RequestVaultSave();

        Core.Ammunition.Kind kind = Core.Ammunition.ById(load.AmmoId);
        string line = load.Line;
        if (kind.MinimumRangeDu > 0)
        {
            line += $" It will not fire these at anything closer than {kind.MinimumRangeDu:F0} du — they " +
                "arm after travel, and that is the whole point of them.";
        }

        // #837 · …and the rounds the captain CHOSE to keep are named separately from the rounds the drum
        // would not take. Core writes both sentences; conflating them would blame a willing magazine for a
        // decision the captain made, and leave the seam between pocket and drum unaccounted for either way.
        int kept = pocket - load.Accepted;
        if (kept > load.LeftOver)
        {
            line += $" {SentryHandLoad.KeptBackLine(kept - load.LeftOver)}";
        }
        return new(true, line);
    }

    /// <summary>#837 · Can the captain's hands reach this gun? Core's amended law
    /// (<see cref="SentryHandLoad.WithinHands"/>) asked with the one fact only a running world has — how far
    /// off it is standing — and at the same arm's length the positional grip has always used. Asked in one
    /// place so the chooser's list, the chooser's hint and the act itself cannot come to three views of a
    /// reach.</summary>
    private bool WithinHandsOf(SurfaceBot gun)
    {
        double dx = gun.X - _avatarX, dy = gun.Y - _avatarY;
        return SentryHandLoad.WithinHands(
            gun.Deployed, Math.Sqrt((dx * dx) + (dy * dy)), DeckPlan.InteractRadius);
    }

    /// <summary>
    /// #803 · THE ROUNDS THE GUNS COULD NOT HOLD GO IN THE POCKET.
    ///
    /// <para>Every auto-route on this ground fills magazines in order and then stops, and until now whatever
    /// was left simply stopped existing. The receipts were not lying — they name the rounds that went IN —
    /// but a captain who watched a drawer produce twenty-two rounds into two drums that could take fourteen
    /// has been shown a thing and then had it taken away, which is the one move an object in this game is
    /// never allowed to make (#587: <i>a find that is shown once is a find that is lost</i>).</para>
    ///
    /// <para>It is also where the found-rounds item comes from at all. #603 wrote the law for rounds in a
    /// pocket, hung the door on a dry sentry and shipped — and nothing in the game ever put one there, so
    /// the whole verb was reachable only by editing a save. The overflow is the supply, and the put verb is
    /// what it is for.</para>
    ///
    /// <para>Silent when there is nothing left over, which is the ordinary case and must stay exactly as
    /// quiet as it is today.</para></summary>
    private void WhatTheDrumsCouldNotHold(int leftOver, string? ammoId = null)
    {
        if (SentryHandLoad.IntoThePocket(leftOver, ammoId) is not { } loose)
        {
            return;
        }
        _satchel = [.. Core.Satchel.Add(_satchel, loose)];
        ShowPulseMessage(SentryHandLoad.PocketedLine(loose.Count));
        RequestVaultSave();
    }
}

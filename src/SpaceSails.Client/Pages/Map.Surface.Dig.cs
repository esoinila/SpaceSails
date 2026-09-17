using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the timed dig channel, burying and lifting a chest, and the 2D6 Old Ones it raises.
public partial class Map
{
    // ── Digging [E]: a timed, abortable channel. The 2D6 roll fires at channel START so the pack can turn
    //    out and close on you WHILE the bar fills — the watch is the gameplay. Two entry points now: an own
    //    cache's ✗ console (DigSiteInteract, 'dig at the X'), and the BARE GROUND (SurfaceGroundInteract,
    //    the beach-comber kit — bury a carried chest or probe an empty hole where you stand). ──

    // The ✗ console: 'dig at the X' lifts the own cache nearest this mark. The only surviving dig CONSOLE —
    // free-form burying/probing retired the fixed ⛏ site (they ride SurfaceGroundInteract instead).
    private void DigSiteInteract()
    {
        if (_surface is not { } ex)
        {
            return;
        }
        if (AnySlowThingUnderYourHands)
        {
            return; // already channeling (dig or door-force) — stepping away aborts, [E] doesn't re-trigger
        }
        if (DigSettling)
        {
            // #452: you are standing on the ✗ you just made. Lifting it is a real choice, not the next tap.
            ShowPulseMessage("The earth's still settling. Give it a breath before you put a shovel back in.");
            return;
        }
        if (_deckPlan.NearestConsoleSpot(_avatarX, _avatarY) is not { Kind: DeckPlan.ConsoleKind.DigSite } spot)
        {
            return;
        }
        string? nearest = NearestOwnCacheId(ex.Stop.Body.Id, ex.Site.Index, spot.X, spot.Y);
        if (nearest is null)
        {
            ShowPulseMessage("The X is scuffed to nothing — no chest here.");
            return;
        }
        BeginDig(ex, DigKind.Lift, cacheId: nearest, anchorX: spot.X, anchorY: spot.Y);
    }

    // The beach-comber kit's bare-ground [E] (owner, Evening wind 2026-07-18): dig where you STAND. With a
    // chest in the sling this buries it here — bury anywhere; empty-handed it probes a hole to try your luck
    // — a fishing expedition, a first-class trip, never a dead end. Either way the ground must be reasonable
    // regolith (outside the landing band and the walls), and the D100 first decides whether it's diggable at
    // all — some ground is too hard, and the die handles that. Called from the deck E handler when no
    // console is in reach (Map.Deck); a no-op off the surface.
    private void SurfaceGroundInteract()
    {
        if (_surface is not { } ex || AnySlowThingUnderYourHands)
        {
            return;
        }

        // ── #723 · THE SHOVEL IS SOMETHING THE GROUND HAS, NOT SOMETHING THE KEY DOES ──
        //
        // Found by playing, on B1 of a Hive: [E] with empty hands in a pressurised spine corridor 150 m
        // down ran the beach-comber probe, left the orange dug square on the rockcrete, and at the canteen's
        // west face said "the shovel rings off bedrock a foot down — too hard to dig here. Try another
        // square." Both halves lie. There is no bedrock under a floor somebody invoiced — and "try another
        // square" is an INVITATION: it tells the captain that some square down here does dig, when nothing
        // can ever be buried on any square of any corridor of the building.
        //
        // The owner's answer was to gate the verb on the GROUND rather than on the keypress, so this is the
        // FIRST question the bare-ground [E] asks — ahead of the settling window too, because a captain who
        // dug on the regolith and then took the lift down would otherwise be told the earth beneath a
        // rockcrete corridor needed a breath to settle. Indoors the shovel is not in the candidate list at
        // all and the press falls through to the same honest nothing [E] gives on any other deck; the
        // too-hard line below still belongs to genuine surface squares, which is where bedrock genuinely is.
        if (!MoonSurface.ShovelWorksOnThisFloor(ex.Floor))
        {
            return;
        }

        if (DigSettling)
        {
            // #452: the shovel just came out of this ground. A held [E] must not immediately start the next
            // hole — one deliberate press per hole, or the chest goes in and out without you meaning it.
            ShowPulseMessage("The earth's still settling. Give it a breath before you put a shovel back in.");
            return;
        }
        // Safe up in the tube / aboard, or up on the landing band — no digging the fused pad.
        if (!MoonSurface.IsDiggableGround(_avatarX, _avatarY, ex.Floor))
        {
            // #319 · ShovelHasSomethingToBury, not Carrying: a captain who walked down with a file to put in
            // the ground is not out fishing, and being told there is "nothing to probe" would be the panel
            // reading his errand back to him wrong.
            ShowPulseMessage(ex.ShovelHasSomethingToBury
                ? "The landing pad's fused rockcrete — no burying here. Carry it out onto the regolith."
                : "Nothing to probe on the landing pad — it's fused rockcrete. Walk out onto the regolith.");
            return;
        }

        (int sqX, int sqY) = BeachComber.SquareOf(_avatarX, _avatarY);

        // #409: the beach-comber's metal detector screams the instant it sweeps the square that hides a lab
        // door — an INSTANT reveal (no dig), the "ping on the right seeded square". Empty-handed only (the
        // detector is the fishing kit); consumes the E. A near-miss shrieks a proximity hint but still probes.
        if (!ex.ShovelHasSomethingToBury && TrySecretLabDetectorReveal(ex, sqX, sqY))
        {
            return;
        }

        // The die's first job (owner: "some surfaces may be too hard to dig … the die could handle those").
        // Bedrock refuses the dig outright — no hole, no watch — but the square is now KNOWN and joins the
        // swept grid so the sweep reads it as checked.
        Probe probe = BeachComber.Roll(ex.Stop.Body.Id, sqX, sqY);
        if (probe.IsTooHard)
        {
            ex.Swept[(sqX, sqY)] = probe.Outcome;
            string bedrockLabTail = ex.ShovelHasSomethingToBury ? "" : SecretLabProximityTail(ex, sqX, sqY);
            RendererInterop.PlayCue(bedrockLabTail.Length > 0 ? "reveal" : "board");
            ShowPulseMessage((ex.ShovelHasSomethingToBury
                ? "⛏ The shovel rings off bedrock — this square won't take a chest. Try a step over."
                : "⛏ The shovel rings off bedrock a foot down — too hard to dig here. Try another square.") + bedrockLabTail);
            return;
        }

        // #319 · ONE BRANCH, WIDENED BY ONE TERM, and that is the whole of the "no parallel path" law in
        // this file: a chest in the sling or a thing out of the coat takes the SAME DigKind.Bury, the same
        // anchor, the same 2D6 at channel start, the same abort, the same ✗.
        if (ex.ShovelHasSomethingToBury)
        {
            BeginDig(ex, DigKind.Bury, cacheId: null, anchorX: _avatarX, anchorY: _avatarY);
        }
        else
        {
            BeginDig(ex, DigKind.Probe, cacheId: null, anchorX: _avatarX, anchorY: _avatarY, squareX: sqX, squareY: sqY);
        }
    }

    // #650 · The chest under a ✗ can only be one that is under THIS ground — same filter that drew the mark,
    // so the shovel and the map can never disagree about which site a chest is on.
    private string? NearestOwnCacheId(string bodyId, int siteIndex, double x, double y)
    {
        string? best = null;
        double bestSq = double.MaxValue;
        foreach ((string id, double cx, double cy, int _) in OwnCachePositionsAt(bodyId, siteIndex))
        {
            double d = (cx - x) * (cx - x) + (cy - y) * (cy - y);
            if (d < bestSq)
            {
                (bestSq, best) = (d, id);
            }
        }
        return best;
    }

    // Start the channel and ROLL THE WATCHDOGS NOW — the pack (if any) turns out at the edges and begins
    // to shamble in while the shovel-bar fills. No modal: the dice reveal rides the pulse line, the grid
    // stays visible so the captain watches the tide. The anchor is where the shovel bit in — stepping away
    // from HERE aborts (no more fixed console to test), and a bury records it as the ✗ (playtest bug #5).
    private void BeginDig(SurfaceExcursion ex, DigKind kind, string? cacheId, double anchorX, double anchorY, int squareX = 0, int squareY = 0)
    {
        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        ReeverRoll roll = ReeverRaid.Roll(ReeverSeed(ex.Stop.Body.Id), standing);
        ex.Channel = new DigChannel
        {
            Kind = kind, CacheId = cacheId, Roll = roll,
            AnchorX = anchorX, AnchorY = anchorY, SquareX = squareX, SquareY = squareY,
        };
        RendererInterop.PlayCue("board");
        RaiseReevers(roll); // spawn the pack (if roused) so it's already closing during the bar
        ex.Channel.Rolled = true;
        ShowPulseMessage(kind switch
        {
            // ── #319 · THE CAPTAIN'S REGISTER, AT THE DIG HERE PRESS ────────────────────────────────────
            //
            // Fable canon, verbatim (CacheDeposit.NotCoinThisTime), and this is the ONE place in the game it
            // is ever said. It is said when the thing going in is not the chest — no coin, no cargo, only
            // what came out of the coat — because that is the only case where "Digging a hole to bury the
            // chest" would be a sentence about an object the captain is not holding, which is this repo's
            // third named bug class in a pulse line.
            //
            // NOTHING ELSE IS SAID, here or anywhere. There is no notice that evidence has left the ship, no
            // banner about the search that will now find nothing, no number. A captain who buries a file and
            // is later waved through a hold inspection has to work out for himself which of those two facts
            // caused the other — the game does not announce.
            DigKind.Bury when !ex.Carrying =>
                $"⛏ {CacheDeposit.NotCoinThisTime} Hold position. Watch the tracker — step away to abort.",
            DigKind.Bury => "⛏ Digging a hole to bury the chest… hold position. Watch the tracker — step away to abort.",
            DigKind.Lift => "⛏ Working the X open… hold position. Step away to abort.",
            _ => "⛏ Sinking a probe hole… hold position. Watch the tracker — step away to abort.",
        });
    }

    // Advance the channel each frame. Stepping off the anchor aborts (chest back in hand, hole abandoned,
    // sprint begins); filling the bar completes the act.
    private void StepDigChannel(double dtRealSeconds)
    {
        if (_surface is not { Channel: { } ch } ex)
        {
            return;
        }
        // Away from where the shovel bit in → abort. (Free-form digs have no console to test, so we hold
        // the captain to the anchor point the dig started at.)
        double dx = _avatarX - ch.AnchorX, dy = _avatarY - ch.AnchorY;
        if ((dx * dx) + (dy * dy) > DeckPlan.InteractRadius * DeckPlan.InteractRadius)
        {
            AbortDig(ex);
            return;
        }

        // #456: the shovel is the loudest thing you choose to do. Every tick of the channel calls anything
        // within earshot to the HOLE — the signature trade of the surface, that the thing worth doing is the
        // thing that announces you. Walls do not muffle it.
        MakeNoise(ch.AnchorX, ch.AnchorY, ReeverHearing.Noise.Digging);

        ch.Progress += dtRealSeconds / DigChannelSeconds;
        if (ch.Progress >= 1.0)
        {
            CompleteDig(ex, ch);
        }
    }

    private void AbortDig(SurfaceExcursion ex)
    {
        DigKind? kind = ex.Channel?.Kind;
        ex.Channel = null;
        if (_reevers.Count == 0)
        {
            ShowPulseMessage("You stop digging. The hole's left half-dug.");
            return;
        }
        ShowPulseMessage(kind switch
        {
            DigKind.Bury => "🩸 You drop the shovel — the hole's abandoned. RUN (or drop the chest: press G).",
            DigKind.Lift => "🩸 You leave the X half-open. RUN.",
            _ => "🩸 You drop the shovel — the probe's abandoned. RUN.",
        });
    }

    // #452 (owner, live 2026-07-27: "it is too easy to bury and dig up by accident now by just pressing down
    // E in sequence"). Burying mints the ✗ AT YOUR FEET, so the instant the shovel goes down you are standing
    // on a dig site — and the very next [E] lifts straight back out what you just spent 3.6 seconds putting
    // in. Hold the key, or tap it twice out of habit, and the ground quietly undoes itself. So a finished
    // dig leaves the earth SETTLING: [E] will not start another one here for a beat, and says why.
    private const double DigSettleSeconds = 2.0;
    private double _digSettleUntilMs = double.NegativeInfinity;

    // True while the last dig is still settling — the guard that makes bury-then-undo a deliberate act.
    private bool DigSettling => (_lastTimestampMs ?? 0) < _digSettleUntilMs;

    private void CompleteDig(SurfaceExcursion ex, DigChannel ch)
    {
        ex.Channel = null;
        _digSettleUntilMs = (_lastTimestampMs ?? 0) + (DigSettleSeconds * 1000.0);
        switch (ch.Kind)
        {
            case DigKind.Bury:
                BuryChestHere(ex, ch.Roll, ch.AnchorX, ch.AnchorY);
                break;
            case DigKind.Lift when ch.CacheId is { } id:
                LiftChestHere(ex, id, ch.Roll);
                break;
            case DigKind.Probe:
                ProbeHere(ex, ch.SquareX, ch.SquareY);
                break;
        }
    }

    // The carried chest goes into the ground AT THE ANCHOR — where the shovel dug, recorded on the cache so
    // the ✗ and 'dig at the X' land exactly there (playtest bug #5, no more hash-scatter). Invisible to
    // confiscation by construction; the presence LEFT on the chest is the pack that turned out (the standing
    // watchdog level, hardened by this roll).
    private void BuryChestHere(SurfaceExcursion ex, ReeverRoll roll, double digX, double digY)
    {
        // #319 · IS THERE A CHEST IN THIS HOLE, asked ONCE and at the top. Two readers below need the answer
        // after the world has already moved under it — the mint's own flags, and the noun the pulse line uses
        // — and `ex.Carrying` is a live read that both `ex.Buried` and the chip's manifest line change on the
        // way past. A question answered twice against two different worlds is how a sentence and a sim come
        // to disagree, which is this ground's third named bug class.
        bool theChestGoesIn = ex.Carrying;

        // …and ONLY a chest that is actually in the sling spends the purse. Before #319 that was the same
        // question as "is this a bury at all"; it is not any more, because a captain who dropped his chest to
        // sprint and then buried the file in his coat would otherwise have paid for a chest still lying out
        // on the regolith with its own ✗ to come.
        int coin = theChestGoesIn ? Math.Clamp(ex.PendingCoin, 0, _credits) : 0;
        _credits -= coin;

        // Only what is IN THE CHEST leaves the books. The chest is a snapshot taken at the shuttle door
        // (ShuttleExcursion.Pack); the hold keeps living all the way down — a cache dug back up on this
        // same ground, a beach-comber scrap recovered after a panic drop. Clearing the whole hold here
        // therefore ATE those units: the map card and the "off the books" line name only the snapshot, so
        // they were neither buried nor aboard. Coin was always deducted honestly (the pending amount, no
        // more); this is cargo's half of the same law. ShuttleExcursion.HoldAfterBurying owns the rule.
        //
        // #319 · …and, for the reason one block up, only when the chest is really in the captain's arms.
        List<CacheCargo> manifest = theChestGoesIn ? ex.PendingCargo : [];
        if (theChestGoesIn)
        {
            var left = ShuttleExcursion.HoldAfterBurying(_cargoByClass, ex.PendingCargo);
            _cargoByClass.Clear();
            foreach (KeyValuePair<string, int> line in left)
            {
                _cargoByClass[line.Key] = line.Value;
            }
            RecomputeCargoTotals();
        }

        // #233 · …and what the captain is carrying ON HIM can go in the hole too, if it is the one thing in
        // the game that is evidence rather than goods. Added AFTER the hold has been settled, because the
        // chip was never in the hold and HoldAfterBurying must not be asked to subtract it from anything;
        // added BEFORE the mint, because the manifest that goes into the ground is the one that comes back
        // out of it. Nothing is said — see Map.Blackmail.
        //
        // #319 · It runs FIRST, and that settles the one overlap between the two: a captain who picked the
        // chip itself as his deposit has it taken here, listed on the manifest by its canon name and flagged
        // hot exactly as #233 ships it, and the coat is then empty when the general path asks. One object, one
        // hole, one entry — never two.
        TheChipGoesInTheChest(manifest);

        // #319 · …AND SO CAN ANYTHING ELSE LIGHT ENOUGH, which is the generalisation #233 was one hand-built
        // case of. The row leaves the satchel HERE, at the shovel, and nowhere earlier: the pick was made at
        // the shuttle door and a whole excursion happens in between, so what goes in the hole is the row as
        // it is in the pocket RIGHT NOW (CacheDeposit.AsCarried) — four rounds picked at the door and two
        // fired on the way out bury two — and a thing that was read, filed, offered or set down on the way is
        // simply not there any more and nothing is said about it.
        //
        // This is also the whole of clause 2. There is no "off the ship" flag to raise and no evidence reader
        // to teach: everything in this game that asks what the captain has on him asks this one satchel, so a
        // thing that has left it is absent from every one of them by construction.
        IReadOnlyList<Core.Satchel.Item> deposit = TakeTheThingOutOfTheCoat(ex);

        int standing = WatchdogLevelAt(ex.Stop.Body.Id);
        int presence = Math.Max(standing, roll.Reevers);
        // #650 · The chest goes into THIS ground, not "somewhere on this moon": the site the shuttle set down
        // at rides the cache, so the ✗ is drawn — and dug — only here, and the map card names the place.
        // #455 · THE CARRY, RECORDED. How far the captain walked this chest out from the pad is the term the
        // whole issue turns on ("the same distance that makes the walk dangerous is what makes the cache
        // safe"), and Core measures it — the client hands over the spot, never a distance of its own
        // devising, because a geometry literal on this side of the wall is this repo's bug class 1.
        TreasureCache cache = _caches.Bury(
            ex.Stop.Body.Id, coin, manifest, SimTime, "you", playerOwned: true, presence, digX, digY,
            siteIndex: ex.Site.Index,
            buried: true, padDistance: CacheSafety.PadDistanceOf(digX, digY),
            // #319 · …in the SAME chest. One record: one ledger row, one map card, one vault section, one
            // rebirth, one CacheSafety read on the way out, one set of #316 marks if a rival gets here first.
            deposit: deposit);
        SeedDiscoveryWatch();

        // #319 · …and the flag that says the CHEST is down only goes up when the chest went down. A deposit-only
        // bury that raised it would strand a dropped chest forever: `Carrying` reads `!Buried`, so the captain
        // could walk back to his own pile, pick it up, and find the shovel would no longer take it.
        ex.Buried = theChestGoesIn || ex.Buried;
        ex.Cache = cache;
        RebuildSurfaceDeck(); // the chest is down; the new ✗ joins the ground where you dug
        RequestVaultSave();
        // #380 item 6 (owner ruling 2026-07-19: "new players are left mystified") — the discovery risk was
        // taught only at the moment of loss. One line at bury time: rivals may dig it up over the coming
        // days, and Reever-haunted ground keeps it safer.
        //
        // #455 · …and now that sentence has real numbers behind it. The rung read is the SAME call the
        // return-trip roll makes (TreasureCache.Safety → CacheSafety.Read), so the promise made here is
        // literally the threshold the dice will be compared against while the captain is away.
        //
        // #316 law 3 · WHICH MEANS IT HAS TO KNOW ABOUT THE MESS HE MADE GETTING HERE. The watch prices this
        // chest against the husks lying on this ground; a bury line that read the chest alone would quote a
        // safe he does not have — this repo's third named bug class, the sentence and the sim disagreeing —
        // and would quote it in the one case the captain most needs told: the loud stand he just made.
        //
        // #319 · The NOUN is the one thing that moves. "Chest buried" in front of a hole that has one folded
        // sheet in it is the sim doing one thing while the sentence reports another; everything after the
        // noun — the manifest, the safety read, the warning, the instruction — is the chest's own line to the
        // character, because it is the chest's own hole.
        string what = theChestGoesIn ? "Chest buried" : "In the ground";
        ShowPulseMessage($"⛏ {what} — {cache.ContentsLine()} off the books. The ✗ marks this spot. {cache.SafetyWith(TheFightThisGroundCarries(cache)).Sentence} Rivals may dig it up over the coming days; the more Reevers haunt this ground, the safer it stays. Now get back to the shuttle.");
    }

    /// <summary>
    /// #319 · <b>THE THING COMES OUT OF THE COAT AND GOES IN THE HOLE.</b> The one place a satchel row is
    /// spent on a burial, and the one place clause 2 of the issue is kept.
    ///
    /// <para>Owner: <i>"hiding evidence of our piracy without losing it… something too important to risk
    /// carrying it around."</i> The whole promise is that the thing stops being ON HIM and goes on EXISTING.
    /// The second half is the cache the caller mints; this is the first, and it is one line of arithmetic
    /// because there is exactly one place the game keeps what a captain has on him. <c>_satchel</c> is what
    /// every single reader that asks "what is he carrying" asks — the bin's rip list, the desk's spread, the
    /// guard's wallet fan, the dark-web desk, the blackmail client, the wallet a locked door is offered, the
    /// authority ids the lift reads, the vault section that saves him. There is no evidence flag to lower and
    /// no reader to teach: a row that has left this list is gone from all of them at once, which is why the
    /// audit in the pull request is a table of readers and not a table of edits.</para>
    ///
    /// <para><b>What is buried is the row as it is NOW</b>, not the row as it was picked. The pick happened at
    /// the shuttle door and a whole excursion has happened since: rounds get fired, papers get read and filed,
    /// folders come apart, things get offered at doors and set down on the ground. So the pocket is asked
    /// again here (<see cref="CacheDeposit.AsCarried"/>) and the hole takes what is actually in the hand —
    /// four rounds picked and two fired bury two, and a thing that is gone is simply not buried. Nothing is
    /// said about it: a captain who spent the thing knows he spent it.</para>
    ///
    /// <para>Returns an empty list, never null, so the mint's own <c>Count: &gt; 0</c> test writes no vault
    /// key for a hole with nothing of his in it — the byte-for-byte legacy round-trip.</para>
    /// </summary>
    private IReadOnlyList<Core.Satchel.Item> TakeTheThingOutOfTheCoat(SurfaceExcursion ex)
    {
        if (ex.PendingDeposit is not { } picked)
        {
            return [];
        }

        ex.PendingDeposit = null; // the shovel spends the pick whether or not the thing is still there
        if (CacheDeposit.AsCarried(_satchel, picked) is not { } asItIsNow)
        {
            return [];
        }

        _satchel = [.. Core.Satchel.Remove(_satchel, asItIsNow.Kind, asItIsNow.Id, asItIsNow.Count)];
        return [asItIsNow];
    }

    /// <summary>
    /// #319 · <b>AND BACK OUT AGAIN.</b> What a dug-up hole hands back to the coat, said in the same voice the
    /// hold's own "{n}u left — hold full" is said in: what fits goes in, and what does not is named.
    ///
    /// <para>The satchel's own law decides, through the satchel's own question
    /// (<see cref="Core.Satchel.CanTake"/>) — which is #678's law and not a new one: a thing the pocket
    /// refuses must not be silently destroyed. A refused row is put BACK IN THE GROUND, in a fresh hole at the
    /// same ✗ with the same terms, because "it is still down there" is the only honest thing a hole can say
    /// about a thing a captain could not carry away from it. The wallet never fills, so the case a player
    /// actually meets is a pocket stuffed with relic paperwork and a tool coming back up.</para>
    ///
    /// <para>Returns the tail for the dig's own pulse line, empty when everything came home.</para>
    /// </summary>
    private string TheCoatTakesBackWhatItCan(SurfaceExcursion ex, TreasureCache dug)
    {
        if (dug.Deposit is not { Count: > 0 } things)
        {
            return "";
        }

        var refused = new List<Core.Satchel.Item>();
        foreach (Core.Satchel.Item item in things)
        {
            if (Core.Satchel.CanTake(_satchel, item))
            {
                _satchel = [.. Core.Satchel.Add(_satchel, item)];
            }
            else
            {
                refused.Add(item);
            }
        }

        if (refused.Count == 0)
        {
            return $" {CacheDeposit.ManifestLine(things.Count)} back in the satchel.";
        }

        // Back in the ground it goes, through the SAME mint every other burial goes through and with the
        // chest's own terms handed back to it, so the thing the captain could not carry is under exactly the
        // odds it was under a second ago rather than under a fresh, kinder reading of the same hole.
        _caches.Bury(
            dug.BodyId, coin: 0, [], SimTime, "you", playerOwned: true, dug.ReeverLevel,
            dug.DigX, dug.DigY, dug.SiteIndex, dug.Buried, dug.PadDistance, refused);
        SeedDiscoveryWatch();
        int home = things.Count - refused.Count;
        string got = home > 0 ? $" {CacheDeposit.ManifestLine(home)} back in the satchel." : "";
        return got + $" {CacheDeposit.ManifestLine(refused.Count)} stays down there — no room on you for it.";
    }

    // ── #455 rule 2 · A CHEST YOU RAN AWAY FROM IS STILL A CHEST ──────────────────────────────────────
    //
    // Owner: "buried beats dropped, BY A LOT … on a return trip the buried one should usually still be
    // there; the dropped one gets a harder roll — it is lying in the open where anyone can see it."
    //
    // Before this, a dropped chest was not a worse cache, it was not a cache at all: the pile was excursion-
    // scoped and evaporated at liftoff with the coin and cargo still on the ship's books (the story-QA audit
    // on this issue called it "a free sprint", and #648 at least made the liftoff line say so out loud).
    // That left the whole "dropped" half of the safety oracle unreachable in play — a rule with no world
    // behind it. So a chest left on the regolith now goes into the ledger EXACTLY as a bury does, with the
    // same deductions and the same map card, and one difference: it is flagged as lying in the open, which
    // is what buys it the harder roll for as long as it stays there.
    //
    // Returns the minted cache, or null when there was nothing left behind (or nothing in the chest).
    private TreasureCache? LeaveTheDroppedChestInTheOpen(SurfaceExcursion ex)
    {
        if (!ex.ChestDropped || (ex.PendingCoin <= 0 && ex.PendingCargo.Count == 0))
        {
            return null;
        }

        int coin = Math.Clamp(ex.PendingCoin, 0, _credits);
        _credits -= coin;

        // The same law a bury obeys (ShuttleExcursion.HoldAfterBurying): only what is IN THE CHEST leaves
        // the books — the hold went on living all the way down and the rest of it comes home.
        var left = ShuttleExcursion.HoldAfterBurying(_cargoByClass, ex.PendingCargo);
        _cargoByClass.Clear();
        foreach (KeyValuePair<string, int> line in left)
        {
            _cargoByClass[line.Key] = line.Value;
        }
        RecomputeCargoTotals();

        TreasureCache cache = _caches.Bury(
            ex.Stop.Body.Id, coin, ex.PendingCargo, SimTime, "you", playerOwned: true,
            reeverLevel: WatchdogLevelAt(ex.Stop.Body.Id),
            digX: ex.DropX, digY: ex.DropY, siteIndex: ex.Site.Index,
            // The two terms that make this the harder roll: no shovel went in, and no carry is credited —
            // the chest was not placed anywhere, it fell where the captain's legs gave out.
            buried: false, padDistance: CacheSafety.PadDistanceOf(ex.DropX, ex.DropY));
        SeedDiscoveryWatch();
        RequestVaultSave();
        return cache;
    }

    // The beach-comber probe resolves (the fishing expedition's payoff, or its honest shrug). The D100
    // already ruled out bedrock at BeginDig, so this hole turned up either nothing (the common case,
    // "unlucky … but still possible") or a rare shallow find — a little coin and maybe a scrap. Modest by
    // design: luck, never an economy. Either way the square joins the per-visit swept grid.
    private void ProbeHere(SurfaceExcursion ex, int squareX, int squareY)
    {
        Probe probe = BeachComber.Roll(ex.Stop.Body.Id, squareX, squareY);
        ex.Swept[(squareX, squareY)] = probe.Outcome;

        // #411: a rare seeded square on an outer icy moon hides a cold KAAMOS supply pod — a cargo run that
        // never arrived, distinct from ordinary treasure. Sweeping it the first time assembles cold-pod (and
        // may open the reach). Once held, the square is ordinary regolith and the normal probe result stands.
        if (!_kaamos.Has("cold-pod") && KaamosPodHere(ex.Stop.Body.Id, squareX, squareY))
        {
            TryAssembleKaamos("cold-pod",
                "❄ Your probe rings off metal a foot down — not a coin, a HULL. You clear the frost and it's a " +
                "SEALED SUPPLY POD, decades cold. " + KaamosLore.ById("cold-pod")!.Lore);
            return;
        }

        // #409: a near-miss on a hidden lab door — the detector shrieks that something big and metal is very
        // close, keep sweeping the squares around here (tacked onto the honest probe result).
        string labTail = SecretLabProximityTail(ex, squareX, squareY);

        if (!probe.IsFind)
        {
            RendererInterop.PlayCue(labTail.Length > 0 ? "reveal" : "board");
            ShowPulseMessage("🕳 Nothing but regolith down there. The detector stays quiet — you mark the square and move on." + labTail);
            return;
        }

        // A shallow find: pocket the coin, and take the scrap if the hold has room (else leave it — a
        // scrap's not worth a sprint). Small numbers on purpose.
        _credits += probe.FindCoin;
        int scrapTaken = 0;
        if (probe.FindScrapUnits > 0 && _cargoUnits < CargoCapacity)
        {
            int take = Math.Min(probe.FindScrapUnits, CargoCapacity - _cargoUnits);
            _cargoUnits += take;
            _cargoValue += take * CargoMarket.UnitValue(BeachComber.FindCargoClass);
            _cargoByClass[BeachComber.FindCargoClass] = _cargoByClass.GetValueOrDefault(BeachComber.FindCargoClass) + take;
            scrapTaken = take;
        }
        RendererInterop.PlayCue("reveal");
        RequestVaultSave();
        string scrapTail = scrapTaken > 0 ? $" + {scrapTaken} scrap of salvage" : "";
        ShowPulseMessage($"✨ The detector chirps — you turn up {probe.FindCoin:N0} cr{scrapTail} a few inches down. Luck, not a fortune. Mark it and keep moving." + labTail);
    }

    private void LiftChestHere(SurfaceExcursion ex, string cacheId, ReeverRoll roll)
    {
        if (_caches.Dig(cacheId) is not { } c)
        {
            return;
        }
        _credits += c.Coin;
        int unitsBack = 0, unitsLost = 0;
        foreach (CacheCargo line in c.Cargo)
        {
            int room = CargoCapacity - _cargoUnits;
            int take = Math.Min(room, line.Units);
            if (take > 0)
            {
                _cargoUnits += take;
                _cargoValue += take * CargoMarket.UnitValue(line.CargoClass);
                _cargoByClass[line.CargoClass] = _cargoByClass.GetValueOrDefault(line.CargoClass) + take;
                unitsBack += take;
            }
            unitsLost += line.Units - take;
        }
        // #319 · …and whatever the captain buried out of his own coat goes back into it, through the satchel's
        // own CanTake. The ONE recovery, on the ONE dig: there is no second verb and no second ✗ — the issue's
        // "recoverable by the same dig at the ✗", to the letter.
        string coat = TheCoatTakesBackWhatItCan(ex, c);
        CompleteFetchCacheFor(c);
        _ = roll; // the pack already turned out at channel start
        RebuildSurfaceDeck(); // the ✗ is gone
        RequestVaultSave();
        string lost = unitsLost > 0 ? $" ({unitsLost}u left — hold full)" : "";
        ShowPulseMessage($"🗺 Dug up {c.Coin:N0} cr + {unitsBack} units{lost}.{coat} Back to the shuttle.");
        PayCompletedQuests();
    }

    // The panic choice (owner's unruled carry-speed, settled): DROP the chest to run full speed. The
    // dropped chest stays on the grid to recover (walk back onto it and [E]).
    private void DropChest()
    {
        if (_surface is not { Carrying: true } ex)
        {
            return;
        }
        ex.ChestDropped = true;
        ex.DropX = _avatarX;
        ex.DropY = _avatarY;
        // #456: a chest hitting regolith is one sharp report. You dropped it to run — and the sound tells
        // anything close where you just were, which is exactly the cost of that trade.
        MakeNoise(_avatarX, _avatarY, ReeverHearing.Noise.Clatter);
        if (ex.Channel is not null)
        {
            ex.Channel = null;
        }
        RebuildSurfaceDeck();
        RendererInterop.PlayCue("alarm");
        // #455 rule 3 · TELL HIM AT COMMIT TIME. A drop is a real hiding place now — lift off without it and
        // it stays in the ground as an OPEN cache (Map.Surface, the liftoff seam) — so the same oracle that
        // prices a bury prices this, read here for the chest as it would be LEFT: in the open, on this
        // ground. He is deciding whether to come back for it, and this is the number that decides it.
        // #316 law 3 · …priced against the fight this ground is already carrying, exactly as the bury line
        // is. A chest abandoned in the middle of a firefight is the loudest hiding place in the game.
        CacheSafetyRead read = CacheSafety.Read(
            CacheSafety.PadDistanceOf(_avatarX, _avatarY), buried: false, WatchdogLevelAt(ex.Stop.Body.Id),
            TheFightThisGroundCarries(ex));
        ShowPulseMessage($"🪤 Chest dropped! {read.Sentence} Full sprint now — come back for it when the ground's clear.");
    }

    private void TryRecoverDroppedChest()
    {
        if (_surface is not { ChestDropped: true } ex)
        {
            return;
        }
        double d = Math.Sqrt((_avatarX - ex.DropX) * (_avatarX - ex.DropX) + (_avatarY - ex.DropY) * (_avatarY - ex.DropY));
        if (d <= DeckPlan.InteractRadius)
        {
            ex.ChestDropped = false;
            RebuildSurfaceDeck();
            RendererInterop.PlayCue("board");
            ShowPulseMessage("🧰 Chest back in the sling.");
        }
    }

    // ── The 2D6 Old Ones: turn out, spawn converging from the edges, and NEVER stop. ──

    private void RaiseReevers(ReeverRoll roll)
    {
        if (!roll.Roused)
        {
            ShowPulseMessage($"🎲 {roll.Describe()} — the ground stays quiet. For now.");
            return;
        }
        SpawnReevers(roll.Reevers);
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage($"🎲 {roll.Describe()} — the OLD ONES stir! {roll.Reevers} shamble up from the regolith, converging. Patient, ancient, and many. Don't get cornered.");
    }

    // Spawn a pack spread across the deep field so they converge from several bearings (not single file)
    // onto the captain and the tube line — the motion-tracker "wall of signal" moment.
    private void SpawnReevers(int count)
    {
        double baseY = Math.Min(_avatarY - 4, MoonSurface.AnchorY + 10);
        for (int i = 0; i < count; i++)
        {
            if (_reevers.Count >= ReeverEngineCeiling)
            {
                break;
            }
            double frac = count > 1 ? i / (double)(count - 1) : 0.5;
            double x = -40 + frac * 70 + (i % 2 == 0 ? -3 : 3);
            double y = baseY - (i % 3) * 4;
            _reevers.Add(new Reever
            {
                X = x, Y = Math.Min(y, MoonSurface.ReeverBarrierY - 1), Facing = Math.PI / 2,
                // Seed the thermal shuffle off the excursion threat seed + the spawn ordinal so each pack
                // member shivers on its own phase (client-only, like the position itself — never saved).
                JitterSeed = ((_surface?.ThreatSeed ?? 0UL) * 0x9E3779B97F4A7C15UL) + (ulong)i + 1UL,

                // #459 (owner, live 2026-07-27: "I did not see any reevers last time… were there any?" —
                // "Not having any is major bug"). THIS pack is roused BY the shovel: the line the player is
                // reading as they spawn literally says they "shamble up from the regolith, CONVERGING".
                // After #446 they were born unaware, so they converged on nothing — they stood where they
                // rose, and standing still they are invisible to a motion-only tracker too. The whole
                // dig-under-threat loop silently became an empty field.
                //
                // They know the DIG, not the captain: LastSeen is the hole, exactly as #456's ear hands out
                // a PLACE rather than a target. Walk away from the noise you made and they still arrive at
                // it. #446's unaware feature is untouched — it governs the Old Ones already standing on the
                // ground when you get there (the tide's), which is the case the owner described.
                EverSeen = true,
                LastSeenX = _avatarX,
                LastSeenY = _avatarY,
            });
        }
    }
}

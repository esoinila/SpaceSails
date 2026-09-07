using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — WHAT HAPPENS WHEN ONE REACHES
// YOU. `ReeverCatch` is the brush that prices a caught digger in heat and nerve, and #453's exchange is the
// rest: five blows, a die between each one and your skin, blood on the regolith when one gets through, and
// #466's rule that a blow lands only when the two bodies TOUCH with nothing standing between them.
// `CaptainBeyondReach` is the one fact that ends all of it — up the tube or past the shuttle lock, nothing
// reaches you there — and it is read by five other files besides this one.
public partial class Map
{
    // A caught digger: no loot taken (the whole point) — it prices the danger in heat, the same lever the
    // law's collectors use. Debounced so one brush isn't a stunlock.
    //
    // #380 item 1 — NOT a death today (owner constraint: don't build the surface-death / insurance-captain
    // mechanic here, just route what exists). A Reever's hand raises heat + shocks the nerve; the captain is
    // told to RUN, not resurrected. When the surface-death lane lands, this is the site that would classify
    // the death via DeathNarration.SurfaceEnd(_nerve, seed) → DeathCause.Reevers / .Joined and hand it to the
    // shared BUSTED resurrection (Cause + DeathBodyName on the encounter); the art + lines are already wired.
    private void ReeverCatch()
    {
        double now = _lastTimestampMs ?? 0;
        if (now - _lastReeverCatchMs < 1500)
        {
            return;
        }
        _lastReeverCatchMs = now;

        // #380 item 1 / Evening wind #20 — THE OVERDRAW. Nerves already bottomed out and an Old One lays
        // hands ANYWAY: this qualifying hit breaks the captain. Read on the nerve BEFORE the touch shock
        // (already empty + more damage), routed place-dependently (the Old Ones took you — or, rarely, you
        // joined them) into the shared BUSTED resurrection, where the piracy insurance issues a new captain.
        // Fail Forward — the run continues (ledger, ship and hoards persist). Below empty is where it breaks;
        // above it, the touch only floors the gauge and the captain is told to RUN, as before.
        if (_surface is { } dying && _busted is null && CaptainSuccession.OverdrawQualifies(_nerve))
        {
            TriggerSurfaceOverdrawDeath(dying, nerveRanOut: true); // the gauge broke first
            return;
        }

        if (_surface is { } ex)
        {
            ex.Catches++;
        }
        // #580 · NO SHIP HEAT FROM A HAND ON YOUR SUIT. This used to raise _heat by one per catch, and that
        // was wrong twice over. Owner: "moving on the planet should NOT cause HEAT" / "any heat should happen
        // on the surface or site, not in space" / "we don't want to be guarding our parking lot ... that is
        // not good game play :-D".
        //
        // He is right on the fiction and on the play. HEAT is what the collectors and the law hold against
        // your SHIP, earned by robbery and piracy and hot cargo — an Old One grabbing a suit on Miranda tells
        // nobody anything, and there is no ledger out here to be entered in. On the play side the coupling
        // was worse than untidy: it turned every excursion into a slow tax on the parked ship, so a good long
        // walk came home to wolves. The site's own pressure is ex.Catches, above, and that stays local.
        //
        // Same class as the Debt Collector deaths he caught earlier: a space-side consequence reaching down
        // onto a moon where it has no business being.
        // #480 · The nerve price of a hand on you is decided by NervePips, not here: ONE pip, ONCE per
        // encounter (owner: "repeated strikes should not cost more of sanity … we already take medical hit
        // from reever"), and again on every hand once the captain is nearly gone. We only report the event.
        _touchedThisFrame = true;
        RendererInterop.PlayCue("alarm");
        ShowPulseMessage("🩸 An Old One lays hands on you — it wants no loot, only you. Tear free and RUN!");
        RequestVaultSave();
    }

    // ── #453 · THE EXCHANGE: five blows, and a die between each one and your skin ──────────────────────
    //
    // Owner, 2026-07-27: "player health could be like 5 reever hits but the reever sphere must touch the
    // player sphere when a hit is received. Player should have some melee blocking ability. Dice throw. We
    // should narrate what happens to the player. Maybe a splash of blood when reever hit goes through
    // players attempt to block it. :-D"
    //
    // A swing resolves ONLY on real contact — the two bodies touching, not merely near — and every Old One
    // winds up on its own cadence, so being held at arm's length by the pack shove (#441) is not a blender.
    private double _bloodUntilMs = double.NegativeInfinity;

    // Blood on the regolith for a moment after a blow gets through — the surface has never had visual
    // punctuation for being hurt, and "you are bleeding" should not be something you read in a corner.
    private bool BloodShowing => (_lastTimestampMs ?? 0) < _bloodUntilMs;

    // #466: a blow lands only when the two bodies TOUCH and nothing stands between them. Stone (and a shut
    // door) stops an arm exactly as it stops a round — otherwise a Reever pressed against the far face of a
    // slab is close enough to kill you through it.
    private bool CanSwingAt(Reever r, IReadOnlyList<SurfaceCollision.Segment> sight)
    {
        // #471: contact is "at arm's length or nearer", and it must include EXACTLY arm's length. The
        // keep-off-the-captain shove (#453) parks a crowding Old One at precisely PersonalSpace — the very
        // same 1.4 that is the touch distance — so a strict comparison left every one of them a floating
        // hair too far away to ever swing. Playtested: three pressed against the captain, nerve shot, and
        // the condition still read "unmarked" because not one blow could register. A hair of tolerance.
        const double reach = CaptainCondition.TouchDistance + 0.05;
        double dx = r.X - _avatarX, dy = r.Y - _avatarY;
        if ((dx * dx) + (dy * dy) > reach * reach)
        {
            return false;
        }
        return SurfaceCollision.HasLineOfSight(r.X, r.Y, _avatarX, _avatarY, sight);
    }

    /// <summary>
    /// WHERE NOTHING CAN REACH THE CAPTAIN, on whichever thing they are standing.
    ///
    /// <para>Owner, standing shoulder to shoulder with an Old One aboard a wreck with a full nerve bar and
    /// five unmarked condition pips: <i>"look I take no damage or sanity loss from reever now."</i> He was
    /// exactly right, and it was never once possible. Both the blow and the being-caught were gated on
    /// <c>MoonSurface.IsSafeAboard</c>, which asks whether the captain is above the regolith's top rim at
    /// y = −20 — and a wreck's ENTIRE deck runs from −9 to +9. Every square metre of every derelict has
    /// always been "safely up the tube at the ship".</para>
    ///
    /// <para>The FOURTH bug of exactly this shape this weekend (the regolith tide aboard, the moon barrier
    /// clamping the pack outside the hull, the moon spawn point, and now this). The pattern is a MOON
    /// CONSTANT GOVERNING A SHIP, and it hides so well because the moon's number is not absurd for a wreck
    /// — it is merely satisfied everywhere, so the feature silently never fires and nothing ever errors.</para>
    ///
    /// <para>Aboard, safety is not a latitude. It is the shuttle's own lock: past that bulkhead is the away
    /// team's side and nothing follows you there, which is the same crew-only-door law the tube obeys.</para>
    /// </summary>
    /// <para>#621 · And the answer now lives in <see cref="AwayTeamSide.BackAtTheShuttle"/>, because the AIR
    /// needed the same fact and worked it out for itself with the moon's rule alone — the same bug, in the
    /// one instrument a captain cannot survive being lied to by. Two places computing one fact is the bug
    /// even while they agree.</para>
    private bool CaptainBeyondReach =>
        AwayTeamSide.BackAtTheShuttle(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius);

    private void ResolveReeverSwings(double nowMs)
    {
        if (_surface is not { } ex || _busted is not null || CaptainBeyondReach)
        {
            return; // up the tube, or past the shuttle lock — nothing reaches you there
        }

        // Who has a hand on you RIGHT NOW: bodies touching, the owner's rule. Counted first, because being
        // swarmed is itself a penalty on the block — every one past the first is another thing to watch.
        // #466 (owner, live 2026-07-27: "The reevers killed me through a wall there"). Touching is not
        // enough — a body a hair from yours on the FAR SIDE of a thin slab is still 1.4 units away, and the
        // swing landed through the stone. A blow needs a clear line as well as contact: the same sight law
        // the eyes and the guns obey (#324/#438), shut doors included (#465).
        IReadOnlyList<SurfaceCollision.Segment> sight = SightBlockers();
        int touching = 0;
        foreach (Reever r in _reevers)
        {
            if (CanSwingAt(r, sight))
            {
                touching++;
            }
        }
        if (touching == 0)
        {
            return;
        }

        foreach (Reever r in _reevers)
        {
            if (!CanSwingAt(r, sight))
            {
                continue;
            }
            if (nowMs - r.LastSwingMs < CaptainCondition.SwingCooldownSeconds * 1000.0)
            {
                continue; // still winding up
            }
            r.LastSwingMs = nowMs;

            // #696 · SOMETHING GOT A HAND ON YOU, AND THE EXPOSURE IS GONE. Placed before the block roll on
            // purpose: being REACHED is what ends the hold, not being hurt by it. A captain who turns a blow
            // aside has still had somebody's arm come through the space they were photographing into, and a
            // darkroom that survived that would be telling them the ground is safer than it is — which is
            // the whole thing the twenty seconds were bought to say.
            ProcessingIsInterrupted(Core.Processing.Interruption.Reached);

            // The die, seeded off this contact and its swing count so a long fight never repeats itself.
            r.Swings++;
            ulong seed = DiceRule.Seed(r.JitterSeed, $"swing:{r.Swings}");
            DiceRoll roll = CaptainCondition.BlockRoll(seed, _nerve, ex.Carrying, touching);

            if (CaptainCondition.Resolve(roll) == CaptainCondition.Exchange.Blocked)
            {
                // #467: its own voice. A block RINGS — bright, hard, over in a blink — so it can never be
                // confused with the blow that gets through (owner: "I should know when I'm hurt").
                ShowPulseMessage($"🛡 {CaptainCondition.BlockLine(seed)}");
                RendererInterop.PlayCue("block");
                if (_showVentPanel)
                {
                    _ventMessage = $"🛡 {CaptainCondition.BlockLine(seed)}";
                }
                continue;
            }

            // It got through. One of the five, blood on the ground, and the old touch cost on top.
            ex.HitsTaken++;
            _bloodUntilMs = nowMs + 900;
            ShowPulseMessage($"🩸 {CaptainCondition.HitLine(seed)}");
            if (_showVentPanel)
            {
                // The pulse message lives on the canvas, and the board is standing on top of the canvas.
                // A blow landed while reading the panel has to arrive ON the panel or it never happened.
                _ventMessage = $"🩸 {CaptainCondition.HitLine(seed)}";
            }
            // #467: low, wet and wrong — nothing else in the game sounds like this. And at one pip left the
            // game stops being subtle about it: a floor-level dread tone on top, every single time.
            RendererInterop.PlayCue("wound");
            if (CaptainCondition.MaxHits - ex.HitsTaken == 1)
            {
                RendererInterop.PlayCue("last");
            }
            // #480: the blow already charged the body. The nerve is charged once for being CAUGHT (and
            // again every time once you are nearly gone) — NervePips decides, we only report it.
            _touchedThisFrame = true;
            RequestVaultSave();

            if (CaptainCondition.IsDown(ex.HitsTaken))
            {
                // The fifth blow. Routed into the SAME staged death the overdraw uses, so the piracy
                // insurance issues a new captain and the run continues (Fail Forward) — the ship, the
                // ledger and every buried cache outlive you (#455's rebirth thread).
                // The FIFTH BLOW — the condition marker decided, not the nerve. Since #480 this is the
                // common surface death, and it must not narrate as an overdraw.
                TriggerSurfaceOverdrawDeath(ex, nerveRanOut: false);
                return;
            }
        }
    }
}

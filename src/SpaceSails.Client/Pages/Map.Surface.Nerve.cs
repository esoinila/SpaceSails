using SpaceSails.Client.Rendering;
using SpaceSails.Core;
using SpaceSails.Core.Interior;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — the nerve gauge, its pips, its ledger and the noise that moves it.
public partial class Map
{
    // ── #317 The nerve gauge: the regolith frays it, the ship's safety eases it, the monolith gores it. ──

    // The one per-frame nerve advance, called from the sim loop every tick (not just on the surface): the
    // pure NerveModel.Advance owns the whole on-planet law — drain only out on the regolith (moving contacts,
    // a live chase, digging under threat, being cornered), the once-in-a-life monolith first-sight hit (the
    // #226 hook #318 named), and the airlock/off-planet ease-off (the ship is safety). The client's only job
    // is to read the live situation and, when the big hit fires, sound the cue and speak.
    private void StepNerve(double dtRealSeconds)
    {
        bool onExcursion = _surface is { } ex;
        // #591 · A SHELTER STEADIES YOU. Owner: "we should restore sanity when we get to a shelter also."
        //
        // No new machinery was needed and that is the tell that it is the right rule: nerve already comes
        // back one beat at a time whenever the captain is SAFE (NervePips.Cause.Airlock), and safe has always
        // meant "aboard, or in her tube". A pressurised drum with a door nothing outside can work is safe by
        // exactly the same definition — it is the reason the air stops there too (#573). Saying so here is
        // the whole change.
        //
        // It also completes the building's argument. It gives you air, it reloads you, it keeps the Old Ones
        // at the threshold — and until now you sat in it watching the one gauge that measures whether you can
        // keep doing this go on falling.
        // #585: a pressurised floor of the Hive steadies you for the same reason a shelter does - it is warm,
        // it is lit, and nothing outside can work its doors. A DEAD floor does not, which is most of them.
        bool inShelter = onExcursion && _surface is { } shelterEx
            && (ShelterUnderfoot(shelterEx).Found
                || (shelterEx.Floor < 0 && UndergroundComplex.HoldsPressure(shelterEx.Stop.Body.Id, shelterEx.Floor)));
        // #637 · AND A DERELICT IS EXPOSED GROUND TOO. This asked the moon's question — "are you above the
        // regolith's top rim at y = −20" — and a wreck's whole deck runs −9..+9, so aboard a hull it was
        // always FALSE: the ambient pressure the entire dread economy runs on never applied inside a ship,
        // `?wreck=infested` included. A captain could walk the spine of a haunted hull, in the dark, in
        // vacuum, and the gauge scored it as standing in the shuttle bay. The damage half of that same
        // constant was fixed in #574 and the air half in #621; this is the sanity half, one call site over.
        //
        // The name changed with the meaning: it is not the regolith, it is being on the far side of your own
        // door — whichever door this world has (AwayTeamSide).
        bool awayFromSafety = onExcursion
            && !AwayTeamSide.BackAtTheShuttle(OnWreck, _avatarX, _avatarY, DeckPlan.AvatarRadius)
            && !inShelter;

        // #380 item 2: the band this frame opened on — so once, per excursion, we can speak the FIRST slide
        // down a rung (naming the cause and the remedy the bare gauge never did). Recovery only ever raises
        // the nerve, so a fall can arise solely from the regolith's toll below.
        NerveModel.NerveBand bandBefore = NerveModel.BandFor(_nerve);

        // #379 · the per-spell sighting tally still lives here (the tracker's own hearing decides what counts
        // as a fresh contact); #480 prices the result in whole pips instead of a shaped float.
        int heardMovers = 0;
        if (awayFromSafety && _surface is not null)
        {
            // #446: the tracker's fan still HEARS to its full detection range — that far, faint blip is the
            // whole point of the instrument. But a contact only FRIGHTENS you inside the dread range.
            double detection = Math.Min(FanReach(), NerveModel.DreadRangeDeckUnits);
            var ents = _reevers.Select(r => new MotionTracker.Entity(r.X, r.Y, r.Vx, r.Vy))
                .Concat(_collectors.Select(c => new MotionTracker.Entity(c.X, c.Y, c.Vx, c.Vy)));
            heardMovers = MotionTracker.DetectedMovingCount(_avatarX, _avatarY, ents, detection);
        }
        // #480: charge ONLY the first fright of a spell. AdvanceSightings reports a fresh contact on every
        // RISE in the heard count, and with a pack weaving in and out of the dread range that rises over and
        // over — playtested as "something crests the tracker −1" four times in eight seconds, which is a
        // repeat-tax and exactly what the owner ruled against. `Seen == 0` is the spell's first fright; the
        // rest of the watch is free until the tracker has been quiet long enough to re-arm it.
        bool firstFrightOfSpell = _sightings.Seen == 0;
        (NerveModel.SightingSpell nextSpell, int freshSightings) =
            NerveModel.AdvanceSightings(_sightings, heardMovers, dtRealSeconds);
        _sightings = nextSpell;

        var frame = new NervePips.Frame(
            OnExcursion: onExcursion,
            OnRegolith: awayFromSafety,
            // There is no monolith aboard a dead ship, and now that a wreck can BE exposed ground the guard
            // has to be said rather than left to arithmetic (#637's whole list is things satisfied by
            // accident). SeesMonolith() now answers the question properly on its own — it asks
            // Monolith.StandsOn, the same predicate the renderer builds the slab from, and a wreck id is
            // not the canon moon — so !OnWreck is belt to that braces rather than the only thing holding it.
            SeesMonolith: !OnWreck && awayFromSafety && SeesMonolith(),
            // #446 (owner, live 2026-07-26: "The reevers should not lower sanity unless they get REALLY
            // close"). ChaseActive used to be the bare `_reevers.Count > 0` — a pack EXISTING anywhere on
            // the field, so one Old One drifting on the far rim taxed the captain at the same flat rate as
            // one at their shoulder, and the gauge bottomed out before anything ever reached them. Now we
            // hand Core the distance to the nearest hunter and it prices the dread off that; the moving
            // count is likewise only the ones near enough to matter, so a far-off tide is atmosphere.
            Stressors: awayFromSafety
                ? new NerveModel.Stressors(
                    CountMovingReeversWithin(NerveModel.DreadRangeDeckUnits),
                    _reevers.Count > 0,
                    _surface!.Channeling,
                    IsCornered(),
                    NearestReeverRange())
                : default,
            FreshSightings: awayFromSafety && firstFrightOfSpell ? freshSightings : 0,
            Touched: _touchedThisFrame,
            DtSeconds: dtRealSeconds,
            // #480 · fear tracks MORTAL DANGER: below a couple of blows left, every further hand costs its
            // pip again instead of being absorbed by the once-per-encounter latch.
            HealthPipsLeft: _surface is { } hurt ? CaptainCondition.MaxHits - hurt.HitsTaken : int.MaxValue,
            // THE DWELL. The one sustained pressure that is not a regolith pressure: a derelict's interior
            // reads as "aboard" to every other rule in NervePips, so standing beside the archive node would
            // otherwise be scored SAFE and hand pips BACK while the ledger said it was taking them.
            InArchiveField: InArchiveField,
            // #867 · WHAT KIND OF SAFE. The ease beat is unchanged; the SENTENCE it prints asks the ground.
            // A pressurised floor of the Hive has been "safe" to this model since #585 and there is no
            // airlock anywhere in it — which is why the owner's ledger, on a park lawn under grow-lights,
            // kept saying "the airlock closes behind you +1".
            SafeGroundHoldsPressure: StandingOnGroundThatHoldsPressure);

        NervePips.Step step = NervePips.Advance(_nerve, _monolithSeen, _nerveBeats, in frame);
        bool monolithFired = !_monolithSeen && step.MonolithSeen;
        _nerve = step.Nerve;
        _monolithSeen = step.MonolithSeen;
        _nerveBeats = step.Beats;
        _touchedThisFrame = false;

        // THE DELIVERABLE OF #480: every pip that moved says why — a line by the gauge in the moment, and a
        // bounded ledger on the Captain desk that can be read back afterwards (and by the death card).
        if (step.Events.Count > 0)
        {
            _nerveLedger = NervePips.Record(_nerveLedger, step.Events);
            FlashNerve(step.Events[^1]);
        }

        if (monolithFired)
        {
            RendererInterop.PlayCue("alarm");
            // #380 item 8: name the bill the shock just dealt — the poetic beat and the NERVE gauge shake hands.
            ShowPulseMessage("👁 The monolith resolves out of the dark — too regular, too old, too patient. Something behind your eyes lurches — your nerve takes the hit.");
            RequestVaultSave();
            // #400 §3: first human eyes on the monolith — the once-in-a-life beat offers a shot for the record.
            // The backdrop the captain's portrait composites onto. This beat shipped with NO vista at all,
            // so the marquee once-in-a-life shot was a portrait disc floating on an empty stage — owner,
            // 2026-07-28: "kind of lame". It now poses against the canon ground doing what the canon ground
            // does: the monolith behind, the pack closing, and GATE-1 firing over your shoulder.
            OfferSelfie(SelfieBeats.FirstMonolith, "art/selfie-monolith.jpg");
        }

        // #649 · AND THE ARRIVAL, which is a different beat from the first sight and lands much closer in.
        //
        // Monolith.ApproachLine has existed since #586 with NO CALLER — designed and never consumed, which
        // QAHandoff-StoryTelling.md §1 names as its own failure class. It is the one beat of pure SCALE the
        // crude grid cannot draw by itself, and the scale pass is exactly where it belongs: the nerve hit
        // fires at three fifths of the thing's height away, while it is still a shape; this fires when you
        // cross onto the swept ground and it fills the view. Two beats, two distances, both derived from how
        // big the thing actually is.
        if (onExcursion && _surface is { MonolithApproachAnnounced: false } walk
            && Monolith.StandsOn(walk.Stop.Body.Id, walk.Site.LayoutSalt)
            && DistanceToAnchorSquared() <= Monolith.ApproachRangeDu * Monolith.ApproachRangeDu)
        {
            walk.MonolithApproachAnnounced = true;
            ShowAndFile(Monolith.ApproachLine, "▮");
        }

        // #649 · AND THEN, RARELY, THE GROUND DOES SOMETHING. See MonolithWatch for the register and the
        // three gates; this is only the clock and the telling. Owner's reference: Babylon 5, Sheridan and
        // the giants on the playground — "background puppeteers watching if their kids perform in the school
        // play." Parental, not predatory: it costs nothing, it never repeats inside an excursion, and the
        // world never remarks on it afterwards.
        StepMonolithWatch(dtRealSeconds);

        // #380 item 2: the one-per-excursion band-drop pulse. The first time this frame's toll drops the nerve
        // a whole rung (Steady→Rattled, or lower), say WHY it falls and HOW to mend it — the cause+remedy the
        // bare gauge never showed. Latched on the excursion (a fresh landing re-arms it), the house one-time idiom.
        if (onExcursion && _surface is { NerveBandDropAnnounced: false } dropEx
            && NerveModel.BandFor(_nerve) > bandBefore)
        {
            dropEx.NerveBandDropAnnounced = true;
            ShowPulseMessage("Nerves fraying — Reevers, digging under threat, and worse all take their toll. Get back aboard to steady them.");
        }
    }

    // #480 · Say it, then keep it. The flash is the in-the-moment cause ("it laid hands on you  −1") that
    // hangs by the gauge for a beat; the ledger is the same line kept so the Captain desk — and the death
    // card — can answer "what broke me?" after the fact.
    private void FlashNerve(NervePips.Event e)
    {
        _nerveFlash = e.Line;
        _nerveFlashUntilMs = (_lastTimestampMs ?? 0) + NerveFlashMs;
    }

    /// <summary>The ONE way anything outside the regolith law may move the nerve (#480). Takes the old
    /// storage-scale amount and a plain-words label, banks anything under a whole pip, and — when a pip
    /// actually moves — flashes it and files it in the ledger. Nothing may change the gauge anonymously:
    /// if a caller cannot name its shock in the house voice, it has no business spending the captain's nerve.
    /// </summary>
    private void ApplyNerveShock(double rawAmount, string label)
    {
        (double nerve, double carry, NervePips.Event? e) =
            NervePips.ApplyShock(_nerve, _nerveShockCarry, rawAmount, label);
        _nerve = nerve;
        _nerveShockCarry = carry;
        if (e is { } fired)
        {
            _nerveLedger = NervePips.Record(_nerveLedger, [fired]);
            FlashNerve(fired);
        }
    }

    /// <summary>The relief seam's counterpart (#308/#321 → #480): a drink, a pill, a bunk or a shared glass
    /// gives WHOLE pips back and says so, so a recovery is exactly as legible as a loss.</summary>
    private void ApplyNerveRelief(double rawRestore)
    {
        (double nerve, NervePips.Event? e) = NervePips.ApplyRelief(_nerve, rawRestore);
        _nerve = nerve;
        if (e is { } fired)
        {
            _nerveLedger = NervePips.Record(_nerveLedger, [fired]);
            FlashNerve(fired);
        }
    }

    /// <summary>The flash line, while it is still fresh — what the gauge writes beside the pips.</summary>
    private string? LiveNerveFlash =>
        _nerveFlash is not null && (_lastTimestampMs ?? 0) < _nerveFlashUntilMs ? _nerveFlash : null;

    /// <summary>The ledger as plain lines for the corner — newest first.</summary>
    private IReadOnlyList<string>? NerveLedgerLines =>
        _nerveLedger.Count == 0 ? null : _nerveLedger.Select(e => e.Line).ToList();

    /// <summary>The DEAD captain's ledger, snapshotted at the rebirth seam so the death card can answer
    /// "what broke you?" after the live one has been handed clean to the new captain.</summary>
    private IReadOnlyList<NervePips.Event> _deathNerveLedger = [];

    /// <summary>Those same events as lines, for the death card.</summary>
    private IReadOnlyList<string>? DeathNerveLedgerLines =>
        _deathNerveLedger.Count == 0 ? null : _deathNerveLedger.Select(e => e.Line).ToList();

    private int CountMovingReevers()
    {
        int n = 0;
        foreach (Reever r in _reevers)
        {
            if (MotionTracker.IsMoving(r.Vx, r.Vy))
            {
                n++;
            }
        }
        return n;
    }

    // #456 · A NOISE ON THE GROUND. Owner, 2026-07-27: "they can hear digging etc loud noises, but generally
    // they have to spot you by hearing or by seeing before they give chase… when they are initially behind
    // obstructions except maybe one or two they do not participate in chasing you if they don't know where
    // you are." This is that ear, and it is what keeps the un-leashed pack (#453) fair.
    //
    // What a Reever gets from a sound is a PLACE, not a target: it learns where the noise came from and goes
    // to look. Hearing ignores walls on purpose — stone hides you from eyes, never from ears — so digging
    // behind a monolith buys sight-cover and nothing else. Move after making noise and they converge on an
    // empty hole, which is a real tactic.
    private void MakeNoise(double x, double y, ReeverHearing.Noise noise)
    {
        if (_surface is not { } ex)
        {
            return;
        }
        // #461: the arrival grace covers the EAR too, or the first shovel-stroke would undo it.
        if (!SurfaceArrival.CanBeSpotted(((_lastTimestampMs ?? 0) - ex.LandedAtMs) / 1000.0))
        {
            return;
        }
        double reachSq = ReeverHearing.RangeOf(noise) * ReeverHearing.RangeOf(noise);
        foreach (Reever r in _reevers)
        {
            double dx = r.X - x, dy = r.Y - y;
            if ((dx * dx) + (dy * dy) > reachSq)
            {
                continue; // too far to have heard it — it keeps its ground (#446's feature)
            }
            // It heard SOMETHING, and now it knows a spot worth walking to. If the captain is still there
            // when it arrives it sees them the honest way; if not, the trail leads to a hole in the ground.
            r.LastSeenX = x;
            r.LastSeenY = y;
            r.EverSeen = true;
        }
    }
}

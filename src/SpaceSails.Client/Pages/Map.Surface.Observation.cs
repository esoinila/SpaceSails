using SpaceSails.Core;

namespace SpaceSails.Client.Pages;

// Part of Map.Surface (#870 split; the header note lives in Map.Surface.cs) — #436, the observation roll:
// the look one Old One takes down a sightline, and the moment it stops being a look.
public partial class Map
{
    // ── #436 · THE EYE ROLLS ──────────────────────────────────────────────────────────────────────────
    //
    // Owner, live 2026-07-26: "There needs to be a reevers observation roll to its line of sight
    // environment. Make an issue about that. Then the moment reever discovers becomes special. 🤭"
    //
    // WHAT USED TO BE HERE, in Map.Surface.Reevers' chase loop, was four lines with no dice in them: the
    // frame geometry allowed a sightline and the latch flipped, silently, in the same instant. Two audits
    // (2026-08-02 and 2026-09-03) said the same two things about it — there was no per-frame churn to unpick,
    // and the moment of discovery produced not one byte of output.
    //
    // So the geometry becomes PERMISSION and the rule that decides is ReeverObservation's, in Core, pure and
    // seeded off the one shared DiceRule. This file is the client's whole share of it: the world the odds
    // read, the state the contact carries between looks, and the one authored sentence.
    //
    // THE CLOCK IS THE SURFACE'S OWN, and that is not a detail. #469 is the scar: the arrival grace was
    // measured on SimTime, which is the SHIP's orbital clock and barely advances while boots are on a
    // regolith, so the grace never expired and no Old One could ever notice anybody — they walked to the spot
    // they were born knowing and froze there for the rest of the game. Every surface clock that has been
    // written since is the rAF one (_lastTimestampMs): the swing cooldown, the blood fade, the dig settle,
    // the grace itself. The look cadence is a real-seconds quantity by its own definition and joins them.

    /// <summary>Real seconds on the ground, the one clock every surface cadence is measured on.</summary>
    private double SurfaceSeconds => (_lastTimestampMs ?? 0) / 1000.0;

    // Where the captain was when the motion was last measured, and how fast they were going. NaN is "not
    // measured yet", which is a different thing from "not moving" and must not read as either.
    private double _lookPrevAvatarX = double.NaN;
    private double _lookPrevAvatarY;
    private double _captainSpeedDu;

    /// <summary>
    /// #436 · HOW FAST THE CAPTAIN IS ACTUALLY GOING, measured exactly the way a contact's own velocity is
    /// measured one file over: distance covered over the frame's own delta. One number, one law — the eye
    /// and the motion fan must not each work out "is he moving" for themselves, because two authorities
    /// agreeing today is this repo's most expensive habit.
    ///
    /// <para><b>A placement is not a sprint.</b> <c>StandCaptainAt</c> sets the captain down at a lift
    /// landing, a stair head, a rescued square — tens of deck units in one frame, which as a speed is a
    /// number no pair of legs can make. Anything past twice a full walk is therefore read as what it is and
    /// reported as STILL, so surfacing out of the shed can never hand the whole field a free look at a
    /// captain who has not taken a step.</para>
    /// </summary>
    private void MeasureTheCaptainsMotion(double dtRealSeconds)
    {
        if (dtRealSeconds <= 0)
        {
            return;   // a zero-length frame measures nothing; keep the last honest reading
        }
        if (double.IsNaN(_lookPrevAvatarX))
        {
            _lookPrevAvatarX = _avatarX;
            _lookPrevAvatarY = _avatarY;
            _captainSpeedDu = 0;
            return;
        }

        double dx = _avatarX - _lookPrevAvatarX, dy = _avatarY - _lookPrevAvatarY;
        _lookPrevAvatarX = _avatarX;
        _lookPrevAvatarY = _avatarY;
        double speed = Math.Sqrt((dx * dx) + (dy * dy)) / dtRealSeconds;
        _captainSpeedDu = speed > TeleportSpeedDu ? 0.0 : speed;
    }

    /// <summary>Above this, a change of position was a PLACEMENT and not a walk. Twice the suit's own full
    /// walk, so nothing a captain can do with the keys ever trips it and nothing a placement does ever
    /// misses it.</summary>
    private const double TeleportSpeedDu = SuitAir.WalkSpeedDu * 2.0;

    /// <summary>
    /// #436 · ONE CONTACT'S LOOK, this frame.
    ///
    /// <para>Called for EVERY awake contact every frame, with or without a sightline, because the head going
    /// back down is as much a part of the beat as the head coming up: <see cref="Reever.Stirred"/> is not
    /// latched, so backing behind stone genuinely un-notices you, which is the fear window the issue is
    /// about. Only the FIX latches, and it latches for the excursion.</para>
    ///
    /// <para>The die itself is cast at most once per <see cref="ReeverObservation.LookIntervalSeconds"/> per
    /// contact and Core owns that decision, so this method is a handful of comparisons on an ordinary
    /// frame.</para>
    /// </summary>
    /// <param name="r">The contact taking the look.</param>
    /// <param name="clearLine">Whether it may look at all: the arrival grace has run AND no stone (nor,
    /// aboard, a shut door) stands between the two. Both are the caller's answers and are not re-decided.</param>
    private void TakeALook(Reever r, bool clearLine)
    {
        ReeverObservation.Glance glance = ReeverObservation.Look(
            clearLine, r.EverSeen, r.JitterSeed, SurfaceSeconds, r.LastLookIndex, WhatTheLookAffords(r));

        r.LastLookIndex = glance.LookIndex;
        r.Stirred = glance.State == ReeverObservation.Watch.Stirred;

        // #324 STAYS WHOLE, AND THE `clearLine` HALF OF THIS IS THE WHOLE OF IT. `Look` reports Fixed for a
        // contact that already has you whether or not it can see you — because that is the LATCH, and the
        // latch is one-way. What the latch is NOT is knowledge of where you are standing now.
        //
        // Written without that half first, and it quietly repealed the oldest law on this ground: an Old One
        // that had once seen the captain went on writing his live position into its memory through a slab,
        // which is exactly what #324 forbids ("duck behind stone and the hunter loses your live position —
        // the maze becomes a real instrument"). It broke no existing test, because every guard the repo has
        // about the maze watches a contact that has NOT yet seen you. The one that watches a contact that
        // HAS is AFixedOneStillLosesYouBehindStone.
        if (!clearLine || glance.State != ReeverObservation.Watch.Fixed)
        {
            return;
        }

        // It has you AND it can see you: the look becomes knowledge of WHERE. Done for an already-fixed
        // contact too — that is the old "tracks your live position while the look holds" behaviour (#324),
        // unchanged.
        r.LastSeenX = _avatarX;
        r.LastSeenY = _avatarY;
        if (r.EverSeen)
        {
            return;   // it already had you. The latch is one-way and there is no second discovery.
        }

        r.EverSeen = true;
        r.Stirred = false;   // past stirred: it is not deciding any more, it is coming
        TheMomentItFixes();
    }

    /// <summary>What the sightline affords this contact right now — range, the captain's own motion, and the
    /// loudest thing about what they are doing. Nothing else: the odds read the world and never the
    /// contact's history.</summary>
    private ReeverObservation.View WhatTheLookAffords(Reever r)
    {
        double dx = r.X - _avatarX, dy = r.Y - _avatarY;
        return new ReeverObservation.View(
            Math.Sqrt((dx * dx) + (dy * dy)), _captainSpeedDu, WhatTheCaptainIsDoing());
    }

    /// <summary>
    /// #436 · THE CAPTAIN'S BUSINESS, as one ordered signal — the loudest thing about them this instant.
    ///
    /// <para>Read in descending order and the first match wins, because the enum's own order IS the claim
    /// (each is more of a gift to a watching eye than the one before it) and a captain hauling a chest away
    /// from a hole under a firing gun is, to anyone looking, simply the muzzle flash.</para>
    ///
    /// <para><b>The flash has to be beside him.</b> A bot holding an arc on the far rim lights the far rim.
    /// So a volley counts as the captain's own giveaway only inside
    /// <see cref="NerveModel.DreadRangeDeckUnits"/> — the range at which Core already says an Old One has
    /// stopped being scenery, borrowed rather than re-typed. It is the eye's half of #456's doctrine: your
    /// own guns are the loudest thing on the moon, and on a ground with no light of its own they are also
    /// the brightest.</para>
    /// </summary>
    private ReeverObservation.Doing WhatTheCaptainIsDoing()
    {
        if (_surface is not { } ex)
        {
            return ReeverObservation.Doing.Nothing;
        }

        double nowMs = _lastTimestampMs ?? 0;
        double flashSq = NerveModel.DreadRangeDeckUnits * NerveModel.DreadRangeDeckUnits;
        foreach (SurfaceBot b in ex.Bots)
        {
            if (!b.Deployed || b.FiringUntilMs <= nowMs)
            {
                continue;
            }
            double bx = b.X - _avatarX, by = b.Y - _avatarY;
            if ((bx * bx) + (by * by) <= flashSq)
            {
                return ReeverObservation.Doing.MuzzleFlash;
            }
        }

        if (ex.Channel is not null)
        {
            return ReeverObservation.Doing.Digging;   // bent over one spot, moving, and not stopping
        }
        return ex.Carrying ? ReeverObservation.Doing.Hauling : ReeverObservation.Doing.Nothing;
    }

    /// <summary>
    /// #436 · <b>THE MOMENT IT FIXES</b> — the beat the 2026-08-02 story-QA run found had a measurable gap:
    /// <i>"A player cannot tell the frame it happened from the frame before it, and by the time they can
    /// tell, it is a shamble already halfway across the field."</i>
    ///
    /// <para>One authored sentence, on the suit's pulse, at <see cref="PulseRank.Climax"/> because it is
    /// exactly what that rank is for — the sentence a whole feature was built to say, which nothing routine
    /// may stand on top of. Filed as well as said, so the book keeps the moment the ground stopped being
    /// empty.</para>
    ///
    /// <para><b>Once per excursion.</b> The latch is one-way by canon: after the first fix the field is never
    /// clean again, so a second announcement would be reporting old news over the sound of the thing actually
    /// coming. The ear (<c>MakeNoise</c>) never fires this at all — a contact that was handed a PLACE by a
    /// shovel is walking to a hole, and "it is looking at you" would be a sentence disagreeing with the sim,
    /// which is a named bug class on this ground and not a turn of phrase.</para>
    ///
    /// <para>No card, no banner. The owner's own note on the shape: <i>"SECURITY ALERTED as a banner is the
    /// wrong shape"</i> — the head coming up is drawn (the pose turns to you), the coming is the walk, and
    /// this is the one thing said out loud between them.</para>
    /// </summary>
    private void TheMomentItFixes()
    {
        if (_surface is not { } ex || ex.FixedOnYouSaid)
        {
            return;
        }
        ex.FixedOnYouSaid = true;
        ShowAndFile(ReeverObservation.FixedOnYouLine, ReeverObservation.FixedOnYouGlyph, PulseRank.Climax);

        // AND NO CUE. A klaxon was written here and taken out again: the 2026-09-05 canon pass enumerates
        // exactly three channels for this beat — the pose, the sentence, and the walk — and a sound is a
        // fourth. It is also the wrong sound. "alarm" is what a sentry volley and a hand on your suit say,
        // and this moment is the opposite of those: nothing has happened yet. The owner's own note on the
        // shape is the argument — "the scariest version of 'it noticed you' is the ground going quiet."
    }
}
